using NUnit.Framework;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.UI;
using UnityEngine.UIElements;
using ApexionGame.UguiPlayable.Core;
using ApexionGame.UguiPlayable.Editor.Clips;
using ApexionGame.UguiPlayable.Editor.Timeline;
using ApexionGame.UguiPlayable.Editor.Tracks;
using ApexionGame.UguiPlayable.Tracks.Controls;
using ApexionGame.UguiPlayable.Tracks.Effects;
using ApexionGame.UguiPlayable.Tracks.Graphics;
using ApexionGame.UguiPlayable.Tracks.Groups;
using ApexionGame.UguiPlayable.Tracks.Layout;
using ApexionGame.UguiPlayable.Tracks.Text;
using ApexionGame.UguiPlayable.Tracks.Transforms;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Tests.EditorMode.Editor;

public sealed class TweenTrackEditorTests
{
    private TimelineAsset _timeline;
    private GameObject _parent;
    private GameObject _child;

    [SetUp]
    public void SetUp()
    {
        _timeline = ScriptableObject.CreateInstance<TimelineAsset>();
    }

    [TearDown]
    public void TearDown()
    {
        if (_timeline.IsValid())
        {
            foreach (var track in System.Linq.Enumerable.ToList(_timeline.GetOutputTracks()))
            {
                foreach (var clip in System.Linq.Enumerable.ToList(track.GetClips()))
                {
                    Object.DestroyImmediate(clip.asset);
                }

                Object.DestroyImmediate(track);
            }

            Object.DestroyImmediate(_timeline);
        }

        if (_parent.IsValid())
        {
            Object.DestroyImmediate(_parent);
        }

        _timeline = null;
        _parent = null;
        _child = null;
    }

    [Test]
    public void ClipEditor_ReportsAnErrorWhenNoChannelIsOn()
    {
        var track = _timeline.CreateTrack<RectTransformTrack>(null, "Rect");
        var timelineClip = track.CreateClip<RectTransformClip>();
        var editor = new RectTransformClipEditor();

        var options = editor.GetClipOptions(timelineClip);

        Assert.AreEqual(TweenClipTexts.NO_CHANNEL_ENABLED, options.errorText);
    }

    [Test]
    public void ClipEditor_HasNoErrorWhenAChannelIsOn()
    {
        var track = _timeline.CreateTrack<RectTransformTrack>(null, "Rect");
        var timelineClip = track.CreateClip<RectTransformClip>();
        var clip = (RectTransformClip)timelineClip.asset;
        var editor = new RectTransformClipEditor();

        clip.Template.Scale = new TweenChannel<Vector3>(Vector3.one, Vector3.one * 2f);

        var options = editor.GetClipOptions(timelineClip);

        Assert.IsTrue(string.IsNullOrEmpty(options.errorText));
    }

    [Test]
    public void ClipEditor_CountsStepChannelsAsOn()
    {
        var track = _timeline.CreateTrack<CanvasGroupTrack>(null, "Group");
        var timelineClip = track.CreateClip<CanvasGroupClip>();
        var clip = (CanvasGroupClip)timelineClip.asset;
        var editor = new CanvasGroupClipEditor();

        clip.Template.Alpha = new TweenChannel<float>(0f, 1f, enabled: false);

        Assert.AreEqual(TweenClipTexts.NO_CHANNEL_ENABLED, editor.GetClipOptions(timelineClip).errorText);

        clip.Template.Interactable = new StepChannel(true);

        Assert.IsTrue(string.IsNullOrEmpty(editor.GetClipOptions(timelineClip).errorText));
    }

    [Test]
    public void ClipEditor_ProvidesTheTrackIcon()
    {
        var track = _timeline.CreateTrack<CanvasGroupTrack>(null, "Group");
        var timelineClip = track.CreateClip<CanvasGroupClip>();

        var options = new CanvasGroupClipEditor().GetClipOptions(timelineClip);

        Assert.IsNotNull(options.icons);
        Assert.IsNotNull(System.Linq.Enumerable.FirstOrDefault(options.icons));
    }

    [Test]
    public void ClipEditor_NamesANewClipAfterTheTrack()
    {
        var track = _timeline.CreateTrack<RectTransformTrack>(null, "Rect");
        var timelineClip = track.CreateClip<RectTransformClip>();

        new RectTransformClipEditor().OnCreate(timelineClip, track, null);

        Assert.AreEqual("Rect Transform", timelineClip.displayName);
    }

    [Test]
    public void ClipEditor_KeepsTheNameOfACopiedClip()
    {
        var track = _timeline.CreateTrack<RectTransformTrack>(null, "Rect");
        var timelineClip = track.CreateClip<RectTransformClip>();
        var source = track.CreateClip<RectTransformClip>();

        timelineClip.displayName = "Slide in";
        new RectTransformClipEditor().OnCreate(timelineClip, track, source);

        Assert.AreEqual("Slide in", timelineClip.displayName);
    }

    [Test]
    public void TrackEditor_WarnsUnderALayoutGroup()
    {
        var track = _timeline.CreateTrack<RectTransformTrack>(null, "Rect");
        var child = CreateChildUnder(typeof(HorizontalLayoutGroup));

        var options = new RectTransformTrackEditor().GetTrackOptions(track, child);

        Assert.AreEqual(TweenClipTexts.LAYOUT_WARNING, options.errorText);
        Assert.IsNotNull(options.icon);
    }

    [Test]
    public void TrackEditor_DoesNotWarnWithIgnoreLayout()
    {
        var track = _timeline.CreateTrack<RectTransformTrack>(null, "Rect");
        var child = CreateChildUnder(typeof(HorizontalLayoutGroup));

        child.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;

        var options = new RectTransformTrackEditor().GetTrackOptions(track, child);

        Assert.IsTrue(string.IsNullOrEmpty(options.errorText));
    }

    [Test]
    public void TrackEditor_WarnsAgainWhenIgnoreLayoutIsOff()
    {
        var track = _timeline.CreateTrack<RectTransformTrack>(null, "Rect");
        var child = CreateChildUnder(typeof(VerticalLayoutGroup));

        child.gameObject.AddComponent<LayoutElement>().ignoreLayout = false;

        var options = new RectTransformTrackEditor().GetTrackOptions(track, child);

        Assert.AreEqual(TweenClipTexts.LAYOUT_WARNING, options.errorText);
    }

    [Test]
    public void TrackEditor_DoesNotWarnWithoutALayoutGroup()
    {
        var track = _timeline.CreateTrack<RectTransformTrack>(null, "Rect");
        var child = CreateChildUnder();

        var options = new RectTransformTrackEditor().GetTrackOptions(track, child);

        Assert.IsTrue(string.IsNullOrEmpty(options.errorText));
    }

    [Test]
    public void TrackEditor_DoesNotWarnWhenTheGroupIsDisabled()
    {
        var child = CreateChildUnder(typeof(HorizontalLayoutGroup));

        _parent.GetComponent<HorizontalLayoutGroup>().enabled = false;

        Assert.IsFalse(RectTransformLayoutCheck.IsDrivenByLayoutGroup(child));
    }

    [Test]
    public void TrackEditor_WarnsForAContentSizeFitter()
    {
        var track = _timeline.CreateTrack<RectTransformTrack>(null, "Rect");
        var child = CreateChildUnder();

        child.gameObject.AddComponent<ContentSizeFitter>();

        var options = new RectTransformTrackEditor().GetTrackOptions(track, child);

        Assert.AreEqual(TweenClipTexts.SIZE_FITTER_WARNING, options.errorText);
    }

    [Test]
    public void TrackEditor_DoesNotWarnForADisabledSizeFitter()
    {
        var child = CreateChildUnder();

        child.gameObject.AddComponent<ContentSizeFitter>().enabled = false;

        Assert.IsFalse(RectTransformLayoutCheck.IsSizeDrivenByFitter(child));
    }

    [Test]
    public void TrackEditor_DoesNotWarnUnderAScrollRect()
    {
        var track = _timeline.CreateTrack<RectTransformTrack>(null, "Rect");
        var child = CreateChildUnder(typeof(ScrollRect));

        var options = new RectTransformTrackEditor().GetTrackOptions(track, child);

        Assert.IsFalse(RectTransformLayoutCheck.IsDrivenByLayoutGroup(child));
        Assert.IsTrue(string.IsNullOrEmpty(options.errorText));
    }

    [Test]
    public void TrackEditor_ProvidesTheTrackIconForEveryTrack()
    {
        Assert.IsNotNull(GetIcon(_timeline.CreateTrack<CanvasGroupTrack>(null, "a"), new CanvasGroupTrackEditor()));
        Assert.IsNotNull(GetIcon(_timeline.CreateTrack<GraphicTrack>(null, "b"), new GraphicTrackEditor()));
        Assert.IsNotNull(GetIcon(_timeline.CreateTrack<ImageTrack>(null, "c"), new ImageTrackEditor()));
        Assert.IsNotNull(GetIcon(_timeline.CreateTrack<SliderTrack>(null, "d"), new SliderTrackEditor()));
        Assert.IsNotNull(GetIcon(_timeline.CreateTrack<ScrollbarTrack>(null, "e"), new ScrollbarTrackEditor()));
        Assert.IsNotNull(GetIcon(_timeline.CreateTrack<ScrollRectTrack>(null, "f"), new ScrollRectTrackEditor()));
        Assert.IsNotNull(GetIcon(_timeline.CreateTrack<RawImageTrack>(null, "g"), new RawImageTrackEditor()));
        Assert.IsNotNull(GetIcon(_timeline.CreateTrack<ShadowTrack>(null, "h"), new ShadowTrackEditor()));
        Assert.IsNotNull(GetIcon(_timeline.CreateTrack<LayoutElementTrack>(null, "i"), new LayoutElementTrackEditor()));
        Assert.IsNotNull(GetIcon(_timeline.CreateTrack<TmpTextTrack>(null, "j"), new TmpTextTrackEditor()));
        Assert.IsNotNull(GetIcon(_timeline.CreateTrack<LayoutGroupTrack>(null, "k"), new LayoutGroupTrackEditor()));
        Assert.IsNotNull(GetIcon(_timeline.CreateTrack<GridLayoutGroupTrack>(null, "l"), new GridLayoutGroupTrackEditor()));
    }

    [Test]
    public void ScrollRectTrackEditor_ReportsAnErrorWithoutAContent()
    {
        var track = _timeline.CreateTrack<ScrollRectTrack>(null, "Scroll");
        _parent = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect));

        var scrollRect = _parent.GetComponent<ScrollRect>();
        var editor = new ScrollRectTrackEditor();

        Assert.AreEqual(TweenClipTexts.SCROLL_RECT_NO_CONTENT, editor.GetTrackOptions(track, scrollRect).errorText);

        _child = new GameObject("Content", typeof(RectTransform));
        scrollRect.content = (RectTransform)_child.transform;

        Assert.IsTrue(string.IsNullOrEmpty(editor.GetTrackOptions(track, scrollRect).errorText));
    }

    [Test]
    public void ControlClipEditors_NameANewClipAfterTheControl()
    {
        var slider = _timeline.CreateTrack<SliderTrack>(null, "S");
        var sliderClip = slider.CreateClip<SliderClip>();
        new SliderClipEditor().OnCreate(sliderClip, slider, null);

        var bar = _timeline.CreateTrack<ScrollbarTrack>(null, "B");
        var barClip = bar.CreateClip<ScrollbarClip>();
        new ScrollbarClipEditor().OnCreate(barClip, bar, null);

        var scroll = _timeline.CreateTrack<ScrollRectTrack>(null, "R");
        var scrollClip = scroll.CreateClip<ScrollRectClip>();
        new ScrollRectClipEditor().OnCreate(scrollClip, scroll, null);

        var raw = _timeline.CreateTrack<RawImageTrack>(null, "I");
        var rawClip = raw.CreateClip<RawImageClip>();
        new RawImageClipEditor().OnCreate(rawClip, raw, null);

        var shadow = _timeline.CreateTrack<ShadowTrack>(null, "J");
        var shadowClip = shadow.CreateClip<ShadowClip>();
        new ShadowClipEditor().OnCreate(shadowClip, shadow, null);

        var layout = _timeline.CreateTrack<LayoutElementTrack>(null, "K");
        var layoutClip = layout.CreateClip<LayoutElementClip>();
        new LayoutElementClipEditor().OnCreate(layoutClip, layout, null);

        var tmp = _timeline.CreateTrack<TmpTextTrack>(null, "L");
        var tmpClip = tmp.CreateClip<TmpTextClip>();
        new TmpTextClipEditor().OnCreate(tmpClip, tmp, null);

        var group = _timeline.CreateTrack<LayoutGroupTrack>(null, "M");
        var groupClip = group.CreateClip<LayoutGroupClip>();
        new LayoutGroupClipEditor().OnCreate(groupClip, group, null);

        var grid = _timeline.CreateTrack<GridLayoutGroupTrack>(null, "N");
        var gridClip = grid.CreateClip<GridLayoutGroupClip>();
        new GridLayoutGroupClipEditor().OnCreate(gridClip, grid, null);

        Assert.AreEqual("Layout Group", groupClip.displayName);
        Assert.AreEqual("Grid Layout Group", gridClip.displayName);
        Assert.AreEqual("TMP Text", tmpClip.displayName);
        Assert.AreEqual("Layout Element", layoutClip.displayName);
        Assert.AreEqual("Raw Image", rawClip.displayName);
        Assert.AreEqual("Shadow", shadowClip.displayName);
        Assert.AreEqual("Slider", sliderClip.displayName);
        Assert.AreEqual("Scrollbar", barClip.displayName);
        Assert.AreEqual("Scroll Rect", scrollClip.displayName);
    }

    [TestCase(typeof(SliderTrack), true)]
    [TestCase(typeof(ScrollbarTrack), true)]
    [TestCase(typeof(ScrollRectTrack), false)]
    [TestCase(typeof(CanvasGroupTrack), false)]
    public void TrackInspector_ShowsTheSendCallbacksSwitchOnlyForSliderAndScrollbar(System.Type trackType, bool expected)
    {
        var track = _timeline.CreateTrack(trackType, null, "Track");
        var editor = UnityEditor.Editor.CreateEditor(track);

        try
        {
            var root = editor.CreateInspectorGUI();
            var toggles = root.Query<UnityEngine.UIElements.Toggle>().ToList();
            var bound = toggles.Find(t => t.bindingPath == TweenTrackInspector.SEND_CALLBACKS);

            Assert.That(bound != null, Is.EqualTo(expected));
        }
        finally
        {
            Object.DestroyImmediate(editor);
        }
    }

    private static Texture2D GetIcon(TrackAsset track, UnityEditor.Timeline.TrackEditor editor)
        => editor.GetTrackOptions(track, null).icon;

    private RectTransform CreateChildUnder(params System.Type[] parentComponents)
    {
        _parent = new GameObject("Parent", typeof(RectTransform));

        foreach (var type in parentComponents)
        {
            _parent.AddComponent(type);
        }

        _child = new GameObject("Child", typeof(RectTransform));
        _child.transform.SetParent(_parent.transform, false);
        return (RectTransform)_child.transform;
    }
}
