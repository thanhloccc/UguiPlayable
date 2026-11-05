using UnityEngine;
using UnityEngine.Timeline;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Transforms
{
    /// <summary>
    /// Tweens a <see cref="RectTransform"/>: anchored position, size, rotation, scale, pivot and anchors.
    /// </summary>
    [TrackColor(0.95f, 0.62f, 0.22f)]
    [TrackClipType(typeof(RectTransformClip))]
    [TrackBindingType(typeof(RectTransform))]
#if UNITY_EDITOR
    [System.ComponentModel.DisplayName("UGUI Playable/Transforms/Rect Transform")]
#endif
    public sealed class RectTransformTrack : TweenTrack<RectTransform, RectTransformBehaviour, RectTransformMixer>
    {
    }
}
