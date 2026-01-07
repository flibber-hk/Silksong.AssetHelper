using AssetsTools.NET;
using AssetsTools.NET.Extra;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Buffers;
using System.IO;

namespace Silksong.AssetHelper;

internal static class AssetsToolsPatch
{
    private static ILHook? _atHook;
    private static Hook? _atChook;


    public static void Init()
    {
        // TODO - remove this when assetstools.net gets updated
        _atHook = new ILHook(typeof(AssetTypeValueIterator).GetMethod(nameof(AssetTypeValueIterator.ReadNext)), PatchATVI);
        _atChook = new Hook(typeof(Net35Polyfill).GetMethod(nameof(Net35Polyfill.CopyToCompat)), PatchC2C);
    }

    /// <summary>
    /// Use array pooling to plug a memory leak; this memory leak doesn't happen in modern versions of
    /// .NET with a better garbage collector.
    /// </summary>
    private static void PatchC2C(Action<Stream, Stream, long, int> orig, Stream input, Stream output, long bytes, int bufferSize)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
        int read;

        // set to largest value so we always go over buffer (hopefully)
        if (bytes == -1)
            bytes = long.MaxValue;

        // bufferSize will always be an int so if bytes is larger, it's also under the size of an int
        while (bytes > 0 && (read = input.Read(buffer, 0, (int)Math.Min(buffer.Length, bytes))) > 0)
        {
            output.Write(buffer, 0, read);
            bytes -= read;
        }
        ArrayPool<byte>.Shared.Return(buffer);
    }

    /// <summary>
    /// Fixes a bug with AssetTypeValueIterator where it moves 4 bytes forward when reading a double rather than 8
    /// </summary>
    private static void PatchATVI(ILContext il)
    {
        ILCursor cursor = new(il);

        if (!cursor.TryGotoNext(MoveType.After,
            i => i.MatchCallvirt<AssetTypeTemplateField>($"get_{nameof(AssetTypeTemplateField.ValueType)}"),
            i => i.MatchStloc(out _),
            i => i.MatchLdloc(out _),
            i => i.MatchLdcI4(out _),
            i => i.MatchSub(),
            i => i.MatchSwitch(out _)
            ))
        {
            return;
        }

        ILLabel[] switchArgs = (ILLabel[])cursor.Prev.Operand;
        switchArgs[(int)AssetValueType.Double - 1] = switchArgs[(int)AssetValueType.Int64 - 1];
    }
}
