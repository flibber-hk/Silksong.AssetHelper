using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System.Collections.Generic;
using PPtrData = (int fileId, long pathId);

namespace Silksong.AssetHelper.BundleTools;

/// <summary>
/// Helper class for calculating the dependencies of assets.
/// </summary>
public class AssetDependencies(AssetsManager mgr, AssetsFileInstance afileInst, AssetDependencies.Config? settings = null)
{
    /// <summary>
    /// Class holding settings for the asset dependencies resolver.
    /// </summary>
    public class Config
    {
        /// <summary>
        /// If false, will not consider the parent of a transform as a dependency.
        /// </summary>
        public bool FollowTransformParent { get; set; } = false;
    }

    /// <summary>
    /// Record representing a collection of PPtrs associated with an asset.
    /// </summary>
    /// <param name="InternalPaths">Path IDs within the current file.</param>
    /// <param name="ExternalPaths">Pairs (file ID, path ID) external to the current file.</param>
    public record ChildPPtrs(HashSet<long> InternalPaths, HashSet<PPtrData> ExternalPaths)
    {
        /// <summary>
        /// Create a new empty <see cref="ChildPPtrs"/> instance.
        /// </summary>
        /// <returns></returns>
        public static ChildPPtrs CreateNew() => new([], []);
        
        /// <summary>
        /// Add a new PPtr to the collection.
        /// </summary>
        public bool Add(int fileId, long pathId)
        {
            if (pathId == 0) return false;
            if (fileId == 0) return InternalPaths.Add(pathId);
            return ExternalPaths.Add((fileId, pathId));
        }

        /// <inheritdoc cref="Add(int, long)" />
        public bool Add(AssetTypeValueField valueField) => Add(valueField["m_FileID"].AsInt, valueField["m_PathID"].AsLong);
    }


    private readonly AssetsManager _mgr = mgr;
    private readonly AssetsFileInstance _afileInst = afileInst;

    /// <summary>
    /// The settings for this instance.
    /// </summary>
    public Config Settings { get; init; } = settings ?? new();

    private readonly Dictionary<long, ChildPPtrs> _immediateDeps = [];
    private readonly Dictionary<long, ChildPPtrs> _bundleDeps = [];

    /// <summary>
    /// The number of cache hits for the <see cref="FindImmediateDeps(long)"/> function.
    /// </summary>
    public int Hits { get; private set; } = 0;

    /// <summary>
    /// The number of cache misses for the <see cref="FindImmediateDeps(long)"/> function.
    /// </summary>
    public int Misses { get; private set; } = 0;

    /// <summary>
    /// Find all PPtr nodes pointed to by the given asset.
    /// </summary>
    /// <param name="assetPathId">The path ID for the asset to check.</param>
    public ChildPPtrs FindImmediateDeps(long assetPathId)
    {
        if (_immediateDeps.TryGetValue(assetPathId, out ChildPPtrs cached))
        {
            Hits++;
            return cached;
        }

        Misses++;

        AssetFileInfo info = _afileInst.file.GetAssetInfo(assetPathId);

        ChildPPtrs childPPtrs = ChildPPtrs.CreateNew();

        if (!Settings.FollowTransformParent && (
            info.TypeId == (int)AssetClassID.Transform
            || info.TypeId == (int)AssetClassID.RectTransform
            ))
        {
            AssetTypeValueField tfValueField = _mgr.GetBaseField(_afileInst, info);

            childPPtrs.Add(tfValueField["m_GameObject"]);

            foreach (AssetTypeValueField childVf in tfValueField["m_Children.Array"].Children)
            {
                childPPtrs.Add(childVf);
            }
            
            return _immediateDeps[assetPathId] = childPPtrs;
        }

        AssetTypeTemplateField templateField = _mgr.GetTemplateBaseField(_afileInst, info);
        RefTypeManager refMan = _mgr.GetRefTypeManager(_afileInst);

        long assetPos = info.GetAbsoluteByteOffset(_afileInst.file);
        AssetTypeValueIterator atvIterator = new(templateField, _afileInst.file.Reader, assetPos, refMan);

        while (atvIterator.ReadNext())
        {
            string typeName = atvIterator.TempField.Type;

            if (!typeName.StartsWith("PPtr<")) continue;

            AssetTypeValueField valueField = atvIterator.ReadValueField();
            childPPtrs.Add(valueField);
        }

        return _immediateDeps[assetPathId] = childPPtrs;
    }

    /// <summary>
    /// Enumerate all pptrs that are dependencies of this asset. PPtrs within the current bundle will be followed
    /// but external pptrs will not.
    /// </summary>
    /// <param name="assetPathId">The path ID for the asset to check.</param>
    public ChildPPtrs FindBundleDeps(long assetPathId)
    {
        if (_bundleDeps.TryGetValue(assetPathId, out ChildPPtrs deps))
        {
            return deps;
        }

        HashSet<long> internalSeen = new([assetPathId]);
        HashSet<PPtrData> externalSeen = [];

        Queue<long> toProcess = new();
        toProcess.Enqueue(assetPathId);

        // Acquire the lock for the whole procedure
        lock (_afileInst.LockReader)
        {
            while (toProcess.TryDequeue(out long current))
            {
                ChildPPtrs childPptrs = FindImmediateDeps(current);

                externalSeen.UnionWith(childPptrs.ExternalPaths);

                foreach (long pathId in childPptrs.InternalPaths)
                {
                    if (internalSeen.Add(pathId))
                    {
                        toProcess.Enqueue(pathId);
                    }
                }
            }
        }

        internalSeen.Remove(assetPathId);

        return _bundleDeps[assetPathId] = new(internalSeen, externalSeen);
    }
}
