using UnityEngine.Timeline;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Graphics
{
    /// <summary>
    /// Tweens an <see cref="Image"/>: color, alpha and fill amount.
    /// </summary>
    [TrackColor(0.42f, 0.78f, 0.66f)]
    [TrackClipType(typeof(ImageClip))]
    [TrackBindingType(typeof(Image))]
#if UNITY_EDITOR
    [System.ComponentModel.DisplayName("UGUI Playable/Graphics/Image")]
#endif
    public sealed class ImageTrack : TweenTrack<Image, ImageBehaviour, ImageMixer>
    {
    }
}
