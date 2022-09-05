using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wallpapeuhrs
{
    public class Program
    {
        [System.STAThreadAttribute()]
        public static void Main(string[] args)
        {
            if (args.Length > 0 && args[0].Contains("wp") && Convert.ToInt32(args[6]) > 3) new WallpapeuhrsBG.App();
            App.nargs = args;
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
