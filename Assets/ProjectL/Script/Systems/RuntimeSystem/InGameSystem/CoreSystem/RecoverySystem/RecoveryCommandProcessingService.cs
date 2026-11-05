using ProjectL.Systems.ServiceKeys.Runtime;
using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.RecoveryView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.RecoverySystem
{
    /// <summary>정확한 입력을 다시 확인한 뒤 거래와 요청 결과를 함께 게시한다.</summary>
    internal sealed class RecoveryCommandProcessingService : IRecoveryCommandProcessingService,
        ISystemInitData<IDataBaseQueryContext>, ISystemPostBindService, IExportSystemServices
    {
        private readonly RecoveryStageService _recovery;
        private readonly RecoveryStateProjection _projection;
        private IBufferQuery _buffers;
        private bool _hasData;

        internal RecoveryCommandProcessingService(RecoveryStageService recovery, RecoveryStateProjection projection)
        {
            _recovery = recovery ?? throw new ArgumentNullException(nameof(recovery));
            _projection = projection ?? throw new ArgumentNullException(nameof(projection));
        }

        public void InitData(IDataBaseQueryContext data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            _hasData = true;
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            var buffers = resolver.GetService(BufferSystemKey.Buffer);
            if (buffers.RecoveryViewReadSink == null || buffers.RecoveryResultReadSink == null ||
                buffers.RecoveryOperations == null)
                throw new InvalidOperationException("회복 입력·결과·거래 경계가 필요합니다.");
            _buffers = buffers;
        }

        public bool ProcessNextRecoveryCommand(int expectedNumber)
        {
            if (expectedNumber <= 0) throw new ArgumentOutOfRangeException(nameof(expectedNumber));
            using (_recovery.BeginOperation())
            {
                if (!_hasData || _buffers == null)
                    throw new InvalidOperationException("회복 명령 처리는 DB 초기화와 서비스 바인딩이 필요합니다.");
                if (!CanPublish() || expectedNumber <= _buffers.RecoveryResultReadSink.HandledCommandNumber)
                    return false;
                if (!_buffers.RecoveryViewReadSink.TryGetCommand(expectedNumber, out var input)) return false;

                RecoveryOperationPreparation operation;
                try
                {
                    operation = _recovery.PrepareOperation(input.Data);
                }
                catch (InvalidOperationException)
                {
                    // 조회 중 취소·진입 교체는 새 거래를 적용하지 않는다. 같은 문맥의 원본 오류는 그대로 전파한다.
                    if (!StillPending(input)) return false;
                    if (!IsStale(input.Data)) throw;
                    operation = _recovery.PrepareOperation(input.Data);
                }

                if (!StillPending(input)) return false;
                if (!CanPublish()) return false;
                if (operation.Facts != null) _projection.RequireUnchanged(_buffers, operation.Facts);
                var result = new RecoveryProcessedResult(input.Number, input.Data, operation.Result);
                var publication = _buffers.RecoveryOperations.PreparePublication(operation.Mutation, result,
                    _buffers.RecoveryViewReadSink.AcknowledgedResultNumber);
                operation.Consumed = true;
                _buffers.RecoveryOperations.InstallPublication(publication);
                return true;
            }
        }

        private bool CanPublish()
        {
            int handled = _buffers.RecoveryResultReadSink.HandledCommandNumber;
            int acknowledged = _buffers.RecoveryViewReadSink.AcknowledgedResultNumber;
            if (acknowledged < 0 || acknowledged > handled)
                throw new InvalidOperationException("회복 소비 확인이 실제 게시 이력을 벗어났습니다.");
            return acknowledged == handled;
        }

        private bool StillPending(RecoveryViewCommand expected)
        {
            return _buffers.RecoveryViewReadSink.TryGetCommand(expected.Number, out var current) &&
                current.Number == expected.Number && current.Data.Kind == expected.Data.Kind &&
                current.Data.Context == expected.Data.Context && current.Data.OptionId == expected.Data.OptionId;
        }

        private bool IsStale(RecoveryCommandData command)
        {
            if (!RecoveryStateProjection.TryGetEntry(_buffers, out var current)) return true;
            if (current.Context != command.Context) return true;
            return _projection.Capture(_buffers).IsCompleted;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null) throw new ArgumentNullException(nameof(sink));
            sink.AddService<IRecoveryCommandProcessingService>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }
}

