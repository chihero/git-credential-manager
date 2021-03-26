// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license.
using System;

namespace GitHub
{
    [Flags]
    public enum AuthenticationModes
    {
        None  = 0,
        Basic = 1,
        OAuth = 1 << 1,
        Pat   = 1 << 2,

        All   = Basic | OAuth | Pat
    }
}
