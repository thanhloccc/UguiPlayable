using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Graphics
{
    /// <summary>
    /// Blends <see cref="GraphicClip"/> channels into the bound <see cref="Graphic"/>.
    /// </summary>
    public sealed class GraphicMixer : TweenMixer<Graphic, GraphicBehaviour>
    {
        private GraphicColorBlend _blend;

        /// <inheritdoc/>
        protected override void CaptureBase(Graphic binding)
        {
            _blend.Capture(binding);
        }

        /// <inheritdoc/>
        protected override void RestoreBase(Graphic binding)
        {
            _blend.Restore(binding);
        }

        /// <inheritdoc/>
        protected override void BeginFrame()
        {
            _blend.Reset();
        }

        /// <inheritdoc/>
        protected override void Accumulate(GraphicBehaviour behaviour, float weight, float progress)
        {
            _blend.Accumulate(behaviour.Color, behaviour.Alpha, weight, progress);
        }

        /// <inheritdoc/>
        protected override void AccumulateHold(GraphicBehaviour behaviour, double endTime)
        {
            _blend.AccumulateHold(behaviour.Color, behaviour.Alpha, endTime);
        }

        /// <inheritdoc/>
        protected override void Apply(Graphic binding)
        {
            _blend.Apply(binding);
        }
    }
}
