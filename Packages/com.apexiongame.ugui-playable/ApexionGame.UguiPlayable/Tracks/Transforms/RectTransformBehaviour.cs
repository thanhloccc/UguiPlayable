using System;
using System.Collections.Generic;
using UnityEngine;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Transforms
{
    /// <summary>
    /// The channels of a <see cref="RectTransformClip"/>: anchored position, size delta, rotation (euler angles),
    /// scale, pivot, anchor min and anchor max. Every channel is disabled by default.
    /// </summary>
    /// <remarks>
    /// The vector channels honour <see cref="TweenChannel{T}.IgnoredAxes"/>, so two clips can animate different axes
    /// of one property.
    /// </remarks>
    [Serializable]
    public sealed class RectTransformBehaviour : TweenBehaviour
    {
        internal const string ANCHORED_POSITION_PROPERTY = "m_AnchoredPosition";
        internal const string LOCAL_POSITION_PROPERTY = "m_LocalPosition";
        internal const string SIZE_DELTA_PROPERTY = "m_SizeDelta";
        internal const string ROTATION_PROPERTY = "m_LocalRotation";
        internal const string SCALE_PROPERTY = "m_LocalScale";
        internal const string PIVOT_PROPERTY = "m_Pivot";
        internal const string ANCHOR_MIN_PROPERTY = "m_AnchorMin";
        internal const string ANCHOR_MAX_PROPERTY = "m_AnchorMax";

        private static readonly Vector2 s_defaultSize = new(100f, 100f);
        private static readonly Vector2 s_center = new(0.5f, 0.5f);

        [SerializeField]
        private TweenChannel<Vector3> _anchoredPosition = new(Vector3.zero, Vector3.zero, enabled: false);

        [SerializeField]
        private TweenChannel<Vector2> _sizeDelta = new(s_defaultSize, s_defaultSize, enabled: false);

        [SerializeField]
        private TweenChannel<Vector3> _rotation = new(Vector3.zero, Vector3.zero, enabled: false);

        [SerializeField]
        private RotationMode _rotationMode;

        [SerializeField]
        private TweenChannel<Vector3> _scale = new(Vector3.one, Vector3.one, enabled: false);

        [SerializeField]
        private TweenChannel<Vector2> _pivot = new(s_center, s_center, enabled: false);

        [SerializeField]
        private TweenChannel<Vector2> _anchorMin = new(s_center, s_center, enabled: false);

        [SerializeField]
        private TweenChannel<Vector2> _anchorMax = new(s_center, s_center, enabled: false);

        [SerializeField]
        private bool _sizeDeltaConstrainFrom;

        [SerializeField]
        private bool _sizeDeltaConstrainTo;

        [SerializeField]
        private bool _scaleConstrainFrom;

        [SerializeField]
        private bool _scaleConstrainTo;

        [SerializeField]
        private OscillationChannel _punch = OscillationChannel.Punch(
              OscillationTarget.Scale
            , new Vector3(0.2f, 0.2f, 0f)
            , enabled: false
        );

        [SerializeField]
        private OscillationChannel _shake = OscillationChannel.Shake(
              OscillationTarget.Position
            , new Vector3(10f, 10f, 0f)
            , enabled: false
        );

        /// <summary>The <see cref="RectTransform.anchoredPosition3D"/> channel.</summary>
        public TweenChannel<Vector3> AnchoredPosition
        {
            get => _anchoredPosition;
            set => _anchoredPosition = value;
        }

        /// <summary>The <see cref="RectTransform.sizeDelta"/> channel.</summary>
        public TweenChannel<Vector2> SizeDelta
        {
            get => _sizeDelta;
            set => _sizeDelta = value;
        }

        /// <summary>The <see cref="Transform.localEulerAngles"/> channel, blended per axis in degrees.</summary>
        /// <remarks>See <see cref="RotationMode"/> for the shortest-path alternative.</remarks>
        public TweenChannel<Vector3> Rotation
        {
            get => _rotation;
            set => _rotation = value;
        }

        /// <summary>
        /// How the rotation channel turns. <see cref="Transforms.RotationMode.Euler"/> (the default) blends each euler
        /// angle; <see cref="Transforms.RotationMode.ShortestPath"/> turns the short way and ignores the axis toggles.
        /// </summary>
        public RotationMode RotationMode
        {
            get => _rotationMode;
            set => _rotationMode = value;
        }

        /// <summary>The <see cref="Transform.localScale"/> channel.</summary>
        public TweenChannel<Vector3> Scale
        {
            get => _scale;
            set => _scale = value;
        }

        /// <summary>The <see cref="RectTransform.pivot"/> channel.</summary>
        public TweenChannel<Vector2> Pivot
        {
            get => _pivot;
            set => _pivot = value;
        }

        /// <summary>The <see cref="RectTransform.anchorMin"/> channel.</summary>
        public TweenChannel<Vector2> AnchorMin
        {
            get => _anchorMin;
            set => _anchorMin = value;
        }

        /// <summary>The <see cref="RectTransform.anchorMax"/> channel.</summary>
        public TweenChannel<Vector2> AnchorMax
        {
            get => _anchorMax;
            set => _anchorMax = value;
        }

        /// <summary>Editor only: keep the size delta's from value proportional. The runtime ignores it.</summary>
        public bool SizeDeltaConstrainFrom
        {
            get => _sizeDeltaConstrainFrom;
            set => _sizeDeltaConstrainFrom = value;
        }

        /// <summary>Editor only: keep the size delta's to value proportional. The runtime ignores it.</summary>
        public bool SizeDeltaConstrainTo
        {
            get => _sizeDeltaConstrainTo;
            set => _sizeDeltaConstrainTo = value;
        }

        /// <summary>Editor only: keep the scale's from value proportional. The runtime ignores it.</summary>
        public bool ScaleConstrainFrom
        {
            get => _scaleConstrainFrom;
            set => _scaleConstrainFrom = value;
        }

        /// <summary>Editor only: keep the scale's to value proportional. The runtime ignores it.</summary>
        public bool ScaleConstrainTo
        {
            get => _scaleConstrainTo;
            set => _scaleConstrainTo = value;
        }

        /// <summary>
        /// A damped oscillation added on top of the blended result of its target, weighted by the input weight.
        /// </summary>
        public OscillationChannel Punch
        {
            get => _punch;
            set => _punch = value;
        }

        /// <summary>
        /// Deterministic noise added on top of the blended result of its target, weighted by the input weight.
        /// </summary>
        public OscillationChannel Shake
        {
            get => _shake;
            set => _shake = value;
        }

        /// <inheritdoc/>
        internal override bool HasEnabledChannel()
            => _anchoredPosition.Enabled
                || _sizeDelta.Enabled
                || _rotation.Enabled
                || _scale.Enabled
                || _pivot.Enabled
                || _anchorMin.Enabled
                || _anchorMax.Enabled
                || _punch.Enabled
                || _shake.Enabled;

        /// <inheritdoc/>
        public override void CollectPropertyNames(ICollection<string> names)
        {
            if (_anchoredPosition.Enabled)
            {
                names.Add(ANCHORED_POSITION_PROPERTY);
                names.Add(LOCAL_POSITION_PROPERTY);
            }

            if (_sizeDelta.Enabled)
            {
                names.Add(SIZE_DELTA_PROPERTY);
            }

            if (_rotation.Enabled)
            {
                names.Add(ROTATION_PROPERTY);
            }

            if (_scale.Enabled)
            {
                names.Add(SCALE_PROPERTY);
            }

            if (_pivot.Enabled)
            {
                names.Add(PIVOT_PROPERTY);
            }

            if (_anchorMin.Enabled)
            {
                names.Add(ANCHOR_MIN_PROPERTY);
            }

            if (_anchorMax.Enabled)
            {
                names.Add(ANCHOR_MAX_PROPERTY);
            }

            if (_punch.Enabled)
            {
                AddOscillationNames(_punch.Target, names);
            }

            if (_shake.Enabled)
            {
                AddOscillationNames(_shake.Target, names);
            }
        }

        private static void AddOscillationNames(OscillationTarget target, ICollection<string> names)
        {
            switch (target)
            {
                case OscillationTarget.Position:
                {
                    names.Add(ANCHORED_POSITION_PROPERTY);
                    names.Add(LOCAL_POSITION_PROPERTY);
                    break;
                }

                case OscillationTarget.Rotation:
                {
                    names.Add(ROTATION_PROPERTY);
                    break;
                }

                case OscillationTarget.Scale:
                {
                    names.Add(SCALE_PROPERTY);
                    break;
                }
            }
        }

        /// <summary>
        /// Captures the start of a <see cref="ValueMode.FromCurrent"/> rotation. The mixer supplies the euler angles
        /// it last wrote, because reading <c>localEulerAngles</c> back can return a flipped decomposition.
        /// </summary>
        internal void CaptureRotation(Vector3 current)
        {
            if (_rotation.Enabled && _rotation.Mode == ValueMode.FromCurrent)
            {
                _rotation.Capture(current);
            }
        }

        /// <summary>
        /// Re-captures the anchored position and the scale of a <see cref="ValueMode.FromCurrent"/> channel from
        /// values without the oscillation offset, so FromCurrent never captures an offset. Runs after
        /// <see cref="OnActivated"/>, which read the binding with the offset still on it.
        /// </summary>
        internal void CapturePreOffset(Vector3 anchoredPosition, Vector3 scale)
        {
            if (_anchoredPosition.Enabled && _anchoredPosition.Mode == ValueMode.FromCurrent)
            {
                _anchoredPosition.Capture(anchoredPosition);
            }

            if (_scale.Enabled && _scale.Mode == ValueMode.FromCurrent)
            {
                _scale.Capture(scale);
            }
        }

        /// <inheritdoc/>
        public override void OnActivated(Component binding)
        {
            if (binding is not RectTransform rect)
            {
                return;
            }

            if (_anchoredPosition.Enabled && _anchoredPosition.Mode == ValueMode.FromCurrent)
            {
                _anchoredPosition.Capture(rect.anchoredPosition3D);
            }

            if (_sizeDelta.Enabled && _sizeDelta.Mode == ValueMode.FromCurrent)
            {
                _sizeDelta.Capture(rect.sizeDelta);
            }

            if (_scale.Enabled && _scale.Mode == ValueMode.FromCurrent)
            {
                _scale.Capture(rect.localScale);
            }

            if (_pivot.Enabled && _pivot.Mode == ValueMode.FromCurrent)
            {
                _pivot.Capture(rect.pivot);
            }

            if (_anchorMin.Enabled && _anchorMin.Mode == ValueMode.FromCurrent)
            {
                _anchorMin.Capture(rect.anchorMin);
            }

            if (_anchorMax.Enabled && _anchorMax.Mode == ValueMode.FromCurrent)
            {
                _anchorMax.Capture(rect.anchorMax);
            }
        }

        /// <inheritdoc/>
        public override void OnDeactivated()
        {
            _anchoredPosition.ClearCapture();
            _sizeDelta.ClearCapture();
            _rotation.ClearCapture();
            _scale.ClearCapture();
            _pivot.ClearCapture();
            _anchorMin.ClearCapture();
            _anchorMax.ClearCapture();
        }
    }
}
