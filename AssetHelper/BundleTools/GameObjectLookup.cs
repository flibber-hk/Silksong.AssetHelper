using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System.Collections;
using System.Collections.Generic;

namespace Silksong.AssetHelper.BundleTools;

/// <summary>
/// Class capable of looking up the path ID of a game object, its transform and its name based on
/// one of the three.
/// </summary>
public class GameObjectLookup : IEnumerable<GameObjectLookup.GameObjectInfo>
{
    /// <summary>
    /// Record encapsulating information about a game object.
    /// </summary>
    /// <param name="GameObjectPathId">The path ID to the game object.</param>
    /// <param name="TransformPathId">The path ID to the transform.</param>
    /// <param name="GameObjectPath">The path to the game object in the form root/.../grandparent/parent/object.</param>
    public record GameObjectInfo(long GameObjectPathId, long TransformPathId, string GameObjectPath);

    private readonly Dictionary<string, GameObjectInfo> _fromName;
    private readonly Dictionary<long, GameObjectInfo> _fromGameObject;
    private readonly Dictionary<long, GameObjectInfo> _fromTransform;

    private GameObjectLookup(Dictionary<string, GameObjectInfo> fromName, Dictionary<long, GameObjectInfo> fromGameObject, Dictionary<long, GameObjectInfo> fromTransform)
    {
        _fromName = fromName;
        _fromGameObject = fromGameObject;
        _fromTransform = fromTransform;
    }

    /// <summary>
    /// Create a GameObjectLookup from a collection of <see cref="GameObjectInfo"/> records.
    /// </summary>
    /// <param name="infos"></param>
    /// <returns></returns>
    public static GameObjectLookup CreateFromInfos(IEnumerable<GameObjectInfo> infos)
    {
        Dictionary<string, GameObjectInfo> fromName = [];
        Dictionary<long, GameObjectInfo> fromGameObject = [];
        Dictionary<long, GameObjectInfo> fromTransform = [];

        foreach (GameObjectInfo info in infos)
        {
            fromName[info.GameObjectPath] = info;
            fromGameObject[info.GameObjectPathId] = info;
            fromTransform[info.TransformPathId] = info;
        }

        return new(fromName, fromGameObject, fromTransform);
    }

    /// <summary>
    /// Create a GameObjectLookup from an assets file instance.
    /// </summary>
    public static GameObjectLookup CreateFromFile(AssetsManager mgr, AssetsFileInstance afileInst)
    {
        Dictionary<long, GameObjectInfo> fromTransformLookup = [];

        GameObjectInfo DoAdd(long tPathId)
        {
            if (fromTransformLookup.TryGetValue(tPathId, out GameObjectInfo info))
            {
                return info;
            }

            AssetTypeValueField tValueField = mgr.GetBaseField(afileInst, tPathId);
            long goPathId = tValueField["m_GameObject.m_PathID"].AsLong;
            AssetTypeValueField goValueField = mgr.GetBaseField(afileInst, goPathId);
            string goName = goValueField["m_Name"].AsString;
            long parentTransformPathId = tValueField["m_Father.PathID"].AsLong;

            if (parentTransformPathId == 0)
            {
                GameObjectInfo newInfo = new(goPathId, tPathId, goName);
                fromTransformLookup[tPathId] = newInfo;
                return newInfo;
            }

            GameObjectInfo parentInfo = DoAdd(parentTransformPathId);
            GameObjectInfo childInfo = new(goPathId, tPathId, $"{parentInfo.GameObjectPath}/{goName}");
            fromTransformLookup[tPathId] = childInfo;
            return childInfo;
        }

        foreach (AssetFileInfo transform in afileInst.file.GetAllTransforms())
        {
            DoAdd(transform.PathId);
        }

        return CreateFromInfos(fromTransformLookup.Values);
    }

    /// <summary>
    /// Get the <see cref="GameObjectInfo"/> corresponding to the given transform path ID.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Raised if the key was not found.</exception>
    public GameObjectInfo LookupTransform(long pathId) => _fromTransform.TryGetValue(pathId, out GameObjectInfo info)
        ? info 
        : throw new KeyNotFoundException($"Did not find transform key {pathId}");

    /// <summary>
    /// Get the <see cref="GameObjectInfo"/> corresponding to the given game object path ID.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Raised if the key was not found.</exception>
    public GameObjectInfo LookupGameObject(long pathId) => _fromGameObject.TryGetValue(pathId, out GameObjectInfo info)
        ? info
        : throw new KeyNotFoundException($"Did not find game object key {pathId}");


    /// <summary>
    /// Get the <see cref="GameObjectInfo"/> corresponding to the given game object name (given in the hierarchy).
    /// </summary>
    /// <exception cref="KeyNotFoundException">Raised if the key was not found.</exception>
    public GameObjectInfo LookupName(string name) => _fromName.TryGetValue(name, out GameObjectInfo info)
        ? info
        : throw new KeyNotFoundException($"Did not find name {name}");

    /// <summary>
    /// Get an enumerator over the GameObjectInfos covered by this instance.
    /// </summary>
    public IEnumerator<GameObjectInfo> GetEnumerator()
    {
        return _fromName.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
