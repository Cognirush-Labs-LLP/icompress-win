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
            }
        }

        /// <summary>
        /// List of available dimension reduction strategies with friendly names (for UI)
        /// </summary>
        public List<DimensionReductionStrategyItem> DimensionStrategies { get; }

        public bool SetPercentage => SelectedStrategy.Value is Strategy.Percentage;
        public bool SetLongEdge => OutputSettings.DimensionStrategy is Strategy.LongEdge;
        public bool SetMaxHeight => OutputSettings.DimensionStrategy is Strategy.MaxHeight;
        public bool SetMaxWidth => OutputSettings.DimensionStrategy is Strategy.MaxWidth;
        public bool SetFixedHeight => OutputSettings.DimensionStrategy is Strategy.FixedHeight;
        public bool SetFixedWidth => OutputSettings.DimensionStrategy is Strategy.FixedWidth;
        public bool SetFitInFrame => OutputSettings.DimensionStrategy is Strategy.FitInFrame;
        public bool SetFixedInFrame => OutputSettings.DimensionStrategy is Strategy.FixedInFrame;
        public bool SetLength => OutputSettings.DimensionStrategy is
                    Strategy.LongEdge or Strategy.MaxHeight or Strategy.MaxWidth or Strategy.FixedHeight or Strategy.FixedWidth;

    public DimensionSettingsView()
        {
            this.InitializeComponent();

            OutputSettings = App.OutputSettingsInstance;

            DimensionStrategies = Enum.GetValues(typeof(DimensionReductionStrategy))
               .Cast<DimensionReductionStrategy>()
               .Select(strategy => new DimensionReductionStrategyItem(strategy))
               .ToList();

            SelectedStrategy = DimensionStrategies.Find(strategy => strategy.Value == OutputSettings.DimensionStrategy) ?? DimensionStrategies.FirstOrDefault();

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
