using System;
using System.Collections.Generic;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using UnityEngine;

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

        /// <summary>
        /// 이 사실에서 원래 모델 대신 보일 모델의 원본입니다(재구성 연출 설계 RE2·계획 TP-3). HUD와 같은 읽기 순서·보이기 규칙(<see cref="IsShown"/>)으로
        /// 상태 외형에 모습(<c>WhileAppearance</c>)이 있는 첫 상태를 고릅니다. 없으면 false입니다. 상태 읽기가 목록을 새로 만들므로 부르는 쪽은
        /// 사실이 바뀐 유닛만 부릅니다(<see cref="UnitStatusAppearance"/>).
        /// </summary>
        internal bool TryGetAppearance(CharacterCombatFacts character, out GameObject prefab)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            foreach (var snapshot in StatusEffectReadModel.Capture(character))
            {
                if (!IsShown(snapshot) || !_visuals.TryGet(snapshot.Key, out var visual) || visual.WhileAppearance == null)
                    continue;
                prefab = visual.WhileAppearance;
                return true;
            }
            prefab = null;
            return false;
        }
    }
}
