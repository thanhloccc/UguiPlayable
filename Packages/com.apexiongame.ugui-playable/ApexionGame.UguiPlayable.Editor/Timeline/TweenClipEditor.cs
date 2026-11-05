using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Editor.Clips;

namespace ApexionGame.UguiPlayable.Editor.Timeline
{
    /// <summary>
    /// The shared Timeline clip hooks: a default display name, the track icon and an error when no channel is on. A
    /// concrete editor reads its clip's template through typed accessors, because <see cref="GetClipOptions"/> runs
    /// on every clip draw: no <c>SerializedObject</c>, no allocation per call. The clip background is left to
    /// Timeline (Owner 2026-11-05: no curve in the clip).
    /// </summary>
    /// <typeparam name="TClip">The clip asset type this editor draws.</typeparam>
    internal abstract class TweenClipEditor<TClip> : ClipEditor
        where TClip : PlayableAsset
    {
        private Texture2D[] _icons;
        private bool _iconsDark;

        /// <summary>The name a newly created clip gets, normally the track's nice name.</summary>
        protected abstract string DefaultName { get; }

        /// <summary>The icon shown on the clip.</summary>
        protected abstract Texture2D Icon { get; }

        /// <summary>The behaviour template of <paramref name="clip"/>, which answers the channel queries.</summary>
        protected abstract TweenBehaviour GetTemplate(TClip clip);

        public override void OnCreate(TimelineClip clip, TrackAsset track, TimelineClip clonedFrom)
        {
            if (clonedFrom == null)
            {
                clip.displayName = DefaultName;
            }
        }

        public override ClipDrawOptions GetClipOptions(TimelineClip clip)
        {
            var options = base.GetClipOptions(clip);
            options.icons = GetIcons();

            var hasError = string.IsNullOrEmpty(options.errorText) == false;

            if (hasError == false && clip.asset is TClip asset && HasEnabledChannel(asset) == false)
            {
                options.errorText = TweenClipTexts.NO_CHANNEL_ENABLED;
            }

            return options;
        }

        private bool HasEnabledChannel(TClip clip)
        {
            var template = GetTemplate(clip);

            return template != null && template.HasEnabledChannel();
        }

        // One cached array, rebuilt only when the editor skin changes or the texture was destroyed.
        private Texture2D[] GetIcons()
        {
            var dark = EditorAssets.IsDark;

            if (_icons == null || _iconsDark != dark || _icons[0] == null)
            {
                _icons = new[] { Icon };
                _iconsDark = dark;
            }

            return _icons;
        }
    }
}
