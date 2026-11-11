# 배치 진입 안내·카드 자동 등장 조립 변경 공유 — 2026-11-07

상태: 코드·씬 연결 완료, 컴파일 미확인·Unity 확인 대기. 외부 미전달.

- 설계/Task 원본: [설계](../specs/2026-11-07-deployment-deck-entry-design.md), [계획](../plans/2026-11-07-deployment-deck-entry-plan.md).
- CombatSceneDisplayFactory가 기존 CombatBoardEntryFactory의 결과와 배치 UI 표시를 CombatEntryPresentation으로 조합한다. CombatSceneRoot의 새 deckCards 참조는 Combat.unity의 기존 DeckCardList 인스턴스로 연결했다.
- GameBootstrap은 Deck 루트의 준비와 공통 Combat UI만 맡고 UnitCard를 즉시 열지 않는다. 맵→TurnDisplay의 배치 안내→카드 완료까지 기존 ICombatEntryPresentation.IsPending을 유지한다. CombatPlan·입력 Policy·결과 순번 계약 변경 없음.
- UI 서비스 생성/등록·Export는 기존 GlobalSystemFactory / GlobalSystemGroup.UIServiceCommand, 소비는 CombatDisplayServices.UI 및 UIBase 바인딩 경로다. 새 루트 서비스 등록·UIId·Framework 모듈·전역 이벤트 없음.
- UnitCard는 기존 Parent Deck인 UIGroup을 유지하고 정책만 Parallel로 바꿨다. 배치 중 필드 정보창이 열려도 카드를 유지한다. 외부 개폐는 IUISystemService를 사용한다. DeckButton/CloseDeck 오브젝트는 삭제하지 않고 비활성화만 했다.
- TurnDisplayUI는 ShowDeployment·IsShowing·HasCompleted를 제공한다. 기존 플레이어/적 턴 알림은 그대로이며 배치 진입 호출자만 실제 정상 종료를 기다린다. 카드 애니메이션은 화면 내부 부품이고 별도 서비스가 아니다.
- Core·FieldCamera·Architecture·공용 UISystem 구현은 수정하지 않았다. Test 코드·MSBuild·Unity 실행·커밋·push 없음.
- 사용자가 튜토리얼 순서 연결은 별도 진행으로 지정했다. 1-1의 기존 로딩 종료 후 설명 시작과 카드 강제 열기가 진입 연출에 겹칠 수 있는 점을 후속으로 인계한다.
