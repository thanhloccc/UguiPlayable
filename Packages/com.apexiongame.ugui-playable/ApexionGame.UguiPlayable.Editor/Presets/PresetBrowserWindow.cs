using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.UIElements;
using ApexionGame.UguiPlayable.Editor.Common;

namespace ApexionGame.UguiPlayable.Editor.Presets
{
    /// <summary>
    /// <c>Window/UGUI Playable/Presets</c>: lists every <see cref="TweenPreset"/> of the project and the installed
    /// packages by category, with search, and shows the selected preset's icon, description, tracks and binding
    /// paths. It adds the preset to the open timeline (<see cref="PresetRecipe"/>), duplicates it into a folder under
    /// <c>Assets/</c> (a preset in an installed package is read-only), pings it, and runs the
    /// <see cref="StaggerRecipe"/> on a container.
    /// </summary>
    /// <remarks>
    /// The window only assembles UI; the rules and the duplication are in <see cref="PresetBrowserModel"/>. Its
    /// state is not serialized: after a domain reload the list is rebuilt and nothing is selected.
    /// </remarks>
    internal sealed class PresetBrowserWindow : EditorWindow
    {
        public const string ROOT_USS_CLASS_NAME = "ugp-preset-browser";
        public const string TOOLBAR_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__toolbar";
        public const string TOOLBAR_ICON_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__toolbar-icon";
        public const string SEARCH_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__search";
        public const string CATEGORY_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__category";
        public const string BODY_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__body";
        public const string LIST_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__list";
        public const string GROUP_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__group";
        public const string ITEM_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__item";
        public const string ITEM_SELECTED_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__item--selected";
        public const string ITEM_ICON_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__item-icon";
        public const string ITEM_NAME_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__item-name";
        public const string EMPTY_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__empty";
        public const string DETAILS_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__details";
        public const string HEADER_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__header";
        public const string HEADER_ICON_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__header-icon";
        public const string TITLE_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__title";
        public const string DESCRIPTION_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__description";
        public const string SECTION_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__section";
        public const string SECTION_TITLE_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__section-title";
        public const string TRACK_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__track";
        public const string TRACK_NAME_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__track-name";
        public const string TRACK_PATH_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__track-path";
        public const string ACTIONS_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__actions";
        public const string ACTION_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__action";
        public const string ACTION_ICON_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__action-icon";
        public const string NOTE_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__note";
        public const string MESSAGE_USS_CLASS_NAME = ROOT_USS_CLASS_NAME + "__message";

        private const string MENU_PATH = "Window/UGUI Playable/Presets";
        private const string TITLE = "Presets";
        private const float MIN_WIDTH = 420f;
        private const float MIN_HEIGHT = 300f;
        private const double REFRESH_MS = 300d;
        private const float DEFAULT_DELAY = 0.05f;
        private const string SEARCH_TOOLTIP = "Search by name, category or description.";
        private const string NO_PRESET = "No preset matches.";
        private const string PICK_PRESET = "Pick a preset in the list.";
        private const string BINDING_ROOT_LABEL = "Binding root";
        private const string BINDING_ROOT_TOOLTIP =
            "The GameObject the preset's paths start from. By default the director's GameObject, or its parent when "
            + "the director sits on a transition-* child.";
        private const string DUPLICATE_AND_ADD_LABEL = "Duplicate and add";
        private const string DUPLICATE_AND_ADD_TOOLTIP =
            "Copy the timeline next to the original, make this director play the copy, then add the preset to it.";
        private const string ADD_LABEL = "Add to timeline";
        private const string DUPLICATE_LABEL = "Duplicate";
        private const string DUPLICATE_TOOLTIP =
            "Copy the preset and its timeline into a folder under Assets, where you can edit them.";
        private const string PING_LABEL = "Ping";
        private const string PING_TOOLTIP = "Select the preset asset in the Project window.";
        private const string STAGGER_TITLE = "Stagger";
        private const string STAGGER_NOTE =
            "Adds this preset once per active child of the container, each starting after the previous one. "
            + "Needs a preset with one track.";
        private const string CONTAINER_LABEL = "Container";
        private const string DELAY_LABEL = "Delay (s)";
        private const string STAGGER_LABEL = "Stagger children";
        private const string TRACKS_TITLE = "Tracks";
        private const string UNBOUND = "(unbound)";
        private const string FOLDER_PANEL_TITLE = "Duplicate preset into";
        private const string OUTSIDE_ASSETS = "Pick a folder inside this project's Assets folder.";
        private const string DONE_WITH_WARNINGS = "Done, with warnings:";

        [NonSerialized] private PresetBrowserModel _model;
        [NonSerialized] private TweenPreset _selected;
        [NonSerialized] private bool _listDirty;

        [NonSerialized] private DropdownField _categoryField;
        [NonSerialized] private ScrollView _list;
        [NonSerialized] private VisualElement _details;
        [NonSerialized] private ObjectField _bindingRoot;
        [NonSerialized] private ObjectField _container;
        [NonSerialized] private FloatField _delay;
        [NonSerialized] private Toggle _duplicateAndAdd;
        [NonSerialized] private HelpBox _sharedWarning;
        [NonSerialized] private Button _addButton;
        [NonSerialized] private Label _addReason;
        [NonSerialized] private Button _staggerButton;
        [NonSerialized] private Label _staggerReason;
        [NonSerialized] private HelpBox _message;
        [NonSerialized] private SharedTimelineUsage _usage;

        [MenuItem(MENU_PATH)]
        public static void Open()
        {
            var window = GetWindow<PresetBrowserWindow>(false, TITLE);
            window.minSize = new Vector2(MIN_WIDTH, MIN_HEIGHT);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.projectChanged += OnProjectChanged;
            Selection.selectionChanged += OnSelectionChanged;
        }

        private void OnDisable()
        {
            EditorApplication.projectChanged -= OnProjectChanged;
            Selection.selectionChanged -= OnSelectionChanged;
        }

        private void CreateGUI()
        {
            titleContent = new GUIContent(TITLE, EditorIcons.Library);

            var root = rootVisualElement;
            root.Clear();
            root.AddToClassList(ROOT_USS_CLASS_NAME);
            PresetBrowserStyleSheetPaths.AddTo(root);

            _model = new PresetBrowserModel(TweenPreset.FindAll());

            root.Add(CreateToolbar());

            var body = new VisualElement();
            body.AddToClassList(BODY_USS_CLASS_NAME);
            root.Add(body);

            _list = new ScrollView(ScrollViewMode.Vertical);
            _list.AddToClassList(LIST_USS_CLASS_NAME);
            body.Add(_list);

            _details = new ScrollView(ScrollViewMode.Vertical);
            _details.AddToClassList(DETAILS_USS_CLASS_NAME);
            body.Add(_details);

            DrawList();
            DrawDetails();
            OnSelectionChanged();

            root.schedule.Execute(RefreshState).Every((long)REFRESH_MS);
        }

        private VisualElement CreateToolbar()
        {
            var toolbar = new VisualElement();
            toolbar.AddToClassList(TOOLBAR_USS_CLASS_NAME);

            toolbar.Add(CreateIcon(EditorIcons.Library, TOOLBAR_ICON_USS_CLASS_NAME));

            var search = new ToolbarSearchField { tooltip = SEARCH_TOOLTIP };
            search.AddToClassList(SEARCH_USS_CLASS_NAME);
            search.RegisterValueChangedCallback(OnSearchChanged);
            toolbar.Add(search);

            _categoryField = new DropdownField(_model.GetCategories(), 0);
            _categoryField.AddToClassList(CATEGORY_USS_CLASS_NAME);
            _categoryField.RegisterValueChangedCallback(OnCategoryChanged);
            toolbar.Add(_categoryField);
            return toolbar;
        }

        private static Image CreateIcon(Texture2D texture, string className)
        {
            var icon = new Image {
                image = texture,
                scaleMode = ScaleMode.ScaleToFit,
                pickingMode = PickingMode.Ignore,
            };

            icon.AddToClassList(className);
            return icon;
        }

        private static Button CreateActionButton(Texture2D icon, string text, string tooltip, Action onClick)
        {
            var button = new Button(onClick) { tooltip = tooltip };
            button.AddToClassList(ACTION_USS_CLASS_NAME);
            button.Add(CreateIcon(icon, ACTION_ICON_USS_CLASS_NAME));
            button.Add(new Label(text));
            return button;
        }

        private static Label CreateNote(string text)
        {
            var note = new Label(text);
            note.AddToClassList(NOTE_USS_CLASS_NAME);
            return note;
        }

        private static VisualElement CreateSection(string title)
        {
            var section = new VisualElement();
            section.AddToClassList(SECTION_USS_CLASS_NAME);

            var label = new Label(title);
            label.AddToClassList(SECTION_TITLE_USS_CLASS_NAME);
            section.Add(label);
            return section;
        }

        private void OnProjectChanged()
        {
            _listDirty = true;
        }

        private void OnSelectionChanged()
        {
            if (_container == null)
            {
                return;
            }

            var selected = Selection.activeGameObject;

            if (selected.IsValid() && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(selected)))
            {
                _container.SetValueWithoutNotify(selected);
                RefreshState();
            }
        }

        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            _model.Query = evt.newValue ?? string.Empty;
            DrawList();
        }

        private void OnCategoryChanged(ChangeEvent<string> evt)
        {
            _model.Category = evt.newValue ?? PresetPicker.ALL_CATEGORIES;
            DrawList();
        }

        private void Rebuild()
        {
            _listDirty = false;

            var query = _model.Query;
            var category = _model.Category;

            _model = new PresetBrowserModel(TweenPreset.FindAll()) { Query = query, Category = category };
            _categoryField.SetValueWithoutNotify(category);
            _categoryField.choices = _model.GetCategories();
            if (_categoryField.choices.Contains(category) == false)
            {
                _model.Category = PresetPicker.ALL_CATEGORIES;
                _categoryField.SetValueWithoutNotify(PresetPicker.ALL_CATEGORIES);
            }

            if (_selected.IsInvalid())
            {
                _selected = null;
            }

            DrawList();
            DrawDetails();
        }

        private void DrawList()
        {
            _list.Clear();

            var groups = _model.GetGroups();

            if (groups.Count < 1)
            {
                var empty = new Label(NO_PRESET);
                empty.AddToClassList(EMPTY_USS_CLASS_NAME);
                _list.Add(empty);
                return;
            }

            for (var i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                var header = new Label(group.Category);
                header.AddToClassList(GROUP_USS_CLASS_NAME);
                _list.Add(header);

                for (var j = 0; j < group.Presets.Count; j++)
                {
                    _list.Add(CreateItem(group.Presets[j]));
                }
            }
        }

        private VisualElement CreateItem(TweenPreset preset)
        {
            var item = new Button(OnClicked) { tooltip = preset.Description };
            item.AddToClassList(ITEM_USS_CLASS_NAME);
            item.EnableInClassList(ITEM_SELECTED_USS_CLASS_NAME, ReferenceEquals(preset, _selected));
            item.Add(CreateIcon(preset.Icon, ITEM_ICON_USS_CLASS_NAME));

            var name = new Label(preset.name);
            name.AddToClassList(ITEM_NAME_USS_CLASS_NAME);
            item.Add(name);
            return item;

            void OnClicked()
            {
                _selected = preset;
                DrawList();
                DrawDetails();
            }
        }

        private void DrawDetails()
        {
            _details.Clear();
            _addButton = null;
            _staggerButton = null;

            if (_selected.IsInvalid())
            {
                var empty = new Label(PICK_PRESET);
                empty.AddToClassList(EMPTY_USS_CLASS_NAME);
                _details.Add(empty);
                return;
            }

            _details.Add(CreateHeader(_selected));

            if (string.IsNullOrEmpty(_selected.Description) == false)
            {
                var description = new Label(_selected.Description);
                description.AddToClassList(DESCRIPTION_USS_CLASS_NAME);
                _details.Add(description);
            }

            AddProblems(_selected);
            _details.Add(CreateTracksSection(_selected));
            _details.Add(CreateAddSection());
            _details.Add(CreateStaggerSection());

            _message = new HelpBox(string.Empty, HelpBoxMessageType.Warning);
            _message.AddToClassList(MESSAGE_USS_CLASS_NAME);
            _message.style.display = DisplayStyle.None;
            _details.Add(_message);

            RefreshState();
        }

        private static VisualElement CreateHeader(TweenPreset preset)
        {
            var header = new VisualElement();
            header.AddToClassList(HEADER_USS_CLASS_NAME);
            header.Add(CreateIcon(preset.Icon, HEADER_ICON_USS_CLASS_NAME));

            var title = new Label($"{preset.name}  ·  {PresetBrowserModel.GetCategory(preset)}");
            title.AddToClassList(TITLE_USS_CLASS_NAME);
            header.Add(title);
            return header;
        }

        private void AddProblems(TweenPreset preset)
        {
            var problems = preset.GetProblems();

            for (var i = 0; i < problems.Count; i++)
            {
                var box = new HelpBox(problems[i].ToMessage(), HelpBoxMessageType.Warning);
                box.AddToClassList(MESSAGE_USS_CLASS_NAME);
                _details.Add(box);
            }
        }

        private static VisualElement CreateTracksSection(TweenPreset preset)
        {
            var section = CreateSection(TRACKS_TITLE);
            var tracks = PresetBrowserModel.GetTracks(preset);

            for (var i = 0; i < tracks.Count; i++)
            {
                var info = tracks[i];
                var row = new VisualElement();
                row.AddToClassList(TRACK_USS_CLASS_NAME);

                var name = new Label($"{info.Name}  ({info.TypeName})");
                name.AddToClassList(TRACK_NAME_USS_CLASS_NAME);
                row.Add(name);

                var path = new Label(info.Path == null ? UNBOUND : info.Path.Length == 0 ? "/" : info.Path);
                path.AddToClassList(TRACK_PATH_USS_CLASS_NAME);
                row.Add(path);
                section.Add(row);
            }

            return section;
        }

        private VisualElement CreateAddSection()
        {
            var section = new VisualElement();
            section.AddToClassList(SECTION_USS_CLASS_NAME);

            _bindingRoot = new ObjectField(BINDING_ROOT_LABEL) {
                objectType = typeof(GameObject),
                allowSceneObjects = true,
                tooltip = BINDING_ROOT_TOOLTIP,
            };

            section.Add(_bindingRoot);

            _sharedWarning = new HelpBox(string.Empty, HelpBoxMessageType.Warning);
            _sharedWarning.AddToClassList(MESSAGE_USS_CLASS_NAME);
            section.Add(_sharedWarning);

            _duplicateAndAdd = new Toggle(DUPLICATE_AND_ADD_LABEL) { value = true, tooltip = DUPLICATE_AND_ADD_TOOLTIP };
            section.Add(_duplicateAndAdd);

            var actions = new VisualElement();
            actions.AddToClassList(ACTIONS_USS_CLASS_NAME);
            section.Add(actions);

            _addButton = CreateActionButton(EditorIcons.WandSparkles, ADD_LABEL, string.Empty, OnAddClicked);
            actions.Add(_addButton);

            actions.Add(CreateActionButton(EditorIcons.Copy, DUPLICATE_LABEL, DUPLICATE_TOOLTIP, OnDuplicateClicked));
            actions.Add(CreateActionButton(EditorIcons.Crosshair, PING_LABEL, PING_TOOLTIP, OnPingClicked));

            _addReason = CreateNote(string.Empty);
            section.Add(_addReason);
            return section;
        }

        private VisualElement CreateStaggerSection()
        {
            var section = CreateSection(STAGGER_TITLE);
            section.Add(CreateNote(STAGGER_NOTE));

            _container = new ObjectField(CONTAINER_LABEL) { objectType = typeof(GameObject), allowSceneObjects = true };
            section.Add(_container);

            _delay = new FloatField(DELAY_LABEL) { value = DEFAULT_DELAY };
            section.Add(_delay);

            var actions = new VisualElement();
            actions.AddToClassList(ACTIONS_USS_CLASS_NAME);
            section.Add(actions);

            _staggerButton = CreateActionButton(EditorIcons.ListOrdered, STAGGER_LABEL, string.Empty, OnStaggerClicked);
            actions.Add(_staggerButton);

            _staggerReason = CreateNote(string.Empty);
            section.Add(_staggerReason);

            var selected = Selection.activeGameObject;

            if (selected.IsValid() && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(selected)))
            {
                _container.SetValueWithoutNotify(selected);
            }

            return section;
        }

        /// <summary>
        /// Updates what depends on the open timeline: whether the buttons are enabled and why not, the binding root
        /// default, and the shared-timeline warning.
        /// </summary>
        private void RefreshState()
        {
            if (_listDirty)
            {
                Rebuild();
                return;
            }

            if (_selected.IsInvalid() || _addButton == null)
            {
                return;
            }

            var inspected = TimelineEditor.inspectedAsset;
            var director = TimelineEditor.inspectedDirector;

            var addReason = PresetBrowserModel.GetAddBlockReason(_selected, inspected);
            _addButton.SetEnabled(addReason.Length == 0);
            _addButton.tooltip = addReason;
            _addReason.text = addReason;
            _addReason.style.display = addReason.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;

            var canBind = inspected.IsValid() && director.IsValid() && director.playableAsset == inspected;
            _bindingRoot.SetEnabled(canBind);

            if (canBind && _bindingRoot.value == null)
            {
                _bindingRoot.SetValueWithoutNotify(PresetRecipe.GetDefaultBindingRoot(director));
            }

            _usage = canBind ? PresetRecipe.CountOtherUsers(inspected, director) : default;
            var shared = _usage.IsShared;
            _sharedWarning.style.display = shared ? DisplayStyle.Flex : DisplayStyle.None;
            _duplicateAndAdd.style.display = shared ? DisplayStyle.Flex : DisplayStyle.None;

            if (shared)
            {
                _sharedWarning.text =
                    $"This timeline is shared by {_usage.Total} other director{(_usage.Total == 1 ? string.Empty : "s")}; "
                    + "they would get unbound tracks.";
            }

            var staggerReason = StaggerRecipe.GetBlockReason(_selected, inspected, _container.value as GameObject);
            _staggerButton.SetEnabled(staggerReason.Length == 0);
            _staggerButton.tooltip = staggerReason;
            _staggerReason.text = staggerReason;
            _staggerReason.style.display = staggerReason.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
        }

        private static double GetPlayhead()
        {
            var window = TimelineEditor.GetWindow();
            return window.IsValid() ? window.playbackControls.GetCurrentTime() : 0d;
        }

        private void OnAddClicked()
        {
            var inspected = TimelineEditor.inspectedAsset;
            var director = TimelineEditor.inspectedDirector;

            if (PresetBrowserModel.GetAddBlockReason(_selected, inspected).Length > 0)
            {
                return;
            }

            var bindingRoot = _bindingRoot.value as GameObject;
            var time = GetPlayhead();
            var duplicate = _usage.IsShared && _duplicateAndAdd.value && director.IsValid();
            var result = duplicate
                ? PresetRecipe.DuplicateAndAdd(_selected, director, bindingRoot, time)
                : PresetRecipe.AddToTimeline(_selected, inspected, director, bindingRoot, time);

            if (duplicate && result.Timeline.IsValid())
            {
                var window = TimelineEditor.GetWindow();

                if (window.IsValid())
                {
                    window.SetTimeline(director);
                }
            }

            ShowResult(result);
        }

        private void OnStaggerClicked()
        {
            var inspected = TimelineEditor.inspectedAsset;
            var container = _container.value as GameObject;

            if (StaggerRecipe.GetBlockReason(_selected, inspected, container).Length > 0)
            {
                return;
            }

            var result = StaggerRecipe.Stagger(
                  _selected
                , inspected
                , TimelineEditor.inspectedDirector
                , container
                , GetPlayhead()
                , _delay.value
            );

            ShowResult(result);
        }

        private void OnDuplicateClicked()
        {
            var folder = EditorUtility.OpenFolderPanel(FOLDER_PANEL_TITLE, "Assets", string.Empty);

            if (string.IsNullOrEmpty(folder))
            {
                return;
            }

            var projectFolder = ToProjectPath(folder);

            if (projectFolder == null)
            {
                ShowMessage(OUTSIDE_ASSETS, HelpBoxMessageType.Warning);
                return;
            }

            var copy = PresetBrowserModel.Duplicate(_selected, projectFolder, out var error);

            if (copy.IsInvalid())
            {
                ShowMessage(error, HelpBoxMessageType.Warning);
                return;
            }

            _selected = copy;
            Rebuild();
            EditorGUIUtility.PingObject(copy);
            ShowMessage($"Copied to {AssetDatabase.GetAssetPath(copy)}.", HelpBoxMessageType.Info);
        }

        private void OnPingClicked()
        {
            if (_selected.IsValid())
            {
                EditorGUIUtility.PingObject(_selected);
            }
        }

        private static string ToProjectPath(string absolute)
        {
            var normalized = absolute.Replace('\\', '/');
            var data = Application.dataPath.Replace('\\', '/');

            if (string.Equals(normalized, data, StringComparison.OrdinalIgnoreCase))
            {
                return "Assets";
            }

            return normalized.StartsWith(data + "/", StringComparison.OrdinalIgnoreCase)
                ? "Assets" + normalized.Substring(data.Length)
                : null;
        }

        private void ShowResult(PresetRecipeResult result)
        {
            var warnings = result.Warnings;

            for (var i = 0; i < warnings.Count; i++)
            {
                Debug.LogWarning($"{_selected.name}: {warnings[i]}", _selected);
            }

            if (warnings.Count < 1)
            {
                _message.style.display = DisplayStyle.None;
                return;
            }

            ShowMessage($"{DONE_WITH_WARNINGS}\n{string.Join("\n", warnings)}", HelpBoxMessageType.Warning);
        }

        private void ShowMessage(string text, HelpBoxMessageType type)
        {
            if (_message == null)
            {
                return;
            }

            _message.messageType = type;
            _message.text = text;
            _message.style.display = DisplayStyle.Flex;
        }
    }
}
