# Tween presets

A tween preset is a ready-made animation: a small timeline of tracks and clips, plus a description that says which
object each track binds to. The package ships 27 starter presets for popups, screens, toasts, buttons, attention
effects, text and lists. You can also make your own.

## The three uses

All three work from the same preset asset.

### 1. Add to timeline

Adds the preset's tracks and clips to the timeline you are editing.

- **Timeline window:** right-click and choose **UGUI Playable > Add Preset…**. A picker opens. The tracks are added at
  the playhead.
- **Preset browser:** **Window > UGUI Playable > Presets**. Pick a preset and click **Add to timeline**. The button is
  disabled, with a reason, when no timeline is open in the Timeline window.

The tool binds each track by hierarchy path, starting at the **binding root** (below). It is one Undo step. The tool
also copies the preset track's Restore Mode and Send callbacks settings.

When something is missing, the tool warns and carries on:

- If a path or a component is missing, the warning names it and that track stays unbound.
- If there is no director (you are editing a timeline asset, or a sub-timeline), the tracks are added unbound.

### 2. Apply to clips

Copies a preset's values into clips you already have.

- Select clips, then click the **Apply preset…** button in the clip card header, or right-click the clips and choose
  **UGUI Playable > Apply Preset…**.
- The picker lists presets that contain this type of clip.
- The values of the matching preset clip are copied into **every selected clip** in one Undo step. Each clip keeps its
  timing and its name.

### 3. Use as is, or duplicate

A preset's timeline is an ordinary timeline asset. You can assign it to a director yourself and bind its tracks by hand.
To change a preset, use **Duplicate** in the preset browser. It copies the preset and its timeline into a folder you
pick under `Assets/`. Presets inside the installed package are read-only.

The browser also has **Ping**, which selects the preset asset in the Project window.

## The binding root

Preset paths are relative to a **binding root** GameObject. An empty path means the root itself.

- By default the root is the director's GameObject.
- When the director sits on a child whose name starts with `transition-`, the root is that child's **parent**. This
  follows the UguiPages standard.
- You can set a different root in the picker or the browser (**Binding root**).

The starter presets assume this hierarchy:

| Kind | Paths |
|---|---|
| Popup | `backdrop`, `body/popup` |
| Screen | the root (empty path), `body` |
| Toast | `body/toast` |
| Button, attention | the element itself (empty path) |
| Text | the TMP text itself (empty path) |
| List | each child of a container (see the stagger recipe) |

If your hierarchy differs, change the binding root, or bind the tracks by hand after adding.

## Shared timelines

Many directors can play the same timeline asset. Adding a preset to it would change all of them, and the others would
not get bindings for the new tracks.

The picker and the browser count the other users of the timeline: directors in the open scenes and prefab stage, and in
prefabs and scenes on disk. When there are others, they show a warning and a **Duplicate and add** switch:

- It copies the timeline next to the original and saves the copy.
- It moves the director's bindings to the copy's tracks and makes the director play the copy.
- It adds the preset to the copy.
- One Undo step reverts the director and the added tracks. The copy stays on disk.

Save your scenes and prefabs before you add a preset. The scan of files on disk is cached until the project changes.

## Value modes in presets (the D21 caveat)

The starter presets use these rules:

- Scale, alpha and colour use **Absolute**. Hides use **From current**, so a half-faded element fades out from where
  it is.
- Position slides use **Relative**. A show is an offset to 0. A hide is 0 to an offset on a track set to **Revert**, so
  the element returns to rest when the graph stops in Play Mode.
- Screen pushes animate the body's **anchors**, in Absolute (shows) and From current (hides).
- Button and attention presets use punch and shake. The element ends at exactly its start values.

**Caveat for Relative slides.** A hide that is interrupted by a show snaps the element back to the rest pose first, and
then the show runs. Relative values are measured from the base captured when the graph was built, so a Relative slide in
a timeline shared by several directors can also drift if the timeline does not return to its start. Only
`popup-slide-up-in`, `popup-slide-down-out`, `toast-drop-in` and `toast-rise-out` use Relative. If you need slides in a
shared show/hide timeline, change them to Absolute targets or From current.

In Edit Mode preview, scrubbing back restores the element.

## Making your own preset

1. Build a timeline with the tracks and clips you want. Name the tracks by role, for example `popup-alpha`.
2. Save it, then choose **Assets > Create > UGUI Playable > Tween Preset**.
3. Fill in the asset:
   - **Timeline:** your timeline asset.
   - **Category**, **Description** and **Icon:** shown in the picker and the browser.
   - **Bindings:** one entry per track: the track name and the path from the binding root. An empty path is the root.
4. The picker and the browser list problems: an unknown icon, no timeline, a track with no path, or a binding whose
   track is not in the timeline.

The preset browser lists every preset in the project and the installed packages, by category, with search.

## Stagger recipe

In the preset browser, the stagger section builds a staggered list:

1. Pick a **container** GameObject, a preset with a single track, and a delay (default 0.05 seconds).
2. Click the stagger button.

The tool adds one track per active child of the container, under a group track named after the container, in sibling
order. Clip `i` starts `i x delay` after the playhead. Each track is bound to its child's component. Children without
that component get no track, and one warning lists them. It is one Undo step.

The presets for this recipe are `list-stagger-fade-up` and `list-stagger-pop`. There is no runtime stagger track.

## The 27 starter presets

All starter presets hold their end value after the clip, except `attention-pulse`, which loops. The package keeps them
in `ApexionGame.UguiPlayable.Editor/Presets/Library/`. Durations and eases are first values; tune them to taste.

### Popup

Binds `backdrop` and `body/popup`.

| Preset | What it does |
|---|---|
| `popup-pop-in` | Backdrop fades in (0.3 s). The popup fades in (0.2 s) and scales 0.8 to 1 (0.3 s, Out Back) |
| `popup-pop-out` | Backdrop and popup fade out (From current, 0.2 s). The popup scales to 0.8 |
| `popup-slide-up-in` | Backdrop and popup fade in. The popup slides up from 300 units below (Relative) |
| `popup-slide-down-out` | Backdrop and popup fade out. The popup slides 300 units down (Relative, Revert) |
| `popup-fade-in` | Backdrop and popup fade in (0.25 s). Interactable and blocks raycasts switch on half-way |
| `popup-fade-out` | Backdrop and popup fade out (From current, 0.2 s). Interactable and blocks raycasts switch off at the start |

### Screen

Binds the root and `body`.

| Preset | What it does |
|---|---|
| `screen-fade-in` | Root alpha 0 to 1 (0.3 s) |
| `screen-fade-out` | Root alpha From current to 0 (0.25 s) |
| `screen-push-left-in` | Body anchors from x 1..2 to 0..1: the screen enters from the right (0.35 s) |
| `screen-push-left-out` | Body anchors From current to x -1..0: the screen leaves to the left |
| `screen-push-right-in` | Body anchors from x -1..0 to 0..1: the screen enters from the left |
| `screen-push-right-out` | Body anchors From current to x 1..2: the screen leaves to the right |
| `screen-zoom-in` | Root alpha 0 to 1 while the body scales 1.1 to 1 (0.3 s) |

### Toast

Binds `body/toast`.

| Preset | What it does |
|---|---|
| `toast-drop-in` | Alpha 0 to 1 (0.25 s). Drops in from 120 units above (Relative, Out Back) |
| `toast-rise-out` | Alpha From current to 0 (0.25 s). Lifts away 120 units up (Relative, Revert) |

### Button

Binds the button itself.

| Preset | What it does |
|---|---|
| `button-press` | A short scale punch of -0.1 (0.2 s) |
| `button-pop` | A springy scale punch of +0.15 (0.3 s) |

### Attention

Binds the element itself.

| Preset | What it does |
|---|---|
| `attention-shake` | A horizontal position shake of 12 units (0.4 s), for an error nudge |
| `attention-wiggle` | A rotation shake of 8 degrees about z (0.5 s) |
| `attention-pulse` | A scale punch of 0.1 (0.8 s) with Loop extrapolation. Lengthen the clip to pulse longer |

### Text

Binds the TMP text itself.

| Preset | What it does |
|---|---|
| `text-typewriter` | The typewriter ratio goes 0 to 1 (1.5 s) |
| `text-scramble-reveal` | Reveals the text with scrambled characters (1.2 s). The To text is "Your text here": change it after adding |
| `text-char-fade-in` | Each character fades in with a 12 unit rise, left to right (0.8 s). Uses the [TMP Characters track](tmp-character-track.md) |
| `text-char-bounce-in` | Each character bounces in: scale 0 to 1 with alpha (0.9 s, Out Back) |
| `text-wave` | Every character moves up and down along a sine of 10 units (2 s, 2 cycles) |

### List

For the stagger recipe above. Each preset has one track.

| Preset | What it does |
|---|---|
| `list-stagger-fade-up` | One alpha fade per item (0.3 s). Every child needs a `CanvasGroup`. The name suggests movement but the preset only fades. Pair it with `list-stagger-pop` |
| `list-stagger-pop` | One scale pop per item (0.6 to 1, 0.35 s, Out Back). Binds each child's `RectTransform` |

Next: [custom-tracks.md](custom-tracks.md).
