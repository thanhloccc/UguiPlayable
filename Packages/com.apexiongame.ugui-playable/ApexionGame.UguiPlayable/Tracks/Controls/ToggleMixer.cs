using System;
using System.Collections.Generic;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Controls
{
    /// <summary>
    /// Blends <see cref="ToggleClip"/> channels into the bound <see cref="Toggle"/>.
    /// </summary>
    /// <remarks>
    /// The base state of the toggle, and of the other toggles of its <see cref="ToggleGroup"/>, is captured when the
    /// track binds the toggle. Revert and the Edit Mode preview restore them as a set.
    /// </remarks>
    public sealed class ToggleMixer : TweenMixer<Toggle, ToggleBehaviour>
    {
        /// <summary>
        /// Whether a write raises <c>onValueChanged</c>. Set by the track; effective in Play Mode only.
        /// </summary>
        internal bool SendCallbacks { get; set; }

        // Slot 0 is the binding, the rest are the other toggles of its group. Grow-only, filled once per bind.
        private Toggle[] _toggles = Array.Empty<Toggle>();
        private bool[] _baseOn = Array.Empty<bool>();
        private int _count;
        private readonly List<Toggle> _siblings = new();

        private bool _baseIsOn;
        private StepAccumulator _isOn;
        private bool _wroteIsOn;

        /// <inheritdoc/>
        protected override void CaptureBase(Toggle binding)
        {
            _baseIsOn = binding.isOn;
            _wroteIsOn = false;

            _siblings.Clear();
            ToggleTrack.CollectGroupSiblings(binding, _siblings);

            _count = 1 + _siblings.Count;

            if (_toggles.Length < _count)
            {
                _toggles = new Toggle[_count];
                _baseOn = new bool[_count];
            }

            _toggles[0] = binding;
            _baseOn[0] = binding.isOn;

            for (var i = 0; i < _siblings.Count; i++)
            {
                _toggles[i + 1] = _siblings[i];
                _baseOn[i + 1] = _siblings[i].isOn;
            }

            _siblings.Clear();
        }

        /// <inheritdoc/>
        protected override void RestoreBase(Toggle binding)
        {
            if (_wroteIsOn == false)
            {
                return;
            }

            // Off first, then on: the group logic then never turns the restored "on" toggle off again, and a group
            // that does not allow switching off never leaves an "off" toggle forced on.
            for (var i = 0; i < _count; i++)
            {
                if (_baseOn[i] == false && _toggles[i].IsValid())
                {
                    _toggles[i].SetIsOnWithoutNotify(false);
                }
            }

            for (var i = 0; i < _count; i++)
            {
                if (_baseOn[i] && _toggles[i].IsValid())
                {
                    _toggles[i].SetIsOnWithoutNotify(true);
                }
            }
        }

        /// <inheritdoc/>
        /// <remarks>
        /// The checkmark alpha lives on the <c>CanvasRenderer</c>, which Timeline cannot revert, and
        /// <c>Toggle.OnValidate</c> does not refresh it after Timeline reverts <c>m_IsOn</c>. The alpha is written from
        /// the captured state.
        /// </remarks>
        protected override void RestoreUnserialized(Toggle binding)
        {
            if (_wroteIsOn == false)
            {
                return;
            }

            for (var i = 0; i < _count; i++)
            {
                var toggle = _toggles[i];

                if (toggle.IsValid() && toggle.graphic.IsValid())
                {
                    toggle.graphic.canvasRenderer.SetAlpha(_baseOn[i] ? 1f : 0f);
                }
            }
        }

        /// <inheritdoc/>
        protected override void BeginFrame()
        {
            _isOn.Reset();
        }

        /// <inheritdoc/>
        protected override void Accumulate(ToggleBehaviour behaviour, float weight, float progress)
        {
            var isOn = behaviour.IsOn;

            if (isOn.Enabled)
            {
                _isOn.Add(isOn, progress, weight);
            }
        }

        /// <inheritdoc/>
        protected override void AccumulateHold(ToggleBehaviour behaviour, double endTime)
        {
            var isOn = behaviour.IsOn;

            if (isOn.Enabled)
            {
                _isOn.OfferHold(isOn, endTime);
            }
        }

        /// <inheritdoc/>
        protected override void Apply(Toggle binding)
        {
            if (_isOn.HasContribution == false)
            {
                return;
            }

            _wroteIsOn = true;

            var value = _isOn.Resolve(_baseIsOn);

            // No write when nothing changes: in Play Mode a write plays the checkmark cross-fade, which allocates.
            if (value == binding.isOn)
            {
                return;
            }

            if (SendCallbacks && PlayModeSeam.IsPlaying)
            {
                binding.isOn = value;
            }
            else
            {
                binding.SetIsOnWithoutNotify(value);
            }
        }
    }
}
