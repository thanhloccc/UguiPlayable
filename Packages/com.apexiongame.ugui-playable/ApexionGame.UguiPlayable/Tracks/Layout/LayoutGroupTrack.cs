using UnityEngine.Timeline;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Layout
{
    /// <summary>
    /// Tweens a <see cref="HorizontalOrVerticalLayoutGroup"/> (horizontal or vertical): its spacing and padding.
    /// </summary>
    [TrackColor(0.36f, 0.66f, 0.55f)]
    [TrackClipType(typeof(LayoutGroupClip))]
    [TrackBindingType(typeof(HorizontalOrVerticalLayoutGroup))]
#if UNITY_EDITOR
    [System.ComponentModel.DisplayName("UGUI Playable/Layout/Layout Group")]
#endif
    public sealed class LayoutGroupTrack
        : TweenTrack<HorizontalOrVerticalLayoutGroup, LayoutGroupBehaviour, LayoutGroupMixer>
    {
    }
}
