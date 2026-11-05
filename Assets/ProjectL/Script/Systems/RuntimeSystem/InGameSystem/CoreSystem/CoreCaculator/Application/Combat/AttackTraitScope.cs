using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>반복 타격이 공유하는 가속 포착값입니다. 독립 추가 공격은 활성 부모의 몫을 다시 빌리지 않습니다.</summary>
    internal sealed class AttackTraitScope
    {
        internal EntityId ActorId { get; }
        internal int? CapturedEnemyCount { get; }
        private readonly AttackTraitScope _parent;
        private readonly int _capturedAcceleration;
        internal int CapturedAcceleration => _capturedAcceleration;
        private bool _finished;
        internal bool IsFinished => _finished;
        internal bool HasActiveAncestor
        {
            get
            {
                for (var ancestor = _parent; ancestor != null; ancestor = ancestor._parent)
                    if (!ancestor._finished) return true;
                return false;
            }
        }

        internal AttackTraitScope(CharacterCombatFacts actor, AttackTraitScope parent = null, int? capturedEnemyCount = null)
        {
            if (actor == null) throw new ArgumentNullException(nameof(actor));
            ActorId = actor.EntityId;
            _parent = parent;
            CapturedEnemyCount = capturedEnemyCount;
            int reserved = 0;
            for (var scope = parent; scope != null; scope = scope._parent)
                if (!scope._finished && scope.ActorId == ActorId) reserved = checked(reserved + scope._capturedAcceleration);
            _capturedAcceleration = Math.Max(0, AccelerationTraitRule.Held(actor.Stats) - reserved);
        }

        private AttackTraitScope(AttackTraitScope source, int capturedEnemyCount)
        {
            ActorId = source.ActorId;
            _parent = source._parent;
            _capturedAcceleration = source._capturedAcceleration;
            CapturedEnemyCount = capturedEnemyCount;
        }

        /// <summary>
        /// 따로 일어나는 추가 공격(처치 연쇄)용 사본입니다. 범위 안 적 수만 그 공격의 대상으로 다시 세고,
        /// 가속 포착값은 원래 공격 것을 그대로 씁니다. 사본은 반납(<see cref="Finish"/>)하지 않습니다.
        /// </summary>
        internal AttackTraitScope WithCapturedEnemyCount(int capturedEnemyCount)
        {
            if (capturedEnemyCount < 0) throw new ArgumentOutOfRangeException(nameof(capturedEnemyCount));
            return new AttackTraitScope(this, capturedEnemyCount);
        }

        /// <summary>계산용 사본만 바꿉니다. 영속 원본과 다른 기여분은 그대로 둡니다.</summary>
        internal CharacterCombatFacts ForDamage(CharacterCombatFacts actor) => actor.With(stats: actor.Stats.With(
            traitAttackBonus: checked(actor.Stats.TraitAttackBonus - AccelerationTraitRule.Held(actor.Stats) + _capturedAcceleration)));

        internal static int CountEnemies(CharacterCombatFacts actor, ResolvedAttackSelection selection) => selection.Targets
            .Where(t => selection.EligibleCharacters.Any(c => c.EntityId == t.Key && !c.Health.IsDead &&
                new HostilityPolicy().AreHostile(actor.Owner, c.Owner))).Select(t => t.Key).Distinct().Count();

        internal void Finish(IDictionary<EntityId, CharacterCombatFacts> characters)
        {
            if (_finished) return;
            _finished = true;
            var actor = characters[ActorId];
            int held = AccelerationTraitRule.Held(actor.Stats);
            if (_capturedAcceleration > 0 && held > 0)
                characters[ActorId] = actor.With(stats: AccelerationTraitRule.WithHeld(actor.Stats, Math.Max(0, held - _capturedAcceleration)));
        }
    }
}
