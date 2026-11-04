// Ported from EncosyTower 0.1.9-preview.5: EncosyTower.Core/Debugging/ThrowHelper.cs
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ApexionGame.UguiPlayable
{
    /// <summary>
    /// Provides generic validation guards and exception factories for the package.
    /// </summary>
    internal static class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [HideInCallstack, StackTraceHidden]
        public static void ThrowIfNull(
              [NotNull] object argument
            , [CallerArgumentExpression("argument")] string paramName = null
        )
        {
            if (argument == null)
            {
                throw CreateArgumentNullException(paramName);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [HideInCallstack, StackTraceHidden]
        public static void ThrowIfUnityObjectInvalid(
              [NotNull] UnityEngine.Object argument
            , [CallerArgumentExpression("argument")] string paramName = null
        )
        {
            if (argument.IsInvalid())
            {
                throw CreateArgumentNullException_UnityObject(paramName);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ArgumentNullException CreateArgumentNullException(string paramName)
            => new(paramName);

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ArgumentNullException CreateArgumentNullException_UnityObject(string paramName)
            => new(paramName, "The Unity object cannot be null or invalid.");
    }
}
