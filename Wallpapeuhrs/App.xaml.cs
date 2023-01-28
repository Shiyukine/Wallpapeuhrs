using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using Windows.Media.ClosedCaptioning;

namespace Wallpapeuhrs
{
    /// <summary>
    /// Logique d'interaction pour App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Dictionary<string, List<string>> types = new Dictionary<string, List<string>>();
        public static string[] nargs;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                types.Add("Video files", new List<string>()
                {
                    ".avi",
                    ".mp4",
                    ".mov",
                    ".webm",
                    ".m4v"
                });
                types.Add("Image files", new List<string>()
                {
                    ".jpg",
                    ".jpeg",
                    ".jfif",
                    ".png",
                    ".bmp",
                    ".gif"
                });
                //
                if (nargs.Length == 0 || !nargs[0].Contains("wp"))
                {
                    try
                    {
                        bool shutdowning = false;
                        W32.EnumWindows((handle, param) =>
                        {
                            if(isWallpapeuhrs(handle) && !shutdowning)
                            {
                                shutdowning = true;
                                W32.SendMessage(handle, 5250, 0, IntPtr.Zero);
                                App.Current.Shutdown();
                                return false;
                            }
                            return true;
                        }, IntPtr.Zero);
                        if (!shutdowning)
                        {
                            bool inbg = nargs.Length > 0 && nargs[0].Contains("background");
                            new MainWindow(inbg);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                }
                else
                {
                    new WPBG(nargs[4], Convert.ToInt32(nargs[2]), Convert.ToInt32(nargs[6]));
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.ToString());
                App.Current.Shutdown();
            }
        }

        private void closeApp(TcpClient tcp)
        {
            tcp.Close();
            tcp.Dispose();
            Dispatcher.Invoke(() => App.Current.Shutdown());
        }

        public bool isTCPAvailable(int port)
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            foreach (TcpConnectionInformation tcpi in ipGlobalProperties.GetActiveTcpConnections())
            {
                if (tcpi.LocalEndPoint.Port == port)
                {
                    return false;
                }
            }
            foreach (IPEndPoint tcpi in ipGlobalProperties.GetActiveTcpListeners())
            {
                if (tcpi.Port == port)
                {
                    return false;
                }
            }
            return true;
        }

        private static bool isWallpapeuhrs(IntPtr hWnd)
        {
            int length = W32.GetWindowTextLength(hWnd);
            StringBuilder sb = new StringBuilder(length + 1);
            W32.GetWindowText(hWnd, sb, sb.Capacity);
            if (sb.ToString() == "Wallpapeuhrs")
            {
                int nRet;
                // Pre-allocate 256 characters, since this is the maximum class name length.
                StringBuilder ClassName = new StringBuilder(256);
                //Get the window class name
                nRet = W32.GetClassName(hWnd, ClassName, ClassName.Capacity);
                if (nRet != 0)
                {
                    return ClassName.ToString().Contains("Wallpapeuhrs");
                }
                else
                {
                    return false;
                }
            }
            return false;
        }
    }
}
