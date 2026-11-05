using System;
using UnityEngine;
using UnityEngine.Playables;

namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// Blends the clips of a tween track into its bound component once per frame.
    /// </summary>
    /// <remarks>
    /// <para>Preview API: this base class may change before 1.0.</para>
    /// <para>
    /// Per frame: the base values are captured when the binding changes; every input with weight is accumulated at
    /// its progress; every clip that has finished (no weight, end ≤ time) offers its end value as the hold value;
    /// then <see cref="Apply"/> writes each property that received a contribution. A property no clip touches is not
    /// written. Subclasses must not allocate in these callbacks.
    /// </para>
    /// </remarks>
    public abstract class TweenMixer<TBinding, TBehaviour> : PlayableBehaviour
        where TBinding : Component
        where TBehaviour : TweenBehaviour, new()
    {
        private ClipWindow[] _windows = Array.Empty<ClipWindow>();
        private bool[] _active = Array.Empty<bool>();
        private RestoreMode _restoreMode;
        private TBinding _binding;
        private bool _hasBase;

        /// <summary>The component the track is bound to, or null before the first frame.</summary>
        public TBinding Binding => _binding;

        /// <summary>The track's restore option.</summary>
        public RestoreMode RestoreMode => _restoreMode;

        internal void Initialize(ClipWindow[] windows, RestoreMode restoreMode)
        {
            _windows = windows ?? Array.Empty<ClipWindow>();
            _active = new bool[_windows.Length];
            _restoreMode = restoreMode;
        }

        /// <inheritdoc/>
        public sealed override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            var binding = playerData as TBinding;

            if (binding.IsInvalid())
            {
                return;
            }

            if (ReferenceEquals(binding, _binding) == false || _hasBase == false)
            {
                _binding = binding;
                CaptureBase(binding);
                _hasBase = true;
            }

            var time = playable.GetGraph().GetRootPlayable(0).GetTime();
            var inputCount = Mathf.Min(playable.GetInputCount(), _windows.Length);

            BeginFrame();

            for (var i = 0; i < inputCount; i++)
            {
                var input = (ScriptPlayable<TBehaviour>)playable.GetInput(i);

                if (input.IsValid() == false)
                {
                    continue;
                }

                var behaviour = input.GetBehaviour();

                if (behaviour == null)
                {
                    continue;
                }

                var weight = playable.GetInputWeight(i);
                var window = _windows[i];

                if (weight > 0f)
                {
                    if (_active[i] == false)
                    {
                        _active[i] = true;
                        behaviour.OnActivated(binding);
                        OnInputActivated(behaviour, binding);
                    }

                    Accumulate(behaviour, weight, window.ToProgress(input.GetTime()));
                    continue;
                }

                if (_active[i])
                {
                    _active[i] = false;
                    behaviour.OnDeactivated();
                }

                if (window.End <= time)
                {
                    AccumulateHold(behaviour, window.End);
                }
            }

            Apply(binding);
        }

        /// <inheritdoc/>
        public override void OnGraphStop(Playable playable)
        {
            RestoreIfNeeded();
        }

        /// <inheritdoc/>
        public override void OnPlayableDestroy(Playable playable)
        {
            RestoreIfNeeded();
        }

        /// <summary>
        /// Called right after <see cref="TweenBehaviour.OnActivated"/>, for captures that need the mixer's own state
        /// (for example a rotation the mixer keeps unwrapped).
        /// </summary>
        protected virtual void OnInputActivated(TBehaviour behaviour, TBinding binding)
        {
        }

        /// <summary>Stores the bound component's current values as the base values.</summary>
        protected abstract void CaptureBase(TBinding binding);

        /// <summary>Writes the base values back, for the properties this mixer wrote.</summary>
        protected abstract void RestoreBase(TBinding binding);

        /// <summary>Resets the accumulators.</summary>
        protected abstract void BeginFrame();

        /// <summary>Adds an active clip at <paramref name="progress"/> with its input <paramref name="weight"/>.</summary>
        protected abstract void Accumulate(TBehaviour behaviour, float weight, float progress);

        /// <summary>Offers a finished clip's end values as hold values.</summary>
        protected abstract void AccumulateHold(TBehaviour behaviour, double endTime);

        /// <summary>Writes every property that received a contribution this frame.</summary>
        protected abstract void Apply(TBinding binding);

        private void RestoreIfNeeded()
        {
            if (_restoreMode == RestoreMode.Revert && _hasBase && _binding.IsValid())
            {
                RestoreBase(_binding);
            }

            _hasBase = false;
            _binding = null;

            for (var i = 0; i < _active.Length; i++)
            {
                _active[i] = false;
            }
        }
    }
}
