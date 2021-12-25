using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Graphics.Effects;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.System.Display;
using Windows.UI.Composition;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

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
        public WPBG parent;
        Windows.UI.Xaml.Controls.Grid main;
        Dictionary<string, BitmapCache> caches = new Dictionary<string, BitmapCache>();
        Dictionary<Windows.UI.Xaml.Controls.Canvas, MediaPlayer> canva = new Dictionary<Windows.UI.Xaml.Controls.Canvas, MediaPlayer>();

        public Media()
        {
            InitializeComponent();
            myHostControl.ChildChanged += (sender, e) =>
            {
                main = myHostControl.GetUwpInternalObject() as Windows.UI.Xaml.Controls.Grid;
            };
            /*Windows.UI.Xaml.Hosting.WindowsXamlManager.InitializeForCurrentThread();
            main = new Windows.UI.Xaml.Controls.Grid();
            main.CanBeScrollAnchor = false;
            main.ReleasePointerCaptures();
            main.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
            main.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch;
            main.IsHitTestVisible = false;
            main.AllowFocusOnInteraction = false;
            IsHitTestVisible = false;
            main.AllowFocusWhenDisabled = false;
            main.IsTapEnabled = main.IsRightTapEnabled = main.IsDoubleTapEnabled = main.IsHoldingEnabled = false;
            myHostControl.Child = main;
            main.ManipulationMode = Windows.UI.Xaml.Input.ManipulationModes.None;*/
        }

        private MediaPlayer _media = null;
        private MediaPlayer curMedia
        {
            get
            {
                return _media;
            }
            set
            {
                try
                {
                    SystemMediaTransportControls.GetForCurrentView().IsEnabled = false;
                    double v = volume;
                    if (v == 100) v = 99.4;
                    //value.SetMediaPlayer(mp);
                    //
                    Windows.UI.Xaml.Controls.Canvas _compositionCanvas = new Windows.UI.Xaml.Controls.Canvas();
                    main.Children.Insert(0, _compositionCanvas);
                    canva.Add(_compositionCanvas, value);
                    //
                    value.SystemMediaTransportControls.IsEnabled = false;
                    value.CommandManager.IsEnabled = false;
                    value.AutoPlay = true;
                    value.IsLoopingEnabled = true;
                    //value.Stretch = Windows.UI.Xaml.Media.Stretch.UniformToFill;
                    value.MediaOpened += async (sender, eee) =>
                    {
                        remove(value);
                        //
                        try
                        {
                            await main.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                            {
                                try
                                {
                                    value.Play();
                                    _compositionCanvas.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Stretch;
                                    _compositionCanvas.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Stretch;
                                    value.SetSurfaceSize(new Windows.Foundation.Size(_compositionCanvas.ActualWidth, _compositionCanvas.ActualHeight));

                                    var compositor = Windows.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(myHostControl.Child).Compositor;
                                    MediaPlayerSurface surface = value.GetSurface(compositor);

                                    SpriteVisual spriteVisual = compositor.CreateSpriteVisual();
                                    spriteVisual.Size =
                                        new System.Numerics.Vector2((float)_compositionCanvas.ActualWidth, (float)_compositionCanvas.ActualHeight);

                                    CompositionBrush brush = compositor.CreateSurfaceBrush(surface.CompositionSurface);
                                    spriteVisual.Brush = brush;

                                    ContainerVisual container = compositor.CreateContainerVisual();
                                    container.Children.InsertAtTop(spriteVisual);

                                    Windows.UI.Xaml.Hosting.ElementCompositionPreview.SetElementChildVisual(_compositionCanvas, container);
                                }
                                catch (Exception err)
                                {
                                    MessageBox.Show(err.ToString());
                                }
                            });
                        }
                        catch (Exception err)
                        {
                            MessageBox.Show(err.ToString());
                        }
                    };
                    _media = value;
                    value.MediaFailed += async (sender, e) =>
                    {
                        await main.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
                        {
                            if (value != null)
                            {
                                Uri source = (value.Source as MediaSource).Uri;
                                string file = source.OriginalString != string.Empty ? source.OriginalString : source.AbsolutePath;
                                MessageBox.Show("Unable to launch or read the media. Please verify the media's path and verify if it's an video (not a .webm).\nFile : " + file + "\n\nIntern error :\n" + e.Error.ToString() + "\n" + e.ExtendedErrorCode, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            }
                        });
                    };
                    value.Volume = v / 100;
                }
                catch (Exception e)
                {
                    MessageBox.Show("Media.xaml:\n" + e.ToString());
                }
            }
        }

        public Windows.UI.Xaml.Media.Animation.DoubleAnimation addDoubleAnimation(Windows.UI.Xaml.UIElement el, TimeSpan time, double? from, double? to, string property)
        {
            Windows.UI.Xaml.Media.Animation.DoubleAnimation da = new Windows.UI.Xaml.Media.Animation.DoubleAnimation();
            da.From = from;
            da.To = to;
            da.Duration = new Windows.UI.Xaml.Duration(time);
            Windows.UI.Xaml.Media.Animation.Storyboard.SetTarget(da, el);
            Windows.UI.Xaml.Media.Animation.Storyboard.SetTargetProperty(da, property);
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
            foreach (string ext in App.types.Keys)
            {
                parent.log("aaaaaaab " + ext);
                if (App.types[ext].Contains(System.IO.Path.GetExtension(newUrl)))
                {
                    parent.log("aaaaaaac " + ext);
                    if (ext == "Video files")
                    {
                        MediaPlayer me = new MediaPlayer();
                        curMedia = me;
                        //me.CacheMode = setCache(newUrl);
                        me.Source = MediaSource.CreateFromUri(new Uri(newUrl));
                    }
                    if (ext == "Image files")
                    {
                        Windows.UI.Xaml.Controls.Image me = new Windows.UI.Xaml.Controls.Image();
                        me.Stretch = Stretch.UniformToFill;
                        me.HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment.Center;
                        me.VerticalAlignment = Windows.UI.Xaml.VerticalAlignment.Center;
                        me.CacheMode = setCache(newUrl);
                        Windows.UI.Xaml.Media.Imaging.BitmapImage img = new Windows.UI.Xaml.Media.Imaging.BitmapImage();
                        img.ImageOpened += (sender, e) =>
                        {
                            remove(null);
                        };
                        img.ImageFailed += async (sender, e) =>
                        {
                            await main.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.High, () =>
                            {
                                Uri source = img.UriSource;
                                string file = source.OriginalString != string.Empty ? source.OriginalString : source.AbsolutePath;
                                MessageBox.Show("Unable to launch or read the image. Please verify the image's path and verify if it's an image.\nFile : " + file + "\n\nIntern error :\n" + e.ErrorMessage + "\n" + e.OriginalSource.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            });
                        };
                        img.AutoPlay = true;
                        img.UriSource = new Uri(newUrl);
                        me.Source = img;
                        main.Children.Insert(0, me);
                        if (img.IsAnimatedBitmap) img.Play();
                    }
                    return;
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
                            if (play) curMedia.Play();
                            else curMedia.Pause();
                        }
                    }
                    if (ext == "Image files")
                    {
                        Windows.UI.Xaml.Controls.Image med = (Windows.UI.Xaml.Controls.Image)main.Children[0];
                        Windows.UI.Xaml.Media.Imaging.BitmapImage img = med.Source as Windows.UI.Xaml.Media.Imaging.BitmapImage;
                        if (img.IsAnimatedBitmap)
                        {
                            if (play) img.Play();
                            else img.Stop();
                        }
                    }
                    return;
                }
            }
        }

        private async void remove(object value)
        {
            await main.Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                for (int index = 0; index < main.Children.Count; index++)
                {
                    if (index > 0 && main.Children[index].Opacity == 1)
                    {
                        if (main.Children[index].GetType() == typeof(Windows.UI.Xaml.Controls.Canvas))
                        {
                            Windows.UI.Xaml.Controls.Canvas can = (Windows.UI.Xaml.Controls.Canvas)main.Children[index];
                            MediaPlayer med = canva[can];
                            Windows.UI.Xaml.Media.Animation.Storyboard sb = new Windows.UI.Xaml.Media.Animation.Storyboard();
                            sb.Children.Add(addDoubleAnimation(can, TimeSpan.FromMilliseconds(1000), 1, 0, "Opacity"));
                            sb.Completed += (sendere, ee) =>
                            {
                                parent.log("uwu2");
                                try
                                {
                                    main.Children.Remove(can);
                                    canva.Remove(can);
                                    if (med.Source != null)
                                    {
                                        (med.Source as MediaSource).Dispose();
                                        med.Source = null;
                                    }
                                    if (med != null)
                                    {
                                        med.Pause();
                                        med.Source = null;
                                        med.Dispose();
                                    }
                                    Windows.UI.Xaml.Hosting.ElementCompositionPreview.SetElementChildVisual(can, null);
                                    can = null;
                                    //med.CacheMode = null;
                                    med = null;
                                    GC.Collect();
                                    GC.WaitForPendingFinalizers();
                                    GC.Collect();
                                    GC.WaitForPendingFinalizers();
                                    if (value != null) _media = value as MediaPlayer;
                                    else _media = null;
                                }
                                catch (Exception e)
                                {
                                    MessageBox.Show("Storyboard end:\n" + e.ToString());
                                }
                            };
                            sb.Begin();
                        }
                        if (main.Children[index].GetType() == typeof(Windows.UI.Xaml.Controls.Image))
                        {
                            Windows.UI.Xaml.Controls.Image med = (Windows.UI.Xaml.Controls.Image)main.Children[index];
                            Windows.UI.Xaml.Media.Animation.Storyboard sb = new Windows.UI.Xaml.Media.Animation.Storyboard();
                            sb.Children.Add(addDoubleAnimation(med, TimeSpan.FromMilliseconds(1000), 1, 0, "Opacity"));
                            sb.Completed += (sendere, ee) =>
                            {
                                parent.log("uwu3");
                                try
                                {
                                    if (med.Source != null)
                                    {
                                        Windows.UI.Xaml.Media.Imaging.BitmapImage img = med.Source as Windows.UI.Xaml.Media.Imaging.BitmapImage;
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
                                    MessageBox.Show("Storyboard end:\n" + e.ToString());
                                }
                            };
                            sb.Begin();
                        }
                    }
                }
            });
        }
    }
}
