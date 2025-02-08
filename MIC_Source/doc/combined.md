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

## Properties

### SupportedInputExtensionsWithDot

Generated from <![CDATA[SupportedInputExtensions]]> but file extensions with dot. i.e. ".jpg", ".jpeg", ".png", ".webp"

```csharp
public static IEnumerable<string> SupportedInputExtensionsWithDot { get; }
```

## Constructors

### CodeConsts()

```csharp
private static CodeConsts()
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

```csharp
[AutoNotify] private int width;
```

### height

```csharp
[AutoNotify] private int height;
```

### fileSize

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

### IsReplaceOperation

Determines whether this operation is replacing the original file.
 Used when OutputLocationSettings is set to ReplaceOriginal.

```csharp
private bool IsReplaceOperation { get; set; }
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

## Methods

### LoadImageMetadataAsync()

Loads image metadata asynchronously without blocking the UI.

```csharp
private Task LoadImageMetadataAsync()
```

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
The full path of the output file.

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
| Jpg | 1 |  |
| Png | 2 |  |
| Tiff | 3 |  |
| Webp | 4 |  |
 
# OutputFormatExtensions

Namespace: miCompressor.core

Provides extension methods for OutputFormat enumeration.

```csharp
public static class OutputFormatExtensions
```

Inheritance: Object → OutputFormatExtensions<br>
Attributes: NullableContextAttribute, NullableAttribute, ExtensionAttribute

## Methods

### GetExtension(OutputFormat, String)

Retrieves the corresponding file extension for a given output format,
 preserving the original file's extension case if it matches.

```csharp
public static string GetExtension(OutputFormat format, string originalFilePath)
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

### secondaryEdgeLength

This is used only for FitInFrame and FixedInFrame dimension strategy.

```csharp
[AutoNotify] public int secondaryEdgeLength = 1080;
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

### SecondaryEdgeLength

```csharp
public int SecondaryEdgeLength { get; set; }
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

### includeSubDirectories

Determines whether subdirectories should be included in the scan.
 Changing this property triggers a new scan.

```csharp
[AutoNotify] private bool includeSubDirectories;
```

### _files

```csharp
private IList<MediaFileInfo> _files;
```

## Properties

### Path

The absolute path of the selected file or directory.

```csharp
public string Path { get; }
```

### IsDirectory

Indicates whether the selected path is a directory.

```csharp
public bool IsDirectory { get; }
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

### IncludeSubDirectories

```csharp
public bool IncludeSubDirectories { get; set; }
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

### ScanForMediaFiles()

Scans the directory asynchronously and updates the file list.
 Only files with supported extensions are included.

```csharp
private Task ScanForMediaFiles()
```

#### Returns

Task<br>

### PopulateAllFilesForSupportedExtension(String[], String, Boolean, List&lt;FileInfo&gt;)

Recursively scans given `rootFolderPath` and populates `files`

```csharp
private void PopulateAllFilesForSupportedExtension(String[] SupportedInputExtensions, string rootFolderPath, bool inlcudeSubDir, List<FileInfo> files)
```

#### Parameters

`SupportedInputExtensions` String[]<br>
Small cased extensions without preceding "." e.g. ["jpg", "jpeg", "png"]

`rootFolderPath` String<br>
Selected Path by user to search the images within

`inlcudeSubDir` Boolean<br>
Should search directories within `rootFolderPath' or not.

`files` List&lt;FileInfo&gt;<br>
Pass an empty list, this will be populated with supported files inside `rootFolderPath`

### OnPropertyChanged(String)

```csharp
protected void OnPropertyChanged(string propertyName)
```

#### Parameters

`propertyName` String<br>

### &lt;ScanForMediaFiles&gt;b__13_0()

```csharp
private void <ScanForMediaFiles>b__13_0()
```

## Events

### PropertyChanged

```csharp
public event PropertyChangedEventHandler PropertyChanged;
```
 
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
 
# EmptyFilesView

Namespace: miCompressor.ui

An empty page that can be used on its own or navigated to within a Frame.

```csharp
public sealed class EmptyFilesView : Microsoft.UI.Xaml.Controls.Page, System.Runtime.InteropServices.ICustomQueryInterface, WinRT.IWinRTObject, System.Runtime.InteropServices.IDynamicInterfaceCastable, System.Runtime.InteropServices.Marshalling.IUnmanagedVirtualMethodTableProvider, System.IEquatable`1[[Microsoft.UI.Xaml.DependencyObject, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.IUIElementOverrides, Microsoft.UI.Composition.IAnimationObject, Microsoft.UI.Composition.IVisualElement, Microsoft.UI.Composition.IVisualElement2, System.IEquatable`1[[Microsoft.UI.Xaml.UIElement, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.IFrameworkElementOverrides, System.IEquatable`1[[Microsoft.UI.Xaml.FrameworkElement, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Controls.IControlOverrides, System.IEquatable`1[[Microsoft.UI.Xaml.Controls.Control, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], System.IEquatable`1[[Microsoft.UI.Xaml.Controls.UserControl, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Controls.IPageOverrides, System.IEquatable`1[[Microsoft.UI.Xaml.Controls.Page, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Markup.IComponentConnector
```

Inheritance: Object → DependencyObject → UIElement → FrameworkElement → Control → UserControl → Page → EmptyFilesView<br>
Implements: ICustomQueryInterface, IWinRTObject, IDynamicInterfaceCastable, IUnmanagedVirtualMethodTableProvider, IEquatable&lt;DependencyObject&gt;, IUIElementOverrides, IAnimationObject, IVisualElement, IVisualElement2, IEquatable&lt;UIElement&gt;, IFrameworkElementOverrides, IEquatable&lt;FrameworkElement&gt;, IControlOverrides, IEquatable&lt;Control&gt;, IEquatable&lt;UserControl&gt;, IPageOverrides, IEquatable&lt;Page&gt;, IComponentConnector<br>
Attributes: WinRTRuntimeClassNameAttribute, WinRTExposedTypeAttribute, PageRcwFactoryAttribute, ContractVersionAttribute, UserControlRcwFactoryAttribute, ContentPropertyAttribute, ContractVersionAttribute, ControlRcwFactoryAttribute, ContractVersionAttribute, FrameworkElementRcwFactoryAttribute, ContractVersionAttribute, UIElementRcwFactoryAttribute, ContractVersionAttribute, DependencyObjectRcwFactoryAttribute, ContractVersionAttribute

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
 
# FileSelectionView

Namespace: miCompressor.ui

An empty page that can be used on its own or navigated to within a Frame.

```csharp
public sealed class FileSelectionView : Microsoft.UI.Xaml.Controls.Page, System.Runtime.InteropServices.ICustomQueryInterface, WinRT.IWinRTObject, System.Runtime.InteropServices.IDynamicInterfaceCastable, System.Runtime.InteropServices.Marshalling.IUnmanagedVirtualMethodTableProvider, System.IEquatable`1[[Microsoft.UI.Xaml.DependencyObject, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.IUIElementOverrides, Microsoft.UI.Composition.IAnimationObject, Microsoft.UI.Composition.IVisualElement, Microsoft.UI.Composition.IVisualElement2, System.IEquatable`1[[Microsoft.UI.Xaml.UIElement, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.IFrameworkElementOverrides, System.IEquatable`1[[Microsoft.UI.Xaml.FrameworkElement, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Controls.IControlOverrides, System.IEquatable`1[[Microsoft.UI.Xaml.Controls.Control, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], System.IEquatable`1[[Microsoft.UI.Xaml.Controls.UserControl, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Controls.IPageOverrides, System.IEquatable`1[[Microsoft.UI.Xaml.Controls.Page, Microsoft.WinUI, Version=3.0.0.0, Culture=neutral, PublicKeyToken=de31ebe4ad15742b]], Microsoft.UI.Xaml.Markup.IComponentConnector
```

Inheritance: Object → DependencyObject → UIElement → FrameworkElement → Control → UserControl → Page → FileSelectionView<br>
Implements: ICustomQueryInterface, IWinRTObject, IDynamicInterfaceCastable, IUnmanagedVirtualMethodTableProvider, IEquatable&lt;DependencyObject&gt;, IUIElementOverrides, IAnimationObject, IVisualElement, IVisualElement2, IEquatable&lt;UIElement&gt;, IFrameworkElementOverrides, IEquatable&lt;FrameworkElement&gt;, IControlOverrides, IEquatable&lt;Control&gt;, IEquatable&lt;UserControl&gt;, IPageOverrides, IEquatable&lt;Page&gt;, IComponentConnector<br>
Attributes: WinRTRuntimeClassNameAttribute, WinRTExposedTypeAttribute, PageRcwFactoryAttribute, ContractVersionAttribute, UserControlRcwFactoryAttribute, ContentPropertyAttribute, ContractVersionAttribute, ControlRcwFactoryAttribute, ContractVersionAttribute, FrameworkElementRcwFactoryAttribute, ContractVersionAttribute, UIElementRcwFactoryAttribute, ContractVersionAttribute, DependencyObjectRcwFactoryAttribute, ContractVersionAttribute

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
 
