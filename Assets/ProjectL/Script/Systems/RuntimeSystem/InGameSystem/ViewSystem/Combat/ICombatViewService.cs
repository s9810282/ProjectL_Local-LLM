using ProjectL.Framework.SystemBase;
using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Input.UiEntry;
using ProjectL.Systems.RuntimeSystem.LifeCycleSystem.LifeCycles.Combat;
namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat
{
    /// <summary>외부 UI가 사용자 확인 뒤 전투 포기를 요청하는 입구입니다. 실제 처리는 기존 전투 진행 흐름이 맡습니다.</summary>
    public interface ICombatAbandonRequestService
    {
        bool TryRequestAbandon();
    }

    /// <summary>
    /// 전투 View가 바깥에 내는 서비스입니다. 바깥에서 실제로 쓰는 것만 둡니다(C 단계 설계 C-7):
    /// 골격의 진행 계약, 다른 작업자 UI(덱·가방·Option)가 부르는 입력 입구.
    /// 씬 연결·해제와 포커스 상실은 로딩 노드·씬 이벤트가 조정자 안에서 부르고, 복귀 입력은 키·버튼이 입력 수신 창구로 바로 들어가므로 계약에 두지 않습니다.
    /// </summary>
    public interface ICombatViewService : IServiceModule, ICombatAbandonRequestService
    {
        /// <summary>
        /// 전투 진행 골격(전투 Plan·사실 읽기)이 보는 작은 계약입니다(개선 설계 §7.1 D1). 골격은 이것만 받고 View 전체 계약에 기대지 않습니다.
        /// </summary>
        ICombatProgressView Progress { get; }

        ICombatDeckPlacementInput DeckPlacement { get; }
        /// <summary>가방 아이템 장착 입력입니다(가방 설계 P4). 씬 연결 전·해제 뒤에는 null입니다.</summary>
        ICombatItemInput ItemInput { get; }
        ICombatDeckHoverInput DeckHover { get; }
    }
}
