using UnityEngine;
using UnityEngine.Timeline;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Groups
{
    /// <summary>
    /// Tweens a <see cref="CanvasGroup"/>: alpha, interactable and blocks raycasts.
    /// </summary>
    [TrackColor(0.36f, 0.62f, 0.86f)]
    [TrackClipType(typeof(CanvasGroupClip))]
    [TrackBindingType(typeof(CanvasGroup))]
#if UNITY_EDITOR
    [System.ComponentModel.DisplayName("UGUI Playable/Groups/Canvas Group")]
#endif
    public sealed class CanvasGroupTrack : TweenTrack<CanvasGroup, CanvasGroupBehaviour, CanvasGroupMixer>
    {
    }
}
