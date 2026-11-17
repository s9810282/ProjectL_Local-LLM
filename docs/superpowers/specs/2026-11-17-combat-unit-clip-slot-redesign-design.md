# 전투 유닛 클립 칸 다시 짜기 — 이벤트 목록·이벤트 지점·탑승물·앞/뒤 단계·블렌드 (설계 초안)

- 날짜: 2026-11-17
- 상태: **설계 확정(11-17 사용자 "확정" — Q-A1~Q-A7·Q-B2·P1~P19), 코드·에셋 변경 없음.** Task 계획 [clip-slot-redesign-plan](../plans/2026-11-17-combat-unit-clip-slot-redesign-plan.md)(CS-a~CS-e, 계획에서 정한 TP-1~TP-14 확인 대상).
- 계기: 사용자 "기존 구조 기준"으로 여섯 방향(블렌드 On/Off, `shots` → 이벤트 목록, 큐 시점 Cue → Event, 지점 통합, 탑승물 칸, 앞/뒤 동작) → 대조 보고 → "4. attackPoint가 아니라 EventPoint로 / 5. Cowboy 올가미는 Cannon들과 같은 무기 / 나머지는 권장안대로 설계 진행".
- 대신함: [동작 구조 제안](2026-11-17-combat-unit-motion-structure-proposal.md)(동작 키 + 단계 목록, Q-MS1~Q-MS5)은 이 설계가 대신한다. 고정 칸(공격·피격)은 그대로 두고 칸 안의 모양을 바꾼다.
- 바탕: [유닛 애니메이션 클립 설계](2026-10-19-combat-unit-animation-clip-design.md)(AC1~AC19, Idle = 컨트롤러, 사건 클립 = 묶음 SO + 코드), 7기준 검토 2차 §13(V3·V8·V12가 이 설계와 겹침).

## 0. 사용자 결정(2026-11-17)

| 번호 | 질문 | 결정 |
|---|---|---|
| Q-A1 | 블렌드 값 자리 | 칸마다 전환(들어올 때·단계 사이·나갈 때)별로 켜고 끔 + 초 |
| Q-A2 | 페이드아웃 중 그 유닛에 다음 클립 | A 남은 페이드를 끊고 새 클립 시작(위층 하나) |
| Q-A3 | 근접에서 이벤트 줄의 뜻 | A 줄마다 맞는 순간 — 앞 줄은 연발과 같은 접촉 피격·숫자 나눔, 마지막 줄이 실제 해제, `hitDelay`는 줄로 |
| Q-A4 | 이벤트 지점 회전 | 큐 칸 하나("지점 회전 / 행위자 방향")로 |
| Q-A5 | 옮기는 방법 | A 에디터 옮기기 도구 한 번 |
| Q-A6 | 앞/뒤 단계 허용 | A 앞 단계는 공격만, 뒤 단계는 공격·피격 |
| Q-A7 | 뒤 단계 동안 그 유닛 다음 연출 | A 기다리지 않음(사건 완료 = 본 단계 끝, 뒤 단계는 채널이 혼자 돌리다 다음 클립이 오면 끊음) |
| 4번 | 지점 이름 | `attackPoint`가 아니라 **이벤트 지점(EventPoint)** |
| 5번 | 탑승물 | 탑승물 칸은 말만. **카우보이 올가미는 Cannon·Ballista·Catus처럼 무기 = 부속 클립** |

## 1. 확인한 사실(코드·에셋 대조, 2026-11-17)

- 묶음 SO [CombatUnitAnimationSO](../../../Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/CombatPresentation/CombatUnitAnimationSO.cs): 공격 칸(`attackClip`·`attackSpeed`·`attackCompanionClips`·`hitDelay`·`shots`·시작 모드 셋), 피격 칸(`hitClip`·`hitCompanionClips`). 묶음 에셋 14개.
- 발사 목록 `shots`(이벤트 번호 + 지점 번호): 비면 (Event, 지점 0) 한 발. 쓰는 에셋은 Akimbo 하나([Event/0, Event2/1]). 근접은 목록이 없고 `ClipEvent` + `hitDelay` 한 번(`CombatClipAttack.ComposeMelee`).
- 큐 시점 `CombatCueAnchor`: `Cue = 2`·`Cue2 = 6`·`Cue3 = 7`. Cue2·Cue3은 "그 이벤트가 있는 원거리 클립"만(`CombatPresentationCueRules.AllowsAnchor`). 쓰는 에셋은 `CombatAttack_Akimbo` 하나.
- 지점: `CombatAnimationEvents`의 `muzzle`·`effectPoint`(지점 0) + `additionalAttackPoints`(`CombatAttackPoint` = 총구·이펙트 쌍, 지점 1부터). 새 프리팹 6개(EnemyCannon·Akimbo·Ballistar·CactusCannon·ShotGun·Sniper)는 두 Transform이 모두 있고 서로 다르다. 옛 Colored CatusCannon 둘은 총구만. 추가 쌍은 Akimbo 하나. 큐 자리 `ActorEffectPoint = 1`(지점 회전)·`ActorMuzzle = 2`(행위자 방향) + `actorPoint`(쌍 번호, 1 이상은 Akimbo 한 줄).
- 부속 클립: Cannon·Ballista·Catus = 무기, Sabow·Bommber = 말, Cowboy = 말 + 올가미. 말은 부속과 섞여 있다.
- 채널 [UnitClipChannel](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Display/Unit/UnitClipChannel.cs): 섞개 입력 둘(0 = Idle 컨트롤러, 1 = 위층 레이어 섞개[주 + 부속]), 가중치 0/1로 바로 바꿈, 끝에 위층을 빼고 Idle이 안 움직이는 뼈를 준비 때 값으로 되돌림(계획 P17). Animator `applyRootMotion`·`fireEvents`·`cullingMode`를 바꾸고 되돌리지 않음(검토 V8, 남음). 채널에 프레임 Tick 없음(시각은 클립 조각이 `SetTime`).
- 시작 모드 1·2를 쓰는 묶음은 `Cowboy`(끌어오기 310)·`SoulEater`(휩쓸기). 연결은 `CombatPreAttackPresentationStarts`.
- 이 설계가 고칠 파일은 모두 용근 줄(`0b007124d` 2연발, `78afcdeea` 시작 모드 등). 다른 작업자 코드 줄 없음. 프리팹(민형님 새 모델 프리팹)의 컴포넌트 값은 바뀐다 → 공유.

## 2. 설계

### CS1 동작 칸 값 — 공격·피격이 같은 모양

칸 하나 = **동작**(`CombatClipMotion`, 가칭) = 단계 셋 + 블렌드:

```text
CombatClipMotion
├─ Pre   : CombatClipStage?   앞 단계(공격만, Q-A6)
├─ Main  : CombatClipStage    본 단계(필수) + 이벤트 목록(CS2)
├─ Post  : CombatClipStage?   뒤 단계(공격·피격)
└─ Blend : In · Between · Out  각각 (켜짐, 초)   (CS7)

CombatClipStage
├─ Body       : AnimationClip    몸(주 클립, ClipStart·ClipEnd 필수)
├─ Mount      : AnimationClip?   탑승물(말) (CS5)
├─ Companions : AnimationClip[]  무기(부속, 올가미 포함)
```

- 공격 칸 = 동작 + 공격에만 있는 값(공격 동작 시작 기준, CS6). 피격 칸 = 동작만.
- **재생 배율 `AnimSpeed`(11-17 사용자: `attackSpeed` 이름을 바꾸고, 단계마다는 필요 없고 공격 칸·피격 칸에 하나씩):** 동작 칸마다 하나(기본 1). 그 칸의 모든 단계(몸·탑승물·부속)와 이벤트 시각에 걸린다. 블렌드 초에는 걸지 않는다. 옮기기 도구는 옛 `attackSpeed`를 공격 칸에, 피격 칸은 1.
- 값 형식은 하나(SSOT): 단계 규칙·블렌드 규칙·검사가 공격·피격에 한 벌.
- SO 입력은 직렬화 클래스 `CombatClipMotionInput`·`CombatClipStageInput`, `GetData`가 불변 값을 매번 새로(P2). 공격 칸·피격 칸은 이름 있는 두 필드 그대로(계획 P2 유지).
- 칸이 비면(본 단계 몸 클립 없음) 지금처럼 코드 모션·코드 반응.

### CS2 이벤트 목록 — `shots`를 대신함

본 단계에 **등록 이벤트 목록**(필수, 하나 이상). 줄 = `CombatClipEventEntry`(이벤트 번호, 이벤트 지점 번호, 지연).

| 칸 | 줄의 뜻 | 지점 | 지연 |
|---|---|---|---|
| 원거리 공격 | 줄마다 한 발(지금 `shots`와 같음) | 발사 지점(필수, 있는 지점) | 0만 |
| 근접 공격 | 줄마다 맞는 순간(Q-A3 A) | 0만(쓰지 않음) | 이벤트 뒤 맞기까지의 초(옛 `hitDelay`) |
| 피격 | 타이밍 표시만(큐 시점) | 0만 | 0만 |

- 같은 (이벤트, 지점)은 두 번 못 씀(지금 규칙). 근접·피격은 같은 이벤트 두 번 금지. 등록한 이벤트는 그 클립에 이름표가 있어야 함.
- **근접 여러 줄:** 줄 i의 맞는 순간에 대상 모두에게 접촉 피드백(연발과 같은 `CombatVolleyReleasePiece`의 접촉 순번 i)을 내고, 마지막 줄에서 출처 해제. 앞 줄의 접촉 피격·숫자 나눔은 지금 원거리 연발 길(`CombatVolleyContactHits`·`CombatVolleyDamageNumbers`)을 그대로 쓴다. 줄 하나면 지금 근접과 같다.
- 기존 `hitDelay`·`shots` 칸은 옮긴 뒤 지운다(CS9).

### CS3 이름 — 큐 시점 Cue → Event

- `CombatCueAnchor.Cue → Event1`, `Cue2 → Event2`, `Cue3 → Event3`. 정수 값(2·6·7)은 그대로라 **에셋은 바뀌지 않는다.**
- `CombatClipEventId.Event → Event1`(값 1 그대로), `Event2`·`Event3` 그대로.
- **애니메이션 이름표 함수 `ClipEvent`·`ClipEvent2`·`ClipEvent3`는 바꾸지 않는다**(P1) — `.anim`의 `m_Events`가 함수 이름 문자열로 붙어 있어 바꾸면 모든 클립 이벤트가 끊긴다.
- 고를 수 있는 시점 규칙: Event1~3은 **그 칸의 본 단계 이벤트 목록에 등록된 번호만**(근접·원거리·피격 모두). 지금의 "원거리 클립에 이벤트가 있을 때만"을 대신한다. 코드 모션 공격의 Event1은 지금처럼 타격·발사 순간.

### CS4 이벤트 지점(EventPoint) — 총구·이펙트 지점 통합

- `CombatAnimationEvents`: `muzzle`·`effectPoint`·`additionalAttackPoints`를 지우고 **`eventPoints : Transform[]`** 하나. 값 `CombatEventPoints`(번호 → Transform, `CombatAttackPoint`·`CombatAttackPoints` 대신함).
- 쓰는 쪽 둘:
  - 이벤트 줄(CS2)의 지점 = 발사 지점.
  - 이펙트 큐의 자리 **`ActorEventPoint`**(값 1, 옛 `ActorEffectPoint`·`ActorMuzzle`을 합침) + 번호 칸 `eventPoint`(옛 `actorPoint`) + 회전 칸 `eventPointRotation`(`Point` = 지점 회전 / `Actor` = 행위자 자세 방향, Q-A4). 옛 `ActorMuzzle = 2`는 값을 없앤다(검사가 거절).
- 한 타이밍에 지점 둘: 이벤트 줄이 발사 지점, 같은 시점(`Event1` 등)의 이펙트 큐가 다른 지점을 각각 고른다.
- 대여 계약 `ICombatUnitClip`의 `TryGetMuzzle`·`TryGetEffectPoint` → `TryGetEventPoint(번호, out 위치, out 회전)` 하나(검토 V3 일부 해소).
- 검사(외형 표, `CombatUnitAnimationRules`): 이벤트 줄·이 유닛 공격 묶음 큐가 가리키는 번호는 있고 비지 않으며 그 프리팹 안 Transform이어야 함. 가리키지 않는 칸은 비어도 됨(P5).

### CS5 탑승물 칸

- 단계마다 `Mount`(말 클립) 하나. 엔진은 부속과 같은 레이어 섞개, 층 순서 = 몸 0 · 탑승물 1 · 부속 2.. (P6).
- 규칙: 부속과 같다(이벤트 없음, 길이 ≤ 몸, 몸·다른 클립과 곡선 속성 겹침 없음 — 에디터 검사).
- 옮김: Sabow·Bommber·Cowboy의 말 → 탑승물, Cowboy 올가미·Cannon·Ballista·Catus 무기 → 부속 그대로(5번 결정).
- **탑승물·부속의 Idle(11-17 정정):** 말·무기 Idle은 묶음 SO가 아니라 Idle 컨트롤러에 이미 있다. 민형님 도구 `Tool/Editor/CharacterAnimationSetup.cs`가 Base 층 = 몸 Idle, `ClipIdle_<클립>` 층(가중치 1) = 말·무기 Idle로 컨트롤러를 만든다(말 유닛: Cowboy `Horse_Idle`·`Lasso_Idle`, Sabow `Enemy_Horse_Idle`, Bommber `Horse_Player_Idle`). 채널은 이 컨트롤러 전체를 Idle 입력으로 쓰므로 말 Idle도 함께 돌고, 공격·피격 클립이 끝나면 말도 그 Idle로 돌아온다. 그래서 동작 칸에 Idle 칸을 두지 않는다(Idle = 컨트롤러, 설계 바탕 AC 규칙 그대로).

### CS6 앞/뒤 단계

- **단계 클립의 이름표:** 모든 단계의 몸 클립은 `ClipStart`·`ClipEnd`가 필수(재생 구간). `ClipEvent`~`ClipEvent3`는 본 단계 몸만(앞·뒤 단계·탑승물·부속은 금지).
- **앞 단계(공격만):** 본 단계 앞에 재생. 공격의 사전 동작이라 특성과 상관없이 그 공격마다 늘 돈다(카우보이 PullBack, 11-17 사용자 — 끌어오기는 카우보이 고유 특성이고 PullBack은 공격의 사전 동작).
  - 공격 동작(앞 단계 → 본 단계)을 언제 시작하나는 CS11 공통 타이밍이 정한다.
  - 큐 시점은 본 단계 클립의 이름표다(P7, 11-17 사용자): `Start` = 본 단계 `ClipStart`, Event1~3 = 본 단계 `ClipEvent`~`ClipEvent3`, `End` = 본 단계 `ClipEnd`. 앞·뒤 단계의 이름표에 큐를 거는 시점은 이번엔 없음. 코드 모션 공격(클립 없음)은 지금처럼 `Start` = 실행 시작, `End` = 모션 끝.
  - 지금 데이터(앞 단계 없음)에서는 `Start`가 실행 시작과 같은 프레임이라 화면 변화 없음. 수호자 나가기 앞 항목·시작 모드 1·2가 있는 공격도 클립 시작 = 지금 `Start` 자리라 같다.
- **뒤 단계(공격·피격):** 본 단계가 끝나면 사건은 완료되고(Q-A7 A) 클립 대여도 돌아간다. 뒤 단계는 **채널이 혼자** 재생한다(꼬리, tail). 다음 클립 대여가 오면 꼬리를 끊고 새 클립을 시작(Q-A2와 같은 규칙). 취소·풀 반환·사망 래그돌은 꼬리를 바로 끊고 되돌린다(P8).
- 첫 대상: 카우보이 `CowBoy_PullBack`(+ `Horse_PullBack` 탑승물, `Lasso_PullBack` 부속)을 공격 앞 단계로, Gravedigger `Recovery`는 SoulEater = Gravedigger 전환 때 공격 뒤 단계로(§8).

### CS7 블렌드 — 전환마다 켜고 끔

- 동작 칸마다 세 전환: **In**(Idle → 첫 단계), **Between**(단계 → 다음 단계), **Out**(마지막 단계 → Idle). 각각 `CombatClipBlend`(켜짐, 초). 켜짐이면 초 > 0(유한), 꺼짐이면 지금처럼 바로 바꿈(Q-A1).
- 채널 모양: 섞개 입력 셋(0 = Idle, 1·2 = 위층 둘). In은 Idle → 위층 1, Between은 위층 1 → 위층 2(같은 대여 안), Out은 위층 → Idle. 위층 하나는 레이어 섞개(몸·탑승물·부속)라 지금 모양 그대로 둘이 된다.
- Out: 말·무기는 컨트롤러에 Idle 층이 있어(CS5 정정) 섞는 동안 그 Idle 쪽으로 간다. Idle 층이 움직이지 않는 뼈만 바인딩 기본값 쪽으로 가고, 그 값이 지금 되돌리기 값과 같아서 되돌리기(P17)는 페이드가 끝난 뒤 한 번.
- 페이드아웃·뒤 단계 꼬리 동안 다음 대여가 오면 끊음(Q-A2 A) — 그 자리에서 지금처럼 위층을 빼고 되돌린 뒤 새 클립의 In을 시작한다(P9).
- 시간: 대여 동안은 클립 조각이 지금처럼 시각을 넣고, In·Between 가중치도 같은 시계로 넣는다. 대여가 끝난 뒤(Out·꼬리)는 `FieldUnitView.Update`가 꼬리가 있을 때만 채널을 진행시킨다(게임 시간, 그래프 `GameTime`과 같음, P10). `Pause`·`Resume`(표시 꺼짐·켜짐)은 꼬리도 멈춘다.
- 사건 완료 시점은 바뀌지 않는다(본 단계 끝). In이 켜져 있어도 클립 시각·이벤트 시각은 그대로(섞이는 동안에도 클립은 0초부터 흐른다).
- 같이 고침: 검토 V8 — 채널이 바꾼 Animator 값 다섯을 모두 기억했다 돌려줌.

### CS8 검사 규칙 한 곳

`CombatUnitAnimationRules`에 단계 규칙 하나(`RequireStage`): 몸 클립 사건 규칙(길이·반복 끔·루트 곡선), 탑승물·부속 규칙, 이벤트는 본 단계 몸만(앞·뒤 단계와 탑승물·부속에는 전투 이름표 금지). 블렌드 규칙은 값 생성자 하나(`CombatClipBlend.FindError`)를 SO·값이 같이 부름(검토 V9 꼴 반복 금지).

### CS10 맞음(Hit)·접촉(Contact)과 칸별 큐 시점 (11-17 사용자 "Hit와 Contact 명확한 구분, 공격 시와 히트 시 상황이 다름")

지금 뜻(코드 대조): `Hit` = 그 공격의 **첫 대상 해제 직후 한 번**(`CombatContactRelease`, 해제 = Gate가 붙잡은 피해·피격 사건을 내보내기 시작), `Contact` = **원거리 투사체가 닿을 때마다**(탄마다, 관통이면 대상마다, 이펙트만·지연 0). 근접에는 `Contact`가 없다. 피격 쪽 큐는 그 대상의 피격 시작(= 그 대상 해제)이 `Start`다.

**맞음은 접촉의 한 경우다(11-17 사용자 "사실상 Hit는 Contact 하위").** 접촉 하나하나는 "앞 접촉(연출만)" 아니면 "실제 피격 접촉(Gate 해제)"이고, 지금 `Hit`은 그 공격의 **첫 실제 피격 접촉** 한 번이다. 그래서 시점을 `Contact` 하나로 합치고, 어느 접촉에 낼지는 큐 칸으로 고른다(결정 대기 Q-B2).

| 쪽 | 시점 | 뜻 |
|---|---|---|
| 공격 칸(공격자) | `Start`·Event1~3·`End` | 공격 본 단계 클립 이름표(CS6) |
| 공격 칸 | **`Contact` + 고르기** | 공격 수단이 대상에 닿는 순간. 원거리 = 투사체 접촉(탄마다·관통 대상마다), **근접 = 이벤트 줄의 맞는 순간(줄마다·대상마다, 새로)** |
| 피격 칸(맞은 쪽) | `Start`·Event1~3·`End` | 그 대상의 피격 동작(피격 본 단계 클립 이름표) |

`Contact` 고르기 칸 `contactFilter`(Contact 시점일 때만 봄):

| 값 | 몇 번 | 옛 시점 | 자리·종류 |
|---|---|---|---|
| `Every` | 모든 접촉(앞 접촉 포함) | 옛 `Contact` | 접촉점만, 이펙트만, 지연 0(P18) |
| `FirstHit` | 공격당 한 번, 첫 실제 피격 접촉 | 옛 `Hit` | 옛 `Hit`과 같음(행위자 자리, 이펙트·흔들림·소리·플래시, 지연 가능) |

- `Hit = 3`은 값을 없앤다(검사가 거절). 옮기기 도구가 `anchor: 3` → `Contact` + `FirstHit`(지금 쓰는 에셋은 `CombatAttack_Hammer` 하나). 플레이어 스킬 칸의 "맞음만" 규칙도 `Contact` + `FirstHit`로.
- 두 값이 쓰는 경로는 지금과 같다: `Every`는 접촉 콜백이 바로 내고, `FirstHit`은 조립 항목(해제 신호 뒤)이라 지연을 받는다.
- 근접 `Contact`의 자리 = 대상 몸 중심 + 맞은 방향(피격 큐의 `SubjectBody`와 같은 식), 원거리는 지금처럼 투사체 접촉점·진행 방향(P17).
- 종류별 접촉(코드 대조, 지금과 같음): 직선 = 경로 위에서 대상 몸에 닿는 진행률마다(관통이면 대상마다). **곡사(Cannon 등) = 투사체가 도착하는 순간 한 번**(경로 끝 = 주 칸 중심 착탄점, `CombatProjectileFlights.FireArc`가 진행률 1에 접촉을 둠), 대상은 그 순간 모두 해제. 그래서 곡사의 `Every`·`FirstHit`은 탄마다 같은 순간이다.
- 필요해지면 고르기 값만 더한다(예: 실제 피격 접촉마다). 이번엔 둘.
- **피격 칸(맞은 쪽)의 앞 접촉 피격(11-17 사용자):** 연출(플래시·흔들림·이펙트·소리·피격 클립)은 이미 처리돼 있고, 남은 것은 체력 바(접촉마다 나눠 줄이기)로 HUD 쪽 문제 때문에 미뤄 둔 것. 이 설계는 건드리지 않는다(옛 Q-B1 없앰). 체력 바는 추후 따로 진행(11-17 사용자). 미룬 기록 문서는 찾지 못함 — 연발 설계(11-02 §6)·정수 피해 숫자(11-10)에는 "체력 사실은 마지막 접촉에 한 번"만 적혀 있다.

### CS11 공격 동작 시작 — 특성별이 아니라 공통 타이밍 (11-17 사용자 "특성마다 일일이 처리하는 구조가 아니라 공통 타이밍 기준")

**지금:** 시작 모드 0 기본 / 1 지정 Kind(`PullApplied` 등) 발동 시작 + 지연 / 2 휩쓸기 첫 걸음 + 지연. 대응기 `CombatPreAttackPresentationStarts`가 특성 셋(`CombatPreAttackTraitKinds` 허용 목록: 끌어오기·선봉·청소)과 휩쓸기를 각각 다른 입구(발동 텍스트 조각·당김 레인·휩쓸기 걸음)로 잇는다. 새 선행 특성마다 목록 한 줄 + 입구 하나 + 묶음 칸 선택지가 는다.

**확인한 사실:** Core 공격 결과(`AttackResult.TraitActivations`)가 그 공격에 속한 발동을 모두 갖고, 결과 사건 목록 순서에서 발동 사건이 공격 사건 **앞이면 선행, 뒤면 후속**이다(`CombatPreAttackTraitMap.Build`가 이미 `occurrence.Event >= attackIndex`로 거른다). 끌어오기 이동 사건·휩쓸기 머리/걸음도 그 사이(선행 발동 ~ 공격 사건)에 온다. 즉 특성 종류를 몰라도 "이 공격의 선행 구간"을 사건 위치만으로 정할 수 있다.

**설계:**

- 공격 하나의 공통 타이밍 셋:

```text
[행동 시작] ─ 선행 구간: 이 공격의 첫 선행 사건 ~ 공격 사건 직전 ─ [공격 사건] ─ 후속(추가 공격·넉백 등)
               └ 선행 시작 = 그 구간 사건 중 처음 연출이 시작되는 순간
                 선행 끝   = 공격 사건이 시작할 수 있게 되는 순간(지금 기본)
```

- 선행 구간 = 공격 결과의 발동 중 공격 사건보다 앞에 있는 것(+ 공격 결과의 휩쓸기 머리) 가운데 가장 앞 사건부터 공격 사건 직전까지. 종류를 보지 않는다(허용 목록 `CombatPreAttackTraitKinds` 삭제).
- 공격 칸 값 **`motionStart`**: `AfterLead`(선행 끝, 기본 = 지금 모드 0) / `WithLead`(선행 시작 + 지연 = 지금 모드 1·2를 합침) + `delay`. 선행 구간이 없는 공격은 둘 다 지금처럼 바로 시작.
- 시작시키는 것은 공격 동작 전체(앞 단계가 있으면 앞 단계부터). 카우보이: `WithLead` → 끌어오기가 일어난 공격은 끌려오는 동안 PullBack → 공격, 안 일어난 공격은 보통처럼 PullBack → 공격. SoulEater: `WithLead` → 휩쓸기 첫 연출과 함께.
- 대응기는 "선행 구간 사건의 연출이 처음 시작됨" 하나만 받는다. 사건 Presenter가 사건 연출을 시작할 때(처리기 시작 한 곳) 사건 위치를 알리고, 대응기가 그 위치가 어느 공격의 선행 구간인지 본다 → 발동 텍스트·당김 레인·휩쓸기 걸음 입구 셋을 하나로.
- 후속 구간은 애니메이션 시작 기준으로 쓰지 않는다(뒤 단계는 본 단계 끝에 이어 붙고 기다리지 않음, Q-A7). 자리만 열어 둠.
- 옮김: 모드 0 → `AfterLead`, 모드 1·2 → `WithLead`(지연 그대로), 기준 Kind 칸 지움.
- 위험·확인: 선행 구간의 첫 사건이 화면에 아무것도 안 그리는 사건(예: 휩쓸기 머리)이면 "처음 연출 시작"이 지금(첫 걸음)보다 이를 수 있다 → 그런 사건은 처리기가 연출 없이 바로 끝내는지 계획 때 사건 종류별로 대조하고, 바로 끝나는 사건은 세지 않는다(P19).

### CS9 옮기기 — 에디터 도구 한 번(Q-A5 A)

- 메뉴 `ProjectL > Combat > 클립 칸 옮기기 (한 번)`(가칭, `Tool/Editor/`). `SerializedObject`로 옛 칸을 읽어 새 칸에 쓰고, 다시 돌려도 결과가 같다(이미 옮긴 에셋은 건너뜀).
- 옮기는 것:
  1. 프리팹(`CombatAnimationEvents`가 붙은 것 모두): `eventPoints` = [옛 muzzle, 옛 effectPoint, 추가 0의 muzzle, 추가 0의 effectPoint, …] — **고정 규칙 "쌍 k의 총구 = 2k, 이펙트 = 2k+1"**(P5). 공격 묶음 큐는 여러 유닛이 같이 쓰므로 번호 규칙이 유닛마다 같아야 해서, 같은 Transform이어도 합치지 않는다.
  2. 큐 항목(모든 큐를 가진 SO — 공격 묶음·반응 묶음·특성 외형·설정 표 등): 자리 2(총구) → 1 + 회전 `Actor`, 자리 1(이펙트) → 1 + 회전 `Point`, 번호 = 2k(총구)·2k+1(이펙트).
  3. 클립 묶음 14개: 몸·부속 → 본 단계, 말(이름이 `Horse`로 시작하는 부속, 도구가 목록을 보여 주고 확인) → 탑승물, `shots` → 원거리 이벤트 목록(비었으면 [Event1/0]), `hitDelay` → 근접 이벤트 목록 [Event1/0/지연], 피격 → [Event1], 블렌드 모두 꺼짐.
- 순서: 새 칸을 더하고 옛 칸은 읽기용으로 남긴 판(CS-a·CS-b) → 도구 실행·확인 → 옛 칸 지움(그다음 Phase 첫 Task, P11). 옛 칸이 남은 동안 DB는 새 칸만 읽고, 새 칸이 비었으면 부트 검사가 "옮기기 도구를 돌리세요"로 멈춘다.
- 옮긴 뒤 화면은 지금과 같아야 한다(블렌드 꺼짐, 근접 한 줄, 단계 없음).

## 3. 바뀌는 곳(예상)

| 층 | 파일 |
|---|---|
| DB 값 | (`attackSpeed` → 칸 `AnimSpeed`, 시작 모드 → `motionStart`, `CombatAttackAnimationStartMode`·`CombatPreAttackTraitKinds` 지움, 큐 시점 `Hit` 없앰·`contactFilter`) `CombatUnitAnimationSO`·`CombatUnitAnimationData`·`CombatClipAttackData`·`CombatClipData`, 새 `CombatClipMotion`·`CombatClipStage`·`CombatClipBlend`·`CombatClipEventEntry`(+ 입력), `CombatClipShot` 지움, `CombatClipTimings`(번호 이름), `CombatClipEventIds`, `CombatCueAnchor`·`CombatCuePoint`·`CombatCueEntries`, `CombatPresentationCueRules`, `CombatUnitAnimationRules`, `CombatAnimationEvents`, `CombatAttackPoint` → `CombatEventPoints`, `CombatAttackAnimationStartData`(기준 문구), 에디터 `CombatUnitAnimationSOEditor` |
| View 표시 | `UnitClipChannel`(위층 둘·블렌드·꼬리·V8), `ICombatUnitClip`·`FieldUnitView`(대여·`Update` 꼬리·지점 입구), `CombatUnitPresentationWindow` |
| View 연출 | `CombatClipPiece`·`CombatClipPlayback`(단계·블렌드 시각), `CombatClipAttack`·`CombatClipRangedAttack`(이벤트 목록, 근접 여러 줄), `CombatCueAnchors`·`CombatCuePoints`·`CombatEffectPiece`·`CombatFirePiece`·`CombatProjectileCueComposer`·`CombatCodeMotionAttack`(이름), `CombatAttackStart`·`CombatPreAttackPresentationStarts`(동작 시작 기준), `CombatCharacterEventHandler`(피격 단계) |
| 도구 | 새 옮기기 도구 `Tool/Editor/` |
| 에셋 | 묶음 14, 큐 가진 SO(자리 1·2를 쓰는 것), `CombatAnimationEvents` 프리팹(새 6 + 옛 Colored 2 + 지점 없는 것은 빈 목록) |

Factory·ServiceKeys·Core·Buffer 무변경. 새 시험 없음, 기존 시험은 컴파일이 깨지는 곳만 최소로(View 규칙).

## 4. Phase(안)

| Phase | 내용 | 화면 |
|---|---|---|
| CS-a | CS3 이름 + CS4 이벤트 지점(값·대여 입구·큐 자리·검사) + 옮기기 도구 1(프리팹·큐). 옛 지점 칸은 읽기용으로 남김 | 같음 |
| CS-b | CS1 동작 칸 값(본 단계만) + CS2 이벤트 목록(근접 여러 줄 포함) + CS5 탑승물 + CS8 + 옮기기 도구 2(묶음). 첫 Task에 CS-a 옛 칸 지움 | 같음(근접 한 줄) |
| CS-c | CS6 앞/뒤 단계(조각·채널 꼬리) + CS10 `Hit` → `Contact`·`FirstHit` 합치기·근접 `Contact` + CS11 공통 타이밍(대응기 입구 하나), 카우보이 PullBack 연결. 첫 Task에 CS-b 옛 칸 지움 | 카우보이 앞 동작 |
| CS-d | CS7 블렌드 + V8 | 켠 칸만 섞임 |
| CS-e | 문서(이펙트·소리 창구 안내·폴더 안내·민형님 클립 작업 안내), 공유(민형님 프리팹 지점·클립 작업, 호진님 클립 메모), 묶음 끝 검토 한 번 | — |

Unity 확인은 Phase마다 사용자(부트 DB 검사·재생). 옮기기 도구는 사용자가 Unity에서 돌린다.

## 5. 7기준 예상(알려진 문제 기준)

| 기준 | 지금 | 뒤 | 근거 |
|---|---|---|---|
| SSOT | 8 | 9 | 동작 모양·단계 규칙·블렌드 규칙 한 벌, 지점 원본 하나, 발사/근접 타이밍이 같은 목록 |
| SOLID | 7.5 | 8 | 새 순간(앞·뒤)이 데이터로, 대여 계약 자리 읽기 하나. 채널 책임은 늘어남(블렌드·꼬리) |
| FW 계약 | 8.5 | 8.5 | 무변경 |
| 독립성 | 8 | 8.5 | 새 지점·새 이벤트가 에셋만. 단, 이번 옮기기는 에셋 넓게 바뀜 |
| 캡슐화 | 7.5 | 8 | 지점 값 하나, V8 |
| C#·Unity | 8 | 8.5 | V8, `Update`는 꼬리 있을 때만 |
| If·Tick | 7.5 | 7.5 | 꼬리 Tick이 새로 생기나 꼬리 있을 때만 |
| **종합** | 7.9(10-20 기준) | **약 8.3** | |

## 6. 위험·Unity 확인

- 옮기기 도구가 놓친 에셋 → 부트 DB 검사가 멈춤(의도). 도구가 바꾼 에셋 목록을 콘솔에 남긴다.
- 프리팹 변형: 새 모델 프리팹 중 원본 프리팹의 컴포넌트를 덮어쓴 것이 있으면 `eventPoints`가 변형에 따로 써질 수 있음 → 도구가 원본·변형을 구분해 적고 결과를 보고.
- 근접 여러 줄: Core 피해 사건은 대상마다 하나 — 앞 줄 피격은 연출뿐(연발과 같은 한계).
- 블렌드 In이 켜진 공격: 맞는 순간 몸이 아직 Idle과 섞여 있을 수 있음(초를 짧게).
- 뒤 단계 꼬리와 다음 사건: 그 유닛의 이동 걸음은 클립을 안 써서 꼬리가 걸음 중 계속 돎 → 걸음 시작이 꼬리를 끊을지(P14).
- 확인 목록(계획에서 상세화): 지점 옮긴 뒤 Akimbo 2연발·Sniper·Cannon 총구·이펙트 자리, 말 셋, 근접 한 줄 = 지금, 카우보이 PullBack + 끌어오기 맞춤, 블렌드 켬/끔, 꼬리 끊김.

## 7. 설계에서 정한 것(사용자 확인 대상)

| 번호 | 정한 것 |
|---|---|
| P1 | 애니메이션 이름표 함수(`ClipEvent`·`ClipEvent2`·`ClipEvent3`·`ClipStart`·`ClipEnd`)는 이름을 바꾸지 않음 |
| P2 | 이름 바꾸기는 정수 값 유지(`CombatCueAnchor` 2·6·7, `CombatClipEventId` 1~3) → 에셋 무변경 |
| P3 | 피격 칸도 이벤트 목록(타이밍 표시만, 지연 0). **(11-17 사용자 정정)** 지점은 제약 없음(쓰지 않음) |
| P4 | 원거리 줄의 지연은 0만. **(11-17 사용자 정정)** 근접 줄의 지점은 제약 없음(쓰지 않음) |
| P5 | 지점 옮기기 규칙 "쌍 k의 총구 = 2k, 이펙트 = 2k+1", 같은 Transform도 합치지 않음, 가리키지 않는 칸은 비어도 됨 |
| P6 | 층 순서 몸 0 · 탑승물 1 · 부속 2.. |
| P7 | **(11-17 사용자 정정)** 큐 시점 = 본 단계 클립 이름표: `Start` = `ClipStart`, Event1~3 = `ClipEvent`~`3`, `End` = `ClipEnd`. 모든 단계 몸 클립은 `ClipStart`·`ClipEnd` 필수, `ClipEvent`류는 본 단계만. 앞·뒤 단계 이름표의 큐 시점은 이번엔 없음 |
| P8 | 취소·풀 반환·사망 래그돌은 뒤 단계·페이드아웃을 바로 끊음 |
| P9 | 끊을 때는 지금처럼 위층을 빼고 되돌린 뒤 새 클립 In(섞어서 넘어가지 않음, Q-A2 A) |
| P10 | 대여 뒤 진행은 `FieldUnitView.Update`가 꼬리 있을 때만(게임 시간) |
| P11 | 옛 칸은 한 Phase 동안 읽기용으로 남기고 다음 Phase 첫 Task에 지움 |
| P12 | **(11-17 사용자 정정)** `attackSpeed` → `AnimSpeed`, 공격 칸·피격 칸에 하나씩(단계마다 아님), 블렌드 초에는 걸지 않음 |
| P13 | **(11-17 사용자 정정 → CS11)** 시작 모드를 특성별에서 공통 타이밍(선행 구간 `AfterLead`/`WithLead` + 지연)으로. 시작시키는 것은 공격 동작 전체(앞 단계부터) |
| P17 | 근접 `Contact` 자리 = 대상 몸 중심 + 맞은 방향 |
| P18 | `Contact`의 `Every`는 이펙트만·지연 0(지금 규칙), `FirstHit`은 옛 `Hit` 규칙 그대로 |
| P19 | 선행 구간에서 연출 없이 바로 끝나는 사건은 "선행 시작"으로 세지 않음 |
| P14 | 그 유닛의 이동 걸음 시작은 꼬리를 끊지 않음(걸음은 위치만, 클립 무관) — 어색하면 CS-c 확인 때 다시 |
| P15 | 탑승물 판별(옮기기 도구): 부속 이름이 `Horse`로 시작하면 탑승물 후보, 도구 창에서 사용자 확인 |
| P16 | 블렌드는 동작 칸(공격·피격)마다 세 값, 단계마다 따로 두지 않음 |

## 8. 범위 밖

- 스킬 전용 클립(`Akimbo_SpecialAttack`) — 스킬 칸이 따로 필요(다음 설계). `Akimbo_Landing`은 쓰일 순간 미정.
- 사건 클립끼리 섞기(공격 중 피격) — 위층은 대여당 하나.
- SoulEater = Gravedigger 모델 전환(새 프리팹 부품·`Recovery` 뒤 단계 연결)은 CS-c 뒤 에셋 작업으로.
- 탑승물 전용 순간(이동 등) — 칸만 열어 둠. 탑승물 Idle은 이미 컨트롤러 층으로 돈다(CS5 정정, 11-17).

## 9. 구현 메모(CS-c, 2026-11-17)

- 앞·뒤 단계 몸 클립은 `ClipStart`·`ClipEnd`만 읽고(`CombatClipEvents.ReadStageTimings`), `ClipEvent`류가 있으면 부트가 거절한다(P7 그대로, 11-17 사용자 결정 A). 민형님 도구가 새 클립마다 자리값 `ClipEvent`를 찍으므로, 앞·뒤 단계로 쓸 클립은 그 줄을 지운다(`CowBoy_PullBack.anim`은 지움).
- 뒤 단계는 본 단계가 끝나는 순간 채널 꼬리로 넘어간다(CS6). 끊는 곳은 다음 클립·표시 대여 정리·래그돌·연출 취소·해제(P8). 블렌드(CS7)는 CS-d.
- CS11 선행 시작은 사건 Presenter 한 곳(`PresentEvent`·`BeginStep`)에서 "선행 구간 사건이고 그 시작 안에서 끝나지 않음"으로 센다(P19). 곁들임만 내는 특성 발동은 세지 않아, 옛 `AttackGranted` 기준(지금 쓰는 묶음 없음)은 공격 사건 시작과 같아진다. 기록은 계획 §5.2.

## 10. 구현 메모(CS-d, 2026-11-17)

- Out(CS7)은 뒤 단계가 끝날 때, 뒤 단계가 없으면 본 단계가 끝날 때 섞기 시작한다(CS-c 꼬리 규칙과 맞춤, 11-17 사용자). Out이 꺼져 있으면 지금처럼 뒤 단계 끝 또는 대여 반환 때 바로 돌아간다. 그래서 CS7 "시간"의 "대여가 끝난 뒤(Out·꼬리)"는 "본 단계 끝에 채널로 넘긴 뒤"로 읽는다.
- 섞기 시간은 대여 동안 클립 조각(`CombatClipPiece`)의 Tick, 넘긴 뒤 채널 `TickTail`이 넣는다. 본 단계가 블렌드 초보다 짧아 섞기가 남으면 그 섞기도 넘긴다.
- 컨트롤러(Idle)가 없는 유닛은 In·Out을 바로 바꾼다. 섞기가 겹치면 가장 오래된 위층을 빼고 그 가중치를 지금 위층에 얹는다(아주 짧은 단계에서만). 기록은 계획 §5.3.
- CS11 보충(11-17 사용자 결정 A): WithLead 지연은 음수도 된다. 음수면 선행 구간에서 처음 닿은 사건에서 공격 동작을 시작하고 그 사건 연출을 그 초만큼 붙잡는다(기존 사건 앞 간격). AfterLead에는 앞당김이 없다.
- 클립 이벤트 시각(11-17 사용자): 중간 이벤트끼리 순서를 보지 않는다(Event2가 Event1보다 앞서도 됨). 재생은 시각 순. 기록은 계획 §5.3 "같이 한 것 2".
- 이벤트 줄 목록(11-17 사용자): 비우면 기본 한 줄(Event1 · 지점 0 · 지연 0). 큐의 Event1~3 시점은 목록 등록이 아니라 본 단계 몸 클립 표식으로 정함(CS3의 "등록한 번호만"을 대신함). 피격 칸 목록은 맞기 전 예열 동작 자리로 남김. 이어서(11-17 사용자 "[Event1, 0, 0] 다 지우고 Header에") `[Event1, 0, 0]`은 적지 않아도 늘 맨 앞에 들어감(Event1 줄이 없으면 붙임, CS2의 "Event1 줄 필수"는 값에서만 지킴) — 공격 칸 = 공격 애니메이션 임팩트 타이밍, 피격 칸 = 피격 타이밍(묶음 SO 머리말), 에셋의 기본 줄은 모두 지움.

## 11. 구현 메모(CS-e, 2026-11-17)

- Ud 문제 없음(사용자). 같이 한 것(큐 목록 여백·음수 시작 지연·이벤트 순서 자유·근접/피격 지점 제약 없음·빈 Events 기본 줄·`[Event1, 0, 0]` 지움 + 머리말)도 사용자 확인 문제 없음.
- 이벤트 줄 목록: Event1 줄이 없으면 `[Event1, 0, 0]`을 맨 앞에 붙인다(`CombatClipMotion.WithPrimary`) — CS2의 "Event1 줄 필수"는 값에서만 지키고 입력에서는 적지 않아도 된다. 공격 소리는 이 설계 범위 밖으로 옮겼다([유닛 소리를 유닛마다](2026-11-17-combat-unit-sound-per-unit-design.md), 공유 묶음 큐의 소리 시점 칸 없음).
- 문서: 이펙트·소리 창구 안내(시점 Event1~3·Contact+고르기·ActorEventPoint·유닛 소리 줄로 다시 씀)·사운드 작업자 안내 시점 낱말·폴더 배치 안내(갱신 줄·Ordering 줄·§2 소리 규칙)·옛 클립 설계 머리말 "대신함(일부)".
- 공유(미전달): 민형님 [새 메모](../handoffs/2026-11-17-combat-clip-slot-minhyeong-share.md)(프리팹 Event Points 번호 규칙·클립 이름표 규칙·탑승물 칸), 호진님 클립 메모 §12(묶음 칸 모양·외형 소리 줄).
