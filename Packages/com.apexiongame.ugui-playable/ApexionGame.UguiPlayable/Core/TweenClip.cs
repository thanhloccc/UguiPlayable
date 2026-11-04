using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// A tween clip asset holding the serialized <typeparamref name="TBehaviour"/> template.
    /// </summary>
    /// <remarks>
    /// Preview API: this base class may change before 1.0. Clips support blending and extrapolation (hold, loop,
    /// ping-pong outside the clip). Clip-in and speed are not offered: a tween has no length of its own, the clip's
    /// duration is the tween's duration.
    /// </remarks>
    public abstract class TweenClip<TBehaviour> : PlayableAsset, ITimelineClipAsset
        where TBehaviour : TweenBehaviour, new()
    {
        [SerializeField]
        private TBehaviour _template = new();

        /// <summary>The serialized channels of this clip.</summary>
        public TBehaviour Template => _template;

        /// <inheritdoc/>
        public ClipCaps clipCaps
            => ClipCaps.Blending | ClipCaps.Extrapolation;

        /// <inheritdoc/>
        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
            => ScriptPlayable<TBehaviour>.Create(graph, _template);
    }
}
