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
                if (e.Args.Length == 0 || !e.Args[0].Contains("wp"))
                {
                    try
                    {
                        TcpClient tcp = new TcpClient();
                        tcp.Connect("localhost", 30929);
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
                        tcp.Client.SendAsync(sa);
                    }
                    catch
                    {
                        bool inbg = e.Args.Length > 0 && e.Args[0].Contains("background");
                        new MainWindow(inbg);
                    }
                }
                else
                {
                    new WPBG(e.Args[4], Convert.ToInt32(e.Args[2]));
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
