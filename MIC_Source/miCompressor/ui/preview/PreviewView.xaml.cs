using miCompressor.core;
using miCompressor.viewmodel;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using Microsoft.UI.Xaml.Input;
using static System.Net.Mime.MediaTypeNames;
using Windows.UI.Core;
using System.Threading.Tasks;

namespace miCompressor.ui;

public sealed partial class PreviewView : UserControl
{
    MasterState CurrentState = App.CurrentState;
    PreviewViewModel vm = new PreviewViewModel(App.CurrentState.FileStore);

    [AutoNotify] public bool showCompressed = false;

    uint OriginalHeight;
    uint OriginalWidth;
    uint Height; //set to adjust the zoom
    uint Width;  //set to adjust the zoom
    uint VisibleAreaHeight;
    uint VisibleAreaWidth;

    bool show100PercentSizeByDefault = false; //True = 100%, False = Fit the image in visible area.

    bool _do_not_use_fitVisibleArea = false;
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
            FitVisibleArea = false;
            if (value == true)
                FitVisibleArea = false;
        }
    }

    public PreviewView()
    {
        this.InitializeComponent();

        App.CurrentState.PropertyChanged += CurrentState_PropertyChanged;
        ImageGridScrollViewArea.SizeChanged += ImageGridScrollViewArea_SizeChanged;
        App.OutputSettingsInstance.PropertyChanged += OutputSettings_PropertyChanged;
        VisibleAreaHeight = (uint)ImageGridScrollViewArea.ActualHeight;
        VisibleAreaWidth = (uint)ImageGridScrollViewArea.ActualWidth;
        this.RegisterPropertyChangedCallback(UIElement.VisibilityProperty, VisibilityChanged);
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
                ShowOriginalImage(CurrentState.SelectedImageForPreview);
            });
    }


    private async void ShowOriginalImage(MediaFileInfo? file = null)
    {
        if (this.Visibility != Visibility.Visible)
            return;

        ImageProgressRing.IsActive = true;
        if (file != null)
        {
            OriginalImage.Source = await vm.GetOriginal(file);
            UIThreadHelper.RunOnUIThread(async () => {
                await Task.Delay(300);
                ResetImageSize();
                await Task.Delay(300);
                ResetImageSize();
            });
        }
        else
        {
            OriginalImage.Source = await vm.GetOriginal();
        }
        ImageProgressRing.IsActive = false;
    }

    private void ResetImageSize()
    {
        var image = CurrentState.selectedImageForPreview;
        if (image == null)
            return; // God knows what happened. Let that knowledge be with him and only him! Praise the lord!
        OriginalHeight = image.Height;
        OriginalWidth = image.Width;
        if (show100PercentSizeByDefault)
        {
            FitImageInVisibleArea(true);
        }
        else
        {
            FitVisibleArea = true;
            //if (ImageGridScrollViewArea.ActualHeight != 0)
            {
                VisibleAreaHeight = (uint)ImageGridScrollViewArea.ActualHeight;
                VisibleAreaWidth = (uint)ImageGridScrollViewArea.ActualWidth;
            }
            /*else
            {
                (int height, int width) = GetWindowDimension();

                VisibleAreaHeight = (uint) Math.Max(200, height - 100);
                VisibleAreaWidth = (uint)Math.Max(200, width - 300); ;
            }*/
            Height = VisibleAreaHeight;
            Width = VisibleAreaWidth;
        }
        AdjustImageSize();
    }

    private (int height, int width) GetWindowDimension()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        var appWindow = AppWindow.GetFromWindowId(Win32Interop.GetWindowIdFromWindow(hwnd));
        return (appWindow.Size.Height, appWindow.Size.Width);
    }
    private void AdjustImageSize()
    {
        OriginalImage.Height = Height;
        OriginalImage.Width = Width;
        CompressedImage.Height = Height;
        CompressedImage.Width = Width;
        ImageContainerGrid.Width = Width; //required for adding scrollbar.
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
        if (this.Visibility != Visibility.Visible)
            return;
        ImageProgressRing.IsActive = true;
        CompressedImage.Opacity = 0.5;
        ImageSource source = null;
        bool isSameAsPrevious = false;
        if (file == null) //no chagne in image
            (source, isSameAsPrevious) = await vm.GetCompressed(App.OutputSettingsInstance);
        else
            (source, isSameAsPrevious) = await vm.GetCompressed(file, App.OutputSettingsInstance);
        if (!isSameAsPrevious)
        {
            CompressedImage.Source = source;
        }
        ImageProgressRing.IsActive = false;
        CompressedImage.Opacity = 1;
        if(Set100pcZoomOfCompressedImage)
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

    private SolidColorBrush accentBrush = (SolidColorBrush)App.Current.Resources["Acccent_100"];
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
            if(vm.CompressedImageWidth == 0)
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

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (ShowCompressed)
        {
            RefreshCompressedImage(CurrentState.selectedImageForPreview);
        }
        else
        {
            ShowOriginalImage(CurrentState.selectedImageForPreview);
        }

    }

    private void VisibilityChanged(DependencyObject sender, DependencyProperty dp)
    {
        if (((UIElement)sender).Visibility == Visibility.Visible)
        {
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
            CompressedImage.Source = null;
            OriginalImage.Source = null;
        }
    }
    #region Mouse Pan

    private bool isDragging = false;
    private Windows.Foundation.Point lastPointerPosition;
    private CoreCursor panCursor = new CoreCursor(CoreCursorType.Hand, 1); // Hand cursor
    private CoreCursor defaultCursor = new CoreCursor(CoreCursorType.Arrow, 1); // Default arrow


    private void PannableImage_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        isDragging = true;
        lastPointerPosition = e.GetCurrentPoint(ImageGridScrollViewArea).Position;
        defaultCursor = Window.Current.CoreWindow.PointerCursor;
        Window.Current.CoreWindow.PointerCursor = panCursor;


        ImageContainerGrid.CapturePointer(e.Pointer);
    }

    private void PannableImage_PointerMoved(object sender, PointerRoutedEventArgs e)
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

    private void PannableImage_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        isDragging = false;
        ImageContainerGrid.ReleasePointerCapture(e.Pointer);
        Window.Current.CoreWindow.PointerCursor = defaultCursor;
    }
    #endregion
}
