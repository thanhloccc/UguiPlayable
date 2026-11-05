using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Editor.Clips;
using ApexionGame.UguiPlayable.Tracks.Controls;
using ApexionGame.UguiPlayable.Tracks.Effects;
using ApexionGame.UguiPlayable.Tracks.Graphics;
using ApexionGame.UguiPlayable.Tracks.Groups;
using ApexionGame.UguiPlayable.Tracks.Layout;
using ApexionGame.UguiPlayable.Tracks.Text;
using ApexionGame.UguiPlayable.Tracks.Transforms;
using Button = UnityEngine.UIElements.Button;
using Object = UnityEngine.Object;
using Toggle = UnityEngine.UIElements.Toggle;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Editor;

public sealed class TweenClipInspectorMultiTests
{
    private const int FRAMES = 3;
    private const string SCALE_TO = "_template._scale._to";
    private const string SCALE_IGNORED = "_template._scale._ignoredAxes";
    private const string SCALE_CONSTRAIN_TO = "_template._scaleConstrainTo";
    private const string SHAKE_SEED = "_template._shake._seed";
    private const string ALPHA_ENABLED = "_template._alpha._enabled";
    private const string ALPHA_TO = "_template._alpha._to";
    private const string TEMP_ASSET = "Assets/UguiPlayableMultiTestsTemp.asset";

    private TimelineAsset _timeline;
    private GameObject _gameObject;
    private UnityEditor.Editor _editor;
    private EditorWindow _window;

    [TearDown]
    public void TearDown()
    {
        if (_window != null)
        {
            _window.Close();
        }

        if (_editor != null)
        {
            Object.DestroyImmediate(_editor);
        }

        if (_gameObject != null)
        {
            Object.DestroyImmediate(_gameObject);
        }

        if (_timeline != null)
        {
            if (AssetDatabase.Contains(_timeline))
            {
                AssetDatabase.DeleteAsset(TEMP_ASSET);
            }
            else
            {
                foreach (var track in _timeline.GetOutputTracks().ToList())
                {
                    foreach (var clip in track.GetClips().ToList())
                    {
                        Object.DestroyImmediate(clip.asset);
                    }

                    Object.DestroyImmediate(track);
                }

                Object.DestroyImmediate(_timeline);
            }
        }

        _window = null;
        _editor = null;
        _gameObject = null;
        _timeline = null;
    }

    [TestCase(typeof(CanvasGroupClip))]
    [TestCase(typeof(RectTransformClip))]
    [TestCase(typeof(GraphicClip))]
    [TestCase(typeof(ImageClip))]
    [TestCase(typeof(SliderClip))]
    [TestCase(typeof(ScrollbarClip))]
    [TestCase(typeof(ScrollRectClip))]
    [TestCase(typeof(RawImageClip))]
    [TestCase(typeof(ShadowClip))]
    [TestCase(typeof(LayoutElementClip))]
    [TestCase(typeof(LayoutGroupClip))]
    [TestCase(typeof(GridLayoutGroupClip))]
    [TestCase(typeof(TmpTextClip))]
    [TestCase(typeof(TmpCharacterClip))]
    [TestCase(typeof(ToggleClip))]
    public void Inspector_BuildsForTwoClipsOfEveryType(Type clipType)
    {
        var clips = CreateTimeline(clipType, 2, out _);

        Assert.IsTrue(
              clips.All(clip => clip.GetType() == clipType)
            , "the clips are of the type the inspector edits"
        );

        _editor = UnityEditor.Editor.CreateEditor(clips);

        var root = _editor.CreateInspectorGUI();
        var model = TweenClipModel.Create(_editor.serializedObject);
        var cards = root.Query<VisualElement>(className: TweenClipSections.CARD_USS_CLASS_NAME).ToList();

        Assert.IsTrue(_editor.serializedObject.isEditingMultipleObjects);
        Assert.IsTrue(cards.Count >= model.Channels.Count);
    }

    [Test]
    public void EveryClipInspectorCanEditMultipleObjects()
    {
        var inspectors = typeof(TweenClipInspector).Assembly.GetTypes()
            .Where(type => type.IsAbstract == false && typeof(TweenClipInspector).IsAssignableFrom(type))
            .ToList();

        Assert.IsNotEmpty(inspectors);

        foreach (var inspector in inspectors)
        {
            Assert.IsTrue(
                  inspector.GetCustomAttributes(typeof(CanEditMultipleObjects), true).Length > 0
                , inspector.Name
            );
        }
    }

    [Test]
    public void Vector_ShowsTheMixedDashOnlyOnTheAxesThatDiffer()
    {
        var clips = CreateTimeline(typeof(RectTransformClip), 2, out _);

        SetVector3(clips[0], SCALE_TO, new Vector3(1f, 2f, 3f));
        SetVector3(clips[1], SCALE_TO, new Vector3(1f, 5f, 3f));

        var field = CreateScaleToField(clips);
        var components = field.Query<FloatField>().ToList();

        Assert.IsFalse(components[0].showMixedValue);
        Assert.IsTrue(components[1].showMixedValue);
        Assert.IsFalse(components[2].showMixedValue);
    }

    [Test]
    public void Vector_EditingOneAxisChangesOnlyThatAxisOnBothClips()
    {
        var clips = CreateTimeline(typeof(RectTransformClip), 2, out _);

        SetVector3(clips[0], SCALE_TO, new Vector3(1f, 2f, 3f));
        SetVector3(clips[1], SCALE_TO, new Vector3(4f, 5f, 6f));

        var field = CreateScaleToField(clips);
        var shown = field.value;

        field.value = new Vector3(shown.x, 9f, shown.z);

        Assert.AreEqual(new Vector3(1f, 9f, 3f), GetVector3(clips[0], SCALE_TO));
        Assert.AreEqual(new Vector3(4f, 9f, 6f), GetVector3(clips[1], SCALE_TO));
    }

    [Test]
    public void Vector_OneUndoRevertsBothClips()
    {
        var clips = CreateTimeline(typeof(RectTransformClip), 2, out _);

        SetVector3(clips[0], SCALE_TO, new Vector3(1f, 2f, 3f));
        SetVector3(clips[1], SCALE_TO, new Vector3(4f, 5f, 6f));

        var field = CreateScaleToField(clips);

        Undo.IncrementCurrentGroup();
        field.value = new Vector3(field.value.x, 9f, field.value.z);

        Assert.AreEqual(9f, GetVector3(clips[0], SCALE_TO).y);
        Assert.AreEqual(9f, GetVector3(clips[1], SCALE_TO).y);

        Undo.PerformUndo();

        Assert.AreEqual(new Vector3(1f, 2f, 3f), GetVector3(clips[0], SCALE_TO));
        Assert.AreEqual(new Vector3(4f, 5f, 6f), GetVector3(clips[1], SCALE_TO));
    }

    [Test]
    public void Vector_ConstrainScalesEachClipFromItsOwnVectorInOneUndoStep()
    {
        var clips = CreateTimeline(typeof(RectTransformClip), 2, out _);

        SetVector3(clips[0], SCALE_TO, new Vector3(1f, 2f, 3f));
        SetVector3(clips[1], SCALE_TO, new Vector3(1f, 1f, 1f));
        SetBool(clips[0], SCALE_CONSTRAIN_TO, true);
        SetBool(clips[1], SCALE_CONSTRAIN_TO, true);

        var field = CreateScaleToField(clips);

        Undo.IncrementCurrentGroup();
        field.value = new Vector3(2f, field.value.y, field.value.z);

        Assert.AreEqual(new Vector3(2f, 4f, 6f), GetVector3(clips[0], SCALE_TO));
        Assert.AreEqual(new Vector3(2f, 2f, 2f), GetVector3(clips[1], SCALE_TO));

        Undo.PerformUndo();

        Assert.AreEqual(new Vector3(1f, 2f, 3f), GetVector3(clips[0], SCALE_TO));
        Assert.AreEqual(new Vector3(1f, 1f, 1f), GetVector3(clips[1], SCALE_TO));
    }

    [Test]
    public void Vector_ConstrainOnOneClipOnlyScalesThatClip()
    {
        var clips = CreateTimeline(typeof(RectTransformClip), 2, out _);

        SetVector3(clips[0], SCALE_TO, new Vector3(1f, 2f, 3f));
        SetVector3(clips[1], SCALE_TO, new Vector3(1f, 5f, 6f));
        SetBool(clips[0], SCALE_CONSTRAIN_TO, true);

        var field = CreateScaleToField(clips);

        field.value = new Vector3(2f, field.value.y, field.value.z);

        Assert.AreEqual(new Vector3(2f, 4f, 6f), GetVector3(clips[0], SCALE_TO));
        Assert.AreEqual(new Vector3(2f, 5f, 6f), GetVector3(clips[1], SCALE_TO));
    }

    [Test]
    public void AxisButtons_AMixedAxisIsNotOnAndAClickSetsItOnAllClips()
    {
        var clips = CreateTimeline(typeof(RectTransformClip), 2, out _);

        // Clip A animates X, Y and Z; clip B ignores Y (bit 1).
        SetInt(clips[0], SCALE_IGNORED, 0);
        SetInt(clips[1], SCALE_IGNORED, 2);

        var card = CreateScaleCard(clips, new ClipBindingSource(clips));
        var buttons = card.Query<Button>(className: TweenClipSections.AXIS_BUTTON_USS_CLASS_NAME).ToList();
        var group = card.Q<ToggleButtonGroup>(className: TweenClipSections.AXIS_GROUP_USS_CLASS_NAME);

        Assert.AreEqual(3, buttons.Count);
        Assert.IsTrue(buttons[0].ClassListContains(TweenClipSections.AXIS_BUTTON_ON_USS_CLASS_NAME));
        Assert.IsFalse(buttons[1].ClassListContains(TweenClipSections.AXIS_BUTTON_ON_USS_CLASS_NAME));
        Assert.IsTrue(buttons[1].ClassListContains(TweenClipSections.AXIS_BUTTON_MIXED_USS_CLASS_NAME));
        Assert.IsTrue(buttons[2].ClassListContains(TweenClipSections.AXIS_BUTTON_ON_USS_CLASS_NAME));

        // Turn the mixed Y on for everyone: B no longer ignores it, A is unchanged.
        Undo.IncrementCurrentGroup();
        group.value = new ToggleButtonGroupState(0b111UL, 3);

        Assert.AreEqual(0, GetInt(clips[0], SCALE_IGNORED));
        Assert.AreEqual(0, GetInt(clips[1], SCALE_IGNORED));
        Assert.IsTrue(buttons[1].ClassListContains(TweenClipSections.AXIS_BUTTON_ON_USS_CLASS_NAME));
        Assert.IsFalse(buttons[1].ClassListContains(TweenClipSections.AXIS_BUTTON_MIXED_USS_CLASS_NAME));

        Undo.PerformUndo();

        Assert.AreEqual(0, GetInt(clips[0], SCALE_IGNORED));
        Assert.AreEqual(2, GetInt(clips[1], SCALE_IGNORED));
    }

    [Test]
    public void AxisButtons_ClearingOneAxisKeepsTheOtherAxesOfEachClip()
    {
        var clips = CreateTimeline(typeof(RectTransformClip), 2, out _);

        SetInt(clips[0], SCALE_IGNORED, 0);
        SetInt(clips[1], SCALE_IGNORED, 2);

        var card = CreateScaleCard(clips, new ClipBindingSource(clips));
        var group = card.Q<ToggleButtonGroup>(className: TweenClipSections.AXIS_GROUP_USS_CLASS_NAME);

        // Turn X off: Y stays mixed (shown off), Z stays on.
        group.value = new ToggleButtonGroupState(0b100UL, 3);

        Assert.AreEqual(1, GetInt(clips[0], SCALE_IGNORED));
        Assert.AreEqual(3, GetInt(clips[1], SCALE_IGNORED));
    }

    [Test]
    public void Summary_ShowsARangeWhenTheCountsDiffer()
    {
        var clips = CreateTimeline(typeof(CanvasGroupClip), 2, out _);

        SetAllEnabled(clips[0], false);
        SetAllEnabled(clips[1], true);
        SetBool(clips[0], ALPHA_ENABLED, true);

        _editor = UnityEditor.Editor.CreateEditor(clips);

        var root = _editor.CreateInspectorGUI();
        var count = root.Q<Label>(className: TweenClipSections.COUNT_USS_CLASS_NAME);

        Assert.AreEqual("1\u20133 of 3 channels on", count.text);

        SetBool(clips[0], ALPHA_ENABLED, false);
        SetBool(clips[1], ALPHA_ENABLED, false);

        var all = TweenClipModel.Create(_editor.serializedObject);

        Assert.AreEqual(0, all.MinEnabledCount);
        Assert.AreEqual(2, all.EnabledCount);
        Assert.IsTrue(all.HasEnabledChannel);
    }

    [Test]
    public void Summary_ShowsOneCountWhenTheyMatchAndTheWarningOnlyWhenNoneIsOn()
    {
        var clips = CreateTimeline(typeof(CanvasGroupClip), 2, out _);

        SetAllEnabled(clips[0], true);
        SetAllEnabled(clips[1], true);

        _editor = UnityEditor.Editor.CreateEditor(clips);

        var root = _editor.CreateInspectorGUI();

        Assert.AreEqual("3 of 3 channels on", root.Q<Label>(className: TweenClipSections.COUNT_USS_CLASS_NAME).text);
        Assert.AreEqual("0\u20133 of 3 channels on", TweenClipTexts.FormatChannelCount(0, 3, 3));
        Assert.AreEqual("2 of 3 channels on", TweenClipTexts.FormatChannelCount(2, 2, 3));

        SetAllEnabled(clips[0], false);
        SetAllEnabled(clips[1], false);

        Assert.IsFalse(TweenClipModel.Create(_editor.serializedObject).HasEnabledChannel);

        SetAllEnabled(clips[1], true);

        Assert.IsTrue(TweenClipModel.Create(_editor.serializedObject).HasEnabledChannel);
    }

    [UnityTest]
    public IEnumerator EnabledSwitch_ShowsMixedAndTheCardIsDimmedOnlyWhenEveryClipIsOff()
    {
        var clips = CreateTimeline(typeof(CanvasGroupClip), 2, out _);

        SetBool(clips[0], ALPHA_ENABLED, true);
        SetBool(clips[1], ALPHA_ENABLED, false);

        _editor = UnityEditor.Editor.CreateEditor(clips);

        var root = BuildInHost();
        yield return Frames();

        var model = TweenClipModel.Create(_editor.serializedObject);
        var cards = root.Query<VisualElement>(className: TweenClipSections.CARD_USS_CLASS_NAME).ToList();
        var index = model.Channels.ToList().FindIndex(channel => channel.Name == "_alpha");
        var card = cards[index];
        var toggle = card.Q<Toggle>();

        Assert.IsTrue(toggle.showMixedValue);
        Assert.IsFalse(card.ClassListContains(TweenClipSections.CARD_DISABLED_USS_CLASS_NAME));

        SetBool(clips[0], ALPHA_ENABLED, false);
        yield return Frames();
        yield return Frames();

        Assert.IsTrue(card.ClassListContains(TweenClipSections.CARD_DISABLED_USS_CLASS_NAME));
    }

    [Test]
    public void Mode_SegmentedControlShowsNoSelectionWhenTheModesDifferAndAClickSetsAll()
    {
        var clips = CreateTimeline(typeof(CanvasGroupClip), 2, out _);

        SetInt(clips[0], "_template._alpha._mode", (int)ValueMode.Absolute);
        SetInt(clips[1], "_template._alpha._mode", (int)ValueMode.Relative);

        var card = CreateCard(clips, "_alpha", new ClipBindingSource(clips));
        var group = card.Q<ToggleButtonGroup>(className: ApexionGame.UguiPlayable.Editor.Common.SegmentedControl.USS_CLASS_NAME);

        Assert.AreEqual(-1, ApexionGame.UguiPlayable.Editor.Common.SegmentedControl.GetSelectedIndex(group.value));

        Undo.IncrementCurrentGroup();
        group.value = ApexionGame.UguiPlayable.Editor.Common.SegmentedControl.ToState((int)ValueMode.FromCurrent, 3);

        Assert.AreEqual((int)ValueMode.FromCurrent, GetInt(clips[0], "_template._alpha._mode"));
        Assert.AreEqual((int)ValueMode.FromCurrent, GetInt(clips[1], "_template._alpha._mode"));

        Undo.PerformUndo();

        Assert.AreEqual((int)ValueMode.Absolute, GetInt(clips[0], "_template._alpha._mode"));
        Assert.AreEqual((int)ValueMode.Relative, GetInt(clips[1], "_template._alpha._mode"));
    }

    [Test]
    public void Pipette_IsDisabledWithTheReasonWhenTheTracksAreBoundToDifferentObjects()
    {
        var clips = CreateTimeline(typeof(CanvasGroupClip), 2, out var director);
        var first = new GameObject("First", typeof(CanvasGroup));
        var second = new GameObject("Second", typeof(CanvasGroup));

        try
        {
            var tracks = _timeline.GetOutputTracks().ToList();

            director.SetGenericBinding(tracks[0], first.GetComponent<CanvasGroup>());
            director.SetGenericBinding(tracks[1], second.GetComponent<CanvasGroup>());

            var source = new ClipBindingSource(clips, Resolver(director));

            source.Refresh();

            Assert.IsNull(source.Binding);
            Assert.AreEqual(TweenClipTexts.MIXED_BINDINGS, source.Reason);

            var card = CreateCard(clips, "_alpha", source);
            var pipettes = card.Query<Button>(className: TweenClipSections.ICON_BUTTON_USS_CLASS_NAME)
                .ToList()
                .Where(button => button.tooltip == TweenClipTexts.MIXED_BINDINGS)
                .ToList();

            Assert.AreEqual(2, pipettes.Count, "the from and to pipettes");
            Assert.IsTrue(pipettes.All(button => button.enabledSelf == false));
        }
        finally
        {
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }
    }

    [Test]
    public void Pipette_WritesTheSameBindingValueToEveryClipInOneUndoStep()
    {
        var clips = CreateTimeline(typeof(CanvasGroupClip), 2, out var director);
        var shared = new GameObject("Shared", typeof(CanvasGroup));

        try
        {
            var group = shared.GetComponent<CanvasGroup>();
            group.alpha = 0.4f;

            foreach (var track in _timeline.GetOutputTracks())
            {
                director.SetGenericBinding(track, group);
            }

            SetFloat(clips[0], ALPHA_TO, 0.1f);
            SetFloat(clips[1], ALPHA_TO, 0.9f);

            var source = new ClipBindingSource(clips, Resolver(director));

            source.Refresh();

            Assert.AreSame(group, source.Binding);

            var card = CreateCard(clips, "_alpha", source);
            var pipettes = card.Query<Button>(className: TweenClipSections.ICON_BUTTON_USS_CLASS_NAME)
                .ToList()
                .Where(button => button.tooltip == TweenClipTexts.PIPETTE_TOOLTIP)
                .ToList();

            Assert.AreEqual(2, pipettes.Count);
            Assert.IsTrue(pipettes.All(button => button.enabledSelf));

            Undo.IncrementCurrentGroup();
            Click(pipettes[1]);

            Assert.AreEqual(0.4f, GetFloat(clips[0], ALPHA_TO), 1e-5f);
            Assert.AreEqual(0.4f, GetFloat(clips[1], ALPHA_TO), 1e-5f);

            Undo.PerformUndo();

            Assert.AreEqual(0.1f, GetFloat(clips[0], ALPHA_TO), 1e-5f);
            Assert.AreEqual(0.9f, GetFloat(clips[1], ALPHA_TO), 1e-5f);
        }
        finally
        {
            Object.DestroyImmediate(shared);
        }
    }

    [Test]
    public void BindingSource_ReportsTheFirstReasonWhenAClipHasNoBinding()
    {
        var clips = CreateTimeline(typeof(CanvasGroupClip), 2, out var director);
        var shared = new GameObject("Shared", typeof(CanvasGroup));

        try
        {
            var tracks = _timeline.GetOutputTracks().ToList();

            director.SetGenericBinding(tracks[0], shared.GetComponent<CanvasGroup>());

            var source = new ClipBindingSource(clips, Resolver(director));

            source.Refresh();

            Assert.IsNull(source.Binding);
            Assert.AreEqual(TweenClipTexts.NO_BINDING, source.Reason);
        }
        finally
        {
            Object.DestroyImmediate(shared);
        }
    }

    [Test]
    public void Seed_EachClipGetsItsOwnNewSeedInOneUndoStep()
    {
        var clips = CreateTimeline(typeof(RectTransformClip), 2, out _);

        SetInt(clips[0], SHAKE_SEED, 7);
        SetInt(clips[1], SHAKE_SEED, 7);

        var card = CreateCard(clips, "_shake", new ClipBindingSource(clips));
        var dice = card.Query<Button>(className: TweenClipSections.ICON_BUTTON_USS_CLASS_NAME)
            .ToList()
            .First(button => button.tooltip == TweenClipTexts.NEW_SEED_TOOLTIP);

        Undo.IncrementCurrentGroup();
        Click(dice);

        var first = GetInt(clips[0], SHAKE_SEED);
        var second = GetInt(clips[1], SHAKE_SEED);

        Assert.AreNotEqual(7, first);
        Assert.AreNotEqual(7, second);
        Assert.AreNotEqual(first, second, "each clip has its own seed");

        Undo.PerformUndo();

        Assert.AreEqual(7, GetInt(clips[0], SHAKE_SEED));
        Assert.AreEqual(7, GetInt(clips[1], SHAKE_SEED));
    }

    [Test]
    public void CharacterTiming_SeedButtonGivesEachClipItsOwnSeed()
    {
        var clips = CreateTimeline(typeof(TmpCharacterClip), 2, out _);

        SetInt(clips[0], "_template._orderSeed", 3);
        SetInt(clips[1], "_template._orderSeed", 3);

        var serializedObject = new SerializedObject(clips);
        var section = CharacterTimingSection.Create(serializedObject);
        var dice = section.Query<Button>(className: TweenClipSections.ICON_BUTTON_USS_CLASS_NAME)
            .ToList()
            .First(button => button.tooltip == TweenClipTexts.NEW_SEED_TOOLTIP);

        Click(dice);

        Assert.AreNotEqual(
              GetInt(clips[0], "_template._orderSeed")
            , GetInt(clips[1], "_template._orderSeed")
        );
    }

    [Test]
    public void Save_TheTimelineAssetWritesTheEditsOfBothClips()
    {
        var timeline = ScriptableObject.CreateInstance<TimelineAsset>();

        AssetDatabase.CreateAsset(timeline, TEMP_ASSET);
        _timeline = timeline;

        var first = timeline.CreateTrack<RectTransformTrack>().CreateClip<RectTransformClip>().asset;
        var second = timeline.CreateTrack<RectTransformTrack>().CreateClip<RectTransformClip>().asset;
        var clips = new[] { first, second };

        SetVector3(clips[0], SCALE_TO, new Vector3(1f, 2f, 3f));
        SetVector3(clips[1], SCALE_TO, new Vector3(4f, 5f, 6f));
        AssetDatabase.SaveAssetIfDirty(timeline);

        var field = CreateScaleToField(clips);

        field.value = new Vector3(field.value.x, 9f, field.value.z);
        AssetDatabase.SaveAssetIfDirty(timeline);
        AssetDatabase.ImportAsset(TEMP_ASSET, ImportAssetOptions.ForceUpdate);

        var reloaded = AssetDatabase.LoadAllAssetsAtPath(TEMP_ASSET).OfType<RectTransformClip>().ToArray();

        Assert.AreEqual(2, reloaded.Length);

        var values = reloaded.Select(clip => GetVector3(clip, SCALE_TO)).OrderBy(value => value.x).ToArray();

        Assert.AreEqual(new Vector3(1f, 9f, 3f), values[0]);
        Assert.AreEqual(new Vector3(4f, 9f, 6f), values[1]);
    }

    private Object[] CreateTimeline(Type clipType, int count, out PlayableDirector director)
    {
        _timeline = ScriptableObject.CreateInstance<TimelineAsset>();

        var clips = new Object[count];

        for (var i = 0; i < count; i++)
        {
            var track = _timeline.CreateTrack(GetTrackType(clipType), null, $"Track {i}");
            var clip = track.CreateDefaultClip();

            clips[i] = clip.asset;
        }

        _gameObject = new GameObject("Director", typeof(PlayableDirector));
        director = _gameObject.GetComponent<PlayableDirector>();
        director.playableAsset = _timeline;
        return clips;
    }

    // The track type is the one whose clip type is the given one.
    private static Type GetTrackType(Type clipType)
    {
        foreach (var type in typeof(CanvasGroupTrack).Assembly.GetTypes())
        {
            if (type.IsAbstract || typeof(TrackAsset).IsAssignableFrom(type) == false)
            {
                continue;
            }

            foreach (var attribute in type.GetCustomAttributes(typeof(TrackClipTypeAttribute), true))
            {
                if (((TrackClipTypeAttribute)attribute).inspectedType == clipType)
                {
                    return type;
                }
            }
        }

        throw new InvalidOperationException($"No track for {clipType.Name}");
    }

    private static ClipBindingSource.TryResolveDelegate Resolver(PlayableDirector director)
        => (Object clip, out Object binding, out string reason)
            => ClipBindingSource.TryResolve(clip, director.playableAsset as TimelineAsset, director, out binding, out reason);

    private VisualElement CreateScaleCard(Object[] clips, ClipBindingSource source)
        => CreateCard(clips, "_scale", source);

    private VisualElement CreateCard(Object[] clips, string channel, ClipBindingSource source)
    {
        var serializedObject = new SerializedObject(clips);
        var info = TweenClipModel.Create(serializedObject).Channels.First(item => item.Name == channel);

        var card = info.Shape switch {
            ChannelShape.Oscillation => OscillationChannelCardSection.Create(serializedObject, info),
            _ => ChannelCardSection.Create(serializedObject, info, source),
        };

        // Value-changed events are only sent to elements that are in a panel.
        _window = ScriptableObject.CreateInstance<HostWindow>();
        _window.Show();
        _window.rootVisualElement.Add(card);
        return card;
    }

    private Vector3Field CreateScaleToField(Object[] clips)
    {
        var card = CreateScaleCard(clips, new ClipBindingSource(clips));

        // From and To fields, in that order.
        return card.Query<Vector3Field>().ToList()[1];
    }

    private VisualElement BuildInHost()
    {
        var root = _editor.CreateInspectorGUI();

        _window = ScriptableObject.CreateInstance<HostWindow>();
        _window.Show();
        _window.rootVisualElement.Add(root);
        _window.rootVisualElement.Bind(_editor.serializedObject);
        return root;
    }

    private static IEnumerator Frames()
    {
        for (var i = 0; i < FRAMES; i++)
        {
            yield return null;
        }
    }

    // A Button has no public way to run its handler without a panel and pointer events.
    private static void Click(Button button)
    {
        var field = typeof(Clickable).GetField("clicked", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        Assert.IsNotNull(field, "Clickable.clicked");
        ((Action)field.GetValue(button.clickable))?.Invoke();
    }

    private static void SetAllEnabled(Object clip, bool enabled)
    {
        var serializedObject = new SerializedObject(clip);
        var channels = TweenClipModel.Create(serializedObject).Channels;

        foreach (var channel in channels)
        {
            serializedObject.FindProperty(TweenClipModel.GetEnabledPath(channel.Name)).boolValue = enabled;
        }

        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetVector3(Object clip, string path, Vector3 value)
        => Modify(clip, path, p => p.vector3Value = value);

    private static void SetBool(Object clip, string path, bool value)
        => Modify(clip, path, p => p.boolValue = value);

    private static void SetFloat(Object clip, string path, float value)
        => Modify(clip, path, p => p.floatValue = value);

    // Enums and flags both go through enumValueFlag, which is the underlying value.
    private static void SetInt(Object clip, string path, int value)
        => Modify(clip, path, p => {
            if (p.propertyType == SerializedPropertyType.Enum)
            {
                p.enumValueFlag = value;
            }
            else
            {
                p.intValue = value;
            }
        });

    private static Vector3 GetVector3(Object clip, string path)
        => Read(clip, path, p => p.vector3Value);

    private static float GetFloat(Object clip, string path)
        => Read(clip, path, p => p.floatValue);

    private static int GetInt(Object clip, string path)
        => Read(clip, path, p => p.propertyType == SerializedPropertyType.Enum ? p.enumValueFlag : p.intValue);

    private static void Modify(Object clip, string path, Action<SerializedProperty> write)
    {
        using var serializedObject = new SerializedObject(clip);

        write(serializedObject.FindProperty(path));
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static T Read<T>(Object clip, string path, Func<SerializedProperty, T> read)
    {
        using var serializedObject = new SerializedObject(clip);

        return read(serializedObject.FindProperty(path));
    }

    private sealed class HostWindow : EditorWindow
    {
    }
}
