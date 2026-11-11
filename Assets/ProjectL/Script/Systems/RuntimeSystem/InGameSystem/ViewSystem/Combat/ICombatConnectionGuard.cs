namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>
    /// 씬 연결 한 번에 대한 재확인 규칙입니다(조정자 나누기 설계 CS1). 조정자(<see cref="CombatViewSystem"/>)가 구현하고,
    /// 조정자에서 나온 입력 분배(<c>Input.CombatSceneInputDriver</c>)는 조정자 전체가 아니라 이 계약만 받습니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 외부 호출(연출·표시·UI 콜백·Layer 변경)에서 돌아온 뒤에는 그 안에서 오류 보고·Detach가 일어났을 수 있어, 처음 잡은 연결로 이어 가도 되는지 다시 묻습니다.
    /// 연결은 필드로 갖지 않고 매 호출 인자로 받으므로 "처음 잡은 연결 = 지금 연결"을 확인하는 지금 방식이 그대로입니다.
    /// </para>
    /// <para>
    /// 입력 해제 도중 슬롯이 보내는 정리 신호(포커스 상실·선택 강제 종료)는 재생 연결이 끊긴 뒤에도 기존 소유자에게 끝까지 가야 합니다(R2).
    /// 그 갈래는 <see cref="IsLive"/>가 아니라 <see cref="IsConnected"/>로 연결만 봅니다.
    /// </para>
    /// </remarks>
    internal interface ICombatConnectionGuard
    {
        /// <summary>
        /// R-A: 이 연결로 이어 가도 되는지입니다. 오류 정지 전 ∧ 진입 활성 ∧ 재생 연결 ∧ 이 연결이 지금 연결입니다.
        /// </summary>
        bool IsLive(CombatViewConnection connection);

        /// <summary>
        /// R-B: 이 연결이 지금 연결인지입니다(재생·오류와 무관). null이면 거짓입니다. 입력 해제 중 정리 신호와 첫 그리기·루트 구독 자리가 씁니다.
        /// </summary>
        bool IsConnected(CombatViewConnection connection);

        /// <summary>R-D: 입력 판단에 쓸 수 있는지입니다(씬 연결 활성 ∧ 입력 슬롯 연결 ∧ 진입 연출 완료).</summary>
        bool IsInputReady { get; }
    }
}
