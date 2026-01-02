using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Silksong.AssetHelper.Util;

/// <summary>
/// Utilities relating to manipulating object paths (interpreted as hierarchy names of game objects).
/// </summary>
public static class ObjPathUtil
{
    /// <summary>
    /// Return true if self == maybePrefix, or self is of the form {maybePrefix}/...
    /// (in other words self represents a descendant of maybePrefix in the hierarchy).
    /// </summary>
    public static bool HasPrefix(this string self, string? maybePrefix)
    {
        if (maybePrefix is null)
        {
            return false;
        }

        if (!self.StartsWith(maybePrefix))
        {
            return false;
        }

        if (self == maybePrefix)
        {
            return true;
        }

        if (self[maybePrefix.Length] == '/')
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Given a collection of strings representing game object paths, return a list
    /// such that any object in the original collection with a proper ancestor
    /// also in the collection has been removed.
    /// </summary>
    public static List<string> GetHighestNodes(this ICollection<string> objPaths)
    {
        List<string> nodes = [];

        string? last = null;

        foreach (string path in objPaths.OrderBy(x => x))
        {
            if (path.HasPrefix(last))
            {
                continue;
            }

            last = path;
            nodes.Add(path);
        }

        return nodes;
    }

    /// <summary>
    /// Get the path of descendant relative to ancestor.
    /// </summary>
    /// <param name="ancestor"></param>
    /// <param name="descendant"></param>
    /// <param name="relativePath">Null if ancestor = descendant (but true is still returned in this case).</param>
    /// <returns>True if descendant is a descendant of ancestor (or they are the same).</returns>
    public static bool TryFindRelativePath(string ancestor, string descendant, out string? relativePath)
    {
        return TryFindAncestor([ancestor], descendant, out _, out relativePath);
    }

    /// <summary>
    /// Get the ancestor of the given game object within the collection of paths.
    /// 
    /// Typically, paths should be a set of highest nodes, see <see cref="GetHighestNodes(ICollection{string})" />.
    /// If this is not the case, then whichever out of the multiple acceptable ancestors is selected
    /// is undefined.
    /// </summary>
    /// <param name="paths">A list of paths of candidate ancestors.</param>
    /// <param name="objName">A path to check.</param>
    /// <param name="ancestorPath">The path representing the ancestor.</param>
    /// <param name="relativePath">The path relative to the ancestor. This will be null if the ancestor is equal to the object.</param>
    /// <returns>False if the supplied game object has no ancestor in the collection.</returns>
    public static bool TryFindAncestor(List<string> paths, string objName, [MaybeNullWhen(false)] out string ancestorPath, out string? relativePath)
    {
        foreach (string path in paths ?? Enumerable.Empty<string>())
        {
            if (objName == path)
            {
                ancestorPath = objName;
                relativePath = null;
                return true;
            }

            if (objName.HasPrefix(path))
            {
                ancestorPath = path;
                relativePath = objName[(1 + path.Length)..];
                return true;
            }
        }

        ancestorPath = null;
        relativePath = null;
        return false;
    }

    /// <summary>
    /// Given the name of a game object in the hierarchy, returns its parent's name.
    /// </summary>
    /// <param name="objName">The name of the object.</param>
    /// <param name="parent">The name of the parent.</param>
    /// <returns>True if the object is not a root game object; false otherwise.</returns>
    public static bool TryGetParent(this string objName, out string parent)
    {
        int lastSlashIndex = objName.LastIndexOf('/');

        if (lastSlashIndex == -1)
        {
            parent = string.Empty;
            return false;
        }

        parent = objName.Substring(0, lastSlashIndex);
        return true;
    }
}
