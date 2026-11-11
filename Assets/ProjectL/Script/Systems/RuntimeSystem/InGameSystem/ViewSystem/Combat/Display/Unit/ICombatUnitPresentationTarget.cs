using System;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 유닛 표시 하나가 창구(<see cref="CombatUnitPresentationWindow"/>)에 주는 것입니다(연출 조립 설계 B13 보완 4). <see cref="FieldUnitView"/>가 구현합니다.
    /// </summary>
    /// <remarks>
    /// 대여의 수명 규칙은 구현이 지킵니다(대여 칸 <see cref="UnitPresentationLeaseSlot{TLease}"/>). 늦은 복원은 대여 객체 참조로 막고, 표시가 꺼지거나 다른 ID로 다시 대여되면
    /// 자세 → 서는 자리 → 크기 → 플래시 → 클립 순서로 되돌리며 취소를 알립니다(서는 자리는 취소 없이 내려놓음, B14 O4 동등성의 근거).
    /// </remarks>
    internal interface ICombatUnitPresentationTarget
    {
        EntityId EntityId { get; }

        EntityKey EntityKey { get; }

        /// <summary>켜져 있고 반응 수명을 쓸 수 있는지입니다.</summary>
        bool IsActive { get; }

        /// <summary>자세 대여가 남아 있는지입니다.</summary>
        bool HasPose { get; }

        /// <summary>지금 표시 회전입니다.</summary>
        Quaternion Rotation { get; }

        /// <summary>위치·회전 자세 대여를 시작합니다. 이전 자세가 남아 있거나 꺼진 표시면 예외입니다.</summary>
        ICombatAttackUnitPose BeginPose(Action cancelled);

        /// <summary>크기 배율 대여를 시작합니다. 이전 크기가 남아 있거나 꺼진 표시면 예외입니다.</summary>
        ICombatUnitScale BeginScale(Action cancelled);

        /// <summary>걸음 보간 위치를 씁니다.</summary>
        void SetPlacementPosition(Vector3 position);

        /// <summary>위치·체력·활성 상태를 건드리지 않고 모델 방향만 바꿉니다.</summary>
        void ApplyFacing(Quaternion rotation);

        void SetMovementOpacity(float opacity);

        void ClearMovementOpacity();

        /// <summary>모델 외형 층(피격 플래시)을 쓸 수 있는지입니다(조각·조립 설계 BP9).</summary>
        bool SupportsHitFlash { get; }

        /// <summary>피격 플래시 층 대여를 시작합니다. 표시가 꺼지거나 다시 대여되면 취소되고 <paramref name="cancelled"/>를 부릅니다.</summary>
        ICombatUnitFlash BeginHitFlash(Action cancelled);

        /// <summary>사건 클립 채널을 쓸 수 있는지입니다(클립 설계 AC3·AC9). 이벤트 컴포넌트가 붙은 표시만 true입니다.</summary>
        bool SupportsClip { get; }

        /// <summary>사건 클립 대여가 남아 있는지입니다.</summary>
        bool HasClip { get; }

        /// <summary>사건 클립 대여를 시작합니다. 이전 대여가 남았거나 꺼진 표시면 예외입니다. 표시가 꺼지거나 다시 대여되면 취소되고 <paramref name="cancelled"/>를 부릅니다.</summary>
        ICombatUnitClip BeginClip(Action cancelled);

        /// <summary>서는 자리 대여가 남아 있는지입니다(유닛 행동 설계 AA2).</summary>
        bool HasStand { get; }

        /// <summary>
        /// 서는 자리 대여를 시작합니다(자세 대여의 아래 층). 이전 대여가 남았거나 꺼진 표시면 예외입니다. 표시가 꺼지거나 다시 대여되면 취소되고 <paramref name="cancelled"/>를 부릅니다.
        /// <paramref name="keepsWhereItIs"/>가 true를 답하는 동안(사망 모델 유지)은 되돌리기가 자리를 쓰지 않습니다.
        /// </summary>
        ICombatUnitStand BeginStand(Action cancelled, Func<bool> keepsWhereItIs);

        /// <summary>서는 자리 대여를 자리를 쓰지 않고 끝냅니다(접기, 설계 AA8). 취소 콜백은 부르지 않습니다. 대여가 없었으면 false입니다.</summary>
        bool FoldStand();

        /// <summary>서는 자리 대여가 있으면 지금 서는 자리입니다.</summary>
        bool TryGetStand(out Vector3 position, out Quaternion rotation);

        /// <summary>붙은 곡사 무기입니다. 없으면 null입니다.</summary>
        CatusCannonWeaponView MortarWeapon { get; }
    }
}
