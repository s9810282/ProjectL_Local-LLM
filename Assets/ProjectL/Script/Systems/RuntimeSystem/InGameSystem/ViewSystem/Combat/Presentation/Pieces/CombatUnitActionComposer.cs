using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 특성 발동 유닛 행동을 조립 항목으로 붙이는 곳입니다(유닛 행동 설계 AA5). 입구는 발동 사실이 온 자리마다 하나입니다(설계 §2).
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item>공격 반응(가로채기 = 대신 맞기, 설계 AA6): <see cref="AddGuards"/>가 공격 조립 맨 앞에 수호자·보호 대상의 나가기를 넣고 공격 뼈대가 시작할 시점을 돌려주며,
    /// <see cref="AddReturns"/>가 뼈대의 맞는 순간 뒤에 대기 → 돌아오기를 붙입니다. 공격 실행 하나가 행동 유닛의 서는 자리를 빌리고 끝날 때 반환합니다.</item>
    /// <item>사건 목록 안 발동(설계 AA7): <see cref="ComposeMoment"/>가 행동 유닛의 유닛 실행 하나(나가기·돌기 → 머물기 → 돌아오기)를 만듭니다.</item>
    /// <item>위치 이동(이펙트 끝 규칙 설계 EL11): <see cref="ComposeRelocate"/>가 미뤄 둔 사실 자리로 한 번 가는 유닛 실행을 만듭니다(돌아오지 않음).</item>
    /// </list>
    /// 일반 발동 자리는 조각 시작 시점, 가로채기 기준점은 모든 나가기 전의 서 있는 자리(<c>standingOf</c>)로 정합니다(자리 계산 한 곳 <see cref="CombatUnitActionPlaces"/>).
    /// 발동 줄의 큐는 이 조립이 아니라 부르는 쪽이 붙입니다(큐 창구가 그쪽에 있음).
    /// </remarks>
    internal static class CombatUnitActionComposer
    {
        /// <summary>대신 맞기 관계와 그 발동 줄의 행동 하나입니다. Actor 역할로 실제 행동 유닛을 구분합니다.</summary>
        internal readonly struct Guard
        {
            internal Guard(EntityId guardian, EntityId protectedUnit, CombatUnitActionData action)
            {
                if (guardian.IsEmpty) throw new ArgumentException("수호자가 비어 있습니다.", nameof(guardian));
                if (protectedUnit.IsEmpty) throw new ArgumentException("지켜진 유닛이 비어 있습니다.", nameof(protectedUnit));
                Guardian = guardian;
                ProtectedUnit = protectedUnit;
                Action = action ?? throw new ArgumentNullException(nameof(action));
            }

            internal EntityId Guardian { get; }
            internal EntityId ProtectedUnit { get; }
            internal CombatUnitActionData Action { get; }
            internal EntityId Unit => Action.Actor == CombatUnitActionActor.Source ? Guardian : ProtectedUnit;
        }

        /// <summary>
        /// 공격 조립에 가로채기 행동의 나가기를 넣고(행동 유닛마다 서는 자리를 빌림) 공격 뼈대가 시작할 시점을 돌려줍니다(계획 TP-14).
        /// 서는 자리를 빌릴 수 없는 유닛은 빼고(설계 P7), 넣은 행동을 <paramref name="added"/>로 돌려줍니다. 하나도 없으면 실행 시작입니다.
        /// 참여자가 여럿이면 뼈대는 모든 나가기가 실제로 끝난 뒤 시작합니다.
        /// </summary>
        /// <param name="attacker">공격자입니다. 지켜진 유닛 앞(공격자 쪽) 자리와 보는 쪽에 씁니다.</param>
        /// <param name="standingOf">유닛이 지금 서 있는 자리(발밑)입니다. 필드 위에 없으면 null입니다. 나가기를 조립할 때 기준점을 포착합니다.</param>
        /// <param name="canStand">그 유닛의 서는 자리를 지금 빌릴 수 있는지입니다(<see cref="CombatUnitPresentationWindow.CanBeginStand"/>).</param>
        /// <param name="hopHeight">뛰기 높이입니다(걸음 모양 설정, 계획 TP-21).</param>
        internal static CombatPieceStart AddGuards(CombatComposition composition, IReadOnlyList<Guard> guards, EntityId attacker,
            Func<EntityId, Pose?> standingOf, Func<EntityId, bool> canStand, float hopHeight, out IReadOnlyList<Guard> added)
        {
            if (composition == null) throw new ArgumentNullException(nameof(composition));
            if (guards == null) throw new ArgumentNullException(nameof(guards));
            if (standingOf == null) throw new ArgumentNullException(nameof(standingOf));
            if (canStand == null) throw new ArgumentNullException(nameof(canStand));
            for (int i = 0; i < guards.Count; i++)
                if (guards[i].Action.Place == CombatUnitActionPlace.Relocate)
                    throw new ArgumentException("대신 맞기에는 위치 이동(Relocate) 행동을 넣지 않습니다(부르는 쪽이 거름).", nameof(guards));
            var kept = new List<Guard>(guards.Count);
            var outMoves = new List<CombatStandMovePiece>(guards.Count);
            for (int i = 0; i < guards.Count; i++)
            {
                Guard guard = guards[i];
                // 같은 유닛의 서는 자리는 한 번만 빌립니다. 여러 가로채기에 겹치면 첫 행동을 유지합니다.
                if (HasUnit(kept, guard.Unit) || !canStand(guard.Unit)) continue;
                composition.Lease(guard.Unit, CombatPieceChannel.Stand);
                CombatUnitActionData action = guard.Action;
                EntityId target = action.Actor == CombatUnitActionActor.Source ? guard.ProtectedUnit : guard.Guardian;
                // 모든 행동을 시작하기 전에 포착합니다. 보호 대상이 먼저 물러나도 수호자 목적지는 그대로입니다.
                Vector3? targetPosition = standingOf(action.Place == CombatUnitActionPlace.GuardFront ? guard.ProtectedUnit : target)?.position;
                Vector3? attackerPosition = standingOf(attacker)?.position;
                var move = new CombatStandMovePiece(guard.Unit,
                        (stand, mapper) => CombatUnitActionPlaces.EndPose(action.Place, action.Distance, action.Face, HomeOf(stand),
                            targetPosition, attackerPosition, mapper),
                        action.OutSeconds, action.Look, hopHeight);
                composition.Add(move, CombatPieceStart.AtStart());
                outMoves.Add(move);
                kept.Add(guard);
            }
            added = kept;
            if (outMoves.Count == 0) return CombatPieceStart.AtStart();
            int completed = composition.Add(new OutMovesWaitPiece(outMoves), CombatPieceStart.AtStart());
            return CombatPieceStart.After(completed, CombatPieceSignal.End);
        }

        /// <summary>
        /// 공격 뼈대의 맞는 순간(<paramref name="hitItem"/>의 <see cref="CombatPieceSignal.Hit"/>) 뒤에 행동 유닛마다 대기 → 돌아오기를 붙입니다(계획 TP-13·TP-15).
        /// 수호자와 행동 유닛의 해당 출처 후속이 모두 끝날 때까지 기다리고, 돌아오기는 빌릴 때 자리로 갑니다. 죽은 유닛은 서는 자리 되돌리기가 자리를 쓰지 않습니다(설계 P8).
        /// 맞는 순간 신호가 끝내 오지 않으면 둘 다 시작하지 않고, 실행이 끝날 때 대여 반환이 빌릴 때 자리로 되돌립니다(계획 TP-16).
        /// </summary>
        /// <param name="guards"><see cref="AddGuards"/>가 넣은 가로채기 행동입니다.</param>
        /// <param name="source">그 공격 출처 사건입니다(결과 안의 같은 인스턴스).</param>
        internal static void AddReturns(CombatComposition composition, IReadOnlyList<Guard> guards, int hitItem, CombatEvent source,
            ICombatSourceSettlement settlement, float hopHeight)
        {
            if (composition == null) throw new ArgumentNullException(nameof(composition));
            if (guards == null) throw new ArgumentNullException(nameof(guards));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (settlement == null) throw new ArgumentNullException(nameof(settlement));
            for (int i = 0; i < guards.Count; i++)
            {
                Guard guard = guards[i];
                int wait = composition.Add(new CombatUnitSettleWaitPiece(guard.Guardian, source, settlement),
                    CombatPieceStart.After(hitItem, CombatPieceSignal.Hit));
                if (guard.Unit != guard.Guardian)
                    wait = composition.Add(new CombatUnitSettleWaitPiece(guard.Unit, source, settlement),
                        CombatPieceStart.After(wait, CombatPieceSignal.End));
                composition.Add(new CombatStandMovePiece(guard.Unit, (stand, mapper) => HomeOf(stand), guard.Action.ReturnSeconds,
                    guard.Action.Look, hopHeight), CombatPieceStart.After(wait, CombatPieceSignal.End));
            }
        }

        /// <summary>
        /// 사건 목록 안 발동의 유닛 행동 하나입니다(설계 AA7): 행동 유닛의 유닛 실행으로 나가기(제자리면 돌기) → 머물기(<see cref="CombatUnitActionData.HoldSeconds"/>) → 돌아오기.
        /// 사건 완료와 묶지 않는 일반 완료이고, 서는 자리를 빌립니다.
        /// </summary>
        /// <param name="place">갈 자리입니다. 줄 값이 이 입구에서 뜻이 없으면(가로채기 없는 지켜진 유닛 앞) 부르는 쪽이 경고 뒤 제자리를 넘깁니다(설계 P10).</param>
        /// <param name="target">대상입니다(보기·대상 쪽). 없으면 비어 있습니다.</param>
        internal static CombatComposition ComposeMoment(EntityId actor, CombatUnitActionData action, CombatUnitActionPlace place, EntityId target,
            Func<EntityId, Pose?> standingOf, float hopHeight)
        {
            if (actor.IsEmpty) throw new ArgumentException("행동 유닛이 비어 있습니다.", nameof(actor));
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (standingOf == null) throw new ArgumentNullException(nameof(standingOf));
            if (place == CombatUnitActionPlace.Relocate)
                throw new ArgumentException("위치 이동(Relocate)은 ComposeRelocate로 조립합니다.", nameof(place));
            var composition = CombatComposition.ForUnit(actor, isDeath: false, CombatPieceCompletion.General)
                .Lease(actor, CombatPieceChannel.Stand);
            int moveOut = composition.Add(new CombatStandMovePiece(actor,
                    (stand, mapper) => CombatUnitActionPlaces.EndPose(place, action.Distance, action.Face, HomeOf(stand),
                        target.IsEmpty ? null : standingOf(target)?.position, null, mapper),
                    action.OutSeconds, action.Look, hopHeight),
                CombatPieceStart.AtStart(), waits: true);
            composition.Add(new CombatStandMovePiece(actor, (stand, mapper) => HomeOf(stand), action.ReturnSeconds, action.Look, hopHeight),
                CombatPieceStart.After(moveOut, CombatPieceSignal.End, action.HoldSeconds), waits: true);
            return composition;
        }

        /// <summary>
        /// 위치 이동 하나입니다(이펙트 끝 규칙 설계 EL11, 계획 Ef.5): 행동 유닛의 유닛 실행으로 서는 자리를 빌려 <paramref name="destination"/>까지 한 번 옮기고 끝납니다.
        /// 돌아오지 않습니다. 실행이 끝나면 서는 자리 대여가 옛 자리로 되돌리고, 그 바로 뒤 완료 콜백이 미뤄 둔 사실을 반영해 새 자리로 그립니다(같은 프레임).
        /// 일반 완료이고 사건 완료와 묶지 않습니다.
        /// </summary>
        /// <param name="destination">미뤄 둔 사실대로 서 있을 자세(위치·사실 방향)입니다. 도착 방향이 사실 방향이라 반영할 때 튀지 않습니다(계획 TP-E5).</param>
        internal static CombatComposition ComposeRelocate(EntityId actor, CombatUnitActionData action, Pose destination, float hopHeight)
        {
            if (actor.IsEmpty) throw new ArgumentException("행동 유닛이 비어 있습니다.", nameof(actor));
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (action.Place != CombatUnitActionPlace.Relocate)
                throw new ArgumentException($"위치 이동 행동이 아닙니다: {action.Place}", nameof(action));
            var composition = CombatComposition.ForUnit(actor, isDeath: false, CombatPieceCompletion.General)
                .Lease(actor, CombatPieceChannel.Stand);
            composition.Add(new CombatStandMovePiece(actor, (stand, mapper) => destination, action.OutSeconds, action.Look, hopHeight),
                CombatPieceStart.AtStart(), waits: true);
            return composition;
        }

        private static Pose HomeOf(ICombatUnitStand stand) => new(stand.HomePosition, stand.HomeRotation);

        private static bool HasUnit(List<Guard> guards, EntityId unit)
        {
            for (int i = 0; i < guards.Count; i++)
                if (guards[i].Unit == unit) return true;
            return false;
        }

        /// <summary>설정 시간 추정 대신 동시에 나간 모든 유닛의 실제 이동 완료를 기다립니다.</summary>
        private sealed class OutMovesWaitPiece : ICombatPresentationPiece
        {
            private readonly IReadOnlyList<CombatStandMovePiece> _moves;
            public bool IsEnded { get; private set; }

            internal OutMovesWaitPiece(IReadOnlyList<CombatStandMovePiece> moves)
                => _moves = moves ?? throw new ArgumentNullException(nameof(moves));

            public void Begin(CombatPieceContext context)
            {
                if (context == null) throw new ArgumentNullException(nameof(context));
                IsEnded = false;
            }

            public void Tick(float deltaTime)
            {
                for (int i = 0; i < _moves.Count; i++)
                    if (!_moves[i].IsEnded) return;
                IsEnded = true;
            }

            public void Cancel()
            {
            }
        }
    }
}
