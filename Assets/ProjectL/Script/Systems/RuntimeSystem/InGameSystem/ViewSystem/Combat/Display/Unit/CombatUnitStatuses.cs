using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>현재 표시 사실의 Core 읽기 결과를 효과 키당 하나의 HUD 항목으로 바꿉니다.</summary>
    internal sealed class CombatUnitStatuses
    {
        private readonly IUnitStatusVisualTableQuery _visuals;
        private readonly CombatEffectFormatters _formatters;
        private readonly CombatStatusDescriptionFormatter _descriptions;
        internal CombatUnitStatuses(IUnitStatusVisualTableQuery visuals, CombatEffectFormatters formatters, CombatStatusDescriptionFormatter descriptions)
        {
            _visuals = visuals ?? throw new ArgumentNullException(nameof(visuals));
            _formatters = formatters ?? throw new ArgumentNullException(nameof(formatters));
            _descriptions = descriptions ?? throw new ArgumentNullException(nameof(descriptions));
        }
        /// <summary>
        /// 걸린 상태 하나가 보이는지의 규칙 원본입니다(이펙트 끝 규칙 계획 TP-E12). HUD 상태 아이콘과 상태 유지 이펙트(<c>CombatStatusEffectSource</c>)가 함께 부릅니다.
        /// 공통 상태 읽기(<see cref="StatusEffectReadModel.Capture"/>)가 상태 키당 하나로 묶어 주고, 소진·비활성 보호막(남은 예산 0)만 숨깁니다(Core 이력으로는 남음).
        /// </summary>
        internal static bool IsShown(StatusEffectSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));
            return snapshot is not ShieldEffectSnapshot shield || shield.RemainingEligibleBudget != 0;
        }

        internal void AddTo(CharacterCombatFacts character, List<UnitHUDStatus> statuses)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            if (statuses == null) throw new ArgumentNullException(nameof(statuses));
            foreach (var snapshot in StatusEffectReadModel.Capture(character))
            {
                if (!IsShown(snapshot))
                    continue;
                var formatter = _formatters.Get(snapshot.Key);
                var visual = _visuals.Get(snapshot.Key);
                statuses.Add(new UnitHUDStatus("StatusEffect:" + (int)snapshot.Key, visual.Icon,
                    _descriptions.Describe(visual.DescribeBody(formatter.Values(snapshot)), snapshot), title: visual.DisplayName));
            }
        }
    }
}
