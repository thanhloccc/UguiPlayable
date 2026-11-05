using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Editor.Clips;
using ApexionGame.UguiPlayable.Editor.Common;
using ApexionGame.UguiPlayable.Editor.Timeline;
using ApexionGame.UguiPlayable.Tracks.Graphics;

namespace ApexionGame.UguiPlayable.Editor.Tracks
{
    [CustomEditor(typeof(GraphicClip)), CanEditMultipleObjects]
    internal sealed class GraphicClipInspector : TweenClipInspector
    {
        protected override string Title => "Graphic Clip";

        protected override Texture2D Icon => EditorIcons.Palette;
    }

    [CustomEditor(typeof(GraphicTrack))]
    internal sealed class GraphicTrackInspector : TweenTrackInspector
    {
        protected override string Title => "Graphic Track";

        protected override Texture2D Icon => EditorIcons.Palette;
    }

    [CustomTimelineEditor(typeof(GraphicClip))]
    internal sealed class GraphicClipEditor : TweenClipEditor<GraphicClip>
    {
        protected override string DefaultName => "Graphic";

        protected override Texture2D Icon => EditorIcons.Palette;

        protected override TweenBehaviour GetTemplate(GraphicClip clip)
            => clip.Template;
    }

    [CustomTimelineEditor(typeof(GraphicTrack))]
    internal sealed class GraphicTrackEditor : TweenTrackEditor
    {
        protected override Texture2D Icon => EditorIcons.Palette;
    }
}
