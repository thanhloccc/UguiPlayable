using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Tracks.Controls;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Tracks;

public sealed class ToggleTrackTests
{
    private readonly List<Object> _created = new();
    private PlayableDirector _director;
    private TimelineAsset _timeline;
    private ToggleTrack _track;
    private ToggleGroup _group;
    private Toggle _a;
    private Toggle _b;
    private Toggle _c;

    [SetUp]
    public void SetUp()
    {
        var directorObject = new GameObject("Director");
        _created.Add(directorObject);

        var groupObject = new GameObject("Group", typeof(RectTransform));
        _created.Add(groupObject);
        _group = groupObject.AddComponent<ToggleGroup>();
        _group.allowSwitchOff = true;

        _a = CreateToggle("A", groupObject.transform, _group);
        _b = CreateToggle("B", groupObject.transform, _group);
        _c = CreateToggle("C", groupObject.transform, _group);

        _director = directorObject.AddComponent<PlayableDirector>();
        _director.playOnAwake = false;

        _timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        _created.Add(_timeline);
        _track = _timeline.CreateTrack<ToggleTrack>();
        _director.playableAsset = _timeline;
        _director.SetGenericBinding(_track, _b);
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
        PlayModeSeam.OverrideForTests = null;
    }

    [Test]
    public void IsOn_SwitchesAtTheThreshold_AndAFinishedClipHolds()
    {
        AddClip(0d, 1d, new StepChannel(true, 0.5f));

        EvaluateAt(0.25d);
        Assert.That(_b.isOn, Is.False, "before the threshold");

        EvaluateAt(0.5d);
        Assert.That(_b.isOn, Is.True, "at the threshold");

        EvaluateAt(1.5d);
        Assert.That(_b.isOn, Is.True, "hold after the clip ends");
    }

    [Test]
    public void DisabledChannel_WritesNothing()
    {
        AddClip(0d, 1d, new StepChannel(true, 0.5f, enabled: false));

        EvaluateAt(0.75d);

        Assert.That(_b.isOn, Is.False);
    }

    [Test]
    public void GatherProperties_RegistersTheNameOnEveryToggleOfTheGroup()
    {
        AddClip(0d, 1d, new StepChannel(true));
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(collector.Names, Is.EqualTo(new[] { "m_IsOn", "m_IsOn", "m_IsOn" }));
    }

    [Test]
    public void GatherProperties_RegistersOnlyTheBindingWithoutAGroup()
    {
        _b.group = null;
        AddClip(0d, 1d, new StepChannel(true));
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(collector.Names, Is.EqualTo(new[] { "m_IsOn" }));
    }

    [Test]
    public void GatherProperties_RegistersNothingWhenTheChannelIsOff()
    {
        AddClip(0d, 1d, new StepChannel(true, enabled: false));
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(collector.Names, Is.Empty);
    }

    [Test]
    public void TurningBOn_TurnsAOffThroughTheGroup()
    {
        _a.SetIsOnWithoutNotify(true);
        AddClip(0d, 1d, new StepChannel(true));

        EvaluateAt(0.75d);

        Assert.That(_b.isOn, Is.True);
        Assert.That(_a.isOn, Is.False, "the group logic still runs");
    }

    [Test]
    public void SendCallbacks_IsOffByDefault()
    {
        Assert.That(_track.SendCallbacks, Is.False);
    }

    [TestCase(false, true)]
    [TestCase(false, false)]
    [TestCase(true, false)]
    public void Evaluate_DoesNotRaiseOnValueChanged(bool sendCallbacks, bool playing)
    {
        var calls = ScrubWithListener(sendCallbacks, playing);

        Assert.That(calls, Is.Zero);
        Assert.That(_b.isOn, Is.False, "the value is still written, last clip step is off");
    }

    [Test]
    public void Evaluate_RaisesOnValueChangedOncePerChange_WhenOnAndPlaying()
    {
        var calls = ScrubWithListener(true, true);

        Assert.That(calls, Is.EqualTo(2), "off to on, then on to off");
    }

    [Test]
    public void Evaluate_WritesNothingWhenTheValueDoesNotChange()
    {
        _track.SendCallbacks = true;
        PlayModeSeam.OverrideForTests = true;
        AddClip(0d, 1d, new StepChannel(true));

        var calls = 0;
        _b.onValueChanged.AddListener(_ => calls++);

        EvaluateAt(0.5d);
        EvaluateAt(0.75d);
        EvaluateAt(1d);
        EvaluateAt(1.5d);

        Assert.That(calls, Is.EqualTo(1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Revert_RestoresBAndItsSiblings_WithoutCallingListeners(bool sendCallbacks)
    {
        _a.SetIsOnWithoutNotify(true);
        _track.RestoreMode = RestoreMode.Revert;
        _track.SendCallbacks = sendCallbacks;
        PlayModeSeam.OverrideForTests = true;
        AddClip(0d, 1d, new StepChannel(true));
        EvaluateAt(0.75d);
        Assert.That(_b.isOn, Is.True);
        Assert.That(_a.isOn, Is.False);

        var calls = 0;
        _a.onValueChanged.AddListener(_ => calls++);
        _b.onValueChanged.AddListener(_ => calls++);
        _c.onValueChanged.AddListener(_ => calls++);
        StopMixer();

        Assert.That(_a.isOn, Is.True);
        Assert.That(_b.isOn, Is.False);
        Assert.That(_c.isOn, Is.False);
        Assert.That(calls, Is.Zero);
    }

    [Test]
    public void Revert_WithoutSwitchOff_RestoresTheOnToggle()
    {
        _group.allowSwitchOff = false;
        _a.SetIsOnWithoutNotify(true);
        _b.SetIsOnWithoutNotify(false);
        _c.SetIsOnWithoutNotify(false);
        _track.RestoreMode = RestoreMode.Revert;
        AddClip(0d, 1d, new StepChannel(true));

        EvaluateAt(0.75d);
        Assert.That(_b.isOn, Is.True);
        Assert.That(_a.isOn, Is.False);

        StopMixer();

        Assert.That(_a.isOn, Is.True);
        Assert.That(_b.isOn, Is.False);
        Assert.That(_c.isOn, Is.False);
    }

    [Test]
    public void RestoreUnserialized_WritesTheCapturedCheckmarkAlpha()
    {
        _a.SetIsOnWithoutNotify(true);
        Assert.That(_a.graphic.canvasRenderer.GetAlpha(), Is.EqualTo(1f));
        AddClip(0d, 1d, new StepChannel(true));

        EvaluateAt(0.75d);
        _a.graphic.canvasRenderer.SetAlpha(0f);
        _b.graphic.canvasRenderer.SetAlpha(1f);
        _c.graphic.canvasRenderer.SetAlpha(1f);
        StopMixer();

        Assert.That(_a.graphic.canvasRenderer.GetAlpha(), Is.EqualTo(1f));
        Assert.That(_b.graphic.canvasRenderer.GetAlpha(), Is.EqualTo(0f));
        Assert.That(_c.graphic.canvasRenderer.GetAlpha(), Is.EqualTo(0f));
    }

    [Test]
    public void Evaluate_DoesNotAllocate()
    {
        _a.SetIsOnWithoutNotify(true);
        AddClip(0d, 1d, new StepChannel(true));
        AddClip(1d, 1d, new StepChannel(false));

        var allocated = TrackTestSupport.MeasureEvaluateAllocation(_director);

        Assert.That(allocated, Is.Zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void Mixer_CarriesTheTrackFlag(bool sendCallbacks)
    {
        _track.SendCallbacks = sendCallbacks;
        AddClip(0d, 1d, new StepChannel(true));
        EvaluateAt(0.5d);

        var mixer = (ScriptPlayable<ToggleMixer>)_director.playableGraph.GetRootPlayable(0).GetInput(0);

        Assert.That(mixer.GetBehaviour().SendCallbacks, Is.EqualTo(sendCallbacks));
    }

    private static Toggle CreateToggle(string name, Transform parent, ToggleGroup group)
    {
        var toggleObject = new GameObject(name, typeof(RectTransform));
        toggleObject.transform.SetParent(parent, false);
        var checkmark = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
        checkmark.transform.SetParent(toggleObject.transform, false);

        var toggle = toggleObject.AddComponent<Toggle>();
        toggle.graphic = checkmark.GetComponent<Image>();
        toggle.group = group;
        toggle.SetIsOnWithoutNotify(false);
        toggle.graphic.canvasRenderer.SetAlpha(0f);
        return toggle;
    }

    // Clip 1 (0..1) turns B on at 0.5; clip 2 (1..2) turns it off at 1.5.
    private int ScrubWithListener(bool sendCallbacks, bool playing)
    {
        _track.SendCallbacks = sendCallbacks;
        PlayModeSeam.OverrideForTests = playing;
        AddClip(0d, 1d, new StepChannel(true));
        AddClip(1d, 1d, new StepChannel(false));

        var calls = 0;
        _b.onValueChanged.AddListener(_ => calls++);

        EvaluateAt(0.25d);
        EvaluateAt(0.75d);
        EvaluateAt(1.75d);
        return calls;
    }

    private TimelineClip AddClip(double start, double duration, StepChannel isOn)
    {
        var clip = _track.CreateClip<ToggleClip>();
        clip.start = start;
        clip.duration = duration;

        ((ToggleClip)clip.asset).Template.IsOn = isOn;
        return clip;
    }

    private void StopMixer()
    {
        var mixer = (ScriptPlayable<ToggleMixer>)_director.playableGraph.GetRootPlayable(0).GetInput(0);
        mixer.GetBehaviour().OnPlayableDestroy(mixer);
    }

    private void EvaluateAt(double time)
    {
        if (_director.playableGraph.IsValid() == false)
        {
            _director.RebuildGraph();
        }

        _director.time = time;
        _director.Evaluate();
    }
}
