using System;
using System.Collections.Generic;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.PlayerSkill.Item
{
    /// <summary>
    /// 플레이어 액티브 스킬 하나의 외형입니다(전투 스킬 View 설계 SK5). 줄 하나 = Core 스킬 키 하나이고, 강화마다 이름·설명·공격 연출을 덮어쓸 수 있습니다.
    /// DB를 만들 때 SO가 한 번 만들고 표가 들고 있는 읽기 전용 값입니다.
    /// </summary>
    /// <remarks>
    /// 어느 강화가 어느 스킬 것인지·몇 점을 고르는지·어떻게 맞는지는 Core 지식이라 이 값은 갖지 않습니다(설계 P10). View는 스킬 종류를 모르고,
    /// 이 값은 아이콘·이름·설명·공격 연출을 찾을 때만 씁니다(설계 §2). 공격 연출은 지금 기존 공격 묶음을 빌려 씁니다(결정 Q-AS4).
    /// </remarks>
    internal sealed class PlayerSkillVisualData : IData
    {
        internal PlayerSkillVisualData(PlayerSkillKey key, string displayName, Sprite icon, string description,
            CombatAttackPresentationData attack, CombatEventCues areaCellCues, IReadOnlyList<PlayerSkillUpgradeVisual> upgrades)
        {
            if (key == PlayerSkillKey.None || !System.Enum.IsDefined(typeof(PlayerSkillKey), key))
                throw new InvalidOperationException($"[PlayerSkillVisualData] 스킬 키가 맞지 않습니다: {key}");
            Key = key;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? key.ToString() : displayName;
            Icon = icon != null ? icon : throw new InvalidOperationException($"[PlayerSkillVisualData] 아이콘이 필요합니다. Key: {key}");
            Description = description ?? string.Empty;
            Attack = attack ?? throw new InvalidOperationException($"[PlayerSkillVisualData] 공격 연출이 필요합니다. Key: {key}");
            AreaCellCues = areaCellCues ?? throw new ArgumentNullException(nameof(areaCellCues));
            string cueError = CombatPresentationCueRules.FindError(areaCellCues, CombatCueEvent.PlayerSkillCell);
            if (cueError != null)
                throw new InvalidOperationException($"[PlayerSkillVisualData] Key: {key}, 영역 칸 큐: {cueError}");
            Upgrades = upgrades ?? throw new ArgumentNullException(nameof(upgrades));
            for (int i = 0; i < upgrades.Count; i++)
            {
                PlayerSkillUpgradeVisual upgrade = upgrades[i] ?? throw new InvalidOperationException($"[PlayerSkillVisualData] 강화 줄 {i}가 비었습니다. Key: {key}");
                for (int j = 0; j < i; j++)
                {
                    if (upgrades[j].Key == upgrade.Key)
                        throw new InvalidOperationException($"[PlayerSkillVisualData] 같은 강화 줄이 둘입니다. Key: {key}, 강화: {upgrade.Key}");
                }
            }
        }

        public PlayerSkillKey Key { get; }
        /// <summary>강화가 없을 때의 이름입니다. 비우면 스킬 키 이름입니다.</summary>
        public string DisplayName { get; }
        /// <summary>행동 패널 스킬 버튼 아이콘입니다(필수, 강화가 바꾸지 않음).</summary>
        public Sprite Icon { get; }
        /// <summary>강화가 없을 때의 설명입니다(버튼 호버, 결정 Q-SK5).</summary>
        public string Description { get; }
        /// <summary>강화가 없을 때의 공격 연출입니다. 사건 모양(투사체 경로가 있나)에 맞아야 합니다(설계 P5 — 재생 때 검사).</summary>
        internal CombatAttackPresentationData Attack { get; }
        /// <summary>투사체 경로가 없는 사건의 칸마다 내는 큐입니다(결정 Q-SK4, 비어도 됨). 강화가 바꾸지 않습니다.</summary>
        internal CombatEventCues AreaCellCues { get; }
        /// <summary>강화 덮어쓰기 줄입니다. 강화 키마다 하나이고 없을 수 있습니다.</summary>
        internal IReadOnlyList<PlayerSkillUpgradeVisual> Upgrades { get; }

        /// <summary>
        /// 그 강화의 외형입니다. 강화 줄이 있으면 그 줄의 값을, 비운 칸과 강화 없음(<see cref="PlayerSkillUpgradeKey.None"/>)은 이 줄의 값을 씁니다.
        /// 강화 줄이 없는 강화도 이 줄의 값을 씁니다(강화 소속은 Core 지식, 설계 P10).
        /// </summary>
        internal PlayerSkillVisualLook LookOf(PlayerSkillUpgradeKey upgrade)
        {
            for (int i = 0; i < Upgrades.Count; i++)
            {
                PlayerSkillUpgradeVisual row = Upgrades[i];
                if (row.Key != upgrade)
                    continue;
                return new PlayerSkillVisualLook(row.DisplayName ?? DisplayName, row.Description ?? Description, Icon, row.Attack ?? Attack,
                    AreaCellCues);
            }
            return new PlayerSkillVisualLook(DisplayName, Description, Icon, Attack, AreaCellCues);
        }
    }

    /// <summary>강화 하나의 덮어쓰기 값입니다. 비운 칸(null)은 스킬 줄의 값을 씁니다.</summary>
    internal sealed class PlayerSkillUpgradeVisual
    {
        internal PlayerSkillUpgradeVisual(PlayerSkillUpgradeKey key, string displayName, string description, CombatAttackPresentationData attack)
        {
            if (key == PlayerSkillUpgradeKey.None || !System.Enum.IsDefined(typeof(PlayerSkillUpgradeKey), key))
                throw new InvalidOperationException($"[PlayerSkillUpgradeVisual] 강화 키가 맞지 않습니다: {key}");
            Key = key;
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? null : displayName;
            Description = string.IsNullOrWhiteSpace(description) ? null : description;
            Attack = attack;
        }

        internal PlayerSkillUpgradeKey Key { get; }
        /// <summary>비우면 null(스킬 줄 이름)입니다.</summary>
        internal string DisplayName { get; }
        /// <summary>비우면 null(스킬 줄 설명)입니다.</summary>
        internal string Description { get; }
        /// <summary>비우면 null(스킬 줄 공격 연출)입니다.</summary>
        internal CombatAttackPresentationData Attack { get; }
    }

    /// <summary>스킬·강화 하나로 고른 외형 값입니다(<see cref="PlayerSkillVisualData.LookOf"/>).</summary>
    internal readonly struct PlayerSkillVisualLook
    {
        internal PlayerSkillVisualLook(string displayName, string description, Sprite icon, CombatAttackPresentationData attack,
            CombatEventCues areaCellCues)
        {
            DisplayName = displayName;
            Description = description;
            Icon = icon;
            Attack = attack;
            AreaCellCues = areaCellCues;
        }

        internal string DisplayName { get; }
        internal string Description { get; }
        internal Sprite Icon { get; }
        internal CombatAttackPresentationData Attack { get; }
        internal CombatEventCues AreaCellCues { get; }
    }
}
