using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace ColorSpy
{
    public static class Theme
    {
        public static float Scale = 1f;

        public static int S(int v)
        {
            return (int)Math.Round(v * Scale);
        }

        public static readonly Color Bg = Color.FromArgb(18, 21, 28);
        public static readonly Color Surface = Color.FromArgb(24, 28, 36);
        public static readonly Color Surface2 = Color.FromArgb(33, 38, 48);
        public static readonly Color Border = Color.FromArgb(44, 51, 64);
        public static readonly Color Text = Color.FromArgb(233, 236, 243);
        public static readonly Color Dim = Color.FromArgb(150, 159, 176);
        public static readonly Color Faint = Color.FromArgb(104, 113, 129);
        public static readonly Color Accent = Color.FromArgb(96, 138, 255);
        public static readonly Color Accent2 = Color.FromArgb(132, 96, 255);
        public static readonly Color Good = Color.FromArgb(72, 200, 146);
        public static readonly Color Warn = Color.FromArgb(235, 172, 78);
        public static readonly Color Bad = Color.FromArgb(235, 100, 108);

        private static readonly Dictionary<string, Font> Fonts = new Dictionary<string, Font>();
        private static string _family;

        public static string FontFamily
        {
            get
            {
                if (_family == null)
                {
                    _family = "Microsoft YaHei UI";
                    try
                    {
                        using (Font test = new Font(_family, 9f))
                        {
                            if (test.Name != _family) _family = "Microsoft YaHei";
                        }
                    }
                    catch
                    {
                        _family = "Segoe UI";
                    }
                }
                return _family;
            }
        }

        public static Font F(float size, FontStyle style)
        {
            string key = size.ToString("0.##") + "|" + (int)style;
            Font f;
            if (Fonts.TryGetValue(key, out f)) return f;
            f = new Font(FontFamily, size, style, GraphicsUnit.Point);
            Fonts[key] = f;
            return f;
        }

        public static Font F(float size)
        {
            return F(size, FontStyle.Regular);
        }

        public static Font Mono(float size)
        {
            string key = "m" + size.ToString("0.##");
            Font f;
            if (Fonts.TryGetValue(key, out f)) return f;
            f = new Font("Consolas", size, FontStyle.Regular, GraphicsUnit.Point);
            Fonts[key] = f;
            return f;
        }

        public static Color Alpha(Color c, int a)
        {
            return Color.FromArgb(ColorMath.Clamp255(a), c.R, c.G, c.B);
        }

        public static GraphicsPath Round(Rectangle r, int radius)
        {
            GraphicsPath p = new GraphicsPath();
            int d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
            if (d <= 1)
            {
                p.AddRectangle(r);
                return p;
            }
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void FillRound(Graphics g, Rectangle r, int radius, Color color)
        {
            using (GraphicsPath p = Round(r, radius))
            using (SolidBrush b = new SolidBrush(color))
                g.FillPath(b, p);
        }

        public static void DrawRound(Graphics g, Rectangle r, int radius, Color color, float width)
        {
            using (GraphicsPath p = Round(r, radius))
            using (Pen pen = new Pen(color, width))
                g.DrawPath(pen, p);
        }

        private static readonly StringFormat FmtLeft = MakeFormat(StringAlignment.Near);
        private static readonly StringFormat FmtRight = MakeFormat(StringAlignment.Far);
        private static readonly StringFormat FmtCenter = MakeFormat(StringAlignment.Center);

        private static StringFormat MakeFormat(StringAlignment align)
        {
            StringFormat f = new StringFormat();
            f.Alignment = align;
            f.LineAlignment = StringAlignment.Center;
            f.Trimming = StringTrimming.EllipsisCharacter;
            f.FormatFlags = StringFormatFlags.NoWrap;
            return f;
        }

        private static void Draw(Graphics g, string text, Font font, Color color, Rectangle bounds, StringFormat format)
        {
            using (SolidBrush brush = new SolidBrush(color))
                g.DrawString(text, font, brush, bounds, format);
        }

        public static void TextLeft(Graphics g, string text, Font font, Color color, Rectangle bounds)
        {
            Draw(g, text, font, color, bounds, FmtLeft);
        }

        public static void TextRight(Graphics g, string text, Font font, Color color, Rectangle bounds)
        {
            Draw(g, text, font, color, bounds, FmtRight);
        }

        public static void TextCenter(Graphics g, string text, Font font, Color color, Rectangle bounds)
        {
            Draw(g, text, font, color, bounds, FmtCenter);
        }

        public static void Setup(Graphics g)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        }
    }
}
