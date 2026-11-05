using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Editor.Clips;
using ApexionGame.UguiPlayable.Editor.Common;
using ApexionGame.UguiPlayable.Editor.Timeline;
using ApexionGame.UguiPlayable.Tracks.Effects;

namespace ApexionGame.UguiPlayable.Editor.Tracks
{
    [CustomEditor(typeof(ShadowClip)), CanEditMultipleObjects]
    internal sealed class ShadowClipInspector : TweenClipInspector
    {
        protected override string Title => "Shadow Clip";

        protected override Texture2D Icon => EditorIcons.Copy;
    }

    [CustomEditor(typeof(ShadowTrack))]
    internal sealed class ShadowTrackInspector : TweenTrackInspector
    {
        protected override string Title => "Shadow Track";

        protected override Texture2D Icon => EditorIcons.Copy;
    }

    [CustomTimelineEditor(typeof(ShadowClip))]
    internal sealed class ShadowClipEditor : TweenClipEditor<ShadowClip>
    {
        protected override string DefaultName => "Shadow";

        protected override Texture2D Icon => EditorIcons.Copy;

        protected override TweenBehaviour GetTemplate(ShadowClip clip)
            => clip.Template;
    }

    [CustomTimelineEditor(typeof(ShadowTrack))]
    internal sealed class ShadowTrackEditor : TweenTrackEditor
    {
        protected override Texture2D Icon => EditorIcons.Copy;
    }
}
