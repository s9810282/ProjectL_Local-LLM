using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.LobbyView
{
    public enum LobbyDepartureStatus
    {
        Created,
        Running,
        Prepared,
        Rejected,
        Cancelled,
        Transitioning,
        Completed,
        Faulted
    }

    /// <summary>로비 출발 입력의 번호와 한 번 실행한 새 Run 호출의 결과를 보관합니다.</summary>
    /// <remarks>
    /// Core 처리 기록을 대신하지 않습니다. 외부는 질문(<see cref="IsPending"/>·<see cref="CanResubmit"/>·
    /// <see cref="IsRejected"/>·<see cref="IsFaulted"/>·<see cref="IsQueued"/>·<see cref="IsPrepared"/>·
    /// <see cref="IsTransitionSucceeded"/>)만 읽고, 단계는 View 취소와 실행 계층 Job만 바꿉니다.
    /// 단계를 바꿀 수 있는지는 <see cref="CanMove"/> 표 한 곳이 정합니다(로비 재구성 설계 §4.7).
    /// </remarks>
    public sealed class LobbyDepartureRequest : ILobbyDepartureState
    {
        public int Number { get; }
        public LobbyDepartureStatus Status { get; private set; }
        public RosterAcquireResult Result { get; private set; }

        /// <summary>
        /// 처리 중이라 출발·시설 입력을 막아야 하는지 답합니다. "끝나지 않은 단계"는 전이 표(<see cref="CanMove"/>)에서 구합니다.
        /// 진행 중인 단계는 모두 오류로 바뀔 수 있고 끝난 단계는 어디로도 바뀌지 않으므로, 오류로 갈 수 있으면 진행 중입니다
        /// (로비 리팩토링 2차 설계 N-46).
        /// </summary>
        public bool IsPending => CanMove(Status, LobbyDepartureStatus.Faulted);

        /// <summary>거절·취소로 끝나 다시 제출할 수 있는지 답합니다. View는 단계 대신 이 질문을 읽습니다.</summary>
        public bool CanResubmit => Status == LobbyDepartureStatus.Rejected || Status == LobbyDepartureStatus.Cancelled;

        /// <summary>새 Run 호출이 정상 거절됐는지 답합니다. 사유는 <see cref="Result"/>에 있습니다.</summary>
        public bool IsRejected => Status == LobbyDepartureStatus.Rejected;

        /// <summary>출발 처리가 오류로 끝났는지 답합니다.</summary>
        public bool IsFaulted => Status == LobbyDepartureStatus.Faulted;

        /// <summary>제출만 되고 새 Run 호출을 아직 시작하지 않았는지 답합니다. 실행 계층이 출발을 걸 때 읽습니다.</summary>
        public bool IsQueued => Status == LobbyDepartureStatus.Created;

        /// <summary>새 Run이 성공해 Stage 전환을 시작할 차례인지 답합니다.</summary>
        public bool IsPrepared => Status == LobbyDepartureStatus.Prepared;

        /// <summary>
        /// 새 Run이 성공한 출발이 Stage로 전환하는 중인지 답합니다. 로비를 떠나도 되는 대기 중 출발은 이것뿐입니다.
        /// 로딩 표현이 없는 경로의 동기 이탈 검사에서만 이 단계로 보입니다. 로딩 표현이 있으면 이탈 검사 때 이미 Completed입니다(N-79).
        /// </summary>
        /// <remarks>전환 단계는 성공 결과(Prepared)에서만 오므로 단계만 봅니다(로비 리팩토링 2차 설계 N-20).</remarks>
        public bool IsTransitionSucceeded => Status == LobbyDepartureStatus.Transitioning;

        internal LobbyDepartureRequest(int number)
        {
            if (number < 1)
                throw new ArgumentOutOfRangeException(nameof(number));
            Number = number;
        }

        /// <summary>시작 전(제출만 된) 요청만 취소합니다. 등록된 Job은 취소된 요청을 보고 Core를 부르지 않습니다.</summary>
        internal bool TryCancel()
        {
            if (!CanMove(Status, LobbyDepartureStatus.Cancelled))
                return false;
            Status = LobbyDepartureStatus.Cancelled;
            return true;
        }

        /// <summary>새 Run Job이 호출 직전에 부릅니다. 이미 취소됐으면 false입니다(설계 P5).</summary>
        internal bool TryStart()
        {
            if (Status == LobbyDepartureStatus.Cancelled)
                return false;
            MoveTo(LobbyDepartureStatus.Running);
            return true;
        }

        internal void PublishResult(RosterAcquireResult result)
        {
            if (result == null)
                throw new InvalidOperationException("로비 출발의 새 Run 호출 결과가 없습니다.");
            MoveTo(result.Succeeded ? LobbyDepartureStatus.Prepared : LobbyDepartureStatus.Rejected);
            Result = result;
        }

        /// <summary>새 Run이 성공한 요청(<c>Prepared</c>)만 전환을 시작합니다.</summary>
        internal void BeginTransition() => MoveTo(LobbyDepartureStatus.Transitioning);

        /// <summary>전환 요청이 수락됐음을 기록합니다. 대상 씬 로딩 완료를 대신하지 않습니다.</summary>
        internal void CompleteTransition() => MoveTo(LobbyDepartureStatus.Completed);

        /// <summary>
        /// 끝나지 않은 요청을 오류로 끝냅니다. 이미 끝난 요청은 그대로 둡니다.
        /// Job의 예외 처리에서 부르므로 던지지 않습니다(원래 예외를 가리지 않게).
        /// </summary>
        internal void Fail()
        {
            if (CanMove(Status, LobbyDepartureStatus.Faulted))
                Status = LobbyDepartureStatus.Faulted;
        }

        /// <summary>단계 전이 표입니다. 거절·취소·완료·오류는 끝난 단계라 더 바뀌지 않습니다.</summary>
        private static bool CanMove(LobbyDepartureStatus from, LobbyDepartureStatus to)
        {
            switch (from)
            {
                case LobbyDepartureStatus.Created:
                    return to == LobbyDepartureStatus.Running || to == LobbyDepartureStatus.Cancelled ||
                        to == LobbyDepartureStatus.Faulted;
                case LobbyDepartureStatus.Running:
                    return to == LobbyDepartureStatus.Prepared || to == LobbyDepartureStatus.Rejected ||
                        to == LobbyDepartureStatus.Faulted;
                case LobbyDepartureStatus.Prepared:
                    return to == LobbyDepartureStatus.Transitioning || to == LobbyDepartureStatus.Faulted;
                case LobbyDepartureStatus.Transitioning:
                    return to == LobbyDepartureStatus.Completed || to == LobbyDepartureStatus.Faulted;
                default:
                    return false;
            }
        }

        private void MoveTo(LobbyDepartureStatus next)
        {
            if (!CanMove(Status, next))
                throw new InvalidOperationException($"로비 출발 단계를 바꿀 수 없습니다. 지금: {Status}, 다음: {next}");
            Status = next;
        }
    }
}
