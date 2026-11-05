using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using ApexionGame.UguiPlayable.Editor.Common;

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
        };

        public static string GetDisplayName(string name)
            => s_entries.TryGetValue(name, out var entry) ? entry.DisplayName : ObjectNames.NicifyVariableName(name);

        public static Texture2D GetIcon(string name)
            => s_entries.TryGetValue(name, out var entry) ? entry.Icon() : null;

        /// <summary>
        /// Whether the channel is a 0..1 value drawn as a slider.
        /// </summary>
        public static bool IsNormalized(string name)
            => name == ALPHA || name == FILL_AMOUNT;

        private readonly record struct Entry(string DisplayName, Func<Texture2D> Icon);
    }
}
