using System;
using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global
{
    /// <summary>Combat 씬의 덱·필드 정보창 UI 루트입니다. 카드의 자동 개폐는 전투 배치 표시가 맡습니다.</summary>
    [UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "ProjectL.Systems", sourceAssembly: "ProjectL.Systems", sourceClassName: "DeckButtonUI")]
    public sealed class DeckButtonUI : UIBase
    {
        [SerializeField] private DeckCardList cards;

        public DeckButtonUI() => Policy = UIOpenPolicy.Parallel;

        public override UIId ID => UIId.Deck;
        public override UIId ParentID => UIId.None;

        public override void ExportUIService(IUIRegistry registry)
        {
            Validate();
            base.ExportUIService(registry);
        }

        public override void BindUIService(ISystemServiceStoreReader resolver)
        {
            if (resolver == null) throw new ArgumentNullException(nameof(resolver));
        }

        public override void GetUIData(IServiceModule query)
        {
            if (query == null) throw new ArgumentNullException(nameof(query));
        }

        private void Validate()
        {
            if (cards == null)
                throw new InvalidOperationException($"[DeckButtonUI] UIId.{ID}, '{name}': cards 참조가 필요합니다.");
            if (cards.gameObject == gameObject || !cards.transform.IsChildOf(transform))
                throw new InvalidOperationException($"[DeckButtonUI] UIId.{ID}, '{name}': cards는 덱 루트의 자식이어야 합니다.");
        }
    }
}
