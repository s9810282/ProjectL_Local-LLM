#if UNITY_EDITOR
using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview.Scenario.Blocks
{
    /// <summary>명시한 최종 HP·귀환 사실만 기록합니다. 회복/제거/비용 규칙은 계산하지 않습니다.</summary>
    internal static class PreviewFactWriter
    {
        internal static void Health(PreviewPacketContext packet, PreviewScenarioBlock block)
        {
            var input = block.health ?? throw new ArgumentException("HP 입력이 없습니다.");
            var before = packet.Ledger.Alive(input.unitId);
            var after = before.WithHealth(new HealthStateSnapshot(input.finalHealth, before.Health.MaxHealth));
            packet.Ledger.Set(input.unitId, after);
            packet.Events.Add(new CombatCharacterEvent(CombatEventKind.HealthChanged, before, after));
            if (after.Health.IsDead) packet.Events.Add(new CombatCharacterEvent(CombatEventKind.Died, after, after));
        }
        internal static void Return(PreviewPacketContext packet, PreviewScenarioBlock block)
        {
            var input = block.returned ?? throw new ArgumentException("귀환 입력이 없습니다.");
            if (input.area != CharacterRuntimeArea.Hand && input.area != CharacterRuntimeArea.EnemyWaiting)
                throw new ArgumentException("귀환 목적지는 Hand 또는 EnemyWaiting이어야 합니다.");
            var before = packet.Ledger.Alive(input.unitId);
            var after = before.WithPlacement(input.area, input.area == CharacterRuntimeArea.Hand ? input.reserveSlot : (int?)null, null, null, Array.Empty<CellCoord>());
            packet.Ledger.Set(input.unitId, after);
            packet.Events.Add(new CombatCharacterEvent(CombatEventKind.Returned, before, after));
        }
    }
}
#endif
