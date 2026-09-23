using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FliqloClock
{
    public class ScreensaverForm : Form
    {
        [DllImport("user32.dll")]
        private static extern IntPtr SetParent(IntPtr hWndChild, IntPtr hWndNewParent);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        private static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern int ShowCursor(bool bShow);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private const int GWL_STYLE = -16;
        private const int WS_CHILD = 0x40000000;
        private const uint SWP_NOZORDER = 0x0004;
        private const uint SWP_FRAMECHANGED = 0x0020;
        private const uint SWP_SHOWWINDOW = 0x0040;

        private static bool cursorIsHidden = false;
        private static readonly object cursorLock = new object();

        private Timer slowTimer;
        private Timer fastTimer;

        private Settings settings;
        private FlipCard hoursCard;
        private FlipCard minutesCard;
        private FlipCard secondsCard;

        private string currentHourValue = "";
        private string targetHourValue = "";
        private float hourProgress = 1.0f;

        private string currentMinuteValue = "";
        private string targetMinuteValue = "";
        private float minuteProgress = 1.0f;

        private string currentSecondValue = "";
        private string targetSecondValue = "";
        private float secondProgress = 1.0f;

        private bool previewMode = false;
        private bool windowedMode = false;
        private Point originalMouseLocation = Point.Empty;
        private DateTime loadTime = DateTime.MinValue;
        private bool paintedOnce = false;

        public ScreensaverForm(Rectangle bounds) : this(bounds, false)
        {
        }

        public ScreensaverForm(Rectangle bounds, bool windowed)
        {
            Log("ScreensaverForm ctor start: windowed=" + windowed + " bounds=" + bounds);
            this.windowedMode = windowed;
            this.StartPosition = FormStartPosition.Manual;

            if (windowed)
            {
                this.FormBorderStyle = FormBorderStyle.Sizable;
                this.Text = "Fliqlo Flip Clock";
                this.ClientSize = new Size(1000, 560);
                this.StartPosition = FormStartPosition.CenterScreen;
                this.ShowInTaskbar = true;
                this.TopMost = false;
            }
            else
            {
                Log("ScreensaverForm: Setting Bounds=" + bounds);
                this.Bounds = bounds;
                Log("ScreensaverForm: Setting FormBorderStyle.None");
                this.FormBorderStyle = FormBorderStyle.None;
                Log("ScreensaverForm: Setting ShowInTaskbar=false");
                this.ShowInTaskbar = false;
                Log("ScreensaverForm: Setting TopMost=true");
                this.TopMost = true;
            }

            Log("ScreensaverForm: Calling InitializeClock()");
            InitializeClock();
            Log("ScreensaverForm ctor finished.");
        }

        public ScreensaverForm(IntPtr previewHandle)
        {
            this.previewMode = true;
            this.StartPosition = FormStartPosition.Manual;
            this.FormBorderStyle = FormBorderStyle.None;
            this.ShowInTaskbar = false;

            InitializeClock();

            SetParent(this.Handle, previewHandle);
            int style = GetWindowLong(this.Handle, GWL_STYLE);
            style |= WS_CHILD;
            SetWindowLong(this.Handle, GWL_STYLE, style);

            RECT rect;
            if (GetClientRect(previewHandle, out rect))
            {
                this.Size = new Size(rect.Right - rect.Left, rect.Bottom - rect.Top);
            }
            this.Location = new Point(0, 0);

            SetWindowPos(this.Handle, IntPtr.Zero, 0, 0, this.Width, this.Height,
                SWP_NOZORDER | SWP_FRAMECHANGED | SWP_SHOWWINDOW);
        }

        private void InitializeClock()
        {
            Log("InitializeClock start");
            settings = new Settings();
            Log("settings.Load()");
            settings.Load();

            Log("creating FlipCards");
            hoursCard = new FlipCard();
            minutesCard = new FlipCard();
            secondsCard = new FlipCard();

            Log("configuring Form styles");
            this.DoubleBuffered = true;
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.BackColor = Color.FromArgb(15, 16, 18);

            Log("calling InitializeTime()");
            InitializeTime();

            this.Load += ScreensaverForm_Load;
            this.MouseMove += ScreensaverForm_MouseMove;
            this.MouseDown += ScreensaverForm_MouseDown;
            this.KeyDown += ScreensaverForm_KeyDown;
            this.Resize += ScreensaverForm_Resize;
            this.FormClosing += ScreensaverForm_FormClosing;
            this.FormClosed += ScreensaverForm_FormClosed;

            Log("creating slowTimer");
            slowTimer = new Timer();
            slowTimer.Interval = 200;
            slowTimer.Tick += SlowTimer_Tick;
            slowTimer.Start();

            Log("creating fastTimer");
            fastTimer = new Timer();
            fastTimer.Interval = 16;
            fastTimer.Tick += FastTimer_Tick;
            Log("InitializeClock finished");
        }

        private void ScreensaverForm_Resize(object sender, EventArgs e)
        {
            this.Invalidate();
        }

        private void InitializeTime()
        {
            DateTime now = DateTime.Now;
            int hour = now.Hour;
            if (!settings.Is24Hour)
            {
                hour = hour % 12;
                if (hour == 0) hour = 12;
            }

            string hourStr = settings.Is24Hour ? hour.ToString("D2") : hour.ToString();
            string minStr = now.Minute.ToString("D2");
            string secStr = now.Second.ToString("D2");

            currentHourValue = hourStr;
            targetHourValue = hourStr;
            hourProgress = 1.0f;

            currentMinuteValue = minStr;
            targetMinuteValue = minStr;
            minuteProgress = 1.0f;

            currentSecondValue = secStr;
            targetSecondValue = secStr;
            secondProgress = 1.0f;
        }

        private void ScreensaverForm_Load(object sender, EventArgs e)
        {
            Log("ScreensaverForm_Load called. Bounds=" + this.Bounds + " windowed=" + windowedMode);

            if (!previewMode && !windowedMode)
            {
                HideCursorSafely();
                originalMouseLocation = Cursor.Position;
                loadTime = DateTime.UtcNow;
            }
        }

        private static void HideCursorSafely()
        {
            lock (cursorLock)
            {
                if (!cursorIsHidden)
                {
                    Cursor.Hide();
                    cursorIsHidden = true;
                }
            }
        }

        private static void RestoreCursorSafely()
        {
            lock (cursorLock)
            {
                if (cursorIsHidden)
                {
                    Cursor.Show();
                    cursorIsHidden = false;
                }
            }
        }

        private void SlowTimer_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            int hour = now.Hour;

            if (!settings.Is24Hour)
            {
                hour = hour % 12;
                if (hour == 0) hour = 12;
            }

            string nextHourStr = settings.Is24Hour ? hour.ToString("D2") : hour.ToString();
            string nextMinStr = now.Minute.ToString("D2");
            string nextSecStr = now.Second.ToString("D2");

            bool needsAnimation = false;

            if (nextHourStr != targetHourValue)
            {
                currentHourValue = targetHourValue;
                targetHourValue = nextHourStr;
                hourProgress = 0.0f;
                needsAnimation = true;
            }

            if (nextMinStr != targetMinuteValue)
            {
                currentMinuteValue = targetMinuteValue;
                targetMinuteValue = nextMinStr;
                minuteProgress = 0.0f;
                needsAnimation = true;
            }

            if (settings.ShowSeconds && nextSecStr != targetSecondValue)
            {
                currentSecondValue = targetSecondValue;
                targetSecondValue = nextSecStr;
                secondProgress = 0.0f;
                needsAnimation = true;
            }

            if (needsAnimation)
            {
                fastTimer.Start();
            }
        }

        private void FastTimer_Tick(object sender, EventArgs e)
        {
            bool animating = false;
            float mainStep = 0.046f; // ~350ms flip duration for hours/minutes
            float secStep = 0.080f;  // ~200ms flip duration for seconds

            if (hourProgress < 1.0f)
            {
                hourProgress += mainStep;
                if (hourProgress >= 1.0f)
                {
                    hourProgress = 1.0f;
                    currentHourValue = targetHourValue;
                }
                else
                {
                    animating = true;
                }
            }

            if (minuteProgress < 1.0f)
            {
                minuteProgress += mainStep;
                if (minuteProgress >= 1.0f)
                {
                    minuteProgress = 1.0f;
                    currentMinuteValue = targetMinuteValue;
                }
                else
                {
                    animating = true;
                }
            }

            if (secondProgress < 1.0f)
            {
                secondProgress += secStep;
                if (secondProgress >= 1.0f)
                {
                    secondProgress = 1.0f;
                    currentSecondValue = targetSecondValue;
                }
                else
                {
                    animating = true;
                }
            }

            this.Invalidate();

            if (!animating)
            {
                fastTimer.Stop();
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;

            int clientW = this.ClientSize.Width;
            int clientH = this.ClientSize.Height;

            if (clientW <= 0 || clientH <= 0) return;

            if (!paintedOnce)
            {
                paintedOnce = true;
                Log("OnPaint first frame rendered. Size=" + clientW + "x" + clientH);
            }

            float maxH = clientH * 0.56f;
            float widthFactor = settings.ShowSeconds ? 2.28f : 1.75f;
            float maxW = (clientW * 0.88f) / widthFactor;
            float baseScale = Math.Min(maxH, maxW);

            int cardHeight = (int)(baseScale * settings.ClockScale);
            int cardWidth = (int)(cardHeight * 0.84f);

            if (cardWidth < 8) cardWidth = 8;
            if (cardHeight < 8) cardHeight = 8;

            int gap = Math.Max(2, (int)(cardWidth * 0.075f));

            int totalWidth;
            int secWidth = 0;
            int secHeight = 0;
            int secGap = 0;

            if (settings.ShowSeconds)
            {
                secHeight = (int)(cardHeight * 0.58f);
                secWidth = (int)(cardWidth * 0.58f);
                secGap = Math.Max(2, (int)(cardWidth * 0.06f));
                totalWidth = 2 * cardWidth + gap + secGap + secWidth;
            }
            else
            {
                totalWidth = 2 * cardWidth + gap;
            }

            int startX = (clientW - totalWidth) / 2;
            int startY = (clientH - cardHeight) / 2;

            string amPmText = null;
            if (!settings.Is24Hour && settings.ShowAmPm)
            {
                amPmText = DateTime.Now.Hour >= 12 ? "PM" : "AM";
            }

            hoursCard.SetValues(currentHourValue, targetHourValue, hourProgress);
            minutesCard.SetValues(currentMinuteValue, targetMinuteValue, minuteProgress);

            hoursCard.Draw(g, startX, startY, cardWidth, cardHeight, amPmText);
            minutesCard.Draw(g, startX + cardWidth + gap, startY, cardWidth, cardHeight, null);

            if (settings.ShowSeconds)
            {
                int secX = startX + 2 * cardWidth + gap + secGap;
                int secY = startY + (cardHeight - secHeight);
                secondsCard.SetValues(currentSecondValue, targetSecondValue, secondProgress);
                secondsCard.Draw(g, secX, secY, secWidth, secHeight, null);
            }
        }

        private void ScreensaverForm_MouseMove(object sender, MouseEventArgs e)
        {
            if (previewMode || windowedMode) return;

            if (DateTime.UtcNow.Subtract(loadTime).TotalMilliseconds < 1500)
            {
                originalMouseLocation = Cursor.Position;
                return;
            }

            if (originalMouseLocation.IsEmpty)
            {
                originalMouseLocation = Cursor.Position;
                return;
            }

            int deltaX = Math.Abs(Cursor.Position.X - originalMouseLocation.X);
            int deltaY = Math.Abs(Cursor.Position.Y - originalMouseLocation.Y);

            if (deltaX > 60 || deltaY > 60)
            {
                ExitScreensaver("MouseMove deltaX=" + deltaX + ", deltaY=" + deltaY);
            }
        }

        private void ScreensaverForm_MouseDown(object sender, MouseEventArgs e)
        {
            if (!previewMode && !windowedMode)
            {
                ExitScreensaver("MouseDown button=" + e.Button);
            }
        }

        private void ScreensaverForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                ExitScreensaver("Escape key");
                return;
            }

            if (!previewMode && !windowedMode)
            {
                ExitScreensaver("KeyDown key=" + e.KeyCode);
            }
        }

        private void ExitScreensaver(string reason = "Unknown")
        {
            Log("ExitScreensaver called: " + reason);
            RestoreCursorSafely();
            Application.Exit();
        }

        private void ScreensaverForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            Log("FormClosing: CloseReason=" + e.CloseReason);
            RestoreCursorSafely();

            if (slowTimer != null)
            {
                slowTimer.Stop();
                slowTimer.Dispose();
                slowTimer = null;
            }
            if (fastTimer != null)
            {
                fastTimer.Stop();
                fastTimer.Dispose();
                fastTimer = null;
            }

            if (hoursCard != null)
            {
                hoursCard.Dispose();
                hoursCard = null;
            }
            if (minutesCard != null)
            {
                minutesCard.Dispose();
                minutesCard = null;
            }
            if (secondsCard != null)
            {
                secondsCard.Dispose();
                secondsCard = null;
            }
        }

        private void ScreensaverForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            Log("FormClosed: CloseReason=" + e.CloseReason);
            RestoreCursorSafely();
        }

        private static void Log(string message)
        {
            try
            {
                string logFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "startup.log");
                File.AppendAllText(logFile, DateTime.Now.ToString("HH:mm:ss.fff") + ": " + message + Environment.NewLine);
            }
            catch {}
        }
    }
}
