# Quickstart

The easiest way to use AssetHelper to load assets is as follows.

## Request assets during your plugin's Awake method

Any asset you want easy access to should be explicitly requested.
Methods on the @"Silksong.AssetHelper.Plugin.AssetRequestAPI"
class can be used to request assets.

## Prepare assets you want to load

Use the @"Silksong.AssetHelper.Managed.AddressableAsset`1.FromSceneAsset"
and @"Silksong.AssetHelper.Managed.AddressableAsset`1.FromNonSceneAsset"
functions to create wrappers around any assets you want to access. These
will have to be assets you have already requested.

## Load up assets

You can call the @"Silksong.AssetHelper.Managed.AddressableAsset`1.Load"
method to load up the assets. The earliest this can be done is in
a callback to @"Silksong.AssetHelper.Plugin.AssetRequestAPI.InvokeAfterBundleCreation".

## Instantiate your assets

The assets can be instantiated at any time from the AddressableAsset instance,
for example by using
@"Silksong.AssetHelper.Managed.AddressableAssetExtensions.InstantiateAsset`1".
