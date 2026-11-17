using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.ActionButtons
{
    /// <summary>
    /// 행동 패널 스킬 버튼의 표시 값입니다(전투 스킬 View 설계 SK4, 결정 Q-SK5 A). 판단은 하지 않고 Core 가용성 답과 스킬 외형을 한 값으로 묶습니다.
    /// 기본값은 "스킬 없음(숨김)"입니다.
    /// </summary>
    internal readonly struct CombatSkillButton : IEquatable<CombatSkillButton>
    {
        internal CombatSkillButton(bool hasSkill, bool canUse, bool isActive, int remainingUses, int stageUseLimit,
            Sprite icon, string displayName, string description)
        {
            HasSkill = hasSkill;
            CanUse = canUse;
            IsActive = isActive;
            RemainingUses = remainingUses;
            StageUseLimit = stageUseLimit;
            Icon = icon;
            DisplayName = displayName;
            Description = description;
        }

        /// <summary>메인에 스킬이 묶여 있어 버튼을 보일지입니다.</summary>
        internal bool HasSkill { get; }

        /// <summary>누를 수 있는지입니다. 스킬 모드 중이면 끄기로 늘 누를 수 있습니다.</summary>
        internal bool CanUse { get; }

        /// <summary>스킬 모드 중인지입니다(켜짐 표시).</summary>
        internal bool IsActive { get; }

        /// <summary>남은 사용 횟수입니다(Core 가용성 답).</summary>
        internal int RemainingUses { get; }

        /// <summary>스테이지 사용 한도입니다(Core 가용성 답).</summary>
        internal int StageUseLimit { get; }

        /// <summary>아이콘입니다(스킬 외형).</summary>
        internal Sprite Icon { get; }

        /// <summary>호버 제목입니다(스킬·강화 외형 이름).</summary>
        internal string DisplayName { get; }

        /// <summary>호버 본문입니다(스킬·강화 외형 설명).</summary>
        internal string Description { get; }

        /// <summary>같은 값으로 누를 수 있음만 바꿉니다.</summary>
        internal CombatSkillButton WithCanUse(bool canUse) =>
            new(HasSkill, canUse, IsActive, RemainingUses, StageUseLimit, Icon, DisplayName, Description);

        /// <summary>보이되 누를 수 없고 켜지지 않은 값입니다(튜토리얼 설명 중).</summary>
        internal CombatSkillButton Locked() =>
            new(HasSkill, false, false, RemainingUses, StageUseLimit, Icon, DisplayName, Description);

        public bool Equals(CombatSkillButton other) =>
            HasSkill == other.HasSkill && CanUse == other.CanUse && IsActive == other.IsActive
            && RemainingUses == other.RemainingUses && StageUseLimit == other.StageUseLimit
            && ReferenceEquals(Icon, other.Icon)
            && string.Equals(DisplayName, other.DisplayName, StringComparison.Ordinal)
            && string.Equals(Description, other.Description, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is CombatSkillButton other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(HasSkill, CanUse, IsActive, RemainingUses, StageUseLimit, DisplayName);
    }
}
