using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Common
{
    /// <summary>팝업 화면의 열림 상태와 알림을 연결합니다.</summary>
    internal interface IPopupViewScreen
    {
        UIId Id { get; }
        bool IsOpen { get; }
        void Subscribe(Action<bool> openStateChanged);
        void Unsubscribe(Action<bool> openStateChanged);
    }
}
