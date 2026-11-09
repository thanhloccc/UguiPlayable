using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Tracks.Text;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Tracks;

public sealed class TmpTextStringTweenTests
{
    private const string CONTENT = "Hello World";

    private readonly List<Object> _created = new();
    private PlayableDirector _director;
    private TimelineAsset _timeline;
    private TmpTextTrack _track;
    private TextMeshProUGUI _text;

    [SetUp]
    public void SetUp()
    {
        var directorObject = new GameObject("Director");
        var canvasObject = new GameObject("Canvas", typeof(Canvas));
        var textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        textObject.transform.SetParent(canvasObject.transform, false);
        _created.Add(directorObject);
        _created.Add(canvasObject);

        _text = textObject.AddComponent<TextMeshProUGUI>();
        _text.text = CONTENT;
        _text.ForceMeshUpdate();

        _director = directorObject.AddComponent<PlayableDirector>();
        _director.playOnAwake = false;

        _timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        _created.Add(_timeline);
        _track = _timeline.CreateTrack<TmpTextTrack>();
        _director.playableAsset = _timeline;
        _director.SetGenericBinding(_track, _text);
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
    public void TheChannelIsOffByDefault()
    {
        var behaviour = new TmpTextBehaviour();

        Assert.IsFalse(behaviour.Text.Enabled);
        Assert.AreEqual(ScrambleMode.None, behaviour.Text.Scramble);
    }

    [Test]
    public void WritesTheExpectedTextMidClip()
    {
        AddClip(0d, 1d, new TextChannel("aaaa", "bbbb"));

        // length 4, count = (int)(4 * 0.5) = 2.
        EvaluateAt(0.5d);

        Assert.AreEqual("bbaa", _text.text);
    }

    [Test]
    public void AtTheClipStartTheTextIsFromUnchanged()
    {
        AddClip(0d, 1d, new TextChannel("ab", "wxyz", scramble: ScrambleMode.All));

        EvaluateAt(0d);

        Assert.AreEqual("ab", _text.text);
    }

    [Test]
    public void WritesNothingBeforeTheFirstClip()
    {
        AddClip(1d, 1d, new TextChannel("aaaa", "bbbb"));

        EvaluateAt(0.5d);

        Assert.AreEqual(CONTENT, _text.text);
    }

    [Test]
    public void HoldsTheEndTextAfterTheClip()
    {
        AddClip(0d, 1d, new TextChannel("aaaa", "bbbb"));

        EvaluateAt(0.5d);
        EvaluateAt(2d);

        Assert.AreEqual("bbbb", _text.text);
    }

    [Test]
    public void ScrubbingBackAndForthGivesTheSameText()
    {
        AddClip(0d, 1d, new TextChannel("LEVEL 1", "LEVEL 2", scramble: ScrambleMode.All));

        EvaluateAt(0.4d);
        var first = _text.text;
        EvaluateAt(0.9d);
        EvaluateAt(0.4d);

        Assert.AreEqual(first, _text.text);
    }

    [Test]
    public void FromCurrent_StartsFromTheBaseText()
    {
        AddClip(0d, 1d, new TextChannel(string.Empty, "ZZZZ", ValueMode.FromCurrent));

        // length 11, count 5: "ZZZZ", then spaces past "ZZZZ", then the rest of the base text.
        EvaluateAt(0.5d);

        Assert.AreEqual("ZZZZ  World", _text.text);
    }

    [Test]
    public void FromCurrent_StartsFromThePreviousClipsTo()
    {
        AddClip(0d, 1d, new TextChannel("aaaa", "bbbb"));
        AddClip(1d, 1d, new TextChannel(string.Empty, "cccc", ValueMode.FromCurrent));

        EvaluateAt(0.5d);
        Assert.AreEqual("bbaa", _text.text);

        // The second clip starts from the first one's To: "bbbb", count 2.
        EvaluateAt(1.5d);
        Assert.AreEqual("ccbb", _text.text);
    }

    [Test]
    public void FromCurrent_JumpingGivesTheSameTextAsPlaying()
    {
        AddClip(0d, 1d, new TextChannel("aaaa", "bbbb"));
        AddClip(1d, 1d, new TextChannel(string.Empty, "cccc", ValueMode.FromCurrent));

        EvaluateAt(0.5d);
        EvaluateAt(1.5d);
        var played = _text.text;

        EvaluateAt(0.1d);
        EvaluateAt(1.5d);
        var jumpedBack = _text.text;

        Assert.AreEqual("ccbb", played);
        Assert.AreEqual(played, jumpedBack);
    }

    [Test]
    public void FromCurrent_WithoutAPreviousClipStartsFromTheBaseText()
    {
        AddClip(1d, 1d, new TextChannel(string.Empty, "ZZZZ", ValueMode.FromCurrent));

        EvaluateAt(1.5d);

        Assert.AreEqual("ZZZZ  World", _text.text);
    }

    [Test]
    public void Revert_RestoresTheBaseText()
    {
        _track.RestoreMode = RestoreMode.Revert;
        AddClip(0d, 1d, new TextChannel("aaaa", "bbbb"));

        EvaluateAt(0.5d);
        Assert.AreEqual("bbaa", _text.text);

        StopMixer();

        Assert.AreEqual(CONTENT, _text.text);
    }

    [Test]
    public void Keep_LeavesTheLastTextInPlayMode()
    {
        Assert.AreEqual(RestoreMode.Keep, _track.RestoreMode);
        AddClip(0d, 1d, new TextChannel("aaaa", "bbbb"));

        EvaluateAt(2d);

        Assert.AreEqual("bbbb", _text.text);
    }

    [Test]
    public void EditMode_RestoresTheTextWhenTimelineStopsThePreview_EvenOnKeep()
    {
        Assert.AreEqual(RestoreMode.Keep, _track.RestoreMode);
        AddClip(0d, 1d, new TextChannel("aaaa", "bbbb"));

        EvaluateAt(0.5d);
        Assert.AreEqual("bbaa", _text.text);

        // What Timeline's window does when the preview stops (WindowState.cs): Stop, then flush the deferred destroy.
        _director.Stop();
        typeof(PlayableDirector)
            .GetMethod(
                  "ProcessPendingGraphChanges"
                , BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            )
            .Invoke(_director, null);

        Assert.AreEqual(CONTENT, _text.text);
    }

    [Test]
    public void GatherProperties_RegistersTheTextName()
    {
        AddClip(0d, 1d, new TextChannel("a", "b"));
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(collector.Names, Is.EquivalentTo(new[] { "m_text" }));
    }

    [Test]
    public void Evaluate_DoesNotAllocateWhenTheTextDoesNotChange()
    {
        AddClip(0d, 1d, new TextChannel("LEVEL 1", "LEVEL 2", scramble: ScrambleMode.All));

        // The first evaluation writes the text. When the text changes, TMP builds its own Editor-only m_text string,
        // which allocates and is not the package's: only frames that leave the text unchanged are measured.
        EvaluateAt(0.5d);

        var graph = _director.playableGraph;
        var root = graph.GetRootPlayable(0);

        root.SetTime(0.5d);
        graph.Evaluate();

        var before = GC.GetAllocatedBytesForCurrentThread();

        for (var i = 0; i < 100; i++)
        {
            root.SetTime(0.5d);
            graph.Evaluate();
        }

        Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
    }

    private TimelineClip AddClip(double start, double duration, TextChannel channel)
    {
        var clip = _track.CreateClip<TmpTextClip>();
        clip.start = start;
        clip.duration = duration;

        var template = ((TmpTextClip)clip.asset).Template;
        template.VisibleRatio = new TweenChannel<float>(0f, 1f, enabled: false);
        template.Text = channel;
        return clip;
    }

    private void StopMixer()
    {
        var mixer = (ScriptPlayable<TmpTextMixer>)_director.playableGraph.GetRootPlayable(0).GetInput(0);
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
