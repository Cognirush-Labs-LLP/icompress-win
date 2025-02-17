# App

Namespace: miCompressor

Provides application-specific behavior to supplement the default Application class.

```csharp
public class App : Microsoft.UI.Xaml.Application, Microsoft.UI.Xaml.IApplicationOverrides, System.Runtime.InteropServices.ICustomQueryInterface, WinRT.IWinRTObject, System.Runtime.InteropServices.IDynamicInterfaceCastable, System.Runtime.InteropServices.Marshalling.IUnmanagedVirtualMethodTableProvider, System.IEquatable`1[[Microsoft.UI.Xaml.Application, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Markup.IXamlMetadataProvider
```

Inheritance: Object → Application → App<br>
Implements: IApplicationOverrides, ICustomQueryInterface, IWinRTObject, IDynamicInterfaceCastable, IUnmanagedVirtualMethodTableProvider, IEquatable&lt;Application&gt;, IXamlMetadataProvider<br>
Attributes: WinRTRuntimeClassNameAttribute, WinRTExposedTypeAttribute, ApplicationRcwFactoryAttribute, ContractVersionAttribute

## Fields

### m_window

```csharp
private Window m_window;
```

### _contentLoaded

```csharp
private bool _contentLoaded;
```

### __appProvider

```csharp
private XamlMetaDataProvider __appProvider;
```

## Properties

### FileStoreInstance

App-wide instance of FileStore.

```csharp
public static FileStore FileStoreInstance { get; }
```

### OutputSettingsInstance

App-wide instance of OutputSettings.

```csharp
public static OutputSettings OutputSettingsInstance { get; }
```

### _AppProvider

```csharp
private XamlMetaDataProvider _AppProvider { get; }
```

## Constructors

### App()

Initializes the singleton application object. This is the first line of authored code
 executed, and as such is the logical equivalent of main() or WinMain().

```csharp
public App()
```

### App()

Initializes the singleton application object. This is the first line of authored code
 executed, and as such is the logical equivalent of main() or WinMain().

```csharp
private static App()
```

## Methods

### OnLaunched(LaunchActivatedEventArgs)

Invoked when the application is launched.

```csharp
protected void OnLaunched(LaunchActivatedEventArgs args)
```

#### Parameters

`args` LaunchActivatedEventArgs<br>
Details about the launch request and process.

### InitializeComponent()

InitializeComponent()

```csharp
public void InitializeComponent()
```

### GetXamlType(Type)

GetXamlType(Type)

```csharp
public IXamlType GetXamlType(Type type)
```

#### Parameters

`type` Type<br>

#### Returns

IXamlType<br>

### GetXamlType(String)

GetXamlType(String)

```csharp
public IXamlType GetXamlType(string fullName)
```

#### Parameters

`fullName` String<br>

#### Returns

IXamlType<br>

### GetXmlnsDefinitions()

GetXmlnsDefinitions()

```csharp
public XmlnsDefinition[] GetXmlnsDefinitions()
```

#### Returns

XmlnsDefinition[]<br>
 
# AdvancedSettings

Namespace: miCompressor.core

Provides advanced settings for the application which user can change.

```csharp
public class AdvancedSettings : System.ComponentModel.INotifyPropertyChanged
```

Inheritance: Object → AdvancedSettings<br>
Implements: INotifyPropertyChanged<br>
Attributes: NullableContextAttribute, NullableAttribute

## Fields

### defaultImageExtension

If the input image has unsupported output format, this format will be used when user wants to keep the original format.

```csharp
[AutoNotify] public OutputFormat defaultImageExtension;
```

## Properties

### Instance

```csharp
public static AdvancedSettings Instance { get; }
```

### DefaultImageExtension

```csharp
public OutputFormat DefaultImageExtension { get; set; }
```

## Constructors

### AdvancedSettings()

```csharp
private AdvancedSettings()
```

### AdvancedSettings()

```csharp
private static AdvancedSettings()
```

## Methods

### OnPropertyChanged(String)

```csharp
protected void OnPropertyChanged(string propertyName)
```

#### Parameters

`propertyName` String<br>

## Events

### PropertyChanged

```csharp
public event PropertyChangedEventHandler PropertyChanged;
```
 
# AutoNotifyAttribute

Namespace: miCompressor.core

This attribute helps generate a public property that auto implements getter and setters for ObservableBase to trigger a change event.

```csharp
public class AutoNotifyAttribute : System.Attribute
```

Inheritance: Object → Attribute → AutoNotifyAttribute<br>
Attributes: AttributeUsageAttribute

**Remarks:**

Do Not Create variable name with first letter capital or something that cannot be capitalized such as underscore. A good variable name 'width'. Bad variable names '_width', 'Width'.
 Additionally, class using `AutoNotifyAttribute` must not be an inner class of any other class.

## Properties

### TypeId

```csharp
public object TypeId { get; }
```

## Constructors

### AutoNotifyAttribute()

```csharp
public AutoNotifyAttribute()
```
 
# CodeConsts

Namespace: miCompressor.core

```csharp
public static class CodeConsts
```

Inheritance: Object → CodeConsts<br>
Attributes: NullableContextAttribute, NullableAttribute

## Fields

### ThumbSize

Size of Thumbnail in pixels to show image list in UI

```csharp
public static int ThumbSize = 100;
```

### SupportedInputExtensions

Supported lower cased input file extensions without dot. i.e. "jpg", "jpeg", "png", "webp".

```csharp
public static String[] SupportedInputExtensions = ["jpg", "jpeg", "png", "webp"];
```

### SupportedOutputExtensions

Supported lower cased output file extensions without dot. i.e. "jpg", "png", "webp"

```csharp
public static String[] SupportedOutputExtensions = ["jpg", "png", "webp"];
```

### compressedDirName

```csharp
public static string compressedDirName = "Compressed";
```

## Properties

### SupportedInputExtensionsWithDot

Generated from <![CDATA[SupportedInputExtensions]]> but file extensions with dot. i.e. ".jpg", ".jpeg", ".png", ".webp"

```csharp
public static HashSet<string> SupportedInputExtensionsWithDot { get; }
```

## Constructors

### CodeConsts()

```csharp
private static CodeConsts()
```
 
# CompressionErrorType

Namespace: miCompressor.core.common

Error type for compression operation.

```csharp
public enum CompressionErrorType
```

Inheritance: Object → ValueType → Enum → CompressionErrorType<br>
Implements: IComparable, ISpanFormattable, IFormattable, IConvertible

## Fields

| Name | Value | Description |
| --- | --: | --- |
| FailedToCompress | 0 | Failed to compress the file. This is a generic error. Most of the time, this is due to unsupported feature of the format or corrupted file. |
 
# PostCompressionWarningType

Namespace: miCompressor.core.common

Warning to show after compression is done.

```csharp
public enum PostCompressionWarningType
```

Inheritance: Object → ValueType → Enum → PostCompressionWarningType<br>
Implements: IComparable, ISpanFormattable, IFormattable, IConvertible

## Fields

| Name | Value | Description |
| --- | --: | --- |
| FileSizeIncreased | 0 | File size increased after compression. Possible reasons: File format changed, dimension changed. |
| FileFormatChanged | 1 | File format changed as user selected to keep original format but it is not supported as output format. |
| FileOverwritten | 2 | Some file already existed which is overwritten. If user selected to replace original file, this warning will not be shown as it is expected. |
 
# PreCompressionWarningType

Namespace: miCompressor.core.common

Warning to show before compression starts. User actions can be captured to mitigate the warning - mostly skip the affected file or cancel the operation.

```csharp
public enum PreCompressionWarningType
```

Inheritance: Object → ValueType → Enum → PreCompressionWarningType<br>
Implements: IComparable, ISpanFormattable, IFormattable, IConvertible

## Fields

| Name | Value | Description |
| --- | --: | --- |
| FileAlreadyExists | 0 | Destination file already exists. User may overwrite or cancel the operation. If user selected to replace original file, this warning will not be shown as it is expected. |
| FileFormatChanged | 1 | File format changed as user selected to keep original format but it is not supported as output format. |
 
# WarningHelper

Namespace: miCompressor.core.common

Singleton class to show compression warnings to user.

```csharp
public class WarningHelper : System.ComponentModel.INotifyPropertyChanged
```

Inheritance: Object → WarningHelper<br>
Implements: INotifyPropertyChanged<br>
Attributes: NullableContextAttribute, NullableAttribute, WinRTRuntimeClassNameAttribute, WinRTExposedTypeAttribute

## Fields

### _lock

```csharp
private object _lock = System.Object;
```

### postCompressionWarnings

```csharp
private Dictionary<PostCompressionWarningType, List<MediaFileInfo>> postCompressionWarnings = System.Collections.Generic.Dictionary`2[miCompressor.core.common.PostCompressionWarningType,System.Collections.Generic.List`1[miCompressor.core.MediaFileInfo]];
```

### preCompressionWarnings

```csharp
private Dictionary<PreCompressionWarningType, List<MediaFileInfo>> preCompressionWarnings = System.Collections.Generic.Dictionary`2[miCompressor.core.common.PreCompressionWarningType,System.Collections.Generic.List`1[miCompressor.core.MediaFileInfo]];
```

### compressionErrors

```csharp
private Dictionary<CompressionErrorType, List<MediaFileInfo>> compressionErrors = System.Collections.Generic.Dictionary`2[miCompressor.core.common.CompressionErrorType,System.Collections.Generic.List`1[miCompressor.core.MediaFileInfo]];
```

## Properties

### Instance

```csharp
public static WarningHelper Instance { get; }
```

### PostCompressionWarnings

```csharp
public IReadOnlyDictionary<PostCompressionWarningType, List<MediaFileInfo>> PostCompressionWarnings { get; }
```

### PreCompressionWarnings

```csharp
public IReadOnlyDictionary<PreCompressionWarningType, List<MediaFileInfo>> PreCompressionWarnings { get; }
```

### CompressionErrors

```csharp
public IReadOnlyDictionary<CompressionErrorType, List<MediaFileInfo>> CompressionErrors { get; }
```

## Constructors

### WarningHelper()

```csharp
public WarningHelper()
```

### WarningHelper()

```csharp
private static WarningHelper()
```

## Methods

### OnPropertyChanged(String)

```csharp
protected void OnPropertyChanged(string propertyName)
```

#### Parameters

`propertyName` String<br>

### AddToDictionary&lt;T&gt;(Dictionary&lt;T, List&lt;MediaFileInfo&gt;&gt;, T, MediaFileInfo)

```csharp
private void AddToDictionary<T>(Dictionary<T, List<MediaFileInfo>> dict, T type, MediaFileInfo info)
```

#### Type Parameters

`T`<br>

#### Parameters

`dict` Dictionary&lt;T, List&lt;MediaFileInfo&gt;&gt;<br>

`type` T<br>

`info` MediaFileInfo<br>

### CopyOf&lt;T&gt;(Dictionary&lt;T, List&lt;MediaFileInfo&gt;&gt;)

```csharp
private IReadOnlyDictionary<T, List<MediaFileInfo>> CopyOf<T>(Dictionary<T, List<MediaFileInfo>> dict)
```

#### Type Parameters

`T`<br>

#### Parameters

`dict` Dictionary&lt;T, List&lt;MediaFileInfo&gt;&gt;<br>

#### Returns

IReadOnlyDictionary&lt;T, List&lt;MediaFileInfo&gt;&gt;<br>

### AddPostWarning(PostCompressionWarningType, MediaFileInfo)

```csharp
public void AddPostWarning(PostCompressionWarningType type, MediaFileInfo info)
```

#### Parameters

`type` PostCompressionWarningType<br>

`info` MediaFileInfo<br>

### AddPreWarning(PreCompressionWarningType, MediaFileInfo)

```csharp
public void AddPreWarning(PreCompressionWarningType type, MediaFileInfo info)
```

#### Parameters

`type` PreCompressionWarningType<br>

`info` MediaFileInfo<br>

### AddCompressionError(CompressionErrorType, MediaFileInfo)

```csharp
public void AddCompressionError(CompressionErrorType type, MediaFileInfo info)
```

#### Parameters

`type` CompressionErrorType<br>

`info` MediaFileInfo<br>

### ClearAll()

```csharp
public void ClearAll()
```

## Events

### PropertyChanged

```csharp
public event PropertyChangedEventHandler PropertyChanged;
```
 
# DimensionHelper

Namespace: miCompressor.core

Helper class to calculate output dimensions based on the provided settings.

```csharp
public static class DimensionHelper
```

Inheritance: Object → DimensionHelper

## Methods

### GetOutputDimensions(OutputSettings, Int32, Int32)

Calculates the output dimensions based on the provided output settings.
 This method ensures correct aspect ratio preservation and handles various dimension strategies.

```csharp
public static ValueTuple<int, int> GetOutputDimensions(OutputSettings outputSettings, int originalWidth, int originalHeight)
```

#### Parameters

`outputSettings` OutputSettings<br>
Output settings containing dimension reduction strategies.

`originalWidth` Int32<br>
Original image width in pixels.

`originalHeight` Int32<br>
Original image height in pixels.

#### Returns

ValueTuple&lt;Int32, Int32&gt;<br>
A tuple representing the new height and width.
 
# DimensionReductionStrategy

Namespace: miCompressor.core

Common Strategy to reduce the image dimension. We always maintain the aspect ratio of the image.

```csharp
public enum DimensionReductionStrategy
```

Inheritance: Object → ValueType → Enum → DimensionReductionStrategy<br>
Implements: IComparable, ISpanFormattable, IFormattable, IConvertible

## Fields

| Name | Value | Description |
| --- | --: | --- |
| KeepSame | 0 | No change in dimension |
| Percentage | 1 | Reduce dimension by percentage |
| LongEdge | 2 | Longest Edge of the image will be considered and will be reduced to specified length. If the long edge is already smaller than specified length, no change is performed. |
| MaxHeight | 3 | Resize to specified height. Image may only decrease in dimension with this settings as if the height is already smaller than specified length, no change is performed. |
| MaxWidth | 4 | Resize to specified width. Image may only decrease in dimension with this settings as if the width is already smaller than specified length, no change is performed. |
| FixedHeight | 5 | Fix the height of each image to specified length. Image size may increase or decrease. |
| FixedWidth | 6 | Fix the width of each image to specified length. Image size may increase or decrease. |
| FitInFrame | 7 | Used for resizing images for print size.  Fit the image in a frame of specified size. Image size will not increase as smaller image will be keep as same. Frame is made of two edges, primary and secondary. Primary edge is the one which is fixed and secondary edge is the one which is flexible. Primary edge is always the longest edge of the image. |
| FixedInFrame | 8 | Used for resizing images for print size.  Fix the image in a frame of specified size. Image size may increase or decrease. Frame is made of two edges, primary and secondary. Primary edge is the one which is fixed and secondary edge is the one which is flexible. Primary edge is always the longest edge of the image. |
 
# FileStore

Namespace: miCompressor.core

Manages a thread-safe collection of selected file paths.

```csharp
public class FileStore : ObservableBase, System.ComponentModel.INotifyPropertyChanged
```

Inheritance: Object → ObservableBase → FileStore<br>
Implements: INotifyPropertyChanged<br>
Attributes: NullableContextAttribute, NullableAttribute

## Fields

### _store

```csharp
private List<SelectedPath> _store = System.Collections.Generic.List`1[miCompressor.core.SelectedPath];
```

### _lock

```csharp
private ReaderWriterLockSlim _lock = System.Threading.ReaderWriterLockSlim;
```

## Properties

### SelectedPaths

Retrieves a thread-safe read-only collection of selected paths.

```csharp
public IReadOnlyCollection<SelectedPath> SelectedPaths { get; }
```

### GetAllFiles

Retrieves all unique media files across selected paths.

```csharp
public IReadOnlyCollection<MediaFileInfo> GetAllFiles { get; }
```

**Remarks:**

This is dynamically created list, be mindful and cache for multiple access.

## Constructors

### FileStore()

```csharp
public FileStore()
```

## Methods

### AddAsync(String, Boolean)

Adds a new path to the store. This method returns the result just after making entry in selected path but all files to process will populate only after

```csharp
public PathAddedResult AddAsync(string path, bool scanSubDirectories)
```

#### Parameters

`path` String<br>
The file or directory path.

`scanSubDirectories` Boolean<br>
Indicates whether subdirectories should be scanned.

#### Returns

PathAddedResult<br>
The result of the add operation.

### Remove(String)

Removes a specified path from the store in a thread-safe manner.

```csharp
public bool Remove(string path)
```

#### Parameters

`path` String<br>
The path to remove.

#### Returns

Boolean<br>
True if the path was removed, false if it was not found.

### ChangeIncludeSubDirectoriesSetting(String, Boolean)

Change setting of scanning the added path to include Subdirectories or not. No change if settings are not changed.

```csharp
public bool ChangeIncludeSubDirectoriesSetting(string path, bool includeSubDirectories)
```

#### Parameters

`path` String<br>
The path to remove.

`includeSubDirectories` Boolean<br>
Should Include Sub-Directories or not

#### Returns

Boolean<br>
True if the path was found as selected path and will be taken up for making setting change if aplicable.

### RemoveAll()

Remove all stored paths in a thread-safe manner.

```csharp
public void RemoveAll()
```

## Events

### PropertyChanged

Event triggered when a property value changes.

```csharp
public event PropertyChangedEventHandler PropertyChanged;
```
 
# HumanReadable

Namespace: miCompressor.core

```csharp
public static class HumanReadable
```

Inheritance: Object → HumanReadable

## Methods

### FileSize(UInt64)

Converts a file size in bytes to a human-readable string format.
 Supports B, KB, MB, GB, TB, PB up to exabytes.

```csharp
public static string FileSize(ulong fileSize)
```

#### Parameters

`fileSize` UInt64<br>
The file size in bytes (ulong).

#### Returns

String<br>
Formatted string with appropriate unit (B, KB, MB, GB, etc.).
 
# MediaFileInfo

Namespace: miCompressor.core

Represents an image file, storing metadata and relative path for compression.
 Uses async loading to prevent UI blocking and supports automatic UI updates.

```csharp
public class MediaFileInfo : System.ComponentModel.INotifyPropertyChanged
```

Inheritance: Object → MediaFileInfo<br>
Implements: INotifyPropertyChanged<br>
Attributes: NullableContextAttribute, NullableAttribute

## Fields

### width

The width of the input image in pixels.

```csharp
[AutoNotify] private int width;
```

### height

The height of the input image in pixels.

```csharp
[AutoNotify] private int height;
```

### fileSize

The size of the input image file in bytes.

```csharp
[AutoNotify] private ulong fileSize;
```

### cameraModel

```csharp
[AutoNotify] private string cameraModel;
```

### dateTaken

```csharp
[AutoNotify] private Nullable<DateTimeOffset> dateTaken;
```

### scanningForFiles

```csharp
[AutoNotify] private bool scanningForFiles;
```

### excludeAndHide

Exclude the media from processing and hide the file in gallery, used for files are not eligible due to filter settings.

```csharp
[AutoNotify] private bool excludeAndHide;
```

### excludeAndShow

Exclude the media from processing but do not hide the file in gallery, may be show it greyed out

```csharp
[AutoNotify] private bool excludeAndShow;
```

### _thumbnail

```csharp
private BitmapImage _thumbnail;
```

### s_thumbnailTasks

```csharp
private static ConcurrentDictionary<string, Task<BitmapImage>> s_thumbnailTasks = System.Collections.Concurrent.ConcurrentDictionary`2[System.String,System.Threading.Tasks.Task`1[Microsoft.UI.Xaml.Media.Imaging.BitmapImage]];
```

### s_globalLoadSemaphore

```csharp
private static SemaphoreSlim s_globalLoadSemaphore = System.Threading.SemaphoreSlim;
```

### MaxParallelThumbnailLoads

```csharp
private static int MaxParallelThumbnailLoads = 8;
```

## Properties

### SelectedRootPath

The root directory selected by the user.

```csharp
public string SelectedRootPath { get; }
```

### FileToCompress

The full path to the image file.

```csharp
public FileInfo FileToCompress { get; }
```

### RelativePath

The relative path of the image within the selected directory.

```csharp
public string RelativePath { get; }
```

### RelativeImageDirPath

The relative path of directory containing the image within the selected directory.

```csharp
public string RelativeImageDirPath { get; }
```

### IsMetadataLoaded

```csharp
public bool IsMetadataLoaded { get; }
```

### IsReplaceOperation

Determines whether this operation is replacing the original file.
 Used when OutputLocationSettings is set to ReplaceOriginal.

```csharp
public bool IsReplaceOperation { get; private set; }
```

### ShouldProcess

returns excludeAndHide AND excludeAndShow

```csharp
public bool ShouldProcess { get; }
```

### FileSizeToShow

```csharp
public string FileSizeToShow { get; }
```

### FilePath

```csharp
public string FilePath { get; }
```

### ThumbnailSize

```csharp
public uint ThumbnailSize { get; set; }
```

### s_thumbnailTasksKey

```csharp
private string s_thumbnailTasksKey { get; }
```

### Thumbnail

Exposes the thumbnail as a bindable property. Loads it on first access.

```csharp
public BitmapImage Thumbnail { get; private set; }
```

### Width

```csharp
public int Width { get; set; }
```

### Height

```csharp
public int Height { get; set; }
```

### FileSize

```csharp
public ulong FileSize { get; set; }
```

### CameraModel

```csharp
public string CameraModel { get; set; }
```

### DateTaken

```csharp
public Nullable<DateTimeOffset> DateTaken { get; set; }
```

### ScanningForFiles

```csharp
public bool ScanningForFiles { get; set; }
```

### ExcludeAndHide

```csharp
public bool ExcludeAndHide { get; set; }
```

### ExcludeAndShow

```csharp
public bool ExcludeAndShow { get; set; }
```

## Constructors

### MediaFileInfo(String, FileInfo)

Initializes a new instance of MediaFileInfo.

```csharp
public MediaFileInfo(string selectedPath, FileInfo mediaFile)
```

#### Parameters

`selectedPath` String<br>
The root directory selected by the user.

`mediaFile` FileInfo<br>
FileInfo of the image/video

#### Exceptions

FileNotFoundException<br>
Throws exception if file doesn't exist.

### MediaFileInfo()

```csharp
private static MediaFileInfo()
```

## Methods

### LoadImageMetadataAsync(Boolean)

Loads image metadata asynchronously without blocking the UI.

```csharp
private Task LoadImageMetadataAsync(bool force)
```

#### Parameters

`force` Boolean<br>

#### Returns

Task<br>

### LoadImageMetadataAsync(String)

Loads image metadata asynchronously without blocking the UI.
 If no file path is provided, it loads metadata for the original file.

```csharp
private Task<ImageMetadata> LoadImageMetadataAsync(string filePath)
```

#### Parameters

`filePath` String<br>
The full path of the file to load metadata from.

#### Returns

Task&lt;ImageMetadata&gt;<br>
Returns an ImageMetadata object containing the extracted details.

### GetOutputPath(OutputSettings, Boolean)

Generates the output path based on the specified output settings.
 Determines the appropriate file path based on multiple settings such as
 location strategy, file naming modifications, and preview mode.
 There is no guarantee that the actual file creation path will be same as this method returns temporary path for replace operations. 
 Throws InvalidOperationException if required settings are missing.
 Throws NotSupportedException if an unsupported OutputLocationSettings is encountered.

```csharp
public string GetOutputPath(OutputSettings outputSettings, bool onlyPreview)
```

#### Parameters

`outputSettings` OutputSettings<br>
The output settings.

`onlyPreview` Boolean<br>
If true, the output will be stored in a temporary directory.

#### Returns

String<br>
The full path of the output file. Doesn't actually create file or directory.

**Remarks:**

The following variables in `outputSettings` must be properly set before calling this method:

- `outputFolder` (required for UserSpecificFolder strategy)
- `prefix`
- `suffix`
- `replaceFrom` (optional but affects filename transformation)
- `replaceTo` (optional, used with replaceFrom)

This method may throw an exception if required output settings are not provided.

### FreezeOutputAsync(String, Int32, Int32)

Finalizes the output operation by verifying file size before replacing or saving.
 Ensures that the original file is not replaced or duplicated with a larger one.
 If compression fails, it copies the original file when appropriate.

```csharp
public Task<ValueTuple<bool, bool>> FreezeOutputAsync(string outputPath, int expectedWidth, int expectedHeight)
```

#### Parameters

`outputPath` String<br>
The path where the compressed file was saved.

`expectedWidth` Int32<br>
The expected width of the compressed image.

`expectedHeight` Int32<br>
The expected height of the compressed image.

#### Returns

Task&lt;ValueTuple&lt;Boolean, Boolean&gt;&gt;<br>
A tuple with:
 1. wasOriginalFileUsed (True if we discarded the compressed file and kept the original).
 2. failedToFreezeOutput (True if the compressed file was corrupt or unreadable).

### ShouldCopyOriginal(Int32, Int32, String)

Determines if the original file should be copied instead of the output file.

```csharp
private bool ShouldCopyOriginal(int expectedWidth, int expectedHeight, string outputPath)
```

#### Parameters

`expectedWidth` Int32<br>
Expected width of the output image.

`expectedHeight` Int32<br>
Expected height of the output image.

`outputPath` String<br>
Path of the compressed file.

#### Returns

Boolean<br>
True if the original file should be copied instead.

### IsLargerFileWithoutChange(ImageMetadata, String)

Checks if the given output file is larger while having the same dimensions and extension.

```csharp
private bool IsLargerFileWithoutChange(ImageMetadata outputMeta, string outputFilePath)
```

#### Parameters

`outputMeta` ImageMetadata<br>
Metadata of the compressed file.

`outputFilePath` String<br>
Path of the compressed file.

#### Returns

Boolean<br>
True if the output file is larger without any useful change.

### HasSameExtension(String)

Checks if the given file has the same extension as the original file.

```csharp
private bool HasSameExtension(string filePath)
```

#### Parameters

`filePath` String<br>
Path of the file to compare.

#### Returns

Boolean<br>
True if the file extension matches the original.

### HasSameDimensions(Int32, Int32)

Checks if the given dimensions match the original file dimensions.

```csharp
private bool HasSameDimensions(int width, int height)
```

#### Parameters

`width` Int32<br>
Width of the file to compare.

`height` Int32<br>
Height of the file to compare.

#### Returns

Boolean<br>
True if the dimensions match the original.

### LoadThumbnailAsync()

```csharp
private Task LoadThumbnailAsync()
```

#### Returns

Task<br>

### LoadThumbnailCoreAsync()

Loads the thumbnail with a global concurrency limit of 8.

```csharp
private Task<BitmapImage> LoadThumbnailCoreAsync()
```

#### Returns

Task&lt;BitmapImage&gt;<br>

### LoadThumbnailWithFallbackAsync()

Attempts to load the thumbnail using Windows.Storage API first.
 Falls back to ImageMagick if Windows.Storage fails.

```csharp
private Task<BitmapImage> LoadThumbnailWithFallbackAsync()
```

#### Returns

Task&lt;BitmapImage&gt;<br>

### TryGetThumbnailFromStorageApiAsync()

```csharp
private Task<BitmapImage> TryGetThumbnailFromStorageApiAsync()
```

#### Returns

Task&lt;BitmapImage&gt;<br>

### GenerateThumbnailWithImageMagickAsync()

```csharp
private Task<BitmapImage> GenerateThumbnailWithImageMagickAsync()
```

#### Returns

Task&lt;BitmapImage&gt;<br>

### GeneratePlaceholderThumbnail()

Generates a placeholder thumbnail in case all attempts fail.

```csharp
private BitmapImage GeneratePlaceholderThumbnail()
```

#### Returns

BitmapImage<br>

### Finalize()

```csharp
protected void Finalize()
```

### OnPropertyChanged(String)

```csharp
protected void OnPropertyChanged(string propertyName)
```

#### Parameters

`propertyName` String<br>

### &lt;LoadThumbnailAsync&gt;b__53_0(String)

```csharp
private Task<BitmapImage> <LoadThumbnailAsync>b__53_0(string _)
```

#### Parameters

`_` String<br>

#### Returns

Task&lt;BitmapImage&gt;<br>

## Events

### PropertyChanged

```csharp
public event PropertyChangedEventHandler PropertyChanged;
```
 
# ObservableBase

Namespace: miCompressor.core

Base class for observable objects that supports property change notifications
 without requiring explicit private fields.

```csharp
public class ObservableBase : System.ComponentModel.INotifyPropertyChanged
```

Inheritance: Object → ObservableBase<br>
Implements: INotifyPropertyChanged<br>
Attributes: NullableContextAttribute, NullableAttribute

#### Example

```csharp
public int Width
{
    get => GetProperty<int>(); // Retrieve the current value from the backing store.
    set
    {
        // Custom logic before change:
        if (value < 0)
            throw new ArgumentException("Width cannot be negative");
        
        // Optionally get the previous value (if needed)
        int previous = GetProperty<int>();
        
        // Attempt to update the property. SetProperty returns true if the value changed.
        if (SetProperty(value, (oldVal, newVal) =>
            {
                // This callback is only invoked if the property value actually changes.
                Console.WriteLine($"Width changed from {oldVal} to {newVal}");
            }))
        {
            // Execute additional custom logic if needed after the property changed
            DoSomethingExtra();
        }
    }
}
```

## Fields

### _propertyValues

```csharp
private Dictionary<string, object> _propertyValues = System.Collections.Generic.Dictionary`2[System.String,System.Object];
```

### _changeCallbacks

```csharp
private Dictionary<string, Action<object, object>> _changeCallbacks = System.Collections.Generic.Dictionary`2[System.String,System.Action`2[System.Object,System.Object]];
```

### _lock

```csharp
private ReaderWriterLockSlim _lock = System.Threading.ReaderWriterLockSlim;
```

## Constructors

### ObservableBase()

```csharp
public ObservableBase()
```

## Methods

### GetProperty&lt;T&gt;(T, String)

Gets a property value. If the property is not set, returns the provided default value.

```csharp
protected T GetProperty<T>(T defaultValue, string propertyName)
```

#### Type Parameters

`T`<br>
Type of the property value.

#### Parameters

`defaultValue` T<br>
Optional default value (default is null or default(T)).

`propertyName` String<br>
Name of the property (automatically inferred).

#### Returns

T<br>
The stored value or the default if not set.

### SetProperty&lt;T&gt;(T, Action&lt;T, T&gt;, String)

Sets a property value and triggers PropertyChanged event if the value has changed.
 Also optionally invokes a callback when the value changes.

```csharp
protected bool SetProperty<T>(T value, Action<T, T> onChanged, string propertyName)
```

#### Type Parameters

`T`<br>
Type of the property value.

#### Parameters

`value` T<br>
New value to set.

`onChanged` Action&lt;T, T&gt;<br>
Optional callback executed when the property value changes.

`propertyName` String<br>
Name of the property (automatically inferred).

#### Returns

Boolean<br>
True if the property value was changed, false otherwise.

### OnPropertyChanged(String)

Raises event in UI thread if the UI Thread is available. Ignores raising the event otherwise.

```csharp
protected void OnPropertyChanged(string propertyName)
```

#### Parameters

`propertyName` String<br>
Name of the property that changed

## Events

### PropertyChanged

Event triggered when a property value changes.

```csharp
public event PropertyChangedEventHandler PropertyChanged;
```
 
# OutputFormat

Namespace: miCompressor.core

Represents the supported output formats for image compression.
 Stores both format name and file extension.

```csharp
public enum OutputFormat
```

Inheritance: Object → ValueType → Enum → OutputFormat<br>
Implements: IComparable, ISpanFormattable, IFormattable, IConvertible

## Fields

| Name | Value | Description |
| --- | --: | --- |
| KeepSame | 0 | Keep the original format when possible. If output format is not supported, default format will be used (Advanced Settings). |
| Jpg | 1 | JPEG format (.jpg, .jpeg, .JPG, .JPEG) |
| Png | 2 | PNG format (.png, .PNG) |
| Tiff | 3 | TIFF format (.tiff, .tif, .TIFF, .TIF) |
| Webp | 4 | WebP format (.webp, .WEBP) |
 
# OutputFormatExtensions

Namespace: miCompressor.core

Provides extension methods for OutputFormat enumeration.

```csharp
public static class OutputFormatExtensions
```

Inheritance: Object → OutputFormatExtensions<br>
Attributes: NullableContextAttribute, NullableAttribute, ExtensionAttribute

## Methods

### GetOutputExtension(OutputFormat, String)

Retrieves the corresponding file extension for a given output format,
 preserving the original file's extension case if it matches.

```csharp
public static string GetOutputExtension(OutputFormat format, string originalFilePath)
```

#### Parameters

`format` OutputFormat<br>
The desired output format.

`originalFilePath` String<br>
The original file's full path.

#### Returns

String<br>
The correct file extension preserving case and format.

### IsJpeg(String)

```csharp
private static bool IsJpeg(string ext)
```

#### Parameters

`ext` String<br>

#### Returns

Boolean<br>

### IsPng(String)

```csharp
private static bool IsPng(string ext)
```

#### Parameters

`ext` String<br>

#### Returns

Boolean<br>

### IsTiff(String)

```csharp
private static bool IsTiff(string ext)
```

#### Parameters

`ext` String<br>

#### Returns

Boolean<br>

### IsWebp(String)

```csharp
private static bool IsWebp(string ext)
```

#### Parameters

`ext` String<br>

#### Returns

Boolean<br>
 
# OutputLocationSettings

Namespace: miCompressor.core

Strategy to save compressed images to.

```csharp
public enum OutputLocationSettings
```

Inheritance: Object → ValueType → Enum → OutputLocationSettings<br>
Implements: IComparable, ISpanFormattable, IFormattable, IConvertible

## Fields

| Name | Value | Description |
| --- | --: | --- |
| InCompressedFolder | 0 | A 'Compressed' folder is created inside the selected directory or beside the selected file. |
| SameFolderWithFileNameSuffix | 1 | New file is created beside the original file with a suffix in the name. i.e. if the original file was file.jpg, this strategy make another file in the same directy called file@3x.jpg if suffix specified was '@3x'. |
| ReplaceOriginal | 2 | Replace original file, results in data loss as orignal files will be replaced with compressed files. |
| UserSpecificFolder | 3 | All selected items will be compressed and stored in user specified directory while maintaining the folder structure of original files (i.e. relative to the selected path). e.g. if selected path "def" is "C:/abc/def" folder and output folder is "C:/output" then "C:/abc/def/xyz/lmn.jpg" will be stored as C:/output/def/xyz/lmn.jpg". |
 
# OutputSettings

Namespace: miCompressor.core

Stores output settings including compression information and others.

```csharp
public class OutputSettings : System.ComponentModel.INotifyPropertyChanged
```

Inheritance: Object → OutputSettings<br>
Implements: INotifyPropertyChanged<br>
Attributes: NullableContextAttribute, NullableAttribute

## Fields

### quality

Output Quality between 0 to 100.

```csharp
[AutoNotify] public int quality = 70;
```

### format

Output Image Format. Defaults to JPEG if the output image format is not supported.

```csharp
[AutoNotify] public OutputFormat format = OutputFormat.Jpg;
```

### dimensionStrategy

Dimension reduction strategy for reducing image size.

```csharp
[AutoNotify] public DimensionReductionStrategy dimensionStrategy;
```

### percentageOfLongEdge

Percentage of original image width (or heigh, doesn't matter). If this is set as 50, then image height and width both are reduced by 50%, hence making image four time smaller than original when total number of pixels are considered.

```csharp
[AutoNotify] public decimal percentageOfLongEdge = 100m;
```

### primaryEdgeLength

When reducing image by specifying long edge, height or width, this server as its value.

```csharp
[AutoNotify] public int primaryEdgeLength = 1920;
```

### copyMetadata

Copy EXIF data to output image or not.

```csharp
[AutoNotify] public bool copyMetadata = true;
```

### outputLocationSettings

Output Location stragegy

```csharp
[AutoNotify] public OutputLocationSettings outputLocationSettings = OutputLocationSettings.UserSpecificFolder;
```

### outputFolder

Path of user specified output folder.

```csharp
[AutoNotify] public string outputFolder = "";
```

### suffix

File name suffix of output (i.e. compressed) file.

```csharp
[AutoNotify] public string suffix = "";
```

### prefix

File name prefix of output (i.e. compressed) file.

```csharp
[AutoNotify] public string prefix = "";
```

### replaceFrom

This specified text from original file name will be replaced to what's specified in <![CDATA[ReplaceTo]]> in output file name.

```csharp
[AutoNotify] public string replaceFrom = "";
```

### replaceTo

Text specified in <![CDATA[ReplaceFrom]]>, if found in name of original file, will be replaced with this text in output file name.

```csharp
[AutoNotify] public string replaceTo = "";
```

### SettingsKey

Key to store settings in local settings.

```csharp
private static string SettingsKey = "SavedOutputSettings";
```

## Properties

### PrintDimension

Print dimension for printing. This is used when dimension strategy is set to FitInFrame or FixedInFrame.
 This value is only considered for display purpose and converted and setting primaryEdgeLength.

```csharp
public PrintDimension PrintDimension { get; set; }
```

### Quality

```csharp
public int Quality { get; set; }
```

### Format

```csharp
public OutputFormat Format { get; set; }
```

### DimensionStrategy

```csharp
public DimensionReductionStrategy DimensionStrategy { get; set; }
```

### PercentageOfLongEdge

```csharp
public decimal PercentageOfLongEdge { get; set; }
```

### PrimaryEdgeLength

```csharp
public int PrimaryEdgeLength { get; set; }
```

### CopyMetadata

```csharp
public bool CopyMetadata { get; set; }
```

### OutputLocationSettings

```csharp
public OutputLocationSettings OutputLocationSettings { get; set; }
```

### OutputFolder

```csharp
public string OutputFolder { get; set; }
```

### Suffix

```csharp
public string Suffix { get; set; }
```

### Prefix

```csharp
public string Prefix { get; set; }
```

### ReplaceFrom

```csharp
public string ReplaceFrom { get; set; }
```

### ReplaceTo

```csharp
public string ReplaceTo { get; set; }
```

## Constructors

### OutputSettings()

```csharp
public OutputSettings()
```

## Methods

### saveForFuture()

Save the current instance to local settings.

```csharp
public void saveForFuture()
```

### restoreFromLastSaved()

Restore the last saved instance from local settings.

```csharp
public void restoreFromLastSaved()
```

### CopyFrom(OutputSettings)

Copies properties from another instance.
 We do not copy outputFolder, suffix, prefix, replaceFrom and replaceTo as that may cause unexpected saving of files in wrong location.

```csharp
private void CopyFrom(OutputSettings other)
```

#### Parameters

`other` OutputSettings<br>

### OnPropertyChanged(String)

```csharp
protected void OnPropertyChanged(string propertyName)
```

#### Parameters

`propertyName` String<br>

## Events

### PropertyChanged

```csharp
public event PropertyChangedEventHandler PropertyChanged;
```
 
# PathAddedResult

Namespace: miCompressor.core

Represents the result of attempting to add a path.

```csharp
public enum PathAddedResult
```

Inheritance: Object → ValueType → Enum → PathAddedResult<br>
Implements: IComparable, ISpanFormattable, IFormattable, IConvertible

## Fields

| Name | Value | Description |
| --- | --: | --- |
| Success | 0 |  |
| AlreadyExists | 1 |  |
| InvalidPath | 2 |  |
 
# PathHelper

Namespace: miCompressor.core

```csharp
public static class PathHelper
```

Inheritance: Object → PathHelper

## Methods

### ConvertToLongPath(String)

Converts a standard Windows path to a long path format if needed.
 - Adds `\\?\` prefix only if:
 1. The path is longer than 260 characters.
 2. It is not already in long path format (`\\?\` or `\\?\UNC\`).
 - Works for both local and UNC paths.

```csharp
public static string ConvertToLongPath(string path)
```

#### Parameters

`path` String<br>
The original file or directory path.

#### Returns

String<br>
The path converted to a long path format if required.
 
# PrintDimension

Namespace: miCompressor.core

Represents a standard print dimension with a common name, long edge, short edge, and optional margin.
 Provides methods to retrieve common print dimensions and lookup names based on dimensions.

```csharp
public class PrintDimension : System.ComponentModel.INotifyPropertyChanged
```

Inheritance: Object → PrintDimension<br>
Implements: INotifyPropertyChanged<br>
Attributes: NullableContextAttribute, NullableAttribute

## Fields

### commonName

The friendly name of the print size (e.g., "4R - Postcard Size").

```csharp
[AutoNotify] public string commonName;
```

### longEdgeInInch

The long edge of the print in inches. 
 Reduce margin from this to get the actual image size and multiply by 300 to get the pixel value as 300 DPI is considered for FitInFrame and FixedInFrame.

```csharp
[AutoNotify] public decimal longEdgeInInch;
```

### shortEdgeInInch

The short edge of the print in inches. Like long edge, reduce margin from this to get the actual image size and multiply by 300 to get the pixel value as 300 DPI is considered for FitInFrame and FixedInFrame.

```csharp
[AutoNotify] public decimal shortEdgeInInch;
```

### margin

Margin in inch to be left in the image when fitting/fixing in a frame. This is used when dimension strategy is set to FitInFrame and FixedInFrame.
 The margin is in inch and is applied to all four sides of the image. 
 Multiply by 300 to get the pixel value as 300 DPI is considered for FitInFrame and FixedInFrame.

```csharp
[AutoNotify] public decimal margin;
```

## Properties

### CommonName

```csharp
public string CommonName { get; set; }
```

### LongEdgeInInch

```csharp
public decimal LongEdgeInInch { get; set; }
```

### ShortEdgeInInch

```csharp
public decimal ShortEdgeInInch { get; set; }
```

### Margin

```csharp
public decimal Margin { get; set; }
```

## Constructors

### PrintDimension(String, Decimal, Decimal, Decimal)

Initializes a new instance of the PrintDimension class with a specified common name.

```csharp
public PrintDimension(string commonName, decimal longEdge, decimal shortEdge, decimal margin)
```

#### Parameters

`commonName` String<br>
The friendly name of the print size.

`longEdge` Decimal<br>
The longer edge of the print in inches.

`shortEdge` Decimal<br>
The shorter edge of the print in inches.

`margin` Decimal<br>
Optional margin size in inches (default is 0.25").

### PrintDimension(Decimal, Decimal, Decimal)

Initializes a new instance of the PrintDimension class without specifying a name.
 The common name is automatically determined based on dimensions.

```csharp
public PrintDimension(decimal longEdge, decimal shortEdge, decimal margin)
```

#### Parameters

`longEdge` Decimal<br>
The longer edge of the print in inches.

`shortEdge` Decimal<br>
The shorter edge of the print in inches.

`margin` Decimal<br>
Optional margin size in inches (default is 0.25").

## Methods

### GetCommonPrintDimensions()

Retrieves a list of common print dimensions with their respective sizes and margins.

```csharp
public static PrintDimension[] GetCommonPrintDimensions()
```

#### Returns

PrintDimension[]<br>

### GetNameFromDimensions(Decimal, Decimal)

Retrieves the common print size name based on given dimensions.

```csharp
public static string GetNameFromDimensions(decimal longEdge, decimal shortEdge)
```

#### Parameters

`longEdge` Decimal<br>
The longer edge of the print.

`shortEdge` Decimal<br>
The shorter edge of the print.

#### Returns

String<br>
The corresponding common name if found, otherwise "Unknown Size".

### OnPropertyChanged(String)

```csharp
protected void OnPropertyChanged(string propertyName)
```

#### Parameters

`propertyName` String<br>

## Events

### PropertyChanged

```csharp
public event PropertyChangedEventHandler PropertyChanged;
```
 
# ReaderWriterLockSlimExtensions

Namespace: miCompressor.core

```csharp
public static class ReaderWriterLockSlimExtensions
```

Inheritance: Object → ReaderWriterLockSlimExtensions<br>
Attributes: ExtensionAttribute

## Methods

### ReadLock(ReaderWriterLockSlim)

```csharp
public static IDisposable ReadLock(ReaderWriterLockSlim rwLock)
```

#### Parameters

`rwLock` ReaderWriterLockSlim<br>

#### Returns

IDisposable<br>

### WriteLock(ReaderWriterLockSlim)

```csharp
public static IDisposable WriteLock(ReaderWriterLockSlim rwLock)
```

#### Parameters

`rwLock` ReaderWriterLockSlim<br>

#### Returns

IDisposable<br>
 
# SelectedPath

Namespace: miCompressor.core

Represents a selected file or directory path that can be scanned for media files.

```csharp
public class SelectedPath : ObservableBase, System.ComponentModel.INotifyPropertyChanged
```

Inheritance: Object → ObservableBase → SelectedPath<br>
Implements: INotifyPropertyChanged<br>
Attributes: NullableContextAttribute, NullableAttribute, WinRTRuntimeClassNameAttribute, WinRTExposedTypeAttribute

## Fields

### scanningForFiles

Indicates whether the directory is currently being scanned.
 Automatically notifies UI when changed.

```csharp
[AutoNotify] private bool scanningForFiles;
```

### _cancellationTokenSource

```csharp
private CancellationTokenSource _cancellationTokenSource;
```

### _files

```csharp
private List<MediaFileInfo> _files;
```

### _lockForFiles

```csharp
private ReaderWriterLockSlim _lockForFiles;
```

### _lockForScannerThread

```csharp
private object _lockForScannerThread;
```

## Properties

### Path

The absolute path of the selected file or directory.

```csharp
public string Path { get; }
```

### DisplayName

Returns the file name if the path is a file, otherwise returns the directory name.

```csharp
public string DisplayName { get; }
```

### IsDirectory

Indicates whether the selected path is a directory.

```csharp
public bool IsDirectory { get; }
```

### IncludeSubDirectories

Determines whether subdirectories should be included in the scan.
 Changing this property triggers a new scan.

```csharp
public bool IncludeSubDirectories { get; set; }
```

### Files

A read-only list of media files found in the selected path.

```csharp
public IReadOnlyList<MediaFileInfo> Files { get; }
```

### ScanningForFiles

```csharp
public bool ScanningForFiles { get; set; }
```

## Constructors

### SelectedPath(String, Boolean)

Initializes a new instance of the SelectedPath class.

```csharp
public SelectedPath(string path, bool includeSubDirectories)
```

#### Parameters

`path` String<br>
The file or directory path.

`includeSubDirectories` Boolean<br>
Specifies whether to scan subdirectories.

## Methods

### ChangeToIncludeSubDirectories(Boolean)

Changes the settings to include or exclude sub-directories. Make sure UI looks for `ScanningForFiles` and disable the toggle control to avoid inconsistency.

```csharp
public void ChangeToIncludeSubDirectories(bool includeSubDirectories)
```

#### Parameters

`includeSubDirectories` Boolean<br>

### CancelScanning()

Cancels the ongoing file scanning operation, it may take some time to cancel the operation.
 Should be called before changing the include subdirectories setting and before removing the path from the store.

```csharp
public void CancelScanning()
```

### Cleanup()

```csharp
public void Cleanup()
```

### ScanForMediaFiles()

Scans the directory asynchronously and updates the file list.
 Only files with supported extensions are included.

```csharp
private Task ScanForMediaFiles()
```

#### Returns

Task<br>

### PopulateAllFilesForSupportedExtension(HashSet&lt;String&gt;, String, Boolean, CancellationToken)

Recursively scans the given `rootFolderPath` and populates files in batches.
 Supports cancellation through `CancellationToken`.

```csharp
private void PopulateAllFilesForSupportedExtension(HashSet<string> supportedInputExtensions, string rootFolderPath, bool includeSubDir, CancellationToken cancellationToken)
```

#### Parameters

`supportedInputExtensions` HashSet&lt;String&gt;<br>

`rootFolderPath` String<br>
Selected Path by user to search the images within

`includeSubDir` Boolean<br>
Should search directories within `rootFolderPath` or not

`cancellationToken` CancellationToken<br>
Token to cancel the ongoing operation

### OnPropertyChanged(String)

```csharp
protected void OnPropertyChanged(string propertyName)
```

#### Parameters

`propertyName` String<br>

### &lt;ChangeToIncludeSubDirectories&gt;b__19_0()

```csharp
private Task <ChangeToIncludeSubDirectories>b__19_0()
```

#### Returns

Task<br>

## Events

### PropertyChanged

```csharp
public event PropertyChangedEventHandler PropertyChanged;
```
 
# TempDataManager

Namespace: miCompressor.core

Helper class for managing temporary data.

```csharp
public static class TempDataManager
```

Inheritance: Object → TempDataManager<br>
Attributes: NullableContextAttribute, NullableAttribute

## Fields

### _tempDirOpLock

Lock object for operations on temp directory.

```csharp
private static object _tempDirOpLock = System.Object;
```

### staleIfOlderThanHours

Files older than this time are considered stale and are deleted when cleaning up temp directory.

```csharp
private static double staleIfOlderThanHours = 2d;
```

### preivewDirName

Name of the directory where preview files are stored.

```csharp
private static string preivewDirName = "preview";
```

### cacheDirName

Name of the directory where compressed files are stored temporarily to be moved to designated location later if needed.

```csharp
private static string cacheDirName = "cache";
```

## Properties

### tempAppDir

Temporary directory of the application. All temporary files are stored here.
 This only returns the path, does not guarantee the directory exists.

```csharp
public static string tempAppDir { get; }
```

## Constructors

### TempDataManager()

```csharp
private static TempDataManager()
```

## Methods

### getTempPreviewFilePath(String)

Get the path of the compressed preview file. Caller is responsible for creating the directory if it doesn't exist.

```csharp
public static string getTempPreviewFilePath(string fileName)
```

#### Parameters

`fileName` String<br>

#### Returns

String<br>

### getTempPreviewDirPath()

Get the path of the compressed preview directory. Caller is responsible for creating the directory if it doesn't exist.

```csharp
public static string getTempPreviewDirPath()
```

#### Returns

String<br>
Preview directory path

### GetTempStorageFilePath(String, String)

Get the temp storage file path for storing compressed files temporarily to be moved to designated location later if applicable.
 Caller is responsible for creating the directory if it doesn't exist.

```csharp
public static string GetTempStorageFilePath(string dirPath, string fileName)
```

#### Parameters

`dirPath` String<br>
Relative directory from selected directory

`fileName` String<br>
Name of the output file

#### Returns

String<br>

### GetTempStorageDirPath(String)

Get the temp storage directory path for storing compressed files temporarily to be moved to designated location later if applicable.

```csharp
public static string GetTempStorageDirPath(string dirPath)
```

#### Parameters

`dirPath` String<br>
May provide dir or file path. If file path is provided, we still return dir, not file.

#### Returns

String<br>

### CleanUpTempDir()

Clean up temp directory, should be called when compression is done, or when application starts.
 Non-blocking operation.
 Ensure there is no other operation using temp directory before calling this.

```csharp
public static void CleanUpTempDir()
```

### CleanDirectory(String)

Clean up the directory by deleting stale files in the given directory and its subdirectories.

```csharp
private static void CleanDirectory(string dirPath)
```

#### Parameters

`dirPath` String<br>
 
# UIThreadHelper

Namespace: miCompressor.core

Provides a globally accessible reference to the UI thread's .
 This allows ViewModels and background tasks to dispatch UI updates safely.

```csharp
public static class UIThreadHelper
```

Inheritance: Object → UIThreadHelper<br>
Attributes: NullableContextAttribute, NullableAttribute

## Properties

### UIThreadDispatcherQueue

Stores the UI thread's DispatcherQueue for cross-thread UI updates.
 This must be initialized on the UI thread.

```csharp
public static DispatcherQueue UIThreadDispatcherQueue { get; private set; }
```

## Methods

### Initialize()

Initializes the UI dispatcher queue. 
 This must be called **only from the UI thread**, preferably in App.xaml.cs or MainWindow.xaml.cs.

```csharp
public static void Initialize()
```

### RunOnUIThread(Action)

Runs an action on the UI thread. If already on the UI thread, the action runs immediately.

```csharp
public static void RunOnUIThread(Action action)
```

#### Parameters

`action` Action<br>

#### Example

```csharp
dispatcher.TryEnqueue(() =>
{
    // Execute UI-related code here.
    // Example: Updating UI elements from a background thread.
    myTextBox.Text = "Updated from UI thread.";
});
```

### RunOnUIThreadAsync(Func&lt;Task&gt;)

Runs the provided async action on the UI thread. If already on the UI thread, it executes immediately.

```csharp
public static Task RunOnUIThreadAsync(Func<Task> asyncAction)
```

#### Parameters

`asyncAction` Func&lt;Task&gt;<br>

#### Returns

Task<br>
 
# MainWindow

Namespace: miCompressor

An empty window that can be used on its own or navigated to within a Frame.

```csharp
public sealed class MainWindow : Microsoft.UI.Xaml.Window, System.Runtime.InteropServices.ICustomQueryInterface, WinRT.IWinRTObject, System.Runtime.InteropServices.IDynamicInterfaceCastable, System.Runtime.InteropServices.Marshalling.IUnmanagedVirtualMethodTableProvider, System.IEquatable`1[[Microsoft.UI.Xaml.Window, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Markup.IComponentConnector
```

Inheritance: Object → Window → MainWindow<br>
Implements: ICustomQueryInterface, IWinRTObject, IDynamicInterfaceCastable, IUnmanagedVirtualMethodTableProvider, IEquatable&lt;Window&gt;, IComponentConnector<br>
Attributes: WinRTRuntimeClassNameAttribute, WinRTExposedTypeAttribute, WindowRcwFactoryAttribute, ContentPropertyAttribute, ContractVersionAttribute

## Fields

### _contentLoaded

```csharp
private bool _contentLoaded;
```

## Constructors

### MainWindow()

```csharp
public MainWindow()
```

## Methods

### InitializeComponent()

InitializeComponent()

```csharp
public void InitializeComponent()
```

### Connect(Int32, Object)

Connect()

```csharp
public void Connect(int connectionId, object target)
```

#### Parameters

`connectionId` Int32<br>

`target` Object<br>

### GetBindingConnector(Int32, Object)

GetBindingConnector(int connectionId, object target)

```csharp
public IComponentConnector GetBindingConnector(int connectionId, object target)
```

#### Parameters

`connectionId` Int32<br>

`target` Object<br>

#### Returns

IComponentConnector<br>
 
# XamlMetaDataProvider

Namespace: miCompressor.miCompressor_XamlTypeInfo

Main class for providing metadata for the app or library

```csharp
public sealed class XamlMetaDataProvider : Microsoft.UI.Xaml.Markup.IXamlMetadataProvider
```

Inheritance: Object → XamlMetaDataProvider<br>
Implements: IXamlMetadataProvider<br>
Attributes: GeneratedCodeAttribute, DebuggerNonUserCodeAttribute, WinRTRuntimeClassNameAttribute, WinRTExposedTypeAttribute

## Fields

### _provider

```csharp
private XamlTypeInfoProvider _provider;
```

## Properties

### Provider

```csharp
private XamlTypeInfoProvider Provider { get; }
```

## Constructors

### XamlMetaDataProvider()

```csharp
public XamlMetaDataProvider()
```

## Methods

### GetXamlType(Type)

GetXamlType(Type)

```csharp
public IXamlType GetXamlType(Type type)
```

#### Parameters

`type` Type<br>

#### Returns

IXamlType<br>

### GetXamlType(String)

GetXamlType(String)

```csharp
public IXamlType GetXamlType(string fullName)
```

#### Parameters

`fullName` String<br>

#### Returns

IXamlType<br>

### GetXmlnsDefinitions()

GetXmlnsDefinitions()

```csharp
public XmlnsDefinition[] GetXmlnsDefinitions()
```

#### Returns

XmlnsDefinition[]<br>
 
# Program

Namespace: miCompressor

Program class

```csharp
public static class Program
```

Inheritance: Object → Program

## Methods

### Main(String[])

```csharp
private static void Main(String[] args)
```

#### Parameters

`args` String[]<br>
 
# BlendedBrushes

Namespace: miCompressor.ui

```csharp
public class BlendedBrushes
```

Inheritance: Object → BlendedBrushes<br>
Attributes: NullableContextAttribute, NullableAttribute

## Properties

### ThemedRed

```csharp
public SolidColorBrush ThemedRed { get; }
```

## Constructors

### BlendedBrushes()

```csharp
public BlendedBrushes()
```

## Methods

### CreateBlendedBrush(Color, Color, Double)

```csharp
private static SolidColorBrush CreateBlendedBrush(Color baseColor, Color overlayColor, double overlayOpacity)
```

#### Parameters

`baseColor` Color<br>

`overlayColor` Color<br>

`overlayOpacity` Double<br>

#### Returns

SolidColorBrush<br>

### GetSystemAccentColor()

```csharp
private static Color GetSystemAccentColor()
```

#### Returns

Color<br>

### GetSystemPrimaryColor()

```csharp
private static Color GetSystemPrimaryColor()
```

#### Returns

Color<br>
 
# EmptyFilesView

Namespace: miCompressor.ui

An empty page that can be used on its own or navigated to within a Frame.

```csharp
public sealed class EmptyFilesView : Microsoft.UI.Xaml.Controls.UserControl, System.Runtime.InteropServices.ICustomQueryInterface, WinRT.IWinRTObject, System.Runtime.InteropServices.IDynamicInterfaceCastable, System.Runtime.InteropServices.Marshalling.IUnmanagedVirtualMethodTableProvider, System.IEquatable`1[[Microsoft.UI.Xaml.DependencyObject, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.IUIElementOverrides, Microsoft.UI.Composition.IAnimationObject, Microsoft.UI.Composition.IVisualElement, Microsoft.UI.Composition.IVisualElement2, System.IEquatable`1[[Microsoft.UI.Xaml.UIElement, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.IFrameworkElementOverrides, System.IEquatable`1[[Microsoft.UI.Xaml.FrameworkElement, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Controls.IControlOverrides, System.IEquatable`1[[Microsoft.UI.Xaml.Controls.Control, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], System.IEquatable`1[[Microsoft.UI.Xaml.Controls.UserControl, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Markup.IComponentConnector
```

Inheritance: Object → DependencyObject → UIElement → FrameworkElement → Control → UserControl → EmptyFilesView<br>
Implements: ICustomQueryInterface, IWinRTObject, IDynamicInterfaceCastable, IUnmanagedVirtualMethodTableProvider, IEquatable&lt;DependencyObject&gt;, IUIElementOverrides, IAnimationObject, IVisualElement, IVisualElement2, IEquatable&lt;UIElement&gt;, IFrameworkElementOverrides, IEquatable&lt;FrameworkElement&gt;, IControlOverrides, IEquatable&lt;Control&gt;, IEquatable&lt;UserControl&gt;, IComponentConnector<br>
Attributes: WinRTRuntimeClassNameAttribute, WinRTExposedTypeAttribute, UserControlRcwFactoryAttribute, ContentPropertyAttribute, ContractVersionAttribute, ControlRcwFactoryAttribute, ContractVersionAttribute, FrameworkElementRcwFactoryAttribute, ContractVersionAttribute, UIElementRcwFactoryAttribute, ContractVersionAttribute, DependencyObjectRcwFactoryAttribute, ContractVersionAttribute

## Fields

### _contentLoaded

```csharp
private bool _contentLoaded;
```

### Bindings

```csharp
private IEmptyFilesView_Bindings Bindings;
```

## Properties

### SupportedExtensionsInCaps

Supported extensions to show in UI.

```csharp
protected string SupportedExtensionsInCaps { get; }
```

## Constructors

### EmptyFilesView()

```csharp
public EmptyFilesView()
```

## Methods

### InitializeComponent()

InitializeComponent()

```csharp
public void InitializeComponent()
```

### Connect(Int32, Object)

Connect()

```csharp
public void Connect(int connectionId, object target)
```

#### Parameters

`connectionId` Int32<br>

`target` Object<br>

### GetBindingConnector(Int32, Object)

GetBindingConnector(int connectionId, object target)

```csharp
public IComponentConnector GetBindingConnector(int connectionId, object target)
```

#### Parameters

`connectionId` Int32<br>

`target` Object<br>

#### Returns

IComponentConnector<br>
 
# ErrorAndWarnings

Namespace: miCompressor.ui

An empty page that can be used on its own or navigated to within a Frame.

```csharp
public sealed class ErrorAndWarnings : Microsoft.UI.Xaml.Controls.Page, System.Runtime.InteropServices.ICustomQueryInterface, WinRT.IWinRTObject, System.Runtime.InteropServices.IDynamicInterfaceCastable, System.Runtime.InteropServices.Marshalling.IUnmanagedVirtualMethodTableProvider, System.IEquatable`1[[Microsoft.UI.Xaml.DependencyObject, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.IUIElementOverrides, Microsoft.UI.Composition.IAnimationObject, Microsoft.UI.Composition.IVisualElement, Microsoft.UI.Composition.IVisualElement2, System.IEquatable`1[[Microsoft.UI.Xaml.UIElement, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.IFrameworkElementOverrides, System.IEquatable`1[[Microsoft.UI.Xaml.FrameworkElement, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Controls.IControlOverrides, System.IEquatable`1[[Microsoft.UI.Xaml.Controls.Control, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], System.IEquatable`1[[Microsoft.UI.Xaml.Controls.UserControl, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Controls.IPageOverrides, System.IEquatable`1[[Microsoft.UI.Xaml.Controls.Page, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Markup.IComponentConnector
```

Inheritance: Object → DependencyObject → UIElement → FrameworkElement → Control → UserControl → Page → ErrorAndWarnings<br>
Implements: ICustomQueryInterface, IWinRTObject, IDynamicInterfaceCastable, IUnmanagedVirtualMethodTableProvider, IEquatable&lt;DependencyObject&gt;, IUIElementOverrides, IAnimationObject, IVisualElement, IVisualElement2, IEquatable&lt;UIElement&gt;, IFrameworkElementOverrides, IEquatable&lt;FrameworkElement&gt;, IControlOverrides, IEquatable&lt;Control&gt;, IEquatable&lt;UserControl&gt;, IPageOverrides, IEquatable&lt;Page&gt;, IComponentConnector<br>
Attributes: WinRTRuntimeClassNameAttribute, WinRTExposedTypeAttribute, PageRcwFactoryAttribute, ContractVersionAttribute, UserControlRcwFactoryAttribute, ContentPropertyAttribute, ContractVersionAttribute, ControlRcwFactoryAttribute, ContractVersionAttribute, FrameworkElementRcwFactoryAttribute, ContractVersionAttribute, UIElementRcwFactoryAttribute, ContractVersionAttribute, DependencyObjectRcwFactoryAttribute, ContractVersionAttribute

## Fields

### _contentLoaded

```csharp
private bool _contentLoaded;
```

## Constructors

### ErrorAndWarnings()

```csharp
public ErrorAndWarnings()
```

## Methods

### InitializeComponent()

InitializeComponent()

```csharp
public void InitializeComponent()
```

### Connect(Int32, Object)

Connect()

```csharp
public void Connect(int connectionId, object target)
```

#### Parameters

`connectionId` Int32<br>

`target` Object<br>

### GetBindingConnector(Int32, Object)

GetBindingConnector(int connectionId, object target)

```csharp
public IComponentConnector GetBindingConnector(int connectionId, object target)
```

#### Parameters

`connectionId` Int32<br>

`target` Object<br>

#### Returns

IComponentConnector<br>
 
# FileSelectionView

Namespace: miCompressor.ui

An empty page that can be used on its own or navigated to within a Frame.

```csharp
public sealed class FileSelectionView : Microsoft.UI.Xaml.Controls.UserControl, System.Runtime.InteropServices.ICustomQueryInterface, WinRT.IWinRTObject, System.Runtime.InteropServices.IDynamicInterfaceCastable, System.Runtime.InteropServices.Marshalling.IUnmanagedVirtualMethodTableProvider, System.IEquatable`1[[Microsoft.UI.Xaml.DependencyObject, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.IUIElementOverrides, Microsoft.UI.Composition.IAnimationObject, Microsoft.UI.Composition.IVisualElement, Microsoft.UI.Composition.IVisualElement2, System.IEquatable`1[[Microsoft.UI.Xaml.UIElement, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.IFrameworkElementOverrides, System.IEquatable`1[[Microsoft.UI.Xaml.FrameworkElement, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Controls.IControlOverrides, System.IEquatable`1[[Microsoft.UI.Xaml.Controls.Control, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], System.IEquatable`1[[Microsoft.UI.Xaml.Controls.UserControl, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Markup.IComponentConnector
```

Inheritance: Object → DependencyObject → UIElement → FrameworkElement → Control → UserControl → FileSelectionView<br>
Implements: ICustomQueryInterface, IWinRTObject, IDynamicInterfaceCastable, IUnmanagedVirtualMethodTableProvider, IEquatable&lt;DependencyObject&gt;, IUIElementOverrides, IAnimationObject, IVisualElement, IVisualElement2, IEquatable&lt;UIElement&gt;, IFrameworkElementOverrides, IEquatable&lt;FrameworkElement&gt;, IControlOverrides, IEquatable&lt;Control&gt;, IEquatable&lt;UserControl&gt;, IComponentConnector<br>
Attributes: WinRTRuntimeClassNameAttribute, WinRTExposedTypeAttribute, UserControlRcwFactoryAttribute, ContentPropertyAttribute, ContractVersionAttribute, ControlRcwFactoryAttribute, ContractVersionAttribute, FrameworkElementRcwFactoryAttribute, ContractVersionAttribute, UIElementRcwFactoryAttribute, ContractVersionAttribute, DependencyObjectRcwFactoryAttribute, ContractVersionAttribute

## Fields

### _contentLoaded

```csharp
private bool _contentLoaded;
```

## Constructors

### FileSelectionView()

```csharp
public FileSelectionView()
```

## Methods

### SelectedItem_SelectedPathDeleted(Object, SelectedPath)

```csharp
private void SelectedItem_SelectedPathDeleted(object sender, SelectedPath e)
```

#### Parameters

`sender` Object<br>

`e` SelectedPath<br>

### InitializeComponent()

InitializeComponent()

```csharp
public void InitializeComponent()
```

### Connect(Int32, Object)

Connect()

```csharp
public void Connect(int connectionId, object target)
```

#### Parameters

`connectionId` Int32<br>

`target` Object<br>

### GetBindingConnector(Int32, Object)

GetBindingConnector(int connectionId, object target)

```csharp
public IComponentConnector GetBindingConnector(int connectionId, object target)
```

#### Parameters

`connectionId` Int32<br>

`target` Object<br>

#### Returns

IComponentConnector<br>
 
# MasterView

Namespace: miCompressor.ui

An empty page that can be used on its own or navigated to within it.

```csharp
public sealed class MasterView : Microsoft.UI.Xaml.Controls.Page, System.Runtime.InteropServices.ICustomQueryInterface, WinRT.IWinRTObject, System.Runtime.InteropServices.IDynamicInterfaceCastable, System.Runtime.InteropServices.Marshalling.IUnmanagedVirtualMethodTableProvider, System.IEquatable`1[[Microsoft.UI.Xaml.DependencyObject, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.IUIElementOverrides, Microsoft.UI.Composition.IAnimationObject, Microsoft.UI.Composition.IVisualElement, Microsoft.UI.Composition.IVisualElement2, System.IEquatable`1[[Microsoft.UI.Xaml.UIElement, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.IFrameworkElementOverrides, System.IEquatable`1[[Microsoft.UI.Xaml.FrameworkElement, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Controls.IControlOverrides, System.IEquatable`1[[Microsoft.UI.Xaml.Controls.Control, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], System.IEquatable`1[[Microsoft.UI.Xaml.Controls.UserControl, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Controls.IPageOverrides, System.IEquatable`1[[Microsoft.UI.Xaml.Controls.Page, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Markup.IComponentConnector, System.ComponentModel.INotifyPropertyChanged
```

Inheritance: Object → DependencyObject → UIElement → FrameworkElement → Control → UserControl → Page → MasterView<br>
Implements: ICustomQueryInterface, IWinRTObject, IDynamicInterfaceCastable, IUnmanagedVirtualMethodTableProvider, IEquatable&lt;DependencyObject&gt;, IUIElementOverrides, IAnimationObject, IVisualElement, IVisualElement2, IEquatable&lt;UIElement&gt;, IFrameworkElementOverrides, IEquatable&lt;FrameworkElement&gt;, IControlOverrides, IEquatable&lt;Control&gt;, IEquatable&lt;UserControl&gt;, IPageOverrides, IEquatable&lt;Page&gt;, IComponentConnector, INotifyPropertyChanged<br>
Attributes: WinRTRuntimeClassNameAttribute, WinRTExposedTypeAttribute, PageRcwFactoryAttribute, ContractVersionAttribute, UserControlRcwFactoryAttribute, ContentPropertyAttribute, ContractVersionAttribute, ControlRcwFactoryAttribute, ContractVersionAttribute, FrameworkElementRcwFactoryAttribute, ContractVersionAttribute, UIElementRcwFactoryAttribute, ContractVersionAttribute, DependencyObjectRcwFactoryAttribute, ContractVersionAttribute

## Fields

### isEmptyViewVisible

```csharp
[AutoNotify] private bool isEmptyViewVisible;
```

### FileSelectionPage

```csharp
private EmptyFilesView FileSelectionPage;
```

### WarningBanner

```csharp
private Border WarningBanner;
```

### WarningText

```csharp
private TextBlock WarningText;
```

### _contentLoaded

```csharp
private bool _contentLoaded;
```

### Bindings

```csharp
private IMasterView_Bindings Bindings;
```

## Properties

### masterState

```csharp
private MasterState masterState { get; }
```

### FileStore

Comes from Static variable of App

```csharp
public FileStore FileStore { get; }
```

### IsFileSelectionViewVisible

```csharp
public bool IsFileSelectionViewVisible { get; }
```

### IsEmptyViewVisible

```csharp
public bool IsEmptyViewVisible { get; set; }
```

## Constructors

### MasterView()

```csharp
public MasterView()
```

## Methods

### masterState_PropertyChanged(Object, PropertyChangedEventArgs)

```csharp
private void masterState_PropertyChanged(object sender, PropertyChangedEventArgs e)
```

#### Parameters

`sender` Object<br>

`e` PropertyChangedEventArgs<br>

### MasterView_DragOver(Object, DragEventArgs)

```csharp
private void MasterView_DragOver(object sender, DragEventArgs e)
```

#### Parameters

`sender` Object<br>

`e` DragEventArgs<br>

### MasterView_Drop(Object, DragEventArgs)

```csharp
private void MasterView_Drop(object sender, DragEventArgs e)
```

#### Parameters

`sender` Object<br>

`e` DragEventArgs<br>

### ShowWarning(String)

```csharp
private void ShowWarning(string message)
```

#### Parameters

`message` String<br>

### InitializeComponent()

InitializeComponent()

```csharp
public void InitializeComponent()
```

### Connect(Int32, Object)

Connect()

```csharp
public void Connect(int connectionId, object target)
```

#### Parameters

`connectionId` Int32<br>

`target` Object<br>

### GetBindingConnector(Int32, Object)

GetBindingConnector(int connectionId, object target)

```csharp
public IComponentConnector GetBindingConnector(int connectionId, object target)
```

#### Parameters

`connectionId` Int32<br>

`target` Object<br>

#### Returns

IComponentConnector<br>

### OnPropertyChanged(String)

```csharp
protected void OnPropertyChanged(string propertyName)
```

#### Parameters

`propertyName` String<br>

### &lt;ShowWarning&gt;b__11_0(Task)

```csharp
private void <ShowWarning>b__11_0(Task _)
```

#### Parameters

`_` Task<br>

### &lt;ShowWarning&gt;b__11_1()

```csharp
private void <ShowWarning>b__11_1()
```

## Events

### PropertyChanged

```csharp
public event PropertyChangedEventHandler PropertyChanged;
```
 
# NullToBooleanConverter

Namespace: miCompressor.ui

```csharp
public class NullToBooleanConverter : Microsoft.UI.Xaml.Data.IValueConverter
```

Inheritance: Object → NullToBooleanConverter<br>
Implements: IValueConverter

## Constructors

### NullToBooleanConverter()

```csharp
public NullToBooleanConverter()
```

## Methods

### Convert(Object, Type, Object, String)

```csharp
public object Convert(object value, Type targetType, object parameter, string language)
```

#### Parameters

`value` Object<br>

`targetType` Type<br>

`parameter` Object<br>

`language` String<br>

#### Returns

Object<br>

### ConvertBack(Object, Type, Object, String)

```csharp
public object ConvertBack(object value, Type targetType, object parameter, string language)
```

#### Parameters

`value` Object<br>

`targetType` Type<br>

`parameter` Object<br>

`language` String<br>

#### Returns

Object<br>
 
# QualitySettings

Namespace: miCompressor.ui

An empty page that can be used on its own or navigated to within a Frame.

```csharp
public sealed class QualitySettings : Microsoft.UI.Xaml.Controls.Page, System.Runtime.InteropServices.ICustomQueryInterface, WinRT.IWinRTObject, System.Runtime.InteropServices.IDynamicInterfaceCastable, System.Runtime.InteropServices.Marshalling.IUnmanagedVirtualMethodTableProvider, System.IEquatable`1[[Microsoft.UI.Xaml.DependencyObject, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.IUIElementOverrides, Microsoft.UI.Composition.IAnimationObject, Microsoft.UI.Composition.IVisualElement, Microsoft.UI.Composition.IVisualElement2, System.IEquatable`1[[Microsoft.UI.Xaml.UIElement, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.IFrameworkElementOverrides, System.IEquatable`1[[Microsoft.UI.Xaml.FrameworkElement, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Controls.IControlOverrides, System.IEquatable`1[[Microsoft.UI.Xaml.Controls.Control, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], System.IEquatable`1[[Microsoft.UI.Xaml.Controls.UserControl, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Controls.IPageOverrides, System.IEquatable`1[[Microsoft.UI.Xaml.Controls.Page, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Markup.IComponentConnector
```

Inheritance: Object → DependencyObject → UIElement → FrameworkElement → Control → UserControl → Page → QualitySettings<br>
Implements: ICustomQueryInterface, IWinRTObject, IDynamicInterfaceCastable, IUnmanagedVirtualMethodTableProvider, IEquatable&lt;DependencyObject&gt;, IUIElementOverrides, IAnimationObject, IVisualElement, IVisualElement2, IEquatable&lt;UIElement&gt;, IFrameworkElementOverrides, IEquatable&lt;FrameworkElement&gt;, IControlOverrides, IEquatable&lt;Control&gt;, IEquatable&lt;UserControl&gt;, IPageOverrides, IEquatable&lt;Page&gt;, IComponentConnector<br>
Attributes: WinRTRuntimeClassNameAttribute, WinRTExposedTypeAttribute, PageRcwFactoryAttribute, ContractVersionAttribute, UserControlRcwFactoryAttribute, ContentPropertyAttribute, ContractVersionAttribute, ControlRcwFactoryAttribute, ContractVersionAttribute, FrameworkElementRcwFactoryAttribute, ContractVersionAttribute, UIElementRcwFactoryAttribute, ContractVersionAttribute, DependencyObjectRcwFactoryAttribute, ContractVersionAttribute

## Fields

### _contentLoaded

```csharp
private bool _contentLoaded;
```

## Constructors

### QualitySettings()

```csharp
public QualitySettings()
```

## Methods

### TextBlock_BringIntoViewRequested(UIElement, BringIntoViewRequestedEventArgs)

```csharp
private void TextBlock_BringIntoViewRequested(UIElement sender, BringIntoViewRequestedEventArgs args)
```

#### Parameters

`sender` UIElement<br>

`args` BringIntoViewRequestedEventArgs<br>

### InitializeComponent()

InitializeComponent()

```csharp
public void InitializeComponent()
```

### Connect(Int32, Object)

Connect()

```csharp
public void Connect(int connectionId, object target)
```

#### Parameters

`connectionId` Int32<br>

`target` Object<br>

### GetBindingConnector(Int32, Object)

GetBindingConnector(int connectionId, object target)

```csharp
public IComponentConnector GetBindingConnector(int connectionId, object target)
```

#### Parameters

`connectionId` Int32<br>

`target` Object<br>

#### Returns

IComponentConnector<br>
 
# SelectedItem

Namespace: miCompressor.ui

Shows Selected Item (file or folder), i.e. each SelectedPath in FileStore

```csharp
public sealed class SelectedItem : Microsoft.UI.Xaml.Controls.UserControl, System.Runtime.InteropServices.ICustomQueryInterface, WinRT.IWinRTObject, System.Runtime.InteropServices.IDynamicInterfaceCastable, System.Runtime.InteropServices.Marshalling.IUnmanagedVirtualMethodTableProvider, System.IEquatable`1[[Microsoft.UI.Xaml.DependencyObject, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.IUIElementOverrides, Microsoft.UI.Composition.IAnimationObject, Microsoft.UI.Composition.IVisualElement, Microsoft.UI.Composition.IVisualElement2, System.IEquatable`1[[Microsoft.UI.Xaml.UIElement, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.IFrameworkElementOverrides, System.IEquatable`1[[Microsoft.UI.Xaml.FrameworkElement, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Controls.IControlOverrides, System.IEquatable`1[[Microsoft.UI.Xaml.Controls.Control, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], System.IEquatable`1[[Microsoft.UI.Xaml.Controls.UserControl, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Markup.IComponentConnector
```

Inheritance: Object → DependencyObject → UIElement → FrameworkElement → Control → UserControl → SelectedItem<br>
Implements: ICustomQueryInterface, IWinRTObject, IDynamicInterfaceCastable, IUnmanagedVirtualMethodTableProvider, IEquatable&lt;DependencyObject&gt;, IUIElementOverrides, IAnimationObject, IVisualElement, IVisualElement2, IEquatable&lt;UIElement&gt;, IFrameworkElementOverrides, IEquatable&lt;FrameworkElement&gt;, IControlOverrides, IEquatable&lt;Control&gt;, IEquatable&lt;UserControl&gt;, IComponentConnector<br>
Attributes: WinRTRuntimeClassNameAttribute, WinRTExposedTypeAttribute, UserControlRcwFactoryAttribute, ContentPropertyAttribute, ContractVersionAttribute, ControlRcwFactoryAttribute, ContractVersionAttribute, FrameworkElementRcwFactoryAttribute, ContractVersionAttribute, UIElementRcwFactoryAttribute, ContractVersionAttribute, DependencyObjectRcwFactoryAttribute, ContractVersionAttribute

## Fields

### DetailViewInstance

```csharp
private SelectionDetailView DetailViewInstance;
```

### _contentLoaded

```csharp
private bool _contentLoaded;
```

### Bindings

```csharp
private ISelectedItem_Bindings Bindings;
```

### SelectedPathProperty

Identifies the SelectedItem.SelectedPath dependency property.

```csharp
public static DependencyProperty SelectedPathProperty;
```

## Properties

### SelectedPath

Gets or sets the selected path.

```csharp
public SelectedPath SelectedPath { get; set; }
```

### ScannedAllFiles

```csharp
public bool ScannedAllFiles { get; }
```

## Constructors

### SelectedItem()

```csharp
public SelectedItem()
```

### SelectedItem()

```csharp
private static SelectedItem()
```

## Methods

### OnDeleteButtonClicked(Object, RoutedEventArgs)

```csharp
private void OnDeleteButtonClicked(object sender, RoutedEventArgs e)
```

#### Parameters

`sender` Object<br>

`e` RoutedEventArgs<br>

### InitializeComponent()

InitializeComponent()

```csharp
public void InitializeComponent()
```

### Connect(Int32, Object)

Connect()

```csharp
public void Connect(int connectionId, object target)
```

#### Parameters

`connectionId` Int32<br>

`target` Object<br>

### GetBindingConnector(Int32, Object)

GetBindingConnector(int connectionId, object target)

```csharp
public IComponentConnector GetBindingConnector(int connectionId, object target)
```

#### Parameters

`connectionId` Int32<br>

`target` Object<br>

#### Returns

IComponentConnector<br>

## Events

### SelectedPathDeleted

```csharp
public event EventHandler<SelectedPath> SelectedPathDeleted;
```
 
# SelectionDetailView

Namespace: miCompressor.ui

An empty page that can be used on its own or navigated to within a Frame.

```csharp
public sealed class SelectionDetailView : Microsoft.UI.Xaml.Controls.UserControl, System.Runtime.InteropServices.ICustomQueryInterface, WinRT.IWinRTObject, System.Runtime.InteropServices.IDynamicInterfaceCastable, System.Runtime.InteropServices.Marshalling.IUnmanagedVirtualMethodTableProvider, System.IEquatable`1[[Microsoft.UI.Xaml.DependencyObject, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.IUIElementOverrides, Microsoft.UI.Composition.IAnimationObject, Microsoft.UI.Composition.IVisualElement, Microsoft.UI.Composition.IVisualElement2, System.IEquatable`1[[Microsoft.UI.Xaml.UIElement, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.IFrameworkElementOverrides, System.IEquatable`1[[Microsoft.UI.Xaml.FrameworkElement, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Controls.IControlOverrides, System.IEquatable`1[[Microsoft.UI.Xaml.Controls.Control, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], System.IEquatable`1[[Microsoft.UI.Xaml.Controls.UserControl, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Markup.IComponentConnector
```

Inheritance: Object → DependencyObject → UIElement → FrameworkElement → Control → UserControl → SelectionDetailView<br>
Implements: ICustomQueryInterface, IWinRTObject, IDynamicInterfaceCastable, IUnmanagedVirtualMethodTableProvider, IEquatable&lt;DependencyObject&gt;, IUIElementOverrides, IAnimationObject, IVisualElement, IVisualElement2, IEquatable&lt;UIElement&gt;, IFrameworkElementOverrides, IEquatable&lt;FrameworkElement&gt;, IControlOverrides, IEquatable&lt;Control&gt;, IEquatable&lt;UserControl&gt;, IComponentConnector<br>
Attributes: WinRTRuntimeClassNameAttribute, WinRTExposedTypeAttribute, UserControlRcwFactoryAttribute, ContentPropertyAttribute, ContractVersionAttribute, ControlRcwFactoryAttribute, ContractVersionAttribute, FrameworkElementRcwFactoryAttribute, ContractVersionAttribute, UIElementRcwFactoryAttribute, ContractVersionAttribute, DependencyObjectRcwFactoryAttribute, ContractVersionAttribute

## Fields

### _contentLoaded

```csharp
private bool _contentLoaded;
```

### Bindings

```csharp
private ISelectionDetailView_Bindings Bindings;
```

### SelectedPathProperty

Identifies the SelectionDetailView.SelectedPath dependency property.

```csharp
public static DependencyProperty SelectedPathProperty;
```

## Properties

### ViewModel

```csharp
public GroupedImageGalleryViewModel ViewModel { get; }
```

### SelectedPath

Gets or sets the selected path.

```csharp
public SelectedPath SelectedPath { get; set; }
```

## Constructors

### SelectionDetailView()

```csharp
public SelectionDetailView()
```

### SelectionDetailView()

```csharp
private static SelectionDetailView()
```

## Methods

### OnSelectedPathChanged(DependencyObject, DependencyPropertyChangedEventArgs)

```csharp
private static void OnSelectedPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
```

#### Parameters

`d` DependencyObject<br>

`e` DependencyPropertyChangedEventArgs<br>

### InitializeComponent()

InitializeComponent()

```csharp
public void InitializeComponent()
```

### Connect(Int32, Object)

Connect()

```csharp
public void Connect(int connectionId, object target)
```

#### Parameters

`connectionId` Int32<br>

`target` Object<br>

### GetBindingConnector(Int32, Object)

GetBindingConnector(int connectionId, object target)

```csharp
public IComponentConnector GetBindingConnector(int connectionId, object target)
```

#### Parameters

`connectionId` Int32<br>

`target` Object<br>

#### Returns

IComponentConnector<br>
 
# SettingsView

Namespace: miCompressor.ui

An empty page that can be used on its own or navigated to within a Frame.

```csharp
public sealed class SettingsView : Microsoft.UI.Xaml.Controls.Page, System.Runtime.InteropServices.ICustomQueryInterface, WinRT.IWinRTObject, System.Runtime.InteropServices.IDynamicInterfaceCastable, System.Runtime.InteropServices.Marshalling.IUnmanagedVirtualMethodTableProvider, System.IEquatable`1[[Microsoft.UI.Xaml.DependencyObject, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.IUIElementOverrides, Microsoft.UI.Composition.IAnimationObject, Microsoft.UI.Composition.IVisualElement, Microsoft.UI.Composition.IVisualElement2, System.IEquatable`1[[Microsoft.UI.Xaml.UIElement, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.IFrameworkElementOverrides, System.IEquatable`1[[Microsoft.UI.Xaml.FrameworkElement, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Controls.IControlOverrides, System.IEquatable`1[[Microsoft.UI.Xaml.Controls.Control, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], System.IEquatable`1[[Microsoft.UI.Xaml.Controls.UserControl, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Controls.IPageOverrides, System.IEquatable`1[[Microsoft.UI.Xaml.Controls.Page, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Markup.IComponentConnector
```

Inheritance: Object → DependencyObject → UIElement → FrameworkElement → Control → UserControl → Page → SettingsView<br>
Implements: ICustomQueryInterface, IWinRTObject, IDynamicInterfaceCastable, IUnmanagedVirtualMethodTableProvider, IEquatable&lt;DependencyObject&gt;, IUIElementOverrides, IAnimationObject, IVisualElement, IVisualElement2, IEquatable&lt;UIElement&gt;, IFrameworkElementOverrides, IEquatable&lt;FrameworkElement&gt;, IControlOverrides, IEquatable&lt;Control&gt;, IEquatable&lt;UserControl&gt;, IPageOverrides, IEquatable&lt;Page&gt;, IComponentConnector<br>
Attributes: WinRTRuntimeClassNameAttribute, WinRTExposedTypeAttribute, PageRcwFactoryAttribute, ContractVersionAttribute, UserControlRcwFactoryAttribute, ContentPropertyAttribute, ContractVersionAttribute, ControlRcwFactoryAttribute, ContractVersionAttribute, FrameworkElementRcwFactoryAttribute, ContractVersionAttribute, UIElementRcwFactoryAttribute, ContractVersionAttribute, DependencyObjectRcwFactoryAttribute, ContractVersionAttribute

## Fields

### _contentLoaded

```csharp
private bool _contentLoaded;
```

## Constructors

### SettingsView()

```csharp
public SettingsView()
```

## Methods

### InitializeComponent()

InitializeComponent()

```csharp
public void InitializeComponent()
```

### Connect(Int32, Object)

Connect()

```csharp
public void Connect(int connectionId, object target)
```

#### Parameters

`connectionId` Int32<br>

`target` Object<br>

### GetBindingConnector(Int32, Object)

GetBindingConnector(int connectionId, object target)

```csharp
public IComponentConnector GetBindingConnector(int connectionId, object target)
```

#### Parameters

`connectionId` Int32<br>

`target` Object<br>

#### Returns

IComponentConnector<br>
 
# GroupedImageGalleryViewModel

Namespace: miCompressor.ui.viewmodel

```csharp
public class GroupedImageGalleryViewModel
```

Inheritance: Object → GroupedImageGalleryViewModel<br>
Attributes: NullableContextAttribute, NullableAttribute

## Fields

### _itemgroupLock

```csharp
private object _itemgroupLock = System.Object;
```

### _currentSelectedPath

```csharp
private SelectedPath _currentSelectedPath;
```

### _throttleTimer

```csharp
private Timer _throttleTimer;
```

### _throttleLock

```csharp
private object _throttleLock = System.Object;
```

### _isThrottlingActive

```csharp
private bool _isThrottlingActive;
```

## Properties

### ImageGroups

```csharp
public ObservableCollection<ImageGroup> ImageGroups { get; }
```

## Constructors

### GroupedImageGalleryViewModel()

```csharp
public GroupedImageGalleryViewModel()
```

## Methods

### LoadData(SelectedPath)

```csharp
public void LoadData(SelectedPath selectedPath)
```

#### Parameters

`selectedPath` SelectedPath<br>

### RefreshImageGroupsThrottled(Int32)

```csharp
private void RefreshImageGroupsThrottled(int throttleTimeInMs)
```

#### Parameters

`throttleTimeInMs` Int32<br>

### RefreshImageGroups()

```csharp
private void RefreshImageGroups()
```

### UpdateImageGroups(IEnumerable&lt;ImageGroup&gt;)

```csharp
private void UpdateImageGroups(IEnumerable<ImageGroup> newGroups)
```

#### Parameters

`newGroups` IEnumerable&lt;ImageGroup&gt;<br>

### GetParentDirectory(String, String)

```csharp
private string GetParentDirectory(string rootPath, string subDirPath)
```

#### Parameters

`rootPath` String<br>

`subDirPath` String<br>

#### Returns

String<br>

### SelectedPath_PropertyChanged(Object, PropertyChangedEventArgs)

```csharp
private void SelectedPath_PropertyChanged(object sender, PropertyChangedEventArgs e)
```

#### Parameters

`sender` Object<br>

`e` PropertyChangedEventArgs<br>

### &lt;RefreshImageGroupsThrottled&gt;b__9_0(Object)

```csharp
private void <RefreshImageGroupsThrottled>b__9_0(object _)
```

#### Parameters

`_` Object<br>

### &lt;RefreshImageGroupsThrottled&gt;b__9_1()

```csharp
private void <RefreshImageGroupsThrottled>b__9_1()
```

### &lt;RefreshImageGroups&gt;b__10_0(MediaFileInfo)

```csharp
private string <RefreshImageGroups>b__10_0(MediaFileInfo file)
```

#### Parameters

`file` MediaFileInfo<br>

#### Returns

String<br>
 
# ImageGroup

Namespace: miCompressor.ui.viewmodel

```csharp
public class ImageGroup
```

Inheritance: Object → ImageGroup<br>
Attributes: NullableContextAttribute, NullableAttribute

## Properties

### FolderPath

```csharp
public string FolderPath { get; }
```

### Images

```csharp
public ObservableCollection<MediaFileInfo> Images { get; }
```

## Constructors

### ImageGroup(String, List&lt;MediaFileInfo&gt;)

```csharp
public ImageGroup(string folderPath, List<MediaFileInfo> images)
```

#### Parameters

`folderPath` String<br>

`images` List&lt;MediaFileInfo&gt;<br>

## Methods

### UpdateImages(IList&lt;MediaFileInfo&gt;)

```csharp
public void UpdateImages(IList<MediaFileInfo> newImages)
```

#### Parameters

`newImages` IList&lt;MediaFileInfo&gt;<br>
 
# WarningGroup

Namespace: miCompressor.ui.viewmodel

```csharp
public class WarningGroup
```

Inheritance: Object → WarningGroup<br>
Attributes: NullableContextAttribute, NullableAttribute

## Properties

### GroupName

```csharp
public string GroupName { get; }
```

### Items

```csharp
public List<MediaFileInfo> Items { get; }
```

## Constructors

### WarningGroup(String, List&lt;MediaFileInfo&gt;)

```csharp
public WarningGroup(string groupName, List<MediaFileInfo> items)
```

#### Parameters

`groupName` String<br>

`items` List&lt;MediaFileInfo&gt;<br>
 
# WarningViewModel

Namespace: miCompressor.ui.viewmodel

```csharp
public class WarningViewModel : System.ComponentModel.INotifyPropertyChanged
```

Inheritance: Object → WarningViewModel<br>
Implements: INotifyPropertyChanged<br>
Attributes: NullableContextAttribute, NullableAttribute

## Fields

### warningHelper

```csharp
private WarningHelper warningHelper = miCompressor.core.common.WarningHelper;
```

### _currentWarningType

```csharp
private string _currentWarningType = "Post Compression Warnings";
```

## Properties

### Warnings

```csharp
public ObservableCollection<WarningGroup> Warnings { get; private set; }
```

### CurrentWarningType

```csharp
public string CurrentWarningType { get; set; }
```

## Constructors

### WarningViewModel()

```csharp
public WarningViewModel()
```

## Methods

### WarningHelper_PropertyChanged(Object, PropertyChangedEventArgs)

```csharp
private void WarningHelper_PropertyChanged(object sender, PropertyChangedEventArgs e)
```

#### Parameters

`sender` Object<br>

`e` PropertyChangedEventArgs<br>

### RefreshWarnings()

```csharp
private void RefreshWarnings()
```

### OnPropertyChanged(String)

```csharp
protected void OnPropertyChanged(string propertyName)
```

#### Parameters

`propertyName` String<br>

## Events

### PropertyChanged

```csharp
public event PropertyChangedEventHandler PropertyChanged;
```
 
# MasterState

Namespace: miCompressor.viewmodels

ViewModel for MasterView to manage UI navigation.

```csharp
public class MasterState
```

Inheritance: Object → MasterState<br>
Attributes: NullableContextAttribute, NullableAttribute

## Properties

### FileStore

Comes from Static variable of App

```csharp
public FileStore FileStore { get; }
```

### OutputSettings

Comes from Static variable of App

```csharp
public OutputSettings OutputSettings { get; }
```

## Constructors

### MasterState()

Constructor initializes with `EmptyFilesView`

```csharp
public MasterState()
```
 
