using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine.Timeline;

namespace ApexionGame.UguiPlayable.Editor.Presets
{
    /// <summary>
    /// The presets the <see cref="PresetBrowserWindow"/> lists, grouped by category and filtered by a search text,
    /// plus the rules the window asks (why "Add to timeline" is disabled, what a preset binds) and the "Duplicate"
    /// action. It holds no UI.
    /// </summary>
    internal sealed class PresetBrowserModel
    {
        public const string NO_CATEGORY = "uncategorized";
        public const string NO_TIMELINE_REASON = "Open a timeline in the Timeline window to add a preset to it.";

        private const string ASSETS_ROOT = "Assets";
        private const string PLAYABLE_EXTENSION = ".playable";
        private const string ASSET_EXTENSION = ".asset";

        private readonly List<TweenPreset> _presets;
        private readonly List<PresetGroup> _groups = new();

        public PresetBrowserModel(List<TweenPreset> presets)
        {
            _presets = presets ?? new List<TweenPreset>();
            Query = string.Empty;
            Category = PresetPicker.ALL_CATEGORIES;
        }

        /// <summary>The search text; a preset matches on its name, category or description.</summary>
        public string Query { get; set; }

        /// <summary>The category filter, or <see cref="PresetPicker.ALL_CATEGORIES"/>.</summary>
        public string Category { get; set; }

        public IReadOnlyList<TweenPreset> Presets => _presets;

        /// <summary>
        /// The categories of the presets in sort order, after <see cref="PresetPicker.ALL_CATEGORIES"/>.
        /// </summary>
        public List<string> GetCategories()
        {
            var categories = new List<string> { PresetPicker.ALL_CATEGORIES };

            for (var i = 0; i < _presets.Count; i++)
            {
                var category = GetCategory(_presets[i]);

                if (categories.Contains(category) == false)
                {
                    categories.Add(category);
                }
            }

            return categories;
        }

        /// <summary>
        /// The presets that match <see cref="Query"/> and <see cref="Category"/>, grouped by category. The list is
        /// rebuilt on each call and reused by the next one.
        /// </summary>
        public List<PresetGroup> GetGroups()
        {
            _groups.Clear();

            var anyCategory = string.Equals(Category, PresetPicker.ALL_CATEGORIES, StringComparison.Ordinal);

            for (var i = 0; i < _presets.Count; i++)
            {
                var preset = _presets[i];
                var category = GetCategory(preset);

                if (anyCategory == false && string.Equals(category, Category, StringComparison.Ordinal) == false)
                {
                    continue;
                }

                if (PresetPicker.Matches(preset, Query, PresetPicker.ALL_CATEGORIES) == false)
                {
                    continue;
                }

                var group = FindGroup(category);

                if (group == null)
                {
                    group = new PresetGroup(category);
                    _groups.Add(group);
                }

                group.Presets.Add(preset);
            }

            return _groups;
        }

        /// <summary>
        /// Why "Add to timeline" is disabled, or an empty string when it is available.
        /// </summary>
        public static string GetAddBlockReason(TweenPreset preset, TimelineAsset inspected)
        {
            if (inspected.IsInvalid())
            {
                return NO_TIMELINE_REASON;
            }

            return preset.IsInvalid() || preset.Timeline.IsInvalid() ? "This preset has no timeline." : string.Empty;
        }

        /// <summary>
        /// One entry per track of the preset's timeline: the track name, its type and the path it binds to.
        /// </summary>
        public static List<PresetTrackInfo> GetTracks(TweenPreset preset)
        {
            var tracks = new List<PresetTrackInfo>();

            if (preset.IsInvalid() || preset.Timeline.IsInvalid())
            {
                return tracks;
            }

            foreach (var track in preset.Timeline.GetOutputTracks())
            {
                var hasPath = preset.TryGetPath(track.name, out var path);
                tracks.Add(new(track.name, track.GetType().Name, hasPath ? path : null));
            }

            return tracks;
        }

        /// <summary>
        /// Copies the preset's timeline into <paramref name="folder"/> (a folder under <c>Assets/</c>), saves the
        /// copy, then copies the preset and points the copy at the copied timeline. The preset copy never
        /// references the original timeline. Returns null with a reason in <paramref name="error"/> when it fails.
        /// </summary>
        public static TweenPreset Duplicate(TweenPreset preset, string folder, out string error)
        {
            error = string.Empty;

            if (preset.IsInvalid() || preset.Timeline.IsInvalid())
            {
                error = "The preset has no timeline to copy.";
                return null;
            }

            folder = folder?.Replace('\\', '/').TrimEnd('/') ?? string.Empty;

            if (IsUnderAssets(folder) == false || AssetDatabase.IsValidFolder(folder) == false)
            {
                error = "Pick an existing folder under Assets.";
                return null;
            }

            var presetPath = AssetDatabase.GetAssetPath(preset);
            var timelinePath = AssetDatabase.GetAssetPath(preset.Timeline);

            if (string.IsNullOrEmpty(presetPath) || string.IsNullOrEmpty(timelinePath))
            {
                error = "The preset or its timeline is not an asset on disk.";
                return null;
            }

            if (string.Equals(presetPath, timelinePath, StringComparison.Ordinal))
            {
                error = "The timeline is stored inside the preset asset and cannot be copied on its own.";
                return null;
            }

            var timelineCopyPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{folder}/{Path.GetFileNameWithoutExtension(timelinePath)}{PLAYABLE_EXTENSION}"
            );

            if (AssetDatabase.CopyAsset(timelinePath, timelineCopyPath) == false)
            {
                error = $"Could not copy '{timelinePath}' to '{timelineCopyPath}'.";
                return null;
            }

            var timelineCopy = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelineCopyPath);

            // Save the copied timeline before anything references it (an unsaved asset binds as fileID 0).
            AssetDatabase.SaveAssetIfDirty(timelineCopy);

            var presetCopyPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{folder}/{Path.GetFileNameWithoutExtension(presetPath)}{ASSET_EXTENSION}"
            );

            if (AssetDatabase.CopyAsset(presetPath, presetCopyPath) == false)
            {
                error = $"Could not copy '{presetPath}' to '{presetCopyPath}'.";
                return null;
            }

            var presetCopy = AssetDatabase.LoadAssetAtPath<TweenPreset>(presetCopyPath);
            var serialized = new SerializedObject(presetCopy);
            serialized.FindProperty(nameof(TweenPreset._timeline)).objectReferenceValue = timelineCopy;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssetIfDirty(presetCopy);
            return presetCopy;
        }

        public static string GetCategory(TweenPreset preset)
            => string.IsNullOrEmpty(preset.Category) ? NO_CATEGORY : preset.Category;

        private static bool IsUnderAssets(string folder)
            => string.Equals(folder, ASSETS_ROOT, StringComparison.Ordinal)
            || folder.StartsWith(ASSETS_ROOT + "/", StringComparison.Ordinal);

        private PresetGroup FindGroup(string category)
        {
            for (var i = 0; i < _groups.Count; i++)
            {
                if (string.Equals(_groups[i].Category, category, StringComparison.Ordinal))
                {
                    return _groups[i];
                }
            }

            return null;
        }
    }

    /// <summary>
    /// The presets of one category that pass the browser's filters.
    /// </summary>
    internal sealed class PresetGroup
    {
        public PresetGroup(string category)
        {
            Category = category;
        }

        public string Category { get; }

        public List<TweenPreset> Presets { get; } = new();
    }

    /// <summary>
    /// One track of a preset: its name, its type name, and the binding path (null when the preset gives none).
    /// </summary>
    internal readonly record struct PresetTrackInfo(string Name, string TypeName, string Path);
}
