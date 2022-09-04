using System;
using System.Collections.Generic;
using System.Text;

namespace Wallpapeuhrs.Utils
{
    public class ChromeBoundObjectForms
    {
        WPBGForm parent;

        public ChromeBoundObjectForms(WPBGForm parent)
        {
            this.parent = parent;
        }

        public void videoLoaded()
        {
            MainWindow.sendData(parent.tcp, "VIDREADY " + parent.moni + " ", null);
        }
    }
}
