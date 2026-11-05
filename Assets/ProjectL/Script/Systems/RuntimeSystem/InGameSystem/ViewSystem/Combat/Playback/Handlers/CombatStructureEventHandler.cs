using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Handlers
{
    /// <summary>기존 Gate 해제 뒤 Structure의 피격 피드백과 사실 반영을 수행합니다.</summary>
    internal sealed class CombatStructureEventHandler : CombatEventHandler
    {
        private readonly Func<EntityId, Pose?> _body;
        private readonly Action<EntityKey, EntityId, Pose> _cue;
        private readonly Action<Vector3, int> _damage;
        private readonly Action<StructureCombatFacts> _apply;
        internal CombatStructureEventHandler(Func<EntityId, Pose?> body, Action<EntityKey, EntityId, Pose> cue,
            Action<Vector3, int> damage, Action<StructureCombatFacts> apply)
        {
            _body = body ?? throw new ArgumentNullException(nameof(body));
            _cue = cue ?? throw new ArgumentNullException(nameof(cue));
            _damage = damage ?? throw new ArgumentNullException(nameof(damage));
            _apply = apply ?? throw new ArgumentNullException(nameof(apply));
        }
        internal override bool TryBeginBeforeFacts(CombatEvent e, Action completed, int version)
        {
            var hit = (CombatStructureEvent)e;
            int loss = hit.Expected.Health.CurrentHealth - hit.Final.Health.CurrentHealth;
            if (loss <= 0 || hit.Final.Owner != Owner.Enemy || ObstacleContentProfile.IsNativeObstacleKey(hit.Final.Definition.Key)) return false;
            Pose? body = _body(hit.Final.EntityId);
            if (!body.HasValue) throw new InvalidOperationException($"Structure 피격 표시의 몸 자리가 없습니다: {hit.Final.EntityId}");
            _cue(hit.Final.Definition.Key, hit.Final.EntityId, body.Value);
            _damage(body.Value.position, loss);
            return false;
        }
        internal override void ApplyFacts(CombatEvent e) => _apply(((CombatStructureEvent)e).Final);
    }
}
