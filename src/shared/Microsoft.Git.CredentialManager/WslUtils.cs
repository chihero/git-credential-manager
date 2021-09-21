using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Git.CredentialManager
{
    public static class WslUtils
    {
        private const string WslUncPrefix = @"\\wsl$\";
        private const string WslCommandName = "wsl.exe";

        /// <summary>
        /// Test if a file path points to a location in a Windows Subsystem for Linux distribution.
        /// </summary>
        /// <param name="path">Path to test.</param>
        /// <returns>True if <paramref name="path"/> is a WSL path, false otherwise.</returns>
        public static bool IsWslPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;

            return path.StartsWith(WslUncPrefix, StringComparison.OrdinalIgnoreCase) &&
                   path.Length > WslUncPrefix.Length;
        }

        /// <summary>
        /// Create a command to be executed in a Windows Subsystem for Linux distribution.
        /// </summary>
        /// <param name="distribution">WSL distribution name.</param>
        /// <param name="command">Command to execute.</param>
        /// <param name="workingDirectory">Optional working directory.</param>
        /// <returns><see cref="Process"/> object ready to start.</returns>
        public static Process CreateWslProcess(string distribution, string command, string workingDirectory = null)
        {
            var args = new StringBuilder();
            args.AppendFormat("--distribution {0} ", distribution);
            args.AppendFormat("--exec {0}", command);

            string wslExePath = GetWslPath();

            var psi = new ProcessStartInfo(wslExePath, args.ToString())
            {
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                WorkingDirectory = workingDirectory ?? string.Empty
            };

            return new Process { StartInfo = psi };
        }

        public static string ConvertToDistroPath(string path, out string distribution)
        {
            if (!IsWslPath(path)) throw new ArgumentException("Must provide a WSL path", nameof(path));

            int distroStart = WslUncPrefix.Length;
            int distroEnd = path.IndexOf('\\', distroStart);

            if (distroEnd < 0) distroEnd = path.Length;

            distribution = path.Substring(distroStart, distroEnd - distroStart);

            if (path.Length > distroEnd)
            {
                return path.Substring(distroEnd).Replace('\\', '/');
            }

            return "/";
        }

        public static IEnumerable<WslDistribution> GetInstalledDistributions()
        {
            string wslExePath = GetWslPath();

            var psi = new ProcessStartInfo(wslExePath, "--list --verbose")
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
            };

            using (var wsl = new Process { StartInfo = psi })
            {
                wsl.Start();

                string output = wsl.StandardOutput.ReadToEnd();
                wsl.WaitForExit();

                // The first line is the table header
                string[] lines = output.Split('\r', '\n').Skip(1).Select(x => x.Trim()).ToArray();

                var lineRegex = new Regex(
                    @"^(?'default'\*)?\s+(?'name'\S+)\s+(?'state'\S+)\s+(?'wslVer'\d+)",
                    RegexOptions.Compiled);

                foreach (string line in lines)
                {
                    Match match = lineRegex.Match(line);
                    if (match.Success)
                    {
                        string name = match.Groups["name"].Value;
                        int wslVersion = int.Parse(match.Groups["wslVer"].Value);
                        bool isDefault = match.Groups["default"].Success;
                        string state = match.Groups["state"].Value;
                        bool isRunning = StringComparer.OrdinalIgnoreCase.Equals(state, "RUNNING");

                        yield return new WslDistribution(name, wslVersion, isDefault, isRunning);
                    }
                }
            }
        }

        internal /*for testing purposes*/ static string GetWslPath()
        {
            // WSL is only supported on 64-bit operating systems
            if (!Environment.Is64BitOperatingSystem)
            {
                throw new Exception("WSL is not supported on 32-bit operating systems");
            }

            //
            // When running as a 32-bit application on a 64-bit operating system, we cannot access the real
            // C:\Windows\System32 directory because the OS will redirect us transparently to the
            // C:\Windows\SysWOW64 directory (containing 32-bit executables).
            //
            // In order to access the real 64-bit System32 directory, we must access via the pseudo directory
            // C:\Windows\SysNative that does **not** experience any redirection for 32-bit applications.
            //
            // HOWEVER, if we're running as a 64-bit application on a 64-bit operating system, the SysNative
            // directory does not exist! This means if running as a 32-bit application on a 64-bit OS we must
            // use the System32 directory name directly.
            //
            var sysDir = Environment.ExpandEnvironmentVariables(
                Environment.Is64BitProcess
                    ? @"%WINDIR%\System32"
                    : @"%WINDIR%\SysNative"
            );

            return Path.Combine(sysDir, WslCommandName);
        }
    }

    public class WslDistribution
    {
        public WslDistribution(string name, int wslVersion, bool isDefault, bool isRunning)
        {
            Name = name;
            WslVersion = wslVersion;
            IsDefault = isDefault;
            IsRunning = isRunning;
        }

        public string Name { get; }
        public int WslVersion { get; }
        public bool IsDefault { get; }
        public bool IsRunning { get; }
    }
}
