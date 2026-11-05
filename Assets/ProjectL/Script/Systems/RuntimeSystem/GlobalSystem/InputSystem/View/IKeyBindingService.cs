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
        RotateRight
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
