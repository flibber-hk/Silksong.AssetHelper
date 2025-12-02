using BepInEx;
using System.Collections;
using System.Collections.Generic;

namespace Silksong.AssetHelper;

[BepInAutoPlugin(id: "io.github.flibber-hk.assethelper")]
public partial class AssetHelperPlugin : BaseUnityPlugin
{
    private static readonly Dictionary<string, string> Keys = [];
    
    public static AssetHelperPlugin Instance { get;private set; }

    private void Awake()
    {
        Instance = this;
        Logger.LogInfo($"Plugin {Name} ({Id}) has loaded!");
    }



    private IEnumerator Start()
    {
        // For some reason we need to wait to load the asset list
        yield return null;
        yield return null;

        Data.LoadBundleKeys();
    }
}
