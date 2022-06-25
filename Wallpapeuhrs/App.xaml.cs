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
                        int port = 30929;
                        if (!isTCPAvailable(port))
                        {
                            TcpClient tcp = new TcpClient();
                            tcp.Connect("127.0.0.1", port);
                            SocketAsyncEventArgs sa = new SocketAsyncEventArgs();
                            sa.Completed += (sendere, ee) =>
                            {
                                if (ee.SocketError != SocketError.Success) MessageBox.Show("An instance of Wallpapeuhrs is already opened !");
                                closeApp(tcp);
                            };
                            byte[] data2 = System.Text.Encoding.ASCII.GetBytes("SHOW");
                            sa.SetBuffer(data2, 0, data2.Length);
                            if (!tcp.Client.SendAsync(sa))
                            {
                                closeApp(tcp);
                            }
                        }
                        else
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
    }
}
