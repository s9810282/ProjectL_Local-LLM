using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    /// <summary>
    /// View가 Unit의 Category 공개 상태를 읽는 복사본입니다.
    /// Base와 Cannon은 추가 상태가 없으므로 가짜 값을 만들지 않고 null로 남깁니다.
    /// </summary>
    public sealed class CategorySnapshot
    {
        public CategoryKey Key { get; }

        /// <summary>Rider만 값을 가집니다. 이번 턴에 실제 양수 환급을 이미 사용했는지 여부입니다.</summary>
        public bool? RefundUsedThisTurn { get; }

        /// <summary>Heavy만 값을 가집니다. footprint Rotation과 독립적인 방어 Facing입니다.</summary>
        public Rotation4? DefenseFacing { get; }

        /// <summary>Heavy만 값을 가집니다. 이번 턴에 자발적으로 이동했는지 여부입니다.</summary>
        public bool? MovedThisTurn { get; }

        /// <summary>Heavy만 값을 가집니다. 보호 영역에서의 피해 경감률입니다.</summary>
        public int? DamageReductionPercent { get; }

        private CategorySnapshot(
            CategoryKey key,
            bool? refundUsedThisTurn,
            Rotation4? defenseFacing,
            bool? movedThisTurn,
            int? damageReductionPercent)
        {
            Key = key;
            RefundUsedThisTurn = refundUsedThisTurn;
            DefenseFacing = defenseFacing;
            MovedThisTurn = movedThisTurn;
            DamageReductionPercent = damageReductionPercent;
        }

        /// <summary>추가 공개 상태가 없는 Category(Base, Cannon)의 Snapshot을 만듭니다.</summary>
        public static CategorySnapshot CreateStateless(CategoryKey key)
        {
            ValidateKey(key);
            return new CategorySnapshot(key, null, null, null, null);
        }

        public static CategorySnapshot CreateRider(bool refundUsedThisTurn)
        {
            return new CategorySnapshot(CategoryKey.Rider, refundUsedThisTurn, null, null, null);
        }

        public static CategorySnapshot CreateHeavy(
            Rotation4 defenseFacing,
            bool movedThisTurn,
            int damageReductionPercent)
        {
            if (Enum.IsDefined(typeof(Rotation4), defenseFacing) == false)
                throw new ArgumentOutOfRangeException(nameof(defenseFacing));
            if (damageReductionPercent < 1 || damageReductionPercent > 100)
                throw new ArgumentOutOfRangeException(nameof(damageReductionPercent));

            return new CategorySnapshot(
                CategoryKey.Heavy,
                null,
                defenseFacing,
                movedThisTurn,
                damageReductionPercent);
        }

        private static void ValidateKey(CategoryKey key)
        {
            if (key == CategoryKey.None || Enum.IsDefined(typeof(CategoryKey), key) == false)
                throw new ArgumentOutOfRangeException(nameof(key));
        }
    }
}
