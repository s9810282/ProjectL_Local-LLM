# UI 작업자 공유: StageSelect 범례

- 작성자: 호진(이호진)
- 작성: 호진, 2026-10-17
- 상태: 구현·검증 완료, **공유용 문서만 작성·외부 전달 안 함**
- 결과·기존 실패의 원본: [스테이지 맵 계획의 범례 결과](../plans/2026-10-10-stage-map-plan-Hojin.md)(옛 범례 완료 기록).

## 추가한 화면

통합 기획서 15슬라이드 5번 범례를 만들었다. 사용자 첨부의 상단 명패에는 `범례`, 종이 본문에는 일반/엘리트/보스/이벤트/상점 스테이지의 아이콘과 텍스트를 각각 넣었다. 배경은 UI 작업자의 기존 `UI_2_textbox`를 Sliced로 사용하며 리소스/Importer는 수정하지 않았다.

실제 표시 원본은 [StageLegendUI.prefab](../../../Assets/ProjectL/Prefabs/UI/StageSelect/StageLegendUI.prefab)이다. 범례 자체의 문구·아이콘·레이아웃은 이 프리팹에서 수정한다. 공용 `GlobalUICanvas.prefab`과 기존 `StageUI.cs`에는 쓰지 않았다.

## 조립·등록을 합칠 때

- 씬은 StageSelect에만 연결한 프리팹 인스턴스 1개다. GlobalUI/DontDestroyOnLoad가 아니다.
- 클래스는 `Screens/Stage/StageLegendUI.cs`, 등록 키는 **기존 `UIId.StageSelect`(4)** 하나다. 다른 화면을 같은 키로 추가 등록하지 않는다. 향후 StageSelect HUD로 합칠 때 이 루트의 역할을 인수하거나 기존 UIGroup 계약으로 함께 조립한다.
- Bootstrap.OpenStageUI의 기존 글로벌 HUD/Deck 표시 뒤에 guard와 `_ui.OpenUI(UIId.StageSelect, true)`를 추가했다. 루트 표시의 서비스 소유권과 Back 이력을 유지한다. 새 System이나 수동 Awake 등록은 없다.
- Canvas order는 100이다. Shop/Recovery 110, 글로벌 UI 999보다 아래다. 범례를 팝업 앞에 그리도록 순서를 바꾸지 않는다.
- 배경만 Raycast Target이다. 기존 StageMapPointerBlocker로 그 영역에서 시작한 맵 선택/드래그를 막는다. 버튼·Submit/Shortcut·Layer·입력 구독은 추가하지 않았다.

## 항목을 더 넣을 때

프리팹 `Frame/Entries`의 행을 복제해 `Icon`의 Sprite와 `Label`의 텍스트를 바꾼다. 아이콘은 실제 노드와 같은 Sprite 참조를 사용하고 preserveAspect를 유지한다. 제목은 `Frame/Title`의 명패 영역에 둔다.

현재 행 높이는 42, 간격은 6이다. N개 행의 Entries 높이는 `42×N + 6×(N-1)`로 맞추고 Frame은 상단 90/하단 40 여백을 포함해 늘린다. 현재 N=5에서 Entries=234, Frame=364다. 이는 프리팹 저작 안내이며 런타임에 새 계산기나 상태 캐시를 만들지 않았다.

현재 Core에는 엘리트용 StageType/MapRole이 없어서 이번에는 범례 설명만 있다. 엘리트 실제 노드·새 이벤트 규칙은 그 계약을 인수하는 작업과 별개다. 노드 아이콘을 다른 리소스로 바꿀 때 범례와 실제 네 NodeVisual의 교차 검사가 불일치를 알려준다.

## 검증 인계

- 새 범례 4/4통과. 실제 Boot 표시·패널 가림·바깥 드래그·로비 왕복, 유지 루트/Back/HideAll, 명패/본문과 실제 아이콘 참조 확인.
- 1920×1080, 1280×720, 1280×960의 Unity 렌더 확인.
- 관련 98건 중 97통과, 변경 전부터 있던 GlobalUITests의 `IGlobalSystemGroupService` 누락 실패 1건 유지. 전체 suite 통과로 인계하지 않는다.
- Core/Camera/Framework 코드·기존 UI 원본 리소스·다른 작업자 문서 변경 없음. HojinCombat 문서의 병행 갱신은 되돌리지 않음.
- 임시 저작 도구 제거 완료. 결과 문서와 본 메모 외에 별도 발송·커밋은 하지 않았다.
