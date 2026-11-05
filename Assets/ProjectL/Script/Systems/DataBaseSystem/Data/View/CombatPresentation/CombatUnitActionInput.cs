using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;
using UnityEngine;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 특성 발동의 유닛 행동 입력입니다(유닛 행동 설계 AA1). 값(<see cref="CombatUnitActionData"/>)은 읽을 때마다 새로 만듭니다(쓰는 쪽이 보관).
    /// 행동 유닛이 <see cref="CombatUnitActionActor.None"/>(기본값)이면 행동이 없고 나머지 칸은 보지 않습니다.
    /// </summary>
    [Serializable]
    internal sealed class CombatUnitActionInput
    {
        [Tooltip("누가 행동하나입니다. None이면 행동이 없습니다(큐·추가 공격만). Source = 발동 유닛(대신 맞기는 수호자), Targets = 발동 대상마다.")]
        [SerializeField] private CombatUnitActionActor actor = CombatUnitActionActor.None;
        [Tooltip("어디로 가나입니다. Stay = 제자리(방향만), GuardFront = 지켜진 유닛 앞(공격자 쪽), TowardTarget = 대상 쪽, AwayFromAttacker = 공격자 반대쪽, "
            + "Relocate = 미뤄 둔 사실 자리로 한 번 감(위치 교환 승객, 돌아오지 않음 — 거리·머묾·돌아오기 0, 보는 쪽 Keep). GuardFront·AwayFromAttacker는 대신 맞기에서만 기준점이 있습니다.")]
        [SerializeField] private CombatUnitActionPlace place = CombatUnitActionPlace.Stay;
        [Tooltip("기준점에서 떨어지는 거리입니다(칸 비율, 0~1.5). GuardFront는 지켜진 유닛이 서 있는 자리(발밑)에서 공격자 쪽으로, TowardTarget은 행동 유닛 자리에서 잽니다. 제자리면 쓰지 않습니다.")]
        [SerializeField, Range(0f, CombatUnitActionData.MaxDistance)] private float distance = 0.6f;
        [Tooltip("옮기는 모양입니다. Hop이면 뛰어 옮기고, 높이는 전투 연출 설정의 뛰기 높이를 같이 씁니다.")]
        [SerializeField] private CombatStepLook look = CombatStepLook.Hop;
        [Tooltip("나가는 시간(초)입니다. 대신 맞기의 목록은 동시에 나가며, 모두 끝난 뒤 공격합니다. 제자리면 도는 시간이고 0이면 즉시입니다.")]
        [SerializeField, Min(0f)] private float outSeconds = 0.2f;
        [Tooltip("머무는 시간(초)입니다. 사건 순간에 하는 행동만 씁니다. 대신 맞기는 그 유닛의 피격·사망 연출이 끝날 때까지 머물러 이 값을 쓰지 않습니다.")]
        [SerializeField, Min(0f)] private float holdSeconds;
        [Tooltip("돌아오는 시간(초)입니다. 대신 맞기는 수호자와 해당 행동 유닛의 피격 후속이 끝나면 복귀합니다. 0이면 즉시, 죽은 유닛은 돌아오지 않습니다.")]
        [SerializeField, Min(0f)] private float returnSeconds = 0.25f;
        [Tooltip("머무는 동안 볼 쪽입니다. Keep = 지금 방향(옮겨 가면 가는 쪽), Target = 대상(대신 맞기는 지켜진 유닛), Attacker = 공격자(대신 맞기에서만).")]
        [SerializeField] private CombatUnitActionFace face = CombatUnitActionFace.Keep;

        /// <summary>행동 값입니다. 행동 유닛이 <see cref="CombatUnitActionActor.None"/>이면 null입니다.</summary>
        /// <exception cref="ArgumentOutOfRangeException">값이 범위 밖일 때입니다(<see cref="CombatUnitActionData"/> 생성자).</exception>
        internal CombatUnitActionData ToData() => actor == CombatUnitActionActor.None
            ? null
            : new CombatUnitActionData(actor, place, distance, look, outSeconds, holdSeconds, returnSeconds, face);
    }
}
