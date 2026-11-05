using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Editor.Clips;
using ApexionGame.UguiPlayable.Editor.Common;
using ApexionGame.UguiPlayable.Editor.Timeline;
using ApexionGame.UguiPlayable.Tracks.Groups;

namespace ApexionGame.UguiPlayable.Editor.Tracks
{
    [CustomEditor(typeof(CanvasGroupClip))]
    internal sealed class CanvasGroupClipInspector : TweenClipInspector
    {
        protected override string Title => "Canvas Group Clip";

        protected override Texture2D Icon => EditorIcons.Layers;
    }

    [CustomEditor(typeof(CanvasGroupTrack))]
    internal sealed class CanvasGroupTrackInspector : TweenTrackInspector
    {
        protected override string Title => "Canvas Group Track";

        protected override Texture2D Icon => EditorIcons.Layers;
    }

    [CustomTimelineEditor(typeof(CanvasGroupClip))]
    internal sealed class CanvasGroupClipEditor : TweenClipEditor<CanvasGroupClip>
    {
        protected override string DefaultName => "Canvas Group";

        protected override Texture2D Icon => EditorIcons.Layers;

        protected override TweenBehaviour GetTemplate(CanvasGroupClip clip)
            => clip.Template;
    }

    [CustomTimelineEditor(typeof(CanvasGroupTrack))]
    internal sealed class CanvasGroupTrackEditor : TweenTrackEditor
    {
        protected override Texture2D Icon => EditorIcons.Layers;
    }
}
