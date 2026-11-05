using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>튜토리얼이 임시로 연 카드 목록 한 인스턴스의 정리 의무만 관리합니다.</summary>
    internal sealed class TutorialViewCardVisibility
    {
        private readonly IUISystemService _ui;
        private bool _prepared;
        private DeckCardList _owned;
        private TutorialViewCardVisibilityObserver _observer;

        internal TutorialViewCardVisibility(IUISystemService ui)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
        }

        internal void Prepare()
        {
            if (_prepared)
                return;
            DeckButtonUI[] parents = UnityEngine.Object.FindObjectsByType<DeckButtonUI>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            DeckCardList[] lists = UnityEngine.Object.FindObjectsByType<DeckCardList>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (parents.Length != 1 || !parents[0].gameObject.activeInHierarchy)
                throw new InvalidOperationException("[TutorialViewCardVisibility] 활성 UIId.Deck 부모가 하나 필요합니다.");
            if (lists.Length != 1 || lists[0].ParentID != UIId.Deck)
                throw new InvalidOperationException("[TutorialViewCardVisibility] Deck의 UIId.UnitCard DeckCardList가 하나 필요합니다.");
            DeckCardList cards = lists[0];
            if (cards.gameObject.activeInHierarchy)
            {
                _prepared = true;
                return;
            }
            // 비활성 조상이 있으면 자식 열기만으로 표시할 수 없으므로 개폐 전에 설정 오류를 알립니다.
            if (cards.transform.parent != null && !cards.transform.parent.gameObject.activeInHierarchy)
                throw new InvalidOperationException("[TutorialViewCardVisibility] DeckCardList의 조상이 비활성 상태입니다.");
            _observer = cards.gameObject.AddComponent<TutorialViewCardVisibilityObserver>();
            _observer.Disabled += OnDisabled;
            _owned = cards;
            _prepared = true;
            try
            {
                _ui.OpenChildUI(UIId.Deck, UIId.UnitCard);
                if (!cards.gameObject.activeInHierarchy)
                    throw new InvalidOperationException("[TutorialViewCardVisibility] UIId.UnitCard 열기 뒤 DeckCardList가 비활성 상태입니다.");
            }
            catch
            {
                Release();
                throw;
            }
        }

        internal void Release()
        {
            bool close = _owned != null && _owned.gameObject.activeInHierarchy;
            RemoveObservation();
            _owned = null;
            if (close)
                _ui.CloseUI(UIId.UnitCard);
        }

        internal void Detach()
        {
            try
            {
                Release();
            }
            finally
            {
                _prepared = false;
            }
        }

        private void OnDisabled()
        {
            // 같은 프레임에 외부에서 다시 열어도 이미 정리 의무를 포기한 상태입니다.
            _owned = null;
            RemoveObservation();
        }

        private void RemoveObservation()
        {
            if (_observer == null)
                return;
            _observer.Disabled -= OnDisabled;
            if (Application.isPlaying)
                UnityEngine.Object.Destroy(_observer);
            else
                UnityEngine.Object.DestroyImmediate(_observer);
            _observer = null;
        }
    }
}
