using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Artifact.Item;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.ServiceKeys.Runtime;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>
    /// 배치된 Artifact 원본과 DB 정의를 결합해 전투 계산용 효과 목록을 만든다.
    /// 전투 입력 조립과 포기 정산이 같은 변환을 사용한다. Buffer에 쓰지 않는다.
    /// </summary>
    internal sealed class ArtifactLoadoutReader : ISystemInitData<IDataBaseQueryContext>, ISystemPostBindService
    {
        private IDataBaseQueryContext _database;
        private IArtifactBufferReadSink _artifacts;

        internal object Configuration => _database;

        internal ArtifactDefinitionData ReadDefinition(int artifactId)
        {
            if (_database == null)
                throw new InvalidOperationException("배치 Artifact 정의에는 DB 초기화가 필요합니다.");
            return _database.InGameQuery.ArtifactDefinitionTableQuery.GetDefinition(artifactId);
        }

        public void InitData(IDataBaseQueryContext data)
        {
            _database = data ?? throw new ArgumentNullException(nameof(data));
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            _artifacts = resolver.GetService(BufferSystemKey.Buffer).ArtifactReadSink;
        }

        /// <summary>배치 인스턴스 ID 순서로 각 정의의 효과를 펼친다. 정의가 없으면 아무것도 바꾸지 않고 예외로 드러난다.</summary>
        public ArtifactLoadoutSnapshot Read()
        {
            if (_artifacts == null)
                throw new InvalidOperationException("배치 Artifact 조회는 Buffer 바인딩이 필요합니다.");
            var effects = new List<ArtifactPlacedEffectSnapshot>();
            foreach (var item in _artifacts.Current.PlacedItems.OrderBy(i => i.InstanceId))
            {
                var definition = ReadDefinition(item.ArtifactId);
                foreach (var effect in definition.Effects)
                    effects.Add(new ArtifactPlacedEffectSnapshot(item.ArtifactId, item.InstanceId, effect));
            }
            return new ArtifactLoadoutSnapshot(0, effects.ToArray());
        }
    }
}
