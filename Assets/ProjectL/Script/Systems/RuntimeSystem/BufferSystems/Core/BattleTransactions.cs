using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Fame;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Items;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Run;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Core
{
    /// <summary>
    /// 전투 하나의 수명(준비·명령·영혼 행동·미리보기·완료·정리)을 저장소 간 원자적으로 게시한다.
    /// 규칙 판정은 Core 제안에 들어 있고, 여기서는 원본 비교와 준비→설치 순서만 지킨다.
    /// </summary>
    internal sealed class BattleTransactions : IBattleBufferWriteSink
    {
        private readonly IRunProgressBufferReadSink _run;
        private readonly IRunProgressBufferStore _runStore;
        private readonly IRunDeckReadSink _deck;
        private readonly IFameBufferReadSink _fameRead;
        private readonly IFameBufferStore _fameStore;
        private readonly ICurrencyBufferReadSink _currencyRead;
        private readonly ICurrencyBufferStore _currencyStore;
        private readonly IItemInventoryStore _items;
        private readonly BufferGuards _guards;
        private readonly BattleCompletionTransaction _completion;
        private readonly ICharacterBufferReadSink _characterRead;
        private readonly ICombatStateBufferReadSink _combatRead;
        private readonly ICombatViewBufferReadSink _combatViewRead;
        private readonly IArtifactBufferReadSink _artifactRead;
        private readonly IStageProgressBufferReadSink _stageRead;
        private readonly ICharacterBufferStore _characterStore;
        private readonly ICombatStateBufferStore _combatStore;
        private readonly ICurrentProgressBufferStore _currentStore;

        internal BattleTransactions(BufferGuards guards, BattleCompletionTransaction completion,
            ICharacterBufferReadSink characterRead,
            ICombatStateBufferReadSink combatRead, ICombatViewBufferReadSink combatViewRead,
            IArtifactBufferReadSink artifactRead, IStageProgressBufferReadSink stageRead,
            ICharacterBufferStore characterStore, ICombatStateBufferStore combatStore, ICurrentProgressBufferStore currentStore, IItemInventoryStore items,
            ICurrencyBufferReadSink currencyRead, ICurrencyBufferStore currencyStore,
            IFameBufferReadSink fameRead, IFameBufferStore fameStore, IRunProgressBufferReadSink run,
            IRunProgressBufferStore runStore, IRunDeckReadSink deck)
        {
            _run = run ?? throw new System.ArgumentNullException(nameof(run));
            _runStore = runStore ?? throw new System.ArgumentNullException(nameof(runStore));
            _deck = deck ?? throw new System.ArgumentNullException(nameof(deck));
            _fameRead = fameRead ?? throw new System.ArgumentNullException(nameof(fameRead));
            _fameStore = fameStore ?? throw new System.ArgumentNullException(nameof(fameStore));
            _currencyRead = currencyRead ?? throw new System.ArgumentNullException(nameof(currencyRead));
            _currencyStore = currencyStore ?? throw new System.ArgumentNullException(nameof(currencyStore));
            _items = items ?? throw new System.ArgumentNullException(nameof(items));
            _guards = guards ?? throw new System.ArgumentNullException(nameof(guards));
            _completion = completion ?? throw new System.ArgumentNullException(nameof(completion));
            _characterRead = characterRead ?? throw new System.ArgumentNullException(nameof(characterRead));
            _combatRead = combatRead ?? throw new System.ArgumentNullException(nameof(combatRead));
            _combatViewRead = combatViewRead ?? throw new System.ArgumentNullException(nameof(combatViewRead));
            _artifactRead = artifactRead ?? throw new System.ArgumentNullException(nameof(artifactRead));
            _stageRead = stageRead ?? throw new System.ArgumentNullException(nameof(stageRead));
            _characterStore = characterStore ?? throw new System.ArgumentNullException(nameof(characterStore));
            _combatStore = combatStore ?? throw new System.ArgumentNullException(nameof(combatStore));
            _currentStore = currentStore ?? throw new System.ArgumentNullException(nameof(currentStore));
        }

        public void PrepareBattle(BattlePreparation preparation)
        {
            var prepared = PrepareBattleStores(preparation);
            _characterStore.InstallBattle(prepared.Characters);
            _combatStore.InstallBattle(prepared.Combat);
        }

        public void PrepareBattle(BattlePreparation preparation, BattleEntrySelection selection)
        {
            if (preparation == null) throw new System.ArgumentNullException(nameof(preparation));
            if (selection == null) throw new System.ArgumentNullException(nameof(selection));
            _guards.EnsureRunInProgress();

            PreparedCurrentSelection current = _currentStore.PrepareSelection(selection);
            if (!_stageRead.GetStages(selection.ExpectedChapterKey).Any(stage =>
                stage.NodeId == selection.TargetNodeId.Value && stage.StageKey == selection.TargetStageKey.Value))
                throw new System.InvalidOperationException("전투 진입 대상의 노드와 스테이지 연결이 현재 기록과 다릅니다.");

            var prepared = PrepareBattleStores(preparation);
            // 모든 저장소의 검증과 준비를 마친 뒤 선택과 전투를 함께 설치한다.
            _characterStore.InstallBattle(prepared.Characters);
            _currentStore.InstallSelection(current);
            _combatStore.InstallBattle(prepared.Combat);
        }

        private (PreparedCharacterBattle Characters, PreparedCombatState Combat) PrepareBattleStores(BattlePreparation preparation)
        {
            if (preparation == null)
                throw new System.ArgumentNullException(nameof(preparation));
            _guards.EnsureRunInProgress();

            // 모든 전투는 진입 때 포착한 참가 목록을 가진다. 참가 목록 없는(전체 보유) 전투 모드는 없다.
            if (preparation.Membership == null)
                throw new System.InvalidOperationException("전투 준비에는 진입 때 포착한 참가 목록이 필요합니다.");
            // 설치 직전에 포착 원본(Run·로스터 버전·확정 덱 인스턴스)을 대조한다. 설치 뒤에는 이 버전을 따로 저장하지 않는다.
            if (preparation.Membership.RunId != _run.RunId || _characterRead.RosterRevision != preparation.RosterRevision ||
                !_deck.TryGetCurrent(out var currentDeck) || !ReferenceEquals(currentDeck, preparation.Membership))
                throw new System.InvalidOperationException("전투 진입 선택 또는 보유 버전이 변경되었습니다.");
            BattlePreparationValidator.Validate(preparation);

            var drawIds = new HashSet<CharacterInstanceId>();
            foreach (CharacterInstanceId id in preparation.Combat.DrawOrder)
            {
                // 드로우 대상 역할(서브만)은 Core 전투 준비가 정한다. 여기서는 중복과 보유 참조만 확인한다.
                if (drawIds.Add(id) == false || _characterRead.TryGetOwned(id, out _) == false)
                    throw new System.InvalidOperationException("드로우 순서가 현재 보유 캐릭터와 맞지 않습니다.");
            }

            PreparedCharacterBattle characters = _characterStore.PrepareBattle(preparation.Characters, preparation.Membership);
            PreparedCombatState combat = _combatStore.PrepareBattle(preparation);

            return (characters, combat);
        }

        public void Apply(CharacterChanges changes)
        {
            _guards.EnsureActiveBattle();
            PreparedCharacterChanges characters = _characterStore.PrepareChanges(changes, ActiveMembership());
            var undo = PrepareUndoInvalidation(characters, changes, _combatRead.SoulField);
            _combatStore.ValidateQueryRevision(undo.ExpectedRevision);
            _characterStore.InstallChanges(characters);
            _combatStore.InstallMovementUndoState(undo);
        }

        public void Apply(SoulActionChanges changes)
        {
            if (changes == null) throw new System.ArgumentNullException(nameof(changes));
            _guards.EnsureActiveBattle();
            SoulFieldSnapshot field = _combatStore.PrepareSoulField(changes.Field);
            // 모든 캐릭터 제안을 검증한 뒤 영혼 필드와 함께 게시한다.
            PreparedCharacterChanges characters = _characterStore.PrepareChanges(changes.Characters, ActiveMembership());
            var undo = PrepareUndoInvalidation(characters, changes.Characters, field);
            _combatStore.ValidateQueryRevision(undo.ExpectedRevision);
            _characterStore.InstallChanges(characters);
            _combatStore.InstallSoulField(field);
            _combatStore.InstallMovementUndoState(undo);
        }

        private (CombatStateData State, long ExpectedRevision, long FinalRevision) PrepareUndoInvalidation(
            PreparedCharacterChanges prepared, CharacterChanges changes, SoulFieldSnapshot field, bool completion = false) =>
            _combatStore.PrepareMovementUndoInvalidation(_characterRead.Characters.Select(c => c.ToFacts()).ToArray(),
                prepared.Views.Select(c => c.ToFacts()).ToArray(), changes, field, completion);

        /// <summary>활성 전투의 참가 목록이다. 원본은 Combat 상태 하나이며 캐릭터 Store의 검사에 그대로 넘긴다.</summary>
        private RunDeckSnapshot ActiveMembership() => _combatRead.Current?.Membership
            ?? throw new System.InvalidOperationException("활성 전투의 참가 목록이 없습니다.");

        public bool CompleteBattle(BattleCompletionChanges changes)
        {
            if (changes == null) throw new System.ArgumentNullException(nameof(changes));
            var progress = changes.Progression;
            _guards.ValidateSelection(progress.ChapterKey, progress.NodeId, progress.StageKey);
            BattleCompletionChanges previous = _combatRead.Completion;
            if (previous != null)
            {
                if (previous.Progression.ChapterKey != progress.ChapterKey ||
                    previous.Progression.NodeId != progress.NodeId || previous.Progression.StageKey != progress.StageKey ||
                    previous.Outcome.Outcome != changes.Outcome.Outcome || previous.Outcome.Reason != changes.Outcome.Reason)
                    throw new System.InvalidOperationException("이미 기록한 완료 결과와 다릅니다.");
                return false;
            }
            _guards.EnsureActiveBattle();
            if (changes.Rewards != null)
                throw new System.InvalidOperationException("보상 완료는 명령 거래로만 게시할 수 있습니다.");
            BattleCompletionTransaction.Prepared prepared = _completion.Prepare(changes);
            PreparedCharacterChanges characters = _characterStore.PrepareChanges(changes.Characters, ActiveMembership());
            var undo = PrepareUndoInvalidation(characters, changes.Characters, _combatRead.SoulField, completion: true);
            _combatStore.ValidateQueryRevision(undo.ExpectedRevision);
            _characterStore.InstallChanges(characters);
            _completion.Install(prepared);
            _combatStore.InstallMovementUndoState(undo);
            return true;
        }

        public void PublishPreview(CombatPreviewResult preview)
        {
            if (preview == null) throw new System.ArgumentNullException(nameof(preview));
            if (!_combatRead.HasBattle) throw new System.InvalidOperationException("미리볼 전투가 없습니다.");
            if (!_combatViewRead.HasPreview || _combatViewRead.Preview.Number != preview.RequestNumber)
                throw new System.InvalidOperationException("현재 미리보기 요청과 응답 번호가 다릅니다.");
            if (preview.RequestNumber <= _combatRead.AnsweredPreviewNumber) return;
            var request = _combatViewRead.Preview.Data;
            var availability = preview.ActionAvailability;
            if (request.Kind == CombatViewPreviewKind.ActionAvailability || availability != null)
            {
                if (request.Kind != CombatViewPreviewKind.ActionAvailability || availability == null ||
                    availability.SourceRevision == 0 || availability.SourceRevision != _combatRead.QueryRevision ||
                    availability.BattleInstanceId != _combatRead.BattleInstanceId || availability.ActorId != request.EntityId ||
                    availability.ControllerTurnSequence != _combatRead.Current.CommandState.Turn.ControllerTurnSequence)
                    throw new System.InvalidOperationException("행동 조회의 전투·턴·개체·원본 버전이 현재 요청과 다릅니다.");
            }
            _combatStore.InstallPreview(preview);
        }

        public void Apply(CombatCommandTransition transition)
        {
            if (transition == null) throw new System.ArgumentNullException(nameof(transition));
            if (transition.Input.ProcessingKind == CombatProcessingKind.Command &&
                transition.Input.Number <= _combatRead.HandledCommandNumber) return;
            _characterStore.ValidateCommandInput(transition.Input, _combatRead.Current?.Membership);
            if (transition.Input.ArtifactItems != null)
            {
                var currentItems = _artifactRead.Current.PlacedItems;
                if (currentItems.Count != transition.Input.ArtifactItems.Count || transition.Input.ArtifactItems.Any(expected =>
                    !currentItems.Any(item => item.InstanceId == expected.InstanceId && item.ArtifactId == expected.ArtifactId)))
                    throw new System.InvalidOperationException("명령 계산이 읽은 Artifact 배치 원본이 변경되었습니다.");
            }
            _guards.EnsureRunInProgress();
            if (!_combatRead.HasBattle) throw new System.InvalidOperationException("처리할 전투가 없습니다.");
            if (transition.Accepted && _combatRead.Completion != null)
                throw new System.InvalidOperationException("완료된 전투에는 게임 변경을 게시할 수 없습니다.");

            // 결과 번호를 포함한 모든 제안을 준비하고, 이후에는 설치만 수행한다.
            if (transition.InventoryChange != null && !transition.Input.ItemInventory.SameValues(transition.InventoryChange.Expected))
                throw new System.InvalidOperationException("명령 입력과 재고 변경의 기대값이 다릅니다.");
            PreparedItemInventoryChange items = transition.InventoryChange == null ? null : _items.Prepare(transition.InventoryChange);
            // 슬롯에서 발동한 명령도 계산 때 읽은 공용 재고와 일치해야 한다. 검증용 제안은 설치하지 않는다.
            if (transition.InventoryChange == null)
                _items.Prepare(new ItemInventoryChange(transition.Input.ItemInventory, transition.Input.ItemInventory));
            PreparedCharacterChanges characters = _characterStore.PrepareCommandChanges(transition.Characters, ActiveMembership(), transition.UndoMovement);
            SoulFieldSnapshot field = _combatStore.PrepareSoulField(transition.Field);
            PreparedCombatCommand command = _combatStore.PrepareCommand(transition,
                characters.Views.Select(c => c.ToFacts()).ToArray());
            var rewards = PrepareRewards(transition);
            var settlement = transition.RewardSettlement;
            int? kills = settlement != null && settlement.KillCount > 0
                ? _runStore.PrepareEnemiesDefeated(settlement.KillCount) : (int?)null;
            BattleCompletionTransaction.Prepared completion = transition.Completion == null ? null : _completion.Prepare(transition.Completion);
            // 종료된 명령은 완료 기록과 같은 거래로 설치해야 한다(저장소 간 원자성). 제안 내부의 일관성은 계약 생성자가 확인한다.
            if (transition.Accepted && transition.Outcome.IsStageEnded && completion == null)
                throw new System.InvalidOperationException("종료된 명령에는 일치하는 완료 제안이 필요합니다.");
            _combatStore.ValidateQueryRevision(command.ExpectedRevision);
            _characterStore.InstallChanges(characters);
            _combatStore.InstallSoulField(field);
            // Run 종료 요약은 완료 설치 순간의 처치 수·쓴 골드·획득 명성으로 만든다.
            // 그래서 이 걸음의 처치·재화를 완료보다 먼저 설치한다(Run 요약 설계 S5).
            if (kills.HasValue) _runStore.InstallEnemiesDefeated(kills.Value);
            if (rewards.Gold.HasValue) _currencyStore.InstallAmount(rewards.Gold.Value);
            if (rewards.Fame.HasValue) _fameStore.InstallAmount(rewards.Fame.Value);
            if (completion != null) _completion.Install(completion);
            if (items != null) _items.Install(items);
            _combatStore.InstallCommand(command);
        }

        private (int? Gold, int? Fame) PrepareRewards(CombatCommandTransition transition)
        {
            var input = transition.Input;
            if (input.GoldBalance.HasValue && input.GoldBalance.Value != _currencyRead.Amount ||
                input.FameBalance.HasValue && input.FameBalance.Value != _fameRead.Amount)
                throw new System.InvalidOperationException("보상 계산이 읽은 재화 잔액이 변경되었습니다.");
            // 정산·영수증·경험치 사건·잔액 제안이 서로 맞는지는 계약(CombatRewardConsistency)이 확인한다.
            // 옛 코드는 처치 골드 차이를 잔액 대조보다 먼저 계산했지만, 두 누적값이 모두 0 이상이라 넘치지 않으므로 순서가 결과를 바꾸지 않는다.
            CombatRewardChange change = CombatRewardConsistency.Validate(transition);
            if (change == null) return (null, null);
            return (_currencyStore.PrepareAmount(change.ExpectedGold, change.FinalGold),
                _fameStore.PrepareAmount(change.ExpectedFame, change.FinalFame));
        }

        public void EndBattle()
        {
            _characterStore.EndBattle();
            _combatStore.EndBattle();
        }
    }
}
