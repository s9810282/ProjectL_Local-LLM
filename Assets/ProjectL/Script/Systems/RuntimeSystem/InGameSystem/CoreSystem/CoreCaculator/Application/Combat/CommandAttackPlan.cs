using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.CombatEffects.Evaluations;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat
{
    /// <summary>
    /// 한 공격 명령이 무엇인지에 대한 불변 사실이다. 공격 단계들이 함께 읽으며 어떤 단계도 바꾸지 않는다.
    /// 명령 계산이 끝나면 버리며 Buffer나 전투 상태로 남기지 않는다.
    /// </summary>
    internal sealed class CommandAttackPlan
    {
        internal CombatCommandInput Input { get; }
        internal IReadOnlyList<IMovementTileRule> Rules { get; }
        internal CombatAttackProfile Profile { get; }
        /// <summary>이동 정산 전 공격자다. 첫 비용 검사와 비용 정산 기준값에 쓴다.</summary>
        internal CharacterCombatFacts OriginalActor { get; }
        /// <summary>미정산 이동을 확정한 공격자다. 피해와 효과 계산은 이 공격자를 쓴다.</summary>
        internal CharacterCombatFacts Actor { get; }
        internal AttackTraitScope TraitScope { get; }
        internal CharacterCombatFacts DamageActor => TraitScope?.ForDamage(Actor) ?? Actor;
        /// <summary>실행은 실제 환경, 공개 미리보기는 <see cref="PreviewObserver"/> 진영에 보이는 환경이다.</summary>
        internal MovementEnvironmentFacts Environment { get; }
        /// <summary>
        /// 공개 미리보기를 보는 진영이다. 지정하지 않으면 공격자 진영이다. 포효 예고처럼 적의 행동을 플레이어에게 보일 때만 플레이어다
        /// (포효 예고 계획 TP-2). 실행(공개 미리보기 아님)에서는 쓰지 않는다.
        /// </summary>
        internal Owner PreviewObserver { get; }
        internal WallGeometry Walls { get; }
        internal ResolvedAttackSelection Selection { get; }
        /// <summary>최초 조준의 셀입니다. 도착 후 재구성한 전달 위치와 구분합니다.</summary>
        internal Contracts.Results.AttackCellSelection RequestedCells { get; }
        internal ResolvedAttackDelivery Delivery { get; }
        /// <summary>수락 시 확정된 주 대상·스플래시 적입니다. 후속 이동과 공격자 사망으로 다시 선택하지 않습니다.</summary>
        internal IReadOnlyList<EntityId> CapturedEnemyIds { get; }
        internal CombatEffectEngine Engine { get; }
        /// <summary>확정한 공격자의 효과 바인딩이다.</summary>
        internal EffectRuleBinding[] Bindings { get; }
        internal TargetDamagePipeline Pipeline { get; }
        internal int CommonDamage { get; }
        internal Rotation4 Direction { get; }
        internal bool PublicPreview { get; }
        internal CombatDamageKind DamageKind { get; }
        internal long AttackGroup { get; }
        internal EffectCausalChain Cause { get; }
        internal ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.AttackAim Aim { get; }

        internal CommandAttackPlan(
            CombatCommandInput input,
            IReadOnlyList<IMovementTileRule> rules,
            CombatAttackProfile profile,
            CharacterCombatFacts originalActor,
            CharacterCombatFacts actor,
            MovementEnvironmentFacts environment,
            WallGeometry walls,
            ResolvedAttackSelection selection,
            ResolvedAttackDelivery delivery,
            CombatEffectEngine engine,
            EffectRuleBinding[] bindings,
            TargetDamagePipeline pipeline,
            int commonDamage,
            Rotation4 direction,
            bool publicPreview,
            CombatDamageKind damageKind = CombatDamageKind.OrdinaryAttack,
            long attackGroup = CommandAttackGroups.Direct,
            EffectCausalChain cause = null,
            ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands.AttackAim? aim = null,
            Contracts.Results.AttackCellSelection requestedCells = null,
            AttackTraitScope traitScope = null,
            Owner? previewObserver = null)
        {
            Input = input ?? throw new ArgumentNullException(nameof(input));
            Rules = rules ?? throw new ArgumentNullException(nameof(rules));
            Profile = profile ?? throw new ArgumentNullException(nameof(profile));
            OriginalActor = originalActor ?? throw new ArgumentNullException(nameof(originalActor));
            Actor = actor ?? throw new ArgumentNullException(nameof(actor));
            Environment = environment ?? throw new ArgumentNullException(nameof(environment));
            Walls = walls ?? throw new ArgumentNullException(nameof(walls));
            Selection = selection ?? throw new ArgumentNullException(nameof(selection));
            var selectedIds = selection.Targets.Select(t => t.Key).ToHashSet();
            CapturedEnemyIds = Array.AsReadOnly(selection.EligibleCharacters.Where(c => selectedIds.Contains(c.EntityId) &&
                new HostilityPolicy().AreHostile(actor.Owner, c.Owner)).Select(c => c.EntityId).Distinct().OrderBy(id => id).ToArray());
            RequestedCells = requestedCells ?? selection.Cells;
            Delivery = delivery ?? throw new ArgumentNullException(nameof(delivery));
            Engine = engine ?? throw new ArgumentNullException(nameof(engine));
            Bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
            Pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
            if (commonDamage < 0) throw new ArgumentOutOfRangeException(nameof(commonDamage));
            CommonDamage = commonDamage;
            Direction = direction;
            PublicPreview = publicPreview;
            PreviewObserver = previewObserver ?? originalActor.Owner;
            DamageKind = damageKind;
            AttackGroup = attackGroup;
            Cause = cause ?? EffectCausalChain.Empty;
            Aim = aim ?? input.Aim;
            TraitScope = traitScope ?? new AttackTraitScope(actor, capturedEnemyCount: AttackTraitScope.CountEnemies(actor, selection));
        }
    }
}
