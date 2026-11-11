
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.Contracts
{
    /// <summary>
    /// 가방 아이템을 끄는 동안 포인터를 따라가는 그림의 표시 의도입니다(가방 설계 P4).
    /// 부르는 쪽은 상태만 말하고, 그림을 어디에 어떻게 그릴지는 가방 표시가 정합니다.
    /// </summary>
    /// <remarks>구현: HUD 표면(<c>CombatHudSurfaces</c>)이 가방에 넘깁니다(씬 표시 나누기 설계 DS4·P6). 모두 멱등적이어야 합니다.</remarks>
    internal interface ICombatItemDragFeedback
    {
        void ShowItemDrag(int itemKey);

        void MoveItemDrag(float screenX, float screenY);

        void HideItemDrag();
    }
}
