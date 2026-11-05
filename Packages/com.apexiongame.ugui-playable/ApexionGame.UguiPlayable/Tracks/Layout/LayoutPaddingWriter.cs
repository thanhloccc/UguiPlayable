using UnityEngine;
using UnityEngine.UI;

namespace ApexionGame.UguiPlayable.Tracks.Layout
{
    /// <summary>
    /// Reads and writes <see cref="LayoutGroup.padding"/> as a <see cref="Vector4"/> (left, right, top, bottom).
    /// </summary>
    /// <remarks>
    /// Assigning a new <see cref="RectOffset"/> every frame would allocate, so the write sets the four ints on the
    /// instance the group already has, and marks the layout for rebuild (pooled, no GC after warm-up) only when a value
    /// actually changed.
    /// </remarks>
    internal static class LayoutPaddingWriter
    {
        public static Vector4 Read(LayoutGroup group)
        {
            var padding = group.padding;

            return padding == null
                ? default
                : new Vector4(padding.left, padding.right, padding.top, padding.bottom);
        }

        public static void Write(LayoutGroup group, Vector4 value)
        {
            var padding = group.padding;

            if (padding == null)
            {
                return;
            }

            var left = Mathf.RoundToInt(value.x);
            var right = Mathf.RoundToInt(value.y);
            var top = Mathf.RoundToInt(value.z);
            var bottom = Mathf.RoundToInt(value.w);

            if (padding.left == left
                && padding.right == right
                && padding.top == top
                && padding.bottom == bottom)
            {
                return;
            }

            padding.left = left;
            padding.right = right;
            padding.top = top;
            padding.bottom = bottom;

            if (group.transform is RectTransform rect)
            {
                LayoutRebuilder.MarkLayoutForRebuild(rect);
            }
        }
    }
}
