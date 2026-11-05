using ProjectL.Systems.ServiceKeys.Runtime;
using System;
using UnityEngine;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    /// <summary>
    /// 덱 정보 카드의 데이터 조회입니다. Buffer·DB·보유 카드에서 값을 읽어 같은 오브젝트의 <see cref="UnitCardView"/>에 넘깁니다.
    /// 카드 표시는 UnitCardView, 공용 상세의 선택·개폐는 수신자가 맡습니다(손패 카드화 설계 C1·C4).
    /// </summary>
    [RequireComponent(typeof(UnitCardView))]
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Systems", sourceAssembly: "ProjectL.Systems", sourceClassName: "UnitCardUI")]
    public sealed class UnitCardUI : UIBase, IUIInputReceiver
    {
        private UnitCardView _view;
        private ICharacterBufferReadSink _characters;
        private ICombatStateBufferReadSink _combat;
        private bool _displayedDeployment;
        private OwnedCharacterStatsReader _ownedStats;
        private OwnedCharacterStatsSnapshot _displayedStats;
        private IUIRuntimeFaultHandler _faults;
        private IUnitVisualTableQuery _unitVisuals;
        private ITraitVisualTableQuery _traitVisuals;
        private EntityId _entityId;
        private CharacterInstanceId _ownedId;
        private OwnedCharacterRecord _displayedOwned;
        private bool _hasCharacter;
        private CharacterData _displayed;
        private int? _level;
        private IUnitCardInfoReceiver _infoReceiver;
        private UnitInfoContent _infoContent;
        internal int DisplayRevision { get; private set; }

        public UnitCardUI()
        {
            Policy = UIOpenPolicy.Parallel;
        }

        public override UIId ID => UIId.UnitCard;

        public override void BindUISystemService(IUISystemService service)
        {
            base.BindUISystemService(service);
            _faults = service as IUIRuntimeFaultHandler;
        }
        public EntityId CharacterId => _displayed?.EntityId ?? _entityId;
        public CharacterInstanceId OwnedCharacterId => _ownedId;

        /// <summary>같은 오브젝트의 표시 컴포넌트입니다. EditMode에서는 Awake가 없으므로 처음 쓸 때 찾습니다.</summary>
        public UnitCardView View
        {
            get
            {
                if (_view == null)
                {
                    _view = GetComponent<UnitCardView>();
                    if (_view == null)
                        throw new InvalidOperationException("[UnitCardUI] 같은 오브젝트에 UnitCardView가 필요합니다.");
                }

                return _view;
            }
        }

        protected override void OnUIOpened()
        {
            _ownedStats?.Clear();
            Refresh();
        }

        protected override void OnUIHidden()
        {
            _infoReceiver?.Forget(this);
            _ownedStats?.Clear();
            _displayedStats = null;
        }

        internal void BindInfoReceiver(IUnitCardInfoReceiver receiver)
        {
            if (receiver == null) throw new ArgumentNullException(nameof(receiver));
            View.BindInfoInput(this);
            if (!ReferenceEquals(_infoReceiver, receiver)) _infoReceiver?.Forget(this);
            _infoReceiver = receiver;
        }

        void IUIInputReceiver.Click(int slot)
        {
            if (slot == 0 && isActiveAndEnabled && View.HasContent) RequireInfoReceiver().Toggle(this);
        }

        void IUIInputReceiver.HoverEnter(int slot) { }
        void IUIInputReceiver.HoverExit(int slot) { }

        private IUnitCardInfoReceiver RequireInfoReceiver() => _infoReceiver ??
            throw new InvalidOperationException($"[UnitCardUI] UIId.UnitCard, '{name}': 공용 상세 입력 바인딩이 필요합니다.");

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            var buffer = resolver.GetService(BufferSystemKey.Buffer);
            _characters = buffer.CharacterReadSink;
            _combat = buffer.CombatStateReadSink;
            _ownedStats = new OwnedCharacterStatsReader(_characters, buffer.CombatStateReadSink,
                buffer.CharacterStatsReadSink, buffer.RunDeckReadSink, _faults, buffer.CharacterStatsRequestSink);
            InvalidateDisplay();
        }

        public override void GetUIData(IServiceModule query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            _unitVisuals = (query as IHasViewQuery)?.ViewQuery.UnitVisualTableQuery;
            _traitVisuals = (query as IHasViewQuery)?.ViewQuery.TraitVisualTableQuery;
            InvalidateDisplay();
        }

        private void InvalidateDisplay()
        {
            // 초기화 단계에서는 상대 서비스의 준비를 가정해 조회하지 않는다.
            _displayed = null;
            _displayedOwned = null;
            _displayedStats = null;
            DisplayRevision++;
            _infoContent = null;
            _infoReceiver?.Forget(this);
            View.Clear();
        }

        /// <summary>전투 EntityId로 Buffer를 조회합니다. Level 데이터가 없으면 '-'로 표시합니다.</summary>
        public void SetCharacter(EntityId id, int? level = null)
        {
            ValidateLevel(level);
            if (_entityId != id || !_ownedId.IsEmpty) View.Clear();
            _entityId = id;
            _ownedId = CharacterInstanceId.Empty;
            _level = level;
            _infoReceiver?.Forget(this);
            Refresh();
        }

        /// <summary>덱의 보유 ID로 조회합니다. 비전투에서는 배치 전 기준 능력치를 표시합니다.</summary>
        public void SetOwnedCharacter(CharacterInstanceId id, int? level = null)
        {
            ValidateLevel(level);
            if (_ownedId != id || !_entityId.IsEmpty) View.Clear();
            _ownedId = id;
            _entityId = EntityId.Empty;
            _level = level;
            _infoReceiver?.Forget(this);
            Refresh();
        }

        public void SetLevel(int? level)
        {
            ValidateLevel(level);
            if (_level == level) return;
            _level = level;
            Refresh();
        }

        private static void ValidateLevel(int? level)
        {
            if (level.HasValue && level.Value < 1) throw new ArgumentOutOfRangeException(nameof(level));
        }

        public override void Open()
        {
            base.Open();
            Refresh();
        }

        public override void Hide()
        {
            _ownedStats?.Clear();
            _displayedStats = null;
            _infoReceiver?.Forget(this);
            base.Hide();
        }

        private CharacterData ReadCharacter()
        {
            if (!_ownedId.IsEmpty && _characters != null)
            {
                foreach (var character in _characters.Characters)
                    if (character.OwnedId == _ownedId) return character;
                return null;
            }
            return _characters != null && !_entityId.IsEmpty && _characters.TryGet(_entityId, out var data)
                ? data : null;
        }

        private OwnedCharacterRecord ReadOwned()
        {
            return _characters != null && !_ownedId.IsEmpty && _characters.TryGetOwned(_ownedId, out var owned)
                ? owned : null;
        }

        private void Update() => RefreshIfChanged();

        private void RefreshIfChanged()
        {
            // 패널 입력도 같은 변경 검사를 사용해, 바뀌지 않은 특성 배열·문자열을 포인터 입력마다 만들지 않습니다.
            var owned = ReadOwned();
            bool statsChanged = !_ownedId.IsEmpty && (_ownedStats?.Refresh() ?? false);
            if (statsChanged || _displayedDeployment != IsDeployment || !ReferenceEquals(_displayed, ReadCharacter()) ||
                !ReferenceEquals(_displayedOwned, owned)) Refresh();
        }

        private bool IsDeployment => _combat?.Current != null &&
            _combat.Current.CommandState.Turn.StagePhase == StagePhase.Deployment;

        public void Refresh()
        {
            UnitCardView view = View;
            DisplayRevision++;
            _displayedDeployment = IsDeployment;
            _displayed = ReadCharacter();
            _displayedOwned = ReadOwned();
            if (!_ownedId.IsEmpty) _ownedStats?.Refresh();
            if (_ownedStats?.IsFaulted == true) return;
            _displayedStats = _ownedStats?.Find(_ownedId);
            if (!_ownedId.IsEmpty && _displayedOwned == null) _ownedId = CharacterInstanceId.Empty;
            _hasCharacter = _displayed != null || _displayedOwned != null;
            if (!_hasCharacter)
            {
                _infoContent = null;
                view.Clear();
                return;
            }

            var character = _displayed;
            EntityKey key = character?.CharacterKey ?? _displayedOwned.CharacterKey;
            string fallbackName = null;
            Sprite image = null;
            if (_unitVisuals != null && _unitVisuals.TryGetUnitVisual(key, out var unitVisual))
            {
                fallbackName = unitVisual.DisplayName;
                image = unitVisual.InfoIcon;
            }
            int? characterLevel = character?.ToFacts().Experience.EnemyLevel;
            if (character?.OwnedId is CharacterInstanceId ownedId &&
                _characters.TryGetOwned(ownedId, out var ownedCharacter))
                characterLevel = ownedCharacter.Level;
            int? level = _level ?? _displayedStats?.Level ?? _displayedOwned?.Level ?? characterLevel;
            view.ShowCharacter(key, fallbackName, image, level);
            if (character != null)
            {
                view.ShowDeckDetails(level, character.Health.CurrentHealth, character.Health.MaxHealth, character.EffectivePhysicalDefense,
                    character.EffectiveAttackPower);
            }
            else if (_displayedStats != null)
            {
                var stats = _displayedStats.Stats;
                view.ShowDeckDetails(level, stats.CurrentHealth, stats.MaxHealth, stats.PhysicalDefense,
                    stats.AttackPower);
            }
            else view.ClearDetails();
            view.SetDead(character != null && character.Health.IsDead);
            view.SetDeployed(_displayedDeployment && character != null && character.Area == CharacterRuntimeArea.Field);
            _infoContent = CreateInfoContent();
        }

        /// <summary>같은 조회 캐시에서 상세 표시값만 만듭니다. 특성 문자열은 상세 갱신 때만 생성합니다.</summary>
        internal UnitInfoContent ReadInfoContent()
        {
            RefreshIfChanged();
            return _ownedStats?.IsFaulted == true ? null : _infoContent;
        }

        private UnitInfoContent CreateInfoContent()
        {
            if (!_hasCharacter || _ownedStats?.IsFaulted == true) return null;
            CharacterData character = _displayed;
            EntityKey key = character?.CharacterKey ?? _displayedOwned.CharacterKey;
            string displayName = null;
            Sprite portrait = null;
            if (_unitVisuals != null && _unitVisuals.TryGetUnitVisual(key, out var visual))
            {
                displayName = visual.DisplayName;
                portrait = visual.InfoIcon;
            }
            int? characterLevel = character?.ToFacts().Experience.EnemyLevel;
            if (character?.OwnedId is CharacterInstanceId id && _characters.TryGetOwned(id, out var owned))
                characterLevel = owned.Level;
            int? level = _level ?? _displayedStats?.Level ?? _displayedOwned?.Level ?? characterLevel;
            UnitCardTraitContent[] traits;
            if (character != null)
            {
                CombatStateData combat = _combat?.Current ?? throw new InvalidOperationException(
                    $"[UnitCardUI] 전투 개체 {character.EntityId}의 특성 설정을 읽을 전투 상태가 없습니다.");
                var facts = character.ToFacts();
                traits = UnitCardTraitContent.Create(_traitVisuals, facts.Stats.TraitStatuses.Span,
                    combat.TraitSettings.For(facts));
            }
            else
            {
                traits = UnitCardTraitContent.Create(_traitVisuals, _displayedStats);
            }
            var baseline = _displayedStats?.Stats;
            return new UnitInfoContent(
                (character?.Stats.Category?.Key ?? baseline?.CombatCategoryKey)?.ToString() ?? "-",
                View.ResolveDisplayName(key, displayName), portrait, level,
                character?.Health.CurrentHealth ?? baseline?.CurrentHealth,
                character?.EffectiveAttackPower ?? baseline?.AttackPower,
                character?.EffectivePhysicalDefense ?? baseline?.PhysicalDefense,
                character?.Stats.MoveRange ?? baseline?.MoveRange,
                character?.Stats.AttackRange ?? baseline?.AttackRange, traits);
        }
    }
}
