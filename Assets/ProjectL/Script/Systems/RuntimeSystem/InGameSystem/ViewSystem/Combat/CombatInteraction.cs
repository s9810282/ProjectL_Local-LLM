using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 배치 Drag와 공격 조준을 묶는 조합 객체입니다(구조 개선 VR3).
    /// <see cref="CombatInputRouter"/>가 고른 대상(<see cref="CombatInputRouter.PointerTarget"/>)에 그대로 위임하며 스스로 대상을 판정하지 않습니다.
    /// </summary>
    /// <remarks>
    /// 전환 순서: 공격 모드에 들어갈 수 있을 때만 배치 쪽 표시·요청을 먼저 정리하고 공격 쪽을 활성화합니다.
    /// 시작/턴 종료 제출과 대기는 Router와 <see cref="CombatCommandGate"/>가 맡습니다. Gate는 CombatViewSystem이 만들어 넘기고
    /// 여기서는 두 하위 객체에 전달만 합니다.
    /// </remarks>
    internal sealed class CombatInteraction
    {
        internal enum DragKind
        {
            None = 0,
            /// <summary>손패·필드의 아군 유닛을 칸에 놓거나 손패로 되돌립니다(배치 단계).</summary>
            Placement = 1,
        }

        internal enum TargetMode
        {
            None = 0,
            Attack = 2
        }

        private readonly PlacementDragInteraction _placement;
        private readonly AttackTargetingInteraction _attack;

        internal CombatInteraction(
            ICombatViewBufferWriteSink input,
            ICombatViewBufferReadSink requests,
            ICombatPointerPicker picker,
            ICombatInteractionFeedback feedback,
            CombatCommandGate gate)
        {
            if (gate == null)
                throw new ArgumentNullException(nameof(gate));

            _placement = new PlacementDragInteraction(input, requests, picker, feedback, gate);
            _attack = new AttackTargetingInteraction(input, requests, picker, feedback, gate);
        }

        /// <summary>공격 모드인지입니다. 입력 대상 판정이 읽습니다.</summary>
        internal bool IsAttackActive => _attack.IsActive;

        /// <summary>배치 Drag 상태입니다(읽기 전용, 배치 소유자 값).</summary>
        internal DragKind Drag => _placement.IsDragging ? DragKind.Placement : DragKind.None;

        /// <summary>조준 모드입니다(읽기 전용, 공격 소유자 값).</summary>
        internal TargetMode Mode => _attack.IsActive ? TargetMode.Attack : TargetMode.None;

        /// <summary>공격 범위 응답을 받았는지입니다(읽기 전용, 공격 소유자 값).</summary>
        internal bool HasRangeAnswer => _attack.HasRangeAnswer;

        #region 대상 지정 입력

        internal void PointerMoved(CombatInputRouter.PointerTarget target, in CombatInputFacts facts,
            IReadOnlyList<CharacterData> characters, float screenX, float screenY)
        {
            if (target == CombatInputRouter.PointerTarget.Placement)
                _placement.PointerMoved(facts, screenX, screenY);
            else if (target == CombatInputRouter.PointerTarget.Attack)
                _attack.PointerMoved(facts, characters, screenX, screenY);
        }

        internal void SelectPressed(CombatInputRouter.PointerTarget target, in CombatInputFacts facts,
            IReadOnlyList<CharacterData> characters, float screenX, float screenY)
        {
            if (target == CombatInputRouter.PointerTarget.Placement)
                _placement.SelectPressed(facts, characters, screenX, screenY);
            else if (target == CombatInputRouter.PointerTarget.Attack)
                _attack.SelectPressed(facts, characters, screenX, screenY);
        }

        internal void SelectReleased(CombatInputRouter.PointerTarget target, in CombatInputFacts facts,
            IReadOnlyList<CharacterData> characters, float screenX, float screenY)
        {
            if (target == CombatInputRouter.PointerTarget.Placement)
                _placement.SelectReleased(facts, screenX, screenY);
            else if (target == CombatInputRouter.PointerTarget.Attack)
                _attack.SelectReleased(facts, characters, screenX, screenY);
        }

        #endregion

        #region 정리·전환

        /// <summary>선택이 강제로 끝났습니다(Layer 전환·Unbind 등). Drag만 끝내고 명령은 기록하지 않습니다.</summary>
        internal void SelectCanceled()
        {
            _placement.Cancel();
        }

        /// <summary>취소 키: Drag 중이면 Drag를, 아니면 공격 모드를 끝냅니다. 대기는 지우지 않습니다.</summary>
        internal void Cancel()
        {
            if (_placement.IsDragging)
                _placement.Cancel();
            else
                _attack.Exit();
        }

        /// <summary>
        /// 공격 모드 진입: 들어갈 수 있을 때만 배치 쪽 표시·요청을 먼저 정리하고, 공격 쪽이 범위를 요청합니다.
        /// 들어갈 수 없으면 아무것도 바꾸지 않습니다.
        /// </summary>
        internal void AttackModeRequested(in CombatInputFacts facts)
        {
            if (!_attack.CanEnter(facts, out EntityId actor))
                return;

            _placement.CancelPresentation();
            _attack.Enter(facts, actor);
        }

        /// <summary>
        /// Core가 현재 요청에 답했습니다(facts의 <see cref="CombatInputFacts.PreviewAnswer"/>, 설계 §21.1).
        /// 요청 번호가 자기 것인 소유자만 반영합니다.
        /// </summary>
        internal void PreviewAnswered(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters)
        {
            if (facts.PreviewAnswer == null)
                return;

            if (_attack.PreviewAnswered(facts, characters))
                return;

            _placement.PreviewAnswered(facts.PreviewAnswer);
        }

        /// <summary>
        /// 결과 후처리: 각 소유자가 현재 사실과 맞지 않게 된 Drag·모드를 정리하고,
        /// 유효하게 남은 쪽만 새 번호로 다시 요청합니다(설계 §5.5·§7.1).
        /// </summary>
        internal void AfterResult(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters)
        {
            _placement.AfterResult(facts, characters);
            _attack.AfterResult(facts);
        }

        /// <summary>
        /// 표현만 정리합니다(Drag·고스트·원본 숨김·호버·모드·범위·자기 미리보기). 대기는 보존합니다.
        /// 정상 종료의 입력 차단과 Layer 전환에서 씁니다. 정리할 것이 없으면 아무것도 하지 않습니다.
        /// </summary>
        internal void CancelPresentation()
        {
            _placement.CancelPresentation();
            _attack.Exit();
        }

        /// <summary>
        /// 오류 정지·씬 연결 해제: 표현과 기록 모드 행위자를 정리합니다. 명령 Buffer는 바꾸지 않습니다.
        /// 시작/턴 종료 대기는 Gate 소유자(CombatViewSystem)가 따로 지웁니다.
        /// </summary>
        internal void Stop()
        {
            CancelPresentation();
            _attack.ClearRecordActor();
        }

        #endregion
    }
}
