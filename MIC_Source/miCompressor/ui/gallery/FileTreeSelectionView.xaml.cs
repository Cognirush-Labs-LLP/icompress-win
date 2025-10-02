using miCompressor.core;
using miCompressor.ui.viewmodel;
using miCompressor.viewmodel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.IO;
using System.Windows.Input;
using Windows.Foundation;
using Windows.Graphics;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace miCompressor.ui
{
    public sealed partial class FileTreeSelectionView : UserControl
    {
        public GroupedImageGalleryViewModel ViewModel { get; } = new GroupedImageGalleryViewModel();
        public MasterState CurrentState => App.CurrentState;
        public FileTreeSelectionView ThisObj => this;


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

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        #region Treeview height
        private const double BottomMargin = 30;
        private double _lastAppliedMaxHeight = -1;
        private bool _inUpdate;

        
        private ScrollViewer _parentScroll;

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Prefer SizeChanged over LayoutUpdated to avoid tight loops
            this.SizeChanged += OnAnySizeChanged;
            TreeContainer.SizeChanged += OnAnySizeChanged; // your Grid x:Name="TreeContainer"

            if (App.MainWindow is Window w)
                w.SizeChanged += OnWindowSizeChanged;

            _parentScroll = FindAncestorScrollViewer(this);
            if (_parentScroll != null)
                _parentScroll.ViewChanged += OnParentScrollChanged;

            UpdateTreeMaxHeight();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            this.SizeChanged -= OnAnySizeChanged;
            TreeContainer.SizeChanged -= OnAnySizeChanged;

            if (App.MainWindow is Window w)
                w.SizeChanged -= OnWindowSizeChanged;

            if (_parentScroll != null)
            {
                _parentScroll.ViewChanged -= OnParentScrollChanged;
                _parentScroll = null;
            }
        }

        private void OnAnySizeChanged(object sender, SizeChangedEventArgs e) => UpdateTreeMaxHeight();
        private void OnWindowSizeChanged(object sender, WindowSizeChangedEventArgs e) => UpdateTreeMaxHeight();
        private void OnParentScrollChanged(object sender, ScrollViewerViewChangedEventArgs e) => UpdateTreeMaxHeight();

        private void UpdateTreeMaxHeight()
        {
            // Reentrancy guard
            if (_inUpdate) return;
            _inUpdate = true;
            try
            {
                if (SelectedPathTreeView == null || App.MainWindow is not Window win) return;
                if (win.Content is not FrameworkElement windowRoot) return;

                // Window client height (DIPs)
                SizeInt32 client = win.AppWindow?.ClientSize ?? new SizeInt32(1200, 800);
                double windowClientHeight = client.Height;

                if(_parentScroll != null)
                    windowClientHeight -= _parentScroll.TransformToVisual(windowRoot).TransformPoint(new Point(0, 0)).Y;


                // Y of TreeView relative to window content root
                GeneralTransform t = SelectedPathTreeView.TransformToVisual(windowRoot);
                Point topLeft = t.TransformPoint(new Point(0, 0));
                double y = topLeft.Y;

                // Compute target; clamp & stabilize to integer to avoid 1px oscillations
                double target = Math.Max(50, Math.Floor(windowClientHeight - BottomMargin));

                // Only set when it actually changes by >= 1 px
                if (Math.Abs(target - _lastAppliedMaxHeight) >= 1)
                {
                    SelectedPathTreeView.MaxHeight = target;
                    _lastAppliedMaxHeight = target;
                }
            }
            catch
            {
                // Transform can throw during early template churn; ignore and next event will retry
            }
            finally
            {
                _inUpdate = false;
            }
        }

        private static ScrollViewer FindAncestorScrollViewer(DependencyObject start)
        {
            for (DependencyObject d = start; d != null; d = VisualTreeHelper.GetParent(d))
                if (d is ScrollViewer sv) return sv;
            return null;
        }
        #endregion


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
            if (_selectedNode != null)
            {
                await Launcher.LaunchFolderPathAsync(PathHelper.GetFolderPath(_selectedNode.FullPath));
            }
        }

        // Open Image with Default Viewer
        private async void OpenFile_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_selectedNode != null && File.Exists(_selectedNode.Name))
            {
                await Launcher.LaunchFolderPathAsync(PathHelper.GetFolderPath(_selectedNode.FullPath));
            }
        }

        private void OpenCompressionPreview(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (_selectedNode != null)
            {
                var mediaInfo = _selectedNode.GetAnyMediaInfo(_selectedNode);
                if (mediaInfo != null)
                {
                    App.CurrentState.SelectedImageForPreview = mediaInfo;
                    App.CurrentState.ShowPreview = true;
                }
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

        public ICommand ShowPreviewCommand => new RelayCommand<object>(param =>
        {
            if (param as MediaFileInfo == null)
                return;

            CurrentState.SelectedImageForPreview = param as MediaFileInfo;
            CurrentState.ShowPreview = true;
        });

        public ICommand ShowAnyPreviewCommand => new RelayCommand<object>(param =>
        {
            if (param as ImageTreeNode != null)
            {
                var node = param as ImageTreeNode;
                var mediaInfo = node.GetAnyMediaInfo(node);
                if (mediaInfo != null)
                {
                    App.CurrentState.SelectedImageForPreview = mediaInfo;
                    App.CurrentState.ShowPreview = true;
                }
            }
        });
    }
}
