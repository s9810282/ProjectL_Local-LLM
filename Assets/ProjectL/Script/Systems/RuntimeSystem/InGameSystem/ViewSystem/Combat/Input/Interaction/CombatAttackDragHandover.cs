using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction
{
    /// <summary>
    /// 공격 모드에서 시작한 Drag를 이동 경로로 넘기는 입력 소유자입니다(View 2라운드 W5-5).
    /// 몸짓(<see cref="CombatAttackDragGesture"/>)은 누른 칸과 release 소유권을, 이 클래스는 "언제 넘기나"를 맡습니다.
    /// 받는 쪽(경로 입력)은 좁은 계약 <see cref="ICombatAttackDragTarget"/>으로만 봅니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 포인터 처리 본문은 옛 입력 기능 묶음 <c>TryHandleAttackDragPointer</c>를 옮긴 것입니다. 옛 "경로 참조 있음·같은 참조" 검사는
    /// 받는 쪽 연결 검사(<see cref="IsTargetAttached"/>) 하나로 바꿨습니다(경로 정지 = 연결 끊김, View 2라운드 TP-5).
    /// 받는 쪽 없이 만든 조립(배치·공격만 검증)에서는 넘기지 않습니다. 콜백마다 받는 쪽 연결뿐 아니라 몸짓 세대도 다시 봅니다.
    /// </para>
    /// <para>
    /// 입력 기능 묶음의 공통 수명에서는 목록 맨 앞이며 포인터 대상은 없습니다(Router가 포인터를 <see cref="TryHandlePointer"/>로 먼저 넘김).
    /// 감시 단계에서 낡은 후보를 무효화하고, 결과 전·표현 정리·입력 정리·정지·강제 종료와 전경 전환(<c>BeginForeground</c>)에서 몸짓을 무효화합니다.
    /// </para>
    /// </remarks>
    internal sealed class CombatAttackDragHandover
    {
        private readonly CombatAttackDragGesture _gesture;
        // 넘겨받는 쪽입니다. 없으면(경로 없는 조립) 넘기지 않습니다.
        private readonly ICombatAttackDragTarget _target;
        private readonly AttackTargetingInteraction _attack;

        /// <param name="target">넘겨받는 쪽입니다. 없으면 null입니다.</param>
        internal CombatAttackDragHandover(CombatAttackDragGesture gesture, ICombatAttackDragTarget target, AttackTargetingInteraction attack)
        {
            _gesture = gesture ?? throw new ArgumentNullException(nameof(gesture));
            _target = target;
            _attack = attack ?? throw new ArgumentNullException(nameof(attack));
            InputOwner = new OwnerAdapter(this);
        }

        /// <summary>입력 기능 묶음의 공통 수명에 참여하는 창구입니다(View 2라운드 W5-1).</summary>
        internal CombatInputOwner InputOwner { get; }

        /// <summary>넘겨받는 쪽이 있고 연결돼 있는지입니다.</summary>
        private bool IsTargetAttached => _target != null && !_target.IsDetached;

        /// <summary>몸짓을 무효화합니다(세대를 올리고, 진행 중이면 release까지 막음). 의도 입구(복귀·공격 끄기·이동 모드 끝·공격 진입)가 부릅니다.</summary>
        internal void Invalidate() => _gesture.Invalidate();

        /// <summary>새 공격 Drag가 소유한 입력만 먼저 처리합니다. 클릭과 소유권 없는 release는 기존 배분으로 보냅니다(false).</summary>
        internal bool TryHandlePointer(CombatInputRouter.Request request, float screenX, float screenY, bool blockedByUi)
        {
            if (request == CombatInputRouter.Request.SelectPressed)
            {
                _gesture.BeginPress(null);
                _gesture.TryGetCandidate(out _, out int pressGeneration);
                if (!blockedByUi && IsTargetAttached && _target.TryGetAttackDragOrigin(screenX, screenY, out var pressed))
                {
                    _gesture.TryGetCandidate(out _, out int currentGeneration);
                    if (currentGeneration == pressGeneration && IsTargetAttached)
                        _gesture.BeginPress(pressed);
                }
                return false;
            }

            InvalidateStaleCandidate(out var origin, out int generation);

            if (request == CombatInputRouter.Request.SelectReleased)
            {
                var route = _gesture.Release();
                if (route == CombatAttackDragGesture.ReleaseRoute.PassThrough)
                    return false;
                if (route == CombatAttackDragGesture.ReleaseRoute.ForwardToPath && IsTargetAttached)
                {
                    if (blockedByUi) _target.CancelInputs();
                    else _target.SelectReleased(screenX, screenY);
                }
                return true;
            }

            if (request != CombatInputRouter.Request.PointerMoved || !_gesture.TryPromote(screenX, screenY, blockedByUi))
                return false;

            // release를 먼저 취득한 뒤 공격을 정리합니다. 콜백마다 받는 쪽 연결뿐 아니라 제스처 세대도 다시 봅니다.
            _attack.Exit();
            if (!_gesture.Owns(generation))
                return true;
            if (!IsTargetAttached
                || !_target.IsAttackDragOriginCurrent(origin, CombatInputRouter.PointerTarget.Path)
                || !_target.TryBeginTransferredDrag(origin))
            {
                if (_gesture.Owns(generation))
                    _gesture.Invalidate();
                return true;
            }
            if (_gesture.Owns(generation) && IsTargetAttached)
                _target.PointerMoved(screenX, screenY);
            return true;
        }

        /// <summary>
        /// 후보가 지금 공격 대상 사실과 맞지 않으면(받는 쪽이 없거나 끊김 포함) 무효화합니다. 포인터 처리와 매 Tick 감시가 이 규칙 하나를 씁니다.
        /// 출발점과 세대는 무효화 전의 것을 줍니다. 후보가 아니면 출발점 값은 의미가 없습니다(쓰는 쪽은 후보를 승격한 뒤에만 씀).
        /// </summary>
        private void InvalidateStaleCandidate(out CombatAttackDragGesture.Origin origin, out int generation)
        {
            if (_gesture.TryGetCandidate(out origin, out generation)
                && (!IsTargetAttached || !_target.IsAttackDragOriginCurrent(origin, CombatInputRouter.PointerTarget.Attack)))
                _gesture.Invalidate();
        }

        /// <summary>
        /// 공통 수명의 단계마다 넘겨받기의 어느 메서드인지만 답합니다. 포인터 대상은 없고, 입력 정리·정지는 기본(= 표현 정리 = 무효화)입니다.
        /// </summary>
        private sealed class OwnerAdapter : CombatInputOwner
        {
            private readonly CombatAttackDragHandover _handover;

            internal OwnerAdapter(CombatAttackDragHandover handover) => _handover = handover;

            internal override void Poll(CombatPreviewResult latestPreview) => _handover.InvalidateStaleCandidate(out _, out _);

            internal override void SelectCanceled() => _handover.Invalidate();

            internal override void BeforeResult() => _handover.Invalidate();

            internal override void CancelPresentation() => _handover.Invalidate();
        }
    }
}
