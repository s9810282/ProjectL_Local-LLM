# 전투 View 5라운드 Task 계획 — 선택·입력 사실·조립 경계 (2026-10-26)

- 설계: [round5-design](../specs/2026-10-26-combat-view-stage-round5-design.md).
- **상태: V5-0~d 및 5라운드 완료(2026-10-26).** a/b/c 각각 사용자 컴파일·기존 시험·Unity 정상 보고를 받았고, W17~19 정적 검토와 d 통합 구조 인수·공유 문서 정리를 마쳤다. 최종 증거/한계·잔여는 §10, 실행 이력은 §8이다. 직접 컴파일·시험·Unity 실행과 구분한다. Q-V39~43 A/Q-T9 한정 승인 및 a의 별도 기대·준비 정정 허락을 지켰고, 새 시험 사례·커밋/push·외부 발송은 없다.
- 기준 HEAD: `11c07868`. 작성 시작 작업 트리는 깨끗했고 V5-0 시작에는 기존 문서 변경 5개만 있었다. 사용자에게 이 HEAD 이후의 결과를 확인하여 **컴파일·기존 EditMode·M01~M14 모두 정상** 보고를 받았다(2026-10-26). 자체 실행 증거로 바꾸지 않는다. 상세 기준선/호출부는 §9, 기존 확인 순서는 [4라운드 계획 §8](2026-10-25-combat-view-stage-round4-plan.md#8-unity-확인-순서-v4-n-사용자-실행)을 사용한다.
- 약어 V/F/T는 설계와 같다. Q-V39~43 A와 아래 §5의 명시된 변경 범위로 실행하며 Phase별 보고/Unity 확인 절차는 유지한다.

## 0. 실행 규칙과 선행조건

1. 최초 요청의 문서 작성/독립 검토를 마쳤고 후속 구현 지시를 받았다. `V5-0 → V5-a → V5-b → V5-c → V5-d` 순서로 실행한다. V5-0은 기준선/범위 기록만 하며 뒤 Phase의 코드를 함께 구현하지 않는다.
2. V5-0에서 V4-n 결과/기준판/허락 범위를 정리한다. 미확인을 통과로 바꾸거나 커밋만으로 인수 완료라고 판단하지 않는다. 기준 실패와 구조 변경에서 생긴 실패를 구분한다.
3. Phase마다 보고하고 사용자 Unity 확인 뒤 다음으로 간다. 큰 결정/계약/허용 파일이 설계와 달라지면 구현 전에 범위를 확인한다. 커밋·push는 사용자가 한다.
4. MSBuild/별도 컴파일러·새 `[Test]`/`[TestCase]`·새 회귀 사례를 만들거나 실행하지 않는다. 기존 시험의 필요한 조립/기계적 참조만 맞춘다. 컴파일/시험/재생은 사용자가 실행하고 실제 결과/미확인을 기록한다.
5. Q-V43에 따라 W17(a)·W18(b)·W19(c) 끝에 새 읽기 전용 검토를 각 1개 수행한다. 앞선 설계 검토는 구현 뒤 검토의 완료로 재사용하지 않는다. 검토는 SSOT/SOLID 5원칙·FW/JMH·기능 독립성과 해당 수명 회귀를 각각 본다. V5-0에서 이 검토를 앞당겨 실행하지 않는다.
6. 선택 쓰기 원본/가용성 원본·결과 확인/Gate는 중복하지 않는다. 생성은 기존 Factory, 표시는 기존 UIBase/UISystem/Display, 실행 순서와 수명은 현재 소유자에 둔다.
7. 새 .cs는 .meta와 함께 만들고 기존 MonoBehaviour GUID/네임스페이스·씬/Prefab/DB 값은 유지한다. Missing Script/참조 없는 에셋은 삭제하지 않는다.
8. Core/Buffer/Framework/GlobalSystem/ServiceKeys·FieldCamera/Architecture·다른 씬 영역은 변경하지 않는다. Factory는 우리 생성/연결 줄만 바꾸고 민형님 공유 메모에 기록한다. 타인 시험은 Q-T9 승인분만 수정한다.

## 1. Phase와 리뷰 지점

| Phase | 산출물 | 선행/허락 | 끝 검토/사용자 확인 |
|---|---|---|---|
| V5-0 | baseline·V4-n 결과·실제 호출/작성자/수정 범위 고정 | 별도 구현 지시, Q-V39~43/Q-T9 결정 | 사전 상태 보고 |
| V5-a | 선택 상태/읽기 계약·표시/입력 공유·가짜의 명시적 선택 | Q-V39, Q-T9 a/d | W17 새 검토 1개, 선택/UI/수명 |
| V5-b | FactsReader·위임·View 선택 명칭 변경 | a, Q-V40, Q-T9 b/d | W18 새 검토 1개, 사실/후처리/캐시 |
| V5-c | typed 운영 의존성·부분 시험 조립 분리 | b, Q-V41/42, Q-T9 c/d/e 해당분 | W19 새 검토 1개, 누락/실패 소유권 |
| V5-d | 통합 인수·공유/사용 안내·잔여 정리 | a~c 실제 결과 | 기존 기능/구조 계약 인수. 새 점수 측정 없음 |

## Phase V5-0 — 기준과 범위 고정

| Task | 내용 | 완료 조건 |
|---|---|---|
| V0.1 | HEAD/status와 기존 동시 변경 확인 | `11c07868` 이후 변경/기존 사용자 작업을 구분, 임의 복원 금지 |
| V0.2 | V4-n 컴파일·기존 EditMode·M01~M14 결과 정리 | 정상/실패/부분 확인/미확인과 모드·스테이지를 기록. 결과 미수집을 통과로 처리하지 않음 |
| V0.3 | Q-V39~43·Q-T9와 실제 수정 줄/작성자 확인 | 설계 A/다른 안, 허락된 고정물/기계적 참조, 허락 밖 단언을 구분 |
| V0.4 | 선택·ReadFacts·빈 범위 관측·CreateInteraction/확인창/AttachCreated·AttachDisplay 호출부 재조사 | 확정한 수정 파일과 기존 회귀를 기록. 동작 규칙/공용 계약 변경이 필요하면 재계획 |

V4-n에서 실패/구성 문제가 나오면 구조 작업에 섞지 않고 원인과 담당을 먼저 정리한다. 문서 초안 작성은 이 결과 수집과 독립적으로 가능하다.

## Phase V5-a — 선택 원본 분리 (W17)

| Task | 파일/접점 | 할 일/보존 계약 |
|---|---|---|
| Va.1 | 새 `V/Selection/CombatSelectionState.cs`·`ICombatSelectionReader.cs` + meta | 선택 ID 하나·Display가 고정한 완료 표시 목록의 읽기 참조·국면 수명. 살아 있는 Buffer 목록을 직접 보관하지 않음. 아군 행동 ID/점유 칸은 파생 읽기. Core 비용/자격 판정·Unity 의존 없음 |
| Va.2 | 새 `V/Selection/ICombatUnitInspection.cs` + meta, 기존 Hud 선언 | 계약만 새 폴더로 분리. Selection Reader/누름 입구 명시. UnitInfo MonoBehaviour 파일/GUID/네임스페이스는 유지 |
| Va.3 | `F/CombatSceneDisplayFactory.cs`, `V/Display/CombatSceneDisplay.cs` | 상태 한 개 생성/소유, UI는 Reader로 주입. Install/Refresh의 공통 완료 표시 적용 자리에서 외부/UI 콜백 전에 불변 CharacterData 참조 목록을 한 번 복사·보관하여 Selection/UnitInfo가 공유. 상태 동기화 뒤 UI/강조 갱신. Clear/Release/새 설치에서 참조 정리, Getter/Tick·UI별 중복 복사 없음 |
| Va.4 | `V/Display/Hud/CombatUnitInfo.cs` | `_selected` 원본/선택 쓰기 제거. `_shown`/슬라이드/비용 사유 캐시는 유지. UI Hide/Clear가 선택을 새로 쓰지 않도록 역할 구분 |
| Va.5 | `V/CombatViewConnection.cs`, `CombatViewSystem.AttachCreated/AttachDisplay/RouteInput/ReadFacts` | 두 Attach 입구의 선택 계약/Reader 참조를 명시하고 TakeDisplay → BeginUse → 첫 표시/입력 순서로 연결. Inspection 생략 기본 인자 제거. AttachCreated의 인수 전 반환과 AttachDisplay의 호출자 보유를 구별하여 조기/재진입/실패 시 기존 소유권 유지 |
| Va.6 | `F/CombatSceneInputFactory.CreateSceneInteraction`, `CombatViewSystem.ReadFacts`, `CombatInteraction`·`CombatPathInput`의 빈 범위 관측 | Facts가 먼저 읽은 선택 ID를 값으로 받는 관측으로 함께 변경. 관측은 전체 Facts/선택 공급자를 역호출하지 않음. 그 뒤 일반 Path 선택 공급자를 Facts 선택에 연결하여 Display 형식 검사/Empty 대체 제거. 관측 변경을 c로 미루지 않고 첫 Facts 읽기부터 비순환 보장 |
| Va.7 | 기존 Q-T9 a/d 고정물 | 직접 UnitInfo.SelectCell·옛 인터페이스·AttachCreated/AttachDisplay 호출을 같은 사용자 절차의 상태/표시 조립으로 맞춤. ViewSystem/MovementView/CoreStageCombatCycle의 AttachDisplay 준비도 명시. Fake의 선택 기본값 1 보존. 기존 기대/사례 유지 |
| Va.8 | W17/사용자 확인 | 선택·정보창·강조/행동 대상·가용성 키·완료 화면·Detach 회귀. 결과 보고 후 멈춤 |

**중간 완료 조건:** 입력/강조는 UnitInfo의 가변 선택에서 읽지 않는다. 적/빈 칸, 같은 아군 재Drag, 같은 진영 턴 선택 유지, 죽음/이탈 정리, 덱 토글·가림/재표시가 현재 동작과 같다. Buffer가 연출 중 갱신·정리되어도 다음 완료 표시 적용 전 선택/UI의 목록 읽기는 바뀌지 않는다. StopInput만으로 마지막 화면의 선택을 먼저 지우지 않는다. 두 Attach 입구에서 첫 표시 전 선택 참조가 연결되고, 첫 Facts 읽기/빈 범위 관측은 비순환이다.

## Phase V5-b — FactsReader와 View 명칭 (W18)

| Task | 파일/접점 | 할 일/보존 계약 |
|---|---|---|
| Vb.1 | 새 `V/Input/CombatInputFactsReader.cs` + meta | 현재 ReadFacts·현재 Preview 답 읽기를 이동. 선택 ID를 먼저 읽어 빈 범위 관측에 값으로 전달하는 a의 비순환 순서 보존. 읽기 전용, 매 호출 최신값, 상태/요청/결과 확인 쓰기 없음 |
| Vb.2 | `V/Input/ICombatSceneInputFactory.cs`, `F/CombatSceneFactory.cs`·`CombatSceneInputFactory.cs` | 기존 생성 계약에 Reader 생성 연결. 필수 고정 싱크/진행 객체·이름 있는 관측 공급자만 받음. 새 서비스/범용 Context 없음 |
| Vb.3 | `CombatViewSystem.BindServices/ReadFacts/CreateInput` | 현재 InputConnection 생성 뒤 Reader 한 개 보관. ReadFacts는 위임. 재바인딩/입력 재료 무효화, followUp/includeUnhandled 의미 보존 |
| Vb.4 | `V/Input/CombatInputFacts.cs`와 실제 View 소비 | ActiveActorId/activeActorId → SelectedActorId/selectedActorId. SceneInput·AttackTargetingInteraction·ActionAvailabilityTracker와 a에서 바뀐 Path 공급자를 함께 맞춤 |
| Vb.5 | Q-T9 b/d의 기존 참조/가짜 Factory | 10파일의 실제 멤버/이름 붙인 생성자 인자만 기계적 변경. 기존 Factory 가짜는 정상 Reader 생성에 위임하고 null Reader로 통과시키지 않음 |
| Vb.6 | W18/사용자 확인 | 결과 전/후·확인 전/후 재읽기, 전경/배경/가용성·튜토리얼/EndTurn·씬 재연결 확인. 결과 보고 후 멈춤 |

**중간 완료 조건:** 조정자에 기존 Facts 필드 조립이 남지 않고 Reader가 별도 캐시/판정식을 소유하지 않는다. 같은 Tick의 후처리/확인 뒤에도 새 사실을 읽는다. Core ActorId·HighlightLayer.ActiveActor·이전 진단 사실은 삭제/변경하지 않는다.

## Phase V5-c — 운영 의존성/시험 조립 (W19)

| Task | 파일/접점 | 할 일/보존 계약 |
|---|---|---|
| Vc.1 | `CombatSceneInputSources`·조정자의 생성 자리·입력 Factory | typed IUIConfirmationPresentation을 필수 재료로 연결. 이미 필수인 나머지 재료는 유지. 깊은 Text as 확인창 캐스팅 제거 |
| Vc.2 | `CombatPathInput`·운영 생성 접점 | 일반 Path 판단을 Facts의 SelectedActorId로 통일, 별도 optional readSelectedActor/Empty 기본값 제거. 빈 범위 관측은 a의 값 인자 경로를 유지하고 Facts를 역호출하지 않음. 이동 기대 문맥/승인 정책 유지 |
| Vc.3 | 운영 CreateSceneInput/CreateSceneInteraction | 필요한 선택/가용성/경로/공격/확인창을 생성 전에 진단하고 전부 명시. 생성 중 실패/Detach의 기존 반환·원인 예외 순서 보존 |
| Vc.4 | 새 `T/CombatInputTestFactory.cs` + meta (Q-T9 c) | 기존 부분 조립만 시험 어셈블리로 옮김. 새 시험/사례가 아님. 기존 고정물과 필요한 가짜/생략 의미를 명시 |
| Vc.5 | Q-T9 c/d의 기존 호출 | 부분 생성 API 호출을 시험 도우미로 변경. 운영 코드가 도우미/부분 경로를 부르지 않는지 조사한 뒤 옛 시험용 optional Factory 길 정리 |
| Vc.6 | Q-T9 e 해당 여부 | 실제 확인창 누락 진단 시점을 보는 기존 타인 시험이 있는지 확인. 기대 변경이 필요하면 정확한 사례/줄을 따로 확정하고 허락 전에는 바꾸지 않음 |
| Vc.7 | W19/사용자 확인 | 정상 배치 확인 Yes/No/취소·오류, 명령/Preview/슬롯 보존, 초기 실패/재진입 Detach·중복 Attach·UI Release 확인. 결과 보고 후 멈춤 |

**중간 완료 조건:** 운영 조립은 부분 시험용 optional 길/표시 형식 발견에 의존하지 않는다. 시험의 생략은 시험 도우미에서 명시하고 운영 누락을 정상으로 감추지 않는다. leaf의 의도적 선택 기능/정상 대기/해제는 유지한다.

## Phase V5-d — 통합 인수·공유·잔여

| Task | 내용 | 완료 조건 |
|---|---|---|
| Vd.1 | §6 회귀와 기존 시험/Unity 결과 취합 | baseline/변경 후 결과·부분 확인/미확인 구분. 기능 유지 확인 |
| Vd.2 | §7의 구조 계약 점검 | 원본/읽기/조립 분리와 대표 변경 시나리오의 실제 수정 범위 제시 |
| Vd.3 | 기존 공유 메모 갱신 | Core 경계 문서: 규칙/Buffer 변경 없음, 민형님: Factory/생성 실패 연결, 현강님: 정보창/선택/타인 시험, 호진님: 기존 Drag 고정물/가용성 유지. 모두 미전달 |
| Vd.4 | 안내/현재 상태/폴더 위치·실행 기록 갱신 | 실제 구현한 새 파일/소유권과 `.meta`, 확인/미확인·허락 대기를 명시 |
| Vd.5 | 완료/잔여 판정 | 세 목표와 기존 기능 인수 모두 충족해야 완료. Consumer/표면/폐지 호환 사슬/작은 공통화/측정은 후속 |

## 2. 변경 경계와 새 파일

| 종류 | 대상 |
|---|---|
| 새 운영 파일 | Selection/CombatSelectionState.cs, ICombatSelectionReader.cs, ICombatUnitInspection.cs; Input/CombatInputFactsReader.cs. 각각 .meta |
| 새 시험 도우미 | T/CombatInputTestFactory.cs + meta. Q-T9 c 승인 범위로 V5-c에서 생성 완료, Test/TestCase/사례 추가 없음 |
| 실제 수정 운영 접점 | UnitInfo·SceneDisplay·ViewConnection·ViewSystem·입력 Facts/실제 소비/Path/Sources/생성 계약, 기존 Factory 셋의 우리 연결 줄 |
| 문서 | 이 설계/계획, 기존 공유 메모·stage-input-guide·Script/README·CLAUDE의 현재 상태 |
| 제외 | Core/Buffer/Framework/GlobalSystem/ServiceKeys·다른 씬·FieldCamera/Architecture·Scene/Prefab/DB·예약 키 저장값/직렬화·별도 구조/기능 |

새 파일/인터페이스를 자동 서비스로 등록하지 않는다. 상태/읽기 계약 분리가 MonoBehaviour 이동·씬 참조 변경으로 번지면 이 계획 범위 밖이다.

## 3. 결정 상태

| 결정 | 현재 상태 |
|---|---|
| Q-V39 선택 생성/소유/읽기 계약 | A 승인, a 구현·W17·사용자 인수 완료 |
| Q-V40 Reader/읽기 시점/View 명칭 | A 승인, b 구현·W18·누락 using 수정 후 사용자 인수 완료 |
| Q-V41 typed 운영 의존성/조립 | A 승인, c 구현·W19·사용자 인수 완료 |
| Q-V42 기존 시험 한정 변경/도우미 | Q-T9 a~d 및 a의 별도 허락분만 적용. e 조사에서 추가 예외 단언 변경 대상 없음. 새 사례 없음 |
| Q-V43 W 단위 검토 | W17(a)·W18(b)·W19(c) 새 읽기 전용 검토 각 1개 완료. d는 통합 정리이며 추가 W 검토 없음 |

## 4. 기존 동작을 바꾸지 않을 항목

- 동일 필드 선택 유지/적 정보/빈 칸 해제·덱 핀 토글·UI 가림·입력 수명 보호.
- 가용성 Unknown/Pending/Ready·키/슬롯/늦은 답·첫 Drag·실제 공격 진입/모드 끄기·비용 안내.
- Core 공용 AP/비용/ActionUse·무선택 EndPlayerTurn·MoveOnly/밤 경계 회복·실제 Move 표시 완료 목표.
- 현재 P/S/결과 토큰·BeforeResult/AfterAcknowledged/FinalizeResult 순서·최종 화면·승패/포기/Run 복귀.
- UIId/부모/Policy/등록·개폐/슬라이드·형식/아이콘·FieldCamera/음향·대여 자원/생성 실패 소유권.
- 무동작 복귀 호환·빈 범위 진단·살아 있는 Forecast·예약 키/저장값. 이번 계획에서 삭제하지 않음.

## 5. 타인 시험 대응 Q-T9 (명시한 한정 범위 승인)

승인 근거는 4라운드 Q-T8이 아니라 이번 **"수정한 설계랑 Task 계획대로 V5-0부터 차례대로 진행해"**다. a~d의 명시된 조립/기계적 참조와 helper에 한정하며 e의 단언 변경은 포함하지 않는다. V5-0에서 실제 줄/작성자를 다시 확인해 §9.3에 기록했다. 우리 시험도 검증 의도를 바꾸거나 새 사례를 넣지 않는다.

| 항목 | 실제 후보 | 허용 범위 / 제외 |
|---|---|---|
| a | CombatUnitInfoTests, CombatViewSystemTests, HojinCombatAttackDragIntegrationTests, CombatMovementViewTests, CoreStageCombatCycleTests | 선택 상태/읽기/Inspection 주입·옛 Hud 네임스페이스·SelectCell/AttachCreated/AttachDisplay 호출 준비만. 추가 두 파일은 AttachDisplay의 명시적 Inspection 인자 연결에 한정. 선택 기본값/클릭·Drag·소유권·결과 단언 유지 |
| b | 아래 10파일 | ActiveActorId/activeActorId의 View Facts 멤버/이름 붙인 인자만 기계적 치환. 값/의미/사례/단언 변경 없음 |
| c | CombatInputRouterTests, CombatInteractionTests, CombatMovementFeedbackTests, CombatPathInputCleanupTests, CombatUnitRangeHoverTests, HojinCombatAttackDragTests 및 새 CombatInputTestFactory | 부분 생성 도우미 이동/호출, Path의 단일 Facts 선택, 필요한 명시적 가짜 준비만. 새 사례·가용성/최종 Gate 우회·검증 약화 없음 |
| d | CombatViewSystemTests의 FakeDisplay·입력 Factory/공통 조립, 실제 재사용 고정물 | Reader 생성 메서드·명시적 선택/확인창 가짜·필수 인자 연결만. 기존 정상/정리/재진입 기대 유지. 새 재사용 파일이 필요하면 파일/줄을 확정해 범위 추가 |
| e | 잘못된 조립의 늦은 실패를 직접 보는 기존 시험이 발견될 때 | **후보 조사만 제안**. 진단 시점/예외 단언 변경은 해당 사례/줄과 변경 이유를 특정한 별도 허락 필요 |

**2026-10-26 Q-T9 a 추가 허락:** V5-a 도중 현행 초기 무선택/빈 칸 해제와 정보창 시험의 옛 자동 선택 기대가 충돌함을 확인하여 사용자에게 해당 정정만 따로 물었고 **"위 기존 기대·준비 정정 허용"** 답변을 받았다. 초기 표시 시험은 첫 유닛을 명시 선택하도록 준비한다. 기존 Selection_KeepsFieldSelectionAndFallsBack은 Selection_KeepsFieldSelectionAndClearsOnEmpty로 이름을 맞추고 해제/숨김을 확인한다. SelectionReturnsToShownUnit_ReversesFromCurrentPosition은 null 대신 실제 원래 유닛 칸을 누른다. UI Clear 고정물은 Display 소유 선택의 정리를 함께 준비하며 기존 사례/특성 선언은 추가하지 않는다. 이 허락을 다른 타인 기대 변경에 확대하지 않는다.

Q-T9 b의 현재 실제 10파일:

1. AttackTrajectoryInteractionTests.cs
2. CombatInputPolicyTests.cs
3. CombatInteractionTests.cs
4. CombatMovementFeedbackTests.cs
5. CombatPathInputCleanupTests.cs
6. CombatUnitRangeHoverTests.cs
7. HojinCombatAttackDragIntegrationTests.cs
8. HojinCombatAttackDragTests.cs
9. PathInputTestFacts.cs
10. WallAttackInteractionTests.cs

UnitCardTraitContentTests·NightChecker 전용 시험·다른 씬 시험은 이번 변경 허락 목록이 아니다. 새 호출부/타인 기대가 발견되면 무단 확대하지 않는다. Q-T9 c의 helper 허용은 조립 코드에 한정하며 새 회귀 시험 허용을 뜻하지 않는다.

## 6. 기존 회귀/사용자 확인 매핑

| ID | 확인할 내용 | 기존 근거/범위 |
|---|---|---|
| R01 | A 재선택→Drag, B/적/빈 칸, 다중 점유 칸·즉시 강조/실제 actor 일치 | UnitInfo/Interaction/실제 씬, V4 M01 |
| R02 | 덱 핀·가림/재표시·A→B 슬라이드·행동 사유 | UnitInfo 기존 사례, V4 M03 |
| R03 | 같은 국면의 턴/결과 유지, 죽음/이탈/국면/새 연결 정리. 연출 중 Buffer 갱신·EndBattle과 완료 표시 스냅숏의 수명 구별 | 기존 UnitInfo/ViewSystem·Unity 수동 확인, V4 M13. 목록 래퍼/참조 복사의 정적 근거도 기록 |
| R04 | 첫 Drag·Unknown/Pending·선점/취소 뒤 회복·현재 Ready | Path/Interaction/호진님 Drag·Unity, V4 M04~M08 |
| R05 | 읽기 전/후 처리 순번·Preview 번호·followUp/includeUnhandled, 첫 Facts 읽기/빈 범위 관측의 비순환 | 기존 ViewSystem/Policy/Interaction·결과/명령 검사, 호출 그래프 정적 확인·Unity 수동 확인 |
| R06 | 정상 무전투/미연결/첫 표시 전/StopInput/종료 화면 유지 | ViewSystem 기존 수명/최종 결과 사례, 임의 새 선택 초기화 없음 |
| R07 | 같은 Runtime 재연결·서비스 재바인딩의 옛 선택/Reader 캡처 없음 | 기존 재연결 사례/수동. 직접 증거가 없으면 미확인 |
| R08 | UI 가림·모달·재생·명령 대기·적 응답 중 입력 | Router/Policy/실제 씬, V4 M02/M05 |
| R09 | 명시적 확인창 Yes/No/취소·누락/생성 실패의 자원 반환 | 기존 확인/생성/Detach 사례. 허락 없는 타인 단언 수정 금지 |
| R10 | 두 Attach 입구의 첫 표시 전 선택 연결, TakeDisplay 전/후 실패·중복 Attach·첫 Install 중 재진입 Detach | CombatViewSystemTests 기존 소유권·공통 고정물, MovementView/CoreStageCombatCycle의 기존 연결 사례 |
| R11 | 수동 종료·MoveOnly의 다음 턴/밤 경계 회복·실제 Move 후 통과 | V4 M07/M09/M11. Jobs/Store/목표 변경 없음 |
| R12 | 일반 승패·포기·최종 승리·최종 표시/씬 종료 | V4 M14, 기존 Run 목적지 유지 |

이 표는 새 시험 작성 지시가 아니다. 기존 시험 이름/단언과 실제 Unity 결과를 연결하며 증거 없는 경우 미확인으로 기록한다. 직접적인 Reader/선택 순수 단위 사례가 없어도 새 시험으로 채우지 않고 확인 공백을 남긴다.

## 7. 구조 인수 기준

1. CombatUnitInfo는 선택의 가변 원본을 갖지 않고 정보창/강조/입력의 선택 인스턴스가 같다. 완료 표시 목록은 Display가 한 번 고정하고 Selection/UI가 같은 참조를 읽는다. Buffer의 읽기 전용 래퍼를 시점 고정으로 간주하지 않는다.
2. Display 형식 검사/선택 공급자의 Empty fallback이 운영 경로에 없다. 정상 Empty와 의존성 누락은 구별한다.
3. Reader가 Facts 조합을 소유하고 Core/Buffer/Policy/선택/진행 상태는 복제하지 않는다. 선택 ID를 값으로 전달하는 빈 범위 관측은 전체 Facts를 역호출하지 않으며, a/b/c 각 경계에서 비순환이다. Tick 캐시·새 결과 상태/번호·새 서비스 없음.
4. 운영 전체 조립이 부분 시험 도우미를 호출하지 않고, 실제 필수 재료 누락은 알려진 오류로 진단한다. UI/자원 소유권 순서는 유지한다.
5. 설계 §6의 대표 변경 시나리오에서 실제 수정 파일/이해·검증 범위와 남는 공유 경계를 기록한다. 관련 없는 UI/음향/결과 재생까지 관성적으로 확대하지 않는다.
6. SSOT·SRP/OCP/LSP/ISP/DIP·FW-001~016·JMH/독립성 항목별 근거와 미확인을 기록한다. 줄 수/클래스 수/예상 점수를 인수 근거로 삼지 않는다.

## 8. 문서 작성/실행 기록

### 2026-10-26 — 사용자 요청으로 설계·Task 작성만

- 사용자의 남은 작업 1~3 묶음 요청을 5라운드로 명명했다. 현재 선택/표시/입력/Factory·기존 시험 참조를 읽고 Q-V39~43 A와 Q-T9 범위를 초안으로 작성했다.
- 선택 ID의 UI 소유, 이동 Factory의 형식 발견/Empty 공급자, 조정자의 Facts 조합, 이미 필수인 Sources, typed 확인창/부분 시험 조립의 실제 접점을 구분했다. FakeDisplay 선택 기본값과 첫 표시/실패 소유권·StopInput의 마지막 화면을 보존하도록 명시했다.
- V4-n은 완료로 바꾸지 않았다. 초안 작성 시점에는 V5-0~d 코드·새 .cs/.meta·기존 시험·에셋 구현 및 검토 에이전트 실행이 모두 미착수였다. MSBuild/Unity/시험·커밋/push·외부 발송 없음. **컴파일 미확인**.
- 현재 상태/4라운드 이월 연결은 문서로만 갱신한다. 정적 확인은 문서 링크·형식·허용 파일 범위이며 실행 증거가 아니다.

### 2026-10-26 — 별도 사용자 지시의 새 에이전트 설계 검토

- 새 읽기 전용 검토자 1개가 현재 코드와 초안을 독립 대조하여 P1 1건·P2 2건을 보고했다. 부모가 원문을 확인하고 설계 §10의 V5-R01~03으로 기록했다.
- V5-R01: Path의 Facts 선택 공급자와 기존 빈 범위 관측이 재귀가 되는 누락을 보완했다. Va.6에 Interaction/Path의 선택 ID 값 인자 관측을 함께 넣고 b/c·R05·구조 인수까지 비순환 순서를 연결했다.
- V5-R02: 살아 있는 CharacterBuffer 목록 래퍼를 완료 표시 목록으로 간주한 부분을 정정했다. Va.1/3에 Display 소유의 1회 참조 복사·Selection/UnitInfo 공유·정리를 넣고 R03에 수명 확인을 연결했다.
- V5-R03: AttachCreated뿐 아니라 AttachDisplay의 첫 표시 전 Inspection/Selection 주입을 Va.5/7에 넣었다. 인수 전 표시 반환 책임은 두 입구의 기존 의미를 보존한다. 호출부 조사에서 CombatMovementViewTests/CoreStageCombatCycleTests가 확인되어 Q-T9 a에 명시적 AttachDisplay 인자 연결만 후보로 추가했다. 허락 수집은 아직 아니다.
- 같은 검토자의 집중 재검토에서 3건 모두 문서상 해소됐고 추가 구체적 결함/모순은 확인되지 않았다. 부모의 정적 확인은 Markdown 5파일의 UTF-8/LF·공백/충돌 흔적·Round5 링크 6개와 변경 범위를 대상으로 했다. V5-0~d 구현·기존 시험 변경·새 helper 생성·W17~19 실행은 미착수다. MSBuild/Unity/시험·커밋/push·외부 발송 없음. **컴파일 미확인**, V4-n 실제 결과 미수집을 유지한다.

### 2026-10-26 — 순차 진행 지시와 V5-0 완료

- 사용자 "그럼 수정한 설계랑 Task 계획대로 V5-0부터 차례대로 진행해."에 따라 V0.1~4를 수행했다. 위 설계 검토 종료 기록의 미승인/미수집 상태는 이 시점의 승인과 사용자 보고로 갱신한다.
- HEAD `11c07868` 및 기존 문서 변경 5개를 확인했고 코드/시험/에셋의 변경은 없었다. Q-V39~43 A와 Q-T9의 한정 범위, 실제 호출부와 작성자 정보를 §9에 고정했다. 새 승인 질문이나 검토 에이전트 실행은 하지 않았다.
- V4-n 결과 질문에 사용자 "컴파일·기존 시험·M01~M14 모두 정상" 답변을 받았다. 이는 현재 HEAD 이후의 사용자 실행 결과이며 f616d5d9의 예전 baseline과 구별한다. 4라운드 현재 상태/결과표도 이 근거로 갱신했다.
- V5-0은 문서 기록만 했다. 코드/기존 시험/에셋·새 .cs/.meta/helper·Core/Buffer/Framework/GlobalSystem·Factory 연결은 수정하지 않았다. MSBuild/별도 컴파일러/Unity/시험·커밋/push·외부 발송도 하지 않았다. Phase 보고 뒤 멈추며 다음은 V5-a다.

### 2026-10-26 — V5-a 코드 구현·W17 진행

- 사용자 "V5-a 진행해."에 따라 Va.1~7을 구현했다. SelectionState/Reader/Inspection 3개와 cs meta·Selection 폴더 meta를 만들었다. Factory가 상태 하나를 생성하고 Display가 소유하며 정보창/Connection은 동일 읽기 참조만 보관한다.
- Display는 Install/Refresh의 완료 표시 목록을 한 번 복사·공유한다. 설치/정리/Release에서 선택과 보관 목록을 비우고 정보창은 빌린 상태를 쓰지 않는다. Hide·같은 턴/결과·같은 유닛 누름·선택 보호는 기존 계약을 유지한다.
- 두 Attach 입구 모두 Inspection을 명시하고 인수 전에 Selection을 검사한다. TakeDisplay의 마지막 대입/BeginUse/첫 표시 순서와 AttachCreated의 인수 전 반환·AttachDisplay의 호출자 보유를 보존했다. 기본 FakeDisplay 선택 1은 명시적인 Reader에 남겼다.
- ReadFacts는 선택 ID를 먼저 읽어 Interaction/Path 빈 범위 관측에 값으로 전달한다. 관측은 일반 선택 공급자/전체 Facts를 역호출하지 않고 ID 기반 개체 읽기를 사용한다. 일반 Path 공급자는 현행 Facts.ActiveActorId로 연결했다. FactsReader·명칭 변경·Path optional 인자 제거·typed 확인창/시험 도우미는 b/c에 남겼다.
- 기존 시험 수정은 Q-T9 a/d 5파일이다. UnitInfo 직접 선택을 상태 적용/정보 표시 준비로, 두 Attach와 옛 인터페이스 참조를 명시 계약으로 맞췄다. 정보창의 추가 기대/준비 정정은 위 별도 사용자 허락만 적용했다. 각 파일의 Test/TestCase 선언은 기준 HEAD와 동일(14/71/18/32/5)하며 새 사례는 없다.
- 새 읽기 전용 W17 검토 1개 진행 중이다. 부모는 허락 범위/시험 선언·새 순수 파일/메타 GUID·기계적 참조와 문서를 별도로 확인한다. W17 결과/수정과 Unity 인수 항목은 아래 후속 기록에 남긴다.
- 민형님 조립 메모 §28, 현강님 UI/시험 메모 §13을 갱신했다. 외부 미전달. Core/Buffer/Framework/GlobalSystem/ServiceKeys·FieldCamera/Architecture·씬/Prefab/DB는 수정하지 않았다. MSBuild/별도 컴파일러·Unity/시험·커밋/push 실행 없음. **변경 후 컴파일 미확인**이다.

### 2026-10-26 — W17 완료·V5-a 사용자 인수 대기

- 새 읽기 전용 검토자 1개가 현재 구현과 승인 설계를 대조하여 **보고할 P1/P2 실제 결함 없음**으로 보고했다. 선택 SSOT, SRP/OCP/LSP/ISP/DIP·FW/JMH·기능 독립성, 목록 고정, UI/선택/StopInput·Release/재진입 수명, 두 Attach 실패 소유권, Facts/Path 비순환, 승인된 기존 시험 의미를 확인했다. 앞선 설계 검토를 재사용하지 않았다. 리뷰어의 파일 변경/컴파일/시험/추가 에이전트 실행은 없다.
- 부모 정적 확인은 수정 범위/기계적 참조·기존 Test/TestCase 동일·새 순수 코드/메타·기존 직렬화 필드/GUID·문서 형식/링크다. 이는 Unity 성공 증거가 아니다. 새 선택 폴더/3개 스크립트의 meta를 함께 인수해야 한다.
- Va.1~7 코드와 Va.8의 W17 검토는 완료, Va.8의 사용자 컴파일·기존 EditMode·Unity 확인은 대기다. **컴파일 미확인**이며 V5-0의 정상 보고를 재사용하지 않는다. 여기서 멈추고 b는 구현하지 않았다.

#### V5-a Unity 확인 순서 (사용자 실행)

1. Unity 임포트/컴파일 오류가 없는지 확인한 뒤 기존 EditMode를 실행한다. 특히 CombatUnitInfoTests, CombatViewSystemTests, CombatMovementViewTests, CoreStageCombatCycleTests, HojinCombatAttackDragIntegrationTests의 기존 사례를 확인한다. 시험 수/실패가 있으면 원문을 받으며 자동 선택을 운영 코드에 복구하지 않는다.
2. Combat 전투 진입 직후 무선택을 확인한다. A 클릭→A 재누름→Drag/취소, B/적/빈 칸 순서로 누른다. 아군 정보·강조·실제 행동 대상이 일치하고 같은 유닛 재누름은 선택 유지, 적은 정보만, 빈 칸은 선택 해제여야 한다.
3. 덱 핀/토글·UI 가림·정보창 Hide 뒤 재검사, A→B 슬라이드 중 A 재선택을 확인한다. Hide가 선택을 새로 쓰거나 Refresh가 카드 목록을 다시 닫지 않아야 한다. 필드 밖/UI/재생/명령 대기 중의 기존 선택 보호를 확인한다.
4. A를 고른 상태로 다른 아군/적 턴·일반 결과를 진행한다. 살아 있는 선택은 유지하고, 선택 유닛의 이동/사망/필드 이탈·국면 전환은 완료 표시 시점에 정보/강조/선택에 반영돼야 한다. 연출 중 최신 Buffer 위치를 먼저 강조하지 않아야 한다.
5. 배치→전투→다음 배치/전투 및 종료/씬 재진입을 확인한다. 최종 결과 표시 중 마지막 화면을 유지하고 해제/새 설치 뒤 이전 선택/정보/강조가 남지 않아야 한다. 정상 승패/포기 목적지는 기존 정책을 유지한다.
6. 첫 이동 Drag/취소/재시도와 공격/E·턴 종료/Space를 확인한다. 첫 Facts 읽기/빈 범위 응답에서 재귀·멈춤이 없고 기존 가용성 Ready/슬롯/명령 정책이 유지돼야 한다. 직접 재현하지 못한 수명/빈 범위 조건은 미확인으로 분리한다.

결과는 `컴파일 / 기존 시험 / 2~6 정상 또는 실패·미확인`으로 받는다. baseline 성공과 구분하며 사용자의 해당 Phase 확인 뒤 V5-b로 간다.

### 2026-10-26 — V5-a 사용자 인수 확인·V5-b 코드/W18 진행

- 사용자 "V5-b 진행해."에 따라 Vb.1~5를 구현했다. 선행 V5-a 변경 후 결과를 별도로 물어 **"컴파일·기존 시험·Unity 확인 모두 정상"** 보고를 받았다. 이는 a 워킹트리에 대한 사용자 결과로 Va.8 인수 완료를 기록하며 V5-0/HEAD 이전 보고와 구분한다. 직접 실행/개별 로그 확인은 하지 않았다.
- 새 Input/CombatInputFactsReader.cs와 meta를 추가했다. ReadFacts/현재 Preview 번호 검사 조합을 이동하고 조정자의 기존 입구는 Reader 위임으로 남겼다. 입력/결과 확인·선택/진행 상태·Core Buffer 쓰기·새 Policy·Tick 캐시는 추가하지 않았다.
- 기존 ICombatSceneInputFactory → CombatSceneFactory → 정적 CombatSceneInputFactory에 생성 메서드를 연결했다. 기존 InputConnection 생성 뒤 Reader를 받는다. 고정 Sink/진행 객체와 이름 있는 읽기 공급자만 전달하며 새 서비스/키/Builder/Context/Resolver는 없다. 재바인딩 시작에서 Reader와 입력 재료를 함께 무효화하며 InitData의 View 표 교체는 Reader를 바꾸지 않는다.
- Reader는 현재 Connection을 매번 얻는다. 정상 무전투/미연결/첫 표시 전과 followUp/includeUnhandled 의미, 현재 요청과 맞는 Preview 응답, 선택 ID를 값으로 전달하는 빈 범위 비순환 관측을 유지한다. 바인딩 전 잘못된 읽기는 명시적 상태 오류, null 조립 결과는 명시적 구성 오류다. IsPresenting은 기존 공용 CombatProgressFacts.IsUnpresented를 그대로 사용한다.
- View Facts/실제 소비 4파일과 운영 Path 선택 공급자의 ActiveActorId/activeActorId를 SelectedActorId/selectedActorId로 바꿨다. Core ActorId·진영 턴·HighlightLayer.ActiveActor·기존 진단 사실은 변경하지 않았다.
- Q-T9 b의 기존 10파일은 이름만 기계적으로 맞췄다. 명칭 역치환/줄바꿈 정규화 후 시작 시점의 전체 파일 해시와 일치하며 기대/값/사례/특성 선언 변경은 없다. 실제 ICombatSceneInputFactory 시험 가짜는 검색상 없어 추가 수정이 필요하지 않았다.
- 시작 시 이미 변경된 FMODStudioCache.asset은 보존한다. Core/Buffer/Framework/GlobalSystem/ServiceKeys·FieldCamera/Architecture·씬/Prefab/DB와 a의 선택/표시 소유권 구현은 수정하지 않았다. c의 typed 확인창·Path optional 선택 인자 제거·시험 도우미는 미구현이다.
- W18 새 읽기 전용 검토 1개 진행 중. 부모는 기계적 시험 동등성/범위·GUID·이름 참조와 문서를 별도로 확인한다. 민형님 §29·현강님 §14 공유 메모는 미전달이다. **b 변경 후 컴파일·기존 시험·Unity 미확인**, MSBuild/별도 컴파일러·Unity/시험·커밋/push·외부 발송 없음.

### 2026-10-26 — W18 완료·V5-b 사용자 인수 대기

- 새 읽기 전용 검토자 1개가 V5-b에서 **보고할 P1/P2 실제 결함 없음**으로 보고했다. 매 호출 최신 읽기, followUp/includeUnhandled/현재 Preview 번호, 결과 확인 전후 순서, 빈 범위 비순환, 현재 Connection 조회, 미연결/첫 표시/종료/재바인딩/InitData/Abort·Detach/생성 실패를 대조했다. SSOT·SRP/OCP/LSP/ISP/DIP·FW/JMH·기능 독립성에서도 구체 결함이 없었다. 이전 W17/설계 검토를 재사용하지 않았으며 파일 편집/컴파일/시험/추가 에이전트 실행은 없다.
- 부모는 시험 10파일의 역치환 내용 동등성과 이름 참조/생성 계약·새 .cs/meta/기존 GUID·변경 범위/문서 형식을 정적으로 확인했다. FMOD 시작 변경은 보존한다. 이 검사는 컴파일/Unity 실행 증거가 아니다.
- Vb.1~5와 Vb.6의 W18은 완료, Vb.6의 사용자 컴파일·기존 EditMode·Unity는 대기다. **b 변경 후 컴파일 미확인**이며 a의 정상 보고를 재사용하지 않는다. Phase 보고 뒤 멈추며 c는 구현하지 않았다.

#### V5-b Unity 확인 순서 (사용자 실행)

1. 새 CombatInputFactsReader.cs/meta의 임포트 후 컴파일 오류를 확인하고 기존 EditMode를 실행한다. 특히 기존 ViewSystem/Policy/Interaction·이동/호버/궤적·호진님 Drag 시험을 확인한다. 명칭 변경으로 인한 오류가 있으면 전체 파일/줄/메시지를 받는다.
2. 배치→전투 진입, A/B/적/빈 칸 선택과 이동/공격을 확인한다. 선택 정보/실제 대상·현재 Ready/비용 안내가 유지되고, 첫 Facts 읽기/빈 범위 관측에 재귀·멈춤이 없어야 한다.
3. Drag/조준 취소·재시도, UI/모달·결과 재생·적 응답 중 입력과 수동 턴 종료를 확인한다. 결과 처리/확인 뒤 최신 사실을 읽고 Preview 늦은 답이 현재 요청으로 적용되거나 자동 행동으로 이어지지 않아야 한다. 의도적 번호 불일치/지연을 직접 만들지 못했으면 그 세부 조건은 미확인으로 남긴다.
4. 같은 Runtime에서 Combat 이탈→재진입, 다음 배치/전투, 정상 종료/포기 뒤 복귀를 확인한다. 옛 씬 선택/입력 상태를 붙잡지 않고 최종 화면/입력 해제의 기존 순서를 유지해야 한다.
5. 기존 튜토리얼 제한·이동 실습·후처리 후 회복을 확인한다. followUp이 제출 닫힘/모달/순번 제한을 우회하지 않아야 한다. 서비스 재바인딩/InitData·확인 전후의 세부 경로는 기존 시험·실제 재현 근거를 구별하고 직접 증거가 없으면 미확인으로 기록한다.

결과는 `컴파일 / 기존 시험 / 2~5 정상 또는 실패·미확인`으로 받으며 해당 Phase 사용자 확인 뒤 V5-c로 간다.

### 2026-10-26 — V5-b 컴파일 CS0103 보고·using 수정

- 사용자에게 CombatInputFactsReader.cs(55,55)의 `StagePhase` CS0103을 보고받았다. Reader 이동 때 Contracts.Common using을 누락한 것이 원인이며 실제 선언은 CoreCaculator/Contracts/Common/StagePhase.cs다.
- View의 Reader에 `using ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Contracts.Common;` 한 줄을 추가했다. Core 파일/타입·Facts/입력 동작·시험·에셋은 수정하지 않았다.
- 앞선 W18/부모 정적 검사에서 이 누락을 발견하지 못했다. 정적 검토 결과와 별도로 V5-b 컴파일 실패를 기록하며, **수정 후 컴파일·기존 시험·Unity는 재확인 대기**다. MSBuild/별도 컴파일러·Unity/시험·커밋/push는 실행하지 않았다. V5-c는 진행하지 않았다.

### 2026-10-26 — V5-b 수정 후 사용자 인수·V5-c 코드/W19 진행

- 사용자 **"컴파일·기존 시험·Unity 확인 모두 정상. V5-c 진행해."** 보고를 받았다. 직전 b의 StagePhase 누락 using 수정 후 결과이며 Vb.6 사용자 인수 완료로 기록한다. 현재 c 변경의 실행 증거로 재사용하지 않는다.
- Vc.1: BindServices에서 텍스트의 IUIConfirmationPresentation 기능을 확인해 typed 필드로 보관하고, 필수 Sources.Confirmation으로 넘겼다. PostBindService도 같은 BindServices 경계를 거친다. Factory의 Text as 확인창 캐스팅은 제거했다. 재바인딩 때 Reader/입력 재료와 typed 확인창 참조를 무효화한다. 새 Global 서비스/키/등록/공용 UI 변경은 없다.
- Vc.2: Path의 optional readSelectedActor/Empty 기본값과 필드를 제거하고 일반 개체 선택은 Facts.SelectedActorId로 통일했다. Facts가 선택 ID를 값으로 전달하는 빈 범위 관측은 유지해 전체 Facts를 역호출하지 않는다. 기존 Path 생성 3시험 접점과 운영 생성 접점을 함께 맞췄다.
- Vc.3~5: 운영 CreateSceneInteraction은 전체 필수 인자를 먼저 검사하고 모든 기능을 명시 조립한다. 옛 runtime CreateInteraction의 부분 조립은 새 시험 어셈블리 CombatInputTestFactory.cs/meta로 옮겼다. 기존 6파일의 부분 호출/Path 준비만 맞추고 runtime은 시험 도우미를 참조하지 않는다. SceneFactory의 옛 부분 API 설명도 현재 경계로 정정했다.
- Q-T9 d: 공통 CombatViewSystemTests.SilentText가 확인창 계약을 명시 구현한다. 수신 대상별 열림/닫힘만 보관하고 자동 Yes/No/명령 제출은 하지 않는다. 이를 재사용하는 MovementView/DisplayReuse/CoreStage 고정물은 별도 파일 수정 없이 typed 전체 조립을 따른다.
- Vc.6/Q-T9 e: 현재 호출/기대 조사에서 늦은 확인창 누락의 진단 시점·예외 메시지를 변경해야 하는 기존 단언은 발견하지 않았다. 기존 독립 leaf의 의도적 표시 생략/대기/해제 계약은 유지한다. 기존 시험 7파일의 Test/TestCase 선언과 Assert 포함 줄은 시작 내용과 동일하며 실제 기대/검증 의도/사례는 변경하지 않았다. 새 시험/사례는 없다.
- 새 W19 읽기 전용 검토 1개 진행 중이다. 부모는 특성/단언·부분 도우미 이동/참조·meta/GUID·변경 경계와 문서 형식을 확인한다. 민형님 §30·현강님 §15 공유 기록은 미전달이다.
- 기존 a/b 변경과 FMOD 캐시는 보존한다. Core/Buffer/Framework/GlobalSystem/ServiceKeys·FieldCamera/Architecture·씬/Prefab/DB 수정, MSBuild/별도 컴파일러·Unity/시험·커밋/push·외부 발송 없음. **c 변경 후 컴파일·기존 시험·Unity 미확인**, d는 미착수다.

### 2026-10-26 — W19 완료·V5-c 사용자 인수 대기

- 새 읽기 전용 검토자 1개가 **보고할 P1/P2 실제 결함 없음**으로 보고했다. 확인창 capability 바인딩 검사/typed 전달·운영 필수 인자 선검사, 시험 도우미의 Gate/가용성/정상·실패 의미, Path 단일 Facts/비순환, 생성 실패/Detach·Preview·최종 Gate/AP/목표·Yes/No/취소/오류를 대조했다. SSOT·SRP/OCP/LSP/ISP/DIP·FW/JMH·기능 독립성에서 구체 위반이 없었다. 앞선 검토를 재사용하지 않았으며 리뷰어의 파일 변경/컴파일/시험/추가 에이전트 실행은 없다.
- 부모 정적 확인은 기존 7파일의 특성/Assert 포함 줄 동등성, 도우미의 기존 부분 조립 메서드 이동, 런타임의 시험 참조/옛 API·선택 공급자·깊은 캐스팅 제거, 새 meta/GUID·수정 범위·문서 형식이다. 컴파일/동작 성공 증거가 아니다.
- Vc.1~6과 Vc.7의 W19는 완료, Vc.7 사용자 컴파일·기존 EditMode·Unity 인수는 대기다. **c 변경 후 컴파일 미확인**이며 b의 정상 보고를 재사용하지 않는다. 여기서 멈추고 d는 진행하지 않았다.

#### V5-c Unity 확인 순서 (사용자 실행)

1. 새 CombatInputTestFactory.cs/meta 임포트 후 컴파일과 기존 EditMode를 확인한다. 특히 Router/Interaction/MovementFeedback/PathInputCleanup/UnitRangeHover/호진님 Drag 및 ViewSystem의 기존 조립·해제/실패 사례를 확인한다. 새 시험은 작성하지 않았다.
2. 메인은 배치하고 참가 서브 일부를 미배치한 상태에서 시작/G를 눌러 확인창을 연다. No/취소로 명령 없이 닫히고 조작이 회복되는지, 재요청 Yes에서 현재 조건으로 한 번만 진입하는지 확인한다. 전부 배치된 정상 진입과 UI Submit도 확인한다.
3. 확인창 열림 중 다른 필드/공격·이동·턴 입력이 막히고 닫힘/StopInput/씬 이탈 때 창/고스트/입력 잔재가 정리되는지 확인한다. 예전 receiver나 늦은 응답이 새 연결의 진입/명령을 만들지 않아야 한다.
4. A/B/적/빈 칸 선택, 첫 이동 Drag/우클릭 취소/재시도와 공격 Drag·모드 끄기·실제 이동을 확인한다. Facts와 다른 선택 원본이 없고 빈 범위 관측에서 재귀하지 않으며 Ready/슬롯·최종 Gate·AP/목표 판정은 기존과 같아야 한다.
5. 배치/전투/다음 배치·종료/재진입을 확인한다. 초기 실패·재진입 Detach/중복 Attach/반환 오류의 세부 경로는 기존 시험 결과와 실제 재현 근거를 구별하고 직접 증거가 없으면 미확인으로 남긴다. 확인창 누락을 만들려고 운영 Scene/Prefab을 임의 변경하지 않는다.

결과는 `컴파일 / 기존 시험 / 2~5 정상 또는 실패·미확인`으로 받으며 이 Phase 사용자 확인 뒤 V5-d 통합 인수·공유로 간다.

### 2026-10-26 — V5-c 사용자 인수·V5-d 통합 정리 완료

- 사용자 **"컴파일·기존 시험·Unity 확인 모두 정상. V5-d 진행해."** 보고를 받았다. c 변경 뒤 Vc.7 사용자 인수 완료다. V5-0/a/b의 정상 보고를 c 실행 증거로 재사용하지 않았다.
- Vd.1: §6의 기존 회귀를 Phase별 정상 보고와 대조했다. 기능 인수는 사용자 보고 기준 완료이며, 개별 로그/건수·희귀 재진입/재바인딩의 별도 실행 증거는 제공받지 않았다. 상세 범위/공백은 §10.1에 남긴다.
- Vd.2: 실제 선택/공유 표시 목록, Reader의 호출 방향·현재 연결 읽기, typed 필수 조립·시험 호출 분리 및 두 Attach의 반환 책임을 재확인했다. SSOT·SOLID 5원칙·FW/JMH·대표 변경 범위의 통합 근거는 §10.2~10.3이다. 새로운 구현 결함이나 계획 변경 필요는 발견하지 않았다.
- Vd.3~4: Core 경계 §39, 민형님 §31, 현강님 §16, 호진님 V5-d 메모, 입력 안내 §2.5·파일 위치, Script README와 UnitInfo 인접 변경 기록·현재 상태를 갱신했다. 공유 메모는 **모두 미전달**이다. 과거 실행 이력은 보존하고 문서 상단에서 현재 결과를 구별했다.
- Vd.5: 승인한 세 구조 목표와 사용자 기능 인수를 충족하여 **5라운드 완료**로 판정한다. 구조 점수 재측정·후속 구현은 하지 않았다. 이월 목록은 §10.4다.
- d는 문서 10개만 변경했다. 시작/종료의 변경·미추적 비문서 파일 42개 경로/SHA256이 모두 일치하여 a/b/c 운영·시험 및 기존 FMOD 캐시 보존을 확인했다. 문서 UTF-8/LF·치환 문자 없음, 새 링크 대상/앵커와 `git diff --check`를 확인했다. 문서 전체 링크 대조에서 발견한 기존 부재 참조 3개(CLAUDE의 옛 Run summary 설계/계획, Core 전달 문서의 옛 CF7 XML)는 이번 추가 링크가 아니므로 변경하지 않았다. 직접 MSBuild/별도 컴파일러·Unity/시험·커밋/push·외부 발송은 없다.

## 9. V5-0 확정 기준선과 수정 범위 (2026-10-26)

### 9.1 V0.1~4 결과

| Task | 결과/근거 |
|---|---|
| V0.1 | HEAD `11c07868`(`[용근] 새 Core 규칙에 전투 View 대응`). 시작 시 추적 변경은 CLAUDE·4라운드 설계/계획 3개, 미추적은 5라운드 설계/계획 2개. 전부 기존 문서 작업이며 유지했다. 런타임/시험/에셋 변경 없음 |
| V0.2 | 이 HEAD 이후 V4-n 결과 질문에 사용자 **"컴파일·기존 시험·M01~M14 모두 정상"** 답변. 현재 기준선 정상으로 채택. 직접 실행/로그 검증을 한 것은 아니며 시험 건수·실행 모드/스테이지 개별 값은 제공받지 않아 추정하지 않음 |
| V0.3 | 수정한 계획대로의 순차 진행 지시를 Q-V39~43 A 및 Q-T9 a~d의 명시된 준비/기계적 변경·helper, e의 조사 범위 승인으로 기록. 기존 15파일의 실제 접점과 blame 작성자는 §9.3. 기대값/검증 의도·새 사례·허락 밖 파일/단언 변경은 포함하지 않음 |
| V0.4 | 선택/두 Attach/빈 범위 관측/Reader·명칭/부분 조립/확인창의 현재 접점을 재검색. 설계 검토에서 고친 3건의 전제와 일치, 계획 밖 새 수정 접점은 발견하지 않음. 10파일 명칭·6파일 부분 생성 목록 일치 |

**현재 기준선은 사용자 확인 정상이다.** V5-a 이후 코드 변경의 컴파일·기존 시험·Unity 결과는 각 Phase에서 새로 받아야 하며 V5-0의 정상 보고를 변경 후 결과로 재사용하지 않는다. 이 Phase에서는 새 구현이 없으므로 추가 런타임 시험을 만들거나 실행하지 않았다.

### 9.2 운영 접점과 Phase 경계

| 접점 | 현재 코드 근거 / 예정 범위 |
|---|---|
| 선택 원본/표현 | UnitInfo:68~72/138~168/341~361의 선택 쓰기·읽기/옛 인터페이스, Display:227~240/339~347의 UI 선택 읽기, DisplayFactory:88/158의 생성. a에서 상태/읽기 계약/공유 표시 목록으로 옮김 |
| 완료 표시 목록 | Display.Refresh:201~227, CharacterBuffer:90/105/594/636의 살아 있는 목록 래퍼·교체/정리. Buffer는 읽기 근거일 뿐 수정 대상 아님. a의 Display 소유 목록 고정·공유·정리 계약 유지 |
| 두 Attach 입구 | ViewSystem:462/519, Connection:58/70. a에서 명시적 Inspection/Selection을 첫 표시 전에 연결. AttachCreated 인수 전 반환/AttachDisplay 호출자 보유·인수 뒤 Detach 구별 |
| Facts/빈 범위 관측 | ViewSystem:875~913/898, Interaction:129, Path:70/217. a에서 먼저 읽은 선택 ID를 값 인자로 전달해 역호출을 끊고, b는 순서를 Reader로 이동, c는 일반 Path의 optional 선택 공급자만 제거 |
| Factory/명칭 | InputFactory:118의 Display 형식 발견/Empty 대체는 a, ICombatSceneInputFactory:14 및 SceneFactory:29~30의 생성 접점·Facts 실제 소비 명칭은 b. 우리 연결 줄만 수정하고 해당 Phase의 민형님 메모에 기록 |
| 확인창/부분 조립 | ViewSystem:178의 운영 확인창 검증과 InputFactory:77의 깊은 캐스팅, :137의 부분 CreateInteraction. c에서 typed 재료/시험 도우미로 분리. 늦은 실패의 타인 단언 변경 여부는 Q-T9 e로 따로 특정 |

SSOT·SOLID 5원칙·FW/JMH·기능 독립성의 승인 설계/인수 기준은 그대로 유지한다. V5-0은 새 구조가 구현되거나 검증됐다는 판정이 아니며 W17~19를 대신하지 않는다. Core/Buffer/Framework/GlobalSystem/ServiceKeys·다른 씬·FieldCamera/Architecture는 수정하지 않는다.

### 9.3 기존 시험 접점/작성자 고정

아래는 HEAD `11c07868`의 실제 참조와 `git blame --line-porcelain`에 나온 **해당 접점 줄의 작성자 이름**이다. 파일 전체의 단독 소유자를 뜻하지 않는다. 줄 번호는 구현 전 후보 위치이며 그 위치의 단언/기대값을 바꾸라는 허락이 아니다. 수정은 §5의 조립/기계적 범위에 한정한다. 파일은 모두 `T/` 아래이며 별도 시험 파일 생성은 c의 CombatInputTestFactory.cs와 meta만 허용된다(새 사례 없음).

| 파일 | Q-T9 | 현재 접점 줄 | 접점 작성자 |
|---|---|---|---|
| CombatUnitInfoTests.cs | a | Configure 48/78/104, 선택/표시 준비 60~65/144~331 | GwonCheonmyeong, Twinsgm |
| CombatViewSystemTests.cs | a/d | AttachDisplay 99~1133/1168/1375, AttachCreated 1157/1182/1199/1217/1226/1241, FakeDisplay 1585/1588 | Droute, GwonCheonmyeong |
| HojinCombatAttackDragIntegrationTests.cs | a/b | Facts 인자 62, 옛 Inspection 138 | GwonCheonmyeong |
| CombatMovementViewTests.cs | a | AttachDisplay 254/328/495의 인자 연결만 | GwonCheonmyeong |
| CoreStageCombatCycleTests.cs | a | AttachDisplay 229의 인자 연결만 | GwonCheonmyeong |
| AttackTrajectoryInteractionTests.cs | b | 44/59/133/167 | Droute |
| CombatInputPolicyTests.cs | b | 212 | Droute |
| CombatInteractionTests.cs | b/c | 75/518/1041/1350/1364/1371 | Droute, Twinsgm |
| CombatMovementFeedbackTests.cs | b/c | 418/419 | Droute, GwonCheonmyeong |
| CombatPathInputCleanupTests.cs | b/c | 20/21 | Droute, GwonCheonmyeong |
| CombatUnitRangeHoverTests.cs | b/c | 292/323/348/471 | Droute, Twinsgm |
| HojinCombatAttackDragTests.cs | b/c | 129/350/351/353/400 | Droute, GwonCheonmyeong, Leehojin |
| PathInputTestFacts.cs | b | 36 | GwonCheonmyeong |
| WallAttackInteractionTests.cs | b | 244/268 | GwonCheonmyeong |
| CombatInputRouterTests.cs | c | 47/227/269 | Droute |

공통 FakeDisplay를 상속하는 MovementView/CoreStageCombatCycle 고정물도 명시적 선택 준비를 따라야 하지만, 위 두 파일은 AttachDisplay 인자 연결을 넘어서 바꾸지 않는다. 테스트별 기대/사례는 유지하며 추가 접점이나 잘못된 조립의 예외 단언 변경이 필요해지면 구현 전에 정확한 파일/사례/줄을 특정한다.

## 10. V5-d 최종 인수와 잔여 (2026-10-26)

### 10.1 기능 결과와 증거 한계 — Vd.1

| 시점 | 확인 결과/증거 |
|---|---|
| V5-0 / `11c07868` 기준선 | 사용자 “컴파일·기존 시험·M01~M14 모두 정상”. V4-n 기능 인수 완료 |
| V5-a 뒤 | 사용자 “컴파일·기존 시험·Unity 확인 모두 정상”. a 확인 순서 2~6과 기존 시험 인수 |
| V5-b 뒤 | CS0103 StagePhase 누락 보고 → View Reader에 Common using 1줄 수정 → 사용자 “컴파일·기존 시험·Unity 확인 모두 정상. V5-c 진행해.”. **수정 전 실패와 수정 후 성공을 구분** |
| V5-c 뒤 | 사용자 “컴파일·기존 시험·Unity 확인 모두 정상. V5-d 진행해.”. c 확인 순서 2~5와 기존 시험 인수 |
| V5-d | 문서 정리와 아래 코드/경계 정적 대조. 운영 코드·시험 추가 변경 없음, 직접 컴파일·시험·Unity 실행 없음 |

| 기존 회귀 매핑 | 통합 인수 근거/직접 증거의 한계 |
|---|---|
| R01~R02 선택/Drag·정보창/덱 핀 | a의 사용자 정상 보고 + 선택 상태/정보창/강조가 같은 Reader를 읽는 정적 근거 |
| R03 표시 수명/무효화 | a 정상 보고 + 완료 표시마다 CharacterData 참조 목록 한 번 복사·같은 목록 공유·Display의 정리 책임. 연출 중 EndBattle 강제 주입의 개별 로그는 없음 |
| R04~R05 범위/가용성·순번/Preview/비순환 | a/b/c 정상 보고 + 기존 시험. 선택 ID 값 전달 → Path 관측의 Facts 역호출 없음, Preview 요청 번호 일치·followUp/includeUnhandled 투영 보존을 정적 대조 |
| R06 정상 대기/종료 화면 | a/b 정상 보고 + 연결 없음의 정상 미준비·StopInput과 Display 선택 정리 분리. 최종 결과 소비/표시 순서 변경 없음 |
| R07 같은 Runtime 재연결/재바인딩 | 사용자 일반 Unity/기존 시험 정상. Reader가 현재 연결을 매번 읽고 BindServices에서 Reader/Sources를 무효화하는 정적 근거 있음. **동일 Runtime의 서비스 재바인딩·InitData 교체를 따로 실행한 증거는 미확인** |
| R08 입력 보호/모달 | b/c 정상 보고 + 기존 Gate/Policy/순번/모달 원본·수신 경로 유지 |
| R09~R10 확인창/실패/두 Attach | c 정상 보고와 기존 시험 + typed 확인창/필수 인자 검사·인수 전후 반환 책임 정적 대조. 강제 누락/각 단계 예외·첫 Install 재진입을 각각 실행한 로그는 없음 |
| R11 MoveOnly 회복/실제 이동 | 기준선 M11 및 a/b/c 정상 보고. 기존 BlockedInputReport 사실을 Reader가 읽고 Jobs/Store/목표·정책은 유지 |
| R12 승패/포기/최종 표시/Run 복귀 | 기준선 M14 및 a/b/c 정상 보고. 결과 Consumer/이탈/Run 목적지 코드는 이번에 수정하지 않음 |

개별 시험 건수·모드/스테이지·드문 오류 주입을 실행했다고 추정하지 않는다. 직접 Reader/Selection 전용 단위 사례도 추가하지 않았다. 위 공백은 증거 범위의 한계로 남기며, 보고된 기능 실패나 새 Core 요청으로 분류하지 않는다. W17~19의 “보고할 P1/P2 없음”은 정적 검토 결과다. W18이 놓친 using 오류와 실제 사용자 컴파일 수정 이력도 유지한다.

### 10.2 구조 인수 — Vd.2

아래 경로는 §0의 V/F/T 약어다. 기존 클래스 전체의 무결성을 다시 보증하는 것이 아니라 이번 변경의 소유권과 호출 경계를 판정한다.

| 기준 | 현재 코드 근거/판정 |
|---|---|
| SSOT / §7.1~2 | `V/Selection/CombatSelectionState`의 `_selected` 하나가 원본. `F/CombatSceneDisplayFactory`가 생성하고 Display가 소유한다. `Display.RefreshCore/SnapshotCharacters`가 불변 CharacterData 참조 목록을 한 번 고정하여 Selection과 UnitInfo에 공유한다. UI는 Reader만 빌리며 `Clear`가 선택을 쓰지 않는다. 정상 무선택/적의 Actor Empty와 누락된 Inspection/Reader 예외를 구별. 선택 공급자의 Display 형식 검사/무조건 Empty 대체 없음 |
| SRP | SelectionState는 선택/무효화, UnitInfo는 표시/슬라이드/비용 표현, InputFactsReader는 사실 조합, Factory는 생성/주입. System의 실행 순서와 결과 소비는 남아 있으며 결과 Consumer 분리는 후속 |
| OCP | Reader/Facts/실제 소비자에서 입력 사실을 확장할 수 있고, 표시 표현은 UnitInfo에서 고칠 수 있다. 새 기능에 필요한 계약/조립 수정은 남으므로 모든 확장이 무수정이라는 의미는 아님 |
| LSP | 실제와 FakeDisplay가 명시적 Inspection/Selection Reader를 제공한다. 적 정보/무선택의 Empty 의미를 유지하고, Fake 기본 선택은 시험의 명시적 준비다. SilentText는 확인창 계약을 명시하되 자동 승인하지 않음 |
| ISP | 정보창·Facts는 `ICombatSelectionReader`, 입력 선택 전달은 `ICombatUnitInspection`. Reader에 전체 조정자/서비스 resolver를 넘기지 않고 기존 Sink/진행 객체와 이름 있는 관측 공급자만 주입 |
| DIP | 선택 읽기/확인창을 계약으로 연결하고 Core/InGame DB를 직접 조회하지 않는다. 기존 View 내부 협력 객체의 구체 의존성은 유지; 필요 없는 서비스/범용 Context를 추가하지 않음 |
| Reader / §7.3 | `Input/CombatInputFactsReader.Read`가 매번 현재 Connection/Buffer를 읽는다. 선택 ID → `CombatInteraction.HasCurrentAcceptedEmptyMovementRange(id)` → Path의 `FindActor(id)/RangeFactsOf`는 Facts를 역호출하지 않는다. 일반 Path는 Facts.SelectedActorId 한 곳. Tick 캐시·새 순번·새 규칙 저장소 없음. BindServices에서 Reader/Sources 교체, InitData는 표시 재료 무효화 |
| 전체/부분 조립 / §7.4 | `F/CombatSceneInputFactory.CreateSceneInteraction`이 필수 재료를 먼저 검사하고 전체 기능을 생성한다. Sources.Confirmation은 바인딩에서 확인된 typed 재료다. Runtime의 부분 CreateInteraction API 삭제, 기존 부분 생성은 `T/CombatInputTestFactory`에만 있음. Runtime→Tests 참조 없음 |
| FW-001~004/016 · JMH 생성/소유 | 새 서비스/모듈/ServiceKey·Builder/Group Export 없음. 기존 Factory/BindServices/Attach/Detach 수명을 사용. 두 Attach는 TakeDisplay→BeginUse→첫 표시 순서. AttachCreated 인수 전 ReleaseUnowned와 AttachDisplay 호출자 보유, 인수 뒤 Detach 책임을 보존 |
| FW-005~009 · 입력/UI | Configure/Submit·슬롯/Layer·기존 Gate, UIInputBinding·UISystem·UIBase 등록/부모/Policy/이력을 변경하지 않음. 새 화면/버튼 이벤트 구독 없음. 필수 구성 오류와 정상 무전투/미연결/반복 해제를 구분 |
| FW-010~015 · 렌더/DB/이벤트/Pool/Queue | 계약/구현 변경 없음. 기존 Display Clear/Release와 실패 반환을 유지. Core/Buffer/Framework/GlobalSystem·씬/Prefab/DB/FieldCamera/Architecture 변경 없음 |
| JMH 역할/추상화 · §7.5~6 | 실제 선택 수명·표현·사실 읽기·생성을 분리했으며 추가 전역 서비스/포괄 조정자를 만들지 않음. 아래 실제 접점으로 기능 독립성을 판단. 줄 수·클래스 수·예상 점수는 인수에 사용하지 않음 |

### 10.3 대표 변경의 실제 경계 — Vd.2

| 다음 변경 시나리오 | 수정/이해할 현재 파일·검증 범위 | 함께 바꿀 이유가 없는 부분/남는 공통 경계 |
|---|---|---|
| 정보창 슬라이드·비용 배치 | `V/Display/Hud/CombatUnitInfo.cs`와 관련 카드 표현, 기존 정보창·덱 핀 확인 | SelectionState/Reader/Core 규칙·음향·결과 Consumer 수정 불필요. UIBase/카드/가용성의 공개 표시 계약은 이해 필요 |
| 선택/무효화 정책 | `V/Selection/CombatSelectionState.cs`, 필요한 경우 `Display/CombatSceneDisplay.cs`의 적용 순서, 기존 UnitInfo/ViewSystem·입력 선택 회귀 | Core 계산/소리/결과 재생 내부까지 수정할 이유 없음. 정보/강조/실제 actor가 같은 Reader라는 통합 확인은 필요 |
| 입력 사실 추가 | `V/Input/CombatInputFactsReader.cs`, `CombatInputFacts.cs`, 실제 소비자. 새 원본 필요 때 `ICombatSceneInputFactory`/SceneFactory/InputFactory 및 System의 연결 줄 | System의 결과 처리·UI 슬라이드 내부 수정 불필요. P/S·Preview·입력 수명 공통 사실 변경이면 통합 회귀 필요 |
| 기능 의존성 교체 | `F/CombatSceneInputFactory.cs`, 필요 시 `CombatSceneInputSources`와 바인딩 연결, 해당 기능/영향받는 T 고정물 | Global/루트 등록·무관 기능 구현 수정 불필요. c에서 실제로 6파일 호출/Path 준비와 공통 SilentText만 맞췄고 타 기능 단언은 유지 |
| 공통 수명/Preview 계약 변경 | 연결·Reader·가용성·경로/공격·기존 수명 회귀를 함께 확인 | 독립성을 이유로 공유 계약의 통합 검증을 생략할 수 없음 |

a에서 Selection/Display/UI/Attach/Path의 연결을 함께 옮겼고, b에서 Reader 조립과 View Facts 소비 이름을 함께 맞췄다. 이는 원본을 이동하는 이번 변경의 실제 영향이다. 이후 해당 기능의 표현 변경까지 같은 넓이의 수정이 필요하다는 뜻은 아니다. c의 공통 Fake는 여러 통합 고정물에서 쓰므로 해당 기존 시험의 실행 범위도 남는다.

### 10.4 완료/공유/후속 — Vd.3~5

- **완료:** 선택 원본 분리, 매 판단 FactsReader/SelectedActorId 정리, typed 운영 의존성·부분 시험 조립 분리. a~c 사용자 기능 인수와 통합 구조 인수를 함께 충족했다. V5-d는 문서 정리 뒤 멈춘다.
- **신규 파일:** 운영 `V/Selection/CombatSelectionState.cs`·`ICombatSelectionReader.cs`·`ICombatUnitInspection.cs`, `V/Input/CombatInputFactsReader.cs`의 4개, 시험 조립 `T/CombatInputTestFactory.cs` 1개. 각 `.cs.meta` 5개와 `Selection.meta` 1개. 기존 MonoBehaviour 네임스페이스/씬 직렬화/GUID 변경 없음.
- **타인 시험:** Q-T9 한정 준비/기계적 참조·helper와 a에서 별도 승인받은 선택 기대/준비 정정만 적용. c의 예외 시점 단언 추가 변경 대상 없음. 새 Test/TestCase/회귀 사례 없음. 승인은 이후 임의 단언 변경에 재사용하지 않는다.
- **공유 준비 완료·모두 미전달:** [Core §39](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#39-전투-view-5라운드--소비-구조-정리-2026-10-26-미전달), [민형님 §31](../handoffs/2026-10-15-combat-scene-display-factory-framework-share.md#31-2026-10-26-v5-d--5라운드-최종-조립-인계-미전달), [현강님 §16](../handoffs/2026-10-22-combat-view-structure-hyeongang-share.md#16-2026-10-26-v5-d--선택정보창기존-시험-최종-인계-미전달), [호진님 V5-d](../handoffs/2026-10-20-tutorial-t7-combat-input-handoff-Hojin.md#2026-10-26-v5-d--입력-사실조립과-기존-drag-고정물-미전달). 추가 Core API 요청 없음.

| 남은 구조 작업 | 이번 완료와의 구분 / 다음에 필요한 범위 |
|---|---|
| 결과 Consumer 분리 | System에 남은 결과 관측/소비·확인/후처리 순서. 기존 Playback/Progress/오류 경계·마지막 결과 표시 계약을 먼저 설계하고 기존 회귀 범위를 정함 |
| 필드 표면·칸 높이 원본 정리 | Field 표시/매핑/유닛·경로 표면의 중복 경계 조사. FieldCamera/다른 작업자 영역에 닿는 수정은 별도 지시/조율 필요 |
| 폐지 기능의 호환 사슬 정리 | 남아 있는 빈 범위 진단/옛 Return 관련 호환 경로의 실제 소비 조사. 예약 키·저장값·씬 참조/옛 에셋을 무단 삭제하지 않음. 단순 미사용 판단으로 일괄 제거하지 않음 |
| 작은 공통화 | 실제 중복/같은 변경 이유가 있는 부분만 개별 조사·설계. 새 포괄 서비스/기능 가방으로 합치지 않음 |
| 구조 재평가·기기/규모별 측정 | 이번에는 새 점수/A·B/성능 측정을 하지 않음. 필요 시 기준판·현재판·동일 시나리오/규모·실측 기준을 따로 정함 |

위 목록은 다음 라운드 후보이며 구현 승인/새 Task 착수를 뜻하지 않는다. Core가 보류한 효과 규칙, 항상 이동 불가능한 실습 데이터의 안내/재시도 정책 같은 외부 후속도 기존 상태를 유지한다. **사용자가 커밋·push·외부 전달한다.**
