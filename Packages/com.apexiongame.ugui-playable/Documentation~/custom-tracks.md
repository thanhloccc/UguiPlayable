# Writing a custom track (preview)

> **Preview API.** The first release is `0.1.0-preview.1`. The base classes on this page are marked "Preview API" in
> their XML docs and may change before 1.0. Serialized field names and enum integer values of the package types are
> frozen. A change to one would use `FormerlySerializedAs` and be listed in the CHANGELOG.

You can write a track for any component by deriving from four small classes. The track reuses everything else:
blending, holds, easing, value modes, and the restore. Your code only says what to read, what to write, and how.

## What you write

| Class | Derives from | Job |
|---|---|---|
| Behaviour | `TweenBehaviour` | The serialized channels of one clip |
| Clip | `TweenClip<TBehaviour>` | The clip asset. Usually empty |
| Mixer | `TweenMixer<TBinding, TBehaviour>` | Reads the base, blends the clips, writes the component |
| Track | `TweenTrack<TBinding, TBehaviour, TMixer>` | The track: binding type, clip type, colour, menu name |

The base classes are in the namespace `ApexionGame.UguiPlayable.Core`. Your concrete classes can live in your own
assembly. Make the classes `public`: Timeline needs to find them, and the mixer is named as a generic argument of
the public track.

Your assembly definition needs these references:

- `ApexionGame.UguiPlayable`
- `Unity.Timeline`
- `UnityEngine.UI`
- `Unity.TextMeshPro`, if you bind to TMP

## Example: an Aspect Ratio Fitter track

This track tweens `AspectRatioFitter.aspectRatio`. It is the same shape as the package's own Slider track.

### Behaviour

```csharp
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

[Serializable]
public sealed class AspectRatioBehaviour : TweenBehaviour
{
    // The serialized name on the component, for Timeline's Edit Mode preview.
    internal const string ASPECT_RATIO_PROPERTY = "m_AspectRatio";

    [SerializeField]
    private TweenChannel<float> _aspectRatio = new(1f, 2f);

    public TweenChannel<float> AspectRatio
    {
        get => _aspectRatio;
        set => _aspectRatio = value;
    }

    public override void CollectPropertyNames(ICollection<string> names)
    {
        if (_aspectRatio.Enabled)
        {
            names.Add(ASPECT_RATIO_PROPERTY);
        }
    }

    // Only needed for ValueMode.FromCurrent: capture the live value when the clip becomes active.
    public override void OnActivated(Component binding)
    {
        if (_aspectRatio.Enabled
            && _aspectRatio.Mode == ValueMode.FromCurrent
            && binding is AspectRatioFitter fitter)
        {
            _aspectRatio.Capture(fitter.aspectRatio);
        }
    }

    public override void OnDeactivated()
    {
        _aspectRatio.ClearCapture();
    }
}
```

`CollectPropertyNames` must list only names that exist on the bound component. An unknown name logs a
`DrivenPropertyManager` error.

### Clip

```csharp
public sealed class AspectRatioClip : TweenClip<AspectRatioBehaviour>
{
}
```

The clip supports Timeline blending and extrapolation.

### Mixer

```csharp
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

public sealed class AspectRatioMixer : TweenMixer<AspectRatioFitter, AspectRatioBehaviour>
{
    private float _baseValue;
    private FloatAccumulator _aspectRatio;
    private bool _wrote;

    // The track bound the component: remember its current value.
    protected override void CaptureBase(AspectRatioFitter binding)
    {
        _baseValue = binding.aspectRatio;
        _wrote = false;
    }

    // The graph is destroyed and Restore Mode is Revert: write the base back.
    protected override void RestoreBase(AspectRatioFitter binding)
    {
        if (_wrote)
        {
            binding.aspectRatio = _baseValue;
        }
    }

    // Start of each frame.
    protected override void BeginFrame()
    {
        _aspectRatio.Reset();
    }

    // A clip with weight: add its value at the current progress.
    protected override void Accumulate(AspectRatioBehaviour behaviour, float weight, float progress)
    {
        var channel = behaviour.AspectRatio;

        if (channel.Enabled)
        {
            _aspectRatio.Add(TweenChannelMath.Evaluate(channel, progress, _baseValue), weight);
        }
    }

    // A clip that has finished: offer its end value as the hold value.
    protected override void AccumulateHold(AspectRatioBehaviour behaviour, double endTime)
    {
        var channel = behaviour.AspectRatio;

        if (channel.Enabled)
        {
            _aspectRatio.OfferHold(TweenChannelMath.Evaluate(channel, 1f, _baseValue), endTime);
        }
    }

    // End of each frame: write the properties that received a contribution.
    protected override void Apply(AspectRatioFitter binding)
    {
        if (_aspectRatio.HasContribution)
        {
            binding.aspectRatio = _aspectRatio.Resolve(_baseValue);
            _wrote = true;
        }
    }
}
```

### Track

```csharp
using UnityEngine.Timeline;
using UnityEngine.UI;
using ApexionGame.UguiPlayable.Core;

[TrackColor(0.6f, 0.6f, 0.9f)]
[TrackClipType(typeof(AspectRatioClip))]
[TrackBindingType(typeof(AspectRatioFitter))]
#if UNITY_EDITOR
[System.ComponentModel.DisplayName("My Tracks/Aspect Ratio")]
#endif
public sealed class AspectRatioTrack : TweenTrack<AspectRatioFitter, AspectRatioBehaviour, AspectRatioMixer>
{
}
```

After compilation the track appears under **My Tracks > Aspect Ratio** in the Timeline track menu.

## The rules of the mixer

`ProcessFrame` is sealed. It drives these abstract members in order:

1. When the binding changes, `CaptureBase` stores the base values.
2. Each frame, `BeginFrame` resets the accumulators.
3. `Accumulate` is called for every clip with weight, with that clip's progress (0 to 1).
4. `AccumulateHold` is called for every clip that has finished.
5. `Apply` writes each property that received a contribution. A property no clip touched is not written.
6. When the graph is destroyed with Restore Mode = Revert, `RestoreBase` writes the base values back.

Follow these rules:

- **Do not allocate** in these callbacks. Use the public accumulator structs (`FloatAccumulator`, `ColorAccumulator`,
  `Vector2Accumulator`, `Vector3Accumulator`, `Vector4Accumulator`, `StepAccumulator`) and the math helpers
  (`TweenChannelMath`, `OscillationMath`).
- **Write only what you wrote.** In `RestoreBase`, restore only the properties your mixer wrote.
- **Pause is not restore.** Pausing the director does not destroy the graph, so nothing is restored.
- **A mixer needs a public parameterless constructor.** The implicit one is enough.

The channel structs you can use in a behaviour:

| Struct | For |
|---|---|
| `TweenChannel<T>` | `float`, `Vector2`, `Vector3`, `Vector4`, `Color`, with value mode, ease and ignored axes |
| `StepChannel` | A bool that switches at a threshold. Use `new StepChannel(value)` for the 0.5 default |
| `TextChannel` | A string tween with scramble |
| `OscillationChannel` | Punch and shake offsets (create with `OscillationChannel.Punch` or `.Shake`) |

A `default(StepChannel)` has a threshold of 0 and is disabled. Use the constructor.

## Optional hooks

Use these only when your component needs them. They are all Preview API.

| Hook | Where | Use it when |
|---|---|---|
| `GatherProperty(binding, name, driver)` | track, `protected virtual` | The animated value is not a serialized property of the binding itself. Register the real objects and properties. The Scroll Rect and Toggle tracks do this |
| `RestoreUnserialized(binding)` | mixer, `protected virtual` | A value is not serialized, so Timeline's Edit Mode preview cannot revert it. Called when the graph is destroyed in Edit Mode, whatever Restore Mode is. TMP Text uses it for `maxVisibleCharacters` |
| `OnInputActivated(behaviour, binding)` | mixer, `protected virtual` | You need the mixer's own state when a clip becomes active. Called right after `OnActivated` |

## A setting on the track

If the mixer needs a setting from the track (like **Send callbacks** on the Slider track), keep the setting in a
`[SerializeField]` field on your track and pass it to the mixer in an override:

```csharp
public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
{
    var playable = base.CreateTrackMixer(graph, go, inputCount);
    ((ScriptPlayable<MyMixer>)playable).GetBehaviour().MySetting = _mySetting;
    return playable;
}
```

This is the pattern the package's own control tracks use. A cleaner hook for it may come before 1.0.

## Editor support

All editor types in this package are internal. A custom clip gets Unity's **default inspector**, not the package's clip
card. That means:

- the clip shows the raw fields of each channel (`_enabled`, `_mode`, `_from`, `_to`, `_ease`, `_ignoredAxes`);
- there is no "animates nothing" notice and no Set from binding button;
- the ease field still uses the package's ease drawer, because the drawer is bound to `EaseSetting`.

## What may change before 1.0

- `TweenTrack`: a hook to pass a track setting to the mixer; `CreateTrackMixer` and `GatherProperties` may be sealed;
  `RestoreMode` may become opt-in.
- `TweenMixer`: `OnPlayableDestroy` may be sealed behind a new hook; `RestoreUnserialized` and `OnInputActivated` may
  change in signature; `Binding` and `RestoreMode` may become `protected`.
- `TweenBehaviour`: `OnActivated(Component)` may become typed.
- The channel structs may lose their capture state members, and may gain setters or `With...` helpers.
- `StepAccumulator.Add` may change its parameter order.

Also see [value-modes-and-restore.md](value-modes-and-restore.md) for what the base classes do for you.
