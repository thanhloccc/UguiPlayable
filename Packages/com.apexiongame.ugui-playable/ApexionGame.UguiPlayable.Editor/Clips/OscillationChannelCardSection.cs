using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Editor.Common;

namespace ApexionGame.UguiPlayable.Editor.Clips
{
    /// <summary>
    /// The card of one <c>OscillationChannel</c> (a punch or a shake): an enable switch, icon, name, target control
    /// and chevron in the header; axes, strength and frequency in the body, then the elasticity of a punch or the
    /// randomness, fade out and seed of a shake. A disabled channel is collapsed and dimmed.
    /// </summary>
    internal sealed class OscillationChannelCardSection
    {
        private const string SHAKE_FIELD = "_shake";
        private const string ELASTICITY = "_elasticity";
        private const string RANDOMNESS = "_randomness";
        private const string FADE_OUT = "_fadeOut";
        private const string SEED = "_seed";
        private const int AXIS_COUNT = 3;

        private static readonly string[] s_targetLabels = { "Position", "Rotation", "Scale" };
        private static readonly string[] s_axisCaptions = { "X", "Y", "Z" };
        private static readonly string[] s_axisClasses = {
              TweenClipSections.AXIS_BUTTON_X_USS_CLASS_NAME
            , TweenClipSections.AXIS_BUTTON_Y_USS_CLASS_NAME
            , TweenClipSections.AXIS_BUTTON_Z_USS_CLASS_NAME
        };

        private readonly SerializedObject _serializedObject;
        private readonly string _expandKey;
        private readonly bool _isShake;

        private readonly SerializedProperty _enabled;
        private readonly SerializedProperty _target;
        private readonly SerializedProperty _ignoredAxes;
        private readonly SerializedProperty _seed;

        private readonly VisualElement _card = new();
        private readonly VisualElement _body = new();
        private readonly Button _chevron;
        private readonly Image _chevronImage;
        private readonly Button[] _axisButtons = new Button[AXIS_COUNT];

        private ToggleButtonGroup _axisGroup;
        private ulong _shownAnimated;

        private OscillationChannelCardSection(SerializedObject serializedObject, ChannelInfo info)
        {
            _serializedObject = serializedObject;
            _isShake = info.Name == SHAKE_FIELD;

            var template = serializedObject.FindProperty(TweenClipModel.TEMPLATE);
            var channel = template.FindPropertyRelative(info.Name);

            _enabled = channel.FindPropertyRelative(TweenClipModel.ENABLED);
            _target = channel.FindPropertyRelative(TweenClipModel.TARGET);
            _ignoredAxes = channel.FindPropertyRelative(TweenClipModel.IGNORED_AXES);
            _seed = channel.FindPropertyRelative(SEED);

            _expandKey = TweenClipSections.GetExpandKey(serializedObject.targetObject, channel.propertyPath);

            _card.AddToClassList(TweenClipSections.CARD_USS_CLASS_NAME);
            _card.AddToClassList(TweenClipSections.CARD_WIDE_LABELS_USS_CLASS_NAME);
            _body.AddToClassList(TweenClipSections.CARD_BODY_USS_CLASS_NAME);

            _chevron = TweenClipSections.CreateIconButton(
                  EditorIcons.ChevronDown
                , string.Empty
                , ToggleExpanded
                , out _chevronImage
            );

            _card.Add(CreateHeader(info));
            _card.Add(_body);

            _body.Add(CreateAxesRow());

            var strengthField = new Vector3Field();
            VectorFieldBinder.Bind(strengthField, channel.FindPropertyRelative(TweenClipModel.STRENGTH));
            strengthField.AddToClassList(TweenClipSections.FIELD_USS_CLASS_NAME);
            _body.Add(CreateRow(TweenClipTexts.STRENGTH, strengthField));

            var frequencyField = new FloatField { tooltip = TweenClipTexts.FREQUENCY_TOOLTIP };
            frequencyField.AddToClassList(TweenClipSections.FIELD_USS_CLASS_NAME);
            frequencyField.BindProperty(channel.FindPropertyRelative(TweenClipModel.FREQUENCY));
            frequencyField.RegisterCallback<BlurEvent>(
                _ => ClampFrequency(channel.FindPropertyRelative(TweenClipModel.FREQUENCY))
            );
            _body.Add(CreateRow(TweenClipTexts.FREQUENCY, frequencyField));

            if (_isShake)
            {
                _body.Add(CreateSliderRow(
                      TweenClipTexts.RANDOMNESS
                    , TweenClipTexts.RANDOMNESS_TOOLTIP
                    , channel.FindPropertyRelative(RANDOMNESS)
                ));

                var fadeOutToggle = new Toggle { tooltip = TweenClipTexts.FADE_OUT_TOOLTIP }.WithSwitchStyle();
                fadeOutToggle.BindProperty(channel.FindPropertyRelative(FADE_OUT));
                _body.Add(CreateRow(TweenClipTexts.FADE_OUT, fadeOutToggle));

                _body.Add(CreateSeedRow());
            }
            else
            {
                _body.Add(CreateSliderRow(
                      TweenClipTexts.ELASTICITY
                    , TweenClipTexts.ELASTICITY_TOOLTIP
                    , channel.FindPropertyRelative(ELASTICITY)
                ));
            }

            _card.TrackPropertyValue(_enabled, _ => RefreshState());
            _card.TrackPropertyValue(_ignoredAxes, _ => RefreshState());
            MultiEdit.PollWhileMultiple(_card, serializedObject, RefreshState);

            RefreshState();
        }

        public static VisualElement Create(SerializedObject serializedObject, ChannelInfo info)
            => new OscillationChannelCardSection(serializedObject, info)._card;

        private bool IsExpanded
        {
            get => SessionState.GetBool(_expandKey, true);
            set => SessionState.SetBool(_expandKey, value);
        }

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
                var image = TweenClipSections.CreateIcon(icon, TweenClipSections.CARD_ICON_USS_CLASS_NAME);
                image.pickingMode = PickingMode.Position;
                image.RegisterCallback<ClickEvent>(_ => ToggleExpanded());
                header.Add(image);
            }

            var name = new Label(ChannelPresentation.GetDisplayName(info.Name));
            name.AddToClassList(TweenClipSections.CARD_NAME_USS_CLASS_NAME);
            name.RegisterCallback<ClickEvent>(_ => ToggleExpanded());
            header.Add(name);

            header.Add(SegmentedControl.Create(s_targetLabels).BindEnum(_target, s_targetLabels.Length));
            header.Add(_chevron);
            return header;
        }

        // The same multi-selection ToggleButtonGroup as the tween cards: a selected button gets the offset.
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

            for (var i = 0; i < AXIS_COUNT; i++)
            {
                var button = new Button {
                    text = s_axisCaptions[i],
                    tooltip = TweenClipTexts.OSCILLATION_AXIS_TOOLTIP,
                };

                button.AddToClassList(TweenClipSections.AXIS_BUTTON_USS_CLASS_NAME);
                button.AddToClassList(s_axisClasses[i]);
                _axisButtons[i] = button;
                _axisGroup.Add(button);
            }

            _axisGroup.RegisterValueChangedCallback(evt => OnAxesChanged(evt.newValue));
            row.Add(_axisGroup);
            return row;
        }

        private static VisualElement CreateRow(string label, VisualElement content)
        {
            var row = TweenClipSections.CreateRow(label);
            row.Add(content);
            return row;
        }

        private static VisualElement CreateSliderRow(string label, string tooltip, SerializedProperty value)
        {
            var slider = new Slider(0f, 1f) { showInputField = true, tooltip = tooltip };
            slider.AddToClassList(TweenClipSections.FIELD_USS_CLASS_NAME);
            slider.BindProperty(value);
            return CreateRow(label, slider);
        }

        private VisualElement CreateSeedRow()
        {
            var seedField = new IntegerField { tooltip = TweenClipTexts.SEED_TOOLTIP };
            seedField.AddToClassList(TweenClipSections.FIELD_USS_CLASS_NAME);
            seedField.BindProperty(_seed);

            var row = CreateRow(TweenClipTexts.SEED, seedField);

            var dice = TweenClipSections.CreateIconButton(
                  EditorIcons.Dices
                , TweenClipTexts.NEW_SEED_TOOLTIP
                , OnNewSeedClicked
                , out _
            );

            row.Add(dice);
            return row;
        }

        // Each selected clip gets its own new seed, in one Undo step.
        private void OnNewSeedClicked()
        {
            var path = _seed.propertyPath;

            MultiEdit.WriteEach(_serializedObject, (each, _) => {
                each.FindProperty(path).intValue = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            });
        }

        // A negative frequency has no meaning; the field accepts any float, so the value is clamped on blur.
        private void ClampFrequency(SerializedProperty frequency)
            => MultiEdit.ClampToZero(_serializedObject, frequency);

        // Only the axes whose button changed are written, so the clips keep their other axes.
        private void OnAxesChanged(ToggleButtonGroupState state)
        {
            var changed = new System.Collections.Generic.List<int>();

            for (var i = 0; i < AXIS_COUNT; i++)
            {
                if (state[i] != ((_shownAnimated & (1UL << i)) != 0UL))
                {
                    changed.Add(i);
                }
            }

            if (changed.Count == 0)
            {
                return;
            }

            _serializedObject.Update();

            var path = _ignoredAxes.propertyPath;

            MultiEdit.WriteEach(_serializedObject, (each, _) => {
                var property = each.FindProperty(path);
                var ignored = property.enumValueFlag;

                for (var i = 0; i < changed.Count; i++)
                {
                    var bit = 1 << changed[i];
                    ignored = state[changed[i]] ? ignored & ~bit : ignored | bit;
                }

                property.enumValueFlag = ignored;
            });

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

            var masks = MultiEdit.ReadEach(_serializedObject, _ignoredAxes.propertyPath, p => (AxisMask)p.enumValueFlag);
            var animated = 0UL;

            for (var i = 0; i < AXIS_COUNT; i++)
            {
                var animatedCount = 0;

                for (var j = 0; j < masks.Length; j++)
                {
                    if (ConstrainMath.IsIgnored(masks[j], i) == false)
                    {
                        animatedCount++;
                    }
                }

                // On only when every clip adds the axis; an axis the clips disagree on is mixed.
                var isOn = animatedCount == masks.Length;
                var isMixed = animatedCount > 0 && isOn == false;

                _axisButtons[i].EnableInClassList(TweenClipSections.AXIS_BUTTON_ON_USS_CLASS_NAME, isOn);
                _axisButtons[i].EnableInClassList(TweenClipSections.AXIS_BUTTON_MIXED_USS_CLASS_NAME, isMixed);

                if (isOn)
                {
                    animated |= 1UL << i;
                }
            }

            _shownAnimated = animated;
            _axisGroup.SetValueWithoutNotify(new ToggleButtonGroupState(animated, AXIS_COUNT));
        }
    }
}
