using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.BufferSystems.Character;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction
{
    /// <summary>
    /// 전투 입력 기능 하나가 입력 기능 묶음(<see cref="CombatInteraction"/>)의 공통 수명에 참여하는 방법입니다(View 2라운드 W5-1).
    /// 기능 파일 안의 작은 어댑터가 이것을 상속해 "이 단계에서는 내 어느 메서드"만 답하고, 묶음은 목록 순서대로 단계를 돕니다.
    /// 기본 구현은 "그 단계에서 할 일 없음"입니다(사건 처리기 <c>CombatEventHandler</c>와 같은 꼴).
    /// </summary>
    /// <remarks>
    /// <para>
    /// 전경 소유자는 포인터를 받는 기능(배치·아이템·공격·경로)이고, 활성(<see cref="IsActive"/>)인 것은 많아야 하나입니다.
    /// 배경 소유자(<see cref="IsBackground"/>)는 사용자 조작 없이 화면을 맞추는 기능(유닛 호버 범위)이며, 전경 처리 뒤에 묶음이 계산한
    /// 허용 값과 함께 <see cref="ObservePointer"/>·<see cref="ObserveTick"/>를 받습니다.
    /// 공격 Drag 넘겨받기는 셋째 꼴입니다: 배경이 아니고, 포인터 대상이 없고(Router가 먼저 넘김), 활성이 되지 않습니다. 몸짓 무효화는 정리 단계(선택 취소·결과 재생 전·표현 정리)와 매 Tick 감시(<see cref="Poll"/>, 후보가 낡았을 때)에서 합니다.
    /// </para>
    /// <para>
    /// 단계마다 정리 범위가 다릅니다. <see cref="CancelPresentation"/>은 경로를 뺀 표현 정리, <see cref="CancelInputs"/>는 경로까지 포함한 정리,
    /// <see cref="Stop"/>은 연결 해제이고, 앞의 것이 기본 구현입니다. <see cref="SelectCanceled"/>는 선택이 강제로 끝났을 때(Layer 전환·Unbind)입니다.
    /// </para>
    /// </remarks>
    internal abstract class CombatInputOwner
    {
        /// <summary>이 소유자가 받는 포인터 대상입니다. 포인터를 받지 않는 소유자는 None입니다. 묶음 안에서 대상은 소유자 하나에만 있습니다.</summary>
        internal virtual CombatInputRouter.PointerTarget Target => CombatInputRouter.PointerTarget.None;

        /// <summary>포인터를 쓰는 중(Drag·모드·경로 지정)인지입니다.</summary>
        internal virtual bool IsActive => false;

        /// <summary>배경 소유자인지입니다. 배경 소유자는 전경 소유자가 활성이 되기 전에 내려놓습니다(<see cref="YieldToForeground"/>).</summary>
        internal virtual bool IsBackground => false;

        /// <summary>대상이 이 소유자인 포인터 이동입니다.</summary>
        internal virtual void PointerMoved(in CombatPointerInput input)
        {
        }

        /// <summary>대상이 이 소유자인 선택 누름입니다.</summary>
        internal virtual void SelectPressed(in CombatPointerInput input)
        {
        }

        /// <summary>대상이 이 소유자인 선택 뗌입니다.</summary>
        internal virtual void SelectReleased(in CombatPointerInput input)
        {
        }

        /// <summary>선택이 강제로 끝났습니다(Layer 전환·Unbind). 명령은 기록하지 않습니다.</summary>
        internal virtual void SelectCanceled()
        {
        }

        /// <summary>배경 소유자의 포인터 이동입니다. 전경 소유자가 처리한 뒤에 받습니다. <paramref name="canShow"/>가 거짓이면 내려놓습니다.</summary>
        internal virtual void ObservePointer(in CombatPointerInput input, bool canShow)
        {
        }

        /// <summary>
        /// 매 Tick 감시입니다. 마지막 응답(번호 무관)을 보며 자기 요청의 수명을 확인하고 다시 시도합니다(View 2라운드 W5-3).
        /// 응답 단계(<see cref="PreviewAnswered"/>)보다 앞에 받습니다. 응답이 없으면 null입니다.
        /// </summary>
        internal virtual void Poll(CombatPreviewResult latestPreview)
        {
        }

        /// <summary>배경 소유자의 매 Tick 감시입니다. 전경 감시 뒤에 받습니다. <paramref name="canShow"/>가 거짓이면 내려놓습니다.</summary>
        internal virtual void ObserveTick(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters, bool canShow)
        {
        }

        /// <summary>
        /// 새 응답일 때 한 번, 현재 슬롯 답(<see cref="CombatInputFacts.PreviewAnswer"/>, null 아님)을 받습니다. 자기 요청이면 쓰고 true입니다.
        /// 요청 번호는 하나뿐이라 쓰는 소유자는 많아야 하나입니다.
        /// </summary>
        internal virtual bool PreviewAnswered(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters) => false;

        /// <summary>결과 재생 전입니다.</summary>
        internal virtual void BeforeResult()
        {
        }

        /// <summary>결과 후처리입니다(확인 전). 현재 사실과 맞지 않게 된 Drag·모드를 정리하고, 남은 쪽은 새 번호로 다시 요청합니다.</summary>
        internal virtual void AfterResult(in CombatInputFacts facts, IReadOnlyList<CharacterData> characters)
        {
        }

        /// <summary>결과 확인(처리 번호 삭제·확인 번호 기록) 뒤입니다.</summary>
        internal virtual void AfterAcknowledged()
        {
        }

        /// <summary>전경 소유자 하나가 활성이 되기 전입니다. 배경 소유자는 표시·요청을 내려놓습니다.</summary>
        internal virtual void YieldToForeground()
        {
        }

        /// <summary>경로를 뺀 표현 정리입니다: Drag·고스트·모드·자기 미리보기. 정리할 것이 없으면 아무것도 하지 않습니다. 대기는 보존합니다.</summary>
        internal virtual void CancelPresentation()
        {
        }

        /// <summary>경로까지 포함한 정리입니다. 기본은 <see cref="CancelPresentation"/>입니다.</summary>
        internal virtual void CancelInputs() => CancelPresentation();

        /// <summary>오류 정지·씬 연결 해제입니다. 기본은 <see cref="CancelInputs"/>입니다.</summary>
        internal virtual void Stop() => CancelInputs();
    }
}
