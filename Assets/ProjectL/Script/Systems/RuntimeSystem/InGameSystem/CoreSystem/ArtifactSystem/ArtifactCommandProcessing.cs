using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.BufferSystems.ArtifactView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ArtifactSystem
{
    /// <summary>Core가 입력을 조회·계산·게시합니다. 기대 번호는 취소·재연결 보호이며 입력 정리는 View가 맡습니다.</summary>
    public interface IArtifactCommandProcessingService : IServiceModule
    {
        bool ProcessNextCommand(int expectedRequestNumber);
        bool ProcessLatestPreview(int expectedRequestNumber);
    }

    internal sealed partial class ArtifactEditingService : IArtifactCommandProcessingService
    {
        private IArtifactCommandResultReadSink _commandResults;
        private IArtifactCommandResultStore _commandStore;
        private IArtifactViewBufferReadSink _commandInput;
        private bool _processingNumberedRequest;

        public bool ProcessNextCommand(int expectedRequestNumber)
        {
            ValidateNumberedRequest(expectedRequestNumber);
            if (!TryReadCommand(expectedRequestNumber, out var request)) return false;
            ArtifactViewCommandData data = request.Data;
            data.Validate();
            _processingNumberedRequest = true;
            try
            {
                RequireInitialized();
                ArtifactEditProposal proposal;
                if (!CanEdit())
                {
                    proposal = new ArtifactEditProposal(ArtifactActionResult.Failure(ArtifactActionFailureReason.LayoutLocked), null);
                }
                else
                {
                    ArtifactState state = _read.Current.ToCalculationState();
                    switch (data.Kind)
                    {
                        case ArtifactViewCommandKind.Place:
                            proposal = _calculation.Place(state, data.ArtifactId, ReadDefinition(data.ArtifactId), data.Cell, data.Rotation);
                            break;
                        case ArtifactViewCommandKind.Move:
                            proposal = _calculation.Move(state, data.InstanceId, ReadPlacedDefinition(state, data.InstanceId), data.Cell, data.Rotation);
                            break;
                        case ArtifactViewCommandKind.Return:
                            proposal = _calculation.ReturnToPool(state, data.InstanceId, ReadPlacedDefinition(state, data.InstanceId));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(data));
                    }
                }
                // 정의 조회·계산 중 취소되거나 연결이 바뀌었다면 이전 제안을 게시하지 않습니다.
                if (!TryReadCommand(expectedRequestNumber, out _)) return false;
                _commandStore.PublishCommand(request.Number, data, proposal);
                return true;
            }
            finally
            {
                _processingNumberedRequest = false;
            }
        }

        public bool ProcessLatestPreview(int expectedRequestNumber)
        {
            ValidateNumberedRequest(expectedRequestNumber);
            if (!TryReadPreview(expectedRequestNumber, out var request)) return false;
            ArtifactViewPreviewData data = request.Data;
            data.Validate();
            _processingNumberedRequest = true;
            try
            {
                ArtifactState state = ReadState();
                ArtifactPlacementPreview preview = null;
                bool skipped = data.Kind == ArtifactViewCommandKind.Move && !ContainsPlacedInstance(state, data.InstanceId);
                if (data.Kind == ArtifactViewCommandKind.Place)
                {
                    preview = _calculation.PreviewPlace(state, data.ArtifactId, ReadDefinition(data.ArtifactId), data.Cell, data.Rotation);
                }
                else if (!skipped)
                {
                    preview = _calculation.PreviewMove(state, data.InstanceId, ReadPlacedDefinition(state, data.InstanceId), data.Cell, data.Rotation);
                }
                if (!skipped && preview == null)
                    throw new InvalidOperationException("아티팩트 계산기가 프리뷰 결과를 반환하지 않았습니다.");
                // 계산 중 취소·교체를 다시 확인합니다. 유효한 미배치 이동만 번호가 있는 생략 결과를 게시합니다.
                if (!TryReadPreview(expectedRequestNumber, out _)) return false;
                _commandStore.PublishAnswer(request.Number, data, preview);
                return true;
            }
            finally
            {
                _processingNumberedRequest = false;
            }
        }

        private void ValidateNumberedRequest(int requestNumber)
        {
            if (requestNumber <= 0) throw new ArgumentOutOfRangeException(nameof(requestNumber));
            RequireReady();
            if (_commandResults == null || _commandStore == null || _commandInput == null)
                throw new InvalidOperationException("아티팩트 번호 결과 서비스가 바인딩되어야 합니다.");
            if (_processingNumberedRequest) throw new InvalidOperationException("아티팩트 요청 처리 중 재진입할 수 없습니다.");
        }

        private bool TryReadCommand(int expectedRequestNumber, out ArtifactViewCommand request)
        {
            return _commandInput.TryGetNextCommand(_commandResults.HandledCommandNumber, out request)
                && request.Number == expectedRequestNumber;
        }

        private bool TryReadPreview(int expectedRequestNumber, out ArtifactViewPreviewRequest request)
        {
            request = default;
            if (!_commandInput.HasPreview) return false;
            request = _commandInput.Preview;
            return request.Number == expectedRequestNumber
                && request.Number > _commandResults.AnsweredPreviewNumber;
        }

        private static bool ContainsPlacedInstance(ArtifactState state, int instanceId)
        {
            foreach (ArtifactItemState item in state.PlacedItems.Span)
            {
                if (item.InstanceId == instanceId) return true;
            }
            return false;
        }
    }
}
