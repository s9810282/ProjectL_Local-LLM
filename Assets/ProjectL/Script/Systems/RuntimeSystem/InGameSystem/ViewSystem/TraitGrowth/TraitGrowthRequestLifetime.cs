using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.TraitGrowthView;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.ServiceKeys.Runtime;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.TraitGrowth
{
    /// <summary>
    /// 씬과 독립적으로 증강 선택 요청 한 건의 제출 번호를 들고 실행 예약을 맡습니다(설계 §14 TG4·TG6).
    /// 화면이 사라져도 번호를 잃지 않게 제출과 보관을 같은 호출에서 합니다.
    /// </summary>
    /// <remarks>
    /// 회복 요청 소유자(<see cref="Recovery.RecoveryRequestLifetime"/>)와 다른 점(Core 인계 §5):
    /// 결과를 스스로 확인하지 않습니다(확인은 화면 컨트롤러가 합니다). 정지·씬 이탈 때 입력을 취소하지 않습니다(취소 API가 없음).
    /// 들고 있는 번호는 결과가 확인되거나, 입력도 결과도 없을 때(Run 재시작 Clear) 놓습니다.
    /// </remarks>
    internal sealed class TraitGrowthRequestLifetime : ITraitGrowthRequestSubmitter, ITraitGrowthRequestExecution,
        ISystemPostBindService, ISystemTick, ISystemShutdown, IExportSystemServices
    {
        private readonly RuntimeFaultBoundary _faults;
        private ITraitGrowthViewReadSink _input;
        private ITraitGrowthViewWriteSink _writer;
        private ITraitGrowthResultReadSink _results;
        private IJobRunner _runner;
        private int _pending;
        private bool _reserved;
        private bool _executing;
        private bool _submitting;
        private bool _stopped;
        private bool _shutdown;

        internal TraitGrowthRequestLifetime(RuntimeFaultBoundary faults) =>
            _faults = faults ?? throw new ArgumentNullException(nameof(faults));

        public bool HasPending => _submitting || _pending != 0;

        // 앞선 결과를 아직 확인하지 않았으면 Core가 처리하지 않으므로 예약하지 않습니다(빈 Job 반복을 막음).
        public bool CanReserveExecution => !_stopped && !_shutdown && !_faults.IsFaulted && !_reserved &&
            _pending != 0 && _results != null && _results.HandledCommandNumber < _pending &&
            _results.HandledCommandNumber <= _input.AcknowledgedResultNumber;

        public bool HasReservedExecution => _reserved;

        internal void BindServices(ITraitGrowthViewReadSink input, ITraitGrowthViewWriteSink writer,
            ITraitGrowthResultReadSink results, IJobRunner runner)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _results = results ?? throw new ArgumentNullException(nameof(results));
            _runner = runner ?? throw new ArgumentNullException(nameof(runner));
        }

        public int Submit(TraitGrowthChoiceRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            RequireAvailable();
            ReleaseSettled();
            if (HasPending) throw new InvalidOperationException("이전 증강 선택의 처리와 결과 확인을 기다려야 합니다.");
            _submitting = true;
            try
            {
                _pending = _writer.Submit(request);
                return _pending;
            }
            finally
            {
                _submitting = false;
            }
        }

        public bool TryReserveExecution(out int number)
        {
            number = 0;
            ReleaseSettled();
            if (!CanReserveExecution) return false;
            if (!_input.TryGetCommand(_pending, out _))
            {
                _pending = 0;
                return false;
            }
            _reserved = true;
            number = _pending;
            return true;
        }

        public bool TryBeginExecution(int number)
        {
            if (_shutdown || !_reserved || _executing || _pending != number) return false;
            _executing = true;
            return true;
        }

        public void NotifyExecutionFinished(int number, bool processed, Exception error)
        {
            // 원인을 먼저 고정합니다.
            if (error != null) _faults.Report(error);
            if (!_reserved || _pending != number)
            {
                if (!_shutdown) Fail("보관한 예약과 다른 증강 실행 종료가 도착했습니다.");
                return;
            }
            _executing = false;
            _reserved = false;
            if (error != null) return;
            // 게시된 결과는 화면 컨트롤러가 확인합니다. 그때까지 번호를 들고 있어 화면이 잠깁니다.
            if (processed || _results.HandledCommandNumber >= number) return;
            if (!_input.TryGetCommand(number, out _))
            {
                _pending = 0; // 실행 전에 Run 재시작으로 지워졌습니다.
                return;
            }
            Fail($"증강 요청 {number}이 남아 있지만 Core 처리 기록이 없습니다.");
        }

        public void Tick(float deltaTime) => ReleaseSettled();

        public void Shutdown()
        {
            if (_shutdown) return;
            _shutdown = true;
            if (_executing || !_reserved) return;
            // 루트는 View 다음 Global을 종료합니다. 통지 없이 버려지는 대기 예약을 놓으려면 같은 Runner를 먼저 비웁니다(회복과 같음).
            _runner?.Clear();
            if (_runner == null || _runner.IsIdle) _reserved = false;
        }

        // 결과가 확인됐거나, 입력도 결과도 없으면(Run 재시작 Clear) 들고 있던 번호를 놓습니다.
        private void ReleaseSettled()
        {
            if (_pending == 0 || _reserved || _input == null) return;
            if (_input.AcknowledgedResultNumber >= _pending)
            {
                _pending = 0;
                return;
            }
            if (_results.HandledCommandNumber < _pending && !_input.TryGetCommand(_pending, out _)) _pending = 0;
        }

        private void RequireAvailable()
        {
            if (_input == null || _stopped || _shutdown || _faults.IsFaulted)
                throw new InvalidOperationException("연결되고 정지하지 않은 증강 요청 소유자가 필요합니다.");
        }

        private void Fail(string message)
        {
            _stopped = true;
            _faults.Report(new InvalidOperationException("[TraitGrowthRequest] " + message));
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            var buffers = resolver.GetService(BufferSystemKey.Buffer);
            var global = resolver.GetService(SystemGroupsService.Global);
            BindServices(buffers.TraitGrowthViewReadSink, buffers.TraitGrowthViewWriteSink,
                buffers.TraitGrowthResultReadSink, global.JobServiceCommand.JobRunner);
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ITraitGrowthRequestSubmitter>(this);
            sink.AddService<ITraitGrowthRequestExecution>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }
}
