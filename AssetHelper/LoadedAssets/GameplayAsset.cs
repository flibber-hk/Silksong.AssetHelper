using System;
using System.Collections.Generic;
using UnityEngine;

namespace Silksong.AssetHelper.LoadedAssets;

/// <summary>
/// Class representing a <see cref="LoadedAsset{T}"/> which is available
/// while in-game but not in the main menu.
/// </summary>
public class GameplayAsset<T> : IDisposable where T : UObject
{
    private readonly string _mainBundleKey;
    private readonly string _assetName;
    private readonly List<string> _dependencies;

    /// <summary>
    /// Event raised when the asset is loaded, after entering game.
    /// </summary>
    public event Action<GameplayAsset<T>>? OnAssetLoaded;

    /// <summary>
    /// Create an instance of the GameplayAsset.
    /// </summary>
    /// <param name="mainBundleKey">The main bundle key.</param>
    /// <param name="assetName">The name of the asset.</param>
    /// <param name="dependencies">Optional list of dependencies.</param>
    public GameplayAsset(string mainBundleKey, string assetName, List<string>? dependencies = null)
    {
        _mainBundleKey = mainBundleKey;
        _assetName = assetName;
        _dependencies = dependencies ?? [];

        GameEvents.OnEnterGame += LoadAsset;
        GameEvents.OnExitGame += UnloadAsset;

        if (GameEvents.IsInGame)
        {
            LoadAsset();
        }
    }

    private List<Action> _queuedActions = new();

    /// <summary>
    /// Execute the given action immediately, or as soon as the asset is loaded
    /// if it is currently unloaded.
    /// 
    /// Actions supplied to this function will be executed no more than once.
    /// </summary>
    public void ExecuteWhenLoaded(Action a)
    {
        if (_storedAssetWrapper != null)
        {
            Util.ActionUtil.SafeInvoke(a);
        }
        else
        {
            _queuedActions.Add(a);
        }
    }

    private void LoadAsset()
    {
        if (_disposed)
        {
            return;
        }
        AssetLoadUtil.LoadAsset<T>(
            _mainBundleKey,
            _assetName,
            asset => 
            {
                _storedAssetWrapper = asset;
                OnAssetLoaded?.Invoke(this);
                foreach (Action a in _queuedActions)
                {
                    Util.ActionUtil.SafeInvoke(a);
                }
                _queuedActions.Clear();
            },
            _dependencies
            );
    }

    private void UnloadAsset()
    {
        _storedAssetWrapper?.Dispose();
        _storedAssetWrapper = null;
    }

    private LoadedAsset<T>? _storedAssetWrapper;

    /// <inheritdoc cref="LoadedAsset{T}.Asset" />
    public T? Asset => _storedAssetWrapper?.Asset;

    /// <inheritdoc cref="LoadedAsset{T}.Bundle" />
    public AssetBundle? Bundle => _storedAssetWrapper?.Bundle;

    private bool _disposed;

    /// <summary>
    /// Virtual dispose method following the standard Dispose pattern.
    /// </summary>
    protected virtual void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing)
        {
            UnloadAsset();
            GameEvents.OnEnterGame -= LoadAsset;
            GameEvents.OnExitGame -= UnloadAsset;
        }

        _disposed = true;
    }

    /// <summary>
    /// Release the Asset Bundles used to load this asset.
    /// 
    /// This method should only be called if this instance is no longer needed.
    /// Asset release will be handled automatically when transitioning from gameplay to main menu;
    /// Dispose should not be called in this case.
    /// </summary>
    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
