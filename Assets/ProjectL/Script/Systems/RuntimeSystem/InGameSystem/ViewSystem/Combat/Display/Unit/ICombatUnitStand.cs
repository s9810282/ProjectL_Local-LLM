using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Unit
{
    /// <summary>
    /// 특성 발동의 유닛 행동이 옮긴 서는 자리 대여 하나입니다(유닛 행동 설계 AA2). 자세 대여(<see cref="ICombatUnitPose"/>)의 아래 층입니다.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 빌린 동안 사실 그리기는 그 유닛의 위치·방향을 쓰지 않고 이 대여가 씁니다. 자세 대여는 빌릴 때의 자리를 기억하므로, 서는 자리 위에서 피격 반응이
    /// 자세를 빌리면 옮긴 자리에서 맞고 옮긴 자리로 돌아옵니다. 그래서 서는 자리는 자세가 비어 있을 때만 움직입니다(<see cref="CanMove"/>).
    /// </para>
    /// <para>
    /// 되돌리기는 빌릴 때 자리(<see cref="HomePosition"/>·<see cref="HomeRotation"/>)로 갑니다(자세 대여와 같은 규칙, 계획 TP-17). 사망 모델 유지 중이면 쓰지 않습니다(죽은 유닛은 돌아오지 않음, 설계 P8).
    /// 걸음 배치가 시작되면 창구가 접습니다(설계 AA8 안전장치). 접힌 대여는 쓰지도 되돌리지도 않습니다.
    /// </para>
    /// <para>
    /// 없어져도 되는 칸입니다(묶음 끝 검토 H1). 표시가 꺼지거나 풀로 돌아가도 빌린 실행을 멈추지 않고 대여만 끝냅니다. 그 뒤 이 대여는 쓸 수 없고(<see cref="IsAvailable"/> 거짓),
    /// 그것을 쓰는 조각은 바로 끝납니다.
    /// </para>
    /// </remarks>
    internal interface ICombatUnitStand
    {
        /// <summary>아직 같은 표시의 지금 대여이고 표시가 켜져 있는지입니다. 접혔거나 끝났으면 false입니다.</summary>
        bool IsAvailable { get; }

        /// <summary>그 유닛에 자세 대여가 없어 서는 자리를 움직여도 되는지입니다. 자세 대여 위에서 움직이면 자세가 되돌릴 자리와 어긋납니다.</summary>
        bool CanMove { get; }

        /// <summary>빌릴 때 자리(위치)입니다. 되돌리기와 돌아오기의 끝 자리입니다.</summary>
        Vector3 HomePosition { get; }

        /// <summary>빌릴 때 방향입니다.</summary>
        Quaternion HomeRotation { get; }

        /// <summary>지금 서는 자리(위치)입니다. 마지막으로 쓴 값이고, 처음은 빌릴 때 자리입니다.</summary>
        Vector3 Position { get; }

        /// <summary>지금 서는 방향입니다.</summary>
        Quaternion Rotation { get; }

        /// <summary>서는 자리를 씁니다. 쓸 수 없거나(<see cref="IsAvailable"/>) 자세 대여가 있으면(<see cref="CanMove"/>) 예외입니다.</summary>
        void ApplyStand(Vector3 position, Quaternion rotation);

        /// <summary>대여를 끝내고 빌릴 때 자리로 되돌립니다. 지난·접힌 대여거나 사망 모델 유지 중이면 쓰지 않습니다. 반복 호출해도 안전합니다.</summary>
        void RestoreStand();
    }
}
