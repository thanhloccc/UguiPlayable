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
    [CustomEditor(typeof(ImageClip))]
    internal sealed class ImageClipInspector : TweenClipInspector
    {
        protected override string Title => "Image Clip";

        protected override Texture2D Icon => EditorIcons.Image;
    }

    [CustomEditor(typeof(ImageTrack))]
    internal sealed class ImageTrackInspector : TweenTrackInspector
    {
        protected override string Title => "Image Track";

        protected override Texture2D Icon => EditorIcons.Image;
    }

    [CustomTimelineEditor(typeof(ImageClip))]
    internal sealed class ImageClipEditor : TweenClipEditor<ImageClip>
    {
        protected override string DefaultName => "Image";

        protected override Texture2D Icon => EditorIcons.Image;

        protected override TweenBehaviour GetTemplate(ImageClip clip)
            => clip.Template;
    }

    [CustomTimelineEditor(typeof(ImageTrack))]
    internal sealed class ImageTrackEditor : TweenTrackEditor
    {
        protected override Texture2D Icon => EditorIcons.Image;
    }
}
