using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using ApexionGame.UguiPlayable.Easing;

namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// The per-clip data of a tween track: one serialized channel per animatable property.
    /// </summary>
    /// <remarks>
    /// Preview API: this base class may change before 1.0. Each clip creates its own copy of the behaviour, so
    /// runtime state such as <see cref="TweenChannel{T}.Capture"/> is per clip.
    /// </remarks>
    [Serializable]
    public abstract class TweenBehaviour : PlayableBehaviour
    {
        /// <summary>
        /// Adds the serialized property names of every enabled channel, for Timeline's edit-mode preview.
        /// </summary>
        /// <remarks>
        /// Use names that exist on the bound component (for example <c>m_Alpha</c>); an unknown name logs a
        /// DrivenPropertyManager error.
        /// </remarks>
        public abstract void CollectPropertyNames(ICollection<string> names);

        // Editor tooling hooks. Virtual, not abstract, so user tracks outside this assembly can still derive from the
        // public base (D7); their clips simply get no "animates nothing" check and no background curve.

        /// <summary>Whether any channel of this behaviour is enabled. Allocation-free.</summary>
        internal virtual bool HasEnabledChannel() => true;

        /// <summary>The ease of the first enabled tween channel, if there is one.</summary>
        internal virtual bool TryGetFirstEnabledEase(out EaseSetting ease)
        {
            ease = default;
            return false;
        }

        /// <summary>
        /// Called on the first frame the clip gets weight. Capture values for <see cref="ValueMode.FromCurrent"/>.
        /// </summary>
        public virtual void OnActivated(Component binding)
        {
        }

        /// <summary>
        /// Called on the first frame the clip has no weight after being active.
        /// </summary>
        public virtual void OnDeactivated()
        {
        }
    }
}
