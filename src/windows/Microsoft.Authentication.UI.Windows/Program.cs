// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;
using Microsoft.Git.CredentialManager.UI;

namespace Microsoft.Authentication.UI
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            IGui gui = new Gui();
            gui.ShowWindow(() => new Tester());
        }
    }
}
