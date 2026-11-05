using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.UiEntry
{
    /// <summary>덱 UI에 공개하는 배치 선택 입구입니다. 명령 제출은 기존 배치 입력이 맡습니다.</summary>
    public interface ICombatDeckPlacementInput
    {
        /// <summary>덱·필드 중 어디서 집었든 배치할 유닛을 들고 있는지입니다.</summary>
        bool IsDragging { get; }
        bool CanSelect(CharacterInstanceId ownedId);
        void Select(CharacterInstanceId ownedId);
        void Cancel();
    }
}
