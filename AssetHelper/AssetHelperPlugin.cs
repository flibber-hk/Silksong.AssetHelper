using BepInEx;
using BepInEx.Logging;
using Silksong.AssetHelper.BundleTools;
using Silksong.AssetHelper.CatalogTools;
using Silksong.AssetHelper.Plugin;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

namespace Silksong.AssetHelper;

[BepInAutoPlugin(id: "io.github.flibber-hk.assethelper")]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public partial class AssetHelperPlugin : BaseUnityPlugin
{
    public static AssetHelperPlugin Instance { get; private set; }
    #pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    internal static ManualLogSource InstanceLogger { get; private set; }

    private static readonly Dictionary<string, string> Keys = [];

    private void Awake()
    {
        Instance = this;
        InstanceLogger = this.Logger;

        AssetsToolsPatch.Init();
        BundleDeps.Setup();
        GameEvents.Hook();
        AssetRepackManager.Hook();
        Addressables.ResourceManager.ResourceProviders.Add(new ChildGameObjectProvider());

        Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
    }

    private IEnumerator Start()
    {
        AssetRequestAPI.RequestApiAvailable = false;

        // Addressables isn't initialized until the next frame
        yield return null;

        while (true)
        {
            // Check this just in case
            bool b = AddressablesData.TryLoadBundleKeys();
            if (b)
            {
                break;
            }

            yield return null;
        }
    }

    private void OnApplicationQuit()
    {
        GameEvents.AfterQuitApplication();
    }
}
