using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Easing;
using ApexionGame.UguiPlayable.Tracks.Groups;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Tracks;

public sealed class CanvasGroupTrackTests
{
    private const float TOLERANCE = 1e-4f;

    private readonly List<Object> _created = new();
    private PlayableDirector _director;
    private TimelineAsset _timeline;
    private CanvasGroupTrack _track;
    private CanvasGroup _group;

    [SetUp]
    public void SetUp()
    {
        var directorObject = new GameObject("Director");
        var groupObject = new GameObject("Group", typeof(RectTransform));
        _created.Add(directorObject);
        _created.Add(groupObject);

        _group = groupObject.AddComponent<CanvasGroup>();
        _director = directorObject.AddComponent<PlayableDirector>();
        _director.playOnAwake = false;

        _timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        _created.Add(_timeline);
        _track = _timeline.CreateTrack<CanvasGroupTrack>();
        _director.playableAsset = _timeline;
        _director.SetGenericBinding(_track, _group);
    }

    [TearDown]
    public void TearDown()
    {
        if (_director != null)
        {
            _director.Stop();
        }

        foreach (var obj in _created)
        {
            if (obj != null)
            {
                Object.DestroyImmediate(obj);
            }
        }

        _created.Clear();
    }

    [Test]
    public void Absolute_Linear_InterpolatesAndHoldsEnd()
    {
        _group.alpha = 0.3f;
        AddClip(0d, 1d, new CanvasGroupBehaviour { Alpha = new TweenChannel<float>(0f, 1f) });

        Assert.That(EvaluateAt(0.5d), Is.EqualTo(0.5f).Within(TOLERANCE));
        Assert.That(EvaluateAt(1.5d), Is.EqualTo(1f).Within(TOLERANCE), "the end value holds after the clip");
    }

    [Test]
    public void Ease_IsApplied()
    {
        var channel = new TweenChannel<float>(0f, 1f, new EaseSetting(Ease.OutQuad));
        AddClip(0d, 1d, new CanvasGroupBehaviour { Alpha = channel });

        Assert.That(EvaluateAt(0.5d), Is.EqualTo(0.75f).Within(TOLERANCE));
    }

    [Test]
    public void BeforeFirstClip_PropertyIsNotWritten()
    {
        _group.alpha = 0.3f;
        AddClip(1d, 1d, new CanvasGroupBehaviour { Alpha = new TweenChannel<float>(0f, 1f) });

        Assert.That(EvaluateAt(0.5d), Is.EqualTo(0.3f).Within(TOLERANCE));
    }

    [Test]
    public void EaseIn_BlendsFromBaseValue_NotFromZero()
    {
        _group.alpha = 0.3f;
        var clip = AddClip(0d, 2d, new CanvasGroupBehaviour { Alpha = new TweenChannel<float>(1f, 1f) });
        clip.easeInDuration = 1d;

        var value = EvaluateAt(0.5d);
        var weight = clip.EvaluateMixIn(0.5d);
        Assert.That(weight, Is.GreaterThan(0f).And.LessThan(1f));
        Assert.That(value, Is.EqualTo(0.3f * (1f - weight) + weight).Within(TOLERANCE));
        Assert.That(value, Is.GreaterThan(0.3f));
    }

    [Test]
    public void Crossfade_BlendsBothClips()
    {
        var first = AddClip(0d, 2d, new CanvasGroupBehaviour { Alpha = new TweenChannel<float>(0f, 0f) });
        var second = AddClip(1d, 2d, new CanvasGroupBehaviour { Alpha = new TweenChannel<float>(1f, 1f) });
        first.blendOutDuration = 1d;
        second.blendInDuration = 1d;

        var value = EvaluateAt(1.5d);
        Assert.That(value, Is.GreaterThan(0f).And.LessThan(1f));
    }

    [Test]
    public void Relative_AddsToBaseValue()
    {
        _group.alpha = 0.4f;
        var channel = new TweenChannel<float>(0f, 0.2f, ValueMode.Relative);
        AddClip(0d, 1d, new CanvasGroupBehaviour { Alpha = channel });

        Assert.That(EvaluateAt(0.5d), Is.EqualTo(0.5f).Within(TOLERANCE));
        Assert.That(EvaluateAt(1.5d), Is.EqualTo(0.6f).Within(TOLERANCE));
    }

    [Test]
    public void FromCurrent_StartsFromHeldValueOfPreviousClip()
    {
        _group.alpha = 1f;
        AddClip(0d, 1d, new CanvasGroupBehaviour { Alpha = new TweenChannel<float>(1f, 0.2f) });
        var fromCurrent = new TweenChannel<float>(0f, 1f, ValueMode.FromCurrent);
        AddClip(2d, 1d, new CanvasGroupBehaviour { Alpha = fromCurrent });

        Assert.That(EvaluateAt(1.5d), Is.EqualTo(0.2f).Within(TOLERANCE), "holds the first clip's end in the gap");
        Assert.That(EvaluateAt(2.5d), Is.EqualTo(0.6f).Within(TOLERANCE), "lerps from the captured 0.2 to 1");
    }

    [Test]
    public void DisabledChannel_IsNotWritten()
    {
        _group.alpha = 0.3f;
        _group.interactable = true;
        var behaviour = new CanvasGroupBehaviour
        {
            Alpha = new TweenChannel<float>(0f, 1f, enabled: false),
            Interactable = new StepChannel(false),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.75d);

        Assert.That(_group.alpha, Is.EqualTo(0.3f).Within(TOLERANCE));
        Assert.That(_group.interactable, Is.False);
    }

    [Test]
    public void StepChannel_SwitchesAtThreshold()
    {
        _group.interactable = true;
        var behaviour = new CanvasGroupBehaviour
        {
            Alpha = new TweenChannel<float>(0f, 1f, enabled: false),
            Interactable = new StepChannel(false, 0.5f),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.25d);
        Assert.That(_group.interactable, Is.True);

        EvaluateAt(0.75d);
        Assert.That(_group.interactable, Is.False);
    }

    // Unity runs OnPlayableDestroy deferred, on a later player-loop update, which EditMode tests do not get
    // (Timeline's own ActivationTrack revert behaves the same). These tests call the callback; the deferred Play Mode
    // path was verified by hand (step 02 Result).
    [Test]
    public void Revert_RestoresBaseValue_WhenGraphStops()
    {
        _group.alpha = 0.3f;
        _group.interactable = false;
        _track.RestoreMode = RestoreMode.Revert;
        AddClip(0d, 1d, new CanvasGroupBehaviour { Alpha = new TweenChannel<float>(0f, 1f) });

        EvaluateAt(0.5d);
        _group.interactable = true;
        StopMixer();

        Assert.That(_group.alpha, Is.EqualTo(0.3f).Within(TOLERANCE));
        Assert.That(_group.interactable, Is.True, "a property the track never wrote is not restored");
    }

    [Test]
    public void Keep_LeavesLastValue_WhenGraphStops()
    {
        _group.alpha = 0.3f;
        _track.RestoreMode = RestoreMode.Keep;
        AddClip(0d, 1d, new CanvasGroupBehaviour { Alpha = new TweenChannel<float>(0f, 1f) });

        EvaluateAt(0.5d);
        StopMixer();

        Assert.That(_group.alpha, Is.EqualTo(0.5f).Within(TOLERANCE));
    }

    [Test]
    public void PauseAndResume_KeepsBaseValue()
    {
        _group.alpha = 0.4f;
        var channel = new TweenChannel<float>(0f, 0.2f, ValueMode.Relative);
        AddClip(0d, 1d, new CanvasGroupBehaviour { Alpha = channel });

        EvaluateAt(0.75d);
        var mixer = (ScriptPlayable<CanvasGroupMixer>)_director.playableGraph.GetRootPlayable(0).GetInput(0);
        mixer.GetBehaviour().OnGraphStop(mixer);

        Assert.That(EvaluateAt(0.5d), Is.EqualTo(0.5f).Within(TOLERANCE), "Relative still adds to the original 0.4");
    }

    [Test]
    public void RootPlayableTime_MatchesDirectorTime()
    {
        AddClip(0d, 2d, new CanvasGroupBehaviour());

        EvaluateAt(1.25d);

        var rootTime = _director.playableGraph.GetRootPlayable(0).GetTime();
        Assert.That(rootTime, Is.EqualTo(1.25d).Within(1e-6d));
    }

    [Test]
    public void GatherProperties_RegistersOnlyEnabledChannels()
    {
        var behaviour = new CanvasGroupBehaviour
        {
            Alpha = new TweenChannel<float>(0f, 1f),
            BlocksRaycasts = new StepChannel(false),
        };
        AddClip(0d, 1d, behaviour);
        var collector = new RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(collector.Names, Is.EquivalentTo(new[] { "m_Alpha", "m_BlocksRaycasts" }));
    }

    [Test]
    public void Evaluate_DoesNotAllocate()
    {
        AddClip(0d, 1d, new CanvasGroupBehaviour { Alpha = new TweenChannel<float>(0f, 1f) });
        AddClip(1d, 1d, new CanvasGroupBehaviour { Interactable = new StepChannel(false) });
        _director.RebuildGraph();
        var graph = _director.playableGraph;
        var root = graph.GetRootPlayable(0);

        for (var i = 0; i < 10; i++)
        {
            root.SetTime(i * 0.2d);
            graph.Evaluate();
        }

        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var i = 0; i < 100; i++)
        {
            root.SetTime(i % 20 * 0.1d);
            graph.Evaluate();
        }

        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Assert.That(allocated, Is.Zero);
    }

    private TimelineClip AddClip(double start, double duration, CanvasGroupBehaviour template)
    {
        var clip = _track.CreateClip<CanvasGroupClip>();
        clip.start = start;
        clip.duration = duration;

        var asset = (CanvasGroupClip)clip.asset;
        asset.Template.Alpha = template.Alpha;
        asset.Template.Interactable = template.Interactable;
        asset.Template.BlocksRaycasts = template.BlocksRaycasts;
        return clip;
    }

    private void StopMixer()
    {
        var mixer = (ScriptPlayable<CanvasGroupMixer>)_director.playableGraph.GetRootPlayable(0).GetInput(0);
        mixer.GetBehaviour().OnPlayableDestroy(mixer);
    }

    private float EvaluateAt(double time)
    {
        if (_director.playableGraph.IsValid() == false)
        {
            _director.RebuildGraph();
        }

        _director.time = time;
        _director.Evaluate();
        return _group.alpha;
    }

    private sealed class RecordingCollector : IPropertyCollector
    {
        public readonly List<string> Names = new();

        public void PushActiveGameObject(GameObject gameObject) { }

        public void PopActiveGameObject() { }

        public void AddFromClip(AnimationClip clip) { }

        public void AddFromClips(IEnumerable<AnimationClip> clips) { }

        public void AddFromName<T>(string name) where T : Component => Names.Add(name);

        public void AddFromName(string name) => Names.Add(name);

        public void AddFromClip(GameObject obj, AnimationClip clip) { }

        public void AddFromClips(GameObject obj, IEnumerable<AnimationClip> clips) { }

        public void AddFromName<T>(GameObject obj, string name) where T : Component => Names.Add(name);

        public void AddFromName(GameObject obj, string name) => Names.Add(name);

        public void AddFromName(Component component, string name) => Names.Add(name);

        public void AddFromComponent(GameObject obj, Component component) { }

        public void AddObjectProperties(Object obj, AnimationClip clip) { }
    }
}
