using System;
using System.Collections;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using ApexionGame.UguiPlayable.Easing;
using ApexionGame.UguiPlayable.Editor.Common;
using ApexionGame.UguiPlayable.Editor.Easing;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Editor;

public sealed class EaseSettingDrawerTests
{
    private const int FRAMES = 3;

    private EaseSettingHolder _holder;
    private SerializedObject _serializedObject;
    private HostWindow _window;

    [TearDown]
    public void TearDown()
    {
        if (_window.IsValid())
        {
            _window.Close();
        }

        _serializedObject?.Dispose();
        _serializedObject = null;

        if (_holder.IsValid())
        {
            Object.DestroyImmediate(_holder);
        }
    }

    [UnityTest]
    public IEnumerator Drawer_BuildsForEveryEase()
    {
        foreach (Ease ease in Enum.GetValues(typeof(Ease)))
        {
            Create(new EaseSetting(ease));

            var root = BuildDrawer();
            yield return Frames();

            Assert.IsNotNull(root.Q<EnumField>(), ease.ToString());
            Assert.IsNotNull(root.Q<CurveField>(), ease.ToString());
            Assert.IsNotNull(root.Q<EaseCurvePreview>(), ease.ToString());

            TearDown();
        }
    }

    [UnityTest]
    public IEnumerator CurveField_IsShownOnlyForCustom()
    {
        Create(new EaseSetting(Ease.OutQuad));

        var root = BuildDrawer();
        yield return Frames();

        var easeField = root.Q<EnumField>();
        var curveField = root.Q<CurveField>();

        Assert.AreEqual(DisplayStyle.None, curveField.style.display.value);

        easeField.value = Ease.Custom;
        yield return Frames();

        Assert.AreEqual(DisplayStyle.Flex, curveField.style.display.value);

        easeField.value = Ease.InOutCubic;
        yield return Frames();

        Assert.AreEqual(DisplayStyle.None, curveField.style.display.value);
    }

    [UnityTest]
    public IEnumerator SwitchingToCustom_CreatesACurveWhenMissing()
    {
        Create(new EaseSetting(Ease.Linear));
        Assert.IsNull(_holder.Setting.Curve);

        var root = BuildDrawer();
        yield return Frames();

        root.Q<EnumField>().value = Ease.Custom;
        yield return Frames();

        Assert.AreEqual(Ease.Custom, _holder.Setting.Ease);
        Assert.IsNotNull(_holder.Setting.Curve);
        Assert.Greater(_holder.Setting.Curve.length, 1);
    }

    [UnityTest]
    public IEnumerator Change_IsOneUndoStep()
    {
        Create(new EaseSetting(Ease.Linear));

        var root = BuildDrawer();
        yield return Frames();

        Undo.IncrementCurrentGroup();
        root.Q<EnumField>().value = Ease.OutQuad;
        yield return Frames();

        Assert.AreEqual(Ease.OutQuad, _holder.Setting.Ease);

        Undo.PerformUndo();
        yield return Frames();

        Assert.AreEqual(Ease.Linear, _holder.Setting.Ease);
    }

    [UnityTest]
    public IEnumerator SwitchToCustom_UndoRestoresEaseAndCurve()
    {
        Create(new EaseSetting(Ease.Linear));

        var root = BuildDrawer();
        yield return Frames();

        Undo.IncrementCurrentGroup();
        root.Q<EnumField>().value = Ease.Custom;
        yield return Frames();

        Assert.AreEqual(Ease.Custom, _holder.Setting.Ease);
        Assert.IsNotNull(_holder.Setting.Curve);

        Undo.PerformUndo();
        yield return Frames();

        Assert.AreEqual(Ease.Linear, _holder.Setting.Ease, "one undo reverts the ease");
        Assert.That(_holder.Setting.Curve == null || _holder.Setting.Curve.length == 0, "and the created curve");
    }

    [UnityTest]
    public IEnumerator Customize_IsShownOnlyForParametricFamilies()
    {
        foreach (Ease ease in Enum.GetValues(typeof(Ease)))
        {
            Create(new EaseSetting(ease));

            var root = BuildDrawer();
            yield return Frames();

            var toggle = root.Q<Toggle>();
            var expected = EaseSetting.IsParametric(ease) ? DisplayStyle.Flex : DisplayStyle.None;

            Assert.IsNotNull(toggle, ease.ToString());
            Assert.AreEqual(expected, toggle.style.display.value, ease.ToString());

            TearDown();
        }
    }

    [UnityTest]
    public IEnumerator Customize_On_WritesDefaultsInOneUndoStep()
    {
        Create(new EaseSetting(Ease.InOutElastic));

        var root = BuildDrawer();
        yield return Frames();

        var floats = root.Query<FloatField>().ToList();

        Assert.AreEqual(DisplayStyle.None, floats[0].style.display.value);

        Undo.IncrementCurrentGroup();
        root.Q<Toggle>().value = true;
        yield return Frames();

        Assert.IsTrue(_holder.Setting.Customize);
        Assert.AreEqual(1f, _holder.Setting.Strength);
        Assert.AreEqual(0.45f, _holder.Setting.Period);
        Assert.AreEqual(DisplayStyle.Flex, floats[0].style.display.value);

        Undo.PerformUndo();
        yield return Frames();

        Assert.IsFalse(_holder.Setting.Customize, "one undo reverts the switch");
        Assert.AreEqual(0f, _holder.Setting.Strength, "and the written parameters");
        Assert.AreEqual(0f, _holder.Setting.Period);
    }

    [Test]
    public void EditorIcons_ReturnsATextureForEveryIcon()
    {
        foreach (var name in EditorIcons.Names)
        {
            Assert.IsNotNull(EditorIcons.Get(name), name);
        }
    }

    [Test]
    public void EditorIcons_EveryIconLoadsAtBothScalesForBothSkins()
    {
        foreach (var name in EditorIcons.Names)
        {
            foreach (var dark in new[] { true, false })
            {
                foreach (var highDpi in new[] { false, true })
                {
                    var path = EditorIcons.GetPath(name, dark, highDpi);

                    Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Texture2D>(path), path);
                }
            }
        }
    }

    private void Create(EaseSetting setting)
    {
        _holder = ScriptableObject.CreateInstance<EaseSettingHolder>();
        _holder.Setting = setting;
        _serializedObject = new SerializedObject(_holder);
    }

    private VisualElement BuildDrawer()
    {
        var property = _serializedObject.FindProperty(nameof(EaseSettingHolder.Setting));
        var root = new EaseSettingDrawer().CreatePropertyGUI(property);

        _window = ScriptableObject.CreateInstance<HostWindow>();
        _window.Show();
        _window.rootVisualElement.Add(root);
        _window.rootVisualElement.Bind(_serializedObject);
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
