using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Editor.Clips;
using ApexionGame.UguiPlayable.Editor.Common;
using ApexionGame.UguiPlayable.Editor.Timeline;
using ApexionGame.UguiPlayable.Tracks.Controls;

namespace ApexionGame.UguiPlayable.Editor.Tracks
{
    [CustomEditor(typeof(SliderClip))]
    internal sealed class SliderClipInspector : TweenClipInspector
    {
        protected override string Title => "Slider Clip";

        protected override Texture2D Icon => EditorIcons.SlidersHorizontal;
    }

    [CustomEditor(typeof(SliderTrack))]
    internal sealed class SliderTrackInspector : TweenTrackInspector
    {
        protected override string Title => "Slider Track";

        protected override Texture2D Icon => EditorIcons.SlidersHorizontal;
    }

    [CustomTimelineEditor(typeof(SliderClip))]
    internal sealed class SliderClipEditor : TweenClipEditor<SliderClip>
    {
        protected override string DefaultName => "Slider";

        protected override Texture2D Icon => EditorIcons.SlidersHorizontal;

        protected override TweenBehaviour GetTemplate(SliderClip clip)
            => clip.Template;
    }

    [CustomTimelineEditor(typeof(SliderTrack))]
    internal sealed class SliderTrackEditor : TweenTrackEditor
    {
        protected override Texture2D Icon => EditorIcons.SlidersHorizontal;
    }
}
