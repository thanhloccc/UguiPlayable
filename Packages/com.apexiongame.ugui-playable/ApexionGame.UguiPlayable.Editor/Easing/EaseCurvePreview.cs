using UnityEngine;
using UnityEngine.UIElements;
using ApexionGame.UguiPlayable.Easing;

namespace ApexionGame.UguiPlayable.Editor.Easing
{
    /// <summary>
    /// Draws an <see cref="EaseSetting"/> as a curve with <see cref="Painter2D"/>. The vertical range grows to
    /// keep overshooting eases (Back, Elastic) visible, and faint baselines mark 0 and 1.
    /// </summary>
    internal sealed class EaseCurvePreview : VisualElement
    {
        public const string CURVE_COLOR_PROPERTY = "--ugp-easing-ease-setting-curve";
        public const string BASELINE_COLOR_PROPERTY = "--ugp-easing-ease-setting-baseline";

        private const int SAMPLE_COUNT = 64;
        private const float LINE_WIDTH = 1.5f;
        private const float BASELINE_WIDTH = 1f;
        private const float PADDING_X = 6f;
        private const float PADDING_RATIO_Y = 0.12f;

        private static readonly CustomStyleProperty<Color> s_curveColorProperty = new(CURVE_COLOR_PROPERTY);
        private static readonly CustomStyleProperty<Color> s_baselineColorProperty = new(BASELINE_COLOR_PROPERTY);

        private readonly float[] _samples = new float[SAMPLE_COUNT];

        private Color _curveColor = new(0.35f, 0.7f, 1f, 1f);
        private Color _baselineColor = new(0.5f, 0.5f, 0.5f, 0.5f);
        private bool _hasSamples;

        public EaseCurvePreview()
        {
            pickingMode = PickingMode.Ignore;
            generateVisualContent += OnGenerateVisualContent;
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        /// <summary>
        /// Samples <paramref name="setting"/> and repaints.
        /// </summary>
        public void SetEase(EaseSetting setting)
        {
            for (var i = 0; i < SAMPLE_COUNT; i++)
            {
                _samples[i] = setting.Evaluate(i / (float)(SAMPLE_COUNT - 1));
            }

            _hasSamples = true;
            MarkDirtyRepaint();
        }

        private void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (evt.customStyle.TryGetValue(s_curveColorProperty, out var curve))
            {
                _curveColor = curve;
            }

            if (evt.customStyle.TryGetValue(s_baselineColorProperty, out var baseline))
            {
                _baselineColor = baseline;
            }

            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            var rect = contentRect;

            if (_hasSamples == false || rect.width <= PADDING_X * 2f || rect.height <= 0f)
            {
                return;
            }

            GetRange(out var min, out var max);

            var painter = context.painter2D;

            painter.lineWidth = BASELINE_WIDTH;
            painter.strokeColor = _baselineColor;
            DrawBaseline(painter, rect, 0f, min, max);
            DrawBaseline(painter, rect, 1f, min, max);

            painter.lineWidth = LINE_WIDTH;
            painter.strokeColor = _curveColor;
            painter.lineJoin = LineJoin.Round;
            painter.BeginPath();
            painter.MoveTo(ToLocal(0, _samples[0], rect, min, max));

            for (var i = 1; i < SAMPLE_COUNT; i++)
            {
                painter.LineTo(ToLocal(i, _samples[i], rect, min, max));
            }

            painter.Stroke();
        }

        private static void DrawBaseline(Painter2D painter, Rect rect, float value, float min, float max)
        {
            var y = ValueToY(value, rect, min, max);

            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin + PADDING_X, y));
            painter.LineTo(new Vector2(rect.xMax - PADDING_X, y));
            painter.Stroke();
        }

        private void GetRange(out float min, out float max)
        {
            min = 0f;
            max = 1f;

            for (var i = 0; i < SAMPLE_COUNT; i++)
            {
                min = Mathf.Min(min, _samples[i]);
                max = Mathf.Max(max, _samples[i]);
            }

            var padding = (max - min) * PADDING_RATIO_Y;
            min -= padding;
            max += padding;
        }

        private static Vector2 ToLocal(int index, float value, Rect rect, float min, float max)
        {
            var t = index / (float)(SAMPLE_COUNT - 1);
            var x = Mathf.Lerp(rect.xMin + PADDING_X, rect.xMax - PADDING_X, t);
            return new Vector2(x, ValueToY(value, rect, min, max));
        }

        private static float ValueToY(float value, Rect rect, float min, float max)
            => rect.yMax - Mathf.InverseLerp(min, max, value) * rect.height;
    }
}
