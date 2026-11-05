# 특성 22종 리뷰 후속 수정 설계

- 작성: 2026-11-02 (Core, 천명 세션)
- 상태: **T-a~T-e 완료(2026-11-02, Core 시험 5480개 중 실패 17 = 이번 작업 전과 같음, 커밋 전)**. 결정: 사용자 "A 공격마다(추가 포함)" = Q1 A, Q2~Q4 권장대로. 먼지 연막 도착 칸 1차 수정은 F1이 대신한다.
- 기준: [특성 시스템 설계](2026-10-28-trait-system-design.md), 사용자 리뷰 답변(2026-11-02), `feature-change-locality-guidelines.md`(수정·이해·검증 범위 평가)
- 범위: Core 계산기(`CoreCaculator/`)와 우리 Core 시험. View·Framework 무변경.

## 0. 사용자 결정 요약

| 항목 | 결정 | 이 문서 |
|---|---|---|
| 무장 해제·발목 부수기 + 희생 | 대신 맞은 유닛(수호자)이 제한을 받는다. 수호자의 카테고리 피해 배율도 적용 | F2 |
| 캐논 배율이 아군에게도 | 기획 의도, 수정 안 함 | — |
| 가속이 밤 회수 때 0 | 의도, 수정 안 함 | — |
| 먼지 연막·땅 다지기 흔적 | 출발 칸·도착 칸에 생기지 않는다 | F1 |
| 뛰어넘기 중간 칸 진입 효과 | 의도 | — |
| 도발을 지킬 수 없을 때 | AI 판단상 가장 이득인 대상을 공격 | F3 |
| 처치 연쇄 추가 공격과 깔끔한 뒷정리·선봉장 | 추가 공격도 센다. 선봉장은 1회만 | F4 (Q1) |
| 일점 포격의 적 수 | 추가 공격은 자기 대상으로 다시 센다 | F5 |
| 포효가 공격 행동으로 세어짐 | 적에게 그 특성을 주지 않으므로 수정 안 함 | — |
| 발목 부수기·낙석 묶기 같은 키 | 의도 | — |
| 밤 회수 뒤 방어막 0 | "밤 회수가 없어졌다" | — (Q4) |
| 상태 목록 누적 | 의도 | — |

## F1. 흔적은 지나간 칸에만 (먼지 연막·땅 다지기)

**변경 사례:** 흔적을 남기는 칸 규칙을 "출발·지나간·도착"에서 "지나간 칸만"으로 바꾼다.

**지금 코드(확인):** `MovementProjection.BeginOrdinaryMove`(출발 칸)·`Enter`(들어간 칸)·`ExchangeEndpoint`(교대 끝점)가 `TraitTrailCalculator.EmitAt`을 부른다. 작업 트리의 1차 수정은 먼지 연막만 "떠난 칸"으로 옮겼다(출발 칸은 남음).

**설계:**
- 정의: 지나간 칸 = 이번 자발 이동에서 **들어갔다가 다시 떠난 칸**. 출발 칸(들어간 적 없음)과 멈춘 칸(떠나지 않음)은 빠진다. 중간에 끊겨 멈춘 칸도 도착 칸으로 본다.
- 생성 시점: 그 칸을 떠나는 걸음 뒤(`Enter`·`ExchangeEndpoint`에서 직전 칸이 출발 칸이 아니면 두 흔적 모두 생성). `BeginOrdinaryMove`는 흔적을 만들지 않는다.
- 규칙 원본은 `TraitTrailCalculator` 한 곳(어느 칸에 남기나 + 무엇을 남기나). `MovementProjection`은 "떠난 칸" 사실만 넘긴다. 두 특성이 같은 칸 규칙을 쓰므로 1차 수정의 키 인자는 걷는다.
- 벽·낭떠러지 등 자발 진입 불가 칸은 지금처럼 건너뛴다(뛰어넘기).

**결과:** 한 칸 이동은 흔적이 없다. 두 칸 이동은 가운데 한 칸. 땅 다지기의 "흔적 위에서 출발하면 +1"은 보유자가 다음 턴에 자기 흔적 위에서 시작하지 않게 되어 사실상 다른 유닛용이 된다(사용자 결정의 직접 결과).

**범위 평가:**
- 수정: `TraitTrailCalculator`, `MovementProjection` 세 자리. 이동 범위·AI·되돌리기는 흔적 사실을 읽기만 해서 무변경.
- 이해: 흔적 생성 순간(사건 순서)과 되돌리기 영수증(`MovementTrailReceipt`)이 "새로 생긴 흔적"만 본다는 계약.
- 검증: 흔적 개수·칸·ID 순서를 단정하는 우리 시험(`TraitTrailTests`·`LeapTraitTests`·`TraitActivationMovementTests`·`TraitTrailPublicationTests`·`LeapPublicationTests`·`MovementEffectUsageTests` 생산자 경로·`MovementUndoLifetimeTests`), 위치 교대.
- 판단: 필요한 연결. 구조 변경 없음.

## F2. 희생이 대신 맞으면 제한도 수호자에게

**변경 사례:** 무장 해제·발목 부수기를 받는 유닛을 "원래 대상"에서 "실제로 피해를 받은 유닛"으로 바꾼다.

**지금 코드(확인):**
- `AttackDirectDamageCalculator.FlushHealth:151`이 `TraitRestrictionApplication.AfterContact(plan, world, hit.Id, …)`로 원래 대상을 넘긴다. `AfterContact`는 "그 ID가 지정 대상(`SelectedTargetId`)일 때만" 건다.
- 희생 모의 계산 `SacrificeDamagePlanner.ApplyHit`의 주석이 "접촉·표식의 원래 대상은 유지하고 숫자 피해의 방어 수신자만 바꾼다"로 이것을 의도로 적어 두었다. 이번 결정으로 무장 해제·발목 부수기는 이 규칙에서 빠진다.
- **카테고리 피해 배율은 이미 수호자 기준이다.** 직접 피해(`AttackDirectDamageCalculator`의 `receiver`)·희생 모의(`ApplyHit`의 `facts[recipientId]`)·처치 연쇄(`projected[recipientId]`) 모두 `CombatDamageCalculator.Attack`에 수호자 사실을 넘기고, 상성은 `TargetDamagePipeline.Calculate(…, victim?.Stats.Category.Key, …)`가 그 사실의 카테고리로 계산한다. 그래서 이 부분은 코드 변경이 없다(Q2로 확인).

**설계:**
- 판정 조건 "지정 대상에게 접촉했는가"는 원래 대상으로 그대로 본다(누가 공격받았는지). 받는 유닛만 수호자로 바꾼다. `AfterContact(plan, world, targetId, recipientId, origin)`.
- 적대·생존 검사는 받는 유닛(수호자) 기준.
- 사건(`StatusApplied`)의 대상도 수호자.
- 처치 연쇄 추가 공격에는 제한을 거는 경로가 없다(지금도 없음). 바꾸지 않는다.
- 희생 모의 계산(`ApplyHit`)은 제한을 모의하지 않는다. 제한은 치명 판정에 영향이 없어 그대로 둔다.

**범위 평가:**
- 수정: `TraitRestrictionApplication.AfterContact` 서명, 호출 한 곳.
- 이해: 희생의 "원래 대상 / 실제 수신자" 두 ID 계약.
- 검증: 무장 해제·발목 부수기 시험, 희생 시험(제한을 가진 공격자가 있는 사례가 있으면 기대값 변경).
- 판단: 필요한 연결.

## F3. 도발을 지킬 수 없으면 기존 AI 판단

**변경 사례:** 도발당한 적이 도발자를 칠 수도, 도발자에게 갈 수도 없을 때의 행동을 정한다.

**지금 코드(확인):** `EnemyAiTurnService.Calculate`는 적마다 공격 고르기 → 못 하면(이동 가능할 때) 추적 고르기 순서다.
- `EnemyAiPolicy.TrySelectAttackAim`: 도발자가 있고 칠 수 없으면 바로 false(`:56`).
- `TrySelectPursuitTarget`: 도발자에게 길이 없으면 false(`:126-131`).
- 그래서 둘 다 안 되면 그 적은 아무것도 하지 않는다.

**설계:**
- "도발을 지킬 수 있나"를 한 판정으로 둔다(`TauntTargeting` 또는 정책 안 한 함수): 도발자 중 하나를 지금 칠 수 있거나, 아직 이동할 수 있고 도발자 중 하나에게 추적 경로가 있으면 지킬 수 있다.
- 지킬 수 있으면 지금과 같다(칠 수 있으면 도발자를 포함하는 조준 중 최선, 아니면 도발자 쪽으로 이동).
- 지킬 수 없으면 **도발이 없는 것처럼** 기존 공격 점수(아군 오인 적음 → 유닛 우선 → 피해 큼 → 타격 수 …)로 고른다. 이때 도발 발동 사건(`TauntSelected`)은 내지 않는다.
- 이미 이동한 적이 이동 뒤에도 도발자를 칠 수 없으면 "지킬 수 없음"이 되어 다른 대상을 친다(지금은 행동 없음).
- Q3: 지킬 수 없고 칠 대상도 없을 때 추적도 기존 규칙(가장 가까운 적)으로 할지.

**범위 평가:**
- 수정: `EnemyAiPolicy` 두 함수(같은 판정 사용).
- 이해: AI 한 행동 계산 순서(공격 → 추적), 도발 사건 기록.
- 검증: 도발 AI 시험(지킬 수 있음 사례는 그대로, 지킬 수 없음 사례 기대값 변경).
- 판단: 판정이 두 함수에 따로 있던 것을 하나로 모으는 책임 명확화. 새 계층 없음.

## F4. 처치 연쇄 추가 공격도 공격 행동으로 센다

**변경 사례:** 처치 연쇄 추가 공격 한 번을 공격 행동 한 번으로 센다.

**지금 코드(확인):** 공격 행동 알림 `TraitAttackNotifications.Apply`(진영 공격 횟수 +1, 선봉장·깔끔한 뒷정리 평가)는 `CombatAttackCalculator.Execute:99` 한 곳에서만 불린다. `KillChainAttackCalculator.Resolve`는 부르지 않는다.

**설계(Q1이 A일 때):**
- 추가 공격 한 번마다 대상 선택 뒤·피해 계산 전에 같은 알림을 낸다. 행위자는 추가 공격자.
- 선봉장: 진영 첫 공격만이라 추가 공격은 영향 없음(사용자 "1회만").
- 깔끔한 뒷정리: 보유자가 아닌 아군의 추가 공격마다 +1. 처치 연쇄가 이어지면 반복 발동.
- 알림 입구가 `CommandAttackProjection`을 받는데 처치 연쇄는 도착 투영(`MovementProjection`)에서 돈다. `CommandAttackProjection.TraitTurn`이 이미 도착 투영에 위임하므로, 처치 연쇄 호출자(`AttackDeliveryExecutor`)가 `world`를 넘기거나 알림 입구를 (캐릭터 사전, 사건, `TraitTurn` 소유자)로 받게 나눈다. 구현 때 둘 중 작은 쪽을 고르고 계획에 적는다.

**Q1(필수):** 사용자 답 "깔끔한 뒷정리는 적이 사망한 횟수 그 자체를 센다"와 "추가 공격은 카운트로 센다"가 서로 다른 규칙으로 읽힌다.
- A. 지금처럼 "다른 아군의 공격 행동마다 +1"이고, 추가 공격도 공격 행동으로 센다(설명 문구 그대로). — 권장(지금 에셋 설명과 같음)
- B. "적이 죽을 때마다 +1"로 발동 조건을 바꾼다(처치자 무관? 보유자 처치 포함?). 설명 문구·View 설명도 바뀐다.

**범위 평가:**
- 수정: `KillChainAttackCalculator`, `AttackDeliveryExecutor`(인자), 필요하면 `TraitAttackNotifications` 입구.
- 이해: 진영 공격 횟수(`TraitTurnFacts`)의 소유자와 갱신 순서.
- 검증: 선봉장·깔끔한 뒷정리 시험, 처치 연쇄 시험(사건 순서가 바뀜 — 발동 사건이 피해 사건 앞에 끼어듦).

## F5. 일점 포격은 추가 공격마다 적 수를 다시 센다

**변경 사례:** 처치 연쇄 추가 공격의 "범위 안 적 수"를 그 추가 공격의 대상으로 다시 센다.

**지금 코드(확인):** `AttackTraitScope.CapturedEnemyCount`는 원래 공격 시작 때 한 번 세고, 처치 연쇄가 `parent.TraitScope`를 그대로 넘긴다(`KillChainAttackCalculator.cs:78·83·94·114`). 같은 묶음에 가속 포착값(`_capturedAcceleration`)이 함께 들어 있어 묶음 전체를 새로 만들면 가속을 두 번 빌리게 된다.

**설계:**
- 처치 연쇄 추가 공격은 대상이 늘 1명이다. 추가 공격용 묶음은 부모의 가속 포착값을 그대로 두고 적 수만 바꾼 사본으로 만든다(`AttackTraitScope.WithCapturedEnemyCount(int)` — 같은 부모·가속, `Finish`는 부모만).
- 결과: 사용자 예시(3명 타격 → 2명 사망 → 추가 공격 → 일점 포격 발동) 그대로, 추가 공격은 늘 "1명" 조건을 만족한다.
- 지금 데이터에서는 처치 연쇄(메인)와 일점 포격(캐논)이 한 유닛에 없어 화면 변화는 없다.

**범위 평가:**
- 수정: `AttackTraitScope`(사본 함수 하나), `KillChainAttackCalculator`(넘기는 묶음).
- 이해: 가속 포착값의 빌림·반납(`Finish`) 계약 — 사본은 반납하지 않는다.
- 검증: 일점 포격·가속 시험.

## F6. 희생이 대신 맞으면 피해와 함께 붙는 효과도 모두 수호자에게 (2026-11-02 추가)

**결정(사용자):** A — 피해와 함께 붙는 효과는 디버프까지 모두 수호자(실제 수신자)가 받고 원래 대상은 아무것도 받지 않는다. 넉백·충격 볼트 밀치기도 같다. 끌어오기는 원래 대상 유지(피해 전에 대상을 옮기는 공격 모양이라 희생 판단보다 먼저 일어남).

**연출과의 관계:** View에서 수호자가 보호 대상 앞까지 나가 맞고 돌아오므로, "맞은 유닛이 효과도 받는다"가 화면과 맞다.

**넉백(코드 확인):** Heavy 카테고리 넉백 면역 상수는 꺼져 있지만(`KnockbackImmunityPolicy.HeavyCategoryImmunityEnabled = false`), Heavy 카테고리 기본 특성이 굳건한 의지다(`CategoryConfig_Default.heavyDefaultTrait` → `TraitGrowth_SteadfastWill`, `BattleDefinitionAdapter`가 카테고리 기본 특성으로 붙임). 희생은 Heavy 특성이므로 수호자는 늘 굳건한 의지를 가진다 → 보통 넉백 규칙대로 수호자에게 넉백을 적용하면 막히고, 원래 대상도 안 밀린다. 특별 규칙("희생이면 넉백 없음")은 두지 않는다.

**지금 원래 대상에게 가는 것과 바꿀 곳(코드 확인):**

| 효과 | 지금 | 바꿀 곳 |
|---|---|---|
| 넉백·충격 볼트 | 원래 대상 | `AttackKnockbackCalculator` 후보: 원래 대상 키(`ledger.Roles`, `Delivery.Hits.TargetId`) → 실제 수신자(접촉 기록의 수신자) |
| 위압 | 원래 대상 | `AttackDirectDamageCalculator`의 `deliveredVictims.Add(id)` → 수신자. 소비자는 위압(`CombatDebuffApplication.CompleteAttack`)과 처치 연쇄뿐 |
| 사냥꾼의 표식(약점 노출) | 원래 대상 | `FlushHealth`의 `WeaknessExposureApplication.CompleteHit(…, hit.Id)` → `hit.Recipient`, 희생 모의 `ApplyHit`도 같게 |
| 공격자 장착 아이템의 공격 뒤 발동 후보 | 원래 대상·위치 | `ItemDirectHits` 키·위치 → 수신자 |
| 포효 피격 횟수 | 원래 대상 | 그대로 — 보스(적)가 맞을 때만 세고, 희생은 아군만 지켜서 겹치지 않음 |

**View 영향(공유 필요):** 수호자에게 위압·표식 상태 사건, 아이템 발동, 굳건한 의지 발동(넉백 막음) 사건이 생길 수 있다. 원래 대상에게는 생기지 않는다. 10-30 View 결정 "대신 맞으면 밀리지 않음"과 Core 요청 R-AA1(지켜진 유닛을 미는 문제)은 이 변경으로 풀린다.

**범위 평가:** 수정 = 위 네 곳(+ 희생 모의 한 줄). 이해 = 희생의 원래 대상/실제 수신자 두 ID 계약. 검증 = 희생·넉백·충격 볼트·위압·표식·아이템 시험, 새 사례(위압·표식·넉백이 수호자에게).

## 6. 설계 문서 갱신

[특성 시스템 설계](2026-10-28-trait-system-design.md)에서 바꿀 줄(구현과 같은 Phase에서):
- R1·R2 자리: "지나간 칸만(출발·도착 제외)". 땅 다지기 만료 "만든 진영의 다음 자기 턴 종료"(코드에 맞춤).
- H4 도발: 지킬 수 없을 때 기존 AI.
- C2 깔끔한 뒷정리·§2.1: Q1 결과.
- A6 일점 포격: 추가 공격은 자기 대상으로 다시 센다.
- H3 희생 + C6·C7: 제한은 실제 수신자.
- H1 위치 교대·R5 정찰대: "보류" 표시 걷기. 캐논 아군 배율·가속 밤 회수·포효·뛰어넘기·`Bind` 공유는 "의도" 한 줄씩.

## 7. Phase(안)

| Phase | 내용 | 멈춤 |
|---|---|---|
| T-a | F1 흔적 + 시험 기대값 — **코드 완료(2026-11-02, 컴파일·Test Runner 미확인)**: `TraitTrailCalculator.EmitOnLeave` 하나(출발 칸·막힌 칸 제외), `MovementProjection.Enter`·`ExchangeEndpoint`가 떠난 칸을 넘김, `BeginOrdinaryMove` 호출 삭제. 우리 시험 6파일 기대값·경로 맞춤. `MovementUndoLifetimeTests.NewTrailAtBOrigin…`은 한 칸 이동이라 흔적이 안 생겨도 통과하지만 "새 흔적" 경우는 더 확인하지 않음 | Test Runner 결과 |
| T-b | F2 제한 수신자 + F5 적 수 사본 — **완료(2026-11-02, Core 시험 5479개 중 실패 17 = 기존과 같음, 새 사례 3 통과)**: `TraitRestrictionApplication.AfterContact(…, targetId, recipientId, …)`, `AttackTraitScope.WithCapturedEnemyCount`, 처치 연쇄가 추가 공격마다 적 수 1 사본 사용. 새 시험 `SacrificeTraitTests.Restriction_GoesToSacrificer_NotOriginalTarget`(2)·`CannonThresholdTraitTests.Focused_KillChainRecountsItsOwnSingleTarget`, 기존 `Focused_CountFrozenAtActionStart_ForRepeatedKillChain` → `Focused_SingleTargetKillChain_KeepsBonus`(이름만) | 〃 |
| T-c | F3 도발 — **완료(2026-11-02, Core 시험 5480개 중 실패 17 = 기존과 같음)**: `EnemyAiPolicy.ReachableTaunter` 하나를 공격·추적 고르기가 같이 씀, 지킬 수 없으면 기존 공격 점수·기존 추적(Q3). 시험 `TauntTraitTests.TauntedEnemy_MovesWithoutFundingOrEndsIfAlreadyMoved` → `…MovesTowardTaunter_OrAttacksOthersIfAlreadyMoved`(이동한 경우 기대값 바뀜), 새 `TauntWithoutPathOrAttack_FallsBackToOrdinaryAttack` | 〃 |
| T-d | F4 추가 공격 알림(Q1 뒤) — **완료(2026-11-02, Core 시험 5480개 중 실패 17 = 기존과 같음)**: `KillChainAttackCalculator.Resolve` 끝 인자 `world`, 추가 공격마다 대상 선택 뒤 `TraitAttackNotifications.Apply`(진영 공격 횟수 +1, 깔끔한 뒷정리 반복 발동, 선봉장은 첫 공격만). `AttackDeliveryExecutor`가 `world`를 넘김. 시험: `TraitAttackNotificationTests.RepeatedHits_CountOneAction` → `KillChainExtraAttacks_EachCountAsAction`(기대값 1 + 추가 공격 수), 공격 횟수 한 줄씩 맞춤 `LegacyEnhancementRetirementTests.NativeKillChain_…`·`TraitActivationOutcomeTests.RepeatedStrike_…`·`TraitKillRewardTests.Morale_KillChain…`·`TraitMovementAttackTests.WholeMultiHitAction_…` | 〃 |
| T-f | F6 희생 수신자 효과 일반화(넉백·위압·표식·아이템) — **완료(2026-11-02, Core 시험 5482개 중 실패 17 = 기존과 같음)**: `AttackDirectDamageCalculator`(위압 대상·아이템 후보·표식 = 수신자), `SacrificeDamagePlanner.ApplyHit` 표식 = 수신자, `CommandAttackLedger.ActualRecipients` + `AttackKnockbackCalculator`(넉백·충격 볼트 후보 = 수신자), **계약 변경** `AttackResult.ValidateKnockbacks`가 넉백 대상으로 실제 수신자(`hit.TargetId`)도 받음. 시험: `SacrificeTraitTests.RedirectedDisplacementPreview_…` → `RedirectedKnockback_GoesToSacrificer_NotOriginalTarget`(2, 옛 동작 = 원래 대상이 밀려 지뢰에 죽음), 새 `Intimidation_GoesToSacrificer_NotOriginalTarget`, `WeaknessExposureDamageTests.Sacrifice_…GrantsOriginalContact` → `…GrantsActualRecipient`(표식 수신자 기대값 바뀜) | Test 결과 |
| T-e | §6 설계 문서 — **완료(2026-11-02)**: 특성 설계 문서 머리에 후속 결정 요약, §2 표·§3 표(C2·C6·H1·H4·R1·R5·A6)·§7.1/§7.4/§8·수락 기준 줄을 "2026-11-02"로 고침. Core 전달 문서(View 경계)는 고치지 않음 — View가 흔적·상태를 사실로 읽어 계약이 바뀌지 않음 | — |

시험: 우리(천명) 작성 Core 시험은 기대값만 맞추고, 다른 사람이 쓴 시험이 깨지면 묻는다. 새 사례는 F3(지킬 수 없음)·F4·F5에 하나씩 권장(Core 시험이라 View 금지 규칙 밖).

## 8. 질문

결정(2026-11-02): Q1 A, Q2 변경 없음, Q3 예, Q4 예(밤 회수 코드 제거는 범위 밖).

- **Q1** 깔끔한 뒷정리 발동 조건 — A(공격 행동마다, 추가 공격 포함, 권장) / B(적 사망마다).
- **Q2** 희생 시 카테고리 배율은 이미 수호자 기준으로 계산된다(F2). 화면에서 다르게 보인 장면이 있었다면 알려 주세요. 없으면 코드 변경 없음.
- **Q3** 도발을 지킬 수 없고 칠 대상도 없으면 추적도 기존 규칙(가장 가까운 적)으로 — 권장 예.
- **Q4** "밤 회수가 없어졌다": 코드에는 아직 `CombatTurnCalculator.EndNight → RecallForNight`가 있다. 기획에서 없앤 것이면 코드 제거는 이번 범위 밖 별도 작업으로 둔다 — 권장 예.
- 참고(질문 아님): 희생 시 약점 노출(`WeaknessExposureApplication.CompleteHit`)도 원래 대상을 쓴다. 이번 결정 범위가 아니라 그대로 둔다.
