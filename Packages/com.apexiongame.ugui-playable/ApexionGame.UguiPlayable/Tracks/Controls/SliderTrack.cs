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
    }
}
