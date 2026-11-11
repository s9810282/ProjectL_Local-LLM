# 튜토리얼 실습 점멸·진영별 색 분리 Implementation Plan

> **For agentic workers:** `superpowers:executing-plans`로 승인된 Phase만 순서대로 수행한다. 체크박스는 실제 증거가 있을 때 갱신한다. 별도 구현 위임은 요청하지 않았다. 프로젝트의 시험·컴파일·커밋 금지와 Phase 종료 규칙이 스킬의 일반 절차보다 우선한다.

**Goal:** 기존 튜토리얼 영역의 흰 점선을 유지하면서 실제 카드·버튼 가장자리에 별도 노란 점선을 추가한다. 설명에서는 고정, 실습에서는 필요한 대상의 노란 점선만 점멸하며, 설명의 모델에는 아군/적별 설정 색을 적용한다.

**Architecture:** 기존 TutorialUI 스타일·FocusLocator/Frame·모델 대여 경로를 확장한다. 실습 대상은 정의 데이터, 표시 가능 시점은 기존 전투 입력 사실의 작은 읽기 값, 단계 수명은 Tutorial Snapshot을 사용한다. 점멸은 UI 출력만 바꾸며 진행·입력 허용·모델 합성 정책을 소유하지 않는다.

**Tech Stack:** Unity 6000.0.73f1, C#, uGUI, 기존 JMH Framework·Tutorial Buffer·CombatInputPolicy·UnitModelAppearance.

**Spec:** [후속 설계](../specs/2026-11-10-tutorial-highlight-colors-practice-design-Hojin.md). 기본값·실습별 대상·사용자 결정의 원본이다. 기존 17단계 진행은 [이전 계획 §9.6](2026-11-10-tutorial-current-rules-refresh-plan-Hojin.md)을 인수한다.

- 작성: 2026-11-10, 갱신: 2026-11-11, Codex. **Phase 1 반영 뒤 사용자 직접 요청에 따라 Phase 2/3 코드 반영·정적 검수 완료. Phase 4 사용자 Unity 확인 대기.** 최초 실행은 §7, 개별 대상 테두리 정정은 §8, 실습 점멸·전체 손패 소개 제외·카드 정렬의 현재 기준은 §9다.
- 조사 기준: HEAD `cf1b17e3b`와 현재 미커밋 작업 파일. 이전 Phase D의 제품 변경과 다른 작업자의 변경을 보존한다. 아군 Cartoon/Opaque 속성은 사용자 확인, 실제 Outline Pass·컴파일·재생은 미확인이다.
- 이번 번호는 **후속 Phase 1~4**다. 이전 A~D를 다시 수행하거나 이전 E를 자동 시작하지 않는다.

## Global Constraints

- 각 Phase가 끝나면 변경·정적 확인·미확인·남은 항목을 보고하고 멈춘다. 계획 작성은 구현 승인이나 개인 작성 역할의 전역 변경이 아니다. 후속 Phase 구현은 사용자 지시 범위를 따른다.
- Test 코드·시험 도우미를 만들지 않고 MSBuild·EditMode·컴파일·Play를 실행하지 않는다. 사용자 Unity 확인을 별도로 기록하고 **컴파일 미확인**을 보고한다. 커밋·push하지 않는다.
- Core·FieldCamera·Architecture·일반 카드/버튼 프리팹·공용 셰이더·Missing Script 에셋은 수정하지 않는다. 기존 시험은 API 영향이 있을 때만 작성자/프로젝트 규칙에 따라 처리한다.
- 17단계 순서·문구·목표·입력 제한·map 1·덮개 0.95·클릭 전용 진행·마지막 강조 없음은 유지한다. 실습 성공이나 전투 규칙을 표시 코드에서 다시 계산하지 않는다.
- 색/점멸 조정값은 설계 §3을 사용한다: 기존 영역 흰색 `#FFFFFF`·두께 4, 개별 대상 노란색 `#FFCC00`·두께 5, 아군 `#39C5FF`, 적/적 넥서스 `#FF5A4F`, 모델 폭 0.04, 점선 길이 16/간격 8, 점멸 켬·1.2초·최소 알파 0.35. 크기는 11-11 사용자 요청(§10·§11)으로 확대했으며 Inspector에서 조정할 수 있다.
- 설명 UI는 고정, 실습의 노란 대상 점선만 점멸, 모델은 고정이다. 실습에서는 흰 영역 테두리/덮개/전체 클릭 차단막을 켜지 않는다. 새 Canvas·UIId·서비스 등록·입력 이벤트는 만들지 않는다.
- 기존 GUID·참조를 유지한다. 새 `.cs`에는 `.meta`, Allman/4칸/LF/UTF-8, 한국어 주석과 구성 오류의 명시적 예외 규칙을 적용한다.

## Review Focus

| 조건 | 기대 | 소유 Task·사용자 확인 |
|---|---|---|
| 실습 점멸 중 설명 복귀·빈 대상·비활성/재활성 | 점멸 알파 1 복원, 설명은 고정, 이전 점선 잔상 없음 | 2-3·3-1·4-1 |
| 적 자동 행동·진입 연출·미처리 제출·모달·종료 | 실습 점선을 숨기고 정상 입력 복귀 때만 재표시 | 2-2·2-3·4-1 |
| 밤 경계 배치 복귀 또는 기존 실습의 늦은 표시 | 현 세션/전투/단계와 배치용 대상만 사용, 통과 판정 중복 없음 | 2-1·2-3·4-1 |
| 아군/적 혼합·재바인딩·Inspector 색 교체 | 해당 모델 대여만 갱신, 다른 효과/진영·원래 재질 보존 | 1-1·4-1 |
| 창 크기·Canvas 배율 변경 중 점멸 | 위치 갱신과 점멸 분리, 입력 통과·정점 한도·설명 회피 유지 | 1-1·3-1·4-1 |

## 1. 파일과 책임

아래 약어는 파일 경로를 줄이기 위한 것이다. 각 Task는 이 표의 정확한 경로를 사용한다.

| 약어 | 경로·책임 |
|---|---|
| T | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Tutorial/` — 현재 튜토리얼 표시 부품 |
| C | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/` — 기존 전투 View/입력 정책 |
| D | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/Tutorial/Item/` — 단계 표현 데이터 |
| F | `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/InGameViewSystemFactory.cs` — 생성·연결만 |
| P | `Assets/ProjectL/Prefabs/UI/Tutorial/TutorialUI.prefab` — 스타일 값과 기존 내부 위젯 |
| A | `Assets/ProjectL/DataBaseSO/ViewGroup/TutorialDefinition/Item/TutorialDefinition_1-1.asset` — 실습 대상 목록 |
| S | `Assets/ProjectL/Editor/Tutorial/TutorialUISetup.cs` — 기존/신규 프리팹 구성 경로 확인 |

| Phase | 결과 | 선행 |
|---|---|---|
| 1 | 흰 영역 유지·개별 대상 노란 점선 추가·아군/적 색 분리 | 현재 A~D 반영 코드 |
| 2 | 실습 UI 대상·입력 대기/전환 수명 연결, 아직 고정 점선 | 1 |
| 3 | 실습에서만 점멸·알파 복원 | 2 |
| 4 | 사용자 Unity 확인·결과 기록 | 1~3 |

## 2. 후속 Phase 1 — 색상 분리

### Task 1-1: 스타일·진영 전달·모델 대여 연결

**Files:** 수정 `T/TutorialHighlightStyle.cs`, `TutorialFocusFrame.cs`, `TutorialViewFocusLocator.cs`, `TutorialModelHighlight.cs`, `TutorialFocusBorderGraphic.cs`, `TutorialViewUIDisplay.cs`, `TutorialViewUI.cs`, `P`, `S`. 추가 `T/TutorialTargetBorderGraphic.cs` + `.meta`. `Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/UISystem/Screens/Stage/BattleChangerUI.cs`에는 실제 버튼 RectTransform 읽기 속성만 추가한다.

**Interfaces:** Style에 `UiBorderColor`, `TargetBorderColor`, `TargetBorderThickness`, `AllyOutlineColor`, `EnemyOutlineColor`, `GetModelColor(Owner owner)`를 둔다. Frame은 기존 UiRects와 별도 `UiTargets`/`AddUiTarget(RectTransform)`으로 실제 대상을 전달하고 TargetBorder는 `SetTargets(IReadOnlyList<RectTransform>, TutorialHighlightStyle)`로 받는다. `BattleChangerUI.ButtonRect`는 실제 버튼만 읽는다. `TutorialModelFocus(FieldUnitView view, Owner owner)`와 `Owner` 읽기 값, `TutorialFocusFrame.AddModel(FieldUnitView view, Rect rect, Owner owner)`, `TutorialModelHighlight.Sync(IReadOnlyList<TutorialModelFocus> targets, TutorialHighlightStyle style)`로 진영을 전달한다. 기존 영역 좌표와 `Locate(step, frame)` 계약은 유지한다.

- [x] 1-1a. `color`를 `[FormerlySerializedAs("color")] uiBorderColor`로 이전하고 아군/적 색을 추가한다. 각 색의 유한 값·알파 `(0,1]`를 검증한다. `GetModelColor`는 Player/Enemy만 허용하며 잘못된 진영을 임의 공통색으로 숨기지 않는다.
- [x] 1-1b. Locator가 이미 읽는 Character/Structure 기록의 Owner를 Frame에 함께 전달한다. 대상 이름/프리팹/선택 여부로 진영을 추정하지 않는다. 사라진 대상은 기존 정상 부재 처리, 존재하지만 지원하지 않는 진영은 대상 식별 가능한 구성 오류로 구분한다.
- [x] 1-1c. 모델 Entry별 적용 색/폭을 비교해 바뀐 진영·색·폭·무효 대여만 반환/재취득한다. UI 색이나 다른 진영 색 변경으로 모든 모델 대여를 갱신하지 않는다. EntityId·Bind 버전·외형 교체 보호와 기존 UnitModelAppearance 작성/복원은 유지한다.
- [x] 1-1d. Graphic은 `UiBorderColor`, UIDisplay는 새 Sync를 사용한다. 프리팹에 설계 기본값을 직렬화하고 기존 FocusBorder/Overlay/Blocker 순서·GUID를 보존한다. Setup의 신규 생성과 기존 사용자 설정 보존도 확인한다.
- [x] 1-1e. 변경 범위의 옛 `style.Color`/직렬화 `color` 참조를 검색해 호출 누락을 확인하고 한정 `git diff --check`를 수행한다. 기존 새 단계/마지막 중앙 배치 변경을 덮어쓰지 않는다.
- [x] 1-1f. 기존 FocusBorder는 흰색·두께 2로 복원하고, TutorialUI 내부에 TargetBorder를 추가한다. 전체 손패 소개에서는 노란 점선을 제외하고, 개별 카드 설명/실습은 기존 카드 Button.targetGraphic의 실제 배경 RectTransform, 버튼은 실제 버튼을 사용해 네 모서리를 투영하고 회전을 따른다(후속 정정 §9). 노란 점선은 각 사각형 안쪽에 그린다. 새 스크립트/meta·프리팹 참조·Setup 신규/보완·정리/레이아웃 경로를 연결하며 원래 흰 영역과 설명 배치는 유지한다.

**완료 기준:** 흰 영역·개별 대상 노란 점선·아군·적 색이 별도 Inspector 항목과 소비 경로에 연결됨. **사용자 Unity:** 전체 카드 소개는 흰 영역만, 개별 카드/실제 버튼의 노란 점선은 배경과 정렬, 혼합 진영/넥서스 색, 색 변경·재바인딩·다른 효과 복원. 실습 유지·점멸은 Phase 2/3의 소유다.

## 3. 후속 Phase 2 — 실습 강조 수명

### Task 2-1: 실습의 배치/전투 UI 대상 데이터

**Files:** 수정 `D/TutorialStepData.cs`, `A`. 기존 DefinitionDataSO/Table은 검증 호출 확인 대상으로만 읽는다.

**Interfaces:** `practiceDeploymentTargets`, `practiceCombatTargets`를 직렬화된 `List<TutorialFocusTarget>`로 추가하고 `PracticeDeploymentTargets`, `PracticeCombatTargets`를 읽기 전용 목록으로 노출한다. 기존 설명의 `FocusTargets`와 실습 목표/제한은 유지한다. 신규 목록이 없는 옛 에셋은 빈 목록으로 읽는다.

- [x] 2-1a. 실습 목록은 None/미정의/중복/모델 대상을 거절하고 기존 UI 대상만 허용한다. 설명 단계에는 두 실습 목록을 비워야 한다. 실습의 기존 설명 문구/Anchor/Placement 금지와 안내 검증은 그대로다.
- [x] 2-1b. 설계 §4를 A에 연결한다: 배치 목록은 12·18·22=`Hand, EnterCombatButton`, 24=빈 목록. 전투 목록은 12·18=빈 목록, 22=`AttackButton, EndTurnButton`, 24=`EndTurnButton`. 13개 설명은 두 목록 모두 비운다.
- [x] 2-1c. 17개 키·문구·목표/제한·map/알파·GUID가 유지되고 두 목록만 추가됐는지 대조한다. 기존 `DataBaseAssetTests`는 사용자 Unity 확인 항목에 넣는다.

**완료 기준:** 국면별 실습 강조 대상의 원본이 정의 한 곳에 있고 C# 단계 번호 분기가 없음. 이 Task만으로 실습 표시를 시작하지 않는다.

### Task 2-2: 현재 입력 가능 구간의 좁은 읽기 값

**Files:** 추가 `C/Input/CombatInputWindow.cs` + `.meta`; 수정 `C/Input/CombatInputPolicy.cs`, `C/CombatViewSystem.cs`, `F`, `T/TutorialViewSystem.cs`. 기존 입력 사실 Reader·명령 제출 동작은 바꾸지 않는다.

**Interfaces:** 내부 불변 `CombatInputWindow(long battleInstanceId, StagePhase stagePhase, bool canShowGuidance)`에 같은 이름의 PascalCase get-only 값을 둔다. `CombatInputPolicy.CanShowGuidance(in CombatInputFacts facts)`와 `CombatViewSystem.ReadInputWindow()`를 추가한다. `TutorialViewSystem` 생성자는 기존 인자에 필수 `Func<CombatInputWindow> readInputWindow`를 `faults` 다음으로 받으며 조립에서 명시적으로 전달한다.

- [x] 2-2a. 표시 가능 판단은 CombatInputPolicy에 둔다. 기존 공통/닫힘/막힘 판정을 재사용하고, 실제 입력 연결·PlayerInput·맞는 Layer·미처리/대기 명령 없음·미표시 결과/후처리 없음·모달/진입 연출 없음일 때만 true다. 개별 유닛의 공격 가능 여부나 실습 성공은 판정하지 않으며 기존 CanSubmit/CanPreview의 결과를 바꾸지 않는다.
- [x] 2-2b. `ReadInputWindow()`는 연결 전/해제 뒤 정상적으로 닫힌 값을 반환하고, 연결 중에는 기존 `ReadFacts(includeUnhandledCommand: true)`를 한 번 읽어 전투 번호·StagePhase·위 정책 결과만 돌려준다. 읽기 결과를 저장하거나 명령/버퍼를 쓰지 않는다. 기존 내부 사실 Reader의 가용성/캐시 계약을 우회하지 않는다.
- [x] 2-2c. F에서 `combatView.ReadInputWindow`를 TutorialViewSystem에 전달하고 PostBind에서 표시 부품까지 전달한다. 전투 View는 Tutorial 타입/단계 번호를 참조하지 않는다. 별도 서비스 등록, 전체 CombatView/BufferQuery 보관, 새 public 루트 API를 추가하지 않는다.
- [x] 2-2d. 생성자 호출처를 다시 검색한다(계획 시 생산 호출은 F 한 곳). 변경 없는 실제 제출/되돌리기/공격/종료 정책과 새 읽기 전용 경계를 구분해 정적 확인한다. 기존 시험 영향은 프로젝트 규칙을 따른다.

**완료 기준:** 새 소비자는 입력 정책을 복제하지 않고 작은 읽기 값만 받음. **사용자 Unity:** 적 행동·미처리 제출·연출·모달·포기 동안 false, 정상 입력 복귀 때 true; 기존 전투 입력 동작 유지.

### Task 2-3: 실습 표시·전환·정리 연결

**Files:** 수정 `T/TutorialViewUIDisplay.cs`, `TutorialViewFocusLocator.cs`, `TutorialViewUI.cs`, `TutorialViewSystem.cs`. `ITutorialViewDisplay`의 호출 형태는 유지하고 필요하면 설명 주석만 갱신한다.

**Interfaces:** UIDisplay 생성자에 기존 Character/Combat 읽기 계약 뒤 `ITutorialStateBufferReadSink tutorial`, `Func<CombatInputWindow> readInputWindow`를 받는다. Locator에 `LocatePractice(TutorialStepData step, StagePhase stagePhase, TutorialFocusFrame output)`, UI에 `RefreshPracticeHighlights(TutorialFocusFrame frame)`와 `IsPracticeShown`을 추가한다. UI 위젯은 StagePhase나 실습 목표를 직접 판정하지 않는다.

- [x] 2-3a. UIDisplay에 현재 설명 또는 실습 자료와 실습 표시 요청 당시 세션/전투/정의/단계/Revision 식별자를 보관한다. Present/Transition/Close/Detach에서 교체·해제하며 지연 표시가 새 세션에 남지 않게 한다. Present 완료는 기존처럼 한 번 알리고 표시 갱신에서 다시 알리지 않는다.
- [x] 2-3b. RefreshLayout에서 실습일 때 현재 Snapshot이 같은 식별자의 Running/Practice/PracticeGoal인지와 2-2의 전투 번호/CanShowGuidance를 확인한다. 조건 불충족은 점선만 정리하고 안내/진행/입력을 변경하지 않는다. 활성 세션 참조가 깨진 구성 오류는 정상 대기와 구분한다.
- [x] 2-3c. LocatePractice는 먼저 Frame을 비우고 현재 국면의 목록을 기존 UI 위치 해석으로 처리한다. 모델/앵커는 만들지 않고 비활성/투명/없는 UI는 기존 기준대로 제외한다. Hand는 개별 카드의 UiTargets를 전달한다.
- [x] 2-3d. UI는 실습의 흰 FocusBorder·덮개·Blocker를 끈 채 TargetBorder의 노란 대상 점선과 안내만 표시한다. RefreshPracticeHighlights는 UiTargets를 TargetBorder에 전달한다. 실습 시작 때 이전 설명의 모델 대여는 반환한다. 적 응답/배치 복귀/재시도에서 대상 점선만 숨김·재표시하고 단계 전환/실습 완료/UI 숨김/오류에는 모두 정리한다. raycastTarget=false를 유지한다.
- [x] 2-3e. 설명의 기존 Frame/모델/회피 배치와 25번 빈 강조를 유지하는지 확인한다. 적 응답 중 숨김, 배치용 목록 전환, 늦은 실습 Snapshot, 완료 뒤 잔상 사례를 사용자 확인표에 연결한다.

**완료 기준:** 설명과 실습 모두 필요한 UI에 고정 점선이 표시되는 코드 경로가 완성됨. **사용자 Unity:** 실습 12/18/22/24의 대상·정상 클릭/드래그·밤 배치 회복·종료 정리. 점멸은 다음 Phase다.

## 4. 후속 Phase 3 — 실습 점멸

### Task 3-1: 점선 전용 알파 애니메이션과 복원

**Files:** 수정 `T/TutorialHighlightStyle.cs`, `TutorialTargetBorderGraphic.cs`, `TutorialViewUI.cs`, `P`. 흰 영역의 TutorialFocusBorderGraphic에는 점멸을 넣지 않는다. S는 새 스타일 필드가 신규 생성에서도 기본값을 받는지 확인한다.

**Interfaces:** Style에 `BlinkEnabled`, `BlinkPeriod`, `BlinkMinAlpha`. TargetBorder에 `SetBlink(bool enabled, TutorialHighlightStyle style, float now)`, `TickBlink(float now)`를 둔다. UI만 현재 실습/설명 표시 종류를 알고 이 API를 호출한다.

- [x] 3-1a. 점멸 켬·주기 1.2·최소 알파 0.35를 직렬화한다. 주기는 유한한 양수, 최소 알파는 유한한 `(0,1]`로 검증한다. false/최소 알파 1은 고정 표시다. 색의 원래 알파와 곱하는 별도 표시 배율로 취급한다.
- [x] 3-1b. 점멸은 밝음에서 시작해 `min + (1-min)*(1+cos(2π*t/period))/2`로 변하는 한 주기를 쓴다. CanvasRenderer의 점선 전용 알파만 갱신하고 메시 재생성/모델 Sync/Coroutine·Tween 대여를 매 프레임 만들지 않는다. 시간은 UI의 기존 Update에서 `Time.unscaledTime`을 전달한다.
- [x] 3-1c. 실습의 노란 대상 점선이 실제 표시되는 동안만 켠다. 반복 Refresh/SetTargets로 주기를 매 프레임 초기화하지 않는다. 표시 시작/재표시·점멸 설정 변경 때만 시작 기준을 갱신한다. UI 색 변경만으로 모델 대여나 점멸 주기를 초기화하지 않는다.
- [x] 3-1d. 설명 전환·BlinkEnabled 꺼짐·빈 대상·Clear·OnDisable·Close/Detach에서 알파 배율 1로 복원한다. 기존 Clear의 빈 목록 조기 반환이 복원을 건너뛰지 않게 한다. 설명은 항상 고정 색이고 25번은 메시가 없다.
- [x] 3-1e. 정적 확인은 실제 실습 호출 경로·갱신/초기화 분리·메시 정점 한도·복원 분기를 대조한다. 사용자 확인에는 주기/켜기 변경, 창 크기 변경, 설명 복귀, 입력 대기 숨김·재표시, UI 재활성화를 포함한다.

**완료 기준:** 실습에서만 점멸하고 설명/모델은 고정. 사용자 Unity 인수 전에는 화면 품질·성능 통과를 주장하지 않는다.

## 5. 후속 Phase 4 — 사용자 확인·인계

### Task 4-1: 통합 확인 기록과 조정 위치 안내

**Files:** 이 계획과 후속 설계의 상태/실행 기록. 표현 값 조정은 사용자가 확인한 P 값만 기록한다. 결함으로 코드 수정이 필요하면 소유 Phase와 범위를 먼저 식별한다.

- [ ] 4-1a. 사용자 Unity 컴파일 결과와 DB 에셋 변경 후 기존 DataBaseAssetTests 결과를 기록한다. 미실행/실패를 통과로 대체하거나 다른 작성자의 시험 기대값을 임의 변경하지 않는다.
- [ ] 4-1b. 일반 맵 1/개발 직행에서 배치→이동→공격→턴 종료→마지막 클릭 흐름, 적 행동/연출/밤 배치/승패·포기·씬 전환을 확인한다. 17단계 진행·0 피해 적중 의미·수동 진행은 이전 계약 그대로다.
- [ ] 4-1c. 색 네 항목(UI Border/Target Border/Ally/Enemy)·점멸 설정을 각각 바꿔 분리 여부를 확인한다. 흰 영역 유지와 기울어진 카드별/실제 버튼의 노란 테두리를 확인한다. 혼합 진영/넥서스·모델 교체·다른 효과 복원과 원래 외곽선을 확인한다. 실제 Pass가 미지원이면 원인을 기록하고 임의 강제 전환하지 않는다.
- [ ] 4-1d. 1280×720/1920×1080/2560×1440·창 크기/포커스 변경·UI 비활성/복귀에서 정렬·입력·설명 고정·실습 점멸·잔상을 확인한다. 모양 불변의 점멸만으로 메시나 모델 대여가 매 프레임 다시 생성되지 않는지도 확인한다.
- [ ] 4-1e. Inspector 경로 `TutorialUI 루트 → TutorialViewUI → Highlight Style`와 새 필드 의미, 수행 증거/미확인/남은 결함을 정리한다. 이전 C/D/E 미확인을 일괄 완료로 바꾸지 말고 실제 겹치는 확인 근거만 연결한다.

**완료 기준:** 사용자 확인 근거 또는 남은 제한이 각각 기록됨. 사용자가 재생하지 않았다면 Phase 4는 확인 대기다.

## 6. 구조 검수·실행 기록

- **SSOT:** 목표/입력 정책은 기존 원본, 대상은 정의, 위치는 실제 RectTransform, 색/점멸 값은 Style, 진영은 공개 기록, 모델 작성은 Appearance. 입력 가능 사실을 읽는 새 값은 캐시/상태 소유자가 아닌 즉시 투영이다.
- **SOLID 5:** S=색/대상/위치/대여/정리의 책임 구분; O=기존 표시 경로 확장; L=유효성·입력·원래 효과/반환 보장 유지; I=작은 CombatInputWindow와 표시 계약; D=공개 Buffer/현재 입력 사실에 의존하고 Core 계산/가변 Store 우회 없음.
- **JMH/FW:** F는 생성·delegate 연결만, TutorialViewSystem PostBind는 기존 Buffer 읽기 접점 전달만 담당한다. 기존 등록→Group Export→UIId.Tutorial 씬 바인딩/UISystem 개폐를 유지하며 하위 서비스 루트 재등록·공용 입력 구독·새 Builder/등록을 하지 않는다.
- **변경 독립성:** 색 조정은 P/Style, 점멸 모양은 TargetBorder, 대상 변경은 A, 실습 표시 수명은 UIDisplay/UI에 한정한다. 영역 Graphic과 대상 Graphic을 분리하고 BattleChangerUI에는 좁은 버튼 영역 읽기만 둔다. 입력 읽기 경계 변경만 C/F/T 접점을 대조한다. 점멸 변경에 공격 계산·전체 Core·다른 UI 프리팹 검증을 요구하지 않는다.
- 각 구현 Phase에는 변경 파일 한정 `git diff --check`·GUID/인코딩·호출 참조 대조와 별도 정적 검수를 기록한다. 새 Test/실행 검증 대신 사용자 확인 항목을 명시하며, 오류/미확인도 그대로 남긴다.

| Phase | 상태 | 증거 | 남은 확인 |
|---|---|---|---|
| 1 | 개별 대상 테두리 정정 포함 코드·프리팹 반영·정적 검수 완료 | §7 이력·§8 현재 | 실제 카드 정렬·색/재질/컴파일·재생 |
| 2 | 코드 반영·정적 검수 완료 | §9 | 실습/입력 대기/배치 복귀·컴파일/재생 |
| 3 | 코드 반영·정적 검수 완료 | §9 | 실습 전용 점멸/알파 복원·컴파일/재생 |
| 4 | 미착수 | 사용자 확인 전 | 통합 인수·설정 안내 |

**계획 작성 당시 자체 검수:** 설계 §1~5를 1-1/2-1~3/3-1/4-1에 대응시켰고, 다섯 Review Focus를 소유 Task와 사용자 확인에 연결했다. 계획 작성은 문서만 변경했고 첫 실행 대상으로 후속 Phase 1을 정했다. 이후 실행 상태는 아래 기록을 따른다.

## 7. 후속 Phase 1 실행 기록 — 2026-11-10

> 아래는 최초 실행 당시의 이력이다. 기존 영역을 노란색으로 변경한 해석은 사용자 카드 이미지 정정으로 잘못임을 확인했다. 현재 UI 값·위젯·확인 항목은 **§8**을 따른다. 모델 진영 색 분리 기록은 유지한다.

### 실행 근거·범위

- 사용자 “Phase 단계마다 진행” 지시에 따라 Phase 1부터 수행하고 종료 보고 지점에서 멈춘다. 현재 checkout `C:/Users/Hyeon2-400/Project_L`, 브랜치 `feature/Hojin/11_03_StageSelect`, 시작 HEAD `cf1b17e3b`를 유지했다. 같은 튜토리얼의 미커밋 D 구현을 이어 쓰며 별도 checkout으로 복제/이동하거나 기존 작업을 정리하지 않았다.
- 직접 구현 지시는 이번 승인 범위에 적용하며 개인 역할 규칙의 전역 변경이 아니다. 제품 파일은 Task 1-1의 C# 6개와 TutorialUI 프리팹 1개다. 기존 D의 정의·StepData·Layout과 다른 작업자의 FMOD/UI/렌더 설정은 수정 대상이 아니다.
- Test 코드·시험 도우미·MSBuild·EditMode·컴파일·Play는 작성/실행하지 않았다. **컴파일 미확인, Unity 재생 미확인, 실제 Outline Pass 출력 미확인.** 커밋·push하지 않았다. Phase 2 실습 강조 유지와 Phase 3 점멸은 아직 구현하지 않았다.

### 반영 내용

- 설정 위치: `TutorialUI.prefab`의 **TutorialUI 객체 → TutorialViewUI → Highlight Style**. `UI Border Color`=`#FFCC00`, `Ally Outline Color`=`#39C5FF`, `Enemy Outline Color`=`#FF5A4F`, 점선 두께 3이다. 점선 길이 10·간격 6·모델 폭 0.015는 유지했다. 원래 `color`는 FormerlySerializedAs로 UI 색에 이전하며 새 세 항목은 각각 유효성 검사를 받는다.
- Locator는 EntityId로 기존 Character 읽기 계약을 조회하고, 구조물은 Combat의 Structure 기록에서 실제 Owner를 읽는다. 모델/기록이 없으면 정상 부재로 처리하고, 실제 대상의 지원하지 않는 Owner는 모델 이름·ID를 포함한 오류다. Frame에 Owner를 담으며 같은 개체라도 진영이 바뀌면 이전 대여와 구별한다.
- ModelHighlight의 전역 단일 색/폭 캐시를 Entry별 적용 색/폭으로 바꿨다. 바뀐 진영/색/폭, 사라진 대상, 무효화된 대여만 반환·재취득한다. UI 색 변경은 모델 대여를 바꾸지 않으며 적 색 변경은 유지 중인 아군 대여에 영향을 주지 않는다. 기존 부분 실패 정리·Bind 버전/외형 참조·원래 재질/효과 복원 계약을 사용한다.
- Graphic은 UI 색만, 모델은 GetModelColor의 Player/Enemy 색을 사용한다. 색을 지정하는 코드가 새로운 ColorHandle/MaterialPropertyBlock 작성 경로를 만들지 않는다. UnitModelAppearance·FieldUnitView·공용 셰이더는 변경하지 않았다.
- Setup은 `TutorialViewUI`를 새로 추가할 때 Style의 기본값을 사용하며, 기존 프리팹 보완은 FocusBorder 참조만 채운다. 옛 스타일 필드명 쓰기나 강제 기본값 덮어쓰기가 없어 수정하지 않았다. Unity의 실제 역직렬화/Inspector 이전 결과는 사용자 확인 대상이다.

### 정적 확인·구조 검수

- 호출 참조 검색: `AddModel`·`TutorialModelFocus` 생성·`ModelHighlight.Sync`의 생산 호출은 모두 새 서명으로 연결됐다. 범위 내 옛 `style.Color`/`highlightStyle.color` 사용은 없고 직렬화 이전 특성만 원래 필드명을 보존한다. 해당 타입을 사용하는 기존 시험 참조는 검색에서 발견하지 못했다.
- SSOT는 Style/Owner 기록/Appearance로 유지한다. S는 색 설정·대상 해석·대여·출력으로 분리, O는 기존 스타일/Frame 확장, L은 기존 효과/반환 계약 보존, I는 Frame의 Owner와 현재 스타일만 전달, D는 공개 읽기 계약/기존 외형 대여에 의존한다.
- JMH/FW 등록 경로는 그대로다. Factory 생성·기존 그룹 등록/Export·TutorialUI의 UIId/바인딩/UISystem 개폐에 새 등록이나 우회가 없다. 변경 독립성은 색 분리의 여섯 표시 파일과 프리팹에 한정하며, 실습 입력/성공 판정·일반 전투 로직을 재구성하지 않았다.
- 별도 실행 문맥의 제품 파일 7개 검수: **Critical 0 / Important 0 / Minor 0**. 유보한 후속 Phase 2/3과 이전 A~D 내부 전체/다른 작업자 변경은 이번 범위에 포함하지 않는다. 실제 Pass·직렬화 이전·혼합 모델 출력·Inspector 조정·효과 복원은 아래 사용자 확인으로 남긴다. 작성 중인 문서는 외부 검수 대상이 아니며 작성자가 실제 diff와 대조한다.
- 최종 자체 확인: 프리팹은 `highlightStyle` 블록만 달라졌고 세 색을 RGB로 환산해 `#FFCC00/#39C5FF/#FF5A4F`·알파 1과 일치함을 확인했다. 프리팹 meta/GUID와 다른 계층/참조는 그대로다. 시작 시 기록한 기존 변경 9개(D 코드·에셋·문서 포함)의 SHA-256도 모두 유지됐다. 변경 파일 9개(C# 6·프리팹 1·문서 2)는 LF/UTF-8이며 한정 `git diff --check`로 추가 변경의 공백을 확인한다. Unity YAML의 기존 빈 필드 뒤 공백은 새 오류로 취급하거나 일괄 정리하지 않았다.

### 사용자 Unity 확인

1. 컴파일 후 Highlight Style의 세 색과 모델 폭을 확인한다. 기존 직렬화 값/씬 Override가 있으면 UI 색으로 정상 이전되는지 확인한다.
2. 설명의 카드·버튼·체력 점선은 노란색, 아군/메인은 청록색, 적/적 넥서스는 붉은색인지 확인한다. 여러 진영이 함께 보이는 상황도 확인한다.
3. UI 색만 변경했을 때 모델 색은 유지되고, 한 진영 색을 바꿨을 때 다른 진영은 유지되는지 확인한다. 실제 모델 외곽선 출력과 원래 효과 복원도 확인한다.
4. 설명 전환·마지막 빈 강조·실습 진입·종료·풀 반환/재바인딩 뒤 잔상이 없는지 확인한다. 이 Phase에서는 실습에 들어가면 기존처럼 설명용 강조가 해제되고 점멸은 없다.

## 8. 후속 Phase 1 대상 테두리 정정 — 2026-11-10

> 당시 이력이다. 손패 소개에서 카드별 노란 테두리를 함께 표시한 동작은 후속 사용자 정정으로 제외했다. 실제 카드 배경 좌표와 실습 점멸의 현재 기준은 §9를 따른다.

### 사용자 정정과 반영

- 사용자는 기존 노란색 적용 부분은 흰색이 맞고, 첨부한 카드 자체의 테두리에 노란 점선을 넣으려는 것이라고 정정했다. 기존 손패 집합 영역의 색만 바꾼 §7의 해석을 바로잡는다. 이 정정은 Phase 1에 한정하며 Phase 2/3을 함께 구현하지 않는다.
- 기존 `FocusBorder`의 UI Border Color를 흰색 `#FFFFFF`, Border Thickness를 2로 복원했다. 새 Target Border Color는 `#FFCC00`, Target Border Thickness는 3이다. 기존 아군/적 색·모델 폭·점선 길이/간격은 유지했다.
- Frame/Locator는 기존 UiRects와 별도로 카드별/실제 버튼별 RectTransform을 UiTargets에 모은다. 손패의 흰 집합 영역·덮개 구멍·설명 회피 좌표는 유지한다. MainCharacterCard는 그 카드만 대상이다. 장애물 위치 조회는 노란 강조 대상으로 추가하지 않는다.
- 새 `TutorialTargetBorderGraphic`은 실제 대상의 네 모서리를 투영해 기울기와 크기를 따르는 노란 점선을 그린다. 사각형 안쪽으로 두께를 주고 좌표/스타일이 바뀔 때 메시를 갱신한다. Canvas 재생성에서도 모서리를 다시 읽으며 정점 수 제한을 확인한다. 스프라이트 알파 윤곽 추적은 하지 않는다.
- `TutorialViewUI`는 기존 흰 FocusBorder 다음·Explanation 이전에 새 TargetBorder를 둔다. 설명 갱신·정리·필수 참조/전체 화면 영역/형제 순서 검증을 연결했다. 새 위젯은 입력을 가로채지 않는다. Setup은 신규 생성과 기존 프리팹 누락 위젯 보완을 모두 처리하며 기존 스타일을 덮어쓰지 않는다.
- `BattleChangerUI.ButtonRect`는 실제 전투 진입 버튼의 영역만 공개한다. 기존 부모 영역은 흰 강조/설명 좌표에 유지하고 노란 테두리는 이 버튼을 사용한다. 클릭 이벤트·입력 정책·일반 카드/버튼 프리팹은 바꾸지 않았다.
- 변경 파일은 Task 1-1f에 대응한다: Style/Frame/Locator/UI·Setup·BattleChangerUI·TutorialUI 프리팹 수정, TargetBorder C#과 meta 추가, 이 계획/설계 정정. 원래 Phase D 코드·정의·문서 및 다른 작업자의 변경은 보존한다.

### 정적 검수와 경계

- 별도 읽기 전용 검수에서 **P0~P2 지적 없음**. 흰 영역/노란 개별 대상 분리, 회전 투영·안쪽 법선·정점 한도, 장애물 제외, UI 정리/참조 검증, Setup 보완 순서, 프리팹 참조와 GUID를 확인했다. 실제 Unity 화면·컴파일·직렬화 실행과 Phase 2/3·기존 모델 내부는 검수 범위 밖이다.
- SSOT는 TutorialUI 스타일·실제 RectTransform·기존 Owner 기록/Appearance에 둔다. SOLID는 S=대상 검색/위치/그리기/조립 분리, O=독립 대상 Graphic 추가, L=수명/원래 강조 계약 보존, I=ButtonRect와 UiTargets의 좁은 읽기, D=기존 UI/Buffer 경계 의존으로 점검했다. JMH/FW는 기존 등록된 UI 내부 위젯을 사용하며 새 서비스·UIId·입력 구독 없이 기존 개폐/정리 수명을 따른다.
- 기능 독립성: 카드/버튼 프리팹·전투 입력/성공 판정·공용 셰이더 수정 없이 대상 테두리만 추가했다. 후속 점멸은 TargetBorder에만 들어가며 흰 FocusBorder와 모델 색에 의존하지 않는다. 설계와 Phase 2/3 계획도 이 경계로 정정했다.
- 최종 정적 확인: Phase 1 소유 파일 14개의 UTF-8/LF·BOM/치환 문자 없음과 소스/문서 공백 검사를 통과했다. 한정 git diff --check, 새 스크립트 GUID 유일성, 프리팹 새 객체 ID·참조·형제 순서·색/폭 값도 확인했다. 기존 변경 9개의 SHA-256은 모두 유지됐다. Unity YAML의 기존 빈 필드 공백은 일괄 정리하지 않았다.
- **컴파일 미확인, Unity 재생·실제 카드 정렬 미확인, 실제 Outline Pass 출력 미확인.** Test 코드/도우미·MSBuild·EditMode·컴파일·Play를 작성/실행하지 않았고 커밋·push하지 않았다.

### 사용자 Unity 확인과 다음 경계

1. TutorialUI 프리팹의 루트 → TutorialViewUI → Highlight Style에서 UI Border Color=흰색, Target Border Color=노란색과 각각의 폭을 확인한다. Ally/Enemy Outline Color는 모델 진영별 설정이다.
2. 손패 설명에서 기존 흰 영역을 유지하면서 각 기울어진 카드의 실제 가장자리에 노란 점선이 나오는지 확인한다. 메인 카드·전투 진입·공격·턴 종료·체력 대상도 해당 RectTransform과 맞는지 확인한다.
3. 해상도/카드 배치 변화, 설명 전환·마지막 빈 강조·UI 숨김에서 정렬/정리와 기존 설명 배치를 확인한다. 실제 카드 스프라이트의 투명 여백 때문에 가장자리가 다르면 그 RectTransform 경계와 함께 확인한다.
4. 지금은 설명에서만 고정 표시한다. 실습 진입에서는 두 테두리를 정리한다. 실습 유지(Phase 2)·실습에서만 노란 점멸(Phase 3)은 다음 구현 범위로 남기고 여기서 보고한다.

## 9. 실제 실습 점멸·손패 소개 제외·카드 기준 좌표 정정 — 2026-11-11

- 실행 근거: 사용자가 턴 종료 실습의 실제 버튼에 노란 점멸 점선을 요청하고, 전체 카드 소개의 노란 점선 제거와 아래로 어긋난 카드 테두리 수정을 명시했다. 이번 수정에 필요한 기존 Phase 2/3 표시 경로를 함께 연결한다. 사용자 Unity 통합 인수(Phase 4)는 자동 수행하지 않는다.
- 원인: Practice 표시는 안내만 열고 강조를 갱신하지 않았다. 카드 강조는 손패 컨테이너(400×550) 모서리를 사용했지만 실제 Background는 289×459, 로컬 Y=25다. 기존 카드 Button.targetGraphic이 이 Background를 이미 참조하므로 그 표시 사각형을 재사용한다. 공유 카드 프리팹이나 새 좌표 보정값은 추가하지 않는다.
- 수정 기준: 설명의 Hand는 기존 흰 영역만 표시하고, 개별 메인 카드 설명과 실습 Hand는 실제 카드 배경을 노란 점선으로 표시한다. 턴 종료 실습 24는 실제 EndTurnButton을 점멸한다. 나머지 실습 대상은 승인된 §3의 배치/전투 목록을 유지한다.
- 사전 대조: 2-1 목록 → 2-3 Locator, 2-2 입력 창/Factory → 2-3 UIDisplay, 2-3 표시 수명 → 3-1 TargetBorder 알파 복원을 연결한다. 기존 API 호출은 생산 Factory/UIDisplay 각 한 곳이며 시험 호출은 검색되지 않았다. 목표/입력 계산·수동 진행·모델 합성은 바꾸지 않는다.
- 검증 제한: 새 시험 코드/도우미·MSBuild·EditMode·컴파일·Play·커밋·push 없이 정적 대조한다. **컴파일 미확인**. 실행 결과와 사용자 확인 항목은 아래에 기록한다.

### 반영 결과

- Phase 2: 단계 정의에 배치/전투 실습 UI 목록과 검증을 추가하고 17단계 에셋에 직렬화했다. 기존 17단계 내용에서 새 목록 34줄을 제외하면 작업 시작 내용과 동일하다. 24번의 전투 대상은 EndTurnButton 하나다.
- CombatInputWindow는 전투 번호·국면·표시 가능 여부만 전달한다. CombatInputPolicy는 기존 공통/막힘 정책과 현재 입력 사실을 재사용하며 새 명령 허가를 만들지 않는다. Factory → TutorialViewSystem → UIDisplay로 읽기 delegate를 전달한다.
- UIDisplay는 실습 표시 때의 세션/화면 연결/전투/정의/단계/Revision을 보관하고 현재 Running·Practice·PracticeGoal 및 입력 가능 구간과 대조한다. 불일치/적 응답/명령 대기/모달/연출/종료에는 강조만 정리한다. 배치로 복귀하면 정의의 배치 목록을 읽는다. 표시 완료 콜백과 성공 판정은 갱신하지 않는다.
- 전체 손패 설명은 UiTargets에 카드를 넣지 않는다. 메인 카드 설명과 배치 실습의 카드 강조는 기존 Button.targetGraphic의 실제 배경 모서리를 사용한다. 실제 배경 참조는 UnitCard.prefab에서 확인했고 공유 카드 프리팹은 수정하지 않았다.
- Phase 3: TargetBorder만 CanvasRenderer 알파를 1↔0.35, 1.2초 주기로 갱신한다. Time.unscaledTime을 사용하며 점멸만으로 메시/모델 대여를 재생성하지 않는다. 반복 갱신은 시작 시간을 유지하고 설명·빈 대상·숨김·종료·설정 해제에는 배율을 1로 복원한다. UI Border/모델 색은 고정이다.
- 조정 위치는 TutorialUI.prefab 루트 → TutorialViewUI → Highlight Style의 Target Border Color/Thickness, Blink Enabled/Period/Min Alpha다. 흰 영역은 기존 UI Border Color/Border Thickness다.

### 정적 검수·한계

- 별도 읽기 전용 검수: **Critical 0 / Important 0 / Minor 0**. 턴 종료 실습 연결, 전체 손패 소개 제외, 실제 카드 배경 참조, 입력 창·세션 수명, 반복 점멸/알파 복원, 에셋 값·새 meta·호출 연결을 확인했다.
- SSOT: 대상은 단계 정의, 카드 배경은 기존 Button.targetGraphic, 색/점멸은 스타일, 입력 가용성은 CombatInputPolicy, 실습 상태는 Tutorial Buffer다. SOLID는 S=정의/사실/대상 검색/표시 분리, O=기존 표시·읽기 경계 확장, L=수명·진행·입력 계약 유지, I=작은 CombatInputWindow, D=공개 읽기 계약과 delegate 연결로 점검했다.
- JMH/FW: Factory에서 생성/읽기 delegate만 조립하고 기존 Tutorial 모듈 등록·Group Export·UIId.Tutorial 바인딩·UISystem 개폐 경로를 유지한다. 하위 서비스 루트 재등록·별도 Canvas/UIId/입력 구독은 없다. 실습 점멸은 모델 합성이나 Core 성공 판정과 독립이다.
- 한정 git diff --check 통과, 신규 CombatInputWindow meta GUID 유일성과 생성자 생산 호출 두 곳을 확인했다. 기존 변경 중 이번 범위 밖 7개의 SHA-256은 모두 유지됐다. 기존 정의 에셋은 실습 목록만 추가했다. 변경 소유 파일 16개는 UTF-8/LF로 정리한다.
- 판단 유보의 처리: 실제 화면 정렬/점멸/해상도/직렬화/기존 효과 복원은 사용자 Unity 확인으로 남긴다. 작성 중 문서는 작성자가 최종 소스와 대조하고 이전 A~D·Phase 1 전체와 무관한 dirty 파일을 재인수하지 않는다. 모델 대여 내부는 이번에 수정하지 않았다.
- **컴파일 미확인, Unity 재생·실제 정렬/점멸 미확인.** Phase 2/3의 코드/정적 확인과 사용자 인수는 별개다. 이번 요청의 구현은 여기서 보고하고 Phase 4 사용자 결과를 기다린다.

### 사용자 Unity 확인

1. 컴파일 후 8번 전체 카드 소개는 흰 영역만, 9번 메인 카드 소개는 실제 카드 배경 가장자리에 고정 노란 점선이 나오는지 확인한다. 기울기·호버 상승·해상도 변경도 확인한다.
2. 24번 턴 종료 실습은 안내 띠와 실제 턴 종료 버튼의 노란 점선이 함께 보이고 부드럽게 점멸하는지 확인한다. 누른 뒤 처리/연출에서는 사라지고 다음 설명에서는 점멸하지 않아야 한다.
3. 배치 실습과 이동/공격 실습의 배치 복귀에는 카드·전투 진입 버튼, 공격 실습의 전투 중에는 공격·턴 종료 버튼을 확인한다. 클릭/드래그를 막지 않고 모달/적 응답/종료/씬 전환에 잔상이 없어야 한다.
4. Blink Enabled 끄기·주기/최소 알파 변경·빈 대상·UI 재활성화·설명 복귀에서 고정/점멸/알파 복원을 확인한다. DB 에셋 변경에 따른 기존 DataBaseAssetTests도 사용자 Unity 확인 항목이다.

## 10. 점선 확대·설명 박스 배경 교체 — 2026-11-11

- 사용자 요청에 따라 흰 점선 두께 2→4, 노란 점선 두께 3→5, 공통 점선 길이 10→16·간격 6→8로 확대했다. 프리팹 값과 신규 생성용 Style 기본값을 맞췄다. 색·실습 전용 점멸·전체 손패 소개 제외는 유지한다.
- ExplanationBox에 지정한 `Assets/ProjectL/Sprites/TextBox/Box.png`를 연결했다. 기존 Sprite의 45px 테두리를 유지하고 Image=Sliced·Pixels Per Unit Multiplier=2로 모서리를 보존한다. 기본 560×144·본문 사방 여백 32를 적용하며 긴 본문의 높이/회피 배치는 기존 경로다. 표시 테두리 22.5 Canvas 단위가 본문 여백 안에 들어간다.
- TutorialUISetup의 신규 생성도 같은 배경·크기·여백을 사용한다. 이미 있는 프리팹의 수동 조정값은 유지한다. 원본 PNG/meta·공유 카드/버튼·실습 안내/막힌 입력 안내 이미지는 수정하지 않았다.
- SSOT/SOLID·JMH/FW·변경 독립성: 스타일은 기존 TutorialUI 설정, 배경은 해당 Image, 배치 측정은 기존 TutorialViewUI가 담당한다. 새 등록/입력/수명/게임 규칙 변경 없이 프리팹 표현과 신규 생성 기본값만 조정했다.
- 정적 확인: 변경 5파일의 UTF-8/LF·한정 git diff --check를 통과했다. 프리팹은 설명 상자 Rect/Image·본문 여백·점선 크기 외 내용이 작업 시작과 동일하다. Box.png와 meta의 SHA-256도 유지됐다. 신규 생성 코드와 프리팹의 크기/여백/슬라이스 배율·스타일 기본값을 대조했다. 앞 절의 옛 두께/길이 기록보다 현재 §10 값을 우선한다.
- **컴파일 미확인, Unity 화면 미확인.** 새 Test·MSBuild·컴파일/Play·커밋/push는 수행하지 않는다. 사용자 Unity에서는 두 점선의 가독성, 설명 박스 모서리/줄바꿈/본문 여백, 긴 설명의 높이 확장과 화면 가장자리 배치를 확인한다.

## 11. 실습 배경·모델 영역 안정화·강조 보완 — 2026-11-11

- 사용자 재생 화면 4장과 요청에 따라 실습 안내에도 Box.png를 적용하고, 모델 설명의 흰 점선 누락·Idle에 따른 덮개 흔들림·체력 UI의 불필요한 노란 점선을 수정한다. 모델 아웃라인도 확대한다. 사용자 화면은 해당 현상의 근거이며 전체 Phase 4 인수 근거로 확대하지 않는다.
- 원인: 흰 FocusBorder에는 UiRects만 전달하고 모델은 Holes에만 넣었다. 모델 경계는 매 갱신 Renderer.bounds를 다시 읽어 Idle의 변형을 따라 바뀌었다. HP는 다른 조작 UI와 함께 노란 UiTargets에도 들어갔다.
- 수정 기준: 흰 점선과 설명 회피는 모든 Holes를 사용한다. HP는 흰 영역만 유지한다. 설명 중 처음 측정한 모델 경계를 개체 루트 좌표로 보관하고 이후 카메라/루트 변환만 반영한다. 실제 Animator는 변경하지 않는다. 전환/실습/숨김/종료/해제/오류의 기존 강조 정리에 경계 캐시도 포함하고 재바인딩/다른 View 인스턴스는 다시 측정한다.
- 실습 PracticeGuide는 Box.png·Sliced·PPU 배율 2, 기본 760×112·사방 여백 32로 구성한다. 긴 문장 높이는 기존 측정을 사용하며 설명 박스 560×144는 유지한다. Setup 신규 생성도 같은 구성이다.
- Model Width는 0.015→0.04로 확대하고 프리팹/Style 기본값을 맞춘다. 모델 경계의 아웃라인 여유도 현재 Model Width를 읽는다. 아군/적 색·노란 점멸·실습 성공/입력 판정은 바꾸지 않는다.
- Test 코드/도우미·MSBuild·컴파일/Play·커밋/push는 수행하지 않는다. **컴파일 미확인**. 정적 검수와 남은 Unity 확인은 아래에 기록한다.

### 정적 검수 결과와 사용자 확인

- 별도 읽기 전용 검수에서 **Critical 0 / Important 0 / Minor 0**. 모델/흰 점선의 동일 영역 사용, HP 제외, 모델 경계 캐시의 변환·재바인딩·정리 수명, 실습 프리팹/신규 생성 값·씬 override 부재를 확인했다. 컴파일/실제 화면은 확인하지 않았다.
- SSOT는 덮개/흰 점선/설명 회피가 동일한 Frame.Holes, 모델 폭은 기존 Style, 실습 이미지는 해당 Image다. SOLID는 S=경계 수명/대상 검색/그리기 분리, O=모델 측정 경계만 보완, L=기존 표시·정리·진행 계약 유지, I=TryGet/Clear의 좁은 파생값 계약, D=기존 View 읽기 경계를 사용한다. 캐시는 기존 Tutorial 표시 수명에 속하며 Factory/Group 등록·UISystem 개폐 경로에 새 서비스/등록을 추가하지 않는다. Core·FieldCamera·Animator·일반 전투 효과는 수정하지 않았다.
- 소유 파일 11개의 UTF-8/LF·소스/문서 공백과 한정 git diff --check를 확인했다. 새 meta GUID는 유일하다. 프리팹은 작업 시작 대비 실습 배경/크기/여백·Model Width만 변경됐다. 기존 범위 밖 변경 7개의 SHA-256은 유지됐다.
- 검수에서 판단을 유보한 최초 자세 영역의 Idle 전체 포괄 여부, 회전된 경계의 여유 크기, 0.04 외곽선의 실제 가독성은 Unity 확인으로 남긴다. 기존 holePadding=8과 현재 아웃라인 폭의 여유를 사용하며 실제 동작을 보지 않고 최종 시각 품질을 확정하지 않는다.
- 사용자 Unity 확인: 실습 안내의 Box.png와 긴 문장 여백, 넥서스·메인·아군 전체 강조의 흰 점선, HP의 흰 테두리만 남음, Idle 동안 덮개/흰 점선/설명 박스가 흔들리지 않음 및 모델 잘림 없음, 카메라 변화/재바인딩/단계 전환 뒤 정상 정렬, 아군/적의 굵어진 아웃라인과 원래 효과 복원을 확인한다. **컴파일 미확인·Unity 재생 미확인** 상태로 보고한다.

## 12. 겹친 강조 영역의 내부 흰 점선 제거 — 2026-11-11

- 사용자 화면에서 인접한 유닛의 사각 테두리가 겹쳐 중앙에 흰 점선이 남았다. 요청에 따라 TutorialFocusBorderGraphic에서 각 점선 사각형과 밝은 영역의 겹침 부분을 빼고 바깥 조각만 출력한다. 부분 겹침은 선 전체를 지우지 않고 겹친 부분만 자른다.
- 덮개와 같은 좌표/여백의 전체 영역을 먼저 로컬 좌표로 갱신한다. Canvas 재생성에서도 모든 대상을 현재 좌표로 맞춘 뒤 겹침을 판정한다. 임시 조각 목록을 재사용하고 분할 결과의 실제 정점 한도도 검사한다.
- 제품 변경은 흰 점선 Graphic 한 파일이다. SSOT는 기존 Frame.Holes/FocusGeometry, SOLID는 표시 기하 처리만 Graphic에 한정하며 기존 입력/수명 계약을 유지한다. JMH/FW 등록이나 서비스 경계 변경은 없다. 검은 덮개 모양·유닛별 아웃라인·노란 실습 점선·Idle 경계 고정은 바꾸지 않는다.
- **컴파일 미확인·Unity 화면 미확인.** Test/도우미·MSBuild·컴파일/Play·커밋/push는 수행하지 않는다. 사용자 Unity에서는 겹침·맞닿음·완전 포함일 때 중앙 선 제거, 떨어진 영역의 개별 테두리 유지, 해상도 변경/단계 전환 뒤 잔상 여부를 확인한다.
- 정적 확인: 제품 1파일·문서 2파일의 UTF-8/LF·공백과 한정 git diff --check를 확인했다. 별도 읽기 전용 검수에서 **Critical 0 / Important 0 / Minor 0**. 사각형 차감의 부분 겹침/맞닿음/포함, 분할 목록 역순 처리, 전체 로컬 좌표 선갱신, 실제 출력 64,996 정점 한도와 정리 수명을 대조했다. 화면 연결부 가독성·해상도·대량 영역 성능은 실행하지 않았으므로 사용자 Unity 확인으로 남긴다.

## 13. 선택 화살표를 모델 강조 경계에서 제외 — 2026-11-11

- 사용자 화면의 머리 위 선택 화살표까지 흰 점선이 길게 감싸는 현상을 수정한다. 행동 커서는 유닛 루트의 자식이며 MeshRenderer 세 개로 구성된다. 기존 TryGetModelBounds는 UI/문자/파티클만 제외하므로 활성 커서의 메시도 실제 몸체로 합산했다.
- 커서 생성자가 루트에 UnitModelBoundsExclusion을 붙이고 FieldUnitView가 해당 하위 Renderer를 측정에서 제외한다. 이름·재질·위치로 화살표를 추측하지 않는다. 최초 경계를 보관하기 전에 제외하므로 Idle 경계 고정과 함께 적용되며 밝은 영역·흰 점선·설명 회피는 같은 수정된 경계를 사용한다.
- 변경은 커서 생성 표시와 모델 경계 읽기, 상태 없는 제외 컴포넌트 및 meta에 한정한다. 커서의 선택/색/위치/표시/풀 수명·실제 모델 외형 합성·흰 점선 겹침 제거·노란 실습 점멸은 유지한다. 프리팹이나 씬에 수동으로 새 참조를 연결할 필요가 없다.
- SSOT는 커서 생성자가 정한 몸체 제외 표시이며 별도의 커서 목록/수명 상태를 복제하지 않는다. SOLID는 S=커서 생성/모델 측정 책임 유지, O=명시적인 제외 표시, L=기존 경계 조회와 커서 수명 유지, I=추가 공개 조작 API 없음, D=같은 View 계층의 작은 표시 계약으로 점검한다. JMH/FW의 Factory/Group 등록·UISystem 개폐와 입력 계약은 바뀌지 않는다. Core·FieldCamera·Architecture는 수정하지 않는다.
- **컴파일 미확인·Unity 화면 미확인.** Test/도우미·MSBuild·컴파일/Play·커밋/push는 수행하지 않는다. 사용자 Unity에서는 유닛을 선택해 화살표가 떠 있는 상태에서 설명에 진입할 때 흰 점선이 몸체 크기를 유지하는지, 다른 유닛 선택/단계 전환/재사용 뒤에도 같은지 확인한다. 화살표 자체의 일반 전투 표시도 함께 확인한다.
- 정적 확인: 이번 6파일의 UTF-8/LF·공백 및 한정 git diff --check 통과, 새 meta GUID 유일성을 확인했다. 범위 밖 기존 변경 28파일의 SHA-256은 유지됐다. 별도 읽기 전용 검수는 **Critical 0 / Important 0 / Minor 0**이며, 커서의 세 MeshRenderer 제외·몸체 형제 유지·최초 캐시 저장 전 제외·커서 재사용/해제 수명을 대조했다. 검수에서 제외한 기존 전체 Phase/겹침 선 알고리즘은 이번에 변경하지 않았으며, 실제 화면 크기·커서 표시 회귀는 위 사용자 Unity 확인으로 남긴다.

## 14. TargetBorder 프리팹 직접 연결 복구 — 2026-11-11

- 사용자 지적 후 현재 디스크를 재확인했다. TutorialViewUI.cs에는 직렬화 targetBorder 필드가 있지만 TutorialUI.prefab에는 해당 필드 값과 TargetBorder 오브젝트가 모두 없었다. 프리팹 스타일/박스 크기/배경도 이전 값이었다. 이전 정적 확인 기록만으로 현재 에셋 연결을 보장할 수 없으며, 사용자가 지적한 누락을 확인했다. 변경 경위는 확인하지 않았다.
- 기존 FocusBorder 뒤·ExplanationBox 앞에 TargetBorder(GameObject/RectTransform/CanvasRenderer/TutorialTargetBorderGraphic)를 복구하고 TutorialViewUI.targetBorder를 해당 Graphic에 직접 연결했다. 화면 전체 Stretch·Raycast Target 꺼짐·활성 상태를 유지한다. Combat 씬은 같은 프리팹을 참조하고 이 연결의 override가 없다.
- 앞서 승인된 흰색 영역/노란 대상 색, 점선 크기·실습 점멸, Model Width=0.04, 설명/실습 Box.png 및 크기/여백도 함께 복구했다. 이전 작업에서 보관한 프리팹과 SHA-256이 일치한다(`D7EE2284058D8E7BA668403B174A708D96B1DC9D77A72C64B4937A3F9174D676`). 다른 프리팹/씬/코드 값은 변경하지 않았다.
- 정적 확인: fileID 48개 유일성, TargetBorder 스크립트 GUID, 직접 참조, 부모/형제 순서, Stretch, 비활성 Raycast, 두 Box.png 참조, 예상한 블록만 변경됨과 한정 git diff --check를 확인했다. 기존 UIId.Tutorial 내부 위젯 연결 복구이며 서비스/입력/수명 계약 변경은 없다.
- **컴파일 미확인·Unity 화면 미확인.** 사용자 Unity에서는 TutorialUI 컴포넌트의 Target Border 슬롯 연결과 설명/실습 전환 표시를 확인한다. 별도 버그 검토에서 보고한 재질 재준비 시 커서가 모델 아웃라인 대상에 포함될 수 있는 문제는 이번 프리팹 연결 수정에 포함하지 않았으며 미수정이다.

## 15. 실습 안내를 오른쪽 위 배경 영역으로 이동 — 2026-11-11

- 사용자 화면에서 실습 안내가 상단 중앙 턴 표시를 덮어, 공격 없이 턴 종료를 누른 뒤 상대 턴임을 알아보기 어려웠다. 요청에 따라 모든 실습 안내를 오른쪽 위 설정 버튼 아래의 배경 영역에 배치한다.
- TutorialViewUI.RefreshPracticeLayout은 화면 오른쪽에서 Screen Margin만큼 띄운 위치를 공통 오른쪽 기준으로 사용한다. Practice Top Margin 기본값/프리팹 값은 40→136, PracticeGuide 기본 폭은 760→520으로 줄이고 기본 높이 112·본문 여백 32·Box.png는 유지한다. 긴 본문은 기존 TMP 측정으로 높이가 늘어난다. 막힌 조작 안내도 같은 오른쪽 기준으로 실습 안내의 실제 높이+Notice Gap 아래에 배치한다.
- 프리팹의 실습/조작 제한 안내 앵커·피벗은 오른쪽 위로 맞추고 초기 위치는 각각 (-24,-136), (-24,-264)로 둔다. Setup 신규 생성에도 같은 폭·위치·앵커를 사용한다. 매 표시 때의 실제 위치는 기존 화면 좌표→Canvas 변환이 정한다. 설명 단계 배치·게임 턴 표시·실습 성공 판정·입력·점멸·TargetBorder 연결은 바꾸지 않는다.
- SSOT/SOLID·JMH/FW·변경 독립성: 화면 여백은 기존 TutorialViewUI 설정, 기본 폭은 해당 RectTransform, 본문 높이는 기존 Measure 경로가 담당한다. View의 배치 계산과 생성 기본값만 변경하며 새로운 상태/서비스/UI 등록/입력 구독은 없다. Core·FieldCamera·Architecture는 수정하지 않는다.
- **컴파일 미확인·Unity 화면 미확인.** Test/도우미·MSBuild·컴파일/Play·커밋/push는 수행하지 않는다. 사용자 Unity에서는 공격 실습 중 바로 턴 종료를 눌러 중앙 상대 턴 표시가 보이는지, 설정 버튼/긴 문장/조작 제한 안내의 겹침과 해상도 변경 뒤 위치를 확인한다.
- 정적 확인: 프리팹은 상단 여백과 두 안내 RectTransform만 변경됐고, 범위 밖 기존 변경 29파일의 SHA-256이 유지됐다. 5파일 UTF-8/LF와 한정 git diff --check를 확인했다. 별도 읽기 전용 검수는 **Critical 0 / Important 0 / Minor 0**이다. 기준 1920×1080 Canvas 좌표에서 안내 x=1376~1896, 턴 표시 오른쪽 끝=1160으로 가로 간격 216, 설정 버튼 아래 간격 31이다. 기존 TMP 높이 측정/두 안내 쌓기·생성 기본값 일치를 대조했다. 실제 줄바꿈·다양한 화면 비율·상대 턴 전환은 Unity 미확인으로 남기며, 변경하지 않은 이전 TargetBorder/모델 커서와 서비스 등록 전체를 재인수하지 않았다.

## 16. 강조 구조 점검 1~5 반영 — 2026-11-11

- 사용자가 "Highlight Style의 아웃라인을 우리 구조에 맞게 정석으로 썼는지" 점검을 요청했고, 찾은 다섯 가지를 고쳤다(사용자 "1~4번 고쳐줘" → "5번도 고쳐줘"). 아웃라인을 그리는 방식은 그대로다. Cartoon 셰이더의 Outline Pass 값을 ColorSystem 공개 손잡이로 바꾸고, `UnitModelAppearance` 한 곳에서 합쳐 쓴다. SelectOutlineSystem은 MeshFilter 메시를 복사하는 방식이라 Skinned 유닛에 쓰지 않는다(이전 설계 §6.5).
- **1. 유닛 찾기:** `TutorialViewFocusLocator`가 위치를 갱신할 때마다 `FindObjectsByType<FieldUnitView>`로 씬 전체를 검색하던 것을, 전투 표시 표(`CombatSceneRoot.FieldUnits`, 개체 ID → 표시의 원본)의 새 조회 `FieldUnitPresenter.TryGetHighlightTarget`으로 바꿨다. 꺼진 표시는 지금처럼 화면에 없는 대상으로 본다.
- **2. 계약:** 튜토리얼이 구체 `FieldUnitView`의 `BeginOutline`·`TryGetModelBounds`·`Hud`·`transform`을 직접 쓰던 것을, 새 좁은 계약 `Combat/Display/Unit/ICombatUnitHighlightTarget`(+ `.meta`)으로 바꿨다. 멤버는 개체 ID·모델 번호·켜짐·루트 변환·HUD Transform·모델 경계·외곽선 대여다. 전투 연출 계약 `ICombatUnitPresentationTarget`에는 넣지 않았다(강조 쪽에만 필요한 경계·HUD를 연출 계약에 섞지 않음). `FieldUnitView`의 두 메서드는 이 계약의 명시적 구현이 됐다. 계약은 파괴된 표시에서도 `IsActive`·`TryGetModelBounds`가 false로 답한다.
- **3. 셰이더 키:** `UnitModelAppearance`가 안에서 만들던 `_OutlineColor`·`_OutlineWidth` 키와 문자열 두 번 쓰기를 공용 목록 `ColorShaderPropertyKey.OutlineColor`·`OutlineWidth`로 옮겼다. StageMap `StageNodeOutlineView`의 `_OutlineColor` 키도 같은 공용 키를 쓴다. SelectOutline 전용 `_OutlineThickness`·`_PulseGap`은 그대로 둔다.
- **4. UI 대상 찾기:** `MainCharactorUI`·`DeckCardList`는 연결마다 한 번만 찾고(꺼진 것도 담아 두고 켜짐은 쓸 때마다 봄, 파괴됐거나 없을 때만 다시 찾음), 자식 Graphic·UnitCardUI·부모 CanvasGroup 수집은 재사용 목록을 쓴다. 설명·실습 중의 매 프레임 씬 검색과 배열 할당이 없어진다.
- 바꾼 파일: Tutorial `TutorialViewFocusLocator`·`TutorialFocusFrame`·`TutorialModelBoundsCache`, Combat `FieldUnitView`·`FieldUnitPresenter`·`UnitModelAppearance`·`UnitModelBoundsExclusion`(설명만), 새 `ICombatUnitHighlightTarget.cs`(+ `.meta`), GlobalSystem `ColorTypeKey.cs`, StageMap `StageNodeOutlineView`. 모두 CRLF·BOM 없음을 유지했다. 시험 코드는 바꾸지 않았다(두 계약을 구현·참조하는 시험 없음).
- 원칙 점검: SSOT = 개체 ID → 표시는 FieldUnitPresenter 한 곳, 외곽선 속성 이름은 ColorShaderPropertyKey 한 곳. SOLID = I(강조 전용 좁은 계약), D(구체 MonoBehaviour 대신 계약). JMH/FW = §25 Find 남용·§15 Tick 할당 제거, 새 서비스·등록·Factory 변경 없음, ColorSystem 공개 손잡이 경로 그대로. 변경 독립성 = 튜토리얼 강조를 바꿀 때 전투 연출 계약을 읽거나 고치지 않아도 된다.
- **다른 작업자 공유(미전달).**
  - 용근님(전투 View): `FieldUnitPresenter`에 조회 함수 `TryGetHighlightTarget` 하나를 추가했다(기존 줄 무변경). `FieldUnitView` 26줄 클래스 선언 끝에 `ICombatUnitHighlightTarget`을 붙이고, 그 계약의 구현 줄을 추가했다. 기존 `BeginOutline`·`TryGetModelBounds`·아웃라인 대여는 호진 줄이며 명시적 구현으로 바뀌었다. 5번으로 `PrepareMovementAppearance`의 Renderer 수집 조건 한 줄(용근님 줄)을 `IsUnitModelPart` 호출로 바꿨다. 선택 커서(`UnitModelBoundsExclusion` 아래)가 모델 외형 층에서 빠지는 것 외에는 전투 동작 변화가 없다.
  - 민형님(GlobalSystem): `ColorShaderPropertyKey`에 `OutlineColor`(`_OutlineColor`)·`OutlineWidth`(`_OutlineWidth`) 키 두 줄을 추가했다. 기존 키·ColorSystem 동작은 바뀌지 않는다. 디졸브 키를 넣은 방식과 같다.
- **5. 선택 커서가 모델 외형에 잡힘(§14의 미수정 문제, 사용자 "5번도 고쳐줘"):** `FieldUnitView.PrepareMovementAppearance`는 꺼진 것을 포함한 모든 하위 Renderer를 모델 외형에 넣었다. 선택 커서는 한 번 붙으면 유닛 아래에 남으므로, 그 뒤 외형을 다시 준비하면(Cartoon 재질 전환·사망 디졸브·풀 재사용) 커서의 MeshRenderer가 모델로 잡혀 `BeginOutline`의 지원 검사가 커서 재질에서 예외를 낼 수 있었다. 처음 준비 때는 커서가 없어 원래도 커서에 외형 층이 걸리지 않았다.
  - "유닛 모델의 일부인가" 규칙을 `FieldUnitView.IsUnitModelPart` 한 곳에 두고(Canvas·TMP·`UnitModelBoundsExclusion` 아래가 아님), 외형 수집과 모델 경계 측정이 함께 쓴다. 커서는 반투명·플래시·디졸브·외곽선 모두에서 빠진다. 다시 준비한 경우도 처음 준비한 경우와 같아지고, 커서 표시가 관리하는 커서 색을 외형 층이 함께 쓰는 일도 없어진다.
  - `UnitModelBoundsExclusion`은 이름을 그대로 두고(GUID·참조 보존) 설명만 넓혔다. 몸체에 속한 무기 등 다른 MeshRenderer가 외곽선 Pass를 지원하지 않으면 지금처럼 예외로 알린다(조용히 건너뛰지 않음).
- **컴파일 미확인·Unity 화면 미확인.** Test/도우미·MSBuild·컴파일/Play·커밋/push는 하지 않았다. 사용자 Unity에서 확인할 것:
  - 설명 단계의 아군·적·넥서스 아웃라인과 흰 점선 위치가 전과 같은지, 단계 전환·숨김 때 사라지는지
  - 카드·메인 체력·행동 코스트 HUD 강조 위치가 전과 같은지, 덱을 숨겼다 다시 열어도 카드 강조가 나오는지
  - StageSelect 노드 호버 윤곽선 색이 그대로인지
  - 유닛을 선택해 커서가 붙은 뒤 적 사망 디졸브·이동 반투명·피격 플래시가 몸체에만 걸리고 커서 색은 그대로인지, 그 상태에서 설명의 유닛 아웃라인이 예외 없이 나오는지

## 17. 막힌 조작 안내 Box.png·같은 문장 주의 — 2026-11-11

- 사용자 화면: 실습 중 다른 유닛을 움직이려 하면 실습 안내 아래에 흰 사각형 상자가 떴다. 이 상자는 `BlockedNotice`(막힌 조작 안내)이며, 배경이 현강님 `6568bb947`에서 지워진 `UI_textbox_fullimg300.png`(GUID `57cef99e…`)를 가리켜 스프라이트 없이 그려졌다. 튜토리얼 상자 셋 중 Box.png를 쓰지 않던 것은 이것 하나였다. 에디터 도구도 Box.png 설정을 설명·실습 두 상자에만 했다.
- 반영: `TutorialUI.prefab`의 BlockedNotice 배경을 Box.png(Sliced, 픽셀 배율 2)로, 본문 여백을 20 → 32로 바꿔 실습 안내와 같게 했다(바뀐 값 셋, 글자 설정은 원래 같음). Combat 씬에는 이 상자의 덮어쓰기가 없다. 상자 높이는 `TutorialViewUI.Measure`가 본문 + 여백으로 늘리므로 글자가 잘리지 않는다. `TutorialUISetup`도 BlockedNotice에 `ConfigureTextBoxBackground`를 불러, 다시 만들어도 같은 배경이 된다.
- 같이 확인한 것(고치지 않음): 공용 `GlobalUICanvas`의 `NoticePannel`은 Box.png, `ConfirmPannel`(턴 종료·배치 확인창)과 `HoverPannel`(특성 설명)은 같은 TextBox 폴더의 `5_450.png`를 쓴다. 지워진 파일이 아니라 현강님 디자인으로 보아 그대로 둔다.
- **같은 문장일 때의 주의(사용자 "같은 설명이면 주의를 주는 것이 좋을 듯" → 흔들기 선택):** 24단계처럼 막힌 조작 안내 문구가 위에 떠 있는 실습 안내와 글자까지 같으면, 같은 상자를 하나 더 쌓지 않고 실습 안내를 흔든다. 판정은 `TutorialViewUI.ShowBlockedNotice` 한 곳이다(실습 안내가 떠 있고 문구가 같을 때). 흔들림은 좌우 세 번 왕복하며 끝으로 갈수록 줄고, 그동안 글자를 경고 색으로 바꿨다가 원래 색으로 돌린다. 흔들림은 실습 안내 배치(`RefreshPracticeLayout`) 뒤에 더해, 매 갱신 배치가 자리를 다시 정해도 쌓이지 않는다. 끝나는 시각은 기존 막힌 안내처럼 `Update`가 보고, 설명 열기·실습 안내 열기/닫기·화면 열기/숨기기에서도 끝내 글자 색이 남지 않는다. 문구가 다르면(18·22단계) 지금처럼 아래 상자를 띄운다. 정의 데이터는 바꾸지 않았다.
- 조정 값(TutorialViewUI 인스펙터 "같은 문장일 때의 주의"): 시간 0.4초, 폭 12(Canvas 단위), 글자 색 붉은색(0.85, 0.15, 0.1). 프리팹에는 아직 저장된 값이 없어 코드 기본값을 쓴다. 잘못된 값(0 이하 시간, 음수 폭, 알파 0 색)은 `ValidateReferences`가 예외로 알린다.
- **컴파일 미확인·Unity 화면 미확인.** 사용자 Unity에서 확인할 것:
  - 18·22단계에서 막힌 조작을 했을 때 아래 상자가 Box.png로 나오고 글자가 여백 안에 들어가는지, 2초 뒤 사라지는지
  - 24단계(턴 종료 실습)에서 다른 유닛을 움직이려 하면 아래 상자 없이 실습 안내가 흔들리고 글자가 잠깐 붉어졌다가 돌아오는지, 연달아 눌러도 색이 붉은 채 남지 않는지
  - 흔드는 중에 턴 종료로 실습을 통과해도 다음 안내의 글자 색이 정상인지

## 18. 턴 종료 실습의 입력 제한 해제 — 2026-11-11

- 사용자 요청: "처음에 아군 유닛으로 적 공격한 다음에 턴 종료를 강제하지 말고 다른 유닛들도 사용할 수 있게. 이동이나 공격 유저가 할 수 있는 것을 다 할 수 있게." 24단계(턴 종료 실습)의 입력 제한을 `EndTurnOnly` → `None`으로 바꿨다. 실습 통과 조건은 그대로 "플레이어 턴 종료"라서, 플레이어는 다른 아군을 자유롭게 이동·공격한 뒤 원할 때 턴을 끝내면 된다. 턴 종료 버튼의 노란 점멸 강조는 그대로 둔다.
- 바꾼 것:
  - `TutorialStepRule.CanPass`: 턴 종료 목표가 `None`도 받는다(`EndTurnOnly`도 그대로 허용). 처음 설계는 `EndTurnOnly`만 허용해, 데이터만 바꾸면 정의를 읽을 때 예외가 났다.
  - `TutorialDefinition_1-1.asset` 24단계: `inputRestriction` 3 → 0, `blockedInputText`를 비움(정의 검사가 "제한이 없으면 막힌 안내 문구는 비운다"를 요구). 그래서 §17의 같은 문장 흔들기는 24단계에서 더는 쓰이지 않는다(18·22단계는 문구가 달라 해당 없음, 흔들기 코드는 같은 문장일 때만 동작하므로 그대로 둔다).
  - 설계 §1·§5.1의 결정과 허용표를 바꿨다.
- 이 에셋은 작업 트리에서 이미 Unity가 다시 저장한 형태(긴 문자열 줄바꿈·빈 값 표기)였다. 형식을 맞춰 HEAD와 비교하면 내용 차이는 이번 두 칸뿐이다.
- 결과로 달라지는 것: 제한이 없으므로 24단계에서 행동하지 않은 아군이 남아 있으면 일반 전투처럼 턴 종료 확인창이 뜬다(`CombatEndTurnEntry.AsksConfirmation`). 확인하면 턴이 끝나 실습을 통과하고, 취소하면 계속 조작할 수 있다. 공격으로 전투를 이겨 버리는 경우 등 실습 밖 결과는 다른 제한 없음 단계와 같은 기존 처리를 따른다.
- **컴파일 미확인·Unity 재생 미확인.** 사용자 Unity에서 확인할 것:
  - 22단계 공격 통과 → 23단계 설명 → 24단계에서 다른 아군을 이동·공격할 수 있는지, 막힌 조작 안내가 뜨지 않는지
  - 행동하지 않은 아군이 있을 때 턴 종료를 누르면 확인창이 뜨고, 확인하면 실습이 통과돼 마지막 안내(25단계)로 가는지
  - 모든 아군을 쓴 뒤 턴 종료하면 확인창 없이 통과하는지
- **실습 안내 문구(사용자 "다른 유닛도 사용해도 되고 턴 종료를 눌러도 된다고, 글 쓰는 사람처럼"):** 24단계 실습 안내를 "남은 아군으로 이동하거나 공격해도 좋고, 바로 턴 종료 버튼을 눌러 턴을 마쳐도 됩니다."로 바꿨다. 앞의 23단계 설명("행동을 마쳤다면 턴 종료를 누르세요. 적이 행동한 뒤 다음 플레이어 턴이 시작됩니다.")은 새 흐름과 맞아 그대로 둔다. 문구가 길어져 실습 안내 상자는 본문에 맞춰 높아진다(`Measure`).
- **턴 종료 확인창 확인(코드 대조, 실행 확인 아님):** 24단계에서 턴 종료를 누르면 `CombatEndTurnConfirmation.Request`가 "턴 종료 가능 ∧ 확인을 물어도 됨(튜토리얼 제한 None)"일 때 행동하지 않은 아군 수를 센다. 필드에 살아 있고 행동 사용 상태가 `Available`인 아군만 세며, 이동만 한 유닛은 세지 않는다. 1명 이상이면 공용 확인창에 "아직 행동하지 않은 유닛이 N명 있습니다. 턴을 종료할까요?"가 뜬다. 확인하면 기존 턴 종료 경로로 제출돼 실습이 통과하고, 취소하면 그대로 계속 조작한다. 튜토리얼 허용표(`CombatInputPolicy.AllowsTutorialCommand`)는 제한 None이면 모두 허용하므로 확인창을 막지 않는다. 확인창(`GlobalUICanvas`의 `ConfirmPannel`)은 화면 가운데 500×300이고, 실습 안내는 오른쪽 위라 겹치지 않는다. 실습 중 튜토리얼 화면의 덮개·막은 꺼져 있고 안내 그래픽은 Raycast를 받지 않아 확인창 버튼 클릭을 가리지 않는다.
- 참고: 24단계 뒤 25단계 설명을 기다리는 동안의 연결 제한도 24단계 값(None)을 이어받는다. 12단계 → 13단계와 같은 기존 방식이며, 적 턴 동안은 원래 입력이 닫혀 있다.
  - (선택) Profiler에서 설명 중 GC Alloc이 줄었는지

## 19. 끝까지 마친 튜토리얼은 새 Run의 1-1에서 생략 — 2026-11-11

- 사용자 요청: "런 중에 여정포기나 패배를 했을 때 다시 1-1을 할 텐데 그때는 튜토리얼이 안 나오게." 권장안("튜토리얼을 끝까지 마치면 그 뒤 런에서는 생략, 게임을 다시 켜면 다시 나옴")으로 진행(사용자 "권장안대로 진행").
- 원인: 1-1 판정(`TutorialViewTarget.Find`)이 "이번 장에서 클리어한 전투가 없음"만 봤고, 스테이지 진행은 새 Run마다 새로 시작해 늘 다시 실행됐다. 기본 계약 설계 §15.2가 처음에 "1-1 진입마다 실행, 이전 완료 기록으로 막지 않음"으로 정했던 것을 바꿨다(§15.2·§15.12, 후속 설계 §1 한 줄).
- 바꾼 것:
  - `ITutorialStateBufferReadSink.HasCompleted(tutorialKey)`(`TutorialBuffer`): 이번 런타임에 그 정의의 `Completed` 세션이 있는지. 따로 기록을 두지 않고, 이미 런타임 동안 보존하던 종료 세션을 훑는다(한 사실은 한 곳). Combat 화면 연결 때 한 번 부르고, 값 열거라 할당 없음. 구현체는 `TutorialBuffer` 하나라 다른 곳은 고칠 것 없음.
  - `TutorialViewTarget`: 정의를 찾은 뒤 `HasCompleted`면 새 이유 `AlreadyCompleted`로 실행하지 않는다. `Find`가 튜토리얼 상태 읽기를 받는다(호출처는 `TutorialViewSystem.Attach` 한 곳, 시험 참조 없음).
- 세는 회차: 마지막 안내까지 마쳐 `Completed(Finished)`가 된 회차만. 패배·포기·화면 해제로 중간에 끊긴 회차(`Cancelled`·`Faulted`)는 세지 않아 다음 1-1에서 처음부터 다시 실행한다. 저장 파일은 없으므로 게임을 다시 실행하면(Play From Boot 재생마다 포함) 다시 나온다.
- 생략된 1-1은 세션이 없으므로 입력 제한 없는 일반 전투로 진행한다. 개발 로그에 "실행 대상이 아닙니다(AlreadyCompleted)"가 남는다.
- **컴파일 미확인·Unity 재생 미확인.** 사용자 Unity에서 확인할 것:
  - 1-1 튜토리얼을 마지막 안내까지 마친 뒤 여정 포기(또는 패배) → 로비 → 새 출발 → 1-1에서 튜토리얼이 안 나오고 일반 전투로 진행되는지
  - 튜토리얼 도중(마지막 안내 전) 여정 포기 → 새 출발 → 1-1에서 튜토리얼이 처음부터 다시 나오는지
  - 게임을 다시 실행하면 1-1에서 다시 나오는지
