using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.StageView;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage
{
    /// <summary>
    /// 이번 Stage 방문에서 맵 명령으로 건 전투 진입 시도 하나를 기억합니다. 여러 Tick을 기다리는 일은 이것뿐입니다.
    /// 입력 정리와 Core 결과 게시는 대신하지 않고, 진입 요청의 수명과 처리 번호를 읽어 끝났는지만 답합니다(옛 노드 진입 Flow의 전투 갈래).
    /// </summary>
    internal sealed class StageEntryAttempts : IStageEntryAttemptRead
    {
        private int _number;
        private CombatEntryRequest _request;
        // 요청이 이미 등록(Queued 이후)된 채 받았으면 진입 Job의 배출까지 기다립니다.
        private bool _jobExpected;

        internal bool HasAttempt => _request != null;

        public bool TryGetCombatEntry(out int mapCommandNumber, out CombatEntryRequest request)
        {
            mapCommandNumber = _request == null ? 0 : _number;
            request = _request;
            return _request != null;
        }

        internal void Begin(int mapCommandNumber, CombatEntryRequest request)
        {
            _number = mapCommandNumber;
            _request = request ?? throw new ArgumentNullException(nameof(request));
            _jobExpected = !request.IsAwaitingRegistration;
        }

        /// <summary>시도를 닫습니다. 방문을 새로 시작할 때도 부릅니다.</summary>
        internal void Clear()
        {
            _number = 0;
            _request = null;
            _jobExpected = false;
        }

        /// <summary>지금 시도가 끝났는지 답합니다. 상태를 바꾸지 않습니다.</summary>
        internal StageCombatEntryState Check(int handledNumber, IStageViewBufferReadSink input, out string problem)
        {
            problem = null;
            CombatEntryRequest request = _request;
            if (request == null) return StageCombatEntryState.None;
            if (request.IsCancelled)
            {
                // 실행 전 취소는 게임 결과가 아닙니다. 진입 Job의 배출과 View의 입력 정리까지 기다립니다.
                return (_jobExpected && !request.IsJobCompleted) || HasInput(input)
                    ? StageCombatEntryState.Waiting : StageCombatEntryState.Done;
            }
            // Running·Closed·Faulted는 전투 골격 요청에 이름 있는 질문이 없어 단계로 읽습니다(설계 B6, 보고만).
            if (request.Status == CombatEntryStatus.Closed || request.Status == CombatEntryStatus.Faulted)
            {
                problem = $"[StageEntry] 요청 {_number}: 전투 진입 요청이 완료·취소 확인 전에 종료됐습니다.";
                return StageCombatEntryState.Broken;
            }
            if (request.IsAwaitingRegistration || request.IsQueued || request.Status == CombatEntryStatus.Running ||
                !request.IsJobCompleted)
                return StageCombatEntryState.Waiting;
            if (handledNumber < _number)
            {
                problem = $"[StageEntry] 요청 {_number}: 전투 진입의 Core 처리 번호가 기록되지 않았습니다.";
                return StageCombatEntryState.Broken;
            }
            // 준비 수락만으로 입력을 다시 받지 않습니다. View 입력 삭제까지 같은 요청을 보존합니다.
            if ((!request.IsActive && !request.IsRejected) || HasInput(input)) return StageCombatEntryState.Waiting;
            return StageCombatEntryState.Done;
        }

        private bool HasInput(IStageViewBufferReadSink input)
        {
            for (int i = 0; i < input.Commands.Count; i++)
                if (input.Commands[i].Number == _number) return true;
            return false;
        }
    }
}
