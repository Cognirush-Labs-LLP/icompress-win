using miCompressor.core;
using miCompressor.viewmodel;
using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;
using Windows.System;
using Windows.UI;

namespace miCompressor.ui;

/// <summary>
/// A Grid with its own mouse cursor. Also visible to UI Automation, unlike a plain Grid.
/// </summary>
public sealed partial class CursorGrid : Grid
{
    public InputSystemCursorShape CursorShape
    {
        get => (ProtectedCursor as InputSystemCursor)?.CursorShape ?? InputSystemCursorShape.Arrow;
        set => ProtectedCursor = InputSystemCursor.Create(value);
    }

    protected override AutomationPeer OnCreateAutomationPeer() => new FrameworkElementAutomationPeer(this);
}

/// <summary>
/// Crops, resizes and rotates one still image. Nothing is stored until Done, so Cancel only has to close the screen.
/// See <see cref="CropGeometry"/> for how the crop's size, the image's zoom and the way they're drawn are kept apart.
/// </summary>
public sealed partial class EditView : UserControl
{
    private const uint MaxDisplayLongEdge = 2560; // sharp enough for any screen this crops on, while a 100 MP image would take 400 MB
    private const double LargeNudgeDips = 20;
    private const double HandleSizeDips = 14;
    private const double ZoomButtonStep = 20; // slider positions, about 13%
    private const double SliderPositions = 1000;

    private const string FreeRatioKey = "free";
    private const string LastRatioSetting = "Edit.LastCropRatio";

    private static readonly Dictionary<string, double?> s_ratios = new()
    {
        [FreeRatioKey] = null,
        ["1:1"] = 1.0,
        ["4:5"] = 4.0 / 5.0,
        ["16:9"] = 16.0 / 9.0,
        ["9:16"] = 9.0 / 16.0,
    };

    /// <summary>
    /// The ratio last picked, saved like other settings. An image that isn't cropped yet starts with it, so a series can be cropped to one ratio with fewer clicks.
    /// </summary>
    private static string LastRatioKey
    {
        get
        {
            string? key = AppSettingsManager.Get(LastRatioSetting, FreeRatioKey);
            return key != null && s_ratios.ContainsKey(key) ? key : FreeRatioKey;
        }
        set => AppSettingsManager.Set(LastRatioSetting, value);
    }

    private readonly MasterState _state = App.CurrentState;
    private readonly RadioButton[] _ratioButtons;

    public Thickness StageMargin { get; } = new(HandleSizeDips);

    private int _loadRequest;
    private bool _imageDecoded;
    private uint _uprightWidth;
    private uint _uprightHeight;
    private int _quarterTurns;
    private CropFill _fill;

    /// <summary>
    /// Null until the image is decoded and the stage has a size, as the screen scale is worked out from both.
    /// </summary>
    private CropGeometry? _geometry;

    /// <summary>
    /// True while code sets the controls, whose change events are only meant for the user's changes.
    /// </summary>
    private bool _settingControls;

    /// <summary>
    /// The size box the user last typed in. Only it may change the other box, which is what stops the two boxes from rewriting each other.
    /// A box's TextChanged event arrives after the change, too late for <see cref="_settingControls"/> to tell who made it, so this is noted before the change.
    /// </summary>
    private TextBox? _boxBeingTyped;

    // The running drag, measured from where it started so that a drag stopped by an edge doesn't drift.
    private bool _dragging;
    private CropCorner? _dragCorner;
    private Point _dragStartPointer;

    private bool IsSideways => _quarterTurns % 2 != 0;
    private uint RotatedWidth => IsSideways ? _uprightHeight : _uprightWidth;
    private uint RotatedHeight => IsSideways ? _uprightWidth : _uprightHeight;

    private double StageWidth => StageHost.ActualWidth - StageMargin.Left - StageMargin.Right;
    private double StageHeight => StageHost.ActualHeight - StageMargin.Top - StageMargin.Bottom;
    private bool HasStage => StageWidth > 0 && StageHeight > 0;

    public EditView()
    {
        InitializeComponent();
        _ratioButtons = [RatioFree, Ratio1x1, Ratio4x5, Ratio16x9, Ratio9x16];
        RegisterPropertyChangedCallback(VisibilityProperty, (_, _) => OnVisibilityChanged());
    }

    private void OnVisibilityChanged()
    {
        if (Visibility == Visibility.Visible && _state.ImageToEdit is MediaFileInfo media)
        {
            LoadImage(media);
            return;
        }

        // Closed: forget the image and any load still running.
        _loadRequest++;
        EditImage.Source = null;
        ForgetImage();
    }

    private void ForgetImage()
    {
        _imageDecoded = false;
        _geometry = null;
        SetControlsEnabled(false);
    }

    private async void LoadImage(MediaFileInfo media)
    {
        int request = ++_loadRequest;
        ForgetImage();
        EditImage.Source = null;
        LoadFailedText.Visibility = Visibility.Collapsed;
        LoadingRing.IsActive = true;
        FileNameText.Text = media.ShortNameToShow;
        ToolTipService.SetToolTip(FileNameText, media.ShortName);
        SetBoxes("", "");
        ExclusionNote.Opacity = 0;
        PushNote.Opacity = 0;
        FillPreview.Visibility = Visibility.Collapsed;
        ShowBackgroundChoice(false);
        RemoveEditButton.Visibility = media.IsEdited ? Visibility.Visible : Visibility.Collapsed;

        LoadedImage? image = await EditedImageLoader.LoadAsync(media, edit: null, MaxDisplayLongEdge);
        if (request != _loadRequest)
            return;

        LoadingRing.IsActive = false;
        if (image == null)
        {
            LoadFailedText.Visibility = Visibility.Visible;
            return;
        }

        _uprightWidth = image.UprightWidth;
        _uprightHeight = image.UprightHeight;
        EditImage.Source = image.Source;
        _imageDecoded = true;
        StartEditingIfReady();
    }

    private void StartEditingIfReady()
    {
        if (!_imageDecoded || !HasStage)
            return;

        MediaFileInfo media = _state.ImageToEdit!;
        ImageEdit? edit = media.Edit;
        _quarterTurns = edit?.QuarterTurns ?? 0;

        // Blank space starts transparent where the output can have it. JPEG can't, whatever the edit stored.
        string outputExtension = _state.OutputSettings.format.GetOutputExtension(media.FilePath);
        FillTransparent.Visibility = ImageEditor.KeepsTransparency(outputExtension) ? Visibility.Visible : Visibility.Collapsed;
        ShowFill(ImageEditor.UsableFill(edit?.Fill ?? CropFill.Transparent, outputExtension));

        string ratioKey = edit == null ? LastRatioKey : s_ratios.First(ratio => ratio.Value == edit.LockedRatio).Key;
        ShowRatio(ratioKey);

        _geometry = edit?.Crop is CropRect crop
            ? CropGeometry.Restore(crop, edit.Zoom, edit.LockedRatio, RotatedWidth, RotatedHeight, StageWidth, StageHeight)
            : new CropGeometry(RotatedWidth, RotatedHeight, StageWidth, StageHeight, s_ratios[ratioKey]);

        SetControlsEnabled(true);
        OnCropChanged();
        Focus(FocusState.Programmatic);
    }

    private void SetControlsEnabled(bool enabled)
    {
        Stage.Opacity = enabled ? 1 : 0;
        Stage.IsHitTestVisible = enabled;
        DoneButton.IsEnabled = enabled;
        CropControls.IsEnabled = enabled;
        ImageControls.IsEnabled = enabled;
    }

    /// <summary>
    /// Runs code that sets controls, without their change events acting on it.
    /// </summary>
    private void SetControls(Action set)
    {
        _settingControls = true;
        try
        {
            set();
        }
        finally
        {
            _settingControls = false;
        }
    }

    private void ShowRatio(string key)
    {
        SetControls(() =>
        {
            foreach (RadioButton button in _ratioButtons)
                button.IsChecked = (string)button.Tag == key;
        });
    }

    #region Toolbar

    private void Close()
    {
        _state.ShowEdit = false;
        _state.ImageToEdit = null;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();

    private void DoneButton_Click(object sender, RoutedEventArgs e)
    {
        // A size typed without pressing OK is still what the user asked for.
        if (!ApplyTypedSize())
            return;

        _state.ImageToEdit!.Edit = _geometry!.ToEdit(_quarterTurns, _fill);
        Close();
    }

    private void RemoveEditButton_Click(object sender, RoutedEventArgs e)
    {
        _state.ImageToEdit!.Edit = null;
        Close();
    }

    private void RotateLeftButton_Click(object sender, RoutedEventArgs e) => Rotate(3);

    private void RotateRightButton_Click(object sender, RoutedEventArgs e) => Rotate(1);

    private void Rotate(int clockwiseQuarterTurns)
    {
        // Starts over, as the old crop's place means nothing on the turned image, which also fits the display differently.
        _quarterTurns = (_quarterTurns + clockwiseQuarterTurns) % 4;
        _geometry = new CropGeometry(RotatedWidth, RotatedHeight, StageWidth, StageHeight, _geometry!.Ratio);
        OnCropChanged();
        Focus(FocusState.Programmatic);
    }

    private void RatioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (_settingControls)
            return;

        string key = (string)((RadioButton)sender).Tag;
        LastRatioKey = key;
        _geometry!.SetRatio(s_ratios[key]);
        OnCropChanged();
        Focus(FocusState.Programmatic);
    }

    #endregion

    #region Zoom

    private void ZoomSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_settingControls)
            return;

        // The slider stays where the user put it, even if the zoom rounds to the same percent.
        _geometry!.SetZoom(CropGeometry.ZoomAtSliderFraction(e.NewValue / SliderPositions));
        ShowImage();
    }

    private void ZoomOutButton_Click(object sender, RoutedEventArgs e) => ZoomSlider.Value -= ZoomButtonStep;

    private void ZoomInButton_Click(object sender, RoutedEventArgs e) => ZoomSlider.Value += ZoomButtonStep;

    private void FitButton_Click(object sender, RoutedEventArgs e)
    {
        _geometry!.FitImageInCrop();
        ShowImageAtNewZoom();
    }

    private void FillButton_Click(object sender, RoutedEventArgs e)
    {
        _geometry!.FillCropWithImage();
        ShowImageAtNewZoom();
    }

    private void ActualSizeButton_Click(object sender, RoutedEventArgs e)
    {
        _geometry!.SetZoom(1);
        ShowImageAtNewZoom();
    }

    /// <summary>
    /// Shows everything except the crop's size, which a change of zoom or window doesn't affect, so that a size being typed is left alone.
    /// </summary>
    private void ShowImage()
    {
        UpdateStage();
        ZoomText.Text = $"{Math.Round(_geometry!.Zoom * 100)}%";
        UpdateNotes();
    }

    private void ShowImageAtNewZoom()
    {
        SetControls(() => ZoomSlider.Value = SliderPositions * CropGeometry.SliderFractionOfZoom(_geometry!.Zoom));
        ShowImage();
    }

    #endregion

    #region Blank space

    private void FillButton_Checked(object sender, RoutedEventArgs e)
    {
        if (_settingControls)
            return;

        ShowFill(sender == FillBlack ? CropFill.Black : sender == FillWhite ? CropFill.White : CropFill.Transparent);
        Focus(FocusState.Programmatic);
    }

    private void FillCustom_Click(object sender, RoutedEventArgs e)
    {
        Color color = FillPicker.Color;
        ShowFill(new CropFill(color.R, color.G, color.B, 255));
        FlyoutBase.ShowAttachedFlyout(FillCustom);
    }

    private void FillPicker_ColorChanged(ColorPicker sender, ColorChangedEventArgs args)
    {
        if (!_settingControls)
            ShowFill(new CropFill(args.NewColor.R, args.NewColor.G, args.NewColor.B, 255));
    }

    private void ShowFill(CropFill fill)
    {
        _fill = fill;
        bool isCustom = fill != CropFill.Black && fill != CropFill.White && fill != CropFill.Transparent;
        SetControls(() =>
        {
            FillBlack.IsChecked = fill == CropFill.Black;
            FillWhite.IsChecked = fill == CropFill.White;
            FillTransparent.IsChecked = fill == CropFill.Transparent;
            FillCustom.IsChecked = isCustom;
            if (isCustom)
                FillPicker.Color = Color.FromArgb(255, fill.R, fill.G, fill.B);
        });

        if (isCustom)
            CustomSwatch.Background = new SolidColorBrush(Color.FromArgb(255, fill.R, fill.G, fill.B));
        CustomSwatch.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
        FillPreview.Fill = fill.A == 0
            ? (Brush)Resources["TransparentFillBrush"]
            : new SolidColorBrush(Color.FromArgb(fill.A, fill.R, fill.G, fill.B));
    }

    #endregion

    #region Typed size

    private void SizeBox_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
    {
        args.Cancel = !args.NewText.All(char.IsAsciiDigit);
        if (!_settingControls && !args.Cancel)
            _boxBeingTyped = sender;
    }

    private void SizeBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateApplySizeButton();
        if (sender != _boxBeingTyped)
            return;

        SizeErrorTip.IsOpen = false;

        // With a ratio locked, the other box follows as the user types, and the box being typed in is left alone. Only OK changes the crop.
        if (_geometry?.Ratio is double ratio && uint.TryParse(_boxBeingTyped.Text, out uint typed) && typed > 0)
        {
            SetControls(() =>
            {
                if (sender == WidthBox)
                    HeightBox.Text = Math.Round(typed / ratio, MidpointRounding.AwayFromZero).ToString();
                else
                    WidthBox.Text = Math.Round(typed * ratio, MidpointRounding.AwayFromZero).ToString();
            });
        }
    }

    private void SizeBox_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        // Applies the size, and must not reach Done, which also uses Enter.
        if (e.Key == VirtualKey.Enter)
        {
            ApplyTypedSize();
            e.Handled = true;
        }
        // Undoes the typing, and must not reach Cancel, which would lose the whole edit. With nothing typed, it does mean Cancel.
        else if (e.Key == VirtualKey.Escape && HasTypedChange)
        {
            ShowCropSize();
            e.Handled = true;
        }
    }

    private void ApplySizeButton_Click(object sender, RoutedEventArgs e) => ApplyTypedSize();

    private (uint width, uint height) CurrentOutputSize()
    {
        return ImageEditor.ResolveOutputSize(_uprightWidth, _uprightHeight, _geometry!.ToEdit(_quarterTurns, _fill));
    }

    /// <summary>
    /// True if the boxes hold anything other than the current size, which includes something that isn't a size.
    /// </summary>
    private bool HasTypedChange
    {
        get
        {
            if (_geometry == null)
                return false;

            (uint width, uint height) = CurrentOutputSize();
            return WidthBox.Text != width.ToString() || HeightBox.Text != height.ToString();
        }
    }

    private void UpdateApplySizeButton()
    {
        ApplySizeButton.Visibility = HasTypedChange ? Visibility.Visible : Visibility.Collapsed;
    }

    /// <summary>
    /// Applies a size waiting in the boxes.
    /// </summary>
    /// <returns>False if it can't be used, which leaves the crop and the boxes as they are, and says so.</returns>
    private bool ApplyTypedSize()
    {
        if (!HasTypedChange)
            return true;

        if (!uint.TryParse(WidthBox.Text, out uint width) || !uint.TryParse(HeightBox.Text, out uint height))
        {
            ShowSizeError("Enter both a width and a height.");
            return false;
        }

        CropGeometry geometry = _geometry!;
        if (geometry.IsTooLarge(width, height))
        {
            (double maxWidth, double maxHeight) = geometry.LargestAllowedSize;
            ShowSizeError($"This crop size is too large for this image. The largest allowed is {maxWidth} x {maxHeight}.");
            return false;
        }
        if (geometry.IsTooSmall(width, height))
        {
            ShowSizeError($"This crop size is too small. The smallest allowed is {geometry.MinCropWidth} x {geometry.MinCropHeight}.");
            return false;
        }

        geometry.SetCropSize(width, height);
        OnCropChanged();
        Focus(FocusState.Programmatic);
        return true;
    }

    private void ShowSizeError(string message)
    {
        SizeErrorText.Text = message;
        SizeErrorTip.IsOpen = true;

        // Done may have taken the focus. Back in the box, the size can be corrected right away, and Esc undoes the typing instead of canceling the whole edit.
        (_boxBeingTyped ?? WidthBox).Focus(FocusState.Programmatic);
    }

    private void SetBoxes(string width, string height)
    {
        _boxBeingTyped = null;
        SetControls(() =>
        {
            WidthBox.Text = width;
            HeightBox.Text = height;
        });
    }

    /// <summary>
    /// Shows the actual size, which replaces anything typed in the boxes and not applied.
    /// </summary>
    private void ShowCropSize()
    {
        (uint width, uint height) = CurrentOutputSize();
        SetBoxes(width.ToString(), height.ToString());
        UpdateApplySizeButton();
        SizeErrorTip.IsOpen = false;
    }

    private void OnCropChanged()
    {
        ShowImageAtNewZoom();
        ShowCropSize();
    }

    private void UpdateNotes()
    {
        CropGeometry geometry = _geometry!;
        ImageEdit? edit = geometry.ToEdit(_quarterTurns, _fill);
        ExclusionNote.Opacity = edit?.OverridesBatchResize == true ? 1 : 0;
        PushNote.Opacity = geometry.ImageRunsOffStage ? 1 : 0;

        bool hasSpill = edit?.Crop is CropRect crop && ImageEditor.ResolveCropOutput(crop, RotatedWidth, RotatedHeight, edit.Zoom).HasSpill;
        FillPreview.Visibility = hasSpill ? Visibility.Visible : Visibility.Collapsed;
        ShowBackgroundChoice(hasSpill);
    }

    /// <summary>
    /// Only offered while the crop has blank space to color. It fades in through its opacity transition, and the zoom controls next to it decide the row's height, so the image above doesn't move.
    /// </summary>
    private void ShowBackgroundChoice(bool show)
    {
        BackgroundChoice.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        BackgroundChoice.Opacity = show ? 1 : 0;
    }

    #endregion

    #region Dragging

    private void CropBox_PointerPressed(object sender, PointerRoutedEventArgs e) => StartDrag(sender, e, corner: null);

    private void Handle_PointerPressed(object sender, PointerRoutedEventArgs e)
        => StartDrag(sender, e, Enum.Parse<CropCorner>((string)((FrameworkElement)sender).Tag));

    private void StartDrag(object sender, PointerRoutedEventArgs e, CropCorner? corner)
    {
        if (!e.GetCurrentPoint(Stage).Properties.IsLeftButtonPressed)
            return;

        _dragging = ((UIElement)sender).CapturePointer(e.Pointer);
        _dragCorner = corner;
        _dragStartPointer = e.GetCurrentPoint(Stage).Position;
        _geometry!.BeginDrag();
        Focus(FocusState.Pointer);
        e.Handled = true;
    }

    private void Drag_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_dragging)
            return;

        CropGeometry geometry = _geometry!;
        Point pointer = e.GetCurrentPoint(Stage).Position;
        double dx = (pointer.X - _dragStartPointer.X) * geometry.PixelsPerDip;
        double dy = (pointer.Y - _dragStartPointer.Y) * geometry.PixelsPerDip;

        if (_dragCorner is CropCorner corner)
            geometry.ResizeCrop(corner, dx, dy);
        else
            geometry.DragCrop(dx, dy);
        OnCropChanged();
        e.Handled = true;
    }

    private void Drag_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        _dragging = false;
        ((UIElement)sender).ReleasePointerCapture(e.Pointer);
        e.Handled = true;
    }

    private void Drag_PointerCaptureLost(object sender, PointerRoutedEventArgs e) => _dragging = false;

    private void EditView_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        // In a size box the arrow keys move the caret, on the slider they change the zoom, and in a group they change the choice.
        if (_geometry is not CropGeometry geometry || e.OriginalSource is TextBox or Slider or RadioButton)
            return;

        // One output pixel, so a crop can be placed exactly. Shift for a step that's easy to see.
        bool shift = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Shift).HasFlag(Windows.UI.Core.CoreVirtualKeyStates.Down);
        double step = shift ? LargeNudgeDips * geometry.PixelsPerDip : 1;
        (double dx, double dy) = e.Key switch
        {
            VirtualKey.Left => (-step, 0.0),
            VirtualKey.Right => (step, 0.0),
            VirtualKey.Up => (0.0, -step),
            VirtualKey.Down => (0.0, step),
            _ => (0.0, 0.0),
        };
        if (dx == 0 && dy == 0)
            return;

        geometry.BeginDrag();
        geometry.DragCrop(dx, dy, snap: false);
        OnCropChanged();
        e.Handled = true;
    }

    #endregion

    #region Drawing

    private void StageHost_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_geometry == null)
        {
            StartEditingIfReady();
            return;
        }

        if (HasStage)
        {
            _geometry.SetStage(StageWidth, StageHeight);
            ShowImage();
        }
    }

    private void UpdateStage()
    {
        CropGeometry geometry = _geometry!;
        double stageWidth = geometry.StageWidth;
        double stageHeight = geometry.StageHeight;
        ImageLayerClip.Rect = new Rect(0, 0, stageWidth, stageHeight);

        PixelRect image = geometry.ImageRect;
        double imageX = geometry.ToStageX(image.X);
        double imageY = geometry.ToStageY(image.Y);
        double imageWidth = image.Width / geometry.PixelsPerDip;
        double imageHeight = image.Height / geometry.PixelsPerDip;

        // Drawn at its unrotated size and turned about its center, which gives the rotated image's footprint.
        double unrotatedWidth = IsSideways ? imageHeight : imageWidth;
        double unrotatedHeight = IsSideways ? imageWidth : imageHeight;
        EditImage.Width = unrotatedWidth;
        EditImage.Height = unrotatedHeight;
        Place(EditImage, imageX + (imageWidth - unrotatedWidth) / 2, imageY + (imageHeight - unrotatedHeight) / 2);
        EditImageRotation.Angle = _quarterTurns * 90;

        // Whole screen units for drawing only: the shades would otherwise show a faint seam where they meet.
        PixelRect crop = geometry.Crop;
        double left = Math.Round(geometry.ToStageX(crop.X));
        double top = Math.Round(geometry.ToStageY(crop.Y));
        double right = Math.Round(geometry.ToStageX(crop.Right));
        double bottom = Math.Round(geometry.ToStageY(crop.Bottom));

        PlaceSized(ShadeTop, 0, 0, stageWidth, top);
        PlaceSized(ShadeBottom, 0, bottom, stageWidth, stageHeight - bottom);
        PlaceSized(ShadeLeft, 0, top, left, bottom - top);
        PlaceSized(ShadeRight, right, top, stageWidth - right, bottom - top);
        PlaceSized(FillPreview, left, top, right - left, bottom - top);
        PlaceSized(CropBox, left, top, right - left, bottom - top);

        PlaceHandle(HandleTopLeft, left, top);
        PlaceHandle(HandleTopRight, right, top);
        PlaceHandle(HandleBottomRight, right, bottom);
        PlaceHandle(HandleBottomLeft, left, bottom);
    }

    private static void Place(UIElement element, double x, double y)
    {
        Canvas.SetLeft(element, x);
        Canvas.SetTop(element, y);
    }

    private static void PlaceSized(FrameworkElement element, double x, double y, double width, double height)
    {
        Place(element, x, y);
        element.Width = Math.Max(0, width);
        element.Height = Math.Max(0, height);
    }

    private static void PlaceHandle(UIElement handle, double x, double y) => Place(handle, x - HandleSizeDips / 2, y - HandleSizeDips / 2);

    #endregion
}
