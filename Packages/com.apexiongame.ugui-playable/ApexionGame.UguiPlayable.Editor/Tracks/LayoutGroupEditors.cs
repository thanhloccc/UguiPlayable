using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Editor.Clips;
using ApexionGame.UguiPlayable.Editor.Common;
using ApexionGame.UguiPlayable.Editor.Timeline;
using ApexionGame.UguiPlayable.Tracks.Layout;

namespace ApexionGame.UguiPlayable.Editor.Tracks
{
    [CustomEditor(typeof(LayoutGroupClip))]
    internal sealed class LayoutGroupClipInspector : TweenClipInspector
    {
        protected override string Title => "Layout Group Clip";

        protected override Texture2D Icon => EditorIcons.Columns3;
    }

    [CustomEditor(typeof(LayoutGroupTrack))]
    internal sealed class LayoutGroupTrackInspector : TweenTrackInspector
    {
        protected override string Title => "Layout Group Track";

        protected override Texture2D Icon => EditorIcons.Columns3;
    }

    [CustomTimelineEditor(typeof(LayoutGroupClip))]
    internal sealed class LayoutGroupClipEditor : TweenClipEditor<LayoutGroupClip>
    {
        protected override string DefaultName => "Layout Group";

        protected override Texture2D Icon => EditorIcons.Columns3;

        protected override TweenBehaviour GetTemplate(LayoutGroupClip clip)
            => clip.Template;
    }

    [CustomTimelineEditor(typeof(LayoutGroupTrack))]
    internal sealed class LayoutGroupTrackEditor : TweenTrackEditor
    {
        protected override Texture2D Icon => EditorIcons.Columns3;
    }
}
