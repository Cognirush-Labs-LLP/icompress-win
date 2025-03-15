using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;

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
    /// As the compressed file was larger, original file was used instead instead.
    /// </summary>
    UsedOriginalFile,

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

    /// <summary>
    /// Output location of the file is computed same as input location so user may lose original file. Note that this flag is not applicable if user has selected to Replace Original option.
    /// </summary>
    LoosingOriginalImage,
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
/// ViewModel to enable binding Enums with descriptions in XAML.
/// </summary>
public class EnumViewModel<T> : INotifyPropertyChanged where T : Enum
{
    private readonly Dictionary<T, string> _descriptions;

    private T _value;
    public event PropertyChangedEventHandler? PropertyChanged;

    public EnumViewModel(Dictionary<T, string> descriptions)
    {
        _descriptions = descriptions;
    }

    /// <summary>
    /// Gets or sets the enum value.
    /// </summary>
    public T Value
    {
        get => _value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                _value = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Description)));
            }
        }
    }

    /// <summary>
    /// Gets the user-friendly description for the selected enum value.
    /// </summary>
    public string Description => _descriptions.TryGetValue(_value, out var description) ? description : _value.ToString();
}
/// <summary>
/// Generic provider for retrieving descriptions of enums.
/// </summary>
/// <typeparam name="T">Enum type</typeparam>
public static class EnumDescriptionProvider<T> where T : Enum
{
    private static readonly Dictionary<T, string> _descriptions = new();

    /// <summary>
    /// Static constructor to initialize descriptions.
    /// </summary>
    static EnumDescriptionProvider()
    {
        if (typeof(T) == typeof(PostCompressionWarningType))
        {
            var map = new Dictionary<PostCompressionWarningType, string>
                {
                    { PostCompressionWarningType.FileSizeIncreased, "File size increased after compression. Cannot use original file as dimension or output format is changed." },
                    { PostCompressionWarningType.UsedOriginalFile, "Used original file because the compressed file was larger than the original." },
                    { PostCompressionWarningType.FileFormatChanged, "File format changed as the selected original format is not supported in output." },
                    { PostCompressionWarningType.FileOverwritten, "Existing file was overwritten, looks unintentional." },
                    { PostCompressionWarningType.AnimationLost, "Animation lost when converting an animated image to a format that does not support animation." }
                };

            foreach (var kvp in map)
                _descriptions[(T)(object)kvp.Key] = kvp.Value;
        }
        else if (typeof(T) == typeof(PreCompressionWarningType))
        {
            var map = new Dictionary<PreCompressionWarningType, string>
                {
                    { PreCompressionWarningType.FileAlreadyExists, "Destination file already exists. You may choose to overwrite or cancel." },
                    { PreCompressionWarningType.FileFormatChanged, "File format will changed as the selected original format is not supported in output." },
                    { PreCompressionWarningType.LoosingOriginalImage, "Output location matches input, risking loss of the original file - looks unintentional" }
                };

            foreach (var kvp in map)
                _descriptions[(T)(object)kvp.Key] = kvp.Value;
        }
        else if (typeof(T) == typeof(CompressionErrorType))
        {
            var map = new Dictionary<CompressionErrorType, string>
                {
                    { CompressionErrorType.None, "No error occurred during compression." },
                    { CompressionErrorType.FailedToCompress, "Compression failed due to unsupported format features or file corruption." },
                    { CompressionErrorType.AccessDenied, "Access denied to the output location." },
                    { CompressionErrorType.Cancelled, "Compression was canceled." }
                };

            foreach (var kvp in map)
                _descriptions[(T)(object)kvp.Key] = kvp.Value;
        }
    }

    /// <summary>
    /// Retrieves the description of the given enum value.
    /// </summary>
    public static string Description(T value)
    {
        return _descriptions.TryGetValue(value, out var description) ? description : value.ToString();
    }
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

    public bool HasWarnings => postCompressionWarnings.Count > 0;
    public bool HasPreCompressionWarnings => preCompressionWarnings.Count > 0;
    public bool HasErrors => compressionErrors.Count > 0;

    public int WarningCount => postCompressionWarnings.Values.Sum(list => list.Count);
    public int ErrorCount => compressionErrors.Values.Sum(list => list.Count);

    public event PropertyChangedEventHandler PropertyChanged;

    protected virtual void OnPropertyChanged(string propertyName)
    {
        UIThreadHelper.RunOnUIThread(() =>
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        });
    }

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
        OnPropertyChanged(nameof(HasWarnings));
        OnPropertyChanged(nameof(WarningCount));
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
        OnPropertyChanged(nameof(HasErrors));
        OnPropertyChanged(nameof(ErrorCount));

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

    public void ClearPreCompressionWarning()
    {
        lock (_lock) 
            preCompressionWarnings.Clear();
        OnPropertyChanged(nameof(PreCompressionWarnings));
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

        OnPropertyChanged(nameof(HasWarnings));
        OnPropertyChanged(nameof(WarningCount));
        OnPropertyChanged(nameof(HasErrors));
        OnPropertyChanged(nameof(ErrorCount));
    }
}
