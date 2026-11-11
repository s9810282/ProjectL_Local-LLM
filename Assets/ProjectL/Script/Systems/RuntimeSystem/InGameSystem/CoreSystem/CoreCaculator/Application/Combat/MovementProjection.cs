using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Experience;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>한 명령 안에서만 존재하는 투영값이다. Buffer 원본과 매 단계의 현재값을 구분한다.
    /// 캐릭터·구조물·가시밭 현재값과 이동 기록(진입 칸·거리·정지)를 소유하고,
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
        private readonly ProjectionDependencies _dependencies;
        private readonly ObstacleReactionCalculator _obstacles;
        private readonly CommandAttackGroups _attackGroups;
        private readonly CombatHitCause _sharedHitCause;
        private readonly CombatCommandState _state;
        internal StageGimmickStateSet Gimmicks { get; private set; }
        internal TornadoContactResolution TornadoResolution { get; set; }
        internal Owner? TornadoObserver { get; set; }
        /// <summary>
        /// 명령 완료 정산에서 사망 특성(자폭·공포)을 실행할지입니다. 실제 명령은 켜고, 공개 미리보기·이동 문맥·AI 평가 투영은 끕니다.
        /// 기본 생성자는 <c>preview</c>로, 공격 도착 투영은 <see cref="CommandAttackProjection.CreateArrivalProjection"/>이 공개 미리보기 여부로 정합니다.
        /// </summary>
        internal bool DeathTraitsEnabled { get; set; } = true;
        /// <summary>이 투영이 다루는 턴 사실입니다. 행동 제한 기간의 종료 감소 진영 판단에 씁니다.</summary>
        internal CombatTurnFacts Turn => _state.Turn;
        /// <summary>이번 명령에서 정한 사망 위치입니다(넉백 끝 칸·이동 중 칸 포함). 읽기 전용으로 냅니다.</summary>
        internal IReadOnlyDictionary<EntityId, CellCoord> DeathAnchors => _deaths.DeathAnchors;
        internal uint ItemRandomState { get; set; }
        internal CombatRewardFacts Rewards { get; set; }
        internal TraitTurnFacts TraitTurn { get; set; }
        private CellCoord _finalStop;
        internal EnvironmentSemanticBindings Bindings => _environment.Bindings;
        internal long NextAttackGroup() => _attackGroups.Next();
        /// <summary>포효 피격 원인이다. 공격 명령의 도착 투영은 공격 원인을 공유하고, 독립 투영은 환경 발동(피해 요청)마다 새 원인을 쓴다(D7).</summary>
        internal CombatHitCause NextHitCause() => _sharedHitCause ?? new CombatHitCause();
        internal HashSet<EntityId> EffectTargetIds => _dependencies.EffectTargetIds;
        internal HashSet<CellCoord> TerrainQueryCells => _dependencies.TerrainQueryCells;
        internal Dictionary<EntityId, CellCoord> ArrivalCells => _dependencies.ArrivalCells;
        internal CombatProfileReadSet ProfileReads => _dependencies.ProfileReads;
        internal CombatCommandInput Input { get; }
        internal CharacterCombatFacts Actor => _characters[Input.ActorId];
        internal List<CombatEvent> Events { get; } = new();
        internal int DistanceSpent => _entered.Count;
        internal CellCoord FinalStop => Actor.Anchor ?? _finalStop;
        internal MovementStopReason StopReason { get; private set; }
        internal UnitActionUseSettlementSnapshot ActionUseSettlement { get; private set; }
        internal bool ArrivalResolved { get; private set; }
        internal bool Terminal => BattleOutcomeCalculator.Resolve(_characters.Values.ToArray(), _structures.Values.ToArray(),
            Input.State.Objective).IsStageEnded;
        internal MovementEnvironmentFacts Environment => _environment.Facts;
        /// <summary>이 투영의 캐릭터 사전·사건 목록을 쓰는 피해 적용 경계다. 공격의 도착 투영은 공격 투영과 같은 인스턴스를 쓴다.</summary>
        internal CombatDamageApplication Damage { get; }

        internal MovementProjection(CombatCommandInput input, bool preview = false)
            : this(input, preview, null)
        {
        }

        internal MovementProjection(CombatCommandInput input, bool preview, CombatProfileReadSet profileReads)
            : this(input, preview, profileReads, null)
        {
        }

        internal MovementProjection(CombatCommandInput input, bool preview, CombatProfileReadSet profileReads,
            MovementEnvironmentFacts visibleEnvironment)
        {
            Input = input;
            _dependencies = new ProjectionDependencies(profileReads);
            _state = input.State;
            Gimmicks = _state.Gimmicks;
            ItemRandomState = _state.ItemRandomState;
            Rewards = _state.Rewards;
            TraitTurn = _state.TraitTurn;
            _characters = input.Characters.ToDictionary(c => c.EntityId);
            TornadoObserver = preview ? Actor.Owner : (Owner?)null;
            DeathTraitsEnabled = !preview;
            Damage = CombatDamageApplication.For(input, _characters, Events, captureMovementUses: !preview, profileReads: ProfileReads);
            _environment = new CommandEnvironmentState(preview ? visibleEnvironment ?? input.State.Environment.VisibleTo(Actor.Owner) : input.State.Environment,
                input.EnvironmentBindings, _state.EnvironmentProgress);
            _thorns = input.State.ThornFields.ToDictionary(f => f.Cell);
            _structures = input.State.Structures.ToDictionary(s => s.EntityId);
            if (input.ObstacleContents.Count > 0 || input.State.Structures.Any(s => ObstacleContentProfile.IsNativeObstacleKey(s.Definition.Key)))
                ObstacleContentProfile.Validate(input.ObstacleContents, input.EnvironmentBindings, input.State.Structures);
            _attackGroups = new CommandAttackGroups();
            _deaths = new CommandDeathSettlement(input.SoulCounts.ToDictionary(p => p.Key, p => p.Value), input.Souls,
                new List<long>(), new Dictionary<EntityId, CellCoord>(), new SoulEffectCalculator(input.Artifacts), null);
            _obstacles = new ObstacleReactionCalculator(input, Environment, Damage, _structures, _deaths.DeathAnchors, _attackGroups);
            _finalStop = input.Path.Origin;
        }

        // 공격·배치의 현재 투영을 이어받아 동일한 진입 계산과 최종 게시 경계를 사용한다.
        internal MovementProjection(CombatCommandInput input, CombatCommandState state,
            CombatDamageApplication damage, Dictionary<EntityId, StructureCombatFacts> structures,
            Dictionary<CellCoord, ThornFieldCellSnapshot> thorns, Dictionary<CharacterInstanceId, int> counts,
            SoulFieldSnapshot soulField, List<long> consumedSouls,
            Dictionary<EntityId, CellCoord> deathAnchors, SoulEffectCalculator souls, CommandAttackGroups attackGroups = null,
            MovementEnvironmentFacts environment = null, CombatHitCause hitCause = null)
        {
            Input = input;
            _state = state;
            Gimmicks = state.Gimmicks;
            ItemRandomState = state.ItemRandomState;
            Rewards = state.Rewards;
            TraitTurn = state.TraitTurn;
            Damage = damage ?? throw new ArgumentNullException(nameof(damage));
            _dependencies = new ProjectionDependencies(Damage.ProfileReads);
            _characters = damage.Characters;
            _structures = structures;
            _thorns = thorns;
            Events = damage.Events;
            _attackGroups = attackGroups ?? new CommandAttackGroups();
            _sharedHitCause = hitCause;
            _environment = new CommandEnvironmentState(environment ?? state.Environment, input.EnvironmentBindings, state.EnvironmentProgress);
            _deaths = new CommandDeathSettlement(counts, soulField, consumedSouls, deathAnchors, souls,
                _characters.Values.Where(c => c.Health.IsDead).Select(c => c.EntityId));
            _obstacles = new ObstacleReactionCalculator(input, Environment, Damage, _structures, _deaths.DeathAnchors, _attackGroups,
                hitCause);
        }

        internal static MovementProjection ForArrival(CombatCommandInput input,
            Dictionary<EntityId, CharacterCombatFacts> characters, List<CombatEvent> events, CombatCommandState state = null) =>
            new(input, state ?? input.State, CombatDamageApplication.For(input, characters, events), input.State.Structures.ToDictionary(s => s.EntityId),
                input.State.ThornFields.ToDictionary(f => f.Cell), input.SoulCounts.ToDictionary(p => p.Key, p => p.Value),
                input.Souls, new List<long>(), new Dictionary<EntityId, CellCoord>(), new SoulEffectCalculator(input.Artifacts));

        internal void BeginOrdinaryMove()
        {
            ActionUseSettlement = CombatActionAdmission.Settlement(Actor, UnitActionUse.Moved);

            ReplaceActor(Actor.With(actionUse: UnitActionUse.Moved));
            if (Input.Path.StepCount > Actor.Stats.MoveRange)
            {
                var extension = Input.State.Environment.Tiles.OfType<TraitTrailFact>().Where(t => t.Active &&
                    t.Key == TraitKey.GroundCompaction && t.Cell == Actor.Anchor.Value && t.MovementRangeBonus > 0)
                    .OrderByDescending(t => t.MovementRangeBonus).ThenBy(t => t.Id).FirstOrDefault();
                if (extension != null) CombatCommandEvents.Trait(Events, new EntityId(extension.Origin.OwnerInstanceId),
                    TraitKey.GroundCompaction, TraitActivationKind.RangeChanged, extension.MovementRangeBonus,
                    new[] { Actor.EntityId }, new[] { extension.Cell });
            }
        }

        internal void SetActionUseSettlement(UnitActionUseSettlementSnapshot actionUseSettlement)
        {
            ActionUseSettlement = actionUseSettlement ?? throw new ArgumentNullException(nameof(actionUseSettlement));

        }

        internal void Enter(CellCoord cell)
        {
            var before = Actor;
            var after = CombatMovementCalculator.MoveTo(before, cell);
            ReplaceActor(after);
            _entered.Add(cell);
            _finalStop = cell;
            Events.Add(new CombatCharacterEvent(CombatEventKind.Moved, before, after));
            TraitTrailCalculator.EmitOnLeave(this, before.Anchor.Value);
        }

        internal void ReplaceActor(CharacterCombatFacts actor)
        {
            if (actor.EntityId != Input.ActorId) throw new ArgumentException("이동자 식별자를 바꿀 수 없습니다.");
            _characters[Input.ActorId] = actor;
        }

        /// <summary>두 배치를 먼저 확정한 뒤 사건과 도착 효과를 만듭니다. 동승자의 자발 이동 효과는 실행하지 않습니다.</summary>
        internal void ExchangeEndpoint(CharacterCombatFacts passenger, CellCoord endpoint)
        {
            var before = Actor;
            var departed = PositionTileModifierCalculator.Replace(passenger, PositionTileModifier.None);
            PositionTileModifierCalculator.RecordNormalization(Events, passenger, departed);
            var moved = CombatMovementCalculator.MoveTo(before, endpoint);
            var displaced = CombatMovementCalculator.MoveTo(departed, Input.Path.Origin);
            _characters[before.EntityId] = moved;
            _characters[passenger.EntityId] = displaced;
            _entered.Add(endpoint);
            _finalStop = endpoint;
            Events.Add(new CombatCharacterEvent(CombatEventKind.Moved, before, moved, null, null,
                CombatDamageCause.NumericDamage, new CombatMovementCause(CombatMovementAgency.Voluntary, TraitKey.PositionSwap)));
            Events.Add(new CombatCharacterEvent(CombatEventKind.Moved, departed, displaced, null, null,
                CombatDamageCause.NumericDamage, new CombatMovementCause(CombatMovementAgency.Forced, TraitKey.PositionSwap)));
            CombatCommandEvents.Trait(Events, before.EntityId, TraitKey.PositionSwap, TraitActivationKind.MovementChanged,
                1, new[] { before.EntityId, passenger.EntityId }, new[] { endpoint, Input.Path.Origin });
            int shield = PositionSwapShields.Amount(Input, before, ProfileReads);
            if (shield > 0)
            {
                GrantSwapShield(moved, before, shield);
                GrantSwapShield(displaced, before, shield);
                CombatCommandEvents.Trait(Events, before.EntityId, TraitKey.PositionSwap, TraitActivationKind.ShieldGranted,
                    shield, new[] { before.EntityId, passenger.EntityId });
            }
            TraitTrailCalculator.EmitOnLeave(this, before.Anchor.Value);
        }

        private void GrantSwapShield(CharacterCombatFacts recipient, CharacterCombatFacts source, int amount)
        {
            var after = PositionSwapShields.Add(recipient, source, amount);
            _characters[after.EntityId] = after;
            CombatCommandEvents.TraitState(Events, recipient, after);
        }

        internal void Stop(MovementStopReason reason)
        {
            if (StopReason == MovementStopReason.Completed || reason == MovementStopReason.Dead ||
                reason == MovementStopReason.BattleEnded) StopReason = reason;
        }
        internal CharacterCombatFacts Character(EntityId id) => _characters[id];
        internal StructureCombatFacts Structure(EntityId id) => _structures.GetValueOrDefault(id);
        /// <summary>이 투영의 현재 구조물(장애물 포함)입니다. 읽기 전용으로 냅니다.</summary>
        internal IEnumerable<StructureCombatFacts> CurrentStructures => _structures.Values;
        internal MovementTileFact Source(long id) => _environment.Source(id);
        internal long NextEnvironmentSourceId => _environment.Progress.NextSourceId;
        internal void AddTraitTrail(TraitTrailFact trail)
        {
            _environment.AddTraitTrail(trail);
            TraitTrailCalculator.RecordCreation(trail, Events);
        }
        internal bool HasThorn(CellCoord cell) => _thorns.ContainsKey(cell);
        internal ThornFieldCellSnapshot Thorn(CellCoord cell) => _thorns[cell];

        internal CombatCommandInput CurrentInput()
        {
            using var work = BeginCurrentInput();
            while (!work.IsComplete) work.Step();
            return work.Result;
        }

        internal InputWork BeginCurrentInput() => new InputWork(this);

        /// <summary>보유 정산과 불변 조회값 조립을 마칠 때까지 소비자에게 중간 값을 노출하지 않는다.</summary>
        internal sealed class InputWork : IDisposable
        {
            private IEnumerator<int> _steps;
            private CombatCommandInput _result;
            private bool _complete;
            private bool _disposed;
            private bool _faulted;
            private long _version;
            internal bool IsComplete
            {
                get
                {
                    RequireActive();
                    return _complete;
                }
            }
            internal long WorkVersion
            {
                get
                {
                    RequireActive();
                    return _version;
                }
            }
            internal CombatCommandInput Result
            {
                get
                {
                    RequireActive();
                    if (!_complete) throw new InvalidOperationException("현재 투영 조회가 끝나지 않았습니다.");
                    return _result;
                }
            }

            internal InputWork(MovementProjection projection)
            {
                _steps = Build(projection).GetEnumerator();
            }

            internal void Step()
            {
                RequireActive();
                if (_complete) return;
                using var sample = CoreProfiling.Sample(CoreProfileSection.ProjectionCurrentInput);
                try
                {
                    _complete = !_steps.MoveNext();
                    _version++;
                    if (_complete)
                    {
                        _steps.Dispose();
                        _steps = null;
                    }
                }
                catch
                {
                    _faulted = true;
                    _steps?.Dispose();
                    _steps = null;
                    _result = null;
                    throw;
                }
            }

            private IEnumerable<int> Build(MovementProjection projection)
            {
                yield return 0;
                using (var presence = projection.Damage.BeginReconcilePresence())
                {
                    while (presence != null && !presence.IsComplete)
                    {
                        presence.Step();
                        yield return 0;
                    }
                }
                var structures = new List<StructureCombatFacts>();
                foreach (var structure in projection._structures.Values)
                {
                    structures.Add(structure);
                    yield return 0;
                }
                var fields = new List<ThornFieldCellSnapshot>();
                foreach (var field in projection._thorns.Values)
                {
                    fields.Add(field);
                    yield return 0;
                }
                var s = projection._state;
                var state = new CombatCommandState(s.BoardWidth, s.BoardHeight, s.Phase, structures, fields, s.Turn,
                    projection.Environment, projection._environment.Progress, s.WallBlocking, s.Objective, s.Allocation,
                    projection.ItemRandomState, projection.Rewards, projection.TraitTurn, s.MovementUndo, projection.Gimmicks, s.Reinforcements);
                yield return 0;
                var characters = new List<CharacterCombatFacts>();
                foreach (var character in projection._characters.Values)
                {
                    characters.Add(character);
                    yield return 0;
                }
                var main = characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main);
                var world = projection.Input.WithWorld(state, characters,
                    projection.Input.Main.WithCurrentHealth(main.Health.CurrentHealth), projection._deaths.Counts,
                    projection._deaths.SoulField, projection.Bindings);
                yield return 0;
                _result = world.WithPersistentFacts(projection.Damage.ProjectOwned(projection.Input.Owned),
                    projection.Input.ItemInventory, projection.Input.GoldBalance, projection.Input.FameBalance);
                yield return 0;
            }

            private void RequireActive()
            {
                if (_disposed) throw new ObjectDisposedException(nameof(InputWork));
                if (_faulted) throw new InvalidOperationException("실패한 투영 조회는 다시 진행할 수 없습니다.");
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _steps?.Dispose();
                _steps = null;
                _result = null;
            }
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
            _environment.ArrivalRuleSourcesAt(cell), arriving.Owner == _state.Turn.Controller);
        }

        internal void MergeCharacter(CharacterStateChange change)
        {
            if (!_characters.TryGetValue(change.Expected.EntityId, out var current) || !ReferenceEquals(current, change.Expected))
                throw new InvalidOperationException("효과 계산의 기대값은 현재 투영 사실이어야 합니다.");
            _characters[current.EntityId] = change.Final;
        }

        internal void MergeStructure(StructureCombatFacts final, CombatMovementCause movementCause = null)
        {
            if (final == null) throw new ArgumentNullException(nameof(final));
            var before = _structures[final.EntityId];
            _structures[final.EntityId] = final;
            Events.Add(new CombatStructureEvent(before, final, null, CombatDamageCause.NumericDamage, movementCause));
        }

        /// <summary>추락은 피해·장애물 반응을 거치지 않는다. 원인 기록은 사망 정산 경계에서도 공유한다.</summary>
        internal void ApplyFall(EntityId id, long terrainSourceId, CellCoord cell)
        {
            Events.Add(new CombatFallEvent(id, terrainSourceId, cell));
            if (_characters.TryGetValue(id, out var character))
            {
                if (!character.HeldEnvironment.SameValues(CharacterHeldEnvironmentState.Empty))
                {
                    var cleared = HeldEnvironmentEffects.WithHeldState(character, CharacterHeldEnvironmentState.Empty);
                    MergeCharacter(new CharacterStateChange(character, cleared));
                    Events.Add(new CombatHeldEnvironmentEvent(character, cleared));
                    character = cleared;
                }
                var dead = CombatCommandCalculator.Hurt(character, character.Health.CurrentHealth);
                MergeCharacter(new CharacterStateChange(character, dead));
                _deaths.DeathAnchors[id] = cell;
                CombatCommandEvents.Health(Events, character, dead);
                Damage.SettleTraitKills();
            }
            else
            {
                var structure = Structure(id);
                MergeStructure(new StructureCombatFacts(id, structure.Definition,
                    new HealthStateSnapshot(0, structure.Health.MaxHealth), structure.Anchor, structure.Rotation,
                    structure.OccupiedCells, structure.Owner));
            }
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
        }

        internal int ChooseRandom(int count) => _environment.ChooseRandom(count);

        internal void ReplaceGimmick(StageGimmickState expected, StageGimmickState final)
        {
            if (!Gimmicks.Get(expected.Id).SameValues(expected)) throw new InvalidOperationException("기믹 투영 기대값이 오래되었습니다.");
            Gimmicks = Gimmicks.Replace(final);
        }

        internal void SetEnvironmentRandom(uint expected, uint final) => _environment.SetRandom(expected, final);


        internal void ChangeTerrain(StaticTerrainFact expected, StaticTerrainFact final)
        {
            _environment.ChangeTerrain(expected, final);
            Events.Add(new CombatMovementSourceEvent(expected, final));
        }

        internal void ReconcilePresence(IReadOnlyList<CellCoord> cells)
        {
            foreach (var _ in ReconcilePresenceSteps(cells)) { }
        }

        internal IEnumerable<string> ReconcilePresenceSteps(IReadOnlyList<CellCoord> cells)
        {
            using var observation = BeginCurrentInput();
            while (!observation.IsComplete)
            {
                observation.Step();
                yield return "EnvironmentPresenceObservation";
            }
            var current = observation.Result;
            foreach (var change in EnvironmentPresenceReconciler.Calculate(current.Characters, Environment, cells, current.Main).Characters)
            {
                PositionTileModifierCalculator.RecordNormalization(Events, change.Expected, change.Final);
                if (change.Expected.Health.Equals(change.Final.Health))
                    CombatCommandEvents.TraitState(Events, change.Expected, change.Final);
                MergeCharacter(change);
                yield return "EnvironmentPresenceChange";
            }
        }

        internal void ChangeSource(MovementTileFact selected, MovementStepEffects effects)
        {
            if (!_environment.ApplySelectedChange(selected, effects)) return;
            Events.Add(new CombatMovementSourceEvent(effects.ExpectedSource, effects.FinalSource));
        }

        internal void ConsumeThorn(CellCoord cell)
        {
            var source = _thorns[cell];
            CombatCommandCalculator.ConsumeFields(_thorns, new[] { cell });
            Events.Add(new CombatThornConsumedEvent(source));
        }

        internal void ChangeHealth(MovementHealthRequest request, bool heal, CombatDamageOrigin origin = null)
        {
            foreach (var _ in ChangeHealthSteps(request, heal, origin)) { }
        }

        internal IEnumerable<string> ChangeHealthSteps(MovementHealthRequest request, bool heal, CombatDamageOrigin origin = null)
        {
            if (heal) origin = null;
            else if (request.DamageOrigin != null)
                origin = new CombatDamageOrigin(request.DamageOrigin.PhysicalCell, request.DamageOrigin.CreditedCharacterId,
                    request.DamageOrigin.AttackGroup > 0 ? request.DamageOrigin.AttackGroup : origin?.AttackGroup ?? _attackGroups.Next());
            if (!heal && origin == null)
                origin = new CombatDamageOrigin(null, null, _attackGroups.Next());
            var origins = origin == null ? null : new[] { origin };
            if (_characters.TryGetValue(request.TargetId, out var before))
            {
                if (before.Health.IsDead || before.Area != CharacterRuntimeArea.Field) yield break;
                if (!heal)
                {
                    TerrainQueryCells.Add(before.Anchor.Value);
                    if (origin?.PhysicalCell is CellCoord source) TerrainQueryCells.Add(source);
                    // 피해 계산을 시작하면 0 피해여도 포효 피격으로 센다(D7).
                    if (RoarHitCounter.Record(_characters, before.EntityId, NextHitCause(), _state.InCombat))
                    {
                        before = _characters[before.EntityId];
                    }
                }
                using var observation = BeginCurrentInput();
                while (!observation.IsComplete)
                {
                    observation.Step();
                    yield return "MovementHealthObservation";
                }
                var current = observation.Result;
                int amount = heal ? request.Amount : CombatDamageCalculator.Environment(current,
                    request.Amount, origin, before.Anchor).FinalDamage;
                CharacterCombatFacts after;
                if (heal)
                {
                    after = CharacterSettlementCalculator.SetHealth(before, current.Main,
                        (int)Math.Min(before.Health.MaxHealth, (long)before.Health.CurrentHealth + amount)).Characters.Single().Final;
                    if (before.Health.CurrentHealth == after.Health.CurrentHealth) yield break;
                    MergeCharacter(new CharacterStateChange(before, after));
                    Damage.RecordRecovery(before.EntityId, after.Health.CurrentHealth - before.Health.CurrentHealth);
                    CombatCommandEvents.Health(Events, before, after);
                }
                else
                {
                    // 적용 경계가 소지자와 효과 대상의 최종 사실을 공유 사전에 반영한다.
                    CombatDamageOutcome outcome = default;
                    foreach (var phase in Damage.ApplySteps(before.EntityId, CombatDamageKind.EnvironmentDamage, origin,
                        amount, value => outcome = value, heldDefenseGroups: _attackGroups))
                        yield return phase;
                    after = outcome.After;
                    if (outcome.ActualHealthLoss == 0 && before.Items.SameValues(after.Items)) yield break;
                }
                if (after.Health.IsDead) _deaths.DeathAnchors[before.EntityId] = before.Anchor.Value;
                yield return "MovementHealthChange";
            }
            else if (_structures.TryGetValue(request.TargetId, out var structure))
            {
                if (structure.Health.IsDead) yield break;
                if (!heal && _obstacles.HasProfile(request.TargetId))
                {
                    _obstacles.DamageStructure(request.TargetId, request.Amount, origin);
                    yield return "MovementObstacleDamage";
                    foreach (var phase in _obstacles.ResolveExplosionSteps()) yield return phase;
                    EffectTargetIds.UnionWith(_obstacles.BlastTargetIds);
                    TerrainQueryCells.UnionWith(_obstacles.TerrainQueryCells);
                    yield return "MovementObstacleEvidence";
                    yield break;
                }
                int amount = heal ? request.Amount : CombatDamageCalculator.Environment(request.Amount, origin).FinalDamage;
                int hp = heal ? (int)Math.Min(structure.Health.MaxHealth, (long)structure.Health.CurrentHealth + amount) :
                    Math.Max(0, structure.Health.CurrentHealth - Math.Min(structure.Health.CurrentHealth, amount));
                if (hp == structure.Health.CurrentHealth) yield break;
                var after = new StructureCombatFacts(structure.EntityId, structure.Definition,
                    new HealthStateSnapshot(hp, structure.Health.MaxHealth), structure.Anchor, structure.Rotation,
                    structure.OccupiedCells, structure.Owner);
                _structures[after.EntityId] = after;
                Events.Add(new CombatStructureEvent(structure, after, origins));
                yield return "MovementStructureHealth";
            }
            else throw new InvalidOperationException("효과 대상이 현재 투영에 없습니다.");
        }

        internal void ResolveDeathBoundary()
        {
            var fallen = Events.OfType<CombatFallEvent>().Select(e => e.EntityId).ToHashSet();
            _environment.ResolveFlagDrops(Input, _characters, _structures.Values.ToArray(), _deaths.DeathAnchors, Events, fallen);
            _deaths.SettleNewDeaths(Input, _characters, _state.InCombat, Events, fallen);
            Damage.SettleTraitKills();
            if (_characters.TryGetValue(Input.ActorId, out var actor) && actor.Health.IsDead) Stop(MovementStopReason.Dead);
            else if (Terminal) Stop(MovementStopReason.BattleEnded);
        }

        internal void ChangeDebuffs(MovementDebuffChange request)
        {
            var current = Character(request.TargetId);
            if (current.Health.IsDead) return;
            if (!current.Debuffs.SameValues(request.Expected)) throw new InvalidOperationException("디버프 기대값이 오래되었습니다.");
            if (current.Debuffs.SameValues(request.Final)) return;
            var final = CombatDebuffApplication.WithDebuffs(current, request.Final);
            MergeCharacter(new CharacterStateChange(current, final));
            CombatCommandEvents.TraitState(Events, current, final);
        }

        /// <summary>
        /// 살아 있는 필드 캐릭터에 효과 하나를 공통 겹침 규칙(<c>AppliedStatusRules.Apply</c>)으로 겁니다.
        /// 실제로 바뀌었을 때만 특성 상태 사건을 남기고 참을 돌려줍니다(긴 쪽 규칙으로 바뀌지 않을 수 있음).
        /// </summary>
        internal bool ApplyStatus(EntityId targetId, Contracts.CombatEffects.AppliedStatus status)
        {
            if (status == null) throw new ArgumentNullException(nameof(status));
            var current = Character(targetId);
            if (current.Health.IsDead || current.Area != CharacterRuntimeArea.Field) return false;
            var statuses = Domain.CombatEffects.Rules.AppliedStatusRules.Apply(current.StatusEffects, status);
            if (ReferenceEquals(statuses, current.StatusEffects)) return false;
            var final = current.WithStatusEffects(statuses);
            MergeCharacter(new CharacterStateChange(current, final));
            CombatCommandEvents.TraitState(Events, current, final);
            return true;
        }

        internal void ChangeMethod(MovementMethodChange request)
        {
            var current = Character(request.TargetId);
            if (current.Stats.MoveMethod != request.Expected) throw new InvalidOperationException("이동 방식 기대값이 오래되었습니다.");
            if (current.Stats.MoveMethod == request.Final) return;
            var final = CharacterSettlementCalculator.Copy(current, current.Health, CombatCommandEffects.With(current.Stats, moveMethod: request.Final));
            MergeCharacter(new CharacterStateChange(current, final));
            CombatCommandEvents.TraitState(Events, current, final);
        }

        internal void RecordForcedMove(CharacterCombatFacts before, CharacterCombatFacts after, long? tornadoSourceId = null,
            TornadoContactOrigin tornadoContact = null, CombatMovementCause movementCause = null)
        {
            MergeCharacter(new CharacterStateChange(before, after));
            Events.Add(new CombatCharacterEvent(tornadoSourceId.HasValue ? CombatEventKind.Relocated : CombatEventKind.Moved,
                before, after, null, tornadoSourceId, CombatDamageCause.NumericDamage,
                movementCause ?? new CombatMovementCause(CombatMovementAgency.Forced), tornadoContact));
            if (after.EntityId == Input.ActorId) { _finalStop = after.Anchor.Value; Stop(MovementStopReason.ForcedRelocation); }
        }

        internal int ResolveForcedArrival(EntityId id)
            => ResolveArrival(id, SoulArrivalKind.Forced);

        internal IEnumerable<string> ResolveForcedArrivalSteps(EntityId id, Action<int> completed)
            => ResolveArrivalSteps(id, SoulArrivalKind.Forced, completed);

        /// <summary>기존 공격 순서의 사망 정산 시점을 유지하며, 이미 정산된 이동 중 사망은 되돌리지 않습니다.</summary>
        internal void DeferDeathsUntilCompletion(IEnumerable<EntityId> ids) => _deaths.DeferUntilCompletion(ids);

        /// <summary>실제 강제 진입 직후 한 번 호출한다. 추락하면 이후 진입 효과를 중단한다.</summary>
        internal bool ResolveForcedTerrainEntry(EntityId id, CellCoord cell)
        {
            if (CliffFallCalculator.ResolveAt(this, id, cell)) return true;
            if (Structure(id) == null) WeakGroundCalculator.RegisterEntry(this, id, cell);
            return false;
        }

        internal void ResolvePathArrival()
        {
            foreach (var _ in ResolvePathArrivalSteps()) { }
        }

        internal IEnumerable<string> ResolvePathArrivalSteps()
        {
            if (!ArrivalResolved && DistanceSpent > 0)
                foreach (var phase in ResolveArrivalSteps(Input.ActorId, SoulArrivalKind.ResolvedPath, null)) yield return phase;
        }

        private int ResolveArrival(EntityId id, SoulArrivalKind kind)
        {
            int result = 0;
            foreach (var _ in ResolveArrivalSteps(id, kind, value => result = value)) { }
            return result;
        }

        private IEnumerable<string> ResolveArrivalSteps(EntityId id, SoulArrivalKind kind, Action<int> completed)
        {
            var actor = Character(id);
            if (Terminal || actor.Health.IsDead) yield break;
            ArrivalCells[id] = actor.Anchor.Value;
            if (actor.OwnedId.HasValue)
            {
                using var observation = BeginCurrentInput();
                while (!observation.IsComplete)
                {
                    observation.Step();
                    yield return "MovementArrivalObservation";
                }
                _deaths.CollectOnArrival(observation.Result, _characters, actor, kind, Events);
                yield return "MovementArrivalSouls";
            }
            var collected = Character(id);
            Damage.RecordRecovery(id, Math.Max(0, collected.Health.CurrentHealth - actor.Health.CurrentHealth));
            if (Terminal) { Stop(MovementStopReason.BattleEnded); yield break; }
            actor = collected;
            var final = PositionTileModifierCalculator.Replace(actor,
                PositionTileModifierCalculator.ResolveAt(actor.Anchor.Value, Environment));
            PositionTileModifierCalculator.RecordNormalization(Events, actor, final);
            MergeCharacter(new CharacterStateChange(actor, final));
            if (id == Input.ActorId) ArrivalResolved = true;
            completed?.Invoke(Math.Max(0, actor.Health.CurrentHealth - final.Health.CurrentHealth));
            yield return "MovementArrivalNormalization";
        }

        internal void ReplacePositionModifier(PositionTileModifier modifier)
        {
            var before = Actor;
            var after = PositionTileModifierCalculator.Replace(before, modifier);
            PositionTileModifierCalculator.RecordNormalization(Events, before, after);
            ReplaceActor(after);
        }

        internal MovementPathResult PathResult() => new(Input.Path,_entered,FinalStop,DistanceSpent,StopReason);

        internal CombatCommandTransition Complete(AttackResult? attack = null, CombatTurnFacts turn = null,
            CombatFlowPhase? phase = null, CombatAllocationFacts allocation = null)
        {
            using var work = BeginComplete(attack, turn, phase, allocation);
            while (!work.IsComplete) work.Step();
            return work.Result;
        }

        internal CompletionWork BeginComplete(AttackResult? attack = null, CombatTurnFacts turn = null,
            CombatFlowPhase? phase = null, CombatAllocationFacts allocation = null) =>
            new CompletionWork(this, attack, turn, phase, allocation);

        /// <summary>명령 끝의 관찰·보상·최종 조립을 같은 순서로 진행하며 중간 전이는 반환하지 않는다.</summary>
        internal sealed class CompletionWork : IDisposable
        {
            private IEnumerator<string> _steps;
            private CombatCommandTransition _result;
            private bool _complete;
            private bool _disposed;
            private bool _faulted;
            private long _version;
            internal string Phase { get; private set; } = "CompletionBegin";
            internal bool IsComplete
            {
                get
                {
                    RequireActive();
                    return _complete;
                }
            }
            internal long WorkVersion
            {
                get
                {
                    RequireActive();
                    return _version;
                }
            }
            internal CombatCommandTransition Result
            {
                get
                {
                    RequireActive();
                    if (!_complete) throw new InvalidOperationException("명령 완료 정산이 끝나지 않았습니다.");
                    return _result;
                }
            }

            internal CompletionWork(MovementProjection projection, AttackResult? attack, CombatTurnFacts turn,
                CombatFlowPhase? phase, CombatAllocationFacts allocation)
            {
                _steps = Build(projection, attack, turn, phase, allocation).GetEnumerator();
            }

            internal void Step()
            {
                RequireActive();
                if (_complete) return;
                using var sample = CoreProfiling.Sample(CoreProfileSection.CommandCompletion);
                try
                {
                    _complete = !_steps.MoveNext();
                    _version++;
                    if (_complete)
                    {
                        _steps.Dispose();
                        _steps = null;
                        Phase = "CompletionDone";
                    }
                    else Phase = _steps.Current;
                }
                catch
                {
                    _faulted = true;
                    _steps?.Dispose();
                    _steps = null;
                    _result = null;
                    throw;
                }
            }

            private IEnumerable<string> Build(MovementProjection projection, AttackResult? attack, CombatTurnFacts turn,
                CombatFlowPhase? phase, CombatAllocationFacts allocation)
            {
                yield return "CompletionBegin";
                // 약한 지반은 원래 명령 끝에서만 정산하며, 이미 종료된 전투에서는 건너뛴다.
                if (!projection.Terminal)
                    foreach (var child in WeakGroundCalculator.CollapsePendingSteps(projection))
                        yield return child;
                yield return "CompletionTerrain";
                // 사망 특성(자폭·공포)은 명령의 모든 피해·강제 이동·처치 연쇄가 끝난 뒤, 처치 특성·영혼·보상 정산 앞에서 한 번 확인한다.
                // 실행할 것이 없으면 단계를 내지 않아 기존 명령의 단계 순서가 그대로다.
                if (projection.DeathTraitsEnabled && !projection.Terminal)
                    foreach (var child in DeathTraitApplication.ResolveSteps(projection))
                        yield return child;
                projection.Damage.SettleTraitKills();
                yield return "CompletionTraitKills";
                if (!projection.Terminal)
                {
                    using var observed = projection.BeginCurrentInput();
                    while (!observed.IsComplete)
                    {
                        observed.Step();
                        yield return "CompletionHunterObservation";
                    }
                    HunterApplication.Reconcile(observed.Result, projection._characters, projection.ChooseRandom, projection.Events);
                    yield return "CompletionHunterReconcile";
                }
                using var finalInput = projection.BeginCurrentInput();
                while (!finalInput.IsComplete)
                {
                    finalInput.Step();
                    yield return "CompletionFinalObservation";
                }
                var state = finalInput.Result.State.With(turn: turn, phase: phase, allocation: allocation);
                yield return "CompletionState";
                var structures = new List<StructureCombatFacts>();
                foreach (var structure in projection._structures.Values)
                {
                    structures.Add(structure);
                    yield return "CompletionStructure";
                }
                using var finish = CombatCommandCalculator.BeginFinish(projection.Input, projection._characters,
                    projection._deaths.Counts, projection._deaths.SoulField, projection._thorns, structures.ToArray(),
                    projection._deaths.ConsumedSouls, projection._deaths.DeathAnchors, projection._deaths.Souls, attack,
                    state, events: projection.Events, resolvedDeaths: projection._deaths.ResolvedDeaths,
                    environmentBindings: projection.Bindings, actionUseSettlement: projection.ActionUseSettlement,
                    // 강제 이동은 자발 경로 거리 합계에 포함하지 않는다.
                    stageTally: new CombatStageTally(projection.Damage.CaptureStageDamage(), projection.Input.ActorId, projection.DistanceSpent),
                    proposedOwned: projection.Damage.OwnedChanges);
                while (!finish.IsComplete)
                {
                    finish.Step();
                    yield return "CompletionFinish";
                }
                _result = finish.Result.WithDamageExecutions(projection.Damage.DamageExecutions);
                yield return "CompletionSeal";
            }

            private void RequireActive()
            {
                if (_disposed) throw new ObjectDisposedException(nameof(CompletionWork));
                if (_faulted) throw new InvalidOperationException("실패한 완료 정산은 다시 진행할 수 없습니다.");
            }

            public void Dispose()
            {
                if (_disposed) return;
                _disposed = true;
                _steps?.Dispose();
                _steps = null;
                _result = null;
            }
        }

        internal CombatCommandTransition Transition()
        {
            return TransitionFromCompleted(Complete());
        }

        internal CombatCommandTransition TransitionFromCompleted(CombatCommandTransition final)
        {
            if (final == null) throw new ArgumentNullException(nameof(final));
            return new CombatCommandTransition(Input, true, "", final.Characters, final.Field, final.FinalState,
                final.Outcome, consumedSoulIds: final.ConsumedSoulIds, events: final.Events, pathMovement: PathResult(),
                environmentBindings: final.EnvironmentBindings, blockedContacts: final.BlockedContacts,
                rewardChange: final.RewardChange, rewardSettlement: final.RewardSettlement, actionUseSettlement: final.ActionUseSettlement,
                movementEffectUses: final.MovementEffectUses, damageExecutions: final.DamageExecutions);
        }
    }
}
