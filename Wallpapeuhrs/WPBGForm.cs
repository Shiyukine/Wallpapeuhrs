using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace Wallpapeuhrs
{
    public partial class WPBGForm : Form
    {
        public System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        int interval = 60 * 1000;
        string curUrl = "";
        public bool isDir = false;
        bool autostop = true;
        bool repeat = true;
        public int startAfter = 0;
        public string moni = "";
        double volume = 0;
        public TcpClient tcp = new TcpClient();
        bool isDebug = false;
        bool allClients = false;
        bool isEdgeEngine = true;
        DebugWindow dw;
        object med;

        public WPBGForm(string moni, int startAfter, int engine)
        {
            this.moni = moni;
            this.startAfter = startAfter;
            this.isEdgeEngine = engine == 0;
            //
            if (isEdgeEngine)
            {
                med = new MediaVW2();
                ((MediaVW2)med).parent = this;
            }
            else
            {
                med = new Media();
                //((Media)med).parent = this;
            }
            InitializeComponent();
            FormBorderStyle = FormBorderStyle.None;
            Controls.Add((MediaVW2)med);
            (med as MediaVW2).Dock = DockStyle.Fill;
            //
            if (isDebug && (allClients || System.Windows.Forms.Screen.PrimaryScreen.DeviceName == moni))
            {
                dw = new DebugWindow(moni);
                dw.Show();
            }
            //
            timer.Tick += Timer_Tick;
            timer.Interval = 1000;
            //
            Show();
            resizeApp();
            Worker.Init();
            IntPtr p = this.Handle;
            if (W32.SetParent(p, Worker.workerw) == IntPtr.Zero)
            {
                MessageBox.Show("Cannot change the parent to WorkerW.\nCode error Win32 " + Marshal.GetLastWin32Error(), "Wallpapeuhrs - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        System.Drawing.Rectangle anSize = new System.Drawing.Rectangle();

        private void resizeApp()
        {
            foreach (System.Windows.Forms.Screen s in System.Windows.Forms.Screen.AllScreens)
            {
                if (s.DeviceName == moni && anSize != s.Bounds)
                {
                    //Code from TouchTransporter
                    int top = 0;
                    int left = 0;
                    foreach (System.Windows.Forms.Screen s2 in System.Windows.Forms.Screen.AllScreens)
                    {
                        if (s2.Bounds.Y < top && s2.Bounds.Y < 0) top = s2.Bounds.Y * -1;
                        if (s2.Bounds.X < left && s2.Bounds.X < 0) left = s2.Bounds.X * -1;
                    }
                    //
                    W32.SetWindowPos(this.Handle, IntPtr.Zero, s.Bounds.X + left, s.Bounds.Y + top, s.Bounds.Width, s.Bounds.Height, W32.SetWindowPosFlags.NoZOrder | W32.SetWindowPosFlags.AsynWindowPos | W32.SetWindowPosFlags.NoRedraw);
                    anSize = s.Bounds;
                    return;
                }
            }
        }

        private async void Window_Loaded(object sender, EventArgs e)
        {
            
            log("curDisplay winforms : " + moni);
            //
            await tcp.ConnectAsync("127.0.0.1", 30930);
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
                    log(stra);
                    if (stra == "") BeginInvoke(new Action(() => Close()));
                    else
                    {
                        foreach (string s in stra.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (s.StartsWith(moni + ": "))
                            {
                                string str = s.Replace(moni + ": ", "");
                                BeginInvoke(new Action(() =>
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
                                    if (str.StartsWith("Volume")) volume = Convert.ToDouble(str.Split('=')[1], CultureInfo.InvariantCulture);
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
                                        if (isEdgeEngine) (med as MediaVW2).nextChange = System.Environment.TickCount + timePaused;
                                        else (med as Media).nextChange = System.Environment.TickCount + timePaused;
                                    }
                                    if (str.StartsWith("VPlay"))
                                    {
                                        changePlayerState(true);
                                    }
                                    if (str.StartsWith("Pause"))
                                    {
                                        changePlayerState(false);
                                        timer.Stop();
                                        if (isEdgeEngine) timePaused = (med as MediaVW2).nextChange - System.Environment.TickCount;
                                        else timePaused = (med as Media).nextChange - System.Environment.TickCount;
                                    }
                                    if (str.StartsWith("ChangeTheme"))
                                    {
                                        string theme = str.Split('=')[1];
                                        double value = Convert.ToDouble(str.Split('=')[2], CultureInfo.InvariantCulture);
                                        if (isEdgeEngine) (med as MediaVW2).changeFilter(theme, value);
                                    }
                                }));
                            }
                        }
                        ns.BeginRead(read, 0, tcp.ReceiveBufferSize, asy, null);
                    }
                }
                catch (Exception ee)
                {
                    BeginInvoke(new Action(() =>
                    {
                        if (ee.GetType() == typeof(IOException)) Close();
                        else MessageBox.Show(ee.ToString(), "Wallpapeuhrs - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
            };
            ns.BeginRead(read, 0, tcp.ReceiveBufferSize, asy, null);
            if (!isEdgeEngine) MainWindow.sendData(tcp, "READY " + moni + " ", null);
        }

        float timePaused = 0;

        private void Timer_Tick(object sender, EventArgs ee)
        {
            try
            {
                if (!curPlay)
                {
                    if (isEdgeEngine) (med as MediaVW2).nextChange += 1 * 1000;
                    else (med as Media).nextChange += 1 * 1000;
                }
                try
                {
                    if (isEdgeEngine)
                    {
                        if (curUrl != "" && isDir && (med as MediaVW2).nextChange <= System.Environment.TickCount)
                        {
                            (med as MediaVW2).nextChange = System.Environment.TickCount + interval * 1000;
                            (med as MediaVW2).changeUrl(getNewMedia());
                        }
                    }
                    else
                    {
                        if (curUrl != "" && isDir && (med as Media).nextChange <= System.Environment.TickCount)
                        {
                            (med as Media).nextChange = System.Environment.TickCount + interval * 1000;
                            (med as Media).changeUrl(getNewMedia());
                        }
                    }
                }
                catch (Exception e)
                {
                    MessageBox.Show("Unable to load the new media : " + e.Message + "\n" + e.StackTrace, "Wallpapeuhrs - Error");
                }
            }
            catch (Exception e)
            {
                MessageBox.Show("" + e.Message + "\n" + e.StackTrace, "Wallpapeuhrs - Error");
            }
        }

        public void beginWP()
        {
            resizeApp();
            isOk = true;
            if (timer.Enabled) timer.Stop();
            string newUrl = getNewMedia();
            if (isEdgeEngine) (med as MediaVW2).volume = volume;
            else (med as Media).volume = volume;
            if (isEdgeEngine) (med as MediaVW2).repeat = repeat;
            else (med as Media).repeat = repeat;
            try
            {
                curPlay = true;
                if (isEdgeEngine) (med as MediaVW2).changeUrl(newUrl);
                else (med as Media).changeUrl(newUrl);
            }
            catch (Exception e)
            {
                MessageBox.Show("Unable to load the new media (" + newUrl + ") : " + e.Message + "\n" + e.StackTrace, "Wallpapeuhrs - Error");
            }
            if (isEdgeEngine) (med as MediaVW2).nextChange = System.Environment.TickCount + (interval + interval / 4 * startAfter) * 1000;
            else (med as Media).nextChange = System.Environment.TickCount + (interval + interval / 4 * startAfter) * 1000;
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
                    foreach (List<string> ext in App.types.Values)
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
                    int newR = rng.Next(0, realList.Count);
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
                    MessageBox.Show("The file or folder : \"" + curUrl + "\" doesn't exist. Please verify the path entered or if the file or folder exists really.", "Wallpapeuhrs - Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            if (isDebug && (allClients || System.Windows.Forms.Screen.PrimaryScreen.DeviceName == moni)) BeginInvoke(new Action(() => dw.Close()));
            Hide();
            W32.SetParent(this.Handle, IntPtr.Zero);
            if (isEdgeEngine)
            {
                (med as MediaVW2).cwb.Dispose();
            }
            else (med as Media).myHostControl.Dispose();
            W32.SetParent(Worker.workerw, IntPtr.Zero);
            App.Current.Shutdown();
        }

        private void Window_Activated(object sender, EventArgs e)
        {

        }

        public void log(string log)
        {
            if (isDebug && (allClients || System.Windows.Forms.Screen.PrimaryScreen.DeviceName == moni)) BeginInvoke(new Action(() => dw.log(log)));
        }

        bool curPlay = true;

        public void changePlayerState(bool play)
        {
            curPlay = play;
            if (isEdgeEngine) (med as MediaVW2).changePlayerState(play);
            else (med as Media).changePlayerState(play);
        }

        private void main_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Alt && e.KeyCode == Keys.F4)
            {
                e.Handled = true;
            }
        }
    }
}
