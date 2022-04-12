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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
        bool isclos = false;
        bool inbg = false;
        System.Windows.Forms.NotifyIcon ni = new System.Windows.Forms.NotifyIcon();
        bool loaded = false;

        public MainWindow(bool inbg)
        {
            App.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            App.Current.MainWindow = this;
            InitializeComponent();
            vname.Text = Update.getVersionName();
            Update.searchUpdates();
            this.inbg = inbg;
            //
            string newF = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Wallpapeuhrs\\";
            if (!inbg) Show();
            if (!Directory.Exists(newF)) Directory.CreateDirectory(newF);
            sf = new SettingsManager(newF + "Settings.cfg");
            loadSettings();
            //
            System.Windows.Forms.ContextMenu cm = new System.Windows.Forms.ContextMenu();
            Stream iconStream = Application.GetResourceStream(new Uri("/Wallpapeuhrs;component/Resources/Icon.ico", UriKind.RelativeOrAbsolute)).Stream;
            ni.Icon = new System.Drawing.Icon(iconStream);
            iconStream.Dispose();
            ni.Text = "Wallpapeuhrs";
            System.Windows.Forms.MenuItem mi3 = new System.Windows.Forms.MenuItem() { Text = "Show app" };
            mi3.Click += (s, ev) =>
            {
                Show();
                Activate();
                if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
                Activate();
                Focus();
                inbg = false;
            };
            cm.MenuItems.Add(mi3);
            System.Windows.Forms.MenuItem mi1 = new System.Windows.Forms.MenuItem() { Text = "Play" };
            mi1.Click += (s, ev) =>
            {
                foreach (string moni in processes.Keys)
                {
                    sendData(processes[moni], "Play", moni);
                }
            };
            cm.MenuItems.Add(mi1);
            System.Windows.Forms.MenuItem mi2 = new System.Windows.Forms.MenuItem() { Text = "Pause" };
            mi2.Click += (s, ev) =>
            {
                foreach (string moni in processes.Keys)
                {
                    sendData(processes[moni], "Pause", moni);
                }
            };
            cm.MenuItems.Add(mi2);
            System.Windows.Forms.MenuItem mi0 = new System.Windows.Forms.MenuItem() { Text = "Change wallpaper" };
            mi0.Click += (s, ev) =>
            {
                beginWP();
            };
            cm.MenuItems.Add(mi0);
            System.Windows.Forms.MenuItem mi = new System.Windows.Forms.MenuItem() { Text = "Close" };
            mi.Click += (s, ev) =>
            {
                isclos = true;
                Close();
            };
            cm.MenuItems.Add(mi);
            ni.ContextMenu = cm;
            ni.Visible = true;
            //
            connectLocalTCP();
            //
            System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
            Dictionary<string, System.Drawing.Rectangle> monis = new Dictionary<string, System.Drawing.Rectangle>();
            t.Interval = 200;
            t.Tick += (sender, e) =>
            {
                if (processes.Count > 0 && loaded)
                {
                    List<string> queredScreen = new List<string>();
                    foreach (System.Windows.Forms.Screen s in System.Windows.Forms.Screen.AllScreens)
                    {
                        queredScreen.Add(s.DeviceName);
                        if (!monis.ContainsKey(s.DeviceName))
                        {
                            monis.Add(s.DeviceName, s.Bounds);
                            if(!processes.ContainsKey(s.DeviceName))
                            {
                                beginWP();
                            }
                        }
                        else
                        {
                            if(s.Bounds != monis[s.DeviceName])
                            {
                                beginWP();
                                monis[s.DeviceName] = s.Bounds;
                            }
                        }
                    }
                    for(int i = 0; i < processes.Count; i++)
                    {
                        string s = processes.Keys.ToList()[i];
                        if(!queredScreen.Contains(s))
                        {
                            Process[] pl = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
                            foreach (Process p in pl)
                            {
                                if (GetCommandLine(p).Contains("/moni \"" + s + "\""))
                                {
                                    p.Kill();
                                    processes.Remove(s);
                                    monis.Remove(s);
                                }
                            }
                        }
                    }
                }
            };
            t.Start();
            // 
            if (urls.Text != "") beginWP();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            inbg = false;
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
                            if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
                            Show();
                            Activate();
                            Focus();
                            Show();
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

        private async void beginWP()
        {
            try
            {
                loaded = false;
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
                    foreach (System.Windows.Forms.Screen mon in System.Windows.Forms.Screen.AllScreens)
                    {
                        /*if(monis.ContainsKey(mon.DeviceName))
                        {
                            //Computer\HKEY_CURRENT_USER\SOFTWARE\Microsoft\DirectX\UserGpuPreferences
                            Microsoft.Win32.RegistryKey key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\DirectX\\UserGpuPreferences", true);
                            string str = Assembly.GetExecutingAssembly().Location;
                            string gpu = monis[mon.DeviceName].Contains("NVIDIA") ? "2" : "1";
                            key.SetValue(str, "GpuPreference=" + gpu + ";");
                            //await Task.Delay(1000);
                        }*/
                        Process[] pl = Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
                        bool haveWPBG = false;
                        foreach (Process p in pl)
                        {
                            var cmdL = GetCommandLine(p);
                            if (cmdL != null && cmdL.Contains("/moni \"" + mon.DeviceName + "\"")) haveWPBG = true;
                        }
                        if (!haveWPBG)
                        {
                            Process p = new Process();
                            p.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + Process.GetCurrentProcess().ProcessName + ".exe";
                            p.StartInfo.Arguments = "--wp /startAfter " + startAfter + " /moni \"" + mon.DeviceName + "\"";
                            p.StartInfo.UseShellExecute = true;
                            p.Start();
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
                                        //Dispatcher.Invoke(() => Activate());
                                        string moni = stra.Split(' ')[1];
                                        sendChange(moni, PCtcp);
                                    }
                                    ns.BeginRead(read, 0, PCtcp.ReceiveBufferSize, asy, null);
                                }
                                catch
                                {
                                    Dispatcher.Invoke(() =>
                                    {
                                        if (!stopping && System.Windows.Forms.Screen.AllScreens.Contains(mon))
                                        {
                                            processes.Remove(mon.DeviceName);
                                            beginWP();
                                        }
                                    });
                                }
                            };
                            ns.BeginRead(read, 0, PCtcp.ReceiveBufferSize, asy, null);
                            processes.Add(mon.DeviceName, PCtcp);
                        }
                        else
                        {
                            if (processes.ContainsKey(mon.DeviceName))
                                sendChange(mon.DeviceName, processes[mon.DeviceName]);
                        }
                        startAfter += 15;
                    }
                    ok = true;
                    loaded = true;
                }
                else MessageBox.Show("Please put the path of a media or a folder to continue.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                /*Microsoft.Win32.RegistryKey keyk = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\DirectX\\UserGpuPreferences", true);
                string strk = Assembly.GetExecutingAssembly().Location;
                keyk.SetValue(strk, "GpuPreference=0;");*/
            }
            catch(Exception e)
            {
                MessageBox.Show("beginWP\n" + e.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetCommandLine(Process process)
        {
            using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT CommandLine FROM Win32_Process WHERE ProcessId = " + process.Id))
            using (ManagementObjectCollection objects = searcher.Get())
            {
                return objects.Cast<ManagementBaseObject>().SingleOrDefault()?["CommandLine"]?.ToString();
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
            string str = Assembly.GetExecutingAssembly().Location;
            try { key.DeleteValue("Wallpapeuhrs"); } catch { }
            if ((bool)startwithw.IsChecked) key.SetValue("Wallpapeuhrs", "\"" + str + "\" --background");
            beginWP();
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
            Process.Start("https://www.intel.com/content/www/us/en/download/19344/691496/intel-graphics-windows-dch-drivers.html");
        }
    }
}
