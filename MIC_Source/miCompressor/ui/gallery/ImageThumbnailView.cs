using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using miCompressor.core; // Assuming FileInfo is defined here
using System;
using Windows.Foundation;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using Microsoft.UI.Xaml.Data;

namespace miCompressor.ui
{
    /// <summary>
    /// A custom control that conditionally loads an Image.
    /// Conditions: Can be specified as in 'Show' property. Removed Bitmap source if not in view. 
    /// </summary>
    public class ImageThumbnailView : ContentControl
    {
        // DependencyProperty for FileInfo.
        public static readonly DependencyProperty FileInfoProperty =
            DependencyProperty.Register(nameof(MediaFileInfo), typeof(MediaFileInfo), typeof(ImageThumbnailView),
                new PropertyMetadata(null, OnFileInfoChanged));

        // DependencyProperty for Show.
        public static readonly DependencyProperty ShowProperty =
            DependencyProperty.Register(nameof(Show), typeof(bool), typeof(ImageThumbnailView),
                new PropertyMetadata(false, OnShowChanged));

        public MediaFileInfo FileInfo
        {
            get => (MediaFileInfo)GetValue(FileInfoProperty);
            set => SetValue(FileInfoProperty, value);
        }

        public bool Show
        {
            get => (bool)GetValue(ShowProperty);
            set => SetValue(ShowProperty, value);
        }

        

        // Optional: if you want to disable auto-loading behavior.
        public static readonly DependencyProperty AutoLoadOnVisibleProperty =
            DependencyProperty.Register(nameof(AutoLoadOnVisible), typeof(bool), typeof(ImageThumbnailView),
                new PropertyMetadata(true));

        public bool AutoLoadOnVisible
        {
            get => (bool)GetValue(AutoLoadOnVisibleProperty);
            set => SetValue(AutoLoadOnVisibleProperty, value);
        }

        public ImageThumbnailView()
        {
            // Subscribe to Loaded/Unloaded to attach/detach event handlers.
            this.Loaded += ImageThumbnailView_Loaded;
            this.Unloaded += ImageThumbnailView_Unloaded;
        }

        private static void OnFileInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ImageThumbnailView)d;
            control.UpdateContent();
        }

        private static void OnShowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (ImageThumbnailView)d;
            control.UpdateContent();
        }

        private void ImageThumbnailView_Loaded(object sender, RoutedEventArgs e)
        {
            this.EffectiveViewportChanged -= ImageThumbnailView_EffectiveViewportChanged;
            this.EffectiveViewportChanged += ImageThumbnailView_EffectiveViewportChanged;

            // Initial check.
            CheckVisibilityAndUpdate();
        }

        private void ImageThumbnailView_Unloaded(object sender, RoutedEventArgs e)
        {
            
            this.LayoutUpdated -= ImageThumbnailView_EffectiveViewportChanged;
        }

        private void ImageThumbnailView_EffectiveViewportChanged(object sender, object e)
        {
            CheckVisibilityAndUpdate();
        }

        private bool _inView = false;
        private object _inView_lock = new();
        /// <summary>
        /// Checks if the control is visible in the parent ScrollViewer's viewport.
        /// </summary>
        private void CheckVisibilityAndUpdate()
        {
            if (!AutoLoadOnVisible || !Show || this.FileInfo == null)
                return;

            // Schedule the update after the current layout pass.
            _ = DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, () =>
            {
                lock (_inView_lock)
                {
                    if (_inView != IsInView())
                    {
                        _inView = !_inView;
                        UpdateContent();
                    }
                }
            });
        }


        /// <summary>
        /// Determines if this control is currently within the visible viewport of its parent ScrollViewer.
        /// </summary>
        /// <returns>True if in view, false otherwise.</returns>
        private bool IsInView()
        {
            try
            {
                // Transform element bounds to window coordinates.
                GeneralTransform elementTransform = this.TransformToVisual(App.MainWindow.Content);
                Rect elementRect = elementTransform.TransformBounds(new Rect(0, 0, this.ActualWidth, this.ActualHeight));

                return RectIntersects(elementRect, App.MainWindow.Bounds);
            }
            catch (Exception)
            {
                // In case transformation fails, assume visible.
                return true;
            }
        }

        private bool RectIntersects(Windows.Foundation.Rect rect1, Windows.Foundation.Rect rect2)
        {
            return rect1.X < rect2.X + rect2.Width &&
                   rect1.X + rect1.Width > rect2.X &&
                   rect1.Y < rect2.Y + rect2.Height &&
                   rect1.Y + rect1.Height > rect2.Y;
        }


        private Image _image = null;
        private object UpdateContentLock = new();
        /// <summary>
        /// Updates the Content of the control based on the Show flag and FileInfo.
        /// </summary>
        private void UpdateContent()
        {
            
                // Only create the Image control (and thus invoke FileInfo.Thumbnail) when Show is true.
                if (this.FileInfo == null || !this.Show)
                {
                    this.Content = null;
                    return;
                }
            ThrottleTask.Add(30, FileInfo.FilePath + "UpdateContent", () =>
            {
                var thumbSize = this.FileInfo.ThumbnailSize;

                if (_image == null)
                {
                    _image = new Image
                    {
                        Stretch = Stretch.Uniform,
                        IsHitTestVisible = false,
                        Width = thumbSize,
                        Height = thumbSize,
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    this.Content = _image;
                }
                
                if (!this._inView)
                {
                    _image.Source = null;
                    return;
                }

                if (this.FileInfo.FileSize > 100 * 1024) // don't bother creating and caching thumbs for smaller images.  
                {
                    if (this.FileInfo.Thumbnail == null)
                    {
                        // Bind Source property to FileInfo.Thumbnail
                        var binding = new Binding
                        {
                            Source = this.FileInfo,
                            Path = new PropertyPath(nameof(FileInfo.Thumbnail)),
                            Mode = BindingMode.OneWay
                        };

                        _image.SetBinding(Image.SourceProperty, binding);
                    }
                    else
                    {
                        _image.Source = this.FileInfo.Thumbnail;
                    }
                }
                else
                {
                    if (_image.Source == null)
                        _image.Source = new BitmapImage(new Uri(this.FileInfo.FilePath));
                }
                this.Content = _image;
            }, true);
        }
    }
}
