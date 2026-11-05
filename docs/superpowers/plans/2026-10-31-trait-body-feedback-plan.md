# 특성 몸체 텍스트·효과 Task 계획

설계 원본: [특성 발동 몸체 텍스트·효과 설계](../specs/2026-10-31-trait-body-feedback-design.md).

상태: **3차 Phase K 구현·코드 컴파일·정적 검증 완료 — Activation별 색·크기·직접 문구·Position Offset, Prefab Override 유지**. 현재 계획/실행 기록은 §10이다. 1차 A~E와 2차 F~J는 구현 이력으로 보존하며 실제 Play 검증은 사용자 후속이다.

후속 사용자 요청 **“계획대로 Phase F~J 순차적으로 구현하고 마지막에 컴파일·정적 검증 및 내용 정리해줘. 에디터 Play 검증은 내가 따로 할게.”**에 따라 F부터 J까지 순차 구현하고 Phase별로 보고 후 계속 진행했다. 실제 Play 검증은 수행하지 않았다.

§1~8의 Body Text Font·Color/Font Size/Screen Offset·코드 직접 생성은 1차 이력이다. 2차 구현에서는 §9에 따라 프리팹으로 이전하고 최종적으로 제거한다.

## 1. 범위와 의존성

| 항목 | 현재 판단 | 처리 |
|---|---|---|
| 공용 발동/희생 큐 경로 | 기존 경로 있음 | 텍스트 조각 추가, 운영/시험 공용 Factory에서 주입 |
| HUD 동시 시작 | 발동과 State는 별도 사건 | 결과 계획에서 대응, Render 직후 피드백 실행 |
| 가속 X N | Magnitude는 공격력. 실제 Moved 사건 제공 | 실제 자발 경로 사건 집계. PathMovement는 대응이 확인된 단일 경로 대조에만 사용 |
| 상태 대응 | TraitState에 TraitKey 직접 필드 없음 | 가속 보유 상태·출처가 포함된 AttackGrants 전후 값으로 첫 두 줄 연결 |
| 이동 중 몸 중심 | 정지 기하와 실제 표시 자세 조회 구분 필요 | 읽기 전용 조회만 확장, 접촉/이동 계산 변경 없음 |
| 텍스트 폰트·출력 | 한글 TMP 폰트·출력 카메라 존재 | 공통 표시 설정에 참조, 전용 텍스트 Canvas 조립 |
| Core 선행 변경 | 현재 대표 요구는 공개 사건으로 설계 가능 | 불명확한 합성 출처가 발견되면 Core 인계 요청. 추측 구현 금지 |
| 미룬 HUD 설명 팝업 | 사용자 후속 보류 | 수정하지 않음 |

Core·FieldCamera·Architecture는 수정하지 않는다. 커밋·push·스테이징·외부 메시지 발송 없음. 기존 다른 작업 변경을 보존한다. 새 View 테스트 코드·자동 검증 도구를 만들지 않는다.

## 2. 파일과 책임 배치

아래 이름은 구현 위치 기준이다. 책임을 유지한 파일 분할은 가능하나 입력 의미·타이밍·수명 변경은 계획 변경이다. 새 `.cs`에는 `.meta`를 함께 만든다.

| 영역 | 예정 파일/역할 |
|---|---|
| `DataBaseSystem/Data/View/Table/Trait/Item/` | `TraitActivationVisual`: Cue Timing·선택적 Body Text 입력/검증 |
| `DataBaseSystem/Data/View/CombatPresentation/` | `CombatBodyTextInput/Data`, 시점·대상·Count의 구체 enum/값 타입 |
| 공통 `CombatPresentationSettingsDataSO` | Body Text Font·Stack Gap 입력/검증 |
| `Combat/Playback/Support/` | `CombatTraitFeedbackPlan`·상태 대응 해석기·이동 횟수 집계. `CombatActivationRows/CueSources`에서 이름·추가 값 제공 |
| `Combat/Presentation/Text/` | `CombatBodyTextWindow`: 요청·페이드·풀·투영·정리 |
| `Combat/Presentation/Pieces/` | `CombatBodyTextPiece`: Accent/희생 Start에 연결 |
| `Combat/Playback/Handlers/` | `CombatTraitEventHandler`: ActivationStart/StateApplied 분기와 중복 방지 |
| `Combat/Playback/Support/CombatGuardLead.cs` | 실제 Interception 앞 항목에 텍스트 시작 연결 |
| `CombatSceneEventPresenter`·관련 수명 조립 | BeginResult 계획 생성·Render 후 통지·Tick/Cancel/Release. 기능 계산은 도우미에 위임 |
| `Combat/Display/Unit/` | 현재 대여된 유닛의 실제 몸 중심 조회 |
| `Factory/Runtime/InGame/View/` | 공용 사건 재생 Factory와 텍스트 전용 조립. 운영/시험의 카메라·설정·자원 연결 |
| `Editor/CombatPreview/` 및 기존 큐 Inspector | 가능한 조합·한계 안내, 조립 사전 진단 |
| View SO·조립 Presets | 대표 세 특성, 공통 폰트 설정, 가속/깔끔한 뒷정리/희생 대표 사건 |
| `viewDocs/` | 기존 Activation 참조·시험 도구 가이드의 새 필드/타이밍 사용법 |

## 3. Phase A — 입력·검증·에셋 조사

- [x] A1. 착수 시 git 상태와 실제 에셋 경로/GUID·폰트·Inspector를 확인하고 기존 값 보존 기준을 기록한다. `Assets/AGENTS.md`도 해당 코드/에셋 수정 전에 읽는다.
- [x] A2. Cue Timing 기본 ActivationStart, Body Text 기본 비활성 입력과 불변 런타임 값 생성·검증을 추가한다.
- [x] A3. 이름 기본값/덮어쓰기, Source/Targets, Count, 색·크기·오프셋·유지/페이드 입력을 구현한다. StateApplied/Count 허용 조합은 설계의 첫 지원 표로 제한한다.
- [x] A4. 공통 폰트·간격 입력을 추가한다. 비활성 기존 에셋에 불필요한 필수 참조를 요구하지 않는다.
- [x] A5. Activation 캐시가 입력 사본·표시 이름을 한 번 읽도록 확장한다. 기존 Cues/UnitActions/공격 덮어쓰기 값은 보존한다.
- [x] A6. 컴파일과 기본값 대조. 기존 줄이 텍스트 없이 같은 시점으로 읽히는지 확인한다.

리뷰 산출물: 새 Inspector 항목·기본값·허용 표·실제 수정 예정 SO 목록. Phase B 전에 보고한다.

## 4. Phase B — 텍스트 재생·현재 몸 추적

- [x] B1. 출력 카메라·폰트·현재 몸 중심 조회를 주입받는 전용 텍스트 창구/Canvas/풀을 구현한다. HUD 프리팹을 수정하지 않는다.
- [x] B2. 현재 표시 자세의 몸 중앙 조회를 추가한다. 이동/Stand 대여 중 Transform을 따르고 없는 몸·무효 대여는 출력하지 않는다.
- [x] B3. 유지 → 선형 페이드, 화면 밖 숨김, 슬롯 유지 방식의 28px 쌓기, 몸 제거 시 회수를 구현한다.
- [x] B4. 기존 Accent 조각으로 출력 요청을 시작한다. 텍스트가 사건/결과 완료·Stand/Attack을 막지 않도록 한다.
- [x] B5. 기존 재생기 목록에서 공격 Gate의 조기 반환보다 먼저 진행되는 텍스트 Player와 렌더 직전 투영을 연결한다. 게임 Pause에서 시간 정지, 카메라 이동/Shake 뒤 위치 갱신을 확인한다.
- [x] B6. Cancel/Release·생성 실패·늦은 완료·다음 결과에서 자연 만료하는 수명을 연결하고 공용 Factory에 조립한다.
- [ ] B7. 사용자 후속 Play 확인: 가독성·페이드·이동 추적·여러 출력·Stop 후 남은 객체를 확인한다.

리뷰 산출물: 동작 화면·자원 소유권·수명 연결·미확인 항목. 상태 사건과 연계했다고 이 단계에서 보고하지 않는다.

## 5. Phase C — 발동·HUD·이동 횟수·희생 연결

- [x] C1. 결과별 파생 피드백 계획을 만든다. 사건 인덱스+발동 인덱스+표시 대상으로 중복 실행을 막는다.
- [x] C2. 가속 보유량 증가, 출처가 포함된 특성 공격력 부여의 전후 사실 대응을 구현한다. 대상 ID만으로 다음 State에 붙이지 않는다.
- [x] C3. StateApplied 줄의 텍스트·Cues를 발동 시점에는 유보하고 실제 State ApplyFacts/Render 직후 실행한다. Origin·대상별 출력·Shake 한 번 정책을 지킨다.
- [x] C4. 가속 N을 이번 자발 이동 구간의 실제 Moved 사건에서 집계한다. 강제 이동·회수·UnitAction·이전 결과는 제외한다. N과 Magnitude를 분리한다.
- [x] C5. 여러 유닛/여러 경로/다른 특성 Effects가 섞인 경우와 모호한 관계의 진단을 확인한다. 증명할 수 없는 경우는 설계의 생략/이름만 출력 정책을 적용한다.
- [x] C6. `CombatGuardLead`의 실제 가로채기 Start에 수호자 텍스트를 붙인다. UnitActions 동시 시작·피격 후 복귀·죽음·공격 완료 정책은 보존한다.
- [x] C7. Cancel/Reset/Detach에서 계획과 활성 텍스트를 비우고, BeginResult만으로 이전 텍스트를 지우지 않는지 확인한다.
- [x] C8. 컴파일 및 대표 사건 대조. 공개 사실만으로 경로/상태 연결을 입증하지 못하는 운영 경우는 Core 전달 문서에 요청하고 미해결로 남긴다.

리뷰 산출물: 사건→State 연결표, N 집계 예시, 희생 시작점과 취소 결과. 규칙·Core 파일 변경 없음 확인.

## 6. Phase D — 에디터·대표 SO·조립 환경

- [x] D1. Kind/Context/Cue Timing/Count 변화에 따른 필드 표시·허용 조합·경고를 추가한다. 기존 Anchor Start와 새 Cue Timing의 차이를 설명한다.
- [x] D2. 조립 컴파일러에서 StateApplied의 같은 Packet·Activation→State·실제 변화 근거와 가속 이동 횟수 자료를 사전 진단한다. 상태를 자동 생성하지 않는다.
- [x] D3. 대표 SO 3개에 텍스트를 활성화한다. 깔끔한 뒷정리/가속은 StateApplied, 희생은 ActivationStart·Source. 가속만 CompletedMovementCells를 쓴다.
- [x] D4. 기존 Effects/Shake 값은 보존하고 첫 세 줄의 지연 0을 확인한다. 공통 설정에 기존 한글 TMP 폰트를 연결한다. GUID·아이콘·특성 수치·희생 UnitActions는 보존한다.
- [x] D5. 가속·깔끔한 뒷정리 프리셋의 Activation→State 순서를 운영과 맞춘다. 가속 N=2, Magnitude=10처럼 서로 다른 예를 마련한다. 이동 횟수 가짜 입력 필드는 추가하지 않는다.
- [ ] D6. 희생 생존/사망 프리셋과 단독 Moment 제한 안내, 다른 기존 Start 프리셋의 호환을 확인한다.
- [x] D7. 컴파일·에셋 직렬화/참조 대조. 다른 작업자의 수정과 충돌하면 덮어쓰지 않는다.

리뷰 산출물: SO별 입력 표·조립 도구 사용 예·호환/제한 목록.

## 7. Phase E — 통합 검증·사용법·정리

새 테스트 코드는 만들지 않는다. 정적 검증·Unity 컴파일·수동 Play를 사용한다. 기존 종료 오류는 이 작업의 수정 범위가 아니다.

| 확인 | 기대 결과 |
|---|---|
| 깔끔한 뒷정리 | 다른 아군 공격 이후 실제 부여된 유닛 HUD와 텍스트/효과가 같이 시작. 자기 공격·단순 HUD 재갱신으로 중복 없음 |
| 가속 1칸/여러 칸 | 끝에서 한 번, `가속 X 1`/`가속 X N`. Magnitude와 다른 N을 확인 |
| 가속 중단/강제 이동 혼합 | 실제 자발 진입 수만 출력. 예정 칸 수·강제 밀림을 합산하지 않음 |
| 가속 반복·복수 유닛 | 이전 결과/다른 유닛의 이동 수가 섞이지 않음. 새 발동은 새 텍스트 |
| 가속 상태 연결 | 이동 완료→실제 상태 Render→문구. 가속 외 다른 이동 특성 Effects가 중간에 있어도 정확히 대응 |
| 희생 생존 | 나가기와 동시에 수호자 문구 시작, 이동 중 추적, 피격/복귀 정상 |
| 희생 사망 | 보호 대상에게 문구 없음. 수호자 제거 후 잔여 텍스트/무효 참조 없음 |
| 겹침·카메라 | 같은 유닛 여러 문구가 겹치지 않고 페이드. 카메라 이동/Shake·화면 밖/복귀 위치 정상 |
| Pause/Stop/Reset/반복 | Pause는 시간 정지. Stop/Reset/반복 시작에서 잔여 문구·예약 출력 없음 |
| 다음 결과 | 텍스트가 명령 진행을 막지 않고 자연 소멸. 최종 HUD Render로 재출력되지 않음 |
| 기존 Start·추가 공격 | 미설정 줄 동작 유지. 기존 Effects·Shake·추가 공격·희생 행동에 불필요한 지연 없음 |
| 반복 자원 | Canvas 연결당 하나, 활성 출력 수가 시간 경과 후 0, 풀의 불필요한 누적·구독 잔류 없음 |
| 조립 도구 오류 | State 누락/앞선 State/Packet 분리/Count 자료 없음은 명확한 사전 진단. 정상 입력은 같은 운영 경로 재생 |

- [x] E1. 런타임/Editor 컴파일과 변경 파일의 형식·meta·직렬화 기본값·참조를 확인한다.
- [ ] E2. 사용자 후속: 표의 대표 시나리오를 수동 Play하고 조립 결과와 실제 인게임 Core 발동 확인을 구분해 기록한다. 실행할 수 없는 항목은 미확인으로 적는다.
- [x] E3. `trait-effects-and-activation-reference.md`, `trait-activation-preview-guide.md`에 설정 위치·시점·N 의미·단독 합성 한계를 갱신한다.
- [x] E4. SSOT/SOLID5/FW/기능 독립성 재검토와 실제 변경 파일·검증 결과·잔여 의존성을 이 계획의 실행 기록에 정리한다.
- [x] E5. 사용자에게 사용법과 확인 결과를 보고한다. 커밋·push하지 않는다.

## 8. 실행 기록

2026-10-31: 사용자 요청에 따라 설계·Task 계획만 작성했다. 가속의 칸별 통지 요구는 폐기하고 이동 완료 시 `가속 X N` 한 번 출력하는 요구를 반영했다. 코드·SO·씬·프리팹 변경과 Unity 실행은 하지 않았다. 모든 Task는 미착수다.


### 2026-10-31 구현 기록 — A~D

- 사용자 “계획대로 Phase A~E 순차적으로 구현하고 마지막에 검증 및 내용 정리”로 일괄 구현 승인. Phase별 중간 보고 후 계속 진행했다.
- A: 선택적 BodyText/시점 입력·값 검증, 공통 폰트/간격, 이름/캐시 확장. 기존 기본값은 텍스트 없음/ActivationStart. 실제 대상 SO는 ViewGroup/TraitVisual/Item의 Acceleration·CleanSweep·Sacrifice. 폰트는 기존 SCDream7 SDF 참조이며 폰트 파일 자체를 수정하지 않았다.
- B: 전용 일시 텍스트 Canvas/풀·페이드·슬롯·투영과 실제 표시 몸 추적, 무효 대여 차단. Accent 시작 조각과 비차단 Player로 공용 재생 Factory에 연결. 수동 Play 항목 B7은 E에서 확인한다.
- C: 공개 이동 사건 집계, 출처가 포함된 공격력 부여/가속 보유 상태 대응, Render 후 통지, 실제 가로채기 Start 연결. 최신 Buffer나 Core Rule 호출 없음.
- D: Inspector의 조건부 텍스트 필드·지원 시점 안내, 공용 계획을 사용한 조립 사전 진단. 대표 SO 3개, 공통 폰트/간격, 가속·깔끔한 뒷정리·ManualStep 프리셋 연결. 가속은 같은 Packet의 Move → Activation → State, N=2/Magnitude=10.
- 컴파일: 올바른 asmdef 경계로 새 소스만 보완한 MSBuild 런타임 `phase-e-runtime-build.log`, Editor `phase-e-editor-build.log` 오류 0. 초기 스크래치 빌드의 Core 소스 중복 포함을 제거했으며 이후 빌드만 최종 증거로 사용한다.
- Assets/AGENTS UI 등록 검토: 새 텍스트는 독립 개폐 HUD/화면이 아닌 사건 Accent의 세부 표시다. Open/Hide·UIId·입력/이력 기능이 없고 Presenter 연결 수명이 Canvas/풀을 회수한다. 별도 UI 서비스·루트 Export를 만들지 않는다. 기존 등록된 HUD/팝업은 변경하지 않았다.


### 최종 정리와 검증 인계

사용자 추가 지시: **“일단 작업 진행하고 에디터 검증 따로할게”**. 이에 따라 공유 HUD 참조 보완을 이번 범위에 추가하지 않고, 코드/에셋/문서 작업을 완료했다. B7·D6·E2의 실제 Play 확인은 사용자 후속이다. 이 항목을 성공으로 기록하지 않는다.

| 검증 | 결과/근거 |
|---|---|
| 런타임 컴파일 | VS MSBuild 오류 0, `Temp/BodyFeedback/phase-release-runtime-build.log`. 최종 자원/연결 진단 보완 후 release 재빌드 기록도 함께 확인 |
| Editor 컴파일 | VS MSBuild 오류 0, `Temp/BodyFeedback/phase-release-editor-build.log` |
| 새 소스 | 7개 모두 meta 존재, UTF-8/LF, U+FFFD 없음, SerializeReference 추가 없음. Allman 형식 정리 전후 토큰 동등성 확인 |
| 대표 SO 값 | Git 원본과 비교해 피드백 입력 외 전체 값 일치. CleanSweep/Sacrifice 기존 Cues·행동·아이콘·설명 보존. Acceleration은 새 AttackGranted 줄만 추가 |
| 공통 설정 | Git 원본과 비교해 기존 값 일치, 기존 한글 TMP 폰트 참조/간격만 추가. 폰트 파일은 다른 작업의 기존 수정 유지 |
| 금지/타 작업 영역 | Core·Framework·FieldCamera·테스트·공유 HUD 프리팹 변경 없음. 기존 다른 HUD 작업 변경 보존 |
| 수동 Play 시도 | TraitActivationPreview 진입 후 별도 HUD 변경의 필수 `categoryImage` 미연결 예외로 유닛 생성 중단. 텍스트 표시/소멸의 실제 성공 증거가 아님 |
| 종료 상태 | 실패한 Play를 종료해 Combat 편집 씬으로 돌아온 것까지 확인. 이후 사용자가 에디터 검증을 맡기로 하여 UI 입력 중단 |
| 기존 오류 | Setting 미등록 종료 오류는 기존 방침대로 수정하지 않음. 이번 재생의 직접 차단 원인은 categoryImage |

구조 재검토: 서비스 생성은 공용 CombatEventPlaybackFactory, 소유/종료는 기존 사건 Presenter의 Player 목록이다. 새 Global/루트 서비스 등록·Export·resolver 조회 없음. 텍스트는 독립 Open/Hide UI가 아닌 사건 Accent 세부 위젯이다. 상태/이동 사실은 공개 Result만 읽으며 View가 Core 규칙을 계산하거나 Buffer를 쓰지 않는다. 같은 스냅숏 재사용도 사건/발동 인덱스로 분리하고, Render 후 통지와 세대 검사를 통해 취소 뒤 늦은 텍스트를 차단한다.

사용법: [특성 몸체 텍스트 사용법](../viewDocs/trait-body-feedback-guide.md). 에디터 확인 순서는 HUD 참조 연결 확인 → 가속 X 2(증가량 10) → 깔끔한 뒷정리 HUD 동시 시작 → 희생 생존/사망 추적 → Stop/Reset/반복 회수다. 이 순서는 확인 지침이며 실제 완료 기록이 아니다.


## 9. 2차 실행 계획 — 프리팹 전환과 검토 보완

설계 원본은 같은 설계 문서의 수정된 §3~5·§9다. [구조 검토](../specs/2026-10-31-trait-body-feedback-structure-review.md)의 R1~R4를 함께 처리한다. 아래 체크 결과는 이번 2차 구현·검증 기록이다. 1차 체크 결과로 대체하지 않았다.

### 9.1 변경 범위와 보존 계약

| 항목 | 2차 결정 |
|---|---|
| 외형 | 글자·배경·폰트·색·크기·내부 위치는 텍스트 프리팹에서 편집 |
| 문구/대상/시간 | Activation에 유지. 기본 이름·Override·Source/Targets·Count·Cue Timing·Hold/Fade 보존 |
| 기본/개별 외형 | 공통 Body Text Prefab + 선택적 줄별 Prefab Override. 다른 외형은 Variant로 작성 가능 |
| 코드 생성 범위 | Canvas 호스트/연결/풀은 Factory, 텍스트 인스턴스는 프리팹 Instantiate/재사용. Window가 TMP/외형을 직접 만들지 않음 |
| 지원 정책 R1 | 불변 공용 프로필 조회와 근거 해석기 등록. Inspector의 숫자/별도 허용 표 제거 |
| Count 수정 R2 | Kind 변경 후에도 None으로 고칠 수 있음. 지원하지 않는 새 선택만 제한 |
| 캡슐화 R3 | 불변 Plan + private 실행 세션. 외부 Map.Remove/플래그 대입 제거 |
| 조회/구독 R4 | Tick은 시간·페이드, 몸/대여 조회는 활성 렌더 투영에서 한 번. 0개일 때 구독 해제 |
| 유지하는 실제 동작 | 가속 X N, HUD 직후 상태 피드백, 희생 시작, 기존 효과·행동·공격 Gate·generation·취소 의미 |
| 사용자 후속 | Unity Play·가독성 검증은 사용자가 수행. 공유 HUD 참조/설명 패널 수정은 제외 |

### 9.2 선행 조사와 파일 책임

| 경계 | 예정 변경 |
|---|---|
| `DataBaseSystem/Data/View/CombatPresentation/` | CombatTraitFeedbackProfiles/프로필 값·근거 종류, BodyText 입력의 Prefab Override. 외형 구 필드는 H에서 최종 제거 |
| `DataBaseSystem/Editor/CombatBodyTextDrawer.cs` | 공용 프로필 조회, 수정 가능한 Count/Timing, 프리팹 설정·Variant 안내 |
| `Combat/Playback/Support/` | 불변 FeedbackPlan·FeedbackSession·상태 근거 처리기/등록 소비. 공개 Result 사실만 해석 |
| `Combat/Presentation/Text/` | CombatBodyTextView(표시 컴포넌트), Prefab별 Pool, Window 시간/투영. View는 특성/카메라/풀을 모름 |
| `Factory/Runtime/InGame/View/` | CombatBodyTextFactory의 호스트/풀 생성·실패 정리, 기존 공용 Factory 연결, 근거 처리기 불변 등록 |
| `CombatPresentationSettingsDataSO` | Body Text Prefab 참조, 기존 Stack Gap 유지. Body Text Font는 H에서 이전 후 제거 |
| `Prefabs/UI/CombatOnly/Feedback/` | TraitBodyText.prefab와 실제 기존 외형 차이가 있을 때만 필요한 Variant, meta |
| 기존 TraitVisual SO | 새로운 기본/Override 참조로 이전. 외형 구 필드 제거. 발동 내용/효과/행동/GUID 유지 |
| `Dev/TraitPreview/Scenario/` | 운영과 같은 프로필/근거 해석/프리팹 검증. 합성 규칙 추가 없음 |
| 문서 | 사용법·확인 목록·리뷰 R1~R4 결과 갱신. 이전 검증 결과를 신규 구현 증거로 재사용하지 않음 |

Core·FieldCamera·Architecture·Framework 공용 파일·HUD.prefab·기존 폰트 에셋은 수정 대상이 아니다. 새 서비스/루트 Export/테스트/자동 검증 메뉴를 만들지 않는다. 생성/이동한 `.cs`·프리팹에는 meta를 함께 다루고 기존 GUID는 유지한다.

### 9.3 Phase F — 공용 지원 정책·Count 수정·계획 캡슐화

- [x] F1. 착수 시 AGENTS·git 상태·활성 BodyText 줄/외형 입력·현재 별도 HUD 변경을 확인한다. 사용자 수정값과 1차 코드 상태를 기록한다.
- [x] F2. TraitKey/Kind를 키로 하는 불변 공용 프로필을 추가한다. 첫 두 상태 근거와 가속 Count만 등록하고 미등록 줄의 기본 Start/None 동작을 유지한다.
- [x] F3. 데이터 검증·Inspector·조립 진단·런타임 계획이 같은 지원 조회를 사용하도록 바꾼다. 370/230/90 숫자와 별도 지원 표를 제거한다.
- [x] F4. 가속/공격력 부여 근거 처리기를 좁은 계약으로 나누고 Factory의 불변 등록 목록에 연결한다. Plan의 TraitKey별 선택 if를 제거한다. 중복/누락 근거 등록은 조립 전에 거절한다.
- [x] F5. Plan을 불변 구성으로 만들고 private 실행 세션에 시작·State 소비·Shake 상태를 모은다. 외부 컬렉션 변경과 Started/Shaken 직접 대입을 없앤다.
- [x] F6. Count가 무효가 된 문맥에서도 None으로 되돌릴 수 있게 한다. Cue Timing도 잘못된 기존 값을 같은 Inspector에서 수정할 수 있게 한다. 단순 그리기에서 자동 저장/초기화하지 않는다.
- [x] F7. 런타임/Editor 컴파일과 지원 표/단독 합성 진단·대표 사건 대응을 정적으로 대조한다. 새로운 특성을 같은 근거에 추가할 때 Text/풀/Inspector 수정이 필요 없는지 검토한다.

리뷰 지점: R1~R3 변경 경계, 공용 프로필 표, 수정 경로, 불변/가변 소유권. 프리팹 전환 전의 기존 외형 출력은 이 단계에서 유지한다.

### 9.4 Phase G — 편집 가능한 텍스트 프리팹·표시 컴포넌트·풀 준비

- [x] G1. 실제 활성 줄 및 비활성 줄에 저장된 맞춤 폰트·색·크기·오프셋 조합을 조사해 이전 표를 만든다. 현재 세 줄의 동일 외형은 기본 프리팹 하나로 대응하고, 사용자 변경으로 차이가 있으면 조합별 Variant를 계획 안에서 작성한다.
- [x] G2. RectTransform/CanvasGroup/Content/Label과 CombatBodyTextView를 가진 기본 프리팹을 만든다. 기존 SCDream7 SDF를 참조하며 폰트 파일 자체는 다시 저장하지 않는다.
- [x] G3. View의 명시적 내부 참조·폰트·계층·입력 비활성·페이드 소유권을 검사한다. 문구/알파/가시성/Reset만 구현하고 Update·서비스 조회·특성 분기를 넣지 않는다.
- [x] G4. 프리팹별 Pool의 생성·Rent/Return·원래 풀 반환·중복 반환 방지·Release를 구현한다. 초기화 시 문구/일시 상태만 지우고 프리팹 외형을 유지한다.
- [x] G5. CombatBodyTextFactory가 Canvas 호스트/풀 자원을 만들고 실패 시 회수하도록 준비한다. 호스트는 소유 Scene의 독립 Overlay 루트이며 출력 카메라의 Display/투영을 따른다. 별도 MonoBehaviour Tick/새 UIId는 없다.
- [x] G6. 새 파일/meta·프리팹 참조·런타임/Editor 컴파일을 확인한다. 에셋 편집 결과의 실제 Play는 사용자 후속으로 남긴다.

리뷰 지점: 프리팹 계층/편집 위치·외형 이전 표·풀/호스트 소유권. 이 단계는 자원 준비이며 운영 경로 전환은 H에서 한다. 임시로 두 외형 우선순위를 선택하는 런타임 fallback은 추가하지 않는다.

### 9.5 Phase H — 공용 런타임 전환·에셋 이전·몸 조회 정리

- [x] H1. 공통 Body Text Prefab와 줄별 Prefab Override 입력을 연결한다. 활성 줄마다 선택된 참조를 검증하고 비활성 입력이나 사용하지 않는 기본 프리팹은 요구하지 않는다.
- [x] H2. Window에 호스트/풀/출력 카메라/몸 조회를 주입한다. EnsureRoot/CreateLabel과 코드 폰트·색·크기·보정 덮어쓰기를 제거한다. 공용 Factory를 통해 운영/시연이 같은 경로를 사용하게 한다.
- [x] H3. 대표 SO와 실제 다른 활성 줄을 G의 이전 표에 따라 연결한다. 구 Color/Font Size/Screen Offset·공통 Body Text Font를 코드와 SO에서 제거한다. 구/신형 숨은 영구 fallback을 남기지 않는다.
- [x] H4. Tick은 시간·페이드·만료만 처리하고, 몸/대여 조회는 렌더 직전 활성 항목별 한 번으로 모은다. 활성 0→1 구독·마지막 Return/Cancel/Release 해제를 구현한다. 첫 투영 전 원점 노출과 한 프레임 위치 지연을 방지한다.
- [x] H5. 프리팹별 재사용과 빈 슬롯 재사용, 작성된 기본 알파·외형 보존을 연결한다. 복귀/사망/다음 Packet/자동 반복에서 소유 풀·호스트·표시가 남지 않게 기존 수명에 연결한다.
- [x] H6. 실제 희생 Start와 상태 Render 직후 시작, 가속 N/Magnitude 구분, Effects/Shake/UnitActions/공격 Gate/generation을 보존한다.
- [x] H7. 기존 GUID·이름·아이콘·설명·사건 입력·시간·효과/행동 값을 이전 전 기록과 비교한다. 런타임/Editor 컴파일과 생성 실패/반복 해제 경로를 정적으로 확인한다.

리뷰 지점: 외형 원본이 프리팹 하나로 정리됐는지, 공개 사실과 동작 시점 보존, R4의 조회/구독 구조. H 완료 시 생산 코드가 구 필드를 읽지 않아야 한다.

### 9.6 Phase I — Inspector·조립 도구·에셋 연결 마무리

- [x] I1. Body Text 입력에서 외형 구 필드를 제거하고 기본 프리팹/Override 선택·수정 위치를 안내한다. Override가 없으면 공통 설정을 사용함을 명확히 표시한다.
- [x] I2. R2의 Kind 변경 → Count=None 수정, Cue Timing 무효 수정, Enabled on/off를 정적으로 대조한다. Scene 객체/잘못된 프리팹·필수 내부 참조 누락은 대상 필드를 명시한 진단으로 막는다.
- [x] I3. 조립 사전 진단과 운영이 같은 프로필/근거/프리팹 검증을 사용하는지 확인한다. StateApplied 단독 Moment는 기존처럼 조립 안내를 유지하며 상태를 자동 생성하지 않는다.
- [x] I4. 가속/깔끔한 뒷정리/희생/ManualStep 프리셋과 기존 간단 프리셋의 GUID·입력을 보존한다. 표시 외형 전환 때문에 사건 순서·피해·N을 바꾸지 않는다.
- [x] I5. 런타임/Editor 컴파일·meta·직렬화/참조 대조를 마친다. 사용자가 에디터에서 확인할 조작/기대 결과를 정리한다.

리뷰 지점: 디자이너가 코드/YAML 수정 없이 프리팹 외형과 발동 입력을 각각 편집할 수 있는지, 공유 HUD 설명 필드와 혼동하지 않는지.

### 9.7 Phase J — 정적 검증·구조 재검토·사용자 검증 인계

새 View 테스트 코드나 자동 검증 메뉴를 만들지 않는다. 에디터 실제 재생은 사용자 후속이라는 지시를 유지한다.

- [x] J1. 최종 런타임/Editor 컴파일, diff 형식·meta·새 참조·구 필드 잔존 여부를 확인한다. 문서 수정 자체를 구현/실행 성공으로 기록하지 않는다.
- [x] J2. R1~R4 및 SSOT/SOLID5/JMH/FW/캡슐화/기능 독립성을 실제 코드로 다시 검토한다. 필요 없는 null fallback·조건문·매 프레임 LINQ/Find/GetComponent/할당을 확인한다. 필요한 시간/투영 검사는 유지한다.
- [x] J3. 생성 주체→소유/해제→기존 Player 연결→서비스 등록/Export/조회가 추가되지 않았음을 기록한다. FW 비적용 항목을 통과로 과장하지 않는다.
- [x] J4. 프리팹 외형 편집, 공통 기본/줄별 Override, 유지/페이드, 몸 보정, 합성 진단과 구형 에셋 병합 시 이전 방법을 사용법에 갱신한다.
- [x] J5. 아래 수동 확인 목록을 사용자에게 넘기고 모든 Play/Profiler 미확인 항목을 명시한다. 코드/에셋/문서 수정 결과와 외부 차단 원인을 구분해 정리한다. 커밋/push/스테이징하지 않는다.

| 사용자 후속 확인 | 기대 결과 |
|---|---|
| 프리팹 외형 편집 | 폰트·색·크기·배경·Content 위치 수정이 코드 수정 없이 반영. Label 이름을 바꿔도 명시적 참조로 동작 |
| 기본/Override 풀 | 기본 외형과 Variant가 섞이지 않고 각 풀에서 재사용. 미설정 활성 참조는 재생 전 명확한 오류 |
| 가속 | 2칸/증가량 10에서 `가속 X 2`, 이동 끝 HUD 직후 한 번 |
| 깔끔한 뒷정리 | 선행 아군 공격 뒤 보유자 HUD/문구 시작, 단순 HUD 재갱신으로 중복 없음 |
| 희생 | 생존/사망에서 수호자 나가기와 동시에 문구, 이동 추적·반환·사망 회수 정상 |
| 수정 가능성 | Count를 설정한 뒤 Kind를 바꿔도 None으로 수정 가능. Cue Timing도 같은 화면에서 복구 가능 |
| 시간/투영 | 유지/페이드·Pause·카메라/Shake·해상도·화면 밖·첫 프레임 원점 노출 없음 |
| 자원 수명 | Stop/Reset/반복/Detach/실패/씬 전환 후 활성 인스턴스·호스트·렌더 구독이 남지 않음 |
| 진행/성능 | 다음 Packet/공격 진행을 막지 않음. 필요 시 Profiler로 동시 다수 문구의 할당/Canvas 비용 확인 |

### 9.8 2차 계획 수정 기록

2026-10-31: 사용자 요청에 따라 프리팹 외형 + 런타임 생성/재사용으로 설계를 변경하고 구조 검토 R1~R4를 F~J에 반영했다. **문서만 수정했으며 코드·SO·프리팹은 변경하지 않았다.** 기존 A~E 실행/검증 인계 기록과 공유 HUD/설명 팝업의 범위 제외를 보존했다. F~J 구현과 해당 검증은 아직 수행하지 않았다.


### 9.9 2차 실행 기록 — F~J

2026-10-31. 위 사용자 지시에 따른 일괄 범위다. A~E의 Play 미확인 항목은 그대로 유지하며, 이번에 재생 성공으로 바꾸지 않았다. §9.8의 “문서만 수정”은 구현 승인 전의 이력이다.

| Phase | 실제 반영 |
|---|---|
| F | `CombatTraitFeedbackProfiles`를 유일한 지원 표로 사용. `ICombatTraitStateEvidence`의 가속 보유량/공격력 부여 해석기를 Factory에 등록하고 중복·누락 등록을 거절. Plan은 불변 구성, Session은 private 실행 상태 소유. Count/Timing의 무효 값도 명시적으로 복구 가능 |
| G | `TraitBodyText.prefab`, `CombatBodyTextView`, 프리팹별 Pool/Rental, Resources, 전용 Factory 추가. 기존 폰트 참조. 텍스트 문구·가시성·페이드만 런타임 변경 |
| H | 공통 BodyTextPrefab/줄별 PrefabOverride 연결. Window의 코드 TMP 생성·외형 덮어쓰기 제거. 시간은 Tick, 몸 조회는 활성 항목의 렌더 투영에서 한 번. 활성 0→1 구독·마지막 반환/Cancel/Release 해제 |
| I | Inspector의 에셋 선택·Variant 안내. 선택된 프리팹 내부 참조·계층·폰트·입력 설정을 운영/시연 공통 검증. 시연에 연결된 프리팹/의존 에셋 변경 시 기존 안전 갱신 경로로 중단 |
| J | 최종 MSBuild 런타임/Editor 컴파일, 직렬화·참조·형식·보존 대조, R1~R4와 SSOT/SOLID5/JMH/FW 재검토. 사용법/사용자 확인 목록 갱신 |

외형 이전 조사: 저장된 BodyText는 Acceleration·CleanSweep·Sacrifice의 세 줄이며 모두 Enabled, 흰색/28/오프셋0이었다. 공통 폰트는 기존 SCDream7 SDF다. 맞춤 외형을 가진 추가 비활성 줄은 없어 기본 프리팹 하나로 이전했고 불필요한 Variant를 만들지 않았다. 세 줄의 PrefabOverride는 비워 공통 프리팹을 사용한다. 향후 다른 외형은 Variant로 추가할 수 있다.

| 최종 검증 | 결과와 범위 |
|---|---|
| 컴파일 | VS 2022 MSBuild `ProjectL.Systems`와 `Assembly-CSharp-Editor`, 모두 오류 0. 최종 로그 `Temp/BodyFeedbackV2/phase-release-runtime-build.log`, `phase-release-editor-build.log`. Unity 생성 csproj에 아직 없는 소스는 임시 CustomAfterMicrosoftCommonTargets로 포함, asmdef 경계 보존 |
| 소스 형식 | 이번 변경 C# 24개 UTF-8/LF·meta·U+FFFD 없음·SerializeReference 추가 없음. 형식 정리 전후 Roslyn 토큰 동등성 확인. `git diff --check` 통과 |
| 직렬화/참조 | 새 프리팹 local fileID·View 스크립트·TMP 폰트/머티리얼 참조 정적 대조. 실제 Unity 임포트 검증은 사용자 후속 |
| 기존 값 | 착수 시 사본과 대표 SO 3개 비교: 구 외형 필드→PrefabOverride 외 Kind/Timing/Count/문구/시간/Effects/Shake/UnitActions/이름/아이콘/설명/GUID 동일. 공통 설정도 폰트 참조→프리팹 참조 외 동일 |
| 보존 영역 | 착수 시 해시와 Core/Global(공유 HUD 코드 포함)/HUD 프리팹/폰트/프리셋 등 2,590파일 일치. 기존 다른 작업 수정 보존. Framework/FieldCamera/Architecture·새 View 테스트 수정 없음 |
| 시간/투영 | Tick에 몸 Position 호출·LINQ·Find/GetComponent·새 Dictionary 없음. 활성 렌더 투영 항목당 Position 한 번. 새 인스턴스는 숨긴 채 준비하고 투영 후 표시. 필요 없는 동일 알파/가시성 대입도 생략 |
| 수명 검토 | 생성 실패의 중첩 finally 회수, 소유 풀 반환·중복 반환 거절, Cancel/Release 반복 방어, 마지막 렌더 구독 해제 확인. 실제 반복/씬 전환 누수 여부는 Play 후속 |
| 미실행 | Unity Play/EditMode Test Runner/Profiler 실행 안 함. 가독성·프리팹 임포트·실제 인게임 Core 발동·성능 측정 성공을 주장하지 않음 |

검증용 임시 기록은 ignored `Temp/BodyFeedbackV2/`에만 두었다. 새 테스트 어셈블리·View 테스트·자동 검증 메뉴는 없다. 컴파일에는 프로젝트의 기존 경고가 있으며 변경 텍스트/피드백 코드에서 새 경고는 확인되지 않았다. 커밋·push·스테이징은 하지 않았다.

### 9.10 구조 검토와 소유권

- 생성/연결: 공용 `CombatEventPlaybackFactory` → `CombatBodyTextFactory`의 Scene 소유 Overlay 호스트/Resources/Pool → 주입된 Window → 기존 Presenter의 Player 목록. 운영/시연은 같은 경로다.
- 소유/종료: Player의 Tick/Cancel/Release가 Window를 진행·종료한다. Window는 Rental 반환과 렌더 구독을 소유하며 Resources는 풀·호스트를 회수한다. 개별 View는 특성/카메라/풀/서비스를 모른다.
- 서비스 경로: 새 IServiceModule·Group 보관·상위 Export·Builder 등록·루트 resolver 조회는 추가되지 않았다. 따라서 새 서비스 등록의 FW-001~003 전체를 테스트했다고 쓰지 않는다. 기존 Player 수명과 자체 생성 자원의 반환 경계를 검사했다. 기존 효과의 Scope/Handle 경로는 유지한다.
- UI 등록 단위: 일시 Accent 세부 위젯이며 독립 Open/Hide·UIId·부모/Policy·입력/이력이 없다. 공유 HUD/팝업 등록을 대체하지 않는다. 향후 독립 화면으로 확장하면 UISystem 등록을 다시 설계한다.
- SSOT: 지원 표는 Profiles, 표시 이름은 TraitVisual, 외형은 프리팹, 기본 참조/간격은 공통 설정, 발동/이동/상태 사실은 Result다. 계획과 N은 일시 파생값이며 Buffer/규칙 사본을 만들지 않는다.
- SOLID5: 입력·근거·계획/실행·표시/풀·조립을 분리(S). 같은 근거의 특성 추가는 프로필 등록, 새 근거는 좁은 해석기/Factory 등록(O). 기존 비차단 Player·Accent와 취소/해제 계약 유지(L). 근거 Kind/Matches와 몸 추적 델리게이트만 받음(I). 카메라·프리팹·풀·근거를 Factory에서 주입(D).
- 기능 변경 독립성: 외형 변경은 프리팹, 시간/투영은 Text, 상태 대응 추가는 프로필/근거/조립에 머문다. 관련 없는 공격/Core/HUD 코드를 읽고 수정하는 의존을 늘리지 않았다. 공통 Player/generation/취소 경계가 바뀌는 후속 작업은 영향 회귀가 여전히 필요하다.
- 캡슐화/if/Tick: 외부에서 Plan Map/Started/Shaken을 수정하지 못한다. private Session이 시작·State 소비·Shake를 소유한다. 선택 기능·만료·화면 밖·대여 소실·세대 취소의 필요한 분기는 유지하고, 지원 분기는 공용 정책으로 모았다. 시간 진행과 렌더 투영은 역할이 달라 둘 다 유지한다.

사용자는 §9.7의 수동 목록과 [갱신된 사용법](../viewDocs/trait-body-feedback-guide.md)을 따라 확인한다. 추가 수동 프리팹 연결은 필요하지 않다. 공유 HUD 설명 팝업은 후속 보류이며 기존 종료 오류도 수정하지 않았다.


### 9.11 Unity 컴파일 누락 후속 — 2026-10-31

사용자 CS0246 보고에 따라 확인했다. Pool/View 소스와 네임스페이스는 정상이었으나 Unity의 실제 `ProjectL.Systems.rsp`에서 두 파일이 빠져 있었다. MSBuild의 임시 소스 보완 빌드가 이 Unity 등록 누락까지 검증하지 못했다. 일반 Refresh·개별 Reimport로도 목록이 복구되지 않아 두 스크립트와 meta만 일시 격리→Refresh→원래 경로 복원→Refresh하여 등록 캐시를 갱신했다. 원래 GUID·프리팹 연결·동작 코드는 유지하고 두 meta에 표준 MonoImporter 구성을 보완했다.

이후 실제 Unity 컴파일 목록에 Pool/View가 포함됐으며 `Tundra build success (8.82 seconds)`와 Systems/Editor DLL 생성·도메인 리로드를 확인했다. Unity 컴파일 확인과 Play 검증을 구분하며 이번에도 Play는 실행하지 않았다. 새 테스트·자동 검증 메뉴·Core/공유 HUD 수정은 없다.


## 10. 3차 Phase K — Activation별 표시 설정

승인 범위는 사용자 색·크기·문구·Position Offset 추가 지시다. [설계 §10](../specs/2026-10-31-trait-body-feedback-design.md#10-3차--activation별-문구색크기위치)을 먼저 작성하고 같은 요청의 하나의 연결 Phase로 구현한다. 앞선 색/크기 제거는2차 이력이며 이번 요청으로 정책을 변경한다. 현재 Play·에디터 입력은 제어하지 않는다. 새 테스트·Core/타 작업 변경·커밋/push/추가 스테이징은 없다.

- [x] K1. git/동시 변경과 기존4줄·선택 프리팹·폰트/색/크기/Content 보정을 기록한다. 직전 사용자가 커밋한71390ac71 및 다른 HUD/맵 작업을 보존한다.
- [x] K2. 기존 BodyText Input/Data에 Color/Font Size/Position Offset을 추가하고 공용 finite/positive 규칙으로 검증한다. 직접 문구는 기존 TextSource/overrideText를 재사용한다. 기본 흰색/28/(0,0), 비활성 입력의 기존 선택 정책 유지.
- [x] K3. Inspector에서 Override일 때 Text 여러 줄 입력, 색·크기·오프셋을 편집하도록 표시한다. Prefab Override·Count/Timing의 수정 가능성을 유지하고 단순 OnGUI로 기존 값을 바꾸지 않는다.
- [x] K4. Window→Pool→View에 문구/색/크기를 전달하고 매 Rent에 적용한다. Auto Size는 인스턴스에서 끈다. Offset은 몸 투영+Stack Gap과 함께 절대 위치로 대입한다. 기존 Tick/구독·반환·페이드·Gate 수명 유지.
- [x] K5. 기존4개 TraitVisual 줄에 현재와 같은 흰색28/오프셋0을 명시한다. 다른 값·GUID·프리팹·폰트·기존/신규 시연 프리셋은 보존한다.
- [x] K6. 런타임/Editor 코드 컴파일과 입력→캐시→Rent→투영, UTF-8/LF/meta/참조·값 보존을 정적으로 확인한다. 새 View 테스트·실제 Play/Profiler/에디터 Refresh는 수행하지 않고 미검증을 명시한다.
- [x] K7. 사용법과 SSOT/SOLID/JMH/FW·캡슐화/독립성 검토 결과를 갱신하고 사용자에게 확인 목록과 Phase 결과를 보고한다.

리뷰 지점: 매 Rent 스타일 초기화·루트 offset 누적 없음·Prefab Override 유지·직접 문구/이동 N·기존 피드백/공격 수명 보존. Phase 완료 후 멈추고 보고한다.


### 10.1 Phase K 실행 기록 — 2026-10-31

사용자 지시에 따라 §10 설계/Task 작성 후 하나의 연결 Phase로 구현했다. 이전 사용자 커밋71390ac71과 별도 HUD 줌/맵 타일·카메라/프리팹/폰트 변경을 보존했다. 현재 Play/에디터 UI를 제어하지 않았으며 커밋·push·추가 스테이징은 하지 않았다.

- 입력/Editor: Color(흰색), Font Size(28), Position Offset(Vector2.zero) 추가. 유한 색/오프셋·양수 크기를 공용 규칙으로 검증한다. Text Source/overrideText의 기존 필드·enum을 유지하고 Override에서 Text 여러 줄 입력을 노출했다. Prefab Override 및 Count/Timing 편집 경로는 유지한다.
- 전달/표시: 불변 Data→기존 캐시→Window→Pool.Rent→View.Prepare. 매 Rent에 문구·색·Font Size와 Auto Size=false를 적용한다. 프리팹 버킷은 스타일별로 나누지 않는다. Font/Material/Content 구조를 변경하지 않는다.
- 위치/수명: 몸의 Canvas 좌표+발동별 Position Offset+Stack Gap×슬롯을 절대 대입한다. 기존 렌더 구독·숨김/추적·Tick 시간/페이드·Cancel/Release·공격 Gate/StateApplied 근거는 유지한다. 새 Update/버퍼 폴링·매 프레임 자원 조회는 없다.
- 에셋: 활성4줄(가속·깔끔한 뒷정리·표식·희생)에 흰색28/offset0을 명시했다. 새3필드 외 모든 에셋 텍스트와 meta/GUID가 직전 사본과 일치한다. 기존 정책·활성 시점·문구·Effect/Shake/UnitActions·프리셋/Prefab Override를 보존했다.

| 검증 | 결과 |
|---|---|
| 코드 컴파일 | Unity 제공 Roslyn+현재 Bee 응답 파일로 런타임·Editor·비Editor 정의 종료0. `Temp/StatusEffectView/k-{runtime,editor,player}-csc.log` |
| 정적 대조 | 소스6개 UTF-8/LF/meta·새 SerializeReference 없음. 신규 소스/meta 없음. 에셋4줄의 기존 값/GUID 보존, 입력→표시→풀/투영 연결 확인. 해당 소스/에셋/문서의 diff --check 통과 |
| 보호 영역 | 착수 기준 Core/Framework/FieldCamera/Architecture·몸체 프리팹·프리셋2,147파일 동일. `Temp/BodyFeedbackV3/verification.json` |
| 구조 | 설계 §10.4의 SSOT/SOLID5/JMH/FW·독립성 적용. 표시 컴포넌트는 DB/특성/카메라에 의존하지 않고 문구/스타일 값만 받음. 새 서비스/Export/UIId/Builder/전역 Tick 없음 |
| 미실행 | 실제 Unity 에디터 임포트·재컴파일·기존 DataBaseAssetTests·Play·Profiler는 사용자 후속. 코드 컴파일/정적 성공과 구분하며 현재 Play 종료/Refresh하지 않음 |

사용자는 [몸체 텍스트 사용법 §2/§7](../viewDocs/trait-body-feedback-guide.md#7-3차-설정-확인-목록)에 따라 Override 문구, 색/크기, offset 방향·해상도 스케일, 같은 풀의 교차 스타일, 중단/반복과 기존 가속/희생 시점을 확인한다. K 완료 후 보고·정지하며 추가 외형/행동 요구는 다음 범위다.
