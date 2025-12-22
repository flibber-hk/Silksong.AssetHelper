namespace Silksong.AssetHelper.LoadedAssets;

/// <summary>
/// Extension methods to control when Loadable Assets are loaded.
/// </summary>
public static class LoadableAssetExtensions
{
    /// <summary>
    /// Ensure that this asset is always loaded while in-game.
    /// 
    /// If currently in game, will load the asset.
    /// </summary>
    /// <param name="asset">The asset to set.</param>
    public static LoadableAsset<T> SetGameplayAsset<T>(
        this LoadableAsset<T> asset)
        where T : UObject
    {
        GameEvents.OnEnterGame += asset.DoLoad;

        if (GameEvents.IsInGame)
        {
            asset.Load();
        }

        return asset;
    }
}
