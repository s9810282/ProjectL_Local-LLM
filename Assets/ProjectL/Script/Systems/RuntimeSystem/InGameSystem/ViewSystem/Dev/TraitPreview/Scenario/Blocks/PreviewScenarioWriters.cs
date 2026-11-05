#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview.Scenario.Blocks
{
    /// <summary>등록된 작성기를 선택합니다. 상태의 구체 변환과 재생 수명은 소유하지 않습니다.</summary>
    internal sealed class PreviewScenarioWriters : IPreviewScenarioWriter
    {
        private readonly IReadOnlyDictionary<PreviewBlockKind, Action<PreviewPacketContext, PreviewScenarioBlock>> _writers;
        private readonly PreviewStateWriters _states;
        internal PreviewScenarioWriters(PreviewStateWriters states, IReadOnlyDictionary<PreviewBlockKind, Action<PreviewPacketContext, PreviewScenarioBlock>> writers)
        {
            _states = states;
            _writers = writers;
        }

        public void Initialize(PreviewScenarioLedger ledger, PreviewScenarioUnit unit) => _states.Initialize(ledger, unit);
        public void Write(PreviewPacketContext packet, PreviewScenarioBlock block)
        {
            if (!_writers.TryGetValue(block.kind, out var writer))
                throw new ArgumentException($"지원하지 않는 블록: {block.kind}");
            writer(packet, block);
        }

        internal void WriteState(PreviewPacketContext packet, PreviewScenarioBlock block)
        {
            var before = packet.Ledger.Alive(block.state.unitId);
            _states.Apply(packet.Ledger, block.state);
            packet.Events.Add(new CombatTraitStateEvent(before, packet.Ledger.Get(block.state.unitId), block.relation == PreviewEventRelation.AttackHit ? packet.Origin : null));
        }
    }
}
#endif
