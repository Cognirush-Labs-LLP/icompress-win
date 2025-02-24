using miCompressor.core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using Windows.Storage;
using miCompressor.core;
using static miCompressor.ui.OutputFileSettingsView;
using miCompressor.core.common;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace miCompressor.ui
{
    public sealed partial class OutputFolderSettingsMiniView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        public class OutputLocationSettingsItem
        {
            public OutputLocationSetting Value { get; }
            public string Name { get; }

            public OutputLocationSettingsItem(OutputLocationSetting value)
            {
                Value = value;
                Name = value.GetDescription();
            }
        }
        public OutputSettings OutputSettings { get; set; }

        public List<OutputLocationSettingsItem> OutputLocationSettings { get; }

        private OutputLocationSettingsItem _outputLocationSettingsItem;
        public OutputLocationSettingsItem SelectedOutputLocationSettingsItem
        {
            get => _outputLocationSettingsItem;
            set
            {
                if (_outputLocationSettingsItem == value)
                    return;
                _outputLocationSettingsItem = value;
                OutputSettings.OutputLocationSettings = value.Value;
                OutputFolderUI.Visibility = value.Value == OutputLocationSetting.UserSpecificFolder ? Visibility.
                    Visible : Visibility.Collapsed;
                if (value.Value == OutputLocationSetting.ReplaceOriginal)
                    OutputSettings.Format = OutputFormat.KeepSame;
            }
        }

        public OutputFolderSettingsMiniView()
        {
            this.InitializeComponent();
            OutputSettings = App.OutputSettingsInstance;

            OutputLocationSettings = Enum.GetValues<OutputLocationSetting>().Select(x => new OutputLocationSettingsItem(x)).ToList();

            _outputLocationSettingsItem = OutputLocationSettings.FirstOrDefault(o => o.Value == OutputSettings.OutputLocationSettings) ?? OutputLocationSettings.First();

            OutputSettings.PropertyChanged += OutputSettings_PropertyChanged;
        }

        private void OutputSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OutputSettings.OutputLocationSettings))
            {
                _outputLocationSettingsItem = OutputLocationSettings.FirstOrDefault(o => o.Value == OutputSettings.OutputLocationSettings) ?? OutputLocationSettings.First();
                OnPropertyChanged(nameof(SelectedOutputLocationSettingsItem));
            }
        }

        private async void PickFolderButton_Click(object sender, RoutedEventArgs e)
        {
            //disable the button to avoid double-clicking
            var folderPickerButton = sender as Button;
            folderPickerButton.IsEnabled = false;

            FolderPicker openPicker = new Windows.Storage.Pickers.FolderPicker();

            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow!);
            WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

            // Set options for your folder picker
            //openPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            openPicker.FileTypeFilter.Add("*");

            // Open the picker for the user to pick a folder
            StorageFolder folder = await openPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                //StorageApplicationPermissions.FutureAccessList.AddOrReplace("PickedFolderToken", folder);
                FoderPathTextBox.Text = folder.Path;
            }

            //re-enable the button
            folderPickerButton.IsEnabled = true;
        }

        
        public string FolderPathError = String.Empty;
        private void FoderPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(String.IsNullOrWhiteSpace(FoderPathTextBox.Text))
            {
                OutputSettings.outputFolder = String.Empty;
                FolderPathError = "Provide output folder path.";
                OnPropertyChanged(nameof(FolderPathError));
                return;
            }

            if (PathValidator.IsValidFolderPath(FoderPathTextBox.Text, out string error))
                OutputSettings.outputFolder = FoderPathTextBox.Text.Trim();
            else
                OutputSettings.outputFolder = String.Empty;

            FolderPathError = error;
            OnPropertyChanged(nameof(FolderPathError));
        }
    }
}
