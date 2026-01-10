using System.Reflection;

namespace Silksong.AssetHelper.Internal;

internal static class VersionData
{
    /// <summary>
    /// The Silksong version. This is calculated using reflection to avoid it being inlined.
    /// </summary>
    public static string SilksongVersion
    {
        get
        {
            _silksongVersion ??= GetSilksongVersion();
            return _silksongVersion;
        }
    }

    private static string? _silksongVersion;

    private static string GetSilksongVersion() => typeof(Constants)
        .GetField(nameof(Constants.GAME_VERSION), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
        ?.GetRawConstantValue()
        as string
        ?? "UNKNOWN";
}
