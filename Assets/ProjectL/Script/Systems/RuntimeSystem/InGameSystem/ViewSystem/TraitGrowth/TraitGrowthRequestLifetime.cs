using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.TraitGrowthView;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.JobSystem.JobRunner;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common;
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
        private ITraitGrowthViewReadSink _input;
        private ITraitGrowthViewWriteSink _writer;
        private ITraitGrowthResultReadSink _results;
        // 제출 중·예약·실행 중·정지·종료 사실과 실행 시작·끝 앞부분은 회복과 함께 쓰는 실행 수명이 맡습니다(설계 §2.7 C-12).
        private readonly ViewRequestExecution _execution;
        private int _pending;

        internal TraitGrowthRequestLifetime(RuntimeFaultBoundary faults)
        {
            // 오류 경계는 실행 수명만 씁니다(실행 오류·실패 보고).
            _execution = new ViewRequestExecution("TraitGrowthRequest", "증강", faults);
        }

        public bool HasPending => _execution.IsSubmitting || _pending != 0;

        /// <summary>
        /// 게시된 결과 중 화면이 아직 확인하지 않은 것이 있습니다. View 안에서 증강 결과 번호를 비교하는 곳은 여기 하나이고,
        /// 증강 화면 컨트롤러도 이것을 읽습니다(설계 §2.7 C-12). 서비스를 받기 전이면 거짓입니다.
        /// </summary>
        public bool HasUnacknowledgedResult => _results != null && _results.HandledCommandNumber > _input.AcknowledgedResultNumber;

        // 앞선 결과를 아직 확인하지 않았으면 Core가 처리하지 않으므로 예약하지 않습니다(빈 Job 반복을 막음).
        public bool CanReserveExecution => _execution.CanReserve && _pending != 0 && _results != null &&
            _results.HandledCommandNumber < _pending && !HasUnacknowledgedResult;

        public bool HasReservedExecution => _execution.IsReserved;

        internal void BindServices(ITraitGrowthViewReadSink input, ITraitGrowthViewWriteSink writer,
            ITraitGrowthResultReadSink results, IJobRunner runner)
        {
            _input = input ?? throw new ArgumentNullException(nameof(input));
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _results = results ?? throw new ArgumentNullException(nameof(results));
            _execution.BindRunner(runner);
        }

        public int Submit(TraitGrowthChoiceRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            _execution.RequireAvailable();
            ReleaseSettled();
            if (HasPending) throw new InvalidOperationException("이전 증강 선택의 처리와 결과 확인을 기다려야 합니다.");
            return _execution.Submit(() => _pending = _writer.Submit(request));
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
            _execution.Reserve();
            number = _pending;
            return true;
        }

        public bool TryBeginExecution(int number) => _execution.TryBegin(number, _pending);

        public void NotifyExecutionFinished(int number, bool processed, Exception error)
        {
            if (!_execution.TryFinish(number, _pending, error)) return;
            _execution.EndReservation();
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
            if (!_execution.BeginShutdown()) return;
            // 통지 없이 버려지는 대기 예약을 놓습니다(Runner 비우기는 실행 수명이 합니다, 회복과 같음).
            _execution.TryReleaseQueuedReservation();
        }

        // 결과가 확인됐거나, 입력도 결과도 없으면(Run 재시작 Clear) 들고 있던 번호를 놓습니다.
        private void ReleaseSettled()
        {
            if (_pending == 0 || _execution.IsReserved || _input == null) return;
            if (_input.AcknowledgedResultNumber >= _pending)
            {
                _pending = 0;
                return;
            }
            if (_results.HandledCommandNumber < _pending && !_input.TryGetCommand(_pending, out _)) _pending = 0;
        }

        private void Fail(string message) => _execution.Fail(message);

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
