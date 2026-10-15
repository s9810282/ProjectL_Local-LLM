using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Character
{
    #region 서비스

    public interface ICharacterBufferReadSink : IServiceModule
    {
        IReadOnlyList<OwnedCharacterRecord> Owned { get; }
        IReadOnlyList<CharacterData> Characters { get; }

        bool TryGet(EntityId id, out CharacterData data);
        bool TryGetOwned(CharacterInstanceId id, out OwnedCharacterRecord record);
        int GetSoulCount(CharacterInstanceId id);
        bool TryGetMainState(out MainCharacterPersistentState state);
        bool TryGetOwnedInstance(EntityId entityId, out CharacterInstanceId ownedId);
    }

    public interface ICharacterBufferWriteSink : IServiceModule
    {
        RosterAcquireResult Acquire(IReadOnlyList<RosterAcquireRequest> requests,
            MainCharacterPersistentState initialMain = null);

        void Apply(CharacterChanges changes);
    }

    internal interface ICharacterBufferStore : IServiceModule
    {
        PreparedRosterAcquisition PrepareAcquire(IReadOnlyList<RosterAcquireRequest> requests,
            MainCharacterPersistentState initialMain = null);
        /// <summary>새 Run 로스터를 빈 로스터 기준으로 준비한다. 설치 전에 ClearRoster가 먼저 실행되어야 한다.</summary>
        PreparedRosterAcquisition PrepareInitialRoster(IReadOnlyList<RosterAcquireRequest> requests,
            MainCharacterPersistentState initialMain);
        void InstallAcquire(PreparedRosterAcquisition prepared);
        PreparedCharacterBattle PrepareBattle(IReadOnlyList<CharacterCombatFacts> characters);
        void InstallBattle(PreparedCharacterBattle prepared);
        PreparedCharacterChanges PrepareChanges(CharacterChanges changes);
        void ValidateCommandInput(CombatCommandInput input);
        void InstallChanges(PreparedCharacterChanges prepared);
        void EndBattle();
        void ClearRoster();
    }

    #endregion

    #region 시스템 모듈

    internal sealed class CharacterBuffer :
        ICharacterBufferReadSink,
        ICharacterBufferWriteSink,
        ICharacterBufferStore,
        IExportSystemServices
    {
        private readonly Dictionary<CharacterInstanceId, OwnedCharacterRecord> _owned = new();
        private readonly List<OwnedCharacterRecord> _ownedItems = new();
        private readonly ReadOnlyCollection<OwnedCharacterRecord> _readOnlyOwned;
        private readonly Dictionary<CharacterInstanceId, int> _souls = new();
        private readonly Dictionary<EntityId, BattleCharacterRecord> _battleRecords = new();
        private readonly List<EntityId> _battleOrder = new();
        private readonly Dictionary<EntityId, CharacterData> _viewsByEntity = new();
        private readonly List<CharacterData> _characterViews = new();
        private readonly ReadOnlyCollection<CharacterData> _readOnlyCharacters;

        private long _nextOwnedId;
        private CharacterInstanceId? _mainOwnedId;
        private MainCharacterPersistentState _mainState;

        public IReadOnlyList<OwnedCharacterRecord> Owned => _readOnlyOwned;
        public IReadOnlyList<CharacterData> Characters => _readOnlyCharacters;

        public CharacterBuffer() : this(1)
        {
        }

        internal CharacterBuffer(long nextOwnedId)
        {
            if (nextOwnedId < 1 || nextOwnedId > (long)int.MaxValue + 1)
                throw new ArgumentOutOfRangeException(nameof(nextOwnedId));

            _nextOwnedId = nextOwnedId;
            _readOnlyOwned = _ownedItems.AsReadOnly();
            _readOnlyCharacters = _characterViews.AsReadOnly();
        }

        public bool TryGet(EntityId id, out CharacterData data)
        {
            return _viewsByEntity.TryGetValue(id, out data);
        }

        public bool TryGetOwned(CharacterInstanceId id, out OwnedCharacterRecord record)
        {
            return _owned.TryGetValue(id, out record);
        }

        public int GetSoulCount(CharacterInstanceId id)
        {
            if (_souls.TryGetValue(id, out int count) == false)
                throw new KeyNotFoundException($"보유 캐릭터 {id}를 찾을 수 없습니다.");

            return count;
        }

        public bool TryGetMainState(out MainCharacterPersistentState state)
        {
            state = _mainState;
            return state != null;
        }

        public bool TryGetOwnedInstance(EntityId entityId, out CharacterInstanceId ownedId)
        {
            if (_battleRecords.TryGetValue(entityId, out BattleCharacterRecord record) &&
                record.OwnedId.HasValue)
            {
                ownedId = record.OwnedId.Value;
                return true;
            }

            ownedId = CharacterInstanceId.Empty;
            return false;
        }

        public RosterAcquireResult Acquire(IReadOnlyList<RosterAcquireRequest> requests,
            MainCharacterPersistentState initialMain = null)
        {
            PreparedRosterAcquisition prepared = PrepareAcquire(requests, initialMain);
            if (prepared.Result.Succeeded)
                InstallAcquire(prepared);

            return prepared.Result;
        }

        public PreparedRosterAcquisition PrepareAcquire(IReadOnlyList<RosterAcquireRequest> requests,
            MainCharacterPersistentState initialMain = null)
            => PrepareAcquire(requests, initialMain, asEmptyRoster: false);

        public PreparedRosterAcquisition PrepareInitialRoster(IReadOnlyList<RosterAcquireRequest> requests,
            MainCharacterPersistentState initialMain)
            => PrepareAcquire(requests, initialMain, asEmptyRoster: true);

        private PreparedRosterAcquisition PrepareAcquire(IReadOnlyList<RosterAcquireRequest> requests,
            MainCharacterPersistentState initialMain, bool asEmptyRoster)
        {
            if (requests == null)
                throw new ArgumentNullException(nameof(requests));
            if (requests.Count == 0)
                return PreparedRosterAcquisition.Failure(RosterAcquireFailureReason.InvalidRequest);

            int mainCount = 0;
            for (int i = 0; i < requests.Count; i++)
            {
                RosterAcquireRequest request = requests[i];
                if (request == null)
                    return PreparedRosterAcquisition.Failure(RosterAcquireFailureReason.InvalidRequest);
                if (request.Role == CharacterOwnershipRole.Main)
                    mainCount++;
            }

            // 새 Run 준비는 이전 로스터를 비운 뒤 설치하므로 지금의 메인 보유를 충돌로 보지 않는다.
            if (mainCount > 1 || mainCount == 1 && _mainOwnedId.HasValue && !asEmptyRoster)
                return PreparedRosterAcquisition.Failure(RosterAcquireFailureReason.MainAlreadyOwned);
            if (mainCount == 1 && (initialMain == null ||
                                  initialMain.CurrentHealth > initialMain.PersistentMaxHealth))
                return PreparedRosterAcquisition.Failure(RosterAcquireFailureReason.InvalidMainState);
            if (mainCount == 0 && initialMain != null)
                return PreparedRosterAcquisition.Failure(RosterAcquireFailureReason.InvalidMainState);

            long lastId = _nextOwnedId + requests.Count - 1L;
            if (lastId > int.MaxValue)
                return PreparedRosterAcquisition.Failure(RosterAcquireFailureReason.IdentityOverflow);

            var acquired = new OwnedCharacterRecord[requests.Count];
            for (int i = 0; i < acquired.Length; i++)
            {
                RosterAcquireRequest request = requests[i];
                var id = new CharacterInstanceId((int)(_nextOwnedId + i));
                acquired[i] = new OwnedCharacterRecord(id, request.CharacterKey, request.Role);
            }

            return new PreparedRosterAcquisition(RosterAcquireResult.Success(acquired),
                initialMain, lastId + 1L);
        }

        public void InstallAcquire(PreparedRosterAcquisition prepared)
        {
            foreach (OwnedCharacterRecord record in prepared.Result.Acquired)
            {
                _owned.Add(record.Id, record);
                _ownedItems.Add(record);
                _souls.Add(record.Id, 0);

                if (record.Role == CharacterOwnershipRole.Main)
                {
                    _mainOwnedId = record.Id;
                    _mainState = prepared.InitialMain;
                }
            }

            _nextOwnedId = prepared.NextId;
        }

        public void Apply(CharacterChanges changes)
        {
            var prepared = PrepareChanges(changes);
            InstallChanges(prepared);
        }

        PreparedCharacterChanges ICharacterBufferStore.PrepareChanges(CharacterChanges changes) => PrepareChanges(changes);
        void ICharacterBufferStore.ValidateCommandInput(CombatCommandInput input)
        {
            // 변경되지 않은 대상과 목록 부재도 계산이 읽은 원본이다. 게시 전에 같은 Buffer 원본인지 확인한다.
            if (input.Characters.Count != _characterViews.Count || input.Owned.Count != _ownedItems.Count ||
                input.SoulCounts.Count != _souls.Count || _mainState == null || !SameMainState(_mainState, input.Main))
                throw new InvalidOperationException("명령 계산의 캐릭터·보유·영혼·메인 원본이 변경되었습니다.");
            foreach (var expected in input.Characters)
                if (!_viewsByEntity.TryGetValue(expected.EntityId, out var current) ||
                    !CharacterCombatFactsComparer.SameFacts(expected, current.ToFacts()))
                    throw new InvalidOperationException("명령 계산이 읽은 캐릭터 원본이 변경되었습니다.");
            foreach (var expected in input.Owned)
                if (!_owned.TryGetValue(expected.Id, out var current) || current.CharacterKey != expected.CharacterKey ||
                    current.Role != expected.Role || !input.SoulCounts.TryGetValue(expected.Id, out int count) ||
                    _souls[expected.Id] != count)
                    throw new InvalidOperationException("명령 계산이 읽은 보유·영혼 원본이 변경되었습니다.");
        }
        void ICharacterBufferStore.InstallChanges(PreparedCharacterChanges prepared) => InstallChanges(prepared);

        private PreparedCharacterChanges PrepareChanges(CharacterChanges changes)
        {
            if (changes == null)
                throw new ArgumentNullException(nameof(changes));

            var changedEntities = new HashSet<EntityId>();
            foreach (CharacterStateChange change in changes.Characters)
                if (changedEntities.Add(change.Expected.EntityId) == false)
                    throw new InvalidOperationException("한 배치에서 같은 캐릭터를 두 번 바꿀 수 없습니다.");

            var changedSouls = new HashSet<CharacterInstanceId>();
            foreach (SoulCountChange change in changes.Souls)
                if (changedSouls.Add(change.Id) == false)
                    throw new InvalidOperationException("한 배치에서 같은 영혼 개수를 두 번 바꿀 수 없습니다.");

            var proposedSouls = new Dictionary<CharacterInstanceId, int>(_souls);
            foreach (SoulCountChange change in changes.Souls)
            {
                if (proposedSouls.TryGetValue(change.Id, out int current) == false ||
                    current != change.ExpectedCount)
                    throw new InvalidOperationException("영혼 변경의 예상 값이 현재 상태와 다릅니다.");

                proposedSouls[change.Id] = change.FinalCount;
            }

            MainCharacterPersistentState proposedMain = _mainState;
            if (changes.Main != null)
            {
                if (_mainState == null || SameMainState(_mainState, changes.Main.Expected) == false)
                    throw new InvalidOperationException("메인 지속 상태의 예상 값이 현재 상태와 다릅니다.");

                proposedMain = changes.Main.Final;
            }

            var proposedRecords = new Dictionary<EntityId, BattleCharacterRecord>(_battleRecords);
            foreach (CharacterStateChange change in changes.Characters)
            {
                if (proposedRecords.TryGetValue(change.Expected.EntityId,
                        out BattleCharacterRecord current) == false)
                    throw new InvalidOperationException("변경할 전투 캐릭터를 찾을 수 없습니다.");

                CharacterCombatFacts currentFacts = current.BuildFacts(_mainState);
                if (CharacterCombatFactsComparer.SameFacts(currentFacts, change.Expected) == false)
                    throw new InvalidOperationException("캐릭터 변경의 예상 값이 현재 상태와 다릅니다.");
                if (CharacterCombatFactsComparer.SameIdentity(change.Expected, change.Final) == false)
                    throw new InvalidOperationException("캐릭터 변경으로 식별자를 바꿀 수 없습니다.");

                // 메인 체력을 지속 원본으로 합성하기 전에 전달된 최종 값의 일관성을 확인한다.
                ValidateBattleRecord(change.Final, proposedMain, proposedSouls);
                proposedRecords[change.Final.EntityId] = new BattleCharacterRecord(change.Final);
            }

            foreach (var created in changes.Created)
            {
                if (created.Owner != Owner.Player || created.OwnershipRole != CharacterOwnershipRole.Sub ||
                    created.Area != CharacterRuntimeArea.Hand || !created.OwnedId.HasValue ||
                    !_owned.TryGetValue(created.OwnedId.Value, out var owned) || owned.CharacterKey != created.CharacterKey ||
                    owned.Role != created.OwnershipRole || proposedRecords.ContainsKey(created.EntityId) ||
                    proposedRecords.Values.Any(r => r.OwnedId == created.OwnedId))
                    throw new InvalidOperationException("생성할 캐릭터 식별자 또는 보유 연결이 잘못되었습니다.");
                ValidateBattleRecord(created, proposedMain, proposedSouls);
                proposedRecords.Add(created.EntityId, new BattleCharacterRecord(created));
            }

            ValidatePersistentMain(proposedMain, proposedSouls);
            foreach (BattleCharacterRecord record in proposedRecords.Values)
                ValidateBattleRecord(record.BuildFacts(proposedMain), proposedMain, proposedSouls);

            CharacterData[] proposedViews = BuildViews(proposedRecords, proposedMain)
                .Concat(changes.Created.Select(c => new CharacterData(proposedRecords[c.EntityId].BuildFacts(proposedMain)))).ToArray();
            return new PreparedCharacterChanges(changes, proposedMain, proposedRecords, proposedViews, proposedSouls);
        }

        private void InstallChanges(PreparedCharacterChanges prepared)
        {
            // 모든 저장소가 준비된 뒤 기존 항목만 교체한다.
            foreach (SoulCountChange change in prepared.Changes.Souls)
                _souls[change.Id] = change.FinalCount;

            _mainState = prepared.Main;
            foreach (CharacterStateChange change in prepared.Changes.Characters)
                _battleRecords[change.Final.EntityId] = prepared.Records[change.Final.EntityId];

            foreach (var created in prepared.Changes.Created)
            {
                _battleRecords.Add(created.EntityId, prepared.Records[created.EntityId]);
                _battleOrder.Add(created.EntityId);
            }

            InstallViews(prepared.Views);
        }

        public void ExportServices(ISystemWriteSink sink)
        {
            sink.AddService<ICharacterBufferReadSink>(this);
            sink.AddService<ICharacterBufferWriteSink>(this);
            sink.AddService<ICharacterBufferStore>(this);
        }

        PreparedCharacterBattle ICharacterBufferStore.PrepareBattle(
            IReadOnlyList<CharacterCombatFacts> characters)
        {
            if (characters == null)
                throw new ArgumentNullException(nameof(characters));
            if (characters.Count == 0)
                throw new ArgumentException("전투 준비에는 캐릭터가 한 명 이상 필요합니다.", nameof(characters));
            if (_battleRecords.Count != 0)
                throw new InvalidOperationException("이미 준비된 전투가 있습니다.");

            var records = new BattleCharacterRecord[characters.Count];
            var views = new CharacterData[characters.Count];
            var entityIds = new HashSet<EntityId>();
            var linkedOwnedIds = new HashSet<CharacterInstanceId>();
            bool includesMain = false;

            for (int i = 0; i < records.Length; i++)
            {
                CharacterCombatFacts facts = characters[i] ?? throw new ArgumentException(
                    "전투 준비 캐릭터에는 null을 넣을 수 없습니다.", nameof(characters));
                if (entityIds.Add(facts.EntityId) == false)
                    throw new ArgumentException("전투 개체 ID는 중복될 수 없습니다.", nameof(characters));

                if (facts.OwnedId.HasValue)
                {
                    CharacterInstanceId ownedId = facts.OwnedId.Value;
                    if (linkedOwnedIds.Add(ownedId) == false)
                        throw new ArgumentException("보유 캐릭터는 전투에 한 번만 연결할 수 있습니다.", nameof(characters));
                    if (_owned.TryGetValue(ownedId, out OwnedCharacterRecord owned) == false ||
                        owned.CharacterKey != facts.CharacterKey ||
                        owned.Role != facts.OwnershipRole)
                        throw new InvalidOperationException("전투 캐릭터의 보유 정보가 현재 로스터와 다릅니다.");

                    includesMain |= owned.Role == CharacterOwnershipRole.Main;
                }
                else if (facts.Owner == Owner.Player)
                {
                    throw new InvalidOperationException("플레이어 캐릭터에는 보유 ID가 필요합니다.");
                }

                ValidateBattleRecord(facts, _mainState, _souls);
                records[i] = new BattleCharacterRecord(facts);
                views[i] = new CharacterData(records[i].BuildFacts(_mainState));
            }

            if (_mainOwnedId.HasValue && includesMain == false)
                throw new InvalidOperationException("전투 준비에는 보유 메인이 포함되어야 합니다.");

            return new PreparedCharacterBattle(records, views);
        }

        void ICharacterBufferStore.InstallBattle(PreparedCharacterBattle prepared)
        {
            if (prepared == null)
                throw new ArgumentNullException(nameof(prepared));
            if (_battleRecords.Count != 0)
                throw new InvalidOperationException("이미 준비된 전투가 있습니다.");

            foreach (BattleCharacterRecord record in prepared.Records)
            {
                _battleRecords.Add(record.EntityId, record);
                _battleOrder.Add(record.EntityId);
            }

            InstallViews(prepared.Views);
        }

        public void EndBattle()
        {
            // 종료 시 최대 체력 정리는 Core 종료 정산(BattleCompletionCalculator)이 소유한다. 정리는 정책 때문에 실패하지 않는다.
            _battleRecords.Clear();
            _battleOrder.Clear();
            _viewsByEntity.Clear();
            _characterViews.Clear();
        }

        void ICharacterBufferStore.ClearRoster()
        {
            _owned.Clear();
            _ownedItems.Clear();
            _souls.Clear();
            _mainOwnedId = null;
            _mainState = null;
        }

        private CharacterData[] BuildViews(IReadOnlyDictionary<EntityId, BattleCharacterRecord> records,
            MainCharacterPersistentState main)
        {
            var views = new CharacterData[_battleOrder.Count];
            for (int i = 0; i < views.Length; i++)
                views[i] = new CharacterData(records[_battleOrder[i]].BuildFacts(main));

            return views;
        }

        private void InstallViews(IReadOnlyList<CharacterData> views)
        {
            _viewsByEntity.Clear();
            _characterViews.Clear();

            foreach (CharacterData data in views)
            {
                _viewsByEntity.Add(data.EntityId, data);
                _characterViews.Add(data);
            }
        }

        private void ValidatePersistentMain(MainCharacterPersistentState main,
            IReadOnlyDictionary<CharacterInstanceId, int> souls)
        {
            // 최대 체력 공식(지속 + 영혼 + 임시·위치)은 Core가 소유한다. 전투 중 상한은 전투 사실의 범위 검사가 확인한다.
            if (main == null)
                return;
            if (_mainOwnedId.HasValue == false || souls.ContainsKey(_mainOwnedId.Value) == false)
                throw new InvalidOperationException("메인 지속 상태에 대응하는 보유 캐릭터가 없습니다.");
        }

        private static void ValidateBattleRecord(CharacterCombatFacts facts,
            MainCharacterPersistentState main,
            IReadOnlyDictionary<CharacterInstanceId, int> souls)
        {
            // 값 형식의 기본값은 생성자 검증을 거치지 않으므로 저장 경계에서 거절한다.
            if (facts.Health.MaxHealth < 1 || facts.Health.CurrentHealth < 0 ||
                facts.Health.CurrentHealth > facts.Health.MaxHealth || facts.Stats.Category == null ||
                facts.Stats.PositionAttackBonus != facts.PositionModifier.AttackBonus)
                throw new InvalidOperationException("전투 캐릭터의 체력 또는 능력치가 초기화되지 않았습니다.");

            // 영혼·임시·위치 보너스의 합산 공식은 Core 계산이 소유한다. 여기서는 참조 존재와 한 곳에 저장한 값의 일치만 본다.
            if (facts.OwnedId.HasValue && souls.ContainsKey(facts.OwnedId.Value) == false)
                throw new InvalidOperationException("보유 캐릭터의 영혼 기록을 찾을 수 없습니다.");

            if (facts.OwnershipRole != CharacterOwnershipRole.Main)
                return;
            if (main == null || facts.OwnedId.HasValue == false)
                throw new InvalidOperationException("메인 전투 상태에 대응하는 지속 상태가 없습니다.");

            // 메인 현재 체력과 지속 능력치는 지속 원본 한 곳에만 저장하므로 전투 사실의 사본이 그 값과 같아야 한다.
            if (facts.Health.CurrentHealth != main.CurrentHealth ||
                facts.Stats.AttackPower != main.PersistentAttack ||
                facts.Stats.MaxActionCost != main.MaxActionCost ||
                facts.Stats.MoveRange != main.MoveRange ||
                facts.Stats.MoveCost != main.MoveCost)
                throw new InvalidOperationException("메인 전투 상태가 지속 원본과 맞지 않습니다.");
        }

        private static bool SameMainState(MainCharacterPersistentState left,
            MainCharacterPersistentState right)
        {
            return left.CurrentHealth == right.CurrentHealth &&
                   left.PersistentMaxHealth == right.PersistentMaxHealth &&
                   left.PersistentAttack == right.PersistentAttack &&
                   left.MaxActionCost == right.MaxActionCost &&
                   left.MoveRange == right.MoveRange &&
                   left.MoveCost == right.MoveCost;
        }

    }

    internal sealed class PreparedRosterAcquisition
    {
        internal RosterAcquireResult Result { get; }
        internal MainCharacterPersistentState InitialMain { get; }
        internal long NextId { get; }

        internal PreparedRosterAcquisition(RosterAcquireResult result,
            MainCharacterPersistentState initialMain, long nextId)
        {
            Result = result;
            InitialMain = initialMain;
            NextId = nextId;
        }

        internal static PreparedRosterAcquisition Failure(RosterAcquireFailureReason reason)
        {
            return new PreparedRosterAcquisition(RosterAcquireResult.Failure(reason), null, 0);
        }
    }

    internal sealed class PreparedCharacterChanges
    {
        internal CharacterChanges Changes { get; }
        internal MainCharacterPersistentState Main { get; }
        internal IReadOnlyDictionary<EntityId, BattleCharacterRecord> Records { get; }
        internal CharacterData[] Views { get; }
        internal IReadOnlyDictionary<CharacterInstanceId, int> Souls { get; }
        internal PreparedCharacterChanges(CharacterChanges changes, MainCharacterPersistentState main,
            IReadOnlyDictionary<EntityId, BattleCharacterRecord> records, CharacterData[] views,
            IReadOnlyDictionary<CharacterInstanceId, int> souls)
        { Changes = changes; Main = main; Records = records; Views = views; Souls = souls; }
    }

    internal sealed class PreparedCharacterBattle
    {
        internal IReadOnlyList<BattleCharacterRecord> Records { get; }
        internal IReadOnlyList<CharacterData> Views { get; }

        internal PreparedCharacterBattle(BattleCharacterRecord[] records, CharacterData[] views)
        {
            Records = Array.AsReadOnly(records);
            Views = Array.AsReadOnly(views);
        }
    }

    internal sealed class BattleCharacterRecord
    {
        private readonly CharacterCombatFacts _facts;
        private readonly int? _battleCurrentHealth;

        internal EntityId EntityId => _facts.EntityId;
        internal CharacterInstanceId? OwnedId => _facts.OwnedId;

        internal BattleCharacterRecord(CharacterCombatFacts facts)
        {
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _battleCurrentHealth = facts.OwnershipRole == CharacterOwnershipRole.Main
                ? null
                : facts.Health.CurrentHealth;
        }

        internal CharacterCombatFacts BuildFacts(MainCharacterPersistentState main)
        {
            int currentHealth = _battleCurrentHealth ?? main?.CurrentHealth ??
                throw new InvalidOperationException("메인 지속 체력을 찾을 수 없습니다.");

            return new CharacterCombatFacts(
                _facts.EntityId,
                _facts.OwnedId,
                _facts.CharacterKey,
                _facts.Owner,
                _facts.OwnershipRole,
                _facts.Area,
                new HealthStateSnapshot(currentHealth, _facts.Health.MaxHealth),
                _facts.Stats,
                _facts.ReserveSlotIndex,
                _facts.Anchor,
                _facts.Rotation,
                _facts.OccupiedCells,
                _facts.MovementCheckpointAnchor,
                _facts.CommittedMoveDistance,
                _facts.PendingMoveActionCost,
                _facts.TemporaryAttackBonus,
                _facts.TemporaryMaxHealthBonus,
                _facts.PendingMoveDistance, _facts.AttackRefundEligibleMoveCost, _facts.PositionModifier, _facts.HeldEnvironment);
        }
    }

    #endregion
}
