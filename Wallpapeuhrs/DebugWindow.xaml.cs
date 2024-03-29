﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Wallpapeuhrs
{
    /// <summary>
    /// Logique d'interaction pour DebugWindow.xaml
    /// </summary>
    public partial class DebugWindow : Window
    {
        int tick = System.Environment.TickCount;
        string moni;

        public DebugWindow(string moni)
        {
            this.moni = moni;
            InitializeComponent();
            Title = "DebugWindow - " + moni;
        }

        public void log(string log)
        {
            debug.Document.Blocks.Add(new Paragraph(new Run(System.Environment.TickCount - tick + " - " + log)));
            debug.ScrollToEnd();
        }
    }
}
