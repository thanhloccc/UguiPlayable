using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Editor.Presets;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Editor;

public sealed class PresetContentTests
{
    private const string FOLDER_PARENT = "Assets";
    private const string FOLDER_NAME = "UguiPlayablePresetContentTestsTemp";
    private const string FOLDER = FOLDER_PARENT + "/" + FOLDER_NAME;
    private const string LIBRARY = PresetPickerStyleSheetPaths.ROOT + "/Library/";
    private const string SAMPLE_FILTER = "UguiPlayable.Samples";
    private const string ENABLED = "_enabled";
    private const string MODE = "_mode";
    private const string TEMPLATE = "_template";
    private const string RESTORE_MODE = "_restoreMode";
    private const int RELATIVE = (int)ValueMode.Relative;
    private const int REVERT = (int)RestoreMode.Revert;
    private const string OUT_SUFFIX = "-out";

    private static readonly string[] s_expectedNames = {
        "popup-pop-in", "popup-pop-out", "popup-slide-up-in", "popup-slide-down-out", "popup-fade-in",
        "popup-fade-out", "screen-fade-in", "screen-fade-out", "screen-push-left-in", "screen-push-left-out",
        "screen-push-right-in", "screen-push-right-out", "screen-zoom-in", "toast-drop-in", "toast-rise-out",
        "button-press", "button-pop", "attention-shake", "attention-wiggle", "attention-pulse", "text-typewriter",
        "text-scramble-reveal", "text-char-fade-in", "text-char-bounce-in", "text-wave", "list-stagger-fade-up",
        "list-stagger-pop",
    };

    // Only these presets move a position by an offset (D21); every other preset is Absolute or From current.
    private static readonly HashSet<string> s_relativePresets = new() {
        "popup-slide-up-in", "popup-slide-down-out", "toast-drop-in", "toast-rise-out",
    };

    private readonly List<GameObject> _objects = new();

    [TearDown]
    public void TearDown()
    {
        for (var i = _objects.Count - 1; i >= 0; i--)
        {
            if (_objects[i].IsValid())
            {
                Object.DestroyImmediate(_objects[i]);
            }
        }

        _objects.Clear();
        AssetDatabase.DeleteAsset(FOLDER);
    }

    [Test]
    public void Library_HasTheStarterSet()
    {
        var names = GetLibrary().Select(static x => x.name).ToList();

        foreach (var expected in s_expectedNames)
        {
            CollectionAssert.Contains(names, expected);
        }

        CollectionAssert.AllItemsAreUnique(names);
    }

    [Test]
    public void EveryPreset_HasATimelineIconCategoryAndNoProblems()
    {
        foreach (var preset in GetLibrary())
        {
            Assert.IsTrue(preset.Timeline.IsValid(), $"{preset.name}: no timeline");
            Assert.IsNotEmpty(preset.Category, $"{preset.name}: no category");
            Assert.IsNotEmpty(preset.Description, $"{preset.name}: no description");
            Assert.IsTrue(TweenPreset.IsKnownIcon(preset._icon), $"{preset.name}: unknown icon '{preset._icon}'");
            Assert.IsEmpty(
                  preset.GetProblems().Select(static x => x.ToMessage()).ToList()
                , $"{preset.name}: problems"
            );
        }
    }

    [Test]
    public void EveryTrackName_ExistsAndEveryTrackHasAClip()
    {
        foreach (var preset in GetLibrary())
        {
            var tracks = preset.Timeline.GetOutputTracks().ToList();
            var trackNames = tracks.Select(static x => x.name).ToList();

            Assert.IsNotEmpty(tracks, $"{preset.name}: no track");

            foreach (var binding in preset._bindings)
            {
                CollectionAssert.Contains(trackNames, binding._trackName, $"{preset.name}: binding track");
            }

            foreach (var track in tracks)
            {
                Assert.IsNotEmpty(track.GetClips().ToList(), $"{preset.name}/{track.name}: no clip");
            }
        }
    }

    [Test]
    public void EveryClip_HasAnEnabledChannel()
    {
        foreach (var preset in GetLibrary())
        {
            foreach (var clip in GetClips(preset))
            {
                Assert.IsTrue(
                      HasEnabledChannel(new SerializedObject(clip.asset))
                    , $"{preset.name}/{clip.displayName}: no enabled channel"
                );
            }
        }
    }

    [Test]
    public void OnlyTheSlidePresets_UseRelative()
    {
        foreach (var preset in GetLibrary())
        {
            var usesRelative = GetClips(preset).Any(static x => UsesRelative(new SerializedObject(x.asset)));

            Assert.AreEqual(s_relativePresets.Contains(preset.name), usesRelative, $"{preset.name}: Relative");
        }
    }

    [Test]
    public void TextScrambleReveal_HasTheTypewriterOff()
    {
        const string PRESET = "text-scramble-reveal";
        const string VISIBLE_RATIO_ENABLED = "_template._visibleRatio._enabled";
        const string TEXT_ENABLED = "_template._text._enabled";

        var preset = GetLibrary().Single(static x => x.name == PRESET);
        var clips = GetClips(preset);

        Assert.IsNotEmpty(clips);

        foreach (var clip in clips)
        {
            var serialized = new SerializedObject(clip.asset);

            Assert.IsFalse(serialized.FindProperty(VISIBLE_RATIO_ENABLED).boolValue, $"{clip.displayName}: typewriter");
            Assert.IsTrue(serialized.FindProperty(TEXT_ENABLED).boolValue, $"{clip.displayName}: string tween");
        }
    }

    [Test]
    public void SlideHideTracks_AreRevert()
    {
        foreach (var preset in GetLibrary())
        {
            foreach (var track in preset.Timeline.GetOutputTracks())
            {
                var relative = track.GetClips().Any(static x => UsesRelative(new SerializedObject(x.asset)));

                if (relative == false)
                {
                    continue;
                }

                var revert = new SerializedObject(track).FindProperty(RESTORE_MODE).intValue == REVERT;

                Assert.AreEqual(preset.name.EndsWith(OUT_SUFFIX, StringComparison.Ordinal), revert, preset.name);
            }
        }
    }

    [Test]
    public void NoPresetClipAsset_IsReferencedFromOutsideItsOwnTimeline()
    {
        foreach (var preset in GetLibrary())
        {
            var timelinePath = AssetDatabase.GetAssetPath(preset.Timeline);
            var presetPath = AssetDatabase.GetAssetPath(preset);

            foreach (var dependency in AssetDatabase.GetDependencies(timelinePath, false))
            {
                Assert.IsFalse(
                      dependency.StartsWith(LIBRARY, StringComparison.Ordinal) && dependency != timelinePath
                    , $"{preset.name}: its timeline depends on {dependency}"
                );
            }

            foreach (var dependency in AssetDatabase.GetDependencies(presetPath, false))
            {
                var inLibrary = dependency.StartsWith(LIBRARY, StringComparison.Ordinal);
                Assert.IsFalse(
                      inLibrary && dependency != presetPath && dependency != timelinePath
                    , $"{preset.name}: it depends on {dependency}"
                );
            }

            var clips = GetClips(preset).Select(static x => x.asset).ToList();
            var libraryPaths = AssetDatabase.FindAssets(string.Empty, new[] { LIBRARY.TrimEnd('/') })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path != timelinePath && path.EndsWith(".playable", StringComparison.Ordinal))
                .ToList();

            foreach (var path in libraryPaths)
            {
                foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(path))
                {
                    Assert.IsFalse(clips.Contains(asset), $"{preset.name}: its clip is inside {path}");
                }
            }
        }
    }

    [Test]
    public void EveryPreset_AddsToItsSamplePrefabWithoutWarningsAndBindsEveryTrack()
    {
        var popup = GetSamplePrefabPath("popup-confirm");
        var screen = GetSamplePrefabPath("screen-home");
        var toast = GetSamplePrefabPath("overlay-toast");

        if (string.IsNullOrEmpty(popup) || string.IsNullOrEmpty(screen) || string.IsNullOrEmpty(toast))
        {
            Assert.Ignore("The UGUI Playable sample is not imported.");
        }

        if (AssetDatabase.IsValidFolder(FOLDER) == false)
        {
            AssetDatabase.CreateFolder(FOLDER_PARENT, FOLDER_NAME);
        }

        var contents = new List<GameObject>();

        try
        {
            foreach (var preset in GetLibrary())
            {
                var category = preset.Category;

                if (category == "list")
                {
                    AssertStagger(preset);
                    continue;
                }

                var prefabPath = category switch {
                    "screen" => screen,
                    "toast" => toast,
                    _ => popup,
                };

                var root = PrefabUtility.LoadPrefabContents(prefabPath);
                contents.Add(root);

                var bindingRoot = category switch {
                    "button" or "attention" => FindDeep(root.transform, "button-yes").gameObject,
                    "text" => FindDeep(root.transform, "label-title").gameObject,
                    _ => root,
                };

                var target = CreateTimeline($"{FOLDER}/{preset.name}-target.playable");
                var director = root.AddComponent<PlayableDirector>();
                director.playableAsset = target;

                var result = PresetRecipe.AddToTimeline(preset, target, director, bindingRoot, 0d);

                Assert.IsEmpty(result.Warnings, $"{preset.name}: warnings");
                Assert.AreEqual(preset.Timeline.GetOutputTracks().Count(), result.Tracks.Count, preset.name);

                foreach (var track in result.Tracks)
                {
                    Assert.IsTrue(director.GetGenericBinding(track).IsValid(), $"{preset.name}/{track.name}: unbound");
                }
            }
        }
        finally
        {
            foreach (var root in contents)
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }

    private void AssertStagger(TweenPreset preset)
    {
        var container = new GameObject("list", typeof(RectTransform));
        _objects.Add(container);

        for (var i = 0; i < 3; i++)
        {
            var item = new GameObject($"item-{i}", typeof(RectTransform), typeof(CanvasGroup));
            item.transform.SetParent(container.transform, false);
        }

        if (AssetDatabase.IsValidFolder(FOLDER) == false)
        {
            AssetDatabase.CreateFolder(FOLDER_PARENT, FOLDER_NAME);
        }

        var target = CreateTimeline($"{FOLDER}/{preset.name}-target.playable");
        var director = container.AddComponent<PlayableDirector>();
        director.playableAsset = target;

        var result = StaggerRecipe.Stagger(preset, target, director, container, 0d, 0.1d);

        Assert.IsEmpty(result.Warnings, $"{preset.name}: warnings");
        Assert.AreEqual(3, result.Tracks.Count, preset.name);

        foreach (var track in result.Tracks)
        {
            Assert.IsTrue(director.GetGenericBinding(track).IsValid(), $"{preset.name}/{track.name}: unbound");
        }
    }

    private static List<TweenPreset> GetLibrary()
    {
        var presets = TweenPreset.FindAll()
            .Where(static x => AssetDatabase.GetAssetPath(x).StartsWith(LIBRARY, StringComparison.Ordinal))
            .ToList();

        Assert.IsNotEmpty(presets);
        return presets;
    }

    private static List<TimelineClip> GetClips(TweenPreset preset)
        => preset.Timeline.GetOutputTracks().SelectMany(static x => x.GetClips()).ToList();

    private static TimelineAsset CreateTimeline(string path)
    {
        var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(timeline, path);
        return timeline;
    }

    private static string GetSamplePrefabPath(string name)
    {
        foreach (var guid in AssetDatabase.FindAssets($"{name} t:Prefab"))
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);

            if (path.Contains(SAMPLE_FILTER, StringComparison.Ordinal)
                && string.Equals(System.IO.Path.GetFileNameWithoutExtension(path), name, StringComparison.Ordinal))
            {
                return path;
            }
        }

        return null;
    }

    private static Transform FindDeep(Transform root, string name)
    {
        foreach (var child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
            {
                return child;
            }
        }

        Assert.Fail($"No '{name}' below '{root.name}'.");
        return null;
    }

    // A channel is a struct with an "_enabled" field; the character track's channels and the oscillation channels
    // use the same field name.
    private static bool HasEnabledChannel(SerializedObject clip)
    {
        var iterator = clip.FindProperty(TEMPLATE);
        var end = iterator.GetEndProperty();

        while (iterator.Next(true) && SerializedProperty.EqualContents(iterator, end) == false)
        {
            if (iterator.name == ENABLED && iterator.propertyType == SerializedPropertyType.Boolean
                && iterator.boolValue)
            {
                return true;
            }
        }

        return false;
    }

    private static bool UsesRelative(SerializedObject clip)
    {
        var iterator = clip.FindProperty(TEMPLATE);
        var end = iterator.GetEndProperty();

        while (iterator.Next(true) && SerializedProperty.EqualContents(iterator, end) == false)
        {
            if (iterator.name != MODE || iterator.propertyType != SerializedPropertyType.Enum
                || iterator.enumValueIndex != RELATIVE)
            {
                continue;
            }

            var path = iterator.propertyPath;
            var channel = path.Substring(0, path.Length - MODE.Length);
            var enabled = clip.FindProperty(channel + ENABLED);

            if (enabled != null && enabled.boolValue)
            {
                return true;
            }
        }

        return false;
    }
}
