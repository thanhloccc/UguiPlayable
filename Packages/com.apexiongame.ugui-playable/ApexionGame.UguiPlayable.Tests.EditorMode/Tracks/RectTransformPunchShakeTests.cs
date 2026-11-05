using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Tracks.Transforms;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Tracks;

public sealed class RectTransformPunchShakeTests
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
    public void Punch_OnScale_ReturnsExactlyToBaseAfterTheClip()
    {
        _rect.localScale = Vector3.one;
        AddClip(0d, 1d, punch: OscillationChannel.Punch(OscillationTarget.Scale, new Vector3(0.5f, 0.5f, 0f)));

        EvaluateAt(0.1d);
        Assert.That(_rect.localScale.x, Is.GreaterThan(1f));
        Assert.That(_rect.localScale.z, Is.EqualTo(1f));

        EvaluateAt(0.7d);
        EvaluateAt(1.5d);

        AssertVector(_rect.localScale, Vector3.one);
    }

    [Test]
    public void Shake_OnPosition_IsDeterministicAndDoesNotDrift()
    {
        _rect.anchoredPosition3D = new Vector3(5f, 6f, 7f);
        AddClip(0d, 1d, shake: CreateShake(OscillationTarget.Position));

        EvaluateAt(0.3d);
        var first = _rect.anchoredPosition3D;
        Assert.That((first - new Vector3(5f, 6f, 7f)).sqrMagnitude, Is.GreaterThan(1e-4f));

        EvaluateAt(0.3d);
        AssertVector(_rect.anchoredPosition3D, first);

        EvaluateAt(0.31d);
        EvaluateAt(0.3d);
        AssertVector(_rect.anchoredPosition3D, first);

        for (var i = 0; i < 50; i++)
        {
            EvaluateAt(0.3d);
        }

        AssertVector(_rect.anchoredPosition3D, first);

        EvaluateAt(1.5d);
        AssertVector(_rect.anchoredPosition3D, new Vector3(5f, 6f, 7f));
    }

    [Test]
    public void Shake_OnTopOfPositionTween_EndsExactlyAtTheTweenTarget()
    {
        var position = new TweenChannel<Vector3>(Vector3.zero, new Vector3(100f, 40f, 0f));
        AddClip(0d, 1d, shake: CreateShake(OscillationTarget.Position), position: position);

        EvaluateAt(0.5d);
        Assert.That(_rect.anchoredPosition3D.x, Is.Not.EqualTo(50f).Within(1e-3f));

        EvaluateAt(0.9d);
        EvaluateAt(1d);
        AssertVector(_rect.anchoredPosition3D, new Vector3(100f, 40f, 0f));

        EvaluateAt(1.5d);
        AssertVector(_rect.anchoredPosition3D, new Vector3(100f, 40f, 0f));
    }

    [Test]
    public void Shake_OnRotation_ReturnsToThePreShakeRotation()
    {
        _rect.localEulerAngles = new Vector3(0f, 0f, 30f);
        AddClip(0d, 1d, shake: CreateShake(OscillationTarget.Rotation));

        EvaluateAt(0.4d);
        Assert.That(Quaternion.Angle(_rect.localRotation, Quaternion.Euler(0f, 0f, 30f)), Is.GreaterThan(0.01f));

        EvaluateAt(0.6d);
        EvaluateAt(1.5d);

        Assert.That(Quaternion.Angle(_rect.localRotation, Quaternion.Euler(0f, 0f, 30f)), Is.LessThan(0.01f));
    }

    [Test]
    public void Revert_RestoresBaseOnOffsetOnlyAxes()
    {
        _rect.anchoredPosition3D = new Vector3(5f, 6f, 7f);
        _track.RestoreMode = RestoreMode.Revert;
        AddClip(0d, 1d, shake: CreateShake(OscillationTarget.Position));

        EvaluateAt(0.4d);
        Assert.That((_rect.anchoredPosition3D - new Vector3(5f, 6f, 7f)).sqrMagnitude, Is.GreaterThan(1e-4f));

        StopMixer();

        AssertVector(_rect.anchoredPosition3D, new Vector3(5f, 6f, 7f));
    }

    [Test]
    public void FromCurrentPosition_ActivatedMidShake_DoesNotCaptureTheOffset()
    {
        _rect.anchoredPosition3D = new Vector3(5f, 6f, 7f);

        var shake = CreateShake(OscillationTarget.Position);
        AddClip(0d, 1.3d, shake: shake);

        // Ends at the value the binding has before the shake, so a clean capture leaves only the shake offset.
        var fromCurrent = new TweenChannel<Vector3>(Vector3.zero, new Vector3(5f, 6f, 7f), ValueMode.FromCurrent);
        AddClip(1d, 1d, position: fromCurrent);

        EvaluateAt(0.5d);
        Assert.That((_rect.anchoredPosition3D - new Vector3(5f, 6f, 7f)).sqrMagnitude, Is.GreaterThan(1e-2f));

        EvaluateAt(1.2d);

        var mixer = _director.playableGraph.GetRootPlayable(0).GetInput(0);
        var weight = mixer.GetInputWeight(0);
        Assert.That(weight, Is.GreaterThan(0f).And.LessThan(1f));

        var offset = OscillationMath.Evaluate(shake, 1.2f / 1.3f, true) * weight;
        var actual = _rect.anchoredPosition3D;
        var expected = new Vector3(5f, 6f, 7f) + offset;

        Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.05f));
        Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.05f));
    }

    [Test]
    public void GatherProperties_IncludesTheTargetNames()
    {
        AddClip(
              0d
            , 1d
            , punch: OscillationChannel.Punch(OscillationTarget.Rotation, Vector3.one)
            , shake: CreateShake(OscillationTarget.Position)
        );

        var collector = new TrackTestSupport.RecordingCollector();
        _track.GatherProperties(_director, collector);

        Assert.That(
              collector.Names
            , Is.EquivalentTo(new[] { "m_LocalRotation", "m_AnchoredPosition", "m_LocalPosition" })
        );
    }

    [Test]
    public void DisabledChannels_AreNotCollectedAndAddNothing()
    {
        _rect.anchoredPosition3D = new Vector3(5f, 6f, 7f);
        AddClip(0d, 1d);

        var collector = new TrackTestSupport.RecordingCollector();
        _track.GatherProperties(_director, collector);
        EvaluateAt(0.5d);

        Assert.That(collector.Names, Is.Empty);
        AssertVector(_rect.anchoredPosition3D, new Vector3(5f, 6f, 7f));
    }

    [Test]
    public void Evaluate_DoesNotAllocate()
    {
        var position = new TweenChannel<Vector3>(Vector3.zero, new Vector3(100f, 0f, 0f));
        AddClip(
              0d
            , 1d
            , punch: OscillationChannel.Punch(OscillationTarget.Scale, Vector3.one * 0.2f)
            , shake: CreateShake(OscillationTarget.Rotation)
            , position: position
        );

        AddClip(1d, 1d, shake: CreateShake(OscillationTarget.Position));

        var allocated = TrackTestSupport.MeasureEvaluateAllocation(_director);

        Assert.That(allocated, Is.Zero);
    }

    private static OscillationChannel CreateShake(OscillationTarget target)
        => OscillationChannel.Shake(target, new Vector3(10f, 10f, 10f), 10f, 1f, false, 11);

    private TimelineClip AddClip(
          double start
        , double duration
        , OscillationChannel? punch = null
        , OscillationChannel? shake = null
        , TweenChannel<Vector3>? position = null
    )
    {
        var clip = _track.CreateClip<RectTransformClip>();
        clip.start = start;
        clip.duration = duration;

        var asset = (RectTransformClip)clip.asset;

        if (punch.HasValue)
        {
            asset.Template.Punch = punch.Value;
        }

        if (shake.HasValue)
        {
            asset.Template.Shake = shake.Value;
        }

        if (position.HasValue)
        {
            asset.Template.AnchoredPosition = position.Value;
        }

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
