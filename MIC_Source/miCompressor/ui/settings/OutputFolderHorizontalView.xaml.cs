using miCompressor.core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace miCompressor.ui
{
    public sealed partial class OutputFolderHorizontalView : UserControl, INotifyPropertyChanged
    {

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            UIThreadHelper.RunOnUIThread(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        public bool ShowOutputFolderUI => OutputSettings.OutputLocationSettings == OutputLocationSetting.UserSpecificFolder;

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

        public bool IsReplaceOperation => this.OutputSettings.OutputLocationSettings == OutputLocationSetting.ReplaceOriginal;

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

        public OutputFolderHorizontalView()
        {
            this.InitializeComponent();
            OutputSettings = App.OutputSettingsInstance;

            OutputLocationSettings = Enum.GetValues<OutputLocationSetting>().Select(x => new OutputLocationSettingsItem(x)).ToList();

            _outputLocationSettingsItem = OutputLocationSettings.FirstOrDefault(o => o.Value == OutputSettings.OutputLocationSettings) ?? OutputLocationSettings.First();

            OutputSettings.PropertyChanged += OutputSettings_PropertyChanged;
            App.FileStoreInstance.PropertyChanged += FileStore_PropertyChanged;
            SetSuggestionText();
        }

        private void OutputSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OutputSettings.OutputLocationSettings))
            {
                _outputLocationSettingsItem = OutputLocationSettings.FirstOrDefault(o => o.Value == OutputSettings.OutputLocationSettings) ?? OutputLocationSettings.First();
                OnPropertyChanged(nameof(SelectedOutputLocationSettingsItem));
                SetSuggestionText();
            }
            if (e.PropertyName == nameof(OutputSettings.OutputFolder))
                FoderPathTextBox.Text = OutputSettings.OutputFolder;
            else
            {
                FolderPathError = String.Empty;
                OnPropertyChanged(nameof(FolderPathError));
            }

            if (e.PropertyName == nameof(OutputSettings.Prefix) || e.PropertyName == nameof(OutputSettings.Suffix))
                SetSuggestionText();

            OnPropertyChanged(nameof(ShowOutputFolderUI));
            OnPropertyChanged(nameof(IsReplaceOperation));            
        }

        private void SetSuggestionText()
        {
            switch (OutputSettings.OutputLocationSettings)
            {
                case OutputLocationSetting.ReplaceOriginal:
                    SuggestionTextBlock.Text = "Original files will be lost. Keep backup.";
                    break;
                case OutputLocationSetting.SameFolderWithFileNameSuffix:
                    if (string.IsNullOrWhiteSpace(OutputSettings.prefix) && string.IsNullOrWhiteSpace(OutputSettings.suffix))
                        SuggestionTextBlock.Text = "Provide Prefix/Suffix in settings panel 🡒 ";
                    else
                        SuggestionTextBlock.Text = "";
                    break;
                default:
                    SuggestionTextBlock.Text = "";
                    break;
            }
        }

        private void FileStore_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(App.FileStoreInstance.SelectedPaths))
            {
                if (App.FileStoreInstance.SelectedPathCount == 1)
                    OutputSettings.DoAutoSetOutDir(GetCompressDirPath(App.FileStoreInstance.GetFirstPath()));
                else if (App.FileStoreInstance.SelectedPathCount > 1)
                    OutputSettings.DoAutoSetCompressedDir();
            }
        }

        private string GetCompressDirPath(string selectedPath)
        {
            string path = selectedPath;

            if (string.IsNullOrWhiteSpace(path) || !Path.IsPathRooted(path))
                return null;

            string compressedDirName = CodeConsts.compressedDirName;

            string? outputPath = null;

            // Prefer Directory.Exists/File.Exists if the path exists, otherwise fall back to extension heuristics.
            if (Directory.Exists(path))
            {
                outputPath = Path.Combine(path, compressedDirName);
            }
            else if (File.Exists(path) || Path.HasExtension(path))
            {
                string? parent = Path.GetDirectoryName(path);
                if (string.IsNullOrEmpty(parent))
                    parent = Path.GetPathRoot(path);

                if (!string.IsNullOrEmpty(parent))
                    outputPath = Path.Combine(parent, compressedDirName);
            }
            else
                return null;

            return outputPath;
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
                OutputSettings.OutputFolder = String.Empty;
                FolderPathError = "Provide output folder path.";
                OnPropertyChanged(nameof(FolderPathError));
                return;
            }

            if (PathValidator.IsValidFolderPath(FoderPathTextBox.Text, out string error))
                OutputSettings.OutputFolder = FoderPathTextBox.Text.Trim();
            else
                OutputSettings.OutputFolder = String.Empty;

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
