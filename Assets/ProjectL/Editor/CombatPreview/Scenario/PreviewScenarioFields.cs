using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview.Scenario;
using UnityEditor;
using UnityEngine;
using CoreKind = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.TraitActivationKind;

namespace ProjectL.Tool.Editor
{
    /// <summary>유닛 참조와 상태/발동의 공통 입력 필드입니다.</summary>
    internal static class PreviewScenarioFields
    {
        internal static void Field(SerializedProperty property, string name, string label = null)
        {
            var field = property.FindPropertyRelative(name);
            if (label == null)
                EditorGUILayout.PropertyField(field, true);
            else
                EditorGUILayout.PropertyField(field, new GUIContent(label), true);
        }

        internal static void Unit(SerializedProperty field, TraitPreviewScenarioInput input, string label)
        {
            var ids = new[]
            {
                ""
            }.Concat(input.units.Select(u => u.id)).ToArray();
            var labels = new[]
            {
                "선택하세요"
            }.Concat(input.units.Select(u => u.name + " (" + u.unit.key + ")")).ToArray();
            int index = Array.IndexOf(ids, field.stringValue);
            if (index < 0)
            {
                EditorGUILayout.HelpBox($"{label}: 삭제되거나 없는 유닛 참조 {field.stringValue}", MessageType.Error);
                index = 0;
            }

            int next = EditorGUILayout.Popup(label, index, labels);
            if (next != index)
                field.stringValue = ids[next];
        }

        internal static void UnitList(SerializedProperty list, TraitPreviewScenarioInput input, string label)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            int remove = -1;
            for (int i = 0; i < list.arraySize; i++)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    Unit(list.GetArrayElementAtIndex(i), input, $"대상 {i}");
                    if (GUILayout.Button("−", GUILayout.Width(26)))
                        remove = i;
                }
            }

            if (remove >= 0)
                list.DeleteArrayElementAtIndex(remove);
            if (GUILayout.Button("대상 추가"))
            {
                int i = list.arraySize++;
                list.GetArrayElementAtIndex(i).stringValue = "";
            }
        }

        internal static void Effect(SerializedProperty effect, TraitPreviewScenarioInput input, bool remove = false)
        {
            Field(effect, "id", "Source ID (갱신/제거 때 재사용)");
            Field(effect, "key", "효과 키");
            if (remove) return;
            Field(effect, "ownerKind", "출처 분류");
            Unit(effect.FindPropertyRelative("sourceId"), input, "출처 유닛 (선택)");
            Field(effect, "ownerInstanceId", "출처 유닛 미지정 시 Owner ID");
            if (effect.FindPropertyRelative("ownerKind").intValue == (int)EffectOwnerKind.Trait) Field(effect, "trait", "출처 특성");
            else Field(effect, "definitionKey", "출처 정의 키");
            var key = (StatusEffectKey)effect.FindPropertyRelative("key").intValue;
            if (key == StatusEffectKey.AttackIncrease || key == StatusEffectKey.Shield) Field(effect, "amount", key == StatusEffectKey.Shield ? "잔여 보호막 예산" : "공격력 부여량");
            if (key == StatusEffectKey.AttackDecrease || key == StatusEffectKey.DefenseDecrease || key == StatusEffectKey.WeaknessExposure) Field(effect, "percent", "출처 증가/감소율 (%)");
            if (key == StatusEffectKey.WeaknessExposure)
            {
                Field(effect, "lifetime", "노출 수명");
                if (effect.FindPropertyRelative("lifetime").intValue == (int)WeaknessExposureLifetimeKind.AffectedSideTurnEnds)
                    Field(effect, "remainingTurns", "대상 진영 턴 종료 잔여 횟수");
                Field(effect, "hasMovement", "이동 기여분 식별 사용");
                if (effect.FindPropertyRelative("hasMovement").boolValue)
                    foreach (string name in new[] { "battleInstanceId", "controllerTurnSequence", "moveCommandNumber" }) Field(effect, name);
            }
            else if (key == StatusEffectKey.AttackIncrease || key == StatusEffectKey.Disarm || key == StatusEffectKey.Bind)
                Field(effect, "remainingTurns", "남은 턴 종료 횟수");
            if (key == StatusEffectKey.Shield)
            {
                Field(effect, "eligible", "현재 적용 가능");
                Field(effect, "lastResetControllerTurnCount", "마지막 초기화 관리 턴 번호 (잔여 수명 아님)");
            }
            if (key == StatusEffectKey.DefenseIncrease) EditorGUILayout.HelpBox("방어 증가는 상태 종류에서 조건부 방어 기여를 사용하세요.", MessageType.Info);
        }

        internal static void Trait(SerializedProperty trait, TraitPreviewScenarioInput input)
        {
            Field(trait, "asset", "TraitVisual SO");
            var asset = trait.FindPropertyRelative("asset").objectReferenceValue;
            var kinds = TraitActivationPreviewBridge.Kinds(asset);
            string available = kinds.Length == 0 ? "작성된 줄 없음 (합성 발동 가능)" : string.Join(", ", kinds.Select(k => ((CoreKind)k).ToString()));
            EditorGUILayout.LabelField("작성된 줄", available, EditorStyles.wordWrappedLabel);
            Field(trait, "kind", "Activation Kind");
            EditorGUILayout.HelpBox(TraitActivationPreviewBridge.FeedbackInfo(asset, trait.FindPropertyRelative("kind").intValue), MessageType.Info);
            Unit(trait.FindPropertyRelative("sourceId"), input, "발동자 / 수호자");
            UnitList(trait.FindPropertyRelative("targetIds"), input, "발동 대상 / 보호 대상");
            Field(trait, "cells", "영향 칸");
            Field(trait, "magnitude", "발동 수치");
        }

        internal static void Attack(SerializedProperty attack, TraitPreviewScenarioInput input)
        {
            Unit(attack.FindPropertyRelative("attackerId"), input, "공격자");
            Field(attack, "direction", "공격 방향");
            Field(attack, "targetCell", "지정 칸 (첫 원래 대상)");
            EditorGUILayout.HelpBox("최종 HP를 입력합니다. 가로채기가 있으면 실제 수신자인 수호자의 HP입니다. 피해는 공격력에서 자동 계산하지 않습니다.", MessageType.None);
            var hits = attack.FindPropertyRelative("hits");
            int remove = -1;
            for (int i = 0; i < hits.arraySize; i++)
            {
                var hit = hits.GetArrayElementAtIndex(i);
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    Unit(hit.FindPropertyRelative("targetId"), input, $"원래 대상 {i}");
                    Field(hit, "finalHealth", "실제 수신자 최종 HP");
                    if (GUILayout.Button("이 대상 제거"))
                        remove = i;
                }
            }

            if (remove >= 0)
                hits.DeleteArrayElementAtIndex(remove);
            if (GUILayout.Button("피격 대상 추가"))
            {
                int i = hits.arraySize++;
                var hit = hits.GetArrayElementAtIndex(i);
                hit.FindPropertyRelative("targetId").stringValue = "";
                hit.FindPropertyRelative("finalHealth").intValue = 90;
            }
        }
    }
}
