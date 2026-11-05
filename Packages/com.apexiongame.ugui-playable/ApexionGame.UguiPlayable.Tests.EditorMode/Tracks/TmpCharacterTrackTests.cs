using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Easing;
using ApexionGame.UguiPlayable.Editor.Clips;
using ApexionGame.UguiPlayable.Editor.Tracks;
using ApexionGame.UguiPlayable.Tracks.Text;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Tracks;

public sealed class TmpCharacterTrackTests
{
    private const float TOLERANCE = 1e-4f;
    private const string CONTENT = "Hello World";
    private const int CHARACTERS = 11;
    private const int SPACE = 5;

    private readonly List<Object> _created = new();
    private Scene _scene;
    private int _sceneDirtied;
    private PlayableDirector _director;
    private TimelineAsset _timeline;
    private TmpCharacterTrack _track;
    private TextMeshProUGUI _text;
    private Vector3[][] _rest;

    [SetUp]
    public void SetUp()
    {
        _scene = SceneManager.GetActiveScene();
        _sceneDirtied = 0;
        EditorSceneManager.sceneDirtied += OnSceneDirtied;

        var directorObject = new GameObject("Director");
        var canvasObject = new GameObject("Canvas", typeof(Canvas));
        var textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        textObject.transform.SetParent(canvasObject.transform, false);
        _created.Add(directorObject);
        _created.Add(canvasObject);

        _text = textObject.AddComponent<TextMeshProUGUI>();
        _text.rectTransform.sizeDelta = new Vector2(600f, 100f);
        _text.text = CONTENT;
        _text.ForceMeshUpdate();
        _rest = Snapshot();

        _director = directorObject.AddComponent<PlayableDirector>();
        _director.playOnAwake = false;

        // Long enough that "after the clip" is inside the timeline, not the director's own hold at the end.
        _timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        _timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
        _timeline.fixedDuration = 10d;
        _created.Add(_timeline);
        _track = _timeline.CreateTrack<TmpCharacterTrack>();
        _director.playableAsset = _timeline;
        _director.SetGenericBinding(_track, _text);

        TmpCharacterTrackCheck.Clear();
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
        TmpCharacterTrackCheck.Clear();
        EditorSceneManager.sceneDirtied -= OnSceneDirtied;
    }

    [Test]
    public void TheDefaultClipIsAPerCharacterFadeIn()
    {
        var behaviour = new TmpCharacterBehaviour();

        Assert.IsTrue(behaviour.Alpha.Enabled);
        Assert.AreEqual(0f, behaviour.Alpha.From);
        Assert.AreEqual(1f, behaviour.Alpha.To);
        Assert.IsFalse(behaviour.Offset.Enabled);
        Assert.IsFalse(behaviour.Rotation.Enabled);
        Assert.IsFalse(behaviour.Scale.Enabled);
        Assert.IsFalse(behaviour.Color.Enabled);
        Assert.IsFalse(behaviour.Wave.Enabled);
        Assert.IsFalse(behaviour.Shake.Enabled);
        Assert.AreEqual(0.5f, behaviour.Stagger);
        Assert.AreEqual(CharacterOrder.Forward, behaviour.Order);
        Assert.AreEqual(Ease.OutCubic, behaviour.Ease.Ease);
        Assert.AreEqual(CharacterPivot.Center, behaviour.Pivot);
        Assert.IsTrue(behaviour.HasEnabledChannel());
    }

    [Test]
    public void TheTextHasTheCharactersTheTestsAssume()
    {
        Assert.AreEqual(CHARACTERS, _text.textInfo.characterCount);
        Assert.IsFalse(_text.textInfo.characterInfo[SPACE].isVisible);
    }

    [Test]
    public void CollectPropertyNames_AddsNothing()
    {
        AddClip(0d, 1d);
        var collector = new TrackTestSupport.RecordingCollector();

        _track.GatherProperties(_director, collector);

        Assert.That(collector.Names, Is.Empty, "the mesh is never serialized");
    }

    [Test]
    public void FadeIn_HidesTheLastCharacterAtTheStart_AndShowsEveryCharacterAtTheEnd()
    {
        AddClip(0d, 1d);

        EvaluateAt(0d);

        Assert.AreEqual(0, AlphaOf(CHARACTERS - 1), "the last character has not started");

        EvaluateAt(1d);

        for (var i = 0; i < CHARACTERS; i++)
        {
            if (i != SPACE)
            {
                Assert.AreEqual(255, AlphaOf(i), $"character {i}");
            }
        }
    }

    [Test]
    public void FadeIn_StaggersFromLeftToRight()
    {
        AddClip(0d, 1d);

        EvaluateAt(0.4d);

        Assert.That(AlphaOf(0), Is.GreaterThan(AlphaOf(4)));
        Assert.That(AlphaOf(4), Is.GreaterThan(AlphaOf(CHARACTERS - 1)));
    }

    [Test]
    public void Offset_MovesEveryVisibleCharacter()
    {
        AddClip(0d, 1d, OffsetOnly());

        EvaluateAt(0.5d);

        AssertOffset(new Vector2(0f, -15f));
    }

    [Test]
    public void Transitions_HoldTheirEndValueAfterTheClip()
    {
        var behaviour = new TmpCharacterBehaviour {
            Stagger = 0f,
            Ease = new EaseSetting(Ease.Linear),
            Alpha = new CharacterChannel<float>(1f, 0f),
        };

        AddClip(0d, 1d, behaviour);

        EvaluateAt(2d);

        Assert.AreEqual(0, AlphaOf(0), "a fade out stays hidden");
    }

    [Test]
    public void ColorClipOnTheSameText_KeepsTheEffectAfterTheCanvasUpdate()
    {
        var colorTrack = _timeline.CreateTrack<TmpTextTrack>();
        _director.SetGenericBinding(colorTrack, _text);

        var colorClip = colorTrack.CreateClip<TmpTextClip>();
        colorClip.start = 0d;
        colorClip.duration = 1d;
        var template = ((TmpTextClip)colorClip.asset).Template;
        template.VisibleRatio = new TweenChannel<float>(0f, 1f, enabled: false);
        template.Color = new TweenChannel<Color>(Color.red, Color.blue);

        AddClip(0d, 1d, OffsetOnly());

        EvaluateAt(0.25d);
        Canvas.ForceUpdateCanvases();
        EvaluateAt(0.5d);
        Canvas.ForceUpdateCanvases();

        Assert.IsFalse(_text.havePropertiesChanged, "the colour change rebuilt the text");
        Assert.That(_text.color.b, Is.EqualTo(0.5f).Within(1e-3f));
        AssertOffset(new Vector2(0f, -15f));
        AssertUploadedOffset(new Vector2(0f, -15f));
    }

    [Test]
    public void Typewriter_SkipsHiddenCharacters()
    {
        var typewriterTrack = _timeline.CreateTrack<TmpTextTrack>();
        _director.SetGenericBinding(typewriterTrack, _text);

        var typewriterClip = typewriterTrack.CreateClip<TmpTextClip>();
        typewriterClip.start = 0d;
        typewriterClip.duration = 1d;

        AddClip(0d, 1d, OffsetOnly());

        // The typewriter's count lags one frame behind (TmpTextBehaviour), so evaluate twice.
        EvaluateAt(0.5d);
        Canvas.ForceUpdateCanvases();
        EvaluateAt(0.5d);
        Canvas.ForceUpdateCanvases();

        var characters = _text.textInfo.characterInfo;
        var hidden = 0;

        for (var i = 0; i < CHARACTERS; i++)
        {
            if (characters[i].isVisible)
            {
                AssertCharacterOffset(i, new Vector2(0f, -15f));
                continue;
            }

            if (i == SPACE)
            {
                continue;
            }

            hidden++;

            for (var v = 0; v < 4; v++)
            {
                Assert.That(VertexOf(i, v), Is.EqualTo(Vector3.zero), $"hidden character {i} stays collapsed");
            }
        }

        Assert.That(hidden, Is.GreaterThan(0), "the typewriter hides characters");
    }

    [Test]
    public void MixedMaterialText_AnimatesTheSubMeshCharacters()
    {
        _text.text = "AB<sprite=0>CD";
        _text.ForceMeshUpdate();
        _rest = Snapshot();

        var textInfo = _text.textInfo;
        Assert.That(textInfo.materialCount, Is.GreaterThanOrEqualTo(2), "the sprite has its own sub-mesh");

        var sprite = -1;

        for (var i = 0; i < textInfo.characterCount; i++)
        {
            if (textInfo.characterInfo[i].materialReferenceIndex > 0)
            {
                sprite = i;
            }
        }

        Assert.That(sprite, Is.GreaterThanOrEqualTo(0));

        AddClip(0d, 1d, OffsetOnly());

        EvaluateAt(0.5d);

        AssertCharacterOffset(sprite, new Vector2(0f, -15f));
        AssertCharacterOffset(0, new Vector2(0f, -15f));

        var m = textInfo.characterInfo[sprite].materialReferenceIndex;
        var uploaded = textInfo.meshInfo[m].mesh.vertices;
        var vertexIndex = textInfo.characterInfo[sprite].vertexIndex;

        Assert.That(uploaded[vertexIndex].y, Is.EqualTo(_rest[m][vertexIndex].y - 15f).Within(TOLERANCE));
    }

    [Test]
    public void Restore_GivesBackTheRestMesh_AndLeavesTheSceneClean()
    {
        // The test runner's untitled scene can already be dirty, which hides a new change from isDirty and from
        // sceneDirtied; the serialized state of the text is what a preview could dirty, so it is compared as well.
        var wasDirty = _scene.isDirty;
        var serialized = EditorJsonUtility.ToJson(_text);
        _sceneDirtied = 0;

        AddClip(0d, 1d, OffsetOnly());

        EvaluateAt(0.5d);
        AssertOffset(new Vector2(0f, -15f));

        var mixer = GetMixer();
        StopAsThePreviewDoes();
        Canvas.ForceUpdateCanvases();

        Assert.IsFalse(mixer.IsHandlerSubscribed);
        Assert.That(MaxDelta(_rest), Is.LessThan(TOLERANCE));
        Assert.That(MaxUploadedDelta(_rest), Is.LessThan(TOLERANCE));
        Assert.AreEqual(wasDirty, _scene.isDirty, "the scene's dirty state is unchanged");
        Assert.AreEqual(0, _sceneDirtied, "nothing marked the scene dirty");
        Assert.AreEqual(serialized, EditorJsonUtility.ToJson(_text), "nothing serialized changed");

        _text.ForceMeshUpdate();
        Assert.That(MaxDelta(_rest), Is.LessThan(TOLERANCE), "a fresh rebuild gives the same mesh");
    }

    [Test]
    public void Restore_HappensWhateverTheRestoreMode()
    {
        _track.RestoreMode = RestoreMode.Keep;
        AddClip(0d, 1d, OffsetOnly());

        EvaluateAt(0.5d);
        StopAsThePreviewDoes();
        Canvas.ForceUpdateCanvases();

        Assert.That(MaxDelta(_rest), Is.LessThan(TOLERANCE));
    }

    [Test]
    public void SecondBind_RightAfterARestore_CapturesACleanSource()
    {
        AddClip(0d, 1d, OffsetOnly());

        EvaluateAt(0.5d);
        StopAsThePreviewDoes();

        // No canvas update: meshInfo still holds the effect, so a capture from it would double the offset.
        EvaluateAt(0.5d);

        AssertOffset(new Vector2(0f, -15f));
    }

    [Test]
    public void PreviewOffSequence_LeavesTheRestMesh_AndNoSubscription()
    {
        AddClip(0d, 1d, OffsetOnly());

        EvaluateAt(0.5d);
        var first = GetMixer();

        // Timeline's preview off: Stop() and the flush (first restore), then one more evaluate on a new graph.
        StopAsThePreviewDoes();
        EvaluateAt(0.5d);
        var second = GetMixer();
        Canvas.ForceUpdateCanvases();

        // The new graph is destroyed in turn (second restore).
        StopAsThePreviewDoes();
        Canvas.ForceUpdateCanvases();

        Assert.AreNotSame(first, second);
        Assert.IsFalse(first.IsHandlerSubscribed);
        Assert.IsFalse(second.IsHandlerSubscribed);
        Assert.That(MaxDelta(_rest), Is.LessThan(TOLERANCE));
        Assert.That(MaxUploadedDelta(_rest), Is.LessThan(TOLERANCE));
    }

    [Test]
    public void ClipExit_WritesTheSourceBackOnce_ThenStopsWriting()
    {
        var behaviour = new TmpCharacterBehaviour {
            Alpha = new CharacterChannel<float>(0f, 1f, enabled: false),
            Wave = new WaveChannel(OscillationTarget.Position, WaveShape.Sine, new Vector3(0f, 10f, 0f)),
        };

        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.3d);
        var mixer = GetMixer();
        Assert.AreEqual(1, mixer.UploadCount);
        Assert.That(MaxDelta(_rest), Is.GreaterThan(1f), "the wave moved the text");

        EvaluateAt(1.5d);
        Assert.AreEqual(2, mixer.UploadCount, "the source is written back once");
        Assert.That(MaxDelta(_rest), Is.LessThan(TOLERANCE));

        EvaluateAt(1.6d);
        EvaluateAt(1.7d);
        Assert.AreEqual(2, mixer.UploadCount, "then nothing is written");
    }

    [Test]
    public void UnchangedContributions_AreNotUploadedAgain()
    {
        AddClip(0d, 1d, OffsetOnly());

        EvaluateAt(2d);
        var mixer = GetMixer();
        var uploads = mixer.UploadCount;

        EvaluateAt(2.5d);
        EvaluateAt(3d);

        Assert.AreEqual(uploads, mixer.UploadCount, "a held pose is uploaded once");
    }

    [Test]
    public void Handler_IsIdempotent()
    {
        AddClip(0d, 1d, OffsetOnly());

        EvaluateAt(0.5d);

        _text.ForceMeshUpdate();
        var once = Snapshot();
        _text.ForceMeshUpdate();

        Assert.That(MaxDelta(once), Is.LessThan(TOLERANCE));
        AssertOffset(new Vector2(0f, -15f));
    }

    [Test]
    public void Wave_FollowsTheFormula_AndHasNoHold()
    {
        var behaviour = new TmpCharacterBehaviour {
            Alpha = new CharacterChannel<float>(0f, 1f, enabled: false),
            Wave = new WaveChannel(
                  OscillationTarget.Position
                , WaveShape.Sine
                , new Vector3(0f, 10f, 0f)
                , cycles: 1f
                , phasePerCharacter: 0.25f
            ),
        };

        AddClip(0d, 1d, behaviour);

        EvaluateAt(0.25d);

        // Ranks count visible characters only: H0 e1 l2 l3 o4, the space is skipped, W5 o6 r7 l8 d9.
        AssertCharacterOffset(0, new Vector2(0f, 10f));
        AssertCharacterOffset(1, new Vector2(0f, 0f));
        AssertCharacterOffset(2, new Vector2(0f, -10f));
        AssertCharacterOffset(6, new Vector2(0f, 10f * Mathf.Sin(2f * Mathf.PI * (0.25f - 0.25f * 5f))));

        EvaluateAt(1.5d);

        Assert.That(MaxDelta(_rest), Is.LessThan(TOLERANCE), "no hold after the clip");
    }

    [Test]
    public void Shake_IsTheSameForTheSameSeed_DiffersForAnother_AndHasNoHold()
    {
        var clip = AddClip(0d, 1d, ShakeOnly(seed: 1));

        EvaluateAt(0.37d);
        var first = Snapshot();

        var channel = ((TmpCharacterClip)clip.asset).Template.Shake;
        var expected = CharacterEffectMath.Shake(channel, 0.37f, 0);
        AssertCharacterOffset(0, new Vector2(expected.x, expected.y));

        ((TmpCharacterClip)clip.asset).Template.Shake = ShakeOnly(seed: 2).Shake;
        _director.RebuildGraph();
        EvaluateAt(0.37d);
        Assert.That(MaxDelta(first), Is.GreaterThan(1e-2f), "another seed shakes differently");

        ((TmpCharacterClip)clip.asset).Template.Shake = ShakeOnly(seed: 1).Shake;
        _director.RebuildGraph();
        EvaluateAt(0.37d);
        Assert.That(MaxDelta(first), Is.LessThan(TOLERANCE), "the same seed shakes the same way");

        EvaluateAt(1.5d);
        Assert.That(MaxDelta(_rest), Is.LessThan(TOLERANCE), "no hold after the clip");
    }

    [Test]
    public void ReverseSorting_WritesNothing()
    {
        _text.geometrySortingOrder = VertexSortingOrder.Reverse;
        _text.ForceMeshUpdate();
        var reversed = Snapshot();

        AddClip(0d, 1d, OffsetOnly());

        EvaluateAt(0.5d);
        Assert.AreEqual(0, GetMixer().UploadCount);
        Assert.That(MaxDelta(reversed), Is.LessThan(TOLERANCE));

        _text.ForceMeshUpdate();
        Assert.That(MaxDelta(reversed), Is.LessThan(TOLERANCE), "the handler writes nothing either");
    }

    [Test]
    public void Evaluate_DoesNotAllocate()
    {
        AddClip(
              0d
            , 1d
            , new TmpCharacterBehaviour {
                Order = CharacterOrder.Random,
                OrderSeed = 3,
                Offset = new CharacterChannel<Vector2>(new Vector2(0f, -30f), Vector2.zero),
                Rotation = new CharacterChannel<float>(90f, 0f),
                Scale = new CharacterChannel<Vector2>(Vector2.zero, Vector2.one),
                Color = new CharacterChannel<Color>(Color.red, Color.white),
                Wave = new WaveChannel(OscillationTarget.Rotation, WaveShape.Bounce, new Vector3(0f, 0f, 10f)),
                Shake = OscillationChannel.Shake(OscillationTarget.Scale, new Vector3(0.1f, 0.1f, 0f)),
            }
        );
        AddClip(1d, 1d, new TmpCharacterBehaviour { Order = CharacterOrder.CenterOut });

        var allocated = TrackTestSupport.MeasureEvaluateAllocation(_director);

        Assert.That(allocated, Is.Zero);
    }

    [Test]
    public void TrackCheck_WarnsOnTwoCharacterTracksForOneText_AndOnReverseSorting()
    {
        Assert.AreEqual(1, TmpCharacterTrackCheck.CountTracks(_track, _text, _director));

        var second = _timeline.CreateTrack<TmpCharacterTrack>();
        _director.SetGenericBinding(second, _text);

        Assert.AreEqual(2, TmpCharacterTrackCheck.CountTracks(_track, _text, _director));

        _text.geometrySortingOrder = VertexSortingOrder.Reverse;

        Assert.AreEqual(TmpCharacterTrackCheck.REVERSE_SORTING, TmpCharacterTrackCheck.GetWarning(_track, _text));
    }

    [Test]
    public void ClipModel_FindsTheCharacterAndWaveChannels()
    {
        var clip = ScriptableObject.CreateInstance<TmpCharacterClip>();
        _created.Add(clip);

        var model = TweenClipModel.Create(new SerializedObject(clip));
        var shapes = model.Channels.ToDictionary(c => c.Name, c => c.Shape);

        Assert.That(shapes.Keys, Is.EquivalentTo(new[] {
            "_offset", "_rotation", "_scale", "_alpha", "_color", "_wave", "_shake",
        }));
        Assert.AreEqual(ChannelShape.Character, shapes["_offset"]);
        Assert.AreEqual(ChannelShape.Character, shapes["_color"]);
        Assert.AreEqual(ChannelShape.Wave, shapes["_wave"]);
        Assert.AreEqual(ChannelShape.Oscillation, shapes["_shake"]);
        Assert.IsTrue(model.Channels.Single(c => c.Name == "_scale").HasConstrain);
        Assert.AreEqual(1, model.EnabledCount);
    }

    private void OnSceneDirtied(Scene scene)
    {
        _sceneDirtied++;
    }

    private static TmpCharacterBehaviour OffsetOnly()
        => new() {
            Stagger = 0f,
            Ease = new EaseSetting(Ease.Linear),
            Alpha = new CharacterChannel<float>(0f, 1f, enabled: false),
            Offset = new CharacterChannel<Vector2>(new Vector2(0f, -30f), Vector2.zero),
        };

    private static TmpCharacterBehaviour ShakeOnly(int seed)
        => new() {
            Alpha = new CharacterChannel<float>(0f, 1f, enabled: false),
            Shake = OscillationChannel.Shake(
                  OscillationTarget.Position
                , new Vector3(5f, 5f, 0f)
                , frequency: 10f
                , fadeOut: false
                , seed: seed
            ),
        };

    private TimelineClip AddClip(double start, double duration, TmpCharacterBehaviour behaviour = null)
    {
        var clip = _track.CreateClip<TmpCharacterClip>();
        clip.start = start;
        clip.duration = duration;

        if (behaviour == null)
        {
            return clip;
        }

        var template = ((TmpCharacterClip)clip.asset).Template;
        template.RangeStart = behaviour.RangeStart;
        template.RangeLength = behaviour.RangeLength;
        template.Stagger = behaviour.Stagger;
        template.Order = behaviour.Order;
        template.OrderSeed = behaviour.OrderSeed;
        template.Ease = behaviour.Ease;
        template.Pivot = behaviour.Pivot;
        template.Offset = behaviour.Offset;
        template.Rotation = behaviour.Rotation;
        template.Scale = behaviour.Scale;
        template.Alpha = behaviour.Alpha;
        template.Color = behaviour.Color;
        template.Wave = behaviour.Wave;
        template.Shake = behaviour.Shake;
        return clip;
    }

    private TmpCharacterMixer GetMixer()
    {
        var root = _director.playableGraph.GetRootPlayable(0);

        for (var i = 0; i < root.GetInputCount(); i++)
        {
            var input = root.GetInput(i);

            if (input.GetPlayableType() == typeof(TmpCharacterMixer))
            {
                return ((ScriptPlayable<TmpCharacterMixer>)input).GetBehaviour();
            }
        }

        Assert.Fail("no character mixer in the graph");
        return null;
    }

    // What Timeline's window does when the preview stops (WindowState.cs): Stop, then flush the deferred destroy.
    private void StopAsThePreviewDoes()
    {
        _director.Stop();
        typeof(PlayableDirector)
            .GetMethod(
                  "ProcessPendingGraphChanges"
                , BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
            )
            .Invoke(_director, null);
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

    private Vector3[][] Snapshot()
    {
        var meshInfo = _text.textInfo.meshInfo;
        var result = new Vector3[_text.textInfo.materialCount][];

        for (var m = 0; m < result.Length; m++)
        {
            result[m] = (Vector3[])meshInfo[m].vertices.Clone();
        }

        return result;
    }

    private Vector3 VertexOf(int character, int corner)
    {
        var info = _text.textInfo.characterInfo[character];

        return _text.textInfo.meshInfo[info.materialReferenceIndex].vertices[info.vertexIndex + corner];
    }

    private byte AlphaOf(int character)
    {
        var info = _text.textInfo.characterInfo[character];

        return _text.textInfo.meshInfo[info.materialReferenceIndex].colors32[info.vertexIndex].a;
    }

    // The largest vertex difference over the visible characters, between the CPU mesh and the given one.
    private float MaxDelta(Vector3[][] expected)
    {
        var textInfo = _text.textInfo;
        var max = 0f;

        for (var i = 0; i < textInfo.characterCount; i++)
        {
            var info = textInfo.characterInfo[i];

            if (info.isVisible == false)
            {
                continue;
            }

            for (var v = 0; v < 4; v++)
            {
                var index = info.vertexIndex + v;
                var actual = textInfo.meshInfo[info.materialReferenceIndex].vertices[index];
                max = Mathf.Max(max, (actual - expected[info.materialReferenceIndex][index]).magnitude);
            }
        }

        return max;
    }

    // The same over the meshes TMP uploaded.
    private float MaxUploadedDelta(Vector3[][] expected)
    {
        var textInfo = _text.textInfo;
        var uploaded = new Vector3[textInfo.materialCount][];

        for (var m = 0; m < uploaded.Length; m++)
        {
            uploaded[m] = textInfo.meshInfo[m].mesh.vertices;
        }

        var max = 0f;

        for (var i = 0; i < textInfo.characterCount; i++)
        {
            var info = textInfo.characterInfo[i];

            if (info.isVisible == false)
            {
                continue;
            }

            for (var v = 0; v < 4; v++)
            {
                var index = info.vertexIndex + v;
                var actual = uploaded[info.materialReferenceIndex][index];
                max = Mathf.Max(max, (actual - expected[info.materialReferenceIndex][index]).magnitude);
            }
        }

        return max;
    }

    private void AssertOffset(Vector2 offset)
    {
        for (var i = 0; i < _text.textInfo.characterCount; i++)
        {
            if (_text.textInfo.characterInfo[i].isVisible)
            {
                AssertCharacterOffset(i, offset);
            }
        }
    }

    private void AssertCharacterOffset(int character, Vector2 offset)
    {
        var info = _text.textInfo.characterInfo[character];
        var rest = _rest[info.materialReferenceIndex];

        for (var v = 0; v < 4; v++)
        {
            var actual = VertexOf(character, v);
            var expected = rest[info.vertexIndex + v] + (Vector3)offset;

            Assert.That(actual.x, Is.EqualTo(expected.x).Within(TOLERANCE), $"character {character} x");
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(TOLERANCE), $"character {character} y");
        }
    }

    private void AssertUploadedOffset(Vector2 offset)
    {
        var textInfo = _text.textInfo;
        var uploaded = textInfo.meshInfo[0].mesh.vertices;

        for (var i = 0; i < textInfo.characterCount; i++)
        {
            var info = textInfo.characterInfo[i];

            if (info.isVisible == false || info.materialReferenceIndex != 0)
            {
                continue;
            }

            var expected = _rest[0][info.vertexIndex] + (Vector3)offset;
            Assert.That(uploaded[info.vertexIndex].y, Is.EqualTo(expected.y).Within(TOLERANCE), $"uploaded {i}");
        }
    }
}
