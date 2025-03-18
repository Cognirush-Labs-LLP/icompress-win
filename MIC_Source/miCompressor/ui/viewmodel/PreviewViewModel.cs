using ImageMagick;
using miCompressor.core;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace miCompressor.ui
{
    /// <summary>
    /// Used for compression preview
    /// </summary>
    public partial class PreviewViewModel
    {
        private FileStore store;

        #region State Variables
        private List<MediaFileInfo> images;
        private string LastSettingHash = "";
        private ImageSource? OriginalImage_cached = null;
        private ImageSource? CompressedImage_cached = null;
        public uint CompressedImageHeight { get; private set; }
        public uint CompressedImageWidth { get; private set; }

        #endregion
        private object imagesLock = new();
        private ImageCompressor compressor;
        [AutoNotify] private bool compressionInProgress = false;
        [AutoNotify] private bool compressionFailed = false;


        private int _do_not_access_currentIndex = 0;

        private int CurrentIndex
        {
            get => _do_not_access_currentIndex;
            set
            {
                if(_do_not_access_currentIndex != value)
                {
                    RemoveCached();
                }
                _do_not_access_currentIndex = value;
                HasPrev = _do_not_access_currentIndex > 0;
                HasNext = images.Count > _do_not_access_currentIndex + 1;
            }
        }

        public PreviewViewModel(FileStore store)
        {
            this.store = store;
            store.PropertyChanged += Store_PropertyChanged;
        }

        ~PreviewViewModel()
        {
            store.PropertyChanged -= Store_PropertyChanged;
            compressor.ImageCompressed -= Compressor_ImageCompressed;
            compressor.CompressionCompleted -= Compressor_CompressionCompleted;
        }

        public void RemoveCached()
        {
            OriginalImage_cached = null;
            CompressedImage_cached = null;
            CompressedImageHeight = 0;
            CompressedImageWidth = 0;
        }

        private void Store_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if(e.PropertyName == nameof(FileStore.SelectedPaths))
                lock (imagesLock)
                    images = new List<MediaFileInfo>(store.GetAllFilesIncludingDuplicates);
        }

        private void RefreshCompressor()
        {
            if(compressor != null)
            {
                compressor.CancelCompression();
                compressor.ImageCompressed -= Compressor_ImageCompressed;
                compressor.CompressionCompleted -= Compressor_CompressionCompleted;
            }
            compressor = new();
            compressor.ImageCompressed += Compressor_ImageCompressed;
            compressor.CompressionCompleted += Compressor_CompressionCompleted;
        }

        private void Compressor_CompressionCompleted(object? sender, CompressionCompletedEventArgs e)
        {
            CompressionInProgress = false;
        }

        private void Compressor_ImageCompressed(object? sender, ImageCompressedEventArgs e)
        {
            if (e.Success)
            {
                CompressionFailed = false;
            }
            else
            {
                CompressionFailed = true;
            }
        }

        // Get Original Image at Current Index
        public async Task<ImageSource> GetOriginal()
        {
            if (OriginalImage_cached != null)
                return OriginalImage_cached;

            string imagePath = "";
            lock(imagesLock)
            {
                if (images[CurrentIndex] == null || images[CurrentIndex].FilePath == null) return null;
            }

            try
            {
                OriginalImage_cached = await GetImage(images[CurrentIndex], false);
            }
            catch
            {
                OriginalImage_cached = null;
            }
            return OriginalImage_cached;
        }

        private async Task<ImageSource> GetImage(MediaFileInfo mediaInfo, bool isCompressed)
        {
            return await GetImage(mediaInfo.FilePath, isCompressed, mediaInfo.Height, mediaInfo.Width);
        }

        /// <summary>
        /// If height and width is provided, it's checked against the decoded image to see if decoder is fine. Required only for raw images. 
        /// </summary>
        /// <param name="imagePath"></param>
        /// <param name="OriginalHeight">Required only for raw images</param>
        /// <param name="OriginalWidth">Required only for raw images</param>
        /// <returns></returns>
        private async Task<ImageSource> GetImage(string imagePath, bool isCompressed, uint OriginalHeight = 0, uint OriginalWidth = 0)
        {
            try
            {
                if (imagePath == null || !File.Exists(imagePath))
                {
                    MicLog.Info($"File doesn't exists. {Path.GetFileName(imagePath == null ? "<Image Path Null>" : imagePath)}");
                    return null;
                }

                var file = await StorageFile.GetFileFromPathAsync(imagePath);
                using (IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read))
                {
                    BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                    if (isCompressed) {
                        CompressedImageHeight = decoder.OrientedPixelHeight;
                        CompressedImageWidth = decoder.OrientedPixelWidth;
                    }

                    if (OriginalHeight == 0 || OriginalWidth == 0 || WindowsImageDecoder.IsHeightAndWidthWithinVariance(decoder, OriginalHeight, OriginalWidth))
                    {
                        SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Premultiplied);
                        SoftwareBitmapSource bitmapSource = new SoftwareBitmapSource();
                        await UIThreadHelper.RunOnUIThreadAsync(async () => await bitmapSource.SetBitmapAsync(softwareBitmap));
                        return bitmapSource;
                    }
                    else
                        return await GetImageFromImageMagic(imagePath, isCompressed);
                }
            }
            catch (Exception)
            {
                return await GetImageFromImageMagic(imagePath, isCompressed);
            }
        }

        private async Task<ImageSource>  GetImageFromImageMagic(string imagePath, bool isCompressed)
        {
            SoftwareBitmap softwareBitmap = await GetBitmapFromImageMagick(imagePath, isCompressed);
            SoftwareBitmapSource bitmapSource = new SoftwareBitmapSource();
            await bitmapSource.SetBitmapAsync(softwareBitmap);
            return bitmapSource;
        }

        public async Task<SoftwareBitmap> GetBitmapFromImageMagick(string filePath, bool isCompressed)
        {
#pragma warning disable CS8603 // Possible null reference return.
            return await Task.Run(() =>
            {
                try
                {
                    using (MagickImage magickImage = new MagickImage(filePath))
                    {
                        magickImage.AutoOrient();
                        int width = (int)magickImage.Width;
                        int height = (int)magickImage.Height;

                        if (isCompressed)
                        {
                            CompressedImageHeight = magickImage.Height;
                            CompressedImageWidth = magickImage.Width;
                        }

                        //magickImage.AutoOrient();
                        // Create SoftwareBitmap with correct format
                        SoftwareBitmap softwareBitmap = new SoftwareBitmap(BitmapPixelFormat.Bgra8, width, height, BitmapAlphaMode.Premultiplied);

                        // Get raw pixel data from MagickImage (BGRA format)
                        if (magickImage == null || magickImage.GetPixels() == null) return null;

                        byte[] pixelData = magickImage.GetPixels().ToByteArray(PixelMapping.BGRA);

                        // Copy data into SoftwareBitmap
                        softwareBitmap.CopyFromBuffer(pixelData.AsBuffer());

                        return softwareBitmap; // Caller must dispose this
                    }
                }
                catch
                {
                    MicLog.Info($"Cannot decode {Path.GetFileName(filePath)}");
                    return null;
                }
            }).ConfigureAwait(false);
#pragma warning restore CS8603 // Possible null reference return.
        }

        public async Task<ImageSource> GetOriginal(MediaFileInfo media)
        {
            OriginalImage_cached = null;
            if (!SetImage(media))
                return null; //cannot find image in the store. 

            return await GetOriginal();
        }

        /// <summary>
        /// Sets the current index to given image. Required for  proper Next/Previous travel.  
        /// </summary>
        /// <param name="media"></param>
        /// <returns></returns>
        public bool SetImage(MediaFileInfo media)
        {
            lock (imagesLock)
                images = new List<MediaFileInfo>(store.GetAllFilesIncludingDuplicates);

            int indexOfMedia = images.IndexOf(media);
            if (indexOfMedia < 0) return false;

            CurrentIndex = indexOfMedia;
            return true;
        }


        public async Task<(ImageSource, bool noChange)> GetCompressed(MediaFileInfo media, OutputSettings settings)
        {
            if (!SetImage(media))
                return (null, false); //cannot find image in the store. 

            return await GetCompressed(settings);
        }

        public async Task<(ImageSource, bool noChange)> GetCompressed(OutputSettings settings)
        {
            var newSettingHash = settings.GetHashForImagePreviewRegeneration();

            if (CompressedImage_cached != null && LastSettingHash == newSettingHash)
                return (CompressedImage_cached, true);

            LastSettingHash = newSettingHash;

            RefreshCompressor();

            MediaFileInfo mediaFile = null;
            lock (imagesLock)
            {
                if (images[CurrentIndex] == null)
                    return (null, false);
                mediaFile = images[CurrentIndex];
            }
            //string outputPath = await compressor.ProcessSingleFileAsync(mediaFile, multipleSelectPaths: false, settings, forPreview: true);

            string outputPath = await Task.Run(async () =>
            {
                return await compressor.ProcessSingleFileAsync(mediaFile, multipleSelectPaths: false, settings, forPreview: true);
            });

            CompressedImage_cached = await GetImage(outputPath, true);

            return (CompressedImage_cached, false);
        }

        [AutoNotify] private bool hasPrev = false;
        [AutoNotify] private bool hasNext = false;

    }
}
