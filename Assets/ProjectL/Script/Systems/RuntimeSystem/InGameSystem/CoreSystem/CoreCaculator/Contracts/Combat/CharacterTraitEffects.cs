using System;
using System.Linq;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.CombatEffects;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>특성 효과의 레거시 입출력 값입니다. 전투 사실의 공격 기여분은 정규 상태 원본에서만 투영합니다.</summary>
    public sealed class CharacterTraitEffects
    {
        public static CharacterTraitEffects Empty { get; } = new(Array.Empty<TraitAttackGrant>());
        public IReadOnlyList<TraitAttackGrant> AttackGrants { get; }
        public IReadOnlyList<TraitShieldGrant> Shields { get; }
        // 레거시 조회 사본은 같은 정규 값만 참조하며, 무관한 사실 복사에서도 재사용합니다.
        internal StatusEffectSet SourceStatuses { get; }
        /// <summary>현재 생존 이웃에서 Core가 투영한 결속 방어 기여분입니다. 기본 방어와 방어막 예산은 바꾸지 않습니다.</summary>
        public int CohesionDefenseBonus { get; }
        public int ActiveShieldTotal => Shields.Where(g => g.Eligible).Aggregate(0, (sum, grant) => checked(sum + grant.RemainingBudget));
        public int AttackBonus
        {
            get
            {
                int total = 0;
                foreach (var grant in AttackGrants) total = checked(total + grant.Amount);
                return total;
            }
        }

        public CharacterTraitEffects(IReadOnlyList<TraitAttackGrant> attackGrants)
            : this(attackGrants, Array.Empty<TraitShieldGrant>())
        {
        }

        public CharacterTraitEffects(IReadOnlyList<TraitAttackGrant> attackGrants, IReadOnlyList<TraitShieldGrant> shields)
            : this(attackGrants, shields, 0)
        {
        }

        public CharacterTraitEffects(IReadOnlyList<TraitAttackGrant> attackGrants, IReadOnlyList<TraitShieldGrant> shields, int cohesionDefenseBonus)
        {
            if (cohesionDefenseBonus < 0) throw new ArgumentOutOfRangeException(nameof(cohesionDefenseBonus));
            CohesionDefenseBonus = cohesionDefenseBonus;
            if (shields == null || shields.Any(s => s == null) || shields.Select(s => s.Origin).Distinct().Count() != shields.Count)
                throw new ArgumentException("방어막 출처는 null이거나 중복될 수 없습니다.", nameof(shields));
            Shields = Array.AsReadOnly(shields.ToArray());
            _ = ActiveShieldTotal;
            if (attackGrants == null) throw new ArgumentNullException(nameof(attackGrants));
            var copy = new TraitAttackGrant[attackGrants.Count];
            for (int i = 0; i < copy.Length; i++)
                copy[i] = attackGrants[i] ?? throw new ArgumentException("공격 기여분은 null일 수 없습니다.", nameof(attackGrants));
            AttackGrants = Array.AsReadOnly(copy);
            _ = AttackBonus;
        }

        internal CharacterTraitEffects(StatusEffectSet statuses, int cohesionDefenseBonus)
            : this((statuses ?? throw new ArgumentNullException(nameof(statuses))).Entries.OfType<AttackIncreaseStatus>()
                .Select(s => new TraitAttackGrant(s)).ToArray(), statuses.Entries.OfType<ShieldStatus>().Select(s => new TraitShieldGrant(s)).ToArray(), cohesionDefenseBonus)
        {
            SourceStatuses = statuses;
        }

        public bool SameValues(CharacterTraitEffects other)
        {
            if (other == null || CohesionDefenseBonus != other.CohesionDefenseBonus || AttackGrants.Count != other.AttackGrants.Count || Shields.Count != other.Shields.Count) return false;
            for (int i = 0; i < Shields.Count; i++) if (!Shields[i].SameValues(other.Shields[i])) return false;
            for (int i = 0; i < AttackGrants.Count; i++)
                if (!AttackGrants[i].SameValues(other.AttackGrants[i])) return false;
            return true;
        }
    }
}
