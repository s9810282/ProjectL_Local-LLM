using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Field
{
    /// <summary>
    /// 공격 궤적을 필드에 그리는 좁은 계약입니다(공격 궤적 계획 P7). 구현: <see cref="FieldView"/>.
    /// 궤적 구성 쪽은 좌표 변환과 이 두 동작만 알고, 메시·핸들·점선 규칙은 모릅니다.
    /// </summary>
    internal interface ICombatTrajectoryCanvas
    {
        /// <summary>현재 필드 좌표 변환입니다. 필드를 구성하기 전에는 null입니다.</summary>
        FieldGridMapper Mapper { get; }

        /// <summary>
        /// 경로를 궤적으로 그립니다. 점은 필드 셀 루트 로컬 좌표이고, 배열 앞쪽 <paramref name="count"/>개를 씁니다.
        /// 막힘이면 막힘 색과 끝점의 표지로 그립니다. 궤적 부품이 없는 필드(재질 없는 설정)에서는 아무것도 하지 않습니다.
        /// </summary>
        void ShowAttackTrajectory(Vector3[] localPath, int count, bool blocked);

        /// <summary>궤적을 숨깁니다. 여러 번 불려도 안전합니다.</summary>
        void ClearAttackTrajectory();
    }
}
