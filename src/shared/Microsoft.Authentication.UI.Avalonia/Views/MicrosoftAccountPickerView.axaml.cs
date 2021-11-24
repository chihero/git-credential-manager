using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Microsoft.Identity.GitCredentialManager.Views
{
    public class MicrosoftAccountPickerView : UserControl
    {
        public MicrosoftAccountPickerView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
