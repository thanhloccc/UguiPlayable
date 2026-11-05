using System;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Editor.Clips
{
    /// <summary>
    /// Finds the object a clip's track is bound to through the open Timeline window, and tells listeners when that
    /// changes. Without a Timeline window, a director or a binding, <see cref="Binding"/> is null and
    /// <see cref="Reason"/> says why.
    /// </summary>
    internal sealed class ClipBindingSource
    {
        private readonly Object _clipAsset;

        private Object _binding;
        private string _reason = TweenClipTexts.NO_TIMELINE;

        public ClipBindingSource(Object clipAsset)
        {
            _clipAsset = clipAsset;
        }

        /// <summary>Raised by <see cref="Refresh"/> when the binding or the reason changed.</summary>
        public event Action Changed;

        public Object Binding => _binding;

        public string Reason => _reason;

        /// <summary>
        /// Resolves the binding again. Cheap: it walks the tracks of the open Timeline once.
        /// </summary>
        public void Refresh()
        {
            TryResolve(_clipAsset, out var binding, out var reason);

            if (_binding == binding && _reason == reason)
            {
                return;
            }

            _binding = binding;
            _reason = reason;
            Changed?.Invoke();
        }

        public static bool TryResolve(Object clipAsset, out Object binding, out string reason)
        {
            binding = null;

            var timeline = TimelineEditor.inspectedAsset;
            var director = TimelineEditor.inspectedDirector;

            if (timeline == null || director == null)
            {
                reason = TweenClipTexts.NO_TIMELINE;
                return false;
            }

            var track = FindTrack(timeline, clipAsset);

            if (track == null)
            {
                reason = TweenClipTexts.NO_TRACK;
                return false;
            }

            binding = director.GetGenericBinding(track);

            if (binding == null)
            {
                reason = TweenClipTexts.NO_BINDING;
                return false;
            }

            reason = string.Empty;
            return true;
        }

        private static TrackAsset FindTrack(TimelineAsset timeline, Object clipAsset)
        {
            foreach (var track in timeline.GetOutputTracks())
            {
                foreach (var clip in track.GetClips())
                {
                    if (clip.asset == clipAsset)
                    {
                        return track;
                    }
                }
            }

            return null;
        }
    }
}
