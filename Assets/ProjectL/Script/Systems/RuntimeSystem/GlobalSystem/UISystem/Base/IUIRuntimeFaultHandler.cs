using System;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base
{
    /// <summary>UI의 조회 실패를 런타임 공용 오류 경계에 전달합니다.</summary>
    internal interface IUIRuntimeFaultHandler
    {
        bool IsFaulted { get; }
        void Report(Exception error);
    }
}
