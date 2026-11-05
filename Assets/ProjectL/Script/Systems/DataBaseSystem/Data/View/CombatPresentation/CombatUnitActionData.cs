using System;
using ProjectL.Systems.DataBaseSystem.Data.View.Table.CombatPresentationSettings.Item;

namespace ProjectL.Systems.DataBaseSystem.Data.View.CombatPresentation
{
    /// <summary>
    /// 특성 발동 하나가 유닛에게 시키는 화면 동작의 값입니다(유닛 행동 설계 AA1). 발동 줄의 세 번째 형태(큐·추가 공격·유닛 행동)입니다.
    /// 자리 옮기기 → 머물기 → 돌아오기로 이루어지고, Core 위치·사실은 바꾸지 않습니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 언제 시작해서 얼마나 머무는지는 이 값이 아니라 발동 사실이 온 자리가 정합니다(설계 §2). 사건 목록 안 발동이면 그 사건 순간에 시작해
    /// <see cref="HoldSeconds"/>만큼 머물고, 공격 결과 안 가로채기면 공격 동작보다 먼저 나가 그 유닛 연출이 끝난 뒤 돌아옵니다(머무는 시간은 쓰지 않음).
    /// </para>
    /// <para>
    /// 값 검사는 이 생성자 하나입니다. 어느 발동 종류가 가로채기인지는 Core가 정하므로 종류로 막지 않습니다(설계 P10).
    /// </para>
    /// </remarks>
    internal sealed class CombatUnitActionData
    {
        /// <summary>기준점에서 떨어지는 거리의 위 끝입니다(칸 비율). 지켜진 유닛 앞이 칸 밖으로 크게 나가지 않게 막습니다.</summary>
        internal const float MaxDistance = 1.5f;

        /// <exception cref="ArgumentOutOfRangeException">행동 유닛이 없거나(<see cref="CombatUnitActionActor.None"/>) 정의되지 않은 열거 값, 범위 밖의 수일 때입니다.</exception>
        internal CombatUnitActionData(CombatUnitActionActor actor, CombatUnitActionPlace place, float distance, CombatStepLook look,
            float outSeconds, float holdSeconds, float returnSeconds, CombatUnitActionFace face)
        {
            if (actor == CombatUnitActionActor.None || !System.Enum.IsDefined(typeof(CombatUnitActionActor), actor))
                throw new ArgumentOutOfRangeException(nameof(actor), actor, "행동 유닛은 발동 유닛 또는 대상이어야 합니다(행동이 없으면 값을 만들지 않습니다).");
            if (!System.Enum.IsDefined(typeof(CombatUnitActionPlace), place))
                throw new ArgumentOutOfRangeException(nameof(place), place, "정의되지 않은 행동 자리입니다.");
            if (!System.Enum.IsDefined(typeof(CombatStepLook), look))
                throw new ArgumentOutOfRangeException(nameof(look), look, "정의되지 않은 걸음 모양입니다.");
            if (!System.Enum.IsDefined(typeof(CombatUnitActionFace), face))
                throw new ArgumentOutOfRangeException(nameof(face), face, "정의되지 않은 보는 쪽입니다.");
            CombatPresentationValueRules.RequireNonNegative(distance, nameof(distance));
            if (distance > MaxDistance)
                throw new ArgumentOutOfRangeException(nameof(distance), distance, $"거리는 {MaxDistance}칸 이하여야 합니다.");
            CombatPresentationValueRules.RequireNonNegative(outSeconds, nameof(outSeconds));
            CombatPresentationValueRules.RequireNonNegative(holdSeconds, nameof(holdSeconds));
            CombatPresentationValueRules.RequireNonNegative(returnSeconds, nameof(returnSeconds));
            // 위치 이동은 미뤄 둔 사실 자리로 한 번 가고 끝입니다(계획 TP-E5). 뜻이 없는 칸은 값으로 받지 않습니다.
            if (place == CombatUnitActionPlace.Relocate && (distance != 0f || holdSeconds != 0f || returnSeconds != 0f || face != CombatUnitActionFace.Keep))
                throw new ArgumentOutOfRangeException(nameof(place), place,
                    "위치 이동(Relocate)은 거리·머무는 시간·돌아오는 시간이 0이고 보는 쪽이 Keep이어야 합니다(바깥 시간·걸음 모양만 씀).");
            Actor = actor;
            Place = place;
            Distance = distance;
            Look = look;
            OutSeconds = outSeconds;
            HoldSeconds = holdSeconds;
            ReturnSeconds = returnSeconds;
            Face = face;
        }

        /// <summary>누가 하는지입니다. 늘 <see cref="CombatUnitActionActor.None"/>이 아닙니다.</summary>
        internal CombatUnitActionActor Actor { get; }

        /// <summary>어디로 가는지입니다.</summary>
        internal CombatUnitActionPlace Place { get; }

        /// <summary>기준점에서 떨어지는 거리입니다(칸 비율, 0~<see cref="MaxDistance"/>). 제자리면 쓰지 않습니다.</summary>
        internal float Distance { get; }

        /// <summary>옮기는 모양입니다. 뛰기의 높이는 걸음 모양 설정의 뛰기 높이를 같이 씁니다(계획 TP-21).</summary>
        internal CombatStepLook Look { get; }

        /// <summary>나가는 시간(제자리면 도는 시간)입니다. 0이면 바로 그 자리입니다.</summary>
        internal float OutSeconds { get; }

        /// <summary>사건 목록 안 발동에서 머무는 시간입니다. 공격 반응(가로채기)은 쓰지 않습니다.</summary>
        internal float HoldSeconds { get; }

        /// <summary>돌아오는 시간입니다. 0이면 바로 사실 자리입니다.</summary>
        internal float ReturnSeconds { get; }

        /// <summary>머무는 동안 볼 쪽입니다.</summary>
        internal CombatUnitActionFace Face { get; }
    }
}
