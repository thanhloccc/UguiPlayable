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

public sealed class GridLayoutGroupTrackTests
{
    private const float TOLERANCE = 1e-3f;

    private readonly List<Object> _created = new();
    private PlayableDirector _director;
    private TimelineAsset _timeline;
    private GridLayoutGroupTrack _track;
    private GridLayoutGroup _grid;

    [SetUp]
    public void SetUp()
    {
        var directorObject = new GameObject("Director");
        var gridObject = new GameObject("Grid", typeof(RectTransform));
        _created.Add(directorObject);
        _created.Add(gridObject);

        _grid = gridObject.AddComponent<GridLayoutGroup>();
        _director = directorObject.AddComponent<PlayableDirector>();
        _director.playOnAwake = false;

        _timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        _created.Add(_timeline);
        _track = _timeline.CreateTrack<GridLayoutGroupTrack>();
        _director.playableAsset = _timeline;
        _director.SetGenericBinding(_track, _grid);
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
    public void Defaults_SpacingOnCellSizeAndPaddingOff()
    {
        var behaviour = new GridLayoutGroupBehaviour();

        Assert.IsTrue(behaviour.Spacing.Enabled);
        Assert.IsFalse(behaviour.CellSize.Enabled);
        Assert.IsFalse(behaviour.Padding.Enabled);
        Assert.AreEqual(new Vector2(120f, 120f), behaviour.CellSize.To);
    }

    [Test]
    public void EachChannel_BlendsAtEndpointsAndMiddle()
    {
        var behaviour = new GridLayoutGroupBehaviour
        {
            Spacing = new TweenChannel<Vector2>(Vector2.zero, new Vector2(10f, 20f)),
            CellSize = new TweenChannel<Vector2>(new Vector2(80f, 80f), new Vector2(120f, 160f)),
            Padding = new TweenChannel<Vector4>(Vector4.zero, new Vector4(10f, 20f, 30f, 40f)),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0d);
        Assert.That(_grid.spacing, Is.EqualTo(Vector2.zero));
        Assert.That(_grid.cellSize, Is.EqualTo(new Vector2(80f, 80f)));
        Assert.AreEqual(0, _grid.padding.left);

        EvaluateAt(0.5d);
        Assert.That(_grid.spacing.x, Is.EqualTo(5f).Within(TOLERANCE));
        Assert.That(_grid.spacing.y, Is.EqualTo(10f).Within(TOLERANCE));
        Assert.That(_grid.cellSize.x, Is.EqualTo(100f).Within(TOLERANCE));
        Assert.That(_grid.cellSize.y, Is.EqualTo(120f).Within(TOLERANCE));
        Assert.AreEqual(5, _grid.padding.left);
        Assert.AreEqual(10, _grid.padding.right);
        Assert.AreEqual(15, _grid.padding.top);
        Assert.AreEqual(20, _grid.padding.bottom);

        EvaluateAt(1d);
        Assert.That(_grid.spacing, Is.EqualTo(new Vector2(10f, 20f)));
        Assert.That(_grid.cellSize, Is.EqualTo(new Vector2(120f, 160f)));
        Assert.AreEqual(40, _grid.padding.bottom);
    }

    [Test]
    public void DisabledChannels_LeaveTheGridAlone()
    {
        _grid.cellSize = new Vector2(33f, 44f);
        _grid.padding = new RectOffset(1, 2, 3, 4);

        AddClip(0d, 1d, new GridLayoutGroupBehaviour { Spacing = new TweenChannel<Vector2>(Vector2.zero, Vector2.one) });

        EvaluateAt(1d);

        Assert.That(_grid.cellSize, Is.EqualTo(new Vector2(33f, 44f)));
        Assert.AreEqual(1, _grid.padding.left);
        Assert.AreEqual(4, _grid.padding.bottom);
    }

    [Test]
    public void CellSize_AppliesEase_AndHoldsEnd()
    {
        var behaviour = new GridLayoutGroupBehaviour
        {
            Spacing = new TweenChannel<Vector2>(Vector2.zero, Vector2.zero, enabled: false),
            CellSize = new TweenChannel<Vector2>(Vector2.zero, new Vector2(100f, 100f), new EaseSetting(Ease.OutQuad)),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.5d);
        Assert.That(_grid.cellSize.x, Is.EqualTo(75f).Within(TOLERANCE));

        EvaluateAt(1.5d);
        Assert.That(_grid.cellSize.x, Is.EqualTo(100f).Within(TOLERANCE), "hold after the clip ends");
    }

    [Test]
    public void Spacing_Relative_OffsetsTheBase()
    {
        _grid.spacing = new Vector2(4f, 6f);
        AddClip(
              0d
            , 1d
            , new GridLayoutGroupBehaviour {
                Spacing = new TweenChannel<Vector2>(Vector2.zero, new Vector2(10f, 10f), ValueMode.Relative),
            }
        );

        EvaluateAt(1d);

        Assert.That(_grid.spacing, Is.EqualTo(new Vector2(14f, 16f)));
    }

    [Test]
    public void CellSize_FromCurrent_StartsAtTheValueWhenTheClipBecomesActive()
    {
        _grid.cellSize = new Vector2(20f, 20f);
        var behaviour = new GridLayoutGroupBehaviour
        {
            Spacing = new TweenChannel<Vector2>(Vector2.zero, Vector2.zero, enabled: false),
            CellSize = new TweenChannel<Vector2>(Vector2.zero, new Vector2(100f, 100f), ValueMode.FromCurrent),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.5d);

        Assert.That(_grid.cellSize.x, Is.EqualTo(60f).Within(TOLERANCE));
    }

    [Test]
    public void Padding_WritesRoundedIntegers_OnTheSameInstance()
    {
        var before = _grid.padding;
        var behaviour = new GridLayoutGroupBehaviour
        {
            Spacing = new TweenChannel<Vector2>(Vector2.zero, Vector2.zero, enabled: false),
            Padding = new TweenChannel<Vector4>(Vector4.zero, new Vector4(10f, 10f, 10f, 10f)),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.37d);

        Assert.AreSame(before, _grid.padding);
        Assert.AreEqual(4, _grid.padding.left, "3.7 rounds to 4");
        Assert.AreEqual(4, _grid.padding.bottom);
    }

    [Test]
    public void Revert_RestoresOnlyTheWrittenAxes()
    {
        _grid.spacing = new Vector2(1f, 2f);
        _grid.cellSize = new Vector2(50f, 60f);
        _grid.padding = new RectOffset(1, 2, 3, 4);
        _track.RestoreMode = RestoreMode.Revert;

        var behaviour = new GridLayoutGroupBehaviour
        {
            Spacing = new TweenChannel<Vector2>(Vector2.zero, new Vector2(20f, 20f), ignoredAxes: AxisMask.Y),
            CellSize = new TweenChannel<Vector2>(
                  new Vector2(100f, 100f)
                , new Vector2(200f, 200f)
                , ignoredAxes: AxisMask.X
            ),
            Padding = new TweenChannel<Vector4>(
                  Vector4.zero
                , new Vector4(20f, 20f, 20f, 20f)
                , ignoredAxes: AxisMask.Y | AxisMask.Z | AxisMask.W
            ),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.5d);
        Assert.That(_grid.spacing, Is.EqualTo(new Vector2(10f, 2f)));
        Assert.That(_grid.cellSize, Is.EqualTo(new Vector2(50f, 150f)));
        Assert.AreEqual(10, _grid.padding.left);

        _grid.spacing = new Vector2(10f, 77f);
        _grid.padding.top = 99;
        StopMixer();

        Assert.That(_grid.spacing, Is.EqualTo(new Vector2(1f, 77f)), "spacing y was never written");
        Assert.That(_grid.cellSize, Is.EqualTo(new Vector2(50f, 60f)));
        Assert.AreEqual(1, _grid.padding.left);
        Assert.AreEqual(99, _grid.padding.top, "top was never written");
    }

    [Test]
    public void Keep_LeavesTheLastValues()
    {
        AddClip(
              0d
            , 1d
            , new GridLayoutGroupBehaviour { Spacing = new TweenChannel<Vector2>(Vector2.zero, new Vector2(10f, 10f)) }
        );

        EvaluateAt(0.5d);
        StopMixer();

        Assert.That(_grid.spacing, Is.EqualTo(new Vector2(5f, 5f)));
    }

    [Test]
    public void CellSizeAndPadding_MoveTheChildrenAfterTheLayoutRebuilds()
    {
        var rect = (RectTransform)_grid.transform;
        rect.sizeDelta = new Vector2(600f, 600f);
        _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        _grid.constraintCount = 2;
        _grid.cellSize = new Vector2(100f, 100f);
        _grid.spacing = Vector2.zero;

        CreateItem(rect, "A");
        var second = CreateItem(rect, "B");

        var behaviour = new GridLayoutGroupBehaviour
        {
            Spacing = new TweenChannel<Vector2>(Vector2.zero, Vector2.zero, enabled: false),
            CellSize = new TweenChannel<Vector2>(new Vector2(100f, 100f), new Vector2(150f, 150f)),
            Padding = new TweenChannel<Vector4>(Vector4.zero, new Vector4(20f, 0f, 0f, 0f)),
        };
        AddClip(0d, 1d, behaviour);

        EvaluateAt(0d);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        var before = second.anchoredPosition.x;

        EvaluateAt(1d);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
        var after = second.anchoredPosition.x;

        Assert.That(after - before, Is.EqualTo(95f).Within(TOLERANCE), "20 more left padding and the second cell centre 1.5 cells (+50 each) further");
    }

    [Test]
    public void GatherProperties_RegistersTheEnabledChannels()
    {
        AddClip(
              0d
            , 1d
            , new GridLayoutGroupBehaviour {
                CellSize = new TweenChannel<Vector2>(Vector2.zero, Vector2.one),
                Padding = new TweenChannel<Vector4>(Vector4.zero, Vector4.one),
            }
        );
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(
              collector.Names
            , Is.EquivalentTo(
                new[] {
                    "m_Spacing", "m_CellSize",
                    "m_Padding.m_Left", "m_Padding.m_Right", "m_Padding.m_Top", "m_Padding.m_Bottom",
                }
            )
        );
    }

    [Test]
    public void GatherProperties_RegistersNothingWhenEveryChannelIsOff()
    {
        AddClip(
              0d
            , 1d
            , new GridLayoutGroupBehaviour {
                Spacing = new TweenChannel<Vector2>(Vector2.zero, Vector2.one, enabled: false),
            }
        );
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(collector.Names, Is.Empty);
    }

    [Test]
    public void Evaluate_DoesNotAllocate()
    {
        var behaviour = new GridLayoutGroupBehaviour
        {
            Spacing = new TweenChannel<Vector2>(Vector2.zero, new Vector2(10f, 10f)),
            CellSize = new TweenChannel<Vector2>(new Vector2(80f, 80f), new Vector2(120f, 120f)),
            Padding = new TweenChannel<Vector4>(Vector4.zero, new Vector4(10f, 10f, 10f, 10f)),
        };
        AddClip(0d, 1d, behaviour);
        AddClip(1d, 1d, new GridLayoutGroupBehaviour { Spacing = new TweenChannel<Vector2>(Vector2.one, Vector2.zero) });

        var allocated = TrackTestSupport.MeasureEvaluateAllocation(_director);

        Assert.That(allocated, Is.Zero);
    }

    private static RectTransform CreateItem(RectTransform parent, string name)
    {
        var item = new GameObject(name, typeof(RectTransform));
        item.transform.SetParent(parent, false);
        return (RectTransform)item.transform;
    }

    private TimelineClip AddClip(double start, double duration, GridLayoutGroupBehaviour behaviour)
    {
        var clip = _track.CreateClip<GridLayoutGroupClip>();
        clip.start = start;
        clip.duration = duration;

        var template = ((GridLayoutGroupClip)clip.asset).Template;
        template.Spacing = behaviour.Spacing;
        template.CellSize = behaviour.CellSize;
        template.Padding = behaviour.Padding;
        return clip;
    }

    private void StopMixer()
    {
        var mixer = (ScriptPlayable<GridLayoutGroupMixer>)_director.playableGraph.GetRootPlayable(0).GetInput(0);
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
