using System;
using Avalonia;

namespace GitCredentialManager.UI
{
    public static class AppBuilderExtensions
    {
        public static AppBuilder UsePlatformDetect(this AppBuilder builder)
        {
#if WINDOWS
            return builder.UseWin32().UseSkia();
#elif OSX
            return builder.UseAvaloniaNative().UseSkia();
#elif LINUX
            return builder.UseX11().UseSkia();
#else
            return builder;
#endif
        }
    }
}
