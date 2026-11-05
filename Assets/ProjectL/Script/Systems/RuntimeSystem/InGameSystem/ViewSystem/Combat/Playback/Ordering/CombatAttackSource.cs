using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering
{
    /// <summary>
    /// 공격 출처 하나의 사실입니다(특성 시스템 View 계획 TP-1). 맞는 순간 대응(<c>CombatHitFollowUps</c>)과 동시 묶음(<see cref="CombatConcurrentGroups"/>)이
    /// 이 값만 읽습니다. 공격 사건·반응 공격이 있는 포효·추가 공격 시작(특성 발동 사건)이 출처이고, 어느 사건에서 어떤 값이 나오는지는
    /// 사건 형식 표(<see cref="CombatEventTypeTable.TryGetAttackSource"/>)가 답합니다.
    /// </summary>
    /// <remarks>
    /// 후속은 피해 근원의 열쇠(공격 그룹 + 가해자, <see cref="Key"/>)가 출처와 같은 사건입니다. 공격 그룹 0은 근원 미지정(옛 기록)이라 출처로 열지 않습니다.
    /// </remarks>
    internal readonly struct CombatAttackSource
    {
        /// <param name="attackerId">공격자입니다(필수). 추가 공격이면 발동 유닛입니다.</param>
        /// <param name="attackGroup">공격 그룹입니다.</param>
        /// <param name="piercingShot">관통 기록입니다. 관통 공격이 아니면 null입니다.</param>
        internal CombatAttackSource(EntityId attackerId, long attackGroup, PiercingShotDelivery piercingShot)
        {
            if (attackerId.IsEmpty)
                throw new ArgumentException("[CombatAttackSource] 공격자가 있어야 합니다.", nameof(attackerId));
            AttackerId = attackerId;
            AttackGroup = attackGroup;
            PiercingShot = piercingShot;
        }

        /// <summary>공격자입니다. 이 출처의 "공격자 자신" 규칙(해제 대상 아님·Gate 규칙 1)이 씁니다.</summary>
        internal EntityId AttackerId { get; }

        /// <summary>공격 그룹입니다. 0이면 근원 미지정이라 후속을 맺지 않습니다.</summary>
        internal long AttackGroup { get; }

        /// <summary>관통 기록입니다. 관통 공격이면 만남 대상만 히트로 해제되고, 아니면 null입니다.</summary>
        internal PiercingShotDelivery PiercingShot { get; }

        /// <summary>이 출처의 근원 열쇠(공격 그룹 + 공격자)입니다.</summary>
        internal CombatEventSubjects.OriginKey Key => new(AttackGroup, AttackerId);

        /// <summary>공격 결과 하나의 출처입니다. 공격자는 부르는 쪽이 줍니다(포효는 보스).</summary>
        internal static CombatAttackSource Of(EntityId attackerId, in AttackResult attack) =>
            new(attackerId, attack.AttackGroup, attack.PiercingShot);
    }
}
