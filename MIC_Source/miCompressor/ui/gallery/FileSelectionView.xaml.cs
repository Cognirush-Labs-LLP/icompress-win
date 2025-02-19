using miCompressor.core;
using miCompressor.viewmodels;
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
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class FileSelectionView : UserControl
    {
        public FileSelectionView()
        {
            this.InitializeComponent();
            //this.DataContext = (this.Parent as Page?).DataContent; // Inherit DataContext from Parent (MasterView)
        }

        private void SelectedItem_SelectedPathDeleted(object sender, SelectedPath e)
        {
            // Handle the deletion, such as removing the path from a collection or refreshing the UI
            App.FileStoreInstance.Remove(e.Path);
        }

        private void Remove_All_Click(object sender, RoutedEventArgs e)
        {
            App.FileStoreInstance.RemoveAll();
        }
    }
}
