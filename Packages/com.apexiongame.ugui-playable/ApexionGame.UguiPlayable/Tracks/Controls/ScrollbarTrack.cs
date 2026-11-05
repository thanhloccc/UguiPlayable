using UnityEngine.Timeline;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Controls
{
    /// <summary>
    /// Tweens a <see cref="Scrollbar"/>: its value and its handle size.
    /// </summary>
    [TrackColor(0.62f, 0.52f, 0.90f)]
    [TrackClipType(typeof(ScrollbarClip))]
    [TrackBindingType(typeof(Scrollbar))]
#if UNITY_EDITOR
    [System.ComponentModel.DisplayName("UGUI Playable/Controls/Scrollbar")]
#endif
    public sealed class ScrollbarTrack : TweenTrack<Scrollbar, ScrollbarBehaviour, ScrollbarMixer>
    {
    }
}
