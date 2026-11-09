using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using ApexionGame.UguiPlayable.Easing;
using ApexionGame.UguiPlayable.Editor.Common;

namespace ApexionGame.UguiPlayable.Editor.Easing
{
    /// <summary>
    /// Draws an <see cref="EaseSetting"/>: an ease dropdown, a curve field shown only for
    /// <see cref="Ease.Custom"/>, a Customize switch with Strength and Period for the Back, Elastic and Bounce
    /// families, and a live curve preview. UI Toolkit only, so it works in any inspector.
    /// </summary>
    [CustomPropertyDrawer(typeof(EaseSetting))]
    internal sealed class EaseSettingDrawer : PropertyDrawer
    {
        public const string ROOT_USS_CLASS_NAME = "ugp-easing-ease-setting";
        public const string PREVIEW_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__preview";

        // EaseSetting keeps these fields private, so nameof is not available to editor code.
        // Their names are serialized user data and cannot change without [FormerlySerializedAs].
        private const string EASE = "_ease";
        private const string CURVE = "_curve";

        private const string CUSTOMIZE = "_customize";
        private const string STRENGTH = "_strength";
        private const string PERIOD = "_period";

        private const string CURVE_LABEL = "Curve";
        private const string CUSTOMIZE_LABEL = "Customize";
        private const string OVERSHOOT_LABEL = "Overshoot";
        private const string AMPLITUDE_LABEL = "Amplitude";
        private const string BOUNCINESS_LABEL = "Bounciness";
        private const string PERIOD_LABEL = "Period";

        private const string CUSTOMIZE_TOOLTIP
            = "Change the parameters of this ease. With the switch off, the ease is the unchanged preset.";

        private const string PERIOD_TOOLTIP
            = "The length of one oscillation, as a fraction of the duration (of each half for In-Out). "
            + "Smaller is faster.";

        private const float DEFAULT_STRENGTH = 1f;
        private const float DEFAULT_PERIOD = 0.3f;
        private const float DEFAULT_IN_OUT_PERIOD = 0.45f;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var ease = property.FindPropertyRelative(EASE);
            var curve = property.FindPropertyRelative(CURVE);
            var customize = property.FindPropertyRelative(CUSTOMIZE);
            var strength = property.FindPropertyRelative(STRENGTH);
            var period = property.FindPropertyRelative(PERIOD);

            var root = new VisualElement();
            root.AddToClassList(ROOT_USS_CLASS_NAME);
            EaseSettingStyleSheetPaths.AddTo(root);

            var easeField = new EnumField(property.displayName, Ease.Linear);
            easeField.AddToClassList(BaseField<System.Enum>.alignedFieldUssClassName);
            easeField.BindProperty(ease);

            var curveField = new CurveField(CURVE_LABEL);
            curveField.AddToClassList(BaseField<AnimationCurve>.alignedFieldUssClassName);
            curveField.BindProperty(curve);

            var customizeToggle = new Toggle(CUSTOMIZE_LABEL) { tooltip = CUSTOMIZE_TOOLTIP }.WithSwitchStyle();
            customizeToggle.BindProperty(customize);

            var strengthField = new FloatField(OVERSHOOT_LABEL);
            strengthField.AddToClassList(BaseField<float>.alignedFieldUssClassName);
            strengthField.BindProperty(strength);

            var periodField = new FloatField(PERIOD_LABEL) { tooltip = PERIOD_TOOLTIP };
            periodField.AddToClassList(BaseField<float>.alignedFieldUssClassName);
            periodField.BindProperty(period);

            var preview = new EaseCurvePreview();
            preview.AddToClassList(PREVIEW_USS_CLASS_NAME);

            root.Add(easeField);
            root.Add(curveField);
            root.Add(customizeToggle);
            root.Add(strengthField);
            root.Add(periodField);
            root.Add(preview);

            var view = new View(
                  ease
                , curve
                , customize
                , strength
                , period
                , curveField
                , customizeToggle
                , strengthField
                , periodField
                , preview
            );

            easeField.RegisterValueChangedCallback(evt => OnEaseChanged(evt, curve));
            customizeToggle.RegisterValueChangedCallback(evt => OnCustomizeChanged(evt, ease, strength, period));
            root.TrackPropertyValue(ease, _ => Refresh(view));
            root.TrackPropertyValue(curve, _ => Refresh(view));
            root.TrackPropertyValue(customize, _ => Refresh(view));
            root.TrackPropertyValue(strength, _ => Refresh(view));
            root.TrackPropertyValue(period, _ => Refresh(view));

            Refresh(view);
            return root;
        }

        // Turning Customize on starts from the preset's own parameters, so the curve does not jump. They are written
        // through the SerializedProperty, in the same Undo group as the switch.
        private static void OnCustomizeChanged(
              ChangeEvent<bool> evt
            , SerializedProperty ease
            , SerializedProperty strength
            , SerializedProperty period
        )
        {
            if (evt.newValue == false)
            {
                return;
            }

            var serializedObject = ease.serializedObject;
            serializedObject.Update();

            var inOutElastic = (Ease)ease.intValue == Ease.InOutElastic;

            strength.floatValue = DEFAULT_STRENGTH;
            period.floatValue = inOutElastic ? DEFAULT_IN_OUT_PERIOD : DEFAULT_PERIOD;
            serializedObject.ApplyModifiedProperties();
        }

        // Switching to Custom with no curve gives the user a usable ease-in-out curve to edit. It is written through
        // the SerializedProperty, so it lands in the same Undo group as the ease change.
        private static void OnEaseChanged(ChangeEvent<System.Enum> evt, SerializedProperty curve)
        {
            if (evt.newValue is not Ease newEase || newEase != Ease.Custom)
            {
                return;
            }

            var serializedObject = curve.serializedObject;
            serializedObject.Update();

            if (HasCurve(curve.animationCurveValue))
            {
                return;
            }

            curve.animationCurveValue = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
            serializedObject.ApplyModifiedProperties();
        }

        private static void Refresh(View view)
        {
            var ease = view.Ease;

            ease.serializedObject.UpdateIfRequiredOrScript();

            var isMixed = ease.hasMultipleDifferentValues;
            var easeValue = (Ease)ease.intValue;
            var isCustom = isMixed == false && easeValue == Ease.Custom;
            var isParametric = isMixed == false && EaseSetting.IsParametric(easeValue);
            var customize = view.Customize.boolValue && view.Customize.hasMultipleDifferentValues == false;
            var showParameters = isParametric && customize;

            view.CurveField.style.display = isCustom ? DisplayStyle.Flex : DisplayStyle.None;
            view.CustomizeToggle.style.display = isParametric ? DisplayStyle.Flex : DisplayStyle.None;
            view.StrengthField.style.display = showParameters ? DisplayStyle.Flex : DisplayStyle.None;
            view.PeriodField.style.display = showParameters && IsElastic(easeValue)
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            view.Preview.style.display = isMixed ? DisplayStyle.None : DisplayStyle.Flex;

            if (isMixed)
            {
                return;
            }

            if (isParametric)
            {
                view.StrengthField.label = GetStrengthLabel(easeValue);
            }

            if (isCustom)
            {
                view.Preview.SetEase(new EaseSetting(view.Curve.animationCurveValue));
            }
            else if (showParameters)
            {
                view.Preview.SetEase(
                    new EaseSetting(easeValue, view.Strength.floatValue, view.Period.floatValue)
                );
            }
            else
            {
                view.Preview.SetEase(new EaseSetting(easeValue));
            }
        }

        private static bool IsElastic(Ease ease)
            => ease >= Ease.InElastic && ease <= Ease.InOutElastic;

        private static string GetStrengthLabel(Ease ease)
        {
            if (IsElastic(ease))
            {
                return AMPLITUDE_LABEL;
            }

            return ease >= Ease.InBack && ease <= Ease.InOutBack ? OVERSHOOT_LABEL : BOUNCINESS_LABEL;
        }

        private static bool HasCurve(AnimationCurve curve)
            => curve != null && curve.length > 0;

        private sealed class View
        {
            public readonly SerializedProperty Ease;
            public readonly SerializedProperty Curve;
            public readonly SerializedProperty Customize;
            public readonly SerializedProperty Strength;
            public readonly SerializedProperty Period;
            public readonly VisualElement CurveField;
            public readonly VisualElement CustomizeToggle;
            public readonly FloatField StrengthField;
            public readonly VisualElement PeriodField;
            public readonly EaseCurvePreview Preview;

            public View(
                  SerializedProperty ease
                , SerializedProperty curve
                , SerializedProperty customize
                , SerializedProperty strength
                , SerializedProperty period
                , VisualElement curveField
                , VisualElement customizeToggle
                , FloatField strengthField
                , VisualElement periodField
                , EaseCurvePreview preview
            )
            {
                Ease = ease;
                Curve = curve;
                Customize = customize;
                Strength = strength;
                Period = period;
                CurveField = curveField;
                CustomizeToggle = customizeToggle;
                StrengthField = strengthField;
                PeriodField = periodField;
                Preview = preview;
            }
        }
    }
}
