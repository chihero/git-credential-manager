using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using GitCredentialManager;

namespace Microsoft.Identity.GitCredentialManager.Controls
{
    public class TesterWindow : Window
    {
        public TesterWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
