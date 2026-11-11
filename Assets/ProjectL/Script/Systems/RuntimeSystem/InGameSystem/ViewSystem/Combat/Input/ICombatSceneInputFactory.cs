using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;
using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Assist;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input
{
    /// <summary>
    /// 연결 입력 묶음과 서비스 수명 입력 사실 읽기의 생성 계약입니다. 조정자는 생성자로 받은 이 계약만 부릅니다.
    /// 운영 구현은 Factory 쪽의 <c>CombatSceneFactory</c> 하나입니다.
    /// </summary>
    internal interface ICombatSceneInputFactory
    {
        /// <summary>서비스 바인딩마다 입력 사실 읽기를 조립합니다.</summary>
        CombatInputFactsReader CreateFactsReader(ICombatStateBufferReadSink state, ICombatViewBufferReadSink inputRead,
            CombatInputConnection inputConnection, CombatResultPlayback playback, CombatViewProgress progress,
            CombatBlockedInputReport blockedInput, Func<CombatViewConnection> readConnection,
            Func<bool> isInputReady, Func<bool> hasPendingCommand, Func<bool> isModalOpen);

        /// <summary>씬 연결 한 번의 입력 객체 열둘을 만들어 연결 입력 묶음(<see cref="CombatSceneInput"/>)으로 돌려줍니다. 첫 그리기 성공 뒤에 부릅니다.</summary>
        /// <param name="display">이 연결의 표시입니다(연결 수명).</param>
        /// <param name="sources">서비스 수명 재료입니다(View 3라운드 W12-1).</param>
        CombatSceneInput CreateSceneInput(ICombatBattleDisplay display, CombatSceneInputSources sources);
    }
}
