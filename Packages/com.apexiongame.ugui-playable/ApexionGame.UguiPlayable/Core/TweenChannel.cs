using System;
using UnityEngine;
using ApexionGame.UguiPlayable.Easing;

namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// One animatable property of a tween clip: whether it is enabled, its from and to values, its ease and its
    /// <see cref="ValueMode"/>.
    /// </summary>
    /// <remarks>
    /// Evaluate a channel with <see cref="TweenChannelMath"/>. The serialized field names are user data.
    /// </remarks>
    [Serializable]
    public struct TweenChannel<T> where T : struct
    {
        [SerializeField]
        private bool _enabled;

        [SerializeField]
        private ValueMode _mode;

        [SerializeField]
        private T _from;

        [SerializeField]
        private T _to;

        [SerializeField]
        private EaseSetting _ease;

        [SerializeField]
        private AxisMask _ignoredAxes;

        [NonSerialized]
        private T _captured;

        [NonSerialized]
        private bool _hasCaptured;

        /// <summary>
        /// Creates a channel with a linear ease.
        /// </summary>
        public TweenChannel(
              T from
            , T to
            , ValueMode mode = ValueMode.Absolute
            , bool enabled = true
            , AxisMask ignoredAxes = AxisMask.None
        )
            : this(from, to, EaseSetting.Default, mode, enabled, ignoredAxes)
        {
        }

        /// <summary>
        /// Creates a channel.
        /// </summary>
        public TweenChannel(
              T from
            , T to
            , EaseSetting ease
            , ValueMode mode = ValueMode.Absolute
            , bool enabled = true
            , AxisMask ignoredAxes = AxisMask.None
        )
        {
            _enabled = enabled;
            _mode = mode;
            _from = from;
            _to = to;
            _ease = ease;
            _ignoredAxes = ignoredAxes;
            _captured = default;
            _hasCaptured = false;
        }

        /// <summary>Whether the channel animates its property.</summary>
        public bool Enabled => _enabled;

        /// <summary>How <see cref="From"/> and <see cref="To"/> are interpreted.</summary>
        public ValueMode Mode => _mode;

        /// <summary>The start value. Ignored in <see cref="ValueMode.FromCurrent"/>.</summary>
        public T From => _from;

        /// <summary>The end value.</summary>
        public T To => _to;

        /// <summary>The ease applied to the clip's progress.</summary>
        public EaseSetting Ease => _ease;

        /// <summary>
        /// The axes this channel leaves alone, so another clip can animate them. Used by <see cref="Vector2"/>,
        /// <see cref="Vector3"/> and <see cref="Vector4"/> channels, ignored by float and color channels.
        /// </summary>
        public AxisMask IgnoredAxes => _ignoredAxes;

        /// <summary>Whether <see cref="Captured"/> holds the value read when the clip became active.</summary>
        public bool HasCaptured => _hasCaptured;

        /// <summary>The value read when the clip became active, used by <see cref="ValueMode.FromCurrent"/>.</summary>
        public T Captured => _captured;

        /// <summary>
        /// Stores the property's current value as the start of a <see cref="ValueMode.FromCurrent"/> tween.
        /// </summary>
        public void Capture(T current)
        {
            _captured = current;
            _hasCaptured = true;
        }

        /// <summary>
        /// Forgets the captured value, so the next activation captures again.
        /// </summary>
        public void ClearCapture()
        {
            _captured = default;
            _hasCaptured = false;
        }
    }
}
