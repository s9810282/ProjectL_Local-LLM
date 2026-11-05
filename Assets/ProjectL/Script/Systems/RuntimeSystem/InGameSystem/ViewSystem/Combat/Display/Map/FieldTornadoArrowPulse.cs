using System;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map
{
    /// <summary>
    /// 토네이도 방향 화살촉 점등(켜짐 → 앞으로 밀림 → 꺼짐 반복)의 프레임 콜백입니다. <see cref="FieldTornadoPresenter"/>가 자기 루트에 붙이고 매 프레임 지난 시간을 넘겨받습니다.
    /// </summary>
    /// <remarks>
    /// 경고 화살촉은 결과 재생이 끝난 뒤(플레이어 차례)에도 켜져 있어야 해서 재생 Tick이 아니라 Unity 프레임 콜백을 씁니다(이펙트 받침 <c>CombatEffectFollower</c>와 같은 까닭).
    /// 무엇을 할지는 표시가 정하고 이 컴포넌트는 시간만 넘깁니다. 루트와 함께 없어집니다.
    /// </remarks>
    public sealed class FieldTornadoArrowPulse : MonoBehaviour
    {
        private Action<float> _tick;

        internal void Bind(Action<float> tick)
        {
            _tick = tick ?? throw new ArgumentNullException(nameof(tick));
        }

        private void Update()
        {
            _tick?.Invoke(Time.deltaTime);
        }
    }
}
