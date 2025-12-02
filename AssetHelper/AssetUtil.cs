using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using Logger = BepInEx.Logging.Logger;
using UObject = UnityEngine.Object;

namespace Silksong.AssetHelper;

public static class AssetUtil
{
    private static readonly ManualLogSource Log = Logger.CreateLogSource(nameof(AssetUtil));

    public static T? LoadAsset<T>(string bundleName, string name, List<string>? extraDependencies = null)
    where T : UObject
    {
        if (Data.BundleKeys is null)
        {
            Log.LogWarning($"Cannot load asset {name} from {bundleName}: too early");
            return default;
        }

        extraDependencies ??= [];

        List<AsyncOperationHandle<IAssetBundleResource>> loadedDependencies = [];
        foreach (string extraBundle in extraDependencies)
        {
            if (!Data.BundleKeys.TryGetValue(extraBundle, out string extraBundleKey))
            {
                Log.LogWarning($"Skipping extra dependency {extraBundle}: Key not found");
                continue;
            }

            loadedDependencies.Add(
                Addressables.LoadAssetAsync<IAssetBundleResource>(extraBundleKey)
                );
        }

        AsyncOperationHandle<IAssetBundleResource> bundleLoadOp = Addressables.LoadAssetAsync<IAssetBundleResource>(
            Data.BundleKeys[bundleName]);

        foreach (AsyncOperationHandle<IAssetBundleResource> op in loadedDependencies)
        {
            op.WaitForCompletion();
        }

        IAssetBundleResource resource = bundleLoadOp.WaitForCompletion();

        AssetBundle bundle = resource.GetAssetBundle();

        string objName = bundle.GetAllAssetNames().FirstOrDefault(x => x.Contains(name));
        if (objName == null)
        {
            Log.LogError($"Could not find name {name} in bundle {bundleName}");
            Log.LogError("Available names:\n" + string.Join(", ", bundle.GetAllAssetNames().ToArray()));

            return default;
        }

        T loaded = bundle.LoadAsset<T>(objName);
        return loaded;
    }
}
