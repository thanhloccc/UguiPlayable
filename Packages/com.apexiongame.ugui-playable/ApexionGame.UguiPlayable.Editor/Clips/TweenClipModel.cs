using System;
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
        public const string SCRAMBLE = "_scramble";
        public const string CUSTOM_SCRAMBLE_CHARS = "_customScrambleChars";
        public const string SCRAMBLE_RATE = "_scrambleRate";
        public const string DECIMALS = "_decimals";
        public const string GROUPING = "_grouping";
        public const string GROUP_SEPARATOR = "_groupSeparator";
        public const string DECIMAL_SEPARATOR = "_decimalSeparator";
        public const string PREFIX = "_prefix";
        public const string SUFFIX = "_suffix";
        public const string TARGET = "_target";
        public const string STRENGTH = "_strength";
        public const string FREQUENCY = "_frequency";
        public const string SHAPE = "_shape";
        public const string AMPLITUDE = "_amplitude";
        public const string CYCLES = "_cycles";
        public const string PHASE_PER_CHARACTER = "_phasePerCharacter";

        private static readonly string[] s_gradientCorners = { "topLeft", "topRight", "bottomLeft", "bottomRight" };

        private const string CONSTRAIN_FROM_SUFFIX = "ConstrainFrom";
        private const string CONSTRAIN_TO_SUFFIX = "ConstrainTo";

        private readonly List<ChannelInfo> _channels;
        private readonly int _minEnabledCount;
        private readonly int _maxEnabledCount;

        public TweenClipModel(List<ChannelInfo> channels, int enabledCount)
            : this(channels, enabledCount, enabledCount)
        {
        }

        public TweenClipModel(List<ChannelInfo> channels, int minEnabledCount, int maxEnabledCount)
        {
            _channels = channels;
            _minEnabledCount = minEnabledCount;
            _maxEnabledCount = maxEnabledCount;
        }

        public IReadOnlyList<ChannelInfo> Channels => _channels;

        /// <summary>The most channels any selected clip has on; the count of the clip when only one is selected.</summary>
        public int EnabledCount => _maxEnabledCount;

        /// <summary>The fewest channels any selected clip has on.</summary>
        public int MinEnabledCount => _minEnabledCount;

        /// <summary>True when at least one selected clip has a channel on.</summary>
        public bool HasEnabledChannel => _maxEnabledCount > 0;

        /// <summary>
        /// Reads the channels of the first target of <paramref name="serializedObject"/>, and the enabled count of
        /// every target (the clips share one type, so they share the channels).
        /// </summary>
        public static TweenClipModel Create(SerializedObject serializedObject)
        {
            var model = CreateForFirstTarget(serializedObject);

            if (serializedObject.isEditingMultipleObjects == false)
            {
                return model;
            }

            var min = int.MaxValue;
            var max = 0;
            var targets = serializedObject.targetObjects;

            for (var i = 0; i < targets.Length; i++)
            {
                using var each = new SerializedObject(targets[i]);

                var count = CreateForFirstTarget(each).EnabledCount;

                min = Math.Min(min, count);
                max = Math.Max(max, count);
            }

            return new TweenClipModel(new List<ChannelInfo>(model._channels), min == int.MaxValue ? 0 : min, max);
        }

        private static TweenClipModel CreateForFirstTarget(SerializedObject serializedObject)
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

            // The number counter has double From/To like a float tween, so it is detected first by its decimals.
            if (child.FindPropertyRelative(DECIMALS) != null)
            {
                info = new ChannelInfo(child.name, ChannelShape.Counter, ChannelValueKind.Float, false);
                enabled = enabledProperty.boolValue;
                return true;
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

            if (from != null && to != null && mode == null && TryGetValueKind(from, out var characterKind))
            {
                var hasConstrain = template.FindPropertyRelative(GetConstrainName(child.name, true)) != null
                    && template.FindPropertyRelative(GetConstrainName(child.name, false)) != null;

                info = new ChannelInfo(child.name, ChannelShape.Character, characterKind, hasConstrain);
                enabled = enabledProperty.boolValue;
                return true;
            }

            if (from != null
                && to != null
                && mode != null
                && from.propertyType == SerializedPropertyType.String
                && to.propertyType == SerializedPropertyType.String
                && child.FindPropertyRelative(SCRAMBLE) != null)
            {
                info = new ChannelInfo(child.name, ChannelShape.Text, ChannelValueKind.Float, false);
                enabled = enabledProperty.boolValue;
                return true;
            }

            if (child.FindPropertyRelative(TARGET) != null
                && child.FindPropertyRelative(STRENGTH) != null
                && child.FindPropertyRelative(FREQUENCY) != null)
            {
                info = new ChannelInfo(child.name, ChannelShape.Oscillation, ChannelValueKind.Vector3, false);
                enabled = enabledProperty.boolValue;
                return true;
            }

            if (child.FindPropertyRelative(TARGET) != null
                && child.FindPropertyRelative(AMPLITUDE) != null
                && child.FindPropertyRelative(CYCLES) != null
            )
            {
                info = new ChannelInfo(child.name, ChannelShape.Wave, ChannelValueKind.Vector3, false);
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

                case SerializedPropertyType.Generic when IsVertexGradient(from):
                    kind = ChannelValueKind.VertexGradient;
                    return true;

                default:
                    kind = default;
                    return false;
            }
        }

        // A VertexGradient is a Generic struct with four Color corners.
        private static bool IsVertexGradient(SerializedProperty from)
        {
            for (var i = 0; i < s_gradientCorners.Length; i++)
            {
                var corner = from.FindPropertyRelative(s_gradientCorners[i]);

                if (corner == null || corner.propertyType != SerializedPropertyType.Color)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
