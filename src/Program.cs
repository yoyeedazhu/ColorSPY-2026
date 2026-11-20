using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace ColorSpy
{
    internal static class Program
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr value);
        [DllImport("shcore.dll")]
        private static extern int SetProcessDpiAwareness(int value);
        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string className, string windowName);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [STAThread]
        private static void Main()
        {
            EnableDpiAwareness();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
                    Theme.Scale = g.DpiX / 96f;
            }
            catch
            {
                Theme.Scale = 1f;
            }

            Application.ThreadException += delegate(object s, ThreadExceptionEventArgs e) { LogCrash(e.Exception); };
            AppDomain.CurrentDomain.UnhandledException += delegate(object s, UnhandledExceptionEventArgs e) { LogCrash(e.ExceptionObject as Exception); };

            bool created;
            using (Mutex mutex = new Mutex(true, "ColorSPY2026.SingleInstance", out created))
            {
                if (!created)
                {
                    ActivateExisting();
                    return;
                }
                try
                {
                    Application.Run(new SpyForm());
                }
                catch (Exception ex)
                {
                    LogCrash(ex);
                    throw;
                }
            }
        }

        private static void EnableDpiAwareness()
        {
            try { if (SetProcessDpiAwarenessContext(new IntPtr(-4))) return; }
            catch { }
            try { if (SetProcessDpiAwareness(2) == 0) return; }
            catch { }
            try { SetProcessDPIAware(); }
            catch { }
        }

        private static void ActivateExisting()
        {
            IntPtr h = FindWindow(null, "ColorSPY 2026");
            if (h == IntPtr.Zero) return;
            ShowWindow(h, 9);
            SetForegroundWindow(h);
        }

        private static void LogCrash(Exception ex)
        {
            try
            {
                File.AppendAllText(Path.Combine(Path.GetTempPath(), "ColorSPY2026-crash.log"),
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " +
                    (ex == null ? "(null)" : ex.ToString()) + Environment.NewLine);
            }
            catch
            {
            }
        }
    }
}
