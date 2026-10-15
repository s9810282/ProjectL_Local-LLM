# 전투 골격 최종 인계 — 2026-10-07

- 작성: 2026-10-07 · 골격 세션 · 브랜치 `feature/Yonggeun/Stage`(커밋 기준 `29a7bbbe` + 이번 세션 미커밋 변경)
- 상태: **골격에서 바로 할 수 있는 작업은 끝났다.** 남은 일은 외부 조건 대기뿐이다. View 작업은 다른 세션에서 진행한다.
- 이 문서는 요약과 링크다. 정책·실행 기록의 원본은 아래 §5의 문서이며, 같은 내용을 여기서 고치지 않는다.

## 1. 완료 범위

| 범위 | 내용 | 검증 |
|---|---|---|
| Phase 0~5 | Flow·Plan·Job 골격, F3 결과 읽기, 네 Job의 Core 연결 | 기존 기록(계획 §6.1~§6.11) |
| 6A·F1 | 명시적 진입(K20), 공유 오류 경계 인수 | 계획 §6.12~§6.14 |
| 6B F2 일반 승리 | 결과 확인 → EndBattle → StageSelect | 계획 §6.14 |
| 6B F2 패배·최종 승리(K21) | Run 종료 판정 → `StartNewRun(1)` → StageSelect | 계획 §6.16, 사용자 Unity 테스트 통과(2026-10-07) |
| 6B F2 자발적 포기(K22) | `RequestAbandon()` → 남은 처리·표시 → `AbandonBattle()` → 위와 같은 Run 종료 흐름 | 계획 §6.17, 사용자 Unity 컴파일·테스트 통과(2026-10-07) |
| 정리 | S5 옛 로스터 Job 삭제, S6 NextEntry fixture(아티팩트 보드 설정), 병합으로 빠졌던 K21 테스트 복원 | 계획 §6.16 정정·후속 |
| Phase 7 | 2026-10-05 공동 인계와 현재 통합 UML | [공동 인계](2026-10-05-combat-view-skeleton-session-handoff.md) |

## 2. View·다른 작업자가 쓰는 공개 계약

| 계약 | 쓰는 곳 | 원본 |
|---|---|---|
| `ILifeCycleSystemGroupService.CombatEntry` → `RequestEntry(chapterKey, nodeId)` / `CancelEntry` / `IsActive` | StageSelect 노드 진입(S2, 다른 작업자), 개발 Bootstrap | 설계 §6.3 K20 |
| `ILifeCycleSystemGroupService.CombatExit` → `RequestNormalExit(destination)` | 자동 호출자가 이미 부른다. 직접 쓸 일은 거의 없음 | 설계 §6.4 |
| `CombatExit.RequestAbandon()` | **View 4-V 포기 버튼·확인창.** `ExitStatus == Idle`이고 전투 중일 때만 버튼 활성, false면 아무것도 하지 않음. Live 전용 | 설계 §6.5 K22 |
| `CombatExit.ExitStatus` | 버튼 활성·입력 차단 판단 | 설계 §6.4·§6.5 |
| `ICombatExitPresentation`(View 구현) | `SuspendSubmissions`, `PresentCompletion(completion, completed)`. 포기도 같은 완료 표시를 쓴다(`Completion.Outcome.Reason == VoluntaryAbandonment`로 문구 구분 가능) | 설계 §6.4 |

- Run 종료 목적지는 `CombatLifeCycle.RunEndDestination` 한 곳(현재 `LifeCycleKeys.Stage`).
- 새 Run 로드아웃은 `CombatPlan.NewRunLoadoutKey = 1` 한 곳(임시 상수).
- 개발 Combat 직접 시작(Boot)은 `GameBootstrap.useDevProgress`(새 Run·챕터·노드 진입)와 `devBattleDefinition`(Fixture = 개발 고정 정의 / Database = 운영 DB 정의)로 나뉜다. DB 전투 확인은 설정 창의 Battle Definition = Database(View 계획 §7.56 S7).
- Combat의 `LifeCycleManager.Change/Exit` 직접 호출은 전환 가드가 막는다. 이탈은 반드시 `CombatExit`를 거친다.

## 3. 남은 일과 다시 열리는 조건

| 작업 | 담당 | 조건 | 그때 할 일 |
|---|---|---|---|
| S2 StageSelect 노드 클릭 진입(`StageMapEntryFlow` 인수) | **다른 작업자**(사용자 결정) | 해당 작업자 진행 | 골격은 `RequestEntry`를 그대로 제공. 수정 없음 |
| 4-V 포기 버튼·확인창 | **View 세션** | 바로 가능 | 위 §2의 `RequestAbandon()` 호출. 재생으로 포기 → StageSelect 확인 |
| S3 보스 승리 → 다음 챕터 | DB·기획 + Core | DB에 챕터 연결 값(다음 챕터·최종 여부)이 없다(`StageMapData`는 깊이·폭·연결 방식·시드만). DB 값 추가 → Core가 읽는 `IChapterContinuationSource` 구현(현재 운영은 `UnavailableChapterContinuationSource`) | 골격 코드 변경 없음. 연결되면 `RunCompleted`/`NextChapterSelected` 분기만 재생 확인. 그전까지 보스 승리는 임시로 Run 종료(K21) |
| Title 목적지 | 골격 | Title LifeCycle·씬이 생김 | `RunEndDestination` 교체, 운영 로드아웃 선택으로 상수 1 대체 |
| Run 시작 재화 | Core·기획 | `PlayerLoadout_1.startingRunCurrency` 기획 값 | 골격 변경 없음 |

## 4. 사용자 확인·전달 대기

- **재생 확인(View 계획 §7.56 진행 순서 0):** 손패 카드, 스테이지 골드(200·드로우 −40), 개발 새 Run(Live·RecordInputOnly), 패배·보스 승리 → 새 Run → StageSelect. 포기는 4-V 뒤.
- **메모 전달(진행 순서 3, 미전달):**
  - 천명님: [Run 종료 목적지 결정 S4](2026-10-07-combat-run-end-destination-memo.md) — 챕터 연결 정의 시점·`DefinitionUnavailable` 임시 처리·`CompleteChapter` 호출 순서 확인 요청 포함
  - 천명님: [VR0 타임라인 요청](2026-10-06-combat-timeline-core-request-memo.md), [이동 입력 형식](2026-10-06-combat-movement-input-format-memo.md)
  - 현강님: [UnitCard 분리](2026-10-06-unit-card-split-share.md)
- 답장이 오면 S3·Title 행과 설계 §6.4·§6.5에 반영한다.

## 5. 원본 문서

| 문서 | 볼 곳 |
|---|---|
| [골격 설계](../specs/2026-09-30-combat-flow-skeleton-design.md) | §4.1 Flow 분기, §6.3 진입(K20), §6.4 정상 이탈·Run 종료(K21), §6.5 포기(K22), §9 결정 기록 |
| [골격 계획](../plans/2026-09-30-combat-flow-skeleton-plan.md) | §0.1·§0.2 순서·차단 사유, §6.14~§6.17 최근 실행 기록 |
| [View 계획](../plans/2026-10-01-combat-view-plan.md) | **§7.56 남은 작업과 진행 순서의 원본** |
| [View 전용 시작 안내](2026-10-05-combat-view-next-session-handoff.md) | View 세션의 읽을 순서·수정 경계 |
| [현재 통합 UML](../specs/2026-10-05-combat-current-uml.md) | 10-05 기준 클래스·흐름. K21·K22(Run 종료 판정, `Abandoning`, `PreparingNewRun`)는 설계 §6.4·§6.5가 더 최신 |
| [공동 Framework 공유](2026-10-05-combat-view-skeleton-framework-share.md) | 공용 파일 변경(`JobRegisterSystem` 등) |

## 6. 주의

- 병합 때 `CombatEntryIntegrationTests.cs`가 옛 판으로 되돌아간 적이 있다(2026-10-07 복원). 병합 뒤에는 K21·K22 테스트(`ChapterVictory_WithoutContinuationDefinition_…`, `FinalChapterVictory_…`, `Defeat_EndsRun_…`, `Abandon_…` 4개)가 남아 있는지 확인한다.
- `CombatFlowIntegrationTests`·`CombatEntryIntegrationTests`의 fixture 값(맵 높이, 본부 칸, 아티팩트 보드 설정)은 Core·스테이지 맵 검증 규칙에 맞춘 값이다. 규칙이 바뀌면 fixture 생성자에서 먼저 실패한다.
- Core 파일은 이 세션에서 수정하지 않았다. 커밋·push는 사용자가 한다.
