# Combat·StageSelect UI 개편 Task 계획 (2026-10-29)

- 상태: P1~P4 구현·컴파일 확인 완료. P4는 사용자 Play 확인까지 완료. 최신 P4 기록은 §13, 진입 정책·P3은 §12. 특성 획득 순서는 §14의 별도 Core 후속으로 문서 인계. P5 담당 작업자 인계, P6 미착수.
- 설계 원본: [UI 개편 설계](../specs/2026-10-29-combat-stage-ui-revision-design.md).
- 작업 규칙: 한 번에 한 Phase만 구현하고 검증·잔여를 보고한 뒤 멈춘다. '진행해'는 다음 Phase 범위로 제한한다.
- 커밋·push는 사용자 담당. Core·FieldCamera·Architecture 및 사용자 기존 작업은 보존.

## 1. 순서와 리뷰 지점

| Phase | 범위 | 완료 판단 | 상태 |
|---|---|---|---|
| P0 | 빈 참조·소비 경로 조사 | 삭제/선택/필수 누락 분류와 설계 제안 | 정적 조사 완료, Unity 확인 전 |
| P1 | 삭제 UI 계약·비용 강조·카드 HP 연출 정리 | 삭제된 필드를 코드가 요구/조회하지 않음 | 구현 완료·컴파일 성공, 동작 검증 대기 |
| P2 | Deck Combat 씬 전용 등록·진입 초기 상태 | 최신 요청으로 진입 완료 시 DeckPannel 개방, Exclusive 유지 | 구현·컴파일 확인 완료, 후속 변경 §12, 실제 Play 확인 대기 |
| P3 | UnitCard 2특성·공용 특성 항목/설명 위젯 | 각 아이콘의 내용 갱신, 없는 슬롯 숨김 | 구현·컴파일 확인 완료, 실제 Play 확인 대기 |
| P4 | UnitInfo 전용 표시·n특성 hover | 유지 항목만 표시, n개·UnHover 정리 | 구현·컴파일·사용자 Play 확인 완료 |
| P5 | StageSelect 대형 Info 함수 준비 | 기존 합의·연결 지점을 담당 작업자에게 인계 | 구현 제외·인계 문서 준비, 미전달 |
| P6 | 전투 전 덱 편성 → 확정 후 진입 | 승인된 덱으로 기존 진입 명령 단 한 번 | 미착수 |

P1→P2→P3→P4까지 진행했다. 최신 요청으로 P5는 담당 작업자에게 넘기며 이 세션에서 구현하지 않는다. P6은 별도 진행 요청 후 시작한다.
P3은 P4가 재사용할 특성 항목/tooltip 계약을 만든다. P6은 카드 개편과 진입 수명 검증을 분리한 마지막 단계다.
각 Phase 끝에서 Unity 미확인 항목을 명시하고, 확인되지 않은 동작을 통과로 기록하지 않는다.

## 2. P0 — 조사 결과와 적용 전 확인

- [x] Combat.unity의 삭제된 UnitInfo 필드와 실제 SerializeField 비교.
- [x] UnitCard/Deck/편성 프리팹의 대상 참조 확인.
- [x] 여섯 씬/프리팹에서 0이 아닌 로컬 fileID 누락 없음 확인.
- [x] 비용 필수 검사·튜토리얼 강조·최초 Deployment 자동 열기 소비 경로 확인.
- [x] Global Factory 생성 → 내부 등록 → Group 보관/Export → Context 조회 추적.
- [x] 사용자 보완 반영: 대형 Info는 StageSelect 클릭용 함수만. 튜토리얼은 UnitInfo 비용 강조만 제거.
- [ ] 구현 직전 사용자 최신 diff와 Unity의 저장 여부 확인. 저장 안 된 Editor 상태는 YAML 조사와 다를 수 있음.
- [ ] 실제 프리팹 상속/override·외부 GUID·Missing Script는 Unity에서 확인.
- [ ] UI/Unity 테스트를 넣을 기존 어셈블리·내부 접근 가능 범위 확인. 새 asmdef 임의 생성 금지.

최신 구현 결과는 §13이다. P4 리뷰 지점에서 중단한다. P5는 인계 범위이며 다음 세션 구현 후보는 P6이다.

## 3. P1 — 삭제 UI 계약 정리

### Task

- [x] CombatUnitInfo에서 moveCost·attackCost의 필수 검사·텍스트 설정·렌더·초기화 연결 제거.
- [x] moveCostRoot·attackCostRoot와 RequireCostRoot 소비 경로 제거.
- [x] TutorialDefinition_1-1 stepKey 21에서 UnitInfoActionCosts(11)만 제거. ActionCost(8)와 다른 비용 안내 유지.
- [x] TutorialViewFocusLocator의 해당 강조 조회를 정리. 다른 설정의 삭제 대상 잔존을 검사. enum 기존 번호 재배치 금지.
- [x] UnitCardView에서 healthFill·healthSubFill·카드 전용 HealthFillAnimation/FixedUpdate 연결 제거. healthText 유지.
- [x] ShowDetails/ShowDeckDetails의 카드 비용 인자·문자열 제거, 실제 소비자만 갱신.
- [x] 현재 UnitInfo가 쓰는 UnitCardView의 남은 null은 간소화 전환 중임을 기록. P4 전용화에서 최종 필수 검사를 확정.

### 주요 파일

Combat/Display/Hud/CombatUnitInfo.cs, Tutorial/TutorialViewFocusLocator.cs,
UISystem/Screens/Global/UnitCardView.cs·UnitCardUI.cs,
UISystem/Screens/Stage/DeckEditUI.cs, TutorialDefinition_1-1.asset, 해당 씬/프리팹 SerializeField 잔존.

### 검증·리뷰

- Unity 컴파일. 삭제 필드·API 소비 잔존 검색.
- 실제 Combat 조립이 비용 null 때문에 실패하지 않는지 확인.
- UnitCard HP 텍스트가 최신 수치로 갱신되고 카드 바 연출이 실행되지 않는지 확인.
- 튜토리얼 21번 나머지 강조·설명 진행 확인. DB 에셋 변경에 따라 관련 DataBaseAssetTests 수행.
- 공용 HP 바 유틸·필드 HUD·메인 HP 및 Core 비용은 영향 없음 확인을 필요한 참조 검색 범위로 제한.
- 완료 보고 후 중단.

## 4. P2 — Deck 씬 등록과 초기 상태

### Task

- [x] DeckButtonUI : GlobalUIBase → UIBase, ID Deck·Parent None·Parallel 유지. 기존 GUID 보존.
- [x] DeckCardList/CombatUnitInfo의 동일 부모·Exclusive 관계와 그룹 멤버 소유 유지.
- [x] Combat 씬 밖 Deck 등록/열기 의존 제거. GlobalUIPresentation의 StageSelect Deck 처리 제거.
- [x] Bootstrap PrepareCombatDeckUI의 부모 준비 순서는 유지하되 진입 초기화의 소유자를 하나로 확정.
- [x] Bootstrap OpenCombatUI의 중복 루트 열기 제거. 카드 자동 열기는 P2에서 제거했으나 최신 요청에 따라 진입 완료(200)에 한 번 실행하도록 후속 적용(§12).
- [x] CombatUnitInfo.Render의 최초 국면 관측 시 자동 열기 제거. 이후 국면 전환과 구별.
- [x] Deck 루트의 이력 제외 열기와 자식 뒤로가기·씬 해제 정리 확인.
- [x] 씬/프리팹에서는 사용자 레이아웃 변경을 보존하며 필요한 참조만 조정.

### 검증·리뷰

- Boot→StageSelect→Combat, 개발 Combat 직행, Combat 재진입 각각 표시 연결 후 DeckPannel 개방. 버튼은 기존 개폐 규칙 유지(최신 요청 §12).
- StageSelect·Lobby에는 전투 Deck이 없음. 등록 누락을 정상으로 숨기는 fallback 추가 금지.
- Deck 열기 → UnitInfo 선택 → Deck 다시 열기에서 상호 배타.
- 뒤로가기 시 자식 닫힘과 버튼 접근성, 드래그 취소·hover 해제.
- 튜토리얼의 명시적 덱 공개와 이후 Deployment 복귀 동작 확인.
- 완료 보고 후 중단.

## 5. P3 — 카드 특성 모델·두 슬롯

### Task

- [x] UnitCardTraitContent를 특성별 항목 변환으로 바꾸고 기존 동적 설명·사용 불가 표기를 유지.
- [x] UnitCardUI/DeckEditUI 및 실제 공통 소비자를 새 항목 계약으로 이관.
- [x] InfoPannel 안 Ability1/Ability2를 명시적 슬롯 참조로 구성.
- [x] 공용 tooltip 위젯에 Icon·Name·Description을 원자적으로 갱신하는 표시 API 준비.
- [x] 정보 패널 열림 상태와 그 안 tooltip 상태를 분리. 기존 상호 배타 enum 폐기/조정.
- [x] 특성 0개/1개/2개, 빈 슬롯 숨김·재사용·다른 유닛 전환 초기화 처리.
- [x] 슬롯 입력은 UIInputBinding·IUIInputReceiver, 클릭·Submit·기존 hover 옵션 유지.
- [x] UnitInfo는 P4 전환까지 기존 표시를 유지할 수 있는 최소 이행 경로만 사용. 영구 합친 문자열 모델 중복 금지.
- [x] 2개 초과 데이터의 카드 처리 계약 위반을 명시적으로 검사하고 임의 절단 금지.

### 검증·리뷰

- 0개: 슬롯/tooltip 비활성. 1개: Ability2 비활성. 2개: 두 아이콘과 각각 올바른 내용.
- A→B 변경 시 Icon/Name/Description 모두 교체, InfoPannel은 유지.
- 재바인딩·닫힘·입력 불가·빠른 hover 이동 시 이전 설명 잔류/잘못 닫힘 없음.
- 덱 클릭 고정과 배치/드래그, 편성 교체/제거 동작 유지.
- 특성 설명 변환의 기존 표식·끌어오기·가시밭 및 사용 불가 표기 검증.
- 공통 계약이 바뀐 실제 소비자만 표시 회귀. 완료 보고 후 중단.

## 6. P4 — UnitInfo n특성과 Hover

### Task

- [x] CombatUnitInfo의 표시를 전용 내부 위젯으로 분리. Name·Level·Category·특성만 필수.
- [x] 삭제된 공용 UnitCardView 의존과 직렬화 필드 정리. 필요 없는 빈 컴포넌트/참조를 남기지 않음.
- [x] 특성 슬롯을 n개 목록으로 연결·확장·재사용. Ability1/2는 최초 슬롯으로 인수.
- [x] 각 HoverEnter에서 P3 tooltip에 해당 특성 표시, HoverExit에서 현재 슬롯 대조 후 닫기.
- [x] UnitInfo에서는 클릭으로 tooltip을 고정하지 않음. Button/Submit 수신은 명시적으로 정의하고 삭제된 행동 호출 금지.
- [x] 유닛 선택 원본·슬라이드·표시 완료 시점·Exclusive 관계 유지.
- [x] 필수 의존성은 UIId·필드·객체를 포함한 예외로 검증.

### 검증·리뷰

- 0/1/2/3개 이상 특성, 슬롯 수 축소와 대상 전환, 동일 유닛의 특성 변경.
- Ability1→Ability2 hover 전환 및 늦은 Exit, UnHover·Hide·Disable·Rebind·interactable 상실.
- 기존 고정 선택과 다른 유닛 hover가 충돌하지 않는지 확인.
- 선택 전환 슬라이드와 DeckPannel Exclusive 검증.
- 완료 보고 후 중단.

## 7. P5 — 대형 Info 함수 준비 인계

2026-10-29 사용자 요청으로 이 세션의 구현 범위에서 제외한다. 아래는 담당 작업자에게 넘기는 원래 합의 범위다. 함수·입력·패널은 추가하지 않았다. [P5 인계 메모](../handoffs/2026-10-29-stage-select-large-info-p5-handoff.md) 준비, 외부 전달은 미실행.

- [ ] StageSelect 클릭용 대형 정보 요청 함수의 대상 ID·인자 계약 정의.
- [ ] 구현 예정 주석과 향후 호출 지점 기록.
- [ ] 실제 패널 생성·SerializeField·UIId·화면 개폐 구현·미완성 경로 입력 전환은 하지 않음.
- [ ] 기존 InfoPannel·편성 교체/제거·Combat/hover 동작 유지.
- [ ] 컴파일 확인. 큰 패널 실제 동작은 미구현으로 보고하고 중단.

## 8. P6 — 전투 전 편성 확인

### Task

- [ ] StageMapInteraction의 즉시 Submit 앞에 명시적 진입 의도 수신 계약 연결.
- [ ] 전투 여부는 Buffer StageProgressData에서 읽음. 증강 우선 차단·비전투 Submit 유지.
- [ ] 진입 조정자 한 곳에서 Run·chapter·node·현재 연결 식별과 보류 수명 소유.
- [ ] 아직 Submit하지 않은 보류 대상에 가짜 StageEntryIdentity/명령 번호를 만들지 않음.
- [ ] DeckEdit 열기 목적(일반/전투진입)을 표시 모델에 반영.
- [ ] confirm label TMP_Text 연결·검증, 전투진입 문구는 '전투 진입'.
- [ ] 기존 draft·Read/Evaluate/Confirm·실패·재평가·편성 버튼 기능 유지.
- [ ] 편성 성공/취소는 명시적 완료 수신 계약으로 전달. 팝업 컨트롤러가 씬/노드를 알지 않도록 분리.
- [ ] Accepted 확인 → 유효성 확인 → 모달 닫기/Layer 복구 → 기존 StageView Submit 순서 구현.
- [ ] 단일 확정·단일 Submit, 재진입/늦은 콜백/취소/Scope 교체 처리.
- [ ] 실제 진입 수락/거절·이동 연출은 기존 Observer/Plan/Job에 맡김.
- [ ] 기존 설정 Canvas(99)와 DeckEdit Canvas(100)의 가림/Layer 문제 재현 및 범위 내 조정.

### 검증·리뷰

- 전투 클릭 직후 명령 0개, 팝업 표시와 '전투 진입' 문구.
- 편성 확정 수락 후에만 명령 1개, 연타·키보드 Submit에도 중복 없음.
- 확정 거절/버전 변경은 팝업 유지. 취소·닫기·뒤로가기에서는 진입 0개.
- 일반 편집 후 전투진입, 전투진입 취소 후 일반 편집의 문구·목적 초기화.
- Run/맵/대상 변경·씬 이탈·설정 열기·증강 대기의 수명과 입력 복구.
- 편성 확정 뒤 진입 거절: 덱 유지, 재확정 자동 호출/자동 재진입 없음.
- 비전투 노드와 개발 Combat 직행의 기존 동작.
- 실제 운영 DB 진입과 Combat 도착 초기 상태를 P2와 함께 인수.
- 완료 보고 후 중단.

## 9. 검증 운영과 기록 양식

UI·진입 수명처럼 행동 변경에는 의미 있는 상태 전이 검증을 둔다. 단순 필드 삭제를 그대로 복제하는 테스트는 쓰지 않는다.
입력은 EventSystem의 pointer/Submit 핸들러로 전달한다. Button.onClick 직접 호출을 증거로 쓰지 않는다.
관련 UI 테스트 어셈블리가 현재 체크아웃에 없다면 기존 호환 어셈블리·Editor 검증 경로를 먼저 확정한다. 문서에만 있는 테스트가 실행됐다고 쓰지 않는다.
매 Phase마다 변경 이유·파일·검증 결과·미확인/잔여·다음 Phase를 기록한다.

| 항목 | 기록 |
|---|---|
| 완료 Task | |
| 설계 대비 변경 | 없으면 없음, 있으면 구현 전 승인 근거 |
| Unity 컴파일 | 미실행/성공/실패 |
| EditMode 검증 | 실제 이름·결과, 미실행이면 이유 |
| Play 확인 | 실행 경로와 관측 결과 |
| 필수 참조/등록/계층 | 생성·등록·Export·소비 조회 확인 |
| SSOT/SOLID/독립성 | 변경 경계와 남는 공통 계약 |
| 다음 Phase | |

## 10. P1 실행 기록 — 2026-10-29

### 구현

- CombatUnitInfo의 비용 텍스트·강조 Root·필수 검사·렌더·초기화와 비용 표시 캐시를 제거했다. 남은 card/categoryIcon 필수 오류는 UIId·오브젝트·필드를 명시한다.
- 비용 표시 전용 전달 경로도 제거했다: ICombatBattleDisplay → CombatSceneDisplay → CombatUnitInfo, CombatSceneInput의 콜백/표시 키, InputFactory의 콜백 주입, CombatViewSystem의 표시 동기화 호출. 의미 없는 빈 메서드는 남기지 않았다.
- CombatActionAvailabilityTracker 및 그 Sync/Read/Clear/Stop, 공격/이동 입력의 가용성 소비는 유지한다. 계산·입력 판정은 변경하지 않았다.
- UnitCardView의 HP 바 직렬화 필드·HealthFillAnimation·FixedUpdate와 연출 상태를 제거했다. HP 텍스트는 즉시 현재 값으로 갱신하고 기존 수치 범위 보정을 유지한다.
- 카드 ShowDetails/ShowDeckDetails의 비용 인자·문자열을 제거하고 UnitCardUI·DeckEditUI 호출을 갱신했다.
- UnitCard.prefab·LobbyDeckPanel.prefab의 삭제된 바 필드 각 4줄과 Combat.unity의 삭제된 필드 8줄만 정리했다. 사용자 기존 레이아웃·폰트 변경을 보존한다.
- 튜토리얼 stepKey 21의 focusTargets에서 11만 제거했다. ActionCost(8), 설명 기준 대상·문구·다른 단계는 그대로다.
- 삭제된 enum 값 11은 제거하고 재사용 금지 주석을 남겼다. 나머지 번호는 유지하므로 옛 11이 남은 설정은 기존 Enum.IsDefined/Locator 검증에서 오류가 된다.
- UnitInfo가 공용 UnitCardView에 가진 나머지 빈 필드는 P4 전용화에서 정리한다. Deck 초기 상태·2특성 연결·대형 Info 함수·전투 전 편성은 아직 구현하지 않았다.

### 검증 증거

- Unity 에디터 자동 재컴파일: 성공. Editor.log의 컴파일 ExitCode 0, CompileScripts 약 6.6초, Domain Reload 완료를 확인했다. ProjectL.Systems.dll 갱신 시각은 이 PC의 2026-10-29 13:50:17이다.
- 삭제된 비용 API·표시 문자열·HP 연출 override 잔존 검색: 대상 경로에서 없음.
- Combat·UnitCard·LobbyDeckPanel·DeckEdit 프리팹/씬의 0이 아닌 로컬 fileID 누락 없음. 대상 컴포넌트의 삭제 필드 잔존 없음. UnitCard HP 텍스트 참조 유지.
- 튜토리얼 에셋 전체를 HEAD와 비교해 focusTargets의 11 제거 이외 내용 변화가 없음을 검사했다.
- 변경 C# UTF-8/LF 및 git diff --check(코드·프리팹·DB): 통과. Combat.unity 전체 diff의 기존 Unity YAML 빈 값 뒤 공백은 사용자 기존 변경에 있어 일괄 수정하지 않았다.
- Core 로직·Framework 코드·공용 HealthFillAnimation·필드 HUD·메인 HP 바 수정 없음.
- 기존 Framework/UI 테스트 소스와 DataBaseAssetTests는 현재 체크아웃에서 확인되지 않았다. Test Runner에도 해당 어셈블리가 없으며, 이번 EditMode 테스트 실행 결과는 없다. 실제 Combat 진입/HP 텍스트/튜토리얼 21번 Play 확인도 미실행이다.

### 설계·계층 검토

- 설계 변경 없음. 비용 표시를 없애면서 그 전용 전달 경로와 직렬화 잔존을 함께 없앤 것은 P1 계약 정리 범위다.
- SSOT: 수치 원본은 Buffer/Core, 가용성 원본은 기존 Tracker. 제거한 것은 표시 사본과 연출 상태다.
- SOLID/독립성: 표시 계약에서 없어진 책임을 Factory·Input·Display 연결 끝까지 정리했다. 행동 가용성 기능을 다시 구현하거나 새 서비스/상태를 만들지 않았다. UI 상속·등록·개폐 정책은 P2/P4에 남겼다.
- 서비스 생성/등록/Export/소비 경로 변경 없음. Global Context 조회 유지, 루트 재등록 추가 없음. 새 입력 구독/UnityEvent 연결 없음.

P1 구현·컴파일 결과를 보고하고 중단한다. 다음 P2 전 리뷰 항목은 Combat 진입 시 비용 참조 오류 해소, 카드 HP 텍스트 갱신, 튜토리얼 21번의 남은 강조/진행이다.

## 11. P2 실행 기록 — 2026-10-29

### 변경 결과

- DeckButtonUI는 GlobalUIBase 대신 UIBase를 상속한다. ID Deck(9), Parent None, Policy Parallel 및 스크립트 GUID는 유지한다.
- Deck 루트는 Combat 씬 BindScene에서 등록되며 Global 목록·DontDestroyOnLoad 대상이 아니다. 일반 씬 바인딩에서 열린 상태와 등록이 교체된다.
- UISystem의 기존 OpenUI(Deck, true)를 사용해 Combat 안에서는 버튼 루트를 뒤로가기 이력에서 제외한다. 자식 패널 이력은 그대로다.
- PrepareCombatDeckUI(90) 한 곳에서 부모 열기 → 자식 닫기를 수행한다. 일반 Open 때마다 초기화하지 않는다. View 연결(100)·후속 공통 UI 표시(200)의 순서는 그대로다.
- OpenCombatUI의 중복 Deck 열기와 최초 카드 자동 열기를 제거했다. GlobalUIPresentation.ShowStageSelect도 전투 Deck을 조회/복원하지 않는다.
- CombatUnitInfo.Render는 이전 국면이 없는 최초 Deployment에서는 목록을 열지 않는다. 이후 배치 국면 복귀에서는 기존 열기를 유지한다.
- DeckPannel/CombatUnitInfo의 ParentID Deck·Policy Exclusive와 덱 카드 4개의 그룹 소유 관계는 유지한다. 기존 UISystem의 형제 교체·자식 뒤로가기·씬 바인딩 규칙을 사용한다.
- DeckButtonUI의 누락 참조/서비스 연결 오류는 UIId·오브젝트·필드명을 포함하는 예외로 보고한다. 서비스 참조는 UIBase의 기존 연결을 사용한다.

### 실제 Hierarchy

Combat 씬의 Canvas 아래 DeckUI에 DeckButton, DeckPannel, UnitInfo를 둔다. 이전 Canvas/UnitInfo를 Unity 에디터에서 Canvas/DeckUI/UnitInfo로 이동해 저장했다.
DeckPannel과 UnitInfo는 형제 패널이며 DeckButton은 루트 내부의 버튼이다. 등록 관계와 실제 Transform 관계가 일치한다.
UnitInfo는 프리팹 인스턴스의 추가 자식으로 연결된다. Unity가 m_AddedGameObjects를 기록했으며, UnitInfo의 앵커·위치(250,125)·크기(500,250)·배율(1,1,1)은 유지됐다.
DeckUI.prefab의 DeckPannel 기본값은 비활성, Combat 씬의 DeckButton override는 활성, UnitInfo 기본값은 비활성으로 맞췄다.

### 분기·참조·컴파일 점검

- BindScene에서 일반 씬 UI 수집 → HideClosedRegisteredUI → PrepareCombatDeck(90) → CombatViewAttach(100) → OpenSceneUI(200) 경로를 대조했다. Bootstrap·최초 Deployment 자동 열기로 패널이 다시 열리는 경로를 제거했다.
- 최초 Deployment/이후 Deployment 복귀, 정상 Combat 진입/개발 직행, 일반 Open/명시적 초기화 분기를 코드로 대조했다.
- GlobalUIPresentation에는 Deck 참조가 없다. Boot·StageSelect·Lobby 씬의 전투 Deck 프리팹/스크립트 인스턴스도 없다.
- DeckPannel→UnitInfo 전환에서 Exclusive 자식만 닫고 Parallel 부모는 유지하는 기존 UISystem 경로를 확인했다. 뒤로가기도 열린 자식을 닫고 이력 제외 Deck 루트는 남긴다.
- 튜토리얼의 TutorialViewCardVisibility는 여전히 명시적으로 덱을 열고 자기 수명에서 닫는다. 진입 기본 상태와는 구별한다.
- Combat.unity와 DeckUI.prefab의 0이 아닌 로컬 fileID 참조 누락 없음. UnitInfo 부모·추가 자식 기록과 Canvas 중복 자식 제거를 확인했다.
- Unity 에디터의 P2 스크립트 재컴파일 ExitCode 0·도메인 재로드를 확인했다. 최종 ProjectL.Systems.dll 갱신은 2026-10-29 14:13:01이며, 마지막 CompileScripts는 약 9.3초다.
- 변경 코드·프리팹의 diff 공백 검사 및 C# UTF-8/LF 확인. 사용자 기존 씬 변경은 P2 시작 시 스냅샷과 별도로 비교해 보존했다.
- 실제 Play 경로(전투 재진입·Exclusive 전환·뒤로가기·드래그 취소·튜토리얼 진행)는 이번 코드/컴파일 점검 결과와 구별하며 미실행으로 남긴다.

### 계약·독립성

- 설계 이탈 없음. UIBase 전환과 실제 부모–자식 정렬은 P2 범위다.
- 생성/내부 등록: GlobalSystemFactory → UISystem.ExportServices의 Global 내부 등록 유지. 보관: GlobalSystemGroup.UIServiceCommand. 상위 Export: Global 그룹 계약/모듈만 유지. 소비: 기존 Global Context 조회를 그대로 사용한다. 루트 서비스 재등록/직접 조회 추가 없음.
- 초기화 정책은 Bootstrap 준비 노드, 화면 개폐는 IUISystemService, 카드 목록/선택은 기존 소유자에 둔다. 특성 표시/덱 편성/전투 계산을 수정하지 않았다. UIBase 및 기존 입력 수신 계약을 유지하며 새 서비스·입력 구독·공유 상태를 만들지 않았다.

P2 구현 결과를 보고하고 중단한다. 다음 P3는 별도 진행 요청 이후에 시작한다.
## 12. P2 후속·P3 실행 기록 — 2026-10-29

### 최신 진입 정책

사용자 요청: “DeckPannel을 스테이지 진입 시 개방하는 것만 추가 적용하고 P3으로 이행하자.”
Combat 진입의 OpenCombatUI(200)에서 공통 표시 연결 후 OpenChildUI(Deck, UnitCard)를 호출한다. 일반 진입과 개발 Combat 직행 모두 같은 경로다. PrepareCombatDeck(90)의 부모 준비·자식 닫기, 최초 국면 관측과 이후 Deployment 복귀 구분은 유지한다.
최종 초기 표시 정책은 §11 당시의 DeckPannel 닫힘을 대체한다. DeckButton 표시 전환·Exclusive·뒤로가기 및 씬 해제는 기존 경로를 사용한다.

### 구현

- UnitCardTraitContent를 Key·Icon·Name·Description 항목 배열로 전환했다. 가시밭·표식·끌어오기 동적 설명과 사용 불가 표기를 항목별로 보존한다. 특성은 있지만 View 외형 데이터가 없는 구성은 명시적 예외로 보고한다.
- UnitAbilitySlot과 UnitAbilityTooltip을 직렬화 가능한 내부 위젯으로 추가했다. MonoBehaviour나 서비스가 아니며 새 UIId·등록·외부 개폐 경로를 만들지 않는다. 슬롯·패널 소유 관계와 필수 참조를 바인딩 전에 검증한다.
- UnitCardView는 InfoPannel의 Ability1·Ability2를 명시적으로 연결한다. 미보유 슬롯을 숨기고 두 슬롯이 같은 설명 패널의 Icon·Name·Description 전체를 갱신한다. 카드의 두 슬롯보다 많은 데이터는 임의 절단하지 않고 구성 예외를 발생시킨다.
- 정보 패널과 특성 설명의 표시·고정 상태를 분리했다. 특성 입력으로 InfoPannel이 닫히지 않으며, 동일 특성 재클릭은 설명만 닫는다. 늦은 HoverExit는 현재 슬롯을 대조한다. Hide·Clear·비활성화·대상 교체에서 내부 상태를 정리한다.
- 입력은 기존 UIInputBinding·IUIInputReceiver로 한 번 바인딩한다. Button 클릭·Submit·기존 hover 옵션을 유지한다. UnitCardUI는 기존 변경 감지를 패널 열기에도 사용해 값이 같은 입력마다 표시 배열·문자열을 재생성하지 않는다.
- UnitCardUI·DeckEditUI를 새 항목 계약으로 이관했다. 편성 카드가 같은 종류의 다른 보유 유닛으로 바뀌면 기존 패널 상태를 초기화한다. 덱 편성 규칙·교체·제거·전투 드래그 기능은 재구현하지 않았다.
- UnitInfo는 P4 전까지 기존 단일 합친 설명을 유지하는 임시 어댑터만 둔다. 특성 변환 원본은 공용 항목 모델 한 곳이다. P4에서 이 조합과 공용 UnitCardView 의존을 제거하고 n개 슬롯·Hover/UnHover를 적용한다.
- UnitCard.prefab의 삭제된 단일 Ability 참조를 정리하고 InfoPannel 내부 두 중첩 Ability 프리팹의 Button·Image를 연결했다. Combat UnitInfo와 LobbyDeckPanel의 직렬화 필드도 새 구조에 맞췄다. 레이아웃은 유지한다.
- 기존 CoreIntegration 소스 2파일의 삭제된 Names/Descriptions 접근만 새 항목 API로 바꿔 컴파일 호환성을 맞췄다. 새 검증 소스·사례는 추가하지 않았다.

### 확인 결과와 미확인

- Combat.unity·UnitCard·UnitCardUI·DeckUI·DeckEditUI·LobbyDeckPanel 여섯 파일에서 로컬 fileID 중복과 0이 아닌 로컬 참조 누락이 없다. 중첩 Ability 프리팹의 Button·Image 원본 ID와 인스턴스 ID를 대조했다. 삭제된 단일 특성 필드·옛 표시 API 소비 잔존이 없다.
- 특성 0/1/2개, 슬롯 축소·동일 특성 내용 갱신·대상 교체·같은 종류의 다른 보유 유닛·정보/설명 동시 표시·늦은 Exit 분기를 코드로 점검했다. 실제 포인터·Submit·드래그 재생 확인과 구별한다.
- P3 작업 중 Unity 스크립트 컴파일 ExitCode 0을 확인했다(마지막 에디터 어셈블리 갱신 14:45:31). 이후 최종 소스는 프로젝트 Unity 6000.0.73f1의 Roslyn과 Bee 응답 파일로 런타임 ProjectL.Systems 및 기존 CoreIntegration 소비 어셈블리를 다시 컴파일했다. 둘 다 ExitCode 0, 컴파일 로그에 오류·경고 없음이다. 이 결과는 테스트 실행 결과가 아니다.
- 최종 컴파일 산출물·응답 파일·로그는 이 세션 스크래치의 p3-compile/에 보관했다. Unity 창 활성화가 시간 초과되어 최종 소스의 에디터 도메인 재로드·실제 Play·프리팹 Inspector 최종 해석은 확인하지 않았다. 실행 중 0/1/2개 표시, tooltip 배치, Exclusive·뒤로가기·입력 동작은 사용자 확인이 남아 있다.
- 새 C#과 .meta를 함께 추가했으며 변경 C#의 UTF-8/LF 및 공백 diff를 확인했다. 기존 사용자 씬 변경과 폰트 파일을 보존했다.

### 구조 검토·중단 지점

- SSOT: 규칙·보유 사실은 Buffer, 외형은 View DB, 동적 설명은 항목 변환, 카드의 표시 상태는 UnitCardView가 각각 소유한다. UnitInfo 임시 조합은 P4 제거 대상으로 한정한다.
- SOLID·독립성: 슬롯 참조·특성 변환·설명 표시·조회 및 조립 책임을 구분한다. 내부 위젯은 서비스·루트 등록을 추가하지 않으며 기존 Global Context → UISystem과 UIBase 수명 경로를 유지한다. Core·FieldCamera·Architecture는 수정하지 않았다.
- P3 구현·컴파일 확인을 마치고 중단한다. 다음은 별도 진행 요청 후 P4(UnitInfo 전용 표시·n특성·Hover)다.

## 13. P4 실행 기록 — 2026-10-29

### 범위·추가 합의

- P4만 구현한다. P5는 사용자 요청으로 담당 작업자에게 넘기며 함수도 작성하지 않는다. 인계 문서는 준비했지만 다른 작업자·다른 채팅에 발송하지 않았다.
- n개 특성의 배치는 사용자 답변 “같은 간격으로 옆에 추가, 공간이 차면 줄바꿈”을 따른다. 기존 Ability1·2의 첫 행 위치·크기는 유지한다.

### 구현·씬 연결

- CombatUnitInfoView를 UIWidgetBase 기반의 UnitInfo 내부 위젯으로 추가했다. Name·Level·Category·특성 슬롯/설명만 요구하며 UnitCardView의 빈 portrait·수치·HP·비용 필드를 사용하지 않는다. CombatUnitInfo의 임시 합친 설명 어댑터도 제거했다.
- CombatUnitInfo는 선택 원본·완료된 Buffer 표시 시점·슬라이드·국면별 덱 전환을 유지한다. 외부 개폐는 기존 UISystem의 OpenChildUI/CloseUI를 사용하고 UI 서비스를 생략한 직접 Open/Hide 우회는 제거했다. 실제 Factory는 기존처럼 UI 및 필요한 View Query를 전달한다.
- Ability1·2는 초기 슬롯으로 재사용한다. 데이터가 더 많아지면 첫 슬롯의 Button·Image 구조를 복제해 한 번 바인딩하고, 이후 목록 갱신에서 다시 사용한다. 슬롯 부족 때문에 특성을 합치거나 자르지 않는다. 미보유 슬롯은 비활성화한다.
- UnitAbilitySlot에 목록 갱신용 Clone을 추가했다. 복제본의 아이콘은 원본의 자식 인덱스 경로로 연결한다. 기존 카드 표시·입력 계약은 바꾸지 않았다.
- 각 슬롯은 UIInputBinding/IUIInputReceiver를 사용한다. HoverEnter에서 해당 항목 전체를 P3 공용 설명에 전달하고 HoverExit에서는 현재 인덱스가 같을 때만 닫는다. 클릭·Submit은 고정하지 않는 것으로 명시했다. 포인터 콜백에서는 조회·슬롯 생성·배열/문자열 생성·레이아웃 갱신을 하지 않는다.
- 같은 특성의 수치가 갱신되면 열린 설명도 갱신한다. 슬롯 축소·다른 키·유닛 전환·Clear·Hide·Disable에서 설명과 바인딩의 Hover 상태를 정리한다. interactable 상실은 기존 UIInputBinding의 정리 경로를 사용한다.
- Combat.unity에서 UnitInfo의 기존 UnitCardView 컴포넌트를 전용 위젯으로 교체하고 CombatUnitInfo.view를 연결했다. AbilityArea를 만들고 두 슬롯을 같은 Flexible Grid로 옮겼다. 셀 70×70, 간격 30, 폭 400으로 현재는 한 행에 4개를 배치한다. 행이 늘면 정보창 높이를 늘려 하단 경계를 유지하고 Name·Level·Category는 상단 기준 위치를 유지한다. 슬라이드 초기화는 X만 복원해 확장된 높이/Y와 충돌하지 않게 했다.
- 첫 AbilityPannel을 UnitInfo의 직접 자식으로 옮겨 하나의 공용 설명으로 사용한다. 두 번째 중복 설명 트리와 설명 클릭용 Button을 제거했다. blocksRaycasts=false인 CanvasGroup으로 설명 패널이 슬롯의 Hover를 가로채지 않도록 했다.
- 구성 검증은 등록 전 ExportUIService와 Configure에서 수행한다. 누락·잘못된 소유 관계·중복 슬롯·Grid 설정·설명의 raycast 구성은 UIId.CombatUnitInfo·오브젝트·필드를 포함한 예외로 보고한다.

### 확인 결과·남은 확인

- 현재 Combat.unity의 로컬 fileID 중복·누락 및 Transform 부모/자식 역참조를 검사했다. 전용 view·공용 설명의 독립 부모·삭제된 중복 패널/버튼 부재·nonblocking CanvasGroup 설정을 확인했다.
- 코드 분기로 특성 0/1/2/3개 이상·축소/재확장·재사용·현재 인덱스/키 변경·같은 항목의 내용 갱신·늦은 Exit·유닛 전환·Hide/Disable/Clear를 점검했다. Grid의 줄바꿈에 따라 높이/Y가 늘고 다시 줄어드는 계산과 X 슬라이드 복원 경계를 대조했다.
- 최종 소스는 Unity 6000.0.73f1의 Roslyn·프로젝트 Bee 응답 파일을 사용해 ProjectL.Systems 및 기존 CoreIntegration 소비 어셈블리를 컴파일했다. 둘 다 ExitCode 0, 오류·경고 로그 없음이다. 산출물·응답 파일·로그는 세션 스크래치 p4-compile/에 있다. 실제 Play나 테스트 실행 결과는 아니다.
- Unity Editor.log에는 이전 실행의 UnitCard→Deck 등록 오류, DeckCard0 특성 슬롯 누락 오류와 GameBootstrap.Update의 NullReferenceException이 남아 있다. 현재 파일의 Deck 등록과 두 카드 슬롯의 중첩 Button/Image 대응은 확인했으나 이번 작업에서 해당 오류를 Play로 재현하거나 해결 판정하지 않았다. 최종 동작 확인과 구별한다.
- 실제 Play의 특성 Hover·빠른 전환·Button/Submit·동적 복제·여러 행 배치·선택 슬라이드·DeckPannel Exclusive 및 프리팹 최종 해석은 미확인이다. 기존 사용자 폰트·프리팹·씬 변경을 보존했다.

### 구조 검토·중단 지점

- 등록 단위는 CombatUnitInfo(37)/Deck/Exclusive 그대로다. 전용 UIWidgetBase는 같은 화면에 BindOwner되며 별도 UIId·이력·서비스로 등록하지 않는다. BindScene에서 등록·검증, CombatViewAttach에서 Configure·표시 바인딩, 화면 Hide에서 Hover 정리, 씬 파괴에서 생성 슬롯까지 함께 해제된다.
- 서비스 생성/등록/Export/조회는 설계 §3의 기존 GlobalSystemFactory → UISystem의 Global 내부 등록 → GlobalSystemGroup 보관 및 Group Export → Global Context → CombatSceneDisplayFactory 전달 경로를 유지한다. 새 서비스·루트 재등록·resolver 조회는 없다(FW-001/002/003/008/009 및 프로젝트 계층 규약).
- SSOT: 선택은 기존 CombatSelectionState, 규칙·특성 사실은 Buffer, 외형은 View DB, 항목 설명은 UnitCardTraitContent, Hover와 파생 레이아웃은 전용 표시 위젯이 소유한다.
- SOLID·독립성: 특성 표현/배치를 바꿀 때 전용 위젯·슬롯·씬만 수정한다. 유닛 선택/슬라이드와 카드 편성·StageSelect 진입은 변경 이유가 다르므로 분리했다. Core·FieldCamera·Architecture 및 기존 서비스 조립 구현은 수정하지 않았다.
- P4 완료 보고 후 중단한다. P5는 인계 대상으로 남기고 P6을 자동 시작하지 않는다.

### 사용자 Play 확인·P4 종료 확정 — 2026-10-29

사용자가 “Play 확인 끝냈어”라고 확인했다. §13의 미실행·미확인 표기는 구현 보고 당시 Codex 확인 범위의 이력이며, 최신 P4 상태는 구현·컴파일·사용자 Play 확인 완료다. 사용자 확인을 Codex가 직접 실행한 결과로 바꾸어 기록하지 않는다.
P4를 종료한다. 특성 획득 순서는 §14 및 Core 요청 문서로 분리해 인계하며, 해당 후속의 미반영 상태 때문에 P4를 확인 대기로 되돌리지 않는다.

## 14. 별도 후속 — 특성 획득 순서 (2026-10-29, Core 요청 문서 인계)

- 사용자 요구: 기본 특성은 Ability1, 나중 획득한 특성은 Ability2. n개 목록도 기본 이후 획득 순서이며 강화는 위치를 바꾸지 않는다.
- 현재 Core의 UnitDefinition에서 TraitKey 정렬이 일어나 전투 TraitStatuses와 전투 밖 TraitKeys가 모두 획득 순서를 잃는다. View 변환은 입력 순서를 유지하고 있다.
- 공개 Growth.Selections에는 선택 순서만 있으며 Acquire/Enhance 구분은 없다. View에서 이를 곧바로 정렬 키로 쓰면 B 획득 후 A 강화 같은 경우가 틀리므로 구현하지 않는다.
- [Core 요청](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#trait-acquisition-display-order-request)에 생산·소비 경로와 표시 순서의 별도 사실 제공안을 준비했다. Core 코드·계산 정렬·View 입력은 변경하지 않았으며 요청은 외부 미전달이다.
- 사용자 결정: Core 요청 문서로 인계한다. 이 세션에서 Core를 직접 수정하지 않는다. Core 작업자의 계약 제공 후 공용 표시 변환과 소비 경로 연결은 별도 후속 작업으로 진행한다. 획득 순서 보장은 아직 미반영이며 P4 완료와 구분한다. P5 인계·P6 미착수 상태는 유지한다.

## 15. 규약 리뷰 1·2번 후속 수정 — 2026-10-29

사용자가 리뷰의 1·2번만 수정하도록 요청했다. 기존 구성 예외 전반에 소유 UIId를 보강하는 3번은 이번 범위에서 제외했다. P4 사용자 Play 확인 완료 상태는 유지하며, 아래 수정 이후의 Play 확인과 구분한다.

- UnitCardView는 정보 버튼·두 패널의 닫기 버튼·각 특성 버튼의 UIInputBinding을 한 번 연결한 뒤 보관한다. Clear·대상 교체·패널 닫기·입력 비활성화·Hide 경로의 ClosePanels에서 바인딩의 Hover도 해제한다.
- 표시/고정 상태를 먼저 지운 뒤 OnHoverExit를 전달해 콜백이 ClosePanels에 재진입하지 않게 했다. 특성 설명만 닫을 때에는 이전 특성 슬롯과 설명 닫기 버튼의 Hover만 정리해 정보창을 유지한다. 같은 슬롯의 특성 키가 바뀌는 갱신도 이 정리 경로를 사용한다.
- InfoPannel·AbilityPannel이 설정된 카드 구성은 해당 패널의 클릭 닫기용 Button을 필수 검사한다. 두 검사 모두 입력 추가·바인딩 전에 끝내며, 누락 시 필드·패널 객체를 포함한 InvalidOperationException을 발생시킨다. 패널 기능 자체가 없는 로비 구성에서는 닫기 버튼도 요구하지 않는 기존 선택 계약을 유지한다.
- 현재 UnitCard.prefab의 두 패널에 Button이 존재함을 정적으로 대조했다. Clear/Hide 중복 정리·HoverExit 재진입·특성 슬롯 변경·로비 선택 구성·필수 Button 누락 분기를 코드로 점검했다.
- Unity 6000.0.73f1의 Roslyn과 현재 프로젝트 Bee 응답 파일로 ProjectL.Systems 전체 컴파일 ExitCode 0, 로그 오류·경고 없음. 변경 파일 diff 공백 검사 통과. 산출물·응답 파일·로그는 세션 스크래치 ui-review-fixes-compile/에 있다. 이번 후속 수정의 실제 Play는 미실행이다.
- 변경 코드는 UnitCardView 한 파일에 한정한다. UI 등록·서비스 조립·Core·입력 계층·씬/프리팹은 수정하지 않았다. 기존 사용자 스테이징을 유지했고 커밋·push하지 않았다.
