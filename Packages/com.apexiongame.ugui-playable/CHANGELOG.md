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
- TMP Characters track (`UGUI Playable/Text/TMP Characters`, binds a `TMP_Text`): animates each character of the text. Transitions (offset, rotation, scale, alpha, tint) blend toward the rest pose and hold their end value; a wave (sine or bounce) and a per-character shake add offsets with no hold. Each clip has a character range, a stagger, an order (forward, backward, centre out, edges in, random by seed), an ease and a pivot; the default clip is a per-character fade-in. The track writes the text mesh and re-applies it whenever TMP rebuilds the text, so a TMP Text track (colour, typewriter, string tween) on the same text keeps working with it; hidden characters are skipped. Limits: one character track per text (the track shows a warning otherwise); Reverse geometry sorting is not supported (warning; the track writes nothing); underline and strikethrough stay still; scales well above 1 soften the SDF edges. The track always restores the text when the timeline stops, whatever Restore Mode would say, so its inspector has no Restore Mode.

### Changed

- Slider and Scrollbar tracks no longer raise `onValueChanged` by default; turn on Send callbacks on the track for Play Mode callbacks. A Scrollbar track no longer scrolls a linked ScrollRect: to scroll content, animate the ScrollRect track, which updates its scrollbars; or turn on Send callbacks.
