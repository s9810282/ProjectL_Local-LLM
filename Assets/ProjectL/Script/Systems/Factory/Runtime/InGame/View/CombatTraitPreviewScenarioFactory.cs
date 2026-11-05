#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview.Scenario;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview.Scenario.Blocks;

namespace ProjectL.Systems.Factory.Runtime.InGame.View
{
    /// <summary>블록 작성기와 상태 변환기만 조립합니다. 전역 서비스는 등록하지 않습니다.</summary>
    internal static class CombatTraitPreviewScenarioFactory
    {
        internal static PreviewScenarioCompiler Create()
        {
            var states = new PreviewStateWriters(new IPreviewStateWriter[] { new PreviewAccelerationWriter(), new PreviewEffectWriter(), new PreviewCohesionWriter(), new PreviewRegenerationWriter() });
            var traits = new PreviewTraitWriter();
            var attack = new PreviewAttackWriter(traits);
            var movement = new PreviewMovementWriter();
            var writers = new Dictionary<PreviewBlockKind, Action<PreviewPacketContext, PreviewScenarioBlock>>();
            var registry = new PreviewScenarioWriters(states, writers);
            writers.Add(PreviewBlockKind.Movement, movement.Write);
            writers.Add(PreviewBlockKind.Attack, attack.Write);
            writers.Add(PreviewBlockKind.ChildAttack, attack.Write);
            writers.Add(PreviewBlockKind.State, registry.WriteState);
            writers.Add(PreviewBlockKind.Activation, traits.Write);
            writers.Add(PreviewBlockKind.FieldPlacement, PreviewFieldPlacementWriter.Write);
            writers.Add(PreviewBlockKind.Health, PreviewFactWriter.Health);
            writers.Add(PreviewBlockKind.Return, PreviewFactWriter.Return);
            // 반응은 공격 작성기가 접촉보다 먼저 수집합니다. 독립 사건으로 중복 작성하지 않습니다.
            writers.Add(PreviewBlockKind.Interception, (packet, block) =>
            {
            });
            return new PreviewScenarioCompiler(registry, CombatTraitFeedbackFactory.CreateEvidence());
        }
    }
}
#endif
