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
using System.Windows.Input;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace miCompressor.ui
{
    using Strategy = miCompressor.core.DimensionReductionStrategy; // Alias for the enum

    public sealed partial class DimensionSettingsView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged(string propertyName) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        /// <summary>
        /// Wrapper class to hold a DimensionReductionStrategy enum value and a user-friendly display name.
        /// </summary>
        public class DimensionReductionStrategyItem
        {
            public Strategy Value { get; }
            public string Name { get; }

            public DimensionReductionStrategyItem(Strategy value)
            {
                Value = value;
                Name = value.GetDescription();
            }
        }

        public OutputSettings OutputSettings { get; set; }

        public PrintDimension[] CommonPrintDimensions = PrintDimension.GetCommonPrintDimensions();
        public PrintDimension _selectedPrintDim;
        public PrintDimension SelectedPrintDim
        {
            get
            {
                return _selectedPrintDim;
            }
            set
            {
                if (_selectedPrintDim == value)
                    return; //So events update doesn't go in infinite loop.

                _selectedPrintDim = value;
                if (_selectedPrintDim.shortEdgeInInch != (double)0) //i.e. if not custom values, custom values are taken from number box.
                {
                    OutputSettings.PrintDimension.CopyValues(_selectedPrintDim); //All events of property change related to long and short edge are raised only after setting all the property. 
                }
                OnPropertyChanged(nameof(SelectedPrintDim)); //HACK: this is somehow required :( WinUI 3 is somewhat unpredictable. 
            }
        }

        private DimensionReductionStrategyItem _selectedStrategy;

        public DimensionReductionStrategyItem SelectedStrategy
        {
            get
            {
                return _selectedStrategy;
            }
            set
            {
                _selectedStrategy = value;
                OutputSettings.DimensionStrategy = value.Value;
                OnPropertyChanged(nameof(SetPercentage));
                OnPropertyChanged(nameof(SetLongEdge));
                OnPropertyChanged(nameof(SetMaxHeight));
                OnPropertyChanged(nameof(SetMaxWidth));
                OnPropertyChanged(nameof(SetFixedHeight));
                OnPropertyChanged(nameof(SetFixedWidth));
                OnPropertyChanged(nameof(SetFitInFrame));
                OnPropertyChanged(nameof(SetFixedInFrame));
                OnPropertyChanged(nameof(SetLength));
                OnPropertyChanged(nameof(SetPrintSize));
            }
        }

        /// <summary>
        /// List of available dimension reduction strategies with friendly names (for UI)
        /// </summary>
        public List<DimensionReductionStrategyItem> DimensionStrategies { get; }

        public bool SetPercentage => OutputSettings.DimensionStrategy is Strategy.Percentage;
        public bool SetLongEdge => OutputSettings.DimensionStrategy is Strategy.LongEdge;
        public bool SetMaxHeight => OutputSettings.DimensionStrategy is Strategy.MaxHeight;
        public bool SetMaxWidth => OutputSettings.DimensionStrategy is Strategy.MaxWidth;
        public bool SetFixedHeight => OutputSettings.DimensionStrategy is Strategy.FixedHeight;
        public bool SetFixedWidth => OutputSettings.DimensionStrategy is Strategy.FixedWidth;
        public bool SetFitInFrame => OutputSettings.DimensionStrategy is Strategy.FitInFrame;
        public bool SetFixedInFrame => OutputSettings.DimensionStrategy is Strategy.FixedInFrame;
        public bool SetLength => OutputSettings.DimensionStrategy is
                    Strategy.LongEdge or Strategy.MaxHeight or Strategy.MaxWidth or Strategy.FixedHeight or Strategy.FixedWidth;
        public bool SetPrintSize => OutputSettings.DimensionStrategy is Strategy.FitInFrame or Strategy.FixedInFrame;

        public DimensionSettingsView()
        {
            this.InitializeComponent();

            OutputSettings = App.OutputSettingsInstance;

            DimensionStrategies = Enum.GetValues(typeof(DimensionReductionStrategy))
               .Cast<DimensionReductionStrategy>()
               .Select(strategy => new DimensionReductionStrategyItem(strategy))
               .ToList();

            SelectedStrategy = DimensionStrategies.Find(strategy => strategy.Value == OutputSettings.DimensionStrategy) ?? DimensionStrategies.FirstOrDefault();

            OutputSettings.PrintDimension.PropertyChanged += PrintDimension_PropertyChanged;

            _selectedPrintDim = CommonPrintDimensions.FirstOrDefault(pd => pd.CommonName == PrintDimension.GetNameFromDimensions(OutputSettings.PrintDimension.longEdgeInInch, OutputSettings.PrintDimension.ShortEdgeInInch)) ?? CommonPrintDimensions[0];
        }

        private void PrintDimension_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
                if (e.PropertyName == nameof(PrintDimension.Margin)) // Don't want anything to be change if user changed margin.
                    return;

                var commonPrintSizeName = PrintDimension.GetNameFromDimensions(OutputSettings.PrintDimension.longEdgeInInch, OutputSettings.PrintDimension.ShortEdgeInInch);
                if (SelectedPrintDim.commonName == commonPrintSizeName)
                {
                    return; // already same is selected in combo box.
                }

                var printDimToSelect = CommonPrintDimensions.FirstOrDefault(pd => pd.CommonName == commonPrintSizeName);
                if (printDimToSelect != null && printDimToSelect.ShortEdgeInInch == (double) 0) //we don't change back from custom to common name even if we find a match as WinUI 3 has a it wil trigger this even back again when combobox value change. We can gracefull handle everything but WinUI 3 cannot handle two way binding well on combobox and numberbox. 
                {
                    SelectedPrintDim = printDimToSelect;
                }
        }

        protected ICommand SetPercentageCommand => new RelayCommand<object>(param =>
        {
            if (param is string percentageValue && double.TryParse(percentageValue, out double percentage))
                App.OutputSettingsInstance.PercentageOfLongEdge = percentage;
        });

        protected ICommand SetPrimaryLength => new RelayCommand<object>(param =>
        {
            var lengthVals = param as string;
            var sizes = lengthVals.Split(",");
            if (int.TryParse(sizes[0].Trim(), out int sizeX) && int.TryParse(sizes[1].Trim(), out int sizeY))
                App.OutputSettingsInstance.PrimaryEdgeLength = App.OutputSettingsInstance.PrimaryEdgeLength == sizeY ? sizeX : sizeY;
        });
    }
}
