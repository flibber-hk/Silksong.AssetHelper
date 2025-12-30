using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Silksong.AssetHelper.CatalogTools
{
    internal static class CatalogEntry
    {

        /// <summary>
        /// Creates a catalog entry for repacked scene bundles
        /// </summary>
        /// <param name="key">The primary key of the bundle.</param>
        /// <param name="bundlePath">The fully qualified path to the bundle on the filesystem</param>
        /// <param name="internalBundleName">The name of the AssetBundle asset in the bundle</param>
        /// <param name="dependencyKeys">List of all the primary keys of the bundle dependencies. Note: All keys found in the dependencies must have their corresponding entry in the catalog</param>
        public static ContentCatalogDataEntry createBundleEntry(string key, string bundlePath, string internalBundleName, List<string> dependencyKeys)
        {

            AssetBundleRequestOptions requestOptions = new AssetBundleRequestOptions();
            requestOptions.AssetLoadMode = AssetLoadMode.RequestedAssetAndDependencies;
            requestOptions.BundleName = internalBundleName;
            requestOptions.ChunkedTransfer = false;
            requestOptions.RetryCount = 0;
            requestOptions.RedirectLimit = 32;
            requestOptions.Timeout = 0;
            requestOptions.BundleSize = 0;
            requestOptions.ClearOtherCachedVersionsWhenLoaded = false;
            requestOptions.Crc = 0;
            requestOptions.UseCrcForCachedBundle = true;
            requestOptions.BundleSize = (new FileInfo(bundlePath)).Length;

            ContentCatalogDataEntry bundleEntry = new(
                typeof(IAssetBundleResource),
                bundlePath,
                "UnityEngine.ResourceManagement.ResourceProviders.AssetBundleProvider",
                new object[] { key },
                dependencyKeys,
                requestOptions

            );

            return bundleEntry;
        }

        /// <summary>
        /// Creates a catalog entry for bundled assets.
        /// </summary>
        /// <param name="assetPath">Addressable path of the asset.</param>
        /// <param name="assetType">Unity type of the asset. Eg: GameObject</param>
        /// <param name="ownerBundleKey">Primary key of the bundle the asset will be defined in.</param>
        public static ContentCatalogDataEntry createAssetEntry(string assetPath, Type assetType, string ownerBundleKey)
        {
            return new ContentCatalogDataEntry(
                assetType,
                assetPath,
                "UnityEngine.ResourceManagement.ResourceProviders.BundledAssetProvider",
                new object[] { assetPath },
                new object[] { ownerBundleKey },
                null
            );
        }

        public static ContentCatalogDataEntry createEntryFromLocation(IResourceLocation location)
        {
            return new ContentCatalogDataEntry(
                location.ResourceType,
                location.InternalId,
                location.ProviderId,
                new object[] { "AssetHelper/" + location.PrimaryKey },
                null,
                location.Data
            );
        }

    }
}
