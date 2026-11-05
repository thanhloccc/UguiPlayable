using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ApexionGame.UguiPlayable.Editor.Common;
using ApexionGame.UguiPlayable.Tracks.Controls;

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
        };

        private static readonly string[] s_defaultAxisCaptions = { "X", "Y", "Z", "W" };

        // Channels whose components are not plain X/Y/Z/W. Phase-2 tracks add their entries here (for example a UV
        // rect as X/Y/W/H, a padding as L/R/T/B).
        private static readonly Dictionary<string, string[]> s_axisCaptions = new();

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
                ALPHA or FILL_AMOUNT or SIZE => true,
                VALUE => clipType == typeof(ScrollbarClip),
                _ => false,
            };

        private readonly record struct Entry(string DisplayName, Func<Texture2D> Icon);
    }
}
