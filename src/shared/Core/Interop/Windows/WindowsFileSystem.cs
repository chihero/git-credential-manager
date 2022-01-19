using System;
using System.IO;

namespace GitCredentialManager.Interop.Windows
{
    public class WindowsFileSystem : FileSystem
    {
        public override bool IsSamePath(string a, string b)
        {
            if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
                return false;

            a = Path.GetFullPath(a);
            b = Path.GetFullPath(b);

            return StringComparer.OrdinalIgnoreCase.Equals(a, b);
        }
    }
}
