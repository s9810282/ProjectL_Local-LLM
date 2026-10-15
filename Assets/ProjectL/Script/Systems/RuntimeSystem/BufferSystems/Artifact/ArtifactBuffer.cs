#nullable enable
using System;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact
{
    #region 서비스

    public interface IArtifactBufferReadSink : IServiceModule
    {
        ArtifactBufferSnapshot Current { get; }
    }

    public interface IArtifactInitializationWriteSink : IServiceModule
    {
        bool TryInitialize(ArtifactState initialState);
    }

    public interface IArtifactEditWriteSink : IServiceModule
    {
        void PublishEdit(ArtifactEditProposal proposal);
    }

    public interface IArtifactPreviewWriteSink : IServiceModule
    {
        void PublishPreview(ArtifactPlacementPreview preview);
    }

    public interface IArtifactEditResultReadSink : IServiceModule
    {
        ArtifactActionResult? LastEditResult { get; }
    }

    public interface IArtifactPreviewReadSink : IServiceModule
    {
        ArtifactPlacementPreview? LastPreview { get; }
    }

    #endregion

    #region 시스템 모듈

    internal sealed class ArtifactBuffer :
        IArtifactBufferReadSink,
        IArtifactInitializationWriteSink,
        IArtifactEditWriteSink,
        IArtifactPreviewWriteSink,
        IArtifactEditResultReadSink,
        IArtifactPreviewReadSink,
        IExportSystemServices
    {
        private ArtifactBufferSnapshot _current = ArtifactBufferSnapshot.Empty;

        public ArtifactBufferSnapshot Current => _current;
        public ArtifactActionResult? LastEditResult { get; private set; }
        public ArtifactPlacementPreview? LastPreview { get; private set; }

        public bool TryInitialize(ArtifactState initialState)
        {
            if (initialState == null) throw new ArgumentNullException(nameof(initialState));
            if (_current.IsInitialized) return false;
            _current = new ArtifactBufferSnapshot(initialState);
            return true;
        }

        public void PublishEdit(ArtifactEditProposal proposal)
        {
            if (proposal == null) throw new ArgumentNullException(nameof(proposal));
            EnsureInitialized();

            // 검증과 복사를 모두 마친 뒤 상태와 결과를 함께 게시한다.
            ArtifactBufferSnapshot next = proposal.Result.Succeeded
                ? new ArtifactBufferSnapshot(proposal.State!) : _current;
            _current = next;
            LastEditResult = proposal.Result;
            LastPreview = null;
        }

        public void PublishPreview(ArtifactPlacementPreview preview)
        {
            if (preview == null) throw new ArgumentNullException(nameof(preview));
            EnsureInitialized();
            LastPreview = preview;
        }

        private void EnsureInitialized()
        {
            if (!_current.IsInitialized)
                throw new InvalidOperationException("아티팩트 상태를 먼저 초기화해야 합니다.");
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IArtifactBufferReadSink>(this);
            sink.AddService<IArtifactInitializationWriteSink>(this);
            sink.AddService<IArtifactEditWriteSink>(this);
            sink.AddService<IArtifactPreviewWriteSink>(this);
            sink.AddService<IArtifactEditResultReadSink>(this);
            sink.AddService<IArtifactPreviewReadSink>(this);
        }
    }

    #endregion
}
