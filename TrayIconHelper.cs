using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace AudioSwitcherWidget;

public static class TrayIconHelper
{
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool DestroyIcon(IntPtr handle);

    public static Icon CreateSoundWaveIcon()
    {
        using var bmp = new Bitmap(32, 32);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(System.Drawing.Color.Transparent);

            // Dark rounded box
            using var bgBrush = new SolidBrush(System.Drawing.Color.FromArgb(38, 40, 59));
            using var path = CreateRoundedRectanglePath(new RectangleF(2, 2, 28, 28), 7);
            g.FillPath(bgBrush, path);

            // Wave bars (#818CF8 -> RGB 129, 140, 248)
            using var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(129, 140, 248), 2.5f);
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;

            // 5 vertical bars: short, med, tall, med, short
            g.DrawLine(pen, 8, 16 - 3, 8, 16 + 3);
            g.DrawLine(pen, 12, 16 - 6, 12, 16 + 6);
            g.DrawLine(pen, 16, 16 - 9, 16, 16 + 9);
            g.DrawLine(pen, 20, 16 - 6, 20, 16 + 6);
            g.DrawLine(pen, 24, 16 - 3, 24, 16 + 3);
        }

        IntPtr hIcon = bmp.GetHicon();
        var icon = (Icon)Icon.FromHandle(hIcon).Clone();
        DestroyIcon(hIcon);
        return icon;
    }

    public static ImageSource CreateSoundWaveImageSource(Icon icon)
    {
        return Imaging.CreateBitmapSourceFromHIcon(
            icon.Handle,
            Int32Rect.Empty,
            BitmapSizeOptions.FromEmptyOptions());
    }

    private static GraphicsPath CreateRoundedRectanglePath(RectangleF rect, float radius)
    {
        var path = new GraphicsPath();
        float diameter = radius * 2;
        var arc = new RectangleF(rect.X, rect.Y, diameter, diameter);

        // Top-left
        path.AddArc(arc, 180, 90);
        // Top-right
        arc.X = rect.Right - diameter;
        path.AddArc(arc, 270, 90);
        // Bottom-right
        arc.Y = rect.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        // Bottom-left
        arc.X = rect.Left;
        path.AddArc(arc, 90, 90);

        path.CloseFigure();
        return path;
    }
}
