using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>
    /// 곡사(포탄) 공격입니다(View 3라운드 W8). 목표점은 주 칸 중심이고(없으면 답하지 않음), 곡선으로 날아 착탄 때 맞습니다.
    /// 코드 모션 길은 곡사 무기 총구에서, 클립 길(공격 클립이 있는 외형)은 클립 포구에서 쏩니다.
    /// </summary>
    internal sealed class CombatMortarStyle : CombatAttackStyle
    {
        internal override CombatAttackPresentationKind Kind => CombatAttackPresentationKind.Mortar;

        internal override bool DrawsArc => true;

        protected override bool ResolveTarget(CellCoord? primaryCell, Rotation4 direction, ReadOnlySpan<CellCoord> affectedCells,
            FieldGridMapper mapper, Vector3 origin, out Vector3 target)
        {
            target = primaryCell.HasValue ? mapper.CellCenterToWorld(primaryCell.Value) : default;
            return primaryCell.HasValue;
        }

        /// <summary>곡사 궤적의 출발 접선 위에 있는 조준점입니다.</summary>
        internal Vector3 LaunchAimPoint(CombatAttackShape shape, CombatAttackPresentationData settings, FieldGridMapper mapper)
        {
            Vector3 landing = ResolveLanding(shape, mapper);
            return CombatShotPath.ArcLaunchAimPoint(landing, mapper.WorldUp, CombatShotPath.ArcHeight(mapper, settings));
        }

        /// <summary>
        /// 출발 기준점은 실제로 쏘는 길을 따릅니다. 공격 클립이 있으면(클립 길, 곡사 무기 없음) 유닛 위치, 없으면(코드 모션 길) 무기 총구입니다.
        /// 갈래 기준은 공격 처리기가 두 길을 고를 때 보는 사실(공격 클립 유무)과 같습니다. 곡사 높이는 셀 폭 × 외형의 곡사 높이입니다.
        /// </summary>
        internal override bool TryResolveTrajectoryOrigin(EntityId attackerId, CombatAttackPresentationData settings,
            CombatUnitPresentationWindow presentation, ICombatUnitPlacementReader placements, FieldGridMapper mapper,
            out Vector3 origin, out float arcHeight)
        {
            origin = default;
            arcHeight = 0f;
            if (presentation.TryGetAttackClip(attackerId, out _))
            {
                // 실제 발사는 클립 포구에서 나가 출발점이 조금 다를 수 있습니다(직선 궤적과 같은 관례).
                if (!placements.TryGetPlacedPosition(attackerId, out origin))
                    return false;
            }
            else
            {
                if (!presentation.TryGetMortarWeapon(attackerId, out CatusCannonWeaponView mortar))
                    return false;
                origin = mortar.MuzzlePosition;
            }
            arcHeight = CombatShotPath.ArcHeight(mapper, settings);
            return true;
        }

        /// <summary>총구 → 바닥 목표입니다(발사 높이를 쓰지 않음).</summary>
        internal override void Segment(Vector3 origin, Vector3 target, Vector3 up, float projectileHeight, out Vector3 from, out Vector3 to)
        {
            from = origin;
            to = target;
        }

        /// <summary>곡사 코드 모션입니다(옛 공격 재생 <c>TryBeginMortar</c>). 주 칸이 없으면 오류입니다.</summary>
        internal override bool TryBeginCodeMotion(ICombatAttackStyleContext context, EntityId attackerId, CombatAttackShape shape,
            CombatAttackPresentationData settings, FieldGridMapper mapper, CombatContactRelease hit, Action cue, Action completed, int version)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            if (shape == null) throw new ArgumentNullException(nameof(shape));
            if (hit == null) throw new ArgumentNullException(nameof(hit));
            // 공격 성공은 공격 처리기가 조립을 만들기 전에 확인했습니다.
            if (!context.TryGetMortarWeapon(attackerId, out CatusCannonWeaponView weapon))
                return false;
            // 외형별 목표점 원본은 이 전략 하나입니다(공격 궤적 구현 평가 P-V1). 곡사는 주 칸이 없으면 답하지 않습니다.
            if (!TryTarget(shape.DeliveryCell, shape.Direction, shape.AffectedCells.Span, mapper, weapon.MuzzlePosition,
                    out Vector3 target))
                throw new InvalidOperationException("[CombatMortarStyle] 곡사 공격의 주 셀 사실이 없습니다.");
            CombatProjectilePool pool = context.PoolOf(settings.ProjectilePrefab);
            // 곡사 자세는 취소 콜백 없이 빌립니다(옛 공격 재생과 같음). 쓸 수 없음은 무기·애니메이션이 알립니다.
            ICombatAttackUnitPose pose = context.BeginAttackPose(attackerId, null);
            StartWithPose(pose, () =>
            {
                var animation = new CombatMortarAnimation(settings, mapper, pool, weapon, pose, target,
                    // 곡사는 착탄 한 번에 모두 해제합니다(관통·죽음의 손 View 설계 D6의 접촉 하나 = 끝).
                    mapper.RotationToWorld(shape.Direction), cue, context.CancelUnavailable, context.ImpactThen(settings, hit.Finish));
                context.BeginAnimation(animation, completed, version);
            });
            return true;
        }

        /// <summary>
        /// 주 칸 중심에 착탄합니다. 착탄 순간이 맞음이고 같은 순간 도착입니다(맞음 먼저). 착탄은 대상 없는 마지막 접촉 하나입니다(관통·죽음의 손 View 설계 §3.1).
        /// </summary>
        internal override ICombatShot FireShot(ICombatAttackStyleContext context, CombatAttackShape shape, CombatAttackPresentationData settings,
            Vector3 from, FieldGridMapper mapper, Vector3 up, Quaternion attackFacing, Action<EntityId, bool> hit, Action arrived, int version)
        {
            if (hit == null) throw new ArgumentNullException(nameof(hit));
            Vector3 landing = ResolveLanding(shape, mapper);
            return context.FireArc(settings.ProjectilePrefab, from, landing, settings.FlightDuration,
                CombatShotPath.ArcHeight(mapper, settings), mapper.WorldUp, settings.SpinDegreesPerSecond, settings.ProjectileEasing,
                context.ImpactThen(settings, () => hit(EntityId.Empty, true), CombatProjectileContact.Along(from, landing, 1f, up, attackFacing)),
                arrived, version, settings.ArcRotationMode);
        }

        private Vector3 ResolveLanding(CombatAttackShape shape, FieldGridMapper mapper)
        {
            if (!TryTarget(shape.DeliveryCell, shape.Direction, shape.AffectedCells.Span, mapper, default, out Vector3 landing))
                throw new InvalidOperationException("[CombatMortarStyle] 곡사 공격의 주 셀 사실이 없습니다.");
            return landing;
        }
    }
}
