using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageInput;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.TutorialInput;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.ActionButtons
{
    /// <summary>
    /// 전투 행동 버튼(공격·턴 종료·이동 되돌리기)의 표시 값입니다(행동 버튼 설계 B3·B4).
    /// 판단은 하지 않고 기존 판단(Policy·공격 진입)의 결과를 한 값으로 묶습니다.
    /// </summary>
    /// <remarks>
    /// 기본값은 "숨김"입니다. 패널은 직전 값과 같으면 다시 쓰지 않습니다.
    /// 되돌리기는 키 입력과 같은 기능의 가능 여부를 표시합니다.
    /// </remarks>
    internal readonly struct CombatActionButtons : IEquatable<CombatActionButtons>
    {
        internal CombatActionButtons(bool visible, bool canAttack, bool canEndTurn, bool isAttackActive, bool canUseReturn,
            CombatSkillButton skill = default)
        {
            Skill = skill;
            Visible = visible;
            CanAttack = canAttack;
            CanEndTurn = canEndTurn;
            IsAttackActive = isAttackActive;
            CanUseReturn = canUseReturn;
        }

        /// <summary>
        /// 버튼을 보일지입니다. 전투 입력 중이거나 실제 전투의 튜토리얼 설명 중이어야 하고, 입력이 연결돼 있으며 막혀 있지 않아야 합니다
        /// (<see cref="CombatInputPolicy.IsInputBlocked"/>).
        /// </summary>
        internal bool Visible { get; }

        /// <summary>공격 버튼을 누를 수 있는지입니다. 이미 켜진 모드의 끄기는 새 공격 허용과 분리합니다.</summary>
        internal bool CanAttack { get; }

        /// <summary>턴 종료 버튼을 누를 수 있는지입니다. Router가 제출 때 쓰는 판단과 같습니다.</summary>
        internal bool CanEndTurn { get; }

        /// <summary>현재 선택 유닛의 이동을 되돌릴 수 있는지입니다.</summary>
        internal bool CanUseReturn { get; }

        /// <summary>공격 모드 중인지입니다. 버튼 강조에 씁니다.</summary>
        internal bool IsAttackActive { get; }

        /// <summary>스킬 버튼입니다(전투 스킬 View 설계 SK4). 스킬이 없거나 버튼이 숨으면 기본값입니다.</summary>
        internal CombatSkillButton Skill { get; }

        /// <summary>
        /// 현재 사실로 버튼 값을 만듭니다. <paramref name="facts"/>는 선행 미처리 명령까지 읽은 사실이어야 합니다(턴 종료 판단).
        /// </summary>
        /// <param name="canEnterAttack">키와 같은 공격 진입 판단입니다. 운영 씬은 현재 Ready.CanAttack을 포함합니다.</param>
        /// <param name="skill">스킬 버튼 값입니다(배경 가용성 답·외형). 튜토리얼 제한은 여기서 더합니다.</param>
        internal static CombatActionButtons Read(in CombatInputFacts facts, bool canEnterAttack, bool isAttackActive, bool canUseReturn,
            CombatSkillButton skill)
        {
            if (CombatInputPolicy.IsInputBlocked(facts) || !facts.IsInputAvailable)
                return default;

            // 설명 대상인 실제 버튼은 남기고 조작만 막는다. 배치 설명·전투 종료에는 이 표시 예외를 적용하지 않는다.
            if (facts.CurrentLayer == TutorialInputLayers.Tutorial
                && CombatInputPolicy.GetDesiredLayer(facts) == StageInputLayers.Combat)
                return new CombatActionButtons(true, false, false, false, canUseReturn: false, skill: skill.Locked());

            if (facts.CurrentLayer != StageInputLayers.Combat)
                return default;

            return new CombatActionButtons(
                true,
                // 대기 중 새 진입은 막지만 이미 켜진 모드의 끄기는 유지합니다.
                isAttackActive || (CombatInputPolicy.AllowsTutorialPreview(facts, CombatViewPreviewKind.AttackRange)
                    && canEnterAttack),
                CombatInputPolicy.CanSubmit(facts, facts.HasPendingCommand, CombatViewCommandKind.EndPlayerTurn),
                isAttackActive,
                canUseReturn: canUseReturn,
                // 이미 켜진 스킬 모드의 끄기는 늘 됩니다. 새 진입은 튜토리얼 허용표도 봅니다(전투 스킬 View 설계 P9).
                skill: skill.WithCanUse(skill.IsActive
                    || (skill.CanUse && CombatInputPolicy.AllowsTutorialPreview(facts, CombatViewPreviewKind.SkillAvailability))));
        }

        public bool Equals(CombatActionButtons other)
        {
            return Visible == other.Visible
                && CanAttack == other.CanAttack
                && CanEndTurn == other.CanEndTurn
                && CanUseReturn == other.CanUseReturn
                && IsAttackActive == other.IsAttackActive
                && Skill.Equals(other.Skill);
        }

        public override bool Equals(object obj) => obj is CombatActionButtons other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Visible, CanAttack, CanEndTurn, IsAttackActive, CanUseReturn, Skill);
    }
}
