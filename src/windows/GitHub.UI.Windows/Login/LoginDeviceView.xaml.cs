using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace GitHub.UI.Login
{
    public partial class LoginDeviceView : UserControl
    {
        public LoginDeviceView()
        {
            InitializeComponent();

            IsVisibleChanged += (s, e) =>
            {
                if (IsVisible)
                {
                    Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, (Action)(() => SetFocus()));
                }
            };
        }

        /// <summary>
        /// The DataContext of this view as a LoginDeviceView.
        /// </summary>
        public LoginDeviceViewModel ViewModel => DataContext as LoginDeviceViewModel;

        void SetFocus()
        {
            deviceCode.Focus();
        }

        private void CopyDeviceCodeButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Clipboard.SetText(deviceCode.Text);
        }
    }
}
