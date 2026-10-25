using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace OmniRouteLauncher
{
    public static class ServerManager
    {
        public const int DefaultPort = 20128;
        public const string DefaultDashboardUrl = "http://localhost:20128/home";
        public const string VersionCheckUrl = "https://raw.githubusercontent.com/krishna-techhub/OmniRoute-Console/main/version.txt";
        public const string ReleasesUrl = "https://github.com/krishna-techhub/OmniRoute-Console/releases";

        public static bool CheckServerHealthy()
        {
            return CheckServerHealthy(DefaultPort);
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
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string npmDir = Path.Combine(appData, "npm");
                string nodeDir = @"C:\Program Files\nodejs";
                string omnirouteCmd = Path.Combine(npmDir, "omniroute.cmd");

                // Check if %APPDATA%\npm\omniroute.cmd exists
                bool cmdExists = File.Exists(omnirouteCmd);
                Trace.WriteLine("OmniRoute CMD exists at " + omnirouteCmd + ": " + cmdExists);

                string enrichedPath = GetEnrichedPath(npmDir, nodeDir);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c omniroute serve --tray",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                psi.EnvironmentVariables["PATH"] = enrichedPath;
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
                string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string npmDir = Path.Combine(appData, "npm");
                string nodeDir = @"C:\Program Files\nodejs";

                string enrichedPath = GetEnrichedPath(npmDir, nodeDir);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = "/c omniroute stop",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    WindowStyle = ProcessWindowStyle.Hidden
                };

                psi.EnvironmentVariables["PATH"] = enrichedPath;

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
                    FileName = DefaultDashboardUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Failed to open dashboard: " + ex.Message);
            }
        }

        private static string GetEnrichedPath(string npmDir, string nodeDir)
        {
            string currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
            string enriched = currentPath;

            if (!string.IsNullOrEmpty(npmDir) && !enriched.Contains(npmDir))
            {
                enriched = npmDir + ";" + enriched;
            }

            if (!string.IsNullOrEmpty(nodeDir) && !enriched.Contains(nodeDir))
            {
                enriched = nodeDir + ";" + enriched;
            }

            return enriched;
        }
    }
}
