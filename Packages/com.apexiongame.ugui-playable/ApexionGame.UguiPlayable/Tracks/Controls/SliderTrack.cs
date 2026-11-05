using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Controls
{
    /// <summary>
    /// Tweens a <see cref="Slider"/>: its value.
    /// </summary>
    [TrackColor(0.90f, 0.48f, 0.55f)]
    [TrackClipType(typeof(SliderClip))]
    [TrackBindingType(typeof(Slider))]
#if UNITY_EDITOR
    [System.ComponentModel.DisplayName("UGUI Playable/Controls/Slider")]
#endif
    public sealed class SliderTrack : TweenTrack<Slider, SliderBehaviour, SliderMixer>
    {
        [SerializeField]
        private bool _sendCallbacks;

        /// <summary>
        /// Whether the track raises <c>Slider.onValueChanged</c> when it writes the value. Off by default.
        /// </summary>
        /// <remarks>
        /// Takes effect in Play Mode only. Scrubbing in Edit Mode and Revert never notify on the track's behalf. In Edit
        /// Mode, Unity itself calls <c>onValueChanged</c> once when the preview ends.
        /// </remarks>
        public bool SendCallbacks
        {
            get => _sendCallbacks;
            set => _sendCallbacks = value;
        }

        /// <inheritdoc/>
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var playable = base.CreateTrackMixer(graph, go, inputCount);
            ((ScriptPlayable<SliderMixer>)playable).GetBehaviour().SendCallbacks = _sendCallbacks;
            return playable;
        }
    }
}
