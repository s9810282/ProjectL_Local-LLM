using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview.Scenario;
using UnityEditor;
using UnityEngine;

namespace ProjectL.Tool.Editor
{
    /// <summary>활성 블록 종류의 입력만 그립니다.</summary>
    internal static class PreviewScenarioBlockEditor
    {
        private static readonly Dictionary<PreviewBlockKind, Action<SerializedProperty, TraitPreviewScenarioInput>> Drawers = new()
        {
            {
                PreviewBlockKind.Movement,
                Movement
            },
            {
                PreviewBlockKind.Attack,
                (p, i) => PreviewScenarioFields.Attack(p.FindPropertyRelative("attack"), i)
            },
            {
                PreviewBlockKind.State,
                State
            },
            {
                PreviewBlockKind.Activation,
                (p, i) => PreviewScenarioFields.Trait(p.FindPropertyRelative("trait"), i)
            },
            {
                PreviewBlockKind.ChildAttack,
                Child
            },
            {
                PreviewBlockKind.Interception,
                Interception
            },
            { PreviewBlockKind.FieldPlacement, Placement },
            { PreviewBlockKind.Health, Health },
            { PreviewBlockKind.Return, Return },
        };
        internal static void Draw(SerializedProperty block, PreviewBlockKind kind, TraitPreviewScenarioInput input)
        {
            PreviewScenarioFields.Field(block, "name", "블록 이름");
            if (kind == PreviewBlockKind.State || kind == PreviewBlockKind.Activation)
                PreviewScenarioFields.Field(block, "relation", "적용 관계 (Recorded / AttackHit)");
            Drawers[kind](block, input);
        }

        private static void Movement(SerializedProperty block, TraitPreviewScenarioInput input)
        {
            var movement = block.FindPropertyRelative("movement");
            PreviewScenarioFields.Unit(movement.FindPropertyRelative("unitId"), input, "이동 유닛");
            PreviewScenarioFields.Field(movement, "path", "순서 있는 경유 칸 (시작 칸 제외)");
            PreviewScenarioFields.Field(movement, "direction", "최종 방향");
        }

        private static void State(SerializedProperty block, TraitPreviewScenarioInput input)
        {
            var state = block.FindPropertyRelative("state");
            PreviewScenarioFields.Unit(state.FindPropertyRelative("unitId"), input, "상태 수혜자");
            int[] kinds = { (int)PreviewStateKind.Acceleration, (int)PreviewStateKind.Effect, (int)PreviewStateKind.Cohesion, (int)PreviewStateKind.Regeneration };
            string[] labels = { "가속 보유분", "출처별 효과", "조건부 방어 기여", "재생 진행" };
            var kind = state.FindPropertyRelative("kind");
            kind.intValue = EditorGUILayout.IntPopup("상태 종류", kind.intValue, labels, kinds);
            PreviewScenarioFields.Field(state, "operation", "명시 설정 / 제거·초기화");
            switch ((PreviewStateKind)kind.intValue)
            {
                case PreviewStateKind.Acceleration:
                    PreviewScenarioFields.Field(state, "acceleration", "다음 공격 보유분");
                    break;
                case PreviewStateKind.Effect:
                    PreviewScenarioFields.Effect(state.FindPropertyRelative("effect"), input,
                        state.FindPropertyRelative("operation").intValue == (int)PreviewStateOperation.Remove);
                    break;
                case PreviewStateKind.Cohesion:
                    PreviewScenarioFields.Field(state, "cohesionDefenseBonus", "Core가 확정한 방어 기여");
                    break;
                case PreviewStateKind.Regeneration:
                    foreach (string name in new[] { "healAmount", "healEveryOwnSideTurnEnds", "completedEligibleTurnEnds" })
                        PreviewScenarioFields.Field(state, name);
                    break;
            }
            EditorGUILayout.HelpBox("값과 수명은 직접 입력합니다. 턴 만료/최대 출처/피해/회복은 자동 계산하지 않습니다. 발동 연출에는 같은 묶음의 Activation → State가 필요합니다.", MessageType.None);
        }

        private static void Placement(SerializedProperty block, TraitPreviewScenarioInput input)
        {
            var placement = block.FindPropertyRelative("placement");
            var kind = placement.FindPropertyRelative("eventKind");
            kind.intValue = EditorGUILayout.IntPopup("공개 사건", kind.intValue, new[] { "Moved", "Returned" }, new[]
            {
                (int)ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.CombatEventKind.Moved,
                (int)ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.CombatEventKind.Returned
            });
            bool restore = kind.intValue == (int)ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.CombatEventKind.Returned;
            if (restore) PreviewScenarioFields.Field(placement, "moveCommandNumber", "명시한 복원 이동 번호");
            else PreviewScenarioFields.Field(placement, "movementTrait", "이동 원인 특성");
            var rows = placement.FindPropertyRelative("entries");
            for (int i = 0; i < rows.arraySize; i++)
            {
                var row = rows.GetArrayElementAtIndex(i);
                PreviewScenarioFields.Unit(row.FindPropertyRelative("unitId"), input, "참여자 " + i);
                foreach (string field in new[] { "cell", "rotation", "agency", "actionUse", "distanceMoved" })
                {
                    if (field == "agency" && restore) continue;
                    PreviewScenarioFields.Field(row, field);
                }
            }
            if (rows.arraySize < 2 && EditorGUILayout.LinkButton("참여자 추가")) rows.arraySize++;
            if (rows.arraySize > 0 && EditorGUILayout.LinkButton("마지막 참여자 제거")) rows.arraySize--;
            EditorGUILayout.HelpBox("최종 칸/회전/행동/누적 거리를 명시합니다. Returned 복원은 이 블록만 있는 단독 묶음입니다. 효과/장비/HP는 자동 복원하지 않습니다.", MessageType.Info);
        }

        private static void Health(SerializedProperty block, TraitPreviewScenarioInput input)
        {
            var health = block.FindPropertyRelative("health");
            PreviewScenarioFields.Unit(health.FindPropertyRelative("unitId"), input, "HP 대상");
            PreviewScenarioFields.Field(health, "finalHealth", "명시한 최종 HP");
        }
        private static void Return(SerializedProperty block, TraitPreviewScenarioInput input)
        {
            var returned = block.FindPropertyRelative("returned");
            PreviewScenarioFields.Unit(returned.FindPropertyRelative("unitId"), input, "귀환 유닛");
            PreviewScenarioFields.Field(returned, "area", "목적지 (Hand / EnemyWaiting)");
            PreviewScenarioFields.Field(returned, "reserveSlot", "손패 슬롯");
            EditorGUILayout.HelpBox("상태 제거는 앞선 State 블록에 명시하세요. 귀환 규칙을 자동 적용하지 않습니다.", MessageType.None);
        }

        private static void Child(SerializedProperty block, TraitPreviewScenarioInput input)
        {
            PreviewScenarioFields.Trait(block.FindPropertyRelative("trait"), input);
            PreviewScenarioFields.Attack(block.FindPropertyRelative("attack"), input);
            EditorGUILayout.HelpBox("FollowUpAttack / KillChainAttack 전용. 발동자와 공격자는 같아야 합니다.", MessageType.None);
        }

        private static void Interception(SerializedProperty block, TraitPreviewScenarioInput input)
        {
            PreviewScenarioFields.Trait(block.FindPropertyRelative("trait"), input);
            EditorGUILayout.HelpBox("DamageRedirected 전용. 같은 묶음의 공격 대상 한 명을 보호합니다. 발동자는 수호자이며 피해는 공격 블록에서 한 번만 작성됩니다.", MessageType.None);
        }
    }
}
