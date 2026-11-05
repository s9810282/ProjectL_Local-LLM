# 전투 특성과 발동 시기 (현존 특성 표)

> **현재 사용 가능 상태·저작 제약은 [2026-10-31 특성 효과·Activation 참조](trait-effects-and-activation-reference.md)를 따른다.** 아래 37종·2연발/행운의 탄환/연쇄 폭발 실행 설명은 당시 코드의 이력이며 현재 인게임 가용성으로 해석하지 않는다.

- 날짜: 2026-10-30
- 기준 판: `6b0755be2`(천명님 특성 시스템 `f848f1f57` 포함) 위 작업 트리.
- 용도: 특성 발동 연출(큐)·HUD·View 재생 길을 정할 때 "이 특성이 언제, 무엇을 내나"를 한곳에서 본다. 발동 연출 에셋을 채울 때 작업 목록으로도 쓴다.
- 원본(이 문서는 옮긴 것):
  - Core → View 인계 [§7 Producer and magnitude reference](../handoffs/2026-10-28-trait-system-core-view-handoff.md) — 특성마다 발동 종류·크기 뜻·적용 경계.
  - Core 특성 설계 [§2·§5·§7](../specs/2026-10-28-trait-system-design.md) — 발동 조건·대상·지속.
  - Core 코드 — 순서 확인: `Application/Combat/AttackDeliveryExecutor.cs:34-46`, `AttackDirectDamageCalculator.cs`, `CombatAttackCalculator.cs:143·175`, `TraitTrailCalculator.cs`, `DeferredCombatFeatures.cs`, `Domain/Traits/Effects/*EffectRule.cs`.
  - 이름·설명은 특성 외형 에셋(`DataBaseSO/ViewGroup/TraitVisual/Item/`)의 문구. "(확인용)"이 붙은 문구는 임시다.
- 범위: `TraitKey` 42개 중 실행되는 특성 37개 + Core가 꺼 둔 5개(§4).

## 1. 발동 시기 단계

공격 한 번 안에서 사건 목록에 나오는 차례와, 공격 밖의 시기를 단계로 나눈다. 표(§2)의 "시기" 칸은 이 글자를 쓴다.

### 1.1 공격 한 번 안 (차례대로)

| 단계 | 언제 | 사건 목록에서 |
|---|---|---|
| **A** 공격 시작 | 공격이 받아들여져 타격하기 전 | 끌어오기·공격 시작 알림 발동이 **공격 사건보다 앞**(`CombatAttackCalculator.cs:143·175`). 일점 포격의 "범위 안 적 수"도 여기서 한 번 잡음 |
| **B** 타격마다, 체력 앞 | 각 타격의 피해 계산 | 피해 변화·방어 무시·즉사·보호막 흡수 발동이 **그 대상 체력 사건보다 앞**. 순서 민감 특성이 없으면 모든 타격의 발동이 먼저 나오고 체력 사건이 뒤에 몰림(`AttackDirectDamageCalculator.cs:36-42`). 보호막 흡수 → 걸린 상태 바뀜 → 체력 순 |
| **C** 타격마다, 체력 뒤 | 그 타격의 피해를 준 뒤 | 위압·무장 해제·발목 부수기·표식 적용·방어구 부수기 발동과 걸린 상태 바뀜(`AttackDirectDamageCalculator.cs:151-156`). 처치 보상(피의 부름·사기 증진·사냥꾼 보상)도 그 처치 바로 뒤 |
| **D** 넉백 | 모든 타격 뒤 | 넉백 적용/막음 발동 → 밀린 걸음(원인 Forced + 특성 키)(`AttackDeliveryExecutor.cs:34-35`) |
| **E** 도착 정산 뒤 | 밀려난 칸의 지뢰 등 처리 뒤 | 연쇄 폭발(밀려나 살아 있는 대상) 추가 공격(`:39` 뒤) |
| **F** 처치 연쇄 | 그 공격의 처치 뒤 | 난사 추가 공격 → 그때마다 행운의 탄환 추가 공격(`:42`) |
| **G** 공격 처리 끝 | 그 공격이 끝날 때 | 2연발(주변 처치 시 같은 방향 추가 공격, `:46`), 가속 소모, 사냥꾼 표적 다시 고르기 |

추가 공격(E·F·G의 셋)은 새 공격 사건 없이 **발동 사건 하나 + 그 안의 추가 공격 결과(`ChildAttack`)**로 오고, 그 뒤에 자기 체력 사건이 따른다. 추가 공격 안에서도 A~G가 다시 돈다.

### 1.2 공격 밖

| 단계 | 언제 |
|---|---|
| **H** 공격받는 쪽, 공격 결과 안 | 희생(대신 맞기)은 따로 된 발동 사건이 아니라 **공격 사건(포효·추가 공격이면 그 결과) 안**에 가로채기로 온다 |
| **I** 이동 | 출발 칸 흔적(첫 걸음 앞), 들어간 칸 흔적(그 걸음 바로 뒤), 뛰어넘기, 가속 쌓기, 고정 사격 "이동함" 표시. 스스로 이동일 때만 |
| **J** 점유 바뀜 | 배치·이동·밀림·회수·사망으로 주변 유닛이 바뀔 때. 단독 행동·결속 보호막 켜고 끄기, 결속 방어 |
| **K** 적 AI 결정 | 적 턴에 도발이 공격·추격 대상을 정할 때 |
| **L** 보스 반응 | 밤 피격 누적이 차면 원래 명령 정산 뒤, 포효 사건 바로 앞 |
| **M** 턴 끝 | 진영 턴 끝: 공격력 부여·제한(무장 해제·발목) 하나 줄기·만료, 보호막 한도 리셋, 상대 진영 턴 끝이면 흔적 줄기·지우기 |
| **N** 정리 | 사망·스테이지 끝·전투 끝: 걸린 상태 해제, 흔적 끄기, 사냥꾼 리셋 |

## 2. 실행되는 특성 37개

- "발동 종류"는 Core `TraitActivationKind` 이름(크기 뜻은 인계 §7).
- "남는 사실"은 HUD·정보창이 읽을 수 있는 유닛 사실(`CharacterCombatFacts`) 칸이다.
- "View"는 지금 길과 이번 특성 시스템 계획([trait-system-view-plan](../plans/2026-10-30-combat-trait-system-view-plan.md))의 Phase다. "큐"는 특성 처리기가 특성 외형 표의 발동 연출 줄로 내는 이펙트·흔들림이고, 줄은 에셋에서 채운다(비면 아무것도 안 나옴).

### 2.1 옛 특성 (15)

| 키 | 특성 | 하는 일 | 시기 | 발동 종류 | 남는 사실 | View |
|---|---|---|---|---|---|---|
| 30 | 관통 볼트 (PiercingFalloff) | 공격 방향 대상을 일직선으로 꿰뚫고, 만날 때마다 피해가 줄어듦 | B(실제 관통 만남마다) | DamageModified | — | 큐 |
| 50 | 넉백 (Knockback) | 공격 뒤 살아남은 대상을 공격 방향으로 1칸 밀어냄. 막히면 안 밀림 | D | KnockbackApplied(밀린 칸 수, 막히면 0), 대상 여럿 | — | 큐 + 밀림 걸음(TS-c 원인 판정) |
| 60 | 산탄 (DistanceDamageScale) | 거리에 따라 피해가 바뀜 | B | DamageModified | — | 큐 |
| 70 | 난사 (KillChainAttack) | 적 캐릭터를 처치하면 전장의 무작위 적 하나를 추가 공격 | F | KillChainAttack + 추가 공격 결과 | — | 추가 공격 다시 재생(TS-b) |
| 90 | 포효 (Roar, 보스) | 밤 동안 피격이 5회 쌓이면 반응 공격 | L | RoarTriggered(영향 칸 수, 대상 없어도 냄) | 포효 카운터 | 포효 처리기(있음) + 큐 |
| 110 | 위압 (Intimidation) | 공격받은 적의 공격력을 다음 공격까지 낮춤(우선도 감소는 Core가 보류) | C 적용 / 대상이 공격할 때 소모 / N | StatusApplied·StatusConsumed·StatusRemoved | `Debuffs.Intimidation` | 큐 + HUD(있음, TS-a로 사건 순간에) |
| 130 | 도어 브리칭 (DoorBreaching) | 주변 8칸의 적을 공격할 때 물리 방어력 무시 | B | DefenseBypassed(무시한 방어) | — | 큐 |
| 140 | 약자 멸시 (ContemptForWeak) | 공격력 비교로 대상별 피해 기여 | B | DamageModified | — | 큐(외형 에셋 없음) |
| 150 | 정밀 사격 (PrecisionShot) | 첫 수신자의 방어력 무시 | B | DefenseBypassed | — | 큐(외형 에셋 없음) |
| 160 | 2연발 (DoubleShot) | 주변 8칸의 적을 처치하면 같은 방향으로 한 번 더(비용 없음, 2연발로 다시 발동 안 함) | G | FollowUpAttack(0) + 추가 공격 결과(아무도 안 맞을 수 있음) | — | TS-b(지금은 안 그림) |
| 170 | 행운의 탄환 (LuckyBullet) | 난사가 발동할 때마다 적 건물이나 보스 하나를 골라 한 번 더 | F(난사마다) | FollowUpAttack + 추가 공격 결과 | — | TS-b |
| 180 | 연쇄 폭발 (ChainExplosion) | 밀려나 살아 있는 대상에게 추가 피해 | E | FollowUpAttack + 추가 공격 결과 | — | TS-b |
| 190 | 사냥꾼의 표식 (HuntersMark) | 공격한 적에게 표식, 다른 아군의 직접 공격이 추가 피해를 주고 소모 | C 적용 / B 소모(다른 아군 타격의 피해 단계) / N | StatusApplied(충전 수)·StatusConsumed(추가 피해)·StatusRemoved | `Debuffs.HuntersMark` + 표식 사건 | 표식 사건(있음, TS-a 칸 보존) + HUD |
| 200 | 끌어오기 (Pull) | 기본 사거리 밖 유닛을 사거리까지 끌어옴(아군 피해 없음, 장애물 제외) | A(끌어오기 사건 앞) | PullApplied(끌린 칸 수) | — | 끌어오기 레인(있음) + 큐 |
| 210 | 충격 볼트 (ImpactBolt) | 착탄 지점 주변의 적을 바깥쪽으로 밀어냄 | D | KnockbackApplied | — | 큐 + 밀림 걸음 |

### 2.2 새 특성 22 (천명님 `f848f1f57`)

| 키 | 특성 | 하는 일 | 시기 | 발동 종류 | 남는 사실 | View |
|---|---|---|---|---|---|---|
| 220 | 선봉장 (Vanguard) | 그 진영 첫 공격 때 다른 배치 아군의 공격력을 이번 턴 동안 올림 | A / M | AttackGranted(받는 유닛 여럿)·StatusConsumed·StatusExpired·StatusRemoved | `TraitEffects.AttackGrants` | 큐(계획 R11: 대상 여럿이라 늦게 나옴) + HUD 공격력(TS-d) |
| 230 | 깔끔한 뒷정리 (CleanSweep) | 다른 아군이 공격할 때마다 자신의 공격력이 이번 턴 동안 오름 | A / M | 위와 같음 | `TraitEffects.AttackGrants` | 큐 + HUD 공격력 |
| 240 | 단독 행동 (SoloAction) | 주변 1칸에 다른 유닛이 없으면 보호막 10 | J / B 흡수 / M 리셋 | ShieldGranted·ShieldReset·ShieldAbsorbed·ShieldRemoved | `TraitEffects.Shields` | 큐 + HUD 보호막(TS-d) |
| 250 | 피의 부름 (CallOfBlood) | 적을 처치하면 그 적 최대 체력의 10%만큼 회복 | C(처치 뒤) | HealthRecovered(실제 회복) | 체력 | 큐 + 체력 사건 |
| 260 | 사기 증진 (MoraleBoost) | 적을 처치하면 배치 아군의 공격력이 이번 턴 동안 오름 | C(처치 뒤) / M | AttackGranted(여럿)·StatusConsumed·StatusExpired·StatusRemoved | `TraitEffects.AttackGrants` | 큐(R11) + HUD 공격력 |
| 270 | 무장 해제 (Disarm) | 직접 지정한 적에게 적중하면 그 진영 1턴 동안 공격 금지 | C / M | StatusApplied(남은 턴 끝 수)·StatusConsumed·StatusExpired·StatusRemoved | `Debuffs.Disarm` | 큐 + HUD(TS-d) + 거절 안내(TS-d) |
| 280 | 발목 부수기 (AnkleBreaker) | 같은 조건으로 1턴 동안 스스로 이동 금지 | C / M | 위와 같음 | `Debuffs.AnkleBreaker` | 큐 + HUD + 거절 안내 |
| 290 | 결속 (Cohesion) | 주변 1칸 아군마다 자신 방어력 +, 그 아군에게 보호막 | J / B 흡수 / M 리셋 | 보호막 넷 + DefenseChanged | `TraitEffects.Shields`·`CohesionDefenseBonus` | 큐 + HUD 보호막 |
| 300 | 희생 (Sacrifice) | 적 턴에 주변 2칸 아군이 치명적인 공격을 받으면 대신 맞음(적 턴마다 1회) | H | DamageRedirected(실제 넘겨받은 체력 손실) + 가로채기 | 희생 보유 상태 | TS-f(지금은 멀리 선 수호자가 맞음) |
| 310 | 도발 (Taunt) | 주변 2칸의 적이 자신을 우선 공격·추격 | K | TauntSelected(0) | 도발 보유 상태 | 큐 |
| 320 | 진지 구축 (Entrenchment) | 자신을 뺀 주변 1칸 아군이 받는 피해 50% 감소 | B(받는 피해 단계) | DamageModified(줄인 피해) | — | 큐 |
| 330 | 굳건한 의지 (SteadfastWill) | 넉백에 밀리지 않음(끌어오기는 적용 안 됨) | D | KnockbackPrevented(밀릴 뻔한 칸 수) | — | 큐 |
| 340 | 땅 다지기 (GroundCompaction) | 스스로 이동한 경로에 흔적, 그 위에서 이동을 시작하면 이동 범위 +1 | I 생성 / M(상대 턴 끝) 줄기·지우기 / 이동 시작 때 범위 | TrailCreated·TrailChanged·TrailRemoved·RangeChanged | 환경 원본 `TraitTrailFact` | 흔적 표시(TS-e) + 큐 |
| 350 | 먼지 연막 (DustScreen) | 같은 경로에 연막, 위 유닛의 사거리 −1·받는 공격 피해 −20% | I / M / B(받는 피해) | 흔적 셋 + DamageModified | 환경 원본 `TraitTrailFact` | 흔적 표시(TS-e) + 큐 |
| 360 | 뛰어넘기 (Leap) | 이동 중 유닛·벽·절벽을 지나고 중간 지뢰를 건너뜀 | I | MovementChanged(특별한 칸 수) | — | 스스로 이동 걸음 + 큐 |
| 370 | 가속 (Acceleration) | 스스로 이동한 칸마다 다음 공격의 공격력 +, 공격 뒤 소모 | I 쌓기 / G 소모 | AttackGranted(쌓인 양)·StatusConsumed | 가속 보유 상태(쌓은 공격력) | 큐. HUD 공격력 줄에는 안 들어감(아래 §3) |
| 380 | 고정 사격 (StationaryFire) | 이번 자기 턴에 스스로 이동하지 않았으면 공격 피해 +100% | I 표시 / 자기 턴 시작 리셋 / B | StatusApplied·StatusReset / DamageModified | 고정 사격 보유 상태(이동 여부) | 큐 |
| 390 | 방어구 부수기 (ArmorBreaker) | 적중할 때마다 대상 방어력 10% 감소, 스테이지 끝까지 겹침 | C | ArmorBroken(설정 비율)·StatusRemoved | `Debuffs.ArmorBreak`(겹친 원본) | 큐 + HUD(TS-d) |
| 400 | 마무리 사격 (FinishingShot) | 타격 직전 대상 체력이 자신 공격력 20% 미만이면 방어·보호막 무시하고 처치 | B(타격 앞) → 사망 | InstantDefeat(그 체력) | 사망 원인 `InstantDefeat` | 큐 + 사망 연출(그대로) |
| 410 | 추적 사격 (TrackingShot) | 타격 직전 대상 체력이 최대의 50% 이상이면 피해 +20% | B | DamageModified | — | 큐 |
| 420 | 사냥꾼 (Hunter) | 체력 가장 높은 적을 표적으로, 직접 처치하면 이번 Run 공격력 +(스테이지 3회) | C(표적 처치 뒤) 보상 / G 다시 고르기 / N 리셋 | HunterTargetSelected·AttackGranted(Run 공격력)·StatusReset·StatusRemoved | 사냥꾼 보유 상태(표적·보상 수) | 큐(표적 표시는 다음 묶음) |
| 430 | 일점 포격 (FocusedBombardment) | 공격 시작 때 범위 안 살아 있는 적이 정확히 1명이면 피해 +50% | A 판정 / B 적용 | DamageModified | — | 큐 |

## 3. View가 알아 둘 것

- **맞는 순간에 묶이는 발동.** 결과 근원(`ResultOrigin`)이 있는 발동·상태 바뀜은 그 공격의 맞는 순간 묶음에 든다(계획 TS-a). 확인된 것: B 단계 전부(피해 변화·방어 무시·보호막 흡수), C 단계의 위압·무장 해제·발목 부수기·표식 적용. 근원이 없는 것(고정 사격 이동 표시·턴 끝·정리·점유 바뀜·흔적)은 앞뒤 사건을 막는 장벽이다.
- **대상 여럿 발동**(넉백 적용, 선봉장·사기 증진 공격력 부여)은 발동 유닛이 주체라 큐가 공격 연출이 끝난 뒤 나온다(계획 R11).
- **추가 공격**(난사·2연발·행운의 탄환·연쇄 폭발)은 발동 사건 하나에 추가 공격 결과(`ChildAttack`)가 붙고, 방향·지정 칸·근원이 늘 있다. 2연발은 아무도 안 맞을 수 있다.
- **희생**은 따로 된 발동 사건으로 오지 않는다. 공격 사건 안을 읽어야 한다(TS-f).
- **가속의 쌓인 공격력**은 공격력 부여 목록(`TraitEffects.AttackGrants`)이 아니라 가속 보유 상태에 있다. 그래서 HUD "공격력 부여" 줄(TS-d)에는 안 들어간다. 따로 띄우려면 보유 상태 줄이 필요하다(TS-d에서 정함).
- **사냥꾼의 Run 공격력**도 공격력 부여 목록이 아니라 보유 기록(`OwnedCharacterRecord.HunterAttackBonus`)에 있다.
- **크기(`Magnitude`) 0**도 뜻이 있다(빈 포효·도발 선택·막힌 넉백·2연발). 큐를 0으로 거르지 않는다.

## 4. Core가 꺼 둔 특성 (발동 없음)

`DeferredCombatFeatures.IsTraitAvailable`이 false인 다섯이다. 외형 에셋은 있지만 발동 사건이 나오지 않는다.

| 키 | 특성 | 꺼 둔 이유(Core) |
|---|---|---|
| 10 | 집중 (Focus) | 진영 턴 시기 재설계 대기 |
| 20 | 이동 후 공격 할인 (MoveAttackDiscount) | 위와 같음 |
| 40 | 가시밭 (ThornField) | 필드 수명·방어 감소 만료를 진영 턴 단위로 다시 정할 때까지 |
| 80 | 사령술 (Necromancy, 보스) | 공용 적 진영의 소환 주기를 정할 때까지 |
| 100 | 재생 (Regeneration) | 진영 턴 시기 재설계 대기 |

위압의 우선도 감소 부분도 보류다(`GetTraitPartialUnavailableReason`). 위치 교대·정찰대는 Core에 등록된 특성이 아니다(이동 되돌리기 설계 뒤).
