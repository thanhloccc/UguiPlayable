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
    [CustomEditor(typeof(GridLayoutGroupClip)), CanEditMultipleObjects]
    internal sealed class GridLayoutGroupClipInspector : TweenClipInspector
    {
        protected override string Title => "Grid Layout Group Clip";

        protected override Texture2D Icon => EditorIcons.Grid3x3;
    }

    [CustomEditor(typeof(GridLayoutGroupTrack))]
    internal sealed class GridLayoutGroupTrackInspector : TweenTrackInspector
    {
        protected override string Title => "Grid Layout Group Track";

        protected override Texture2D Icon => EditorIcons.Grid3x3;
    }

    [CustomTimelineEditor(typeof(GridLayoutGroupClip))]
    internal sealed class GridLayoutGroupClipEditor : TweenClipEditor<GridLayoutGroupClip>
    {
        protected override string DefaultName => "Grid Layout Group";

        protected override Texture2D Icon => EditorIcons.Grid3x3;

        protected override TweenBehaviour GetTemplate(GridLayoutGroupClip clip)
            => clip.Template;
    }

    [CustomTimelineEditor(typeof(GridLayoutGroupTrack))]
    internal sealed class GridLayoutGroupTrackEditor : TweenTrackEditor
    {
        protected override Texture2D Icon => EditorIcons.Grid3x3;
    }
}
