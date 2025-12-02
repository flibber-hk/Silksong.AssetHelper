using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace Silksong.AssetHelper;

public static class Data
{
    private static readonly ManualLogSource Log = Logger.CreateLogSource($"AssetHelper.{nameof(Data)}");
    

    private static Dictionary<string, string>? _bundleKeys { get; set; }
    public static IReadOnlyDictionary<string, string>? BundleKeys => new ReadOnlyDictionary<string, string>(_bundleKeys);

    
    private static readonly string BundleSuffix = @"_[0-9a-fA-F]{32}\.bundle+$";
    private static readonly Regex BundleSuffixRegex = new(BundleSuffix, RegexOptions.Compiled);

    private static bool TryStrip(string key, [MaybeNullWhen(false)] out string stripped)
    {
        if (BundleSuffixRegex.IsMatch(key))
        {
            stripped = BundleSuffixRegex.Replace(key, "");
            return true;
        }
        else
        {
            stripped = key;
            return false;
        }
    }

    internal static void LoadBundleKeys()
    {
        Dictionary<string, string> keys = [];

        Stopwatch sw = Stopwatch.StartNew();
        IResourceLocator locator = Addressables.InitializeAsync().WaitForCompletion();

        foreach (string key in locator.Keys)
        {
            if (!TryStrip(key, out string? stripped)) continue;

            keys[stripped] = key;
        }

        sw.Stop();
        // This takes about 2 ms I believe so no strong need to cache
        Log.LogInfo($"Loaded asset list in {sw.ElapsedMilliseconds} ms");

        _bundleKeys = keys;
    }
}
