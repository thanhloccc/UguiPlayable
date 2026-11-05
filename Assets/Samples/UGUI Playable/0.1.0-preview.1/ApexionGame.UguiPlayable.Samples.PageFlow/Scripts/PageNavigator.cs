using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ApexionGame.UguiPlayable.Samples.PageFlow
{
    /// <summary>
    /// A minimal page flow: a screen history (push and back), a popup stack over the current screen, and a toast.
    /// Requests that arrive while a screen or popup transition is running are ignored.
    /// </summary>
    public sealed class PageNavigator : MonoBehaviour
    {
        [SerializeField]
        private SamplePage _firstScreen;

        [SerializeField]
        private SamplePage _toast;

        [SerializeField]
        private TMP_Text _toastText;

        private readonly Stack<SamplePage> _screens = new();
        private readonly Stack<SamplePage> _popups = new();
        private bool _busy;

        private void Start()
        {
            _screens.Push(_firstScreen);
            _busy = true;
            _firstScreen.Show(Done);
        }

        /// <summary>Hides the current screen and shows <paramref name="screen"/>.</summary>
        public void Push(SamplePage screen)
        {
            if (_busy || screen == null || _popups.Count > 0 || _screens.Peek() == screen)
            {
                return;
            }

            _busy = true;
            _screens.Peek().Hide();
            _screens.Push(screen);
            screen.Show(Done);
        }

        /// <summary>Hides the current screen and shows the previous one.</summary>
        public void Back()
        {
            if (_busy || _screens.Count <= 1 || _popups.Count > 0)
            {
                return;
            }

            _busy = true;
            _screens.Pop().Hide();
            _screens.Peek().Show(Done);
        }

        /// <summary>Shows <paramref name="popup"/> over the current screen.</summary>
        public void OpenPopup(SamplePage popup)
        {
            if (_busy || popup == null || _popups.Contains(popup))
            {
                return;
            }

            _busy = true;
            _popups.Push(popup);
            popup.Show(Done);
        }

        /// <summary>Hides the top popup.</summary>
        public void ClosePopup()
        {
            if (_busy || _popups.Count == 0)
            {
                return;
            }

            _busy = true;
            _popups.Pop().Hide(Done);
        }

        /// <summary>Shows the toast with <paramref name="message"/>. Its timeline slides in, holds and slides out.</summary>
        public void Toast(string message)
        {
            if (_toastText != null)
            {
                _toastText.text = message;
            }

            _toast.Show(HideToast);
        }

        private void HideToast()
        {
            _toast.Hide();
        }

        private void Done()
        {
            _busy = false;
        }
    }
}
