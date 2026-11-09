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
using TmpTextClipEditor = ApexionGame.UguiPlayable.Editor.Tracks.TmpTextClipEditor;
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

    // Probe of TMP itself: the rules the slot scan relies on. A tag TMP does not know is printed as text, while the
    // scan treats it as a tag (no slot); that difference is documented.
    [TestCase("<b>x</b>", true, 1, 1)]
    [TestCase("a<br>b", true, 3, 3)]
    [TestCase("a<BR>b", true, 3, 3)]
    [TestCase("a<nbsp>b", true, 3, 3)]
    [TestCase("a<zwsp>b", true, 3, 3)]
    [TestCase("a<zwj>b", true, 3, 3)]
    [TestCase("a<shy>b", true, 3, 3)]
    [TestCase("a<cr>b", true, 3, 3)]
    [TestCase("<noparse><b></noparse>", true, 3, 3)]
    [TestCase("<NoParse><br></NOPARSE>", true, 4, 4)]
    [TestCase("a\\nb", true, 3, 3)]
    [TestCase("a\\nb", false, 4, 4)]
    [TestCase("a\\\\b", true, 3, 3)]
    [TestCase("\\u0041", false, 1, 1)]
    [TestCase("😀", true, 1, 1)]
    [TestCase("a<>b", true, 4, 4)]
    [TestCase("a < b", true, 5, 5)]
    [TestCase("<foo>", true, 5, 0)]
    public void TmpCountsTheCharactersTheSlotScanExpects(string text, bool parseEscapes, int tmpCount, int slotCount)
    {
        _text.parseCtrlCharacters = parseEscapes;
        _text.text = text;
        _text.ForceMeshUpdate();

        var buffer = new char[TextTweenMath.RequiredLength(string.Empty, text, richText: true)];

        TextTweenMath.Fill(
              buffer: buffer
            , from: string.Empty
            , to: text
            , progress: 1f
            , mode: ScrambleMode.None
            , custom: string.Empty
            , richText: true
            , parseEscapes: parseEscapes
            , slotCount: out var scanned
        );

        Assert.AreEqual(tmpCount, _text.textInfo.characterCount, "TMP");
        Assert.AreEqual(slotCount, scanned, "slot scan");
    }

    // Where text from different places meets, TMP must not see an escape or a tag the slot scan did not count.
    [TestCase("abcnefgh", "C:\\Users", true)]
    [TestCase("abcuefgh", "C:\\Users", false)]
    [TestCase("a\\<b>n", "xyz", true)]
    [TestCase("aab>yz", "x<<<<<", true)]
    [TestCase("wxyz", "<b>a<b>b</b>c</b>d", true)]
    [TestCase("1 2 3 4 5 6 7", "a<pos=50%>b<space=2em>c<page>d", true)]
    public void TmpCountsTheSlotsOfEveryFrameAtTheSeams(string from, string to, bool parseEscapes)
    {
        _text.parseCtrlCharacters = parseEscapes;

        var buffer = new char[TextTweenMath.RequiredLength(from, to, richText: true)];

        foreach (var mode in new[] { ScrambleMode.None, ScrambleMode.All })
        {
            for (var i = 0; i <= 20; i++)
            {
                var length = TextTweenMath.Fill(
                      buffer: buffer
                    , from: from
                    , to: to
                    , progress: i / 20f
                    , mode: mode
                    , custom: string.Empty
                    , richText: true
                    , parseEscapes: parseEscapes
                    , slotCount: out var slotCount
                );

                _text.text = new string(buffer, 0, length);
                _text.ForceMeshUpdate();

                Assert.AreEqual(slotCount, _text.textInfo.characterCount, $"'{_text.text}' ({mode}, step {i})");
            }
        }
    }

    [Test]
    public void RichText_CharacterCountNeverCountsTagCharactersMidClip()
    {
        AddClip(0d, 1d, new TextChannel(string.Empty, "<b>Hello</b> <color=#F00>World</color>"));

        for (var i = 0; i <= 20; i++)
        {
            EvaluateAt(i / 20d);
            _text.ForceMeshUpdate();

            var info = _text.textInfo;
            var count = (int)(i / 20f * 11 + 1e-5f);

            // From is empty: nothing at count 0, then 11 characters (the revealed part and spaces).
            Assert.AreEqual(count == 0 ? 0 : 11, info.characterCount, $"step {i}");

            for (var c = 0; c < info.characterCount; c++)
            {
                var character = info.characterInfo[c].character;

                Assert.That(character != '<' && character != '>', $"'{_text.text}' shows a tag at step {i}");
            }
        }
    }

    [Test]
    public void Typewriter_UsesTheSlotCountOfRichTextWrittenThisFrame()
    {
        var clip = AddClip(0d, 1d, new TextChannel("xxxx", "<b>yyyy</b>"));

        ((TmpTextClip)clip.asset).Template.VisibleRatio = new TweenChannel<float>(0.5f, 0.5f);

        EvaluateAt(1d);

        // The new text shows 4 characters (11 with its tags; TMP's text info still counts the 11 of the old one).
        Assert.AreEqual("<b>yyyy</b>", _text.text);
        Assert.AreEqual(2, _text.maxVisibleCharacters);
    }

    [Test]
    public void ScrambleRate_ChangesGlyphsInsideOneRevealStep()
    {
        AddClip(
              0d
            , 5d
            , new TextChannel("----------", "ABCDEFGHIJ", ScrambleMode.All, scrambleRate: 20f)
        );

        // Reveal count is 1 at both times (0.10 and 0.112 of the clip); the scramble step is 10 then 11.
        EvaluateAt(0.50d);
        var first = _text.text;
        EvaluateAt(0.56d);
        var second = _text.text;

        Assert.AreEqual(first[0], second[0]);
        Assert.AreNotEqual(first, second);
    }

    [Test]
    public void ScrambleRate_Zero_IsThePerStepBehaviour()
    {
        AddClip(0d, 5d, new TextChannel("----------", "ABCDEFGHIJ", ScrambleMode.All));

        EvaluateAt(0.50d);
        var first = _text.text;
        EvaluateAt(0.56d);

        Assert.AreEqual(first, _text.text);
    }

    [Test]
    public void ScrambleRate_ScrubbingIsStable()
    {
        AddClip(
              0d
            , 5d
            , new TextChannel("----------", "ABCDEFGHIJ", ScrambleMode.All, scrambleRate: 20f)
        );

        EvaluateAt(0.56d);
        var first = _text.text;
        EvaluateAt(3.21d);
        EvaluateAt(1.07d);
        EvaluateAt(0.56d);

        Assert.AreEqual(first, _text.text);
    }

    [Test]
    public void NewClip_HasScrambleRateTwenty()
    {
        var clip = _track.CreateClip<TmpTextClip>();
        var editor = new TmpTextClipEditor();

        Assert.AreEqual(0f, ((TmpTextClip)clip.asset).Template.Text.ScrambleRate);

        editor.OnCreate(clip, _track, null);

        Assert.AreEqual(20f, ((TmpTextClip)clip.asset).Template.Text.ScrambleRate);

        var copy = _track.CreateClip<TmpTextClip>();

        editor.OnCreate(copy, _track, clip);

        Assert.AreEqual(0f, ((TmpTextClip)copy.asset).Template.Text.ScrambleRate);
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
