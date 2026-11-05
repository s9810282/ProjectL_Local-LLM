using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Movement
{
    /// <summary>
    /// 접촉 계산에 쓰는 몸입니다. 필드 로컬 XZ 평면의 축 정렬 사각형이며 <see cref="Center"/>·<see cref="HalfSize"/>의 y는 필드 로컬 z입니다(히트 타이밍 설계 H4).
    /// 반 크기 0이면 점 몸입니다. 별도 "점 여부" 필드를 두지 않습니다(P7).
    /// </summary>
    /// <remarks>
    /// 모양의 원본은 유닛 외형 프리팹 루트 BoxCollider, 배치의 원본은 그 시점 Core 사실이며 <see cref="FieldUnitPresenter"/>가 둘을 합칩니다.
    /// 접촉 시각은 이 값으로 그때 한 번 파생하고 DB·Buffer에 두지 않습니다.
    /// </remarks>
    internal readonly struct CombatBody
    {
        internal CombatBody(Vector2 center, Vector2 halfSize)
        {
            if (!float.IsFinite(center.x) || !float.IsFinite(center.y))
                throw new ArgumentOutOfRangeException(nameof(center), "몸 중심은 유한해야 합니다.");
            if (!float.IsFinite(halfSize.x) || !float.IsFinite(halfSize.y) || halfSize.x < 0f || halfSize.y < 0f)
                throw new ArgumentOutOfRangeException(nameof(halfSize), "몸 반 크기는 유한한 0 이상이어야 합니다.");
            Center = center;
            HalfSize = halfSize;
        }

        internal Vector2 Center { get; }
        internal Vector2 HalfSize { get; }
        internal bool IsPoint => HalfSize.x == 0f && HalfSize.y == 0f;

        internal static CombatBody Point(Vector2 center) => new(center, Vector2.zero);

        /// <summary>필드 로컬 3D 좌표를 몸 평면(XZ) 좌표로 바꿉니다.</summary>
        internal static Vector2 Flatten(Vector3 local) => new(local.x, local.z);
    }

    /// <summary>
    /// 현재 표시 사실에서 대상의 몸과 외형 키를 찾는 좁은 조회입니다. 표시 사실은 사건 Presenter가 소유하고, 공격 재생은 이 조회만 받습니다(설계 H4).
    /// 대상의 표시 사실을 찾지 못하거나 필드에 없으면 false입니다(몸 조회 실패). 콜라이더가 없는 것은 실패가 아니라 점 몸입니다.
    /// 키는 진단 경고에만 씁니다.
    /// </summary>
    internal delegate bool CombatBodyLookup(EntityId id, out CombatBody body, out EntityKey key);
}
