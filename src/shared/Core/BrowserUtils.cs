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

            ProcessStartInfo psi = null;
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
                // One additional 'shell execute' utility we also attempt to use is `wslview`
                // that is commonly found on WSL (Windows Subsystem for Linux) distributions that
                // opens the browser on the Windows host.
                foreach (string shellExec in new[] {"xdg-open", "gnome-open", "kfmclient", "wslview"})
                {
                    if (environment.TryLocateExecutable(shellExec, out string shellExecPath))
                    {
                        psi = new ProcessStartInfo(shellExecPath, url)
                        {
                            RedirectStandardOutput = true,
                            RedirectStandardError = true
                        };

                        // We found a way to open the URI; stop searching!
                        break;
                    }
                }

                if (psi is null)
                {
                    throw new Exception("Failed to locate a utility to launch the default web browser.");
                }
            }
            else
            {
                // On Windows and macOS, `ShellExecute` and `/usr/bin/open` disconnect the child process
                // from our standard in/out streams, so we can just use the Framework to do this.
                psi = new ProcessStartInfo(url) {UseShellExecute = true};
            }

            Process.Start(psi);
        }
    }
}
