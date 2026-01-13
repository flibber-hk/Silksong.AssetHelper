using System;

namespace Silksong.AssetHelper.ManagedAssets;

/// <summary>
/// Extensions for working with instances of <see cref="AddressableAsset{T}"/>.
/// </summary>
public static class AddressableAssetExtensions
{
    /// <summary>
    /// Instantiate the asset managed by this instance.
    /// </summary>
    public static T InstantiateAsset<T>(this AddressableAsset<T> asset)
        where T : UObject
    {
        if (!asset.IsLoaded)
        {
            throw new InvalidOperationException($"The asset has not finished loading!");
        }

        return UObject.Instantiate(asset.Handle.Result);
    }

    /// <summary>
    /// Instantiate an asset in this group accessed by key.
    /// </summary>
    public static T InstantiateAsset<T>(this AddressableAssetGroup<T> group, string key)
        where T : UObject
    {
        if (!group.IsLoaded)
        {
            throw new InvalidOperationException($"The group has not finished loading!");
        }

        return UObject.Instantiate(group[key].Result);
    }
}
