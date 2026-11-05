using ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display.Map;

namespace ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Display
{
    /// <summary>
    /// 전투 씬 표시의 생성 계약입니다(View 2라운드 W6-6). 조정자(CombatViewSystem)는 생성자로 받은 이 계약만 부르고 정적 Factory를 직접 부르지 않습니다.
    /// 운영 구현은 Factory 쪽의 <c>CombatSceneFactory</c> 하나입니다.
    /// </summary>
    internal interface ICombatSceneFactory
    {
        /// <summary>씬 로드 때 전투 배경(스테이지 맵 프랍)을 세울 준비를 합니다.</summary>
        StageViewPropPresenter CreateBackdrop(CombatSceneRoot root, CombatDisplayTables tables);

        /// <summary>씬 루트에 전투 표시를 조립합니다. 배경은 씬 로드 때 같은 루트에 세운 것만 넘깁니다(없으면 null).</summary>
        CombatSceneDisplay CreateDisplay(CombatSceneRoot root, CombatDisplayTables tables, CombatDisplayServices services,
            StageViewPropPresenter backdrop);
    }
}
