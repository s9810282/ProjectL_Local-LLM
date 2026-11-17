using ProjectL.Framework.SystemBase;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.InputSystem.View
{
    public enum KeyBindingAction
    {
        EnterCombat,
        EndTurn,
        AttackMode,
        Return,
        RotateLeft,
        RotateRight,
        // 전투 플레이어 액티브 스킬 키(기본 R)입니다. 화면 줄에 직렬화된 정수가 바뀌지 않게 맨 끝에 둡니다(전투 스킬 View 설계 SK6).
        Skill
    }

    public enum KeyBindingResult
    {
        Success,
        Forbidden,
        Duplicate,
        CloseRequested
    }

    /// <summary>키 입력 결과를 표시 측에 명시적으로 반환합니다.</summary>
    public interface IKeyBindingReceiver
    {
        void KeyBindingReceived(KeyBindingResult result);
    }

    /// <summary>런타임 키 변경과 옵션 편집의 저장·취소를 소유합니다.</summary>
    public interface IKeyBindingService : IServiceModule
    {
        bool HasChanges { get; }
        string GetKeyText(KeyBindingAction action);
        void BeginCapture(KeyBindingAction action, IKeyBindingReceiver receiver);
        void CancelCapture();
        void BeginEdit();
        void SaveEdit();
        void CancelEdit();
    }
}
