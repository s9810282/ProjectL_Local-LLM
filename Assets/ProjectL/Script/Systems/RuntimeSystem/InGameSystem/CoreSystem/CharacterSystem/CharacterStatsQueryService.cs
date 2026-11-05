using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.ServiceKeys.Runtime;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem
{
    /// <summary>
    /// CharacterBuffer의 보유 원본과 DB 정의로 배치 전 능력치를 조회 시점에 계산한다. 쓰기 Sink·ID 발급·드로우·seed를 건드리지 않는다.
    /// DB 테이블은 조회할 때 읽는다. 등록만으로 다른 경로의 DB 조회 범위를 넓히지 않기 위해서다.
    /// 순차 실행 경계에서 동기로 호출한다. 진행 중인 게시 트랜잭션 안이나 다른 스레드에서 호출하지 않는다.
    /// </summary>
    internal sealed class CharacterStatsQueryService : ICharacterStatsReadSink,
        ISystemInitData<IDataBaseQueryContext>, ISystemPostBindService, IExportSystemServices
    {
        private IInGameQuery _query;
        private IBufferQuery _buffers;

        public void InitData(IDataBaseQueryContext data)
        {
            _query = data?.InGameQuery ?? throw new ArgumentNullException(nameof(data));
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            _buffers = resolver.GetService(BufferSystemKey.Buffer);
        }

        public CharacterStatsQueryResult ReadOwned()
        {
            if (_query == null || _buffers == null)
                throw new InvalidOperationException("보유 능력치 조회는 DB 초기화와 서비스 바인딩이 필요합니다.");

            RunOutcome outcome = _buffers.RunProgressReadSink.Outcome;
            if (_buffers.CombatStateReadSink.HasBattle)
                return Empty(CharacterStatsQueryStatus.BattleActive, outcome);

            // 보유 목록·영혼·메인 지속 상태를 같은 호출에서 캡처한다.
            ICharacterBufferReadSink characters = _buffers.CharacterReadSink;
            OwnedCharacterRecord[] owned = characters.Owned.ToArray();
            if (owned.Length == 0)
                return Empty(CharacterStatsQueryStatus.EmptyRoster, outcome);

            OwnedStatsProjection.ReadPersistent(characters, owned, out var souls, out var mains);
            var items = OwnedStatsProjection.Calculate(_query, owned, mains, souls);
            return new CharacterStatsQueryResult(CharacterStatsQueryStatus.Ready, outcome, items);
        }

        private static CharacterStatsQueryResult Empty(CharacterStatsQueryStatus status, RunOutcome outcome) =>
            new(status, outcome, Array.Empty<OwnedCharacterStatsSnapshot>());

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ICharacterStatsReadSink>(this);
            sink.AddModule<ISystemModule>(this);
        }
    }

    /// <summary>
    /// 보유 능력치 계산의 단일 구현이다. 보유 능력치 조회와 덱 편집 표시 조회가 함께 쓴다.
    /// 호출자가 캡처한 불변 보유·영혼·메인 원본만으로 공용 능력치 계산기를 호출한다. 덱 규칙 판정에는 쓰지 않는다.
    /// </summary>
    internal static class OwnedStatsProjection
    {
        /// <summary>보유 목록에 대응하는 영혼 수와 ID별 메인 지속 상태를 읽는다. 메인 상태가 없으면 설정 오류다.</summary>
        internal static void ReadPersistent(ICharacterBufferReadSink characters, IReadOnlyList<OwnedCharacterRecord> owned,
            out IReadOnlyDictionary<CharacterInstanceId, int> souls,
            out IReadOnlyDictionary<CharacterInstanceId, MainCharacterPersistentState> mains)
        {
            var soulCounts = new Dictionary<CharacterInstanceId, int>(owned.Count);
            var mainStates = new Dictionary<CharacterInstanceId, MainCharacterPersistentState>();
            foreach (var record in owned)
            {
                soulCounts.Add(record.Id, characters.GetSoulCount(record.Id));
                if (record.Role != CharacterOwnershipRole.Main)
                    continue;
                if (!characters.TryGetMainState(record.Id, out var state))
                    throw new InvalidOperationException("보유 메인 지속 상태가 필요합니다.");
                mainStates.Add(record.Id, state);
            }
            souls = soulCounts;
            mains = mainStates;
        }

        internal static IReadOnlyList<OwnedCharacterStatsSnapshot> Calculate(IInGameQuery query,
            IReadOnlyList<OwnedCharacterRecord> owned,
            IReadOnlyDictionary<CharacterInstanceId, MainCharacterPersistentState> mains,
            IReadOnlyDictionary<CharacterInstanceId, int> souls)
        {
            var profiles = owned.Select(record => record.CharacterKey).Distinct()
                .Select(key => UnitSpecAdapter.ReadProfile(query, UnitSpecAdapter.ReadUnit(query.UnitTableQuery.GetUnit(key)))).ToArray();
            var config = query.CategoryConfigTableQuery.Config;
            var categories = UnitSpecAdapter.ReadCategories(config);
            var growth = query as IHasTraitGrowthTables;
            var catalog = TraitGrowthDefinitionAdapter.Build(growth?.CategoryTraitProgressions,
                growth?.TraitGrowthDefinitions, config.DefaultTraits);
            return OwnedCharacterStatsCalculator.CalculateForMains(profiles, categories, owned, mains, souls, catalog);
        }
    }
}
