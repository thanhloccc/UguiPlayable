# UGUI Playable

Timeline tracks, clips and UI Toolkit editor tooling for animating uGUI in Unity: move, fade, scale, recolor and fill
UI elements on a Timeline instead of writing tween code.

> Status: early development. The package skeleton exists; tracks are being designed.

It is a maintained successor in spirit to
[TweenPlayables](https://github.com/AnnulusGames/TweenPlayables), focused on uGUI, with UI Toolkit editors.

## Repository layout

| Path | Content |
|---|---|
| `Packages/com.apexiongame.ugui-playable/` | The package (runtime, Editor, tests) |
| `Assets/Samples/UGUI Playable/` | Sample sources (shipped as package samples) |
| `Assets/ApexionGame/` | Development sandbox |
| `docs/conventions/` | Project, coding and commit conventions |

The development project also contains [EncosyTower](https://github.com/laicasaane/EncosyTower) as a design reference
and development tooling. The package does not depend on it.

## Install

Requires Unity 6000.3 or newer. In **Window > Package Manager**, choose **+ > Install package from git URL…**:

```text
https://github.com/<owner>/<repo>.git?path=Packages/com.apexiongame.ugui-playable
```

## License

MIT. See [LICENSE](LICENSE).
