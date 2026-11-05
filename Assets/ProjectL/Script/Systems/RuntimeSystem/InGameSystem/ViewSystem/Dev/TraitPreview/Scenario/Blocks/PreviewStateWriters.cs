#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview.Scenario.Blocks
{
    internal interface IPreviewStateWriter
    {
        PreviewStateKind Kind { get; }

        CharacterCombatFacts Apply(PreviewScenarioLedger ledger, CharacterCombatFacts before, PreviewStateInput input);
    }

    internal sealed class PreviewAccelerationWriter : IPreviewStateWriter
    {
        public PreviewStateKind Kind => PreviewStateKind.Acceleration;

        public CharacterCombatFacts Apply(PreviewScenarioLedger ledger, CharacterCombatFacts before, PreviewStateInput input)
        {
            int value = input.operation == PreviewStateOperation.Remove ? 0 : input.acceleration;
            if (value < 0)
                throw new ArgumentException("가속 축적은 0 이상이어야 합니다.");
            var statuses = before.Stats.TraitStatuses.ToArray();
            int index = Array.FindIndex(statuses, s => s is AccelerationTraitStatusSnapshot);
            if (index < 0)
                throw new ArgumentException("가속 보유 특성을 초기 유닛에 지정하세요.");
            statuses[index] = new AccelerationTraitStatusSnapshot(value);
            return before.With(stats: before.Stats.With(traitStatuses: statuses));
        }
    }

    internal sealed class PreviewEffectWriter : IPreviewStateWriter
    {
        public PreviewStateKind Kind => PreviewStateKind.Effect;
        public CharacterCombatFacts Apply(PreviewScenarioLedger ledger, CharacterCombatFacts before, PreviewStateInput input)
        {
            var value = input.effect ?? throw new ArgumentException("효과 입력이 없습니다.");
            PreviewScenarioValidation.Defined(value.key);
            string rule = "preview:" + value.id;
            var entries = before.StatusEffects.Entries.ToList();
            int index = entries.FindIndex(s => s.Origin?.RuleKey == rule);
            bool remove = input.operation == PreviewStateOperation.Remove;
            EffectOriginKey? origin = null;
            if (!remove)
            {
                if (value.ownerKind == EffectOwnerKind.Trait)
                {
                    PreviewScenarioValidation.Defined(value.trait);
                    if (value.trait == default) throw new ArgumentException("효과 출처 특성이 필요합니다.");
                }
                int owner = string.IsNullOrEmpty(value.sourceId) ? value.ownerInstanceId : ledger.Get(value.sourceId).EntityId.Value;
                int definition = value.ownerKind == EffectOwnerKind.Trait ? (int)value.trait : value.definitionKey;
                origin = new EffectOriginKey(value.ownerKind, owner, definition, rule);
            }
            MovementEffectId? movement = null;
            if (!remove && value.key == StatusEffectKey.WeaknessExposure && value.hasMovement)
                movement = new MovementEffectId(value.battleInstanceId, value.controllerTurnSequence,
                    new ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids.EntityId(origin.Value.OwnerInstanceId), value.moveCommandNumber);
            ledger.EffectSource(value.id, input.unitId, value.key, origin, remove, movement);
            if (remove)
            {
                if (index < 0) throw new ArgumentException($"수신자에게 제거할 효과가 없습니다: {value.id}");
                entries.RemoveAt(index);
            }
            else
            {
                AppliedStatus status = value.key switch
                {
                    StatusEffectKey.AttackIncrease => new AttackIncreaseStatus(origin.Value, value.amount, value.remainingTurns),
                    StatusEffectKey.AttackDecrease => new AttackDecreaseStatus(origin.Value, value.percent),
                    StatusEffectKey.DefenseDecrease => new DefenseDecreaseStatus(origin.Value, value.percent),
                    StatusEffectKey.Shield => new ShieldStatus(origin.Value, value.amount, value.eligible, value.lastResetControllerTurnCount),
                    StatusEffectKey.Disarm or StatusEffectKey.Bind => new ActionRestrictionStatus(value.key, origin.Value, value.remainingTurns),
                    StatusEffectKey.WeaknessExposure => new WeaknessExposureStatus(origin.Value, value.percent, value.lifetime,
                        value.lifetime == WeaknessExposureLifetimeKind.AffectedSideTurnEnds ? value.remainingTurns : (int?)null, movement),
                    _ => throw new ArgumentException($"정규 출처 상태 입력을 지원하지 않습니다: {value.key}. 방어 증가에는 Cohesion 입력을 사용하세요.")
                };
                if (index < 0) entries.Add(status); else entries[index] = status;
            }
            var after = before.WithStatusEffects(new StatusEffectSet(entries));
            // Core의 읽기 계약으로 유효한 typed 행이 만들어지는지 검사합니다. 규칙을 재적용하지 않습니다.
            _ = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StatusEffectReadModel.Capture(after);
            return after;
        }
    }

    internal sealed class PreviewCohesionWriter : IPreviewStateWriter
    {
        public PreviewStateKind Kind => PreviewStateKind.Cohesion;
        public CharacterCombatFacts Apply(PreviewScenarioLedger ledger, CharacterCombatFacts before, PreviewStateInput input) =>
            before.With(traitEffects: new CharacterTraitEffects(before.TraitEffects.AttackGrants, before.TraitEffects.Shields,
                input.operation == PreviewStateOperation.Remove ? 0 : input.cohesionDefenseBonus));
    }

    internal sealed class PreviewRegenerationWriter : IPreviewStateWriter
    {
        public PreviewStateKind Kind => PreviewStateKind.Regeneration;
        public CharacterCombatFacts Apply(PreviewScenarioLedger ledger, CharacterCombatFacts before, PreviewStateInput input)
        {
            var statuses = before.Stats.TraitStatuses.ToArray();
            int index = Array.FindIndex(statuses, s => s is RegenerationTraitStatusSnapshot);
            if (index < 0) throw new ArgumentException("초기 유닛에 재생 보유를 지정하세요.");
            var old = (RegenerationTraitStatusSnapshot)statuses[index];
            statuses[index] = input.operation == PreviewStateOperation.Remove
                ? new RegenerationTraitStatusSnapshot(old.HealAmount, old.HealEveryOwnSideTurnEnds, 0)
                : new RegenerationTraitStatusSnapshot(input.healAmount, input.healEveryOwnSideTurnEnds, input.completedEligibleTurnEnds);
            return before.With(stats: before.Stats.With(traitStatuses: statuses));
        }
    }

    /// <summary>시연에 명시한 사실만 옮깁니다. 턴 경과/발동/피해/회복 규칙은 실행하지 않습니다.</summary>
    internal sealed class PreviewStateWriters
    {
        private readonly Dictionary<PreviewStateKind, IPreviewStateWriter> _writers;
        internal PreviewStateWriters(IEnumerable<IPreviewStateWriter> writers) => _writers = writers.ToDictionary(w => w.Kind);
        internal void Apply(PreviewScenarioLedger ledger, PreviewStateInput input)
        {
            if (input == null) throw new ArgumentException("상태 입력이 없습니다.");
            PreviewScenarioValidation.Defined(input.operation);
            if (!_writers.TryGetValue(input.kind, out var writer)) throw new ArgumentException($"지원하지 않는 상태: {input.kind}");
            var after = writer.Apply(ledger, ledger.Alive(input.unitId), input);
            after = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.CombatEffectiveStatsCalculator.ProjectTraitAttack(after);
            ledger.Set(input.unitId, after);
        }
        internal void Initialize(PreviewScenarioLedger ledger, PreviewScenarioUnit unit)
        {
            if (unit.acceleration != 0 || unit.ownsAcceleration)
                Apply(ledger, new PreviewStateInput { unitId = unit.id, acceleration = unit.acceleration });
            if (unit.effects == null || unit.grants == null || unit.grants.Count != 0)
                throw new ArgumentException("초기 효과 목록이 비었거나 v1 입력 이전이 필요합니다.");
            var ids = new HashSet<string>();
            foreach (var effect in unit.effects)
            {
                if (effect == null || !ids.Add(effect.id)) throw new ArgumentException("초기 효과의 Source ID가 비었거나 중복됩니다.");
                Apply(ledger, new PreviewStateInput { unitId = unit.id, kind = PreviewStateKind.Effect, effect = effect });
            }
            if (unit.cohesionDefenseBonus != 0)
                Apply(ledger, new PreviewStateInput { unitId = unit.id, kind = PreviewStateKind.Cohesion, cohesionDefenseBonus = unit.cohesionDefenseBonus });
        }
    }
}
#endif
