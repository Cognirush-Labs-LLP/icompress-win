# Backlog

Found by the release integrity run on 2026-11-10 (every supported input format to every output format, animations frame by frame, SVG, broken files, RAW, resize, watermark). All of these already existed before the Magick.NET 15 upgrade.

## To be fixed

### 1. Transparent images saved as JPEG get a black background

JPEG has no transparency, and transparent areas come out black instead of white.

- Repro: `dice.png`, `eglite.gif` or `anim-icos.webp` → JPEG. The corner pixel of the output is `#000000`.
- Expected: transparent areas become white.

### 2. Watermark is never applied to GIF input saved as GIF, PNG or Keep Same

GIF → GIF and GIF → PNG go through gifsicle and gif2apng (`ConvertFromGIFToGIF` and `ConvertFromGIFToPNG` in `ImageCompressor.cs`), and that path has no watermark step. It happens at any image size.

- Repro: an 800×450 animated GIF or a 900×600 still GIF, watermark on, output GIF, PNG or Keep Same. No watermark in any frame.
- Expected: watermark in every frame, as with WebP, AVIF and JPEG XL output.

### 3. Watermark is dropped when the watermarked file isn't smaller than the original

When the output has the same format and dimensions as the input and isn't smaller, `MediaFileInfo.IsLargerFileWithoutChange` copies the original over it. It checks for an edit but not for a watermark, so the watermark is lost.

- Repro: `dice.webp` (800×600) → WebP or Keep Same with watermark on. Output is the unwatermarked original, with the "used original file" warning.
- Expected: a watermarked output is always kept.

### 4. Multi-page TIFF saved as JPEG XL keeps only the last page

The pages have no frame delay, so the JPEG XL encoder merges them into one frame, and only the last page is visible. WebP, PNG, AVIF and GIF keep all pages.

- Repro: a 3-page TIFF → JPEG XL. Output has 1 frame showing page 3.
- Expected: show a warning about potential data loss.

### 5. Typo in `WatermarkHelper.WatermarkAllFrames`

The check for different-sized frames reads `f.Height != frames[0].Width`; it should be `f.Width != frames[0].Width`. No visible effect found in testing.

### 6. Typo on the pre-compression warning page

"File format will changed as the selected original format is not supported in output." should read "will change". The text is in `WarningHelper.cs`.

### 7. Read a file by its content when its extension's decoder fails

Some "RAW" files are plain JPEGs with a RAW extension. Windows Photos opens them because Windows identifies the format from the content. The app fails:

1. Magick picks its RAW decoder from the extension and rejects the file, so the dimensions read as 0×0.
2. For `.dng`, `.nef`, `.cr2` and `.cr3`, the app tries the Windows decoder first, and it can read these files. But `WindowsImageDecoder` rejects that result because it isn't within 5% of the 0×0 size.

- Repro: `DSC_9101~2 ….NEF` and `IMG_5245.cr2` in `data/unable_to_read_metadata` (both start with the JPEG signature `FF D8`).
- Expected: when decoding by extension fails, detect the format from the content and decode again.

## Deferred unless it causes a problem or users ask for it

- **JPEG XL from GIF or low-colour images can be much larger than the source.** The app tries lossless for low-colour images and keeps the smaller of lossy and lossless, which can still be bigger than the GIF. `eglite.gif` 15.5 KB → 98 KB. The app shows the "file size increased" warning.
- **A truncated animated GIF saved as GIF reports success.** The output is as truncated as the input, and Windows still shows the frames that survived.
- **A watermark wider than the image is cropped on the left** instead of being scaled down to fit.
- **Multi-page TIFF pages get a 10–100 ms frame delay** in WebP, PNG and AVIF output. The source pages have none.
- **After Cancel, the result screen says "Done!"**, with the cancelled count below it. Images that were mid-compression when Cancel was pressed still finish and are saved (complete and valid), but they're counted as cancelled. For example, 11 files were written while the screen said 7 processed and 43 cancelled.
- **Icon output of an animated image shows a generic error.** The error page says "Compression failed due to unsupported format features or file corruption" instead of "Icon doesn't support animated images".
- **These supported input extensions have never been tested**, for lack of samples: 3fr, crw, dcr, erf, gpr, iiq, kdc, mef, mos, mpo, mrw, nrw, pef, raw, rwl, sr2, srf, srw, x3f.
