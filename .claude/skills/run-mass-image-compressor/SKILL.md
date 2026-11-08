---
name: run-mass-image-compressor
description: Build, run, and drive Mass Image Compressor (the WinUI 3 app in MIC_Source/miCompressor). Use when asked to start or launch the app, click through its UI, take a screenshot, check a UI change in the real app, compress an image through the UI, or run its integration tests.
---

Mass Image Compressor is a Windows-only WinUI 3 desktop app. Drive it with `.claude/skills/run-mass-image-compressor/driver.ps1`, which launches the real app, runs the steps you give it through UI Automation, kills it, and restores the user's saved settings.

All commands run from the repo root in PowerShell.

## Prerequisites

Windows, Windows PowerShell 5.1 (`powershell.exe`, not `pwsh`: the driver needs .NET Framework's UI Automation) and the .NET SDK. There's no `global.json`. This was verified with SDK 9.0.319:

```powershell
dotnet --version
```

## Build

```powershell
dotnet build MIC_Source\miCompressor\miCompressor.csproj -c Release -p:Platform=x64 -p:GenerateAppxPackageOnBuild=false
```

About 40 seconds. The driver runs `MIC_Source\miCompressor\bin\x64\Release\net8.0-windows10.0.19041.0\win-x64\Mass Image Compressor.exe`, which runs unpackaged even though the project is set up for MSIX. Rebuild first: the driver doesn't, and it will happily test a stale exe.

## Run (agent path)

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .claude\skills\run-mass-image-compressor\driver.ps1 formats state shot:start
```

Each argument is a step. Steps run in order, and a failed step stops the run with exit code 1. Screenshots land in `%TEMP%\mic-driver\shots\`. **Look at them.**

| Step | What it does |
|---|---|
| `state` | Prints the format and whether Preview, Quality, Resize, Metadata, Watermark and Rename are showing |
| `tree` | Prints every visible control with its automation id: start here to find a control |
| `formats` | Prints the format dropdown's items in order |
| `format:<label>` | Selects a format, such as `format:Icon` or `format:JPEG XL` |
| `location:<label>` | Selects where to save, such as `location:Specify Output Folder` |
| `folder` / `folder:<path>` | Prints or sets the output folder |
| `expand` | Expands the compression list down to file rows |
| `preview` | Clicks the first Preview link |
| `edit` | Clicks the first Edit link, which opens the Edit screen |
| `click:<id or name>` | Clicks a control through UI Automation, by automation id or visible name, such as `click:EditDone` or `"click:Remove All"`. Doesn't use the mouse |
| `add:<path>` | Adds a file or folder to the compression list through the input box |
| `config:<key>` | Prints a saved setting's file, such as `config:Edit.LastCropRatio`. Settings are restored when the driver ends |
| `key:<name>` | Presses `Left`, `Right`, `Up`, `Down`, `Enter` or `Esc`. Fails without sending anything unless the app is in front |
| `resize:<width>,<height>` | Resizes the window |
| `shown:<id>` | Prints whether a control with that automation id is on screen |
| `rect:<id>` | Prints a control's position and size on screen |
| `text:<id>` | Prints a text control's content by automation id |
| `type:<id>:<text>` | Types into a text box and leaves it focused, such as `type:EditWidthBox:800` |
| `value:<id>` | Prints a text box's content, such as `value:EditWidthBox` |
| `slider:<id>:<value>` | Sets a slider, such as `slider:EditZoomSlider:769`. The Edit screen's zoom slider takes a position from 0 to 1000 spaced by ratio (769 is 100%), and the zoom shows in `text:EditZoomText` |
| `size:<path>` | Prints an output image's width and height (formats Windows' GDI+ can read, such as PNG and JPEG) |
| `menu` / `menu:<shot>` | Right-clicks the file's row, prints the menu, and optionally takes a screenshot of it |
| `compress` | Clicks Compress All |
| `waitfile:<path>` | Waits up to 60 seconds for an output file |
| `shot:<name>` | Screenshots the window without bringing it forward. Misses popups |
| `screen:<name>` | Screenshots the screen area of the window, popups included. Brings it forward |
| `sleep:<seconds>` | Waits |

Compress the bundled sample to Icon and check the result screen:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .claude\skills\run-mass-image-compressor\driver.ps1 "format:Icon" compress "waitfile:$env:TEMP\mic-driver\input\Compressed\sample.ico" shot:done tree
```

Options, placed before the steps:

- `-ImagePath <file or folder>`: input to use. By default a sample is copied to `%TEMP%\mic-driver\input\`.
- `-SavedFormat <number>`: start as if that format were saved, using the `OutputFormat` enum's value. For example, 7 is Icon:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .claude\skills\run-mass-image-compressor\driver.ps1 -SavedFormat 7 state
```

- `-Exe <path>`: run a different build.
- `-OutDir <folder>`: use a different folder than `%TEMP%\mic-driver`.

## Test

The integration suite calls the compressor directly, without any UI. For changes under `core/`, this is usually the faster check. See `MIC_Source\miCompressor.IntegrationTests\IcoOutputTests.cs` for the pattern.

```powershell
dotnet test MIC_Source\miCompressor.IntegrationTests\miCompressor.IntegrationTests.csproj -p:Platform=x64
```

About a minute. Three tests fail on unchanged code, so check these against a clean checkout before blaming a change: `TestFileStore.TestBasicOpsSinglePath`, `GetOutputPathTests.PreviewModeWithReplaceOriginal_ReturnsTempPath` and `TestVersionChecker.TestWhenAlareadyLatest`.

Run one test class:

```powershell
dotnet test MIC_Source\miCompressor.IntegrationTests\miCompressor.IntegrationTests.csproj -p:Platform=x64 --filter "FullyQualifiedName~IcoOutputTests"
```

## Gotchas

- **Dragging can't be driven.** A left mouse press sent with `mouse_event` made VS Code take the foreground instead of reaching the app, so it could land in the user's editor. The driver has no drag step for that reason. Check the Edit screen's crop box and handles by hand.
- **Compress overwrites the user's saved settings.** The app saves to `%LOCALAPPDATA%\miCompressor\user_config\` when Compress is clicked, and the running app shares that folder with any other build. The driver backs it up to `<OutDir>\user_config.backup` and restores it after every run. If the driver is killed mid-run, restore from that backup by hand.
- **Don't hand-edit `SavedOutputSettings.json` with a tool that decodes escapes.** `Value` is JSON inside a string, with literal `"` quotes. If they're decoded, the file breaks and the app silently starts with default settings. Use `-SavedFormat`.
- **Output goes next to the input by default**, in a `Compressed` folder. That's why the sample is copied out of the repo first. Passing a repo path to `-ImagePath` and compressing writes into the repo.
- **Never compress real files with `location:Replace Original (Lose Original)`.** It overwrites them. Selecting it also resets the format to Keep Same and disables the format dropdown, so `format:` then fails.
- **`IsShown` means on screen.** A collapsed control leaves the UI Automation tree, but a control scrolled out of the settings panel also reads as hidden. For example, turning Metadata copy on pushes Apply Watermark out of view, so `state` reports it as `False`. Confirm with a screenshot.
- **The run takes over the desktop for a moment.** A window opens, `menu` moves the real mouse and right-clicks, and `screen` brings the app forward. A `screen` shot also captures anything showing around the window's edges.
- **The first launch on a machine adds a Send To shortcut** pointing at whichever exe ran. If one already exists, it's left alone.
- **The automation id `FoderPathTextBox` is spelled that way** in the app. Search for that spelling.

## Troubleshooting

- **`MakeAppx : error : ... error C00CE018: App manifest validation error ... Line 2, Column 2, Reason: Text is not allowed in the context of element '...Package'`**: this comes from a Release `dotnet build` or `dotnet test` without `-p:GenerateAppxPackageOnBuild=false`. The exe is built before this step, so it may still run, but no package is produced. The cause hasn't been found.
- **`App not built: <path>`**: run the build command above.
- **`Format dropdown is disabled (Replace Original is selected)`**: pick another location first with `location:Specify Output Folder`.
