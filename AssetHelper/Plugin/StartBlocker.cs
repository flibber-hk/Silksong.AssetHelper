using MonoDetour.HookGen;
using System.Collections;

namespace Silksong.AssetHelper.Plugin;

/// <summary>
/// Class to execute code before entering the main menu.
/// </summary>
[MonoDetourTargets(typeof(StartManager))]
internal static class StartBlocker
{
    internal static void Hook()
    {
        Md.StartManager.Start.Postfix(PrependStartManagerStart);
    }

    private static void PrependStartManagerStart(StartManager self, ref IEnumerator returnValue)
    {
        // TODO - this func should be a no-op if there's no repacking to do
        returnValue = WrapStartManagerStart(self, returnValue);
    }

    private static IEnumerator WrapStartManagerStart(StartManager self, IEnumerator original)
    {
        // TODO - turn this into a coroutine
        // TODO - Add progress bar
        SceneAssetManager.Run(SceneAssetAPI.sceneAssetRequest);
        
        yield return original;
    }
}
