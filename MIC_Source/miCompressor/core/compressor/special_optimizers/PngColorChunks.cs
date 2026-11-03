using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace miCompressor.core;

/// <summary>
/// Reads and restores PNG chunks that affect how colours render (iCCP, sRGB, gAMA, cHRM, cICP), for tools that drop them.
/// </summary>
internal static class PngColorChunks
{
    private static readonly string[] ColorChunkTypes = ["cICP", "iCCP", "sRGB", "gAMA", "cHRM"];
    private const int SignatureLength = 8;
    private const int ChunkOverhead = 12; // length + type + CRC

    /// <summary>
    /// Returns the colour chunks of the PNG as raw bytes, ready to write back with <see cref="Restore"/>.
    /// </summary>
    public static List<byte[]> Read(string pngPath)
    {
        byte[] png = File.ReadAllBytes(pngPath);
        return Chunks(png)
            .Where(c => ColorChunkTypes.Contains(c.Type))
            .Select(c => png.AsSpan(c.Offset, c.Length).ToArray())
            .ToList();
    }

    /// <summary>
    /// Types of the colour chunks present in the PNG.
    /// </summary>
    public static HashSet<string> TypesIn(string pngPath) =>
        Chunks(File.ReadAllBytes(pngPath)).Select(c => c.Type).Where(ColorChunkTypes.Contains).ToHashSet();

    /// <summary>
    /// Adds the given colour chunks right after IHDR (where the PNG spec requires them), unless the PNG already has colour chunks.
    /// </summary>
    public static void Restore(string pngPath, List<byte[]> colorChunks)
    {
        if (colorChunks.Count == 0)
            return;

        byte[] png = File.ReadAllBytes(pngPath);
        var chunks = Chunks(png).ToList();
        if (chunks.Count == 0 || chunks[0].Type != "IHDR" || chunks.Any(c => ColorChunkTypes.Contains(c.Type)))
            return;

        int insertAt = chunks[0].Offset + chunks[0].Length;
        using var output = new MemoryStream(png.Length + colorChunks.Sum(c => c.Length));
        output.Write(png, 0, insertAt);
        foreach (var chunk in colorChunks)
            output.Write(chunk);
        output.Write(png, insertAt, png.Length - insertAt);
        File.WriteAllBytes(pngPath, output.ToArray());
    }

    private static IEnumerable<(string Type, int Offset, int Length)> Chunks(byte[] png)
    {
        int offset = SignatureLength;
        while (offset + ChunkOverhead <= png.Length)
        {
            int dataLength = (png[offset] << 24) | (png[offset + 1] << 16) | (png[offset + 2] << 8) | png[offset + 3];
            if (dataLength < 0 || offset + ChunkOverhead + dataLength > png.Length)
                yield break;
            string type = Encoding.ASCII.GetString(png, offset + 4, 4);
            yield return (type, offset, ChunkOverhead + dataLength);
            if (type == "IEND")
                yield break;
            offset += ChunkOverhead + dataLength;
        }
    }
}
