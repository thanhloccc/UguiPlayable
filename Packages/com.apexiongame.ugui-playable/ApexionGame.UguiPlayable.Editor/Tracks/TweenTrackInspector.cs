using UnityEditor;
using UnityEditor.Timeline;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UIElements;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Editor.Clips;
using ApexionGame.UguiPlayable.Editor.Common;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Editor.Tracks
{
    /// <summary>
    /// The shared track inspector: a header, the Restore Mode segmented control with a one-line explanation, and the
    /// binding warning of the open Timeline as a HelpBox. A concrete inspector only names itself.
    /// </summary>
    /// <remarks>
    /// This replaces Timeline's IMGUI fallback inspector for the track type.
    /// <c>UnityEditor.Editor</c> is written in full because the namespace contains <c>.Editor</c>.
    /// </remarks>
    internal abstract class TweenTrackInspector : UnityEditor.Editor
    {
        // TweenTrack keeps this field private, so nameof is not available to editor code. Its name is serialized user
        // data and cannot change without [FormerlySerializedAs].
        public const string RESTORE_MODE = "_restoreMode";

        // Same reason as RESTORE_MODE: SliderTrack and ScrollbarTrack keep the field private.
        public const string SEND_CALLBACKS = "_sendCallbacks";

        public const string RESTORE_LABEL_USS_CLASS_NAME = TweenClipSections.CARD_USS_CLASS_NAME + "__row-label--wide";

        private const long BINDING_POLL_MILLISECONDS = 500;

        private static readonly string[] s_restoreLabels = { "Keep", "Revert" };

        /// <summary>The title in the header, for example "Rect Transform Track".</summary>
        protected abstract string Title { get; }

        /// <summary>The track icon in the header.</summary>
        protected abstract Texture2D Icon { get; }

        public override VisualElement CreateInspectorGUI()
        {
            var root = TweenClipSections.CreateRoot();
            var restoreMode = serializedObject.FindProperty(RESTORE_MODE);

            root.Add(CreateHeader());
            root.Add(CreateNameRow());

            var control = SegmentedControl.Create(s_restoreLabels).BindEnum(restoreMode, s_restoreLabels.Length);
            var row = TweenClipSections.CreateRow(TweenClipTexts.RESTORE_MODE);
            row[0].AddToClassList(RESTORE_LABEL_USS_CLASS_NAME);
            row.Add(control);
            root.Add(row);

            var note = TweenClipSections.CreateNote(string.Empty);
            root.Add(note);
            root.TrackPropertyValue(restoreMode, p => {
                RefreshNote(note, p);

                // Timeline caches the graph: rebuild it so the new mode applies to the open preview.
                TimelineEditor.Refresh(RefreshReason.ContentsModified);
            });
            RefreshNote(note, restoreMode);

            AddTrackRows(root);

            var warning = new HelpBox(string.Empty, HelpBoxMessageType.Warning);
            root.Add(warning);
            RefreshWarning(warning);
            RefreshLocked(root);
            root.schedule.Execute(() => {
                RefreshWarning(warning);
                RefreshLocked(root);
            }).Every(BINDING_POLL_MILLISECONDS);
            return root;
        }

        /// <summary>
        /// Adds rows after the restore note and before the binding warning. The default adds nothing.
        /// </summary>
        protected virtual void AddTrackRows(VisualElement root)
        {
        }

        /// <summary>
        /// Adds the "Send callbacks" switch bound to <see cref="SEND_CALLBACKS"/>, and its note.
        /// </summary>
        protected void AddSendCallbacksRows(VisualElement root, string noteText)
        {
            var sendCallbacks = serializedObject.FindProperty(SEND_CALLBACKS);
            var toggle = new Toggle().WithSwitchStyle();
            toggle.BindProperty(sendCallbacks);

            var row = TweenClipSections.CreateRow(TweenClipTexts.SEND_CALLBACKS);
            row[0].AddToClassList(RESTORE_LABEL_USS_CLASS_NAME);
            row.Add(toggle);
            root.Add(row);
            root.Add(TweenClipSections.CreateNote(noteText));

            // Timeline caches the graph: rebuild it so the new flag reaches the open mixer.
            root.TrackPropertyValue(sendCallbacks, _ => TimelineEditor.Refresh(RefreshReason.ContentsModified));
        }

        /// <summary>
        /// The warning for the object the track is bound to in the open Timeline, or an empty string.
        /// </summary>
        protected virtual string GetBindingWarning(Object binding)
            => string.Empty;

        private VisualElement CreateHeader()
        {
            var header = new VisualElement();
            header.AddToClassList(TweenClipSections.HEADER_USS_CLASS_NAME);

            header.Add(TweenClipSections.CreateIcon(Icon, TweenClipSections.HEADER_ICON_USS_CLASS_NAME));

            var title = new Label(Title);
            title.AddToClassList(TweenClipSections.TITLE_USS_CLASS_NAME);
            header.Add(title);
            return header;
        }

        // Timeline's own track inspector shows a track name field in the header, which this custom inspector replaces.
        // The name is not bound to m_Name: ObjectNames.SetNameSmart is what Timeline itself uses to rename a track.
        private VisualElement CreateNameRow()
        {
            var row = TweenClipSections.CreateRow(TweenClipTexts.NAME);
            row[0].AddToClassList(RESTORE_LABEL_USS_CLASS_NAME);

            var field = new TextField { isDelayed = true, value = target.name };
            field.AddToClassList(TweenClipSections.FIELD_USS_CLASS_NAME);

            field.RegisterValueChangedCallback(evt => {
                if (string.IsNullOrEmpty(evt.newValue) || target == null)
                {
                    field.SetValueWithoutNotify(target != null ? target.name : evt.previousValue);
                    return;
                }

                Undo.RecordObject(target, "Rename Track");
                ObjectNames.SetNameSmart(target, evt.newValue);
                EditorUtility.SetDirty(target);
                TimelineEditor.Refresh(RefreshReason.ContentsModified);
            });

            row.Add(field);
            return row;
        }

        // A track that is locked in Timeline (itself or through a parent group) must not be edited here either.
        private void RefreshLocked(VisualElement root)
        {
            var locked = target is TrackAsset track && track.lockedInHierarchy;

            root.SetEnabled(locked == false);
            root.tooltip = locked ? TweenClipTexts.TRACK_LOCKED : string.Empty;
        }

        private static void RefreshNote(Label note, SerializedProperty restoreMode)
        {
            var mode = (RestoreMode)restoreMode.enumValueIndex;
            var isRevert = mode == RestoreMode.Revert;

            note.text = isRevert ? TweenClipTexts.RESTORE_REVERT_NOTE : TweenClipTexts.RESTORE_KEEP_NOTE;
        }

        private void RefreshWarning(HelpBox warning)
        {
            var director = TimelineEditor.inspectedDirector;
            var binding = director != null && target is TrackAsset track ? director.GetGenericBinding(track) : null;
            var text = binding == null ? string.Empty : GetBindingWarning(binding);

            warning.text = text;
            warning.style.display = string.IsNullOrEmpty(text) ? DisplayStyle.None : DisplayStyle.Flex;
        }
    }
}
