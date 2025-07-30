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
using Microsoft.UI.Xaml.Media.Imaging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;
using miCompressor.core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace miCompressor.ui
{
    public sealed partial class WatermarkSettingsView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            if(App.OutputSettingsInstance.ApplyWatermark) App.OutputSettingsInstance.WatermarkSettingsHash++;
        }
        public ObservableCollection<Brush> AvailableBackgroundBrushes { get; }

        private Brush _selectedBackgroundBrush;
        public Brush SelectedBackgroundBrush
        {
            get => _selectedBackgroundBrush;
            set { _selectedBackgroundBrush = value; OnPropertyChanged(); }
        }

        private int _minDimension;
        public int MinDimension
        {
            get => _minDimension;
            set
            {
                _minDimension = value;
                WatermarkSettings.SetMinDimension(value);
                OnPropertyChanged();
            }
        }

        private int _maxHeightPercentage;
        public int MaxHeightPercentage
        {
            get => _maxHeightPercentage;
            set
            {
                _maxHeightPercentage = value;
                WatermarkSettings.SetMaxHeightPercentage(value);
                OnPropertyChanged();
            }
        }

        private int _opacityPercentage;
        public int OpacityPercentage
        {
            get => _opacityPercentage;
            set
            {
                _opacityPercentage = value;
                WatermarkSettings.SetOpacityPercentage(value);
                OnPropertyChanged();
            }
        }

        private string _watermarkPath;
        public string WatermarkPath
        {
            get => _watermarkPath;
            set
            {
                _watermarkPath = value;
                WatermarkSettings.SetWatermarkPath(value);
                OnPropertyChanged();
                _ = LoadPreviewWatermarkAsync();
            }
        }

        private ImageSource _watermarkImage;
        public ImageSource WatermarkImage
        {
            get => _watermarkImage;
            set { _watermarkImage = value; OnPropertyChanged(); }
        }

        public WatermarkSettingsView()
        {
            InitializeComponent();
            DataContext = this;

            // Default brushes
            AvailableBackgroundBrushes = new ObservableCollection<Brush>
{
    new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 255, 255)), // White
    new SolidColorBrush(Windows.UI.Color.FromArgb(255, 211, 211, 211)), // LightGray
    new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 0)),       // Black
    new SolidColorBrush(Windows.UI.Color.FromArgb(255, 255, 0, 0)),     // Red
    new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 128, 0)),     // Green
    new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 0, 255)),     // Blue
};
            SelectedBackgroundBrush = AvailableBackgroundBrushes[1]; // LightGray

            // Load persisted settings
            MinDimension = WatermarkSettings.GetMinDimension();
            MaxHeightPercentage = WatermarkSettings.GetMaxHeightPercentage();
            OpacityPercentage = WatermarkSettings.GetOpacityPercentage();
            WatermarkPath = WatermarkSettings.GetWatermarkPath();
        }

        private async Task LoadPreviewWatermarkAsync()
        {
            if (string.IsNullOrWhiteSpace(WatermarkPath) || !File.Exists(WatermarkPath))
            {
                PreviewWatermark.Visibility = Visibility.Collapsed;
                ErrorText.Visibility = Visibility.Visible;
                return;
            }

            ErrorText.Visibility = Visibility.Collapsed;
            PreviewWatermark.Visibility = Visibility.Visible;

            try
            {
                var bitmap = new BitmapImage(new Uri(WatermarkPath));
                WatermarkImage = bitmap;
            }
            catch
            {
                PreviewWatermark.Visibility = Visibility.Collapsed;
                ErrorText.Visibility = Visibility.Visible;
            }
        }

        private void ColorBorder_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (sender is Border b && b.Background is Brush brush)
            {
                SelectedBackgroundBrush = brush;
                OnPropertyChanged(nameof(SelectedBackgroundBrush));
            }
        }

        private async void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var picker = new FileOpenPicker
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.PicturesLibrary
            };
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".webp");
            picker.FileTypeFilter.Add(".tif");
            picker.FileTypeFilter.Add(".tiff");

            // Initialize with current window handle
            InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.MainWindow));

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                WatermarkPath = file.Path;
            }
        }
    }

    /// <summary>
    /// Converts a 0-100 integer percentage into a 0.0-1.0 opacity value.
    /// </summary>
    public class PercentToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is int percent)
                return percent / 100.0;
            return 1.0;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }

    /// <summary>
    /// Multiplies a numeric dimension by a parameter percentage (e.g., 0.5 for 50%).
    /// </summary>
    public class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is double d && parameter is string s && double.TryParse(s, out var pct))
                return d * pct;
            return 0;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
            => throw new NotImplementedException();
    }
}
