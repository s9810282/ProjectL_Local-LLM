using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Recovery;
using ProjectL.Systems.RuntimeSystem.BufferSystems.RecoveryView;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
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
        private IJobRunner _runner;
        private RecoveryViewCommand _pending;
        private RecoveryViewCommand _consumed;
        private bool _reserved;
        private bool _executing;
        private bool _submitting;
        private bool _draining;
        private bool _stopped;
        private bool _shutdown;

        internal RecoveryRequestLifetime(RuntimeFaultBoundary faults) => _faults = faults ?? throw new ArgumentNullException(nameof(faults));
        public bool HasPending => _submitting || _pending.Number != 0;
        public bool CanReserveExecution => !_stopped && !_shutdown && !_faults.IsFaulted &&
            !_reserved && _pending.Number != 0;
        public bool HasReservedExecution => _reserved;

        internal void BindServices(IRecoveryViewBufferReadSink input, IRecoveryViewBufferWriteSink writer,
            IRecoveryResultReadSink results, IJobRunner runner)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _results = results ?? throw new ArgumentNullException(nameof(results));
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        }

        public int Submit(RecoveryCommandData command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command));
            RequireAvailable();
            if (HasPending) throw new InvalidOperationException("이전 회복 요청의 실행과 소비를 기다려야 합니다.");
            DrainPublished();
            RequireAvailable();
            _submitting = true;
            try
            {
                int number = _writer.Submit(command);
                _pending = new RecoveryViewCommand(number, command);
                if (_stopped || _faults.IsFaulted) CancelPending();
                return number;
            }
            finally
            {
                _submitting = false;
            }
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
            _reserved = true;
            command = _pending;
            return true;
        }

        public bool TryBeginExecution(int number)
        {
            if (_shutdown || !_reserved || _executing || _pending.Number != number) return false;
            _executing = true;
            return true;
        }

        public void NotifyExecutionFinished(int number, bool processed, Exception error)
        {
            // 원인을 먼저 고정합니다. fault 정리가 재진입해도 아직 실행 기록을 놓지 않습니다.
            if (error != null) _faults.Report(error);
            if (!_reserved || _pending.Number != number)
            {
                if (!_shutdown) Fail("보관한 예약과 다른 회복 실행 종료가 도착했습니다.");
                return;
            }
            _executing = false;
            try
            {
                bool backpressure = _results.HandledCommandNumber > _input.AcknowledgedResultNumber &&
                    _results.HandledCommandNumber != number;
                DrainPublished();
                _reserved = false;
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
                _stopped = true;
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
            if (!_reserved && _pending.Number == number)
            {
                if (_results.HandledCommandNumber < number || _consumed.Number == number) _pending = default;
                else Fail($"취소한 회복 요청 {number}의 게시 결과를 대조할 수 없습니다.");
            }
        }

        internal void StopInput()
        {
            _stopped = true;
            CancelPending();
            DrainPublished();
        }

        public void Tick(float deltaTime) => DrainPublished();

        public void Shutdown()
        {
            if (_shutdown) return;
            _shutdown = true;
            StopInput();
            if (_executing || !_reserved) return;
            // 루트는 View 다음 Global을 종료합니다. 같은 Runner의 공개 Clear를 먼저 완료해야
            // 통지 없이 버려지는 queued 예약을 해제할 수 있습니다. 일반 취소에는 적용하지 않습니다.
            _runner?.Clear();
            if (_runner == null || _runner.IsIdle)
            {
                _pending = default;
                _reserved = false;
            }
        }

        private void DrainPublished()
        {
            if (_draining || _results == null || _results.HandledCommandNumber <= _input.AcknowledgedResultNumber) return;
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
                if (pending && !_reserved) _pending = default;
            }
            finally
            {
                _draining = false;
            }
        }

        private void RequireAvailable()
        {
            if (_input == null || _stopped || _shutdown || _faults.IsFaulted)
                throw new InvalidOperationException("연결되고 정지하지 않은 회복 요청 소유자가 필요합니다.");
        }

        private void Fail(string message)
        {
            _stopped = true;
            _faults.Report(new InvalidOperationException("[RecoveryRequest] " + message));
        }

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
