using UnityEngine.UIElements;

namespace ApexionGame.UguiPlayable.Editor.Presets
{
    /// <summary>
    /// Paths and cached loads of the <see cref="PresetPicker"/> style sheets.
    /// </summary>
    internal static class PresetPickerStyleSheetPaths
    {
        public const string ROOT = "Packages/com.apexiongame.ugui-playable/ApexionGame.UguiPlayable.Editor/Presets";
        public const string USS_PATH = ROOT + "/StyleSheets/PresetPicker.uss";
        public const string DARK_USS_PATH = ROOT + "/StyleSheets/PresetPickerDark.uss";
        public const string LIGHT_USS_PATH = ROOT + "/StyleSheets/PresetPickerLight.uss";

        private static StyleSheet s_styleSheet;
        private static StyleSheet s_darkStyleSheet;
        private static StyleSheet s_lightStyleSheet;

        public static StyleSheet StyleSheet => EditorAssets.GetOrLoadAsset(ref s_styleSheet, USS_PATH);

        public static StyleSheet ThemeStyleSheet => EditorAssets.GetOrLoadStyleSheet(
              ref s_darkStyleSheet, DARK_USS_PATH
            , ref s_lightStyleSheet, LIGHT_USS_PATH
        );

        /// <summary>
        /// Adds the theme sheet first, then the main sheet, to <paramref name="element"/>.
        /// </summary>
        public static void AddTo(VisualElement element)
        {
            element.styleSheets.Add(ThemeStyleSheet);
            element.styleSheets.Add(StyleSheet);
        }
    }
}
