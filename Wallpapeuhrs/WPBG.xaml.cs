using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Management;
using System.Net.Sockets;
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
using System.Windows.Shapes;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using Windows.Media;
using Windows.Media.Core;

namespace Wallpapeuhrs
{
    /// <summary>
    /// Logique d'interaction pour WPBG.xaml
    /// </summary>
    public partial class WPBG : Window
    {
        public System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        int interval = 60 * 1000;
        string curUrl = "";
        bool isDir = false;
        bool autostop = true;
        bool repeat = true;
        public int startAfter = 0;
        public string moni = "";
        double volume = 0;
        TcpClient tcp = new TcpClient();
        bool isDebug = false;
        bool allClients = false;
        DebugWindow dw;

        public WPBG(string moni, int startAfter)
        {
            this.moni = moni;
            this.startAfter = startAfter;
            //
            InitializeComponent();
            //
            if (isDebug && (allClients || System.Windows.Forms.Screen.PrimaryScreen.DeviceName == moni))
            {
                dw = new DebugWindow();
                dw.Show();
            }
            //
            resizeApp();
            //
            med.parent = this;
            //
            timer.Tick += Timer_Tick;
            timer.Interval = 1000;
            //
            Show();
        }

        System.Drawing.Rectangle anSize = new System.Drawing.Rectangle();

        private void resizeApp()
        {
            foreach (System.Windows.Forms.Screen s in System.Windows.Forms.Screen.AllScreens)
            {
                if (s.DeviceName == moni && anSize != s.Bounds)
                {
                    Width = s.Bounds.Width;
                    Height = s.Bounds.Height;
                    //Code from TouchTransporter
                    double top = 0;
                    double left = 0;
                    foreach (System.Windows.Forms.Screen s2 in System.Windows.Forms.Screen.AllScreens)
                    {
                        if (s2.Bounds.Y < top && s2.Bounds.Y < 0) top = s2.Bounds.Y * -1;
                        if (s2.Bounds.X < left && s2.Bounds.X < 0) left = s2.Bounds.X * -1;
                    }
                    //
                    Left = s.Bounds.X + left;
                    Top = s.Bounds.Y + top;
                    anSize = s.Bounds;
                    return;
                }
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Worker.Init();
            IntPtr p = new WindowInteropHelper(this).Handle;
            W32.SetParent(p, Worker.workerw);
            //
            tcp.Connect("localhost", 30930);
            NetworkStream ns = tcp.GetStream();
            byte[] read = new byte[tcp.ReceiveBufferSize];
            bool isList = false;
            //
            AsyncCallback asy = null;
            asy = (ar) =>
            {
                try
                {
                    int bytesRead = ns.EndRead(ar);
                    string stra = Encoding.Unicode.GetString(read, 0, bytesRead);
                    if (stra == "") Dispatcher.Invoke(() => App.Current.Shutdown());
                    else
                    {
                        foreach (string s in stra.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (s.StartsWith(moni + ": "))
                            {
                                string str = s.Replace(moni + ": ", "");
                                Dispatcher.Invoke(() =>
                                {
                                    if (str.StartsWith("Endliste")) isList = false;
                                    if (isList)
                                    {
                                        curUrl = str;
                                        isDir = !File.Exists(curUrl);
                                    }
                                    if (str.StartsWith("Liste")) isList = true;
                                    //
                                    str = str.Replace(",", ".");
                                    if (str.StartsWith("Volume")) volume = Convert.ToDouble(str.Split('=')[1]);
                                    if (str.StartsWith("Interval")) interval = Convert.ToInt32(str.Split('=')[1]);
                                    if (str.StartsWith("Repeat")) repeat = Convert.ToBoolean(str.Split('=')[1]);
                                    if (str.StartsWith("Autostop"))
                                    {
                                        autostop = Convert.ToBoolean(str.Split('=')[1]);
                                        beginWP();
                                    }
                                    if (str.StartsWith("Play"))
                                    {
                                        changePlayerState(true);
                                        timer.Start();
                                        med.nextChange = System.Environment.TickCount + timePaused;
                                    }
                                    if (str.StartsWith("Pause"))
                                    {
                                        changePlayerState(false);
                                        timer.Stop();
                                        timePaused = med.nextChange - System.Environment.TickCount;
                                    }
                                });
                            }
                        }
                        ns.BeginRead(read, 0, tcp.ReceiveBufferSize, asy, null);
                    }
                }
                catch (Exception ee)
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (ee.GetType() == typeof(IOException)) Close();
                        else MessageBox.Show(ee.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            };
            ns.BeginRead(read, 0, tcp.ReceiveBufferSize, asy, null);
            MainWindow.sendData(tcp, "READY " + moni + " ", null);
        }

        float timePaused = 0;
        List<int> win = new List<int>();
        int anWin = -1;

        private void Timer_Tick(object sender, EventArgs ee)
        {
            try
            {
                if (autostop)
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
                if(!curPlay) med.nextChange += 1 * 1000;
                if (curUrl != "" && isDir && med.nextChange <= System.Environment.TickCount)
                {
                    try
                    {
                        med.nextChange = System.Environment.TickCount + interval * 1000;
                        med.changeUrl(getNewMedia());
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show("Unable to load the new media : " + e.Message + "\n" + e.StackTrace, "Error");
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("" + e.Message + "\n" + e.StackTrace, "Error");
            }
        }

        public void beginWP()
        {
            resizeApp();
            isOk = true;
            if (timer.Enabled) timer.Stop();
            string newUrl = getNewMedia();
            med.volume = System.Windows.Forms.Screen.PrimaryScreen.DeviceName != moni ? 0 : volume;
            med.repeat = repeat;
            try
            {
                curPlay = true;
                med.changeUrl(newUrl);
            }
            catch (Exception e)
            {
                MessageBox.Show("Unable to load the new media (" + newUrl + ") : " + e.Message + "\n" + e.StackTrace, "Error");
            }
            med.nextChange = System.Environment.TickCount + (interval + interval / 4 * startAfter) * 1000;
            timer.Start();
        }

        bool isOk = true;

        private string getNewMedia()
        {
            string media = "";
            try
            {
                if (isDir)
                {
                    Random rng = new Random(Guid.NewGuid().GetHashCode());
                    IEnumerable<string> list = Directory.EnumerateFiles(curUrl + "\\", "*");
                    List<string> realList = new List<string>();
                    List<string> exts = new List<string>();
                    foreach(List<string> ext in App.types.Values)
                    {
                        exts.AddRange(ext);
                    }
                    foreach (string f in list)
                    {
                        if (exts.Contains(System.IO.Path.GetExtension(f)))
                        {
                            realList.Add(f);
                        }
                    }
                    int newR = rng.Next(0, realList.Count());
                    isOk = true;
                    media = realList[newR];
                }
                else
                {
                    isOk = true;
                    media = curUrl;
                }
            }
            catch
            {
                if (isOk)
                {
                    isOk = false;
                    MessageBox.Show("The file or folder : \"" + curUrl + "\" doesn't exist. Please verify the path entered or if the file or folder exists really.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            /*async void verifyWebM()
            {
                await Task.Run(() =>
                {
                    if (media.EndsWith(".webm") && (isDir || System.Windows.Forms.Screen.PrimaryScreen.DeviceName == moni))
                    {
                        try
                        {
                            PackageManager packageManager = new PackageManager();
                            IEnumerable<Package> pkgs = packageManager.FindPackagesForUser(string.Empty, "Microsoft.AV1VideoExtension");
                            if (pkgs.Count() == 0)
                            {
                                MessageBoxResult mbr = MessageBox.Show("AV1 video extension for .webm videos is not installed. Would you install it ?", "Wallpapeuhrs - Error", MessageBoxButton.YesNo, MessageBoxImage.Error);
                                if (mbr == MessageBoxResult.Yes)
                                {
                                    Process.Start("ms-windows-store://pdp?productId=9mvzqvxjbq9v");
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.ToString());
                        }
                    }
                });
            }
            verifyWebM();*/
            return media;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(isDebug && (allClients || System.Windows.Forms.Screen.PrimaryScreen.DeviceName == moni)) Dispatcher.Invoke(() => dw.Close());
            //W32.SetParent(new WindowInteropHelper(this).Handle, IntPtr.Zero);
            med.myHostControl.Dispose();
            W32.SetParent(Worker.workerw, IntPtr.Zero);
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            
        }

        public void log(string log)
        {
            if (isDebug && (allClients || System.Windows.Forms.Screen.PrimaryScreen.DeviceName == moni)) Dispatcher.Invoke(() => dw.log(log));
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
            if (autostop && anLength != win.Count) play = true;
            if (play || !autostop || anLength == win.Count)
            {
                //log("changePlayerState " + play);
                curPlay = play;
                med.changePlayerState(play);
            }
        }
    }
}
