using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;
using ApexionGame.UguiPlayable.Editor.Presets;
using ApexionGame.UguiPlayable.Tracks.Groups;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Editor;

public sealed class PresetBrowserTests
{
    private const string FOLDER_PARENT = "Assets";
    private const string FOLDER_NAME = "UguiPlayablePresetBrowserTestsTemp";
    private const string FOLDER = FOLDER_PARENT + "/" + FOLDER_NAME;
    private const string TARGET_FOLDER_NAME = "copies";
    private const string TARGET_FOLDER = FOLDER + "/" + TARGET_FOLDER_NAME;
    private const string TIMELINE_PATH = FOLDER + "/fade.playable";
    private const string PRESET_PATH = FOLDER + "/fade-preset.asset";

    private readonly List<Object> _objects = new();

    [SetUp]
    public void SetUp()
    {
        if (AssetDatabase.IsValidFolder(FOLDER) == false)
        {
            AssetDatabase.CreateFolder(FOLDER_PARENT, FOLDER_NAME);
            AssetDatabase.CreateFolder(FOLDER, TARGET_FOLDER_NAME);
        }
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
        AssetDatabase.DeleteAsset(FOLDER);
    }

    [Test]
    public void GetGroups_GroupsByCategoryInOrderAndHandlesAnEmptyCategory()
    {
        var model = new PresetBrowserModel(new List<TweenPreset> {
            CreateMemoryPreset("a-fade", "popup", "Fades in"),
            CreateMemoryPreset("b-scale", "popup", "Scales"),
            CreateMemoryPreset("c-slide", "screen", "Slides"),
            CreateMemoryPreset("d-none", string.Empty, string.Empty),
        });

        var groups = model.GetGroups();

        Assert.AreEqual(3, groups.Count);
        Assert.AreEqual("popup", groups[0].Category);
        Assert.AreEqual(2, groups[0].Presets.Count);
        Assert.AreEqual("screen", groups[1].Category);
        Assert.AreEqual(PresetBrowserModel.NO_CATEGORY, groups[2].Category);
        CollectionAssert.AreEqual(
            new[] { PresetPicker.ALL_CATEGORIES, "popup", "screen", PresetBrowserModel.NO_CATEGORY }
            , model.GetCategories()
        );
    }

    [Test]
    public void GetGroups_FiltersByQueryAndCategory()
    {
        var model = new PresetBrowserModel(new List<TweenPreset> {
            CreateMemoryPreset("a-fade", "popup", "Fades in"),
            CreateMemoryPreset("b-scale", "popup", "Scales"),
            CreateMemoryPreset("c-slide", "screen", "Slides"),
            CreateMemoryPreset("d-none", string.Empty, string.Empty),
        });

        model.Query = "FADE";
        Assert.AreEqual("a-fade", model.GetGroups().Single().Presets.Single().name);

        model.Query = "slides";
        Assert.AreEqual("c-slide", model.GetGroups().Single().Presets.Single().name);

        model.Query = string.Empty;
        model.Category = "screen";
        Assert.AreEqual("c-slide", model.GetGroups().Single().Presets.Single().name);

        model.Category = PresetBrowserModel.NO_CATEGORY;
        Assert.AreEqual("d-none", model.GetGroups().Single().Presets.Single().name);

        model.Category = "popup";
        model.Query = "none";
        Assert.IsEmpty(model.GetGroups());
    }

    [Test]
    public void GetTracks_ListsEachTrackWithItsPath()
    {
        var preset = CreateAssetPreset(out _);
        var tracks = PresetBrowserModel.GetTracks(preset);

        Assert.AreEqual(1, tracks.Count);
        Assert.AreEqual("fade", tracks[0].Name);
        Assert.AreEqual("body/popup", tracks[0].Path);
        Assert.AreEqual(nameof(CanvasGroupTrack), tracks[0].TypeName);
    }

    [Test]
    public void GetAddBlockReason_IsGivenWhenNoTimelineIsOpen()
    {
        var preset = CreateAssetPreset(out var timeline);

        Assert.AreEqual(PresetBrowserModel.NO_TIMELINE_REASON, PresetBrowserModel.GetAddBlockReason(preset, null));
        Assert.IsEmpty(PresetBrowserModel.GetAddBlockReason(preset, timeline));

        var empty = CreateMemoryPreset("empty", "x", string.Empty);
        Assert.IsNotEmpty(PresetBrowserModel.GetAddBlockReason(empty, timeline));

        // The window passes TimelineEditor.inspectedAsset; with no Timeline window open it is null.
        if (TimelineEditor.inspectedAsset == null)
        {
            Assert.AreEqual(
                  PresetBrowserModel.NO_TIMELINE_REASON
                , PresetBrowserModel.GetAddBlockReason(preset, TimelineEditor.inspectedAsset)
            );
        }
    }

    [Test]
    public void Duplicate_CopiesTheTimelineAndPointsThePresetCopyAtIt()
    {
        var preset = CreateAssetPreset(out var timeline);
        var originalBytes = File.ReadAllBytes(PRESET_PATH);

        var copy = PresetBrowserModel.Duplicate(preset, TARGET_FOLDER, out var error);

        Assert.IsEmpty(error);
        Assert.IsNotNull(copy);
        Assert.AreNotSame(preset, copy);
        Assert.AreNotSame(timeline, copy.Timeline);
        Assert.AreEqual(TARGET_FOLDER, GetFolder(AssetDatabase.GetAssetPath(copy)));
        Assert.AreEqual(TARGET_FOLDER, GetFolder(AssetDatabase.GetAssetPath(copy.Timeline)));
        Assert.AreEqual(timeline.GetOutputTracks().Count(), copy.Timeline.GetOutputTracks().Count());
        Assert.AreEqual(preset.Category, copy.Category);
        Assert.IsTrue(copy.TryGetPath("fade", out var path));
        Assert.AreEqual("body/popup", path);

        // The original preset still references the original timeline, and its file is untouched.
        Assert.AreSame(timeline, preset.Timeline);
        CollectionAssert.AreEqual(originalBytes, File.ReadAllBytes(PRESET_PATH));
    }

    [Test]
    public void Duplicate_RejectsAFolderOutsideAssets()
    {
        var preset = CreateAssetPreset(out _);

        Assert.IsNull(PresetBrowserModel.Duplicate(preset, "Packages/com.apexiongame.ugui-playable", out var error));
        Assert.IsNotEmpty(error);
        Assert.IsNull(PresetBrowserModel.Duplicate(preset, FOLDER + "/missing", out error));
        Assert.IsNotEmpty(error);
    }

    private static string GetFolder(string path)
        => Path.GetDirectoryName(path)?.Replace('\\', '/');

    private TweenPreset CreateMemoryPreset(string name, string category, string description)
    {
        var preset = ScriptableObject.CreateInstance<TweenPreset>();
        preset.name = name;
        preset._category = category;
        preset._description = description;
        _objects.Add(preset);
        return preset;
    }

    private static TweenPreset CreateAssetPreset(out TimelineAsset timeline)
    {
        timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(timeline, TIMELINE_PATH);
        timeline.CreateTrack<CanvasGroupTrack>(null, "fade");
        AssetDatabase.SaveAssetIfDirty(timeline);

        var preset = ScriptableObject.CreateInstance<TweenPreset>();
        preset._timeline = timeline;
        preset._category = "test";
        preset._bindings = new[] { new TweenPresetBinding("fade", "body/popup") };
        AssetDatabase.CreateAsset(preset, PRESET_PATH);
        AssetDatabase.SaveAssetIfDirty(preset);
        return preset;
    }
}
