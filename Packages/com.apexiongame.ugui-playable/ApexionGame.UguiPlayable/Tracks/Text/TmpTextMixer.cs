using System;
using TMPro;
using UnityEngine;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Tracks.Graphics;

namespace ApexionGame.UguiPlayable.Tracks.Text
{
    /// <summary>
    /// Blends <see cref="TmpTextClip"/> channels into the bound <see cref="TMP_Text"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each float and the gradient blend on their own and are written only when a clip animates them. The typewriter
    /// ratio blends in ratio space (never as raw counts against TMP's 99999) and becomes
    /// <see cref="TMP_Text.maxVisibleCharacters"/> once per frame.
    /// </para>
    /// <para>
    /// <see cref="TMP_Text.maxVisibleCharacters"/> is not serialized, so Timeline's edit-mode preview cannot revert
    /// it: the mixer restores it itself when the graph is destroyed in Edit Mode.
    /// </para>
    /// <para>
    /// The text channel is not blended: the active input with the highest weight decides, otherwise the latest
    /// finished clip's <c>to</c>. The mixer owns two character buffers that grow (doubling) only when a longer pair
    /// appears, fills one with <see cref="TextTweenMath"/> and calls <see cref="TMP_Text.SetCharArray"/> only when
    /// the characters differ from the previous frame. Restoring uses the <c>text</c> setter, because
    /// <c>SetCharArray</c> leaves TMP's input source on "text array" and only the setter re-renders reliably.
    /// </para>
    /// <para>
    /// A From current text tween takes its start from the mixer's own knowledge: the captured base text when the
    /// mixer has not written, otherwise the characters it wrote last (one string built per clip activation, never
    /// per frame). Reading <see cref="TMP_Text.text"/> would allocate in TMP.
    /// </para>
    /// </remarks>
    public sealed class TmpTextMixer : TweenMixer<TMP_Text, TmpTextBehaviour>
    {
        private GraphicColorBlend _blend;
        private FloatBlend _fontSize;
        private FloatBlend _characterSpacing;
        private FloatBlend _wordSpacing;
        private FloatBlend _lineSpacing;
        private FloatBlend _paragraphSpacing;

        private int _baseMaxVisible;
        private float _baseRatio;
        private FloatAccumulator _ratio;
        private bool _wroteRatio;

        private VertexGradient _baseGradient;
        private VertexGradientAccumulator _gradient;
        private bool _wroteGradient;

        private string _baseText;
        private char[] _buffer = Array.Empty<char>();
        private char[] _previous = Array.Empty<char>();
        private int _previousLength;
        private bool _wroteText;
        private TextChannel _textChannel;
        private TmpTextBehaviour _textBehaviour;
        private float _textProgress;
        private float _textWeight;
        private bool _hasActiveText;
        private TextChannel _holdChannel;
        private double _holdEnd;
        private bool _hasHoldText;

        /// <inheritdoc/>
        protected override void CaptureBase(TMP_Text binding)
        {
            _blend.Capture(binding);
            _fontSize.Capture(binding.fontSize);
            _characterSpacing.Capture(binding.characterSpacing);
            _wordSpacing.Capture(binding.wordSpacing);
            _lineSpacing.Capture(binding.lineSpacing);
            _paragraphSpacing.Capture(binding.paragraphSpacing);

            _baseMaxVisible = binding.maxVisibleCharacters;
            _baseRatio = TmpTextBehaviour.ReadVisibleRatio(binding);
            _wroteRatio = false;

            _baseGradient = binding.colorGradient;
            _wroteGradient = false;

            _baseText = binding.text;
            _previousLength = 0;
            _wroteText = false;
        }

        /// <inheritdoc/>
        /// <remarks>Only the properties this mixer wrote are restored.</remarks>
        protected override void RestoreBase(TMP_Text binding)
        {
            _blend.Restore(binding);

            if (_fontSize.Wrote)
            {
                binding.fontSize = _fontSize.Base;
            }

            if (_characterSpacing.Wrote)
            {
                binding.characterSpacing = _characterSpacing.Base;
            }

            if (_wordSpacing.Wrote)
            {
                binding.wordSpacing = _wordSpacing.Base;
            }

            if (_lineSpacing.Wrote)
            {
                binding.lineSpacing = _lineSpacing.Base;
            }

            if (_paragraphSpacing.Wrote)
            {
                binding.paragraphSpacing = _paragraphSpacing.Base;
            }

            if (_wroteGradient)
            {
                binding.colorGradient = _baseGradient;
            }

            RestoreUnserialized(binding);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Restores the typewriter count and, whenever the text was written, the base text with the <c>text</c>
        /// setter (Timeline's preview revert of <c>m_text</c> is not relied on).
        /// </remarks>
        protected override void RestoreUnserialized(TMP_Text binding)
        {
            if (_wroteText)
            {
                binding.text = _baseText;
                _wroteText = false;
                _previousLength = 0;
            }

            if (_wroteRatio)
            {
                binding.maxVisibleCharacters = _baseMaxVisible;
            }
        }

        /// <inheritdoc/>
        protected override void BeginFrame()
        {
            _blend.Reset();
            _fontSize.Reset();
            _characterSpacing.Reset();
            _wordSpacing.Reset();
            _lineSpacing.Reset();
            _paragraphSpacing.Reset();
            _ratio.Reset();
            _gradient.Reset();

            _hasActiveText = false;
            _textWeight = 0f;
            _hasHoldText = false;
        }

        /// <inheritdoc/>
        protected override void OnInputActivated(TmpTextBehaviour behaviour, TMP_Text binding)
        {
            var text = behaviour.Text;

            if (text.Enabled == false || text.Mode != ValueMode.FromCurrent)
            {
                return;
            }

            // One string per clip activation, never per frame.
            behaviour.CaptureText(_wroteText ? new string(_previous, 0, _previousLength) : _baseText);
        }

        /// <inheritdoc/>
        protected override void Accumulate(TmpTextBehaviour behaviour, float weight, float progress)
        {
            _blend.Accumulate(behaviour.Color, behaviour.Alpha, weight, progress);
            _fontSize.Accumulate(behaviour.FontSize, weight, progress);
            _characterSpacing.Accumulate(behaviour.CharacterSpacing, weight, progress);
            _wordSpacing.Accumulate(behaviour.WordSpacing, weight, progress);
            _lineSpacing.Accumulate(behaviour.LineSpacing, weight, progress);
            _paragraphSpacing.Accumulate(behaviour.ParagraphSpacing, weight, progress);

            var visibleRatio = behaviour.VisibleRatio;

            if (visibleRatio.Enabled)
            {
                _ratio.Add(TweenChannelMath.Evaluate(visibleRatio, progress, _baseRatio), weight);
            }

            var gradient = behaviour.ColorGradient;

            if (gradient.Enabled)
            {
                _gradient.Add(VertexGradientMath.Evaluate(gradient, progress, _baseGradient), weight);
            }

            if (behaviour.Text.Enabled && (_hasActiveText == false || weight > _textWeight))
            {
                _textChannel = behaviour.Text;
                _textProgress = progress;
                _textWeight = weight;
                _hasActiveText = true;
            }
        }

        /// <inheritdoc/>
        protected override void AccumulateHold(TmpTextBehaviour behaviour, double endTime)
        {
            _blend.AccumulateHold(behaviour.Color, behaviour.Alpha, endTime);
            _fontSize.AccumulateHold(behaviour.FontSize, endTime);
            _characterSpacing.AccumulateHold(behaviour.CharacterSpacing, endTime);
            _wordSpacing.AccumulateHold(behaviour.WordSpacing, endTime);
            _lineSpacing.AccumulateHold(behaviour.LineSpacing, endTime);
            _paragraphSpacing.AccumulateHold(behaviour.ParagraphSpacing, endTime);

            var visibleRatio = behaviour.VisibleRatio;

            if (visibleRatio.Enabled)
            {
                _ratio.OfferHold(TweenChannelMath.Evaluate(visibleRatio, 1f, _baseRatio), endTime);
            }

            var gradient = behaviour.ColorGradient;

            if (gradient.Enabled)
            {
                _gradient.OfferHold(VertexGradientMath.Evaluate(gradient, 1f, _baseGradient), endTime);
            }

            if (behaviour.Text.Enabled && (_hasHoldText == false || endTime >= _holdEnd))
            {
                _holdChannel = behaviour.Text;
                _holdEnd = endTime;
                _hasHoldText = true;
            }
        }

        /// <inheritdoc/>
        protected override void Apply(TMP_Text binding)
        {
            _blend.Apply(binding);

            if (_fontSize.TryResolve(out var fontSize))
            {
                binding.fontSize = fontSize;
            }

            if (_characterSpacing.TryResolve(out var characterSpacing))
            {
                binding.characterSpacing = characterSpacing;
            }

            if (_wordSpacing.TryResolve(out var wordSpacing))
            {
                binding.wordSpacing = wordSpacing;
            }

            if (_lineSpacing.TryResolve(out var lineSpacing))
            {
                binding.lineSpacing = lineSpacing;
            }

            if (_paragraphSpacing.TryResolve(out var paragraphSpacing))
            {
                binding.paragraphSpacing = paragraphSpacing;
            }

            if (_ratio.HasContribution)
            {
                var ratio = _ratio.Resolve(_baseRatio);

                binding.maxVisibleCharacters = ratio >= 1f
                    ? TmpTextBehaviour.ALL_CHARACTERS
                    : Mathf.Max(0, Mathf.RoundToInt(ratio * binding.textInfo.characterCount));

                _wroteRatio = true;
            }

            if (_gradient.HasContribution)
            {
                binding.colorGradient = _gradient.Resolve(_baseGradient);
                _wroteGradient = true;
            }

            ApplyText(binding);
        }

        private void ApplyText(TMP_Text binding)
        {
            int length;

            if (_hasActiveText)
            {
                var channel = _textChannel;
                var from = channel.Mode == ValueMode.FromCurrent
                    ? (channel.HasCaptured ? channel.Captured : _baseText)
                    : channel.From;

                EnsureCapacity(TextTweenMath.RequiredLength(from, channel.To));

                length = TextTweenMath.Fill(
                      _buffer
                    , from
                    , channel.To
                    , channel.Ease.Evaluate(_textProgress)
                    , channel.Scramble
                    , channel.CustomScrambleChars
                );
            }
            else if (_hasHoldText)
            {
                var to = _holdChannel.To ?? string.Empty;

                EnsureCapacity(to.Length);
                length = TextTweenMath.Fill(_buffer, string.Empty, to, 1f, ScrambleMode.None, string.Empty);
            }
            else
            {
                return;
            }

            if (_wroteText && IsSameAsPrevious(length))
            {
                return;
            }

            if (_previous.Length < length)
            {
                _previous = new char[_buffer.Length];
            }

            Array.Copy(_buffer, _previous, length);
            _previousLength = length;
            _wroteText = true;
            binding.SetCharArray(_buffer, 0, length);
        }

        private bool IsSameAsPrevious(int length)
        {
            if (length != _previousLength)
            {
                return false;
            }

            for (var i = 0; i < length; i++)
            {
                if (_buffer[i] != _previous[i])
                {
                    return false;
                }
            }

            return true;
        }

        // Grows by doubling, and only when a longer pair appears.
        private void EnsureCapacity(int required)
        {
            if (_buffer.Length >= required)
            {
                return;
            }

            _buffer = new char[Math.Max(required, _buffer.Length * 2)];
        }

        // One float property of the text: its base value, this frame's blend and whether it was ever written. The
        // mixer owns each instance as a field and must not copy it.
        private struct FloatBlend
        {
            private FloatAccumulator _accumulator;

            public float Base { get; private set; }

            public bool Wrote { get; private set; }

            public void Capture(float value)
            {
                Base = value;
                Wrote = false;
            }

            public void Reset()
            {
                _accumulator.Reset();
            }

            public void Accumulate(in TweenChannel<float> channel, float weight, float progress)
            {
                if (channel.Enabled)
                {
                    _accumulator.Add(TweenChannelMath.Evaluate(channel, progress, Base), weight);
                }
            }

            public void AccumulateHold(in TweenChannel<float> channel, double endTime)
            {
                if (channel.Enabled)
                {
                    _accumulator.OfferHold(TweenChannelMath.Evaluate(channel, 1f, Base), endTime);
                }
            }

            public bool TryResolve(out float value)
            {
                if (_accumulator.HasContribution == false)
                {
                    value = default;
                    return false;
                }

                value = _accumulator.Resolve(Base);
                Wrote = true;
                return true;
            }
        }
    }
}
