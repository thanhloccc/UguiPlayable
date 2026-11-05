using UnityEngine.UIElements;

namespace ApexionGame.UguiPlayable.Editor.Common
{
    /// <summary>
    /// Draws a <see cref="Toggle"/> as a pill switch. Every bool in the package's editor UI uses it.
    /// </summary>
    internal static class SwitchToggleExtensions
    {
        public const string SWITCH_USS_CLASS_NAME = "ugp-switch";

        /// <summary>
        /// Applies the shared switch look and the aligned-field layout to <paramref name="self"/>.
        /// </summary>
        public static Toggle WithSwitchStyle(this Toggle self)
        {
            CommonStyleSheetPaths.AddTo(self);
            self.AddToClassList(BaseField<bool>.alignedFieldUssClassName);
            self.AddToClassList(SWITCH_USS_CLASS_NAME);
            return self;
        }
    }
}
