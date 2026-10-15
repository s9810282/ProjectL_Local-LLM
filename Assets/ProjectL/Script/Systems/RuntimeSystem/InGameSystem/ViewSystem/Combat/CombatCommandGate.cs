using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 명령 제출의 마지막 관문과 시작/턴 종료 대기(pending)의 유일한 원본입니다(구조 개선 설계 VR1, §2.3).
    /// 제출 직전에 <see cref="CombatInputPolicy.CanSubmit"/>를 현재 대기 값으로 다시 확인하고,
    /// 시작/턴 종료 제출이 번호를 받으면 그 번호를 대기로 기록합니다.
    /// </summary>
    /// <remarks>
    /// CombatViewSystem이 씬 연결(Attach) 때 만들고, 결과를 그린 뒤 <see cref="Resolve"/>, 오류 정지·Detach 때 <see cref="Clear"/>합니다.
    /// 정상 종료의 새 제출 정지는 대기를 지우지 않습니다. 표시 정리·모드·경로 context 검사·결과 재생은 맡지 않으며,
    /// Move/Return 명령은 Core context 검사 경로를 따로 쓰므로 이 관문을 거치지 않습니다.
    /// </remarks>
    internal sealed class CombatCommandGate
    {
        private readonly ICombatViewBufferWriteSink _input;
        private PendingInputCommand? _pending;

        internal CombatCommandGate(ICombatViewBufferWriteSink input)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
        }

        /// <summary>시작/턴 종료 결과를 기다리는 중인지입니다.</summary>
        internal bool HasPending => _pending.HasValue;

        /// <summary>기다리는 명령입니다. 없으면 null입니다.</summary>
        internal PendingInputCommand? Pending => _pending;

        /// <summary>
        /// 명령 하나를 지금 기록해도 되면 입력 Buffer에 제출하고 번호를 돌려줍니다. 거부하면 0입니다(설계 §7.1).
        /// facts가 입력 처리 시작 시점의 사본이어도, 대기 여부는 이 관문의 현재 값으로 확인합니다.
        /// </summary>
        internal int TrySubmit(in CombatInputFacts facts, CombatViewCommandData command)
        {
            if (!CombatInputPolicy.CanSubmit(facts, HasPending, command.Kind))
                return 0;

            return _input.Submit(command);
        }

        /// <summary>
        /// 시작/턴 종료를 제출하고, 번호를 받았을 때만 대기를 기록합니다(설계 §7.2).
        /// 표시 정리는 호출하는 쪽이 합니다. 기록했으면 true입니다.
        /// </summary>
        internal bool SubmitPending(in CombatInputFacts facts, CombatViewCommandData command)
        {
            int number = TrySubmit(facts, command);
            if (number == 0)
                return false;

            _pending = new PendingInputCommand(command.Kind, number);
            return true;
        }

        /// <summary>
        /// 결과를 그린 뒤 부릅니다. 명령 처리 결과이고 번호가 대기와 같을 때만 수락·거절 모두 대기를 풉니다.
        /// 다른 번호나 자동 진행 결과는 대기를 유지합니다.
        /// </summary>
        internal void Resolve(CombatResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            if (_pending.HasValue
                && result.Kind == CombatProcessingKind.Command
                && result.CommandNumber == _pending.Value.Number)
                _pending = null;
        }

        /// <summary>오류 정지·씬 연결 해제 때 로컬 대기를 끝냅니다. 명령 Buffer는 바꾸지 않습니다.</summary>
        internal void Clear()
        {
            _pending = null;
        }
    }
}
