using miCompressor.core;
using miCompressor.viewmodel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.IO;
using System.Linq;
using Windows.Storage;
using Windows.Storage.Pickers;

namespace miCompressor.ui
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FileSelectionView : UserControl
    {

        public const string c_ThumbSettingOption_Show = "Show Thumb";
        public const string c_ThumbSettingOption_OnHover = "Thumb On Mouse";
        public const string c_ThumbSettingOption_NoThumb = "No Thumbs";

        public string ThumbSettingOption_Show => c_ThumbSettingOption_Show;
        public string ThumbSettingOption_OnHover => c_ThumbSettingOption_OnHover;
        public string ThumbSettingOption_NoThumb => c_ThumbSettingOption_NoThumb;

        public string ThumbSettingOptionIcon_Show => "\uf03a";
        public string ThumbSettingOptionIcon_OnHover => "\uf27a";
        public string ThumbSettingOptionIcon_NoThumb => "\uf0c9";

        [AutoNotify] public string thumbSettingOptionIcon_Selected = "\uf03a";

        [AutoNotify] private bool isEmptyViewVisible = true;

        /// <summary>
        /// Global State.
        /// </summary>
        public MasterState CurrentState = App.CurrentState;

        public FileStore FileStore => CurrentState.FileStore;

        public FileSelectionView()
        {
            this.InitializeComponent();
            SetThumbSettingOptionIcon_Selected();
            IsEmptyViewVisible = !App.FileStoreInstance.SelectedPaths.Any();
            App.FileStoreInstance.PropertyChanged += FileStore_PropertyChanged;

            ShowFilterButton.Label = CurrentState.ShowFilterOptions ? "Hide Filters" : "Show Filters";
        }

        private void FileStore_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(FileStore.SelectedPaths))
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
                    App.FileStoreInstance.Enqueue(file.Path, false);
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
                App.FileStoreInstance.Enqueue(folder.Path);
            }
            AddFolderButton.IsEnabled = true;
        }

        private async void ShowFilterButton_Click(object sender, RoutedEventArgs e)
        {
            var isFilterOptionVisible = FilterOptions.Visibility == Visibility.Visible;
            if (isFilterOptionVisible)
                HideFilterOptions();
            else
                ShowFilterOptions();

            ShowFilterButton.Label = isFilterOptionVisible ? "Show Filters" : "Hide Filters"; // reverse as we just toggled visibility value. 
        }

        private void ShowFilterOptions()
        {
            FilterOptions.Visibility = Visibility.Visible; // Make it render
            FadeInAnimation.Begin(); // Start fade-in effect
            CurrentState.ShowFilterOptions = true;
        }

        private void HideFilterOptions()
        {
            FadeOutAnimation.Completed += (s, e) =>
            {
                FilterOptions.Visibility = Visibility.Collapsed; // Hide after animation ends
            };
            FadeOutAnimation.Begin();
            CurrentState.ShowFilterOptions = false;
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
        private async void ShowFilterKeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            ShowFilterButton_Click(sender, new RoutedEventArgs());
            args.Handled = true;
        }


        private void OnThumbSettingSelected(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem)
            {
                string selectedValue = menuItem.Text;
                switch (selectedValue)
                {
                    case c_ThumbSettingOption_Show:
                        App.CurrentState.ShowImageIconInFileSelectionTreeView = true;
                        App.CurrentState.ShowImageIconInFileSelectionTreeViewWhenMouseHovers = false;
                        ThumbSettingOptionIcon_Selected = ThumbSettingOptionIcon_Show;
                        break;
                    case c_ThumbSettingOption_OnHover:
                        App.CurrentState.ShowImageIconInFileSelectionTreeView = false;
                        App.CurrentState.ShowImageIconInFileSelectionTreeViewWhenMouseHovers = true;
                        ThumbSettingOptionIcon_Selected = ThumbSettingOptionIcon_OnHover;
                        break;
                    case c_ThumbSettingOption_NoThumb:
                        App.CurrentState.ShowImageIconInFileSelectionTreeView = false;
                        App.CurrentState.ShowImageIconInFileSelectionTreeViewWhenMouseHovers = false;
                        ThumbSettingOptionIcon_Selected = ThumbSettingOptionIcon_NoThumb;
                        break;
                }
            }
        }

        private void SetThumbSettingOptionIcon_Selected()
        {
            if (App.CurrentState.ShowImageIconInFileSelectionTreeView == false && App.CurrentState.ShowImageIconInFileSelectionTreeViewWhenMouseHovers == false)
                ThumbSettingOptionIcon_Selected = ThumbSettingOptionIcon_NoThumb;
            if (App.CurrentState.ShowImageIconInFileSelectionTreeView == true && App.CurrentState.ShowImageIconInFileSelectionTreeViewWhenMouseHovers == false)
                ThumbSettingOptionIcon_Selected = ThumbSettingOptionIcon_Show;
            if (App.CurrentState.ShowImageIconInFileSelectionTreeView == false && App.CurrentState.ShowImageIconInFileSelectionTreeViewWhenMouseHovers == true)
                ThumbSettingOptionIcon_Selected = ThumbSettingOptionIcon_OnHover;
        }
    }
}
