using System;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>
    /// 공격 연출 종류 하나의 지식입니다(View 3라운드 W8, 설계 §6.2). 목표점·궤적·코드 모션·출처 발사가 종류마다 다른 곳을 이 한 클래스가 답합니다.
    /// 종류별 구현은 <see cref="CombatAttackStyles.Of"/>로 찾습니다. 클립 공격의 해제는 종류마다 다르지 않아 처리기가 출처의 대상을 모두 해제합니다
    /// (옛 Ballista 관통 길만 대상별로 해제했고, 그 길과 함께 종류별 해제 입구를 걷었습니다).
    /// </summary>
    /// <remarks>
    /// 수명(Gate·투사체 진행·Tick·취소)은 공격 재생에 남고, 전략은 좁은 맥락(<see cref="ICombatAttackStyleContext"/>)으로만 씁니다.
    /// DB 쪽 종류 검사(공격 표시 SO·클립 규칙)는 DataBaseSystem이 View를 참조하지 않아 열거 기준으로 따로 있습니다(설계 P26).
    /// </remarks>
    internal abstract class CombatAttackStyle
    {
        /// <summary>이 전략의 공격 연출 종류입니다.</summary>
        internal abstract CombatAttackPresentationKind Kind { get; }

        /// <summary>
        /// 발사 목표점(월드, 필드 바닥 높이)입니다. 실제 공격(결과)과 궤적 미리보기(응답)·공격 방향이 같은 값을 넘겨 부릅니다(구현 평가 P-V1).
        /// 발사 높이는 이 점에 더하지 않습니다(직선은 부르는 쪽이 <see cref="CombatShotPath.Raise"/>).
        /// </summary>
        /// <param name="origin">방향 해석의 출발점입니다(직선은 유닛 위치).</param>
        internal bool TryTarget(CellCoord? primaryCell, Rotation4 direction, ReadOnlySpan<CellCoord> affectedCells,
            FieldGridMapper mapper, Vector3 origin, out Vector3 target)
        {
            if (mapper == null)
                throw new ArgumentNullException(nameof(mapper));
            return ResolveTarget(primaryCell, direction, affectedCells, mapper, origin, out target);
        }

        /// <summary>종류마다의 목표점 해석입니다. Mapper는 <see cref="TryTarget"/>이 검사했습니다.</summary>
        protected abstract bool ResolveTarget(CellCoord? primaryCell, Rotation4 direction, ReadOnlySpan<CellCoord> affectedCells,
            FieldGridMapper mapper, Vector3 origin, out Vector3 target);

        /// <summary>궤적 미리보기가 곡선인지입니다(곡사).</summary>
        internal virtual bool DrawsArc => false;

        /// <summary>
        /// 궤적의 출발 기준점(월드)과 곡사 높이입니다. 궤적이 없는 종류(근접)와 표시 없는 공격자는 false입니다(공격 궤적 설계 TJ4).
        /// 공격 클립이 없는 곡사 외형(코드 모션 길)에 무기 연결이 빠지면 실제 공격과 같이 창구가 예외로 알립니다(데이터 오류).
        /// </summary>
        internal virtual bool TryResolveTrajectoryOrigin(EntityId attackerId, CombatAttackPresentationData settings,
            CombatUnitPresentationWindow presentation, ICombatUnitPlacementReader placements, FieldGridMapper mapper,
            out Vector3 origin, out float arcHeight)
        {
            origin = default;
            arcHeight = 0f;
            return false;
        }

        /// <summary>
        /// 궤적의 출발·끝(월드)입니다. 출발 기준점을 답한 종류(<see cref="TryResolveTrajectoryOrigin"/>)만 부르며, 궤적이 없는 종류는 예외입니다.
        /// 실제 연출과 같은 기하 함수(<see cref="CombatShotPath"/>)만 씁니다.
        /// </summary>
        internal virtual void Segment(Vector3 origin, Vector3 target, Vector3 up, float projectileHeight, out Vector3 from, out Vector3 to) =>
            throw new InvalidOperationException($"[CombatAttackStyle] 궤적이 없는 공격 연출 종류입니다: {Kind}");

        /// <summary>정면 공격 모션(조립의 포효 박치기, 공격 사건 없음)을 지원하는지입니다. 주 칸이 필요한 곡사는 false입니다.</summary>
        internal virtual bool SupportsFacingMotion => false;

        /// <summary>
        /// 코드 모션을 시작합니다. 연출을 시작할 수 없는 공격자는 false이며, 코드 모션 조각이 바로 끝납니다(Gate는 조립 실행이 맡음, 범용 연출 구조 계획 P8).
        /// <paramref name="shape"/>가 null이면 정면 공격 모션이고, <see cref="SupportsFacingMotion"/>인 종류만 그렇게 부릅니다.
        /// </summary>
        /// <param name="shape">공격 모양입니다(특성 발동 연출 설계 TA5). 공격자는 <paramref name="attackerId"/>와 같습니다(공격 재생이 확인).</param>
        /// <param name="hit">타격 순간입니다(대상 해제, 투사체 종류는 접촉 이펙트 뒤).</param>
        /// <param name="cue">기준 순간입니다(근접 = 타격 직전, 직선·곡사 = 발사, 조각 신호 <c>MotionCue</c>). 옛 효과음 순간과 같습니다.</param>
        internal abstract bool TryBeginCodeMotion(ICombatAttackStyleContext context, EntityId attackerId, CombatAttackShape shape,
            CombatAttackPresentationData settings, FieldGridMapper mapper, Action hit, Action cue, Action completed, int version);

        /// <summary>
        /// 출처 발사(클립 공격의 발사 조각)입니다. 투사체를 쏘지 않는 종류(근접)는 예외입니다.
        /// </summary>
        /// <param name="up">정규화한 필드 위쪽입니다.</param>
        /// <param name="attackFacing">접촉 자리가 수평 길이 없이 정해질 때의 회전(공격 방향)입니다.</param>
        internal virtual ICombatShot FireShot(ICombatAttackStyleContext context, CombatAttackShape shape, CombatAttackPresentationData settings,
            Vector3 from, FieldGridMapper mapper, Vector3 up, Quaternion attackFacing, Action hit, Action arrived, int version) =>
            throw new InvalidOperationException($"[CombatAttackStyle] 출처 발사는 직선·곡사만 쏩니다: {Kind}");

        /// <summary>
        /// 빌린 공격 자세로 연출을 시작합니다. 시작이 실패하면 자세를 되돌리고, 되돌리기도 실패하면 둘을 함께 던집니다.
        /// 코드 모션 둘(근접·직선 공통, 곡사)이 같은 규칙을 씁니다.
        /// </summary>
        protected static void StartWithPose(ICombatAttackUnitPose pose, Action start)
        {
            try
            {
                start();
            }
            catch (Exception error)
            {
                try
                {
                    pose.RestorePose();
                }
                catch (Exception cleanup)
                {
                    throw new AggregateException(error, cleanup);
                }
                throw;
            }
        }
    }

    /// <summary>
    /// 근접·직선의 공통 코드 모션입니다(옛 공격 재생 <c>TryBeginDirect</c>). 공격 모양이 있으면 그 타격 칸·공격 방향을,
    /// 없으면(정면 공격 모션) 공격자의 현재 방향 앞을 목표로 씁니다. Gate 수명은 이 동작을 담은 조립 실행이 맡습니다.
    /// </summary>
    internal abstract class CombatDirectAttackStyle : CombatAttackStyle
    {
        internal sealed override bool SupportsFacingMotion => true;

        /// <summary>주 칸, 없으면 방향 앞 가장 먼 영향 칸입니다(<see cref="CombatAttackTarget"/>). 늘 답합니다.</summary>
        protected override bool ResolveTarget(CellCoord? primaryCell, Rotation4 direction, ReadOnlySpan<CellCoord> affectedCells,
            FieldGridMapper mapper, Vector3 origin, out Vector3 target)
        {
            target = CombatAttackTarget.Resolve(primaryCell, direction, affectedCells, mapper, origin);
            return true;
        }

        internal sealed override bool TryBeginCodeMotion(ICombatAttackStyleContext context, EntityId attackerId, CombatAttackShape shape,
            CombatAttackPresentationData settings, FieldGridMapper mapper, Action hit, Action cue, Action completed, int version)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            CombatProjectilePool pool = PoolOf(context, settings);
            ICombatAttackUnitPose pose = context.BeginAttackPose(attackerId, context.CancelUnavailable);
            StartWithPose(pose, () =>
            {
                Vector3 target;
                Quaternion direction;
                if (shape != null)
                {
                    // 외형별 목표점 원본은 이 전략 하나입니다(궤적 미리보기와 공유, 공격 궤적 구현 평가 P-V1). 직선·근접은 항상 답합니다.
                    // 목표 칸은 실제 타격 칸입니다(끌어오기면 Core가 준 도착 칸).
                    TryTarget(shape.DeliveryCell, shape.Direction, shape.AffectedCells.Span, mapper, pose.Position, out target);
                    direction = mapper.RotationToWorld(shape.Direction);
                }
                else
                {
                    direction = pose.Rotation;
                    target = pose.Position + (pose.Rotation * Vector3.forward);
                }
                ICombatAttackAnimation animation = CreateAnimation(context, settings, mapper, pool, pose, target, direction, shape, hit, cue);
                context.BeginAnimation(animation, completed, version);
            });
            return true;
        }

        /// <summary>코드 모션의 투사체 풀입니다. 투사체가 없으면(근접) null입니다. 자세를 빌리기 전에 부릅니다.</summary>
        protected abstract CombatProjectilePool PoolOf(ICombatAttackStyleContext context, CombatAttackPresentationData settings);

        /// <summary>코드 모션 애니메이션을 만듭니다. <paramref name="shape"/>가 null이면 정면 공격 모션입니다.</summary>
        protected abstract ICombatAttackAnimation CreateAnimation(ICombatAttackStyleContext context, CombatAttackPresentationData settings,
            FieldGridMapper mapper, CombatProjectilePool pool, ICombatAttackUnitPose pose, Vector3 target, Quaternion direction,
            CombatAttackShape shape, Action hit, Action cue);
    }
}
