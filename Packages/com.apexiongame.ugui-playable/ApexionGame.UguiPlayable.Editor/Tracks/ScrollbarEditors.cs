using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.UIElements;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Editor.Clips;
using ApexionGame.UguiPlayable.Editor.Common;
using ApexionGame.UguiPlayable.Editor.Timeline;
using ApexionGame.UguiPlayable.Tracks.Controls;

namespace ApexionGame.UguiPlayable.Editor.Tracks
{
    [CustomEditor(typeof(ScrollbarClip)), CanEditMultipleObjects]
    internal sealed class ScrollbarClipInspector : TweenClipInspector
    {
        protected override string Title => "Scrollbar Clip";

        protected override Texture2D Icon => EditorIcons.GalleryVertical;
    }

    [CustomEditor(typeof(ScrollbarTrack))]
    internal sealed class ScrollbarTrackInspector : TweenTrackInspector
    {
        protected override string Title => "Scrollbar Track";

        protected override Texture2D Icon => EditorIcons.GalleryVertical;

        protected override void AddTrackRows(VisualElement root)
            => AddSendCallbacksRows(root, TweenClipTexts.SEND_CALLBACKS_SCROLLBAR_NOTE);
    }

    [CustomTimelineEditor(typeof(ScrollbarClip))]
    internal sealed class ScrollbarClipEditor : TweenClipEditor<ScrollbarClip>
    {
        protected override string DefaultName => "Scrollbar";

        protected override Texture2D Icon => EditorIcons.GalleryVertical;

        protected override TweenBehaviour GetTemplate(ScrollbarClip clip)
            => clip.Template;
    }

    [CustomTimelineEditor(typeof(ScrollbarTrack))]
    internal sealed class ScrollbarTrackEditor : TweenTrackEditor
    {
        protected override Texture2D Icon => EditorIcons.GalleryVertical;
    }
}
