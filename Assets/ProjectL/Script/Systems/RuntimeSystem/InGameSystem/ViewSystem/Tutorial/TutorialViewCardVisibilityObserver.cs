using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>빌린 인스턴스가 꺼진 사실만 알립니다. 진행·입력·Job을 처리하지 않습니다.</summary>
    internal sealed class TutorialViewCardVisibilityObserver : MonoBehaviour
    {
        internal event Action Disabled;

        private void OnDisable()
        {
            Disabled?.Invoke();
        }
    }
}
