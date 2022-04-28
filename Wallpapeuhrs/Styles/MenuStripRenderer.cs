using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Wallpapeuhrs.Styles
{
    public class MenuStripRenderer : ToolStripProfessionalRenderer
    {
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            var size = e.Item.Size;
            size.Height += 1;
            Rectangle rc = new Rectangle(Point.Empty, size);
            Color c = e.Item.Selected ? Color.FromArgb(68, 68, 68) : Color.FromArgb(46, 46, 46);
            using (SolidBrush brush = new SolidBrush(c))
                e.Graphics.FillRectangle(brush, rc);
        }
    }
}
