namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common
{
    /// <summary>람다 걷기 측정 사본 전용: 특성 상태 목록에 그 키가 있나(ToArray·Any 없이).</summary>
    public static class TraitStatusScan
    {
        public static bool Has(System.ReadOnlyMemory<Snapshots.Traits.TraitStatusSnapshot> statuses, TraitKey a)
        {
            foreach (var t in statuses.Span) if (t.Key == a) return true;
            return false;
        }

        public static bool Has(System.ReadOnlyMemory<Snapshots.Traits.TraitStatusSnapshot> statuses, TraitKey a, TraitKey b)
        {
            foreach (var t in statuses.Span) if (t.Key == a || t.Key == b) return true;
            return false;
        }
    }
}
