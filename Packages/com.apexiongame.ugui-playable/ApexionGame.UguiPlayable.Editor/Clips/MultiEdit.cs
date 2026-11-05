using System;
using UnityEditor;
using UnityEngine.UIElements;

namespace ApexionGame.UguiPlayable.Editor.Clips
{
    /// <summary>
    /// Helpers for inspecting several clips at once. The inspector's own <see cref="SerializedObject"/> holds every
    /// selected clip, so a plain write goes to all of them as one Undo step. A write that needs a different value per
    /// clip (a scaled vector, a new seed, one axis flag) goes through <see cref="WriteEach"/> instead.
    /// </summary>
    internal static class MultiEdit
    {
        private const long POLL_MILLISECONDS = 250;

        /// <summary>
        /// True when the channel is on for at least one selected clip. A card is dimmed and collapsed only when this
        /// is false.
        /// </summary>
        public static bool IsAnyOn(SerializedProperty enabled)
            => enabled.hasMultipleDifferentValues || enabled.boolValue;

        /// <summary>
        /// Calls <paramref name="refresh"/> every few hundred milliseconds while several clips are inspected. The
        /// property trackers only see the first clip's value change, so a change of the others (an Undo, a write by
        /// another control) would not reach the card otherwise. A single clip needs no poll.
        /// </summary>
        public static void PollWhileMultiple(VisualElement element, SerializedObject serializedObject, Action refresh)
        {
            if (serializedObject.isEditingMultipleObjects)
            {
                element.schedule.Execute(refresh).Every(POLL_MILLISECONDS);
            }
        }

        /// <summary>
        /// Sets a negative int or float to 0 on every clip where it is negative; the other clips are not touched.
        /// </summary>
        public static void ClampToZero(SerializedObject serializedObject, SerializedProperty property)
        {
            var path = property.propertyPath;
            var isInt = property.propertyType == SerializedPropertyType.Integer;
            var negative = ReadEach(serializedObject, path, p => isInt ? p.intValue < 0 : p.floatValue < 0f);

            if (Array.IndexOf(negative, true) < 0)
            {
                return;
            }

            WriteEach(serializedObject, (each, index) => {
                if (negative[index] == false)
                {
                    return;
                }

                var target = each.FindProperty(path);

                if (isInt)
                {
                    target.intValue = 0;
                }
                else
                {
                    target.floatValue = 0f;
                }
            });
        }

        /// <summary>True when the bool is on for every selected clip.</summary>
        public static bool IsAllOn(SerializedProperty flag)
            => flag.hasMultipleDifferentValues == false && flag.boolValue;

        /// <summary>
        /// Reads <paramref name="path"/> from every target, in target order. With one target it reads that target.
        /// </summary>
        public static T[] ReadEach<T>(SerializedObject serializedObject, string path, Func<SerializedProperty, T> read)
        {
            if (serializedObject.isEditingMultipleObjects == false)
            {
                serializedObject.UpdateIfRequiredOrScript();
                return new[] { read(serializedObject.FindProperty(path)) };
            }

            var targets = serializedObject.targetObjects;
            var result = new T[targets.Length];

            for (var i = 0; i < targets.Length; i++)
            {
                using var each = new SerializedObject(targets[i]);
                result[i] = read(each.FindProperty(path));
            }

            return result;
        }

        /// <summary>
        /// Writes to every target with its own <see cref="SerializedObject"/>, so each one can get its own value, and
        /// collapses the writes into one Undo step. With one target it writes through
        /// <paramref name="serializedObject"/>.
        /// </summary>
        public static void WriteEach(SerializedObject serializedObject, Action<SerializedObject, int> write)
        {
            if (serializedObject.isEditingMultipleObjects == false)
            {
                serializedObject.Update();
                write(serializedObject, 0);
                serializedObject.ApplyModifiedProperties();
                return;
            }

            Undo.IncrementCurrentGroup();

            var group = Undo.GetCurrentGroup();
            var targets = serializedObject.targetObjects;

            for (var i = 0; i < targets.Length; i++)
            {
                using var each = new SerializedObject(targets[i]);
                write(each, i);
                each.ApplyModifiedProperties();
            }

            Undo.CollapseUndoOperations(group);
            serializedObject.Update();
        }
    }
}
