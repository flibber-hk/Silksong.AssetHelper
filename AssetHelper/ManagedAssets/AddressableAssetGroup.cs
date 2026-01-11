using Silksong.AssetHelper.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Silksong.AssetHelper.ManagedAssets;

/// <summary>
/// Class representing a collection of Addressable assets of the same type that are
/// loaded together.
/// </summary>
public class AddressableAssetGroup<T>
{
    private Dictionary<string, string> _keyLookup;

    /// <summary>
    /// Construct an Addressable asset group from a mapping {name -> key}.
    /// </summary>
    /// <param name="keyLookup">A mapping name -> key.
    /// The name should be a string used to access the individual asset; names should be unique but
    /// their values do not matter.
    /// The key should be an Addressables key.</param>
    public AddressableAssetGroup(Dictionary<string, string> keyLookup)
    {
        _keyLookup = keyLookup;
    }

    /// <summary>
    /// Request keys for the given scene and/or non-scene assets, and create an <see cref="AddressableAssetGroup{T}"></see>
    /// managing them.
    /// </summary>
    /// <param name="sceneAssets">A mapping (key) -> </param>
    /// <param name="nonSceneAssets"></param>
    /// <exception cref="InvalidOperationException">Exception thrown if the request is made after plugins have finished Awake-ing.</exception>
    public static AddressableAssetGroup<T> RequestAndCreate(
        List<(string name, string sceneName, string objPath)>? sceneAssets = null,
        List<(string name, string bundleName, string assetName)>? nonSceneAssets = null)
    {
        if (!AssetRequestAPI.RequestApiAvailable)
        {
            throw new InvalidOperationException("Asset requests should be made during or before a plugin's Awake method!");
        }

        Dictionary<string, string> keyLookup = [];

        if (sceneAssets != null)
        {
            if (typeof(T) != typeof(GameObject))
            {
                AssetHelperPlugin.InstanceLogger.LogWarning($"{nameof(AddressableAssetGroup<>)} instances for scene assets should have GameObject as the type argument!");
            }

            foreach ((string name, string sceneName, string objPath) in sceneAssets)
            {
                AssetRequestAPI.RequestSceneAsset(sceneName, objPath);
                keyLookup.Add(name, CatalogKeys.GetKeyForSceneAsset(sceneName, objPath));
            }
        }

        if (nonSceneAssets != null)
        {
            foreach ((string name, string bundleName, string assetName) in nonSceneAssets)
            {
                AssetRequestAPI.RequestNonSceneAsset<T>(bundleName, assetName);
                keyLookup.Add(name, CatalogKeys.GetKeyForNonSceneAsset(assetName));
            }
        }

        return new(keyLookup);
    }

    private Dictionary<string, AsyncOperationHandle<T>>? _handles;

    /// <summary>
    /// Get a <see cref="CustomYieldInstruction"/> that can be used to wait for the assets to finish loading.
    /// 
    /// Calling `yield return group.GetYieldInstruction()` in an IEnumerator will cause Unity to pause the coroutine
    /// until all assets are loaded.
    /// </summary>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException">If this function is called before loading the assets.</exception>
    public CustomYieldInstruction GetYieldInstruction()
    {
        if (_handles == null)
        {
            throw new InvalidOperationException($"This {nameof(AddressableAssetGroup<>)} must be loaded before awaiting!");
        }

        return new WaitUntil(() => this.IsLoaded);
    }

    /// <summary>
    /// Load the underlying asset. This operation is idempotent.
    /// 
    /// This should be called prior to using the asset.
    /// </summary>
    /// <returns>The output of <see cref="GetYieldInstruction"/>.</returns>
    public CustomYieldInstruction Load()
    {
        if (_handles != null)
        {
            return GetYieldInstruction();
        }

        _handles = [];
        foreach ((string name, string key) in _keyLookup)
        {
            _handles[name] = Addressables.LoadAssetAsync<T>(key);
        }

        return GetYieldInstruction();
    }

    /// <summary>
    /// Access a loaded asset by name.
    /// </summary>
    /// <param name="name">The name as provided when creating this instance.</param>
    public AsyncOperationHandle<T> this[string name]
    {
        get
        {
            if (_handles == null)
            {
                throw new InvalidOperationException("Handles can not be accessed until this instance has started loading");
            }

            return _handles![name];
        }
    }

    /// <summary>
    /// Unload the underlying assets. This operation is idempotent.
    /// 
    /// This should not be called if the asset is still in use.
    /// </summary>
    public void Unload()
    {
        if (_handles != null)
        {
            foreach (var handle in _handles.Values)
            {
                Addressables.Release(handle);
            }
            
            _handles = null;
        }
    }

    /// <summary>
    /// Whether or not the assets have finished loading.
    /// </summary>
    public bool IsLoaded => HasBeenLoaded && _handles!.Values.All(x => x.IsDone);

    /// <summary>
    /// Whether or not the asset load request has been made.
    /// </summary>
    public bool HasBeenLoaded => _handles != null;
}
