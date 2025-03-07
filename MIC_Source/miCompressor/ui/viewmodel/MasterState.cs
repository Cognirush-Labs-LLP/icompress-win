using miCompressor.core;
using System.ComponentModel;
using miCompressor.ui;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using System;
using miCompressor.ui.viewmodel;
using Windows.ApplicationModel.UserDataTasks;

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

        public Filter SelectionFilter = new();

        /// <summary>
        /// Constructor initializes with `EmptyFilesView`
        /// </summary>
        public MasterState()
        {
            FileStore = new();
            OutputSettings = new();
            CompressionViewModel = new(FileStore, OutputSettings);

            showImageIconInFileSelectionTreeView = AppSettingsManager.Get<bool>("Visuals." + nameof(ShowImageIconInFileSelectionTreeView));
            ShowImageIconInFileSelectionTreeViewWhenMouseHovers = AppSettingsManager.Get<bool>("Visuals." + nameof(ShowImageIconInFileSelectionTreeViewWhenMouseHovers));

            this.PropertyChanged += MasterState_PropertyChanged;
            this.CompressionViewModel.PropertyChanged += CompressionViewModel_PropertyChanged;
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
            if (e.PropertyName == nameof(ShowImageIconInFileSelectionTreeView))
                AppSettingsManager.Set("Visuals." + nameof(ShowImageIconInFileSelectionTreeView), showImageIconInFileSelectionTreeView);
            if (e.PropertyName == nameof(ShowImageIconInFileSelectionTreeViewWhenMouseHovers))
                AppSettingsManager.Set("Visuals." + nameof(ShowImageIconInFileSelectionTreeViewWhenMouseHovers), ShowImageIconInFileSelectionTreeViewWhenMouseHovers);
        }
    }
}
