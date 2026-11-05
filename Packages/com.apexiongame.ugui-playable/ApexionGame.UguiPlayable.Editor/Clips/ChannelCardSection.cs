using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Editor.Common;

namespace ApexionGame.UguiPlayable.Editor.Clips
{
    /// <summary>
    /// The card of one <c>TweenChannel&lt;T&gt;</c>: an enable switch, icon, name, mode control and chevron in the
    /// header; axes, from/to rows and the ease in the body. A disabled channel is collapsed and dimmed.
    /// </summary>
    internal sealed class ChannelCardSection
    {
        private static readonly string[] s_modeLabels = { "Absolute", "Relative", "From current" };
        private static readonly string[] s_axisClasses = {
              TweenClipSections.AXIS_BUTTON_X_USS_CLASS_NAME
            , TweenClipSections.AXIS_BUTTON_Y_USS_CLASS_NAME
            , TweenClipSections.AXIS_BUTTON_Z_USS_CLASS_NAME
            , TweenClipSections.AXIS_BUTTON_W_USS_CLASS_NAME
        };

        private readonly SerializedObject _serializedObject;
        private readonly ChannelInfo _info;
        private readonly ClipBindingSource _source;
        private readonly string _expandKey;

        private readonly SerializedProperty _enabled;
        private readonly SerializedProperty _mode;
        private readonly SerializedProperty _ignoredAxes;

        private readonly VisualElement _card = new();
        private readonly VisualElement _body = new();
        private readonly Button _chevron;
        private readonly Image _chevronImage;
        private readonly Label _note;

        private readonly Button[] _axisButtons;
        private ToggleButtonGroup _axisGroup;
        private readonly ValueRow _fromRow;
        private readonly ValueRow _toRow;

        private ChannelCardSection(SerializedObject serializedObject, ChannelInfo info, ClipBindingSource source)
        {
            _serializedObject = serializedObject;
            _info = info;
            _source = source;

            var template = serializedObject.FindProperty(TweenClipModel.TEMPLATE);
            var channel = template.FindPropertyRelative(info.Name);

            _enabled = channel.FindPropertyRelative(TweenClipModel.ENABLED);
            _mode = channel.FindPropertyRelative(TweenClipModel.MODE);
            _ignoredAxes = channel.FindPropertyRelative(TweenClipModel.IGNORED_AXES);
            _expandKey = TweenClipSections.GetExpandKey(serializedObject.targetObject, channel.propertyPath);

            _card.AddToClassList(TweenClipSections.CARD_USS_CLASS_NAME);
            _body.AddToClassList(TweenClipSections.CARD_BODY_USS_CLASS_NAME);

            _chevron = TweenClipSections.CreateIconButton(
                  EditorIcons.ChevronDown
                , string.Empty
                , ToggleExpanded
                , out _chevronImage
            );

            _card.Add(CreateHeader());
            _card.Add(_body);

            var hasAxes = GetDimension(info.ValueKind) > 1;

            if (hasAxes)
            {
                _axisButtons = CreateAxisButtons();
                _body.Add(CreateAxesRow());
            }

            _note = TweenClipSections.CreateNote(string.Empty);
            _body.Add(_note);

            _fromRow = CreateValueRow(
                  TweenClipTexts.FROM
                , channel.FindPropertyRelative(TweenClipModel.FROM)
                , FindConstrain(template, true)
            );

            _toRow = CreateValueRow(
                  TweenClipTexts.TO
                , channel.FindPropertyRelative(TweenClipModel.TO)
                , FindConstrain(template, false)
            );

            _body.Add(_fromRow.Root);
            _body.Add(_toRow.Root);
            _body.Add(new PropertyField(channel.FindPropertyRelative(TweenClipModel.EASE)));

            _card.TrackPropertyValue(_enabled, _ => RefreshState());
            _card.TrackPropertyValue(_mode, _ => RefreshState());
            _card.TrackPropertyValue(_ignoredAxes, _ => RefreshState());
            TrackConstrain(_fromRow);
            TrackConstrain(_toRow);

            _source.Changed += UpdatePipettes;

            RefreshState();
            UpdatePipettes();
        }

        private void TrackConstrain(ValueRow row)
        {
            if (row.Constrain != null)
            {
                _card.TrackPropertyValue(row.Constrain, _ => RefreshState());
            }
        }

        public static VisualElement Create(
              SerializedObject serializedObject
            , ChannelInfo info
            , ClipBindingSource source
        )
            => new ChannelCardSection(serializedObject, info, source)._card;

        private bool IsExpanded
        {
            get => SessionState.GetBool(_expandKey, true);
            set => SessionState.SetBool(_expandKey, value);
        }

        private VisualElement CreateHeader()
        {
            var header = new VisualElement();
            header.AddToClassList(TweenClipSections.CARD_HEADER_USS_CLASS_NAME);

            var enabledToggle = new Toggle { tooltip = TweenClipTexts.ENABLED_TOOLTIP }.WithSwitchStyle();
            enabledToggle.BindProperty(_enabled);
            header.Add(enabledToggle);

            var icon = ChannelPresentation.GetIcon(_info.Name);

            if (icon != null)
            {
                var image = TweenClipSections.CreateIcon(icon, TweenClipSections.CARD_ICON_USS_CLASS_NAME);
                image.pickingMode = PickingMode.Position;
                image.RegisterCallback<ClickEvent>(_ => ToggleExpanded());
                header.Add(image);
            }

            var name = new Label(ChannelPresentation.GetDisplayName(_info.Name));
            name.AddToClassList(TweenClipSections.CARD_NAME_USS_CLASS_NAME);
            name.RegisterCallback<ClickEvent>(_ => ToggleExpanded());
            header.Add(name);

            header.Add(SegmentedControl.Create(s_modeLabels).BindEnum(_mode, s_modeLabels.Length));
            header.Add(_chevron);
            return header;
        }

        private Button[] CreateAxisButtons()
        {
            var count = GetDimension(_info.ValueKind);
            var captions = ChannelPresentation.GetAxisCaptions(_info.Name);
            var buttons = new Button[count];

            for (var i = 0; i < count; i++)
            {
                var button = new Button {
                    text = captions[i],
                    tooltip = TweenClipTexts.AXIS_TOOLTIP,
                };

                button.AddToClassList(TweenClipSections.AXIS_BUTTON_USS_CLASS_NAME);
                button.AddToClassList(s_axisClasses[i]);
                buttons[i] = button;
            }

            return buttons;
        }

        // The same Unity ToggleButtonGroup as the mode control, with multiple selection: a selected button is an
        // animated axis. The axis colour is only an accent (text and underline), so the row matches the header.
        private VisualElement CreateAxesRow()
        {
            var row = TweenClipSections.CreateRow(TweenClipTexts.AXES);

            _axisGroup = new ToggleButtonGroup {
                isMultipleSelection = true,
                allowEmptySelection = true,
            };

            CommonStyleSheetPaths.AddTo(_axisGroup);
            _axisGroup.AddToClassList(SegmentedControl.USS_CLASS_NAME);
            _axisGroup.AddToClassList(TweenClipSections.AXIS_GROUP_USS_CLASS_NAME);

            for (var i = 0; i < _axisButtons.Length; i++)
            {
                _axisGroup.Add(_axisButtons[i]);
            }

            _axisGroup.RegisterValueChangedCallback(evt => OnAxesChanged(evt.newValue));
            row.Add(_axisGroup);
            return row;
        }

        private static int GetDimension(ChannelValueKind kind)
            => kind switch {
                ChannelValueKind.Vector2 => 2,
                ChannelValueKind.Vector3 => 3,
                ChannelValueKind.Vector4 => 4,
                _ => 1,
            };

        private SerializedProperty FindConstrain(SerializedProperty template, bool isFrom)
            => template.FindPropertyRelative(TweenClipModel.GetConstrainName(_info.Name, isFrom));

        private ValueRow CreateValueRow(string label, SerializedProperty value, SerializedProperty constrain)
        {
            var row = TweenClipSections.CreateRow(label);
            row.Add(CreateValueField(value, constrain));

            Button link = null;
            Image linkImage = null;

            if (constrain != null)
            {
                link = TweenClipSections.CreateIconButton(
                      EditorIcons.Unlink
                    , TweenClipTexts.LINK_TOOLTIP
                    , () => OnLinkClicked(constrain)
                    , out linkImage
                );

                row.Add(link);
            }

            var pipette = TweenClipSections.CreateIconButton(
                  EditorIcons.Pipette
                , TweenClipTexts.PIPETTE_TOOLTIP
                , () => OnPipetteClicked(value)
                , out _
            );

            row.Add(pipette);
            return new ValueRow(row, link, linkImage, pipette, constrain);
        }

        private VisualElement CreateValueField(SerializedProperty value, SerializedProperty constrain)
        {
            VisualElement field;

            switch (_info.ValueKind)
            {
                case ChannelValueKind.Float when IsNormalized():
                {
                    var slider = new Slider(0f, 1f) { showInputField = true };
                    slider.BindProperty(value);
                    field = slider;
                    break;
                }

                case ChannelValueKind.Float:
                {
                    var floatField = new FloatField();
                    floatField.BindProperty(value);
                    field = floatField;
                    break;
                }

                case ChannelValueKind.Color:
                {
                    var colorField = new ColorField();
                    colorField.BindProperty(value);
                    field = colorField;
                    break;
                }

                case ChannelValueKind.Vector2:
                {
                    var vector2Field = new Vector2Field();
                    BindVector2(vector2Field, value, constrain);
                    field = vector2Field;
                    break;
                }

                case ChannelValueKind.Vector4:
                {
                    // No constrain rule for four components, so the field binds directly. The component labels
                    // follow the channel's captions (for example X/Y/W/H for a UV rect).
                    var vector4Field = new Vector4Field();
                    vector4Field.BindProperty(value);
                    ApplyCaptions(vector4Field);
                    field = vector4Field;
                    break;
                }

                default:
                {
                    var vector3Field = new Vector3Field();
                    BindVector3(vector3Field, value, constrain);
                    field = vector3Field;
                    break;
                }
            }

            field.AddToClassList(TweenClipSections.FIELD_USS_CLASS_NAME);
            return field;
        }

        // The clip type decides for channel names that two clips share (_value is 0..1 only on a scrollbar).
        private bool IsNormalized()
            => ChannelPresentation.IsNormalized(_serializedObject.targetObject.GetType(), _info.Name);

        private void ApplyCaptions(VisualElement vectorField)
        {
            var captions = ChannelPresentation.GetAxisCaptions(_info.Name);
            var index = 0;

            vectorField.Query<FloatField>().ForEach(component => {
                if (index < captions.Length)
                {
                    component.label = captions[index];
                }

                index++;
            });
        }

        // Vector fields are not bound directly: the constrain rule rewrites the other axes in the same write, so the
        // edit and the rewrite land in one Undo step. While an edit is in progress (the field has focus, or a label
        // is being dragged) the other axes scale from the vector the edit started with, so a transient 0 while typing
        // does not wipe them.
        private void BindVector2(Vector2Field field, SerializedProperty value, SerializedProperty constrain)
        {
            field.SetValueWithoutNotify(value.vector2Value);

            var edit = TrackEdit(field, () => value.vector2Value);

            field.RegisterValueChangedCallback(evt => {
                _serializedObject.Update();

                var result = (Vector2)Constrain(edit, evt.previousValue, evt.newValue, constrain, 2);
                value.vector2Value = result;
                _serializedObject.ApplyModifiedProperties();
                field.SetValueWithoutNotify(result);
            });

            field.TrackPropertyValue(value, p => field.SetValueWithoutNotify(p.vector2Value));
        }

        private void BindVector3(Vector3Field field, SerializedProperty value, SerializedProperty constrain)
        {
            field.SetValueWithoutNotify(value.vector3Value);

            var edit = TrackEdit(field, () => value.vector3Value);

            field.RegisterValueChangedCallback(evt => {
                _serializedObject.Update();

                var result = Constrain(edit, evt.previousValue, evt.newValue, constrain, 3);
                value.vector3Value = result;
                _serializedObject.ApplyModifiedProperties();
                field.SetValueWithoutNotify(result);
            });

            field.TrackPropertyValue(value, p => field.SetValueWithoutNotify(p.vector3Value));
        }

        private static VectorEdit TrackEdit(VisualElement field, System.Func<Vector3> read)
        {
            var edit = new VectorEdit();

            field.RegisterCallback<FocusInEvent>(_ => edit.Focus(read), TrickleDown.TrickleDown);
            field.RegisterCallback<FocusOutEvent>(_ => edit.Blur(field), TrickleDown.TrickleDown);
            field.RegisterCallback<PointerDownEvent>(_ => edit.Press(read), TrickleDown.TrickleDown);
            field.RegisterCallback<PointerUpEvent>(_ => edit.Release(field), TrickleDown.TrickleDown);
            field.RegisterCallback<PointerCaptureOutEvent>(_ => edit.Release(field), TrickleDown.TrickleDown);
            return edit;
        }

        private Vector3 Constrain(
              VectorEdit edit
            , Vector3 previous
            , Vector3 next
            , SerializedProperty constrain
            , int dimension
        )
        {
            if (constrain == null || constrain.boolValue == false)
            {
                return next;
            }

            var baseValue = edit.IsActive ? edit.Base : previous;

            return ConstrainMath.Apply(baseValue, previous, next, (AxisMask)_ignoredAxes.enumValueFlag, dimension);
        }

        private void OnAxesChanged(ToggleButtonGroupState state)
        {
            var ignored = 0;

            for (var i = 0; i < _axisButtons.Length; i++)
            {
                if (state[i] == false)
                {
                    ignored |= 1 << i;
                }
            }

            if (ignored == _ignoredAxes.enumValueFlag)
            {
                return;
            }

            _serializedObject.Update();
            _ignoredAxes.enumValueFlag = ignored;
            _serializedObject.ApplyModifiedProperties();
        }

        private void OnLinkClicked(SerializedProperty constrain)
        {
            _serializedObject.Update();
            constrain.boolValue = constrain.boolValue == false;
            _serializedObject.ApplyModifiedProperties();
            RefreshState();
        }

        private void OnPipetteClicked(SerializedProperty target)
        {
            if (BindingValueReader.TryRead(_info.Name, _source.Binding, out var sample) == false)
            {
                return;
            }

            _serializedObject.Update();

            switch (_info.ValueKind)
            {
                case ChannelValueKind.Float:
                    target.floatValue = sample.x;
                    break;

                case ChannelValueKind.Vector2:
                    target.vector2Value = new Vector2(sample.x, sample.y);
                    break;

                case ChannelValueKind.Vector3:
                    target.vector3Value = new Vector3(sample.x, sample.y, sample.z);
                    break;

                case ChannelValueKind.Vector4:
                    target.vector4Value = sample;
                    break;

                case ChannelValueKind.Color:
                    target.colorValue = new Color(sample.x, sample.y, sample.z, sample.w);
                    break;
            }

            _serializedObject.ApplyModifiedProperties();
        }

        private void ToggleExpanded()
        {
            _serializedObject.UpdateIfRequiredOrScript();

            if (_enabled.boolValue == false)
            {
                return;
            }

            IsExpanded = IsExpanded == false;
            RefreshState();
        }

        private void RefreshState()
        {
            _serializedObject.UpdateIfRequiredOrScript();

            var enabled = _enabled.boolValue;
            var expanded = IsExpanded;
            var mode = (ValueMode)_mode.enumValueIndex;

            _card.EnableInClassList(TweenClipSections.CARD_DISABLED_USS_CLASS_NAME, enabled == false);
            _body.style.display = enabled && expanded ? DisplayStyle.Flex : DisplayStyle.None;
            _chevron.style.visibility = enabled ? Visibility.Visible : Visibility.Hidden;
            _chevronImage.image = expanded ? EditorIcons.ChevronDown : EditorIcons.ChevronRight;

            _fromRow.Root.style.display = mode == ValueMode.FromCurrent ? DisplayStyle.None : DisplayStyle.Flex;
            _note.text = GetNote(mode);
            _note.style.display = mode == ValueMode.Absolute ? DisplayStyle.None : DisplayStyle.Flex;

            RefreshAxes();
            UpdatePipettes();
            RefreshLink(_fromRow);
            RefreshLink(_toRow);
        }

        private void RefreshAxes()
        {
            if (_axisButtons == null)
            {
                return;
            }

            var ignored = (AxisMask)_ignoredAxes.enumValueFlag;

            var animated = 0UL;

            for (var i = 0; i < _axisButtons.Length; i++)
            {
                var isOn = ConstrainMath.IsIgnored(ignored, i) == false;
                _axisButtons[i].EnableInClassList(TweenClipSections.AXIS_BUTTON_ON_USS_CLASS_NAME, isOn);

                if (isOn)
                {
                    animated |= 1UL << i;
                }
            }

            _axisGroup?.SetValueWithoutNotify(new ToggleButtonGroupState(animated, _axisButtons.Length));
        }

        private static void RefreshLink(ValueRow row)
        {
            if (row.Link == null)
            {
                return;
            }

            var isOn = row.Constrain.boolValue;
            row.Link.EnableInClassList(TweenClipSections.ICON_BUTTON_ON_USS_CLASS_NAME, isOn);
            row.LinkImage.image = isOn ? EditorIcons.Link : EditorIcons.Unlink;
        }

        private void UpdatePipettes()
        {
            _serializedObject.UpdateIfRequiredOrScript();

            var available = BindingValueReader.CanRead(_info.Name, _source.Binding);
            var tooltip = TweenClipTexts.PIPETTE_TOOLTIP;

            if (available == false)
            {
                tooltip = _source.Binding == null ? _source.Reason : TweenClipTexts.UNSUPPORTED_BINDING;
            }

            // A pipette writes the bound object's absolute value, which is wrong for an offset.
            if ((ValueMode)_mode.enumValueIndex == ValueMode.Relative)
            {
                available = false;
                tooltip = TweenClipTexts.PIPETTE_RELATIVE_TOOLTIP;
            }

            _fromRow.Pipette.SetEnabled(available);
            _fromRow.Pipette.tooltip = tooltip;
            _toRow.Pipette.SetEnabled(available);
            _toRow.Pipette.tooltip = tooltip;
        }

        private static string GetNote(ValueMode mode)
        {
            switch (mode)
            {
                case ValueMode.FromCurrent:
                    return TweenClipTexts.FROM_CURRENT_NOTE;

                case ValueMode.Relative:
                    return TweenClipTexts.RELATIVE_NOTE;

                default:
                    return string.Empty;
            }
        }

        // The vector a field had when the current edit started.
        private sealed class VectorEdit
        {
            private bool _focused;
            private bool _pressed;

            public Vector3 Base { get; private set; }

            public bool IsActive { get; private set; }

            public void Focus(System.Func<Vector3> read)
            {
                _focused = true;
                Begin(read);
            }

            public void Press(System.Func<Vector3> read)
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

            private void Begin(System.Func<Vector3> read)
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

        private readonly struct ValueRow
        {
            public readonly VisualElement Root;
            public readonly Button Link;
            public readonly Image LinkImage;
            public readonly Button Pipette;
            public readonly SerializedProperty Constrain;

            public ValueRow(
                  VisualElement root
                , Button link
                , Image linkImage
                , Button pipette
                , SerializedProperty constrain
            )
            {
                Root = root;
                Link = link;
                LinkImage = linkImage;
                Pipette = pipette;
                Constrain = constrain;
            }
        }
    }
}
