using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace miCompressor.core;

/// <summary>
/// Image edits by file path. Kept apart from <see cref="MediaFileInfo"/>, as one file can have many of those: adding a folder and a file inside it makes two, and a rescan replaces them all.
/// </summary>
public static class ImageEditStore
{
    private static readonly ConcurrentDictionary<string, ImageEdit> s_edits = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Raised with the file's path when its edit is set or removed. Can be raised on any thread.
    /// </summary>
    public static event Action<string>? Changed;

    public static bool IsEmpty => s_edits.IsEmpty;

    /// <summary>
    /// Number of files excluded from batch resize.
    /// </summary>
    public static int ResizeOverrideCount => s_edits.Values.Count(edit => edit.OverridesBatchResize);

    public static ImageEdit? Get(string filePath) => s_edits.TryGetValue(filePath, out ImageEdit? edit) ? edit : null;

    /// <param name="filePath">Full path of the image.</param>
    /// <param name="edit">The edit to store, null to remove it.</param>
    public static void Set(string filePath, ImageEdit? edit)
    {
        if (edit == null)
        {
            Remove(filePath);
            return;
        }
        s_edits[filePath] = edit;
        Changed?.Invoke(filePath);
    }

    /// <summary>
    /// Removes the edits of all files except the given ones.
    /// </summary>
    public static void RetainOnly(IEnumerable<string> filePaths)
    {
        HashSet<string> keep = new(filePaths, StringComparer.OrdinalIgnoreCase);
        foreach (string filePath in s_edits.Keys)
        {
            if (!keep.Contains(filePath))
                Remove(filePath);
        }
    }

    private static void Remove(string filePath)
    {
        if (s_edits.TryRemove(filePath, out _))
            Changed?.Invoke(filePath);
    }
}
