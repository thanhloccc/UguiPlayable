using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Editor.Timeline
{
    /// <summary>
    /// The shared Timeline track hooks: the track icon, and an optional binding warning that fills
    /// <see cref="TrackDrawOptions.errorText"/> when Timeline itself found no binding error.
    /// </summary>
    internal abstract class TweenTrackEditor : TrackEditor
    {
        /// <summary>The icon shown on the track header.</summary>
        protected abstract Texture2D Icon { get; }

        public override TrackDrawOptions GetTrackOptions(TrackAsset track, Object binding)
        {
            var options = base.GetTrackOptions(track, binding);
            options.icon = Icon;

            if (string.IsNullOrEmpty(options.errorText))
            {
                options.errorText = GetBindingWarning(binding);
            }

            return options;
        }

        /// <summary>
        /// The warning for <paramref name="binding"/>, or an empty string. Runs on every track draw: keep it cheap.
        /// </summary>
        protected virtual string GetBindingWarning(Object binding)
            => string.Empty;
    }
}
