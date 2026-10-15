# 전투 View — Core 전달 메모 (쓰임새 답변·확인 요청)

> **2026-10-05 갱신:** 6B에서 필요한 최신 Core 요청과 완료 조건은 [2026-10-05 Core 요청사항](2026-10-05-combat-core-requests-phase6b.md)이 원본이다. 이 문서의 날짜별 알림과 당시 미병합 상태는 이력이다. 외부 전달·답변은 아직 확인하지 않았다.

- 작성일: 2026-10-02
- 작성: View 작업(스테이지 내부 Combat 담당)
- 대상: Core 담당자(천명님)
- 기준: `feature/Yonggeun/Stage` HEAD `ede27fef`, Core 브랜치 `069327c3`(8R6) 문서 확인, [전투 View 설계 8차](../specs/2026-09-30-combat-view-design.md)
- 상태: **전달용 문서 준비 완료. 외부 전달·답변 없음.** 모두 급하지 않으며 View 작업을 막지 않는다. Core 규칙·코드 결정이 필요한 항목은 §3(시작 재화)과 §3a(시작 서브 수)이고, 둘 다 운영 전투 전까지 정하면 된다.
- 위치: 기존 [Core 전달 문서](2026-09-24-view-core-data-boundary-handoff.md) §13.7에서 이 파일을 가리킨다. 기존 요청 총정리에는 붙이지 않았다.

## 1. 답변 — 준비 인덱스·리비전은 당장 필요 없음

[Core 답변](2026-09-30-combat-core-response-to-view.md) §6("View가 필요한 판단과 사용 사례를 알려 주면 최소 계약을 정한다")에 대한 답이다.

- 준비/전투 단계는 `Current.CommandState.Turn.StagePhase`로 읽는다(설계 W13). 밤이 끝나 배치로 돌아오는 것도 이 값으로 판단한다.
- 다시 그릴 시점은 결과 순번(`LatestResultSequence` / `PresentedResultSequence`)으로 충분하다(W4).
- 따라서 **새 인덱스·리비전 계약은 필요 없다.** 8R6 문서의 R7 정리("현재 지원하는 턴 인덱스는 `Current.CommandState.Turn`, 리비전 추가 없음")와 같은 결론이다. 턴·밤 HUD(R7)를 붙일 때 다시 본다.

## 2. 요청 — 거절 `Reason` 코드 목록 확인

View는 `CombatResult.Reason`을 표시 문구가 아닌 코드로 받는다. 이번 범위에서는 로그만 남기고, HUD에 거절 문구를 붙일 때 코드 → 문구 표를 만든다. 그 전에 목록이 맞는지 확인하고 싶다.

코드에서 찾은 명령 거절 코드(2026-10-02):

| 출처 | 코드 |
|---|---|
| `CombatCommandCalculator.Reject(input, "…")` 직접 문자열 | `ActionCostInsufficient`, `DeferredMultiCellUnit`, `EnemyRedeploymentBlocked`, `InvalidEnemyHeadquarters`, `InvalidMainCharacter`, `InvalidPlacement`, `InvalidReturn`, `ReserveFull`, `StageEnded`, `UnsupportedCommand`, `WrongPhase` |
| `AttackFailureReason.ToString()` | `WrongPhase`, `EntityNotFound`, `NotUnit`, `NotInCombat`, `Dead`, `NotActiveUnit`, `NotSupportAppointMethod`, `InvalidPrimaryTarget`, `NonHostile`, `OutOfBounds`, `ActionCostInsufficient`, `MapOccupancyMismatch`, `StageEnded`, `InvalidAttackDirection`, `AmbiguousAttackDirection`, `InvalidAttackAim`, `AppointCellRejected`, `AttackerDiedOnMovementSettlement`, `NoEligibleTargets`, `UnresolvedEmptyAll`, `DeferredMultiCellUnit` |
| `MovementFailureReason.ToString()` (이동·턴 종료) | `EntityNotFound`, `NotUnit`, `WrongPhase`, `NotInCombat`, `Dead`, `NotActiveUnit`, `UnsupportedMoveMethod`, `SameAnchor`, `OutOfBounds`, `DestinationOccupied`, `MoveRangeExceeded`, `ActionCostInsufficient`, `StateInconsistent`, `StageEnded` |
| `CharacterDrawFailureReason.ToString()` (드로우) | `StageEnded`, `WrongPhase`, `FirstPreparation`, `DeckEmpty`, `LivingSubLimitReached`, `ReserveFull`, `InsufficientGold` |

확인하고 싶은 것:

1. 위 목록이 명령 거절 코드의 전부인가? 빠진 경로가 있는가?
2. 코드 문자열은 바뀌지 않는 계약으로 봐도 되는가? enum 이름을 바꾸면 View 문구 표도 바뀌므로, 바꿀 때 알려 주기 바란다.
3. 자동 진행(`Kind == Advance`)이 거절될 때의 `Reason` 값은 무엇인가? [Core 답변](2026-10-01-combat-core-reply-to-task4a-view-reply.md)대로 자동 진행 거절은 "계속할 수 없음"이라 Flow가 멈춘다. 그때 로그에 남길 코드를 알고 싶다.

## 3. 요청 — 시작 재화를 채우는 공개 경로

- **현재 코드:** `CurrencyBuffer`를 채우는 Core 공개 경로가 없다. 새 Core는 `StageEconomySpec.InitialGold`를 재화에 반영하지 않고 `ManualDrawGoldCost`만 전투 상태로 옮긴다. 그래서 새 런타임의 재화는 0이다.
- **View의 임시 대응:** 개발 fixture의 `ManualDrawGoldCost`를 0으로 둬 드로우가 재화 부족으로 거절되지 않게 한다. View·부트스트랩이 Buffer에 재화를 직접 쓰지는 않는다. 재화 부족 거절은 아직 검증하지 않는다.
- **질문:** 시작 재화(런 시작 또는 스테이지 진입 시 `InitialGold`)는 누가, 어느 시점에 채우는가? 8R6 남은 일의 "Startup roster/wallet — 실제 로드아웃 키와 호출 시점 합의"와 같은 항목으로 보면 되는가? 정해지면 개발 준비도 그 공개 경로를 호출하도록 맞춘다.

## 3a. 요청 — 시작 서브 캐릭터 수 규칙

- **현재 코드:** `CharacterRosterRules.StartingSubCount = 3`이고 `PlanInitialRoster`는 시작 서브가 **정확히 3명**이 아니면 `InvalidRequest`로 거절한다.
- **데이터와의 차이:** 옛 `PlayerLoadout_1`(Akimbo 2·ShotGun 1·Sniper 1)과 DB 계획 T11 구성(Akimbo·ShotGun·Sniper·CactusCannon 각 1)은 둘 다 4명이라 그대로는 로스터 초기화가 거절된다.
- **View 쪽 임시 조치(2026-10-02, 사용자 결정):** 개발·카탈로그 검증을 위해 `PlayerLoadout_1`을 메인 MainCharacter + Akimbo·ShotGun·Sniper 각 1(3명)로 맞췄다.
- **질문:** 기획상 시작 서브는 이후 **현재 덱 상황에 따라 달라질 예정**이다. 시작 서브 수를 고정 상수(3)로 둘지, 로드아웃 데이터(또는 덱 상태)에서 정할지 Core 규칙으로 정해 주기 바란다. 바뀌면 `PlayerLoadoutData` 검증과 데이터도 함께 맞춘다.

## 4. 알림 — 개발 전용 진행 준비 경로

전투가 승패로 끝나면 `CombatProcessingService`가 `ChapterProgression.PlanStageResolution`을 부르므로, 현재 챕터·노드가 선택돼 있어야 한다. 개발 Live 부트스트랩은 이를 위해 Core 공개 서비스만 호출한다(설계 W12).

```text
CoreGroup.Characters.InitializeRoster(loadoutKey)
→ CoreGroup.ChapterProgression.EnterChapter(chapterKey)
→ CoreGroup.ChapterProgression.ConfirmStageEntry(chapterKey, nodeId)
```

- `ConfirmStageEntry`의 주석은 "진입 준비를 마친 진행 Job이 호출"이다. 부트스트랩에서 직접 부르는 것은 `UNITY_EDITOR || DEVELOPMENT_BUILD`로 격리한 개발 전용 단축 경로이며 운영 진입 경로를 대신하지 않는다. 이 사용에 문제가 있으면 알려 주기 바란다.
- 개발 Live에서는 진행 판정이 DB의 노드·Stage를, 전투 내용이 개발 fixture를 따르므로 둘이 같은 스테이지가 아닐 수 있다. 이 정산 결과를 운영 결과로 보지 않는다.

## 5. 알림 — 브랜치 상태

- 8R6 문서의 "세 Job 미연결" 표는 Core 브랜치 기준이다. Stage 브랜치 `ede27fef`에서 세 Job을 이미 연결했다(정상 false도 완료, 예외는 전파).
- Core 브랜치 `069327c3`(옛 구조 삭제)은 아직 Stage 브랜치에 병합하지 않았다. 2026-10-02에 확인한 결과 병합 충돌은 없고, Stage 브랜치에만 있는 두 커밋의 파일은 삭제된 타입을 참조하지 않는다. View가 쓰는 API(`CombatProcessingService`, Buffer, Factory, LifeCycle)는 이 커밋에서 바뀌지 않았다. 병합 시점은 사용자가 정한다.

## 6. 전달·답변 기록

| 항목 | 전달 | 답변 | 결과 |
|---|---|---|---|
| §1 준비 인덱스·리비전 | 미전달 | — | — |
| §2 Reason 코드 | 미전달 | — | — |
| §3 시작 재화 경로 | 미전달 | — | — |
| §3a 시작 서브 수 규칙 | 미전달 | — | — |
| §4 개발 진행 준비 | 미전달 | — | — |

전달은 사용자가 한다. 답변이 오면 이 표와 View 계획 §7에 적는다.

## 6a. A2 지정 방식 DB 제공 (2026-10-04, 미전달)

공격 지정 방식 테이블과 유닛 5개의 참조를 추가했다. 새 API·키 표·생성자 변경·테스트 대역 변경과 Core의 기본 source 조립/T1·G7 해소 요청은 [DB 경계 인계 §5.6](2026-09-24-view-core-data-boundary-handoff.md#56-view-a2--공격-지정-방식-db-제공-2026-10-04-미전달)에 모았다. 실행·검증 상태는 View 계획 §7.19를 따른다. 이 변경만으로 운영 DB 진입 성공이나 시작 재화·F2 계약이 해결된 것은 아니다.


## 7. 6A/B3 명시적 진입 이전 반영 (2026-10-04, 미전달)

§4의 개발 Combat ConfirmStageEntry 경로는 6A/B3에서 제거했다. 두 모드 모두 로스터·EnterChapter 후 캡처한 chapter/node를 기존 Register→Plan→Runner의 CombatPrepareJob으로 전달하며 EnterBattle이 한 번 실행된다. 정상 거절도 Job은 완료하고 이번 수락 결과에만 LifeCycle·View를 활성화한다. 호환 PrepareBattle은 확정 선택의 기존 테스트/호출자용으로 유지한다. §5의 미병합 안내는 과거 기록이며 현재 기준은 Core 81c8c38이 포함된 491cae4d다.

Core·Buffer 생산 파일은 수정하지 않았다. 실제 등록 경로 검증과 제한은 [골격 계획 §6.10](../plans/2026-09-30-combat-flow-skeleton-plan.md#610-phase-6a-실행-기록-2026-10-04-코드-완료unity-검증-대기), Bootstrap·표시 검증은 View 계획 §7.17이 원본이다. 새 테스트는 서로 다른 대상 정의와 유효한 맵/메인 범위를 사용하며 운영 DB·F2 성공으로 보고하지 않는다. T1/G7·포기 입력 조립·Completion 표시 수명은 여전히 후속이다. 외부 발송·새 답변 수신 없음.


## 8. Phase F / 골격 7 인계 결과 (2026-10-05)

현재 코드 기준은 `bfe63a27`이다. §1의 준비 인덱스/새 리비전 불필요 답변은 유지한다. §2 Reason 목록은 작성 당시 코드 목록이며 새 경로/Return 추가 후의 전체 안정 목록으로 보장하지 않는다. Reason별 HUD 확장 시 Core와 다시 대조한다. §3의 초기 재화 질문은 [최신 요청 3](2026-10-05-combat-core-requests-phase6b.md#11-요청-3-상세--두-재화의-원본초기화보존)으로 대체하며 두 재화를 합친 초기화 질문을 다시 사용하지 않는다. §3a 시작 서브 수는 후속 규칙 질문이다.

View는 명령/미리보기/자동 진행 API, 입력 유지·번호 대응, 일반 결과 보존 계약을 사용하고 실제 경로 이동·Return·최종 결과 소비를 연결했다. 정상 종료의 결과/Completion 표시 뒤 EndBattle 호출자는 골격 §6.4다. 일반 승리의 실제 호출자도 연결됐지만 포기·운영 DB·재화 분리는 [최신 Core 요청](2026-10-05-combat-core-requests-phase6b.md)의 별도 조건이다.

실행 증거와 한계는 [공동 인계 §2](2026-10-05-combat-view-skeleton-session-handoff.md#2-인계-범위와-증거)를 참조한다. 이번 문서 정리에서 Core 코드·Buffer·게임 규칙을 변경하지 않았고 외부 발송·새 API 인수를 완료로 표시하지 않는다.
