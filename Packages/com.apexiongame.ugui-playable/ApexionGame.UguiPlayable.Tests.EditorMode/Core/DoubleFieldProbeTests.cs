using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Tracks.Text;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Core;

/// <summary>
/// Probes what UnityEditor does with a <see cref="double"/> field, because the counter editor card relies on it:
/// the property type, the YAML round trip and <see cref="DoubleField"/> binding. The probe runs on
/// <see cref="CounterChannel"/> itself (<c>_from</c> and <c>_to</c> are doubles).
/// </summary>
public sealed class DoubleFieldProbeTests
{
    private const string FROM_PATH = "_template._counter._from";
    private const string TO_PATH = "_template._counter._to";

    private const int FRAMES = 3;

    private TmpTextClip _clip;
    private EditorWindow _window;
    private string _file;

    [SetUp]
    public void SetUp()
    {
        _clip = ScriptableObject.CreateInstance<TmpTextClip>();
        _file = Path.Combine(Path.GetTempPath(), "ugp-double-probe-" + System.Guid.NewGuid().ToString("N") + ".asset");
    }

    [TearDown]
    public void TearDown()
    {
        if (_window != null)
        {
            _window.Close();
        }

        if (_clip != null)
        {
            Object.DestroyImmediate(_clip);
        }

        if (File.Exists(_file))
        {
            File.Delete(_file);
        }
    }

    [Test]
    public void DoubleField_ReportsTheFloatPropertyType()
    {
        var serialized = new SerializedObject(_clip);

        Assert.AreEqual(SerializedPropertyType.Float, serialized.FindProperty(FROM_PATH).propertyType);
        Assert.AreEqual(SerializedPropertyType.Float, serialized.FindProperty(TO_PATH).propertyType);
    }

    [Test]
    public void DoubleField_KeepsTheFullValueThroughSerialization()
    {
        var serialized = new SerializedObject(_clip);

        serialized.FindProperty(FROM_PATH).doubleValue = 1234567890123.0d;
        serialized.FindProperty(TO_PATH).doubleValue = 1234567.891d;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        Assert.AreEqual(1234567890123.0d, _clip.Template.Counter.From);
        Assert.AreEqual(1234567.891d, _clip.Template.Counter.To);

        // Through a YAML file and back, as an asset on disk.
        UnityEditorInternal.InternalEditorUtility.SaveToSerializedFileAndForget(new Object[] { _clip }, _file, true);

        var yaml = File.ReadAllText(_file);

        StringAssert.Contains("1234567890123", yaml);
        StringAssert.Contains("1234567.891", yaml);

        var loaded = UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(_file);
        TmpTextClip reloaded = null;

        foreach (var obj in loaded)
        {
            reloaded ??= obj as TmpTextClip;
        }

        try
        {
            Assert.IsNotNull(reloaded);
            Assert.AreEqual(1234567890123.0d, reloaded.Template.Counter.From);
            Assert.AreEqual(1234567.891d, reloaded.Template.Counter.To);
        }
        finally
        {
            foreach (var obj in loaded)
            {
                if (obj != null)
                {
                    Object.DestroyImmediate(obj, true);
                }
            }
        }
    }

    [UnityTest]
    public IEnumerator DoubleField_BindsToTheProperty()
    {
        var serialized = new SerializedObject(_clip);

        serialized.FindProperty(TO_PATH).doubleValue = 1234567.891d;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        var field = new DoubleField { bindingPath = TO_PATH };

        _window = ScriptableObject.CreateInstance<HostWindow>();
        _window.Show();
        _window.rootVisualElement.Add(field);
        _window.rootVisualElement.Bind(serialized);

        for (var i = 0; i < FRAMES; i++)
        {
            yield return null;
        }

        Assert.AreEqual(1234567.891d, field.value);

        // Editing the field writes the property back.
        field.value = 9876543210.5d;

        for (var i = 0; i < FRAMES; i++)
        {
            yield return null;
        }

        serialized.Update();
        Assert.AreEqual(9876543210.5d, serialized.FindProperty(TO_PATH).doubleValue);
        Assert.AreEqual(9876543210.5d, _clip.Template.Counter.To);
    }

    private sealed class HostWindow : EditorWindow
    {
    }
}
