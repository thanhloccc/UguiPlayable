using UnityEngine.Timeline;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Effects
{
    /// <summary>
    /// Tweens a <see cref="Shadow"/>: its color and distance.
    /// </summary>
    /// <remarks>
    /// <see cref="Outline"/> derives from <see cref="Shadow"/>, so the track accepts an outline too.
    /// </remarks>
    [TrackColor(0.55f, 0.50f, 0.80f)]
    [TrackClipType(typeof(ShadowClip))]
    [TrackBindingType(typeof(Shadow))]
#if UNITY_EDITOR
    [System.ComponentModel.DisplayName("UGUI Playable/Effects/Shadow")]
#endif
    public sealed class ShadowTrack : TweenTrack<Shadow, ShadowBehaviour, ShadowMixer>
    {
    }
}
