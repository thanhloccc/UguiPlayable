using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using ApexionGame.UguiPlayable.Editor.Common;

namespace ApexionGame.UguiPlayable.Editor.Clips
{
    /// <summary>
    /// The card of a <c>WaveChannel</c>: an enable switch, icon, name, target control and chevron in the header; the
    /// shape, amplitude, cycles and phase per character in the body. Modelled on
    /// <see cref="OscillationChannelCardSection"/>. A disabled channel is collapsed and dimmed.
    /// </summary>
    internal sealed class WaveChannelCardSection
    {
        private const string SHAPE = "Shape";
        private const string AMPLITUDE = "Amplitude";
        private const string CYCLES = "Cycles";
        private const string PHASE = "Phase / char";
        private const string SHAPE_TOOLTIP = "Sine swings both ways; Bounce stays on the positive side.";
        private const string CYCLES_TOOLTIP = "Wave cycles over the clip.";
        private const string PHASE_TOOLTIP = "Delay, in cycles, from one character to the next in the clip's order.";
        private const string AXES_NOTE =
            "Position uses X and Y, Rotation uses Z (degrees), Scale adds X and Y. The wave has no hold.";

        private static readonly string[] s_targetLabels = { "Position", "Rotation", "Scale" };
        private static readonly string[] s_shapeLabels = { "Sine", "Bounce" };

        private readonly SerializedObject _serializedObject;
        private readonly string _expandKey;
        private readonly SerializedProperty _enabled;

        private readonly VisualElement _card = new();
        private readonly VisualElement _body = new();
        private readonly Button _chevron;
        private readonly Image _chevronImage;

        private WaveChannelCardSection(SerializedObject serializedObject, ChannelInfo info)
        {
            _serializedObject = serializedObject;

            var template = serializedObject.FindProperty(TweenClipModel.TEMPLATE);
            var channel = template.FindPropertyRelative(info.Name);

            _enabled = channel.FindPropertyRelative(TweenClipModel.ENABLED);
            _expandKey = TweenClipSections.GetExpandKey(serializedObject.targetObject, channel.propertyPath);

            _card.AddToClassList(TweenClipSections.CARD_USS_CLASS_NAME);
            _body.AddToClassList(TweenClipSections.CARD_BODY_USS_CLASS_NAME);

            _chevron = TweenClipSections.CreateIconButton(
                  EditorIcons.ChevronDown
                , string.Empty
                , ToggleExpanded
                , out _chevronImage
            );

            _card.Add(CreateHeader(info, channel.FindPropertyRelative(TweenClipModel.TARGET)));
            _card.Add(_body);

            _body.Add(TweenClipSections.CreateNote(AXES_NOTE));

            var shape = SegmentedControl.Create(s_shapeLabels)
                .BindEnum(channel.FindPropertyRelative(TweenClipModel.SHAPE), s_shapeLabels.Length);

            shape.tooltip = SHAPE_TOOLTIP;
            _body.Add(CreateRow(SHAPE, shape));

            var amplitudeField = new Vector3Field();
            amplitudeField.AddToClassList(TweenClipSections.FIELD_USS_CLASS_NAME);
            VectorFieldBinder.Bind(amplitudeField, channel.FindPropertyRelative(TweenClipModel.AMPLITUDE));
            _body.Add(CreateRow(AMPLITUDE, amplitudeField));

            var cycles = channel.FindPropertyRelative(TweenClipModel.CYCLES);
            var cyclesField = new FloatField { tooltip = CYCLES_TOOLTIP };
            cyclesField.AddToClassList(TweenClipSections.FIELD_USS_CLASS_NAME);
            cyclesField.BindProperty(cycles);
            cyclesField.RegisterCallback<BlurEvent>(_ => ClampCycles(cycles));
            _body.Add(CreateRow(CYCLES, cyclesField));

            var phaseField = new FloatField { tooltip = PHASE_TOOLTIP };
            phaseField.AddToClassList(TweenClipSections.FIELD_USS_CLASS_NAME);
            phaseField.BindProperty(channel.FindPropertyRelative(TweenClipModel.PHASE_PER_CHARACTER));
            _body.Add(CreateRow(PHASE, phaseField));

            _card.TrackPropertyValue(_enabled, _ => RefreshState());
            MultiEdit.PollWhileMultiple(_card, serializedObject, RefreshState);

            RefreshState();
        }

        public static VisualElement Create(SerializedObject serializedObject, ChannelInfo info)
            => new WaveChannelCardSection(serializedObject, info)._card;

        private bool IsExpanded
        {
            get => SessionState.GetBool(_expandKey, true);
            set => SessionState.SetBool(_expandKey, value);
        }

        private VisualElement CreateHeader(ChannelInfo info, SerializedProperty target)
        {
            var header = new VisualElement();
            header.AddToClassList(TweenClipSections.CARD_HEADER_USS_CLASS_NAME);

            var enabledToggle = new Toggle { tooltip = TweenClipTexts.ENABLED_TOOLTIP }.WithSwitchStyle();
            enabledToggle.BindProperty(_enabled);
            header.Add(enabledToggle);

            var icon = ChannelPresentation.GetIcon(info.Name);

            if (icon != null)
            {
                var image = TweenClipSections.CreateIcon(icon, TweenClipSections.CARD_ICON_USS_CLASS_NAME);
                image.pickingMode = PickingMode.Position;
                image.RegisterCallback<ClickEvent>(_ => ToggleExpanded());
                header.Add(image);
            }

            var name = new Label(ChannelPresentation.GetDisplayName(info.Name));
            name.AddToClassList(TweenClipSections.CARD_NAME_USS_CLASS_NAME);
            name.RegisterCallback<ClickEvent>(_ => ToggleExpanded());
            header.Add(name);

            header.Add(SegmentedControl.Create(s_targetLabels).BindEnum(target, s_targetLabels.Length));
            header.Add(_chevron);
            return header;
        }

        private static VisualElement CreateRow(string label, VisualElement content)
        {
            var row = TweenClipSections.CreateRow(label);
            row.Add(content);
            return row;
        }

        // A negative cycle count has no meaning; the field accepts any float, so the value is clamped on blur.
        private void ClampCycles(SerializedProperty cycles)
            => MultiEdit.ClampToZero(_serializedObject, cycles);

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
        }
    }
}
