using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using ApexionGame.UguiPlayable.Easing;

namespace ApexionGame.UguiPlayable.Editor.Easing
{
    /// <summary>
    /// Draws an <see cref="EaseSetting"/>: an ease dropdown, a curve field shown only for
    /// <see cref="Ease.Custom"/>, and a live curve preview. UI Toolkit only, so it works in any inspector.
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

        private const string CURVE_LABEL = "Curve";

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var ease = property.FindPropertyRelative(EASE);
            var curve = property.FindPropertyRelative(CURVE);

            var root = new VisualElement();
            root.AddToClassList(ROOT_USS_CLASS_NAME);
            EaseSettingStyleSheetPaths.AddTo(root);

            var easeField = new EnumField(property.displayName, Ease.Linear);
            easeField.AddToClassList(BaseField<System.Enum>.alignedFieldUssClassName);
            easeField.BindProperty(ease);

            var curveField = new CurveField(CURVE_LABEL);
            curveField.AddToClassList(BaseField<AnimationCurve>.alignedFieldUssClassName);
            curveField.BindProperty(curve);

            var preview = new EaseCurvePreview();
            preview.AddToClassList(PREVIEW_USS_CLASS_NAME);

            root.Add(easeField);
            root.Add(curveField);
            root.Add(preview);

            easeField.RegisterValueChangedCallback(evt => OnEaseChanged(evt, curve));
            root.TrackPropertyValue(ease, _ => Refresh(ease, curve, curveField, preview));
            root.TrackPropertyValue(curve, _ => Refresh(ease, curve, curveField, preview));

            Refresh(ease, curve, curveField, preview);
            return root;
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

        private static void Refresh(
              SerializedProperty ease
            , SerializedProperty curve
            , VisualElement curveField
            , EaseCurvePreview preview
        )
        {
            ease.serializedObject.UpdateIfRequiredOrScript();

            var isMixed = ease.hasMultipleDifferentValues;
            var easeValue = (Ease)ease.intValue;
            var isCustom = isMixed == false && easeValue == Ease.Custom;

            curveField.style.display = isCustom ? DisplayStyle.Flex : DisplayStyle.None;
            preview.style.display = isMixed ? DisplayStyle.None : DisplayStyle.Flex;

            if (isMixed)
            {
                return;
            }

            if (isCustom)
            {
                preview.SetEase(new EaseSetting(curve.animationCurveValue));
            }
            else
            {
                preview.SetEase(new EaseSetting(easeValue));
            }
        }

        private static bool HasCurve(AnimationCurve curve)
            => curve != null && curve.length > 0;
    }
}
