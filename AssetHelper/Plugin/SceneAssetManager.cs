using Silksong.AssetHelper.BundleTools;
using Silksong.AssetHelper.BundleTools.Repacking;
using Silksong.AssetHelper.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RepackDataCollection = System.Collections.Generic.Dictionary<string, Silksong.AssetHelper.BundleTools.RepackedBundleData>;

namespace Silksong.AssetHelper.Plugin;

/// <summary>
/// Class managing the scene repacking.
/// </summary>
public static class SceneAssetManager
{
    private static RepackDataCollection? _repackData;

    internal static event Action? SingleRepackOperationCompleted;

    /// <summary>
    /// Run a repacking procedure so that by the end, anything in toRepack which could be repacked has been.
    /// </summary>
    /// <param name="toRepack"></param>
    internal static void Run(Dictionary<string, HashSet<string>> toRepack)
    {
        string repackDataPath = Path.Combine(AssetPaths.RepackedSceneBundleDir, "repack_data.json");

        if (JsonExtensions.TryLoadFromFile<RepackDataCollection>(repackDataPath, out RepackDataCollection? repackData))
        {
            _repackData = repackData;
        }
        else
        {
            _repackData = [];
        }

        Dictionary<string, HashSet<string>> updatedToRepack = [];

        foreach ((string scene, HashSet<string> request) in toRepack)
        {
            if (!_repackData.TryGetValue(scene, out RepackedBundleData existingBundleData))
            {
                updatedToRepack[scene] = request;
                continue;
            }

            // TODO - cache invalidation based on version

            if (request.All(x => existingBundleData.TriedToRepack(x)))
            {
                // No need to re-repack as there's nothing new to try
                continue;
            }

            updatedToRepack[scene] = new(request
                .Union(existingBundleData.GameObjectAssets?.Values ?? Enumerable.Empty<string>())
                .Union(existingBundleData.NonRepackedAssets ?? Enumerable.Empty<string>())
                );
        }

        SceneRepacker repacker = new StrippedSceneRepacker();

        foreach ((string scene, HashSet<string> request) in updatedToRepack)
        {
            RepackedBundleData newData = repacker.Repack(scene, request.ToList(), Path.Combine(AssetPaths.RepackedSceneBundleDir, $"repacked_{scene}.bundle"));
            _repackData[scene] = newData;
            _repackData.SerializeToFile(repackDataPath);
            SingleRepackOperationCompleted?.Invoke();
        }
    }
}
