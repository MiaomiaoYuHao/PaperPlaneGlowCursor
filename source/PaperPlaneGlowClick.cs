using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace PaperPlaneGlowClick
{
    static class Program
    {
        private const uint IMAGE_CURSOR = 2;
        private const uint LR_LOADFROMFILE = 0x0010;
        private const uint OCR_NORMAL = 32512;
        private const int VK_LBUTTON = 0x01;
        private const int CursorSize = 48;
        private const int PulseDurationMs = 220;

        [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern IntPtr LoadImage(IntPtr hInst, string name, uint type, int cx, int cy, uint flags);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool SetSystemCursor(IntPtr hcur, uint id);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CopyIcon(IntPtr hIcon);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyCursor(IntPtr hCursor);

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x; public int y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct CURSORINFO
        {
            public int cbSize;
            public int flags;
            public IntPtr hCursor;
            public POINT ptScreenPos;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ICONINFO
        {
            public bool fIcon;
            public int xHotspot;
            public int yHotspot;
            public IntPtr hbmMask;
            public IntPtr hbmColor;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(ref CURSORINFO pci);

        [DllImport("user32.dll")]
        private static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO pii);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private static IntPtr normalCursor = IntPtr.Zero;
        private static string animatedCursorPath;
        private static bool pulseActive = false;
        private static bool enabled = true;
        private static bool wasDown = false;
        private static bool debug = false;
        private static NotifyIcon tray;
        private static ToolStripMenuItem enabledItem;
        private static System.Windows.Forms.Timer inputTimer;
        private static System.Windows.Forms.Timer animationTimer;
        private static string logPath;
        private static Mutex instanceMutex;

        [STAThread]
        static void Main(string[] args)
        {
            bool createdNew;
            instanceMutex = new Mutex(true, "Local\\PaperPlaneGlowClickCursor", out createdNew);
            if (!createdNew) return;

            SetProcessDPIAware();
            debug = args.Length > 0 && String.Equals(args[0], "--debug", StringComparison.OrdinalIgnoreCase);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string iconPath = Path.Combine(appDir, "PaperPlaneGlow.ico");
            logPath = Path.Combine(appDir, "PaperPlaneGlowClick.log");
            animatedCursorPath = Path.Combine(appDir, "PaperPlaneGlowClick.ani");
            normalCursor = LoadImage(IntPtr.Zero, Path.Combine(appDir, "PaperPlaneGlow.cur"), IMAGE_CURSOR, CursorSize, CursorSize, LR_LOADFROMFILE);
            if (normalCursor == IntPtr.Zero)
            {
                Log("Normal cursor load failed. error=" + Marshal.GetLastWin32Error());
                MessageBox.Show("Could not load PaperPlaneGlow.cur.", "Paper Plane Glow Cursor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            IntPtr animatedProbe = LoadImage(IntPtr.Zero, animatedCursorPath, IMAGE_CURSOR, CursorSize, CursorSize, LR_LOADFROMFILE);
            if (animatedProbe == IntPtr.Zero)
            {
                Log("Animated cursor load failed. error=" + Marshal.GetLastWin32Error());
                MessageBox.Show("Could not load PaperPlaneGlowClick.ani.", "Paper Plane Glow Cursor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            DestroyCursor(animatedProbe);

            ApplyNormal();

            inputTimer = new System.Windows.Forms.Timer();
            inputTimer.Interval = 12;
            inputTimer.Tick += InputTimerTick;
            inputTimer.Start();

            animationTimer = new System.Windows.Forms.Timer();
            animationTimer.Interval = PulseDurationMs;
            animationTimer.Tick += AnimationTimerTick;

            ContextMenuStrip menu = new ContextMenuStrip();
            enabledItem = new ToolStripMenuItem("Click Grow: Enabled");
            enabledItem.Checked = true;
            enabledItem.Click += EnabledItemClick;
            menu.Items.Add(enabledItem);
            menu.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem sizeInfo = new ToolStripMenuItem("Animated cursor: 40 -> 48 -> 40 px");
            sizeInfo.Enabled = false;
            menu.Items.Add(sizeInfo);
            menu.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += ExitItemClick;
            menu.Items.Add(exitItem);

            tray = new NotifyIcon();
            try { tray.Icon = new Icon(iconPath); }
            catch { tray.Icon = SystemIcons.Application; }
            tray.Text = "Paper Plane Glow - Animated Click Cursor";
            tray.ContextMenuStrip = menu;
            tray.Visible = true;

            Log("Started; debug=" + debug + "; engine=animated-cursor");
            Application.Run();
            Cleanup();
        }

        private static void InputTimerTick(object sender, EventArgs e)
        {
            bool down = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;
            if (enabled && down && !wasDown && !pulseActive && IsNormalArrowVisible())
            {
                StartPulse();
            }
            wasDown = down;
        }

        private static bool IsNormalArrowVisible()
        {
            CURSORINFO ci = new CURSORINFO();
            ci.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
            if (!GetCursorInfo(ref ci) || ci.flags != 1) return false;
            ICONINFO ii;
            if (!GetIconInfo(ci.hCursor, out ii)) return false;
            bool isArrow = ii.xHotspot >= 3 && ii.xHotspot <= 14 && ii.yHotspot >= 3 && ii.yHotspot <= 10;
            if (ii.hbmMask != IntPtr.Zero) DeleteObject(ii.hbmMask);
            if (ii.hbmColor != IntPtr.Zero) DeleteObject(ii.hbmColor);
            return isArrow;
        }

        private static void StartPulse()
        {
            // Load a fresh cursor for each pulse so the native animation starts at frame zero.
            // CopyIcon flattens .ani files to a static frame, so the file-backed cursor handle
            // must be passed directly to SetSystemCursor, which takes ownership of it.
            IntPtr animated = LoadImage(IntPtr.Zero, animatedCursorPath, IMAGE_CURSOR, CursorSize, CursorSize, LR_LOADFROMFILE);
            if (animated == IntPtr.Zero)
            {
                Log("Animated cursor reload failed. error=" + Marshal.GetLastWin32Error());
                return;
            }

            if (!SetSystemCursor(animated, OCR_NORMAL))
            {
                int error = Marshal.GetLastWin32Error();
                DestroyCursor(animated);
                Log("SetSystemCursor(animated) failed error=" + error);
                return;
            }

            pulseActive = true;
            animationTimer.Start();
            if (debug) Log("Animated cursor pulse started");
        }

        private static void AnimationTimerTick(object sender, EventArgs e)
        {
            if (!pulseActive) return;
            FinishPulse();
        }

        private static void FinishPulse()
        {
            animationTimer.Stop();
            ApplyNormal();
            pulseActive = false;
            if (debug) Log("Animated cursor pulse finished");
        }

        private static void ApplyNormal()
        {
            IntPtr copy = CopyIcon(normalCursor);
            if (copy == IntPtr.Zero) return;
            if (!SetSystemCursor(copy, OCR_NORMAL))
            {
                int error = Marshal.GetLastWin32Error();
                DestroyCursor(copy);
                Log("SetSystemCursor(normal) failed error=" + error);
            }
        }

        private static void EnabledItemClick(object sender, EventArgs e)
        {
            enabled = !enabled;
            enabledItem.Checked = enabled;
            enabledItem.Text = enabled ? "Click Grow: Enabled" : "Click Grow: Disabled";
            if (!enabled)
            {
                if (pulseActive) FinishPulse();
                else ApplyNormal();
            }
        }

        private static void ExitItemClick(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private static void Cleanup()
        {
            try { if (inputTimer != null) inputTimer.Stop(); } catch { }
            try { if (animationTimer != null) animationTimer.Stop(); } catch { }
            ApplyNormal();
            if (tray != null)
            {
                tray.Visible = false;
                tray.Dispose();
                tray = null;
            }
            Log("Stopped");
            if (instanceMutex != null) instanceMutex.ReleaseMutex();
        }

        private static void Log(string message)
        {
            try { File.AppendAllText(logPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + message + Environment.NewLine); }
            catch { }
        }
    }
}