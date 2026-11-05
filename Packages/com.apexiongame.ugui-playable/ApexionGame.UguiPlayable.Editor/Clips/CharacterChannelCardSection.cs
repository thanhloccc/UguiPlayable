using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Editor.Common;

namespace ApexionGame.UguiPlayable.Editor.Clips
{
    /// <summary>
    /// The card of one <c>CharacterChannel&lt;T&gt;</c> (a per-character transition of the TMP Characters clip): an
    /// enable switch, icon, name and chevron in the header; From and To rows in the body, with the constrain link
    /// for a scale. The values are relative to the character's rest pose, so there is no mode and no pipette. A
    /// disabled channel is collapsed and dimmed.
    /// </summary>
    internal sealed class CharacterChannelCardSection
    {
        private const float SOFT_SCALE = 2f;

        private const string RELATIVE_NOTE = "Values are relative to each character's rest pose.";
        private const string SOFT_SCALE_NOTE =
            "Scales above 2 soften the glyph edges: TMP keeps each glyph's SDF scale.";

        private readonly SerializedObject _serializedObject;
        private readonly ChannelInfo _info;
        private readonly string _expandKey;

        private readonly SerializedProperty _enabled;
        private readonly SerializedProperty _from;
        private readonly SerializedProperty _to;

        private readonly VisualElement _card = new();
        private readonly VisualElement _body = new();
        private readonly Button _chevron;
        private readonly Image _chevronImage;
        private readonly Label _note;
        private readonly LinkButton _fromLink;
        private readonly LinkButton _toLink;

        private CharacterChannelCardSection(SerializedObject serializedObject, ChannelInfo info)
        {
            _serializedObject = serializedObject;
            _info = info;

            var template = serializedObject.FindProperty(TweenClipModel.TEMPLATE);
            var channel = template.FindPropertyRelative(info.Name);

            _enabled = channel.FindPropertyRelative(TweenClipModel.ENABLED);
            _from = channel.FindPropertyRelative(TweenClipModel.FROM);
            _to = channel.FindPropertyRelative(TweenClipModel.TO);

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

            _note = TweenClipSections.CreateNote(RELATIVE_NOTE);
            _body.Add(_note);

            var constrainFrom = info.HasConstrain ? FindConstrain(template, true) : null;
            var constrainTo = info.HasConstrain ? FindConstrain(template, false) : null;

            _body.Add(CreateValueRow(TweenClipTexts.FROM, _from, constrainFrom, out _fromLink));
            _body.Add(CreateValueRow(TweenClipTexts.TO, _to, constrainTo, out _toLink));

            _card.TrackPropertyValue(_enabled, _ => RefreshState());
            _card.TrackPropertyValue(_from, _ => RefreshState());
            _card.TrackPropertyValue(_to, _ => RefreshState());
            MultiEdit.PollWhileMultiple(_card, serializedObject, RefreshState);

            if (constrainFrom != null)
            {
                _card.TrackPropertyValue(constrainFrom, _ => RefreshState());
                _card.TrackPropertyValue(constrainTo, _ => RefreshState());
            }

            RefreshState();
        }

        public static VisualElement Create(SerializedObject serializedObject, ChannelInfo info)
            => new CharacterChannelCardSection(serializedObject, info)._card;

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

            header.Add(_chevron);
            return header;
        }

        private SerializedProperty FindConstrain(SerializedProperty template, bool isFrom)
            => template.FindPropertyRelative(TweenClipModel.GetConstrainName(_info.Name, isFrom));

        private VisualElement CreateValueRow(
              string label
            , SerializedProperty value
            , SerializedProperty constrain
            , out LinkButton link
        )
        {
            var row = TweenClipSections.CreateRow(label);
            row.Add(CreateValueField(value, constrain));

            link = default;

            if (constrain != null)
            {
                var button = TweenClipSections.CreateIconButton(
                      EditorIcons.Unlink
                    , TweenClipTexts.LINK_TOOLTIP
                    , () => OnLinkClicked(constrain)
                    , out var image
                );

                row.Add(button);
                link = new LinkButton(button, image, constrain);
            }

            return row;
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

                default:
                {
                    var vectorField = new Vector2Field();
                    BindVector2(vectorField, value, constrain);
                    field = vectorField;
                    break;
                }
            }

            field.AddToClassList(TweenClipSections.FIELD_USS_CLASS_NAME);
            return field;
        }

        private bool IsNormalized()
            => ChannelPresentation.IsNormalized(_serializedObject.targetObject.GetType(), _info.Name);

        // Not bound with BindProperty: an edit writes only the axes that changed, and the constrain rule rewrites the
        // other axis in the same Undo step.
        private void BindVector2(Vector2Field field, SerializedProperty value, SerializedProperty constrain)
            => VectorFieldBinder.Bind(field, value, constrain);

        private void OnLinkClicked(SerializedProperty constrain)
        {
            _serializedObject.Update();
            constrain.boolValue = MultiEdit.IsAllOn(constrain) == false;
            _serializedObject.ApplyModifiedProperties();
            RefreshState();
        }

        private void ToggleExpanded()
        {
            _serializedObject.UpdateIfRequiredOrScript();

            if (MultiEdit.IsAnyOn(_enabled) == false)
            {
                return;
            }

            IsExpanded = IsExpanded == false;
            RefreshState();
        }

        private void RefreshState()
        {
            _serializedObject.UpdateIfRequiredOrScript();

            var enabled = MultiEdit.IsAnyOn(_enabled);
            var expanded = IsExpanded;

            _card.EnableInClassList(TweenClipSections.CARD_DISABLED_USS_CLASS_NAME, enabled == false);
            _body.style.display = enabled && expanded ? DisplayStyle.Flex : DisplayStyle.None;
            _chevron.style.visibility = enabled ? Visibility.Visible : Visibility.Hidden;
            _chevronImage.image = expanded ? EditorIcons.ChevronDown : EditorIcons.ChevronRight;

            _note.text = IsSoftScale() ? RELATIVE_NOTE + "\n" + SOFT_SCALE_NOTE : RELATIVE_NOTE;

            _fromLink.Refresh();
            _toLink.Refresh();
        }

        private bool IsSoftScale()
        {
            if (_info.Name != ChannelPresentation.SCALE || _info.ValueKind != ChannelValueKind.Vector2)
            {
                return false;
            }

            var from = _from.vector2Value;
            var to = _to.vector2Value;

            return Mathf.Max(Mathf.Abs(from.x), Mathf.Abs(from.y), Mathf.Abs(to.x), Mathf.Abs(to.y)) > SOFT_SCALE;
        }

        private readonly struct LinkButton
        {
            private readonly Button _button;
            private readonly Image _image;
            private readonly SerializedProperty _constrain;

            public LinkButton(Button button, Image image, SerializedProperty constrain)
            {
                _button = button;
                _image = image;
                _constrain = constrain;
            }

            public void Refresh()
            {
                if (_button == null)
                {
                    return;
                }

                var isOn = MultiEdit.IsAllOn(_constrain);
                _button.EnableInClassList(TweenClipSections.ICON_BUTTON_ON_USS_CLASS_NAME, isOn);
                _image.image = isOn ? EditorIcons.Link : EditorIcons.Unlink;
            }
        }
    }
}
