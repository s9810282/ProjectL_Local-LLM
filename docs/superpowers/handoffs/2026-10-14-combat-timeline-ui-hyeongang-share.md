# 전투 타임라인 UI 열기·닫기 이전 공유 (용근 → 현강님)

- 작성일: 2026-10-14. 작성: 용근(View). 브랜치 `feature/Yonggeun/Stage`.
- **상태: 2026-10-16 폐기.** `993b0068`에서 Timeline UI가 삭제돼 사용자 지시로 남은 열기·닫기 시스템과 연결을 정리한다. 아래는 2026-10-14의 이력이며 §4의 요청은 철회한다. 외부 전달은 하지 않았다. 현행 계약은 설계 D11, 실행·검증 기록은 계획 §6.12다.
- 관련: [구조 리팩토링 설계](../specs/2026-10-14-combat-view-structure-refactor-design.md) D11, [리팩토링 계획](../plans/2026-10-14-combat-view-structure-refactor-plan.md) R4·§6.8

## 1. 요약

- 현강님이 `CombatViewSystem.SyncTimelineUI`에 넣으신 타임라인 UI 열기·닫기를 **새 View 시스템 `CombatTimelineViewSystem`으로 옮겼습니다.** `CombatViewSystem`은 이제 타임라인을 모르고, UI 서비스도 참조하지 않습니다.
- **`TimelineUI.cs`는 고치지 않았습니다.** 여는 방식도 그대로 `OpenUI(UIId.Timeline, true)` / `CloseUI(UIId.Timeline)`입니다.
- 정상 흐름(G로 밤 진입 → 열림, 밤 종료 → 닫힘, 승패·포기 → 닫힘)은 같습니다. 바뀐 동작 세 가지는 §3에 있습니다.
- 부탁드리는 것은 §4에 있습니다(중복 검사 정리 1건, 확인 후보 1건).

## 2. 무엇이 어디로 바뀌었나

| 항목 | 전 (`CombatViewSystem.SyncTimelineUI`) | 후 (`CombatTimelineViewSystem`) |
|---|---|---|
| 위치 | 전투 View 조정자의 결과 후처리 안 | 독립 View 시스템. View Factory에 전투 View 바로 다음으로 등록 |
| 판단 근거 | 결과의 **명령 종류**: EnterCombat 수락 → 열기 / Finished 또는 EndUnitTurn 뒤 배치 복귀 → 닫기 | Buffer **상태**: 전투가 있음 ∧ 씬 연결 때의 전투와 같음 ∧ `StagePhase == Combat` ∧ `Phase != Finished`이면 보임 |
| 시점 | 결과를 그린 직후(확인 전) | Combat 씬에 연결된 동안 매 Tick. 표시 안 된 결과가 있으면(`PresentedResultSequence < LatestResultSequence`) 지난 보임을 유지하고, 결과 연출이 끝나 확인된 그 Tick에 맞춥니다. 프레임으로는 전과 같습니다 |
| 닫는 때 | 위 결과일 때만 | 위 상태가 아닐 때 + Combat 씬 해제·종료 + 오류 정지 |

## 3. 바뀌는 동작 세 가지

1. **밤 도중 다시 연결되면 열립니다.** 전에는 다음 EnterCombat 결과가 올 때까지 닫혀 있었습니다. 표시 안 된 결과가 남아 있으면 그 연출이 끝난 뒤 엽니다.
2. **Combat 씬 해제·종료, 오류 정지 때 닫습니다.** 전에는 닫지 않았습니다.
3. **판단 근거가 상태입니다.** 명령 종류와 관계없이 밤이 끝나 배치로 돌아가면 닫히고, 전투가 끝나면(Finished) 닫힙니다. 거절된 결과는 상태를 바꾸지 않으므로 전처럼 아무 일도 없습니다.

## 4. 부탁

### 4.1 `TimelineUI` 안의 단계 검사 정리 (같은 조건이 두 곳)

- `TimelineUI.RefreshTimeline`(TimelineUI.cs 100~101행)은 `_combat.Completion != null`, `state.Phase == CombatFlowPhase.Finished`, `state.CommandState.Turn.StagePhase != StagePhase.Combat`이면 유닛을 지웁니다. 같은 "밤 전투 중" 조건이 이제 `CombatTimelineViewSystem.ShouldShow`에도 있어 두 곳입니다.
- 패널은 시스템이 열어 둔 동안에만 활성이라 이 단계 검사는 지워도 됩니다. 남겨야 할 것은 준비 확인(`isActiveAndEnabled`, `state == null`, `_characters == null`)과 유닛별 걸러내기(사망·필드 밖)입니다.
- `Completion != null` 확인은 시스템 조건에 없습니다. 지우시기 전에 완료 설치와 `Finished`가 같은 결과에서 함께 오는지 봐 주세요. 다르다면 알려 주시면 시스템 쪽 조건에 더하겠습니다.
- 고칠지와 시기는 현강님이 정해 주세요. 저희는 `TimelineUI.cs`를 건드리지 않았습니다.

### 4.2 (확인 후보) 목록이 연출보다 먼저 바뀜

- `TimelineUI`는 매 `Update`마다 최신 Buffer를 그립니다. 패널 열기·닫기는 연출이 끝난 뒤인데 목록은 즉시 바뀝니다. 예: 밤 종료 결과가 게시되면 마지막 연출이 도는 동안 유닛이 먼저 사라져 빈 틀이 잠깐 보일 수 있고, 밤 중에도 순서가 연출보다 먼저 바뀔 수 있습니다.
- 이번 리팩토링 전부터 있던 동작이고, 패널을 누가 여닫든 남는 문제라 이번 범위에 넣지 않았습니다.
- 고친다면 한 가지 방법은 `TimelineUI`가 `ICombatViewBufferReadSink.PresentedResultSequence < ICombatStateBufferReadSink.LatestResultSequence`인 동안 목록을 고정하는 것입니다. 필요하다고 보시면 알려 주세요.

## 5. 확인 방법

- EditMode: `CombatTimelineViewSystemTests`(보임 진리표, 연결 전·연출 중 유지, 밤 종료·전투 종료·전투 정리·다른 전투에서 닫힘, 같은 값 재호출 없음, 씬 해제·오류 정지 닫힘, UI 서비스 없는 조립, Factory 등록 순서).
- 재생: G로 밤 진입 → 밤 진입 연출이 끝난 뒤 타임라인 열림 → 밤 종료 → 닫힘. 승리·패배·포기 → 닫힘.
