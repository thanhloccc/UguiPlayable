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

public sealed class ScrollbarTrackTests
{
    private const float TOLERANCE = 1e-4f;

    private readonly List<Object> _created = new();
    private PlayableDirector _director;
    private TimelineAsset _timeline;
    private ScrollbarTrack _track;
    private Scrollbar _scrollbar;

    [SetUp]
    public void SetUp()
    {
        var directorObject = new GameObject("Director");
        var barObject = new GameObject("Scrollbar", typeof(RectTransform));
        _created.Add(directorObject);
        _created.Add(barObject);

        _scrollbar = barObject.AddComponent<Scrollbar>();
        _director = directorObject.AddComponent<PlayableDirector>();
        _director.playOnAwake = false;

        _timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        _created.Add(_timeline);
        _track = _timeline.CreateTrack<ScrollbarTrack>();
        _director.playableAsset = _timeline;
        _director.SetGenericBinding(_track, _scrollbar);
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
        AddClip(0d, 1d, new ScrollbarBehaviour {
            Value = new TweenChannel<float>(0f, 1f, new EaseSetting(Ease.OutQuad)),
        });

        EvaluateAt(0.5d);
        Assert.That(_scrollbar.value, Is.EqualTo(0.75f).Within(TOLERANCE));

        EvaluateAt(1.5d);
        Assert.That(_scrollbar.value, Is.EqualTo(1f).Within(TOLERANCE), "hold after the clip ends");
    }

    [Test]
    public void Size_IsOffByDefault_AndValueAloneLeavesItAlone()
    {
        _scrollbar.size = 0.4f;
        AddClip(0d, 1d, new ScrollbarBehaviour());

        EvaluateAt(0.5d);

        Assert.That(_scrollbar.value, Is.EqualTo(0.5f).Within(TOLERANCE));
        Assert.That(_scrollbar.size, Is.EqualTo(0.4f).Within(TOLERANCE));
    }

    [Test]
    public void Size_Endpoints_AreFromAndTo()
    {
        AddClip(0d, 1d, new ScrollbarBehaviour {
            Value = new TweenChannel<float>(0f, 1f, enabled: false),
            Size = new TweenChannel<float>(0.2f, 1f),
        });

        EvaluateAt(0d);
        Assert.That(_scrollbar.size, Is.EqualTo(0.2f).Within(TOLERANCE));

        EvaluateAt(1d);
        Assert.That(_scrollbar.size, Is.EqualTo(1f).Within(TOLERANCE));
    }

    [Test]
    public void ValueAndSize_AnimateTogether()
    {
        AddClip(0d, 1d, new ScrollbarBehaviour {
            Value = new TweenChannel<float>(0f, 1f),
            Size = new TweenChannel<float>(0.2f, 0.6f),
        });

        EvaluateAt(0.5d);

        Assert.That(_scrollbar.value, Is.EqualTo(0.5f).Within(TOLERANCE));
        Assert.That(_scrollbar.size, Is.EqualTo(0.4f).Within(TOLERANCE));
    }

    [Test]
    public void Size_RelativeAndFromCurrent()
    {
        _scrollbar.size = 0.3f;
        AddClip(0d, 1d, new ScrollbarBehaviour {
            Value = new TweenChannel<float>(0f, 1f, enabled: false),
            Size = new TweenChannel<float>(0f, 0.4f, ValueMode.Relative),
        });
        AddClip(2d, 1d, new ScrollbarBehaviour {
            Value = new TweenChannel<float>(0f, 1f, enabled: false),
            Size = new TweenChannel<float>(0f, 1f, ValueMode.FromCurrent),
        });

        EvaluateAt(1d);
        Assert.That(_scrollbar.size, Is.EqualTo(0.7f).Within(TOLERANCE));

        EvaluateAt(2.5d);
        Assert.That(_scrollbar.size, Is.EqualTo(0.85f).Within(TOLERANCE));
    }

    [Test]
    public void Revert_RestoresBothBaseValues()
    {
        _scrollbar.value = 0.3f;
        _scrollbar.size = 0.4f;
        _track.RestoreMode = RestoreMode.Revert;
        AddClip(0d, 1d, new ScrollbarBehaviour {
            Value = new TweenChannel<float>(0f, 1f),
            Size = new TweenChannel<float>(0.1f, 0.9f),
        });

        EvaluateAt(0.5d);
        Assert.That(_scrollbar.value, Is.EqualTo(0.5f).Within(TOLERANCE));
        Assert.That(_scrollbar.size, Is.EqualTo(0.5f).Within(TOLERANCE));
        StopMixer();

        Assert.That(_scrollbar.value, Is.EqualTo(0.3f).Within(TOLERANCE));
        Assert.That(_scrollbar.size, Is.EqualTo(0.4f).Within(TOLERANCE));
    }

    [Test]
    public void Revert_RestoresOnlyWhatWasWritten()
    {
        _scrollbar.size = 0.4f;
        _track.RestoreMode = RestoreMode.Revert;
        AddClip(0d, 1d, new ScrollbarBehaviour());

        EvaluateAt(0.5d);
        _scrollbar.size = 0.9f;
        StopMixer();

        Assert.That(_scrollbar.size, Is.EqualTo(0.9f).Within(TOLERANCE));
    }

    [Test]
    public void GatherProperties_RegistersTheEnabledChannels()
    {
        AddClip(0d, 1d, new ScrollbarBehaviour());
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(collector.Names, Is.EquivalentTo(new[] { "m_Value" }));

        AddClip(1d, 1d, new ScrollbarBehaviour { Size = new TweenChannel<float>(0.2f, 1f) });
        collector.Names.Clear();

        _track.GatherProperties(_director, collector);

        Assert.That(collector.Names, Is.EquivalentTo(new[] { "m_Value", "m_Size" }));
    }

    [Test]
    public void Evaluate_DoesNotAllocate()
    {
        AddClip(0d, 1d, new ScrollbarBehaviour());
        AddClip(1d, 1d, new ScrollbarBehaviour { Size = new TweenChannel<float>(0.2f, 1f) });

        var allocated = TrackTestSupport.MeasureEvaluateAllocation(_director);

        Assert.That(allocated, Is.Zero);
    }

    private TimelineClip AddClip(double start, double duration, ScrollbarBehaviour template)
    {
        var clip = _track.CreateClip<ScrollbarClip>();
        clip.start = start;
        clip.duration = duration;

        var asset = (ScrollbarClip)clip.asset;
        asset.Template.Value = template.Value;
        asset.Template.Size = template.Size;
        return clip;
    }

    private void StopMixer()
    {
        var mixer = (ScriptPlayable<ScrollbarMixer>)_director.playableGraph.GetRootPlayable(0).GetInput(0);
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
