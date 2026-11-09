using UnityEngine;
using UnityEngine.Playables;

namespace ApexionGame.UguiPlayable.Samples.Showcase
{
    /// <summary>
    /// Plays the Showcase timeline again from its first frame. It sits on the <c>screen-showcase</c> root next to the
    /// <see cref="PlayableDirector"/> that plays the trailer.
    /// </summary>
    /// <remarks>
    /// The trailer holds its last frame when it ends. <see cref="Replay"/> takes no arguments, so the Replay button
    /// calls it from its <c>onClick</c> event in the Inspector; a persistent UnityEvent call cannot pass the
    /// <see cref="double"/> that <see cref="PlayableDirector.time"/> needs. This is a sample helper, not part of the
    /// package API.
    /// </remarks>
    public sealed class ShowcaseReplay : MonoBehaviour
    {
        [SerializeField]
        private PlayableDirector _director;

        /// <summary>Rewinds the trailer to its first frame, shows that frame, then plays it.</summary>
        public void Replay()
        {
            if (_director == null)
            {
                return;
            }

            _director.time = 0d;
            _director.Evaluate();
            _director.Play();
        }
    }
}
