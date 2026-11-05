using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>
    /// 직선(총알) 공격입니다(View 3라운드 W8). 목표점은 근접과 같은 해석이고, 발사 높이로 올린 직선을 날려 주 대상 몸에 처음 닿을 때 맞습니다.
    /// </summary>
    internal sealed class CombatLinearStyle : CombatDirectAttackStyle
    {
        internal override CombatAttackPresentationKind Kind => CombatAttackPresentationKind.Linear;

        /// <summary>출발 기준점은 유닛 위치입니다(발사 높이는 <see cref="CombatAttackStyle.Segment"/>가 올림).</summary>
        internal override bool TryResolveTrajectoryOrigin(EntityId attackerId, CombatAttackPresentationData settings,
            CombatUnitPresentationWindow presentation, ICombatUnitPlacementReader placements, FieldGridMapper mapper,
            out Vector3 origin, out float arcHeight)
        {
            arcHeight = 0f;
            return placements.TryGetPlacedPosition(attackerId, out origin);
        }

        /// <summary>발사 높이에서 0.2 앞 → 발사 높이 목표입니다(실제 직선 발사와 같은 기하).</summary>
        internal override void Segment(Vector3 origin, Vector3 target, Vector3 up, float projectileHeight, out Vector3 from, out Vector3 to)
        {
            to = CombatShotPath.Raise(target, up, projectileHeight);
            from = CombatShotPath.LinearStart(CombatShotPath.Raise(origin, up, projectileHeight), to);
        }

        protected override CombatProjectilePool PoolOf(ICombatAttackStyleContext context, CombatAttackPresentationData settings) =>
            context.PoolOf(settings.ProjectilePrefab);

        protected override ICombatAttackAnimation CreateAnimation(ICombatAttackStyleContext context, CombatAttackPresentationData settings,
            FieldGridMapper mapper, CombatProjectilePool pool, ICombatAttackUnitPose pose, Vector3 target, Quaternion direction,
            CombatAttackShape shape, Action hit, Action cue) =>
            new CombatLinearAttackAnimation(settings, mapper, pool, pose, target, direction,
                cue, context.CancelUnavailable, context.ImpactThen(settings, hit),
                shape != null ? (from, to) => context.LinearContactProgress(shape, from, to, mapper) : null);

        /// <summary>목표점은 발사 높이로 올린 점이고, 주 대상 몸 접촉 진행률에서 맞습니다(클립 설계 5차 AC12).</summary>
        internal override ICombatShot FireShot(ICombatAttackStyleContext context, CombatAttackShape shape, CombatAttackPresentationData settings,
            Vector3 from, FieldGridMapper mapper, Vector3 up, Quaternion attackFacing, Action hit, Action arrived, int version)
        {
            TryTarget(shape.DeliveryCell, shape.Direction, shape.AffectedCells.Span, mapper, from, out Vector3 target);
            Vector3 to = CombatShotPath.Raise(target, up, settings.ProjectileHeight);
            float contact = context.LinearContactProgress(shape, from, to, mapper);
            if (!float.IsFinite(contact))
                throw new InvalidOperationException("[CombatAttackPlayback] 직선 출처 발사의 히트 진행률이 유한하지 않습니다.");
            float hitAt = Mathf.Clamp01(contact);
            // 방향 기준 위쪽은 직선 코드 모션과 같은 월드 위쪽입니다(표시 회전 유지).
            return context.FireLinear(settings.ProjectilePrefab, from, to, settings.ProjectileSpeed, Vector3.up, hitAt, settings.ProjectileEasing,
                context.ImpactThen(settings, hit, CombatProjectileContact.Along(from, to, hitAt, up, attackFacing)), arrived, version);
        }
    }
}
