using miCompressor.core;
using miCompressor.viewmodel;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.System;

namespace miCompressor.ui;

public sealed partial class PreviewView : UserControl
{
    MasterState CurrentState = App.CurrentState;
    PreviewViewModel vm = new PreviewViewModel(App.CurrentState);

    [AutoNotify] public bool showCompressed = false;
    [AutoNotify] public string currentZoomLevel = "";

    uint OriginalHeight;
    uint OriginalWidth;
    uint Height; //set to adjust the zoom
    uint Width;  //set to adjust the zoom
    uint VisibleAreaHeight;
    uint VisibleAreaWidth;

    bool show100PercentSizeByDefault = false; //True = 100%, False = Fit the image in visible area (if FitVisibleArea = false)

    bool _do_not_use_fitVisibleArea = true;
    bool FitVisibleArea
    {
        get => _do_not_use_fitVisibleArea;
        set
        {
            _do_not_use_fitVisibleArea = value;
            ZoomFitButton.Background = value ? accentBrush : defaultBrush;
            if (value == true)
                Set100pcZoomOfCompressedImage = false;
        }
    }

    bool _do_not_use_set100pcZoom = false;
    bool Set100pcZoomOfCompressedImage
    {
        get => _do_not_use_set100pcZoom;
        set
        {
            _do_not_use_set100pcZoom = value;
            if (value == true)
                FitVisibleArea = false;
        }
    }

    bool initialized = false;
    public PreviewView()
    {
        this.InitializeComponent();

        initialized = true;

        App.CurrentState.PropertyChanged -= CurrentState_PropertyChanged;
        ImageGridScrollViewArea.SizeChanged -= ImageGridScrollViewArea_SizeChanged;
        App.OutputSettingsInstance.PropertyChanged -= OutputSettings_PropertyChanged;
        vm.PropertyChanged -= ViewModel_PropertyChanged;
        ShortcutKeyManager.KeyPressed -= ShortcutKeyManager_KeyPressed;

        App.CurrentState.PropertyChanged += CurrentState_PropertyChanged;
        ImageGridScrollViewArea.SizeChanged += ImageGridScrollViewArea_SizeChanged;
        App.OutputSettingsInstance.PropertyChanged += OutputSettings_PropertyChanged;
        this.RegisterPropertyChangedCallback(UIElement.VisibilityProperty, VisibilityChanged);
        vm.PropertyChanged += ViewModel_PropertyChanged;
        ShortcutKeyManager.KeyPressed += ShortcutKeyManager_KeyPressed;

        VisibleAreaHeight = (uint)ImageGridScrollViewArea.ActualHeight;
        VisibleAreaWidth = (uint)ImageGridScrollViewArea.ActualWidth;

        _zoomLevelUpdateTimer = new Timer(ZoomLevelTimerCallback, null, 0, 1000); // Recalculate zoom level periodically every 1000ms (1 second)

    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (ShowCompressed && this.Visibility == Visibility.Visible && Set100pcZoomOfCompressedImage)
        {
            if (e.PropertyName == nameof(PreviewViewModel.CompressedImageWidth))
            {
                Width = vm.CompressedImageWidth;
                Height = vm.CompressedImageHeight;
                AdjustImageSize();
            }
        }
    }

    private void OutputSettings_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (ShowCompressed && this.Visibility == Visibility.Visible)
        {
            DebounceTask.Add(200, "OutputSettings_PropertyChanged_in_PreviewView_UserControl", () =>
            {
                RefreshCompressedImage();
            }, shouldRunInUI: true);
        }
    }

    private void ImageGridScrollViewArea_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        VisibleAreaHeight = (uint)ImageGridScrollViewArea.ActualHeight;
        VisibleAreaWidth = (uint)ImageGridScrollViewArea.ActualWidth;

        if (FitVisibleArea)
        {
            FitImageInVisibleArea();
        }
    }

    private async void CurrentState_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MasterState.SelectedImageForPreview))
            UIThreadHelper.RunOnUIThread(() =>
            {
                //ShowOriginalImage(CurrentState.SelectedImageForPreview);
            });
    }

    /// <summary>
    /// Called when the user control is loaded (made visible) with Original Image to show. 
    /// </summary>
    /// <param name="file"></param>
    private async void ShowOriginalImage(MediaFileInfo? file = null)
    {
        CurrentZoomLevel = "";

        if (this.Visibility != Visibility.Visible)
            return;
        OriginalImage.Source = null;
        OriginalImage.Opacity = 0.3;
        ImageProgressRing.IsActive = true;
        if (file != null)
        {
            OriginalImage.Source = await vm.GetOriginal(file);
            UIThreadHelper.RunOnUIThread(async () =>
            {
                await Task.Delay(300);
                ResetImageSize();
                await Task.Delay(300);
                ResetImageSize();//hack.
            });
        }
        else
        {
            OriginalImage.Source = await vm.GetOriginal();
        }
        ImageProgressRing.IsActive = false;
        OriginalImage.Opacity = 1;
    }


    /// <summary>
    /// Should be called once per image load
    /// </summary>
    private void ResetImageSize()
    {
        FitVisibleArea = true; //default behavior 

        var image = CurrentState.selectedImageForPreview;
        if (image == null)
            return;

        OriginalHeight = image.Height;
        OriginalWidth = image.Width;

        if (ImageGridScrollViewArea.ActualHeight != 0)
        {
            VisibleAreaHeight = (uint)ImageGridScrollViewArea.ActualHeight;
            VisibleAreaWidth = (uint)ImageGridScrollViewArea.ActualWidth;
        }
        else
        {
            (int height, int width) = GetWindowDimension();

            VisibleAreaHeight = (uint)Math.Max(200, height - 100);
            VisibleAreaWidth = (uint)Math.Max(200, width - 300); ;
        }

        if (FitVisibleArea)
        {
            FitImageInVisibleArea(true);
        }
        else
        {
            Height = VisibleAreaHeight;
            Width = VisibleAreaWidth;
            AdjustImageSize();
        }
    }

    #region Zoom Level Thread

    //  We need to find zoom level periodically. This is the only simple 
    // and most accurate way to get zoom level with minimum code. 
    private static Timer? _zoomLevelUpdateTimer;
    private static bool _isZoomLevelUpdatePaused = false;
    private static readonly object _zoomLevelUpdateLock = new();

    private void ZoomLevelTimerCallback(object? state)
    {
        lock (_zoomLevelUpdateLock)
        {
            if (_isZoomLevelUpdatePaused) return;
            UIThreadHelper.RunOnUIThread(() =>
                {
                    if (this.Visibility != Visibility.Visible) return;
                    CurrentZoomLevel = GetCurrentZoomLevel();
                });
        }
    }

    private string GetCurrentZoomLevel()
    {
        try
        {
            if (CurrentState.SelectedImageForPreview == null)
                return "";

            double imageWidth = (double)CurrentState.SelectedImageForPreview.Width; ;
            double imageHeight = (double)CurrentState.SelectedImageForPreview.Height;
            double containerHeight = OriginalImage.ActualHeight;
            double containerWidth = OriginalImage.ActualWidth;

            if (ShowCompressed)
            {
                imageWidth = (double)CurrentState.SelectedImageForPreview.CompressedWidth;
                imageHeight = (double)CurrentState.SelectedImageForPreview.CompressedHeight;
                containerWidth = CompressedImage.ActualWidth;
                containerHeight = CompressedImage.ActualHeight;
            }

            var widthPercentage = 100 * containerWidth / imageWidth;
            var heightPercentage = 100 * containerHeight / imageHeight;

            //return " at Zoom Level " + (widthPercentage > heightPercentage ? widthPercentage : heightPercentage).ToString("0.##") + " %";
            return (widthPercentage > heightPercentage ? widthPercentage : heightPercentage).ToString("0.##") + "%";
        }
        catch { }
        return "";
    }
    #endregion

    private (int height, int width) GetWindowDimension()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        var appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));
        return (appWindow.Size.Height, appWindow.Size.Width);
    }
    private void AdjustImageSize()
    {
        CurrentZoomLevel = "";
        OriginalImage.Height = Height;
        OriginalImage.Width = Width;
        CompressedImage.Height = Height;
        CompressedImage.Width = Width;
        ImageContainerGrid.Width = Width; //required for adding scrollbar.

        /*UIThreadHelper.RunOnUIThreadAsync( async () => {
                await Task.Delay(300);
                CurrentZoomLevel = GetCurrentZoomLevel();
        });*/
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        CurrentState.ShowPreview = false;

    }

    private async void OriginalButton_Click(object sender, RoutedEventArgs e)
    {
        ShowCompressed = false;
        SetOriginalCompressedButtonBG();
        UIThreadHelper.RunOnUIThread(() =>
        {
            ShowOriginalImage();
        });
    }

    private async void CompressedImageButton_Click(object sender, RoutedEventArgs e)
    {
        ShowCompressed = true;
        SetOriginalCompressedButtonBG();

        UIThreadHelper.RunOnUIThread(() =>
        {
            RefreshCompressedImage();
        });
    }

    private async void RefreshCompressedImage(MediaFileInfo file = null)
    {
        CurrentZoomLevel = "";
        if (this.Visibility != Visibility.Visible)
            return;
        ImageProgressRing.IsActive = true;
        CompressedImage.Opacity = 0.5;
        ImageSource source = null;
        bool isSameAsPrevious = false;
        if (file == null) //no chagne in image
            (source, isSameAsPrevious) = await vm.GetCompressed();
        else
            (source, isSameAsPrevious) = await vm.GetCompressed(file);
        if (!isSameAsPrevious)
        {
            CompressedImage.Source = source;
        }
        ImageProgressRing.IsActive = false;
        CompressedImage.Opacity = 1;
        if (Set100pcZoomOfCompressedImage)
        {
            Zoom100Button_Click(this, new RoutedEventArgs());
        }
    }

    private void SetOriginalCompressedButtonBG()
    {
        if (showCompressed)
        {
            OriginalImageButton.Background = defaultBrush;
            CompressedImageButton.Background = accentBrush;
        }
        else
        {
            CompressedImageButton.Background = defaultBrush;
            OriginalImageButton.Background = accentBrush;
        }
    }

    private SolidColorBrush accentBrush = (SolidColorBrush)App.Current.Resources["Acccent_70"];
    private SolidColorBrush defaultBrush = new SolidColorBrush(Microsoft.UI.Colors.Transparent);

    private void ZoomInButton_Click(object sender, RoutedEventArgs e)
    {
        FitVisibleArea = false;
        Set100pcZoomOfCompressedImage = false;

        Height = (uint)Math.Min(OriginalHeight, Height + Height * 0.1);
        Width = Height * OriginalWidth / OriginalHeight;
        AdjustImageSize();
    }

    private void ZoomOutButton_Click(object sender, RoutedEventArgs e)
    {
        FitVisibleArea = false;
        Set100pcZoomOfCompressedImage = false;

        Height = (uint)Math.Max(Math.Min(100, OriginalHeight), Height - Height * 0.1);
        Width = Height * OriginalWidth / OriginalHeight;
        AdjustImageSize();
    }

    private void Zoom100Button_Click(object sender, RoutedEventArgs e)
    {
        FitVisibleArea = false;

        if (ShowCompressed)
        {
            if (vm.CompressedImageWidth == 0)
            {
                Height = OriginalHeight;
                Width = OriginalWidth;
            }
            else
            {
                Height = vm.CompressedImageHeight;
                Width = vm.CompressedImageWidth;
            }
            Set100pcZoomOfCompressedImage = true;
        }
        else
        {
            Set100pcZoomOfCompressedImage = false;
            Height = OriginalHeight;
            Width = OriginalWidth;
        }
        AdjustImageSize();
    }

    private void ZoomFitButton_Click(object sender, RoutedEventArgs e)
    {
        FitVisibleArea = true;
        FitImageInVisibleArea(immediate: true);
    }

    private void FitImageInVisibleArea(bool immediate = false)
    {
        DebounceTask.Add(immediate ? 0 : 500, "PreviewView_UserControl_FitImageInVisibleArea", () =>
        {
            Width = Math.Min(VisibleAreaWidth - 10, OriginalWidth);
            Height = Math.Min(VisibleAreaHeight - 10, OriginalHeight);
            AdjustImageSize();
        }, shouldRunInUI: true);
    }


    private void VisibilityChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (((UIElement)sender).Visibility == Visibility.Visible)
        {
            _isZoomLevelUpdatePaused = false;
            vm.RemoveCached();
            ResetImageSize();
            if (ShowCompressed)
            {
                RefreshCompressedImage(CurrentState.selectedImageForPreview);
            }
            else
            {
                ShowOriginalImage(CurrentState.selectedImageForPreview);
            }
        }
        else
        {
            _isZoomLevelUpdatePaused = true;
            CompressedImage.Source = null;
            OriginalImage.Source = null;
        }
    }

    #region Mouse Pan

    private bool isDragging = false;
    private Windows.Foundation.Point lastPointerPosition;

    private void PannableImage_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            isDragging = true;
            lastPointerPosition = e.GetCurrentPoint(ImageGridScrollViewArea).Position;
            ImageContainerGrid.CapturePointer(e.Pointer);
        }
        catch
        {

        }
    }

    private void PannableImage_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            if (!isDragging) return;

            var currentPosition = e.GetCurrentPoint(ImageGridScrollViewArea).Position;

            // Calculate drag distance
            double deltaX = lastPointerPosition.X - currentPosition.X;
            double deltaY = lastPointerPosition.Y - currentPosition.Y;

            // Update ScrollViewer position (inverse to move the image in expected direction)
            ImageGridScrollViewArea.ScrollBy(deltaX, deltaY);

            lastPointerPosition = currentPosition; // Update last pointer position
        }
        catch
        {

        }
    }

    private void PannableImage_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        try
        {
            isDragging = false;
            ImageContainerGrid.ReleasePointerCapture(e.Pointer);
        }
        catch
        {

        }
    }
    #endregion

    #region Previous Next navigation 

    private bool PrevNextImageLoadInProgress = false;

    private async void PrevButton_Click(object sender, RoutedEventArgs e)
    {
        if (vm.HasPrev == false || PrevNextImageLoadInProgress)
            return;

        PrevNextImageLoadInProgress = true;
        PrevButton.IsEnabled = false;
        NextButton.IsEnabled = false;
        CurrentZoomLevel = "";

        if (ShowCompressed)
        {
            ImageProgressRing.IsActive = true;
            CompressedImage.Opacity = 0.1;
            ImageSource source = null;
            bool isSameAsPrevious = false;
            (source, isSameAsPrevious) = await vm.GetPrevCompressed();
            if (!isSameAsPrevious)
            {
                CompressedImage.Source = source;
            }
            ImageProgressRing.IsActive = false;
            CompressedImage.Opacity = 1;
        }
        else
        {
            ImageProgressRing.IsActive = true;
            OriginalImage.Opacity = 0.1;
            OriginalImage.Source = await vm.GetPrevOriginal();
            ImageProgressRing.IsActive = false;
            OriginalImage.Opacity = 1;
        }

        PrevButton.IsEnabled = vm.HasPrev;
        NextButton.IsEnabled = vm.HasNext;
        PrevNextImageLoadInProgress = false;
    }

    private async void NextButton_Click(object sender, RoutedEventArgs e)
    {
        if (vm.HasNext == false || PrevNextImageLoadInProgress) //when calling next button with keyboard accelerator, we should reject the keystroke while next image is loading. 
            return;
        PrevNextImageLoadInProgress = true;
        PrevButton.IsEnabled = false;
        NextButton.IsEnabled = false;
        CurrentZoomLevel = "";
        if (ShowCompressed)
        {
            ImageProgressRing.IsActive = true;
            CompressedImage.Opacity = 0.1;
            ImageSource source = null;
            bool isSameAsPrevious = false;
            (source, isSameAsPrevious) = await vm.GetNextCompressed();
            if (!isSameAsPrevious)
            {
                CompressedImage.Source = source;
            }
            ImageProgressRing.IsActive = false;
            CompressedImage.Opacity = 1;
        }
        else
        {
            ImageProgressRing.IsActive = true;
            OriginalImage.Opacity = 0.1;
            OriginalImage.Source = await vm.GetNextOriginal();
            ImageProgressRing.IsActive = false;
            OriginalImage.Opacity = 1;
        }

        PrevButton.IsEnabled = vm.HasPrev;
        NextButton.IsEnabled = vm.HasNext;
        PrevNextImageLoadInProgress = false;
    }

    private void ToggleImages()
    {
        if (ShowCompressed)
            OriginalButton_Click(this, new RoutedEventArgs());
        else
            CompressedImageButton_Click(this, new RoutedEventArgs());
    }

    #endregion

    private void ShortcutKeyManager_KeyPressed(object? sender, ProcessKeyboardAcceleratorEventArgs e)
    {
        if (this.Visibility != Visibility.Visible)
            return;

        if (e.Key == VirtualKey.F && e.Modifiers == VirtualKeyModifiers.Control)
        {
            if (!FitVisibleArea)
                ZoomFitButton_Click(sender, new RoutedEventArgs());
            else
                Zoom100Button_Click(sender, new RoutedEventArgs());
        }
        else if (e.Key == VirtualKey.I && e.Modifiers == VirtualKeyModifiers.Control)
            ZoomInButton_Click(sender, new RoutedEventArgs());
        else if (e.Key == VirtualKey.O && e.Modifiers == VirtualKeyModifiers.Control)
            ZoomOutButton_Click(sender, new RoutedEventArgs());
        else if (e.Key == VirtualKey.P && e.Modifiers == VirtualKeyModifiers.Control)
            PrevButton_Click(sender, new RoutedEventArgs());
        else if (e.Key == VirtualKey.N && e.Modifiers == VirtualKeyModifiers.Control)
            NextButton_Click(sender, new RoutedEventArgs());
        else if (e.Key == VirtualKey.T && e.Modifiers == VirtualKeyModifiers.Control)
            ToggleImages();
        else if (e.Key == VirtualKey.Escape)
            BackButton_Click(sender, new RoutedEventArgs());
    }
}
