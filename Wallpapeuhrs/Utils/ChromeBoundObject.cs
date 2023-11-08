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
            async void a()
            {
                try
                {
                    string str = await (parent.med as MediaVW).webview.ExecuteScriptAsync("screenshotData");
                    str = str.Replace("\"", "");
                    parent.log("Screenshot");
                    var data = str.Split(' ').Select(byte.Parse).ToArray();
                    parent.changeNativeWallpaper(new System.IO.MemoryStream(data));
                    data = new byte[1];
                    data = null;
                    str = "";
                    str = null;
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
            a();
        }
    }
}
