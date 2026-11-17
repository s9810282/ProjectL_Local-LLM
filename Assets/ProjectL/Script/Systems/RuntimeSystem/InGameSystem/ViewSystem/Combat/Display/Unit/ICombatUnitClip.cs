using ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 유닛 하나의 사건 클립 채널 대여입니다(클립 설계 AC3·AC9). 유닛마다 하나이고, 바닥(Idle 컨트롤러) 위에 사건 클립 하나를 얹습니다.
    /// 사건 클립 하나는 주 클립(몸)과 같은 시각으로 도는 탑승물·부속 클립(말·무기 등)입니다(설계 6차 AC16·AC17, 클립 칸 다시 짜기 설계 CS5).
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
        /// 사건 클립(주 클립 + 탑승물·부속 클립)을 위층에 꽂고 시각을 0으로 둡니다.
        /// 대여의 첫 호출은 바닥(Idle)에서 <paramref name="blend"/>(In)로 섞어 들어오고, 그 유닛 채널에 남은 꼬리(뒤 단계·Out 섞기)가 있으면 끊고 꽂습니다(설계 P9).
        /// 같은 대여 안에서 다시 부르면 단계를 바꾸고 앞 단계에서 <paramref name="blend"/>(Between)로 섞습니다(클립 칸 다시 짜기 설계 CS6·CS7).
        /// 블렌드가 꺼져 있으면 바로 바꿉니다(설계 AC9, 지금과 같음). 대여는 값의 이벤트 시각을 쓰지 않습니다(시각은 재생 단위가 <see cref="SetTime"/>으로 정함).
        /// </summary>
        void Play(CombatClipData clip, CombatClipBlend blend);

        /// <summary>사건 클립(주 클립·탑승물·부속 클립 모두)의 시각(초)만 정합니다. 그 자세는 같은 프레임의 Unity 평가에서 보입니다.</summary>
        void SetTime(float time);

        /// <summary>섞는 중인지입니다(설계 CS7).</summary>
        bool IsBlending { get; }

        /// <summary>
        /// 섞기를 실제 초만큼 진행합니다(재생 배율은 걸지 않음, 설계 P12). 대여 동안 섞기 시간은 대여를 쥔 쪽이 같은 시계로 넣습니다(설계 CS7 시간).
        /// <see cref="PlayTail"/>로 넘긴 뒤에는 채널이 혼자 넣으므로 아무 일도 하지 않습니다.
        /// </summary>
        void AdvanceBlend(float deltaTime);

        /// <summary>
        /// 본 단계가 끝난 순간 나머지를 채널에 넘깁니다(클립 칸 다시 짜기 설계 CS6·CS7, 결정 Q-A7 A).
        /// 뒤 단계(<paramref name="tail"/>, 없으면 null)는 <paramref name="between"/>으로 섞어 꽂고 게임 시간 × <paramref name="speed"/>로 혼자 끝까지 돌리며,
        /// 마지막 단계가 끝나면 <paramref name="fadeOut"/>으로 Idle에 섞어 돌아갑니다(뒤 단계가 없으면 넘기는 순간부터). 넘길 때 남은 섞기는 채널이 이어 진행합니다.
        /// 뒤 단계와 Out 섞기는 이 대여가 돌아가도 이어지고, 그 유닛의 다음 클립·취소·풀 반환·래그돌이 끊습니다(설계 P8). 부른 뒤에는 <see cref="SetTime"/>을 쓰지 않습니다.
        /// </summary>
        void PlayTail(CombatClipData tail, float speed, CombatClipBlend between, CombatClipBlend fadeOut);

        /// <summary>
        /// 위층을 빼고 바닥(Idle)으로 바로 돌아갑니다. 넘긴 뒤 단계·Out 섞기가 아직 돌고 있으면 그대로 두고 대여만 돌려줍니다.
        /// 이미 끝났거나 다른 대여로 바뀌었으면 아무 일도 하지 않습니다.
        /// </summary>
        void RestoreClip();

        /// <summary>
        /// 번호별 이벤트 지점(이벤트 컴포넌트의 Event Points 목록 칸)의 월드 위치·회전입니다(클립 칸 다시 짜기 설계 CS4). 목록 밖이거나 칸이 비었으면 false이고,
        /// 음수 번호는 인자 오류입니다. 원거리 발사 지점과 이펙트 자리가 같은 입구를 씁니다. 클립 채널과 같은 컴포넌트의 자리 표시를 읽기만 하며,
        /// Unity 평가 전에 읽으므로 앞 프레임 자세의 값입니다.
        /// </summary>
        bool TryGetEventPoint(int point, out Vector3 position, out Quaternion rotation);
    }
}
