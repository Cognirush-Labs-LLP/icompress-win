using miCompressor.core;
using miCompressor.ui.viewmodel;
using miCompressor.viewmodel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace miCompressor.ui
{
    public sealed partial class FileImageTreeView : UserControl
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
            var control = (FileImageTreeView)d;
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

        public FileImageTreeView()
        {
            this.InitializeComponent();
            this.DataContext = this;
        }
    }

    public class ImageFolderTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ImageTemplate { get; set; }
        public DataTemplate FolderTemplate { get; set; }

        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if (item is ImageTreeNode node)
            {
                return node.IsImage ? ImageTemplate : FolderTemplate;
            }
            return base.SelectTemplateCore(item, container);
        }
    }

}
