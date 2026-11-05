using System;

namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// Selects vector axes. A <see cref="TweenChannel{T}"/> uses it to list the axes it does not animate.
    /// </summary>
    /// <remarks>
    /// The integer values are serialized in user timelines and must never change.
    /// </remarks>
    [Flags]
    public enum AxisMask
    {
        /// <summary>No axis.</summary>
        None = 0,

        /// <summary>The X axis.</summary>
        X = 1,

        /// <summary>The Y axis.</summary>
        Y = 2,

        /// <summary>The Z axis. Ignored by <see cref="UnityEngine.Vector2"/> channels.</summary>
        Z = 4,

        /// <summary>
        /// The W axis, the fourth component of a <see cref="UnityEngine.Vector4"/> channel (for example the height of
        /// a UV rect or the bottom padding). Ignored by two- and three-component channels.
        /// </summary>
        W = 8,
    }
}
