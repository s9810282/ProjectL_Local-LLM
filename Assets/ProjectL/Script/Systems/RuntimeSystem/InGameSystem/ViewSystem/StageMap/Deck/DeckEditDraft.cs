using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Deck
{
    internal enum DeckEditDraftState
    {
        /// <summary>기준 스냅샷이 없습니다(Run·덱 없음 또는 읽는 중 원본이 바뀜).</summary>
        NotReady,

        /// <summary>후보가 바뀌었고 아직 Core 평가 전입니다.</summary>
        Unevaluated,

        /// <summary>마지막 평가가 수락됐습니다. 후보는 Core가 확정할 순서입니다.</summary>
        Valid,

        /// <summary>마지막 평가가 덱 규칙으로 거절됐습니다.</summary>
        Invalid,

        /// <summary>기준 버전이 낡았습니다. 사용자가 다시 평가해야 확정할 수 있습니다.</summary>
        Stale,

        /// <summary>지금은 편집 창이 닫혀 있습니다(<see cref="DeckEditDraft.Availability"/>).</summary>
        Unavailable
    }

    /// <summary>
    /// 덱 편집 초안 하나입니다(Run 덱 설계 §5.3). 후보 ID와 그것을 평가할 기준 스냅샷을 함께 가집니다.
    /// 덱 규칙은 판단하지 않고 Core 평가·확정 결과만 반영합니다. 새 버전을 평가되지 않은 초안에 몰래 붙이지 않습니다.
    /// 다만 화면 표시 규칙(병과당 한 칸, <see cref="DeckEditSlotLayout"/>)은 있습니다. 칸에 놓을 수 없는 덱은 표시 모델이 편집만 막습니다.
    /// </summary>
    internal sealed class DeckEditDraft
    {
        private readonly List<CharacterInstanceId> _candidates = new();
        private DeckEditRequest _pendingRequest;

        internal DeckEditorSnapshot Basis { get; private set; }
        internal DeckEditDraftState State { get; private set; } = DeckEditDraftState.NotReady;
        internal DeckReadStatus ReadStatus { get; private set; } = DeckReadStatus.NotReady;
        internal DeckFailureReason Reason { get; private set; }
        private DeckFailureReason _placementRejection;

        /// <summary>가장 최근에 받은 편집 가능 여부입니다. 기준 스냅샷 또는 평가·확정 결과의 현재 상태에서 옵니다.</summary>
        internal DeckEditAvailability Availability { get; private set; }

        /// <summary>바로 앞 동작이 수락된 확정이었는지입니다(안내 문구용). 다음 편집에서 지웁니다.</summary>
        internal bool Committed { get; private set; }

        /// <summary>표시·요청 순서의 후보입니다. 평가가 수락되면 Core의 정규 순서(메인 0번)로 바뀝니다.</summary>
        internal IReadOnlyList<CharacterInstanceId> Candidates => _candidates;

        /// <summary>평가 후 정규 후보와 기준 덱의 실제 차이입니다. 편집했다가 되돌리면 false입니다.</summary>
        internal bool HasChanges
        {
            get
            {
                if (Basis == null) return false;
                var original = Basis.Deck.OrderedIds;
                if (original.Count != _candidates.Count) return true;
                for (int i = 0; i < original.Count; i++)
                    if (original[i] != _candidates[i]) return true;
                return false;
            }
        }

        internal bool CanEdit => Basis != null && State != DeckEditDraftState.NotReady &&
            State != DeckEditDraftState.Stale && State != DeckEditDraftState.Unavailable;

        /// <summary>다시 평가하면 풀리는 상태(기준이 낡음·아직 준비 안 됨)인지입니다. 다시 평가 버튼을 이때만 보입니다.</summary>
        internal bool CanReevaluate => State == DeckEditDraftState.Stale || State == DeckEditDraftState.NotReady;

        internal bool CanConfirm => Basis != null && State == DeckEditDraftState.Valid &&
            Availability != null && Availability.CanEdit && !Committed;

        /// <summary>열 때 부릅니다. 확정 덱으로 후보를 채웁니다.</summary>
        internal void Open(DeckReadResult read)
        {
            Clear();
            if (!Accept(read)) return;
            _candidates.AddRange(Basis.Deck.OrderedIds);
            MarkUnevaluated();
        }

        /// <summary>
        /// 사용자가 다시 평가를 누르면 부릅니다. 새 스냅샷을 기준으로 삼되 후보는 그대로 둡니다.
        /// 사라진 선택을 몰래 지우거나 바꾸지 않습니다(그 결과는 평가가 거절로 알려 줌).
        /// </summary>
        internal void Rebase(DeckReadResult read)
        {
            bool hadBasis = Basis != null;
            if (!Accept(read)) return;
            if (!hadBasis)
            {
                _candidates.Clear();
                _candidates.AddRange(Basis.Deck.OrderedIds);
            }
            Committed = false;
            MarkUnevaluated();
        }

        /// <summary>병과 슬롯의 교체 후보를 한 요청으로 평가합니다. 수락되기 전에는 현재 후보를 변경하지 않습니다.</summary>
        internal DeckEditRequest CreatePlacementRequest(CharacterInstanceId id, int slotIndex)
        {
            if (!CanEdit || id.IsEmpty || _candidates.Contains(id)) return null;
            CategoryKey category = DeckEditSlotLayout.CategoryAt(slotIndex);
            if (CategoryOf(id) != category) return null;
            var replacement = new List<CharacterInstanceId>(_candidates);
            int target = -1;
            for (int i = 0; i < replacement.Count; i++)
            {
                if (CategoryOf(replacement[i]) != category) continue;
                if (target >= 0)
                    throw new InvalidOperationException($"[DeckEditDraft] UIId.DeckEdit: {category} 슬롯에 대응하는 유닛이 중복됩니다.");
                target = i;
            }
            if (target >= 0) replacement[target] = id;
            else replacement.Add(id);
            // 확정 표지는 놓기가 수락돼 후보가 실제로 바뀔 때 지웁니다(ApplyEvaluation).
            _pendingRequest = new DeckEditRequest(Basis.RunId, Basis.RosterRevision, Basis.DeckRevision, replacement);
            return _pendingRequest;
        }

        private CategoryKey CategoryOf(CharacterInstanceId id)
        {
            foreach (var stats in Basis.Stats)
                if (stats.OwnedId == id) return stats.Stats.CategoryKey;
            // 다시 평가 중 사라진 보유 ID는 정상적인 Core 거절 대상입니다.
            return CategoryKey.None;
        }

        /// <summary>고정 병과 칸 하나를 비웁니다. 표시 인덱스와 Core 후보 인덱스를 구분합니다.</summary>
        internal void RemoveSlot(int index)
        {
            if (!CanEdit) return;
            CategoryKey category = DeckEditSlotLayout.CategoryAt(index);
            for (int i = 0; i < _candidates.Count; i++)
            {
                if (CategoryOf(_candidates[i]) != category) continue;
                _candidates.RemoveAt(i);
                Committed = false;
                MarkUnevaluated();
                return;
            }
        }

        /// <summary>지금 후보로 평가·확정 요청을 만듭니다. 기준 스냅샷의 버전을 그대로 씁니다.</summary>
        internal DeckEditRequest CreateRequest()
        {
            if (Basis == null) return null;
            _pendingRequest = new DeckEditRequest(Basis.RunId, Basis.RosterRevision, Basis.DeckRevision, _candidates);
            return _pendingRequest;
        }

        internal void ApplyEvaluation(DeckEvaluationResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            // 지금 후보로 만든 요청의 결과만 반영합니다.
            if (!ReferenceEquals(result.Request, _pendingRequest)) return;
            _pendingRequest = null;
            Apply(result.Reason, result.Current);
            if (State == DeckEditDraftState.Valid && result.CanonicalIds != null)
            {
                if (!SameCandidates(result.CanonicalIds)) Committed = false;
                _candidates.Clear();
                _candidates.AddRange(result.CanonicalIds);
            }
        }

        /// <summary>
        /// 놓기(교체) 평가 결과를 반영합니다. 덱 규칙으로 거절되면 현재 후보의 평가 상태는 그대로 두고 거절 사유만 따로 남깁니다(평가 C-08).
        /// 기준이 낡음·편집 불가 같은 초안 전체의 상태 변화는 일반 평가처럼 반영합니다.
        /// </summary>
        internal void ApplyPlacementEvaluation(DeckEvaluationResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (!ReferenceEquals(result.Request, _pendingRequest)) return;
            if (StateFor(result.Reason, result.Current) == DeckEditDraftState.Invalid)
            {
                _pendingRequest = null;
                UpdateAvailability(result.Current);
                _placementRejection = result.Reason;
                return;
            }
            ApplyEvaluation(result);
        }

        /// <summary>마지막 놓기 거절 사유를 꺼내고 지웁니다. 안내는 한 번만 띄웁니다.</summary>
        internal DeckFailureReason TakePlacementRejection()
        {
            DeckFailureReason reason = _placementRejection;
            _placementRejection = DeckFailureReason.None;
            return reason;
        }

        private bool SameCandidates(IReadOnlyList<CharacterInstanceId> ids)
        {
            if (ids.Count != _candidates.Count) return false;
            for (int i = 0; i < ids.Count; i++)
                if (ids[i] != _candidates[i]) return false;
            return true;
        }

        /// <summary>거절된 확정을 반영합니다. 수락된 확정은 <see cref="MarkCommitted"/> 뒤 <see cref="Open"/>으로 새 기준을 받습니다.</summary>
        internal void ApplyRejectedConfirm(DeckEditResult result)
        {
            if (result == null) throw new ArgumentNullException(nameof(result));
            if (result.Accepted) throw new InvalidOperationException("[DeckEditDraft] 수락된 확정은 거절로 반영할 수 없습니다.");
            _pendingRequest = null;
            Apply(result.Reason, result.Current);
        }

        internal void MarkCommitted() => Committed = true;

        internal void Clear()
        {
            _candidates.Clear();
            _pendingRequest = null;
            Basis = null;
            State = DeckEditDraftState.NotReady;
            ReadStatus = DeckReadStatus.NotReady;
            Reason = DeckFailureReason.None;
            _placementRejection = DeckFailureReason.None;
            Availability = null;
            Committed = false;
        }

        private bool Accept(DeckReadResult read)
        {
            if (read == null) throw new ArgumentNullException(nameof(read));
            ReadStatus = read.Status;
            _pendingRequest = null;
            if (read.Status != DeckReadStatus.Ready || read.Snapshot == null)
            {
                Basis = null;
                State = DeckEditDraftState.NotReady;
                return false;
            }
            Basis = read.Snapshot;
            Availability = Basis.Availability;
            return true;
        }

        private void MarkUnevaluated()
        {
            _pendingRequest = null;
            Reason = DeckFailureReason.None;
            State = DeckEditDraftState.Unevaluated;
        }

        private void Apply(DeckFailureReason reason, DeckRuleState current)
        {
            Reason = reason;
            UpdateAvailability(current);
            State = StateFor(reason, current);
        }

        private void UpdateAvailability(DeckRuleState current)
        {
            if (current != null && current.Status == DeckReadStatus.Ready && current.Availability != null)
                Availability = current.Availability;
        }

        private DeckEditDraftState StateFor(DeckFailureReason reason, DeckRuleState current)
        {
            // 거절이든 수락이든 원본 버전이 기준과 다르면 낡은 초안입니다.
            if (current != null && current.Status == DeckReadStatus.Ready && !MatchesBasis(current))
                return DeckEditDraftState.Stale;
            return reason switch
            {
                DeckFailureReason.None => DeckEditDraftState.Valid,
                DeckFailureReason.StaleRun or DeckFailureReason.StaleRoster or DeckFailureReason.StaleDeck or
                    DeckFailureReason.StateChanged => DeckEditDraftState.Stale,
                DeckFailureReason.WrongPhase => DeckEditDraftState.Unavailable,
                DeckFailureReason.NotReady => DeckEditDraftState.NotReady,
                _ => DeckEditDraftState.Invalid
            };
        }

        private bool MatchesBasis(DeckRuleState current) =>
            Basis != null && current.Deck != null && current.RosterRevision == Basis.RosterRevision &&
            current.Deck.RunId == Basis.RunId && current.Deck.DeckRevision == Basis.DeckRevision;
    }
}
