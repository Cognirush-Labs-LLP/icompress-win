using miCompressor.core;
using miCompressor.ui.viewmodel;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Threading.Tasks;

namespace miCompressor.viewmodel
{
    /// <summary>
    /// ViewModel for MasterView to manage UI navigation.
    /// </summary>
    public partial class MasterState 
    {

        /// <summary>
        /// Comes from Static variable of App
        /// </summary>
        public FileStore FileStore { get; private set; }

        /// <summary>
        /// Comes from Static variable of App
        /// </summary>
        public OutputSettings OutputSettings { get; private set; }

        public CompressionViewModel CompressionViewModel { get; private set; }

        [AutoNotify]
        private bool showImageIconInFileSelectionTreeView = false;

        [AutoNotify]
        private bool showImageIconInFileSelectionTreeViewWhenMouseHovers = false;

        [AutoNotify]
        public bool showCompressionProgress = false;

        [AutoNotify]
        public bool showPreview = false;

        [AutoNotify]
        public bool showFilterOptions = true;

        [AutoNotify]
        public MediaFileInfo? selectedImageForPreview = null;

        /// <summary>
        /// True while the Edit screen is open. It sits over whichever screen opened it, so closing it returns there.
        /// </summary>
        [AutoNotify]
        public bool showEdit = false;

        [AutoNotify]
        public MediaFileInfo? imageToEdit = null;

        public bool ShowGalleryScreen => !ShowPreview && !ShowEdit;

        public bool ShowPreviewScreen => ShowPreview && !ShowEdit;

        private bool _isOpeningEdit = false;

        /// <summary>
        /// Opens the Edit screen, or explains why the image can't be edited.
        /// </summary>
        public async Task TryOpenEditAsync(MediaFileInfo media)
        {
            if (_isOpeningEdit)
                return;

            _isOpeningEdit = true;
            try
            {
                if (await Task.Run(() => ImageEditor.CanEdit(media.FilePath)))
                {
                    ImageToEdit = media;
                    ShowEdit = true;
                    return;
                }

                ContentDialog dialog = new()
                {
                    Title = "This image can't be edited",
                    Content = "GIF, SVG, animated and multi-page images can't be edited.",
                    CloseButtonText = "OK",
                    XamlRoot = App.MainWindow!.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
            finally
            {
                _isOpeningEdit = false;
            }
        }

        public Filter SelectionFilter = new();

        #region Microsoft Store Related Settings
        public bool IsOnMicrosoftStore => CodeConsts.IsForStoreDistribution;

        public string SoftwareRatingLink => IsOnMicrosoftStore ? "ms-windows-store://review/?ProductId=9NF6R54S63L3" : "https://sourceforge.net/projects/icompress/reviews/new";

        #endregion

        /// <summary>
        /// Constructor initializes with `EmptyFilesView`
        /// </summary>
        public MasterState()
        {
            FileStore = new();
            OutputSettings = new();
            CompressionViewModel = new(FileStore, OutputSettings);

            ShowImageIconInFileSelectionTreeView = AppSettingsManager.Get<bool>("Visuals." + nameof(ShowImageIconInFileSelectionTreeView));
            ShowImageIconInFileSelectionTreeViewWhenMouseHovers = AppSettingsManager.Get<bool>("Visuals." + nameof(ShowImageIconInFileSelectionTreeViewWhenMouseHovers));
            showFilterOptions = AppSettingsManager.Get<bool>("Visuals." + nameof(ShowFilterOptions), true);

            this.PropertyChanged += MasterState_PropertyChanged;
            this.CompressionViewModel.PropertyChanged += CompressionViewModel_PropertyChanged;
            this.FileStore.PropertyChanged += FileStore_PropertyChanged;
            this.OutputSettings.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName != nameof(OutputSettings.Format))
                    return;
                OnPropertyChanged(nameof(IsIconOutput));
                if (IsIconOutput)
                    ShowPreview = false;
            };
        }

        /// <summary>
        /// Icon output has no preview and ignores quality, resize, watermark, metadata and file time settings, so the UI hides them.
        /// </summary>
        public bool IsIconOutput => OutputSettings.Format == OutputFormat.Ico;

        private void FileStore_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(FileStore.SelectedPaths))
            {
                foreach (var selectedPath in FileStore.SelectedPaths)
                {
                    selectedPath.PropertyChanged -= SelectedPath_PropertyChanged;
                    selectedPath.PropertyChanged += SelectedPath_PropertyChanged;
                }
            }
        }

        private void SelectedPath_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(SelectedPath.Files))
            {
                SelectedPath? selectedPath = sender as SelectedPath;
                if (selectedPath == null) return;

                foreach (var file in selectedPath.Files)
                    SelectionFilter.Apply(file);
            }
        }

        private void CompressionViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(CompressionViewModel.CompressionInProgress))
            {
                if (CompressionViewModel.CompressionInProgress)
                    ShowCompressionProgress = true;
            }
        }

        private void MasterState_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ShowPreview) || e.PropertyName == nameof(ShowEdit))
            {
                OnPropertyChanged(nameof(ShowGalleryScreen));
                OnPropertyChanged(nameof(ShowPreviewScreen));
            }

            if (e.PropertyName == nameof(ShowImageIconInFileSelectionTreeView))
                AppSettingsManager.Set("Visuals." + nameof(ShowImageIconInFileSelectionTreeView), showImageIconInFileSelectionTreeView);
            if (e.PropertyName == nameof(ShowImageIconInFileSelectionTreeViewWhenMouseHovers))
                AppSettingsManager.Set("Visuals." + nameof(ShowImageIconInFileSelectionTreeViewWhenMouseHovers), ShowImageIconInFileSelectionTreeViewWhenMouseHovers);
            if (e.PropertyName == nameof(ShowFilterOptions))
                AppSettingsManager.Set("Visuals." + nameof(ShowFilterOptions), ShowFilterOptions);
        }
    }
}
