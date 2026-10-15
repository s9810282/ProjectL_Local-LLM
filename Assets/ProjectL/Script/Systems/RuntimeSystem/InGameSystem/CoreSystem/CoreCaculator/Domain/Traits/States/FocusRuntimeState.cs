using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.States
{
    /// <summary>
    /// 개별 Unit이 Stage 동안 보유하는 Focus Trait의 현재 상태입니다.
    /// </summary>
    public sealed class FocusRuntimeState : TraitRuntimeState
    {
        /// <summary>
        /// 현재 적용 중인 Focus 스택 수입니다. 지금 수행하는 공격의 피해 보너스에 반영됩니다.
        /// </summary>
        public int ActiveStacks { get; private set; }

        /// <summary>
        /// 다음 자기 턴 시작에 Focus 스택 1개를 적용하도록 예약했는지를 나타냅니다.
        /// </summary>
        public bool GainScheduled { get; private set; }

        /// <summary>
        /// 이번 자기 턴에 성공한 이동이 있었는지를 나타냅니다.
        /// true이면 같은 턴의 공격 성공으로 새 Focus 스택을 예약할 수 없습니다.
        /// </summary>
        public bool MovedThisTurn { get; private set; }

        public FocusRuntimeState()
            : this(activeStacks: 0, gainScheduled: false, movedThisTurn: false)
        {
        }

        public FocusRuntimeState(
            int activeStacks,
            bool gainScheduled,
            bool movedThisTurn)
            : base(TraitKey.Focus)
        {
            if (activeStacks < 0)
                throw new ArgumentOutOfRangeException(nameof(activeStacks));

            ActiveStacks = activeStacks;
            GainScheduled = gainScheduled;
            MovedThisTurn = movedThisTurn;
        }

        internal override TraitRuntimeState Clone()
        {
            return new FocusRuntimeState(ActiveStacks, GainScheduled, MovedThisTurn);
        }

        internal override void CopyFinalValuesFrom(TraitRuntimeState source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (source is not FocusRuntimeState focusSource)
            {
                throw new ArgumentException("Focus 상태에는 Focus 최종값만 복사할 수 있습니다.", nameof(source));
            }

            ActiveStacks = focusSource.ActiveStacks;
            GainScheduled = focusSource.GainScheduled;
            MovedThisTurn = focusSource.MovedThisTurn;
        }
    }
}
