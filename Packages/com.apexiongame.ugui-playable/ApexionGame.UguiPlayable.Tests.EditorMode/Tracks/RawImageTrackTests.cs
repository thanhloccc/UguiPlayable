using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Easing;
using ApexionGame.UguiPlayable.Tracks.Graphics;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Tracks;

public sealed class RawImageTrackTests
{
    private const float TOLERANCE = 1e-4f;

    private readonly List<Object> _created = new();
    private PlayableDirector _director;
    private TimelineAsset _timeline;
    private RawImageTrack _track;
    private RawImage _rawImage;

    [SetUp]
    public void SetUp()
    {
        var directorObject = new GameObject("Director");
        var imageObject = new GameObject("RawImage", typeof(RectTransform), typeof(CanvasRenderer));
        _created.Add(directorObject);
        _created.Add(imageObject);

        _rawImage = imageObject.AddComponent<RawImage>();
        _director = directorObject.AddComponent<PlayableDirector>();
        _director.playOnAwake = false;

        _timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        _created.Add(_timeline);
        _track = _timeline.CreateTrack<RawImageTrack>();
        _director.playableAsset = _timeline;
        _director.SetGenericBinding(_track, _rawImage);
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
    public void DefaultClip_ScrollsTheTextureOneWidthInX()
    {
        AddClip(0d, 1d, new RawImageBehaviour());

        EvaluateAt(0d);
        AssertRect(_rawImage.uvRect, new Rect(0f, 0f, 1f, 1f));

        EvaluateAt(0.5d);
        AssertRect(_rawImage.uvRect, new Rect(0.5f, 0f, 1f, 1f));

        EvaluateAt(1d);
        AssertRect(_rawImage.uvRect, new Rect(1f, 0f, 1f, 1f));
    }

    [Test]
    public void DefaultClip_LeavesTheColorAlone()
    {
        _rawImage.color = new Color(0.2f, 0.4f, 0.6f, 0.8f);
        AddClip(0d, 1d, new RawImageBehaviour());

        EvaluateAt(0.5d);

        Assert.That(_rawImage.color, Is.EqualTo(new Color(0.2f, 0.4f, 0.6f, 0.8f)));
    }

    [Test]
    public void ColorAndAlpha_BlendLikeAnImage()
    {
        var behaviour = new RawImageBehaviour
        {
            Color = new TweenChannel<Color>(new Color(1f, 0f, 0f, 1f), new Color(0f, 0f, 1f, 1f)),
            Alpha = new TweenChannel<float>(0f, 0.5f),
            UvRect = new TweenChannel<Vector4>(Vector4.zero, Vector4.one, enabled: false),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.5d);

        var color = _rawImage.color;
        Assert.That(color.r, Is.EqualTo(0.5f).Within(TOLERANCE));
        Assert.That(color.b, Is.EqualTo(0.5f).Within(TOLERANCE));
        Assert.That(color.a, Is.EqualTo(0.25f).Within(TOLERANCE), "alpha wins for A");
        AssertRect(_rawImage.uvRect, new Rect(0f, 0f, 1f, 1f));
    }

    [Test]
    public void UvRect_AppliesEase_AndHoldsEnd()
    {
        var behaviour = new RawImageBehaviour
        {
            UvRect = new TweenChannel<Vector4>(
                  new Vector4(0f, 0f, 1f, 1f)
                , new Vector4(0f, 0f, 0.5f, 0.5f)
                , new EaseSetting(Ease.OutQuad)
            ),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.5d);
        AssertRect(_rawImage.uvRect, new Rect(0f, 0f, 0.625f, 0.625f));

        EvaluateAt(1.5d);
        AssertRect(_rawImage.uvRect, new Rect(0f, 0f, 0.5f, 0.5f));
    }

    [Test]
    public void UvRect_IgnoredAxes_KeepTheirCurrentValue()
    {
        _rawImage.uvRect = new Rect(0.1f, 0.2f, 0.3f, 0.4f);

        var behaviour = new RawImageBehaviour
        {
            UvRect = new TweenChannel<Vector4>(
                  Vector4.zero
                , new Vector4(1f, 1f, 1f, 1f)
                , ignoredAxes: AxisMask.Y | AxisMask.Z | AxisMask.W
            ),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.5d);

        AssertRect(_rawImage.uvRect, new Rect(0.5f, 0.2f, 0.3f, 0.4f));
    }

    [Test]
    public void UvRect_Relative_OffsetsTheBase()
    {
        _rawImage.uvRect = new Rect(0.25f, 0f, 1f, 1f);

        var behaviour = new RawImageBehaviour
        {
            UvRect = new TweenChannel<Vector4>(Vector4.zero, new Vector4(0.5f, 0f, 0f, 0f), ValueMode.Relative),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(1d);

        AssertRect(_rawImage.uvRect, new Rect(0.75f, 0f, 1f, 1f));
    }

    [Test]
    public void UvRect_FromCurrent_StartsAtTheRectWhenTheClipBecomesActive()
    {
        _rawImage.uvRect = new Rect(0.2f, 0f, 1f, 1f);

        var behaviour = new RawImageBehaviour
        {
            UvRect = new TweenChannel<Vector4>(Vector4.zero, new Vector4(1f, 0f, 1f, 1f), ValueMode.FromCurrent),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.5d);

        AssertRect(_rawImage.uvRect, new Rect(0.6f, 0f, 1f, 1f));
    }

    [Test]
    public void Revert_RestoresTheColorAndOnlyTheWrittenUvAxes()
    {
        _rawImage.color = new Color(0.2f, 0.4f, 0.6f, 1f);
        _rawImage.uvRect = new Rect(0f, 0f, 1f, 1f);
        _track.RestoreMode = RestoreMode.Revert;

        var behaviour = new RawImageBehaviour
        {
            Alpha = new TweenChannel<float>(0.5f, 0.5f),
            UvRect = new TweenChannel<Vector4>(
                  Vector4.zero
                , new Vector4(1f, 1f, 1f, 1f)
                , ignoredAxes: AxisMask.Y | AxisMask.Z | AxisMask.W
            ),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.5d);
        Assert.That(_rawImage.color.a, Is.EqualTo(0.5f).Within(TOLERANCE));

        _rawImage.uvRect = new Rect(0.5f, 0.3f, 1f, 1f);
        StopMixer();

        Assert.That(_rawImage.color.a, Is.EqualTo(1f).Within(TOLERANCE));
        AssertRect(_rawImage.uvRect, new Rect(0f, 0.3f, 1f, 1f));
    }

    [Test]
    public void Keep_LeavesTheLastRect()
    {
        AddClip(0d, 1d, new RawImageBehaviour());

        EvaluateAt(0.5d);
        StopMixer();

        AssertRect(_rawImage.uvRect, new Rect(0.5f, 0f, 1f, 1f));
    }

    [Test]
    public void GatherProperties_RegistersTheColorAndUvRect()
    {
        var behaviour = new RawImageBehaviour
        {
            Alpha = new TweenChannel<float>(0f, 1f),
        };
        AddClip(0d, 1d, behaviour);
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(collector.Names, Is.EquivalentTo(new[] { "m_Color", "m_UVRect" }));
    }

    [Test]
    public void GatherProperties_RegistersNothingWhenEveryChannelIsOff()
    {
        var behaviour = new RawImageBehaviour
        {
            UvRect = new TweenChannel<Vector4>(Vector4.zero, Vector4.one, enabled: false),
        };
        AddClip(0d, 1d, behaviour);
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(collector.Names, Is.Empty);
    }

    [Test]
    public void Evaluate_DoesNotAllocate()
    {
        var first = new RawImageBehaviour
        {
            Color = new TweenChannel<Color>(Color.white, Color.black),
            Alpha = new TweenChannel<float>(0f, 1f),
        };
        AddClip(0d, 1d, first);
        AddClip(1d, 1d, new RawImageBehaviour());

        var allocated = TrackTestSupport.MeasureEvaluateAllocation(_director);

        Assert.That(allocated, Is.Zero);
    }

    private static void AssertRect(Rect actual, Rect expected)
    {
        Assert.That(actual.x, Is.EqualTo(expected.x).Within(TOLERANCE), "x");
        Assert.That(actual.y, Is.EqualTo(expected.y).Within(TOLERANCE), "y");
        Assert.That(actual.width, Is.EqualTo(expected.width).Within(TOLERANCE), "width");
        Assert.That(actual.height, Is.EqualTo(expected.height).Within(TOLERANCE), "height");
    }

    private TimelineClip AddClip(double start, double duration, RawImageBehaviour behaviour)
    {
        var clip = _track.CreateClip<RawImageClip>();
        clip.start = start;
        clip.duration = duration;

        var template = ((RawImageClip)clip.asset).Template;
        template.Color = behaviour.Color;
        template.Alpha = behaviour.Alpha;
        template.UvRect = behaviour.UvRect;
        return clip;
    }

    private void StopMixer()
    {
        var mixer = (ScriptPlayable<RawImageMixer>)_director.playableGraph.GetRootPlayable(0).GetInput(0);
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
