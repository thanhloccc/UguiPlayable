using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Editor.Clips
{
    /// <summary>
    /// Shared USS class names and element factories of the tween clip inspector.
    /// </summary>
    internal static class TweenClipSections
    {
        public const string ROOT_USS_CLASS_NAME = "ugp-tween-clip";
        public const string HEADER_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__header";
        public const string HEADER_ICON_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__header-icon";
        public const string TITLE_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__title";
        public const string COUNT_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__count";
        public const string NOTE_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__note";

        public const string CARD_USS_CLASS_NAME = "ugp-tween-card";
        public const string CARD_DISABLED_USS_CLASS_NAME = CARD_USS_CLASS_NAME + "--disabled";
        public const string CARD_HEADER_USS_CLASS_NAME = CARD_USS_CLASS_NAME + "__header";
        public const string CARD_ICON_USS_CLASS_NAME = CARD_USS_CLASS_NAME + "__icon";
        public const string CARD_NAME_USS_CLASS_NAME = CARD_USS_CLASS_NAME + "__name";
        public const string CARD_BODY_USS_CLASS_NAME = CARD_USS_CLASS_NAME + "__body";
        public const string ROW_USS_CLASS_NAME = CARD_USS_CLASS_NAME + "__row";
        public const string ROW_LABEL_USS_CLASS_NAME = CARD_USS_CLASS_NAME + "__row-label";
        public const string FIELD_USS_CLASS_NAME = CARD_USS_CLASS_NAME + "__field";
        public const string ICON_BUTTON_USS_CLASS_NAME = CARD_USS_CLASS_NAME + "__icon-button";
        public const string ICON_BUTTON_ON_USS_CLASS_NAME = ICON_BUTTON_USS_CLASS_NAME + "--on";
        public const string AXIS_BUTTON_USS_CLASS_NAME = CARD_USS_CLASS_NAME + "__axis-button";
        public const string AXIS_BUTTON_ON_USS_CLASS_NAME = AXIS_BUTTON_USS_CLASS_NAME + "--on";
        public const string AXIS_BUTTON_MIXED_USS_CLASS_NAME = AXIS_BUTTON_USS_CLASS_NAME + "--mixed";
        public const string AXIS_GROUP_USS_CLASS_NAME = CARD_USS_CLASS_NAME + "__axis-group";
        public const string AXIS_BUTTON_X_USS_CLASS_NAME = AXIS_BUTTON_USS_CLASS_NAME + "--x";
        public const string AXIS_BUTTON_Y_USS_CLASS_NAME = AXIS_BUTTON_USS_CLASS_NAME + "--y";
        public const string AXIS_BUTTON_Z_USS_CLASS_NAME = AXIS_BUTTON_USS_CLASS_NAME + "--z";
        public const string AXIS_BUTTON_W_USS_CLASS_NAME = AXIS_BUTTON_USS_CLASS_NAME + "--w";
        public const string GRADIENT_USS_CLASS_NAME = CARD_USS_CLASS_NAME + "__gradient";
        public const string GRADIENT_ROW_USS_CLASS_NAME = CARD_USS_CLASS_NAME + "__gradient-row";
        public const string GRADIENT_FIELD_USS_CLASS_NAME = CARD_USS_CLASS_NAME + "__gradient-field";
        public const string ROW_TOP_USS_CLASS_NAME = CARD_USS_CLASS_NAME + "__row--top";
        public const string ROW_LABEL_TEXT_USS_CLASS_NAME = CARD_USS_CLASS_NAME + "__row-label--text";

        // On a card whose row labels are longer than From/To (Strength, Frequency, Amplitude…).
        public const string CARD_WIDE_LABELS_USS_CLASS_NAME = CARD_USS_CLASS_NAME + "--wide-labels";
        public const string TEXT_FIELD_USS_CLASS_NAME = CARD_USS_CLASS_NAME + "__text-field";
        public const string PERCENT_USS_CLASS_NAME = CARD_USS_CLASS_NAME + "__percent";

        private const string EXPAND_KEY_PREFIX = "ugp.tween-clip.expanded";
        private const string UNSAVED = "unsaved";

        public static VisualElement CreateRoot()
        {
            var root = new VisualElement();
            root.AddToClassList(ROOT_USS_CLASS_NAME);
            TweenClipStyleSheetPaths.AddTo(root);
            return root;
        }

        public static Image CreateIcon(Texture2D texture, string className)
        {
            var image = new Image {
                image = texture,
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore,
            };

            image.AddToClassList(className);
            return image;
        }

        public static Label CreateNote(string text)
        {
            var label = new Label(text);
            label.AddToClassList(NOTE_USS_CLASS_NAME);
            return label;
        }

        /// <summary>
        /// A body row: a fixed-width label, then whatever the caller adds.
        /// </summary>
        public static VisualElement CreateRow(string label)
        {
            var row = new VisualElement();
            row.AddToClassList(ROW_USS_CLASS_NAME);

            var text = new Label(label);
            text.AddToClassList(ROW_LABEL_USS_CLASS_NAME);
            row.Add(text);
            return row;
        }

        /// <summary>
        /// A small button that shows only an image.
        /// </summary>
        public static Button CreateIconButton(Texture2D icon, string tooltip, Action onClick, out Image image)
        {
            var button = new Button(onClick) { tooltip = tooltip };
            button.AddToClassList(ICON_BUTTON_USS_CLASS_NAME);

            image = CreateIcon(icon, CARD_ICON_USS_CLASS_NAME);
            button.Add(image);
            return button;
        }

        /// <summary>
        /// The <see cref="SessionState"/> key of a card's expand state: the asset GUID and file id of the clip plus
        /// the channel's property path.
        /// </summary>
        public static string GetExpandKey(Object target, string propertyPath)
        {
            if (target == null)
            {
                return $"{EXPAND_KEY_PREFIX}.{UNSAVED}.{propertyPath}";
            }

            if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(target, out var guid, out long localId))
            {
                return $"{EXPAND_KEY_PREFIX}.{guid}.{localId}.{propertyPath}";
            }

            // Not a persistent asset: the instance id keeps two unsaved clips from sharing one expand state.
            return $"{EXPAND_KEY_PREFIX}.{UNSAVED}.{target.GetInstanceID()}.{propertyPath}";
        }
    }
}
