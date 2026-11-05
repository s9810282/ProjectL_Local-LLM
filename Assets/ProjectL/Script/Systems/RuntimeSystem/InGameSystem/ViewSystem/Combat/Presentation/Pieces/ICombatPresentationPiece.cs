namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Pieces
{
    /// <summary>
    /// 연출 조각 하나의 계약입니다(조각·조립 설계 BP1). 조각은 자기 연출만 압니다. 다음에 무엇이 오는지, 사건이 언제 완료되는지는 모릅니다.
    /// </summary>
    /// <remarks>
    /// 지켜야 할 뜻:
    /// <list type="bullet">
    /// <item><see cref="Begin"/>은 시간 0으로 시작합니다. 시작 안에서 끝날 수 있습니다(시간 없는 조각, 길이 0).</item>
    /// <item>끝나면 실행(<see cref="CombatPieceRun"/>)이 그 조각의 끝 신호를 냅니다. 끝 말고 다른 신호(예: 타격 <see cref="CombatPieceSignal.Hit"/>)는
    /// 맥락의 신호 입구(<see cref="CombatPieceContext.SignalCallback"/>)로 냅니다.</item>
    /// <item>자원은 직접 빌리지 않습니다. 실행이 빌린 대여를 맥락(<see cref="CombatPieceContext"/>)에서 받고, 되돌리는 것도 실행이 합니다.</item>
    /// <item>외부 진행 조각(진행을 다른 재생기가 함)은 맥락의 끝 입구(<see cref="CombatPieceContext.EndCallback"/>)를 진행 쪽 완료 콜백으로 넘깁니다.
    /// 그 조각의 <see cref="Tick"/>·<see cref="Cancel"/>은 아무 일도 하지 않고, 끝은 실행이 콜백으로 바로 받습니다.</item>
    /// </list>
    /// </remarks>
    internal interface ICombatPresentationPiece
    {
        /// <summary>시간 0으로 시작합니다. 바로 끝나도 됩니다.</summary>
        void Begin(CombatPieceContext context);

        /// <summary>자기 시계로 진행합니다. 음수 시간은 받지 않습니다.</summary>
        void Tick(float deltaTime);

        /// <summary>자기 연출이 끝났는지입니다. 외부 진행 조각은 false여도 됩니다(끝은 콜백으로 옵니다).</summary>
        bool IsEnded { get; }

        /// <summary>진행을 멈춥니다. 대여 복원은 실행이 합니다.</summary>
        void Cancel();
    }
}
