using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Easing;
using ApexionGame.UguiPlayable.Tracks.Layout;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Tracks;

public sealed class LayoutGroupTrackTests
{
    private const float TOLERANCE = 1e-3f;

    private readonly List<Object> _created = new();
    private PlayableDirector _director;
    private TimelineAsset _timeline;
    private LayoutGroupTrack _track;
    private HorizontalLayoutGroup _group;

    [SetUp]
    public void SetUp()
    {
        var directorObject = new GameObject("Director");
        var groupObject = new GameObject("Row", typeof(RectTransform));
        _created.Add(directorObject);
        _created.Add(groupObject);

        _group = groupObject.AddComponent<HorizontalLayoutGroup>();
        _director = directorObject.AddComponent<PlayableDirector>();
        _director.playOnAwake = false;

        _timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        _created.Add(_timeline);
        _track = _timeline.CreateTrack<LayoutGroupTrack>();
        _director.playableAsset = _timeline;
        _director.SetGenericBinding(_track, _group);
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
    public void Defaults_SpacingOnPaddingOff()
    {
        var behaviour = new LayoutGroupBehaviour();

        Assert.IsTrue(behaviour.Spacing.Enabled);
        Assert.IsFalse(behaviour.Padding.Enabled);
        Assert.AreEqual(20f, behaviour.Spacing.To);
        Assert.AreEqual(new Vector4(10f, 10f, 10f, 10f), behaviour.Padding.To);
    }

    [Test]
    public void Spacing_BlendsFromToAtEndpointsAndMiddle()
    {
        AddClip(0d, 1d, new LayoutGroupBehaviour { Spacing = new TweenChannel<float>(0f, 40f) });

        EvaluateAt(0d);
        Assert.That(_group.spacing, Is.EqualTo(0f).Within(TOLERANCE));

        EvaluateAt(0.5d);
        Assert.That(_group.spacing, Is.EqualTo(20f).Within(TOLERANCE));

        EvaluateAt(1d);
        Assert.That(_group.spacing, Is.EqualTo(40f).Within(TOLERANCE));
    }

    [Test]
    public void Spacing_AppliesEase_AndHoldsEnd()
    {
        AddClip(
              0d
            , 1d
            , new LayoutGroupBehaviour { Spacing = new TweenChannel<float>(0f, 100f, new EaseSetting(Ease.OutQuad)) }
        );

        EvaluateAt(0.5d);
        Assert.That(_group.spacing, Is.EqualTo(75f).Within(TOLERANCE));

        EvaluateAt(1.5d);
        Assert.That(_group.spacing, Is.EqualTo(100f).Within(TOLERANCE), "hold after the clip ends");
    }

    [Test]
    public void Spacing_Relative_OffsetsTheBase()
    {
        _group.spacing = 10f;
        AddClip(0d, 1d, new LayoutGroupBehaviour { Spacing = new TweenChannel<float>(0f, 30f, ValueMode.Relative) });

        EvaluateAt(1d);

        Assert.That(_group.spacing, Is.EqualTo(40f).Within(TOLERANCE));
    }

    [Test]
    public void Spacing_FromCurrent_StartsAtTheValueWhenTheClipBecomesActive()
    {
        _group.spacing = 20f;
        AddClip(0d, 1d, new LayoutGroupBehaviour { Spacing = new TweenChannel<float>(0f, 100f, ValueMode.FromCurrent) });

        EvaluateAt(0.5d);

        Assert.That(_group.spacing, Is.EqualTo(60f).Within(TOLERANCE));
    }

    [Test]
    public void Padding_WritesRoundedIntegers()
    {
        var behaviour = new LayoutGroupBehaviour
        {
            Spacing = new TweenChannel<float>(0f, 0f, enabled: false),
            Padding = new TweenChannel<Vector4>(Vector4.zero, new Vector4(11f, 21f, 31f, 41f)),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.5d);

        Assert.AreEqual(6, _group.padding.left, "5.5 rounds to even");
        Assert.AreEqual(10, _group.padding.right, "10.5 rounds to even");
        Assert.AreEqual(16, _group.padding.top, "15.5 rounds to even");
        Assert.AreEqual(20, _group.padding.bottom, "20.5 rounds to even");

        EvaluateAt(1d);

        Assert.AreEqual(11, _group.padding.left);
        Assert.AreEqual(21, _group.padding.right);
        Assert.AreEqual(31, _group.padding.top);
        Assert.AreEqual(41, _group.padding.bottom);
    }

    [Test]
    public void Padding_IgnoredAxes_KeepTheirCurrentValue()
    {
        _group.padding = new RectOffset(1, 2, 3, 4);
        var behaviour = new LayoutGroupBehaviour
        {
            Spacing = new TweenChannel<float>(0f, 0f, enabled: false),
            Padding = new TweenChannel<Vector4>(
                  Vector4.zero
                , new Vector4(20f, 20f, 20f, 20f)
                , ignoredAxes: AxisMask.Y | AxisMask.Z | AxisMask.W
            ),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(1d);

        Assert.AreEqual(20, _group.padding.left);
        Assert.AreEqual(2, _group.padding.right);
        Assert.AreEqual(3, _group.padding.top);
        Assert.AreEqual(4, _group.padding.bottom);
    }

    [Test]
    public void Padding_KeepsTheSameRectOffsetInstance()
    {
        var before = _group.padding;
        AddClip(
              0d
            , 1d
            , new LayoutGroupBehaviour { Padding = new TweenChannel<Vector4>(Vector4.zero, new Vector4(8f, 8f, 8f, 8f)) }
        );

        EvaluateAt(1d);

        Assert.AreSame(before, _group.padding);
    }

    [Test]
    public void Revert_RestoresTheWrittenSpacingAndOnlyTheWrittenPaddingSides()
    {
        _group.spacing = 5f;
        _group.padding = new RectOffset(1, 2, 3, 4);
        _track.RestoreMode = RestoreMode.Revert;

        var behaviour = new LayoutGroupBehaviour
        {
            Spacing = new TweenChannel<float>(0f, 50f),
            Padding = new TweenChannel<Vector4>(
                  Vector4.zero
                , new Vector4(20f, 20f, 20f, 20f)
                , ignoredAxes: AxisMask.Y | AxisMask.Z | AxisMask.W
            ),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.5d);
        Assert.That(_group.spacing, Is.EqualTo(25f).Within(TOLERANCE));
        Assert.AreEqual(10, _group.padding.left);

        _group.padding.top = 99;
        StopMixer();

        Assert.That(_group.spacing, Is.EqualTo(5f).Within(TOLERANCE));
        Assert.AreEqual(1, _group.padding.left);
        Assert.AreEqual(2, _group.padding.right);
        Assert.AreEqual(99, _group.padding.top, "top was never written");
        Assert.AreEqual(4, _group.padding.bottom);
    }

    [Test]
    public void Keep_LeavesTheLastValues()
    {
        AddClip(0d, 1d, new LayoutGroupBehaviour { Spacing = new TweenChannel<float>(0f, 100f) });

        EvaluateAt(0.5d);
        StopMixer();

        Assert.That(_group.spacing, Is.EqualTo(50f).Within(TOLERANCE));
    }

    [Test]
    public void SpacingAndPadding_MoveTheChildrenAfterTheLayoutRebuilds()
    {
        var rect = (RectTransform)_group.transform;
        rect.sizeDelta = new Vector2(600f, 100f);
        _group.childControlWidth = true;
        _group.childControlHeight = true;
        _group.childForceExpandWidth = false;
        _group.childForceExpandHeight = false;

        CreateItem(rect, "First", 50f);
        var second = CreateItem(rect, "Second", 50f);

        var behaviour = new LayoutGroupBehaviour
        {
            Spacing = new TweenChannel<float>(0f, 40f),
            Padding = new TweenChannel<Vector4>(Vector4.zero, new Vector4(30f, 0f, 0f, 0f)),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0d);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        var before = second.anchoredPosition.x;

        EvaluateAt(1d);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        var after = second.anchoredPosition.x;

        Assert.That(after - before, Is.EqualTo(70f).Within(TOLERANCE), "40 more spacing and 30 more left padding");
    }

    [Test]
    public void GatherProperties_RegistersSpacingAndEachPaddingSide()
    {
        AddClip(
              0d
            , 1d
            , new LayoutGroupBehaviour {
                Padding = new TweenChannel<Vector4>(Vector4.zero, Vector4.one),
            }
        );
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(
              collector.Names
            , Is.EquivalentTo(
                new[] { "m_Spacing", "m_Padding.m_Left", "m_Padding.m_Right", "m_Padding.m_Top", "m_Padding.m_Bottom" }
            )
        );
    }

    [Test]
    public void GatherProperties_RegistersNothingWhenEveryChannelIsOff()
    {
        AddClip(
              0d
            , 1d
            , new LayoutGroupBehaviour { Spacing = new TweenChannel<float>(0f, 1f, enabled: false) }
        );
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(collector.Names, Is.Empty);
    }

    [Test]
    public void Evaluate_DoesNotAllocate()
    {
        var behaviour = new LayoutGroupBehaviour
        {
            Spacing = new TweenChannel<float>(0f, 40f),
            Padding = new TweenChannel<Vector4>(Vector4.zero, new Vector4(10f, 10f, 10f, 10f)),
        };
        AddClip(0d, 1d, behaviour);
        AddClip(1d, 1d, new LayoutGroupBehaviour { Spacing = new TweenChannel<float>(40f, 0f) });

        var allocated = TrackTestSupport.MeasureEvaluateAllocation(_director);

        Assert.That(allocated, Is.Zero);
    }

    private static RectTransform CreateItem(RectTransform parent, string name, float width)
    {
        var item = new GameObject(name, typeof(RectTransform), typeof(LayoutElement));
        item.transform.SetParent(parent, false);

        var element = item.GetComponent<LayoutElement>();
        element.preferredWidth = width;
        element.preferredHeight = 50f;
        return (RectTransform)item.transform;
    }

    private TimelineClip AddClip(double start, double duration, LayoutGroupBehaviour behaviour)
    {
        var clip = _track.CreateClip<LayoutGroupClip>();
        clip.start = start;
        clip.duration = duration;

        var template = ((LayoutGroupClip)clip.asset).Template;
        template.Spacing = behaviour.Spacing;
        template.Padding = behaviour.Padding;
        return clip;
    }

    private void StopMixer()
    {
        var mixer = (ScriptPlayable<LayoutGroupMixer>)_director.playableGraph.GetRootPlayable(0).GetInput(0);
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
