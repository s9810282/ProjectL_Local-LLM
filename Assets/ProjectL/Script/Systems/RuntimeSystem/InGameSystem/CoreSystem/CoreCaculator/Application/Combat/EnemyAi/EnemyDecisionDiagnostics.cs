using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat.EnemyAi;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi
{
    /// <summary>
    /// 행위자가 이번 결정에서 행동하지 않은 이유다(적 AI 근간 개편 계획 R-3, 최소 집합). 행위자·결정마다 하나다.
    /// </summary>
    internal enum EnemyHoldReason
    {
        /// <summary>교전 필드에서 닿는 공격 위치가 없다.</summary>
        NoReachableTarget,
        /// <summary>교전 필드가 칸 상한에 걸려 탐색이 끝나지 않았다("닿는 대상 없음"과 구별한다).</summary>
        FieldTruncated,
        /// <summary>모든 후보가 확실히 치명적이다.</summary>
        LethalOnly,
        /// <summary>치명적이지 않은 후보가 있지만 교환 점수가 대기보다 낮다.</summary>
        ExchangeLoss,
        /// <summary>공개 사실로 정할 수 없는 결과(아군 피해 상한 등) 때문에 대기보다 낮거나, 실제 정산이 고른 행동을 거절했다.</summary>
        WorldUncertainty,
        /// <summary>빔(이동 후보 K개)이 대기보다 나은 선택지를 잘라 냈다(계획 R-4). 잘린 칸의 저가 점수가 대기보다 높았다.</summary>
        Pruned
    }

    /// <summary>결정 하나의 수치와 선택적으로 보관하는 후보 표본이다. 측정 시각과 문자열은 호출자가 다룬다.</summary>
    public sealed class EnemyDecisionDiagnostics
    {
        private readonly Alternative[] _alternatives;
        private readonly List<HoldRecord> _holds;
        private readonly List<ActorRecord> _actors;

        /// <param name="captureAlternatives">참이면 후보 표본, 대기 이유와 후보별 점수 기록(설계 6.9)을 함께 보관한다.</param>
        public EnemyDecisionDiagnostics(bool captureAlternatives = false)
        {
            if (!captureAlternatives) return;
            _alternatives = new Alternative[4];
            _holds = new List<HoldRecord>();
            _actors = new List<ActorRecord>();
        }

        internal int HoldCount => _holds?.Count ?? 0;

        internal HoldRecord GetHold(int index)
        {
            if (_holds == null || index < 0 || index >= _holds.Count) throw new ArgumentOutOfRangeException(nameof(index));
            return _holds[index];
        }

        internal void RecordHold(EntityId actorId, EnemyHoldReason reason)
        {
            if (_holds == null) return;
            _holds.Add(new HoldRecord(actorId, reason));
            // 같은 행위자의 마지막 평가 기록에 대기 이유를 붙인다(기록은 평가 순서대로 쌓인다).
            for (int i = _actors.Count - 1; i >= 0; i--)
                if (_actors[i].ActorId == actorId)
                {
                    _actors[i].HoldReason = reason;
                    break;
                }
        }

        /// <summary>평가한 행위자 기록 수다. 행위자를 평가할 때마다 하나씩 쌓인다(같은 결정 안의 평가 순서).</summary>
        internal int ActorRecordCount => _actors?.Count ?? 0;

        internal ActorRecord GetActorRecord(int index)
        {
            if (_actors == null || index < 0 || index >= _actors.Count) throw new ArgumentOutOfRangeException(nameof(index));
            return _actors[index];
        }

        /// <summary>행위자 평가를 시작한다. 대기 점수 항목과 제자리 치명 판정을 남긴다. 보관하지 않으면 null이다.</summary>
        internal ActorRecord BeginActor(EntityId actorId, EnemyScoreTerms holdTerms, int holdTier, bool holdLethal)
        {
            if (_actors == null) return null;
            var record = new ActorRecord(actorId, holdTerms, holdTier, holdLethal);
            _actors.Add(record);
            return record;
        }

        /// <summary>
        /// 행위자 한 명의 평가 기록이다(설계 6.9): 대기 항목, 비교한 모든 후보의 항목, 대기했으면 그 이유다.
        /// 후보는 이동 정밀 재평가를 마친 최종 값이다. 받아들여지지 않은(정산 거절) 후보는 평가 결과가 없어 빠진다.
        /// </summary>
        internal sealed class ActorRecord
        {
            private readonly List<CandidateRecord> _candidates = new List<CandidateRecord>();

            internal EntityId ActorId { get; }
            internal EnemyScoreTerms HoldTerms { get; }
            internal int HoldTier { get; }
            internal bool HoldLethal { get; }
            /// <summary>이 평가에서 행동을 고르지 못했으면 그 이유다. 행동을 골랐으면 null이다.</summary>
            internal EnemyHoldReason? HoldReason { get; set; }
            internal IReadOnlyList<CandidateRecord> Candidates => _candidates;

            internal ActorRecord(EntityId actorId, EnemyScoreTerms holdTerms, int holdTier, bool holdLethal)
            {
                ActorId = actorId;
                HoldTerms = holdTerms;
                HoldTier = holdTier;
                HoldLethal = holdLethal;
            }

            internal void Add(CandidateRecord candidate) => _candidates.Add(candidate);
        }

        /// <summary>후보 하나의 점수 항목·등급·확실한 치명 판정과 정밀 평가 여부다.</summary>
        internal readonly struct CandidateRecord
        {
            internal EnemyDecision Action { get; }
            internal EnemyScoreTerms Terms { get; }
            internal int Tier { get; }
            internal bool Lethal { get; }
            internal bool Precise { get; }

            internal CandidateRecord(EnemyDecision action, EnemyScoreTerms terms, int tier, bool lethal, bool precise)
            {
                Action = action;
                Terms = terms;
                Tier = tier;
                Lethal = lethal;
                Precise = precise;
            }
        }

        internal readonly struct HoldRecord
        {
            internal EntityId ActorId { get; }
            internal EnemyHoldReason Reason { get; }

            internal HoldRecord(EntityId actorId, EnemyHoldReason reason)
            {
                ActorId = actorId;
                Reason = reason;
            }
        }

        public int WorkUnits { get; internal set; }
        public int ExpandedNodes { get; internal set; }
        public int GeneratedAttacks { get; internal set; }
        public int GeneratedMoves { get; internal set; }
        public int PreviewCalls { get; internal set; }
        public int FollowupPreviewCalls { get; internal set; }
        public int RefinedMoves { get; internal set; }
        public int CacheHits { get; internal set; }
        public int RejectedAttempts { get; internal set; }
        public int CapHits { get; internal set; }
        public int AlternativeCount { get; private set; }
        internal AttackAim? SelectedAim { get; set; }
        internal EnemyDecision LastRejectedDecision { get; private set; }
        internal string LastRejectionReason { get; private set; }

        internal void RecordRejection(EnemyDecision decision, string reason)
        {
            RejectedAttempts++;
            LastRejectedDecision = decision;
            if (_alternatives != null) LastRejectionReason = reason;
        }

        public Alternative GetAlternative(int index)
        {
            if (index < 0 || index >= AlternativeCount) throw new ArgumentOutOfRangeException(nameof(index));
            return _alternatives[index];
        }

        internal void RecordAlternative(EntityId actorId, AttackAim? aim, CellCoord? destination)
        {
            if (_alternatives == null || AlternativeCount == _alternatives.Length) return;
            _alternatives[AlternativeCount++] = new Alternative(actorId, aim, destination);
        }

        public readonly struct Alternative
        {
            internal EntityId ActorId { get; }
            public AttackAim? Aim { get; }
            public CellCoord? Destination { get; }

            internal Alternative(EntityId actorId, AttackAim? aim, CellCoord? destination)
            {
                ActorId = actorId;
                Aim = aim;
                Destination = destination;
            }
        }
    }
}
