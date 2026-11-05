using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;
using UnityEngine;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts
{
    /// <summary>
    /// 입력 중 초안과 견적만 표시하는 계약입니다. 이동 범위 표시(<see cref="ICombatMovementRangeFeedback"/>)와
    /// 이동 범위 표시 계약을 상속합니다.
    /// </summary>
    internal interface ICombatMovementFeedback : ICombatMovementRangeFeedback
    {
        // path는 확인된 표시 경로, candidate는 경로에 편입하지 않은 후보/거절 칸이다.
        // 미응답 예상값은 null이며 포인터 무효 여부는 Core 예상값과 별도로 표시한다.
        void ShowDraft(MovementPath path, MovePreview? preview, bool pointerValid, CellCoord? candidate = null);
        void ClearDraft();
        /// <summary>실제 초안 편집의 표시 수명입니다. Q의 시작 칸 대기에서는 호출하지 않습니다.</summary>
        void BeginMovementPlanning(EntityId actor, EntityKey key, Rotation4 rotation);
        /// <summary>연속 커서 위치의 null은 일시 숨김입니다. valid의 null은 견적 대기 중립색입니다.</summary>
        void UpdateMovementPointer(Vector2? screenPosition, bool? valid, bool frozen = false);
        void EndMovementPlanning();

        /// <summary>Q로 경로를 지정하는 중인지입니다(시작 칸 대기 포함, 확정·취소·이탈 때 false). 값이 바뀔 때만 부릅니다.</summary>
        void SetPathPlanning(bool planning);
    }
}
