using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Editor.Clips;
using ApexionGame.UguiPlayable.Editor.Common;
using ApexionGame.UguiPlayable.Editor.Timeline;
using ApexionGame.UguiPlayable.Tracks.Text;
using Object = UnityEngine.Object;

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

        protected override string GetBindingWarning(Object binding)
            => TmpTextTrackCheck.GetWarning(target as TrackAsset, binding);
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

        public override TrackDrawOptions GetTrackOptions(TrackAsset track, Object binding)
        {
            var options = base.GetTrackOptions(track, binding);

            if (string.IsNullOrEmpty(options.errorText))
            {
                options.errorText = TmpTextTrackCheck.GetWarning(track, binding);
            }

            return options;
        }
    }

    /// <summary>
    /// Warns that a Font Size channel does nothing while the bound text has Auto Size on.
    /// </summary>
    /// <remarks>Runs on every track draw: it reads the clip array and two fields, and allocates nothing else.</remarks>
    internal static class TmpTextTrackCheck
    {
        public static string GetWarning(TrackAsset track, Object binding)
        {
            if (track.IsInvalid() || BindingValueReader.IsAutoSizing(binding) == false)
            {
                return string.Empty;
            }

            foreach (var clip in track.GetClips())
            {
                if (clip.asset is TmpTextClip textClip && textClip.Template.FontSize.Enabled)
                {
                    return TweenClipTexts.AUTO_SIZE_WARNING;
                }
            }

            return string.Empty;
        }
    }
}
