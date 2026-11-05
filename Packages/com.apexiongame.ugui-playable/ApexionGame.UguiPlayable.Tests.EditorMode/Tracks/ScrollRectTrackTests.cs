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

public sealed class ScrollRectTrackTests
{
    private const float TOLERANCE = 1e-3f;

    private readonly List<Object> _created = new();
    private PlayableDirector _director;
    private TimelineAsset _timeline;
    private ScrollRectTrack _track;
    private ScrollRect _scrollRect;
    private RectTransform _content;

    [SetUp]
    public void SetUp()
    {
        var directorObject = new GameObject("Director");
        var scrollObject = new GameObject("ScrollRect", typeof(RectTransform));
        _created.Add(directorObject);
        _created.Add(scrollObject);

        // A 100 x 100 viewport over a 300 x 400 content pinned to its top-left corner: 200 hidden horizontally, 300
        // vertically.
        scrollObject.GetComponent<RectTransform>().sizeDelta = new Vector2(100f, 100f);

        var contentObject = new GameObject("Content", typeof(RectTransform));
        contentObject.transform.SetParent(scrollObject.transform, false);
        _content = contentObject.GetComponent<RectTransform>();
        _content.anchorMin = new Vector2(0f, 1f);
        _content.anchorMax = new Vector2(0f, 1f);
        _content.pivot = new Vector2(0f, 1f);
        _content.sizeDelta = new Vector2(300f, 400f);
        _content.anchoredPosition = Vector2.zero;

        _scrollRect = scrollObject.AddComponent<ScrollRect>();
        _scrollRect.content = _content;
        _director = directorObject.AddComponent<PlayableDirector>();
        _director.playOnAwake = false;

        _timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        _created.Add(_timeline);
        _track = _timeline.CreateTrack<ScrollRectTrack>();
        _director.playableAsset = _timeline;
        _director.SetGenericBinding(_track, _scrollRect);
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
    public void DefaultClip_ScrollsFromTopToBottom()
    {
        AddClip(0d, 1d, new TweenChannel<Vector2>(new Vector2(0f, 1f), Vector2.zero, ignoredAxes: AxisMask.X));

        EvaluateAt(0d);
        Assert.That(_scrollRect.normalizedPosition.y, Is.EqualTo(1f).Within(TOLERANCE));
        Assert.That(_content.anchoredPosition.y, Is.EqualTo(0f).Within(TOLERANCE));

        EvaluateAt(0.5d);
        Assert.That(_scrollRect.normalizedPosition.y, Is.EqualTo(0.5f).Within(TOLERANCE));
        Assert.That(_content.anchoredPosition.y, Is.EqualTo(150f).Within(TOLERANCE));

        EvaluateAt(1d);
        Assert.That(_scrollRect.normalizedPosition.y, Is.EqualTo(0f).Within(TOLERANCE));
    }

    [Test]
    public void Value_AppliesEase_AndHoldsEnd()
    {
        AddClip(0d, 1d, new TweenChannel<Vector2>(Vector2.zero, Vector2.one, new EaseSetting(Ease.OutQuad)));

        EvaluateAt(0.5d);
        Assert.That(_scrollRect.normalizedPosition.x, Is.EqualTo(0.75f).Within(TOLERANCE));
        Assert.That(_scrollRect.normalizedPosition.y, Is.EqualTo(0.75f).Within(TOLERANCE));

        EvaluateAt(1.5d);
        Assert.That(_scrollRect.normalizedPosition.x, Is.EqualTo(1f).Within(TOLERANCE), "hold after the clip ends");
        Assert.That(_scrollRect.normalizedPosition.y, Is.EqualTo(1f).Within(TOLERANCE));
    }

    [Test]
    public void IgnoredAxis_KeepsItsCurrentPosition()
    {
        _scrollRect.normalizedPosition = new Vector2(0.4f, 1f);
        AddClip(0d, 1d, new TweenChannel<Vector2>(new Vector2(0f, 1f), Vector2.zero, ignoredAxes: AxisMask.X));

        EvaluateAt(0.5d);

        Assert.That(_scrollRect.normalizedPosition.x, Is.EqualTo(0.4f).Within(TOLERANCE));
        Assert.That(_scrollRect.normalizedPosition.y, Is.EqualTo(0.5f).Within(TOLERANCE));
    }

    [Test]
    public void Write_ClearsTheVelocity()
    {
        _scrollRect.velocity = new Vector2(500f, 500f);
        AddClip(0d, 1d, new TweenChannel<Vector2>(new Vector2(0f, 1f), Vector2.zero));

        EvaluateAt(0.5d);

        Assert.That(_scrollRect.velocity, Is.EqualTo(Vector2.zero));
    }

    [Test]
    public void Relative_OffsetsTheBase()
    {
        _scrollRect.normalizedPosition = new Vector2(0.2f, 0.2f);
        AddClip(0d, 1d, new TweenChannel<Vector2>(Vector2.zero, new Vector2(0.5f, 0.3f), ValueMode.Relative));

        EvaluateAt(1d);

        Assert.That(_scrollRect.normalizedPosition.x, Is.EqualTo(0.7f).Within(TOLERANCE));
        Assert.That(_scrollRect.normalizedPosition.y, Is.EqualTo(0.5f).Within(TOLERANCE));
    }

    [Test]
    public void FromCurrent_StartsAtThePositionWhenTheClipBecomesActive()
    {
        _scrollRect.normalizedPosition = new Vector2(0f, 0.2f);
        AddClip(0d, 1d, new TweenChannel<Vector2>(Vector2.zero, new Vector2(0f, 1f), ValueMode.FromCurrent));

        EvaluateAt(0.5d);

        Assert.That(_scrollRect.normalizedPosition.y, Is.EqualTo(0.6f).Within(TOLERANCE));
    }

    [Test]
    public void Revert_RestoresOnlyTheAxesThatWereWritten()
    {
        _scrollRect.normalizedPosition = new Vector2(0f, 1f);
        _track.RestoreMode = RestoreMode.Revert;
        AddClip(0d, 1d, new TweenChannel<Vector2>(new Vector2(0f, 1f), Vector2.zero, ignoredAxes: AxisMask.X));

        EvaluateAt(0.5d);
        Assert.That(_scrollRect.normalizedPosition.y, Is.EqualTo(0.5f).Within(TOLERANCE));

        _scrollRect.horizontalNormalizedPosition = 0.7f;
        StopMixer();

        Assert.That(_scrollRect.normalizedPosition.y, Is.EqualTo(1f).Within(TOLERANCE));
        Assert.That(_scrollRect.normalizedPosition.x, Is.EqualTo(0.7f).Within(TOLERANCE), "X was never written");
    }

    [Test]
    public void Keep_LeavesTheLastPosition()
    {
        AddClip(0d, 1d, new TweenChannel<Vector2>(new Vector2(0f, 1f), Vector2.zero));

        EvaluateAt(0.5d);
        StopMixer();

        Assert.That(_scrollRect.normalizedPosition.y, Is.EqualTo(0.5f).Within(TOLERANCE));
    }

    [Test]
    public void ContentResize_RecapturesTheBaseBeforeAnythingIsWritten()
    {
        _content.sizeDelta = new Vector2(100f, 100f);
        _track.RestoreMode = RestoreMode.Revert;
        AddClip(1d, 1d, new TweenChannel<Vector2>(new Vector2(0f, 1f), Vector2.zero));

        // The track binds while the content has no scrollable length, so the base is captured as 0.
        EvaluateAt(0.5d);

        _content.sizeDelta = new Vector2(100f, 400f);
        EvaluateAt(1.5d);
        StopMixer();

        Assert.That(_scrollRect.normalizedPosition.y, Is.EqualTo(1f).Within(TOLERANCE), "the base was captured again");
    }

    [Test]
    public void WithoutAContent_WritesNothing()
    {
        _scrollRect.content = null;
        AddClip(0d, 1d, new TweenChannel<Vector2>(new Vector2(0f, 1f), Vector2.zero));

        Assert.DoesNotThrow(() => EvaluateAt(0.5d));
    }

    [Test]
    public void GatherProperties_RegistersTheContentPosition()
    {
        AddClip(0d, 1d, new TweenChannel<Vector2>(new Vector2(0f, 1f), Vector2.zero));
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(collector.Names, Is.EquivalentTo(new[] { "m_AnchoredPosition" }));
    }

    [Test]
    public void GatherProperties_RegistersTheScrollbarsThatAreSet()
    {
        _scrollRect.horizontalScrollbar = CreateScrollbar("Horizontal");
        _scrollRect.verticalScrollbar = CreateScrollbar("Vertical");
        AddClip(0d, 1d, new TweenChannel<Vector2>(new Vector2(0f, 1f), Vector2.zero));
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(
              collector.Names
            , Is.EquivalentTo(new[] { "m_AnchoredPosition", "m_Value", "m_Size", "m_Value", "m_Size" })
        );
    }

    [Test]
    public void GatherProperties_SkipsAMissingContent()
    {
        _scrollRect.content = null;
        AddClip(0d, 1d, new TweenChannel<Vector2>(new Vector2(0f, 1f), Vector2.zero));
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(collector.Names, Is.Empty);
    }

    [Test]
    public void GatherProperties_RegistersNothingWhenTheChannelIsOff()
    {
        AddClip(0d, 1d, new TweenChannel<Vector2>(Vector2.zero, Vector2.one, enabled: false));
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(collector.Names, Is.Empty);
    }

    [Test]
    public void Evaluate_DoesNotAllocate()
    {
        AddClip(0d, 1d, new TweenChannel<Vector2>(new Vector2(0f, 1f), Vector2.zero));
        AddClip(1d, 1d, new TweenChannel<Vector2>(Vector2.zero, Vector2.one, ignoredAxes: AxisMask.X));

        var allocated = TrackTestSupport.MeasureEvaluateAllocation(_director);

        Assert.That(allocated, Is.Zero);
    }

    private Scrollbar CreateScrollbar(string name)
    {
        var barObject = new GameObject(name, typeof(RectTransform));
        _created.Add(barObject);
        return barObject.AddComponent<Scrollbar>();
    }

    private TimelineClip AddClip(double start, double duration, TweenChannel<Vector2> position)
    {
        var clip = _track.CreateClip<ScrollRectClip>();
        clip.start = start;
        clip.duration = duration;

        ((ScrollRectClip)clip.asset).Template.NormalizedPosition = position;
        return clip;
    }

    private void StopMixer()
    {
        var mixer = (ScriptPlayable<ScrollRectMixer>)_director.playableGraph.GetRootPlayable(0).GetInput(0);
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
