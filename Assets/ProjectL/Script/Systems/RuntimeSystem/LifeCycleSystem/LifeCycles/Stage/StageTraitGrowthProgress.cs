using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Stage
{
    /// <summary>
    /// 증강 진행을 Core에서 읽어 둡니다(강화 View 설계 §14 TG6). 읽기만 하고 처리·준비·결과 확인은 하지 않습니다.
    /// </summary>
    /// <remarks>
    /// Core 진행 읽기는 표가 있으면 카탈로그를 매번 새로 만듭니다. 그래서 Core가 읽는 원본과 같은 열쇠가 바뀔 때만 다시 읽습니다
    /// (강화 View 계획 P-TG6, 옛 TraitGrowthFlow와 같음).
    /// </remarks>
    internal sealed class StageTraitGrowthProgress
    {
        private readonly ITraitGrowthAvailabilityRead _core;
        private readonly IBufferQuery _buffers;
        private ProgressKey _key;
        private bool _hasKey;
        private TraitGrowthProgressSnapshot _last;

        internal StageTraitGrowthProgress(ITraitGrowthAvailabilityRead core, IBufferQuery buffers)
        {
            _core = core ?? throw new ArgumentNullException(nameof(core));
            _buffers = buffers ?? throw new ArgumentNullException(nameof(buffers));
        }

        /// <summary>선택 필요 또는 결과 확인 대기인지입니다. 확인과 다음 준비 사이에도 참입니다.</summary>
        internal bool IsPending
        {
            get
            {
                Refresh();
                return _last.Status == TraitGrowthProgressStatus.ChoiceRequired ||
                    _last.Status == TraitGrowthProgressStatus.AwaitingAcknowledgement;
            }
        }

        /// <summary>선택이 필요한데 제안이 아직 없는지(준비할 차례)입니다.</summary>
        internal bool NeedsOffer
        {
            get
            {
                Refresh();
                return _last.Status == TraitGrowthProgressStatus.ChoiceRequired && _last.ActiveOffer == null;
            }
        }

        /// <summary>지금 원본 열쇠를 돌려받아, 준비 뒤에 원본이 바뀌었는지 <see cref="HasChangedSince"/>로 봅니다.</summary>
        internal ProgressKey ReadKey() => ProgressKey.Read(_buffers);

        internal bool HasChangedSince(ProgressKey key) => !ProgressKey.Read(_buffers).Equals(key);

        internal void Reset()
        {
            _hasKey = false;
            _last = null;
        }

        private void Refresh()
        {
            ProgressKey key = ProgressKey.Read(_buffers);
            if (_hasKey && key.Equals(_key)) return;
            _last = _core.ReadTraitGrowthProgress();
            _key = key;
            _hasKey = true;
        }

        /// <summary>Core 진행 읽기(CharacterRosterService.ReadTraitGrowthProgressCore)가 읽는 원본 그대로입니다.</summary>
        internal readonly struct ProgressKey : IEquatable<ProgressKey>
        {
            private readonly RunId _run;
            private readonly RunOutcome _outcome;
            private readonly bool _battle;
            private readonly int _fieldCharacters;
            private readonly long _roster;
            private readonly TraitGrowthOffer _offer;
            private readonly int _handled;
            private readonly int _acknowledged;

            private ProgressKey(RunId run, RunOutcome outcome, bool battle, int fieldCharacters, long roster,
                TraitGrowthOffer offer, int handled, int acknowledged)
            {
                _run = run;
                _outcome = outcome;
                _battle = battle;
                _fieldCharacters = fieldCharacters;
                _roster = roster;
                _offer = offer;
                _handled = handled;
                _acknowledged = acknowledged;
            }

            internal static ProgressKey Read(IBufferQuery buffers) => new(
                buffers.RunProgressReadSink.RunId, buffers.RunProgressReadSink.Outcome,
                buffers.CombatStateReadSink.HasBattle, buffers.CharacterReadSink.Characters.Count,
                buffers.CharacterReadSink.RosterRevision, buffers.TraitGrowthStateReadSink.ActiveOffer,
                buffers.TraitGrowthResultReadSink.HandledCommandNumber,
                buffers.TraitGrowthViewReadSink.AcknowledgedResultNumber);

            public bool Equals(ProgressKey other) =>
                _run.Equals(other._run) && _outcome == other._outcome && _battle == other._battle &&
                _fieldCharacters == other._fieldCharacters && _roster == other._roster &&
                ReferenceEquals(_offer, other._offer) && _handled == other._handled &&
                _acknowledged == other._acknowledged;

            public override bool Equals(object obj) => obj is ProgressKey other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(_run, _roster, _handled, _acknowledged);
        }
    }
}
