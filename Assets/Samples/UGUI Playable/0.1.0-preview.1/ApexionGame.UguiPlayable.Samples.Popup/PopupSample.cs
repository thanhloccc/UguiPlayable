using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;

namespace ApexionGame.UguiPlayable.Samples.Popup
{
    /// <summary>
    /// Plays the popup's open and close timelines from two buttons. Each timeline stops the other, and the close
    /// timeline uses From current channels, so it starts from wherever the open timeline left the popup.
    /// </summary>
    public sealed class PopupSample : MonoBehaviour
    {
        [SerializeField]
        private PlayableDirector _open;

        [SerializeField]
        private PlayableDirector _close;

        [SerializeField]
        private Button _openButton;

        [SerializeField]
        private Button _closeButton;

        private void OnEnable()
        {
            _openButton.onClick.AddListener(Open);
            _closeButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            _openButton.onClick.RemoveListener(Open);
            _closeButton.onClick.RemoveListener(Close);
        }

        /// <summary>Plays the open timeline from the start.</summary>
        public void Open()
        {
            Switch(_close, _open);
        }

        /// <summary>Plays the close timeline from the start.</summary>
        public void Close()
        {
            Switch(_open, _close);
        }

        // Stop (not Pause) destroys the other graph, so the next Play builds a fresh one whose From current channels
        // capture the popup where it is now. The tracks use Restore Mode Keep, so stopping leaves the values in place.
        private static void Switch(PlayableDirector from, PlayableDirector to)
        {
            from.Stop();
            to.Stop();
            to.Play();
        }
    }
}
