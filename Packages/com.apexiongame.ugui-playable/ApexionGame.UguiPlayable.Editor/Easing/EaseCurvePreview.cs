using UnityEngine;
using UnityEngine.UIElements;
using ApexionGame.UguiPlayable.Easing;

namespace ApexionGame.UguiPlayable.Editor.Easing
{
    /// <summary>
    /// Draws an <see cref="EaseSetting"/> with <see cref="Painter2D"/>: a faint grid (quarters in time, 0 and 1 in
    /// value), the area under the curve, the curve itself with round caps, and dots at its start and end. The value
    /// range grows to keep overshooting eases (Back, Elastic) visible; the bands outside [0, 1] are tinted.
    /// </summary>
    internal sealed class EaseCurvePreview : VisualElement
    {
        public const string CURVE_COLOR_PROPERTY = "--ugp-easing-ease-setting-curve";
        public const string GRID_COLOR_PROPERTY = "--ugp-easing-ease-setting-grid";
        public const string BASELINE_COLOR_PROPERTY = "--ugp-easing-ease-setting-baseline";
        public const string OVERSHOOT_COLOR_PROPERTY = "--ugp-easing-ease-setting-overshoot";

        private const int SAMPLE_COUNT = 96;
        private const float LINE_WIDTH = 2f;
        private const float GRID_WIDTH = 1f;
        private const float DOT_RADIUS = 2.5f;
        private const float PADDING_X = 10f;
        private const float PADDING_Y = 8f;
        private const float FILL_ALPHA = 0.16f;

        private static readonly CustomStyleProperty<Color> s_curveColorProperty = new(CURVE_COLOR_PROPERTY);
        private static readonly CustomStyleProperty<Color> s_gridColorProperty = new(GRID_COLOR_PROPERTY);
        private static readonly CustomStyleProperty<Color> s_baselineColorProperty = new(BASELINE_COLOR_PROPERTY);
        private static readonly CustomStyleProperty<Color> s_overshootColorProperty = new(OVERSHOOT_COLOR_PROPERTY);

        private readonly float[] _samples = new float[SAMPLE_COUNT];

        private Color _curveColor = new(0.35f, 0.7f, 1f, 1f);
        private Color _gridColor = new(1f, 1f, 1f, 0.05f);
        private Color _baselineColor = new(1f, 1f, 1f, 0.14f);
        private Color _overshootColor = new(1f, 0.7f, 0.3f, 0.07f);
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
            var style = evt.customStyle;

            if (style.TryGetValue(s_curveColorProperty, out var curve))
            {
                _curveColor = curve;
            }

            if (style.TryGetValue(s_gridColorProperty, out var grid))
            {
                _gridColor = grid;
            }

            if (style.TryGetValue(s_baselineColorProperty, out var baseline))
            {
                _baselineColor = baseline;
            }

            if (style.TryGetValue(s_overshootColorProperty, out var overshoot))
            {
                _overshootColor = overshoot;
            }

            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            var area = GetPlotArea(contentRect);

            if (_hasSamples == false || area.width <= 0f || area.height <= 0f)
            {
                return;
            }

            GetRange(out var min, out var max);

            var painter = context.painter2D;
            var zeroY = ValueToY(0f, area, min, max);
            var oneY = ValueToY(1f, area, min, max);

            DrawOvershootBands(painter, area, zeroY, oneY);
            DrawGrid(painter, area, zeroY, oneY);
            DrawFill(painter, area, zeroY, min, max);
            DrawCurve(painter, area, min, max);
            DrawDot(painter, ToPoint(0, area, min, max));
            DrawDot(painter, ToPoint(SAMPLE_COUNT - 1, area, min, max));
        }

        private void DrawOvershootBands(Painter2D painter, Rect area, float zeroY, float oneY)
        {
            painter.fillColor = _overshootColor;

            if (oneY > area.yMin + 0.5f)
            {
                FillRect(painter, new Rect(area.xMin, area.yMin, area.width, oneY - area.yMin));
            }

            if (zeroY < area.yMax - 0.5f)
            {
                FillRect(painter, new Rect(area.xMin, zeroY, area.width, area.yMax - zeroY));
            }
        }

        private void DrawGrid(Painter2D painter, Rect area, float zeroY, float oneY)
        {
            painter.lineWidth = GRID_WIDTH;
            painter.lineCap = LineCap.Butt;
            painter.strokeColor = _gridColor;

            for (var quarter = 1; quarter < 4; quarter++)
            {
                var x = Mathf.Lerp(area.xMin, area.xMax, quarter * 0.25f);
                Line(painter, new Vector2(x, area.yMin), new Vector2(x, area.yMax));
            }

            var halfY = (zeroY + oneY) * 0.5f;
            Line(painter, new Vector2(area.xMin, halfY), new Vector2(area.xMax, halfY));

            painter.strokeColor = _baselineColor;
            Line(painter, new Vector2(area.xMin, zeroY), new Vector2(area.xMax, zeroY));
            Line(painter, new Vector2(area.xMin, oneY), new Vector2(area.xMax, oneY));
        }

        private void DrawFill(Painter2D painter, Rect area, float zeroY, float min, float max)
        {
            var fill = _curveColor;
            fill.a *= FILL_ALPHA;
            painter.fillColor = fill;

            painter.BeginPath();
            painter.MoveTo(new Vector2(area.xMin, zeroY));

            for (var i = 0; i < SAMPLE_COUNT; i++)
            {
                painter.LineTo(ToPoint(i, area, min, max));
            }

            painter.LineTo(new Vector2(area.xMax, zeroY));
            painter.ClosePath();
            painter.Fill(FillRule.NonZero);
        }

        private void DrawCurve(Painter2D painter, Rect area, float min, float max)
        {
            painter.lineWidth = LINE_WIDTH;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            painter.strokeColor = _curveColor;

            painter.BeginPath();
            painter.MoveTo(ToPoint(0, area, min, max));

            for (var i = 1; i < SAMPLE_COUNT; i++)
            {
                painter.LineTo(ToPoint(i, area, min, max));
            }

            painter.Stroke();
        }

        private void DrawDot(Painter2D painter, Vector2 center)
        {
            painter.fillColor = _curveColor;
            painter.BeginPath();
            painter.Arc(center, DOT_RADIUS, Angle.Degrees(0f), Angle.Degrees(360f), ArcDirection.Clockwise);
            painter.ClosePath();
            painter.Fill(FillRule.NonZero);
        }

        private static void Line(Painter2D painter, Vector2 from, Vector2 to)
        {
            painter.BeginPath();
            painter.MoveTo(from);
            painter.LineTo(to);
            painter.Stroke();
        }

        private static void FillRect(Painter2D painter, Rect rect)
        {
            painter.BeginPath();
            painter.MoveTo(new Vector2(rect.xMin, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMin));
            painter.LineTo(new Vector2(rect.xMax, rect.yMax));
            painter.LineTo(new Vector2(rect.xMin, rect.yMax));
            painter.ClosePath();
            painter.Fill(FillRule.NonZero);
        }

        private static Rect GetPlotArea(Rect rect)
            => new(
                  rect.xMin + PADDING_X
                , rect.yMin + PADDING_Y
                , rect.width - PADDING_X * 2f
                , rect.height - PADDING_Y * 2f
            );

        private void GetRange(out float min, out float max)
        {
            min = 0f;
            max = 1f;

            for (var i = 0; i < SAMPLE_COUNT; i++)
            {
                min = Mathf.Min(min, _samples[i]);
                max = Mathf.Max(max, _samples[i]);
            }
        }

        private Vector2 ToPoint(int index, Rect area, float min, float max)
        {
            var x = Mathf.Lerp(area.xMin, area.xMax, index / (float)(SAMPLE_COUNT - 1));
            return new Vector2(x, ValueToY(_samples[index], area, min, max));
        }

        private static float ValueToY(float value, Rect area, float min, float max)
            => area.yMax - Mathf.InverseLerp(min, max, value) * area.height;
    }
}
