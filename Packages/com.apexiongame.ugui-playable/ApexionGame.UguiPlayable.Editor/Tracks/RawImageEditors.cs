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
    [CustomEditor(typeof(RawImageClip)), CanEditMultipleObjects]
    internal sealed class RawImageClipInspector : TweenClipInspector
    {
        protected override string Title => "Raw Image Clip";

        protected override Texture2D Icon => EditorIcons.Wallpaper;
    }

    [CustomEditor(typeof(RawImageTrack))]
    internal sealed class RawImageTrackInspector : TweenTrackInspector
    {
        protected override string Title => "Raw Image Track";

        protected override Texture2D Icon => EditorIcons.Wallpaper;
    }

    [CustomTimelineEditor(typeof(RawImageClip))]
    internal sealed class RawImageClipEditor : TweenClipEditor<RawImageClip>
    {
        protected override string DefaultName => "Raw Image";

        protected override Texture2D Icon => EditorIcons.Wallpaper;

        protected override TweenBehaviour GetTemplate(RawImageClip clip)
            => clip.Template;
    }

    [CustomTimelineEditor(typeof(RawImageTrack))]
    internal sealed class RawImageTrackEditor : TweenTrackEditor
    {
        protected override Texture2D Icon => EditorIcons.Wallpaper;
    }
}
