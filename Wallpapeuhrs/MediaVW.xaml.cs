using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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
using Wallpapeuhrs.Utils;
using Windows.Media;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.System.Display;

namespace Wallpapeuhrs
{
    /// <summary>
    /// Logique d'interaction pour Media.xaml
    /// </summary>
    public partial class MediaVW : UserControl
    {
        public double volume;
        public bool repeat;
        public float nextChange = 0;
        public WPBG parent;
        bool coreinit = false;

        public MediaVW()
        {
            //Windows.UI.Xaml.Hosting.WindowsXamlManager.InitializeForCurrentThread();
            InitializeComponent();
            webview.CoreWebView2InitializationCompleted += (s, e) =>
            {
                coreinit = true;
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "Wallpapeuhrs.Resources.web.index.html";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    //if (System.Windows.Forms.Screen.PrimaryScreen.DeviceName == parent.moni) webview.CoreWebView2.OpenDevToolsWindow();
                    string result = reader.ReadToEnd();
                    CoreWebView2WebResourceRequest r = webview.CoreWebView2.Environment.CreateWebResourceRequest("file:///C:/", "GET", null, "");
                    webview.NavigationStarting += async (s, e) =>
                    {
                        await webview.CoreWebView2.ExecuteScriptAsync("document.write(`" + result + "`)");
                        MainWindow.sendData(parent.tcp, "READY " + parent.moni + " ", null);
                        webview.Visibility = Visibility.Visible;
                        SystemMediaTransportControls.GetForCurrentView().IsEnabled = false;
                        setDWM(new WindowInteropHelper(parent).Handle);
                    };
                    webview.CoreWebView2.NavigateWithWebResourceRequest(r);
                    webview.CoreWebView2.AddHostObjectToScript("boundobject", new ChromeBoundObject(parent));
                }
            };
            async void a()
            {
                CoreWebView2EnvironmentOptions opt = new CoreWebView2EnvironmentOptions();
                opt.AdditionalBrowserArguments = "--disable-features=HardwareMediaKeyHandling";
                await webview.EnsureCoreWebView2Async(await CoreWebView2Environment.CreateAsync(options: opt));
            }
            a();
            SystemMediaTransportControls.GetForCurrentView().IsEnabled = false;
            //  setDWM(myHostControl.Handle);
            /*main = new Windows.UI.Xaml.Controls.Grid();
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

        private void setDWM(IntPtr handle)
        {
            int a = 1;
            int b = 0;
            W32.DwmSetWindowAttribute(handle, W32.DWMWINDOWATTRIBUTE.DisallowPeek, ref a, Marshal.SizeOf(typeof(int)));
            W32.DwmSetWindowAttribute(handle, W32.DWMWINDOWATTRIBUTE.AllowNCPaint, ref b, Marshal.SizeOf(typeof(int)));
            W32.DwmSetWindowAttribute(handle, W32.DWMWINDOWATTRIBUTE.ExcludedFromPeek, ref a, Marshal.SizeOf(typeof(int)));
            W32.DwmSetWindowAttribute(handle, W32.DWMWINDOWATTRIBUTE.DWMWA_USE_HOSTBACKDROPBRUSH, ref b, Marshal.SizeOf(typeof(int)));
            //W32.DwmSetWindowAttribute(myHostControl.Handle, W32.DWMWINDOWATTRIBUTE.Cloak, ref c, Marshal.SizeOf(typeof(int)));
            IntPtr exStyle = W32.GetWindowLongPtr(handle, W32.GWL_EXSTYLE);
            W32.SetWindowLongPtr(handle, W32.GWL_EXSTYLE, (IntPtr)(exStyle.ToInt64() | W32.WS_EX_LAYERED));
            W32.DwmEnableComposition(W32.CompositionAction.DWM_EC_DISABLECOMPOSITION);
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
                    if (ext == "Video files")
                    {
                        if (coreinit) await webview.ExecuteScriptAsync(@"changeUrl('" + newUrl.Replace("\\", "\\\\") + "', true, " + volume + ")");
                    }
                    if(ext == "Image files")
                    {
                        if (coreinit) await webview.ExecuteScriptAsync(@"changeUrl('" + newUrl.Replace("\\", "\\\\") + "', false, " + volume + ")");
                    }
                    return;
                }
            }
        }

        public async void changePlayerState(bool play)
        {
            string np = play ? "true" : "false";
            if (coreinit) await webview.ExecuteScriptAsync(@"changePlayerState(" + np + ")");
        }

        public async void resetVideoTime()
        {
            if (coreinit) await webview.ExecuteScriptAsync(@"resetVideoTime()");
        }

        public async void changeFilter(string filter, double value)
        {
            if(coreinit) await webview.ExecuteScriptAsync(@"changeFilter('" + filter + "', " + value + ")");
        }
    }
}
