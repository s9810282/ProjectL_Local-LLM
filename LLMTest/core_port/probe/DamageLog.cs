using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
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
    /// 측정 전용: 예상 피해(<c>PlannerEvaluator.Estimate</c>)가 같은 질문을 몇 번 되풀이하는지와, 같은 질문이면 답이 늘 같은지를 센다.
    /// "같은 질문"을 여러 열쇠로 나눠 본다. 답이 달라지는 것(어긋남)이 있으면 그 열쇠로 기억하면 결정이 바뀐다는 뜻이다.
    /// L6·L7은 기억 설계(LLMTest/docs/2026-11-18-planner-damage-memo-design.md) DM1·DM2의 열쇠다.
    /// </summary>
    internal static class DamageLog
    {
        internal sealed class Level
        {
            internal readonly string Name;
            internal readonly Dictionary<object[], int> First = new Dictionary<object[], int>(KeyComparer.Instance);
            internal long Repeats;
            internal long Mismatches;
            internal Level(string name) { Name = name; }
        }

        internal static long Calls;
        /// <summary>DM2로 막힌 질문: 확인한 목록 밖의 받는 피해 규칙을 대상이 가짐.</summary>
        internal static long BlockedRule;
        /// <summary>DM2로 막힌 질문: 공격자나 대상이 확인한 특성 키 목록 밖의 키를 가짐.</summary>
        internal static long BlockedKey;
        /// <summary>DM1 열쇠에 결속 값·참호 보유자·도발 공격자·먼지 연막이 하나라도 들어간 질문(판에서 읽는 몫이 실제로 있었던 수).</summary>
        internal static long WithBoardPart;
        internal static readonly Level[] Levels =
        {
            new Level("판 객체·공격자 객체·대상 객체 같음"),
            new Level("환경 객체·공격자 객체·대상 객체 같음(판은 달라도)"),
            new Level("공격자 객체·대상 객체 같음(판·환경 무시)"),
            new Level("값 같음: 위치·체력·능력치·상태 묶음 객체 + 환경 객체"),
            new Level("값 같음: 위치·체력·능력치·상태 묶음 객체(판·환경 무시)"),
            new Level("DM1 열쇠(프로필 목록 객체) + DM2 막기"),
            new Level("DM1 열쇠(고른 프로필 객체) + DM2 막기"),
            new Level("L7 + 두 유닛을 '전투 기록(StageRecord)만 다르면 같은 유닛'으로 묶음(Core SameValues)"),
        };

        /// <summary>L8용: 사실 객체 → 대표 객체. 대표는 같은 아이디의 앞선 객체 중 전투 기록만 다른 것(없으면 자기 자신).</summary>
        private static readonly Dictionary<object, CharacterCombatFacts> _canon =
            new Dictionary<object, CharacterCombatFacts>(ReferenceComparer.Instance);
        private static readonly Dictionary<EntityId, List<CharacterCombatFacts>> _canonById = new Dictionary<EntityId, List<CharacterCombatFacts>>();
        internal static long CanonLookups;
        internal static long CanonCompares;

        /// <summary>받는 피해를 바꾸는 규칙 중 판에서 읽는 것을 확인한 종류(설계 §3.2).</summary>
        private static bool IsAuditedIncoming(object rule) =>
            rule is TauntEffectRule || rule is EntrenchmentEffectRule || rule is DustScreenEffectRule;

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

        /// <summary>DM1 열쇠에서 "적용 안 됨"을 나타내는 표지(판 위 객체가 없어서 null인 것과 구분).</summary>
        private static readonly object NotApplicable = new object();

        private static readonly Dictionary<(object unit, CellCoord cell), CharacterCombatFacts> _moved =
            new Dictionary<(object, CellCoord), CharacterCombatFacts>(MoveKeyComparer.Instance);

        internal static long MoveCalls;
        internal static long MoveRepeats;

        /// <summary>
        /// L5(값 같음)로는 되풀이인데 L7(DM1 열쇠)로는 처음인 질문에서, 처음 물은 사실 객체와 지금 사실 객체의 어느 속성이 달랐나.
        /// 값 형식은 값, 참조 형식은 같은 객체인지로 본다. "어디서 같은 값의 새 객체가 생기나"를 찾는 진단이다.
        /// </summary>
        internal static readonly Dictionary<string, long> Diffs = new Dictionary<string, long>();
        internal static long DiffCases;
        private static readonly Dictionary<object[], (object attacker, object target)> _valueFirst =
            new Dictionary<object[], (object, object)>(KeyComparer.Instance);
        private static readonly System.Reflection.PropertyInfo[] FactProperties =
            typeof(CharacterCombatFacts).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                .Where(property => property.GetIndexParameters().Length == 0).ToArray();

        internal static void Reset()
        {
            Calls = 0; BlockedRule = 0; BlockedKey = 0; WithBoardPart = 0; MoveCalls = 0; MoveRepeats = 0; DiffCases = 0;
            _moved.Clear(); Diffs.Clear(); _valueFirst.Clear(); _canon.Clear(); _canonById.Clear(); CanonLookups = 0; CanonCompares = 0;
            foreach (var level in Levels) { level.First.Clear(); level.Repeats = 0; level.Mismatches = 0; }
        }

        /// <summary>
        /// DM3 측정용: <c>QuickMoveScore</c>의 <c>MoveTo</c>를 이것으로 바꾸면(instrument.py --move-memo) 같은 유닛 객체를
        /// 같은 칸으로 옮긴 사본이 같은 객체가 된다. <c>MoveTo</c>는 위치만 바꾼 새 객체를 만드는 순수 함수다.
        /// </summary>
        internal static CharacterCombatFacts MoveTo(CharacterCombatFacts unit, CellCoord cell)
        {
            MoveCalls++;
            if (_moved.TryGetValue((unit, cell), out var moved)) { MoveRepeats++; return moved; }
            moved = CombatMovementCalculator.MoveTo(unit, cell);
            _moved.Add((unit, cell), moved);
            return moved;
        }

        internal static void Record(CombatCommandInput board, CharacterCombatFacts attacker, PlannerTarget target,
            int damage, AttackTargetRole role, int percent, int result)
        {
            Calls++;
            object targetObject = target.IsUnit ? (object)target.Unit : target.Structure;
            var environment = board.State.Environment;
            Note(Levels[0], new object[] { board, attacker, targetObject, damage, role, percent }, result);
            Note(Levels[1], new object[] { environment, attacker, targetObject, damage, role, percent }, result);
            Note(Levels[2], new object[] { attacker, targetObject, damage, role, percent }, result);
            Note(Levels[3], new object[]
            {
                environment, damage, role, percent,
                attacker.EntityId, attacker.Anchor, attacker.Health.CurrentHealth, attacker.Health.MaxHealth, attacker.Stats,
                attacker.StatusEffects, attacker.TraitEffects, attacker.Debuffs, attacker.Items, attacker.TemporaryAttackBonus,
                attacker.PositionModifier,
                target.Key, target.Health,
                target.IsUnit ? (object)target.Unit.Anchor : null, target.IsUnit ? (object)target.Unit.Stats : null,
                target.IsUnit ? (object)target.Unit.StatusEffects : null, target.IsUnit ? (object)target.Unit.TraitEffects : null,
                target.IsUnit ? (object)target.Unit.Debuffs : null, target.IsUnit ? (object)target.Unit.Items : null,
                target.IsUnit ? (object)target.Unit.PositionModifier : null,
                target.IsUnit ? null : (object)target.Structure,
            }, result);
            var valueKey = new object[]
            {
                damage, role, percent,
                attacker.EntityId, attacker.Anchor, attacker.Health.CurrentHealth, attacker.Health.MaxHealth, attacker.Stats,
                attacker.StatusEffects, attacker.TraitEffects, attacker.Debuffs, attacker.Items, attacker.TemporaryAttackBonus,
                attacker.PositionModifier,
                target.Key, target.Health,
                target.IsUnit ? (object)target.Unit.Anchor : null, target.IsUnit ? (object)target.Unit.Stats : null,
                target.IsUnit ? (object)target.Unit.StatusEffects : null, target.IsUnit ? (object)target.Unit.TraitEffects : null,
                target.IsUnit ? (object)target.Unit.Debuffs : null, target.IsUnit ? (object)target.Unit.Items : null,
                target.IsUnit ? (object)target.Unit.PositionModifier : null,
                target.IsUnit ? null : (object)target.Structure,
            };
            bool valueRepeat = Note(Levels[4], valueKey, result);
            if (!valueRepeat) _valueFirst.Add(valueKey, (attacker, targetObject));

            if (!TryMemoKeys(board, attacker, target, damage, role, percent, out var listKey, out var profileKey)) return;
            Note(Levels[5], listKey, result);
            bool memoRepeat = Note(Levels[6], profileKey, result);
            var canonKey = (object[])profileKey.Clone();
            canonKey[0] = Canon(attacker);
            if (target.IsUnit) canonKey[1] = Canon(target.Unit);
            Note(Levels[7], canonKey, result);
            if (valueRepeat && !memoRepeat)
            {
                DiffCases++;
                var (firstAttacker, firstTarget) = _valueFirst[valueKey];
                Diff("공격자", firstAttacker, attacker);
                Diff("대상", firstTarget, targetObject);
            }
        }

        private static void Diff(string side, object first, object now)
        {
            if (ReferenceEquals(first, now)) return;
            if (first is not CharacterCombatFacts a || now is not CharacterCombatFacts b) { Count(side + ".(구조물 객체)"); return; }
            foreach (var property in FactProperties)
            {
                object x = property.GetValue(a), y = property.GetValue(b);
                bool same = x == null || y == null ? x == y : x.GetType().IsValueType ? x.Equals(y) : ReferenceEquals(x, y);
                if (!same) Count(side + "." + property.Name);
            }
        }

        private static void Count(string name) => Diffs[name] = Diffs.TryGetValue(name, out var n) ? n + 1 : 1;

        /// <summary>
        /// 설계 DM1·DM2: <c>CombatDamageCalculator.Attack</c>이 두 유닛 사실 말고 판에서 읽는 몫을 열쇠에 넣는다.
        /// 막히면(DM2) false. listKey는 프로필 목록 객체를, profileKey는 고른 프로필 객체를 넣는다.
        /// </summary>
        private static bool TryMemoKeys(CombatCommandInput board, CharacterCombatFacts attacker, PlannerTarget target,
            int damage, AttackTargetRole role, int percent, out object[] listKey, out object[] profileKey)
        {
            listKey = profileKey = null;
            if (HasUnauditedKey(attacker) || target.IsUnit && HasUnauditedKey(target.Unit)) { BlockedKey++; return false; }
            var attackerProfile = CombatCommandCalculator.RequiredProfile(board, attacker);
            if (!target.IsUnit)
            {
                // 구조물 대상: 받는 피해 규칙·방어·지형 단계를 지나지 않는다(Attack의 victim == null 갈래).
                listKey = new object[] { attacker, target.Structure, target.Structure.Health, damage, role, percent, board.Profiles };
                profileKey = new object[] { attacker, target.Structure, target.Structure.Health, damage, role, percent, attackerProfile };
                return true;
            }

            var victim = target.Unit;
            var victimProfile = CombatCommandCalculator.RequiredProfile(board, victim);
            bool taunt = false;
            foreach (var binding in CombatCommandEffects.Capture(victim, victimProfile))
            {
                if (binding.Rule is not IEffectIncomingDamageRule) continue;
                if (!IsAuditedIncoming(binding.Rule)) { BlockedRule++; return false; }
                if (binding.Rule is TauntEffectRule) taunt = true;
            }

            var common = new List<object> { attacker, victim, damage, role, percent };
            bool boardPart = false;
            // 결속 방어 보너스: Core 함수의 값(대상이 결속이 아니면 0).
            int cohesion = CombatEffectiveStatsCalculator.CohesionDefenseBonus(board, victim, board.Characters);
            common.Add(cohesion);
            boardPart |= cohesion != 0;
            // 도발: 규칙이 판 위 공격자 사실을 아이디로 찾는다.
            if (taunt)
            {
                common.Add(board.Characters.SingleOrDefault(c => c.EntityId == attacker.EntityId));
                boardPart = true;
            }
            else common.Add(NotApplicable);
            // 먼지 연막: 대상 칸의 흔적(Core가 고르는 그대로).
            var dust = DustScreenTraitRule.DefenseSourcesAt(victim, board.State.Environment);
            common.Add(dust.Count);
            foreach (var trail in dust) common.Add(trail);
            boardPart |= dust.Count > 0;
            // 지형: Core 함수의 값(Attack은 대상 위치가 없으면 지형 단계를 건너뛴다).
            common.Add(victim.Anchor.HasValue
                ? EnvironmentTerrainRules.RemainingDamagePercent(board.State.Environment, board.EnvironmentBindings, attacker.Anchor,
                    victim.Anchor.Value)
                : 100);
            // 참호: 보유자 전부(살아 있는지·필드인지는 거르지 않음 = 넉넉하게). 대상 자신의 참호 규칙도 판 위 사본을 읽으므로 같이 들어간다.
            var holders = board.Characters.Where(c => c.Stats.TraitStatuses.ToArray().Any(t => t.Key == TraitKey.Entrenchment)).ToArray();
            common.Add(holders.Length);
            boardPart |= holders.Length > 0;

            var list = new List<object>(common) { board.Profiles };
            foreach (var holder in holders) list.Add(holder);
            var profiles = new List<object>(common) { attackerProfile, victimProfile };
            foreach (var holder in holders) { profiles.Add(holder); profiles.Add(CombatCommandCalculator.RequiredProfile(board, holder)); }
            listKey = list.ToArray();
            profileKey = profiles.ToArray();
            if (boardPart) WithBoardPart++;
            return true;
        }

        /// <summary>
        /// 전투 기록(<see cref="CharacterStageRecord"/>)만 다른 앞선 객체가 있으면 그것을, 없으면 자기를 대표로 돌려준다.
        /// "같은 값"은 Core <see cref="CharacterCombatFacts.SameValues"/>가 정한다(필드를 더하면 Core 시험이 알려 줌).
        /// 전투 기록은 경험치 정산·이동 되돌리기만 읽고 피해 식은 읽지 않는다(2026-11-18 grep).
        /// </summary>
        private static CharacterCombatFacts Canon(CharacterCombatFacts unit)
        {
            CanonLookups++;
            if (_canon.TryGetValue(unit, out var known)) return known;
            if (!_canonById.TryGetValue(unit.EntityId, out var seen)) _canonById.Add(unit.EntityId, seen = new List<CharacterCombatFacts>());
            foreach (var candidate in seen)
            {
                CanonCompares++;
                if (candidate.With(stageRecord: unit.StageRecord).SameValues(unit)) { _canon.Add(unit, candidate); return candidate; }
            }
            seen.Add(unit);
            _canon.Add(unit, unit);
            return unit;
        }

        private sealed class ReferenceComparer : IEqualityComparer<object>
        {
            internal static readonly ReferenceComparer Instance = new ReferenceComparer();
            public new bool Equals(object a, object b) => ReferenceEquals(a, b);
            public int GetHashCode(object o) => RuntimeHelpers.GetHashCode(o);
        }

        private static bool HasUnauditedKey(CharacterCombatFacts unit)
        {
            foreach (var status in unit.Stats.TraitStatuses.ToArray())
                if (!AuditedKeys.Contains(status.Key)) return true;
            return false;
        }

        private static bool Note(Level level, object[] key, int result)
        {
            if (level.First.TryGetValue(key, out var first))
            {
                level.Repeats++;
                if (first != result) level.Mismatches++;
                return true;
            }
            level.First.Add(key, result);
            return false;
        }

        /// <summary>값 형식은 값으로, 참조 형식은 같은 객체인지로 비교한다.</summary>
        private sealed class KeyComparer : IEqualityComparer<object[]>
        {
            internal static readonly KeyComparer Instance = new KeyComparer();

            public bool Equals(object[] a, object[] b)
            {
                if (a.Length != b.Length) return false;
                for (int i = 0; i < a.Length; i++)
                {
                    object x = a[i], y = b[i];
                    if (x == null || y == null) { if (x != y) return false; continue; }
                    if (x.GetType().IsValueType) { if (!x.Equals(y)) return false; }
                    else if (!ReferenceEquals(x, y)) return false;
                }
                return true;
            }

            public int GetHashCode(object[] key)
            {
                var hash = new HashCode();
                foreach (var x in key)
                    hash.Add(x == null ? 0 : x.GetType().IsValueType ? x.GetHashCode() : RuntimeHelpers.GetHashCode(x));
                return hash.ToHashCode();
            }
        }

        /// <summary>유닛은 같은 객체인지, 칸은 값으로 비교한다.</summary>
        private sealed class MoveKeyComparer : IEqualityComparer<(object unit, CellCoord cell)>
        {
            internal static readonly MoveKeyComparer Instance = new MoveKeyComparer();
            public bool Equals((object unit, CellCoord cell) a, (object unit, CellCoord cell) b) =>
                ReferenceEquals(a.unit, b.unit) && a.cell.Equals(b.cell);
            public int GetHashCode((object unit, CellCoord cell) key) =>
                HashCode.Combine(RuntimeHelpers.GetHashCode(key.unit), key.cell);
        }
    }
}
