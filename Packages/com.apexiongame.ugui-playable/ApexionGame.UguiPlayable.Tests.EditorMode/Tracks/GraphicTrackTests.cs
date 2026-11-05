using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Tracks.Graphics;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Tracks;

public sealed class GraphicTrackTests
{
    private const float TOLERANCE = 1e-4f;

    private readonly List<Object> _created = new();
    private PlayableDirector _director;
    private TimelineAsset _timeline;
    private GraphicTrack _track;
    private Graphic _graphic;

    [SetUp]
    public void SetUp()
    {
        var directorObject = new GameObject("Director");
        var graphicObject = new GameObject("Graphic", typeof(RectTransform), typeof(CanvasRenderer));
        _created.Add(directorObject);
        _created.Add(graphicObject);

        _graphic = graphicObject.AddComponent<Image>();
        _director = directorObject.AddComponent<PlayableDirector>();
        _director.playOnAwake = false;

        _timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        _created.Add(_timeline);
        _track = _timeline.CreateTrack<GraphicTrack>();
        _director.playableAsset = _timeline;
        _director.SetGenericBinding(_track, _graphic);
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
    public void AlphaOnly_KeepsRgb()
    {
        _graphic.color = new Color(0.2f, 0.4f, 0.6f, 1f);
        var behaviour = new GraphicBehaviour { Alpha = new TweenChannel<float>(1f, 0.2f) };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.5d);

        AssertColor(_graphic.color, new Color(0.2f, 0.4f, 0.6f, 0.6f));
    }

    [Test]
    public void ColorAndAlpha_InOneClip_AlphaWinsForA()
    {
        var behaviour = new GraphicBehaviour
        {
            Color = new TweenChannel<Color>(new Color(1f, 0f, 0f, 1f), new Color(0f, 0f, 1f, 1f)),
            Alpha = new TweenChannel<float>(0f, 0.5f),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.5d);

        AssertColor(_graphic.color, new Color(0.5f, 0f, 0.5f, 0.25f));
    }

    [Test]
    public void ColorOnly_BlendsAllComponents()
    {
        var behaviour = new GraphicBehaviour
        {
            Color = new TweenChannel<Color>(new Color(1f, 0f, 0f, 1f), new Color(0f, 0f, 1f, 0f)),
            Alpha = new TweenChannel<float>(0f, 1f, enabled: false),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.5d);

        AssertColor(_graphic.color, new Color(0.5f, 0f, 0.5f, 0.5f));
    }

    [Test]
    public void ColorRelative_AddsPerComponent_AndClampsAlpha()
    {
        _graphic.color = new Color(0.2f, 0.4f, 0.6f, 0.8f);
        var behaviour = new GraphicBehaviour
        {
            Color = new TweenChannel<Color>(
                new Color(0f, 0f, 0f, 0f)
                , new Color(0.2f, 0.2f, 0.2f, 0.4f)
                , ValueMode.Relative
            ),
            Alpha = new TweenChannel<float>(0f, 1f, enabled: false),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.5d);
        AssertColor(_graphic.color, new Color(0.3f, 0.5f, 0.7f, 1f));

        EvaluateAt(1.5d);
        AssertColor(_graphic.color, new Color(0.4f, 0.6f, 0.8f, 1f));
    }

    [Test]
    public void EaseIn_BlendsFromBaseColor()
    {
        var baseColor = new Color(0.2f, 0.4f, 0.6f, 1f);
        _graphic.color = baseColor;
        var behaviour = new GraphicBehaviour
        {
            Color = new TweenChannel<Color>(Color.white, Color.white),
            Alpha = new TweenChannel<float>(0f, 1f, enabled: false),
        };
        var clip = AddClip(0d, 2d, behaviour);
        clip.easeInDuration = 1d;

        EvaluateAt(0.5d);

        var weight = clip.EvaluateMixIn(0.5d);
        Assert.That(weight, Is.GreaterThan(0f).And.LessThan(1f));
        var expected = baseColor * (1f - weight) + Color.white * weight;
        AssertColor(_graphic.color, expected);
    }

    [Test]
    public void Revert_RestoresBaseColor()
    {
        var baseColor = new Color(0.2f, 0.4f, 0.6f, 1f);
        _graphic.color = baseColor;
        _track.RestoreMode = RestoreMode.Revert;
        AddClip(0d, 1d, new GraphicBehaviour { Alpha = new TweenChannel<float>(0f, 0.2f) });

        EvaluateAt(0.5d);
        StopMixer();

        AssertColor(_graphic.color, baseColor);
    }

    [Test]
    public void LaterColorClip_OverridesEarlierAlphaHold_ForA()
    {
        _graphic.color = Color.white;
        AddClip(0d, 1d, new GraphicBehaviour { Alpha = new TweenChannel<float>(0f, 1f) });
        var red = new Color(1f, 0f, 0f, 0f);
        AddClip(2d, 1d, new GraphicBehaviour { Color = new TweenChannel<Color>(Color.white, red) });

        EvaluateAt(1.5d);
        EvaluateAt(3.5d);

        AssertColor(_graphic.color, red);
    }

    [Test]
    public void GatherProperties_RegistersColorOnce()
    {
        var behaviour = new GraphicBehaviour
        {
            Color = new TweenChannel<Color>(Color.white, Color.white),
        };
        AddClip(0d, 1d, behaviour);
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(collector.Names, Is.EquivalentTo(new[] { "m_Color" }));
    }

    [Test]
    public void Evaluate_DoesNotAllocate()
    {
        AddClip(0d, 1d, new GraphicBehaviour { Alpha = new TweenChannel<float>(0f, 1f) });
        var second = new GraphicBehaviour
        {
            Color = new TweenChannel<Color>(Color.white, Color.black),
            Alpha = new TweenChannel<float>(0f, 1f, enabled: false),
        };
        AddClip(1d, 1d, second);

        var allocated = TrackTestSupport.MeasureEvaluateAllocation(_director);

        Assert.That(allocated, Is.Zero);
    }

    private TimelineClip AddClip(double start, double duration, GraphicBehaviour template)
    {
        var clip = _track.CreateClip<GraphicClip>();
        clip.start = start;
        clip.duration = duration;

        var asset = (GraphicClip)clip.asset;
        asset.Template.Color = template.Color;
        asset.Template.Alpha = template.Alpha;
        return clip;
    }

    private void StopMixer()
    {
        var mixer = (ScriptPlayable<GraphicMixer>)_director.playableGraph.GetRootPlayable(0).GetInput(0);
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

    private static void AssertColor(Color actual, Color expected)
    {
        Assert.That(actual.r, Is.EqualTo(expected.r).Within(TOLERANCE), "r");
        Assert.That(actual.g, Is.EqualTo(expected.g).Within(TOLERANCE), "g");
        Assert.That(actual.b, Is.EqualTo(expected.b).Within(TOLERANCE), "b");
        Assert.That(actual.a, Is.EqualTo(expected.a).Within(TOLERANCE), "a");
    }
}
