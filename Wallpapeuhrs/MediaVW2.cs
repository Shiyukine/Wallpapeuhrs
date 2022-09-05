using CefSharp;
using CefSharp.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Wallpapeuhrs.Utils;

namespace Wallpapeuhrs
{
    public partial class MediaVW2 : UserControl
    {
        public double volume;
        public bool repeat;
        public float nextChange = 0;
        public WPBGForm parent;
        bool coreinit = false;

        public MediaVW2()
        {
            //Windows.UI.Xaml.Hosting.WindowsXamlManager.InitializeForCurrentThread();
            var settings = new CefSettings();
            settings.CachePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Wallpapeuhrs\\CEF\\";
            settings.PersistSessionCookies = true;
            settings.PersistUserPreferences = true;
            //settings.BrowserSubprocessPath = appFolder + "AyMusic.exe";
            settings.LogSeverity = LogSeverity.Warning;
            //settings.SetOffScreenRenderingBestPerformanceArgs();
            settings.CefCommandLineArgs.Add("autoplay-policy", "no-user-gesture-required");
            settings.CefCommandLineArgs.Add("enable-media-stream");
            settings.CefCommandLineArgs.Add("disable-direct-composition");
            settings.CefCommandLineArgs.Add("disable-plugins-discovery");
            settings.CefCommandLineArgs.Add("disable-pdf-extension");
            settings.CefCommandLineArgs.Add("disable-extensions");
            settings.CefCommandLineArgs.Add("disable-features", "HardwareMediaKeyHandling");
            settings.CefCommandLineArgs.Add("use-angle", "d3d11");
            //settings.CefCommandLineArgs.Add("disable-d3d11");
            //if (!AppInfo.sf.settingExists("Use_Proxy_Server")) settings.CefCommandLineArgs.Add("no-proxy-server");
            //settings.CefCommandLineArgs.Add("disable-gpu-vsync");
            CefSharpSettings.ConcurrentTaskExecution = true;
            //settings.CefCommandLineArgs.Add("disable-renderer-accessibility");
            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
            InitializeComponent();
            cwb.LoadingStateChanged += (s, e) =>
            {
                BeginInvoke(new Action(() =>
                {
                    try
                    {
                        if (!coreinit && !e.IsLoading && !cwb.IsLoading)
                        {
                            coreinit = true;
                            var assembly = Assembly.GetExecutingAssembly();
                            var resourceName = "Wallpapeuhrs.Resources.web.index.html";

                            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                //if (System.Windows.Forms.Screen.PrimaryScreen.DeviceName == parent.moni) cwb.ShowDevTools();
                                string result = reader.ReadToEnd();
                                CefSharpSettings.LegacyJavascriptBindingEnabled = true;
                                CefSharpSettings.ConcurrentTaskExecution = true;
                                cwb.JavascriptObjectRepository.Register("boundobject", new ChromeBoundObjectForms(parent), true, BindingOptions.DefaultBinder);
                                cwb.GetMainFrame().ExecuteJavaScriptAsync("document.write(`" + result + "`)");
                                MainWindow.sendData(parent.tcp, "READY " + parent.moni + " ", null);
                                cwb.Visible = true;
                                //SystemMediaTransportControls.GetForCurrentView().IsEnabled = false;
                                setDWM(this.Handle);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("a\n" + ex);
                    }
                }));
            };
            cwb.Load("file:///C:/");
            /*async void a()
            {
                CoreWebView2EnvironmentOptions opt = new CoreWebView2EnvironmentOptions();
                /*opt.AdditionalBrowserArguments = "--disable-features=HardwareMediaKeyHandling " +
                    "--use-angle=d3d11on12 " +
                    "--disable-gpu-vsync " +
                    "--disable-renderer-accessibility " +
                    "--enable-media-stream " +
                    "--enable-begin-frame-scheduling " +
                    "--enable-gpu-rasterization " +
                    "--disallow-non-exact-resource-reuse " +
                    "--disable-d3d11 " +
                    "--disable-accelerated-2d-canvas " +
                    "--max-gum-fps=\"60\"";
                /*actual 
                opt.AdditionalBrowserArguments = "--disable-features=HardwareMediaKeyHandling " +
                    "--use-angle=gl " +
                    "--disable-gpu-vsync " +
                    "--disable-renderer-accessibility " +
                    "--enable-media-stream " +
                    "--enable-begin-frame-scheduling " +
                    "--disable-gpu-program-cache " +
                    "--force-gpu-mem-available-mb=1 " +
                    "--gpu-program-cache-size-kb=1 " +
                    "--enable-gpu-rasterization " +
                    "--disallow-non-exact-resource-reuse " +
                    "--disable-d3d11 " +
                    "--max-gum-fps=1";
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
                    "--flag-switches-end";
                string data = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Wallpapeuhrs\\WebView2\\";
                await webview.EnsureCoreWebView2Async(await CoreWebView2Environment.CreateAsync(options: opt, userDataFolder: data));
            }
            a();*/
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
            //IntPtr exStyle = W32.GetWindowLongPtr(handle, W32.GWL_EXSTYLE);
            //W32.SetWindowLongPtr(handle, W32.GWL_EXSTYLE, (IntPtr)(exStyle.ToInt64() | W32.WS_EX_LAYERED));
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
                        if (coreinit) cwb.ExecuteScriptAsync(@"changeUrl('" + newUrl.Replace("\\", "\\\\").Replace("\'", "\\'") + "', true, " + volume + ")");
                    }
                    if (ext == "Image files")
                    {
                        if (coreinit) cwb.ExecuteScriptAsync(@"changeUrl('" + newUrl.Replace("\\", "\\\\").Replace("\'", "\\'") + "', false, " + volume + ")");
                    }
                    return;
                }
            }
        }

        public void changePlayerState(bool play)
        {
            string np = play ? "true" : "false";
            if (coreinit) cwb.ExecuteScriptAsync(@"changePlayerState(" + np + ")");
        }

        public void changeFilter(string filter, double value)
        {
            if (coreinit) cwb.ExecuteScriptAsync(@"changeFilter('" + filter + "', " + value + ")");
        }
    }
}
