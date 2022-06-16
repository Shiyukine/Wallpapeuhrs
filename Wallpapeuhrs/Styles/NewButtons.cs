using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace Wallpapeuhrs.Styles
{
    public partial class NewButtons : Button
    {
        public static readonly DependencyProperty BorderRadiusProperty =
            DependencyProperty.Register("BorderRadius", typeof(CornerRadius), typeof(NewButtons));

        public CornerRadius BorderRadius
        {
            get { return (CornerRadius)GetValue(BorderRadiusProperty); }
            set { SetValue(BorderRadiusProperty, value); }
        }

        public static readonly DependencyProperty BackgroundHoverProperty =
            DependencyProperty.Register("BackgroundHover", typeof(System.Windows.Media.Brush), typeof(NewButtons));

        public System.Windows.Media.Brush BackgroundHover
        {
            get { return (System.Windows.Media.Brush)GetValue(BackgroundHoverProperty); }
            set { SetValue(BackgroundHoverProperty, value); }
        }
    }
}
