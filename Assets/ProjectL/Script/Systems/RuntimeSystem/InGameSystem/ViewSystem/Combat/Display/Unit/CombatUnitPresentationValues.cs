using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 한 외형의 연출 값 셋(공격 표시·피격 반응·클립 묶음)입니다(범용 연출 구조 설계 G8, 계획 P2).
    /// 외형 표의 읽기는 SO에서 매번 새 값을 만들고 클립 이벤트를 다시 꺼내 검사하므로, 표시 집합이 외형마다 한 번 읽어 이 값으로 보관합니다.
    /// 셋 다 없을 수 있습니다(null).
    /// </summary>
    internal sealed class CombatUnitPresentationValues
    {
        internal CombatUnitPresentationValues(CombatAttackPresentationData attack, CombatUnitReactionPresentationData reaction,
            CombatUnitAnimationData animation)
        {
            Attack = attack;
            Reaction = reaction;
            Animation = animation;
        }

        /// <summary>공격 표시 값입니다. 없으면 null입니다.</summary>
        internal CombatAttackPresentationData Attack { get; }

        /// <summary>피격·사망 반응 값입니다. 없으면 null입니다.</summary>
        internal CombatUnitReactionPresentationData Reaction { get; }

        /// <summary>공격·피격 클립 묶음입니다. 없으면 null입니다.</summary>
        internal CombatUnitAnimationData Animation { get; }
    }
}
