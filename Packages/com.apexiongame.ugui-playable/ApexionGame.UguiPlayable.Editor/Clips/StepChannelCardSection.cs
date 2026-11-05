using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using ApexionGame.UguiPlayable.Editor.Common;

namespace ApexionGame.UguiPlayable.Editor.Clips
{
    /// <summary>
    /// The card of one <c>StepChannel</c>: an enable switch, icon and name in the header; a value switch and a
    /// threshold slider in the body. A disabled channel is collapsed and dimmed.
    /// </summary>
    internal sealed class StepChannelCardSection
    {
        private readonly SerializedObject _serializedObject;
        private readonly SerializedProperty _enabled;
        private readonly SerializedProperty _threshold;

        private readonly VisualElement _card = new();
        private readonly VisualElement _body = new();
        private readonly Label _percent = new();

        private StepChannelCardSection(SerializedObject serializedObject, ChannelInfo info)
        {
            _serializedObject = serializedObject;

            var template = serializedObject.FindProperty(TweenClipModel.TEMPLATE);
            var channel = template.FindPropertyRelative(info.Name);

            _enabled = channel.FindPropertyRelative(TweenClipModel.ENABLED);
            _threshold = channel.FindPropertyRelative(TweenClipModel.THRESHOLD);

            _card.AddToClassList(TweenClipSections.CARD_USS_CLASS_NAME);
            _body.AddToClassList(TweenClipSections.CARD_BODY_USS_CLASS_NAME);

            _card.Add(CreateHeader(info));
            _card.Add(_body);

            var valueToggle = new Toggle().WithSwitchStyle();
            valueToggle.BindProperty(channel.FindPropertyRelative(TweenClipModel.VALUE));

            var valueRow = TweenClipSections.CreateRow(TweenClipTexts.VALUE);
            valueRow.Add(valueToggle);
            _body.Add(valueRow);

            var slider = new Slider(0f, 1f) { tooltip = TweenClipTexts.STEP_TOOLTIP };
            slider.AddToClassList(TweenClipSections.FIELD_USS_CLASS_NAME);
            slider.BindProperty(_threshold);

            _percent.AddToClassList(TweenClipSections.PERCENT_USS_CLASS_NAME);

            var thresholdRow = TweenClipSections.CreateRow(TweenClipTexts.AT);
            thresholdRow.Add(slider);
            thresholdRow.Add(_percent);
            _body.Add(thresholdRow);

            _card.TrackPropertyValue(_enabled, _ => RefreshState());
            _card.TrackPropertyValue(_threshold, _ => RefreshState());
            MultiEdit.PollWhileMultiple(_card, serializedObject, RefreshState);

            RefreshState();
        }

        public static VisualElement Create(SerializedObject serializedObject, ChannelInfo info)
            => new StepChannelCardSection(serializedObject, info)._card;

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
            return header;
        }

        private void RefreshState()
        {
            _serializedObject.UpdateIfRequiredOrScript();

            var enabled = MultiEdit.IsAnyOn(_enabled);

            _card.EnableInClassList(TweenClipSections.CARD_DISABLED_USS_CLASS_NAME, enabled == false);
            _body.style.display = enabled ? DisplayStyle.Flex : DisplayStyle.None;
            _percent.text = _threshold.hasMultipleDifferentValues
                ? "-"
                : $"{Mathf.RoundToInt(_threshold.floatValue * 100f)}%";
        }
    }
}
