using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
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
        private const int HotX = 20;
        private const int HotY = 12;
        private const int CanvasSize = 64;

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
        private static extern bool GetCursorPos(out POINT point);

        [DllImport("user32.dll")]
        private static extern bool SetProcessDPIAware();

        [DllImport("dwmapi.dll")]
        private static extern int DwmFlush();

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT { public int x; public int y; }

        [StructLayout(LayoutKind.Sequential)]
        private struct SIZE { public int cx; public int cy; }

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

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        private struct BLENDFUNCTION
        {
            public byte BlendOp;
            public byte BlendFlags;
            public byte SourceConstantAlpha;
            public byte AlphaFormat;
        }

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(ref CURSORINFO pci);

        [DllImport("user32.dll")]
        private static extern bool GetIconInfo(IntPtr hIcon, out ICONINFO pii);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private static IntPtr normalCursor = IntPtr.Zero;
        private static IntPtr blankCursor = IntPtr.Zero;
        private static CursorFrame[] frames;
        private static readonly int[] sequence = new int[] { 0, 1, 2, 3, 4, 4, 3, 2, 1, 0 };
        private static int frameIndex = 0;
        private static bool pulseActive = false;
        private static bool enabled = true;
        private static bool wasDown = false;
        private static bool debug = false;
        private static LayeredOverlay overlay;
        private static NotifyIcon tray;
        private static ToolStripMenuItem enabledItem;
        private static System.Windows.Forms.Timer inputTimer;
        private static System.Windows.Forms.Timer renderTimer;
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

            normalCursor = LoadImage(IntPtr.Zero, Path.Combine(appDir, "PaperPlaneGlow.cur"), IMAGE_CURSOR, 40, 40, LR_LOADFROMFILE);
            blankCursor = LoadImage(IntPtr.Zero, Path.Combine(appDir, "PaperPlaneGlow-Blank.cur"), IMAGE_CURSOR, 40, 40, LR_LOADFROMFILE);
            if (normalCursor == IntPtr.Zero || blankCursor == IntPtr.Zero)
            {
                Log("Cursor load failed. error=" + Marshal.GetLastWin32Error());
                MessageBox.Show("Could not load the cursor files.", "Paper Plane Click Cursor", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            frames = new CursorFrame[5];
            for (int i = 0; i < frames.Length; i++)
            {
                int size = 40 + i * 2;
                frames[i] = CursorFrame.Load(Path.Combine(appDir, "PaperPlaneGlow-Frame" + size + ".png"), CanvasSize, HotX, HotY);
            }

            ApplyNormal();

            overlay = new LayeredOverlay(CanvasSize, CanvasSize);
            overlay.PrepareForUse();

            inputTimer = new System.Windows.Forms.Timer();
            inputTimer.Interval = 12;
            inputTimer.Tick += InputTimerTick;
            inputTimer.Start();

            renderTimer = new System.Windows.Forms.Timer();
            renderTimer.Interval = 20;
            renderTimer.Tick += RenderTimerTick;

            ContextMenuStrip menu = new ContextMenuStrip();
            enabledItem = new ToolStripMenuItem("Click Grow: Enabled");
            enabledItem.Checked = true;
            enabledItem.Click += EnabledItemClick;
            menu.Items.Add(enabledItem);
            menu.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem sizeInfo = new ToolStripMenuItem("Overlay animation: 40 -> 48 -> 40 px");
            sizeInfo.Enabled = false;
            menu.Items.Add(sizeInfo);
            menu.Items.Add(new ToolStripSeparator());
            ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit");
            exitItem.Click += ExitItemClick;
            menu.Items.Add(exitItem);

            tray = new NotifyIcon();
            try { tray.Icon = new Icon(iconPath); }
            catch { tray.Icon = SystemIcons.Application; }
            tray.Text = "Paper Plane Glow - Smooth Click Overlay";
            tray.ContextMenuStrip = menu;
            tray.Visible = true;

            Log("Started; debug=" + debug);
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
            POINT p;
            if (!GetCursorPos(out p)) return;

            frameIndex = 0;
            overlay.Render(frames[sequence[0]], p.x - HotX, p.y - HotY);
            overlay.WaitForPresentation();
            IntPtr blank = CopyIcon(blankCursor);
            if (blank == IntPtr.Zero) return;
            if (!SetSystemCursor(blank, OCR_NORMAL))
            {
                DestroyCursor(blank);
                overlay.HideOverlay();
                return;
            }

            pulseActive = true;
            frameIndex = 1;
            renderTimer.Start();
            if (debug) Log("Overlay pulse started");
        }

        private static void RenderTimerTick(object sender, EventArgs e)
        {
            if (!pulseActive) return;
            if (frameIndex >= sequence.Length)
            {
                FinishPulse();
                return;
            }

            POINT p;
            if (GetCursorPos(out p))
            {
                overlay.Render(frames[sequence[frameIndex]], p.x - HotX, p.y - HotY);
            }
            frameIndex++;
        }

        private static void FinishPulse()
        {
            renderTimer.Stop();
            ApplyNormal();
            overlay.WaitForPresentation();
            overlay.HideOverlay();
            pulseActive = false;
            if (debug) Log("Overlay pulse finished");
        }

        private static void ApplyNormal()
        {
            IntPtr copy = CopyIcon(normalCursor);
            if (copy == IntPtr.Zero) return;
            if (!SetSystemCursor(copy, OCR_NORMAL))
            {
                DestroyCursor(copy);
                Log("SetSystemCursor(normal) failed error=" + Marshal.GetLastWin32Error());
            }
        }

        private static void EnabledItemClick(object sender, EventArgs e)
        {
            enabled = !enabled;
            enabledItem.Checked = enabled;
            enabledItem.Text = enabled ? "Click Grow: Enabled" : "Click Grow: Disabled";
            if (!enabled)
            {
                renderTimer.Stop();
                ApplyNormal();
                overlay.HideOverlay();
                pulseActive = false;
            }
        }

        private static void ExitItemClick(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private static void Cleanup()
        {
            try { if (inputTimer != null) inputTimer.Stop(); } catch { }
            try { if (renderTimer != null) renderTimer.Stop(); } catch { }
            ApplyNormal();
            if (overlay != null)
            {
                overlay.HideOverlay();
                overlay.Dispose();
                overlay = null;
            }
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

        private sealed class CursorFrame
        {
            public byte[] Pixels;

            public static CursorFrame Load(string path, int canvasSize, int hotX, int hotY)
            {
                using (Bitmap source = new Bitmap(path))
                using (Bitmap canvas = new Bitmap(canvasSize, canvasSize, PixelFormat.Format32bppArgb))
                {
                    int size = source.Width;
                    int sourceHotX = (int)Math.Round(size * 12.0 / 48.0);
                    int sourceHotY = (int)Math.Round(size * 8.0 / 48.0);
                    using (Graphics g = Graphics.FromImage(canvas))
                    {
                        g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                        g.DrawImageUnscaled(source, hotX - sourceHotX, hotY - sourceHotY);
                    }

                    CursorFrame frame = new CursorFrame();
                    frame.Pixels = new byte[canvasSize * canvasSize * 4];
                    BitmapData data = canvas.LockBits(new Rectangle(0, 0, canvasSize, canvasSize), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    try
                    {
                        byte[] raw = new byte[canvasSize * canvasSize * 4];
                        Marshal.Copy(data.Scan0, raw, 0, raw.Length);
                        for (int i = 0; i < raw.Length; i += 4)
                        {
                            int a = raw[i + 3];
                            frame.Pixels[i] = (byte)(raw[i] * a / 255);
                            frame.Pixels[i + 1] = (byte)(raw[i + 1] * a / 255);
                            frame.Pixels[i + 2] = (byte)(raw[i + 2] * a / 255);
                            frame.Pixels[i + 3] = (byte)a;
                        }
                    }
                    finally
                    {
                        canvas.UnlockBits(data);
                    }
                    return frame;
                }
            }
        }

        private sealed class LayeredOverlay : Form
        {
            private readonly int width;
            private readonly int height;
            private IntPtr memoryDc = IntPtr.Zero;
            private IntPtr dib = IntPtr.Zero;
            private IntPtr bits = IntPtr.Zero;
            private IntPtr oldBitmap = IntPtr.Zero;

            private const int WS_EX_LAYERED = 0x00080000;
            private const int WS_EX_TRANSPARENT = 0x00000020;
            private const int WS_EX_TOOLWINDOW = 0x00000080;
            private const int WS_EX_NOACTIVATE = 0x08000000;
            private const int WS_EX_TOPMOST = 0x00000008;
            private const int ULW_ALPHA = 2;
            private const byte AC_SRC_OVER = 0;
            private const byte AC_SRC_ALPHA = 1;
            private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
            private const uint SWP_NOMOVE = 0x0002;
            private const uint SWP_NOSIZE = 0x0001;
            private const uint SWP_NOACTIVATE = 0x0010;
            private const uint SWP_SHOWWINDOW = 0x0040;
            private const int SW_HIDE = 0;

            [StructLayout(LayoutKind.Sequential)]
            private struct BITMAPINFOHEADER
            {
                public int biSize;
                public int biWidth;
                public int biHeight;
                public short biPlanes;
                public short biBitCount;
                public int biCompression;
                public int biSizeImage;
                public int biXPelsPerMeter;
                public int biYPelsPerMeter;
                public int biClrUsed;
                public int biClrImportant;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct RGBQUAD { public byte b, g, r, reserved; }

            [StructLayout(LayoutKind.Sequential)]
            private struct BITMAPINFO
            {
                public BITMAPINFOHEADER bmiHeader;
                public RGBQUAD bmiColors;
            }

            [DllImport("user32.dll", SetLastError = true)]
            private static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pptSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);

            [DllImport("gdi32.dll")]
            private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

            [DllImport("gdi32.dll")]
            private static extern IntPtr CreateDIBSection(IntPtr hdc, ref BITMAPINFO pbmi, uint usage, out IntPtr bits, IntPtr section, uint offset);

            [DllImport("gdi32.dll")]
            private static extern IntPtr SelectObject(IntPtr hdc, IntPtr obj);

            [DllImport("gdi32.dll")]
            private static extern bool DeleteObject(IntPtr obj);

            [DllImport("gdi32.dll")]
            private static extern bool DeleteDC(IntPtr hdc);

            [DllImport("user32.dll")]
            private static extern IntPtr GetDC(IntPtr hwnd);

            [DllImport("user32.dll")]
            private static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

            [DllImport("user32.dll")]
            private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

            [DllImport("user32.dll")]
            private static extern bool ShowWindow(IntPtr hwnd, int command);

            [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
            private static extern IntPtr GetWindowLongPtr(IntPtr hwnd, int index);

            public LayeredOverlay(int w, int h)
            {
                width = w;
                height = h;
                AutoScaleMode = AutoScaleMode.None;
                FormBorderStyle = FormBorderStyle.None;
                ShowInTaskbar = false;
                TopMost = true;
                StartPosition = FormStartPosition.Manual;
                Size = new Size(w, h);
                SetStyle(ControlStyles.Opaque, true);
                CreateHandle();
                CreateSurface();
            }

            protected override bool ShowWithoutActivation { get { return true; } }

            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams cp = base.CreateParams;
                    cp.ExStyle |= WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_TOPMOST;
                    return cp;
                }
            }

            private void CreateSurface()
            {
                memoryDc = CreateCompatibleDC(IntPtr.Zero);
                BITMAPINFO bmi = new BITMAPINFO();
                bmi.bmiHeader.biSize = Marshal.SizeOf(typeof(BITMAPINFOHEADER));
                bmi.bmiHeader.biWidth = width;
                bmi.bmiHeader.biHeight = -height;
                bmi.bmiHeader.biPlanes = 1;
                bmi.bmiHeader.biBitCount = 32;
                bmi.bmiHeader.biCompression = 0;
                dib = CreateDIBSection(memoryDc, ref bmi, 0, out bits, IntPtr.Zero, 0);
                oldBitmap = SelectObject(memoryDc, dib);
            }

            public void PrepareForUse()
            {
                if (Visible) return;

                byte[] empty = new byte[width * height * 4];
                Marshal.Copy(empty, 0, bits, empty.Length);
                POINT dst = new POINT(); dst.x = -32000; dst.y = -32000;
                POINT src = new POINT(); src.x = 0; src.y = 0;
                SIZE size = new SIZE(); size.cx = width; size.cy = height;
                BLENDFUNCTION blend = new BLENDFUNCTION();
                blend.BlendOp = AC_SRC_OVER;
                blend.SourceConstantAlpha = 255;
                blend.AlphaFormat = AC_SRC_ALPHA;
                IntPtr screen = GetDC(IntPtr.Zero);
                bool updated = UpdateLayeredWindow(Handle, screen, ref dst, ref size, memoryDc, ref src, 0, ref blend, ULW_ALPHA);
                ReleaseDC(IntPtr.Zero, screen);
                if (!updated) Log("Overlay prewarm update failed error=" + Marshal.GetLastWin32Error());

                Location = new Point(-32000, -32000);
                Size = new Size(width, height);
                Show();
                SetWindowPos(Handle, HWND_TOPMOST, -32000, -32000, width, height, SWP_NOACTIVATE | SWP_SHOWWINDOW);
                WaitForPresentation();
                if (debug) Log("Overlay prewarmed visible=" + Visible + " bounds=" + Bounds);
            }

            public void WaitForPresentation()
            {
                try { DwmFlush(); }
                catch { }
            }
            public void Render(CursorFrame frame, int left, int top)
            {
                Marshal.Copy(frame.Pixels, 0, bits, frame.Pixels.Length);
                POINT dst = new POINT(); dst.x = left; dst.y = top;
                POINT src = new POINT(); src.x = 0; src.y = 0;
                SIZE size = new SIZE(); size.cx = width; size.cy = height;
                BLENDFUNCTION blend = new BLENDFUNCTION();
                blend.BlendOp = AC_SRC_OVER;
                blend.SourceConstantAlpha = 255;
                blend.AlphaFormat = AC_SRC_ALPHA;
                IntPtr screen = GetDC(IntPtr.Zero);
                bool updated = UpdateLayeredWindow(Handle, screen, ref dst, ref size, memoryDc, ref src, 0, ref blend, ULW_ALPHA);
                int updateError = Marshal.GetLastWin32Error();
                ReleaseDC(IntPtr.Zero, screen);
                if (!updated) Log("UpdateLayeredWindow failed error=" + updateError);
                if (debug) Log("Render left=" + left + " top=" + top + " visibleBefore=" + Visible + " updated=" + updated);

                if (!Visible)
                {
                    Location = new Point(left, top);
                    Size = new Size(width, height);
                    Show();
                    screen = GetDC(IntPtr.Zero);
                    UpdateLayeredWindow(Handle, screen, ref dst, ref size, memoryDc, ref src, 0, ref blend, ULW_ALPHA);
                    ReleaseDC(IntPtr.Zero, screen);
                    if (debug) Log("Overlay shown visible=" + Visible + " bounds=" + Bounds);
                    bool topmost = SetWindowPos(Handle, HWND_TOPMOST, left, top, width, height, SWP_NOACTIVATE | SWP_SHOWWINDOW);
                    if (!topmost) Log("SetWindowPos topmost failed error=" + Marshal.GetLastWin32Error());
                    else if (debug) Log("Topmost set once; ex=" + GetWindowLongPtr(Handle, -20));
                }

            }

            public void HideOverlay()
            {
                if (debug) Log("Overlay hide; visibleBefore=" + Visible);
                if (Handle != IntPtr.Zero)
                {
                    bool hidden = SetWindowPos(Handle, HWND_TOPMOST, -32000, -32000, width, height, SWP_NOSIZE | SWP_NOACTIVATE);
                    if (!hidden) Log("Overlay move offscreen failed error=" + Marshal.GetLastWin32Error());
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (oldBitmap != IntPtr.Zero) SelectObject(memoryDc, oldBitmap);
                if (dib != IntPtr.Zero) DeleteObject(dib);
                if (memoryDc != IntPtr.Zero) DeleteDC(memoryDc);
                base.Dispose(disposing);
            }
        }
    }
}
