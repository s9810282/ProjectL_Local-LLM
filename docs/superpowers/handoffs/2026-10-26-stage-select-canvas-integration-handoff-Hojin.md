# StageSelect Canvas 통합 인계 — 2026-10-26

- 작성자: 호진(이호진) — 본문 초안은 호진 요청으로 Codex가 씀

> 후속: 사용자가 컴파일 오류 해소를 확인했으나 덱 열기 버튼이 글로벌 이미지에 가려 클릭되지 않았다. 현재 변경 방향과 상태는 [스테이지 맵 계획의 독립 덱 Canvas·UI 소유권 결과](../plans/2026-10-10-stage-map-plan-Hojin.md)(옛 독립 덱 Canvas·UI 소유권 계획)를 따른다. 이 인계의 단일 Canvas/정렬 전환은 이전 상태다.

- 작성: Codex, 사용자 승인 Phase 2 작업. 외부 발송하지 않음.
- 상태: 구현·정적 대조·독립 리뷰 완료, 사용자 화면 확인 대기. 컴파일 미확인, Unity 재생 미확인. (→ 바뀜: 맨 위 후속 — 사용자가 컴파일 오류 해소를 확인했고, 재생에서 덱 열기 버튼이 글로벌 이미지에 가려져 독립 덱 Canvas 후속이 이어졌다.)
- [스테이지 맵 설계](../specs/2026-10-10-stage-map-design-Hojin.md), [스테이지 맵 계획](../plans/2026-10-10-stage-map-plan-Hojin.md)(둘 다 2026-10-27에 옛 Canvas 통합 설계·Task 계획을 합친 문서).

## 씬과 등록

```text
StageSelectCanvas (Canvas/Scaler/Raycaster 각 1개)
├─ StageSelectPanel (UIId.StageSelect, 고정 Parallel UIGroup)
│  ├─ StageLegendUI (그룹 멤버만)
│  └─ DeckEditUI (독립 등록 자식, ParentID.StageSelect)
│     ├─ OpenButton (초기 활성)
│     └─ Panel (초기 비활성)
└─ StageScreenRoot (UIId.StageScreenRoot=40, 고정 Parallel UIBase)
   ├─ ShopUI (독립 등록 자식)
   └─ RecoveryUI (독립 등록 자식)
```

실제 UIId 등록 항목1개와 화면 파생체를 추가했다. UISystem/UIBase/UIGroup/PopupViewScope/PopupInputSession 공용 구현은 그대로다. UI 멤버의 별도 수동 등록이나 외부 SetActive, Button.onClick/UnityEvent 우회는 없다. 기존 Global Context → UIServiceCommand → StageMapViewSystem → Scope/Factory 경로를 사용하고 서비스·Builder·상위 Export를 추가하지 않았다.

StageMapSceneRoot의 필수 참조: stageSelectPanel, stageScreenRoot, uiCanvas, deckEditUI. 설정 원본은 normalUiOrder=4, deckEditingUiOrder=100이다. 그룹 initialInstances는 범례만 연결한다. GlobalUICanvas는 별도이며 원본 Overlay order5/layer0을 유지한다.

## 표시·복귀와 종료

부모를 서비스로 연 호출이 완료된 뒤 표시 수명을 발급한다. 맵 준비와 기존 팝업 복원을 마친 뒤 최초 선택 UI를 연다. 초기 Open은 Bootstrap에서 Scope로 이전했고 글로벌 표시 호출은 보존했다.

팝업 열림은 현재 IsOpen을 확인해 같은 호출에서 선택 패널을 숨긴다. 내부 정상 완료와 외부 Close/Back/Exclusive 교체는 복귀 예약만 만든다. 실제 복귀는 팝업 입력 반환 후 맵 준비·현재 Scope·같은 부모/표시 수명·Selection Layer·다른 팝업/새 요청/이동을 대조한 뒤 수행한다. 내부 오류·문맥 불일치 닫힘은 복귀를 요청하지 않는다.

부모 Hidden은 권한 철회만 담당한다. HideAll 이후 늦은 상점 결과는 소비하되 화면을 다시 열지 않는다. 부모가 닫히면 자동 복원/새 열기도 중단한다. mapChanged는 표시 복귀 신호가 아니다. 동일 팝업 외부 재열기는 기존 알림 관찰 경로를 사용한다. 서로 다른 모달을 강제 연속 Open하는 사용법의 기존 입력 반환 순서는 이번 작업이 확대 지원하지 않는다.

전투는 기존 Observer의 준비·결과 검증 이후 IStageMapEntryPresentation 경계에서 선택 표시를 닫는다. 준비 전 거절/취소는 숨김 경계에 오지 않는다. 정상 씬 이탈/오류는 Scope를 정리한다. 정렬 구독·원래 값 복원은 다른 정리 콜백보다 먼저 실행한다.

## UI 작업자에게 필요한 변경

원본4개 UI 프리팹은 이제 부모 Canvas가 필요한 패널이다. Canvas/Scaler/Raycaster를 루트에서 제거하고 루트 RectTransform만 full stretch/local identity로 맞췄다. CanvasRenderer, 카드/버튼/아트와 GUID는 유지했다. 다른 프리팹에서 재사용할 때에는 부모 Canvas를 연결한다. StageScreenRoot 아래에는 향후 기능이 준비된 스테이지 UI만 독립 자식으로 등록한다. 강화 기능/빈 화면은 추가하지 않았다.

## 기존 시험 호환 수정

새 시험/시험 도우미를 만들거나 실행하지 않았다. 아래 기존 조립의 새 필수 부모·Canvas·참조만 맞췄다.

- StageMapViewSystemTests.cs, RecoveryStageMapViewTests.cs, CombatEntryIntegrationTests.MapMovement.cs, DevelopmentNavigationIntegrationTests.cs: 기존 Setup/생성자/Root 조립에 실제 UI 부모·서비스 연결.
- RecoveryUITests.cs, ViewPopupCompositionTests.cs: Recovery의 새 ParentID 때문에 실제 부모를 기존 조립에 등록·개방.
- RecoverySceneConnectionTests.cs, StageShopUIAssetTests.cs: 공통 부모 Canvas·새 등록 부모로 에셋 조립 변경.
- StageLegendBootSmokeTests.cs, StageMapRecoveryEntryBootSmokeTests.cs: 기존 개별 Canvas 대신 부모 Canvas 조회 및 범례 타입 조회.
- StageLegendUITests.cs: 제거된 개별 Canvas 정렬만 단언하던 사례1개 제거.

## 정적 대조와 사용자 화면 확인 범위

정적: UIId40 유일, 부모/멤버 관계, 씬 fileID와 meta GUID, Canvas1/중첩0/EventSystem1, 내부 참조·override 보존, 보호 파일 해시, UTF-8/LF와 diff 공백 검사. 실제 컴파일·실행·재진입 실패 주입 결과와 구분한다.

사용자 화면 확인 범위는 선택 화면의 범례/버튼, 덱 편집 열기/닫기/Back과 order복원, 상점/회복 진입·정상 복귀·복원 진입, 전체 숨김 뒤 늦은 결과의 재표시 방지, 전투 왕복 및 화면 비율/글로벌 UI 겹침이다. 옵션 World Space Canvas까지 숫자 정렬만으로 보존됐다고 확정하지 않는다.

Core/FieldCamera/Architecture/Stage Job·Plan·Flow 및 Claude 작업 문서는 변경하지 않았다. 기존 사용자 Deck/Recovery 프리팹 변경을 보존했다. 커밋·push 없음.

## 독립 검수와 남은 확인

최종 읽기 전용 리뷰는 Critical 0 / Important 0 / Minor 0이며 Phase 2 결과 보고 가능 판정이다. 컴파일·Unity 재생이나 사용자 인수를 대신하지 않는다. 생성 실패·파괴·재진입은 정적 분기를 검수했으며 실제 주입 실행은 미확인이다.

기존 제약/후속 후보는 두 가지다. 서로 다른 모달의 외부 강제 연속 Open은 입력 반환 완료 순서가 필요하다. 또한 노드 요청을 낸 뒤 이동이 시작되기 전에 Deck을 여는 조건과 공유 입력 순서는 이번 변경 전에도 같아서, 해당 타이밍의 입력 경합 가능성은 기존 후보로 분리했다. 실제 재현은 하지 않았고 이번 Canvas 변경 범위로 수정하지 않았다.
