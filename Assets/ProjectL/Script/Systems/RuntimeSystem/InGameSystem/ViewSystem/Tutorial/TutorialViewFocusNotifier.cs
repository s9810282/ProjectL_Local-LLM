using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Tutorial
{
    /// <summary>
    /// 게임 창 활성 변경을 튜토리얼 실행 소유자에게 알립니다(튜토리얼 설계 §15.4.1). 사실만 전하고 설명을 넘기지 않습니다.
    /// 실행 소유자가 Combat 화면 연결 때 구독하고 해제 때 끊습니다. 꺼진 오브젝트는 알림을 받지 못하므로 늘 켜진 곳(튜토리얼 Canvas 루트)에 둡니다.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TutorialViewFocusNotifier : MonoBehaviour
    {
        internal event Action<bool> FocusChanged;

        private void OnApplicationFocus(bool hasFocus)
        {
            FocusChanged?.Invoke(hasFocus);
        }
    }
}
