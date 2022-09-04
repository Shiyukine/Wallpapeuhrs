using CefSharp;
using CefSharp.WinForms;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace WPBG2
{
    public partial class Form1 : Form
    {
        public static string appFolder;
        public static string userFolder;

        public Form1()
        {
            appFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\";
            userFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Wallpapeuhrs\\";
            var settings = new CefSettings();
            settings.CachePath = userFolder + "CEF\\";
            settings.PersistSessionCookies = true;
            settings.PersistUserPreferences = true;
            //settings.WindowlessRenderingEnabled = true;
            //settings.DisableGpuAcceleration();
            //settings.BrowserSubprocessPath = appFolder + "AyMusic.exe";
            settings.LogSeverity = LogSeverity.Warning;
            //settings.SetOffScreenRenderingBestPerformanceArgs();
            settings.CefCommandLineArgs.Add("autoplay-policy", "no-user-gesture-required");
            //settings.CefCommandLineArgs.Add("disable-direct-composition");
            //settings.CefCommandLineArgs.Add("enable-accelerated-2d-canvas");
            //settings.CefCommandLineArgs.Add("enable-accelerated-video-decode");
            //settings.CefCommandLineArgs.Add("enable-gpu-rasterization");
            //settings.CefCommandLineArgs.Add("enable-zero-copy");
            //settings.CefCommandLineArgs.Add("ignore-gpu-blocklist");
            //settings.CefCommandLineArgs.Add("use-angle", "d3d11on12");
            //settings.CefCommandLineArgs.Add("enable-accelerated-video-encode");
            //if (!AppInfo.sf.settingExists("Use_Proxy_Server")) settings.CefCommandLineArgs.Add("no-proxy-server");
            //settings.CefCommandLineArgs.Add("disable-gpu-vsync");
            CefSharpSettings.ConcurrentTaskExecution = true;
            //settings.CefCommandLineArgs.Add("disable-renderer-accessibility");
            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
            //Cef.RegisterWidevineCdm(appFolder + "Widevine");
            InitializeComponent();
            //ChromiumWebBrowser cwb = new ChromiumWebBrowser("chrome://version");
            ChromiumWebBrowser cwb = new ChromiumWebBrowser("E:\\Vidéos\\Wallpapers\\mes pref\\ninomae-inanis-torii-gate-desktop-wallpaperwaifu.com.mp4");
            Controls.Add(cwb);
            cwb.Dock = DockStyle.Fill;
            WindowState = FormWindowState.Maximized;
            cwb.FrameLoadEnd += (s, e) =>
            {
                BeginInvoke(new Action(() =>
                {
                    SuspendLayout();
                }));
            };
        }
    }
}