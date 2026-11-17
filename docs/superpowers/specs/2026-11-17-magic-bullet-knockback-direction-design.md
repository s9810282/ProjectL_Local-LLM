# 마탄 넉백 방향 — 꺾인 탄의 밀림 방향 설계

날짜: 2026-11-17
상태: **Q-MK1 A로 결정(2026-11-17 사용자 "A안으로 진행"), 코드 반영 — §8.** Q-MK2는 권장(예)으로 진행, Q-MK3은 Core 시험 추가로 결정(2026-11-17 사용자 "시험 추가해줘") — §8.4.
출처: 메인 캐릭터 액티브 스킬 Core 코드 리뷰(2026-11-17, 정적 검토 — 컴파일·Unity 실행 없음).
관련: [액티브 스킬 Core 설계](2026-11-15-player-active-skills-core-design.md) M13~M15, [리뷰 수정 계획](../plans/2026-11-15-active-skill-review-fixes.md).

## 1. 문제 요약

마탄(일반·왜곡)에 **넉백 특성**이 붙으면, 꺾인 뒤 맞은 대상이 **탄이 들어온 방향이 아니라 저격수 칸에서 대상 쪽 방향**으로 밀린다. 그래서 대각선으로 밀리거나, 왜곡의 마지막 구간에서는 탄이 온 쪽으로 되밀린다.

- 심판(Judgment)은 설계 M15대로 "들어온 구간 방향 + 2칸"을 명시 요청으로 넘겨서 문제 없다.
- 일반·왜곡에서 넉백 특성의 방향 규칙은 설계 문서에 정해져 있지 않다(설계 빈칸).
- 예외로 멈추지는 않는다. 결과가 이상하게 보이는 문제다.

### 1.1 지금 데이터로 닿는가 — 닿는다

- 넉백 특성은 **메인 직군 성장 풀**에 있다(`DataBaseSO/InGameGroup/TraitGrowth/Item/CategoryTraitProgression_Main.asset`의 `TraitGrowth_Knockback`, Heavy 풀에도 있음).
- 저격수 메인은 관통 감쇠가 필수지만, 공격 프로필(`CombatAttackProfile`)은 "넉백 + 관통"을 막지 않는다.
- 스킬 호환 규칙 `PlayerSkillDefinition.SupportsTrait`는 화약 꽃 + 충격 볼트만 막는다.
- 따라서 저격수 메인이 성장으로 넉백을 얻으면 바로 일어난다.

### 1.2 재현 예

저격수 (0,0), 사거리 3 이상.

| 경우 | 선택 | 맞은 대상 | 탄 진행 방향 | 지금 밀리는 방향 |
|---|---|---|---|---|
| 일반 첫 구간 | A(3,0), B(3,3) | (2,0) | (+1,0) | (+1,0) — 같음 |
| 일반 꺾인 뒤 | A(3,0), B(3,3) | (3,2) | (0,+1) | **(+1,+1) 대각선** |
| 왜곡 마지막 구간 | A(3,0), B(3,3), C(1,3) | (2,3) | (−1,0) | **(+1,+1) — 탄이 온 쪽으로** |
| 왜곡 마지막 구간 | 같음 | (0,3) | (−1,0) | **(0,+1) 옆으로** |

## 2. 원인 (코드 대조)

1. 넉백 원점은 공격자 칸 하나다. [`AttackKnockbackCalculator.cs:76`](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/AttackKnockbackCalculator.cs#L76) `origin = actor.Anchor.Value`.
2. 방향은 원점 → 대상의 부호 `ResolveBranchStep`이다(8방향, 대각선 허용).
   - 캐릭터: `:123`
   - 장애물: `:97`에서 원점을 넘기고 `:258`에서 계산
   - 시체(사망 특성 보유자): `:219`
3. 명시 방향은 `plan.ForcedDisplacement` 하나뿐이고, 마탄은 심판일 때만 채운다([`MagicBulletCalculator.cs:67`](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/PlayerSkills/MagicBulletCalculator.cs#L67)).
4. 마탄은 칸마다 들어온 방향(`route[i].Incoming`)을 이미 계산하지만, 결과 사건(`IncomingDirections`)에만 쓰고 넉백에는 넘기지 않는다.

일반 관통 공격은 한 직선이라 "공격자 → 대상" 부호가 탄 방향과 같다. 꺾이는 탄이 처음 생기면서 이 가정이 깨졌다.

## 3. 영향 범위

- 캐릭터 넉백, 장애물(화약통 등) 넉백, 사망 특성 보유자 시체 밀림 — 세 갈래 모두 같은 원점을 쓴다.
- 작렬(CrashImpact) 부딪힘은 넉백 경로의 `step`을 받아 쓰므로 방향만 고치면 함께 맞는다.
- View는 Core 결과(`AttackKnockbackSnapshot.Step`·경로)를 그대로 그리므로 View 변경은 없다. 넉백 미리보기 화살표도 Core 답을 따른다.
- 일반 공격·반응 공격·처치 연쇄·화약 꽃은 대상이 아니다(직선이거나 공격자 중심이 맞는 공격).

## 4. 선택지

| 안 | 내용 | 장점 | 단점 |
|---|---|---|---|
| **A (권장)** | 마탄 일반·왜곡도 **그 대상이 맞은 구간의 들어온 방향**으로 민다. 거리는 넉백 특성 그대로. | 탄이 날아간 방향과 화면이 맞음. 심판(M15)과 같은 원리. | 공용 넉백 코드 세 곳에 "대상별 방향" 입구가 하나 생김. |
| B | 마탄 메인에게 넉백 특성을 막는다(`SupportsTrait`에 한 줄). 성장 후보에서도 빠짐. | 변경 한 줄, 공용 코드 무변경. | 저격수 메인의 성장 선택지가 줄어듦(기획 결정). |
| C | 지금대로 둔다(저격수 칸 기준). 문서에 규칙으로 적는다. | 변경 없음. | 대각선·되밀림이 그대로 보임. |

## 5. 권장안(A) 설계

### 5.1 규칙

- 방향 우선순위(위가 이김): ① 명시 요청 `ForcedDisplacement`(심판) → ② **대상별 투사체 방향(새로)** → ③ 충격 볼트 충돌 중심 → ④ 공격자 칸 기준 `ResolveBranchStep`(지금).
- 대상별 방향 = 그 대상을 **처음 맞힌 접촉**의 들어온 구간 방향. 다칸 대상은 처음 닿은 칸 기준(조우 한 번 규칙 M08과 같음).
- 희생으로 수호자가 대신 맞으면, 수호자는 **대신 맞은 원래 대상의 접촉 방향**으로 밀린다(지금도 넉백 후보는 `ActualRecipients`로 수호자를 고름, `:87`).
- 피해 출발 칸 `ImpactOrigin`은 **바꾸지 않는다.** 고지대 피해 감소(`EnvironmentTerrainRules.RemainingDamagePercent`)가 "쏜 사람이 고지대에 있나"를 이 칸으로 판정하므로, 저격수 칸이 맞다.

### 5.2 바꿀 곳

| 파일 | 변경 |
|---|---|
| `CommandAttackPlan.cs` | 끝 선택 인자 `projectileIncomingSteps`(원래 대상 ID → 한 칸 방향). 비어 있으면 지금과 같음. 생성자에서 값이 한 칸 상하좌우인지 검사. |
| `MagicBulletCalculator.cs` | 일반·왜곡일 때 `contacts`의 각 원래 대상 → `route[PathIndex].Incoming`을 만들어 넘김. 심판은 지금처럼 `forcedDisplacement`만 씀(둘을 같이 넘기지 않음). |
| `AttackKnockbackCalculator.cs` | 방향을 정하는 도우미 하나(`StepFor`)로 세 갈래(캐릭터 `:115-123`, 장애물 `:97`, 시체 `:214-219`)를 모음. 수호자 → 원래 대상은 장부 접촉(`ledger.Contacts(recipient)`의 첫 `OriginalTargetId`)에서 찾음. |
| Core→View 인계 문서 | "마탄 넉백 방향 = 접촉 구간 방향" 한 줄. View 코드 변경 없음. |

일반 공격 경로는 새 인자를 넘기지 않으므로 동작이 같다. 공용 넉백 코드에 갈래가 하나 늘어나는 것이 변경 영향의 전부다.

### 5.3 확인할 것(사용자 실행)

- 표 1.2의 네 경우: 넉백 요약의 `Step`이 탄 진행 방향과 같은지.
- 희생 수호자가 꺾인 구간의 대상을 대신 맞을 때 수호자 밀림 방향.
- 화약통이 꺾인 구간에서 맞아 밀릴 때 방향, 사망 특성 보유자가 꺾인 구간에서 죽을 때 시체 끝 칸.
- 심판은 그대로(들어온 방향 2칸), 일반 관통 공격·화약 꽃 넉백은 그대로.
- 마탄 예측(`PlayerSkillQueries.Forecast`)과 실행의 넉백 요약이 같은지.

## 6. 정할 것

- **Q-MK1** 방향 규칙: A(권장) / B / C.
- **Q-MK2** (A일 때) 장애물·시체 밀림도 같은 규칙: 권장 예(같은 원인이라 한 도우미로).
- **Q-MK3** 시험: Core 시험(`ProjectL.Tests.Core`/`CoreIntegration`)에 표 1.2 사례를 더할지, 사용자 확인만 할지.

## 7. 같은 리뷰에서 확인했지만 문제 없던 곳

다시 볼 필요가 없도록 남긴다(정적 대조, 실행 확인 아님).

- **마탄 + 충격 볼트:** 마탄 선택에는 주 칸(`PrimaryCell`)이 없어, 충격 볼트(충돌 중심 넉백)가 붙으면 `AttackKnockbackCalculator`의 충돌 중심 계산에서 예외가 날 구조였다. 그러나 `CombatAttackProfile.cs:33`이 "충격 볼트 + 관통"을 거부하고 마탄 메인은 관통 감쇠가 필수(`PlayerActiveSkillTable.ValidateOwners`)라 닿지 않는다. §8 반영 뒤에는 투사체 방향이 있으면 충돌 중심 모드를 쓰지 않으므로, 프로필 규칙을 풀어도 마탄 일반·왜곡에서는 이 예외가 나지 않는다.
- 턴 순번: 플레이어 전투 국면은 늘 `ControllerTurnSequence ≥ 1`이라 사용 정산 생성자의 `turn > lastUsed` 조건이 깨지지 않는다.
- 기절(단죄): 구조물·죽은 유닛에는 걸리지 않음(`deliveredVictims`는 캐릭터만, `ApplyStatus`는 죽은 대상 거절). 적 AI 이동·공격과 보스 포효 모두 기절을 본다. 만료는 걸린 쪽 턴 끝.
- 상태 보존: `CombatCommandState`를 새로 만드는 곳(턴 전환·투영·전투 준비·기믹 초기화)이 모두 스킬 사용량을 넘긴다.
- Buffer 게시 검증(`CombatStateBuffer.ValidatePlayerSkillState`)과 Core 계산의 출처 도장 비교가 일치한다. View 명령 생성자는 `ForPlayerSkill` 하나뿐이라 기대 전투·턴 값이 어긋날 길이 없다.
- 화약 꽃 마스크 칸 수 8·20·36, 처치 연쇄 초기 근거(버스트마다 직접 처치만), 연쇄 종료(처치 없음·전투 종료·시전자 사망), 결과 묶음의 순번·공격 그룹 검증.
- 마탄 지점 검증(범위·직각·벽·같은 점 금지)과 경로(꺾는 점 한 번, 마지막 구간은 벽·끝까지), 관통 예산과 실제 경로 길이 자르기.
- 예측용 공개 입력(`KnownInput`)은 숨은 환경을 빼고 난수를 고정해 같은 요청이면 같은 답을 낸다(설계 D12의 "조건부 예측").

## 8. 실행 기록 (2026-11-17, A안)

### 8.1 바꾼 것

| 파일 | 변경 |
|---|---|
| `Application/Combat/CommandAttackPlan.cs` | 끝 선택 인자 `projectileIncomingSteps` → 읽기 전용 `ProjectileIncomingSteps`. 값마다 한 칸 상하좌우, 빈 ID 금지, `ForcedDisplacement`와 함께 넣으면 예외. |
| `Application/Combat/PlayerSkills/MagicBulletCalculator.cs` | 일반·왜곡이면 경로 접촉(`contacts`)마다 원래 대상 → `route[PathIndex].Incoming`을 넘김. 심판은 지금처럼 `forcedDisplacement`만. |
| `Application/Combat/AttackKnockbackCalculator.cs` | 도우미 `ExplicitStep`(명시 요청 → 투사체 구간 방향, 수호자는 처음 대신 맞은 원래 대상 기준)을 후보마다 한 번 구해 캐릭터·장애물·시체 세 갈래가 같이 씀. |
| Core→View 인계 문서 §3 | 일반·왜곡 넉백 방향 한 단락(View 변경 없음). |

코드 새 파일 없음. 시험은 §8.4. 사용자 쪽 커밋 전 변경(`AttackKnockbackCalculator.cs`의 시체 진입·장애물 착지 두 줄)은 그대로 두고 그 위에 고쳤다.

**설계와 다른 점 하나:** 충돌 중심 모드(`impactCenter`) 판정에 "투사체 방향이 없을 때만"을 더했다. §5.1 우선순위(투사체 방향이 충돌 중심보다 먼저)를 코드에서 그대로 지키려면 필요하다. 지금 데이터로는 둘이 같이 올 수 없어(§7 첫 항목) 동작 차이는 없고, §7의 충격 볼트 예외 경로도 함께 닫힌다.

### 8.2 변경 영향

| 고친 것 | 같이 확인할 경로 | 다시 돌릴 시험 | 확인하지 않아도 되는 것 |
|---|---|---|---|
| 넉백 방향 도우미 | 심판 넉백(명시 요청), 마탄 일반·왜곡 + 넉백 특성, 장애물·시체 밀림 | Core·CoreIntegration 전체(공용 넉백 경로) | 일반 공격·반응 공격·처치 연쇄·화약 꽃 — 새 값을 넘기지 않아 `ExplicitStep`이 옛 규칙과 같은 값을 냄 |
| 공격 계획 새 인자 | 계획을 만드는 네 곳(일반 공격 둘·마탄·화약 꽃) | 위와 같음 | View·Buffer — 결과 형식 무변경 |

### 8.3 검증

- 정적 확인: 변경 diff 검토, `git diff --check` 통과, 기존 CRLF 유지.
- Core·CoreIntegration 배치 실행(2026-11-17, 이 변경 포함 작업 트리): 7,235건 — 통과 7,216, 실패 0, 건너뜀 19(Core 6,060 / CoreIntegration 1,175). 같은 날 11:00 기준선과 숫자·건너뛴 목록이 같다. 실행이 바꾼 부수 파일(FMOD 카탈로그·글꼴 에셋)은 되돌렸다.
- 위 실행은 새 시험 추가 전이다. 기존 시험에는 "넉백 특성을 가진 마탄 일반·왜곡" 사례가 없어, 그때의 통과는 다른 경로가 그대로라는 근거였다. 새 규칙 자체는 §8.4에서 확인했다.
- 사용자 확인 대기: §5.3 목록 중 시험으로 덮지 못한 것 — 희생 수호자 방향, 실제 게임 화면의 넉백 연출.

### 8.4 추가한 시험 (Q-MK3, 2026-11-17)

새 파일 `Assets/Tests/EditMode/MagicBulletKnockbackDirectionTests.cs`(`ProjectL.Tests.Core`, `.meta`는 Unity가 만듦). 기존 도우미 `MagicBulletTestData`·`PowderBlossomFixture`를 쓰고, 저격수에 넉백 1칸 특성을 붙인다.

| 시험 | 확인 내용 | 예전 규칙이면 |
|---|---|---|
| `BentSegmentPushesAlongIncomingDirection` | 저격수 (1,1), A(3,1)·B(3,4). 첫 구간 대상 (2,1)은 (1,0), 꺾인 뒤 대상 (3,3)은 (0,1)로 밀림. 넉백 방향 = 공개된 `IncomingDirections` | (3,3)이 (1,1) 대각선 |
| `DistortionFinalSegmentPushesAlongTravelNotBackTowardCaster` | A(4,1)·B(4,4)·C(2,4). 마지막 구간 대상 (3,4)는 (−1,0)으로 (2,4)에 | (1,1) — 탄이 온 쪽 |
| `ObstacleOnBentSegmentIsPushedAlongIncomingDirection` | 꺾인 구간의 화약통(내구 5)이 (0,1)로 (3,4)에 | (1,1) |
| `SelfDestructCorpseOnBentSegmentMovesAlongIncomingDirection` | 꺾인 구간에서 죽은 자폭 유닛 시체가 (3,3) → (3,4), 폭발도 (3,4) | (4,4) |
| `ForecastPublishesTheSameBentKnockbackAsExecution` | 예측과 실행의 넉백 대상·방향·도착 칸이 같음 | (규칙과 무관한 일치 검사) |

- 배치 실행(같은 날, 새 시험 포함): 7,240건 — 통과 7,221, 실패 0, 건너뜀 19(Core 6,065 / CoreIntegration 1,175). 새 5건 모두 통과, 기존 숫자는 그대로. 첫 실행은 시험 파일 using 누락(`Contracts.Ids`)으로 컴파일 실패 → 고쳐 다시 돌림. 부수 파일은 되돌림.
- 앞의 네 시험이 예전 규칙에서 실패하는지는 코드 대조로만 확인했다(예전 `ResolveBranchStep` 값이 표의 오른쪽 열). 코드를 되돌려 실행해 보지는 않았다.
- 덮지 못한 것: 희생 수호자 방향. 기존 희생 시험은 적의 공격에서 아군을 지키는 구성이고 사용 횟수도 적 턴 기준(`ActivationsThisEnemyTurn`)이라, 플레이어 턴 마탄에서 희생이 일어나는 구성을 확인하지 못해 시험을 세우지 않았다.
