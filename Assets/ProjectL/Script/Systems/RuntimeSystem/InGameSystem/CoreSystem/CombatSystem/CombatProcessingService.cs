using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Core;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Current;
using ProjectL.Systems.DataBaseSystem.Data.InGame.Table.Stage.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Commands;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Artifact;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CombatSystem
{
    public interface ICombatProcessingService : IServiceModule
    {
        /// <summary>진입 수락 시 선택과 준비된 전투가 함께 게시된다. Job 완료와 생명주기 활성화는 호출자가 담당한다.</summary>
        ChapterProgressionResult EnterBattle(int chapterKey, StageNodeId targetNodeId);
        /// <summary>이미 확정된 선택을 준비하는 호환 경로다. 새 스테이지 진입에는 EnterBattle을 사용한다.</summary>
        void PrepareBattle();
        bool ProcessNextCommand();
        bool ProcessLatestPreview();
        bool AdvanceCombat();
        CombatDrawAvailability GetDrawAvailability();
        CombatTimelineEntrySnapshot[] GetTimeline(int maxEntries = 10);
        void ApplyHealth(EntityId entityId, int currentHealth);
        void ApplyPersistentMain(MainCharacterPersistentState final);
        void FinalizeTurn(EntityId actorId, SoulActionChanges settled, ArtifactLoadoutSnapshot loadout);
        bool ResolveBattle(ArtifactLoadoutSnapshot loadout);
        /// <summary>
        /// 활성 전투를 자발적 포기(패배)로 완료한다. 정산 입력(현재 Artifact 배치 효과 등)은 Core가 내부 상태에서 조립한다.
        /// 완료 기록까지만 하고 일반 전투 결과(CombatResult)는 새로 게시하지 않는다. 표시·EndBattle·전환은 호출하는 흐름이 한다.
        /// </summary>
        BattleAbandonResult AbandonBattle();
        bool Recover(RecoveryOptionId optionId);
        void EndBattle();
    }

    /// <summary>현재 선택을 추측하지 않고 지정된 챕터와 스테이지의 정적 정의를 제공한다.</summary>
    internal interface IBattleInitializationSource
    {
        BattleInitialization Read(IDataBaseQueryContext database, int chapterKey, StageKey stageKey);
    }

    /// <summary>입력 조회, Core 계산, Buffer 게시를 조율하며 전투 런타임을 소유하지 않는다.</summary>
    internal sealed class CombatProcessingService : ICombatProcessingService,
        ISystemInitData<IDataBaseQueryContext>, ISystemPostBindService, IExportSystemServices
    {
        private readonly IBattleInitializationSource _source;
        private IDataBaseQueryContext _database;
        private IBufferQuery _buffers;
        private readonly ICharacterRosterService _characters;
        private readonly IChapterProgressionCoordinator _progression;
        private readonly IReadOnlyList<IMovementTileRule> _movementRules;
        // 정의 캐시만 보관한다. 현재 전투 값은 매번 Buffer에서 읽는다.
        private BattleInitialization _definitions;

        internal CombatProcessingService(IBattleInitializationSource source, ICharacterRosterService characters,
            IReadOnlyList<IMovementTileRule> movementRules, IChapterProgressionCoordinator progression = null)
        {
            _source = source;
            _characters = characters ?? throw new ArgumentNullException(nameof(characters));
            _progression = progression;
            if (movementRules == null) throw new ArgumentNullException(nameof(movementRules));
            var copiedRules = movementRules.ToArray();
            if (copiedRules.Any(r => r == null || string.IsNullOrWhiteSpace(r.Key)) ||
                copiedRules.Select(r => r.Key).Distinct(StringComparer.Ordinal).Count() != copiedRules.Length)
                throw new ArgumentException("이동 규칙의 키는 유효하며 중복될 수 없습니다.", nameof(movementRules));
            _movementRules = Array.AsReadOnly(copiedRules);
        }

        #region 프레임워크 연결

        public void InitData(IDataBaseQueryContext data)
        {
            _database = data ?? throw new ArgumentNullException(nameof(data));
        }

        public void PostBindService(ISystemServiceStoreReader resolver)
        {
            _buffers = resolver.GetService(BufferSystemKey.Buffer);
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ICombatProcessingService>(this);
            sink.AddModule<ISystemModule>(this);
        }

        #endregion

        #region 전투 준비와 명령 처리

        public ChapterProgressionResult EnterBattle(int chapterKey, StageNodeId targetNodeId)
        {
            if (chapterKey < 1) throw new ArgumentOutOfRangeException(nameof(chapterKey));
            if (targetNodeId.IsEmpty) throw new ArgumentException("진입할 노드 ID가 필요합니다.", nameof(targetNodeId));
            EnsurePreparationDependencies();
            if (RunEnded) return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.RunEnded);
            EnsurePreparationReady();
            if (_progression == null) throw new InvalidOperationException("진입 판단 서비스가 필요합니다.");
            // 로스터가 없으면 전투를 준비할 수 없다. 정의를 읽거나 선택을 바꾸기 전에 거절한다.
            if (!_buffers.CharacterReadSink.TryGetMainState(out _))
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.RosterNotInitialized);

            var current = _buffers.CurrentProgressReadSink;
            int originChapter = current.CurrentMapKey;
            bool originSelected = current.HasSelection;
            StageSelectionData originStage = current.CurrentStage;
            ChapterProgressionResult decision = _progression.PrepareStageEntry(chapterKey, targetNodeId);
            if (!decision.Accepted) return decision;

            ValidateCombatTarget(decision);
            BattleInitialization initialization = ReadDefinition(decision.ChapterKey, decision.StageKey);
            BattlePreparation preparation = CalculatePreparation(initialization);

            // 정의 조회와 계산이 끝난 뒤에도 현재 원본과 합법적인 대상이 같은지 확인한다.
            ChapterProgressionResult revalidated = _progression.PrepareStageEntry(chapterKey, targetNodeId);
            if (!revalidated.Accepted) return revalidated;
            if (current.CurrentMapKey != originChapter || current.HasSelection != originSelected ||
                current.CurrentStage.NodeId != originStage.NodeId || current.CurrentStage.StageKey != originStage.StageKey ||
                current.CurrentStage.StageName != originStage.StageName ||
                revalidated.ChapterKey != decision.ChapterKey || revalidated.NodeId != decision.NodeId ||
                revalidated.StageKey != decision.StageKey)
                return ChapterProgressionResult.Reject(ChapterProgressionFailureReason.StageMismatch);

            var selection = new BattleEntrySelection(originChapter, originSelected, originStage,
                revalidated.NodeId, revalidated.StageKey, $"Stage {revalidated.StageKey.Value}");
            _buffers.BattleWriteSink.PrepareBattle(preparation, selection);
            _definitions = initialization;
            return revalidated;
        }

        public void PrepareBattle()
        {
            EnsurePreparationReady();
            StageSelectionData selected = ValidateCommittedSelection();
            int chapter = _buffers.CurrentProgressReadSink.CurrentMapKey;
            BattleInitialization initialization = ReadCommittedDefinition();
            EnsurePreparationReady();
            StageSelectionData live = ValidateCommittedSelection();
            if (_buffers.CurrentProgressReadSink.CurrentMapKey != chapter ||
                live.NodeId != selected.NodeId || live.StageKey != selected.StageKey || live.StageName != selected.StageName)
                throw new InvalidOperationException("정의 조회 중 확정된 선택이 변경되었습니다.");
            _buffers.BattleWriteSink.PrepareBattle(CalculatePreparation(initialization));
            _definitions = initialization;
        }

        private void EnsurePreparationReady()
        {
            EnsurePreparationDependencies();
            EnsureRunInProgress();
            if (_buffers.CombatStateReadSink.HasBattle)
                throw new InvalidOperationException("이미 준비된 전투가 있습니다.");
        }

        private void EnsurePreparationDependencies()
        {
            if (_database == null || _buffers == null)
                throw new InvalidOperationException("전투 서비스는 DB 초기화와 서비스 바인딩이 필요합니다.");
            if (_source == null)
                throw new InvalidOperationException("전투 초기화 입력 경로(G7)가 연결되지 않았습니다.");
        }

        private StageSelectionData ValidateCommittedSelection()
        {
            var current = _buffers.CurrentProgressReadSink;
            if (!current.HasMap || !current.HasSelection)
                throw new InvalidOperationException("G7: 전투 준비에는 확정된 챕터와 선택이 필요합니다.");
            var selected = current.CurrentStage;
            if (!_buffers.MapProgressReadSink.Maps.Any(map => map.MapKey == current.CurrentMapKey) ||
                _buffers.MapProgressReadSink.GetMap(current.CurrentMapKey).IsClear ||
                !_buffers.StageProgressReadSink.GetStages(current.CurrentMapKey).Any(node =>
                    node.NodeId == selected.NodeId && node.StageKey == selected.StageKey && !node.IsClear))
                throw new InvalidOperationException("클리어하지 않은 현재 노드와 챕터만 준비할 수 있습니다.");
            return selected;
        }

        private void ValidateCombatTarget(ChapterProgressionResult decision)
        {
            var root = _database.InGameQuery.StageTableQuery.GetStage(decision.StageKey.Value)
                ?? throw new InvalidOperationException("진입 대상의 스테이지 정의가 필요합니다.");
            root.Validate();
            if (root.StageKey != decision.StageKey.Value || root.MapKey != decision.ChapterKey ||
                root.StageData is not CombatStageDetail)
                throw new InvalidOperationException("진입 대상은 해당 챕터의 전투 스테이지여야 합니다.");
        }

        private BattleInitialization ReadCommittedDefinition()
        {
            var current = _buffers.CurrentProgressReadSink;
            if (!current.HasMap || !current.HasSelection)
                throw new InvalidOperationException("G7: 정의 조회에는 확정된 챕터와 스테이지 선택이 필요합니다.");
            return ReadDefinition(current.CurrentMapKey, new StageKey(current.CurrentStage.StageKey));
        }

        private BattleInitialization ReadDefinition(int chapterKey, StageKey stageKey)
            => _source.Read(_database, chapterKey, stageKey)
                ?? throw new InvalidOperationException("전투 정의 입력이 필요합니다.");

        private BattlePreparation CalculatePreparation(BattleInitialization initialization)
        {
            if (_buffers.CharacterReadSink.TryGetMainState(out var main) == false)
                throw new InvalidOperationException("보유 메인 지속 상태가 필요합니다.");

            var souls = ReadSoulCounts();
            var active = ReadCharacterFacts();

            return BattlePreparationCalculator.Prepare(initialization,
                _buffers.CharacterReadSink.Owned, main, souls,
                _characters.PlanInitialDraw(initialization.Stage.TimelineSeed), active);
        }

        public bool ProcessNextCommand()
        {
            EnsureBound();
            if (RunEnded) return false;
            if (!_buffers.CombatStateReadSink.HasBattle)
                return false;
            if (!_buffers.CombatViewReadSink.TryGetNextCommand(
                _buffers.CombatStateReadSink.HandledCommandNumber, out var command))
                return false;

            var input = ReadInput(
                command.Number, command.Data.EntityId, command.Data.Aim,
                ToCommandKind(command.Data.Kind), command.Data.Cell, command.Data.Rotation,
                path: command.Data.Path, movementContext: command.Data.MovementContext);
            if (RunEnded) return false;
            if (input.Kind == CombatCommandKind.Move || input.Kind == CombatCommandKind.ReturnToCheckpoint)
                if (!MovementContextCalculator.Matches(input.MovementContext, input, Owner.Player, _movementRules))
                    return Publish(MovementContextCalculator.RejectStale(input));
            return Publish(CombatCommandCalculator.Calculate(input, _movementRules));
        }

        public bool ProcessLatestPreview()
        {
            EnsureBound();
            if (!_buffers.CombatStateReadSink.HasBattle || !_buffers.CombatViewReadSink.HasPreview)
                return false;

            var request = _buffers.CombatViewReadSink.Preview;
            if (request.Number <= _buffers.CombatStateReadSink.AnsweredPreviewNumber)
                return false;

            var kind = request.Data.Kind switch
            {
                CombatViewPreviewKind.MainCharacterPlacement => CombatCommandKind.PlaceMainCharacter,
                CombatViewPreviewKind.UnitPlacement => CombatCommandKind.PlaceUnit,
                CombatViewPreviewKind.Move or CombatViewPreviewKind.MoveRange => CombatCommandKind.Move,
                CombatViewPreviewKind.ReturnToCheckpoint => CombatCommandKind.ReturnToCheckpoint,
                CombatViewPreviewKind.Attack or CombatViewPreviewKind.AttackRange => CombatCommandKind.Attack,
                _ => throw new ArgumentOutOfRangeException(nameof(request))
            };
            var input = ReadInput(
                request.Number, request.Data.EntityId, request.Data.Aim,
                kind, request.Data.Cell, request.Data.Rotation, path: request.Data.Path);
            var result = request.Data.Kind switch
            {
                CombatViewPreviewKind.AttackRange => CombatPreviewResult.ForAttackRange(request.Number, CombatCommandCalculator.GetAttackRange(input)),
                CombatViewPreviewKind.Attack => CombatPreviewResult.ForAttack(request.Number, CombatCommandCalculator.Preview(input, _movementRules)),
                CombatViewPreviewKind.MoveRange => CombatPreviewResult.ForMoveRange(request.Number, CombatMovementCalculator.Range(input)),
                CombatViewPreviewKind.Move => CombatPreviewResult.ForMove(request.Number, CombatMovementCalculator.Preview(input, _movementRules),
                    MovementContextCalculator.Capture(input, Owner.Player, _movementRules)),
                CombatViewPreviewKind.ReturnToCheckpoint => CombatPreviewResult.ForCheckpointReturn(request.Number,
                    CheckpointReturnCalculator.Preview(input), MovementContextCalculator.Capture(input, Owner.Player, _movementRules)),
                _ => CombatPreviewResult.ForPlacement(request.Number, CombatPlacementCalculator.Preview(input))
            };
            if (!_buffers.CombatStateReadSink.HasBattle || _buffers.CombatStateReadSink.BattleInstanceId != input.BattleInstanceId ||
                !_buffers.CombatViewReadSink.HasPreview || _buffers.CombatViewReadSink.Preview.Number != request.Number) return false;
            _buffers.BattleWriteSink.PublishPreview(result);
            return true;
        }

        public bool AdvanceCombat()
        {
            EnsureBound();
            if (RunEnded) return false;
            if (!_buffers.CombatStateReadSink.HasBattle || _buffers.CombatStateReadSink.Phase != CombatFlowPhase.Automatic)
                return false;

            var input = ReadInput(0, _buffers.CombatStateReadSink.Current.CommandState.ActiveActorId, default,
                CombatCommandKind.EndUnitTurn, processingKind: CombatProcessingKind.Advance);
            if (RunEnded) return false;
            return Publish(CombatCommandCalculator.Advance(input, _movementRules));
        }

        #endregion

        #region 전투 조회와 상태 반영

        public CombatDrawAvailability GetDrawAvailability()
        {
            EnsureBound();
            return CombatDrawCalculator.Availability(ReadInput(1, EntityId.Empty, default, CombatCommandKind.DrawCharacter));
        }

        public CombatTimelineEntrySnapshot[] GetTimeline(int maxEntries = 10)
        {
            EnsureBound();
            return CombatTurnCalculator.Forecast(ReadInput(1, EntityId.Empty, default, CombatCommandKind.EndUnitTurn), maxEntries);
        }

        public void ApplyHealth(EntityId entityId, int currentHealth)
        {
            EnsureRunInProgress();
            var character = ReadCharacter(entityId);
            _buffers.CharacterReadSink.TryGetMainState(out var main);
            _buffers.BattleWriteSink.Apply(CharacterSettlementCalculator.SetHealth(character, main, currentHealth));
        }

        public void ApplyPersistentMain(MainCharacterPersistentState final)
        {
            EnsureRunInProgress();
            var main = ReadMain();
            _buffers.CharacterReadSink.TryGetMainState(out var persistent);
            _buffers.BattleWriteSink.Apply(CharacterSettlementCalculator.SetPersistentMain(main, persistent, final));
        }

        public void FinalizeTurn(EntityId actorId, SoulActionChanges settled, ArtifactLoadoutSnapshot loadout)
        {
            EnsureRunInProgress();
            var turn = CharacterSettlementCalculator.EndTurn(ReadCharacter(actorId), settled);
            if (!_buffers.CombatStateReadSink.HasBattle)
                throw new InvalidOperationException("활성 전투가 없습니다.");

            var projected = new List<CharacterCombatFacts>();
            foreach (var character in _buffers.CharacterReadSink.Characters)
            {
                var final = character.ToFacts();
                foreach (var change in turn.Characters.Characters)
                {
                    if (change.Final.EntityId == final.EntityId)
                    {
                        final = change.Final;
                        break;
                    }
                }
                projected.Add(final);
            }
            var outcome = BattleOutcomeCalculator.Resolve(projected, _buffers.CombatStateReadSink.Current.Structures);
            if (outcome.IsStageEnded)
            {
                if (!Complete(outcome, loadout, turn)) EnsureRunInProgress();
            }
            else
                _buffers.BattleWriteSink.Apply(turn);
        }

        public bool ResolveBattle(ArtifactLoadoutSnapshot loadout)
        {
            EnsureBound();
            if (_buffers.CombatStateReadSink.Completion != null)
                return false;
            if (RunEnded) return false;
            if (!_buffers.CombatStateReadSink.HasBattle)
                throw new InvalidOperationException("활성 전투가 없습니다.");

            var characters = ReadCharacterFacts();
            var outcome = BattleOutcomeCalculator.Resolve(characters, _buffers.CombatStateReadSink.Current.Structures);
            return outcome.IsStageEnded && Complete(outcome, loadout);
        }

        public BattleAbandonResult AbandonBattle()
        {
            EnsureBound();
            if (_buffers.CombatStateReadSink.Completion != null)
                return BattleAbandonResult.AlreadyCompleted;
            if (RunEnded) return BattleAbandonResult.RunEnded;
            if (!_buffers.CombatStateReadSink.HasBattle) return BattleAbandonResult.NoActiveBattle;

            // DB 정의 오류는 완료 계산 전에 예외로 드러나며 아무것도 바꾸지 않는다.
            ArtifactLoadoutSnapshot loadout = ReadArtifactLoadout();
            if (Complete(new StageOutcomeSnapshot(StageOutcome.Defeat, StageOutcomeReason.VoluntaryAbandonment), loadout))
                return BattleAbandonResult.Abandoned;
            // 완료 판단 중 Run이 끝났거나 같은 완료가 이미 기록된 경우다.
            return RunEnded ? BattleAbandonResult.RunEnded : BattleAbandonResult.AlreadyCompleted;
        }

        public void EndBattle()
        {
            EnsureBound();
            if (_buffers.CombatStateReadSink.HasBattle && _buffers.CombatStateReadSink.Completion == null)
                throw new InvalidOperationException("전투 완료 결과를 기록한 뒤 정리해야 합니다.");
            _buffers.BattleWriteSink.EndBattle();
            _definitions = null;
        }

        #endregion

        #region 회복 스테이지 처리

        public bool Recover(RecoveryOptionId optionId)
        {
            EnsureBound();
            EnsureRunInProgress();
            var recovery = _buffers.CurrentProgressReadSink.Recovery;
            if (recovery == null || !recovery.TryGetOption(optionId, out var option))
                throw new InvalidOperationException("현재 회복 선택지가 아닙니다.");
            if (_buffers.CombatStateReadSink.HasBattle)
                throw new InvalidOperationException("전투 중에는 회복할 수 없습니다.");
            if (recovery.AppliedOptionId.HasValue)
            {
                if (recovery.AppliedOptionId == optionId)
                    return false;
                throw new InvalidOperationException("이미 다른 회복 선택지를 소비했습니다.");
            }
            if (!_buffers.CharacterReadSink.TryGetMainState(out var main))
                throw new InvalidOperationException("메인 지속 상태가 필요합니다.");
            // 회복 최대 체력은 메인의 영혼 수를 쓴다(Core 규칙). 출처를 결정에 담아 Buffer가 원본 일치만 확인하게 한다.
            var mainOwned = _buffers.CharacterReadSink.Owned.Single(o => o.Role == CharacterOwnershipRole.Main);
            int count = _buffers.CharacterReadSink.GetSoulCount(mainOwned.Id);
            var decision = CharacterRecoveryCalculator.Plan(main, mainOwned.Id, count, option, _buffers.CurrencyReadSink.Amount);
            var selection = _buffers.CurrentProgressReadSink.CurrentStage;
            return _buffers.RecoveryStageWriteSink.PublishRecovery(new StageNodeId(selection.NodeId),
                new StageKey(selection.StageKey), decision);
        }

        #endregion

        #region 명령 입력과 Buffer 사실 조회

        private CombatCommandInput ReadInput(int number, EntityId actor, AttackAim aim,
            CombatCommandKind kind = CombatCommandKind.Attack, CellCoord cell = default, Rotation4 rotation = Rotation4.R0,
            CombatProcessingKind processingKind = CombatProcessingKind.Command, MovementPath path = null,
            MovementPreviewContext movementContext = null)
        {
            if (!_buffers.CombatStateReadSink.HasBattle)
                throw new InvalidOperationException("활성 전투가 없습니다.");
            if (!_buffers.CharacterReadSink.TryGetMainState(out var main))
                throw new InvalidOperationException("메인 지속 상태가 필요합니다.");
            if (_definitions == null && _source != null)
            {
                if (_database == null)
                    throw new InvalidOperationException("전투 정의 조회에는 DB 초기화가 필요합니다.");
                _definitions = ReadCommittedDefinition();
            }

            var characters = ReadCharacterFacts();
            var souls = ReadSoulCounts();
            var loadout = ReadArtifactLoadout();
            var battle = _buffers.CombatStateReadSink.Current;
            // 환경 원본은 설치된 CommandState에서 읽는다. 정의를 다시 읽어 소비된 원본을 되살리지 않는다.
            if (kind == CombatCommandKind.EndUnitTurn && actor.IsEmpty)
                actor = battle.CommandState.ActiveActorId;

            var context = new CombatBattleFacts(battle.ReserveCapacity, battle.NightLength, battle.TimelineSeed,
                battle.PlayerDeploymentDepth, battle.EnemyDeploymentDepth, battle.DrawOrder, battle.DrawCursor, battle.NextEntityId,
                battle.StageGold, battle.ManualDrawGoldCost, battle.RailCells, battle.StageGimmicks, _definitions?.Categories);
            return new CombatCommandInput(number, kind, actor,
                aim, battle.CommandState, characters,
                _definitions?.AttackProfiles ?? Array.Empty<CombatAttackProfile>(), main, _buffers.CharacterReadSink.Owned,
                souls, _buffers.CombatStateReadSink.SoulField, loadout, cell, rotation, context, processingKind,
                path, movementContext, _buffers.CombatStateReadSink.BattleInstanceId,
                _buffers.ArtifactReadSink.Current.PlacedItems.Select(i => new ArtifactItemState(i.ArtifactId, i.InstanceId,
                    true, default, default, Array.Empty<CellCoord>())).ToArray(), battle.EnvironmentBindings, battle.ObstacleContents);
        }

        private CharacterCombatFacts[] ReadCharacterFacts()
        {
            return _buffers.CharacterReadSink.Characters.Select(character => character.ToFacts()).ToArray();
        }

        private Dictionary<CharacterInstanceId, int> ReadSoulCounts()
        {
            return _buffers.CharacterReadSink.Owned.ToDictionary(
                owned => owned.Id,
                owned => _buffers.CharacterReadSink.GetSoulCount(owned.Id));
        }

        private ArtifactLoadoutSnapshot ReadArtifactLoadout()
        {
            var effects = new List<ArtifactPlacedEffectSnapshot>();
            foreach (var item in _buffers.ArtifactReadSink.Current.PlacedItems.OrderBy(i => i.InstanceId))
            {
                if (_database == null)
                    throw new InvalidOperationException("배치 Artifact 정의에는 DB 초기화가 필요합니다.");
                var definition = _database.InGameQuery.ArtifactDefinitionTableQuery.GetDefinition(item.ArtifactId);
                foreach (var effect in definition.Effects)
                    effects.Add(new ArtifactPlacedEffectSnapshot(item.ArtifactId, item.InstanceId, effect));
            }
            return new ArtifactLoadoutSnapshot(0, effects.ToArray());
        }

        private CharacterCombatFacts ReadCharacter(EntityId id)
        {
            EnsureBound();
            if (!_buffers.CharacterReadSink.TryGet(id, out var character))
                throw new InvalidOperationException("활성 캐릭터를 찾을 수 없습니다.");
            return character.ToFacts();
        }

        private CharacterCombatFacts ReadMain()
        {
            EnsureBound();
            foreach (var character in _buffers.CharacterReadSink.Characters)
            {
                if (character.OwnershipRole == CharacterOwnershipRole.Main)
                    return character.ToFacts();
            }
            throw new InvalidOperationException("활성 메인 캐릭터가 필요합니다.");
        }

        private static CombatCommandKind ToCommandKind(CombatViewCommandKind kind) => kind switch
        {
            CombatViewCommandKind.PlaceMainCharacter => CombatCommandKind.PlaceMainCharacter,
            CombatViewCommandKind.PlaceUnit => CombatCommandKind.PlaceUnit,
            CombatViewCommandKind.ReturnUnit => CombatCommandKind.ReturnUnit,
            CombatViewCommandKind.EnterCombat => CombatCommandKind.EnterCombat,
            CombatViewCommandKind.Move => CombatCommandKind.Move,
            CombatViewCommandKind.ReturnToCheckpoint => CombatCommandKind.ReturnToCheckpoint,
            CombatViewCommandKind.Attack => CombatCommandKind.Attack,
            CombatViewCommandKind.EndUnitTurn => CombatCommandKind.EndUnitTurn,
            CombatViewCommandKind.DrawCharacter => CombatCommandKind.DrawCharacter,
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };

        private void EnsureBound()
        {
            if (_buffers == null)
                throw new InvalidOperationException("전투 서비스는 Buffer 바인딩이 필요합니다.");
        }

        private bool RunEnded => _buffers.RunProgressReadSink.Outcome != RunOutcome.InProgress;

        private void EnsureRunInProgress()
        {
            EnsureBound();
            if (RunEnded) throw new InvalidOperationException("RunEnded: 이미 종료된 Run입니다.");
        }

        #endregion

        #region 계산 결과 게시

        private bool Publish(CombatCommandTransition transition)
        {
            if (transition.Accepted && transition.Outcome.IsStageEnded)
            {
                if (_progression == null)
                    throw new InvalidOperationException("진행 판단 서비스가 필요합니다.");
                var progress = _progression.PlanStageResolution(_buffers.CurrentProgressReadSink.CurrentMapKey, transition.Outcome.Outcome);
                if (!progress.Accepted && progress.FailureReason == ChapterProgressionFailureReason.RunEnded)
                    return false;
                if (!progress.Accepted)
                    throw new InvalidOperationException($"스테이지 완료가 거절되었습니다: {progress.FailureReason}");
                var settled = new SoulActionChanges(transition.Characters, transition.Field, transition.ConsumedSoulIds);
                // 종료 회복과 사망 영혼은 계산기에 이미 합쳐져 있으므로 완료 기록만 연결한다.
                var completion = new BattleCompletionChanges(settled, transition.Outcome, progress);
                transition = transition.WithCompletion(completion);
            }
            _buffers.BattleWriteSink.Apply(transition);
            return true;
        }

        private bool Complete(StageOutcomeSnapshot outcome, ArtifactLoadoutSnapshot loadout, SoulActionChanges settled = null)
        {
            if (_progression == null)
                throw new InvalidOperationException("진행 판단 서비스가 필요합니다.");

            var main = ReadMain();
            _buffers.CharacterReadSink.TryGetMainState(out var persistent);
            var progress = _progression.PlanStageResolution(_buffers.CurrentProgressReadSink.CurrentMapKey, outcome.Outcome);
            if (!progress.Accepted && progress.FailureReason == ChapterProgressionFailureReason.RunEnded)
                return false;
            if (!progress.Accepted)
                throw new InvalidOperationException($"스테이지 완료가 거절되었습니다: {progress.FailureReason}");

            var calculator = new BattleCompletionCalculator(loadout);
            var changes = settled == null
                ? calculator.Complete(main, persistent, outcome, progress)
                : calculator.Complete(main, persistent, outcome, progress, settled);
            return _buffers.BattleWriteSink.CompleteBattle(changes);
        }

        #endregion
    }
}
