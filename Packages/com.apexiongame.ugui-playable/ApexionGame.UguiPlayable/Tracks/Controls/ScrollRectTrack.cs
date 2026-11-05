using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Controls
{
    /// <summary>
    /// Tweens a <see cref="ScrollRect"/>: its normalized scroll position.
    /// </summary>
    /// <remarks>
    /// The normalized position is not serialized, it is computed from the content. For the edit-mode preview the track
    /// registers the content's anchored position and the value and size of the scrollbars, which the scroll rect
    /// writes.
    /// </remarks>
    [TrackColor(0.30f, 0.72f, 0.82f)]
    [TrackClipType(typeof(ScrollRectClip))]
    [TrackBindingType(typeof(ScrollRect))]
#if UNITY_EDITOR
    [System.ComponentModel.DisplayName("UGUI Playable/Controls/Scroll Rect")]
#endif
    public sealed class ScrollRectTrack : TweenTrack<ScrollRect, ScrollRectBehaviour, ScrollRectMixer>
    {
        private const string CONTENT_PROPERTY = "m_AnchoredPosition";
        private const string SCROLLBAR_VALUE_PROPERTY = "m_Value";
        private const string SCROLLBAR_SIZE_PROPERTY = "m_Size";

        /// <inheritdoc/>
        protected override void GatherProperty(ScrollRect binding, string name, IPropertyCollector driver)
        {
            if (name != ScrollRectBehaviour.NORMALIZED_POSITION_PROPERTY)
            {
                base.GatherProperty(binding, name, driver);
                return;
            }

            var content = binding.content;

            if (content.IsValid())
            {
                driver.AddFromName(content, CONTENT_PROPERTY);
            }

            GatherScrollbar(binding.horizontalScrollbar, driver);
            GatherScrollbar(binding.verticalScrollbar, driver);
        }

        private static void GatherScrollbar(Scrollbar scrollbar, IPropertyCollector driver)
        {
            if (scrollbar.IsInvalid())
            {
                return;
            }

            driver.AddFromName(scrollbar, SCROLLBAR_VALUE_PROPERTY);
            driver.AddFromName(scrollbar, SCROLLBAR_SIZE_PROPERTY);
        }
    }
}
