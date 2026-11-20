using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ColorSpy
{
    /// <summary>
    /// 右下角托盘小条。平时显示上一次取到的颜色；
    /// 点“屏幕取色”或按 Ctrl+Alt+C 进入全屏拾取模式。
    /// </summary>
    public class SpyForm : Form
    {
        private const int WM_HOTKEY = 0x0312;
        private const int WM_DISPLAYCHANGE = 0x007E;
        private const int HOT_PICK = 1;
        private const uint MOD_ALT = 0x0001;
        private const uint MOD_CONTROL = 0x0002;

        private const int CW = 272;
        private const int CH = 122;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private readonly SpySettings _settings;
        private Color _current;
        private bool _picking;
        private bool _dragging;
        private bool _moved;
        private Point _dragOffset;
        private Point _dragStartScreen;
        private int _pendingDpi;
        private Rectangle _closeRect;
        private Rectangle _pickRect;
        private Rectangle _swatchRect;
        private bool _hoverClose;
        private bool _hoverPick;
        private bool _pressedPick;
        private string _flash;
        private Timer _flashTimer;
        private NotifyIcon _tray;
        private ContextMenuStrip _menu;
        private ToolStripMenuItem _miTop;

        public SpyForm()
        {
            _settings = Store.Load();
            _current = _settings.LastColor != 0 ? Color.FromArgb(_settings.LastColor)
                : Color.FromArgb(92, 134, 255);

            Text = "ColorSPY 2026";
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = _settings.TopMost;
            StartPosition = FormStartPosition.Manual;
            AutoScaleMode = AutoScaleMode.None;
            DoubleBuffered = true;
            KeyPreview = true;
            BackColor = Theme.Bg;
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            Size = new Size(Theme.S(CW), Theme.S(CH));
            LayoutRects();

            try { Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath); }
            catch { }

            PlaceWindow();
            UpdateRegion();
            BuildMenu();
            BuildTray();

            _flashTimer = new Timer();
            _flashTimer.Interval = 1600;
            _flashTimer.Tick += delegate
            {
                _flashTimer.Stop();
                _flash = null;
                Invalidate();
            };
        }

        private int S(int v) { return Theme.S(v); }

        private int S(float v) { return (int)Math.Round(v * Theme.Scale); }

        private void LayoutRects()
        {
            _closeRect = new Rectangle(Width - S(34), S(6), S(24), S(24));
            _swatchRect = new Rectangle(S(12), S(30), S(54), S(54));
            _pickRect = new Rectangle(S(12), S(88), Width - S(24), S(26));
        }

        private void PlaceWindow()
        {
            if (_settings.HasPos)
            {
                Location = new Point(_settings.X, _settings.Y);
                return;
            }
            // 默认吸附在桌面右下角（任务栏上方）
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(wa.Right - Width - S(14), wa.Bottom - Height - S(14));
        }

        private void EnsureVisible()
        {
            Rectangle b = Bounds;
            Screen best = null;
            int bestArea = 0;
            foreach (Screen s in Screen.AllScreens)
            {
                Rectangle inter = Rectangle.Intersect(b, s.WorkingArea);
                int area = inter.Width * inter.Height;
                if (area > bestArea) { bestArea = area; best = s; }
            }
            if (best == null)
            {
                Rectangle wa = Screen.PrimaryScreen.WorkingArea;
                Location = new Point(wa.Right - Width - S(14), wa.Bottom - Height - S(14));
                return;
            }
            Rectangle work = best.WorkingArea;
            int x = Math.Max(work.Left, Math.Min(b.X, work.Right - Width));
            int y = Math.Max(work.Top, Math.Min(b.Y, work.Bottom - Height));
            if (x != b.X || y != b.Y) Location = new Point(x, y);
        }

        private void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0) return;
            using (GraphicsPath p = Theme.Round(new Rectangle(0, 0, Width, Height), S(12)))
                Region = new Region(p);
        }

        private float WindowDpi()
        {
            try
            {
                uint dpi = GetDpiForWindow(Handle);
                if (dpi >= 96) return dpi / 96f;
            }
            catch
            {
            }
            return DeviceDpi / 96f;
        }

        private void ApplyScale(float scale)
        {
            float old = Theme.Scale;
            Theme.Scale = scale;
            if (Math.Abs(old - scale) > 0.01f)
                Size = new Size(S(CW), S(CH));
            LayoutRects();
            UpdateRegion();
            EnsureVisible();
            Invalidate();
        }

        // ---------------- 托盘图标与菜单 ----------------

        private class DarkColors : ProfessionalColorTable
        {
            public override Color ToolStripDropDownBackground { get { return Color.FromArgb(28, 32, 41); } }
            public override Color MenuBorder { get { return Color.FromArgb(54, 61, 76); } }
            public override Color MenuItemSelected { get { return Color.FromArgb(43, 50, 63); } }
            public override Color MenuItemSelectedGradientBegin { get { return Color.FromArgb(43, 50, 63); } }
            public override Color MenuItemSelectedGradientEnd { get { return Color.FromArgb(43, 50, 63); } }
            public override Color MenuItemBorder { get { return Color.FromArgb(43, 50, 63); } }
            public override Color SeparatorDark { get { return Color.FromArgb(44, 50, 62); } }
            public override Color SeparatorLight { get { return Color.FromArgb(44, 50, 62); } }
            public override Color ImageMarginGradientBegin { get { return Color.FromArgb(28, 32, 41); } }
            public override Color ImageMarginGradientMiddle { get { return Color.FromArgb(28, 32, 41); } }
            public override Color ImageMarginGradientEnd { get { return Color.FromArgb(28, 32, 41); } }
        }

        private ContextMenuStrip NewMenu()
        {
            ContextMenuStrip m = new ContextMenuStrip();
            m.BackColor = Color.FromArgb(28, 32, 41);
            m.ForeColor = Theme.Text;
            m.ShowImageMargin = false;
            m.Font = Theme.F(9f);
            m.Renderer = new ToolStripProfessionalRenderer(new DarkColors());
            return m;
        }

        private ToolStripMenuItem Item(ContextMenuStrip m, string text, EventHandler handler)
        {
            ToolStripMenuItem item = new ToolStripMenuItem(text);
            item.ForeColor = Theme.Text;
            item.Click += handler;
            m.Items.Add(item);
            return item;
        }

        private void BuildMenu()
        {
            _menu = NewMenu();
            Item(_menu, "开始取色", delegate { StartPick(); });
            _menu.Items.Add(new ToolStripSeparator());
            Item(_menu, "复制 HEX", delegate { Copy(ColorMath.Hex(_current), "HEX"); });
            Item(_menu, "复制 RGB", delegate { Copy(ColorMath.RgbText(_current), "RGB"); });
            Item(_menu, "复制 HSV", delegate { Copy(ColorMath.HsvText(_current), "HSV"); });
            _menu.Items.Add(new ToolStripSeparator());
            _miTop = Item(_menu, "窗口置顶", delegate
            {
                TopMost = !TopMost;
                _miTop.Checked = TopMost;
                _settings.TopMost = TopMost;
                Store.Save(_settings);
                Invalidate();
            });
            _miTop.Checked = TopMost;
            Item(_menu, "隐藏到托盘", delegate { Hide(); });
            _menu.Items.Add(new ToolStripSeparator());
            Item(_menu, "退出", delegate { Close(); });
        }

        private void BuildTray()
        {
            _tray = new NotifyIcon();
            _tray.Icon = Icon != null ? Icon : SystemIcons.Application;
            _tray.Text = "ColorSPY 2026";
            _tray.Visible = true;
            ContextMenuStrip tm = NewMenu();
            Item(tm, "开始取色", delegate { StartPick(); });
            Item(tm, "显示面板", delegate { ShowPanel(); Activate(); });
            tm.Items.Add(new ToolStripSeparator());
            Item(tm, "退出", delegate { Close(); });
            _tray.ContextMenuStrip = tm;
            _tray.DoubleClick += delegate { StartPick(); };
        }

        // ---------------- 取色流程 ----------------

        private void StartPick()
        {
            if (_picking) return;
            _picking = true;
            IntPtr previous = GetForegroundWindow();
            try
            {
                Hide();
                Application.DoEvents();
                System.Threading.Thread.Sleep(140);
                Application.DoEvents();

                using (PickOverlay overlay = new PickOverlay())
                {
                    DialogResult r = overlay.ShowDialog();
                    if (r == DialogResult.OK)
                    {
                        _current = overlay.PickedColor;
                        _settings.LastColor = _current.ToArgb();
                        Store.Save(_settings);
                        ShowPanel();
                        Copy(ColorMath.Hex(_current), "HEX");
                    }
                    else
                    {
                        ShowPanel();
                    }
                }
            }
            catch
            {
                ShowPanel();
                Flash("取色失败，请重试");
            }
            finally
            {
                _picking = false;
                ShowPanel();
            }
            if (previous != IntPtr.Zero && previous != Handle)
                SetForegroundWindow(previous);
        }

        private void ShowPanel()
        {
            if (!Visible) Show();
            if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;
        }

        private void Copy(string text, string label)
        {
            try
            {
                Clipboard.SetText(text);
                Flash("已复制 " + label + " " + text);
            }
            catch
            {
                try
                {
                    System.Threading.Thread.Sleep(50);
                    Clipboard.SetText(text);
                    Flash("已复制 " + label);
                }
                catch
                {
                    Flash("剪贴板被占用");
                }
            }
        }

        private void Flash(string message)
        {
            _flash = message;
            _flashTimer.Stop();
            _flashTimer.Start();
            Invalidate();
        }

        // ---------------- 鼠标 / 键盘 ----------------

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (_closeRect.Contains(e.Location))
                {
                    Close();
                    return;
                }
                if (_pickRect.Contains(e.Location))
                {
                    _pressedPick = true;
                    Invalidate();
                    return;
                }
                _dragging = true;
                _moved = false;
                _dragOffset = new Point(Cursor.Position.X - Location.X, Cursor.Position.Y - Location.Y);
                _dragStartScreen = Cursor.Position;
                Capture = true;
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            bool hc = _closeRect.Contains(e.Location);
            bool hp = _pickRect.Contains(e.Location);
            if (hc != _hoverClose || hp != _hoverPick)
            {
                _hoverClose = hc;
                _hoverPick = hp;
                Invalidate();
            }
            if (_dragging)
            {
                Point p = Cursor.Position;
                int dx = p.X - _dragStartScreen.X;
                int dy = p.Y - _dragStartScreen.Y;
                if (!_moved && (Math.Abs(dx) > 3 || Math.Abs(dy) > 3)) _moved = true;
                if (_moved) Location = new Point(p.X - _dragOffset.X, p.Y - _dragOffset.Y);
            }
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                if (_pressedPick)
                {
                    _pressedPick = false;
                    Invalidate();
                    if (_pickRect.Contains(e.Location)) { StartPick(); return; }
                }
                if (_dragging)
                {
                    bool wasMoved = _moved;
                    _dragging = false;
                    Capture = false;
                    if (wasMoved)
                    {
                        if (_pendingDpi > 0)
                        {
                            int dpi = _pendingDpi;
                            _pendingDpi = 0;
                            ApplyScale(dpi / 96f);
                        }
                        EnsureVisible();
                        _settings.X = Location.X;
                        _settings.Y = Location.Y;
                        _settings.HasPos = true;
                        Store.Save(_settings);
                    }
                    else if (_swatchRect.Contains(e.Location))
                    {
                        Copy(ColorMath.Hex(_current), "HEX");
                    }
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                _pressedPick = false;
                if (_miTop != null) _miTop.Checked = TopMost;
                _menu.Show(Cursor.Position);
            }
            base.OnMouseUp(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            if (_hoverClose || _hoverPick)
            {
                _hoverClose = false;
                _hoverPick = false;
                Invalidate();
            }
            base.OnMouseLeave(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) { Hide(); e.Handled = true; }
            base.OnKeyDown(e);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyScale(WindowDpi());
            PlaceWindow();
            UpdateRegion();
            RegisterHotKey(Handle, HOT_PICK, MOD_CONTROL | MOD_ALT, (uint)Keys.C);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY && m.WParam.ToInt32() == HOT_PICK)
            {
                StartPick();
                return;
            }
            if (m.Msg == 0x02E0)
            {
                int dpi = (int)(m.WParam.ToInt64() & 0xFFFF);
                if (dpi > 0)
                {
                    if (_dragging) _pendingDpi = dpi;
                    else ApplyScale(dpi / 96f);
                }
                return;
            }
            base.WndProc(ref m);
            if (m.Msg == WM_DISPLAYCHANGE)
            {
                BeginInvoke(new MethodInvoker(delegate
                {
                    ApplyScale(WindowDpi());
                    Invalidate();
                }));
            }
        }

        // ---------------- 绘制 ----------------

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            Theme.Setup(g);
            Rectangle r = new Rectangle(0, 0, Width - 1, Height - 1);
            using (GraphicsPath p = Theme.Round(r, S(12)))
            using (LinearGradientBrush br = new LinearGradientBrush(
                new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height + 1)),
                Color.FromArgb(30, 35, 46), Color.FromArgb(17, 20, 27), 90f))
                g.FillPath(br, p);
            Theme.DrawRound(g, r, S(12), Theme.Border, 1f);
            using (Pen hi = new Pen(Color.FromArgb(20, 255, 255, 255), 1f))
                g.DrawLine(hi, S(12), 1, Width - S(12), 1);

            DrawHeader(g);
            DrawSwatch(g);
            DrawValues(g);
            DrawPickButton(g);
        }

        private void DrawHeader(Graphics g)
        {
            Font title = Theme.F(9f, FontStyle.Bold);
            Theme.TextLeft(g, "ColorSPY", title, Theme.Text, new Rectangle(S(14), S(3), S(110), S(22)));
            SizeF ts = g.MeasureString("ColorSPY", title);
            Theme.TextLeft(g, "2026", Theme.F(7.8f, FontStyle.Bold), Theme.Accent,
                new Rectangle(S(14) + (int)ts.Width + S(4), S(3), S(46), S(22)));

            if (_hoverClose)
                Theme.FillRound(g, _closeRect, S(6), Theme.Alpha(Theme.Bad, 34));
            Color cc = _hoverClose ? Theme.Bad : Theme.Faint;
            using (Pen pen = new Pen(cc, S(1.4f)))
            {
                int pad = S(8);
                g.DrawLine(pen, _closeRect.Left + pad, _closeRect.Top + pad,
                    _closeRect.Right - pad, _closeRect.Bottom - pad);
                g.DrawLine(pen, _closeRect.Right - pad, _closeRect.Top + pad,
                    _closeRect.Left + pad, _closeRect.Bottom - pad);
            }
        }

        private void DrawSwatch(Graphics g)
        {
            Theme.FillRound(g, _swatchRect, S(9), _current);
            Theme.DrawRound(g, _swatchRect, S(9), Color.FromArgb(60, 255, 255, 255), 1f);
        }

        private void DrawValues(Graphics g)
        {
            int x = S(76);
            Font hex = Theme.F(13.5f, FontStyle.Bold);
            g.DrawString(ColorMath.Hex(_current), hex, new SolidBrush(Theme.Text), x, S(18));
            Font small = Theme.F(7.8f);
            Theme.TextLeft(g, string.Format("R {0}   G {1}   B {2}", _current.R, _current.G, _current.B),
                small, Theme.Dim, new Rectangle(x, S(44), Width - x - S(12), S(16)));
            Theme.TextLeft(g, ColorMath.HsvText(_current), small, Theme.Dim,
                new Rectangle(x, S(60), Width - x - S(12), S(16)));

        }

        private void DrawPickButton(Graphics g)
        {
            bool hot = _hoverPick || _pressedPick;
            Color from = hot ? ColorMath.Mix(Theme.Accent, Color.White, 0.12) : Theme.Accent;
            Color to = hot ? ColorMath.Mix(Theme.Accent2, Color.White, 0.12) : Theme.Accent2;
            if (_pressedPick) { from = ColorMath.Mix(from, Color.Black, 0.12); to = ColorMath.Mix(to, Color.Black, 0.12); }

            using (GraphicsPath p = Theme.Round(_pickRect, S(7)))
            using (LinearGradientBrush br = new LinearGradientBrush(_pickRect, from, to, 45f))
                g.FillPath(br, p);

            // 小吸管图标
            using (Pen pen = new Pen(Color.FromArgb(230, 255, 255, 255), S(1.4f)))
            {
                int cx = _pickRect.Left + S(14);
                int cy = _pickRect.Top + S(13);
                g.DrawLine(pen, cx + S(3), cy - S(3), cx - S(3), cy + S(3));
                using (SolidBrush b = new SolidBrush(Color.FromArgb(230, 255, 255, 255)))
                    g.FillEllipse(b, cx - S(5), cy + S(1), S(4), S(4));
            }

            if (!string.IsNullOrEmpty(_flash))
            {
                Theme.TextCenter(g, _flash, Theme.F(8.4f, FontStyle.Bold), Color.White, _pickRect);
            }
            else
            {
                Theme.TextCenter(g, "屏幕取色", Theme.F(9f, FontStyle.Bold), Color.White,
                    new Rectangle(_pickRect.Left + S(20), _pickRect.Top, _pickRect.Width - S(40), _pickRect.Height));
                Theme.TextRight(g, "Ctrl+Alt+C", Theme.F(7f), Color.FromArgb(200, 255, 255, 255),
                    new Rectangle(_pickRect.Left, _pickRect.Top, _pickRect.Width - S(9), _pickRect.Height));
            }
        }

        // ---------------- 清理 ----------------

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _settings.X = Location.X;
            _settings.Y = Location.Y;
            _settings.HasPos = true;
            _settings.TopMost = TopMost;
            _settings.LastColor = _current.ToArgb();
            Store.Save(_settings);
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            UnregisterHotKey(Handle, HOT_PICK);
            if (_flashTimer != null) { _flashTimer.Stop(); _flashTimer.Dispose(); }
            if (_tray != null) { _tray.Visible = false; _tray.Dispose(); }
            if (_menu != null) _menu.Dispose();
            base.OnFormClosed(e);
        }
    }
}
