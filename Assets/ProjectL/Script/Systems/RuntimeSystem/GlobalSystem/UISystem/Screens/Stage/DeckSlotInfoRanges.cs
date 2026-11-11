using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage
{
    [Serializable]
    internal sealed class DeckSlotInfoRanges
    {
        [SerializeField] private UnitRangeImages movement;
        [SerializeField] private UnitRangeImages attack;

        internal void Validate(Transform owner)
        {
            DeckSlotInfoReferences.RequireChild(movement, owner, "ranges.movement");
            DeckSlotInfoReferences.RequireChild(attack, owner, "ranges.attack");
            if (movement == attack)
                throw DeckSlotInfoReferences.Error(owner, "ranges의 이동·공격 표시 영역은 서로 분리돼야 합니다.");
            movement.Validate("ranges.movement", showSplash: false);
            attack.Validate("ranges.attack", showSplash: true);
        }

        internal void Render(MoveMethod? moveMethod, int? moveDistance, AttackPatternSnapshot attackPattern)
        {
            // 카드 설명용 빈 격자의 직교 이동 범위입니다.
            movement.SetMovementRadius(moveMethod == MoveMethod.None ? 0 : moveDistance);
            attack.SetAttackPattern(attackPattern);
        }
    }
}
