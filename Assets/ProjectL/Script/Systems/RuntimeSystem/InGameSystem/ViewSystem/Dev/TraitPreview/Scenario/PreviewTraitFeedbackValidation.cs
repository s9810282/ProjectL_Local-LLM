#if UNITY_EDITOR
using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Text;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.Trait.Item;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Dev.TraitPreview.Scenario
{
    /// <summary>선택한 외형과 운영의 사실 대응 계획을 사용해 재생 전 누락을 알립니다.</summary>
    internal static class PreviewTraitFeedbackValidation
    {
        internal static void Require(CombatResult result, PreviewScenarioEntry entry, CombatTraitStateEvidenceRegistry evidence)
        {
            if (result == null)
                return;
            var visuals = new Dictionary<TraitKey, TraitVisualData>();
            foreach (var block in entry.blocks)
                if (block.kind is PreviewBlockKind.Activation or PreviewBlockKind.ChildAttack or PreviewBlockKind.Interception && block.trait.asset is TraitVisualDataSO so)
                {
                    var visual = (TraitVisualData)so.GetData();
                    visual.Validate();
                    // 방향 줄(결속 오름·내림)도 몸 글자 프리팹을 가질 수 있어 그 종류의 줄을 모두 봅니다.
                    foreach (TraitActivationVisual activation in visual.Activations)
                        if (activation.Kind == block.trait.kind && activation.BodyText?.PrefabOverride != null)
                            CombatBodyTextView.RequirePrefab(activation.BodyText.PrefabOverride);
                    visuals[visual.Key] = visual;
                }

            // 운영 줄 보관(CombatActivationRows.Of)과 같이 방향 줄이 없으면 Any 줄로 갑니다(묶음 끝 검토 L2 — 결속 방향 줄도 검사).
            CombatActivationRow Row(CoreSystem.CoreCaculator.Contracts.Snapshots.Traits.TraitActivationSnapshot trait, CombatActivationDirection direction) =>
                visuals.TryGetValue(trait.TraitKey, out var visual) &&
                (visual.TryGetActivation(trait.TraitActivationKind, direction, out var row) || visual.TryGetActivation(trait.TraitActivationKind, out row))
                    ? new CombatActivationRow(row.Cues, row.AttackPresentation, row.UnitActions, row.CueTiming, row.BodyText, visual.DisplayName)
                    : default;
            var plan = CombatTraitFeedbackPlan.Build(result, Row, evidence);
            if (plan.Issues.Count > 0)
                throw new ArgumentException($"묶음 '{entry.name}': {string.Join("\n", plan.Issues)}");
        }
    }
}
#endif
