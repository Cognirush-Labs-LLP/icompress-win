using System.Threading.Tasks;
using Windows.Storage.Pickers;
using Windows.Storage;
using Microsoft.UI.Xaml;
using WinRT.Interop;
using System;

namespace miCompressor.ui;

/// <summary>
/// Folder picker. 
/// </summary>
public static class FolderPickerHelper
{
    /// <summary>
    /// Opens a folder picker dialog and returns the selected folder path.
    /// </summary>
    /// <param name="window">The main application window (required for picker association).</param>
    /// <returns>The folder path if selected, otherwise null.</returns>
    public static async Task<string?> PickFolderAsync(Window window)
    {
        if (window is null) throw new ArgumentNullException(nameof(window));

        var folderPicker = new FolderPicker();
        var hWnd = WindowNative.GetWindowHandle(window);
        InitializeWithWindow.Initialize(folderPicker, hWnd);

        folderPicker.FileTypeFilter.Add("*");

        StorageFolder? folder = await folderPicker.PickSingleFolderAsync();
        return folder?.Path;
    }
}
