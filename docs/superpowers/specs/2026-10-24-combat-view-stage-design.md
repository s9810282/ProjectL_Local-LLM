# 전투 View 단계 설계 (2026-10-24)

- **상태:** 1라운드 구현 완료(V-a~V-d 코드, V-e 문서, 2026-10-24, V-a~V-d Unity 확인 문제 없음 — 사용자, V-d의 마지막 검사 한 줄은 그 뒤에 더해 컴파일 미확인). 1라운드 측정(V-f) 결과는 §10(7.4 → 8.3)이다. 남은 것은 사용자 커밋과 2라운드 상세 설계다. 결정은 Q-V1~Q-V8·Q-T1~Q-T3 모두 A, P1~P7 그대로다. 계획과 실행 기록, 이 설계와 달라진 점은 [round1-plan](../plans/2026-10-24-combat-view-stage-round1-plan.md)에 있다(TP-1~TP-15와 각 Phase의 "계획과 다른 점"). 아래 §3의 "구현 메모"는 그 요약이다.
- **입력:** 2026-10-24 View 단계 독립 평가 **7.4/10**(하위 에이전트 3개, 우리 문서·메모리·git 기록 없이 지금 코드만, 채팅 보고). 기준별 점수는 SSOT 6.75, SOLID 6.25, FW 9.5, 있는 기능 8.0, 독립성 8.25, 불필요·우회 5.5다. 그 보고는 채팅에만 있어서, **문제 목록을 이 문서 §1에 기준선으로 고정한다.**
- **기준:** 작업 트리(HEAD `4c328d07` + 커밋 전 C 단계·골격 점검). 줄 번호는 지금 작업 트리 기준이다.
- **약어:**
  - `V/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`
  - `G/` = `Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/`
  - `F/` = `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/`
  - `T/` = `Assets/Tests/EditMode/Framework/`
- **지킬 것:**
  - MSBuild를 돌리지 않고, 새 `[Test]`를 만들지 않는다(보고에 "컴파일 미확인").
  - 커밋하지 않는다.
  - 범위는 전투 View(`V/`), `CombatViewBuffer`, 결과 화면 `CombatEndUI`, 전투 View Factory 셋이다.
  - GlobalSystem의 다른 UI(`PackUI`·`BattleChangerUI` 등), Framework, Core, 골격은 바꾸지 않는다. 골격은 바로 앞 단계에서 정리했고 이번에는 기반으로 둔다.
  - Missing Script를 만들거나, 참조 없는 에셋을 지우지 않는다. 그런 것을 찾으면 보고한다.

## 0. 목표와 측정

**목표:** 평가가 찾은 높음 1·중간 12를 묶음으로 풀고, 그 결과를 같은 범위에서 같은 평가자가 전후 비교로 잰다.

**측정 방법(10-24 사용자 질문 "점수가 개선되긴 한 거야?"에 대한 대답):**
- 범위를 고정한다. 오늘 평가와 같은 `V/` 241개 파일, `CombatViewBuffer`, `CombatEndUI`, Factory 셋이다.
- 라운드가 끝날 때마다, 같은 평가자가 라운드 전 판(A/B 중 하나)과 뒤 판을 함께 매긴다. 어느 쪽이 새것인지는 알리지 않는다(골격 A/B와 같은 방식).
- §1 문제 목록을 기준선으로 두고 "해결 / 남음 / 새로 찾음(기존 코드) / 새로 생김(우리 변경)"을 따로 센다.
- 예상 점수는 "알려진 문제 기준"으로만 적는다. 지난 측정에서는 예상보다 0.5~1.8 낮게 나왔다(10-22 예상 9.3 → 측정 7.5, C 단계 예상 8.0 → 측정 7.5). 평가자가 기존 코드에서 새 문제를 찾았기 때문이다.

## 1. 기준선 문제 목록 (10-24 View 평가, 같은 문제는 한 기준에서만 셈)

### 1.1 높음

| # | 기준 | 문제 | 근거 |
|---|---|---|---|
| **V-H1** | 불필요·우회 | 최종 승리 화면이 씬에 연결되지 않았다. 지금 데이터에서는 첫 보스 승리가 곧 Run 승리라(맵 `StageMap_Chapter1` 하나, `nextChapterKey` 0 = 마지막 챕터), 보스를 이기면 `CombatEndUI.Present`가 예외를 던지고 F1로 멈춘다. 참조 검사가 `ending`을 보지 않아 진입 때는 드러나지 않는다 | `G/UISystem/Screens/Stage/CombatEndUI.cs:93-97·258-286`, 씬 `Assets/Scenes/Combat.unity:11752` `ending: {fileID: 0}`, `ChapterContinuationRules.cs:25-26`. 10-23 사용자 결정으로 미뤘음(현강님 메모 §5·호진님 메모 §7) |

### 1.2 중간 (12)

| # | 기준 | 문제 | 근거 |
|---|---|---|---|
| **V-M1** | SSOT | "새 입력을 받을 수 있나"를 Policy 밖에서 조합한다. 제출 정지·확인창이 입력 사실(`CombatInputFacts`)에 없어서 입구마다 따로 붙이고, 그 조합이 서로 다르다(BattleChanger는 정지 ∨ 확인창, Pack은 정지만, 행동 버튼은 정지만) | `V/CombatViewSystem.cs:886-890·1072·1185-1188·1195`, `V/Input/CombatInputPolicy.cs:13·54-68·222-229`, `V/Input/ActionButtons/CombatActionButtons.cs:47-50` |
| **V-M2** | SSOT | 유닛 표시 대여 칸의 정리 목록이 세 벌이다(`Bind`·`OnDisable`·`ResetForPool`). `ResetForPool`에는 크기 정리가 빠졌고, `EntityId`를 먼저 비워 `OnDisable`의 크기 복원도 `IsCurrent`에 걸린다. 그래서 크기 대여가 남은 표시는 커진 채로 풀에 돌아간다 | `V/Display/Unit/FieldUnitView.cs:125-137·211-226·264-274·569` |
| **V-M3** | SSOT | 강조 층의 우선순위·색이 두 벌이다. 원본은 `FieldHighlightLayers.DrawOrder`·`ColorOf`이고, GridOutline 쪽 `FillKind`·`TryGetFillKind`·`FillColor`가 따로 다시 적혀 있다. 층을 더할 때마다 두 곳을 맞춰야 한다 | `V/Display/Field/FieldHighlightLayers.cs:75-87·124-140`, `V/Display/Field/FieldOutlineRenderer.cs:124-133·539-579` |
| **V-M4** | SOLID | 조정자 `CombatViewSystem`이 1216줄이다. 인터페이스 8개, 필드 약 45개, `BindServices` 인자 16개를 가진다. `BattleChangerUI`·`PackUI`를 직접 조작하고, 완료 배경음·튜토리얼 세션 해석·재고 읽기·타임라인 길이도 맡는다 | `V/CombatViewSystem.cs:63-139·211-227·946-947·1014-1025·1078·1140-1188` |
| **V-M5** | SOLID | 입력 묶음 `CombatInteraction`이 허브다. 가방 기능 하나가 묶음의 14곳과 Router 열거형·분기에 흩어져 있다. 새 입력 기능마다 메서드 약 10개를 함께 고쳐야 한다 | `V/Input/Interaction/CombatInteraction.cs:53-423`, `V/Input/CombatInputRouter.cs:43-51·83-86·109-114·208-247` |
| **V-M6** | SOLID | 공격 연출 종류(`CombatAttackPresentationKind`)의 분기가 파일 넷에 있다. 궤적 미리보기는 출발점·끝점을 실제 발사와 따로 조합한다 | `V/Presentation/Attack/CombatAttackPlayback.cs`, `V/Display/Feedback/CombatAttackTrajectory.cs:162-262`, `V/Presentation/Attack/CombatShotPath.cs:53-63`, `V/Playback/Handlers/CombatAttackEventHandler.cs:71-83`, `V/Display/Unit/CombatUnitPresentationWindow.cs:231-270` |
| **V-M7** | SOLID | 사건 형식 지식이 처리기 표 밖의 정적 분류기 넷에 흩어져 있다. 행동 사건을 하나 더하면 5~6곳을 함께 고쳐야 하고, 빠뜨리면 Gate·후속 반응·걸음 묶음이 오류 없이 틀어진다 | `V/Playback/Ordering/CombatActionBoundary.cs`, `V/Playback/Support/CombatEnemyPresentationPacing.cs`, `V/Presentation/Attack/CombatHitFollowUps.cs`, `V/Playback/Ordering/CombatEventSubjects.cs`, `V/Playback/CombatPhaseTransitionPresenter.cs:33-35`(민형님 코드) |
| **V-M8** | 있는 기능 | 미리보기 슬롯의 "내 요청일 때만 비우기"를 소유자 6곳이 각자 구현한다. Buffer에는 조건 없는 `ClearPreview()`만 있다. 새 소유자가 번호 확인을 빠뜨리면 남의 요청을 지운다 | `V/Input/Interaction/PlacementDragInteraction.cs:355`, `AttackTargetingInteraction.cs:488·500`, `CombatUnitRangeHover.cs:255`, `V/Input/MovePath/CombatMovementInteraction.cs:394`, `CombatMovementRangeTracker.cs:94·134`, `CombatReturnEstimateTracker.cs:88`, `BufferSystems/CombatView/CombatViewBuffer.cs:41·114-118` |
| **V-M9** | 있는 기능 | 화면 전환 설정(카메라 이름 "Day Camera"·"Night Camera", 레이어 이름, 속도 30·40)이 코드 상수다. View DB 설정 표와 맵 카메라 사실을 쓰지 않는다 | `V/Playback/CombatPhaseTransitionSettings.cs:19-39`(민형님 코드), `F/CombatSceneDisplayFactory.cs:150-153` |
| **V-M10** | 독립성 | 유닛 소리 프리셋이 유닛 키 switch와 분류 열거 값의 숫자에 박혀 있다. 포효 소리도 보스 키로 고정돼 있다. 유닛을 더하면 DB와 switch를 함께 고쳐야 하고, 빠뜨리면 오류 없이 소리만 나지 않는다 | `V/Playback/Support/CombatEventAudio.cs:59-86`, `V/Playback/Support/CombatEventSounds.cs:105` |
| **V-M11** | 불필요·우회 | 옛 손패(`HandPresenter`) 경로는 운영에서 닿지 않는다(늘 빈 목록·숨김). 그런데 필수 씬 참조와 입력 분기(손패 집기·손패 영역에 놓아 회수)로 남아 있다 | `V/Display/CombatSceneDisplay.cs:219-220`, `V/Display/Hud/HandPresenter.cs`, `V/Input/Interaction/PlacementDragInteraction.cs:249-250·319-323`, `V/Display/Feedback/CombatPointerMapper.cs:83-92·112`, `V/Input/Contracts/ICombatPointerPicker.cs:18·21`, `V/CombatSceneRoot.cs:35·56·81·99` |
| **V-M12** | 불필요·우회 | 시험 조립 때문에 운영 의존을 null 허용 선택 인자로 열어 두었다. 대표가 HUD 완료 대체 경로다. 운영 Factory는 결과 화면을 늘 넘기므로 닿지 않는다. 씬에서는 HUD의 `completionButton`과 결과 화면의 `confirmButton`이 같은 버튼이다. 그 밖에 `_combatQueries?.` 등이 같은 꼴이라, 운영 조립이 하나를 빠뜨려도 소리·전환·카메라·환경 설명이 조용히 꺼진다 | `V/Display/CombatSceneDisplay.cs:115-120·279-286`, `V/Display/Hud/CombatHud.cs:52-68·135`, 씬 `:10432`·`:11768`(같은 버튼 `2100000302`), `V/CombatViewSystem.cs:224-227·643-645·871`, `V/CombatDisplayServices.cs:31-35`, `F/CombatSceneDisplayFactory.cs:92·145-148`, `F/CombatSceneInputFactory.cs:63-64` |

### 1.3 낮음 (기준마다 상한 −1.0)

| # | 기준 | 문제 |
|---|---|---|
| V-L1 | SSOT | "필드 위 생존 유닛" 판정 사본이 약 9곳에 있다(변형 셋) |
| V-L2 | SSOT | 결과 화면이 골격 판정을 다시 계산한다(포기 사유 `:150`·최종 승리 `:94`·요약 일치, 스테이지 표기 두 번) |
| V-L3 | SSOT | Policy `CanSubmit`이 공유 규칙 `IsUnpresented` 대신 `S != P`를 직접 쓰고, `CanEditPath`는 `!IsPresenting`을 겹쳐 검사한다 |
| V-L4 | SSOT | 정리 순서·목록이 여러 벌이다(`Detach`·`StopInput`, 표시의 `_fieldBound`·`Clear`·`Release`) |
| V-L5 | SSOT | UI 입구의 보호 코드(`_released || IsFaulted || !_canReceive` + try/report)가 반복된다 |
| V-L6 | SOLID | 숨은 기능을 형식 검사로 꺼낸다(`_display is ICombatUnitInspection`, `V/CombatViewSystem.cs:1150`). 표시 계약 주석과 어긋난다 |
| V-L7 | SOLID | `CombatPathInput`(720줄)이 경로 상태 기계·범위·복귀 시점·공격 Drag 이관을 함께 맡는다 |
| V-L8 | SOLID | `CombatSceneEventPresenter` 생성자 선택 인자가 20개다 |
| V-L9 | FW | `CombatEndUI`가 `GetUIData` 대신 Factory의 `Configure`로 외형 표를 받는다 |
| V-L10 | FW | 런타임 조정자가 정적 Factory를 직접 부른다 |
| V-L11 | 있는 기능 | 레일이 기차 출발마다 EffectScope를 새로 받아 쌓는다 |
| V-L12 | 있는 기능 | DB 생성 때 검사한 연출 값을 다시 검사한다 |
| V-L13 | 독립성 | 포기를 요청한 뒤 제출 정지 전까지 빈틈이 있다(그 사이 제출한 명령은 Plan이 처리하지 않음) |
| V-L14 | 독립성 | 공격 모드와 아군 호버가 같은 층(`AttackRange`)을 쓰고, 배타가 호출 순서에 기댄다 |
| V-L15 | 독립성 | 미리보기 응답 전달 규칙이 둘이다(경로 입력은 매 Tick, 나머지는 번호 일치·순서 고정) |
| V-L16 | 독립성 | 확인창 캐스트 `textPresentation as IUIConfirmationPresentation`(`:252`)이 실패해도 연결 때 드러나지 않는다 |
| V-L17 | 불필요·우회 | `Display/Hud/CombatAbandonPanel.cs`는 코드·씬·프리팹 어디서도 쓰지 않는다(시험 하나만 씀) |
| V-L18 | 불필요·우회 | 시험에서만 쓰는 운영 멤버가 많다(조정자 `AttachDisplay`·`RequestReturn`·`Input` 등 약 20개) |
| V-L19 | 불필요·우회 | 죽은 코드가 있다: `FieldUnitPresenter.TryGetRelocationBody`, `CatusCannonWeaponView.PoseVersion`, `CombatActionPanel`의 복귀 문구 잔재, `CombatHud`의 옛 드로우 문구 |
| V-L20 | 불필요·우회 | Ballista 무기(코드 모션)·재장전 경로가 지금 데이터로는 닿지 않는데 임시 클립 경로와 두 벌로 남아 있다(추정) |

**기반 한계(감점 안 함):** 타임라인이 Buffer에 없음, Buffer 변경 알림 없음(순번 폴링), Core 미리보기 슬롯 하나, 확인창 계약이 서비스로 없음(캐스트 자체), 이펙트 Scope 회수 API 없음, 카메라 효과에 끝 신호 없음, 진행 계약이 정지·포기·완료 깃발을 View에 둠, `PackUI`·`BattleChangerUI`가 facts로 자기 보임을 다시 계산함(GlobalSystem).

## 2. 묶음과 라운드

문제를 근본 원인과 고칠 파일이 겹치는 것끼리 여덟 묶음으로 나눈다. 한 번에 다 하지 않고 셋으로 나눈다(Q-V2). 1라운드는 결함·SSOT·죽은 경로처럼 위험이 낮고 효과가 확실한 것이다. 2라운드는 입력·조정자·사건 처리의 구조다. 3라운드는 화면이 바뀔 위험이 큰 공격 연출과, 다른 사람 코드·시험에 닿는 값 이전이다.

| 라운드 | 묶음 | 푸는 문제 | 성격 |
|---|---|---|---|
| **1** | **W1 최종 승리 화면**(Q-V1) | V-H1 | 결함. 씬 편집이 필요하다 |
| 1 | **W2 입력 수신 판정 하나로** | V-M1, V-L3, V-L13(Q-V3) | SSOT. 보이는 차이는 Q-V3·Q-V4 |
| 1 | **W3 표시 대여·강조 층 원본 하나로** | V-M2, V-M3 | SSOT. V-M2는 결함 수정을 겸한다 |
| 1 | **W4 죽은 경로·조용한 실패 걷기** | V-M11, V-M12, V-L6, V-L16, V-L17(Q-V7), V-L19 | 불필요·우회. 씬·다른 사람 시험이 닿는다(Q-V5·Q-V6) |
| **2** | **W5 입력 소유자 공통 계약** | V-M5, V-M8, V-L7, V-L14, V-L15 | 구조. 1라운드 뒤 상세 설계 |
| 2 | **W6 조정자 나누기** | V-M4, V-L10 | 구조(10-22 결정 Q4로 미뤄 둔 클래스 분리). W2 뒤가 쉽다 |
| 2 | **W7 사건 형식 지식을 처리기로** | V-M7 | 구조. 민형님 `CombatPhaseTransitionPresenter`가 닿는다 |
| **3** | **W8 공격 종류 전략 하나** | V-M6, V-L20 | 구조. 공격 연출·궤적 화면이 바뀔 위험이 크다 |
| 3 | **W9 값은 DB로** | V-M9, V-M10, V-L11, V-L12 | 있는 기능. 민형님 코드, 현강님·호진님 시험이 닿는다 |

남는 낮음(V-L1·V-L2·V-L4·V-L5·V-L8·V-L9·V-L18)은 그 근처를 고치는 묶음에서 함께 보거나 남긴다(§7).

## 3. 1라운드 항목별 설계

### W1 최종 승리 화면 연결 (V-H1, Q-V1)

**지금.**
- 최종 승리(지금 데이터에서는 첫 보스 승리)에서 `CombatEndUI.Present`는 `ending`(`EndingRunSummaryUI`)이 비면 예외를 던진다(`CombatEndUI.cs:93-97`).
- 엔딩 패널은 병합 `9e0c0f2b` 때 Combat 씬에서 빠졌다. 예전 자리였던 결과 캔버스 `CombatEndCanvas`는 평소 꺼진 채 저장돼 있다. 그래서 그 아래에 다시 두면, 결과 화면을 열지 않는 최종 승리에서 엔딩의 부모 활성 조건에 걸린다(현강님 메모 §5).
- `ValidateReferences`(`:258-286`)가 `ending`을 검사하지 않는다.

**바꾼 뒤(Q-V1 A).** 순서가 중요하다. 검사를 먼저 넣으면, 씬을 고치기 전까지 모든 Combat 진입이 연결 검사에서 멈춘다.
1. **사용자(Unity 에디터):** `EndingPanel.prefab`을 Combat 씬의 **항상 켜진 캔버스** 아래에 두고, `CombatEndUI.ending`에 연결한다. 어느 캔버스인지는 현강님 메모 §5의 권장을 따른다.
2. **코드:** `ValidateReferences`에 `Require(ending, nameof(ending))`를 더한다. 그러면 다시 빠질 때 진입 때 바로 드러난다.
3. **확인:** 보스 승리 → 엔딩 요약 → 확인 → 로비 흐름을 재생해 본다(사용자).

- **동작:** 최종 승리에서 F1 대신 엔딩 요약이 보인다. 다른 결과는 같다.
- **다른 사람 줄:** `CombatEndUI.cs`는 현강님 파일이다(우리 영역 안). 검사 한 줄을 더한다. 씬 편집은 사용자가 한다. 호진님 엔딩 작업과 겹칠 수 있어 10-23에 미뤘던 일이므로, 착수 전 사용자가 다른 작업자와 확인한다.
- **구현 메모(2026-10-24):**
  - 검사는 셋이 됐다: `ending` null, 결과 화면 밖, 부모 캔버스 켜짐(`EndingRunSummaryUI.Open`과 같은 조건). 뒤의 둘은 계획 TP-13과 V-d 검토 뒤 사용자 결정 A다.
  - 기존 도우미 `Require`는 "결과 화면 안"을 보므로 엔딩에는 쓰지 않았다.
  - 사용자가 만든 캔버스 이름은 `BossEndingCanvas`(Overlay, 정렬 7, 켜짐)이고, 패널은 꺼진 채 저장돼 열릴 때 켜진다.

### W2 입력 수신 판정 하나로 (V-M1·V-L3·V-L13)

**지금.**
- `CombatInputPolicy`는 "판단은 이 한 곳에서"라고 적혀 있다(`:13`). 그런데 제출 정지·확인창은 입력 사실에 없어서 조정자가 입구마다 직접 붙인다.

| 자리 | 지금 조건 |
|---|---|
| 입력 Layer 맞추기 `SyncInputLayer`(`:886-890`) | 정지면 해제 |
| 덱·가방 UI 입구 `CanReceiveDeckInput`(`:1072`) | 입력 준비 ∧ ¬확인창 ∧ ¬정지 |
| BattleChanger(`:1186`) | 정지 ∨ 확인창이면 숨김 |
| Pack(`:1187`) | 정지면 숨김(확인창은 안 봄) |
| 행동 버튼(`:1188`, `CombatActionButtons.cs:49`) | 정지면 숨김(확인창은 안 봄) |
| 배치 확인창 판단 `CanEnterFromDeployment`(`:1195`) | ¬정지(확인창은 일부러 안 봄, 스스로 닫히지 않게) |

- 포기를 확인하면 깃발만 서고, 입력은 골격의 제출 정지 Job이 돌 때까지 열려 있다(V-L13).
- `CanSubmit`은 공유 규칙 `CombatProgressFacts.IsUnpresented` 대신 `S != P`를 쓴다(V-L3).

**바꾼 뒤.**
- `CombatInputFacts`에 사실 둘(Q-V3 A면 셋)을 더한다. 생성자 끝의 선택 인자로 더하므로 기존 호출은 그대로 컴파일된다.
  - `IsSubmissionSuspended`(진행 사실 `_progress.IsSubmissionSuspended`)
  - `IsModalOpen`(배치 확인창 열림)
  - Q-V3 A: `IsAbandonRequested`(진행 사실)
- `CombatInputPolicy`에 판정 하나를 둔다.

```csharp
/// <summary>새 입력을 닫아야 하는지입니다. 제출 정지(골격) 또는 포기 요청이 있으면 닫습니다. 입력 입구는 이 판정 하나를 읽습니다.</summary>
internal static bool IsInputClosed(in CombatInputFacts facts) =>
    facts.IsSubmissionSuspended || facts.IsAbandonRequested;   // Q-V3 A

/// <summary>확인창이 열려 있어 다른 입력을 막아야 하는지까지 더한 판정입니다. 확인창 자신의 판단은 이것을 쓰지 않습니다.</summary>
internal static bool IsInputBlocked(in CombatInputFacts facts) => IsInputClosed(facts) || facts.IsModalOpen;
```

- `PassesCommon`이 `!IsInputClosed(facts)`를 본다. 그러면 `CanSubmit`·`CanPreview` 등 Policy 판정이 모두 같은 조건을 쓴다.
- 조정자의 자리마다 판정 하나를 읽는다.
  - `SyncInputLayer`·`CanEnterFromDeployment` → `IsInputClosed`
  - `CanReceiveDeckInput`·BattleChanger·Pack·행동 버튼 → `IsInputBlocked`
  - `PackUI.Refresh(facts, blocked)`·`BattleChangerUI.Refresh(facts, blocked)`의 서명은 바꾸지 않는다(GlobalSystem). 넘기는 값만 같은 판정이 된다.
- `CanSubmit`의 `S != P`는 `CombatProgressFacts.IsUnpresented(P, S)`로 바꾼다. 결과가 S > P에서는 같다. P > S는 생기지 않는다. `CanEditPath`의 겹친 검사는 지운다.

**동작(보이는 차이):**
- Q-V4 A면 배치 확인창이 열린 동안 가방도 숨는다. 지금은 BattleChanger만 숨는다.
- Q-V3 A면 포기를 확인한 그 Tick부터 행동 버튼·덱·가방·입력이 닫힌다. 지금은 골격의 제출 정지 Job이 도는 다음 Tick부터 닫힌다. 그 한 Tick 사이의 제출은 지금도 Plan이 처리하지 않는다.
- 그 밖에는 같다.

**다른 사람 줄:** `CombatInputFacts.cs`(호진님 12줄 — 튜토리얼 제한 사실), `CombatInputPolicy.cs`(호진님 22줄). 더하는 줄은 우리 줄이다. `SyncActionButtons` 주변의 현강님 줄은 계획에서 blame으로 센다.

**구현 메모(2026-10-24):**
- 호스트 콜백 하나(`OnInputClosed`)를 제출 정지와 포기 요청이 함께 부른다.
- `CanEnterFromDeployment`는 `CanSubmit`의 공통 판정이 입력 닫힘을 보므로 따로 검사하지 않는다.
- **예외 입구:** 수신 창구(`CombatInputReceiver`)는 계속 자기 확인창 조건(`IsModalOpen`)을 쓴다. 확인창 동안에도 FocusLost·SelectCanceled는 통과해야 하고, 입력 닫힘은 Unbind가 맡기 때문이다(V-b 검토 L7).
- `CanEditPath`의 겹친 `!IsPresenting`은 남겼다. 우리 시험 사례가 순번과 따로 "재생 중"을 켜기 때문이다(사용자 결정 A, 2라운드에서 사실 묶음의 "재생 중"과 함께 본다).

### W3 표시 대여·강조 층 원본 하나로 (V-M2·V-M3)

**V-M2 지금:** 정리 목록이 셋이다.
- `Bind`(`:130-135`): 자세·크기·플래시·클립·투명도·체력 미리보기
- `OnDisable`(`:268-274`): 투명도·자세·크기·플래시·클립·체력 미리보기
- `ResetForPool`(`:213-226`): 투명도·자세·플래시·클립·체력 미리보기 — **크기가 없다.** 그리고 `EntityId`를 먼저 비운 뒤 끈다.

**V-M2 바꾼 뒤:**
- private `ReleasePresentation()` 하나가 대여 칸 다섯(자세·크기·플래시·클립·투명도)과 체력 미리보기를 정리한다. 세 자리가 이것을 부른다.
- `ResetForPool`은 `ReleasePresentation()` → Ballista 무기 정리(지금 그대로) → HUD 정리 → `EntityId` 비우기 → 끄기 순서로 바꾼다. 대여의 `IsCurrent`가 `EntityId`를 보므로, 비우기 전에 정리해야 복원이 된다. `OnDisable`의 HUD 동기화·클립 멈춤은 그 자리에만 남긴다.
- 순서가 달랐던 두 목록(`Bind`는 자세 먼저, `OnDisable`은 투명도 먼저)은 하나로 맞춘다. 칸들은 서로 독립이라 순서는 결과에 영향이 없다(계획에서 확인).
- **동작:** 크기 대여가 남은 채 풀로 돌아가던 표시가 원래 크기로 돌아간다(결함 수정). 정상 흐름에서는 재생 취소가 먼저 되돌리므로 보이는 차이는 없다.

**V-M3 지금:**
- 원본 `FieldHighlightLayers.DrawOrder`(낮은 것부터 목표·행위자·이동·공격·적 공격·위험·예정 경로·재생 강조·영향·차단 벽)와 `ColorOf`가 있다.
- GridOutline 쪽이 `FillKind` 열거(배치 구역·이동·공격·적 공격·위험·영향·호버), 우선순위 사슬 `TryGetFillKind`, 색 `FillColor`를 따로 가진다.

**V-M3 바꾼 뒤:**
- `FieldHighlightLayers`에 "GridOutline에서 칸을 채우는 층인가"를 한 곳에 둔다. 예: `FillsCell(layer)`가 이동·공격·적 공격·위험·영향이면 참.
- 아웃라인은 `DrawOrder`를 높은 것부터 돌며 처음 `FillsCell`인 층을 고른다. 호버·배치 구역은 지금처럼 그 앞뒤의 특례로 둔다.
- 색은 `ColorOf(layer)` → 팔레트로 읽는다. 영향 칸만 "공격 호버 상태 색"을 쓰는 특례를 `FieldHighlightLayers` 쪽 함수 하나로 옮긴다.
- `FillKind` 열거는 지운다(호버·배치 구역을 표시하는 작은 값만 남김).
- **동작:** 같다. 지금 두 사슬의 순서가 같고(위험 > 적 공격 > 공격 > 이동), 같은 칸에서 둘 이상이 함께 보이지 않는다.
- 계획에서 두 순서를 줄 단위로 대조한다.
- **다른 사람 줄:** `FieldOutlineRenderer.cs`의 민형님 26줄(데칼 쪽으로 보임)은 계획에서 blame으로 확인해 닿지 않게 한다.

### W4 죽은 경로·조용한 실패 걷기 (V-M11·V-M12·V-L6·V-L16·V-L17·V-L19)

**V-M11 옛 손패(Q-V5):**
- 손패 선택은 유지 덱 카드가 맡는다. `HandPresenter`는 늘 빈 목록·숨김으로 그려지므로 집기·놓기 판정이 늘 거짓이다.
- 바꾼 뒤(Q-V5 A):
  - 표시 계약과 입력 계약에서 손패 멤버를 뺀다(`ICombatPointerPicker.TryPickHandUnit`·`IsOverHandDropZone`, `CombatSceneDisplay:328-334`, `CombatPointerMapper`의 손패 분기). 포인터 변환의 취소 영역 판정(`CombatPointerMapper:112`)은 UI 가림만 남는다. 손패 영역은 늘 숨겨져 있어 지금도 거짓이므로 결과는 같다.
  - `PlacementDragInteraction`의 손패 집기·손패 영역 회수 분기를 지운다.
  - `CombatSceneDisplay`·`CombatPlacementFeedback`·Factory에서 `HandPresenter`를 뺀다.
  - `CombatSceneRoot`의 `hand` 필수 검사와 필드를 뺀다(직렬화 값은 Unity가 무시한다).
- **스크립트 파일 `HandPresenter.cs`·`HandCardHoverView.cs`는 이번에 지우지 않는다.** `HandPresenter`는 Combat 씬에 컴포넌트로 붙어 있어 지우면 Missing Script가 된다. 사용자가 씬에서 컴포넌트를 뗀 뒤 지울지 정한다(보고). `HandCardHoverView`는 호진님 시험 `TutorialViewFocusLocatorTests.cs:92`가 형식을 쓴다.
- 우리 시험 `HandCardHoverTests`(손패 호버 전체)는 걷은 기능을 보므로 지운다(Q-T1). `CombatDisplayReuseTests`·`CombatPlacementFeedbackTests`·`CombatInteractionTests`·`CombatPointerMappingTests`의 손패 부분은 지우거나 호출을 맞춘다.
- 다른 사람 가짜(현강님 `CombatEnvironmentHoverTests`, 호진님 `HojinCombatAttackDragTests`)는 계약에서 빠진 멤버가 공개 메서드로 남아도 컴파일되므로 손대지 않는다.

**V-M12 HUD 완료 대체 경로와 null 허용 의존(Q-V6·Q-V7):**
- HUD 완료 경로는 운영에서 닿지 않는다. 바꾼 뒤(Q-V6 A):
  - `CombatSceneDisplay`의 `_completion == null` 갈래와 `CombatHud.ShowCompletion`·`HideCompletion`, HUD의 `completionButton`·`completionText` 참조를 지운다.
  - 결과 화면을 필수로 받는다.
  - 씬의 버튼 객체는 그대로 둔다(결과 화면 확인 버튼).
- **다른 사람 시험(허락 필요):** 호진님 `StageMapNodeMovementBootSmokeTests.cs:523-533`·`.Victory.cs:207`은 `Hud`의 `completionButton`을 리플렉션으로 찾아 누른다. 같은 버튼이므로 `CombatEndUI`의 `confirmButton`을 찾도록 두 곳을 바꾼다.
- null 허용 의존(Q-V7 A):
  - 운영 조립 입구(조정자 `BindServices`, `CombatDisplayServices`, 두 Factory)는 소리·전환·카메라·Core 질의를 필수로 받는다.
  - 시험 편의는 시험용 입구에서 가짜나 널 객체를 넘기게 한다. 10-22 개선의 Factory 오류 경계와 같은 꼴이다.
  - 영향 시험 목록은 계획에서 센다(대부분 우리 시험).
- **동작:** 같다. 운영 조립이 의존을 빠뜨리면 조용히 꺼지는 대신 연결 때 예외가 난다.

**V-L6·V-L16 조용한 형식 검사:**
- `_display is ICombatUnitInspection`(`:1150`): 형식 검사를 없앤다. 표시 Factory가 만들 때 유닛 정보 창구를 따로 넘긴다. 표시 계약 `ICombatBattleDisplay`에는 넣지 않는다(그 계약의 시험 가짜가 많아서).
- `textPresentation as IUIConfirmationPresentation`(`:252`): 캐스트 자체는 기반 한계다. 실패하면 `BindServices`에서 바로 예외를 내게 한다.

**V-L17 `CombatAbandonPanel`(Q-V8):** 코드·씬·프리팹 어디서도 쓰지 않는다. 우리 시험 `CombatDisplayReuseTests.cs:760` 하나만 쓴다. 지울지는 사용자 결정이다(Q-V8, 규칙상 먼저 묻는다).

**V-L19 죽은 코드:** `FieldUnitPresenter.TryGetRelocationBody`(참조 0), `CatusCannonWeaponView.PoseVersion`, `CombatActionPanel`의 주석 처리된 복귀 문구와 쓰지 않는 비교, `CombatHud`의 옛 드로우 문구 숨김을 지운다. 계획에서 줄 주인을 확인한다. `CombatHud`의 옛 드로우 문구는 씬이 아직 연결하므로 필드는 남기고 코드만 정리할지 계획에서 정한다.

**구현 메모(2026-10-24):**
- 손패 스크립트 두 파일과 그 자체를 보는 우리 시험은 남겼다(계획 TP-10). 위 "`HandCardHoverTests`는 지운다"와 다르다. 씬 컴포넌트를 뗀 뒤 함께 지운다.
- 손패로 Drag를 시작하던 시험은 덱 카드 선택으로 입력만 바꿨다. `ClickToPick_…`은 필드 유닛 입력으로 살렸다.
- HUD 완료 시험 둘에 현강님 `[TestCase]` 줄이 있었다. 삭제는 사후 확정을 받았다.
- 행동 패널 시험 도우미는 이미 깨져 있었다(없는 `returnText`, 빠진 필수 참조 셋). 함께 고쳤다(TP-12).
- 옛 드로우 문구는 필드까지 지웠다. 씬의 그 객체는 이미 꺼져 있었다(TP-15, Q-T3 c).
- V-M12는 표시 생성자 선택 인자 넷을 필수로, 씬 루트 `unitInfo` 필수로 했다. 시험 입구의 null 허용은 2라운드 W6으로 넘겼다(TP-7). 정리 경로의 Unity null 검사(파괴된 객체)는 남겼다.

## 4. 정할 것

권장은 A다.

| # | 묻는 것 | A(권장) | B | C |
|---|---|---|---|---|
| **Q-V1** | V-H1을 이번에 | 1라운드에 넣음. 사용자가 씬에 엔딩을 연결한 뒤 우리가 검사를 더함(다른 작업자와 겹치는지 사용자가 먼저 확인) | 계속 미룸(보스 승리 F1 그대로) | — |
| **Q-V2** | 라운드 구성 | 1(W1~W4) → 2(W5~W7) → 3(W8·W9). 라운드마다 A/B 측정 | 1라운드만 하고 다시 평가 | 한 번에 모두 |
| **Q-V3** | 포기 요청도 입력을 닫나 | 닫음(확인한 Tick부터, 한 Tick 빠름) | 지금처럼 골격의 제출 정지부터 | — |
| **Q-V4** | 배치 확인창이 열린 동안 가방도 숨기나 | 숨김(BattleChanger·행동 버튼과 같은 판정) | 지금처럼 가방은 정지만 봄 | — |
| **Q-V5** | 옛 손패 | 코드 경로·계약·필수 참조를 걷음. 스크립트 파일은 남기고, 씬 컴포넌트를 뗀 뒤 지울지 다시 정함 | 그대로 | — |
| **Q-V6** | HUD 완료 대체 경로 | 지움. 호진님 Boot 시험 두 곳은 결과 화면 버튼을 찾게 바꿈(허락) | 지우되 HUD의 버튼 참조는 남김(시험 무변경, 죽은 참조가 남음) | 그대로 |
| **Q-V7** | null 허용 운영 의존 | 운영 입구 필수, 시험 입구 분리 | HUD 완료만 하고 나머지는 2라운드 | — |
| **Q-V8** | `CombatAbandonPanel.cs` | 지움(우리 시험 한 사례도 지움) | 남기고 보고만 | — |
| **Q-T1** | 우리 시험 | 걷은 기능·바뀐 동작은 지우고 나머지는 호출만 맞춤(지난 단계와 같음) | — | — |
| **Q-T2** | 다른 사람 시험 | 호진님 `StageMapNodeMovementBootSmokeTests` 두 곳(Q-V6 A일 때) 고침 허락 | 고치지 않음 → Q-V6 B | — |

## 5. 설계에서 정한 것 (확인 대상)

- **P1** 입력 사실에 더하는 값은 생성자 끝의 선택 인자다(기존 호출 무변경). 조정자의 `ReadFacts` 한 곳이 채운다.
- **P2** GlobalSystem의 `PackUI`·`BattleChangerUI` 서명은 바꾸지 않고 넘기는 값만 같은 판정으로 한다. 두 UI가 facts로 보임을 다시 계산하는 것은 기반 한계로 둔다.
- **P3** 배치 확인창 자신의 판단(`CanEnterFromDeployment`)은 확인창 열림을 보지 않는 지금 규칙을 지킨다(C 단계 C-a에서 찾은 "스스로 닫힘" 방지).
- **P4** W3의 강조 층은 `FieldHighlightLayers`가 원본이고, 아웃라인은 읽기만 한다. 셰이더·팔레트 값은 바꾸지 않는다.
- **P5** W4에서 Missing Script를 만들지 않는다. 씬에 붙은 컴포넌트의 스크립트는 지우지 않는다.
- **P6** 유닛 정보 창구는 표시 계약에 넣지 않고, 표시를 만들 때 따로 넘긴다.
- **P7** 2·3라운드는 1라운드 측정 뒤 상세 설계를 보완한다. 이 문서의 §6은 방향만 적는다.

## 6. 2·3라운드 방향 (1라운드 뒤 상세 설계)

> 2라운드 상세 설계는 [round2-design](2026-10-24-combat-view-stage-round2-design.md)이다(2026-10-24). 아래 W5의 "응답 전달 하나로"와 W7의 "처리기 멤버로"는 그 설계에서 바꿔 권장했다(Q-V13·Q-V18).

- **W5 입력 소유자 공통 계약(V-M5·V-M8·V-L7·V-L14·V-L15):**
  - 입력 기능(배치·공격·경로·가방·호버)이 공통 계약(포인터 이동·누름·뗌·취소·결과 뒤·멈춤·활성)을 구현한다. `CombatInteraction`은 목록을 돈다.
  - Router의 대상 열거형은 계약의 "이 포인터를 받나"로 바꾼다.
  - 미리보기 슬롯은 `CombatViewBuffer.ClearPreview(int number)`(번호가 같을 때만 비움) 하나로 바꾼다. 소유자 6곳이 그것을 부른다.
  - 공격 Drag 이관은 경로 입력 밖으로 뺀다.
- **W6 조정자 나누기(V-M4·V-L10):**
  - 입력 표면 동기화(BattleChanger·Pack·행동 버튼·덱 입구)를 하나로 뺀다. W2 판정을 쓴다.
  - 그 밖에 뺄 것: 완료 배경음(표시 쪽으로), 튜토리얼 세션 보고, 타임라인 읽기.
  - 표시·입력 생성은 Factory가 주입한다.
- **W7 사건 형식 지식(V-M7):**
  - 행동 경계·적 연출 간격·후속 반응·주체를 형식별 처리기 멤버로 옮긴다(`ActorOf`가 이미 그 꼴).
  - 민형님 `CombatPhaseTransitionPresenter`가 경로 표 밖에서 전환 사건을 고르는 것은 그 묶음 설계에서 다시 본다(민형님 확인).
- **W8 공격 종류 전략(V-M6·V-L20):**
  - 종류별 전략 객체 하나(출발점·구간·발사·접촉)를 실제 연출과 궤적 미리보기가 함께 쓴다.
  - Ballista 코드 모션 경로를 남길지 정한다.
- **W9 값은 DB로(V-M9·V-M10·V-L11·V-L12):**
  - 화면 전환 설정은 View DB 설정 표로 옮기고, 카메라는 맵 카메라 사실에서 고른다(민형님 코드).
  - 유닛 소리 프리셋은 유닛 외형 데이터 칸으로 옮긴다. 포효 소리는 포효한 유닛의 키로 고른다.
  - 프리셋 값을 직접 단언하는 현강님 `GameAudioTests`·호진님 `StageMapCombatEntryBootSmokeTests`가 닿는다. 그 라운드에서 허락을 받는다.

## 7. 원칙 점검과 예상

**SSOT.** 입력 수신, 대여 정리, 강조 층 순서·색이 각각 한 곳이 된다(W2·W3).

**SOLID.** 1라운드는 형식 검사 하나만 줄인다. 큰 개선은 2라운드(W5~W7)다.

**FW 계약.** 1라운드 변화는 없다. 조정자가 Factory를 직접 부르는 것(V-L10)은 W6에서 푼다.

**있는 기능 사용.** 2·3라운드(W5의 미리보기 비우기, W9)다.

**독립성.** 포기 빈틈(V-L13)과 늦게 드러나는 캐스트(V-L16)를 1라운드에서 푼다.

**불필요·우회.** H1, 옛 손패, HUD 완료·null 허용 의존, 죽은 코드를 1라운드에서 푼다.

**새로 생기는 문제(예상).**
- 입력 사실의 멤버가 늘어난다(낮음 아님, 사실 묶음의 역할 그대로).
- 손패 스크립트 두 파일이 쓰는 곳 없이 남는다. Q-V5에서 씬 컴포넌트를 뗀 뒤 정할 때까지다(낮음 하나로 셀 수 있음).

**예상 점수(알려진 문제 기준, §0의 주의대로 실제 측정은 더 낮을 수 있음):**

| 기준 | 지금 | 1라운드 뒤(권장안) | 2라운드 뒤 | 3라운드 뒤 |
|---|---|---|---|---|
| SSOT | 6.75 | 9.0 | 9.0 | 9.0 |
| SOLID | 6.25 | 6.5 | 9.0 | 9.75 |
| FW 계약 | 9.5 | 9.5 | 9.75 | 9.75 |
| 있는 기능 사용 | 8.0 | 8.0 | 8.75 | 9.5 |
| 독립성 | 8.25 | 8.75 | 9.25 | 10.0 |
| 불필요·우회 | 5.5 | 9.25 | 9.25 | 9.25 |
| **합** | **7.4** | **약 8.5** | **약 9.2** | **약 9.5** |

- Q-V1 B(H1을 미룸)면 1라운드 뒤 불필요·우회가 7.25가 되어 합은 약 8.2다.
- Q-V8 A(`CombatAbandonPanel` 삭제)면 불필요·우회 낮음이 하나 줄어 9.5, 합은 약 8.55다.

## 8. 다른 사람 줄과 공유 (각 라운드 끝 문서 Phase, 모두 미전달로 작성)

- **현강님:**
  - `CombatEndUI.cs` 검사 한 줄(W1)
  - `CombatViewSystem.cs`의 현강님 줄 중 W2가 바꾸는 곳(계획에서 blame으로 셈)
  - `FieldUnitView.cs`의 HUD 줄은 W3에서 닿지 않게 한다
  - `PackUI` 보임 조건에 확인창이 더해지는 차이(Q-V4 A)
- **호진님:**
  - `CombatInputFacts`·`CombatInputPolicy`의 호진님 줄 옆에 더하는 것(W2)
  - 고친 Boot 시험 두 곳(Q-V6 A)
  - 손패 형식을 쓰는 시험(`TutorialViewFocusLocatorTests`)은 그대로라는 것
  - 엔딩 연결(W1)이 호진님 엔딩 작업과 겹치는지
- **민형님:** 1라운드에는 없다(W3에서 `FieldOutlineRenderer`의 민형님 줄은 닿지 않게 함). 2·3라운드 W7·W9에서 민형님 코드에 닿는다.
- **천명님:** 1라운드에는 없다.

## 9. 이번 단계 밖

- **골격:** 바로 앞 단계(골격 점검)에서 정리했다. 골격 A/B 평가에 남은 중간 둘(진입 요청 단계의 분산, 제출 정지 깃발의 별도 저장)은 이번 View 단계에서 다루지 않는다. 다만 제출 정지 깃발은 W2와 맞물린다. "깃발을 완료·포기 사실에서 계산"으로 바꾸려면 진행 계약(`ICombatProgressView`)을 바꿔야 해서, 골격과 View를 함께 보는 다음 기회로 둔다.
- **기반 한계(§1.3 끝):** 감점하지 않으며 고치지 않는다.
- **남는 낮음:** 생존 유닛 판정 사본(V-L1), 결과 화면의 재계산(V-L2, 결과 화면이 판정된 값만 받게 하는 일은 W6과 함께 다시 봄), 정리 목록(V-L4), UI 입구 보호 코드(V-L5), Presenter 인자(V-L8), `CombatEndUI.GetUIData`(V-L9), 시험 전용 멤버(V-L18).

## 10. 1라운드 측정 (2026-10-24, V-f)

**방법:**
- 범위는 §0과 같다. `ViewSystem/Combat/` 전체, `CombatView` Buffer, `CombatEndUI`, 전투 View Factory 셋(표시·입력·유닛 HUD), 그리고 씬 판단용 `Combat.unity`다.
- 두 판을 스크래치패드의 이름 없는 폴더 둘(P·Q)로 꺼냈다. 배정은 무작위였고, 결과는 P = 기준 커밋 `8ed3f8cb`, Q = 1라운드 뒤 작업 트리다.
- 하위 에이전트 3개가 두 판을 함께 매겼다. 나눈 기준은 SSOT·독립성 / SOLID·FW·있는 기능 / 불필요·우회다. 어느 쪽이 새 판인지는 알리지 않았고, 우리 문서·메모리·시험·git 기록도 읽지 못하게 했다.
- 기준·감점은 10-24 평가와 같다. 높음 −2, 중간 −0.75, 낮음 −0.25이고, 낮음은 기준마다 −1.0까지만 깎는다.
- 합칠 때 같은 문제는 한 기준에서만 셌다. 기준선(§1)에 있던 문제는 §1과 같은 기준에 두어 전후를 비교할 수 있게 했다.
- 새로 나온 중간 이상 주장(Ballista 경로)과 낮음 둘(`lastHit`, 남긴 `HandPresenter`)은 코드·에셋으로 직접 대조했다. 모두 맞았다.

**점수(합친 뒤):**

| 기준 | P(`8ed3f8cb`) | Q(1라운드 뒤) | §7 예상(Q) |
|---|---|---|---|
| SSOT | 7.0 | **9.25** | 9.0 |
| SOLID | 6.0 | **6.25** | 6.5 |
| FW 계약 | 9.5 | **9.5** | 9.5 |
| 있는 기능 사용 | 8.75 | **8.75** | 8.0 |
| 독립성 | 8.25 | **8.25** | 8.75 |
| 불필요·우회 | 4.75 | **7.75** | 9.0 |
| **합** | **7.4** | **8.3** | 8.5 |

- P(7.4)는 10-24 단독 평가(7.4)와 같게 나왔다. 그래서 이 측정은 같은 잣대라고 볼 수 있다.
- Q는 예상보다 0.2 낮다. 지난 두 번의 차이(0.5~1.8)보다 작다.
- 평가자 원점수(합치기 전, 같은 문제를 여러 기준이 셈)는 P 5.9 / Q 7.75였다.

**기준선 문제의 상태(Q):**
- **해결:**
  - V-H1(최종 승리 화면)
  - V-M1(입력 수신 조합), V-M2(대여 정리 세 벌), V-M3(강조 층 두 벌)
  - V-M11(옛 손패 경로)
  - V-L3·V-L6·V-L13·V-L16·V-L17·V-L19
- **일부 해결:** V-M12. HUD 완료 대체 경로·표시 선택 인자·씬 루트 `unitInfo`는 풀렸다. 하지만 시험 입구의 null 허용(`BindServices`·`CombatDisplayServices`·`CombatSceneEventPresenter` 선택 인자, 입력 Factory의 알림 생략)을 평가자가 **중간**으로 매겼다. 계획 TP-7은 낮음으로 예상했었다.
- **남음:**
  - SOLID 중간 넷: V-M4 조정자, V-M5 입력 묶음 허브, V-M6 공격 종류 분기, V-M7 사건 형식 분류기
  - V-M8 미리보기 비우기 여섯 곳(있는 기능, 중간)
  - V-M10 소리 프리셋 switch(독립성, 중간)
  - V-M9 화면 전환 설정 고정(평가자는 **낮음**으로 매김)
  - 낮음: V-L2·V-L7·V-L9·V-L14·V-L15·V-L18
- **등급이 바뀐 것:** V-L20(Ballista 두 경로)을 평가자가 **중간**으로 올렸다. `BallistaWeaponView`가 붙은 `EnemyUnit_Ballista.prefab`은 어떤 DB 에셋·씬도 참조하지 않는다(직접 확인). 그래서 코드 모션 경로가 닿지 않는데도 계약·상태가 남아 있다. 이 프리팹은 참조 없는 에셋이라 지우지 않고 보고한다.

**새로 찾음(기존 코드, 두 판 모두, 낮음):**
- 지뢰·화약통 목록 두 곳(`CombatHazardLookup`·`CombatExplosionRules`, SSOT)
- 폭발 이펙트 종류·회전 고정(`EffectType.Boom`, 있는 기능)
- Core 사유를 문자열 `"InvalidMainCharacter"`로 비교(`CombatActionNotice`, 독립성)
- 아이템 드래그 여부가 사실 묶음에 없어 기본 인자에 기댐(`CombatInputRouter`·`CombatPathInput`, 독립성)
- 행동 중 층·평소 층 마스크 둘(독립성)
- 강조 층 하나를 더하는 데 다섯 곳(SOLID)
- 표시가 그리기 밖 전역 상태를 바꿈(배경음 프리셋, 덱 UI 개폐, SOLID)
- 결과 화면 `lastHit`가 필수인데 켜지는 곳이 없음(불필요)

**새로 생김(우리 변경, Q만, 낮음):**
- 엔딩 부모 계층 조건이 `EndingRunSummaryUI.Open`의 사본(SSOT)
- 쓰는 곳 없이 남은 손패 스크립트 두 파일. 씬 컴포넌트의 `Update`가 매 프레임 바로 빠져나온다(TP-10에서 예상, 불필요)

**2라운드 방향에 주는 것(§6 보완):**
- W5(입력 소유자 공통 계약)는 V-M5와, 평가자가 지적한 Poll 순서·층 배타의 숨은 순서(V-L14·V-L15)를 함께 푼다. V-M8도 함께 푼다.
- W6(조정자 나누기)에 V-M12의 남은 부분(시험 입구 null 허용 → 주입·널 객체)을 넣는다. 중간으로 남았기 때문이다.
- W7은 V-M7을 푼다.
- 3라운드 W8에 Ballista 두 경로(이제 중간)를 넣는다.
