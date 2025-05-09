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
                if (!coreinit)
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
                }
            };
            async void a()
            {
                CoreWebView2EnvironmentOptions opt = new CoreWebView2EnvironmentOptions();
                string args = "--disable-features=HardwareMediaKeyHandling ";
                //update v1.2.17 (1/05/2025): NOW DEPRECATED
                //if (renderer == 1) args += "--use-angle=gl ";
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
                for(int i = 0; i < 5; i++)
                {
                    //MessageBox.Show("a");
                    parent.log("attempt " + i + " " + (i == 4));
                    try
                    {
                        await webview.EnsureCoreWebView2Async(await CoreWebView2Environment.CreateAsync(options: opt, userDataFolder: data));
                        //break loop because it's working
                        break;
                    }
                    catch (Exception ex)
                    {
                        //wait for EnsureCoreWebView2Async works multiple time
                        //update v1.2.16 (10/04/2024): + 1 is mandatory
                        await Task.Delay((parent.startAfter + 1) * 200);
                        if (i == 4)
                        {
                            MessageBox.Show("Error when launching Edge WebView2 after 5 times\n" + ex);
                            parent.Close();
                        }
                    }
                }
            }
            a();
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
            webview.CoreWebView2.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Normal;
            curUrl = newUrl;
            string isMainScreen = System.Windows.Forms.Screen.PrimaryScreen.DeviceName == parent.moni ? "true" : "false";
            parent.log(newUrl + " " + System.IO.Path.GetExtension(newUrl));
            foreach (string ext in App.types.Keys)
            {
                if (App.types[ext].Contains(System.IO.Path.GetExtension(newUrl)))
                {
                    if (ext == "Video files")
                    {
                        if (coreinit) evaluateJS(@"changeUrl('" + newUrl.Replace("\\", "\\\\").Replace("\'", "\\'") + "', true, " + volume + ", " + isMainScreen + ")");
                        webview.CoreWebView2.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Low;
                    }
                    if(ext == "Image files")
                    {
                        if (coreinit) evaluateJS(@"changeUrl('" + newUrl.Replace("\\", "\\\\").Replace("\'", "\\'") + "', false, " + volume + ", " + isMainScreen + ")");
                        webview.CoreWebView2.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Low;
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
            try
            {
                string np = play ? "true" : "false";
                if (coreinit)
                {
                    webview.CoreWebView2.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Normal;
                    await webview.ExecuteScriptAsync(@"changePlayerState(" + np + ")");
                    /*if (!play)
                    {
                        webview.CoreWebView2.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Low;
                    }
                    else
                    {
                        webview.CoreWebView2.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Normal;
                    }*/
                    webview.CoreWebView2.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Low;
                }
            }
            catch(Exception e)
            {
                parent.log(e + "");
            }
        }

        public async void changeFilter(string filter, double value)
        {
            if(coreinit) await webview.ExecuteScriptAsync(@"changeFilter('" + filter + "', " + value + ")");
        }
    }
}
