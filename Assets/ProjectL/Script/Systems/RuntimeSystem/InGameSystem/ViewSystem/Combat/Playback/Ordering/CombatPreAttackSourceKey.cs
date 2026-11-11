using System;
using System.Runtime.CompilerServices;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering
{
    /// <summary>결과 참조·재생 세대·출처 위치로 구분하는 공격 실행의 키입니다.</summary>
    internal readonly struct CombatPreAttackSourceKey : IEquatable<CombatPreAttackSourceKey>
    {
        internal CombatPreAttackSourceKey(CombatResult result, int generation, int eventIndex)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
            if (eventIndex < 0 || eventIndex >= result.Events.Count)
                throw new ArgumentOutOfRangeException(nameof(eventIndex));
            Generation = generation;
            EventIndex = eventIndex;
        }

        internal CombatResult Result { get; }
        internal int Generation { get; }
        internal int EventIndex { get; }

        public bool Equals(CombatPreAttackSourceKey other) =>
            ReferenceEquals(Result, other.Result) && Generation == other.Generation && EventIndex == other.EventIndex;

        public override bool Equals(object obj) => obj is CombatPreAttackSourceKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Result == null ? 0 : RuntimeHelpers.GetHashCode(Result);
                return (hash * 397 ^ Generation) * 397 ^ EventIndex;
            }
        }
    }
}
