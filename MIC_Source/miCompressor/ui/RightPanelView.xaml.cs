using miCompressor.viewmodel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Threading.Tasks;

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
        CurrentState.ShowPreview = false;
        (bool good, string error) = CurrentState.CompressionViewModel.CheckSettingsCondition();
        if (!good)
        {
            if (CurrentState.OutputSettings.OutputLocationSettings == core.OutputLocationSetting.UserSpecificFolder)
            {
                var dialog = new ContentDialog
                {
                    Title = "Select Output Folder",
                    Content = "You need to select output folder to store compressed images.",
                    PrimaryButtonText = "Select Output Folder",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = ((FrameworkElement)this.Content).XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
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
                else
                {
                    return;
                }
            }
            else
            {
                await ShowOutputFolderSettingError(error);
                return;
            }
        }

        (bool alreadyInProgress, bool nothingToCompres) = CurrentState.CompressionViewModel.StartCompression();

        if (nothingToCompres)
            ShowNothingToCompressMessageDialog();
    }

    private static async Task ShowNothingToCompressMessageDialog()
    {
        ContentDialog dialog = new()
        {
            Title = "Nothing to Compress.",
            Content = "You have not selected any image for compression. Happens if you, \n 1. Unchecked all selected Images. \n 2. Forget to add Images for compression. \n 3. Forgot to check Sub-Folders option.",
            CloseButtonText = "OK",
            XamlRoot = App.MainWindow!.Content.XamlRoot
        };

        await dialog.ShowAsync();
    }

    private static async Task ShowOutputFolderSettingError(string ErrorText)
    {
        ContentDialog dialog = new()
        {
            Title = "Output Setting Needs a Relook",
            Content = ErrorText,
            CloseButtonText = "OK",
            XamlRoot = App.MainWindow!.Content.XamlRoot
        };

        await dialog.ShowAsync();
    }
}
