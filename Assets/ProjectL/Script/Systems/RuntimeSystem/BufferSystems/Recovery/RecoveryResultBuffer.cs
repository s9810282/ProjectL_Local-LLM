using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Framework.Composition;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Recovery
{
    internal sealed class RecoveryResultBuffer : IRecoveryResultReadSink, IRecoveryResultStore, IExportSystemServices
    {
        private RecoveryProcessedResult _latest;

        public int HandledCommandNumber => _latest?.Number ?? 0;

        public bool TryGetLatest(out RecoveryProcessedResult result)
        {
            result = _latest;
            return result != null;
        }

        public RecoveryProcessedResult PrepareResult(RecoveryProcessedResult result, int acknowledgedNumber)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (acknowledgedNumber != HandledCommandNumber || result.Number <= HandledCommandNumber)
                throw new InvalidOperationException("이전 게시 결과의 소비 확인과 증가한 요청 번호가 필요합니다.");
            return result;
        }

        // 거래 경계에서 최종 검증을 끝낸 값만 설치한다. 이력은 게임 진행 초기화에 참여하지 않는다.
        public void InstallResult(RecoveryProcessedResult prepared) => _latest = prepared;

        public void ExportServices(ISystemWriteSink sink)
        {
            if (sink == null) throw new ArgumentNullException(nameof(sink));
            sink.AddService<IRecoveryResultReadSink>(this);
            sink.AddService<IRecoveryResultStore>(this);
        }
    }
}

