using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Wallpapeuhrs.Utils
{
    public static class ScreenInformation
    {

        /// <summary>
        /// Returns the scaling of the given screen.
        /// </summary>
        /// <param name="dpiType">The type of dpi that should be given back..</param>
        /// <param name="dpiX">Gives the horizontal scaling back (in dpi).</param>
        /// <param name="dpiY">Gives the vertical scaling back (in dpi).</param>
        public static uint GetDpi(System.Windows.Forms.Screen screen)
        {
            var pnt = new System.Drawing.Point(screen.Bounds.Left + 1, screen.Bounds.Top + 1);
            var mon = MonitorFromPoint(pnt, 2/*MONITOR_DEFAULTTONEAREST*/);
            uint dpiX;
            uint dpiY;
            GetDpiForMonitor(mon, DpiType.ANGULAR, out dpiX, out dpiY);
            uint max1 = Math.Max(dpiX, dpiY);
            GetDpiForMonitor(mon, DpiType.RAW, out dpiX, out dpiY);
            uint max2 = Math.Max(dpiX, dpiY);
            GetDpiForMonitor(mon, DpiType.EFFECTIVE, out dpiX, out dpiY);
            uint max3 = Math.Max(dpiX, dpiY);
            return max(max1, max2, max3);
        }

        private static uint max(uint a, uint b, uint c)
        {
            uint max = a;
            if (b > max) max = b;
            if (c > max) max = c;
            return max;
        }

        //https://msdn.microsoft.com/en-us/library/windows/desktop/dd145062.aspx
        [DllImport("User32.dll")]
        private static extern IntPtr MonitorFromPoint([In] System.Drawing.Point pt, [In] uint dwFlags);

        //https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510.aspx
        [DllImport("Shcore.dll")]
        private static extern IntPtr GetDpiForMonitor([In] IntPtr hmonitor, [In] DpiType dpiType, [Out] out uint dpiX, [Out] out uint dpiY);

        const int _S_OK = 0;
        const int _MONITOR_DEFAULTTONEAREST = 2;
        const int _E_INVALIDARG = -2147024809;
    }

    /// <summary>
    /// Represents the different types of scaling.
    /// </summary>
    /// <seealso cref="https://msdn.microsoft.com/en-us/library/windows/desktop/dn280511.aspx"/>
    public enum DpiType
    {
        EFFECTIVE = 0,
        ANGULAR = 1,
        RAW = 2,
    }
}
