using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat
{
    /// <summary>명령의 관련 변경을 모두 검증한 뒤 한 번 게시할 제안이다.</summary>
    public sealed class CombatCommandTransition
    {
        public CombatCommandInput Input { get; }
        public bool Accepted { get; }
        public string Reason { get; }
        public CharacterChanges Characters { get; }
        public SoulFieldChange Field { get; }
        public CombatCommandState FinalState { get; }
        public StageOutcomeSnapshot Outcome { get; }
        public AttackResult? Attack { get; }
        public IReadOnlyList<long> ConsumedSoulIds { get; }
        public BattleCompletionChanges Completion { get; }
        public MovementDestination? Movement { get; }
        public MovementPathResult PathMovement { get; }
        public CheckpointReturnResult ReturnMovement { get; }
        public CombatDrawSettlement Draw { get; }
        public IReadOnlyList<CombatEvent> Events { get; }
        public EnvironmentBindingsChange EnvironmentBindings { get; }
        public IReadOnlyList<AttackBlockedContact> BlockedContacts { get; }

        public CombatCommandTransition(CombatCommandInput input, bool accepted, string reason,
            CharacterChanges characters, SoulFieldChange field, CombatCommandState finalState,
            StageOutcomeSnapshot outcome, AttackResult? attack = null,
            IReadOnlyList<long> consumedSoulIds = null, BattleCompletionChanges completion = null,
            MovementDestination? movement = null, CombatDrawSettlement draw = null, IReadOnlyList<CombatEvent> events = null,
            MovementPathResult pathMovement = null, CheckpointReturnResult returnMovement = null,
            EnvironmentBindingsChange environmentBindings = null, IReadOnlyList<AttackBlockedContact> blockedContacts = null)
        {
            Input = input ?? throw new ArgumentNullException(nameof(input));
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
            ReturnMovement = returnMovement;
            if (pathMovement != null && returnMovement != null)
                throw new ArgumentException("하나의 명령에는 일반 이동 또는 체크포인트 복귀 결과만 넣을 수 있습니다.");
            Draw = draw;
            Events = Array.AsReadOnly((events ?? Array.Empty<CombatEvent>()).ToArray());
            if (Events.Any(e => e == null) || (!accepted && Events.Count != 0))
                throw new ArgumentException("거절 결과 또는 null 사건을 게시할 수 없습니다.");
            ConsumedSoulIds = Array.AsReadOnly((consumedSoulIds ?? Array.Empty<long>()).ToArray());
            if (!input.Souls.SameValues(field.Expected))
                throw new ArgumentException("명령과 영혼 변경의 원본이 다릅니다.");
            _ = new SoulActionChanges(characters, field, ConsumedSoulIds);
            if (!accepted && (characters.Characters.Count != 0 || characters.Souls.Count != 0 ||
                characters.Main != null || characters.Created.Count != 0 || movement.HasValue || pathMovement != null || returnMovement != null || draw != null || !input.State.SameValues(finalState) ||
                !input.Souls.SameValues(field.Final) || completion != null || attack.HasValue || ConsumedSoulIds.Count != 0))
                throw new ArgumentException("거절 결과에는 전투 변경을 넣을 수 없습니다.");
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
            new CombatCommandTransition(Input, Accepted, Reason, completion.Characters,
                completion.Settlement?.Field ?? Field, FinalState, Outcome, Attack, ConsumedSoulIds, completion, Movement, Draw, Events,
                PathMovement, ReturnMovement, EnvironmentBindings, BlockedContacts);
    }
}
