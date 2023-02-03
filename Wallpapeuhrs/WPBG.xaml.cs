using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Wallpapeuhrs.Utils;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace Wallpapeuhrs
{
    /// <summary>
    /// Logique d'interaction pour WPBG.xaml
    /// </summary>
    public partial class WPBG : Window
    {
        public System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
        int interval = 60 * 1000;
        string _curUrl = "";
        int fileIndex = 0;
        string curUrl
        {
            get
            {
                return _curUrl;
            }
            set
            {
                if(_curUrl != value) fileIndex = 0;
                _curUrl = value;
            }
        }
        public bool isDir = false;
        bool repeat = true;
        bool _fullrdm = true;
        bool fullrdm
        {
            get { return _fullrdm; }
            set
            {
                if (_fullrdm != value) fileIndex = 0;
                _fullrdm = value;
            }
        }
        public int startAfter = 0;
        public string moni = "";
        double volume = 0;
        public TcpClient tcp = new TcpClient();
        bool isDebug = false;
        bool allClients = false;
        bool isEdgeEngine = true;
        DebugWindow dw;
        UserControl med;

        public WPBG(string moni, int startAfter, int engine)
        {
            this.moni = moni;
            this.startAfter = startAfter;
            this.isEdgeEngine = engine <= 3;
            //
            if(isEdgeEngine)
            {
                med = new MediaVW(this, engine);
            }
            else
            {
                med = new Media(this);
            }
            InitializeComponent();
            AddChild(med);
            WindowStartupLocation = WindowStartupLocation.Manual;
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
                    W32.SetWindowPos(new WindowInteropHelper(this).Handle, IntPtr.Zero, s.Bounds.X + left, s.Bounds.Y + top, s.Bounds.Width, s.Bounds.Height, W32.SetWindowPosFlags.NoZOrder | W32.SetWindowPosFlags.AsynWindowPos | W32.SetWindowPosFlags.NoRedraw);
                    anSize = s.Bounds;
                    return;
                }
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Worker.Init();
            IntPtr p = new WindowInteropHelper(this).Handle;
            if (W32.SetParent(p, Worker.workerw) == IntPtr.Zero)
            {
                MessageBox.Show("Cannot change the parent to WorkerW.\nCode error Win32 " + Marshal.GetLastWin32Error(), "Wallpapeuhrs - Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
            log("curDisplay : " + moni);
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
                                    if (str.StartsWith("Volume")) volume = Convert.ToDouble(str.Split('=')[1], CultureInfo.InvariantCulture);
                                    if (str.StartsWith("Interval")) interval = Convert.ToInt32(str.Split('=')[1]);
                                    if (str.StartsWith("Repeat")) repeat = Convert.ToBoolean(str.Split('=')[1]);
                                    if (str.StartsWith("Fullrdm")) fullrdm = Convert.ToBoolean(str.Split('=')[1]);
                                    if (str.StartsWith("ForceReset")) fileIndex = 0;
                                    if (str.StartsWith("InitOK"))
                                    {
                                        //Last parameter. We can start display wp.
                                        beginWP();
                                    }
                                    if (str.StartsWith("Play"))
                                    {
                                        changePlayerState(true);
                                        timer.Start();
                                        if(isEdgeEngine) (med as MediaVW).nextChange = System.Environment.TickCount + timePaused;
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
                                        if(isEdgeEngine) timePaused = (med as MediaVW).nextChange - System.Environment.TickCount;
                                        else timePaused = (med as Media).nextChange - System.Environment.TickCount;
                                    }
                                    if (str.StartsWith("ChangeTheme"))
                                    {
                                        string theme = str.Split('=')[1];
                                        double value = Convert.ToDouble(str.Split('=')[2], CultureInfo.InvariantCulture);
                                        if(isEdgeEngine) (med as MediaVW).changeFilter(theme, value);
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
                        else MessageBox.Show(ee.ToString(), "Wallpapeuhrs - Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            };
            ns.BeginRead(read, 0, tcp.ReceiveBufferSize, asy, null);
            if(!isEdgeEngine) MainWindow.sendData(tcp, "READY " + moni + " ", null);
        }

        float timePaused = 0;

        private void Timer_Tick(object sender, EventArgs ee)
        {
            try
            {
                if (!curPlay)
                {
                    if (isEdgeEngine) (med as MediaVW).nextChange += 1 * 1000;
                    else (med as Media).nextChange += 1 * 1000;
                }
                try
                {
                    if (isEdgeEngine)
                    {
                        if (curUrl != "" && isDir && (med as MediaVW).nextChange <= System.Environment.TickCount)
                        {
                            (med as MediaVW).nextChange = System.Environment.TickCount + interval * 1000;
                            (med as MediaVW).changeUrl(getNewMedia());
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
            if(isEdgeEngine) (med as MediaVW).volume = volume;
            else (med as Media).volume = volume;
            if(isEdgeEngine) (med as MediaVW).repeat = repeat;
            else (med as Media).repeat = repeat;
            try
            {
                curPlay = true;
                if (isEdgeEngine) (med as MediaVW).changeUrl(newUrl);
                else (med as Media).changeUrl(newUrl);
                //changeNativeWallpaper(newUrl);
            }
            catch (Exception e)
            {
                MessageBox.Show("Unable to load the new media (" + newUrl + ") : " + e.Message + "\n" + e.StackTrace, "Wallpapeuhrs - Error");
            }
            if (isEdgeEngine) (med as MediaVW).nextChange = System.Environment.TickCount + (interval + interval / 4 * startAfter) * 1000;
            else (med as Media).nextChange = System.Environment.TickCount + (interval + interval / 4 * startAfter) * 1000;
            timer.Start();
        }

        bool isOk = true;

        public async void changeNativeWallpaper(MemoryStream msVW)
        {
            try
            {
                if (System.Windows.Forms.Screen.PrimaryScreen.DeviceName == moni)
                {
                    string data = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\Wallpapeuhrs\\NativeWallpaper\\";
                    //var a = VideoThumbnail.getVideoThumbnail(newUrl);
                    //a.Save(data + "thumb.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                    /*path = path.Replace("\\\\", "\\");
                    StorageFile sf = await StorageFile.GetFileFromPathAsync(path);
                    ThumbnailOptions opt = ThumbnailOptions.UseCurrentScale;
                    StorageItemThumbnail sit = await sf.GetThumbnailAsync(Windows.Storage.FileProperties.ThumbnailMode.SingleItem, 1440, opt);
                    if (sit != null)
                    {
                        Directory.CreateDirectory(data);
                        var fileStream = File.Create(data + "thumb.png");
                        sit.AsStreamForRead().Seek(0, SeekOrigin.Begin);
                        sit.AsStreamForRead().CopyTo(fileStream);
                        fileStream.Close();
                        NativeWallpaper.changeWallpaper(data + "thumb.png");
                    }*/
                    MemoryStream ms;
                    if (isEdgeEngine) ms = msVW;
                    else ms = await (med as Media).screenshot();
                    Directory.CreateDirectory(data);
                    var fileStream = File.Create(data + "thumb.png");
                    ms.WriteTo(fileStream);
                    fileStream.Close();
                    NativeWallpaper.changeWallpaper(data + "thumb.png");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Unable to load the new thumb wallpaper : " + ex.ToString(), "Wallpapeuhrs - Error");
            }
        }

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
                    if (fileIndex >= realList.Count) fileIndex = 0;
                    int newR = fullrdm ? rng.Next(0, realList.Count()) : fileIndex;
                    isOk = true;
                    media = realList[newR];
                    fileIndex += 1;
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
                    MessageBox.Show("The file or folder : \"" + curUrl + "\" doesn't exist. Please verify the path entered or if the file or folder exists really.", "Wallpapeuhrs - Error", MessageBoxButton.OK, MessageBoxImage.Error);
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
            if(isEdgeEngine) (med as MediaVW).webview.Dispose();
            else (med as Media).myHostControl.Dispose();
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
            curPlay = play;
            if(isEdgeEngine) (med as MediaVW).changePlayerState(play);
            else (med as Media).changePlayerState(play);
            //if (play) W32.SetThreadExecutionState(W32.EXECUTION_STATE.ES_CONTINUOUS);
        }

        private void main_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.System && e.SystemKey == Key.F4)
            {
                e.Handled = true;
            }
        }
    }
}
