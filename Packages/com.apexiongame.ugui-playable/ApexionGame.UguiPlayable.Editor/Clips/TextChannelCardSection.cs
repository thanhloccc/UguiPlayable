using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Editor.Common;

namespace ApexionGame.UguiPlayable.Editor.Clips
{
    /// <summary>
    /// The card of one <c>TextChannel</c>: an enable switch, icon, name and a two-way mode control in the header;
    /// multi-line From and To fields, the ease, the scramble mode and its custom characters in the body. A disabled
    /// channel is collapsed and dimmed.
    /// </summary>
    /// <remarks>
    /// The mode control offers Absolute and From current only: Relative has no meaning for text and behaves like
    /// Absolute at runtime, so a stored Relative is shown as Absolute.
    /// </remarks>
    internal sealed class TextChannelCardSection
    {
        private const string TAG_START = "<";

        private static readonly string[] s_modeLabels = { "Absolute", "From current" };

        private readonly SerializedObject _serializedObject;
        private readonly ClipBindingSource _source;
        private readonly SerializedProperty _enabled;
        private readonly SerializedProperty _mode;
        private readonly SerializedProperty _from;
        private readonly SerializedProperty _to;
        private readonly SerializedProperty _scramble;

        private readonly VisualElement _card = new();
        private readonly VisualElement _body = new();
        private readonly Label _note;
        private readonly VisualElement _fromRow;
        private readonly VisualElement _toRow;
        private readonly VisualElement _customRow;
        private readonly Button _fromPipette;
        private readonly Button _toPipette;

        private ToggleButtonGroup _modeGroup;

        private TextChannelCardSection(SerializedObject serializedObject, ChannelInfo info, ClipBindingSource source)
        {
            _serializedObject = serializedObject;
            _source = source;

            var template = serializedObject.FindProperty(TweenClipModel.TEMPLATE);
            var channel = template.FindPropertyRelative(info.Name);

            _enabled = channel.FindPropertyRelative(TweenClipModel.ENABLED);
            _mode = channel.FindPropertyRelative(TweenClipModel.MODE);
            _from = channel.FindPropertyRelative(TweenClipModel.FROM);
            _to = channel.FindPropertyRelative(TweenClipModel.TO);
            _scramble = channel.FindPropertyRelative(TweenClipModel.SCRAMBLE);

            _card.AddToClassList(TweenClipSections.CARD_USS_CLASS_NAME);
            _body.AddToClassList(TweenClipSections.CARD_BODY_USS_CLASS_NAME);

            _card.Add(CreateHeader(info));
            _card.Add(_body);

            _note = TweenClipSections.CreateNote(string.Empty);
            _body.Add(_note);

            _fromRow = CreateTextRow(TweenClipTexts.FROM, _from, out _fromPipette);
            _toRow = CreateTextRow(TweenClipTexts.TO, _to, out _toPipette);
            _body.Add(_fromRow);
            _body.Add(_toRow);
            _body.Add(new PropertyField(channel.FindPropertyRelative(TweenClipModel.EASE)));

            var scrambleField = new EnumField(ScrambleMode.None) { tooltip = TweenClipTexts.TEXT_SCRAMBLE_TOOLTIP };
            scrambleField.AddToClassList(TweenClipSections.FIELD_USS_CLASS_NAME);
            scrambleField.BindProperty(_scramble);

            var scrambleRow = CreateRow(TweenClipTexts.SCRAMBLE);
            scrambleRow.Add(scrambleField);
            _body.Add(scrambleRow);

            var customField = new TextField { tooltip = TweenClipTexts.TEXT_CUSTOM_TOOLTIP };
            customField.AddToClassList(TweenClipSections.FIELD_USS_CLASS_NAME);
            customField.BindProperty(channel.FindPropertyRelative(TweenClipModel.CUSTOM_SCRAMBLE_CHARS));

            _customRow = CreateRow(TweenClipTexts.CHARACTERS);
            _customRow.Add(customField);
            _body.Add(_customRow);

            _card.TrackPropertyValue(_enabled, _ => RefreshState());
            _card.TrackPropertyValue(_mode, _ => RefreshState());
            _card.TrackPropertyValue(_from, _ => RefreshState());
            _card.TrackPropertyValue(_to, _ => RefreshState());
            _card.TrackPropertyValue(_scramble, _ => RefreshState());
            MultiEdit.PollWhileMultiple(_card, serializedObject, RefreshState);

            _source.Changed += RefreshState;

            RefreshState();
        }

        public static VisualElement Create(
              SerializedObject serializedObject
            , ChannelInfo info
            , ClipBindingSource source
        )
            => new TextChannelCardSection(serializedObject, info, source)._card;

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

        private VisualElement CreateTextRow(string label, SerializedProperty value, out Button pipette)
        {
            var row = CreateRow(label);
            row.AddToClassList(TweenClipSections.ROW_TOP_USS_CLASS_NAME);

            var field = new TextField { multiline = true };
            field.AddToClassList(TweenClipSections.FIELD_USS_CLASS_NAME);
            field.AddToClassList(TweenClipSections.TEXT_FIELD_USS_CLASS_NAME);
            field.BindProperty(value);
            row.Add(field);

            pipette = TweenClipSections.CreateIconButton(
                  EditorIcons.Pipette
                , TweenClipTexts.PIPETTE_TOOLTIP
                , () => OnPipetteClicked(value)
                , out _
            );

            row.Add(pipette);
            return row;
        }

        // The row captions of this card are longer than the default label width ("Scramble").
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

        private void OnPipetteClicked(SerializedProperty target)
        {
            if (BindingValueReader.TryReadText(_source.Binding, out var text) == false)
            {
                return;
            }

            _serializedObject.Update();
            target.stringValue = text;
            _serializedObject.ApplyModifiedProperties();
        }

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
            _customRow.style.display = _scramble.hasMultipleDifferentValues
                || _scramble.enumValueIndex == (int)ScrambleMode.Custom
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            var note = fromCurrent ? TweenClipTexts.TEXT_FROM_CURRENT_NOTE : string.Empty;

            var hasTag = HasTag(_to.stringValue) || (fromCurrent == false && HasTag(_from.stringValue));

            if (hasTag && BindingValueReader.IsRichTextOff(_source.Binding))
            {
                note = note.Length == 0
                    ? TweenClipTexts.TEXT_RICH_TAGS_NOTE
                    : note + "\n" + TweenClipTexts.TEXT_RICH_TAGS_NOTE;
            }

            _note.text = note;
            _note.style.display = note.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;

            UpdatePipettes();
        }

        private static bool HasTag(string value)
            => string.IsNullOrEmpty(value) == false && value.Contains(TAG_START);

        private void UpdatePipettes()
        {
            _serializedObject.UpdateIfRequiredOrScript();

            var available = BindingValueReader.TryReadText(_source.Binding, out _);
            var tooltip = TweenClipTexts.PIPETTE_TOOLTIP;

            if (available == false)
            {
                tooltip = _source.Binding == null ? _source.Reason : TweenClipTexts.UNSUPPORTED_BINDING;
            }

            _fromPipette.SetEnabled(available);
            _fromPipette.tooltip = tooltip;
            _toPipette.SetEnabled(available);
            _toPipette.tooltip = tooltip;
        }
    }
}
