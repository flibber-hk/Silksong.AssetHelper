using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Silksong.AssetHelper.Internal;

namespace Silksong.AssetHelper.Plugin;

internal class CatalogMetadata
{
    public string SilksongVersion { get; set; } = VersionData.SilksongVersion;

    public string PluginVersion { get; set; } = AssetHelperPlugin.Version;
}

/// <summary>
/// Data about the scene asset catalog written by AssetHelper.
/// </summary>
internal class SceneCatalogMetadata : CatalogMetadata
{
    // Could list catalogued objects but I think it's unnecessary
}

/// <summary>
/// Data about the non-scene asset catalog written by AssetHelper.
/// </summary>
internal class NonSceneCatalogMetadata : CatalogMetadata
{
    [JsonConverter(typeof(DictListConverter<(string, string), Type>))]
    public Dictionary<(string bundleName, string assetName), Type> CatalogAssets { get; set; } = [];
}
