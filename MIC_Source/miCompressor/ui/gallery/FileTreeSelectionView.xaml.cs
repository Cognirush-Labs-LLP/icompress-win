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
    }
}
