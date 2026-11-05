using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Editor.Clips;
using ApexionGame.UguiPlayable.Editor.Tracks;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Editor.Presets
{
    /// <summary>
    /// Adds a <see cref="TweenPreset"/> to a timeline (tracks and clips at a time, bound by path from a binding root)
    /// or applies it to clips (copies the template of the preset's clip of the same type). Each call is one Undo step
    /// and returns the warnings the user must see.
    /// </summary>
    /// <remarks>
    /// Track and clip creation register their own Undo; the calls group them with the director's binding changes
    /// into one step. A preset's clip asset is never referenced from the target timeline: only values are copied.
    /// </remarks>
    internal static class PresetRecipe
    {
        public const string ADD_UNDO_NAME = "Add Preset";
        public const string APPLY_UNDO_NAME = "Apply Preset";

        // Timeline keeps the extrapolation setters internal; they are written through the track's serialized clips.
        private const string CLIP_PATH_FORMAT = "m_Clips.Array.data[{0}].";
        private const string PRE_EXTRAPOLATION = "m_PreExtrapolationMode";
        private const string POST_EXTRAPOLATION = "m_PostExtrapolationMode";
        private const string TRANSITION_PREFIX = "transition-";
        private const string PLAYABLE_EXTENSION = ".playable";
        private const string DISK_USER_FILTER = "t:Prefab t:Scene";
        private const string PREFAB_EXTENSION = ".prefab";

        private static readonly string[] s_trackSettings = {
            TweenTrackInspector.RESTORE_MODE,
            TweenTrackInspector.SEND_CALLBACKS,
        };

        private static readonly Dictionary<string, List<DiskUser>> s_diskUsers = new();

        /// <summary>
        /// The binding root of a director: its parent when the director's GameObject is a <c>transition-*</c> child
        /// (the UguiPages standard), otherwise the director's own GameObject.
        /// </summary>
        public static GameObject GetDefaultBindingRoot(PlayableDirector director)
        {
            if (director.IsInvalid())
            {
                return null;
            }

            var transform = director.transform;
            var isTransition = transform.name.StartsWith(TRANSITION_PREFIX, StringComparison.Ordinal);

            if (isTransition && transform.parent.IsValid())
            {
                return transform.parent.gameObject;
            }

            return director.gameObject;
        }

        /// <summary>
        /// Creates the preset's tracks and clips in <paramref name="target"/>, starting at
        /// <paramref name="startTime"/>, and binds them by path from <paramref name="bindingRoot"/> when
        /// <paramref name="director"/> plays <paramref name="target"/>. Otherwise (asset mode, a sub-timeline) the
        /// tracks are added unbound. The timeline is saved afterwards.
        /// </summary>
        /// <param name="bindingRoot">Null uses <see cref="GetDefaultBindingRoot"/>.</param>
        public static PresetRecipeResult AddToTimeline(
              TweenPreset preset
            , TimelineAsset target
            , PlayableDirector director
            , GameObject bindingRoot
            , double startTime
        )
        {
            var result = new PresetRecipeResult(target);
            var group = BeginGroup(ADD_UNDO_NAME);

            AddCore(preset, target, director, bindingRoot, startTime, result);

            Undo.CollapseUndoOperations(group);
            return result;
        }

        /// <summary>
        /// Copies the director's timeline next to the original, saves the copy, moves the director's bindings to the
        /// copy's tracks, makes the director play the copy, then adds the preset to the copy. One Undo step reverts
        /// the director and the added tracks; the copy stays on disk.
        /// </summary>
        public static PresetRecipeResult DuplicateAndAdd(
              TweenPreset preset
            , PlayableDirector director
            , GameObject bindingRoot
            , double startTime
        )
        {
            var group = BeginGroup(ADD_UNDO_NAME);
            var copy = DuplicateForDirector(director, bindingRoot, out var warning);
            var result = new PresetRecipeResult(copy);

            if (copy.IsInvalid())
            {
                result.Warnings.Add(warning);
            }
            else
            {
                AddCore(preset, copy, director, bindingRoot, startTime, result);
            }

            Undo.CollapseUndoOperations(group);
            return result;
        }

        /// <summary>
        /// Copies the template of the preset's clip of the same type into each clip asset of
        /// <paramref name="clipAssets"/>. Timing and the clip name are on the <see cref="TimelineClip"/>, so they stay.
        /// </summary>
        /// <param name="source">
        /// The preset clip asset to copy from, for the clips of its type. Null, or a clip of another type, uses the
        /// first clip of that type in the preset.
        /// </param>
        public static PresetRecipeResult ApplyToClips(
              TweenPreset preset
            , IReadOnlyList<PlayableAsset> clipAssets
            , PlayableAsset source = null
        )
        {
            var result = new PresetRecipeResult(null);
            var group = BeginGroup(APPLY_UNDO_NAME);
            var count = clipAssets.Count;

            for (var i = 0; i < count; i++)
            {
                var clipAsset = clipAssets[i];

                if (clipAsset.IsInvalid())
                {
                    continue;
                }

                var type = clipAsset.GetType();
                var from = source.IsValid() && source.GetType() == type ? source : FindFirstClipAsset(preset, type);

                if (from.IsInvalid())
                {
                    result.Warnings.Add($"Preset '{preset.name}' has no {type.Name}; '{clipAsset.name}' is unchanged.");
                    continue;
                }

                if (from == clipAsset)
                {
                    continue;
                }

                UndoExtensions.RegisterPlayableAsset(clipAsset, APPLY_UNDO_NAME);
                CopyTemplate(from, clipAsset);
                result.AppliedClipAssets.Add(clipAsset);
            }

            Undo.CollapseUndoOperations(group);
            TimelineEditor.Refresh(RefreshReason.ContentsModified);
            return result;
        }

        /// <summary>
        /// Every clip of the preset's timeline whose asset is a <paramref name="clipType"/>, with its track.
        /// </summary>
        public static List<PresetClip> FindClips(TweenPreset preset, Type clipType)
        {
            var clips = new List<PresetClip>();
            var timeline = preset.Timeline;

            if (timeline.IsInvalid())
            {
                return clips;
            }

            foreach (var track in timeline.GetOutputTracks())
            {
                foreach (var clip in track.GetClips())
                {
                    if (clip.asset is PlayableAsset asset && asset.IsValid() && asset.GetType() == clipType)
                    {
                        clips.Add(new(track, clip, asset));
                    }
                }
            }

            return clips;
        }

        /// <summary>
        /// Whether <paramref name="clipAsset"/> is a clip of this package (derives from <c>TweenClip&lt;T&gt;</c>).
        /// </summary>
        public static bool IsTweenClip(Object clipAsset)
        {
            if (clipAsset.IsInvalid())
            {
                return false;
            }

            for (var type = clipAsset.GetType(); type != null; type = type.BaseType)
            {
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(TweenClip<>))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Counts the other users of <paramref name="timeline"/>: the directors that play it in the open scenes and
        /// the open prefab stage (except <paramref name="director"/>), plus the prefabs and scenes on disk that are
        /// not open and reference it. A prefab counts each of its directors that plays the timeline; a scene counts
        /// once.
        /// </summary>
        /// <remarks>
        /// The disk part is cached per timeline until the next <see cref="EditorApplication.projectChanged"/>.
        /// </remarks>
        public static SharedTimelineUsage CountOtherUsers(TimelineAsset timeline, PlayableDirector director)
        {
            if (timeline.IsInvalid())
            {
                return default;
            }

            var openFiles = new HashSet<string>(StringComparer.Ordinal);
            var openDirectors = CountOpenDirectors(timeline, director, openFiles);
            var diskDirectors = 0;
            var users = GetDiskUsers(timeline);
            var count = users.Count;

            for (var i = 0; i < count; i++)
            {
                var user = users[i];

                if (openFiles.Contains(user.Path) == false)
                {
                    diskDirectors += user.DirectorCount;
                }
            }

            return new(openDirectors, diskDirectors);
        }

        /// <summary>Forgets the cached disk scan of <see cref="CountOtherUsers"/>.</summary>
        public static void ClearUsageCache()
        {
            s_diskUsers.Clear();
        }

        [InitializeOnLoadMethod]
        private static void ClearUsageCacheOnProjectChange()
        {
            EditorApplication.projectChanged += ClearUsageCache;
        }

        private static int BeginGroup(string name)
        {
            Undo.IncrementCurrentGroup();
            Undo.SetCurrentGroupName(name);
            return Undo.GetCurrentGroup();
        }

        private static void AddCore(
              TweenPreset preset
            , TimelineAsset target
            , PlayableDirector director
            , GameObject bindingRoot
            , double startTime
            , PresetRecipeResult result
        )
        {
            var source = preset.Timeline;

            if (source.IsInvalid())
            {
                result.Warnings.Add($"Preset '{preset.name}' has no timeline; nothing was added.");
                return;
            }

            var canBind = director.IsValid() && director.playableAsset == target;

            if (canBind == false)
            {
                result.Warnings.Add(
                    "No director plays this timeline (asset mode or a sub-timeline): the tracks were added unbound."
                );
            }
            else if (bindingRoot.IsInvalid())
            {
                bindingRoot = GetDefaultBindingRoot(director);
            }

            var binder = new Binder(preset, director, bindingRoot, result);
            var sourceTracks = source.GetOutputTracks().ToList();
            var trackCount = sourceTracks.Count;

            for (var i = 0; i < trackCount; i++)
            {
                var sourceTrack = sourceTracks[i];
                var track = target.CreateTrack(sourceTrack.GetType(), null, sourceTrack.name);

                CopyTrackSettings(sourceTrack, track);
                AddClips(sourceTrack, track, startTime, result);
                result.Tracks.Add(track);

                if (canBind)
                {
                    binder.Bind(sourceTrack.name, track);
                }
            }

            TimelineEditor.Refresh(RefreshReason.ContentsAddedOrRemoved);

            // A prefab or scene save does not write the .playable.
            AssetDatabase.SaveAssetIfDirty(target);
        }

        /// <summary>
        /// Copies the package's track settings (restore mode, send callbacks) that the track type has. Never
        /// <c>CopySerialized</c>: that would copy the clip list too.
        /// </summary>
        private static void CopyTrackSettings(TrackAsset source, TrackAsset track)
        {
            var from = new SerializedObject(source);
            var to = new SerializedObject(track);

            for (var i = 0; i < s_trackSettings.Length; i++)
            {
                var property = from.FindProperty(s_trackSettings[i]);

                if (property != null)
                {
                    to.CopyFromSerializedProperty(property);
                }
            }

            to.ApplyModifiedProperties();
        }

        private static void AddClips(
              TrackAsset sourceTrack
            , TrackAsset track
            , double startTime
            , PresetRecipeResult result
        )
        {
            var sourceClips = sourceTrack.GetClips().ToList();
            var count = sourceClips.Count;
            var created = new List<TimelineClip>(count);

            for (var i = 0; i < count; i++)
            {
                var sourceClip = sourceClips[i];
                var clip = track.CreateDefaultClip();

                if (clip == null)
                {
                    continue;
                }

                clip.start = startTime + sourceClip.start;
                clip.duration = sourceClip.duration;
                clip.easeInDuration = sourceClip.easeInDuration;
                clip.easeOutDuration = sourceClip.easeOutDuration;
                clip.displayName = sourceClip.displayName;

                if (sourceClip.asset is Object from && clip.asset is Object to)
                {
                    CopyTemplate(from, to);
                }

                created.Add(clip);
                result.Clips.Add(clip);
            }

            SetExtrapolation(track, sourceClips, created);
        }

        private static void SetExtrapolation(
              TrackAsset track
            , List<TimelineClip> sourceClips
            , List<TimelineClip> created
        )
        {
            var serializedTrack = new SerializedObject(track);
            var clips = track.GetClips().ToList();
            var count = created.Count;

            for (var i = 0; i < count; i++)
            {
                var index = clips.IndexOf(created[i]);

                if (index < 0)
                {
                    continue;
                }

                var prefix = string.Format(CLIP_PATH_FORMAT, index);
                var sourceClip = sourceClips[i];

                var pre = serializedTrack.FindProperty(prefix + PRE_EXTRAPOLATION);
                var post = serializedTrack.FindProperty(prefix + POST_EXTRAPOLATION);

                pre.intValue = (int)sourceClip.preExtrapolationMode;
                post.intValue = (int)sourceClip.postExtrapolationMode;
            }

            serializedTrack.ApplyModifiedProperties();
        }

        private static void CopyTemplate(Object from, Object to)
        {
            var template = new SerializedObject(from).FindProperty(TweenClipModel.TEMPLATE);

            if (template == null)
            {
                return;
            }

            var serializedClip = new SerializedObject(to);
            serializedClip.CopyFromSerializedProperty(template);
            serializedClip.ApplyModifiedProperties();
        }

        private static PlayableAsset FindFirstClipAsset(TweenPreset preset, Type clipType)
        {
            var clips = FindClips(preset, clipType);
            return clips.Count > 0 ? clips[0].Asset : null;
        }

        private static TimelineAsset DuplicateForDirector(
              PlayableDirector director
            , GameObject bindingRoot
            , out string warning
        )
        {
            warning = string.Empty;

            if (director.IsInvalid() || director.playableAsset is not TimelineAsset original || original.IsInvalid())
            {
                warning = "No director plays a timeline asset; nothing was duplicated.";
                return null;
            }

            var path = AssetDatabase.GetAssetPath(original);

            if (string.IsNullOrEmpty(path))
            {
                warning = $"Timeline '{original.name}' is not an asset on disk; nothing was duplicated.";
                return null;
            }

            var owner = bindingRoot.IsValid() ? bindingRoot : GetDefaultBindingRoot(director);
            var folder = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? string.Empty;
            var copyPath = AssetDatabase.GenerateUniqueAssetPath(
                $"{folder}/{original.name}-{owner.name}{PLAYABLE_EXTENSION}"
            );

            if (AssetDatabase.CopyAsset(path, copyPath) == false)
            {
                warning = $"Could not copy '{path}' to '{copyPath}'; nothing was added.";
                return null;
            }

            var copy = AssetDatabase.LoadAssetAtPath<TimelineAsset>(copyPath);

            // Save the copy before any binding refers to it (a timeline that was never saved binds as fileID 0).
            AssetDatabase.SaveAssetIfDirty(copy);

            Undo.RegisterCompleteObjectUndo(director, ADD_UNDO_NAME);

            var originalTracks = original.GetOutputTracks().ToList();
            var copyTracks = copy.GetOutputTracks().ToList();
            var count = Math.Min(originalTracks.Count, copyTracks.Count);

            for (var i = 0; i < count; i++)
            {
                var binding = director.GetGenericBinding(originalTracks[i]);

                if (binding.IsValid())
                {
                    director.SetGenericBinding(copyTracks[i], binding);
                }
            }

            director.playableAsset = copy;
            RecordPrefabInstance(director);
            return copy;
        }

        private static void RecordPrefabInstance(PlayableDirector director)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(director))
            {
                PrefabUtility.RecordPrefabInstancePropertyModifications(director);
            }
        }

        private static int CountOpenDirectors(
              TimelineAsset timeline
            , PlayableDirector director
            , HashSet<string> openFiles
        )
        {
            var directors = new HashSet<PlayableDirector>();

            directors.UnionWith(
                Object.FindObjectsByType<PlayableDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            );

            for (var i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);

                if (scene.isLoaded && string.IsNullOrEmpty(scene.path) == false)
                {
                    openFiles.Add(scene.path);
                }
            }

            var stage = PrefabStageUtility.GetCurrentPrefabStage();

            if (stage != null && stage.prefabContentsRoot.IsValid())
            {
                directors.UnionWith(stage.prefabContentsRoot.GetComponentsInChildren<PlayableDirector>(true));
                openFiles.Add(stage.assetPath);
            }

            var count = 0;

            foreach (var each in directors)
            {
                if (each.IsValid() && each != director && each.playableAsset == timeline)
                {
                    count++;
                }
            }

            return count;
        }

        private static List<DiskUser> GetDiskUsers(TimelineAsset timeline)
        {
            var path = AssetDatabase.GetAssetPath(timeline);

            if (string.IsNullOrEmpty(path))
            {
                return new();
            }

            var guid = AssetDatabase.AssetPathToGUID(path);

            if (s_diskUsers.TryGetValue(guid, out var cached))
            {
                return cached;
            }

            var users = new List<DiskUser>();
            var guids = AssetDatabase.FindAssets(DISK_USER_FILTER);

            for (var i = 0; i < guids.Length; i++)
            {
                var userPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                var dependencies = AssetDatabase.GetDependencies(userPath, false);

                if (Array.IndexOf(dependencies, path) < 0)
                {
                    continue;
                }

                var directorCount = CountDiskDirectors(userPath, timeline);

                if (directorCount > 0)
                {
                    users.Add(new(userPath, directorCount));
                }
            }

            s_diskUsers[guid] = users;
            return users;
        }

        /// <summary>
        /// The directors of a prefab that play <paramref name="timeline"/>. A scene is not opened: it counts as one.
        /// </summary>
        private static int CountDiskDirectors(string path, TimelineAsset timeline)
        {
            if (path.EndsWith(PREFAB_EXTENSION, StringComparison.OrdinalIgnoreCase) == false)
            {
                return 1;
            }

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab.IsInvalid())
            {
                return 0;
            }

            var directors = prefab.GetComponentsInChildren<PlayableDirector>(true);
            var count = 0;

            for (var i = 0; i < directors.Length; i++)
            {
                if (directors[i].playableAsset == timeline)
                {
                    count++;
                }
            }

            return count;
        }

        private readonly record struct DiskUser(string Path, int DirectorCount);

        /// <summary>
        /// Binds created tracks by the preset's paths from the binding root, recording the director once.
        /// </summary>
        private sealed class Binder
        {
            private readonly TweenPreset _preset;
            private readonly PlayableDirector _director;
            private readonly GameObject _root;
            private readonly PresetRecipeResult _result;

            private bool _recorded;

            public Binder(TweenPreset preset, PlayableDirector director, GameObject root, PresetRecipeResult result)
            {
                _preset = preset;
                _director = director;
                _root = root;
                _result = result;
            }

            public void Bind(string trackName, TrackAsset track)
            {
                if (_preset.TryGetPath(trackName, out var path) == false)
                {
                    _result.Warnings.Add($"Track '{track.name}': the preset gives no binding path; it is unbound.");
                    return;
                }

                if (_root.IsInvalid())
                {
                    _result.Warnings.Add($"Track '{track.name}': there is no binding root; it is unbound.");
                    return;
                }

                var transform = string.IsNullOrEmpty(path) ? _root.transform : _root.transform.Find(path);

                if (transform.IsInvalid())
                {
                    _result.Warnings.Add(
                        $"Track '{track.name}': '{_root.name}' has no child at '{path}'; the track is unbound."
                    );
                    return;
                }

                var bindingType = GetBindingType(track);
                var binding = ResolveBinding(transform, bindingType);

                if (binding.IsInvalid())
                {
                    _result.Warnings.Add(
                        $"Track '{track.name}': '{path}' has no {bindingType?.Name}; the track is unbound."
                    );
                    return;
                }

                if (_recorded == false)
                {
                    Undo.RegisterCompleteObjectUndo(_director, ADD_UNDO_NAME);
                    _recorded = true;
                }

                _director.SetGenericBinding(track, binding);
                RecordPrefabInstance(_director);
            }

            private static Type GetBindingType(TrackAsset track)
            {
                var attributes = track.GetType().GetCustomAttributes(typeof(TrackBindingTypeAttribute), true);
                return attributes.Length > 0 ? ((TrackBindingTypeAttribute)attributes[0]).type : null;
            }

            private static Object ResolveBinding(Transform transform, Type type)
            {
                if (type == null)
                {
                    return null;
                }

                if (type == typeof(GameObject))
                {
                    return transform.gameObject;
                }

                return typeof(Component).IsAssignableFrom(type) ? transform.GetComponent(type) : null;
            }
        }
    }

    /// <summary>
    /// What a <see cref="PresetRecipe"/> call did and the warnings to show.
    /// </summary>
    internal sealed class PresetRecipeResult
    {
        public readonly List<string> Warnings = new();
        public readonly List<TrackAsset> Tracks = new();
        public readonly List<TimelineClip> Clips = new();
        public readonly List<PlayableAsset> AppliedClipAssets = new();

        public PresetRecipeResult(TimelineAsset timeline)
        {
            Timeline = timeline;
        }

        /// <summary>The timeline the tracks were added to (the copy after a duplicate), or null.</summary>
        public TimelineAsset Timeline { get; }
    }

    /// <summary>
    /// One clip of a preset timeline, with its track.
    /// </summary>
    internal readonly record struct PresetClip(TrackAsset Track, TimelineClip Clip, PlayableAsset Asset);

    /// <summary>
    /// The other users of a timeline asset: directors open in the Editor and directors in files on disk.
    /// </summary>
    internal readonly record struct SharedTimelineUsage(int OpenDirectors, int DiskDirectors)
    {
        public int Total => OpenDirectors + DiskDirectors;

        public bool IsShared => Total > 0;
    }
}
