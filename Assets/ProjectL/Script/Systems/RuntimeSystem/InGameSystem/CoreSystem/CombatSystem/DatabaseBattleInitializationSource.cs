using ProjectL.Systems.ServiceKeys.Runtime;
using System;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase;
using ProjectL.Systems.DataBaseSystem.Queries.InGameDataBase.InGameTable;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    /// <summary>
    /// 운영 전투 정의 공급원이다. 지정된 대상과 보유 로스터로 DB 정의를 읽고, 유닛의 공격 지정 방식으로 공격 프로필을 붙인다.
    /// Buffer에 쓰지 않으며, 설치는 전투 준비가 담당한다.
    /// </summary>
    internal sealed class DatabaseBattleInitializationSource : IBattleInitializationSource, ISystemPostBindService
    {
        private ICharacterBufferReadSink _characters;
        private readonly IConsumableItemTableQuery _items;

        internal DatabaseBattleInitializationSource(IConsumableItemTableQuery items = null)
        {
            _items = items;
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            var buffers = resolver.GetService(BufferSystemKey.Buffer);
            _characters = buffers.CharacterReadSink;
        }

        public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
        {
            if (chapterKey < 1)
                throw new ArgumentOutOfRangeException(nameof(chapterKey));
            if (stageKey.IsEmpty)
                throw new ArgumentException("스테이지 키가 필요합니다.", nameof(stageKey));
            if (_characters == null)
                throw new InvalidOperationException("G7: 전투 정의 조회는 Buffer 서비스 바인딩이 필요합니다.");
            var items = _items ?? (database?.InGameQuery as IHasConsumableItemTable)?.ConsumableItems;
            BattleInitialization definition = BattleDefinitionAdapter.Read(database, chapterKey, stageKey.Value, _characters.Owned,
                items == null ? null : ItemDefinitionAdapter.Build(items.GetAll()));

            return definition.WithAttackProfiles(BattleDefinitionAdapter.ReadAttackProfiles(database, definition.Units));
        }
    }
}
