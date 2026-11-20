using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ColorSpy
{
    /// <summary>
    /// 拾取模式的全屏层：鼠标变成十字、跟随放大镜，点击只取色。
    /// 因为整层覆盖桌面并接收鼠标，点击不会落到下面的任何程序。
    /// </summary>
    public class PickOverlay : Form
    {
        private const int GridSize = 11;
        private static readonly int[] ZoomSteps = new int[] { 6, 8, 10, 13, 17 };

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int X; public int Y; }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint flags);
        [DllImport("Shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr hmon, int type, out uint dpiX, out uint dpiY);

        private Bitmap _screen;
        private Rectangle _virtual;
        private Point _cursor;
        private int _zoomIndex = 1;
        private int _monDpi = 96;
        private string _monName = "";
        private float _formDpi = 96f;
        private Color _picked;
        private bool _leftDown;
        private bool _rightDown;

        public Color PickedColor { get { return _picked; } }

        public PickOverlay()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            DoubleBuffered = true;
            KeyPreview = true;
            BackColor = Color.Black;
            Cursor = Cursors.Cross;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);

            _virtual = SystemInformation.VirtualScreen;
            Bounds = _virtual;
            _screen = CaptureVirtual(_virtual);

            Point gp = Cursor.Position;
            _cursor = new Point(gp.X - _virtual.X, gp.Y - _virtual.Y);
            ClampCursor();
            UpdateMonitor(gp);
        }

        private int L(int v)
        {
            return (int)Math.Round(v * _monDpi / 96.0);
        }

        private int L(float v)
        {
            return (int)Math.Round(v * _monDpi / 96.0);
        }

        private Font LF(float points, FontStyle style)
        {
            float formScale = _formDpi / 96f;
            if (formScale <= 0.01f) formScale = 1f;
            return Theme.F(points * (_monDpi / 96f) / formScale, style);
        }

        private Point Global(Point local)
        {
            return new Point(_virtual.X + local.X, _virtual.Y + local.Y);
        }

        private void ClampCursor()
        {
            if (_screen == null) return;
            _cursor.X = Math.Max(0, Math.Min(_screen.Width - 1, _cursor.X));
            _cursor.Y = Math.Max(0, Math.Min(_screen.Height - 1, _cursor.Y));
        }

        private Color PixelAt(int x, int y)
        {
            x = Math.Max(0, Math.Min(_screen.Width - 1, x));
            y = Math.Max(0, Math.Min(_screen.Height - 1, y));
            return _screen.GetPixel(x, y);
        }

        private Bitmap CaptureVirtual(Rectangle v)
        {
            Bitmap bmp = new Bitmap(Math.Max(1, v.Width), Math.Max(1, v.Height),
                PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                Screen[] screens = Screen.AllScreens;
                for (int i = 0; i < screens.Length; i++)
                {
                    Rectangle b = screens[i].Bounds;
                    int dx = b.X - v.X;
                    int dy = b.Y - v.Y;
                    try
                    {
                        using (Bitmap one = new Bitmap(b.Width, b.Height, PixelFormat.Format32bppArgb))
                        {
                            using (Graphics og = Graphics.FromImage(one))
                                og.CopyFromScreen(b.X, b.Y, 0, 0, b.Size, CopyPixelOperation.SourceCopy);
                            g.DrawImageUnscaled(one, dx, dy);
                        }
                    }
                    catch
                    {
                        try { g.CopyFromScreen(b.X, b.Y, dx, dy, b.Size, CopyPixelOperation.SourceCopy); }
                        catch { }
                    }
                }
            }
            return bmp;
        }

        private void UpdateMonitor(Point global)
        {
            Screen target = null;
            Screen[] screens = Screen.AllScreens;
            for (int i = 0; i < screens.Length; i++)
            {
                if (screens[i].Bounds.Contains(global)) { target = screens[i]; break; }
            }
            _monName = target == null ? "" : target.DeviceName.Replace("\\.\\", "");
            int dpi = 96;
            try
            {
                POINT pt;
                pt.X = global.X;
                pt.Y = global.Y;
                IntPtr hm = MonitorFromPoint(pt, 2);
                uint dx, dy;
                if (GetDpiForMonitor(hm, 0, out dx, out dy) == 0 && dx > 0) dpi = (int)dx;
            }
            catch
            {
            }
            _monDpi = dpi;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Bounds = SystemInformation.VirtualScreen;
            Activate();
            BringToFront();
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            _cursor = new Point(e.X, e.Y);
            ClampCursor();
            UpdateMonitor(Global(_cursor));
            Invalidate();
            base.OnMouseMove(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            // 只记录，不关闭；否则随后的抬起会落到下面的程序上
            if (e.Button == MouseButtons.Left) _leftDown = true;
            else if (e.Button == MouseButtons.Right) _rightDown = true;
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Button == MouseButtons.Left && _leftDown)
            {
                _leftDown = false;
                _picked = PixelAt(_cursor.X, _cursor.Y);
                DialogResult = DialogResult.OK;
                Close();
            }
            else if (e.Button == MouseButtons.Right && _rightDown)
            {
                _rightDown = false;
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e)
        {
            _zoomIndex += e.Delta > 0 ? 1 : -1;
            if (_zoomIndex < 0) _zoomIndex = 0;
            if (_zoomIndex >= ZoomSteps.Length) _zoomIndex = ZoomSteps.Length - 1;
            Invalidate();
            base.OnMouseWheel(e);
        }

        private void MoveCursor(int dx, int dy)
        {
            Point gp = Cursor.Position;
            gp.Offset(dx, dy);
            Cursor.Position = gp;
            _cursor = new Point(gp.X - _virtual.X, gp.Y - _virtual.Y);
            ClampCursor();
            UpdateMonitor(gp);
            Invalidate();
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData == Keys.Left || keyData == Keys.Right || keyData == Keys.Up || keyData == Keys.Down)
                return true;
            return base.IsInputKey(keyData);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            int step = e.Shift ? 10 : 1;
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
            else if (e.KeyCode == Keys.Left) MoveCursor(-step, 0);
            else if (e.KeyCode == Keys.Right) MoveCursor(step, 0);
            else if (e.KeyCode == Keys.Up) MoveCursor(0, -step);
            else if (e.KeyCode == Keys.Down) MoveCursor(0, step);
            else if (e.KeyCode == Keys.Enter)
            {
                _picked = PixelAt(_cursor.X, _cursor.Y);
                DialogResult = DialogResult.OK;
                Close();
            }
            base.OnKeyDown(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Setup(g);
            _formDpi = g.DpiX;
            if (_screen == null) return;
            g.DrawImageUnscaled(_screen, 0, 0);

            int cell = L(ZoomSteps[_zoomIndex]);
            int lensRadius = Math.Max(cell * GridSize / 2, L(46));

            using (GraphicsPath hole = new GraphicsPath())
            {
                hole.FillMode = FillMode.Alternate;
                hole.AddRectangle(new Rectangle(0, 0, Width, Height));
                hole.AddEllipse(_cursor.X - lensRadius, _cursor.Y - lensRadius,
                    lensRadius * 2, lensRadius * 2);
                using (SolidBrush dim = new SolidBrush(Color.FromArgb(86, 5, 7, 10)))
                    g.FillPath(dim, hole);
            }
            using (Pen lens = new Pen(Color.FromArgb(150, 255, 255, 255), L(1)))
                g.DrawEllipse(lens, _cursor.X - lensRadius, _cursor.Y - lensRadius,
                    lensRadius * 2, lensRadius * 2);

            using (Pen line = new Pen(Color.FromArgb(110, 255, 255, 255), L(1)))
            {
                g.DrawLine(line, 0, _cursor.Y, _cursor.X - L(8), _cursor.Y);
                g.DrawLine(line, _cursor.X + L(8), _cursor.Y, Width, _cursor.Y);
                g.DrawLine(line, _cursor.X, 0, _cursor.X, _cursor.Y - L(8));
                g.DrawLine(line, _cursor.X, _cursor.Y + L(8), _cursor.X, Height);
            }
            using (Pen ring = new Pen(Color.FromArgb(220, 255, 255, 255), L(1)))
                g.DrawEllipse(ring, _cursor.X - L(6), _cursor.Y - L(6), L(12), L(12));

            DrawLoupe(g, cell);
            DrawHints(g);
        }

        private void DrawLoupe(Graphics g, int cell)
        {
            int grid = cell * GridSize;
            int pad = L(10);
            int head = L(24);
            int readout = L(88);
            int w = Math.Max(grid + pad * 2, L(178));
            int h = head + grid + readout + pad;

            Screen s = Screen.FromPoint(Global(_cursor)) ?? Screen.PrimaryScreen;
            Rectangle area = s.Bounds;
            area.Offset(-_virtual.X, -_virtual.Y); // 转成覆盖层内的本地坐标
            int offset = L(26);
            int x = _cursor.X + offset;
            int y = _cursor.Y + offset;
            if (x + w > area.Right - L(8)) x = _cursor.X - w - offset;
            if (y + h > area.Bottom - L(8)) y = _cursor.Y - h - offset;
            x = Math.Max(area.Left + L(8), Math.Min(area.Right - w - L(8), x));
            y = Math.Max(area.Top + L(8), Math.Min(area.Bottom - h - L(8), y));

            Rectangle panel = new Rectangle(x, y, w, h);
            using (GraphicsPath sh = Theme.Round(new Rectangle(x + L(2), y + L(4), w, h), L(14)))
            using (SolidBrush sb = new SolidBrush(Color.FromArgb(70, 0, 0, 0)))
                g.FillPath(sb, sh);
            using (GraphicsPath p = Theme.Round(panel, L(14)))
            using (LinearGradientBrush br = new LinearGradientBrush(panel,
                Color.FromArgb(245, 28, 33, 43), Color.FromArgb(245, 16, 19, 26), 90f))
                g.FillPath(br, p);
            Theme.DrawRound(g, panel, L(14), Color.FromArgb(90, 255, 255, 255), L(1));

            Font fb = LF(9f, FontStyle.Bold);
            Font ft = LF(8f, FontStyle.Regular);
            g.DrawString("取色", fb, new SolidBrush(Theme.Text), x + pad, y + L(6));
            string meta = _monName;
            if (meta.Length == 0) meta = "显示器";
            meta += "  " + (int)Math.Round(_monDpi * 100.0 / 96.0) + "%";
            SizeF ms = g.MeasureString(meta, ft);
            g.DrawString(meta, ft, new SolidBrush(Theme.Faint), x + w - pad - ms.Width, y + L(7));

            int gx = x + (w - grid) / 2;
            int gy = y + head;
            int left = _cursor.X - GridSize / 2;
            int top = _cursor.Y - GridSize / 2;

            for (int gyi = 0; gyi < GridSize; gyi++)
            {
                for (int gxi = 0; gxi < GridSize; gxi++)
                {
                    Color c = PixelAt(left + gxi, top + gyi);
                    using (SolidBrush b = new SolidBrush(c))
                        g.FillRectangle(b, gx + gxi * cell, gy + gyi * cell, cell, cell);
                }
            }

            using (Pen gridPen = new Pen(Color.FromArgb(28, 0, 0, 0), L(1)))
            {
                for (int i = 1; i < GridSize; i++)
                {
                    g.DrawLine(gridPen, gx + i * cell, gy, gx + i * cell, gy + grid);
                    g.DrawLine(gridPen, gx, gy + i * cell, gx + grid, gy + i * cell);
                }
            }
            Rectangle center = new Rectangle(gx + (GridSize / 2) * cell, gy + (GridSize / 2) * cell, cell, cell);
            using (Pen dark = new Pen(Color.FromArgb(200, 0, 0, 0), Math.Max(2f, L(2.5f))))
                g.DrawRectangle(dark, center.X - 1, center.Y - 1, center.Width + 2, center.Height + 2);
            using (Pen white = new Pen(Color.White, Math.Max(1f, L(1.5f))))
                g.DrawRectangle(white, center);
            Theme.DrawRound(g, new Rectangle(gx, gy, grid, grid), L(4),
                Color.FromArgb(70, 255, 255, 255), L(1));

            Color c0 = PixelAt(_cursor.X, _cursor.Y);
            double hh, ss, vv;
            ColorMath.ToHsv(c0, out hh, out ss, out vv);

            int ry = gy + grid + L(8);
            int px = x + pad;
            Rectangle chip = new Rectangle(px, ry, L(36), L(36));
            Theme.FillRound(g, chip, L(8), c0);
            Theme.DrawRound(g, chip, L(8), Color.FromArgb(70, 255, 255, 255), 1f);

            Font mono = LF(13.5f, FontStyle.Bold);
            Font small = LF(8f, FontStyle.Regular);
            g.DrawString(ColorMath.Hex(c0), mono, new SolidBrush(Theme.Text), px + L(44), ry - L(2));
            g.DrawString(string.Format("R {0}   G {1}   B {2}", c0.R, c0.G, c0.B),
                small, new SolidBrush(Theme.Dim), px, ry + L(38));
            g.DrawString(string.Format("H {0}°   S {1}%   V {2}%",
                (int)Math.Round(hh), (int)Math.Round(ss * 100), (int)Math.Round(vv * 100)),
                small, new SolidBrush(Theme.Dim), px, ry + L(52));
            g.DrawString(string.Format("X {0}   Y {1}", _virtual.X + _cursor.X, _virtual.Y + _cursor.Y),
                small, new SolidBrush(Theme.Faint), px, ry + L(66));
        }

        private void DrawHints(Graphics g)
        {
            string hint = "单击取色   ·   右键 / Esc 取消   ·   滚轮缩放   ·   方向键微调";
            Screen s = Screen.FromPoint(Global(_cursor)) ?? Screen.PrimaryScreen;
            Font f = LF(9f, FontStyle.Regular);
            Size sz = TextRenderer.MeasureText(hint, f);
            int w = sz.Width + L(28);
            int h = L(30);
            int x = s.Bounds.Left - _virtual.X + (s.Bounds.Width - w) / 2;
            int y = s.Bounds.Bottom - _virtual.Y - h - L(16);
            Rectangle box = new Rectangle(x, y, w, h);
            Theme.FillRound(g, box, L(8), Color.FromArgb(220, 14, 16, 21));
            Theme.DrawRound(g, box, L(8), Color.FromArgb(60, 255, 255, 255), 1f);
            Theme.TextCenter(g, hint, f, Theme.Text, box);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (_screen != null) { _screen.Dispose(); _screen = null; }
            base.OnFormClosed(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _screen != null)
            {
                _screen.Dispose();
                _screen = null;
            }
            base.Dispose(disposing);
        }
    }
}
