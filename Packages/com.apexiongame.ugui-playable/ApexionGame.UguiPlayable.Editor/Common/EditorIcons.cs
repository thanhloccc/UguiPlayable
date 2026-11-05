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
    /// <c>name-dark.png</c> or <c>name-light.png</c> (16 px) is loaded when
    /// <see cref="UnityEditor.EditorGUIUtility.pixelsPerPoint"/> is 1 or less. <c>name-dark@2x.png</c> or
    /// <c>name-light@2x.png</c> (32 px) is loaded when it is above 1. Unverified: whether <c>pixelsPerPoint</c>
    /// reports the display scale outside <c>OnGUI</c> (when a UI Toolkit inspector is built) at scales above 100 %.
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
        public const string SLIDERS_HORIZONTAL = "sliders-horizontal";
        public const string GALLERY_VERTICAL = "gallery-vertical";
        public const string SCROLL = "scroll";
        public const string CHEVRONS_LEFT_RIGHT = "chevrons-left-right";
        public const string WALLPAPER = "wallpaper";
        public const string FRAME = "frame";
        public const string COPY = "copy";
        public const string RULER = "ruler";
        public const string COLUMNS_3 = "columns-3";
        public const string GRID_3X3 = "grid-3x3";
        public const string MOVE_HORIZONTAL = "move-horizontal";
        public const string TYPE = "type";
        public const string CASE_SENSITIVE = "case-sensitive";
        public const string SPACE = "space";
        public const string BASELINE = "baseline";
        public const string PILCROW = "pilcrow";
        public const string TEXT_CURSOR = "text-cursor";
        public const string PAINTBRUSH = "paintbrush";
        public const string WHOLE_WORD = "whole-word";
        public const string ACTIVITY = "activity";
        public const string VIBRATE = "vibrate";
        public const string DICES = "dices";
        public const string TOGGLE_RIGHT = "toggle-right";
        public const string TEXT_SELECT = "text-select";
        public const string WAND_SPARKLES = "wand-sparkles";
        public const string LIBRARY = "library";
        public const string LIST_ORDERED = "list-ordered";

        private static readonly string[] s_names = {
            LAYERS, MOVE, PALETTE, IMAGE, LINK, UNLINK, PIPETTE, CHART_SPLINE,
            SCALING, ROTATE_CW, SCALE_3D, CROSSHAIR, ANCHOR, BLEND, PERCENT, MOUSE_POINTER_CLICK, SHIELD_CHECK,
            TRIANGLE_ALERT, CHEVRON_DOWN, CHEVRON_RIGHT,
            SLIDERS_HORIZONTAL, GALLERY_VERTICAL, SCROLL, CHEVRONS_LEFT_RIGHT, WALLPAPER, FRAME, COPY, RULER,
            COLUMNS_3, GRID_3X3, MOVE_HORIZONTAL, TYPE, CASE_SENSITIVE, SPACE, BASELINE, PILCROW, TEXT_CURSOR,
            PAINTBRUSH, WHOLE_WORD, ACTIVITY, VIBRATE, DICES,
            TOGGLE_RIGHT, TEXT_SELECT, WAND_SPARKLES, LIBRARY, LIST_ORDERED,
        };

        private static readonly Texture2D[] s_darkIcons = new Texture2D[s_names.Length];
        private static readonly Texture2D[] s_lightIcons = new Texture2D[s_names.Length];
        private static readonly Texture2D[] s_darkIcons2x = new Texture2D[s_names.Length];
        private static readonly Texture2D[] s_lightIcons2x = new Texture2D[s_names.Length];

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

        /// <summary>Slider track and clip.</summary>
        public static Texture2D SlidersHorizontal => Get(20);

        /// <summary>Scrollbar track and clip.</summary>
        public static Texture2D GalleryVertical => Get(21);

        /// <summary>ScrollRect track and clip.</summary>
        public static Texture2D Scroll => Get(22);

        /// <summary>Scroll position and value channels.</summary>
        public static Texture2D ChevronsLeftRight => Get(23);

        /// <summary>RawImage track and clip.</summary>
        public static Texture2D Wallpaper => Get(24);

        /// <summary>UV rect channel.</summary>
        public static Texture2D Frame => Get(25);

        /// <summary>Shadow/Outline track and clip.</summary>
        public static Texture2D Copy => Get(26);

        /// <summary>LayoutElement track and clip; size channels.</summary>
        public static Texture2D Ruler => Get(27);

        /// <summary>Horizontal/Vertical LayoutGroup track and clip.</summary>
        public static Texture2D Columns3 => Get(28);

        /// <summary>GridLayoutGroup track and clip.</summary>
        public static Texture2D Grid3x3 => Get(29);

        /// <summary>Spacing and padding channels.</summary>
        public static Texture2D MoveHorizontal => Get(30);

        /// <summary>TMP_Text track and clip.</summary>
        public static Texture2D Type => Get(31);

        /// <summary>Font size channel.</summary>
        public static Texture2D CaseSensitive => Get(32);

        /// <summary>Character and word spacing channels.</summary>
        public static Texture2D Space => Get(33);

        /// <summary>Line spacing channel.</summary>
        public static Texture2D Baseline => Get(34);

        /// <summary>Paragraph spacing channel.</summary>
        public static Texture2D Pilcrow => Get(35);

        /// <summary>Typewriter (visible ratio) channel.</summary>
        public static Texture2D TextCursor => Get(36);

        /// <summary>Vertex gradient channel.</summary>
        public static Texture2D Paintbrush => Get(37);

        /// <summary>Text (string tween) channel.</summary>
        public static Texture2D WholeWord => Get(38);

        /// <summary>Punch channel.</summary>
        public static Texture2D Activity => Get(39);

        /// <summary>Shake channel.</summary>
        public static Texture2D Vibrate => Get(40);

        /// <summary>New random seed (shake channel).</summary>
        public static Texture2D Dices => Get(41);

        /// <summary>Toggle track and clip; Is On channel.</summary>
        public static Texture2D ToggleRight => Get(42);

        /// <summary>TMP Characters track and clip.</summary>
        public static Texture2D TextSelect => Get(43);

        /// <summary>Presets: Apply preset, and the default preset icon.</summary>
        public static Texture2D WandSparkles => Get(44);

        /// <summary>Preset browser window.</summary>
        public static Texture2D Library => Get(45);

        /// <summary>Stagger recipe.</summary>
        public static Texture2D ListOrdered => Get(46);

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
            => GetPath(name, dark, false);

        public static string GetPath(string name, bool dark, bool highDpi)
            => $"{ROOT}/{name}-{(dark ? "dark" : "light")}{(highDpi ? "@2x" : "")}.png";

        private static Texture2D Get(int index)
        {
            var dark = EditorAssets.IsDark;
            var highDpi = UnityEditor.EditorGUIUtility.pixelsPerPoint > 1f;
            var cache = highDpi
                ? (dark ? s_darkIcons2x : s_lightIcons2x)
                : (dark ? s_darkIcons : s_lightIcons);

            if (cache[index].IsInvalid())
            {
                cache[index] = EditorAssets.LoadAsset<Texture2D>(GetPath(s_names[index], dark, highDpi));
            }

            return cache[index];
        }
    }
}
