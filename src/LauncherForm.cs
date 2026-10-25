using System;
using System.Diagnostics;
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

        // System Tray & Context Menu
        private NotifyIcon _trayIcon;
        private ContextMenuStrip _trayMenu;
        private ToolStripMenuItem _trayMenuDashboard;
        private ToolStripMenuItem _trayMenuRestart;
        private ToolStripMenuItem _trayMenuToggle;
        private ToolStripMenuItem _trayMenuLogs;
        private ToolStripMenuItem _trayMenuDataDir;
        private ToolStripMenuItem _trayMenuChangeDataDir;
        private ToolStripMenuItem _trayMenuAutoStart;
        private ToolStripMenuItem _trayMenuWatchdog;
        private ToolStripMenuItem _trayMenuShowWindow;
        private ToolStripMenuItem _trayMenuExit;

        // State & Timers
        private System.Windows.Forms.Timer _autoCloseTimer;
        private System.Windows.Forms.Timer _animTimer;
        private System.Windows.Forms.Timer _monitorTimer;
        private int _countdownSeconds = 5;
        private Point _initialMousePos = Point.Empty;
        private bool _isServerRunning = false;
        private bool _isBusy = false;
        private bool _allowRealExit = false;
        private bool _startMinimized = false;
        private bool _userRequestedStop = false;
        private bool _watchdogEnabled = true;
        private long _currentLatencyMs = -1;
        private Color _statusBorderColor = UITheme.Border;
        private string _animBaseText = "";
        private int _animFrame = 0;

        public LauncherForm(bool startMinimized = false)
        {
            _startMinimized = startMinimized;
            InitializeComponent();
            InitializeTrayIcon();
            LoadApplicationIcon();
            AttachMouseCancellation(this);
            this.KeyPreview = true;
            this.KeyDown += LauncherForm_KeyDown;
        }

        protected override void SetVisibleCore(bool value)
        {
            if (_startMinimized)
            {
                value = false;
                if (!this.IsHandleCreated) CreateHandle();
            }
            base.SetVisibleCore(value);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Program.WM_SHOWME)
            {
                ShowFormFromTray();
            }
            base.WndProc(ref m);
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
            lblEndpoint.Text = string.Format("127.0.0.1 : {0}", ServerManager.ActivePort);
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

        private void InitializeTrayIcon()
        {
            _trayMenu = new ContextMenuStrip();
            _trayMenu.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            _trayMenuDashboard = new ToolStripMenuItem("Open Dashboard", null, delegate { ServerManager.OpenDashboard(); });
            _trayMenuDashboard.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

            _trayMenuRestart = new ToolStripMenuItem("Restart Server", null, delegate { btnRestart_Click(this, EventArgs.Empty); });
            _trayMenuToggle = new ToolStripMenuItem("Stop Server", null, delegate { btnToggle_Click(this, EventArgs.Empty); });

            _trayMenuLogs = new ToolStripMenuItem("View Server Logs", null, delegate { ServerManager.OpenLogsDirectory(); });
            _trayMenuDataDir = new ToolStripMenuItem("Open Data Directory (.omniroute)", null, delegate { ServerManager.OpenDataDirectory(); });
            _trayMenuChangeDataDir = new ToolStripMenuItem("Change Data Directory...", null, delegate { PromptChangeDataDirectory(); });

            bool isAutoStart = ServerManager.IsAutoStartEnabled();
            _trayMenuAutoStart = new ToolStripMenuItem("Start with Windows", null, delegate
            {
                bool newState = !_trayMenuAutoStart.Checked;
                ServerManager.SetAutoStart(newState);
                _trayMenuAutoStart.Checked = newState;
            });
            _trayMenuAutoStart.Checked = isAutoStart;

            _trayMenuWatchdog = new ToolStripMenuItem("Crash Guardian Watchdog", null, delegate
            {
                _watchdogEnabled = !_watchdogEnabled;
                _trayMenuWatchdog.Checked = _watchdogEnabled;
            });
            _trayMenuWatchdog.Checked = _watchdogEnabled;

            _trayMenuShowWindow = new ToolStripMenuItem("Show Console", null, delegate { ShowFormFromTray(); });
            _trayMenuExit = new ToolStripMenuItem("Exit OmniRoute Console", null, delegate { ExitApplication(); });

            _trayMenu.Items.Add(_trayMenuDashboard);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add(_trayMenuRestart);
            _trayMenu.Items.Add(_trayMenuToggle);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add(_trayMenuLogs);
            _trayMenu.Items.Add(_trayMenuDataDir);
            _trayMenu.Items.Add(_trayMenuChangeDataDir);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add(_trayMenuAutoStart);
            _trayMenu.Items.Add(_trayMenuWatchdog);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add(_trayMenuShowWindow);
            _trayMenu.Items.Add(_trayMenuExit);

            _trayIcon = new NotifyIcon();
            _trayIcon.Text = string.Format("OmniRoute Console (Port {0})...", ServerManager.ActivePort);
            _trayIcon.ContextMenuStrip = _trayMenu;
            _trayIcon.Visible = true;
            _trayIcon.DoubleClick += delegate { ShowFormFromTray(); };
            _trayIcon.BalloonTipClicked += delegate
            {
                if (!_isServerRunning)
                {
                    btnToggle_Click(this, EventArgs.Empty);
                }
                else
                {
                    ShowFormFromTray();
                }
            };
        }

        private void PromptChangeDataDirectory()
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select your OmniRoute configuration & data directory (where storage.sqlite / .env is located):";
                dlg.SelectedPath = ServerManager.GetDataDirectory();
                dlg.ShowNewFolderButton = true;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    ServerManager.SetCustomDataDirectory(dlg.SelectedPath);
                    lblEndpoint.Text = string.Format("127.0.0.1 : {0}  •  Detecting...", ServerManager.ActivePort);
                    if (_trayIcon != null)
                    {
                        _trayIcon.ShowBalloonTip(3000, "OmniRoute Console", "Data directory updated: " + dlg.SelectedPath, ToolTipIcon.Info);
                    }
                }
            }
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
                    if (_trayIcon != null) _trayIcon.Icon = appIcon;
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
                            if (_trayIcon != null) _trayIcon.Icon = (Icon)ico.Clone();
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

            // Initial non-blocking health check with latency measurement
            ThreadPool.QueueUserWorkItem(delegate
            {
                long latency;
                bool healthy = ServerManager.MeasurePingLatency(ServerManager.ActivePort, out latency);
                SafeInvoke(delegate
                {
                    _currentLatencyMs = latency;
                    ApplyServerStatus(healthy, true, latency);
                });
            });

            // Start periodic latency and health polling timer (every 4s)
            StartMonitorTimer();

            // Check for background auto-updates
            CheckForUpdates();
        }

        private void StartMonitorTimer()
        {
            if (_monitorTimer == null)
            {
                _monitorTimer = new System.Windows.Forms.Timer();
                _monitorTimer.Interval = 4000;
                _monitorTimer.Tick += delegate
                {
                    if (_isBusy) return;

                    ThreadPool.QueueUserWorkItem(delegate
                    {
                        long latency;
                        bool healthy = ServerManager.MeasurePingLatency(ServerManager.ActivePort, out latency);
                        SafeInvoke(delegate
                        {
                            if (!_isBusy)
                            {
                                bool previouslyRunning = _isServerRunning;
                                _currentLatencyMs = latency;
                                ApplyServerStatus(healthy, false, latency);

                                // Watchdog crash detection: was running, now stopped without user requesting stop
                                if (previouslyRunning && !healthy && !_userRequestedStop && _watchdogEnabled)
                                {
                                    if (_trayIcon != null)
                                    {
                                        _trayIcon.ShowBalloonTip(4000, "OmniRoute Crash Guardian",
                                            "OmniRoute server stopped unexpectedly! Click here to restart.",
                                            ToolTipIcon.Warning);
                                    }
                                }
                            }
                        });
                    });
                };
            }
            _monitorTimer.Start();
        }

        private void CheckForUpdates()
        {
            ServerManager.CheckUpdateAsync("1.0.0", delegate(string newVersion)
            {
                SafeInvoke(delegate
                {
                    lblSubtitle.Text = string.Format("Update v{0} available • Click to view", newVersion);
                    lblSubtitle.ForeColor = UITheme.Brand;
                    lblSubtitle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
                    lblSubtitle.Cursor = Cursors.Hand;
                    lblSubtitle.Click += delegate
                    {
                        try { Process.Start(ServerManager.ReleasesUrl); } catch { }
                    };

                    if (_trayIcon != null)
                    {
                        _trayIcon.ShowBalloonTip(3000, "OmniRoute Console Update", string.Format("Version v{0} is available for download.", newVersion), ToolTipIcon.Info);
                    }
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

        private void ApplyServerStatus(bool isRunning, bool isInitial, long latency)
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

                if (latency >= 0)
                {
                    lblEndpoint.Text = string.Format("127.0.0.1 : {0}  •  {1}ms latency", ServerManager.ActivePort, latency);
                }
                else
                {
                    lblEndpoint.Text = string.Format("127.0.0.1 : {0}  •  Active", ServerManager.ActivePort);
                }

                btnToggle.Text = "Stop";
                btnToggle.BackColor = UITheme.RedBg;
                btnToggle.ForeColor = UITheme.RedText;
                btnToggle.FlatAppearance.BorderColor = UITheme.RedBorder;
                btnToggle.FlatAppearance.MouseOverBackColor = UITheme.RedHover;
                btnToggle.FlatAppearance.MouseDownBackColor = UITheme.RedBg;

                btnRestart.Enabled = true;

                if (_trayIcon != null)
                {
                    string trayText = latency >= 0
                        ? string.Format("OmniRoute: Online ({0}ms)", latency)
                        : string.Format("OmniRoute: Online ({0})", ServerManager.ActivePort);
                    if (trayText.Length > 63) trayText = trayText.Substring(0, 63);
                    _trayIcon.Text = trayText;
                }

                if (_trayMenuToggle != null) _trayMenuToggle.Text = "Stop Server";
                if (_trayMenuRestart != null) _trayMenuRestart.Enabled = true;

                if (isInitial && !_startMinimized)
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

                lblEndpoint.Text = string.Format("127.0.0.1 : {0}  •  Offline", ServerManager.ActivePort);

                btnToggle.Text = "Start";
                btnToggle.BackColor = UITheme.GreenBg;
                btnToggle.ForeColor = UITheme.GreenText;
                btnToggle.FlatAppearance.BorderColor = UITheme.GreenBorder;
                btnToggle.FlatAppearance.MouseOverBackColor = UITheme.GreenHover;
                btnToggle.FlatAppearance.MouseDownBackColor = UITheme.GreenBg;

                btnRestart.Enabled = false;

                if (_trayIcon != null)
                {
                    _trayIcon.Text = "OmniRoute: Offline";
                }

                if (_trayMenuToggle != null) _trayMenuToggle.Text = "Start Server";
                if (_trayMenuRestart != null) _trayMenuRestart.Enabled = false;

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
                        this.Hide(); // Hide to tray rather than killing
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

        private void LauncherForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                CancelAutoClose();
                this.Hide();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Space && btnToggle.Enabled)
            {
                btnToggle_Click(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter && btnDashboard.Enabled)
            {
                btnDashboard_Click(this, EventArgs.Empty);
                e.Handled = true;
            }
        }

        public void ShowFormFromTray()
        {
            _startMinimized = false;
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.BringToFront();
            this.Activate();
            CancelAutoClose();
        }

        public void ExitApplication()
        {
            _allowRealExit = true;
            if (_trayIcon != null)
            {
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _trayIcon = null;
            }
            Application.Exit();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!_allowRealExit && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                return;
            }
            base.OnFormClosing(e);
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
            _userRequestedStop = false;

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
                long latency = -1;
                for (int i = 0; i < 14; i++)
                {
                    Thread.Sleep(500);
                    if (ServerManager.MeasurePingLatency(ServerManager.ActivePort, out latency))
                    {
                        healthy = true;
                        break;
                    }
                }

                SafeInvoke(delegate
                {
                    StopStatusAnimation();
                    _isBusy = false;
                    _currentLatencyMs = latency;
                    ApplyServerStatus(healthy, false, latency);
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
                _userRequestedStop = true;
                StartStatusAnimation("STOPPING");

                ThreadPool.QueueUserWorkItem(delegate
                {
                    ServerManager.StopServerProcess();
                    Thread.Sleep(1000);
                    long latency;
                    bool healthy = ServerManager.MeasurePingLatency(ServerManager.ActivePort, out latency);

                    SafeInvoke(delegate
                    {
                        StopStatusAnimation();
                        _isBusy = false;
                        _currentLatencyMs = latency;
                        ApplyServerStatus(healthy, false, latency);
                    });
                });
            }
            else
            {
                _userRequestedStop = false;
                StartStatusAnimation("STARTING");

                ThreadPool.QueueUserWorkItem(delegate
                {
                    ServerManager.StartServerProcess();

                    bool healthy = false;
                    long latency = -1;
                    for (int i = 0; i < 12; i++)
                    {
                        Thread.Sleep(500);
                        if (ServerManager.MeasurePingLatency(ServerManager.ActivePort, out latency))
                        {
                            healthy = true;
                            break;
                        }
                    }

                    SafeInvoke(delegate
                    {
                        StopStatusAnimation();
                        _isBusy = false;
                        _currentLatencyMs = latency;
                        ApplyServerStatus(healthy, false, latency);
                    });
                });
            }
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            CancelAutoClose();
            this.Hide(); // Minimize to system tray
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
                if (_monitorTimer != null)
                {
                    _monitorTimer.Dispose();
                    _monitorTimer = null;
                }
                if (_trayIcon != null)
                {
                    _trayIcon.Visible = false;
                    _trayIcon.Dispose();
                    _trayIcon = null;
                }
                if (_trayMenu != null)
                {
                    _trayMenu.Dispose();
                    _trayMenu = null;
                }
            }
            base.Dispose(disposing);
        }
    }
}
