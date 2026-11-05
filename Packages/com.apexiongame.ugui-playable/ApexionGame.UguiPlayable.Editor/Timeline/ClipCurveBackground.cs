using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;
using ApexionGame.UguiPlayable.Easing;

namespace ApexionGame.UguiPlayable.Editor.Timeline
{
    /// <summary>
    /// Draws an ease curve as a faint polyline in a Timeline clip's background. This is the only IMGUI file of the
    /// package: <see cref="ClipEditor.DrawBackground"/> is an immediate-mode hook, so it gets drawing calls only, no
    /// UI Toolkit.
    /// </summary>
    internal static class ClipCurveBackground
    {
        private const int MAX_SEGMENTS = 64;
        private const float MIN_WIDTH = 8f;
        private const float SEGMENT_WIDTH = 4f;
        private const float LINE_THICKNESS = 2f;
        private const float PADDING_Y = 4f;
        private const float PADDING_RATIO = 0.12f;
        private const float ALPHA = 0.4f;

        private static readonly Color s_color = new(1f, 1f, 1f, ALPHA);

        /// <summary>
        /// Draws <paramref name="ease"/> across <paramref name="region"/>. The region covers
        /// [<see cref="ClipBackgroundRegion.startTime"/>, <see cref="ClipBackgroundRegion.endTime"/>] of the clip, so a
        /// clip scrolled partly off screen shows the matching part of the curve. Region times map to progress the same way
        /// the runtime does: <c>(time - clipIn) / (duration * timeScale)</c>.
        /// </summary>
        public static void Draw(TimelineClip clip, ClipBackgroundRegion region, EaseSetting ease)
        {
            var rect = region.position;
            var length = clip.duration * clip.timeScale;

            if (rect.width < MIN_WIDTH || rect.height <= PADDING_Y * 2f || length <= 0d)
            {
                return;
            }

            var spanTime = region.endTime - region.startTime;

            if (spanTime <= 0d)
            {
                return;
            }

            var segments = Mathf.Clamp(Mathf.RoundToInt(rect.width / SEGMENT_WIDTH), 2, MAX_SEGMENTS);
            var startProgress = (float)((region.startTime - clip.clipIn) / length);
            var progressSpan = (float)(spanTime / length);

            GetRange(ease, startProgress, progressSpan, segments, out var min, out var max);

            var previousY = ToY(ease.Evaluate(Mathf.Clamp01(startProgress)), rect, min, max);
            var stepX = rect.width / segments;

            for (var i = 1; i <= segments; i++)
            {
                var progress = Mathf.Clamp01(startProgress + progressSpan * i / segments);
                var y = ToY(ease.Evaluate(progress), rect, min, max);
                var top = Mathf.Min(previousY, y);
                var height = Mathf.Abs(y - previousY) + LINE_THICKNESS;
                var x = rect.x + stepX * (i - 1);

                EditorGUI.DrawRect(new Rect(x, top - LINE_THICKNESS * 0.5f, stepX + 1f, height), s_color);
                previousY = y;
            }
        }

        private static void GetRange(
              EaseSetting ease
            , float startProgress
            , float progressSpan
            , int segments
            , out float min
            , out float max
        )
        {
            // 0 and 1 are always inside the range so a flat ease sits on a baseline, and overshooting eases (Back,
            // Elastic) stay visible.
            min = 0f;
            max = 1f;

            for (var i = 0; i <= segments; i++)
            {
                var value = ease.Evaluate(Mathf.Clamp01(startProgress + progressSpan * i / segments));
                min = Mathf.Min(min, value);
                max = Mathf.Max(max, value);
            }

            var padding = (max - min) * PADDING_RATIO;
            min -= padding;
            max += padding;
        }

        private static float ToY(float value, Rect rect, float min, float max)
            => rect.yMax - PADDING_Y - Mathf.InverseLerp(min, max, value) * (rect.height - PADDING_Y * 2f);
    }
}
