using System.Linq;
using NUnit.Framework;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Tests.CoreIntegration
{
    internal sealed class CoreIntegrationSnapshot
    {
        private readonly object[] _stores;

        internal CoreIntegrationSnapshot(IBufferQuery buffers) => _stores = CaptureStores(buffers);

        internal void AssertUnchanged(IBufferQuery buffers) => Assert.That(CaptureStores(buffers), Is.EqualTo(_stores));

        // 불변 조회값과 목록 사본을 비교한다. 무작업·거래 거절은 어떤 저장소도 바꾸지 않아야 한다.
        internal static object[] CaptureGameplay(IBufferQuery b)
        {
            var owned = b.CharacterReadSink.Owned.ToArray();
            var mains = owned.Where(c => c.Role == CharacterOwnershipRole.Main).Select(c =>
            {
                Assert.That(b.CharacterReadSink.TryGetMainState(c.Id, out var state), Is.True);
                return state;
            }).ToArray();
            var membership = b.CombatStateReadSink.Current?.Membership;
            object capturedMain = null;
            if (membership != null)
            {
                Assert.That(b.CharacterReadSink.TryGetMainState(membership.MainId, out var state), Is.True);
                capturedMain = state;
            }
            b.RunDeckReadSink.TryGetCurrent(out var deck);
            return new object[]
            {
                b.CombatStateReadSink.HasBattle, b.CombatStateReadSink.BattleInstanceId,
                b.CombatStateReadSink.QueryRevision,
                b.CombatStateReadSink.Current, b.CombatStateReadSink.SoulField, b.CombatStateReadSink.Completion,
                b.CharacterReadSink.Characters.ToArray(), owned, mains, b.CharacterReadSink.RosterRevision,
                owned.Select(c => b.CharacterReadSink.GetSoulCount(c.Id)).ToArray(),
                b.ItemInventoryReadSink.Current, b.ArtifactReadSink.Current, deck, membership, membership?.MainId, capturedMain,
                b.CurrencyReadSink.Amount, b.CurrencyReadSink.RunSpent, b.FameReadSink.Amount, b.FameReadSink.RunEarned,
                b.RunProgressReadSink.RunId, b.RunProgressReadSink.Outcome, b.RunProgressReadSink.Completion,
                b.RunProgressReadSink.LastSummary, b.RunProgressReadSink.EnemiesDefeated, b.RunProgressReadSink.StartingHandSize,
                b.CurrentProgressReadSink.HasMap, b.CurrentProgressReadSink.CurrentMapKey,
                b.CurrentProgressReadSink.HasSelection, b.CurrentProgressReadSink.CurrentStage,
                b.CurrentProgressReadSink.Shop, b.CurrentProgressReadSink.Recovery,
                b.MapProgressReadSink.Maps.ToArray(),
                b.MapProgressReadSink.Maps.Select(m => b.StageProgressReadSink.GetStages(m.MapKey).ToArray()).ToArray()
            };
        }

        internal static object[] CaptureStores(IBufferQuery b)
        {
            b.CombatStateReadSink.TryGetLatestPreview(out var preview);
            b.CombatResultReadSink.TryGetLatest(out var result);
            return new object[]
            {
                CaptureGameplay(b), b.CombatStateReadSink.LatestResultSequence, b.CombatStateReadSink.HandledCommandNumber,
                b.CombatStateReadSink.AnsweredPreviewNumber, preview, result, b.CombatResultHistoryReadSink.Results.ToArray(),
                b.CombatViewReadSink.Commands.ToArray(), b.CombatViewReadSink.PresentedResultSequence,
                b.CombatViewReadSink.HasPreview, b.CombatViewReadSink.Preview
            };
        }
    }
}
