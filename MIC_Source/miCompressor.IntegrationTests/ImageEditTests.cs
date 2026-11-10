using ImageMagick;
using miCompressor.core;

namespace miCompressor.IntegrationTests;

/// <summary>
/// Source images have four colored quadrants, so tests can check which part of the image was kept and which way it was turned.
/// In this non-parallel collection as edits are stored per process, and any FileStore can remove them.
/// </summary>
[Collection("Image compression")]
public class ImageEditTests : IDisposable
{
    private const int SourceWidth = 400;
    private const int SourceHeight = 200;
    private static readonly CropRect TopLeftQuadrant = new(0, 0, 0.5, 0.5);
    private static readonly CropRect BottomRightQuadrant = new(0.5, 0.5, 0.5, 0.5);
    private static readonly MagickColor Red = MagickColors.Red, Lime = MagickColors.Lime, Blue = MagickColors.Blue, Yellow = MagickColors.Yellow;

    private readonly string _testRoot;

    public ImageEditTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), "miCompressor-edit-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testRoot);
    }

    #region Crop geometry

    private const double DisplayWidth = 964;
    private const double DisplayHeight = 510;

    private static CropGeometry LargePhoto(double? ratio = null) => new(6000, 4000, DisplayWidth, DisplayHeight, ratio);

    /// <summary>
    /// A display area twice the size of the source, so one screen unit is one output pixel and the plane spans -400 to 400 across.
    /// </summary>
    private static CropGeometry SourceInLargeDisplay(double? ratio = null) => new(SourceWidth, SourceHeight, 800, 400, ratio);

    private static (uint width, uint height) OutputSizeOf(CropGeometry geometry)
    {
        ImageEdit? edit = geometry.ToEdit(0, CropFill.Black);
        return ImageEditor.ResolveOutputSize((uint)geometry.ImageWidth, (uint)geometry.ImageHeight, edit);
    }

    [Theory]
    [InlineData(6000, 4000, null)]
    [InlineData(6000, 4000, 1.0)]
    [InlineData(6000, 4000, 0.8)]
    [InlineData(6000, 4000, 16.0 / 9.0)]
    [InlineData(6000, 4000, 9.0 / 16.0)]
    [InlineData(4000, 6000, 16.0 / 9.0)]
    [InlineData(400, 300, 1.0)]
    [InlineData(30, 30, 16.0 / 9.0)]
    [InlineData(10000, 20, 1.0)]
    [InlineData(20, 10000, 0.8)]
    public void OnLoad_CropIsCenteredOnTheImage_FollowsTheRatio_AndStaysInsideTheImage(double imageWidth, double imageHeight, double? ratio)
    {
        var geometry = new CropGeometry(imageWidth, imageHeight, DisplayWidth, DisplayHeight, ratio);
        PixelRect crop = geometry.Crop;
        PixelRect image = geometry.ImageRect;
        const double Precision = 1e-6;

        Assert.Equal(image.CenterX, crop.CenterX, Precision);
        Assert.Equal(image.CenterY, crop.CenterY, Precision);
        Assert.True(crop.X >= image.X - Precision && crop.Y >= image.Y - Precision);
        Assert.True(crop.Right <= image.Right + Precision && crop.Bottom <= image.Bottom + Precision);
        if (ratio is double locked)
            Assert.Equal(locked, crop.Width / crop.Height, Precision);
        // The largest that fits: it touches the image on at least one axis.
        Assert.True(Math.Abs(crop.Width - image.Width) < Precision || Math.Abs(crop.Height - image.Height) < Precision);
        Assert.False(geometry.ImageRunsOffStage);
    }

    [Theory]
    [InlineData(6000, 4000, 4000 / DisplayHeight)] // larger than the display, so fitted
    [InlineData(400, 300, 1)] // smaller than the display, so at actual pixels
    public void OnLoad_ImageIsAt100PercentOrFitted(double imageWidth, double imageHeight, double expectedScale)
    {
        var geometry = new CropGeometry(imageWidth, imageHeight, DisplayWidth, DisplayHeight, null);

        Assert.Equal(expectedScale, geometry.PixelsPerDip, 6);
        Assert.Equal(1, geometry.Zoom);
    }

    [Fact]
    public void DraggingAHandle_ChangesTheCropByTheDistanceTimesTheScreenScale()
    {
        CropGeometry geometry = LargePhoto();
        double scale = geometry.PixelsPerDip;

        geometry.BeginDrag();
        geometry.ResizeCrop(CropCorner.BottomRight, -100 * scale, -50 * scale);

        Assert.Equal(6000 - 100 * scale, geometry.Crop.Width, 6);
        Assert.Equal(4000 - 50 * scale, geometry.Crop.Height, 6);
        Assert.Equal(scale, geometry.PixelsPerDip);
    }

    [Fact]
    public void ZoomFitFillAndWindowSize_NeverChangeTheOutputSize()
    {
        CropGeometry geometry = LargePhoto();
        geometry.SetCropSize(1920, 1080);

        foreach (double zoom in new[] { 0.01, 0.27, 1.5, 4 })
        {
            geometry.SetZoom(zoom);
            Assert.Equal((1920u, 1080u), OutputSizeOf(geometry));
        }

        geometry.FitImageInCrop();
        Assert.Equal((1920u, 1080u), OutputSizeOf(geometry));
        geometry.FillCropWithImage();
        Assert.Equal((1920u, 1080u), OutputSizeOf(geometry));

        geometry.SetStage(300, 200);
        Assert.Equal((1920u, 1080u), OutputSizeOf(geometry));
        geometry.SetStage(2000, 1500);
        Assert.Equal((1920u, 1080u), OutputSizeOf(geometry));
    }

    [Fact]
    public void Zoom_DoesNotChangeTheScreenScale()
    {
        CropGeometry geometry = LargePhoto();
        double scale = geometry.PixelsPerDip;

        geometry.SetZoom(0.25);
        geometry.SetZoom(3);

        Assert.Equal(scale, geometry.PixelsPerDip);
    }

    [Fact]
    public void Zoom_KeepsTheCropStill_AndWhatIsInItsCenter()
    {
        CropGeometry geometry = SourceInLargeDisplay();
        geometry.SetCropSize(100, 100);
        geometry.BeginDrag();
        geometry.DragCrop(60, 30);
        PixelRect crop = geometry.Crop;
        double FractionAtCropCenter() => (crop.CenterX - geometry.ImageRect.X) / geometry.ImageRect.Width;
        double before = FractionAtCropCenter();

        geometry.SetZoom(3);

        Assert.Equal(crop, geometry.Crop);
        Assert.Equal(before, FractionAtCropCenter(), 9);
    }

    [Fact]
    public void Fit_PutsTheWholeImageInsideTheCrop_Centered()
    {
        CropGeometry geometry = LargePhoto();
        geometry.SetCropSize(1080, 1080);

        geometry.FitImageInCrop();

        Assert.Equal(0.18, geometry.Zoom, 9);
        Assert.Equal(new PixelRect(geometry.Crop.X, geometry.Crop.Y + 180, 1080, 720), Rounded(geometry.ImageRect, geometry.Crop));
    }

    [Fact]
    public void Fill_CoversTheCropWithTheImage_Centered()
    {
        CropGeometry geometry = LargePhoto();
        geometry.SetCropSize(1080, 1080);

        geometry.FillCropWithImage();

        Assert.Equal(0.27, geometry.Zoom, 9);
        Assert.Equal(new PixelRect(geometry.Crop.X - 270, geometry.Crop.Y, 1620, 1080), Rounded(geometry.ImageRect, geometry.Crop));
        Assert.False(ImageEditor.ResolveCropOutput(geometry.ToEdit(0, CropFill.Black)!.Crop!.Value, 6000, 4000, geometry.Zoom).HasSpill);
    }

    /// <summary>
    /// Removes floating point noise from a rectangle measured around the crop.
    /// </summary>
    private static PixelRect Rounded(PixelRect rect, PixelRect crop) => new(
        crop.X + Math.Round(rect.X - crop.X, 6),
        crop.Y + Math.Round(rect.Y - crop.Y, 6),
        Math.Round(rect.Width, 6),
        Math.Round(rect.Height, 6));

    [Theory]
    [InlineData(1080, 408)] // a small crop is drawn large, which enlarges the image on screen
    [InlineData(2000, 408)]
    [InlineData(3000, 408)]
    [InlineData(4000, 510)] // would make the image smaller than fitted, so the view stays fitted
    [InlineData(6000, 510)] // doesn't fit otherwise, so the image gets smaller on screen
    public void TypedSize_RescalesTheScreen_ToShowTheCropLarge(double side, double expectedSideOnScreen)
    {
        CropGeometry geometry = LargePhoto();

        geometry.SetCropSize(side, side);

        Assert.Equal(expectedSideOnScreen, geometry.Crop.Height / geometry.PixelsPerDip, 6);
        Assert.Equal(0, geometry.Crop.CenterX, 6);
        Assert.Equal(0, geometry.Crop.CenterY, 6);
    }

    [Fact]
    public void TypedSize_NeverDrawsTheImageLargerThanActualPixels()
    {
        var geometry = new CropGeometry(400, 300, DisplayWidth, DisplayHeight, null);

        geometry.SetCropSize(100, 100);

        Assert.Equal(1, geometry.PixelsPerDip);
    }

    [Theory]
    [InlineData(4032u, 3024u)]
    [InlineData(333u, 777u)]
    [InlineData(1921u, 1079u)]
    public void TypedSize_IsExactlyTheOutputSize(uint imageWidth, uint imageHeight)
    {
        // Fractions go through floating point, so a size can only be promised if every one survives that.
        var geometry = new CropGeometry(imageWidth, imageHeight, DisplayWidth, DisplayHeight, null);
        for (uint width = 16; width < imageWidth; width += 7)
        {
            uint height = 16 + width * (imageHeight - 17) / imageWidth;

            geometry.SetCropSize(width, height);

            Assert.Equal((width, height), OutputSizeOf(geometry));
        }
    }

    [Fact]
    public void TypedSize_MovesTheCropToStayInsideTheImage_IfItFits()
    {
        CropGeometry geometry = SourceInLargeDisplay();
        geometry.SetCropSize(50, 50);
        geometry.BeginDrag();
        geometry.DragCrop(-9000, -9000, snap: false); // now past the top left of the image
        geometry.SetCropSize(300, 150);

        PixelRect image = geometry.ImageRect;
        Assert.True(geometry.Crop.X >= image.X && geometry.Crop.Y >= image.Y);
        Assert.True(geometry.Crop.Right <= image.Right && geometry.Crop.Bottom <= image.Bottom);
    }

    [Fact]
    public void TypedSize_LargerThanTheImage_CoversTheImage()
    {
        CropGeometry geometry = SourceInLargeDisplay();

        geometry.SetCropSize(600, 300);

        PixelRect image = geometry.ImageRect;
        Assert.True(geometry.Crop.X <= image.X && geometry.Crop.Y <= image.Y);
        Assert.True(geometry.Crop.Right >= image.Right && geometry.Crop.Bottom >= image.Bottom);
    }

    [Theory]
    [InlineData(9000, 6000, false)]
    [InlineData(9001, 6000, true)]
    [InlineData(9000, 6001, true)]
    public void CropSize_CanBeAtMost150PercentOfTheImage(double width, double height, bool tooLarge)
    {
        Assert.Equal(tooLarge, LargePhoto().IsTooLarge(width, height));
    }

    [Theory]
    [InlineData(null, 9000, 6000)]
    [InlineData(1.0, 6000, 6000)]
    [InlineData(2.0, 9000, 4500)]
    public void LargestAllowedSize_FollowsTheRatio(double? ratio, double width, double height)
    {
        Assert.Equal((width, height), LargePhoto(ratio).LargestAllowedSize);
    }

    [Fact]
    public void CropSize_HasAMinimum_UnlessTheImageIsTiny()
    {
        Assert.True(LargePhoto().IsTooSmall(15, 100));
        Assert.False(LargePhoto().IsTooSmall(16, 16));

        var tiny = new CropGeometry(5, 4, DisplayWidth, DisplayHeight, null);
        Assert.False(tiny.IsTooSmall(7, 6)); // its largest allowed size
        Assert.True(tiny.IsTooLarge(8, 6));
    }

    [Fact]
    public void DraggingAHandle_StopsAtTheSizeLimit()
    {
        var geometry = new CropGeometry(100, 100, DisplayWidth, DisplayHeight, null);

        geometry.BeginDrag();
        geometry.ResizeCrop(CropCorner.BottomRight, 9000, 9000);

        Assert.Equal((150, 150), (geometry.Crop.Width, geometry.Crop.Height));
    }

    [Fact]
    public void DraggingAHandle_StopsAtTheDisplayEdge()
    {
        var geometry = new CropGeometry(400, 400, 500, 500, null);

        geometry.BeginDrag();
        geometry.ResizeCrop(CropCorner.BottomRight, 9000, 9000);

        Assert.Equal((450, 450), (geometry.Crop.Width, geometry.Crop.Height)); // from the image's far corner to the display's edge
    }

    [Theory]
    [InlineData(-10, -20, 110, 120)]
    [InlineData(9000, 9000, CropGeometry.MinSidePixels, CropGeometry.MinSidePixels)]
    public void DraggingAHandle_HoldsTheOppositeCorner(double dx, double dy, double width, double height)
    {
        CropGeometry geometry = SourceInLargeDisplay();
        geometry.SetCropSize(100, 100);
        PixelRect start = geometry.Crop;

        geometry.BeginDrag();
        geometry.ResizeCrop(CropCorner.TopLeft, dx, dy);

        Assert.Equal(start.Right, geometry.Crop.Right);
        Assert.Equal(start.Bottom, geometry.Crop.Bottom);
        Assert.Equal((width, height), (geometry.Crop.Width, geometry.Crop.Height));
    }

    [Theory]
    [InlineData(50, 0, 150, 75)] // grows with the direction that was dragged
    [InlineData(-50, 0, 50, 25)] // a sideways drag can shrink it
    [InlineData(0, -10, 80, 40)]
    [InlineData(-9000, -9000, 32, 16)] // limited by the minimum, still 2:1
    public void DraggingAHandle_WithARatio_KeepsTheRatio(double dx, double dy, double width, double height)
    {
        CropGeometry geometry = SourceInLargeDisplay(2);
        geometry.SetCropSize(100, 50);

        geometry.BeginDrag();
        geometry.ResizeCrop(CropCorner.BottomRight, dx, dy);

        Assert.Equal((width, height), (geometry.Crop.Width, geometry.Crop.Height));
    }

    [Fact]
    public void DraggingTheCrop_CanGoPastTheImage_UpToTheDisplayEdge_WithoutMovingTheImage()
    {
        CropGeometry geometry = SourceInLargeDisplay();
        geometry.SetCropSize(100, 100);

        geometry.BeginDrag();
        geometry.DragCrop(-9000, 0);

        Assert.Equal(-400, geometry.Crop.X);
        Assert.Equal(0, geometry.ImageCenterX);
    }

    [Theory]
    [InlineData(-146, -200)] // the left edge comes within 4 units of the image's, so it lands on it
    [InlineData(-154, -200)] // a little past the edge also lands on it, so a drag can't overshoot by accident
    [InlineData(-170, -220)] // well past it stays there, as a deliberate choice
    [InlineData(-120, -170)] // well inside stays there too
    public void DraggingTheCrop_SnapsToTheImageEdge_OnlyWhenClose(double dx, double expectedX)
    {
        CropGeometry geometry = SourceInLargeDisplay();
        geometry.SetCropSize(100, 100);

        geometry.BeginDrag();
        geometry.DragCrop(dx, 0);

        Assert.Equal(expectedX, geometry.Crop.X);
    }

    [Fact]
    public void ArrowKeys_DontSnap_SoAThinTrimIsPossible()
    {
        CropGeometry geometry = new(SourceWidth, SourceHeight, 800, 400, null);
        geometry.BeginDrag();
        geometry.ResizeCrop(CropCorner.TopLeft, 10, 0);

        geometry.BeginDrag();
        geometry.DragCrop(-9, 0, snap: false);

        Assert.Equal(geometry.ImageRect.X + 1, geometry.Crop.X);
    }

    [Fact]
    public void PushingTheCropAgainstTheDisplayEdge_MovesTheImageTheOtherWay()
    {
        CropGeometry geometry = SourceInLargeDisplay();
        geometry.SetCropSize(100, 100);
        geometry.SetZoom(4); // 1600 x 800 on a display area of 800 x 400

        geometry.BeginDrag();
        geometry.DragCrop(-350, 0); // reaches the display's edge
        Assert.Equal(-400, geometry.Crop.X);
        Assert.Equal(0, geometry.ImageCenterX);

        geometry.DragCrop(-400, 0); // measured from the start of the drag, so 50 past the edge
        Assert.Equal(-400, geometry.Crop.X);
        Assert.Equal(50, geometry.ImageCenterX);

        geometry.DragCrop(-300, 0); // coming back moves the crop again, and the image back
        Assert.Equal(-350, geometry.Crop.X);
        Assert.Equal(0, geometry.ImageCenterX);
    }

    [Theory]
    [InlineData(-9000, 0)]
    [InlineData(9000, 0)]
    [InlineData(0, -9000)]
    [InlineData(0, 9000)]
    public void Pushing_StopsAtTheImageEdge_SoItCantAddBlankSpace(double dx, double dy)
    {
        CropGeometry geometry = SourceInLargeDisplay();
        geometry.SetCropSize(100, 100);
        geometry.SetZoom(4);

        geometry.BeginDrag();
        geometry.DragCrop(dx, dy);

        PixelRect crop = geometry.Crop;
        PixelRect image = geometry.ImageRect;
        Assert.True(crop.X >= image.X && crop.Y >= image.Y && crop.Right <= image.Right && crop.Bottom <= image.Bottom);
        Assert.False(ImageEditor.ResolveCropOutput(geometry.ToEdit(0, CropFill.Black)!.Crop!.Value, SourceWidth, SourceHeight, 4).HasSpill);
    }

    [Fact]
    public void Pushing_ACropThatFillsTheDisplay_MovesTheImageOnAnyDrag()
    {
        var geometry = new CropGeometry(SourceWidth, SourceHeight, 400, 200, null); // the crop is the image, which fills the display
        geometry.SetZoom(2);

        geometry.BeginDrag();
        geometry.DragCrop(-100, 0);

        Assert.Equal(-200, geometry.Crop.X);
        Assert.Equal(100, geometry.ImageCenterX);
    }

    [Fact]
    public void ImageRunsOffStage_OnlyWhenPartOfItIsOutsideTheDisplay()
    {
        CropGeometry geometry = SourceInLargeDisplay();
        Assert.False(geometry.ImageRunsOffStage);

        geometry.SetZoom(1.5);
        Assert.False(geometry.ImageRunsOffStage);

        geometry.SetZoom(2.5);
        Assert.True(geometry.ImageRunsOffStage);
    }

    [Fact]
    public void SmallerWindow_ShrinksTheViewToFitTheCrop_AndALargerOneChangesNothing()
    {
        CropGeometry geometry = LargePhoto();
        double scale = geometry.PixelsPerDip;

        geometry.SetStage(482, 255);
        double shrunk = geometry.PixelsPerDip;
        Assert.Equal(scale * 2, shrunk, 6);
        Assert.Equal((6000u, 4000u), OutputSizeOf(geometry));

        geometry.SetStage(2000, 1500);
        Assert.Equal(shrunk, geometry.PixelsPerDip);
    }

    [Fact]
    public void PickingARatio_StartsOver_AndFreeKeepsTheCrop()
    {
        CropGeometry geometry = LargePhoto();
        geometry.SetCropSize(1080, 1080);
        geometry.FillCropWithImage();

        geometry.SetRatio(null);
        Assert.Equal(1080, geometry.Crop.Width);
        Assert.Equal(0.27, geometry.Zoom, 9);

        geometry.SetRatio(1);
        Assert.Equal(1, geometry.Zoom);
        Assert.Equal(4000, geometry.Crop.Width, 6);
    }

    [Fact]
    public void ToEdit_OnlyKeepsWhatChanged()
    {
        Assert.Null(SourceInLargeDisplay().ToEdit(0, CropFill.Black));
        Assert.Equal(new ImageEdit(1, null), SourceInLargeDisplay().ToEdit(1, CropFill.Black));
        Assert.NotNull(SourceInLargeDisplay(1).ToEdit(0, CropFill.Black)?.Crop);
    }

    [Fact]
    public void ToEdit_TheWholeImageAtAnotherZoom_IsACrop_SoThatItResizes()
    {
        CropGeometry geometry = SourceInLargeDisplay();
        geometry.SetCropSize(200, 100);
        geometry.FitImageInCrop();

        ImageEdit edit = geometry.ToEdit(0, CropFill.Black)!;

        Assert.Equal(new CropRect(0, 0, 1, 1), edit.Crop);
        Assert.Equal(0.5, edit.Zoom);
        Assert.Equal((200u, 100u), OutputSizeOf(geometry));
    }

    [Fact]
    public void ToEdit_ACropLargerThanTheImage_IsACrop()
    {
        CropGeometry geometry = SourceInLargeDisplay();
        geometry.SetCropSize(500, 200);

        Assert.NotNull(geometry.ToEdit(0, CropFill.Black)?.Crop);
    }

    [Fact]
    public void Restore_GivesTheSameEditBack_WithTheRatioLocked()
    {
        CropGeometry geometry = LargePhoto(16.0 / 9.0);
        geometry.SetCropSize(1920, 1080);
        geometry.SetZoom(0.4);
        geometry.BeginDrag();
        geometry.DragCrop(300, -200);
        ImageEdit edit = geometry.ToEdit(0, CropFill.White)!;

        CropGeometry restored = CropGeometry.Restore(edit.Crop!.Value, edit.Zoom, edit.LockedRatio, 6000, 4000, DisplayWidth, DisplayHeight);
        ImageEdit again = restored.ToEdit(0, CropFill.White)!;

        Assert.Equal(16.0 / 9.0, restored.Ratio);
        Assert.Equal(edit.Zoom, again.Zoom);
        Assert.Equal(edit.Crop.Value.X, again.Crop!.Value.X, 9);
        Assert.Equal(edit.Crop.Value.Y, again.Crop.Value.Y, 9);
        Assert.Equal(edit.Crop.Value.Width, again.Crop.Value.Width, 9);
        Assert.Equal(edit.Crop.Value.Height, again.Crop.Value.Height, 9);
        Assert.Equal((1920u, 1080u), OutputSizeOf(restored));
    }

    [Theory]
    [InlineData(0, 0, 1, 1, 1, 400u, 200u, 0, 0, 400u, 200u, false)]
    [InlineData(0.5, 0.5, 0.5, 0.5, 1, 200u, 100u, 200, 100, 200u, 100u, false)]
    [InlineData(0.5, 0.5, 0, 0, 1, 1u, 1u, 200, 100, 1u, 1u, false)] // at least 1 pixel
    [InlineData(0, 0, 1.5, 1, 1, 600u, 200u, 0, 0, 600u, 200u, true)] // reaches past the right edge
    [InlineData(-0.25, 0, 1.5, 1, 1, 600u, 200u, -100, 0, 600u, 200u, true)] // reaches past both sides
    [InlineData(1.5, 0, 0.5, 1, 1, 200u, 200u, 600, 0, 200u, 200u, true)] // wholly beside the image
    [InlineData(0, 0, 1, 1, 0.5, 200u, 100u, 0, 0, 400u, 200u, false)] // zoom only changes the output's size
    [InlineData(0.25, 0, 0.5, 1, 3, 600u, 600u, 100, 0, 200u, 200u, false)]
    public void ResolveCropOutput_GivesSizeAndSource(double x, double y, double width, double height, double zoom, uint expectedWidth, uint expectedHeight, int sourceX, int sourceY, uint sourceWidth, uint sourceHeight, bool expectedSpill)
    {
        CropOutput output = ImageEditor.ResolveCropOutput(new CropRect(x, y, width, height), SourceWidth, SourceHeight, zoom);

        Assert.Equal(new CropOutput(expectedWidth, expectedHeight, sourceX, sourceY, sourceWidth, sourceHeight, expectedSpill), output);
    }

    [Theory]
    [InlineData(0, 400u, 200u)]
    [InlineData(2, 400u, 200u)]
    [InlineData(1, 200u, 400u)]
    [InlineData(3, 200u, 400u)]
    public void ResolveOutputSize_SwapsSidesOnQuarterTurns(int quarterTurns, uint width, uint height)
    {
        Assert.Equal((width, height), ImageEditor.ResolveOutputSize(SourceWidth, SourceHeight, new ImageEdit(quarterTurns, null)));
    }

    [Fact]
    public void ResolveOutputSize_CropsTheRotatedImage_AndNoEditKeepsTheSize()
    {
        Assert.Equal((100u, 200u), ImageEditor.ResolveOutputSize(SourceWidth, SourceHeight, new ImageEdit(1, TopLeftQuadrant)));
        Assert.Equal((400u, 200u), ImageEditor.ResolveOutputSize(SourceWidth, SourceHeight, null));
    }

    #endregion

    #region Edit sharing

    [Fact]
    public void Store_ReportsChangesAndCountsCrops()
    {
        List<string> changes = [];
        void OnChanged(string path) => changes.Add(path);
        ImageEditStore.Changed += OnChanged;
        try
        {
            ImageEditStore.Set("a.png", new ImageEdit(0, TopLeftQuadrant));
            ImageEditStore.Set("b.png", new ImageEdit(1, null));
            Assert.Equal(1, ImageEditStore.ResizeOverrideCount);

            ImageEditStore.Set("missing.png", null); // nothing to remove, so no change
            ImageEditStore.RetainOnly(["b.png"]);

            Assert.Equal(["a.png", "b.png", "a.png"], changes);
            Assert.Equal(0, ImageEditStore.ResizeOverrideCount);
        }
        finally
        {
            ImageEditStore.Changed -= OnChanged;
        }
    }

    [Fact]
    public void EditIsSharedByAllObjectsOfTheSameFile_IgnoringPathCase()
    {
        string input = CreateQuadrants("shared.png");
        var first = new MediaFileInfo(_testRoot, new FileInfo(input));
        var second = new MediaFileInfo(_testRoot, new FileInfo(input));
        var differentCase = new MediaFileInfo(_testRoot, new FileInfo(input.ToUpperInvariant()));
        ImageEdit edit = new(1, TopLeftQuadrant);

        first.Edit = edit;

        Assert.Equal(edit, second.Edit);
        Assert.Equal(edit, differentCase.Edit);
        Assert.True(second.OverridesBatchResize);

        second.Edit = null;

        Assert.Null(first.Edit);
        Assert.False(first.OverridesBatchResize);
    }

    [Fact]
    public void RotateOnlyEdit_IsNotCropped()
    {
        var media = new MediaFileInfo(_testRoot, new FileInfo(CreateQuadrants("rotated.png")));

        media.Edit = new ImageEdit(1, null);

        Assert.False(media.OverridesBatchResize);
    }

    #endregion

    #region Crop and rotate in output

    [Theory]
    [InlineData(OutputFormat.Png, ".png")]
    [InlineData(OutputFormat.Jpg, ".jpg")]
    [InlineData(OutputFormat.Webp, ".webp")]
    [InlineData(OutputFormat.avif, ".avif")]
    [InlineData(OutputFormat.Jxl, ".jxl")]
    [InlineData(OutputFormat.Gif, ".gif")]
    public async Task Crop_KeepsChosenPart_InEveryFormat(OutputFormat format, string extension)
    {
        string input = CreateQuadrants("crop.png");

        string output = await CompressAsync(input, new ImageEdit(0, BottomRightQuadrant), CreateSettings(format));

        Assert.Equal(extension, Path.GetExtension(output), ignoreCase: true);
        using var image = new MagickImage(output);
        Assert.Equal(SourceWidth / 2u, image.Width);
        Assert.Equal(SourceHeight / 2u, image.Height);
        AssertColorNear(Yellow, image, 0.5, 0.5);
        // No trace of the position on the old canvas, which formats such as GIF store.
        Assert.Equal(0, image.Page.X);
        Assert.Equal(0, image.Page.Y);
    }

    [Theory]
    [InlineData(1, 200u, 400u)]
    [InlineData(2, 400u, 200u)]
    [InlineData(3, 200u, 400u)]
    public async Task Rotate_TurnsClockwise(int quarterTurns, uint expectedWidth, uint expectedHeight)
    {
        string input = CreateQuadrants($"rotate{quarterTurns}.png");

        string output = await CompressAsync(input, new ImageEdit(quarterTurns, null), CreateSettings(OutputFormat.Png));

        using var image = new MagickImage(output);
        Assert.Equal(expectedWidth, image.Width);
        Assert.Equal(expectedHeight, image.Height);
        // Corners in the order top left, top right, bottom right, bottom left turn with the image.
        MagickColor[] corners = [Red, Lime, Yellow, Blue];
        AssertColorNear(corners[(4 - quarterTurns) % 4], image, 0.25, 0.25);
        AssertColorNear(corners[(5 - quarterTurns) % 4], image, 0.75, 0.25);
        AssertColorNear(corners[(6 - quarterTurns) % 4], image, 0.75, 0.75);
        AssertColorNear(corners[(7 - quarterTurns) % 4], image, 0.25, 0.75);
    }

    [Fact]
    public async Task RotateThenCrop_CropsTheRotatedImage()
    {
        string input = CreateQuadrants("rotate-crop.png");

        // After a clockwise quarter turn, blue is in the top left.
        string output = await CompressAsync(input, new ImageEdit(1, TopLeftQuadrant), CreateSettings(OutputFormat.Png));

        using var image = new MagickImage(output);
        Assert.Equal(SourceHeight / 2u, image.Width);
        Assert.Equal(SourceWidth / 2u, image.Height);
        AssertColorNear(Blue, image, 0.5, 0.5);
    }

    [Fact]
    public async Task ExifRotatedJpeg_IsCroppedAsDisplayed()
    {
        string input = Path.Combine(_testRoot, "exif.jpg");
        using (var source = CreateQuadrantsImage())
        {
            // Displayed turned clockwise, blue in the top left. Magick writes the tag from the property, into a profile that must already have it.
            var exif = new ExifProfile();
            exif.SetValue(ExifTag.Orientation, (ushort)Orientation.RightTop);
            source.SetProfile(exif);
            source.Orientation = Orientation.RightTop;
            source.Write(input, MagickFormat.Jpeg);
        }
        Assert.Equal(Orientation.RightTop, new MagickImageInfo(input).Orientation);

        string output = await CompressAsync(input, new ImageEdit(0, TopLeftQuadrant), CreateSettings(OutputFormat.Png));

        using var image = new MagickImage(output);
        Assert.Equal(SourceHeight / 2u, image.Width);
        Assert.Equal(SourceWidth / 2u, image.Height);
        AssertColorNear(Blue, image, 0.5, 0.5);
    }

    [Fact]
    public async Task IconOutput_UsesCroppedImage()
    {
        string input = CreateQuadrants("icon.png");
        OutputSettings settings = CreateSettings(OutputFormat.Ico);

        string output = await CompressAsync(input, new ImageEdit(0, TopLeftQuadrant), settings);

        using var icon = new MagickImage(output);
        AssertColorNear(Red, icon, 0.5, 0.5); // the whole image would have yellow here
    }

    [Fact]
    public async Task NoEdit_OutputIsTheWholeImage()
    {
        string input = CreateQuadrants("whole.png");

        string output = await CompressAsync(input, null, CreateSettings(OutputFormat.Png));

        using var image = new MagickImage(output);
        Assert.Equal((uint)SourceWidth, image.Width);
        Assert.Equal((uint)SourceHeight, image.Height);
        AssertColorNear(Red, image, 0.25, 0.25);
        AssertColorNear(Yellow, image, 0.75, 0.75);
    }

    #endregion

    #region Batch resize

    [Theory]
    [InlineData(DimensionReductionStrategy.Percentage)]
    [InlineData(DimensionReductionStrategy.LongEdge)]
    [InlineData(DimensionReductionStrategy.FixedWidth)]
    public async Task CroppedImage_IsExcludedFromBatchResize(DimensionReductionStrategy strategy)
    {
        string input = CreateQuadrants("crop-resize.png");

        string output = await CompressAsync(input, new ImageEdit(0, TopLeftQuadrant), CreateResizeSettings(strategy));

        using var image = new MagickImage(output);
        Assert.Equal(SourceWidth / 2u, image.Width);
        Assert.Equal(SourceHeight / 2u, image.Height);
    }

    [Fact]
    public async Task RotateOnlyImage_FollowsBatchResize_OnItsRotatedSize()
    {
        string input = CreateQuadrants("rotate-resize.png");

        string output = await CompressAsync(input, new ImageEdit(1, null), CreateResizeSettings(DimensionReductionStrategy.Percentage));

        using var image = new MagickImage(output);
        Assert.Equal(SourceHeight / 4u, image.Width);
        Assert.Equal(SourceWidth / 4u, image.Height);
    }

    [Fact]
    public async Task UneditedImage_FollowsBatchResize()
    {
        string input = CreateQuadrants("plain-resize.png");

        string output = await CompressAsync(input, null, CreateResizeSettings(DimensionReductionStrategy.Percentage));

        using var image = new MagickImage(output);
        Assert.Equal(SourceWidth / 4u, image.Width);
        Assert.Equal(SourceHeight / 4u, image.Height);
    }

    #endregion


    #region Rounding to whole pixels

    [Theory]
    [InlineData(1080, 1080, 16.0 / 9.0)] // 607.5 tall, so it can sit at half-pixel positions
    [InlineData(1081, 1080, 16.0 / 9.0)]
    [InlineData(1921, 1079, 1.0)]
    [InlineData(1079, 1921, 0.8)]
    [InlineData(333, 777, 9.0 / 16.0)]
    [InlineData(1000, 1000, 1.3)]
    public void ACropAgainstAnImageEdge_StaysInsideTheImage_AtTheSizeShown(double imageWidth, double imageHeight, double ratio)
    {
        foreach ((double dx, double dy) in new[] { (-9000.0, 0.0), (9000.0, 0.0), (0.0, -9000.0), (0.0, 9000.0), (-9000.0, -9000.0), (9000.0, 9000.0) })
        {
            var geometry = new CropGeometry(imageWidth, imageHeight, imageWidth, imageHeight, ratio);
            geometry.BeginDrag();
            geometry.DragCrop(dx, dy);

            CropOutput output = ImageEditor.ResolveCropOutput(geometry.ToEdit(0, CropFill.Black)!.Crop!.Value, (uint)imageWidth, (uint)imageHeight);

            Assert.False(output.HasSpill);
            Assert.Equal((output.SourceWidth, output.SourceHeight), (output.Width, output.Height)); // at 100%, no pixels are resized
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0.5)]
    public void ACropAgainstTheBottomEdge_HasNoLineOfBlankSpace(double zoom)
    {
        using var image = new MagickImage(MagickColors.Red, 1080, 1080);

        // 472.5 and 607.5 pixels: rounded separately they reach 1081, one past the image's end at 1080.
        ImageEditor.Apply(image, new ImageEdit(0, new CropRect(0, 472.5 / 1080, 1, 607.5 / 1080), zoom, CropFill.Black), ".png");

        using var pixels = image.GetPixels();
        Assert.Equal(MagickColors.Red, pixels.GetPixel(0, (int)image.Height - 1)!.ToColor());
    }

    [Fact]
    public void ACropOnePixelLargerThanTheImage_KeepsItsTypedSize_WithRealBlankSpace()
    {
        CropGeometry geometry = SourceInLargeDisplay();

        geometry.SetCropSize(SourceWidth + 1, SourceHeight);

        Assert.Equal((SourceWidth + 1u, (uint)SourceHeight), OutputSizeOf(geometry));
        Assert.True(ImageEditor.ResolveCropOutput(geometry.ToEdit(0, CropFill.Black)!.Crop!.Value, SourceWidth, SourceHeight).HasSpill);
    }

    #endregion

    #region A position stored in the file

    /// <summary>
    /// Quadrants that also say they sit 100, 50 into a larger canvas, as some PNG and TIFF files do.
    /// </summary>
    private string CreateQuadrantsWithCanvasPosition(string fileName)
    {
        string path = Path.Combine(_testRoot, fileName);
        using var source = CreateQuadrantsImage();
        source.Page = new MagickGeometry(100, 50, SourceWidth, SourceHeight);
        source.Write(path);
        using var written = new MagickImage(path);
        Assert.Equal(100, written.Page.X); // guards the test itself
        return path;
    }

    [Theory]
    [InlineData(1)]
    [InlineData(0.5)]
    [InlineData(2)]
    public void CanvasPosition_DoesNotShiftTheCrop(double zoom)
    {
        using var image = new MagickImage(CreateQuadrantsWithCanvasPosition($"position-{zoom}.png"));

        ImageEditor.Apply(image, new ImageEdit(0, BottomRightQuadrant, zoom), ".png");

        Assert.Equal((uint)(200 * zoom), image.Width);
        AssertColorNear(Yellow, image, 0.02, 0.02);
        AssertColorNear(Yellow, image, 0.98, 0.98);
        Assert.Equal(0, image.Page.X);
    }

    [Fact]
    public void CanvasPosition_DoesNotShiftACropWithBlankSpace()
    {
        using var image = new MagickImage(CreateQuadrantsWithCanvasPosition("position-blank.png"));

        ImageEditor.Apply(image, new ImageEdit(0, new CropRect(0.5, 0, 1, 1), Fill: CropFill.Black), ".png");

        Assert.Equal((400u, 200u), (image.Width, image.Height));
        AssertColorNear(Lime, image, 0.02, 0.02); // the image's right half starts the output
        AssertColorNear(Yellow, image, 0.48, 0.98);
        AssertColorNear(MagickColors.Black, image, 0.75, 0.5);
    }

    [Fact]
    public void CanvasPosition_DoesNotShiftARotatedCrop()
    {
        using var image = new MagickImage(CreateQuadrantsWithCanvasPosition("position-rotated.png"));

        ImageEditor.Apply(image, new ImageEdit(1, TopLeftQuadrant), ".png");

        AssertColorNear(Blue, image, 0.02, 0.02);
        AssertColorNear(Blue, image, 0.98, 0.98);
    }

    #endregion

    #region Zoom slider

    [Fact]
    public void EveryStepOfTheZoomSlider_ChangesTheZoom_AndTheEndsAreTheLimits()
    {
        const double ButtonStep = 20.0 / 1000;
        double previous = 0;
        for (double fraction = 0; fraction <= 1; fraction += ButtonStep)
        {
            double zoom = CropGeometry.ZoomAtSliderFraction(fraction);
            Assert.True(zoom > previous, $"stuck at {zoom} at {fraction}");
            previous = zoom;
        }

        Assert.Equal(CropGeometry.MinZoom, CropGeometry.ZoomAtSliderFraction(0), 9);
        Assert.Equal(CropGeometry.MaxZoom, CropGeometry.ZoomAtSliderFraction(1), 9);
    }

    [Fact]
    public void TheZoomSlider_CanLandOnExactly100Percent()
    {
        bool landed = Enumerable.Range(0, 1001).Any(position => CropGeometry.ZoomAtSliderFraction(position / 1000.0) == 1);

        Assert.True(landed);
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(0.18)]
    [InlineData(2.5)]
    [InlineData(4)]
    public void TheZoomSlider_ShowsTheZoom_ItWasGiven(double zoom)
    {
        Assert.Equal(zoom, CropGeometry.ZoomAtSliderFraction(CropGeometry.SliderFractionOfZoom(zoom)), 9);
    }

    #endregion

    #region Background rule and memory estimate

    [Theory]
    [InlineData(".png", 0, 0)] // transparent stays transparent
    [InlineData(".jpg", 0, 255)] // JPEG can't be transparent, so white
    [InlineData(".jpg", 255, 255)]
    public void UsableFill_OnlyReplacesTransparencyTheOutputCantHave(string extension, byte alpha, byte expectedAlpha)
    {
        CropFill fill = alpha == 0 ? CropFill.Transparent : CropFill.Black;

        CropFill usable = ImageEditor.UsableFill(fill, extension);

        Assert.Equal(expectedAlpha, usable.A);
        Assert.Equal(alpha == 0 && expectedAlpha == 255 ? CropFill.White : fill, usable);
    }

    [Fact]
    public void AnEditOfAnImageOfUnknownSize_LeavesTheMemoryEstimate_ToTheFileSize()
    {
        string path = Path.Combine(_testRoot, "unknown-size.png");
        File.WriteAllBytes(path, new byte[4096]);
        var media = new MediaFileInfo(_testRoot, new FileInfo(path));
        Assert.Equal(0u, media.Width); // guards the test itself
        OutputSettings settings = CreateSettings(OutputFormat.Png);
        ulong unedited = BatchMemoryPlanner.EstimateBytes(media, settings);

        media.Edit = new ImageEdit(0, TopLeftQuadrant);

        Assert.Equal(unedited, BatchMemoryPlanner.EstimateBytes(media, settings));
    }

    #endregion

    #region File list after scanning

    [Fact]
    public async Task FilesInGalleryOrder_IsSortedOncePerScan()
    {
        string folder = Path.Combine(_testRoot, "sorted");
        string first = CreateQuadrants("a.png", Path.Combine("sorted", "sub"));
        string second = CreateQuadrants("b.png", "sorted");
        string third = CreateQuadrants("c.png", "sorted");
        var selectedPath = new SelectedPath(folder, includeSubDirectories: true);

        await TestFileHelper.WaitUntil(() => selectedPath.FilesInGalleryOrder != null, TimeSpan.FromSeconds(10));

        Assert.Equal([first, second, third], selectedPath.FilesInGalleryOrder!.Select(file => file.FilePath));
        Assert.Same(selectedPath.FilesInGalleryOrder, selectedPath.FilesInGalleryOrder);
    }

    [Fact]
    public async Task ARescanCancelledByAnother_EndsWithTheLastChoice()
    {
        string folder = Path.Combine(_testRoot, "rescan");
        CreateQuadrants("top.png", "rescan");
        CreateQuadrants("inner.png", Path.Combine("rescan", "sub"));
        var selectedPath = new SelectedPath(folder, includeSubDirectories: true);
        await TestFileHelper.WaitUntil(() => selectedPath.FilesInGalleryOrder?.Count == 2, TimeSpan.FromSeconds(10));

        selectedPath.ChangeToIncludeSubDirectories(false);
        selectedPath.ChangeToIncludeSubDirectories(true);
        selectedPath.ChangeToIncludeSubDirectories(false);

        await TestFileHelper.WaitUntil(() => selectedPath.FilesInGalleryOrder?.Count == 1 && !selectedPath.ScanningForFiles, TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task ACleanupDuringARescan_KeepsEditsOfFilesStillSelected()
    {
        string folder = Path.Combine(_testRoot, "stress");
        string edited = CreateQuadrants("edited.png", "stress");
        for (int i = 0; i < 30; i++)
            CreateQuadrants($"inner{i}.png", Path.Combine("stress", "sub"));
        string other = CreateQuadrants("other.png", "stress-other");
        string otherFolder = Path.GetDirectoryName(other)!;
        var store = new FileStore();
        store.Enqueue(folder, scanSubDirectories: true);
        store.Enqueue(otherFolder, scanSubDirectories: true);
        await WaitForFilesAsync(store, 32);
        ImageEdit edit = new(1, null);
        ImageEditStore.Set(edited, edit);

        for (int i = 0; i < 40; i++)
        {
            // The second rescan cancels the first, whose ending could make the rescans look finished while the list is still empty.
            store.ChangeIncludeSubDirectoriesSetting(folder, false);
            store.ChangeIncludeSubDirectoriesSetting(folder, true);
            store.Remove(otherFolder); // which cleans up edits
            store.Enqueue(otherFolder, scanSubDirectories: true);

            Assert.Equal(edit, ImageEditStore.Get(edited));
        }
    }

    #endregion

    #region Zoom resizes the output

    [Fact]
    public async Task Zoom_ResizesTheWholeImage_WhenTheCropIsTheWholeImage()
    {
        string input = CreateQuadrants("zoom-whole.png");

        string output = await CompressAsync(input, new ImageEdit(0, new CropRect(0, 0, 1, 1), Zoom: 0.5), CreateSettings(OutputFormat.Png));

        using var image = new MagickImage(output);
        Assert.Equal((200u, 100u), (image.Width, image.Height));
        AssertColorNear(Red, image, 0.25, 0.25);
        AssertColorNear(Yellow, image, 0.75, 0.75);
    }

    [Theory]
    [InlineData(0.5, 100u, 50u)]
    [InlineData(2, 400u, 200u)]
    public async Task Zoom_ResizesTheCroppedPart(double zoom, uint expectedWidth, uint expectedHeight)
    {
        string input = CreateQuadrants($"zoom-part-{zoom}.png");

        string output = await CompressAsync(input, new ImageEdit(0, BottomRightQuadrant, zoom), CreateSettings(OutputFormat.Png));

        using var image = new MagickImage(output);
        Assert.Equal((expectedWidth, expectedHeight), (image.Width, image.Height));
        AssertColorNear(Yellow, image, 0.5, 0.5);
    }

    [Fact]
    public async Task Zoom_At100Percent_KeepsThePixelsOfTheCrop()
    {
        string input = CreateQuadrants("zoom-none.png");
        CropRect crop = new(0.25, 0.25, 0.5, 0.5);

        string noZoomGiven = await CompressAsync(input, new ImageEdit(0, crop), CreateSettings(OutputFormat.Png));
        string at100Percent = await CompressAsync(input, new ImageEdit(0, crop, Zoom: 1), CreateSettings(OutputFormat.Png));

        Assert.Equal(File.ReadAllBytes(noZoomGiven), File.ReadAllBytes(at100Percent));
        using var image = new MagickImage(at100Percent);
        Assert.Equal((200u, 100u), (image.Width, image.Height));
        AssertColorNear(Red, image, 0.25, 0.25);
        AssertColorNear(Yellow, image, 0.75, 0.75);
    }

    [Fact]
    public async Task FittingTheImageInASquareCrop_AddsBlankSpaceAroundTheSmallerImage()
    {
        string input = CreateQuadrants("zoom-fit.png");
        CropGeometry geometry = new(SourceWidth, SourceHeight, 800, 400, null);
        geometry.SetCropSize(100, 100);
        geometry.FitImageInCrop();
        ImageEdit edit = geometry.ToEdit(0, CropFill.White)!;

        string output = await CompressAsync(input, edit, CreateSettings(OutputFormat.Png));

        using var image = new MagickImage(output);
        Assert.Equal((100u, 100u), (image.Width, image.Height));
        AssertColorNear(MagickColors.White, image, 0.5, 0.1); // above the image, which is 100 x 50 in the middle
        AssertColorNear(Red, image, 0.25, 0.35);
        AssertColorNear(Yellow, image, 0.75, 0.65);
        AssertColorNear(MagickColors.White, image, 0.5, 0.9);
    }

    [Fact]
    public async Task FillingASquareCrop_CoversIt_WithTheImageCentered()
    {
        string input = CreateQuadrants("zoom-fill.png");
        CropGeometry geometry = new(SourceWidth, SourceHeight, 800, 400, null);
        geometry.SetCropSize(100, 100);
        geometry.FillCropWithImage();

        string output = await CompressAsync(input, geometry.ToEdit(0, CropFill.White)!, CreateSettings(OutputFormat.Png));

        using var image = new MagickImage(output);
        Assert.Equal((100u, 100u), (image.Width, image.Height));
        AssertColorNear(Red, image, 0.25, 0.25);
        AssertColorNear(Yellow, image, 0.75, 0.75);
    }

    [Fact]
    public async Task ZoomedFarOut_WithBlankSpace_DoesNotNeedAnEnormousCanvas()
    {
        string input = CreateQuadrants("zoom-far.png");

        // In image pixels this crop is 40400 x 20200, which is 800 megapixels. Only the output's size should be allocated.
        string output = await CompressAsync(input, new ImageEdit(0, new CropRect(-50, -50, 101, 101), CropGeometry.MinZoom, CropFill.Black), CreateSettings(OutputFormat.Png));

        using var image = new MagickImage(output);
        Assert.Equal((404u, 202u), (image.Width, image.Height));
        AssertColorNear(MagickColors.Black, image, 0.1, 0.1);
    }

    [Fact]
    public async Task ZoomedImage_IsExcludedFromBatchResize()
    {
        string input = CreateQuadrants("zoom-batch.png");

        string output = await CompressAsync(input, new ImageEdit(0, new CropRect(0, 0, 1, 1), Zoom: 0.5), CreateResizeSettings(DimensionReductionStrategy.Percentage));

        using var image = new MagickImage(output);
        Assert.Equal((200u, 100u), (image.Width, image.Height));
    }

    #endregion

    #region Rotation stored in the image's metadata

    private string CreateJpegDisplayedTurnedClockwise(string fileName)
    {
        string path = Path.Combine(_testRoot, fileName);
        using var source = CreateQuadrantsImage();
        // Magick writes the tag from the property, into a profile that must already have it.
        var exif = new ExifProfile();
        exif.SetValue(ExifTag.Orientation, (ushort)Orientation.RightTop);
        source.SetProfile(exif);
        source.Orientation = Orientation.RightTop;
        source.Write(path, MagickFormat.Jpeg);
        Assert.Equal(Orientation.RightTop, new MagickImageInfo(path).Orientation);
        return path;
    }

    [Fact]
    public async Task UserRotation_AddsToTheRotationInMetadata()
    {
        string input = CreateJpegDisplayedTurnedClockwise("exif-turned.jpg");

        // Displayed as 200 x 400 with blue in the top left. One more clockwise turn gives 400 x 200 with yellow there.
        string output = await CompressAsync(input, new ImageEdit(1, null), CreateSettings(OutputFormat.Png));

        using var image = new MagickImage(output);
        Assert.Equal((400u, 200u), (image.Width, image.Height));
        AssertColorNear(Yellow, image, 0.25, 0.25);
    }

    [Fact]
    public async Task BatchResize_UsesTheSizeAfterBothRotations()
    {
        string input = CreateJpegDisplayedTurnedClockwise("exif-turned-resize.jpg");
        OutputSettings settings = CreateSettings(OutputFormat.Png);
        settings.dimensionStrategy = DimensionReductionStrategy.FixedWidth;
        settings.primaryEdgeLength = 100;

        // Both rotations together give 400 x 200, so a width of 100 gives a height of 50. Ignoring the one in metadata would give 100 x 200.
        string output = await CompressAsync(input, new ImageEdit(1, null), settings);

        using var image = new MagickImage(output);
        Assert.Equal((100u, 50u), (image.Width, image.Height));
        AssertColorNear(Yellow, image, 0.25, 0.25);
    }

    [Theory]
    [InlineData(OutputFormat.Jpg)]
    [InlineData(OutputFormat.Png)]
    public async Task RotatedOutput_HasNoRotationInItsMetadata_SoViewersDontTurnItAgain(OutputFormat format)
    {
        string input = CreateJpegDisplayedTurnedClockwise($"exif-turned-copy-{format}.jpg");
        OutputSettings settings = CreateSettings(format);
        settings.copyMetadata = true;

        string output = await CompressAsync(input, new ImageEdit(1, null), settings);

        Assert.Contains(new MagickImageInfo(output).Orientation, new[] { Orientation.Undefined, Orientation.TopLeft });
        using var image = new MagickImage(output);
        Assert.Equal((400u, 200u), (image.Width, image.Height));
    }

    #endregion

    #region Blank space

    [Fact]
    public async Task BlankSpace_CanBeTransparent()
    {
        string input = CreateQuadrants("spill-transparent.png");

        string output = await CompressAsync(input, new ImageEdit(0, new CropRect(-0.25, 0, 1.5, 1)), CreateSettings(OutputFormat.Png));

        using var image = new MagickImage(output);
        Assert.Equal((600u, 200u), (image.Width, image.Height));
        Assert.Equal(0, AlphaAt(image, 50, 100));
        Assert.Equal(0, AlphaAt(image, 550, 100));
        Assert.Equal(255, AlphaAt(image, 150, 50));
        AssertColorNear(Red, image, 0.25, 0.25); // the image starts 100 pixels in
        AssertColorNear(Yellow, image, 0.75, 0.75);
    }

    [Theory]
    [InlineData(OutputFormat.Png, 0, 0, 0)]
    [InlineData(OutputFormat.Png, 10, 200, 30)]
    [InlineData(OutputFormat.Webp, 10, 200, 30)]
    [InlineData(OutputFormat.Jpg, 255, 255, 255)]
    public async Task BlankSpace_UsesTheChosenColor(OutputFormat format, byte r, byte g, byte b)
    {
        string input = CreateQuadrants($"spill-{format}-{r}.png");
        CropFill fill = new(r, g, b, 255);

        string output = await CompressAsync(input, new ImageEdit(0, new CropRect(0, 0, 1.5, 1), Fill: fill), CreateSettings(format));

        using var image = new MagickImage(output);
        Assert.Equal((600u, 200u), (image.Width, image.Height));
        AssertColorNear(new MagickColor(r, g, b), image, 500.0 / 600, 0.5);
        AssertColorNear(Red, image, 100.0 / 600, 0.25);
    }

    [Fact]
    public async Task BlankSpace_IsWhite_WhenTheFormatCantBeTransparent()
    {
        string input = CreateQuadrants("spill-jpeg.png");

        string output = await CompressAsync(input, new ImageEdit(0, new CropRect(0, 0, 1.5, 1), Fill: CropFill.Transparent), CreateSettings(OutputFormat.Jpg));

        using var image = new MagickImage(output);
        AssertColorNear(MagickColors.White, image, 500.0 / 600, 0.5);
    }

    [Fact]
    public async Task ACropWhollyBesideTheImage_IsOnlyBlankSpace()
    {
        string input = CreateQuadrants("spill-beside.png");

        string output = await CompressAsync(input, new ImageEdit(0, new CropRect(1.5, 0, 0.5, 1), Fill: CropFill.Black), CreateSettings(OutputFormat.Png));

        using var image = new MagickImage(output);
        Assert.Equal((200u, 200u), (image.Width, image.Height));
        AssertColorNear(MagickColors.Black, image, 0.5, 0.5);
    }

    [Fact]
    public async Task BlankSpace_AppliesAfterRotation_AndHasNoCanvasOffset()
    {
        string input = CreateQuadrants("spill-rotated.png");

        // After a clockwise quarter turn the image is 200 x 400 with blue in the top left.
        string output = await CompressAsync(input, new ImageEdit(1, new CropRect(-0.5, 0, 1.5, 1), Fill: CropFill.White), CreateSettings(OutputFormat.Gif));

        using var image = new MagickImage(output);
        Assert.Equal((300u, 400u), (image.Width, image.Height));
        AssertColorNear(MagickColors.White, image, 50.0 / 300, 0.5);
        AssertColorNear(Blue, image, 150.0 / 300, 0.25);
        Assert.Equal(0, image.Page.X);
    }

    [Fact]
    public async Task ImageWithBlankSpace_IsExcludedFromBatchResize()
    {
        string input = CreateQuadrants("spill-resize.png");

        string output = await CompressAsync(input, new ImageEdit(0, new CropRect(0, 0, 1.5, 1)), CreateResizeSettings(DimensionReductionStrategy.Percentage));

        using var image = new MagickImage(output);
        Assert.Equal((600u, 200u), (image.Width, image.Height));
    }

    [Fact]
    public async Task EnlargingEdit_RaisesTheMemoryEstimate()
    {
        MediaFileInfo media = await LoadAsync(CreateQuadrants("spill-memory.png"));
        OutputSettings settings = CreateSettings(OutputFormat.Png);
        ulong unedited = BatchMemoryPlanner.EstimateBytes(media, settings);

        media.Edit = new ImageEdit(0, new CropRect(-0.5, -0.5, 2, 2));
        ulong enlarged = BatchMemoryPlanner.EstimateBytes(media, settings);

        media.Edit = new ImageEdit(1, null);
        ulong rotated = BatchMemoryPlanner.EstimateBytes(media, settings);

        Assert.True(enlarged > unedited);
        Assert.Equal(unedited, rotated);
    }

    [Fact]
    public async Task ReplaceOriginal_DoesNotAddBlankSpaceAgainOnTheNextRun()
    {
        string input = CreateQuadrants("spill-replace.png");
        MediaFileInfo media = await LoadAsync(input);
        media.Edit = new ImageEdit(0, new CropRect(0, 0, 1.5, 1), Fill: CropFill.White);
        OutputSettings settings = CreateSettings(OutputFormat.KeepSame);
        settings.OutputLocationSettings = OutputLocationSetting.ReplaceOriginal;

        await new ImageCompressor().ProcessSingleFileAsync(media, multipleSelectPaths: false, settings, forPreview: false);
        await new ImageCompressor().ProcessSingleFileAsync(media, multipleSelectPaths: false, settings, forPreview: false);

        using var source = new MagickImage(input);
        Assert.Equal((600u, 200u), (source.Width, source.Height));
    }

    private static int AlphaAt(MagickImage image, int x, int y)
    {
        using var pixels = image.GetPixels();
        return pixels.GetPixel(x, y)!.ToColor()!.A;
    }

    #endregion

    #region Falling back to the original

    [Fact]
    public async Task EditedImage_WithLargerOutput_IsNotReplacedByTheOriginal()
    {
        string input = Path.Combine(_testRoot, "low-quality.jpg");
        using (var source = CreateQuadrantsImage())
        {
            source.Quality = 10;
            source.Write(input, MagickFormat.Jpeg);
        }
        OutputSettings settings = CreateSettings(OutputFormat.Jpg);
        settings.quality = 100;

        // A half turn keeps the size and format, which is what the original is otherwise kept for.
        string output = await CompressAsync(input, new ImageEdit(2, null), settings);

        Assert.True(new FileInfo(output).Length > new FileInfo(input).Length, "test needs output larger than input");
        using var image = new MagickImage(output);
        AssertColorNear(Yellow, image, 0.25, 0.25);
    }

    [Fact]
    public async Task UneditedImage_WithLargerOutput_IsStillReplacedByTheOriginal()
    {
        string input = Path.Combine(_testRoot, "low-quality-plain.jpg");
        using (var source = CreateQuadrantsImage())
        {
            source.Quality = 10;
            source.Write(input, MagickFormat.Jpeg);
        }
        OutputSettings settings = CreateSettings(OutputFormat.Jpg);
        settings.quality = 100;

        string output = await CompressAsync(input, null, settings);

        Assert.Equal(File.ReadAllBytes(input), File.ReadAllBytes(output));
    }

    [Fact]
    public async Task EditedImage_WithUnreadableOutput_FailsInsteadOfCopyingTheOriginal()
    {
        MediaFileInfo media = await LoadAsync(CreateQuadrants("unreadable.png"));
        media.Edit = new ImageEdit(0, TopLeftQuadrant);
        byte[] garbage = [1, 2, 3, 4, 5];
        string output = Path.Combine(_testRoot, "unreadable-output.png");
        File.WriteAllBytes(output, garbage);

        var (wasOriginalFileUsed, failedToFreezeOutput) = await media.FreezeOutputAsync(output, CreateSettings(OutputFormat.KeepSame));

        Assert.False(wasOriginalFileUsed);
        Assert.True(failedToFreezeOutput);
        Assert.Equal(garbage, File.ReadAllBytes(output));
    }

    [Fact]
    public async Task UneditedImage_WithUnreadableOutput_StillCopiesTheOriginal()
    {
        string input = CreateQuadrants("unreadable-plain.png");
        MediaFileInfo media = await LoadAsync(input);
        string output = Path.Combine(_testRoot, "unreadable-plain-output.png");
        File.WriteAllBytes(output, [1, 2, 3, 4, 5]);

        var (wasOriginalFileUsed, failedToFreezeOutput) = await media.FreezeOutputAsync(output, CreateSettings(OutputFormat.KeepSame));

        Assert.True(wasOriginalFileUsed);
        Assert.False(failedToFreezeOutput);
        Assert.Equal(File.ReadAllBytes(input), File.ReadAllBytes(output));
    }

    #endregion

    #region Replace original

    [Fact]
    public async Task ReplaceOriginal_EditsTheSource_AndSecondRunDoesNotEditAgain()
    {
        string input = CreateQuadrants("replace.png");
        MediaFileInfo media = await LoadAsync(input);
        media.Edit = new ImageEdit(1, TopLeftQuadrant);
        OutputSettings settings = CreateSettings(OutputFormat.KeepSame);
        settings.OutputLocationSettings = OutputLocationSetting.ReplaceOriginal;

        await new ImageCompressor().ProcessSingleFileAsync(media, multipleSelectPaths: false, settings, forPreview: false);

        AssertSourceIsTheEditedImage();
        Assert.Null(media.Edit);
        Assert.Equal(SourceHeight / 2u, media.Width);
        Assert.Equal(SourceWidth / 2u, media.Height);

        await new ImageCompressor().ProcessSingleFileAsync(media, multipleSelectPaths: false, settings, forPreview: false);

        AssertSourceIsTheEditedImage();

        void AssertSourceIsTheEditedImage()
        {
            using var source = new MagickImage(input);
            Assert.Equal(SourceHeight / 2u, source.Width);
            Assert.Equal(SourceWidth / 2u, source.Height);
            AssertColorNear(Blue, source, 0.5, 0.5);
        }
    }

    #endregion

    #region Metadata

    [Theory]
    [InlineData(DimensionReductionStrategy.FitInFrame)]
    [InlineData(DimensionReductionStrategy.FixedWidth)]
    public async Task PrintDpi_IsOnlySetOnImagesThatAreResized(DimensionReductionStrategy strategy)
    {
        OutputSettings CreateMetadataSettings()
        {
            OutputSettings settings = CreateSettings(OutputFormat.Jpg);
            settings.copyMetadata = true;
            settings.dimensionStrategy = strategy;
            return settings;
        }

        string resized = await CompressAsync(CreateQuadrants("dpi-resized.png"), null, CreateMetadataSettings());
        string cropped = await CompressAsync(CreateQuadrants("dpi-cropped.png"), new ImageEdit(0, TopLeftQuadrant), CreateMetadataSettings());

        Assert.Equal(300, new MagickImage(resized).Density.X);
        Assert.Equal(72, new MagickImage(cropped).Density.X);
    }

    #endregion

    #region Removing edits with their files

    [Fact]
    public async Task TurningSubFoldersOff_KeepsEditsOfSelectedFiles_AndRemovesOthers()
    {
        string folder = Path.Combine(_testRoot, "scan");
        string top = CreateQuadrants("top.png", "scan");
        string inner = CreateQuadrants("inner.png", Path.Combine("scan", "sub"));
        var store = new FileStore();
        store.Enqueue(folder, scanSubDirectories: true);
        await WaitForFilesAsync(store, 2);
        ImageEdit edit = new(1, null);
        ImageEditStore.Set(top, edit);
        ImageEditStore.Set(inner, edit);

        store.ChangeIncludeSubDirectoriesSetting(folder, false);
        await TestFileHelper.WaitUntil(() => ImageEditStore.Get(inner) == null, TimeSpan.FromSeconds(10));

        Assert.Equal(edit, ImageEditStore.Get(top));
    }

    [Fact]
    public async Task RemovingFolder_KeepsEditOfFileAlsoAddedSeparately_AndRemovesOthers()
    {
        string folder = Path.Combine(_testRoot, "remove");
        string alsoAdded = CreateQuadrants("added.png", "remove");
        string onlyInFolder = CreateQuadrants("folder-only.png", "remove");
        var store = new FileStore();
        store.Enqueue(folder, scanSubDirectories: true);
        store.Enqueue(alsoAdded, scanSubDirectories: true);
        await WaitForFilesAsync(store, 2);
        await TestFileHelper.WaitUntil(() => store.GetAllFilesIncludingDuplicates.Count() == 3, TimeSpan.FromSeconds(10));
        ImageEdit edit = new(1, null);
        ImageEditStore.Set(alsoAdded, edit);
        ImageEditStore.Set(onlyInFolder, edit);

        Assert.True(store.Remove(folder));

        Assert.Equal(edit, ImageEditStore.Get(alsoAdded));
        Assert.Null(ImageEditStore.Get(onlyInFolder));
    }

    [Fact]
    public async Task RemovingAll_RemovesAllEdits()
    {
        string input = CreateQuadrants("all.png", "remove-all");
        var store = new FileStore();
        store.Enqueue(Path.GetDirectoryName(input)!, scanSubDirectories: true);
        await WaitForFilesAsync(store, 1);
        ImageEditStore.Set(input, new ImageEdit(1, null));

        store.RemoveAll();

        Assert.True(ImageEditStore.IsEmpty);
    }

    [Fact]
    public async Task RemovingAndAddingBack_StartsWithNoEdit()
    {
        string input = CreateQuadrants("back.png", "add-back");
        string folder = Path.GetDirectoryName(input)!;
        var store = new FileStore();
        store.Enqueue(folder, scanSubDirectories: true);
        await WaitForFilesAsync(store, 1);
        store.GetAllFiles.Single().Edit = new ImageEdit(1, TopLeftQuadrant);

        store.Remove(folder);
        store.Enqueue(folder, scanSubDirectories: true);
        await WaitForFilesAsync(store, 1);

        Assert.Null(store.GetAllFiles.Single().Edit);
        Assert.True(ImageEditStore.IsEmpty);
    }

    [Fact]
    public async Task UncheckedOrFilteredOutImage_KeepsItsEdit_ThroughACleanup()
    {
        string unchecked_ = CreateQuadrants("unchecked.png", "keep");
        string filteredOut = CreateQuadrants("filtered.png", "keep");
        string removedWithFolder = CreateQuadrants("other.png", "keep-other");
        var store = new FileStore();
        store.Enqueue(Path.GetDirectoryName(unchecked_)!, scanSubDirectories: true);
        store.Enqueue(Path.GetDirectoryName(removedWithFolder)!, scanSubDirectories: true);
        await WaitForFilesAsync(store, 3);
        ImageEdit edit = new(1, null);
        foreach (MediaFileInfo file in store.GetAllFiles)
            file.Edit = edit;
        store.GetAllFiles.Single(file => file.FilePath == unchecked_).ExcludeAndShow = true;
        store.GetAllFiles.Single(file => file.FilePath == filteredOut).ExcludeAndHide = true;

        // Removing the other folder cleans up, which must only touch files that left the selection.
        store.Remove(Path.GetDirectoryName(removedWithFolder)!);

        Assert.Equal(edit, ImageEditStore.Get(unchecked_));
        Assert.Equal(edit, ImageEditStore.Get(filteredOut));
        Assert.Null(ImageEditStore.Get(removedWithFolder));
    }

    [Fact]
    public async Task FilesForPreview_AreInGalleryOrder_FoldersFirstThenByName()
    {
        string folder = Path.Combine(_testRoot, "order");
        string[] expected =
        [
            CreateQuadrants("b.png", Path.Combine("order", "sub1", "deep")),
            CreateQuadrants("a.png", Path.Combine("order", "sub1")),
            CreateQuadrants("y.png", Path.Combine("order", "sub2")),
            CreateQuadrants("a.png", "order"),
            CreateQuadrants("m.png", "order"),
            CreateQuadrants("z.png", "order"),
        ];
        var store = new FileStore();
        store.Enqueue(folder, scanSubDirectories: true);
        await WaitForFilesAsync(store, expected.Length);

        Assert.Equal(expected, store.GetAllFilesIncludingDuplicates.Select(file => file.FilePath));
    }

    [Fact]
    public async Task FilesForPreview_KeepTheOrderOfTheSelectedPaths()
    {
        string second = CreateQuadrants("a.png", "paths-z");
        string first = CreateQuadrants("z.png", "paths-a");
        var store = new FileStore();
        store.Enqueue(Path.GetDirectoryName(second)!, scanSubDirectories: true);
        await WaitForFilesAsync(store, 1);
        store.Enqueue(Path.GetDirectoryName(first)!, scanSubDirectories: true);
        await WaitForFilesAsync(store, 2);

        Assert.Equal([second, first], store.GetAllFilesIncludingDuplicates.Select(file => file.FilePath));
    }

    [Theory]
    [InlineData("short.png", "short.png")]
    [InlineData("exactly_forty_characters_long_name01.png", "exactly_forty_characters_long_name01.png")]
    [InlineData("holiday_photo_export_from_my_phone_taken_in_2026_final_edit.png", "holiday_photo_export_from_…inal_edit.png")]
    public void MiddleEllipsis_KeepsTheStartAndTheEnd(string name, string expected)
    {
        Assert.Equal(expected, HumanReadable.MiddleEllipsis(name));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(11)]
    public void MiddleEllipsis_NeverSplitsATwoPartCharacter(int maxLength)
    {
        string name = string.Concat(Enumerable.Repeat("😀", 20)) + ".png";

        string shortened = HumanReadable.MiddleEllipsis(name, maxLength);

        Assert.True(shortened.Length <= maxLength);
        Assert.Contains("…", shortened);
        // Encoding only round-trips if no half of a pair was left behind.
        Assert.Equal(shortened, System.Text.Encoding.UTF8.GetString(System.Text.Encoding.UTF8.GetBytes(shortened)));
    }

    private static Task WaitForFilesAsync(FileStore store, int count)
        => TestFileHelper.WaitUntil(() => store.GetAllFiles.Count() == count, TimeSpan.FromSeconds(10));

    #endregion

    #region Which files can be edited

    [Theory]
    [InlineData(".jpg")]
    [InlineData(".png")]
    [InlineData(".webp")]
    [InlineData(".tiff")]
    [InlineData(".avif")]
    [InlineData(".jxl")]
    [InlineData(".bmp")]
    public void CanEdit_StillImages(string extension)
    {
        Assert.True(ImageEditor.CanEdit(CreateQuadrants("still" + extension)));
    }

    [Theory]
    [InlineData(MagickFormat.APng, ".png")]
    [InlineData(MagickFormat.WebP, ".webp")]
    [InlineData(MagickFormat.Tiff, ".tiff")]
    [InlineData(MagickFormat.Jxl, ".jxl")]
    public void CanEdit_NotForAnimatedOrMultiPageFiles(MagickFormat format, string extension)
    {
        string path = Path.Combine(_testRoot, "frames" + extension);
        using var frames = new MagickImageCollection();
        foreach (MagickColor color in new[] { Red, Blue, Yellow })
        {
            var frame = new MagickImage(color, 32, 32) { AnimationDelay = 10 };
            frames.Add(frame);
        }
        frames.Write(path, format);

        Assert.False(ImageEditor.CanEdit(path));
    }

    [Fact]
    public void CanEdit_NotForGif_EvenIfStill()
    {
        Assert.False(ImageEditor.CanEdit(CreateQuadrants("still.gif")));
    }

    [Fact]
    public void CanEdit_NotForSvg()
    {
        string path = Path.Combine(_testRoot, "vector.svg");
        File.WriteAllText(path, "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"10\" height=\"10\"/>");

        Assert.False(ImageEditor.CanEdit(path));
    }

    [Fact]
    public void CanEdit_NotForUnreadableFile()
    {
        string path = Path.Combine(_testRoot, "broken.webp");
        File.WriteAllBytes(path, [1, 2, 3, 4, 5]);

        Assert.False(ImageEditor.CanEdit(path));
    }

    #endregion

    private OutputSettings CreateSettings(OutputFormat format) => new()
    {
        format = format,
        quality = 90,
        copyMetadata = false,
        retainDateTime = false,
        OutputLocationSettings = OutputLocationSetting.UserSpecificFolder,
        OutputFolder = Path.Combine(_testRoot, "output-" + Guid.NewGuid().ToString("N")),
    };

    /// <summary>
    /// Settings that would make the source a quarter of its width and height.
    /// </summary>
    private OutputSettings CreateResizeSettings(DimensionReductionStrategy strategy)
    {
        OutputSettings settings = CreateSettings(OutputFormat.Png);
        settings.dimensionStrategy = strategy;
        settings.percentageOfLongEdge = 25;
        settings.primaryEdgeLength = SourceWidth / 4;
        return settings;
    }

    private static async Task<MediaFileInfo> LoadAsync(string input)
    {
        var media = new MediaFileInfo(Path.GetDirectoryName(input)!, new FileInfo(input));
        await TestFileHelper.WaitUntil(() => media.IsMetadataLoaded, TimeSpan.FromSeconds(5));
        return media;
    }

    private static async Task<string> CompressAsync(string input, ImageEdit? edit, OutputSettings settings)
    {
        MediaFileInfo media = await LoadAsync(input);
        media.Edit = edit;

        string output = await new ImageCompressor().ProcessSingleFileAsync(media, multipleSelectPaths: false, settings, forPreview: false);

        Assert.True(File.Exists(output));
        return output;
    }

    /// <summary>
    /// Red, lime on top. Blue, yellow below.
    /// </summary>
    private static MagickImage CreateQuadrantsImage()
    {
        var image = new MagickImage(Red, SourceWidth, SourceHeight);
        Fill(image, Lime, SourceWidth / 2, 0);
        Fill(image, Blue, 0, SourceHeight / 2);
        Fill(image, Yellow, SourceWidth / 2, SourceHeight / 2);
        return image;
    }

    private static void Fill(MagickImage image, MagickColor color, int x, int y)
    {
        using var quadrant = new MagickImage(color, SourceWidth / 2, SourceHeight / 2);
        image.Composite(quadrant, x, y, CompositeOperator.Over);
    }

    private string CreateQuadrants(string fileName, string? subFolder = null)
    {
        string folder = subFolder == null ? _testRoot : Path.Combine(_testRoot, subFolder);
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, fileName);
        using var image = CreateQuadrantsImage();
        image.Write(path);
        return path;
    }

    private static void AssertColorNear(MagickColor expected, MagickImage image, double xFraction, double yFraction)
    {
        const int Tolerance = 40; // lossy formats shift solid colors a little
        using var pixels = image.GetPixels();
        IMagickColor<byte> actual = pixels.GetPixel((int)(image.Width * xFraction), (int)(image.Height * yFraction))!.ToColor()!;
        Assert.InRange((int)actual.R, expected.R - Tolerance, expected.R + Tolerance);
        Assert.InRange((int)actual.G, expected.G - Tolerance, expected.G + Tolerance);
        Assert.InRange((int)actual.B, expected.B - Tolerance, expected.B + Tolerance);
    }

    public void Dispose()
    {
        ImageEditStore.RetainOnly([]);
        try
        {
            Directory.Delete(_testRoot, recursive: true);
        }
        catch (IOException) { }
    }
}
