using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace miCompressor.core;

/// <summary>
/// Warning to show after compression is done.
/// </summary>
public enum PostCompressionWarningType
{
    /// <summary>
    /// File size increased after compression. Possible reasons: File format changed, dimension changed.
    /// </summary>
    FileSizeIncreased,

    /// <summary>
    /// File format changed as user selected to keep original format but it is not supported as output format.
    /// </summary>
    FileFormatChanged,

    /// <summary>
    /// Some file already existed which is overwritten. If user selected to replace original file, this warning will not be shown as it is expected.
    /// </summary>
    FileOverwritten,

    /// <summary>
    /// Animation might be lost if we try to convert animated images into format that doesn't support animation (such as JPEG, TIFF).
    /// </summary>
    AnimationLost,
}

/// <summary>
/// Warning to show before compression starts. User actions can be captured to mitigate the warning - mostly skip the affected file or cancel the operation.
/// </summary>
public enum PreCompressionWarningType
{
    /// <summary>
    /// Destination file already exists. User may overwrite or cancel the operation. If user selected to replace original file, this warning will not be shown as it is expected.
    /// </summary>
    FileAlreadyExists,

    /// <summary>
    /// File format changed as user selected to keep original format but it is not supported as output format.
    /// </summary>
    FileFormatChanged,
}

/// <summary>
/// Error type for compression operation.
/// </summary>
public enum CompressionErrorType
{
    //no error
    None,
    /// <summary>
    /// Failed to compress the file. This is a generic error. Most of the time, this is due to unsupported feature of the format or corrupted file.
    /// </summary>
    FailedToCompress,

    /// <summary>
    /// Access denied to location where image should have been created
    /// </summary>
    AccessDenied,

    /// <summary>
    /// If compression was cancelled by user
    /// </summary>
    Cancelled
}


/// <summary>
/// Singleton class to show compression warnings to user.
/// </summary>
public partial class WarningHelper : INotifyPropertyChanged
{
    public static WarningHelper Instance { get; } = new WarningHelper();

    private readonly object _lock = new();

    private Dictionary<PostCompressionWarningType, List<MediaFileInfo>> postCompressionWarnings = new();
    private Dictionary<PreCompressionWarningType, List<MediaFileInfo>> preCompressionWarnings = new();
    private Dictionary<CompressionErrorType, List<MediaFileInfo>> compressionErrors = new();

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private void AddToDictionary<T>(Dictionary<T, List<MediaFileInfo>> dict, T type, MediaFileInfo info)
    {
        lock (_lock)
        {
            if (!dict.TryGetValue(type, out var list))
            {
                list = new List<MediaFileInfo>();
                dict[type] = list;
            }
            list.Add(info);
        }
    }

    private IReadOnlyDictionary<T, List<MediaFileInfo>> CopyOf<T>(Dictionary<T, List<MediaFileInfo>> dict)
    {
        lock (_lock)
        {
            return dict.ToDictionary(
                entry => entry.Key,
                entry => new List<MediaFileInfo>(entry.Value)
            );
        }
    }

    // Methods to Add or Clear warnings/errors
    public void AddPostWarning(PostCompressionWarningType type, MediaFileInfo info)
    {
        AddToDictionary(postCompressionWarnings, type, info);
        OnPropertyChanged(nameof(PostCompressionWarnings));
    }

    public void AddPreWarning(PreCompressionWarningType type, MediaFileInfo info)
    {
        AddToDictionary(preCompressionWarnings, type, info);
        OnPropertyChanged(nameof(PreCompressionWarnings));
    }

    public void AddCompressionError(CompressionErrorType type, MediaFileInfo info)
    {
        AddToDictionary(compressionErrors, type, info);
        OnPropertyChanged(nameof(CompressionErrors));
    }

    public IReadOnlyDictionary<PostCompressionWarningType, List<MediaFileInfo>> PostCompressionWarnings
    {
        get
        {
            return CopyOf(postCompressionWarnings);
        }
    }        

    public IReadOnlyDictionary<PreCompressionWarningType, List<MediaFileInfo>> PreCompressionWarnings
    {
        get
        {
            return CopyOf(preCompressionWarnings);
        }
    }

    public IReadOnlyDictionary<CompressionErrorType, List<MediaFileInfo>> CompressionErrors
    {
        get
        {
            return CopyOf(compressionErrors);
        }
    }

    public void ClearAll()
    {
        lock (_lock)
        {
            postCompressionWarnings.Clear();
            preCompressionWarnings.Clear();
            compressionErrors.Clear();
        }

        OnPropertyChanged(nameof(PostCompressionWarnings));
        OnPropertyChanged(nameof(PreCompressionWarnings));
        OnPropertyChanged(nameof(CompressionErrors));
    }
}
