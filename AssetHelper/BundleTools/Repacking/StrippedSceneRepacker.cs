using AssetsTools.NET.Extra;
using Silksong.AssetHelper.Internal;
using System;
using System.Collections.Generic;
using GameObjectInfo = Silksong.AssetHelper.BundleTools.GameObjectLookup.GameObjectInfo;

namespace Silksong.AssetHelper.BundleTools.Repacking;

/// <summary>
/// Class that repacks scenes by taking a minimal set of objects in the scene that allow
/// all provided game objects to be loaded.
/// 
/// Any game objects whose parents are not needed will be deparented.
/// </summary>
public class StrippedSceneRepacker : SceneRepacker
{
    /// <inheritdoc />
    public override RepackedBundleData Repack(string sceneBundlePath, List<string> objectNames, string outBundlePath)
    {
        objectNames = objectNames.GetHighestNodes();

        RepackedBundleData outData = new();
        AssetsManager mgr = BundleUtils.CreateDefaultManager();

        GetDefaultBundleNames(sceneBundlePath, objectNames, outBundlePath, out string newCabName, out string newBundleName);
        outData.BundleName = newBundleName;
        outData.CabName = newCabName;

        BundleFileInstance sceneBun = mgr.LoadBundleFile(sceneBundlePath);
        if (!TryFindAssetsFiles(mgr, sceneBun, out AssetsFileInstance? mainSceneAfileInst, out AssetsFileInstance? sceneSharedAssetsFileInst))
        {
            throw new NotSupportedException($"Could not find assets files for {sceneBundlePath}");
        }

        GameObjectLookup goLookup = GameObjectLookup.CreateFromFile(mgr, mainSceneAfileInst);

        Dictionary<string, BundleUtils.ChildPPtrs> dependencies = [];
        foreach (string objName in objectNames)
        {
            if (goLookup.TryLookupName(objName, out GameObjectInfo? info))
            {
                dependencies[objName] = mgr.FindBundleDependentObjects(mainSceneAfileInst, info.GameObjectPathId);
            }
            else
            {
                AssetHelperPlugin.InstanceLogger.LogError($"Couldn't find game object {objName}");
            }
        }

        HashSet<long> internalPathIds = [];
        foreach (BundleUtils.ChildPPtrs deps in dependencies.Values)
        {
            internalPathIds.UnionWith(deps.InternalPaths);
        }

        // Collect all game objects that are being included
        List<string> includedGos = [];
        foreach (long pathId in internalPathIds)
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
            if (ObjPathUtil.TryGetAncestor(rootmostGos, objName, out string? ancestor, out _))
            {
                includedContainerGos.Add(ancestor);
            }
        }

        // Recalculate dependencies for the roots if needed
        Dictionary<string, BundleUtils.ChildPPtrs> containerDeps = [];
        foreach (string name in includedContainerGos)
        {
            GameObjectInfo info = goLookup.LookupName(name);

            long gameObjectId = info.GameObjectPathId;
            long transformId = info.TransformPathId;
            if (dependencies.TryGetValue(name, out BundleUtils.ChildPPtrs deps))
            {
                containerDeps[name] = deps;
            }
            else
            {
                deps = mgr.FindBundleDependentObjects(mainSceneAfileInst, gameObjectId);
                containerDeps[name] = deps;
            }
        }

        // Actually do the repacking - TODO

        outData.GameObjectAssets = rootmostGos;
        return outData;
    }

}
