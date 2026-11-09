# Changelog

All notable changes to this package are documented in this file.
The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and the package uses [Semantic Versioning](https://semver.org/).

## [Unreleased]

### Added

- TMP Text track: the Font Size card notes that the channel has no effect while the bound text has Auto Size on, and
  the track row and track inspector warn while any clip enables Font Size.

### Changed

- `package.json` links the documentation, changelog and licence, so the Package Manager window shows them.
- TMP Text track: the string tween shows `from` unchanged at progress 0 and reveals in even steps (the count is
  `floor(progress * length)`), and the typewriter floors instead of rounding, so the first character appears at 1/n
  of the ratio, not at 0.5/n. The `text-scramble-reveal` preset has Visible Ratio off, so its scramble shows.

### Fixed

- TMP Characters track: the pivot is blended with the weights of the rotation and scale it serves, so it no longer
  pops at the start of a crossfade between a Center and a Baseline clip, and the latest finished clip decides the
  rest pivot.
- TMP Characters track: the stagger and wave phase of letters already shown no longer jump while a typewriter reveals
  more letters (the order counts every character TMP would draw, not only the visible ones), and Centre out and Edges
  in use the full 0 to 1 range for an even character count.
- TMP Text track: `\r\n` in a string tween no longer becomes two line breaks, and a revealed position shows `to` even
  where `from` has a newline.
- TMP Text track: the typewriter count no longer lags one frame behind the string tween of the same track.

## [0.1.0-preview.2] - 2026-11-05

### Fixed

- Clip inspector: long row labels (Strength, Frequency, Elasticity, Amplitude, Stagger…) no longer overlap their
  fields; labels that still do not fit end with an ellipsis.

## [0.1.0-preview.1] - 2026-11-05

### Added

- Package skeleton: runtime, Editor and EditMode test assemblies.
- Editor icons use the `@2x` images on high-DPI displays.
- RectTransform clips: Rotation mode Shortest path (quaternion); Euler stays the default.
- The sample adds its UI input module at runtime, so it works with the Input System or the Input Manager.
- Toggle track (`UGUI Playable/Controls/Toggle`): animates `isOn` with an Is On step channel (the value applies when the clip passes its threshold). Send callbacks (off by default, Play Mode only) works as on Slider and Scrollbar. Previewing a toggle in a `ToggleGroup` leaves the group as it was, and the checkmark matches the restored state. In Edit Mode, Unity itself calls `onValueChanged` once per toggle of the group when the preview ends. With `allowSwitchOff` off, the group turns the last toggle back on.
- TMP Characters track (`UGUI Playable/Text/TMP Characters`, binds a `TMP_Text`): animates each character of the text. Transitions (offset, rotation, scale, alpha, tint) blend toward the rest pose and hold their end value; a wave (sine or bounce) and a per-character shake add offsets with no hold. Each clip has a character range, a stagger, an order (forward, backward, centre out, edges in, random by seed), an ease and a pivot; the default clip is a per-character fade-in. The track writes the text mesh and re-applies it whenever TMP rebuilds the text, so a TMP Text track (colour, typewriter, string tween) on the same text keeps working with it; hidden characters are skipped. Limits: one character track per text (the track shows a warning otherwise); Reverse geometry sorting is not supported (warning; the track writes nothing); underline and strikethrough stay still; scales well above 1 soften the SDF edges. The track always restores the text when the timeline stops, whatever Restore Mode would say, so its inspector has no Restore Mode.
- Multi-clip editing: select several clips of one type in the Timeline window to edit them together in one clip card. Fields whose values differ show the mixed dash; an edit writes only the changed field (for a vector, only the edited axis), so each clip keeps its other axes, and one Undo reverts all of them. The axis buttons, mode controls and the summary ("N-M of K channels on") handle mixed states; the seed button gives each clip its own seed; Set from binding works when every clip's track is bound to the same object and is disabled with a reason otherwise.
- Presets: a `Tween Preset` asset (`Assets/Create/UGUI Playable/Tween Preset`) names a preset timeline and gives its category, description, icon and, per track, the hierarchy path it binds to. **Add Preset…** (Timeline context menu, `UGUI Playable`) adds the preset's tracks and clips at the playhead and binds them by path from a binding root (by default the director's parent when the director sits on a `transition-*` child, otherwise the director's GameObject), in one Undo step; a missing path or component gives a warning and leaves that track unbound, and with no director (asset mode or a sub-timeline) the tracks are added unbound. When other directors (open, or in prefabs and scenes on disk) use the same timeline asset, the picker warns and offers **Duplicate and add**: the timeline is copied next to the original, the director plays the copy with its bindings, and the preset goes into the copy. **Apply preset…** (the clip card header button, or the clip context menu) copies the matching preset clip's values into every selected clip in one Undo step and keeps their timing and names. The starter set ships 27 presets: Popup (`popup-pop-in`, `popup-pop-out`, `popup-slide-up-in`, `popup-slide-down-out`, `popup-fade-in`, `popup-fade-out`), Screen (`screen-fade-in`, `screen-fade-out`, `screen-push-left-in`, `screen-push-left-out`, `screen-push-right-in`, `screen-push-right-out`, `screen-zoom-in`), Toast (`toast-drop-in`, `toast-rise-out`), Button (`button-press`, `button-pop`), Attention (`attention-shake`, `attention-wiggle`, `attention-pulse`), Text (`text-typewriter`, `text-scramble-reveal`, `text-char-fade-in`, `text-char-bounce-in`, `text-wave`) and List (`list-stagger-fade-up`, `list-stagger-pop`, single-track presets for the stagger recipe). Scale, alpha and colour are Absolute and hides start From current. Popup and toast slides use a Relative offset (show: offset to 0; hide: 0 to offset on a Revert track, so the element returns to rest when the graph stops in Play Mode). Screen pushes animate the body's anchors. Button and attention presets use punch and shake, so the element ends exactly at its start values.
- Preset browser: `Window/UGUI Playable/Presets` lists every Tween Preset of the project and the installed packages by category, with search. Selecting a preset shows its icon, description, the tracks it creates and the paths they bind to, plus any problems. **Add to timeline** adds it to the timeline open in the Timeline window at the playhead (disabled with the reason when none is open), with the same binding root and shared-timeline handling as Add Preset…. **Duplicate** copies the preset and its timeline into a folder you pick under `Assets/`, because presets inside an installed package are read-only; **Ping** selects the asset.
- Stagger recipe (in the Presets window): takes a container GameObject, a preset with a single track and a delay, and adds one track per active child under a Group track named after the container, in sibling order. Clip `i` starts `i × delay` after the playhead, and each track is bound to its child's component. Children without that component get no track and one warning lists them. It is one Undo step. There is no runtime stagger track.

### Changed

- Slider and Scrollbar tracks no longer raise `onValueChanged` by default; turn on Send callbacks on the track for Play Mode callbacks. A Scrollbar track no longer scrolls a linked ScrollRect: to scroll content, animate the ScrollRect track, which updates its scrollbars; or turn on Send callbacks.
