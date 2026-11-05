using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace ApexionGame.UguiPlayable.Editor.Presets
{
    /// <summary>
    /// A small window that lists the <see cref="TweenPreset"/> assets, with a search field and a category filter, and
    /// either adds the picked preset to a timeline or applies it to clips.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It is a utility window rather than a drop-down, so the binding root object field and the category menu can
    /// open their own windows without closing it. It closes after an action without warnings; otherwise it lists the
    /// warnings (they also go to the Console) until the user closes it.
    /// </para>
    /// <para>
    /// The add mode shows the binding root (see <see cref="PresetRecipe.GetDefaultBindingRoot"/>) and, when other
    /// directors use the timeline, a warning and a "Duplicate and add" switch, on by default. The apply mode lists
    /// only the presets with a clip of the selected type; a preset with several such clips lists each of them.
    /// </para>
    /// <para>
    /// The state is not serialized: after a domain reload the window closes itself.
    /// </para>
    /// </remarks>
    internal sealed class PresetPicker : EditorWindow
    {
        public const string ROOT_USS_CLASS_NAME = "ugp-preset-picker";
        public const string TITLE_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__title";
        public const string NOTE_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__note";
        public const string FILTERS_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__filters";
        public const string SEARCH_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__search";
        public const string CATEGORY_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__category";
        public const string LIST_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__list";
        public const string ITEM_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__item";
        public const string ITEM_ICON_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__item-icon";
        public const string ITEM_NAME_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__item-name";
        public const string ITEM_NOTE_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__item-note";
        public const string EMPTY_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__empty";
        public const string WARNING_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__warning";
        public const string CLOSE_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__close";

        private const float WIDTH = 340f;
        private const float HEIGHT = 420f;
        private const float MIN_HEIGHT = 200f;
        private const string ALL_CATEGORIES = "All";
        private const string ADD_TITLE = "Add Preset";
        private const string APPLY_TITLE = "Apply Preset";
        private const string BINDING_ROOT_LABEL = "Binding root";
        private const string BINDING_ROOT_TOOLTIP =
            "The GameObject the preset's paths start from. By default the director's GameObject, or its parent when "
            + "the director sits on a transition-* child.";
        private const string DUPLICATE_LABEL = "Duplicate and add";
        private const string DUPLICATE_TOOLTIP =
            "Copy the timeline next to the original, make this director play the copy, then add the preset to it.";
        private const string UNBOUND_NOTE =
            "No director plays this timeline (asset mode or a sub-timeline): the tracks are added unbound.";
        private const string NO_PRESET = "No preset matches.";
        private const string RESULT_TITLE = "Done, with warnings:";
        private const string CLOSE = "Close";
        private const string NOTE_SEPARATOR = " · ";

        [NonSerialized] private bool _isApply;
        [NonSerialized] private TimelineAsset _timeline;
        [NonSerialized] private PlayableDirector _director;
        [NonSerialized] private double _time;
        [NonSerialized] private PlayableAsset[] _clipAssets;
        [NonSerialized] private List<Entry> _entries;
        [NonSerialized] private SharedTimelineUsage _usage;

        [NonSerialized] private ScrollView _list;
        [NonSerialized] private ObjectField _bindingRoot;
        [NonSerialized] private Toggle _duplicate;
        [NonSerialized] private string _query = string.Empty;
        [NonSerialized] private string _category = ALL_CATEGORIES;

        /// <summary>
        /// Opens the picker to add a preset to <paramref name="timeline"/> at <paramref name="time"/>.
        /// </summary>
        /// <param name="director">The director playing the timeline, or null in asset mode.</param>
        /// <param name="anchor">A screen rectangle the window opens below.</param>
        public static void ShowAdd(TimelineAsset timeline, PlayableDirector director, double time, Rect anchor)
        {
            var picker = Create(ADD_TITLE);
            picker._timeline = timeline;
            picker._director = director;
            picker._time = time;
            picker.ShowAt(anchor);
        }

        /// <summary>
        /// Opens the picker to apply a preset to <paramref name="clipAssets"/>, listing only the presets that have a
        /// clip of their type.
        /// </summary>
        public static void ShowApply(PlayableAsset[] clipAssets, Rect anchor)
        {
            var picker = Create(APPLY_TITLE);
            picker._isApply = true;
            picker._clipAssets = clipAssets;
            picker.ShowAt(anchor);
        }

        /// <summary>
        /// A screen rectangle under the top centre of <paramref name="window"/>, or of the main Editor window when
        /// it is null.
        /// </summary>
        public static Rect GetAnchor(EditorWindow window)
        {
            var area = window.IsValid() ? window.position : EditorGUIUtility.GetMainWindowPosition();
            return new Rect(area.center.x - WIDTH * 0.5f, area.y + 40f, WIDTH, 0f);
        }

        /// <summary>
        /// A screen rectangle over <paramref name="element"/> of the window that is focused, for a picker opened by
        /// a button.
        /// </summary>
        public static Rect GetAnchor(VisualElement element)
        {
            var window = EditorWindow.focusedWindow;

            if (window.IsInvalid())
            {
                return GetAnchor((EditorWindow)null);
            }

            var bound = element.worldBound;
            var origin = window.position.position;
            return new Rect(origin.x + bound.xMax - WIDTH, origin.y + bound.yMax, WIDTH, 0f);
        }

        /// <summary>
        /// The presets the picker lists for <paramref name="clipAssets"/>: with one clip type, each preset clip of
        /// that type (a preset with several lists each); with several types, each preset that has any of them.
        /// </summary>
        public static List<Entry> GetApplyEntries(IReadOnlyList<TweenPreset> presets, PlayableAsset[] clipAssets)
        {
            var types = new HashSet<Type>();

            for (var i = 0; i < clipAssets.Length; i++)
            {
                if (clipAssets[i].IsValid())
                {
                    types.Add(clipAssets[i].GetType());
                }
            }

            var entries = new List<Entry>();
            var count = presets.Count;

            for (var i = 0; i < count; i++)
            {
                AddApplyEntries(presets[i], types, entries);
            }

            return entries;
        }

        /// <summary>
        /// Closes an open picker and creates a new one, not shown yet, so its state is set before
        /// <see cref="CreateGUI"/> can run.
        /// </summary>
        private static PresetPicker Create(string title)
        {
            if (HasOpenInstances<PresetPicker>())
            {
                GetWindow<PresetPicker>().Close();
            }

            var picker = CreateInstance<PresetPicker>();
            picker.titleContent = new GUIContent(title);
            picker.minSize = new Vector2(WIDTH, MIN_HEIGHT);
            return picker;
        }

        private static void AddApplyEntries(TweenPreset preset, HashSet<Type> types, List<Entry> entries)
        {
            if (types.Count == 1)
            {
                foreach (var type in types)
                {
                    var clips = PresetRecipe.FindClips(preset, type);
                    var clipCount = clips.Count;

                    for (var i = 0; i < clipCount; i++)
                    {
                        var clip = clips[i];
                        var note = clipCount > 1 ? preset.Category + NOTE_SEPARATOR + clip.Track.name : preset.Category;
                        entries.Add(new(preset, clip.Asset, note));
                    }
                }

                return;
            }

            foreach (var type in types)
            {
                if (PresetRecipe.FindClips(preset, type).Count > 0)
                {
                    entries.Add(new(preset, null, preset.Category));
                    return;
                }
            }
        }

        private static List<Entry> GetAddEntries(List<TweenPreset> presets)
        {
            var entries = new List<Entry>(presets.Count);
            var count = presets.Count;

            for (var i = 0; i < count; i++)
            {
                var preset = presets[i];

                if (preset.Timeline.IsValid())
                {
                    entries.Add(new(preset, null, preset.Category));
                }
            }

            return entries;
        }

        private static Label CreateNote(string text)
        {
            var note = new Label(text);
            note.AddToClassList(NOTE_USS_CLASS_NAME);
            return note;
        }

        private static string FormatSharedWarning(int count)
            => $"This timeline is shared by {count} other director{(count == 1 ? string.Empty : "s")}; "
                + "they would get unbound tracks.";

        private static bool Matches(TweenPreset preset, string query, string category)
        {
            var inCategory = category == ALL_CATEGORIES
                || string.Equals(preset.Category, category, StringComparison.Ordinal);

            if (inCategory == false)
            {
                return false;
            }

            return string.IsNullOrEmpty(query)
                || preset.name.Contains(query, StringComparison.OrdinalIgnoreCase)
                || preset.Category.Contains(query, StringComparison.OrdinalIgnoreCase)
                || preset.Description.Contains(query, StringComparison.OrdinalIgnoreCase);
        }

        private void CreateGUI()
        {
            // After a domain reload the non-serialized state is gone: close instead of showing an empty picker.
            if (_entries == null && _timeline.IsInvalid() && _clipAssets == null)
            {
                EditorApplication.delayCall += Close;
            }
        }

        private void ShowAt(Rect anchor)
        {
            ShowUtility();
            position = new Rect(anchor.x, anchor.yMax, WIDTH, HEIGHT);
            Build();
        }

        private void Build()
        {
            var root = rootVisualElement;
            root.Clear();
            root.AddToClassList(ROOT_USS_CLASS_NAME);
            PresetPickerStyleSheetPaths.AddTo(root);

            var presets = TweenPreset.FindAll();
            _entries = _isApply ? GetApplyEntries(presets, _clipAssets) : GetAddEntries(presets);

            root.Add(CreateTitle());

            if (_isApply == false)
            {
                AddBindingRows(root);
            }

            root.Add(CreateFilters());

            _list = new ScrollView(ScrollViewMode.Vertical);
            _list.AddToClassList(LIST_USS_CLASS_NAME);
            root.Add(_list);

            DrawList();
        }

        private Label CreateTitle()
        {
            var text = _isApply
                ? $"Apply a preset to {_clipAssets.Length} clip{(_clipAssets.Length == 1 ? string.Empty : "s")}"
                : $"Add a preset at {_time:0.###} s to '{_timeline.name}'";

            var title = new Label(text);
            title.AddToClassList(TITLE_USS_CLASS_NAME);
            return title;
        }

        private void AddBindingRows(VisualElement root)
        {
            var canBind = _director.IsValid() && _director.playableAsset == _timeline;

            if (canBind == false)
            {
                root.Add(CreateNote(UNBOUND_NOTE));
                return;
            }

            _bindingRoot = new ObjectField(BINDING_ROOT_LABEL) {
                objectType = typeof(GameObject),
                allowSceneObjects = true,
                tooltip = BINDING_ROOT_TOOLTIP,
            };

            _bindingRoot.SetValueWithoutNotify(PresetRecipe.GetDefaultBindingRoot(_director));
            root.Add(_bindingRoot);

            _usage = PresetRecipe.CountOtherUsers(_timeline, _director);

            if (_usage.IsShared == false)
            {
                return;
            }

            var warning = new HelpBox(FormatSharedWarning(_usage.Total), HelpBoxMessageType.Warning);
            warning.AddToClassList(WARNING_USS_CLASS_NAME);
            root.Add(warning);

            _duplicate = new Toggle(DUPLICATE_LABEL) { value = true, tooltip = DUPLICATE_TOOLTIP };
            root.Add(_duplicate);
        }

        private VisualElement CreateFilters()
        {
            var filters = new VisualElement();
            filters.AddToClassList(FILTERS_USS_CLASS_NAME);

            var search = new ToolbarSearchField();
            search.AddToClassList(SEARCH_USS_CLASS_NAME);
            search.RegisterValueChangedCallback(OnSearchChanged);
            filters.Add(search);

            var category = new DropdownField(GetCategories(), 0);
            category.AddToClassList(CATEGORY_USS_CLASS_NAME);
            category.RegisterValueChangedCallback(OnCategoryChanged);
            filters.Add(category);

            search.schedule.Execute(search.Focus);
            return filters;
        }

        private List<string> GetCategories()
        {
            var categories = new List<string> { ALL_CATEGORIES };
            var count = _entries.Count;

            for (var i = 0; i < count; i++)
            {
                var category = _entries[i].Preset.Category;

                if (string.IsNullOrEmpty(category) == false && categories.Contains(category) == false)
                {
                    categories.Add(category);
                }
            }

            return categories;
        }

        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            _query = evt.newValue ?? string.Empty;
            DrawList();
        }

        private void OnCategoryChanged(ChangeEvent<string> evt)
        {
            _category = evt.newValue ?? ALL_CATEGORIES;
            DrawList();
        }

        private void DrawList()
        {
            _list.Clear();

            var shown = 0;
            var count = _entries.Count;

            for (var i = 0; i < count; i++)
            {
                var entry = _entries[i];

                if (Matches(entry.Preset, _query, _category) == false)
                {
                    continue;
                }

                _list.Add(CreateItem(entry));
                shown++;
            }

            if (shown < 1)
            {
                var empty = new Label(NO_PRESET);
                empty.AddToClassList(EMPTY_USS_CLASS_NAME);
                _list.Add(empty);
            }
        }

        private VisualElement CreateItem(Entry entry)
        {
            var item = new Button(OnClicked) { tooltip = entry.Preset.Description };
            item.AddToClassList(ITEM_USS_CLASS_NAME);

            var icon = new Image {
                image = entry.Preset.Icon,
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore,
            };

            icon.AddToClassList(ITEM_ICON_USS_CLASS_NAME);
            item.Add(icon);

            var name = new Label(entry.Preset.name);
            name.AddToClassList(ITEM_NAME_USS_CLASS_NAME);
            item.Add(name);

            var note = new Label(entry.Note);
            note.AddToClassList(ITEM_NOTE_USS_CLASS_NAME);
            item.Add(note);
            return item;

            void OnClicked()
            {
                Run(entry);
            }
        }

        private void Run(Entry entry)
        {
            var result = _isApply ? Apply(entry) : Add(entry);

            if (result == null)
            {
                Close();
                return;
            }

            var warnings = result.Warnings;
            var count = warnings.Count;

            for (var i = 0; i < count; i++)
            {
                Debug.LogWarning($"{entry.Preset.name}: {warnings[i]}", entry.Preset);
            }

            if (count < 1)
            {
                Close();
                return;
            }

            ShowWarnings(warnings);
        }

        private PresetRecipeResult Add(Entry entry)
        {
            if (_timeline.IsInvalid())
            {
                return null;
            }

            var bindingRoot = _bindingRoot?.value as GameObject;
            var duplicate = _duplicate != null && _duplicate.value && _director.IsValid();
            var time = _time;
            var result = duplicate
                ? PresetRecipe.DuplicateAndAdd(entry.Preset, _director, bindingRoot, time)
                : PresetRecipe.AddToTimeline(entry.Preset, _timeline, _director, bindingRoot, time);

            if (duplicate && result.Timeline.IsValid())
            {
                var window = TimelineEditor.GetWindow();

                if (window.IsValid())
                {
                    window.SetTimeline(_director);
                }
            }

            return result;
        }

        private PresetRecipeResult Apply(Entry entry)
        {
            var assets = new List<PlayableAsset>(_clipAssets.Length);

            for (var i = 0; i < _clipAssets.Length; i++)
            {
                if (_clipAssets[i].IsValid())
                {
                    assets.Add(_clipAssets[i]);
                }
            }

            return assets.Count > 0 ? PresetRecipe.ApplyToClips(entry.Preset, assets, entry.Source) : null;
        }

        private void ShowWarnings(List<string> warnings)
        {
            var root = rootVisualElement;
            root.Clear();

            var title = new Label(RESULT_TITLE);
            title.AddToClassList(TITLE_USS_CLASS_NAME);
            root.Add(title);

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            scroll.AddToClassList(LIST_USS_CLASS_NAME);
            root.Add(scroll);

            var count = warnings.Count;

            for (var i = 0; i < count; i++)
            {
                var warning = new HelpBox(warnings[i], HelpBoxMessageType.Warning);
                warning.AddToClassList(WARNING_USS_CLASS_NAME);
                scroll.Add(warning);
            }

            var close = new Button(Close) { text = CLOSE };
            close.AddToClassList(CLOSE_USS_CLASS_NAME);
            root.Add(close);
        }

        /// <summary>
        /// One row of the picker: the preset, the preset clip to copy from (apply mode, or null for the first of each
        /// type), and the note shown after the name.
        /// </summary>
        internal readonly record struct Entry(TweenPreset Preset, PlayableAsset Source, string Note);
    }
}
