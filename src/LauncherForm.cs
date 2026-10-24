using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace OmniRouteLauncher
{
    public class LauncherForm : Form
    {
        // UI Controls
        public PictureBox picIcon;
        public Label lblTitle;
        public Label lblSubtitle;
        public Label lblStatus;
        public Label lblEndpoint;
        public Panel pnlDivider;
        public Button btnDashboard;
        public Button btnRestart;
        public Button btnToggle;
        public Button btnClose;

        // State & Timers
        private System.Windows.Forms.Timer _autoCloseTimer;
        private System.Windows.Forms.Timer _animTimer;
        private int _countdownSeconds = 5;
        private Point _initialMousePos = Point.Empty;
        private bool _isServerRunning = false;
        private bool _isBusy = false;
        private Color _statusBorderColor = UITheme.Border;
        private string _animBaseText = "";
        private int _animFrame = 0;

        public LauncherForm()
        {
            InitializeComponent();
            LoadApplicationIcon();
            AttachMouseCancellation(this);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            // Form Properties
            this.Text = "OmniRoute Server";
            this.Size = new Size(500, 205);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.TopMost = true;
            this.BackColor = UITheme.Bg;
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            // picIcon (54x54) Zoom mode at (24, 22)
            picIcon = new PictureBox();
            picIcon.Location = new Point(24, 22);
            picIcon.Size = new Size(54, 54);
            picIcon.SizeMode = PictureBoxSizeMode.Zoom;
            picIcon.BackColor = Color.Transparent;

            // lblTitle ("OmniRoute Server", 13pt Bold) at (92, 22)
            lblTitle = new Label();
            lblTitle.Text = "OmniRoute Server";
            lblTitle.Location = new Point(92, 22);
            lblTitle.AutoSize = true;
            lblTitle.Font = new Font("Segoe UI", 13F, FontStyle.Bold);
            lblTitle.ForeColor = UITheme.Ink;

            // lblSubtitle ("Local server control", 9pt Regular, Muted) at (93, 49)
            lblSubtitle = new Label();
            lblSubtitle.Text = "Local server control";
            lblSubtitle.Location = new Point(93, 49);
            lblSubtitle.AutoSize = true;
            lblSubtitle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            lblSubtitle.ForeColor = UITheme.Muted;

            // lblStatus ("CHECKING", 8pt Bold, Center) at (372, 28), Size 104x27
            lblStatus = new Label();
            lblStatus.Text = "CHECKING";
            lblStatus.Location = new Point(372, 28);
            lblStatus.Size = new Size(104, 27);
            lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            lblStatus.Font = new Font("Segoe UI", 8F, FontStyle.Bold);
            lblStatus.BackColor = UITheme.StatusCheckingBg;
            lblStatus.ForeColor = UITheme.Muted;
            lblStatus.Paint += new PaintEventHandler(lblStatus_Paint);

            // lblEndpoint ("127.0.0.1 : 20128", 9pt) at (93, 72)
            lblEndpoint = new Label();
            lblEndpoint.Text = "127.0.0.1 : 20128";
            lblEndpoint.Location = new Point(93, 72);
            lblEndpoint.AutoSize = true;
            lblEndpoint.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            lblEndpoint.ForeColor = UITheme.Muted;

            // pnlDivider (456x1, Border color) at (22, 105)
            pnlDivider = new Panel();
            pnlDivider.Location = new Point(22, 105);
            pnlDivider.Size = new Size(456, 1);
            pnlDivider.BackColor = UITheme.Border;

            // btnDashboard ("Dashboard", Brand primary) at (22, 125), Size (120, 38)
            btnDashboard = UITheme.CreateButton("Dashboard", new Point(22, 125), new Size(120, 38), UITheme.Brand, Color.White, UITheme.Brand, true);
            btnDashboard.Click += new EventHandler(btnDashboard_Click);

            // btnRestart ("Restart", neutral) at (152, 125), Size (100, 38)
            btnRestart = UITheme.CreateButton("Restart", new Point(152, 125), new Size(100, 38), Color.White, UITheme.Ink, UITheme.Border, false);
            btnRestart.Click += new EventHandler(btnRestart_Click);

            // btnToggle ("Stop" / "Start", colored) at (262, 125), Size (100, 38)
            btnToggle = UITheme.CreateButton("Stop", new Point(262, 125), new Size(100, 38), UITheme.RedBg, UITheme.RedText, UITheme.RedBorder, false);
            btnToggle.Click += new EventHandler(btnToggle_Click);

            // btnClose ("Close", neutral) at (372, 125), Size (106, 38)
            btnClose = UITheme.CreateButton("Close", new Point(372, 125), new Size(106, 38), Color.White, UITheme.Ink, UITheme.Border, false);
            btnClose.Click += new EventHandler(btnClose_Click);

            // Add controls to form
            this.Controls.Add(picIcon);
            this.Controls.Add(lblTitle);
            this.Controls.Add(lblSubtitle);
            this.Controls.Add(lblStatus);
            this.Controls.Add(lblEndpoint);
            this.Controls.Add(pnlDivider);
            this.Controls.Add(btnDashboard);
            this.Controls.Add(btnRestart);
            this.Controls.Add(btnToggle);
            this.Controls.Add(btnClose);

            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void LoadApplicationIcon()
        {
            try
            {
                Icon appIcon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
                if (appIcon != null)
                {
                    this.Icon = appIcon;
                    picIcon.Image = appIcon.ToBitmap();
                    return;
                }
            }
            catch
            {
                // Ignore extraction failures
            }

            try
            {
                string[] iconPaths = new string[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"assets\omniroute.ico"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\assets\omniroute.ico"),
                    @"C:\Users\Admin\Desktop\omni console\assets\omniroute.ico"
                };

                foreach (string path in iconPaths)
                {
                    if (File.Exists(path))
                    {
                        using (Icon ico = new Icon(path, 64, 64))
                        {
                            this.Icon = (Icon)ico.Clone();
                            picIcon.Image = ico.ToBitmap();
                            return;
                        }
                    }
                }
            }
            catch
            {
                // Fallback gracefully
            }
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // Initial non-blocking health check
            ThreadPool.QueueUserWorkItem(delegate
            {
                bool healthy = ServerManager.CheckServerHealthy(ServerManager.DefaultPort);
                SafeInvoke(delegate
                {
                    ApplyServerStatus(healthy, true);
                });
            });
        }

        private void lblStatus_Paint(object sender, PaintEventArgs e)
        {
            using (Pen pen = new Pen(_statusBorderColor, 1))
            {
                Rectangle rect = lblStatus.ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;
                e.Graphics.DrawRectangle(pen, rect);
            }
        }

        private void ApplyServerStatus(bool isRunning, bool isInitial)
        {
            _isServerRunning = isRunning;
            btnDashboard.Enabled = true;
            btnToggle.Enabled = true;

            if (isRunning)
            {
                lblStatus.Text = "RUNNING";
                lblStatus.BackColor = UITheme.GreenBg;
                lblStatus.ForeColor = UITheme.GreenText;
                _statusBorderColor = UITheme.GreenBorder;
                lblStatus.Invalidate();

                btnToggle.Text = "Stop";
                btnToggle.BackColor = UITheme.RedBg;
                btnToggle.ForeColor = UITheme.RedText;
                btnToggle.FlatAppearance.BorderColor = UITheme.RedBorder;
                btnToggle.FlatAppearance.MouseOverBackColor = UITheme.RedHover;
                btnToggle.FlatAppearance.MouseDownBackColor = UITheme.RedBg;

                btnRestart.Enabled = true;

                if (isInitial)
                {
                    StartAutoCloseTimer();
                }
            }
            else
            {
                lblStatus.Text = "STOPPED";
                lblStatus.BackColor = UITheme.RedBg;
                lblStatus.ForeColor = UITheme.RedText;
                _statusBorderColor = UITheme.RedBorder;
                lblStatus.Invalidate();

                btnToggle.Text = "Start";
                btnToggle.BackColor = UITheme.GreenBg;
                btnToggle.ForeColor = UITheme.GreenText;
                btnToggle.FlatAppearance.BorderColor = UITheme.GreenBorder;
                btnToggle.FlatAppearance.MouseOverBackColor = UITheme.GreenHover;
                btnToggle.FlatAppearance.MouseDownBackColor = UITheme.GreenBg;

                btnRestart.Enabled = false;
                CancelAutoClose();
            }
        }

        private void StartStatusAnimation(string baseText)
        {
            _animBaseText = baseText;
            _animFrame = 0;
            lblStatus.Text = baseText;
            lblStatus.BackColor = UITheme.WarningBg;
            lblStatus.ForeColor = UITheme.WarningText;
            _statusBorderColor = UITheme.WarningBg;
            lblStatus.Invalidate();

            if (_animTimer == null)
            {
                _animTimer = new System.Windows.Forms.Timer();
                _animTimer.Interval = 350;
                _animTimer.Tick += delegate
                {
                    _animFrame = (_animFrame + 1) % 4;
                    string dots = new string('.', _animFrame);
                    lblStatus.Text = _animBaseText + dots;
                };
            }
            _animTimer.Start();
        }

        private void StopStatusAnimation()
        {
            if (_animTimer != null)
            {
                _animTimer.Stop();
            }
        }

        private void StartAutoCloseTimer()
        {
            CancelAutoClose();
            _initialMousePos = Cursor.Position;
            _countdownSeconds = 5;
            btnClose.Text = string.Format("Close ({0}s)", _countdownSeconds);

            if (_autoCloseTimer == null)
            {
                _autoCloseTimer = new System.Windows.Forms.Timer();
                _autoCloseTimer.Interval = 1000;
                _autoCloseTimer.Tick += delegate
                {
                    _countdownSeconds--;
                    if (_countdownSeconds <= 0)
                    {
                        _autoCloseTimer.Stop();
                        this.Close();
                    }
                    else
                    {
                        btnClose.Text = string.Format("Close ({0}s)", _countdownSeconds);
                    }
                };
            }
            _autoCloseTimer.Start();
        }

        private void CancelAutoClose()
        {
            if (_autoCloseTimer != null && _autoCloseTimer.Enabled)
            {
                _autoCloseTimer.Stop();
            }
            _countdownSeconds = 5;
            if (btnClose != null && !btnClose.IsDisposed)
            {
                btnClose.Text = "Close";
            }
        }

        private void AttachMouseCancellation(Control ctrl)
        {
            ctrl.MouseMove += Control_MouseMove;
            ctrl.MouseEnter += Control_MouseEnter;
            foreach (Control child in ctrl.Controls)
            {
                AttachMouseCancellation(child);
            }
        }

        private void Control_MouseEnter(object sender, EventArgs e)
        {
            if (_autoCloseTimer != null && _autoCloseTimer.Enabled)
            {
                CancelAutoClose();
            }
        }

        private void Control_MouseMove(object sender, MouseEventArgs e)
        {
            if (_autoCloseTimer != null && _autoCloseTimer.Enabled)
            {
                Point current = Cursor.Position;
                if (_initialMousePos == Point.Empty)
                {
                    _initialMousePos = current;
                    return;
                }

                if (Math.Abs(current.X - _initialMousePos.X) > 2 || Math.Abs(current.Y - _initialMousePos.Y) > 2)
                {
                    CancelAutoClose();
                }
            }
        }

        private void btnDashboard_Click(object sender, EventArgs e)
        {
            CancelAutoClose();
            ServerManager.OpenDashboard();
        }

        private void btnRestart_Click(object sender, EventArgs e)
        {
            CancelAutoClose();
            if (_isBusy) return;
            _isBusy = true;

            btnDashboard.Enabled = false;
            btnRestart.Enabled = false;
            btnToggle.Enabled = false;

            StartStatusAnimation("RESTARTING");

            ThreadPool.QueueUserWorkItem(delegate
            {
                ServerManager.StopServerProcess();
                Thread.Sleep(1000);
                ServerManager.StartServerProcess();

                bool healthy = false;
                for (int i = 0; i < 12; i++)
                {
                    Thread.Sleep(500);
                    if (ServerManager.CheckServerHealthy(ServerManager.DefaultPort))
                    {
                        healthy = true;
                        break;
                    }
                }

                SafeInvoke(delegate
                {
                    StopStatusAnimation();
                    _isBusy = false;
                    ApplyServerStatus(healthy, false);
                });
            });
        }

        private void btnToggle_Click(object sender, EventArgs e)
        {
            CancelAutoClose();
            if (_isBusy) return;
            _isBusy = true;

            btnDashboard.Enabled = false;
            btnRestart.Enabled = false;
            btnToggle.Enabled = false;

            if (_isServerRunning)
            {
                StartStatusAnimation("STOPPING");

                ThreadPool.QueueUserWorkItem(delegate
                {
                    ServerManager.StopServerProcess();
                    Thread.Sleep(1000);
                    bool healthy = ServerManager.CheckServerHealthy(ServerManager.DefaultPort);

                    SafeInvoke(delegate
                    {
                        StopStatusAnimation();
                        _isBusy = false;
                        ApplyServerStatus(healthy, false);
                    });
                });
            }
            else
            {
                StartStatusAnimation("STARTING");

                ThreadPool.QueueUserWorkItem(delegate
                {
                    ServerManager.StartServerProcess();

                    bool healthy = false;
                    for (int i = 0; i < 10; i++)
                    {
                        Thread.Sleep(500);
                        if (ServerManager.CheckServerHealthy(ServerManager.DefaultPort))
                        {
                            healthy = true;
                            break;
                        }
                    }

                    SafeInvoke(delegate
                    {
                        StopStatusAnimation();
                        _isBusy = false;
                        ApplyServerStatus(healthy, false);
                    });
                });
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            CancelAutoClose();
            this.Close();
        }

        private void SafeInvoke(Action action)
        {
            if (this.IsDisposed || !this.IsHandleCreated) return;
            try
            {
                this.BeginInvoke((Action)delegate
                {
                    if (this.IsDisposed) return;
                    action();
                });
            }
            catch (ObjectDisposedException) { }
            catch (InvalidOperationException) { }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_autoCloseTimer != null)
                {
                    _autoCloseTimer.Dispose();
                    _autoCloseTimer = null;
                }
                if (_animTimer != null)
                {
                    _animTimer.Dispose();
                    _animTimer = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
