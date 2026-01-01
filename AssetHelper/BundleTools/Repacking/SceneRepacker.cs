using AssetsTools.NET.Extra;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;

namespace Silksong.AssetHelper.BundleTools.Repacking;

/// <summary>
/// Base class for repacking strategies.
/// </summary>
public abstract class SceneRepacker
{
    /// <summary>
    /// Determine sensible cab and bundle names for the given bundle.
    /// 
    /// These don't matter, but these ones look like the ones made by unity.
    /// </summary>
    /// <param name="sceneBundlePath"></param>
    /// <param name="objectNames"></param>
    /// <param name="outBundlePath"></param>
    /// <param name="cabName"></param>
    /// <param name="bundleName"></param>
    protected static void GetDefaultBundleNames(
        string? sceneBundlePath,
        List<string>? objectNames,
        string outBundlePath,
        out string cabName,
        out string bundleName)
    {
        const string salt = "AssetHelperSalt\n";

        using SHA256 sha256 = SHA256.Create();

        StringBuilder inputSb = new();
        inputSb.AppendLine(salt);
        inputSb.AppendLine(sceneBundlePath ?? "NULL SCENE BUNDLE PATH");

        foreach (string name in objectNames ?? ["NULL OBJECT NAMES"])
        {
            inputSb.AppendLine($"\n{name}");
        }

        inputSb.AppendLine(outBundlePath);

        string saltedInput = inputSb.ToString();

        byte[] inputBytes = Encoding.UTF8.GetBytes(saltedInput);
        byte[] hashBytes = sha256.ComputeHash(inputBytes);

        StringBuilder sb = new(64);
        foreach (byte b in hashBytes)
        {
            sb.Append(b.ToString("x2"));
        }

        string fullHash = sb.ToString();

        cabName = $"CAB-{fullHash.Substring(0, 32)}";
        bundleName = $"{fullHash.Substring(32, 32)}.bundle";
    }

    /// <summary>
    /// Create a bundle that can be used to spawn objects from the provided scene bundle.
    /// </summary>
    /// <param name="sceneBundlePath">A path to the scene bundle.</param>
    /// <param name="objectNames">A list of game objects to include.
    /// Any game object in this list should have an ancestor which is accessible via <see cref="UnityEngine.AssetBundle.LoadAsset(string)"/>,
    /// provided the supplied game object exists in the bundle.</param>
    /// <param name="outBundlePath">File location for the repacked bundle.</param>
    public abstract RepackedBundleData Repack(
        string sceneBundlePath,
        List<string> objectNames,
        string outBundlePath
        );
}
