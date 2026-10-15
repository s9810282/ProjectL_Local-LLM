using System;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.NodeEvent.Core
{
    public interface IRegisterEvent
    {
        void RegisterEvent(Action action);
        void ClearEvent();
    }
}
