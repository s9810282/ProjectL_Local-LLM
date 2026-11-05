using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Ids;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.UiEntry
{
    /// <summary>
    /// 가방 UI가 아이템 장착 입력을 전투 View에 넘기는 입구입니다(가방 설계 P4). 손패 카드의 <see cref="ICombatDeckPlacementInput"/>과 같은 꼴입니다.
    /// </summary>
    /// <remarks>집을 수 있는지와 놓을 대상은 전투 입력이 판단하고, 장착 가부는 Core가 판정합니다.</remarks>
    public interface ICombatItemInput
    {
        /// <summary>지금 그 아이템을 집을 수 있는지입니다(배치 국면·대기 없음·재고 1 이상).</summary>
        bool CanSelect(int itemKey);

        /// <summary>그 아이템을 집습니다. 놓기·취소는 전투 입력의 포인터 입력이 처리합니다.</summary>
        void Select(int itemKey);

        /// <summary>집은 아이템을 놓지 않고 내려놓습니다. 집지 않았으면 아무것도 하지 않습니다.</summary>
        void Cancel();

        /// <summary>
        /// 그 유닛의 장착 아이템을 해제합니다(가방 설계 P6, 유닛 HUD 아이템 아이콘 우클릭). 배치 국면·대기 없음·필드의 아군 메인·서브·장착 칸 있음일 때만
        /// 제출하고, 아니면(적 포함) 아무것도 하지 않습니다.
        /// </summary>
        void RequestUnequip(EntityId unitId);
    }
}
