#if UNITY_EDITOR
using System;
using System.Linq;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview.Scenario.Blocks
{
    internal sealed class PreviewTraitWriter
    {
        internal TraitActivationSnapshot Create(PreviewScenarioLedger ledger, PreviewTraitInput input, CombatDamageOrigin origin, PreviewAttackInput attack = null)
        {
            if (input == null || input.asset is not TraitVisualDataSO asset)
                throw new ArgumentException("특성 외형 SO가 필요합니다.");
            PreviewTraitRegistration.Require(asset);
            var visual = (TraitVisualData)asset.GetData();
            visual.Validate();
            var source = ledger.Alive(input.sourceId);
            if (input.targetIds == null || input.targetIds.Distinct().Count() != input.targetIds.Count)
                throw new ArgumentException("발동 대상 목록이 없거나 중복됩니다.");
            var targets = input.targetIds.Select(id => ledger.Get(id).EntityId).ToArray();
            if (input.cells == null)
                throw new ArgumentException("발동 칸 목록이 없습니다.");
            return new TraitActivationSnapshot(source.EntityId, null, visual.Key, input.kind, input.magnitude, targets, input.cells.Select(c => PreviewScenarioLedger.Cell(c.x, c.y)).ToArray(), origin, attack?.direction, attack == null ? null : PreviewScenarioLedger.Cell(attack.targetCell.x, attack.targetCell.y));
        }

        internal void Write(PreviewPacketContext packet, PreviewScenarioBlock block)
        {
            var activation = Create(packet.Ledger, block.trait, block.relation == PreviewEventRelation.AttackHit ? packet.Origin : null);
            packet.Events.Add(new CombatEffectsEvent(new[] { activation }, Array.Empty<CategoryActivationSnapshot>()));
        }
    }
}
#endif
