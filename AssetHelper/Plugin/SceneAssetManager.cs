using Silksong.AssetHelper.BundleTools;
using Silksong.AssetHelper.BundleTools.Repacking;
using Silksong.AssetHelper.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using RepackDataCollection = System.Collections.Generic.Dictionary<string, Silksong.AssetHelper.BundleTools.RepackedBundleData>;

namespace Silksong.AssetHelper.Plugin;

/// <summary>
/// Class managing the scene repacking.
/// </summary>
internal static class SceneAssetManager
{
    private static readonly Version _lastAcceptablePluginVersion = Version.Parse("0.1.0");

    private static RepackDataCollection? _repackData;

    /// <summary>
    /// Event raised each time a single scene is repacked.
    /// </summary>
    internal static event Action? SingleRepackOperationCompleted;

    /// <summary>
    /// Run a repacking procedure so that by the end, anything in toRepack which could be repacked has been.
    /// </summary>
    /// <param name="toRepack"></param>
    internal static void Run(Dictionary<string, HashSet<string>> toRepack)
    {
        string repackDataPath = Path.Combine(AssetPaths.RepackedSceneBundleDir, "repack_data.json");

        if (JsonExtensions.TryLoadFromFile(repackDataPath, out RepackDataCollection? repackData))
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

            // TODO - accept silksong version changes if the bundle hasn't changed
            Version current = Version.Parse(AssetHelperPlugin.Version);
            if (existingBundleData.SilksongVersion != AssetPaths.SilksongVersion
                || !Version.TryParse(existingBundleData.PluginVersion ?? string.Empty, out Version oldPluginVersion)
                || oldPluginVersion > current
                || oldPluginVersion < _lastAcceptablePluginVersion
                )
            {
                updatedToRepack[scene] = request;
                continue;
            }

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

        AssetHelperPlugin.InstanceLogger.LogInfo($"Repacking {updatedToRepack.Count} scenes");
        foreach ((string scene, HashSet<string> request) in updatedToRepack)
        {
            AssetHelperPlugin.InstanceLogger.LogInfo($"Repacking {request.Count} objects in scene {scene}");
            RepackedBundleData newData = repacker.Repack(scene, request.ToList(), Path.Combine(AssetPaths.RepackedSceneBundleDir, $"repacked_{scene}.bundle"));
            _repackData[scene] = newData;
            _repackData.SerializeToFile(repackDataPath);
            SingleRepackOperationCompleted?.Invoke();
        }
    }
}
