using System;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Entities;

namespace ProjectL.Tests.Core
{
    /// <summary>
    /// Buffer 계약 지식 이전 계획 B-1: 메인 지속 상태의 값 비교(SameValues)와 전투 사실 대응(MatchesMainState).
    /// 대응 규칙을 적용하는 두 곳(기준 능력치 생성, SetPersistentMain)이 같은 대응을 만드는지도 묶는다.
    /// </summary>
    public sealed class MainStateMatchTests
    {
        private static MainCharacterPersistentState State() =>
            new(18, 30, 15, 2);

        [Test]
        public void SameValues_DetectsEachField()
        {
            var state = State();

            Assert.That(state.SameValues(State()), Is.True);
            Assert.That(state.SameValues(state.With(currentHealth: 17)), Is.False);
            Assert.That(state.SameValues(state.With(persistentMaxHealth: 31)), Is.False);
            Assert.That(state.SameValues(state.With(persistentAttack: 16)), Is.False);
            Assert.That(state.SameValues(state.With(persistentDefense: 3)), Is.False);
            Assert.Throws<NullReferenceException>(() => state.SameValues(null));
        }

        [Test]
        public void BaselineMain_MatchesItsPersistentState()
        {
            var state = State();

            Assert.That(CreateMain(state).MatchesMainState(state), Is.True);
        }

        [Test]
        public void MatchesMainState_ChecksMirroredValuesOnly()
        {
            var state = State();
            var character = CreateMain(state);

            Assert.That(character.MatchesMainState(state.With(currentHealth: 17)), Is.False);
            Assert.That(character.MatchesMainState(state.With(persistentAttack: 16)), Is.False);
            Assert.That(character.MatchesMainState(state.With(persistentDefense: 3)), Is.False);
            // 지속 최대 체력은 영혼·보정이 더해진 뒤 전투 사실에 들어가므로 같은 값으로 비치지 않는다.
            Assert.That(character.MatchesMainState(state.With(persistentMaxHealth: 31)), Is.True);
            Assert.Throws<ArgumentNullException>(() => character.MatchesMainState(null));
        }

        [Test]
        public void SetPersistentMain_ResultMatchesFinalState()
        {
            var expected = State();
            var character = CreateMain(expected);
            var final = expected.With(persistentAttack: 20, persistentDefense: 3);

            var changes = CharacterSettlementCalculator.SetPersistentMain(character, expected, final);

            Assert.That(changes.Characters[0].Final.MatchesMainState(final), Is.True);
            Assert.That(changes.Characters[0].Final.MatchesMainState(expected), Is.False);
            Assert.That(changes.Characters[0].Final.Stats.MoveRange, Is.EqualTo(character.Stats.MoveRange));
            Assert.That(changes.Characters[0].Final.ActionUse, Is.EqualTo(character.ActionUse));
        }

        private static CharacterCombatFacts CreateMain(MainCharacterPersistentState state) =>
            BattleCharacterCalculator.CreateOwned(new EntityId(1), MainDefinition(),
                new OwnedCharacterRecord(new CharacterInstanceId(1), EntityKey.Unit_Gravekeeper, CharacterOwnershipRole.Main, 1),
                state, 2, null, CellCoord.Zero, Rotation4.R0, new[] { CellCoord.Zero }, TraitRegistry.CreateDefault());

        // 메인 정의는 1x1·공격 범위 1이어야 한다(UnitDefinition 규칙). CharacterBaselineStatsTests의 정의와 같은 값이다.
        private static UnitDefinition MainDefinition()
        {
            var spec = new UnitSpec(EntityKey.Unit_Gravekeeper, UnitRole.MainCharacter, 1, 1, new[] { CellCoord.Zero }, 10,
                MoveMethod.LiftAndPlace, 1, CategoryKey.Main, 5, 1, 7,
                Array.Empty<TraitSpec>(), UnitOwnerEligibility.PlayerOnly);
            return new UnitCatalog(new[] { spec }, TraitRegistry.CreateDefault(),
                new CategoryRegistry(UnitSpecTestFactory.CreateGrowthCategoryCatalog(default))).GetRequired(EntityKey.Unit_Gravekeeper);
        }
    }
}
