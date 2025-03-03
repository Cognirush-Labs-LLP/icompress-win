using miCompressor.core;
using miCompressor.ui.viewmodel;
using miCompressor.viewmodel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace miCompressor.ui
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within it.
    /// </summary>
    public sealed partial class MasterView : Page
    {
        /// <summary>
        /// Comes from Static variable of App
        /// </summary>
        public FileStore FileStore => App.FileStoreInstance;

        public MasterState CurrentState => App.CurrentState;

        [AutoNotify]
        private bool isEmptyViewVisible = true;

        public bool IsFileSelectionViewVisible => !IsEmptyViewVisible;


        public MasterView()
        {
            this.InitializeComponent();
            isEmptyViewVisible = !FileStore.SelectedPaths.Any();

            FileStore.PropertyChanged += masterState_PropertyChanged;
        }

        private void masterState_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            IsEmptyViewVisible = !FileStore.SelectedPaths.Any();
            OnPropertyChanged(nameof(IsFileSelectionViewVisible));
        }

        private void MasterView_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            e.DragUIOverride.Caption = "Add to Selection";
        }

        private async void MasterView_Drop(object sender, DragEventArgs e)
        {
            try
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items == null || items.Count == 0) return;

                List<string> unsupportedFiles = new();

                foreach (var item in items)
                {
                    string path = item.Path;
                    if (Directory.Exists(path))
                    {
                        // Add Folders
                        FileStore.Enqueue(path);
                    }
                    else if (File.Exists(path))
                    {
                        // Check if file is supported
                        string extension = Path.GetExtension(path).ToLower();
                        if (CodeConsts.SupportedInputExtensionsWithDot.Contains(extension))
                            FileStore.Enqueue(path);
                        else
                            unsupportedFiles.Add(path);
                    }
                }

                // Show warning message if unsupported files were ignored
                if (unsupportedFiles.Any())
                {
                    ShowWarning($"Files with extensions {string.Join(", ", unsupportedFiles.Select(f => Path.GetExtension(f)))} were ignored as they are unsupported.");
                }
            } 
            catch
            {
                // Some strange drag/drop may cause this. Those should be ignored.
            }
        }

        private void ShowWarning(string message)
        {
            WarningText.Text = message;
            WarningBanner.Visibility = Visibility.Visible;

            // Auto-hide the warning after 5 seconds
            Task.Delay(5000).ContinueWith(_ =>
            {
                UIThreadHelper.RunOnUIThread(() => WarningBanner.Visibility = Visibility.Collapsed);
            });
        }
    }
}
