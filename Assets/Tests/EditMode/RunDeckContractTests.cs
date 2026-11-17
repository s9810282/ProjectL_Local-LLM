using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Tests.Core
{
    public sealed class RunDeckContractTests
    {
        private static readonly CharacterInstanceId MainId = new CharacterInstanceId(1);
        private static readonly RunId Run = new RunId(Guid.Parse("a66202db-72ee-4881-902f-4502e3b7939d"));

        private static DeckEditorSnapshot Editor(long roster = 2, long revision = 3)
        {
            return new DeckEditorSnapshot(roster, new RunDeckSnapshot(Run, revision, new[] { MainId }),
                Owned(), Definitions(), Stats(), new DeckEditAvailability(DeckUnavailableReason.None));
        }

        private static DeckRuleState Rule(long roster = 2, long revision = 3, params CharacterInstanceId[] ids) =>
            DeckRuleState.Ready(roster, new RunDeckSnapshot(Run, revision, ids.Length == 0 ? new[] { MainId } : ids),
                new DeckEditAvailability(DeckUnavailableReason.None));

        private static OwnedCharacterRecord[] Owned() => new[]
        {
            new OwnedCharacterRecord(MainId, EntityKey.Unit_Akimbo, CharacterOwnershipRole.Main),
        };
        private static Dictionary<EntityKey, CharacterDefinitionFacts> Definitions() => new Dictionary<EntityKey, CharacterDefinitionFacts>
        {
            [EntityKey.Unit_Akimbo] = new CharacterDefinitionFacts(EntityKey.Unit_Akimbo, UnitRole.MainCharacter,
                UnitOwnerEligibility.PlayerOnly, category: CategoryKey.Main),
        };
        private static OwnedCharacterStatsSnapshot[] Stats() => new[]
        {
            new OwnedCharacterStatsSnapshot(MainId, EntityKey.Unit_Akimbo, CharacterOwnershipRole.Main, 1, new ExperienceProgressSnapshot(false, 0, 0), 0,
                new CharacterBaselineStatsSnapshot(10,10,2,0,MoveMethod.LiftAndPlace,3,1,10,CategoryKey.Main,Array.Empty<TraitKey>()),
                new AttackPatternSnapshot(UnitSpecTestFactory.DefaultMethod(), Array.Empty<CellCoord>(), Array.Empty<CellCoord>()),
                TraitSettingsSet.Empty),
        };

        [Test]
        public void DeckSnapshot_CopiesInputsAndDerivesMain()
        {
            var ids = new[] { MainId };
            var deck = new RunDeckSnapshot(Run, 3, ids);
            ids[0] = new CharacterInstanceId(9);
            Assert.That(deck.OrderedIds, Is.EqualTo(new[] { MainId }));
            Assert.That(deck.MainId, Is.EqualTo(MainId));
            Assert.Throws<NotSupportedException>(() => ((IList<CharacterInstanceId>)deck.OrderedIds)[0] = ids[0]);
        }

        [Test]
        public void StructuralContracts_RejectInvalidIdentityRevisionAndIds()
        {
            Assert.Throws<ArgumentException>(() => new RunDeckSnapshot(RunId.Empty, 0, new[] { MainId }));
            Assert.Throws<ArgumentOutOfRangeException>(() => new RunDeckSnapshot(Run, -1, new[] { MainId }));
            Assert.Throws<ArgumentException>(() => new RunDeckSnapshot(Run, 0, Array.Empty<CharacterInstanceId>()));
            Assert.Throws<ArgumentException>(() => new RunDeckSnapshot(Run, 0, new[] { MainId, MainId }));
            Assert.Throws<ArgumentException>(() => new RunDeckSnapshot(Run, 0, new[] { CharacterInstanceId.Empty }));
        }

        [Test]
        public void RequestAndDecision_CopyArraysWithoutRebasing()
        {
            var ids = new[] { MainId };
            var request = new DeckEditRequest(Run, 2, 3, ids);
            var decision = new DeckSelectionDecision(DeckFailureReason.None, ids);
            var evaluation = new DeckEvaluationResult(request, DeckFailureReason.None, Rule(), ids);
            ids[0] = new CharacterInstanceId(9);
            Assert.That(request.CandidateIds, Is.EqualTo(new[] { MainId }));
            Assert.That(decision.CanonicalIds, Is.EqualTo(new[] { MainId }));
            Assert.That(evaluation.CanonicalIds, Is.EqualTo(new[] { MainId }));
            Assert.That(evaluation.Request, Is.SameAs(request));
            Assert.That(request.ExpectedRosterRevision, Is.EqualTo(2));
            Assert.That(request.ExpectedDeckRevision, Is.EqualTo(3));
            Assert.That(new DeckEditRequest(Run, 2, 3, Array.Empty<CharacterInstanceId>()).CandidateIds, Is.Empty);
        }

        [Test]
        public void EditorSnapshot_CopiesEveryCollectionAndUsesOneTuple()
        {
            var owned = Owned();
            var definitions = Definitions();
            var stats = Stats();
            var snapshot = new DeckEditorSnapshot(2, new RunDeckSnapshot(Run, 3, new[] { MainId }),
                owned, definitions, stats, new DeckEditAvailability(DeckUnavailableReason.None));
            owned[0] = null;
            definitions.Clear();
            stats[0] = null;
            Assert.That(snapshot.Owned[0], Is.Not.Null);
            Assert.That(snapshot.Owned[0].Id, Is.EqualTo(MainId));
            Assert.That(snapshot.Definitions.Count, Is.EqualTo(1));
            Assert.That(snapshot.Stats[0].OwnedId, Is.EqualTo(MainId));
            Assert.That(snapshot.RunId, Is.EqualTo(snapshot.Deck.RunId));
            Assert.That(snapshot.DeckRevision, Is.EqualTo(snapshot.Deck.DeckRevision));
            Assert.Throws<NotSupportedException>(() => ((IDictionary<EntityKey, CharacterDefinitionFacts>)snapshot.Definitions).Clear());
        }

        [Test]
        public void ReadyRequiresSnapshot_AndUnreadyCannotInventOne()
        {
            Assert.Throws<ArgumentException>(() => new DeckReadResult(DeckReadStatus.Ready));
            Assert.Throws<ArgumentException>(() => new DeckReadResult(DeckReadStatus.StateChanged, Editor()));
            Assert.That(new DeckReadResult(DeckReadStatus.NotReady).Snapshot, Is.Null);
            Assert.That(new DeckReadResult(DeckReadStatus.StateChanged).Snapshot, Is.Null);
            Assert.That(new DeckEditAvailability(DeckUnavailableReason.JobPending).CanEdit, Is.False);
        }

        [Test]
        public void Evaluation_RejectsAnUnrelatedVersionTuple()
        {
            var request = new DeckEditRequest(Run, 2, 3, new[] { MainId });
            var otherRunRequest = new DeckEditRequest(new RunId(Guid.NewGuid()), 2, 3, new[] { MainId });
            Assert.Throws<ArgumentException>(() => new DeckEvaluationResult(otherRunRequest, DeckFailureReason.None,
                Rule(), new[] { MainId }));
            Assert.Throws<ArgumentException>(() => new DeckEvaluationResult(request, DeckFailureReason.None,
                Rule(roster: 4), new[] { MainId }));
            Assert.Throws<ArgumentException>(() => new DeckEvaluationResult(request, DeckFailureReason.None,
                Rule(revision: 4), new[] { MainId }));
            var rejected = new DeckEvaluationResult(request, DeckFailureReason.StaleRoster, Rule(roster: 4));
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.CanonicalIds, Is.Null);
            Assert.That(rejected.Request.ExpectedRosterRevision, Is.EqualTo(2));
        }

        [Test]
        public void RejectedDecisionsCannotCarryAcceptedIds_AndAcceptedEditNeedsReadyState()
        {
            var request = new DeckEditRequest(Run, 2, 3, new[] { MainId });
            Assert.Throws<ArgumentException>(() => new DeckSelectionDecision(DeckFailureReason.MissingMain, new[] { MainId }));
            Assert.Throws<ArgumentException>(() => new DeckEditResult(request, DeckFailureReason.None,
                DeckRuleState.Unavailable(DeckReadStatus.NotReady)));
            var rejected = new DeckEditResult(request, DeckFailureReason.NotReady, DeckRuleState.Unavailable(DeckReadStatus.NotReady));
            Assert.That(rejected.Accepted, Is.False);
            Assert.That(rejected.Request, Is.SameAs(request));
        }

        [Test]
        public void EditResult_AcceptsPostCommitDeck_NotTheOldRequestTuple()
        {
            var sub = new CharacterInstanceId(2);
            var request = new DeckEditRequest(Run, 2, 3, new[] { sub, MainId });
            // 바뀐 덱: 게시 후 덱 버전이 커진다. 요청의 기대 덱 버전과 같을 필요가 없다.
            var changed = new DeckEditResult(request, DeckFailureReason.None, Rule(2, 4, MainId, sub));
            Assert.That(changed.Current.Deck.DeckRevision, Is.EqualTo(4));
            // 같은 덱(no-op): 같은 덱 버전이 허용된다.
            Assert.That(new DeckEditResult(request, DeckFailureReason.None, Rule(2, 3, MainId, sub)).Accepted, Is.True);
            // 게시 후 원본이 요청 원본에서 이어지지 않으면 거절한다.
            Assert.Throws<ArgumentException>(() => new DeckEditResult(request, DeckFailureReason.None, Rule(2, 2, MainId, sub)));
            Assert.Throws<ArgumentException>(() => new DeckEditResult(request, DeckFailureReason.None, Rule(5, 4, MainId, sub)));
            Assert.Throws<ArgumentException>(() => new DeckEditResult(request, DeckFailureReason.None, Rule(2, 4, MainId)));
            var otherRun = new DeckEditRequest(new RunId(Guid.NewGuid()), 2, 3, new[] { sub, MainId });
            Assert.Throws<ArgumentException>(() => new DeckEditResult(otherRun, DeckFailureReason.None, Rule(2, 4, MainId, sub)));
        }

        [Test]
        public void RuleState_ReadyCarriesDeckAndAvailability_UnavailableCarriesNothing()
        {
            var ready = Rule();
            Assert.That(ready.Status, Is.EqualTo(DeckReadStatus.Ready));
            Assert.That(ready.Deck.MainId, Is.EqualTo(MainId));
            Assert.That(ready.Availability.CanEdit, Is.True);
            var unavailable = DeckRuleState.Unavailable(DeckReadStatus.StateChanged);
            Assert.That(unavailable.Deck, Is.Null);
            Assert.That(unavailable.Availability, Is.Null);
            Assert.That(unavailable.RosterRevision, Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(() => DeckRuleState.Unavailable(DeckReadStatus.Ready));
            Assert.Throws<ArgumentNullException>(() => DeckRuleState.Ready(1, null, new DeckEditAvailability(DeckUnavailableReason.None)));
            Assert.Throws<ArgumentNullException>(() => DeckRuleState.Ready(1, new RunDeckSnapshot(Run, 0, new[] { MainId }), null));
            Assert.Throws<ArgumentOutOfRangeException>(() => DeckRuleState.Ready(-1, new RunDeckSnapshot(Run, 0, new[] { MainId }),
                new DeckEditAvailability(DeckUnavailableReason.None)));
        }
    }
}
