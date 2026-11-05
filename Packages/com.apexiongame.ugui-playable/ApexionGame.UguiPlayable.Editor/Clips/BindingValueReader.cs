using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Editor.Clips
{
    /// <summary>
    /// Reads the current value a channel animates from a bound object, for "Set from binding". Vectors fill x, y, z;
    /// a color fills x, y, z, w as r, g, b, a; a float fills x.
    /// </summary>
    internal static class BindingValueReader
    {
        public static bool TryRead(string channelName, Object binding, out Vector4 value)
        {
            value = default;

            if (binding == null)
            {
                return false;
            }

            switch (channelName)
            {
                case ChannelPresentation.ANCHORED_POSITION:
                    return TryReadRect(binding, rect => rect.anchoredPosition3D, out value);

                case ChannelPresentation.SIZE_DELTA:
                    return TryReadRect(binding, rect => rect.sizeDelta, out value);

                case ChannelPresentation.ROTATION:
                    return TryReadRect(binding, rect => Normalize(rect.localEulerAngles), out value);

                case ChannelPresentation.SCALE:
                    return TryReadRect(binding, rect => rect.localScale, out value);

                case ChannelPresentation.PIVOT:
                    return TryReadRect(binding, rect => rect.pivot, out value);

                case ChannelPresentation.ANCHOR_MIN:
                    return TryReadRect(binding, rect => rect.anchorMin, out value);

                case ChannelPresentation.ANCHOR_MAX:
                    return TryReadRect(binding, rect => rect.anchorMax, out value);

                case ChannelPresentation.COLOR:
                    return TryReadGraphic(binding, graphic => graphic.color, out value);

                case ChannelPresentation.ALPHA:
                    return TryReadAlpha(binding, out value);

                case ChannelPresentation.FILL_AMOUNT:
                    return TryReadFill(binding, out value);

                default:
                    return false;
            }
        }

        public static bool CanRead(string channelName, Object binding)
            => TryRead(channelName, binding, out _);

        // Euler angles read as 0..360. A from/to authored as -90 would look like 270, so map each axis to -180..180.
        private static Vector3 Normalize(Vector3 euler)
            => new(
                  Mathf.DeltaAngle(0f, euler.x)
                , Mathf.DeltaAngle(0f, euler.y)
                , Mathf.DeltaAngle(0f, euler.z)
            );

        private static bool TryReadRect(Object binding, System.Func<RectTransform, Vector4> read, out Vector4 value)
        {
            if (TryResolve<RectTransform>(binding, out var rect))
            {
                value = read(rect);
                return true;
            }

            value = default;
            return false;
        }

        private static bool TryReadGraphic(Object binding, System.Func<Graphic, Vector4> read, out Vector4 value)
        {
            if (TryResolve<Graphic>(binding, out var graphic))
            {
                value = read(graphic);
                return true;
            }

            value = default;
            return false;
        }

        // The binding type decides first, so a Graphic track on an object that also has a CanvasGroup reads the
        // Graphic's alpha.
        private static bool TryReadAlpha(Object binding, out Vector4 value)
        {
            if (binding is Graphic graphic)
            {
                value = new Vector4(graphic.color.a, 0f, 0f, 0f);
                return true;
            }

            if (TryResolve<CanvasGroup>(binding, out var group))
            {
                value = new Vector4(group.alpha, 0f, 0f, 0f);
                return true;
            }

            if (TryResolve<Graphic>(binding, out graphic))
            {
                value = new Vector4(graphic.color.a, 0f, 0f, 0f);
                return true;
            }

            value = default;
            return false;
        }

        private static bool TryReadFill(Object binding, out Vector4 value)
        {
            if (TryResolve<Image>(binding, out var image))
            {
                value = new Vector4(image.fillAmount, 0f, 0f, 0f);
                return true;
            }

            value = default;
            return false;
        }

        private static bool TryResolve<T>(Object binding, out T result)
            where T : Component
        {
            if (binding is T direct)
            {
                result = direct;
                return true;
            }

            if (binding is GameObject gameObject)
            {
                return gameObject.TryGetComponent(out result);
            }

            if (binding is Component component)
            {
                return component.TryGetComponent(out result);
            }

            result = null;
            return false;
        }
    }
}
