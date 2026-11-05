using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using ApexionGame.UguiPlayable.Editor.Common;
using ApexionGame.UguiPlayable.Editor.Presets;

namespace ApexionGame.UguiPlayable.Editor.Clips
{
    /// <summary>
    /// The shared clip inspector: a header (track icon, title, "N of M channels on", an "Apply preset…" button), a
    /// warning when no channel is on, then one card per channel found under <c>_template</c>. A concrete clip
    /// inspector only names itself.
    /// </summary>
    /// <remarks>
    /// Timeline hosts this in an <c>InspectorElement</c> inside its clip inspector, so
    /// <see cref="CreateInspectorGUI"/> is the clip inspector. <c>UnityEditor.Editor</c> is written in full because
    /// the namespace contains <c>.Editor</c>.
    /// </remarks>
    internal abstract class TweenClipInspector : UnityEditor.Editor
    {
        private const long BINDING_POLL_MILLISECONDS = 500;
        private const string APPLY_PRESET_TOOLTIP = "Apply preset…";

        /// <summary>The title in the header, for example "Rect Transform Clip".</summary>
        protected abstract string Title { get; }

        /// <summary>The track icon in the header.</summary>
        protected abstract Texture2D Icon { get; }

        public override VisualElement CreateInspectorGUI()
        {
            var root = TweenClipSections.CreateRoot();

            var model = TweenClipModel.Create(serializedObject);
            var source = new ClipBindingSource(targets);

            var count = new Label();
            count.AddToClassList(TweenClipSections.COUNT_USS_CLASS_NAME);

            var warning = new HelpBox(TweenClipTexts.NO_CHANNEL_ENABLED, HelpBoxMessageType.Warning);

            root.Add(CreateHeader(count));
            root.Add(warning);

            AddSections(root);

            var channels = model.Channels;

            for (var i = 0; i < channels.Count; i++)
            {
                root.Add(CreateCard(channels[i], source));
                root.TrackPropertyValue(
                      serializedObject.FindProperty(TweenClipModel.GetEnabledPath(channels[i].Name))
                    , _ => RefreshSummary(count, warning)
                );
            }

            RefreshSummary(count, warning);
            MultiEdit.PollWhileMultiple(root, serializedObject, () => RefreshSummary(count, warning));

            source.Refresh();
            root.schedule.Execute(source.Refresh).Every(BINDING_POLL_MILLISECONDS);
            return root;
        }

        /// <summary>
        /// Adds sections between the header and the channel cards, for clip settings that are not channels. The
        /// default adds nothing.
        /// </summary>
        protected virtual void AddSections(VisualElement root)
        {
        }

        private VisualElement CreateHeader(Label count)
        {
            var header = new VisualElement();
            header.AddToClassList(TweenClipSections.HEADER_USS_CLASS_NAME);

            header.Add(TweenClipSections.CreateIcon(Icon, TweenClipSections.HEADER_ICON_USS_CLASS_NAME));

            var title = new Label(Title);
            title.AddToClassList(TweenClipSections.TITLE_USS_CLASS_NAME);
            header.Add(title);

            header.Add(count);

            var applyPreset = TweenClipSections.CreateIconButton(
                  EditorIcons.WandSparkles
                , APPLY_PRESET_TOOLTIP
                , OnApplyPresetClicked
                , out _
            );

            header.Add(applyPreset);
            return header;

            void OnApplyPresetClicked()
            {
                ShowPresetPicker(header);
            }
        }

        /// <summary>
        /// Opens the preset picker for every selected clip, so one pick applies to all of them in one Undo step.
        /// </summary>
        private void ShowPresetPicker(VisualElement anchor)
        {
            var assets = new List<PlayableAsset>(targets.Length);

            for (var i = 0; i < targets.Length; i++)
            {
                if (targets[i] is PlayableAsset asset && asset.IsValid())
                {
                    assets.Add(asset);
                }
            }

            if (assets.Count > 0)
            {
                PresetPicker.ShowApply(assets.ToArray(), PresetPicker.GetAnchor(anchor));
            }
        }

        private VisualElement CreateCard(ChannelInfo info, ClipBindingSource source)
        {
            if (info.Shape == ChannelShape.Step)
            {
                return StepChannelCardSection.Create(serializedObject, info);
            }

            if (info.Shape == ChannelShape.Oscillation)
            {
                return OscillationChannelCardSection.Create(serializedObject, info);
            }

            if (info.Shape == ChannelShape.Text)
            {
                return TextChannelCardSection.Create(serializedObject, info, source);
            }

            if (info.Shape == ChannelShape.Character)
            {
                return CharacterChannelCardSection.Create(serializedObject, info);
            }

            if (info.Shape == ChannelShape.Wave)
            {
                return WaveChannelCardSection.Create(serializedObject, info);
            }

            return ChannelCardSection.Create(serializedObject, info, source);
        }

        private void RefreshSummary(Label count, VisualElement warning)
        {
            serializedObject.UpdateIfRequiredOrScript();

            var model = TweenClipModel.Create(serializedObject);

            count.text = TweenClipTexts.FormatChannelCount(
                  model.MinEnabledCount
                , model.EnabledCount
                , model.Channels.Count
            );
            warning.style.display = model.HasEnabledChannel ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
