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
        public FileStore FileStore { get; } = App.FileStoreInstance;

        /// <summary>
        /// Comes from Static variable of App
        /// </summary>
        public OutputSettings OutputSettings { get; } = App.OutputSettingsInstance;

        /// <summary>
        /// Constructor initializes with `EmptyFilesView`
        /// </summary>
        public MasterState()
        {
        }
    }
}
