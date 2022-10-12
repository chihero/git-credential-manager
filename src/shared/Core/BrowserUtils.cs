using System;
using System.Diagnostics;

namespace GitCredentialManager
{
    public static class BrowserUtils
    {
        private static bool? _isWebBrowserAvailable;

        public static bool IsWebBrowserAvailable(ICommandContext context) =>
            IsWebBrowserAvailable(context.Environment, context.FileSystem, context.SessionManager);

        public static bool IsWebBrowserAvailable(IEnvironment env, IFileSystem fs, ISessionManager sm) =>
            _isWebBrowserAvailable ??= GetWebBrowserAvailable(env, fs, sm);

        private static bool GetWebBrowserAvailable(IEnvironment env, IFileSystem fs, ISessionManager sm)
        {
            // If this is a Windows Subsystem for Linux distribution we may
            // be able to launch the web browser of the host Windows OS.
            if (PlatformUtils.IsLinux() && WslUtils.IsWslDistribution(env, fs, out _))
            {
                // We need a shell execute handler to be able to launch to browser
                if (!TryGetLinuxShellExecuteHandler(env, out _))
                {
                    return false;
                }

                //
                // If we are in Windows logon session 0 then the user can never interact,
                // even in the WinSta0 window station. This is typical when SSH-ing into a
                // Windows 10+ machine using the default OpenSSH Server configuration,
                // which runs in the 'services' session 0.
                //
                // If we're in any other session, and in the WinSta0 window station then
                // the user can possibly interact. However, since it's hard to determine
                // the window station from PowerShell cmdlets (we'd need to write P/Invoke
                // code and that's just messy and too many levels of indirection quite
                // frankly!) we just assume any non session 0 is interactive.
                //
                // This assumption doesn't hold true if the user has changed the user that
                // the OpenSSH Server service runs as (not a built-in NT service) *AND*
                // they've SSH-ed into the Windows host (and then started a WSL shell).
                // This feels like a very small subset of users...
                //
                if (WslUtils.GetWindowsSessionId(fs) == 0)
                {
                    return false;
                }

                // If we are not in session 0, or we cannot get the Windows session ID,
                // assume that we *CAN* launch the browser so that users are never blocked.
                return true;
            }

            // We require an interactive desktop session to be able to launch a browser
            return sm.IsDesktopSession;
        }

        public static void OpenDefaultBrowser(IEnvironment environment, string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
            {
                throw new ArgumentException($"Not a valid URI: '{url}'");
            }

            OpenDefaultBrowser(environment, uri);
        }

        public static void OpenDefaultBrowser(IEnvironment environment, Uri uri)
        {
            if (!uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
                !uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException("Can only open HTTP/HTTPS URIs", nameof(uri));
            }

            string url = uri.ToString();

            ProcessStartInfo psi;
            if (PlatformUtils.IsLinux())
            {
                //
                // On Linux, 'shell execute' utilities like xdg-open launch a process without
                // detaching from the standard in/out descriptors. Some applications (like
                // Chromium) write messages to stdout, which is currently hooked up and being
                // consumed by Git, and cause errors.
                //
                // Sadly, the Framework does not allow us to redirect standard streams if we
                // set ProcessStartInfo::UseShellExecute = true, so we must manually launch
                // these utilities and redirect the standard streams manually.
                //
                // We try and use the same 'shell execute' utilities as the Framework does,
                // searching for them in the same order until we find one.
                //
                if (!TryGetLinuxShellExecuteHandler(environment, out string shellExecPath))
                {
                    throw new Exception("Failed to locate a utility to launch the default web browser.");
                }

                psi = new ProcessStartInfo(shellExecPath, url)
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };
            }
            else
            {
                // On Windows and macOS, `ShellExecute` and `/usr/bin/open` disconnect the child process
                // from our standard in/out streams, so we can just use the Framework to do this.
                psi = new ProcessStartInfo(url) {UseShellExecute = true};
            }

            Process.Start(psi);
        }

        private static bool TryGetLinuxShellExecuteHandler(IEnvironment env, out string shellExecPath)
        {
            // One additional 'shell execute' utility we also attempt to use over the Framework
            // is `wslview` that is commonly found on WSL (Windows Subsystem for Linux) distributions
            // that opens the browser on the Windows host.
            string[] shellHandlers = { "xdg-open", "gnome-open", "kfmclient", WslUtils.WslViewShellHandlerName };
            foreach (string shellExec in shellHandlers)
            {
                if (env.TryLocateExecutable(shellExec, out shellExecPath))
                {
                    return true;
                }
            }

            shellExecPath = null;
            return false;
        }
    }
}
