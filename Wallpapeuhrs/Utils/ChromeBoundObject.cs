using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace Wallpapeuhrs.Utils
{
    public class ChromeBoundObject
    {
        WPBG parent;

        public ChromeBoundObject(WPBG parent)
        {
            this.parent = parent;
        }

        public void videoLoaded()
        {
            MainWindow.sendData(parent.tcp, "VIDREADY " + parent.moni + " ", null);
        }

        public void screenshoted()
        {
            if (System.Windows.Forms.Screen.PrimaryScreen.DeviceName == parent.moni)
            {
                async void a()
                {
                    try
                    {
                        string str = await (parent.med as MediaVW).webview.ExecuteScriptAsync("screenshotData");
                        str = str.Replace("\"", "");
                        List<byte> data = new List<byte>();
                        string cur = "";
                        for (int i = 0; i < str.Length; i++)
                        {
                            if (str[i] == ' ')
                            {
                                data.Add(byte.Parse(cur));
                                cur = "";
                            }
                            else
                            {
                                cur += str[i];
                            }
                        }
                        parent.changeNativeWallpaper(new System.IO.MemoryStream(data.ToArray()));
                        data = null;
                        str = null;
                        GC.Collect();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString());
                    }
                }
                a();
            }
        }
    }
}
