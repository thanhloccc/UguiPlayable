using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ApexionGame.UguiPlayable.Editor.Common
{
    /// <summary>
    /// A single-choice segmented control built on <see cref="ToggleButtonGroup"/> and bound to an enum property whose
    /// values are the contiguous indices 0..n-1.
    /// </summary>
    internal static class SegmentedControl
    {
        public const string USS_CLASS_NAME = "ugp-segmented";

        private const long POLL_MILLISECONDS = 250;

        /// <summary>
        /// Creates a group with one button per label. The first button is selected.
        /// </summary>
        public static ToggleButtonGroup Create(IReadOnlyList<string> labels)
        {
            var group = new ToggleButtonGroup {
                isMultipleSelection = false,
                allowEmptySelection = false,
            };

            CommonStyleSheetPaths.AddTo(group);
            group.AddToClassList(USS_CLASS_NAME);

            for (var i = 0; i < labels.Count; i++)
            {
                group.Add(new Button { text = labels[i] });
            }

            group.SetValueWithoutNotify(ToState(0, labels.Count));
            return group;
        }

        /// <summary>
        /// Binds <paramref name="self"/>, which has <paramref name="length"/> buttons, to <paramref name="property"/>.
        /// A click writes the index in one Undo step. When several objects are edited and their values differ, no
        /// button is selected, and a click sets the value on all of them.
        /// </summary>
        public static ToggleButtonGroup BindEnum(this ToggleButtonGroup self, SerializedProperty property, int length)
        {
            var serializedObject = property.serializedObject;

            self.SetValueWithoutNotify(ToState(property, length));

            self.RegisterValueChangedCallback(evt => {
                var index = GetSelectedIndex(evt.newValue);

                if (index < 0)
                {
                    return;
                }

                serializedObject.Update();

                if (property.hasMultipleDifferentValues == false && index == property.enumValueIndex)
                {
                    return;
                }

                property.enumValueIndex = index;
                serializedObject.ApplyModifiedProperties();
            });

            self.TrackPropertyValue(property, p => {
                self.SetValueWithoutNotify(ToState(p, length));
            });

            // The tracker sees only the first object's value; the mixed state follows the others by polling.
            if (serializedObject.isEditingMultipleObjects)
            {
                self.schedule.Execute(() => {
                    serializedObject.UpdateIfRequiredOrScript();
                    self.SetValueWithoutNotify(ToState(property, length));
                }).Every(POLL_MILLISECONDS);
            }

            return self;
        }

        /// <summary>
        /// The state that shows <paramref name="property"/>: the selected index, or no selection when the edited
        /// objects disagree.
        /// </summary>
        public static ToggleButtonGroupState ToState(SerializedProperty property, int length)
            => ToState(property.hasMultipleDifferentValues ? -1 : property.enumValueIndex, length);

        public static ToggleButtonGroupState ToState(int index, int length)
            => new(index < 0 ? 0UL : 1UL << index, length);

        public static int GetSelectedIndex(ToggleButtonGroupState state)
        {
            for (var i = 0; i < state.length; i++)
            {
                if (state[i])
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
