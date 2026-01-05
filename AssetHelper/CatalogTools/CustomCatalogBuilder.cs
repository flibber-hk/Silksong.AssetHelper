using Silksong.AssetHelper.BundleTools;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;

namespace Silksong.AssetHelper.CatalogTools;

/// <summary>
/// Class to help in building a custom catalog with base game dependencies.
/// 
/// This class should only be used after the base game catalog has loaded.
/// </summary>
internal class CustomCatalogBuilder
{
    private readonly string _primaryKeyPrefix;
    
    private readonly Dictionary<string, ContentCatalogDataEntry> _baseBundleEntries;
    private readonly HashSet<string> _includedBaseBundles = [];
    private readonly Dictionary<string, string> _basePrimaryKeys = [];

    private readonly List<ContentCatalogDataEntry> _addedEntries = [];

    public CustomCatalogBuilder(string primaryKeyPrefix = "AssetHelper")
    {
        _primaryKeyPrefix = primaryKeyPrefix;
        _baseBundleEntries = [];
        foreach (IResourceLocation location in Addressables.ResourceLocators.First().AllLocations)
        {
            if (location.ResourceType != typeof(IAssetBundleResource))
            {
                continue;
            }
            if (location.PrimaryKey.StartsWith("scenes_scenes_scenes"))
            {
                continue;
            }

            if (!AddressablesData.TryStrip(location.PrimaryKey, out string? bundleName))
            {
                continue;
            }

            string primaryKey = $"{primaryKeyPrefix}_{bundleName}";
            ContentCatalogDataEntry entry = CatalogEntryUtils.CreateEntryFromLocation(location, primaryKey);

            _baseBundleEntries.Add(bundleName, entry);
            _basePrimaryKeys.Add(bundleName, primaryKey);
        }
    }

    public void AddRepackedSceneData(string sceneName, RepackedBundleData data, string bundlePath)
    {
        // Create an entry for the bundle
        string repackedSceneBundleKey = $"{_primaryKeyPrefix}/RepackedScenes/{sceneName}";

        ContentCatalogDataEntry bundleEntry = CatalogEntryUtils.CreateBundleEntry(
                repackedSceneBundleKey,
                bundlePath,
                data.BundleName!,
                []);
        _addedEntries.Add(bundleEntry);

        // Get dependency list
        List<string> dependencyKeys = [repackedSceneBundleKey];
        foreach (string dep in BundleDeps.DetermineDirectDeps($"scenes_scenes_scenes/{sceneName}.bundle"))
        {
            string depKey = dep.Replace(".bundle", "");
            _includedBaseBundles.Add(depKey);
            dependencyKeys.Add(_basePrimaryKeys[depKey]);
        }

        // Create entries for the assets
        foreach ((string containerPath, string objPath) in data.GameObjectAssets ?? [])
        {
            ContentCatalogDataEntry entry = CatalogEntryUtils.CreateAssetEntry(
                containerPath,
                typeof(GameObject),
                dependencyKeys,
                $"{_primaryKeyPrefix}/RepackedAssets/{sceneName}/{objPath}"
                );
            _addedEntries.Add(entry);
        }
    }

    // TODO - this should produce information about the catalog
    public string Build(string? catalogId = null)
    {
        catalogId ??= _primaryKeyPrefix;

        List<ContentCatalogDataEntry> allEntries = [.. _includedBaseBundles.Select(x => _baseBundleEntries[x]), .. _addedEntries];

        string catalogPath = CatalogUtils.WriteCatalog(allEntries, catalogId);

        return catalogPath;
    }
}
