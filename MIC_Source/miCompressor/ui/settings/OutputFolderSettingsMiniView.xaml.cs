using miCompressor.core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Windows.Storage;
using Windows.Storage.Pickers;

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
            if (e.PropertyName == nameof(OutputSettings.OutputFolder))
            {
                FoderPathTextBox.Text = OutputSettings.OutputFolder;
            }
        }

        private async void PickFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button folderPickerButton)
            {
                folderPickerButton.IsEnabled = false;

                string? selectedFolderPath = await FolderPickerHelper.PickFolderAsync(App.MainWindow!);
                if (selectedFolderPath != null)
                    FoderPathTextBox.Text = selectedFolderPath;

                folderPickerButton.IsEnabled = true;
            }
        }

        public string FolderPathError = String.Empty;
        private void FolderPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(FoderPathTextBox.Text))
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

        private void OutputLocationSettingsDropDownButton_Loaded(object sender, RoutedEventArgs e)
        {
            OutputLocationMenuFlyout.Items.Clear();
            foreach (var setting in OutputLocationSettings)
            {
                var menuItem = new MenuFlyoutItem()
                {
                    Text = setting.Name,
                    Tag = setting 
                };
                menuItem.Click += SelectedOutputLocationSettings_Click;
                OutputLocationMenuFlyout.Items.Add(menuItem);
            }
        }

        // When a flyout item is selected, update the bound property.
        private void SelectedOutputLocationSettings_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item)
                SelectedOutputLocationSettingsItem = item.Tag as OutputLocationSettingsItem;
        }
    }
}
