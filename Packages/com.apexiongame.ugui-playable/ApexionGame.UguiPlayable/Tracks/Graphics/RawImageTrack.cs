using UnityEngine.Timeline;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Graphics
{
    /// <summary>
    /// Tweens a <see cref="RawImage"/>: color, alpha and UV rect.
    /// </summary>
    [TrackColor(0.36f, 0.70f, 0.86f)]
    [TrackClipType(typeof(RawImageClip))]
    [TrackBindingType(typeof(RawImage))]
#if UNITY_EDITOR
    [System.ComponentModel.DisplayName("UGUI Playable/Graphics/Raw Image")]
#endif
    public sealed class RawImageTrack : TweenTrack<RawImage, RawImageBehaviour, RawImageMixer>
    {
    }
}
