# 전투 View 선택 인자 나머지 Task 계획 (OA-0~OA-b)

- 날짜: 2026-11-06
- 상태: **OA-0 완료(11-06, 코드 변경 없음, §7.1). OA-a 코드 완료(11-06 사용자 "진행해", 컴파일 미확인, 새 시험 없음, 시험 변경 = 우리 시험 호출 한 곳, Unity 확인 §4 대기, §7.2). OA-b 완료(11-07 사용자 "다음 진행", 묶음 끝 검토 높음·중간 0, 낮음 L3 하나 결정 대기, §7.3).** 결정(11-06 사용자 "권장안대로 진행"): Q-OA1 A(입력 사실 인자 28개 모두 필수), Q-OA2 A(코드 Phase 하나 + 문서 Phase, Unity 확인 한 번), Q-OA3 B(평가가 꼽지 않은 같은 성격 기본값은 이번에 걷지 않음). 설계에서 정한 P1~P13은 쓴 그대로 간다. 실행 기록은 §7.
- 설계: [optional-args-design](../specs/2026-11-06-combat-view-optional-args-design.md)(OA1~OA5, 독립 검토 반영판). 줄 번호는 HEAD `b3ba1768` 기준이다.
- 대상 문제: C5-02(중간, 불필요/우회) 2단계. 1단계는 평가 후속 3차-a(`fd8b0d2f`).
- 다음 묶음: ② 씬 표시 나누기(C2-03), ③ 기믹 공통 계약·조립 옮기기(C2-01·C4-02, Q-S3 결정 필요). 사용자 "1,2,3 대로 진행"(11-06).
- 규칙:
  - View 작업이라 새 시험을 쓰지 않고 MSBuild도 돌리지 않는다. 보고에는 "컴파일 미확인"을 적는다.
  - 시험은 컴파일이 깨지는 우리 시험 호출 한 곳(`StructureHitPresentationTests.cs:168`)만 맞추고 단언은 그대로 둔다.
  - 동작은 같아야 한다. 설계 §4·§5를 벗어나는 결정이 필요하면 구현하지 않고 먼저 묻는다.
  - Phase가 끝나면 멈추고 보고한다. 묶음 끝(OA-b)에 검토 에이전트 하나를 돌린다.
  - 커밋·push는 사용자가 한다.

## 0. 계획에서 정한 것 (사용자 확인 대상)

| 번호 | 정한 것 | 이유 |
|---|---|---|
| TP-1 | `CombatInteraction` 생성자는 네 협력자(`path`·`attackDrag`·`item`·`rangeHover`)를 넘겨받기를 만들기 **전에** 하나씩 `ArgumentNullException`으로 막는다. 인자 순서·이름은 그대로다 | 넘겨받기 생성자도 `target`을 막지만(P11), 빠진 쪽을 `CombatInteraction`의 인자 이름으로 알려야 원인이 바로 보인다 |
| TP-2 | 소유자 목록은 `new[] { … }` 고정 배열 여섯으로 만든다. `_byTarget`의 중복 대상 검사는 그대로 둔다 | 순서(TP-2 원본: 넘겨받기 → 경로 → 아이템 → 배치 → 공격 → 호버)를 한 줄에 보이게 한다. 중복 검사는 같은 대상 둘을 막는 별개 규칙(P10)이다 |
| TP-3 | `CombatInputFacts` 서명은 기본값만 지운다. 한 줄에 인자 둘이 있는 줄(39·44)과 인자 순서는 그대로 둔다 | 경계 커밋·다른 작업자 줄의 변경을 기본값 글자로 줄인다 |
| TP-4 | 시험 호출은 서명 순서대로 이름 인자 28개를 한 줄에 하나씩 적는다 | 값 대조(옛 기본값)를 줄마다 볼 수 있게 한다 |
| TP-5 | 확인창 둘은 여는 곳의 `InvalidOperationException` 갈래를 지우고, 생성자가 `ArgumentNullException`으로 막는다(예외 종류가 바뀐다) | 같은 사실을 생성 순간에 막는다. 운영에서는 둘 다 나지 않는다 |
| TP-6 | 고칠 주석은 바꾼 사실을 적는 줄만 고친다: `CombatInteraction` remarks 32, 필드 주석 54·56·58·64, 인자 주석 71-74, 192·207 문서, `OwnerOf` 문서(246), 두 `Poll` 문서, 넘겨받기 인자 주석(28), `CombatInputSurfaces.BindRoot` 문서(32) | 낡게 되는 주석만 고친다(검토 L4). 표면 remarks 13("루트를 묶기 전·푼 뒤에는 … 건너뜁니다")은 `_root?.`가 남아 여전히 맞다 |

## 1. OA-0 착수 전 확인 (코드 변경 없음)

| Task | 내용 |
|---|---|
| T0.1 | 원격 브랜치와 HEAD가 같은지, 바꿀 파일이 원격에서 바뀌지 않았는지 본다 |
| T0.2 | 경계 커밋 줄(`^83fb2767`·`^4782921e`·`^8ed3f8cb`, 설계 §1.7)의 실제 작성자를 GitHub 경로 이력(옛 경로 포함)으로 찾는다 |
| T0.3 | 설계가 "다시 본다"고 적은 줄(경로 입력 398, 넘겨받기 33·42, `PlacementDragInteraction` 103-105)의 blame을 본다 |
| T0.4 | `Assets/Tests`에서 바꿀 형식(생성자·`Sync`·`Read`·`Cancel`·`Poll`·`CombatInputFacts`)을 쓰는 곳을 다시 grep한다. 168 하나뿐인지 본다 |
| T0.5 | 결과로 공유 대상(설계 §10)을 고친다. 계획과 다른 사실이 나오면 멈추고 묻는다 |

## 2. OA-a 코드 (설계 OA1~OA5)

| Task | 파일 | 할 일 |
|---|---|---|
| Ta.1 | `Input/MovePath/CombatPathInput.cs` | 생성자 `rejections`·`actionAvailability` 기본값 지우고 `?? throw`(92-93). 153·261·649 `_actionAvailability?.` → `.`, 264 `_rejections?.` → `.`. 82 `actor != null && actor.Anchor.HasValue` → `actor != null`. 398 `actor.Rotation ?? Rotation4.R0` → `actor.Rotation.Value`. 154·262 `availability != null`은 그대로(P3) |
| Ta.2 | `Input/MovePath/CombatMovementInteraction.cs` | 생성자 `rejections` 기본값 지우고 `?? throw`. 249·282 `_rejections?.` → `.` |
| Ta.3 | `Input/Interaction/CombatAttackDragHandover.cs` | 생성자 `target ?? throw`, `IsTargetAttached => !_target.IsDetached`, 인자 주석(28) 고침(P11) |
| Ta.4 | `Input/Interaction/CombatInteraction.cs` | 생성자 네 인자 필수(TP-1)·소유자 고정 배열(TP-2)·`_hasBackground` 필드와 검사 셋(217·235·386) 삭제. `_item` 갈래(113·179·190·194), `_handover` 갈래(210·285·300·319) 삭제. `_path` 갈래 셋(137·270·301)은 그대로(P1). 두 `Poll` private(P9). `Cancel`·`CancelDragOrAttack` 기본값 삭제. 주석(TP-6) |
| Ta.5 | `Input/Interaction/AttackTargetingInteraction.cs` | `Cancel(characters = null)` 기본값과 안쪽 `characters == null ||`(178) 삭제 |
| Ta.6 | `Input/Interaction/PlacementDragInteraction.cs` | `FindDeckUnit`의 `characters == null ||`(105) 삭제(`ownedId.IsEmpty` 검사는 남김) |
| Ta.7 | `Input/ActionButtons/CombatDeploymentConfirmation.cs`, `CombatEndTurnConfirmation.cs` | 생성자 `presentation ?? throw`, `IsOpen`·`Close`의 `?.`·`?? false` 삭제, 여는 곳의 null 갈래 삭제(TP-5) |
| Ta.8 | `Input/ActionButtons/CombatActionButtons.cs` | 생성자·`Read`의 `canUseReturn = false` 기본값 삭제 |
| Ta.9 | `Input/ActionButtons/CombatInputSurfaces.cs` | `Sync`의 `canUseReturn`·`availability` 기본값 삭제(값 null은 받음, 문서에 "null = 응답 전"). 안쪽 `BattleChanger?.`·`Pack?.`·`UnitInfo?.` → `.`(39·47-49·61-63·73-75), `_root?.`는 그대로. `BindRoot`의 null 반환 → `ArgumentNullException`(P12), 문서 고침 |
| Ta.10 | `Input/CombatInputFacts.cs` | 생성자 기본값 23개 삭제(TP-3) |
| Ta.11 | `Playback/CombatSceneEventPresenter.cs` | 117(원격 `7279a9d5` 기준, 설계 판 119) 두 번째 `cueSources` null 검사 삭제 |
| Ta.12 | `Assets/Tests/Editor/CombatField/StructureHitPresentationTests.cs` | 168 호출을 이름 인자 28개로(TP-4). 값: 넘기던 넷 그대로, 나머지는 옛 기본값(`default`·`null`·`false`·`0`) |
| Ta.13 | 자체 대조 | 지운 `?.`·갈래 수를 설계 §1과 맞춘다. 시험 값 표(아래)를 줄마다 대조한다. `grep "= null\b"`·`"?\."`로 남은 것이 의도한 것뿐인지 본다 |

- Ta.12 값 표(옛 기본값): `currentLayer` = `CombatInputPolicy.LayerOf(stage)`(넘기던 값), `latestResultSequence` 0, `presentedResultSequence` 0, `isFinalizing` false, `selectedActorId` default, `hasUnhandledCommand` false, `previewAnswer` null, `hasPendingCommand` = pending(넘기던 값), `isPresenting` false, `isInputAvailable` true(넘기던 값), `isAttackActive` false, `combatInputRestriction` default(= None), `hasCurrentAcceptedEmptyMovementRange` false, `battleInstanceId` 0, `controllerTurnSequence` 0, `isSubmissionSuspended` false, `isAbandonRequested` false, `isModalOpen` = modal(넘기던 값), `isItemDragActive` false, `isTutorialPracticeGoalPending` false, `queryRevision` 0, `isEntryPresentationPending` false, `pointerTarget` default(= None). 필수였던 다섯도 이름 인자로 쓴다(`isBattleDisplayed: true, hasBattle: true, phase: phase, stagePhase: stage, hasCompletion: false`).

## 3. OA-b 문서·공유·묶음 끝 검토

| Task | 내용 |
|---|---|
| Tb.1 | 묶음 끝 검토 에이전트 하나(정적, 컴파일 오류·동작 차이·남은 갈래). 높음·중간은 반영, 낮음은 보고 |
| Tb.2 | 공유 메모(미전달): 설계 §10 — 현강님·호진님·천명님 새 절(OA-0 작성자 표 §1.7 기준), 민형님 없음 |
| Tb.3 | 설계 §13 구현 메모, 이 계획 §7, 후속 계획 §6.6 줄, 평가 아티팩트 '적용 기록' 9차, CLAUDE.md |

## 4. Unity 확인 (사용자, OA-a 뒤 한 번)

설계 §6 Ua-1~Ua-8과 같다.

1. 컴파일, Boot에서 전투 진입(배치·전투 국면) 때 F1(`ArgumentNullException`) 없음.
2. 이동 AP가 없는 유닛을 끌어 이동하려 할 때 거절 안내.
3. 이동한 유닛으로 공격 모드에 들어가 자기 칸을 끌 때 공격 모드 유지. 이동 전 유닛은 이동으로 넘어가고, 이동 지정 시작 때 고스트 방향이 유닛 방향과 같음.
4. 배치 국면 미배치 확인창의 열기·Y·N·닫기.
5. 턴 종료 확인창의 열기·Y·N·닫기.
6. 가방 아이템 끌기·우클릭 해제·끌기 취소(우클릭).
7. 유닛 호버 범위, 공격 모드 자기 칸 끌기의 이동 넘겨받기.
8. 행동 버튼(공격·턴 종료·복귀), UnitInfo 행동 가능 표시, BattleChanger·가방 표시 갱신·숨김. EditMode `StructureHitPresentationTests.InspectionPolicy_DeploymentKeepsPendingAndModalBoundaries` 다섯 사례 통과.

## 5. 바뀌는 파일과 커밋 묶음 (사용자)

- 코드 열둘(Ta.1~Ta.11)과 시험 하나(Ta.12)는 한 커밋이다. 생성자 서명이 바뀌어 나누면 컴파일이 깨질 수 있다(특히 `CombatInputFacts`와 시험).
- 새 파일·`.meta`·에셋 변경은 없다.
- 문서(설계·계획·후속 계획·CLAUDE.md·공유 메모)는 같은 커밋이나 따로.

## 6. 위험과 되돌리기

- 컴파일 오류가 나면 대개 서명 한 곳(호출이 인자를 덜 넘김)이다. 운영 호출은 모두 값을 넘기므로 남는 곳은 시험 호출이다.
- 동작 차이가 보이면 그 파일만 되돌린다. 파일 사이 의존은 `CombatInteraction`(Ta.4) ↔ 넘겨받기(Ta.3) 하나다.

## 7. 실행 기록

### 7.1 OA-0 착수 전 확인 (2026-11-06, 코드 변경 없음)

- T0.1 원격: 원격이 16커밋 앞서 있었다(`7279a9d5`까지, 용근 "유닛 커서 개선"·"손패 카드 spread"·"적 본부 증원" 등, 민형님 랙돌·IK 병합 포함). 로컬 문서 변경을 stash에 넣고 빨리 감기로 받은 뒤 다시 얹었다(CLAUDE.md 한 곳 충돌 — 원격의 스테이지 선택 줄과 우리 새 줄이 붙어 있어 둘 다 살림). **기준 판은 `7279a9d5`로 바뀐다.**
  - 바꿀 파일 가운데 원격에서 바뀐 것은 `CombatSceneEventPresenter.cs`(선택 강조 출처 삭제·증원 처리기·적 턴 시작 인자) 하나다. 고칠 이중 검사는 그대로 있고 줄만 119 → **117**로 옮겼다. 나머지 열한 파일과 시험은 원격에서 바뀌지 않아 설계·계획의 줄 번호가 그대로 맞다.
  - `CombatSceneInputFactory.cs`는 원격에서 바뀌었지만(본부 증원 호버 연결, 50-57) 이 묶음이 고치는 곳이 아니다. 경로 입력·입력 기능 묶음 생성 줄은 152·163으로 옮겼다(값은 그대로 모두 넘김).
- T0.3 blame: 경로 입력 398은 `^83fb2767` 경계 줄이다(T0.2 대상에 더함). 넘겨받기 28-42는 `35562e96`(용근, 10-25 3라운드)이다. `PlacementDragInteraction` 103-105는 `^8ed3f8cb` 경계 줄이다(T0.2 대상).
- T0.4 시험 grep: `Assets/Tests`에서 바꿀 형식을 쓰는 곳은 `StructureHitPresentationTests.cs:168` 하나다. `ArtifactDragController`의 `_interaction.Cancel()`은 다른 형식(`ArtifactInteraction`)이다.
- T0.2 경계 커밋 줄 작성자: 새 에이전트 하나가 원격 전체 이력을 스크래치패드에 blobless로 복제해 `git blame -w`·`git log -L`(옛 평면 경로까지 이름 바꾸기 추적, 쉼표·공백만 바꾼 커밋은 뺌)으로 셌다. 사용자 저장소는 건드리지 않았다. 결과는 설계 §1.7 표로 옮겼다.
  - 설계 초안 때 본 것보다 다른 작업자 줄이 많다. 현강님: 배치 확인창 파일 전체(`fc133bf7`), 거절 창구 기본값·쓰는 곳(`80eb7481`), `FindDeckUnit`(`4dc3ba8a`), `CombatInteraction.Cancel` 기본값(`1b155e02`). 호진님: `attackDrag` 기본값(`32654528`), 경로 입력 82·입력 사실 37·38·행동 버튼 58과 `canUseReturn` 인자(`34ee0de4`), 표면(`8339e308`). 천명님: 경로 입력·이동 상호작용 생성자 서명(`e0b02cc7`·`87348b86`), 입력 사실 39·행동 버튼 기본값 `= false`(`d4a6d6fd`), 턴 종료 확인창(`57af3244`).
  - 영역 규칙(작성자와 무관하게 전투 View 구조는 개선 대상, 고치면 변경 정리를 넘김)대로 계획은 그대로 간다. 공유는 설계 §10(현강님·호진님·천명님 새 절, 민형님 없음).
  - `57af3244`는 천명님 계정 커밋이지만 우리 행동 버튼 설계 §8·CLAUDE.md도 함께 고쳤다. 우리 세션 변경을 그 계정으로 커밋했을 가능성이 있으나 확인 못 함 — 조정자 나누기 CS-a 선례대로 천명님 줄로 센다.
- T0.5 결과: 계획과 다른 사실은 없다(기준 판 `7279a9d5`, Presenter 줄 117, 공유 대상 늘어남). OA-a를 계획대로 할 수 있다.

### 7.2 OA-a 코드 (2026-11-06, 컴파일 미확인, 새 시험 없음)

사용자 "진행해". 기준 판 `7279a9d5`(원격과 같음, 착수 직전 다시 확인). 계획 §2 Ta.1~Ta.13대로 했고 계획과 다른 점은 없다.

| Task | 한 것 |
|---|---|
| Ta.1 경로 입력 | 생성자 `rejections`·`actionAvailability` 필수(`?? throw`), `?.` 넷 삭제(153·261·264·649). 82 Anchor 재확인 삭제(주석에 근거), 398 `actor.Rotation.Value`. 응답 전 `availability != null` 둘은 그대로(P3) |
| Ta.2 이동 상호작용 | 생성자 `rejections` 필수, `?.` 둘 삭제 |
| Ta.3 넘겨받기 | `target` 필수, `IsTargetAttached => !_target.IsDetached`, 필드·인자 주석 |
| Ta.4 입력 기능 묶음 | 네 협력자 필수(넘겨받기를 만들기 전에 이름으로 막음, TP-1), 소유자 고정 배열(TP-2), `_hasBackground`와 검사 셋 삭제, `_item`·`_handover` 갈래 여덟 삭제, `_path` 갈래 셋은 그대로(P1, 필드 주석에 까닭), 두 `Poll` private, `Cancel`·`CancelDragOrAttack` 기본값 삭제, 낡은 주석(remarks·필드·인자·`TryHandleAttackDragPointer`·`RequestItemUnequip`·`OwnerOf`·두 `Poll`) |
| Ta.5 공격 조준 | `Cancel(characters)` 기본값과 안쪽 null 검사 삭제 |
| Ta.6 배치 Drag | `FindDeckUnit`의 `characters == null ||` 삭제 |
| Ta.7 확인창 둘 | `presentation` 필수, `IsOpen`·`Close`의 `?.`, 여는 곳의 `InvalidOperationException` 갈래 삭제(TP-5) |
| Ta.8 행동 버튼 | 생성자·`Read`의 `canUseReturn` 기본값 삭제 |
| Ta.9 입력 표면 | `Sync` 기본값 둘 삭제(`availability` 문서에 "응답 전이면 null"), 안쪽 `BattleChanger?.`·`Pack?.`·`UnitInfo?.` → `.`(`_root?.`는 그대로), `BindRoot` null 반환 → `ArgumentNullException`(P12)·문서 |
| Ta.10 입력 사실 | 생성자 기본값 23개 삭제(서명 순서·줄 묶음 그대로, TP-3), remarks에 "인자는 모두 필수" 한 줄 |
| Ta.11 Presenter | 117 두 번째 `cueSources` 검사 삭제 |
| Ta.12 시험 | `StructureHitPresentationTests.cs:168` 이름 인자 28개(TP-4). 넘기던 넷은 그대로, 나머지는 옛 기본값(열거·구조체는 `default`, 참조는 `null`, using 추가 없음). 단언·`[TestCase]` 무변경 |

- Ta.13 자체 대조:
  - 시험 호출의 이름 28개가 생성자 서명과 같은 이름·같은 순서인지 스크립트로 맞춰 봤다(일치). 운영 읽기(`CombatInputFactsReader.Read`)는 위치 12 + 이름 16 = 28로 모두 넘긴다.
  - 바꾼 서명의 호출처를 다시 grep했다: 경로 입력·묶음은 Factory 152·163, 이동 상호작용은 경로 입력 103, 넘겨받기는 묶음 하나, 확인창은 진입 객체 둘, `Cancel`·`CancelDragOrAttack`은 Router 둘, `Surfaces.Sync`는 입력 분배 176, `Read`는 표면 67, `BindRoot`는 연결 객체 113. 모두 값을 넘긴다.
  - 남은 `?.`·null 검사는 모두 정말 없을 수 있는 값이다(정지 뒤 `_path`, 대상 None, 응답 전 가용성·미리보기, 루트 묶기 전, 참가 목록 없음 등). Q-OA3 B로 남긴 기본값(`Begin(deferPreview = false)` 등)은 손대지 않았다.
  - 바꾼 파일 13개 모두 LF, `git diff --check` 문제 없음.
- 바뀐 파일: 코드 12(`CombatPathInput`·`CombatMovementInteraction`·`CombatAttackDragHandover`·`CombatInteraction`·`AttackTargetingInteraction`·`PlacementDragInteraction`·`CombatDeploymentConfirmation`·`CombatEndTurnConfirmation`·`CombatActionButtons`·`CombatInputSurfaces`·`CombatInputFacts`·`CombatSceneEventPresenter`) + 시험 1. +147 −129줄. 새 파일·`.meta`·에셋 없음. Factory 무변경.
- 다른 작업자 줄을 고친 곳(설계 §1.7 표): 현강님(배치 확인창·거절 창구 기본값·`FindDeckUnit`·`Cancel` 기본값), 호진님(표면 줄·`attackDrag` 기본값·경로 입력 82·입력 사실 37·38·행동 버튼 `canUseReturn`), 천명님(경로 입력·이동 상호작용 생성자 서명·입력 사실 39·행동 버튼 기본값·턴 종료 확인창). 공유 메모는 OA-b에서 쓴다.
- Unity 확인은 §4 Ua-1~Ua-8.

### 7.3 OA-b 문서·공유·묶음 끝 검토 (2026-11-07)

사용자 "커밋 푸쉬는 나중에, 다음 진행". Unity 확인(§4)은 아직 받지 않았다.

- Tb.1 묶음 끝 검토: 새 에이전트 하나가 HEAD `7279a9d5` 대비 작업 트리를 읽었다(읽기 전용, 빌드 없음). 높음 0·중간 0·낮음 4. 맞다고 확인한 것은 설계 §13.
  - L1 `CombatAttackDragHandover.cs` 15·99의 "받는 쪽 없이 만든 조립"·"받는 쪽이 없거나" — 이번 변경으로 낡음, 고침(TP-6 목록이 놓친 줄).
  - L2 `CombatInputFacts.cs` `PointerTarget` 문서의 "생성자 기본값은 None" — 낡음, "`default(CombatInputFacts)`이면 None"으로 고침.
  - L4 우리 시험 168 주석 "넘기던 넷" — "정하던 값(앞 다섯 + 이름 인자 넷)"으로 고침. 시험 결과 무관.
  - L3 같은 성격의 `characters == null` 검사 넷이 남음: `PlacementDragInteraction.cs:222` `TryReturnUnderPointer`(이번에 바꾼 파일, 부르는 곳은 Router 136 ← Buffer 목록이라 null이 아님), `CombatUnitRangeHover.cs:182`, `CombatItemDragInteraction.cs:119`·`:155`(안 바꾼 파일). 설계 P5가 지울 대상을 `FindDeckUnit`·`AttackTargetingInteraction.Cancel` 둘로 한정해 빠뜨린 것은 아니다. 코드 변경이라 계획 밖이므로 고치지 않았다 — **사용자 결정 대기**(권장: 222만 이번에 함께 지움, 나머지 셋은 그 파일을 고칠 때). `CombatActionAvailabilityTracker.cs:58`은 계약대로 예외를 던지는 검사라 대상이 아니다.
  - 고친 뒤 코드 열둘 + 시험 하나 +150 −132줄, `git diff --check` 문제 없음.
- Tb.2 공유 메모(모두 미전달): 현강님 [메모 §26](../handoffs/2026-10-22-combat-view-structure-hyeongang-share.md)(배치 확인창·거절 창구·`FindDeckUnit`·`Cancel` 기본값), 호진님 [메모 §19](../handoffs/2026-10-22-combat-view-structure-hojin-share.md)(입력 표면·`attackDrag`·경로 입력 83·입력 사실 38-39·행동 버튼 `canUseReturn`), 천명님 [특성 공유 §9](../handoffs/2026-10-30-combat-trait-system-view-cheon-share.md)(생성자 서명 둘·입력 사실 40·행동 버튼 기본값·턴 종료 확인창, View가 기대는 Core 보장 참고, `57af3244` 작성자 질문). 민형님 없음(Factory 무변경). 줄 번호는 고친 뒤 기준이고 하나씩 대조했다.
- Tb.3: 설계 §13 구현 메모, 이 절, 후속 계획 §6.6 줄, 평가 아티팩트 '적용 기록' 9차(C5-02 다 풀림), CLAUDE.md.
- 커밋·push: 사용자 지시("지금까지 한거 푸쉬하고", 11-07)로 Claude가 코드 열둘·시험 하나·문서를 한 커밋으로 push했다.
- 남은 것: Unity 확인 §4(Ua-1~Ua-8), L3 결정, 공유 메모 전달. 다음 묶음은 ② 씬 표시 나누기(C2-03, 설계부터).
