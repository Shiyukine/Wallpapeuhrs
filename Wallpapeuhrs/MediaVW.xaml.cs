using Microsoft.Web.WebView2.Core;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using Wallpapeuhrs.Utils;
using System.Web;

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
        private WPBG parent;
        private int renderer;
        bool coreinit = false;

        public MediaVW(WPBG parent, int renderer)
        {
            this.parent = parent;
            this.renderer = renderer;
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
                    webview.NavigationCompleted += async (s, e) =>
                    {
                        webview.CoreWebView2.AddHostObjectToScript("boundobject", new ChromeBoundObject(parent));
                        await webview.CoreWebView2.ExecuteScriptAsync("document.write(`" + result + "`)");
                        MainWindow.sendData(parent.tcp, "READY " + parent.moni + " ", null);
                        webview.Visibility = Visibility.Visible;
                        //SystemMediaTransportControls.GetForCurrentView().IsEnabled = false;
                        setDWM(new WindowInteropHelper(parent).Handle);
                    };
                    webview.Source = new Uri("file:///C:/");
                }
            };
            async void a()
            {
                CoreWebView2EnvironmentOptions opt = new CoreWebView2EnvironmentOptions();
                string args = "--disable-features=HardwareMediaKeyHandling ";
                if (renderer == 1) args += "--use-angle=gl ";
                if (renderer == 2) args += "--use-angle=d3d11 ";
                if (renderer == 3) args += "--use-angle=d3d11on12 ";
                args += "--disable-gpu-vsync ";
                args += "--disable-web-security ";
                opt.AdditionalBrowserArguments = args;
                /*actual 
                 * opt.AdditionalBrowserArguments = "--disable-features=HardwareMediaKeyHandling " +
                    "--use-angle=gl " +
                    "--disable-gpu-vsync " +
                    "--disable-renderer-accessibility " +
                    "--enable-media-stream " +
                    "--enable-begin-frame-scheduling " +
                    "--enable-gpu-rasterization " +
                    "--disallow-non-exact-resource-reuse " +
                    "--disable-d3d11 " +
                    "--max-gum-fps=\"60\"";
                /* avant
                opt.AdditionalBrowserArguments = "--flag-switches-begin " +
                    "--disable-features=HardwareMediaKeyHandling " +
                    //"--use-angle=gl " +
                    "--use-angle=d3d11on12 " +
                    //"--ignore-gpu-blocklist " +
                    //"--disable-zero-copy " +
                    "--disable-gpu-rasterization " +
                    //"--disable-accelerated-video-encode " +
                    //"--disable-accelerated-video-decode " +
                    "--disable-accelerated-2d-canvas " +
                    "--flag-switches-end";*/
                string data = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Wallpapeuhrs\\WebView2\\";
                try
                {
                    //wait for EnsureCoreWebView2Async works multiple time
                    await Task.Delay(parent.startAfter * 200);
                    await webview.EnsureCoreWebView2Async(await CoreWebView2Environment.CreateAsync(options: opt, userDataFolder: data));
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Error\n" + ex);
                    parent.Close();
                }
            }
            a();
            //SystemMediaTransportControls.GetForCurrentView().IsEnabled = false;
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
                        if (coreinit) evaluateJS(@"changeUrl('" + newUrl.Replace("\\", "\\\\").Replace("\'", "\\'") + "', true, " + volume + ")");
                    }
                    if(ext == "Image files")
                    {
                        if (coreinit) evaluateJS(@"changeUrl('" + newUrl.Replace("\\", "\\\\").Replace("\'", "\\'") + "', false, " + volume + ")");
                    }
                    return;
                }
            }
        }

        private async void evaluateJS(string js)
        {
            await webview.ExecuteScriptAsync(js);
        }

        public async void changePlayerState(bool play)
        {
            string np = play ? "true" : "false";
            if (coreinit) await webview.ExecuteScriptAsync(@"changePlayerState(" + np + ")");
        }

        public async void changeFilter(string filter, double value)
        {
            if(coreinit) await webview.ExecuteScriptAsync(@"changeFilter('" + filter + "', " + value + ")");
        }
    }
}
