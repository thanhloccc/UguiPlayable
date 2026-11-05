using UnityEngine;
using UnityEngine.Playables;

namespace ApexionGame.UguiPlayable.Samples
{
    /// <summary>
    /// Plays a page's Show and Hide timelines. It sits on the page root (screen, popup or overlay prefab) next to
    /// the two <see cref="PlayableDirector"/>s that play the shared transition timelines on this page's nodes.
    /// </summary>
    /// <remarks>
    /// The page is activated before Show plays and deactivated when Hide ends. Starting one timeline stops the other,
    /// so a page hidden while it is still showing continues from where it is (From current channels).
    /// <see cref="Show"/> and <see cref="Hide"/> take no arguments, so buttons can call them from their
    /// <c>onClick</c> event in the Inspector. This is a sample helper, not a page-flow framework.
    /// </remarks>
    public sealed class SamplePage : MonoBehaviour
    {
        [SerializeField]
        private PlayableDirector _show;

        [SerializeField]
        private PlayableDirector _hide;

        private bool _hiding;

        private void OnEnable()
        {
            if (_hide != null)
            {
                _hide.stopped += OnHideStopped;
            }
        }

        private void OnDisable()
        {
            if (_hide != null)
            {
                _hide.stopped -= OnHideStopped;
            }
        }

        /// <summary>Activates the page and plays its Show timeline.</summary>
        public void Show()
        {
            _hiding = false;
            gameObject.SetActive(true);

            if (_hide != null)
            {
                _hide.Stop();
            }

            if (_show != null)
            {
                _show.Stop();
                _show.Play();
            }
        }

        /// <summary>Plays the Hide timeline; the page deactivates when it ends.</summary>
        public void Hide()
        {
            if (gameObject.activeInHierarchy == false)
            {
                return;
            }

            // Stop() raises PlayableDirector.stopped, so the flag is set only after stopping.
            _hiding = false;

            if (_show != null)
            {
                _show.Stop();
            }

            if (_hide == null)
            {
                gameObject.SetActive(false);
                return;
            }

            _hide.Stop();
            _hiding = true;
            _hide.Play();
        }

        private void OnHideStopped(PlayableDirector director)
        {
            if (_hiding)
            {
                _hiding = false;
                gameObject.SetActive(false);
            }
        }
    }
}
