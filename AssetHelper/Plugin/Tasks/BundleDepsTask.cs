using Silksong.AssetHelper.Core;
using Silksong.AssetHelper.Internal;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Silksong.AssetHelper.Plugin.Tasks;

internal class BundleDepsTask : BaseStartupTask
{
    public override IEnumerator Run(LoadingBar loadingBar)
    {
        if (!AssetRequestAPI.AnyRequestMade)
        {
            yield break;
        }

        loadingBar.SetText(LanguageKeys.COMPUTING_BUNDLE_DEPS.GetLocalized());
        yield return null;
        AssetHelperPlugin.InstanceLogger.LogInfo("Computing bundle deps");

        Stopwatch sw = Stopwatch.StartNew();

        List<string> bundles = AddressablesData.BundleKeys!.Keys.Where(x => !x.Contains("scenes_scenes_scenes")).ToList();

        loadingBar.SetProgress(0);
        int ct = 0;

        foreach (string s in bundles)
        {
            BundleMetadata.DetermineDirectDeps(s);
            ct++;
            loadingBar.SetProgress((float)ct / (float)bundles.Count);
            
            if (ct % 5 == 0)
            {
                yield return null;
            }
        }

        sw.Stop();
        AssetHelperPlugin.InstanceLogger.LogInfo($"Time {sw.ElapsedMilliseconds} ms");
    }
}
