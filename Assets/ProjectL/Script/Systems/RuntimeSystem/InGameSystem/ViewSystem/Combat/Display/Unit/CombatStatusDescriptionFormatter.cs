using System;
using System.Collections.Generic;
using System.Text;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Rockfall.Item;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>확정된 출처와 서로 다른 수명을 문구로만 옮깁니다.</summary>
    internal sealed class CombatStatusDescriptionFormatter
    {
        private readonly ITraitVisualTableQuery _traits;
        private readonly RockfallVisualData _rockfall;
        internal CombatStatusDescriptionFormatter(ITraitVisualTableQuery traits, RockfallVisualData rockfall)
        {
            _traits = traits ?? throw new ArgumentNullException(nameof(traits));
            _rockfall = rockfall ?? throw new ArgumentNullException(nameof(rockfall));
        }
        internal string Describe(string summary, StatusEffectSnapshot snapshot)
        {
            var text = new StringBuilder(summary);
            if (snapshot is AttackIncreaseEffectSnapshot attack && attack.HeldForAttackAmount > 0)
                text.Append($"\n다음 공격 보유분: +{attack.HeldForAttackAmount} / 다음 공격 완료까지");
            if (snapshot is DefenseIncreaseEffectSnapshot)
                text.Append("\n현재 적용 조건이 충족되는 동안");
            foreach (var source in snapshot.Sources)
            {
                text.Append("\n").Append(Origin(source.Origin)).Append(": ");
                text.Append(source.Status switch
                {
                    AttackIncreaseStatus value => $"공격력 +{value.Amount}",
                    AttackDecreaseStatus value => $"공격력 {value.ReductionPercent}% 감소",
                    DefenseDecreaseStatus value => $"방어력 {value.ReductionPercent}% 감소",
                    ShieldStatus value => $"잔여 예산 {value.RemainingBudget} / {(value.Eligible ? "적용 가능" : "현재 비활성")} / 마지막 초기화 {value.LastResetControllerTurnCount}",
                    WeaknessExposureStatus value => $"받는 피해 {value.IncreasePercent}% 증가" +
                        (value.Movement.HasValue ? $" / 이동 기록 #{value.Movement.Value.MoveCommandNumber}" : string.Empty),
                    ActionRestrictionStatus value => value.Key == StatusEffectKey.Bind ? "이동 제한" : "행동 제한",
                    TauntedStatus => "도발",
                    // 2026-11-07 폭주·용암포·출혈, 2026-11-10 재구성으로 생긴 상태입니다(값은 Core 상태가 그대로 줌).
                    FireStatus value => $"턴 시작마다 피해 {value.Damage}",
                    BleedingStatus value => $"출혈 {value.Stacks.Count}중첩 / 턴 시작마다 피해 {BleedingDamage(value.Stacks)}",
                    MovementRangeDecreaseStatus value => $"이동력 {value.Magnitude} 감소",
                    ReconstructionStatus => "재구성 중(이동·공격 불가)",
                    _ => throw new InvalidOperationException($"상태 세부 표시가 없습니다: {source.Status.GetType().Name}")
                });
                text.Append(" / ").Append(source.LifetimeKind switch
                {
                    StatusEffectLifetimeKind.ControllerTurnEnds => $"관리 턴 종료 {source.RemainingControllerTurnEnds}회 남음",
                    StatusEffectLifetimeKind.AffectedSideTurnEnds => $"대상 진영 턴 종료 {source.RemainingAffectedSideTurnEnds}회 남음",
                    StatusEffectLifetimeKind.UntilReserveReturnOrDeath => "대상이 예비 영역으로 귀환하거나 사망할 때까지",
                    StatusEffectLifetimeKind.AttackCompletion => "다음 공격 완료까지",
                    StatusEffectLifetimeKind.BattleEnd => "전투 종료까지",
                    StatusEffectLifetimeKind.ReconciledBudget => "예산·적용 조건에 따라 유지",
                    StatusEffectLifetimeKind.CurrentEligibility => "조건이 맞는 동안",
                    StatusEffectLifetimeKind.AffectedSideTurnStarts => $"대상 진영 턴 시작 {source.RemainingAffectedSideStarts}회 남음",
                    StatusEffectLifetimeKind.NextAffectedSideTurnEnd => "대상 진영의 다음 턴 종료까지",
                    _ => throw new InvalidOperationException($"출처 수명 표시가 없습니다: {source.LifetimeKind}")
                });
            }
            return text.ToString();
        }
        /// <summary>
        /// 출혈의 다음 턴 시작 피해입니다. 중첩마다 걸어 준 유닛의 단계 피해를 그대로 가지므로 중첩 피해의 합입니다(Core 출혈 규칙).
        /// HUD 표시 변환(<c>CombatUnitStatusFactory</c>)과 이 설명 문구가 함께 부르는 한 곳입니다.
        /// </summary>
        internal static int BleedingDamage(IReadOnlyList<BleedingStack> stacks)
        {
            if (stacks == null) throw new ArgumentNullException(nameof(stacks));
            int total = 0;
            for (int i = 0; i < stacks.Count; i++)
                total += stacks[i].Damage;
            return total;
        }

        private string Origin(EffectOriginKey? origin)
        {
            if (!origin.HasValue) return "출처 정보 없음";
            var value = origin.Value;
            string name = value.Kind == EffectOwnerKind.Trait && _traits.TryGetTraitVisual((TraitKey)value.DefinitionKey, out var visual)
                ? visual.DisplayName : value.Kind switch
                {
                    EffectOwnerKind.Trait => $"특성 {value.DefinitionKey}",
                    EffectOwnerKind.Category => $"병과 {value.DefinitionKey}",
                    EffectOwnerKind.Artifact => $"유물 {value.DefinitionKey}",
                    EffectOwnerKind.Item => $"아이템 {value.DefinitionKey}",
                    EffectOwnerKind.StageGimmick when value.DefinitionKey == (int)StageGimmickKey.Rockfall => _rockfall.DisplayName,
                    EffectOwnerKind.StageGimmick => $"기믹 {value.DefinitionKey}",
                    _ => throw new InvalidOperationException($"표시할 수 없는 출처 종류: {value.Kind}")
                };
            return $"{name} · 출처 #{value.OwnerInstanceId}";
        }
    }
}
