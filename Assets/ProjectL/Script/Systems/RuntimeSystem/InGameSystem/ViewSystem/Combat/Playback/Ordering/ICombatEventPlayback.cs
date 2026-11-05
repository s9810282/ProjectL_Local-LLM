using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Playback.Ordering
{
    /// <summary>
    /// 순차 연출(<see cref="OrderedCombatPresentation"/>)이 결과 하나를 재생할 때 표시 쪽에 거는 계약 전체입니다(구조 리팩토링 설계 D6).
    /// </summary>
    /// <remarks>
    /// <para>
    /// 예전에는 순서 준비·Barrier·이동 시작/끝을 형식 검사(as/is)로 꺼내 썼고, 구현하지 않은 표시는 대기를 조용히 건너뛰었습니다.
    /// 이제는 모두 이 계약의 일부입니다. 기다릴 것이 없는 구현은 "대기 없음·즉시 완료"로 구현합니다
    /// (Barrier는 항상 시작 가능·완료, 순서 준비·이동 시작은 아무것도 하지 않음, 이동 끝은 바로 완료 통지).
    /// </para>
    /// <para>
    /// 결과 준비·사건·Barrier·이동 시작/끝·진행/취소는 이 계약 말고는 소비자가 없어 별도 계약으로 나누지 않고 여기에 둡니다(구조 개선 SV3).
    /// 이동 연결(<see cref="CombatMovementPlayback"/>)이 따로 받는 <see cref="ICombatMovementPlaybackPresenter"/>만 분리합니다. 걸음은 유닛별 레인에서 함께 돌 수 있고
    /// 걸음별 사건 완료는 시작 때 넘긴 통지로 받습니다(동시 처리 설계 C5·C6).
    /// </para>
    /// <para>
    /// 구현: 재생 조정 <see cref="CombatSceneEventPlayback"/>(해제 뒤 사건 시작 거절, 리팩토링 설계 D9), 테스트 가짜.
    /// 표시는 <see cref="ICombatBattleDisplay.EventPlayback"/>으로 이 구현을 넘기기만 합니다(연출 조립 설계 §9 I-4).
    /// 사건 Presenter(<see cref="CombatSceneEventPresenter"/>)는 같은 이름의 멤버를 갖지만 이 계약을 구현하지 않습니다.
    /// 해제 방어가 없는 사건 Presenter를 연출(<see cref="ICombatPresentation.Attach"/>)에 넘기면 컴파일 오류가 납니다.
    /// </para>
    /// </remarks>
    internal interface ICombatEventPlayback : ICombatMovementPlaybackPresenter
    {
        /// <summary>
        /// 결과 하나의 표시를 준비합니다. 원본 순회는 유지하되 Railway 직후의 직접 사망은 해당 발동 안에서 접촉 순으로 표시할 수 있습니다.
        /// </summary>
        void BeginPresentation(CombatResult result);

        /// <summary>
        /// 순차 재생기가 붙을 때(<see cref="ICombatPresentation.Attach"/>) 자기 후속 정리 질문을 넘깁니다(유닛 행동 계획 TP-13). 대신 맞은 수호자의 돌아오기가 이 답을 기다립니다.
        /// 기다릴 것이 없는 구현은 받아 두지 않아도 됩니다.
        /// </summary>
        void BindSettlement(ICombatSourceSettlement settlement);

        /// <summary>
        /// 비이동 사건 하나를 표시합니다. <paramref name="completed"/>는 순차 진행을 넘겨도 된다는 사건 완료 통지이며(히트 타이밍 설계 H3),
        /// 공격 사건은 완료 정책에 따라 남은 공격 연출(투사체 비행·반동)보다 먼저 올 수 있습니다. 결과 전체 완료가 아니므로
        /// 순차 연출은 모든 사건 소비 뒤에도 <see cref="IsPresentationComplete"/>를 기다리고, 최종 표시·결과 확인은 기존 결과 완료 경로만 합니다.
        /// 중복·늦은 콜백의 결과 확인은 View 소유자가 막습니다.
        /// </summary>
        void BeginEvent(CombatEvent combatEvent, Action completed);

        /// <summary>
        /// 원본 순회와 후속 정산 전에 남은 표시를 기다리는 Barrier입니다. Railway 묶음은 열차·모든 접촉 사망의 완료까지 대기합니다.
        /// </summary>
        bool CanBeginPresentationEvent(CombatEvent item);

        /// <summary>남은 표시가 없는지입니다(Barrier). 결과의 최종 사실은 이것이 참일 때만 표시합니다.</summary>
        bool IsPresentationComplete { get; }

        /// <summary>한 이동 결과에서 유닛별 첫 이동 시작을 한 번 알립니다. 칸별 보간과는 별개입니다.</summary>
        void BeginMovement(CombatCharacterEvent firstStep);

        /// <summary>
        /// 마지막 걸음 뒤 경로 종료 연출이 끝나야 다음 사건으로 진행합니다. 취소는 완료가 아닙니다. 접촉으로 걸음 사건 완료가 일찍 와서
        /// 아직 보간 중이면 방향 복귀를 보간 끝 뒤로 미루고 바로 완료합니다(이동 접촉 설계 M3, 미룬 복귀는 남은 표시).
        /// </summary>
        void BeginMovementEnd(CombatCharacterEvent lastStep, Action completed);

        /// <summary>기존 View Tick에서 진행 중인 연출을 진행합니다.</summary>
        void TickPresentation(float deltaTime);

        /// <summary>진행 중인 연출을 취소합니다. 취소는 완료 통지가 아닙니다.</summary>
        void CancelPresentation();
    }
}
