using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Global;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Entry
{
    /// <summary>최초 배치 안내→카드와 이후 국면별 카드 개폐를 맡습니다. 맵 연출·규칙·카드 자세는 소유하지 않습니다.</summary>
    internal sealed class CombatDeploymentPresentation
    {
        private enum Phase
        {
            Inactive,
            Waiting,
            Announcement,
            Cards,
            Ready
        }
        private readonly IUISystemService _ui;
        private readonly TurnDisplayUI _notice;
        private readonly DeckCardList _cards;
        private readonly IAudioSystemService _audio;
        private Phase _phase;
        private bool _isDeployment;

        internal CombatDeploymentPresentation(IUISystemService ui, TurnDisplayUI notice, DeckCardList cards, IAudioSystemService audio)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _notice = notice != null ? notice : throw new ArgumentNullException(nameof(notice));
            _cards = cards != null ? cards : throw new ArgumentNullException(nameof(cards));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
        }

        internal bool IsPending => _phase == Phase.Waiting || _phase == Phase.Announcement || _phase == Phase.Cards;

        internal void Prepare()
        {
            Cancel();
            _phase = Phase.Waiting;
            _isDeployment = false;
            _ui.CloseUI(UIId.UnitCard);
        }

        /// <summary>표시한 국면만 받습니다. 최초 준비 중에는 카드를 열지 않습니다.</summary>
        internal void Render(StagePhase stage, CombatFlowPhase flow)
        {
            bool deployment = stage == StagePhase.Deployment && flow != CombatFlowPhase.Finished;
            bool changed = deployment != _isDeployment;
            _isDeployment = deployment;
            if (_phase != Phase.Ready || !changed) return;
            if (deployment) _ui.OpenChildUI(UIId.Deck, UIId.UnitCard);
            else _ui.CloseUI(UIId.UnitCard);
        }

        internal void Tick(float deltaTime)
        {
            if (_phase == Phase.Waiting)
            {
                if (!_isDeployment)
                {
                    _phase = Phase.Ready;
                    return;
                }
                _phase = Phase.Announcement;
                _notice.ShowDeployment();
                return;
            }
            if (_phase == Phase.Announcement)
            {
                if (_notice.IsShowing) return;
                // 외부 Hide/씬 정리로 숨긴 안내를 정상 완료로 간주해 덱을 뒤늦게 열지 않습니다.
                if (!_notice.HasCompleted)
                {
                    Cancel();
                    return;
                }
                _phase = Phase.Cards;
                _cards.PrepareEntrance();
                if (_phase != Phase.Cards)
                {
                    _cards.CancelEntrance();
                    return;
                }
                _ui.OpenChildUI(UIId.Deck, UIId.UnitCard);
                if (_phase == Phase.Cards) TickCards(0f);
                return;
            }
            if (_phase == Phase.Cards) TickCards(deltaTime);
        }

        private void TickCards(float deltaTime)
        {
            int startedCards = _cards.TickEntrance(deltaTime);
            for (int i = 0; i < startedCards && _phase == Phase.Cards; i++)
                _audio.Play(AmbgmEvent.CardShuffle);
            if (_phase == Phase.Cards && !_cards.IsEntrancePlaying) _phase = Phase.Ready;
        }

        internal void Cancel()
        {
            if (_phase == Phase.Inactive) return;
            bool dismissNotice = _phase == Phase.Announcement;
            _phase = Phase.Inactive;
            _isDeployment = false;
            try
            {
                if (dismissNotice && _notice != null) _notice.Dismiss();
            }
            finally
            {
                if (_cards != null) _ui.CloseUI(UIId.UnitCard);
            }
        }
    }
}
