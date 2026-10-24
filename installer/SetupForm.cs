using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace OmniRouteInstaller
{
    public class SetupForm : Form
    {
        [DllImport("shell32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);
        private const int SHCNE_ASSOCCHANGED = 0x08000000;
        private const int SHCNF_FLUSH = 0x1000;

        private PictureBox picIcon;
        private Label lblTitle;
        private Label lblSubtitle;
        private Panel pnlTopDivider;

        private Label lblDest;
        private TextBox txtDest;
        private Button btnBrowse;

        private CheckBox chkDesktop;
        private CheckBox chkStartMenu;
        private CheckBox chkLaunch;

        private ProgressBar progressBar;
        private Label lblStatus;
        private Panel pnlBottomDivider;

        private Button btnCancel;
        private Button btnInstall;

        private bool isFinished = false;

        private static readonly Color Ink = Color.FromArgb(28, 35, 52);
        private static readonly Color Muted = Color.FromArgb(105, 115, 135);
        private static readonly Color Brand = Color.FromArgb(82, 73, 218);
        private static readonly Color Border = Color.FromArgb(225, 229, 238);

        public SetupForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.Text = "OmniRoute Console Setup";
            this.ClientSize = new Size(520, 380);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.TopMost = true;
            this.BackColor = Color.FromArgb(247, 248, 252);
            this.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            this.AutoScaleMode = AutoScaleMode.Font;

            try
            {
                this.Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }

            picIcon = new PictureBox();
            picIcon.Location = new Point(24, 20);
            picIcon.Size = new Size(48, 48);
            picIcon.SizeMode = PictureBoxSizeMode.Zoom;
            if (this.Icon != null) picIcon.Image = this.Icon.ToBitmap();

            lblTitle = new Label();
            lblTitle.Text = "Install OmniRoute Console";
            lblTitle.Font = new Font("Segoe UI", 13.5F, FontStyle.Bold);
            lblTitle.ForeColor = Ink;
            lblTitle.Location = new Point(84, 18);
            lblTitle.AutoSize = true;

            lblSubtitle = new Label();
            lblSubtitle.Text = "Lightweight companion & system tray manager for OmniRoute";
            lblSubtitle.Font = new Font("Segoe UI", 9F, FontStyle.Regular);
            lblSubtitle.ForeColor = Muted;
            lblSubtitle.Location = new Point(85, 46);
            lblSubtitle.AutoSize = true;

            pnlTopDivider = new Panel();
            pnlTopDivider.Location = new Point(20, 78);
            pnlTopDivider.Size = new Size(480, 1);
            pnlTopDivider.BackColor = Border;

            lblDest = new Label();
            lblDest.Text = "Destination Folder:";
            lblDest.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            lblDest.ForeColor = Ink;
            lblDest.Location = new Point(24, 95);
            lblDest.AutoSize = true;

            string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OmniRouteConsole");
            txtDest = new TextBox();
            txtDest.Text = defaultPath;
            txtDest.Location = new Point(24, 118);
            txtDest.Size = new Size(390, 25);
            txtDest.Font = new Font("Segoe UI", 9F, FontStyle.Regular);

            btnBrowse = new Button();
            btnBrowse.Text = "Browse...";
            btnBrowse.Location = new Point(420, 116);
            btnBrowse.Size = new Size(76, 28);
            btnBrowse.FlatStyle = FlatStyle.Flat;
            btnBrowse.FlatAppearance.BorderColor = Border;
            btnBrowse.Cursor = Cursors.Hand;
            btnBrowse.Click += BtnBrowse_Click;

            chkDesktop = new CheckBox();
            chkDesktop.Text = "Create Desktop shortcut (OmniRoute Console.lnk)";
            chkDesktop.Checked = true;
            chkDesktop.Location = new Point(26, 160);
            chkDesktop.AutoSize = true;
            chkDesktop.ForeColor = Ink;

            chkStartMenu = new CheckBox();
            chkStartMenu.Text = "Create Start Menu shortcut";
            chkStartMenu.Checked = true;
            chkStartMenu.Location = new Point(26, 188);
            chkStartMenu.AutoSize = true;
            chkStartMenu.ForeColor = Ink;

            chkLaunch = new CheckBox();
            chkLaunch.Text = "Launch OmniRoute Console after installation";
            chkLaunch.Checked = true;
            chkLaunch.Location = new Point(26, 216);
            chkLaunch.AutoSize = true;
            chkLaunch.ForeColor = Ink;

            progressBar = new ProgressBar();
            progressBar.Location = new Point(24, 256);
            progressBar.Size = new Size(472, 14);
            progressBar.Style = ProgressBarStyle.Continuous;
            progressBar.Visible = false;

            lblStatus = new Label();
            lblStatus.Text = "Ready to install.";
            lblStatus.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular);
            lblStatus.ForeColor = Muted;
            lblStatus.Location = new Point(24, 256);
            lblStatus.Size = new Size(472, 20);

            pnlBottomDivider = new Panel();
            pnlBottomDivider.Location = new Point(20, 290);
            pnlBottomDivider.Size = new Size(480, 1);
            pnlBottomDivider.BackColor = Border;

            btnCancel = new Button();
            btnCancel.Text = "Cancel";
            btnCancel.Location = new Point(290, 310);
            btnCancel.Size = new Size(95, 36);
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderColor = Border;
            btnCancel.ForeColor = Muted;
            btnCancel.Cursor = Cursors.Hand;
            btnCancel.Click += delegate { this.Close(); };

            btnInstall = new Button();
            btnInstall.Text = "Install";
            btnInstall.Location = new Point(395, 310);
            btnInstall.Size = new Size(105, 36);
            btnInstall.BackColor = Brand;
            btnInstall.ForeColor = Color.White;
            btnInstall.FlatStyle = FlatStyle.Flat;
            btnInstall.FlatAppearance.BorderSize = 0;
            btnInstall.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            btnInstall.Cursor = Cursors.Hand;
            btnInstall.Click += BtnInstall_Click;

            this.Controls.Add(picIcon);
            this.Controls.Add(lblTitle);
            this.Controls.Add(lblSubtitle);
            this.Controls.Add(pnlTopDivider);
            this.Controls.Add(lblDest);
            this.Controls.Add(txtDest);
            this.Controls.Add(btnBrowse);
            this.Controls.Add(chkDesktop);
            this.Controls.Add(chkStartMenu);
            this.Controls.Add(chkLaunch);
            this.Controls.Add(progressBar);
            this.Controls.Add(lblStatus);
            this.Controls.Add(pnlBottomDivider);
            this.Controls.Add(btnCancel);
            this.Controls.Add(btnInstall);
        }

        private void BtnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select Installation Directory";
                fbd.SelectedPath = txtDest.Text;
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtDest.Text = fbd.SelectedPath;
                }
            }
        }

        private void BtnInstall_Click(object sender, EventArgs e)
        {
            if (isFinished)
            {
                if (chkLaunch.Checked)
                {
                    string targetExe = Path.Combine(txtDest.Text, "OmniRouteConsole.exe");
                    if (File.Exists(targetExe))
                    {
                        try { Process.Start(targetExe); } catch { }
                    }
                }
                this.Close();
                return;
            }

            string targetDir = txtDest.Text.Trim();
            if (string.IsNullOrEmpty(targetDir))
            {
                MessageBox.Show("Please select a valid destination directory.", "OmniRoute Setup", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnBrowse.Enabled = false;
            txtDest.ReadOnly = true;
            chkDesktop.Enabled = false;
            chkStartMenu.Enabled = false;
            chkLaunch.Enabled = false;
            btnCancel.Enabled = false;
            btnInstall.Enabled = false;
            btnInstall.Text = "Installing...";

            bool createDesktop = chkDesktop.Checked;
            bool createStartMenu = chkStartMenu.Checked;

            lblStatus.Visible = false;
            progressBar.Visible = true;
            progressBar.Value = 10;

            Thread worker = new Thread(delegate ()
            {
                try
                {
                    // Close running instances if updating
                    try
                    {
                        Process[] procs = Process.GetProcessesByName("OmniRouteConsole");
                        for (int i = 0; i < procs.Length; i++)
                        {
                            try
                            {
                                procs[i].CloseMainWindow();
                                if (!procs[i].WaitForExit(400)) { procs[i].Kill(); }
                            }
                            catch { }
                            finally { procs[i].Dispose(); }
                        }
                    }
                    catch { }

                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    UpdateProgress(30, "Extracting OmniRouteConsole.exe...");
                    string exePath = Path.Combine(targetDir, "OmniRouteConsole.exe");
                    ExtractResource("OmniRouteConsole.exe", exePath);

                    UpdateProgress(60, "Extracting icon assets...");
                    string icoPath = Path.Combine(targetDir, "omniroute.ico");
                    ExtractResource("omniroute.ico", icoPath);

                    UpdateProgress(80, "Creating shortcuts...");
                    if (createDesktop)
                    {
                        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                        string shortcutFile = Path.Combine(desktopPath, "OmniRoute Console.lnk");
                        CreateShortcut(shortcutFile, exePath, targetDir, icoPath);
                    }

                    if (createStartMenu)
                    {
                        string programsPath = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                        string shortcutFile = Path.Combine(programsPath, "OmniRoute Console.lnk");
                        CreateShortcut(shortcutFile, exePath, targetDir, icoPath);
                    }

                    UpdateProgress(95, "Refreshing Windows Explorer icon cache...");
                    try
                    {
                        SHChangeNotify(SHCNE_ASSOCCHANGED, 0, IntPtr.Zero, IntPtr.Zero);
                    }
                    catch { }

                    this.Invoke((MethodInvoker)delegate
                    {
                        progressBar.Value = 100;
                        progressBar.Visible = false;
                        lblStatus.Visible = true;
                        lblStatus.Text = "Installation completed successfully!";
                        lblStatus.ForeColor = Color.FromArgb(28, 125, 70);
                        lblStatus.Font = new Font("Segoe UI", 9F, FontStyle.Bold);

                        btnCancel.Visible = false;
                        btnInstall.Enabled = true;
                        btnInstall.Text = "Finish";
                        isFinished = true;
                    });
                }
                catch (Exception ex)
                {
                    this.Invoke((MethodInvoker)delegate
                    {
                        progressBar.Visible = false;
                        lblStatus.Visible = true;
                        lblStatus.Text = "Installation error: " + ex.Message;
                        lblStatus.ForeColor = Color.FromArgb(181, 55, 55);
                        btnInstall.Enabled = true;
                        btnInstall.Text = "Retry";
                        btnCancel.Enabled = true;
                    });
                }
            });
            worker.IsBackground = true;
            worker.Start();
        }

        private void UpdateProgress(int val, string text)
        {
            this.Invoke((MethodInvoker)delegate
            {
                progressBar.Value = val;
                lblStatus.Text = text;
            });
            Thread.Sleep(200);
        }

        private void ExtractResource(string resourceName, string targetFilePath)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            Stream s = asm.GetManifestResourceStream(resourceName);

            if (s == null)
            {
                foreach (string name in asm.GetManifestResourceNames())
                {
                    if (name.EndsWith(resourceName, StringComparison.OrdinalIgnoreCase))
                    {
                        s = asm.GetManifestResourceStream(name);
                        break;
                    }
                }
            }

            if (s != null)
            {
                using (s)
                using (FileStream fs = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write))
                {
                    byte[] buffer = new byte[8192];
                    int read;
                    while ((read = s.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        fs.Write(buffer, 0, read);
                    }
                }
            }
            else
            {
                string[] fallbacks = new string[]
                {
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, resourceName),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin", resourceName),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", resourceName),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "bin", resourceName),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "assets", resourceName),
                    Path.Combine(@"C:\Users\Admin\Desktop\omni console\bin", resourceName),
                    Path.Combine(@"C:\Users\Admin\Desktop\omni console\assets", resourceName)
                };

                for (int i = 0; i < fallbacks.Length; i++)
                {
                    if (File.Exists(fallbacks[i]))
                    {
                        File.Copy(fallbacks[i], targetFilePath, true);
                        return;
                    }
                }
            }
        }

        private void CreateShortcut(string shortcutPath, string targetPath, string workingDir, string iconPath)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;
                dynamic shell = Activator.CreateInstance(shellType);
                dynamic shortcut = shell.CreateShortcut(shortcutPath);
                shortcut.TargetPath = targetPath;
                shortcut.WorkingDirectory = workingDir;
                shortcut.IconLocation = iconPath + ",0";
                shortcut.Description = "OmniRoute Console - Native Windows Companion & Tray Controller";
                shortcut.Save();
            }
            catch { }
        }
    }
}
