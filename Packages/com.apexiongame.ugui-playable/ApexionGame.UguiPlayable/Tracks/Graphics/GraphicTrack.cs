using UnityEngine.Timeline;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Graphics
{
    /// <summary>
    /// Tweens a <see cref="Graphic"/>: color, and an alpha-only channel that keeps the RGB.
    /// </summary>
    [TrackColor(0.55f, 0.78f, 0.42f)]
    [TrackClipType(typeof(GraphicClip))]
    [TrackBindingType(typeof(Graphic))]
#if UNITY_EDITOR
    [System.ComponentModel.DisplayName("UGUI Playable/Graphics/Graphic")]
#endif
    public sealed class GraphicTrack : TweenTrack<Graphic, GraphicBehaviour, GraphicMixer>
    {
    }
}
