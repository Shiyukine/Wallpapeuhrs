using System;
using System.Collections.Generic;
using System.Text;

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
    }
}
