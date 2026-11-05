using UnityEngine.UIElements;

namespace ApexionGame.UguiPlayable.Editor.Clips
{
    /// <summary>
    /// Paths and cached loads of the tween clip inspector style sheets.
    /// </summary>
    internal static class TweenClipStyleSheetPaths
    {
        public const string ROOT = "Packages/com.apexiongame.ugui-playable/ApexionGame.UguiPlayable.Editor/Clips";
        public const string USS_PATH = ROOT + "/StyleSheets/TweenClip.uss";
        public const string DARK_USS_PATH = ROOT + "/StyleSheets/TweenClipDark.uss";
        public const string LIGHT_USS_PATH = ROOT + "/StyleSheets/TweenClipLight.uss";

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
