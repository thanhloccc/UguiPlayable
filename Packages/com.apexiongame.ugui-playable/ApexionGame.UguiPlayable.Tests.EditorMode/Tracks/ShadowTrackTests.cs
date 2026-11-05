using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Easing;
using ApexionGame.UguiPlayable.Tracks.Effects;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Tracks;

public sealed class ShadowTrackTests
{
    private const float TOLERANCE = 1e-4f;

    private readonly List<Object> _created = new();
    private PlayableDirector _director;
    private TimelineAsset _timeline;
    private ShadowTrack _track;
    private GameObject _target;
    private Shadow _shadow;

    [SetUp]
    public void SetUp()
    {
        var directorObject = new GameObject("Director");
        _target = new GameObject("Shadow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        _created.Add(directorObject);
        _created.Add(_target);

        _shadow = _target.AddComponent<Shadow>();
        _director = directorObject.AddComponent<PlayableDirector>();
        _director.playOnAwake = false;

        _timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        _created.Add(_timeline);
        _track = _timeline.CreateTrack<ShadowTrack>();
        _director.playableAsset = _timeline;
        _director.SetGenericBinding(_track, _shadow);
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
    public void DefaultClip_FadesTheEffectColorIn()
    {
        AddClip(0d, 1d, new ShadowBehaviour());

        EvaluateAt(0d);
        AssertColor(_shadow.effectColor, new Color(0f, 0f, 0f, 0.5f));

        EvaluateAt(0.5d);
        AssertColor(_shadow.effectColor, new Color(0f, 0f, 0f, 0.75f));

        EvaluateAt(1d);
        AssertColor(_shadow.effectColor, new Color(0f, 0f, 0f, 1f));
    }

    [Test]
    public void DefaultClip_LeavesTheDistanceAlone()
    {
        _shadow.effectDistance = new Vector2(7f, -7f);
        AddClip(0d, 1d, new ShadowBehaviour());

        EvaluateAt(0.5d);

        Assert.That(_shadow.effectDistance, Is.EqualTo(new Vector2(7f, -7f)));
    }

    [Test]
    public void Distance_AppliesEase_AndHoldsEnd()
    {
        var behaviour = new ShadowBehaviour
        {
            EffectColor = new TweenChannel<Color>(Color.black, Color.black, enabled: false),
            EffectDistance = new TweenChannel<Vector2>(
                  new Vector2(1f, -1f)
                , new Vector2(5f, -5f)
                , new EaseSetting(Ease.OutQuad)
            ),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.5d);
        Assert.That(_shadow.effectDistance.x, Is.EqualTo(4f).Within(TOLERANCE));
        Assert.That(_shadow.effectDistance.y, Is.EqualTo(-4f).Within(TOLERANCE));

        EvaluateAt(1.5d);
        Assert.That(_shadow.effectDistance.x, Is.EqualTo(5f).Within(TOLERANCE), "hold after the clip ends");
        Assert.That(_shadow.effectDistance.y, Is.EqualTo(-5f).Within(TOLERANCE));
    }

    [Test]
    public void Distance_IgnoredAxis_KeepsItsCurrentValue()
    {
        _shadow.effectDistance = new Vector2(1f, -3f);

        var behaviour = new ShadowBehaviour
        {
            EffectDistance = new TweenChannel<Vector2>(
                  new Vector2(0f, 0f)
                , new Vector2(10f, 10f)
                , ignoredAxes: AxisMask.Y
            ),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.5d);

        Assert.That(_shadow.effectDistance.x, Is.EqualTo(5f).Within(TOLERANCE));
        Assert.That(_shadow.effectDistance.y, Is.EqualTo(-3f).Within(TOLERANCE));
    }

    [Test]
    public void Relative_OffsetsTheBase()
    {
        _shadow.effectColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
        _shadow.effectDistance = new Vector2(2f, -2f);

        var behaviour = new ShadowBehaviour
        {
            EffectColor = new TweenChannel<Color>(Color.clear, new Color(0.2f, 0f, 0f, 0.5f), ValueMode.Relative),
            EffectDistance = new TweenChannel<Vector2>(Vector2.zero, new Vector2(3f, -3f), ValueMode.Relative),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(1d);

        AssertColor(_shadow.effectColor, new Color(0.4f, 0.2f, 0.2f, 1f));
        Assert.That(_shadow.effectDistance.x, Is.EqualTo(5f).Within(TOLERANCE));
        Assert.That(_shadow.effectDistance.y, Is.EqualTo(-5f).Within(TOLERANCE));
    }

    [Test]
    public void FromCurrent_StartsAtTheValuesWhenTheClipBecomesActive()
    {
        _shadow.effectColor = new Color(0f, 0f, 0f, 0.2f);
        _shadow.effectDistance = new Vector2(2f, -2f);

        var behaviour = new ShadowBehaviour
        {
            EffectColor = new TweenChannel<Color>(Color.clear, new Color(0f, 0f, 0f, 1f), ValueMode.FromCurrent),
            EffectDistance = new TweenChannel<Vector2>(Vector2.zero, new Vector2(4f, -4f), ValueMode.FromCurrent),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.5d);

        AssertColor(_shadow.effectColor, new Color(0f, 0f, 0f, 0.6f));
        Assert.That(_shadow.effectDistance.x, Is.EqualTo(3f).Within(TOLERANCE));
        Assert.That(_shadow.effectDistance.y, Is.EqualTo(-3f).Within(TOLERANCE));
    }

    [Test]
    public void Revert_RestoresTheColorAndOnlyTheWrittenDistanceAxes()
    {
        _shadow.effectColor = new Color(0f, 0f, 0f, 0.3f);
        _shadow.effectDistance = new Vector2(1f, -1f);
        _track.RestoreMode = RestoreMode.Revert;

        var behaviour = new ShadowBehaviour
        {
            EffectDistance = new TweenChannel<Vector2>(
                  new Vector2(0f, 0f)
                , new Vector2(10f, 10f)
                , ignoredAxes: AxisMask.Y
            ),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.5d);
        Assert.That(_shadow.effectDistance.x, Is.EqualTo(5f).Within(TOLERANCE));

        _shadow.effectDistance = new Vector2(5f, -8f);
        StopMixer();

        AssertColor(_shadow.effectColor, new Color(0f, 0f, 0f, 0.3f));
        Assert.That(_shadow.effectDistance.x, Is.EqualTo(1f).Within(TOLERANCE));
        Assert.That(_shadow.effectDistance.y, Is.EqualTo(-8f).Within(TOLERANCE), "Y was never written");
    }

    [Test]
    public void Keep_LeavesTheLastValues()
    {
        AddClip(0d, 1d, new ShadowBehaviour());

        EvaluateAt(0.5d);
        StopMixer();

        AssertColor(_shadow.effectColor, new Color(0f, 0f, 0f, 0.75f));
    }

    [Test]
    public void BoundToAnOutline_Works()
    {
        var outline = _target.AddComponent<Outline>();
        _director.SetGenericBinding(_track, outline);

        var behaviour = new ShadowBehaviour
        {
            EffectDistance = new TweenChannel<Vector2>(new Vector2(1f, 1f), new Vector2(3f, 3f)),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.5d);

        AssertColor(outline.effectColor, new Color(0f, 0f, 0f, 0.75f));
        Assert.That(outline.effectDistance.x, Is.EqualTo(2f).Within(TOLERANCE));
        Assert.That(outline.effectDistance.y, Is.EqualTo(2f).Within(TOLERANCE));
    }

    [Test]
    public void GatherProperties_RegistersTheEnabledChannels()
    {
        var behaviour = new ShadowBehaviour
        {
            EffectDistance = new TweenChannel<Vector2>(Vector2.zero, Vector2.one),
        };
        AddClip(0d, 1d, behaviour);
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(collector.Names, Is.EquivalentTo(new[] { "m_EffectColor", "m_EffectDistance" }));
    }

    [Test]
    public void GatherProperties_RegistersNothingWhenEveryChannelIsOff()
    {
        var behaviour = new ShadowBehaviour
        {
            EffectColor = new TweenChannel<Color>(Color.black, Color.black, enabled: false),
        };
        AddClip(0d, 1d, behaviour);
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(collector.Names, Is.Empty);
    }

    [Test]
    public void Evaluate_DoesNotAllocate()
    {
        var first = new ShadowBehaviour
        {
            EffectDistance = new TweenChannel<Vector2>(Vector2.zero, new Vector2(4f, -4f), ignoredAxes: AxisMask.Y),
        };
        AddClip(0d, 1d, first);
        AddClip(1d, 1d, new ShadowBehaviour());

        var allocated = TrackTestSupport.MeasureEvaluateAllocation(_director);

        Assert.That(allocated, Is.Zero);
    }

    private static void AssertColor(Color actual, Color expected)
    {
        Assert.That(actual.r, Is.EqualTo(expected.r).Within(TOLERANCE), "r");
        Assert.That(actual.g, Is.EqualTo(expected.g).Within(TOLERANCE), "g");
        Assert.That(actual.b, Is.EqualTo(expected.b).Within(TOLERANCE), "b");
        Assert.That(actual.a, Is.EqualTo(expected.a).Within(TOLERANCE), "a");
    }

    private TimelineClip AddClip(double start, double duration, ShadowBehaviour behaviour)
    {
        var clip = _track.CreateClip<ShadowClip>();
        clip.start = start;
        clip.duration = duration;

        var template = ((ShadowClip)clip.asset).Template;
        template.EffectColor = behaviour.EffectColor;
        template.EffectDistance = behaviour.EffectDistance;
        return clip;
    }

    private void StopMixer()
    {
        var mixer = (ScriptPlayable<ShadowMixer>)_director.playableGraph.GetRootPlayable(0).GetInput(0);
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
