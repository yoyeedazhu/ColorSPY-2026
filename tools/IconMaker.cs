using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

internal static class IconMaker
{
    private static void Main(string[] args)
    {
        string outPath = args.Length > 0 ? args[0] : "ColorSPY.ico";
        int[] sizes = new int[] { 16, 24, 32, 48, 64, 128, 256 };
        byte[][] images = new byte[sizes.Length][];

        for (int i = 0; i < sizes.Length; i++)
        {
            using (Bitmap bmp = Render(sizes[i]))
                images[i] = ToDib(bmp);
        }

        using (FileStream fs = new FileStream(outPath, FileMode.Create, FileAccess.Write))
        using (BinaryWriter w = new BinaryWriter(fs))
        {
            w.Write((ushort)0);
            w.Write((ushort)1);
            w.Write((ushort)sizes.Length);
            int offset = 6 + 16 * sizes.Length;
            for (int i = 0; i < sizes.Length; i++)
            {
                w.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i]));
                w.Write((byte)(sizes[i] >= 256 ? 0 : sizes[i]));
                w.Write((byte)0);
                w.Write((byte)0);
                w.Write((ushort)1);
                w.Write((ushort)32);
                w.Write(images[i].Length);
                w.Write(offset);
                offset += images[i].Length;
            }
            for (int i = 0; i < sizes.Length; i++) w.Write(images[i]);
        }
        Console.WriteLine("WROTE " + outPath);
    }

    // 32bpp DIB（底部向上）+ 全零 AND 掩码；兼容老 GDI+ 与资源管理器
    private static byte[] ToDib(Bitmap bmp)
    {
        int w = bmp.Width;
        int h = bmp.Height;
        byte[] xor = new byte[w * h * 4];
        BitmapData data = bmp.LockBits(new Rectangle(0, 0, w, h),
            ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        try
        {
            for (int y = 0; y < h; y++)
            {
                IntPtr src = new IntPtr(data.Scan0.ToInt64() + (long)y * data.Stride);
                Marshal.Copy(src, xor, (h - 1 - y) * w * 4, w * 4);
            }
        }
        finally
        {
            bmp.UnlockBits(data);
        }

        int maskStride = ((w + 31) / 32) * 4;
        byte[] mask = new byte[maskStride * h];

        using (MemoryStream ms = new MemoryStream())
        using (BinaryWriter bw = new BinaryWriter(ms))
        {
            bw.Write(40);
            bw.Write(w);
            bw.Write(h * 2);
            bw.Write((ushort)1);
            bw.Write((ushort)32);
            bw.Write(0);
            bw.Write(xor.Length + mask.Length);
            bw.Write(0);
            bw.Write(0);
            bw.Write(0);
            bw.Write(0);
            bw.Write(xor);
            bw.Write(mask);
            return ms.ToArray();
        }
    }

    private static Bitmap Render(int size)
    {
        Bitmap bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (Graphics g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.Transparent);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            float k = size / 256f;
            RectangleF full = new RectangleF(0, 0, size, size);
            using (GraphicsPath p = Round(full, 56 * k))
            using (LinearGradientBrush br = new LinearGradientBrush(full,
                Color.FromArgb(96, 138, 255), Color.FromArgb(132, 96, 255), 45f))
                g.FillPath(br, p);

            float cx = 106 * k;
            float cy = 102 * k;
            float r = 46 * k;
            float ring = Math.Max(1.6f, 17 * k);
            using (Pen pen = new Pen(Color.White, ring))
                g.DrawEllipse(pen, cx - r, cy - r, r * 2, r * 2);

            using (Pen pen = new Pen(Color.White, Math.Max(1.8f, 22 * k)))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawLine(pen, cx + r * 0.72f, cy + r * 0.72f, 196 * k, 196 * k);
            }

            float dot = Math.Max(1.4f, 17 * k);
            using (SolidBrush b = new SolidBrush(Color.FromArgb(255, 209, 102)))
                g.FillEllipse(b, cx - dot, cy - dot, dot * 2, dot * 2);
        }
        return bmp;
    }

    private static GraphicsPath Round(RectangleF r, float radius)
    {
        GraphicsPath p = new GraphicsPath();
        float d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        p.AddArc(r.X, r.Y, d, d, 180, 90);
        p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        p.CloseFigure();
        return p;
    }
}
