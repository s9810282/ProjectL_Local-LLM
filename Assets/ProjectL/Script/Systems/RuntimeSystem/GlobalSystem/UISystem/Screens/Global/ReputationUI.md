# GlobalUI 재화 표시 — 2026-10-19

## 설계

- 로비는 Reputation만, StageSelect·Combat은 Gold만 표시한다. Combat 직접 진입도 같은 정책이다.
- ReputationUI는 기존 GlobalUICanvas의 Portrait/Reputation에 붙는 UIBase다. UIId.Reputation(25), ParentID.MainCharactor, Policy.Parallel로 등록한다. 부모는 유지 UI이고 자식은 씬 바인딩 때 재수집·바인딩한다. 유지 Canvas 인스턴스를 재사용하며 씬별 복제는 없다.
- 최초/씬 진입의 UISystem.BindScene이 비활성 Reputation도 수집하고 등록·서비스 연결한다. GameBootstrap의 로딩 완료 콜백 → GlobalUIPresentation → IUISystemService.OpenChildUI/CloseUI로 개폐한다. 실제 활성 변경은 ReputationUI.Open/Hide가 담당하며 부모 종료·Shutdown에서도 자식 정리 경로를 따른다.
- Gold는 기존 MainCharactorUI 내부 위젯이다. goldRoot로 아이콘·금액을 함께 바꾸고 기존 금액 갱신 경로를 재사용한다. 별도 Gold UI를 중복 등록하지 않는다.
- Reputation 금액 원본은 Buffer.FameReadSink.Amount다. 값이 바뀔 때만 TMP를 갱신하고 재바인딩 때 캐시를 비운다. Core·InGame DB·재화 쓰기 경로는 수정하지 않는다.

## SSOT·SOLID·FW·독립성 검토

- 잔액은 기존 Buffer 한 곳에서만 소유한다. 씬별 선택은 GlobalUIPresentation, 금액 표시는 각 UI, 연결은 기존 Bootstrap 콜백과 프리팹이 맡는다.
- 생성·내부 등록: GlobalSystemFactory가 기존 GlobalUIPresentation을 Global Builder에 등록한다. 보관·상위 접근: GlobalSystemGroup.GlobalUIPresentation. 소비: GameBootstrap이 기존 Global Context 접근자로 받아 쓴다. 새 서비스·루트 재등록·중복 Builder 등록은 없다(FW-001/002/003).
- UI 등록 단위는 기존 MainCharactor와 새 자식 Reputation이다. Parallel 계약으로 다른 화면을 닫지 않는다. 금액 위젯은 입력·이벤트 구독을 만들지 않는다. 금액 참조·Buffer 바인딩 오류는 대상 UIId와 필드를 명시한 예외로 중단한다.
- 명성 표시 형식 변경은 ReputationUI와 관련 검증만, 씬별 표시 정책 변경은 GlobalUIPresentation에서 처리한다. 전투·로비 도메인 기능과 서비스 조립 전체를 수정할 필요가 없다.

## Task·단일 Phase 결과

- [x] UIId·ReputationUI 추가, Fame Buffer 읽기, Parent·Parallel 설정.
- [x] 기존 프리팹에 Reputation 컴포넌트·amountText, MainCharactor.goldRoot 연결.
- [x] Lobby/StageSelect/Combat 진입 표시 전환과 Title 부모 정리 경로 유지.
- [x] 관련 테스트에 로비 복귀·Combat 직접 표시 정책, Parallel·Parent, 프리팹 참조 검증 반영.
- [x] Unity csproj 소스·참조를 사용한 Roslyn 게임 코드 컴파일 성공. 관련 GlobalUITests·MainCharactorUITests와 보조 코드 컴파일 성공.
- [ ] Unity EditMode 테스트 실행·실제 씬 전환 확인.

전체 Framework 테스트 컴파일은 기존 CS0023 6건으로 실패했다: RailwayTrackLayoutTests:69, FieldOutlineRendererTests:175·302, FieldRailwayPresenterTests:125, BattleDefinitionAdapterTests:533·914. 이번 범위에서는 수정하지 않았다. 컴파일 산출물은 Codex 스크래치 디렉터리에만 생성했다.
