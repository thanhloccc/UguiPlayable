using System;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
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
    /// <see cref="TMP_Text.maxVisibleCharacters"/> once per frame. A clip that reveals by word or line, or pauses after
    /// punctuation, is mapped to its character ratio first, once the frame's character count is known (after the
    /// text is written); its word and line boundaries are those of <see cref="TMP_Text.textInfo"/>, one frame late
    /// after a text change.
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
    /// The counter channel shares this text path. Counter values blend by weight in double; the format comes from the
    /// input with the highest weight (or the latest finished one). The string channel wins over the counter while
    /// any active input has it on; with no active text or counter input, the hold that ends later wins, the string
    /// channel on a tie. The counter never parses the text: From current starts from the last value the mixer wrote,
    /// else <c>From</c>.
    /// </para>
    /// <para>
    /// A From current text tween starts from the <c>to</c> of the previous text clip on the track (the one that
    /// started earlier and ends last), or from the base text when there is none, so jumping to a time gives the same
    /// text as playing to it. The base text is read once per bind (<see cref="TMP_Text.text"/> allocates once after
    /// <c>SetCharArray</c> in the player) and never per frame.
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
        private UnitRatio[] _unitRatios = Array.Empty<UnitRatio>();
        private int _unitRatioCount;
        private UnitRatio _unitHold;
        private double _unitHoldEnd;
        private bool _hasUnitHold;

        private VertexGradient _baseGradient;
        private VertexGradientAccumulator _gradient;
        private bool _wroteGradient;

        private string _baseText;
        private char[] _buffer = Array.Empty<char>();
        private char[] _previous = Array.Empty<char>();
        private int _previousLength;
        private int _previousSlotCount;
        private bool _wroteText;
        private TextChannel _textChannel;
        private TmpTextBehaviour _textBehaviour;
        private float _textProgress;
        private double _textSeconds;
        private float _textWeight;
        private bool _hasActiveText;
        private TextChannel _holdChannel;
        private double _holdEnd;
        private bool _hasHoldText;
        private int _producedCount;
        private bool _hasProducedText;

        private DoubleAccumulator _counter;
        private CounterChannel _counterFormat;
        private double _counterStart;
        private float _counterWeight;
        private bool _hasActiveCounter;
        private CounterChannel _holdCounter;
        private double _holdCounterEnd;
        private bool _hasHoldCounter;
        private double _lastCounterValue;
        private bool _hasLastCounter;

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
            _previousSlotCount = 0;
            _wroteText = false;
            _hasLastCounter = false;
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
                _previousSlotCount = 0;
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
            _unitRatioCount = 0;
            _hasUnitHold = false;
            _gradient.Reset();

            _hasActiveText = false;
            _textWeight = 0f;
            _hasHoldText = false;
            _hasProducedText = false;

            _counter.Reset();
            _counterWeight = 0f;
            _hasActiveCounter = false;
            _hasHoldCounter = false;
        }

        /// <inheritdoc/>
        protected override void OnInputActivated(TmpTextBehaviour behaviour, TMP_Text binding)
        {
            if (_wroteText)
            {
                behaviour.CaptureVisibleRatio(binding, _previousSlotCount);
            }

            behaviour.CaptureCounter(_hasLastCounter, _lastCounterValue);

            var text = behaviour.Text;

            if (text.Enabled == false || text.Mode != ValueMode.FromCurrent)
            {
                return;
            }

            // The previous clip's To (or the base text) is passed as is: no string is built.
            behaviour.CaptureText(FindPreviousText());
        }

        // Among the other text clips that started earlier than the one being activated, the one that ends last (ties:
        // the later input) gives its To; none gives the base text. It does not depend on what was scrubbed before.
        private string FindPreviousText()
        {
            var playable = CurrentPlayable;
            var index = CurrentInputIndex;
            var start = GetWindow(index).Start;
            var count = Math.Min(playable.GetInputCount(), WindowCount);
            var found = false;
            var bestEnd = 0d;
            string result = null;

            for (var j = 0; j < count; j++)
            {
                if (j == index)
                {
                    continue;
                }

                var window = GetWindow(j);

                if (window.Start >= start || (found && window.End < bestEnd))
                {
                    continue;
                }

                var input = (ScriptPlayable<TmpTextBehaviour>)playable.GetInput(j);

                if (input.IsValid() == false)
                {
                    continue;
                }

                var other = input.GetBehaviour();

                if (other == null || other.Text.Enabled == false)
                {
                    continue;
                }

                found = true;
                bestEnd = window.End;
                result = other.Text.To ?? string.Empty;
            }

            return found ? result : _baseText;
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
                var ratio = TweenChannelMath.Evaluate(visibleRatio, progress, _baseRatio);

                if (behaviour.IsCharacterTypewriter())
                {
                    _ratio.Add(ratio, weight);
                }
                else
                {
                    AddUnitRatio(new UnitRatio(behaviour, ratio, weight));
                }
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
                _textSeconds = CurrentLocalSeconds;
                _textWeight = weight;
                _hasActiveText = true;
            }

            var counter = behaviour.Counter;

            if (counter.Enabled)
            {
                var start = CounterChannelMath.StartOf(counter);

                _counter.Add(CounterChannelMath.Evaluate(counter, progress, start), weight);

                if (_hasActiveCounter == false || weight > _counterWeight)
                {
                    _counterFormat = counter;
                    _counterStart = start;
                    _counterWeight = weight;
                    _hasActiveCounter = true;
                }
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
                var ratio = TweenChannelMath.Evaluate(visibleRatio, 1f, _baseRatio);

                if (behaviour.IsCharacterTypewriter())
                {
                    _ratio.OfferHold(ratio, endTime);
                }

                // The latest end wins, as in the accumulator; a character hold that ends later replaces it there.
                if (_hasUnitHold == false || endTime >= _unitHoldEnd)
                {
                    _unitHold = new UnitRatio(behaviour.IsCharacterTypewriter() ? null : behaviour, ratio, 0f);
                    _unitHoldEnd = endTime;
                    _hasUnitHold = true;
                }
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

            var counter = behaviour.Counter;

            if (counter.Enabled)
            {
                var value = CounterChannelMath.Evaluate(counter, 1f, CounterChannelMath.StartOf(counter));

                _counter.OfferHold(value, endTime);

                if (_hasHoldCounter == false || endTime >= _holdCounterEnd)
                {
                    _holdCounter = counter;
                    _holdCounterEnd = endTime;
                    _hasHoldCounter = true;
                }
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

            ApplyText(binding);
            AddUnitRatios(binding);

            if (_ratio.HasContribution)
            {
                var ratio = _ratio.Resolve(_baseRatio);
                var count = GetCharacterCount(binding);

                binding.maxVisibleCharacters = ratio >= 1f
                    ? TmpTextBehaviour.ALL_CHARACTERS
                    : TextTweenMath.CountOf(ratio, count);

                _wroteRatio = true;
            }

            if (_gradient.HasContribution)
            {
                binding.colorGradient = _gradient.Resolve(_baseGradient);
                _wroteGradient = true;
            }
        }

        // The text written this frame when there is one, otherwise TMP's last layout.
        private int GetCharacterCount(TMP_Text binding)
            => _hasProducedText ? _producedCount : binding.textInfo.characterCount;

        // Grows by doubling; the inputs of one frame fit after the first frames.
        private void AddUnitRatio(in UnitRatio entry)
        {
            if (_unitRatioCount == _unitRatios.Length)
            {
                var grown = new UnitRatio[Math.Max(4, _unitRatios.Length * 2)];

                Array.Copy(_unitRatios, grown, _unitRatioCount);
                _unitRatios = grown;
            }

            _unitRatios[_unitRatioCount++] = entry;
        }

        // The word, line and pause clips join the blend as character ratios, now that the count is known.
        private void AddUnitRatios(TMP_Text binding)
        {
            if (_unitRatioCount == 0 && (_hasUnitHold == false || _unitHold.Behaviour == null))
            {
                return;
            }

            var count = GetCharacterCount(binding);
            var info = binding.textInfo;

            for (var i = 0; i < _unitRatioCount; i++)
            {
                ref readonly var entry = ref _unitRatios[i];

                _ratio.Add(entry.Behaviour.ToCharacterRatio(info, count, entry.Ratio), entry.Weight);
            }

            if (_hasUnitHold && _unitHold.Behaviour != null)
            {
                _ratio.OfferHold(_unitHold.Behaviour.ToCharacterRatio(info, count, _unitHold.Ratio), _unitHoldEnd);
            }

            for (var i = 0; i < _unitRatioCount; i++)
            {
                _unitRatios[i] = default;
            }
        }

        private void ApplyText(TMP_Text binding)
        {
            var richText = binding.richText;
            var parseEscapes = binding.parseCtrlCharacters;
            int length;
            int slotCount;

            if (_hasActiveText)
            {
                var channel = _textChannel;
                var from = channel.Mode == ValueMode.FromCurrent
                    ? (channel.HasCaptured ? channel.Captured : _baseText)
                    : channel.From;

                EnsureCapacity(TextTweenMath.RequiredLength(from, channel.To, richText));

                // A rate above 0 makes the scramble step a function of the clip-local time; 0 keeps the reveal count.
                var rate = channel.ScrambleRate;
                var step = rate > 0f
                    ? (int)Math.Min(Math.Floor(_textSeconds * rate), int.MaxValue)
                    : TextTweenMath.SCRAMBLE_STEP_FROM_REVEAL;

                length = TextTweenMath.Fill(
                      _buffer
                    , from
                    , channel.To
                    , channel.Ease.Evaluate(_textProgress)
                    , channel.Scramble
                    , channel.CustomScrambleChars
                    , richText
                    , parseEscapes
                    , step
                    , out slotCount
                );
            }
            else if (_hasActiveCounter || (_hasHoldCounter && (_hasHoldText == false || _holdCounterEnd > _holdEnd)))
            {
                // The counter decides: an active counter clip, or a counter hold that ends after the string hold.
                var format = _hasActiveCounter ? _counterFormat : _holdCounter;
                var value = _counter.Resolve(_hasActiveCounter ? _counterStart : format.To);

                EnsureCapacity(CounterFormatMath.RequiredLength(format));

                length = CounterFormatMath.Fill(_buffer, value, format);
                slotCount = length;
                _lastCounterValue = value;
                _hasLastCounter = true;
            }
            else if (_hasHoldText)
            {
                var to = _holdChannel.To ?? string.Empty;

                EnsureCapacity(to.Length);

                length = TextTweenMath.Fill(
                      buffer: _buffer
                    , from: string.Empty
                    , to: to
                    , progress: 1f
                    , mode: ScrambleMode.None
                    , custom: string.Empty
                    , richText: richText
                    , parseEscapes: parseEscapes
                    , slotCount: out slotCount
                );
            }
            else
            {
                return;
            }

            _producedCount = slotCount;
            _hasProducedText = true;

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
            _previousSlotCount = slotCount;
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

        // The eased ratio of a clip that does not reveal by character with no pause, kept until the count is known.
        // A null behaviour marks a character hold, already offered to the accumulator.
        private readonly struct UnitRatio
        {
            public readonly TmpTextBehaviour Behaviour;
            public readonly float Ratio;
            public readonly float Weight;

            public UnitRatio(TmpTextBehaviour behaviour, float ratio, float weight)
            {
                Behaviour = behaviour;
                Ratio = ratio;
                Weight = weight;
            }
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
