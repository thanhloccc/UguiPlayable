using UnityEngine;
using ApexionGame.UguiPlayable.Easing;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Editor;

/// <summary>
/// Test-only asset that holds an <see cref="EaseSetting"/> for the drawer tests.
/// </summary>
public sealed class EaseSettingHolder : ScriptableObject
{
    public EaseSetting Setting;
}
