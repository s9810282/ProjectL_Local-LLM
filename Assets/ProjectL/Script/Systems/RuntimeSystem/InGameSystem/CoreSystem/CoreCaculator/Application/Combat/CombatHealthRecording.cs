using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>적용자가 이미 확정한 체력 단계의 참조를 명령의 실행 목록에 남깁니다.</summary>
    internal static class CombatHealthRecording
    {
        internal static CombatHealthStep Add(List<CombatHealthExecution> executions, EntityId recipient,
            HealthStateSnapshot before, HealthStateSnapshot after, CombatHealthSource source, CombatEvent evidence,
            int loss = 0, int recovery = 0, int normalization = 0)
        {
            var step = new CombatHealthStep(recipient, before, after, loss, recovery, normalization, source, evidence, 0);
            executions.Add(new CombatHealthExecution(new[] { step }, new[] { evidence }));
            return step;
        }

        // 범위 안 실행을 선택한 뒤, 체력의 시간순 정렬은 근거 사건과 순번을 사용하는 검증 경계에 맡깁니다.
        internal static IEnumerable<CombatHealthStep> StepsInScope(List<CombatHealthExecution> executions, int start, int end) =>
            executions.Skip(start).Take(end - start).SelectMany(execution => execution.Steps);

        internal static CombatHealthStep[] Reconstruction(CombatCharacterEvent evidence, HealthStateSnapshot depleted,
            CombatHealthSource lossSource, int actualLoss)
        {
            var recipient = evidence.Expected.EntityId;
            var restoration = new CombatHealthSource(CombatHealthOperationKind.EffectRecovery,
                TraitEffectOrigin.Create(recipient, TraitKey.Reconstruction));
            return new[]
            {
                new CombatHealthStep(recipient, evidence.Expected.Health, depleted, actualLoss, 0, 0, lossSource, evidence, 0),
                new CombatHealthStep(recipient, depleted, evidence.Final.Health, 0, evidence.Final.Health.CurrentHealth, 0, restoration, evidence, 1)
            };
        }
    }
}
