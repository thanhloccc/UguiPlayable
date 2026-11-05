using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Tracks.Transforms;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Tracks;

public sealed class RectTransformRotationModeTests
{
    private const float ANGLE_TOLERANCE = 0.01f;
    private const float WEIGHT_TOLERANCE = 1e-4f;

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
    public void NewBehaviour_IsEuler_AndDataWithoutTheFieldLoadsAsEuler()
    {
        Assert.AreEqual(RotationMode.Euler, new RectTransformBehaviour().RotationMode);

        var shortest = new RectTransformBehaviour { RotationMode = RotationMode.ShortestPath };
        var json = JsonUtility.ToJson(shortest);
        Assert.That(json, Does.Contain("\"_rotationMode\":1"));

        var oldJson = json.Replace("\"_rotationMode\":1,", string.Empty);
        Assert.That(oldJson, Does.Not.Contain("_rotationMode"));

        var loaded = (RectTransformBehaviour)JsonUtility.FromJson(oldJson, typeof(RectTransformBehaviour));

        Assert.AreEqual(RotationMode.Euler, loaded.RotationMode);
    }

    [Test]
    public void Absolute_350To10_ShortestPathTurnsThroughZero()
    {
        AddRotationClip(0d, 1d, RotationZ(350f, 10f), RotationMode.ShortestPath);

        EvaluateAt(0.5d);

        AssertRotation(Quaternion.identity);
    }

    [Test]
    public void Absolute_350To10_EulerStillTakesTheLongWay()
    {
        AddRotationClip(0d, 1d, RotationZ(350f, 10f), RotationMode.Euler);

        EvaluateAt(0.5d);

        AssertRotation(Quaternion.Euler(0f, 0f, 180f));
    }

    [Test]
    public void ShortestPath_IgnoresTheIgnoredAxes()
    {
        var rotation = new TweenChannel<Vector3>(
              Vector3.zero
            , new Vector3(0f, 0f, 90f)
            , ignoredAxes: AxisMask.Z
        );
        AddRotationClip(0d, 1d, rotation, RotationMode.ShortestPath);

        EvaluateAt(0.5d);

        AssertRotation(Quaternion.Euler(0f, 0f, 45f));
    }

    [Test]
    public void Hold_AfterAShortestClip_IsItsEnd_AndALaterEulerClipContinuesFromIt()
    {
        AddRotationClip(0d, 1d, RotationZ(350f, 10f), RotationMode.ShortestPath);
        var fromCurrent = new TweenChannel<Vector3>(Vector3.zero, new Vector3(0f, 0f, 40f), ValueMode.FromCurrent);
        AddRotationClip(2d, 1d, fromCurrent, RotationMode.Euler);

        EvaluateAt(1.5d);
        AssertRotation(Quaternion.Euler(0f, 0f, 10f));

        EvaluateAt(2.5d);
        AssertRotation(Quaternion.Euler(0f, 0f, 25f));
    }

    [Test]
    public void FromCurrent_ShortestPath_StartsAtTheHeldRotation()
    {
        AddRotationClip(0d, 1d, RotationZ(0f, 170f), RotationMode.ShortestPath);
        var fromCurrent = new TweenChannel<Vector3>(Vector3.zero, new Vector3(0f, 0f, -170f), ValueMode.FromCurrent);
        AddRotationClip(2d, 1d, fromCurrent, RotationMode.ShortestPath);

        EvaluateAt(1.5d);
        EvaluateAt(2.5d);

        AssertRotation(Quaternion.Euler(0f, 0f, 180f));
    }

    [Test]
    public void Relative_ShortestPath_ComposesOnTheBaseRotation()
    {
        var baseRotation = Quaternion.Euler(30f, 0f, 0f);
        _rect.localRotation = baseRotation;
        var rotation = new TweenChannel<Vector3>(Vector3.zero, new Vector3(0f, 0f, 60f), ValueMode.Relative);
        AddRotationClip(0d, 1d, rotation, RotationMode.ShortestPath);

        EvaluateAt(0.5d);

        AssertRotation(baseRotation * Quaternion.Euler(0f, 0f, 30f));
    }

    [Test]
    public void MixedOverlap_TakesTheQuaternionPath_AndIsDeterministic()
    {
        _rect.localEulerAngles = new Vector3(0f, 0f, 10f);
        AddRotationClip(0d, 1d, RotationZ(0f, 90f), RotationMode.Euler);
        AddRotationClip(0.5d, 1d, RotationZ(90f, 180f), RotationMode.ShortestPath);

        EvaluateAt(0.75d);
        AssertHalfWeights();

        var first = _rect.localRotation;

        // 67.5° and 112.5° at half weight each; the Euler path alone would top the Euler clip up with the base.
        AssertRotation(Quaternion.Euler(0f, 0f, 90f));

        EvaluateAt(0.3d);
        EvaluateAt(1.2d);
        EvaluateAt(0.75d);

        Assert.That(Quaternion.Angle(_rect.localRotation, first), Is.LessThan(1e-3f));
    }

    [Test]
    public void Crossfade_EulerToShortestPath_DoesNotPullTowardTheBase()
    {
        AddRotationClip(0d, 1d, RotationZ(0f, 40f), RotationMode.Euler);
        AddRotationClip(0.5d, 1d, RotationZ(40f, 80f), RotationMode.ShortestPath);

        // The Euler clip is at 30° and the Shortest-path clip at 50°, weighing 0.5 each.
        EvaluateAt(0.75d);
        AssertHalfWeights();

        AssertRotation(Quaternion.Euler(0f, 0f, 40f));
    }

    [Test]
    public void Hold_EulerClipEndedLast_HoldsTheEulerEnd()
    {
        AddRotationClip(0d, 1d, RotationZ(0f, 20f), RotationMode.ShortestPath);
        AddRotationClip(2d, 1d, RotationZ(0f, 60f), RotationMode.Euler);

        EvaluateAt(1.5d);
        AssertRotation(Quaternion.Euler(0f, 0f, 20f));

        EvaluateAt(3.5d);
        AssertRotation(Quaternion.Euler(0f, 0f, 60f));
        Assert.That(_rect.localEulerAngles.z, Is.EqualTo(60f).Within(1e-3f));
    }

    [Test]
    public void Hold_ShortestClipEndedLast_HoldsTheQuaternionEnd()
    {
        AddRotationClip(0d, 1d, RotationZ(0f, 60f), RotationMode.Euler);
        AddRotationClip(2d, 1d, RotationZ(0f, 20f), RotationMode.ShortestPath);

        EvaluateAt(1.5d);
        AssertRotation(Quaternion.Euler(0f, 0f, 60f));

        EvaluateAt(3.5d);
        AssertRotation(Quaternion.Euler(0f, 0f, 20f));
    }

    [Test]
    public void Shake_OnAShortestClip_ComposesOnTheTween_AndEndsExactlyOnIt()
    {
        var shake = OscillationChannel.Shake(
              target: OscillationTarget.Rotation
            , strength: new Vector3(10f, 10f, 10f)
            , frequency: 10f
            , randomness: 1f
            , fadeOut: false
            , seed: 11
        );
        AddRotationClip(0d, 1d, RotationZ(0f, 90f), RotationMode.ShortestPath, shake);

        EvaluateAt(0.4d);
        var tweenOnly = Quaternion.Euler(0f, 0f, 36f);
        Assert.That(Quaternion.Angle(_rect.localRotation, tweenOnly), Is.GreaterThan(ANGLE_TOLERANCE));

        EvaluateAt(0.5d);
        var offset = OscillationMath.Evaluate(channel: shake, p: 0.5f, isShake: true);
        AssertRotation(Quaternion.Euler(0f, 0f, 45f) * Quaternion.Euler(offset));

        EvaluateAt(0.7d);
        EvaluateAt(1.5d);

        AssertRotation(Quaternion.Euler(0f, 0f, 90f));
    }

    [Test]
    public void ExternalRotation_BetweenFrames_IsResyncedOnTheNextFrame()
    {
        AddRotationClip(0d, 1d, RotationZ(0f, 20f), RotationMode.ShortestPath);
        var fromCurrent = new TweenChannel<Vector3>(Vector3.zero, Vector3.zero, ValueMode.FromCurrent);
        AddRotationClip(2d, 1d, fromCurrent, RotationMode.ShortestPath);

        EvaluateAt(1.5d);
        _rect.localRotation = Quaternion.Euler(0f, 0f, 100f);
        EvaluateAt(2.5d);

        AssertRotation(Quaternion.Euler(0f, 0f, 50f));
    }

    // See CanvasGroupTrackTests: the stop callback runs deferred in Play Mode, so the test calls it directly.
    [Test]
    public void Revert_AfterAShortestWrite_RestoresTheRotationExactly()
    {
        var baseRotation = Quaternion.Euler(10f, 20f, 30f);
        _rect.localRotation = baseRotation;
        _track.RestoreMode = RestoreMode.Revert;
        AddRotationClip(0d, 1d, RotationZ(0f, 90f), RotationMode.ShortestPath);

        EvaluateAt(0.5d);
        Assert.That(Quaternion.Angle(_rect.localRotation, baseRotation), Is.GreaterThan(1f));

        StopMixer();

        AssertRotation(baseRotation);
    }

    [Test]
    public void Evaluate_DoesNotAllocate()
    {
        var shake = OscillationChannel.Shake(OscillationTarget.Rotation, new Vector3(10f, 10f, 10f));
        AddRotationClip(0d, 1d, RotationZ(0f, 90f), RotationMode.Euler);
        AddRotationClip(0.5d, 1d, RotationZ(0f, 350f), RotationMode.ShortestPath, shake);

        var fromCurrent = new TweenChannel<Vector3>(Vector3.zero, new Vector3(0f, 90f, 0f), ValueMode.FromCurrent);
        AddRotationClip(1.6d, 0.3d, fromCurrent, RotationMode.ShortestPath);

        var allocated = TrackTestSupport.MeasureEvaluateAllocation(_director);

        Assert.That(allocated, Is.Zero);
    }

    private static TweenChannel<Vector3> RotationZ(float from, float to)
        => new(new Vector3(0f, 0f, from), new Vector3(0f, 0f, to));

    private TimelineClip AddRotationClip(
          double start
        , double duration
        , TweenChannel<Vector3> rotation
        , RotationMode mode
        , OscillationChannel? shake = null
    )
    {
        var clip = _track.CreateClip<RectTransformClip>();
        clip.start = start;
        clip.duration = duration;

        var asset = (RectTransformClip)clip.asset;
        asset.Template.Rotation = rotation;
        asset.Template.RotationMode = mode;

        if (shake.HasValue)
        {
            asset.Template.Shake = shake.Value;
        }

        return clip;
    }

    private void AssertHalfWeights()
    {
        var mixer = _director.playableGraph.GetRootPlayable(0).GetInput(0);

        Assert.That(mixer.GetInputWeight(0), Is.EqualTo(0.5f).Within(WEIGHT_TOLERANCE), "Euler clip weight");
        Assert.That(mixer.GetInputWeight(1), Is.EqualTo(0.5f).Within(WEIGHT_TOLERANCE), "Shortest clip weight");
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

    private void AssertRotation(Quaternion expected)
    {
        var angle = Quaternion.Angle(_rect.localRotation, expected);

        Assert.That(angle, Is.LessThan(ANGLE_TOLERANCE), $"{_rect.localEulerAngles} vs {expected.eulerAngles}");
    }
}
