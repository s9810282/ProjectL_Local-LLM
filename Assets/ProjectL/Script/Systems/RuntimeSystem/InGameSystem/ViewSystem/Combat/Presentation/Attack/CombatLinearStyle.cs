using System;
using System.Collections.Generic;
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
    /// 관통 탄은 사라지는 점까지 날아가며 조우 대상 몸에 닿을 때마다 그 대상이 맞습니다(관통·죽음의 손 View 설계 D1·D6).
    /// </summary>
    internal sealed class CombatLinearStyle : CombatDirectAttackStyle
    {
        internal override CombatAttackPresentationKind Kind => CombatAttackPresentationKind.Linear;

        /// <summary>샷건 부채꼴처럼 지정 칸 대상이 여럿이면 대상마다 선을 긋습니다(궤적 대상별 선 설계 PT1).</summary>
        internal override bool DrawsPerTarget => true;

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
            CombatAttackShape shape, CombatContactRelease hit, Action cue)
        {
            if (hit == null) throw new ArgumentNullException(nameof(hit));
            // 접촉마다 그 자리에 접촉 이펙트를 낸 뒤 해제 규칙에 알립니다(관통·죽음의 손 View 설계 D6). 마지막 접촉이 남은 대상을 모두 풉니다.
            Action<CombatProjectileContact> impact = context.ImpactThen(settings, null);
            return new CombatLinearAttackAnimation(settings, mapper, pool, pose, target, direction,
                cue, context.CancelUnavailable, (targetId, point, final) =>
                {
                    impact(point);
                    hit.Contact(targetId, final);
                },
                (from, to) => context.CollectContacts(shape, from, to, mapper), CombatShotRoute.For(shape));
        }

        /// <summary>
        /// 목표점은 발사 높이로 올린 점이고(관통이면 사라지는 점), 접촉 목록의 진행률마다 그 대상에 맞습니다(클립 설계 5차 AC12, 관통·죽음의 손 View 설계 §3.1).
        /// </summary>
        internal override ICombatShot FireShot(ICombatAttackStyleContext context, CombatAttackShape shape, CombatAttackPresentationData settings,
            Vector3 from, FieldGridMapper mapper, Vector3 up, Quaternion attackFacing, Action<EntityId, bool> hit, Action arrived, int version)
        {
            if (hit == null) throw new ArgumentNullException(nameof(hit));
            // 스킬 사건 51의 투사체 경로가 있으면 꺾인 직선으로 쏩니다. 접촉 칸마다 그 대상을 풉니다(전투 스킬 View 설계 SK3).
            CombatShotRoute route = CombatShotRoute.For(shape);
            if (route != null)
            {
                route.Build(from, mapper, up, settings.ProjectileHeight, attackFacing);
                var routeReached = new (float Time, Action Reached)[route.Contacts.Count];
                for (int i = 0; i < route.Contacts.Count; i++)
                {
                    CombatShotContact contact = route.Contacts[i];
                    bool final = i == route.Contacts.Count - 1;
                    routeReached[i] = (contact.Progress, context.ImpactThen(settings, () => hit(contact.Target, final), route.ContactPoints[i]));
                }
                return context.FirePath(settings.ProjectilePrefab, route.Points, settings.ProjectileSpeed, Vector3.up, settings.ProjectileEasing,
                    routeReached, arrived, version);
            }
            TryShotTarget(shape.DeliveryCell, shape.Direction, shape.AffectedCells.Span, shape.PiercingShot, mapper, from, out Vector3 target);
            Vector3 to = CombatShotPath.Raise(target, up, settings.ProjectileHeight);
            IReadOnlyList<CombatShotContact> contacts = context.CollectContacts(shape, from, to, mapper);
            var reached = new (float Time, Action Reached)[contacts.Count];
            for (int i = 0; i < contacts.Count; i++)
            {
                CombatShotContact contact = contacts[i];
                bool final = i == contacts.Count - 1;
                reached[i] = (contact.Progress, context.ImpactThen(settings, () => hit(contact.Target, final),
                    CombatProjectileContact.Along(from, to, contact.Progress, up, attackFacing)));
            }
            // 방향 기준 위쪽은 직선 코드 모션과 같은 월드 위쪽입니다(표시 회전 유지).
            return context.FireLinear(settings.ProjectilePrefab, from, to, settings.ProjectileSpeed, Vector3.up, settings.ProjectileEasing,
                reached, arrived, version);
        }
    }
}
