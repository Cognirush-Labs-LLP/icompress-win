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
using System.ComponentModel;
using System.Runtime.CompilerServices;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace miCompressor.ui
{
    /// <summary>
    /// A small control to toggle watermark application and launch watermark settings.
    /// </summary>
    public sealed partial class ApplyWatermarkToggleView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private bool SettingOpenAsApplyWatermarkWasChecked = false;

        /// <summary>
        /// Binds to OutputSettings.ApplyWatermark.
        /// </summary>
        public bool ApplyWatermark
        {
            get => App.OutputSettingsInstance.ApplyWatermark;
            set
            {
                if(value == false)
                {
                    App.OutputSettingsInstance.ApplyWatermark = value;
                    OnPropertyChanged();
                    return;
                }

                if (WatermarkHelper.IsWatermarkConfigured())
                {
                    App.OutputSettingsInstance.ApplyWatermark = value;
                } else
                {
                    SettingOpenAsApplyWatermarkWasChecked = true;
                    App.OutputSettingsInstance.ApplyWatermark = false;
                    OnPropertyChanged();
                    OpenSettingsDialog();
                }
                OnPropertyChanged();
            }
        }

        public ApplyWatermarkToggleView()
        {
            InitializeComponent();
            // DataContext for x:Bind
            DataContext = this;
            ApplyWatermark = ApplyWatermark && WatermarkHelper.IsWatermarkConfigured();
        }

        private async void OnSettingsButtonClick(object sender, RoutedEventArgs e)
        {
            OpenSettingsDialog();
        }

        private async void OpenSettingsDialog()
        {
            var dialog = new ContentDialog
            {
                Title = "Watermark Settings",
                Content = new WatermarkSettingsView(),
                CloseButtonText = "Close",
                XamlRoot = this.XamlRoot,
                MinHeight = 500,
                MinWidth = 300
            };


            await dialog.ShowAsync();
            if(SettingOpenAsApplyWatermarkWasChecked)
            {
                SettingOpenAsApplyWatermarkWasChecked = false;
                ApplyWatermark = WatermarkHelper.IsWatermarkConfigured();
            } else
            {
                ApplyWatermark = ApplyWatermark && WatermarkHelper.IsWatermarkConfigured();
            }
        }

    }
}
