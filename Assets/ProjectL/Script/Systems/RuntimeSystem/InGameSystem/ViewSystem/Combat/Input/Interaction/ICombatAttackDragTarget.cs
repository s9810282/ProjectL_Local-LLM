namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Interaction
{
    /// <summary>
    /// 공격 Drag를 넘겨받는 쪽(이동 경로 입력)의 좁은 계약입니다(View 2라운드 W5-5). 넘겨받기 소유자(<see cref="CombatAttackDragHandover"/>)는 받는 쪽을 이것으로만 봅니다.
    /// 출발점을 고르고 지금도 맞는지 보는 판단은 받는 쪽의 지정 상태를 읽어야 해서 받는 쪽에 남습니다.
    /// </summary>
    internal interface ICombatAttackDragTarget
    {
        /// <summary>연결이 끊겼는지입니다(정지 뒤). 끊긴 받는 쪽에는 넘기지 않습니다.</summary>
        bool IsDetached { get; }

        /// <summary>공격 모드에서 누른 위치가 지금 행위자의 칸이면 그 출발점을 줍니다.</summary>
        bool TryGetAttackDragOrigin(float x, float y, out CombatAttackDragGesture.Origin origin);

        /// <summary>출발점이 지금 사실(전투·행위자·칸·결과 번호)과 맞고 포인터 대상이 <paramref name="expectedTarget"/>인지입니다.</summary>
        bool IsAttackDragOriginCurrent(in CombatAttackDragGesture.Origin origin, CombatInputRouter.PointerTarget expectedTarget);

        /// <summary>출발점에서 경로 지정을 시작합니다. 시작해서 지정 중이면 true입니다.</summary>
        bool TryBeginTransferredDrag(in CombatAttackDragGesture.Origin origin);

        void PointerMoved(float screenX, float screenY);

        void SelectReleased(float screenX, float screenY);

        /// <summary>경로 입력을 취소합니다(UI 위에서 뗐을 때).</summary>
        void CancelInputs();
    }
}
