#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.ArtifactSystem
{
    public interface IArtifactEditingService : IServiceModule
    {
        bool InitializeBoard(int width, int height);
        void Place(int artifactId, CellCoord anchor, Rotation4 rotation);
        void Move(int instanceId, CellCoord anchor, Rotation4 rotation);
        void ReturnToPool(int instanceId);
        void ExpandBoard(int width, int height);
        void PreviewPlace(int artifactId, CellCoord anchor, Rotation4 rotation);
        void PreviewMove(int instanceId, CellCoord anchor, Rotation4 rotation);
    }

    /// <summary>현재 사실을 계산 포트에 전달하고 완성된 제안을 Buffer에 게시한다.</summary>
    internal sealed class ArtifactEditingService : IArtifactEditingService,
        ISystemInitData<IDataBaseQueryContext>, ISystemPostBindService, IExportSystemServices
    {
        private readonly IArtifactEditCalculation _calculation;
        private IArtifactDefinitionTableQuery _definitions = null!;
        private IArtifactBufferReadSink _read = null!;
        private IArtifactInitializationWriteSink _initialization = null!;
        private IArtifactEditWriteSink _edits = null!;
        private IArtifactPreviewWriteSink _previews = null!;
        private ICombatStateBufferReadSink _combat = null!;

        internal ArtifactEditingService(IArtifactEditCalculation calculation)
        {
            _calculation = calculation ?? throw new ArgumentNullException(nameof(calculation));
        }

        public void InitData(IDataBaseQueryContext data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            _definitions = data.InGameQuery.ArtifactDefinitionTableQuery
                ?? throw new InvalidOperationException("아티팩트 정의 조회가 필요합니다.");
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            var buffers = resolver.GetService(BufferSystemKey.Buffer);
            _read = buffers.ArtifactReadSink;
            _initialization = buffers.ArtifactInitializationWriteSink;
            _edits = buffers.ArtifactEditWriteSink;
            _previews = buffers.ArtifactPreviewWriteSink;
            _combat = buffers.CombatStateReadSink;
        }

        public bool InitializeBoard(int width, int height)
        {
            RequireReady();
            if (_read.Current.IsInitialized) return false;
            if (!CanEdit()) throw new InvalidOperationException("전투 중에는 아티팩트 초기화를 시작할 수 없습니다.");

            var definitions = _definitions.GetAllDefinitions().Select(data => data?.Definition
                ?? throw new InvalidOperationException("아티팩트 정의 값이 필요합니다."))
                .OrderBy(definition => definition.ArtifactId).ToArray();
            return _initialization.TryInitialize(_calculation.CreateInitialState(width, height, definitions));
        }

        public void Place(int artifactId, CellCoord anchor, Rotation4 rotation)
        {
            ArtifactState? state = ReadEditState();
            if (state == null) return;
            _edits.PublishEdit(_calculation.Place(state, artifactId, ReadDefinition(artifactId), anchor, rotation));
        }

        public void Move(int instanceId, CellCoord anchor, Rotation4 rotation)
        {
            ArtifactState? state = ReadEditState();
            if (state == null) return;
            _edits.PublishEdit(_calculation.Move(state, instanceId, ReadPlacedDefinition(state, instanceId), anchor, rotation));
        }

        public void ReturnToPool(int instanceId)
        {
            ArtifactState? state = ReadEditState();
            if (state == null) return;
            _edits.PublishEdit(_calculation.ReturnToPool(state, instanceId, ReadPlacedDefinition(state, instanceId)));
        }

        public void ExpandBoard(int width, int height)
        {
            ArtifactState? state = ReadEditState();
            if (state == null) return;
            var definitions = new List<ArtifactDefinition>();
            var ids = new HashSet<int>();
            foreach (ArtifactItemState item in state.PlacedItems.Span)
                if (ids.Add(item.ArtifactId))
                    definitions.Add(ReadDefinition(item.ArtifactId)
                        ?? throw new InvalidOperationException("배치된 아티팩트 정의가 필요합니다."));
            _edits.PublishEdit(_calculation.ExpandBoard(state, width, height, definitions));
        }

        public void PreviewPlace(int artifactId, CellCoord anchor, Rotation4 rotation)
        {
            ArtifactState state = ReadState();
            _previews.PublishPreview(_calculation.PreviewPlace(state, artifactId, ReadDefinition(artifactId), anchor, rotation));
        }

        public void PreviewMove(int instanceId, CellCoord anchor, Rotation4 rotation)
        {
            ArtifactState state = ReadState();
            _previews.PublishPreview(_calculation.PreviewMove(state, instanceId, ReadPlacedDefinition(state, instanceId), anchor, rotation));
        }

        private ArtifactState? ReadEditState()
        {
            RequireReady();
            RequireInitialized();
            // 실행 자격은 한 경계에서 먼저 확인하며 잠긴 요청은 정의 조회 없이 완료한다.
            if (!CanEdit())
            {
                _edits.PublishEdit(new ArtifactEditProposal(ArtifactActionResult.Failure(ArtifactActionFailureReason.LayoutLocked), null));
                return null;
            }
            return _read.Current.ToCalculationState();
        }

        private ArtifactState ReadState()
        {
            RequireReady();
            RequireInitialized();
            return _read.Current.ToCalculationState();
        }

        private bool CanEdit() => !_combat.HasBattle;

        private ArtifactDefinition? ReadPlacedDefinition(ArtifactState state, int instanceId)
        {
            foreach (ArtifactItemState item in state.PlacedItems.Span)
                if (item.InstanceId == instanceId) return ReadDefinition(item.ArtifactId);
            return null;
        }

        private ArtifactDefinition? ReadDefinition(int artifactId)
        {
            if (!_definitions.TryGetDefinition(artifactId, out var data)) return null;
            return data?.Definition ?? throw new InvalidOperationException("조회한 아티팩트 정의 값이 필요합니다.");
        }

        private void RequireInitialized()
        {
            if (!_read.Current.IsInitialized)
                throw new InvalidOperationException("아티팩트 보드를 먼저 초기화해야 합니다.");
        }

        private void RequireReady()
        {
            if (_definitions == null || _read == null || _initialization == null || _edits == null || _previews == null || _combat == null)
                throw new InvalidOperationException("아티팩트 서비스는 DB 초기화와 서비스 바인딩이 필요합니다.");
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<IArtifactEditingService>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }
}
