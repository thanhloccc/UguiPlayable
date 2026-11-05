using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Editor.Clips;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Editor.Timeline
{
    /// <summary>
    /// Detects a RectTransform whose position a LayoutGroup rewrites on every layout rebuild.
    /// </summary>
    internal static class RectTransformLayoutCheck
    {
        // Reused by the main thread only: Timeline calls these from its draw hooks, so no allocation per call.
        private static readonly List<ILayoutGroup> s_groups = new();
        private static readonly List<ILayoutSelfController> s_controllers = new();

        /// <summary>
        /// The warning for <paramref name="binding"/>: the layout group one, then the size fitter one, or an empty
        /// string.
        /// </summary>
        public static string GetWarning(Object binding)
        {
            if (IsDrivenByLayoutGroup(binding))
            {
                return TweenClipTexts.LAYOUT_WARNING;
            }

            return IsSizeDrivenByFitter(binding) ? TweenClipTexts.SIZE_FITTER_WARNING : string.Empty;
        }

        /// <summary>
        /// Whether <paramref name="binding"/> is a RectTransform with an enabled <see cref="ILayoutSelfController"/>
        /// (<c>ContentSizeFitter</c>, <c>AspectRatioFitter</c>) that rewrites its size on layout rebuild.
        /// </summary>
        public static bool IsSizeDrivenByFitter(Object binding)
        {
            if (binding is not RectTransform rect)
            {
                return false;
            }

            rect.GetComponents(s_controllers);

            try
            {
                for (var i = 0; i < s_controllers.Count; i++)
                {
                    if (s_controllers[i] is Behaviour { isActiveAndEnabled: true })
                    {
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                s_controllers.Clear();
            }
        }

        /// <summary>
        /// Whether <paramref name="binding"/> is a RectTransform whose parent has an enabled layout group, unless the
        /// object has a layout ignorer (a <c>LayoutElement</c>) with <c>ignoreLayout</c> on.
        /// </summary>
        public static bool IsDrivenByLayoutGroup(Object binding)
        {
            if (binding is not RectTransform rect || rect.parent == null)
            {
                return false;
            }

            if (HasEnabledLayoutGroup(rect.parent) == false)
            {
                return false;
            }

            return IsIgnoringLayout(rect) == false;
        }

        private static bool HasEnabledLayoutGroup(Transform parent)
        {
            // GetComponents<ILayoutGroup> finds every LayoutGroup subclass, including user-written ones. ScrollRect
            // implements ILayoutGroup but only lays out its own content, never the children it parents.
            parent.GetComponents(s_groups);

            try
            {
                for (var i = 0; i < s_groups.Count; i++)
                {
                    if (s_groups[i] is Behaviour { isActiveAndEnabled: true } and not ScrollRect)
                    {
                        return true;
                    }
                }

                return false;
            }
            finally
            {
                s_groups.Clear();
            }
        }

        private static bool IsIgnoringLayout(Component rect)
        {
            return rect.TryGetComponent<ILayoutIgnorer>(out var ignorer) && ignorer.ignoreLayout;
        }
    }
}
