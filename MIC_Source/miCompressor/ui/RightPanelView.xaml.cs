using miCompressor.viewmodel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.IO;

namespace miCompressor.ui;

public sealed partial class RightPanelView : UserControl
{
    public MasterState CurrentState => App.CurrentState;

    public RightPanelView()
    {
        this.InitializeComponent();
    }
    private async void CompressAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (!CurrentState.CompressionViewModel.CheckSettingsCondition().good)
        {
            string? selectedFolderPath = await FolderPickerHelper.PickFolderAsync(App.MainWindow!);
            if (selectedFolderPath != null && Directory.Exists(selectedFolderPath))
            {
                CurrentState.OutputSettings.OutputLocationSettings = core.OutputLocationSetting.UserSpecificFolder;
                CurrentState.OutputSettings.OutputFolder = selectedFolderPath.Trim();
            }
            else
                return;
        }
        
        CurrentState.CompressionViewModel.StartCompression();

    }
}
