using TMPro;
using UnityEngine.Timeline;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Text
{
    /// <summary>
    /// Animates each character of a <see cref="TMP_Text"/>: offset, rotation, scale, alpha and tint transitions with
    /// a stagger, plus a wave and a shake.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The track writes the text mesh, never a serialized property, and re-applies its effect whenever TMP rebuilds
    /// the text, so a TMP Text track (colour, typewriter, string tween) on the same text keeps working with it. Works
    /// on <c>TextMeshProUGUI</c> and on the world-space <c>TextMeshPro</c>.
    /// </para>
    /// <para>
    /// The track always restores the text when the timeline stops, whatever its Restore Mode says: a mesh effect
    /// cannot outlive the graph, because the next rebuild of the text would wipe it.
    /// </para>
    /// <para>
    /// Limits: one character track per text (a second one would animate the first one's result); Reverse geometry
    /// sorting is not supported (the track writes nothing on such a text); underline and strikethrough stay still;
    /// scales well above 1 soften the SDF edges, because TMP keeps each glyph's SDF scale.
    /// </para>
    /// </remarks>
    [TrackColor(0.62f, 0.52f, 0.86f)]
    [TrackClipType(typeof(TmpCharacterClip))]
    [TrackBindingType(typeof(TMP_Text))]
#if UNITY_EDITOR
    [System.ComponentModel.DisplayName("UGUI Playable/Text/TMP Characters")]
#endif
    public sealed class TmpCharacterTrack : TweenTrack<TMP_Text, TmpCharacterBehaviour, TmpCharacterMixer>
    {
    }
}
