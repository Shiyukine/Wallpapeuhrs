using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Wallpapeuhrs
{
    public class Program
    {
        [System.STAThreadAttribute()]
        public static void Main()
        {
            MessageBox.Show("test");
            using (new WallpapeuhrsBG.App())
            {
                //MessageBox.Show(args.ToString());
                //App.nargs = args;
                var app = new App();
                app.InitializeComponent();
                app.Run();
            }
        }
    }
}
