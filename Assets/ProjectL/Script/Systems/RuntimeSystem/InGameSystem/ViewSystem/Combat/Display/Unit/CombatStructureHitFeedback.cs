using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>건물 피격의 표시 자격·몸 위치·출력을 공유합니다. 피해 분배와 사실 반영 시점은 호출자가 소유합니다.</summary>
    internal sealed class CombatStructureHitFeedback
    {
        private readonly Func<EntityId, Pose?> _body;
        private readonly Action<EntityKey, EntityId, Pose> _cue;
        private readonly Action<Vector3, int> _damage;

        internal CombatStructureHitFeedback(Func<EntityId, Pose?> body, Action<EntityKey, EntityId, Pose> cue,
            Action<Vector3, int> damage)
        {
            _body = body ?? throw new ArgumentNullException(nameof(body));
            _cue = cue ?? throw new ArgumentNullException(nameof(cue));
            _damage = damage ?? throw new ArgumentNullException(nameof(damage));
        }

        /// <summary>기존 적 건물 표시 조건입니다. 고유 장애물은 자신의 피드백 경로를 사용합니다.</summary>
        internal static bool CanShow(CombatStructureEvent hit) =>
            hit.Final.Owner == Owner.Enemy && !ObstacleContentProfile.IsNativeObstacleKey(hit.Final.Definition.Key);

        /// <summary>후속 사건 시점에 피격 큐를 한 번 냅니다. 분할하지 않은 숫자는 같은 몸 위치에 이어서 표시합니다.</summary>
        internal void ShowHit(CombatStructureEvent hit, bool showDamage)
        {
            int loss = hit.Expected.Health.CurrentHealth - hit.Final.Health.CurrentHealth;
            if (loss <= 0 || !CanShow(hit)) return;
            Pose body = RequireBody(hit);
            _cue(hit.Final.Definition.Key, hit.Final.EntityId, body);
            if (showDamage) _damage(body.position, loss);
        }

        /// <summary>
        /// 탄별 접촉의 숫자만 표시합니다. 체력·파괴는 진행하지 않습니다. 피격 큐는 앞 탄이 <see cref="ShowContactHit"/>,
        /// 마지막 탄(출처 해제가 연 Core 사건)이 <see cref="ShowHit"/>로 냅니다.
        /// </summary>
        internal void ShowDamage(CombatStructureEvent hit, int amount)
        {
            if (amount <= 0 || !CanShow(hit)) return;
            _damage(RequireBody(hit).position, amount);
        }

        /// <summary>연발 앞 탄의 접촉 피격입니다(2026-11-11). 피격 큐만 내고 숫자(<see cref="ShowDamage"/>)·체력·파괴는 진행하지 않습니다.</summary>
        internal void ShowContactHit(CombatStructureEvent hit)
        {
            if (hit == null) throw new ArgumentNullException(nameof(hit));
            if (hit.Expected.Health.CurrentHealth - hit.Final.Health.CurrentHealth <= 0 || !CanShow(hit)) return;
            _cue(hit.Final.Definition.Key, hit.Final.EntityId, RequireBody(hit));
        }

        private Pose RequireBody(CombatStructureEvent hit) =>
            _body(hit.Final.EntityId) ?? throw new InvalidOperationException($"Structure 피격 표시의 몸 자리가 없습니다: {hit.Final.EntityId}");
    }
}
