using System;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Editor.Clips
{
    /// <summary>
    /// Finds the object a clip's track is bound to through the open Timeline window, and tells listeners when that
    /// changes. Without a Timeline window, a director or a binding, <see cref="Binding"/> is null and
    /// <see cref="Reason"/> says why. With several clips, <see cref="Binding"/> is the object every clip's track is
    /// bound to; when the tracks are bound to different objects it is null and the reason is
    /// <see cref="TweenClipTexts.MIXED_BINDINGS"/>.
    /// </summary>
    internal sealed class ClipBindingSource
    {
        private readonly Object[] _clipAssets;
        private readonly TryResolveDelegate _resolve;

        private Object _binding;
        private string _reason = TweenClipTexts.NO_TIMELINE;

        public ClipBindingSource(Object clipAsset)
            : this(new[] { clipAsset }, TryResolve)
        {
        }

        public ClipBindingSource(Object[] clipAssets)
            : this(clipAssets, TryResolve)
        {
        }

        /// <param name="resolve">Finds the binding of one clip. Tests pass their own instead of the open Timeline.</param>
        public ClipBindingSource(Object[] clipAssets, TryResolveDelegate resolve)
        {
            _clipAssets = clipAssets;
            _resolve = resolve;
        }

        public delegate bool TryResolveDelegate(Object clipAsset, out Object binding, out string reason);

        /// <summary>Raised by <see cref="Refresh"/> when the binding or the reason changed.</summary>
        public event Action Changed;

        public Object Binding => _binding;

        public string Reason => _reason;

        /// <summary>
        /// Resolves the binding again. Cheap: it walks the tracks of the open Timeline once.
        /// </summary>
        public void Refresh()
        {
            ResolveAll(out var binding, out var reason);

            if (_binding == binding && _reason == reason)
            {
                return;
            }

            _binding = binding;
            _reason = reason;
            Changed?.Invoke();
        }

        private void ResolveAll(out Object binding, out string reason)
        {
            binding = null;
            reason = TweenClipTexts.NO_TRACK;

            for (var i = 0; i < _clipAssets.Length; i++)
            {
                if (_resolve(_clipAssets[i], out var each, out var eachReason) == false)
                {
                    binding = null;
                    reason = eachReason;
                    return;
                }

                if (i > 0 && each != binding)
                {
                    binding = null;
                    reason = TweenClipTexts.MIXED_BINDINGS;
                    return;
                }

                binding = each;
                reason = string.Empty;
            }
        }

        public static bool TryResolve(Object clipAsset, out Object binding, out string reason)
            => TryResolve(
                  clipAsset
                , TimelineEditor.inspectedAsset
                , TimelineEditor.inspectedDirector
                , out binding
                , out reason
            );

        public static bool TryResolve(
              Object clipAsset
            , TimelineAsset timeline
            , PlayableDirector director
            , out Object binding
            , out string reason
        )
        {
            binding = null;

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
