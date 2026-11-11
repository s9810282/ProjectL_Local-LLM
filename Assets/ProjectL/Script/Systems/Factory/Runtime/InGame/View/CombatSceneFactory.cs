using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input;
using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Assist;

namespace ProjectL.Systems.Factory.Runtime.InGame.View
{
    /// <summary>
    /// 전투 씬 표시·연결 입력 묶음의 생성 계약 둘을 함께 구현하는 운영 구현입니다(View 2라운드 W6-6, 설계 P16).
    /// 상태가 없고 <see cref="Instance"/> 하나만 쓰며, 정적 Factory 둘(<see cref="CombatSceneDisplayFactory"/>·<see cref="CombatSceneInputFactory"/>)에 위임합니다.
    /// </summary>
    /// <remarks>조정자는 이 생성 계약으로 받고, 운영 조립(<see cref="InGameViewSystemFactory"/>)이 <see cref="Instance"/>를 넘깁니다. 특성 확인 표시는 이 구현을 거치지 않고 필요한 Factory 함수를 직접 부릅니다.</remarks>
    internal sealed class CombatSceneFactory : ICombatSceneFactory, ICombatSceneInputFactory
    {
        /// <summary>하나뿐인 구현입니다(상태 없음).</summary>
        internal static readonly CombatSceneFactory Instance = new();

        private CombatSceneFactory()
        {
        }

        public StageViewPropPresenter CreateBackdrop(CombatSceneRoot root, CombatDisplayTables tables) =>
            CombatSceneDisplayFactory.CreateBackdrop(root, tables);

        public CombatSceneDisplay CreateDisplay(CombatSceneRoot root, CombatDisplayTables tables, CombatDisplayServices services,
            StageViewPropPresenter backdrop) =>
            CombatSceneDisplayFactory.Create(root, tables, services, backdrop);

        public CombatSceneInput CreateSceneInput(ICombatBattleDisplay display, CombatSceneInputSources sources) =>
            CombatSceneInputFactory.CreateSceneInput(display, sources);

        public CombatInputFactsReader CreateFactsReader(ICombatStateBufferReadSink state, ICombatViewBufferReadSink inputRead,
            CombatInputConnection inputConnection, CombatResultPlayback playback, CombatViewProgress progress,
            CombatBlockedInputReport blockedInput, Func<CombatViewConnection> readConnection,
            Func<bool> isInputReady, Func<bool> hasPendingCommand, Func<bool> isModalOpen) =>
            CombatSceneInputFactory.CreateFactsReader(state, inputRead, inputConnection, playback, progress,
                blockedInput, readConnection, isInputReady, hasPendingCommand, isModalOpen);
    }
}
