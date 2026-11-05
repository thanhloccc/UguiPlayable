using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Editor.Clips;
using ApexionGame.UguiPlayable.Editor.Common;
using ApexionGame.UguiPlayable.Editor.Timeline;
using ApexionGame.UguiPlayable.Tracks.Transforms;

namespace ApexionGame.UguiPlayable.Editor.Tracks
{
    [CustomEditor(typeof(RectTransformClip)), CanEditMultipleObjects]
    internal sealed class RectTransformClipInspector : TweenClipInspector
    {
        protected override string Title => "Rect Transform Clip";

        protected override Texture2D Icon => EditorIcons.Move;
    }

    [CustomEditor(typeof(RectTransformTrack))]
    internal sealed class RectTransformTrackInspector : TweenTrackInspector
    {
        protected override string Title => "Rect Transform Track";

        protected override Texture2D Icon => EditorIcons.Move;

        protected override string GetBindingWarning(UnityEngine.Object binding)
            => RectTransformLayoutCheck.GetWarning(binding);
    }

    [CustomTimelineEditor(typeof(RectTransformClip))]
    internal sealed class RectTransformClipEditor : TweenClipEditor<RectTransformClip>
    {
        protected override string DefaultName => "Rect Transform";

        protected override Texture2D Icon => EditorIcons.Move;

        protected override TweenBehaviour GetTemplate(RectTransformClip clip)
            => clip.Template;
    }

    [CustomTimelineEditor(typeof(RectTransformTrack))]
    internal sealed class RectTransformTrackEditor : TweenTrackEditor
    {
        protected override Texture2D Icon => EditorIcons.Move;

        protected override string GetBindingWarning(UnityEngine.Object binding)
            => RectTransformLayoutCheck.GetWarning(binding);
    }
}
