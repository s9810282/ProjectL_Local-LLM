using System;
using System.Collections.Generic;
using System.Linq;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Combat
{
    #region 서비스

    public interface ICombatResultReadSink : IServiceModule
    {
        /// <summary>결과가 없으면 false와 null. 거절된 결과도 존재하는 결과다.</summary>
        bool TryGetLatest(out CombatResult result);
    }

    /// <summary>현재 전투의 처리 이력. 전투 종료 후 유지하며 다음 전투 준비 시 비운다.</summary>
    public interface ICombatResultHistoryReadSink : IServiceModule
    {
        IReadOnlyList<CombatResult> Results { get; }
    }

    public interface ICombatStateBufferReadSink : IServiceModule
    {
        bool HasBattle { get; }
        long BattleInstanceId { get; }
        long QueryRevision { get; }
        CombatStateData Current { get; }
        CombatFlowPhase Phase { get; }
        SoulFieldSnapshot SoulField { get; }
        BattleCompletionChanges Completion { get; }
        int LatestResultSequence { get; }
        int HandledCommandNumber { get; }
        int AnsweredPreviewNumber { get; }
        bool TryGetLatestPreview(out CombatPreviewResult preview);
    }

    internal interface ICombatStateBufferStore : IServiceModule
    {
        PreparedCombatState PrepareBattle(BattlePreparation preparation);
        void InstallBattle(PreparedCombatState prepared);
        SoulFieldSnapshot PrepareSoulField(SoulFieldChange change);
        void InstallSoulField(SoulFieldSnapshot field);
        void InstallCompletion(BattleCompletionChanges completion);
        /// <summary>새 회차 초기화 때만 완료 기록을 비운다. 일반 전투 정리는 기록을 보존한다.</summary>
        void ClearCompletion();
        PreparedCombatCommand PrepareCommand(CombatCommandTransition transition, IReadOnlyList<CharacterCombatFacts> characters);
        void InstallCommand(PreparedCombatCommand prepared);
        void ValidateQueryRevision(long expected);
        (CombatStateData State, long ExpectedRevision, long FinalRevision) PrepareMovementUndoInvalidation(
            IReadOnlyList<CharacterCombatFacts> before, IReadOnlyList<CharacterCombatFacts> after,
            CharacterChanges changes, SoulFieldSnapshot field, bool completion = false);
        void InstallMovementUndoState((CombatStateData State, long ExpectedRevision, long FinalRevision) prepared);
        void InstallPreview(CombatPreviewResult preview);
        void EndBattle();
    }

    #endregion

    #region 시스템 모듈

    internal sealed class CombatStateBuffer :
        ICombatStateBufferReadSink,
        ICombatResultReadSink,
        ICombatResultHistoryReadSink,
        ICombatStateBufferStore,
        IExportSystemServices
    {
        private CombatStateData _current;
        // 준비와 게시를 분리하여 결과 번호도 게임 상태와 함께 설치한다.
        private CombatResult _latestResult = null;
        private CombatPreviewResult _latestPreview;
        private readonly List<CombatResult> _results = new();
        private IReadOnlyList<CombatResult> _readOnlyResults;
        public IReadOnlyList<CombatResult> Results => _readOnlyResults ??= _results.AsReadOnly();

        public bool HasBattle => _current != null;
        public long BattleInstanceId { get; private set; }
        public long QueryRevision { get; private set; }
        public CombatStateData Current => _current;
        public CombatFlowPhase Phase => _current?.Phase ?? CombatFlowPhase.None;
        public SoulFieldSnapshot SoulField { get; private set; } = SoulFieldSnapshot.Empty;
        public BattleCompletionChanges Completion { get; private set; }
        public int LatestResultSequence { get; private set; }
        public int HandledCommandNumber { get; private set; }
        public int AnsweredPreviewNumber { get; private set; }

        public bool TryGetLatest(out CombatResult result)
        {
            result = _latestResult;
            return result != null;
        }

        public bool TryGetLatestPreview(out CombatPreviewResult preview)
        {
            preview = _latestPreview;
            return preview != null;
        }

        void ICombatStateBufferStore.InstallPreview(CombatPreviewResult preview)
        {
            _latestPreview = preview;
            AnsweredPreviewNumber = preview.RequestNumber;
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ICombatStateBufferReadSink>(this);
            sink.AddService<ICombatResultReadSink>(this);
            sink.AddService<ICombatResultHistoryReadSink>(this);
            sink.AddService<ICombatStateBufferStore>(this);
        }

        PreparedCombatState ICombatStateBufferStore.PrepareBattle(BattlePreparation preparation)
        {
            if (preparation == null)
                throw new ArgumentNullException(nameof(preparation));
            if (_current != null)
                throw new InvalidOperationException("이미 준비된 전투가 있습니다.");

            preparation.CommandState.Gimmicks.ValidateDefinitions(preparation.Combat.StageGimmicks,
                preparation.Combat.BoardWidth, preparation.Combat.BoardHeight);
            if (!preparation.CommandState.TraitTurn.SameValues(TraitTurnFacts.Empty))
                throw new InvalidOperationException("새 전투는 특성 행동 횟수 없이 시작해야 합니다.");
            if (!preparation.CommandState.MovementUndo.SameValues(MovementUndoState.Empty))
                throw new InvalidOperationException("새 전투의 이동 취소 기록은 비어 있어야 합니다.");
            if (!preparation.CommandState.Rewards.SameValues(CombatRewardFacts.Empty))
                throw new InvalidOperationException("새 전투의 보상 사실은 비어 있어야 합니다.");
            if (preparation.CommandState.Turn.ControllerTurnSequence != 0 ||
                preparation.CommandState.Turn.CompletedPlayerTurnCount != 0 ||
                preparation.CommandState.Turn.CompletedControllerTurnCount != 0 ||
                preparation.CommandState.Turn.ControllerPhase != CombatControllerPhase.None ||
                preparation.Characters.Any(character => character.ActionUse != UnitActionUse.Deferred))
                throw new InvalidOperationException("새 전투는 진영 진행 전의 대기 행동 상태로 준비되어야 합니다.");
            ValidatePlacement(preparation.CommandState, preparation.Characters, preparation.Combat.ReserveCapacity);
            if (preparation.Combat.ObstacleContents.Count > 0 || preparation.Structures.Any(s => ObstacleContentProfile.IsNativeObstacleKey(s.Definition.Key)))
                ObstacleContentProfile.Validate(preparation.Combat.ObstacleContents, preparation.Combat.EnvironmentBindings, preparation.Structures);
            return new PreparedCombatState(new CombatStateData(preparation), checked(BattleInstanceId + 1));
        }

        void ICombatStateBufferStore.InstallBattle(PreparedCombatState prepared)
        {
            if (prepared == null)
                throw new ArgumentNullException(nameof(prepared));
            if (_current != null)
                throw new InvalidOperationException("이미 준비된 전투가 있습니다.");

            _current = prepared.State;
            BattleInstanceId = prepared.BattleInstanceId;
            QueryRevision = 1;
            _results.Clear();
            Completion = null;
            _latestPreview = null;
        }

        SoulFieldSnapshot ICombatStateBufferStore.PrepareSoulField(SoulFieldChange change)
        {
            if (change == null) throw new ArgumentNullException(nameof(change));
            if (_current == null || !SoulField.SameValues(change.Expected))
                throw new InvalidOperationException("영혼 필드 원본이 현재 전투와 다릅니다.");
            foreach (SoulFieldFacts entry in change.Final.Entries)
            {
                if (entry.Cell.X < 0 || entry.Cell.X >= _current.BoardWidth ||
                    entry.Cell.Y < 0 || entry.Cell.Y >= _current.BoardHeight)
                    throw new InvalidOperationException("영혼 위치가 보드 밖입니다.");
                if (entry.Id >= SoulField.NextSoulId) continue;
                SoulFieldFacts original = null;
                foreach (SoulFieldFacts previous in SoulField.Entries)
                    if (previous.Id == entry.Id) { original = previous; break; }
                if (original == null || !original.Origin.Equals(entry.Origin) ||
                    !original.Cell.Equals(entry.Cell) || original.HealAmount != entry.HealAmount ||
                    entry.RemainingUnitTurns > original.RemainingUnitTurns)
                    throw new InvalidOperationException("기존 영혼을 재생성하거나 원본을 바꿀 수 없습니다.");
            }
            return change.Final;
        }

        void ICombatStateBufferStore.InstallSoulField(SoulFieldSnapshot field)
        {
            // PrepareSoulField 및 캐릭터 검증 이후에는 실패 가능한 작업을 하지 않는다.
            SoulField = field;
        }

        public void EndBattle()
        {
            _current = null;
            QueryRevision = 0;
            SoulField = SoulFieldSnapshot.Empty;
            _latestPreview = null;
        }

        void ICombatStateBufferStore.InstallCompletion(BattleCompletionChanges completion) => Completion = completion;

        void ICombatStateBufferStore.ClearCompletion() => Completion = null;

        void ICombatStateBufferStore.ValidateQueryRevision(long expected)
        {
            if (QueryRevision != expected) throw new InvalidOperationException("게시 준비 이후 조회 원본이 바뀌었습니다.");
        }

        (CombatStateData State, long ExpectedRevision, long FinalRevision) ICombatStateBufferStore.PrepareMovementUndoInvalidation(
            IReadOnlyList<CharacterCombatFacts> before, IReadOnlyList<CharacterCombatFacts> after,
            CharacterChanges changes, SoulFieldSnapshot field, bool completion)
        {
            var actors = CombatChangeEvidenceReader.ReadCharacterChanges(before, after);
            var souls = CombatChangeEvidenceReader.ReadChanges(new SoulFieldChange(SoulField, field));
            bool changed = completion || actors.AffectedEntityIds.Count != 0 || !SoulField.SameValues(field) ||
                changes.Main != null && !changes.Main.Expected.SameValues(changes.Main.Final) ||
                changes.Souls.Any(c => c.ExpectedCount != c.FinalCount);
            var undo = completion ? MovementUndoState.Empty :
                _current.CommandState.MovementUndo.WithoutAffected(actors).WithoutAffected(souls);
            var state = ReferenceEquals(undo, _current.CommandState.MovementUndo) ? _current :
                new CombatStateData(_current, _current.CommandState.With(movementUndo: undo));
            return (state, QueryRevision, changed ? checked(QueryRevision + 1) : QueryRevision);
        }

        void ICombatStateBufferStore.InstallMovementUndoState((CombatStateData State, long ExpectedRevision, long FinalRevision) prepared)
        {
            _current = prepared.State;
            QueryRevision = prepared.FinalRevision;
        }

        PreparedCombatCommand ICombatStateBufferStore.PrepareCommand(CombatCommandTransition transition,
            IReadOnlyList<CharacterCombatFacts> characters)
        {
            // 모든 전투는 1 이상의 진입 식별자를 가진다. 0을 포함해 다른 식별자의 명령은 이전 전투의 결과로 보고 거절한다.
            if (_current == null || transition.Input.BattleInstanceId != BattleInstanceId ||
                !_current.CommandState.SameValues(transition.Input.State) ||
                !_current.EnvironmentBindings.SameValues(transition.Input.EnvironmentBindings) ||
                !ObstacleContentProfile.SameValues(_current.ObstacleContents, transition.Input.ObstacleContents))
                throw new InvalidOperationException("명령의 전투 원본이 다릅니다.");
            var before = _current.CommandState;
            var final = transition.FinalState;
            CombatChangeEvidenceReader.ValidateGimmickChanges(transition);
            if (!before.Objective.SameValues(final.Objective))
                throw new InvalidOperationException("전투 중 승리 목표를 바꿀 수 없습니다.");
            if (before.BoardWidth != final.BoardWidth || before.BoardHeight != final.BoardHeight ||
                before.Structures.Count != final.Structures.Count)
                throw new InvalidOperationException("공격으로 보드와 구조물 식별자를 바꿀 수 없습니다.");
            for (int i = 0; i < before.Structures.Count; i++)
            {
                var a = before.Structures[i]; var b = final.Structures[i];
                if (a.EntityId != b.EntityId || a.Definition != b.Definition || a.Owner != b.Owner ||
                    a.Rotation != b.Rotation || a.Health.MaxHealth != b.Health.MaxHealth ||
                    !PreservesStructurePosition(a, b, transition.Input.EnvironmentBindings))
                    throw new InvalidOperationException("공격으로 구조물 원본을 바꿀 수 없습니다.");
            }
            ValidateRewards(transition);
            MovementUndoPolicy.ValidatePublication(transition, characters);
            ValidateActionState(transition, characters);
            ValidatePlacement(final, characters, _current.ReserveCapacity);
            var validatedBindings = ValidateEnvironmentChange(transition);
            var bindings = transition.EnvironmentBindings == null ? _current.EnvironmentBindings : validatedBindings;
            int sequence = checked(LatestResultSequence + 1);
            ValidateCreation(before, final, transition.Characters.Created, characters);
            var result = new CombatResult(sequence, transition.Input.ProcessingKind, transition.Input.Number,
                transition.Accepted, transition.Reason, transition.Attack, transition.Outcome,
                transition.Input.Kind, transition.ActionUseSettlement?.ActorId ?? transition.Input.ActorId, transition.Movement, transition.Events,
                transition.PathMovement, transition.BlockedContacts, BattleInstanceId,
                final.Turn.ControllerTurnSequence, transition.ActionUseSettlement, transition.UndoMovement);
            return new PreparedCombatCommand(new CombatStateData(_current, final, bindings), result,
                QueryRevision, transition.Accepted ? checked(QueryRevision + 1) : QueryRevision);
        }

        /// <summary>공유 예산·행동 사용·진영 식별자의 정산 근거를 검증한다. 가격이나 행동 후보는 계산하지 않는다.</summary>
        private static void ValidateActionState(CombatCommandTransition transition, IReadOnlyList<CharacterCombatFacts> characters)
        {
            var input = transition.Input;
            var before = input.State;
            var final = transition.FinalState;
            var previousTurn = before.Turn;
            var nextTurn = final.Turn;
            var settlement = transition.ActionUseSettlement;
            if (!transition.Accepted)
            {
                if (!before.SameValues(final) || transition.Characters.Characters.Count != 0 ||
                    transition.Characters.Created.Count != 0 || transition.Characters.Owned.Count != 0 ||
                    transition.Characters.Souls.Count != 0 || transition.Characters.Main != null ||
                    !transition.Field.Expected.SameValues(transition.Field.Final) || settlement != null)
                    throw new InvalidOperationException("거절된 명령은 게임 사실이나 행동 정산을 바꿀 수 없습니다.");
                return;
            }

            bool playerStart = previousTurn.ControllerPhase == CombatControllerPhase.StartingPlayer &&
                nextTurn.ControllerPhase == CombatControllerPhase.Player;
            bool enemyStart = previousTurn.ControllerPhase == CombatControllerPhase.StartingEnemy &&
                nextTurn.ControllerPhase == CombatControllerPhase.Enemy;
            bool sideStart = playerStart || enemyStart;
            bool playerEnd = input.ProcessingKind == CombatProcessingKind.Command && input.Kind == CombatCommandKind.EndPlayerTurn &&
                previousTurn.ControllerPhase == CombatControllerPhase.Player;
            bool enemyEnd = input.ProcessingKind == CombatProcessingKind.Advance && input.Kind == CombatCommandKind.CompleteEnemyTurn &&
                previousTurn.StagePhase == StagePhase.Combat && previousTurn.ControllerPhase == CombatControllerPhase.Enemy &&
                (nextTurn.ControllerPhase == CombatControllerPhase.StartingPlayer || nextTurn.StagePhase == StagePhase.Deployment);
            if (nextTurn.ControllerTurnSequence != checked(previousTurn.ControllerTurnSequence + (sideStart ? 1 : 0)) ||
                nextTurn.CompletedPlayerTurnCount != checked(previousTurn.CompletedPlayerTurnCount + (playerEnd ? 1 : 0)) ||
                nextTurn.CompletedControllerTurnCount != checked(previousTurn.CompletedControllerTurnCount + (playerEnd || enemyEnd ? 1 : 0)))
                throw new InvalidOperationException("진영 시작·플레이어 종료 식별자와 누적값이 정산 경계와 다릅니다.");
            if (sideStart && (input.ProcessingKind != CombatProcessingKind.Advance || input.Kind != CombatCommandKind.StartControllerTurn))
                throw new InvalidOperationException("진영 시작은 자동 시작 처리에서만 설치할 수 있습니다.");
            if (input.ProcessingKind == CombatProcessingKind.Command &&
                (input.Kind == CombatCommandKind.Move || input.Kind == CombatCommandKind.Attack || input.Kind == CombatCommandKind.EndPlayerTurn || input.Kind == CombatCommandKind.UndoMove) &&
                (input.ExpectedBattleInstanceId != input.BattleInstanceId ||
                    input.ExpectedControllerTurnSequence != previousTurn.ControllerTurnSequence))
                throw new InvalidOperationException("수락된 명령의 전투·진영 턴 식별자가 원본과 다릅니다.");
            if (nextTurn.NightStartPlayerTurnCount != previousTurn.NightStartPlayerTurnCount &&
                (input.Kind != CombatCommandKind.EnterCombat || previousTurn.StagePhase != StagePhase.Deployment ||
                    nextTurn.NightStartPlayerTurnCount != nextTurn.CompletedPlayerTurnCount))
                throw new InvalidOperationException("밤 시작 기준은 배치에서 전투에 들어갈 때만 갱신할 수 있습니다.");

            if (settlement != null)
            {
                if (sideStart || playerEnd || previousTurn.ControllerTurnSequence != nextTurn.ControllerTurnSequence)
                    throw new InvalidOperationException("일반 행동 사용과 진영 경계는 한 정산으로 섞을 수 없습니다.");
                var actorBefore = input.Characters.SingleOrDefault(character => character.EntityId == settlement.ActorId);
                var actorAfter = characters.SingleOrDefault(character => character.EntityId == settlement.ActorId);
                // 이동은 미사용에서만, 공격은 미사용·이동함에서 시작한다(UnitActionUseRules).
                bool startable = input.Kind == CombatCommandKind.UndoMove && transition.UndoMovement != null &&
                    settlement.BeforeActionUse == UnitActionUse.Moved && settlement.FinalActionUse == UnitActionUse.Available ||
                    (settlement.FinalActionUse == UnitActionUse.Moved
                    ? UnitActionUseRules.CanStartMove(settlement.BeforeActionUse)
                    : settlement.FinalActionUse == UnitActionUse.Attacked &&
                      UnitActionUseRules.CanStartAttack(settlement.BeforeActionUse));
                if (actorBefore == null || actorAfter == null || actorBefore.ActionUse != settlement.BeforeActionUse ||
                    actorAfter.ActionUse != settlement.FinalActionUse || !startable)
                    throw new InvalidOperationException("행동 사용 정산과 행위자의 원본·최종 사실이 다릅니다.");
                if (settlement.FinalActionUse == UnitActionUse.Moved && transition.PathMovement == null ||
                    settlement.FinalActionUse == UnitActionUse.Attacked && !transition.Attack.HasValue)
                    throw new InvalidOperationException("행동 사용 정산에는 일치하는 이동·공격 결과가 필요합니다.");
                bool playerCommand = input.ProcessingKind == CombatProcessingKind.Command;
                if (playerCommand
                    ? actorBefore.Owner != Owner.Player || previousTurn.ControllerPhase != CombatControllerPhase.Player ||
                      input.ActorId != settlement.ActorId ||
                      (input.Kind != CombatCommandKind.Move && input.Kind != CombatCommandKind.Attack && input.Kind != CombatCommandKind.UndoMove)
                    : input.ProcessingKind != CombatProcessingKind.Advance || actorBefore.Owner != Owner.Enemy ||
                      previousTurn.ControllerPhase != CombatControllerPhase.Enemy)
                    throw new InvalidOperationException("행동 정산의 처리 출처·행위자·진영이 다릅니다.");
                if (transition.Attack.HasValue)
                {
                    var attack = transition.Attack.Value;
                    if (attack.AttackerId != settlement.ActorId || attack.ActionUseSettlement == null ||
                        attack.ActionUseSettlement.BeforeActionUse != settlement.BeforeActionUse ||
                        attack.ActionUseSettlement.FinalActionUse != settlement.FinalActionUse)
                        throw new InvalidOperationException("공격 결과와 행동 사용 정산이 다릅니다.");
                }
            }
            else if (input.Kind == CombatCommandKind.Move || input.Kind == CombatCommandKind.Attack ||
                transition.PathMovement != null || transition.Attack.HasValue)
                throw new InvalidOperationException("수락된 일반 행동에는 행동 사용 정산이 필요합니다.");

            var finalById = characters.ToDictionary(character => character.EntityId);
            foreach (var character in input.Characters)
            {
                var after = finalById[character.EntityId];
                if (settlement != null && character.EntityId == settlement.ActorId) continue;
                bool reset = sideStart && character.Owner == nextTurn.Controller &&
                    !character.Health.IsDead && character.Area == CharacterRuntimeArea.Field;
                bool deploymentChange = final.Turn.StagePhase == StagePhase.Deployment &&
                    (before.Turn.StagePhase != StagePhase.Deployment || character.Area != after.Area || character.Anchor != after.Anchor);
                bool enteringCombat = input.Kind == CombatCommandKind.EnterCombat &&
                    before.Turn.StagePhase == StagePhase.Deployment && final.Turn.StagePhase == StagePhase.Combat &&
                    !after.Health.IsDead && after.Area == CharacterRuntimeArea.Field;
                if (reset ? after.ActionUse != UnitActionUse.Available :
                    enteringCombat ? after.ActionUse != UnitActionUse.Deferred :
                    deploymentChange ? after.ActionUse != character.ActionUse && after.ActionUse != UnitActionUse.Deferred :
                    after.ActionUse != character.ActionUse)
                    throw new InvalidOperationException("진영 시작·배치 변경·행동 정산 밖에서 행동 사용 상태를 바꿀 수 없습니다.");
            }
        }

        /// <summary>참여자 확정의 출처와 보상 사실의 저장 연속성을 검사합니다. 대상 선택 규칙은 Core가 소유합니다.</summary>
        private static void ValidateRewards(CombatCommandTransition transition)
        {
            var before = transition.Input.State.Rewards;
            var final = transition.FinalState.Rewards;
            if (final.KillGoldEarned < before.KillGoldEarned ||
                (!transition.Accepted && !before.SameValues(final)))
                throw new InvalidOperationException("보상 사실을 되돌리거나 거절된 명령에서 변경할 수 없습니다.");
            if (before.ParticipantsCaptured && (!final.ParticipantsCaptured ||
                !before.ClearParticipantIds.SequenceEqual(final.ClearParticipantIds)))
                throw new InvalidOperationException("확정된 전투 참여자는 변경할 수 없습니다.");
            if (!before.ParticipantsCaptured && final.ParticipantsCaptured &&
                (!transition.Accepted || transition.Input.Kind != CombatCommandKind.EnterCombat ||
                    transition.Input.State.Turn.StagePhase != StagePhase.Deployment))
                throw new InvalidOperationException("참여자는 수락한 전투 시작에서만 확정할 수 있습니다.");
            foreach (var id in final.ClearParticipantIds)
            {
                if (!transition.Input.Owned.Any(c => c.Id == id) ||
                    !transition.Input.Characters.Any(c => c.OwnedId == id && c.Owner == Owner.Player))
                    throw new InvalidOperationException("참여자는 현재 전투의 보유 아군이어야 합니다.");
            }
        }

        /// <summary>분류된 살아 있는 Obstacle만 점유 형태를 유지하여 이동할 수 있다. 이동 규칙은 Core가 판정한다.</summary>
        private static bool PreservesStructurePosition(StructureCombatFacts expected, StructureCombatFacts final,
            EnvironmentSemanticBindings bindings)
        {
            if (expected.Anchor == final.Anchor)
                return expected.OccupiedCells.SequenceEqual(final.OccupiedCells);
            if (expected.Health.IsDead || !bindings.ObstacleEntityIds.Contains(expected.EntityId)) return false;
            var offset = final.Anchor - expected.Anchor;
            return expected.OccupiedCells.Select(cell => cell + offset).SequenceEqual(final.OccupiedCells);
        }

        /// <summary>환경 원본의 저장 불변식(분류 참조·할당 연속성·식별 유지)만 검사한다.
        /// 후보·난수·주기 같은 규칙 판정은 Core 계산이 소유한다.</summary>
        private static EnvironmentSemanticBindings ValidateEnvironmentChange(CombatCommandTransition transition)
        {
            var before = transition.Input.State;
            var final = transition.FinalState;
            if (before.WallBlocking.CornerCutTicks != final.WallBlocking.CornerCutTicks)
                throw new InvalidOperationException("전투 중 벽 설정을 변경할 수 없습니다.");
            var walls = before.Environment.Tiles.OfType<StaticTerrainFact>().Where(t => t.Kind == StaticTerrainKind.Wall).ToArray();
            var finalWalls = final.Environment.Tiles.OfType<StaticTerrainFact>().Where(t => t.Kind == StaticTerrainKind.Wall)
                .ToDictionary(t => t.Id);
            if (walls.Length != finalWalls.Count || walls.Any(wall =>
                !finalWalls.TryGetValue(wall.Id, out var same) || !wall.SameValues(same)))
                throw new InvalidOperationException("전투 중 정적 벽 원본을 변경할 수 없습니다.");
            ValidateGroundChange(transition);
            ValidateTraitTrailEvents(transition);
            var expected = transition.Input.EnvironmentBindings;
            var bindings = transition.EnvironmentBindings?.Final ?? expected;
            var previous = before.Environment.Tiles.ToDictionary(t => t.Id);
            var allAdded = final.Environment.Tiles.Where(t => !previous.ContainsKey(t.Id)).ToArray();
            var added = allAdded.OfType<ObjectSourceFact>().OrderBy(t => t.Id).ToArray();
            var generated = allAdded.Where(t => t is ObjectSourceFact || t is TraitTrailFact).OrderBy(t => t.Id).ToArray();

            // 분류: Terrain·Obstacle은 고정하고, Object 분류는 새로 만든 Object와 같은 ID만 덧붙는다.
            if (!new HashSet<long>(expected.TerrainSourceIds).SetEquals(bindings.TerrainSourceIds) ||
                !new HashSet<EntityId>(expected.ObstacleEntityIds).SetEquals(bindings.ObstacleEntityIds) ||
                expected.ObjectSourceIds.Except(bindings.ObjectSourceIds).Any() ||
                !bindings.ObjectSourceIds.Except(expected.ObjectSourceIds).OrderBy(id => id)
                    .SequenceEqual(added.Select(t => t.Id)))
                throw new InvalidOperationException("환경 분류 변경은 새 Object와 함께 추가되어야 합니다.");

            // 할당: Object와 흔적은 같은 ID 범위를 쓰고, 진입 순서는 Object만 늘립니다. 기존 미분류 원본 경로는 유지합니다.
            var p = before.EnvironmentProgress;
            var next = final.EnvironmentProgress;
            if (next.NextSourceId < p.NextSourceId || next.NextObjectEntryOrdinal < p.NextObjectEntryOrdinal ||
                allAdded.Any(t => t.Id < p.NextSourceId) ||
                (allAdded.Length == 0 && next.NextSourceId != p.NextSourceId) ||
                (generated.Length != 0 && (allAdded.Length != generated.Length ||
                    next.NextSourceId != checked(p.NextSourceId + generated.Length) ||
                    next.NextObjectEntryOrdinal != checked(p.NextObjectEntryOrdinal + added.Length))))
                throw new InvalidOperationException("환경 원본 할당 진행값이 생성된 원본과 다릅니다.");
            for (int index = 0; index < generated.Length; index++)
                if (generated[index].Id != checked(p.NextSourceId + index))
                    throw new InvalidOperationException("할당 범위를 벗어난 환경 원본 생성입니다.");
            for (int index = 0; index < added.Length; index++)
                if (added[index].EntryOrdinal != checked(p.NextObjectEntryOrdinal + index))
                    throw new InvalidOperationException("할당 범위를 벗어난 Object 생성입니다.");

            // 식별: 기존 ID는 기능 타입을 바꾸지 않고, Object는 내용 변경이나 재활성화를 하지 않는다.
            foreach (var source in final.Environment.Tiles)
            {
                if (!previous.TryGetValue(source.Id, out var old)) continue;
                // 종류별로 바뀌면 안 되는 값(형식·토네이도 간격·Object 내용·재활성화)은 계약이 정한다.
                var failure = old.SuccessorFailure(source);
                if (failure != null)
                    throw new InvalidOperationException(failure);
            }
            if (final.Environment.Tiles.OfType<ObjectSourceFact>().Where(t => t.Active).GroupBy(t => t.Cell)
                .Any(g => g.Select(t => t.EntryOrdinal).Distinct().Count() != g.Count()))
                throw new InvalidOperationException("활성 Object 진입 순서가 중복됩니다.");
            return bindings;
        }

        /// <summary>약한 지반의 누적값과 단방향 붕괴, 사건에 기록된 원본 연결을 검증한다.</summary>
        private static void ValidateGroundChange(CombatCommandTransition transition)
        {
            var before = transition.Input.State.Environment.Tiles.OfType<StaticTerrainFact>()
                .Where(IsGround).ToDictionary(t => t.Id);
            var final = transition.FinalState.Environment.Tiles.OfType<StaticTerrainFact>()
                .Where(IsGround).ToDictionary(t => t.Id);
            if (before.Count != final.Count || before.Keys.Except(final.Keys).Any())
                throw new InvalidOperationException("전투 중 약한 지반·낭떠러지 원본을 추가하거나 제거할 수 없습니다.");

            // 한 명령의 마지막 진입과 붕괴가 연이어 일어나면 중간 예정 상태는 사건에 남는다.
            var recorded = new Dictionary<long, StaticTerrainFact>(before);
            var changed = new HashSet<long>();
            foreach (var source in transition.Events.OfType<CombatMovementSourceEvent>())
            {
                if (!(source.Expected is StaticTerrainFact) && !(source.Final is StaticTerrainFact)) continue;
                if (!(source.Expected != null && before.ContainsKey(source.Expected.Id)) &&
                    !(source.Final is StaticTerrainFact terrain && IsGround(terrain))) continue;
                if (!(source.Expected is StaticTerrainFact expected) || !(source.Final is StaticTerrainFact next) ||
                    !recorded.TryGetValue(expected.Id, out var current) || !current.SameValues(expected) ||
                    !expected.IsGroundSuccessor(next, singleEntry: true))
                    throw new InvalidOperationException("지형 사건의 기대값 또는 다음 누적·붕괴 상태가 원본과 다릅니다.");
                recorded[expected.Id] = next;
                changed.Add(expected.Id);
            }
            foreach (var ground in before.Values)
            {
                var next = final[ground.Id];
                if (!transition.Input.EnvironmentBindings.TerrainSourceIds.Contains(ground.Id) ||
                    (changed.Contains(ground.Id) ? !recorded[ground.Id].SameValues(next) :
                        !ground.SameValues(next) && !ground.IsGroundSuccessor(next, singleEntry: false)))
                    throw new InvalidOperationException("지형 최종값은 기존 식별·설정과 누적 진행 또는 예정된 붕괴를 유지해야 합니다.");
            }
        }

        private static void ValidateTraitTrailEvents(CombatCommandTransition transition)
        {
            var recorded = transition.Input.State.Environment.Tiles.OfType<TraitTrailFact>().ToDictionary(t => t.Id);
            foreach (var change in transition.Events.OfType<CombatMovementSourceEvent>().Where(e => e.Final is TraitTrailFact))
            {
                var next = (TraitTrailFact)change.Final;
                if (change.Expected == null)
                {
                    if (recorded.ContainsKey(next.Id)) throw new InvalidOperationException("이미 있는 흔적을 다시 생성할 수 없습니다.");
                }
                else if (!recorded.TryGetValue(next.Id, out var before) || !before.SameValues(change.Expected))
                    throw new InvalidOperationException("흔적 사건의 기대값이 현재 원본과 다릅니다.");
                recorded[next.Id] = next;
            }
            var final = transition.FinalState.Environment.Tiles.OfType<TraitTrailFact>().ToArray();
            if (recorded.Count != final.Length || final.Any(t => !recorded.TryGetValue(t.Id, out var value) || !value.SameValues(t)))
                throw new InvalidOperationException("흔적 사건과 최종 환경 원본이 다릅니다.");
        }

        private static bool IsGround(StaticTerrainFact terrain) =>
            terrain.Kind == StaticTerrainKind.WeakGround || terrain.Kind == StaticTerrainKind.Cliff;

        /// <summary>전투 중 생성의 저장 불변식만 검사한다. 어디에 무엇을 만들지는 Core 계산이 정한다.
        /// 할당은 빈틈없이 이어지고, 소환 기록은 추가만 되며, 기록과 생성 개체는 1:1이어야 한다.</summary>
        private static void ValidateCreation(CombatCommandState before, CombatCommandState final,
            IReadOnlyList<CharacterCombatFacts> created, IReadOnlyList<CharacterCombatFacts> characters)
        {
            int count = created.Count;
            if (count != 0 && !before.Allocation.IsAvailable)
                throw new InvalidOperationException("할당 원본이 없는 전투에서는 개체를 만들 수 없습니다.");
            // 난수 상태는 생성 없이도(다음 밤 복귀 추첨) 바뀔 수 있으므로 소비 규칙은 Core에 맡긴다.
            if (before.Allocation.IsAvailable != final.Allocation.IsAvailable ||
                before.Allocation.IsAvailable && final.Allocation.NextEntityId != checked(before.Allocation.NextEntityId + count))
                throw new InvalidOperationException("개체 할당 진행값이 생성한 개체 수와 다릅니다.");
            var ids = created.Select(c => c.EntityId.Value).OrderBy(id => id).ToArray();
            for (int i = 0; i < ids.Length; i++)
                if (ids[i] != checked(before.Allocation.NextEntityId + i))
                    throw new InvalidOperationException("할당 범위를 벗어난 개체 생성입니다.");

            var previous = before.Turn.Summons;
            var next = final.Turn.Summons;
            if (next.Count < previous.Count || !next.Take(previous.Count).SequenceEqual(previous))
                throw new InvalidOperationException("기존 소환 기록을 바꾸거나 지울 수 없습니다.");
            var added = next.Skip(previous.Count).ToArray();
            if (!new HashSet<EntityId>(added.Select(r => r.SummonedId)).SetEquals(created.Select(c => c.EntityId)))
                throw new InvalidOperationException("소환 기록과 생성한 개체가 일치하지 않습니다.");
            foreach (var record in added)
                if (!characters.Any(c => c.EntityId == record.SummonerId && c.Owner == Owner.Enemy))
                    throw new InvalidOperationException("소환자는 전투의 적 캐릭터여야 합니다.");
            foreach (var character in created)
            {
                if (character.ActionUse != UnitActionUse.Deferred)
                    throw new InvalidOperationException("새 개체는 다음 자기 진영 시작까지 행동을 대기해야 합니다.");
            }
        }

        internal static void ValidatePlacement(CombatCommandState state, IReadOnlyList<CharacterCombatFacts> characters, int reserveCapacity)
        {
            var ids = new HashSet<EntityId>();
            var occupied = new HashSet<CellCoord>();
            var slots = new HashSet<int>();
            foreach (var character in characters)
            {
                if (!ids.Add(character.EntityId)) throw new InvalidOperationException("전투 개체 ID가 중복됩니다.");
                if (character.Area == CharacterRuntimeArea.Hand && (!character.ReserveSlotIndex.HasValue ||
                    character.ReserveSlotIndex.Value < 0 || character.ReserveSlotIndex.Value >= reserveCapacity ||
                    !slots.Add(character.ReserveSlotIndex.Value)))
                    throw new InvalidOperationException("대기 슬롯이 범위 밖이거나 중복됩니다.");
                if (character.Health.IsDead && character.Area == CharacterRuntimeArea.Field)
                    throw new InvalidOperationException("사망한 캐릭터가 필드를 점유할 수 없습니다.");
                foreach (var cell in character.OccupiedCells)
                    if (!state.IsInside(cell) || !occupied.Add(cell))
                        throw new InvalidOperationException("캐릭터 점유 위치가 보드 밖이거나 중복됩니다.");
            }
            foreach (var structure in state.Structures)
            {
                if (!ids.Add(structure.EntityId)) throw new InvalidOperationException("전투 개체 ID가 중복됩니다.");
                if (structure.Health.IsDead) continue;
                foreach (var cell in structure.OccupiedCells)
                    if (!state.IsInside(cell) || !occupied.Add(cell))
                        throw new InvalidOperationException("구조물 점유 위치가 보드 밖이거나 중복됩니다.");
            }
            var fields = new HashSet<CellCoord>();
            foreach (var field in state.ThornFields)
                if (!state.IsInside(field.Cell) || !fields.Add(field.Cell) || field.RemainingUnitTurns < 1 || field.ThornDamage < 1 ||
                    field.DefenseReductionPercent < 0 || field.DefenseReductionPercent > 100 || !Enum.IsDefined(typeof(UnitTurnTickPhase), field.TickPhase))
                    throw new InvalidOperationException("가시밭 위치 또는 유지 값이 잘못되었습니다.");
            foreach (var tile in state.Environment.Tiles)
                if (!state.IsInside(tile.Cell))
                    throw new InvalidOperationException("이동 타일 원본이 보드 밖에 있습니다.");
            foreach (var origin in state.Turn.EnemyOrigins)
                if (!state.IsInside(origin.Anchor) || !characters.Any(c => c.EntityId == origin.EntityId && c.Owner == Owner.Enemy))
                    throw new InvalidOperationException("적 재배치 원본이 보드·캐릭터 원본과 다릅니다.");
        }

        void ICombatStateBufferStore.InstallCommand(PreparedCombatCommand prepared)
        {
            _current = prepared.State;
            QueryRevision = prepared.FinalRevision;
            _latestResult = prepared.Result;
            _results.Add(prepared.Result);
            LatestResultSequence = prepared.Result.Sequence;
            if (prepared.Result.Kind == CombatProcessingKind.Command) HandledCommandNumber = prepared.Result.CommandNumber;
        }
    }

    internal sealed class PreparedCombatCommand
    {
        internal CombatStateData State { get; }
        internal CombatResult Result { get; }
        internal long ExpectedRevision { get; }
        internal long FinalRevision { get; }
        internal PreparedCombatCommand(CombatStateData state, CombatResult result, long expectedRevision, long finalRevision)
        {
            State = state; Result = result; ExpectedRevision = expectedRevision; FinalRevision = finalRevision;
        }
    }

    internal sealed class PreparedCombatState
    {
        internal CombatStateData State { get; }
        internal long BattleInstanceId { get; }

        internal PreparedCombatState(CombatStateData state, long battleInstanceId)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            BattleInstanceId = battleInstanceId;
        }
    }

    #endregion
}
