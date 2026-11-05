using UnityEngine;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Editor.Clips
{
    /// <summary>
    /// The "constrain proportions" rule of the from/to vector fields.
    /// </summary>
    internal static class ConstrainMath
    {
        /// <summary>
        /// Returns <paramref name="next"/>, where the other axes that are not ignored are scaled by the ratio the
        /// edited axis changed by. Only the first <paramref name="dimension"/> axes count. A zero previous value on
        /// the edited axis has no ratio, so the edit applies to that axis alone, and so does an edit of an ignored
        /// axis.
        /// </summary>
        public static Vector3 Apply(Vector3 previous, Vector3 next, AxisMask ignored, int dimension)
            => Apply(previous, previous, next, ignored, dimension);

        /// <summary>
        /// Like <see cref="Apply(Vector3, Vector3, AxisMask, int)"/>, but the ratio and the other axes come from
        /// <paramref name="baseValue"/>, the vector when the edit started, not from the previous event. Typing "0" and
        /// then "0.5" therefore scales the others from the base by 0.5 / base, instead of wiping them to zero at the
        /// first keystroke. <paramref name="previous"/> only identifies which axis changed.
        /// </summary>
        public static Vector3 Apply(
              Vector3 baseValue
            , Vector3 previous
            , Vector3 next
            , AxisMask ignored
            , int dimension
        )
        {
            var changed = -1;

            for (var i = 0; i < dimension; i++)
            {
                if (Mathf.Approximately(previous[i], next[i]) == false)
                {
                    changed = i;
                    break;
                }
            }

            if (changed < 0 || IsIgnored(ignored, changed) || Mathf.Approximately(baseValue[changed], 0f))
            {
                return next;
            }

            var ratio = next[changed] / baseValue[changed];
            var result = next;

            for (var i = 0; i < dimension; i++)
            {
                if (i == changed || IsIgnored(ignored, i))
                {
                    continue;
                }

                result[i] = baseValue[i] * ratio;
            }

            return result;
        }

        public static bool IsIgnored(AxisMask ignored, int axis)
            => (ignored & (AxisMask)(1 << axis)) != AxisMask.None;
    }
}
