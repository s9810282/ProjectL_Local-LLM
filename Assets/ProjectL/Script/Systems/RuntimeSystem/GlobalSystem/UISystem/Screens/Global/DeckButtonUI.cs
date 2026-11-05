using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    /// <summary>Combat 씬의 덱 루트입니다. 버튼과 자식 패널을 전환하며 카드 목록·배치 선택은 DeckCardList가 맡습니다.</summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Systems", sourceAssembly: "ProjectL.Systems", sourceClassName: "DeckButtonUI")]
    public sealed class DeckButtonUI : UIBase, IUIInputReceiver
    {
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private DeckCardList cards;

        public DeckButtonUI() => Policy = UIOpenPolicy.Parallel;

        public override UIId ID => UIId.Deck;
        public override UIId ParentID => UIId.None;

        private void Awake()
        {
            Validate();
            UIInputBinding.Attach(openButton, this, 0);
            UIInputBinding.Attach(closeButton, this, 1);
            cards.BindOpenButton(openButton);
        }

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
        }

        public override void GetUIData(IServiceModule query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
        }

        public void OpenPanel()
        {
            Validate();
            RequireUISystem().OpenChildUI(ID, cards.ID);
        }

        public void ClosePanel()
        {
            Validate();
            RequireUISystem().CloseChildUI(ID);
        }

        private IUISystemService RequireUISystem() => UISystemService ??
            throw new InvalidOperationException($"[DeckButtonUI] UIId.{ID}, '{name}': UISystem 연결이 필요합니다.");

        void IUIInputReceiver.Click(int slot)
        {
            if (!isActiveAndEnabled) return;
            if (slot == 0) OpenPanel();
            else if (slot == 1) ClosePanel();
        }

        void IUIInputReceiver.HoverEnter(int slot)
        {
        }
        void IUIInputReceiver.HoverExit(int slot)
        {
        }

        private void Validate()
        {
            if (openButton == null)
                throw new InvalidOperationException($"[DeckButtonUI] UIId.{ID}, '{name}': openButton 참조가 필요합니다.");
            if (closeButton == null)
                throw new InvalidOperationException($"[DeckButtonUI] UIId.{ID}, '{name}': closeButton 참조가 필요합니다.");
            if (cards == null)
                throw new InvalidOperationException($"[DeckButtonUI] UIId.{ID}, '{name}': cards 참조가 필요합니다.");
            if (openButton.gameObject == gameObject || cards.gameObject == gameObject ||
                openButton.transform.parent != cards.transform.parent || !cards.transform.IsChildOf(transform) ||
                !closeButton.transform.IsChildOf(cards.transform))
                throw new InvalidOperationException($"[DeckButtonUI] UIId.{ID}, '{name}': openButton과 cards는 덱 루트 아래 형제이며 closeButton은 cards의 자식이어야 합니다.");
        }
    }
}
