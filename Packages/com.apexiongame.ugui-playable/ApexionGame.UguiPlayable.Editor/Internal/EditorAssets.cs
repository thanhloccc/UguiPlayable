// Ported from EncosyTower 0.1.9-preview.5: EncosyTower.Core/Editor/EditorAPI.cs

using UnityEditor;
using UnityEngine.UIElements;

// Declared in the assembly root namespace, like the runtime assembly's Internal/ helpers.
namespace ApexionGame.UguiPlayable.Editor
{
    /// <summary>
    /// Loads and caches required editor assets and theme style sheets. A missing asset throws with its path.
    /// </summary>
    internal static class EditorAssets
    {
        public static bool IsDark => EditorGUIUtility.isProSkin;

        /// <summary>
        /// Returns the cached style sheet for the current editor skin, loading it from <paramref name="darkPath"/>
        /// or <paramref name="lightPath"/> when it is not cached or was destroyed.
        /// </summary>
        public static StyleSheet GetOrLoadStyleSheet(
              ref StyleSheet darkField, string darkPath
            , ref StyleSheet lightField, string lightPath
        )
        {
            ref var field = ref IsDark ? ref darkField : ref lightField;
            var path = IsDark ? darkPath : lightPath;

            if (field.IsInvalid())
            {
                field = LoadAsset<StyleSheet>(path);
            }

            return field;
        }

        /// <summary>
        /// Returns <paramref name="field"/> when it is still alive, otherwise loads the asset at
        /// <paramref name="path"/> into it.
        /// </summary>
        public static T GetOrLoadAsset<T>(ref T field, string path)
            where T : UnityEngine.Object
        {
            if (field.IsInvalid())
            {
                field = LoadAsset<T>(path);
            }

            return field;
        }

        /// <summary>
        /// Loads the style sheet for the current editor skin from <paramref name="darkPath"/> or
        /// <paramref name="lightPath"/>.
        /// </summary>
        public static StyleSheet LoadStyleSheet(string darkPath, string lightPath)
        {
            ThrowHelper.ThrowIfNullOrEmpty(darkPath, nameof(darkPath));
            ThrowHelper.ThrowIfNullOrEmpty(lightPath, nameof(lightPath));
            return LoadAsset<StyleSheet>(IsDark ? darkPath : lightPath);
        }

        /// <summary>
        /// Loads the asset at <paramref name="path"/>. Throws when it does not exist.
        /// </summary>
        public static T LoadAsset<T>(string path)
            where T : UnityEngine.Object
        {
            ThrowHelper.ThrowIfNullOrEmpty(path, nameof(path));

            var asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset.IsInvalid())
            {
                ThrowHelper.ThrowIfAssetNotFound(false, path);
            }

            return asset;
        }
    }
}
