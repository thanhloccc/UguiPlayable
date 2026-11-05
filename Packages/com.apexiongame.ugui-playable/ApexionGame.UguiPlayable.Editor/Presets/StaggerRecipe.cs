using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Editor.Presets
{
    /// <summary>
    /// Adds a one-track <see cref="TweenPreset"/> once per active child of a container, each copy starting
    /// <c>delay</c> seconds after the previous one, so the children animate one after another. It is an editor-time
    /// recipe: it writes ordinary tracks and clips, and there is no runtime stagger track.
    /// </summary>
    /// <remarks>
    /// The tracks sit under one <see cref="GroupTrack"/> named after the container, in sibling order, and the whole
    /// call is one Undo step. When a director plays the timeline, each track is bound to the component of its binding
    /// type on its child; a child without that component gets no track, and one warning lists them. The index in
    /// <c>start = time + index × delay</c> counts the tracks created, so a skipped child leaves no gap.
    /// </remarks>
    internal static class StaggerRecipe
    {
        public const string UNDO_NAME = "Stagger Preset";

        /// <summary>
        /// Creates the group and the tracks in <paramref name="target"/>. The timeline is saved afterwards.
        /// </summary>
        /// <param name="director">The director playing <paramref name="target"/>; null adds unbound tracks.</param>
        /// <param name="time">Start of the first clip, normally the playhead.</param>
        /// <param name="delay">Seconds between the starts of consecutive clips.</param>
        public static PresetRecipeResult Stagger(
              TweenPreset preset
            , TimelineAsset target
            , PlayableDirector director
            , GameObject container
            , double time
            , double delay
        )
        {
            var result = new PresetRecipeResult(target);

            if (GetBlockReason(preset, target, container) is { Length: > 0 } reason)
            {
                result.Warnings.Add(reason);
                return result;
            }

            var sourceTrack = preset.Timeline.GetOutputTracks().First();
            var children = GetActiveChildren(container);

            if (children.Count < 1)
            {
                result.Warnings.Add($"'{container.name}' has no active child; nothing was added.");
                return result;
            }

            var canBind = director.IsValid() && director.playableAsset == target;

            if (canBind == false)
            {
                result.Warnings.Add(
                    "No director plays this timeline (asset mode or a sub-timeline): the tracks were added unbound."
                );
            }

            var group = PresetRecipe.BeginGroup(UNDO_NAME);
            var groupTrack = target.CreateTrack(typeof(GroupTrack), null, container.name);
            var bindingType = PresetRecipe.Binder.GetBindingType(sourceTrack);
            var skipped = new List<string>();
            var recorded = false;
            var index = 0;

            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                Object binding = null;

                if (canBind)
                {
                    binding = PresetRecipe.Binder.ResolveBinding(child, bindingType);

                    if (binding.IsInvalid())
                    {
                        skipped.Add(child.name);
                        continue;
                    }
                }

                var track = target.CreateTrack(sourceTrack.GetType(), groupTrack, child.name);

                PresetRecipe.CopyTrackSettings(sourceTrack, track);
                PresetRecipe.AddClips(sourceTrack, track, time + index * delay, result);
                result.Tracks.Add(track);
                index++;

                if (binding.IsValid())
                {
                    if (recorded == false)
                    {
                        Undo.RegisterCompleteObjectUndo(director, UNDO_NAME);
                        recorded = true;
                    }

                    director.SetGenericBinding(track, binding);
                    PresetRecipe.RecordPrefabInstance(director);
                }
            }

            if (skipped.Count > 0)
            {
                result.Warnings.Add(
                    $"No {bindingType?.Name} on {string.Join(", ", skipped)}; they got no track."
                );
            }

            Undo.CollapseUndoOperations(group);
            TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);

            // A prefab or scene save does not write the .playable.
            AssetDatabase.SaveAssetIfDirty(target);
            return result;
        }

        /// <summary>
        /// Why the stagger cannot run, or an empty string when it can: it needs a container, a timeline to add to,
        /// and a preset whose timeline has exactly one track.
        /// </summary>
        public static string GetBlockReason(TweenPreset preset, TimelineAsset target, GameObject container)
        {
            if (preset.IsInvalid() || preset.Timeline.IsInvalid())
            {
                return "Pick a preset with a timeline.";
            }

            if (target.IsInvalid())
            {
                return "Open a timeline in the Timeline window.";
            }

            if (container.IsInvalid())
            {
                return "Pick a container GameObject.";
            }

            var count = preset.Timeline.GetOutputTracks().Count();
            return count == 1 ? string.Empty : $"Stagger needs a preset with one track; '{preset.name}' has {count}.";
        }

        /// <summary>
        /// The children of <paramref name="container"/> that are active themselves, in sibling order.
        /// </summary>
        public static List<Transform> GetActiveChildren(GameObject container)
        {
            var children = new List<Transform>();

            if (container.IsInvalid())
            {
                return children;
            }

            var transform = container.transform;
            var count = transform.childCount;

            for (var i = 0; i < count; i++)
            {
                var child = transform.GetChild(i);

                if (child.gameObject.activeSelf)
                {
                    children.Add(child);
                }
            }

            return children;
        }
    }
}
