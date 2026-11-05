using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.UIElements;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Editor.Clips;
using ApexionGame.UguiPlayable.Editor.Common;
using ApexionGame.UguiPlayable.Editor.Timeline;
using ApexionGame.UguiPlayable.Tracks.Controls;

namespace ApexionGame.UguiPlayable.Editor.Tracks
{
    internal static class ToggleGraphicCheck
    {
        public const string NO_GRAPHIC = "No checkmark graphic: nothing changes visually.";

        public static string GetWarning(UnityEngine.Object binding)
        {
            var toggle = binding switch {
                UnityEngine.UI.Toggle direct => direct,
                GameObject gameObject => gameObject.GetComponent<UnityEngine.UI.Toggle>(),
                _ => null,
            };

            return toggle.IsValid() && toggle.graphic.IsInvalid() ? NO_GRAPHIC : string.Empty;
        }
    }

    [CustomEditor(typeof(ToggleClip))]
    internal sealed class ToggleClipInspector : TweenClipInspector
    {
        protected override string Title => "Toggle Clip";

        protected override Texture2D Icon => EditorIcons.ToggleRight;
    }

    [CustomEditor(typeof(ToggleTrack))]
    internal sealed class ToggleTrackInspector : TweenTrackInspector
    {
        private const string SEND_CALLBACKS_NOTE =
            "Play Mode only. Scrubbing and Revert never call onValueChanged. In Edit Mode, Unity itself calls "
            + "onValueChanged once per toggle of the group when the preview ends.";

        protected override string Title => "Toggle Track";

        protected override Texture2D Icon => EditorIcons.ToggleRight;

        protected override void AddTrackRows(VisualElement root)
            => AddSendCallbacksRows(root, SEND_CALLBACKS_NOTE);

        protected override string GetBindingWarning(UnityEngine.Object binding)
            => ToggleGraphicCheck.GetWarning(binding);
    }

    [CustomTimelineEditor(typeof(ToggleClip))]
    internal sealed class ToggleClipEditor : TweenClipEditor<ToggleClip>
    {
        protected override string DefaultName => "Toggle";

        protected override Texture2D Icon => EditorIcons.ToggleRight;

        protected override TweenBehaviour GetTemplate(ToggleClip clip)
            => clip.Template;
    }

    [CustomTimelineEditor(typeof(ToggleTrack))]
    internal sealed class ToggleTrackEditor : TweenTrackEditor
    {
        protected override Texture2D Icon => EditorIcons.ToggleRight;

        protected override string GetBindingWarning(UnityEngine.Object binding)
            => ToggleGraphicCheck.GetWarning(binding);
    }
}
