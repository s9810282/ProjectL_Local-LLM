using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    public interface ICombatViewService : IServiceModule, ICombatExitPresentation, ICombatProgressView
    {
        void Attach(CombatSceneRoot scene);
        void Detach();
        void FocusLost();
        void CaptureLost();
        void RequestReturn();
    }
}
