using System;
using System.Collections.Generic;
using UnityEngine;
using ApexionGame.UguiPlayable.Core;

namespace ApexionGame.UguiPlayable.Tracks.Groups
{
    /// <summary>
    /// The channels of a <see cref="CanvasGroupClip"/>: alpha (blended), interactable and blocks raycasts (steps).
    /// </summary>
    [Serializable]
    public sealed class CanvasGroupBehaviour : TweenBehaviour
    {
        internal const string ALPHA_PROPERTY = "m_Alpha";
        internal const string INTERACTABLE_PROPERTY = "m_Interactable";
        internal const string BLOCKS_RAYCASTS_PROPERTY = "m_BlocksRaycasts";

        [SerializeField]
        private TweenChannel<float> _alpha = new(0f, 1f);

        [SerializeField]
        private StepChannel _interactable = new(true, enabled: false);

        [SerializeField]
        private StepChannel _blocksRaycasts = new(true, enabled: false);

        /// <summary>The <see cref="CanvasGroup.alpha"/> channel.</summary>
        public TweenChannel<float> Alpha
        {
            get => _alpha;
            set => _alpha = value;
        }

        /// <summary>The <see cref="CanvasGroup.interactable"/> channel.</summary>
        public StepChannel Interactable
        {
            get => _interactable;
            set => _interactable = value;
        }

        /// <summary>The <see cref="CanvasGroup.blocksRaycasts"/> channel.</summary>
        public StepChannel BlocksRaycasts
        {
            get => _blocksRaycasts;
            set => _blocksRaycasts = value;
        }

        /// <inheritdoc/>
        public override void CollectPropertyNames(ICollection<string> names)
        {
            if (_alpha.Enabled)
            {
                names.Add(ALPHA_PROPERTY);
            }

            if (_interactable.Enabled)
            {
                names.Add(INTERACTABLE_PROPERTY);
            }

            if (_blocksRaycasts.Enabled)
            {
                names.Add(BLOCKS_RAYCASTS_PROPERTY);
            }
        }

        /// <inheritdoc/>
        public override void OnActivated(Component binding)
        {
            if (_alpha.Enabled && _alpha.Mode == ValueMode.FromCurrent && binding is CanvasGroup group)
            {
                _alpha.Capture(group.alpha);
            }
        }

        /// <inheritdoc/>
        public override void OnDeactivated()
        {
            _alpha.ClearCapture();
        }
    }
}
