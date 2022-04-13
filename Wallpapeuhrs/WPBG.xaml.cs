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
        DebugWindow dw;

        public WPBG(string moni, int startAfter)
        {
            resizeApp();
            //
            InitializeComponent();
            //
            if (isDebug && System.Windows.Forms.Screen.PrimaryScreen.DeviceName == moni)
            {
                dw = new DebugWindow();
                dw.Show();
            }
            this.moni = moni;
            this.startAfter = startAfter;
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
                    Top = s.Bounds.Y;
                    Left = s.Bounds.X;
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
                    if (stra == "") Dispatcher.Invoke(() => Close());
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

        private void Timer_Tick(object sender, EventArgs ee)
        {
            try
            {
                bool pause = false;
                if (autostop)
                {
                    if (!win.Contains(W32.GetForegroundWindow().ToInt32()))
                    {
                        changePlayerState(false);
                        pause = true;
                        med.nextChange += 1 * 1000;
                    }
                    else
                    {
                        changePlayerState(true);
                    }
                }
                if (!pause && curUrl != "" && isDir)
                {
                    if (med.nextChange <= System.Environment.TickCount)
                    {
                        try
                        {
                            med.changeUrl(getNewMedia());
                            med.nextChange = System.Environment.TickCount + interval * 1000;
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("Unable to load the new media : " + e.Message + "\n" + e.StackTrace, "Error");
                        }
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
                anPlay = true;
                med.changeUrl(newUrl);
            }
            catch (Exception e)
            {
                MessageBox.Show("Unable to load the new media (" + newUrl + ") : " + e.Message + "\n" + e.StackTrace, "Error");
            }
            med.nextChange = System.Environment.TickCount + (startAfter + interval) * 1000;
            timer.Start();
        }

        bool isOk = true;

        private string getNewMedia()
        {
            try
            {
                if (isDir)
                {
                    Random rng = new Random(Guid.NewGuid().GetHashCode());
                    IEnumerable<string> list = Directory.EnumerateFiles(curUrl + "\\", "*");
                    int newR = rng.Next(0, list.Count());
                    int index = 0;
                    foreach (string f in list)
                    {
                        foreach (List<string> ext in App.types.Values)
                        {
                            if (ext.Contains(System.IO.Path.GetExtension(f)) && newR == index)
                            {
                                isOk = true;
                                return f;
                            }
                        }
                        index++;
                    }
                }
                else
                {
                    isOk = true;
                    return curUrl;
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
            return "";
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(isDebug && System.Windows.Forms.Screen.PrimaryScreen.DeviceName == moni) Dispatcher.Invoke(() => dw.Close());
            W32.SetParent(new WindowInteropHelper(this).Handle, IntPtr.Zero);
            W32.SetParent(Worker.workerw, IntPtr.Zero);
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            
        }

        public void log(string log)
        {
            if (isDebug && System.Windows.Forms.Screen.PrimaryScreen.DeviceName == moni) Dispatcher.Invoke(() => dw.log(log));
        }

        bool anPlay = true;

        public void changePlayerState(bool play)
        {
            if (play == anPlay) return;
            anPlay = play;
            IntPtr pp = IntPtr.Zero;
            string[] wins = new string[] { "Task View", "Start", "Search" };
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
            //
            if(play || !autostop || (!play && autostop && !win.Contains(W32.GetForegroundWindow().ToInt32()))) 
                med.changePlayerState(play);
        }
    }
}
