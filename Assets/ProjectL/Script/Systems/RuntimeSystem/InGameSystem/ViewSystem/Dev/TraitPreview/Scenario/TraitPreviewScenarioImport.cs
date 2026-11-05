#if UNITY_EDITOR
using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview.Scenario
{
    /// <summary>기존 입력을 원본 수정 없이 공통 블록으로 옮깁니다.</summary>
    public static class TraitPreviewScenarioImport
    {
        /// <summary>구형 입력은 원본/식별을 바꾸지 않고 정규 효과 입력으로 명시 이전합니다.</summary>
        public static TraitPreviewScenarioInput Upgrade(TraitPreviewScenarioInput original)
        {
            if (original == null) throw new ArgumentNullException(nameof(original));
            var copy = original.Copy();
            if (copy.version < 1 || copy.version > 3)
                throw new ArgumentException($"지원하지 않는 조립 버전: {copy.version}");
            if (copy.units == null || copy.entries == null) throw new ArgumentException("유닛/시연 순서 목록이 없습니다.");
            if (copy.version == 3) return copy;
            if (copy.version == 2) return UpgradeEffects(copy);

            foreach (var unit in copy.units)
            {
                if (unit == null) throw new ArgumentException("초기 유닛 입력이 비었습니다.");
                if (unit.grants == null) throw new ArgumentException("초기 상태 목록이 비었습니다.");
                unit.effects ??= new System.Collections.Generic.List<PreviewEffectSourceInput>();
                foreach (var grant in unit.grants) unit.effects.Add(Effect(grant));
                unit.grants.Clear();
            }
            foreach (var entry in copy.entries)
            {
                if (entry?.blocks == null) throw new ArgumentException("시연 블록 목록이 비었습니다.");
                foreach (var block in entry.blocks)
                {
                    if (block == null) throw new ArgumentException("이전할 시연 블록이 비었습니다.");
                    if (block.kind == PreviewBlockKind.State && block.state == null)
                        throw new ArgumentException("이전할 상태 입력이 비었습니다.");
                    if (block.kind == PreviewBlockKind.State && block.state.kind == PreviewStateKind.AttackGrant)
                    {
                        block.state.effect = Effect(block.state.grant);
                        block.state.kind = PreviewStateKind.Effect;
                    }
                }
            }
            copy.version = 2;
            return UpgradeEffects(copy);
        }
        private static TraitPreviewScenarioInput UpgradeEffects(TraitPreviewScenarioInput copy)
        {
            void Upgrade(PreviewEffectSourceInput effect)
            {
                if (effect == null) throw new ArgumentException("이전할 효과가 비었습니다.");
                effect.lifetime = WeaknessExposureLifetimeKind.AffectedSideTurnEnds;
                effect.hasMovement = false;
            }
            foreach (var unit in copy.units)
            {
                if (unit?.effects == null) throw new ArgumentException("이전할 초기 효과 목록이 없습니다.");
                unit.actionUse = ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.UnitActionUse.Available;
                unit.distanceMoved = 0;
                foreach (var effect in unit.effects) Upgrade(effect);
            }
            foreach (var entry in copy.entries)
            {
                if (entry?.blocks == null) throw new ArgumentException("이전할 블록 목록이 없습니다.");
                foreach (var block in entry.blocks)
                {
                    if (block == null) throw new ArgumentException("이전할 블록이 비었습니다.");
                    if (block.kind == PreviewBlockKind.State && block.state?.kind == PreviewStateKind.Effect)
                        Upgrade(block.state.effect);
                }
            }
            copy.version = 3;
            return copy;
        }

        private static PreviewEffectSourceInput Effect(PreviewGrantInput grant)
        {
            if (grant == null) throw new ArgumentException("이전할 공격력 부여가 비었습니다.");
            return new PreviewEffectSourceInput
            {
                id = grant.id, key = StatusEffectKey.AttackIncrease, sourceId = grant.sourceId,
                trait = grant.trait, amount = grant.amount, remainingTurns = grant.remainingTurns
            };
        }

        public static TraitPreviewScenarioInput FromSimple(TraitActivationPreviewInput original)
        {
            if (original == null)
                throw new ArgumentNullException(nameof(original));
            var input = original.Copy();
            PreviewScenarioValidation.Defined(input.context);
            if (input.damage < 0)
                throw new ArgumentException("피해는 음수일 수 없습니다.");
            if (input.units == null || input.targetIndices == null)
                throw new ArgumentException("유닛/대상 목록이 없습니다.");
            string Id(int index) => index >= 0 && index < input.units.Count ? "unit_" + index : throw new ArgumentException($"유닛 인덱스 범위 밖: {index}");
            var scenario = new TraitPreviewScenarioInput
            {
                stageViewKey = input.stageViewKey,
                repeat = input.repeat,
                observationSeconds = input.observationSeconds,
                repeatInterval = input.repeatInterval,
                units = input.units.Select((u, i) => new PreviewScenarioUnit { id = Id(i), name = u.key.ToString(), unit = u }).ToList()
            };
            var entry = new PreviewScenarioEntry
            {
                name = "간단 입력 가져오기"
            };
            var trait = new PreviewTraitInput
            {
                asset = input.traitAsset,
                kind = input.kind,
                sourceId = Id(input.sourceIndex),
                targetIds = input.targetIndices.Select(Id).ToList(),
                cells = input.cells,
                magnitude = input.magnitude
            };
            if (input.context == TraitActivationPreviewContext.Moment)
                entry.blocks.Add(new PreviewScenarioBlock { kind = PreviewBlockKind.Activation, name = "일반 발동", trait = trait });
            else
            {
                bool interception = input.context == TraitActivationPreviewContext.Interception;
                var attack = new PreviewAttackInput
                {
                    attackerId = Id(interception ? input.attackerIndex : input.sourceIndex),
                    direction = input.direction,
                    targetCell = input.targetCell
                };
                foreach (int index in input.targetIndices)
                {
                    Id(index);
                    int health = input.units[interception ? input.sourceIndex : index].health;
                    attack.hits.Add(new PreviewHitInput { targetId = Id(index), finalHealth = input.kill ? 0 : Math.Max(0, health - input.damage) });
                }

                if (interception)
                {
                    if (input.targetIndices.Count != 1)
                        throw new ArgumentException("가로채기 대상은 한 명이어야 합니다.");
                    attack.targetCell = input.units[input.targetIndices[0]].cell;
                    entry.blocks.Add(new PreviewScenarioBlock { kind = PreviewBlockKind.Attack, name = "공격", attack = attack });
                    entry.blocks.Add(new PreviewScenarioBlock { kind = PreviewBlockKind.Interception, name = "가로채기", trait = trait });
                }
                else
                    entry.blocks.Add(new PreviewScenarioBlock { kind = PreviewBlockKind.ChildAttack, name = "추가 공격", attack = attack, trait = trait });
            }

            scenario.entries.Add(entry);
            return scenario;
        }
    }
}
#endif
