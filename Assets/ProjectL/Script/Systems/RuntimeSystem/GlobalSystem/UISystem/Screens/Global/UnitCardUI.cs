using System;
using UnityEngine;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.DataBaseSystem;
using ProjectL.Systems.DataBaseSystem.Queries.ViewDataBase.ViewTable;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Roster;
using ProjectL.Systems.ServiceKeys;

namespace ProjectL.Systems
{
    /// <summary>
    /// 덱 정보 카드의 데이터 조회입니다. Buffer·DB·보유 카드에서 값을 읽어 같은 오브젝트의 <see cref="UnitCardView"/>에 넘깁니다.
    /// 표시(직렬화 참조·문구·패널)는 UnitCardView가 맡습니다(손패 카드화 설계 C1·C4).
    /// </summary>
    [RequireComponent(typeof(UnitCardView))]
    public class UnitCardUI : UIBase
    {
        private UnitCardView _view;
        private ICharacterBufferReadSink _characters;
        private IUnitVisualTableQuery _unitVisuals;
        private ITraitVisualTableQuery _traitVisuals;
        private EntityId _entityId;
        private CharacterInstanceId _ownedId;
        private OwnedCharacterRecord _displayedOwned;
        private bool _hasCharacter;
        private CharacterData _displayed;
        private int? _level;
        private bool _subscribed;

        public override UIId ID => UIId.UnitCard;
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

                if (!_subscribed)
                {
                    // 패널을 열기 직전에 최신 값으로 다시 그린다(분리 전 동작 유지).
                    _view.PanelOpening += Refresh;
                    _subscribed = true;
                }

                return _view;
            }
        }

        private void OnEnable() => Refresh();

        private void OnDestroy()
        {
            if (_subscribed && _view != null)
                _view.PanelOpening -= Refresh;
        }

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
            _characters = resolver.GetService(BufferSystemKey.Buffer).CharacterReadSink;
            Refresh();
        }

        public override void GetUIData(IServiceModule query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
            _unitVisuals = (query as IHasViewQuery)?.ViewQuery.UnitVisualTableQuery;
            _traitVisuals = (query as IHasViewQuery)?.ViewQuery.TraitVisualTableQuery;
            Refresh();
        }

        /// <summary>전투 EntityId로 Buffer를 조회합니다. Level 데이터가 없으면 '-'로 표시합니다.</summary>
        public void SetCharacter(EntityId id, int? level = null)
        {
            ValidateLevel(level);
            _entityId = id;
            _ownedId = CharacterInstanceId.Empty;
            _level = level;
            ClosePanels();
            Refresh();
        }

        /// <summary>덱의 보유 ID로 Buffer를 조회합니다. 전투 조회값이 없으면 능력치를 비웁니다.</summary>
        public void SetOwnedCharacter(CharacterInstanceId id, int? level = null)
        {
            ValidateLevel(level);
            _ownedId = id;
            _entityId = EntityId.Empty;
            _level = level;
            ClosePanels();
            Refresh();
        }

        public void SetLevel(int? level)
        {
            ValidateLevel(level);
            _level = level;
            View.SetLevel(_level);
        }

        private static void ValidateLevel(int? level)
        {
            if (level.HasValue && level.Value < 1) throw new ArgumentOutOfRangeException(nameof(level));
        }

        public override void Open()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        public override void Hide()
        {
            ClosePanels();
            gameObject.SetActive(false);
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

        private void Update()
        {
            // Buffer는 불변 조회값을 교체한다. 변경된 경우에만 텍스트와 이미지를 갱신한다.
            var owned = ReadOwned();
            if (!ReferenceEquals(_displayed, ReadCharacter()) ||
                !ReferenceEquals(_displayedOwned, owned)) Refresh();
        }

        public void Refresh()
        {
            UnitCardView view = View;
            _displayed = ReadCharacter();
            _displayedOwned = ReadOwned();
            _hasCharacter = _displayed != null || _displayedOwned != null;
            if (!_hasCharacter)
            {
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
            view.ShowCharacter(key, fallbackName, image, _level);
            if (character != null)
            {
                view.ShowDetails(character);
            }
            else view.ClearDetails();
            SetPanelContent(key);
        }

        private void SetPanelContent(EntityKey key)
        {
            string names = string.Empty;
            string descriptions = string.Empty;
            Sprite icon = null;
            if (_displayed != null && _traitVisuals != null)
            {
                foreach (var trait in _displayed.Stats.TraitStatuses.Span)
                {
                    if (!_traitVisuals.TryGetTraitVisual(trait.Key, out var traitVisual)) continue;
                    // 현재 패널은 아이콘 하나를 사용한다. 여러 특성은 이름과 설명을 순서대로 표시한다.
                    if (names.Length == 0) icon = traitVisual.Icon;
                    names += (names.Length == 0 ? string.Empty : "\n") + traitVisual.DisplayName;
                    string description = traitVisual.Description.Replace("\\n", "\n");
                    descriptions += (descriptions.Length == 0 ? string.Empty : "\n\n") + description;
                }
            }
            View.ShowPanelContent(key, icon, names, descriptions);
        }

        public void HoverSynergyEnter() => View.HoverSynergyEnter();
        public void HoverSynergyExit() => View.HoverSynergyExit();
        public void HoverFunctionEnter() => View.HoverFunctionEnter();
        public void HoverFunctionExit() => View.HoverFunctionExit();
        public void ToggleSynergyPanel() => View.ToggleSynergyPanel();
        public void ToggleFunctionPanel() => View.ToggleFunctionPanel();

        public void ClosePanels() => View.ClosePanels();
    }
}
