using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Wallpapeuhrs
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class ScreenConfig : UserControl
    {
        public ScreenConfig()
        {
            InitializeComponent();
        }

        public string screenName { get; set; }

        private void vid_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.vidClick(urls);
        }

        private void slide_Click(object sender, RoutedEventArgs e)
        {
            MainWindow.slideClick(urls);
        }

        private void th_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            MainWindow.changeThemeValue(sender, screenName);
        }

        private void th_DragDelta(object sender, DragDeltaEventArgs e)
        {
            /*Slider slider = (Slider)sender;
            foreach (string monii in processes.Keys)
            {
                sendData(processes[monii], "ChangeTheme=" + (string)slider.Tag + "=" + slider.Value, monii);
            }*/
        }

        private void th_reset(object sender, RoutedEventArgs e)
        {
            MainWindow.resetThemeValue(sender, screenName);
        }
    }
}
