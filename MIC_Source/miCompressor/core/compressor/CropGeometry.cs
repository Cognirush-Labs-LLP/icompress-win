using System;

namespace miCompressor.core;

/// <summary>
/// A rectangle in output pixels.
/// </summary>
public readonly record struct PixelRect(double X, double Y, double Width, double Height)
{
    public double Right => X + Width;
    public double Bottom => Y + Height;
    public double CenterX => X + Width / 2;
    public double CenterY => Y + Height / 2;
}

public enum CropCorner { TopLeft, TopRight, BottomRight, BottomLeft }

/// <summary>
/// The arithmetic of the Edit screen. The image and the crop box lie on one plane measured in output pixels, with its origin in the middle of the display area.
/// Three values are kept apart, so that changing one doesn't disturb the others:
/// <list type="bullet">
/// <item><see cref="Crop"/>: its size is the output size. Only typing, dragging the box and picking a ratio change it.</item>
/// <item><see cref="Zoom"/>: the image's size on the plane, 1 leaving its pixels unchanged. This is how an edited image is resized.</item>
/// <item><see cref="PixelsPerDip"/>: how the plane is drawn on screen. It never affects the output.</item>
/// </list>
/// </summary>
public sealed class CropGeometry
{
    /// <summary>
    /// Smallest side of a crop, unless the largest allowed side is smaller.
    /// </summary>
    public const double MinSidePixels = 16;

    /// <summary>
    /// Largest side of a crop, relative to the same side of the image.
    /// </summary>
    public const double MaxSideFactor = 1.5;

    public const double MinZoom = 0.01;
    public const double MaxZoom = 4;

    /// <summary>
    /// How much of the display area the crop box aims to fill after a size is typed, leaving some of the image visible around it.
    /// </summary>
    private const double TypedCropShareOfDisplay = 0.8;

    /// <summary>
    /// A dragged edge this close to the image's edge lands on it, so that a near miss doesn't add a thin strip of blank space.
    /// </summary>
    private const double SnapDips = 4;

    /// <summary>
    /// A rectangle with each edge this close to the image's edge covers the image, as crops work in whole pixels.
    /// </summary>
    private const double WholeImageTolerancePixels = 0.5;

    private const double ExactlyActualSizeTolerance = 0.005;

    private PixelRect _dragStartCrop;
    private double _dragStartImageCenterX;
    private double _dragStartImageCenterY;

    /// <summary>
    /// Starts with the image fitted to the display area, and the crop centered on the image.
    /// </summary>
    /// <param name="imageWidth">Width after rotation.</param>
    /// <param name="imageHeight">Height after rotation.</param>
    /// <param name="stageWidth">Width of the display area in screen units.</param>
    /// <param name="stageHeight">Height of the display area in screen units.</param>
    /// <param name="ratio">Width divided by height to keep, null if free.</param>
    public CropGeometry(double imageWidth, double imageHeight, double stageWidth, double stageHeight, double? ratio)
    {
        ImageWidth = imageWidth;
        ImageHeight = imageHeight;
        StageWidth = stageWidth;
        StageHeight = stageHeight;
        Ratio = ratio;
        StartOver();
    }

    /// <summary>
    /// Width after rotation, before zoom.
    /// </summary>
    public double ImageWidth { get; }

    /// <summary>
    /// Height after rotation, before zoom.
    /// </summary>
    public double ImageHeight { get; }

    public double StageWidth { get; private set; }
    public double StageHeight { get; private set; }

    /// <summary>
    /// Width divided by height to keep, null if free.
    /// </summary>
    public double? Ratio { get; private set; }

    /// <summary>
    /// The screen scale: output pixels per screen unit.
    /// </summary>
    public double PixelsPerDip { get; private set; }

    /// <summary>
    /// Output pixels per image pixel.
    /// </summary>
    public double Zoom { get; private set; }

    public double ImageCenterX { get; private set; }
    public double ImageCenterY { get; private set; }
    public PixelRect Crop { get; private set; }

    public PixelRect ImageRect
    {
        get
        {
            double width = ImageWidth * Zoom;
            double height = ImageHeight * Zoom;
            return new PixelRect(ImageCenterX - width / 2, ImageCenterY - height / 2, width, height);
        }
    }

    /// <summary>
    /// The part of the plane that the display area shows.
    /// </summary>
    public PixelRect StageArea
    {
        get
        {
            double width = StageWidth * PixelsPerDip;
            double height = StageHeight * PixelsPerDip;
            return new PixelRect(-width / 2, -height / 2, width, height);
        }
    }

    /// <summary>
    /// True if part of the image is outside the display area, where only pushing the crop box against the display's edge reaches it.
    /// </summary>
    public bool ImageRunsOffStage
    {
        get
        {
            PixelRect image = ImageRect;
            PixelRect stage = StageArea;
            double tolerance = PixelsPerDip / 2;
            return image.X < stage.X - tolerance || image.Y < stage.Y - tolerance || image.Right > stage.Right + tolerance || image.Bottom > stage.Bottom + tolerance;
        }
    }

    public double MaxCropWidth => Math.Floor(ImageWidth * MaxSideFactor);
    public double MaxCropHeight => Math.Floor(ImageHeight * MaxSideFactor);
    public double MinCropWidth => Math.Min(MinSidePixels, MaxCropWidth);
    public double MinCropHeight => Math.Min(MinSidePixels, MaxCropHeight);

    /// <summary>
    /// The largest crop allowed at the current ratio.
    /// </summary>
    public (double width, double height) LargestAllowedSize
    {
        get
        {
            if (Ratio is not double locked)
                return (MaxCropWidth, MaxCropHeight);

            double width = Math.Min(MaxCropWidth, MaxCropHeight * locked);
            return (Math.Floor(width), Math.Floor(width / locked));
        }
    }

    public bool IsTooLarge(double width, double height) => width > MaxCropWidth || height > MaxCropHeight;

    public bool IsTooSmall(double width, double height) => width < MinCropWidth || height < MinCropHeight;

    private double FittedPixelsPerDip => Math.Max(1, Math.Max(ImageWidth / StageWidth, ImageHeight / StageHeight));

    /// <summary>
    /// The geometry of a stored crop, shown as a typed size would be.
    /// </summary>
    public static CropGeometry Restore(CropRect crop, double zoom, double? ratio, double imageWidth, double imageHeight, double stageWidth, double stageHeight)
    {
        CropGeometry geometry = new(imageWidth, imageHeight, stageWidth, stageHeight, ratio) { Zoom = zoom };
        PixelRect image = geometry.ImageRect;
        geometry.Crop = new PixelRect(image.X + crop.X * image.Width, image.Y + crop.Y * image.Height, crop.Width * image.Width, crop.Height * image.Height);
        geometry.ShowCropLarge();
        return geometry;
    }

    /// <summary>
    /// The zoom at a place along a slider, spaced by ratio so that going from 10% to 20% takes as much of it as going from 100% to 200%. Close to 100% it's exactly that, so that the image's pixels can be kept by hand.
    /// </summary>
    /// <param name="fraction">0 at the slider's start, 1 at its end.</param>
    public static double ZoomAtSliderFraction(double fraction)
    {
        double zoom = MinZoom * Math.Pow(MaxZoom / MinZoom, fraction);
        return Math.Abs(zoom - 1) < ExactlyActualSizeTolerance ? 1 : zoom;
    }

    /// <summary>
    /// The place along a slider that shows this zoom.
    /// </summary>
    public static double SliderFractionOfZoom(double zoom) => Math.Log(zoom / MinZoom) / Math.Log(MaxZoom / MinZoom);

    public double ToStageX(double x) => StageWidth / 2 + x / PixelsPerDip;

    public double ToStageY(double y) => StageHeight / 2 + y / PixelsPerDip;

    /// <summary>
    /// Follows a change of the display area's size. Only a crop that no longer fits changes how things are drawn, and never the output.
    /// </summary>
    public void SetStage(double stageWidth, double stageHeight)
    {
        StageWidth = stageWidth;
        StageHeight = stageHeight;

        PixelsPerDip = Math.Max(PixelsPerDip, Math.Max(Crop.Width / StageWidth, Crop.Height / StageHeight));

        PixelRect stage = StageArea;
        double dx = Math.Clamp(Crop.X, stage.X, Math.Max(stage.X, stage.Right - Crop.Width)) - Crop.X;
        double dy = Math.Clamp(Crop.Y, stage.Y, Math.Max(stage.Y, stage.Bottom - Crop.Height)) - Crop.Y;
        ShiftEverything(dx, dy);
    }

    /// <summary>
    /// Picking a ratio starts over. Free only unlocks the crop, which stays as it is.
    /// </summary>
    /// <param name="ratio">Width divided by height to keep, null if free.</param>
    public void SetRatio(double? ratio)
    {
        Ratio = ratio;
        if (ratio != null)
            StartOver();
    }

    /// <summary>
    /// Sets the zoom keeping the crop still, with whatever is in the crop's center staying there.
    /// </summary>
    public void SetZoom(double zoom)
    {
        zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
        double factor = zoom / Zoom;
        ImageCenterX = Crop.CenterX + (ImageCenterX - Crop.CenterX) * factor;
        ImageCenterY = Crop.CenterY + (ImageCenterY - Crop.CenterY) * factor;
        Zoom = zoom;
    }

    /// <summary>
    /// Sizes the whole image to fit inside the crop, centered, which may leave blank space.
    /// </summary>
    public void FitImageInCrop() => SetZoomCenteredOnCrop(Math.Min(Crop.Width / ImageWidth, Crop.Height / ImageHeight));

    /// <summary>
    /// Sizes the image to cover the crop, centered, leaving no blank space.
    /// </summary>
    public void FillCropWithImage() => SetZoomCenteredOnCrop(Math.Max(Crop.Width / ImageWidth, Crop.Height / ImageHeight));

    /// <summary>
    /// Applies a typed size, which must be allowed. A crop that fits the image moves to stay inside it, so that it only goes past the image if it's larger than the image on that side.
    /// </summary>
    public void SetCropSize(double width, double height)
    {
        PixelRect image = ImageRect;
        Crop = new PixelRect(
            KeepOnImage(Crop.CenterX - width / 2, width, image.X, image.Right),
            KeepOnImage(Crop.CenterY - height / 2, height, image.Y, image.Bottom),
            width,
            height);
        ShowCropLarge();
    }

    /// <summary>
    /// Notes where things are for the following <see cref="DragCrop"/> or <see cref="ResizeCrop"/> calls, which measure from here so that a stopped drag doesn't drift.
    /// </summary>
    public void BeginDrag()
    {
        _dragStartCrop = Crop;
        _dragStartImageCenterX = ImageCenterX;
        _dragStartImageCenterY = ImageCenterY;
    }

    /// <summary>
    /// Moves the crop within the display area. Pushed further against the display's edge, it stays there and the image moves the other way, until the image's own edge reaches the display's edge. So pushing never adds blank space.
    /// </summary>
    /// <param name="dx">Distance from where the drag started, in output pixels.</param>
    /// <param name="dy">Distance from where the drag started, in output pixels.</param>
    /// <param name="snap">True to land on an image edge that's close.</param>
    public void DragCrop(double dx, double dy, bool snap = true)
    {
        PixelRect stage = StageArea;
        double snapDistance = snap ? SnapDips * PixelsPerDip : 0;

        (double x, ImageCenterX) = DragAlongAxis(_dragStartCrop.X, _dragStartCrop.Width, dx, stage.X, stage.Right, _dragStartImageCenterX, ImageWidth * Zoom, snapDistance);
        (double y, ImageCenterY) = DragAlongAxis(_dragStartCrop.Y, _dragStartCrop.Height, dy, stage.Y, stage.Bottom, _dragStartImageCenterY, ImageHeight * Zoom, snapDistance);
        Crop = new PixelRect(x, y, _dragStartCrop.Width, _dragStartCrop.Height);
    }

    /// <summary>
    /// Moves one corner while the opposite corner stays where it is. Stops at the display's edge and at the size limits.
    /// </summary>
    /// <param name="dx">Distance from where the drag started, in output pixels.</param>
    /// <param name="dy">Distance from where the drag started, in output pixels.</param>
    public void ResizeCrop(CropCorner corner, double dx, double dy)
    {
        PixelRect start = _dragStartCrop;
        PixelRect stage = StageArea;
        PixelRect image = ImageRect;
        double snapDistance = SnapDips * PixelsPerDip;

        bool left = corner is CropCorner.TopLeft or CropCorner.BottomLeft;
        bool top = corner is CropCorner.TopLeft or CropCorner.TopRight;
        double anchorX = left ? start.Right : start.X;
        double anchorY = top ? start.Bottom : start.Y;
        double roomX = Math.Min(MaxCropWidth, Math.Max(0, left ? anchorX - stage.X : stage.Right - anchorX));
        double roomY = Math.Min(MaxCropHeight, Math.Max(0, top ? anchorY - stage.Y : stage.Bottom - anchorY));

        double movedX = SnapEdge((left ? start.X : start.Right) + dx, left ? image.X : image.Right, snapDistance);
        double movedY = SnapEdge((top ? start.Y : start.Bottom) + dy, top ? image.Y : image.Bottom, snapDistance);
        double width = left ? anchorX - movedX : movedX - anchorX;
        double height = top ? anchorY - movedY : movedY - anchorY;

        if (Ratio is double locked)
        {
            // The direction that changed most, relative to the size, leads. Otherwise a sideways drag couldn't shrink the rectangle.
            bool widthLeads = Math.Abs(width - start.Width) * start.Height >= Math.Abs(height - start.Height) * start.Width;
            double maxWidth = Math.Min(roomX, roomY * locked);
            double minWidth = Math.Min(maxWidth, Math.Max(MinSidePixels, MinSidePixels * locked));
            width = Math.Clamp(widthLeads ? width : height * locked, minWidth, maxWidth);
            height = width / locked;
        }
        else
        {
            width = Math.Clamp(width, Math.Min(MinSidePixels, roomX), roomX);
            height = Math.Clamp(height, Math.Min(MinSidePixels, roomY), roomY);
        }

        Crop = new PixelRect(left ? anchorX - width : anchorX, top ? anchorY - height : anchorY, width, height);
    }

    /// <summary>
    /// The edit these choices stand for: not cropped if the crop is exactly the image at its own size, and null if nothing changed.
    /// </summary>
    public ImageEdit? ToEdit(int quarterTurns, CropFill fill)
    {
        PixelRect image = ImageRect;
        bool coversImage = Zoom == 1
            && Math.Abs(Crop.X - image.X) < WholeImageTolerancePixels && Math.Abs(Crop.Y - image.Y) < WholeImageTolerancePixels
            && Math.Abs(Crop.Right - image.Right) < WholeImageTolerancePixels && Math.Abs(Crop.Bottom - image.Bottom) < WholeImageTolerancePixels;

        if (coversImage)
            return quarterTurns == 0 ? null : new ImageEdit(quarterTurns, null);

        CropRect crop = new((Crop.X - image.X) / image.Width, (Crop.Y - image.Y) / image.Height, Crop.Width / image.Width, Crop.Height / image.Height);
        return new ImageEdit(quarterTurns, crop, Zoom, fill, Ratio);
    }

    private void StartOver()
    {
        PixelsPerDip = FittedPixelsPerDip;
        Zoom = 1;
        ImageCenterX = 0;
        ImageCenterY = 0;

        PixelRect image = ImageRect;
        if (Ratio is not double locked)
        {
            Crop = image;
            return;
        }

        double width = Math.Min(image.Width, image.Height * locked);
        double height = width / locked;
        Crop = new PixelRect(image.CenterX - width / 2, image.CenterY - height / 2, width, height);
    }

    /// <summary>
    /// Puts the crop in the middle of the display area, as large as is comfortable. The image is only drawn smaller than fitted if the crop wouldn't fit otherwise, and never larger than actual pixels.
    /// </summary>
    private void ShowCropLarge()
    {
        double comfortable = Math.Max(Crop.Width / StageWidth, Crop.Height / StageHeight) / TypedCropShareOfDisplay;
        double largest = Math.Max(FittedPixelsPerDip, Math.Max(Crop.Width / StageWidth, Crop.Height / StageHeight));
        PixelsPerDip = Math.Clamp(comfortable, 1, largest);
        ShiftEverything(-Crop.CenterX, -Crop.CenterY);
    }

    private void SetZoomCenteredOnCrop(double zoom)
    {
        Zoom = Math.Clamp(zoom, MinZoom, MaxZoom);
        ImageCenterX = Crop.CenterX;
        ImageCenterY = Crop.CenterY;
    }

    /// <summary>
    /// Moves the crop and the image together, which keeps the output as it is.
    /// </summary>
    private void ShiftEverything(double dx, double dy)
    {
        Crop = Crop with { X = Crop.X + dx, Y = Crop.Y + dy };
        ImageCenterX += dx;
        ImageCenterY += dy;
    }

    private static (double cropStart, double imageCenter) DragAlongAxis(double cropStart, double cropLength, double delta, double stageStart, double stageEnd, double imageCenter, double imageLength, double snapDistance)
    {
        double wanted = cropStart + delta;
        double lastStart = Math.Max(stageStart, stageEnd - cropLength);
        double placed = Math.Clamp(wanted, stageStart, lastStart);
        double imageStart = imageCenter - imageLength / 2;
        double imageEnd = imageCenter + imageLength / 2;

        // Past the display's edge, the distance left over moves the image, for as long as part of it is hidden beyond that edge.
        double pushed = wanted - placed;
        if (pushed < 0)
            return (placed, imageCenter + Math.Min(-pushed, Math.Max(0, stageStart - imageStart)));
        if (pushed > 0)
            return (placed, imageCenter - Math.Min(pushed, Math.Max(0, imageEnd - stageEnd)));

        double snapped = SnapStart(placed, cropLength, imageStart, imageEnd, snapDistance);
        return (Math.Clamp(snapped, stageStart, lastStart), imageCenter);
    }

    /// <summary>
    /// Where a crop starts along one axis, staying over the image if it fits, or covering the image if it's larger.
    /// </summary>
    private static double KeepOnImage(double start, double length, double imageStart, double imageEnd)
    {
        double lastStart = imageEnd - length;
        return Math.Clamp(start, Math.Min(imageStart, lastStart), Math.Max(imageStart, lastStart));
    }

    /// <summary>
    /// Where a crop starts along one axis, with whichever of its two edges is close to the image's matching edge landing on it.
    /// </summary>
    private static double SnapStart(double start, double length, double imageStart, double imageEnd, double snapDistance)
    {
        double startDistance = Math.Abs(start - imageStart);
        double endDistance = Math.Abs(start + length - imageEnd);

        if (startDistance <= snapDistance && startDistance <= endDistance)
            return imageStart;
        return endDistance <= snapDistance ? imageEnd - length : start;
    }

    private static double SnapEdge(double position, double imageEdge, double snapDistance)
    {
        return Math.Abs(position - imageEdge) <= snapDistance ? imageEdge : position;
    }
}
