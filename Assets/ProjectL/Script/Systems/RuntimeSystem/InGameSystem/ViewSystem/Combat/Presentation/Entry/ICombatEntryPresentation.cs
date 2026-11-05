namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Entry
{
    /// <summary>씬 연결이 진행하는 진입 연출입니다. 준비는 표시 설치, 진행과 취소는 연결 수명이 맡습니다.</summary>
    internal interface ICombatEntryPresentation
    {
        bool IsPending { get; }
        void Tick(float deltaTime, bool isLoading);
        void Cancel();
    }
}
