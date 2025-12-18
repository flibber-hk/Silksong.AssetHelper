namespace Silksong.AssetHelper.LoadedAssets;

/// <summary>
/// Extension methods to control when Loadable Assets are loaded.
/// </summary>
public static class LoadableAssetExtensions
{
    /// <summary>
    /// Set this asset so it is loaded while in game, and unloaded otherwise.
    /// If currently in game, will load the asset.
    /// </summary>
    /// <param name="asset">The asset to set.</param>
    /// <param name="manualLoad">If true, will not automatically load the asset.</param>
    public static LoadableAsset<T> SetGameplayAsset<T>(
        this LoadableAsset<T> asset,
        bool manualLoad = false)
        where T : UObject
    {
        GameEvents.OnExitGame += asset.Unload;

        if (!manualLoad)
        {
            GameEvents.OnEnterGame += () => asset.Load();

            if (GameEvents.IsInGame)
            {
                asset.Load();
            }
        }

        return asset;
    }
}
