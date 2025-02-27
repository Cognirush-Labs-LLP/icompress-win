using miCompressor.core;
using miCompressor.ui.viewmodel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace miCompressor.ui
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SelectionDetailView : UserControl
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
            var control = (SelectionDetailView)d;
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
            DependencyProperty.Register("SelectedPath", typeof(SelectedPath), typeof(SelectionDetailView), new PropertyMetadata(null, OnSelectedPathChanged));

        public SelectionDetailView()
        {
            ViewModel.IsTreeView = false;
            this.InitializeComponent();
        }
    }
}
