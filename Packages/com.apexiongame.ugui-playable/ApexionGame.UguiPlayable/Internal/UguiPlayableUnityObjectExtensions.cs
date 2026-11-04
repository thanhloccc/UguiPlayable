// Ported from EncosyTower 0.1.9-preview.5: EncosyTower.Core/UnityEngine.Extensions/EncosyUnityObjectExtensions.cs

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

// Declared in the root namespace so every package namespace sees it without a using.
namespace ApexionGame.UguiPlayable
{
    internal static class UguiPlayableUnityObjectExtensions
    {
        /// <summary>
        /// Returns true if <paramref name="self"/> is not null and not destroyed, otherwise returns false.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsValid([NotNullWhen(true)] this UnityEngine.Object self)
            => self != null && self != false;

        /// <summary>
        /// Returns true if <paramref name="self"/> is null or destroyed, otherwise returns false.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsInvalid([NotNullWhen(false)] this UnityEngine.Object self)
            => self == null || self == false;
    }
}
