using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;
using UnityEngine.UIElements;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Editor.Clips;
using ApexionGame.UguiPlayable.Editor.Common;
using ApexionGame.UguiPlayable.Tracks.Controls;
using ApexionGame.UguiPlayable.Tracks.Effects;
using ApexionGame.UguiPlayable.Tracks.Graphics;
using ApexionGame.UguiPlayable.Tracks.Groups;
using ApexionGame.UguiPlayable.Tracks.Layout;
using ApexionGame.UguiPlayable.Tracks.Text;
using ApexionGame.UguiPlayable.Tracks.Transforms;
using Button = UnityEngine.UIElements.Button;
using Image = UnityEngine.UI.Image;
using Object = UnityEngine.Object;
using Slider = UnityEngine.UI.Slider;
using Toggle = UnityEngine.UIElements.Toggle;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Editor;

public sealed class TweenClipInspectorTests
{
    private const int FRAMES = 3;

    private ScriptableObject _clip;
    private UnityEditor.Editor _editor;
    private HostWindow _window;
    private GameObject _gameObject;

    [TearDown]
    public void TearDown()
    {
        if (_window.IsValid())
        {
            _window.Close();
        }

        if (_editor.IsValid())
        {
            Object.DestroyImmediate(_editor);
        }

        if (_clip.IsValid())
        {
            Object.DestroyImmediate(_clip);
        }

        if (_gameObject.IsValid())
        {
            Object.DestroyImmediate(_gameObject);
        }

        _editor = null;
        _clip = null;
        _gameObject = null;
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
    public void Inspector_BuildsWithoutABinding(Type clipType)
    {
        Create(clipType);

        var root = _editor.CreateInspectorGUI();
        var model = TweenClipModel.Create(_editor.serializedObject);

        var cardCount = root.Query<VisualElement>(className: TweenClipSections.CARD_USS_CLASS_NAME).ToList().Count;

        Assert.IsNotNull(root);
        Assert.Greater(cardCount, 0);
        Assert.AreEqual(model.Channels.Count, cardCount);

        Assert.IsInstanceOf<TweenClipInspector>(_editor);
    }

    [TestCase(typeof(CanvasGroupClip), "_alpha,_interactable,_blocksRaycasts")]
    [TestCase(
          typeof(RectTransformClip)
        , "_anchoredPosition,_sizeDelta,_rotation,_scale,_pivot,_anchorMin,_anchorMax,_punch,_shake"
    )]
    [TestCase(typeof(GraphicClip), "_color,_alpha")]
    [TestCase(typeof(ImageClip), "_color,_alpha,_fillAmount")]
    [TestCase(typeof(SliderClip), "_value")]
    [TestCase(typeof(ScrollbarClip), "_value,_size")]
    [TestCase(typeof(ScrollRectClip), "_normalizedPosition")]
    [TestCase(typeof(RawImageClip), "_color,_alpha,_uvRect")]
    [TestCase(typeof(ShadowClip), "_effectColor,_effectDistance")]
    [TestCase(
          typeof(LayoutElementClip)
        , "_minWidth,_minHeight,_preferredWidth,_preferredHeight,_flexibleWidth,_flexibleHeight"
    )]
    [TestCase(typeof(LayoutGroupClip), "_spacing,_padding")]
    [TestCase(typeof(GridLayoutGroupClip), "_spacing,_cellSize,_padding")]
    [TestCase(
          typeof(TmpTextClip)
        , "_color,_alpha,_fontSize,_characterSpacing,_wordSpacing,_lineSpacing,_paragraphSpacing,_visibleRatio,_colorGradient,_text"
    )]
    public void Model_FindsTheChannelsOfEachClipType(Type clipType, string expected)
    {
        Create(clipType);

        var model = TweenClipModel.Create(_editor.serializedObject);
        var names = string.Join(",", model.Channels.Select(channel => channel.Name));

        Assert.AreEqual(expected, names);
    }

    [Test]
    public void Model_DetectsShapesValueKindsAndConstrainFlags()
    {
        Create(typeof(RectTransformClip));

        var channels = TweenClipModel.Create(_editor.serializedObject).Channels;

        Assert.IsTrue(channels
            .Where(channel => channel.Shape != ChannelShape.Oscillation)
            .All(channel => channel.Shape == ChannelShape.Tween)
        );

        Assert.AreEqual(ChannelShape.Oscillation, Find(channels, "_punch").Shape);
        Assert.AreEqual(ChannelShape.Oscillation, Find(channels, "_shake").Shape);
        Assert.AreEqual(ChannelValueKind.Vector3, Find(channels, "_scale").ValueKind);
        Assert.AreEqual(ChannelValueKind.Vector2, Find(channels, "_sizeDelta").ValueKind);
        Assert.IsTrue(Find(channels, "_scale").HasConstrain);
        Assert.IsTrue(Find(channels, "_sizeDelta").HasConstrain);
        Assert.IsFalse(Find(channels, "_anchoredPosition").HasConstrain);

        Object.DestroyImmediate(_editor);
        Object.DestroyImmediate(_clip);

        Create(typeof(CanvasGroupClip));

        channels = TweenClipModel.Create(_editor.serializedObject).Channels;

        Assert.AreEqual(ChannelShape.Tween, Find(channels, "_alpha").Shape);
        Assert.AreEqual(ChannelShape.Step, Find(channels, "_interactable").Shape);
        Assert.AreEqual(ChannelShape.Step, Find(channels, "_blocksRaycasts").Shape);

        Object.DestroyImmediate(_editor);
        Object.DestroyImmediate(_clip);

        Create(typeof(GraphicClip));

        channels = TweenClipModel.Create(_editor.serializedObject).Channels;

        Assert.AreEqual(ChannelValueKind.Color, Find(channels, "_color").ValueKind);
    }

    [Test]
    public void Model_CountsEnabledChannels()
    {
        Create(typeof(CanvasGroupClip));

        var model = TweenClipModel.Create(_editor.serializedObject);

        Assert.AreEqual(1, model.EnabledCount, "only alpha is on by default");
        Assert.IsTrue(model.HasEnabledChannel);
    }

    [UnityTest]
    public IEnumerator EnableSwitch_IsOneUndoStep()
    {
        Create(typeof(RectTransformClip));

        var clip = (RectTransformClip)_clip;
        var root = BuildInHost();
        yield return Frames();

        Assert.IsFalse(clip.Template.AnchoredPosition.Enabled);

        var toggle = root.Query<Toggle>().ToList().First(t => t.ClassListContains("ugp-switch"));

        Undo.IncrementCurrentGroup();
        toggle.value = true;
        yield return Frames();

        Assert.IsTrue(clip.Template.AnchoredPosition.Enabled);

        Undo.PerformUndo();
        yield return Frames();

        Assert.IsFalse(clip.Template.AnchoredPosition.Enabled, "one undo reverts the switch");
    }

    [UnityTest]
    public IEnumerator ModeControl_WritesTheModeInOneUndoStep()
    {
        Create(typeof(GraphicClip));

        var clip = (GraphicClip)_clip;
        var root = BuildInHost();
        yield return Frames();

        var modeControl = root.Q<ToggleButtonGroup>();

        Assert.AreEqual(ValueMode.Absolute, clip.Template.Color.Mode);

        Undo.IncrementCurrentGroup();
        modeControl.value = SegmentedControl.ToState((int)ValueMode.Relative, 3);
        yield return Frames();

        Assert.AreEqual(ValueMode.Relative, clip.Template.Color.Mode);

        Undo.PerformUndo();
        yield return Frames();

        Assert.AreEqual(ValueMode.Absolute, clip.Template.Color.Mode);
    }

    [Test]
    public void RotationModePath_IsOnlyOnTheRectTransformRotation()
    {
        Assert.AreEqual(
              "_rotationMode"
            , ChannelPresentation.GetRotationModePath(typeof(RectTransformClip), ChannelPresentation.ROTATION)
        );

        Assert.IsNull(ChannelPresentation.GetRotationModePath(typeof(RectTransformClip), ChannelPresentation.SCALE));
        Assert.IsNull(ChannelPresentation.GetRotationModePath(typeof(GraphicClip), ChannelPresentation.ROTATION));

        Create(typeof(RectTransformClip));

        var path = TweenClipModel.TEMPLATE + "." + ChannelPresentation.ROTATION_MODE;

        Assert.IsNotNull(_editor.serializedObject.FindProperty(path));
    }

    [UnityTest]
    public IEnumerator RotationCard_PathRowWritesTheModeInOneUndoStep_AndShortestPathDisablesTheAxes()
    {
        Create(typeof(RectTransformClip));

        var clip = (RectTransformClip)_clip;
        var root = BuildInHost();
        yield return Frames();

        var pathRows = root.Query<VisualElement>(className: TweenClipSections.ROW_USS_CLASS_NAME)
            .ToList()
            .Where(IsPathRow)
            .ToList();

        Assert.AreEqual(1, pathRows.Count, "no other card has the row");

        var card = FindCard(pathRows[0]);
        var cardName = card.Q<Label>(className: TweenClipSections.CARD_NAME_USS_CLASS_NAME);
        var pathControl = pathRows[0].Q<ToggleButtonGroup>();
        var axes = card.Q<ToggleButtonGroup>(className: TweenClipSections.AXIS_GROUP_USS_CLASS_NAME);
        var note = card.Q<Label>(className: TweenClipSections.NOTE_USS_CLASS_NAME);

        Assert.AreEqual("Rotation", cardName.text);
        Assert.AreEqual(RotationMode.Euler, clip.Template.RotationMode);
        Assert.IsTrue(axes.enabledSelf);

        Undo.IncrementCurrentGroup();
        pathControl.value = SegmentedControl.ToState((int)RotationMode.ShortestPath, 2);
        yield return Frames();

        Assert.AreEqual(RotationMode.ShortestPath, clip.Template.RotationMode);
        Assert.IsFalse(axes.enabledSelf, "Shortest path ignores the axis toggles");
        Assert.IsTrue(note.text.Contains(TweenClipTexts.SHORTEST_PATH_NOTE));

        Undo.PerformUndo();
        yield return Frames();

        Assert.AreEqual(RotationMode.Euler, clip.Template.RotationMode, "one undo reverts the path");
        Assert.IsTrue(axes.enabledSelf);
        Assert.IsFalse(note.text.Contains(TweenClipTexts.SHORTEST_PATH_NOTE));
    }

    private static bool IsPathRow(VisualElement row)
    {
        var label = row.Q<Label>(className: TweenClipSections.ROW_LABEL_USS_CLASS_NAME);
        return label != null && label.text == TweenClipTexts.ROTATION_PATH;
    }

    private static VisualElement FindCard(VisualElement element)
    {
        while (element != null && element.ClassListContains(TweenClipSections.CARD_USS_CLASS_NAME) == false)
        {
            element = element.parent;
        }

        return element;
    }

    [Test]
    public void Pipette_IsDisabledWithoutATimelineBinding()
    {
        Create(typeof(RectTransformClip));

        var root = _editor.CreateInspectorGUI();
        var pipettes = root.Query<Button>(className: TweenClipSections.ICON_BUTTON_USS_CLASS_NAME).ToList();

        Assert.IsNotEmpty(pipettes);
        Assert.IsTrue(pipettes.Any(button => button.enabledSelf == false && button.tooltip.Length > 0));
    }

    [Test]
    public void ChannelPresentation_FallsBackToTheNicifiedName()
    {
        Assert.AreEqual("Anchored Position", ChannelPresentation.GetDisplayName("_anchoredPosition"));
        Assert.IsNotNull(ChannelPresentation.GetIcon("_anchoredPosition"));
        Assert.AreEqual("Some Custom Channel", ChannelPresentation.GetDisplayName("_someCustomChannel"));
        Assert.IsNull(ChannelPresentation.GetIcon("_someCustomChannel"));
    }

    [Test]
    public void ConstrainMath_ScalesTheOtherAxesByTheSameRatio()
    {
        var result = ConstrainMath.Apply(new Vector3(1f, 2f, 3f), new Vector3(2f, 2f, 3f), AxisMask.None, 3);

        Assert.AreEqual(new Vector3(2f, 4f, 6f), result);
    }

    [Test]
    public void ConstrainMath_LeavesIgnoredAxesAlone()
    {
        var result = ConstrainMath.Apply(new Vector3(1f, 2f, 3f), new Vector3(3f, 2f, 3f), AxisMask.Y, 3);

        Assert.AreEqual(new Vector3(3f, 2f, 9f), result);
    }

    [Test]
    public void ConstrainMath_IgnoresAxesBeyondTheDimension()
    {
        var result = ConstrainMath.Apply(new Vector3(1f, 2f, 5f), new Vector3(2f, 2f, 5f), AxisMask.None, 2);

        Assert.AreEqual(new Vector3(2f, 4f, 5f), result);
    }

    [Test]
    public void ConstrainMath_ZeroPreviousValueHasNoRatio()
    {
        var result = ConstrainMath.Apply(new Vector3(0f, 2f, 3f), new Vector3(5f, 2f, 3f), AxisMask.None, 3);

        Assert.AreEqual(new Vector3(5f, 2f, 3f), result);
    }

    [Test]
    public void ConstrainMath_IgnoredEditedAxisDoesNotRescaleTheOthers()
    {
        var result = ConstrainMath.Apply(new Vector3(1f, 2f, 3f), new Vector3(2f, 2f, 3f), AxisMask.X, 3);

        Assert.AreEqual(new Vector3(2f, 2f, 3f), result);
    }

    [Test]
    public void ConstrainMath_TypingThroughZeroScalesFromTheBase()
    {
        var baseValue = new Vector3(2f, 4f, 6f);

        // Keystroke 1: "0" replaces "2". The others follow to 0 while the edit is open.
        var first = ConstrainMath.Apply(baseValue, baseValue, new Vector3(0f, 4f, 6f), AxisMask.None, 3);

        Assert.AreEqual(Vector3.zero, first);

        // Keystroke 2: "0.5". The ratio comes from the base, not from the 0 the previous event left.
        var second = ConstrainMath.Apply(baseValue, first, new Vector3(0.5f, 0f, 0f), AxisMask.None, 3);

        Assert.AreEqual(0.5f, second.x, 0.0001f);
        Assert.AreEqual(baseValue.y * 0.5f / baseValue.x, second.y, 0.0001f);
        Assert.AreEqual(baseValue.z * 0.5f / baseValue.x, second.z, 0.0001f);
    }

    [Test]
    public void ConstrainMath_ZeroBaseOnTheEditedAxisHasNoRatio()
    {
        var baseValue = new Vector3(0f, 4f, 6f);
        var result = ConstrainMath.Apply(baseValue, baseValue, new Vector3(3f, 4f, 6f), AxisMask.None, 3);

        Assert.AreEqual(new Vector3(3f, 4f, 6f), result);
    }

    [UnityTest]
    public IEnumerator LinkIcon_FollowsTheConstrainFlagThroughUndo()
    {
        Create(typeof(RectTransformClip));

        var root = BuildInHost();
        yield return Frames();

        var link = root.Query<Button>().ToList().First(button => button.tooltip == TweenClipTexts.LINK_TOOLTIP);
        var path = $"{TweenClipModel.TEMPLATE}.{TweenClipModel.GetConstrainName("_sizeDelta", true)}";
        var serialized = _editor.serializedObject;

        Assert.IsFalse(link.ClassListContains(TweenClipSections.ICON_BUTTON_ON_USS_CLASS_NAME));

        Undo.IncrementCurrentGroup();
        serialized.Update();
        serialized.FindProperty(path).boolValue = true;
        serialized.ApplyModifiedProperties();
        yield return Frames();

        Assert.IsTrue(link.ClassListContains(TweenClipSections.ICON_BUTTON_ON_USS_CLASS_NAME));

        Undo.PerformUndo();
        yield return Frames();

        Assert.IsFalse(link.ClassListContains(TweenClipSections.ICON_BUTTON_ON_USS_CLASS_NAME));
    }

    [UnityTest]
    public IEnumerator Pipette_IsDisabledInRelativeMode()
    {
        Create(typeof(GraphicClip));

        var root = BuildInHost();
        yield return Frames();

        var serialized = _editor.serializedObject;
        var path = $"{TweenClipModel.TEMPLATE}._color.{TweenClipModel.MODE}";

        serialized.Update();
        serialized.FindProperty(path).enumValueIndex = (int)ValueMode.Relative;
        serialized.ApplyModifiedProperties();
        yield return Frames();

        var relative = root.Query<Button>()
            .ToList()
            .Where(button => button.tooltip == TweenClipTexts.PIPETTE_RELATIVE_TOOLTIP)
            .ToList();

        Assert.AreEqual(2, relative.Count, "the from and to pipettes of the color card");
        Assert.IsTrue(relative.All(button => button.enabledSelf == false));
    }

    [Test]
    public void BindingValueReader_ReadsTheCurrentValues()
    {
        _gameObject = new GameObject("Bound", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));

        var rect = (RectTransform)_gameObject.transform;
        rect.anchoredPosition3D = new Vector3(10f, 20f, 30f);
        rect.localScale = new Vector3(2f, 3f, 4f);
        rect.localEulerAngles = new Vector3(0f, 0f, 270f);

        var image = _gameObject.GetComponent<Image>();
        image.color = new Color(0.1f, 0.2f, 0.3f, 0.4f);
        image.fillAmount = 0.25f;

        _gameObject.GetComponent<CanvasGroup>().alpha = 0.6f;

        Assert.IsTrue(BindingValueReader.TryRead("_anchoredPosition", rect, out var position));
        Assert.AreEqual(new Vector3(10f, 20f, 30f), (Vector3)position);

        Assert.IsTrue(BindingValueReader.TryRead("_scale", rect, out var scale));
        Assert.AreEqual(new Vector3(2f, 3f, 4f), (Vector3)scale);

        Assert.IsTrue(BindingValueReader.TryRead("_rotation", rect, out var rotation));
        Assert.AreEqual(-90f, rotation.z, 0.001f, "270 degrees reads as -90");

        Assert.IsTrue(BindingValueReader.TryRead("_color", image, out var color));
        Assert.AreEqual(0.4f, color.w, 0.0001f);

        Assert.IsTrue(BindingValueReader.TryRead("_fillAmount", image, out var fill));
        Assert.AreEqual(0.25f, fill.x, 0.0001f);

        Assert.IsTrue(BindingValueReader.TryRead("_alpha", image, out var imageAlpha));
        Assert.AreEqual(0.4f, imageAlpha.x, 0.0001f, "a Graphic binding reads the Graphic alpha");

        var group = _gameObject.GetComponent<CanvasGroup>();

        Assert.IsTrue(BindingValueReader.TryRead("_alpha", group, out var groupAlpha));
        Assert.AreEqual(0.6f, groupAlpha.x, 0.0001f);

        Assert.IsFalse(BindingValueReader.TryRead("_unknown", rect, out _));
        Assert.IsFalse(BindingValueReader.TryRead("_scale", null, out _));
    }

    [Test]
    public void IsNormalized_DependsOnTheClipForASharedChannelName()
    {
        Assert.IsTrue(ChannelPresentation.IsNormalized(typeof(ImageClip), "_alpha"));
        Assert.IsTrue(ChannelPresentation.IsNormalized(typeof(ImageClip), "_fillAmount"));
        Assert.IsTrue(ChannelPresentation.IsNormalized(typeof(ScrollbarClip), "_value"));
        Assert.IsTrue(ChannelPresentation.IsNormalized(typeof(ScrollbarClip), "_size"));
        Assert.IsFalse(ChannelPresentation.IsNormalized(typeof(SliderClip), "_value"));
        Assert.IsFalse(ChannelPresentation.IsNormalized(typeof(ScrollRectClip), "_normalizedPosition"));
        Assert.IsFalse(ChannelPresentation.IsNormalized(typeof(RectTransformClip), "_scale"));
    }

    [Test]
    public void Inspector_DrawsASliderForAScrollbarValueAndAFloatFieldForASliderValue()
    {
        Create(typeof(ScrollbarClip));

        var scrollbarRoot = _editor.CreateInspectorGUI();

        var scrollbarSliders = scrollbarRoot.Query<UnityEngine.UIElements.Slider>().ToList();

        Assert.AreEqual(4, scrollbarSliders.Count, "value and size, from and to");

        Object.DestroyImmediate(_editor);
        Object.DestroyImmediate(_clip);

        Create(typeof(SliderClip));

        var sliderRoot = _editor.CreateInspectorGUI();

        Assert.AreEqual(0, sliderRoot.Query<UnityEngine.UIElements.Slider>().ToList().Count);
        Assert.AreEqual(2, sliderRoot.Query<FloatField>().ToList().Count, "from and to");
    }

    [Test]
    public void ScrollRectInspector_HasTheXAndYAxes()
    {
        Create(typeof(ScrollRectClip));

        var root = _editor.CreateInspectorGUI();
        var axes = root.Query<Button>(className: TweenClipSections.AXIS_BUTTON_USS_CLASS_NAME).ToList();

        Assert.AreEqual(2, axes.Count);

        var channels = TweenClipModel.Create(_editor.serializedObject).Channels;

        Assert.AreEqual(ChannelValueKind.Vector2, Find(channels, "_normalizedPosition").ValueKind);
    }

    [Test]
    public void BindingValueReader_ReadsTheControls()
    {
        // Both are Selectables, which allow one per object: the bound object is the control itself.
        _gameObject = new GameObject("Slider", typeof(RectTransform), typeof(Slider));

        var barObject = new GameObject("Scrollbar", typeof(RectTransform), typeof(Scrollbar));
        barObject.transform.SetParent(_gameObject.transform, false);

        var slider = _gameObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 10f;
        slider.value = 4f;

        var bar = barObject.GetComponent<Scrollbar>();
        bar.value = 0.3f;
        bar.size = 0.6f;

        Assert.IsTrue(BindingValueReader.TryRead("_value", slider, out var sliderValue));
        Assert.AreEqual(4f, sliderValue.x, 0.0001f, "a Slider binding reads the Slider");

        Assert.IsTrue(BindingValueReader.TryRead("_value", bar, out var barValue));
        Assert.AreEqual(0.3f, barValue.x, 0.0001f, "a Scrollbar binding reads the Scrollbar");

        Assert.IsTrue(BindingValueReader.TryRead("_size", bar, out var size));
        Assert.AreEqual(0.6f, size.x, 0.0001f);
    }

    [Test]
    public void RawImageInspector_CaptionsTheUvRectAxesAsXYWH()
    {
        Create(typeof(RawImageClip));

        var root = _editor.CreateInspectorGUI();
        var axes = root.Query<Button>(className: TweenClipSections.AXIS_BUTTON_USS_CLASS_NAME).ToList();

        Assert.AreEqual(4, axes.Count);
        Assert.AreEqual("X,Y,W,H", string.Join(",", axes.Select(axis => axis.text)));

        var channels = TweenClipModel.Create(_editor.serializedObject).Channels;

        Assert.AreEqual(ChannelValueKind.Vector4, Find(channels, "_uvRect").ValueKind);
    }

    [Test]
    public void LayoutElementCard_NotesANegativeEndOnlyWhenTheChannelUsesIt()
    {
        Create(typeof(LayoutElementClip));

        var clip = (LayoutElementClip)_clip;
        clip.Template.PreferredWidth = new TweenChannel<float>(-1f, 100f);
        clip.Template.PreferredHeight = new TweenChannel<float>(0f, 100f);
        clip.Template.MinWidth = new TweenChannel<float>(-1f, 100f, ValueMode.Relative);
        clip.Template.MinHeight = new TweenChannel<float>(-1f, 100f, ValueMode.FromCurrent);

        var serializedObject = new SerializedObject(_clip);
        var channels = TweenClipModel.Create(serializedObject).Channels;

        Assert.AreEqual(TweenClipTexts.NEGATIVE_UNSET_NOTE, GetNoteText(serializedObject, channels, "_preferredWidth"));
        Assert.AreEqual(string.Empty, GetNoteText(serializedObject, channels, "_preferredHeight"));
        Assert.IsTrue(GetNoteText(serializedObject, channels, "_minWidth").Contains("Offsets"));
        Assert.IsFalse(GetNoteText(serializedObject, channels, "_minWidth").Contains("not set"), "relative offsets");
        Assert.IsFalse(GetNoteText(serializedObject, channels, "_minHeight").Contains("not set"), "from is unused");

        clip.Template.MinHeight = new TweenChannel<float>(0f, -1f, ValueMode.FromCurrent);
        serializedObject.Update();

        var fromCurrent = GetNoteText(serializedObject, channels, "_minHeight");

        Assert.IsTrue(fromCurrent.Contains("Starts from"));
        Assert.IsTrue(fromCurrent.Contains(TweenClipTexts.NEGATIVE_UNSET_NOTE));
    }

    [Test]
    public void OtherClips_NeverNoteANegativeEnd()
    {
        Create(typeof(SliderClip));

        var clip = (SliderClip)_clip;
        clip.Template.Value = new TweenChannel<float>(-1f, -5f);

        var serializedObject = new SerializedObject(_clip);
        var channels = TweenClipModel.Create(serializedObject).Channels;

        Assert.AreEqual(string.Empty, GetNoteText(serializedObject, channels, "_value"));
    }

    [Test]
    public void BindingValueReader_ReadsALayoutElement()
    {
        _gameObject = new GameObject("Item", typeof(RectTransform), typeof(LayoutElement));

        var element = _gameObject.GetComponent<LayoutElement>();
        element.minWidth = 1f;
        element.minHeight = 2f;
        element.preferredWidth = 3f;
        element.preferredHeight = 4f;
        element.flexibleWidth = 5f;
        element.flexibleHeight = 6f;

        var expected = new (string Name, float Value)[] {
            ("_minWidth", 1f),
            ("_minHeight", 2f),
            ("_preferredWidth", 3f),
            ("_preferredHeight", 4f),
            ("_flexibleWidth", 5f),
            ("_flexibleHeight", 6f),
        };

        foreach (var (name, value) in expected)
        {
            Assert.IsTrue(BindingValueReader.TryRead(name, element, out var read), name);
            Assert.AreEqual(value, read.x, 0.0001f, name);
        }

        var plain = new GameObject("Plain", typeof(RectTransform));

        try
        {
            Assert.IsFalse(BindingValueReader.TryRead("_minWidth", plain, out _));
        }
        finally
        {
            Object.DestroyImmediate(plain);
        }
    }

    [Test]
    public void BindingValueReader_ReadsTheUvRectAndTheShadow()
    {
        _gameObject = new GameObject("Raw", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));

        var rawImage = _gameObject.GetComponent<RawImage>();
        rawImage.uvRect = new Rect(0.1f, 0.2f, 0.3f, 0.4f);

        Assert.IsTrue(BindingValueReader.TryRead("_uvRect", rawImage, out var uv));
        Assert.AreEqual(new Vector4(0.1f, 0.2f, 0.3f, 0.4f), uv);

        var shadow = _gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.1f, 0.2f, 0.3f, 0.4f);
        shadow.effectDistance = new Vector2(3f, -5f);

        Assert.IsTrue(BindingValueReader.TryRead("_effectColor", shadow, out var color));
        Assert.AreEqual(0.4f, color.w, 0.0001f);

        Assert.IsTrue(BindingValueReader.TryRead("_effectDistance", shadow, out var distance));
        Assert.AreEqual(3f, distance.x, 0.0001f);
        Assert.AreEqual(-5f, distance.y, 0.0001f);
    }

    [Test]
    public void BindingValueReader_ReadsTheScrollPositionOnlyWithAContent()
    {
        _gameObject = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));

        var scrollRect = _gameObject.GetComponent<ScrollRect>();

        Assert.IsFalse(BindingValueReader.TryRead("_normalizedPosition", scrollRect, out _), "no content");

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(_gameObject.transform, false);
        scrollRect.content = (RectTransform)content.transform;

        Assert.IsTrue(BindingValueReader.TryRead("_normalizedPosition", scrollRect, out _));
    }

    [Test]
    public void TmpTextClip_DrawsTheGradientAsAGridOfFourColorFieldsWithoutAxes()
    {
        Create(typeof(TmpTextClip));

        var serializedObject = new SerializedObject(_clip);
        var channels = TweenClipModel.Create(serializedObject).Channels;

        Assert.AreEqual(ChannelValueKind.VertexGradient, Find(channels, "_colorGradient").ValueKind);
        Assert.AreEqual(ChannelValueKind.Float, Find(channels, "_visibleRatio").ValueKind);

        var card = ChannelCardSection.Create(serializedObject, Find(channels, "_colorGradient"), new ClipBindingSource(_clip));

        // From and to, four corners each, and no axes row.
        Assert.AreEqual(8, card.Query<ColorField>().ToList().Count);
        Assert.AreEqual(4, card.Query<VisualElement>(className: TweenClipSections.GRADIENT_ROW_USS_CLASS_NAME).ToList().Count);
        Assert.AreEqual(0, card.Query<Button>(className: TweenClipSections.AXIS_BUTTON_USS_CLASS_NAME).ToList().Count);
    }

    [Test]
    public void TmpTextClip_DrawsTheVisibleRatioAsASlider()
    {
        Create(typeof(TmpTextClip));

        var serializedObject = new SerializedObject(_clip);
        var channels = TweenClipModel.Create(serializedObject).Channels;
        var card = ChannelCardSection.Create(serializedObject, Find(channels, "_visibleRatio"), new ClipBindingSource(_clip));

        Assert.AreEqual(2, card.Query<UnityEngine.UIElements.Slider>().ToList().Count);
    }

    [Test]
    public void TmpTextClip_VisibleRatioCardHasUnitAndPause()
    {
        Create(typeof(TmpTextClip));

        var serializedObject = new SerializedObject(_clip);
        var channels = TweenClipModel.Create(serializedObject).Channels;
        var source = new ClipBindingSource(_clip);
        var card = ChannelCardSection.Create(serializedObject, Find(channels, "_visibleRatio"), source);

        var unit = card.Query<EnumField>().ToList().Single(f => f.tooltip == TweenClipTexts.TYPEWRITER_UNIT_TOOLTIP);
        var pause = card.Query<FloatField>().ToList().Single(f => f.tooltip == TweenClipTexts.TYPEWRITER_PAUSE_TOOLTIP);

        Assert.AreEqual("_template._visibleUnit", unit.bindingPath);
        Assert.AreEqual("_template._punctuationPause", pause.bindingPath);

        // The fields are siblings of the channel, not channels, and no other card shows them.
        Assert.IsFalse(channels.Any(channel => channel.Name == "_visibleUnit" || channel.Name == "_punctuationPause"));

        var alpha = ChannelCardSection.Create(serializedObject, Find(channels, "_alpha"), source);

        Assert.IsFalse(alpha.Query<EnumField>().ToList().Any(f => f.tooltip == TweenClipTexts.TYPEWRITER_UNIT_TOOLTIP));
    }

    [Test]
    public void TmpTextClip_DrawsTheTextChannelAsATextCard()
    {
        Create(typeof(TmpTextClip));

        var serializedObject = new SerializedObject(_clip);
        var channels = TweenClipModel.Create(serializedObject).Channels;
        var info = Find(channels, "_text");

        Assert.AreEqual(ChannelShape.Text, info.Shape);

        var card = TextChannelCardSection.Create(serializedObject, info, new ClipBindingSource(_clip));

        // From, to and the custom characters.
        Assert.AreEqual(3, card.Query<TextField>().ToList().Count);
        Assert.AreEqual(1, card.Query<EnumField>().ToList().Count);
        Assert.AreEqual(2, card.Query<Button>(className: TweenClipSections.ICON_BUTTON_USS_CLASS_NAME).ToList().Count);
    }

    [Test]
    public void BindingValueReader_ReadsTheTextOfATmpText()
    {
        _gameObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));

        var text = _gameObject.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = "LEVEL 1";

        Assert.IsTrue(BindingValueReader.TryReadText(text, out var value));
        Assert.AreEqual("LEVEL 1", value);
        Assert.IsFalse(BindingValueReader.TryReadText(null, out _));
    }

    [Test]
    public void BindingValueReader_ReadsATmpText()
    {
        _gameObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));

        var text = _gameObject.AddComponent<TMPro.TextMeshProUGUI>();
        text.fontSize = 20f;
        text.characterSpacing = 1f;
        text.wordSpacing = 2f;
        text.lineSpacing = 3f;
        text.paragraphSpacing = 4f;
        text.colorGradient = new TMPro.VertexGradient(Color.red, Color.green, Color.blue, Color.black);

        var expected = new (string Name, float Value)[] {
            ("_fontSize", 20f),
            ("_characterSpacing", 1f),
            ("_wordSpacing", 2f),
            ("_lineSpacing", 3f),
            ("_paragraphSpacing", 4f),
            ("_visibleRatio", 1f),
        };

        foreach (var (name, value) in expected)
        {
            Assert.IsTrue(BindingValueReader.TryRead(name, text, out var read), name);
            Assert.AreEqual(value, read.x, 0.0001f, name);
        }

        Assert.IsTrue(BindingValueReader.TryReadGradient(text, out var gradient));
        Assert.AreEqual(Color.red, gradient.topLeft);
        Assert.AreEqual(Color.green, gradient.topRight);
        Assert.AreEqual(Color.blue, gradient.bottomLeft);
        Assert.AreEqual(Color.black, gradient.bottomRight);
        Assert.IsTrue(BindingValueReader.CanRead("_colorGradient", _gameObject));
        Assert.IsTrue(BindingValueReader.TryRead("_alpha", text, out _));
        Assert.IsTrue(BindingValueReader.TryRead("_color", text, out _));
    }

    [Test]
    public void BindingValueReader_NotesAGradientOnlyWhileItIsSwitchedOff()
    {
        _gameObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));

        var text = _gameObject.AddComponent<TMPro.TextMeshProUGUI>();

        Assert.IsTrue(BindingValueReader.IsVertexGradientOff(text));

        text.enableVertexGradient = true;

        Assert.IsFalse(BindingValueReader.IsVertexGradientOff(text));
        Assert.IsFalse(BindingValueReader.IsVertexGradientOff(null));
        Assert.IsFalse(BindingValueReader.TryReadGradient(null, out _));
    }

    [Test]
    public void BindingValueReader_NotesAutoSizeOnlyWhileItIsOn()
    {
        _gameObject = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer));

        var text = _gameObject.AddComponent<TMPro.TextMeshProUGUI>();

        Assert.IsFalse(BindingValueReader.IsAutoSizing(text));

        text.enableAutoSizing = true;

        Assert.IsTrue(BindingValueReader.IsAutoSizing(text));
        Assert.IsTrue(BindingValueReader.IsAutoSizing(_gameObject));
        Assert.IsFalse(BindingValueReader.IsAutoSizing(null));
    }

    private static ChannelInfo Find(System.Collections.Generic.IReadOnlyList<ChannelInfo> channels, string name)
        => channels.First(channel => channel.Name == name);

    [Test]
    public void Vector4Channel_DrawsFourAxesAndVector4Fields()
    {
        _clip = ScriptableObject.CreateInstance<Vector4TestClip>();

        var serializedObject = new SerializedObject(_clip);
        var model = TweenClipModel.Create(serializedObject);

        Assert.AreEqual(1, model.Channels.Count);
        Assert.AreEqual(ChannelValueKind.Vector4, model.Channels[0].ValueKind);

        var card = ChannelCardSection.Create(serializedObject, model.Channels[0], new ClipBindingSource(_clip));
        var axes = card.Query<Button>(className: TweenClipSections.AXIS_BUTTON_USS_CLASS_NAME).ToList();

        Assert.AreEqual(2, card.Query<Vector4Field>().ToList().Count);
        Assert.AreEqual(4, axes.Count);
        Assert.IsTrue(axes[3].ClassListContains(TweenClipSections.AXIS_BUTTON_W_USS_CLASS_NAME));
        // The test channel is named _uvRect, so its components are captioned X/Y/W/H.
        Assert.AreEqual("H", axes[3].text);
    }

    private static string GetNoteText(
          SerializedObject serializedObject
        , System.Collections.Generic.IReadOnlyList<ChannelInfo> channels
        , string name
    )
    {
        var source = new ClipBindingSource(serializedObject.targetObject);
        var card = ChannelCardSection.Create(serializedObject, Find(channels, name), source);
        var note = card.Q<Label>(className: TweenClipSections.NOTE_USS_CLASS_NAME);

        return note.style.display == DisplayStyle.None ? string.Empty : note.text;
    }

    private void Create(Type clipType)
    {
        _clip = ScriptableObject.CreateInstance(clipType);
        _editor = UnityEditor.Editor.CreateEditor(_clip);
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

    private sealed class HostWindow : EditorWindow
    {
    }
}
