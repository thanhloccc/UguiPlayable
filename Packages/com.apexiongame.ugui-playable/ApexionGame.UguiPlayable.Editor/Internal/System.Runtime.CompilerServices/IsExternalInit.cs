// Ported from EncosyTower 0.1.9-preview.5: EncosyTower.Core/System.Runtime.CompilerServices/IsExternalInit.cs
// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if !NET5_0_OR_GREATER

// Allows init setters and record types on downlevel frameworks.
namespace System.Runtime.CompilerServices
{
    internal sealed class IsExternalInit
    {
    }
}

#endif
