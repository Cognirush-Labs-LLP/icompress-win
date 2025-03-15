using miCompressor.core;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using static miCompressor.ui.DimensionSettingsView;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace miCompressor.ui
{
    public sealed partial class OutputFileSettingsView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            UIThreadHelper.RunOnUIThread(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            });
        }

        /// <summary>
        /// Wrapper class to hold a DimensionReductionStrategy enum value and a user-friendly display name.
        /// </summary>
        public class OutputFormatItem
        {
            public OutputFormat Value { get; }
            public string Name { get; }

            public OutputFormatItem(OutputFormat value)
            {
                Value = value;
                Name = value.GetDescription();
            }
        }

        public bool XMP_IPTC_CopySupported
        {
            get
            {
                return OutputSettings.CopyMetadata && !OutputSettings.SkipSensitiveMetadata;
            }
        }

        public OutputSettings OutputSettings { get; set; }

        public List<OutputFormatItem> OutputFormats { get; }

        private OutputFormatItem _outputFormatItem;
        public OutputFormatItem SelectedOutputFormatItem
        {
            get => _outputFormatItem;
            set
            {
                if (_outputFormatItem == value)
                    return;
                _outputFormatItem = value;
                OutputSettings.Format = value.Value;
            }
        }

        public OutputFileSettingsView()
        {
            this.InitializeComponent();
            OutputSettings = App.OutputSettingsInstance;
            OutputFormats = Enum.GetValues<OutputFormat>().Select(x => new OutputFormatItem(x)).ToList();
            _outputFormatItem = OutputFormats.FirstOrDefault(o => o.Value == OutputSettings.Format) ?? OutputFormats.First();

            OutputSettings.PropertyChanged += OutputSettings_PropertyChanged;
        }

        private void OutputSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(OutputSettings.Format))
            {
                _outputFormatItem = OutputFormats.FirstOrDefault(o => o.Value == OutputSettings.Format) ?? OutputFormats.First();
                OnPropertyChanged(nameof(SelectedOutputFormatItem));
            }
            if (e.PropertyName == nameof(OutputSettings.OutputLocationSettings))
            {
                DisableFileNameChangeOperations(OutputSettings.OutputLocationSettings == OutputLocationSetting.ReplaceOriginal);
            }
            if( e.PropertyName == nameof(OutputSettings.CopyMetadata) || e.PropertyName == nameof(OutputSettings.SkipSensitiveMetadata))
            {
                OnPropertyChanged(nameof(XMP_IPTC_CopySupported));
            }
        }

        private void DisableFileNameChangeOperations(bool disable)
        {
            FileNameOperationsUI.Children.OfType<Control>().ToList().ForEach(control => control.IsEnabled = !disable);
            OutputFormatCombobox.IsEnabled = !disable;
            var newOpacity = disable ? 0.5 : 1;
            FileNameOperationsUI.Opacity = newOpacity;
            //OutputFormatCombobox.Opacity = newOpacity;
            OutputFormatTextblock.Opacity = newOpacity;

            if(disable)
            {
                OutputSettings.Prefix = String.Empty;
                OutputSettings.Suffix = String.Empty;
                OutputSettings.ReplaceFrom = String.Empty;
                OutputSettings.ReplaceTo = String.Empty;
            }
        }
    }
}
