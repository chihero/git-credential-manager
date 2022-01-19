using System;
using System.IO;
using GitCredentialManager.Interop.Posix;

namespace GitCredentialManager.Interop.Linux
{
    public class LinuxFileSystem : PosixFileSystem
    {
        public override bool IsSamePath(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
                return false;

            a = Path.GetFullPath(a);
            b = Path.GetFullPath(b);

            return StringComparer.Ordinal.Equals(a, b);
        }
    }
}
