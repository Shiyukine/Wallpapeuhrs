﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Wallpapeuhrs.Styles
{
    public partial class FlatCheckBox : CheckBox
    {
        public static readonly DependencyProperty CheckMarkColorProperty =
            DependencyProperty.Register("CheckMarkColor", typeof(Brush), typeof(FlatCheckBox));

        public Brush CheckMarkColor
        {
            get { return (Brush)GetValue(CheckMarkColorProperty); }
            set { SetValue(CheckMarkColorProperty, value); }
        }
    }
}
