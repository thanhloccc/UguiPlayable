using UnityEngine;

namespace ApexionGame.UguiPlayable
{
    /// <summary>
    /// The one place the package asks whether the player is in Play Mode, so EditMode tests can drive both answers.
    /// </summary>
    internal static class PlayModeSeam
    {
        /// <summary>Set by tests to force the answer. Tests must clear it in TearDown.</summary>
        internal static bool? OverrideForTests;

        /// <summary><see cref="Application.isPlaying"/> unless a test overrides it.</summary>
        internal static bool IsPlaying
        {
            get
            {
                var overridden = OverrideForTests;
                return overridden.HasValue ? overridden.Value : Application.isPlaying;
            }
        }
    }
}
