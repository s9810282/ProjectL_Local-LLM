using System;
using System.Collections.Generic;
using System.Linq;
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
        PreparedCombatCommand PrepareCommand(CombatCommandTransition transition, IReadOnlyList<CharacterCombatFacts> characters);
        void InstallCommand(PreparedCombatCommand prepared);
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
            SoulField = SoulFieldSnapshot.Empty;
            _latestPreview = null;
        }

        void ICombatStateBufferStore.InstallCompletion(BattleCompletionChanges completion) => Completion = completion;

        PreparedCombatCommand ICombatStateBufferStore.PrepareCommand(CombatCommandTransition transition,
            IReadOnlyList<CharacterCombatFacts> characters)
        {
            if (_current == null || (transition.Input.BattleInstanceId != 0 && transition.Input.BattleInstanceId != BattleInstanceId) ||
                !_current.CommandState.SameValues(transition.Input.State) ||
                !_current.EnvironmentBindings.SameValues(transition.Input.EnvironmentBindings) ||
                !ObstacleContentProfile.SameValues(_current.ObstacleContents, transition.Input.ObstacleContents))
                throw new InvalidOperationException("명령의 전투 원본이 다릅니다.");
            var before = _current.CommandState;
            var final = transition.FinalState;
            if (before.BoardWidth != final.BoardWidth || before.BoardHeight != final.BoardHeight ||
                before.Structures.Count != final.Structures.Count)
                throw new InvalidOperationException("공격으로 보드와 구조물 식별자를 바꿀 수 없습니다.");
            for (int i = 0; i < before.Structures.Count; i++)
            {
                var a = before.Structures[i]; var b = final.Structures[i];
                if (a.EntityId != b.EntityId || a.Definition != b.Definition || a.Owner != b.Owner ||
                    a.Anchor != b.Anchor || a.Rotation != b.Rotation || a.Health.MaxHealth != b.Health.MaxHealth ||
                    !a.OccupiedCells.SequenceEqual(b.OccupiedCells))
                    throw new InvalidOperationException("공격으로 구조물 원본을 바꿀 수 없습니다.");
            }
            ValidatePlacement(final, characters, _current.ReserveCapacity);
            var validatedBindings = ValidateEnvironmentChange(transition);
            var bindings = transition.EnvironmentBindings == null ? _current.EnvironmentBindings : validatedBindings;
            int sequence = checked(LatestResultSequence + 1);
            if (transition.Draw != null)
            {
                var draw = transition.Draw;
                var created = transition.Characters.Created.Single();
                if (draw.ExpectedCursor != _current.DrawCursor || draw.ExpectedNextEntityId != _current.NextEntityId ||
                    draw.ExpectedGold != _current.StageGold ||
                    draw.ExpectedCursor >= _current.DrawOrder.Count || created.OwnedId != _current.DrawOrder[draw.ExpectedCursor] ||
                    created.EntityId != draw.EntityId || created.CharacterKey != draw.CharacterKey || created.ReserveSlotIndex != draw.Slot)
                    throw new InvalidOperationException("드로우 커서·식별자·스테이지 골드·생성 결과가 원본과 다릅니다.");
            }
            else if (transition.Characters.Created.Count != 0)
                throw new InvalidOperationException("캐릭터 생성에는 드로우 변경이 필요합니다.");
            var result = new CombatResult(sequence, transition.Input.ProcessingKind, transition.Input.Number,
                transition.Accepted, transition.Reason, transition.Attack, transition.Outcome,
                transition.Input.Kind, transition.Input.ActorId, transition.Movement, transition.Draw, transition.Events,
                transition.PathMovement, transition.ReturnMovement, transition.BlockedContacts);
            return new PreparedCombatCommand(new CombatStateData(_current, final, transition.Draw, bindings), result);
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
            var expected = transition.Input.EnvironmentBindings;
            var bindings = transition.EnvironmentBindings?.Final ?? expected;
            var previous = before.Environment.Tiles.ToDictionary(t => t.Id);
            var allAdded = final.Environment.Tiles.Where(t => !previous.ContainsKey(t.Id)).ToArray();
            var added = allAdded.OfType<ObjectSourceFact>().OrderBy(t => t.Id).ToArray();

            // 분류: Terrain·Obstacle은 고정하고, Object 분류는 새로 만든 Object와 같은 ID만 덧붙는다.
            if (!new HashSet<long>(expected.TerrainSourceIds).SetEquals(bindings.TerrainSourceIds) ||
                !new HashSet<EntityId>(expected.ObstacleEntityIds).SetEquals(bindings.ObstacleEntityIds) ||
                expected.ObjectSourceIds.Except(bindings.ObjectSourceIds).Any() ||
                !bindings.ObjectSourceIds.Except(expected.ObjectSourceIds).OrderBy(id => id)
                    .SequenceEqual(added.Select(t => t.Id)))
                throw new InvalidOperationException("환경 분류 변경은 새 Object와 함께 추가되어야 합니다.");

            // 할당: Object 생성은 ID·진입 순서를 빈틈없이 이어 받는다. 분류 없는 기존 원본 추가는 별도 경로로 유지한다.
            var p = before.EnvironmentProgress;
            var next = final.EnvironmentProgress;
            if (next.NextSourceId < p.NextSourceId || next.NextObjectEntryOrdinal < p.NextObjectEntryOrdinal ||
                allAdded.Any(t => t.Id < p.NextSourceId) ||
                (allAdded.Length == 0 && next.NextSourceId != p.NextSourceId) ||
                (added.Length != 0 && (allAdded.Length != added.Length ||
                    next.NextSourceId != checked(p.NextSourceId + added.Length) ||
                    next.NextObjectEntryOrdinal != checked(p.NextObjectEntryOrdinal + added.Length))))
                throw new InvalidOperationException("환경 원본 할당 진행값이 생성된 원본과 다릅니다.");
            for (int index = 0; index < added.Length; index++)
                if (added[index].Id != checked(p.NextSourceId + index) ||
                    added[index].EntryOrdinal != checked(p.NextObjectEntryOrdinal + index))
                    throw new InvalidOperationException("할당 범위를 벗어난 Object 생성입니다.");

            // 식별: 기존 ID는 기능 타입을 바꾸지 않고, Object는 내용 변경이나 재활성화를 하지 않는다.
            foreach (var source in final.Environment.Tiles)
            {
                if (!previous.TryGetValue(source.Id, out var old)) continue;
                if (old.GetType() != source.GetType() || old is ObjectSourceFact original &&
                    (!original.WithActive(source.Active).SameValues(source) || !original.Active && source.Active))
                    throw new InvalidOperationException("기존 환경 원본의 기능·식별 내용 변경 또는 재활성화는 허용하지 않습니다.");
            }
            if (final.Environment.Tiles.OfType<ObjectSourceFact>().Where(t => t.Active).GroupBy(t => t.Cell)
                .Any(g => g.Select(t => t.EntryOrdinal).Distinct().Count() != g.Count()))
                throw new InvalidOperationException("활성 Object 진입 순서가 중복됩니다.");
            return bindings;
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
            if (!state.ActiveActorId.IsEmpty && state.Phase != CombatFlowPhase.Finished &&
                !characters.Any(c => c.EntityId == state.ActiveActorId && !c.Health.IsDead && c.Area == CharacterRuntimeArea.Field))
                throw new InvalidOperationException("현재 턴 캐릭터가 살아 있는 필드 캐릭터가 아닙니다.");
            var fields = new HashSet<CellCoord>();
            foreach (var field in state.ThornFields)
                if (!state.IsInside(field.Cell) || !fields.Add(field.Cell) || field.RemainingUnitTurns < 1 || field.ThornDamage < 1)
                    throw new InvalidOperationException("가시밭 위치 또는 유지 값이 잘못되었습니다.");
            var participants = new HashSet<EntityId>();
            foreach (var tile in state.Environment.Tiles)
                if (!state.IsInside(tile.Cell))
                    throw new InvalidOperationException("이동 타일 원본이 보드 밖에 있습니다.");
            foreach (var entry in state.Timeline)
            {
                if (entry.Kind == CombatTimelineEntryKind.None)
                    throw new InvalidOperationException("타임라인 항목 종류가 없습니다.");
                if (entry.Kind == CombatTimelineEntryKind.UnitTurn &&
                    (!entry.UnitId.HasValue || !participants.Add(entry.UnitId.Value) ||
                     !characters.Any(c => c.EntityId == entry.UnitId.Value && !c.Health.IsDead && c.Area == CharacterRuntimeArea.Field)))
                    throw new InvalidOperationException("타임라인 캐릭터가 살아 있는 필드 캐릭터가 아니거나 중복됩니다.");
            }
            foreach (var participant in state.Turn.Participants)
                if (participant.EntityId.IsEmpty || !participants.Contains(participant.EntityId))
                    throw new InvalidOperationException("실제 턴 참가자와 타임라인 조회값이 다릅니다.");
            if (state.Turn.Participants.Count != 0 && state.Turn.Participants.Count != participants.Count)
                throw new InvalidOperationException("실제 턴 참가자와 타임라인 조회값이 다릅니다.");
            foreach (var origin in state.Turn.EnemyOrigins)
                if (!state.IsInside(origin.Anchor) || !characters.Any(c => c.EntityId == origin.EntityId && c.Owner == Owner.Enemy))
                    throw new InvalidOperationException("적 재배치 원본이 보드·캐릭터 원본과 다릅니다.");
        }

        void ICombatStateBufferStore.InstallCommand(PreparedCombatCommand prepared)
        {
            _current = prepared.State;
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
        internal PreparedCombatCommand(CombatStateData state, CombatResult result) { State = state; Result = result; }
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
