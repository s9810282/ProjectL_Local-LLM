using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules
{
    internal sealed class PullTraitRule : ICombatTraitRule
    {
        public TraitKey Key => TraitKey.Pull;

        internal static void ValidateMethod(TargetingMethodSpec method)
        {
            if (method == null) throw new ArgumentNullException(nameof(method));
            // 조건은 프로필 생성 검사와 같은 정의를 쓴다. 여기서는 불변식으로만 다시 확인한다.
            if (!CombatAttackProfile.IsSupportedPullMethod(method))
                throw new InvalidOperationException("끌어오기는 직선·단일·방향 제한 없음·추가 범위 없음 지정만 지원합니다.");
        }

        internal static int SelectionRange(int attackRange, int extraRangeCells)
        {
            if (attackRange < 1) throw new ArgumentOutOfRangeException(nameof(attackRange));
            if (extraRangeCells < 0) throw new ArgumentOutOfRangeException(nameof(extraRangeCells));
            return checked(attackRange + extraRangeCells);
        }

        /// <summary>기본 사거리 안에서는 그대로 두고, 바깥에서는 기본 사거리 경계로 당깁니다.</summary>
        internal static CellCoord Destination(CellCoord actor, CellCoord contact, int attackRange)
        {
            if (attackRange < 1) throw new ArgumentOutOfRangeException(nameof(attackRange));
            long dx = (long)contact.X - actor.X, dy = (long)contact.Y - actor.Y;
            if ((dx == 0) == (dy == 0)) throw new ArgumentException("끌어오기 대상은 서로 다른 같은 행 또는 열에 있어야 합니다.", nameof(contact));
            if (Math.Abs(dx) + Math.Abs(dy) <= attackRange) return contact;
            return new CellCoord(checked(actor.X + Math.Sign(dx) * attackRange),
                checked(actor.Y + Math.Sign(dy) * attackRange));
        }

        internal static bool DealsAttackDamage(Owner attackerOwner, Owner targetOwner) =>
            new HostilityPolicy().AreHostile(attackerOwner, targetOwner);

        internal static AttackTargetingPolicy TargetingPolicy => new(false, false, false, false, allowsFriendlyCharacters: true);
    }
}
