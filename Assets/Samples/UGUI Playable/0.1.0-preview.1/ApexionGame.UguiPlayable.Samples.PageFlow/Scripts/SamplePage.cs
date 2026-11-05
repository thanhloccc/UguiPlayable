using System;
using UnityEngine;
using UnityEngine.Playables;

namespace ApexionGame.UguiPlayable.Samples.PageFlow
{
    /// <summary>
    /// A screen, popup or overlay whose transitions are two UGUI Playable timelines: Show and Hide.
    /// </summary>
    /// <remarks>
    /// The page is activated before Show plays and deactivated when Hide ends. Starting one timeline stops the other,
    /// so a page can be hidden while it is still showing; From current channels then continue from where it is.
    /// The directors live outside the page, so they keep running while the page is inactive.
    /// </remarks>
    public sealed class SamplePage : MonoBehaviour
    {
        [SerializeField]
        private PlayableDirector _show;

        [SerializeField]
        private PlayableDirector _hide;

        private PlayableDirector _running;
        private Action _onDone;
        private bool _deactivateWhenDone;

        /// <summary>Whether the page was last asked to show.</summary>
        public bool IsVisible { get; private set; }

        private void Awake()
        {
            Subscribe(_show);
            Subscribe(_hide);
        }

        private void OnDestroy()
        {
            Unsubscribe(_show);
            Unsubscribe(_hide);
        }

        /// <summary>Activates the page and plays its Show timeline; <paramref name="onDone"/> runs when it ends.</summary>
        public void Show(Action onDone = null)
        {
            gameObject.SetActive(true);
            IsVisible = true;
            Play(_show, _hide, onDone, deactivateWhenDone: false);
        }

        /// <summary>Plays the Hide timeline, then deactivates the page and runs <paramref name="onDone"/>.</summary>
        public void Hide(Action onDone = null)
        {
            IsVisible = false;
            Play(_hide, _show, onDone, deactivateWhenDone: true);
        }

        private void Play(PlayableDirector director, PlayableDirector other, Action onDone, bool deactivateWhenDone)
        {
            // Clear the running director first, so the stopped events raised below are ignored.
            _running = null;

            if (other != null)
            {
                other.Stop();
            }

            if (director == null)
            {
                Finish(onDone, deactivateWhenDone);
                return;
            }

            director.Stop();
            _onDone = onDone;
            _deactivateWhenDone = deactivateWhenDone;
            _running = director;
            director.Play();
        }

        private void OnStopped(PlayableDirector director)
        {
            if (director != _running)
            {
                return;
            }

            _running = null;
            var onDone = _onDone;
            _onDone = null;
            Finish(onDone, _deactivateWhenDone);
        }

        private void Finish(Action onDone, bool deactivate)
        {
            if (deactivate)
            {
                gameObject.SetActive(false);
            }

            onDone?.Invoke();
        }

        private void Subscribe(PlayableDirector director)
        {
            if (director != null)
            {
                director.stopped += OnStopped;
            }
        }

        private void Unsubscribe(PlayableDirector director)
        {
            if (director != null)
            {
                director.stopped -= OnStopped;
            }
        }
    }
}
