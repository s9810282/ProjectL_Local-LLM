namespace ProjectL.Systems.RuntimeSystem.ErrorHandling
{
    /// <summary>
    /// 씬 Loading Node 순서의 원본입니다(골격 점검 설계 K-4). 같은 단계(Enter·Exit) 안에서는 작은 값부터 돕니다.
    /// 기준 값(<see cref="LifeCycle"/>·<see cref="BindSceneUI"/>·<see cref="BindCameras"/>·<see cref="OpenSceneUI"/>)은 숫자로,
    /// 그 기준에 붙는 값("무엇보다 앞·뒤")은 식으로 적어, 기준을 옮기면 붙은 값도 따라 옮겨집니다.
    /// 기준 값끼리의 관계(예: 화면 열기는 View 연결 뒤)는 숫자 간격으로만 지켜지므로, 기준 값을 옮길 때 함께 봅니다.
    /// </summary>
    /// <remarks>
    /// 범위는 Bootstrap의 씬 연결 노드(세 씬, 덱 카드 준비는 Combat만)와 Combat 씬 시스템(전투 View·튜토리얼)의 노드입니다.
    /// Stage·Lobby 시스템의 자기 노드(이탈 준비·View 연결·해제·카메라 핸들)는 각 시스템의 상수입니다.
    /// <see cref="LifeCycle"/>(0)은 기반(<c>LifeCycleBase</c>)이 정한 값을 따라 적은 것입니다. 기반 값이 바뀌면 이 표도 맞춰야 합니다.
    /// 전체 순서 표와 설명은 viewDocs/runtime-scene-binding-guide.md §1.1에 있습니다.
    /// </remarks>
    internal static class SceneNodeOrder
    {
        /// <summary>LifeCycle의 Enter·Exit입니다(기반 값). <c>OnEnter</c>·<c>OnExit</c>가 여기서 돕니다.</summary>
        internal const int LifeCycle = 0;

        // Enter

        /// <summary>UI 참조 다시 연결입니다. LifeCycle 진입과 View 연결 전입니다.</summary>
        internal const int BindSceneUI = -200;

        /// <summary>씬 가상 카메라 수집입니다. View가 카메라 핸들을 빌리기 전입니다. 효과도 같은 씬 수명에서 정리합니다.</summary>
        internal const int BindCameras = -190;

        /// <summary>Combat 씬 루트 찾기·배경 맵 세우기입니다. 카메라 수집보다 앞이라 그 수집에 맵 카메라가 들어갑니다(배경 설계 BD1).</summary>
        internal const int CombatBackdrop = BindCameras - 5;

        /// <summary>전투 View 연결입니다. CombatLifeCycle의 Enter(수락된 진입 활성화) 뒤입니다.</summary>
        internal const int CombatViewAttach = LifeCycle + 100;

        /// <summary>전투 덱 루트를 열고 자식 패널은 닫습니다. 전투 View의 최초 Render보다 먼저 초기화합니다.</summary>
        internal const int PrepareCombatDeck = CombatViewAttach - 10;

        /// <summary>튜토리얼 표시 연결입니다. 전투 View가 전투를 붙인 뒤입니다.</summary>
        internal const int TutorialAttach = CombatViewAttach + 10;

        /// <summary>씬별 화면 열기입니다. LifeCycle 진입과 View 연결이 끝난 뒤입니다.</summary>
        internal const int OpenSceneUI = 200;

        // Exit

        /// <summary>전투 View 해제(씬 연결 끊기·배경 맵 해제)입니다. LifeCycle의 Exit 전입니다.</summary>
        internal const int CombatViewRelease = LifeCycle - 100;

        /// <summary>
        /// 튜토리얼 표시 해제입니다. 전투 View 해제 전에 마감해, 빌린 입력을 전투 View가 Layer를 바꾸기 전에 돌려줍니다.
        /// </summary>
        internal const int TutorialDetach = CombatViewRelease - 10;
    }
}
