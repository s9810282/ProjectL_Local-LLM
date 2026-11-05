using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 전투 목표 표시와 유닛 HUD 상태 칸 문구를 정합니다(보스 활성화 설계 D3·D4). 상태를 갖지 않는 순수 계산입니다.
    /// </summary>
    /// <remarks>
    /// 목표 대상은 Buffer의 <see cref="BattleObjective"/>로만 고르고 유닛 키·이름·분류로 고르지 않습니다.
    /// 포효 횟수·임계값은 Core 스냅샷 값을 그대로 옮기며 View가 세거나 정하지 않습니다.
    /// </remarks>
    internal static class CombatObjectiveDisplay
    {
        /// <summary>목표 대상 상태 칸의 키입니다. 특성 상태 칸 키(TraitKey 이름)와 겹치지 않습니다.</summary>
        internal const string ObjectiveStatusKey = "BattleObjective";

        /// <summary>목표 대상 상태 칸의 설명입니다.</summary>
        internal const string ObjectiveStatusDescription = "격파 목표";

        /// <summary>표시할 목표 대상입니다. 지정 보스 목표가 아니면(본부 파괴 등) 비어 있습니다.</summary>
        internal static EntityId TargetOf(BattleObjective objective)
        {
            return objective != null && objective.Kind == BattleObjectiveKind.DesignatedBossDefeated
                ? objective.TargetEntityId
                : EntityId.Empty;
        }

        /// <summary>
        /// 특성 상태 칸의 설명입니다. 포효는 표시 이름 뒤에 피격 횟수/발동 횟수를 붙이고, 다른 특성은 아직 설명을 쓰지 않아 빈 문자열입니다.
        /// </summary>
        internal static string DescribeTrait(TraitStatusSnapshot status, string displayName)
        {
            return status is RoarTraitStatusSnapshot roar
                ? $"{displayName} {roar.HitCount}/{roar.HitThreshold}"
                : string.Empty;
        }
    }
}
