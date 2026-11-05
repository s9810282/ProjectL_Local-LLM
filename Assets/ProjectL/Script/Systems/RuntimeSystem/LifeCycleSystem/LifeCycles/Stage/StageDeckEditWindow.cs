using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage
{
    /// <summary>
    /// StageSelect의 노드 상호작용 사이에서만 덱 편집을 여는 창입니다(Run 덱 설계 §7.1, C1 요청 §2).
    /// 사실은 갖지 않고 판단 순서만 가집니다. 전투 유지·Run 종료는 Core가 먼저 막으므로 여기서 판단하지 않습니다.
    /// </summary>
    internal sealed class StageDeckEditWindow : IDeckEditAvailabilityRead
    {
        // 결과는 이유별로 하나씩 재사용합니다(불변 값).
        private static readonly DeckEditAvailability[] Results = CreateResults();
        private IStageDeckEditFacts _facts;
        private bool _stopped;

        internal void Bind(IStageDeckEditFacts facts)
        {
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _stopped = false;
        }

        /// <summary>Stage 수명이 종료되면 부릅니다. 그 뒤로는 <see cref="DeckUnavailableReason.Stopped"/>입니다.</summary>
        internal void Unbind()
        {
            _facts = null;
            _stopped = true;
        }

        public DeckEditAvailability ReadDeckEditAvailability() => Results[(int)ReadReason()];

        internal DeckUnavailableReason ReadReason()
        {
            if (_stopped) return DeckUnavailableReason.Stopped;
            IStageDeckEditFacts facts = _facts;
            if (facts == null) return DeckUnavailableReason.NotReady;
            if (facts.IsFaulted) return DeckUnavailableReason.Faulted;
            if (!facts.IsStageActive) return DeckUnavailableReason.InactiveStage;
            if (facts.IsLoading) return DeckUnavailableReason.Loading;
            if (!facts.IsRunnerIdle) return DeckUnavailableReason.JobPending;
            if (facts.IsFlowStopped) return DeckUnavailableReason.Stopped;
            if (!facts.IsMapPrepared || !facts.HasRoster) return DeckUnavailableReason.NotReady;
            if (facts.HasPendingFlowCheck) return DeckUnavailableReason.JobPending;
            // Runner가 비어도 제출만 되고 다음 Flow Tick 전인 요청이 있으면 열지 않습니다.
            if (facts.HasUnregisteredRequest) return DeckUnavailableReason.RequestPending;
            if (facts.HasOpenInteraction) return DeckUnavailableReason.InteractionOpen;
            return DeckUnavailableReason.None;
        }

        private static DeckEditAvailability[] CreateResults()
        {
            Array values = System.Enum.GetValues(typeof(DeckUnavailableReason));
            int max = 0;
            foreach (DeckUnavailableReason value in values)
                max = Math.Max(max, (int)value);
            var results = new DeckEditAvailability[max + 1];
            foreach (DeckUnavailableReason value in values)
                results[(int)value] = new DeckEditAvailability(value);
            return results;
        }
    }
}
