using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using ApexionGame.UguiPlayable.Editor.Common;
using ApexionGame.UguiPlayable.Tracks.Text;

namespace ApexionGame.UguiPlayable.Editor.Clips
{
    /// <summary>
    /// The Timing card at the top of the TMP Characters clip inspector: the character range, the stagger, the order
    /// (with its seed for the random order), the per-character ease and the pivot.
    /// </summary>
    internal sealed class CharacterTimingSection
    {
        // TmpCharacterBehaviour keeps these fields private, so nameof is not available to editor code. Their names are
        // serialized user data and cannot change without [FormerlySerializedAs].
        public const string RANGE_START = "_rangeStart";
        public const string RANGE_LENGTH = "_rangeLength";
        public const string STAGGER = "_stagger";
        public const string ORDER = "_order";
        public const string ORDER_SEED = "_orderSeed";
        public const string EASE = "_ease";
        public const string PIVOT = "_pivot";

        private const string TITLE = "Timing";
        private const string START = "Start";
        private const string LENGTH = "Length";
        private const string STAGGER_LABEL = "Stagger";
        private const string ORDER_LABEL = "Order";
        private const string PIVOT_LABEL = "Pivot";
        private const string START_TOOLTIP = "The first animated character (rich-text tags are not counted).";
        private const string LENGTH_TOOLTIP = "The number of animated characters. 0 animates to the end of the text.";
        private const string STAGGER_TOOLTIP =
            "The share of the clip over which the characters' starts are spread. 0 animates them together.";
        private const string ORDER_TOOLTIP = "The order in which the characters start. Hidden characters are skipped.";
        private const string SEED_TOOLTIP = "The same seed always gives the same order.";
        private const string PIVOT_TOOLTIP = "The point each character rotates and scales about.";

        private static readonly string[] s_pivotLabels = { "Center", "Baseline" };

        private readonly SerializedObject _serializedObject;
        private readonly SerializedProperty _order;
        private readonly SerializedProperty _seed;

        private readonly VisualElement _card = new();
        private readonly VisualElement _seedRow;

        private CharacterTimingSection(SerializedObject serializedObject)
        {
            _serializedObject = serializedObject;

            var template = serializedObject.FindProperty(TweenClipModel.TEMPLATE);

            _order = template.FindPropertyRelative(ORDER);
            _seed = template.FindPropertyRelative(ORDER_SEED);

            _card.AddToClassList(TweenClipSections.CARD_USS_CLASS_NAME);
            _card.Add(CreateHeader());

            var body = new VisualElement();
            body.AddToClassList(TweenClipSections.CARD_BODY_USS_CLASS_NAME);
            _card.Add(body);

            body.Add(CreateCountRow(START, START_TOOLTIP, template.FindPropertyRelative(RANGE_START)));
            body.Add(CreateCountRow(LENGTH, LENGTH_TOOLTIP, template.FindPropertyRelative(RANGE_LENGTH)));

            var stagger = new Slider(0f, TmpCharacterBehaviour.MAX_STAGGER) {
                showInputField = true,
                tooltip = STAGGER_TOOLTIP,
            };

            stagger.AddToClassList(TweenClipSections.FIELD_USS_CLASS_NAME);
            stagger.BindProperty(template.FindPropertyRelative(STAGGER));
            body.Add(CreateRow(STAGGER_LABEL, stagger));

            var order = new EnumField(CharacterOrder.Forward) { tooltip = ORDER_TOOLTIP };
            order.AddToClassList(TweenClipSections.FIELD_USS_CLASS_NAME);
            order.BindProperty(_order);
            body.Add(CreateRow(ORDER_LABEL, order));

            _seedRow = CreateSeedRow();
            body.Add(_seedRow);

            body.Add(new PropertyField(template.FindPropertyRelative(EASE)));

            var pivot = SegmentedControl.Create(s_pivotLabels)
                .BindEnum(template.FindPropertyRelative(PIVOT), s_pivotLabels.Length);

            pivot.tooltip = PIVOT_TOOLTIP;
            body.Add(CreateRow(PIVOT_LABEL, pivot));

            _card.TrackPropertyValue(_order, _ => RefreshState());
            MultiEdit.PollWhileMultiple(_card, serializedObject, RefreshState);

            RefreshState();
        }

        public static VisualElement Create(SerializedObject serializedObject)
            => new CharacterTimingSection(serializedObject)._card;

        private static VisualElement CreateHeader()
        {
            var header = new VisualElement();
            header.AddToClassList(TweenClipSections.CARD_HEADER_USS_CLASS_NAME);

            var name = new Label(TITLE);
            name.AddToClassList(TweenClipSections.CARD_NAME_USS_CLASS_NAME);
            header.Add(name);
            return header;
        }

        private static VisualElement CreateRow(string label, VisualElement content)
        {
            var row = TweenClipSections.CreateRow(label);
            row.Add(content);
            return row;
        }

        private VisualElement CreateCountRow(string label, string tooltip, SerializedProperty value)
        {
            var field = new IntegerField { tooltip = tooltip };
            field.AddToClassList(TweenClipSections.FIELD_USS_CLASS_NAME);
            field.BindProperty(value);
            field.RegisterCallback<BlurEvent>(_ => ClampToZero(value));
            return CreateRow(label, field);
        }

        private VisualElement CreateSeedRow()
        {
            var seedField = new IntegerField { tooltip = SEED_TOOLTIP };
            seedField.AddToClassList(TweenClipSections.FIELD_USS_CLASS_NAME);
            seedField.BindProperty(_seed);

            var row = CreateRow(TweenClipTexts.SEED, seedField);

            var dice = TweenClipSections.CreateIconButton(
                  EditorIcons.Dices
                , TweenClipTexts.NEW_SEED_TOOLTIP
                , OnNewSeedClicked
                , out _
            );

            row.Add(dice);
            return row;
        }

        // Each selected clip gets its own new seed, in one Undo step.
        private void OnNewSeedClicked()
        {
            var path = _seed.propertyPath;

            MultiEdit.WriteEach(_serializedObject, (each, _) => {
                each.FindProperty(path).intValue = UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            });
        }

        // A negative index or count has no meaning; the field accepts any int, so the value is clamped on blur.
        private void ClampToZero(SerializedProperty value)
            => MultiEdit.ClampToZero(_serializedObject, value);

        private void RefreshState()
        {
            _serializedObject.UpdateIfRequiredOrScript();

            // The seed shows when any selected clip uses the random order.
            var isRandom = _order.hasMultipleDifferentValues || _order.enumValueIndex == (int)CharacterOrder.Random;

            _seedRow.style.display = isRandom ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
