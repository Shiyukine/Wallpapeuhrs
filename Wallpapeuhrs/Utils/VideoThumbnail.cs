using Microsoft.WindowsAPICodePack.Shell;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;

namespace Wallpapeuhrs.Utils
{
    public static class VideoThumbnail
    {
        public static Bitmap getVideoThumbnail(string path)
        {
            ShellFile shellFile = ShellFile.FromFilePath(path);
            return shellFile.Thumbnail.Bitmap;
        }

        public static BitmapImage getVideoThumbnailImg(string path)
        {
            ShellFile shellFile = ShellFile.FromFilePath(path);
            MemoryStream ms = new MemoryStream();
            shellFile.Thumbnail.Bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }
    }
}
