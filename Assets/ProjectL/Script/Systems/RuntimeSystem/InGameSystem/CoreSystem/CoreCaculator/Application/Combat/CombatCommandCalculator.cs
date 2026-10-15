using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Movement;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Tiles;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>한 명령의 최종 변경만 계산한다. 계산 중의 투영값은 호출 이후 보관하지 않는다.</summary>
    public static class CombatCommandCalculator
    {
        public static CombatCommandTransition Calculate(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
            => Calculate(input, rules, out _);

        public static AttackRangeResult GetAttackRange(CombatCommandInput input) => CombatAttackQueries.Range(input);

        public static CombatCommandTransition Advance(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
        {
            MovementEnvironmentCalculator.RequireRules(rules);
            if (input.ProcessingKind != CombatProcessingKind.Advance || input.State.Phase != CombatFlowPhase.Automatic ||
                input.State.Turn.StagePhase != StagePhase.Combat)
                throw new InvalidOperationException("자동 진행 입력이 아닙니다.");
            return input.State.ActiveActorId.IsEmpty
                ? CombatGimmickCalculator.TryAdvance(input) ?? CombatTurnCalculator.BeginNext(input)
                : EnemyAi.EnemyAiTurnService.Calculate(input, rules);
        }

        public static AttackPreview Preview(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules)
            => Preview(input, rules, out _);

        internal static AttackPreview Preview(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            out CombatCommandTransition projection)
        {
            projection = Calculate(input, rules, out var preview, publicPreview: true);
            return projection.Accepted ? preview : AttackPreview.Failure(
                Enum.Parse<AttackFailureReason>(projection.Reason), input.ActorId, EntityId.Empty, projection.BlockedContacts);
        }

        private static CombatCommandTransition Calculate(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            out AttackPreview preview, bool publicPreview = false)
        {
            preview = default;
            if (input == null) throw new ArgumentNullException(nameof(input));
            MovementEnvironmentCalculator.RequireRules(rules);
            if (input.Kind == CombatCommandKind.Move) return CombatMovementCalculator.Calculate(input, rules);
            if (input.Kind == CombatCommandKind.ReturnToCheckpoint) return CheckpointReturnCalculator.Calculate(input);
            if (input.Kind == CombatCommandKind.EndUnitTurn) return CombatTurnCalculator.EndTurn(input, rules);
            if (input.Kind == CombatCommandKind.EnterCombat) return CombatTurnCalculator.EnterCombat(input, rules);
            if (input.Kind == CombatCommandKind.PlaceMainCharacter || input.Kind == CombatCommandKind.PlaceUnit)
                return CombatPlacementCalculator.Place(input, rules);
            if (input.Kind == CombatCommandKind.ReturnUnit) return CombatPlacementCalculator.Return(input);
            if (input.Kind == CombatCommandKind.DrawCharacter) return CombatDrawCalculator.Calculate(input);
            if (input.Kind != CombatCommandKind.Attack) return Reject(input, "UnsupportedCommand");
            var admission = CombatAttackQueries.Actor(input, out var originalActor);
            if (admission != AttackFailureReason.None) return Reject(input, admission.ToString());
            var profile = RequiredProfile(input, originalActor);
            var environment = publicPreview ? input.State.Environment.VisibleTo(originalActor.Owner) : input.State.Environment;
            if (input.ObstacleContents.Count > 0 || input.State.Structures.Any(s => ObstacleContentProfile.IsNativeObstacleKey(s.Definition.Key)))
                ObstacleContentProfile.Validate(input.ObstacleContents, input.EnvironmentBindings, input.State.Structures);
            var walls = WallGeometryBuilder.Build(input.State.BoardWidth, input.State.BoardHeight,
                input.State.Environment, input.EnvironmentBindings, input.State.WallBlocking);
            var evaluation = CombatAttackQueries.Selection(input, originalActor, profile, environment: environment, walls: walls);
            if (evaluation.FailureReason != AttackFailureReason.None)
                return Reject(input, evaluation.FailureReason.ToString(), evaluation.BlockedContacts);
            var blockedContacts = new List<AttackBlockedContact>(evaluation.BlockedContacts);
            var selection = evaluation.Selection;
            var selected = selection.ReachableCells;
            var targets = selection.Targets;
            bool pierce = selection.IsPiercing;
            var eligible = selection.EligibleCharacters.ToArray();
            var structures = selection.EligibleStructures.ToArray();
            var direction = AttackRotation(input.Aim, originalActor.Anchor.Value);
            var engine = new CombatEffectEngine();
            var bindings = CombatCommandEffects.Capture(originalActor, profile);
            var cost = engine.EvaluateAttackCost(bindings, new AttackCostEffectContext(originalActor.Stats.AttackCost));
            if (cost.EffectiveCost > originalActor.Stats.CurrentActionCost) return Reject(input, "ActionCostInsufficient", blockedContacts);

            var projected = input.Characters.ToDictionary(c => c.EntityId);
            var finalStructures = input.State.Structures.ToArray();
            var fields = input.State.ThornFields.ToDictionary(f => f.Cell);
            var counts = input.SoulCounts.ToDictionary(p => p.Key, p => p.Value);
            var consumed = new List<long>();
            var events = new List<CombatEvent>();
            var deathAnchors = new Dictionary<EntityId, CellCoord>();
            var projectedStructures = finalStructures.ToDictionary(s => s.EntityId);
            var attackGroups = new CommandAttackGroups();
            var obstacles = new ObstacleReactionCalculator(input, environment, projected, projectedStructures, events, deathAnchors, attackGroups);
            var obstacleHits = new List<(EntityId Id, int Damage, CombatDamageOrigin Origin)>();
            var souls = new SoulEffectCalculator(input.Artifacts);
            souls.RequireSupportedAttackSettlement();
            var soulField = input.Souls;
            var actor = CombatMovementCalculator.SealPendingMovement(originalActor);
            projected[actor.EntityId] = actor;
            bindings = CombatCommandEffects.Capture(actor, profile);
            cost = engine.EvaluateAttackCost(bindings, new AttackCostEffectContext(actor.Stats.AttackCost));
            if (cost.EffectiveCost > actor.Stats.CurrentActionCost) return Reject(input, "ActionCostInsufficient", blockedContacts);
            int count = actor.OwnedId.HasValue ? counts[actor.OwnedId.Value] : 0;
            int commonDamage = engine.EvaluateAttackDamage(bindings, new AttackDamageEffectContext(
                souls.EvaluateAttackPower(actor, count, input.Characters, events))).EffectiveDamage;
            var pipeline = new TargetDamagePipeline(new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault()));
            var potential = new Dictionary<EntityId, int>();
            var normalizationLoss = new Dictionary<EntityId, int>();
            var breakdowns = new Dictionary<EntityId, TargetDamageBreakdown>();
            var rayDetails = new Dictionary<EntityId, List<PierceRayHitPreview>>();
            var roles = targets.ToDictionary(p => p.Key, p => p.Value);
            int attackEventIndex = events.Count;
            if (pierce)
            {
                int budget = PierceTargetBudget(input, actor, profile, commonDamage);
                var hits = PierceTargetCollector.ApplyBudget(selection.PierceTrace, budget);
                roles.Clear();
                foreach (var hit in hits)
                {
                    var decay = engine.EvaluatePierceFalloff(bindings, new PierceFalloffEffectContext(commonDamage, hit.Encounter));
                    var victim = eligible.SingleOrDefault(c => c.EntityId == hit.Id);
                    int scaled = ScaleByTargetDistance(engine, bindings, actor, hit.Id, victim, structures,
                        CategoryMatchupDamageCalculator.Apply(commonDamage, decay.RemainingPercent, 100));
                    var damage = CombatDamageCalculator.Attack(input, engine, pipeline, actor, victim, scaled, AttackTargetRole.Designated,
                        profile.Method.SplashPercent, null, direction, environment);
                    int finalDamage = CombatDamageCalculator.ApplyHeldDefense(attackGroups, projected, hit.Id,
                        damage.FinalDamage, new CombatDamageOrigin(actor.Anchor, actor.EntityId, CommandAttackGroups.Direct), events);
                    potential[hit.Id] = checked(potential.GetValueOrDefault(hit.Id) + finalDamage);
                    breakdowns[hit.Id] = damage;
                    if (!rayDetails.TryGetValue(hit.Id, out var detail)) rayDetails.Add(hit.Id, detail = new List<PierceRayHitPreview>());
                    detail.Add(new PierceRayHitPreview(hit.Ray, hit.Encounter, damage.DamageBeforeMatchup,
                        damage.MatchupNumerator, damage.MatchupDenominator, damage.DamageAfterMatchup,
                        damage.DamageAfterPhysicalDefense, damage.IncomingReduction, finalDamage));
                    roles[hit.Id] = AttackTargetRole.Designated;
                    if (obstacles.HasProfile(hit.Id)) obstacleHits.Add((hit.Id, damage.FinalDamage,
                        new CombatDamageOrigin(actor.Anchor, actor.EntityId, 1)));
                }
            }
            else
                foreach (var target in targets)
                {
                    var victim = eligible.SingleOrDefault(c => c.EntityId == target.Key);
                    int scaled = ScaleByTargetDistance(engine, bindings, actor, target.Key, victim, structures, commonDamage);
                    var damage = CombatDamageCalculator.Attack(input, engine, pipeline, actor, victim, scaled, target.Value,
                        profile.Method.SplashPercent, target.Value == AttackTargetRole.Additional ? selected.PrimaryCell : actor.Anchor,
                        null, environment);
                    int finalDamage = CombatDamageCalculator.ApplyHeldDefense(attackGroups, projected, target.Key,
                        damage.FinalDamage, new CombatDamageOrigin(target.Value == AttackTargetRole.Additional ? selected.PrimaryCell : actor.Anchor,
                            actor.EntityId, CommandAttackGroups.Direct), events);
                    potential.Add(target.Key, finalDamage);
                    breakdowns.Add(target.Key, damage);
                    if (obstacles.HasProfile(target.Key)) obstacleHits.Add((target.Key, damage.FinalDamage,
                        new CombatDamageOrigin(target.Value == AttackTargetRole.Additional ? selected.PrimaryCell : actor.Anchor, actor.EntityId, 1)));
                }
            var directDamageByTarget = potential.ToDictionary(p => p.Key, p => p.Value);
            foreach (var pair in potential)
            {
                if (obstacles.HasProfile(pair.Key)) continue;
                var origins = new[] { new CombatDamageOrigin(
                    !pierce && roles[pair.Key] == AttackTargetRole.Additional ? selected.PrimaryCell : actor.Anchor,
                    actor.EntityId, CommandAttackGroups.Direct) };
                if (projected.TryGetValue(pair.Key, out var victim))
                {
                    projected[pair.Key] = Hurt(victim, pair.Value);
                    CombatCommandEvents.Health(events, victim, projected[pair.Key], origins);
                }
                else
                {
                    obstacles.DamageStructure(pair.Key, pair.Value, origins[0]);
                }
            }

            // 장애물만 타격 단위로 정산한다. 기존 일반 대상의 관통 합산 계약은 유지한다.
            foreach (var hit in obstacleHits)
            {
                obstacles.DamageStructure(hit.Id, hit.Damage, hit.Origin);
                obstacles.ResolveExplosions();
            }
            foreach (var blast in obstacles.BlastDamage)
                if (potential.ContainsKey(blast.Key)) potential[blast.Key] = checked(potential[blast.Key] + blast.Value);
            finalStructures = input.State.Structures.Select(s => projectedStructures[s.EntityId]).ToArray();

            // 모든 일반 피해와 사망을 먼저 고정한 뒤 ID 순서로 밀치기를 처리한다.
            var forcedArrivals = new List<EntityId>();
            var knockback = engine.EvaluateKnockback(bindings);
            int distance = knockback.Contributions.Count == 0 ? 0 : knockback.Contributions.Max(c => c.DistanceCells);
            if (distance > 1) throw new NotSupportedException("현재 밀치기 계약은 한 칸입니다.");
            if (distance > 0)
                foreach (var id in roles.Keys.OrderBy(id => id).ToArray())
                {
                    if (!projected.TryGetValue(id, out var victim) || victim.Health.IsDead) continue;
                    if (victim.PendingMoveActionCost != 0 || victim.MovementCheckpointAnchor != victim.Anchor.Value)
                        throw new InvalidOperationException("밀치기 대상에 미정산 자발적 이동이 있습니다.");
                    var step = ForcedDisplacementGeometry.ResolveBranchStep(actor.Anchor.Value, victim.Anchor.Value);
                    var destination = ForcedDisplacementGeometry.ResolveDestinationAnchor(victim.Anchor.Value, step, distance);
                    if (!EnvironmentTerrainRules.CanKnockback(environment, input.EnvironmentBindings, victim.Anchor.Value, destination)) continue;
                    if (!input.State.IsInside(destination) || projected.Values.Any(c => c.EntityId != id && !c.Health.IsDead && c.OccupiedCells.Contains(destination)) ||
                        finalStructures.Any(s => !s.Health.IsDead && s.OccupiedCells.Contains(destination))) continue;
                    var departed = PositionTileModifierCalculator.Replace(victim, PositionTileModifier.None);
                    PositionTileModifierCalculator.RecordNormalization(events, victim, departed);
                    normalizationLoss[id] = victim.Health.CurrentHealth - departed.Health.CurrentHealth;
                    var consumedFields = new List<ThornFieldCellSnapshot>();
                    ConsumeFields(fields, MovementPathCalculator.ResolvePathCells(victim.Anchor.Value, destination,
                        Footprint(RequiredProfile(input, victim).Definition), victim.Rotation.Value), consumedFields);
                    var origins = consumedFields.Select(f => new CombatDamageOrigin(f.Cell, null, attackGroups.Next())).ToArray();
                    var relocated = Copy(departed, departed.Health, departed.Stats, destination);
                    events.Add(new CombatCharacterEvent(CombatEventKind.Moved, departed, relocated));
                    projected[id] = relocated;
                    int thornDamage = 0;
                    for (int j = 0; j < consumedFields.Count; j++)
                        thornDamage = checked(thornDamage + CombatDamageCalculator.ApplyHeldDefense(attackGroups, projected, id,
                            CombatDamageCalculator.Environment(input, consumedFields[j].ThornDamage, origins[j], destination, environment).FinalDamage,
                            origins[j], events));
                    relocated = projected[id];
                    projected[id] = Hurt(relocated, thornDamage);
                    CombatCommandEvents.Health(events, relocated, projected[id], origins);
                    potential[id] = checked(potential.GetValueOrDefault(id) + thornDamage);
                    if (projected[id].Health.IsDead) deathAnchors[id] = destination;
                    else forcedArrivals.Add(id);
                }

            if (selected.PrimaryCell.HasValue)
                foreach (var contribution in engine.EvaluateThornFieldApplication(bindings,
                    new ThornFieldApplicationEffectContext(selected.PrimaryCell.Value)).Contributions)
                {
                    var propagation = AttackOcclusionResolver.ResolvePropagationCells(selected.PrimaryCell.Value,
                        contribution.ApplicationCells.Where(input.State.IsInside).ToArray(), walls, profile.Occlusion);
                    blockedContacts.AddRange(propagation.BlockedContacts);
                    foreach (var cell in propagation.Cells)
                    {
                        var occupant = input.Characters.SingleOrDefault(c => c.Area == CharacterRuntimeArea.Field && !c.Health.IsDead && c.OccupiedCells.Contains(cell));
                        // 피격 자격과 무관하게 살아 있는 구조물은 지형 생성 칸을 점유한다.
                        var structure = input.State.Structures.SingleOrDefault(s => !s.Health.IsDead && s.OccupiedCells.Contains(cell));
                        if (structure != null) { fields.Remove(cell); continue; }
                        if (occupant != null)
                        {
                            var beforeThorn = projected[occupant.EntityId];
                            // 공격 직후의 가시 피해와 피격 결과도 일반 공격의 대상 자격을 따른다.
                            if (!AttackTargetEligibility.CanTargetCharacter(actor, beforeThorn, selection.Policy)) continue;
                            var origin = new CombatDamageOrigin(cell, actor.EntityId, attackGroups.Next());
                            int thornDamage = CombatDamageCalculator.Environment(input, contribution.ThornDamage, origin,
                                beforeThorn.Anchor, environment).FinalDamage;
                            thornDamage = CombatDamageCalculator.ApplyHeldDefense(attackGroups, projected, occupant.EntityId, thornDamage, origin, events);
                            beforeThorn = projected[occupant.EntityId];
                            projected[occupant.EntityId] = Hurt(beforeThorn, thornDamage);
                            CombatCommandEvents.Health(events, beforeThorn, projected[occupant.EntityId], new[] { origin });
                            potential[occupant.EntityId] = checked(potential.GetValueOrDefault(occupant.EntityId) + thornDamage);
                            if (!roles.ContainsKey(occupant.EntityId)) roles.Add(occupant.EntityId, AttackTargetRole.Additional);
                        }
                        else if (fields.TryGetValue(cell, out var existing))
                            fields[cell] = new ThornFieldCellSnapshot(cell, Math.Max(existing.RemainingUnitTurns, contribution.LifetimeUnitTurns), existing.ThornDamage);
                        else fields.Add(cell, new ThornFieldCellSnapshot(cell, contribution.LifetimeUnitTurns, contribution.ThornDamage));
                    }
                }

            var traitActivations = new List<TraitActivationSnapshot>();
            var categoryActivations = new List<CategoryActivationSnapshot>();
            var success = engine.PlanAttackSucceeded(bindings, new AttackSucceededEffectContext(cost.PositiveCostContributors.ToArray(), direction));
            var stats = CombatCommandEffects.Apply(actor.Stats, success);
            CombatCommandEffects.AppendActivations(success, traitActivations, categoryActivations);
            CombatCommandEvents.Effects(events, success);
            int afterCost = checked(stats.CurrentActionCost - cost.EffectiveCost);
            var refund = engine.EvaluateAttackCostRefund(bindings, new AttackActionCostRefundEffectContext(originalActor.AttackRefundEligibleMoveCost),
                checked(stats.MaxActionCost - afterCost));
            var refundPlan = engine.PlanAttackCostRefundApplied(bindings, refund);
            stats = CombatCommandEffects.Apply(stats, refundPlan);
            CombatCommandEffects.AppendActivations(refundPlan, traitActivations, categoryActivations);
            CombatCommandEvents.Effects(events, refundPlan);
            projected[actor.EntityId] = Copy(projected[actor.EntityId], projected[actor.EntityId].Health,
                CombatCommandEffects.With(stats, ap: checked(afterCost + refund.EffectiveRefund)));
            var settledActor = projected[actor.EntityId];
            projected[actor.EntityId] = CombatMovementCalculator.CopyMovement(settledActor, settledActor.Stats,
                settledActor.Anchor, settledActor.CommittedMoveDistance, 0, 0, 0, settledActor.MovementCheckpointAnchor);
            // 피격 결과는 피해 정산 시점에 고정한다. 이후 도착 회복이 피해량을 되돌리지 않는다.
            var hitHealth = roles.Keys.ToDictionary(id => id, id => projected.TryGetValue(id, out var c)
                ? c.Health : finalStructures.Single(s => s.EntityId == id).Health);
            var arrivalProjection = new MovementProjection(input, input.State, projected, projectedStructures,
                fields, counts, soulField, consumed, events, deathAnchors, souls, attackGroups, environment);
            foreach (var id in forcedArrivals.OrderBy(id => id))
            {
                if (projected[id].Health.IsDead || arrivalProjection.Terminal) continue;
                MovementEnvironmentCalculator.ResolveObjectsAt(arrivalProjection, id, projected[id].Anchor.Value, rules);
                arrivalProjection.ResolveForcedArrival(id);
            }
            // 처치 연쇄 공격은 강제 도착 뒤에 시작한다. 기본 공격 결과는 위에서 고정했으며 미리보기에는 넣지 않는다.
            if (!publicPreview)
                KillChainAttackCalculator.Resolve(input, engine, pipeline, bindings, commonDamage, arrivalProjection,
                    projected, attackGroups, deathAnchors);
            var results = roles.OrderBy(p => p.Key).Select(pair =>
            {
                var before = input.Characters.SingleOrDefault(c => c.EntityId == pair.Key)?.Health ?? structures.Single(s => s.EntityId == pair.Key).Health;
                var after = hitHealth[pair.Key];
                return new AttackHitResult(pair.Key, before.CurrentHealth, after.CurrentHealth, pair.Value,
                    pair.Value == AttackTargetRole.Additional ? profile.Method.SplashPercent : 100,
                    normalizationLoss.GetValueOrDefault(pair.Key));
            }).ToArray();
            var primary = roles.Where(p => p.Value == AttackTargetRole.Primary).Select(p => p.Key).DefaultIfEmpty(EntityId.Empty).Single();
            var result = AttackResult.FromFacts(actor.EntityId, primary, direction,
                new ActionCostSettlementSnapshot(originalActor.Stats.CurrentActionCost, cost.EffectiveCost, refund.EffectiveRefund,
                    projected[actor.EntityId].Stats.CurrentActionCost),
                selected.DesignatedCells.ToArray().Concat(selected.AdditionalCells.ToArray()).Distinct().OrderBy(c => c.Y).ThenBy(c => c.X).ToArray(),
                results, selection.Cells, traitActivations: traitActivations.ToArray(), categoryActivations: categoryActivations.ToArray(),
                blockedContacts: blockedContacts);
            var targetPreviews = results.Select(hit =>
            {
                var original = input.Characters.SingleOrDefault(c => c.EntityId == hit.TargetId);
                var occupied = original?.OccupiedCells ?? structures.Single(s => s.EntityId == hit.TargetId).OccupiedCells;
                var damage = breakdowns.GetValueOrDefault(hit.TargetId, TargetDamageBreakdown.CreatePassthrough(0));
                var rays = rayDetails.TryGetValue(hit.TargetId, out var details) && details.Count > 1
                    ? details.ToArray() : Array.Empty<PierceRayHitPreview>();
                int expectedDamage = potential.GetValueOrDefault(hit.TargetId);
                int directDamage = directDamageByTarget.GetValueOrDefault(hit.TargetId);
                return new AttackTargetPreview(hit.TargetId, expectedDamage, hit.TargetHealthBefore,
                    occupied.ToArray(), rays.Length > 1 ? checked(rays.Sum(r => r.DamageBeforeMatchup)) : damage.DamageBeforeMatchup,
                    damage.MatchupNumerator, damage.MatchupDenominator,
                    rays.Length > 1 ? checked(rays.Sum(r => r.IncomingReduction)) : damage.IncomingReduction,
                    rays, hit.Role, hit.AppliedSplashPercent, hit.HealthNormalizationLoss,
                    secondaryDamage: checked(expectedDamage - directDamage));
            }).ToArray();
            preview = AttackPreview.FromFacts(result, targetPreviews, projected[actor.EntityId].Stats.TraitStatuses.ToArray(),
                projected[actor.EntityId].Stats.Category);
            events.Insert(attackEventIndex, new CombatAttackEvent(result));
            return arrivalProjection.Complete(result);
        }

        internal static CombatCommandTransition Finish(CombatCommandInput input,
            Dictionary<EntityId, CharacterCombatFacts> projected, Dictionary<CharacterInstanceId, int> counts,
            SoulFieldSnapshot soulField, Dictionary<CellCoord, ThornFieldCellSnapshot> fields,
            StructureCombatFacts[] structures, List<long> consumed, Dictionary<EntityId, CellCoord> deathAnchors,
            SoulEffectCalculator souls, AttackResult? attack, CombatCommandState proposedState = null, bool countdown = false,
            List<CombatEvent> events = null, ISet<EntityId> resolvedDeaths = null,
            EnvironmentSemanticBindings environmentBindings = null)
        {
            events ??= new List<CombatEvent>();
            var sourceState = proposedState ?? input.State;
            var environment = sourceState.Environment;
            var progress = sourceState.EnvironmentProgress;
            environmentBindings ??= input.EnvironmentBindings;
            FlagDropCalculator.ResolveDeaths(input, projected, structures, deathAnchors,
                ref environment, ref environmentBindings, ref progress, events);
            var outcome = BattleOutcomeCalculator.Resolve(projected.Values.ToArray(), structures);
            var main = input.Characters.Single(c => c.OwnershipRole == CharacterOwnershipRole.Main);
            var finalMain = projected[main.EntityId];
            if (outcome.IsStageEnded)
            {
                foreach (var c in projected.Values.OrderBy(c => c.EntityId).ToArray())
                {
                    var cleared = HeldEnvironmentEffects.ClearForBattleEnd(c);
                    if (ReferenceEquals(c, cleared)) continue;
                    projected[c.EntityId] = cleared;
                    events.Add(new CombatHeldEnvironmentEvent(c, cleared));
                }
                finalMain = projected[main.EntityId];
                var terminal = new BattleCompletionCalculator(input.Artifacts).PlanTerminalHealth(finalMain,
                    CharacterSettlementCalculator.WithHealth(input.Main, finalMain.Health.CurrentHealth), outcome, events);
                var beforeTerminal = finalMain;
                projected[main.EntityId] = finalMain = terminal.Characters[0].Final;
                CombatCommandEvents.Health(events, beforeTerminal, finalMain);
            }
            var changes = input.Characters.Where(c => !ReferenceEquals(c, projected[c.EntityId])).OrderBy(c => c.EntityId)
                .Select(c => new CharacterStateChange(c, projected[c.EntityId])).ToArray();
            var persistent = new MainCharacterPersistentChange(input.Main,
                CharacterSettlementCalculator.WithHealth(input.Main, finalMain.Health.CurrentHealth));
            var characterChanges = new CharacterChanges(changes, counts.Where(p => p.Value != input.SoulCounts[p.Key])
                .Select(p => new SoulCountChange(p.Key, input.SoulCounts[p.Key], p.Value)).ToArray(), persistent);
            var deaths = changes.Where(c => !c.Expected.Health.IsDead && c.Final.Health.IsDead &&
                (resolvedDeaths == null || !resolvedDeaths.Contains(c.Final.EntityId)))
                .Select(c => new SoulDeathFacts(c, deathAnchors.GetValueOrDefault(c.Final.EntityId,
                    c.Expected.Anchor ?? c.Expected.MovementCheckpointAnchor))).ToArray();
            var deathField = souls.PlanDeaths(soulField, deaths, input.State.IsNight, countdown);
            if (!deathField.Expected.SameValues(deathField.Final)) events.Add(new CombatSoulEvent(deathField));
            soulField = deathField.Final;
            var dead = new HashSet<EntityId>(projected.Values.Where(c => c.Health.IsDead).Select(c => c.EntityId));
            bool actorDead = dead.Contains(input.ActorId);
            var turn = sourceState.Turn;
            turn = new CombatTurnFacts(turn.StagePhase, turn.PreparationIndex, turn.MainPlacementLocked, turn.CurrentTime,
                turn.CurrentNightTime, turn.CompletedUnitTurnCount, turn.Participants.Where(p => !dead.Contains(p.EntityId))
                    .Select(p => new CombatTurnParticipant(p.EntityId,
                        projected.TryGetValue(p.EntityId, out var participant) ? HeldEnvironmentEffects.EffectivePriority(participant) : p.Priority, p.Delay,
                        p.NextActionTime, p.OpeningTurnPending, p.CompletedTurnCount)).ToArray(),
                turn.EnemyOrigins, turn.Gimmicks);
            var state = new CombatCommandState(input.State.BoardWidth, input.State.BoardHeight,
                outcome.IsStageEnded ? CombatFlowPhase.Finished : actorDead && proposedState == null ? CombatFlowPhase.Automatic : sourceState.Phase,
                actorDead || outcome.IsStageEnded ? EntityId.Empty : sourceState.ActiveActorId, sourceState.IsNight, structures,
                fields.Values.OrderBy(f => f.Cell.Y).ThenBy(f => f.Cell.X).ToArray(),
                sourceState.Timeline.Where(t => !t.UnitId.HasValue || !dead.Contains(t.UnitId.Value)).Select(t =>
                    !t.UnitId.HasValue || !projected.TryGetValue(t.UnitId.Value, out var participant) ? t : t.ProjectedNightTimeAfterTurn.HasValue ?
                    CombatTimelineEntrySnapshot.ForUnit(t.UnitId.Value, HeldEnvironmentEffects.EffectivePriority(participant),
                        t.ScheduledTime, t.IsActive, t.ProjectedNightTimeAfterTurn.Value, t.EndsNightAfterTurn) :
                    CombatTimelineEntrySnapshot.ForUnit(t.UnitId.Value, HeldEnvironmentEffects.EffectivePriority(participant),
                        t.ScheduledTime, t.IsActive)).ToArray(), turn, environment, progress, sourceState.WallBlocking);
            if (outcome.IsStageEnded) events.Add(new CombatOutcomeEvent(outcome));
            return new CombatCommandTransition(input, true, "", characterChanges,
                new SoulFieldChange(input.Souls, soulField), state, outcome, attack, consumed, events: events,
                environmentBindings: input.EnvironmentBindings.SameValues(environmentBindings) ? null :
                    new EnvironmentBindingsChange(input.EnvironmentBindings, environmentBindings), blockedContacts: attack?.BlockedContacts);
        }

        internal static void Collect(CombatCommandInput input, SoulEffectCalculator souls,
            Dictionary<EntityId, CharacterCombatFacts> projected, Dictionary<CharacterInstanceId, int> counts,
            CharacterCombatFacts actor, SoulArrivalKind arrivalKind, ref SoulFieldSnapshot field, List<long> consumed, List<CombatEvent> events = null)
        {
            var owned = input.Owned.Single(o => o.Id == actor.OwnedId.Value);
            var main = actor.OwnershipRole == CharacterOwnershipRole.Main
                ? CharacterSettlementCalculator.WithHealth(input.Main, actor.Health.CurrentHealth) : null;
            int baseMax = main?.PersistentMaxHealth ?? RequiredProfile(input, actor).Definition.MaxHealth;
            var collected = souls.Collect(new SoulCollectorInput(actor, owned, counts[owned.Id], baseMax, main, arrivalKind), field, true);
            if (collected.ConsumedSoulIds.Count != 0) events?.Add(new CombatSoulEvent(collected.Field, actor.EntityId, collected));
            foreach (var change in collected.Characters.Characters) projected[change.Final.EntityId] = change.Final;
            foreach (var change in collected.Characters.Souls) counts[change.Id] = change.FinalCount;
            field = collected.Field.Final;
            consumed.AddRange(collected.ConsumedSoulIds);
        }

        internal static int ConsumeFields(Dictionary<CellCoord, ThornFieldCellSnapshot> fields, CellCoord[] visited,
            List<ThornFieldCellSnapshot> consumed = null)
        {
            int damage = 0;
            foreach (var cell in visited)
                if (fields.Remove(cell, out var field))
                {
                    consumed?.Add(field);
                    damage = checked(damage + field.ThornDamage);
                }
            return damage;
        }

        /// <summary>실행과 AI 목표 판정이 같은 관통 제한을 사용한다. AP·피해 적용·도착 효과는 계산하지 않는다.</summary>
        internal static int PierceTargetBudget(CombatCommandInput input, CharacterCombatFacts actor,
            CombatAttackProfile profile, int? commonDamage = null)
        {
            var engine = new CombatEffectEngine();
            var bindings = CombatCommandEffects.Capture(actor, profile);
            int damage = commonDamage ?? engine.EvaluateAttackDamage(bindings, new AttackDamageEffectContext(
                new SoulEffectCalculator(input.Artifacts).EvaluateAttackPower(actor,
                    actor.OwnedId.HasValue ? input.SoulCounts[actor.OwnedId.Value] : 0, input.Characters))).EffectiveDamage;
            var falloff = engine.EvaluatePierceFalloff(bindings, new PierceFalloffEffectContext(damage, 0));
            return falloff.TargetBudget ?? int.MaxValue;
        }

        /// <summary>공격자와 대상 사이 체비셰프 거리의 배율을 대상 피해에 적용한다. 반올림은 상성 배율과 같은 half-up이다.</summary>
        private static int ScaleByTargetDistance(CombatEffectEngine engine, EffectRuleBinding[] bindings,
            CharacterCombatFacts actor, EntityId targetId, CharacterCombatFacts victim, StructureCombatFacts[] structures, int damage)
        {
            var origin = actor.Anchor.Value;
            var cells = victim?.OccupiedCells ?? structures.Single(s => s.EntityId == targetId).OccupiedCells;
            int distance = cells.Min(c => checked((int)Math.Max(Math.Abs((long)c.X - origin.X), Math.Abs((long)c.Y - origin.Y))));
            var scale = engine.EvaluateTargetDistanceDamage(bindings, new TargetDistanceDamageEffectContext(distance));
            return scale.HasContribution ? CategoryMatchupDamageCalculator.Apply(damage, scale.Numerator, scale.Denominator) : damage;
        }

        internal static CombatAttackProfile RequiredProfile(CombatCommandInput input, CharacterCombatFacts character) =>
            input.Profiles.SingleOrDefault(p => p.Definition.Key == character.CharacterKey) ??
            throw new InvalidOperationException("명시적으로 연결한 지정 방식과 유닛 정의가 필요합니다.");
        private static TileFootprint Footprint(UnitSpec spec) => new(spec.FootprintWidth, spec.FootprintHeight, spec.FootprintLocalCells.ToArray());
        internal static CharacterCombatFacts Hurt(CharacterCombatFacts c, int damage) => Copy(c,
            new HealthStateSnapshot(Math.Max(0, c.Health.CurrentHealth - Math.Min(c.Health.CurrentHealth, damage)), c.Health.MaxHealth), c.Stats);
        internal static CombatCommandTransition Reject(CombatCommandInput input, string reason,
            IReadOnlyList<AttackBlockedContact> blockedContacts = null) => new(input, false, reason,
            new CharacterChanges(Array.Empty<CharacterStateChange>(), Array.Empty<SoulCountChange>()), new SoulFieldChange(input.Souls, input.Souls),
            input.State, new StageOutcomeSnapshot(StageOutcome.InProgress, StageOutcomeReason.None), blockedContacts: blockedContacts);

        internal static CharacterCombatFacts Copy(CharacterCombatFacts c, HealthStateSnapshot health,
            UnitStatsSnapshot stats, CellCoord? anchor = null)
        {
            bool dead = health.IsDead;
            var position = anchor ?? c.Anchor ?? c.MovementCheckpointAnchor;
            return new CharacterCombatFacts(c.EntityId, c.OwnedId, c.CharacterKey, c.Owner, c.OwnershipRole,
                dead ? CharacterRuntimeArea.Excluded : c.Area, health, stats, dead ? null : c.ReserveSlotIndex,
                dead ? null : c.Anchor.HasValue ? position : null, dead ? null : c.Rotation,
                dead ? Array.Empty<CellCoord>() : c.Anchor.HasValue ? new[] { position } : c.OccupiedCells,
                anchor.HasValue ? position : c.MovementCheckpointAnchor,
                c.CommittedMoveDistance, c.PendingMoveActionCost,
                c.TemporaryAttackBonus, c.TemporaryMaxHealthBonus,
                c.PendingMoveDistance, c.AttackRefundEligibleMoveCost, c.PositionModifier, c.HeldEnvironment);
        }

        internal static Rotation4 AttackRotation(AttackAim aim, CellCoord origin)
        {
            if (aim.Kind == AttackAimKind.Direction)
                return aim.Direction == AttackDirection.Right ? Rotation4.R90 : aim.Direction == AttackDirection.Down ? Rotation4.R180 :
                    aim.Direction == AttackDirection.Left ? Rotation4.R270 : Rotation4.R0;
            if (!aim.Cell.HasValue) return Rotation4.R0;
            long x = (long)aim.Cell.Value.X - origin.X, y = (long)aim.Cell.Value.Y - origin.Y;
            return Math.Abs(x) >= Math.Abs(y) ? x > 0 ? Rotation4.R90 : x < 0 ? Rotation4.R270 : Rotation4.R0 : y > 0 ? Rotation4.R0 : Rotation4.R180;
        }
    }
}
