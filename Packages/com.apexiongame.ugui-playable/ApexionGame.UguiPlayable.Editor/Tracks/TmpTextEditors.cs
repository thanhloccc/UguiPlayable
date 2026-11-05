using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Editor.Clips;
using ApexionGame.UguiPlayable.Editor.Common;
using ApexionGame.UguiPlayable.Editor.Timeline;
using ApexionGame.UguiPlayable.Tracks.Text;

namespace ApexionGame.UguiPlayable.Editor.Tracks
{
    [CustomEditor(typeof(TmpTextClip)), CanEditMultipleObjects]
    internal sealed class TmpTextClipInspector : TweenClipInspector
    {
        protected override string Title => "TMP Text Clip";

        protected override Texture2D Icon => EditorIcons.Type;
    }

    [CustomEditor(typeof(TmpTextTrack))]
    internal sealed class TmpTextTrackInspector : TweenTrackInspector
    {
        protected override string Title => "TMP Text Track";

        protected override Texture2D Icon => EditorIcons.Type;
    }

    [CustomTimelineEditor(typeof(TmpTextClip))]
    internal sealed class TmpTextClipEditor : TweenClipEditor<TmpTextClip>
    {
        protected override string DefaultName => "TMP Text";

        protected override Texture2D Icon => EditorIcons.Type;

        protected override TweenBehaviour GetTemplate(TmpTextClip clip)
            => clip.Template;
    }

    [CustomTimelineEditor(typeof(TmpTextTrack))]
    internal sealed class TmpTextTrackEditor : TweenTrackEditor
    {
        protected override Texture2D Icon => EditorIcons.Type;
    }
}
