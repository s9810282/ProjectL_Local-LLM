# 전투 유닛 호버 공격 범위 Task 계획 (2026-10-19)

- 설계: [unit-hover-attack-range-design](../specs/2026-10-19-combat-unit-hover-attack-range-design.md)(UR1~UR7). 이 계획의 결정·규칙 원본은 설계 문서다. 이 문서는 Task·순서·검증·실행 기록만 둔다.
- 결정(2026-10-19, 사용자): Q-UR1 A(같은 슬롯 + 유휴 규칙 R-a·R-b), Q-UR2 A(적 범위는 플레이어 시점), Q-UR3 A(공격 범위와 같은 `AttackRange` 색). Core 조회는 사용자 지시로 직접 구현한다(CLAUDE.md 예외, 데이터 경계 문서 §5.46).
- 상태: **P1 = A 결정(2026-10-19). U-0·U-1·U-2와 적·아군 색 나누기(§7.4) 모두 완료, Unity 확인 문제 없음(2026-10-19 사용자). 남은 것은 사용자 커밋.** Phase마다 멈추고 보고한다.

## 0. 착수 조건·기준

### 0.1 착수 조건

- 기준 코드: HEAD `927962fb`(MR-1 `4b6e7320` 병합 뒤). 작업 트리의 다른 변경은 `FMODStudioCache.asset`·`Unit_MainCharacter.asset`뿐이고 이 작업과 파일이 겹치지 않는다(착수 때 `git status`로 다시 확인).
- 폴더 정리(M 계획)는 아직 착수 전이다. 새 파일은 지금 평면 폴더에 두고 M 계획 §3 대응표에 줄을 더한다(T19). M이 먼저 들어가면 대응표 위치(`Input/Interaction/`)에 둔다.
- 같은 파일을 고치는 진행 중 작업: 없음(카메라 K·가방 P·MR-1은 커밋됨). 착수 전에 `CombatPathInput.cs`·`CombatInteraction.cs`·`CombatSceneInputFactory.cs`·`CombatPreviewService.cs`의 미커밋 변경이 없는지 본다.

### 0.2 검증 방법

- 컴파일: VS 2022 MSBuild + extra.targets(새 파일만 Compile에 더함, 산출물은 스크래치패드), 시험·에디터·비개발 정의.
- 회귀: Unity 내장 Mono 관리 실행(순번 `@n`으로 실행, 도구는 스크래치에 다시 만든다). U-0 기준선 대비 기존 시험 상태 변화를 본다.
- Unity: 관리 실행에서 NATIVE·Unity 전용인 시험과 화면은 Phase 끝에 사용자 확인 목록으로 넘긴다.

### 0.3 계획에서 정할 것 P1 — 슬롯 규칙의 코드 소유자 (MR 평가 M2)

[이동 모드 범위 설계](../specs/2026-10-19-combat-move-mode-range-design.md) §8.6 M2(중간): 한 슬롯의 사용 규칙에 코드 소유자가 없다. 지금 범위 → 복귀 견적 순서가 경로 입력 네 조각(`RefreshReturnEstimate`의 범위 대기, `RefreshMoveModeRange`의 복귀 원함 대기, `RequestReturn`의 내려놓기, `OnPreview`의 잃은 요청 정리)에 있고, M2는 "호버 Q-UR1 결정 때 함께 정한다"고 권한다. Q-UR1 A는 규칙(R-a·R-b)을 정했고, 그 규칙의 **코드 자리**를 여기서 정한다.

| 안 | 내용 | 대가 |
|---|---|---|
| **A (권장) 배경 요청 규칙 함수 하나** | 순수 정적 함수(가칭 `CombatPreviewSlotRule.ShouldRequestBackground(answeredForFacts, ownPending, hasPreview, slotNumber, answeredNumber)`)가 R-a("슬롯이 비었거나 들어 있는 요청이 답을 받았을 때만")와 R-b("같은 사실로 답 받았거나 자기 요청이 기다리는 중이면 안 함")를 한 식으로 답한다. 배경 요청자 셋(이동 모드 범위·복귀 견적·호버)이 이것만 부른다. 범위의 "복귀 원함 대기"와 복귀의 "범위 대기"가 이 한 식으로 바뀐다(둘 다 R-a의 경우). 전경 요청(Drag·공격 모드·배치 Drag·복귀 버튼)은 지금처럼 바로 덮어쓴다. | `CombatPathInput` 두 곳(`RefreshMoveModeRange`·`RefreshReturnEstimate`)의 조건을 바꾼다. 범위 → 복귀 순서는 `Refresh`의 호출 순서로 유지되고 MR 시험은 그대로 통과해야 한다(바뀌면 그 시험을 보고하고 멈춘다). 새 배경 요청은 이 함수를 부르기만 하면 된다(M2 독립성 해소). |
| B UR 범위만 | 호버에 R-a, 복귀 견적에 R-b만 더한다. 범위·복귀의 기존 순서 조각은 그대로다. | 슬롯 규칙 코드가 다섯 조각 이상으로 늘어 M2가 남는다. |

- **결정(2026-10-19, 사용자): A.**
- 두 안 모두 M1(범위 수명 소유자 분리, 재평가 N3(a))은 하지 않는다. 그것은 폴더 정리 뒤 따로 한다(MR 평가 권고).
- A안에서 "답 받음"은 요청자마다 자기 사실로 기억한다(범위 = 보이는 사실, 복귀 견적 = `CombatReturnRequest.IsAnswered`, 호버 = 보이는 유닛). 함수는 그 값을 받기만 한다.
- 슬롯 응답 여부는 Buffer 슬롯 번호와 Core 응답 번호(`ICombatStateBufferReadSink.AnsweredPreviewNumber`)로 판단한다. 경로 입력은 이미 둘을 갖고 있고, 호버는 씬 조립에서 같은 읽기 계약을 받는다.

## 1. 바꾸는 파일

| 영역 | 파일 | Phase | 변경 |
|---|---|---|---|
| Core 계약 | `CoreCaculator/Contracts/Results/UnitAttackRangeResult.cs`(새) | U-1 | 조회 결과(성공·실패 이유·유닛·범위 칸) |
| Core 계약 | `CoreCaculator/Contracts/Combat/CombatPreviewResult.cs` | U-1 | `UnitRange` 칸·`ForUnitAttackRange` |
| Core 계산 | `CoreCaculator/Application/Combat/CombatAttackQueries.cs` | U-1 | `UnitReady`·`RangeCells` 분리, `UnitRange` |
| Core 계산 | `CoreCaculator/Application/Combat/CombatCommandCalculator.cs` | U-1 | `GetUnitAttackRange` |
| Core 서비스 | `CoreSystem/CombatSystem/CombatPreviewService.cs` | U-1 | 종류 switch 두 곳 |
| Buffer(View 정의) | `BufferSystems/CombatView/CombatViewPreviewKind.cs`·`CombatViewPreview.cs` | U-1 | `UnitAttackRange = 8`·생성 함수 |
| View 정책 | `ViewSystem/Combat/CombatInputPolicy.cs` | U-2 | 국면 무관 종류·`CanInspectUnit` |
| View 슬롯 규칙(P1 A) | `ViewSystem/Combat/CombatPreviewSlotRule.cs`(새, 가칭) | U-2 | 배경 요청 규칙 함수 |
| View 경로 | `ViewSystem/Combat/CombatReturnRequest.cs`·`CombatPathInput.cs` | U-2 | 복귀 견적 R-b(답 받음), P1 A면 범위·복귀 조건을 규칙 함수로 |
| View 입력 | `ViewSystem/Combat/CombatUnitRangeHover.cs`(새) | U-2 | 호버 소유자 |
| View 입력 | `ViewSystem/Combat/CombatInteraction.cs` | U-2 | 다섯째 기능 위임, 소유자 활성 속성 |
| View 조정자 | `ViewSystem/Combat/CombatViewSystem.cs` | U-2 | `Poll` 호출에 사실·캐릭터(한 줄) |
| Factory | `Factory/Runtime/InGame/View/CombatSceneInputFactory.cs` | U-2 | 씬 조립에서 호버 생성 |
| 시험 | 새 `UnitAttackRangeQueryTests.cs`·`CombatUnitRangeHoverTests.cs`, 기존 정책·복귀·상호작용·미리보기 서비스·Buffer 시험 | U-1·U-2 | §4 |
| 문서 | 데이터 경계 §5.46, Factory 공유 §17, M 계획 §3, `viewDocs/stage-input-guide.md` §2.2, 설계 실행 기록 | U-1·U-2 | §5 |

## 2. Phase U-0 기준선

- **T1** 관리 실행 도구를 스크래치에 다시 만든다(VS 2022 MSBuild로 시험 어셈블리를 빌드하고 Unity 내장 Mono로 순수 시험을 돌리는, 앞 계획들과 같은 방식). 기준 HEAD `927962fb`, 지금 작업 트리.
- **T2** 전체 관리 실행 기준선을 잡고 결과(PASS·FAIL·NATIVE 수, 기존 FAIL 목록)를 §7.1에 적는다. MR-1 기록의 기존 실패(`CombatEntryIntegrationTests` 15·`ShopStageServiceTests` 1)와 대조한다.

## 3. Phase U-1 Core 조회·Buffer 종류 (화면 동작 변화 없음)

- **T3 결과 계약** `UnitAttackRangeResult`(readonly struct, Contracts/Results):
  - `Succeeded`·`FailureReason`(`AttackFailureReason`)·`UnitId`·`RangeCells`(`ReadOnlyMemory<CellCoord>`, 받은 배열을 복사).
  - `Success(unitId, cells)`·`Failure(reason, unitId)`. `Succeeded`와 `FailureReason == None`이 늘 같다.
- **T4 응답 칸** `CombatPreviewResult.UnitRange`(`UnitAttackRangeResult?`)와 `ForUnitAttackRange(number, result)`. 빈 유닛 ID·성공/실패 불일치는 `ArgumentException`. "내용은 하나만" 검사에 새 칸을 더한다.
- **T5 계산** `CombatAttackQueries`(설계 UR2):
  - `UnitReady(input, unit)`: 지금 `Actor`의 유닛 쪽 검사(필드·생존 → 기본 공격 가능 → 여러 칸 보류)를 그대로 옮긴다.
  - `Actor` = 종료 → 국면·처리 종류 → 현재 행위자 → 찾기 → 소유자 → `UnitReady`. **검사 순서와 실패 이유는 지금과 같다.**
  - `RangeCells(input, actor, profile, policy, environment)`: `Range` 본문의 칸 계산(모양·사거리 보정·보드 안·전달 검사·대포 인접 금지·정렬)을 그대로 옮긴다. `Range`는 이것을 부른다.
  - `UnitRange(input)`: 종료면 `StageEnded` → `input.ActorId`로 찾기(없으면 `EntityNotFound`) → `UnitReady` → 프로필·정책 → 환경 `VisibleTo(Owner.Player)` → `RangeCells` → 성공. 차례·국면·소유자는 보지 않는다.
- **T6 진입** `CombatCommandCalculator.GetUnitAttackRange(input)`.
- **T7 Buffer** `CombatViewPreviewKind.UnitAttackRange = 8`("유닛 하나의 공격 범위 조회, 차례·국면 무관, 플레이어 시점"), `CombatViewPreviewData.UnitAttackRange(EntityId unit)`(빈 ID 거절).
- **T8 서비스** `CombatPreviewService`:
  - 명령 종류 매핑에 `UnitAttackRange => CombatCommandKind.Attack`.
  - 결과 생성에 `UnitAttackRange => ForUnitAttackRange(...)`를 **명시**한다. 지금 결과 switch의 기본 갈래는 배치 미리보기라, 빠뜨리면 예외 없이 배치 계산이 돈다.
- **T9 Core 시험**(§4.1).
- **T10 기록** 데이터 경계 문서 §5.46(사용자 지시 구현, 바꾼 Core 파일·계약·검사 순서 보존·플레이어 시점). 미전달 표시.
- **검증·보고**: MSBuild 오류 0·새 경고 0, 관리 실행 U-0 대비 기존 시험 상태 변화 0, 새 시험 PASS. §7.2에 적고 멈춘다.

## 4. Phase U-2 View

- **T11 정책** `CombatInputPolicy`(설계 UR3):
  - `CanPreview`: 국면 무관 종류(`UnitAttackRange`)는 공통 조건 + PlayerInput + 실제 Layer == `LayerOf(facts.StagePhase)`. 판별은 이 함수 한 곳.
  - `StageOf(CombatViewPreviewKind)`는 이 종류에 단계를 돌려주지 않는다(지금처럼 예외). 문서 주석에 국면 무관 종류는 `CanPreview`가 다룬다고 적는다.
  - `CanInspectUnit(facts, hasPending)` = `CanPreview(UnitAttackRange)` && 재생 중 아님.
- **T12 복귀 견적 R-b·슬롯 규칙**(설계 UR4, 계획 P1):
  - `CombatReturnRequest`: `IsAnswered`·`MarkAnswered()`, `Reset`이 지운다. `Begin`은 지운다(새 요청).
  - `CombatPathInput.HandleReturnPreview`: `IsAnswer` 통과 뒤 `MarkAnswered`.
  - P1 A: 새 `CombatPreviewSlotRule`(순수 정적, 문서 주석이 설계 UR4를 가리킴). `RefreshMoveModeRange`의 `_return.IsWanted` 대기와 `RefreshReturnEstimate`의 `OwnsPreview`·`_rangeRequest.IsPending` 검사를 이 함수 호출로 바꾼다(복귀 쪽은 `IsAnswered`를 답 받음으로 넘김). `RequestReturn`의 내려놓기·`OnPreview`의 잃은 요청 정리는 전경 요청·잃은 요청 처리라 그대로 둔다.
  - P1 B: `RefreshReturnEstimate`에 `IsAnswered`면 다시 요청하지 않는 검사만 더한다.
  - 원함(`RequestReturn`) 흐름은 그대로다(`CancelInputs` → `Reset` → 새 요청).
  - 이 Task 뒤 MR 시험(`CombatMoveModeRangeTests`·`CombatMovementRangeFeedbackTests`·`CombatMovementFeedbackTests`·`CombatMovementPlanningInputTests`)을 먼저 돌려 기존 상태 변화 0을 확인하고 다음 Task로 간다.
- **T13 호버 소유자** `CombatUnitRangeHover`(설계 UR5):
  - 생성 인자: Buffer 쓰기·읽기, 칸 집기, 입력 표시(`ICombatInteractionFeedback`), UI 가림(`ICombatPointerOcclusion`), Gate, P1 A면 전투 상태 읽기(Core 응답 번호).
  - 상태: 마지막 포인터(있음·좌표), 다시 집기 필요, 원하는 유닛, 자기 요청 번호·유닛, 보이는 유닛.
  - `PointerMoved(facts, characters, x, y)`: 포인터를 기억하고 판단한다.
  - 판단: 허용(`CanInspectUnit`)·UI 위 아님·칸 집기·`FindAnyAt` → 유닛. 유닛 없음이면 숨김·자기 요청 정리. 보이는 유닛·기다리는 유닛과 같으면 그대로. 다르면 숨기고 요청(R-a: P1 A면 `CombatPreviewSlotRule`, B면 슬롯이 비었거나 `facts.PreviewAnswer`가 있을 때만. 아니면 원하는 유닛만 두고 기다림).
  - `Poll(facts, characters)`: 허용이 아니면 숨김·요청 정리(포인터는 남기고 다시 집기 필요). 허용이면 다시 집기 필요 시 마지막 포인터로 판단, 원하는 유닛이 있고 요청이 없으면 R-a 조건으로 요청.
  - `PreviewAnswered(facts)` → bool: 자기 번호이고 `UnitRange`가 있고 유닛이 같을 때만. 성공이면 `ShowRangeCells`, 실패면 숨김. 자기 슬롯을 비운다.
  - `Suspend()`: 숨김·요청 정리, 포인터는 남기고 다시 집기 필요(다른 소유자 활성·결과 전·표현 정리).
  - `Clear()`: `Suspend` + 포인터를 잊는다(포커스 상실·강제 선택 종료·정지).
  - 숨김은 자기가 그렸을 때만 `ClearRangeCells`. 슬롯 비우기는 최신 요청이 자기 번호일 때만.
  - 할당: 포인터 콜백은 칸 집기·`FindAnyAt`만, 배열은 응답 때 한 번.
- **T14 묶음 연결** `CombatInteraction`(설계 UR6):
  - 생성자 끝 선택 인자 `rangeHover`(없으면 아무것도 하지 않음).
  - `IsInputOwnerActive`(배치 Drag·아이템 Drag·공격 모드·경로 지정) 속성을 두고 `IsCursorSnapMode`가 이것을 쓴다.
  - `PointerMoved`: 대상이 Placement·Path이고 소유자 비활성이면 호버로, 아니면 `Suspend`.
  - `SelectPressed` 뒤 소유자가 활성이면 `Suspend`. `SelectDeckUnit`·`SelectItem`·`AttackModeRequested`(공격 `Enter` 전)에서 `Suspend`.
  - `Poll(facts, characters, latestPreview)`: 경로 뒤 소유자 활성이면 `Suspend`, 아니면 호버 `Poll`.
  - `PreviewAnswered`: 공격 → 호버 → 배치 순서.
  - `BeforeResult`·`CancelPresentation` → `Suspend`, `SelectCanceled`·`CancelInputs`·`Stop` → `Clear`.
- **T15 조정자** `CombatViewSystem.cs` Tick의 `_interaction.Poll(...)` 한 줄에 `ReadFacts()`·`_characters.Characters`를 넘긴다. 다른 조정자 변경 없음.
- **T16 Factory** `CombatSceneInputFactory.CreateSceneInteraction`이 호버를 만들고(`display`가 집기·표시·가림을 모두 제공) `CreateInteraction`의 새 선택 인자로 넘긴다. `CreateInteraction`을 직접 쓰는 시험 조립에는 넣지 않는다(설계 §4.1).
- **T17 시험**(§4.2).
- **T18 기존 시험 영향 조사**: 씬 경로(`CreateSceneInteraction`)를 쓰는 Unity 시험 중 포인터를 유닛 칸으로 옮긴 뒤 미리보기 슬롯·요청 번호·복귀 견적 왕복 수를 단언하는 것을 grep으로 찾는다(`PointerMoved`·`SetPreview`·`AnsweredPreviewNumber`·`Preview.Number`). 호버 요청·R-b 때문에 기대가 바뀌는 것만 고치고 §7.3에 목록을 남긴다.
- **T19 문서**: Factory 공유 문서 §17(민형님, 미전달), M 계획 §3 `Input/Interaction/` 줄에 `CombatUnitRangeHover`, `viewDocs/stage-input-guide.md` §2.2에 호버 범위 한 단락, 설계 문서 머리말 상태.
- **검증·보고**: MSBuild 오류 0·새 경고 0, 관리 실행 U-1 대비 기존 시험 상태 변화(T18에서 고친 것만), 새 시험 PASS. Unity 확인 목록(§6)을 넘기고 멈춘다.

## 4.1 U-1 시험

| 파일 | 사례 |
|---|---|
| 새 `UnitAttackRangeQueryTests.cs`(EditMode, `CombatCommandTestData`) | 현재 플레이어 행위자의 `UnitRange` 칸 = `GetAttackRange` 칸(공유 함수 회귀) / 차례가 아닌 아군 성공 / 적 유닛 성공 / 배치 국면 성공 / 손패·사망 `NotInCombat` / 없는 ID `EntityNotFound` / 기본 공격 불가 `BasicAttackDisabled` / 끝난 전투 `StageEnded` / 적만 아는 모래폭풍 칸의 적은 사거리가 줄지 않음(시험 자료가 지형 소유자 지식을 지원할 때) |
| `CombatPreviewServiceTests` | 새 종류 요청에 `UnitRange` 응답 게시(성공·실패), 게임 상태 무변경 |
| `CombatPreviewResult` 시험(있는 곳에, 없으면 위 파일) | 빈 유닛 ID·성공/실패 불일치 거절, 내용 하나 |
| `CombatViewBufferTests` | `UnitAttackRange` 생성 함수가 빈 ID를 거절 |
| 기존 공격 범위 시험 | 변경 없이 통과(`Range` 동작 보존 확인) |

## 4.2 U-2 시험

| 파일 | 사례 |
|---|---|
| `CombatInputPolicyTests` | `UnitAttackRange`: 배치·전투 Layer 모두 허용 / Automatic·대기·종료·Layer 불일치 거절 / `CanInspectUnit`은 재생 중 거절 |
| `CombatReturnRequestTests` | `MarkAnswered`·`Reset`·`Begin`의 답 받음 |
| 새 `CombatPreviewSlotRuleTests.cs`(P1 A, 순수) | 빈 슬롯·답 받은 슬롯이면 요청 / 답 안 된 남의 요청이면 기다림 / 자기 요청이 기다리는 중이면 안 함 / 같은 사실로 답 받았으면 안 함 |
| `CombatMoveModeRangeTests`(또는 새 경로 시험) | 복귀 견적이 답을 받은 뒤 남이 슬롯을 가져가도 다시 요청하지 않음 / `CancelInputs` 뒤에는 다시 요청 / 범위 → 복귀 견적 순서 유지 |
| 새 `CombatUnitRangeHoverTests.cs`(순수) | 유닛이 바뀔 때만 요청 / 같은 유닛 위 이동은 요청 없음 / 빈 칸·UI 위·집기 실패면 숨김 / 남의 응답·다른 유닛 응답 무시 / 실패 응답은 숨김 / 답 안 된 슬롯이면 기다렸다가 `Poll`에서 요청(R-a) / 답 받은 슬롯이면 바로 요청 / 자기 응답 뒤 슬롯 비움, 남의 요청은 안 비움 / `Suspend` 뒤 `Poll`이 마지막 포인터로 다시 판단 / `Clear` 뒤에는 새 포인터 전까지 아무것도 안 함 / 허용이 꺼지면 `Poll`이 숨김 / 숨김은 자기가 그렸을 때만 |
| `CombatInteractionTests` | 소유자 활성 때 호버 `Suspend`(배치 Drag·공격 모드·경로 지정) / 공격 진입이 공격 범위 요청보다 먼저 호버를 지움 / `PreviewAnswered` 순서 / 호버 없는 조립은 지금과 같음 |
| 수렴 시험(순수, 경로 입력 + 호버) | 이동 모드에서 범위 → 복귀 견적 → 호버가 각각 한 번씩 요청·응답되고 그 뒤 반복 `Refresh`·`Poll`이 슬롯을 다시 쓰지 않음 |

## 5. 공유·기록

- 데이터 경계 문서 §5.46(천명님): 사용자 지시로 직접 구현한 Core 조회. U-1에서 쓴다.
- Factory 공유 문서 §17(민형님): `CombatSceneInputFactory` 호버 생성·`CreateInteraction` 선택 인자. U-2에서 쓴다.
- 다른 작업자 단독 파일(`CombatEnvironmentHover`·`UnitHUD` 등)은 건드리지 않는다.
- 커밋 때 새 파일(`UnitAttackRangeResult.cs`·`CombatUnitRangeHover.cs`·`UnitAttackRangeQueryTests.cs`·`CombatUnitRangeHoverTests.cs`, P1 A면 `CombatPreviewSlotRule.cs`·`CombatPreviewSlotRuleTests.cs`)을 `.meta`와 함께. 커밋은 사용자가 한다.

## 6. Unity 확인 목록 (U-2 끝, 사용자)

1. 배치 국면: 아군·적 유닛에 호버 → 공격 범위, 빈 칸·UI 위 → 사라짐, 손패 Drag 시작 → 사라짐.
2. 전투 이동 모드: 현재 유닛·다른 아군·적 호버 → 이동 범위 위에 공격 범위.
3. 한 번 이동한 뒤: 호버 범위가 뜨고, 복귀 버튼 상태도 정상.
4. 공격 모드: 들어가면 호버 범위가 사라지고 공격 범위만, 나오면 마우스를 움직이지 않아도 마지막 커서 위치로 다시.
5. 유닛 Drag(이동 지정) 중 사라짐, 취소 뒤 다시.
6. 결과 재생 중 안 보임, 끝나면 마우스 아래 유닛의 새 범위.
7. 적 차례·턴 종료 대기 중 안 보임.
8. 기본 공격이 불가능한 유닛은 표시 없음.
9. 클릭 UnitInfo는 그대로.
10. GridOutline 모드에서 호버 범위 표시.
11. Unity Test Runner: §4.1·§4.2의 Unity 전용·NATIVE 시험과 T18에서 고친 시험.

## 7. 실행 기록

### 7.1 U-0 (2026-10-19, 완료)

- 기준: HEAD `927962fb`, 작업 트리의 코드 변경 없음(다른 세션의 문서·에셋 변경만: `CombatPresentationSettings_Default.asset`·FMOD 캐시·`Unit_MainCharacter.asset`·계획/설계 문서). csproj Compile 목록은 디스크와 모두 일치(Systems·Core 세 어셈블리·두 시험 어셈블리).
- 도구: 앞 세션(MR-1)의 관리 실행기·실행 스크립트를 이 세션 스크래치로 옮기고, 빌드가 `ProjectL.Tests.Core`·`ProjectL.Tests.Framework` 둘을 같은 출력 폴더로 만들게 고쳤다. 시험 하나당 Mono 프로세스 하나, 6개 병렬, 60초 제한, 순번(`@n`) 실행.
- MSBuild: 오류 0, 경고 40종(기존).
- 관리 실행 기준선(전체 8,956개):

| 어셈블리 | 전체 | PASS | FAIL | NATIVE | UNSUPPORTED |
|---|---|---|---|---|---|
| `ProjectL.Tests.Core` | 3,237 | 3,227 | 0 | 0 | 10 |
| `ProjectL.Tests.Framework` | 5,719 | 3,260 | 94 | 2,316 | 49 |

- 기존 FAIL 94의 클래스별 수: `ChapterContinuationServiceTests` 35, `CombatEntryIntegrationTests` 15, `CombatProgressIntegrationTests` 9, `StageEnvironmentAdapterTests` 8, `TemporaryCoreChapterContinuationTests` 5, `MovementPublicationTests`·`LobbyDomainStopTests`·`CombatInputAssemblyTests` 각 3, `TemporaryCoreRuntimeTests`·`CombatServiceCompositionTests` 각 2, 그 밖 9개 클래스 각 1(`ShopStageServiceTests`·`ScreenTransitionInitializationTests`·`FrenzyViewContractTests`·`EnvironmentEntryPublicationTests`·`CombatItemEquipmentServiceTests`·`CombatCommonPublicationTests`·`CombatCommandPublicationTests`·`CharacterStatsQueryServiceTests`·`BossCombatIntegrationTests`). 이 작업 범위 밖이며 비교는 시험 이름 단위로 한다.

### 7.2 U-1 (2026-10-19, 코드 완료)

- 코드(T3~T8): 새 `UnitAttackRangeResult`(+ `.meta`), `CombatPreviewResult.UnitRange`·`ForUnitAttackRange`, `CombatAttackQueries`의 `UnitReady`·`RangeCells` 분리와 `UnitRange`, `CombatCommandCalculator.GetUnitAttackRange`, `CombatPreviewService` 종류 switch 두 곳, Buffer `CombatViewPreviewKind.UnitAttackRange = 8`·`CombatViewPreviewData.UnitAttackRange`. `Actor`의 검사 순서·실패 이유와 `Range`의 결과는 그대로다. 화면 동작 변화 없음(아직 요청하는 View가 없음).
- 시험(T9):
  - 새 `UnitAttackRangeQueryTests`(Core, + `.meta`) 12개: 현재 행위자·적(자기 차례의 `GetAttackRange`)과 칸이 같음, 차례가 아닌 아군·배치 국면 성공, 손패·사망 `NotInCombat`, 없는 ID `EntityNotFound`, 기본 공격 불가 `BasicAttackDisabled`, 끝난 전투 `StageEnded`, 적만 아는 모래폭풍은 사거리를 줄이지 않고 플레이어도 아는 모래폭풍은 줄임, 결과 복사·불일치 거절, 응답 내용 하나.
  - `CombatPreviewServiceTests` 2개(새 종류가 배치로 떨어지지 않고 `UnitRange`로 답함·상태 무변경, 없는 유닛은 실패 응답 게시), `CombatViewBufferTests` 1개 + 기존 거절 시험에 한 줄.
  - 첫 실행에서 새 시험 하나가 시험 자료 조립에서 실패했다(자동 진행 입력은 명령 번호 0이어야 함). 시험 도우미만 고쳐 다시 돌렸다.
- 검증:
  - MSBuild 오류 0, 경고는 기준선 40종과 같음(새 경고 0). 새 형식이 산출 DLL에 들어감.
  - Unity가 08:11에 csproj를 다시 만들고 컴파일·어셈블리 재로드에 성공(Editor.log). 사용자도 컴파일 문제 없음을 확인했다. Unity Test Runner 결과는 아니다.
  - 관리 실행 Core 전체: 3,249개 = 기준 3,237 + 새 12. 기존 시험 상태 변화 0, 새 12개 PASS(도우미 수정 뒤 다시 돌림).
  - 관리 실행 Framework: 사용자 결정("컴파일 문제 없는 거 확인했으니 그냥 끝내")으로 전체 실행을 중간에 멈췄다. 멈추기 전 5,723개 중 4,837개 결과에서 기존 시험 상태 변화 0, 새 3개 PASS. 남은 886개는 이번 변경과 무관한 파일 위주지만 확인하지 않았다.
- 기록: 데이터 경계 문서 §5.46(미전달).
- 커밋 때 새 파일 `UnitAttackRangeResult.cs`·`UnitAttackRangeQueryTests.cs`를 `.meta`와 함께.

### 7.3 U-2 (2026-10-19, 코드 완료, Unity 확인 대기)

- 착수 때 사실: HEAD는 그대로 `927962fb`이고 U-1 파일은 커밋되지 않은 채였다(사용자는 커밋했다고 알림, 이 폴더의 git에는 없음). U-2는 U-1과 다른 파일만 고쳤다. 같은 작업 트리에서 다른 세션이 조각 실행 대여 분리(`CombatPieceRun`·`CombatPieceContext`·`CombatComposition`·`CombatCameraPieceTests`, 새 `CombatPieceRunLeases`)를 진행 중이라, 검증 빌드는 그 파일들을 HEAD 판으로 고정하고 새 파일을 뺐다(스크래치 `u2.targets`).
- 코드:
  - T11 `CombatInputPolicy`: `CanPreview`가 국면 무관 종류(`UnitAttackRange`)를 지금 국면으로 판단, 새 `CanInspectUnit`(+ 재생 중 아님), `StageOf` 문서 주석.
  - T12 새 `CombatPreviewSlotRule`(`IsSettled` R-b·`IsIdle` R-a·`ShouldRequestBackground`). `CombatReturnRequest.IsAnswered`·`MarkAnswered`(`Begin`·`Reset`이 지움). `CombatPathInput`: `RangeMatches`가 `IsSettled`를 부름, 범위의 복귀 원함 대기 → `IsIdle`, 복귀 견적의 슬롯 소유·범위 대기 → `IsSettled`(답 받음 포함)·`IsIdle`, `HandleReturnPreview`가 `MarkAnswered`.
  - T13 새 `CombatUnitRangeHover`(225줄).
  - T14 `CombatInteraction`: 끝 선택 인자 `rangeHover`, `IsInputOwnerActive`(커서 스냅이 같은 식을 씀), 포인터 이동·누름·손패/아이템 집기·공격 진입(공격 `Enter` 전)·응답(공격 → 호버 → 배치)·결과 전·표현 정리(`Suspend`)·강제 선택 종료·입력 취소·정지(`Clear`), 새 `Poll(preview, facts, characters)` 오버로드(경로 다음에 호버).
  - T15 조정자 `CombatViewSystem` Tick 한 줄이 새 `Poll` 오버로드를 부름.
  - T16 `CombatSceneInputFactory.CreateSceneInteraction`이 호버를 만들고 `CreateInteraction`의 새 끝 선택 인자로 넘김.
- 계획과 다른 점(작은 구현 형태):
  - 계획 T15는 "`Poll` 호출에 사실·캐릭터를 넘긴다"였다. 다른 작업자 시험(`HojinCombatAttackDrag*`)·`CombatInputRouterTests`가 한 인자 `Poll(preview)`를 쓰고 있어, 그 서명은 경로만 맞추는 판으로 남기고 사실을 받는 오버로드를 더했다. 동작은 계획과 같다.
  - 계획 §6 4번(공격 모드를 끈 뒤 "마우스를 움직였을 때 다시")은 구현이 마지막 포인터로 바로 다시 판단하게 되어 "움직이지 않아도 다시"로 고쳤다.
  - 상호작용 시험은 `CombatInteractionTests`(1,308줄)에 더하지 않고 새 `CombatUnitRangeHoverTests`에 모았다(호버 없는 조립이 지금과 같은지도 같은 파일에서 본다).
- 시험(T17):
  - 새 `CombatPreviewSlotRuleTests` 8개, 새 `CombatUnitRangeHoverTests` 17개(유닛이 바뀔 때만 요청·자기 응답 뒤 슬롯 비움·빈 칸/UI 위/필드 밖에서 한 번 지움·실패 응답은 그리지 않고 다시 묻지 않음·남의 응답 무시·답 안 된 남의 요청은 기다렸다 요청·답 받은 남의 요청이면 바로·자기 요청을 잃으면 덮지 않고 한가할 때 다시·`Suspend`·`Clear`·허용 아님·공격 진입이 공격 범위보다 먼저 호버를 지우고 끄면 다시·호버 없는 조립은 지금과 같음).
  - `CombatInputPolicyTests` 2개, `CombatReturnRequestTests` 1개, `CombatMoveModeRangeTests` 2개(복귀 견적 R-b·R-a, 범위 → 복귀 견적 → 호버 수렴 — 호버 응답은 실제 Core 조회).
- T18 기존 시험 영향 조사: 씬 조립을 쓰는 Unity 시험(`CombatViewSystemTests`·`CombatMovementViewTests`·`CombatDisplayReuseTests` 등)은 포인터를 누름(Drag 시작) 뒤나 공격 모드에서만 움직여 호버가 먼저 슬롯을 잡지 않는다. 이동 뒤 미리보기 왕복 수를 세는 `CombatDisplayReuseTests.AuthoredCombatScene_DragPlaybackAndReturn_UseActualPresenters`·`CombatMovementViewTests.CancelledDraft_RestoresReturnPreview`는 흐름을 따라가 보니 호버 요청이 복귀 견적 뒤에 끼고 복귀 버튼(전경 요청)이 그것을 덮어 기대가 그대로다. 고친 기존 시험은 없다. 두 시험은 Unity 전용이라 Unity 결과로 확인한다.
- 검증:
  - MSBuild 오류 0, 경고는 기준선 40종과 같음(새 경고 0). 새 파일이 csproj에 이미 들어갔는지와 관계없이 한 번만 컴파일되게 했다.
  - 관리 실행 관련 클래스 묶음(14개 클래스, 343개): PASS 342·UNSUPPORTED 1(`HojinCombatAttackDragIntegrationTests.Boot_…`, 기준선도 UNSUPPORTED).
  - 관리 실행 Framework 전체: 5,752개 = 기준 5,719 + 새 33. 기존 시험 상태 변화 0(FAIL 94는 기준선과 같은 기존 실패), 새 33개 PASS. U-1 때 확인하지 못한 886개도 이번에 함께 확인됐다.
  - 관리 실행 Core 전체: 3,249개 = 기준 3,237 + U-1 새 12. 기존 시험 상태 변화 0, PASS 3,239·UNSUPPORTED 10(기준선과 같음).
  - 두 어셈블리 합계 9,001개, 기준선(HEAD `927962fb`, 8,956개) 대비 기존 시험 상태 변화 0, 새 45개(U-1 15·U-2 30) 모두 PASS. Unity Test Runner·재생 결과는 아니다(§6).
- 공유: 민형님 Factory 공유 문서 §17(미전달). 폴더 정리 계획 §3 `Input/Interaction/` 줄, `viewDocs/stage-input-guide.md` §2.2 7번.
- 커밋 때 새 파일 `CombatPreviewSlotRule.cs`·`CombatUnitRangeHover.cs`·`CombatPreviewSlotRuleTests.cs`·`CombatUnitRangeHoverTests.cs`를 `.meta`와 함께(U-1 새 파일 둘도 아직 커밋 전). 다른 세션의 대여 분리 파일은 이 작업이 아니다.

### 7.4 U-2 뒤 — 적·아군 범위 색 나누기 (2026-10-19, 완료 — Unity 확인 문제 없음)

- 사용자: U-2 Unity 확인 "문제 없음", 이어서 "AttackRangeColor 적이랑 아군이랑 색 다르게". 설계 Q-UR3을 A에서 B로 바꿈(설계 §4 결정 줄). 구현은 설계에 적어 둔 B 그대로다.
- 코드:
  - 강조 층 `FieldHighlightLayer`를 byte → ushort로 넓히고 `EnemyAttackRange = 1 << 8`, 색 종류 `FieldHighlightColor.EnemyAttackRange`, 그리기 순서는 공격 범위 바로 위, `ColorOf`, 행동 중 층(`FieldHighlightState.ActingLayers`)에 더함.
  - 팔레트 `FieldHighlightPalette`에 `EnemyAttackRange`(생성자 인자, `attackRange` 다음), `FieldView` 씬 값 `enemyAttackRangeColor`(기본 `(1, 0.45, 0.35)`, 툴팁). 기존 씬은 값이 없어 이 기본값을 쓴다 — 바꾸려면 `Combat.unity`의 FieldView "Enemy Attack Range Color".
  - GridOutline 모드: 채우기 종류 `EnemyAttackRange`(호버 > 영향 > 적 공격 범위 > 공격 범위 > 이동 범위), 테두리는 그리기 순서에서 슬롯이 하나 생김.
  - 좁은 표시 계약 새 `ICombatUnitRangeFeedback`(`ShowUnitRange(cells, enemy)`·`ClearUnitRange`), `ICombatBattleDisplay`가 상속. `CombatSceneDisplay` → `CombatAttackFeedback.ShowUnitRange`(아군 = 공격 범위 층, 적 = 적 공격 범위 층, 지난번 층이 다르면 지움)·`ClearUnitRange`(그린 층만 지움).
  - `CombatUnitRangeHover`는 `ICombatInteractionFeedback` 대신 이 계약을 받고, 집을 때 유닛 소유자로 적 여부를 기억해 그린다. Factory 코드는 그대로(같은 `display`를 넘김).
  - `ICombatInteractionFeedback`에는 멤버를 더하지 않았다(다른 작업자 시험 가짜 `HojinCombatAttackDragTests`를 건드리지 않으려고).
- 시험(돌리지 않음 — 사용자 "엔진 테스트 내가 할 테니 따로 돌리지 마"):
  - 고친 기존 시험: `FieldOutlineRendererTests`(테두리 핸들 10 → 11·20 → 22, 채우기 이름 목록, 팔레트 생성자), `CombatObjectiveDisplayTests`(팔레트 생성자), `CombatViewSystemTests.FakeDisplay`(새 계약 두 멤버, 하는 일 없음).
  - 새 사례: `FieldOutlineRendererTests.EnemyAttackRange_FillsAndOutlinesWithEnemyColor_OverMoveRange`, `CombatUnitRangeHoverTests.EnemyUnit_UsesEnemyRangeColor_AllyDoesNot`. 호버 시험 가짜 `RecordingFeedback`는 두 계약을 구현(호버 범위·공격 모드 범위 카운터를 나눔).
- 검증: MSBuild 오류 0. 경고 목록은 기준선과 같다(`FieldOutlineRenderer`의 기존 CS0162 하나가 줄 번호만 434 → 441). 관리 실행·Unity Test Runner는 사용자 확인.
- Unity 확인(사용자): 적 유닛 호버 = 적 색, 아군 유닛 호버·공격 모드 = 지금 공격 범위 색, GridOutline 모드 채우기·테두리, Test Runner `FieldOutlineRendererTests`·`CombatUnitRangeHoverTests`·`CombatObjectiveDisplayTests`. **2026-10-19 사용자 확인: 범위 색·시험 문제 없음.**
- 커밋 때 새 파일 `ICombatUnitRangeFeedback.cs`를 `.meta`와 함께(U-1·U-2 새 파일도 아직 커밋 전).
