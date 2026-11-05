using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Easing;
using ApexionGame.UguiPlayable.Tracks.Controls;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Tracks;

public sealed class SliderTrackTests
{
    private const float TOLERANCE = 1e-4f;

    private readonly List<Object> _created = new();
    private PlayableDirector _director;
    private TimelineAsset _timeline;
    private SliderTrack _track;
    private Slider _slider;

    [SetUp]
    public void SetUp()
    {
        var directorObject = new GameObject("Director");
        var sliderObject = new GameObject("Slider", typeof(RectTransform));
        _created.Add(directorObject);
        _created.Add(sliderObject);

        _slider = sliderObject.AddComponent<Slider>();
        _director = directorObject.AddComponent<PlayableDirector>();
        _director.playOnAwake = false;

        _timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        _created.Add(_timeline);
        _track = _timeline.CreateTrack<SliderTrack>();
        _director.playableAsset = _timeline;
        _director.SetGenericBinding(_track, _slider);
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
    public void Value_AppliesEase_AndHoldsEnd()
    {
        AddClip(0d, 1d, new TweenChannel<float>(0f, 1f, new EaseSetting(Ease.OutQuad)));

        EvaluateAt(0.5d);
        Assert.That(_slider.value, Is.EqualTo(0.75f).Within(TOLERANCE));

        EvaluateAt(1.5d);
        Assert.That(_slider.value, Is.EqualTo(1f).Within(TOLERANCE), "hold after the clip ends");
    }

    [Test]
    public void Value_Endpoints_AreFromAndTo()
    {
        AddClip(0d, 1d, new TweenChannel<float>(0.2f, 0.8f));

        EvaluateAt(0d);
        Assert.That(_slider.value, Is.EqualTo(0.2f).Within(TOLERANCE));

        EvaluateAt(1d);
        Assert.That(_slider.value, Is.EqualTo(0.8f).Within(TOLERANCE));
    }

    [Test]
    public void Value_AbsoluteIsInTheSliderRange_AndTheSetterClamps()
    {
        _slider.minValue = 0f;
        _slider.maxValue = 10f;
        AddClip(0d, 1d, new TweenChannel<float>(0f, 20f));

        EvaluateAt(0.25d);
        Assert.That(_slider.value, Is.EqualTo(5f).Within(TOLERANCE));

        EvaluateAt(1d);
        Assert.That(_slider.value, Is.EqualTo(10f).Within(TOLERANCE), "Slider.value clamps to maxValue");
    }

    [Test]
    public void Value_RelativeOffsetsTheBase()
    {
        _slider.value = 0.25f;
        AddClip(0d, 1d, new TweenChannel<float>(0f, 0.5f, ValueMode.Relative));

        EvaluateAt(1d);

        Assert.That(_slider.value, Is.EqualTo(0.75f).Within(TOLERANCE));
    }

    [Test]
    public void Value_FromCurrentStartsAtTheValueWhenTheClipBecomesActive()
    {
        _slider.value = 0.2f;
        AddClip(0d, 1d, new TweenChannel<float>(0f, 1f, ValueMode.FromCurrent));

        EvaluateAt(0.5d);

        Assert.That(_slider.value, Is.EqualTo(0.6f).Within(TOLERANCE));
    }

    [Test]
    public void Value_Revert_RestoresBaseValue()
    {
        _slider.value = 0.3f;
        _track.RestoreMode = RestoreMode.Revert;
        AddClip(0d, 1d, new TweenChannel<float>(0f, 1f));

        EvaluateAt(0.5d);
        Assert.That(_slider.value, Is.EqualTo(0.5f).Within(TOLERANCE));
        StopMixer();

        Assert.That(_slider.value, Is.EqualTo(0.3f).Within(TOLERANCE));
    }

    [Test]
    public void Value_Keep_LeavesTheLastValue()
    {
        _slider.value = 0.3f;
        AddClip(0d, 1d, new TweenChannel<float>(0f, 1f));

        EvaluateAt(0.5d);
        StopMixer();

        Assert.That(_slider.value, Is.EqualTo(0.5f).Within(TOLERANCE));
    }

    [Test]
    public void DisabledChannel_WritesNothing()
    {
        _slider.value = 0.3f;
        AddClip(0d, 1d, new TweenChannel<float>(0f, 1f, enabled: false));

        EvaluateAt(0.5d);

        Assert.That(_slider.value, Is.EqualTo(0.3f).Within(TOLERANCE));
    }

    [Test]
    public void GatherProperties_RegistersTheValue()
    {
        AddClip(0d, 1d, new TweenChannel<float>(0f, 1f));
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(collector.Names, Is.EquivalentTo(new[] { "m_Value" }));
    }

    [Test]
    public void GatherProperties_RegistersNothingWhenTheChannelIsOff()
    {
        AddClip(0d, 1d, new TweenChannel<float>(0f, 1f, enabled: false));
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(collector.Names, Is.Empty);
    }

    [Test]
    public void Evaluate_DoesNotAllocate()
    {
        AddClip(0d, 1d, new TweenChannel<float>(0f, 1f));
        AddClip(1d, 1d, new TweenChannel<float>(1f, 0f));

        var allocated = TrackTestSupport.MeasureEvaluateAllocation(_director);

        Assert.That(allocated, Is.Zero);
    }

    private TimelineClip AddClip(double start, double duration, TweenChannel<float> value)
    {
        var clip = _track.CreateClip<SliderClip>();
        clip.start = start;
        clip.duration = duration;

        ((SliderClip)clip.asset).Template.Value = value;
        return clip;
    }

    private void StopMixer()
    {
        var mixer = (ScriptPlayable<SliderMixer>)_director.playableGraph.GetRootPlayable(0).GetInput(0);
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
