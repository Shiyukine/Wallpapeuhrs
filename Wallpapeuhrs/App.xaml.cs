using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
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
                        TcpClient tcp = new TcpClient();
                        tcp.Connect("127.0.0.1", 30929);
                        SocketAsyncEventArgs sa = new SocketAsyncEventArgs();
                        sa.Completed += (sendere, ee) =>
                        {
                            if (ee.SocketError != SocketError.Success) MessageBox.Show("An instance of Wallpapeuhrs is already opened !");
                            tcp.Close();
                            tcp.Dispose();
                            Dispatcher.Invoke(() => App.Current.Shutdown());
                        };
                        byte[] data2 = System.Text.Encoding.ASCII.GetBytes("SHOW");
                        sa.SetBuffer(data2, 0, data2.Length);
                        if (!tcp.Client.SendAsync(sa))
                        {
                            tcp.Close();
                            tcp.Dispose();
                            Dispatcher.Invoke(() => App.Current.Shutdown());
                        }
                    }
                    catch
                    {
                        bool inbg = nargs.Length > 0 && nargs[0].Contains("background");
                        new MainWindow(inbg);
                    }
                }
                else
                {
                    new WPBG(nargs[4], Convert.ToInt32(nargs[2]));
                }
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.ToString());
                App.Current.Shutdown();
            }
        }
    }
}
