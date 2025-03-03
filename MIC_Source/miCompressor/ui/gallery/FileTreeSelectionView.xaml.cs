using miCompressor.core;
using miCompressor.ui.viewmodel;
using miCompressor.viewmodel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using System;
using System.IO;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace miCompressor.ui
{
    public sealed partial class FileTreeSelectionView : UserControl
    {
        public GroupedImageGalleryViewModel ViewModel { get; } = new GroupedImageGalleryViewModel();
        public MasterState CurrentState => App.CurrentState;


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
            this.DataContext = this;
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
            if (_selectedNode != null && Directory.Exists(_selectedNode.FullPath))
            {
                await Launcher.LaunchFolderPathAsync(_selectedNode.FullPath);
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

        #region Image Flyout
        
        private void Image_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (!CurrentState.ShowImageIconInFileSelectionTreeViewWhenMouseHovers)
                return; //do not display hover thumbnail if showing image in the treeview.

            if (sender is FrameworkElement parent && parent.DataContext is ImageTreeNode node)
            {
                if (node.FileInfo == null)
                    return;
                var flyout = FlyoutBase.GetAttachedFlyout(parent) as Flyout;
                if (flyout.IsOpen) return;
                flyout.ShowAt(parent);
                var previewImage = (flyout.Content as Image);

                if (previewImage != null)
                {
                    CapturePointer(e.Pointer);
                    ThrottleTask.Add(90, "FileTreeSelectionView_Image_PointerEntered", () =>
                    {
                        //previewImage.Source = new BitmapImage(new Uri(node.FileInfo.FileToCompress.FullName));
                        if (flyout.IsOpen) return;
                        //flyout.ShowAt(parent);
                    }, shouldRunInUI: true);
                }
            }
            e.Handled = true;
            System.Diagnostics.Debug.WriteLine("Pinter Entered");
        }

        private async void Image_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            if (!CurrentState.ShowImageIconInFileSelectionTreeViewWhenMouseHovers)
                return; //do not display hover thumbnail if showing image in the treeview.

            if (sender is FrameworkElement parent)
            {
                ThrottleTask.Add(110, "FileTreeSelectionView_Image_PointerExited", () =>
                {
                    var flyout = FlyoutBase.GetAttachedFlyout(parent) as Flyout;
                    flyout?.Hide();
                }, shouldRunInUI: true);
            }
            e.Handled = true;
            
            System.Diagnostics.Debug.WriteLine("Pinter EXITED");
        }

        private void Image_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            e.Handled = true;
        }
        #endregion


    }
}
