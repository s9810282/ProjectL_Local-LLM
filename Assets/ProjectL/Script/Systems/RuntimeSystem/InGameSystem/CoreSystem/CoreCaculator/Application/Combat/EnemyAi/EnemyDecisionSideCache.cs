using System;
using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.StageGimmicks;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyAi
{
    /// <summary>
    /// 적 진영 하나 동안 결정끼리 다시 쓰는 공유 구조의 보관소다(적 AI 근간 개편 설계 6.8). 공개 멤버가 없는 불투명 핸들이다.
    /// 진영 시작에 만들고 진영 끝이나 전투가 바뀌면 버린다(<c>CombatAdvanceService</c>). 핸들을 넘기지 않으면 캐시를 쓰지 않는다.
    /// 보관소는 저장과 비교만 한다. 무엇을 읽었는지(읽기 집합)는 각 구조를 만드는 쪽이 자기 코드 옆에서 정한다:
    /// 위험 예보 <see cref="StageHazardForecast.ReadSet"/>, 위협 필드 <see cref="EnemyThreatField.ReadSet"/>.
    /// 목표 판정 재사용은 효과가 작고 읽기 집합이 가장 복잡해 걷었다(계획 R-6 뒤 사용자 결정, 2026-11-10).
    /// 전투 번호나 진영 턴 순번이 다른 입력이 오면 모든 항목을 버린다(같은 핸들을 잘못 넘겨도 다른 진영의 값을 쓰지 않는다).
    /// </summary>
    public sealed class EnemyDecisionSideCache
    {
        private bool _scoped;
        private long _battleInstanceId;
        private long _controllerTurnSequence;
        private StageHazardForecast.ReadSet _forecastReads;
        private StageHazardForecast _forecast;
        private EnemyThreatField.ReadSet _threatReads;
        private EnemyThreatField _threat;

        /// <summary>
        /// 턴 계획 AI가 이번 적 진영 턴에 세운 계획이다(연구 저장소 전용 연결, Assets/ResearchEnemyPlanner, 설계 CP4).
        /// 전투나 진영 턴이 바뀌면(<see cref="EnterScope"/>) 버린다.
        /// </summary>
        internal EnemyPlanner.EnemyTurnPlan TurnPlan { get; set; }

        // 시험·측정용 수치다. 결정에는 쓰지 않는다.
        internal int ScopeResets { get; private set; }
        internal int ForecastHits { get; private set; }
        internal int ForecastMisses { get; private set; }
        internal int ThreatHits { get; private set; }
        internal int ThreatMisses { get; private set; }

        /// <summary>입력의 전투·진영과 다르면 모든 항목을 버린다. 결정 문맥이 만들어질 때 한 번 부른다.</summary>
        internal void EnterScope(CombatCommandInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            long battle = input.BattleInstanceId;
            long sequence = input.State.Turn.ControllerTurnSequence;
            if (_scoped && battle == _battleInstanceId && sequence == _controllerTurnSequence) return;
            if (_scoped) ScopeResets++;
            _scoped = true;
            _battleInstanceId = battle;
            _controllerTurnSequence = sequence;
            _forecastReads = null;
            _forecast = null;
            _threatReads = null;
            _threat = null;
            TurnPlan = null;
        }

        /// <summary>놓친 이유별 횟수다(구조:처음 다른 항목, 측정용). 보관한 항목이 없으면 "empty"다.</summary>
        internal IReadOnlyDictionary<string, int> MissReasons => _missReasons;
        private readonly Dictionary<string, int> _missReasons = new Dictionary<string, int>();

        private void CountMiss(string structure, string reason)
        {
            string key = structure + ":" + reason;
            _missReasons[key] = _missReasons.TryGetValue(key, out int count) ? count + 1 : 1;
        }

        internal bool TryGetForecast(StageHazardForecast.ReadSet reads, out StageHazardForecast forecast)
        {
            string difference = _forecastReads == null ? "empty" : _forecastReads.Difference(reads);
            if (difference == null)
            {
                ForecastHits++;
                forecast = _forecast;
                return true;
            }
            ForecastMisses++;
            CountMiss("forecast", difference);
            forecast = null;
            return false;
        }

        internal void StoreForecast(StageHazardForecast.ReadSet reads, StageHazardForecast forecast)
        {
            _forecastReads = reads ?? throw new ArgumentNullException(nameof(reads));
            _forecast = forecast ?? throw new ArgumentNullException(nameof(forecast));
        }

        internal bool TryGetThreat(EnemyThreatField.ReadSet reads, out EnemyThreatField threat)
        {
            string difference = _threatReads == null ? "empty" : _threatReads.Difference(reads);
            if (difference == null)
            {
                ThreatHits++;
                threat = _threat;
                return true;
            }
            ThreatMisses++;
            CountMiss("threat", difference);
            threat = null;
            return false;
        }

        internal void StoreThreat(EnemyThreatField.ReadSet reads, EnemyThreatField threat)
        {
            _threatReads = reads ?? throw new ArgumentNullException(nameof(reads));
            _threat = threat ?? throw new ArgumentNullException(nameof(threat));
        }
    }

    /// <summary>
    /// 읽기 집합 비교에 쓰는 값 비교다. 같다고 답하면 내용이 같다는 뜻이다(참조가 같거나 값이 같다).
    /// 같은 내용이어도 다시 만든 객체는 다르다고 답할 수 있다(그러면 캐시가 다시 계산할 뿐 결과는 같다).
    /// </summary>
    internal static class EnemyReadSetValues
    {
        /// <summary>
        /// 공격 프로필 목록이다. 프로필은 보유 식별자·유닛 정의·지정 방식 셋이 전부다(관통 여부는 정의에서 파생).
        /// 입력 조립은 결정마다 프로필(과 보유 개체의 유닛 정의)을 새로 합성한다. 합성 재료는 전투 정의(기본 정의·지정 방식·특성 성장 표,
        /// 전투 번호마다 고정이라 캐시 범위가 덮는다)와 보유 기록(<see cref="SameOwned"/>가 값으로 비교한다)뿐이므로,
        /// 같은 범위에서는 보유 식별자·유닛 키·지정 방식이 같으면 정의 내용도 같다. 같은 전투 번호로 정의만 바꾼 입력은 이 비교가 구별하지 않는다.
        /// </summary>
        internal static bool SameProfiles(IReadOnlyList<CombatAttackProfile> left, IReadOnlyList<CombatAttackProfile> right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++)
            {
                var a = left[i];
                var b = right[i];
                if (!ReferenceEquals(a, b) && (a == null || b == null || a.OwnedId != b.OwnedId || a.Definition.Key != b.Definition.Key ||
                    !ReferenceEquals(a.Method, b.Method))) return false;
            }
            return true;
        }

        /// <summary>보유 기록 목록이다(프로필의 성장·전직 재료이고, 프로필 선택이 성장 여부를 읽는다). 값으로 비교한다.</summary>
        internal static bool SameOwned(IReadOnlyList<OwnedCharacterRecord> left, IReadOnlyList<OwnedCharacterRecord> right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Count != right.Count) return false;
            for (int i = 0; i < left.Count; i++)
                if (!ReferenceEquals(left[i], right[i]) && (left[i] == null || !left[i].SameValues(right[i]))) return false;
            return true;
        }

        /// <summary>배치 Artifact 효과다(유물 공격력 계산이 읽는다).</summary>
        internal static bool SameArtifacts(ArtifactLoadoutSnapshot left, ArtifactLoadoutSnapshot right)
        {
            if (left.Revision != right.Revision || left.Effects.Length != right.Effects.Length) return false;
            var a = left.Effects.Span;
            var b = right.Effects.Span;
            for (int i = 0; i < a.Length; i++)
                if (a[i].ArtifactId != b[i].ArtifactId || a[i].InstanceId != b[i].InstanceId || !a[i].Effect.Equals(b[i].Effect)) return false;
            return true;
        }

        internal static bool SameCharacter(CharacterCombatFacts left, CharacterCombatFacts right) =>
            ReferenceEquals(left, right) || left != null && right != null && left.SameValues(right);

        internal static bool SameCharacters(CharacterCombatFacts[] left, CharacterCombatFacts[] right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Length != right.Length) return false;
            for (int i = 0; i < left.Length; i++)
                if (!SameCharacter(left[i], right[i])) return false;
            return true;
        }

        /// <summary>
        /// 다른 개체의 존재만 읽는 판정용 요약이다: 식별자·진영·영역·사망·점유 칸 수.
        /// 유물 공격력의 생존 목록(식별자·진영)과 여러 칸 유닛 보류 판정(영역·칸 수)이 이것만 읽는다.
        /// </summary>
        internal static Presence PresenceOf(CharacterCombatFacts unit) =>
            new Presence(unit.EntityId, unit.Owner, unit.Area, unit.Health.IsDead, unit.OccupiedCells.Count);

        internal readonly struct Presence : IEquatable<Presence>
        {
            private readonly EntityId _id;
            private readonly Owner _owner;
            private readonly CharacterRuntimeArea _area;
            private readonly bool _dead;
            private readonly int _cells;

            internal Presence(EntityId id, Owner owner, CharacterRuntimeArea area, bool dead, int cells)
            {
                _id = id;
                _owner = owner;
                _area = area;
                _dead = dead;
                _cells = cells;
            }

            public bool Equals(Presence other) => _id == other._id && _owner == other._owner && _area == other._area &&
                _dead == other._dead && _cells == other._cells;

            public override bool Equals(object obj) => obj is Presence other && Equals(other);

            public override int GetHashCode() => HashCode.Combine(_id, _owner, _area, _dead, _cells);
        }

        internal static bool SameSouls(IReadOnlyDictionary<CharacterInstanceId, int> left, IReadOnlyDictionary<CharacterInstanceId, int> right,
            CharacterInstanceId? owned)
        {
            if (!owned.HasValue) return true;
            bool hasLeft = left.TryGetValue(owned.Value, out int a);
            bool hasRight = right.TryGetValue(owned.Value, out int b);
            return hasLeft == hasRight && a == b;
        }
    }
}
