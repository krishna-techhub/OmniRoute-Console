using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace OmniRouteLauncher
{
    public static class ServerManager
    {
        public const int DefaultPort = 20128;
        public const string DefaultDashboardUrl = "http://localhost:20128/home";
        public const string VersionCheckUrl = "https://raw.githubusercontent.com/krishna-techhub/OmniRoute-Console/main/version.txt";
        public const string ReleasesUrl = "https://github.com/krishna-techhub/OmniRoute-Console/releases";

        private const string RunRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string RunAppName = "OmniRouteConsole";
        private const string AppSettingsRegistryKey = @"Software\OmniRouteConsole";
        private const string DataDirSettingName = "DataDir";

        public static int ActivePort = DefaultPort;
        public static string ActiveDashboardUrl = DefaultDashboardUrl;
        public static string CustomDataDir = null;

        static ServerManager()
        {
            LoadCustomDataDir();
            DetectPort();
        }

        public static void LoadCustomDataDir()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(AppSettingsRegistryKey, false))
                {
                    if (key != null)
                    {
                        object val = key.GetValue(DataDirSettingName);
                        if (val != null && Directory.Exists(val.ToString()))
                        {
                            CustomDataDir = val.ToString();
                        }
                    }
                }
            }
            catch { }
        }

        public static void SetCustomDataDirectory(string path)
        {
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path)) return;
            CustomDataDir = path;
            try
            {
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(AppSettingsRegistryKey))
                {
                    if (key != null)
                    {
                        key.SetValue(DataDirSettingName, path);
                    }
                }
            }
            catch { }
            DetectPort();
        }

        public static string GetDataDirectory()
        {
            // 1. Explicitly configured custom directory
            if (!string.IsNullOrEmpty(CustomDataDir) && Directory.Exists(CustomDataDir))
            {
                return CustomDataDir;
            }

            // 2. Environment variables (OMNIROUTE_HOME, OMNIROUTE_DIR, OMNIROUTE_DATA)
            string[] envKeys = new string[] { "OMNIROUTE_HOME", "OMNIROUTE_DIR", "OMNIROUTE_DATA", "OMNIROUTE_PATH" };
            foreach (string envKey in envKeys)
            {
                string val = Environment.GetEnvironmentVariable(envKey);
                if (!string.IsNullOrEmpty(val) && Directory.Exists(val))
                {
                    return val;
                }
            }

            // 3. Standard user home directory %USERPROFILE%\.omniroute
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string userDir = Path.Combine(userProfile, ".omniroute");
            if (Directory.Exists(userDir)) return userDir;

            // 4. %APPDATA%\.omniroute
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string appDataDir = Path.Combine(appData, ".omniroute");
            if (Directory.Exists(appDataDir)) return appDataDir;

            // 5. %LOCALAPPDATA%\.omniroute
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string localDir = Path.Combine(localAppData, ".omniroute");
            if (Directory.Exists(localDir)) return localDir;

            return userDir;
        }

        public static string GetLogsDirectory()
        {
            string dataDir = GetDataDirectory();
            string logsDir = Path.Combine(dataDir, "logs");
            if (Directory.Exists(logsDir)) return logsDir;
            return dataDir;
        }

        public static void OverridePort(int port)
        {
            if (port > 0 && port <= 65535)
            {
                ActivePort = port;
                ActiveDashboardUrl = string.Format("http://localhost:{0}/home", port);
            }
        }

        public static void DetectPort()
        {
            try
            {
                // 1. Check environment variable
                string envPortStr = Environment.GetEnvironmentVariable("PORT") ?? Environment.GetEnvironmentVariable("OMNIROUTE_PORT");
                int parsed;
                if (!string.IsNullOrEmpty(envPortStr) && int.TryParse(envPortStr.Trim(), out parsed))
                {
                    OverridePort(parsed);
                    return;
                }

                // 2. Check detected data directory for .env
                string dataDir = GetDataDirectory();
                string envFile = Path.Combine(dataDir, ".env");
                if (File.Exists(envFile))
                {
                    string[] lines = File.ReadAllLines(envFile);
                    foreach (string line in lines)
                    {
                        string trimmed = line.Trim();
                        if (trimmed.StartsWith("#") || !trimmed.Contains("=")) continue;
                        string[] parts = trimmed.Split(new char[] { '=' }, 2);
                        string key = parts[0].Trim();
                        string val = parts[1].Trim().Trim(new char[] { '"', '\'' });
                        if ((string.Equals(key, "PORT", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(key, "OMNIROUTE_PORT", StringComparison.OrdinalIgnoreCase)) &&
                            int.TryParse(val, out parsed))
                        {
                            OverridePort(parsed);
                            return;
                        }
                    }
                }
            }
            catch
            {
                // Fallback to default port
            }
        }

        public static bool CheckServerHealthy()
        {
            return CheckServerHealthy(ActivePort);
        }

        public static bool CheckServerHealthy(int port)
        {
            long latency;
            return MeasurePingLatency(port, out latency);
        }

        public static bool MeasurePingLatency(int port, out long latencyMs)
        {
            latencyMs = -1;
            Stopwatch sw = Stopwatch.StartNew();
            try
            {
                using (TcpClient client = new TcpClient())
                {
                    IAsyncResult result = client.BeginConnect("127.0.0.1", port, null, null);
                    bool success = result.AsyncWaitHandle.WaitOne(350, false);
                    if (!success)
                    {
                        client.Close();
                        return false;
                    }

                    client.EndConnect(result);
                    sw.Stop();
                    if (client.Connected)
                    {
                        latencyMs = sw.ElapsedMilliseconds;
                        return true;
                    }
                    return false;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void CheckUpdateAsync(string currentVersion, Action<string> onUpdateFound)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(VersionCheckUrl);
                    request.Timeout = 3000;
                    request.ReadWriteTimeout = 3000;
                    request.UserAgent = "OmniRouteConsole/" + currentVersion;

                    using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                            {
                                string remoteVer = reader.ReadToEnd().Trim();
                                if (!string.IsNullOrEmpty(remoteVer) && !string.Equals(remoteVer, currentVersion, StringComparison.OrdinalIgnoreCase))
                                {
                                    if (onUpdateFound != null)
                                    {
                                        onUpdateFound(remoteVer);
                                    }
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Fail silently on network errors
                }
            });
        }

        public static void StartServerProcess()
        {
            try
            {
                string enrichedPath = GetEnrichedPath();

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c omniroute serve --tray",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                psi.EnvironmentVariables["PATH"] = enrichedPath;

                // Propagate custom data directory if specified
                if (!string.IsNullOrEmpty(CustomDataDir))
                {
                    psi.EnvironmentVariables["OMNIROUTE_HOME"] = CustomDataDir;
                }

                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Failed to start server process: " + ex.Message);
            }
        }

        public static void StopServerProcess()
        {
            try
            {
                string enrichedPath = GetEnrichedPath();

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c omniroute stop",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                psi.EnvironmentVariables["PATH"] = enrichedPath;

                if (!string.IsNullOrEmpty(CustomDataDir))
                {
                    psi.EnvironmentVariables["OMNIROUTE_HOME"] = CustomDataDir;
                }

                Process proc = Process.Start(psi);
                if (proc != null)
                {
                    proc.WaitForExit(4000);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Failed to stop server process: " + ex.Message);
            }
        }

        public static void OpenDashboard()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = ActiveDashboardUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Failed to open dashboard: " + ex.Message);
            }
        }

        public static void OpenLogsDirectory()
        {
            try
            {
                string logsDir = GetLogsDirectory();
                if (Directory.Exists(logsDir))
                {
                    Process.Start("explorer.exe", logsDir);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Failed to open logs: " + ex.Message);
            }
        }

        public static void OpenDataDirectory()
        {
            try
            {
                string dataDir = GetDataDirectory();
                if (Directory.Exists(dataDir))
                {
                    Process.Start("explorer.exe", dataDir);
                }
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Failed to open data directory: " + ex.Message);
            }
        }

        public static bool IsAutoStartEnabled()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, false))
                {
                    if (key == null) return false;
                    object val = key.GetValue(RunAppName);
                    return val != null;
                }
            }
            catch
            {
                return false;
            }
        }

        public static void SetAutoStart(bool enable)
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RunRegistryKey, true))
                {
                    if (key == null) return;
                    if (enable)
                    {
                        string exePath = string.Format("\"{0}\" --minimized", Application.ExecutablePath);
                        key.SetValue(RunAppName, exePath);
                    }
                    else
                    {
                        key.DeleteValue(RunAppName, false);
                    }
                }
            }
            catch
            {
            }
        }

        private static string GetEnrichedPath()
        {
            string currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string[] searchDirs = new string[]
            {
                Path.Combine(appData, "npm"),
                Path.Combine(localAppData, "pnpm"),
                Path.Combine(localAppData, "Yarn", "bin"),
                @"C:\Program Files\nodejs",
                @"C:\Program Files (x86)\nodejs",
                Environment.GetEnvironmentVariable("NVM_SYMLINK"),
                Environment.GetEnvironmentVariable("NVM_HOME"),
                Path.Combine(localAppData, "fnm_multishells"),
                Path.Combine(localAppData, "Volta", "bin")
            };

            string enriched = currentPath;
            foreach (string dir in searchDirs)
            {
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir) && !enriched.Contains(dir))
                {
                    enriched = dir + ";" + enriched;
                }
            }

            return enriched;
        }
    }
}
