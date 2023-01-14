using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;

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

        public void screenshoted(object[] ret)
        {
            byte[] data = new byte[ret.Length];
            for(int i = 0; i < ret.Length; i++)
            {
                data[i] = Convert.ToByte(ret[i]);
            }
            parent.changeNativeWallpaper(new System.IO.MemoryStream(data));
        }
    }
}
