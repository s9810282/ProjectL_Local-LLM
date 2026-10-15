using System;
using ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.NodeEvent.Core;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.GlobalSystem.UtilitySystem.NodeEvent.View
{
    [DisallowMultipleComponent]
    public sealed class UnityEventNode : MonoBehaviour, IRegisterEvent, IInvokeEvent
    {
        private Action action;
        public void RegisterEvent(Action action) => this.action = action ?? throw new ArgumentNullException(nameof(action));
        public void ClearEvent() => action = null;
        public void InvokeEvent()
        {
            if (this != null && isActiveAndEnabled) action?.Invoke();
        }
        private void OnDestroy() => ClearEvent();
    }
}
