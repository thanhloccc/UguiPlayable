using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UIElements;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Editor.Clips;
using ApexionGame.UguiPlayable.Editor.Presets;
using ApexionGame.UguiPlayable.Tracks.Groups;
using ApexionGame.UguiPlayable.Tracks.Transforms;
using Button = UnityEngine.UIElements.Button;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Editor;

public sealed class PresetRecipeTests
{
    private const string FOLDER_PARENT = "Assets";
    private const string FOLDER_NAME = "UguiPlayablePresetTestsTemp";
    private const string FOLDER = FOLDER_PARENT + "/" + FOLDER_NAME;
    private const string PRESET_TIMELINE_PATH = FOLDER + "/preset.playable";
    private const string TARGET_TIMELINE_PATH = FOLDER + "/target.playable";
    private const string PREFAB_PATH = FOLDER + "/user.prefab";
    private const string LIBRARY = PresetPickerStyleSheetPaths.ROOT + "/Library/Popup/";
    private const string BACKDROP = "backdrop";
    private const string POPUP_ALPHA = "popup-alpha";
    private const string BACKDROP_PATH = "backdrop";
    private const string POPUP_PATH = "body/popup";
    private const string ALPHA_ENABLED = "_template._alpha._enabled";
    private const string ALPHA_MODE = "_template._alpha._mode";
    private const string ALPHA_FROM = "_template._alpha._from";
    private const string ALPHA_TO = "_template._alpha._to";
    private const string RESTORE_MODE = "_restoreMode";
    private const double START = 1.5d;

    private readonly List<Object> _objects = new();

    private TimelineAsset _presetTimeline;
    private TweenPreset _preset;
    private TimelineAsset _target;
    private GameObject _root;
    private PlayableDirector _director;

    [SetUp]
    public void SetUp()
    {
        if (AssetDatabase.IsValidFolder(FOLDER) == false)
        {
            AssetDatabase.CreateFolder(FOLDER_PARENT, FOLDER_NAME);
        }

        PresetRecipe.ClearUsageCache();
        _presetTimeline = CreatePresetTimeline();
        _preset = CreatePreset(_presetTimeline, POPUP_PATH);
        _target = CreateTimelineAsset(TARGET_TIMELINE_PATH);
        _root = CreatePopupHierarchy(out _director);
        _director.playableAsset = _target;
    }

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

        if (_root.IsValid())
        {
            Object.DestroyImmediate(_root);
        }

        AssetDatabase.DeleteAsset(FOLDER);
        PresetRecipe.ClearUsageCache();

        _presetTimeline = null;
        _preset = null;
        _target = null;
        _root = null;
        _director = null;
    }

    [Test]
    public void AddToTimeline_CreatesTracksClipsTimingTemplateAndExtrapolation()
    {
        var result = PresetRecipe.AddToTimeline(_preset, _target, _director, null, START);
        var sourceTracks = _presetTimeline.GetOutputTracks().ToList();
        var tracks = _target.GetOutputTracks().ToList();

        Assert.AreEqual(2, tracks.Count);
        CollectionAssert.AreEqual(tracks, result.Tracks);

        for (var i = 0; i < tracks.Count; i++)
        {
            var source = sourceTracks[i];
            var track = tracks[i];

            Assert.AreEqual(source.name, track.name);
            Assert.AreEqual(source.GetType(), track.GetType());
            Assert.AreEqual(GetInt(source, RESTORE_MODE), GetInt(track, RESTORE_MODE));

            var sourceClip = source.GetClips().Single();
            var clip = track.GetClips().Single();

            Assert.AreEqual(START + sourceClip.start, clip.start, 1e-9);
            Assert.AreEqual(sourceClip.duration, clip.duration, 1e-9);
            Assert.AreEqual(sourceClip.easeInDuration, clip.easeInDuration, 1e-9);
            Assert.AreEqual(sourceClip.easeOutDuration, clip.easeOutDuration, 1e-9);
            Assert.AreEqual(sourceClip.displayName, clip.displayName);
            Assert.AreEqual(sourceClip.preExtrapolationMode, clip.preExtrapolationMode);
            Assert.AreEqual(sourceClip.postExtrapolationMode, clip.postExtrapolationMode);
            Assert.AreEqual(GetFloat(sourceClip.asset, ALPHA_FROM), GetFloat(clip.asset, ALPHA_FROM));
            Assert.AreEqual(GetFloat(sourceClip.asset, ALPHA_TO), GetFloat(clip.asset, ALPHA_TO));
            Assert.AreEqual(GetInt(sourceClip.asset, ALPHA_MODE), GetInt(clip.asset, ALPHA_MODE));
        }

        Assert.AreEqual((int)RestoreMode.Revert, GetInt(tracks[1], RESTORE_MODE));
        Assert.AreEqual(TimelineClip.ClipExtrapolation.Hold, tracks[0].GetClips().Single().postExtrapolationMode);
        Assert.AreEqual(TimelineClip.ClipExtrapolation.Hold, tracks[1].GetClips().Single().preExtrapolationMode);
    }

    [Test]
    public void AddToTimeline_BindsByPathFromTheBindingRoot()
    {
        var result = PresetRecipe.AddToTimeline(_preset, _target, _director, null, START);
        var tracks = _target.GetOutputTracks().ToList();
        var backdrop = _root.transform.Find(BACKDROP_PATH).GetComponent<CanvasGroup>();
        var popup = _root.transform.Find(POPUP_PATH).GetComponent<CanvasGroup>();

        Assert.IsEmpty(result.Warnings);
        Assert.AreSame(backdrop, _director.GetGenericBinding(tracks[0]));
        Assert.AreSame(popup, _director.GetGenericBinding(tracks[1]));
    }

    [Test]
    public void AddToTimeline_MissingPathGivesOneWarningAndAnUnboundTrack()
    {
        var preset = CreatePreset(_presetTimeline, "body/missing");
        var result = PresetRecipe.AddToTimeline(preset, _target, _director, null, START);
        var tracks = _target.GetOutputTracks().ToList();

        Assert.AreEqual(1, result.Warnings.Count);
        Assert.IsNotNull(_director.GetGenericBinding(tracks[0]));
        Assert.IsNull(_director.GetGenericBinding(tracks[1]));
    }

    [Test]
    public void AddToTimeline_IsUndoneInOneStep()
    {
        var bindingCount = GetSceneBindingCount(_director);

        PresetRecipe.AddToTimeline(_preset, _target, _director, null, START);

        Assert.AreEqual(2, _target.GetOutputTracks().Count());
        Assert.Greater(GetSceneBindingCount(_director), bindingCount);

        Undo.PerformUndo();

        Assert.AreEqual(0, _target.GetOutputTracks().Count());
        Assert.AreEqual(bindingCount, GetSceneBindingCount(_director));
    }

    [Test]
    public void AddToTimeline_WithoutADirectorPlayingTheTimelineAddsUnboundTracks()
    {
        var assetMode = PresetRecipe.AddToTimeline(_preset, _target, null, null, START);

        Assert.AreEqual(2, assetMode.Tracks.Count);
        Assert.AreEqual(1, assetMode.Warnings.Count);

        var other = CreateTimelineAsset(FOLDER + "/other.playable");
        var bindingCount = GetSceneBindingCount(_director);
        var subTimeline = PresetRecipe.AddToTimeline(_preset, other, _director, null, START);

        Assert.AreEqual(2, subTimeline.Tracks.Count);
        Assert.AreEqual(1, subTimeline.Warnings.Count);
        Assert.AreEqual(bindingCount, GetSceneBindingCount(_director));
    }

    [Test]
    public void GetDefaultBindingRoot_IsTheParentOfATransitionChild()
    {
        Assert.AreSame(_root, PresetRecipe.GetDefaultBindingRoot(_director));

        var plain = Track(new GameObject("popup-plain"));
        var director = plain.AddComponent<PlayableDirector>();

        Assert.AreSame(plain, PresetRecipe.GetDefaultBindingRoot(director));
    }

    [Test]
    public void CountOtherUsers_CountsOpenDirectorsAndAPrefabOnDisk()
    {
        Assert.IsFalse(PresetRecipe.CountOtherUsers(_target, _director).IsShared);

        var open = Track(new GameObject("open-user")).AddComponent<PlayableDirector>();
        open.playableAsset = _target;

        var onDisk = new GameObject("disk-user");
        onDisk.AddComponent<PlayableDirector>().playableAsset = _target;
        PrefabUtility.SaveAsPrefabAsset(onDisk, PREFAB_PATH);
        Object.DestroyImmediate(onDisk);
        PresetRecipe.ClearUsageCache();

        var usage = PresetRecipe.CountOtherUsers(_target, _director);

        Assert.AreEqual(1, usage.OpenDirectors);
        Assert.AreEqual(1, usage.DiskDirectors);
        Assert.AreEqual(2, usage.Total);
    }

    [Test]
    public void DuplicateAndAdd_PlaysACopyAndLeavesTheOriginalUnchanged()
    {
        var existing = _target.CreateTrack<CanvasGroupTrack>(null, "existing");
        var existingBinding = _root.GetComponent<CanvasGroup>();
        _director.SetGenericBinding(existing, existingBinding);
        AssetDatabase.SaveAssetIfDirty(_target);

        var before = File.ReadAllBytes(TARGET_TIMELINE_PATH);
        var result = PresetRecipe.DuplicateAndAdd(_preset, _director, null, START);
        var copy = result.Timeline;

        Assert.IsTrue(copy.IsValid());
        Assert.AreNotSame(_target, copy);
        Assert.AreSame(copy, _director.playableAsset);
        Assert.AreEqual(
              Path.GetDirectoryName(TARGET_TIMELINE_PATH)
            , Path.GetDirectoryName(AssetDatabase.GetAssetPath(copy))
        );

        var copyTracks = copy.GetOutputTracks().ToList();

        Assert.AreEqual(3, copyTracks.Count);
        Assert.AreSame(existingBinding, _director.GetGenericBinding(copyTracks[0]));
        Assert.IsNotNull(_director.GetGenericBinding(copyTracks[1]));
        Assert.AreEqual(1, _target.GetOutputTracks().Count());
        CollectionAssert.AreEqual(before, File.ReadAllBytes(TARGET_TIMELINE_PATH));
    }

    [Test]
    public void Icon_FallsBackToTheDefaultWhenUnknownOrEmpty()
    {
        _preset._icon = "no-such-icon";

        Assert.AreEqual(TweenPreset.DEFAULT_ICON, _preset.IconName);
        Assert.IsNotNull(_preset.Icon);
        Assert.IsTrue(_preset.GetProblems().Any(static p => p.Kind == TweenPresetProblemKind.UnknownIcon));

        _preset._icon = string.Empty;

        Assert.AreEqual(TweenPreset.DEFAULT_ICON, _preset.IconName);

        _preset._icon = "blend";

        Assert.AreEqual("blend", _preset.IconName);
        Assert.IsFalse(_preset.GetProblems().Any(static p => p.Kind == TweenPresetProblemKind.UnknownIcon));
    }

    [Test]
    public void GetProblems_ReportsAnUnknownTrackAndATrackWithoutPath()
    {
        Assert.IsEmpty(_preset.GetProblems());

        _preset._bindings = new[] {
            new TweenPresetBinding(BACKDROP, BACKDROP_PATH),
            new TweenPresetBinding("no-such-track", POPUP_PATH),
        };

        var kinds = _preset.GetProblems().Select(static p => p.Kind).ToList();

        CollectionAssert.Contains(kinds, TweenPresetProblemKind.UnknownTrack);
        CollectionAssert.Contains(kinds, TweenPresetProblemKind.TrackWithoutPath);
    }

    [Test]
    public void ApplyToClips_KeepsTimingAndNameCopiesTheTemplateAndUndoesInOneStep()
    {
        var track = _target.CreateTrack<CanvasGroupTrack>(null, "mine");
        var first = CreateClip(track, 2d, 0.7d, "first", 0.5f, 0.6f);
        var second = CreateClip(track, 3d, 0.4d, "second", 0.3f, 0.2f);
        var source = _presetTimeline.GetOutputTracks().ToList()[1].GetClips().Single().asset as PlayableAsset;
        var assets = new List<PlayableAsset> { first.asset as PlayableAsset, second.asset as PlayableAsset };

        var result = PresetRecipe.ApplyToClips(_preset, assets, source);

        Assert.IsEmpty(result.Warnings);
        Assert.AreEqual(2, result.AppliedClipAssets.Count);

        foreach (var clip in new[] { first, second })
        {
            Assert.AreEqual(GetFloat(source, ALPHA_FROM), GetFloat(clip.asset, ALPHA_FROM));
            Assert.AreEqual(GetFloat(source, ALPHA_TO), GetFloat(clip.asset, ALPHA_TO));
        }

        Assert.AreEqual(2d, first.start, 1e-9);
        Assert.AreEqual(0.7d, first.duration, 1e-9);
        Assert.AreEqual("first", first.displayName);
        Assert.AreEqual(3d, second.start, 1e-9);
        Assert.AreEqual("second", second.displayName);

        Undo.PerformUndo();

        Assert.AreEqual(0.5f, GetFloat(first.asset, ALPHA_FROM));
        Assert.AreEqual(0.6f, GetFloat(first.asset, ALPHA_TO));
        Assert.AreEqual(0.3f, GetFloat(second.asset, ALPHA_FROM));
        Assert.AreEqual(0.2f, GetFloat(second.asset, ALPHA_TO));
    }

    [Test]
    public void ApplyToClips_WithoutAMatchingClipWarnsAndLeavesTheClip()
    {
        var track = _target.CreateTrack<RectTransformTrack>(null, "rect");
        var clip = track.CreateDefaultClip();

        var result = PresetRecipe.ApplyToClips(_preset, new[] { clip.asset as PlayableAsset });

        Assert.AreEqual(1, result.Warnings.Count);
        Assert.IsEmpty(result.AppliedClipAssets);
    }

    [Test]
    public void TargetTimeline_NeverReferencesThePresetAssets()
    {
        PresetRecipe.AddToTimeline(_preset, _target, _director, null, START);

        var track = _target.CreateTrack<CanvasGroupTrack>(null, "mine");
        var clip = CreateClip(track, 2d, 0.5d, "mine", 0f, 1f);
        PresetRecipe.ApplyToClips(_preset, new[] { clip.asset as PlayableAsset });
        AssetDatabase.SaveAssetIfDirty(_target);

        var presetPaths = new[] { PRESET_TIMELINE_PATH, AssetDatabase.GetAssetPath(_preset) };

        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(TARGET_TIMELINE_PATH))
        {
            var property = new SerializedObject(asset).GetIterator();

            while (property.Next(true))
            {
                if (property.propertyType != SerializedPropertyType.ObjectReference
                    || property.objectReferenceValue.IsInvalid()
                )
                {
                    continue;
                }

                var path = AssetDatabase.GetAssetPath(property.objectReferenceValue);

                CollectionAssert.DoesNotContain(presetPaths, path, $"{asset.name}.{property.propertyPath}");
            }
        }
    }

    [Test]
    public void ShippedPresets_AreValidAndFound()
    {
        var all = TweenPreset.FindAll();

        foreach (var key in new[] { "popup-fade-in", "popup-fade-out" })
        {
            var preset = AssetDatabase.LoadAssetAtPath<TweenPreset>(LIBRARY + key + ".asset");

            Assert.IsTrue(preset.IsValid(), key);
            Assert.IsEmpty(preset.GetProblems(), key);
            Assert.IsTrue(preset.TryGetPath(BACKDROP, out var backdrop), key);
            Assert.AreEqual(BACKDROP_PATH, backdrop, key);
            Assert.IsTrue(preset.TryGetPath(POPUP_ALPHA, out var popup), key);
            Assert.AreEqual(POPUP_PATH, popup, key);
            CollectionAssert.Contains(all, preset, key);
        }
    }

    [Test]
    public void ShippedPresets_AddToThePopupHierarchyWithoutWarnings()
    {
        var preset = AssetDatabase.LoadAssetAtPath<TweenPreset>(LIBRARY + "popup-fade-in.asset");
        var result = PresetRecipe.AddToTimeline(preset, _target, _director, null, 0d);

        Assert.IsEmpty(result.Warnings);
        Assert.AreEqual(2, result.Tracks.Count);
        Assert.IsTrue(result.Tracks.All(track => _director.GetGenericBinding(track).IsValid()));
    }

    [Test]
    public void ApplyEntries_ListEachClipOfTheSelectedType()
    {
        var track = _target.CreateTrack<CanvasGroupTrack>(null, "mine");
        var clip = track.CreateDefaultClip();
        var presets = new List<TweenPreset> { _preset };

        var entries = PresetPicker.GetApplyEntries(presets, new[] { clip.asset as PlayableAsset });

        Assert.AreEqual(2, entries.Count);
        Assert.AreNotSame(entries[0].Source, entries[1].Source);

        var rect = _target.CreateTrack<RectTransformTrack>(null, "rect").CreateDefaultClip();

        Assert.IsEmpty(PresetPicker.GetApplyEntries(presets, new[] { rect.asset as PlayableAsset }));
    }

    [Test]
    public void ClipInspector_HeaderHasTheApplyPresetButton()
    {
        var clip = Track(ScriptableObject.CreateInstance<CanvasGroupClip>());
        var editor = Track(UnityEditor.Editor.CreateEditor(clip));
        var root = editor.CreateInspectorGUI();
        var header = root.Q<VisualElement>(className: TweenClipSections.HEADER_USS_CLASS_NAME);

        var buttons = header.Query<Button>(className: TweenClipSections.ICON_BUTTON_USS_CLASS_NAME).ToList();

        Assert.AreEqual(1, buttons.Count);
    }

    private T Track<T>(T obj)
        where T : Object
    {
        _objects.Add(obj);
        return obj;
    }

    private static TimelineAsset CreateTimelineAsset(string path)
    {
        var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(timeline, path);
        AssetDatabase.SaveAssetIfDirty(timeline);
        return timeline;
    }

    /// <summary>
    /// Two CanvasGroup tracks: <c>backdrop</c> (clip at 0, Hold after) and <c>popup-alpha</c> (clip at 0.1 with ease
    /// in, Hold before, Revert restore mode).
    /// </summary>
    private static TimelineAsset CreatePresetTimeline()
    {
        var timeline = CreateTimelineAsset(PRESET_TIMELINE_PATH);

        var backdrop = timeline.CreateTrack<CanvasGroupTrack>(null, BACKDROP);
        CreateClip(backdrop, 0d, 0.25d, BACKDROP, 0.1f, 0.8f);
        SetExtrapolation(backdrop, 0, TimelineClip.ClipExtrapolation.None, TimelineClip.ClipExtrapolation.Hold);

        var popup = timeline.CreateTrack<CanvasGroupTrack>(null, POPUP_ALPHA);
        popup.RestoreMode = RestoreMode.Revert;
        var popupClip = CreateClip(popup, 0.1d, 0.3d, POPUP_ALPHA, 0.2f, 0.9f);
        popupClip.easeInDuration = 0.05d;
        SetExtrapolation(popup, 0, TimelineClip.ClipExtrapolation.Hold, TimelineClip.ClipExtrapolation.None);

        AssetDatabase.SaveAssetIfDirty(timeline);
        return timeline;
    }

    private TweenPreset CreatePreset(TimelineAsset timeline, string popupPath)
    {
        var preset = Track(ScriptableObject.CreateInstance<TweenPreset>());
        preset.name = "test-preset";
        preset._timeline = timeline;
        preset._category = "test";
        preset._bindings = new[] {
            new TweenPresetBinding(BACKDROP, BACKDROP_PATH),
            new TweenPresetBinding(POPUP_ALPHA, popupPath),
        };

        return preset;
    }

    /// <summary>
    /// <c>popup-test</c> (CanvasGroup) → <c>backdrop</c> (CanvasGroup), <c>body</c> → <c>popup</c> (CanvasGroup),
    /// and <c>transition-show</c> with the director.
    /// </summary>
    private static GameObject CreatePopupHierarchy(out PlayableDirector director)
    {
        var root = new GameObject("popup-test", typeof(RectTransform), typeof(CanvasGroup));
        var backdrop = new GameObject(BACKDROP_PATH, typeof(RectTransform), typeof(CanvasGroup));
        backdrop.transform.SetParent(root.transform, false);

        var body = new GameObject("body", typeof(RectTransform));
        body.transform.SetParent(root.transform, false);

        var popup = new GameObject("popup", typeof(RectTransform), typeof(CanvasGroup));
        popup.transform.SetParent(body.transform, false);

        var transition = new GameObject("transition-show");
        transition.transform.SetParent(root.transform, false);
        director = transition.AddComponent<PlayableDirector>();
        return root;
    }

    private static TimelineClip CreateClip(
          TrackAsset track
        , double start
        , double duration
        , string name
        , float from
        , float to
    )
    {
        var clip = track.CreateDefaultClip();
        clip.start = start;
        clip.duration = duration;
        clip.displayName = name;

        var serializedClip = new SerializedObject(clip.asset);
        serializedClip.FindProperty(ALPHA_ENABLED).boolValue = true;
        serializedClip.FindProperty(ALPHA_FROM).floatValue = from;
        serializedClip.FindProperty(ALPHA_TO).floatValue = to;
        serializedClip.ApplyModifiedPropertiesWithoutUndo();
        return clip;
    }

    private static void SetExtrapolation(
          TrackAsset track
        , int index
        , TimelineClip.ClipExtrapolation pre
        , TimelineClip.ClipExtrapolation post
    )
    {
        var serializedTrack = new SerializedObject(track);
        serializedTrack.FindProperty($"m_Clips.Array.data[{index}].m_PreExtrapolationMode").intValue = (int)pre;
        serializedTrack.FindProperty($"m_Clips.Array.data[{index}].m_PostExtrapolationMode").intValue = (int)post;
        serializedTrack.ApplyModifiedPropertiesWithoutUndo();
    }

    private static int GetSceneBindingCount(PlayableDirector director)
        => new SerializedObject(director).FindProperty("m_SceneBindings").arraySize;

    private static int GetInt(Object target, string path)
        => new SerializedObject(target).FindProperty(path).intValue;

    private static float GetFloat(Object target, string path)
        => new SerializedObject(target).FindProperty(path).floatValue;
}
