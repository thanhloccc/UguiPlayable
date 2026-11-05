using System.Collections.Generic;
using UnityEditor.Timeline;
using UnityEditor.Timeline.Actions;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace ApexionGame.UguiPlayable.Editor.Presets
{
    /// <summary>
    /// "UGUI Playable/Add Preset…" in the Timeline context menu: opens the <see cref="PresetPicker"/> to add a preset
    /// at the playhead of the open timeline.
    /// </summary>
    /// <remarks>
    /// Timeline finds actions with <c>TypeCache</c> and creates them with their default constructor.
    /// </remarks>
    [MenuEntry("UGUI Playable/Add Preset…")]
    internal sealed class AddPresetAction : TimelineAction
    {
        public override ActionValidity Validate(ActionContext context)
            => context.timeline.IsValid() ? ActionValidity.Valid : ActionValidity.NotApplicable;

        public override bool Execute(ActionContext context)
        {
            if (context.timeline.IsInvalid())
            {
                return false;
            }

            var window = TimelineEditor.GetWindow();
            var time = window.IsValid() ? window.playbackControls.GetCurrentTime() : 0d;

            PresetPicker.ShowAdd(context.timeline, context.director, time, PresetPicker.GetAnchor(window));
            return true;
        }
    }

    /// <summary>
    /// "UGUI Playable/Apply Preset…" in the clip context menu: opens the <see cref="PresetPicker"/> to apply a
    /// preset to the selected clips. Shown when every selected clip is a clip of this package.
    /// </summary>
    [MenuEntry("UGUI Playable/Apply Preset…")]
    internal sealed class ApplyPresetAction : ClipAction
    {
        public override ActionValidity Validate(IEnumerable<TimelineClip> clips)
        {
            var any = false;

            foreach (var clip in clips)
            {
                if (PresetRecipe.IsTweenClip(clip.asset) == false)
                {
                    return ActionValidity.NotApplicable;
                }

                any = true;
            }

            return any ? ActionValidity.Valid : ActionValidity.NotApplicable;
        }

        public override bool Execute(IEnumerable<TimelineClip> clips)
        {
            var assets = new List<PlayableAsset>();

            foreach (var clip in clips)
            {
                if (clip.asset is PlayableAsset asset && PresetRecipe.IsTweenClip(asset))
                {
                    assets.Add(asset);
                }
            }

            if (assets.Count < 1)
            {
                return false;
            }

            PresetPicker.ShowApply(assets.ToArray(), PresetPicker.GetAnchor(TimelineEditor.GetWindow()));
            return true;
        }
    }
}
