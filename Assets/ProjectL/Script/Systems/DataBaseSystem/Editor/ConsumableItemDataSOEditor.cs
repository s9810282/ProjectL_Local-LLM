#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Item.Item;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Item.Authoring;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace ProjectL.Systems.DataBaseSystem.Editor
{
    /// <summary>
    /// 소모 아이템 SO의 효과 Authoring 선택기다. DB SO Inspector는 Systems 안에 두어
    /// 다른 어셈블리에 Systems 내부 타입을 열지 않는다(CombatStageDataEditor와 같은 방식).
    /// </summary>
    [CustomEditor(typeof(ConsumableItemDataSO))]
    internal sealed class ConsumableItemDataSOEditor : UnityEditor.Editor
    {
        private const string NoneLabel = "None";

        // 효과 선택지의 유일한 목록이다. 새 효과 동작을 추가하면 한 줄을 더한다.
        // 지원 여부의 원본은 Core ItemRuleRegistry이며, 이 표는 Inspector 표시만 맡는다.
        private static readonly EffectChoice[] Choices =
        {
            EffectChoice.Of<WhiskeyItemAuthoring>("Whiskey"),
            EffectChoice.Of<RoketItemAuthoring>("Roket"),
            EffectChoice.Of<RopeItemAuthoring>("Rope"),
            EffectChoice.Of<LanternItemAuthoring>("Lantern"),
        };

        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            var effect = serializedObject.FindProperty("effect");
            var labels = new List<string> { NoneLabel };
            foreach (var choice in Choices)
                labels.Add(choice.Label);
            Type currentType = effect.managedReferenceValue?.GetType();
            int selected = Array.FindIndex(Choices, c => c.Type == currentType) + 1;
            var selector = new DropdownField("Effect type", labels, selected);
            selector.RegisterValueChangedCallback(change =>
            {
                serializedObject.Update();
                int index = Array.FindIndex(Choices, c => c.Label == change.newValue);
                effect.managedReferenceValue = index < 0 ? null : Choices[index].Create();
                serializedObject.ApplyModifiedProperties();
            });
            root.Add(selector);
            InspectorElement.FillDefaultInspector(root, serializedObject, this);
            return root;
        }

        private sealed class EffectChoice
        {
            public string Label { get; }
            public Type Type { get; }
            public Func<ItemEffectAuthoring> Create { get; }

            private EffectChoice(string label, Type type, Func<ItemEffectAuthoring> create)
            {
                Label = label;
                Type = type;
                Create = create;
            }

            public static EffectChoice Of<T>(string label) where T : ItemEffectAuthoring, new() =>
                new EffectChoice(label, typeof(T), () => new T());
        }
    }
}
#endif
