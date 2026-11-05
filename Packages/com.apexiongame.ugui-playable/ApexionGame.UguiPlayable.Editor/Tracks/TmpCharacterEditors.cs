using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UIElements;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Editor.Clips;
using ApexionGame.UguiPlayable.Editor.Common;
using ApexionGame.UguiPlayable.Editor.Timeline;
using ApexionGame.UguiPlayable.Tracks.Text;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Editor.Tracks
{
    /// <summary>
    /// The warnings of a TMP Characters track: Reverse geometry sorting on the bound text, and a second character
    /// track bound to the same text in the inspected director.
    /// </summary>
    /// <remarks>
    /// The track count per text is cached per timeline and rebuilt only when the inspected director, the timeline,
    /// its output track count or this track's binding changes, so a track draw costs a dictionary lookup.
    /// </remarks>
    internal static class TmpCharacterTrackCheck
    {
        public const string TWO_TRACKS =
            "Another TMP Characters track drives this text. Use one character track per text.";

        public const string REVERSE_SORTING =
            "Reverse geometry sorting is not supported: this track writes nothing on this text.";

        private static readonly Dictionary<TMP_Text, int> s_counts = new();
        private static readonly Dictionary<TrackAsset, TMP_Text> s_bindings = new();

        private static TimelineAsset s_timeline;
        private static PlayableDirector s_director;
        private static int s_trackCount = -1;

        public static string GetWarning(TrackAsset track, Object binding)
        {
            var text = ToText(binding);

            if (text.IsInvalid())
            {
                return string.Empty;
            }

            if (text.geometrySortingOrder != VertexSortingOrder.Normal)
            {
                return REVERSE_SORTING;
            }

            return CountTracks(track, text, TimelineEditor.inspectedDirector) > 1 ? TWO_TRACKS : string.Empty;
        }

        /// <summary>
        /// The number of character tracks of <paramref name="track"/>'s timeline that <paramref name="director"/>
        /// binds to <paramref name="text"/>.
        /// </summary>
        internal static int CountTracks(TrackAsset track, TMP_Text text, PlayableDirector director)
        {
            var timeline = track.IsValid() ? track.timelineAsset : null;

            if (timeline.IsInvalid() || director.IsInvalid())
            {
                return 0;
            }

            var isStale = timeline != s_timeline
                || director != s_director
                || timeline.outputTrackCount != s_trackCount
                || s_bindings.TryGetValue(track, out var cached) == false
                || cached != text;

            if (isStale)
            {
                Rebuild(timeline, director);
            }

            return s_counts.TryGetValue(text, out var count) ? count : 0;
        }

        /// <summary>Tests: forgets the cache.</summary>
        internal static void Clear()
        {
            s_counts.Clear();
            s_bindings.Clear();
            s_timeline = null;
            s_director = null;
            s_trackCount = -1;
        }

        private static void Rebuild(TimelineAsset timeline, PlayableDirector director)
        {
            s_counts.Clear();
            s_bindings.Clear();
            s_timeline = timeline;
            s_director = director;
            s_trackCount = timeline.outputTrackCount;

            foreach (var output in timeline.GetOutputTracks())
            {
                if (output is not TmpCharacterTrack)
                {
                    continue;
                }

                var bound = ToText(director.GetGenericBinding(output));
                s_bindings[output] = bound;

                if (bound.IsInvalid())
                {
                    continue;
                }

                s_counts[bound] = s_counts.TryGetValue(bound, out var count) ? count + 1 : 1;
            }
        }

        private static TMP_Text ToText(Object binding)
            => binding switch {
                TMP_Text text => text,
                GameObject gameObject => gameObject.GetComponent<TMP_Text>(),
                _ => null,
            };
    }

    [CustomEditor(typeof(TmpCharacterClip))]
    internal sealed class TmpCharacterClipInspector : TweenClipInspector
    {
        protected override string Title => "TMP Characters Clip";

        protected override Texture2D Icon => EditorIcons.TextSelect;

        protected override void AddSections(VisualElement root)
            => root.Add(CharacterTimingSection.Create(serializedObject));
    }

    [CustomEditor(typeof(TmpCharacterTrack))]
    internal sealed class TmpCharacterTrackInspector : TweenTrackInspector
    {
        private const string ALWAYS_RESTORES_NOTE = "This track always restores the text when the timeline stops.";

        protected override string Title => "TMP Characters Track";

        protected override Texture2D Icon => EditorIcons.TextSelect;

        protected override bool ShowRestoreMode => false;

        protected override void AddTrackRows(VisualElement root)
            => root.Add(TweenClipSections.CreateNote(ALWAYS_RESTORES_NOTE));

        protected override string GetBindingWarning(Object binding)
            => TmpCharacterTrackCheck.GetWarning(target as TrackAsset, binding);
    }

    [CustomTimelineEditor(typeof(TmpCharacterClip))]
    internal sealed class TmpCharacterClipEditor : TweenClipEditor<TmpCharacterClip>
    {
        protected override string DefaultName => "TMP Characters";

        protected override Texture2D Icon => EditorIcons.TextSelect;

        protected override TweenBehaviour GetTemplate(TmpCharacterClip clip)
            => clip.Template;
    }

    [CustomTimelineEditor(typeof(TmpCharacterTrack))]
    internal sealed class TmpCharacterTrackEditor : TweenTrackEditor
    {
        protected override Texture2D Icon => EditorIcons.TextSelect;

        // The two-track check needs the track, so it lives here rather than in GetBindingWarning(Object), which has
        // no track and runs on every draw.
        public override TrackDrawOptions GetTrackOptions(TrackAsset track, Object binding)
        {
            var options = base.GetTrackOptions(track, binding);

            if (string.IsNullOrEmpty(options.errorText))
            {
                options.errorText = TmpCharacterTrackCheck.GetWarning(track, binding);
            }

            return options;
        }
    }
}
