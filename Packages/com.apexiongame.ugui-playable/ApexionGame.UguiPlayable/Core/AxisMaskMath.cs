using UnityEngine;

namespace ApexionGame.UguiPlayable.Core
{
    /// <summary>
    /// Per-axis helpers for <see cref="AxisMask"/>.
    /// </summary>
    internal static class AxisMaskMath
    {
        /// <summary>Returns <paramref name="current"/> with the <paramref name="axes"/> taken from
        /// <paramref name="replacement"/>.</summary>
        public static Vector3 Merge(Vector3 current, Vector3 replacement, AxisMask axes)
        {
            if ((axes & AxisMask.X) != 0)
            {
                current.x = replacement.x;
            }

            if ((axes & AxisMask.Y) != 0)
            {
                current.y = replacement.y;
            }

            if ((axes & AxisMask.Z) != 0)
            {
                current.z = replacement.z;
            }

            return current;
        }

        /// <inheritdoc cref="Merge(Vector3, Vector3, AxisMask)"/>
        public static Vector2 Merge(Vector2 current, Vector2 replacement, AxisMask axes)
        {
            if ((axes & AxisMask.X) != 0)
            {
                current.x = replacement.x;
            }

            if ((axes & AxisMask.Y) != 0)
            {
                current.y = replacement.y;
            }

            return current;
        }

        /// <summary>Maps each euler component to (-180, 180].</summary>
        public static Vector3 NormalizeEuler(Vector3 euler)
            => new(Mathf.DeltaAngle(0f, euler.x), Mathf.DeltaAngle(0f, euler.y), Mathf.DeltaAngle(0f, euler.z));
    }
}
