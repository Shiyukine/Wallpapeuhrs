using Microsoft.Graphics.Canvas;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Windows.Graphics.DirectX.Direct3D11;
using Windows.Graphics.Imaging;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using Windows.System.Display;

namespace Wallpapeuhrs
{
    /// <summary>
    /// Logique d'interaction pour Media.xaml
    /// </summary>
    public partial class Media : UserControl
    {
        public double volume;
        public bool repeat;
        public float nextChange = 0;
        private WPBG parent;
        Dictionary<string, BitmapCache> caches = new Dictionary<string, BitmapCache>();
        bool canScreenshot = true;

        public Media(WPBG parent)
        {
            this.parent = parent;
            InitializeComponent();
        }

        public void init()
        {
        }

        private Microsoft.UI.Xaml.Controls.MediaPlayerElement _media = null;
        private Microsoft.UI.Xaml.Controls.MediaPlayerElement curMedia
        {
            get
            {
                return _media;
            }
            set
            {
                try
                {
                    double v = volume;
                    value.AutoPlay = true;
                    value.MediaPlayer.IsLoopingEnabled = true;
                    value.MediaPlayer.Volume = v / 100;
                    main.Children.Insert(0, value);
                    value.Stretch = Microsoft.UI.Xaml.Media.Stretch.UniformToFill;
                    value.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
                    (value as Microsoft.UI.Xaml.Controls.MediaPlayerElement).AreTransportControlsEnabled = false;
                    (value as Microsoft.UI.Xaml.Controls.MediaPlayerElement).MediaPlayer.SystemMediaTransportControls.IsEnabled = false;
                    (value as Microsoft.UI.Xaml.Controls.MediaPlayerElement).MediaPlayer.CommandManager.IsEnabled = false;
                    value.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center;
                    value.MediaPlayer.MediaOpened += async (sender, eee) =>
                    {
                        remove(value);
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            parent.changeNativeWallpaper(null);
                        });
                    };
                    _media = value;
                    value.MediaPlayer.MediaFailed += async (sender, e) =>
                    {
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            if (value.MediaPlayer != null)
                            {
                                Uri source = (value.Source as MediaSource).Uri;
                                string file = source.OriginalString != string.Empty ? source.OriginalString : source.AbsolutePath;
                                System.Windows.MessageBox.Show("Unable to launch or read the media. Please verify the media's path and verify if it's an video.\nFile : " + file + "\n\nIntern error :\n" + e.Error.ToString() + "\n" + e.ExtendedErrorCode, "Wallpapeuhrs - Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                            }
                        });
                    };
                }
                catch (Exception e)
                {
                    System.Windows.MessageBox.Show("Media.xaml:\n" + e.ToString());
                }
            }
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

        public void changeUrl(string newUrl)
        {
            curUrl = newUrl;
            parent.log("aaaaaaaa " + newUrl + " " + System.IO.Path.GetExtension(newUrl));
            //SystemMediaTransportControls.GetForCurrentView().IsEnabled = false;
            foreach (string ext in App.types.Keys)
            {
                parent.log("aaaaaaab " + ext);
                if (App.types[ext].Contains(System.IO.Path.GetExtension(newUrl)))
                {
                    parent.log("aaaaaaac " + ext);
                    stopPlayers();
                    canScreenshot = true;
                    if (ext == "Video files")
                    {
                        Microsoft.UI.Xaml.Controls.MediaPlayerElement me = new Microsoft.UI.Xaml.Controls.MediaPlayerElement();
                        me.CacheMode = setCache(newUrl);
                        me.Source = MediaSource.CreateFromUri(new Uri(newUrl));
                        me.MediaPlayer.SystemMediaTransportControls.IsEnabled = false;
                        me.MediaPlayer.CommandManager.IsEnabled = false;
                        me.AreTransportControlsEnabled = false;
                        curMedia = me;
                    }
                    if(ext == "Image files")
                    {
                        Microsoft.UI.Xaml.Controls.Image me = new Microsoft.UI.Xaml.Controls.Image();
                        me.CacheMode = setCache(newUrl);
                        Microsoft.UI.Xaml.Media.Imaging.BitmapImage img = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                        me.Stretch = Stretch.UniformToFill;
                        me.VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment.Center;
                        me.HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment.Center;
                        if(main.Children.Count > 1) me.Opacity = 0;
                        img.ImageOpened += async (sender, e) =>
                        {
                            remove(me);
                            DispatcherQueue.TryEnqueue(() =>
                            {
                                if (img.IsAnimatedBitmap) img.Play();
                                parent.changeNativeWallpaper(null);
                            });
                        };
                        img.ImageFailed += async (sender, e) =>
                        {
                            DispatcherQueue.TryEnqueue(() =>
                            {
                                Uri source = img.UriSource;
                                string file = source.OriginalString != string.Empty ? source.OriginalString : source.AbsolutePath;
                                System.Windows.MessageBox.Show("Unable to launch or read the image. Please verify the image's path and verify if it's an image.\nFile : " + file + "\n\nIntern error :\n" + e.ErrorMessage + "\n" + e.OriginalSource.ToString(), "Wallpapeuhrs - Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                            });
                        };
                        main.Children.Insert(main.Children.Count == 0 ? 0 : 1, me);
                        img.AutoPlay = true;
                        img.UriSource = new Uri(newUrl);
                        me.Source = img;
                    }
                    return;
                }
            }
        }

        public void stopPlayers()
        {
            if(main.Children.Count == 0) return;
            if (main.Children[0].GetType() == typeof(Microsoft.UI.Xaml.Controls.MediaPlayerElement))
            {
                if (curMedia != null)
                {
                    curMedia.MediaPlayer.Pause();
                }
            }
            else if (main.Children[0].GetType() == typeof(Microsoft.UI.Xaml.Controls.Image))
            {
                Microsoft.UI.Xaml.Controls.Image med = (Microsoft.UI.Xaml.Controls.Image)main.Children[0];
                Microsoft.UI.Xaml.Media.Imaging.BitmapImage img = med.Source as Microsoft.UI.Xaml.Media.Imaging.BitmapImage;
                if (img.IsAnimatedBitmap)
                {
                    img.Stop();
                }
            }
        }

        public void changePlayerState(bool play)
        {
            foreach (string ext in App.types.Keys)
            {
                if (App.types[ext].Contains(System.IO.Path.GetExtension(curUrl)))
                {
                    if (ext == "Video files")
                    {
                        if (curMedia != null)
                        {
                            if(play) curMedia.MediaPlayer.Play();
                            else curMedia.MediaPlayer.Pause();
                        }
                    }
                    if (ext == "Image files")
                    {
                        Microsoft.UI.Xaml.Controls.Image med = (Microsoft.UI.Xaml.Controls.Image)main.Children[0];
                        Microsoft.UI.Xaml.Media.Imaging.BitmapImage img = med.Source as Microsoft.UI.Xaml.Media.Imaging.BitmapImage;
                        if(img.IsAnimatedBitmap)
                        {
                            if (play) img.Play();
                            else img.Stop();
                        }
                    }
                    return;
                }
            }
        }

        private async void remove(Microsoft.UI.Xaml.UIElement value)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if(value is Microsoft.UI.Xaml.Controls.MediaPlayerElement)
                {                   
                    (value as Microsoft.UI.Xaml.Controls.MediaPlayerElement).AreTransportControlsEnabled = false;
                    (value as Microsoft.UI.Xaml.Controls.MediaPlayerElement).MediaPlayer.SystemMediaTransportControls.IsEnabled = false;
                    (value as Microsoft.UI.Xaml.Controls.MediaPlayerElement).MediaPlayer.CommandManager.IsEnabled = false;
                    //(value as Microsoft.UI.Xaml.Controls.MediaPlayerElement).TransportControls.IsEnabled = false;
                    //SystemMediaTransportControls.GetForCurrentView().IsEnabled = false;
                    //SystemMediaTransportControls.GetForCurrentView().DisplayUpdater.Update();
                }
                Microsoft.UI.Xaml.Media.Animation.Storyboard sb = new Microsoft.UI.Xaml.Media.Animation.Storyboard();
                int index = 0;
                if (value is Microsoft.UI.Xaml.Controls.MediaPlayerElement && main.Children.Count > 1)
                {
                    sb.Children.Add(addDoubleAnimation(main.Children[1], TimeSpan.FromMilliseconds(1000), 1, 0, "Opacity"));
                    index = 1;
                }
                else if (value is Microsoft.UI.Xaml.Controls.Image && main.Children.Count > 1)
                {
                    sb.Children.Add(addDoubleAnimation(value, TimeSpan.FromMilliseconds(1000), 0, 1, "Opacity"));
                    index = 0;
                }
                if (main.Children.Count > 1)
                {
                    if (main.Children[index].GetType() == typeof(Microsoft.UI.Xaml.Controls.MediaPlayerElement))
                    {
                        Microsoft.UI.Xaml.Controls.MediaPlayerElement med = (Microsoft.UI.Xaml.Controls.MediaPlayerElement)main.Children[index];

                        sb.Completed += (sendere, ee) =>
                        {
                            parent.log("uwu2");
                            try
                            {
                                main.Children.Remove(med);
                                med.IsEnabled = false;
                                if (med.Source != null)
                                {
                                    (med.Source as MediaSource).Dispose();
                                    med.Source = null;
                                }
                                if (med.MediaPlayer != null)
                                {
                                    med.MediaPlayer.Pause();
                                    med.MediaPlayer.Source = null;
                                    med.MediaPlayer.Dispose();
                                    med.SetMediaPlayer(null);
                                }
                                //med.CacheMode = null;
                                med = null;
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                                if (value != null) _media = value as Microsoft.UI.Xaml.Controls.MediaPlayerElement;
                                else _media = null;
                            }
                            catch (Exception e)
                            {
                                System.Windows.MessageBox.Show("Storyboard end:\n" + e.ToString());
                            }
                        };
                    }
                    if (main.Children[index].GetType() == typeof(Microsoft.UI.Xaml.Controls.Image))
                    {
                        Microsoft.UI.Xaml.Controls.Image med = (Microsoft.UI.Xaml.Controls.Image)main.Children[index];
                        sb.Completed += (sendere, ee) =>
                        {
                            parent.log("uwu3");
                            try
                            {
                                if (med.Source != null)
                                {
                                    Microsoft.UI.Xaml.Media.Imaging.BitmapImage img = med.Source as Microsoft.UI.Xaml.Media.Imaging.BitmapImage;
                                    img.Stop();
                                    //img.SetSource(null);
                                }
                                med.Source = null;
                                //med.CacheMode = null;
                                main.Children.Remove(med);
                                med = null;
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                                GC.Collect();
                                GC.WaitForPendingFinalizers();
                            }
                            catch (Exception e)
                            {
                                System.Windows.MessageBox.Show("Storyboard end:\n" + e.ToString());
                            }
                        };
                    }
                    sb.Begin();
                }
                if(main.Children.Count > 2)
                {
                    for (int i = 2; i < main.Children.Count; i++)
                    {
                        main.Children.RemoveAt(i);
                    }
                }
            });
        }

        public async Task<MemoryStream> screenshot()
        {
            if(!canScreenshot) return null;
            canScreenshot = false;
            await Task.Delay(1000);
            parent.log("screenshot");
            foreach (string ext in App.types.Keys)
            {
                if (App.types[ext].Contains(System.IO.Path.GetExtension(curUrl)))
                {
                    if (ext == "Video files" && curMedia != null)
                    {
                        SoftwareBitmap softwareBitmap;
                        var frame = new SoftwareBitmap(BitmapPixelFormat.Rgba8,
                        (int)curMedia.ActualWidth, (int)curMedia.ActualHeight, BitmapAlphaMode.Ignore);
                        var canvasDevice = CanvasDevice.GetSharedDevice();
                        using (var inputBitmap = CanvasBitmap.CreateFromSoftwareBitmap(canvasDevice, frame))
                        {
                            curMedia.MediaPlayer.CopyFrameToVideoSurface(inputBitmap);

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
                    if (ext == "Image files")
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
                }
            }
            parent.log("No screenshot taken, no media found");
            return null;
        }

        public void changeVolume(double value)
        {
            volume = value;
            if (curMedia == null) return;
            MediaPlayerElement me = (MediaPlayerElement)curMedia;
            if (me.MediaPlayer != null)
            {
                me.MediaPlayer.Volume = value / 100;
            }
        }
    }
}
