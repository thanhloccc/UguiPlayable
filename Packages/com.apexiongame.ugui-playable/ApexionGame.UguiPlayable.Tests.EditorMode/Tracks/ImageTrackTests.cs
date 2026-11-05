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

public sealed class ImageTrackTests
{
    private const float TOLERANCE = 1e-4f;

    private readonly List<Object> _created = new();
    private PlayableDirector _director;
    private TimelineAsset _timeline;
    private ImageTrack _track;
    private Image _image;

    [SetUp]
    public void SetUp()
    {
        var directorObject = new GameObject("Director");
        var imageObject = new GameObject("Image", typeof(RectTransform), typeof(CanvasRenderer));
        _created.Add(directorObject);
        _created.Add(imageObject);

        _image = imageObject.AddComponent<Image>();
        _image.type = Image.Type.Filled;
        _director = directorObject.AddComponent<PlayableDirector>();
        _director.playOnAwake = false;

        _timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        _created.Add(_timeline);
        _track = _timeline.CreateTrack<ImageTrack>();
        _director.playableAsset = _timeline;
        _director.SetGenericBinding(_track, _image);
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
    public void FillAmount_AppliesEase_AndHoldsEnd()
    {
        var fill = new TweenChannel<float>(0f, 1f, new EaseSetting(Ease.OutQuad));
        var behaviour = new ImageBehaviour
        {
            Alpha = new TweenChannel<float>(0f, 1f, enabled: false),
            FillAmount = fill,
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.5d);
        Assert.That(_image.fillAmount, Is.EqualTo(0.75f).Within(TOLERANCE));

        EvaluateAt(1.5d);
        Assert.That(_image.fillAmount, Is.EqualTo(1f).Within(TOLERANCE));
    }

    [Test]
    public void FillAmount_Revert_RestoresBaseValue()
    {
        _image.fillAmount = 0.3f;
        _track.RestoreMode = RestoreMode.Revert;
        var behaviour = new ImageBehaviour
        {
            Alpha = new TweenChannel<float>(0f, 1f, enabled: false),
            FillAmount = new TweenChannel<float>(0f, 1f),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.5d);
        Assert.That(_image.fillAmount, Is.EqualTo(0.5f).Within(TOLERANCE));
        StopMixer();

        Assert.That(_image.fillAmount, Is.EqualTo(0.3f).Within(TOLERANCE));
    }

    [Test]
    public void ColorAndAlpha_BehaveLikeGraphic()
    {
        var behaviour = new ImageBehaviour
        {
            Color = new TweenChannel<Color>(new Color(1f, 0f, 0f, 1f), new Color(0f, 0f, 1f, 1f)),
            Alpha = new TweenChannel<float>(0f, 0.5f),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.5d);

        var color = _image.color;
        Assert.That(color.r, Is.EqualTo(0.5f).Within(TOLERANCE));
        Assert.That(color.g, Is.EqualTo(0f).Within(TOLERANCE));
        Assert.That(color.b, Is.EqualTo(0.5f).Within(TOLERANCE));
        Assert.That(color.a, Is.EqualTo(0.25f).Within(TOLERANCE), "alpha wins for A");
    }

    [Test]
    public void AlphaOnly_KeepsRgb()
    {
        _image.color = new Color(0.2f, 0.4f, 0.6f, 1f);
        AddClip(0d, 1d, new ImageBehaviour { Alpha = new TweenChannel<float>(1f, 0.2f) });

        EvaluateAt(0.5d);

        var color = _image.color;
        Assert.That(color.r, Is.EqualTo(0.2f).Within(TOLERANCE));
        Assert.That(color.g, Is.EqualTo(0.4f).Within(TOLERANCE));
        Assert.That(color.b, Is.EqualTo(0.6f).Within(TOLERANCE));
        Assert.That(color.a, Is.EqualTo(0.6f).Within(TOLERANCE));
    }

    [Test]
    public void GatherProperties_RegistersColorAndFillAmount()
    {
        var behaviour = new ImageBehaviour
        {
            Alpha = new TweenChannel<float>(0f, 1f),
            FillAmount = new TweenChannel<float>(0f, 1f),
        };
        AddClip(0d, 1d, behaviour);
        AddClip(1d, 1d, new ImageBehaviour { FillAmount = new TweenChannel<float>(1f, 0f) });
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(collector.Names, Is.EquivalentTo(new[] { "m_Color", "m_FillAmount" }));
    }

    [Test]
    public void Evaluate_DoesNotAllocate()
    {
        var first = new ImageBehaviour
        {
            FillAmount = new TweenChannel<float>(0f, 1f),
        };
        var second = new ImageBehaviour
        {
            Color = new TweenChannel<Color>(Color.white, Color.black),
        };
        AddClip(0d, 1d, first);
        AddClip(1d, 1d, second);

        var allocated = TrackTestSupport.MeasureEvaluateAllocation(_director);

        Assert.That(allocated, Is.Zero);
    }

    private TimelineClip AddClip(double start, double duration, ImageBehaviour template)
    {
        var clip = _track.CreateClip<ImageClip>();
        clip.start = start;
        clip.duration = duration;

        var asset = (ImageClip)clip.asset;
        asset.Template.Color = template.Color;
        asset.Template.Alpha = template.Alpha;
        asset.Template.FillAmount = template.FillAmount;
        return clip;
    }

    private void StopMixer()
    {
        var mixer = (ScriptPlayable<ImageMixer>)_director.playableGraph.GetRootPlayable(0).GetInput(0);
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
