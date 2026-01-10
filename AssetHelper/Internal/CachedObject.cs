using Newtonsoft.Json;
using Silksong.AssetHelper.Core;
using System;
using System.IO;

namespace Silksong.AssetHelper.Internal;

/// <summary>
/// Object that is loaded from cache if possible, and instantiated if not.
/// 
/// The object is saved when quitting the application.
/// </summary>
internal class CachedObject<T> where T : class
{
    /// <summary>
    /// The last acceptable version for cached data. When making a change that impacts cached data,
    /// this number should be increased.
    /// </summary>
    private static readonly Version _lastAcceptablePluginVersion = Version.Parse("0.1.0");

    private CachedObject() { }
        
    [JsonProperty] public required string SilksongVersion { get; init; }
    [JsonProperty] public required string PluginVersion { get; init; }
    [JsonProperty] public required T Value { get; set; }

    private bool IsValid()
    {
        if (SilksongVersion == null || PluginVersion == null)
        {
            return false;
        }
        
        if (AssetPaths.SilksongVersion != SilksongVersion)
        {
            return false;
        }

        Version current = Version.Parse(AssetHelperPlugin.Version);
        Version fromCache = Version.Parse(PluginVersion);
        if (fromCache > current)
        {
            return false;
        }
        if (fromCache < _lastAcceptablePluginVersion)
        {
            return false;
        }
        // Automatically regenerate cache on major version change
        if (fromCache.Major != current.Major)
        {
            return false;
        }

        return true;
    }

    public static CachedObject<T> CreateSynced(string filename, Func<T> createDefault)
    {
        string filePath = Path.Combine(AssetPaths.CacheDirectory, filename);

        // Check if the object already exists
        if (JsonExtensions.TryLoadFromFile<CachedObject<T>>(filePath, out CachedObject<T>? fromCache))
        {
            if (fromCache.Value is not null && fromCache.IsValid())
            {
                AssetHelperPlugin.OnQuitApplication += () => fromCache.SerializeToFile(filePath);
                return fromCache;
            }
        }

        CachedObject<T> created = new()
        {
            SilksongVersion = AssetPaths.SilksongVersion,
            PluginVersion = AssetHelperPlugin.Version,
            Value = createDefault()
        };
        AssetHelperPlugin.OnQuitApplication += () => created.SerializeToFile(filePath);
        return created;
    }
}
