using System;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Text
{
    /// <summary>
    /// Blends <see cref="TmpCharacterClip"/> effects into the mesh of the bound <see cref="TMP_Text"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The mesh is written in two places. <see cref="Apply"/> writes <c>source + effect</c> into
    /// <see cref="TMP_TextInfo.meshInfo"/> and uploads the vertices and colours with
    /// <see cref="TMP_Text.UpdateVertexData(TMP_VertexDataUpdateFlags)"/>, without a rebuild. A cached
    /// <see cref="TMP_Text.OnPreRenderText"/> handler runs whenever TMP rebuilds the text (another track changed its
    /// colour, string or typewriter count): it captures the fresh vertices as the new source and writes the same
    /// effect before TMP uploads them, so nothing flickers. The handler never accumulates, so several calls in one
    /// rebuild (auto size) give the same mesh.
    /// </para>
    /// <para>
    /// The source is captured only by the handler, from a fresh rebuild: the bind forces one with
    /// <see cref="TMP_Text.ForceMeshUpdate"/>. Until a source exists nothing is written.
    /// </para>
    /// <para>
    /// When the graph is destroyed the mixer always unsubscribes and marks the text for a rebuild
    /// (<see cref="TMP_Text.havePropertiesChanged"/>), whatever the restore mode: TMP regenerates the rest mesh at the
    /// next canvas update. The mesh is never serialized, so the preview leaves the scene clean.
    /// </para>
    /// <para>
    /// Transitions blend per character like any channel (<c>hold·(1 − W) + Σ wᵢ·vᵢ</c>, the hold being the latest
    /// finished clip's end value for the characters of its range); the wave and the shake are added with their
    /// weight and have no hold. The mixer owns grow-only buffers and allocates nothing per frame.
    /// </para>
    /// </remarks>
    public sealed class TmpCharacterMixer : TweenMixer<TMP_Text, TmpCharacterBehaviour>
    {
        private const TMP_VertexDataUpdateFlags UPLOAD_FLAGS
            = TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32;

        private const int MIN_CAPACITY = 4;

        private readonly Action<TMP_TextInfo> _handler;

        private TMP_Text _subscribed;

        private Vector3[][] _sourceVertices = Array.Empty<Vector3[]>();
        private Color32[][] _sourceColors = Array.Empty<Color32[]>();
        private int _sourceMaterialCount;
        private int _sourceCharacterCount;
        private int _layoutVersion;
        private int _layoutCount;
        private char[] _layoutCharacters = Array.Empty<char>();
        private TMP_TextElementType[] _layoutTypes = Array.Empty<TMP_TextElementType>();
        private bool _hasSource;
        private bool _rebuilt;
        private bool _wroteEffect;

        private Contribution[] _contributions = Array.Empty<Contribution>();
        private Contribution[] _previousContributions = Array.Empty<Contribution>();
        private RankCache[] _ranks = Array.Empty<RankCache>();
        private int _contributionCount;
        private int _previousContributionCount;

        private Hold[] _holds = Array.Empty<Hold>();
        private Hold[] _previousHolds = Array.Empty<Hold>();
        private int _holdCount;
        private int _previousHoldCount;

        private int[] _visible = Array.Empty<int>();
        private int[] _visibleRanks = Array.Empty<int>();
        private uint[] _sortKeys = Array.Empty<uint>();
        private int[] _sortItems = Array.Empty<int>();

        /// <summary>Creates the mixer and its one cached <see cref="TMP_Text.OnPreRenderText"/> handler.</summary>
        public TmpCharacterMixer()
        {
            _handler = HandlePreRenderText;
        }

        /// <summary>Tests: whether the handler is subscribed to a text.</summary>
        internal bool IsHandlerSubscribed => ReferenceEquals(_subscribed, null) == false;

        /// <summary>Tests: whether the handler has captured a source mesh.</summary>
        internal bool HasSource => _hasSource;

        /// <summary>Tests: the number of mesh uploads <see cref="Apply"/> made.</summary>
        internal int UploadCount { get; private set; }

        /// <summary>Tests: the number of rank tables <see cref="PrepareRanks"/> rebuilt.</summary>
        internal int RankBuildCount { get; private set; }

        /// <inheritdoc/>
        public override void PrepareFrame(Playable playable, FrameData info)
        {
            EnsureContributionCapacity(playable.GetInputCount());
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Unsubscribes the handler and marks the text for a rebuild before the base restore, whatever the
        /// <see cref="TweenMixer{TBinding, TBehaviour}.RestoreMode"/>: the effect cannot outlive the graph.
        /// </remarks>
        public override void OnPlayableDestroy(Playable playable)
        {
            Unsubscribe();
            ResetState();
            base.OnPlayableDestroy(playable);
        }

        /// <inheritdoc/>
        /// <remarks>
        /// Subscribes the handler (once per binding) and forces a rebuild of an active text, which lets the handler
        /// capture the source. An inactive text rebuilds when it is enabled, and the handler captures then.
        /// </remarks>
        protected override void CaptureBase(TMP_Text binding)
        {
            if (ReferenceEquals(_subscribed, binding) == false)
            {
                Unsubscribe();
                binding.OnPreRenderText += _handler;
                _subscribed = binding;
            }

            ResetState();

            if (binding.isActiveAndEnabled)
            {
                binding.ForceMeshUpdate();
            }
        }

        /// <inheritdoc/>
        /// <remarks>Nothing to do: <see cref="OnPlayableDestroy"/> always restores the mesh.</remarks>
        protected override void RestoreBase(TMP_Text binding)
        {
        }

        /// <inheritdoc/>
        protected override void BeginFrame()
        {
            _contributionCount = 0;
            _holdCount = 0;
        }

        /// <inheritdoc/>
        protected override void Accumulate(TmpCharacterBehaviour behaviour, float weight, float progress)
        {
            EnsureContributionCapacity(_contributionCount + 1);
            _contributions[_contributionCount++] = new Contribution(behaviour, weight, progress);
        }

        /// <inheritdoc/>
        protected override void AccumulateHold(TmpCharacterBehaviour behaviour, double endTime)
        {
            if (behaviour.HasTransition == false)
            {
                return;
            }

            EnsureHoldCapacity(_holdCount + 1);
            _holds[_holdCount++] = new Hold(behaviour, endTime);
        }

        /// <inheritdoc/>
        protected override void Apply(TMP_Text binding)
        {
            var hasEffect = _contributionCount > 0 || _holdCount > 0;
            var skip = _hasSource == false
                || binding.geometrySortingOrder != VertexSortingOrder.Normal
                || (hasEffect == false && _wroteEffect == false)
                || (hasEffect && _rebuilt == false && IsSameAsPrevious());

            if (skip)
            {
                SavePrevious();
                return;
            }

            // A pending rebuild wipes whatever is pushed now; the handler writes the effect during it instead.
            if (binding.havePropertiesChanged || binding.isActiveAndEnabled == false)
            {
                SavePrevious();
                return;
            }

            var textInfo = binding.textInfo;

            if (CanWrite(textInfo) == false)
            {
                SavePrevious();
                return;
            }

            WriteMesh(textInfo, hasEffect);
            binding.UpdateVertexData(UPLOAD_FLAGS);

            UploadCount++;
            _wroteEffect = hasEffect;
            _rebuilt = false;
            SavePrevious();
        }

        // TMP raises this inside its rebuild, after regenerating the vertices and before uploading them.
        private void HandlePreRenderText(TMP_TextInfo textInfo)
        {
            var text = _subscribed;

            if (text.IsInvalid() || text.geometrySortingOrder != VertexSortingOrder.Normal)
            {
                _hasSource = false;
                return;
            }

            Capture(textInfo);

            var hasEffect = _contributionCount > 0 || _holdCount > 0;

            if (hasEffect && CanWrite(textInfo))
            {
                WriteMesh(textInfo, true);
                _wroteEffect = true;
            }
            else
            {
                _wroteEffect = false;
            }
        }

        private void Unsubscribe()
        {
            var text = _subscribed;

            if (ReferenceEquals(text, null))
            {
                return;
            }

            text.OnPreRenderText -= _handler;
            _subscribed = null;

            // A deferred full rebuild: Timeline reverts the serialized properties after the destroy, so an immediate
            // ForceMeshUpdate here would bake the previewed values into the mesh.
            if (text.IsValid())
            {
                text.havePropertiesChanged = true;
            }
        }

        private void ResetState()
        {
            _hasSource = false;
            _rebuilt = false;
            _wroteEffect = false;
            _contributionCount = 0;
            _previousContributionCount = 0;
            _holdCount = 0;
            _previousHoldCount = 0;
        }

        private void Capture(TMP_TextInfo textInfo)
        {
            var materialCount = textInfo.materialCount;
            var meshInfo = textInfo.meshInfo;

            if (_sourceVertices.Length < materialCount)
            {
                Array.Resize(ref _sourceVertices, materialCount);
                Array.Resize(ref _sourceColors, materialCount);
            }

            // ResizeMeshInfo may have reallocated the arrays, so they are re-read on every rebuild.
            for (var m = 0; m < materialCount; m++)
            {
                CopyInto(meshInfo[m].vertices, ref _sourceVertices[m]);
                CopyInto(meshInfo[m].colors32, ref _sourceColors[m]);
            }

            _sourceMaterialCount = materialCount;
            _sourceCharacterCount = textInfo.characterCount;
            TrackLayout(textInfo.characterInfo, textInfo.characterCount);
            _hasSource = true;
            _rebuilt = true;
        }

        // The ranks depend only on the character count and on which characters are drawable, so the layout version
        // changes only when one of them differs from the previous rebuild (a typewriter or a colour change keeps it).
        private void TrackLayout(TMP_CharacterInfo[] characters, int characterCount)
        {
            var changed = characterCount != _layoutCount;

            if (_layoutCharacters.Length < characterCount)
            {
                var capacity = Math.Max(Math.Max(characterCount, MIN_CAPACITY), _layoutCharacters.Length * 2);

                _layoutCharacters = new char[capacity];
                _layoutTypes = new TMP_TextElementType[capacity];
                changed = true;
            }

            for (var i = 0; i < characterCount; i++)
            {
                ref var character = ref characters[i];

                if (changed == false
                    && (_layoutCharacters[i] != character.character || _layoutTypes[i] != character.elementType)
                )
                {
                    changed = true;
                }

                _layoutCharacters[i] = character.character;
                _layoutTypes[i] = character.elementType;
            }

            _layoutCount = characterCount;

            if (changed)
            {
                _layoutVersion++;
            }
        }

        private static void CopyInto<T>(T[] source, ref T[] target)
        {
            if (source == null)
            {
                return;
            }

            if (target == null || target.Length < source.Length)
            {
                target = new T[source.Length];
            }

            Array.Copy(source, target, source.Length);
        }

        // The text must still have the layout the source was captured from, and every mesh must exist.
        private bool CanWrite(TMP_TextInfo textInfo)
        {
            var characterCount = textInfo.characterCount;
            var materialCount = textInfo.materialCount;

            if (characterCount == 0
                || characterCount != _sourceCharacterCount
                || materialCount > _sourceMaterialCount
            )
            {
                return false;
            }

            var meshInfo = textInfo.meshInfo;

            for (var m = 0; m < materialCount; m++)
            {
                if (meshInfo[m].mesh == null || meshInfo[m].vertices == null || meshInfo[m].colors32 == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void WriteMesh(TMP_TextInfo textInfo, bool hasEffect)
        {
            var characterCount = textInfo.characterCount;
            var characters = textInfo.characterInfo;
            var meshInfo = textInfo.meshInfo;

            if (hasEffect)
            {
                PrepareRanks(characters, characterCount);
            }

            for (var i = 0; i < characterCount; i++)
            {
                ref var character = ref characters[i];

                if (character.isVisible == false)
                {
                    continue;
                }

                var m = character.materialReferenceIndex;
                var vertexIndex = character.vertexIndex;

                if (m < 0 || m >= _sourceMaterialCount)
                {
                    continue;
                }

                var sourceVertices = _sourceVertices[m];
                var sourceColors = _sourceColors[m];
                var vertices = meshInfo[m].vertices;
                var colors = meshInfo[m].colors32;
                var last = vertexIndex + 3;

                var outOfRange = sourceVertices == null
                    || sourceColors == null
                    || last >= sourceVertices.Length
                    || last >= sourceColors.Length
                    || last >= vertices.Length
                    || last >= colors.Length;

                if (vertexIndex < 0 || outOfRange)
                {
                    continue;
                }

                if (hasEffect == false)
                {
                    Array.Copy(sourceVertices, vertexIndex, vertices, vertexIndex, 4);
                    Array.Copy(sourceColors, vertexIndex, colors, vertexIndex, 4);
                    continue;
                }

                var pose = EvaluatePose(i, characterCount);
                var pivot = GetPivot(i, ref character);
                var radians = pose.Rotation * Mathf.Deg2Rad;
                var sin = Mathf.Sin(radians);
                var cos = Mathf.Cos(radians);

                for (var v = vertexIndex; v <= last; v++)
                {
                    vertices[v] = CharacterEffectMath.Transform(
                          sourceVertices[v]
                        , pivot
                        , pose.Offset
                        , sin
                        , cos
                        , pose.Scale
                    );

                    colors[v] = CharacterEffectMath.Tint(sourceColors[v], pose.Tint, pose.Alpha);
                }
            }
        }

        // The pivot point, blended like the rotation and scale it serves: the latest finished clip that turns or
        // scales the character is the rest pivot, and the active ones add theirs with their weights.
        private Vector2 GetPivot(int index, ref TMP_CharacterInfo character)
        {
            var bottomLeft = character.bottomLeft;
            var topRight = character.topRight;
            var baseLine = character.baseLine;
            var center = CharacterEffectMath.Pivot(CharacterPivot.Center, bottomLeft, topRight, baseLine);
            var pivot = default(Vector2Accumulator);

            for (var h = 0; h < _holdCount; h++)
            {
                var behaviour = _holds[h].Behaviour;

                if (behaviour.TurnsOrScales && behaviour.IsInRange(index, _sourceCharacterCount))
                {
                    var point = CharacterEffectMath.Pivot(behaviour.Pivot, bottomLeft, topRight, baseLine);
                    pivot.OfferHold(point, _holds[h].EndTime);
                }
            }

            for (var c = 0; c < _contributionCount; c++)
            {
                var behaviour = _contributions[c].Behaviour;

                if (behaviour.TurnsOrScales && _ranks[c].Ranks[index] >= 0)
                {
                    var point = CharacterEffectMath.Pivot(behaviour.Pivot, bottomLeft, topRight, baseLine);
                    pivot.Add(point, _contributions[c].Weight);
                }
            }

            return pivot.Resolve(center, center);
        }

        private Pose EvaluatePose(int index, int characterCount)
        {
            var offset = default(Vector2Accumulator);
            var rotation = default(FloatAccumulator);
            var scale = default(Vector2Accumulator);
            var alpha = default(FloatAccumulator);
            var tint = default(ColorAccumulator);

            for (var h = 0; h < _holdCount; h++)
            {
                var hold = _holds[h];
                var behaviour = hold.Behaviour;

                if (behaviour.IsInRange(index, characterCount) == false)
                {
                    continue;
                }

                var eased = behaviour.Ease.Evaluate(1f);
                OfferHold(behaviour, eased, hold.EndTime, ref offset, ref rotation, ref scale, ref alpha, ref tint);
            }

            var offsetAdd = Vector2.zero;
            var rotationAdd = 0f;
            var scaleAdd = Vector2.zero;

            for (var c = 0; c < _contributionCount; c++)
            {
                ref var rank = ref _ranks[c];
                var characterRank = rank.Ranks[index];

                if (characterRank < 0)
                {
                    continue;
                }

                var contribution = _contributions[c];
                var behaviour = contribution.Behaviour;
                var weight = contribution.Weight;
                var u = CharacterEffectMath.Normalize(characterRank, rank.MinRank, rank.MaxRank);
                var progress = CharacterEffectMath.CharacterProgress(contribution.Progress, behaviour.Stagger, u);
                var eased = behaviour.Ease.Evaluate(progress);

                AddTransitions(behaviour, eased, weight, ref offset, ref rotation, ref scale, ref alpha, ref tint);

                var wave = behaviour.Wave;

                if (wave.Enabled)
                {
                    var unit = CharacterEffectMath.Wave(
                          wave.Shape
                        , wave.Cycles
                        , wave.PhasePerCharacter
                        , contribution.Progress
                        , characterRank
                    );

                    var value = wave.Amplitude * (unit * weight);
                    AddOffset(wave.Target, value, ref offsetAdd, ref rotationAdd, ref scaleAdd);
                }

                var shake = behaviour.Shake;

                if (shake.Enabled)
                {
                    var value = CharacterEffectMath.Shake(shake, contribution.Progress, index) * weight;
                    AddOffset(shake.Target, value, ref offsetAdd, ref rotationAdd, ref scaleAdd);
                }
            }

            return new Pose(
                  offset.Resolve(Vector2.zero, Vector2.zero) + offsetAdd
                , (rotation.HasContribution ? rotation.Resolve(0f) : 0f) + rotationAdd
                , scale.Resolve(Vector2.one, Vector2.one) + scaleAdd
                , alpha.HasContribution ? alpha.Resolve(1f) : 1f
                , tint.HasContribution ? tint.Resolve(Color.white) : Color.white
            );
        }

        private static void OfferHold(
              TmpCharacterBehaviour behaviour
            , float eased
            , double endTime
            , ref Vector2Accumulator offset
            , ref FloatAccumulator rotation
            , ref Vector2Accumulator scale
            , ref FloatAccumulator alpha
            , ref ColorAccumulator tint
        )
        {
            var offsetChannel = behaviour.Offset;

            if (offsetChannel.Enabled)
            {
                offset.OfferHold(Vector2.LerpUnclamped(offsetChannel.From, offsetChannel.To, eased), endTime);
            }

            var rotationChannel = behaviour.Rotation;

            if (rotationChannel.Enabled)
            {
                rotation.OfferHold(Mathf.LerpUnclamped(rotationChannel.From, rotationChannel.To, eased), endTime);
            }

            var scaleChannel = behaviour.Scale;

            if (scaleChannel.Enabled)
            {
                scale.OfferHold(Vector2.LerpUnclamped(scaleChannel.From, scaleChannel.To, eased), endTime);
            }

            var alphaChannel = behaviour.Alpha;

            if (alphaChannel.Enabled)
            {
                alpha.OfferHold(Mathf.LerpUnclamped(alphaChannel.From, alphaChannel.To, eased), endTime);
            }

            var colorChannel = behaviour.Color;

            if (colorChannel.Enabled)
            {
                tint.OfferHold(Color.LerpUnclamped(colorChannel.From, colorChannel.To, eased), endTime);
            }
        }

        private static void AddTransitions(
              TmpCharacterBehaviour behaviour
            , float eased
            , float weight
            , ref Vector2Accumulator offset
            , ref FloatAccumulator rotation
            , ref Vector2Accumulator scale
            , ref FloatAccumulator alpha
            , ref ColorAccumulator tint
        )
        {
            var offsetChannel = behaviour.Offset;

            if (offsetChannel.Enabled)
            {
                offset.Add(Vector2.LerpUnclamped(offsetChannel.From, offsetChannel.To, eased), weight);
            }

            var rotationChannel = behaviour.Rotation;

            if (rotationChannel.Enabled)
            {
                rotation.Add(Mathf.LerpUnclamped(rotationChannel.From, rotationChannel.To, eased), weight);
            }

            var scaleChannel = behaviour.Scale;

            if (scaleChannel.Enabled)
            {
                scale.Add(Vector2.LerpUnclamped(scaleChannel.From, scaleChannel.To, eased), weight);
            }

            var alphaChannel = behaviour.Alpha;

            if (alphaChannel.Enabled)
            {
                alpha.Add(Mathf.LerpUnclamped(alphaChannel.From, alphaChannel.To, eased), weight);
            }

            var colorChannel = behaviour.Color;

            if (colorChannel.Enabled)
            {
                tint.Add(Color.LerpUnclamped(colorChannel.From, colorChannel.To, eased), weight);
            }
        }

        private static void AddOffset(
              OscillationTarget target
            , Vector3 value
            , ref Vector2 offset
            , ref float rotation
            , ref Vector2 scale
        )
        {
            switch (target)
            {
                case OscillationTarget.Rotation:
                    rotation += value.z;
                    break;

                case OscillationTarget.Scale:
                    scale += new Vector2(value.x, value.y);
                    break;

                default:
                    offset += new Vector2(value.x, value.y);
                    break;
            }
        }

        // One rank table per contribution slot, rebuilt only when the clip, its timing or the text layout changed.
        private void PrepareRanks(TMP_CharacterInfo[] characters, int characterCount)
        {
            EnsureScratchCapacity(characterCount);

            for (var c = 0; c < _contributionCount; c++)
            {
                var behaviour = _contributions[c].Behaviour;
                ref var cache = ref _ranks[c];

                if (cache.Matches(behaviour, _layoutVersion, characterCount))
                {
                    continue;
                }

                if (cache.Ranks == null || cache.Ranks.Length < characterCount)
                {
                    cache.Ranks = new int[Math.Max(characterCount, MIN_CAPACITY)];
                }

                var ranks = cache.Ranks;

                for (var i = 0; i < characterCount; i++)
                {
                    ranks[i] = -1;
                }

                behaviour.GetRange(characterCount, out var start, out var end);

                var visibleCount = 0;

                for (var i = start; i < end; i++)
                {
                    if (CharacterEffectMath.IsRenderable(characters[i]))
                    {
                        _visible[visibleCount++] = i;
                    }
                }

                CharacterEffectMath.FillRanks(
                      behaviour.Order
                    , behaviour.OrderSeed
                    , _visible
                    , visibleCount
                    , _visibleRanks
                    , _sortKeys
                    , _sortItems
                );

                var minRank = 0;
                var maxRank = 0;

                for (var k = 0; k < visibleCount; k++)
                {
                    var rank = _visibleRanks[k];

                    ranks[_visible[k]] = rank;

                    if (k == 0)
                    {
                        minRank = rank;
                        maxRank = rank;
                    }
                    else
                    {
                        minRank = Math.Min(minRank, rank);
                        maxRank = Math.Max(maxRank, rank);
                    }
                }

                RankBuildCount++;
                cache.Store(behaviour, _layoutVersion, characterCount, visibleCount, minRank, maxRank);
            }
        }

        private bool IsSameAsPrevious()
        {
            if (_contributionCount != _previousContributionCount || _holdCount != _previousHoldCount)
            {
                return false;
            }

            for (var c = 0; c < _contributionCount; c++)
            {
                if (_contributions[c].Equals(_previousContributions[c]) == false)
                {
                    return false;
                }
            }

            for (var h = 0; h < _holdCount; h++)
            {
                if (_holds[h].Equals(_previousHolds[h]) == false)
                {
                    return false;
                }
            }

            return true;
        }

        private void SavePrevious()
        {
            Array.Copy(_contributions, _previousContributions, _contributionCount);
            Array.Copy(_holds, _previousHolds, _holdCount);
            _previousContributionCount = _contributionCount;
            _previousHoldCount = _holdCount;
        }

        // Sized to the input count on the first frame; grows (doubling) only if a frame has more.
        private void EnsureContributionCapacity(int required)
        {
            if (_contributions.Length >= required)
            {
                return;
            }

            var capacity = Math.Max(Math.Max(required, MIN_CAPACITY), _contributions.Length * 2);

            Array.Resize(ref _contributions, capacity);
            Array.Resize(ref _previousContributions, capacity);
            Array.Resize(ref _ranks, capacity);
            EnsureHoldCapacity(capacity);
        }

        private void EnsureHoldCapacity(int required)
        {
            if (_holds.Length >= required)
            {
                return;
            }

            var capacity = Math.Max(Math.Max(required, MIN_CAPACITY), _holds.Length * 2);

            Array.Resize(ref _holds, capacity);
            Array.Resize(ref _previousHolds, capacity);
        }

        private void EnsureScratchCapacity(int required)
        {
            if (_visible.Length >= required)
            {
                return;
            }

            var capacity = Math.Max(Math.Max(required, MIN_CAPACITY), _visible.Length * 2);

            _visible = new int[capacity];
            _visibleRanks = new int[capacity];
            _sortKeys = new uint[capacity];
            _sortItems = new int[capacity];
        }

        private readonly struct Contribution : IEquatable<Contribution>
        {
            public readonly TmpCharacterBehaviour Behaviour;
            public readonly float Weight;
            public readonly float Progress;

            public Contribution(TmpCharacterBehaviour behaviour, float weight, float progress)
            {
                Behaviour = behaviour;
                Weight = weight;
                Progress = progress;
            }

            public bool Equals(Contribution other)
                => ReferenceEquals(Behaviour, other.Behaviour) && Weight == other.Weight && Progress == other.Progress;
        }

        private readonly struct Hold : IEquatable<Hold>
        {
            public readonly TmpCharacterBehaviour Behaviour;
            public readonly double EndTime;

            public Hold(TmpCharacterBehaviour behaviour, double endTime)
            {
                Behaviour = behaviour;
                EndTime = endTime;
            }

            public bool Equals(Hold other)
                => ReferenceEquals(Behaviour, other.Behaviour) && EndTime == other.EndTime;
        }

        private readonly struct Pose
        {
            public readonly Vector2 Offset;
            public readonly float Rotation;
            public readonly Vector2 Scale;
            public readonly float Alpha;
            public readonly Color Tint;

            public Pose(Vector2 offset, float rotation, Vector2 scale, float alpha, Color tint)
            {
                Offset = offset;
                Rotation = rotation;
                Scale = scale;
                Alpha = alpha;
                Tint = tint;
            }
        }

        // The ranks of one contribution slot, by character index (-1: not animated by that clip).
        private struct RankCache
        {
            public int[] Ranks;
            public int VisibleCount;
            public int MinRank;
            public int MaxRank;

            private TmpCharacterBehaviour _behaviour;
            private CharacterOrder _order;
            private int _seed;
            private int _rangeStart;
            private int _rangeLength;
            private int _version;
            private int _characterCount;

            public readonly bool Matches(TmpCharacterBehaviour behaviour, int version, int characterCount)
                => Ranks != null
                    && ReferenceEquals(_behaviour, behaviour)
                    && _version == version
                    && _characterCount == characterCount
                    && _order == behaviour.Order
                    && _seed == behaviour.OrderSeed
                    && _rangeStart == behaviour.RangeStart
                    && _rangeLength == behaviour.RangeLength;

            public void Store(
                  TmpCharacterBehaviour behaviour
                , int version
                , int characterCount
                , int visibleCount
                , int minRank
                , int maxRank
            )
            {
                _behaviour = behaviour;
                _order = behaviour.Order;
                _seed = behaviour.OrderSeed;
                _rangeStart = behaviour.RangeStart;
                _rangeLength = behaviour.RangeLength;
                _version = version;
                _characterCount = characterCount;
                VisibleCount = visibleCount;
                MinRank = minRank;
                MaxRank = maxRank;
            }
        }
    }
}
