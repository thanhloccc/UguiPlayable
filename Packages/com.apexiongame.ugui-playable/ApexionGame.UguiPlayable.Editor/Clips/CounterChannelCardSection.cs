using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Editor.Common;

namespace ApexionGame.UguiPlayable.Editor.Clips
{
    /// <summary>
    /// The card of one <c>CounterChannel</c>: an enable switch, icon, name and a two-way mode control in the header;
    /// From and To, the ease, decimals, a grouping switch, separators, prefix and suffix in the body. A disabled
    /// channel is collapsed and dimmed.
    /// </summary>
    /// <remarks>
    /// The mode control offers Absolute and From current only: Relative has no meaning for a counter and behaves like
    /// Absolute at runtime, so a stored Relative is shown as Absolute. There is no pipette: the text is never parsed.
    /// </remarks>
    internal sealed class CounterChannelCardSection
    {
        private static readonly string[] s_modeLabels = { "Absolute", "From current" };

        private readonly SerializedObject _serializedObject;
        private readonly SerializedProperty _enabled;
        private readonly SerializedProperty _mode;
        private readonly SerializedProperty _textEnabled;

        private readonly VisualElement _card = new();
        private readonly VisualElement _body = new();
        private readonly Label _note;
        private readonly VisualElement _fromRow;

        private ToggleButtonGroup _modeGroup;

        private CounterChannelCardSection(SerializedObject serializedObject, ChannelInfo info)
        {
            _serializedObject = serializedObject;

            var template = serializedObject.FindProperty(TweenClipModel.TEMPLATE);
            var channel = template.FindPropertyRelative(info.Name);

            _enabled = channel.FindPropertyRelative(TweenClipModel.ENABLED);
            _mode = channel.FindPropertyRelative(TweenClipModel.MODE);

            // The string channel of the same clip; absent on a template that has no text channel.
            _textEnabled = template.FindPropertyRelative(ChannelPresentation.TEXT)
                ?.FindPropertyRelative(TweenClipModel.ENABLED);

            _card.AddToClassList(TweenClipSections.CARD_USS_CLASS_NAME);
            _body.AddToClassList(TweenClipSections.CARD_BODY_USS_CLASS_NAME);

            _card.Add(CreateHeader(info));
            _card.Add(_body);

            _note = TweenClipSections.CreateNote(string.Empty);
            _body.Add(_note);

            _fromRow = CreateNumberRow(TweenClipTexts.FROM, channel.FindPropertyRelative(TweenClipModel.FROM));
            _body.Add(_fromRow);
            _body.Add(CreateNumberRow(TweenClipTexts.TO, channel.FindPropertyRelative(TweenClipModel.TO)));
            _body.Add(new PropertyField(channel.FindPropertyRelative(TweenClipModel.EASE)));

            var decimals = new IntegerField { tooltip = TweenClipTexts.COUNTER_DECIMALS_TOOLTIP };
            decimals.AddToClassList(TweenClipSections.FIELD_USS_CLASS_NAME);
            decimals.BindProperty(channel.FindPropertyRelative(TweenClipModel.DECIMALS));
            decimals.RegisterValueChangedCallback(static evt => {
                if (evt.target is IntegerField field)
                {
                    var clamped = System.Math.Clamp(evt.newValue, 0, CounterChannel.MAX_DECIMALS);

                    if (clamped != evt.newValue)
                    {
                        field.value = clamped;
                    }
                }
            });

            var decimalsRow = CreateRow(TweenClipTexts.COUNTER_DECIMALS);
            decimalsRow.Add(decimals);
            _body.Add(decimalsRow);

            var grouping = new Toggle { tooltip = TweenClipTexts.COUNTER_GROUPING_TOOLTIP }.WithSwitchStyle();
            grouping.BindProperty(channel.FindPropertyRelative(TweenClipModel.GROUPING));

            var groupingRow = CreateRow(TweenClipTexts.COUNTER_GROUPING);
            groupingRow.Add(grouping);
            _body.Add(groupingRow);

            _body.Add(CreateTextRow(
                  TweenClipTexts.COUNTER_GROUP_SEPARATOR
                , channel.FindPropertyRelative(TweenClipModel.GROUP_SEPARATOR)
                , TweenClipTexts.COUNTER_GROUP_SEPARATOR_TOOLTIP
            ));

            _body.Add(CreateTextRow(
                  TweenClipTexts.COUNTER_DECIMAL_SEPARATOR
                , channel.FindPropertyRelative(TweenClipModel.DECIMAL_SEPARATOR)
                , TweenClipTexts.COUNTER_DECIMAL_SEPARATOR_TOOLTIP
            ));

            _body.Add(CreateTextRow(
                  TweenClipTexts.COUNTER_PREFIX
                , channel.FindPropertyRelative(TweenClipModel.PREFIX)
                , string.Empty
            ));

            _body.Add(CreateTextRow(
                  TweenClipTexts.COUNTER_SUFFIX
                , channel.FindPropertyRelative(TweenClipModel.SUFFIX)
                , string.Empty
            ));

            _card.TrackPropertyValue(_enabled, _ => RefreshState());
            _card.TrackPropertyValue(_mode, _ => RefreshState());

            if (_textEnabled != null)
            {
                _card.TrackPropertyValue(_textEnabled, _ => RefreshState());
            }

            MultiEdit.PollWhileMultiple(_card, serializedObject, RefreshState);

            RefreshState();
        }

        public static VisualElement Create(SerializedObject serializedObject, ChannelInfo info)
            => new CounterChannelCardSection(serializedObject, info)._card;

        private VisualElement CreateHeader(ChannelInfo info)
        {
            var header = new VisualElement();
            header.AddToClassList(TweenClipSections.CARD_HEADER_USS_CLASS_NAME);

            var enabledToggle = new Toggle { tooltip = TweenClipTexts.ENABLED_TOOLTIP }.WithSwitchStyle();
            enabledToggle.BindProperty(_enabled);
            header.Add(enabledToggle);

            var icon = ChannelPresentation.GetIcon(info.Name);

            if (icon != null)
            {
                header.Add(TweenClipSections.CreateIcon(icon, TweenClipSections.CARD_ICON_USS_CLASS_NAME));
            }

            var name = new Label(ChannelPresentation.GetDisplayName(info.Name));
            name.AddToClassList(TweenClipSections.CARD_NAME_USS_CLASS_NAME);
            header.Add(name);

            _modeGroup = SegmentedControl.Create(s_modeLabels);
            _modeGroup.RegisterValueChangedCallback(evt => OnModeChanged(evt.newValue));
            header.Add(_modeGroup);
            return header;
        }

        private static VisualElement CreateNumberRow(string label, SerializedProperty value)
        {
            var field = new DoubleField();
            field.AddToClassList(TweenClipSections.FIELD_USS_CLASS_NAME);
            field.BindProperty(value);

            var row = CreateRow(label);
            row.Add(field);
            return row;
        }

        private static VisualElement CreateTextRow(string label, SerializedProperty value, string tooltip)
        {
            var field = new TextField { tooltip = tooltip };
            field.AddToClassList(TweenClipSections.FIELD_USS_CLASS_NAME);
            field.BindProperty(value);

            var row = CreateRow(label);
            row.Add(field);
            return row;
        }

        // The row captions of this card are longer than the default label width ("Decimals").
        private static VisualElement CreateRow(string label)
        {
            var row = TweenClipSections.CreateRow(label);
            row[0].AddToClassList(TweenClipSections.ROW_LABEL_TEXT_USS_CLASS_NAME);
            return row;
        }

        // Index 0 is Absolute (a stored Relative shows as Absolute), index 1 is From current.
        private void OnModeChanged(ToggleButtonGroupState state)
        {
            var index = SegmentedControl.GetSelectedIndex(state);

            if (index < 0)
            {
                return;
            }

            var mode = index == 1 ? ValueMode.FromCurrent : ValueMode.Absolute;

            _serializedObject.Update();

            if (_mode.hasMultipleDifferentValues == false && IsFromCurrent() == (mode == ValueMode.FromCurrent))
            {
                return;
            }

            _mode.enumValueIndex = (int)mode;
            _serializedObject.ApplyModifiedProperties();
        }

        private bool IsFromCurrent()
            => (ValueMode)_mode.enumValueIndex == ValueMode.FromCurrent;

        private void RefreshState()
        {
            _serializedObject.UpdateIfRequiredOrScript();

            var enabled = MultiEdit.IsAnyOn(_enabled);
            var modeMixed = _mode.hasMultipleDifferentValues;
            var fromCurrent = modeMixed == false && IsFromCurrent();

            _card.EnableInClassList(TweenClipSections.CARD_DISABLED_USS_CLASS_NAME, enabled == false);
            _body.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;
            _modeGroup.SetValueWithoutNotify(
                SegmentedControl.ToState(modeMixed ? -1 : fromCurrent ? 1 : 0, s_modeLabels.Length)
            );

            _fromRow.style.display = fromCurrent ? DisplayStyle.None : DisplayStyle.Flex;

            var note = fromCurrent ? TweenClipTexts.COUNTER_FROM_CURRENT_NOTE : string.Empty;

            if (_textEnabled != null && MultiEdit.IsAnyOn(_textEnabled))
            {
                note = note.Length == 0
                    ? TweenClipTexts.COUNTER_STRING_ON_NOTE
                    : note + "\n" + TweenClipTexts.COUNTER_STRING_ON_NOTE;
            }

            _note.text = note;
            _note.style.display = note.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
