using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts
{
    /// <summary>
    /// 플레이어 액티브 스킬 모드의 표시 의도입니다(전투 스킬 View 설계 SK2). 스킬 모드 입력(<c>CombatPlayerSkillInteraction</c>)이 부르고,
    /// 공격 조준과 같은 그림 부품(영향 칸·궤적·넉백·예상 체력)의 주인이 구현합니다(계획 TP-3 — <c>Display/Feedback/CombatAttackFeedback</c>).
    /// </summary>
    /// <remarks>
    /// 선택 쪽(후보 칸·고른 점·시전자 → 점 선)과 예보 쪽(영향 칸·실제 경로 선·예상 체력·넉백)은 따로 지웁니다. 예보가 경로 선을 그리는 동안은
    /// 선택 선 대신 경로 선이 보이고, 예보를 지우면 선택 선이 다시 보입니다. 무엇을 고를 수 있는지·무엇이 맞는지는 모두 Core 답이고 여기서 계산하지 않습니다.
    /// </remarks>
    internal interface ICombatPlayerSkillFeedback
    {
        /// <summary>
        /// 선택 중 그림입니다. 다음 후보 칸, 고른 점, 시전자에서 고른 점을 차례로 잇는 선(점이 있을 때만)을 그립니다.
        /// </summary>
        /// <param name="settings">스킬 외형의 공격 연출입니다. 선의 발사 높이·출발점에 씁니다(직선 투사체가 아니면 선을 그리지 않음).</param>
        void ShowSkillSelection(EntityId caster, IReadOnlyList<CellCoord> candidates, IReadOnlyList<CellCoord> points,
            CombatAttackPresentationData settings);

        /// <summary>선택 중 그림(후보·점·선)을 지웁니다. 여러 번 불려도 안전합니다.</summary>
        void ClearSkillSelection();

        /// <summary>
        /// 예보 그림입니다. 영향 칸, 실제 경로 선(경로가 있을 때), 대상 예상 체력, 넉백 화살표를 그립니다.
        /// </summary>
        void ShowSkillForecast(EntityId caster, CombatPlayerSkillForecastView forecast, CombatAttackPresentationData settings);

        /// <summary>예보 그림을 지웁니다. 선택 선이 있으면 다시 보입니다. 여러 번 불려도 안전합니다.</summary>
        void ClearSkillForecast();
    }
}
