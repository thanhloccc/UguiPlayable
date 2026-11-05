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

public sealed class LayoutElementTrackTests
{
    private const float TOLERANCE = 1e-3f;

    private readonly List<Object> _created = new();
    private PlayableDirector _director;
    private TimelineAsset _timeline;
    private LayoutElementTrack _track;
    private LayoutElement _element;

    [SetUp]
    public void SetUp()
    {
        var directorObject = new GameObject("Director");
        var elementObject = new GameObject("Element", typeof(RectTransform));
        _created.Add(directorObject);
        _created.Add(elementObject);

        _element = elementObject.AddComponent<LayoutElement>();
        _director = directorObject.AddComponent<PlayableDirector>();
        _director.playOnAwake = false;

        _timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        _created.Add(_timeline);
        _track = _timeline.CreateTrack<LayoutElementTrack>();
        _director.playableAsset = _timeline;
        _director.SetGenericBinding(_track, _element);
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
    public void EveryChannelIsOffByDefault()
    {
        var behaviour = new LayoutElementBehaviour();

        Assert.IsFalse(behaviour.MinWidth.Enabled);
        Assert.IsFalse(behaviour.MinHeight.Enabled);
        Assert.IsFalse(behaviour.PreferredWidth.Enabled);
        Assert.IsFalse(behaviour.PreferredHeight.Enabled);
        Assert.IsFalse(behaviour.FlexibleWidth.Enabled);
        Assert.IsFalse(behaviour.FlexibleHeight.Enabled);
    }

    [Test]
    public void EachChannel_WritesItsOwnSize()
    {
        var behaviour = new LayoutElementBehaviour
        {
            MinWidth = new TweenChannel<float>(0f, 10f),
            MinHeight = new TweenChannel<float>(0f, 20f),
            PreferredWidth = new TweenChannel<float>(0f, 30f),
            PreferredHeight = new TweenChannel<float>(0f, 40f),
            FlexibleWidth = new TweenChannel<float>(0f, 1f),
            FlexibleHeight = new TweenChannel<float>(0f, 2f),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(1d);

        Assert.That(_element.minWidth, Is.EqualTo(10f).Within(TOLERANCE));
        Assert.That(_element.minHeight, Is.EqualTo(20f).Within(TOLERANCE));
        Assert.That(_element.preferredWidth, Is.EqualTo(30f).Within(TOLERANCE));
        Assert.That(_element.preferredHeight, Is.EqualTo(40f).Within(TOLERANCE));
        Assert.That(_element.flexibleWidth, Is.EqualTo(1f).Within(TOLERANCE));
        Assert.That(_element.flexibleHeight, Is.EqualTo(2f).Within(TOLERANCE));
    }

    [Test]
    public void DisabledChannels_LeaveTheElementAlone()
    {
        _element.minWidth = 12f;
        _element.preferredHeight = 34f;

        AddClip(0d, 1d, new LayoutElementBehaviour { PreferredWidth = new TweenChannel<float>(0f, 100f) });

        EvaluateAt(0.5d);

        Assert.That(_element.preferredWidth, Is.EqualTo(50f).Within(TOLERANCE));
        Assert.That(_element.minWidth, Is.EqualTo(12f).Within(TOLERANCE));
        Assert.That(_element.preferredHeight, Is.EqualTo(34f).Within(TOLERANCE));
        Assert.That(_element.flexibleWidth, Is.EqualTo(-1f), "never set, so still not set");
    }

    [Test]
    public void Value_AppliesEase_AndHoldsEnd()
    {
        AddClip(
              0d
            , 1d
            , new LayoutElementBehaviour {
                PreferredWidth = new TweenChannel<float>(0f, 100f, new EaseSetting(Ease.OutQuad)),
            }
        );

        EvaluateAt(0.5d);
        Assert.That(_element.preferredWidth, Is.EqualTo(75f).Within(TOLERANCE));

        EvaluateAt(1.5d);
        Assert.That(_element.preferredWidth, Is.EqualTo(100f).Within(TOLERANCE), "hold after the clip ends");
    }

    [Test]
    public void Relative_OffsetsTheBase()
    {
        _element.preferredWidth = 40f;

        AddClip(
              0d
            , 1d
            , new LayoutElementBehaviour {
                PreferredWidth = new TweenChannel<float>(0f, 60f, ValueMode.Relative),
            }
        );

        EvaluateAt(1d);

        Assert.That(_element.preferredWidth, Is.EqualTo(100f).Within(TOLERANCE));
    }

    [Test]
    public void FromCurrent_StartsAtTheSizeWhenTheClipBecomesActive()
    {
        _element.preferredWidth = 20f;

        AddClip(
              0d
            , 1d
            , new LayoutElementBehaviour {
                PreferredWidth = new TweenChannel<float>(0f, 100f, ValueMode.FromCurrent),
            }
        );

        EvaluateAt(0.5d);

        Assert.That(_element.preferredWidth, Is.EqualTo(60f).Within(TOLERANCE));
    }

    [Test]
    public void Revert_RestoresOnlyTheSizesThatWereWritten()
    {
        _element.preferredWidth = 20f;
        _element.minHeight = 5f;
        _track.RestoreMode = RestoreMode.Revert;

        AddClip(0d, 1d, new LayoutElementBehaviour { PreferredWidth = new TweenChannel<float>(0f, 100f) });

        EvaluateAt(0.5d);
        Assert.That(_element.preferredWidth, Is.EqualTo(50f).Within(TOLERANCE));

        _element.minHeight = 99f;
        StopMixer();

        Assert.That(_element.preferredWidth, Is.EqualTo(20f).Within(TOLERANCE));
        Assert.That(_element.minHeight, Is.EqualTo(99f).Within(TOLERANCE), "minHeight was never written");
    }

    [Test]
    public void Keep_LeavesTheLastSize()
    {
        AddClip(0d, 1d, new LayoutElementBehaviour { PreferredWidth = new TweenChannel<float>(0f, 100f) });

        EvaluateAt(0.5d);
        StopMixer();

        Assert.That(_element.preferredWidth, Is.EqualTo(50f).Within(TOLERANCE));
    }

    [Test]
    public void ANegativeEnd_TurnsTheSizeOff()
    {
        AddClip(0d, 1d, new LayoutElementBehaviour { PreferredWidth = new TweenChannel<float>(100f, -1f) });

        EvaluateAt(1d);

        Assert.That(_element.preferredWidth, Is.EqualTo(-1f), "-1 is not set for the layout group");
    }

    [Test]
    public void AGrowingElement_MovesItsSiblingsAfterTheLayoutRebuilds()
    {
        var parentObject = new GameObject("Row", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        _created.Add(parentObject);

        var parent = (RectTransform)parentObject.transform;
        parent.sizeDelta = new Vector2(600f, 100f);

        var group = parentObject.GetComponent<HorizontalLayoutGroup>();
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = false;

        var first = CreateItem(parent, "First", 50f);
        var second = CreateItem(parent, "Second", 50f);

        _director.SetGenericBinding(_track, first.GetComponent<LayoutElement>());
        AddClip(0d, 1d, new LayoutElementBehaviour { PreferredWidth = new TweenChannel<float>(50f, 150f) });

        EvaluateAt(0d);
        LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        var before = second.anchoredPosition.x;

        EvaluateAt(1d);
        LayoutRebuilder.ForceRebuildLayoutImmediate(parent);
        var after = second.anchoredPosition.x;

        Assert.That(first.rect.width, Is.EqualTo(150f).Within(TOLERANCE));
        Assert.That(after - before, Is.EqualTo(100f).Within(TOLERANCE), "the sibling made room");
    }

    [Test]
    public void GatherProperties_RegistersTheEnabledSizes()
    {
        AddClip(
              0d
            , 1d
            , new LayoutElementBehaviour {
                MinWidth = new TweenChannel<float>(0f, 1f),
                FlexibleHeight = new TweenChannel<float>(0f, 1f),
            }
        );
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(collector.Names, Is.EquivalentTo(new[] { "m_MinWidth", "m_FlexibleHeight" }));
    }

    [Test]
    public void GatherProperties_RegistersNothingWhenEveryChannelIsOff()
    {
        AddClip(0d, 1d, new LayoutElementBehaviour());
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(collector.Names, Is.Empty);
    }

    [Test]
    public void Evaluate_DoesNotAllocate()
    {
        AddClip(
              0d
            , 1d
            , new LayoutElementBehaviour {
                MinWidth = new TweenChannel<float>(0f, 10f),
                PreferredWidth = new TweenChannel<float>(0f, 100f),
                FlexibleHeight = new TweenChannel<float>(0f, 1f),
            }
        );
        AddClip(1d, 1d, new LayoutElementBehaviour { PreferredHeight = new TweenChannel<float>(0f, 100f) });

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

    private TimelineClip AddClip(double start, double duration, LayoutElementBehaviour behaviour)
    {
        var clip = _track.CreateClip<LayoutElementClip>();
        clip.start = start;
        clip.duration = duration;

        var template = ((LayoutElementClip)clip.asset).Template;
        template.MinWidth = behaviour.MinWidth;
        template.MinHeight = behaviour.MinHeight;
        template.PreferredWidth = behaviour.PreferredWidth;
        template.PreferredHeight = behaviour.PreferredHeight;
        template.FlexibleWidth = behaviour.FlexibleWidth;
        template.FlexibleHeight = behaviour.FlexibleHeight;
        return clip;
    }

    private void StopMixer()
    {
        var mixer = (ScriptPlayable<LayoutElementMixer>)_director.playableGraph.GetRootPlayable(0).GetInput(0);
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
