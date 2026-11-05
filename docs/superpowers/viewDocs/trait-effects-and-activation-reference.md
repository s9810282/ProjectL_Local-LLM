# 특성 효과·사용 가능 상태·Activation 저작 참조

- 확인일: **2026-10-31**. 기준: `2bd95ad34`를 병합한 `0403babef` 위 현재 작업 트리의 Core 코드와 실제 TraitVisual SO.
- 목적: 특성이 하는 일, 인게임에서 사용할 수 있는지, 어떤 Activation 줄을 작성할지, Anchor·Point를 어디까지 고를 수 있는지 한 문서에서 확인한다.
- 확인 방식: 코드·에셋 정적 대조. 이번 연동은 Unity Roslyn 코드 컴파일·정적 대조까지 확인했다. 현재 Play 유지 지시로 실제 에디터 컴파일/임포트·기존 DB 시험은 대기이며 Play는 사용자 후속이다. [실행 기록](../plans/2026-10-31-status-effect-view-integration-plan.md#11-구현검증-기록--2026-10-31)을 따른다.
- 이전 문서의 특성 수·부분 보류 표기는 과거 기준이다. 현재 가용성은 같은 Core 조회의 TraitAvailability가 원본이다. 재생은 활성화됐고 비용 할인은 Retired다. View가 별도 고정 개수나 퇴역 목록을 관리하지 않는다.
- 수치·반경·기간은 해당 InGame 정의/SO의 설정을 따른다. 아래 효과 개요를 고정된 밸런스 수치로 사용하지 않는다.

## 1. 먼저 구분할 것

| 구분 | 의미 | 원본/결정 주체 |
|---|---|---|
| 특성 효과 | 피해·회복·이동·상태·추가 공격의 실제 계산 | Core의 특성 규칙과 명령 계산 |
| 사용 가능 상태 | 그 효과를 현재 전투 규칙에서 실행할 수 있는지 | `DeferredCombatFeatures`·기본 TraitRegistry |
| `TraitVisualDataSO.icon` | 정보 카드의 특성 아이콘 | View 특성 외형 표. 효과 HUD 슬롯 생성이나 발동 조건 판정이 아님 |
| `activations` | Core가 보낸 `(TraitKey, TraitActivationKind)`에 대응하는 연출 줄 | View 특성 외형 표 |
| 버프·디버프·보호막 HUD | 현재 걸린 상태와 남은 수치/수명 표시 | Core 사실 + 별도 상태 외형/기존 HUD 소비자 |
| `trailPrefab` | 땅 다지기·먼지 연막의 지속 흔적 외형 | 특성 외형 표. 순간 Activation 이펙트와 수명 소유자가 다름 |
| 미리보기 | 발동 사실·피해 결과를 합성해 기존 연출기로 재생 | Trait Activation Preview. 실제 발동 조건을 판정하지 않음 |

**SO에 Activation 줄을 추가해도 특성이 사용 가능해지거나 Core 효과가 새로 생기지는 않는다.** 줄이 없으면 특성 전용 큐·유닛 행동을 내지 않는다. 실제 추가 공격 결과가 있으면 줄이 없어도 유닛의 기본 공격 연출은 재생한다.

| “Kind”로 혼동하기 쉬운 세 개념 | 예 | 결정하는 것 |
|---|---|---|
| `TraitActivationKind` | AttackGranted, KillChainAttack | 어떤 발동 사실과 연출 줄을 매칭할지 |
| `CombatCueEvent` | TraitActivation, MeleeAttack, RangedAttack, Hit | 그 큐가 붙은 칸의 Anchor·Point 허용 규칙 |
| `CombatAttackPresentationKind` | Melee, Linear, Mortar | 공격 모션·투사체 표현 방식과 근접/원거리 큐 칸 |

상태 표기:

| 표기 | 인게임 의미 | 연출 저작 |
|---|---|---|
| 사용 가능 | 현재 등록·실행 경로가 있음 | 실제로 생산되는 Kind를 작성하면 소비됨. 발동 조건 충족이 필요 |
| 일부 보류 | Core가 부분 사유를 제공할 때만 표시 | 현재 부분 보류 항목 없음 |
| 턴 규칙 보류 | `IsTraitAvailable=false`, 사유 `DeferredUnitTurnTiming` | 보유 아이콘/에셋이 있어도 실제 발동 큐를 기대할 수 없음 |
| Retired | 실행·저작 대상에서 제외한 예약 키 | 역사적 SO는 남겨도 활성 표 미등록이면 시연 사전 검사에서 거절 |
| 정의만 있음 | enum/입력 구조에 값이 존재 | 현재 Core 생산 경로가 없으면 인게임에서는 오지 않음 |

`IsImplementationPending` 분류도 코드에는 있지만 현재 해당하는 특성은 없다. 판정 원본: [DeferredCombatFeatures](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/DeferredCombatFeatures.cs), [TraitRegistry](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/Traits/TraitRegistry.cs).

## 2. 활성 특성 — 효과와 실제 Activation

아래 Kind는 현재 생산 경로를 기준으로 한다. “정리”는 턴 차감·만료·사망/전투 정리 등의 후속 사건이다. 전용 큐가 필요할 때 별도 줄을 작성할 수 있지만 모든 생애주기에 연출을 넣어야 하는 것은 아니다. 변화가 없거나 실제 수치가 0이면 일부 사건은 생략된다.

| 키 / 코드 | 표시명 | 실제 효과 개요 | 주요 Activation Kind | 후속·정리 Kind / 주의 |
|---|---|---|---|---|
| 30 `PiercingFalloff` | 관통 볼트 | 관통 전달의 조우 순번에 따라 피해를 감쇠 | `DamageModified` | 실제 관통·접촉은 공격 전달/투사체 경로가 담당 |
| 50 `Knockback` | 넉백 | 타격 대상을 정해진 칸 수만큼 밀고 도착 효과 정산 | `KnockbackApplied` | 실제 위치 변경은 Forced 이동 사건. 발동 큐만으로 유닛을 옮기지 않음 |
| 60 `DistanceDamageScale` | 산탄 | 공격자와 대상 거리별 피해 백분율 적용 | `DamageModified` | 거리 판정·피해 배율은 Core에서 계산 |
| 70 `KillChainAttack` | 난사 | 처치로 얻은 횟수만큼 맵의 무작위 적 캐릭터를 추가 공격. 추가 공격 처치로 연쇄 | `KillChainAttack` | `ChildAttack` 결과가 있는 추가 공격. 실제 공격 모션/투사체 재생 가능 |
| 90 `Roar` | 포효 | 밤의 피격 누적 조건 충족 시 보스 반응 공격 | `RoarTriggered` | 별도 `CombatRoarEvent`와 실제 공격 결과가 옴. 일반 Moment와 달리 기존 포효 처리 경로가 있음 |
| 100 `Regeneration` | 재생 | 자기 진영 턴 종료 주기마다 설정량 회복, 최대 HP로 제한 | `HealthRecovered` | 진행 변경은 실제 회복이 아님. 만피이면 가짜 회복 발동 없음 |
| 110 `Intimidation` | 위압 | 타격한 생존 적의 공격력을 다음 공격 완료까지 감소 | `StatusApplied`, `StatusConsumed` | 정리 시 `StatusRemoved`. 우선도 감소 구성은 없음 |
| 190 `HuntersMark` | 사냥꾼의 표식 | 적중 후 약점 노출. 같은 제공자 종류·정의의 최대 기여를 서로 다른 그룹과 합산한 받는 피해 증가율 적용 | `StatusApplied` | 같은 출처 재부여는 값·기간 갱신. 대상 진영 턴 종료 수명. 첫 적중 소급/일회 소모/다른 아군 한정 아님 |
| 200 `Pull` | 끌어오기 | 기본 사거리 밖의 대상을 사거리 경계까지 당겨 공격 | `PullApplied` | 별도 끌어오기 경로/이동 사실이 위치를 바꿈 |
| 210 `ImpactBolt` | 충격 볼트 | 적중 유닛을 공격 중심의 바깥 방향으로 밀어냄 | `KnockbackApplied` | 피격 자격을 충족한 아군도 밀 수 있음. 면역·경로·도착 정산은 기존 넉백 경로 |
| 220 `Vanguard` | 선봉장 | 진영의 첫 공격을 자신이 하면 다른 배치 아군에게 공격력 부여 | `AttackGranted` | 부여 수명에 따라 `StatusConsumed`, `StatusExpired`, 정리 시 `StatusRemoved` |
| 230 `CleanSweep` | 깔끔한 뒷정리 | 다른 배치 아군이 공격할 때 자신에게 공격력 부여 | `AttackGranted` | `StatusConsumed`, `StatusExpired`, `StatusRemoved`. 자기 공격으로는 획득하지 않음 |
| 240 `SoloAction` | 단독 행동 | 설정 반경의 점유 칸 수가 조건과 같으면 자신에게 보호막 | `ShieldGranted`, `ShieldAbsorbed` | `ShieldRemoved`, `ShieldReset`. 조건은 단순 “혼자 있음”이 아니라 설정된 점유 수 비교 |
| 250 `CallOfBlood` | 피의 부름 | 자신에게 귀속된 적 처치 시 그 적 최대 HP 기준으로 자신 회복 | `HealthRecovered` | 실제 회복량이 0이면 회복 발동을 내지 않음 |
| 260 `MoraleBoost` | 사기 증진 | 자신에게 귀속된 적 처치 시 그 적 기본 공격력 기준으로 생존 배치 아군 전체에 공격력 부여 | `AttackGranted` | `StatusConsumed`, `StatusExpired`, `StatusRemoved`. 대상에는 자신도 포함될 수 있음 |
| 270 `Disarm` | 무장 해제 | 타격한 적에게 공격 제한 부여 | `StatusApplied` | `StatusConsumed`=남은 영향 진영 턴 차감, `StatusExpired`=만료, `StatusRemoved`=강제 정리 |
| 280 `AnkleBreaker` | 발목 부수기 | 타격한 적에게 이동 제한 부여 | `StatusApplied` | `StatusConsumed`, `StatusExpired`, `StatusRemoved` |
| 290 `Cohesion` | 결속 | 주변 아군 수에 따른 자신의 방어 보너스와 주변 아군 보호막 | `DefenseChanged`, `ShieldGranted`, `ShieldAbsorbed` | `ShieldRemoved`, `ShieldReset`. 방어 변화는 증가·감소 모두 가능 |
| 300 `Sacrifice` | 희생 | 적 진영 턴의 치명적인 아군 타격을 조건에 맞는 수호자가 대신 받음 | `DamageRedirected` | 일반 발동 사건 대신 공격 결과의 가로채기 반응에 포함. 횟수·반경 조건은 Core |
| 310 `Taunt` | 도발 | 적 AI의 공격·추격 대상 선정에 개입 | `TauntSelected` | 발동자=도발 소유자, 대상=선택을 수행한 적 AI 유닛. 피해 대상이라고 해석하지 않음 |
| 320 `Entrenchment` | 진지 구축 | 주변의 다른 아군이 받는 공격 피해 경감 | `DamageModified` | 발동자와 실제 보호받은 대상이 다름 |
| 330 `SteadfastWill` | 굳건한 의지 | 밀림을 막음 | `KnockbackPrevented` | 실제 이동이 생긴 것으로 표현하지 않음 |
| 340 `GroundCompaction` | 땅 다지기 | 자발 이동 경로에 흔적을 남기고 해당 칸의 이동 범위 보너스 제공 | `TrailCreated`, `RangeChanged` | 흔적 수명 변화 `TrailChanged`, `TrailRemoved`. 지속 외형은 `trailPrefab` |
| 350 `DustScreen` | 먼지 연막 | 이동 경로에 흔적. 해당 칸의 공격 사거리 감소·받는 공격 피해 경감 | `TrailCreated`, `DamageModified` | `TrailChanged`, `TrailRemoved`. 사거리 변화 자체를 위한 별도 `RangeChanged` 생산 경로는 현재 없음 |
| 360 `Leap` | 뛰어넘기 | 이동 규칙에서 통과 특례 제공 | `MovementChanged` | 통과 칸 등 Core가 준 사실을 표현. 실제 점프/이동은 기존 이동 연출 |
| 370 `Acceleration` | 가속 | 자발 이동한 칸 수에 따라 공격력을 축적하고 공격 후 소모 | `AttackGranted`, `StatusConsumed` | 정리 시 `StatusReset`. 소모 사건의 시점을 발사 순간으로 가정하지 않음 |
| 380 `StationaryFire` | 고정 사격 | 자신의 진영 턴에 이동하지 않았으면 공격 피해 증가 | `DamageModified` | 이동함 표시 `StatusApplied`, 자기 턴 시작/정리 `StatusReset` |
| 390 `ArmorBreaker` | 방어구 부수기 | 타격 뒤 살아 있는 실제 수신자에게 방어 감소 스택 추가 | `ArmorBroken` | 정리 시 `StatusRemoved`. 스택의 잔존 방어 비율 계산은 Core |
| 400 `FinishingShot` | 마무리 사격 | 대상 현재 HP가 공격력 기반 임계값보다 작으면 즉시 격파 | `InstantDefeat` | 현재 HP와 공격력의 비교이며 최대 HP 비율 조건이 아님. 사망 연출은 기존 사망 사건 |
| 410 `TrackingShot` | 추적 사격 | 대상의 현재 HP/최대 HP 비율이 임계값 이상이면 피해 증가 | `DamageModified` | 조건과 증폭량은 InGame 설정 |
| 420 `Hunter` | 사냥꾼 | 최대 현재 HP의 적 중 표적을 선정·유지. 자신이 표적을 처치하면 제한 횟수 내 지속 공격력 보상 | `HunterTargetSelected`, `AttackGranted` | 표적 제거 `StatusRemoved`, 스테이지 정리 `StatusReset`. 표식(190)과 다른 특성 |
| 430 `FocusedBombardment` | 일점 포격 | 공격 범위에서 포착한 적 수가 설정값과 정확히 같으면 피해 증가 | `DamageModified` | “이상”이 아니라 개수 일치. 공격 시작의 포착 값을 사용 |
| 440 `PositionSwap` | 위치 교환 | 정상 이동의 아군 끝점과 위치 교환. 승객은 강제 이동 사실만 받음 | `MovementChanged` | 두 Moved 배치를 함께 표시. PathMovement는 주체만 |
| 450 `ReconSquad` | 정찰 부대 | 자발 이동의 시작/실제 진입 주변 적에 약점 노출. 같은 이동 접촉 중복은 Core가 정리 | `StatusApplied` | 대상 귀환/사망까지. 생산자 이탈로 지우지 않음. 수치는 같은 Core 설정 조회 |

주요 근거: [Core 규칙 폴더](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Traits/Rules/), [발동 생산/적용 폴더](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/), [공격 시작 알림](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/TraitAttackNotifications.cs), [상태·보호막 재조정](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/TraitPresenceReconciler.cs), [턴 수명](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/TraitTurnLifecycle.cs), [정리](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Application/Combat/CombatTemporaryStateCleanup.cs).

## 3. 시스템에서 막아 둔 특성

### 3.1 턴 규칙 보류

| 키 / 코드 | 표시명 | 원래 효과 개념 | 현재 차단 | Activation 저작 판단 |
|---|---|---|---|---|
| 10 `Focus` | 집중 | 이동하지 않고 공격한 누적에 따른 추가 피해 | `DeferredUnitTurnTiming` | `FocusGainScheduled`·`FocusStackApplied`·`FocusReset`은 enum에 남았지만 현재 정상 인게임 생산 대상으로 쓰지 않음 |
| 40 `ThornField` | 가시밭 | 가시 지형 생성·피해·방어 감소 | 동일 | 현재 인게임 발동 보류. 새 수명 정책 없이 연출만 붙여 사용 가능하다고 표시하지 않음 |
| 80 `Necromancy` | 사령술 | 보스의 적 소환 | 동일 | 소환은 별도 소환 사건 계약이며 `Summoned`라는 TraitActivationKind는 없음 |

### 3.2 Retired

아래 효과·Kind는 **삭제 전 기능 설명/과거 대응**이다. 현재 등록·발동 구현으로 오해하지 않는다. 역사 확인 기준은 삭제 커밋 `98fd0c642`의 부모다.

| 키 / 코드 | 이름 | 삭제 전 효과 개요 | 과거 대응 Kind | 현재 외형 SO |
|---|---|---|---|---|
| 20 `MoveAttackDiscount` | 이동 후 공격 할인 | 현재 행동 사용 체계에서 제외 | `AttackDiscountArmed`, `AttackCostReduced`, `AttackDiscountExpired` | SO/GUID 보존, 활성 표 제외 |
| 130 `DoorBreaching` | 도어 브리칭 | 가까운 거리 대상 방어 무시 | `DefenseBypassed` | 있음, Activation 줄 없음 |
| 140 `ContemptForWeak` | 약자 멸시 | 공격력 비교 조건에 따른 대상별 피해 증폭 | `DamageModified` | 없음 |
| 150 `PrecisionShot` | 정밀 사격 | 첫 실제 수신자의 방어 무시 | `DefenseBypassed` | 없음 |
| 160 `DoubleShot` | 2연발 | 주변 적 처치 시 같은 방향 추가 공격, 자기 파생 공격으로 재발동하지 않음 | `FollowUpAttack` | 있음, 합성 확인용 줄 1개 |
| 170 `LuckyBullet` | 행운의 탄환 | 난사 발동 시 적 건물·보스 중 추가 표적 공격 | `FollowUpAttack` | 있음, 줄 없음 |
| 180 `ChainExplosion` | 연쇄 폭발 | 실제 밀려난 뒤 살아 있는 대상에 추가 피해 공격 | `FollowUpAttack` | 있음, 줄 없음 |

비용 할인은 이번 Core 변경으로 Retired에 포함됐다. 현재 소스에서는 2연발도 다시 `IsTraitRetired=true`이며 기본 등록·발동 구현이 없다. 미리보기의 2연발 줄과 Shake는 실제 특성이 복원됐다는 증거가 아니다. 이전 대화/작업 기록보다 이 문서 확인 시점의 소스를 우선한다.

## 4. TraitActivationKind 전체 36종 — 실제 사용과 의미

**이 표의 모든 유효 Kind는 `TraitVisualDataSO.activations[].cues`에서 동일한 제약을 쓴다. Anchor=`Start`만, 이펙트 Point=`ActorBody`·`SubjectBody`·`EventCell`만 가능하다.** Kind에 따라 Hit·Contact가 새로 허용되는 구조가 아니다. Point는 해당 사건에 실제 몸/대상/칸이 있어야 보인다(§6).

| ID / Kind | 현재 의미·생산 예 | 현재 인게임 상태 / 작성 주의 |
|---|---|---|
| 10 `FocusGainScheduled` | 집중 누적 예약 | 집중 보류, 정상 발동 기대 불가 |
| 11 `FocusStackApplied` | 집중 누적 적용 | 동일 |
| 12 `FocusReset` | 집중 리셋 | 동일 |
| 20 `AttackDiscountArmed` | 공격 할인 준비 | 이동 후 공격 할인 Retired |
| 21 `AttackCostReduced` | 공격 할인 사용 | 동일 |
| 22 `AttackDiscountExpired` | 미사용 할인 만료 | 동일 |
| 70 `KillChainAttack` | 난사 추가 공격 1회 시작 | 사용 가능. `ChildAttack`이 있어야 실제 추가 공격 재생 |
| 80 `FollowUpAttack` | 옛 2연발·행운의 탄환·연쇄 폭발의 추가 공격 | enum/미리보기 ChildAttack 계약은 남았지만 현재 해당 Core 생산 구현 없음 |
| 90 `AttackGranted` | 공격력 부여·획득 | 선봉장·깔끔한 뒷정리·사기 증진·가속·사냥꾼 |
| 100 `HealthRecovered` | 실제 회복 | 피의 부름·재생. 설정량이 아닌 실제 회복량. HP는 별도 체력 사실로 표시 |
| 110 `DamageRedirected` | 희생의 가로채기 관계 | 공격 결과 안 반응. 일반 Moment로 만든 것과 구분 |
| 120 `ArmorBroken` | 타격 후 방어구 감소 스택 추가 | 방어구 부수기 |
| 130 `HunterTargetSelected` | 사냥꾼 표적 선정·교체 | 대상은 새 표적. 지속 표식 자체를 새로 소유하지 않음 |
| 140 `DamageModified` | 피해 증폭·감쇠·경감의 기여 | 관통/산탄/진지 구축/먼지 연막/고정 사격/추적 사격/일점 포격. 증가 전용으로 해석 금지 |
| 150 `DefenseBypassed` | 방어 무시 기여 | 공용 계산·검증 구조에는 있음. 현재 특성 등록에서 방어 무시 생산자는 없어 정상 발동 기대 불가 |
| 160 `StatusApplied` | 상태 부여 또는 상태 플래그 변경 | 위압·표식·무장 해제·발목 부수기, 고정 사격의 이동함 표시 등 |
| 170 `StatusConsumed` | 상태 소모·남은 수명 차감 | 가속/위압 소모 또는 공격력 부여·제한 턴 차감. 약점 노출을 일회 소모 표식으로 취급하지 않음. 같은 의미의 “공격 발동”으로 통일하지 않음 |
| 180 `StatusRemoved` | 표적·버프·디버프 강제 제거/정리 | 출처·대상이 비어 있을 수 있음. 사망/전투 정리와 같은 실제 생산 지점을 확인 |
| 190 `StatusExpired` | 제한·공격력 부여 등 상태 수명 만료 | 자연 만료와 강제 제거의 구분 |
| 200 `StatusReset` | 사냥꾼/가속/고정 사격 등 상태 초기화 | `Magnitude=0`도 의미 있는 사건 |
| 210 `ShieldGranted` | 보호막 부여 | 단독 행동·결속 |
| 220 `ShieldAbsorbed` | 보호막이 실제 피해를 흡수 | 단독 행동·결속. 실제 흡수량은 Core |
| 230 `ShieldRemoved` | 보호막 적용 조건 해제/정리 | 같은 두 특성. 보호막 파괴 타격이라고 단정하지 않음 |
| 240 `ShieldReset` | 보호막 예산 주기 리셋 | `Magnitude=0`도 가능. 수치 증가 전용 표현 금지 |
| 250 `MovementChanged` | 뛰어넘기 등 이동 계산 기여 | 실제 이동은 별도 이동 사건 |
| 260 `TrailCreated` | 땅 다지기·먼지 연막 흔적 생성 | `TargetCells` 기반 EventCell 활용 |
| 270 `TrailChanged` | 흔적 잔여 수명/활성 상태 변화 | 새 흔적 생성으로 표현하지 않음 |
| 280 `TrailRemoved` | 흔적 종료 | 지속 프리팹 제거는 기존 흔적 소비자 |
| 290 `KnockbackApplied` | 넉백·충격 볼트의 밀림 결과 | 0칸 결과도 가능. 큐만으로 이동 성공 판정 금지 |
| 300 `KnockbackPrevented` | 굳건한 의지로 밀림 방지 | 버팀 표현. 실제 이동을 새로 만들지 않음 |
| 310 `PullApplied` | 끌어오기 경로 적용 | 별도 당김 이동과 조립된 사건 |
| 320 `TauntSelected` | 적 AI가 도발 때문에 대상을 선정 | 발동자=도발자, 대상=적 AI |
| 330 `RangeChanged` | 땅 다지기의 이동 범위 기여 | 모든 사거리 변화의 공통 발동이라고 가정하지 않음 |
| 340 `DefenseChanged` | 결속 방어 보너스 증감·정리 | 감소도 포함 |
| 350 `InstantDefeat` | 마무리 사격의 즉시 격파 | 사망 사건/사망 연출과 중복 실행 금지 |
| 360 `RoarTriggered` | 밤 누적으로 포효 반응 시작 | 별도 포효 공격 경로와 함께 사용 |

`None=0`은 줄의 Kind로 사용할 수 없다. 유효 enum에 속한 Kind라도 **그 특성이 실제로 생산하지 않는 조합이면 줄을 작성해도 정상 인게임에서 매칭되지 않는다.** 입력 Validate는 현재 “이 TraitKey에 이 Kind가 맞는가”까지 목록으로 제한하지 않는다.

원본: [TraitActivationKind](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Common/TraitActivationKind.cs), [TraitActivationVisual 검증](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/Trait/Item/TraitActivationVisual.cs), [변경 증거 검증](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Combat/CombatChangeEvidenceReader.cs).

## 5. Anchor 표 — Activations와 공격 표시를 구분

### 5.1 Anchor 값과 뜻

| 값 / 이름 | 기준 순간 | Trait Activations에서 사용 |
|---|---|---|
| 0 | 미정의 값 | **오류**. Start가 아님 |
| 1 `Start` | 해당 실행 시작. 특성 큐에서는 그 발동 사실을 소비하는 순간 | **허용** |
| 2 `Cue` | 동작의 발사/타격 기준 신호. 클립이면 지정 이벤트, 코드 모션이면 그 기준 순간 | 불가 |
| 3 `Hit` | 공격 대상의 맞는 순간 | 불가 |
| 4 `Contact` | 투사체 접촉 순간 | 불가 |
| 5 `End` | 동작/반응의 끝 | 불가 |

Anchor 0 오류가 나는데 Inspector에는 Start로 보인다면 다른 값으로 바꾼 뒤 Start를 다시 선택·저장하고 재생한다. 유효한 저장값은 `anchor: 1`이다. 표시가 Start라는 이유만으로 실제 저장값 0을 유효한 것으로 해석하지 않는다.

### 5.2 큐가 붙는 칸(CombatCueEvent)별 허용 Anchor·Point

이 표는 **이펙트 큐** 기준이다. Shake·Sound·Flash의 추가 제약은 §7을 따른다. 허용 Anchor와 Point는 동시에 만족해야 한다.

| 큐가 붙는 칸 | 허용 Anchor | 허용 Effect Point | 추가 조건 |
|---|---|---|---|
| `TraitActivation` | Start | ActorBody, SubjectBody, EventCell | 모든 TraitActivationKind 공통 |
| `MeleeAttack` | Start, Cue, Hit, End | ActorEffectPoint, ActorBody | 지점 사용은 해당 공격 클립·프리팹 연결 필요 |
| `RangedAttack` | Start, Cue, Hit, End | ActorEffectPoint, ActorMuzzle, ActorBody | Linear·Mortar. 지점은 공격 클립·연결 필요 |
| `RangedAttack`의 접촉 | Contact | Contact | **이펙트만**, Delay=0. 다른 Point와 조합 불가 |
| `Hit`(피격 반응 칸) | Start, Cue, End | SubjectBody | Cue는 피격 클립이 있어야 함. Anchor Hit와 “피격 칸 Hit”는 다른 개념 |
| `Death` | Start, End | SubjectBody | 사망 반응 동작 |
| `ItemActivated` | Start | SubjectBody | 아이템 소지자 |
| `Tornado` | Start, End | SubjectBody | Start=휩쓸림, End=착지. **Delay=0** |
| `ObjectActivated` | Start | EventCell | 오브젝트 발동/획득 칸 |
| `ObstacleHit` | Start | EventCell | 장애물 피해 칸 |
| `ObstacleDestroyed` | Start | EventCell | 장애물 파괴 칸 |
| `Fall` | Start | EventCell | 추락 칸 |
| `WeakGroundCollapse` | Start | EventCell | 약한 지반 붕괴 칸 |

현재 공격 표시 Kind는 **Melee=2, Linear=1, Mortar=3**이다. Melee만 근접 규칙, 나머지 둘은 원거리 규칙이다. 삭제된 옛 Kind 숫자 4를 사용하지 않는다.

원본: [CombatPresentationCueRules](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/CombatPresentation/CombatPresentationCueRules.cs), [공격 표시 Kind](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/CombatPresentation/CombatAttackPresentationKind.cs).

## 6. Point 표 — 누구/어디에 이펙트를 내는가

Point는 이펙트의 위치·회전 기준이다. **Shake에는 Point가 없다.**

| 값 / Point | 의미 | Activations 허용 | 위치/대상이 없을 때 |
|---|---|---|---|
| 0 | 미정의 | 불가, 검증 오류 | 다른 자리로 자동 보정하지 않음 |
| 1 `ActorEffectPoint` | 행위자 공격 클립의 effectPoint 위치·회전 | 불가 | 일반 공격에서도 클립·지점 누락이면 구성 오류 |
| 2 `ActorMuzzle` | 행위자 발사 지점 위치 + 행위자 자세 방향 | 불가 | 원거리 공격 클립·muzzle 필요 |
| 3 `ActorBody` | 발동 유닛 몸 상자 중심·모델 방향 | 허용 | 발동 유닛의 보이는 몸이 없으면 해당 이펙트 없음 |
| 4 `SubjectBody` | 발동 사실 `TargetIds`의 대상 몸마다 | 허용 | 대상 목록이 비었거나 몸이 없으면 해당 대상에 이펙트 없음 |
| 5 `Contact` | 투사체 접촉 위치·진행 방향 | 불가 | 원거리 공격 표시의 Contact Anchor에서만 사용 |
| 6 `EventCell` | 발동 사실 `TargetCells`의 칸 표면 중심·필드 방향 | 허용 | 칸 목록이 비면 이펙트 없음. 바깥 칸/필드 연결 오류는 별도 예외 |

| 흔히 혼동하는 상황 | 실제 규칙 |
|---|---|
| 대상이 여러 명 | SubjectBody 이펙트는 찾은 대상 몸마다 하나씩 생성 |
| 영향 칸이 여러 개 | EventCell 이펙트는 칸마다 생성 |
| SubjectBody인데 TargetIds가 비어 있음 | 발동자 몸으로 대체하지 않음 |
| Target Cell을 입력함 | 공격 조준용 `TargetCell`과 이펙트 자리용 `TargetCells`는 다른 값. 단일 조준 칸을 자동으로 EventCell 목록에 복사하지 않음 |
| 사냥꾼의 표적 해제 | TargetId가 null일 수 있어 ActorBody가 있어도 SubjectBody는 없을 수 있음 |
| 희생 | ActorBody=수호자, SubjectBody=지켜진 대상. 피해를 받는 실제 수신자와 같다고 가정하지 않음 |
| 도발 | SubjectBody=결정한 적 AI 유닛. 도발 소유자에 내고 싶으면 ActorBody |
| Position Offset | 선택한 자리의 오른쪽·위·앞 기준 월드 거리. 자동 필드 배율 적용 아님 |
| Rotation Offset | 자리 회전 뒤에 더하는 로컬 오일러 회전 |

원본: [CombatCuePoint](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/CombatPresentation/CombatCuePoint.cs), [실제 자리 해석](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Presentation/Pieces/CombatCuePoints.cs).

## 7. Effects·Shakes·Sound·Flash 차이

| 종류 | Activations 직접 입력 | 타이밍/제약 | 실행·완료 |
|---|---|---|---|
| Effects | 가능 | Start + Delay, Point 3종. EffectType은 Render DB에 등록된 실제 리소스여야 함 | 파티클의 자연 종료를 다음 사건의 완료 조건으로 삼지 않음 |
| Shakes | 가능 | Start + Delay, Point 없음. Strength=0이면 꺼짐. 세기/Delay는 유한한 0 이상 값, 시간·모양은 카메라 효과 계약의 검증도 받음 | 개별 Shake 완료를 기다려 다음 사건을 막지 않음 |
| Sound | 이 큐 입력에는 필드 없음 | 특성/사건 소리 표의 기존 계약을 따름. 공격 표시에는 별도 소리 시점이 있음 | 임의의 Sound 필드를 Activations YAML에 넣는 것으로 추가하지 않음 |
| Flash | 이 큐 입력에는 필드 없음 | 피격 반응 칸에서만 Start/Cue 허용 | 피격 Flash는 그 실행의 완료 대기 대상 |

공격·피격·사망 쪽 큐를 설정할 때도 **Shake는 Contact 불가**다. 투사체가 맞는 순간의 Shake는 공격 표시의 **Hit**에 둔다. Contact에서는 이펙트만, Delay=0이며 Point도 Contact여야 한다. 토네이도는 Shake도 Delay=0이다.

Shake가 설정돼도 보이지 않을 수 있는 조건:

| 조건 | 결과 |
|---|---|
| 맵 가상 카메라 또는 활성 Impulse Listener/수신 채널 없음 | Shake를 보내지 않음 |
| 진행 중인 Shake보다 세기가 같거나 작음 | 새 Shake를 겹쳐 보내지 않음. 더 센 Shake만 전송 |
| Strength=0 | 해당 Shake 꺼짐 |
| 유효한 발동 사실 없음 | 줄 자체가 실행되지 않음 |
| Delay가 있음 | 그 시각으로 미룰 뿐 다음 사건이 기다려 주지는 않음 |

현재 미리보기는 생성한 맵 안의 가상 카메라를 읽어 기존 카메라 효과 창구에 연결한다. 창의 `맵 카메라 없음` 안내는 카메라 목록 검사이며, **목록에 카메라가 있다는 사실만으로 Listener 수신 가능까지 보장하는 것은 아니다.**

원본: [큐 입력 타입](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/CombatPresentation/CombatEventCues.cs), [큐 값 검증](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/CombatPresentation/CombatCueEntries.cs), [큐 조립](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Presentation/Pieces/CombatCueComposer.cs), [카메라 Shake](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Presentation/Cameras/CombatCameraWindow.cs).

## 8. Attack Presentation·Unit Action의 사용 가능/무시/오류

### 8.1 Attack Presentation

| 설정/상황 | 결과 |
|---|---|
| 실제 발동에 `ChildAttack` 결과가 있음 | 그 추가 공격을 재생. Kind 이름만으로 피해를 새로 계산하지 않음 |
| Attack Presentation 비움 | 발동 유닛의 기본 공격 표시 사용 |
| ChildAttack 없는 일반 발동에 Attack Presentation 지정 | 추가 공격을 만들어 주지 않음. 덮어쓰기 사용 안 함 |
| 일반 공격의 Hit에 Shake를 넣음 | 그 공격이 맞는 시점에 흔들림 |
| 특성의 추가 공격 표시 Hit에 Shake를 넣음 | 해당 추가 공격이 맞는 시점에 흔들림 |
| 특성 덮어쓰기 Effects에 ActorEffectPoint/ActorMuzzle 사용 | **DB 검증 오류**. 어떤 유닛이 특성을 보유할지 모르므로 금지 |
| 덮어쓰기의 공격 계열이 공격 클립과 다름 | 경고 후 유닛 기본 공격 표시로 대체 |

특성 덮어쓰기에서 사용할 수 있는 이펙트 자리는 근접=ActorBody, 원거리의 일반 시점=ActorBody, 원거리 Contact=Contact다. 이는 §5 일반 공격 규칙에 **특성 덮어쓰기의 지점 금지**를 추가 적용한 결과다.

### 8.2 Unit Actions 목록

한 Activation 줄에 행동을 여러 개 넣을 수 있다. 서로 다른 역할의 행동을 **동시에 시작**하며, 목록 순서대로 실행하는 타임라인은 아니다. 같은 활성 Actor 역할을 두 번 넣거나 null 항목을 넣으면 검증 오류다. 빈 목록과 Actor=None 항목은 행동이 없다.

| 항목/상황 | 허용 또는 실제 처리 |
|---|---|
| Actor=None | 행동 없음. 기본값 |
| Actor=Source | 발동 유닛 한 명이 행동 |
| Actor=Targets | 발동 대상마다 행동. 일반 발동에서 행동의 기준 대상은 발동 유닛 |
| Place=Stay | 제자리 방향 동작 |
| Place=TowardTarget | 기준 대상 쪽으로 이동. Source의 대상이 여러 명이면 첫 대상 사용 |
| Place=GuardFront | 가로채기의 지켜진 유닛 앞. 일반 발동에서 고르면 경고 후 Stay로 대체 |
| Place=AwayFromAttacker | 가로채기에서 자기 원래 자리 기준 공격자 반대쪽으로 이동. 일반 발동은 공격자 기준점이 없어 경고 후 Stay로 대체 |
| 거리 | 0~1.5칸 비율. 범위 밖이면 검증 오류 |
| Out/Hold/Return Seconds | 유한한 0 이상 값. 음수/비정상 값은 오류 |
| 실제 추가 공격을 하는 발동에 UnitAction 지정 | 공격이 자세를 사용하므로 행동은 경고 후 건너뜀. 큐·공격은 재생 |
| 희생의 가로채기 | Source=수호자, Targets=보호 대상. 모든 나가기 완료 뒤 공격. 수호자 및 행동 유닛의 피격·사망 후속 처리 뒤 복귀. Hold Seconds 사용 안 함, 죽은 유닛은 복귀하지 않음 |
| 행동 유닛의 몸이 없음 | 행동 안 함 |
| 행동 유닛이 다른 연출 중이거나 자리 대여 불가 | 행동만 경고 후 건너뜀. 발동 큐는 계속 가능 |
| 대상이 없음 | 대상 쪽 이동은 제자리, 대상 보기는 현재 방향을 사용 |

희생은 `Source / GuardFront`와 `Targets / AwayFromAttacker`를 함께 넣으면 수호자가 앞으로 나갈 때 보호 대상이 뒤로 물러난다. 수호자의 목적지는 두 유닛이 움직이기 전 보호 대상 자리로 고정한다. 같은 공격 안에서 한 유닛이 여러 가로채기에 중복되면 첫 행동을 유지한다.

이 행동은 화면의 자세만 바꾸며 Core 위치·이동 규칙을 변경하지 않는다. 넉백/끌어오기/뛰어넘기를 UnitAction으로 다시 구현하지 않는다.

원본: [TraitActivationVisual](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/Trait/Item/TraitActivationVisual.cs), [유닛 행동 입력](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/CombatPresentation/CombatUnitActionInput.cs), [특성 처리기](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Playback/Handlers/CombatTraitEventHandler.cs).

## 9. 현재 실제 SO에 작성된 줄

활성 표의 등록과 실제 Kind 줄을 각각 확인한다. SO가 존재한다는 사실만으로 선택/실행할 수 있는 것은 아니다.

| SO / 특성 | 현재 Kind | 실제 입력 | 인게임 판단 |
|---|---|---|---|
| `TraitVisual_Acceleration` | AttackGranted(90) | StateApplied, Source, 이동 칸 수, 공통 몸체 텍스트 | 실제 완료 이동 근거 필요 |
| `TraitVisual_CleanSweep` | AttackGranted(90) | StateApplied, Source, 공통 몸체 텍스트 + 기존 Effect5 | canonical 해당 출처 공격력 부여 근거 |
| `TraitVisual_HuntersMark` | StatusApplied(160) | StateApplied, Targets, Count None, 공통 몸체 텍스트 | 약점 노출 출처 추가/값·수명 갱신. Magnitude=1은 증가율 아님 |
| `TraitVisual_Sacrifice` | DamageRedirected(110) | ActivationStart, Source. 기존 동시 나가기/피격 뒤 복귀 행동과 Effect 유지 | 실제 가로채기 반응 경로 |
| `TraitVisual_DoubleShot` | FollowUpAttack(80) | 기존 Effect/Shake/공격 표시 SO 보존 | Retired·활성 미등록이면 시연도 진단 |

이 표는 값이 작성돼 있는지에 대한 정적 목록이다. 모든 리소스의 실제 화면 품질·연출 강도 검증을 뜻하지 않는다. 등록된 EffectType/효과 프리팹은 Render DB를 함께 확인한다.

| 기타 외형 필드 | 현재 사용 |
|---|---|
| `icon` | 특성 정보 카드의 외형. 효과 HUD는 별도 UnitStatusVisual의 효과 키로 조회 |
| `displayName` / `description` | 표시 이름·설명. Core 계산의 원본이 아님 |
| `trailPrefab` | 땅 다지기·먼지 연막에 지속 흔적 외형이 연결돼 있음 |

현재 에셋 위치: [TraitVisual 폴더](../../../Assets/ProjectL/DataBaseSO/ViewGroup/TraitVisual/Item/). 값은 이후 사용자 편집으로 바뀔 수 있다.

## 10. 미리보기에서 되는 것과 실제 발동 검증의 차이

2026-10-31 조립 재생에서는 아래 발동 문맥을 이동·공격·상태 변경·대기와 함께 묶을 수 있다. 가속/특성 공격력 부여를 명시적으로 설정·제거하면 기존 버프 HUD가 전후 상태를 표시한다. 사건의 발동 조건과 수식은 여전히 합성 입력이며 자동 판정하지 않는다. 조립 순서·공격 후속 제약은 [사용 안내 §0](trait-activation-preview-guide.md#0-조립-재생으로-전후-상황-확인하기)을 따른다.

| Context | 합성하는 내용 | 적합한 확인 | 하지 않는 것 |
|---|---|---|---|
| Moment | 특성 발동 사실 하나 | Start 큐·다중 대상/칸·일반 UnitAction | 실제 발동 조건, 버프/보호막/흔적의 전체 수명 검증 |
| ChildAttack | 추가 공격 시작 발동 + 그 피해/사망 결과 | 발동 큐·기본/덮어쓴 공격·투사체·피격 | 앞선 일반 공격·처치·실제 특성 판정. 현재 첫 공격은 포함하지 않음 |
| Interception | 공격 결과 안 가로채기 관계와 수호자 피해/사망 | 희생의 선행 이동·피격·복귀/사망 | 실제 치명타 여부·수호자 선정·진영 횟수 제한 판정 |

Context도 자유롭게 섞을 수는 없다. `ChildAttack`은 `FollowUpAttack` 또는 `KillChainAttack` Kind만 추가 공격 결과를 붙일 수 있다. `Interception`은 `DamageRedirected` Kind와 수호자/보호 대상/실제 수신자의 참조 계약을 만족해야 한다. 잘못된 조합은 스냅숏 생성 검증에서 예외가 난다. `Moment`는 추가 공격·가로채기 결과를 붙이지 않는 합성 문맥이다.

| 제한/오해 | 올바른 해석 |
|---|---|
| Retired 프리셋/에셋이 남아 있음 | 보존 자료. 활성 표 미등록 외형은 선택/구성 검사에서 거절. Core 구현 복원 아님 |
| 줄 없음도 확인 | 특성 전용 큐가 없는 경로를 확인. 기능 활성화 버튼이 아님 |
| 미리보기 HP 감소 | 사용자가 입력한 Damage/Kill의 가상 결과. 실제 전투 HP·저장 데이터 변경 아님 |
| Observation Seconds 증가 | 재생 완료 뒤 관찰 시간 증가. 첫 재생 지연/애니메이션 속도 변경 아님 |
| 2연발 Shake가 즉시 나옴 | ChildAttack 미리보기는 추가 공격 발동부터 시작하기 때문. 첫 일반 공격은 합성하지 않음 |
| SO 수정 후 인게임 변화 없음 | 발동 Kind·보유 특성·가용성·실제 생산 사실 확인. 줄은 전투 연결 동안 캐시하므로 전투 재진입 또는 Play 재시작 후 확인 |
| Delay로 타격 시점 맞추기 | Start 뒤 고정 시간일 뿐 공격의 Hit 신호와 연결되지 않음. 공격 타격 순간은 공격 표시의 Hit 사용 |

예시:

| 원하는 연출 | 설정 위치 | Anchor / Point |
|---|---|---|
| 깔끔한 뒷정리로 자신 공격력 획득 | 해당 SO Activations → AttackGranted → Effects | Start / ActorBody 또는 사건 대상 SubjectBody |
| 난사 추가 공격 시작에 흔들림 | 해당 SO Activations → KillChainAttack → Shakes | Start / Point 없음 |
| 난사 추가 공격이 맞을 때 흔들림 | 해당 줄의 Attack Presentation → Shakes | Hit / Point 없음 |
| 난사 투사체 접촉 이펙트 | 추가 공격용 원거리 Attack Presentation → Effects | Contact / Contact / Delay=0 |
| 땅 다지기 흔적 생성의 순간 입자 | Activations → TrailCreated → Effects | Start / EventCell |
| 땅 다지기 지속 지형 | TraitVisualDataSO의 Trail Prefab | Activation Anchor/Point로 수명 관리하지 않음 |
| 희생 수호자가 앞으로 나가 대신 맞음 | DamageRedirected → Unit Actions | Source / GuardFront. 큐 Anchor와 별도 행동 설정 |
| 희생의 기존 피격 대상이 뒤로 물러남 | 같은 줄 → Unit Actions에 항목 추가 | Targets / AwayFromAttacker / Face Attacker. 화면상의 이동이며 피격 대상의 Core 칸은 유지 |
| 2연발을 인게임에서 사용 | 현재 불가 | Retired·구현/등록 제거. 줄·아이콘·Shake만 추가해 해결되지 않음 |

미리보기 사용법: [Trait Activation Preview 안내](trait-activation-preview-guide.md). 이전 타이밍 문서/37종 작성 계획의 과거 구현·가용성 설명은 현재 상태 판정에 사용하지 않는다.

## 11. 변경 시 유지할 경계

| 점검 | 이 문서의 기준 |
|---|---|
| SSOT | 사용 가능 상태는 Core, 효과 수치는 InGame 정의, 발동 사실은 Core 결과, 연출 값은 View SO, 허용 Anchor/Point는 CombatPresentationCueRules |
| 역할 분리 | Core는 계산·사건 생산, View는 받은 사실의 표현, Factory는 의존성 조립 |
| 기능 독립성 | 큐 값 변경 때문에 Core 규칙·운영 진행·다른 UI를 수정하지 않음. 기능 복원은 별도 Core 작업 |
| Framework 계약 | 기존 서비스·Buffer·재생/취소/자원 반환 경로 사용. 연출 편의용 루트 서비스 재등록/새 수명 소유자 생성 없음 |
| 확인 한계 | 이 문서는 코드·SO 대조 결과. 사용 가능 판정과 실제 조작/화면 검증 완료 여부를 구분 |

이 문서의 특성 목록·Kind 전체 목록·현재 에셋 줄 수를 코드/파일과 대조했다. 컴파일·테스트·게임 실행의 새 통과 결과를 주장하지 않는다.


## 2026-10-31 몸체 텍스트 확장

Activations에 Cue Timing과 선택적 Body Text가 추가됐다. 첫 연결은 가속(이동 끝 `가속 X N`), 깔끔한 뒷정리(HUD 상태 갱신 직후), 희생(수호자 나가기 시작)이다. 기존 미설정 줄은 텍스트 없이 ActivationStart를 유지한다. StateApplied 지원 조합과 설정 방법은 [몸체 텍스트 사용법](trait-body-feedback-guide.md)에 있다.


몸체 텍스트의 폰트·색·크기·배경·위치 보정은 프리팹 Label/Content가 원본이다. 기본 외형은 `CombatPresentationSettings_Default > Body Text Prefab`, 개별 외형은 `Body Text > Prefab Override`로 연결한다. 구형 Body Text Font·줄별 Color/Font Size/Screen Offset는 제거했으며 문구/대상/시간은 줄에 유지한다.


## 12. 현재 효과 HUD와 카드 설정

특성은 효과의 출처이고 아이콘은 수신자의 **효과 키당 하나**다. 예를 들어 가속+깔끔한 뒷정리는 AttackIncrease 하나, 사냥꾼의 표식은 대상의 WeaknessExposure 하나로 표시한다. Core Capture의 버프→디버프/키 순서를 유지한다.

| 효과 키 | HUD 요약 | 상세의 의미 |
|---|---|---|
| AttackIncrease1 | 총 증가량·일반 부여·Held | 출처별 값/관리 턴 수명, Held는 다음 공격 완료까지 |
| AttackDecrease2 | 유효 공격력 감소율 | 출처별 감소율/다음 공격 완료까지 |
| DefenseIncrease3 | 현재 방어 보너스 | 현재 적용 조건. 가짜 출처/잔여 턴 없음 |
| DefenseDecrease4 | Core 유효 방어 | 출처별 감소율/전투 종료까지 |
| Shield7 | Core 유효 보호막 예산이 양수일 때만 아이콘 | 활성 표시 중 비활성 출처도 상세에 유지. 예산0/비활성 이력만 있으면 칩 없음. 남은 턴 아님 |
| Disarm10 / Bind11 | 공격 제한 / 이동 제한 | 출처별 대상 진영 턴 종료 잔여 횟수 |
| WeaknessExposure13 | Core 유효 받는 피해 증가율 | 모든 출처별 값. timed는 대상 진영 잔여 횟수, untimed는 대상 귀환/사망까지 |

다른5키는 현재 Capture가 상시 행을 제공하지 않는다. 보유 특성만 보고 효과를 합성하지 않는다. 출처별 남은 수명을 하나로 합치지 않으며 출처 사망/이탈을 View가 만료로 판정하지 않는다. 같은 키 갱신은 기존 슬롯·고정 설명을 갱신하고 소멸/Clear/풀 반환은 정리한다.

약점 노출2출처 20%/2·30%/1이면 한 아이콘의 요약은30, 상세는 두 줄이다. 다음 상태를20%/1로 받으면 같은 슬롯이 바뀐다. HUD 제목은 **약점 노출**, 몸체 발동 문구는 기본 **사냥꾼의 표식**이다. 전용 깨진 방패 Sprite는 아트 인수 대기다.

정책 설명은 TraitVisual SO, 설정량/기간은 같은 Core Snapshot이다. 카드의 표식 증가율/기간은 전투 안팎 모두 연결했다. 위압/재생의 전투 수치는 TraitStatuses로 연결했지만 **전투 밖 정량/강화 설명은 CVI-R1 응답 대기**다. View가 운영 InGame SO를 읽어 메우지 않는다.

공유 HUD의 Description Panel/Text가 미연결이면 상세 문자열은 준비돼도 팝업은 보이지 않는다. 해당 프리팹 연결은 사용자 후속 보류이며 몸체 텍스트 프리팹과는 별개다.


## 13. 위치 교환·정찰 부대 Core 병합 후 View 상태

Core2bd95ad34는0403babef로 병합됐다. 유효 노출값은 EffectiveIncreasePercent를 그대로 읽으며 Recon10+20+HuntersMark30은50% 한 아이콘이다. View가 최대/합계를 다시 계산하지 않는다. UI 문자열에는 억제된 출처도 남기고 무기한의 null을0턴으로 표시하지 않는다.

새 외형 두 줄은 기존 Body Text 설정을 사용한다. 위치 교환/MovementChanged는 ActivationStart·Source·Count None, 정찰 부대/StatusApplied는 StateApplied·Targets·Count None이다. 임의 파티클·Shake·UnitActions는 추가하지 않았다. 아이콘 아트와 HUD 설명 패널은 기존 후속이다.

교환은 실제 주체/승객 Moved를 묶고 주체 도착에서 두 배치를 함께 그린다. 쌍 취소는 연속 Returned를 함께 그리며 Participants나 RemovedEffects로 게임 상태를 다시 적용하지 않는다. 효과·아이템·거리·허용 여부·환불은 Core 사실을 따른다. 사용 잠금은 기존 QueryRevision/SourceRevision 응답으로 갱신한다. 실제 Play는 사용자 후속이며 [실행 계획](../plans/2026-10-31-position-swap-recon-undo-view-integration-plan.md)을 따른다.

## 14. 이펙트의 Rider 높이 보정 (2026-11-02)

`TraitVisual_FinishingShot > Always Effects > Element > Effect > Rider Y Offset`에서 Rider 대상의 표식 높이를 따로 조절한다. 같은 입력을 쓰는 특성 상시 이펙트와 상태 유지 이펙트에도 이 칸이 있다. 발동 이펙트는 `Activations > Element > Cues > Effects > Element > Rider Y Offset`에서 조절하며, 공격·피격 등 같은 큐 입력을 쓰는 곳도 지원한다.

- `Position Offset`은 모든 대상에 적용하는 몸 기준 보정이다.
- `Rider Y Offset`은 **이펙트가 붙는 대상**이 Rider일 때 추가하는 월드 Y 거리다. 양수는 위, 음수는 아래이며 기본값 0이다. 발동 유닛의 병과는 기준이 아니다.
- 예를 들어 `Position Offset.y = -1.5`, `Rider Y Offset = 1`이면 기본 위치에서 일반 대상은 1.5만큼 내려가고, Rider 대상은 그 위치보다 1만큼 올라간다.

병과는 현재 보인 Core 사실의 `Stats.Category.Key`에서 읽는다. 유지 이펙트는 기존 동기화 시점에, 큐는 자리를 정할 때 적용하며 같은 이펙트 창구의 병과 조회를 공유한다. 추가 Tick이나 Core 변경은 없다.

| Cues의 Point | Rider 보정 기준 |
|---|---|
| ActorBody·ActorMuzzle·ActorEffectPoint | 해당 행위자 |
| SubjectBody | 해당 대상마다 개별 적용. Stay In Place를 켜도 적용 |
| EventCell·Contact | 유닛에 놓는 자리가 아니므로 적용하지 않음 |

칸에 놓는 하붑 구름도 Rider 보정을 적용하지 않는다. 기본 Position Offset과 Rotation Offset은 기존 자리 기준을 유지하고, Rider 보정만 월드 Y 방향으로 더한다.

정적 대조 완료, 컴파일·Unity 재생 미확인. Unity에서 Rider·일반 대상이 섞인 SubjectBody 큐, Rider 행위자의 몸·총구·이펙트 지점 큐, Stay In Place, Execution 동시 표시, 보정값 0의 기존 위치와 이펙트 회수를 확인한다.
