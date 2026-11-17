using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.EnemyPlanner
{
    /// <summary>칠 수 있는 대상 하나(캐릭터 또는 적 본진).</summary>
    internal readonly struct PlannerTarget
    {
        internal CharacterCombatFacts Unit { get; }
        internal StructureCombatFacts Structure { get; }
        internal bool IsUnit => Unit != null;
        internal long Key => IsUnit ? Unit.EntityId.Value : -Structure.EntityId.Value;
        internal int Health => IsUnit ? Unit.Health.CurrentHealth : Structure.Health.CurrentHealth;
        internal IReadOnlyList<CellCoord> Cells => IsUnit ? Unit.OccupiedCells : Structure.OccupiedCells;

        internal PlannerTarget(CharacterCombatFacts unit) { Unit = unit; Structure = null; }
        internal PlannerTarget(StructureCombatFacts structure) { Unit = null; Structure = structure; }
    }

    /// <summary>
    /// 빠른 판 평가 V(설계 CP6). 식은 연구 시뮬레이터 <c>sim/evaluate.py</c>와 같다:
    /// V = 자산(우리) − 자산(상대) − 겁 × 위험 + 기회 × 기회 − 거리 벌점 + G1.
    /// 한 번의 계획 동안 쓰고, 도달 표는 계획 시작 판(<see cref="Base"/>) 기준으로 (유닛, 칸)마다 기억한다.
    /// </summary>
    internal sealed class PlannerEvaluator
    {
        private readonly Dictionary<(long id, CellCoord cell), Dictionary<CellCoord, int>> _reach =
            new Dictionary<(long, CellCoord), Dictionary<CellCoord, int>>();
        private readonly Dictionary<string, Dictionary<CellCoord, int>> _fields = new Dictionary<string, Dictionary<CellCoord, int>>();
        private readonly Dictionary<EntityKey, double> _poolPower = new Dictionary<EntityKey, double>();
        private readonly CombatEffectEngine _engine = new CombatEffectEngine();
        private readonly TargetDamagePipeline _pipeline = new TargetDamagePipeline(new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault()));

        internal CombatCommandInput Base { get; }
        /// <summary>본진 값(설계 TP-1): 입력의 모든 적 캐릭터(죽은 유닛 포함)의 공격력 × 최대 HP 합.</summary>
        internal double HeadquartersValue { get; }
        /// <summary>빠른 V로 매긴 판 수(진단).</summary>
        internal int Evaluations { get; private set; }

        internal PlannerEvaluator(CombatCommandInput root)
        {
            Base = root ?? throw new ArgumentNullException(nameof(root));
            HeadquartersValue = root.Characters.Where(c => c.Owner == Owner.Enemy)
                .Sum(c => (double)c.Stats.AttackPower * c.Health.MaxHealth);
            foreach (var profile in root.Profiles)
                if (!_poolPower.ContainsKey(profile.Definition.Key))
                    _poolPower.Add(profile.Definition.Key, (double)profile.Definition.AttackPower * profile.Definition.MaxHealth);
        }

        // ---------------------------------------------------------------- 사실
        internal static Owner Other(Owner side) => side == Owner.Enemy ? Owner.Player : Owner.Enemy;

        internal static bool OnField(CharacterCombatFacts c) =>
            c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead && c.Anchor.HasValue;

        internal static IEnumerable<CharacterCombatFacts> Living(CombatCommandInput board, Owner side) =>
            board.Characters.Where(c => c.Owner == side && OnField(c)).OrderBy(c => c.EntityId.Value);

        internal static bool IsMain(CharacterCombatFacts c) => c.OwnershipRole == CharacterOwnershipRole.Main;

        internal static IEnumerable<StructureCombatFacts> Headquarters(CombatCommandInput board) =>
            board.State.Structures.Where(s => s.Owner == Owner.Enemy && s.Definition.Key == EntityKey.Structure_Headquarters && !s.IsDestroyed);

        internal static double Power(CharacterCombatFacts c)
        {
            double power = (double)c.Stats.EffectiveAttackPower * c.Health.CurrentHealth;
            return IsMain(c) ? power * EnemyTurnPlannerConfig.MainWeight : power;
        }

        internal double Assets(CombatCommandInput board, Owner side)
        {
            double total = Living(board, side).Sum(Power);
            if (side != Owner.Enemy) return total;
            bool alive = false;
            foreach (var hq in Headquarters(board))
            {
                alive = true;
                total += HeadquartersValue * hq.Health.CurrentHealth / Math.Max(1, hq.Health.MaxHealth);
            }
            var spec = board.Battle?.NexusReinforcements;
            var pool = board.State.Reinforcements;
            if (alive && spec != null && pool.IsEnabled)
                for (int index = 0; index < spec.Pool.Count; index++)
                    if (_poolPower.TryGetValue(spec.Pool[index].UnitKey, out var power))
                        total += EnemyTurnPlannerConfig.ReinforcementDiscount * power * pool.RemainingCount(spec, index);
            return total;
        }

        /// <summary>side가 칠 수 있는 상대: 상대 유닛 + (플레이어라면) 적 본진.</summary>
        internal static List<PlannerTarget> Targets(CombatCommandInput board, Owner side)
        {
            var targets = Living(board, Other(side)).Select(c => new PlannerTarget(c)).ToList();
            if (side == Owner.Player) targets.AddRange(Headquarters(board).Select(s => new PlannerTarget(s)));
            return targets;
        }

        /// <summary>그 피해로 잃는 자산.</summary>
        internal double Loss(PlannerTarget target, int damage)
        {
            if (target.IsUnit)
            {
                double lost = (double)Math.Min(damage, target.Unit.Health.CurrentHealth) * target.Unit.Stats.EffectiveAttackPower;
                return IsMain(target.Unit) ? lost * EnemyTurnPlannerConfig.MainWeight : lost;
            }
            var health = target.Structure.Health;
            return HeadquartersValue * Math.Min(damage, health.CurrentHealth) / Math.Max(1, health.MaxHealth);
        }

        // ---------------------------------------------------------------- 도달 표·피해 예상
        /// <summary>
        /// 그 유닛이 cell에 있으면 다음 턴에 (이동해서) 공격이 닿는 칸 → 적용 비율(지정 칸 100, 스플래시 칸 스플래시 %).
        /// 칸 규칙은 게임의 공격 범위 표시와 같은 <see cref="CombatAttackQueries.RangeCells"/>이고, 스플래시는 그 칸 둘레 모양이다.
        /// </summary>
        internal Dictionary<CellCoord, int> Reach(CharacterCombatFacts unit, CellCoord cell)
        {
            var key = (unit.EntityId.Value, cell);
            if (_reach.TryGetValue(key, out var cached)) return cached;
            var result = new Dictionary<CellCoord, int>();
            _reach.Add(key, result);
            var moved = CombatMovementCalculator.MoveTo(unit, cell);
            var characters = Base.Characters.Select(c => c.EntityId == unit.EntityId ? moved : c).ToArray();
            if (characters.All(c => c.EntityId != unit.EntityId)) return result;     // 계획 중에 생긴 유닛
            var probe = Base.WithWorld(Base.State, characters, Base.Main, Base.SoulCounts, Base.Souls, Base.EnvironmentBindings);
            var environment = Base.State.Environment.VisibleTo(unit.Owner);
            var profile = CombatCommandCalculator.RequiredProfile(Base, unit);
            var policy = _engine.EvaluateAttackTargeting(CombatCommandEffects.Capture(unit, profile)).Policy;
            var spots = new List<CellCoord> { cell };
            spots.AddRange(CombatMovementCalculator.UnitMoveCells(probe, moved, environment));
            var direct = new HashSet<CellCoord>();
            foreach (var spot in spots)
                foreach (var target in CombatAttackQueries.RangeCells(probe, CombatMovementCalculator.MoveTo(moved, spot), profile, policy, environment))
                    direct.Add(target);
            foreach (var target in direct) result[target] = 100;
            int splash = profile.Method.SplashPercent;
            if (splash > 0)
            {
                var offsets = AppointShapeCalculator.CreateSplashOffsets(profile.Method.Splash);
                foreach (var target in direct)
                    foreach (var offset in offsets)
                    {
                        var around = new CellCoord(target.X + offset.X, target.Y + offset.Y);
                        if (!Base.State.IsInside(around) || direct.Contains(around)) continue;
                        if (!result.TryGetValue(around, out var percent) || percent < splash) result[around] = splash;
                    }
            }
            return result;
        }

        /// <summary>다음 턴 공격 한 번의 예상 피해(엔진 피해 식: 상성·방어·지형). 지형 판정 출발 칸은 공격자의 지금 칸.</summary>
        internal int Estimate(CombatCommandInput board, CharacterCombatFacts attacker, PlannerTarget target, int percent) =>
            Estimate(board, attacker, BaseDamage(board, attacker), target, percent);

        /// <summary>공격자의 공통 피해(프로필 + 공통 피해 식). 같은 판·같은 공격자면 대상과 상관없이 같다.</summary>
        internal static int BaseDamage(CombatCommandInput board, CharacterCombatFacts attacker) =>
            CombatAttackCalculator.CommonAttackDamage(board, attacker, CombatCommandCalculator.RequiredProfile(board, attacker),
                CombatDamageKind.OrdinaryAttack);

        /// <summary>
        /// 공통 피해(<see cref="BaseDamage"/>)를 미리 구해 둔 예상 피해. 한 공격자가 여러 대상을 볼 때 공통 피해를 한 번만 계산한다
        /// (할당 줄이기 AL-1, 결정 같음).
        /// </summary>
        internal int Estimate(CombatCommandInput board, CharacterCombatFacts attacker, int damage, PlannerTarget target, int percent)
        {
            var role = percent >= 100 ? AttackTargetRole.Primary : AttackTargetRole.Additional;
            var result = target.IsUnit
                ? CombatDamageCalculator.Attack(board, _engine, _pipeline, attacker, target.Unit, damage, role, percent,
                    attacker.Anchor, null, board.State.Environment)
                : CombatDamageCalculator.Attack(board, _engine, _pipeline, attacker, null, damage, role, percent,
                    attacker.Anchor, null, board.State.Environment, targetHealth: target.Structure.Health);
            return Math.Max(0, result.FinalDamage);
        }

        /// <summary>
        /// 다음 턴에 기본 공격을 할 수 있나. 우리 유닛(own)은 이번 턴 끝에 무장 해제가 풀린다고 보고 제약을 보지 않는다
        /// (시뮬레이터 <c>_best_strike</c>의 own 규칙). 상대 유닛은 지금 제약을 본다.
        /// </summary>
        internal static bool CanStrike(CombatCommandInput board, CharacterCombatFacts unit, bool own) =>
            UnitBehaviorRules.CanBasicAttack(board, unit) && (own || !CombatActionAdmission.IsAttackRestricted(unit));

        /// <summary>그 유닛이 다음 턴에 고를 가장 좋은 공격 하나: (대상, 피해, 잃게 하는 자산). 없으면 대상 없음.</summary>
        internal (PlannerTarget? target, int damage, double value) BestStrike(CombatCommandInput board, CharacterCombatFacts attacker,
            IReadOnlyList<PlannerTarget> targets, bool own)
        {
            (PlannerTarget? target, int damage, double value) best = (null, 0, 0.0);
            if (!CanStrike(board, attacker, own)) return best;
            var reach = Reach(attacker, attacker.Anchor.Value);
            int? baseDamage = null;
            foreach (var target in targets)
            {
                int percent = 0;
                foreach (var c in target.Cells)
                    if (reach.TryGetValue(c, out var value) && value > percent) percent = value;
                if (percent <= 0) continue;
                baseDamage ??= BaseDamage(board, attacker);
                int damage = Estimate(board, attacker, baseDamage.Value, target, percent);
                double loss = Loss(target, damage);
                if (loss > best.value) best = (target, damage, loss);
            }
            return best;
        }

        // ---------------------------------------------------------------- 위험·기회·거리
        /// <summary>상대 유닛마다 우리 자산을 가장 많이 깎는 공격 하나를 고른다고 보고, 대상마다 피해를 더해 HP에서 자른다.</summary>
        internal double Risk(CombatCommandInput board, Owner side)
        {
            var incoming = new Dictionary<long, (PlannerTarget target, int damage)>();
            var mine = Targets(board, Other(side));
            foreach (var attacker in Living(board, Other(side)))
            {
                var (target, damage, _) = BestStrike(board, attacker, mine, own: false);
                if (!target.HasValue) continue;
                long key = target.Value.Key;
                incoming[key] = incoming.TryGetValue(key, out var sum) ? (sum.target, sum.damage + damage) : (target.Value, damage);
            }
            return incoming.Values.Sum(pair => Loss(pair.target, pair.damage));
        }

        /// <summary>(기회, 거리 벌점). 우리 유닛마다 다음 턴의 가장 좋은 공격 하나. 닿는 대상이 없으면 거리 벌점.</summary>
        internal (double gain, double penalty) Opportunity(CombatCommandInput board, Owner side)
        {
            var targets = Targets(board, side);
            if (targets.Count == 0) return (0.0, 0.0);
            double gain = 0.0, penalty = 0.0;
            Dictionary<CellCoord, int> field = null;
            foreach (var unit in Living(board, side))
            {
                var (target, _, value) = BestStrike(board, unit, targets, own: true);
                if (target.HasValue)
                {
                    gain += value;
                    continue;
                }
                field ??= DistanceField(board, targets);
                penalty += EnemyTurnPlannerConfig.StepPenalty * Beyond(unit, field);
            }
            return (gain, penalty);
        }

        /// <summary>대상 옆 칸까지 걸음 중 이동·사거리로 못 메우는 만큼(시뮬레이터 거리 벌점과 같은 식). 길이 없으면 0.</summary>
        internal static int Beyond(CharacterCombatFacts unit, IReadOnlyDictionary<CellCoord, int> field)
        {
            if (!field.TryGetValue(unit.Anchor.Value, out var steps)) return 0;
            return Math.Max(0, steps - unit.Stats.MoveRange - Math.Max(0, unit.Stats.AppointRange - 1));
        }

        /// <summary>대상 옆 칸까지 걷는 거리(유닛은 지나간다고 봄, 벽·낭떠러지·구조물은 못 지남).</summary>
        internal Dictionary<CellCoord, int> DistanceField(CombatCommandInput board, IReadOnlyList<PlannerTarget> targets)
        {
            var sources = new SortedSet<(int x, int y)>();
            foreach (var target in targets)
                foreach (var c in target.Cells)
                {
                    sources.Add((c.X + 1, c.Y));
                    sources.Add((c.X - 1, c.Y));
                    sources.Add((c.X, c.Y + 1));
                    sources.Add((c.X, c.Y - 1));
                }
            var structures = new HashSet<CellCoord>(board.State.Structures.Where(s => !s.IsDestroyed).SelectMany(s => s.OccupiedCells));
            string key = string.Join(";", sources.Select(s => s.x + "," + s.y)) + "#" + structures.Count;
            if (_fields.TryGetValue(key, out var cached)) return cached;
            var environment = board.State.Environment;
            bool Open(CellCoord cell) => board.State.IsInside(cell) && !structures.Contains(cell) &&
                !EnvironmentTerrainRules.BlocksVoluntaryEntry(environment, board.EnvironmentBindings, cell);
            var distances = new Dictionary<CellCoord, int>();
            var queue = new Queue<CellCoord>();
            foreach (var (x, y) in sources)
            {
                var cell = new CellCoord(x, y);
                if (!Open(cell)) continue;
                distances[cell] = 0;
                queue.Enqueue(cell);
            }
            while (queue.Count > 0)
            {
                var cell = queue.Dequeue();
                foreach (var (dx, dy) in Steps)
                {
                    var next = new CellCoord(cell.X + dx, cell.Y + dy);
                    if (distances.ContainsKey(next) || !Open(next)) continue;
                    distances[next] = distances[cell] + 1;
                    queue.Enqueue(next);
                }
            }
            _fields.Add(key, distances);
            return distances;
        }

        private static readonly (int dx, int dy)[] Steps = { (0, 1), (1, 0), (0, -1), (-1, 0) };

        // ---------------------------------------------------------------- 승리 대상 노림 G1
        /// <summary>적이 잡으면 이기는 것: 플레이어 메인(필드에 산 것).</summary>
        internal static List<PlannerTarget> WinTargets(CombatCommandInput board) =>
            Living(board, Owner.Player).Where(IsMain).Select(c => new PlannerTarget(c)).ToList();

        /// <summary>승리 대상 가치. 최대 HP 기준으로 고정한다(다쳐도 끌림이 줄지 않게).</summary>
        internal static double WinValue(PlannerTarget target) =>
            (double)target.Unit.Stats.EffectiveAttackPower * target.Unit.Health.MaxHealth * EnemyTurnPlannerConfig.MainWeight;

        /// <summary>그 유닛이 cell에 있을 때 승리 대상까지 모자란 걸음. 다음 턴에 칠 수 있으면 0. 공격할 수 없는 종류·길 없는 칸도 0.</summary>
        internal int GoalSteps(CombatCommandInput board, CharacterCombatFacts unit, CellCoord cell, IReadOnlyList<PlannerTarget> targets)
        {
            if (targets.Count == 0 || !UnitBehaviorRules.CanBasicAttack(board, unit)) return 0;
            var reach = Reach(unit, cell);
            foreach (var target in targets)
                if (target.Cells.Any(c => reach.TryGetValue(c, out var percent) && percent > 0)) return 0;
            var field = DistanceField(board, targets);
            if (!field.TryGetValue(cell, out var steps)) return 0;
            return Math.Max(0, steps - unit.Stats.MoveRange - Math.Max(0, unit.Stats.AppointRange - 1));
        }

        /// <summary>G1의 유닛 하나 몫(빼는 점수, 0 이상). 판 평가와 빠른 이동 점수가 함께 부른다.</summary>
        internal double GoalPenalty(CombatCommandInput board, CharacterCombatFacts unit, CellCoord cell, IReadOnlyList<PlannerTarget> targets)
        {
            if (EnemyTurnPlannerConfig.GoalStep <= 0 || targets.Count == 0) return 0.0;
            int steps = GoalSteps(board, unit, cell, targets);
            if (steps == 0) return 0.0;
            double reference = targets.Max(WinValue);
            return EnemyTurnPlannerConfig.GoalStep * reference / 100 * steps;
        }

        internal double GoalTerm(CombatCommandInput board)
        {
            var targets = WinTargets(board);
            if (targets.Count == 0) return 0.0;
            return -Living(board, Owner.Enemy).Sum(unit => GoalPenalty(board, unit, unit.Anchor.Value, targets));
        }

        // ---------------------------------------------------------------- 합
        /// <summary>끝난 판의 값(적 입장). 플레이어 패배 = 적 승리.</summary>
        internal static double Terminal(StageOutcome outcome) =>
            outcome == StageOutcome.Defeat ? EnemyTurnPlannerConfig.Win : -EnemyTurnPlannerConfig.Win;

        /// <summary>적 입장의 빠른 V.</summary>
        internal double Value(PlannerState state)
        {
            Evaluations++;
            if (state.IsEnded) return Terminal(state.Outcome);
            var board = state.Input;
            double total = Assets(board, Owner.Enemy) - Assets(board, Owner.Player);
            total -= EnemyTurnPlannerConfig.Fear * Risk(board, Owner.Enemy);
            var (gain, penalty) = Opportunity(board, Owner.Enemy);
            total += EnemyTurnPlannerConfig.Opportunity * gain - penalty;
            return total + GoalTerm(board);
        }

        /// <summary>
        /// 이동만 하는 후보를 엔진 없이 빠르게: 그 칸에서의 다음 턴 기회 − 겁 × 그 칸에서 맞을 피해 − 그 칸의 G1 몫.
        /// 시뮬레이터 <c>_quick_move_score</c>와 같은 식.
        /// </summary>
        internal double QuickMoveScore(CombatCommandInput board, CharacterCombatFacts unit, CellCoord destination)
        {
            var moved = CombatMovementCalculator.MoveTo(unit, destination);
            var targets = Targets(board, Owner.Enemy);
            double gain = BestStrike(board, moved, targets, own: true).value;
            int danger = 0;
            var self = new PlannerTarget(moved);
            foreach (var attacker in Living(board, Owner.Player))
            {
                if (!CanStrike(board, attacker, own: false)) continue;
                if (!Reach(attacker, attacker.Anchor.Value).TryGetValue(destination, out var percent) || percent <= 0) continue;
                danger += Estimate(board, attacker, self, percent);
            }
            double loss = (double)Math.Min(danger, unit.Health.CurrentHealth) * unit.Stats.EffectiveAttackPower *
                (IsMain(unit) ? EnemyTurnPlannerConfig.MainWeight : 1.0);
            if (gain == 0)
            {
                var field = DistanceField(board, targets);
                gain = -EnemyTurnPlannerConfig.StepPenalty * (field.TryGetValue(destination, out var steps) ? steps : 99);
            }
            double score = EnemyTurnPlannerConfig.Opportunity * gain - EnemyTurnPlannerConfig.Fear * loss;
            return score - GoalPenalty(board, moved, destination, WinTargets(board));
        }
    }
}
