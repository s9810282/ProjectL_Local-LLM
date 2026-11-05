# 증강 선택 화면 UI — 현강님 공유 (2026-10-29, 호진, 미전달)

> **대신함(2026-11-02):** 화면 구조·계약·프리팹 설명은 [공통 화면 UI 공유](2026-11-02-stage-offer-ui-share-Hojin.md)가 대신한다. `UIId.TraitGrowth`와 Canvas 배치만 이 문서 그대로다.

- 작성자: 호진(이호진)
- 설계: [증강 설계](../specs/2026-10-28-trait-growth-design-Hojin.md)(TG1~TG10, 옛 강화 설계 §14, 10-29 분리), 계획 [증강 계획](../plans/2026-10-28-trait-growth-plan-Hojin.md)(TG-a·TG-d, 옛 강화 계획 §5).
- UISystem 폴더 사용은 호진님이 허락했습니다(10-28). 현강님 규칙(`Assets/AGENTS.md`)대로 `UIBase` 등록, `UIInputBinding`·`IUIInputReceiver`, onClick 없음으로 만들었습니다.
- 상태: Unity 재생 확인 문제 없음(호진, 10-29), 커밋됨(`24c21b3a3`).

## 1 더한 것 (UISystem 폴더)

- `UIIDs.cs`: `TraitGrowth = 42`(10-29 `feature/JMH/Effect` 병합 때 41을 현강님 `TurnDisplay`가 먼저 써서 42로 옮김)(레벨업 특성 성장 = 증강 선택 화면).
- 새 `Screens/Stage/TraitGrowthUI.cs`(+ `.meta`)
  - `UIBase`, ID `TraitGrowth`, ParentID `None`, Policy **Parallel**(생성자에서 정함).
  - Exclusive로 두면 열 때 상점·회복·덱 편집을 그 Scope를 거치지 않고 닫아 버리고, 반대로 그쪽이 열릴 때 증강 화면이 닫혀 서로 닫기를 되풀이합니다.
  - 루트는 늘 켜 두고 자식 Panel만 열고 닫습니다(덱 편집 `DeckEditUI`와 같은 꼴).
  - 카드는 공용 `StageOfferCardView` 원본(`StageEventOfferDescriptionCard.prefab`)을 복제해 재사용합니다(`StageOfferPopupUI`의 카드 배치와 같은 식, 상속은 하지 않음).
  - 버튼 슬롯: 계속 −1, 숨김 −2, 다시 열기 −3, 카드 0~. 열릴 때 EventSystem 초점을 첫 카드(결과면 "계속")에 둡니다.
  - 다시 열기 버튼은 Panel 밖에 있어 숨긴 동안에만 보입니다. 이 버튼의 수신자는 별도 계약 `ITraitGrowthReopenInput`입니다(10-29 고침 — 처음에는 `Action` 대리자였는데 `Assets/AGENTS.md`의 "명시적 수신 계약"에 맞췄습니다).
  - 필수 참조 검사는 `TraitGrowthUI.Validate()` 하나입니다. `Awake`는 빠진 것이 있으면 준비만 미루고, 첫 사용과 아래 구성 검사가 같은 `Validate`를 부릅니다(10-29 고침).
- 새 계약 `Screens/Stage/ITraitGrowthPopupDisplay.cs`·`ITraitGrowthPopupInput.cs`·`ITraitGrowthReopenInput.cs`(10-29), 화면 값 `TraitGrowthPopupModel.cs`(+ `.meta`).
- `StageMapCanvasView.cs`: 직렬화 칸 `traitGrowth`와 읽기 속성 `TraitGrowth` 하나씩. 10-29부터 `ValidateComposition`이 `traitGrowth` 연결·같은 씬을 보고 `traitGrowth.Validate()`를 부릅니다(처음에는 검사 줄을 넣지 않았다가 UI 규약 검사 뒤 넣음).

## 2 프리팹·씬

- 에디터 메뉴 `ProjectL/Stage/Ensure StageSelect Trait Growth UI`(새 `Assets/ProjectL/Editor/Stage/StageTraitGrowthUISetup.cs`)가 묶음 프리팹 `Prefabs/UI/TraitGrowth/TraitGrowthUI.prefab`을 만들고, StageSelect 씬의 공통 Canvas의 마지막 자식으로 놓아 `StageMapCanvasView.traitGrowth`에 잇습니다. 이미 있으면 유지합니다.
- 2026-10-30 통합: 프리팹 자체 Canvas·CanvasScaler·GraphicRaycaster는 제거했습니다. StageSelectCanvas(4)의 마지막 자식으로 배치하며 설정 창(99) 아래입니다. 같은 날 덱도 공통 Canvas로 통합됐으며 Canvas 내부 정렬은 형제 순서를 따릅니다.
  - 처음에는 110(덱 편집 100 위)이었는데, 검토에서 설정 창이 입력 Layer를 덮어쓴 동안 증강 팝업이 열리거나 눌리면 `PopupInputSession`이 Layer를 못 잡아 멈추거나, 팝업이 설정 창 뒤를 가린 채 닫혀 맵 입력이 죽는 경로가 나와서 내렸습니다.
  - **참고(보고만):** 덱 편집 Canvas(100)도 설정 창(99)보다 위라 같은 "설정 창이 팝업 뒤에 가려진 채 팝업을 닫으면 Layer 복귀가 빠짐" 경로가 있을 수 있다고 검토가 짚었습니다. 확인 부탁드립니다.
- 지금 프리팹 모양은 도구가 만든 임시입니다. 버튼·아이콘·배치 같은 UI 다듬기는 나중에 더할 예정입니다(호진 10-29). 고정 글자("계속"·"숨김"·"증강 선택")는 프리팹 안에 있고, 바뀌는 글자는 View의 `TraitGrowthPopupText`에 있습니다.

## 3 확인할 점

- 키보드·컨트롤러 자동 Navigation이 팝업 뒤 버튼으로 넘어갈 수 있다는 지적이 있었습니다(확인 필요). 카드·계속·숨김에 명시적 Navigation을 둘지 UI 다듬을 때 같이 정하면 좋겠습니다.
