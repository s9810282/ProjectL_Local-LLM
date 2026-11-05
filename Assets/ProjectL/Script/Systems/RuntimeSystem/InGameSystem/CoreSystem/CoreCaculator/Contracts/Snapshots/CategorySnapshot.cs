using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots
{
    /// <summary>
    /// 추가 상태가 없는 병과의 공개 식별 값입니다.
    /// </summary>
    public sealed class CategorySnapshot
    {
        public CategoryKey Key { get; }

        private CategorySnapshot(CategoryKey key) => Key = key;

        /// <summary>추가 공개 상태를 제공하지 않는 카테고리의 사본입니다.</summary>
        public static CategorySnapshot CreateStateless(CategoryKey key)
        {
            ValidateKey(key);
            return new CategorySnapshot(key);
        }

        private static void ValidateKey(CategoryKey key)
        {
            if (key == CategoryKey.None || Enum.IsDefined(typeof(CategoryKey), key) == false)
                throw new ArgumentOutOfRangeException(nameof(key));
        }

        /// <summary>
        /// 두 분류 상태가 같은지다. 둘 다 null이면 같고 하나만 null이면 다르다
        /// (기본값 능력치는 분류가 null이다).
        /// </summary>
        public static bool Same(CategorySnapshot left, CategorySnapshot right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (left == null || right == null)
                return false;

            return left.Key == right.Key;
        }
    }
}
