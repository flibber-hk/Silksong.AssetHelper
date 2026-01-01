using BepInEx;
using BepInEx.Logging;
using HutongGames.PlayMaker.Actions;
using Silksong.AssetHelper.BundleTools;
using Silksong.AssetHelper.BundleTools.Repacking;
using Silksong.AssetHelper.LoadedAssets;
using Silksong.FsmUtil;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        
        BundleDeps.Setup();

        GameEvents.Hook();
        Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");

        (new InplaceStrippingSceneRepacker()).Repack(
            AssetPaths.GetScenePath("Greymoor_05_boss"),
            ["Vampire Gnat Boss Scene/Vampire Gnat"],
            Path.Combine(AssetPaths.AssemblyFolder, "double_asset_file.bundle"));
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            StartCoroutine(SpawnMoorwing());
        }
    }

    private AssetBundle? _loadedModBundle;
    private AssetBundleGroup? dependencyGrp;

    IEnumerator SpawnMoorwing()
    {
        Stopwatch sw = Stopwatch.StartNew();

        Logger.LogInfo($"Start {AssetBundle.GetAllLoadedAssetBundles().Count()}: {sw.ElapsedMilliseconds} ms");

        // Load dependencies
        if (dependencyGrp is null)
        {
            dependencyGrp = AssetBundleGroup.CreateForScene("Greymoor_05_boss", false);
        }

        yield return dependencyGrp.LoadAsync();

        Logger.LogInfo($"Deps loaded {AssetBundle.GetAllLoadedAssetBundles().Count()}: {sw.ElapsedMilliseconds} ms");

        // Load bundle
        if (_loadedModBundle == null)
        {
            var req = AssetBundle.LoadFromFileAsync(Path.Combine(AssetPaths.AssemblyFolder, "double_asset_file.bundle"));

            yield return req;

            _loadedModBundle = req.assetBundle;
        }

        Logger.LogInfo($"Asset Names:");
        foreach (string s in _loadedModBundle.GetAllAssetNames())
        {
            Logger.LogInfo(s);
        }
        Logger.LogInfo($"MB loaded {AssetBundle.GetAllLoadedAssetBundles().Count()}: {sw.ElapsedMilliseconds} ms");

        // Spawn moorwing
        GameObject theAsset = _loadedModBundle.LoadAsset<GameObject>($"AssetHelper/Vampire Gnat Boss Scene/Vampire Gnat.prefab");
        Logger.LogInfo($"Asset loaded: {sw.ElapsedMilliseconds} ms");

        GameObject go = UObject.Instantiate(theAsset);
        FixMoorwing(go);
        go.name = $"Modded Moorwing";

        if (HeroController.instance != null)
        {
            go.transform.position = HeroController.instance.transform.position + new Vector3(0, 3, 0);
        }

        go.SetActive(true);

        Logger.LogInfo($"Spawned: {sw.ElapsedMilliseconds} ms");
    }

    void FixMoorwing(GameObject obj)
    {
        var fsm = obj.LocateMyFSM("Control");
        fsm.GetState("Dormant")!.AddMethod(_ => fsm.SendEvent("BATTLE START"));
        var zoom = fsm.GetState("Zoom Down")!;
        zoom.DisableAction(2);
        zoom.DisableAction(3);
        zoom.AddMethod(_ => fsm.SendEvent("FINISHED"));
        ((StartRoarEmitter)fsm.GetState("Quick Roar")!.actions[3]).stunHero = false;
    }

    private IEnumerator Start()
    {
        // Addressables isn't initialized until the next frame
        yield return null;

        while (true)
        {
            // Check this just in case
            bool b = AssetsData.TryLoadBundleKeys();
            if (b)
            {
                yield break;
            }

            yield return null;
        }
    }

    private void OnApplicationQuit()
    {
        GameEvents.AfterQuitApplication();
    }
}
