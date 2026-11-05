using System;
using System.Collections.Generic;
using UnityEngine;

namespace ApexionGame.UguiPlayable.Editor.Common
{
    /// <summary>
    /// Owns the paths and cached textures of the package's editor icons (Lucide, ISC; see
    /// THIRD-PARTY-NOTICES.md). Each icon has <c>name-dark.png</c> and <c>name-light.png</c> at 16 px, picked by the
    /// editor skin.
    /// </summary>
    /// <remarks>
    /// The <c>@2x</c> (32 px) files sit next to them for later use. They are not loaded here: the AssetDatabase has
    /// no API that picks the @2x variant automatically, so a caller that needs a high-DPI image loads the
    /// <c>@2x</c> path itself.
    /// </remarks>
    internal static class EditorIcons
    {
        public const string ROOT = CommonStyleSheetPaths.ROOT + "/Icons";

        public const string LAYERS = "layers";
        public const string MOVE = "move";
        public const string PALETTE = "palette";
        public const string IMAGE = "image";
        public const string LINK = "link";
        public const string UNLINK = "unlink";
        public const string PIPETTE = "pipette";
        public const string CHART_SPLINE = "chart-spline";
        public const string SCALING = "scaling";
        public const string ROTATE_CW = "rotate-cw";
        public const string SCALE_3D = "scale-3d";
        public const string CROSSHAIR = "crosshair";
        public const string ANCHOR = "anchor";
        public const string BLEND = "blend";
        public const string PERCENT = "percent";
        public const string MOUSE_POINTER_CLICK = "mouse-pointer-click";
        public const string SHIELD_CHECK = "shield-check";
        public const string TRIANGLE_ALERT = "triangle-alert";
        public const string CHEVRON_DOWN = "chevron-down";
        public const string CHEVRON_RIGHT = "chevron-right";

        private static readonly string[] s_names = {
            LAYERS, MOVE, PALETTE, IMAGE, LINK, UNLINK, PIPETTE, CHART_SPLINE,
            SCALING, ROTATE_CW, SCALE_3D, CROSSHAIR, ANCHOR, BLEND, PERCENT, MOUSE_POINTER_CLICK, SHIELD_CHECK,
            TRIANGLE_ALERT, CHEVRON_DOWN, CHEVRON_RIGHT,
        };

        private static readonly Texture2D[] s_darkIcons = new Texture2D[s_names.Length];
        private static readonly Texture2D[] s_lightIcons = new Texture2D[s_names.Length];

        /// <summary>Every icon name.</summary>
        public static IReadOnlyList<string> Names => s_names;

        /// <summary>CanvasGroup track and clip.</summary>
        public static Texture2D Layers => Get(0);

        /// <summary>RectTransform.</summary>
        public static Texture2D Move => Get(1);

        /// <summary>Graphic.</summary>
        public static Texture2D Palette => Get(2);

        /// <summary>Image.</summary>
        public static Texture2D Image => Get(3);

        /// <summary>Constrain proportions, on.</summary>
        public static Texture2D Link => Get(4);

        /// <summary>Constrain proportions, off.</summary>
        public static Texture2D Unlink => Get(5);

        /// <summary>Set from binding.</summary>
        public static Texture2D Pipette => Get(6);

        /// <summary>Ease preview.</summary>
        public static Texture2D ChartSpline => Get(7);

        /// <summary>Size delta channel.</summary>
        public static Texture2D Scaling => Get(8);

        /// <summary>Rotation channel.</summary>
        public static Texture2D RotateCw => Get(9);

        /// <summary>Scale channel.</summary>
        public static Texture2D Scale3d => Get(10);

        /// <summary>Pivot channel.</summary>
        public static Texture2D Crosshair => Get(11);

        /// <summary>Anchor min and max channels.</summary>
        public static Texture2D Anchor => Get(12);

        /// <summary>Alpha channel.</summary>
        public static Texture2D Blend => Get(13);

        /// <summary>Fill amount channel.</summary>
        public static Texture2D Percent => Get(14);

        /// <summary>Interactable channel.</summary>
        public static Texture2D MousePointerClick => Get(15);

        /// <summary>Blocks raycasts channel.</summary>
        public static Texture2D ShieldCheck => Get(16);

        /// <summary>Warnings.</summary>
        public static Texture2D TriangleAlert => Get(17);

        /// <summary>Expanded card.</summary>
        public static Texture2D ChevronDown => Get(18);

        /// <summary>Collapsed card.</summary>
        public static Texture2D ChevronRight => Get(19);

        /// <summary>
        /// Returns the icon called <paramref name="name"/> (one of <see cref="Names"/>) for the current skin.
        /// </summary>
        public static Texture2D Get(string name)
        {
            var index = Array.IndexOf(s_names, name);

            if (index < 0)
            {
                throw new ArgumentException($"Unknown editor icon '{name}'.", nameof(name));
            }

            return Get(index);
        }

        public static string GetPath(string name, bool dark)
            => $"{ROOT}/{name}-{(dark ? "dark" : "light")}.png";

        private static Texture2D Get(int index)
        {
            var dark = EditorAssets.IsDark;
            var cache = dark ? s_darkIcons : s_lightIcons;

            if (cache[index].IsInvalid())
            {
                cache[index] = EditorAssets.LoadAsset<Texture2D>(GetPath(s_names[index], dark));
            }

            return cache[index];
        }
    }
}
