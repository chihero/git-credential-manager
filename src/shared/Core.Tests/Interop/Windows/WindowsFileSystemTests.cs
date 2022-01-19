using GitCredentialManager.Interop.Windows;
using Xunit;

namespace GitCredentialManager.Tests.Interop.Windows
{
    public class WindowsFileSystemTests
    {
        [PlatformTheory(Platforms.Windows)]
        [InlineData("", "", false)]
        [InlineData("", @"C:\Users\test\bin\file1", false)]
        [InlineData(@"C:\Users\test\file1", "", false)]
        [InlineData(@"C:\Users\test\file1", @"C:\Users\test\file2", false)]
        [InlineData(@"C:\Users\test\bin\file2", @"C:\Users\test\bin\file1", false)]
        [InlineData(@"C:\Users\test\myapp", @"X:\Users\test\myapp", false)]
        [InlineData(@"C:\Users\test\myapp", @"C:\Users\test\bin\myapp", false)]
        [InlineData(@"C:\Users\test\bin\myapp", @"C:\Users\test\bin\myapp", true)]
        [InlineData(@"C:\Users\test\BIN\myapp", @"C:\Users\test\bin\MYAPP", true)]
        [InlineData(@"C:\Users\test\bin\..\bin\myapp", @"C:\Users\test\bin\myapp", true)]
        [InlineData(@"C:\Users\test\bin\myapp", @"C:\Users\test\bin\..\bin\myapp", true)]
        public void WindowsFileSystem_IsSamePath(string a, string b, bool expected)
        {
            var fs = new WindowsFileSystem();
            bool actual = fs.IsSamePath(a, b);
            Assert.Equal(expected, actual);
        }
    }
}
