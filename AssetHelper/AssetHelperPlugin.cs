using BepInEx;
using BepInEx.Logging;
using Silksong.AssetHelper.BundleTools;
using Silksong.AssetHelper.CatalogTools;
using Silksong.AssetHelper.Internal;
using Silksong.AssetHelper.Plugin;
using System;
using System.Collections;
using UnityEngine.AddressableAssets;

namespace Silksong.AssetHelper;

[BepInAutoPlugin(id: "io.github.flibber-hk.assethelper")]
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
public partial class AssetHelperPlugin : BaseUnityPlugin
{
    public static AssetHelperPlugin Instance { get; private set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

    /// <summary>
    /// Event raised when quitting the application.
    /// </summary>
    public static event Action? OnQuitApplication;

    internal static ManualLogSource InstanceLogger { get; private set; }

    private void Awake()
    {
        Instance = this;
        InstanceLogger = this.Logger;

        InitLibLogging();
        AssetsToolsPatch.Init();
        BundleDeps.Setup();
        AssetRepackManager.Hook();
        Addressables.ResourceManager.ResourceProviders.Add(new ChildGameObjectProvider());

        Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
    }

    private static void InitLibLogging()
    {
        ManualLogSource ahlLog = BepInEx.Logging.Logger.CreateLogSource("AssetHelper.Lib");
        AssetHelperLib.Logging.OnLog += ahlLog.LogInfo;
        AssetHelperLib.Logging.OnLogWarning += ahlLog.LogWarning;
        AssetHelperLib.Logging.OnLogError += ahlLog.LogError;
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
        foreach (Action a in OnQuitApplication?.GetInvocationList() ?? Array.Empty<Action>())
        {
            ActionUtil.SafeInvoke(a);
        }
    }
}
