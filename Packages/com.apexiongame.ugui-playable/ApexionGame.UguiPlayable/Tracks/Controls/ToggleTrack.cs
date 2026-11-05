using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Controls
{
    /// <summary>
    /// Tweens a <see cref="Toggle"/>: its <see cref="Toggle.isOn"/> state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The step value applies when the clip passes its threshold. Scrubbing, preview and Revert never call
    /// <see cref="Toggle.onValueChanged"/> on the track's behalf; <see cref="SendCallbacks"/> opts in, in Play Mode only.
    /// </para>
    /// <para>
    /// For a toggle in a <see cref="ToggleGroup"/>, the Edit Mode preview registers <c>isOn</c> of every toggle of the
    /// group in the scene, so the group is left as it was. In Edit Mode, Unity itself calls <c>onValueChanged</c>
    /// once per registered toggle when the preview ends. With <see cref="ToggleGroup.allowSwitchOff"/> off, the group
    /// turns the last toggle back on when it is switched off.
    /// </para>
    /// </remarks>
    [TrackColor(0.55f, 0.78f, 0.45f)]
    [TrackClipType(typeof(ToggleClip))]
    [TrackBindingType(typeof(Toggle))]
#if UNITY_EDITOR
    [System.ComponentModel.DisplayName("UGUI Playable/Controls/Toggle")]
#endif
    public sealed class ToggleTrack : TweenTrack<Toggle, ToggleBehaviour, ToggleMixer>
    {
        [SerializeField]
        private bool _sendCallbacks;

        /// <summary>
        /// Whether the track raises <c>Toggle.onValueChanged</c> when it writes the value. Off by default.
        /// </summary>
        /// <remarks>
        /// Takes effect in Play Mode only. Scrubbing in Edit Mode and Revert never notify on the track's behalf. In Edit
        /// Mode, Unity itself calls <c>onValueChanged</c> once per registered toggle when the preview ends.
        /// </remarks>
        public bool SendCallbacks
        {
            get => _sendCallbacks;
            set => _sendCallbacks = value;
        }

        /// <inheritdoc/>
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            var playable = base.CreateTrackMixer(graph, go, inputCount);
            ((ScriptPlayable<ToggleMixer>)playable).GetBehaviour().SendCallbacks = _sendCallbacks;
            return playable;
        }

        /// <inheritdoc/>
        protected override void GatherProperty(Toggle binding, string name, IPropertyCollector driver)
        {
            base.GatherProperty(binding, name, driver);

            if (binding.group == null)
            {
                return;
            }

            // Edit Mode only, once per preview: allocating is fine here.
            var siblings = new List<Toggle>();
            CollectGroupSiblings(binding, siblings);

            foreach (var sibling in siblings)
            {
                driver.AddFromName(sibling, name);
            }
        }

        /// <summary>
        /// Adds to <paramref name="result"/> every other toggle in the binding's scene that shares its group.
        /// </summary>
        internal static void CollectGroupSiblings(Toggle binding, List<Toggle> result)
        {
            var group = binding.group;

            if (group == null)
            {
                return;
            }

            var roots = new List<GameObject>();
            binding.gameObject.scene.GetRootGameObjects(roots);

            var found = new List<Toggle>();

            foreach (var root in roots)
            {
                root.GetComponentsInChildren(true, found);

                foreach (var toggle in found)
                {
                    if (toggle != binding && toggle.group == group)
                    {
                        result.Add(toggle);
                    }
                }
            }
        }
    }
}
