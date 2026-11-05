using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat.Items;
using System;
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
            preview = default;
            // S1: 공격자와 지정을 검사한다. 실패하면 투영을 만들지 않는다.
            var admission = CombatAttackQueries.Actor(input, out var originalActor);
            if (admission != AttackFailureReason.None)
                return CombatCommandCalculator.Reject(input, admission.ToString());
            var profile = CombatCommandCalculator.RequiredProfile(input, originalActor);
            var environment = publicPreview ? input.State.Environment.VisibleTo(originalActor.Owner) : input.State.Environment;
            var walls = Walls(input);
            var elevation = AttackElevation.From(environment, input.EnvironmentBindings);
            var evaluation = CombatAttackQueries.Selection(input, originalActor, profile, environment: environment, walls: walls,
                elevation: elevation);
            if (evaluation.FailureReason != AttackFailureReason.None)
                return CombatCommandCalculator.Reject(input, evaluation.FailureReason.ToString(), evaluation.BlockedContacts);
            var engine = new CombatEffectEngine();
            return Execute(input, rules, originalActor, profile, input.Aim, environment, walls, elevation, evaluation, engine,
                publicPreview, reaction: false, out preview);
        }

        /// <summary>표시 범위가 필요 없는 기존 반응 공격 호출을 같은 계산 경로로 연결한다.</summary>
        internal static CombatCommandTransition Reaction(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            CharacterCombatFacts attacker, AttackAim aim)
        {
            return Reaction(input, rules, attacker, aim, out _);
        }

        /// <summary>
        /// 원래 명령 정산 뒤의 반응 공격(보스 포효)이다. 입장 검사(<see cref="CombatAttackQueries.Actor"/>)와 행동 사용·공격 성공 효과는
        /// 건너뛰고(G7), 지정·피해·넉백·도착·연쇄는 일반 공격과 같은 단계를 쓴다. 입력의 ActorId는 원래 행동자를 유지한다(D8).
        /// 결과는 <see cref="CombatRoarEvent"/>로만 내고 transition의 Attack은 비운다. 대상이 없으면 null을 돌려준다.
        /// 그때 <paramref name="emptyAreaCells"/>는 지정이 덮었을 칸(표시용, <see cref="CombatRoarEvent.AreaCells"/>)이며, 그 밖에는 비어 있다.
        /// </summary>
        internal static CombatCommandTransition Reaction(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            CharacterCombatFacts attacker, AttackAim aim, out IReadOnlyList<CellCoord> emptyAreaCells)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            if (attacker == null) throw new ArgumentNullException(nameof(attacker));
            emptyAreaCells = Array.Empty<CellCoord>();
            if (CombatActionAdmission.IsAttackRestricted(attacker)) return null;
            var profile = CombatCommandCalculator.RequiredProfile(input, attacker);
            var walls = Walls(input);
            var elevation = AttackElevation.From(input.State.Environment, input.EnvironmentBindings);
            var evaluation = CombatAttackQueries.Selection(input, attacker, profile, aim, input.State.Environment, walls, elevation);
            emptyAreaCells = evaluation.EmptyAreaCells;
            if (evaluation.FailureReason != AttackFailureReason.None) return null;
            return Execute(input, rules, attacker, profile, aim, input.State.Environment, walls, elevation, evaluation,
                new CombatEffectEngine(), publicPreview: false, reaction: true, out _);
        }

        private static WallGeometry Walls(CombatCommandInput input)
        {
            if (input.ObstacleContents.Count > 0 || input.State.Structures.Any(s => ObstacleContentProfile.IsNativeObstacleKey(s.Definition.Key)))
                ObstacleContentProfile.Validate(input.ObstacleContents, input.EnvironmentBindings, input.State.Structures);
            return WallGeometryBuilder.Build(input.State.BoardWidth, input.State.BoardHeight,
                input.State.Environment, input.EnvironmentBindings, input.State.WallBlocking);
        }

        private static CombatCommandTransition Execute(CombatCommandInput input, IReadOnlyList<IMovementTileRule> rules,
            CharacterCombatFacts originalActor, CombatAttackProfile profile, AttackAim aim, MovementEnvironmentFacts environment,
            WallGeometry walls, AttackElevation elevation, AttackSelectionEvaluation evaluation, CombatEffectEngine engine,
            bool publicPreview, bool reaction, out AttackPreview preview)
        {
            var direction = CombatCommandCalculator.AttackRotation(aim, originalActor.Anchor.Value);
            // S2~S4: 명령 투영, 공통 피해와 전달을 준비한다.
            var world = new CommandAttackProjection(input, environment, evaluation.BlockedContacts, captureMovementUses: !publicPreview);
            var actor = originalActor;
            world.Characters[actor.EntityId] = actor;
            var bindings = CombatCommandEffects.Capture(actor, profile);
            var traitScope = new AttackTraitScope(actor, capturedEnemyCount: AttackTraitScope.CountEnemies(actor, evaluation.Selection));
            int actionEventStart = world.Events.Count;
            TraitAttackNotifications.Apply(input, world, traitScope);
            actor = world.Characters[actor.EntityId];
            bool requiresPreHitMovement = evaluation.Pull?.RequiresPull == true;
            int commonDamage = requiresPreHitMovement ? 0 : CommonAttackDamage(input, actor, profile, world.Events, traitScope);
            var delivery = requiresPreHitMovement
                ? new ResolvedAttackDelivery(Array.Empty<DeliveredAttackHit>(), Array.Empty<CellCoord>())
                : CombatAttackDelivery.Resolve(input, traitScope.ForDamage(actor), profile, evaluation.Selection, commonDamage, walls, elevation);
            world.BlockedContacts.AddRange(delivery.BlockedContacts);
            var plan = new CommandAttackPlan(input,rules,profile,originalActor,actor,environment,walls,evaluation.Selection,                delivery,engine,bindings,new TargetDamagePipeline(new CategoryMatchupResolver(CategoryMatchupSpec.CreateDefault())),                commonDamage,direction,publicPreview,                reaction ? CombatDamageKind.ReactionAttack : CombatDamageKind.OrdinaryAttack,aim: aim,traitScope: traitScope);
            var ledger = new CommandAttackLedger();
            ledger.BeginScope(plan, world);
            MovementProjection arrival = null;
            if (HunterApplication.HasHunters(world.Characters.Values))
            {
                arrival = world.CreateArrivalProjection(plan);
                HunterApplication.Reconcile(arrival.CurrentInput(), world.Characters, arrival.ChooseRandom, world.Events);
            }
            AttackPullSnapshot pullMovement = null;
            bool deliverDirectDamage = true;
            if (requiresPreHitMovement)
            {
                arrival ??= world.CreateArrivalProjection(plan);
                var pulled = PullAttackCalculator.Execute(evaluation.Pull, plan, world, ledger, arrival);
                pullMovement = pulled.Movement;
                deliverDirectDamage = pulled.CanDeliverDamage;
                if (deliverDirectDamage)
                    plan = PullAttackCalculator.RebuildDelivery(plan, world, arrival, evaluation.Pull.TargetId);
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
            arrival = AttackDeliveryExecutor.Execute(plan, world, ledger, deliveredVictims,
                arrival: arrival, settleActionUse: settleActionUse, deliverDirectDamage: deliverDirectDamage);

            CombatDebuffApplication.CompleteAttack(input, originalActor, deliveredVictims, world.Characters, world.Events,
                new CombatDamageOrigin(plan.Actor.Anchor, plan.Actor.EntityId, plan.AttackGroup));
            if (!reaction) ApplyAttackCompletedItem(plan, world, arrival);

            var settlement = reaction
                ? new UnitActionUseSettlementSnapshot(actor.EntityId, actor.ActionUse, actor.ActionUse)
                : CombatActionAdmission.Settlement(originalActor, UnitActionUse.Attacked);
            if (!reaction) arrival.SetActionUseSettlement(settlement);
            traits = world.Events.Skip(actionEventStart).OfType<CombatEffectsEvent>().SelectMany(e => e.Traits)
                .Where(value => value.ResultOrigin == null || value.ResultOrigin.AttackGroup == plan.AttackGroup &&
                    value.ResultOrigin.CreditedCharacterId == actor.EntityId).ToArray();
            categories = world.Events.Skip(actionEventStart).OfType<CombatEffectsEvent>().SelectMany(e => e.Categories).ToArray();
            var result = AttackResultFactory.Create(plan, world, ledger, settlement, traits, categories, out preview, reaction: reaction, pull: pullMovement);
            if (reaction)
            {
                world.Events.Insert(attackEventIndex, new CombatRoarEvent(actor.EntityId, result, result.AffectedCells.ToArray()));
                return arrival.Complete();
            }
            world.Events.Insert(attackEventIndex, new CombatAttackEvent(result));
            return arrival.Complete(result);
        }

        private static void ApplyAttackCompletedItem(CommandAttackPlan plan, CommandAttackProjection world, MovementProjection arrival)
        {
            var holder = world.Characters[plan.Actor.EntityId];
            var definition = CombatItemResolver.EquippedDefinition(plan.Input.ItemDefinitions, holder);
            if (definition == null) return;
            var decision = CombatItemResolver.AfterOrdinaryAttack(definition, new ItemAttackContext(holder,
                plan.Actor.Anchor.Value, world.ItemDirectHits.Values.ToArray(), world.Characters.Values.ToArray(),
                plan.Input.State.Turn.PreparationIndex, arrival.ItemRandomState));
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
