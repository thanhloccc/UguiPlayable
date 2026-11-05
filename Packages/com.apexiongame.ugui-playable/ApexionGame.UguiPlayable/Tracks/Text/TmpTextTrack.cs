using TMPro;
using UnityEngine.Timeline;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Text
{
    /// <summary>
    /// Tweens a <see cref="TMP_Text"/>: color, alpha, font size, spacing, a typewriter ratio and the vertex gradient.
    /// </summary>
    /// <remarks>
    /// Works on <c>TextMeshProUGUI</c> and on the world-space <c>TextMeshPro</c>. The legacy
    /// <see cref="UnityEngine.UI.Text"/> is not supported.
    /// </remarks>
    [TrackColor(0.86f, 0.52f, 0.62f)]
    [TrackClipType(typeof(TmpTextClip))]
    [TrackBindingType(typeof(TMP_Text))]
#if UNITY_EDITOR
    [System.ComponentModel.DisplayName("UGUI Playable/Text/TMP Text")]
#endif
    public sealed class TmpTextTrack : TweenTrack<TMP_Text, TmpTextBehaviour, TmpTextMixer>
    {
    }
}
