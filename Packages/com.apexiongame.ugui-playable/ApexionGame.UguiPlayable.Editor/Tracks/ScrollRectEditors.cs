using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Editor.Clips;
using ApexionGame.UguiPlayable.Editor.Common;
using ApexionGame.UguiPlayable.Editor.Timeline;
using ApexionGame.UguiPlayable.Tracks.Controls;

namespace ApexionGame.UguiPlayable.Editor.Tracks
{
    [CustomEditor(typeof(ScrollRectClip))]
    internal sealed class ScrollRectClipInspector : TweenClipInspector
    {
        protected override string Title => "Scroll Rect Clip";

        protected override Texture2D Icon => EditorIcons.Scroll;
    }

    [CustomEditor(typeof(ScrollRectTrack))]
    internal sealed class ScrollRectTrackInspector : TweenTrackInspector
    {
        protected override string Title => "Scroll Rect Track";

        protected override Texture2D Icon => EditorIcons.Scroll;

        protected override string GetBindingWarning(UnityEngine.Object binding)
            => ScrollRectContentCheck.GetWarning(binding);
    }

    [CustomTimelineEditor(typeof(ScrollRectClip))]
    internal sealed class ScrollRectClipEditor : TweenClipEditor<ScrollRectClip>
    {
        protected override string DefaultName => "Scroll Rect";

        protected override Texture2D Icon => EditorIcons.Scroll;

        protected override TweenBehaviour GetTemplate(ScrollRectClip clip)
            => clip.Template;
    }

    [CustomTimelineEditor(typeof(ScrollRectTrack))]
    internal sealed class ScrollRectTrackEditor : TweenTrackEditor
    {
        protected override Texture2D Icon => EditorIcons.Scroll;

        protected override string GetBindingWarning(UnityEngine.Object binding)
            => ScrollRectContentCheck.GetWarning(binding);
    }
}
