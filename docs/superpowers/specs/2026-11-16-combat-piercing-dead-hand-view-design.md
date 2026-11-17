# 관통·죽음의 손·기절 전투 View 대응 설계 (2026-11-16)

상태: **결정 완료(D0~D6, Q-PD1~Q-PD4), Task 계획 [piercing-dead-hand-view-plan](../plans/2026-11-16-combat-piercing-dead-hand-view-plan.md) 작성, 코드 착수 전.** 확인용 임시 설정(§7)만 사용자 지시("확인 용 임시 설정 등록 진행")로 에셋에 등록했다(커밋하지 않음).

근거:

- Core → View 인계: [관통·죽음의 손 인계](../handoffs/2026-11-14-piercing-dead-hand-core-view-handoff.md), [Core 설계](2026-11-14-piercing-and-dead-hand-design.md), [액티브 스킬 인계](../handoffs/2026-11-15-player-active-skills-core-view-handoff.md)(기절 상태만).
- Core 커밋 `a95ca5a22`·`04726fed7`(관통·죽음의 손), `1b76f69db`(기절 상태), 병합 `5d6fc2827`. 우리 마지막 판 `3ccb8719a` 뒤로 전투 View 파일은 바뀌지 않았다.
- 아래 사실은 2026-11-16 정적 대조다. 컴파일·Unity 재생은 하지 않았다.

## 1. 사용자 결정 (2026-11-16)

| 번호 | 결정 |
|---|---|
| D0 | 관통은 기존 `TraitVisual_PiercingFalloff`(키 30)를 갱신해서 쓴다. 죽음의 손은 밀림 미리보기(도착 칸)·화살표·공격 범위 표기를 맞춘다. |
| D1 | 관통 탄은 적 몸에 닿았을 때가 아니라 **Endpoint에서 사라진다.** 가는 길에 조우 대상과 닿으면 그 대상마다 **기존 접촉 이펙트**를 낸다. |
| D2 | 죽음의 손은 **기본 방향(공격 방향 기준 오른쪽)으로만** 쓸린다(좌·우 선택 입력 없음). 재생은 **밀리는 대상이 모두 함께** 움직인다(충격 볼트 넉백과 같은 방식). 공격 동작 싱크는 **열어 둔 방식**(선행 특성 연출 기준 공격 클립 시작, [당김 싱크 설계](2026-11-10-pull-attack-animation-sync-design.md))으로 맞춘다. |
| D3 | 기절(`StatusEffectKey.Stun`)을 새 상태로 등록한다. |
| D4 | 확인용 임시 설정을 등록한다(§7). |
| D5 | Q-PD2~Q-PD4는 권장대로: 관통 감쇠 글자·이펙트는 이번엔 넣지 않음, 휩쓸기 화살표는 넉백 색 재사용, 기절 거절 안내 "기절" 문구를 더함(2026-11-16 사용자 "나머지는 권장대로"). |
| D6 | 원거리 공격의 피격 해제를 "투사체 접촉 시"로 통일한다. 기준은 **대상마다 그 대상을 맞히는 투사체의 마지막 접촉**이다(2026-11-16 사용자 "마지막 접촉 기준으로 설계 고치고"). 기존 원거리는 보이는 동작이 같고 관통만 조우마다 나뉜다(§3.1). |

## 2. 확인한 사실

### 2.1 관통 (`PiercingFalloff`, 키 30)

- 가진 유닛은 스나이퍼(키 12) 하나다. 지정 방식 `TargetingMethod_SniperCross`(CardinalLine, Single), 사거리 4, 감쇠 25%, 최대 4명.
- Core 사실: 미리보기·결과의 `PiercingShot`(`PiercingShotDelivery`).
  - `OriginCell`·`AimCell`·`Step`, `TraveledCells`(출발 다음 칸부터 연속), `Encounters`(대상·순번·칸·남은 %), `Endpoint`, `StopReason`.
  - Endpoint 규칙(생성자 검사): 벽(`Wall`)이면 마지막 지나간 칸 한 칸 너머의 벽 칸, 예산 소진(`TargetBudget`)이면 마지막 조우 칸, 그 밖(`Structure`·`BoardEdge`)은 마지막 지나간 칸.
- 줄어든 타격마다 `DamageModified` 발동이 사건 목록에 들어온다(`CombatCommandEvents.DamageTraits`, 대상 = 맞은 유닛). 피드백 프로필 표는 모르는 쌍에 기본값을 돌려줘 멈추지 않는다(`CombatTraitFeedbackProfiles.Get`).
- **지금 View 재생**: 스나이퍼는 공유 `CombatAttack_Linear` 코드 모션이다(클립 없음, `UnitVisual_Sniper.animationPresentation` 비어 있음).
  - 탄은 주 대상까지만 날아가고(`CombatLinearStyle.CreateAnimation`·`FireShot`), 주 대상 몸 접촉 한 번에 출처 전체를 해제한다.
  - 조우 대상은 모두 히트 대상이라(`CombatHitFollowUps.cs:134`) 그 순간 한꺼번에 반응한다.
  - Gate에는 대상별 해제(`CombatHitGate.Release(source, target)`)가 이미 있다. 대상별 접촉 진행률 계산(`CombatAttackPlayback.ContactProgress`)은 대상·칸을 받는다.
- **지금 View 궤적**: 직선은 대상마다 선을 긋는데, 조우 대상은 역할이 `Effect`라 선이 없고 주 대상(조준 칸)에서 끝난다(`CombatAttackTrajectory.IsLineTarget`).
- **`TraitVisual_PiercingFalloff`**: 이름 "관통 볼트", 문구 "30% 감소 … 해당 특성 폐기"(옛 Ballista 문구), 발동 줄 없음. 토큰 `{대상당피해감소율}`·`{최대관통대상수}`는 `TraitDescriptionBindings`에 이미 있다.

### 2.2 죽음의 손 (`DeadHand`, 키 530)

- 가진 유닛은 SoulEater(키 27) 하나다. 지정 방식 `TargetingMethod_SoulEater`(ForwardRectangle 폭 3, Direction), 사거리 2, 벽 너머 전달.
- **발동 종류(`TraitActivationKind`)가 없다.** `CombatEffectsEvent`를 내지 않고, 공격 요약(`AttackResult.TraitActivations`)에도 없다.
- 사건 순서: `CombatSweepEvent`(50, 머리) → 대상마다 `Moved`(원인 `Forced` + `TraitKey.DeadHand`)와 진입 효과 → `CombatAttackEvent` → 피해.
- 근거 값 `AttackSweepSnapshot`: `Origin`·`ActorId`·`Facing`·`Side`·`AreaCells`(판 밖 포함)·`CapturedTargetIds`·대상 행 `Targets`·`Interrupted`. 대상 행은 `StartAnchor`·`IntendedEndpoint`·`EnteredCells`·`FinalAnchor`(nullable)·`Stop`(`Completed`·`Blocked`·`Immune`·`Interrupted`·`NotProcessed`).
  - 대상 행은 한 축으로만 움직인다(생성자 검사). 사건의 `Sweep`과 공격 결과의 `Sweep`은 같은 객체다(인계 문서).
- 미리보기 `AttackPreview.Sweep`. Core가 포착 대상 전부를 미리보기 대상 목록에 보조 행으로 넣는다(`AttackResultFactory.cs:203`).
- **지금 View**
  - 사건 50은 형식 표에 없어 건너뛴다(`CombatEventTypeTable.RouteOf`).
  - 이동은 원인이 Forced라 넉백과 같은 밀림 걸음이다(`CombatMovementKinds.Of`).
  - 피해 근원이 없는 이동이라 동시 묶음 밖이어서(`CombatConcurrentGroups`) 대상마다 차례로 걸을 것으로 본다(재생 미확인).
  - `Sweep` 미리보기는 아무도 읽지 않는다. 넉백 화살표(`CombatKnockbackPreview.Append`)는 넉백·끌어오기 두 출처만 읽는다.
  - 공격 동작 싱크 장치는 기준을 `(TraitKey, TraitActivationKind)` 지원 목록(`CombatPreAttackTraitKinds`)과 클립 SO의 `attackAnimationStartKind`(`TraitActivationKind`)로 고른다. 죽음의 손은 발동이 없어 **지금 장치로는 휩쓸기를 기준으로 고를 수 없다.** 지정 모드는 공격 클립이 있는 유닛만 쓸 수 있다.
- SoulEater는 외형(UnitVisual)·모델·손 그림이 저장소에 없고, 로드아웃·로스터에도 없었다. 죽음의 손 TraitVisual도 없었다(§7에서 임시 등록).

### 2.3 기절 (`StatusEffectKey.Stun = 20`)

- `1b76f69db`에서 새로 생긴 상태다. 그 전에는 "이동불가 + 공격불가"가 상태 하나가 아니었고, 공포 특성이 무장해제(`Disarm`)·속박(`Bind`) 둘을 같이 걸었다(`DeathTraitApplication.cs:141-142`). `ArtifactEffectType.Stun`은 아티팩트 효과 종류 값이라 유닛 상태가 아니다.
- 지금 거는 곳은 스킬 단죄 하나(1턴, `PowderBlossomCalculator.ApplyStun`)다. `ActionRestrictionStatus(Stun)` → 공개 스냅샷 `ActionRestrictionEffectSnapshot`.
- 이동·공격·스킬을 모두 막는다(`CombatActionAdmission.IsStunned`). 거절 이유 `AttackFailureReason.Stunned`·`MovementFailureReason.Stunned`(53).
- **View**: `CombatUnitStatusFactory`에 표시 변환이 없어 기절이 걸리면 `CombatEffectFormatters`가 "표시 변환이 등록되지 않았습니다: Stun" 예외를 낸다. 상태 외형 에셋도 없다. 거절 안내는 기본 문구 "사용불가"로 나온다(예외 아님).

## 3. 설계

### 3.1 원거리 접촉 해제 규칙 하나 + 관통 탄 진행 — PD1 (D1, D6)

"해제"는 Core에 무엇을 보내는 것이 아니다. Core가 이미 확정해 게시한 결과 사건(체력·피격·숫자·사망·넉백)을 공격 Gate가 붙잡았다가 화면에 내보내기 시작하는 순간이다. 해제 시점은 화면에서 누가 언제 맞아 보이는지만 바꾼다.

**규칙(D6)**: 원거리 공격의 해제는 **대상마다, 그 대상을 맞히는 투사체 접촉 가운데 마지막 접촉**이다.

- 한 공격의 탄들은 모두 같은 공격 모양(같은 대상)을 쓴다. 그래서 규칙은 이렇게 풀린다.
  - **앞 탄**(연발의 마지막이 아닌 탄): 지금처럼 탄별 숫자·겉보기 피격만 낸다(`CombatVolleyDamageNumbers`·`CombatVolleyContactHits`). 해제하지 않는다.
  - **마지막 탄**: 그 탄의 접촉마다 그 접촉의 대상만 해제한다(`CombatHitGate.Release(source, target)`).
  - **마지막 탄의 끝**(마지막 접촉, 접촉이 없으면 도착): 남은 대상을 모두 해제한다(`ReleaseAll`). 탄이 몸에 직접 닿지 않는 대상(스플래시·부채꼴의 다른 대상·특성 경로)이 여기서 풀린다.
  - Hit 기준 시점(Hit 큐·소리·후속)은 첫 해제다. Gate 완료 정책(첫 해제)은 그대로다.
  - 합류 전 조기 공격(선행 특성 싱크)은 지금처럼 해제 허용(`canRelease`)을 기다린다. 기다리는 동안 생긴 접촉 해제는 접촉 순서대로 모았다가 허용되면 낸다.
- **탄 하나의 접촉 목록**(대상·순서는 Core, 진행률은 View):
  - 관통이면 `PiercingShot.Encounters` 순서대로 조우마다 하나.
  - 관통이 아니면 지금 대상 하나(`CombatAttackShape.ContactTargetId` = 주 대상, 대신 맞기면 수호자). 없으면 접촉 없음(도착 = 끝).
  - 곡사는 착탄 한 번이 접촉이다(대상 지정 없이 끝 = 착탄).
  - 진행률은 탄 직선 경로(0 = 출발, 1 = 끝) 위에서 대상 몸 상자에 처음 닿는 지점이다(기존 `ContactProgress`, 닿지 않으면 Core가 준 칸을 경로에 투영).
- **소실점**: 관통이면 탄은 `PiercingShot.Endpoint`에서 사라진다. 소실점 함수 하나를 재생과 궤적(§3.2)이 같이 쓴다.
  - 벽에서 멈춘 탄은 벽 칸 몸에 처음 닿는 곳에서 사라진다(궤적 막힘 선과 같은 규칙 `CutProgress`, P1 확인 대상). 그 밖은 Endpoint 칸 가운데다.
  - 관통이 아니면 지금 목표점 그대로다.
- **접촉 이펙트**: 접촉마다 기존 접촉 이펙트(공격 표시의 접촉 큐, `PlayImpact` + `CombatProjectileContact.Along`)를 낸다. 앞 탄도 지금처럼 낸다.
- **값의 자리**: 공격 모양 값(`CombatAttackShape`)에 Core `PiercingShot`을 그대로 싣는다. View는 조우·감쇠·멈춤을 계산하지 않는다.
- **두 길이 한 규칙**: 코드 모션 직선(스나이퍼, `CombatLinearAttackAnimation`)과 클립 원거리(`CombatVolleyReleasePiece` + `CombatLinearStyle.FireShot`)가 같은 접촉 목록 함수와 같은 해제 규칙을 쓴다. 투사체 진행은 접촉 여러 번을 받는 기존 비공개 발사(`CombatProjectileFlights.FireThrough`, 옛 Ballista가 쓰던 것)를 다시 연다. 출처 수명 계약(`ICombatSourceLifetime`)에 대상 해제 입구 하나를 더한다(조기 출처 `CombatDeferredAttackSource`도 같이 넘김).

**기존 원거리의 보이는 동작은 같다.**

| 공격 | 해제 | 지금과 비교 |
|---|---|---|
| 단발 직선(스나이퍼·샷건 등) | 주 대상 접촉 = 마지막 탄의 유일한 접촉 → 그 대상 해제, 이어서 남은 대상 전부 | 같음 |
| 연발 클립(Akimbo 2발) | 2발째 접촉에서 해제. 1발째는 숫자·겉보기 피격 | 같음 |
| 곡사(대포·선인장 대포) | 착탄에서 전부 | 같음 |
| 관통 | 조우 대상마다 자기 접촉에서, 마지막 조우 뒤 남은 대상 전부 | 바뀜 |

- 관통이 아닌 공격은 접촉 목록이 하나라, 결과는 지금처럼 그 접촉에서 전부 해제다.
- **기차와 비교**(2026-11-16 사용자 질문): 원리는 같다. 기차(`CombatRailwayContactPlayback`·`RailwayContactSchedule`)도 경로 위에서 대상 몸에 처음 닿는 시각을 미리 계산해 시각 순으로 정렬하고, 진행하면서 그 시각이 지난 대상만 하나씩 시작한다(히트 타이밍 설계 H7). 몸 모양(프리팹 BoxCollider)·접촉 기하(`CombatContactGeometry`)도 같다.
  - 다른 점은 해제 원본이다. 기차는 공격 출처가 없어 자기 사망 사건을 직접 예약·소비한다(설계 H1). 관통은 공격이라 공격 출처 Gate의 대상별 해제를 쓰고, 맞은 대상의 피격·피해·사망·후속은 지금 공격 흐름 그대로다.
  - 시간 단위도 다르다. 기차는 여러 구간 경로의 초, 관통은 직선 한 구간의 진행률(0~1)이며 투사체 진행이 그 진행률을 넘길 때 알린다(지금 직선 탄의 접촉 하나와 같은 꼴을 여러 개로).

### 3.2 관통 궤적 미리보기 — PD2

- 응답에 `PiercingShot`이 있으면 대상별 선 대신 공격자 → 소실점(§3.1과 같은 함수) 한 줄을 긋는다. 탄이 지나는 길 그대로다.
- 막힌 응답(`ShowBlocked`)·관통 없는 공격은 지금과 같다.

### 3.3 관통 TraitVisual 갱신 — PD3 (에셋만)

- 이름 "관통", 문구는 토큰으로(초안, 사용자 수정 가능):
  `공격 방향 일직선의 적과 아군을 꿰뚫습니다.\n대상을 지날 때마다 피해가 {대상당피해감소율}%씩 줄고, 최대 {최대관통대상수}명까지 맞힙니다.`
- "해당 특성 폐기" 문장을 지운다. 감쇠 타격의 글자·이펙트(`DamageModified` 발동 줄)는 Q-PD2.

### 3.4 휩쓸기 동시 재생 — DH1 (D2)

- 동시 묶음 해석(`CombatConcurrentGroups`)에 규칙 하나를 더한다: 휩쓸기 머리 사건 뒤, 같은 행동 구간 안에서 그 공격의 `CombatAttackEvent` 앞까지 오는 포착 대상의 `Moved`(원인 `TraitKey.DeadHand`)를 한 묶음으로 둔다. 열쇠는 (공격 그룹, 공격자)다.
- 경로가 다른 사건으로 끊긴 대상은 지금 규칙 (a)대로 묶음 밖이다(진입 지형 피해·사망). 약한 지반 진입 기록 붙이기도 그대로다.
- 걸음 모양은 바꾸지 않는다. 밀림 걸음·젖힘이 넉백·충격 볼트와 같다.
- 머리 사건(50)은 지금처럼 화면 연출이 없다(건너뜀). 묶음 판정이 머리 사건을 어떻게 알아보는지(형식 표 줄로 둘지)는 P2.

### 3.5 공격 동작 싱크 — DH2 (D2, Q-PD1 결정 필요)

작업자 입력은 열어 둔 그대로 `CombatUnitAnimationSO`의 시작 설정(모드·기준·지연)이다. 새 SO·새 칸을 만들지 않는다.

다만 지금 설정만으로는 휩쓸기에 맞춰지지 않는다(2026-11-16 사용자 질문 "이미 CombatUnitAnimationSO에 해놨는데 안 되나"에 대한 대조).

- 기준 선택지는 `CombatPreAttackTraitKinds`의 `(TraitKey, TraitActivationKind)` 목록에서 나온다. 지금은 `PullApplied`·`AttackGranted` 둘뿐이다.
- 실행 쪽 소속 대응(`CombatPreAttackTraitMap.Build`)은 공격 요약 `AttackResult.TraitActivations`에 있는 지원 발동만 찾는다. 죽음의 손은 발동을 내지 않아 대응이 생기지 않는다.
- 그래서 SoulEater 클립 SO에서 지정 모드를 켜도 "일치하는 시작 없음"이 되어 기존 순차 경로(휩쓸기가 끝난 뒤 공격 클립)로 돈다. 예외는 없다.

필요한 것은 그 설정의 **선택지 하나와 실행 쪽 대응 하나**다. 두 길이 있다.

| 안 | 내용 | 장단 |
|---|---|---|
| **A (권장)** View만 | 같은 `CombatUnitAnimationSO` 시작 모드에 값 하나(`FromSweepPresentationStart`, 기준 칸은 안 씀, 지연은 씀)를 더한다. 소속은 `CombatSweepEvent.Sweep`과 `AttackResult.Sweep`의 같은 객체로 잇고, 시작 통지는 휩쓸기 묶음의 첫 이동 레인 시작(당김 레인 통지와 같은 꼴)이다. 지연 값은 그대로 쓴다. | Core 무변경. 휩쓸기는 발동이 아니라는 Core 설계와 맞는다. 대신 장치의 결합 값(지금 발동 하나)을 "발동 또는 휩쓸기"로 넓혀야 한다. |
| B Core 발동 추가 | Core가 새 발동 종류(예: `SweepApplied`)를 당김(`PullApplied`)처럼 공격 요약·사건에 내고, View는 지원 목록에 한 줄을 더한다. | View 장치는 거의 그대로다. Core(천명님 영역) 변경과 사건이 하나 늘고, 그 발동의 HUD·큐 처리도 새로 생긴다. |

- 어느 안이든 지정 모드는 공격 클립이 있어야 한다. SoulEater 전용 클립 묶음 SO가 필요하다(P3). 임시 설정은 Boomber 클립 묶음을 빌려 쓰므로 거기에 모드를 켜면 Boomber도 바뀐다.

### 3.6 휩쓸기 미리보기 화살표·도착 칸 — DH3

- `CombatKnockbackPreview.Append`에 셋째 출처 `preview.Sweep`을 더한다. 조준 표시 조건(`ShowAim`)에도 넣는다.
  - 방향: 대상 행의 `IntendedEndpoint - StartAnchor` 부호(한 축, Core 검사).
  - 도착 칸: 움직인 대상(`EnteredCells` 있음, `FinalAnchor` 있음)만 점유 칸을 그만큼 옮겨 도착 칸 층에 칠한다.
  - 표지: 완료·중단 = 밀림, 막힘 = 막힘, 면역 = 면역, 미처리 = 표지 없음.
  - 점유 칸은 같은 응답의 대상 목록에서 읽는다(Core 보조 행이 보장).
- 화살표 색은 Q-PD3.

### 3.7 공격 범위 표기 — DH4

- 새 코드 없음으로 본다. 방향 조준(샷건 부채꼴과 같은 흐름)이라 Core 응답의 범위·영향 칸을 그대로 그린다. `Sweep.AreaCells`는 판 밖을 포함하지만 겹침 표시는 판 안 영향 칸을 쓴다. Unity에서 확인한다.

### 3.8 방향 고정 — DH5 (D2)

- 미리보기·명령은 `sweepSide`를 넘기지 않는다. Core 기본값(오른쪽)이다. 입력 변경 없음. 나중에 좌·우 선택을 넣으면 공격 미리보기·명령 생성 인자 하나다.

### 3.9 죽음의 손 TraitVisual — DH6

- 새 에셋(키 530, "죽음의 손")은 §7에서 토큰 없는 문구로 먼저 등록했다.
- 코드 Phase에서 `TraitDescriptionBindings`에 한 줄(`{피해계수}` = `DeadHandTraitSpec.DamagePercent`)을 더하고 문구에 토큰을 넣는다. 훈련 카드 수치 글(`rankValueFormat`)은 에셋 값만이다(`TrainingTraitValueFormatter`가 값 = 피해 계수로 읽음).

### 3.10 기절 상태 등록 — ST1 (D3)

- `CombatUnitStatusFactory`에 `new CombatEffectFormatter<ActionRestrictionEffectSnapshot>(StatusEffectKey.Stun)` 한 줄(우리 줄).
- 새 상태 외형 `UnitStatusVisual_Stun`(effectKey 20, "기절", "이동·공격·스킬을 사용할 수 없습니다.")과 표 등록. 표시 변환 묶음이 생성 때 외형을 읽으므로 **코드와 에셋은 한 커밋**이다.
- 거절 안내 "기절" 문구는 Q-PD4.

## 4. 바뀔 곳 (예상)

| 묶음 | 파일 | 작성자 |
|---|---|---|
| PD1 | `CombatAttackShape`·새 접촉 목록 함수·`CombatLinearStyle`·`CombatLinearAttackAnimation`·`CombatProjectileFlights`·`CombatAttackPlayback`·`ICombatAttackStyleContext`·`ICombatSourceLifetime`·`CombatDeferredAttackSource`·`CombatVolleyReleasePiece`·코드 모션 해제 묶음 | 우리 |
| PD2 | `CombatAttackTrajectory` | 우리 |
| PD3 | `TraitVisual_PiercingFalloff.asset` | 에셋 |
| DH1 | `CombatConcurrentGroups`(+ P2에 따라 `CombatEventTypeTable`) | 우리 |
| DH2 | 안 A: `CombatAttackAnimationStartMode`·`…StartData`·`CombatPreAttackTraitMap`·`…Binding`·`CombatPreAttackPresentationStarts`·이동 레인 시작 통지, SoulEater 클립 묶음 SO | 우리 |
| DH3 | `CombatKnockbackPreview` | 우리 |
| DH6 | `TraitDescriptionBindings`, `TraitVisual_DeadHand.asset` | 확인 필요 |
| ST1 | `CombatUnitStatusFactory`, `UnitStatusVisual_Stun.asset`·표 | 우리 |

Factory·ServiceKeys·Core 변경은 없다(안 A 기준). 시험은 새로 쓰지 않는다.

## 5. 원칙 점검

- **SSOT**: 조우·감쇠·멈춤·휩쓸기 경로·도착 칸은 Core 값 그대로다. 소실점은 함수 하나를 재생과 궤적이 같이 쓴다. 휩쓸기 묶음 규칙은 묶음 해석 한 곳이다. 화살표 표지 바꾸기는 `KindOf` 옆 한 곳이다.
- **SOLID·독립성**: 관통은 직선 전략 안 갈래이고 다른 공격은 그대로다. 휩쓸기 화살표는 출처 하나를 더하는 꼴(끌어오기 선례)이다. 싱크 안 A는 장치의 결합 값을 넓히는 리팩토링이 따라온다.
- **FW 계약**: 새 서비스·Job·등록 없음. 입력·요청 경로 무변경.

## 6. 정할 것

| 번호 | 질문 | 권장 / 결정 |
|---|---|---|
| Q-PD1 | 공격 동작 싱크 방식(§3.5). 작업자 입력은 어느 안이든 `CombatUnitAnimationSO` 그대로 | **결정: A** View만(같은 SO의 시작 모드 값 추가, Core 무변경, 2026-11-16 사용자 "A안으로 진행") |
| Q-PD2 | 관통 감쇠 타격에 글자·이펙트(`DamageModified` 발동 줄)를 넣을지 | **결정: 이번엔 안 넣음**(D5) |
| Q-PD3 | 휩쓸기 화살표 색 | **결정: 넉백 색 재사용**(D5) |
| Q-PD4 | 기절 거절 안내 문구 "기절"을 더할지 | **결정: 더함**(D5, 안내 표의 다른 작업자 줄이면 공유) |

계획에서 정할 것(확인 대상): P1 벽 종료 소실점(벽 몸 접촉 / 칸 가운데), P2 휩쓸기 머리 사건의 형식 표 줄 여부, P3 SoulEater 전용 클립 묶음 SO와 임시 모델.

## 7. 확인용 임시 설정 (D4, 등록함, 커밋하지 않음)

| 에셋 | 바꾼 것 | 되돌리기 |
|---|---|---|
| `DataBaseSO/InGameGroup/Loadout/Item/PlayerLoadout_1.asset` | ~~메인 Akimbo → Sniper, 서브에 SoulEater ×1 추가, 첫 덱 인덱스에 4 추가, 첫 손패 4 → 5~~ → **HEAD로 되돌림(2026-11-16).** 사용자가 19:57에 인스펙터에서 메인을 Akimbo로 돌리고 SoulEater 줄을 뺐는데 첫 덱 인덱스 4가 남아 `[PlayerLoadoutData] Initial deck index 4 is outside the expanded grant range [0, 4)`로 DB 생성이 멈춤 → 파일을 HEAD와 같게 둠(메인 Akimbo·서브 셋·인덱스 0~3·손패 4). 대신 사용자가 `UnitRoster_1`에 SoulEater·Sniper를 더함(전투 정의에 들어갈 유닛 목록, 커밋 여부는 사용자) | 변경 없음 |
| `DataBaseSO/ViewGroup/UnitVisual/Item/Playerable/UnitVisual_SoulEater.asset`(+ `.meta`, 표 등록) | 키 27 임시 외형. Boomber의 프리팹·공격 표시(`CombatAttack_Hammer`)·반응·소리를 빌림. 클립 묶음은 PD-c에서 Boomber 묶음을 복사한 임시 `CombatUnitAnimation_SoulEater`(휩쓸기 시작 모드, 지연 0)로 바꿈 | 진짜 모델이 오면 프리팹·클립 묶음만 교체 |
| `DataBaseSO/ViewGroup/TraitVisual/Item/TraitVisual_DeadHand.asset`(+ `.meta`, 표 등록) | 키 530, "죽음의 손", 토큰 없는 문구, 아이콘 없음 | 실제 에셋으로 유지(토큰은 DH6) |

- 로드아웃이 HEAD로 돌아가 지금은 시작 메인이 Akimbo이고 SoulEater가 첫 덱에 없다. 관통·죽음의 손을 확인하려면 메인 Sniper·SoulEater 줄을 다시 넣어야 한다 — 인스펙터에서 `entries`에 줄을 더하면 **`initialDeckGrantIndices`에 그 색인(메인 0 + 줄 순서)도 함께** 더하고, 손패에 모두 넣으려면 `startingHandSize`도 늘린다(색인이 펼친 지급 수보다 크면 DB 생성이 멈춘다). 로비 메인 선택(천명님 `0d1f7c3c0`, View 미연결)이 붙으면 메인은 로드아웃을 고치지 않고 고를 수 있다.
- 확인 방법: Play From Boot(Database)로 전투에 들어가 스나이퍼로 일직선 적을 조준(관통), SoulEater를 배치해 방향 조준(죽음의 손). 위 설정을 다시 넣은 뒤.
- 로드아웃 변경은 커밋하지 않는다. 임시 외형·죽음의 손 외형의 커밋 여부는 사용자가 정한다.

## 8. Phase(안)

| Phase | 내용 | Unity 확인 |
|---|---|---|
| PD-a | 원거리 접촉 해제 규칙·관통 탄 진행(PD1)·궤적(PD2)·TraitVisual 문구(PD3) | 조준 칸 너머까지 가는 탄, 조우마다 접촉 이펙트·반응, 4명 소진·벽·구조물·판 끝 소실, 궤적 끝이 소실점, 아군 관통, 피해 숫자 25%씩 감소, 문구 토큰. **기존 원거리 그대로**: Akimbo 2연발(2발째 해제), 샷건 부채꼴, 대포·선인장 대포 착탄, 대신 맞기 수호자 |
| PD-b | 죽음의 손: 동시 묶음(DH1)·화살표·도착 칸(DH3)·문구 토큰(DH6) | 네 방향 범위 표기, 오른쪽 기준 화살표·도착 칸, 막힘·면역 표지, 판 가장자리, 대상들이 함께 밀린 뒤 공격·피해, 진입 지형 피해·사망이 낀 대상, 희생 |
| PD-c | 공격 동작 싱크(DH2, Q-PD1 뒤) | 휩쓸기 시작 + 지연에 공격 클립 시작, 피해는 휩쓸기 뒤, 지정 모드 아닌 유닛 그대로 |
| PD-d | 기절 등록(ST1) | 기절 HUD 아이콘·설명(스킬 시전이 생긴 뒤 확인 가능), 거절 안내 |
| PD-e | 문서·공유·묶음 끝 검토 | — |

컴파일·재생 확인은 사용자가 Unity에서 한다. 보고에는 "컴파일 미확인"을 적는다.
