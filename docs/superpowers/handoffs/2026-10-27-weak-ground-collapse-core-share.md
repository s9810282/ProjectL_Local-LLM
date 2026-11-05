# 약한 지반 다시 켬 — Core 변경 공유

- 작성: 2026-10-27, View 작업(용근)
- 받는 사람: 천명님(Core)
- 상태: **Core 변경 완료, 사용자 커밋 `2805717b`.** 사용자 지시로 View 작업에서 Core를 고쳤다. 미전달. 컴파일·시험은 사용자가 Unity에서 확인할 예정이다(지금은 미확인).
- 기록 위치: [Core 전달 문서 §5.55](2026-09-24-view-core-data-boundary-handoff.md)
- 원본 문서: 설계 [weak-ground-collapse-after-move-design](../specs/2026-10-27-combat-weak-ground-collapse-after-move-design.md)(규칙 원본 WG1), 계획·실행 기록 [weak-ground-collapse-plan](../plans/2026-10-27-combat-weak-ground-collapse-plan.md) §7

## 1. 요지

10-24(`d4a6d6fd`)에 보류하신 약한 지반을 다시 켰다. 보류 표의 기획 질문("주인 턴 끝 vs 플레이어 턴 끝")에 사용자가 **"밟은 행동(명령)이 끝날 때"**로 답했다.

- 진입을 세는 규칙은 보류 전 그대로다(이동 경로·넉백·끌어오기 모두 셈).
- 붕괴 시점만 새로 정했다. 한도에 닿은 칸은 **그 진입이 일어난 명령 끝**에 낭떠러지가 된다.
- Core 변경은 두 파일이다. 옛 본문 둘을 되살리고, 명령 공통 끝에 한 줄을 넣었다.

## 2. 규칙

1. **세는 진입:** 캐릭터가 실제로 들어간 칸마다 +1이다.
   - 이동 경로는 지나가기만 해도 센다.
   - 넉백·끌어오기·이동 중 환경 밀기도 센다. 여러 칸 밀리면 지나간 칸마다 센다.
   - 밀린 장애물(구조물)·배치·손패 복귀는 세지 않는다.
   - 아군·적 모두 같다. 여기까지는 보류 전 규칙과 같다.
2. **붕괴:** 진입이 일어난 명령이 끝나는 순간, 한도에 닿은 약한 지반이 모두 낭떠러지가 된다. 그 위의 캐릭터·장애물은 떨어진다(옛 `CollapsePending` 그대로).
   - 이동 명령에서 떨어질 수 있는 것은 이동을 마친 이동자뿐이다(지나간 칸은 비어 있어 무너지기만 함).
   - 공격 명령에서는 그 칸으로 밀리거나 끌려와 멈춘 대상이다.
3. **승패가 난 뒤:** 명령 중 승패가 이미 났으면 무너뜨리지 않는다(옛 `Terminal` 건너뜀).
4. **턴 경계:** 플레이어 턴 끝·적 국면 끝·밤 끝에는 아무 일도 없다. 진입한 명령이 끝에서 무너뜨리므로 **명령 끝에 예정 붕괴가 남지 않는다.** 보류 때의 조건("풀리지 않는 예정 붕괴를 쌓지 않음")을 지킨다.
5. **숨은 약한 지반:** 실행은 세고 무너뜨린다. 미리보기는 보이는 환경만 본다(보류 전 규칙).

## 3. 바꾼 Core 코드

경로 약칭 `Combat/` = `CoreCaculator/Application/Combat/`.

### 3.1 `Combat/WeakGroundCalculator.cs`

- `RegisterEntry`·`CollapsePending` 본문을 `2d0509df` 그대로 되살렸다(줄 단위 같음, 주석만 더함).
- `ResolveInterruptedActionEnd`는 지웠다. 턴 경계에 붕괴가 없어서 할 일이 없다.

### 3.2 `Combat/MovementProjection.cs` `Complete`

```csharp
// 전
WeakGroundCalculator.ResolveInterruptedActionEnd(this);

// 후
// 진입이 일어난 명령 끝에 한도에 닿은 약한 지반을 무너뜨립니다. 승패가 이미 났으면 무너뜨리지 않습니다.
// 턴 끝·적 국면 끝은 이 함수를 지나지 않습니다. state를 넘기는 호출(전투 시작)은 진입이 없어 여기서 무너질 칸이 없습니다.
if (!Terminal) WeakGroundCalculator.CollapsePending(this);
```

`Complete`를 지나는 명령과 붕괴 여부:

| 명령 | 지나나 | 비고 |
|---|---|---|
| 이동(미리보기 포함) | 예 | `Transition()` |
| 공격·반응 공격(포효) | 예 | `CombatAttackCalculator.cs:156·159` |
| 배치 | 예 | 진입을 세지 않아 무너질 칸이 없음 |
| 전투 시작 | 예 | 재배치는 세지 않아 무너질 칸이 없음 |
| 플레이어 턴 끝·적 국면 끝 | **아니오** | `CombatTurnCalculator.Changed`로 바로 만듦 |

바꾸지 않은 것:
- 진입을 세는 호출 자리: 이동 경로 `CombatMovementCalculator.cs:105`, 강제 진입 `MovementProjection.ResolveForcedTerrainEntry`(넉백 `AttackKnockbackCalculator.cs:169`·끌어오기 `AttackForcedMovement.cs:57`·환경 밀기 `MovementEnvironmentCalculator.cs:191`).
- Contracts(`StaticTerrainFact` 등)·Buffer·사건 형식. Buffer `ValidateGroundChange`는 한 명령 안의 "진입 → 붕괴"를 이미 받는다.

## 4. 고친 시험 (사용자 허락)

보류를 확인하던 천명님 시험 열 사례를 새 규칙에 맞췄다. 사례마다 위에 "2026-10-27 View 작업에서 사용자 허락으로 기대값 변경" 주석을 달았다. 새 `[Test]`는 만들지 않았다.

| # | 시험 | 바꾼 것 |
|---|---|---|
| 1 | `WeakGroundMovementTests.MovementDoesNotCountOrCollapseWeakGround` → `Movement_CountsEveryEntry_AndCollapsesAtCommandEnd` | 사례 `2L` → `1L`(시작부터 예정 상태인 입력은 이제 생기지 않음). 지나간 칸이 세어지고, 한도에 닿으면 무너지고, D에 멈춘 이동자는 산다 |
| 2 | `WeakGroundMovementTests.HiddenGround_RemainsHiddenAndUnchangedDuringMovement` → `HiddenGround_PreviewDoesNotRevealOrCountItButExecutionDoes`(보류 전 이름) | 미리보기 단언은 그대로. 실행은 숨은 지반도 셈 |
| 3 | `WeakGroundMovementTests.ForcedAttackRelocations_DoNotChangeWeakGroundOrAttackerSettlement` → `ForcedAttackRelocations_CountEveryEntryWithoutChangingAttackerSettlement` | 진입 수 0 → 1(보류 전 기대값과 같음), 원본 사건 둘. 공격자 정산 단언은 그대로 |
| 4 | `WeakGroundTurnTests.EndBoundaries_DoNotCollapsePendingGround` | 삭제. 지운 함수를 부르고, "턴 끝에는 무너지지 않음"은 같은 파일 `ControllerCompletion_…`이 이미 확인 |
| 5 | `WeakGroundTurnTests.EntryBoundary_DoesNotArmACollapse` → `EntryThenCollapse_TurnsGroundIntoCliffAndDropsOccupant` | 직접 부른 진입·붕괴: 낭떠러지, 원본 사건 둘, 추락 하나, 점유자 사망 |
| 6 | `CliffFallTests.AttackKnockback_ResolvesTerrainBeforeThornsAndObjects_AndPreviewAgrees(WeakGround)` | 약한 지반 갈래만 진입 수 1·원본 사건 있음(한도 2 미만이라 붕괴 없음) |
| 7 | `Framework/WeakGroundPublicationTests.PreviewAndEntry_…` → `PreviewAndEntry_PublishExactCountAndOneResult_WithoutChangingOriginal` | 한도 2 → 3(이동자가 그 칸에 멈춰 한도에 닿으면 떨어지므로), 남은 횟수 1·예정 아님 |
| 8 | `Framework/WeakGroundPublicationTests.Collapse_PublishesEverySourceAndFallWithFinalOccupantsInOneAcceptedResult` | 턴 끝 대신 이동 명령 하나(72를 지나 71에 멈춤)로 두 원본을 무너뜨린다. 붕괴 사건만 골라 {71, 72}, 추락 칸 (1,3) |
| 9 | `Framework/WeakGroundPublicationTests.KnockedObstacleThenCollapse_PublishesRemovalAndPreservesBindings` | 턴 끝 단계를 걷었다. 밀기 공격 결과 안에서 지반이 무너지고 화약통 51이 떨어진다(`Destroyed` 반응 없음). 장애물 진입을 세지 않는 것은 원본 사건이 붕괴 하나뿐인지로 본다 |
| 10 | `Framework/WeakGroundPublicationTests.FreshBattle_InstallsAuthoredZeroProgressInsteadOfPreviousCliff` | 앞 전투에서 무너뜨리는 단계를 턴 끝 대신 이동 명령(지반을 지나감)으로. 새 전투 설치 단언은 그대로 |

- 7~10은 10-24 보류 뒤 이미 실패하던 것으로 보인다(코드 대조, 실행 안 함). 옛 규칙(진입을 셈, 턴 끝 붕괴)에 기대고 있었다.
- 컴파일 맞춤 하나: `CoreIntegration/PullAndMarkPresentationTests.cs:422`. View 필드 창구에 칸이 하나 늘어 생성 인자 `(_, _) => { }` 하나를 더했다.
- 그대로 통과로 보는 것: `WeakGroundTurnTests.ControllerCompletion_…`, `ExperienceAttributionPathTests.DeferredGroundAtPlayerEnd_…`, `RejectedRoute_…`, `PlayerPlacement_…`, `ObstacleReactionTests.Knockback_SurvivingObstacleMovesWithoutGroundCount`, `CliffFallTests.EnvironmentForcedEntry_…`(낭떠러지 사례), `DeferredCombatFeatureTests.EnvironmentClocksDoNotActivateOrArmCounters`, `WeakGroundStateTests`, `TileSuccessorTests`.

## 5. View가 기대는 Core 사실

바뀌면 View도 고쳐야 한다.

- 붕괴 사건은 `CombatMovementSourceEvent(약한 지반 → 같은 ID 낭떠러지)`이고, 그 뒤에 `CombatFallEvent` → `Died` 순서다. View는 붕괴 사건 때 칸을 낭떠러지로 바꾸고, 추락 사건이 있는 사망은 가라앉기 연출로 그린다.
- 진입만 바뀐 사건(같은 종류, 진입 수 +1)은 외형을 바꾸지 않는다.
- 강제 진입의 진입 사건은 그 강제 이동(`Moved`) **바로 뒤**에 온다(`ResolveForcedTerrainEntry`). View는 이 순서에 기대 진입 사건을 그 이동의 동시 묶음에 붙인다(여러 대상 동시 넉백이 끊기지 않게). 순서가 바뀌면 그 대상만 차례 재생이 된다(동작은 맞고 동시성만 줄어듦).

## 6. 알려진 차이·제약 (요청 아님)

1. **공격 미리보기는 붕괴 추락을 예상하지 못한다.** `AttackResultFactory.Create`(`CombatAttackCalculator.cs:150`)가 `Complete`(`:159`)보다 먼저 미리보기 값을 만든다. 그래서 넉백으로 무너져 떨어질 대상도 체력·처치 예상에는 살아남는 것으로 나온다. 공격 결과 `Hits`의 맞은 뒤 체력도 같다. Buffer는 맞은 뒤 체력을 최종 체력과 대조하지 않아 거절되지는 않는다.
2. **`Complete(state: …)` 제약:** 상태를 미리 만들어 넘기는 호출(전투 시작 `CombatTurnCalculator.cs:54`)에서 붕괴가 일어나면 넘긴 상태의 환경이 낡는다. 지금은 전투 시작에 진입이 없어 생기지 않는다. 주석만 남겼다.
3. **적 AI는 약한 지반을 모른다.** `EnemyMovementPathfinder`는 지뢰와 들어갈 수 없는 칸만 피한다. 적이 한도에 닿는 칸에서 멈춰 스스로 떨어질 수 있다. 사용자 결정으로 이번에는 그대로 두었다(플레이 확인 뒤 기획이 정함).
4. 붕괴 추락은 `ApplyFall`이 피해 원천 없이 체력을 0으로 만든다. 넉백으로 낭떠러지에 바로 떨어뜨릴 때와 같은 처리이고 바꾸지 않았다.

## 7. 질문

1. 보류 표의 기획 질문을 이번 답("밟은 행동이 끝날 때")으로 닫아도 되는지.
2. 공격 미리보기가 붕괴 추락을 예상하도록 할지(§6의 1). 하면 View는 체력 예상에 그대로 나온다.
3. 적 AI가 무너질 칸을 피하게 할지(§6의 3). 기획 결정이 먼저다.

## 8. 데이터·확인

- 약한 지반 정의 `Terrain_WeakGround`는 한도 3이다. 확인용으로 `Test` 스테이지 칸 (6,3)에 하나 놓았다(커밋 `2805717b`).
- 숨은 약한 지반이 무너지면 낭떠러지도 숨어 화면에 안 보인다. 데이터는 양쪽 공개로 두기를 권한다(작성 메모, 코드로 막지 않음).
- View 쪽: 약한 지반 외형(금 간 반투명 사각형), 설명 문구, 무너지는 순간 칸 바꾸기, 동시 넉백 묶음 처리를 함께 넣었다. Core에는 영향이 없다.
- 컴파일·시험·재생 확인은 사용자가 Unity에서 할 예정이다. 결과가 나오면 이 문서에 덧붙인다.
