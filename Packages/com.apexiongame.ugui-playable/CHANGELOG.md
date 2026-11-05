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

### Changed

- Slider and Scrollbar tracks no longer raise `onValueChanged` by default; turn on Send callbacks on the track for Play Mode callbacks. A Scrollbar track no longer scrolls a linked ScrollRect: to scroll content, animate the ScrollRect track, which updates its scrollbars; or turn on Send callbacks.
