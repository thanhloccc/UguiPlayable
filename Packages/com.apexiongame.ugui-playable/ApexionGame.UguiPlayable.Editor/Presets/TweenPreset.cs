using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;
using ApexionGame.UguiPlayable.Editor.Common;

namespace ApexionGame.UguiPlayable.Editor.Presets
{
    /// <summary>
    /// Names a preset timeline and describes it: its category, description, icon and the hierarchy path each of its
    /// tracks binds to, relative to a binding root (for example <c>backdrop</c> or <c>body/popup</c>).
    /// </summary>
    /// <remarks>
    /// The serialized field names are user data: users author their own presets with this asset.
    /// <see cref="PresetRecipe"/> adds a preset to a timeline or applies it to clips.
    /// </remarks>
    [CreateAssetMenu(fileName = "tween-preset", menuName = "UGUI Playable/Tween Preset")]
    internal sealed class TweenPreset : ScriptableObject
    {
        [SerializeField]
        [Tooltip("The preset timeline whose tracks and clips this preset adds.")]
        internal TimelineAsset _timeline;

        [SerializeField]
        [Tooltip("The group the preset is listed under, for example popup or screen.")]
        internal string _category = string.Empty;

        [SerializeField, TextArea]
        [Tooltip("What the preset does, shown as the tooltip of the preset in the picker.")]
        internal string _description = string.Empty;

        [SerializeField]
        [Tooltip("A Lucide icon name of the package, for example wand-sparkles. Unknown names use wand-sparkles.")]
        internal string _icon = DEFAULT_ICON;

        [SerializeField]
        [Tooltip("The hierarchy path, relative to the binding root, that each track of the timeline binds to.")]
        internal TweenPresetBinding[] _bindings = Array.Empty<TweenPresetBinding>();

        /// <summary>The icon of a preset whose <c>_icon</c> is empty or unknown.</summary>
        public const string DEFAULT_ICON = EditorIcons.WAND_SPARKLES;

        private const string ASSET_FILTER = "t:" + nameof(TweenPreset);

        public TimelineAsset Timeline => _timeline;

        public string Category => _category ?? string.Empty;

        public string Description => _description ?? string.Empty;

        /// <summary>
        /// <c>_icon</c> when it is one of <see cref="EditorIcons.Names"/>, otherwise <see cref="DEFAULT_ICON"/>.
        /// </summary>
        public string IconName => IsKnownIcon(_icon) ? _icon : DEFAULT_ICON;

        public Texture2D Icon => EditorIcons.Get(IconName);

        /// <summary>
        /// Finds the binding path of the track called <paramref name="trackName"/>. An empty path is the binding root.
        /// </summary>
        public bool TryGetPath(string trackName, out string path)
        {
            var bindings = _bindings ?? Array.Empty<TweenPresetBinding>();
            var count = bindings.Length;

            for (var i = 0; i < count; i++)
            {
                var binding = bindings[i];

                if (string.Equals(binding._trackName, trackName, StringComparison.Ordinal))
                {
                    path = binding._path ?? string.Empty;
                    return true;
                }
            }

            path = string.Empty;
            return false;
        }

        /// <summary>
        /// Lists what is wrong with the preset: an unknown icon, no timeline, a track with no binding path, or a
        /// binding whose track is not an output track of the timeline.
        /// </summary>
        public List<TweenPresetProblem> GetProblems()
        {
            var problems = new List<TweenPresetProblem>();

            if (IsKnownIcon(_icon) == false)
            {
                problems.Add(new(TweenPresetProblemKind.UnknownIcon, _icon));
            }

            if (_timeline.IsInvalid())
            {
                problems.Add(new(TweenPresetProblemKind.NoTimeline, string.Empty));
                return problems;
            }

            var trackNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (var track in _timeline.GetOutputTracks())
            {
                trackNames.Add(track.name);

                if (TryGetPath(track.name, out _) == false)
                {
                    problems.Add(new(TweenPresetProblemKind.TrackWithoutPath, track.name));
                }
            }

            var bindings = _bindings ?? Array.Empty<TweenPresetBinding>();
            var count = bindings.Length;

            for (var i = 0; i < count; i++)
            {
                var trackName = bindings[i]._trackName ?? string.Empty;

                if (trackNames.Contains(trackName) == false)
                {
                    problems.Add(new(TweenPresetProblemKind.UnknownTrack, trackName));
                }
            }

            return problems;
        }

        /// <summary>
        /// Every <see cref="TweenPreset"/> asset in the project and the packages, sorted by category, then name.
        /// </summary>
        public static List<TweenPreset> FindAll()
        {
            var guids = AssetDatabase.FindAssets(ASSET_FILTER);
            var presets = new List<TweenPreset>(guids.Length);

            for (var i = 0; i < guids.Length; i++)
            {
                var preset = AssetDatabase.LoadAssetAtPath<TweenPreset>(AssetDatabase.GUIDToAssetPath(guids[i]));

                if (preset.IsValid())
                {
                    presets.Add(preset);
                }
            }

            presets.Sort(static (a, b) => {
                var category = string.CompareOrdinal(a.Category, b.Category);
                return category != 0 ? category : string.CompareOrdinal(a.name, b.name);
            });

            return presets;
        }

        public static bool IsKnownIcon(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            var names = EditorIcons.Names;
            var count = names.Count;

            for (var i = 0; i < count; i++)
            {
                if (string.Equals(names[i], name, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        /// <remarks>
        /// Warns about an unknown icon and an unknown track name. A preset that is still being filled in (no
        /// timeline, or tracks without a path yet) is not reported here; the picker shows those problems.
        /// </remarks>
        private void OnValidate()
        {
            var problems = GetProblems();
            var count = problems.Count;

            for (var i = 0; i < count; i++)
            {
                var problem = problems[i];

                if (problem.Kind is TweenPresetProblemKind.UnknownIcon or TweenPresetProblemKind.UnknownTrack)
                {
                    Debug.LogWarning($"Tween preset '{name}': {problem.ToMessage()}", this);
                }
            }
        }
    }

    /// <summary>
    /// One track of a preset and the hierarchy path, relative to the binding root, that it binds to.
    /// </summary>
    [Serializable]
    internal struct TweenPresetBinding
    {
        [SerializeField]
        [Tooltip("The name of an output track of the preset timeline.")]
        internal string _trackName;

        [SerializeField]
        [Tooltip("The path below the binding root, for example body/popup. Empty binds the root itself.")]
        internal string _path;

        public TweenPresetBinding(string trackName, string path)
        {
            _trackName = trackName;
            _path = path;
        }
    }

    internal enum TweenPresetProblemKind
    {
        UnknownIcon,
        NoTimeline,
        TrackWithoutPath,
        UnknownTrack,
    }

    /// <summary>
    /// One problem of a <see cref="TweenPreset"/>: its kind and the icon or track name it is about.
    /// </summary>
    internal readonly record struct TweenPresetProblem(TweenPresetProblemKind Kind, string Name)
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public string ToMessage()
            => Kind switch {
                TweenPresetProblemKind.UnknownIcon
                    => $"Icon '{Name}' is not a package icon; '{TweenPreset.DEFAULT_ICON}' is used instead.",
                TweenPresetProblemKind.NoTimeline => "No preset timeline is assigned.",
                TweenPresetProblemKind.TrackWithoutPath => $"Track '{Name}' has no binding path; it is added unbound.",
                TweenPresetProblemKind.UnknownTrack
                    => $"A binding names track '{Name}', which the timeline does not have.",
                _ => Kind.ToString(),
            };
    }
}
