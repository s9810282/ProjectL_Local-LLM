using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.UiEntry
{
    /// <summary>덱 카드의 정보용 Hover입니다. Empty는 이전 Arrow 해제이며 배치 입력 허용과 독립입니다.</summary>
    public interface ICombatDeckHoverInput
    {
        void Hover(CharacterInstanceId ownedId);
    }
}
