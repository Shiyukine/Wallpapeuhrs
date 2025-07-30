using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wallpapeuhrs.Bridge
{
    public sealed class BoundObject
    {
        public event EventHandler<string> called;

        public void videoLoaded()
        {
            if(called != null) called(this, "videoLoaded");
        }

        public void screenshoted()
        {
            if (called != null) called(this, "screenshoted");
        }
    }
}
