using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items;
using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Contexts;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 공격 명령의 입장·행동 사용·완료·게시 경계를 소유한다. 공통 전달 순서는 AttackDeliveryExecutor가 가진다.
    /// 아래 순서가 설계 2026-10-12 §4의 불변식 I-1~I-7이므로 순서를 바꾸려면 동작 변경으로 다룬다.
    /// </summary>
    internal static class CombatAttackCalculator
    {
        internal static CombatCommandTransition Calculate(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            bool publicPreview, out AttackPreview preview)
        {
            using var work = new CombatAttackCalculation(input, rules, publicPreview);
            while (!work.IsComplete) work.Step();
            preview = work.Preview;
            return work.Transition;
        }

        internal static CombatCommandTransition Reaction(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            CharacterCombatFacts attacker, AttackAim aim) => Reaction(input, rules, attacker, aim, out _);

        /// <summary>반응 공격도 같은 계산을 끝까지 진행하며 대상이 없으면 null과 표시 범위를 돌려준다.</summary>
        internal static CombatCommandTransition Reaction(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            CharacterCombatFacts attacker, AttackAim aim, out IReadOnlyList<CellCoord> emptyAreaCells)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            using var work = new CombatAttackCalculation(input, rules, false, attacker, aim);
            while (!work.IsComplete) work.Step();
            emptyAreaCells = work.EmptyAreaCells;
            return work.Transition;
        }

        internal static IEnumerable<string> Run(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            bool publicPreview, CharacterCombatFacts reactionActor, AttackAim? reactionAim,
            Action<CombatCommandTransition, AttackPreview, IReadOnlyList<CellCoord>> completed, Owner? previewObserver = null)
        {
            if (reactionActor == null) MovementEnvironmentCalculator.RequireRules(rules);
            yield return "Rules";
            bool reaction = reactionActor != null;
            var originalActor = reactionActor;
            if (reaction)
            {
                if (CombatActionAdmission.IsAttackRestricted(originalActor))
                {
                    completed(null, default, Array.Empty<CellCoord>());
                    yield break;
                }
            }
            else
            {
                var admission = CombatAttackQueries.Actor(input, out originalActor);
                if (admission != AttackFailureReason.None)
                {
                    completed(CombatCommandCalculator.Reject(input, admission.ToString()), default, Array.Empty<CellCoord>());
                    yield break;
                }
            }
            yield return "Admission";
            var aim = reactionAim ?? input.Aim;
            var profile = CombatCommandCalculator.RequiredProfile(input, originalActor);
            yield return "Profile";
            var environment = input.State.Environment;
            // 공개 미리보기를 보는 진영이다. 기본은 공격자 진영이고, 포효 예고만 플레이어를 넘긴다(포효 예고 계획 TP-2).
            var observer = previewObserver ?? originalActor.Owner;
            if (publicPreview)
                foreach (var phase in environment.VisibleSteps(observer, value => environment = value))
                    yield return phase;
            yield return "Environment";
            WallGeometry walls = null;
            foreach (var phase in WallSteps(input, value => walls = value)) yield return phase;
            AttackElevation elevation = null;
            foreach (var phase in AttackElevation.BuildSteps(environment, input.EnvironmentBindings, value => elevation = value))
                yield return phase;
            AttackSelectionEvaluation evaluation = null;
            foreach (var phase in CombatAttackQueries.SelectionSteps(input, originalActor, profile, aim, environment, walls,
                elevation, value => evaluation = value)) yield return phase;
            if (evaluation.FailureReason != AttackFailureReason.None)
            {
                completed(reaction ? null : CombatCommandCalculator.Reject(input, evaluation.FailureReason.ToString(),
                    evaluation.BlockedContacts), default, evaluation.EmptyAreaCells);
                yield break;
            }
            foreach (var phase in ExecuteSteps(input, rules, originalActor, profile, aim, environment, walls,
                elevation, evaluation, new CombatEffectEngine(), publicPreview, reaction, completed, observer))
                yield return phase;
        }

        private static IEnumerable<string> WallSteps(CombatCommandInput input, Action<WallGeometry> completed)
        {
            using (CoreProfiling.Sample(CoreProfileSection.AttackGeometry))
                if (input.ObstacleContents.Count > 0 || input.State.Structures.Any(s => ObstacleContentProfile.IsNativeObstacleKey(s.Definition.Key)))
                    ObstacleContentProfile.Validate(input.ObstacleContents, input.EnvironmentBindings, input.State.Structures);
            yield return "AttackObstacleValidation";
            foreach (var phase in WallGeometryBuilder.BuildSteps(input.State.BoardWidth, input.State.BoardHeight,
                input.State.Environment, input.EnvironmentBindings, input.State.WallBlocking, completed))
                yield return phase;
        }

        private static IEnumerable<string> ExecuteSteps(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            CharacterCombatFacts originalActor, CombatAttackProfile profile, AttackAim aim, MovementEnvironmentFacts environment,
            WallGeometry walls, AttackElevation elevation, AttackSelectionEvaluation evaluation, CombatEffectEngine engine,
            bool publicPreview, bool reaction, Action<CombatCommandTransition, AttackPreview, IReadOnlyList<CellCoord>> completed,
            Owner observer)
        {
            var direction = CombatCommandCalculator.AttackRotation(aim, originalActor.Anchor.Value);
            // S2~S4: 명령 투영, 공통 피해와 전달을 준비한다.
            CommandAttackProjection world;
            using (var construction = CommandAttackProjection.BeginCreate(input, environment, evaluation.BlockedContacts, captureMovementUses: !publicPreview))
            {
                while (!construction.IsComplete)
                {
                    construction.Step();
                    yield return "Projection";
                }
                world = construction.Result;
            }
            var actor = originalActor;
            world.Characters[actor.EntityId] = actor;
            var bindings = CombatCommandEffects.Capture(actor, profile);
            var traitScope = new AttackTraitScope(actor, capturedEnemyCount: AttackTraitScope.CountEnemies(actor, evaluation.Selection));
            int actionEventStart = world.Events.Count;
            TraitAttackNotifications.Apply(input, world, traitScope);
            actor = world.Characters[actor.EntityId];
            yield return "AttackNotifications";
            bool requiresPreHitMovement = evaluation.Pull?.RequiresPull == true;
            int commonDamage = requiresPreHitMovement ? 0 : CommonAttackDamage(input, actor, profile, world.Events, traitScope);
            yield return "DeliveryCommonDamage";
            ResolvedAttackDelivery delivery = null;
            if (requiresPreHitMovement)
                delivery = new ResolvedAttackDelivery(Array.Empty<DeliveredAttackHit>(), Array.Empty<CellCoord>());
            else
                foreach (var phase in CombatAttackDelivery.ResolveSteps(input, traitScope.ForDamage(actor), profile,
                    evaluation.Selection, commonDamage, walls, elevation, value => delivery = value)) yield return phase;
            foreach (var contact in delivery.BlockedContacts)
            {
                world.BlockedContacts.Add(contact);
                yield return "BlockedContact";
            }
            var plan = new CommandAttackPlan(input,rules,profile,originalActor,actor,environment,walls,evaluation.Selection,                delivery,engine,bindings,new TargetDamagePipeline(new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault())),                commonDamage,direction,publicPreview,                reaction ? CombatDamageKind.ReactionAttack : CombatDamageKind.OrdinaryAttack,aim: aim,traitScope: traitScope,
                previewObserver: observer);
            var ledger = new CommandAttackLedger();
            ledger.BeginScope(plan, world);
            yield return "PlanLedger";
            MovementProjection arrival = null;
            if (HunterApplication.HasHunters(world.Characters.Values))
            {
                arrival = world.CreateArrivalProjection(plan);
                using var observation = arrival.BeginCurrentInput();
                while (!observation.IsComplete)
                {
                    observation.Step();
                    yield return "HunterObservation";
                }
                HunterApplication.Reconcile(observation.Result, world.Characters, arrival.ChooseRandom, world.Events);
                yield return "HunterReconcile";
            }
            AttackPullSnapshot pullMovement = null;
            bool deliverDirectDamage = true;
            if (requiresPreHitMovement)
            {
                arrival ??= world.CreateArrivalProjection(plan);
                foreach (var phase in PullAttackCalculator.ExecuteSteps(evaluation.Pull, plan, world, ledger, arrival,
                    (canDeliver, movement) => { deliverDirectDamage = canDeliver; pullMovement = movement; }))
                    yield return phase;
                if (deliverDirectDamage)
                    foreach (var phase in PullAttackCalculator.RebuildDeliverySteps(plan, world, arrival, evaluation.Pull.TargetId,
                        rebuilt => plan = rebuilt))
                        yield return phase;
                yield return "Pull";
            }
            // 당김과 공통 공격력 정산 뒤, 실제 발사부터 피해 결과를 기록합니다.
            int attackEventIndex = world.Events.Count;

            var deliveredVictims = new HashSet<EntityId>();
            var traits = Array.Empty<TraitActivationSnapshot>();
            var categories = Array.Empty<CategoryActivationSnapshot>();
            // 행동 사용 정산은 명령이 소유하고, 실행기는 기존 단계 경계에서 한 번 호출한다.
            Action settleActionUse = reaction ? null : () =>
            {
                (traits, categories) = SettleActionUse(plan, world);
            };
            foreach (var phase in AttackDeliveryExecutor.ExecuteSteps(plan, world, ledger, deliveredVictims,
                value => arrival = value, arrival, settleActionUse, deliverDirectDamage))
                yield return phase;

            CombatDebuffApplication.CompleteAttack(input, originalActor, deliveredVictims, world.Characters, world.Events,
                new CombatDamageOrigin(plan.Actor.Anchor, plan.Actor.EntityId, plan.AttackGroup));
            yield return "CompleteDebuff";
            if (!reaction) ApplyAttackCompletedItem(plan, world, arrival);
            yield return "CompleteItem";

            var settlement = reaction
                ? new UnitActionUseSettlementSnapshot(actor.EntityId, actor.ActionUse, actor.ActionUse)
                : CombatActionAdmission.Settlement(originalActor, UnitActionUse.Attacked);
            if (!reaction) arrival.SetActionUseSettlement(settlement);
            var traitRows = new List<TraitActivationSnapshot>();
            var categoryRows = new List<CategoryActivationSnapshot>();
            for (int i = actionEventStart; i < world.Events.Count; i++)
            {
                if (world.Events[i] is CombatEffectsEvent effects)
                {
                    foreach (var value in effects.Traits)
                    {
                        if (value.ResultOrigin == null || value.ResultOrigin.AttackGroup == plan.AttackGroup &&
                            value.ResultOrigin.CreditedCharacterId == actor.EntityId)
                            traitRows.Add(value);
                        yield return "TraitEvidence";
                    }
                    foreach (var value in effects.Categories)
                    {
                        categoryRows.Add(value);
                        yield return "CategoryEvidence";
                    }
                }
                yield return "EventScan";
            }
            AttackResult result;
            AttackPreview preview;
            using (var resultWork = AttackResultFactory.BeginCreate(plan, world, ledger, settlement,
                traitRows.ToArray(), categoryRows.ToArray(), reaction, pullMovement))
            {
                while (!resultWork.IsComplete)
                {
                    resultWork.Step();
                    yield return "Result";
                }
                result = resultWork.Result;
                preview = resultWork.Preview;
            }
            if (reaction) world.Events.Insert(attackEventIndex, new CombatRoarEvent(actor.EntityId, result, result.AffectedCells.ToArray()));
            else world.Events.Insert(attackEventIndex, new CombatAttackEvent(result));
            yield return "AttackEvent";
            using var finalization = arrival.BeginComplete(reaction ? null : result);
            while (!finalization.IsComplete)
            {
                finalization.Step();
                yield return finalization.Phase;
            }
            completed(finalization.Result, preview, evaluation.EmptyAreaCells);
        }

        private static void ApplyAttackCompletedItem(CommandAttackPlan plan, CommandAttackProjection world, MovementProjection arrival)
        {
            var holder = world.Characters[plan.Actor.EntityId];
            var definition = CombatItemResolver.EquippedDefinition(plan.Input.ItemDefinitions, holder);
            if (definition == null) return;
            var decision = CombatItemResolver.AfterOrdinaryAttack(definition, new ItemAttackContext(holder,
                plan.Actor.Anchor.Value, world.ItemDirectHits.Values.ToArray(), world.Characters.Values.ToArray(),
                arrival.ItemRandomState));
            arrival.ItemRandomState = decision.FinalRandomState ?? arrival.ItemRandomState;
            if (!decision.Activated) return;
            var target = world.Characters[decision.TargetId.Value];
            var changes = ItemCharacterChanges.ApplyDecision(holder, target, decision, out bool added);
            foreach (var change in changes) world.Characters[change.Final.EntityId] = change.Final;
            ItemCharacterChanges.AppendDecisionEvent(world.Events, definition, holder.EntityId, decision,
                ItemEventStage.AttackCompleted, null,
                CombatEffectiveStatsCalculator.ItemReduction(target, world.Characters[target.EntityId], decision.TemporaryEffect.Stat), added);
        }

        /// <summary>실행과 AI 목표 판정이 같은 관통 제한을 사용한다. AP·피해 적용·도착 효과는 계산하지 않는다.</summary>
        internal static int CommonAttackDamage(CombatCommandInput input, CharacterCombatFacts actor,
            CombatAttackProfile profile, List<CombatEvent> events = null, AttackTraitScope traitScope = null)
        {
            using var sample = CoreProfiling.Sample(CoreProfileSection.AttackCommonDamage);
            actor = traitScope?.ForDamage(actor) ?? actor;
            var engine = new CombatEffectEngine();
            var bindings = CombatCommandEffects.Capture(actor, profile);
            int damage = engine.EvaluateAttackDamage(bindings, new AttackDamageEffectContext(
                new SoulEffectCalculator(input.Artifacts).EvaluateAttackPower(actor,
                    actor.OwnedId.HasValue ? input.SoulCounts[actor.OwnedId.Value] : 0, input.Characters, events))).EffectiveDamage;
            return CombatDebuffCalculator.EvaluateAttackPower(damage, actor.Debuffs);
        }

        /// <summary>S10: 공격 성공 효과를 적용하고 일반 행동을 한 번 사용한다. 행동 사용 증거는 도착 투영의 최종 전이에서 게시한다.</summary>
        private static (TraitActivationSnapshot[] Traits, CategoryActivationSnapshot[] Categories) SettleActionUse(
            CommandAttackPlan plan, CommandAttackProjection world)
        {
            var engine = plan.Engine;
            var actorId = plan.Actor.EntityId;
            var traitActivations = new List<TraitActivationSnapshot>();
            var categoryActivations = new List<CategoryActivationSnapshot>();
            var success = engine.PlanAttackSucceeded(plan.Bindings, new AttackSucceededEffectContext(plan.Direction));
            var stats = CombatCommandEffects.Apply(world.Characters[actorId].Stats, success);
            CombatCommandEffects.AppendActivations(success, traitActivations, categoryActivations);
            CombatCommandEvents.Effects(world.Events, success);
            var current = world.Characters[actorId];
            world.Characters[actorId] = CombatCommandCalculator.Copy(current, current.Health, stats)
                .With(actionUse: UnitActionUse.Attacked);
            return (traitActivations.ToArray(), categoryActivations.ToArray());
        }
    }
}
