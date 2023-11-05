using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Wallpapeuhrs
{
    public static class Update
    {
        static string versionName = "v1.2.15";
        static int versionNumber = 68;

        static Dictionary<string, string> pageCache = new Dictionary<string, string>();
        static HttpClient httpClient = new HttpClient();

        public static string getVersionName()
        {
            return versionName;
        }

        public static int getVersionNumber()
        {
            return versionNumber;
        }

        static async Task<string> httpRequestGET(string url)
        {
            try
            {
                if (!pageCache.ContainsKey(url))
                {
                    string script = await httpClient.GetStringAsync(url);
                    try { pageCache.Add(url, script); } catch { }
                    return script;
                }
                else return pageCache[url];
            }
            catch
            {
                return null;
            }
        }

        public static async void searchUpdates()
        {
            try
            {
                string serv = await getServer();
                if (serv != null)
                {
                    serv = serv.Replace("\n", "").Replace("\r", "");
                    string str = await httpRequestGET(serv + "/dl/Wallpapeuhrs/update.php");
                    if (str != null)
                    {
                        string[] infos = str.Split(new string[] { " [|] " }, StringSplitOptions.None);
                        int ver = int.Parse(infos[0]);
                        if (ver > versionNumber)
                        {
                            DialogResult dr = MessageBox.Show("A new version of Wallpapeuhrs is out !\nDo you want to download this update and install it now ?\n\nChangelogs : " + infos[1], "Wallpapeuhrs - Update available", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                            if (dr == DialogResult.Yes)
                            {
                                installUpdate();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Can't search for updates. Error :\n" + ex, "Wallpapeurhs - Update failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public static bool haveUpdate = false;

        public static async Task searchUpdatesSilent()
        {
            try
            {
                string serv = await getServer();
                if (serv != null)
                {
                    string str = await httpRequestGET(serv + "/dl/Wallpapeuhrs/update.php");
                    if (str != null)
                    {
                        string[] infos = str.Split(new string[] { " [|] " }, StringSplitOptions.None);
                        int ver = int.Parse(infos[0]);
                        if (ver > versionNumber)
                        {
                            haveUpdate = true;
                            return;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Can't search for updates. Error :\n" + ex, "Wallpapeurhs - Update failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            haveUpdate = false;
            return;
        }

        public static async void installUpdate()
        {
            var serv = await getServer();
            if (serv != null)
            {
                downloadExe(serv + "/dl/Wallpapeuhrs/download.php");
                MessageBox.Show("Downloading...\nWallpapeuhrs will restart in a short time.", "Wallpapeuhrs - Update available");
            }
        }

        public static async Task<string> getServer()
        {
            var result = await httpRequestGET("https://raw.githubusercontent.com/Shiyukine/Shiyukine/main/serv.txt");
            //var result = "http://192.168.0.33/";
            if(result != null)
            {
                return result.Replace("\n", "").Replace("\r", "");
            }
            return null;
        }

        static void downloadExe(string url)
        {
            try
            {
                WebClient wc = new WebClient();
                wc.Encoding = Encoding.UTF8;
                wc.DownloadFileCompleted += (sendere, ee) =>
                {
                    MainWindow.isclos = true;
                    Process p = new Process();
                    p.StartInfo = new ProcessStartInfo();
                    p.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "\\WallpapeuhrsInstall.exe";
                    p.StartInfo.Arguments = "/VERYSILENT";
                    p.Start();
                    //p.StartInfo.UseShellExecute = true;
                    //App.Current.Shutdown();
                    Process.GetCurrentProcess().Kill();
                };
                wc.DownloadFileAsync(new Uri(url), AppDomain.CurrentDomain.BaseDirectory + "\\WallpapeuhrsInstall.exe");
            }
            catch (Exception ei)
            {
                MessageBox.Show("Can't install update. Error :\n" + ei, "Wallpapeuhrs - Update failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
