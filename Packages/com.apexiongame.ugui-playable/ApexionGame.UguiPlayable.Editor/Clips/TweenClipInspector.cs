using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace ApexionGame.UguiPlayable.Editor.Clips
{
    /// <summary>
    /// The shared clip inspector: a header (track icon, title, "N of M channels on"), a warning when no channel is on,
    /// then one card per channel found under <c>_template</c>. A concrete clip inspector only names itself.
    /// </summary>
    /// <remarks>
    /// Timeline hosts this in an <c>InspectorElement</c> inside its clip inspector, so
    /// <see cref="CreateInspectorGUI"/> is the clip inspector. <c>UnityEditor.Editor</c> is written in full because
    /// the namespace contains <c>.Editor</c>.
    /// </remarks>
    internal abstract class TweenClipInspector : UnityEditor.Editor
    {
        private const long BINDING_POLL_MILLISECONDS = 500;

        /// <summary>The title in the header, for example "Rect Transform Clip".</summary>
        protected abstract string Title { get; }

        /// <summary>The track icon in the header.</summary>
        protected abstract Texture2D Icon { get; }

        public override VisualElement CreateInspectorGUI()
        {
            var root = TweenClipSections.CreateRoot();

            var model = TweenClipModel.Create(serializedObject);
            var source = new ClipBindingSource(target);

            var count = new Label();
            count.AddToClassList(TweenClipSections.COUNT_USS_CLASS_NAME);

            var warning = new HelpBox(TweenClipTexts.NO_CHANNEL_ENABLED, HelpBoxMessageType.Warning);

            root.Add(CreateHeader(count));
            root.Add(warning);

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

            source.Refresh();
            root.schedule.Execute(source.Refresh).Every(BINDING_POLL_MILLISECONDS);
            return root;
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
            return header;
        }

        private VisualElement CreateCard(ChannelInfo info, ClipBindingSource source)
        {
            if (info.Shape == ChannelShape.Step)
            {
                return StepChannelCardSection.Create(serializedObject, info);
            }

            return ChannelCardSection.Create(serializedObject, info, source);
        }

        private void RefreshSummary(Label count, VisualElement warning)
        {
            serializedObject.UpdateIfRequiredOrScript();

            var model = TweenClipModel.Create(serializedObject);

            count.text = TweenClipTexts.FormatChannelCount(model.EnabledCount, model.Channels.Count);
            warning.style.display = model.HasEnabledChannel ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
