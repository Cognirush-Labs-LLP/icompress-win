using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using miCompressor.core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace miCompressor.ui
{
    /// <summary>
    /// Shows Selected Item (file or folder), i.e. each SelectedPath in FileStore
    /// </summary>
    public sealed partial class SelectedItem : Page
    {
        /// <summary>
        /// Gets or sets the selected path.
        /// </summary>
        public SelectedPath SelectedPath
        {
            get => (SelectedPath)GetValue(SelectedPathProperty);
            set => SetValue(SelectedPathProperty, value);
        }

        public bool ScannedAllFiles => !(SelectedPath?.ScanningForFiles ?? false);

        /// <summary>
        /// Identifies the <see cref="SelectedPath"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedPathProperty =
            DependencyProperty.Register("SelectedPath", typeof(SelectedPath), typeof(SelectedItem), new PropertyMetadata(null));

        public event EventHandler<SelectedPath>? SelectedPathDeleted;

        public SelectedItem()
        {
            this.InitializeComponent();
        }

        private void OnDeleteButtonClicked(object sender, RoutedEventArgs e)
        {
            // Trigger the event, notifying the parent
            SelectedPathDeleted?.Invoke(this, SelectedPath);

            // Optionally, clear or reset the `SelectedPath` here if needed
        }
    }
}
