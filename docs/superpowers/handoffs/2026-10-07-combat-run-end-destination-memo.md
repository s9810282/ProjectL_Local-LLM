# Run 종료(패배·최종 승리) 뒤 흐름 결정 공유 — 천명님께

- 작성: 2026-10-07 · View·골격 담당 · 브랜치 `feature/Yonggeun/Stage`
- 상태: **미전달**(사용자가 보낸다)
- 관련 Core 문서: `2026-10-06-core-contract-changes-and-status-share.md` §2-4·§2-5·§3·§4 표
- 원본: 골격 설계 `specs/2026-09-30-combat-flow-skeleton-design.md` §6.4·K21, 골격 계획 §6.16

## 1. 한 줄 요약

공유 문서의 "패배·최종 승리 목적지는 보류, 끝난 Run은 자동으로 새로 시작하지 않음"과 달리, **사용자가 목적지를 결정했고(2026-10-06) 골격에 이미 연결했습니다.** 최종 목적지는 Title이고, Title 화면이 생길 때까지는 **새 Run을 시작한 뒤 StageSelect**로 갑니다.

## 2. 현재 동작

정상 이탈(F2) 순서는 그대로이고, `EndBattle` 뒤에 Run 종료 판정이 하나 더해졌습니다.

```text
승패 확정(포기 제외) → 결과 표시·확인 → Combat.EndBattle()
→ Run 종료 판정 (CombatExitSequence)
   · 보스 승리(Completion.Progression.ClearMap):
       ChapterProgression.CompleteChapter(chapter, bossNode)
         RunCompleted          → Run 종료
         NextChapterSelected   → Run 계속(초기화 없이 StageSelect)
         DefinitionUnavailable → Run 종료  ← 임시 규칙(아래 §4-2)
         그 밖의 거절          → 오류 경계(F1)
   · 그 밖: ChapterProgression.Outcome != InProgress 이면 Run 종료(패배의 시도 종료)
→ Run 종료면 Register(Combat, NewRun)
     → Stage와 같은 NewRunJob → RunStart.StartNewRun(1)
     → Runner Idle, Outcome == InProgress, 전투 없음 확인
→ StageSelect 전환
```

- 판정에는 Core가 돌려준 값만 씁니다. View·골격이 승패나 진행 규칙을 다시 계산하지 않습니다.
- 로드아웃 1은 Title의 새 게임 선택이 생기기 전까지 쓰는 임시 상수(`CombatPlan.NewRunLoadoutKey`)입니다.
- Run 시작 재화는 `StartNewRun`이 설치하는 값을 그대로 씁니다(현재 `PlayerLoadout_1.startingRunCurrency = 0`).

## 3. 공유 문서와 맞춰 볼 곳

| 공유 문서 내용 | 현재 골격 | 영향 |
|---|---|---|
| §2-5 "패배·최종 승리 목적지: 결정 대기(보류 유지)" | 결정됨: Title, 그전까지 새 Run → StageSelect | 공유 문서 표기만 갱신 부탁드립니다 |
| §2-1 "끝난 Run은 로스터가 남아 있어 자동으로 새로 시작하지 않는다" | Combat 이탈이 전환 **전에** `StartNewRun`을 끝냅니다. StageSelect에 들어갈 때는 새 로스터가 있으므로 `StageLifeCycle`은 `NewRun`이 아니라 `Start`(→ `EnterChapter(1)`)를 등록합니다 | 새 Run이 두 번 시작되지 않습니다. `StageLifeCycle`의 분기는 바꾸지 않았습니다 |
| §2-4 "보스 승리 뒤 챕터 계속(`ContinueChapter`) 호출자 없음" | 골격이 `EndBattle` 뒤 `CompleteChapter`를 한 번 부릅니다 | 호출자는 있습니다. 남은 것은 아래 §4-1의 정의 원본입니다 |
| §4 표 "Run 종료 후 StageSelect: `Start`(자동 새 Run 없음)" | 위 §2 흐름 | 표기 갱신 부탁드립니다 |

## 4. 확인 부탁드릴 것

1. **챕터 계속 정의 원본(`IChapterContinuationSource`)의 운영 연결 시점.** 지금 운영 조립은 `UnavailableChapterContinuationSource`라서 보스 승리가 항상 `DefinitionUnavailable`로 거절됩니다. 확인해 보니 DB에도 이 값(다음 챕터·최종 여부)이 없습니다(`StageMapData`는 깊이·폭·연결 방식·시드만). DB 값을 어디에 둘지(DB 담당과 협의)와 Core Source 구현 시점을 알려 주세요. 연결되면 골격 코드 변경 없이 최종 챕터는 Run 종료, 중간 챕터는 다음 챕터로 갈립니다.
2. **`DefinitionUnavailable`을 임시로 Run 종료로 다루는 것이 Core 의도와 맞는지.** 정의가 없으면 이어갈 챕터가 없다고 보고 끝냈습니다. 이 거절에서 Core가 기대하는 다른 처리가 있으면 알려 주세요.
3. **보스 승리 직후 `CompleteChapter` 호출 위치.** `Combat.EndBattle()` 바로 뒤, 같은 Tick에서 부릅니다(전투가 남아 있으면 `BattleRetained`로 거절되므로). 이 순서가 맞는지 확인 부탁드립니다.

## 5. 그대로 두는 것

- 자발적 포기는 아직 자동 이탈하지 않습니다. 무인자 `AbandonBattle()`(`BattleAbandonResult`)을 받아 다음 골격 작업(S1)에서 같은 이탈 경계에 연결합니다.
- Core 파일은 수정하지 않았습니다.
