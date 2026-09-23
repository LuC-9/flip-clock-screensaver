using System;
using System.Drawing;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FliqloClock
{
    public class SettingsForm : Form
    {
        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private CheckBox chk24Hour;
        private CheckBox chkShowAmPm;
        private CheckBox chkShowSeconds;
        private TrackBar trackScale;
        private Label lblScale;
        private Button btnOk;
        private Button btnCancel;
        private Settings settings;
        private IntPtr parentHwnd = IntPtr.Zero;

        public SettingsForm() : this(IntPtr.Zero)
        {
        }

        public SettingsForm(IntPtr parentHandle)
        {
            this.parentHwnd = parentHandle;
            settings = new Settings();
            settings.Load();
            InitializeComponent();
            LoadSettingsIntoUi();
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                if (parentHwnd != IntPtr.Zero)
                {
                    cp.Parent = parentHwnd;
                }
                return cp;
            }
        }

        private void InitializeComponent()
        {
            this.Text = "Fliqlo Flip Clock Settings";
            this.ClientSize = new Size(330, 290);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowIcon = false;
            this.ShowInTaskbar = (parentHwnd == IntPtr.Zero);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(28, 29, 32);
            this.ForeColor = Color.FromArgb(236, 239, 244);
            this.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);

            chk24Hour = new CheckBox
            {
                Text = "24-Hour Format",
                Location = new Point(32, 18),
                Size = new Size(260, 26),
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat
            };
            chk24Hour.CheckedChanged += Chk24Hour_CheckedChanged;

            chkShowAmPm = new CheckBox
            {
                Text = "Show AM/PM Label (12-Hour)",
                Location = new Point(32, 50),
                Size = new Size(260, 26),
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat
            };

            chkShowSeconds = new CheckBox
            {
                Text = "Show Seconds Card",
                Location = new Point(32, 82),
                Size = new Size(260, 26),
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                FlatStyle = FlatStyle.Flat
            };

            lblScale = new Label
            {
                Text = "Scale: 1.0x",
                Location = new Point(32, 120),
                Size = new Size(260, 20),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular)
            };

            trackScale = new TrackBar
            {
                Location = new Point(26, 144),
                Size = new Size(278, 45),
                Minimum = 5,   // 0.5x
                Maximum = 15,  // 1.5x
                Value = 10,    // 1.0x
                TickStyle = TickStyle.BottomRight,
                TickFrequency = 1,
                BackColor = Color.FromArgb(28, 29, 32)
            };
            trackScale.Scroll += TrackScale_Scroll;

            btnOk = new Button
            {
                Text = "OK",
                Location = new Point(62, 220),
                Size = new Size(95, 34),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                DialogResult = DialogResult.OK,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(50, 52, 58),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderColor = Color.FromArgb(90, 94, 102);
            btnOk.Click += BtnOk_Click;

            btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(172, 220),
                Size = new Size(95, 34),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                DialogResult = DialogResult.Cancel,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(36, 37, 41),
                ForeColor = Color.FromArgb(200, 204, 212),
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(60, 63, 70);
            btnCancel.Click += BtnCancel_Click;

            this.AcceptButton = btnOk;
            this.CancelButton = btnCancel;

            this.Controls.Add(chk24Hour);
            this.Controls.Add(chkShowAmPm);
            this.Controls.Add(chkShowSeconds);
            this.Controls.Add(lblScale);
            this.Controls.Add(trackScale);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);

            this.Load += SettingsForm_Load;
        }

        private void SettingsForm_Load(object sender, EventArgs e)
        {
            if (parentHwnd != IntPtr.Zero)
            {
                RECT parentRect;
                if (GetWindowRect(parentHwnd, out parentRect))
                {
                    int parentW = parentRect.Right - parentRect.Left;
                    int parentH = parentRect.Bottom - parentRect.Top;
                    int x = parentRect.Left + (parentW - this.Width) / 2;
                    int y = parentRect.Top + (parentH - this.Height) / 2;
                    this.Location = new Point(Math.Max(0, x), Math.Max(0, y));
                }
            }
        }

        private void LoadSettingsIntoUi()
        {
            chk24Hour.Checked = settings.Is24Hour;
            chkShowAmPm.Checked = settings.ShowAmPm;
            chkShowAmPm.Enabled = !settings.Is24Hour;
            chkShowSeconds.Checked = settings.ShowSeconds;

            int scaleVal = (int)Math.Round(settings.ClockScale * 10.0f);
            if (scaleVal >= 5 && scaleVal <= 15)
            {
                trackScale.Value = scaleVal;
            }
            else
            {
                trackScale.Value = 10;
            }
            lblScale.Text = string.Format(CultureInfo.InvariantCulture, "Scale: {0:0.0}x", settings.ClockScale);
        }

        private void TrackScale_Scroll(object sender, EventArgs e)
        {
            float scale = trackScale.Value / 10.0f;
            lblScale.Text = string.Format(CultureInfo.InvariantCulture, "Scale: {0:0.0}x", scale);
        }

        private void Chk24Hour_CheckedChanged(object sender, EventArgs e)
        {
            chkShowAmPm.Enabled = !chk24Hour.Checked;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            settings.Is24Hour = chk24Hour.Checked;
            settings.ShowAmPm = chkShowAmPm.Checked;
            settings.ShowSeconds = chkShowSeconds.Checked;
            settings.ClockScale = trackScale.Value / 10.0f;
            settings.Save();
            this.Close();
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
