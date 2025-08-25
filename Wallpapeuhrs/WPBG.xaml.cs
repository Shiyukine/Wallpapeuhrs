using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
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
        int engine = -1;
        DebugWindow dw;
        public Microsoft.UI.Xaml.Controls.UserControl med;
        //public Microsoft.UI.Xaml.Hosting.DesktopWindowXamlSource m_dwxs;
        Icon _icon;
        AppWindow appWin;
        IntPtr webviewHandle = IntPtr.Zero;

        public WPBG(string moni, int startAfter, int engine)
        {
            this.moni = moni;
            this.startAfter = startAfter;
            this.isEdgeEngine = engine <= 3;
            this.engine = engine;
            //
            if (isDebug && (allClients || System.Windows.Forms.Screen.PrimaryScreen.DeviceName == moni))
            {
                dw = new DebugWindow(moni);
                dw.Show();
            }
            //
            InitializeComponent();
            Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().TryEnqueue(
        Microsoft.UI.Dispatching.DispatcherQueuePriority.Low,
        new Microsoft.UI.Dispatching.DispatcherQueueHandler(() =>
        {
            Window_Loaded(this, new RoutedEventArgs());
        }));
            appWin = AppWindow;
            if (isEdgeEngine)
            {
                med = new MediaVW(this, engine);
                var alreadySet = false;
                (med as MediaVW).webview.NavigationCompleted += async (s, e) =>
                {
                    if (alreadySet) return;
                    alreadySet = true;
                    setWorker();
                };
            }
            else if(engine == 4)
            {
                med = new Media(this);
                setWorker();
            }
            else
            {
                med = new MediaEffect(this);
                setWorker();
            }
            Content = med;
            if (isEdgeEngine) (med as MediaVW).init();
            else if(med is MediaEffect) (med as MediaEffect).init();
            else (med as Media).init();
            //
            //WindowStartupLocation = WindowStartupLocation.Manual;
            timer.Tick += Timer_Tick;
            timer.Interval = 1000;
            //
            //Show();
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
                    W32.SetWindowPos(WinRT.Interop.WindowNative.GetWindowHandle(this), IntPtr.Zero, s.Bounds.X + left, s.Bounds.Y + top, s.Bounds.Width, s.Bounds.Height, W32.SetWindowPosFlags.NoZOrder | W32.SetWindowPosFlags.AsynWindowPos | W32.SetWindowPosFlags.NoRedraw);
                    if(webviewHandle != IntPtr.Zero)
                    {
                        W32.RECT workerSize = new W32.RECT();
                        W32.GetWindowRect(Worker.workerw, out workerSize);
                        W32.SetWindowPos(webviewHandle, IntPtr.Zero, -workerSize.Left + appWin.Position.X, -workerSize.Top + appWin.Position.Y, 0, 0, W32.SetWindowPosFlags.AsynWindowPos | W32.SetWindowPosFlags.NoRedraw | W32.SetWindowPosFlags.NoZOrder | W32.SetWindowPosFlags.NoSize);
                    }
                    anSize = s.Bounds;
                    return;
                }
            }
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            log("curDisplay : " + moni);
            //
            await tcp.ConnectAsync("127.0.0.1", 30930);
            NetworkStream ns = tcp.GetStream();
            byte[] read = new byte[tcp.ReceiveBufferSize];
            bool isList = false;
            //
            AsyncCallback asy = null;
            asy = async (ar) =>
            {
                try
                {
                    int bytesRead = ns.EndRead(ar);
                    string stra = Encoding.Unicode.GetString(read, 0, bytesRead);
                    log(stra);
                    if (stra == "") DispatcherQueue.TryEnqueue(() => App.Current.Shutdown());
                    else
                    {
                        foreach (string s in stra.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            if (s.StartsWith(moni + ": "))
                            {
                                string str = s.Replace(moni + ": ", "");
                                DispatcherQueue.TryEnqueue(() =>
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
                                    if (str.StartsWith("Volume"))
                                    {
                                        volume = Convert.ToDouble(str.Split('=')[1], CultureInfo.InvariantCulture);
                                        if (isEdgeEngine) (med as MediaVW).changeVolume(volume);
                                        else if (med is MediaEffect) (med as MediaEffect).changeVolume(volume);
                                        else (med as Media).changeVolume(volume);
                                    }
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
                                        else if (med is MediaEffect) (med as MediaEffect).nextChange = System.Environment.TickCount + timePaused;
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
                                        else if (med is MediaEffect) timePaused = (med as MediaEffect).nextChange - System.Environment.TickCount;
                                        else timePaused = (med as Media).nextChange - System.Environment.TickCount;
                                    }
                                    if (str.StartsWith("ChangeTheme"))
                                    {
                                        string theme = str.Split('=')[1];
                                        double value = Convert.ToDouble(str.Split('=')[2], CultureInfo.InvariantCulture);
                                        if(isEdgeEngine) (med as MediaVW).changeFilter(theme, value);
                                        else if(med is MediaEffect) (med as MediaEffect).changeFilter(theme, value);
                                    }
                                });
                            }
                        }
                        ns.BeginRead(read, 0, tcp.ReceiveBufferSize, asy, null);
                    }
                }
                catch (Exception ee)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (ee.GetType() == typeof(IOException)) Close();
                        else System.Windows.MessageBox.Show(ee.ToString(), "Wallpapeuhrs - Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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
                    else if (med is MediaEffect) (med as MediaEffect).nextChange += 1 * 1000;
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
                    else if (med is MediaEffect)
                    {
                        if (curUrl != "" && isDir && (med as MediaEffect).nextChange <= System.Environment.TickCount)
                        {
                            (med as MediaEffect).nextChange = System.Environment.TickCount + interval * 1000;
                            (med as MediaEffect).changeUrl(getNewMedia());
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
                    System.Windows.MessageBox.Show("Unable to load the new media : " + e.Message + "\n" + e.StackTrace, "Wallpapeuhrs - Error");
                }
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("" + e.Message + "\n" + e.StackTrace, "Wallpapeuhrs - Error");
            }
        }

        public void beginWP()
        {
            resizeApp();
            isOk = true;
            if (timer.Enabled) timer.Stop();
            string newUrl = getNewMedia();
            if(isEdgeEngine) (med as MediaVW).volume = volume;
            else if (med is MediaEffect) (med as MediaEffect).volume = volume;
            else (med as Media).volume = volume;
            if(isEdgeEngine) (med as MediaVW).repeat = repeat;
            else if (med is MediaEffect) (med as MediaEffect).repeat = repeat;
            else (med as Media).repeat = repeat;
            try
            {
                curPlay = true;
                if (isEdgeEngine) (med as MediaVW).changeUrl(newUrl);
                else if (med is MediaEffect) (med as MediaEffect).changeUrl(newUrl);
                else (med as Media).changeUrl(newUrl);
                //changeNativeWallpaper(newUrl);
            }
            catch (Exception e)
            {
                System.Windows.MessageBox.Show("Unable to load the new media (" + newUrl + ") : " + e.Message + "\n" + e.StackTrace, "Wallpapeuhrs - Error");
            }
            if (isEdgeEngine) (med as MediaVW).nextChange = System.Environment.TickCount + (interval + interval / 4 * startAfter) * 1000;
            else if (med is MediaEffect) (med as MediaEffect).nextChange = System.Environment.TickCount + (interval + interval / 4 * startAfter) * 1000;
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
                    else if (med is MediaEffect) ms = await (med as MediaEffect).screenshot();
                    else ms = await (med as Media).screenshot();
                    if (ms == null) return;
                    Directory.CreateDirectory(data);
                    var fileStream = File.Create(data + "thumb.png");
                    ms.WriteTo(fileStream);
                    if (msVW != null) msVW.Close();
                    fileStream.Close();
                    ms.Close();
                    if (msVW != null) msVW.Dispose();
                    fileStream.Dispose();
                    ms.Dispose();
                    fileStream = null;
                    ms = null;
                    msVW = null;
                    NativeWallpaper.changeWallpaper(data + "thumb.png");
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Unable to load the new thumb wallpaper : " + ex.ToString(), "Wallpapeuhrs - Error");
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
                log(media);
            }
            catch
            {
                if (isOk)
                {
                    isOk = false;
                    System.Windows.MessageBox.Show("The file or folder : \"" + curUrl + "\" doesn't exist. Please verify the path entered or if the file or folder exists really.", "Wallpapeuhrs - Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
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

        private async void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if(isDebug && (allClients || System.Windows.Forms.Screen.PrimaryScreen.DeviceName == moni)) DispatcherQueue.TryEnqueue(() => dw.Close());
            //W32.SetParent(new WindowInteropHelper(this).Handle, IntPtr.Zero);
            //if(isEdgeEngine) (med as MediaVW).webview.Dispose();
            //else (med as Media).myHostControl.Dispose();
            IntPtr result = IntPtr.Zero;
            W32.SendMessageTimeout(Worker.progman,
                                   0x052C,
                                   new IntPtr(0x0D),
                                   new IntPtr(0),
                                   W32.SendMessageTimeoutFlags.SMTO_NORMAL,
                                   1000,
                                   out result);
        }

        private void Window_Activated(object sender, EventArgs e)
        {
            
        }

        public void log(string log)
        {
            if (isDebug && (allClients || System.Windows.Forms.Screen.PrimaryScreen.DeviceName == moni)) DispatcherQueue.TryEnqueue(() => dw.log(log));
        }

        bool curPlay = true;

        public void changePlayerState(bool play)
        {
            curPlay = play;
            if(isEdgeEngine) (med as MediaVW).changePlayerState(play);
            else if (med is MediaEffect) (med as MediaEffect).changePlayerState(play);
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

        async Task setWorker(int tries = 0)
        {
            IntPtr p = WinRT.Interop.WindowNative.GetWindowHandle(this);
            Worker.Init();
            if (tries == 0)
            {
                AppWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.Overlapped);
                _icon = new Icon(typeof(App).Assembly.GetManifestResourceStream("Wallpapeuhrs.Resources.IconWPBG.ico")!);
                AppWindow.SetIcon(Win32Interop.GetIconIdFromIcon(_icon.Handle));
                var presenter = AppWindow.Presenter as OverlappedPresenter;
                presenter.IsResizable = false;
                presenter.SetBorderAndTitleBar(false, false);
                AppWindow.Show();
                var styles = W32.GetWindowLongPtr(WinRT.Interop.WindowNative.GetWindowHandle(this), (int)W32.WindowLongFlags.GWL_STYLE);
                W32.SetWindowLong(WinRT.Interop.WindowNative.GetWindowHandle(this), W32.WindowLongFlags.GWL_STYLE, (int)(styles & ~(nint)W32.WindowStyles.WS_CAPTION));
            }
            if (Worker.workerw == IntPtr.Zero)
            {
                System.Windows.MessageBox.Show("Cannot find WorkerW window.\nThe wallpaper application will restart.", "Wallpapeuhrs - Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                Close();
            }
            if (W32.SetParent(p, Worker.workerw) == IntPtr.Zero)
            {
                if(tries < 3)
                {
                    await Task.Delay(300);
                    await setWorker(tries + 1);
                    return;
                }
                System.Windows.MessageBox.Show("Cannot change the parent to WorkerW.\nCode error Win32 " + Marshal.GetLastWin32Error() + "\nDebug: " + p.ToString("X") + ", " + Worker.workerw.ToString("X"), "Wallpapeuhrs - Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                Close();
            }
            if(isEdgeEngine)
            {
                await fixWebview();
            }
        }

        async Task fixWebview()
        {
            W32.EnumWindows(new W32.EnumWindowsProc((tophandle, topparamhandle) =>
            {
                // get window title and class name
                StringBuilder title = new StringBuilder(256);
                W32.GetWindowText(tophandle, title, title.Capacity);
                StringBuilder className = new StringBuilder(256);
                W32.GetClassName(tophandle, className, className.Capacity);
                // check if the window is a Chrome window
                if (className.ToString() == "Chrome_WidgetWin_1" && title.ToString() == "Wallpapeuhrs-" + moni.Substring(4) && webviewHandle == IntPtr.Zero)
                {
                    // set the parent of the Chrome window to the WorkerW window
                    webviewHandle = tophandle;
                }

                return true;
            }), IntPtr.Zero);
            if (webviewHandle != IntPtr.Zero && med is MediaVW)
            {
                try
                {
                    await (med as MediaVW).webview.CoreWebView2.ExecuteScriptAsync("document.title = 'Wallpapeuhrs Background'");
                    if (W32.SetParent(webviewHandle, Worker.workerw) == IntPtr.Zero)
                    {
                        System.Windows.MessageBox.Show("Cannot change the parent to WorkerW for WebView2.\nCode error Win32 " + Marshal.GetLastWin32Error(), "Wallpapeuhrs - Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        Close();
                    }
                    else
                    {
                        // If two screen are not along the same line, we need to move the webview to the right position
                        // WorkerW's position is the difference between two screens X and Y positions
                        // So we need to move the webview to the negative of that position to get the 0, 0 position
                        W32.RECT workerSize = new W32.RECT();
                        W32.GetWindowRect(Worker.workerw, out workerSize);
                        W32.SetWindowPos(webviewHandle, IntPtr.Zero, -workerSize.Left + appWin.Position.X, -workerSize.Top + appWin.Position.Y, 0, 0, W32.SetWindowPosFlags.AsynWindowPos | W32.SetWindowPosFlags.NoRedraw | W32.SetWindowPosFlags.NoZOrder | W32.SetWindowPosFlags.NoSize);
                    }
                }
                catch
                {
                    System.Windows.MessageBox.Show("Cannot access to WebView2.\nThe wallpaper application will restart.", "Wallpapeuhrs - Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    Close();
                }
            }
            else
            {
                await Task.Delay(200);
                await fixWebview();
            }
        }

        private void main_SourceInitialized(object sender, EventArgs e)
        {
            //base.OnSourceInitialized(e);
            /*IntPtr m_hWnd = new WindowInteropHelper(this).Handle;
            if (m_dwxs is null)
            {
                m_dwxs = new Microsoft.UI.Xaml.Hosting.DesktopWindowXamlSource();
                Microsoft.UI.WindowId myWndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(m_hWnd);
                m_dwxs.Initialize(myWndId);
                var sb = m_dwxs.SiteBridge;
                var csv = sb.SiteView;
                var rs = 1;
                Windows.Graphics.RectInt32 rect = new Windows.Graphics.RectInt32((int)(0 * rs), (int)(0 * rs), (int)(800 * rs), (int)(900 * rs));
                //sb.MoveAndResize(rect);
                //MessageBox.Show("a");
            }*/
            //Content = m_dwxs;
            
        }
    }
}
