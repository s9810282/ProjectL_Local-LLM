using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    /// <summary>
    /// 적 전용 재구성 특성의 판단·결과 원본입니다. 피해로 HP가 0이 되는 순간(즉사·낙사·기차 제외) 전투당 한 번 죽지 않고
    /// 체력을 최대치로 되돌리며 재구성 중 상태가 됩니다. Lv2부터는 최대 체력의 50% 보호막을 함께 받습니다.
    /// 공통 피해 경계와 낙석 충돌이 이 판단 하나를 부릅니다.
    /// </summary>
    internal sealed class ReconstructionTraitRule : ICombatTraitRule
    {
        /// <summary>재구성 순간의 남은 자기 턴 시작 수입니다. 발동 뒤 두 번째 자기 턴 시작에 부활합니다(설계 P3).</summary>
        internal const int InitialOwnTurnStarts = 2;

        public TraitKey Key => TraitKey.Reconstruction;

        internal static ReconstructionTraitStatusSnapshot Status(CharacterCombatFacts character) =>
            character.Stats.TraitStatuses.ToArray().OfType<ReconstructionTraitStatusSnapshot>().SingleOrDefault();

        /// <summary>재구성을 가졌고, 이번 전투에서 아직 쓰지 않았으며, 필드에 있는 캐릭터입니다.</summary>
        internal static bool CanTrigger(CharacterCombatFacts character)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            var status = Status(character);
            return status != null && !status.Used && string.IsNullOrEmpty(status.UnavailableReason) &&
                character.Area == CharacterRuntimeArea.Field;
        }

        /// <summary>
        /// 앞 사실에서 뒤 사실 사이에 재구성이 일어났는지입니다(재구성 연출 설계 RE3, 공격 미리보기 칸). 앞에서는 재구성할 수 있었고,
        /// 뒤에서는 살아 있으며 재구성을 사용함으로 바뀌었습니다. 판정 원본은 이 규칙 하나이고 공격 장부는 부르기만 합니다.
        /// </summary>
        internal static bool Triggered(CharacterCombatFacts before, CharacterCombatFacts after)
        {
            if (before == null) throw new ArgumentNullException(nameof(before));
            if (after == null) throw new ArgumentNullException(nameof(after));
            return CanTrigger(before) && !after.Health.IsDead && Status(after)?.Used == true;
        }

        internal static ReconstructionStatus Current(CharacterCombatFacts character) =>
            character.StatusEffects.Entries.OfType<ReconstructionStatus>().SingleOrDefault();

        /// <summary>재구성 중이면 이동·공격할 수 없습니다. 행동 입장 함수가 이 판단을 읽습니다.</summary>
        internal static bool IsReconstructing(CharacterCombatFacts character) => Current(character) != null;

        /// <summary>
        /// 치명 피해 직전 사실에서 재구성 결과를 만듭니다. 체력 최대, 재구성 중 상태, 사용함 표시, Lv2 이상이면 보호막입니다.
        /// 보호막은 희생 보호막과 같은 꼴로 소모될 때까지 남습니다(설계 P4).
        /// </summary>
        internal static CharacterCombatFacts Trigger(CharacterCombatFacts beforeLethal)
        {
            if (!CanTrigger(beforeLethal)) throw new InvalidOperationException("재구성할 수 없는 캐릭터입니다.");
            var status = Status(beforeLethal);
            var origin = TraitEffectOrigin.Create(beforeLethal.EntityId, TraitKey.Reconstruction);
            var used = (TraitStatusSnapshot)new ReconstructionTraitStatusSnapshot(status.Level, true)
                .WithAvailability(status.UnavailableReason, status.PartialUnavailableReason);
            var stats = beforeLethal.Stats.With(traitStatuses: beforeLethal.Stats.TraitStatuses.ToArray()
                .Select(t => t is ReconstructionTraitStatusSnapshot ? used : t).ToArray());
            int max = beforeLethal.Health.MaxHealth;
            var revived = beforeLethal.With(stats: stats).WithHealth(new HealthStateSnapshot(max, max));
            var statuses = AppliedStatusRules.Apply(revived.StatusEffects, new ReconstructionStatus(origin, InitialOwnTurnStarts));
            int shield = checked((int)((long)max * EnemyDeathTraitLevels.ReconstructionShieldPercent(status.Level) / 100));
            if (shield > 0)
            {
                var prior = statuses.Entries.OfType<ShieldStatus>().SingleOrDefault(s => s.Origin == origin);
                var granted = new ShieldStatus(origin, checked((prior?.RemainingBudget ?? 0) + shield), true,
                    prior?.LastResetControllerTurnCount ?? 0);
                statuses = ShieldStatusRules.Restore(statuses, origin, granted);
            }
            return revived.WithStatusEffects(statuses);
        }

        /// <summary>재구성 중 상태를 자기 진영 턴 시작 한 번만큼 줄입니다. 다 줄면 상태를 지우고 참(부활)을 돌려줍니다.</summary>
        internal static CharacterCombatFacts BeginOwnTurn(CharacterCombatFacts character, out bool revived)
        {
            var current = Current(character) ?? throw new InvalidOperationException("재구성 중이 아닌 캐릭터입니다.");
            revived = current.RemainingOwnTurnStarts == 1;
            var rest = character.StatusEffects.Entries.Where(s => s is not ReconstructionStatus);
            var next = revived ? rest : rest.Append(new ReconstructionStatus(current.Origin.Value, current.RemainingOwnTurnStarts - 1));
            var entries = next.ToArray();
            return character.WithStatusEffects(entries.Length == 0 ? StatusEffectSet.Empty : new StatusEffectSet(entries));
        }
    }
}
