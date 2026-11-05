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
    [CustomEditor(typeof(LayoutElementClip))]
    internal sealed class LayoutElementClipInspector : TweenClipInspector
    {
        protected override string Title => "Layout Element Clip";

        protected override Texture2D Icon => EditorIcons.Ruler;
    }

    [CustomEditor(typeof(LayoutElementTrack))]
    internal sealed class LayoutElementTrackInspector : TweenTrackInspector
    {
        protected override string Title => "Layout Element Track";

        protected override Texture2D Icon => EditorIcons.Ruler;
    }

    [CustomTimelineEditor(typeof(LayoutElementClip))]
    internal sealed class LayoutElementClipEditor : TweenClipEditor<LayoutElementClip>
    {
        protected override string DefaultName => "Layout Element";

        protected override Texture2D Icon => EditorIcons.Ruler;

        protected override TweenBehaviour GetTemplate(LayoutElementClip clip)
            => clip.Template;
    }

    [CustomTimelineEditor(typeof(LayoutElementTrack))]
    internal sealed class LayoutElementTrackEditor : TweenTrackEditor
    {
        protected override Texture2D Icon => EditorIcons.Ruler;
    }
}
