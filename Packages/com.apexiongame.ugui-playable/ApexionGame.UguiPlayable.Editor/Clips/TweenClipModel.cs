using System.Collections.Generic;
using UnityEditor;

namespace ApexionGame.UguiPlayable.Editor.Clips
{
    /// <summary>
    /// Discovers the channels under a clip's <c>_template</c> by shape, so one inspector serves every tween clip and a
    /// new channel on a behaviour needs no editor change. Holds plain data and no UI Toolkit types.
    /// </summary>
    internal sealed class TweenClipModel
    {
        // The clip, behaviour and channel structs keep these fields private, so nameof is not available to editor
        // code. Their names are serialized user data and cannot change without [FormerlySerializedAs].
        public const string TEMPLATE = "_template";
        public const string ENABLED = "_enabled";
        public const string MODE = "_mode";
        public const string FROM = "_from";
        public const string TO = "_to";
        public const string EASE = "_ease";
        public const string IGNORED_AXES = "_ignoredAxes";
        public const string VALUE = "_value";
        public const string THRESHOLD = "_threshold";

        private const string CONSTRAIN_FROM_SUFFIX = "ConstrainFrom";
        private const string CONSTRAIN_TO_SUFFIX = "ConstrainTo";

        private readonly List<ChannelInfo> _channels;
        private readonly int _enabledCount;

        public TweenClipModel(List<ChannelInfo> channels, int enabledCount)
        {
            _channels = channels;
            _enabledCount = enabledCount;
        }

        public IReadOnlyList<ChannelInfo> Channels => _channels;

        public int EnabledCount => _enabledCount;

        public bool HasEnabledChannel => _enabledCount > 0;

        /// <summary>
        /// Reads the channels of the first target of <paramref name="serializedObject"/>.
        /// </summary>
        public static TweenClipModel Create(SerializedObject serializedObject)
        {
            var channels = new List<ChannelInfo>();
            var enabledCount = 0;
            var template = serializedObject.FindProperty(TEMPLATE);

            if (template == null)
            {
                return new TweenClipModel(channels, enabledCount);
            }

            var iterator = template.Copy();
            var end = template.GetEndProperty();
            var enter = true;

            while (iterator.NextVisible(enter) && SerializedProperty.EqualContents(iterator, end) == false)
            {
                enter = false;

                if (iterator.propertyType != SerializedPropertyType.Generic)
                {
                    continue;
                }

                if (TryCreateInfo(template, iterator, out var info, out var enabled) == false)
                {
                    continue;
                }

                channels.Add(info);

                if (enabled)
                {
                    enabledCount++;
                }
            }

            return new TweenClipModel(channels, enabledCount);
        }

        public static string GetConstrainName(string channelName, bool isFrom)
            => channelName + (isFrom ? CONSTRAIN_FROM_SUFFIX : CONSTRAIN_TO_SUFFIX);

        public static string GetEnabledPath(string channelName)
            => $"{TEMPLATE}.{channelName}.{ENABLED}";

        private static bool TryCreateInfo(
              SerializedProperty template
            , SerializedProperty child
            , out ChannelInfo info
            , out bool enabled
        )
        {
            info = default;
            enabled = false;

            var enabledProperty = child.FindPropertyRelative(ENABLED);

            if (enabledProperty == null || enabledProperty.propertyType != SerializedPropertyType.Boolean)
            {
                return false;
            }

            var from = child.FindPropertyRelative(FROM);
            var to = child.FindPropertyRelative(TO);
            var mode = child.FindPropertyRelative(MODE);

            if (from != null && to != null && mode != null && TryGetValueKind(from, out var valueKind))
            {
                var hasConstrain = template.FindPropertyRelative(GetConstrainName(child.name, true)) != null
                    && template.FindPropertyRelative(GetConstrainName(child.name, false)) != null;

                info = new ChannelInfo(child.name, ChannelShape.Tween, valueKind, hasConstrain);
                enabled = enabledProperty.boolValue;
                return true;
            }

            if (child.FindPropertyRelative(THRESHOLD) != null && child.FindPropertyRelative(VALUE) != null)
            {
                info = new ChannelInfo(child.name, ChannelShape.Step, ChannelValueKind.Float, false);
                enabled = enabledProperty.boolValue;
                return true;
            }

            return false;
        }

        private static bool TryGetValueKind(SerializedProperty from, out ChannelValueKind kind)
        {
            switch (from.propertyType)
            {
                case SerializedPropertyType.Float:
                    kind = ChannelValueKind.Float;
                    return true;

                case SerializedPropertyType.Vector2:
                    kind = ChannelValueKind.Vector2;
                    return true;

                case SerializedPropertyType.Vector3:
                    kind = ChannelValueKind.Vector3;
                    return true;

                case SerializedPropertyType.Vector4:
                    kind = ChannelValueKind.Vector4;
                    return true;

                case SerializedPropertyType.Color:
                    kind = ChannelValueKind.Color;
                    return true;

                default:
                    kind = default;
                    return false;
            }
        }
    }
}
