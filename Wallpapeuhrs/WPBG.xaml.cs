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
                                    med.changePlayerState(true);
                                    timer.Start();
                                    med.nextChange = System.Environment.TickCount + timePaused;
                                }
                                if (str.StartsWith("Pause"))
                                {
                                    med.changePlayerState(false);
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

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        float timePaused = 0;

        private void Timer_Tick(object sender, EventArgs ee)
        {
            try
            {
                IntPtr pp = IntPtr.Zero;
                W32.EnumWindows(new W32.EnumWindowsProc((tophandle, topparamhandle) =>
                {
                    IntPtr p = W32.FindWindowEx(tophandle,
                                                IntPtr.Zero,
                                                "SHELLDLL_DefView",
                                                IntPtr.Zero);
                    if (p != IntPtr.Zero)
                    {
                        pp = tophandle;
                        return true;
                    }
                    return true;
                }), IntPtr.Zero);
                bool pause = false;
                if (autostop)
                {
                    if (pp != IntPtr.Zero && GetForegroundWindow() != pp)
                    {
                        med.changePlayerState(false);
                        pause = true;
                        med.nextChange += 1 * 1000;
                    }
                    else
                    {
                        med.changePlayerState(true);
                    }
                }
                if (!pause && curUrl != "" && !System.IO.Path.GetExtension(curUrl).Contains("."))
                {
                    if (med.nextChange <= System.Environment.TickCount)
                    {
                        try
                        {
                            med.changeUrl(getNewMedia());
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show("Unable to load the new media : " + e.Message + "\n" + e.StackTrace, "Error");
                        }
                        med.nextChange = System.Environment.TickCount + interval * 1000;
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
            if (timer.Enabled) timer.Stop();
            string newUrl = getNewMedia();
            med.volume = System.Windows.Forms.Screen.PrimaryScreen.DeviceName != moni ? 0 : volume;
            med.repeat = repeat;
            try
            {
                med.changeUrl(newUrl);
            }
            catch (Exception e)
            {
                MessageBox.Show("Unable to load the new media (" + newUrl + ") : " + e.Message + "\n" + e.StackTrace, "Error");
            }
            med.nextChange = System.Environment.TickCount + (startAfter + interval) * 1000;
            timer.Start();
        }

        private string getNewMedia()
        {
            try
            {
                if (!System.IO.Path.GetExtension(curUrl).Contains("."))
                {
                    Random rng = new Random(Guid.NewGuid().GetHashCode());
                    IEnumerable<string> list = Directory.EnumerateFiles(curUrl + "\\", "*");
                    int newR = rng.Next(0, list.Count());
                    int index = 0;
                    foreach (string f in list)
                    {
                        foreach (List<string> ext in App.types.Values)
                        {
                            if (ext.Contains(System.IO.Path.GetExtension(f)) && newR == index) return f;
                        }
                        index++;
                    }
                }
                else return curUrl;
            }
            catch
            {
                MessageBox.Show("The file or folder : \"" + curUrl + "\" doesn't exist. Please verify the path entered or if the file or folder exists really.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
    }
}
