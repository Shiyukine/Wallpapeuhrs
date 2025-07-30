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
                        List<byte> data = new List<byte>();
                        string cur = "";
                        for (int i = 1; i < str.Length - 1; i++)
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
                        var ms = new System.IO.MemoryStream(data.ToArray());
                        parent.changeNativeWallpaper(ms);
                        ms.Close();
                        ms.Dispose();
                        data.Clear();
                        ms = null;
                        data = null;
                        str = null;
                        await (parent.med as MediaVW).webview.ExecuteScriptAsync("screenshotData = undefined");
                        (parent.med as MediaVW).webview.CoreWebView2.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Low;
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.ToString(), "Wallpapeuhrs - Error");
                    }
                }
                a();
            }
        }
    }
}
