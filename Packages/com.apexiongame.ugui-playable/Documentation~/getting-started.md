# Getting started

This page takes you from an empty scene to a panel that fades in. It should take under five minutes.

You need the package installed. See the package [README](../README.md).

## 1. Make a panel

1. Create a new scene and add a canvas: **GameObject > UI > Canvas**.
2. Add a panel under the canvas: right-click the canvas, **UI > Panel**. Rename it `Panel`.
3. Select `Panel` and click **Add Component**. Add a `CanvasGroup`.

## 2. Create a timeline

1. With `Panel` selected, open **Window > Sequencing > Timeline**.
2. Click **Create**. Unity asks where to save the timeline asset. Save it as `panel-show`.

Unity adds a `PlayableDirector` to `Panel`. Its **Play On Awake** option is on by default, so the timeline plays when
you enter Play Mode.

## 3. Add a Canvas Group track

1. In the Timeline window, click the **+** next to the track list (or right-click the empty track area).
2. Choose **UGUI Playable > Groups > Canvas Group**.
3. Drag `Panel` from the Hierarchy onto the new track's binding slot, or pick its `CanvasGroup` there.

The track is now bound to the panel's `CanvasGroup`.

## 4. Add a clip

1. Right-click the track and choose **Add Canvas Group Clip**.
2. Drag the right edge of the clip to make it about one second long.
3. Select the clip. The inspector shows the clip card.

The **Alpha** channel is on by default, from 0 to 1, with a Linear ease. The other two channels, **Interactable** and
**Blocks Raycasts**, are off. You do not have to change anything.

## 5. Preview and play

1. Drag the playhead across the clip. The panel fades in. Timeline previews the clip in Edit Mode and reverts the
   values when you stop previewing.
2. Press **Play**. The panel fades from transparent to opaque over the clip length and stays visible.

## Make it nicer

- Change the **Ease** of the Alpha channel to Out Quad for a softer end.
- Turn on **Interactable** and **Blocks Raycasts**. Each is a step channel: its value applies once the clip passes the
  threshold (0.5 by default), so the panel accepts clicks half-way through the fade. See
  [tracks-groups-and-layout.md](tracks-groups-and-layout.md).
- Add a **Rect Transform** track bound to `Panel` and a clip that moves it. See
  [tracks-transform.md](tracks-transform.md).
- Skip the manual steps: right-click the timeline and choose **UGUI Playable > Add Preset…**. See
  [presets.md](presets.md).

## If something does not work

- The panel does not change in Play Mode: check that the track has a binding and that the clip is not muted.
- The clip inspector says "This clip animates nothing. Turn on a channel.": turn on at least one channel.
- More help: [troubleshooting.md](troubleshooting.md).

## See it all at work

Import the **Showcase** sample (**Window > Package Manager > UGUI Playable > Samples**) for a 42-second trailer that
uses every track. Open its scene, press Play, and open `showcase-trailer` in the Timeline window to see how each
effect is built. Import the TMP Essential Resources first. See [troubleshooting.md](troubleshooting.md).

Next: [value-modes-and-restore.md](value-modes-and-restore.md).
