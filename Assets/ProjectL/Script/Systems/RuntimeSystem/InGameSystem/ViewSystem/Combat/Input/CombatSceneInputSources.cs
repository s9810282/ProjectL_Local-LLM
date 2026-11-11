using System;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Combat;
using ProjectL.Systems.RuntimeSystem.BufferSystems.CombatView;
using ProjectL.Systems.RuntimeSystem.ErrorHandling;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.AudioSystem;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input
{
    /// <summary>
    /// 연결 입력 묶음(<see cref="CombatSceneInput"/>)을 만드는 재료입니다(View 3라운드 W12-1). 씬 연결마다 바뀌지 않는 서비스 수명 재료만 갖고,
    /// 연결 수명인 표시는 따로 넘깁니다(계획 TP-24).
    /// </summary>
    /// <remarks>
    /// 조정자가 첫 입력 생성 때 한 번 만듭니다. 표시 표는 <c>InitData</c>가, 나머지는 서비스를 받을 때(조정자의 <c>Bind</c>) 생겨서 그 둘이 모두 끝난 뒤이기 때문입니다.
    /// 두 입구가 다시 불리면 조정자가 이 묶음을 버리고 다음 입력 생성 때 다시 만듭니다.
    /// 입력 객체에 의존이 하나 늘면 이 묶음, 조정자의 생성 자리 하나, 입력 Factory만 고칩니다. 생성 계약·어댑터의 서명은 그대로입니다.
    /// 모든 재료는 필수입니다.
    /// </remarks>
    internal sealed class CombatSceneInputSources
    {
        /// <param name="tables">환경 설명에 쓰는 View DB 표입니다.</param>
        /// <param name="readPathFacts">경로 입력이 판단할 때마다 새로 읽는 사실 공급자입니다(선행 미처리 명령 포함).</param>
        /// <param name="readInputFacts">Router·덱·아이템 입구·배치 진입이 판단할 때마다 읽는 사실 공급자입니다(인자 = 선행 미처리 명령까지 읽을지).</param>
        /// <param name="canReceiveUiInput">덱·가방 UI 입구의 수신 조건입니다.</param>
        /// <param name="reportBlockedInput">Router가 실제로 막은 입력 시도 하나를 알리는 입구입니다.</param>
        /// <param name="audio">효과음 서비스입니다(표시 서비스 묶음의 것).</param>
        /// <param name="itemStock">가방 아이템 드래그가 읽는 아이템 키별 재고 수입니다(가방 설계 P4).</param>
        /// <param name="text">텍스트 서비스입니다(거절 안내·환경 호버).</param>
        /// <param name="confirmation">바인딩 때 확인한 배치 확인창 표시 계약입니다.</param>
        /// <param name="isInputReady">배치 진입 판단에 쓰는 조정자의 판정식(씬 연결 활성 ∧ 입력 슬롯 연결)입니다.</param>
        /// <param name="afterConfirmedSubmit">배치 진입·턴 종료(확인창 경로)를 제출한 뒤 행동 버튼을 바로 맞추는 입구입니다(입력 분배 <c>CombatSceneInputDriver.SyncActionButtons</c>, 계획 TP-21).</param>
        internal CombatSceneInputSources(
            CombatDisplayTables tables,
            ICombatStateBufferReadSink state,
            ICharacterBufferReadSink characters,
            ICombatViewBufferReadSink inputRead,
            ICombatViewBufferWriteSink inputWrite,
            Func<CombatInputFacts> readPathFacts,
            Func<bool, CombatInputFacts> readInputFacts,
            Func<bool> canReceiveUiInput,
            Action reportBlockedInput,
            RuntimeFaultBoundary faults,
            IAudioSystemService audio,
            Func<int, int> itemStock,
            IUITextPresentation text,
            IUIConfirmationPresentation confirmation,
            Func<bool> isInputReady,
            Action afterConfirmedSubmit)
        {
            Tables = tables ?? throw new ArgumentNullException(nameof(tables));
            State = state ?? throw new ArgumentNullException(nameof(state));
            Characters = characters ?? throw new ArgumentNullException(nameof(characters));
            InputRead = inputRead ?? throw new ArgumentNullException(nameof(inputRead));
            InputWrite = inputWrite ?? throw new ArgumentNullException(nameof(inputWrite));
            ReadPathFacts = readPathFacts ?? throw new ArgumentNullException(nameof(readPathFacts));
            ReadInputFacts = readInputFacts ?? throw new ArgumentNullException(nameof(readInputFacts));
            CanReceiveUiInput = canReceiveUiInput ?? throw new ArgumentNullException(nameof(canReceiveUiInput));
            ReportBlockedInput = reportBlockedInput ?? throw new ArgumentNullException(nameof(reportBlockedInput));
            Faults = faults ?? throw new ArgumentNullException(nameof(faults));
            Audio = audio ?? throw new ArgumentNullException(nameof(audio));
            ItemStock = itemStock ?? throw new ArgumentNullException(nameof(itemStock));
            Text = text ?? throw new ArgumentNullException(nameof(text));
            Confirmation = confirmation ?? throw new ArgumentNullException(nameof(confirmation));
            IsInputReady = isInputReady ?? throw new ArgumentNullException(nameof(isInputReady));
            AfterConfirmedSubmit = afterConfirmedSubmit ?? throw new ArgumentNullException(nameof(afterConfirmedSubmit));
        }

        internal CombatDisplayTables Tables { get; }

        internal ICombatStateBufferReadSink State { get; }

        internal ICharacterBufferReadSink Characters { get; }

        internal ICombatViewBufferReadSink InputRead { get; }

        internal ICombatViewBufferWriteSink InputWrite { get; }

        internal Func<CombatInputFacts> ReadPathFacts { get; }

        internal Func<bool, CombatInputFacts> ReadInputFacts { get; }

        internal Func<bool> CanReceiveUiInput { get; }

        internal Action ReportBlockedInput { get; }

        internal RuntimeFaultBoundary Faults { get; }

        internal IAudioSystemService Audio { get; }

        internal Func<int, int> ItemStock { get; }

        internal IUITextPresentation Text { get; }

        internal IUIConfirmationPresentation Confirmation { get; }

        internal Func<bool> IsInputReady { get; }

        internal Action AfterConfirmedSubmit { get; }
    }
}
