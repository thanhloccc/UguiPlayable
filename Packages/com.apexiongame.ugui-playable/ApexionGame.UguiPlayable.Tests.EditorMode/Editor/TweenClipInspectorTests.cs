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
using ApexionGame.UguiPlayable.Tracks.Graphics;
using ApexionGame.UguiPlayable.Tracks.Groups;
using ApexionGame.UguiPlayable.Tracks.Transforms;
using Button = UnityEngine.UIElements.Button;
using Image = UnityEngine.UI.Image;
using Object = UnityEngine.Object;
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
        , "_anchoredPosition,_sizeDelta,_rotation,_scale,_pivot,_anchorMin,_anchorMax"
    )]
    [TestCase(typeof(GraphicClip), "_color,_alpha")]
    [TestCase(typeof(ImageClip), "_color,_alpha,_fillAmount")]
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

        Assert.IsTrue(channels.All(channel => channel.Shape == ChannelShape.Tween));
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

    private static ChannelInfo Find(System.Collections.Generic.IReadOnlyList<ChannelInfo> channels, string name)
        => channels.First(channel => channel.Name == name);

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
