using UnityEngine;
using UnityEngine.EventSystems;

namespace ApexionGame.UguiPlayable.Samples.Showcase
{
    /// <summary>
    /// Adds the UI input module that fits the project's input setting to the EventSystem at runtime.
    /// </summary>
    /// <remarks>
    /// The Showcase scene serializes no input module, so it imports cleanly with or without the Input System package.
    /// The Input System module is added when the project uses the Input System; otherwise the Input Manager module is
    /// added when the legacy Input Manager is enabled. Under "Both", the Input System wins. A module already on the
    /// GameObject is left alone. This is a sample helper, not part of the package API.
    /// </remarks>
    public sealed class ShowcaseInputModule : MonoBehaviour
    {
        private void Awake()
        {
            if (GetComponent<BaseInputModule>() != null)
            {
                return;
            }

#if ENABLE_INPUT_SYSTEM && UGP_SHOWCASE_INPUT_SYSTEM
            gameObject.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#elif ENABLE_LEGACY_INPUT_MANAGER
            gameObject.AddComponent<StandaloneInputModule>();
#else
            Debug.LogWarning("UGUI Playable Showcase: no UI input module. Install the Input System package, or set Player Settings > Active Input Handling to Input Manager or Both.");
#endif
        }
    }
}
