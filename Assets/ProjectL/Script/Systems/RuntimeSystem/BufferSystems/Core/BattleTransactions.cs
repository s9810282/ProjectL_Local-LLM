using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Artifact;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Core
{
    /// <summary>
    /// 전투 하나의 수명(준비·명령·영혼 행동·미리보기·완료·정리)을 저장소 간 원자적으로 게시한다.
    /// 규칙 판정은 Core 제안에 들어 있고, 여기서는 원본 비교와 준비→설치 순서만 지킨다.
    /// </summary>
    internal sealed class BattleTransactions : IBattleBufferWriteSink
    {
        private readonly BufferGuards _guards;
        private readonly BattleCompletionTransaction _completion;
        private readonly ICharacterBufferReadSink _characterRead;
        private readonly ICharacterBufferWriteSink _characterWrite;
        private readonly ICombatStateBufferReadSink _combatRead;
        private readonly ICombatViewBufferReadSink _combatViewRead;
        private readonly IArtifactBufferReadSink _artifactRead;
        private readonly IStageProgressBufferReadSink _stageRead;
        private readonly ICharacterBufferStore _characterStore;
        private readonly ICombatStateBufferStore _combatStore;
        private readonly ICurrentProgressBufferStore _currentStore;

        internal BattleTransactions(BufferGuards guards, BattleCompletionTransaction completion,
            ICharacterBufferReadSink characterRead, ICharacterBufferWriteSink characterWrite,
            ICombatStateBufferReadSink combatRead, ICombatViewBufferReadSink combatViewRead,
            IArtifactBufferReadSink artifactRead, IStageProgressBufferReadSink stageRead,
            ICharacterBufferStore characterStore, ICombatStateBufferStore combatStore,
            ICurrentProgressBufferStore currentStore)
        {
            _guards = guards ?? throw new System.ArgumentNullException(nameof(guards));
            _completion = completion ?? throw new System.ArgumentNullException(nameof(completion));
            _characterRead = characterRead ?? throw new System.ArgumentNullException(nameof(characterRead));
            _characterWrite = characterWrite ?? throw new System.ArgumentNullException(nameof(characterWrite));
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

            BattlePreparationValidator.Validate(preparation);

            var drawIds = new HashSet<CharacterInstanceId>();
            foreach (CharacterInstanceId id in preparation.Combat.DrawOrder)
            {
                // 드로우 대상 역할(서브만)은 Core 전투 준비가 정한다. 여기서는 중복과 보유 참조만 확인한다.
                if (drawIds.Add(id) == false || _characterRead.TryGetOwned(id, out _) == false)
                    throw new System.InvalidOperationException("드로우 순서가 현재 보유 캐릭터와 맞지 않습니다.");
            }

            PreparedCharacterBattle characters = _characterStore.PrepareBattle(preparation.Characters);
            PreparedCombatState combat = _combatStore.PrepareBattle(preparation);

            return (characters, combat);
        }

        public void Apply(CharacterChanges changes)
        {
            _guards.EnsureActiveBattle();
            _characterWrite.Apply(changes);
        }

        public void Apply(SoulActionChanges changes)
        {
            if (changes == null) throw new System.ArgumentNullException(nameof(changes));
            _guards.EnsureActiveBattle();
            SoulFieldSnapshot field = _combatStore.PrepareSoulField(changes.Field);
            // 모든 캐릭터 제안을 검증한 뒤 영혼 필드와 함께 게시한다.
            _characterWrite.Apply(changes.Characters);
            _combatStore.InstallSoulField(field);
        }

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
            BattleCompletionTransaction.Prepared prepared = _completion.Prepare(changes);
            PreparedCharacterChanges characters = _characterStore.PrepareChanges(changes.Characters);
            _characterStore.InstallChanges(characters);
            _completion.Install(prepared);
            return true;
        }

        public void PublishPreview(CombatPreviewResult preview)
        {
            if (preview == null) throw new System.ArgumentNullException(nameof(preview));
            if (!_combatRead.HasBattle) throw new System.InvalidOperationException("미리볼 전투가 없습니다.");
            if (!_combatViewRead.HasPreview || _combatViewRead.Preview.Number != preview.RequestNumber)
                throw new System.InvalidOperationException("현재 미리보기 요청과 응답 번호가 다릅니다.");
            if (preview.RequestNumber <= _combatRead.AnsweredPreviewNumber) return;
            _combatStore.InstallPreview(preview);
        }

        public void Apply(CombatCommandTransition transition)
        {
            if (transition == null) throw new System.ArgumentNullException(nameof(transition));
            if (transition.Input.ProcessingKind == CombatProcessingKind.Command &&
                transition.Input.Number <= _combatRead.HandledCommandNumber) return;
            _characterStore.ValidateCommandInput(transition.Input);
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
            PreparedCharacterChanges characters = _characterStore.PrepareChanges(transition.Characters);
            SoulFieldSnapshot field = _combatStore.PrepareSoulField(transition.Field);
            PreparedCombatCommand command = _combatStore.PrepareCommand(transition,
                characters.Views.Select(c => c.ToFacts()).ToArray());
            // 드로우의 스테이지 골드는 전투 상태(PrepareCommand)가 커서·ID와 함께 바꾼다. Run 재화는 건드리지 않는다.
            BattleCompletionTransaction.Prepared completion = transition.Completion == null ? null : _completion.Prepare(transition.Completion);
            // 종료된 명령은 완료 기록과 같은 거래로 설치해야 한다(저장소 간 원자성). 제안 내부의 일관성은 계약 생성자가 확인한다.
            if (transition.Accepted && transition.Outcome.IsStageEnded && completion == null)
                throw new System.InvalidOperationException("종료된 명령에는 일치하는 완료 제안이 필요합니다.");
            _characterStore.InstallChanges(characters);
            _combatStore.InstallSoulField(field);
            if (completion != null) _completion.Install(completion);
            _combatStore.InstallCommand(command);
        }

        public void EndBattle()
        {
            _characterStore.EndBattle();
            _combatStore.EndBattle();
        }
    }
}
