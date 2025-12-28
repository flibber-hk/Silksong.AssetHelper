using Silksong.AssetHelper.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Silksong.AssetHelper.LoadedAssets;

/// <summary>
/// An asset loadable with an Addressables key.
/// </summary>
/// <typeparam name="T">The type of the asset to load.</typeparam>
public class AddressableAsset<T> : ILoadableAsset<T> where T : UObject
{
    private object _key;

    /// <summary>
    /// Construct an Addressable asset for the given key.
    /// </summary>
    public AddressableAsset(object key)
    {
        _key = key; 
    }

    /// <summary>
    /// The handle to the load operation used to load this asset.
    /// </summary>
    public AsyncOperationHandle<T>? LoadOpHandle { get; private set; }

    /// <inheritdoc />
    public T? Asset
    {
        get
        {
            if (!Loaded) return null;
            return LoadOpHandle!.Value.Result;
        }
    }

    /// <summary>
    /// Event invoked when this asset is loaded.
    /// 
    /// This event is only invoked if the asset is actually loaded; if the
    /// asset was already loaded, the event will not be raised.
    /// </summary>
    public event Action<BundleAsset<T>>? OnLoaded;

    /// <inheritdoc />
    public bool Loaded => LoadOpHandle.HasValue && LoadOpHandle.Value.IsDone;

    private readonly List<Action<AddressableAsset<T>>> _toInvokeWhenLoaded = [];

    /// <summary>
    /// Execute the supplied action when this asset is loaded.
    /// 
    /// If it is already loaded, execute the action immediately.
    /// </summary>
    public void ExecuteWhenLoaded(Action<AddressableAsset<T>> toInvoke)
    {
        if (Loaded)
        {
            ActionUtil.SafeInvoke(toInvoke, this);
            return;
        }
        _toInvokeWhenLoaded.Add(toInvoke);
    }

    /// <inheritdoc />
    void ILoadableAsset<T>.ExecuteWhenLoaded(Action<ILoadableAsset<T>> toInvoke) => ExecuteWhenLoaded(toInvoke);

    private void OnLoadedCallback()
    {
        if (OnLoaded != null)
        {
            foreach (Action<AddressableAsset<T>> toInvoke in OnLoaded.GetInvocationList())
            {
                ActionUtil.SafeInvoke(toInvoke, this);
            }
        }

        foreach (Action<AddressableAsset<T>> toInvoke in _toInvokeWhenLoaded)
        {
            ActionUtil.SafeInvoke(toInvoke, this);
        }
        _toInvokeWhenLoaded.Clear();
    }

    /// <inheritdoc />
    public void Load()
    {
        LoadOpHandle = Addressables.LoadAssetAsync<T>(_key);
        LoadOpHandle.Value.Completed += _ => OnLoadedCallback();
    }

    /// <inheritdoc />
    public IEnumerator LoadAsync()
    {
        LoadOpHandle = Addressables.LoadAssetAsync<T>(_key);
        yield return LoadOpHandle;
        OnLoadedCallback();
    }

    /// <inheritdoc />
    public void LoadImmediate()
    {
        LoadOpHandle = Addressables.LoadAssetAsync<T>(_key);
        LoadOpHandle.Value.WaitForCompletion();
        OnLoadedCallback();
    }

    /// <inheritdoc />
    public void Unload()
    {
        if (LoadOpHandle.HasValue)
        {
            Addressables.Release(LoadOpHandle);
            LoadOpHandle = null;
        }
    }
}
