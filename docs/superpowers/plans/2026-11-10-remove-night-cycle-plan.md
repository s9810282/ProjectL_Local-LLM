# 낮밤 전환(밤 종료 회수) 삭제 Task 계획

- 작성: 2026-11-10, Core(천명)
- 설계: [remove-night-cycle-design](../specs/2026-11-10-remove-night-cycle-design.md)(결정 Q-NR1 B, Q-NR2~Q-NR5 A)
- 상태: 계획. 코드 착수 전. 계획에서 정한 것 TP-1~TP-9는 확인 대상.
- 진행 규칙: Phase마다 멈추고 보고한다. 확인은 Unity Test Runner(Core 시험 어셈블리 `ProjectL.Tests.Core`·`CoreIntegration`)로 하고, 결과는 마지막 실행 결과 XML을 읽는다. 커밋·push는 사용자가 한다.

## 0. 순서와 동작 변화

| Phase | 내용 | 동작 변화 |
|---|---|---|
| NR-0 | 착수 전 확인 | 없음(코드 변경 없음) |
| NR-a | 밤 종료 흐름 삭제 | 99번째 플레이어 턴 뒤에도 밤이 끝나지 않음 |
| NR-b | `IsNight` → `InCombat` | 없음 |
| NR-c | 턴 값·준비 번호·`NightEnded` 정리, View·UI·Buffer 줄 | `NightCheckerUI` 표시 문구만 바뀜 |
| NR-d | 공유·문서 | 없음 |

Phase마다 그 Phase에서 지운 멤버를 읽는 View·UI 줄까지 함께 고쳐, Phase가 끝날 때마다 컴파일이 되게 한다.

## 1. NR-0 착수 전 확인

| Task | 내용 |
|---|---|
| T0.1 | 적 AI 작업 커밋 여부 확인. 지금 커밋 전 파일 중 이 작업이 고치는 것이 있다: `StageHazardForecast.cs`(NR-a), `CombatCommandCalculator.cs`(NR-b), 시험 `EnemyEngagementProgressTests`·`EnemyExchangeModelTests`(NR-b·NR-c 생성자 맞춤). 사용자가 AI 작업을 커밋한 뒤 시작하는 것을 권한다. |
| T0.2 | 기준 시험 결과: 사용자가 Core 시험 어셈블리를 한 번 돌리고, 결과 XML에서 이미 실패하는 시험 목록을 기록한다. 이후 Phase는 이 목록 대비로 본다. |
| T0.3 | 고칠 시험 파일의 작성자 확인(`git log`). 다른 사람이 쓴 시험은 고치기 전에 묻는다. |
| T0.4 | 다른 작업자 줄 작성자 확인(`git blame`): `NightCheckerUI:63-80`, `CombatSceneDisplay:219`, `CombatEventTypeTable:135-137`, `CombatEventSounds:134-135`, `CombatEnemyPresentationPacing:89`, `CombatStateBuffer:309-311`. NR-d 변경 정리에 쓴다. |

## 2. NR-a 밤 종료 흐름 삭제

| Task | 파일 | 내용 |
|---|---|---|
| Ta.1 | `Application/Combat/CombatTurnCalculator.cs` | `EndEnemyPhase`에서 `endsNight`·`EndNight` 합치기를 지우고, 정리한 종료 결과를 그대로 돌려준다. `EndsNightAfterEnemyPhase`·`EndNight`를 지운다. 클래스 요약의 "밤 정산" 문구를 고친다. |
| Ta.2 | `Application/Combat/CombatPlacementCalculator.cs` | `RecallForNight`를 지운다. `RedeployEnemies`·`Return`은 그대로(설계 P1·P2). |
| Ta.3 | `Application/Combat/HunterApplication.cs` | 상태를 돌려주는 `Reconcile(input, characters, state, events)`(밤 종료만 부름)를 지운다. 다른 겹치기는 그대로. |
| Ta.4 | `Application/Combat/StageGimmicks/StageHazardForecast.cs:361` | `EndsNightAfterEnemyPhase(...) ? 1 : 2` → `2`(TP-2). |
| Ta.5 | `Contracts/Combat/CombatTurnFacts.cs` | 생성자의 "한 밤 99턴 초과" 검사 조건만 지운다(TP-1). 속성·상수는 NR-c까지 남긴다(UI가 읽음). |
| Ta.6 | 시험 | 밤 종료를 일으키거나 그 결과를 확인하는 사례를 지운다: `EightTurnNightTests`, `BossNightLifecycleTests`, 그리고 `CombatItemLifetimeTests`·`HeldEnvironmentEffectTests`·`HunterTraitTests`·`HunterPersistenceTests`·`NexusReinforcementLifecycleTests`·`NexusReinforcementStateTests`·`PlayerTurnRuleTests`·`FlagLifecycleTests`·`IntimidationScheduleTests`·`MovementUndoLifetimeTests`·`RailwayGimmickTests`·`TraitShieldTests`·`WeakGroundStateTests`·`BossTurnTests`·`CombatCommonCommandTests`·`CombatCreationCompositionTests`·`CombatAdvanceServiceTests`·`ControllerTurnCountPublicationTests`·`RockfallPublicationTests`·`NexusReinforcementPublicationTests`·`EnemyHazardForecastTests`의 해당 사례. 밤과 무관한 내용을 함께 확인하던 사례는 밤 부분만 걷고 남긴다. 사례마다 무엇을 지웠는지 보고에 적는다. |

**Ua(사용자 확인):** Core 시험 어셈블리 실행. 기준 대비 새 실패 없음. 재생해서 적 국면이 끝나면 다음 플레이어 턴으로 가는지(지금과 같음).

## 3. NR-b `IsNight` → `InCombat`

| Task | 파일 | 내용 |
|---|---|---|
| Tb.1 | `Contracts/Combat/CombatCommandState.cs` | `IsNight` 속성, 생성자 넷째 인자, `With(isNight)`, `SameValues` 비교를 지운다. `public bool InCombat => Turn.StagePhase == StagePhase.Combat;`을 더한다. |
| Tb.2 | `Contracts/Combat/BattlePreparation.cs:176,179` | 생성자 호출에서 `false`·`CommandState.IsNight` 인자를 뺀다. |
| Tb.3 | `Application/Combat/MovementProjection.cs:326,796`, `CompletionWork` | 상태 복사 생성자 호출에서 인자를 빼고, `Complete`·`BeginComplete`·`CompletionWork`의 `isNight` 인자를 지운다. |
| Tb.4 | `Application/Combat/CombatTurnCalculator.cs` | `State(…, isNight)` 인자를 지운다. `EnterCombat`의 `isNight: true`를 지운다(턴이 `Combat`이 되면 `InCombat`도 참, TP-3). |
| Tb.5 | 규칙 | `isNight` → `inCombat`으로 이름·값을 바꾼다: `Domain/Artifact/Rules/SoulAltarRule.cs`, `Domain/Artifact/Rules/ThornHeartRule.cs`, `Application/Artifact/SoulEffectCalculator.cs`(`PlanDeaths`), `Application/Artifact/ThornHeartAttackScope.cs`, `Application/Combat/CommandDeathSettlement.cs`, `Application/Combat/CombatCommandCalculator.cs:329`, `Application/Combat/RoarHitCounter.cs`와 부르는 다섯 곳(`AttackDirectDamageCalculator:89`, `AttackForcedMovement:81`, `KillChainAttackCalculator:74`, `MovementProjection:508`, `ObstacleReactionCalculator:105`), `Application/Combat/AttackDeliveryExecutor.cs:35`, `Application/Combat/BossRoarCalculator.cs:31`. |
| Tb.6 | 비교 삭제 | `Application/Combat/MovementUndoPolicy.cs:70`의 `IsNight` 비교, `Contracts/Combat/MovementPreviewContext.cs`의 `IsNight` 속성과 `Application/Combat/MovementContextCalculator.cs:142` 비교. |
| Tb.7 | 유물 조건 | `Contracts/Common/ArtifactEffectKey.cs`의 `ArtifactConditionType.IsNight` → `InCombat`(숫자 그대로), `Application/Artifact/ArtifactEffectCompiler.cs:192-193,223-224` 조건·문구. DB 에셋은 고치지 않는다. |
| Tb.8 | View `ViewSystem/Combat/Display/CombatSceneDisplay.cs:219` | `ApplyPhase(state.CommandState.IsNight)` → `ApplyPhase(state.CommandState.InCombat)`. |
| Tb.9 | 시험 | `new CombatCommandState(…)` 139곳(74개 파일)에서 넷째 인자를 뺀다. `isNight`로 참을 넘기면서 턴을 넘기지 않은(배치 국면 기본값) 호출은 턴을 전투 국면으로 바꿔 같은 내용을 확인하게 한다. 개수와 파일을 보고에 적는다. `IsNight` 확인문은 `InCombat`으로 바꾸거나, 밤 전용 확인이면 지운다. `ArtifactConditionType.IsNight` 두 곳. |

**Ub(사용자 확인):** Core 시험 실행. 재생해서 전투 중 적이 죽으면 영혼 제단 영혼이 나오는지, 가시 심장 반사·보스 포효 피격 수가 지금과 같은지, 배치 = 낮 배경음·전투 = 밤 배경음이 그대로인지.

## 4. NR-c 계약 정리

| Task | 파일 | 내용 |
|---|---|---|
| Tc.1 | `Contracts/Combat/CombatTurnFacts.cs` | `PreparationIndex`·`MainPlacementLocked`·`NightStartPlayerTurnCount` 인자와 속성, `PlayerTurnsPerNight`, `CompletedPlayerTurnsThisNight`를 지운다. 새 생성자 순서는 TP-4. 요약 문구의 "밤"을 고친다. |
| Tc.2 | `Contracts/Common/PlayerTurnRules.cs`(+ `.meta`) | 상수 하나뿐이라 파일을 지운다. |
| Tc.3 | 턴 사실 생성 자리 | `CombatTurnCalculator`(다섯), `NexusReinforcementCalculator:84`, `BattlePreparation`, `CombatCommandState` 기본값, `StageSnapshot`. |
| Tc.4 | `Application/Combat/CombatPlacementCalculator.cs:26` | `MainPlacementLocked` 조건을 지운다. |
| Tc.5 | 아이템 | `Contracts/Items/ItemDamageContext.cs`·`ItemAttackContext.cs`·`ItemTemporaryEffect.cs`의 `PreparationIndex`, `CharacterItemState.cs:38` 비교, `Domain/Items/LanternItemRule.cs:24`·`RopeItemRule.cs:32` 생성 인자, `Application/Combat/CombatDamageApplication.cs`의 `PreparationIndex` 속성·생성자 인자·`For`, `MovementProjection.cs:115` 검사, `Contracts/Snapshots/StageSnapshot.cs`의 `PreparationIndex`. |
| Tc.6 | `ItemEventReason.RoundEnded` | 멤버를 지우고 번호를 비워 둔다. `ItemLifetimeCalculator.ClearRoundItems(source)`는 TP-6. |
| Tc.7 | `CombatEventKind.NightEnded` | `Contracts/Combat/CombatEvent.cs`에서 지우고 "번호 15 재사용 안 함" 주석. `CombatTurnEvent` 허용 종류와 `CombatChangeEvidenceReader`의 갈래에서 뺀다. |
| Tc.8 | Buffer `BufferSystems/Combat/CombatStateBuffer.cs:309-311` | 밤 시작 턴 수 검사를 지운다. |
| Tc.9 | View | `ViewSystem/Combat/Playback/Ordering/CombatEventTypeTable.cs:136` `NightEnded` 갈래, `Playback/Support/CombatEventSounds.cs:134-135` 갈래, `Playback/Support/CombatEnemyPresentationPacing.cs:89` 조건 하나. |
| Tc.10 | UI `GlobalSystem/UISystem/Screens/Stage/NightCheckerUI.cs` | TP-5대로 표시를 바꾸고 `_shownNightLength`를 지운다. 클래스 이름·프리팹은 그대로. |
| Tc.11 | 시험 | `new CombatTurnFacts(…)` 89곳(55개 파일, 위치 인자 7개 이상 25곳), 아이템 문맥 생성(`ItemDamageContext` 15·`ItemAttackContext` 10·`ItemTemporaryEffect` 15곳), `new CombatDamageApplication(` 6곳, `NightEnded` 14곳(10개 파일), `MainPlacementLocked` 13곳(8개 파일), `PreparationIndex` 21곳(15개 파일), `RoundEnded` 2곳, `CombatChangeEvidenceCoverageTests`(사건 종류 목록). |

**Uc(사용자 확인):** Core 시험 실행. 재생해서 `NightCheckerUI`가 "국면 · 완료 턴 수"로 보이는지, 랜턴·로프 임시 효과와 장착 아이템이 그대로 동작하는지.

## 5. NR-d 공유·문서

| Task | 내용 |
|---|---|
| Td.1 | 다른 작업자 변경 정리: T0.4 작성자별로 바꾼 줄과 이유를 짧은 메모로 채팅에 붙여 쓸 수 있게 드린다(파일로 만들지 않음). Core 전달 문서에도 절을 더한다. |
| Td.2 | 기획 공유 문구(채팅): 턴 상한 없음, 회수·재배치 없음, 보급품·장착 아이템·랜턴/로프 효과가 전투 끝까지, 영혼 제단·가시 심장 "밤" → "전투 중". |
| Td.3 | [적 전용 특성 설계](../specs/2026-11-10-enemy-death-traits-design.md)에서 Q-ED5·§4.6·검토 L5를 지우고 밤 삭제를 적는다. |
| Td.4 | 설계 문서에 구현 메모(계획과 다른 점, 지운 시험 사례 수). |

## 6. 계획에서 정한 것 (확인 대상)

| 번호 | 내용 |
|---|---|
| TP-1 | NR-a는 턴 사실 생성자의 99턴 검사만 지운다. 밤 턴 속성·상수는 UI가 읽으므로 NR-c에서 UI와 함께 지운다. |
| TP-2 | 낙석 예보는 밤 종료가 없으므로 늘 "다음 두 진영 종료까지"를 본다. |
| TP-3 | 전투 진입은 `isNight`를 넘기지 않는다. 턴을 `Combat`으로 바꾸는 것만으로 `InCombat`이 참이 된다. |
| TP-4 | 새 `CombatTurnFacts` 생성자: `(stagePhase, controllerPhase = None, controllerTurnSequence = 0, completedPlayerTurnCount = 0, enemyOrigins = null, summons = null, completedControllerTurnCount = 0)`. 남는 인자의 상대 순서는 지금과 같다. |
| TP-5 | `NightCheckerUI`: 문구 = `DescribePhase(...) + " · " + 전투 국면이면 CompletedPlayerTurnCount, 아니면 0`, 진행 막대 = 0. |
| TP-6 | `ClearRoundItems(source)`(사건 없는 겹치기)는 이유를 쓰지 않으므로 이유를 `BattleEnded`로 바꾼다. 부르는 곳이 시험뿐이면 겹치기를 지운다. 이름 `ClearRoundItems`는 이번에 바꾸지 않는다. |
| TP-7 | 지운 사건·이유 번호(`NightEnded = 15`, `RoundEnded`)는 주석으로 남겨 재사용을 막는다. |
| TP-8 | 다른 사람이 쓴 시험을 고쳐야 하면 고치기 전에 묻는다(T0.3). |
| TP-9 | 다른 작업자 메모는 채팅용 짧은 글로 드린다. Core 전달 문서 절은 기록용으로 함께 적는다. |

## 7. 위험

- 시험 고칠 곳이 많다(특히 Tb.9·Tc.11). 기계적인 수정이 대부분이지만, `isNight: true` + 기본 턴 조합은 의미가 바뀌므로 사례마다 확인한다.
- AI 작업 파일과 겹친다(T0.1). 커밋 전에 시작하면 같은 파일에 두 작업이 섞인다.
- View·UI 줄은 다른 작업자 영역이다. Q-NR1 B로 사용자가 허락했지만, 화면 동작이 지금과 같은지는 재생으로 확인해야 한다(Ub·Uc).

## 8. 실행 기록

### 8.1 NR-0 착수 전 확인 (2026-11-10, 코드 변경 없음)

- **T0.1 AI 작업:** 커밋됨(HEAD `3e7681411`). 남은 변경은 FMOD 캐시·글꼴 에셋·패키지 설정뿐이라 이 작업과 겹치지 않는다.
- **T0.2 기준 시험(HEAD `3e7681411`, 에디터를 끈 뒤 Claude가 일괄 실행, 2026-11-10):**
  - `ProjectL.Tests.Core` 5,493개: 통과 5,460 · 실패 25 · 건너뜀 8.
  - `ProjectL.Tests.CoreIntegration` 1,070개: 통과 1,032 · 실패 35 · 건너뜀 3.
  - 실패 60개 중 **39개가 밤 관련**이다. 밤 길이를 8턴으로 가정하거나 밤 종료·회수·재배치를 확인하는 시험이다. 2026-11-03 밤 길이 99 변경(`3a20dc9a3`) 뒤로 이미 깨져 있었다. Core 24개(`BossNightLifecycleTests` 6, `EightTurnNightTests` 3, `CombatItemLifetimeTests` 2, `PlayerTurnRuleTests.NightLengthIsEight`, `RailwayGimmickTests` 밤 사례 등), CoreIntegration 15개(`HunterPersistenceTests` 3, `MainSpecializationBattleTests` 3, `ControllerTurnCountPublicationTests`·`TornadoRuntimeIntegrationTests`·`TraitActivationPublicationTests` 등). NR-a~NR-c에서 지우거나 밤 부분을 걷는 대상이다.
  - **밤과 무관한 실패 21개**는 이 작업 범위 밖이다. Core 1개(`TraitActivationOutcomeTests.Taunt_OnlyAnAppliedForcedDecisionActivates(True)`, 기대 1·실제 2), CoreIntegration 20개(`DataBaseAssetTests` 7, `DeckMainChangePolicyTests` 8, `RunGoldFacilityTests` 2, `CombatCameraZoomPoseTests`·`CombatStructureInspectionTests`·`DeckSlotInfoPanelTests` 각 1). 이후 Phase는 이 21개를 기준 실패로 본다.
  - 실패 전체 목록: 스크래치 `baseline-failed.txt`, 결과 XML `core-results-baseline.xml`(세션 스크래치 폴더).
- **T0.3 고칠 시험 파일:** 120개. 그중 106개는 천명만 작성했다. 나머지 14개 중 바꿀 줄의 작성자가 다른 사람인 곳은 용근(`Droute`) 넷이다.

| 파일:줄 | 내용 | 바꾸는 성격 |
|---|---|---|
| `ControllerTurnCountTests.cs:15-20` | 이름 있는 인자로 턴 사실 생성 | 바꿀 것 없음(새 생성자도 같은 이름) |
| `ControllerTurnCountPublicationTests.cs:76-77` | 위치 인자로 턴 사실 생성 | 호출만 맞춤 |
| `RailwayGimmickTests.cs:117-118`, `:138-140` | 위치 인자로 턴 사실 생성 | 호출만 맞춤 |
| `RailwayGimmickTests.cs:109-131` `LastEnemyEndChecksRailwayBeforeRecall_AndTerminalStopsNightCleanup` | 마지막 적 국면 끝의 기차 → 밤 종료(`NightEnded` 1개·준비 번호 2) 확인 | **확인 내용 바뀜**: 밤 종료 확인 두 줄(`:128-129`)을 지우고, 기차 사건 순서·종료 확인은 남긴다. 이름에서 Recall·NightCleanup을 뺀다. |
| `TraitShieldTests.cs:205` | 이름 있는 인자 | 바꿀 것 없음 |
| `TraitShieldTests.cs:215-218` | 위치 인자로 턴 사실 생성 | 호출만 맞춤 |

  - 호출만 맞추는 곳도 다른 사람 줄이라 TP-8대로 사용자 확인 대상이다. → **허락(2026-11-10 사용자 "고쳐도 돼")**: 호출 맞춤 셋과 `RailwayGimmickTests` 밤 확인 두 줄 삭제·이름 변경. 변경은 NR-d 용근님 메모에 적는다.
- **T0.4 다른 작업자 줄 작성자(`git blame`):**

| 자리 | 작성자 |
|---|---|
| `NightCheckerUI.cs:70` | 천명(`d4a6d6fd`) |
| `NightCheckerUI.cs:71,76,77,78,79` | Twinsgm(10-18~10-27) |
| `NightCheckerUI.cs:72-75,80` | 용근(`11c07868`) |
| `CombatSceneDisplay.cs:219` | `Claude` 작성자 커밋 `2cf55c5f`(11-07, View 작업 세션) |
| `CombatEventTypeTable.cs:135-137` | 용근(`35562e96`) |
| `CombatEventSounds.cs:134-135` | 용근(`59c39023`·`c86b6759`) |
| `CombatEnemyPresentationPacing.cs:89` | 용근(`35562e96`) |
| `CombatStateBuffer.cs:309-311` | 천명(`d4a6d6fd`) |

### 8.2 NR-a 밤 종료 흐름 삭제 (2026-11-10, 시험 일괄 실행으로 확인)

- **코드:**
  - `CombatTurnCalculator`에서 `EndsNightAfterEnemyPhase`·`EndNight`와 `EndEnemyPhase`의 밤 종료 합치기를 지웠다. 적 국면 종료는 기믹·정리 결과를 그대로 돌려준다. 클래스 요약도 고쳤다.
  - `CombatPlacementCalculator.RecallForNight`, 밤 종료만 부르던 `HunterApplication.Reconcile(input, characters, state, events)` 겹치기를 지웠다.
  - `StageHazardForecast:361`은 늘 `current + 2`(TP-2).
  - `CombatTurnFacts` 생성자의 99턴 검사 조건만 지웠다(TP-1). 밤 턴 속성·상수는 NR-c까지 남는다.
- **시험(사용자 지시 2026-11-10: 의미가 적은 시험은 고치지 말고 지운다):**
  - 파일 삭제(`.meta` 함께): `BossNightLifecycleTests`(6), `EightTurnNightTests`(3), `PlayerTurnRuleTests`(1).
  - 메서드 삭제 Core: `CombatCommonCommandTests.NightRecallCapacityFailure_LeavesFactsUnchanged`, `CombatItemLifetimeTests`의 `OutgoingEnvironmentalDamage_ActivatesWhiskeyBeforeRoundExpiry`·`RoundEnd_ClearsPlayerAndEnemyItems_AndRedeploymentDoesNotRegrant`, `RailwayGimmickTests.LastEnemyEndChecksRailwayBeforeRecall_AndTerminalStopsNightCleanup`(용근님, 허락분), `MainDeckInitialPlacementTests.FirstNightEnd_…`, `PositionTileModifierTests.NightRecallAndPlacement_…`, `RegenerationCadenceTests.LastEnemyEnd_…`, `RegenerationTurnTests.EighthEnemyCompletion_…`, `TargetingAngleLifecycleTests.RecallRedeploy_…`, `NexusReinforcementLifecycleTests.NightRecall_…`, `EnemyHazardForecastTests.LastNightStopsAfterImmediateHazardBeforeRecall`(+ 도우미의 `lastNight` 인자), `EnemyForecastBindDecisionTests.RecallRemovesOnlyFutureBind`(+ 도우미의 `lastNight` 인자).
  - 메서드 삭제 CoreIntegration: `ArmorBreakerPublicationTests.ActualNightRecall_…`, `CombatAdvanceServiceTests.TwoNights_…`, `HunterPersistenceTests`의 `EnterCombat_LateHunterSelectionAndRngPublishTogether`·`NightAndRecall_…`, `MainSpecializationBattleTests.NightRecallAndStageTeardownKeepOwnedClass`, `RegenerationPublicationTests.NightRecall_…`, `RockfallPublicationTests.ReturnCleansesBeforeAnOpportunityWithoutHiddenState`, `TornadoRuntimeIntegrationTests.SameBattleReturnToDeployment…`, `TraitActivationPublicationTests.EnterCombat_LateHunterResultRereadDoesNotDrawAgain`, `TraitGrowthStatSettlementTests.RankedDefault_RemainsInstanceLocalAcrossRecall…`, `TraitMovementAttackPublicationTests.Acceleration_…NightRecallClearsIt`, `NexusReinforcementPublicationTests.DbStage_CompletesReinforcementLifetimeAndStartsFreshOnNextNode`.
  - 밤 경우 한 줄만 삭제: `HeldEnvironmentEffectTests`·`HuntersMarkStateTests`·`IntimidationStatePreservationTests`·`WeaknessExposureLifecycleTests`의 `[TestCase(8, true)]`, `WeakGroundTurnTests`의 `[TestCase(Enemy, 8)]`. 시험 본문은 그대로다.
  - 확인문만 삭제: `NexusReinforcementStateTests.Copies_PreserveProvenanceAndRandomState`의 밤 종료 확인 네 줄과 `completed: PlayerTurnsPerNight` 인자.
  - 남김: `ControllerTurnCountPublicationTests.RealProgressCountsEachSideOnce_PreservesAcrossDayAndNewNight`. 용근님이 통째로 쓴 시험이라 지우기 전에 묻는다(기준선에서도 실패).
- **확인(일괄 실행):** Core 5,463개 중 실패 1(기준 `Taunt…`), CoreIntegration 1,055개 중 실패 21(기준 20 + 위 남긴 시험 1). 기준선에 없던 새 실패 0.
- **계획과 다른 점:**
  - 처음 돌릴 때 `EnemyForecastBindDecisionTests`가 여러 줄 호출로 `lastNight`를 넘겨 컴파일 오류가 났다(검색이 놓침). 고쳤다.
  - 일괄 실행을 `Start-Process -Wait`로 기다리면, 컴파일 뒤 남는 C# 컴파일 서버(`VBCSCompiler`)까지 기다려 멈춘다. Unity 프로세스만 기다리게 바꿨다(`WaitForExit()`).
- **추가(2026-11-10 사용자 "지워도 돼"):** 남겨 둔 용근님 시험 `ControllerTurnCountPublicationTests.RealProgressCountsEachSideOnce_PreservesAcrossDayAndNewNight`도 지웠다. 이제 CoreIntegration 실패는 기준 20개뿐일 것으로 본다(이 삭제 뒤 일괄 실행은 하지 않음). NR-d 용근님 메모에 적는다.

### 8.3 NR-b `IsNight` → `InCombat` (2026-11-10, 시험 일괄 실행으로 확인)

- **코드:**
  - `CombatCommandState`: `IsNight` 필드·생성자 인자·`With(isNight)`·`SameGameplayValues` 비교를 지우고 `InCombat => Turn.StagePhase == StagePhase.Combat`을 더했다.
  - 생성·복사 자리: `BattlePreparation`(둘), `MovementProjection` 상태 복사, `CombatTurnCalculator.State`, `MovementProjection.Complete/BeginComplete/CompletionWork`의 `isNight` 인자, 전투 진입의 `isNight: true`.
  - 규칙 인자 `isNight` → `inCombat`: `SoulAltarRule`, `ThornHeartRule`, `SoulEffectCalculator.PlanDeaths`, `ThornHeartAttackScope`, `CommandDeathSettlement`, `RoarHitCounter`. 부르는 곳은 `State.InCombat`을 넘긴다(`AttackDirectDamageCalculator`, `AttackForcedMovement`, `KillChainAttackCalculator`, `MovementProjection`, `ObstacleReactionCalculator`, `AttackDeliveryExecutor`, `BossRoarCalculator`, `CombatCommandCalculator`).
  - **계획에 없던 자리:** `RoarTurnApplication.cs:25`(커밋 `97c378b3d`에서 새로 생긴 포효 수 턴 종료 감소)도 `IsNight`를 읽어 같이 바꿨다.
  - 비교 삭제: `MovementUndoPolicy:70`, `MovementPreviewContext.IsNight`와 `MovementContextCalculator:142`.
  - 유물 조건 `ArtifactConditionType.IsNight` → `InCombat`(값 2 그대로), 컴파일러 문구. `IsDay`는 이름을 두고 주석만 고쳤다(쓰는 유물 없음, 시험에서 "틀린 조건" 예로만 씀).
  - View `CombatSceneDisplay:214`(작성 당시 `:219`) `ApplyPhase(state.CommandState.InCombat)`. `CombatBgmPolicy.ApplyPhase(bool isNight)` 인자 이름은 그대로(담당자 결정, 설계 P8).
- **시험:**
  - `new CombatCommandState(…)` 넷째 인자 제거(perl 일괄 + 손으로 넷: `BossRoarTests` 둘, `CellEnvironmentTestData`, `ThornHeartReflectionDirectTests`, `WallStateLifecycleTests`의 `new(…)`, `PeriodicStatusTurnTests`).
  - `With(isNight: …)` 인자 제거 다섯 곳, `RoarHitCounter.Record(…, isNight:)`·`ThornHeartAttackScope(…, isNight:)` 이름 있는 인자 → `inCombat:`, `HunterTraitTests:41` 확인문 → `InCombat`, `ContractShapeGuardTests` 속성 목록 `IsNight` → `InCombat`.
  - 낮/밤 비교 시험 삭제(사용자 지시: 의미가 적으면 지움): `BossHitCounterTests.DayAttack_DoesNotCount`, `BossRoarTests.FourHits_SurviveDay_ThenOneHitRoars`, `ThornHeartReflectionCommandTests.DayAttack_DoesNotReflect`. 도우미의 `night` 인자 제거(`BossRoarTests.Input`·`WithNight` 삭제, `ThornHeartTestData.Input`). 규칙 단위 시험 `ThornHeartReflectionScopeTests.DayOrNonOrdinaryExecution_DoesNotReflect`는 규칙 인자 그대로라 남겼다.
  - **동결 기준 데이터(천명 작성, `docs/superpowers/reports/2026-11-07-enemy-utility-ai/`):** 직렬화가 공개 속성 이름을 알파벳 순으로 쓰므로 `IsNight` 키가 `InCombat`으로 바뀌어 비교 시험 17개가 실패했다. 다시 뜨지 않고 옛 기준에서 키 이름만 바꿨다: `phase-2-preview-reference.json` 430곳, `phase-2-movement-reference.json` 20곳, `phase-4-route-reference.tsv` 21곳(값은 모두 `true`, 줄 끝 형식 그대로). 이후 비교 시험이 모두 통과해 **계산 결과는 키 이름 말고 바뀐 것이 없음**을 확인했다. 원본은 세션 스크래치 `ref-backup`에 둠.
  - AI 기준 추적(`baseline-traces.tsv`)은 `EnemyAiBenchmarkFixtures.Describe` 문자열 해시로 찾는다. `night=` 표기를 남기고 값만 `InCombat`에서 읽어 해시를 지켰다(주석으로 이유를 남김).
  - 계획에서 걱정한 "`isNight: true` + 기본(배치) 턴" 조합으로 의미가 바뀐 시험은 나오지 않았다.
- **확인(일괄 실행):** Core 5,460개 중 실패 1(기준 `Taunt…`), CoreIntegration 1,054개 중 실패 20(기준 20). 새 실패 0.

### 8.4 NR-c 계약 정리 (2026-11-10, 시험 일괄 실행으로 확인)

- **턴 사실:** `CombatTurnFacts`에서 `PreparationIndex`·`MainPlacementLocked`·`NightStartPlayerTurnCount`·`PlayerTurnsPerNight`·`CompletedPlayerTurnsThisNight`를 지웠다. 생성자는 TP-4 순서(`stagePhase, controllerPhase, controllerTurnSequence, completedPlayerTurnCount, enemyOrigins, summons, completedControllerTurnCount`), 완료 플레이어 턴 음수 검사를 더했다. `PlayerTurnRules.cs`(+ `.meta`)를 지웠다. 생성 자리: `CombatTurnCalculator` 넷, `NexusReinforcementCalculator`(나머지는 이름 있는 인자라 그대로).
- **메인 배치 잠금:** `CombatPlacementCalculator` 배치 미리보기 조건에서 지웠다.
- **준비 번호:** `ItemTemporaryEffect`·`ItemDamageContext`·`ItemAttackContext`의 속성·생성자 인자·검사, `CharacterItemState` 비교, 랜턴·로프 생성 인자, `CombatAttackCalculator`의 문맥 생성, `CombatDamageApplication`의 속성·생성자 인자·`For`, `MovementProjection`의 준비 번호 일치 검사. `StageSnapshot`의 `PreparationIndex`·`MainCharacterPlacementLocked`와 그 인자만 받던 겹치기 생성자 하나도 지웠다(계약은 남김, 설계 P7).
- **사건·이유:** `CombatEventKind.NightEnded`를 지우고 다음 값 `GimmickActivated = 16`을 명시(번호 15 재사용 안 함 주석). `CombatTurnEvent` 허용 종류·변화 근거 판독기 갈래에서 뺐다. `ItemEventReason.RoundEnded`를 지우고 `BattleEnded = 8`을 명시(번호 7 재사용 안 함 주석), 폐기 사건 검사에서 뺐다. 사건 없는 `ItemLifetimeCalculator.ClearRoundItems(source)` 겹치기는 시험만 불러 지웠다(TP-6). 이름 `ClearRoundItems`는 그대로.
- **Buffer:** `CombatStateBuffer`의 밤 시작 턴 수 검사를 지웠다.
- **View(용근님 줄):** `CombatEventTypeTable`의 `NightEnded → Morning` 갈래, `CombatEventSounds`의 `NightEnded → 낮 배경음` 갈래, `CombatEnemyPresentationPacing`의 `NightEnded` 조건.
- **UI `NightCheckerUI`(Twinsgm·용근님·천명 줄):** "국면 · 완료 플레이어 턴 수"만 보이고 진행 막대는 0, `_shownNightLength` 삭제(TP-5).
- **시험:**
  - 생성자 인자 제거를 괄호 짝을 맞추는 perl 스크립트로 했다(지운 위치·이름 인자만 뺌): `CombatTurnFacts` 17곳, `ItemTemporaryEffect` 14, `ItemDamageContext` 15, `ItemAttackContext` 10, `CombatDamageApplication` 6. 나머지 턴 사실 호출은 이름 있는 인자라 그대로였다.
  - 확인문만 삭제: 준비 번호·메인 배치 잠금·밤 시작 턴 수를 보던 줄(`CombatCommonCommandTests` 넷, `HunterTraitTests` 둘, `CombatItemContractTests` 하나, `MainDeckInitialPlacementTests` 둘). `CombatCommonCommandTests.EnterCombat_LocksMainAndDefersUnitsUntilPlayerStart(int)`는 준비 번호 경우 둘이 같아져 `EnterCombat_DefersUnitsUntilPlayerStart()` 하나로 했다.
  - 준비 번호 음수 거절을 보던 경우 삭제: `CombatItemContractTests`의 공격 문맥·피해 문맥 한 줄씩, `TemporaryEffect_RejectsInvalidDomains`의 `(3, Priority, 10, -1)` 경우, `State_CopiesEffects…`의 "준비 번호만 다르면 다른 값" 확인 한 줄.
  - 메서드 삭제: `CombatDamageApplicationTests.SharedProjection_RejectsBoundaryWithDifferentPreparationIndex`(지운 검사), `CombatItemLifetimeTests.ClearRoundItems_IsIdempotentAndPreservesOtherFacts`(지운 겹치기, 다음 시험이 같은 정리를 확인).
  - `NightEnded`·`RoundEnded` 값 빼기: `BossTurnTests`·`CombatCommonCommandTests`·`IntimidationScheduleTests`의 "밤 종료 사건 없음" 조건, `MovementUndoLifetimeTests`의 `[TestCase(NightEnded)]`, `WeakGroundStateTests` 사건 목록(번호 호환 확인에 15를 빈 번호로 더함), `CombatChangeEvidenceCoverageTests`·`CombatChangeEvidenceVariantTests` 목록.
  - **동결 기준 데이터:** 지운 턴 속성 키 다섯 개가 옛 기준에 남아 비교 시험 6개가 실패했다. 키만 걷었다(각 471곳 = 턴 사실 개수와 같음). 이후 모두 통과해 계산 결과는 바뀌지 않았음을 확인했다. 원본은 스크래치 `ref-backup-nrc`.
- **확인(일괄 실행):** Core 5,454개 중 실패 1(기준 `Taunt…`), CoreIntegration 1,054개 중 실패 20(기준 20). 새 실패 0.
