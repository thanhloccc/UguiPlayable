using UnityEngine.Timeline;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Layout
{
    /// <summary>
    /// Tweens a <see cref="GridLayoutGroup"/>: its spacing, cell size and padding.
    /// </summary>
    [TrackColor(0.50f, 0.58f, 0.82f)]
    [TrackClipType(typeof(GridLayoutGroupClip))]
    [TrackBindingType(typeof(GridLayoutGroup))]
#if UNITY_EDITOR
    [System.ComponentModel.DisplayName("UGUI Playable/Layout/Grid Layout Group")]
#endif
    public sealed class GridLayoutGroupTrack
        : TweenTrack<GridLayoutGroup, GridLayoutGroupBehaviour, GridLayoutGroupMixer>
    {
    }
}
