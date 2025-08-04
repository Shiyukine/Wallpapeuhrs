using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Wallpapeuhrs.Utils;
using Windows.Devices.Geolocation;
using Windows.Foundation.Collections;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Display;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using Windows.System.Display;
using Windows.UI.WebUI;

namespace Wallpapeuhrs
{
    /// <summary>
    /// Logique d'interaction pour Media.xaml
    /// </summary>
    public partial class MediaEffect : UserControl
    {
        public double volume;
        public bool repeat;
        public float nextChange = 0;
        private WPBG parent;
        Dictionary<string, BitmapCache> caches = new Dictionary<string, BitmapCache>();
        float saturation = 1f;
        float hue = 1f;
        float contrast = 1f;
        float brightness = 1f;
        bool canScreenshot = true;

        public MediaEffect(WPBG parent)
        {
            this.parent = parent;
            InitializeComponent();
        }

        public void init()
        {
        }

        public Microsoft.UI.Xaml.Media.Animation.DoubleAnimation addDoubleAnimation(Microsoft.UI.Xaml.UIElement el, TimeSpan time, double? from, double? to, string property)
        {
            Microsoft.UI.Xaml.Media.Animation.DoubleAnimation da = new Microsoft.UI.Xaml.Media.Animation.DoubleAnimation();
            da.From = from;
            da.To = to;
            da.Duration = new Microsoft.UI.Xaml.Duration(time);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTarget(da, el);
            Microsoft.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(da, property);
            return da;
        }

        public BitmapCache setCache(string url)
        {
            if (!caches.ContainsKey(url))
            {
                BitmapCache bc = new BitmapCache();
                caches.Add(url, bc);
                return bc;
            }
            else return caches[url];
        }

        string curUrl = "";

        public async void changeUrl(string newUrl)
        {
            curUrl = newUrl;
            parent.log("aaaaaaaa " + newUrl + " " + System.IO.Path.GetExtension(newUrl));
            foreach (string ext in App.types.Keys)
            {
                parent.log("aaaaaaab " + ext);
                if (App.types[ext].Contains(System.IO.Path.GetExtension(newUrl)))
                {
                    parent.log("aaaaaaac " + ext);
                    stopPlayers();
                    canScreenshot = true;
                    CanvasControl canvas = new CanvasControl();
                    canvas.Width = main.ActualWidth;
                    canvas.Height = main.ActualHeight;
                    canvas.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
                    canvas.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center;
                    CanvasDevice canvasDevice = null;
                    CanvasRenderTarget _currentFrame = null;
                    canvas.CreateResources += async (s, e) =>
                    {
                        try
                        {
                            if (canvas == null || canvas.Device == null) return;
                            canvasDevice = canvas.Device;
                        }
                        catch (Exception ex)
                        {
                            System.Windows.MessageBox.Show("Canvas create resources error:\n" + ex.ToString());
                        }
                    };
                    bool rendering = false;
                    canvas.Draw += async (sender, args) =>
                    {
                        try
                        {
                            if (_currentFrame == null) return;
                            var ds = args.DrawingSession;
                            var effect = CreateImageAdjustmentEffect(_currentFrame, brightness, contrast, saturation, hue);
                            ds.DrawImage(effect);
                            effect.Dispose();
                            rendering = false;
                        }
                        catch (COMException ex) when (canvasDevice.IsDeviceLost(ex.ErrorCode))
                        {
                            canvasDevice.RaiseDeviceLost();
                        }
                    };
                    main.Children.Insert(0, canvas);
                    if (ext == "Video files")
                    {
                        Microsoft.UI.Xaml.Controls.MediaPlayerElement me = new Microsoft.UI.Xaml.Controls.MediaPlayerElement();
                        me.CacheMode = setCache(newUrl);
                        MediaPlayer mediaPlayer = new MediaPlayer();
                        mediaPlayer.Source = MediaSource.CreateFromUri(new Uri(newUrl));
                        mediaPlayer.SystemMediaTransportControls.IsEnabled = false;
                        mediaPlayer.CommandManager.IsEnabled = false;
                        mediaPlayer.MediaOpened += async (sender, eee) =>
                        {
                            remove(me);
                        };
                        mediaPlayer.MediaFailed += async (sender, e) =>
                        {
                            DispatcherQueue.TryEnqueue(() =>
                            {
                                if (me.MediaPlayer != null)
                                {
                                    Uri source = (me.Source as MediaSource).Uri;
                                    string file = source.OriginalString != string.Empty ? source.OriginalString : source.AbsolutePath;
                                    System.Windows.MessageBox.Show("Unable to launch or read the media. Please verify the media's path and verify if it's an video.\nFile : " + file + "\n\nIntern error :\n" + e.Error.ToString() + "\n" + e.ExtendedErrorCode, "Wallpapeuhrs - Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                                }
                            });
                        };
                        mediaPlayer.IsVideoFrameServerEnabled = true;
                        me.AreTransportControlsEnabled = false;
                        double v = volume;
                        me.AutoPlay = true;
                        mediaPlayer.IsLoopingEnabled = true;
                        mediaPlayer.Volume = v / 100;
                        double w = main.ActualWidth;
                        double h = main.ActualHeight;
                        var scale = main.XamlRoot.RasterizationScale;
                        mediaPlayer.VideoFrameAvailable += (s, e) =>
                        {
                            try
                            {
                                if(_currentFrame == null && canvasDevice != null)
                                {
                                    DispatcherQueue.TryEnqueue(() =>
                                    {
                                        try
                                        {
                                            // Get video dimensions
                                            var vidWidth = mediaPlayer.PlaybackSession.NaturalVideoWidth;
                                            var vidHeight = mediaPlayer.PlaybackSession.NaturalVideoHeight;
                                            parent.log("Video dimensions: " + vidWidth + "x" + vidHeight);

                                            // Define your target canvas size
                                            double canvasWidth = w * scale;
                                            double canvasHeight = h * scale;

                                            // Calculate aspect ratios
                                            double videoAspect = (double)vidWidth / vidHeight;
                                            double canvasAspect = canvasWidth / canvasHeight;

                                            double renderWidth, renderHeight;
                                            double marginLeft = 0, marginTop = 0;

                                            if (videoAspect < canvasAspect)
                                            {
                                                // Portrait video (or video taller than canvas aspect)
                                                // Use ALL the canvas WIDTH, scale height proportionally
                                                renderWidth = canvasWidth;
                                                renderHeight = canvasWidth / videoAspect;

                                                // Center vertically with margins
                                                marginTop = (canvasHeight - renderHeight) / 2;
                                                marginLeft = 0;
                                            }
                                            else
                                            {
                                                // Landscape video (or video wider than canvas aspect)  
                                                // Use ALL the canvas HEIGHT, scale width proportionally
                                                renderHeight = canvasHeight;
                                                renderWidth = canvasHeight * videoAspect;

                                                // Center horizontally with margins
                                                marginLeft = (canvasWidth - renderWidth) / 2;
                                                marginTop = 0;
                                            }

                                            parent.log($"Video aspect: {videoAspect:F2}, Canvas aspect: {canvasAspect:F2}");
                                            parent.log($"Render size: {renderWidth}x{renderHeight}");
                                            parent.log($"Margins: left={marginLeft}, top={marginTop}");

                                            // Use the ENTIRE video as source (no cropping)
                                            double sourceX = 0;
                                            double sourceY = 0;
                                            double sourceWidth = vidWidth;
                                            double sourceHeight = vidHeight;

                                            _currentFrame = new CanvasRenderTarget(canvasDevice, (int)renderWidth, (int)renderHeight, 96f);
                                            canvas.Width = (float)renderWidth;
                                            canvas.Height = (float)renderHeight;
                                            canvas.Margin = new Microsoft.UI.Xaml.Thickness(
                                                (float)marginLeft,
                                                (float)marginTop,
                                                (float)marginLeft,
                                                (float)marginTop
                                            );
                                        }
                                        catch (Exception ex)
                                        {
                                            System.Windows.MessageBox.Show("MediaPlayerElement MediaOpened error:\n" + ex.ToString());
                                        }
                                    });
                                }
                                if (_currentFrame == null || canvasDevice == null || rendering) return;
                                rendering = true;
                                mediaPlayer.CopyFrameToVideoSurface(_currentFrame);
                                canvas.Invalidate();
                            }
                            catch (COMException ex) when (canvasDevice.IsDeviceLost(ex.ErrorCode))
                            {
                                canvasDevice.RaiseDeviceLost();
                            }
                        };
                        me.SetMediaPlayer(mediaPlayer);
                        mediaPlayer.Play();
                    }
                    if (ext == "Image files")
                    {
                        ImageToMemoryStream img = new ImageToMemoryStream();
                        img.ImageLoadCompleted += () =>
                        {
                            remove(img);
                            /* necessary?
                            *DispatcherQueue.TryEnqueue(() =>
                            {
                                parent.changeNativeWallpaper(null);
                            });*/
                        };
                        img.ImageLoadFailed += (e) =>
                        {
                            DispatcherQueue.TryEnqueue(() =>
                            {
                                Uri source = img.UriSource;
                                string file = source.OriginalString != string.Empty ? source.OriginalString : source.AbsolutePath;
                                System.Windows.MessageBox.Show("Unable to launch or read the image. Please verify the image's path and verify if it's an image.\nFile : " + file + "\n\nIntern error :\n" + e.Message + "\n" + e.ToString(), "Wallpapeuhrs - Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                            });
                        };
                        double w = main.ActualWidth;
                        double h = main.ActualHeight;
                        async void updateImage(MemoryStream ms)
                        {
                            if(_currentFrame == null && canvasDevice != null)
                            {
                                _currentFrame = new CanvasRenderTarget(canvasDevice, (float)w, (float)h, 96f);
                            }
                            if (_currentFrame == null || canvasDevice == null || rendering) return;
                            rendering = true;
                            var r2 = await CanvasBitmap.LoadAsync(canvasDevice, ms.AsRandomAccessStream());
                            using (var ds = _currentFrame.CreateDrawingSession())
                            {
                                // Calculate the aspect ratios
                                float imageAspect = (float)((float)img.Width / img.Height);
                                float canvasAspect = (float)((float)_currentFrame.SizeInPixels.Width / _currentFrame.SizeInPixels.Height);

                                float sourceX, sourceY, sourceWidth, sourceHeight;

                                if (imageAspect > canvasAspect)
                                {
                                    // Image is wider than canvas - crop sides
                                    sourceHeight = img.Height;
                                    sourceWidth = img.Height * canvasAspect;
                                    sourceX = (img.Width - sourceWidth) / 2;
                                    sourceY = 0;
                                }
                                else
                                {
                                    // Image is taller than canvas - crop top/bottom
                                    sourceWidth = img.Width;
                                    sourceHeight = img.Width / canvasAspect;
                                    sourceX = 0;
                                    sourceY = (img.Height - sourceHeight) / 2;
                                }

                                // Draw the cropped center portion to fill the entire canvas
                                ds.DrawImage(r2,
                                    new Windows.Foundation.Rect(0, 0, _currentFrame.SizeInPixels.Width, _currentFrame.SizeInPixels.Height), // destination (full canvas)
                                    new Windows.Foundation.Rect(sourceX, sourceY, sourceWidth, sourceHeight)); // source (cropped center)

                                canvas.Invalidate();
                            }
                        }
                        img.ImageUpdated += (ms) =>
                        {
                            if(img.IsAnimated) updateImage(ms);
                            else
                            {
                                canvas.CreateResources += (s, e) =>
                                {
                                    updateImage(ms);
                                };
                            }
                        };

                        img.UriSource = new Uri(newUrl);
                    }
                    return;
                }
            }
        }

        public void stopPlayers()
        {
            if (_value == null) return;
            if (_value.GetType() == typeof(Microsoft.UI.Xaml.Controls.MediaPlayerElement))
            {
                (_value as MediaPlayerElement).MediaPlayer.Pause();
            }
            else if (_value.GetType() == typeof(ImageToMemoryStream))
            {
                ImageToMemoryStream med = (ImageToMemoryStream)_value;
                if (med.UriSource != null)
                {
                    med.StopAnimation();
                }
            }
        }

        public void changePlayerState(bool play)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    if (_value == null) return;
                    if (_value is Microsoft.UI.Xaml.Controls.MediaPlayerElement)
                    {
                        MediaPlayerElement me = (MediaPlayerElement)_value;
                        if (me.MediaPlayer != null)
                        {
                            if (play) me.MediaPlayer.Play();
                            else me.MediaPlayer.Pause();
                        }
                    }
                    else if (_value is ImageToMemoryStream)
                    {
                        ImageToMemoryStream med = (ImageToMemoryStream)_value;
                        if (play) med.ResumeAnimation();
                        else med.PauseAnimation();
                    }
                }
                catch (Exception e)
                {
                    System.Windows.MessageBox.Show("changePlayerState:\n" + e.ToString());
                }
            });
        }

        object _value = null;

        private async void remove(object value)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                try
                {
                    if (value is Microsoft.UI.Xaml.Controls.MediaPlayerElement)
                    {
                        (value as Microsoft.UI.Xaml.Controls.MediaPlayerElement).AreTransportControlsEnabled = false;
                        (value as Microsoft.UI.Xaml.Controls.MediaPlayerElement).MediaPlayer.SystemMediaTransportControls.IsEnabled = false;
                        (value as Microsoft.UI.Xaml.Controls.MediaPlayerElement).MediaPlayer.CommandManager.IsEnabled = false;
                        //(value as Windows.UI.Xaml.Controls.MediaPlayerElement).TransportControls.IsEnabled = false;
                        //SystemMediaTransportControls.GetForCurrentView().IsEnabled = false;
                        //SystemMediaTransportControls.GetForCurrentView().DisplayUpdater.Update();
                    }
                    for (int index = 0; index < main.Children.Count; index++)
                    {
                        if (index > 0 && main.Children[index].Opacity == 1)
                        {
                            if (main.Children[index].GetType() == typeof(CanvasControl))
                            {
                                Microsoft.UI.Xaml.Media.Animation.Storyboard sb = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
                                var canvas = (CanvasControl)main.Children[index];
                                sb.Children.Add(addDoubleAnimation(canvas, TimeSpan.FromMilliseconds(1000), 1, 0, "Opacity"));
                                sb.Completed += (sendere, ee) =>
                                {
                                    parent.log("uwu2");
                                    try
                                    {
                                        if (_value != null && _value is MediaPlayerElement)
                                        {
                                            Microsoft.UI.Xaml.Controls.MediaPlayerElement med = _value as MediaPlayerElement;
                                            med.IsEnabled = false;
                                            if (med.Source != null)
                                            {
                                                (med.Source as MediaSource).Dispose();
                                                med.Source = null;
                                            }
                                            if (med.MediaPlayer != null)
                                            {
                                                med.MediaPlayer.Pause();
                                                med.MediaPlayer.IsVideoFrameServerEnabled = false;
                                                med.MediaPlayer.Dispose();
                                                med.SetMediaPlayer(null);
                                            }
                                            //med.CacheMode = null;
                                            med = null;
                                        }
                                        else if(_value != null && _value is ImageToMemoryStream)
                                        {
                                            ImageToMemoryStream med = _value as ImageToMemoryStream;
                                            if (med.UriSource != null)
                                            {
                                                med.StopAnimation();
                                                //img.SetSource(null);
                                            }
                                            med.Dispose();
                                            //med.CacheMode = null;
                                            med = null;
                                        }
                                        main.Children.Remove(canvas);
                                        canvas = null;
                                        GC.Collect();
                                        GC.WaitForPendingFinalizers();
                                        GC.Collect();
                                        GC.WaitForPendingFinalizers();
                                    }
                                    catch (Exception e)
                                    {
                                        System.Windows.MessageBox.Show("Storyboard end:\n" + e.ToString());
                                    }
                                    _value = value;
                                };
                                sb.Begin();
                            }
                        }
                    }
                }
                catch(Exception e)
                {
                    System.Windows.MessageBox.Show("remove:\n" + e.ToString());
                }
                if(_value == null) _value = value;
            });
        }

        public async Task<MemoryStream> screenshot()
        {
            if (!canScreenshot) return null;
            canScreenshot = false;
            await Task.Delay(1000);
            parent.log("screenshot");
            if (_value is ImageToMemoryStream)
            {
                Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap renderTargetBitmap =
                    new Microsoft.UI.Xaml.Media.Imaging.RenderTargetBitmap();
                await renderTargetBitmap.RenderAsync(main);
                IBuffer buf = await renderTargetBitmap.GetPixelsAsync();
                var encoded = new InMemoryRandomAccessStream();
                var encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, encoded);
                byte[] bytes = buf.ToArray();
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                    (uint)renderTargetBitmap.PixelWidth, (uint)renderTargetBitmap.PixelHeight, 96, 96, bytes);
                await encoder.FlushAsync();
                encoded.Seek(0);
                var by = new byte[encoded.Size];
                await encoded.AsStream().ReadAsync(by, 0, by.Length);
                encoded.Dispose();
                encoded = null;
                renderTargetBitmap = null;
                buf = null;
                bytes = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                return new MemoryStream(by);
            }
            if (_value is Microsoft.UI.Xaml.Controls.MediaPlayerElement && main.Children.Count > 0 && main.Children[0] is CanvasControl)
            {
                var canvas = main.Children[0] as CanvasControl;
                SoftwareBitmap softwareBitmap;
                var frame = new SoftwareBitmap(BitmapPixelFormat.Rgba8,
                (int)(_value as Microsoft.UI.Xaml.Controls.MediaPlayerElement).MediaPlayer.PlaybackSession.NaturalVideoWidth, (int)(_value as Microsoft.UI.Xaml.Controls.MediaPlayerElement).MediaPlayer.PlaybackSession.NaturalVideoHeight, BitmapAlphaMode.Ignore);
                var canvasDevice = CanvasDevice.GetSharedDevice();
                using (var inputBitmap = CanvasBitmap.CreateFromSoftwareBitmap(canvasDevice, frame))
                {
                    (_value as Microsoft.UI.Xaml.Controls.MediaPlayerElement).MediaPlayer.CopyFrameToVideoSurface(inputBitmap);

                    using (var stream = new Windows.Storage.Streams.InMemoryRandomAccessStream())
                    {
                        await inputBitmap.SaveAsync(stream, CanvasBitmapFileFormat.Png);
                        var decoder = await Windows.Graphics.Imaging.BitmapDecoder.CreateAsync(stream);
                        softwareBitmap = await decoder.GetSoftwareBitmapAsync();
                    }
                }
                IBuffer buf = new Windows.Storage.Streams.Buffer((uint)(softwareBitmap.PixelWidth * softwareBitmap.PixelHeight * 4));
                softwareBitmap.CopyToBuffer(buf);
                var encoded = new InMemoryRandomAccessStream();
                var encoder = await Windows.Graphics.Imaging.BitmapEncoder.CreateAsync(Windows.Graphics.Imaging.BitmapEncoder.PngEncoderId, encoded);
                byte[] bytes = buf.ToArray();
                encoder.SetPixelData(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                    (uint)softwareBitmap.PixelWidth, (uint)softwareBitmap.PixelHeight, 96, 96, bytes);
                await encoder.FlushAsync();
                encoded.Seek(0);
                var by = new byte[encoded.Size];
                await encoded.AsStream().ReadAsync(by, 0, by.Length);
                encoded.Dispose();
                encoded = null;
                buf = null;
                bytes = null;
                GC.Collect();
                GC.WaitForPendingFinalizers();
                return new MemoryStream(by);
            }
            return null;
        }

        public static Microsoft.Graphics.Canvas.Effects.ColorMatrixEffect CreateImageAdjustmentEffect(
    ICanvasImage source,
    float brightness = 100,    // 0-200 (100 = normal)
    float contrast = 100,      // 0-200 (100 = normal) 
    float saturation = 100,    // 0-200 (100 = normal)
    float hue = 0)           // 0-360 degrees (0 = no change)
        {
            // Convert values to factors
            float brightnessFactor = brightness / 100.0f;
            float contrastFactor = contrast / 100.0f;
            float s = saturation / 100.0f; // Saturation factor

            // Convert 0-360 to radians
            float hueRadians = (hue % 360) * (float)(Math.PI / 180.0);

            // Microsoft's EXACT luminance weights from the documentation image
            const float lumR = 0.213f;
            const float lumG = 0.715f;
            const float lumB = 0.072f;

            // Microsoft's EXACT saturation matrix from the image:
            // Row 1: [0.213 + 0.787s, 0.213 - 0.213s, 0.213 - 0.213s]
            // Row 2: [0.715 - 0.715s, 0.715 + 0.285s, 0.715 - 0.715s]  
            // Row 3: [0.072 - 0.072s, 0.072 - 0.072s, 0.072 + 0.928s]

            float sat11 = 0.213f + 0.787f * s;
            float sat12 = 0.213f - 0.213f * s;
            float sat13 = 0.213f - 0.213f * s;

            float sat21 = 0.715f - 0.715f * s;
            float sat22 = 0.715f + 0.285f * s;
            float sat23 = 0.715f - 0.715f * s;

            float sat31 = 0.072f - 0.072f * s;
            float sat32 = 0.072f - 0.072f * s;
            float sat33 = 0.072f + 0.928f * s;

            // Hue rotation matrix coefficients
            float cosHue = (float)Math.Cos(hueRadians);
            float sinHue = (float)Math.Sin(hueRadians);

            float a = cosHue + (1.0f - cosHue) / 3.0f;
            float b = (1.0f / 3.0f) * (1.0f - cosHue) - (float)Math.Sqrt(1.0f / 3.0f) * sinHue;
            float c = (1.0f / 3.0f) * (1.0f - cosHue) + (float)Math.Sqrt(1.0f / 3.0f) * sinHue;

            // Create combined matrix: Brightness × Contrast × (Saturation × Hue)
            Matrix5x4 matrix = new Matrix5x4();

            // Red channel - multiply saturation matrix by hue matrix
            matrix.M11 = brightnessFactor * contrastFactor * (sat11 * a + sat12 * c + sat13 * b);
            matrix.M12 = brightnessFactor * contrastFactor * (sat11 * b + sat12 * a + sat13 * c);
            matrix.M13 = brightnessFactor * contrastFactor * (sat11 * c + sat12 * b + sat13 * a);
            matrix.M14 = 0;

            // Green channel
            matrix.M21 = brightnessFactor * contrastFactor * (sat21 * a + sat22 * c + sat23 * b);
            matrix.M22 = brightnessFactor * contrastFactor * (sat21 * b + sat22 * a + sat23 * c);
            matrix.M23 = brightnessFactor * contrastFactor * (sat21 * c + sat22 * b + sat23 * a);
            matrix.M24 = 0;

            // Blue channel
            matrix.M31 = brightnessFactor * contrastFactor * (sat31 * a + sat32 * c + sat33 * b);
            matrix.M32 = brightnessFactor * contrastFactor * (sat31 * b + sat32 * a + sat33 * c);
            matrix.M33 = brightnessFactor * contrastFactor * (sat31 * c + sat32 * b + sat33 * a);
            matrix.M34 = 0;

            // Alpha channel (unchanged)
            matrix.M41 = 0;
            matrix.M42 = 0;
            matrix.M43 = 0;
            matrix.M44 = 1;

            // Contrast offset
            float contrastOffset = (1 - contrastFactor) * 0.5f;
            matrix.M51 = contrastOffset;
            matrix.M52 = contrastOffset;
            matrix.M53 = contrastOffset;
            matrix.M54 = 0;

            return new Microsoft.Graphics.Canvas.Effects.ColorMatrixEffect
            {
                Source = source,
                ColorMatrix = matrix,
                CacheOutput = true
            };
        }

        DebounceDispatcher debounceEffect = new DebounceDispatcher();

        public async void changeFilter(string filter, double value)
        {
            if (filter == "saturate")
            {
                saturation = (float)value;
            }
            else if (filter == "hue-rotate")
            {
                hue = (float)(360 - value);
            }
            else if (filter == "brightness")
            {
                brightness = (float)value;
            }
            else if (filter == "contrast")
            {
                contrast = (float)value;
            }
            if (main.Children.Count > 0 && main.Children[0] is CanvasControl)
            {
                CanvasControl canvas = main.Children[0] as CanvasControl;
                canvas.Invalidate();
            }
            debounceEffect.Debounce(1000, () =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    //if (img.IsAnimatedBitmap) img.Play();
                    canScreenshot = true;
                    parent.changeNativeWallpaper(null);
                });
            });
        }

        public void changeVolume(double value)
        {
            volume = value;
            if (_value == null) return;
            if (_value.GetType() == typeof(Microsoft.UI.Xaml.Controls.MediaPlayerElement))
            {
                MediaPlayerElement me = (MediaPlayerElement)_value;
                if (me.MediaPlayer != null)
                {
                    me.MediaPlayer.Volume = value / 100;
                }
            }
        }
    }
}
