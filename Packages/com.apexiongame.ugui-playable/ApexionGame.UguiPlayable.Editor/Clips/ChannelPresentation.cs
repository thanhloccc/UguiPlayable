using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ApexionGame.UguiPlayable.Editor.Common;
using ApexionGame.UguiPlayable.Tracks.Controls;
using ApexionGame.UguiPlayable.Tracks.Layout;

namespace ApexionGame.UguiPlayable.Editor.Clips
{
    /// <summary>
    /// Maps a channel's serialized field name to its display name and icon. An unknown name falls back to the
    /// nicified field name and no icon.
    /// </summary>
    internal static class ChannelPresentation
    {
        public const string ANCHORED_POSITION = "_anchoredPosition";
        public const string SIZE_DELTA = "_sizeDelta";
        public const string ROTATION = "_rotation";
        public const string SCALE = "_scale";
        public const string PIVOT = "_pivot";
        public const string ANCHOR_MIN = "_anchorMin";
        public const string ANCHOR_MAX = "_anchorMax";
        public const string COLOR = "_color";
        public const string ALPHA = "_alpha";
        public const string FILL_AMOUNT = "_fillAmount";
        public const string INTERACTABLE = "_interactable";
        public const string BLOCKS_RAYCASTS = "_blocksRaycasts";
        public const string VALUE = "_value";
        public const string SIZE = "_size";
        public const string NORMALIZED_POSITION = "_normalizedPosition";
        public const string UV_RECT = "_uvRect";
        public const string EFFECT_COLOR = "_effectColor";
        public const string EFFECT_DISTANCE = "_effectDistance";
        public const string MIN_WIDTH = "_minWidth";
        public const string MIN_HEIGHT = "_minHeight";
        public const string PREFERRED_WIDTH = "_preferredWidth";
        public const string PREFERRED_HEIGHT = "_preferredHeight";
        public const string FLEXIBLE_WIDTH = "_flexibleWidth";
        public const string FLEXIBLE_HEIGHT = "_flexibleHeight";
        public const string SPACING = "_spacing";
        public const string CELL_SIZE = "_cellSize";
        public const string PADDING = "_padding";
        public const string FONT_SIZE = "_fontSize";
        public const string CHARACTER_SPACING = "_characterSpacing";
        public const string WORD_SPACING = "_wordSpacing";
        public const string LINE_SPACING = "_lineSpacing";
        public const string PARAGRAPH_SPACING = "_paragraphSpacing";
        public const string VISIBLE_RATIO = "_visibleRatio";
        public const string COLOR_GRADIENT = "_colorGradient";

        private static readonly Dictionary<string, Entry> s_entries = new() {
            [ANCHORED_POSITION] = new("Anchored Position", static () => EditorIcons.Move),
            [SIZE_DELTA] = new("Size Delta", static () => EditorIcons.Scaling),
            [ROTATION] = new("Rotation", static () => EditorIcons.RotateCw),
            [SCALE] = new("Scale", static () => EditorIcons.Scale3d),
            [PIVOT] = new("Pivot", static () => EditorIcons.Crosshair),
            [ANCHOR_MIN] = new("Anchor Min", static () => EditorIcons.Anchor),
            [ANCHOR_MAX] = new("Anchor Max", static () => EditorIcons.Anchor),
            [COLOR] = new("Color", static () => EditorIcons.Palette),
            [ALPHA] = new("Alpha", static () => EditorIcons.Blend),
            [FILL_AMOUNT] = new("Fill Amount", static () => EditorIcons.Percent),
            [INTERACTABLE] = new("Interactable", static () => EditorIcons.MousePointerClick),
            [BLOCKS_RAYCASTS] = new("Blocks Raycasts", static () => EditorIcons.ShieldCheck),
            [VALUE] = new("Value", static () => EditorIcons.ChevronsLeftRight),
            [SIZE] = new("Size", static () => EditorIcons.Ruler),
            [NORMALIZED_POSITION] = new("Normalized Position", static () => EditorIcons.ChevronsLeftRight),
            [UV_RECT] = new("UV Rect", static () => EditorIcons.Frame),
            [EFFECT_COLOR] = new("Effect Color", static () => EditorIcons.Palette),
            [EFFECT_DISTANCE] = new("Effect Distance", static () => EditorIcons.Move),
            [MIN_WIDTH] = new("Min Width", static () => EditorIcons.Ruler),
            [MIN_HEIGHT] = new("Min Height", static () => EditorIcons.Ruler),
            [PREFERRED_WIDTH] = new("Preferred Width", static () => EditorIcons.Ruler),
            [PREFERRED_HEIGHT] = new("Preferred Height", static () => EditorIcons.Ruler),
            [FLEXIBLE_WIDTH] = new("Flexible Width", static () => EditorIcons.Ruler),
            [FLEXIBLE_HEIGHT] = new("Flexible Height", static () => EditorIcons.Ruler),
            [SPACING] = new("Spacing", static () => EditorIcons.MoveHorizontal),
            [CELL_SIZE] = new("Cell Size", static () => EditorIcons.Scaling),
            [PADDING] = new("Padding", static () => EditorIcons.MoveHorizontal),
            [FONT_SIZE] = new("Font Size", static () => EditorIcons.CaseSensitive),
            [CHARACTER_SPACING] = new("Character Spacing", static () => EditorIcons.Space),
            [WORD_SPACING] = new("Word Spacing", static () => EditorIcons.Space),
            [LINE_SPACING] = new("Line Spacing", static () => EditorIcons.Baseline),
            [PARAGRAPH_SPACING] = new("Paragraph Spacing", static () => EditorIcons.Pilcrow),
            [VISIBLE_RATIO] = new("Visible Ratio", static () => EditorIcons.TextCursor),
            [COLOR_GRADIENT] = new("Color Gradient", static () => EditorIcons.Paintbrush),
        };

        private static readonly string[] s_defaultAxisCaptions = { "X", "Y", "Z", "W" };

        // Channels whose components are not plain X/Y/Z/W. Phase-2 tracks add their entries here (a padding as
        // L/R/T/B, for example).
        private static readonly Dictionary<string, string[]> s_axisCaptions = new() {
            [UV_RECT] = new[] { "X", "Y", "W", "H" },
            [PADDING] = new[] { "L", "R", "T", "B" },
        };

        /// <summary>
        /// The caption of each vector component of a channel, used by the axes row and the from/to fields.
        /// </summary>
        public static string[] GetAxisCaptions(string name)
            => s_axisCaptions.TryGetValue(name, out var captions) ? captions : s_defaultAxisCaptions;

        public static string GetDisplayName(string name)
            => s_entries.TryGetValue(name, out var entry) ? entry.DisplayName : ObjectNames.NicifyVariableName(name);

        public static Texture2D GetIcon(string name)
            => s_entries.TryGetValue(name, out var entry) ? entry.Icon() : null;

        /// <summary>
        /// Whether the channel is a 0..1 value drawn as a slider. The clip type decides for a name two clips share:
        /// <c>_value</c> is 0..1 on a scrollbar but spans the slider's own range on a slider.
        /// </summary>
        public static bool IsNormalized(Type clipType, string name)
            => name switch {
                ALPHA or FILL_AMOUNT or SIZE or VISIBLE_RATIO => true,
                VALUE => clipType == typeof(ScrollbarClip),
                _ => false,
            };

        /// <summary>
        /// Whether a negative value of the clip's float channels means "not set" to uGUI, as for every
        /// <c>LayoutElement</c> size.
        /// </summary>
        public static bool IsUnsetWhenNegative(Type clipType)
            => clipType == typeof(LayoutElementClip);

        private readonly record struct Entry(string DisplayName, Func<Texture2D> Icon);
    }
}
