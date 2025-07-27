using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using miCompressor.core;
using System.ComponentModel;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace miCompressor.ui
{
    public sealed partial class InputSelectionView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            UIThreadHelper.RunOnUIThread(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        public InputSelectionView()
        {
            this.InitializeComponent();
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
                    App.FileStoreInstance.Enqueue(inputPathTxt);
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

        public bool InputPathExists
        {
            get
            {                
                return !String.IsNullOrEmpty(InputPathTextBox.Text) && !App.FileStoreInstance.IsPathAdded(InputPathTextBox.Text) && (File.Exists(InputPathTextBox.Text.Trim()) || Directory.Exists(InputPathTextBox.Text.Trim()));
            }
        }

        private void InputPathTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            OnPropertyChanged(nameof(InputPathExists));
        }

        private async void PickFolderButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button folderPickerButton)
            {
                folderPickerButton.IsEnabled = false;

                string? selectedFolderPath = await FolderPickerHelper.PickFolderAsync(App.MainWindow!);
                if (selectedFolderPath != null)
                {
                    InputPathTextBox.Text = selectedFolderPath;
                    if(InputPathExists)
                    {
                        string inputPathTxt = (InputPathTextBox.Text ?? "").Trim();
                        App.FileStoreInstance.Enqueue(inputPathTxt);
                    }

                }

                folderPickerButton.IsEnabled = true;
            }
        }
    }
}
