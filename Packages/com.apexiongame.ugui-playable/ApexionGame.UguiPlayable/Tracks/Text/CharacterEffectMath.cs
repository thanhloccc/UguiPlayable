using System;
using TMPro;
using UnityEngine;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Text
{
    /// <summary>
    /// The pure math of <see cref="TmpCharacterTrack"/>: the rank of each character in a clip's order, the
    /// per-character progress, the transform about the pivot, the tint, the wave and the shake. Nothing here allocates
    /// or uses a random generator, so scrubbing always gives the same mesh.
    /// </summary>
    internal static class CharacterEffectMath
    {
        private const float TWO_PI = 2f * Mathf.PI;

        /// <summary>
        /// Returns the rank, from 0 to <c>n − 1</c>, of the visible character <paramref name="k"/> (its place in
        /// reading order among the <paramref name="n"/> visible characters of the range) for every order but
        /// <see cref="CharacterOrder.Random"/>, which <see cref="FillRanks"/> sorts. Centre out and edges in give
        /// mirrored characters the same rank, so both halves move together.
        /// </summary>
        public static int Rank(CharacterOrder order, int k, int n)
        {
            var last = n - 1;

            switch (order)
            {
                case CharacterOrder.Backward:
                    return last - k;

                case CharacterOrder.CenterOut:
                    return Math.Abs(2 * k - last);

                case CharacterOrder.EdgesIn:
                    return last - Math.Abs(2 * k - last);

                default:
                    return k;
            }
        }

        /// <summary>
        /// Fills <paramref name="ranks"/>[k] for the <paramref name="n"/> visible characters whose character indices
        /// are <paramref name="indices"/>[0..n). The random order sorts the characters by
        /// <see cref="Hash"/>(<paramref name="seed"/>, character index) using the two scratch arrays, which must hold
        /// at least <paramref name="n"/> items.
        /// </summary>
        public static void FillRanks(
              CharacterOrder order
            , int seed
            , int[] indices
            , int n
            , int[] ranks
            , uint[] keys
            , int[] items
        )
        {
            if (order != CharacterOrder.Random)
            {
                for (var k = 0; k < n; k++)
                {
                    ranks[k] = Rank(order, k, n);
                }

                return;
            }

            for (var k = 0; k < n; k++)
            {
                keys[k] = Hash(seed, indices[k]);
                items[k] = k;
            }

            Array.Sort(keys, items, 0, n);

            for (var r = 0; r < n; r++)
            {
                ranks[items[r]] = r;
            }
        }

        /// <summary>Returns <c>u = rank / (n − 1)</c>, or 0 when the range has one visible character or none.</summary>
        public static float Normalize(int rank, int n)
            => n <= 1 ? 0f : rank / (float)(n - 1);

        /// <summary>
        /// Returns <c>u = (rank − min) / (max − min)</c> over the actual rank range, or 0 when the range is a single
        /// rank. Forward, backward and random orders span 0 to <c>n − 1</c>; centre out and edges in span 0 to
        /// <c>n − 1</c> only for an odd count, so an even count needs the real range to reach 0 and 1.
        /// </summary>
        public static float Normalize(int rank, int min, int max)
            => max <= min ? 0f : (rank - min) / (float)(max - min);

        /// <summary>
        /// Returns whether TMP would draw the character as part of the text: a sprite, or a character that is neither
        /// white space nor a zero-width space, soft hyphen or end-of-text control. It has no visibility term on
        /// purpose, so a character a typewriter has not revealed yet keeps its place in the order. A tab or a
        /// preserve-whitespace space that TMP draws gets no rank and is not animated.
        /// </summary>
        public static bool IsRenderable(in TMP_CharacterInfo c)
            => c.elementType == TMP_TextElementType.Sprite
                || (char.IsWhiteSpace(c.character) == false
                    && c.character != '\u200B'
                    && c.character != '\u00AD'
                    && c.character != '\u0003');

        /// <summary>
        /// Returns a character's progress: <c>clamp01((p − s·u) / (1 − s))</c>, with <paramref name="stagger"/>
        /// clamped to [0, <see cref="TmpCharacterBehaviour.MAX_STAGGER"/>].
        /// </summary>
        public static float CharacterProgress(float progress, float stagger, float u)
        {
            var s = Mathf.Clamp(stagger, 0f, TmpCharacterBehaviour.MAX_STAGGER);

            return Mathf.Clamp01((progress - s * u) / (1f - s));
        }

        /// <summary>
        /// Returns the unit wave of a character: <c>sin(2π · (cycles · p − phasePerCharacter · rank))</c>, or its
        /// absolute value for <see cref="WaveShape.Bounce"/>.
        /// </summary>
        public static float Wave(WaveShape shape, float cycles, float phasePerCharacter, float progress, int rank)
        {
            var value = Mathf.Sin(TWO_PI * (cycles * progress - phasePerCharacter * rank));

            return shape == WaveShape.Bounce ? Mathf.Abs(value) : value;
        }

        /// <summary>
        /// Returns the shake offset of character <paramref name="characterIndex"/> at clip progress
        /// <paramref name="progress"/>: the channel's shake with its seed mixed with the index, so every character
        /// gets its own noise.
        /// </summary>
        public static Vector3 Shake(in OscillationChannel channel, float progress, int characterIndex)
        {
            var seed = unchecked((int)Hash(channel.Seed, characterIndex));
            var unit = OscillationMath.Shake(
                  Mathf.Clamp01(progress)
                , channel.Frequency
                , seed
                , channel.Randomness
                , channel.FadeOut
            );

            var strength = channel.Strength;
            var ignored = channel.IgnoredAxes;

            return new Vector3(
                  (ignored & AxisMask.X) != 0 ? 0f : strength.x * unit.x
                , (ignored & AxisMask.Y) != 0 ? 0f : strength.y * unit.y
                , (ignored & AxisMask.Z) != 0 ? 0f : strength.z * unit.z
            );
        }

        /// <summary>
        /// Returns the point a character rotates and scales about: the centre of its box, or the middle of its
        /// baseline.
        /// </summary>
        public static Vector2 Pivot(CharacterPivot pivot, Vector3 bottomLeft, Vector3 topRight, float baseLine)
        {
            var x = (bottomLeft.x + topRight.x) * 0.5f;

            return pivot == CharacterPivot.Baseline
                ? new Vector2(x, baseLine)
                : new Vector2(x, (bottomLeft.y + topRight.y) * 0.5f);
        }

        /// <summary>
        /// Returns <paramref name="vertex"/> moved by <c>T(offset) · R(rotation) · S(scale)</c> about
        /// <paramref name="pivot"/>. The rotation is given as its sine and cosine; Z is kept.
        /// </summary>
        public static Vector3 Transform(
              Vector3 vertex
            , Vector2 pivot
            , Vector2 offset
            , float sin
            , float cos
            , Vector2 scale
        )
        {
            var x = (vertex.x - pivot.x) * scale.x;
            var y = (vertex.y - pivot.y) * scale.y;

            return new Vector3(
                  pivot.x + offset.x + x * cos - y * sin
                , pivot.y + offset.y + x * sin + y * cos
                , vertex.z
            );
        }

        /// <summary>
        /// Returns <paramref name="source"/> multiplied by <paramref name="tint"/>, with the alpha also multiplied by
        /// <paramref name="alpha"/>. Each channel is clamped to [0, 255].
        /// </summary>
        public static Color32 Tint(Color32 source, Color tint, float alpha)
        {
            return new Color32(
                  ToByte(source.r * tint.r)
                , ToByte(source.g * tint.g)
                , ToByte(source.b * tint.b)
                , ToByte(source.a * tint.a * alpha)
            );
        }

        /// <summary>
        /// An integer hash of (<paramref name="seed"/>, <paramref name="index"/>), with the same mixing as
        /// <see cref="OscillationMath"/>'s noise hash.
        /// </summary>
        public static uint Hash(int seed, int index)
        {
            unchecked
            {
                var h = (uint)seed * 0x9E3779B1u;
                h ^= 0x7F4A7C15u;
                h = (h ^ (h >> 15)) * 0x2C1B3C6Du;
                h ^= (uint)index * 0xC2B2AE35u;
                h = (h ^ (h >> 13)) * 0x297A2D39u;
                h ^= h >> 16;
                h *= 0x85EBCA6Bu;
                h ^= h >> 13;

                return h;
            }
        }

        private static byte ToByte(float value)
            => (byte)Mathf.Clamp(Mathf.RoundToInt(value), 0, 255);
    }
}
