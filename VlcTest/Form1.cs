using LibVLCSharp.Shared;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VlcTest
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            Core.Initialize();
            var _libVLC = new LibVLC();
            var _mp = new MediaPlayer(_libVLC);
            _mp.Media = new Media(_libVLC, @"E:\Vidéos\Wallpapers\mes pref\mylivewallpapers.com-Butterfly-Anime-Girl.mp4", FromType.FromPath);
            _mp.SetMarqueeInt(VideoMarqueeOption.Enable, 1);
            videoView1.MediaPlayer = _mp;
            _mp.SetMarqueeInt(VideoMarqueeOption.Opacity, 0);
            _mp.SetMarqueeInt(VideoMarqueeOption.Opacity, 1);
            _mp.Play();
            _mp.SetMarqueeInt(VideoMarqueeOption.Opacity, 1);
        }
    }
}
