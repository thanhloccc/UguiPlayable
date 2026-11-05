using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Easing;
using ApexionGame.UguiPlayable.Tracks.Text;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Tracks;

public sealed class TmpTextTrackTests
{
    private const float TOLERANCE = 1e-3f;
    private const int ALL = 99999;
    private const string CONTENT = "Hello World";
    private const int CHARACTERS = 11;

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
    public void OnlyTheTypewriterChannelIsOnByDefault()
    {
        var behaviour = new TmpTextBehaviour();

        Assert.IsTrue(behaviour.VisibleRatio.Enabled);
        Assert.IsFalse(behaviour.Color.Enabled);
        Assert.IsFalse(behaviour.Alpha.Enabled);
        Assert.IsFalse(behaviour.FontSize.Enabled);
        Assert.IsFalse(behaviour.CharacterSpacing.Enabled);
        Assert.IsFalse(behaviour.WordSpacing.Enabled);
        Assert.IsFalse(behaviour.LineSpacing.Enabled);
        Assert.IsFalse(behaviour.ParagraphSpacing.Enabled);
        Assert.IsFalse(behaviour.ColorGradient.Enabled);
    }

    [Test]
    public void TheTextHasTheCharactersTheTestsAssume()
    {
        Assert.AreEqual(CHARACTERS, _text.textInfo.characterCount);
    }

    [Test]
    public void ColorAndAlpha_WriteTheGraphicColor()
    {
        AddClip(
              0d
            , 1d
            , new TmpTextBehaviour {
                VisibleRatio = Off(),
                Color = new TweenChannel<Color>(Color.red, Color.blue),
                Alpha = new TweenChannel<float>(1f, 0.25f),
            }
        );

        EvaluateAt(1d);

        Assert.That(_text.color.r, Is.EqualTo(0f).Within(TOLERANCE));
        Assert.That(_text.color.b, Is.EqualTo(1f).Within(TOLERANCE));
        Assert.That(_text.color.a, Is.EqualTo(0.25f).Within(TOLERANCE), "alpha wins for A");
    }

    [Test]
    public void FloatChannels_WriteTheirOwnProperty()
    {
        AddClip(
              0d
            , 1d
            , new TmpTextBehaviour {
                VisibleRatio = Off(),
                FontSize = new TweenChannel<float>(10f, 20f),
                CharacterSpacing = new TweenChannel<float>(0f, 1f),
                WordSpacing = new TweenChannel<float>(0f, 2f),
                LineSpacing = new TweenChannel<float>(0f, 3f),
                ParagraphSpacing = new TweenChannel<float>(0f, 4f),
            }
        );

        EvaluateAt(0.5d);

        Assert.That(_text.fontSize, Is.EqualTo(15f).Within(TOLERANCE));
        Assert.That(_text.characterSpacing, Is.EqualTo(0.5f).Within(TOLERANCE));
        Assert.That(_text.wordSpacing, Is.EqualTo(1f).Within(TOLERANCE));
        Assert.That(_text.lineSpacing, Is.EqualTo(1.5f).Within(TOLERANCE));
        Assert.That(_text.paragraphSpacing, Is.EqualTo(2f).Within(TOLERANCE));

        EvaluateAt(1d);

        Assert.That(_text.fontSize, Is.EqualTo(20f).Within(TOLERANCE));
        Assert.That(_text.paragraphSpacing, Is.EqualTo(4f).Within(TOLERANCE));
    }

    [Test]
    public void DisabledChannels_LeaveTheTextAlone()
    {
        _text.fontSize = 22f;
        _text.wordSpacing = 7f;

        AddClip(
              0d
            , 1d
            , new TmpTextBehaviour {
                VisibleRatio = Off(),
                CharacterSpacing = new TweenChannel<float>(0f, 10f),
            }
        );

        EvaluateAt(0.5d);

        Assert.That(_text.characterSpacing, Is.EqualTo(5f).Within(TOLERANCE));
        Assert.That(_text.fontSize, Is.EqualTo(22f).Within(TOLERANCE));
        Assert.That(_text.wordSpacing, Is.EqualTo(7f).Within(TOLERANCE));
        Assert.AreEqual(ALL, _text.maxVisibleCharacters, "the typewriter is off");
    }

    [Test]
    public void FontSize_AppliesEase_AndHoldsEnd()
    {
        AddClip(
              0d
            , 1d
            , new TmpTextBehaviour {
                VisibleRatio = Off(),
                FontSize = new TweenChannel<float>(0f, 100f, new EaseSetting(Ease.OutQuad)),
            }
        );

        EvaluateAt(0.5d);
        Assert.That(_text.fontSize, Is.EqualTo(75f).Within(TOLERANCE));

        EvaluateAt(1.5d);
        Assert.That(_text.fontSize, Is.EqualTo(100f).Within(TOLERANCE), "hold after the clip ends");
    }

    [Test]
    public void Relative_OffsetsTheBase()
    {
        _text.fontSize = 40f;

        AddClip(
              0d
            , 1d
            , new TmpTextBehaviour {
                VisibleRatio = Off(),
                FontSize = new TweenChannel<float>(0f, 10f, ValueMode.Relative),
            }
        );

        EvaluateAt(1d);

        Assert.That(_text.fontSize, Is.EqualTo(50f).Within(TOLERANCE));
    }

    [Test]
    public void FromCurrent_StartsAtTheValueWhenTheClipBecomesActive()
    {
        _text.characterSpacing = 4f;

        AddClip(
              0d
            , 1d
            , new TmpTextBehaviour {
                VisibleRatio = Off(),
                CharacterSpacing = new TweenChannel<float>(0f, 8f, ValueMode.FromCurrent),
            }
        );

        EvaluateAt(0.5d);

        Assert.That(_text.characterSpacing, Is.EqualTo(6f).Within(TOLERANCE));
    }

    [TestCase(0f, 0)]
    [TestCase(0.5f, 6)]
    [TestCase(1f, ALL)]
    public void Typewriter_WritesTheCountOfTheRatio(float ratio, int expected)
    {
        AddClip(
              0d
            , 1d
            , new TmpTextBehaviour { VisibleRatio = new TweenChannel<float>(ratio, ratio) }
        );

        EvaluateAt(0.5d);

        Assert.AreEqual(expected, _text.maxVisibleCharacters);
    }

    [Test]
    public void Typewriter_ReachesTheEndAsAll_AndHoldsIt()
    {
        AddClip(0d, 1d, new TmpTextBehaviour());

        EvaluateAt(0.5d);
        Assert.AreEqual(6, _text.maxVisibleCharacters);

        EvaluateAt(1d);
        Assert.AreEqual(ALL, _text.maxVisibleCharacters, "a ratio of 1 shows everything, even later text");

        EvaluateAt(2d);
        Assert.AreEqual(ALL, _text.maxVisibleCharacters, "hold after the clip ends");

        EvaluateAt(0.1d);
        Assert.AreEqual(1, _text.maxVisibleCharacters, "scrubbing back hides the letters again");
    }

    [Test]
    public void Typewriter_BlendsInRatioSpace_NeverAgainstTheRawAllCount()
    {
        // The base is 99999, the clip eases in over half its length. A raw-count blend would write about 50000.
        var clip = AddClip(0d, 1d, new TmpTextBehaviour());
        clip.easeInDuration = 0.5d;

        for (var time = 0.05d; time < 1d; time += 0.05d)
        {
            EvaluateAt(time);

            Assert.LessOrEqual(_text.maxVisibleCharacters, CHARACTERS, $"count at {time}");
            Assert.GreaterOrEqual(_text.maxVisibleCharacters, 0, $"count at {time}");
        }

        // weight 0.5, progress 0.25: 0.5 * 1 (base ratio) + 0.5 * 0.25 = 0.625 of 11 characters.
        EvaluateAt(0.25d);
        Assert.AreEqual(7, _text.maxVisibleCharacters);
    }

    [Test]
    public void Typewriter_FromCurrent_StartsAtTheRatioTheTextHas()
    {
        _text.maxVisibleCharacters = 4;

        AddClip(
              0d
            , 1d
            , new TmpTextBehaviour { VisibleRatio = new TweenChannel<float>(0f, 1f, ValueMode.FromCurrent) }
        );

        EvaluateAt(0d);

        Assert.AreEqual(4, _text.maxVisibleCharacters, "4 of 11 characters at the start");
    }

    [Test]
    public void Typewriter_RestoresTheBaseCount_OnRevert()
    {
        _track.RestoreMode = RestoreMode.Revert;
        _text.fontSize = 30f;

        AddClip(
              0d
            , 1d
            , new TmpTextBehaviour { FontSize = new TweenChannel<float>(30f, 60f) }
        );

        EvaluateAt(0.5d);
        Assert.AreEqual(6, _text.maxVisibleCharacters);
        Assert.That(_text.fontSize, Is.EqualTo(45f).Within(TOLERANCE));

        StopMixer();

        Assert.AreEqual(ALL, _text.maxVisibleCharacters);
        Assert.That(_text.fontSize, Is.EqualTo(30f).Within(TOLERANCE));
    }

    [Test]
    public void EditMode_RestoresTheVisibleCountWhenTimelineStopsThePreview_EvenOnKeep()
    {
        Assert.AreEqual(RestoreMode.Keep, _track.RestoreMode);

        AddClip(0d, 1d, new TmpTextBehaviour());

        EvaluateAt(0.5d);
        Assert.AreEqual(6, _text.maxVisibleCharacters);

        // What Timeline's window does when the preview stops (WindowState.cs): Stop, then flush the deferred destroy.
        _director.Stop();
        typeof(PlayableDirector)
            .GetMethod(
                  "ProcessPendingGraphChanges"
                , BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            )
            .Invoke(_director, null);

        Assert.AreEqual(ALL, _text.maxVisibleCharacters);
    }

    [Test]
    public void Keep_LeavesTheLastSpacing()
    {
        AddClip(
              0d
            , 1d
            , new TmpTextBehaviour {
                VisibleRatio = Off(),
                CharacterSpacing = new TweenChannel<float>(0f, 10f),
            }
        );

        EvaluateAt(0.5d);
        StopMixer();

        Assert.That(_text.characterSpacing, Is.EqualTo(5f).Within(TOLERANCE));
    }

    [Test]
    public void Gradient_BlendsEachCorner()
    {
        AddClip(
              0d
            , 1d
            , new TmpTextBehaviour {
                VisibleRatio = Off(),
                ColorGradient = new TweenChannel<VertexGradient>(
                      new VertexGradient(Color.red, Color.green, Color.blue, Color.black)
                    , new VertexGradient(Color.white)
                ),
            }
        );

        EvaluateAt(0d);
        Assert.AreEqual(Color.red, _text.colorGradient.topLeft);
        Assert.AreEqual(Color.green, _text.colorGradient.topRight);
        Assert.AreEqual(Color.blue, _text.colorGradient.bottomLeft);
        Assert.AreEqual(Color.black, _text.colorGradient.bottomRight);

        EvaluateAt(0.5d);
        AssertColor(new Color(1f, 0.5f, 0.5f, 1f), _text.colorGradient.topLeft);
        AssertColor(new Color(0.5f, 1f, 0.5f, 1f), _text.colorGradient.topRight);
        AssertColor(new Color(0.5f, 0.5f, 1f, 1f), _text.colorGradient.bottomLeft);
        AssertColor(new Color(0.5f, 0.5f, 0.5f, 1f), _text.colorGradient.bottomRight);

        EvaluateAt(1d);
        AssertColor(Color.white, _text.colorGradient.bottomRight);
        Assert.IsFalse(_text.enableVertexGradient, "the track never switches the gradient on");
    }

    [Test]
    public void Gradient_Relative_OffsetsEachCornerAndClampsTheAlpha()
    {
        _text.colorGradient = new VertexGradient(
              new Color(0.2f, 0.2f, 0.2f, 0.8f)
            , new Color(0.4f, 0.4f, 0.4f, 1f)
            , new Color(0.6f, 0.6f, 0.6f, 1f)
            , new Color(0.8f, 0.8f, 0.8f, 1f)
        );

        AddClip(
              0d
            , 1d
            , new TmpTextBehaviour {
                VisibleRatio = Off(),
                ColorGradient = new TweenChannel<VertexGradient>(
                      new VertexGradient(Color.clear)
                    , new VertexGradient(new Color(0.1f, 0.1f, 0.1f, 0.5f))
                    , ValueMode.Relative
                ),
            }
        );

        EvaluateAt(1d);

        AssertColor(new Color(0.3f, 0.3f, 0.3f, 1f), _text.colorGradient.topLeft);
        AssertColor(new Color(0.5f, 0.5f, 0.5f, 1f), _text.colorGradient.topRight);
        AssertColor(new Color(0.9f, 0.9f, 0.9f, 1f), _text.colorGradient.bottomRight);
    }

    [Test]
    public void Gradient_FromCurrent_StartsAtTheGradientTheTextHas()
    {
        _text.colorGradient = new VertexGradient(Color.black);

        AddClip(
              0d
            , 1d
            , new TmpTextBehaviour {
                VisibleRatio = Off(),
                ColorGradient = new TweenChannel<VertexGradient>(
                      new VertexGradient(Color.red)
                    , new VertexGradient(Color.white)
                    , ValueMode.FromCurrent
                ),
            }
        );

        EvaluateAt(0.5d);

        AssertColor(new Color(0.5f, 0.5f, 0.5f, 1f), _text.colorGradient.topLeft);
    }

    [Test]
    public void Gradient_RevertsToTheBaseGradient()
    {
        _track.RestoreMode = RestoreMode.Revert;
        _text.colorGradient = new VertexGradient(Color.black);

        AddClip(
              0d
            , 1d
            , new TmpTextBehaviour {
                VisibleRatio = Off(),
                ColorGradient = new TweenChannel<VertexGradient>(
                      new VertexGradient(Color.white)
                    , new VertexGradient(Color.white)
                ),
            }
        );

        EvaluateAt(0.5d);
        AssertColor(Color.white, _text.colorGradient.topLeft);

        StopMixer();

        AssertColor(Color.black, _text.colorGradient.topLeft);
    }

    [Test]
    public void Revert_RestoresOnlyThePropertiesThatWereWritten()
    {
        _track.RestoreMode = RestoreMode.Revert;
        _text.fontSize = 20f;
        _text.lineSpacing = 5f;

        AddClip(
              0d
            , 1d
            , new TmpTextBehaviour {
                VisibleRatio = Off(),
                FontSize = new TweenChannel<float>(20f, 80f),
                Color = new TweenChannel<Color>(Color.red, Color.red),
            }
        );

        EvaluateAt(0.5d);
        _text.lineSpacing = 9f;
        StopMixer();

        Assert.That(_text.fontSize, Is.EqualTo(20f).Within(TOLERANCE));
        Assert.That(_text.lineSpacing, Is.EqualTo(9f).Within(TOLERANCE), "line spacing was never written");
        Assert.AreEqual(Color.white, _text.color);
    }

    [Test]
    public void GatherProperties_RegistersTheSerializedNamesOfTheEnabledChannels()
    {
        AddClip(
              0d
            , 1d
            , new TmpTextBehaviour {
                Alpha = new TweenChannel<float>(0f, 1f),
                FontSize = new TweenChannel<float>(0f, 1f),
                WordSpacing = new TweenChannel<float>(0f, 1f),
                ColorGradient = new TweenChannel<VertexGradient>(new VertexGradient(Color.white), new VertexGradient(Color.white)),
            }
        );
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(
              collector.Names
            , Is.EquivalentTo(
                new[] {
                    "m_fontColor", "m_fontColor32", "m_fontSize", "m_fontSizeBase", "m_wordSpacing",
                    "m_fontColorGradient",
                }
            )
        );
    }

    [Test]
    public void GatherProperties_RegistersNothingForTheTypewriterAlone()
    {
        AddClip(0d, 1d, new TmpTextBehaviour());
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(collector.Names, Is.Empty, "maxVisibleCharacters is not serialized");
    }

    [Test]
    public void GatherProperties_RegistersTheSpacingNames()
    {
        AddClip(
              0d
            , 1d
            , new TmpTextBehaviour {
                VisibleRatio = Off(),
                CharacterSpacing = new TweenChannel<float>(0f, 1f),
                LineSpacing = new TweenChannel<float>(0f, 1f),
                ParagraphSpacing = new TweenChannel<float>(0f, 1f),
            }
        );
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(
              collector.Names
            , Is.EquivalentTo(new[] { "m_characterSpacing", "m_lineSpacing", "m_paragraphSpacing" })
        );
    }

    [Test]
    public void Evaluate_DoesNotAllocate()
    {
        AddClip(
              0d
            , 1d
            , new TmpTextBehaviour {
                Color = new TweenChannel<Color>(Color.red, Color.blue),
                Alpha = new TweenChannel<float>(0f, 1f),
                FontSize = new TweenChannel<float>(20f, 40f),
                CharacterSpacing = new TweenChannel<float>(0f, 10f),
                ColorGradient = new TweenChannel<VertexGradient>(
                      new VertexGradient(Color.red)
                    , new VertexGradient(Color.blue)
                ),
            }
        );
        AddClip(1d, 1d, new TmpTextBehaviour { LineSpacing = new TweenChannel<float>(0f, 5f) });

        var allocated = TrackTestSupport.MeasureEvaluateAllocation(_director);

        Assert.That(allocated, Is.Zero);
    }

    private static TweenChannel<float> Off()
        => new(0f, 1f, enabled: false);

    private static void AssertColor(Color expected, Color actual)
    {
        Assert.That(actual.r, Is.EqualTo(expected.r).Within(TOLERANCE), "r");
        Assert.That(actual.g, Is.EqualTo(expected.g).Within(TOLERANCE), "g");
        Assert.That(actual.b, Is.EqualTo(expected.b).Within(TOLERANCE), "b");
        Assert.That(actual.a, Is.EqualTo(expected.a).Within(TOLERANCE), "a");
    }

    private TimelineClip AddClip(double start, double duration, TmpTextBehaviour behaviour)
    {
        var clip = _track.CreateClip<TmpTextClip>();
        clip.start = start;
        clip.duration = duration;

        var template = ((TmpTextClip)clip.asset).Template;
        template.Color = behaviour.Color;
        template.Alpha = behaviour.Alpha;
        template.FontSize = behaviour.FontSize;
        template.CharacterSpacing = behaviour.CharacterSpacing;
        template.WordSpacing = behaviour.WordSpacing;
        template.LineSpacing = behaviour.LineSpacing;
        template.ParagraphSpacing = behaviour.ParagraphSpacing;
        template.VisibleRatio = behaviour.VisibleRatio;
        template.ColorGradient = behaviour.ColorGradient;
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
