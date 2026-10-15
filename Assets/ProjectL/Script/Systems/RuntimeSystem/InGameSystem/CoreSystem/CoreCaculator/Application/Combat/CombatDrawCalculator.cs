using System;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.BootstrapModels;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    public static class CombatDrawCalculator
    {
        public static CombatDrawAvailability Availability(CombatCommandInput input)
        {
            var b = CombatTurnCalculator.RequiredBattle(input);
            int cost = b.ManualDrawGoldCost ?? throw new InvalidOperationException("수동 드로우 비용 정의가 연결되지 않았습니다.");
            int remaining = b.DrawOrder.Count - b.DrawCursor;
            var reason = input.State.Phase == CombatFlowPhase.Finished ? CharacterDrawFailureReason.StageEnded
                : input.State.Turn.StagePhase != StagePhase.Deployment ? CharacterDrawFailureReason.WrongPhase
                : input.State.Turn.PreparationIndex < 2 ? CharacterDrawFailureReason.FirstPreparation
                : remaining == 0 ? CharacterDrawFailureReason.DeckEmpty
                : input.Characters.Count(c => c.Owner == Owner.Player && c.OwnershipRole == CharacterOwnershipRole.Sub && !c.Health.IsDead &&
                    (c.Area == CharacterRuntimeArea.Field || c.Area == CharacterRuntimeArea.Hand)) >= b.ReserveCapacity
                    ? CharacterDrawFailureReason.LivingSubLimitReached : CharacterDrawFailureReason.None;
            int slot = CombatPlacementCalculator.EmptySlot(input, input.Characters);
            if (reason == CharacterDrawFailureReason.None && slot < 0) reason = CharacterDrawFailureReason.ReserveFull;
            if (reason == CharacterDrawFailureReason.None && b.Gold < cost) reason = CharacterDrawFailureReason.InsufficientGold;
            return new CombatDrawAvailability(reason, remaining, cost, reason == CharacterDrawFailureReason.None ? slot : null);
        }

        internal static CombatCommandTransition Calculate(CombatCommandInput input)
        {
            var query = Availability(input);
            if (!query.CanDraw) return CombatCommandCalculator.Reject(input, query.FailureReason.ToString());
            var b = input.Battle;
            var record = input.Owned.Single(o => o.Id == b.DrawOrder[b.DrawCursor]);
            if (record.Role != CharacterOwnershipRole.Sub || input.Characters.Any(c => c.OwnedId == record.Id))
                throw new InvalidOperationException("드로우할 보유 캐릭터 연결이 잘못되었습니다.");
            var traits = TraitRegistry.CreateDefault();
            var specs = new[] { input.Profiles.Single(p => p.Definition.Key == record.CharacterKey).Definition };
            var catalog = b.Categories == null ? new UnitCatalog(specs, traits) : new UnitCatalog(specs, traits, new CategoryRegistry(b.Categories));
            var definition = catalog.GetRequired(record.CharacterKey);
            if (definition.Role != UnitRole.Regular || definition.OwnerEligibility == UnitOwnerEligibility.EnemyOnly)
                throw new InvalidOperationException("드로우할 정의가 플레이어 일반 유닛이 아닙니다.");
            var created = BattleCharacterCalculator.Create(new EntityId(checked((int)b.NextEntityId)), definition, Owner.Player, record, null,
                input.SoulCounts[record.Id], query.Slot.Value, null, null, Array.Empty<CellCoord>(), traits);
            var draw = new CombatDrawSettlement(created.EntityId, created.CharacterKey, query.Slot.Value,
                b.DrawCursor, b.NextEntityId, b.Gold, query.GoldCost);
            return new CombatCommandTransition(input, true, "",
                new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>(), created: new[] { created }),
                new SoulFieldChange(input.Souls, input.Souls), input.State,
                new Contracts.Snapshots.StageOutcomeSnapshot(StageOutcome.InProgress, StageOutcomeReason.None), draw: draw, events: new[] { new CombatCharacterEvent(CombatEventKind.Drawn, null, created) });
        }
    }
}
