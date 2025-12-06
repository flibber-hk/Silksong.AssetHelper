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

    public event Action<GameplayAsset<T>>? OnAssetLoaded;

    internal GameplayAsset(string mainBundleKey, string assetName, List<string> dependencies)
    {
        _mainBundleKey = mainBundleKey;
        _assetName = assetName;
        _dependencies = dependencies;

        GameEvents.OnEnterGame += LoadAsset;
        GameEvents.OnExitGame += UnloadAsset;

        if (GameEvents.IsInGame)
        {
            LoadAsset();
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
