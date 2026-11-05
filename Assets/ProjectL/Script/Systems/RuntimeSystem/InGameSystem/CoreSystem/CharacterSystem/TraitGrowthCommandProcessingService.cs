using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.TraitGrowthView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.ServiceKeys.Runtime;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem
{
    public interface ITraitGrowthCommandProcessingService : IServiceModule
    {
        bool ProcessNextTraitGrowthCommand(int expectedNumber);
    }

    /// <summary>실제 입력 하나와 기존 로스터 준비를 대조한 뒤 선택·제안·결과를 함께 게시한다.</summary>
    internal sealed class TraitGrowthCommandProcessingService : ITraitGrowthCommandProcessingService,
        ISystemPostBindService, IExportSystemServices
    {
        private readonly CharacterRosterService _roster;
        private IBufferQuery _buffers;

        internal TraitGrowthCommandProcessingService(CharacterRosterService roster)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            _buffers = resolver.GetService(BufferSystemKey.Buffer);
        }

        public bool ProcessNextTraitGrowthCommand(int expectedNumber)
        {
            if (expectedNumber < 1) throw new ArgumentOutOfRangeException(nameof(expectedNumber));
            if (_buffers == null) throw new InvalidOperationException("성장 명령 처리는 서비스 바인딩이 필요합니다.");
            if (!CanPublish() || expectedNumber <= _buffers.TraitGrowthResultReadSink.HandledCommandNumber ||
                !_buffers.TraitGrowthViewReadSink.TryGetCommand(expectedNumber, out var command))
                return false;

            var operation = _roster.PrepareTraitGrowthChoice(command.Request);
            if (!StillPending(command) || !CanPublish()) return false;
            if (operation.Decision.Succeeded)
            {
                // 제안 뒤 DB가 다시 로드되어 선택 특성이 병과 기본값이 된 경우도 게시 전에 거른다.
                var checkedChoice = _roster.PrepareTraitGrowthChoice(command.Request);
                if (operation.RunId != checkedChoice.RunId || operation.Revision != checkedChoice.Revision ||
                    !ReferenceEquals(operation.Offer, checkedChoice.Offer) || !StillPending(command) || !CanPublish())
                    return false;
                operation = checkedChoice;
            }
            var result = operation.Decision.Succeeded
                ? TraitGrowthResult.Success(operation.Decision.Change.Final)
                : TraitGrowthResult.Failure(operation.Decision.FailureReason);
            var receipt = new TraitGrowthProcessedResult(operation.RunId, command.Number, command.Request, result);
            var publication = _buffers.TraitGrowthOperations.PrepareChoicePublication(operation.RunId,
                operation.Revision, operation.Offer, operation.Decision.Change, receipt);
            _buffers.TraitGrowthOperations.InstallChoicePublication(publication);
            return true;
        }

        private bool CanPublish()
        {
            int handled = _buffers.TraitGrowthResultReadSink.HandledCommandNumber;
            int acknowledged = _buffers.TraitGrowthViewReadSink.AcknowledgedResultNumber;
            if (acknowledged < 0 || acknowledged > handled)
                throw new InvalidOperationException("성장 소비 확인이 실제 게시 이력을 벗어났습니다.");
            return acknowledged == handled;
        }

        private bool StillPending(TraitGrowthViewCommand expected) =>
            expected.Number > _buffers.TraitGrowthResultReadSink.HandledCommandNumber &&
            _buffers.TraitGrowthViewReadSink.TryGetCommand(expected.Number, out var current) &&
            current.Request.SameValues(expected.Request);

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null) throw new ArgumentNullException(nameof(sink));
            sink.AddService<ITraitGrowthCommandProcessingService>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }
}
