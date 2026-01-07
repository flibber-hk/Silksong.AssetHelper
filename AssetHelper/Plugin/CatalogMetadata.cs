namespace Silksong.AssetHelper.Plugin;

/// <summary>
/// Data about a catalog written by AssetHelper.
/// </summary>
internal class CatalogMetadata
{
    public string SilksongVersion { get; set; } = AssetPaths.SilksongVersion;

    public string PluginVersion { get; set; } = AssetHelperPlugin.Version;
}
