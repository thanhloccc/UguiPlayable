using UnityEngine.Timeline;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Layout
{
    /// <summary>
    /// Tweens a <see cref="LayoutElement"/>: its minimum, preferred and flexible width and height.
    /// </summary>
    /// <remarks>
    /// This is the supported way to animate an element inside a layout group: the group reads these sizes on every
    /// layout rebuild and moves the siblings to make room.
    /// </remarks>
    [TrackColor(0.78f, 0.62f, 0.34f)]
    [TrackClipType(typeof(LayoutElementClip))]
    [TrackBindingType(typeof(LayoutElement))]
#if UNITY_EDITOR
    [System.ComponentModel.DisplayName("UGUI Playable/Layout/Layout Element")]
#endif
    public sealed class LayoutElementTrack : TweenTrack<LayoutElement, LayoutElementBehaviour, LayoutElementMixer>
    {
    }
}
