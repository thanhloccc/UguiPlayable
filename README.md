# UGUI Playable

[![License: MIT](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Unity 6000.3+](https://img.shields.io/badge/Unity-6000.3%2B-black?logo=unity)](https://unity.com/releases/editor/archive)
[![Version](https://img.shields.io/github/v/tag/thanhloccc/UguiPlayable?include_prereleases&sort=semver&label=version)](https://github.com/thanhloccc/UguiPlayable/tags)

Timeline tracks, clips and UI Toolkit editor tooling for animating uGUI in Unity: move, fade, scale, recolor and fill
UI elements on a Timeline instead of writing tween code.

> Status: preview (`0.1.0-preview.2`). 15 tracks, a UI Toolkit clip inspector, multi-clip editing and 27 starter
> presets are in. The base classes for custom tracks are marked "Preview API" and may change before 1.0.

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
https://github.com/thanhloccc/UguiPlayable.git?path=/Packages/com.apexiongame.ugui-playable
```

OpenUPM: planned.

## Documentation

- [Package README](Packages/com.apexiongame.ugui-playable/README.md): requirements, install, quick start.
- [Manual](Packages/com.apexiongame.ugui-playable/Documentation~/index.md): every track and channel, value modes,
  presets, custom tracks and troubleshooting.
- [Getting started](Packages/com.apexiongame.ugui-playable/Documentation~/getting-started.md): a fading panel in five
  minutes.
- [Changelog](Packages/com.apexiongame.ugui-playable/CHANGELOG.md)

## License

MIT. See [LICENSE](LICENSE) and
[THIRD-PARTY-NOTICES.md](Packages/com.apexiongame.ugui-playable/THIRD-PARTY-NOTICES.md).
