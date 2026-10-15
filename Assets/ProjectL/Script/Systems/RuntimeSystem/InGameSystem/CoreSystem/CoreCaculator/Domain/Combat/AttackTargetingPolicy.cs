namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat
{
    /// <summary>공격 지정과 대상 자격의 불변 정책이다. 기본값은 예외 권한과 근거리 제한이 모두 없다.</summary>
    internal readonly struct AttackTargetingPolicy
    {
        public bool AllowsEmptySingle { get; }
        public bool AllowsFriendlyTargets { get; }
        public bool AllowsNeutralTargets { get; }
        public bool ExcludesAdjacentDirectCells { get; }

        public AttackTargetingPolicy(bool allowsEmptySingle, bool allowsFriendlyTargets,
            bool allowsNeutralTargets, bool excludesAdjacentDirectCells)
        {
            AllowsEmptySingle = allowsEmptySingle;
            AllowsFriendlyTargets = allowsFriendlyTargets;
            AllowsNeutralTargets = allowsNeutralTargets;
            ExcludesAdjacentDirectCells = excludesAdjacentDirectCells;
        }

        /// <summary>권한과 제한을 각각 OR로 합친다. 다른 규칙의 기여를 취소하지 않는다.</summary>
        internal AttackTargetingPolicy Combine(in AttackTargetingPolicy other) => new AttackTargetingPolicy(
            AllowsEmptySingle || other.AllowsEmptySingle,
            AllowsFriendlyTargets || other.AllowsFriendlyTargets,
            AllowsNeutralTargets || other.AllowsNeutralTargets,
            ExcludesAdjacentDirectCells || other.ExcludesAdjacentDirectCells);
    }
}
