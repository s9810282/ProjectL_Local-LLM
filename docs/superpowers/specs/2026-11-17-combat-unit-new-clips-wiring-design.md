# 전투 유닛 새 애니메이션 클립 연결 — 설계·Phase 계획 (초안)

- 날짜: 2026-11-17
- 상태: 결정 완료(2026-11-17 사용자, §3). Task 계획 [new-clips-wiring-plan](../plans/2026-11-17-combat-unit-new-clips-wiring-plan.md) CW-0·CW-a·CW-b·CW-d·CW-e·CW-f 완료(에셋만, 코드 변경 없음, 구현 메모 §5), CW-c와 Unity 확인 대기.
- 계기: develop 병합으로 민형님 `4f2f496`(캐릭터 FBX 교체, 11-15)·`fbb9780d4`(유닛마다 Idle·Attack·Hit·추가 클립, 폴더 정리, 11-16)가 들어옴 → 사용자 "어떤 클립 있는지 정리" → "연결 계획 쓰기".
- 경로 줄임: `A/` = `Assets/ProjectL/Prefabs/Characters/New/<진영>/<유닛>/Animation/`, `B/` = `Assets/ProjectL/DataBaseSO/ViewGroup/CombatPresentation/AnimationClip/`.
- 대조 방법: 스크립트로 에셋 YAML·FBX를 읽음(Unity 실행 없음). 원시 결과는 세션 스크래치(`clip_report.md`·`wire_facts.md`).

## 1. 확인 사실

### 1.1 클립 현황
- `Prefabs/Characters/New` 아래 `.anim` 115개, 그중 77개는 아무 에셋도 참조하지 않음.
- 세 종류:
  1. 옛 세트 `BaseHumanBone_*`(11-15 전, 사람형 공통 뼈 이름).
  2. 새 세트 `<유닛>_Attack`·`_Hit`·`_Idle`(11-16). 컨트롤러 Idle만 연결됨.
  3. 옛 세트 복사본(`Playerable_*`·`Enemy_*` 앞붙이, `.001`·`.002`·`_Duplicate` 뒷붙이), 대부분 참조 없음.
- 내용이 완전히 같은 파일 10묶음, 동작은 같고 이벤트만 다른 파일 5묶음. 프로젝트 규칙상 지우지 않고 보고만 한다(민형님 정리 대상, §5 공유).

### 1.2 새 세트의 모양
- **몸 클립 + 부속 클립**: 새 세트는 Idle 컨트롤러 층(`Base Layer` + `ClipIdle_<부속>`)과 같은 나눔이다. 몸 클립(`CowBoy_Attack` 등)과 부속 클립(말 `Horse_Attack`, 올가미 `Lasso_Attack`, 무기 `Slingshot_Attack`·`Ballista_Attack`·`Cannon_Attack`·`Enemy_Horse_Attack`)이 같은 뼈를 만지더라도 **속성이 겹치지 않는다**(몸 = 위치·크기, 부속 = 회전 등, 속성 단위 대조 0). 그래서 우리 부속 규칙(`CombatUnitAnimationRules.RequireCompanionClips` — 같은 곡선 금지)을 그대로 통과한다.
- **이벤트**: 새 Attack·Hit·추가 클립에는 모두 `ClipStart`(0)·`ClipEvent`·`ClipEnd`(끝)가 있고 Loop Time·루트 곡선이 없다. 다만 **`ClipEvent`가 모두 정확히 길이의 절반**이다(예: 1.4초 → 0.7, 3.167초 → 1.583). 실제 타격 프레임이 아니라 자리만 찍은 값으로 보인다(Q-CW1).
- **부속 클립에는 이벤트가 없다**(규칙대로).

### 1.3 지금 클립 묶음과 새 세트 대응

| 클립 묶음(`B/`) | 쓰는 UnitVisual | 지금 공격(길이) | 새 공격 몸 + 부속(길이) | 새 피격 몸 + 부속 |
|---|---|---|---|---|
| `Enemy/Cannon` | Cannon(25)·특성 셋 | 옛 `BaseHumanBone_Cannon_Attack` + `Cannon_Attack`(1.40) | `Enemy_Cannon_Attack` + `Cannon_Attack`(1.40) | `Enemy_Cannon_Hit` + `Cannon_Hit` |
| `Enemy/FixSo` | Demon(13)·특성 둘 | 옛 `BaseHumanBone_Pixso_Attack`(2.33, 배율 2) | `Enemy_Miner_Attack`(2.00) | `Enemy_Miner_Hit` |
| `Enemy/Heavy` | Heavy(26)·Entrenchment(88) | 옛 `EnemyHeavy_Shild_Attack.001`(1.67) | `EnemyHeavy_Attack`(2.13) | `EnemyHeavy_Hit` |
| `Enemy/Sabow` | Gravekeeper(17)·lqwe(83)·Disarm(87) | **새** `Enemy_Horse_Shovel_Attack` + `Enemy_Horse_Attack`(2.40) | (이미 새 것) | `Enemy_Horse_Shovel_Hit` + `Enemy_Horse_Hit` |
| `Enemy/Stage1Boss` | StageBoss1(18) | 옛 `BaseHumanBone_Spical_Attack`(2.00) | `Stage1Boss_Attack`(3.08) | `Stage1Boss_Hit` |
| `Enemy/SoulEater` | (쓰는 UnitVisual 없음) | 옛 `BaseHumanBone_Spical_Attack` | — | — |
| `Playerable/Akimbo` | Akimbo(10)·MainCharacter(14) | 옛 `BaseHumanBone_TwoHandGun_Attack`(1.50, **ClipEvent·ClipEvent2 두 발**) | `Akimbo_Attack`(1.21, **ClipEvent 하나뿐**) | `Akimbo_Hit` |
| `Playerable/Ballista` | Ballista(15) | 옛 `BaseHumanBone_Cannon_Attack` + `BarristarBone_Ballistar_Attack`(1.40) | `Ballista_Sniper_Attack` + `Ballista_Attack`(2.40) | `Ballista_Sniper_Hit` + `Ballista_Hit` |
| `Playerable/Bommber` | Boomber(24) | **새** `Horse_Player_Boomber_Attack` + `Horse_Player_Attack`(1.93) | (이미 새 것) | `Horse_Player_Boomber_Hit` + `Horse_Player_Hit` |
| `Playerable/Catus` | CactusCannon(16) | 옛 `BaseHumanBone_Cannon_Attack` + `Cannon_Attack.002`(1.40) | `CactusCannon_Attack` + `Slingshot_Attack`(2.67) | `CactusCannon_Hit` + `Slingshot_Hit` |
| `Playerable/Cowboy` | Cowboy(19) | 옛 `BaseHumanBone_lassoAttack.001` + `lasso.001_Attack.002`(1.25) — **새 모델과 뼈 경로 0/217, 지금 공격 동작 안 나옴** | `CowBoy_Attack` + `Horse_Attack`·`Lasso_Attack`(3.17) | `CowBoy_Hit` + `Horse_Hit`·`Lasso_Hit` |
| `Playerable/Shotgun` | ShotGun(11) | 옛 `BaseHumanBone_ShotGun_Attack`(1.00) | `ShotGun_Attack`(2.43) | `ShotGun_Hit` |
| `Playerable/SoulEater` | SoulEater(27, ShotGun 프리팹 빌림) | 옛 `BaseHumanBone_Horse_Shovel_Attack`(1.33, Boomber 폴더) | (전용 모델은 Gravedigger? Q-CW6) | — |

- 옛 공격 클립은 카우보이만 빼고 새 모델과 뼈 경로가 100% 맞는다(새 Idle 클립과 전체 경로 대조) — 지금도 움직인다.
- 모든 UnitVisual에 피격 반응 묶음(`CombatUnitReaction_Default`)이 있어 피격 클립을 넣을 조건(`ValidateUnit`)은 충족한다. 지금 어떤 묶음에도 피격 클립이 없다.

### 1.4 피격 클립이 재생에서 하는 일(코드)
- `CombatCharacterEventHandler.ComposeHit`: 피격 클립이 있으면 클립 대여 + 클립 조각을 `waits: true`로 넣는다 → **그 유닛의 피격 사건은 클립 끝까지 완료되지 않는다**(지금 코드 반응 조각도 함께 돈다). 새 Hit 클립 길이는 1.0~2.0초.
- 연발 앞 탄 접촉 피격(`TryBeginContactHit`)·넉백과 함께 가는 피격(`BeginPushHit`)도 같은 피격 클립을 쓴다. 접촉 피격은 피격 클립이 있으면 다른 연출 중일 때 내지 않는 조건이 있다(`CanBeginContactHit(id, hitClip != null)`).
- 피격 배율 칸은 없다(공격은 `attackSpeed`가 있음).

### 1.5 넣을 자리가 없는 새 클립
- `Akimbo_SpecialAttack`(화약꽃 스킬로 보임): 스킬 재생은 시전자의 일반 공격 클립을 빌린다(`CombatAttackStart` → `TryGetAttackClip`). 스킬 전용 클립 칸이 없다.
- `CowBoy_PullBack`(+ 말·올가미): 카우보이 끌어오기 특성 동작으로 보인다. 지금 카우보이 묶음은 공격 클립 시작을 끌어오기 발동(310)에 맞추는 모드(`attackAnimationStartMode` 1)다.
- `Gravedigger_Recovery`, `Akimbo_Landing`: 쓰임 미정.

### 1.6 새 모델
- 새 `Playerable/Sniper/Sniper.prefab`·`Playerable/Gravedigger/Gravedigger.prefab`: 컨트롤러 Idle은 있으나 `CombatAnimationEvents` 컴포넌트가 없고 어떤 UnitVisual도 쓰지 않는다.
- `UnitVisual_Sniper`(12, 마탄 메인)는 옛 `Colored/Playerable/Sniper.prefab`, 클립 묶음 없음(코드 모션). 마탄은 직선 원거리라 새 모델에 묶음을 붙이면 **총구 지점이 필수**다.
- `UnitVisual_UndeadHeavy_Entrenchment`(88)는 옛 `Colored/Enemy/HeavyEnemy.prefab`에 새 Heavy 컨트롤러·묶음을 붙여 뼈 경로가 맞지 않는다(Idle·공격 모두 안 움직임, 11-11부터, 재령님 줄).

## 2. 설계

- **CW1 공격 묶음을 새 세트로**: §1.3 표의 "새 공격 몸 + 부속"을 묶음 `attackClip`·`attackCompanionClips`에 넣는다. 코드 변경 없음(에셋 값만). 카우보이는 지금 깨져 있어 이것이 고침이다. Sabow·Bommber는 이미 새 것.
- **CW2 피격 묶음**: §1.3 표의 "새 피격 몸 + 부속"을 `hitClip`·`hitCompanionClips`에 넣는다. 지금 코드로 그대로 돈다(Q-CW3·Q-CW4가 재생 동작을 정함).
- **CW3 Akimbo 두 발**: 발사 목록(`shots`: ClipEvent → 총구 0, ClipEvent2 → 총구 1)은 그대로 두고, 새 `Akimbo_Attack`에 `ClipEvent2`를 찍는다(Q-CW5). 찍지 않고 바꾸면 발사 목록이 없는 이벤트를 가리켜 **부트 DB 생성이 예외로 멈춘다**(`CombatClipAttackData.ReadShots` "발사 1의 이벤트 Event2가 없습니다").
- **CW4 이벤트 시점**: 새 클립의 `ClipEvent`(절반 자리)를 실제 타격 프레임으로 바꾸는 주인을 정한다(Q-CW1). 그 전까지는 지금 값으로 연결해도 동작은 한다(맞는 순간이 클립 절반).
- **CW5 템포**: 새 공격 클립이 대부분 길다(카우보이 1.25 → 3.17초, 샷건 1.00 → 2.43, 보스 2.00 → 3.08). 공격 배율 `attackSpeed`로 맞출지 정한다(Q-CW2). 피격은 배율 칸이 없다.
- **CW6 새 모델**: Sniper·Gravedigger 프리팹에 `CombatAnimationEvents`(Animator와 같은 GameObject, Sniper는 총구 지점)를 붙이고 UnitVisual을 새 프리팹 + 새 묶음으로(Q-CW6). Entrenchment(88)는 새 EnemyHeavy 프리팹으로(Q-CW8).
- **CW7 특수 클립 칸(나중)**: 스킬 전용 클립(`Akimbo_SpecialAttack`)·특성 동작 클립(`CowBoy_PullBack`)·`Recovery`·`Landing`은 칸 설계가 필요하다 — 스킬은 묶음에 스킬 클립 칸 + `CombatAttackStart`가 그 칸을 고르는 입구, 특성 동작은 발동 줄의 유닛 행동·선행 특성 싱크와의 관계를 정해야 한다(Q-CW7).

## 3. 정할 것 — 결정 완료(2026-11-17 사용자)

| 번호 | 결정 |
|---|---|
| Q-CW1 | **B: 이벤트 시점은 우리가 바꾼다**(Unity에서 보고 `.anim` 이벤트 시각을 고침, 민형님 공유) |
| Q-CW2 | A: 그대로 연결하고 보며 유닛마다 `attackSpeed` 조정 |
| Q-CW3 | A: 일단 지금 코드대로(피격 클립 끝까지 그 유닛 대기) |
| Q-CW4 | A: 일단 지금 코드대로(피격 클립과 코드 반응 함께) |
| Q-CW5 | **`Akimbo_Attack`의 3/4 위치에 `ClipEvent2`를 임의로 추가**(1.208초 × 0.75 = 약 0.906초) |
| Q-CW6 | A: 이번에 Sniper만 |
| Q-CW7 | A: 특수 클립 칸은 빼고 진행 |
| Q-CW8 | A: Entrenchment(88)를 새 EnemyHeavy 프리팹으로 교체 |
| 추가 | **아예 안 쓰는 클립은 제거**(사용자). CLAUDE.md의 "참조 없는 에셋은 삭제하지 않는다" 규칙에 대한 이번 작업 한정 사용자 지시 — 범위는 계획 CW-e |

아래는 결정 전 질문 표(기록).


| 번호 | 질문 | 권장 |
|---|---|---|
| Q-CW1 | 새 클립 `ClipEvent`(절반 자리)를 누가 실제 타격 프레임으로 맞추나 | **A: 민형님께 요청(애니메이션 원본 작업자), 그때까지 지금 값으로 연결** / B: 우리가 Unity에서 보고 `.anim` 이벤트 시각을 고침(민형님 공유) / C: 그대로 |
| Q-CW2 | 길어진 공격 템포 | **A: 그대로 연결하고 Unity에서 보며 유닛마다 `attackSpeed` 조정**(에셋 값만) / B: 옛 길이에 맞춘 배율로 한 번에(카우보이 약 2.5배 등 — 동작이 어색할 수 있음) |
| Q-CW3 | 피격 클립 동안 그 유닛의 다음 사건 대기 | **A: 지금 코드대로 클립 끝까지**(그 유닛만 막음, 다른 유닛은 안 막음) / B: 맞는 순간(ClipEvent)까지만 기다리고 나머지는 흘림(코드 변경) |
| Q-CW4 | 피격 클립과 코드 반응(젖힘·밀림)을 함께 | **A: 함께(지금 코드)** / B: 클립이 있으면 코드 반응 끔(코드 변경) |
| Q-CW5 | Akimbo 두 번째 발사 이벤트 | **A: 우리가 `Akimbo_Attack.anim`에 `ClipEvent2` 한 줄**(시각은 옛 클립 비율 1.267/1.5 → 약 1.02초, 민형님 공유) / B: 민형님께 요청하고 그때까지 Akimbo만 옛 클립 |
| Q-CW6 | 새 모델 Sniper·Gravedigger 연결을 이번에 | **A: 이번에 Sniper만**(마탄 메인, 프리팹에 컴포넌트·총구 — 사용자 에디터 작업) / B: 둘 다(Gravedigger = SoulEater인지 확인 필요) / C: 다음에 |
| Q-CW7 | 특수 클립 칸(스킬·끌어오기 동작 등) | **A: 이번 범위에서 빼고 따로 설계** / B: 스킬 클립 칸만 이번에 |
| Q-CW8 | Entrenchment(88) 옛 프리팹 | **A: 새 EnemyHeavy 프리팹으로 같이 고침**(재령님께 알림) / B: 보고만 |

## 4. Phase(안)

| Phase | 내용 | 바꾸는 것 | Unity 확인 |
|---|---|---|---|
| CW-0 | 착수 전 확인: 병합 뒤 부트·전투 진입이 지금 그대로인지, 클립 참조 다시 셈 | 없음 | 부트 DB 생성, 카우보이 공격이 지금 안 움직이는지 |
| CW-a | 공격 묶음 새 세트로(CW1) + Akimbo `ClipEvent2`(Q-CW5 A면) | `B/` 묶음 에셋 9개(Cannon·FixSo·Heavy·Stage1Boss·Akimbo·Ballista·Catus·Cowboy·Shotgun), `Akimbo_Attack.anim` 이벤트 한 줄 | 유닛마다 공격 동작·부속(말·무기·올가미) 함께 움직임, 발사·맞는 순간, Akimbo 두 발 |
| CW-b | 피격 묶음(CW2) | `B/` 묶음 에셋 11개 `hitClip`·`hitCompanionClips` | 피격 동작, 그 유닛 다음 사건 대기, 연발 접촉 피격·넉백 피격, 사망과 겹침 |
| CW-c | 템포 조정(Q-CW2 A) | 묶음 `attackSpeed` 값 | 한 턴 체감 |
| CW-d | 새 모델(Q-CW6)·Entrenchment(Q-CW8) | Sniper 프리팹 컴포넌트·총구(사용자), `UnitVisual_Sniper`·새 묶음 `Playerable/Sniper`, `UnitVisual_UndeadHeavy_Entrenchment` 프리팹 | 마탄 메인 Idle·공격·스킬 발사 위치 |
| CW-e | 참조 없는 클립 제거(사용자 지시, 계획 TP-7) | `Prefabs/Characters/New` 아래 연결 뒤에도 참조 없는 옛 클립·복사본 `.anim` + `.meta` | 부트 DB 생성, 모든 유닛 Idle·공격·피격 |
| CW-f | 문서·공유·묶음 끝 검토 | 민형님 공유(이벤트 시점·Akimbo 이벤트·지운 클립 목록·프리팹 컴포넌트), 재령님(88) | — |

- 코드 변경은 Q-CW3 B·Q-CW4 B·Q-CW7 B를 고를 때만 생긴다(권장안이면 에셋 값만).
- 작업 영역: 클립 묶음 SO와 UnitVisual 값은 전투 View DB(우리 영역 연결), `.anim` 이벤트와 프리팹 컴포넌트는 민형님 에셋이라 바꾸면 공유한다.
- 커밋은 사용자. 묶음 에셋과 `.anim` 이벤트(Akimbo)는 한 커밋(이벤트 없이 묶음만 들어가면 Akimbo 발사 목록이 없는 이벤트를 가리켜 부트가 멈춤).

## 5. 구현 메모(2026-11-17, 기록은 계획 §5)

- **연결:** CW1·CW2는 §1.3 표 그대로(묶음 11개 + 새 Sniper 묶음). 코드 변경 없음. Akimbo `ClipEvent2`는 0.90625초(Q-CW5).
- **설계에 없던 것(사용자 요청·결정으로 더함):**
  - 감싼 다섯 프리팹(캐논 셋·라이더 둘)의 몸 상자를 루트로 옮김 — 몸 모양은 루트 `BoxCollider`만 읽어서(10-29부터 점 몸 + 경고).
  - 캐논 셋 루트 태그 `EditorOnly` → `Untagged`.
  - CactusCannon 새총 렌더러 6번째 뼈 — 풀린 프리팹이 FBX 교체 뒤 뼈 목록을 따라가지 않아 새총이 안 그려졌음.
  - CW6 Sniper 프리팹 부품을 사용자 에디터 작업 대신 Claude가 YAML로(계획 TP-8 바뀜). FBX 안 오브젝트 번호는 Unity 가져오기 결과(Library 산출물)에서 읽음.
- **확인한 것:** 클립 경로는 Animator가 붙은 오브젝트 기준이라 빈 루트로 감싼 프리팹에서도 새 클립 경로가 100% 맞음. 새 공격·피격 첫 자세 = Idle 첫 자세(보스 왼팔 17.4°만 다름).
- **확인 중:** 캐논 셋은 안쪽 모델 Animator가 꺼져 있어(11-11) Unity 클립 미리보기에 그 프리팹을 넣으면 안 움직임. 전투는 클립 채널이 Animator를 켜서 영향 없음.
- **남은 것:** CW-c(템포·이벤트 시각 — 사용자가 보고 값), 마지막 Unity 확인.

## 부록 A. 제거 후보(2026-11-17 계산, CW-e에서 다시 셈)

범위: `Assets/ProjectL/Prefabs/Characters/New` 아래 `.anim`. CW-a·CW-b·CW-d 연결(§1.3 새 공격·피격 몸 + 부속, Sniper `Sniper_Attack`·`Sniper_Hit`) 뒤에도 에셋(`.asset`·`.controller`·`.prefab`·`.unity` 등) 참조가 하나도 없는 클립. 경로는 `Prefabs/Characters/New/` 기준, 괄호는 크기, "was"는 지금 그 클립을 쓰지만 연결로 바뀌는 묶음.

- **남김(정한 것, 확인 대상):** 새 세트(11-16) 중 지금 안 쓰는 8개 — `Akimbo_Landing`·`Akimbo_SpecialAttack`·`CowBoy_PullBack`·`Horse_PullBack`·`Lasso_PullBack`·`Gravedigger_Attack`·`Gravedigger_Hit`·`Gravedigger_Recovery`(특수 클립 칸·Gravedigger 연결에서 쓸 것). 쓰는 UnitVisual이 없는 `Enemy/CombatUnitAnimation_SoulEater` 묶음이 참조하는 `Stage1Boss/…/BaseHumanBone_Spical_Attack`, 아군 SoulEater가 쓰는 `Boomber/…/BaseHumanBone_Horse_Shovel_Attack`도 참조가 있어 남김.

```
DELETE 47
  Enemy/EnemyCannon/Animation/BaseHumanBone_Cannon_Attack.anim  (8171 KB)  was: CombatUnitAnimation_Cannon.asset
  Enemy/EnemyCannon/Animation/BaseHumanBone_Melee_Idle.anim  (3963 KB)
  Enemy/EnemyHeavy/Animation/BaseHumanBone_Shild_Attack.001.anim  (6889 KB)
  Enemy/EnemyHeavy/Animation/BaseHumanBone_TwoHandGun_Idle.anim  (3561 KB)
  Enemy/EnemyHeavy/Animation/EnemyHeavy_Shild_Attack.001.anim  (6880 KB)  was: CombatUnitAnimation_Heavy.asset
  Enemy/EnemyHeavy/Animation/EnemyHeavy_TwoHandGun_Idle.anim  (927 KB)
  Enemy/EnemyHeavy/Animation/Playerable_BaseHumanBone_Shild_Attack.001.anim  (6336 KB)
  Enemy/EnemyHeavy/Animation/Playerable_BaseHumanBone_Shild_Attack.002.anim  (6336 KB)
  Enemy/EnemyMiner/Animation/BaseHumanBone_Melee_Idle.anim  (3963 KB)
  Enemy/EnemyMiner/Animation/BaseHumanBone_Pixso_Attack.anim  (7759 KB)  was: CombatUnitAnimation_FixSo.asset
  Enemy/EnemyMiner/Animation/Playerable_BaseHumanBone_Pixso_Attack.001.anim  (7133 KB)
  Enemy/EnemyMiner/Animation/Playerable_BaseHumanBone_Pixso_Attack.anim  (7133 KB)
  Enemy/EnemyShabow/Animation/BaseHumanBone_Horse_Idle.anim  (3725 KB)
  Enemy/EnemyShabow/Animation/BaseHumanBone_Horse_Shovel_Attack.anim  (7733 KB)
  Enemy/Stage1Boss/Animation/BaseHumanBone_Melee_Idle.anim  (3963 KB)
  Enemy/Stage1Boss/Animation/Playerable_BaseHumanBone_Spical_Attack.001.anim  (8319 KB)
  Enemy/Stage1Boss/Animation/Playerable_BaseHumanBone_Spical_Attack.anim  (8319 KB)
  Playerable/Akimbo/Animation/BaseHumanBone_TwoHandGun_Attack.001.anim  (7623 KB)
  Playerable/Akimbo/Animation/BaseHumanBone_TwoHandGun_Attack.anim  (7623 KB)  was: CombatUnitAnimation_Akimbo.asset
  Playerable/Akimbo/Animation/BaseHumanBone_TwoHandGun_Idle.001.anim  (3399 KB)
  Playerable/Akimbo/Animation/BaseHumanBone_TwoHandGun_Idle.anim  (3399 KB)
  Playerable/Akimbo/Animation/Enemy_BaseHumanBone_TwoHandGun_Attack.anim  (8375 KB)
  Playerable/Ballistar/Animation/BarristarBone_Ballistar_Attack.anim  (31 KB)  was: CombatUnitAnimation_Ballista.asset
  Playerable/Ballistar/Animation/BaseHumanBone_Cannon_Attack.anim  (7475 KB)  was: CombatUnitAnimation_Ballista.asset
  Playerable/Ballistar/Animation/BaseHumanBone_Melee_Idle.anim  (3742 KB)
  Playerable/Boomber/Animation/BaseHumanBone_Horse_Idle.001.anim  (3552 KB)
  Playerable/Boomber/Animation/BaseHumanBone_Horse_Idle.anim  (3552 KB)
  Playerable/Boomber/Animation/BaseHumanBone_Horse_Shovel_Attack.001.anim  (7006 KB)
  Playerable/CactusCannon/Animation/BaseHumanBone_Cannon_Attack.001.anim  (7475 KB)
  Playerable/CactusCannon/Animation/BaseHumanBone_Cannon_Attack.anim  (7475 KB)  was: CombatUnitAnimation_Catus.asset
  Playerable/CactusCannon/Animation/BaseHumanBone_TwoHandGun_Idle.anim  (3399 KB)
  Playerable/CactusCannon/Animation/Cannon_Attack.002.anim  (27 KB)  was: CombatUnitAnimation_Catus.asset
  Playerable/CowBoy/Animation/BaseHumanBone_Horse_Idle.anim  (3614 KB)
  Playerable/CowBoy/Animation/BaseHumanBone_lassoAttack.001.anim  (6459 KB)  was: CombatUnitAnimation_Cowboy.asset
  Playerable/CowBoy/Animation/BaseHumanBone_lassoAttack.001_BaseHumanBone.anim  (6399 KB)
  Playerable/CowBoy/Animation/BaseHumanBone_lassoAttack.002.anim  (6398 KB)
  Playerable/CowBoy/Animation/Enemy_BaseHumanBone_lassoAttack.001.anim  (6981 KB)
  Playerable/CowBoy/Animation/lasso.001_Attack.002.anim  (82 KB)  was: CombatUnitAnimation_Cowboy.asset
  Playerable/CowBoy/Animation/lasso.001_Attack.002_Duplicate.anim  (82 KB)
  Playerable/ShotGun/Animation/BaseHumanBone_Melee_Idle.001.anim  (3742 KB)
  Playerable/ShotGun/Animation/BaseHumanBone_Melee_Idle.anim  (3742 KB)
  Playerable/ShotGun/Animation/BaseHumanBone_ShotGun_Attack.001.anim  (6193 KB)
  Playerable/ShotGun/Animation/BaseHumanBone_ShotGun_Attack.anim  (6193 KB)  was: CombatUnitAnimation_Shotgun.asset
  Playerable/ShotGun/Animation/Enemy_BaseHumanBone_ShotGun_Attack.anim  (6774 KB)
  Playerable/Sniper/Animation/BaseHumanBone_Rifle_Idle.001.anim  (3536 KB)
  Playerable/Sniper/Animation/BaseHumanBone_Rifle_Idle.anim  (3536 KB)
  Playerable/Sniper/Animation/Enemy_BaseHumanBone_Rifle_Idle.anim  (3749 KB)
KEEP new-set unused 8
  Playerable/Akimbo/Animation/Akimbo_Landing.anim
  Playerable/Akimbo/Animation/Akimbo_SpecialAttack.anim
  Playerable/CowBoy/Animation/CowBoy_PullBack.anim
  Playerable/CowBoy/Animation/Horse_PullBack.anim
  Playerable/CowBoy/Animation/Lasso_PullBack.anim
  Playerable/Gravedigger/Animation/Gravedigger_Attack.anim
  Playerable/Gravedigger/Animation/Gravedigger_Hit.anim
  Playerable/Gravedigger/Animation/Gravedigger_Recovery.anim
total KB deleted 242767
```
