// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Authentication.UI.Login;
using Microsoft.Git.CredentialManager.UI;
using Microsoft.Git.CredentialManager.UI.Controls;

namespace Microsoft.Authentication.UI
{
    /// <summary>
    /// Interaction logic for Tester.xaml
    /// </summary>
    public partial class Tester : Window
    {
        public Tester()
        {
            InitializeComponent();
        }

        private IntPtr Handle => new WindowInteropHelper(this).Handle;

        private void ShowDeviceCode(object sender, RoutedEventArgs e)
        {
            var model = new LoginDeviceViewModel(deviceCode.Text, new Uri(verificationUrl.Text));
            var view = new LoginDeviceView();
            var window = new DialogWindow(model, view);
            Gui.ShowDialog(window, Handle);
        }
    }
}
