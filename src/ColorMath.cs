using System;
using System.Drawing;

namespace ColorSpy
{
    public static class ColorMath
    {
        public static double Clamp01(double v)
        {
            if (v < 0) return 0;
            if (v > 1) return 1;
            return v;
        }

        public static int Clamp255(int v)
        {
            if (v < 0) return 0;
            if (v > 255) return 255;
            return v;
        }

        public static string Hex(Color c)
        {
            return string.Format("#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
        }

        public static string RgbText(Color c)
        {
            return string.Format("rgb({0}, {1}, {2})", c.R, c.G, c.B);
        }

        public static string HsvText(Color c)
        {
            double h, s, v;
            ToHsv(c, out h, out s, out v);
            return string.Format("hsv({0}°, {1}%, {2}%)",
                (int)Math.Round(h), (int)Math.Round(s * 100), (int)Math.Round(v * 100));
        }

        public static void ToHsv(Color c, out double h, out double s, out double v)
        {
            double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double d = max - min;
            v = max;
            s = max <= 0 ? 0 : d / max;
            if (d <= 0)
            {
                h = 0;
                return;
            }
            if (max == r) h = 60.0 * (((g - b) / d) % 6.0);
            else if (max == g) h = 60.0 * (((b - r) / d) + 2.0);
            else h = 60.0 * (((r - g) / d) + 4.0);
            if (h < 0) h += 360.0;
        }

        public static double Luminance(Color c)
        {
            return 0.2126 * Channel(c.R / 255.0) + 0.7152 * Channel(c.G / 255.0) + 0.0722 * Channel(c.B / 255.0);
        }

        private static double Channel(double v)
        {
            return v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }

        public static Color IdealFore(Color bg)
        {
            return Luminance(bg) > 0.179 ? Color.FromArgb(18, 20, 26) : Color.White;
        }

        public static Color Mix(Color a, Color b, double t)
        {
            t = Clamp01(t);
            return Color.FromArgb(
                (int)Math.Round(a.A + (b.A - a.A) * t),
                (int)Math.Round(a.R + (b.R - a.R) * t),
                (int)Math.Round(a.G + (b.G - a.G) * t),
                (int)Math.Round(a.B + (b.B - a.B) * t));
        }
    }
}
