using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace OmniRouteLauncher
{
    internal static class Program
    {
        private const string MutexName = "OmniRouteConsole_SingleInstance_Mutex";
        private const int HWND_BROADCAST = 0xffff;
        public static readonly int WM_SHOWME = RegisterWindowMessage("WM_SHOW_OMNIROUTE_CONSOLE");

        [DllImport("user32.dll")]
        private static extern bool PostMessage(IntPtr hwnd, int msg, IntPtr wparam, IntPtr lparam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int RegisterWindowMessage(string message);

        [STAThread]
        private static void Main(string[] args)
        {
            bool createdNew;
            using (Mutex mutex = new Mutex(true, MutexName, out createdNew))
            {
                if (!createdNew)
                {
                    // Existing instance is running: bring it to foreground and exit this instance
                    PostMessage((IntPtr)HWND_BROADCAST, WM_SHOWME, IntPtr.Zero, IntPtr.Zero);
                    return;
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                bool startMinimized = false;
                for (int i = 0; i < args.Length; i++)
                {
                    if (string.Equals(args[i], "--minimized", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(args[i], "--silent", StringComparison.OrdinalIgnoreCase))
                    {
                        startMinimized = true;
                    }
                    else if (string.Equals(args[i], "--port", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                    {
                        int customPort;
                        if (int.TryParse(args[i + 1], out customPort))
                        {
                            ServerManager.OverridePort(customPort);
                        }
                    }
                }

                Application.Run(new LauncherForm(startMinimized));
            }
        }
    }
}
