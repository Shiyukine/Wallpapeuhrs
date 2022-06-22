using Microsoft.WindowsAPICodePack;
using Microsoft.WindowsAPICodePack.Dialogs;
using Microsoft.WindowsAPICodePack.Shell;
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

namespace Wallpapeuhrs
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Dictionary<string, TcpClient> processes = new Dictionary<string, TcpClient>();
        TcpListener PStcp = null;
        SettingsManager sf = null;
        bool ok = false;
        public static bool isclos = false;
        bool inbg = false;
        System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();
        bool loaded = false;
        //string curNativeWallpaper = "";

        public MainWindow(bool inbg)
        {
            NameScope.SetNameScope(this, new NameScope());
            App.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            App.Current.MainWindow = this;
            InitializeComponent();
            RegisterName("MyAnimatedTransform", show_more_r);
            vname.Content = Update.getVersionName();
            Update.searchUpdates();
            //curNativeWallpaper = NativeWallpaper.getCurrentDesktopWallpaper();
            this.inbg = inbg;
            //
            var wih = new WindowInteropHelper(this);
            var hwnd = wih.EnsureHandle();
            _ScreenStateNotify = W32.RegisterPowerSettingNotification(hwnd, ref W32.GUID_CONSOLE_DISPLAY_STATE, W32.DEVICE_NOTIFY_WINDOW_HANDLE);
            _HwndSource = HwndSource.FromHwnd(hwnd);
            _HwndSource.AddHook(HwndHook);
            //
            string newF = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Wallpapeuhrs\\";
            if (!inbg) Show();
            if (!Directory.Exists(newF)) Directory.CreateDirectory(newF);
            sf = new SettingsManager(newF + "Settings.cfg");
            loadSettings();
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
                //sv.CanContentScroll = false;
                int ii = 0;
                sv.PreviewMouseWheel += (sendere, ee) =>
                {
                    sv.InvalidateScrollInfo();
                    ee.Handled = true;
                    sv.ScrollToVerticalOffset(sv.VerticalOffset);
                    ii++;
                    DoubleAnimation verticalAnimation = new DoubleAnimation();

                    verticalAnimation.From = sv.VerticalOffset;
                    int delta = ee.Delta;
                    //if (delta < 0) delta = ee.Delta * (-1);
                    int offset = 40 * ee.Delta / 120 * ii;
                    verticalAnimation.To = sv.VerticalOffset - offset;
                    verticalAnimation.Duration = new Duration(TimeSpan.FromMilliseconds(150));

                    Storyboard storyboard = new Storyboard();

                    storyboard.Children.Add(verticalAnimation);
                    Storyboard.SetTarget(verticalAnimation, sv);
                    Storyboard.SetTargetProperty(verticalAnimation, new PropertyPath(ScrollAnimationBehavior.VerticalOffsetProperty));
                    storyboard.Completed += (ss, eee) =>
                    {
                        ii = 0;
                    };
                    storyboard.Begin();
                };
            }
            //
            connectLocalTCP();
            //
            System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
            t.Interval = 1000;
            t.Tick += (sender, e) =>
            {
                if (loaded)
                {
                    Process[] pl = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
                    if (pl.Length - 1 > System.Windows.Forms.Screen.AllScreens.Count())
                    {
                        List<Process> pRem = new List<Process>();
                        foreach(Process p in pl)
                        {
                            if (p.MainWindowHandle != Process.GetCurrentProcess().MainWindowHandle)
                                pRem.Add(p);
                        }
                        List<System.Windows.Forms.Screen> sRem = new List<System.Windows.Forms.Screen>(System.Windows.Forms.Screen.AllScreens);
                        foreach (System.Windows.Forms.Screen s in System.Windows.Forms.Screen.AllScreens)
                        {
                            Process process = null;
                            foreach (Process p in pl)
                            {
                                if (GetCommandLine(p).Contains("/moni \"" + s.DeviceName + "\""))
                                {
                                    process = p;
                                }
                            }
                            if (process != null)
                            {
                                pRem.Remove(process);
                                sRem.Remove(s);
                            }
                        }
                        foreach(Process p in pRem)
                        {
                            p.Kill();
                        }
                        foreach(System.Windows.Forms.Screen s in sRem)
                        {
                            processes.Remove(s.DeviceName);
                            monis.Remove(s.DeviceName);
                        }
                        foreach (string monii in processes.Keys)
                        {
                            sendChange(monii, processes[monii]);
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
                            if(monis.ContainsKey(s.DeviceName) && monis[s.DeviceName] != s.Bounds)
                            {
                                beginWP();
                                monis[s.DeviceName] = s.Bounds;
                            }
                        }
                    }
                }
                if ((bool)stopan.IsChecked)
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
            if (urls.Text != "") beginWP();
        }

        List<int> win = new List<int>();
        int anWin = -1;
        Dictionary<string, System.Drawing.Rectangle> monis = new Dictionary<string, System.Drawing.Rectangle>();

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            // handler of console display state system event 
            if (msg == W32.WM_POWERBROADCAST)
            {
                if (wParam.ToInt32() == W32.PBT_POWERSETTINGCHANGE)
                {
                    var s = (W32.POWERBROADCAST_SETTING)Marshal.PtrToStructure(lParam, typeof(W32.POWERBROADCAST_SETTING));
                    if (s.PowerSetting == W32.GUID_CONSOLE_DISPLAY_STATE && loaded)
                    {
                        if(s.Data == 1)
                        {
                            //Debug.WriteLine("Not in sleep mode");
                            changePlayerState(true);
                        }
                        if(s.Data == 0)
                        {
                            //Debug.WriteLine("Sleep mode");
                            foreach (string moni in processes.Keys)
                            {
                                changePlayerState(false);
                            }
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

        private async void connectLocalTCP()
        {
            TcpClient tcp = null;
            TcpListener tcps = null;
            int port = 30929;
            try
            {
                tcps = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
                tcps.Start();
                tcp = await tcps.AcceptTcpClientAsync();
            }
            catch
            {
                MessageBox.Show("Unable to start Wallpapeuhrs. The TCP at port locahost:" + port + " is already used. Please kill all applications who use this port and then restart Wallpapeuhrs.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                App.Current.Shutdown();
            }
            NetworkStream ns = tcp.GetStream();
            tcp.ReceiveBufferSize = 1024;
            byte[] read = new byte[tcp.ReceiveBufferSize];
            //
            AsyncCallback asy = null;
            asy = async (ar) =>
            {
                try
                {
                    int bytesRead = ns.EndRead(ar);
                    string stra = Encoding.ASCII.GetString(read, 0, bytesRead).Replace(",", ".");
                    if (stra == "SHOW")
                    {
                        Dispatcher.Invoke(() =>
                        {
                            if (WindowState != WindowState.Maximized) WindowState = WindowState.Normal;
                            IntPtr handle = new WindowInteropHelper(this).Handle;
                            ForceWindowIntoForeground(handle);
                            inbg = false;
                        });
                    }
                    if (stra == "")
                    {
                        /*tcps.Stop();
                        tcp.Close();
                        connectLocalTCP();*/
                        tcp = await tcps.AcceptTcpClientAsync();
                        ns = tcp.GetStream();
                        tcp.ReceiveBufferSize = 1024;
                        read = new byte[tcp.ReceiveBufferSize];
                    }
                    ns.BeginRead(read, 0, tcp.ReceiveBufferSize, asy, null);
                }
                catch (Exception e)
                {
                    MessageBox.Show("TCP error:\n" + e);
                }
            };
            ns.BeginRead(read, 0, tcp.ReceiveBufferSize, asy, null);
        }

        bool stopping = false;

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //W32.SetParent(new WindowInteropHelper(this).Handle, WPBG.Form1.workerw);
            if (isclos)
            {
                //NativeWallpaper.changeWallpaper(curNativeWallpaper);
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
                sf.setSetting("Stop", (bool)stopan.IsChecked, null);
                sf.setSetting("RestartExplorer", (bool)restartexplo.IsChecked, null);
                restart_dwm.Visibility = sf.getBoolSetting("RestartExplorer") ? Visibility.Visible : Visibility.Collapsed;
            }
            catch
            {
                MessageBox.Show("A parameter does not correspond to what is requested. Please check all settings before saving.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void loadSettings()
        {
            if (!sf.settingExists("Path")) sf.setSetting("Path", "", null);
            if (!sf.settingExists("Vol")) sf.setSetting("Vol", 0, null);
            if (!sf.settingExists("Interval")) sf.setSetting("Interval", 60, null);
            if (!sf.settingExists("Start")) sf.setSetting("Start", true, null);
            if (!sf.settingExists("Repeat")) sf.setSetting("Repeat", true, null);
            if (!sf.settingExists("Stop")) sf.setSetting("Stop", false, null);
            if (!sf.settingExists("RestartExplorer")) sf.setSetting("RestartExplorer", false, null);
            //
            urls.Text = sf.getStringSetting("Path");
            vol.Text = sf.getStringSetting("Vol");
            interval.Text = sf.getStringSetting("Interval");
            startwithw.IsChecked = sf.getBoolSetting("Start");
            stopan.IsChecked = sf.getBoolSetting("Stop");
            restartexplo.IsChecked = sf.getBoolSetting("RestartExplorer");
            restart_dwm.Visibility = sf.getBoolSetting("RestartExplorer") ? Visibility.Visible : Visibility.Collapsed;
        }

        //for explorer.exe
        bool alreadyRestarted = false;

        //for beginWP
        bool isAddingNewProcess = false;
        bool alreadySendChange = false;

        private async void beginWP()
        {
            try
            {
                //NativeWallpaper.changeWallpaper("");
                loaded = false;
                alreadySendChange = false;
                if (!alreadyRestarted && sf.getBoolSetting("RestartExplorer") && inbg) await stopExplorer();
                if (!ok)
                {
                    PStcp = new TcpListener(IPAddress.Parse("127.0.0.1"), 30930);
                    PStcp.Start();
                    ok = true;
                }
                if (urls.Text != "")
                {
                    //Dictionary<string, string> monis = W32.getMoniGPU();
                    int startAfter = 0;
                    var pe = new List<string>();
                    Process[] pl = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
                    foreach (System.Windows.Forms.Screen mon in System.Windows.Forms.Screen.AllScreens)
                    {
                        foreach (Process p in pl)
                        {
                            try
                            { 
                                var cmdL = GetCommandLine(p);
                                if (cmdL != "" && cmdL.Contains("/moni \"" + mon.DeviceName + "\"")) pe.Add(mon.DeviceName);
                            }
                            catch { }
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
                                            processes.Add(moni, PCtcp);
                                            //Debug.WriteLine(System.Windows.Forms.Screen.AllScreens.Count() + " " + processes.Count + " " + moni);
                                            if (processes.Count == System.Windows.Forms.Screen.AllScreens.Count() /*&& !alreadySendChange*/)
                                            {
                                                //alreadySendChange = true;
                                                //Debug.WriteLine("launch " + System.Windows.Forms.Screen.AllScreens.Count() + " " + processes.Count + " " + moni);
                                                foreach (string monii in processes.Keys)
                                                {
                                                    sendChange(monii, processes[monii]);
                                                }
                                            }
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
                                                    MessageBox.Show("ID 01-489\n" + ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                            p.StartInfo.Arguments = "--wp /startAfter " + startAfter + " /moni \"" + mon.DeviceName + "\"";
                            p.StartInfo.UseShellExecute = true;
                            p.Start();
                        }
                        else
                        {
                            if (processes.ContainsKey(mon.DeviceName) && !isAddingNewProcess)
                                sendChange(mon.DeviceName, processes[mon.DeviceName]);
                        }
                        startAfter += 1;
                    }
                    ok = true;
                    loaded = true;
                }
                else MessageBox.Show("Please put the path of a media or a folder to continue.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                isAddingNewProcess = false;
                /*Microsoft.Win32.RegistryKey keyk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\DirectX\\UserGpuPreferences", true);
                string strk = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");
                keyk.SetValue(strk, "GpuPreference=0;");*/
            }
            catch (Exception e)
            {
                MessageBox.Show("beginWP\n" + e.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
                return "";
            }
        }

        private void sendChange(string moni, TcpClient PCtcp)
        {
            sendData(PCtcp, "Liste", moni);
            Dispatcher.Invoke(() => sendData(PCtcp, urls.Text, moni));
            sendData(PCtcp, "Endliste", moni);
            //
            sendData(PCtcp, "Volume=" + sf.getIntSetting("Vol"), moni);
            sendData(PCtcp, "Interval=" + sf.getIntSetting("Interval"), moni);
            sendData(PCtcp, "Repeat=" + sf.getBoolSetting("Repeat"), moni);
            sendData(PCtcp, "Autostop=" + sf.getBoolSetting("Stop"), moni);
        }

        private void start_Click(object sender, RoutedEventArgs e)
        {
            setSettings();
            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            string str = Assembly.GetExecutingAssembly().Location.Replace(".dll", ".exe");
            try { key.DeleteValue("Wallpapeuhrs"); } catch { }
            if ((bool)startwithw.IsChecked) key.SetValue("Wallpapeuhrs", "\"" + str + "\" --background");
            beginWP();
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
            if ((bool)stopan.IsChecked && anLength != win.Count) play = true;
            if (play || !(bool)stopan.IsChecked || anLength == win.Count)
            {
                //log("changePlayerState " + play);
                curPlay = play;
                foreach (string moni in processes.Keys)
                {
                    sendData(processes[moni], play ? "Play" : "Pause", moni);
                }
            }
        }

        private void vid_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.InitialDirectory = System.IO.Path.GetFullPath(urls.Text != "" ? urls.Text : "c:\\");
            string filter = "";
            foreach(string type in App.types.Keys)
            {
                filter += type + "|";
                int i = 0;
                foreach (string ext in App.types[type])
                {
                    filter += "*" + ext + (i < App.types[type].Count - 1 ? ";" : "");
                    i++;
                }
                filter += "|";
            }
            filter += "All files (*.*)|*.*";
            //openFileDialog.Filter = "Videos files|*.mp4;*.avi;*.gif;*.mov|Images files|*.jpg;*.jpeg;*.png;*.bmp|All files (*.*)|*.*";
            openFileDialog.Filter = filter;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.Multiselect = false;
            if (openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                urls.Text = openFileDialog.FileName;
            }
        }

        private void slide_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.InitialDirectory = System.IO.Path.GetFullPath(urls.Text != "" ? urls.Text : "c:\\");
            dialog.AllowNonFileSystemItems = true;
            dialog.Multiselect = false;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                urls.Text = dialog.FileName;
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

        /* NE PAS TOUCHER
         * [Windows.Foundation.Metadata.ContractVersion(typeof(Windows.Foundation.UniversalApiContract), 65536)]
    [Windows.Foundation.Metadata.MarshalingBehavior(Windows.Foundation.Metadata.MarshalingType.None)]
    [Windows.Foundation.Metadata.Threading(Windows.Foundation.Metadata.ThreadingModel.STA)]
    [Windows.Foundation.Metadata.Activatable(65536, "Windows.Foundation.UniversalApiContract")]*/
    }
}
