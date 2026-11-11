using System;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common
{
    /// <summary>
    /// 씬과 독립적인 요청 소유자(회복·증강)가 함께 쓰는 실행 수명입니다. 제출 중·예약·실행 중·정지·종료 사실과
    /// 실행 시작·끝 앞부분, 사용 가능 검사, 실패 보고, 종료 때 Runner 비우기를 맡습니다(스테이지 선택 리팩토링 설계 §2.7 C-12).
    /// </summary>
    /// <remarks>
    /// 상속이 아니라 조합입니다. 보관 번호와 그 대조, 예약 조건, 결과 확인 방식(회복은 스스로 확인·입력 제거, 증강은 화면이 확인),
    /// 회복만의 취소·입력 정지는 각 소유자에 남깁니다. 상점은 씬 수명 컨트롤러가 직접 제출하는 꼴이라 쓰지 않습니다.
    /// </remarks>
    internal sealed class ViewRequestExecution
    {
        private readonly string _name;
        private readonly string _kind;
        private readonly RuntimeFaultBoundary _faults;
        private IJobRunner _runner;

        /// <param name="name">실패 문구 머리입니다(예: <c>RecoveryRequest</c>).</param>
        /// <param name="kind">실패 문구에 넣는 요청 종류입니다(예: 회복).</param>
        internal ViewRequestExecution(string name, string kind, RuntimeFaultBoundary faults)
        {
            _name = string.IsNullOrEmpty(name) ? throw new ArgumentException("소유자 이름이 필요합니다.", nameof(name)) : name;
            _kind = string.IsNullOrEmpty(kind) ? throw new ArgumentException("요청 종류가 필요합니다.", nameof(kind)) : kind;
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
        }

        internal bool IsSubmitting { get; private set; }
        internal bool IsReserved { get; private set; }
        internal bool IsExecuting { get; private set; }
        internal bool IsStopped { get; private set; }
        internal bool IsShutdown { get; private set; }

        /// <summary>새 예약을 받을 수 있습니다. 정지·종료·오류 정지가 아니고 이미 예약하지 않았습니다. 보관 번호 조건은 소유자가 더합니다.</summary>
        internal bool CanReserve => !IsStopped && !IsShutdown && !_faults.IsFaulted && !IsReserved;

        internal void BindRunner(IJobRunner runner)
        {
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        }

        /// <summary>서비스를 받았고 정지·종료·오류 정지가 아닐 때만 통과합니다.</summary>
        internal void RequireAvailable()
        {
            if (_runner == null || IsStopped || IsShutdown || _faults.IsFaulted)
                throw new InvalidOperationException($"연결되고 정지하지 않은 {_kind} 요청 소유자가 필요합니다.");
        }

        /// <summary>제출 중 표시를 켠 채 <paramref name="write"/>를 부릅니다. 그 사이에도 "기다리는 요청 있음"으로 보이게 합니다.</summary>
        internal int Submit(Func<int> write)
        {
            if (write == null) throw new ArgumentNullException(nameof(write));
            IsSubmitting = true;
            try
            {
                return write();
            }
            finally
            {
                IsSubmitting = false;
            }
        }

        internal void Reserve() => IsReserved = true;

        internal void EndReservation() => IsReserved = false;

        /// <summary>예약한 그 번호(<paramref name="pending"/>)의 실행만 시작합니다.</summary>
        internal bool TryBegin(int number, int pending)
        {
            if (IsShutdown || !IsReserved || IsExecuting || pending != number) return false;
            IsExecuting = true;
            return true;
        }

        /// <summary>
        /// 실행 끝의 앞부분입니다. 실행 오류를 먼저 보고하고, 예약한 그 번호의 끝이면 실행 중 표시를 끄고 참입니다.
        /// 다른 번호의 끝이 오면 정지하고 거짓입니다(종료 중이면 조용히 거짓). 예약 해제 시점은 소유자가 정합니다.
        /// </summary>
        internal bool TryFinish(int number, int pending, Exception error)
        {
            // 원인을 먼저 고정합니다. fault 정리가 재진입해도 아직 실행 기록을 놓지 않습니다.
            if (error != null) _faults.Report(error);
            if (!IsReserved || pending != number)
            {
                if (!IsShutdown) Fail($"보관한 예약과 다른 {_kind} 실행 종료가 도착했습니다.");
                return false;
            }
            IsExecuting = false;
            return true;
        }

        /// <summary>새 제출·예약을 막습니다.</summary>
        internal void Stop() => IsStopped = true;

        internal void Fail(string message)
        {
            IsStopped = true;
            _faults.Report(new InvalidOperationException($"[{_name}] " + message));
        }

        /// <summary>종료를 표시합니다. 이미 종료했으면 거짓입니다.</summary>
        internal bool BeginShutdown()
        {
            if (IsShutdown) return false;
            IsShutdown = true;
            return true;
        }

        /// <summary>
        /// 실행 전 대기 예약을 놓습니다. 루트는 View 다음 Global을 종료하므로, 통지 없이 버려지는 대기 예약을 놓으려면
        /// 같은 Runner의 공개 Clear를 먼저 마쳐야 합니다. 놓았으면 참입니다. 실행 중이거나 예약이 없으면 아무것도 하지 않습니다.
        /// 일반 취소에는 쓰지 않습니다.
        /// </summary>
        internal bool TryReleaseQueuedReservation()
        {
            if (IsExecuting || !IsReserved) return false;
            _runner?.Clear();
            if (_runner != null && !_runner.IsIdle) return false;
            IsReserved = false;
            return true;
        }
    }
}
