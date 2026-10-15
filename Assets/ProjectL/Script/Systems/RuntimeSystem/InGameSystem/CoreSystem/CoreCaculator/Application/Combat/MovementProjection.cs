using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>한 명령 안에서만 존재하는 투영값이다. Buffer 원본과 매 단계의 현재값을 구분한다.
    /// 캐릭터·구조물·가시밭 현재값과 경로 장부(진입 칸·비용·정지·귀환 장벽)를 소유하고,
    /// 환경 원본은 <see cref="CommandEnvironmentState"/>, 사망·영혼 정산은 <see cref="CommandDeathSettlement"/>,
    /// 공개 의존 기록은 <see cref="ProjectionDependencies"/>에 맡긴다.</summary>
    internal sealed class MovementProjection
    {
        private readonly Dictionary<EntityId, CharacterCombatFacts> _characters;
        private readonly List<CellCoord> _entered = new();
        private readonly Dictionary<CellCoord, ThornFieldCellSnapshot> _thorns;
        private readonly Dictionary<EntityId, StructureCombatFacts> _structures;
        private readonly CommandEnvironmentState _environment;
        private readonly CommandDeathSettlement _deaths;
        private readonly ProjectionDependencies _dependencies = new();
        private readonly ObstacleReactionCalculator _obstacles;
        private readonly CommandAttackGroups _attackGroups;
        private readonly CombatCommandState _state;
        private CellCoord _finalStop;
        internal EnvironmentSemanticBindings Bindings => _environment.Bindings;
        internal long NextAttackGroup() => _attackGroups.Next();
        internal HashSet<EntityId> EffectTargetIds => _dependencies.EffectTargetIds;
        internal HashSet<CellCoord> TerrainQueryCells => _dependencies.TerrainQueryCells;
        internal Dictionary<EntityId, CellCoord> ArrivalCells => _dependencies.ArrivalCells;
        internal CombatCommandInput Input { get; }
        internal CharacterCombatFacts Actor => _characters[Input.ActorId];
        internal List<CombatEvent> Events { get; } = new();
        internal int ActionCostSpent { get; private set; }
        internal int DistanceSpent => _entered.Count;
        internal CellCoord FinalStop => Actor.Anchor ?? _finalStop;
        internal MovementStopReason StopReason { get; private set; }
        internal bool ReturnBarrierOccurred { get; private set; }
        internal bool ArrivalResolved { get; private set; }
        internal bool Terminal => BattleOutcomeCalculator.Resolve(_characters.Values.ToArray(), _structures.Values.ToArray()).IsStageEnded;
        internal MovementEnvironmentFacts Environment => _environment.Facts;

        internal MovementProjection(CombatCommandInput input, bool preview = false)
        {
            Input = input;
            _state = input.State;
            _characters = input.Characters.ToDictionary(c => c.EntityId);
            _environment = new CommandEnvironmentState(preview ? input.State.Environment.VisibleTo(Actor.Owner) : input.State.Environment,
                input.EnvironmentBindings, _state.EnvironmentProgress);
            _thorns = input.State.ThornFields.ToDictionary(f => f.Cell);
            _structures = input.State.Structures.ToDictionary(s => s.EntityId);
            if (input.ObstacleContents.Count > 0 || input.State.Structures.Any(s => ObstacleContentProfile.IsNativeObstacleKey(s.Definition.Key)))
                ObstacleContentProfile.Validate(input.ObstacleContents, input.EnvironmentBindings, input.State.Structures);
            _attackGroups = new CommandAttackGroups();
            _deaths = new CommandDeathSettlement(input.SoulCounts.ToDictionary(p => p.Key, p => p.Value), input.Souls,
                new List<long>(), new Dictionary<EntityId, CellCoord>(), new SoulEffectCalculator(input.Artifacts), null);
            _obstacles = new ObstacleReactionCalculator(input, Environment, _characters, _structures, Events, _deaths.DeathAnchors, _attackGroups);
            _finalStop = input.Path.Origin;
        }

        // 공격·배치의 현재 투영을 이어받아 동일한 진입 계산과 최종 게시 경계를 사용한다.
        internal MovementProjection(CombatCommandInput input, CombatCommandState state,
            Dictionary<EntityId, CharacterCombatFacts> characters, Dictionary<EntityId, StructureCombatFacts> structures,
            Dictionary<CellCoord, ThornFieldCellSnapshot> thorns, Dictionary<CharacterInstanceId, int> counts,
            SoulFieldSnapshot soulField, List<long> consumedSouls, List<CombatEvent> events,
            Dictionary<EntityId, CellCoord> deathAnchors, SoulEffectCalculator souls, CommandAttackGroups attackGroups = null,
            MovementEnvironmentFacts environment = null)
        {
            Input = input;
            _state = state;
            _characters = characters;
            _structures = structures;
            _thorns = thorns;
            Events = events;
            _attackGroups = attackGroups ?? new CommandAttackGroups();
            _environment = new CommandEnvironmentState(environment ?? state.Environment, input.EnvironmentBindings, state.EnvironmentProgress);
            _deaths = new CommandDeathSettlement(counts, soulField, consumedSouls, deathAnchors, souls,
                characters.Values.Where(c => c.Health.IsDead).Select(c => c.EntityId));
            _obstacles = new ObstacleReactionCalculator(input, Environment, _characters, _structures, Events, _deaths.DeathAnchors, _attackGroups);
        }

        internal static MovementProjection ForArrival(CombatCommandInput input,
            Dictionary<EntityId, CharacterCombatFacts> characters, List<CombatEvent> events) =>
            new(input, input.State, characters, input.State.Structures.ToDictionary(s => s.EntityId),
                input.State.ThornFields.ToDictionary(f => f.Cell), input.SoulCounts.ToDictionary(p => p.Key, p => p.Value),
                input.Souls, new List<long>(), events, new Dictionary<EntityId, CellCoord>(), new SoulEffectCalculator(input.Artifacts));

        internal void Enter(CellCoord cell, int stepCost)
        {
            var before = Actor;
            var paid = CombatMovementCalculator.RecordEnteredStep(before, stepCost);
            var after = CombatMovementCalculator.MoveTo(paid, cell);
            ReplaceActor(after);
            _entered.Add(cell);
            _finalStop = cell;
            ActionCostSpent = checked(ActionCostSpent + stepCost);
            Events.Add(new CombatCharacterEvent(CombatEventKind.Moved, before, after));
        }

        internal void ReplaceActor(CharacterCombatFacts actor)
        {
            if (actor.EntityId != Input.ActorId) throw new ArgumentException("이동자 식별자를 바꿀 수 없습니다.");
            _characters[Input.ActorId] = actor;
        }

        internal void Stop(MovementStopReason reason)
        {
            if (StopReason == MovementStopReason.Completed || reason == MovementStopReason.Dead ||
                reason == MovementStopReason.BattleEnded) StopReason = reason;
        }
        internal CharacterCombatFacts Character(EntityId id) => _characters[id];
        internal MovementTileFact Source(long id) => _environment.Source(id);
        internal bool HasThorn(CellCoord cell) => _thorns.ContainsKey(cell);
        internal ThornFieldCellSnapshot Thorn(CellCoord cell) => _thorns[cell];

        internal CombatCommandInput CurrentInput()
        {
            var s = _state;
            bool actorDead = _characters.TryGetValue(Input.ActorId, out var actor) && actor.Health.IsDead;
            var state = new CombatCommandState(s.BoardWidth, s.BoardHeight,
                actorDead && s.Phase == CombatFlowPhase.PlayerInput ? CombatFlowPhase.Automatic : s.Phase,
                actorDead ? EntityId.Empty : s.ActiveActorId, s.IsNight, _structures.Values.ToArray(),
                _thorns.Values.ToArray(), s.Timeline, s.Turn, Environment, _environment.Progress, s.WallBlocking);
            var main = _characters.Values.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main);
            return new CombatCommandInput(Input.Number, Input.Kind, Input.ActorId, Input.Aim, state,
                _characters.Values.ToArray(), Input.Profiles, CharacterSettlementCalculator.WithHealth(Input.Main, main.Health.CurrentHealth),
                Input.Owned, _deaths.Counts, _deaths.SoulField, Input.Artifacts, Input.TargetCell, Input.Rotation,
                Input.Battle, Input.ProcessingKind, Input.Path, Input.MovementContext, Input.BattleInstanceId, Input.ArtifactItems,
                Bindings, Input.ObstacleContents);
        }

        internal MovementStepInput StepInput(CellCoord cell, MovementEffectPhase phase, IReadOnlyList<EntityId> targetIds = null,
            EntityId? mover = null)
        {
            var arriving = mover.HasValue ? Character(mover.Value) : Actor;
            targetIds ??= new[] { arriving.EntityId };
            foreach (var id in targetIds) EffectTargetIds.Add(id);
            return new(cell, arriving, phase,
            _characters.Values.Where(c => c.Area == CharacterRuntimeArea.Field).Select(c =>
                new MovementHealthTarget(c.EntityId, c.Owner, c.Health, c.OccupiedCells))
                .Concat(_structures.Values.Select(s => new MovementHealthTarget(s.EntityId, s.Owner, s.Health, s.OccupiedCells)))
                .Where(t => targetIds.Contains(t.EntityId))
                .OrderBy(t => t.EntityId).ToArray(),
            _environment.ArrivalRuleSourcesAt(cell));
        }

        internal void MergeCharacter(CharacterStateChange change)
        {
            if (!_characters.TryGetValue(change.Expected.EntityId, out var current) || !ReferenceEquals(current, change.Expected))
                throw new InvalidOperationException("효과 계산의 기대값은 현재 투영 사실이어야 합니다.");
            _characters[current.EntityId] = change.Final;
        }

        /// <summary>규칙이 제안한 보유 효과 변경을 현재 투영 기대값과 비교한 뒤 적용한다. 콘텐츠 종류는 판단하지 않는다.</summary>
        internal void ChangeHeld(MovementHeldChange change)
        {
            var before = Character(change.TargetId);
            if (!before.HeldEnvironment.SameValues(change.Expected))
                throw new InvalidOperationException("보유 효과 변경의 기대값이 현재 투영과 다릅니다.");
            if (change.Expected.SameValues(change.Final)) return;
            var after = HeldEnvironmentEffects.WithHeldState(before, change.Final);
            MergeCharacter(new CharacterStateChange(before, after));
            Events.Add(new CombatHeldEnvironmentEvent(before, after));
            ReturnBarrierOccurred = true;
        }

        internal int ChooseRandom(int count) => _environment.ChooseRandom(count);

        internal void ChangeTornado(TornadoSourceFact expected, TornadoSourceFact final) => _environment.ChangeTornado(expected, final);

        internal void ReconcilePresence(IReadOnlyList<CellCoord> cells)
        {
            var current = CurrentInput();
            foreach (var change in EnvironmentPresenceReconciler.Calculate(current.Characters, Environment, cells, current.Main).Characters)
            {
                PositionTileModifierCalculator.RecordNormalization(Events, change.Expected, change.Final);
                MergeCharacter(change);
            }
        }

        internal void ChangeSource(MovementTileFact selected, MovementStepEffects effects)
        {
            if (!_environment.ApplySelectedChange(selected, effects)) return;
            ReturnBarrierOccurred = true;
            Events.Add(new CombatMovementSourceEvent(effects.ExpectedSource, effects.FinalSource));
        }

        internal void ConsumeThorn(CellCoord cell)
        {
            var source = _thorns[cell];
            CombatCommandCalculator.ConsumeFields(_thorns, new[] { cell });
            ReturnBarrierOccurred = true;
            Events.Add(new CombatThornConsumedEvent(source));
        }

        internal void ChangeHealth(MovementHealthRequest request, bool heal, CombatDamageOrigin origin = null)
        {
            if (heal) origin = null;
            else if (request.DamageOrigin != null)
                origin = new CombatDamageOrigin(request.DamageOrigin.PhysicalCell, request.DamageOrigin.CreditedCharacterId,
                    request.DamageOrigin.AttackGroup > 0 ? request.DamageOrigin.AttackGroup : origin.AttackGroup);
            var origins = origin == null ? null : new[] { origin };
            if (_characters.TryGetValue(request.TargetId, out var before))
            {
                if (before.Health.IsDead || before.Area != CharacterRuntimeArea.Field) return;
                if (!heal)
                {
                    TerrainQueryCells.Add(before.Anchor.Value);
                    if (origin?.PhysicalCell is CellCoord source) TerrainQueryCells.Add(source);
                }
                int amount = heal ? request.Amount : CombatDamageCalculator.Environment(CurrentInput(),
                    request.Amount, origin, before.Anchor).FinalDamage;
                if (!heal)
                {
                    amount = CombatDamageCalculator.ApplyHeldDefense(_attackGroups, _characters, before.EntityId, amount, origin, Events);
                    if (!ReferenceEquals(before, _characters[before.EntityId])) ReturnBarrierOccurred = true;
                    before = _characters[before.EntityId];
                }
                var after = heal ? CharacterSettlementCalculator.SetHealth(before, CurrentInput().Main,
                    (int)Math.Min(before.Health.MaxHealth, (long)before.Health.CurrentHealth + amount))
                    .Characters.Single().Final : CombatCommandCalculator.Hurt(before, amount);
                if (before.Health.CurrentHealth == after.Health.CurrentHealth) return;
                if (after.Health.IsDead) _deaths.DeathAnchors[before.EntityId] = before.Anchor.Value;
                MergeCharacter(new CharacterStateChange(before, after));
                CombatCommandEvents.Health(Events, before, after, origins);
            }
            else if (_structures.TryGetValue(request.TargetId, out var structure))
            {
                if (structure.Health.IsDead) return;
                if (!heal && _obstacles.HasProfile(request.TargetId))
                {
                    _obstacles.DamageStructure(request.TargetId, request.Amount, origin);
                    _obstacles.ResolveExplosions();
                    EffectTargetIds.UnionWith(_obstacles.BlastTargetIds);
                    TerrainQueryCells.UnionWith(_obstacles.TerrainQueryCells);
                    ReturnBarrierOccurred |= request.Amount > 0;
                    return;
                }
                int amount = heal ? request.Amount : CombatDamageCalculator.Environment(request.Amount, origin).FinalDamage;
                int hp = heal ? (int)Math.Min(structure.Health.MaxHealth, (long)structure.Health.CurrentHealth + amount) :
                    Math.Max(0, structure.Health.CurrentHealth - Math.Min(structure.Health.CurrentHealth, amount));
                if (hp == structure.Health.CurrentHealth) return;
                var after = new StructureCombatFacts(structure.EntityId, structure.Definition,
                    new HealthStateSnapshot(hp, structure.Health.MaxHealth), structure.Anchor, structure.Rotation,
                    structure.OccupiedCells, structure.Owner);
                _structures[after.EntityId] = after;
                Events.Add(new CombatStructureEvent(structure, after, origins));
            }
            else throw new InvalidOperationException("효과 대상이 현재 투영에 없습니다.");
            ReturnBarrierOccurred = true;
        }

        internal void ResolveDeathBoundary()
        {
            _environment.ResolveFlagDrops(Input, _characters, _structures.Values.ToArray(), _deaths.DeathAnchors, Events);
            if (_deaths.SettleNewDeaths(Input, _characters, _state.IsNight, Events)) ReturnBarrierOccurred = true;
            if (_characters.TryGetValue(Input.ActorId, out var actor) && actor.Health.IsDead) Stop(MovementStopReason.Dead);
            else if (Terminal) Stop(MovementStopReason.BattleEnded);
        }

        internal void ChangeMethod(MovementMethodChange request)
        {
            var current = Character(request.TargetId);
            if (current.Stats.MoveMethod != request.Expected) throw new InvalidOperationException("이동 방식 기대값이 오래되었습니다.");
            if (current.Stats.MoveMethod == request.Final) return;
            MergeCharacter(new CharacterStateChange(current, CharacterSettlementCalculator.Copy(current, current.Health,
                CombatCommandEffects.With(current.Stats, moveMethod: request.Final))));
            ReturnBarrierOccurred = true;
        }

        internal void RecordForcedMove(CharacterCombatFacts before, CharacterCombatFacts after)
        {
            MergeCharacter(new CharacterStateChange(before, after));
            Events.Add(new CombatCharacterEvent(CombatEventKind.Moved, before, after));
            ReturnBarrierOccurred = true;
            if (after.EntityId == Input.ActorId) { _finalStop = after.Anchor.Value; Stop(MovementStopReason.ForcedRelocation); }
        }

        internal void ResolveForcedArrival(EntityId id)
            => ResolveArrival(id, SoulArrivalKind.Forced);

        internal void ResolvePathArrival()
        {
            if (!ArrivalResolved && DistanceSpent > 0)
                ResolveArrival(Input.ActorId, SoulArrivalKind.ResolvedPath);
        }

        private void ResolveArrival(EntityId id, SoulArrivalKind kind)
        {
            var actor = Character(id);
            if (Terminal || actor.Health.IsDead) return;
            ArrivalCells[id] = actor.Anchor.Value;
            if (actor.OwnedId.HasValue && _deaths.CollectOnArrival(CurrentInput(), _characters, actor, kind, Events))
                ReturnBarrierOccurred = true;
            if (Terminal) { Stop(MovementStopReason.BattleEnded); return; }
            actor = Character(id);
            var final = PositionTileModifierCalculator.Replace(actor,
                PositionTileModifierCalculator.ResolveAt(actor.Anchor.Value, Environment));
            PositionTileModifierCalculator.RecordNormalization(Events, actor, final);
            MergeCharacter(new CharacterStateChange(actor, final));
            if (id == Input.ActorId) ArrivalResolved = true;
        }

        internal void ReplacePositionModifier(PositionTileModifier modifier)
        {
            var before = Actor;
            var after = PositionTileModifierCalculator.Replace(before, modifier);
            PositionTileModifierCalculator.RecordNormalization(Events, before, after);
            ReplaceActor(after);
        }

        internal MovementPathResult PathResult() => new(Input.Path, _entered, FinalStop,
            ActionCostSpent, DistanceSpent, StopReason, ReturnBarrierOccurred);

        internal CombatCommandTransition Complete(AttackResult? attack = null, CombatCommandState state = null, bool countdown = false)
            => CombatCommandCalculator.Finish(Input, _characters, _deaths.Counts, _deaths.SoulField, _thorns,
                _structures.Values.ToArray(), _deaths.ConsumedSouls, _deaths.DeathAnchors, _deaths.Souls, attack,
                state ?? CurrentInput().State, countdown: countdown, events: Events, resolvedDeaths: _deaths.ResolvedDeaths,
                environmentBindings: Bindings);

        internal CombatCommandTransition Transition()
        {
            var final = Complete();
            return new CombatCommandTransition(Input, true, "", final.Characters, final.Field, final.FinalState,
                final.Outcome, consumedSoulIds: final.ConsumedSoulIds, events: final.Events, pathMovement: PathResult(),
                environmentBindings: final.EnvironmentBindings, blockedContacts: final.BlockedContacts);
        }
    }
}
