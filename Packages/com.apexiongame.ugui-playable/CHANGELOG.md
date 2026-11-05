# Changelog

All notable changes to this package are documented in this file.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the package uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- Package skeleton: runtime, Editor and EditMode test assemblies.
- Editor icons use the `@2x` images on high-DPI displays.
- RectTransform clips: Rotation mode Shortest path (quaternion); Euler stays the default.
- The sample adds its UI input module at runtime, so it works with the Input System or the Input Manager.
- Toggle track (`UGUI Playable/Controls/Toggle`): animates `isOn` with an Is On step channel (the value applies when the clip passes its threshold). Send callbacks (off by default, Play Mode only) works as on Slider and Scrollbar. Previewing a toggle in a `ToggleGroup` leaves the group as it was, and the checkmark matches the restored state. In Edit Mode, Unity itself calls `onValueChanged` once per toggle of the group when the preview ends. With `allowSwitchOff` off, the group turns the last toggle back on.

### Changed

- Slider and Scrollbar tracks no longer raise `onValueChanged` by default; turn on Send callbacks on the track for Play Mode callbacks. A Scrollbar track no longer scrolls a linked ScrollRect: to scroll content, animate the ScrollRect track, which updates its scrollbars; or turn on Send callbacks.
