#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.Factory.Runtime.Development
{
    /// <summary>기존 DB 변환기를 사용하고 임시 유닛의 지정 방식만 명시한다. 실제 DB 실패 시의 대체 경로가 아니다.</summary>
    internal sealed class TemporaryBattleInitializationSource : IBattleInitializationSource
    {
        private readonly ICharacterBufferReadSink _characters;

        internal TemporaryBattleInitializationSource(ICharacterBufferReadSink characters)
            => _characters = characters ?? throw new ArgumentNullException(nameof(characters));

        public BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey)
        {
            if (database is not TemporaryCoreDatabase)
                throw new InvalidOperationException("임시 전투 공급원은 임시 Core 데이터에만 사용할 수 있습니다.");
            var definition = BattleDefinitionAdapter.Read(database, chapterKey, stageKey.Value, _characters.Owned);
            var profiles = definition.Units.Select(unit => {
                if (unit.Key != EntityKey.Unit_MainCharacter && unit.Key != EntityKey.Unit_Akimbo)
                    throw new InvalidOperationException("임시 지정 방식을 정의하지 않은 유닛입니다.");
                return new CombatAttackProfile(unit, new TargetingMethodSpec(
                    TargetingShape.RadialSquare, TargetingChoiceMode.Single, TargetingSplash.None, 0));
            }).ToArray();
            return definition.WithAttackProfiles(profiles);
        }
    }
}
#endif
