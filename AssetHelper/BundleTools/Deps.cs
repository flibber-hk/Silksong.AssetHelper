using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Silksong.AssetHelper.Internal;
using System.Collections.Generic;
using System.IO;

namespace Silksong.AssetHelper.BundleTools;

/// <summary>
/// Helpers for determining bundle dependencies
/// </summary>
public static class Deps
{
    internal static void Setup()
    {
        CabLookup = CacheManager.GetCached(GenerateCabLookup, "cabs.json");
        DirectDependencyLookup = new("direct_deps.json");
    }
    
    /// <summary>
    /// Lookup for bundle name to cab name.
    /// </summary>
    public static IReadOnlyDictionary<string, string> CabLookup { get; private set; } = null!;

    private static CachedObject<Dictionary<string, List<string>>> DirectDependencyLookup { get; set; } = null!;

    private static Dictionary<string, string> GenerateCabLookup()
    {
        AssetsManager mgr = new();

        string bundleFolder = AssetPaths.BundleFolder;

        Dictionary<string, string> lookup = [];

        foreach (string f in Directory.EnumerateFiles(bundleFolder, "*.bundle", SearchOption.AllDirectories))
        {
            string key = Path.GetRelativePath(bundleFolder, f).Replace("\\", "/");

            BundleFileInstance bun = mgr.LoadBundleFile(f);
            string cab = bun.file.GetFileName(0).Split(".")[0].ToLowerInvariant();
            lookup[cab] = key;
        }

        return lookup;
    }

    /// <summary>
    /// Determine the direct dependencies for a given bundle.
    /// </summary>
    /// <param name="bundleName"></param>
    /// <returns></returns>
    public static List<string> DetermineDirectDeps(string bundleName)
    {
        string bundleFile = bundleName;
        if (!bundleFile.EndsWith(".bundle"))
        {
            bundleFile = bundleFile + ".bundle";
        }

        if (DirectDependencyLookup.Value.TryGetValue(bundleFile, out List<string> deps))
        {
            return [.. deps];
        }

        AssetsManager mgr = new();

        BundleFileInstance bun = mgr.LoadBundleFile(Path.Combine(AssetPaths.BundleFolder, bundleFile));
        AssetsFileInstance afileInst = mgr.LoadAssetsFileFromBundle(bun, 0, false);
        AssetsFile afile = afileInst.file;
        AssetFileInfo assetInfos = afile.GetAssetsOfType(AssetClassID.AssetBundle)[0];

        List<string> computedDeps = [];
        foreach (AssetsFileExternal x in afile.Metadata.Externals)
        {
            string path = x.OriginalPathName;
            string cab = path.Split('/')[^1].Split(".")[0].ToLowerInvariant();
            if (!CabLookup.TryGetValue(cab, out string dep))
            {
                continue;
            }

            computedDeps.Add(dep);
        }

        DirectDependencyLookup.Value[bundleFile] = [.. computedDeps];

        return computedDeps;
    }

    /// <summary>
    /// Enumerate all pptrs, both within the current bundle and external to the current bundle, that
    /// are dependencies of this asset.
    /// </summary>
    /// <param name="mgr">The AssetsManager in use.</param>
    /// <param name="afileInst">The Assets file instance.</param>
    /// <param name="assetPathId">The path ID for the asset to check.</param>
    /// <param name="internalPaths">A list of path ids to assets within the current file
    /// that this asset depends on.</param>
    /// <param name="externalPaths">A list of (file id, path id) pairs to assets external
    /// to the current file that this asset depends on.</param>
    public static void FindDirectDependentObjects(
        AssetsManager mgr,
        AssetsFileInstance afileInst,
        long assetPathId,
        out List<long> internalPaths,
        out List<(int fileId, long pathId)> externalPaths
        )
    {
        HashSet<long> internalSeen = new([assetPathId]);
        HashSet<(int fileId, long pathId)> externalSeen = [];

        Queue<long> toProcess = new();
        toProcess.Enqueue(assetPathId);

        while (toProcess.TryDequeue(out long current))
        {
            AssetFileInfo info = afileInst.file.GetAssetInfo(current);
            AssetTypeTemplateField templateField = mgr.GetTemplateBaseField(afileInst, info);
            RefTypeManager refMan = mgr.GetRefTypeManager(afileInst);
            lock (afileInst.LockReader)
            {
                long assetPos = info.GetAbsoluteByteOffset(afileInst.file);
                AssetTypeValueIterator atvIterator = new(templateField, afileInst.file.Reader, assetPos, refMan);

                while (atvIterator.ReadNext())
                {
                    string typeName = atvIterator.TempField.Type;

                    if (!typeName.StartsWith("PPtr<")) continue;
                    
                    AssetTypeValueField valueField = atvIterator.ReadValueField();
                    int fileID = valueField["m_FileID"].AsInt;
                    long pathID = valueField["m_PathID"].AsLong;

                    if (pathID == 0)
                    {
                        // pptr target is null
                        continue;
                    }

                    if (fileID != 0)
                    {
                        externalSeen.Add((fileID, pathID));
                        continue;
                    }

                    if (internalSeen.Add(pathID))
                    {
                        toProcess.Enqueue(pathID);
                    }
                }
            }
        }

        internalPaths = [.. internalSeen];
        internalPaths.Remove(assetPathId);
        externalPaths = [.. externalSeen];
    }
}
