using Microsoft.Web.WebView2.Core;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack;
using Microsoft.WindowsAPICodePack.Dialogs;
using ShiyukiUtils.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Wallpapeuhrs.Styles;
using Wallpapeuhrs.Utils;
using Windows.ApplicationModel;
using Windows.Management.Deployment;

namespace Wallpapeuhrs
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static Dictionary<string, TcpClient> processes = new Dictionary<string, TcpClient>();
        TcpListener PStcp = null;
        static SettingsManager sf = null;
        bool ok = false;
        public static bool isclos = false;
        bool inbg = false;
        System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();
        static bool loaded = false;
        string curNativeWallpaper = "";
        bool settingsLoaded = false;
        static string userFolder = "";

        public MainWindow(bool inbg)
        {
            NameScope.SetNameScope(this, new NameScope());
            App.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            App.Current.MainWindow = this;
            InitializeComponent();
            RegisterName("MyAnimatedTransform", show_more_r);
            vname.Content = Update.getVersionName();
            changeScreenConfig(0);
            Update.searchUpdates();
            this.inbg = inbg;
            //
            var wih = new WindowInteropHelper(this);
            var hwnd = wih.EnsureHandle();
            _ScreenStateNotify = W32.RegisterPowerSettingNotification(hwnd, ref W32.GUID_CONSOLE_DISPLAY_STATE, W32.DEVICE_NOTIFY_WINDOW_HANDLE);
            _HwndSource = HwndSource.FromHwnd(hwnd);
            _HwndSource.AddHook(HwndHook);
            //SystemEvents.PowerModeChanged += OnPowerChange;
            //
            string newF = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Wallpapeuhrs\\";
            userFolder = newF;
            if (!inbg) Show();
            if (!Directory.Exists(newF)) Directory.CreateDirectory(newF);
            sf = new SettingsManager(newF + "Settings.cfg");
            loadSettings();
            settingsLoaded = true;
            //
            File.Create(newF + "latest.log").Close();
            //
            string dFonds = newF + "NativeWallpaper\\";
            if (!Directory.Exists(dFonds)) Directory.CreateDirectory(dFonds);
            curNativeWallpaper = NativeWallpaper.getCurrentDesktopWallpaper();
            if (curNativeWallpaper != null && curNativeWallpaper != dFonds + "thumb.png") sf.setSetting("NativeWallpaper", curNativeWallpaper, null);
            //
            System.Windows.Forms.ContextMenuStrip cm = new System.Windows.Forms.ContextMenuStrip();
            cm.BackColor = System.Drawing.Color.FromArgb(38, 38, 38);
            cm.Renderer = new MenuStripRenderer();
            Stream iconStream = Application.GetResourceStream(new Uri("/Wallpapeuhrs;component/Resources/Icon.ico", UriKind.RelativeOrAbsolute)).Stream;
            ni.Icon = new System.Drawing.Icon(iconStream);
            iconStream.Dispose();
            ni.Text = "Wallpapeuhrs";
            var mi3 = new System.Windows.Forms.ToolStripMenuItem() { Text = "Show app", ForeColor = System.Drawing.Color.White };
            mi3.Click += (s, ev) =>
            {
                if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
                ForceWindowIntoForeground(new WindowInteropHelper(this).Handle);
                inbg = false;
            };
            cm.Items.Add(mi3);
            var mi1 = new System.Windows.Forms.ToolStripMenuItem() { Text = "Play", ForeColor = System.Drawing.Color.White };
            mi1.Click += (s, ev) =>
            {
                changePlayerState(true);
            };
            cm.Items.Add(mi1);
            var mi2 = new System.Windows.Forms.ToolStripMenuItem() { Text = "Pause", ForeColor = System.Drawing.Color.White };
            mi2.Click += (s, ev) =>
            {
                changePlayerState(false);
            };
            cm.Items.Add(mi2);
            var mi0 = new System.Windows.Forms.ToolStripMenuItem() { Text = "Change wallpaper", ForeColor = System.Drawing.Color.White };
            mi0.Click += (s, ev) =>
            {
                beginWP();
            };
            cm.Items.Add(mi0);
            var mi = new System.Windows.Forms.ToolStripMenuItem() { Text = "Close", ForeColor = System.Drawing.Color.White };
            mi.Click += (s, ev) =>
            {
                isclos = true;
                Close();
            };
            cm.Items.Add(mi);
            ni.ContextMenuStrip = cm;
            ni.Visible = true;
            //
            foreach (ScrollViewer sv in FindVisualChildren<ScrollViewer>(g_main))
            {
                ScrollAnimationBehavior.SetTimeDuration(sv, TimeSpan.FromMilliseconds(150));
                sv.PreviewMouseWheel += ScrollAnimationBehavior.ScrollViewerPreviewMouseWheel;
            }
            //
            System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
            t.Interval = 1000;
            t.Tick += (sender, e) =>
            {
                if (loaded)
                {
                    try
                    {
                        Process[] pl = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
                        if (pl.Length - 1 > System.Windows.Forms.Screen.AllScreens.Count())
                        {
                            Dictionary<Process, string> list = new Dictionary<Process, string>();
                            foreach (Process p in pl)
                            {
                                string cmd = GetCommandLine(p);
                                if (cmd != null && cmd.Contains("/moni")) list.Add(p, cmd);
                            }
                            List<Process> pRem = new List<Process>(list.Keys);
                            List<System.Windows.Forms.Screen> sRem = new List<System.Windows.Forms.Screen>(System.Windows.Forms.Screen.AllScreens);
                            foreach (System.Windows.Forms.Screen s in System.Windows.Forms.Screen.AllScreens)
                            {
                                Process process = list.Where((x) => x.Value.Contains("/moni \"" + s.DeviceName + "\"")).FirstOrDefault().Key;
                                if (process != null)
                                {
                                    pRem.Remove(process);
                                    sRem.Remove(s);
                                }
                            }
                            bool haveKilledApp = false;
                            foreach (Process p in pRem)
                            {
                                p.Kill();
                                haveKilledApp = true;
                            }
                            if (haveKilledApp)
                            {
                                foreach (System.Windows.Forms.Screen s in sRem)
                                {
                                    processes.Remove(s.DeviceName);
                                    monis.Remove(s.DeviceName);
                                }
                                vidReady = 0;
                                foreach (string monii in processes.Keys)
                                {
                                    sendChange(monii, processes[monii], true);
                                }
                                refreshScreensConfig();
                            }
                        }
                        else if (pl.Length - 1 < System.Windows.Forms.Screen.AllScreens.Count())
                        {
                            beginWP();
                        }
                        else
                        {
                            foreach (System.Windows.Forms.Screen s in System.Windows.Forms.Screen.AllScreens)
                            {
                                if (monis.ContainsKey(s.DeviceName) && monis[s.DeviceName] != s.Bounds)
                                {
                                    beginWP();
                                    monis[s.DeviceName] = s.Bounds;
                                }
                            }
                        }
                    }
                    catch
                    {

                    }
                }
                if (curIndexEcoConfig == 1)
                {
                    List<string> list = new List<string>();
                    foreach (System.Windows.Forms.Screen s in System.Windows.Forms.Screen.AllScreens)
                    {
                        list.Add(s.DeviceName);
                        if (!playersStateEco.ContainsKey(s.DeviceName))
                            playersStateEco.Add(s.DeviceName, true);
                    }
                    IntPtr curProg = W32.GetForegroundWindow();
                    if (curProg != IntPtr.Zero)
                    {
                        if(!programs.Contains(curProg))
                        {
                            programs.Add(curProg);
                        }
                    }
                    foreach(IntPtr prog in programs)
                    {
                        System.Windows.Forms.Screen s = System.Windows.Forms.Screen.FromHandle(prog);
                        int length = W32.GetWindowTextLength(prog);
                        StringBuilder sb = new StringBuilder(length + 1);
                        W32.GetWindowText(prog, sb, sb.Capacity);
                        string aa = sb.ToString();
                        if (list.Contains(s.DeviceName) && 
                        aa != "WPBG" && 
                        aa != "Microsoft Text Input Application" && 
                        aa != "Task Switching" && 
                        aa != "Window" && 
                        aa != "" &&
                        aa != "Windows Default Lock Screen" &&
                        !aa.Contains("NVIDIA GeForce Overlay"))
                        {
                            W32.RECT rct;
                            W32.GetWindowRect(prog, out rct);
                            System.Drawing.Rectangle r = s.WorkingArea;
                            System.Drawing.Rectangle r2 = rct;
                            //test maximized 1
                            bool b1 = r == r2;
                            //test maximized 2
                            r2.X += 8;
                            r2.Y += 8;
                            r2.Width -= 16;
                            r2.Height -= 16;
                            bool b2 = r == r2;
                            //
                            if (b1 || b2)
                            {
                                list.Remove(s.DeviceName);
                                if (processes.ContainsKey(s.DeviceName) && playersStateEco[s.DeviceName])
                                {
                                    //MessageBox.Show(prog + " " + aa);
                                    addLog("EcoMode", prog + " " + aa);
                                    sendData(processes[s.DeviceName], "Pause", s.DeviceName);
                                    playersStateEco[s.DeviceName] = false;
                                }
                            }
                        }
                    }
                    foreach (string s in list)
                    {
                        if (processes.ContainsKey(s) && !playersStateEco[s])
                        {
                            sendData(processes[s], "Play", s);
                            playersStateEco[s] = true;
                        }
                    }
                }
                if (curIndexEcoConfig == 2)
                {
                    int window = W32.GetForegroundWindow().ToInt32();
                    //log("play " + ", contains " + !win.Contains(window) + ", diff win " + (anWin != window));
                    if (anWin != window)
                    {
                        if (!win.Contains(window))
                        {
                            changePlayerState(false);
                        }
                        else
                        {
                            changePlayerState(true);
                        }
                    }
                    anWin = window;
                }
            };
            t.Start();
            //
            /* Disabled
             * string a = "";
            foreach(Process p in Process.GetProcesses())
            {
                IntPtr curProg = p.MainWindowHandle;
                if (curProg != IntPtr.Zero)
                {
                    a += p.MainWindowTitle + "\n";
                    if (!programs.Contains(curProg))
                    {
                        programs.Add(curProg);
                    }
                }
            }
            MessageBox.Show(a);*/
            // 
            if (urls.Text != "") beginWP();
        }

        Dictionary<string, bool> playersStateEco = new Dictionary<string, bool>();
        List<IntPtr> programs = new List<IntPtr>();

        /*private void OnPowerChange(object sender, PowerModeChangedEventArgs e)
        {
            switch (e.Mode)
            {
                case PowerModes.Resume:
                    Debug.WriteLine(System.Environment.TickCount + " - PowerModes -> " + "Resume");
                    changePlayerState(true);
                    loaded = true;
                    break;

                case PowerModes.Suspend:
                    Debug.WriteLine(System.Environment.TickCount + " - PowerModes -> " + "Suspend");
                    break;
            }
        }*/

        List<int> win = new List<int>();
        int anWin = -1;
        Dictionary<string, System.Drawing.Rectangle> monis = new Dictionary<string, System.Drawing.Rectangle>();

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // handler of console display state system event 
            if(msg == 5250)
            {
                if (WindowState != WindowState.Maximized) WindowState = WindowState.Normal;
                IntPtr handle = new WindowInteropHelper(this).Handle;
                ForceWindowIntoForeground(handle);
                inbg = false;
            }
            if (msg == W32.WM_POWERBROADCAST)
            {
                if (wParam.ToInt32() == W32.PBT_POWERSETTINGCHANGE)
                {
                    var s = (W32.POWERBROADCAST_SETTING)Marshal.PtrToStructure(lParam, typeof(W32.POWERBROADCAST_SETTING));
                    if (s.PowerSetting == W32.GUID_CONSOLE_DISPLAY_STATE && ok)
                    {
                        Debug.WriteLine(System.Environment.TickCount + " - GUID_CONSOLE_DISPLAY_STATE -> " + s.Data);
                        if(s.Data == 1)
                        {
                            //Debug.WriteLine("Not in sleep mode");
                            if (!loaded)
                            {
                                changePlayerState(true);
                                loaded = true;
                            }
                        }
                        if(s.Data == 0)
                        {
                            //Debug.WriteLine("Sleep mode");
                            changePlayerState(false);
                            loaded = false;
                        }
                    }
                }
            }

            return IntPtr.Zero;
        }

        ~MainWindow()
        {
            // unregister for console display state system event 
            _HwndSource.RemoveHook(HwndHook);
            W32.UnregisterPowerSettingNotification(_ScreenStateNotify);
        }

        private HwndSource _HwndSource;
        private readonly IntPtr _ScreenStateNotify;

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            inbg = false;
            maxHeightExpand = show_more_expend.ActualHeight;
            show_more_expend.Height = 0;
            show_more_expend.Visibility = Visibility.Collapsed;
            await Update.searchUpdatesSilent();
            if (Update.haveUpdate)
            {
                vname.BackgroundHover = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFFFF500"));
                vname.Background = new SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#FFE0D700"));
                vname.Foreground = new SolidColorBrush(System.Windows.Media.Color.FromRgb(0,0,0));
                vname.ToolTip = "Update available - Click to install";
            }
        }

        bool stopping = false;

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //W32.SetParent(new WindowInteropHelper(this).Handle, WPBG.Form1.workerw);
            if (isclos)
            {
                string dFonds = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Wallpapeuhrs\\NativeWallpaper\\";
                if (sf.settingExists("NativeWallpaper") && sf.getStringSetting("NativeWallpaper") != "None")
                {
                    NativeWallpaper.changeWallpaper(sf.getStringSetting("NativeWallpaper"));
                    sf.setSetting("NativeWallpaper", "None", null);
                }
                stopping = true;
                if(PStcp != null) PStcp.Stop();
                foreach (string moni in processes.Keys)
                {
                    processes[moni].Close();
                    processes[moni].Dispose();
                }
                ni.Visible = false;
                ni.Dispose();
            }
            else
            {
                e.Cancel = true;
                Hide();
            }
        }

        void setSettings()
        {
            try
            {
                sf.setSetting("Path", urls.Text, null);
                sf.setSetting("Vol", Convert.ToInt32(vol.Text), null);
                sf.setSetting("Interval", Convert.ToInt32(interval.Text), null);
                sf.setSetting("Start", (bool)startwithw.IsChecked, null);
                sf.setSetting("EcoMode", curIndexEcoConfig, null);
                sf.setSetting("RestartExplorer", (bool)restartexplo.IsChecked, null);
                sf.setSetting("Edge_Engine", engine.SelectedIndex, null);
                sf.setSetting("FullRdm", (bool)fullrdm.IsChecked, null);
                foreach(FrameworkElement el in multiscreen_g.Children)
                {
                    if(el is ScreenConfig)
                    {
                        ScreenConfig config = (ScreenConfig)el;
                        sf.setSetting("Screen_" + config.screenName + "_url", config.urls.Text, null);
                        if(config.interval.Text != "") sf.setSetting("Screen_" + config.screenName + "_interval", Convert.ToInt32(config.interval.Text), null);
                        else sf.setSetting("Screen_" + config.screenName + "_interval", "", null);
                        if (config.vol.Text != "") sf.setSetting("Screen_" + config.screenName + "_vol", Convert.ToInt32(config.vol.Text), null);
                        else sf.setSetting("Screen_" + config.screenName + "_vol", "", null);
                    }
                }
                restart_dwm.Visibility = sf.getBoolSetting("RestartExplorer") ? Visibility.Visible : Visibility.Collapsed;
                filters.Visibility = sf.getIntSetting("Edge_Engine") <= 3 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch 
            {
                MessageBox.Show("A parameter does not correspond to what is requested. Please check all settings before saving.", "Wallpapeuhrs - Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void loadSettings()
        {
            if (!sf.settingExists("Path")) sf.setSetting("Path", "", null);
            if (!sf.settingExists("Vol")) sf.setSetting("Vol", 0, null);
            if (!sf.settingExists("Interval")) sf.setSetting("Interval", 60, null);
            if (!sf.settingExists("Start")) sf.setSetting("Start", true, null);
            if (!sf.settingExists("Repeat")) sf.setSetting("Repeat", true, null);
            if (!sf.settingExists("EcoMode")) sf.setSetting("EcoMode", 0, null);
            if (!sf.settingExists("FullRdm")) sf.setSetting("FullRdm", true, null);
            if (!sf.settingExists("RestartExplorer")) sf.setSetting("RestartExplorer", false, null);
            if (!sf.settingExists("Edge_Engine")) sf.setSetting("Edge_Engine", 0, null);
            //
            urls.Text = sf.getStringSetting("Path");
            vol.Text = sf.getStringSetting("Vol");
            interval.Text = sf.getStringSetting("Interval");
            startwithw.IsChecked = sf.getBoolSetting("Start");
            if (sf.settingExists("Stop"))
            {
                if (sf.getBoolSetting("Stop"))
                {
                    sf.setSetting("EcoMode", 2, null);
                }
                sf.removeSetting("Stop");
            }
            changeEcoConfig(sf.getIntSetting("EcoMode"));
            restartexplo.IsChecked = sf.getBoolSetting("RestartExplorer");
            fullrdm.IsChecked = sf.getBoolSetting("FullRdm");
            engine.SelectedIndex = sf.getIntSetting("Edge_Engine");
            restart_dwm.Visibility = sf.getBoolSetting("RestartExplorer") ? Visibility.Visible : Visibility.Collapsed;
            filters.Visibility = sf.getIntSetting("Edge_Engine") <= 3 ? Visibility.Visible : Visibility.Collapsed;
            //
            foreach (Grid g in filters.Children)
            {
                var slider = g.Children[1] as Slider;
                if (slider.Tag == null || (string)slider.Tag == "") MessageBox.Show("ID 01-466\n'" + slider.Tag + "'", "Wallpapeuhrs - Error", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                {
                    if (sf.settingExists("Theme_" + slider.Tag)) slider.Value = sf.getDoubleSetting("Theme_" + slider.Tag);
                }
            }
            //add settings loaded
        }

        //for explorer.exe
        bool alreadyRestarted = false;

        //for beginWP
        bool isAddingNewProcess = false;
        int vidReady = 0;
        bool oneIsDir = false;

        private async void beginWP()
        {
            try
            {
                //NativeWallpaper.changeWallpaper("");
                loaded = false;
                /*if (sf.getIntSetting("Edge_Engine") == 4)
                {
                    try
                    {
                        Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\VisualStudio\\14.0\\VC\\Runtimes\\X64", false);
                        if (key == null)
                        {
                            start.Visibility = Visibility.Collapsed;
                            MessageBoxResult mbr = MessageBox.Show(@"Microsoft Visual C++ 2015-2022 Redistributable (x64) is not installed.
We need this application to show your wallpapers. Do you want to download and install Microsoft Visual C++ 2015-2022 Redistributable (x64)?
Yes = Download and install
No = Change the render engine to Edge WebView2 - Auto
Cancel = Close this message", "Wallpapeuhrs - Error", MessageBoxButton.YesNoCancel, MessageBoxImage.Error);
                            if (mbr == MessageBoxResult.Yes)
                            {
                                WebClient wc = new WebClient();
                                wc.Encoding = Encoding.UTF8;
                                wc.DownloadFileCompleted += (sendere, ee) =>
                                {
                                    Process p = new Process();
                                    p.StartInfo = new ProcessStartInfo();
                                    p.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "\\vc_redist.x64.exe";
                                    p.EnableRaisingEvents = true;
                                    p.Exited += (s, e) =>
                                    {
                                        Debug.WriteLine("fdsdsq " + p.ExitCode);
                                        Dispatcher.Invoke(() => beginWP());
                                    };
                                    //p.StartInfo.Arguments = "/VERYSILENT";
                                    p.Start();
                                    //p.StartInfo.UseShellExecute = true;
                                    //App.Current.Shutdown();
                                    //Process.GetCurrentProcess().Kill();
                                };
                                wc.DownloadFileAsync(new Uri("https://aka.ms/vs/17/release/vc_redist.x64.exe"), AppDomain.CurrentDomain.BaseDirectory + "\\vc_redist.x64.exe");
                            }
                            if (mbr == MessageBoxResult.No)
                            {
                                loaded = true;
                                engine.SelectedIndex = 0;
                            }
                            if(mbr == MessageBoxResult.Cancel)
                            {
                                start.Visibility = Visibility.Visible;
                            }
                            return;
                        }
                        else
                        {
                            start.Visibility = Visibility.Visible;
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Unable to check if Microsoft Visual C++ 2015-2022 Redistributable (x64) is installed.\nException :\n" + ex, "Wallpapeuhrs - Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }*/
                if (sf.getIntSetting("Edge_Engine") <= 3)
                {
                    try
                    {
                        CoreWebView2Environment.GetAvailableBrowserVersionString();
                        start.Visibility = Visibility.Visible;
                    }
                    catch
                    {
                        start.Visibility = Visibility.Collapsed;
                        MessageBoxResult r = MessageBox.Show(@"WebView2 Runtime is not installed.
We need this application to show your wallpapers. Do you want to download and install WebView2?
Yes = Download and install
No = Change the render engine to UWP MediaPlayerElement
Cancel = Close this message", "Wallpapeuhrs - Error", MessageBoxButton.YesNoCancel, MessageBoxImage.Error);
                        if (r == MessageBoxResult.Yes)
                        {
                            WebClient wc = new WebClient();
                            wc.Encoding = Encoding.UTF8;
                            wc.DownloadFileCompleted += (sendere, ee) =>
                            {
                                Process p = new Process();
                                p.StartInfo = new ProcessStartInfo();
                                p.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "\\WebView2Setup.exe";
                                p.EnableRaisingEvents = true;
                                p.Exited += (s, e) =>
                                {
                                    Debug.WriteLine("fdsdsq " + p.ExitCode);
                                    Dispatcher.Invoke(() => beginWP());
                                };
                                //p.StartInfo.Arguments = "/VERYSILENT";
                                p.Start();
                                //p.StartInfo.UseShellExecute = true;
                                //App.Current.Shutdown();
                                //Process.GetCurrentProcess().Kill();
                            };
                            wc.DownloadFileAsync(new Uri("https://go.microsoft.com/fwlink/p/?LinkId=2124703"), AppDomain.CurrentDomain.BaseDirectory + "\\WebView2Setup.exe");
                        }
                        if (r == MessageBoxResult.No)
                        {
                            loaded = true;
                            engine.SelectedIndex = 4;
                        }
                        if (r == MessageBoxResult.Cancel)
                        {
                            start.Visibility = Visibility.Visible;
                        }
                        return;
                    }
                }
                vidReady = 0;
                oneIsDir = false;
                refreshScreensConfig();
                if (!alreadyRestarted && sf.getBoolSetting("RestartExplorer") && inbg) await stopExplorer();
                if (!ok)
                {
                    PStcp = new TcpListener(IPAddress.Parse("127.0.0.1"), 30930);
                    PStcp.Start();
                    ok = true;
                }
                filters.Visibility = sf.getIntSetting("Edge_Engine") <= 3 ? Visibility.Visible : Visibility.Collapsed;
                if (urls.Text != "")
                {
                    //Dictionary<string, string> monis = W32.getMoniGPU();
                    int startAfter = 0;
                    var pe = new List<string>();
                    Process[] pl = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
                    foreach (System.Windows.Forms.Screen mon in System.Windows.Forms.Screen.AllScreens)
                    {
                        ScreenConfig sc = multiscreen_g.Children.OfType<ScreenConfig>().Where(x => x.screenName == mon.DeviceName).FirstOrDefault();
                        string url = sc.urls.Text;
                        if (url == "") url = urls.Text;
                        //Debug.WriteLine("dsqdsqdqsdqs " + url);
                        if(url != "" && !File.Exists(url)) oneIsDir = true;
                        foreach (Process p in pl)
                        {
                            var cmdL = GetCommandLine(p);
                            if (cmdL != null && cmdL.Contains("/moni \"" + mon.DeviceName + "\"")) pe.Add(mon.DeviceName);
                        }
                    }
                    foreach (System.Windows.Forms.Screen mon in System.Windows.Forms.Screen.AllScreens)
                    {
                        /*if(monis.ContainsKey(mon.DeviceName))
                        {
                            //Computer\HKEY_CURRENT_USER\SOFTWARE\Microsoft\DirectX\UserGpuPreferences
                            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\DirectX\\UserGpuPreferences", true);
                            string str = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");
                            string gpu = monis[mon.DeviceName].Contains("NVIDIA") ? "2" : "1";
                            key.SetValue(str, "GpuPreference=" + gpu + ";");
                            //await Task.Delay(1000);
                        }*/
                        if (!pe.Contains(mon.DeviceName) && !processes.ContainsKey(mon.DeviceName))
                        {
                            if (!monis.ContainsKey(mon.DeviceName)) monis.Add(mon.DeviceName, mon.Bounds);
                            isAddingNewProcess = true;
                            async void a()
                            {
                                int index = startAfter;
                                TcpClient PCtcp = await PStcp.AcceptTcpClientAsync();
                                NetworkStream ns = PCtcp.GetStream();
                                byte[] read = new byte[PCtcp.ReceiveBufferSize];
                                //
                                AsyncCallback asy = null;
                                asy = (ar) =>
                                {
                                    try
                                    {
                                        int bytesRead = ns.EndRead(ar);
                                        string stra = Encoding.Unicode.GetString(read, 0, bytesRead).Replace(",", ".");
                                        if (stra.StartsWith("READY "))
                                        {
                                            string moni = stra.Split(" ", StringSplitOptions.RemoveEmptyEntries)[1];
                                            Dispatcher.Invoke(() =>
                                            {
                                                processes.Add(moni, PCtcp);
                                                //Debug.WriteLine(System.Windows.Forms.Screen.AllScreens.Count() + " " + processes.Count + " " + moni);
                                                if (processes.Count == System.Windows.Forms.Screen.AllScreens.Count() /*&& !alreadySendChange*/)
                                                {
                                                    //alreadySendChange = true;
                                                    //Debug.WriteLine("launch " + System.Windows.Forms.Screen.AllScreens.Count() + " " + processes.Count + " " + moni);
                                                    foreach (string monii in processes.Keys)
                                                    {
                                                        sendChange(monii, processes[monii], true);
                                                    }
                                                }
                                            });
                                        }
                                        if (stra.StartsWith("VIDREADY "))
                                        {
                                            string moni = stra.Split(" ", StringSplitOptions.RemoveEmptyEntries)[1];
                                            vidReady++;
                                            Dispatcher.Invoke(() =>
                                            {
                                                //Debug.WriteLine("a " + oneIsDir);
                                                if (!oneIsDir)
                                                {
                                                    //Debug.WriteLine("aaaaaa " + moni + " " + vidReady + " " + System.Windows.Forms.Screen.AllScreens.Count());
                                                    if (vidReady == System.Windows.Forms.Screen.AllScreens.Count() /*&& !alreadySendChange*/)
                                                    {
                                                        foreach (string monii in processes.Keys)
                                                        {
                                                            sendData(processes[monii], "VPlay", monii);
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    //Debug.WriteLine("aaaaab " + moni);
                                                    sendData(processes[moni], "VPlay", moni);
                                                }
                                            });
                                        }
                                        ns.BeginRead(read, 0, PCtcp.ReceiveBufferSize, asy, null);
                                    }
                                    catch
                                    {
                                        Dispatcher.Invoke(() =>
                                        {
                                            if (!stopping)
                                            {
                                                //unknown error here : System.ArgumentNullException: Value cannot be null. (Parameter 'key')
                                                try
                                                {
                                                    foreach (string moni in processes.Keys)
                                                    {
                                                        var tcp = processes[moni];
                                                        if (tcp == PCtcp)
                                                        {
                                                            processes.Remove(moni);
                                                            vidReady -= 1;
                                                            //isAddingNewProcess = true;
                                                            //beginWP();
                                                            return;
                                                        }
                                                    }
                                                }
                                                catch (Exception ex)
                                                {
                                                    //isAddingNewProcess = true;
                                                    //beginWP();
                                                    MessageBox.Show("ID 01-489\n" + ex.ToString(), "Wallpapeuhrs - Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                                }
                                            }
                                        });
                                    }
                                };
                                ns.BeginRead(read, 0, PCtcp.ReceiveBufferSize, asy, null);
                            }
                            a();
                            Process p = new Process();
                            p.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + Process.GetCurrentProcess().ProcessName + ".exe";
                            p.StartInfo.Arguments = "--wp /startAfter " + startAfter + " /moni \"" + mon.DeviceName + "\" /engine " + engine.SelectedIndex + "";
                            //p.StartInfo.UseShellExecute = true;
                            p.Start();
                        }
                        else
                        {
                            if (processes.ContainsKey(mon.DeviceName) && !isAddingNewProcess)
                                sendChange(mon.DeviceName, processes[mon.DeviceName], false);
                        }
                        startAfter += 1;
                    }
                    ok = true;
                    loaded = true;
                }
                else MessageBox.Show("Please put the path of a media or a folder to continue.", "Wallpapeuhrs - Error", MessageBoxButton.OK, MessageBoxImage.Error);
                isAddingNewProcess = false;
                /*Microsoft.Win32.RegistryKey keyk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\DirectX\\UserGpuPreferences", true);
                string strk = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");
                keyk.SetValue(strk, "GpuPreference=0;");*/
            }
            catch (Exception e)
            {
                MessageBox.Show("ID 01-677\n" + e.ToString(), "Wallpapeuhrs - Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetCommandLine(Process process)
        {
            try
            {
                using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id))
                using (ManagementObjectCollection objects = searcher.Get())
                {
                    return objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
                }
            }
            catch
            {
                return null;
            }
        }

        private void sendChange(string moni, TcpClient PCtcp, bool forceReset)
        {
            Dispatcher.Invoke(() =>
            {
                foreach (FrameworkElement el in multiscreen_g.Children)
                {
                    if (el is ScreenConfig)
                    {
                        ScreenConfig screenConfig = (ScreenConfig)el;
                        if (moni == screenConfig.screenName)
                        {
                            sendData(PCtcp, "Liste", moni);
                            sendData(PCtcp, screenConfig.urls.Text == "" ? urls.Text : screenConfig.urls.Text, moni);
                            sendData(PCtcp, "Endliste", moni);
                            //
                            sendData(PCtcp, "Volume=" + (screenConfig.vol.Text == "" ? sf.getIntSetting("Vol") : Convert.ToInt32(screenConfig.vol.Text)), moni);
                            sendData(PCtcp, "Interval=" + (screenConfig.interval.Text == "" ? sf.getIntSetting("Interval") : Convert.ToInt32(screenConfig.interval.Text)), moni);
                            sendData(PCtcp, "Repeat=" + sf.getBoolSetting("Repeat"), moni);
                            sendData(PCtcp, "Fullrdm=" + sf.getBoolSetting("FullRdm"), moni);
                            if(forceReset) sendData(PCtcp, "ForceReset", moni);
                            sendData(PCtcp, "InitOK", moni);
                            int i = 0;
                            foreach(Grid g in filters.Children)
                            {
                                var slider = g.Children[1] as Slider;
                                if (!sf.settingExists("Screen_" + screenConfig.screenName + "_Theme_" + slider.Tag))
                                    sendData(PCtcp, "ChangeTheme=" + slider.Tag + "=" + slider.Value, moni);
                                else
                                {
                                    var sli = (screenConfig.filters.Children[i] as Grid).Children[1] as Slider;
                                    sendData(PCtcp, "ChangeTheme=" + sli.Tag + "=" + sli.Value, moni);
                                }
                                i++;
                            }
                        }
                    }
                }
            });
        }

        private void start_Click(object sender, RoutedEventArgs e)
        {
            setSettings();
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            string str = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");
            try { key.DeleteValue("Wallpapeuhrs"); } catch { }
            if ((bool)startwithw.IsChecked) key.SetValue("Wallpapeuhrs", "\"" + str + "\" --background");
            int curConfIndex = curIndexConfig;
            beginWP();
            changeScreenConfig(curConfIndex);
        }

        bool curPlay = true;

        public void changePlayerState(bool play)
        {
            IntPtr pp = IntPtr.Zero;
            string[] wins = new string[] { "Task View", "Start", "Search" };
            int anLength = win.Count;
            foreach (string w in wins)
            {
                if (!win.Contains(W32.FindWindow("Windows.UI.Core.CoreWindow", w).ToInt32()))
                    win.Add(W32.FindWindow("Windows.UI.Core.CoreWindow", w).ToInt32());
            }
            W32.EnumWindows(new W32.EnumWindowsProc((tophandle, topparamhandle) =>
            {
                IntPtr p = W32.FindWindowEx(tophandle,
                                            IntPtr.Zero,
                                            "SHELLDLL_DefView",
                                            IntPtr.Zero);
                StringBuilder cn = new StringBuilder(256);
                if (p != IntPtr.Zero)
                {
                    pp = tophandle;
                    if (!win.Contains(tophandle.ToInt32())) win.Add(tophandle.ToInt32());
                }
                int cls = W32.GetClassName(tophandle, cn, cn.Capacity);
                if (cls != 0)
                {
                    if (cn.ToString().EndsWith("TrayWnd") && cn.ToString().StartsWith("Shell_"))
                    {
                        if (!win.Contains(tophandle.ToInt32())) win.Add(tophandle.ToInt32());
                    }
                }
                return true;
            }), IntPtr.Zero);
            if (curIndexEcoConfig == 2 && anLength != win.Count) play = true;
            if (play || curIndexEcoConfig == 0 || anLength == win.Count)
            {
                //log("changePlayerState " + play);
                curPlay = play;
                try
                {
                    Dispatcher.Invoke(() =>
                    {
                        foreach (string moni in processes.Keys)
                        {
                            sendData(processes[moni], play ? "Play" : "Pause", moni);
                        }
                    });
                }
                catch 
                {
                    if (play) beginWP();
                    else
                    {
                        Process[] pl = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
                        IntPtr handle = new WindowInteropHelper(this).Handle;
                        foreach(Process p in pl)
                        {
                            if (p.MainWindowHandle != handle) p.Kill();
                        }
                    }
                }
            }
        }

        private void vid_Click(object sender, RoutedEventArgs e)
        {
            vidClick(urls);
        }

        public static void vidClick(TextBox tb)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.InitialDirectory = System.IO.Path.GetFullPath(tb.Text != "" ? tb.Text : "c:\\");
            string filter = "Video or Image|";
            int i = 0;
            foreach (string type in App.types.Keys)
            {
                int j = 0;
                foreach (string ext in App.types[type])
                {
                    filter += "*" + ext + (j < App.types[type].Count - 1 ? ";" : "");
                    j++;
                }
                filter += i < App.types.Keys.Count - 1 ? ";" : "";
                i++;
            }
            //openFileDialog.Filter = "Videos files|*.mp4;*.avi;*.gif;*.mov|Images files|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*";
            openFileDialog.Filter = filter;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.Multiselect = false;
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                tb.Text = openFileDialog.FileName;
            }
        }

        private void slide_Click(object sender, RoutedEventArgs e)
        {
            slideClick(urls);
        }

        public static void slideClick(TextBox tb)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = System.IO.Path.GetFullPath(tb.Text != "" ? tb.Text : "c:\\");
            dialog.AllowNonFileSystemItems = true;
            dialog.Multiselect = false;
            dialog.IsFolderPicker = true;
            dialog.RestoreDirectory = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                tb.Text = dialog.FileName;
            }
        }

        public static void sendData(TcpClient tcp, string text, string moni)
        {
            try
            {
                byte[] data2 = System.Text.Encoding.Unicode.GetBytes((moni != null ? moni + ": " : "") + text + "|");
                /*NetworkStream nwStream = tcp.GetStream();
                await nwStream.WriteAsync(data2, 0, data2.Length);*/
                //nwStream.Flush();
                SocketAsyncEventArgs sa = new SocketAsyncEventArgs();
                sa.Completed += (sendere, ee) =>
                {
                    if (ee.SocketError != SocketError.Success) MessageBox.Show("Error tcp send data " + ee.SocketError);
                };
                sa.SetBuffer(data2, 0, data2.Length);
                tcp.Client.SendAsync(sa);
            }
            catch(Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

        public async Task stopExplorer()
        {
            try
            {
                Process cmd = new Process();
                cmd.StartInfo.FileName = "cmd.exe";
                cmd.StartInfo.CreateNoWindow = true;
                cmd.StartInfo.UseShellExecute = true;
                cmd.StartInfo.Arguments = "/c taskkill /F /IM \"dwm.exe\" | taskkill /F /IM \"explorer.exe\"";
                cmd.StartInfo.Verb = "runas";
                cmd.Start();
                cmd.WaitForExit();
                Process.Start("explorer.exe");
                alreadyRestarted = true;
                await Task.Delay(5000);
            }
            catch { }
        }

        private async void restart_dwm_Click(object sender, RoutedEventArgs e)
        {
            await stopExplorer();
        }

        private void intel_link_MouseUp(object sender, MouseButtonEventArgs e)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "https://www.intel.com/content/www/us/en/download/19344/691496/intel-graphics-windows-dch-drivers.html",
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        public void ForceWindowIntoForeground(IntPtr window)
        {
            uint currentThread = W32.GetCurrentThreadId();

            IntPtr activeWindow = W32.GetForegroundWindow();
            uint activeProcess;
            uint activeThread = W32.GetWindowThreadProcessId(activeWindow, out activeProcess);

            uint windowProcess;
            uint windowThread = W32.GetWindowThreadProcessId(window, out windowProcess);

            if (currentThread != activeThread)
                W32.AttachThreadInput(currentThread, activeThread, true);
            if (windowThread != currentThread)
                W32.AttachThreadInput(windowThread, currentThread, true);

            uint oldTimeout = 0, newTimeout = 0;
            W32.SystemParametersInfo(W32.SPI_GETFOREGROUNDLOCKTIMEOUT, 0, ref oldTimeout, 0);
            W32.SystemParametersInfo(W32.SPI_SETFOREGROUNDLOCKTIMEOUT, 0, ref newTimeout, 0);
            W32.LockSetForegroundWindow(W32.LSFW_UNLOCK);
            W32.AllowSetForegroundWindow(W32.ASFW_ANY);

            Show();
            Topmost = true;
            Activate();
            Topmost = false;
            W32.SetForegroundWindow(window);
            //W32.ShowWindow(window, (int)W32.ShowWindowCommands.Restore);

            W32.SystemParametersInfo(W32.SPI_SETFOREGROUNDLOCKTIMEOUT, 0, ref oldTimeout, 0);

            if (currentThread != activeThread)
                W32.AttachThreadInput(currentThread, activeThread, false);
            if (windowThread != currentThread)
                W32.AttachThreadInput(windowThread, currentThread, false);
        }

        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }

        private async void about_us_Click(object sender, RoutedEventArgs e)
        {
            string link = await Update.getServer();
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = link,
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        private async void more_info_Click(object sender, RoutedEventArgs e)
        {
            string link = await Update.getServer();
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = link + "news/wallpapeuhrs.php",
                UseShellExecute = true
            };
            Process.Start(psi);
        }

        double maxHeightExpand = 0;

        private void show_more_Click(object sender, RoutedEventArgs e)
        {
            if (show_more_expend.Visibility == Visibility.Visible)
            {
                Storyboard sb = new Storyboard();
                sb.Children.Add(addDoubleAnimation("MyAnimatedTransform", TimeSpan.FromMilliseconds(200), 90, 0, new PropertyPath(RotateTransform.AngleProperty)));
                sb.Children.Add(addDoubleAnimation(show_more_expend, TimeSpan.FromMilliseconds(200), maxHeightExpand, 0, new PropertyPath(Grid.HeightProperty)));
                sb.Completed += (sender, e) =>
                {
                    show_more_expend.Visibility = Visibility.Collapsed;
                };
                sb.Begin(this);
            }
            else
            {
                show_more_expend.Visibility = Visibility.Visible;
                Storyboard sb = new Storyboard();
                sb.Children.Add(addDoubleAnimation("MyAnimatedTransform", TimeSpan.FromMilliseconds(200), 0, 90, new PropertyPath(RotateTransform.AngleProperty)));
                sb.Children.Add(addDoubleAnimation(show_more_expend, TimeSpan.FromMilliseconds(200), 0, maxHeightExpand, new PropertyPath(Grid.HeightProperty)));
                sb.Begin(this);
            }
        }

        public DoubleAnimation addDoubleAnimation(DependencyObject el, TimeSpan time, double? from, double? to, PropertyPath property)
        {
            DoubleAnimation da = new DoubleAnimation();
            if(from != null) da.From = from;
            if (to != null) da.To = to;
            da.Duration = new Duration(time);
            Storyboard.SetTarget(da, el);
            Storyboard.SetTargetProperty(da, property);
            return da;
        }

        /// <summary>
        /// sb.Begin(this); is required to work
        /// </summary>
        /// <param name="el"></param>
        /// <param name="time"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="property"></param>
        /// <returns></returns>
        public DoubleAnimation addDoubleAnimation(string el, TimeSpan time, double? from, double? to, PropertyPath property)
        {
            DoubleAnimation da = new DoubleAnimation();
            if (from != null) da.From = from;
            if (to != null) da.To = to;
            da.Duration = new Duration(time);
            Storyboard.SetTargetName(da, el);
            Storyboard.SetTargetProperty(da, property);
            return da;
        }

        private void vname_Click(object sender, RoutedEventArgs e)
        {
            if (!Update.haveUpdate)
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "https://github.com/Shiyukine/Wallpapeuhrs",
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            else Update.installUpdate();
        }

        private void ScrollViewer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer sv = (ScrollViewer)sender;
            sv.ScrollToHorizontalOffset(e.Delta);
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            ScrollViewer sv = (ScrollViewer)sender;
            sv.ScrollToHorizontalOffset(sv.HorizontalOffset + e.Delta * -1);
        }

        FrameworkElement anBorder = null;
        NewButtons anButton = null;

        private void change_screen_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            changeScreenConfig(multiscreen.Children.IndexOf(button));
        }

        int curIndexConfig = -1;
        private void changeScreenConfig(int index)
        {
            NewButtons nButton = (NewButtons)multiscreen.Children[index];
            FrameworkElement nBorder = (FrameworkElement)multiscreen_g.Children[index];
            if (anBorder != null && anButton != null)
            {
                anButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 79, 79, 79));
                anBorder.Visibility = Visibility.Collapsed;
                nButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 79, 79, 79));
                nBorder.Visibility = Visibility.Visible;
            }
            anButton = nButton;
            anBorder = nBorder;
            curIndexConfig = index;
        }

        NewButtons anButtonEco = null;

        private void change_eco_Click(object sender, RoutedEventArgs e)
        {
            Button button = (Button)sender;
            changeEcoConfig(ecomode.Children.IndexOf(button));
        }

        int curIndexEcoConfig = -1;
        private void changeEcoConfig(int index)
        {
            NewButtons nButton = (NewButtons)ecomode.Children[index];
            if (anButtonEco != null)
            {
                anButtonEco.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 79, 79, 79));
            }
            nButton.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 79, 79, 79));
            anButtonEco = nButton;
            curIndexEcoConfig = index;
        }

        private void refreshScreensConfig()
        {
            changeScreenConfig(0);
            multiscreen.Children.RemoveRange(1, multiscreen.Children.Count - 1);
            multiscreen_g.Children.RemoveRange(1, multiscreen_g.Children.Count - 1);
            int i = 1;
            foreach(System.Windows.Forms.Screen s in System.Windows.Forms.Screen.AllScreens)
            {
                string index = s.DeviceName;
                NewButtons bu = new NewButtons();
                bu.Content = "Screen " + i;
                bu.BorderRadius = screen_all.BorderRadius;
                bu.Height = screen_all.Height;
                bu.Width = screen_all.Width;
                bu.Click += change_screen_Click;
                bu.Background = new SolidColorBrush(System.Windows.Media.Color.FromArgb(0, 79, 79, 79));
                bu.Foreground = screen_all.Foreground;
                bu.FontSize = screen_all.FontSize;
                bu.BorderBrush = screen_all.BorderBrush;
                bu.Margin = screen_all.Margin;
                bu.Padding = screen_all.Padding;
                bu.HorizontalAlignment = screen_all.HorizontalAlignment;
                bu.VerticalAlignment = screen_all.VerticalAlignment;
                bu.BackgroundHover = screen_all.BackgroundHover;
                bu.Name = "";
                multiscreen.Children.Add(bu);
                ScreenConfig b = new ScreenConfig();
                b.screenName = s.DeviceName;
                b.Visibility = Visibility.Collapsed;
                if (!sf.settingExists("Screen_" + index + "_url")) sf.setSetting("Screen_" + index + "_url", "", null);
                if (!sf.settingExists("Screen_" + index + "_interval")) sf.setSetting("Screen_" + index + "_interval", "", null);
                if (!sf.settingExists("Screen_" + index + "_vol")) sf.setSetting("Screen_" + index + "_vol", "", null);
                b.urls.Text = sf.getStringSetting("Screen_" + index + "_url");
                b.interval.Text = sf.getStringSetting("Screen_" + index + "_interval");
                b.vol.Text = sf.getStringSetting("Screen_" + index + "_vol");
                if (sf.settingExists("Edge_Engine") && sf.getIntSetting("Edge_Engine") <= 3)
                {
                    foreach (Grid g in b.filters.Children)
                    {
                        var slider = g.Children[1] as Slider;
                        if (slider.Tag == null || (string)slider.Tag == "") MessageBox.Show("ID 01-1160\n'" + slider.Tag + "'", "Wallpapeuhrs - Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        if (sf.settingExists("Screen_" + index + "_Theme_" + slider.Tag)) slider.Value = sf.getDoubleSetting("Screen_" + index + "_Theme_" + slider.Tag);
                    }
                }
                else
                {
                    b.filters.Visibility = Visibility.Collapsed;
                }
                multiscreen_g.Children.Add(b);
                i++;
            }
        }

        private void th_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (settingsLoaded)
            {
                changeThemeValue(sender, "-");
            }
        }

        public static void changeThemeValue(object sender, string moni)
        {
            try
            {
                var slider = sender as Slider;
                if (loaded)
                {
                    if (slider.Tag != null && (string)slider.Tag != "")
                    {
                        if (moni == "-")
                        {
                            foreach (string monii in processes.Keys)
                            {
                                if (!sf.settingExists("Screen_" + monii + "_Theme_" + (string)slider.Tag))
                                    sendData(processes[monii], "ChangeTheme=" + (string)slider.Tag + "=" + slider.Value, monii);
                            }
                            if ("Theme_" + slider.Tag == "Theme_") MessageBox.Show("ID 01-1199" + Environment.StackTrace, "Wallpapeuhrs - Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            else sf.setSetting("Theme_" + slider.Tag, slider.Value, null);
                        }
                        else
                        {
                            if (sf.settingExists("Screen_" + moni + "_Theme_" + slider.Tag))
                                sendData(processes[moni], "ChangeTheme=" + (string)slider.Tag + "=" + slider.Value, moni);
                            else
                            {
                                if (sf.settingExists("Theme_" + slider.Tag))
                                    sendData(processes[moni], "ChangeTheme=" + (string)slider.Tag + "=" + sf.getDoubleSetting("Theme_" + slider.Tag), moni);
                                else
                                    sendData(processes[moni], "ChangeTheme=" + (string)slider.Tag + "=" + slider.Value, moni);
                            }
                            if ("Screen_" + moni + "_Theme_" + slider.Tag == "Screen_" + moni + "_Theme_") MessageBox.Show("ID 01-1214" + Environment.StackTrace, "Wallpapeuhrs - Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            else sf.setSetting("Screen_" + moni + "_Theme_" + slider.Tag, slider.Value, null);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void th_DragDelta(object sender, DragDeltaEventArgs e)
        {
            /*Slider slider = (Slider)sender;
            foreach (string monii in processes.Keys)
            {
                sendData(processes[monii], "ChangeTheme=" + (string)slider.Tag + "=" + slider.Value, monii);
            }*/
        }

        private void engine_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (loaded)
            {
                foreach (string monii in processes.Keys)
                {
                    TcpClient tcpClient = processes[monii];
                    tcpClient.Close();
                    processes.Remove(monii);
                }
                sf.setSetting("Edge_Engine", engine.SelectedIndex, null);
                beginWP();
            }
        }

        private void th_reset(object sender, RoutedEventArgs e)
        {
            resetThemeValue(sender, null);
        }

        public static void resetThemeValue(object sender, string moni)
        {
            var btn = sender as NewButtons;
            var slider = (btn.Parent as Grid).Children[1] as Slider;
            string tag = (btn.Tag as string).Split(' ')[0];
            int value = Convert.ToInt32((btn.Tag as string).Split(' ')[1]);
            slider.Value = value;
            if (loaded)
            {
                if (moni == null)
                {
                    foreach (string monii in processes.Keys)
                    {
                        if(!sf.settingExists("Screen_" + monii + "_Theme_" + tag))
                            sendData(processes[monii], "ChangeTheme=" + tag + "=" + value, monii);
                    }
                    sf.removeSetting("Theme_" + tag);
                }
                else
                {
                    if (!sf.settingExists("Theme_" + slider.Tag))
                        sendData(processes[moni], "ChangeTheme=" + tag + "=" + value, moni);
                    else
                        sendData(processes[moni], "ChangeTheme=" + tag + "=" + sf.getDoubleSetting("Theme_" + tag), moni);
                    sf.removeSetting("Screen_" + moni + "_Theme_" + tag);
                }
            }
        }

        public static void addLog(string type, string className, string log)
        {
            File.AppendAllText(userFolder + "latest.log", "[" + className + "] " + type + " -> " + log + "\n");
        }

        public static void addLog(string className, string log)
        {
            addLog("log", className, log);
        }

        /* NE PAS TOUCHER
         * [Windows.Foundation.Metadata.ContractVersion(typeof(Windows.Foundation.UniversalApiContract), 65536)]
    [Windows.Foundation.Metadata.MarshalingBehavior(Windows.Foundation.Metadata.MarshalingType.None)]
    [Windows.Foundation.Metadata.Threading(Windows.Foundation.Metadata.ThreadingModel.STA)]
    [Windows.Foundation.Metadata.Activatable(65536, "Windows.Foundation.UniversalApiContract")]*/
    }
}
