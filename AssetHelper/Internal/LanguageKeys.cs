using DataDrivenConstants.Marker;
using TeamCherry.Localization;

namespace Silksong.AssetHelper.Internal;

[JsonData("$.*~", "**/languages/en.json")]
internal static partial class LanguageKeys
{
    public static string GetLocalized(this string key) => Language.Get(key, $"Mods.{AssetHelperPlugin.Id}");
}
