using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots.Traits;
using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Application.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Enhancements;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using ProjectL.Framework.Composition;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.PlayerSkills;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;

namespace ProjectL.Systems.RuntimeSystem.BufferSystems.Character
{
    #region 서비스

    public interface ICharacterBufferReadSink : IServiceModule
    {
        long RosterRevision
        {
            get;
        }
        IReadOnlyList<OwnedCharacterRecord> Owned
        {
            get;
        }
        IReadOnlyList<CharacterData> Characters
        {
            get;
        }

        bool TryGet(EntityId id, out CharacterData data);
        bool TryGetOwned(CharacterInstanceId id, out OwnedCharacterRecord record);
        int GetSoulCount(CharacterInstanceId id);
        bool TryGetMainState(CharacterInstanceId id, out MainCharacterPersistentState state);
        bool TryGetOwnedInstance(EntityId entityId, out CharacterInstanceId ownedId);
    }

    public interface ICharacterBufferWriteSink : IServiceModule
    {
        RosterAcquireResult Acquire(IReadOnlyList<RosterAcquireRequest> requests);

        void Apply(CharacterChanges changes);
    }

    internal interface ICharacterBufferStore : IServiceModule
    {
        PreparedRosterAcquisition PrepareAcquire(IReadOnlyList<RosterAcquireRequest> requests);
        /// <summary>새 Run 로스터를 빈 로스터 기준으로 준비한다. 설치 전에 ClearRoster가 먼저 실행되어야 한다.</summary>
        PreparedRosterAcquisition PrepareInitialRoster(IReadOnlyList<RosterAcquireRequest> requests);
        void InstallAcquire(PreparedRosterAcquisition prepared);
        PreparedTraitGrowthChange PrepareGrowth(TraitGrowthChange change);
        PreparedTraitGrowthChange PrepareTraining(OwnedCharacterRecord expected, TraitKey key);
        void InstallGrowth(PreparedTraitGrowthChange prepared);
        PreparedMainSpecializationChange PrepareMainSpecialization(MainSpecializationChange change);
        void InstallMainSpecialization(PreparedMainSpecializationChange prepared);
        PreparedPlayerSkillUpgrade PreparePlayerSkillUpgrade(OwnedCharacterRecord expected, OwnedCharacterRecord final);
        void InstallPlayerSkillUpgrade(PreparedPlayerSkillUpgrade prepared);
        /// <summary>참가 목록은 Combat 상태가 소유한다. 이 Store는 목록을 저장하지 않고 넘겨받은 목록으로 전투 캐릭터 묶음을 검증한다.</summary>
        PreparedCharacterBattle PrepareBattle(IReadOnlyList<CharacterCombatFacts> characters, RunDeckSnapshot membership);
        void InstallBattle(PreparedCharacterBattle prepared);
        /// <param name="membership">활성 전투의 참가 목록(Combat 상태)이다. 전투가 없을 때만 null이다.</param>
        PreparedCharacterChanges PrepareChanges(CharacterChanges changes, RunDeckSnapshot membership);
        /// <summary>전투 명령 게시 전용이다. 적 생성(Created)은 Combat Store의 할당·소환 기록 검증과 함께 이 경로로만 허용한다.</summary>
        PreparedCharacterChanges PrepareCommandChanges(CharacterChanges changes, RunDeckSnapshot membership, MovementUndoResult undo = null,
            IReadOnlyList<CombatReinforcementEvent> reinforcements = null);
        void ValidateCommandInput(CombatCommandInput input, RunDeckSnapshot membership);
        void InstallChanges(PreparedCharacterChanges prepared);
        void EndBattle();
        long PrepareClearRoster();
        void ClearRoster(long revision);
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

        private readonly TraitGrowthOfferState _traitGrowth = new();
        internal ITraitGrowthResultReadSink TraitGrowthResults => _traitGrowth;
        private long _nextOwnedId;
        private readonly Dictionary<CharacterInstanceId, MainCharacterPersistentState> _mainStates = new();

        public long RosterRevision
        {
            get; private set;
        }

        public bool TryGetMainState(CharacterInstanceId id, out MainCharacterPersistentState state) =>
            _mainStates.TryGetValue(id, out state);

        public IReadOnlyList<OwnedCharacterRecord> Owned => _readOnlyOwned;
        public IReadOnlyList<CharacterData> Characters => _readOnlyCharacters;

        public CharacterBuffer() : this(1)
        {
        }

        internal CharacterBuffer(long nextOwnedId, long rosterRevision = 0)
        {
            if (nextOwnedId < 1 || nextOwnedId > (long)int.MaxValue + 1)
                throw new ArgumentOutOfRangeException(nameof(nextOwnedId));

            if (rosterRevision < 0)
                throw new ArgumentOutOfRangeException(nameof(rosterRevision));
            RosterRevision = rosterRevision;
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

        PreparedTraitGrowthChange ICharacterBufferStore.PrepareGrowth(TraitGrowthChange change)
        {
            if (change == null)
                throw new ArgumentNullException(nameof(change));
            return PrepareOwnedGrowth(change.Expected, change.Final);
        }

        PreparedTraitGrowthChange ICharacterBufferStore.PrepareTraining(OwnedCharacterRecord expected, TraitKey key)
        {
            if (expected == null) throw new ArgumentNullException(nameof(expected));
            return PrepareOwnedGrowth(expected, expected.WithGrowth(expected.Growth.WithPurchase(key)));
        }

        private PreparedTraitGrowthChange PrepareOwnedGrowth(OwnedCharacterRecord expected, OwnedCharacterRecord final)
        {
            if (_battleRecords.Count != 0)
                throw new InvalidOperationException("전투 기록 정리 후에 특성을 바꿀 수 있습니다.");
            if (!_owned.TryGetValue(expected.Id, out var current) || !current.SameValues(expected))
                throw new InvalidOperationException("특성 성장 계산의 보유 원본이 변경되었습니다.");
            int index = _ownedItems.IndexOf(current);
            if (index < 0)
                throw new InvalidOperationException("보유 목록과 인덱스가 일치하지 않습니다.");
            return new PreparedTraitGrowthChange(index, final,
                current.SameValues(final) ? RosterRevision : checked(RosterRevision + 1), RosterRevision);
        }

        void ICharacterBufferStore.InstallGrowth(PreparedTraitGrowthChange prepared)
        {
            RequireRevision(prepared.ExpectedRevision);
            if (prepared.Revision == RosterRevision)
                return;
            _ownedItems[prepared.Index] = prepared.Final;
            _owned[prepared.Final.Id] = prepared.Final;
            RosterRevision = prepared.Revision;
        }

        PreparedMainSpecializationChange ICharacterBufferStore.PrepareMainSpecialization(MainSpecializationChange change)
        {
            int index = RequireMainSpecializationSource(change);
            return new PreparedMainSpecializationChange(this, change, index, RosterRevision, checked(RosterRevision + 1));
        }

        void ICharacterBufferStore.InstallMainSpecialization(PreparedMainSpecializationChange prepared)
        {
            if (prepared == null)
                throw new ArgumentNullException(nameof(prepared));
            if (!ReferenceEquals(prepared.Owner, this) || prepared.Installed)
                throw new InvalidOperationException("다른 저장소의 준비 결과이거나 이미 설치한 전직입니다.");
            RequireRevision(prepared.ExpectedRevision);
            if (RequireMainSpecializationSource(prepared.Change) != prepared.Index ||
                prepared.Revision != checked(RosterRevision + 1))
                throw new InvalidOperationException("전직 준비의 보유 위치 또는 revision이 다릅니다.");
            // 기존 항목만 교체한다. 모든 실패 검사는 첫 쓰기 전에 끝낸다.
            _ownedItems[prepared.Index] = prepared.Change.Final;
            _owned[prepared.Change.Final.Id] = prepared.Change.Final;
            RosterRevision = prepared.Revision;
            prepared.MarkInstalled();
        }

        private int RequireMainSpecializationSource(MainSpecializationChange change)
        {
            if (change == null)
                throw new ArgumentNullException(nameof(change));
            _ = new MainSpecializationChange(change.Expected, change.Final);
            if (_battleRecords.Count != 0)
                throw new InvalidOperationException("전투 기록 정리 후에 전직할 수 있습니다.");
            if (!_owned.TryGetValue(change.Expected.Id, out var current) || !current.SameValues(change.Expected))
                throw new InvalidOperationException("전직 계산의 보유 원본이 변경되었습니다.");
            int index = _ownedItems.IndexOf(current);
            if (index < 0)
                throw new InvalidOperationException("보유 목록과 인덱스가 일치하지 않습니다.");
            return index;
        }

        PreparedPlayerSkillUpgrade ICharacterBufferStore.PreparePlayerSkillUpgrade(OwnedCharacterRecord expected, OwnedCharacterRecord final)
        {
            if (expected == null || final == null || expected.Role != CharacterOwnershipRole.Main ||
                expected.PlayerSkillUpgrade != PlayerSkillUpgradeKey.None || final.PlayerSkillUpgrade == PlayerSkillUpgradeKey.None ||
                !expected.WithPlayerSkillUpgrade(final.PlayerSkillUpgrade).SameValues(final))
                throw new ArgumentException("메인의 스킬 강화만 바꿀 수 있습니다.", nameof(final));
            int index = RequirePlayerSkillUpgradeSource(expected);
            return new PreparedPlayerSkillUpgrade(this, expected, final, index, RosterRevision, checked(RosterRevision + 1));
        }

        void ICharacterBufferStore.InstallPlayerSkillUpgrade(PreparedPlayerSkillUpgrade prepared)
        {
            if (prepared.Owner != this)
                throw new InvalidOperationException("다른 캐릭터 저장소에서 준비한 스킬 강화입니다.");
            RequireRevision(prepared.ExpectedRevision);
            if (RequirePlayerSkillUpgradeSource(prepared.Expected) != prepared.Index)
                throw new InvalidOperationException("스킬 강화의 보유 목록 위치가 변경되었습니다.");
            _ownedItems[prepared.Index] = prepared.Final;
            _owned[prepared.Final.Id] = prepared.Final;
            RosterRevision = prepared.Revision;
        }

        private int RequirePlayerSkillUpgradeSource(OwnedCharacterRecord expected)
        {
            if (_battleRecords.Count != 0)
                throw new InvalidOperationException("전투 정리 후에 스킬을 강화할 수 있습니다.");
            if (!_owned.TryGetValue(expected.Id, out var current) || !current.SameValues(expected))
                throw new InvalidOperationException("스킬 강화의 보유 원본이 변경되었습니다.");
            int index = _ownedItems.IndexOf(current);
            if (index < 0)
                throw new InvalidOperationException("보유 목록과 인덱스가 일치하지 않습니다.");
            return index;
        }

        public RosterAcquireResult Acquire(IReadOnlyList<RosterAcquireRequest> requests)
        {
            PreparedRosterAcquisition prepared = PrepareAcquire(requests);
            if (prepared.Result.Succeeded)
                InstallAcquire(prepared);

            return prepared.Result;
        }

        public PreparedRosterAcquisition PrepareAcquire(IReadOnlyList<RosterAcquireRequest> requests)
            => PrepareAcquire(requests, asEmptyRoster: false);

        public PreparedRosterAcquisition PrepareInitialRoster(IReadOnlyList<RosterAcquireRequest> requests)
            => PrepareAcquire(requests, asEmptyRoster: true);

        private PreparedRosterAcquisition PrepareAcquire(IReadOnlyList<RosterAcquireRequest> requests,
            bool asEmptyRoster)
        {
            if (requests == null)
                throw new ArgumentNullException(nameof(requests));
            long expectedRevision = RosterRevision;
            long expectedNextId = _nextOwnedId;
            requests = requests.ToArray();
            if (RosterRevision != expectedRevision || _nextOwnedId != expectedNextId)
                throw new InvalidOperationException("획득 요청을 읽는 동안 로스터가 변경되었습니다.");
            if (requests.Count == 0)
                return PreparedRosterAcquisition.Failure(RosterAcquireFailureReason.InvalidRequest);

            // 정의당 보유 수 같은 소유 한도는 Core 소유 규칙(CharacterRosterRules.ValidateOwnership)이 정한다.
            // 여기서는 ID별 저장에 필요한 형식(메인은 지속 상태를, 서브는 없음)만 확인한다.
            for (int i = 0; i < requests.Count; i++)
            {
                var request = requests[i];
                if (request == null)
                    return PreparedRosterAcquisition.Failure(RosterAcquireFailureReason.InvalidRequest);
                if (request.Role == CharacterOwnershipRole.Main)
                {
                    var state = request.InitialMain;
                    if (state == null || state.CurrentHealth > state.PersistentMaxHealth)
                        return PreparedRosterAcquisition.Failure(RosterAcquireFailureReason.InvalidMainState);
                }
                else if (request.InitialMain != null)
                    return PreparedRosterAcquisition.Failure(RosterAcquireFailureReason.InvalidMainState);
            }

            long lastId = _nextOwnedId + requests.Count - 1L;
            if (lastId > int.MaxValue)
                return PreparedRosterAcquisition.Failure(RosterAcquireFailureReason.IdentityOverflow);

            long revision = checked(RosterRevision + 1);
            var mains = new Dictionary<CharacterInstanceId, MainCharacterPersistentState>();
            var acquired = new OwnedCharacterRecord[requests.Count];
            for (int i = 0; i < acquired.Length; i++)
            {
                RosterAcquireRequest request = requests[i];
                var id = new CharacterInstanceId((int)(_nextOwnedId + i));
                acquired[i] = new OwnedCharacterRecord(id, request.CharacterKey, request.Role, request.InitialLevel);
                if (request.Role == CharacterOwnershipRole.Main)
                    mains.Add(id, request.InitialMain);
            }

            // 거래의 첫 쓰기 전에 보유 저장소의 확장 할당을 끝낸다.
            int requiredCapacity = asEmptyRoster ? acquired.Length : checked(_ownedItems.Count + acquired.Length);
            if (_ownedItems.Capacity < requiredCapacity)
                _ownedItems.Capacity = requiredCapacity;
            _owned.EnsureCapacity(requiredCapacity);
            _souls.EnsureCapacity(requiredCapacity);
            _mainStates.EnsureCapacity(asEmptyRoster ? mains.Count : checked(_mainStates.Count + mains.Count));
            return new PreparedRosterAcquisition(RosterAcquireResult.Success(acquired),
                mains, lastId + 1L, revision, asEmptyRoster, expectedRevision);
        }

        public void InstallAcquire(PreparedRosterAcquisition prepared)
        {
            RequireRevision(prepared.ExpectedRevision);
            if (prepared.ReplaceRoster)
                ClearRosterItems();
            foreach (OwnedCharacterRecord record in prepared.Result.Acquired)
            {
                _owned.Add(record.Id, record);
                _ownedItems.Add(record);
                _souls.Add(record.Id, 0);

                if (record.Role == CharacterOwnershipRole.Main)
                {
                    _mainStates.Add(record.Id, prepared.Mains[record.Id]);
                }
            }

            _nextOwnedId = prepared.NextId;
            RosterRevision = prepared.Revision;
        }

        /// <summary>
        /// 전투가 없을 때의 직접 변경이다. 전투가 설치된 동안의 변경은 참가 목록을 검증하는 전투 거래(BattleWriteSink)로만 게시한다.
        /// </summary>
        public void Apply(CharacterChanges changes)
        {
            if (_battleRecords.Count != 0)
                throw new InvalidOperationException("전투 중 캐릭터 변경은 전투 거래로만 게시할 수 있습니다.");
            var prepared = PrepareChanges(changes, null);
            InstallChanges(prepared);
        }

        PreparedCharacterChanges ICharacterBufferStore.PrepareChanges(CharacterChanges changes, RunDeckSnapshot membership) =>
            PrepareChanges(changes, membership);
        PreparedCharacterChanges ICharacterBufferStore.PrepareCommandChanges(CharacterChanges changes, RunDeckSnapshot membership, MovementUndoResult undo,
            IReadOnlyList<CombatReinforcementEvent> reinforcements) =>
            PrepareChanges(changes, membership ?? throw new ArgumentNullException(nameof(membership)), commandPublication: true, undo: undo,
                reinforcements: reinforcements);
        void ICharacterBufferStore.ValidateCommandInput(CombatCommandInput input, RunDeckSnapshot membership)
        {
            if (membership == null)
                throw new ArgumentNullException(nameof(membership));
            // 명령 계산은 참가 목록의 보유·영혼·메인만 읽는다. 목록과 다른 입력이나 이전 전투의 목록은 거절한다.
            if (input.Characters.Count != _characterViews.Count || input.Owned.Count != membership.OrderedIds.Count ||
                input.SoulCounts.Count != membership.OrderedIds.Count ||
                !ReferenceEquals(input.Membership, membership) || input.MainId != membership.MainId ||
                !input.Owned.Select(o => o.Id).OrderBy(id => id.Value).SequenceEqual(membership.OrderedIds.OrderBy(id => id.Value)) ||
                !_mainStates.TryGetValue(input.MainId, out var main) || !main.SameValues(input.Main))
                throw new InvalidOperationException("명령 계산의 캐릭터·보유·영혼·메인 원본이 변경되었습니다.");
            foreach (var expected in input.Characters)
                if (!_viewsByEntity.TryGetValue(expected.EntityId, out var current) ||
                    !expected.SameValues(current.ToFacts()))
                    throw new InvalidOperationException("명령 계산이 읽은 캐릭터 원본이 변경되었습니다.");
            foreach (var expected in input.Owned)
                if (!_owned.TryGetValue(expected.Id, out var current) || !current.SameValues(expected) ||
                    !input.SoulCounts.TryGetValue(expected.Id, out int count) ||
                    _souls[expected.Id] != count)
                    throw new InvalidOperationException("명령 계산이 읽은 보유·영혼 원본이 변경되었습니다.");
        }
        void ICharacterBufferStore.InstallChanges(PreparedCharacterChanges prepared) => InstallChanges(prepared);

        /// <param name="commandPublication">
        /// 전투 명령 게시 경로인지다. 전투 중 적 생성, 보유 기록(레벨·진행 경험치) 변경, 전투 획득 경험치 증가는 이 경로에서만 받는다.
        /// 공개 쓰기 경로가 이것들을 바꾸면 Core 판단 없이 레벨을 바꾸는 길이 되므로 거절한다.
        /// </param>
        private PreparedCharacterChanges PrepareChanges(CharacterChanges changes, RunDeckSnapshot membership,
            bool commandPublication = false, MovementUndoResult undo = null, IReadOnlyList<CombatReinforcementEvent> reinforcements = null)
        {
            if (changes == null)
                throw new ArgumentNullException(nameof(changes));
            // 단계 순서가 예외 순서다. 단계를 바꾸거나 합치지 않는다.
            RequireChangeScope(changes, membership, commandPublication);
            var proposedOwned = PrepareOwnedChanges(changes);
            var finalOwned = new Dictionary<CharacterInstanceId, OwnedCharacterRecord>(_owned);
            foreach (var pair in proposedOwned) finalOwned[pair.Key] = pair.Value;
            RequireSingleChangePerTarget(changes);
            var proposedSouls = PrepareSoulChanges(changes);
            var proposedMain = PrepareMainChange(changes);
            var proposedRecords = new Dictionary<EntityId, BattleCharacterRecord>(_battleRecords);
            PrepareCharacterRecords(changes, commandPublication, proposedRecords, proposedMain, proposedSouls, finalOwned, undo);
            PrepareCreatedRecords(changes, commandPublication, proposedRecords, proposedMain, proposedSouls, finalOwned, reinforcements);
            ValidateProposedState(proposedRecords, proposedMain, proposedSouls, finalOwned);
            return CreatePrepared(changes, proposedOwned, proposedSouls, proposedMain, proposedRecords);
        }

        /// <summary>쓰기 경로별 허용 범위와 전투 참가 목록 범위를 확인한다.</summary>
        private void RequireChangeScope(CharacterChanges changes, RunDeckSnapshot membership, bool commandPublication)
        {
            // 전투가 설치된 동안의 변경은 참가 목록으로만 검증된다. 목록 없는 전투 중 변경은 받지 않는다.
            if (_battleRecords.Count != 0 && membership == null)
                throw new InvalidOperationException("전투 중 캐릭터 변경에는 참가 목록이 필요합니다.");
            if (!commandPublication && changes.Owned.Count != 0)
                throw new InvalidOperationException("보유 기록 변경은 전투 명령 게시로만 반영할 수 있습니다.");

            if (membership != null &&
                (changes.Main != null && changes.Main.Id != membership.MainId ||
                changes.Owned.Any(c => !membership.OrderedIds.Contains(c.Expected.Id)) ||
                changes.Souls.Any(c => !membership.OrderedIds.Contains(c.Id)) ||
                changes.Created.Any(c => c.Owner == Owner.Player && (!c.OwnedId.HasValue || !membership.OrderedIds.Contains(c.OwnedId.Value)))))
                throw new InvalidOperationException("전투 참가 목록 밖의 보유 상태를 바꿀 수 없습니다.");
        }

        /// <summary>보유 기록 변경의 기대값을 확인하고 제안 값을 모은다.</summary>
        private Dictionary<CharacterInstanceId, OwnedCharacterRecord> PrepareOwnedChanges(CharacterChanges changes)
        {
            var proposedOwned = new Dictionary<CharacterInstanceId, OwnedCharacterRecord>();
            foreach (OwnedCharacterChange change in changes.Owned)
            {
                if (_owned.TryGetValue(change.Expected.Id, out OwnedCharacterRecord current) == false ||
                    current.SameValues(change.Expected) == false)
                    throw new InvalidOperationException("보유 기록 변경의 예상 값이 현재 상태와 다릅니다.");

                proposedOwned.Add(change.Final.Id, change.Final);
            }
            return proposedOwned;
        }

        /// <summary>한 배치에서 같은 캐릭터·영혼 개수를 두 번 바꾸지 않는지 확인한다.</summary>
        private static void RequireSingleChangePerTarget(CharacterChanges changes)
        {
            var changedEntities = new HashSet<EntityId>();
            foreach (CharacterStateChange change in changes.Characters)
                if (changedEntities.Add(change.Expected.EntityId) == false)
                    throw new InvalidOperationException("한 배치에서 같은 캐릭터를 두 번 바꿀 수 없습니다.");

            var changedSouls = new HashSet<CharacterInstanceId>();
            foreach (SoulCountChange change in changes.Souls)
                if (changedSouls.Add(change.Id) == false)
                    throw new InvalidOperationException("한 배치에서 같은 영혼 개수를 두 번 바꿀 수 없습니다.");
        }

        /// <summary>영혼 개수 변경의 기대값을 확인하고 제안 값을 만든다.</summary>
        private Dictionary<CharacterInstanceId, int> PrepareSoulChanges(CharacterChanges changes)
        {
            var proposedSouls = new Dictionary<CharacterInstanceId, int>(_souls);
            foreach (SoulCountChange change in changes.Souls)
            {
                if (proposedSouls.TryGetValue(change.Id, out int current) == false ||
                    current != change.ExpectedCount)
                    throw new InvalidOperationException("영혼 변경의 예상 값이 현재 상태와 다릅니다.");

                proposedSouls[change.Id] = change.FinalCount;
            }
            return proposedSouls;
        }

        /// <summary>메인 지속 상태 변경의 대상과 기대값을 확인하고 제안 값을 만든다.</summary>
        private Dictionary<CharacterInstanceId, MainCharacterPersistentState> PrepareMainChange(CharacterChanges changes)
        {
            var proposedMain = new Dictionary<CharacterInstanceId, MainCharacterPersistentState>(_mainStates);
            if (changes.Main != null)
            {
                if (_battleRecords.Values.Any(record => record.IsMain && record.OwnedId != changes.Main.Id))
                    throw new InvalidOperationException("메인 변경 대상은 준비된 전투의 메인과 같아야 합니다.");
                if (!proposedMain.TryGetValue(changes.Main.Id, out var expectedMain) ||
                    !expectedMain.SameValues(changes.Main.Expected))
                    throw new InvalidOperationException("메인 지속 상태의 예상 값이 현재 상태와 다릅니다.");

                if (!expectedMain.SameValues(changes.Main.Final))
                    proposedMain[changes.Main.Id] = changes.Main.Final;
            }
            return proposedMain;
        }

        /// <summary>기존 전투 캐릭터 변경의 기대값·식별·획득 경험치·스테이지 기록을 확인하고 제안 기록에 반영한다.</summary>
        private void PrepareCharacterRecords(CharacterChanges changes, bool commandPublication,
            Dictionary<EntityId, BattleCharacterRecord> proposedRecords,
            Dictionary<CharacterInstanceId, MainCharacterPersistentState> proposedMain,
            Dictionary<CharacterInstanceId, int> proposedSouls,
            IReadOnlyDictionary<CharacterInstanceId, OwnedCharacterRecord> finalOwned, MovementUndoResult undo)
        {
            foreach (CharacterStateChange change in changes.Characters)
            {
                if (proposedRecords.TryGetValue(change.Expected.EntityId,
                        out BattleCharacterRecord current) == false)
                    throw new InvalidOperationException("변경할 전투 캐릭터를 찾을 수 없습니다.");

                CharacterCombatFacts currentFacts = current.BuildFacts(_mainStates);
                if (currentFacts.SameValues(change.Expected) == false)
                    throw new InvalidOperationException("캐릭터 변경의 예상 값이 현재 상태와 다릅니다.");
                if (change.Expected.SameIdentity(change.Final) == false)
                    throw new InvalidOperationException("캐릭터 변경으로 식별자를 바꿀 수 없습니다.");
                // 이번 전투 획득 경험치는 승리 때 보유 기록에 반영된다. 명령 게시는 쌓기만 하고(줄지 않음),
                // 다른 쓰기 경로는 바꿀 수 없다. 그렇지 않으면 Core 판단 없이 레벨을 올리는 길이 된다.
                int earnedBefore = change.Expected.Experience.Earned;
                int earnedAfter = change.Final.Experience.Earned;
                if (commandPublication ? earnedAfter < earnedBefore : earnedAfter != earnedBefore)
                    throw new InvalidOperationException(commandPublication
                        ? "전투 획득 경험치는 줄어들 수 없습니다."
                        : "전투 획득 경험치는 전투 명령 게시로만 바꿀 수 있습니다.");
                // 스테이지 기록도 같은 수명·같은 규칙이다. 경험치 계산의 입력이므로 다른 쓰기 경로가 바꾸면 안 된다.
                CharacterStageRecord recordBefore = change.Expected.StageRecord;
                CharacterStageRecord recordAfter = change.Final.StageRecord;
                bool undoActor = commandPublication && undo != null && undo.ActorId == currentFacts.EntityId;
                if (undoActor ? !recordAfter.SameValues(recordBefore.UndoMovement(undo.DistanceReverted)) :
                    commandPublication ? !recordAfter.IsAtLeast(recordBefore) : !recordAfter.SameValues(recordBefore))
                    throw new InvalidOperationException(commandPublication
                        ? "스테이지 기록은 줄어들 수 없습니다."
                        : "스테이지 기록은 전투 명령 게시로만 바꿀 수 있습니다.");

                // 메인 체력을 지속 원본으로 합성하기 전에 전달된 최종 값의 일관성을 확인한다.
                ValidateBattleRecord(change.Final, proposedMain, proposedSouls, finalOwned);
                proposedRecords[change.Final.EntityId] = new BattleCharacterRecord(change.Final);
            }
        }

        /// <summary>새로 생기는 전투 캐릭터(전투 중 적 소환·손패 서브)를 확인하고 제안 기록에 더한다.</summary>
        private void PrepareCreatedRecords(CharacterChanges changes, bool commandPublication,
            Dictionary<EntityId, BattleCharacterRecord> proposedRecords,
            Dictionary<CharacterInstanceId, MainCharacterPersistentState> proposedMain,
            Dictionary<CharacterInstanceId, int> proposedSouls,
            IReadOnlyDictionary<CharacterInstanceId, OwnedCharacterRecord> finalOwned, IReadOnlyList<CombatReinforcementEvent> reinforcements)
        {
            foreach (var created in changes.Created)
            {
                // 증원은 생성 직후 받은 효과를 포함한 최종 사실로 설치한다. 초기 진행 검사는 생성 사건에 적용한다.
                // 생성 사건과 최종 사실의 연결은 같은 거래의 Combat Store가 설치 전에 검증한다.
                var initial = commandPublication && created.Owner == Owner.Enemy
                    ? reinforcements?.SingleOrDefault(e => e.Created.EntityId == created.EntityId)?.Created
                    : null;
                RequireFreshBattleProgress(initial ?? created);
                if (created.Owner == Owner.Enemy)
                {
                    // 전투 중 소환 개체다. 할당 범위·소환 기록·참가자 일관성은 같은 거래의 Combat Store가 검증한다.
                    // 같은 명령에서 밤이 끝나면 살아 있는 생성 개체는 곧바로 EnemyWaiting으로 회수될 수 있다.
                    bool validArea = created.Health.IsDead ? created.Area == CharacterRuntimeArea.Excluded :
                        created.Area == CharacterRuntimeArea.Field || created.Area == CharacterRuntimeArea.EnemyWaiting;
                    if (!commandPublication || created.OwnedId.HasValue || created.OwnershipRole.HasValue || !validArea ||
                        proposedRecords.ContainsKey(created.EntityId))
                        throw new InvalidOperationException("전투 명령 밖의 적 생성이거나 생성할 적의 식별자·위치가 잘못되었습니다.");
                    ValidateBattleRecord(created, proposedMain, proposedSouls, finalOwned);
                    proposedRecords.Add(created.EntityId, new BattleCharacterRecord(created));
                    continue;
                }
                if (created.Owner != Owner.Player || created.OwnershipRole != CharacterOwnershipRole.Sub ||
                    created.Area != CharacterRuntimeArea.Hand || !created.OwnedId.HasValue ||
                    !_owned.TryGetValue(created.OwnedId.Value, out var owned) || owned.CharacterKey != created.CharacterKey ||
                    owned.Role != created.OwnershipRole || proposedRecords.ContainsKey(created.EntityId) ||
                    proposedRecords.Values.Any(r => r.OwnedId == created.OwnedId))
                    throw new InvalidOperationException("생성할 캐릭터 식별자 또는 보유 연결이 잘못되었습니다.");
                ValidateBattleRecord(created, proposedMain, proposedSouls, finalOwned);
                proposedRecords.Add(created.EntityId, new BattleCharacterRecord(created));
            }
        }

        /// <summary>제안 전체가 메인·영혼 원본과 맞는지 마지막으로 확인한다.</summary>
        private static void ValidateProposedState(Dictionary<EntityId, BattleCharacterRecord> proposedRecords,
            Dictionary<CharacterInstanceId, MainCharacterPersistentState> proposedMain,
            Dictionary<CharacterInstanceId, int> proposedSouls,
            IReadOnlyDictionary<CharacterInstanceId, OwnedCharacterRecord> finalOwned)
        {
            foreach (var pair in proposedMain)
                if (!proposedSouls.ContainsKey(pair.Key))
                    throw new InvalidOperationException("메인 지속 상태에 대응하는 보유 캐릭터가 없습니다.");
            foreach (BattleCharacterRecord record in proposedRecords.Values)
                ValidateBattleRecord(record.BuildFacts(proposedMain), proposedMain, proposedSouls, finalOwned);
        }

        /// <summary>뷰와 revision을 계산해 준비 결과를 만든다. revision이 넘치면 실제 설치 전에 실패한다.</summary>
        private PreparedCharacterChanges CreatePrepared(CharacterChanges changes,
            Dictionary<CharacterInstanceId, OwnedCharacterRecord> proposedOwned,
            Dictionary<CharacterInstanceId, int> proposedSouls,
            Dictionary<CharacterInstanceId, MainCharacterPersistentState> proposedMain,
            Dictionary<EntityId, BattleCharacterRecord> proposedRecords)
        {
            CharacterData[] proposedViews = BuildViews(proposedRecords, proposedMain)
                .Concat(changes.Created.Select(c => new CharacterData(proposedRecords[c.EntityId].BuildFacts(proposedMain)))).ToArray();
            bool changed = proposedOwned.Any(pair => !_owned[pair.Key].SameValues(pair.Value)) ||
                proposedSouls.Any(pair => _souls[pair.Key] != pair.Value) ||
                proposedMain.Any(pair => !_mainStates[pair.Key].SameValues(pair.Value));
            long revision = changed ? checked(RosterRevision + 1) : RosterRevision;
            return new PreparedCharacterChanges(changes, proposedMain, proposedRecords, proposedViews, proposedSouls,
                proposedOwned, revision, RosterRevision);
        }

        private void InstallChanges(PreparedCharacterChanges prepared)
        {
            RequireRevision(prepared.ExpectedRevision);
            // 모든 저장소가 준비된 뒤 기존 항목만 교체한다.
            foreach (SoulCountChange change in prepared.Changes.Souls)
                _souls[change.Id] = change.FinalCount;

            // 보유 목록의 순서는 획득 순서이므로 같은 자리를 교체한다.
            foreach (KeyValuePair<CharacterInstanceId, OwnedCharacterRecord> owned in prepared.Owned)
            {
                _ownedItems[_ownedItems.IndexOf(_owned[owned.Key])] = owned.Value;
                _owned[owned.Key] = owned.Value;
            }

            foreach (var pair in prepared.Main)
                _mainStates[pair.Key] = pair.Value;
            RosterRevision = prepared.Revision;
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
            sink.AddService<ITraitGrowthStateReadSink>(_traitGrowth);
            sink.AddService<ITraitGrowthResultReadSink>(_traitGrowth);
            sink.AddService<ITraitGrowthStateStore>(_traitGrowth);
            sink.AddService<ICharacterBufferReadSink>(this);
            sink.AddService<ICharacterBufferWriteSink>(this);
            sink.AddService<ICharacterBufferStore>(this);
        }

        PreparedCharacterBattle ICharacterBufferStore.PrepareBattle(
            IReadOnlyList<CharacterCombatFacts> characters, RunDeckSnapshot membership)
        {
            if (characters == null)
                throw new ArgumentNullException(nameof(characters));
            if (characters.Count == 0)
                throw new ArgumentException("전투 준비에는 캐릭터가 한 명 이상 필요합니다.", nameof(characters));
            if (_battleRecords.Count != 0)
                throw new InvalidOperationException("이미 준비된 전투가 있습니다.");
            // 참가 목록의 규칙(장수·메인 하나·순서)은 Core가 확정할 때 판정했다. 여기서는 이 묶음이 목록을 가리키는지만 본다.
            if (membership == null)
                throw new ArgumentNullException(nameof(membership));
            foreach (var id in membership.OrderedIds)
                if (!_owned.ContainsKey(id))
                    throw new InvalidOperationException("참가 목록의 ID가 현재 보유에 없습니다.");
            if (_owned[membership.MainId].Role != CharacterOwnershipRole.Main || !_mainStates.ContainsKey(membership.MainId))
                throw new InvalidOperationException("참가 목록의 메인 ID가 보유 메인이 아닙니다.");

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

                    if (!membership.OrderedIds.Contains(ownedId) ||
                        owned.Role == CharacterOwnershipRole.Main && ownedId != membership.MainId)
                        throw new InvalidOperationException("전투 캐릭터가 참가 목록 밖이거나 목록의 메인과 다릅니다.");
                    includesMain |= ownedId == membership.MainId;
                }
                else if (facts.Owner == Owner.Player)
                {
                    throw new InvalidOperationException("플레이어 캐릭터에는 보유 ID가 필요합니다.");
                }

                RequireFreshBattleProgress(facts);
                ValidateBattleRecord(facts, _mainStates, _souls, _owned);
                records[i] = new BattleCharacterRecord(facts);
                views[i] = new CharacterData(records[i].BuildFacts(_mainStates));
            }

            if (includesMain == false)
                throw new InvalidOperationException("전투 준비에는 참가 목록의 메인이 포함되어야 합니다.");

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

        long ICharacterBufferStore.PrepareClearRoster() =>
            _owned.Count == 0 ? RosterRevision : checked(RosterRevision + 1);

        void ICharacterBufferStore.ClearRoster(long revision)
        {
            ClearRosterItems();
            RosterRevision = revision;
        }

        private void RequireRevision(long expected)
        {
            if (RosterRevision != expected)
                throw new InvalidOperationException("설치 전에 로스터 원본이 변경되었습니다.");
        }

        private void ClearRosterItems()
        {
            _owned.Clear();
            _ownedItems.Clear();
            _souls.Clear();
            _mainStates.Clear();
        }

        private CharacterData[] BuildViews(IReadOnlyDictionary<EntityId, BattleCharacterRecord> records,
            IReadOnlyDictionary<CharacterInstanceId, MainCharacterPersistentState> main)
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

        private static void ValidateBattleRecord(CharacterCombatFacts facts,
            IReadOnlyDictionary<CharacterInstanceId, MainCharacterPersistentState> mains,
            IReadOnlyDictionary<CharacterInstanceId, int> souls,
            IReadOnlyDictionary<CharacterInstanceId, OwnedCharacterRecord> owned)
        {
            // 값 형식의 기본값은 생성자 검증을 거치지 않으므로 저장 경계에서 거절한다.
            if (facts.Health.MaxHealth < 1 || facts.Health.CurrentHealth < 0 ||
                facts.Health.CurrentHealth > facts.Health.MaxHealth || facts.Stats.Category == null ||
                facts.Stats.PositionAttackBonus != facts.PositionModifier.AttackBonus)
                throw new InvalidOperationException("전투 캐릭터의 체력 또는 능력치가 초기화되지 않았습니다.");

            // 영혼·임시·위치 보너스의 합산 공식은 Core 계산이 소유한다. 여기서는 참조 존재와 한 곳에 저장한 값의 일치만 본다.
            if (facts.OwnedId.HasValue && souls.ContainsKey(facts.OwnedId.Value) == false)
                throw new InvalidOperationException("보유 캐릭터의 영혼 기록을 찾을 수 없습니다.");

            int hunterAttackBonus = 0;
            if (facts.OwnedId.HasValue)
            {
                if (!owned.TryGetValue(facts.OwnedId.Value, out var record))
                    throw new InvalidOperationException("특성 기여분에 대응하는 보유 기록이 없습니다.");
                if (facts.Stats.SpecializationCategory != record.SpecializationCategory)
                    throw new InvalidOperationException("전투 병과 투영이 보유 전직 원본과 다릅니다.");
                hunterAttackBonus = record.HunterAttackBonus;
            }
            if (!facts.OwnedId.HasValue && facts.Stats.SpecializationCategory.HasValue)
                throw new InvalidOperationException("보유 Main이 아닌 개체는 전직 병과를 가질 수 없습니다.");
            if (facts.Stats.TraitAttackBonus != CombatEffectiveStatsCalculator.TraitAttackBonus(facts, hunterAttackBonus))
                throw new InvalidOperationException("전투 특성 공격 투영이 보유·수신 원본과 다릅니다.");

            if (facts.OwnershipRole != CharacterOwnershipRole.Main)
                return;
            if (!facts.OwnedId.HasValue || !mains.TryGetValue(facts.OwnedId.Value, out var main))
                throw new InvalidOperationException("메인 전투 상태에 대응하는 지속 상태가 없습니다.");

            // 메인 현재 체력과 지속 능력치는 지속 원본 한 곳에만 저장하므로 전투 사실의 사본이 그 값과 같아야 한다.
            // 어떤 값이 대응하는지는 계약(CharacterCombatFacts.MatchesMainState)이 정한다.
            if (!facts.MatchesMainState(main))
                throw new InvalidOperationException("메인 전투 상태가 지속 원본과 맞지 않습니다.");
        }

        /// <summary>새로 생기는 전투 사실은 번 경험치·스테이지 기록·받은 특성 효과 없이 시작한다.</summary>
        private static void RequireFreshBattleProgress(CharacterCombatFacts facts)
        {
            if (facts.Experience.Earned != 0 || !facts.StageRecord.IsEmpty || facts.StatusEffects.Entries.Count != 0 ||
                !facts.TraitEffects.SameValues(CharacterTraitEffects.Empty) || facts.Debuffs.ArmorBreak != null ||
                facts.Stats.TraitStatuses.ToArray().Any(t => t is AccelerationTraitStatusSnapshot a && a.HeldAttackBonus != 0 ||
                    t is StationaryFireTraitStatusSnapshot s && s.MovedThisOwnTurn ||
                    t is RegenerationTraitStatusSnapshot r && r.CompletedEligibleTurnEnds != 0 ||
                    t is HunterTraitStatusSnapshot h && (h.TargetId.HasValue || h.SuccessfulRewards != 0)))
                throw new InvalidOperationException("새 전투 캐릭터는 획득 경험치·스테이지 기록·받은 특성 효과 없이 시작해야 합니다.");
        }

    }

    internal sealed class PreparedRosterAcquisition
    {
        internal RosterAcquireResult Result { get; }
        internal IReadOnlyDictionary<CharacterInstanceId, MainCharacterPersistentState> Mains { get; }
        internal long NextId { get; }
        internal long Revision { get; }
        internal long ExpectedRevision { get; }
        internal bool ReplaceRoster { get; }

        internal PreparedRosterAcquisition(RosterAcquireResult result,
            IReadOnlyDictionary<CharacterInstanceId, MainCharacterPersistentState> mains, long nextId,
            long revision, bool replaceRoster, long expectedRevision)
        {
            Result = result;
            Mains = mains;
            NextId = nextId;
            Revision = revision;
            ExpectedRevision = expectedRevision;
            ReplaceRoster = replaceRoster;
        }

        internal static PreparedRosterAcquisition Failure(RosterAcquireFailureReason reason) =>
            new PreparedRosterAcquisition(RosterAcquireResult.Failure(reason), null, 0, 0, false, 0);
    }

    internal sealed class PreparedCharacterChanges
    {
        internal CharacterChanges Changes { get; }
        internal IReadOnlyDictionary<CharacterInstanceId, MainCharacterPersistentState> Main { get; }
        internal long Revision { get; }
        internal long ExpectedRevision { get; }
        internal IReadOnlyDictionary<EntityId, BattleCharacterRecord> Records { get; }
        internal CharacterData[] Views { get; }
        internal IReadOnlyDictionary<CharacterInstanceId, int> Souls { get; }
        internal IReadOnlyDictionary<CharacterInstanceId, OwnedCharacterRecord> Owned { get; }
        internal PreparedCharacterChanges(CharacterChanges changes, IReadOnlyDictionary<CharacterInstanceId, MainCharacterPersistentState> main,
            IReadOnlyDictionary<EntityId, BattleCharacterRecord> records, CharacterData[] views,
            IReadOnlyDictionary<CharacterInstanceId, int> souls,
            IReadOnlyDictionary<CharacterInstanceId, OwnedCharacterRecord> owned, long revision, long expectedRevision)
        {
            ExpectedRevision = expectedRevision;
            Revision = revision;
            Changes = changes;
            Main = main;
            Records = records;
            Views = views;
            Souls = souls;
            Owned = owned;
        }
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
        internal bool IsMain => _facts.OwnershipRole == CharacterOwnershipRole.Main;

        internal BattleCharacterRecord(CharacterCombatFacts facts)
        {
            _facts = facts ?? throw new ArgumentNullException(nameof(facts));
            _battleCurrentHealth = facts.OwnershipRole == CharacterOwnershipRole.Main
                ? null
                : facts.Health.CurrentHealth;
        }

        internal CharacterCombatFacts BuildFacts(IReadOnlyDictionary<CharacterInstanceId, MainCharacterPersistentState> mains)
        {
            MainCharacterPersistentState main = null;
            if (_facts.OwnedId.HasValue)
                mains.TryGetValue(_facts.OwnedId.Value, out main);
            int currentHealth = _battleCurrentHealth ?? main?.CurrentHealth ??
                throw new InvalidOperationException("메인 지속 체력을 찾을 수 없습니다.");

            // 메인 체력만 지속 원본 값으로 바꾼다. 나머지 사실은 계약 사본이 그대로 옮긴다.
            return _facts.WithHealth(new HealthStateSnapshot(currentHealth, _facts.Health.MaxHealth));
        }
    }

    #endregion
}
