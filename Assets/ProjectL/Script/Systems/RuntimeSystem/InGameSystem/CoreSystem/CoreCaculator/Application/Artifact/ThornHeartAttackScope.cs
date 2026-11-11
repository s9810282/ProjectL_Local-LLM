using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Artifact.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact
{
    /// <summary>공격 한 번의 가시 심장 발동 기회만 소유합니다. 반사 피해의 적용과 투영 반영은 공유 피해 적용 경계가 맡습니다.</summary>
    internal sealed class ThornHeartAttackScope
    {
        private readonly ThornHeartRule _rule;
        private readonly EffectOriginKey _origin;
        private readonly bool _actualOrdinaryAttack;
        private readonly bool _inCombat;
        private bool _consumed;

        internal ThornHeartAttackScope(CompiledArtifactLoadout loadout, bool actualOrdinaryAttack, bool inCombat)
        {
            if (loadout == null) throw new ArgumentNullException(nameof(loadout));
            foreach (var compiled in loadout.Rules.Span)
            {
                if (compiled.Rule is not ThornHeartRule rule) continue;
                if (_rule != null || compiled.Origin.Kind != EffectOwnerKind.Artifact)
                    throw new ArgumentException("가시 심장 규칙은 Artifact 출처 하나만 허용합니다.", nameof(loadout));
                _rule = rule;
                _origin = compiled.Origin;
            }
            _actualOrdinaryAttack = actualOrdinaryAttack;
            _inCombat = inCombat;
        }

        private ThornHeartAttackScope(ThornHeartAttackScope source)
        {
            _rule = source._rule;
            _origin = source._origin;
            _actualOrdinaryAttack = source._actualOrdinaryAttack;
            _inCombat = source._inCombat;
            _consumed = source._consumed;
        }

        /// <summary>폐기할 피해 계획에서 같은 반사 기회와 조건을 독립적으로 평가합니다.</summary>
        internal ThornHeartAttackScope Clone() => new ThornHeartAttackScope(this);

        internal bool CanReflect(CharacterCombatFacts attacker, CharacterCombatFacts struckAllyBeforeDamage)
        {
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            if (struckAllyBeforeDamage == null) throw new ArgumentNullException(nameof(struckAllyBeforeDamage));
            if (attacker.EntityId == struckAllyBeforeDamage.EntityId)
                throw new ArgumentException("공격자와 피격 아군은 다른 개체여야 합니다.", nameof(struckAllyBeforeDamage));

            return !_consumed && _rule != null && _actualOrdinaryAttack && _inCombat &&
                attacker.Owner == Owner.Enemy && struckAllyBeforeDamage.Owner == Owner.Player &&
                attacker.Area == CharacterRuntimeArea.Field && !attacker.Health.IsDead &&
                struckAllyBeforeDamage.Area == CharacterRuntimeArea.Field && !struckAllyBeforeDamage.Health.IsDead;
        }

        internal bool TryReflect(CharacterCombatFacts attacker, CharacterCombatFacts struckAllyBeforeDamage,
            long reflectionGroup, CombatDamageApplication damage, out ThornHeartReflection reflection)
        {
            if (damage == null) throw new ArgumentNullException(nameof(damage));
            reflection = null;
            if (!CanReflect(attacker, struckAllyBeforeDamage)) return false;
            var context = new ThornHeartReflectionContext(struckAllyBeforeDamage.EntityId,
                CommandAttackGroups.Direct, reflectionGroup);
            int requested = _rule.EvaluateDamage(_inCombat, hasEnemyDirectHitOnAlly: true);
            if (requested == 0)
            {
                _consumed = true;
                return false;
            }

            if (!damage.Characters.TryGetValue(attacker.EntityId, out var current) || !ReferenceEquals(current, attacker))
                throw new ArgumentException("반사 대상은 피해 적용 경계의 현재 사실이어야 합니다.", nameof(attacker));
            var origin = new CombatDamageOrigin(struckAllyBeforeDamage.Anchor, null, reflectionGroup);
            int activationIndex = damage.Events.Count;
            // 반사도 공격의 공유 사전 전체를 쓰는 적용 경계를 지난다. 공격자 사실은 경계가 사전에 반영한다.
            var outcome = damage.Apply(attacker.EntityId, CombatDamageKind.ReflectedDamage, origin, requested, deferTraitKills: true);
            int applied = outcome.ActualHealthLoss;
            var activation = new CombatArtifactEvent(new[]
            {
                // 기존 반사 요청은 유지하고, 수신 증폭이 더 크면 그 요청을 기록해 실제 손실을 포함합니다.
                new ArtifactActivationSnapshot(_origin, attacker.EntityId, Math.Max(requested, outcome.DamageBeforeItems), applied),
            }, context);
            damage.Events.Insert(activationIndex, activation);
            // 반사의 피격 아군 귀속이 보인 뒤 확정 사망을 소비해야 합니다.
            damage.SettleTraitKills(activationIndex);
            reflection = new ThornHeartReflection(new CharacterStateChange(outcome.Before, outcome.After), activation, origin);
            _consumed = true;
            return true;
        }
    }
}
