using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// A Timeline track that tweens one <typeparamref name="TBinding"/> component with
    /// <typeparamref name="TBehaviour"/> clips, blended by <typeparamref name="TMixer"/>.
    /// </summary>
    /// <remarks>
    /// Preview API: this base class may change before 1.0. A concrete track also needs
    /// <see cref="TrackClipTypeAttribute"/> and <see cref="TrackBindingTypeAttribute"/>.
    /// </remarks>
    public abstract class TweenTrack<TBinding, TBehaviour, TMixer> : TrackAsset
        where TBinding : Component
        where TBehaviour : TweenBehaviour, new()
        where TMixer : TweenMixer<TBinding, TBehaviour>, new()
    {
        [SerializeField]
        private RestoreMode _restoreMode = RestoreMode.Keep;

        /// <summary>What happens to the animated properties when the graph stops in Play Mode.</summary>
        public RestoreMode RestoreMode
        {
            get => _restoreMode;
            set => _restoreMode = value;
        }

        /// <inheritdoc/>
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var playable = ScriptPlayable<TMixer>.Create(graph, inputCount);
            var windows = new ClipWindow[inputCount];
            var index = 0;

            // Timeline connects clip i of GetClips() to mixer input i (TrackAsset.CompileClips).
            foreach (var clip in GetClips())
            {
                if (index >= inputCount)
                {
                    break;
                }

                windows[index++] = new ClipWindow(clip);
            }

            playable.GetBehaviour().Initialize(windows, _restoreMode);
            return playable;
        }

        /// <inheritdoc/>
        public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            var binding = director.GetGenericBinding(this) as TBinding;

            if (binding.IsInvalid())
            {
                return;
            }

            var names = new HashSet<string>();

            foreach (var clip in GetClips())
            {
                if (clip.asset is TweenClip<TBehaviour> tweenClip && tweenClip.Template != null)
                {
                    tweenClip.Template.CollectPropertyNames(names);
                }
            }

            foreach (var name in names)
            {
                GatherProperty(binding, name, driver);
            }
        }

        /// <summary>
        /// Registers one name from <see cref="TweenBehaviour.CollectPropertyNames"/> for Timeline's edit-mode preview.
        /// The default registers the serialized property <paramref name="name"/> on the bound component.
        /// </summary>
        /// <remarks>
        /// Preview API: this hook may change before 1.0. Override it when a channel animates something that is not
        /// serialized on the binding itself: the behaviour reports a name of its own choosing, and the track maps it
        /// to the real objects and properties (for example a ScrollRect's content position and scrollbars). The
        /// name is only reported while a clip enables the channel.
        /// </remarks>
        protected virtual void GatherProperty(TBinding binding, string name, IPropertyCollector driver)
        {
            driver.AddFromName(binding, name);
        }
    }
}
