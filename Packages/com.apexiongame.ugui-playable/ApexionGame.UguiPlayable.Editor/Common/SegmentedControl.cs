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
        /// A click writes the index in one Undo step.
        /// </summary>
        public static ToggleButtonGroup BindEnum(this ToggleButtonGroup self, SerializedProperty property, int length)
        {
            var serializedObject = property.serializedObject;

            self.SetValueWithoutNotify(ToState(property.enumValueIndex, length));

            self.RegisterValueChangedCallback(evt => {
                var index = GetSelectedIndex(evt.newValue);

                if (index < 0 || index == property.enumValueIndex)
                {
                    return;
                }

                serializedObject.Update();
                property.enumValueIndex = index;
                serializedObject.ApplyModifiedProperties();
            });

            self.TrackPropertyValue(property, p => {
                self.SetValueWithoutNotify(ToState(p.enumValueIndex, length));
            });

            return self;
        }

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
