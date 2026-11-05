// Ported from EncosyTower 0.1.9-preview.5: EncosyTower.Core/Editor/ThrowHelper.cs
// and EncosyTower.Core/Debugging/ThrowHelper.cs (ThrowIfNullOrEmpty)

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

// Declared in the assembly root namespace, like the runtime assembly's Internal/ThrowHelper.cs.
namespace ApexionGame.UguiPlayable.Editor
{
    /// <summary>
    /// Guards used by the editor asset loaders. They stay active in every build of the editor assembly because a
    /// missing editor asset is a packaging error, not optional validation.
    /// </summary>
    internal static class ThrowHelper
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        [HideInCallstack]
        public static void ThrowIfAssetNotFound(bool isFound, string path)
        {
            if (isFound == false)
            {
                throw CreateAssetNotFoundException(path);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [HideInCallstack]
        public static void ThrowIfNullOrEmpty(string argument, string paramName)
        {
            if (string.IsNullOrEmpty(argument))
            {
                throw CreateArgumentNullOrEmptyException(paramName);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static InvalidOperationException CreateAssetNotFoundException(string path)
            => new($"Editor asset was not found at '{path}'.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static ArgumentNullException CreateArgumentNullOrEmptyException(string paramName)
            => new(paramName, "The string cannot be null or empty.");
    }
}
