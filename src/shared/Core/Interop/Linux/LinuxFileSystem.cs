using System;
using System.IO;
using System.Runtime.Versioning;
using GitCredentialManager.Interop.Posix;

namespace GitCredentialManager.Interop.Linux
{
    [SupportedOSPlatform("linux")]
    public class LinuxFileSystem : PosixFileSystem
    {
        public override bool IsSamePath(string a, string b)
        {
            a = Path.GetFileName(a);
            b = Path.GetFileName(b);

            return StringComparer.Ordinal.Equals(a, b);
        }
    }
}
