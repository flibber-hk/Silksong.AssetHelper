using AssetsTools.NET;
using AssetsTools.NET.Extra;
using Silksong.AssetHelper.Internal;
using System.Collections.Generic;
using System.IO;

namespace Silksong.AssetHelper.Core;

/// <summary>
/// Helpers for determining bundle dependencies, with the results automatically cached.
/// </summary>
public static class BundleDeps
{
    internal static void Setup()
    {
        CabLookup = CachedObject<IReadOnlyDictionary<string, string>>.CreateSynced("cabs.json", GenerateCabLookup).Value;
        DirectDependencyLookup = CachedObject<Dictionary<string, List<string>>>.CreateSynced("direct_deps.json", () => []);
    }
    
    /// <summary>
    /// Lookup for cab name to bundle path.
    /// </summary>
    public static IReadOnlyDictionary<string, string> CabLookup { get; private set; } = null!;

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

    private static CachedObject<Dictionary<string, List<string>>> DirectDependencyLookup { get; set; } = null!;

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
        string sceneBundlePath = Path.Combine(AssetPaths.BundleFolder, bundleFile);
        using MemoryStream ms = new(File.ReadAllBytes(sceneBundlePath));
        BundleFileInstance bun = mgr.LoadBundleFile(ms, sceneBundlePath);

        AssetsFileInstance afileInst = mgr.LoadAssetsFileFromBundle(bun, 0, false);
        AssetsFile afile = afileInst.file;

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
}
