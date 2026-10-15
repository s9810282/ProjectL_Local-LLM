using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Combat;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Results;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>입력 중 초안과 견적만 표시하는 계약입니다.</summary>
    public interface ICombatMovementFeedback
    {
        // path는 확인된 표시 경로, candidate는 경로에 편입하지 않은 후보/거절 칸이다.
        // 미응답 예상값은 null이며 포인터 무효 여부는 Core 예상값과 별도로 표시한다.
        void ShowDraft(MovementPath path, MovePreview? preview, bool pointerValid, CellCoord? candidate = null);
        void ClearDraft();
        void SetReturnState(CheckpointReturnPreview preview);

        /// <summary>Q로 경로를 지정하는 중인지입니다(시작 칸 대기 포함, 확정·취소·이탈 때 false). 값이 바뀔 때만 부릅니다.</summary>
        void SetPathPlanning(bool planning);
    }
}
