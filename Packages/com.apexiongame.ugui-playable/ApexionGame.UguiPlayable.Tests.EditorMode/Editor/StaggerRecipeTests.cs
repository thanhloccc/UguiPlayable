using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using ApexionGame.UguiPlayable.Editor.Presets;
using ApexionGame.UguiPlayable.Tracks.Groups;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Editor;

public sealed class StaggerRecipeTests
{
    private const string FOLDER_PARENT = "Assets";
    private const string FOLDER_NAME = "UguiPlayableStaggerTestsTemp";
    private const string FOLDER = FOLDER_PARENT + "/" + FOLDER_NAME;
    private const string PRESET_TIMELINE_PATH = FOLDER + "/preset.playable";
    private const string TARGET_TIMELINE_PATH = FOLDER + "/target.playable";
    private const string BINDINGS = "m_SceneBindings";
    private const double TIME = 2d;
    private const double DELAY = 0.05d;

    private readonly List<Object> _objects = new();

    private TimelineAsset _presetTimeline;
    private TweenPreset _preset;
    private TimelineAsset _target;
    private GameObject _container;
    private PlayableDirector _director;

    [SetUp]
    public void SetUp()
    {
        if (AssetDatabase.IsValidFolder(FOLDER) == false)
        {
            AssetDatabase.CreateFolder(FOLDER_PARENT, FOLDER_NAME);
        }

        _presetTimeline = CreateTimelineAsset(PRESET_TIMELINE_PATH);
        var track = _presetTimeline.CreateTrack<CanvasGroupTrack>(null, "fade");
        var clip = track.CreateDefaultClip();
        clip.start = 0d;
        clip.duration = 0.25d;
        AssetDatabase.SaveAssetIfDirty(_presetTimeline);

        _preset = ScriptableObject.CreateInstance<TweenPreset>();
        _preset.name = "stagger-test";
        _preset._timeline = _presetTimeline;
        _objects.Add(_preset);

        _target = CreateTimelineAsset(TARGET_TIMELINE_PATH);

        _container = new GameObject("list", typeof(RectTransform));
        _director = _container.AddComponent<PlayableDirector>();
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

        if (_container.IsValid())
        {
            Object.DestroyImmediate(_container);
        }

        AssetDatabase.DeleteAsset(FOLDER);
    }

    [Test]
    public void Stagger_MakesOneTrackPerActiveChildUnderOneGroupWithStaggeredStarts()
    {
        var children = AddChildren(5);
        children[2].SetActive(false);

        var result = StaggerRecipe.Stagger(_preset, _target, _director, _container, TIME, DELAY);

        Assert.IsEmpty(result.Warnings);
        Assert.AreEqual(4, result.Tracks.Count);

        var group = _target.GetRootTracks().Single();
        Assert.IsInstanceOf<GroupTrack>(group);
        Assert.AreEqual("list", group.name);
        CollectionAssert.AreEqual(result.Tracks, group.GetChildTracks().ToList());

        var active = children.Where(static c => c.activeSelf).ToList();

        for (var i = 0; i < result.Tracks.Count; i++)
        {
            var track = result.Tracks[i];
            var clip = track.GetClips().Single();

            Assert.IsInstanceOf<CanvasGroupTrack>(track);
            Assert.AreEqual(active[i].name, track.name);
            Assert.AreEqual(TIME + i * DELAY, clip.start, 1e-9);
            Assert.AreEqual(0.25d, clip.duration, 1e-9);
            Assert.AreSame(active[i].GetComponent<CanvasGroup>(), _director.GetGenericBinding(track));
        }
    }

    [Test]
    public void Stagger_SkipsChildrenWithoutTheComponentWithOneWarning()
    {
        var children = AddChildren(4);
        Object.DestroyImmediate(children[1].GetComponent<CanvasGroup>());
        Object.DestroyImmediate(children[2].GetComponent<CanvasGroup>());

        var result = StaggerRecipe.Stagger(_preset, _target, _director, _container, TIME, DELAY);

        Assert.AreEqual(1, result.Warnings.Count);
        StringAssert.Contains(children[1].name, result.Warnings[0]);
        StringAssert.Contains(children[2].name, result.Warnings[0]);
        Assert.AreEqual(2, result.Tracks.Count);
        Assert.AreEqual(TIME, result.Tracks[0].GetClips().Single().start, 1e-9);
        Assert.AreEqual(TIME + DELAY, result.Tracks[1].GetClips().Single().start, 1e-9);
    }

    [Test]
    public void Stagger_IsUndoneInOneStep()
    {
        AddChildren(3);
        var bindingCount = GetBindingCount();

        StaggerRecipe.Stagger(_preset, _target, _director, _container, TIME, DELAY);

        Assert.AreEqual(3, _target.GetOutputTracks().Count());
        Assert.Greater(GetBindingCount(), bindingCount);

        Undo.PerformUndo();

        Assert.AreEqual(0, _target.GetOutputTracks().Count());
        Assert.AreEqual(0, _target.GetRootTracks().Count());
        Assert.AreEqual(bindingCount, GetBindingCount());
    }

    [Test]
    public void Stagger_WithoutADirectorAddsUnboundTracksWithAWarning()
    {
        AddChildren(2);

        var result = StaggerRecipe.Stagger(_preset, _target, null, _container, TIME, DELAY);

        Assert.AreEqual(2, result.Tracks.Count);
        Assert.AreEqual(1, result.Warnings.Count);
    }

    [Test]
    public void Stagger_NeedsAPresetWithOneTrack()
    {
        AddChildren(2);
        _presetTimeline.CreateTrack<CanvasGroupTrack>(null, "second");

        var result = StaggerRecipe.Stagger(_preset, _target, _director, _container, TIME, DELAY);

        Assert.IsEmpty(result.Tracks);
        Assert.AreEqual(1, result.Warnings.Count);
        Assert.AreEqual(0, _target.GetRootTracks().Count());
        Assert.IsNotEmpty(StaggerRecipe.GetBlockReason(_preset, _target, _container));
        Assert.IsNotEmpty(StaggerRecipe.GetBlockReason(_preset, null, _container));
        Assert.IsNotEmpty(StaggerRecipe.GetBlockReason(_preset, _target, null));
    }

    private int GetBindingCount()
        => new SerializedObject(_director).FindProperty(BINDINGS).arraySize;

    private List<GameObject> AddChildren(int count)
    {
        var children = new List<GameObject>(count);

        for (var i = 0; i < count; i++)
        {
            var child = new GameObject($"item-{i}", typeof(RectTransform), typeof(CanvasGroup));
            child.transform.SetParent(_container.transform, false);
            children.Add(child);
        }

        return children;
    }

    private static TimelineAsset CreateTimelineAsset(string path)
    {
        var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        AssetDatabase.CreateAsset(timeline, path);
        AssetDatabase.SaveAssetIfDirty(timeline);
        return timeline;
    }
}
