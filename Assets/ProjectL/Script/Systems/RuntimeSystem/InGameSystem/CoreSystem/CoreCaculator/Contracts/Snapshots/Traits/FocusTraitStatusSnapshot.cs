using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits
{
    public sealed class FocusTraitStatusSnapshot : TraitStatusSnapshot
    {
        public int ActiveStacks { get; } // 현재 실제 적용 중인 stack
        public bool GainScheduled { get; } // 다음 자기 턴에 stack 증가 예약되어있는지
        public bool MovedThisTurn { get; } // 이번 턴에 이동했는지

        public FocusTraitStatusSnapshot(int activeStacks, bool gainScheduled, bool movedThisTurn)
            : base(global::ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common.TraitKey.Focus)
        {
            if (activeStacks < 0)
                throw new ArgumentOutOfRangeException(nameof(activeStacks));
 
            ActiveStacks = activeStacks;
            GainScheduled = gainScheduled;
            MovedThisTurn = movedThisTurn;
        }

        public override bool SameValues(TraitStatusSnapshot other) => other is FocusTraitStatusSnapshot b &&
            ActiveStacks == b.ActiveStacks && GainScheduled == b.GainScheduled && MovedThisTurn == b.MovedThisTurn;
    }
}
