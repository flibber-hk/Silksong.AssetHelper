using Silksong.AssetHelper.Util;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Silksong.AssetHelper.BundleTools;

/// <summary>
/// Data about a repacked bundle.
/// </summary>
public class RepackedBundleData
{
    /// <summary>
    /// String indicating how repacking was done.
    /// </summary>
    public string? RepackStrategy { get; set; }

    /// <summary>
    /// The Silksong version used to create this bundle.
    /// </summary>
    public string? SilksongVersion { get; set; }

    /// <summary>
    /// The Asset Helper version used to create this bundle.
    /// </summary>
    public string? PluginVersion { get; set; }
    
    /// <summary>
    /// Construct an instance of this class with default version parameters.
    /// </summary>
    public RepackedBundleData()
    {
        SilksongVersion = AssetPaths.SilksongVersion;
        PluginVersion = AssetHelperPlugin.Version;
    }

    /// <summary>
    /// The name of the internal asset bundle.
    /// </summary>
    public string? BundleName { get; set; }

    /// <summary>
    /// The CAB name of the bundle file.
    /// </summary>
    public string? CabName { get; set; }

    /// <summary>
    /// A lookup {name in container -> original game object path} for game object assets in the container.
    /// </summary>
    public Dictionary<string, string>? GameObjectAssets { get; set; }

    /// <summary>
    /// Assets which were requested but failed to be repacked.
    /// </summary>
    public List<string>? NonRepackedAssets { get; set; }
}

/// <summary>
/// Extension methods for the <see cref="RepackedBundleData"/> class.
/// </summary>
public static class RepackedBundleDataExtensions
{
    /// <summary>
    /// Return true if this instance is capable of loading the provided game object, assuming it is available in the bundle.
    /// 
    /// If true, the asset can be loaded with:
    /// UObject.Instantiate(bundle.LoadAsset&lt;GameObject&gt;(assetPath).transform.Find(relativePath).gameObject);
    /// </summary>
    /// <param name="data">The data instance.</param>
    /// <param name="objName">The hierarchy name of the object (relative to the root).</param>
    /// <param name="assetPath">The path of the ancestor within the bundle that has a container path.</param>
    /// <param name="relativePath">The path of the gameobject relative to the ancestor, or null if they are the same.</param>
    /// <returns></returns>
    public static bool CanLoad(this RepackedBundleData data, string objName, [MaybeNullWhen(false)] out string assetPath, out string? relativePath)
    {
        if (data.GameObjectAssets == null)
        {
            assetPath = default;
            relativePath = default;
            return false;
        }

        foreach ((string containerName, string goPath) in data.GameObjectAssets)
        {
            if (ObjPathUtil.TryFindRelativePath(goPath, objName, out relativePath))
            {
                assetPath = containerName;
                return true;
            }
        }

        assetPath = default;
        relativePath = default;
        return false;
    }

    /// <summary>
    /// Return true if the repacking operation tried to create a bundle capable of loading the given object.
    /// </summary>
    public static bool TriedToRepack(this RepackedBundleData data, string objName)
    {
        if (data.NonRepackedAssets != null)
        {
            if (ObjPathUtil.TryFindAncestor(data.NonRepackedAssets, objName, out _, out _))
            {
                return true;
            }
        }
        
        return data.CanLoad(objName, out _, out _);
    }
}
