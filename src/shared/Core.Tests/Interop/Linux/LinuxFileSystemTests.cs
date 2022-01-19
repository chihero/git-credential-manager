using GitCredentialManager.Interop.Linux;
using Xunit;

namespace GitCredentialManager.Tests.Interop.Linux
{
    public class LinuxFileSystemTests
    {
        [PlatformTheory(Platforms.Linux)]
        [InlineData("", "", false)]
        [InlineData("", "/bin/ls", false)]
        [InlineData("/bin/ls", "", false)]
        [InlineData("/bin/ls", "/usr/bin/touch", false)]
        [InlineData("/usr/bin/touch", "/bin/ls", false)]
        [InlineData("/usr/bin/myapp", "/bin/myapp", false)]
        [InlineData("/bin/myapp", "/usr/bin/myapp", false)]
        [InlineData("/usr/bin/myapp", "/usr/bin/myapp", true)]
        [InlineData("/usr/BIN/myapp", "/usr/bin/MYAPP", false)]
        [InlineData("/usr/bin/../bin/myapp", "/usr/bin/myapp", true)]
        [InlineData("/usr/bin/myapp", "/usr/bin/../bin/myapp", true)]
        public void LinuxFileSystem_IsSamePath(string a, string b, bool expected)
        {
            var fs = new LinuxFileSystem();
            bool actual = fs.IsSamePath(a, b);
            Assert.Equal(expected, actual);
        }
    }
}
