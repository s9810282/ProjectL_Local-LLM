using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 유닛 하나의 사건 클립 채널 대여입니다(클립 설계 AC3·AC9). 유닛마다 하나이고, 바닥(Idle 컨트롤러) 위에 사건 클립 하나를 얹습니다.
    /// 사건 클립 하나는 주 클립(몸)과 같은 시각으로 도는 부속 클립(무기 등)입니다(설계 6차 AC16·AC17).
    /// </summary>
    /// <remarks>
    /// 구현은 ID·대여 수명을 확인하고, 다음 대여를 이전 <see cref="RestoreClip"/>가 수정하지 못하게 합니다.
    /// 표시가 꺼지거나 다시 대여되면 대여가 취소되고, 시작 때 받은 취소 콜백을 부릅니다.
    /// 그래프의 평가는 그래프 주인이 Unity 게임 시간으로 합니다(결정 Q-AC10 A). 대여는 시각만 정하고 평가하지 않습니다(설계 I2).
    /// </remarks>
    internal interface ICombatUnitClip
    {
        bool IsAvailable { get; }

        /// <summary>
        /// 사건 클립(주 클립 + 부속 클립)을 위층에 꽂고 시각을 0으로 둡니다. 바닥(Idle)은 이 클립이 끝날 때까지 쉽니다(바로 갈아 끼움, 설계 AC9).
        /// 대여는 값의 이벤트 시각을 쓰지 않습니다(시각은 재생 단위가 <see cref="SetTime"/>으로 정함).
        /// </summary>
        void Play(CombatClipData clip);

        /// <summary>사건 클립(주 클립·부속 클립 모두)의 시각(초)만 정합니다. 그 자세는 같은 프레임의 Unity 평가에서 보입니다.</summary>
        void SetTime(float time);

        /// <summary>위층을 빼고 바닥(Idle)으로 돌아갑니다. 이미 끝났거나 다른 대여로 바뀌었으면 아무 일도 하지 않습니다.</summary>
        void RestoreClip();

        /// <summary>
        /// 원거리 공격 클립의 발사 지점(이벤트 컴포넌트의 포구) 월드 위치입니다(클립 설계 5차 AC13, 계획 P9). 포구가 없으면 false입니다.
        /// 클립 채널과 같은 컴포넌트의 자리 표시를 읽기만 합니다. Unity 평가 전에 읽으므로 앞 프레임 자세의 위치입니다.
        /// </summary>
        bool TryGetMuzzle(out Vector3 position);

        /// <summary>지정 번호 총구의 현재 월드 위치입니다. 정의/총구가 없으면 false이며 음수 번호는 인자 오류입니다.</summary>
        bool TryGetMuzzle(int point, out Vector3 position);

        /// <summary>
        /// 공격 클립의 이벤트 타이밍 이펙트 지점(이벤트 컴포넌트의 <c>effectPoint</c>) 월드 위치·회전입니다(이펙트 설계 EF4, 계획 P8). 지점이 없으면 false입니다.
        /// 포구와 같이 Unity 평가 전에 읽으므로 앞 프레임 자세의 값입니다.
        /// </summary>
        bool TryGetEffectPoint(out Vector3 position, out Quaternion rotation);

        /// <summary>지정 번호 이펙트 지점의 현재 월드 자세입니다. 정의/지점이 없으면 false입니다.</summary>
        bool TryGetEffectPoint(int point, out Vector3 position, out Quaternion rotation);
    }
}
