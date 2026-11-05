using System.Collections.Generic;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>
    /// 넉백 표지 종류입니다. 그리는 쪽에서는 색만 가릅니다. Core 멈춤 이유를 모르는 필드 쪽 형식입니다(넉백 미리보기 설계 KB1).
    /// </summary>
    internal enum FieldKnockbackMarkKind
    {
        /// <summary>밀리는 대상(다 가거나 중간에 멈춤)입니다.</summary>
        Push = 1,
        /// <summary>막혀서 덜 가거나 못 가는 대상입니다.</summary>
        Blocked = 2,
        /// <summary>넉백 면역이라 안 움직이는 대상입니다.</summary>
        Immune = 3
    }

    /// <summary>
    /// 필드 셀 루트 로컬의 넉백 표지 하나입니다. 위치의 높이는 대상 점유 칸과 화살표가 걸치는 칸 표면 중 가장 높은 값이고(결정 Q-KBb3 A),
    /// 띄움 높이는 그리는 쪽이 더합니다(계획 TP-5).
    /// </summary>
    internal readonly struct FieldKnockbackMark
    {
        internal FieldKnockbackMark(Vector3 localPosition, Quaternion localRotation, FieldKnockbackMarkKind kind)
        {
            LocalPosition = localPosition;
            LocalRotation = localRotation;
            Kind = kind;
        }

        internal Vector3 LocalPosition { get; }

        internal Quaternion LocalRotation { get; }

        internal FieldKnockbackMarkKind Kind { get; }
    }

    /// <summary>
    /// 넉백 미리보기(도착 칸 층과 화살표)를 필드에 그리는 좁은 계약입니다(넉백 미리보기 설계 KB1, 계획 TP-1). 구현: <see cref="FieldView"/>.
    /// 구성 쪽은 좌표 변환과 이 두 동작만 알고, 어느 층에 칠하는지·인스턴스·색 핸들은 모릅니다.
    /// </summary>
    internal interface ICombatKnockbackCanvas
    {
        /// <summary>현재 필드 좌표 변환입니다. 필드를 구성하기 전에는 null입니다.</summary>
        FieldGridMapper Mapper { get; }

        /// <summary>
        /// 도착 칸 층과 표지를 함께 바꿉니다. 빈 목록이면 지운 것과 같습니다. 필드를 구성하기 전이면 아무것도 하지 않습니다.
        /// 표지 부품이 없는 필드(화살촉 재질 없는 설정)에서는 도착 칸만 칠합니다.
        /// </summary>
        void ShowKnockbacks(IReadOnlyList<CellCoord> destinations, IReadOnlyList<FieldKnockbackMark> marks);

        /// <summary>도착 칸 층과 표지를 지웁니다. 여러 번 불려도 안전합니다.</summary>
        void ClearKnockbacks();
    }
}
