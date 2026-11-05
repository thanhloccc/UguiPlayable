using UnityEngine.UI;
using ApexionGame.UguiPlayable.Editor.Clips;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Editor.Timeline
{
    /// <summary>
    /// Detects a ScrollRect without a content, which has nothing to scroll.
    /// </summary>
    internal static class ScrollRectContentCheck
    {
        /// <summary>
        /// The error for <paramref name="binding"/> when it is a ScrollRect with no content, or an empty string.
        /// Allocation-free.
        /// </summary>
        public static string GetWarning(Object binding)
            => binding is ScrollRect scrollRect && scrollRect.content.IsInvalid()
                ? TweenClipTexts.SCROLL_RECT_NO_CONTENT
                : string.Empty;
    }
}
