using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Easing;
using ApexionGame.UguiPlayable.Tracks.Transforms;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Tracks;

public sealed class RectTransformTrackTests
{
    private const float TOLERANCE = 1e-4f;

    private readonly List<Object> _created = new();
    private PlayableDirector _director;
    private TimelineAsset _timeline;
    private RectTransformTrack _track;
    private RectTransform _rect;

    [SetUp]
    public void SetUp()
    {
        var directorObject = new GameObject("Director");
        var rectObject = new GameObject("Rect", typeof(RectTransform));
        _created.Add(directorObject);
        _created.Add(rectObject);

        _rect = rectObject.GetComponent<RectTransform>();
        _director = directorObject.AddComponent<PlayableDirector>();
        _director.playOnAwake = false;

        _timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        _created.Add(_timeline);
        _track = _timeline.CreateTrack<RectTransformTrack>();
        _director.playableAsset = _timeline;
        _director.SetGenericBinding(_track, _rect);
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
    public void Position_Absolute_InterpolatesAndHoldsEnd()
    {
        _rect.anchoredPosition3D = new Vector3(5f, 6f, 7f);
        var position = new TweenChannel<Vector3>(Vector3.zero, new Vector3(100f, 50f, 10f));
        AddClip(0d, 1d, new RectTransformBehaviour { AnchoredPosition = position });

        EvaluateAt(0.5d);
        AssertVector(_rect.anchoredPosition3D, new Vector3(50f, 25f, 5f));

        EvaluateAt(1.5d);
        AssertVector(_rect.anchoredPosition3D, new Vector3(100f, 50f, 10f));
    }

    [Test]
    public void Position_IgnoredAxes_KeepBindingValues()
    {
        _rect.anchoredPosition3D = new Vector3(5f, 6f, 7f);
        var position = new TweenChannel<Vector3>(
            new Vector3(0f, 99f, 99f)
            , new Vector3(100f, 99f, 99f)
            , ValueMode.Absolute
            , true
            , AxisMask.Y | AxisMask.Z
        );
        AddClip(0d, 1d, new RectTransformBehaviour { AnchoredPosition = position });

        EvaluateAt(0.5d);

        AssertVector(_rect.anchoredPosition3D, new Vector3(50f, 6f, 7f));
    }

    [Test]
    public void TwoClips_AnimatingDifferentAxes_DoNotDisturbEachOther()
    {
        _rect.anchoredPosition3D = new Vector3(5f, 6f, 7f);
        var xOnly = new TweenChannel<Vector3>(
            Vector3.zero
            , new Vector3(100f, 0f, 0f)
            , ValueMode.Absolute
            , true
            , AxisMask.Y | AxisMask.Z
        );
        var yOnly = new TweenChannel<Vector3>(
            Vector3.zero
            , new Vector3(0f, 40f, 0f)
            , ValueMode.Absolute
            , true
            , AxisMask.X | AxisMask.Z
        );
        AddClip(0d, 1d, new RectTransformBehaviour { AnchoredPosition = xOnly });
        AddClip(2d, 1d, new RectTransformBehaviour { AnchoredPosition = yOnly });

        EvaluateAt(1.5d);
        AssertVector(_rect.anchoredPosition3D, new Vector3(100f, 6f, 7f));

        EvaluateAt(2.5d);
        AssertVector(_rect.anchoredPosition3D, new Vector3(100f, 20f, 7f));
    }

    [Test]
    public void Scale_OutBack_Overshoots()
    {
        var scale = new TweenChannel<Vector3>(Vector3.one, Vector3.one * 2f, new EaseSetting(Ease.OutBack));
        AddClip(0d, 1d, new RectTransformBehaviour { Scale = scale });

        EvaluateAt(0.8d);

        var expected = 1f + new EaseSetting(Ease.OutBack).Evaluate(0.8f);
        Assert.That(expected, Is.GreaterThan(2f));
        Assert.That(_rect.localScale.x, Is.EqualTo(expected).Within(TOLERANCE));
        Assert.That(_rect.localScale.z, Is.EqualTo(expected).Within(TOLERANCE));
    }

    [Test]
    public void SizeDelta_Absolute_Interpolates()
    {
        var size = new TweenChannel<Vector2>(new Vector2(100f, 100f), new Vector2(200f, 60f));
        AddClip(0d, 1d, new RectTransformBehaviour { SizeDelta = size });

        EvaluateAt(0.5d);

        Assert.That(_rect.sizeDelta.x, Is.EqualTo(150f).Within(TOLERANCE));
        Assert.That(_rect.sizeDelta.y, Is.EqualTo(80f).Within(TOLERANCE));
    }

    [Test]
    public void PivotAndAnchors_Interpolate()
    {
        _rect.anchorMin = Vector2.zero;
        _rect.anchorMax = Vector2.one;
        var behaviour = new RectTransformBehaviour
        {
            Pivot = new TweenChannel<Vector2>(new Vector2(0.5f, 0.5f), new Vector2(0f, 1f)),
            AnchorMin = new TweenChannel<Vector2>(Vector2.zero, new Vector2(0.4f, 0.2f)),
            AnchorMax = new TweenChannel<Vector2>(Vector2.one, new Vector2(0.6f, 0.8f)),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.5d);

        Assert.That(_rect.pivot.x, Is.EqualTo(0.25f).Within(TOLERANCE));
        Assert.That(_rect.pivot.y, Is.EqualTo(0.75f).Within(TOLERANCE));
        Assert.That(_rect.anchorMin.x, Is.EqualTo(0.2f).Within(TOLERANCE));
        Assert.That(_rect.anchorMin.y, Is.EqualTo(0.1f).Within(TOLERANCE));
        Assert.That(_rect.anchorMax.x, Is.EqualTo(0.8f).Within(TOLERANCE));
        Assert.That(_rect.anchorMax.y, Is.EqualTo(0.9f).Within(TOLERANCE));
    }

    [Test]
    public void Rotation_Relative_AddsToBaseAngle()
    {
        _rect.localEulerAngles = new Vector3(0f, 0f, 30f);
        var rotation = new TweenChannel<Vector3>(Vector3.zero, new Vector3(0f, 0f, 60f), ValueMode.Relative);
        AddClip(0d, 1d, new RectTransformBehaviour { Rotation = rotation });

        EvaluateAt(0.5d);

        Assert.That(_rect.localEulerAngles.z, Is.EqualTo(60f).Within(1e-3f));
    }

    [Test]
    public void IgnoredAxes_KeepExternalChanges()
    {
        _rect.anchoredPosition3D = new Vector3(5f, 6f, 7f);
        var position = new TweenChannel<Vector3>(
              Vector3.zero
            , new Vector3(100f, 0f, 0f)
            , ignoredAxes: AxisMask.Y | AxisMask.Z
        );
        AddClip(0d, 1d, new RectTransformBehaviour { AnchoredPosition = position });

        EvaluateAt(0.25d);
        _rect.anchoredPosition3D = new Vector3(_rect.anchoredPosition3D.x, 40f, 7f);
        EvaluateAt(0.5d);

        AssertVector(_rect.anchoredPosition3D, new Vector3(50f, 40f, 7f));
    }

    [Test]
    public void Rotation_XBeyond90_WithIgnoredAxes_DoesNotFlip()
    {
        var rotation = new TweenChannel<Vector3>(
              Vector3.zero
            , new Vector3(180f, 0f, 0f)
            , ignoredAxes: AxisMask.Y | AxisMask.Z
        );
        AddClip(0d, 1d, new RectTransformBehaviour { Rotation = rotation });

        for (var i = 1; i <= 9; i++)
        {
            var t = i / 10f;
            EvaluateAt(t);
            var expected = Quaternion.Euler(180f * t, 0f, 0f);
            Assert.That(Quaternion.Angle(_rect.localRotation, expected), Is.LessThan(0.01f), $"t={t}");
        }
    }

    [Test]
    public void Rotation_FromCurrent_StartsFromUnwrappedHeldAngle()
    {
        var first = new TweenChannel<Vector3>(Vector3.zero, new Vector3(0f, 0f, -90f));
        AddClip(0d, 1d, new RectTransformBehaviour { Rotation = first });
        var second = new TweenChannel<Vector3>(Vector3.zero, Vector3.zero, ValueMode.FromCurrent);
        AddClip(2d, 1d, new RectTransformBehaviour { Rotation = second });

        EvaluateAt(1.5d);
        EvaluateAt(2.5d);

        var expected = Quaternion.Euler(0f, 0f, -45f);
        Assert.That(Quaternion.Angle(_rect.localRotation, expected), Is.LessThan(0.01f), "takes the 90° way, not 270°");
    }

    [Test]
    public void Rotation_NegativeBase_EaseInTakesShortWay()
    {
        _rect.localEulerAngles = new Vector3(0f, 0f, -30f);
        var clip = AddClip(0d, 2d, new RectTransformBehaviour
        {
            Rotation = new TweenChannel<Vector3>(Vector3.zero, Vector3.zero),
        });
        clip.easeInDuration = 1d;

        EvaluateAt(0.5d);

        var weight = clip.EvaluateMixIn(0.5d);
        var expected = Quaternion.Euler(0f, 0f, -30f * (1f - weight));
        Assert.That(Quaternion.Angle(_rect.localRotation, expected), Is.LessThan(0.01f));
    }

    [Test]
    public void Revert_RestoresOnlyWrittenAxes()
    {
        _rect.anchoredPosition3D = new Vector3(5f, 6f, 7f);
        _track.RestoreMode = RestoreMode.Revert;
        var position = new TweenChannel<Vector3>(
              Vector3.zero
            , new Vector3(100f, 0f, 0f)
            , ignoredAxes: AxisMask.Y | AxisMask.Z
        );
        AddClip(0d, 1d, new RectTransformBehaviour { AnchoredPosition = position });

        EvaluateAt(0.5d);
        _rect.anchoredPosition3D = new Vector3(_rect.anchoredPosition3D.x, 40f, 7f);
        StopMixer();

        AssertVector(_rect.anchoredPosition3D, new Vector3(5f, 40f, 7f));
    }

    [Test]
    public void DisabledChannels_AreNotWritten()
    {
        _rect.anchoredPosition3D = new Vector3(5f, 6f, 7f);
        _rect.localScale = new Vector3(2f, 2f, 2f);
        AddClip(0d, 1d, new RectTransformBehaviour());

        EvaluateAt(0.5d);

        AssertVector(_rect.anchoredPosition3D, new Vector3(5f, 6f, 7f));
        AssertVector(_rect.localScale, new Vector3(2f, 2f, 2f));
    }

    // See CanvasGroupTrackTests: the stop callback runs deferred in Play Mode, so the test calls it directly.
    [Test]
    public void Revert_RestoresOnlyWrittenChannels()
    {
        _rect.anchoredPosition3D = new Vector3(5f, 6f, 7f);
        _rect.localScale = Vector3.one;
        _track.RestoreMode = RestoreMode.Revert;
        var position = new TweenChannel<Vector3>(Vector3.zero, new Vector3(100f, 50f, 10f));
        AddClip(0d, 1d, new RectTransformBehaviour { AnchoredPosition = position });

        EvaluateAt(0.5d);
        _rect.localScale = new Vector3(3f, 3f, 3f);
        StopMixer();

        AssertVector(_rect.anchoredPosition3D, new Vector3(5f, 6f, 7f));
        AssertVector(_rect.localScale, new Vector3(3f, 3f, 3f));
    }

    [Test]
    public void GatherProperties_RegistersOnlyEnabledChannels()
    {
        var behaviour = new RectTransformBehaviour
        {
            AnchoredPosition = new TweenChannel<Vector3>(Vector3.zero, Vector3.one),
        };
        AddClip(0d, 1d, behaviour);
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(collector.Names, Is.EquivalentTo(new[] { "m_AnchoredPosition", "m_LocalPosition" }));
    }

    [Test]
    public void Evaluate_DoesNotAllocate()
    {
        var first = new RectTransformBehaviour
        {
            AnchoredPosition = new TweenChannel<Vector3>(Vector3.zero, new Vector3(100f, 0f, 0f)),
            Scale = new TweenChannel<Vector3>(Vector3.one, Vector3.one * 2f),
        };
        var second = new RectTransformBehaviour
        {
            AnchoredPosition = new TweenChannel<Vector3>(
                Vector3.zero
                , new Vector3(0f, 40f, 0f)
                , ValueMode.Absolute
                , true
                , AxisMask.X | AxisMask.Z
            ),
        };
        AddClip(0d, 1d, first);
        AddClip(1d, 1d, second);

        var allocated = TrackTestSupport.MeasureEvaluateAllocation(_director);

        Assert.That(allocated, Is.Zero);
    }

    private TimelineClip AddClip(double start, double duration, RectTransformBehaviour template)
    {
        var clip = _track.CreateClip<RectTransformClip>();
        clip.start = start;
        clip.duration = duration;

        var asset = (RectTransformClip)clip.asset;
        asset.Template.AnchoredPosition = template.AnchoredPosition;
        asset.Template.SizeDelta = template.SizeDelta;
        asset.Template.Rotation = template.Rotation;
        asset.Template.Scale = template.Scale;
        asset.Template.Pivot = template.Pivot;
        asset.Template.AnchorMin = template.AnchorMin;
        asset.Template.AnchorMax = template.AnchorMax;
        return clip;
    }

    private void StopMixer()
    {
        var mixer = (ScriptPlayable<RectTransformMixer>)_director.playableGraph.GetRootPlayable(0).GetInput(0);
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

    private static void AssertVector(Vector3 actual, Vector3 expected)
    {
        Assert.That(actual.x, Is.EqualTo(expected.x).Within(TOLERANCE), "x");
        Assert.That(actual.y, Is.EqualTo(expected.y).Within(TOLERANCE), "y");
        Assert.That(actual.z, Is.EqualTo(expected.z).Within(TOLERANCE), "z");
    }
}
