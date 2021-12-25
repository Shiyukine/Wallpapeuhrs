using CefSharp;
using CefSharp.WinForms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CefTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            var settings = new CefSettings();
            //settings.CachePath = userFolder + "Cache\\WebCache\\";
            settings.PersistSessionCookies = true;
            settings.PersistUserPreferences = true;
            //settings.BrowserSubprocessPath = appFolder + "Anime Hub CEF.exe";
            settings.LogSeverity = LogSeverity.Warning;
            //settings.SetOffScreenRenderingBestPerformanceArgs();
            settings.CefCommandLineArgs.Add("autoplay-policy", "no-user-gesture-required");
            settings.CefCommandLineArgs.Add("enable-media-stream");
            //settings.CefCommandLineArgs.Add("disable-direct-composition");
            settings.CefCommandLineArgs.Add("disable-plugins-discovery");
            settings.CefCommandLineArgs.Add("disable-pdf-extension");
            settings.CefCommandLineArgs.Add("disable-extensions");
            //if (!AppInfo.sf.settingExists("Use_Proxy_Server")) settings.CefCommandLineArgs.Add("no-proxy-server");
            settings.CefCommandLineArgs.Add("disable-gpu-vsync");
            settings.CefCommandLineArgs.Add("disable-renderer-accessibility");
            //settings.CefCommandLineArgs.Add("allow-file-access-from-files");
            Cef.Initialize(settings, performDependencyCheck: true, browserProcessHandler: null);
            InitializeComponent();
            ChromiumWebBrowser cwb = new ChromiumWebBrowser("file:///E:/index.html");
            //cwb.Dock = DockStyle.Fill;
            //cwb.Load("file:///E:/index.html");
            cwb.IsBrowserInitializedChanged += (sender, e) =>
            {
                cwb.ShowDevTools();
            };
            Controls.Add(cwb);
        }
    }
}
