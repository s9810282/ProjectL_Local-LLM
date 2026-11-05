# 전투 View 단계 4라운드 Task 계획 — 새 Core 대응 우선 (2026-10-25)

- 설계: [round4-design](../specs/2026-10-25-combat-view-stage-round4-design.md).
- **상태: 2026-10-26 V4-n 기능 인수 완료(사용자 실행 보고).** V5-0에서 `11c07868` 이후 **"컴파일·기존 시험·M01~M14 모두 정상"** 답변을 받았다. a/d/e/f/g/h 코드·W13~W16 정적 검토와 m 문서 정리를 포함해 Core 대응 완료로 기록한다. 결과 근거는 §8.4이며 직접 실행/개별 로그 확인과 구분한다. 사용자 결정 "4라운드에서는 구조 개선보다는 Core 변경 대응을 우선으로 하자"를 반영했고, 후속 "수정한대로 구현 차례대로 진행해"로 수정안의 순차 구현이 승인됐다.
- 이번 완료 목표는 새 Core 규칙의 기능 인수다. 독립적 구조 개선과 블라인드 구조 측정은 후속 라운드로 이월한다.
- 조사 기준 `6425ec18`·`130b3308`(Combat 코드 동일). V4-0 구현 기준은 `f616d5d9`이다. 초기 평가는 단일 정적 평가 8.3/10으로 이미 수행했으며 반복 평가를 시작 조건으로 요구하지 않는다.
- Q-V29~33·Q-V38은 A, Q-T8 a~e는 명시한 한정 범위로 승인됐다(2026-10-26). 비용 안내의 기존 기대 문자열 6개는 후속 명시 허락으로 f/g에 추가했다. Q-V34~36은 후속으로 이월한다. W 끝 검토와 새 시험 금지의 기존 방침은 승계한다. 사용자 Unity 확인은 별도 기록한다.
- 약어: 설계의 V/F/G/T와 같다. `S/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageInput/`, `L/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Tutorial/`.

## 0. 진행 규칙

1. 구현은 별도 진행 지시 뒤 Phase 단위로 한다. Phase마다 보고하고 사용자의 Unity 확인 뒤 다음 단계로 간다. 커밋·push는 사용자가 한다.
2. MSBuild와 새 `[Test]`·`[TestCase]`/새 회귀 사례 추가는 하지 않는다. 기존 시험의 필요한 호출·고정물·승인된 의미 변경만 맞춘다. 실제 시험 실행과 수동 확인/미확인을 분리 기록한다.
3. 생성자/인터페이스/파일 위치를 전수 정리하지 않는다. Core 대응에 필요한 변경만 기존 조립·입력·표시 접점에 넣는다. 관련 없는 필수화/이름 변경/죽은 API 삭제는 이월한다.
4. Core/Buffer/Framework/ServiceKeys·FieldCamera/Architecture·다른 씬 영역은 바꾸지 않는다. Global UI·튜토리얼·공용 입력·타인 시험은 허락분만 변경한다.
5. 새 Tracker/표시 변환은 `.meta`와 함께 만들고 기존 GUID는 유지한다. Missing Script 에셋·참조 없는 에셋·빈 Ballista 보존 컴포넌트는 삭제하지 않는다.
6. 검토는 W 끝에 한다. W13(a) 하나, W14(d/e) 셋(요청 수명/입력/독립성), W15(f/g) 하나, W16(h) 하나, 마지막 통합은 기능 인수 확인이다. 큰 구조 변경이 필요해지면 먼저 재계획한다.

**실행 순서: `V4-0 → V4-a → V4-d → V4-e → V4-f → V4-g → V4-h → V4-m → V4-n`.** 빠진 ID는 이월한 작업이며 다른 의미로 재사용하지 않는다. f/g/h 일부가 외부 허락 대기면 의존 없는 기능을 진행할 수 있지만 대기 항목을 완료로 표기하지 않는다.

## 1. Phase와 선행조건

| Phase | 이번 산출물 | 선행/결정 |
|---|---|---|
| V4-0 | 기준 판·병합 검증 상태·실제 허용 파일 확인 | 별도 구현 지시 |
| V4-a | 기존 선택 구조의 재누름/Drag·즉시 강조 수정 | Q-V29, Q-T8 a |
| V4-d | Core 가용성 Tracker·슬롯·현재 수명 연결 | a |
| V4-e | 버튼/키/이동 입구·거절 안내 | d, Q-T8 e의 실제 시험 준비 변경 허락 |
| V4-f | 기존 UI의 진영/행동/특성 상태 소비 | e, Q-V30·31, Q-T8 b |
| V4-g | 이동 실습의 수동 턴 종료·밤 경계 회복 | e, Q-V32·38, Q-T8 c |
| V4-h | 폐지 기능 사용자 접점의 구성/동작 문제 | Q-V33, Q-T8 d |
| V4-m | Core 대응 공유·사용 안내·이월 목록 | 실제 진행한 코드 Phase |
| V4-n | 새 Core 통합 기능 인수 | m, 미확인/대기 항목 확인 |

## Phase V4-0 — 기준·현상 확인

| Task | 내용 | 완료/기록 |
|---|---|---|
| V0.1 | HEAD/status·현재 관련 변경 확인 | 확인: `f616d5d9`. 추가 Core/맵 편집기 병합의 영향과 기존 문서 변경을 §6에 기록. FMOD 캐시 보존/범위 제외 |
| V0.2 | 사용자 Unity 컴파일·기존 EditMode·DB 에셋 교차 검증·Combat 진입 증거 확인 | 10-26 사용자 "컴파일·기존 시험·Combat 진입 모두 정상" 확인. V4-a 변경 전 baseline으로 기록. DataBaseAssetTests 개별 결과는 별도 제시되지 않았고 이번 DB 에셋 변경은 없음. 옛 결과/Editor.log를 현재 통과 증거로 대체하지 않음 |
| V0.3 | 선택 재누름/강조·행동 사용·밤8/적응답·이동 실습의 현재 현상 확인 | 코드상 위험 재확인. Unity 재현/미재현 판정은 미확인. 점수 재측정 없음 |
| V0.4 | Q-V29~33·38·Q-T8 결정과 실제 수정 줄의 작성자 확인 | 확인: 10-26 수정안 구현 승인. 현재 선택 줄/기존 선택 시험의 현강님·천명님 작성 이력 확인. 기존 새 시험 금지 유지 |

## Phase V4-a — 현재 구조에서 선택/Drag/강조 맞추기 (W13)

**상태:** Va.1~5 코드 완료, Va.6 W13 새 에이전트 1개의 정적 검토에서 높음·중간 결함 없음. 사용자 "아하 진행해"·"V4-d 진행해" 지시로 다음 Phase에 진입했다. 변경 후 **컴파일/기존 시험/수동 재생 성공을 별도로 보고받은 것은 아니다.**

| Task | 파일 | 내용 |
|---|---|---|
| Va.1 | `V/Display/Hud/CombatUnitInfo.cs` | 현재 `_selected` 원본 유지. Q-V29 A면 동일 필드 유닛 선택 유지·빈 칸 해제로 클릭 정책만 수정 |
| Va.2 | `V/CombatViewSystem.RouteInput`, 기존 `CombatInputPolicy`·Facts 주석 | 공격/기존 경로 지정/UI/모달/결과 재생·후처리/적 턴에서 선택 변경 차단. 공통 선택 자격은 Policy.CanSelectUnit, 선행 미처리 명령은 현재 Facts에서 읽음. 외부 선택 표시 콜백 뒤 같은 연결인지 검사 |
| Va.3 | `V/Display/CombatSceneDisplay.cs` | InspectUnit 직후 현재 선택으로 강조를 즉시 맞춤. 완료 표시 캐릭터 캐시 재사용, 전체 Refresh/결과 확인 금지 |
| Va.4 | 기존 운영 선택 연결 | `ICombatUnitInspection`과 path의 현재 선택 공급자를 유지. 실제 운영 누락은 연결 때 명확히 진단하되 생성 계약 전체 필수화는 하지 않음 |
| Va.5 | `T/CombatUnitInfoTests.cs`·공통 FakeDisplay/실제 닿는 시험 | 승인된 필드 재클릭 기대만 변경. 덱 토글·핀·슬라이드·기존 공격 Drag 기대는 유지 |
| Va.6 | W13 검토/수동 | 같은 유닛 재Drag, A→B/적/빈 칸, 즉시 강조, 경로 지정 중 선택 유지, UI 가림, 결과/씬 해제 중 입력 확인 |

**이번에 만들지 않는 것:** CombatSelectionState·선택 전용 새 계약·CreateSceneInput 서명 변경. 선택의 UI 종속은 이월 부채로 기록한다.

## Phase V4-d — Core 행동 가능성 요청/응답 (W14 앞)

**상태:** Vd.1~6 코드 완료. 변경 후 **컴파일 미확인**, Unity 확인 대기. W14 묶음 검토 3개는 계획대로 V4-e 끝에 완료했다(§6). W13 검토를 이 변경의 검토 결과로 재사용하지 않는다.

| Task | 파일 | 내용 |
|---|---|---|
| Vd.1 | 새 `V/Input/Interaction/CombatActionAvailabilityTracker.cs`(+meta) | Unknown/Pending/Ready, 키(전투·진영 턴·결과·선택), 요청 번호/슬롯/응답 검증. 실패 응답도 보관 |
| Vd.2 | `CombatSceneInput`, `F/CombatSceneInputFactory.cs` | 연결 입력에 Tracker 생성/보관/정리. Read/WriteSink의 기존 Preview 길 사용. 경로 입력에는 배경 범위 우선순위를 맞추는 용도로 같은 Tracker를 빌려줌 |
| Vd.3 | `CombatInputPolicy` | ActionAvailability의 Combat 단계·MoveOnly의 읽기 전용 조회 허용. 배경 조회로 튜토리얼 막힘 보고 없음 |
| Vd.4 | 기존 `CombatViewSystem.ReadFacts/TickCore` | 기존 메서드 위치를 유지하며 키 조립·Tracker 자기 응답 소비→최신 Facts→Interaction Tick 연결. Tracker/Facts 순환 호출 금지 |
| Vd.5 | 기존 범위/호버 요청 입구·`CombatPreviewSlotRule` | 전경 우선, 안정 시 선택 가용성→이동 모드 범위→호버. 미응답 타인 슬롯 덮어쓰기/매 Tick 재요청 방지. 새 일반 목적 큐 없음 |
| Vd.6 | 기존 BeforeResult/AfterAcknowledged/Stop 접점 | 새 결과 관측 즉시 무효화, 확인 완료 뒤 재조회. 선택/죽음/턴/전투 교체·정지의 늦은 답 무시. 기존 FinalizeResult 순서 유지 |

**수동:** 같은 턴 B가 AP 사용→A의 Ready 폐기, A→B→A, 슬롯 상실/다른 유닛/옛 턴 응답, 실패 응답 반복 요청 없음, 첫 Drag가 Tracker 대기로 막히지 않음. FactsReader/Consumer 분리는 하지 않는다.

## Phase V4-e — 사용자 행동과 실패 안내 (W14 끝)

**상태:** Ve.1~5 코드 반영·Ve.6 새 에이전트 셋 정적 검토 완료. 중간(P2) 1건을 수정하고 해당 검토자의 재검토에서 해소를 확인했다. 남은 높음/중간 결함 없음. **컴파일 미확인**, Unity/EditMode 대기. 여기서 Phase 보고 후 멈춘다.

| Task | 파일 | 내용 |
|---|---|---|
| Ve.1 | `CombatActionButtons.Read`, `AttackTargetingInteraction.CanEnter`, 기존 Facts/입력 연결 | 버튼과 키가 같은 현재 Ready.CanAttack을 소비. 확인 중 새 공격 진입 없음·늦은 자동 실행 없음, 모드 끄기는 허용 |
| Ve.2 | `CombatPathInput`, 기존 이동/공격 입구 | 유효 CanMove=false면 실제 시도 안내. Unknown 이동은 기존 Core 범위/상세 Preview 승인 경로 유지. 기존 Move 기대 문맥·공격 전투/턴 검사 보존 |
| Ve.3 | `CombatActionNotice` | ActionAlreadyUsed/ActionDeferred/WrongController/StaleTurn/StaleContext 안내. 실제 시도/최종 거절에서만, Poll/호버 팝업 없음 |
| Ve.4 | EndTurn/공용 AP/비용 미리보기 접점 | 무선택 수동 턴 종료·AP=0 자동 종료 없음 유지. Core 비용/행동 판정 재계산 금지 |
| Ve.5 | 기존 Policy/Router/Interaction/Notice·공격 Drag 시험 | 현재 함수 변경에 필요한 호출/고정물과 승인된 기대만 수정. Q-T8 e의 기존 공격 진입 준비에 현재 키의 Ready를 마련한다. 관계없는 생성자 null/이름 일괄 정리 없음 |
| Ve.6 | W14 검토 | d/e의 stale 응답·슬롯·입력 동작·Core 소비/중복 판정 여부를 셋으로 확인 |

**수동:** Available/Moved/Attacked/Deferred, AP 부족, 같은 턴 복수 유닛, 옛 공격/이동 승인, 비용 미리보기 취소, 무선택/0 AP 수동 종료, 공격 모드 끄기.

## Phase V4-f — 기존 UI에서 새 규칙 설명 (W15 앞)

**시작 조건:** Q-V30·31·Q-T8 b 허락. 허락이 없으면 이 부분은 담당자 인계 대기로 남긴다.

**상태:** Vf.1~4 코드 반영. 사용자 "V4-f 진행해"로 진입했으며 변경 후 **컴파일 미확인**, Unity/EditMode 대기. W15 독립 검토 1개는 V4-g 끝에 완료했다(§6). W14 검토를 이 변경의 검토 결과로 재사용하지 않는다.

| Task | 파일 | 내용 |
|---|---|---|
| Vf.1 | `CombatUnitInfo`, `CombatSceneDisplay`, `ICombatBattleDisplay`, `CombatSceneInput`, 기존 System/Factory 연결 줄 | 현재 키의 Ready/대기 해제를 필요한 표시 메서드로 전달. 표시 중 개체·선택·키가 일치할 때만 적용하고 `ApplyCharacter`/슬라이드 완료/재표시에 비용+상태를 함께 재구성. 작은 문구 변환이 필요하면 `CombatActionStateText.cs`(+meta)만 추가 |
| Vf.2 | `G/Screens/Stage/NightCheckerUI.cs` | 현재 progressText에 진영/준비/완료 플레이어 턴 표시. StagePhase/ControllerPhase도 캐시 비교. 적 응답8/8은 밤 완료가 아님 |
| Vf.3 | `G/Screens/Global/UnitCardTraitContent.cs` | 공개된 전체/부분 사용 불가 상태 반영. 정적 TraitKey 경로의 활성 여부 단정 금지 |
| Vf.4 | 기존 등록/시험/기록 | UIId/부모/Policy·Open/Hide·AP 표시 시점 유지. UnitCardTraitContent/UnitInfo 시험 허락분만. NightChecker 전용 시험은 현재 못 찾았으므로 수동 필수 |

새 화면·부대 전체 배지·필수 씬 참조·UI 등록 체계 변경은 하지 않는다. 상태 표시만 추가하며 선택 소유권은 그대로다.

## Phase V4-g — 이동 실습 회복 (W15 끝)

**시작 조건:** Q-V32·38·Q-T8 c 결정. 밤 경계 회복은 새 검토에서 찾은 추가 정책이므로 수동 턴 종료 허락만으로 구현하지 않는다.

**상태:** 사용자 "V4-g 진행해"로 Q-V32·38 A/Q-T8 c의 승인 범위를 구현했다. Vg.1~3 코드 반영, Vg.4 새 에이전트 1개의 W15(f/g) 정적 검토에서 추가 결함 없음. **컴파일 미확인**, Unity/EditMode 및 실제 회복 확인 대기. 여기서 Phase 보고 후 멈춘다.

| Task | 파일 | 내용 |
|---|---|---|
| Vg.1 | `CombatInputPolicy.AllowsTutorialCommand`·기존 전투 진입 접점·Facts/튜토리얼 읽기 연결 | 승인된 Q-V32 A로 MoveOnly에서 Move+수동 EndPlayerTurn 허용. Q-V38 A로 같은 전투의 미완료 실습에서만 Deployment의 정상 EnterCombat 허용. 제한 유지 구간과 구별할 현재 실습 목표 대기 사실만 전달. 정상 Gate/PlayerInput/S=P 조건 유지 |
| Vg.2 | `L/TutorialFactsReader.cs` | 실제 수락된 플레이어 이동의 표시 완료로만 실습 통과. 옛 Core 활성 유닛 주석 정정. 목표/Jobs/Store 변경 없음 |
| Vg.3 | 기존 TutorialInput/Policy 시험 | 실제 정책 변경에 닿는 승인된 기대만 맞춤. 밤 경계는 기존 시험에 해당 사례가 없으면 수동 확인하고 새 사례를 추가하지 않음. HasCurrentAcceptedEmptyMovementRange와 관련 추적 API의 광범위 삭제는 이월 |
| Vg.4 | W15 검토/수동 | 무선택/AP부족/행동사용/보류→수동종료→다음턴 이동. 미이동 상태로 8번째 적 응답→Deployment→합의한 경로로 회복→실제 이동. 설명 Layer 독점, 금지 공격 보고, 이동 전 통과 없음 |

항상 이동 불가능한 튜토리얼 데이터는 별도 잔여로 보고한다. View가 성공 결과를 만들어 통과시키지 않는다.

## Phase V4-h — 폐지 기능의 사용자 접점만 (W16)

**상태:** 사용자 "V4-h 진행해"로 Q-V33 A/Q-T8 d의 승인 범위를 구현했다. Vh.1~3 코드/현재 안내 반영, Vh.4 기존 시험 대응·새 읽기 전용 에이전트 1개 W16 정적 검토에서 추가 결함 없음. **컴파일 미확인**, Unity/EditMode 대기. 여기서 Phase 보고 후 멈춘다.

| Task | 파일 | 내용 |
|---|---|---|
| Vh.1 | `CombatActionPanel` | 복귀 버튼 숨김/무동작 유지, 숨긴 returnButton의 필수 참조 검사 제거. 폐지 슬롯이 null이면 Bind/입력 Attach/표시에서도 해당 슬롯만 건너뛰어, 미연결이어도 정상 진입 |
| Vh.2 | `S/CombatInputController.cs` | 체크포인트 복귀 키의 실제 소비 해제. 배치 ReturnUnit/회수/취소·남은 Pointer/Submit 보존 |
| Vh.3 | 실제 수정한 코드 주석·현재 사용 안내 | 새 규칙에 없는 복귀/타임라인/유닛 AP 표현 정정. 과거 실행 기록은 보존 |
| Vh.4 | 기존 관련 입력/버튼 시험·검토 | 실제 소비가 없어진 동작만 승인 범위에서 조정. 예약 KeyBinding/액션 저장 기대 유지. 복귀 요청 생성 없음과 나머지 입력 확인 |

**수정 제외:** IStageInputReceiver/슬롯/Router enum/BindActions/CanUseReturn의 일괄 삭제, 씬/프리팹 객체 대청소, KeyBindingAction.Return·저장값·PlayerInputActions 예약 액션 삭제. 구조 잔재와 설정 노출 인계는 후속 목록에 남긴다.

## Phase V4-m — Core 대응 공유·안내

**상태:** 사용자 "V4-m 공유·안내 정리 진행해"로 Vm.1~4 문서를 정리했다. 코드/시험/에셋 변경 없이 실제 소비 계약·작업자별 공유·사용 안내·기능 미확인/구조 이월을 §7에 모았다. 공유 문서는 **미전달**, 변경 후 **컴파일 미확인**이며 V4-n은 아직 시작하지 않았다. 여기서 Phase 보고 후 멈춘다.

| Task | 내용 |
|---|---|
| Vm.1 | 실제 Core 소비 방식·새 API 필요 여부를 Core 경계 인계에 기록. Core 요청이 필요하면 구현하지 않고 요청으로 남김 |
| Vm.2 | 현강님/호진님/민형님에게 필요한 변경 파일·동작·타인 시험 수정·Unity 확인/미확인 공유 메모 작성(미전달). 이미 가진 해당 메모에 이번 범위를 명확히 추가 |
| Vm.3 | 현재 선택/행동/진영/밤/폐지 기능의 사용 안내·CLAUDE.md·본 실행 기록 갱신. 새 Tracker 위치만 폴더 안내에 반영 |
| Vm.4 | 미완료 Core 대응과 이월한 구조 개선을 별도 목록으로 정리. 기능 완료가 구조 부채 해결을 뜻하지 않음을 기록 |

외부 전달·커밋은 사용자가 한다. 코드 인수와 문서 인계는 구분한다.

## Phase V4-n — 새 Core 통합 기능 인수

**상태:** 사용자 "V4-n 진행해. 내가 Unity에서 확인할 항목을 순서대로 정리해줘."로 진입했다. §8에 실제 조작 순서/기대/원래 M01~M14 대응과 결과 양식을 작성했다. Vn.1 결과 수집 대기, Vn.2~3 실행 미확인, Vn.4 판정 틀 준비 상태다. **컴파일 미확인·통합 기능 인수 미완료**이며 새 시험/구현 코드/에셋 수정은 없다.

| Task | 내용 |
|---|---|
| Vn.1 | 아래 M01~M14와 기존 EditMode·실제 DB 변경이 있다면 DataBaseAssetTests 결과/로그 수집. 미확인과 실패를 통과로 합치지 않음 |
| Vn.2 | 선택→이동/공격→공용 AP→수동 턴 종료→적 응답→밤 종료→다음 배치의 연속 흐름 확인 |
| Vn.3 | 정상 승패/포기/최종 승리·Run 복귀와 입력 정지/씬 해제의 기존 계약 회귀 확인 |
| Vn.4 | 사용자 허락 대기/구성 문제/회귀 공백/후속 구조 목록을 각각 보고. 구조 점수 평가 없이 기능 인수 결과로 마무리 |

블라인드 A/B 구조 측정은 이번 완료 조건에서 제외했다. 별도 요청 시 현재 코드의 기능 대응 범위와 평가 방법을 밝히고 평가한다.

## 2. 수동 인수 시나리오

| ID | 시나리오 | 기대 |
|---|---|---|
| M01 | 같은 선택 재누름→Drag, 다른 유닛/적/빈 칸 | 합의한 선택 정책·즉시 강조·행동 대상 일치 |
| M02 | 경로 지정/공격 모드/UI 가림/재생 중 누름 | 선택 부수효과 없음, 기존 입력 수명 유지 |
| M03 | 덱 토글·정보창 핀/슬라이드·가려진 정보창, A→B 전환 중 B Ready 수신 | 기존 규약 유지. 다른 개체에 이유를 붙이지 않고 전환/재표시 뒤에도 현재 행동 문구 유지 |
| M04 | 같은 턴 다른 유닛이 AP 사용 | 이전 가용성 캐시 폐기 |
| M05 | 빠른 선택/턴 전환/늦은 응답/슬롯 경쟁, 가용성 Pending 선점→Drag 취소 | 키가 같아도 미응답 요청은 빈 슬롯에서 재조회해 Ready 회복. 폐기 번호 무시, Ready 중복 조회·잘못된 응답 적용·자동 재실행·무한 요청 없음 |
| M06 | Available/Moved/Attacked/Deferred·AP 부족 | Core 사실과 행동/안내 일치 |
| M07 | 무선택·AP=0 | 자동 종료 없음, 정상 수동 EndPlayerTurn |
| M08 | 첫 Drag·공격 확인 중·모드 끄기·비용 예상 취소 | 조회 대기가 이동 확정을 임의 승인/봉쇄하지 않음 |
| M09 | 플레이어8 종료→적응답8→낮 | 밤 완료를 앞당겨 표시하지 않음 |
| M10 | 특성 전체/부분 불가·알 수 없는 사유·정적 키 설명 | 실제 가동 상태와 설명 출처 구분 |
| M11 | 이동 실습 무선택/비용부족/행동사용→다음턴 이동, 미이동 8번째 적 응답→Deployment | Q-V38 합의 경로로 실습 회복 후 실제 이동. 미결정/미확인은 완료로 표시하지 않고 이동 전 통과 없음 |
| M12 | 복귀 키/버튼 미연결·배치 회수/취소/Submit | 폐지 요청 없음, 나머지 입력 정상 |
| M13 | 죽음/필드 이탈/단계 전환/Stop/씬 해제 | 옛 선택·가용성 응답·입력 적용 없음 |
| M14 | 일반 승패·포기·최종승리·연속 밤 | 기존 결과 확인/Run 복귀 계약 유지 |

## 3. 이번 라운드에서 이월한 Phase

| ID | 이월 내용 | 이번 기능의 선행조건 여부 |
|---|---|---|
| V4-b | 선택 원본/명시 계약 분리 | 아님; 기존 원본에서 기능 수정 |
| V4-c | FactsReader·SelectedActor 명칭 전수 변경 | 아님; 기존 ReadFacts 사용 |
| V4-i | 운영 입력 필수화·시험 조립 정리 | 아님; 실제 누락만 해당 접점 진단 |
| V4-j | 표면 목표점·가시밭 높이 통일 | 새 Core 대응과 독립 |
| V4-k | 결과 Consumer 분리 | 아님; 기존 후처리 접점 유지 |
| V4-l | 레이어 이름·배경음·정리 목록 공통화 | 기능 인수와 독립 |
| 옛 V4-h 일부 | 복귀/타임라인 API·전용 빈 범위 사실 전체 삭제 | 사용자 접점 수정 뒤 별도 조사 |
| 옛 V4-n 측정 | 블라인드 구조 A/B 평가 | 이번에는 기능 인수로 종료 |

이월 Phase를 이번 구현 지시에 포함된 것으로 해석하지 않는다. 2026-10-26 후속으로 선택 원본/읽기/조립 세 항목만 [5라운드 설계](../specs/2026-10-26-combat-view-stage-round5-design.md)·[Task](2026-10-26-combat-view-stage-round5-plan.md) 초안으로 작성했으며 구현은 미착수다. 나머지 이월의 라운드/설계는 미정이다.

## 4. 변경 경계

- 우리 V/F의 전투 입력/표시 접점과 S의 Combat 소비만 다룬다. Factory에 새 Tracker를 조립하는 변경은 허용 제안이지만 생성 계약 전체 재설계는 제외다.
- 현강님 Global UI는 Q-V30 허락된 두 파일, 튜토리얼은 Q-V32·38에서 결정한 전투 접점만. Core/Framework 계산/등록, FieldCamera, 다른 씬은 제외다.
- 기존 생성자/입구에서 null이 가능한 사유를 전부 없애지 않는다. 현재 대응에 필수인 의존성 누락과 정상적인 대기/해제 상태만 구별한다.

## 5. 타인 시험 대응 범위(Q-T8, 2026-10-26 승인)

| 항목 | 실제 대상 | 허락할 변경 |
|---|---|---|
| a | 현강님 CombatUnitInfoTests의 필드 선택 사례(천명님 변경 줄 포함) | Q-V29가 바꾼 동일 유닛 누름 기대만. 다른 표시/덱 토글 단언 유지 |
| b | UnitCardTraitContentTests·CombatUnitInfoTests | 행동/특성 상태의 합의한 표시 기대·필요 입력. NightChecker 전용 시험은 발견하지 못해 수동 확인 |
| c | TutorialInputTests/관련 Policy 시험에서 실제 닿는 타인 줄 | MoveOnly 수동 턴 종료·Q-V38에서 합의한 밤 경계 회복 정책 기대만. 실습 통과 목표는 유지 |
| d | StageInputTests/UnityStageInputTests·관련 버튼 시험의 실제 소비 부분 | 체크포인트 복귀 소비 제거/숨김 버튼 필수 참조 해제에 닿는 호출·기대만. 공용 저장 호환 기대 유지 |
| e | 호진님 HojinCombatAttackDragTests의 Rig.Enter 및 HojinCombatAttackDragIntegrationTests의 공격 진입 준비(천명님 변경 고정물 줄 포함) | 새 공격 진입에 필요한 현재 키 Ready의 고정물/조회·응답 대기만. 기존 클릭/Drag·소유권·명령 단언을 약화하거나 가용성 Gate를 우회하지 않음 |
| f | 현강님 CombatActionNoticeTests의 비용 부족 기대 2개 | 사용자 10-26 "기존 기대 문자열 2개 수정 허용". "ActionCost가 부족합니다!" → "공용 AP가 부족합니다!"만, 사례/단언 추가 없음 |
| g | 현강님 CombatInteractionTests 2줄·CombatUnitRangeHoverTests 1줄·MovementDraftTests 1줄 | 사용자 10-26 "추가 기대 문자열 4개 수정 허용". f와 같은 비용 안내 문자열만 맞추며 사례·단언·검증 대상 유지 |

호진님 두 시험은 선택 계약 변경과 별개로 가용성 Ready의 선행조건에 실제 영향을 받는다. 현재 즉시 공격 진입을 호출하므로 준비/대기에 한정한 e 범위가 승인됐다. 그 밖의 영향이 있으면 범위와 작성자를 확인해 추가 제안한다. 새 [Test]/사례 금지는 기존 지시대로 유지한다.

## 6. 문서 변경/실행 기록

### 2026-10-25 — 최초 설계·Task 작성

새 Core 대응과 구조 개선을 함께 제안했다. 코드 구현은 하지 않았다.

### 2026-10-25 — 사용자 우선순위 반영

- Core 변경 대응을 4라운드의 완료 목표로 확정했다. 실제 기능 대응과 직접 필요한 작은 요청/표시 객체만 남겼다.
- 선택 원본 분리/FactsReader/필수 조립/높이/Consumer/레이어·공통화/구조 측정을 이월했다. 기존 Phase ID는 보존하고 실행 순서에서 제외했다.
- 기존 선택·ReadFacts·결과 후처리 안에서 대응하도록 선행조건을 수정했다. 복귀도 사용자 접점만 다루고 광범위한 API/시험 정리를 이월했다.
- 상세 UX·외부 영역·타인 시험 결정은 대기다. 문서만 수정했고 코드·시험·씬·DB 구현, Unity 실행, 커밋/push는 하지 않았다.

### 2026-10-25 — 새 에이전트 독립 검토와 코드 근거 확인

읽기 전용 새 에이전트 1개가 현재 코드와 설계/Task를 대조했다. 실행 재현이 아닌 정적 검토이며, 부모가 지적 근거를 다시 확인했다.

| 검토 항목 | 판정/문서 처리 | 남은 조건 |
|---|---|---|
| 높음: MoveOnly에서 밤 종료 후 Deployment 진입 시 EnterCombat까지 차단 | Core 밤 종료·Policy·실습 통과 조건을 확인. Q-V38 및 V4-g 선행조건/M11 추가 | 회복 정책 결정·구현·Unity 확인 전까지 미해결 |
| 중간: 정보창 슬라이드 뒤 ApplyCharacter가 행동 문구를 비용 숫자로 덮음 | 설계 §6·Vf.1·M03에 개체/키 일치 및 비용+상태 재구성 추가 | 구현·Unity 확인 전까지 미검증 |
| 명세 모호: Pending 선점 뒤 같은 키 재요청 금지로 읽힐 수 있음 | 설계 §4에 Pending 재시도와 Ready 캐시를 구별. M05에 Drag 취소 후 회복 추가 | 구현·Unity 확인 전까지 미검증 |
| 부모 확인: 기존 호진님 공격 Drag 시험이 Ready 준비 없이 즉시 진입 | blame/호출부 확인. Q-T8 e와 Ve.5에 준비/대기 허락 제안 | 타인 시험 수정 허락 대기 |

Core 대응 우선 범위와 구조 개선 이월은 유지한다. 문서만 보완했으며 런타임 코드·시험·씬·에셋 변경, 컴파일/테스트 실행, 커밋/push는 하지 않았다.

### 2026-10-26 — 구현 승인·V4-0 사전 확인

- 사용자 "수정한대로 구현 차례대로 진행해"를 수정안 A(Q-V29~33·38)와 Q-T8 a~e의 한정된 기존 시험 대응에 대한 구현 승인으로 기록했다. 범위 밖 구조 개선·새 시험·Core 수정은 포함하지 않는다.
- 현재 브랜치 `feature/Yonggeun/MergeX-Refector-Stage`, 기준 HEAD `f616d5d95ca8755f41fa5493d09089ce344340fe`. 기존 수정은 FMOD 캐시와 이 라운드 문서이며 캐시는 보존했다.
- 설계 이후 `79c4fb16`(Core 병종 이동 설정)과 `f616d5d9`(맵 편집기)가 병합됐다. `130b3308..HEAD` 비교에서 Combat View/입력 Factory/StageInput 및 공개 ActionAvailability/Preview 계약은 동일했다. Core 내부의 이동 한도·비용 원본 변경과 개발 fixture CategorySpec 인수 추가는 기존 Core Preview 소비 방향과 맞으므로 새 View 판정식을 추가하지 않는다. 맵 편집기 변경은 이번 범위 밖이다.
- 선택 재누름 토글(`CombatUnitInfo.SelectCell`), 결과 Refresh에서만 선택 강조 갱신, RouteInput의 기존 선택 보호 조건, MoveOnly 회복 정책의 코드상 상태를 재확인했다. Unity에서의 재현/미재현은 확인하지 않았다. 선택 구현 줄은 현강님, 기존 선택 시험은 현강님 본문·천명님 메서드명 변경임을 blame으로 확인했다.
- 실행 중 Unity가 있으며 별도 에디터는 시작하지 않았다. 현재 프로젝트의 `C:/Users/User/AppData/Local/Unity/Editor/Editor.log`(최종 수정 2026-10-26 00:05 KST)에 reload 흔적은 있으나, 현재 HEAD의 사용자 컴파일/시험/Combat 진입 성공을 증명하지는 않는다. `Logs/UtilityAiValidation/results.xml`은 2026-10-02의 별도 Core 시험 결과라 현재 baseline에서 제외했다. **컴파일·기존 EditMode/DataBaseAssetTests·Combat 진입·문제 재현은 사용자 확인 대기**다.
- V4-0의 저장소/계약/승인 범위 확인을 마쳤으며 V0.2·실행 재현은 미확인으로 남겼다. 계획 §0.1의 Phase별 사용자 Unity 확인 절차에 따라 V4-a 코드 변경 전 확인 결과를 요청했다. 런타임 코드/시험/씬/DB는 아직 수정하지 않았고 MSBuild/Unity 실행·커밋/push도 하지 않았다.

### 2026-10-26 — 사용자 baseline 확인·V4-a 코드 완료(W13 끝)

- 사용자 응답 **"컴파일·기존 시험·Combat 진입 모두 정상"**을 `f616d5d9`의 V4-a 변경 전 baseline으로 인수했다. 선택/강조 문제 자체의 실행 재현이나 DataBaseAssetTests의 개별 결과까지 확인됐다고 확대하지 않는다. V4-0 확인 지점을 통과하고 승인된 V4-a에 진입했다.
- Va.1: `CombatUnitInfo._selected`를 유지하고 필드 선택에서 동일 대상 재누름은 유지, 빈 칸은 해제로 바꿨다. 덱/손패 핀·토글 코드는 수정하지 않았다.
- Va.2: `CombatInputPolicy.CanSelectUnit`에 입력 가용성/표시 완료/현재 Combat Layer·PlayerInput/대기·종료·모달·후처리·선행 명령 검사를 모았다. 공격/현재 경로 지정은 기존 Interaction 상태로 막고, UI 가림은 기존 Display/PointerMapper 검사를 유지했다. `RouteInput`은 연결을 잡아 외부 선택 콜백/Router/호버 후 같은 연결인지 확인한다. Facts는 새 상태를 저장하지 않고 선택 판단에서 선행 명령을 읽는다는 주석만 맞췄다.
- Va.3: `SelectedActorCells`는 정보창의 기존 완료 표시 목록에서 계산하는 속성이다. 새 선택/캐릭터 캐시를 만들지 않았다. 결과 Refresh와 InspectUnit 직후가 같은 `RenderSelectedActor`를 써서 선택 강조를 맞추며, 정보창 콜백에서 Release됐으면 강조를 다시 그리지 않는다. 전체 Refresh/결과 확인을 클릭에서 호출하지 않는다.
- Va.4: 운영 `Attach(root)`가 구체 `CombatSceneDisplay`를 표시와 `ICombatUnitInspection`으로 함께 넘기고, 표시 생성자는 필수 `CombatUnitInfo` 누락 시 예외를 던진다. Factory의 path 공급자도 같은 display.SelectedActorId를 읽는다. 운영 누락 경로가 이미 진단되므로 Factory 서명/시험용 선택 생략 조립은 변경하지 않았다.
- Va.5: Q-T8 a 범위에서 현강님 본문·천명님 메서드명 변경 줄이 있는 기존 `CombatUnitInfoTests` 한 사례의 이름과 재클릭 직후 HP 기대만 맞췄다. 사례/단언 수를 늘리지 않았다. 기존 FakeDisplay와 공격 Drag 시험 변경은 필요하지 않았다.
- 기존 UI 등록/개폐·서비스 조립/등록/Group Export·입력 Submit 경로를 유지했다. 선택/완료 표시 값은 기존 원본을 읽고, 명령/가용성 규칙을 View에 복제하지 않는다. 기능 변경은 선택/강조/선택 자격 및 기존 시험 한 곳에 한정했다. 선택의 UI 종속은 이월 부채다.
- W13 검토: 읽기 전용 새 에이전트 1개가 해당 6파일의 diff와 실제 입력/표시/운영 조립을 대조해 **높음·중간 결함 없음**으로 보고했다. SSOT·SOLID 5원칙·FW 계약·조립/기능 독립성의 이번 변경상 위반도 발견하지 못했다. 정적 검토 결과이며 실행 증거가 아니다.
- 정적 확인: `git diff --check` 통과. 수정한 소스/시험 6파일을 UTF-8·LF로 확인했고 새 파일/메타 변경은 없다. 기존 선택 시험의 시험 특성 선언 수 14개·Assert 호출 60개는 변경 전후 동일하다. Core/Framework/ServiceKeys/씬/DB 변경은 없고 기존 FMOD 캐시는 보존했다.
- **변경 후 컴파일 미확인**, Unity 시험/수동 확인 미실시. 사용자에게 M01~M03(재누름→Drag·다른 아군/적/빈 칸·즉시 강조·Q 경로/공격/UI 가림·덱 핀/슬라이드) 및 선택 중 정지/씬 해제의 확인을 요청하고 여기서 멈춘다. 정상 확인 뒤 다음은 V4-d다. MSBuild/새 시험/씬·DB·Core 변경·커밋/push 없음.

### 2026-10-26 — 사용자 다음 단계 지시·V4-d 코드 완료(W14 앞)

- 사용자에게 b(선택 원본/계약 분리)·c(FactsReader/명칭 정리)는 후속 이월이며 Phase 번호를 보존해 a→d임을 설명했다. 이후 사용자 "아하 진행해" 및 **"V4-d 진행해"**의 명시 지시로 이 단계에 진입했다. V4-a 변경 후 Unity 성공 결과를 새로 받았다고 기록하지 않는다.
- Vd.1: `Input/Interaction/CombatActionAvailabilityTracker.cs`와 `.meta`를 추가했다. Unknown/Pending/Ready 상태와 `(전투 번호, 진영 턴 순번, 최신 결과 순번, 선택 아군)` 키를 보관한다. 요청 번호·현재 슬롯의 종류/행위자·응답 전투/턴/행위자가 맞아야 수락한다. 결과 순번은 요청 키와 현재 Facts에서 대조한다. 실패 응답도 Ready로 보관하고 동일 키의 Ready는 슬롯 상실 후에도 유지한다.
- Pending이 선점되면 자기 번호만 내려놓고 Unknown으로 돌아간다. 같은 키라도 다시 빈 슬롯을 얻으면 재조회하며 폐기한 번호의 늦은 답을 쓰지 않는다. 선택/전투/턴/새 결과·죽음/필드 이탈·입력 닫힘/정지에서 무효화한다. 비용·병종·ActionUse 판정식은 작성하지 않았다.
- Vd.2: 입력 Factory가 Tracker 하나를 생성해 `CombatSceneInput`이 소유한다. `Stop`은 Tracker를 먼저 정지하며 이후 재조회하지 않는다. 기존 격리 조립의 생성 계약을 광범위하게 필수화하지 않았다. 같은 Tracker를 경로 입력이 빌려 배경 범위 요청 순서만 맞추고, 경로는 Tracker를 정지/소유하지 않는다. 새 Framework 서비스/ServiceKey/Group 등록은 없다.
- Vd.3: 기존 Policy에 ActionAvailability의 Combat 단계와 MoveOnly의 읽기 전용 조회 허용을 넣었다. `CanQueryActionAvailability`는 입력 가용성·안정된 표시/국면·대기/미처리 명령·모달/닫힘을 확인한다. 수동 명령을 게시하거나 튜토리얼 막힘 안내를 호출하지 않는다.
- Vd.4~5: 기존 Tick의 Interaction 앞에서 Tracker 자기 응답을 소비하고 Facts를 다시 읽는다. 입력 직전/직후와 결과 확인 뒤에도 같은 수명을 맞춘다. 전경은 조회가 슬롯을 덮지 않으며 첫 Drag의 기존 MoveRange는 Pending 가용성을 즉시 선점할 수 있다. 안정 시 선택 가용성→배경 이동 범위→호버 순서를 지킨다. Cancel/Q/AfterAcknowledged가 직접 배경 이동 범위를 요청하는 접점에서도 같은 Tracker를 먼저 맞춘다. 전역 요청 큐/일반 우선순위 시스템/FactsReader는 추가하지 않았다.
- Vd.6: 새 결과 관측의 Tick과 BeforeResult에서 가용성을 무효화한다. 기존 RemoveCommandsThrough→AcknowledgePresented→CompleteFinalization 순서는 유지하고 그 뒤 기존 AfterAcknowledged 앞에서 새 조회를 준비한다. 입력 닫힘/Stop의 자기 슬롯만 정리하며, Abort 뒤 Unbind가 보내는 SelectCanceled/FocusLost는 기존 소유자에게 전달한다.
- 자체 코드 대조: Ready 캐시의 반복 요청 억제, Pending 선점 후 같은 키 재조회, 응답 종류/번호/키 불일치 무시, 다른 유닛의 AP 사용 결과에서 무효화, 첫 Drag의 범위 요청 선점, 입력 닫힘·Stop 뒤 재조회 금지를 점검했다. 실행 재현이나 시험 통과 증거는 아니다. W14 독립 검토는 V4-e 끝에 남아 있다.
- 정적 확인: 수정 소스 UTF-8/LF·신규 `.meta` 짝·`git diff --check` 확인. 이번 Phase의 시험 코드 변경/새 사례는 없다(기존 V4-a 선택 시험 수정만 유지). Core/Buffer/Framework/ServiceKeys/씬/DB와 기존 에디터 자산 변경은 건드리지 않았다.
- **컴파일 미확인**, 기존 EditMode/Unity 재생 미실시. 선택→대기→Drag/Q 취소/공격, 빠른 선택 전환, 턴 종료/연속 결과에서 입력이 멈추지 않는지와 기존 시험을 사용자에게 확인 요청한다. 현재는 조회/캐시 수명까지이며 실제 버튼·행동 진입은 다음 V4-e다. 여기서 Phase 보고 후 멈춘다. 커밋/push하지 않았다.

### 2026-10-26 — 사용자 V4-e 진행 지시·코드 완료·W14 검토 완료

- 사용자 **"V4-e 진행해"**로 행동 입구/안내 구현에 진입했다. V4-a/d 변경 후 Unity 성공을 새로 보고받은 것으로 간주하지 않는다. 변경 후 **컴파일 미확인**, Unity/EditMode 미실시이며 MSBuild·새 시험·커밋/push는 하지 않는다.
- Ve.1: 공격의 기존 `CanEnter`가 운영 씬에서 현재 키의 `Ready.CanAttack`을 요구한다. 버튼과 Router가 같은 진입 판단을 사용하고, Unknown/Pending에서는 새 공격에 들어가지 않는다. 입력 의도를 저장하지 않으므로 늦은 응답으로 자동 진입하지 않는다. 이미 켜진 모드를 끄는 버튼/취소 경로는 그대로다.
- 운영 Factory는 같은 Tracker를 공격/경로에 전달하며 `CreateSceneInteraction`에서는 누락을 인자 오류로 진단한다. 기존 범위/조준 격리 시험용 `CreateInteraction`/공격 생성자의 생략 조립만 유지했다. 운영 조립이 이 생략 경로를 타지 않으며 광범위한 생성자 정리는 하지 않았다. 생성·소유는 Factory→SceneInput이고 공격/경로는 빌려 읽기만 한다.
- Ve.2: 실제 경로 시작에서 유효 Ready의 `CanMove=false`를 확인하면 Core `MoveFailure`만 안내하고 경로/명령을 시작하지 않는다. Unknown은 기존 Core 범위/상세 승인 경로를 유지한다. 이동 기대 문맥·공격의 캡처된 전투/턴 번호는 변경하지 않았다.
- Ve.3: `CombatActionNotice` 한 곳에서 ActionAlreadyUsed/ActionDeferred/WrongController/StaleTurn/StaleContext를 사용자 문구로 바꾼다. 기존 비용 문구는 공용 AP 의미로 맞췄다. 실제 공격 진입/이동 시작 시도 또는 최종 거절 결과에서만 호출하며 버튼 Read/CanEnter·Tracker Sync·Poll·호버에서는 새 안내를 하지 않는다. 사전 거절은 명령을 만들지 않아 같은 시도의 최종 거절과 중복되지 않는다.
- Ve.4: 무선택 수동 EndPlayerTurn·0 AP 자동 종료 없음은 기존 경로를 유지했다. AP/비용/병종/ActionUse 규칙을 View에 다시 계산하지 않는다. 이동 실습의 정책 변경은 V4-g에 남긴다.
- Ve.5: Q-T8 e에 따라 호진님 격리 고정물은 실제 Core 가용성을 조회/수락한 뒤 기존 공격 진입을 수행하고, 통합 시험은 현재 Ready를 기다린 뒤 버튼을 누른다. 기존 클릭/Drag·소유권·명령 단언은 유지한다. Q-T8 f의 Notice 기대 2개는 별도 사용자 허락으로 문자열만 맞췄다. 추가로 발견한 현강님 Interaction 2줄·RangeHover 1줄·MovementDraft 1줄도 **"추가 기대 문자열 4개 수정 허용"**을 받은 뒤 Q-T8 g로 기록하고 같은 문자열만 맞췄다.
- Ve.6: 새 읽기 전용 에이전트 셋으로 요청 수명/슬롯, 입력/안내/고정물, SSOT·SOLID 5원칙·FW·조립/기능 독립성을 나눠 검토했다. 요청 수명 및 조립/설계 계약은 추가 결함 없음. 입력 검토는 `HojinCombatAttackDragTests.Rig.Enter`의 중간(P2) 1건을 찾았다. 기존 CancelPresentation 사례의 두 번째 Enter는 같은 키 Ready를 유지해 요청이 없는데 무조건 Calculate를 호출해 실패한다. Sync 뒤 Pending일 때만 실제 조회/수락하고 Ready는 재사용하도록 수정했다. 해당 검토자의 정적 재검토에서 해소 및 기존 최초/새 키 준비·소유권/명령 단언 보존을 확인했다. 최종 남은 높음/중간 결함은 없다. Unity 실행 증거로 대체하지 않는다.
- 정적 확인: `git diff --check`, 작업 소스/문서 UTF-8·LF 및 Tracker `.meta` 짝을 확인했다. E에서 바꾼 기존 시험 6파일의 시험 특성 선언 수 207개·Assert 호출 555개는 변경 전후 동일하다. 새 시험/사례/단언은 추가하지 않았다. Core/Buffer/Framework/ServiceKeys/GlobalSystem/씬/DB는 변경하지 않았다. 기존 FMOD 및 별도 편집기 자산 변경은 보존했다.
- 사용자 확인 항목: 현재 응답 대기 중 공격 버튼/키와 늦은 자동 진입 없음, 모드 끄기, Ready의 이동/공격 거절 안내 및 중복 없음, 같은 턴 여러 유닛의 공용 AP 사용 뒤 재조회, 빠른 선택/결과/턴 전환의 옛 승인 무효화, 무선택/0 AP 수동 턴 종료, 기존 공격 클릭→Drag·Q/취소·씬 정지. 기존 EditMode와 Boot 통합 시험도 Unity에서 확인해야 한다. **컴파일 미확인**을 유지하며 여기서 멈춘다. 다음 V4-f는 기존 UI 상태/특성 설명이고 이번에 구현하지 않았다. 커밋/push 없음.

### 2026-10-26 — 사용자 V4-f 진행 지시·코드 완료(W15 앞)

- 사용자 **"V4-f 진행해"**의 명시 지시로 승인된 Q-V30·31·Q-T8 b 범위에 진입했다. V4-e 뒤 Unity 성공을 새로 보고받은 것은 아니다. 이번 변경 후 **컴파일 미확인**, 기존 EditMode/수동 재생 미실시다. MSBuild·새 시험·커밋/push는 하지 않았다.
- Vf.1 연결: Factory가 기존 `display.ShowActionAvailability` 메서드 창구만 SceneInput에 주입한다. SceneInput은 Tracker.Read가 현재 전투/턴/결과/선택 키를 검증한 응답 또는 null을, 표시 키/응답이 바뀔 때만 전달한다. 가용성 원본은 Tracker이며 새 규칙/가용성 원본은 만들지 않았다. 새 결과·CancelPresentation·Stop에서 오래된 이유도 함께 비우며 이미 null을 보냈으면 반복하지 않는다. 새 표시 콜백 뒤에는 기존 연결인지 확인하고 결과 소비를 이어 간다.
- Vf.1 표시: `CombatActionStateText.cs`(+meta)는 완료 CharacterData의 ActionUse를 미사용/이동함/공격함/보류로, 현재 Core MoveFailure/AttackFailure를 짧은 이유로 바꾼다. 보통 비용은 완료 Stats 값을 유지한다. UnitInfo는 선택/표시 개체·현재 진영 턴을 확인하고 표시용 응답만 보관한다. A→B 슬라이드 중 B Ready를 A에 붙이지 않으며 `ApplyCharacter`·전환 완료·재표시·같은 값 Refresh에서 비용과 상태를 함께 복원한다. 선택/턴/국면 변경·Clear에서 오래된 이유를 제거한다. 입력 연결의 현재 키 검증을 표시 쪽에 다시 규칙으로 구현하지 않았다.
- 레이아웃: 운영 비용 숫자의 TMP 영역이 30×30임을 씬 원문에서 확인했다. 새 씬 참조 없이 같은 TMP만 50×50·숫자 18px·상태/이유 12px·줄바꿈 없음으로 맞췄다. 기존 아이콘/배경/강조 Root는 유지하며 Scene/Prefab 에셋은 편집하지 않았다. 실제 해상도·글꼴에서 읽기/겹침 여부는 Unity 수동 확인이 남는다.
- Vf.2: NightChecker는 기존 live Buffer 읽기 시점과 UIBase 등록/개폐를 유지한다. 완료된 아군 턴 수와 배치/아군 준비/아군 턴/적 준비/적 응답을 기존 progressText에 표시한다. 비교 캐시에 StagePhase·ControllerPhase를 포함해 같은 8/8이라도 진영 변화가 반영된다. `적 응답 · 8/8`을 밤 완료라고 쓰지 않는다.
- Vf.3: UnitCardTraitContent의 Snapshot 경로만 IsAvailable/UnavailableReason/PartialUnavailableReason을 읽어 이름과 설명에 전체/부분 불가를 덧붙인다. 공개된 사유 코드를 표시 문구로 번역하며 TraitKey 지원 목록이나 DeferredCombatFeatures 호출은 복제하지 않았다. static TraitKey 경로는 현재 활성 여부를 단정하지 않고 기존 설명을 유지한다. 기존 가시의 개체별 값·아이콘/순서·정적 설명도 유지한다.
- Vf.4: Q-T8 b 범위의 기존 CombatUnitInfoTests 비용 기대 2개만 새 문구로 맞췄다. 인터페이스 구현 보정을 위해 **원래 Droute가 작성한** CombatViewSystemTests 공통 FakeDisplay에 빈 표시 메서드 하나를 더했다(최초 작성 466f8c62, 기존 표시 창구 줄 c417e9ee). FakeDisplay 보정 때문에 기존 사례·단언을 추가하거나 약화하지 않았다. 두 파일의 시험 특성 선언 수 14/71·Assert 호출 60/295는 변경 전후 동일하다. UnitCardTraitContentTests의 기존 단독 Snapshot/정적 키 기대는 영향이 없어 수정하지 않았다. 새 사용 불가 상태의 자동 시험을 추가한 것은 아니므로 수동 확인이 필요하다.
- 자체 대조: SSOT는 Core/완료 Buffer·현재 선택·Tracker에 유지했다. S는 조립/조회 수명/문구 변환/실제 표시를 구분하고, O는 기존 표시 창구와 작은 변환만 확장, L은 해제 뒤 표시 무시/정지 뒤 재조회 금지/완료 표시 계약을 보존했다. I/D는 SceneInput에 필요한 표시 메서드만 주입하고 기존 Buffer/Group 경로를 사용했다. 새 서비스/Export/UI 등록 우회는 없다. 상태 문구 변경은 변환/표시, 밤 문구 변경은 NightChecker, 특성 문구 변경은 기존 TraitContent 안에 머물며 조립 연결과 기능 구현을 분리했다. W15 독립 검토는 다음 g 끝에 남아 있다.
- 공유 준비: Factory 연결 줄은 [민형님 공유 §26](../handoffs/2026-10-15-combat-scene-display-factory-framework-share.md#26-2026-10-26-후속--4라운드-가용성-생성과-기존-표시-연결-미전달), Global UI 전투 접점·기존 시험 기대는 [현강님 공유 §11](../handoffs/2026-10-22-combat-view-structure-hyeongang-share.md#11-전투-view-4라운드-v4-f-기존-상태특성-표시-2026-10-26-미전달)에 기록했다. 외부 메시지는 보내지 않았다. 전체 라운드의 문서 인계 정리는 V4-m에 남긴다.
- 정적 확인: V4-f의 명시한 작업 파일 17개를 UTF-8/LF로 확인하고 `git diff --check`를 통과했다. 새 변환의 `.meta` 짝/32자리 GUID 및 중복 없음, 인터페이스 구현 둘(운영/공통 FakeDisplay)과 생성 호출자 하나의 연결을 대조했다. 로컬 TMP 패키지에서 textWrappingMode/NoWrap API가 존재함을 확인했다. 컴파일·시험 실행을 대신하는 검증은 아니다.
- 사용자 확인: Available/Moved/Attacked/Deferred와 AP 부족, A→B 빠른 전환/가려진 정보창/재표시·값만 Refresh·새 결과/턴/Stop, 밤 8번째 적 응답→다음 배치, 전체/부분 불가 특성 설명·정적 덱 설명 및 긴 문구 겹침을 Unity에서 확인한다. 기존 UnitInfo/TraitContent/CombatView 시험도 실행해야 한다. Core/Buffer/Framework/ServiceKeys·다른 Global UI·씬/DB·필드 카메라/Architecture는 변경하지 않았고 기존 FMOD 캐시는 보존했다. 여기서 Phase 보고 후 멈춘다. 다음은 V4-g다.

### 2026-10-26 — V4-f 컴파일 오류 후속 수정

- 사용자 보고: `CombatSceneInput.cs(40,33)` CS0246, EntityId를 찾을 수 없음. V4-f에서 표시 창구/표시 키에 EntityId를 추가하면서 `Contracts.Ids` using을 누락했다. 해당 using 한 줄을 추가했다. 다른 코드/시험/에셋 변경은 없다.
- 정적 대조와 `git diff --check`만 확인했다. 수정 후 **컴파일 미확인**, Unity 재컴파일 결과를 기다린다. MSBuild/시험/커밋/push는 하지 않았다.

### 2026-10-26 — 사용자 V4-g 진행 지시·코드 완료·W15 검토 완료

- 사용자 **"V4-g 진행해"**로 승인된 Q-V32·38 A/Q-T8 c 범위에 진입했다. 앞선 V4-f의 using 수정 뒤 Unity 컴파일 성공을 새로 보고받은 것은 아니다. 이번 변경 후 **컴파일 미확인**, Unity/EditMode 미실시. MSBuild·새 시험·커밋/push는 하지 않았다.
- Vg.1: MoveOnly의 수동 EndPlayerTurn에서 성공한 빈 이동 범위 응답 선행조건을 제거했다. 선택/가용성/AP와 무관하게 기존 Gate·PlayerInput·실제 Layer·결과 표시 완료·종료/닫힘/확인창 검사를 통과해야 한다. 기존 버튼과 키는 같은 Policy를 사용하며 자동 턴 종료는 추가하지 않았다.
- Q-V38 A의 전투 재진입은 MoveOnly + Deployment + 같은 전투의 Running/Practice/PracticeGoal 대기일 때만 허용한다. MoveOnly는 실습 통과 후 다음 설명의 시작 대기에도 유지되므로 제한 값만으로 재진입을 허용하지 않았다. 기존 CombatBlockedInputReport가 공개 Tutorial 상태에서 대기 사실을 읽어 Facts의 bool 하나로 넘기고 Policy가 허용을 결정한다. 새 FactsReader/조회 서비스·Buffer 필드는 만들지 않았다. 공격/일반 배치 제한은 유지하고 Core 거절을 우회하지 않는다.
- Vg.2: TutorialFactsReader는 잘못된 "Core 활성 행위자" 주석만 선택한 아군/플레이어 명령 의미로 정정했다. 수락된 Command Move, PathMovement의 실제 진입 칸 1개 이상, 결과 순번의 표시 완료가 이동 통과 사실이다. EnterCombat/EndPlayerTurn/자동 적 이동은 실습 통과로 만들지 않는다. 목표/Jobs/Store는 수정하지 않았다.
- Vg.3: Q-T8 c 범위에서 CombatInputPolicyTests의 기존 무선택 EndPlayerTurn 기대 1개와 이름을 현재 정책에 맞췄다(해당 기대 줄 천명님, 최초 사례 Droute). 새 사례/단언은 추가하지 않았다. 기존 TutorialInputTests에는 이번 명령 정책의 직접 기대가 없어 수정하지 않았다. 기존 빈 범위 추적/시험용 API는 삭제하지 않았으며 후속 구조 정리 부채로 남긴다. 밤 경계/미완료와 제한 유지 구간의 구별은 기존 직접 사례가 없어 수동 확인이 필수다.
- Vg.4: 새 읽기 전용 에이전트 1개가 V4-f/g의 표시 수명·밤/특성 소비·회복 정책·실습 통과 조건을 검토해 **수정이 필요한 구체적 결함 없음**으로 보고했다. SSOT·SRP/OCP/LSP/ISP/DIP·JMH/FW·Factory 조립/기능 구현의 독립성도 각각 점검해 새 위반을 찾지 못했다. f의 Contracts.Ids using 수정은 소스로만 확인했다. W14 검토를 재사용하지 않았고, 이번 결과 역시 컴파일/실행 증거가 아니다.
- 정적 확인: 실제 수정한 코드/문서를 명시해 UTF-8/LF·`git diff --check`를 확인했다. 기존 Policy 시험의 특성 선언 45개·Assert 호출 42개는 변경 전후 동일하다. 새 `.cs`/`.meta`는 만들지 않았으며 기존 GUID도 변경하지 않았다. Core/Buffer/Framework/ServiceKeys·GlobalSystem·Jobs/Store·씬/Prefab/DB·FieldCamera/Architecture는 이번 Phase에서 수정하지 않았다. 이전 Phase의 Global UI 변경과 기존 FMOD 캐시는 그대로 보존했다.
- [호진님 T-7 공유 메모](../handoffs/2026-10-20-tutorial-t7-combat-input-handoff-Hojin.md#2026-10-26-v4-g--새-core-이동-실습-회복-미전달)에 현재 정책/이전 이력/변경 파일/미확인을 구분해 추가했다. 외부 발송은 하지 않았다. 사용자 Unity 확인: 무선택/AP부족/행동 사용/보류→수동 종료→다음 턴 실제 이동, 미이동 8번째 적 응답→Deployment→정상 재진입→이동, 결과 표시 전 통과 없음, 통과 후 제한 유지 구간의 재진입 차단, 설명 Layer 독점·금지 공격 안내 및 기존 시험. 항상 이동 불가능한 기획 데이터는 별도 잔여로 남긴다. **컴파일·실제 회복은 미확인**이며 여기서 멈춘다. 다음 V4-h는 구현하지 않았다. 커밋/push 없음.

### 2026-10-26 — 사용자 V4-h 진행 지시·폐지 기능 접점 정리

- 사용자 **"V4-h 진행해"**로 Q-V33 A/Q-T8 d의 범위를 구현했다. V4-a~g 변경 후 Unity 성공을 새로 인수한 것은 아니다. **컴파일 미확인**, Unity/EditMode 미실시이며 MSBuild·새 시험·커밋/push는 하지 않았다.
- Vh.1: `CombatActionPanel`은 기존 복귀 참조가 있으면 Bind(해제 포함)와 SetAvailable의 동일 값 조기 반환 전에도 숨김/비활성으로 맞춘다. `returnButton`만 필수 검사와 null Attach/표시에서 제외했고 공격 버튼·이미지·두 sprite·턴 종료 버튼의 필수 검사는 유지했다. `Apply` 전체에 null 회피를 넣지 않았다. 세 콜백 연결/일괄 해제 계약과 세 번째 인자·슬롯은 호환용으로 보존하고 복귀 클릭은 기존 무동작을 유지한다. 씬/프리팹 참조는 바꾸지 않았다.
- Vh.2: `CombatInputController.Configure`에서 Combat.Return의 Press 등록만 제거했다. 공용 InputSystem은 등록 없는 액션을 TryDispatch에서 전달하지 않는다. DeploymentInputController·배치 ReturnUnit·우클릭 Cancel·Pointer/Select·Submit은 변경하지 않았다. 예약 Names/KeyBindingAction.Return·저장값·PlayerInputActions 액션/바인딩도 보존했다. 설정 화면의 복귀 항목 노출과 호환 API/객체 정리는 후속 부채다.
- Vh.3: 실제 수정한 ActionPanel/ActionButtons/표시 계약의 주석과 `viewDocs/stage-input-guide.md`의 현재 사용 기준을 정정했다. R은 예약·미소비, 선택 아군·Core 진영 턴/공용 AP·현재 가용성/범위 응답 기준이며, 복귀 견적/실행한 이동 되감기 안내를 제거했다. 과거 실행 기록과 원래 설계 이력은 보존했다.
- Vh.4 기존 시험: StageInputTests의 R 입력은 그대로 보내되 이벤트 기대에서 Return만 제외했다. CombatDisplayReuseTests의 기존 실제 이동 경로/견적/재생/결과 확인은 유지하고, 폐지 복귀용 재조회·명령 처리 부분을 숨김/비활성·새 명령 없음·이동한 위치 유지 기대로 맞췄다. 기존 HUD 반복 표시 시험의 준비에서 returnButton=null로 Bind/SetAvailable을 거치며 기존 골드 할당 검증은 그대로다. 이 측정이 ActionPanel 반복 표시 성능까지 검증한다는 뜻은 아니다. 새 특성/사례를 만들지 않았으며 StageInputTests의 특성 선언 19개·Assert 호출 30개는 동일, CombatDisplayReuseTests의 특성 선언 22개는 동일하고 Assert 호출은 폐지된 처리 기대 교체로 140→138이다. UnityStageInputTests의 예약 Return 액션 고정물/설정 검증은 수정하지 않았다.
- W16: 새 읽기 전용 에이전트 1개가 폐지 입력 소비·선택적인 버튼 참조·엄격한 운영 UI 참조·저장 호환·기존 시험 기대를 검토해 **수정이 필요한 구체적 결함 없음**으로 보고했다. SSOT·SRP/OCP/LSP/ISP/DIP·JMH/FW/UIInputBinding 계약과 조립/기능 독립성에도 새 위반을 찾지 못했다. 과거 W 검토를 재사용하지 않았으며 컴파일/실행·할당 측정 증거는 아니다.
- 정적 확인: 이번에 수정한 코드/문서 11개를 명시해 UTF-8/LF와 추적 파일의 git diff --check를 확인했다. 미추적 설계/계획의 공백·충돌 표식도 별도 확인했다. 새 .cs/.meta·GUID 변경은 없고 Core/Buffer/Framework/ServiceKeys·GlobalSystem·Factory·Jobs/Store·씬/Prefab/DB·FieldCamera/Architecture는 이번 Phase에서 수정하지 않았다. 이전 Phase의 변경과 기존 FMOD 캐시는 보존했다. 현강님 공유 메모에 Panel/기존 시험 변경과 미확인을 추가했으며 외부 발송은 하지 않았다.
- 사용자 Unity 확인 항목: returnButton 연결/미연결에서 Combat 진입, 연결 버튼의 숨김/클릭 무동작, R 입력의 요청/명령 미생성, 공격/E·턴 종료/Space·포인터/Submit, 배치 회수/우클릭 취소, 기존 EditMode. **실행 결과는 미확인**이며 여기서 멈춘다. 다음 V4-m은 구현하지 않았다.

### 2026-10-26 — 사용자 V4-m 공유·안내 정리 지시 (문서만)

- 사용자 **"V4-m 공유·안내 정리 진행해"**에 따라 Vm.1~4를 정리했다. 구현 코드·기존 시험·씬/Prefab/DB·.meta는 수정하지 않았으며 V4-n 실행도 시작하지 않았다. 커밋/push·외부 메시지 발송은 하지 않았다.
- Vm.1: Core 경계 인계 §38에 실제 ActionAvailability/진영 턴·행동 사용·특성 Snapshot·Move 결과 소비와 수명 전제를 기록했다. 이번 구현 범위에서 새 Core API 요청/수정은 없다. 보류된 효과 지원 재개와 항상 이동 불가능한 데이터의 정책은 별도 판단으로 남겼으며 옛 요청의 상태는 변경하지 않았다.
- Vm.2: 기존 현강님 메모 §12, 호진님 T-7 메모의 V4-m 절, 민형님 조립 메모 §27에 변경 파일/동작·관련 타인 시험 허락 범위·정적 검토/미확인을 모았다. 초반 안내에서 최신 절과 과거 검증의 범위를 구분했다. 모든 문서는 미전달이다.
- Vm.3: stage-input-guide §2.3~2.4에 선택/Ready·슬롯·공용 AP·진영/밤·특성·실습 사용법을 추가하고 CLAUDE/설계/계획 현재 상태를 맞췄다. Script/README에는 새 Tracker의 위치와 안내 링크만 추가했다. 수동 M02와 현재 설계/Task의 옛 Q 경로 표기는 경로 지정으로 정정했고 과거 실행 이력은 보존했다.
- Vm.4: §7에 문서 지도와 실제 인수 상태, 남은 기능 확인·외부/기획 후속·구조 이월을 구분했다. SSOT는 Core/Buffer·선택 원본·Tracker·실행 기록의 현재 소유를 유지한다. SRP/OCP/LSP/ISP/DIP와 JMH/FW·조립/기능 독립성은 W13~W16의 정적 검토 근거를 연결했으며 문서만으로 새 구조 평가나 점수 상승을 주장하지 않는다.
- 정적 확인: 이번 문서 9개의 UTF-8/LF·공백/충돌 표식·새 링크를 확인하고 시작 시점의 비문서 변경/미추적 파일 해시와 대조했다. 이 검사는 컴파일/시험/조작 성공 증거가 아니다. **변경 후 컴파일·기존 EditMode·Unity는 미확인**이며 다음은 V4-n 기능 인수다. 여기서 Phase 보고 후 멈춘다.

### 2026-10-26 — 사용자 V4-n 진입·Unity 확인 순서 작성

- 사용자 **"V4-n 진행해. 내가 Unity에서 확인할 항목을 순서대로 정리해줘."**에 따라 인수에 진입했다. §8에 컴파일/기존 시험→배치/선택/행동/UI→수동 턴/밤→튜토리얼→종료 순서와 M01~M14 대응을 기록했다. 키 기본값 G/E/Space/R과 배치 우클릭 서브 유닛 회수, Run이 이어지는 승리/Run 종료의 목적지를 현재 소스와 대조했다.
- 실제 Unity 실행은 사용자가 하며 결과는 아직 받지 않았다. Vn.1 수집 대기, Vn.2~3 실행 미확인, Vn.4 결과 분류/보고 틀만 준비했다. 전 항목을 미확인으로 두고 인수 완료를 선언하지 않는다. 재현하지 못한 상태/구성과 세부 슬롯 수명은 부분 확인으로 구분한다.
- 계획/설계/CLAUDE 현재 상태와 확인 절차만 갱신했다. 코드/기존 시험/씬/Prefab/DB/.meta·Core/Framework는 수정하지 않았고 MSBuild/Unity/시험 실행·새 시험·커밋/push·외부 발송도 하지 않았다. 문서 형식/범위만 정적으로 확인했다. **컴파일 미확인, 사용자 결과 대기**다.
## 7. 4라운드 현재 인계와 잔여 (V4-m, 2026-10-26)

### 7.1 구현/검토/실행 상태

| 범위 | 현재 산출물 | 확인 범위 |
|---|---|---|
| V4-a | 같은 필드 선택 유지·입력 보호·즉시 강조 | W13 새 에이전트 1개 정적 검토 완료 |
| V4-d/e | 가용성 Tracker·현재 키/슬롯·공격/이동 입구·공용 AP 안내 | W14 새 에이전트 3개 정적 검토 완료. 고정물 P2 한 건 수정/재검토 완료 |
| V4-f/g | 행동/밤/특성 표시·MoveOnly 수동 종료/밤 경계 회복 | W15 새 에이전트 1개 정적 검토 완료 |
| V4-h | 복귀 버튼 선택 참조·숨김/무동작·R 미소비 | W16 새 에이전트 1개 정적 검토 완료 |
| V4-m | 소비 경계/공유/안내/잔여 문서 | 문서 대조·형식/범위 확인. 외부 미전달 |
| V4-n | 통합 기능 인수 | **2026-10-26 사용자 보고로 완료. `11c07868` 이후 컴파일·기존 EditMode·M01~M14 모두 정상. 근거 §8.4** |

V4-0의 사용자 "컴파일·기존 시험·Combat 진입 모두 정상"은 a 변경 전 `f616d5d9` baseline에만 해당한다. 이와 별도로 V5-0에서 `11c07868` 이후 전체 정상 보고를 받아 f의 using 수정 뒤 컴파일과 V4-n 기능 인수를 확인했다. 정적 검토와 사용자 실행 보고는 서로 다른 근거로 기록한다.

### 7.2 문서 지도 (공유는 모두 미전달)

| 대상/목적 | 문서 | 현재 범위 |
|---|---|---|
| Core 작업자 | [Core 경계 인계 §38](../handoffs/2026-09-24-view-core-data-boundary-handoff.md) | 소비 계약/결과 게시 의존·추가 API 요청 없음·보류 효과와 기획 후속 |
| 현강님 | [기존 UI/입력 공유 §12](../handoffs/2026-10-22-combat-view-structure-hyeongang-share.md) | 선택·가용성·상태 UI·폐지 접점·승인된 기존 시험·수동 확인 |
| 호진님 | [T-7/공격 Drag 공유 V4-m](../handoffs/2026-10-20-tutorial-t7-combat-input-handoff-Hojin.md) | 실습 회복/통과 조건·Ready 고정물/대기·기존 Policy 기대 |
| 민형님 | [조립 공유 §27](../handoffs/2026-10-15-combat-scene-display-factory-framework-share.md) | Factory→SceneInput 소유·표시 계약·서비스/등록 경계 |
| View 작업자 사용법 | [stage-input-guide §2.1~2.4](../viewDocs/stage-input-guide.md) | 예약 R·선택/행동·진영/밤·특성·이동 실습 |
| Tracker 위치 | [Script/README](../../../Assets/ProjectL/Script/README.md) | 새 Tracker 위치만 반영. 생성/수명은 사용 안내 참조 |

### 7.3 남은 기능 확인과 외부/기획 후속

| 항목 | 상태/처리 경계 |
|---|---|
| 컴파일·기존 EditMode·M01~M14 | `11c07868` 이후 사용자 전체 정상 보고를 수집해 인수했다(§8.4). 이번 라운드는 DB 에셋을 바꾸지 않았다. 개별 시험 건수/로그는 제공받지 않아 추정하지 않음 |
| UI 구성/표시 | M03/M09/M10/M12도 사용자 전체 정상 보고 범위에 포함된다. 직접 재현/구성별 자료를 확인한 것으로 기록하지 않으며 이후 변경의 검증으로 재사용하지 않음 |
| 항상 이동 불가능한 실습 데이터 | 정상 데이터의 수동 종료/Deployment 재진입 회복은 구현했지만, 영구적으로 이동할 수 없는 데이터의 재시도/안내·목표 정책은 별도 기획/튜토리얼 결정이 필요하다. Jobs/Store/데이터를 임의 수정하지 않았다 |
| 보류된 특성 효과 지원 | Core가 공개한 전체/부분 불가를 표시할 뿐 효과를 재개하지 않았다. 지원 여부는 Core/기획 후속이며 이번 범위의 새 API 요청은 없다 |
| 설정 화면의 복귀 항목 노출 | 후속 담당 UI 정리 대상이다. 이번 승인 범위는 실제 입력 소비/버튼 참조 처리까지다. 예약 액션·키 저장값을 임의 삭제하지 않는다 |

이 목록의 후속 정책/기능을 현재 View 버그로 단정하거나 자동 구현하지 않는다. 실제 인수에서 새 결함이 확인되면 근거와 담당 경계를 별도 기록한다. 외부 메시지 발송과 커밋/push는 사용자가 한다.

### 7.4 구조 이월 (이번 미착수)

범위 원본은 본 계획 §3과 [설계 §12](../specs/2026-10-25-combat-view-stage-round4-design.md#12-후속-구조-개선-목록이번-미착수)다. 선택 UI 원본/계약 분리(b), FactsReader·ActiveActor 명칭(c), 운영 필수화/시험 조립(i), 표면 높이(j), 결과 Consumer(k), 레이어/배경음/정리 공통화(l), 호환 복귀/타임라인 사슬·빈 범위 전용 사실 삭제, 블라인드 구조 A/B 측정은 시작하지 않았다.

Core 대응 코드·공유 문서 작성은 이 부채의 해결이나 구조 점수 상승을 뜻하지 않는다. 현재 Tracker는 기능 대응에 필요한 기존 슬롯/캐시 소유자이고 선택 원본은 여전히 정보창에 있다. 새 후속 라운드의 설계/Task와 사용자 지시 없이 구조 작업을 재개하지 않는다.

후속 설계 원본: [5라운드 설계](../specs/2026-10-26-combat-view-stage-round5-design.md)·[계획](2026-10-26-combat-view-stage-round5-plan.md). 사용자 요청으로 첫 세 항목의 문서만 작성했으며 본 라운드의 기능 인수 결과나 구조 이월 완료를 변경하지 않았다.

## 8. Unity 확인 순서 (V4-n, 사용자 실행)

### 8.1 준비와 기록 원칙

- 현재 기준은 `f616d5d9` + 작업 트리의 a/d/e/f/g/h다. 먼저 재컴파일한 현재 소스로 확인한다. V4-0의 정상 보고는 재사용하지 않는다.
- 평소 정상 진입하던 Boot/Play From Boot 설정을 유지하고 일반 전투를 준비한다. 스테이지/챕터·노드, Database/Fixture, 튜토리얼 여부를 기록한다. 일반 전투는 가능하면 조작 가능한 아군 A/B 두 명 이상과 적이 있는 구성을 쓴다. Fixture 성공을 운영 Database 성공으로 합치지 않는다.
- 기본 키는 G=배치에서 전투 시작, E=공격 모드, Space=턴 종료, R=소비 없는 복귀 예약이다. 키 설정을 바꿨다면 현재 설정으로 확인한다. 우클릭은 취소이며 배치에서 Drag 중이 아닐 때 일반 서브 유닛 위에서는 회수다. 메인 캐릭터는 회수 대상이 아니다.
- 아래 순서를 따르되 이미 행동을 쓴 유닛은 다음 아군 턴에서 새로 준비해도 된다. 일반 전투와 이동 실습, 승리/패배/포기/최종 승리는 각각 필요한 별도 플레이로 확인한다. 기존 Scene/Prefab/DB나 Core 상태를 영구 수정해 조건을 만들지 않는다.
- 결과는 **정상 / 실패 / 부분 확인 / 미확인**으로 기록한다. 재현할 수 없는 조건은 이유를 쓰고 미확인으로 남긴다. 컴파일 오류가 있으면 1번 결과부터 전달하고 플레이 확인은 보류한다.

### 8.2 실행 순서와 기대

| 순서 | Unity에서 할 조작 | 정상 기대 / 확인 범위 | 원래 항목 |
|---|---|---|---|
| 1 | 스크립트 재컴파일이 끝날 때까지 기다리고 Console 오류를 확인한다 | 컴파일 오류 0. 이전 CombatSceneInput의 EntityId CS0246을 포함해 오류가 없어야 한다. 있으면 전체 오류 문구/파일/줄을 기록 | Vn.1 |
| 2 | Test Runner의 EditMode에서 기존 시험을 실행한다. 총/성공/실패/무시 수와 실패 이름/메시지를 기록한다 | 현재 코드의 기존 시험 결과를 수집한다. 실패를 기존 문제로 임의 분류하지 않는다. DB 에셋을 별도로 바꿨다면 DataBaseAssetTests 결과도 명시 | Vn.1 |
| 3 | Boot로 일반 전투의 배치에 진입한다. 손패 유닛을 배치하고 Drag 중 우클릭 취소, Drag를 끝낸 뒤 필드 서브 유닛 위 우클릭 회수, 재배치 후 시작 버튼/G를 확인한다 | 배치/취소/서브 회수 정상. 메인 캐릭터는 회수하지 않는다. 시작 확인창/정상 Combat 진입, 버튼 클릭과 가능한 환경의 UI Submit 정상 | M12 |
| 4 | 전투에서 A 선택→A 재누름→Drag, B/적/빈 칸 순서로 클릭한다 | 같은 필드 유닛은 선택 유지, 다른 개체로 전환/빈 칸 해제, 강조 즉시 갱신. 선택 정보와 실제 행동 대상 일치. 덱 핀 토글은 8번에서 별도 확인 | M01 |
| 5 | 새로 행동할 수 있는 아군으로 첫 Drag→우클릭 취소→다시 Drag→끝점에서 놓기를 한다. UI/필드 밖에서 놓기도 확인한다 | 정상 최신 경로 승인 뒤 한 번만 이동한다. 취소/범위 밖 놓기는 실행하지 않는다. 고스트/범위/원본 표시가 정리되고 다시 Drag 가능. 가용성 대기가 이동을 임의 확정하거나 계속 봉쇄하지 않는다 | M02, M08 |
| 6 | 행동 가능한 아군으로 공격 버튼/E→취소/모드 끄기→다시 조준·공격한다. 확인 중 빠른 추가 입력, R 예약 키도 눌러 본다 | 버튼/키가 같은 허용 상태. 확인 중 새 공격 진입 없음, 응답이 늦게 와도 자동 공격/자동 모드 진입 없음, 모드 끄기 정상. R로 되감기·AP 환불·명령 생성 없음. 복귀 버튼은 숨김 | M08, M12 |
| 7 | 아직 행동하지 않은 A의 허용 상태 확인→B로 AP 사용→A 재선택. 이동/공격한 유닛과 AP 부족 상황에서도 버튼·키를 시도한다 | 현재 공용 AP와 Core 판정에 맞게 허용/거절·미사용/이동함/공격함·불가 사유 갱신. AP가 충분하면 정상 허용. 부족하면 실제 시도에 “공용 AP가 부족합니다!” 안내. 가만히 호버/선택만 할 때 안내가 반복되지 않음 | M04, M06 |
| 8 | 덱 핀 켜기/다시 누르기/다른 카드 전환, 정보창 핀/슬라이드/가림 뒤 재표시, A→B 전환을 해 본다. 보유 특성의 설명도 확인한다 | 기존 덱 토글 유지. 비용/행동 문구가 숫자만으로 돌아가거나 A 사유가 B에 붙지 않음. 작은 비용 영역/긴 문구 읽기 정상. Snapshot 전체/부분 불가 표시와 정적 설명을 구별하며 원래 아이콘/설명 유지 | M03, M10 |
| 9 | A→B→A를 빠르게 반복하고 Drag 시작→취소를 섞는다. 공격 모드/경로 지정·UI/확인창·결과 재생·적 응답 중 필드 입력도 확인한다 | 옛 개체 사유/강조/명령이 적용되지 않고 입력 가능 상태로 회복. 보호 구간에서 선택 부수효과·중복 이동/공격·조작 멈춤 없음. 빠른 조작만으로 인위적 지연 응답/모든 슬롯 경우를 확인했다고 간주하지 않음 | M02, M05 |
| 10 | 빈 칸으로 선택 해제→턴 종료 버튼/Space. AP를 0까지 쓸 수 있다면 0에서도 기다렸다가 수동 종료한다 | 선택/AP와 무관하게 정상 수동 종료. AP=0만으로 자동 종료되지 않음. 적 응답/재생 중 중복 턴 종료 없음, 다음 아군 턴에서 새 행동 가능 | M07 |
| 11 | 충분히 생존 가능한 일반 전투에서 마지막 아군 턴 종료→8번째 적 응답→다음 배치를 관찰한다. 필요하면 에디터 Pause/Step으로 표시를 본다 | 적 응답 8/8 동안 현재 진영이 적 응답임을 표시하며 밤 완료를 앞당기지 않음. 실제 밤 종료 뒤 배치/다음 밤 정상 진입. 전환 전 선택/사유/고스트/입력 잔재 없음 | M09, M13, M14 일부 |
| 12 | 튜토리얼의 이동 실습으로 별도 진입한다. 이동하지 않고 수동 종료→다음 턴 이동을 확인한다. 다시 준비해 이동 없이 마지막 적 응답까지 종료→Deployment에서 정상 시작/G→실제 이동을 확인한다 | 무선택/AP부족/행동 사용 상태에도 수동 종료로 회복. 같은 전투의 미완료 실습만 정상 재진입. 종료/재진입만으로 통과하지 않고 실제 Move의 진입/표시 완료 뒤 통과. 통과 뒤 다음 설명 대기 구간에는 재진입 예외 없음, 설명 중 조작/금지 공격은 기존 제한 유지 | M11 |
| 13 | 자연스럽게 재현 가능한 선택 유닛 사망/필드 이탈·단계 전환에서 정보/입력을 확인한다. 종료/씬 전환 중에도 이전 필드 입력을 눌러 본다 | 사라진 개체의 선택/행동 사유/늦은 입력을 적용하지 않음. 입력/고스트 정리, 다음 연결에서 정상 회복. SceneRoot를 임의 비활성화해 조건을 만들지는 않음 | M13 |
| 14 | 각 별도 플레이로 일반 승리·패배·포기·최종 승리를 확인한다 | Run이 이어지는 일반 승리→StageSelect. Run이 끝나는 패배/포기/최종 승리→새 Run 준비 후 Lobby. 마지막 결과 표시/확인이 끝난 뒤 전환하고 이전 Combat 입력이 남지 않음 | M14, M13 |

### 8.3 별도 재현 조건과 결과 양식

- M05의 의도적인 늦은 응답/미응답 슬롯 선점, M06의 Deferred, M10의 전체/부분 불가·미지 사유/정적 키 경로, M11의 통과 뒤 짧은 제한 유지 구간, M12의 returnButton=null **초기 진입**, M13의 정지/해제 경합은 일반 플레이만으로 모두 만들기 어려울 수 있다. 직접 확인하지 못한 세부 조건은 따로 미확인으로 남긴다. 빠른 클릭/연결 버튼 숨김만으로 해당 세부 조건을 통과 처리하지 않는다.
- M12의 미연결 구성은 원본 Scene/Prefab을 임의 수정하거나 Play 중 뒤늦게 필드를 비워 검증한 것으로 대체하지 않는다. 기존 시험의 null Bind/표시 고정물은 실제 씬 초기 진입과 별도 근거다. 재현할 구성이 없으면 구성 미확인으로 기록한다.
- 입력이 멈추거나 새 오류가 나면 순서 번호·스테이지/모드·선택 유닛·직전 조작·Console 오류 전문을 기록한다. Runtime 오류 이후 다른 항목을 계속 확인한 결과는 그 영향을 구분한다.

| 결과 구분 | 현재 상태 | 기록할 내용 |
|---|---|---|
| 컴파일 | 정상(사용자 보고) | `11c07868` 이후 결과. 개별 로그 미첨부 |
| 기존 EditMode | 정상(사용자 보고) | 개별 총/성공/실패/무시 수는 미제공 |
| M01~M03 | 정상(사용자 보고) | 순서 4/5/8/9의 선택·입력 보호·UI |
| M04~M08 | 정상(사용자 보고) | 순서 5/6/7/9/10의 가용성/AP·슬롯·행동·수동 종료 |
| M09~M11 | 정상(사용자 보고) | 순서 8/11/12의 밤·특성·실습 |
| M12~M14 | 정상(사용자 보고) | 순서 3/6/11/13/14의 폐지 입력·기존 입력·수명·승패/복귀 |

보고 예시(실행 결과가 아닌 양식): `1 정상 / 2 총 __·성공 __·실패 __·무시 __ / 3~6 정상 / 7 실패: B 공격 뒤 A 선택 시 ... / 8~14 미확인`. 일부만 확인해도 그 범위부터 받으며 실패/미확인/기획·구성 한계와 구조 이월을 구분한다. 위 현재 결과표는 다음 §8.4의 사용자 보고를 반영했다.

### 8.4 2026-10-26 — V5-0에서 받은 최종 결과

- V5-0 기준선 질문은 `11c07868` 이후의 컴파일·기존 EditMode·M01~M14 결과를 명시했고, 사용자 **"컴파일·기존 시험·M01~M14 모두 정상"** 답변을 받았다. f616d5d9의 이전 보고를 재사용한 것이 아니다.
- Vn.1의 컴파일/기존 시험 및 Vn.2~3의 기능/종료 회귀를 사용자 정상 보고로 수집했고, Vn.4의 미수집 상태를 해소하여 **V4-n 기능 인수 완료**로 기록한다. 기획/외부 후속과 구조 이월은 §7.3~7.4대로 남기며, 선택/Facts/조립 세 항목만 승인된 V5 계획으로 진행한다.
- 직접 Unity/MSBuild/시험을 실행하거나 개별 로그/시험 수/모드·스테이지를 확인하지 않았고 추가 수치를 추정하지 않는다. 코드/시험/에셋 변경 없이 현재 상태와 결과 문서만 갱신했다.
