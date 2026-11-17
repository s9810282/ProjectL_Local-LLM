using System;
using System.Collections.Generic;
using ProjectL.Framework.DataBase;
using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.Table.PlayerSkill.Item
{
    /// <summary>플레이어 액티브 스킬 하나의 외형을 입력하고 조회마다 새 읽기 전용 데이터를 만듭니다(DB 규칙 P2, 전투 스킬 View 설계 SK5).</summary>
    [CreateAssetMenu(fileName = "NewPlayerSkillVisualData", menuName = "ProjectL/Database/View/PlayerSkillVisualData")]
    internal sealed class PlayerSkillVisualDataSO : DatabaseItem
    {
        [SerializeField] private Input playerSkillVisualData = new();

        public override IData GetData() => playerSkillVisualData.ToData();

        [Serializable]
        private sealed class Input
        {
            [Tooltip("Core 스킬 키입니다. 스킬 하나에 줄 하나입니다. 어느 메인이 어느 스킬을 쓰는지는 Core 표(InGame)가 정합니다.")]
            [SerializeField] private PlayerSkillKey skillKey = PlayerSkillKey.None;
            [Tooltip("강화가 없을 때 스킬 버튼 호버에 보일 이름입니다. 비우면 스킬 키 이름입니다.")]
            [SerializeField] private string displayName;
            [Tooltip("행동 패널 스킬 버튼 아이콘입니다(필수). 강화가 있어도 이 아이콘을 씁니다.")]
            [SerializeField] private Sprite icon;
            [Tooltip("강화가 없을 때 스킬 버튼 호버에 보일 설명입니다.")]
            [SerializeField, TextArea(2, 6)] private string description;
            [Tooltip("강화가 없을 때의 공격 연출입니다(필수). 투사체를 날리는 스킬(경로가 있는 사건)은 직선 투사체 묶음, 범위 폭발 스킬은 투사체 없는 묶음이어야 합니다. "
                + "맞지 않으면 그 스킬을 재생할 때 멈춥니다. 지금은 기존 공격 묶음을 빌려 씁니다.")]
            [SerializeField] private CombatAttackPresentationSO attackPresentation;
            [Tooltip("범위 폭발 스킬(투사체 경로가 없는 사건)에서 칸마다 맞는 순간(Hit)에 낼 이펙트입니다. 자리는 그 칸(EventCell)만 됩니다. 비워도 됩니다.")]
            [SerializeField] private CombatVisualCueInput areaCellCues = new();
            [Tooltip("강화마다 이름·설명·공격 연출을 덮어씁니다. 비운 칸은 위 값을 씁니다. 줄이 없는 강화도 위 값을 씁니다.")]
            [SerializeField] private List<UpgradeInput> upgrades = new();

            internal PlayerSkillVisualData ToData()
            {
                var rows = new List<PlayerSkillUpgradeVisual>(upgrades?.Count ?? 0);
                if (upgrades != null)
                {
                    for (int i = 0; i < upgrades.Count; i++)
                    {
                        UpgradeInput row = upgrades[i] ?? throw new InvalidOperationException(
                            $"[PlayerSkillVisualData] 강화 줄 {i}가 비었습니다. Key: {skillKey}");
                        // 강화 줄(공격 연출 값 포함) 오류에 어느 스킬 줄인지 붙입니다(SK-a 검토 L6).
                        try
                        {
                            rows.Add(row.ToData());
                        }
                        catch (Exception error) when (error is ArgumentException || error is InvalidOperationException)
                        {
                            throw new InvalidOperationException($"[PlayerSkillVisualData] 강화 줄 {i} 값이 맞지 않습니다. Key: {skillKey}. {error.Message}", error);
                        }
                    }
                }
                CombatEventCues cues;
                try
                {
                    cues = (areaCellCues ?? new CombatVisualCueInput()).ToCues();
                }
                catch (ArgumentException error)
                {
                    throw new InvalidOperationException($"[PlayerSkillVisualData] 영역 칸 큐 값이 맞지 않습니다. Key: {skillKey}. {error.Message}", error);
                }
                return new PlayerSkillVisualData(skillKey, displayName, icon, description,
                    attackPresentation != null ? attackPresentation.CreateData() : null, cues, rows.AsReadOnly());
            }
        }

        [Serializable]
        private sealed class UpgradeInput
        {
            [Tooltip("Core 강화 키입니다. 그 스킬의 강화만 실제로 쓰입니다(어느 강화가 어느 스킬 것인지는 Core가 정함).")]
            [SerializeField] private PlayerSkillUpgradeKey upgradeKey = PlayerSkillUpgradeKey.None;
            [Tooltip("이 강화일 때의 이름입니다. 비우면 스킬 줄 이름입니다.")]
            [SerializeField] private string displayName;
            [Tooltip("이 강화일 때의 설명입니다. 비우면 스킬 줄 설명입니다.")]
            [SerializeField, TextArea(2, 6)] private string description;
            [Tooltip("이 강화일 때의 공격 연출입니다. 비우면 스킬 줄 공격 연출입니다.")]
            [SerializeField] private CombatAttackPresentationSO attackPresentation;

            internal PlayerSkillUpgradeVisual ToData() => new(upgradeKey, displayName, description,
                attackPresentation != null ? attackPresentation.CreateData() : null);
        }
    }
}
