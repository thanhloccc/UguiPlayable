using System.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Tracks.Text;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Core;

/// <summary>
/// Loads a clip saved by version 0.1 (the fixture is never saved again) and checks that the fields added later
/// load as the 0.1 behaviour.
/// </summary>
public sealed class LegacyAssetLoadTests
{
    private const string FIXTURE
        = "Packages/com.apexiongame.ugui-playable/ApexionGame.UguiPlayable.Tests.EditorMode/Fixtures/legacy-0.1-tmp-text.playable";

    private const string FROM = "LEVEL 1";
    private const string TO = "LEVEL 2";

    private GameObject _directorObject;
    private GameObject _canvasObject;
    private TimelineAsset _timeline;
    private PlayableDirector _director;
    private TextMeshProUGUI _text;
    private TmpTextClip _copy;

    [SetUp]
    public void SetUp()
    {
        _directorObject = new GameObject("Director");
        _canvasObject = new GameObject("Canvas", typeof(Canvas));
        var textObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));
        textObject.transform.SetParent(_canvasObject.transform, false);

        _text = textObject.AddComponent<TextMeshProUGUI>();
        _text.text = FROM;

        _director = _directorObject.AddComponent<PlayableDirector>();
        _director.playOnAwake = false;
        _timeline = ScriptableObject.CreateInstance<TimelineAsset>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_director != null)
        {
            _director.Stop();
        }

        Object.DestroyImmediate(_directorObject);
        Object.DestroyImmediate(_canvasObject);
        Object.DestroyImmediate(_timeline);

        if (_copy != null)
        {
            Object.DestroyImmediate(_copy);
        }
    }

    [Test]
    public void LegacyClip_HasScrambleRateZero()
    {
        var clip = LoadLegacyClip();

        Assert.AreEqual(0f, clip.Template.Text.ScrambleRate);
    }

    [Test]
    public void LegacyClip_EasesAreNotCustomized()
    {
        var clip = LoadLegacyClip();
        var serialized = new SerializedObject(clip);
        var iterator = serialized.GetIterator();
        var checkedCount = 0;

        // Every EaseSetting in the clip, found by its serialized shape.
        while (iterator.Next(true))
        {
            if (iterator.propertyType != SerializedPropertyType.Generic || iterator.name != "_ease")
            {
                continue;
            }

            var customize = iterator.FindPropertyRelative("_customize");

            if (customize == null)
            {
                continue;
            }

            checkedCount++;
            Assert.IsFalse(customize.boolValue, iterator.propertyPath);
            Assert.AreEqual(0f, iterator.FindPropertyRelative("_strength").floatValue, iterator.propertyPath);
            Assert.AreEqual(0f, iterator.FindPropertyRelative("_period").floatValue, iterator.propertyPath);
        }

        Assert.Greater(checkedCount, 0, "The clip has no EaseSetting to check.");
    }

    [TestCase(0.25f)]
    [TestCase(0.5f)]
    [TestCase(0.75f)]
    public void LegacyClip_EvaluatesLikeVersion01(float progress)
    {
        // An in-memory copy of the legacy clip, with only the text channel switched on; its scramble rate is the
        // loaded one.
        _copy = Object.Instantiate(LoadLegacyClip());

        var serialized = new SerializedObject(_copy);
        var channel = serialized.FindProperty("_template._text");

        channel.FindPropertyRelative("_enabled").boolValue = true;
        channel.FindPropertyRelative("_from").stringValue = FROM;
        channel.FindPropertyRelative("_to").stringValue = TO;
        channel.FindPropertyRelative("_scramble").enumValueIndex = (int)ScrambleMode.All;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        var track = _timeline.CreateTrack<TmpTextTrack>();
        var clip = track.CreateClip<TmpTextClip>();

        clip.asset = _copy;
        clip.start = 0d;
        clip.duration = 4d;

        _director.playableAsset = _timeline;
        _director.SetGenericBinding(track, _text);
        _director.RebuildGraph();
        _director.time = progress * 4d;
        _director.Evaluate();

        var buffer = new char[TextTweenMath.RequiredLength(FROM, TO)];
        var length = TextTweenMath.Fill(buffer, FROM, TO, progress, ScrambleMode.All, string.Empty);

        Assert.AreEqual(new string(buffer, 0, length), _text.text);
    }

    private static TmpTextClip LoadLegacyClip()
    {
        var clip = AssetDatabase.LoadAllAssetsAtPath(FIXTURE).OfType<TmpTextClip>().SingleOrDefault();

        Assert.IsNotNull(clip, "The legacy fixture is missing or has no TmpTextClip.");
        return clip;
    }
}
