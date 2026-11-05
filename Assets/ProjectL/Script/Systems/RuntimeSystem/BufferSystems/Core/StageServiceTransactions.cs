using System.Linq;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Recovery;
using ProjectL.Systems.RuntimeSystem.BufferSystems.RecoveryView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.DataBaseSystem.Data.Common;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Currency;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Map;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Core
{
    /// <summary>
    /// 회복 스테이지의 진입·소비를 게시한다.
    /// 진입 가능·가격·회복량 판정은 Core가 하고, 여기서는 원본 비교와 준비→설치 순서만 지킨다.
    /// </summary>
    internal sealed class StageServiceTransactions :
        IRecoveryStageBufferWriteSink, IRecoveryOperationTransaction
    {
        private readonly BufferGuards _guards;
        private readonly ICurrentProgressBufferReadSink _currentRead;
        private readonly ICombatStateBufferReadSink _combatRead;
        private readonly IMapProgressBufferReadSink _mapRead;
        private readonly IStageProgressBufferReadSink _stageRead;
        private readonly ICharacterBufferReadSink _characterRead;
        private readonly ICharacterBufferStore _characterStore;
        private readonly ICurrencyBufferStore _currencyStore;
        private readonly ICurrentProgressBufferStore _currentStore;
        private readonly ICurrencyBufferReadSink _currencyRead;
        private readonly IStageProgressBufferStore _stageStore;
        private readonly ProgressTransactions _progress;
        private readonly IRecoveryResultReadSink _resultRead;
        private readonly IRecoveryResultStore _resultStore;
        private readonly IRecoveryViewBufferReadSink _recoveryInput;

        internal StageServiceTransactions(BufferGuards guards, ICurrentProgressBufferReadSink currentRead,
            ICombatStateBufferReadSink combatRead, IMapProgressBufferReadSink mapRead,
            IStageProgressBufferReadSink stageRead, ICharacterBufferReadSink characterRead,
            ICharacterBufferStore characterStore,
            ICurrencyBufferStore currencyStore, ICurrentProgressBufferStore currentStore,
            ICurrencyBufferReadSink currencyRead, IStageProgressBufferStore stageStore, ProgressTransactions progress,
            IRecoveryResultReadSink resultRead, IRecoveryResultStore resultStore, IRecoveryViewBufferReadSink recoveryInput)
        {
            _guards = guards ?? throw new System.ArgumentNullException(nameof(guards));
            _currentRead = currentRead ?? throw new System.ArgumentNullException(nameof(currentRead));
            _combatRead = combatRead ?? throw new System.ArgumentNullException(nameof(combatRead));
            _mapRead = mapRead ?? throw new System.ArgumentNullException(nameof(mapRead));
            _stageRead = stageRead ?? throw new System.ArgumentNullException(nameof(stageRead));
            _characterRead = characterRead ?? throw new System.ArgumentNullException(nameof(characterRead));
            _characterStore = characterStore ?? throw new System.ArgumentNullException(nameof(characterStore));
            _currencyStore = currencyStore ?? throw new System.ArgumentNullException(nameof(currencyStore));
            _currentStore = currentStore ?? throw new System.ArgumentNullException(nameof(currentStore));
            _currencyRead = currencyRead ?? throw new System.ArgumentNullException(nameof(currencyRead));
            _stageStore = stageStore ?? throw new System.ArgumentNullException(nameof(stageStore));
            _progress = progress ?? throw new System.ArgumentNullException(nameof(progress));
            _resultRead = resultRead ?? throw new System.ArgumentNullException(nameof(resultRead));
            _resultStore = resultStore ?? throw new System.ArgumentNullException(nameof(resultStore));
            _recoveryInput = recoveryInput ?? throw new System.ArgumentNullException(nameof(recoveryInput));
        }

        public void PrepareRecovery(RecoveryEntryPreparation preparation)
        {
            if (preparation == null) throw new System.ArgumentNullException(nameof(preparation));
            InstallEntry(preparation, null, null);
        }

        public void InstallEntry(RecoveryEntryPreparation preparation, StageEntryCommandResult? result,
            IStageCommandResultStore resultStore)
        {
            if (result.HasValue && resultStore == null) throw new System.ArgumentNullException(nameof(resultStore));
            PreparedCurrentSelection current = preparation == null ? null : PrepareRecoveryEntry(preparation);

            // 준비 중 검증·할당을 모두 끝낸다. 첫 쓰기 뒤에는 외부 조회나 결과 구성을 하지 않는다.
            if (current != null) _currentStore.InstallSelection(current);
            if (result.HasValue) resultStore.InstallMapResult(result.Value);
        }

        private PreparedCurrentSelection PrepareRecoveryEntry(RecoveryEntryPreparation preparation)
        {
            _guards.EnsureRunInProgress();
            if (_combatRead.HasBattle)
                throw new System.InvalidOperationException("남아 있는 전투를 정리한 뒤 회복에 진입해야 합니다.");

            PreparedCurrentSelection current = _currentStore.PrepareRecoverySelection(preparation);
            if (!BufferGuards.IsOpenTarget(_mapRead, _stageRead, preparation.ExpectedChapterKey, preparation.TargetNodeId, preparation.TargetStageKey,
                    preparation.ExpectedTargetStageType, preparation.ExpectedTargetMapRole))
                throw new System.InvalidOperationException("회복 진입 대상의 연결, 종류, 역할 또는 완료 상태가 계산 시점과 다릅니다.");

            return current;
        }

        public bool PublishRecovery(StageNodeId nodeId, StageKey stageKey, CharacterRecoveryDecision decision)
        {
            var prepared = PrepareRecoveryMutation(nodeId, stageKey, decision);
            if (prepared == null) return false;
            InstallMutation(prepared);
            return true;
        }

        public PreparedRecoveryMutation PrepareSelection(RecoveryEntryContext context, CharacterRecoveryDecision decision)
        {
            RequireContext(context);
            return PrepareRecoveryMutation(context.NodeId, context.StageKey, decision)
                ?? throw new System.InvalidOperationException("이미 적용한 회복의 변경을 다시 준비할 수 없습니다.");
        }

        private PreparedRecoveryMutation PrepareRecoveryMutation(StageNodeId nodeId, StageKey stageKey, CharacterRecoveryDecision decision)
        {
            if (decision == null) throw new System.ArgumentNullException(nameof(decision));
            _guards.EnsureRunInProgress();
            _guards.ValidateSelection(_currentRead.CurrentMapKey, nodeId, stageKey);
            if (_combatRead.HasBattle)
                throw new System.InvalidOperationException("전투 중에는 회복 스테이지를 소비할 수 없습니다.");
            CurrentRecoveryData recovery = _currentRead.Recovery;
            if (recovery == null || !recovery.TryGetOption(decision.Option.Id, out var option))
                throw new System.InvalidOperationException("현재 회복 선택지가 아닙니다.");
            if (recovery.MainId.IsEmpty || decision.SoulSourceId != recovery.MainId || decision.Characters.Main.Id != recovery.MainId)
                throw new System.InvalidOperationException("회복 변경과 영혼 출처는 진입 시 캡처한 메인 ID와 같아야 합니다.");
            if (recovery.AppliedOptionId.HasValue)
            {
                if (recovery.AppliedOptionId != option.Id)
                    throw new System.InvalidOperationException("이미 다른 회복 선택지를 소비했습니다.");
                return null;
            }
            if (option.Cost != decision.Option.Cost || option.EffectKind != decision.Option.EffectKind ||
                option.MaxHealthIncrease != decision.Option.MaxHealthIncrease ||
                option.Heal != null && (option.Heal.Mode != decision.Option.Heal.Mode || option.Heal.Value != decision.Option.Heal.Value))
                throw new System.InvalidOperationException("회복 제안이 현재 선택지와 다릅니다.");
            // 어느 캐릭터의 영혼을 쓸지는 Core가 정한다. 여기서는 그 출처의 영혼 수가 계산 시점과 같은지만 본다.
            if (!_characterRead.TryGetOwned(decision.SoulSourceId, out _) ||
                _characterRead.GetSoulCount(decision.SoulSourceId) != decision.SoulCount)
                throw new System.InvalidOperationException("회복 최대 체력의 영혼 원본이 다릅니다.");
            int balance = _currencyStore.PrepareAmount(decision.ExpectedBalance, decision.FinalBalance);
            CurrentRecoveryData receipt = recovery.WithRecovery(option.Id, option.Cost, decision.HealedAmount, decision.MaxHealthIncrease);
            // 회복은 전투 밖 상호작용이므로 참가 목록이 없다. 전투가 남아 있으면 캐릭터 Store가 거절한다.
            PreparedCharacterChanges characters = _characterStore.PrepareChanges(decision.Characters, membership: null);
            return CaptureMutation(decision.SoulSourceId, characters, balance, receipt, null);
        }

        public PreparedRecoveryMutation PrepareCompletion(RecoveryEntryContext context)
        {
            RequireContext(context);
            var clear = _progress.PrepareStageClear(context.ChapterKey, context.NodeId, context.StageKey);
            return CaptureMutation(_currentRead.Recovery.MainId, null, _currencyRead.Amount, _currentRead.Recovery, clear);
        }

        private void RequireContext(RecoveryEntryContext context)
        {
            var validated = new RecoveryEntryContext(context.EntryId, context.ChapterKey, context.NodeId, context.StageKey);
            _guards.EnsureRunInProgress();
            _guards.ValidateSelection(validated.ChapterKey, validated.NodeId, validated.StageKey);
            if (_combatRead.HasBattle || _currentRead.Recovery == null || _currentRead.Recovery.Context != validated)
                throw new System.InvalidOperationException("회복 변경의 진입 문맥이 현재와 다릅니다.");
        }

        private PreparedRecoveryMutation CaptureMutation(CharacterInstanceId soulSource, PreparedCharacterChanges characters,
            int finalGold, CurrentRecoveryData receipt, PreparedStageClear clear)
        {
            if (_currentRead.Recovery.MainId != soulSource || !_characterRead.TryGetMainState(soulSource, out var main))
                throw new System.InvalidOperationException("회복 원본 메인이 필요합니다.");
            int chapter = _currentRead.CurrentMapKey;
            return new PreparedRecoveryMutation(this, chapter, _currentRead.CurrentStage, _currentRead.Recovery,
                main, soulSource, _characterRead.GetSoulCount(soulSource), _currencyRead.Amount,
                _characterRead.RosterRevision,
                _stageRead.GetStages(chapter).ToArray(), _mapRead.GetMap(chapter).IsClear,
                characters, finalGold, receipt, clear);
        }

        public void InstallMutation(PreparedRecoveryMutation prepared)
        {
            if (prepared == null) throw new System.ArgumentNullException(nameof(prepared));
            if (!ReferenceEquals(prepared.Owner, this) || prepared.Installed)
                throw new System.InvalidOperationException("회복 변경은 준비한 소유자가 한 번만 설치해야 합니다.");
            _guards.EnsureRunInProgress();
            _guards.ValidateSelection(prepared.Chapter, new StageNodeId(prepared.Selection.NodeId),
                new StageKey(prepared.Selection.StageKey));
            if (_combatRead.HasBattle || !_currentRead.CurrentStage.Equals(prepared.Selection) ||
                !ReferenceEquals(_currentRead.Recovery, prepared.ExpectedRecovery) ||
                !_characterRead.TryGetMainState(prepared.SoulSource, out var main) || !ReferenceEquals(main, prepared.ExpectedMain) ||
                !_characterRead.TryGetOwned(prepared.SoulSource, out _) ||
                _characterRead.GetSoulCount(prepared.SoulSource) != prepared.ExpectedSouls ||
                _characterRead.RosterRevision != prepared.ExpectedRosterRevision ||
                _currencyRead.Amount != prepared.ExpectedGold ||
                _mapRead.GetMap(prepared.Chapter).IsClear != prepared.ExpectedMapClear ||
                !_stageRead.GetStages(prepared.Chapter).SequenceEqual(prepared.ExpectedNodes))
                throw new System.InvalidOperationException("회복 설치 전에 원본 사실이 변경되었습니다.");
            if (prepared.Clear != null)
            {
                var currentClear = _progress.PrepareStageClear(prepared.Chapter,
                    new StageNodeId(prepared.Selection.NodeId), new StageKey(prepared.Selection.StageKey));
                if (!ReferenceEquals(currentClear.Stages, prepared.Clear.Stages))
                    throw new System.InvalidOperationException("회복 완료를 준비한 노드 목록이 교체되었습니다.");
            }

            // 준비 이후 다른 소비로 RunSpent가 달라져도 첫 쓰기 전에 넘침을 다시 검증한다.
            _currencyStore.PrepareAmount(prepared.ExpectedGold, prepared.FinalGold);
            // 이후에는 준비된 값만 설치한다. 결과 구성·조회·외부 호출을 사이에 넣지 않는다.
            prepared.Installed = true;
            if (prepared.Characters != null)
            {
                _characterStore.InstallChanges(prepared.Characters);
                _currencyStore.InstallAmount(prepared.FinalGold);
                _currentStore.InstallRecovery(prepared.Recovery);
            }
            if (prepared.Clear != null) _stageStore.InstallClear(prepared.Clear);
        }

        public PreparedRecoveryPublication PreparePublication(PreparedRecoveryMutation mutation,
            RecoveryProcessedResult result, int acknowledgedNumber)
        {
            var prepared = _resultStore.PrepareResult(result, acknowledgedNumber);
            return new PreparedRecoveryPublication(this, mutation, prepared, acknowledgedNumber,
                _resultRead.HandledCommandNumber);
        }

        public void InstallPublication(PreparedRecoveryPublication prepared)
        {
            if (prepared == null) throw new System.ArgumentNullException(nameof(prepared));
            if (!ReferenceEquals(prepared.Owner, this) || prepared.Installed)
                throw new System.InvalidOperationException("회복 게시는 준비한 소유자가 한 번만 설치해야 합니다.");
            if (_resultRead.HandledCommandNumber != prepared.HandledNumber ||
                _recoveryInput.AcknowledgedResultNumber != prepared.AcknowledgedNumber)
                throw new System.InvalidOperationException("회복 게시 준비 후 결과 또는 소비 확인이 바뀌었습니다.");
            _resultStore.PrepareResult(prepared.Result, prepared.AcknowledgedNumber);

            // 변경 설치의 검증은 첫 쓰기 전에 끝난다. 이후 입력 취소를 다시 읽어 결과를 버리지 않는다.
            if (prepared.Mutation != null) InstallMutation(prepared.Mutation);
            _resultStore.InstallResult(prepared.Result);
            prepared.Installed = true;
        }

    }
}
