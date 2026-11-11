using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Screens.Stage;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Turn
{
    /// <summary>턴 시작 사건 또는 결과 끝 사실로 같은 턴의 안내를 한 번만 엽니다.</summary>
    internal sealed class CombatTurnDisplayPresenter
    {
        private readonly TurnDisplayUI _view;
        private readonly IAudioSystemService _audio;
        private long _shownTurnSequence = -1;

        internal CombatTurnDisplayPresenter(TurnDisplayUI view, IAudioSystemService audio)
        {
            _view = view != null ? view : throw new ArgumentNullException(nameof(view));
            _audio = audio ?? throw new ArgumentNullException(nameof(audio));
        }

        /// <summary>적 턴 시작 사건을 재생할 때 안내를 먼저 엽니다. 결과 끝 그리기는 같은 순번을 다시 열지 않습니다.</summary>
        internal void ShowEnemyTurnStart(CombatTurnEvent turnEvent)
        {
            if (turnEvent == null) throw new ArgumentNullException(nameof(turnEvent));
            if (turnEvent.Kind == CombatEventKind.ControllerTurnStarted
                && turnEvent.Final.ControllerPhase == CombatControllerPhase.Enemy)
                Render(turnEvent.Final, CombatFlowPhase.Automatic);
        }

        internal void Render(CombatTurnFacts turn, CombatFlowPhase phase)
        {
            if (turn == null) throw new ArgumentNullException(nameof(turn));
            if (turn.StagePhase != StagePhase.Combat || phase == CombatFlowPhase.Finished)
            {
                Clear();
                return;
            }
            // Starting은 턴 시작 처리 대기입니다. Enemy는 시작 사건에서, 나머지는 결과 끝 Player/Enemy에서 안내합니다.
            if (turn.ControllerPhase != CombatControllerPhase.Player && turn.ControllerPhase != CombatControllerPhase.Enemy)
                return;
            if (_shownTurnSequence == turn.ControllerTurnSequence)
                return;

            bool isPlayer = turn.ControllerPhase == CombatControllerPhase.Player;
            if (isPlayer) _view.ShowPlayerTurn();
            else _view.ShowEnemyTurn();
            _shownTurnSequence = turn.ControllerTurnSequence;
            _audio.Play(UiEvent.ChangeTurn, (int)(isPlayer ? ChangeTurnPreset.Player : ChangeTurnPreset.Enemy));
        }

        internal void Clear()
        {
            _shownTurnSequence = -1;
            // 씬 파괴 뒤 반복 해제는 정상 경로입니다.
            if (_view != null) _view.Dismiss();
        }
    }
}
