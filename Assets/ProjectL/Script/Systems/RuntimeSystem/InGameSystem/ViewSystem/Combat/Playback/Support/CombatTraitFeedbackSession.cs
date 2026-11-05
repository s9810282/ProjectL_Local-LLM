using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Support
{
    /// <summary>한 결과의 시작·상태 소비·흔들림·발동 유닛 텍스트 시작 여부를 단독 소유합니다.</summary>
    internal sealed class CombatTraitFeedbackSession
    {
        private CombatTraitFeedbackPlan _plan;
        private readonly HashSet<int> _started = new();
        private readonly HashSet<int> _shaken = new();
        private readonly HashSet<int> _sourceTextStarted = new();
        private readonly HashSet<CombatTraitStateEvent> _consumed = new();
        internal void Begin(CombatTraitFeedbackPlan plan)
        {
            Clear();
            _plan = plan ?? throw new ArgumentNullException(nameof(plan));
        }

        internal bool TryStart(CombatEffectsEvent source, int index, out CombatTraitFeedbackPlan.Occurrence occurrence)
        {
            occurrence = null;
            return _plan != null && _plan.TryGetOccurrence(source, index, out occurrence) && _started.Add(occurrence.Id);
        }

        /// <summary>이번 결과 계획의 발동 하나입니다. 시작 여부와 관계없이 찾기만 합니다.</summary>
        internal bool TryGetOccurrence(CombatEffectsEvent source, int index, out CombatTraitFeedbackPlan.Occurrence occurrence)
        {
            occurrence = null;
            return _plan != null && _plan.TryGetOccurrence(source, index, out occurrence);
        }

        internal bool TryConsumeState(CombatTraitStateEvent state, out IReadOnlyList<CombatTraitFeedbackPlan.Binding> bindings)
        {
            bindings = null;
            return _plan != null && !_consumed.Contains(state) && _plan.TryGetBindings(state, out bindings) && _consumed.Add(state);
        }

        internal bool TryBeginShake(CombatTraitFeedbackPlan.Occurrence occurrence) => _shaken.Add(occurrence.Id);

        /// <summary>All에서 상태 수신자가 아닌 발동 유닛의 텍스트는 첫 대응 상태 뒤 한 번만 시작합니다.</summary>
        internal bool TryBeginSourceText(CombatTraitFeedbackPlan.Occurrence occurrence) => _sourceTextStarted.Add(occurrence.Id);

        internal void Clear()
        {
            _plan = null;
            _started.Clear();
            _shaken.Clear();
            _sourceTextStarted.Clear();
            _consumed.Clear();
        }
    }
}
