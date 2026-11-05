using System;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Editor.Clips
{
    /// <summary>
    /// Binds a vector field to a vector property of one clip or of several selected clips. An edit writes only the
    /// axes that changed, so each clip keeps its other axes; a field whose axis differs between the clips shows the
    /// mixed dash on that axis. The optional constrain rule scales the other axes of each clip from that clip's own
    /// base vector.
    /// </summary>
    /// <remarks>
    /// The field is not bound with <c>BindProperty</c>, which would write the whole vector and so overwrite the axes
    /// the clips do not share. A constrained edit writes through one <see cref="SerializedObject"/> per clip (see
    /// <see cref="MultiEdit.WriteEach"/>); any other edit writes the axis sub-properties through the inspector's own
    /// multi-object <see cref="SerializedObject"/>.
    /// </remarks>
    internal sealed class VectorFieldBinder
    {
        private const long MIXED_POLL_MILLISECONDS = 250;

        private static readonly string[] s_axisNames = { "x", "y", "z", "w" };

        private readonly SerializedObject _serializedObject;
        private readonly SerializedProperty _value;
        private readonly SerializedProperty _constrain;
        private readonly SerializedProperty _ignoredAxes;
        private readonly int _dimension;
        private readonly VisualElement _field;
        private readonly Action<Vector4> _setValue;
        private readonly Edit _edit = new();

        private VectorFieldBinder(
              SerializedProperty value
            , SerializedProperty constrain
            , SerializedProperty ignoredAxes
            , int dimension
            , VisualElement field
            , Action<Vector4> setValue
        )
        {
            _serializedObject = value.serializedObject;
            _value = value;
            _constrain = constrain;
            _ignoredAxes = ignoredAxes;
            _dimension = dimension;
            _field = field;
            _setValue = setValue;
        }

        public static void Bind(
              Vector2Field field
            , SerializedProperty value
            , SerializedProperty constrain = null
            , SerializedProperty ignoredAxes = null
        )
        {
            var binder = new VectorFieldBinder(value, constrain, ignoredAxes, 2, field, v => field.SetValueWithoutNotify(v));

            field.RegisterValueChangedCallback(evt => binder.OnChanged(evt.previousValue, evt.newValue));
            binder.Start();
        }

        public static void Bind(
              Vector3Field field
            , SerializedProperty value
            , SerializedProperty constrain = null
            , SerializedProperty ignoredAxes = null
        )
        {
            var binder = new VectorFieldBinder(value, constrain, ignoredAxes, 3, field, v => field.SetValueWithoutNotify(v));

            field.RegisterValueChangedCallback(evt => binder.OnChanged(evt.previousValue, evt.newValue));
            binder.Start();
        }

        public static void Bind(Vector4Field field, SerializedProperty value)
        {
            var binder = new VectorFieldBinder(value, null, null, 4, field, v => field.SetValueWithoutNotify(v));

            field.RegisterValueChangedCallback(evt => binder.OnChanged(evt.previousValue, evt.newValue));
            binder.Start();
        }

        /// <summary>
        /// Shows the mixed dash on every axis whose value differs between the selected clips.
        /// </summary>
        public void RefreshMixed()
        {
            _serializedObject.UpdateIfRequiredOrScript();

            var index = 0;

            _field.Query<FloatField>().ForEach(component => {
                if (index < _dimension)
                {
                    component.showMixedValue = _value.FindPropertyRelative(s_axisNames[index]).hasMultipleDifferentValues;
                }

                index++;
            });
        }

        private void Start()
        {
            Refresh();

            _field.RegisterCallback<FocusInEvent>(_ => _edit.Focus(ReadAll), TrickleDown.TrickleDown);
            _field.RegisterCallback<FocusOutEvent>(_ => _edit.Blur(_field), TrickleDown.TrickleDown);
            _field.RegisterCallback<PointerDownEvent>(_ => _edit.Press(ReadAll), TrickleDown.TrickleDown);
            _field.RegisterCallback<PointerUpEvent>(_ => _edit.Release(_field), TrickleDown.TrickleDown);
            _field.RegisterCallback<PointerCaptureOutEvent>(_ => _edit.Release(_field), TrickleDown.TrickleDown);

            _field.TrackPropertyValue(_value, _ => Refresh());

            // The first target's value can stay the same while another target's changes, so the dash is polled.
            if (_serializedObject.isEditingMultipleObjects)
            {
                _field.schedule.Execute(RefreshMixed).Every(MIXED_POLL_MILLISECONDS);
            }
        }

        private void Refresh()
        {
            _serializedObject.UpdateIfRequiredOrScript();
            _setValue(Read(_value, _dimension));
            RefreshMixed();
        }

        private void OnChanged(Vector4 previous, Vector4 next)
        {
            _serializedObject.Update();

            var changed = 0;

            for (var i = 0; i < _dimension; i++)
            {
                if (previous[i] != next[i])
                {
                    changed |= 1 << i;
                }
            }

            if (changed == 0)
            {
                return;
            }

            if (IsAnyConstrained())
            {
                WriteConstrained(changed, next);
            }
            else
            {
                for (var i = 0; i < _dimension; i++)
                {
                    if ((changed & (1 << i)) != 0)
                    {
                        _value.FindPropertyRelative(s_axisNames[i]).floatValue = next[i];
                    }
                }

                _serializedObject.ApplyModifiedProperties();
            }

            Refresh();
        }

        private bool IsAnyConstrained()
            => _constrain != null && (_constrain.hasMultipleDifferentValues || _constrain.boolValue);

        // Each clip scales from its own vector, so the write is per clip.
        private void WriteConstrained(int changed, Vector4 next)
        {
            var path = _value.propertyPath;
            var dimension = _dimension;
            var current = MultiEdit.ReadEach(_serializedObject, path, p => Read(p, dimension));
            var constrained = MultiEdit.ReadEach(_serializedObject, _constrain.propertyPath, p => p.boolValue);
            var ignored = _ignoredAxes == null
                ? null
                : MultiEdit.ReadEach(_serializedObject, _ignoredAxes.propertyPath, p => p.enumValueFlag);

            MultiEdit.WriteEach(_serializedObject, (each, index) => {
                var target = current[index];

                for (var i = 0; i < dimension; i++)
                {
                    if ((changed & (1 << i)) != 0)
                    {
                        target[i] = next[i];
                    }
                }

                var result = target;

                if (constrained[index])
                {
                    var baseVector = _edit.IsActive && _edit.Base.Length == current.Length
                        ? _edit.Base[index]
                        : current[index];

                    var mask = ignored == null ? AxisMask.None : (AxisMask)ignored[index];

                    result = ConstrainMath.Apply(baseVector, current[index], target, mask, dimension);
                }

                Write(each.FindProperty(path), result, dimension);
            });
        }

        private Vector4[] ReadAll()
        {
            var dimension = _dimension;
            return MultiEdit.ReadEach(_serializedObject, _value.propertyPath, p => Read(p, dimension));
        }

        private static Vector4 Read(SerializedProperty property, int dimension)
            => dimension switch {
                2 => property.vector2Value,
                3 => property.vector3Value,
                _ => property.vector4Value,
            };

        private static void Write(SerializedProperty property, Vector4 value, int dimension)
        {
            switch (dimension)
            {
                case 2:
                    property.vector2Value = value;
                    break;

                case 3:
                    property.vector3Value = value;
                    break;

                default:
                    property.vector4Value = value;
                    break;
            }
        }

        // The vectors the clips had when the current edit started.
        private sealed class Edit
        {
            private bool _focused;
            private bool _pressed;

            public Vector4[] Base { get; private set; } = Array.Empty<Vector4>();

            public bool IsActive { get; private set; }

            public void Focus(Func<Vector4[]> read)
            {
                _focused = true;
                Begin(read);
            }

            public void Press(Func<Vector4[]> read)
            {
                _pressed = true;
                Begin(read);
            }

            public void Blur(VisualElement field)
            {
                _focused = false;
                End(field);
            }

            public void Release(VisualElement field)
            {
                _pressed = false;
                End(field);
            }

            private void Begin(Func<Vector4[]> read)
            {
                if (IsActive)
                {
                    return;
                }

                Base = read();
                IsActive = true;
            }

            // Deferred, so a value committed on blur still sees the base.
            private void End(VisualElement field)
            {
                field.schedule.Execute(() => {
                    if (_focused == false && _pressed == false)
                    {
                        IsActive = false;
                    }
                });
            }
        }
    }
}
