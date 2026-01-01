using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Silksong.AssetHelper.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using GameObjectInfo = Silksong.AssetHelper.BundleTools.GameObjectLookup.GameObjectInfo;

namespace Silksong.AssetHelper.BundleTools.Repacking;

/// <summary>
/// Alternate testing version of the stripped scene repacker.
/// </summary>
public class InplaceStrippingSceneRepacker : SceneRepacker
{
    /// <inheritdoc />
    public override RepackedBundleData Repack(string sceneBundlePath, List<string> objectNames, string outBundlePath)
    {
        objectNames = objectNames.GetHighestNodes();

        RepackedBundleData outData = new();
        AssetsManager mgr = BundleUtils.CreateDefaultManager();

        GetDefaultBundleNames(sceneBundlePath, objectNames, outBundlePath, out string newCabName, out string newBundleName);
        GetDefaultBundleNames(sceneBundlePath, null, outBundlePath, out string newSACabName, out _);
        outData.BundleName = newBundleName;
        outData.CabName = newCabName;

        BundleFileInstance sceneBun = mgr.LoadBundleFile(sceneBundlePath);
        if (!mgr.TryFindAssetsFiles(sceneBun, out BundleUtils.SceneBundleInfo sceneBundleInfo))
        {
            throw new NotSupportedException($"Could not find assets files for {sceneBundlePath}");
        }

        AssetsFileInstance mainSceneAfileInst = mgr.LoadAssetsFileFromBundle(sceneBun, sceneBundleInfo.mainAfileInstIndex);
        AssetsFileInstance sceneSharedAssetsFileInst = mgr.LoadAssetsFileFromBundle(sceneBun, sceneBundleInfo.sharedAssetsAfileIndex);
        int mainAfileIdx = sceneBundleInfo.mainAfileInstIndex;
        int sharedAssetsAfileIdx = sceneBundleInfo.sharedAssetsAfileIndex;

        GameObjectLookup goLookup = GameObjectLookup.CreateFromFile(mgr, mainSceneAfileInst);

        AssetDependencies dependencies = new(mgr, mainSceneAfileInst);
        HashSet<long> includedPathIds = [];

        foreach (string objName in objectNames)
        {
            if (goLookup.TryLookupName(objName, out GameObjectInfo? info))
            {
                includedPathIds.Add(info.GameObjectPathId);
                includedPathIds.UnionWith(dependencies.FindBundleDeps(info.GameObjectPathId).InternalPaths);
            }
            else
            {
                AssetHelperPlugin.InstanceLogger.LogError($"Couldn't find game object {objName}");
            }
        }

        // Collect all game objects that are being included
        List<string> includedGos = [];
        foreach (long pathId in includedPathIds)
        {
            if (goLookup.TryLookupGameObject(pathId, out GameObjectInfo? info))
            {
                includedGos.Add(info.GameObjectName);
            }
        }
        List<string> rootmostGos = includedGos.GetHighestNodes();

        // Generate a path for each rootmost go which has a child in the request
        HashSet<string> includedContainerGos = [];
        foreach (string objName in objectNames)
        {
            if (ObjPathUtil.TryFindAncestor(rootmostGos, objName, out string? ancestor, out _))
            {
                includedContainerGos.Add(ancestor);
            }
            else
            {
                AssetHelperPlugin.InstanceLogger.LogWarning($"Did not find {objName} in bundle");
            }
        }

        // Strip all assets that are not needed
        foreach (AssetFileInfo afileInfo in mainSceneAfileInst.file.AssetInfos.ToList())
        {
            if (!includedPathIds.Contains(afileInfo.PathId))
            {
                mainSceneAfileInst.file.Metadata.RemoveAssetInfo(afileInfo);
            }
        }

        // Deparent transforms which are now rooted
        foreach (GameObjectInfo current in goLookup)
        {
            if (!includedPathIds.Contains(current.TransformPathId))
            {
                continue;
            }

            if (!current.GameObjectName.TryGetParent(out string parentName))
            {
                // No need to deparent what is already a root go
                continue;
            }

            if (!goLookup.TryLookupName(parentName, out GameObjectInfo? parentInfo))
            {
                AssetHelperPlugin.InstanceLogger.LogWarning($"Unexpectedly failed to find {parentName} from {current.GameObjectName}");
                continue;
            }

            if (includedPathIds.Contains(parentInfo.TransformPathId))
            {
                continue;
            }

            // We now have to deparent the object
            AssetFileInfo afInfo = mainSceneAfileInst.file.GetAssetInfo(current.TransformPathId);
            AssetTypeValueField transformField = mgr.GetBaseField(mainSceneAfileInst, afInfo);
            transformField["m_Father.m_PathID"].AsLong = 0;
            afInfo.SetNewData(transformField);
        }

        // Update the externals on the shared assets bundle
        sceneSharedAssetsFileInst.file.Metadata.Externals.Clear();
        sceneSharedAssetsFileInst.file.Metadata.Externals.Add(new()
        {
            VirtualAssetPathName = "",
            Guid = new() { data0 = 0, data1 = 0, data2 = 0, data3 = 0 },
            Type = AssetsFileExternalType.Normal,
            PathName = $"archive:/{newCabName}/{newCabName}",
            OriginalPathName = $"archive:/{newCabName}/{newCabName}",
        });
        sceneSharedAssetsFileInst.file.Metadata.Externals.AddRange(mainSceneAfileInst.file.Metadata.Externals);

        // Set up the internal bundle
        AssetFileInfo internalBundle = sceneSharedAssetsFileInst.file.GetAssetsOfType(AssetClassID.AssetBundle).First();
        AssetTypeValueField iBundleData = mgr.GetBaseField(sceneSharedAssetsFileInst, internalBundle);

        // Set simple data
        iBundleData["m_Name"].AsString = newBundleName;
        iBundleData["m_AssetBundleName"].AsString = newBundleName;
        iBundleData["m_IsStreamedSceneAssetBundle"].AsBool = false;
        iBundleData["m_SceneHashes.Array"].Children.Clear();

        // Add main bundle as a dependency to the internal bundle
        AssetTypeValueField newDep = ValueBuilder.DefaultValueFieldFromArrayTemplate(iBundleData["m_Dependencies.Array"]);
        newDep.AsString = newCabName.ToLowerInvariant();
        iBundleData["m_Dependencies.Array"].Children.Add(newDep);

        // Add objects to the container
        List<AssetTypeValueField> preloadPtrs = [];
        List<AssetTypeValueField> newChildren = [];

        foreach (string containerGo in includedContainerGos)
        {
            GameObjectInfo cgInfo = goLookup.LookupName(containerGo);
            AssetDependencies.ChildPPtrs deps = dependencies.FindBundleDeps(cgInfo.GameObjectPathId);

            int start = preloadPtrs.Count;

            // Preload the asset itself
            AssetTypeValueField assetDep = ValueBuilder.DefaultValueFieldFromArrayTemplate(iBundleData["m_PreloadTable.Array"]);
            assetDep["m_FileID"].AsInt = 1;
            assetDep["m_PathID"].AsLong = goLookup.LookupName(containerGo).GameObjectPathId;
            preloadPtrs.Add(assetDep);

            foreach ((int fileId, long pathId) in deps.ExternalPaths)
            {
                AssetTypeValueField depPtr = ValueBuilder.DefaultValueFieldFromArrayTemplate(iBundleData["m_PreloadTable.Array"]);
                depPtr["m_FileID"].AsInt = fileId + 1;  // External 1 is the actual assets file, externals 2+ are the deps
                depPtr["m_PathID"].AsLong = pathId;
                preloadPtrs.Add(depPtr);
            }

            int count = preloadPtrs.Count - start;

            string containerPath = $"{nameof(AssetHelper)}/{containerGo}.prefab";

            AssetTypeValueField newChild = ValueBuilder.DefaultValueFieldFromArrayTemplate(iBundleData["m_Container.Array"]);
            newChild["first"].AsString = containerPath;
            newChild["second.preloadIndex"].AsInt = start;
            newChild["second.preloadSize"].AsInt = count;
            newChild["second.asset.m_FileID"].AsInt = 1;  // File ID is 1 because it's going across to the other assets file
            newChild["second.asset.m_PathID"].AsLong = cgInfo.GameObjectPathId;
            newChildren.Add(newChild);
        }

        iBundleData["m_PreloadTable.Array"].Children.Clear();
        iBundleData["m_PreloadTable.Array"].Children.AddRange(preloadPtrs);
        iBundleData["m_Container.Array"].Children.Clear();
        iBundleData["m_Container.Array"].Children.AddRange(newChildren);
        outData.GameObjectAssets = includedContainerGos.ToList();

        // Clear out all the non-internal asset bundle assets from the sharedassets
        foreach (AssetFileInfo afileInfo in sceneSharedAssetsFileInst.file.AssetInfos.ToList())
        {
            if (afileInfo.PathId != internalBundle.PathId)
            {
                sceneSharedAssetsFileInst.file.Metadata.RemoveAssetInfo(afileInfo);
            }
        }

        // Move the internal asset bundle to pathID 1
        if (internalBundle.PathId != 1)  // this should always be true
        {
            AssetFileInfo newInternalBundle = AssetFileInfo.Create(sceneSharedAssetsFileInst.file, 1, (int)AssetClassID.AssetBundle);
            newInternalBundle.SetNewData(iBundleData);
            sceneSharedAssetsFileInst.file.Metadata.AddAssetInfo(newInternalBundle);
            sceneSharedAssetsFileInst.file.Metadata.RemoveAssetInfo(internalBundle);
        }

        // Update the block and dir infos
        sceneBun.file.BlockAndDirInfo.DirectoryInfos[mainAfileIdx].SetNewData(mainSceneAfileInst.file);
        sceneBun.file.BlockAndDirInfo.DirectoryInfos[mainAfileIdx].Name = newCabName;

        sceneBun.file.BlockAndDirInfo.DirectoryInfos[sharedAssetsAfileIdx].SetNewData(sceneSharedAssetsFileInst.file);
        sceneBun.file.BlockAndDirInfo.DirectoryInfos[sharedAssetsAfileIdx].Name = newSACabName;

        int tot = sceneBun.file.BlockAndDirInfo.DirectoryInfos.Count;
        for (int i = 0; i < tot; i++)
        {
            if (i == mainAfileIdx) { continue; }
            if (i == sharedAssetsAfileIdx) { continue; }
            sceneBun.file.BlockAndDirInfo.DirectoryInfos[i].SetRemoved();
        }

        using (AssetsFileWriter writer = new(outBundlePath))
        {
            sceneBun.file.Write(writer);
        }

        return outData;
    }
}
