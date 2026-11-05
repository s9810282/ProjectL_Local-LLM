using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    /// <summary>
    /// View가 Unit의 이동·공격·정체성 및 실효 전투 병과를 읽는 복사본이다.
    /// </summary>
    public readonly struct UnitStatsSnapshot
    {
        // ⚠ 필드를 추가·삭제할 때 반드시 함께 고친다: SameValues, With, ContractShapeGuardTests 목록.
        //   건너뛰면 ContractShapeGuardTests가 실패한다.
        public MoveMethod MoveMethod { get; }
        public int MoveRange { get; }
        public int AppointRange { get; }
        public int AttackRange => AppointRange;
        public int AttackPower { get; }
        /// <summary>Buffer 경로에서는 보유 카드 영혼 개수로 계산한다. 기존 Stage 경로는 이전 완료 전까지 별도다.</summary>
        public int SoulAttackBonus { get; }
        /// <summary>Buffer 경로에서는 보유 카드 영혼 개수로 계산한 최대 체력 보너스다.</summary>
        public int SoulMaxHealthBonus { get; }
        /// <summary>현재 Stage의 Artifact 사실 집합만 반영한 공격력 보너스다.</summary>
        public int ArtifactAttackBonus { get; }
        /// <summary>현재 위치에서만 적용하는 부호 있는 공격력 기여분이다.</summary>
        public int PositionAttackBonus { get; }
        /// <summary>받은 특성·소유 특성·보유 기록에서 계산한 공격 기여분입니다.</summary>
        public int TraitAttackBonus { get; }
        /// <summary>기본 공격력과 Artifact·위치·특성 공격 기여분의 합이며 피해 전용 배율은 포함하지 않는다.</summary>
        public int EffectiveAttackPower { get; }
        public int PhysicalDefense { get; }
        public int Priority { get; }
        public ReadOnlyMemory<TraitStatusSnapshot> TraitStatuses { get; }
        public CategorySnapshot Category { get; }
        /// <summary>정체성은 Main으로 유지하며 Category는 선택한 전투 병과를 나타낸다.</summary>
        public CategoryKey? SpecializationCategory { get; }
        public CategoryKey IdentityCategoryKey => SpecializationCategory.HasValue ? CategoryKey.Main : Category.Key;
        public CategoryKey CombatCategoryKey => Category.Key;

        public UnitStatsSnapshot(
            MoveMethod moveMethod,            int moveRange,            int appointRange,            int attackPower,            int physicalDefense,

            int priority,
            TraitStatusSnapshot[] traitStatuses,            CategorySnapshot category,            int artifactAttackBonus = 0,            int? effectiveAttackPower = null,            int soulAttackBonus = 0,            int soulMaxHealthBonus = 0,            int positionAttackBonus = 0,int traitAttackBonus = 0, CategoryKey? specializationCategory = null)
        {
            if (Enum.IsDefined(typeof(MoveMethod), moveMethod) == false)
                throw new ArgumentOutOfRangeException(nameof(moveMethod));
            if (moveRange < 0)
                throw new ArgumentOutOfRangeException(nameof(moveRange));
            if (appointRange < 0)
                throw new ArgumentOutOfRangeException(nameof(appointRange));
            if (attackPower < 0)
                throw new ArgumentOutOfRangeException(nameof(attackPower));
            if (artifactAttackBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(artifactAttackBonus));
            if (soulAttackBonus < 0 || soulAttackBonus > artifactAttackBonus)
                throw new ArgumentOutOfRangeException(nameof(soulAttackBonus));
            if (soulMaxHealthBonus < 0)
                throw new ArgumentOutOfRangeException(nameof(soulMaxHealthBonus));
            if (traitAttackBonus < 0) throw new ArgumentOutOfRangeException(nameof(traitAttackBonus));
            int resolvedEffectiveAttackPower = effectiveAttackPower ??
                checked(attackPower + artifactAttackBonus + positionAttackBonus + traitAttackBonus);
            if (resolvedEffectiveAttackPower < 0 ||
                resolvedEffectiveAttackPower != checked(attackPower + artifactAttackBonus + positionAttackBonus + traitAttackBonus))
                throw new ArgumentException(
                    "Effective attack power must be nonnegative and equal base plus Artifact, position and trait bonuses.",
                    nameof(effectiveAttackPower));
            if (physicalDefense < 0)
                throw new ArgumentOutOfRangeException(nameof(physicalDefense));
            if (priority < 0)
                throw new ArgumentOutOfRangeException(nameof(priority));
            if (traitStatuses == null)
                throw new ArgumentNullException(nameof(traitStatuses));
            if (category == null)
                throw new ArgumentNullException(nameof(category));
            if (specializationCategory.HasValue && (category.Key != specializationCategory ||
                specializationCategory != CategoryKey.Rider && specializationCategory != CategoryKey.Heavy && specializationCategory != CategoryKey.Cannon))
                throw new ArgumentException("전직 병과는 Rider/Heavy/Cannon 중 실제 전투 병과와 같아야 합니다.", nameof(specializationCategory));

            TraitStatusSnapshot[] copiedTraitStatuses =
                new TraitStatusSnapshot[traitStatuses.Length];
            for (int i = 0; i < traitStatuses.Length; i++)
            {
                if (traitStatuses[i] == null)
                    throw new ArgumentException(
                        "Trait Status 목록에는 null 항목을 넣을 수 없습니다.",
                        nameof(traitStatuses));

                copiedTraitStatuses[i] = traitStatuses[i];
            }

            MoveMethod = moveMethod;
            MoveRange = moveRange;

            AppointRange = appointRange;
            AttackPower = attackPower;
            SoulAttackBonus = soulAttackBonus;
            SoulMaxHealthBonus = soulMaxHealthBonus;
            ArtifactAttackBonus = artifactAttackBonus;
            PositionAttackBonus = positionAttackBonus;
            TraitAttackBonus = traitAttackBonus;
            EffectiveAttackPower = resolvedEffectiveAttackPower;
            PhysicalDefense = physicalDefense;


            Priority = priority;

            TraitStatuses = copiedTraitStatuses;
            Category = category;
            SpecializationCategory = specializationCategory;
        }

        /// <summary>모든 능력치 값이 같은지다. 특성 목록은 순서까지, 특성별 값은 각 특성 스냅샷이 비교한다.</summary>
        public bool SameValues(UnitStatsSnapshot other)
        {
            if (MoveMethod != other.MoveMethod ||
                MoveRange != other.MoveRange ||
                AppointRange != other.AppointRange ||
                AttackPower != other.AttackPower ||
                SoulAttackBonus != other.SoulAttackBonus ||
                SoulMaxHealthBonus != other.SoulMaxHealthBonus ||
                ArtifactAttackBonus != other.ArtifactAttackBonus ||
                EffectiveAttackPower != other.EffectiveAttackPower ||
                PositionAttackBonus != other.PositionAttackBonus ||
                TraitAttackBonus != other.TraitAttackBonus ||
                PhysicalDefense != other.PhysicalDefense ||
                Priority != other.Priority ||
                SpecializationCategory != other.SpecializationCategory ||
                CategorySnapshot.Same(Category, other.Category) == false ||
                TraitStatuses.Length != other.TraitStatuses.Length)
                return false;

            for (int i = 0; i < TraitStatuses.Length; i++)
                if (SameTraitStatus(TraitStatuses.Span[i], other.TraitStatuses.Span[i]) == false)
                    return false;

            return true;
        }

        /// <summary>
        /// 지정한 값만 바꾼 새 능력치다. 지정하지 않은(null) 값은 그대로 둔다.
        /// 단 <paramref name="effectiveAttackPower"/>만은 null이면 "그대로"가 아니라 "공격·아티팩트·위치·특성 기여분으로 다시 계산"이다
        /// (옛 값을 두면 공격 계열을 바꿀 때 생성자 검증에 걸린다). 지금 값을 명시해 넘기던 호출처는 계속 명시해 넘긴다.
        /// </summary>
        public UnitStatsSnapshot With(MoveMethod? moveMethod = null,int? moveRange = null,            int? appointRange = null,int? attackPower = null,int? physicalDefense = null,int? priority = null,            TraitStatusSnapshot[] traitStatuses = null,CategorySnapshot category = null,            int? artifactAttackBonus = null,int? effectiveAttackPower = null,int? soulAttackBonus = null,            int? soulMaxHealthBonus = null,int? positionAttackBonus = null,int? traitAttackBonus = null) =>
            new UnitStatsSnapshot(moveMethod ?? MoveMethod,moveRange ?? MoveRange,                appointRange ?? AppointRange,attackPower ?? AttackPower,physicalDefense ?? PhysicalDefense,priority ?? Priority,traitStatuses ?? TraitStatuses.ToArray(),category ?? Category,artifactAttackBonus ?? ArtifactAttackBonus,                effectiveAttackPower,soulAttackBonus ?? SoulAttackBonus,soulMaxHealthBonus ?? SoulMaxHealthBonus,                positionAttackBonus ?? PositionAttackBonus,traitAttackBonus ?? TraitAttackBonus,SpecializationCategory);

        private static bool SameTraitStatus(TraitStatusSnapshot left, TraitStatusSnapshot right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left == null || right == null || left.Key != right.Key || left.UnavailableReason != right.UnavailableReason ||
                left.PartialUnavailableReason != right.PartialUnavailableReason)
                return false;

            // 특성별 값 비교는 각 스냅샷이 소유한다. 새 특성을 추가해도 이 비교는 바뀌지 않는다.
            return left.SameValues(right);
        }
    }
}
