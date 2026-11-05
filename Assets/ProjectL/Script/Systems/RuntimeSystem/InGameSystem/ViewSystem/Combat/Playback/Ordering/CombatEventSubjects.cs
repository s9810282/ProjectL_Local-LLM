using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering
{
    /// <summary>
    /// 피해 근원 열쇠와, 근원 여럿을 열쇠 하나로 합치는 규칙입니다(동시 처리 설계 C1). 칸·이름으로 추측하지 않습니다.
    /// 사건의 주체와 근원 목록은 사건 형식 표(<see cref="CombatEventTypeTable.TryGetSubject"/>·<see cref="CombatEventTypeTable.TryGetOrigins"/>)가 답합니다(View 2라운드 W7-2).
    /// </summary>
    internal static class CombatEventSubjects
    {
        /// <summary>
        /// 피해 근원 열쇠입니다. 같은 행동 구간에서 열쇠가 같은 사건은 Core가 같은 순간으로 기록한 것입니다
        /// (공격 그룹 = 피해 하나마다 Core가 붙이는 번호, 가해자 = <c>CreditedCharacterId</c>).
        /// </summary>
        internal readonly struct OriginKey : IEquatable<OriginKey>
        {
            internal OriginKey(long attackGroup, EntityId? credited)
            {
                AttackGroup = attackGroup;
                Credited = credited;
            }

            internal long AttackGroup { get; }
            internal EntityId? Credited { get; }

            public bool Equals(OriginKey other) => AttackGroup == other.AttackGroup && Nullable.Equals(Credited, other.Credited);
            public override bool Equals(object obj) => obj is OriginKey other && Equals(other);
            public override int GetHashCode() => unchecked((AttackGroup.GetHashCode() * 397) ^ Credited.GetHashCode());
        }

        /// <summary>
        /// 그 사건의 피해 근원 열쇠입니다. 근원이 여럿이면 모두 같은 열쇠일 때만 그 열쇠이고, 섞이면 false(묶음 밖, 보수적)입니다.
        /// 공격 그룹 0(근원 미지정)도 false입니다.
        /// </summary>
        internal static bool TryGetOriginKey(CombatEvent item, out OriginKey key)
        {
            key = default;
            return CombatEventTypeTable.TryGetOrigins(item, out IReadOnlyList<CombatDamageOrigin> origins) && TryGetKey(origins, out key);
        }

        private static bool TryGetKey(IReadOnlyList<CombatDamageOrigin> origins, out OriginKey key)
        {
            key = default;
            if (origins == null || origins.Count == 0) return false;
            bool found = false;
            for (int i = 0; i < origins.Count; i++)
            {
                if (origins[i] == null || !TryGetKey(origins[i], out OriginKey next)) return false;
                if (found && !key.Equals(next)) return false;
                key = next;
                found = true;
            }
            return found;
        }

        private static bool TryGetKey(CombatDamageOrigin origin, out OriginKey key)
        {
            key = new OriginKey(origin.AttackGroup, origin.CreditedCharacterId);
            return origin.AttackGroup != 0;
        }
    }
}
