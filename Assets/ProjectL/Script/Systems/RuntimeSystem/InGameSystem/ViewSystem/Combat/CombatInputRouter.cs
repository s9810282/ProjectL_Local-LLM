using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.Data;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 전투 입력 의도를 처리 순서대로 대상에 나눕니다(구조 개선 VR2, 설계 VR2 표).
    /// 포인터 입력의 대상은 <see cref="ResolvePointerTarget"/> 한 곳에서 정하고, 시작·턴 종료·드로우는 Gate로 직접 제출합니다.
    /// </summary>
    /// <remarks>
    /// 모드·대기·초안을 소유하지 않고 정책을 계산하지 않습니다. 사실은 판단이 필요할 때마다 공급자에게서 새로 읽으며,
    /// 외부 표시 콜백(경로 취소 등) 뒤에는 다시 읽습니다. 오류 경계·입력 연결 검사는 CombatViewSystem.Receive가 먼저 합니다.
    /// 입력마다 새 대리자를 만들지 않습니다.
    /// </remarks>
    internal sealed class CombatInputRouter
    {
        /// <summary>외부 입력 콜백의 종류입니다. 콜백마다 클로저를 만들지 않으려고 한 입구로 모읍니다.</summary>
        internal enum Request
        {
            PointerMoved,
            SelectPressed,
            SelectReleased,
            SelectCanceled,
            Cancel,
            EnterCombat,
            Draw,
            EndTurn,
            MoveMode,
            AttackMode,
            FocusLost,
            Return
        }

        /// <summary>포인터 입력을 받을 소유자입니다.</summary>
        internal enum PointerTarget
        {
            None = 0,
            Placement = 1,
            Attack = 2,
            Path = 3
        }

        private readonly CombatInteraction _interaction;
        private readonly CombatCommandGate _gate;
        // 인자는 선행 미처리 명령까지 읽을지 여부다(턴 종료 판단).
        private readonly Func<bool, CombatInputFacts> _readFacts;
        private CombatPathInput _path;

        internal CombatInputRouter(CombatInteraction interaction, CombatCommandGate gate, Func<bool, CombatInputFacts> readFacts)
        {
            _interaction = interaction ?? throw new ArgumentNullException(nameof(interaction));
            _gate = gate ?? throw new ArgumentNullException(nameof(gate));
            _readFacts = readFacts ?? throw new ArgumentNullException(nameof(readFacts));
        }

        /// <summary>
        /// 포인터 입력 대상의 유일한 판정식입니다. 실제 Layer를 먼저 보고, 배치는 Placement,
        /// 전투는 공격 모드면 Attack 아니면 Path입니다(모드가 없어도 전투 포인터는 경로 입력). 그 밖의 Layer는 None입니다.
        /// </summary>
        /// <remarks>
        /// RecordInputOnly의 전투 Layer는 모드와 관계없이 Attack입니다. 기록 모드는 Core 행위자·이동 응답이 없어 경로 지정이 시작되지 않고
        /// (W20: 검증 context 없는 경로 기록 없음), 공격 전 클릭으로 기록용 공격 행위자를 골라야 하기 때문입니다(설계 §8.1, 계획 §7.48).
        /// </remarks>
        internal static PointerTarget ResolvePointerTarget(InputLayer layer, bool attackActive, CombatExecutionMode executionMode)
        {
            if (layer == StageInputLayers.Deployment)
                return PointerTarget.Placement;
            if (layer == StageInputLayers.Combat)
            {
                if (attackActive || executionMode == CombatExecutionMode.RecordInputOnly)
                    return PointerTarget.Attack;
                return PointerTarget.Path;
            }

            return PointerTarget.None;
        }

        /// <summary>전투가 설치될 때 새 경로 입력을 연결하고, 정리될 때 null로 끊습니다. 소유자는 CombatViewSystem입니다.</summary>
        internal void SetPathInput(CombatPathInput path)
        {
            _path = path;
        }

        internal void Route(Request request, float screenX, float screenY, IReadOnlyList<CharacterData> characters)
        {
            switch (request)
            {
                case Request.FocusLost:
                    _path?.CancelInputs();
                    _interaction.CancelPresentation();
                    return;
                case Request.SelectCanceled:
                    // 표현만 정리한다. 명령을 기록하지 않으며 대기·종료 중에도 가능하다.
                    _path?.CancelInputs();
                    _interaction.SelectCanceled();
                    return;
                case Request.Cancel:
                    _interaction.Cancel();
                    _path?.Cancel();
                    return;
                case Request.Return:
                    _path?.RequestReturn();
                    return;
                case Request.EnterCombat:
                    // 성공했을 때만 대기 기록 뒤 Interaction 표시를 정리한다. 경로 취소는 하지 않는다.
                    if (_gate.SubmitPending(_readFacts(false), CombatViewCommandData.EnterCombat()))
                        _interaction.CancelPresentation();
                    return;
                case Request.Draw:
                    // 일반 제출만 한다. 대기·표시 취소 없음.
                    _gate.TrySubmit(_readFacts(false), CombatViewCommandData.DrawCharacter());
                    return;
                case Request.EndTurn:
                    // 경로를 먼저 취소한다. 제출이 거절돼도 취소는 이미 끝났다. 취소가 표시 콜백을 부르므로 사실은 그 뒤에 읽는다.
                    _path?.CancelInputs();
                    if (_gate.SubmitPending(_readFacts(true), CombatViewCommandData.EndUnitTurn()))
                        _interaction.CancelPresentation();
                    return;
                case Request.MoveMode:
                    // Q: Interaction 표시를 정리한 뒤 경로 입력이 새 사실로 지정 시작 여부를 판단한다.
                    _interaction.CancelPresentation();
                    _path?.BeginPlanning();
                    return;
                case Request.AttackMode:
                    // E: 경로를 취소한 뒤 최신 사실로 공격 모드 진입을 시도한다.
                    _path?.CancelInputs();
                    _interaction.AttackModeRequested(_readFacts(false));
                    return;
                case Request.PointerMoved:
                case Request.SelectPressed:
                case Request.SelectReleased:
                    RoutePointer(request, screenX, screenY, characters);
                    return;
                default:
                    throw new ArgumentOutOfRangeException(nameof(request), request, null);
            }
        }

        private void RoutePointer(Request request, float screenX, float screenY, IReadOnlyList<CharacterData> characters)
        {
            CombatInputFacts facts = _readFacts(false);
            PointerTarget target = ResolvePointerTarget(facts.CurrentLayer, _interaction.IsAttackActive, facts.ExecutionMode);
            if (target == PointerTarget.Path)
            {
                if (request == Request.PointerMoved)
                    _path?.PointerMoved(screenX, screenY);
                else if (request == Request.SelectPressed)
                    _path?.SelectPressed(screenX, screenY);
                else
                    _path?.SelectReleased(screenX, screenY);
                return;
            }

            if (request == Request.PointerMoved)
                _interaction.PointerMoved(target, facts, characters, screenX, screenY);
            else if (request == Request.SelectPressed)
                _interaction.SelectPressed(target, facts, characters, screenX, screenY);
            else
                _interaction.SelectReleased(target, facts, characters, screenX, screenY);
        }
    }
}
