using miCompressor.core;
using miCompressor.ui.viewmodel;
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
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace miCompressor.ui
{
    public sealed partial class FileTreeSelectionView : UserControl
    {
        public GroupedImageGalleryViewModel ViewModel { get; } = new GroupedImageGalleryViewModel();

        /// <summary>
        /// Gets or sets the selected path.
        /// </summary>
        public SelectedPath SelectedPath
        {
            get => (SelectedPath)GetValue(SelectedPathProperty);
            set => SetValue(SelectedPathProperty, value);
        }

        private static void OnSelectedPathChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (FileTreeSelectionView)d;
            if (e.NewValue is SelectedPath newPath)
            {
                if (newPath == null)
                    return;
                control.ViewModel.LoadData(newPath);
                System.Diagnostics.Debug.WriteLine($"Loaded Data for: {newPath.DisplayName}");
            }
        }

        /// <summary>
        /// Identifies the <see cref="SelectedPath"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedPathProperty =
            DependencyProperty.Register("SelectedPath", typeof(SelectedPath), typeof(FileTreeSelectionView), new PropertyMetadata(null, OnSelectedPathChanged));

        public FileTreeSelectionView()
        {
            ViewModel.IsTreeView = true;
            this.InitializeComponent();
        }

        private void OnCheckBoxClicked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is ImageTreeNode node)
            {
                node.IsIncluded = !node.IsIncluded; // Toggle selection without using SelectionState
            }
        }

        private ImageTreeNode _selectedNode; // Stores the last right-clicked folder node

        private void FolderTreeView_RightTapped(object sender, RightTappedRoutedEventArgs e)
        {
            if (e.OriginalSource is FrameworkElement element && element.DataContext is ImageTreeNode node)
            {
                _selectedNode = node;
            }
        }

        private async void OpenFolderInExplorer_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_selectedNode != null && Directory.Exists(_selectedNode.Name))
            {
                await Launcher.LaunchFolderPathAsync(_selectedNode.Name);
            }
        }

        // Open Image with Default Viewer
        private async void OpenFile_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_selectedNode != null && File.Exists(_selectedNode.Name))
            {
                await Launcher.LaunchFileAsync(await Windows.Storage.StorageFile.GetFileFromPathAsync(_selectedNode.Name));
            }
        }
    }
}
