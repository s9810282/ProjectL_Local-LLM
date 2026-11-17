using System;
using System.Collections.Generic;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CharacterSystem;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Deck;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Snapshots;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.StageMap.Deck;
using ProjectL.Systems.ServiceKeys.Runtime;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    /// <summary>Combat 덱 카드가 공유하는 상세창의 선택·고정·개폐·포커스 수명입니다.</summary>
    [DisallowMultipleComponent]
    public sealed class CombatCardInfoUI : UIBase, IUnitCardInfoReceiver, IUIInputReceiver
    {
        private const int CloseSlot = 0;
        private const int PreviousSlot = 1;
        private const int NextSlot = 2;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // 이 기능의 계측 정의입니다. 일반 배포 빌드에서는 선언과 호출을 함께 제외합니다.
        private static readonly Unity.Profiling.ProfilerMarker _profileUICardInfoToggle = new("Combat.Action.UI.CardInfo.Toggle");
        private static readonly Unity.Profiling.ProfilerMarker _profileUICardInfoClose = new("Combat.Action.UI.CardInfo.Close");
        private static readonly Unity.Profiling.ProfilerMarker _profileUICardInfoClick = new("Combat.Action.UI.CardInfo.Click");
#endif

        [SerializeField] private Button closeButton;
        [SerializeField] private Button changeButton;
        [SerializeField] private Button leftButton;
        [SerializeField] private Button rightButton;
        [SerializeField] private DeckSlotInfoStats stats = new();
        [SerializeField] private DeckSlotInfoExperience experience = new();
        [SerializeField] private DeckSlotInfoAbilities abilities = new();
        [SerializeField] private DeckSlotInfoRanges ranges = new();
        private IRunDeckService _deck;
        private ICharacterBufferReadSink _characters;
        private DeckEditorSnapshot _deckSnapshot;
        private IReadOnlyList<UnitCardUI> _cards;
        private UnitCardUI _source;
        private int _revision;
        private bool _prepared;
        private bool _ownsFocus;
        private GameObject _previousSelection;

        public CombatCardInfoUI() => Policy = UIOpenPolicy.Exclusive;
        public override UIId ID => UIId.CombatCardInfo;
        public override UIId ParentID => UIId.UnitCard;

        internal void Validate()
        {
            DeckSlotInfoReferences.RequireChild(closeButton, transform, nameof(closeButton));
            DeckSlotInfoReferences.RequireChild(changeButton, transform, nameof(changeButton));
            DeckSlotInfoReferences.RequireChild(leftButton, transform, nameof(leftButton));
            DeckSlotInfoReferences.RequireChild(rightButton, transform, nameof(rightButton));
            if (closeButton == changeButton || closeButton.targetGraphic == null ||
                !closeButton.targetGraphic.raycastTarget)
                throw DeckSlotInfoReferences.Error(transform, "closeButton에 독립적인 Raycast Target Graphic이 필요합니다.");
            if (leftButton == rightButton || leftButton == closeButton || leftButton == changeButton ||
                rightButton == closeButton || rightButton == changeButton)
                throw DeckSlotInfoReferences.Error(transform, "leftButton과 rightButton은 각각 별도의 버튼이어야 합니다.");
            if (leftButton.targetGraphic == null || !leftButton.targetGraphic.raycastTarget ||
                rightButton.targetGraphic == null || !rightButton.targetGraphic.raycastTarget)
                throw DeckSlotInfoReferences.Error(transform, "leftButton과 rightButton에 Raycast Target Graphic이 필요합니다.");
            if (stats == null || experience == null || abilities == null || ranges == null)
                throw DeckSlotInfoReferences.Error(transform, "stats·experience·abilities·ranges가 필요합니다.");
            stats.Validate(transform);
            experience.Validate(transform);
            abilities.Validate(transform);
            ranges.Validate(transform);
        }

        internal void BindCards(IReadOnlyList<UnitCardUI> cards) =>
            _cards = cards ?? throw new ArgumentNullException(nameof(cards));

        public override void ExportUIService(IUIRegistry registry)
        {
            Validate();
            base.ExportUIService(registry);
        }

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            base.BindUIService(resolver);
            var deck = resolver.GetService(SystemGroupsService.InGame).CoreGroup?.Deck;
            var characters = resolver.GetService(BufferSystemKey.Buffer).CharacterReadSink;
            if (deck == null || characters == null)
                throw DeckSlotInfoReferences.Error(transform, "기준 공격 범위를 읽을 CoreGroup.Deck과 CharacterReadSink가 필요합니다.");
            _deck = deck;
            _characters = characters;
            _deckSnapshot = null;
        }

        private void Prepare()
        {
            if (_prepared) return;
            if (_cards == null)
                throw DeckSlotInfoReferences.Error(transform, "상세 선택 전에 DeckCardList의 카드 목록을 바인딩해야 합니다.");
            Validate();
            UIInputBinding.Attach(closeButton, this, CloseSlot);
            UIInputBinding.Attach(leftButton, this, PreviousSlot);
            UIInputBinding.Attach(rightButton, this, NextSlot);
            abilities.BindInput();
            changeButton.gameObject.SetActive(false);
            _prepared = true;
        }

        void IUnitCardInfoReceiver.Toggle(UnitCardUI source)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUICardInfoToggleSample = _profileUICardInfoToggle.Auto();
#endif

            if (source == null) throw new ArgumentNullException(nameof(source));
            if (_source == source)
            {
                CloseInfo();
                return;
            }
            IUISystemService ui = RequireUI();
            if (TrySelect(source)) ui.OpenChildUI(ParentID, ID);
        }

        void IUnitCardInfoReceiver.Forget(UnitCardUI source)
        {
            if (_source == source) CloseInfo();
        }

        private IUISystemService RequireUI() => UISystemService ??
            throw new InvalidOperationException($"[CombatCardInfoUI] UIId.{ID}, '{name}': UISystem 바인딩이 필요합니다.");

        private bool TrySelect(UnitCardUI source)
        {
            if (_deck == null || _characters == null)
                throw DeckSlotInfoReferences.Error(transform, "기준 공격 범위 조회 전에 덱과 캐릭터 Buffer를 바인딩해야 합니다.");
            Prepare();
            // 빈 카드와 이미 숨겨진 카드에서 지난 입력이 도착하는 것은 정상적인 수명 경계입니다.
            if (!source.isActiveAndEnabled) return false;
            UnitInfoContent content = source.ReadInfoContent();
            if (content == null) return false;
            if (_deckSnapshot?.RosterRevision != _characters.RosterRevision) _deckSnapshot = null;
            Render(content, source.OwnedCharacterId);
            abilities.ResetForSelection();
            _source = source;
            _revision = source.DisplayRevision;
            return true;
        }

        private void SelectAdjacentCategory(int direction)
        {
            if (_source == null || !_source.isActiveAndEnabled) return;
            int current = DeckEditSlotLayout.IndexOf(ReadCategory(_source));
            if (current < 0) return;

            // 비어 있는 병종을 건너뛰고, 같은 병종이 여럿이면 덱 순서상 첫 표시 가능한 카드를 선택합니다.
            int count = DeckEditSlotLayout.Count;
            for (int step = 1; step < count; step++)
            {
                CategoryKey category = DeckEditSlotLayout.CategoryAt((current + count + direction * step) % count);
                for (int i = 0; i < _cards.Count; i++)
                {
                    UnitCardUI card = _cards[i];
                    if (card != _source && card.isActiveAndEnabled && card.View.HasContent &&
                        ReadCategory(card) == category && TrySelect(card)) return;
                }
            }
        }

        private CategoryKey ReadCategory(UnitCardUI card)
        {
            CategoryKey category = card.DeckCategory;
            if (category != CategoryKey.None) return category;

            // 전투 개체가 없어도 보유 능력치가 도착한 카드는 편성 병종으로 이동할 수 있습니다.
            if (card.OwnedCharacterId.IsEmpty || _deckSnapshot?.RosterRevision != _characters.RosterRevision)
                return CategoryKey.None;
            return FindOwnedStats(card.OwnedCharacterId).Stats.CategoryKey;
        }

        private bool RefreshDeckSnapshot()
        {
            if (_deckSnapshot?.RosterRevision == _characters.RosterRevision) return false;

            // StageSelect와 동일한 보유 조회를 씁니다. 전투 중에도 편집만 제한되고 기준 능력치는 읽을 수 있습니다.
            // 카드 입력 밖에서 읽으며 초기 준비 중이거나 원본이 바뀌었으면 다음 갱신에서 다시 조회합니다.
            DeckEditorSnapshot snapshot = _deck.Read().Snapshot;
            bool changed = !ReferenceEquals(_deckSnapshot, snapshot);
            _deckSnapshot = snapshot;
            return changed;
        }

        private OwnedCharacterStatsSnapshot FindOwnedStats(CharacterInstanceId ownedId)
        {
            if (_deckSnapshot == null) return null;
            for (int i = 0; i < _deckSnapshot.Stats.Count; i++)
            {
                var item = _deckSnapshot.Stats[i];
                if (item.OwnedId == ownedId) return item;
            }
            throw DeckSlotInfoReferences.Error(transform, $"덱 조회에 보유 유닛 '{ownedId}'의 능력치가 없습니다.");
        }

        private void Render(UnitInfoContent content, CharacterInstanceId ownedId)
        {
            abilities.ValidateContent(content.Abilities);
            OwnedCharacterStatsSnapshot owned = FindOwnedStats(ownedId);
            stats.Render(content);
            experience.Render(owned?.Experience);
            abilities.Render(content.Abilities, owned?.TraitRanks);
            ranges.Render(content.MoveMethod, content.MoveDistance, owned?.AttackPattern);
        }

        public override void Open()
        {
            Prepare();
            if (_source == null)
                throw new InvalidOperationException($"[CombatCardInfoUI] UIId.{ID}, '{name}': Open 전에 표시 대상이 필요합니다.");
            base.Open();
            if (!_ownsFocus && EventSystem.current != null)
            {
                _previousSelection = EventSystem.current.currentSelectedGameObject;
                _ownsFocus = true;
                EventSystem.current.SetSelectedGameObject(closeButton.gameObject);
            }
        }

        private void Update()
        {
            if (_source == null || !_source.isActiveAndEnabled)
            {
                CloseInfo();
                return;
            }
            bool deckChanged = RefreshDeckSnapshot();
            if (!deckChanged && _revision == _source.DisplayRevision) return;
            UnitInfoContent content = _source.ReadInfoContent();
            if (content == null) CloseInfo();
            else
            {
                Render(content, _source.OwnedCharacterId);
                _revision = _source.DisplayRevision;
            }
        }

        private void CloseInfo()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUICardInfoCloseSample = _profileUICardInfoClose.Auto();
#endif

            if (ReferenceEquals(_source, null)) return;
            IUISystemService ui = RequireUI();
            _source = null;
            ui.CloseUI(ID);
        }

        void IUIInputReceiver.Click(int slot)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            using var profileUICardInfoClickSample = _profileUICardInfoClick.Auto();
#endif

            // 닫히는 사이에 도착한 입력은 무시합니다.
            if (!isActiveAndEnabled) return;
            switch (slot)
            {
                case CloseSlot: CloseInfo(); break;
                case PreviousSlot: SelectAdjacentCategory(-1); break;
                case NextSlot: SelectAdjacentCategory(1); break;
            }
        }
        void IUIInputReceiver.HoverEnter(int slot) { }
        void IUIInputReceiver.HoverExit(int slot) { }

        protected override void OnUIHidden()
        {
            _source = null;
            _revision = 0;
            _deckSnapshot = null;
            EventSystem events = EventSystem.current;
            if (_ownsFocus && events != null)
            {
                if (_previousSelection != null && _previousSelection.activeInHierarchy)
                    events.SetSelectedGameObject(_previousSelection);
                else if (events.currentSelectedGameObject != null &&
                    events.currentSelectedGameObject.transform.IsChildOf(transform))
                    events.SetSelectedGameObject(null);
            }
            _ownsFocus = false;
            _previousSelection = null;
        }
    }
}
