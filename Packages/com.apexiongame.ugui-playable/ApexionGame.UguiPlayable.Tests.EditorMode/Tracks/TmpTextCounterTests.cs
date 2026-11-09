using System;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Tracks.Text;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Tracks;

public sealed class TmpTextCounterTests
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
    public void Counter_IsOffByDefault()
    {
        var behaviour = new TmpTextBehaviour();

        Assert.IsFalse(behaviour.Counter.Enabled);
        Assert.AreEqual(0, behaviour.Counter.Decimals);
        Assert.IsFalse(behaviour.Counter.Grouping);
    }

    [Test]
    public void Counter_WritesTheFormattedValueMidClip()
    {
        AddClip(0d, 1d, new CounterChannel(0d, 1000d, 1, true, suffix: " pts"));

        EvaluateAt(0.5d);

        Assert.AreEqual("500.0 pts", _text.text);
    }

    [Test]
    public void Counter_WritesNothingBeforeTheFirstClip()
    {
        AddClip(1d, 1d, new CounterChannel(0d, 1000d));

        EvaluateAt(0.5d);

        Assert.AreEqual(CONTENT, _text.text);
    }

    [Test]
    public void Counter_HoldsTheEndValue()
    {
        AddClip(0d, 1d, new CounterChannel(0d, 1500d, 0, true));

        EvaluateAt(0.5d);
        EvaluateAt(2d);

        Assert.AreEqual("1,500", _text.text);
    }

    [Test]
    public void Counter_BlendsTwoOverlappingClips()
    {
        AddClip(0d, 2d, new CounterChannel(0d, 100d, 2, false));
        AddClip(1d, 2d, new CounterChannel(200d, 200d, 2, false));

        // The overlap 1 to 2 mixes linearly: at 1.5 each weighs 0.5, the first is at 75 and the second at 200.
        EvaluateAt(1.5d);

        Assert.AreEqual(137.5d, double.Parse(_text.text, System.Globalization.CultureInfo.InvariantCulture), 0.5d);
    }

    [Test]
    public void Counter_StringChannelWins_InOneClip()
    {
        var clip = AddClip(0d, 1d, new CounterChannel(0d, 1000d));
        var template = ((TmpTextClip)clip.asset).Template;

        template.Text = new TextChannel("aaaa", "bbbb");

        EvaluateAt(0.5d);

        Assert.AreEqual("bbaa", _text.text);
    }

    [Test]
    public void Counter_StringChannelWins_AcrossClips()
    {
        AddClip(0d, 2d, new CounterChannel(0d, 1000d));
        AddText(1d, 2d, new TextChannel("aaaa", "bbbb"));

        // The string clip weighs 0.1 here, the counter clip 0.9; the string path still decides.
        EvaluateAt(1.1d);

        // The string clip is at 5 %: no position is revealed yet, so the text is its From.
        Assert.AreEqual("aaaa", _text.text);
    }

    [Test]
    public void Counter_LaterHoldWins_CounterEndsLater()
    {
        AddText(0d, 1d, new TextChannel("aaaa", "bbbb"));
        AddClip(2d, 2d, new CounterChannel(0d, 42d));

        EvaluateAt(5d);

        Assert.AreEqual("42", _text.text);
    }

    [Test]
    public void Counter_LaterHoldWins_StringEndsLater()
    {
        AddClip(0d, 1d, new CounterChannel(0d, 42d));
        AddText(2d, 2d, new TextChannel("aaaa", "bbbb"));

        EvaluateAt(5d);

        Assert.AreEqual("bbbb", _text.text);
    }

    [Test]
    public void Counter_LaterHoldWins_StringOnATie()
    {
        // Both holds end at 3.
        AddClip(0d, 3d, new CounterChannel(0d, 42d));
        AddText(1d, 2d, new TextChannel("aaaa", "bbbb"));

        EvaluateAt(5d);

        Assert.AreEqual("bbbb", _text.text);
    }

    [Test]
    public void Counter_FromCurrent_StartsFromTheLastWrittenValue()
    {
        AddClip(0d, 1d, new CounterChannel(0d, 100d));
        AddClip(1d, 1d, new CounterChannel(0d, 200d, ValueMode.FromCurrent));

        EvaluateAt(0.5d);
        Assert.AreEqual("50", _text.text);

        // Last written 50, so 50 + (200 - 50) * 0.5.
        EvaluateAt(1.5d);

        Assert.AreEqual("125", _text.text);
    }

    [Test]
    public void Counter_FromCurrent_ElseStartsFromFrom()
    {
        AddClip(0d, 1d, new CounterChannel(10d, 110d, ValueMode.FromCurrent));

        EvaluateAt(0.5d);

        Assert.AreEqual("60", _text.text);
    }

    [Test]
    public void Counter_FromCurrent_NeverParsesTheText()
    {
        // The label shows "7" but the mixer has written nothing: the count starts from From, not from 7.
        _text.text = "7";
        AddClip(0d, 1d, new CounterChannel(100d, 200d, ValueMode.FromCurrent));

        EvaluateAt(0.5d);

        Assert.AreEqual("150", _text.text);
    }

    [Test]
    public void Counter_RestoresTheBaseText()
    {
        _track.RestoreMode = RestoreMode.Revert;
        AddClip(0d, 1d, new CounterChannel(0d, 100d));

        EvaluateAt(0.5d);
        Assert.AreEqual("50", _text.text);

        StopMixer();

        Assert.AreEqual(CONTENT, _text.text);
    }

    [Test]
    public void Counter_RegistersTheTextProperty()
    {
        AddClip(0d, 1d, new CounterChannel(0d, 100d));
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(collector.Names, Is.EquivalentTo(new[] { "m_text" }));
    }

    [Test]
    public void Counter_LargeValuesStayExact()
    {
        AddClip(0d, 1d, new CounterChannel(0d, 1234567890123d));

        EvaluateAt(0.5d);
        Assert.AreEqual("617283945062", _text.text);

        EvaluateAt(2d);
        Assert.AreEqual("1234567890123", _text.text);
    }

    [Test]
    public void Counter_TypewriterCountsTheFormattedText()
    {
        var clip = AddClip(0d, 1d, new CounterChannel(0d, 1000d, 0, true));
        var template = ((TmpTextClip)clip.asset).Template;

        template.VisibleRatio = new TweenChannel<float>(0.5f, 0.5f, enabled: true);

        EvaluateAt(2d);

        // "1,000" has five characters; half of them, rounded down, is two.
        Assert.AreEqual(2, _text.maxVisibleCharacters);
    }

    [Test]
    public void Evaluate_DoesNotAllocateWhenTheDigitsDoNotChange()
    {
        AddClip(0d, 1d, new CounterChannel(0d, 1000d, 2, true));

        // The first evaluation writes the text; TMP builds its own Editor-only m_text string then, which is not the
        // package's allocation. Only frames that leave the digits unchanged are measured.
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

    private TimelineClip AddClip(double start, double duration, CounterChannel channel)
    {
        var clip = _track.CreateClip<TmpTextClip>();
        clip.start = start;
        clip.duration = duration;

        var template = ((TmpTextClip)clip.asset).Template;
        template.VisibleRatio = new TweenChannel<float>(0f, 1f, enabled: false);
        template.Counter = channel;
        return clip;
    }

    private TimelineClip AddText(double start, double duration, TextChannel channel)
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
