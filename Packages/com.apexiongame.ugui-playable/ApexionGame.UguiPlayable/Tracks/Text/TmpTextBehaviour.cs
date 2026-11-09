using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Tracks.Graphics;

namespace ApexionGame.UguiPlayable.Tracks.Text
{
    /// <summary>
    /// The channels of a <see cref="TmpTextClip"/>: color, alpha, font size, four spacings, the typewriter ratio, the
    /// vertex gradient and the string tween.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Color and alpha both write <see cref="Graphic.color"/>; when both are enabled the alpha channel wins for A.
    /// Only the typewriter channel is on by default.
    /// </para>
    /// <para>
    /// The visible ratio is a 0 to 1 share of the text's character count. It is blended as a ratio and converted once
    /// to <see cref="TMP_Text.maxVisibleCharacters"/> as <c>floor(ratio * count)</c>: a ratio of 1 or more writes
    /// 99999 (TMP's "all"), so text set later is not cut. The count is the length of the string this track writes in
    /// the same frame; when the track writes no text, it comes from <see cref="TMP_Text.textInfo"/>, which can lag one
    /// frame behind a text change. The mixer never forces a mesh update.
    /// </para>
    /// <para>
    /// The gradient channel writes <see cref="TMP_Text.colorGradient"/> only. TMP shows it when
    /// <see cref="TMP_Text.enableVertexGradient"/> is on, which the track leaves as it is.
    /// </para>
    /// <para>
    /// The text channel is off by default. It is not blended and writes <see cref="TMP_Text.text"/> through the
    /// mixer's character buffer; rich-text tags are revealed as plain characters.
    /// </para>
    /// </remarks>
    [Serializable]
    public sealed class TmpTextBehaviour : TweenBehaviour
    {
        internal const string COLOR_PROPERTY = "m_fontColor";
        internal const string COLOR32_PROPERTY = "m_fontColor32";
        internal const string FONT_SIZE_PROPERTY = "m_fontSize";
        internal const string FONT_SIZE_BASE_PROPERTY = "m_fontSizeBase";
        internal const string CHARACTER_SPACING_PROPERTY = "m_characterSpacing";
        internal const string WORD_SPACING_PROPERTY = "m_wordSpacing";
        internal const string LINE_SPACING_PROPERTY = "m_lineSpacing";
        internal const string PARAGRAPH_SPACING_PROPERTY = "m_paragraphSpacing";
        internal const string COLOR_GRADIENT_PROPERTY = "m_fontColorGradient";
        internal const string TEXT_PROPERTY = "m_text";

        /// <summary>The value of <see cref="TMP_Text.maxVisibleCharacters"/> that shows every character.</summary>
        internal const int ALL_CHARACTERS = 99999;

        [SerializeField]
        private TweenChannel<Color> _color = new(UnityEngine.Color.white, UnityEngine.Color.white, enabled: false);

        [SerializeField]
        private TweenChannel<float> _alpha = new(0f, 1f, enabled: false);

        [SerializeField]
        private TweenChannel<float> _fontSize = new(36f, 48f, enabled: false);

        [SerializeField]
        private TweenChannel<float> _characterSpacing = new(0f, 10f, enabled: false);

        [SerializeField]
        private TweenChannel<float> _wordSpacing = new(0f, 10f, enabled: false);

        [SerializeField]
        private TweenChannel<float> _lineSpacing = new(0f, 10f, enabled: false);

        [SerializeField]
        private TweenChannel<float> _paragraphSpacing = new(0f, 10f, enabled: false);

        [SerializeField]
        private TweenChannel<float> _visibleRatio = new(0f, 1f, enabled: true);

        [SerializeField]
        private TweenChannel<VertexGradient> _colorGradient = new(
              new VertexGradient(UnityEngine.Color.white)
            , new VertexGradient(UnityEngine.Color.white)
            , enabled: false
        );

        [SerializeField]
        private TextChannel _text = new(string.Empty, string.Empty, enabled: false);

        /// <summary>The <see cref="Graphic.color"/> channel.</summary>
        public TweenChannel<Color> Color
        {
            get => _color;
            set => _color = value;
        }

        /// <summary>The channel for the A component of <see cref="Graphic.color"/>.</summary>
        public TweenChannel<float> Alpha
        {
            get => _alpha;
            set => _alpha = value;
        }

        /// <summary>The <see cref="TMP_Text.fontSize"/> channel.</summary>
        public TweenChannel<float> FontSize
        {
            get => _fontSize;
            set => _fontSize = value;
        }

        /// <summary>The <see cref="TMP_Text.characterSpacing"/> channel.</summary>
        public TweenChannel<float> CharacterSpacing
        {
            get => _characterSpacing;
            set => _characterSpacing = value;
        }

        /// <summary>The <see cref="TMP_Text.wordSpacing"/> channel.</summary>
        public TweenChannel<float> WordSpacing
        {
            get => _wordSpacing;
            set => _wordSpacing = value;
        }

        /// <summary>The <see cref="TMP_Text.lineSpacing"/> channel.</summary>
        public TweenChannel<float> LineSpacing
        {
            get => _lineSpacing;
            set => _lineSpacing = value;
        }

        /// <summary>The <see cref="TMP_Text.paragraphSpacing"/> channel.</summary>
        public TweenChannel<float> ParagraphSpacing
        {
            get => _paragraphSpacing;
            set => _paragraphSpacing = value;
        }

        /// <summary>
        /// The typewriter channel: the share (0 to 1) of the text's characters that is visible, written to
        /// <see cref="TMP_Text.maxVisibleCharacters"/>.
        /// </summary>
        public TweenChannel<float> VisibleRatio
        {
            get => _visibleRatio;
            set => _visibleRatio = value;
        }

        /// <summary>The <see cref="TMP_Text.colorGradient"/> channel.</summary>
        public TweenChannel<VertexGradient> ColorGradient
        {
            get => _colorGradient;
            set => _colorGradient = value;
        }

        /// <summary>The <see cref="TMP_Text.text"/> channel: the string tween.</summary>
        public TextChannel Text
        {
            get => _text;
            set => _text = value;
        }

        /// <inheritdoc/>
        internal override bool HasEnabledChannel()
            => _color.Enabled
                || _alpha.Enabled
                || _fontSize.Enabled
                || _characterSpacing.Enabled
                || _wordSpacing.Enabled
                || _lineSpacing.Enabled
                || _paragraphSpacing.Enabled
                || _visibleRatio.Enabled
                || _colorGradient.Enabled
                || _text.Enabled;

        /// <inheritdoc/>
        /// <remarks>The visible ratio is not serialized by TMP, so it registers no property.</remarks>
        public override void CollectPropertyNames(ICollection<string> names)
        {
            if (_color.Enabled || _alpha.Enabled)
            {
                names.Add(COLOR_PROPERTY);
                names.Add(COLOR32_PROPERTY);
            }

            if (_fontSize.Enabled)
            {
                names.Add(FONT_SIZE_PROPERTY);
                names.Add(FONT_SIZE_BASE_PROPERTY);
            }

            if (_characterSpacing.Enabled)
            {
                names.Add(CHARACTER_SPACING_PROPERTY);
            }

            if (_wordSpacing.Enabled)
            {
                names.Add(WORD_SPACING_PROPERTY);
            }

            if (_lineSpacing.Enabled)
            {
                names.Add(LINE_SPACING_PROPERTY);
            }

            if (_paragraphSpacing.Enabled)
            {
                names.Add(PARAGRAPH_SPACING_PROPERTY);
            }

            if (_colorGradient.Enabled)
            {
                names.Add(COLOR_GRADIENT_PROPERTY);
            }

            if (_text.Enabled)
            {
                names.Add(TEXT_PROPERTY);
            }
        }

        /// <inheritdoc/>
        public override void OnActivated(Component binding)
        {
            GraphicColorBlend.CaptureCurrent(binding, ref _color, ref _alpha);

            if (binding is not TMP_Text text)
            {
                return;
            }

            CaptureCurrent(ref _fontSize, text.fontSize);
            CaptureCurrent(ref _characterSpacing, text.characterSpacing);
            CaptureCurrent(ref _wordSpacing, text.wordSpacing);
            CaptureCurrent(ref _lineSpacing, text.lineSpacing);
            CaptureCurrent(ref _paragraphSpacing, text.paragraphSpacing);

            if (_visibleRatio.Enabled && _visibleRatio.Mode == ValueMode.FromCurrent)
            {
                _visibleRatio.Capture(ReadVisibleRatio(text));
            }

            if (_colorGradient.Enabled && _colorGradient.Mode == ValueMode.FromCurrent)
            {
                _colorGradient.Capture(text.colorGradient);
            }
        }

        /// <inheritdoc/>
        public override void OnDeactivated()
        {
            _color.ClearCapture();
            _alpha.ClearCapture();
            _fontSize.ClearCapture();
            _characterSpacing.ClearCapture();
            _wordSpacing.ClearCapture();
            _lineSpacing.ClearCapture();
            _paragraphSpacing.ClearCapture();
            _visibleRatio.ClearCapture();
            _colorGradient.ClearCapture();
            _text.ClearCapture();
        }

        /// <summary>
        /// Stores the text a <see cref="ValueMode.FromCurrent"/> text tween starts from. The mixer calls it, because
        /// reading <see cref="TMP_Text.text"/> allocates when TMP's backing array is dirty.
        /// </summary>
        internal void CaptureText(string current)
        {
            if (_text.Enabled && _text.Mode == ValueMode.FromCurrent)
            {
                _text.Capture(current);
            }
        }

        /// <summary>
        /// The share of the text that is visible: <c>maxVisibleCharacters / characterCount</c>, 1 when every
        /// character shows and 0 when the text has no built characters.
        /// </summary>
        internal static float ReadVisibleRatio(TMP_Text text)
            => ReadVisibleRatio(text, text.textInfo.characterCount);

        /// <summary>
        /// The share of <paramref name="count"/> characters that is visible: 1 when every character shows, 0 when
        /// the text has no characters, otherwise <c>maxVisibleCharacters / count</c> clamped to 0 and 1.
        /// </summary>
        internal static float ReadVisibleRatio(TMP_Text text, int count)
        {
            var visible = text.maxVisibleCharacters;

            if (visible >= ALL_CHARACTERS)
            {
                return 1f;
            }

            return count <= 0 ? 0f : Mathf.Clamp01(visible / (float)count);
        }

        /// <summary>
        /// Stores the ratio a <see cref="ValueMode.FromCurrent"/> typewriter starts from, counted against
        /// <paramref name="count"/> characters (the text the mixer wrote).
        /// </summary>
        internal void CaptureVisibleRatio(TMP_Text text, int count)
        {
            if (_visibleRatio.Enabled && _visibleRatio.Mode == ValueMode.FromCurrent)
            {
                _visibleRatio.Capture(ReadVisibleRatio(text, count));
            }
        }

        private static void CaptureCurrent(ref TweenChannel<float> channel, float current)
        {
            if (channel.Enabled && channel.Mode == ValueMode.FromCurrent)
            {
                channel.Capture(current);
            }
        }
    }
}
