namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction
{
    /// <summary>
    /// 미리보기 슬롯 하나를 나눠 쓰는 배경 요청의 규칙입니다(호버 공격 범위 설계 UR4, 계획 P1 A). 배경 요청은 사용자가 조작하지 않아도
    /// 화면을 맞추려고 내는 요청(선택 행동 가용성·이동 모드 범위·호버 공격 범위)이고, 모두 이 규칙만 부릅니다.
    /// 사용자 조작이 내는 전경 요청(Drag·공격 모드·배치 Drag·복귀 버튼)은 이 규칙 없이 바로 덮어씁니다.
    /// </summary>
    /// <remarks>
    /// <para>R-a: 슬롯이 비었거나, 들어 있는 요청에 Core가 이미 답했을 때만 요청합니다. 답을 받지 않은 남의 요청은 기다립니다.</para>
    /// <para>R-b: 지금 사실로 이미 답을 받았거나 자기 요청이 아직 기다리는 중이면 다시 요청하지 않습니다(슬롯을 잃었어도).</para>
    /// <para>둘이 함께라서 서로 덮어쓰는 반복 없이 각 요청이 한 번씩 답을 받습니다. 답 받음·기다림은 요청자마다 자기 사실로 기억하고 여기에 넘깁니다.</para>
    /// </remarks>
    internal static class CombatPreviewSlotRule
    {
        /// <summary>배경 요청을 지금 내도 되는지입니다(R-b를 통과하고 R-a를 통과).</summary>
        /// <param name="answeredForFacts">지금 사실로 이미 답을 받았는지입니다(요청자가 기억한 값).</param>
        /// <param name="ownPending">자기 요청이 슬롯에서 답을 기다리는 중인지입니다.</param>
        /// <param name="hasPreview">슬롯에 요청이 있는지입니다.</param>
        /// <param name="slotNumber">슬롯 요청 번호입니다. 요청이 없으면 무시합니다.</param>
        /// <param name="answeredNumber">Core가 마지막으로 답한 요청 번호입니다.</param>
        internal static bool ShouldRequestBackground(bool answeredForFacts, bool ownPending,
            bool hasPreview, int slotNumber, int answeredNumber)
        {
            return !IsSettled(answeredForFacts, ownPending) && IsIdle(hasPreview, slotNumber, answeredNumber);
        }

        /// <summary>R-b: 다시 요청할 필요가 없는지입니다. 사실이 바뀌면 요청자가 기억을 지워 다시 요청하게 됩니다.</summary>
        internal static bool IsSettled(bool answeredForFacts, bool ownPending) => answeredForFacts || ownPending;

        /// <summary>R-a: 슬롯이 한가한지(비었거나 들어 있는 요청에 답이 났는지)입니다.</summary>
        internal static bool IsIdle(bool hasPreview, int slotNumber, int answeredNumber) =>
            !hasPreview || slotNumber <= answeredNumber;
    }
}
