namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Attack
{
    /// <summary>공격 하나의 표시 동작입니다. 계산·결과 확인·개체 View의 대여는 맡지 않습니다.</summary>
    /// <remarks>
    /// Begin/Tick은 완료 사실만 제공하며 호출자는 Release 뒤 완료를 통지합니다.
    /// Cancel은 완료 통지가 아니고 Release는 동작이 소유한 자원을 멱등으로 정리합니다.
    /// </remarks>
    internal interface ICombatAttackAnimation
    {
        bool IsComplete { get; }
        void Begin();
        void Tick(float deltaTime);
        void Cancel();
        void Release();
    }
}
