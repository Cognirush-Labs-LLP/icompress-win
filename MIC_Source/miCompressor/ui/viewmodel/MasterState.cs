using miCompressor.core;
using System.ComponentModel;
using miCompressor.ui;
using System.Linq;
using Microsoft.UI.Xaml.Controls;
using System;

namespace miCompressor.viewmodels
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

        [AutoNotify]
        private bool showImageIconInFileSelectionTreeView = false;

        [AutoNotify]
        private bool showImageIconInFileSelectionTreeViewWhenMouseHovers = false;

        /// <summary>
        /// Constructor initializes with `EmptyFilesView`
        /// </summary>
        public MasterState()
        {
            FileStore = new();
            OutputSettings = new();

            showImageIconInFileSelectionTreeView = AppSettingsManager.Get<bool>("Visuals." + nameof(ShowImageIconInFileSelectionTreeView));
            ShowImageIconInFileSelectionTreeViewWhenMouseHovers = AppSettingsManager.Get<bool>("Visuals." + nameof(ShowImageIconInFileSelectionTreeViewWhenMouseHovers));

            this.PropertyChanged += MasterState_PropertyChanged;
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
