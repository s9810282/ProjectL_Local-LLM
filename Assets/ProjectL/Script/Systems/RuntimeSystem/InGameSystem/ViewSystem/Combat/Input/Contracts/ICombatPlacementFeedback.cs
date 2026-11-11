using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;
using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts
{
    /// <summary>
    /// 배치 Drag 중 고스트와 원본 숨김 표시 의도만 전하는 계약입니다(설계 §5.4·§7, E.2).
    /// </summary>
    /// <remarks>
    /// 옛 <c>ICombatInteractionFeedback</c>을 쓰는 쪽마다 나눈 가운데 배치 Drag 몫입니다(씬 표시 나누기 설계 DS2, 결정 Q-DS2 A).
    /// 구현: 고스트 피드백(<c>CombatPlacementFeedback</c>). 모두 멱등적이어야 하고, 표시 해제 뒤에는 아무것도 하지 않습니다.
    /// </remarks>
    internal interface ICombatPlacementFeedback
    {
        /// <summary>해당 종류의 Drag 고스트를 띄웁니다. 위치는 이어지는 Move 호출이 정합니다.</summary>
        void ShowGhost(EntityKey entityKey);

        void MoveGhostToCell(CellCoord cell);

        /// <summary>칸 밖에서도 고스트가 포인터를 따라가도록 포인터가 가리키는 자리로 옮깁니다.</summary>
        void MoveGhostToPointer(float screenX, float screenY);

        /// <summary>고스트의 배치 가능·불가 색입니다. Core 배치 응답으로 정합니다.</summary>
        void SetGhostPlaceable(bool placeable);

        void HideGhost();

        /// <summary>Drag 중인 원본 View를 숨깁니다(필드에 그 개체가 있을 때만, 유지 덱 카드는 숨기지 않음).</summary>
        void HideOriginal(EntityId entityId);

        void ShowOriginal();
    }
}
