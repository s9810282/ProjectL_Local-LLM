using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>명령의 관련 변경을 모두 검증한 뒤 한 번 게시할 제안이다.</summary>
    public sealed class CombatCommandTransition
    {
        public CombatRewardSettlement RewardSettlement { get; }
        public CombatRewardChange RewardChange { get; }
        public ItemInventoryChange InventoryChange { get; }
        public CombatCommandInput Input { get; }
        public MovementUndoResult UndoMovement { get; }
        public IReadOnlyList<MovementEffectUse> MovementEffectUses { get; }
        public IReadOnlyList<CombatHealthExecution> HealthExecutions { get; }
        public IReadOnlyList<CombatHealthClaim> HealthClaims { get; }
        public bool Accepted { get; }
        public string Reason { get; }
        public CharacterChanges Characters { get; }
        public SoulFieldChange Field { get; }
        public CombatCommandState FinalState { get; }
        public StageOutcomeSnapshot Outcome { get; }
        public AttackResult? Attack { get; }
        public PlayerSkillResult PlayerSkill { get; }
        public IReadOnlyList<long> ConsumedSoulIds { get; }
        public BattleCompletionChanges Completion { get; }
        public MovementDestination? Movement { get; }
        public MovementPathResult PathMovement { get; }
        public UnitActionUseSettlementSnapshot ActionUseSettlement { get; }
        public IReadOnlyList<CombatEvent> Events { get; }
        public EnvironmentBindingsChange EnvironmentBindings { get; }
        public IReadOnlyList<AttackBlockedContact> BlockedContacts { get; }

        public CombatCommandTransition(CombatCommandInput input, bool accepted, string reason,
            CharacterChanges characters, SoulFieldChange field, CombatCommandState finalState,
            StageOutcomeSnapshot outcome, AttackResult? attack = null,
            IReadOnlyList<long> consumedSoulIds = null, BattleCompletionChanges completion = null,
            MovementDestination? movement = null, IReadOnlyList<CombatEvent> events = null,
            MovementPathResult pathMovement = null,
            EnvironmentBindingsChange environmentBindings = null, IReadOnlyList<AttackBlockedContact> blockedContacts = null, ItemInventoryChange inventoryChange = null,
            CombatRewardChange rewardChange = null, CombatRewardSettlement rewardSettlement = null, UnitActionUseSettlementSnapshot actionUseSettlement = null,
            MovementUndoResult undoMovement = null, IReadOnlyList<MovementEffectUse> movementEffectUses = null,
            IReadOnlyList<CombatHealthExecution> healthExecutions = null, IReadOnlyList<CombatHealthClaim> healthClaims = null,
            PlayerSkillResult playerSkill = null)
            : this(healthExecutions, healthClaims, input, accepted, reason, characters, field, finalState,
                outcome, attack, consumedSoulIds, completion, movement, events, pathMovement, environmentBindings, blockedContacts,
                inventoryChange, rewardChange, rewardSettlement, actionUseSettlement, undoMovement, movementEffectUses, playerSkill)
        {
        }

        internal CombatCommandTransition(IEnumerable<CombatHealthExecution> healthExecutions, IEnumerable<CombatHealthClaim> healthClaims,
            CombatCommandInput input, bool accepted, string reason,
            CharacterChanges characters, SoulFieldChange field, CombatCommandState finalState,
            StageOutcomeSnapshot outcome, AttackResult? attack = null,
            IReadOnlyList<long> consumedSoulIds = null, BattleCompletionChanges completion = null,
            MovementDestination? movement = null, IReadOnlyList<CombatEvent> events = null,
            MovementPathResult pathMovement = null,
            EnvironmentBindingsChange environmentBindings = null, IReadOnlyList<AttackBlockedContact> blockedContacts = null, ItemInventoryChange inventoryChange = null,
            CombatRewardChange rewardChange = null, CombatRewardSettlement rewardSettlement = null, UnitActionUseSettlementSnapshot actionUseSettlement = null,
            MovementUndoResult undoMovement = null, IReadOnlyList<MovementEffectUse> movementEffectUses = null, PlayerSkillResult playerSkill = null)
        {
            Input = input ?? throw new ArgumentNullException(nameof(input));
            PlayerSkill = playerSkill;
            if (!accepted && inventoryChange != null)
                throw new ArgumentException("거절 결과에는 아이템 재고 변경을 넣을 수 없습니다.", nameof(inventoryChange));
            if (!accepted && rewardChange != null)
                throw new ArgumentException("거절 결과에는 보상 변경을 넣을 수 없습니다.", nameof(rewardChange));
            if (rewardSettlement != null && (!accepted ||
                (rewardSettlement.HasClearReward || rewardSettlement.Characters.Count != 0) && outcome.Outcome != StageOutcome.Victory ||
                rewardSettlement.HasClearReward && input.Battle?.ClearRewards == null))
                throw new ArgumentException("보상 정산의 수락 상태·승리·클리어 정의가 일치하지 않습니다.", nameof(rewardSettlement));
            RewardSettlement = rewardSettlement;
            RewardChange = rewardChange;
            InventoryChange = inventoryChange;
            BlockedContacts = AttackBlockedContact.CopyCollection(blockedContacts);
            if (environmentBindings != null && (!accepted || !input.EnvironmentBindings.SameValues(environmentBindings.Expected)))
                throw new ArgumentException("환경 분류 변경의 기대값 또는 수락 상태가 잘못되었습니다.");
            EnvironmentBindings = environmentBindings;
            if (reason == null || (accepted ? reason.Length != 0 : string.IsNullOrWhiteSpace(reason)))
                throw new ArgumentException("수락은 빈 사유, 거절은 사유 코드가 필요합니다.");
            Accepted = accepted; Reason = reason;
            Characters = characters ?? throw new ArgumentNullException(nameof(characters));
            Field = field ?? throw new ArgumentNullException(nameof(field));
            FinalState = finalState ?? throw new ArgumentNullException(nameof(finalState));
            Outcome = outcome; Attack = attack; Completion = completion;
            Movement = movement;
            PathMovement = pathMovement;
            ActionUseSettlement = actionUseSettlement;
            if (!accepted && actionUseSettlement != null) throw new ArgumentException("거절 결과에는 행동 정산이 없습니다.");
            Events = Array.AsReadOnly((events ?? Array.Empty<CombatEvent>()).ToArray());
            if (playerSkill != null)
            {
                if (!accepted || attack.HasValue || movement.HasValue || pathMovement != null || actionUseSettlement != null || undoMovement != null)
                    throw new ArgumentException("스킬 결과는 수락된 독립 사용 정산이며 일반 행동·이동 결과와 섞을 수 없습니다.", nameof(playerSkill));
                playerSkill.ValidateCommand(input.Kind, input.ActorId, Events);
            }
            var uses = (movementEffectUses ?? Array.Empty<MovementEffectUse>()).ToArray();
            if (uses.Any(u => u == null || !Events.Contains(u.Execution)) || !accepted && uses.Length != 0 ||
                uses.Where((u, i) => uses.Take(i).Any(u.SameValues)).Any())
                throw new ArgumentException("효과 사용은 이 수락된 제안의 실행 사건에 한 번만 연결해야 합니다.", nameof(movementEffectUses));
            MovementEffectUses = Array.AsReadOnly(uses);
            bool HasEvent(CombatEvent occurrence) => Events.Any(e => ReferenceEquals(e, occurrence));

            var executions = (healthExecutions ?? Array.Empty<CombatHealthExecution>()).ToArray();
            if (!accepted && executions.Length != 0 || executions.Distinct().Count() != executions.Length ||
                executions.Any(e => e == null || !e.Evidence.All(HasEvent) ||
                    e.Steps.Any(step => step.Source.ContextEvent != null && !HasEvent(step.Source.ContextEvent))))
                throw new ArgumentException("체력 실행은 이 수락된 제안의 사건만 참조하며 실행당 한 번만 포함되어야 합니다.", nameof(healthExecutions));
            HealthExecutions = Array.AsReadOnly(executions);
            var claims = (healthClaims ?? Array.Empty<CombatHealthClaim>()).ToArray();
            if (claims.Length != 0)
            {
                var steps = executions.SelectMany(e => e.Steps).ToHashSet();
                if (!accepted || claims.Any(claim => claim == null || !HasEvent(claim.Event) ||
                    claim.Steps.Any(step => !steps.Contains(step))))
                    throw new ArgumentException("체력 선언은 이 수락된 제안의 사건과 실행 단계만 참조해야 합니다.", nameof(healthClaims));
            }
            HealthClaims = Array.AsReadOnly(claims);
            if (Events.Any(e => e == null) || (!accepted && Events.Count != 0))
                throw new ArgumentException("거절 결과 또는 null 사건을 게시할 수 없습니다.");
            MovementUndoResult.Validate(accepted, input.Kind, input.ActorId, undoMovement, actionUseSettlement, Events, movement, pathMovement, attack);
            UndoMovement = undoMovement;
            ConsumedSoulIds = Array.AsReadOnly((consumedSoulIds ?? Array.Empty<long>()).ToArray());
            if (!input.Souls.SameValues(field.Expected))
                throw new ArgumentException("명령과 영혼 변경의 원본이 다릅니다.");
            _ = new SoulActionChanges(characters, field, ConsumedSoulIds);
            if (!accepted && (characters.Characters.Count != 0 || characters.Souls.Count != 0 ||
                characters.Main != null || characters.Created.Count != 0 || characters.Owned.Count != 0 || movement.HasValue || pathMovement != null || !input.State.SameValues(finalState) ||
                !input.Souls.SameValues(field.Final) || completion != null || attack.HasValue || ConsumedSoulIds.Count != 0))
                throw new ArgumentException("거절 결과에는 전투 변경을 넣을 수 없습니다.");
            // 영혼 변경과 같이 보유 기록 변경의 원본도 이 명령이 읽은 입력이어야 한다.
            if (characters.Owned.Any(change => !input.Owned.Any(owned => owned.SameValues(change.Expected))))
                throw new ArgumentException("명령과 보유 기록 변경의 원본이 다릅니다.");
            if (completion != null && (!accepted || !outcome.Equals(completion.Outcome)))
                throw new ArgumentException("명령의 승패와 완료 제안이 다릅니다.");
            // 제안 자체의 앞뒤가 맞는지는 계약이 만들 때 확인한다. Buffer는 저장소 간 원본 일치만 본다.
            if (accepted && outcome.IsStageEnded != (finalState.Phase == CombatFlowPhase.Finished))
                throw new ArgumentException("종료 승패와 최종 전투 국면이 다릅니다.");
            if (completion != null && (!ReferenceEquals(characters, completion.Characters) ||
                completion.Settlement != null && !completion.Settlement.Field.Final.SameValues(field.Final)))
                throw new ArgumentException("명령과 완료의 캐릭터·영혼 최종값이 다릅니다. 완료는 WithCompletion으로 붙인다.");
        }

        public CombatCommandTransition WithCompletion(BattleCompletionChanges completion) =>
            new CombatCommandTransition(HealthExecutions, HealthClaims, Input, Accepted, Reason, completion.Characters,
                completion.Settlement?.Field ?? Field, FinalState, Outcome, Attack, ConsumedSoulIds, completion, Movement, Events,
                PathMovement, EnvironmentBindings, BlockedContacts, InventoryChange, RewardChange, RewardSettlement,
                ActionUseSettlement, UndoMovement, MovementEffectUses, PlayerSkill);

        public CombatCommandTransition WithFinalState(CombatCommandState finalState) =>
            new CombatCommandTransition(HealthExecutions, HealthClaims, Input, Accepted, Reason, Characters, Field, finalState, Outcome, Attack,
                ConsumedSoulIds, Completion, Movement, Events, PathMovement, EnvironmentBindings, BlockedContacts,
                InventoryChange, RewardChange, RewardSettlement, ActionUseSettlement, UndoMovement, MovementEffectUses, PlayerSkill);

        internal CombatCommandTransition WithPlayerSkill(PlayerSkillResult skill, CombatCommandState finalState) =>
            new CombatCommandTransition(HealthExecutions, HealthClaims, Input, Accepted, Reason, Characters, Field, finalState,
                Outcome, Attack, ConsumedSoulIds, Completion, Movement, Events,
                PathMovement, EnvironmentBindings, BlockedContacts, InventoryChange, RewardChange, RewardSettlement,
                ActionUseSettlement, UndoMovement, MovementEffectUses, skill);

        public CombatCommandTransition WithMovementEffectUses(IReadOnlyList<MovementEffectUse> uses) =>
            new CombatCommandTransition(HealthExecutions, HealthClaims, Input, Accepted, Reason, Characters, Field, FinalState, Outcome, Attack,
                ConsumedSoulIds, Completion, Movement, Events, PathMovement, EnvironmentBindings, BlockedContacts,
                InventoryChange, RewardChange, RewardSettlement, ActionUseSettlement, UndoMovement, uses, PlayerSkill);

        internal CombatCommandTransition WithHealthEvidence(IReadOnlyList<CombatHealthExecution> executions, IReadOnlyList<CombatHealthClaim> claims) =>
            new CombatCommandTransition(executions, claims, Input, Accepted, Reason, Characters, Field, FinalState, Outcome, Attack,
                ConsumedSoulIds, Completion, Movement, Events, PathMovement, EnvironmentBindings, BlockedContacts,
                InventoryChange, RewardChange, RewardSettlement, ActionUseSettlement, UndoMovement,
                CombatHealthExecution.CollectMovementUses(executions, MovementEffectUses), PlayerSkill);
    }
}
