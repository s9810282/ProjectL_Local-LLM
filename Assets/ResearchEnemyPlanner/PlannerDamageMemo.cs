using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Rules;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Effects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Traits.Rules;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyPlanner
{
    /// <summary>
    /// 예상 피해 기억(LLMTest/docs/2026-11-18-planner-damage-memo-design.md DM1·DM2·DM5). 계획 하나 동안만 쓰고 버린다.
    /// <para>
    /// 열쇠 = (공격자 대표, 대상 대표 또는 구조물·그 체력, 공통 피해, 역할, 비율, 프로필 목록, 판에서 읽는 몫).
    /// 대표 객체는 같은 아이디의 앞선 사실 객체 중 전투 기록(<see cref="CharacterCombatFacts.StageRecord"/>)만 다른 것이다.
    /// "같은 값"은 Core <see cref="CharacterCombatFacts.SameValues"/>가 정한다(필드 목록을 베끼지 않는다).
    /// 우리가 지키는 사실은 "피해 식은 전투 기록을 읽지 않는다" 하나다(2026-11-18 Core 검색). Core 피해 식에는 늘 진짜 객체를 넘긴다.
    /// </para>
    /// <para>
    /// 판에서 읽는 몫(설계 §3.2): 결속 방어 보너스(Core 값), 도발이면 판 위 공격자 객체, 대상 칸의 먼지 연막 흔적, 참호 보유자 전부, 지형 비율(Core 값).
    /// 받는 피해 규칙이 확인한 셋(도발·참호·먼지 연막) 밖이거나, 공격자·대상이 확인한 특성 키 밖의 키를 가지면 기억하지 않는다(DM2).
    /// </para>
    /// </summary>
    internal sealed class PlannerDamageMemo
    {
        internal delegate int Compute(CombatCommandInput board, CharacterCombatFacts attacker, int damage, PlannerTarget target,
            AttackTargetRole role, int percent);

        /// <summary>측정 전용: 기억을 끈다(기준 결정 기록 만들기). 게임은 바꾸지 않는다.</summary>
        internal static bool Disabled;
        /// <summary>측정 전용(설계 DM4 memocheck): 기억한 답이 있어도 늘 다시 계산해 비교하고, 다시 계산한 값을 쓴다.</summary>
        internal static bool Check;

        /// <summary>설계를 쓸 때(2026-11-18) 있던 특성 키 전부. 이 밖의 키는 판을 새로 읽을 수 있어 기억하지 않는다(DM2).</summary>
        private static readonly HashSet<TraitKey> AuditedKeys = new HashSet<TraitKey>
        {
            TraitKey.None, TraitKey.Focus, TraitKey.MoveAttackDiscount, TraitKey.PiercingFalloff, TraitKey.ThornField,
            TraitKey.Knockback, TraitKey.DistanceDamageScale, TraitKey.KillChainAttack, TraitKey.Necromancy, TraitKey.Roar,
            TraitKey.Regeneration, TraitKey.Intimidation, TraitKey.DoubleShot, TraitKey.LuckyBullet, TraitKey.ChainExplosion,
            TraitKey.HuntersMark, TraitKey.Pull, TraitKey.ImpactBolt, TraitKey.Vanguard, TraitKey.CleanSweep, TraitKey.SoloAction,
            TraitKey.CallOfBlood, TraitKey.MoraleBoost, TraitKey.Disarm, TraitKey.AnkleBreaker, TraitKey.Cohesion, TraitKey.Sacrifice,
            TraitKey.Taunt, TraitKey.Entrenchment, TraitKey.SteadfastWill, TraitKey.GroundCompaction, TraitKey.DustScreen,
            TraitKey.Leap, TraitKey.Acceleration, TraitKey.StationaryFire, TraitKey.ArmorBreaker, TraitKey.FinishingShot,
            TraitKey.TrackingShot, TraitKey.Hunter, TraitKey.FocusedBombardment, TraitKey.PositionSwap, TraitKey.ReconSquad,
            TraitKey.Rampage, TraitKey.LavaCannon, TraitKey.Bleeding, TraitKey.Reconstruction, TraitKey.SelfDestruct, TraitKey.Fear,
            TraitKey.CrashImpact, TraitKey.DeadHand, TraitKey.ThornBomb, TraitKey.DoorBreaching, TraitKey.ContemptForWeak,
            TraitKey.PrecisionShot,
        };

        private readonly Compute _compute;
        private readonly Dictionary<DamageKey, int> _memo = new Dictionary<DamageKey, int>();
        private readonly Dictionary<CharacterCombatFacts, Unit> _units = new Dictionary<CharacterCombatFacts, Unit>(Identity.Instance);
        private readonly Dictionary<EntityId, List<Unit>> _byId = new Dictionary<EntityId, List<Unit>>();

        // 판마다 같은 조각(설계 §11.3): 바로 앞에 물은 판 하나만 기억한다. 한 판의 질문은 이어서 온다.
        private CombatCommandInput _board;
        private CharacterCombatFacts[] _holders;
        private bool _hasDust;

        /// <summary>예상 피해를 물은 수.</summary>
        internal long Calls { get; private set; }
        /// <summary>기억한 답을 쓴 수(확인 모드에서는 다시 계산해 비교한 수).</summary>
        internal long Hits { get; private set; }
        /// <summary>DM2로 막혀 기억하지 않은 수.</summary>
        internal long Blocked { get; private set; }
        /// <summary>대표 찾기에서 Core SameValues로 비교한 수.</summary>
        internal long CanonCompares { get; private set; }
        /// <summary>확인 모드에서 기억한 답과 다시 계산한 답이 다른 수. 0이어야 한다.</summary>
        internal long Mismatches { get; private set; }
        /// <summary>확인 모드의 마지막 어긋남 설명.</summary>
        internal string LastMismatch { get; private set; }

        internal PlannerDamageMemo(Compute compute)
        {
            _compute = compute ?? throw new ArgumentNullException(nameof(compute));
        }

        internal int Estimate(CombatCommandInput board, CharacterCombatFacts attacker, int damage, PlannerTarget target,
            AttackTargetRole role, int percent)
        {
            Calls++;
            if (Disabled || !TryKey(board, attacker, damage, target, role, percent, out var key))
            {
                if (!Disabled) Blocked++;
                return _compute(board, attacker, damage, target, role, percent);
            }
            if (_memo.TryGetValue(key, out var known))
            {
                Hits++;
                if (!Check) return known;
                int fresh = _compute(board, attacker, damage, target, role, percent);
                if (fresh != known)
                {
                    Mismatches++;
                    LastMismatch = $"attacker={attacker.EntityId.Value} target={target.Key} damage={damage} role={role} " +
                        $"percent={percent} memo={known} fresh={fresh}";
                }
                return fresh;
            }
            int value = _compute(board, attacker, damage, target, role, percent);
            _memo.Add(key, value);
            return value;
        }

        // ---------------------------------------------------------------- 열쇠
        private bool TryKey(CombatCommandInput board, CharacterCombatFacts attacker, int damage, PlannerTarget target,
            AttackTargetRole role, int percent, out DamageKey key)
        {
            key = default;
            var a = UnitOf(attacker);
            if (a.UnauditedKey) return false;
            if (!target.IsUnit)
            {
                // 구조물 대상: 받는 피해 규칙·방어·지형 단계를 지나지 않는다(Attack의 victim == null 갈래).
                var health = target.Structure.Health;
                key = new DamageKey(a.Facts, target.Structure, health.CurrentHealth, health.MaxHealth, damage, role, percent,
                    board.Profiles, 0, 100, null);
                return true;
            }

            var victim = target.Unit;
            var v = UnitOf(victim);
            if (v.UnauditedKey) return false;
            v.EnsureRules(board);
            if (v.UnauditedRule) return false;
            SetBoard(board);

            // 결속 방어 보너스: 대상이 결속이 아니면 Core 함수도 0을 돌려준다.
            int cohesion = v.Cohesion ? CombatEffectiveStatsCalculator.CohesionDefenseBonus(board, victim, board.Characters) : 0;
            // 지형: Attack은 대상 위치가 없으면 지형 단계를 건너뛴다. 판에 모래폭풍·고지대 칸이 없으면 Core 함수도 늘 100이다.
            int terrain = victim.Anchor.HasValue && !_terrainFree
                ? EnvironmentTerrainRules.RemainingDamagePercent(board.State.Environment, board.EnvironmentBindings, attacker.Anchor,
                    victim.Anchor.Value)
                : 100;
            // 도발: 규칙이 판 위 공격자 사실을 아이디로 찾는다.
            object taunt = v.Taunt ? (object)board.Characters.SingleOrDefault(c => c.EntityId == attacker.EntityId) ?? NotFound : null;
            // 먼지 연막: 대상 칸의 흔적(Core가 고르는 그대로). 판에 흔적이 하나도 없으면 Core도 빈 목록이다.
            IReadOnlyList<TraitTrailFact> dust = _hasDust ? DustScreenTraitRule.DefenseSourcesAt(victim, board.State.Environment) : null;
            Extra extra = null;
            if (taunt != null || dust != null && dust.Count > 0 || _holders.Length > 0)
                extra = new Extra(taunt, dust, _holders);
            key = new DamageKey(a.Facts, v.Facts, 0, 0, damage, role, percent, board.Profiles, cohesion, terrain, extra);
            return true;
        }

        private void SetBoard(CombatCommandInput board)
        {
            if (ReferenceEquals(board, _board)) return;
            _board = board;
            // 참호: 보유자 전부(살아 있는지·필드인지는 거르지 않음 = 넉넉하게). 대상 자신의 참호 규칙도 판 위 사본을 읽으므로 같이 들어간다.
            var characters = board.Characters;
            int count = 0;
            for (int i = 0; i < characters.Count; i++)
                if (UnitOf(characters[i]).Entrenchment) count++;
            if (count == 0) _holders = Array.Empty<CharacterCombatFacts>();
            else
            {
                _holders = new CharacterCombatFacts[count];
                for (int i = 0, n = 0; i < characters.Count; i++)
                    if (UnitOf(characters[i]).Entrenchment) _holders[n++] = characters[i];
            }
            var environment = board.State.Environment;
            _hasDust = false;
            var tiles = environment.Tiles;
            for (int i = 0; i < tiles.Count; i++)
                if (tiles[i] is TraitTrailFact trail && trail.Key == TraitKey.DustScreen) { _hasDust = true; break; }
            // 지형 비율(Core RemainingDamagePercent)은 대상 칸이 모래폭풍이거나 고지대일 때만 100이 아니다.
            // TerrainCells는 Core가 HasTerrain과 같은 판정이라고 밝힌 함수다. 규칙 자체는 베끼지 않고, 둘 다 없을 때만 부르기를 건너뛴다.
            _terrainFree =
                EnvironmentTerrainRules.TerrainCells(environment, board.EnvironmentBindings, StaticTerrainKind.Sandstorm).Count == 0 &&
                EnvironmentTerrainRules.TerrainCells(environment, board.EnvironmentBindings, StaticTerrainKind.HighGround).Count == 0;
        }

        private bool _terrainFree;

        /// <summary>도발 대상인데 판 위에 공격자가 없음(null과 "도발 아님"을 구분).</summary>
        private static readonly object NotFound = new object();

        // ---------------------------------------------------------------- 대표 객체
        /// <summary>사실 객체의 대표와, 대표마다 한 번만 보는 것들(특성 키 확인, 받는 피해 규칙).</summary>
        private sealed class Unit
        {
            internal readonly CharacterCombatFacts Facts;
            internal readonly bool UnauditedKey;
            internal readonly bool Cohesion;
            internal readonly bool Entrenchment;
            private IReadOnlyList<CombatAttackProfile> _rulesFor;
            internal bool UnauditedRule { get; private set; }
            internal bool Taunt { get; private set; }

            internal Unit(CharacterCombatFacts facts)
            {
                Facts = facts;
                foreach (var status in facts.Stats.TraitStatuses.ToArray())
                {
                    if (!AuditedKeys.Contains(status.Key)) UnauditedKey = true;
                    if (status.Key == TraitKey.Cohesion) Cohesion = true;
                    if (status.Key == TraitKey.Entrenchment) Entrenchment = true;
                }
            }

            /// <summary>받는 피해 규칙(<see cref="CombatCommandEffects.Capture"/>가 고르는 것)을 프로필 목록마다 한 번 본다.</summary>
            internal void EnsureRules(CombatCommandInput board)
            {
                if (ReferenceEquals(_rulesFor, board.Profiles)) return;
                _rulesFor = board.Profiles;
                UnauditedRule = false;
                Taunt = false;
                var profile = CombatCommandCalculator.RequiredProfile(board, Facts);
                foreach (var binding in CombatCommandEffects.Capture(Facts, profile))
                {
                    if (binding.Rule is not IEffectIncomingDamageRule) continue;
                    if (binding.Rule is TauntEffectRule) Taunt = true;
                    else if (!(binding.Rule is EntrenchmentEffectRule || binding.Rule is DustScreenEffectRule)) UnauditedRule = true;
                }
            }
        }

        /// <summary>
        /// 같은 아이디의 앞선 객체 중 전투 기록만 다른 것이 있으면 그 대표를, 없으면 이 객체를 새 대표로 돌려준다.
        /// 위치·체력·행동 사용·영역이 다르면 SameValues가 거짓이므로 비교하지 않는다(비교 수 줄이기, 설계 §11.3).
        /// </summary>
        private Unit UnitOf(CharacterCombatFacts facts)
        {
            if (_units.TryGetValue(facts, out var known)) return known;
            if (!_byId.TryGetValue(facts.EntityId, out var seen)) _byId.Add(facts.EntityId, seen = new List<Unit>());
            foreach (var candidate in seen)
            {
                var c = candidate.Facts;
                if (c.Anchor != facts.Anchor || c.Health.CurrentHealth != facts.Health.CurrentHealth ||
                    c.ActionUse != facts.ActionUse || c.Area != facts.Area) continue;
                CanonCompares++;
                var same = c.StageRecord.SameValues(facts.StageRecord)
                    ? c.SameValues(facts)
                    : c.With(stageRecord: facts.StageRecord).SameValues(facts);
                if (!same) continue;
                _units.Add(facts, candidate);
                return candidate;
            }
            var unit = new Unit(facts);
            seen.Add(unit);
            _units.Add(facts, unit);
            return unit;
        }

        // ---------------------------------------------------------------- 열쇠 형식
        private readonly struct DamageKey : IEquatable<DamageKey>
        {
            private readonly object _attacker, _target, _profiles;
            private readonly Extra _extra;
            private readonly int _health, _maxHealth, _damage, _percent, _cohesion, _terrain;
            private readonly AttackTargetRole _role;

            internal DamageKey(object attacker, object target, int health, int maxHealth, int damage, AttackTargetRole role,
                int percent, object profiles, int cohesion, int terrain, Extra extra)
            {
                _attacker = attacker; _target = target; _profiles = profiles; _extra = extra;
                _health = health; _maxHealth = maxHealth; _damage = damage; _percent = percent;
                _cohesion = cohesion; _terrain = terrain; _role = role;
            }

            public bool Equals(DamageKey other) =>
                ReferenceEquals(_attacker, other._attacker) && ReferenceEquals(_target, other._target) &&
                ReferenceEquals(_profiles, other._profiles) &&
                _health == other._health && _maxHealth == other._maxHealth && _damage == other._damage &&
                _percent == other._percent && _cohesion == other._cohesion && _terrain == other._terrain && _role == other._role &&
                (_extra == null ? other._extra == null : _extra.Equals(other._extra));

            public override bool Equals(object obj) => obj is DamageKey other && Equals(other);

            public override int GetHashCode()
            {
                var hash = new HashCode();
                hash.Add(RuntimeHelpers.GetHashCode(_attacker));
                hash.Add(RuntimeHelpers.GetHashCode(_target));
                hash.Add(RuntimeHelpers.GetHashCode(_profiles));
                hash.Add(_health); hash.Add(_maxHealth); hash.Add(_damage); hash.Add(_percent);
                hash.Add(_cohesion); hash.Add(_terrain); hash.Add((int)_role);
                hash.Add(_extra == null ? 0 : _extra.Hash);
                return hash.ToHashCode();
            }
        }

        /// <summary>판에서 읽는 몫 중 객체 목록인 것(드물다): 도발 공격자, 먼지 연막 흔적, 참호 보유자. 같은 객체인지로 비교한다.</summary>
        private sealed class Extra : IEquatable<Extra>
        {
            private readonly object[] _parts;
            internal readonly int Hash;

            internal Extra(object taunt, IReadOnlyList<TraitTrailFact> dust, CharacterCombatFacts[] holders)
            {
                int dustCount = dust?.Count ?? 0;
                _parts = new object[3 + dustCount + holders.Length];
                int i = 0;
                _parts[i++] = taunt;
                _parts[i++] = dustCount;
                for (int d = 0; d < dustCount; d++) _parts[i++] = dust[d];
                _parts[i++] = holders.Length;
                foreach (var holder in holders) _parts[i++] = holder;
                var hash = new HashCode();
                foreach (var part in _parts) hash.Add(part is int n ? n : part == null ? 0 : RuntimeHelpers.GetHashCode(part));
                Hash = hash.ToHashCode();
            }

            public bool Equals(Extra other)
            {
                if (other == null || other.Hash != Hash || other._parts.Length != _parts.Length) return false;
                for (int i = 0; i < _parts.Length; i++)
                {
                    object x = _parts[i], y = other._parts[i];
                    if (x is int n) { if (!(y is int m) || n != m) return false; }
                    else if (!ReferenceEquals(x, y)) return false;
                }
                return true;
            }

            public override bool Equals(object obj) => Equals(obj as Extra);
            public override int GetHashCode() => Hash;
        }

        private sealed class Identity : IEqualityComparer<CharacterCombatFacts>
        {
            internal static readonly Identity Instance = new Identity();
            public bool Equals(CharacterCombatFacts a, CharacterCombatFacts b) => ReferenceEquals(a, b);
            public int GetHashCode(CharacterCombatFacts o) => RuntimeHelpers.GetHashCode(o);
        }
    }
}
