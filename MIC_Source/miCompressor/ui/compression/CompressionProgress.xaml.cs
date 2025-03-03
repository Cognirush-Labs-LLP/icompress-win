using miCompressor.core;
using miCompressor.ui.viewmodel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace miCompressor.ui
{
    public sealed partial class CompressionProgress : UserControl
    {
        public CompressionViewModel vm => App.CurrentState.CompressionViewModel;

        [AutoNotify] public List<PieChartData> compressionProgressVisuals = new();

        public CompressionProgress()
        {
            this.InitializeComponent();

            vm.PropertyChanged += CompressionVM_PropertyChanged;
        }

        private void CompressionVM_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName != nameof(CompressionViewModel.TimeToShow))
            {
                var remaining = vm.TotalFilesToCompress - vm.TotalFilesCompressed - vm.TotalFilesFailedToCompress - vm.TotalFilesCancelled;

                CompressionProgressVisuals = new()
                {
                    new() { Label="Remaining", Value=remaining, Color = Color.FromArgb(255, 217, 217, 92) },
                    new() { Label="Processed", Value=vm.TotalFilesCompressed, Color = Color.FromArgb(255, 92, 184, 92) },
                    new() { Label="Failed", Value=vm.TotalFilesFailedToCompress, Color = Color.FromArgb(255,  217, 92, 108) },
                    new() { Label="Cancelled", Value= vm.TotalFilesCancelled, Color = Color.FromArgb(255,  92, 128, 217) },
                };
            }
        }

        private async void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            vm.CancelCompression();

        }

        private async void BackButton_Click(object sender, RoutedEventArgs e)
        {
            App.CurrentState.ShowCompressionProgress = false;
        }
    }
}
