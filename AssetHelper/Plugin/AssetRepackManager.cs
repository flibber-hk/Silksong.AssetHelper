using MonoDetour.HookGen;
using Silksong.AssetHelper.Core;
using System.Collections;
using UnityEngine;

namespace Silksong.AssetHelper.Plugin;

/// <summary>
/// Class managing the scene repacking.
/// </summary>
[MonoDetourTargets(typeof(StartManager))]
internal static class AssetRepackManager
{
    internal static void Hook()
    {
        Md.StartManager.Start.Postfix(PrependStartManagerStart);
    }

    private static void PrependStartManagerStart(StartManager self, ref IEnumerator returnValue)
    {
        returnValue = WrapStartManagerStart(self, returnValue);
    }

    private static IEnumerator WrapStartManagerStart(StartManager self, IEnumerator original)
    {
        // This should already be the case, but we should check just in case it matters.
        yield return new WaitUntil(() => AddressablesData.IsAddressablesLoaded);

        yield return (new Tasks.SceneRepacking()).RepackAndCatalogScenes();
        yield return (new Tasks.NonSceneCatalog()).CreateAndLoadCatalog();

        AssetHelperPlugin.InstanceLogger.LogInfo($"{nameof(AssetHelper)} prep complete!");
        AssetRequestAPI.AfterBundleCreationComplete.Activate();

        yield return original;
        yield break;
    }
}
