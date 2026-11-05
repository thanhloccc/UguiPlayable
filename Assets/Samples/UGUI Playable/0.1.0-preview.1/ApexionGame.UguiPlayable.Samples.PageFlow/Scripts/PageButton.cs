using UnityEngine;
using UnityEngine.UI;

namespace ApexionGame.UguiPlayable.Samples.PageFlow
{
    /// <summary>What a <see cref="PageButton"/> asks the <see cref="PageNavigator"/> to do.</summary>
    public enum PageAction
    {
        /// <summary>Nothing; useful for a button that only shows a toast.</summary>
        None = 0,

        /// <summary>Push the target screen.</summary>
        Push = 1,

        /// <summary>Go back to the previous screen.</summary>
        Back = 2,

        /// <summary>Open the target popup.</summary>
        OpenPopup = 3,

        /// <summary>Close the top popup.</summary>
        ClosePopup = 4,
    }

    /// <summary>
    /// Wires a <see cref="Button"/> to one navigator action, plus an optional toast message.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public sealed class PageButton : MonoBehaviour
    {
        [SerializeField]
        private PageNavigator _navigator;

        [SerializeField]
        private PageAction _action;

        [SerializeField]
        private SamplePage _target;

        [SerializeField]
        private string _toast;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            _button.onClick.AddListener(OnClick);
        }

        private void OnDisable()
        {
            _button.onClick.RemoveListener(OnClick);
        }

        private void OnClick()
        {
            switch (_action)
            {
                case PageAction.Push:
                    _navigator.Push(_target);
                    break;

                case PageAction.Back:
                    _navigator.Back();
                    break;

                case PageAction.OpenPopup:
                    _navigator.OpenPopup(_target);
                    break;

                case PageAction.ClosePopup:
                    _navigator.ClosePopup();
                    break;
            }

            if (string.IsNullOrEmpty(_toast) == false)
            {
                _navigator.Toast(_toast);
            }
        }
    }
}
