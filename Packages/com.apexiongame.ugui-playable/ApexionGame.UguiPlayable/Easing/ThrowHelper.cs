using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace ApexionGame.UguiPlayable.Easing
{
    internal static class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [HideInCallstack, StackTraceHidden, DoesNotReturn]
        public static void ThrowEaseHasNoFormula(Ease ease)
            => throw new ArgumentOutOfRangeException(
                nameof(ease)
                , ease
                , "This ease has no formula. Evaluate it through EaseSetting."
            );
    }
}
