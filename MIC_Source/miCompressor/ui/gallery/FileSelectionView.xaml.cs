using miCompressor.core;
using miCompressor.viewmodels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using Windows.Storage;
using miCompressor.core.common;
using System.Threading;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace miCompressor.ui
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FileSelectionView : UserControl
    {
        [AutoNotify]
        private bool isEmptyViewVisible = true;

        public FileSelectionView()
        {
            this.InitializeComponent();
            IsEmptyViewVisible = !App.FileStoreInstance.SelectedPaths.Any();
            App.FileStoreInstance.PropertyChanged += FileStore_PropertyChanged;
        }

        private void FileStore_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(FileStore.SelectedPaths))
                IsEmptyViewVisible = !App.FileStoreInstance.SelectedPaths.Any();
        }

        private void SelectedItem_SelectedPathDeleted(object sender, SelectedPath e)
        {
            // Handle the deletion, such as removing the path from a collection or refreshing the UI
            App.FileStoreInstance.Remove(e.Path);
        }

        private void Remove_All_Click(object sender, RoutedEventArgs e)
        {
            App.FileStoreInstance.RemoveAll();
        }


        private async void AddFilesButton_Click(object sender, RoutedEventArgs e)
        {
            AddFilesButton.IsEnabled = false;
            FileOpenPicker openPicker = new();

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow!);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            foreach (var ext in CodeConsts.SupportedInputExtensionsWithDot)
            {
                openPicker.FileTypeFilter.Add(ext);
            }

            //openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            // Allow selecting multiple files
            var files = await openPicker.PickMultipleFilesAsync();

            if (files != null && files.Count > 0)
            {
                foreach (var file in files)
                {
                    App.FileStoreInstance.AddAsync(file.Path, false);
                }
            }

            AddFilesButton.IsEnabled = true;
        }


        private async void AddFolderButton_Click(object sender, RoutedEventArgs e)
        {
            AddFolderButton.IsEnabled = false;
            FolderPicker openPicker = new();

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow!);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            openPicker.FileTypeFilter.Add("*");

            // Open the picker for the user to pick a folder
            StorageFolder folder = await openPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                //StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                App.FileStoreInstance.AddAsync(folder.Path);
            }
            AddFolderButton.IsEnabled = true;
        }

        private async void AddFolderKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            AddFolderButton_Click(sender, new RoutedEventArgs());
            args.Handled = true;
        }

        private async void AddFileKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            AddFilesButton_Click(sender, new RoutedEventArgs());
            args.Handled = true;
        }

        private void AddInputPathButton_Click(object sender, RoutedEventArgs e)
        {
            AddInputPathButton.IsEnabled = false;
            try
            {
                string inputPathTxt = (InputPathTextBox.Text ?? "").Trim();
                if (string.IsNullOrWhiteSpace(inputPathTxt))
                    { return; }

                if (Directory.Exists(inputPathTxt) || File.Exists(inputPathTxt))
                {
                    App.FileStoreInstance.AddAsync(inputPathTxt);
                    InputPathTextBox.Text = "";
                }
            }
            finally
            {
                    AddInputPathButton.IsEnabled = true;
            }
        }

        private void InputPathTextBox_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                AddInputPathButton_Click(sender, new RoutedEventArgs());
            }
        }
    }
}
