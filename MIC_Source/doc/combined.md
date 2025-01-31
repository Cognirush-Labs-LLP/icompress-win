# App `Public class`

## Details
### Summary
Provides application-specific behavior to supplement the default Application class.

### Inheritance
 - `IXamlMetadataProvider`
 - `Application`

### Constructors
#### App
```csharp
public App()
```
##### Summary
Initializes the singleton application object.  This is the first line of authored code
            executed, and as such is the logical equivalent of main() or WinMain().

### Fields
#### Window m_window
```csharp
private  Window m_window
```

#### Boolean _contentLoaded
```csharp
private  Boolean _contentLoaded
```

#### XamlMetaDataProvider __appProvider
```csharp
private  XamlMetaDataProvider __appProvider
```

### Methods
#### OnLaunched
```csharp
protected override void OnLaunched(LaunchActivatedEventArgs args)
```
##### Arguments

 - args : Details about the launch request and process.

##### Summary
Invoked when the application is launched.

#### InitializeComponent
```csharp
public void InitializeComponent()
```
##### Summary
InitializeComponent()

#### GetXamlType [1/2]
```csharp
public virtual IXamlType GetXamlType(Type type)
```
##### Arguments



##### Summary
GetXamlType(Type)

#### GetXamlType [2/2]
```csharp
public virtual IXamlType GetXamlType(string fullName)
```
##### Arguments



##### Summary
GetXamlType(String)

#### GetXmlnsDefinitions
```csharp
public virtual XmlnsDefinition GetXmlnsDefinitions()
```
##### Summary
GetXmlnsDefinitions()

### Properties
#### _AppProvider
```csharp
private XamlMetaDataProvider _AppProvider { get; }
```
 
# MainWindow `Public class`

## Details
### Summary
An empty window that can be used on its own or navigated to within a Frame.

### Inheritance
 - `IComponentConnector`
 - `Window`

### Constructors
#### MainWindow
```csharp
public MainWindow()
```

### Fields
#### Button myButton
```csharp
private  Button myButton
```

#### Boolean _contentLoaded
```csharp
private  Boolean _contentLoaded
```

### Methods
#### myButton_Click
```csharp
private void myButton_Click(object sender, RoutedEventArgs e)
```
##### Arguments




#### InitializeComponent
```csharp
public void InitializeComponent()
```
##### Summary
InitializeComponent()

#### Connect
```csharp
public virtual void Connect(int connectionId, object target)
```
##### Arguments




##### Summary
Connect()

#### GetBindingConnector
```csharp
public virtual IComponentConnector GetBindingConnector(int connectionId, object target)
```
##### Arguments




##### Summary
GetBindingConnector(int connectionId, object target)
 
# Program `Public class`

## Details
### Summary
Program class

### Methods
#### Main
```csharp
private static void Main(string[] args)
```
##### Arguments


 
# AutoNotifyAttribute `Public class`

## Details
### Summary
This attribute helps generate a public property that auto implements getter and setters for ObservableBase to trigger a change event.

### Remarks
Do Not Create variable name with first letter capital or something that cannot be capitalized such as underscore. A good variable name 'width'. Bad variable names '_width', 'Width'.
            Additionally, class using `AutoNotifyAttribute` must not be an inner class of any other class.

### Inheritance
 - `Attribute`

### Constructors
#### AutoNotifyAttribute
```csharp
public AutoNotifyAttribute()
```
 
# FileStore `Public class`

## Details
### Summary
Manages a thread-safe collection of selected file paths.

### Inheritance
 - [
`ObservableBase`
](./micompressorcore-ObservableBase.md)

### Constructors
#### FileStore
```csharp
public FileStore()
```

### Fields
#### List`1 _store
```csharp
private  List`1 _store
```

#### ReaderWriterLockSlim _lock
```csharp
private  ReaderWriterLockSlim _lock
```

### Methods
#### AddAsync
```csharp
public PathAddedResult AddAsync(string path, bool scanSubDirectories)
```
##### Arguments

 - path : The file or directory path.
 - scanSubDirectories : Indicates whether subdirectories should be scanned.

##### Summary
Adds a new path to the store. This method returns the result just after making entry in selected path but all files to process will populate only after

##### Returns
The result of the add operation.

#### Remove
```csharp
public bool Remove(string path)
```
##### Arguments

 - path : The path to remove.

##### Summary
Removes a specified path from the store in a thread-safe manner.

##### Returns
True if the path was removed, false if it was not found.

#### ChangeIncludeSubDirectoriesSetting
```csharp
public bool ChangeIncludeSubDirectoriesSetting(string path, bool includeSubDirectories)
```
##### Arguments

 - path : The path to remove.
 - includeSubDirectories : Should Include Sub-Directories or not

##### Summary
Change setting of scanning the added path to include Subdirectories or not. No change if settings are not changed.

##### Returns
True if the path was found as selected path and will be taken up for making setting change if aplicable.

#### RemoveAll
```csharp
public void RemoveAll()
```
##### Summary
Remove all stored paths in a thread-safe manner.

### Properties
#### SelectedPaths
```csharp
public IReadOnlyCollection<SelectedPath> SelectedPaths { get; }
```
##### Summary
Retrieves a thread-safe read-only collection of selected paths.

#### GetAllFiles
```csharp
public IReadOnlyCollection<MediaFileInfo> GetAllFiles { get; }
```
##### Summary
Retrieves all unique media files across selected paths.

##### Remarks
This is dynamically created list, be mindful and cache for multiple access.
 
# HumanReadable `Public class`

## Details
### Methods
#### FileSize
```csharp
public static string FileSize(ulong fileSize)
```
##### Arguments

 - fileSize : The file size in bytes (ulong).

##### Summary
Converts a file size in bytes to a human-readable string format.
            Supports B, KB, MB, GB, TB, PB up to exabytes.

##### Returns
Formatted string with appropriate unit (B, KB, MB, GB, etc.).
 
# MediaFileInfo `Public class`

## Details
### Summary
Represents an image file, storing metadata and relative path for compression.
            Uses async loading to prevent UI blocking and supports automatic UI updates.

### Inheritance
 - [
`ObservableBase`
](./micompressorcore-ObservableBase.md)

### Constructors
#### MediaFileInfo
```csharp
public MediaFileInfo(string selectedPath, FileInfo mediaFile)
```
##### Arguments

 - selectedPath : The root directory selected by the user.


##### Summary
Initializes a new instance of [MediaFileInfo](micompressorcore-MediaFileInfo.md) .

##### Exceptions
| Name | Description |
| --- | --- |
| FileNotFoundException | Throws exception if file doesn't exist. |

### Fields
#### Int32 width
```csharp
private  Int32 width
```

#### Int32 height
```csharp
private  Int32 height
```

#### UInt64 fileSize
```csharp
private  UInt64 fileSize
```

#### String cameraModel
```csharp
private  String cameraModel
```

#### Nullable`1 dateTaken
```csharp
private  Nullable`1 dateTaken
```

#### Boolean excludeAndHide
```csharp
private  Boolean excludeAndHide
```

#### Boolean excludeAndShow
```csharp
private  Boolean excludeAndShow
```

### Methods
#### LoadImageMetadataAsync
```csharp
private async Task LoadImageMetadataAsync()
```
##### Summary
Loads image metadata asynchronously without blocking the UI.

### Properties
#### SelectedRootPath
```csharp
public string SelectedRootPath { get; }
```
##### Summary
The root directory selected by the user.

#### fileToProcess
```csharp
public FileInfo fileToProcess { get; }
```
##### Summary
The full path to the image file.

#### RelativePath
```csharp
public string RelativePath { get; }
```
##### Summary
The relative path of the image within the selected directory.

#### ShouldProcess
```csharp
public bool ShouldProcess { get; }
```

#### FileSizeToShow
```csharp
public string FileSizeToShow { get; }
```

#### Width
```csharp
public int Width { get; private set; }
```

#### Height
```csharp
public int Height { get; private set; }
```

#### FileSize
```csharp
public ulong FileSize { get; private set; }
```

#### CameraModel
```csharp
public string CameraModel { get; private set; }
```

#### DateTaken
```csharp
public Nullable<DateTimeOffset> DateTaken { get; private set; }
```

#### ExcludeAndHide
```csharp
public bool ExcludeAndHide { get; private set; }
```

#### ExcludeAndShow
```csharp
public bool ExcludeAndShow { get; private set; }
```
 
# ObservableBase `Public class`

## Details
### Summary
Base class for observable objects that supports property change notifications
            without requiring explicit private fields.

### Example
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

### Inheritance
 - `INotifyPropertyChanged`

### Constructors
#### ObservableBase
```csharp
public ObservableBase()
```

### Fields
#### Dictionary`2 _propertyValues
```csharp
private  Dictionary`2 _propertyValues
```

#### Dictionary`2 _changeCallbacks
```csharp
private  Dictionary`2 _changeCallbacks
```

#### ReaderWriterLockSlim _lock
```csharp
private  ReaderWriterLockSlim _lock
```

#### PropertyChangedEventHandler PropertyChanged
```csharp
private  PropertyChangedEventHandler PropertyChanged
```

### Methods
#### GetProperty
```csharp
protected T GetProperty<T>(T defaultValue, string propertyName)
where T : 
```
##### Arguments




#### SetProperty
```csharp
protected bool SetProperty<T>(T value, Action<T, T> onChanged, string propertyName)
where T : 
```
##### Arguments





#### raisePropertyChanged
```csharp
protected virtual void raisePropertyChanged(string propertyName)
```
##### Arguments

 - propertyName : Name of the property that changed

##### Summary
Raises event in UI thread if the UI Thread is available. Ignores raising the event otherwise.

### Events
#### PropertyChanged
```csharp
public event PropertyChangedEventHandler PropertyChanged
```
##### Summary
Event triggered when a property value changes.
 
# OutputFormat `Public enum`

## Details
### Summary
Represents the supported output formats for image compression.
            Stores both format name and file extension.

### Fields
#### Jpg


#### Png


#### Tiff


#### Webp

 
# OutputFormatExtensions `Public class`

## Details
### Summary
Provides extension methods for OutputFormat enumeration.

### Methods
#### GetExtension
```csharp
public static string GetExtension(OutputFormat format, string originalFilePath)
```
##### Arguments

 - format : The desired output format.
 - originalFilePath : The original file's full path.

##### Summary
Retrieves the corresponding file extension for a given output format,
            preserving the original file's extension case if it matches.

##### Returns
The correct file extension preserving case and format.

#### IsJpeg
```csharp
private static bool IsJpeg(string ext)
```
##### Arguments



#### IsPng
```csharp
private static bool IsPng(string ext)
```
##### Arguments



#### IsTiff
```csharp
private static bool IsTiff(string ext)
```
##### Arguments



#### IsWebp
```csharp
private static bool IsWebp(string ext)
```
##### Arguments


 
# OutputSettings `Public class`

## Details
### Summary
Stores output settings including compression information and others.

### Inheritance
 - [
`ObservableBase`
](./micompressorcore-ObservableBase.md)

### Constructors
#### OutputSettings
```csharp
public OutputSettings()
```

### Fields
#### Int32 quality
```csharp
public  Int32 quality
```

#### OutputFormat format
```csharp
public  OutputFormat format
```

### Properties
#### Quality
```csharp
public int Quality { get; set; }
```

#### Format
```csharp
public OutputFormat Format { get; set; }
```
 
# PathAddedResult `Public enum`

## Details
### Summary
Represents the result of attempting to add a path.

### Fields
#### Success


#### AlreadyExists


#### InvalidPath

 
# PathHelper `Public class`

## Details
### Methods
#### ConvertToLongPath
```csharp
public static string ConvertToLongPath(string path)
```
##### Arguments

 - path : The original file or directory path.

##### Summary
Converts a standard Windows path to a long path format if needed.
            - Adds `\\?\` prefix only if:
              1. The path is longer than 260 characters.
              2. It is not already in long path format (`\\?\` or `\\?\UNC\`).
            - Works for both local and UNC paths.

##### Returns
The path converted to a long path format if required.
 
# ReaderWriterLockSlimExtensions `Public class`

## Details
### Nested types
#### Classes
 - `LockReleaser`

### Methods
#### ReadLock
```csharp
public static IDisposable ReadLock(ReaderWriterLockSlim rwLock)
```
##### Arguments



#### WriteLock
```csharp
public static IDisposable WriteLock(ReaderWriterLockSlim rwLock)
```
##### Arguments


 
# SelectedPath `Public class`

## Details
### Summary
Represents a selected file or directory path that can be scanned for media files.

### Inheritance
 - [
`ObservableBase`
](./micompressorcore-ObservableBase.md)

### Constructors
#### SelectedPath
```csharp
public SelectedPath(string path, bool includeSubDirectories)
```
##### Arguments

 - path : The file or directory path.
 - includeSubDirectories : Specifies whether to scan subdirectories.

##### Summary
Initializes a new instance of the [SelectedPath](micompressorcore-SelectedPath.md) class.

### Fields
#### Boolean scanningForFiles
```csharp
private  Boolean scanningForFiles
```

#### Boolean includeSubDirectories
```csharp
private  Boolean includeSubDirectories
```

#### IList`1 _files
```csharp
private  IList`1 _files
```

### Methods
#### ChangeToIncludeSubDirectories
```csharp
public void ChangeToIncludeSubDirectories(bool includeSubDirectories)
```
##### Arguments



##### Summary
Changes the settings to include or exclude sub-directories. Make sure UI looks for `ScanningForFiles` and disable the toggle control to avoid inconsistency.

#### ScanForMediaFiles
```csharp
private async Task ScanForMediaFiles()
```
##### Summary
Scans the directory asynchronously and updates the file list.
            Only files with supported extensions are included.

#### PopulateAllFilesForSupportedExtension
```csharp
private void PopulateAllFilesForSupportedExtension(string[] SupportedInputExtensions, string rootFolderPath, bool inlcudeSubDir, List<FileInfo> files)
```
##### Arguments






#### <ScanForMediaFiles>b__13_0
```csharp
private void <ScanForMediaFiles>b__13_0()
```

### Properties
#### Path
```csharp
public string Path { get; }
```
##### Summary
The absolute path of the selected file or directory.

#### IsDirectory
```csharp
public bool IsDirectory { get; }
```
##### Summary
Indicates whether the selected path is a directory.

#### Files
```csharp
public IReadOnlyList<MediaFileInfo> Files { get; }
```
##### Summary
A read-only list of media files found in the selected path.

#### ScanningForFiles
```csharp
public bool ScanningForFiles { get; private set; }
```

#### IncludeSubDirectories
```csharp
public bool IncludeSubDirectories { get; private set; }
```
 
# UIThreadHelper `Public class`

## Details
### Summary
Provides a globally accessible reference to the UI thread's DispatcherQueue .
            This allows ViewModels and background tasks to dispatch UI updates safely.

### Methods
#### Initialize
```csharp
public static void Initialize()
```
##### Summary
Initializes the UI dispatcher queue. 
            This must be called **only from the UI thread**, preferably in App.xaml.cs or MainWindow.xaml.cs.

### Properties
#### UIThreadDispatcherQueue
```csharp
public static DispatcherQueue UIThreadDispatcherQueue { get; private set; }
```
##### Summary
Stores the UI thread's DispatcherQueue for cross-thread UI updates.
            This must be initialized on the UI thread.
 
# CodeConsts `Public class`

## Details
### Constructors
#### CodeConsts
```csharp
private static CodeConsts()
```

### Fields
#### Int32 ThumbSize
```csharp
public static  Int32 ThumbSize
```

#### String[] SupportedInputExtensions
```csharp
public static  String[] SupportedInputExtensions
```

#### String[] SupportedOutputExtensions
```csharp
public static  String[] SupportedOutputExtensions
```
 
# LockReleaser `Private class`

## Details
### Inheritance
 - `IDisposable`

### Constructors
#### LockReleaser
```csharp
public LockReleaser(Action release)
```
##### Arguments



### Fields
#### Action _release
```csharp
private  Action _release
```

### Methods
#### Dispose
```csharp
public virtual void Dispose()
```
 
