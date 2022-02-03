using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using GitHub.UI.ViewModels;

namespace GitHub.UI.Views
{
    public class DeviceCodeView : UserControl
    {
        public DeviceCodeView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void CopyCode(object sender, RoutedEventArgs e)
        {
            if (DataContext is DeviceCodeViewModel vm)
            {
                Avalonia.Application.Current.Clipboard.SetTextAsync(vm.UserCode);
            }
        }
    }
}
