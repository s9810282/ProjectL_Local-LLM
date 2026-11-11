using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Recovery;
using ProjectL.Systems.RuntimeSystem.BufferSystems.RecoveryView;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
using ProjectL.Systems.ServiceKeys.Runtime;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Recovery
{
    /// <summary>씬과 독립적으로 회복 요청 한 건의 제출·실행·결과 소비 수명을 소유합니다.</summary>
    internal sealed class RecoveryRequestLifetime : IRecoveryRequestSubmitter, IRecoveryRequestExecution,
        IRecoveryRequestStatus, ISystemPostBindService, ISystemTick, ISystemShutdown, IExportSystemServices
    {
        private readonly RuntimeFaultBoundary _faults;
        private IRecoveryViewBufferReadSink _input;
        private IRecoveryViewBufferWriteSink _writer;
        private IRecoveryResultReadSink _results;
        // 제출 중·예약·실행 중·정지·종료 사실과 실행 시작·끝 앞부분은 증강과 함께 쓰는 실행 수명이 맡습니다(설계 §2.7 C-12).
        private readonly ViewRequestExecution _execution;
        private RecoveryViewCommand _pending;
        private RecoveryViewCommand _consumed;
        private bool _draining;

        internal RecoveryRequestLifetime(RuntimeFaultBoundary faults)
        {
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));
            _execution = new ViewRequestExecution("RecoveryRequest", "회복", faults);
        }

        public bool HasPending => _execution.IsSubmitting || _pending.Number != 0;
        public bool CanReserveExecution => _execution.CanReserve && _pending.Number != 0;
        public bool HasReservedExecution => _execution.IsReserved;

        /// <summary>게시된 결과 중 아직 확인하지 않은 것이 있습니다. 이 소유자 안에서 결과 번호를 비교하는 곳은 여기 하나입니다.</summary>
        private bool HasUnacknowledgedResult => _results.HandledCommandNumber > _input.AcknowledgedResultNumber;

        internal void BindServices(IRecoveryViewBufferReadSink input, IRecoveryViewBufferWriteSink writer,
            IRecoveryResultReadSink results, IJobRunner runner)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _results = results ?? throw new ArgumentNullException(nameof(results));
            _execution.BindRunner(runner);
        }

        public int Submit(RecoveryCommandData command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            _execution.RequireAvailable();
            if (HasPending) throw new InvalidOperationException("이전 회복 요청의 실행과 소비를 기다려야 합니다.");
            DrainPublished();
            _execution.RequireAvailable();
            return _execution.Submit(() =>
            {
                int number = _writer.Submit(command);
                _pending = new RecoveryViewCommand(number, command);
                if (_execution.IsStopped || _faults.IsFaulted) CancelPending();
                return number;
            });
        }

        public bool TryReserveExecution(out RecoveryViewCommand command)
        {
            command = default;
            DrainPublished();
            if (!CanReserveExecution) return false;
            if (!_input.TryGetCommand(_pending.Number, out var input))
            {
                _pending = default;
                return false;
            }
            if (!Matches(input, _pending))
            {
                Fail("예약할 회복 입력의 전체 명령이 보관 기록과 다릅니다.");
                return false;
            }
            _execution.Reserve();
            command = _pending;
            return true;
        }

        public bool TryBeginExecution(int number) => _execution.TryBegin(number, _pending.Number);

        public void NotifyExecutionFinished(int number, bool processed, Exception error)
        {
            if (!_execution.TryFinish(number, _pending.Number, error)) return;
            try
            {
                bool backpressure = HasUnacknowledgedResult && _results.HandledCommandNumber != number;
                DrainPublished();
                _execution.EndReservation();
                if (_consumed.Number == number)
                {
                    _pending = default;
                    return;
                }
                if (error != null)
                {
                    _writer.RemoveCommand(number);
                    _pending = default;
                    return;
                }
                if (processed || _results.HandledCommandNumber >= number)
                {
                    Fail($"회복 요청 {number}의 대조 가능한 게시 결과가 없습니다.");
                    return;
                }
                if (!_input.TryGetCommand(number, out var input))
                {
                    _pending = default; // 실행 전 취소는 처리 결과도 ack도 만들지 않습니다.
                    return;
                }
                if (!Matches(input, _pending))
                {
                    Fail("실행을 마친 회복 입력의 전체 명령이 바뀌었습니다.");
                    return;
                }
                if (!backpressure) Fail($"회복 요청 {number}이 남아 있지만 Core 처리 기록이 없습니다.");
                // 자기 과거 출력을 소비한 backpressure만 같은 요청의 재예약을 허용합니다.
            }
            catch (Exception cleanupError)
            {
                _execution.Stop();
                if (error == null) _faults.Report(cleanupError);
                else error.Data["RecoveryRequestCleanupError"] = cleanupError;
            }
        }

        public bool TryGetResult(int number, RecoveryCommandData command, out RecoveryProcessedResult result)
        {
            result = null;
            if (command == null || _results == null || _consumed.Number != number ||
                !Matches(_consumed.Data, command) || !_results.TryGetLatest(out var latest) ||
                latest.Number != number || !Matches(latest.Command, command) ||
                _input.AcknowledgedResultNumber < number) return false;
            result = latest;
            return true;
        }

        public void CancelPending()
        {
            if (_pending.Number == 0) return;
            int number = _pending.Number;
            _writer.RemoveCommand(number);
            DrainPublished();
            if (!_execution.IsReserved && _pending.Number == number)
            {
                if (_results.HandledCommandNumber < number || _consumed.Number == number) _pending = default;
                else Fail($"취소한 회복 요청 {number}의 게시 결과를 대조할 수 없습니다.");
            }
        }

        internal void StopInput()
        {
            _execution.Stop();
            CancelPending();
            DrainPublished();
        }

        public void Tick(float deltaTime) => DrainPublished();

        public void Shutdown()
        {
            if (!_execution.BeginShutdown()) return;
            StopInput();
            // 통지 없이 버려지는 대기 예약을 놓았으면 보관 번호도 놓습니다(Runner 비우기는 실행 수명이 합니다).
            if (_execution.TryReleaseQueuedReservation()) _pending = default;
        }

        private void DrainPublished()
        {
            if (_draining || _results == null || !HasUnacknowledgedResult) return;
            _draining = true;
            try
            {
                if (!_results.TryGetLatest(out var result) || result.Number != _results.HandledCommandNumber)
                {
                    Fail("회복 처리 번호에 해당하는 실제 게시 결과가 없습니다.");
                    return;
                }
                bool pending = result.Number == _pending.Number && Matches(result.Command, _pending.Data);
                bool consumed = result.Number == _consumed.Number && Matches(result.Command, _consumed.Data);
                if (!pending && !consumed)
                {
                    Fail("회복 결과의 번호와 전체 명령에 해당하는 소유 기록이 없습니다.");
                    return;
                }
                _writer.AcknowledgeResult(result.Number);
                if (pending) _consumed = _pending;
                _writer.RemoveCommand(result.Number);
                if (pending && !_execution.IsReserved) _pending = default;
            }
            finally
            {
                _draining = false;
            }
        }

        private void Fail(string message) => _execution.Fail(message);

        private static bool Matches(RecoveryViewCommand first, RecoveryViewCommand second) =>
            first.Number == second.Number && Matches(first.Data, second.Data);

        private static bool Matches(RecoveryCommandData first, RecoveryCommandData second) =>
            first != null && second != null && first.Kind == second.Kind &&
            first.Context == second.Context && first.OptionId == second.OptionId;
        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            var buffers = resolver.GetService(BufferSystemKey.Buffer);
            var global = resolver.GetService(SystemGroupsService.Global);
            BindServices(buffers.RecoveryViewReadSink, buffers.RecoveryViewWriteSink,
                buffers.RecoveryResultReadSink, global.JobServiceCommand.JobRunner);
        }
        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IRecoveryRequestSubmitter>(this);
            sink.AddService<IRecoveryRequestExecution>(this);
            sink.AddService<IRecoveryRequestStatus>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }
}
