# 전투 유닛 새 애니메이션 클립 연결 — Task 계획

- 날짜: 2026-11-17
- 상태: TP-1~TP-13 그대로(11-17 사용자 "일단 진행"). CW-0 정적 확인 완료(§5.1, 문제 0), U0 대기. 찾은 것 F-1은 A로 반영(§5.2, 감싼 다섯 프리팹의 몸 상자를 루트로, 에셋만), F-2는 `Untagged`로 되돌림, CactusCannon 새총(뼈 수 어긋나 안 그려짐)은 뼈 하나 더해 고침(§5.3). 코드 변경 없음. U0·Uf·Ug 문제 없음(사용자). **CW-a·CW-b·CW-d·CW-e·CW-f 문서 완료(§5.4~§5.9, 정적 대조 문제 0, TP-8은 사용자 결정으로 Claude가 YAML).** 남은 것은 §5.10(묶음 끝 검토 결과, CW-c 값, 마지막 Unity 확인, 동작 구조 제안 결정). 사용자 지시로 스테이지에 올림(11-17, 다른 PC에서 이어 함).
- 설계: [2026-11-17-combat-unit-new-clips-wiring-design](../specs/2026-11-17-combat-unit-new-clips-wiring-design.md)(결정 §3, Phase §4, 제거 후보 부록 A)
- 경로 줄임: `A/` = `Assets/ProjectL/Prefabs/Characters/New/<진영>/<유닛>/Animation/`, `B/` = `Assets/ProjectL/DataBaseSO/ViewGroup/CombatPresentation/AnimationClip/`, `V/` = `Assets/ProjectL/DataBaseSO/ViewGroup/UnitVisual/Item/`

## 0. 진행 규칙

- Phase마다 멈추고 보고한다. 커밋·push는 사용자.
- **코드 변경 없음**(에셋 값만). 새 시험 없음, 기존 시험 변경 없음. 코드가 없어 MSBuild도 돌리지 않는다.
- 위험은 컴파일이 아니라 **부트의 DB 생성 검사**(`CombatUnitAnimationRules.ValidateUnit`)다. 클립 이벤트 셋·Loop Time·루트 곡선·부속 곡선 겹침·이벤트 컴포넌트·총구가 어긋나면 부트가 멈춘다. 그래서 Phase마다 Claude가 같은 규칙을 스크립트로 먼저 대조하고(정적), 사용자가 Unity에서 부트·재생을 확인한다.
- 묶음 끝 검토는 CW-f에서 새 에이전트 하나로 한 번(메모리 규칙).

## 1. 계획에서 정한 것(TP, 사용자 확인 대상)

| 번호 | 정한 것 | 이유 |
|---|---|---|
| TP-1 | CW-a·CW-b는 새 클립의 이벤트 시각(`ClipEvent` = 길이 절반)을 **그대로** 연결하고, 시각은 CW-c에서 바꾼다 | Q-CW1 B는 "보고 고침"이라 먼저 붙여서 봐야 정할 수 있다 |
| TP-2 | CW-c의 시각은 **사용자가 Unity에서 보고 정해 알려 주면 Claude가 `.anim`의 `m_Events`에 적는다**(사용자가 Animation 창에서 직접 옮겨도 된다 — 그때는 Claude가 값만 대조). 형식: 유닛·`attackSpeed`·`ClipEvent` 초(또는 프레임 + fps)·Akimbo는 `ClipEvent2`도 | 화면을 보는 사람이 정하고, 적는 일과 규칙 대조는 한 곳에서 |
| TP-3 | Akimbo `ClipEvent2` = **0.90625초**(1.2083334 × 0.75), `ClipEvent`(0.6041667)와 `ClipEnd` 사이 줄, 나머지 칸은 다른 이벤트와 같은 모양, 줄 끝 CRLF 유지 | Q-CW5. 시각 규칙은 시작 ≤ `ClipEvent` ≤ `ClipEvent2` ≤ 끝(`CombatClipTimings.FindTimeError`) — CW-c에서 `ClipEvent`를 0.906 뒤로 옮기면 `ClipEvent2`도 함께 뒤로 |
| TP-4 | 묶음의 `attackSpeed`·`shots`·`hitDelay`·시작 모드 칸은 CW-a·CW-b에서 **그대로**(템포는 CW-c) | 한 Phase에 한 가지 변화만 보이게 |
| TP-5 | 카우보이 시작 모드 1(끌어오기 발동 310에 맞춤) 그대로. 새 공격 클립이 3.17초라 끌어온 뒤 때리는 순간이 늦어 보일 수 있다 → CW-c에서 `attackSpeed`·`ClipEvent`로 | Q-CW2 A |
| TP-6 | SoulEater 묶음 둘(`Enemy/`·`Playerable/`)은 그대로 | 아군 SoulEater는 임시 설정(11-16), 적 묶음은 쓰는 외형 없음 |
| TP-7 | 같은 묶음을 함께 쓰는 외형은 함께 바뀐다: Cannon = 25·84·85·86, FixSo = 13·81·82, Sabow = 17·83·87, Heavy = 26·88, Akimbo = 10·14(메인) | 묶음은 외형이 참조하는 값 하나(SSOT) |
| TP-8 | Sniper 새 프리팹 부품은 **사용자 에디터 작업**(CW-d 목록), 외형 항목·새 묶음은 그 뒤 Claude가 YAML로. 프리팹·새 묶음·외형 항목은 한 커밋 | 프리팹 변형의 부품 추가는 YAML로 하면 틀리기 쉽다. 묶음이 붙었는데 이벤트 컴포넌트가 없으면 부트가 멈춘다 |
| TP-9 | Entrenchment(88) 프리팹 참조 = `UnitVisual_Heavy`와 같은 값(`{fileID: 7218930156937943906, guid: adac08525a1cbc441afb51c94ea3a602, type: 3}`), 다른 칸은 그대로 | Q-CW8 A, 새 EnemyHeavy는 이미 Heavy(26)가 쓰는 완성된 프리팹 |
| TP-10 | 묶음·외형 에셋은 YAML을 직접 고친다. 클립 참조 모양은 `{fileID: 7400000, guid: …, type: 2}`. 새 묶음 `.meta`는 새 GUID로 Unity 기본 형식(`NativeFormatImporter`, `mainObjectFileID: 11400000`)을 직접 쓴다 | 지금까지 묶음 에셋을 고친 방식 |
| TP-11 | 제거 범위 = `Assets/ProjectL/Prefabs/Characters/New` 아래 `.anim` 중 **CW-a~CW-d 연결 뒤에도 참조가 0**인 것 + 그 `.meta`. 새 세트 미사용 8개와 참조가 남는 옛 클립 둘은 남김(설계 부록 A). 목록은 CW-e 착수 때 작업 트리 기준으로 다시 셈. `Colored/` 등 다른 폴더는 범위 밖 | 사용자 "아예 안 쓰는 클립은 제거"(이번 한정). 그 사이 다른 작업자 변경을 반영 |
| TP-12 | 지우기는 파일 삭제만(`git rm`·커밋 안 함). Unity가 열려 있으면 삭제 뒤 에셋 새로고침 | 커밋은 사용자 |
| TP-13 | 피격 클립을 넣으면 보이는 차이 — 그 유닛의 다음 사건이 피격 클립 끝(1.0~2.0초)까지 기다림, 연발 앞 탄 접촉 피격은 그 유닛이 다른 연출 중이면 나오지 않음(`CanBeginContactHit`) — 는 Q-CW3·Q-CW4 A대로 그대로 받아들이고, Ub에서 보고 다시 정한다 | 결정 그대로. 피격 큐(이펙트·흔들림·플래시)는 시작 시점(`anchor: 1` = Start)이라 피격 클립 `ClipEvent`와 무관 |

## 2. 값 표

### 2.1 클립 GUID(`A/` 아래 `.anim`, 11-17 `.meta`에서 읽음 — CW-0에서 다시 대조)

| 유닛 폴더 | 클립 | GUID |
|---|---|---|
| Enemy/EnemyCannon | Enemy_Cannon_Attack | aa3ecba4b76be4f4187b211a34feb144 |
| | Cannon_Attack | 62b0092ffee04664283b6cd5bc42bf3f |
| | Enemy_Cannon_Hit | dcbac5cdbf32f464f8e9017fdec55cc0 |
| | Cannon_Hit | 5ebade1806cc20c4c887e36d8c3454a0 |
| Enemy/EnemyMiner | Enemy_Miner_Attack | be280a0b24a083543bba9a2541b40bdb |
| | Enemy_Miner_Hit | 28b70c7e9b6f73b4b9507c6cf72bcec9 |
| Enemy/EnemyHeavy | EnemyHeavy_Attack | 1bd925c8b80462c498849aa1b91d4e7b |
| | EnemyHeavy_Hit | 25ad9e50593ab6841a78216501edd5a4 |
| Enemy/EnemyShabow | Enemy_Horse_Shovel_Hit | bc3f92b89211d864cbf71e6745013741 |
| | Enemy_Horse_Hit | bdbe11422376c1a4ab10992db4376790 |
| Enemy/Stage1Boss | Stage1Boss_Attack | 6833abb8c7f81cb4aa21dbed98f4f1db |
| | Stage1Boss_Hit | da554922e4483124a8e48ba13789fd6c |
| Playerable/Akimbo | Akimbo_Attack | 36f80e1d1beff684db931dc90d8fe39f |
| | Akimbo_Hit | 8ed117f80daffea49a7c22e28080c7d7 |
| Playerable/Ballistar | Ballista_Sniper_Attack | 73150e572894ea64ea7188137521611b |
| | Ballista_Attack | 2f485cfc569b31b4eacb800c715af59e |
| | Ballista_Sniper_Hit | a447aa59a6345f549a768b13622ab990 |
| | Ballista_Hit | de5b9b0c950876d439f01044ffb81061 |
| Playerable/Boomber | Horse_Player_Boomber_Hit | ebda55849174e064bb3136874f3a0288 |
| | Horse_Player_Hit | 4e9e74057a6f7fb45b2d91a589485bb1 |
| Playerable/CactusCannon | CactusCannon_Attack | c69ce99b90a8aed44ab786052ae47467 |
| | Slingshot_Attack | 18303e4b4307779438f057b3ffb55481 |
| | CactusCannon_Hit | bbb924603b20f3a4f8a6a76021df14cc |
| | Slingshot_Hit | 69c15430538c870449f48666b420a942 |
| Playerable/CowBoy | CowBoy_Attack | ee0da1a41f20d5d4899d9ba435beab65 |
| | Horse_Attack | a337a743922714e4793bb92996710bff |
| | Lasso_Attack | ed9c7137fc9677c40a3d225db55a81f5 |
| | CowBoy_Hit | a175f05178e0b0343a2f3ac67e5ad544 |
| | Horse_Hit | 88863f66bd3c17046b68d077a0bb9561 |
| | Lasso_Hit | 1deefa9e49a76c742920b980e3112b36 |
| Playerable/ShotGun | ShotGun_Attack | 1366d84bc83ded044861e3a4aeb4eab6 |
| | ShotGun_Hit | 01d9f63793b836b489a629e6c7db6af9 |
| Playerable/Sniper | Sniper_Attack | b17525b116f714148b254ab179f89de8 |
| | Sniper_Hit | 6c6b4621d0f9de54182363dc12e3cc61 |

### 2.2 프리팹·묶음

| 대상 | 값 |
|---|---|
| 새 Sniper 프리팹 `New/Playerable/Sniper/Sniper.prefab` | guid `189244eaa27a73f063b327c890529bc1`, 루트 GameObject(변형의 stripped 객체) fileID `904658592152046879` — CW-d에서 사용자 편집 뒤 다시 확인 |
| 새 EnemyHeavy 프리팹 | guid `adac08525a1cbc441afb51c94ea3a602`, 루트 fileID `7218930156937943906`(Heavy 26과 같음) |
| 옛 Sniper(`Colored/Playerable/Sniper.prefab`) | guid `8ffc0a6b8a5119e49b7fa9abe1c1127f` — 지금 12번 외형, 교체 뒤에도 지우지 않음 |
| 옛 HeavyEnemy(`Colored/Enemy/HeavyEnemy.prefab`) | guid `79c7b6b528f2889468309c812f519bc7` — 지금 88번 외형, 교체 뒤에도 지우지 않음 |
| 묶음 스크립트(`CombatUnitAnimationSO`) | `m_Script: {fileID: 11500000, guid: 870abfb8e4aa47df9fb2edb07fed2577, type: 3}` |

## 3. Phase별 Task

### CW-0 착수 전 확인(코드·에셋 변경 없음)

- [ ] 0.1 `git fetch` 뒤 HEAD·원격·develop 차이, 이번에 고칠 파일(§4 목록)을 다른 커밋이 건드렸는지.
- [ ] 0.2 정적 규칙 대조(스크립트, 세션 스크래치 — 지워졌으면 다시 만듦): §2.1 클립 34개 GUID, 몸 클립의 이벤트 셋(`ClipStart`·`ClipEvent`·`ClipEnd`)·Loop Time 꺼짐·루트 곡선 없음, 부속 클립 이벤트 없음·몸 클립과 속성 단위 곡선 겹침 0, 새 클립 경로 ↔ 같은 폴더 Idle 클립 경로 대조(카우보이 옛 공격 0/217 재확인 포함).
- [ ] 0.3 작성자 줄: 묶음 대부분은 우리(Droute) 에셋, `Sabow`·`Ballista`·`Bommber`는 민형님 `fbb9780d4`가 마지막으로 고침(이번에 Ballista 공격 칸을 바꿈 → 공유), `UnitVisual_UndeadHeavy_Entrenchment`는 재령님(`rkf_k`), `UnitVisual_Sniper`의 소리 칸은 `Twinsgm` 줄(안 바꿈 → 공유 없음).
- [ ] **U0(사용자):** 부트 DB 생성 통과, 카우보이 공격이 지금 움직이지 않음(기준 상태), 다른 유닛 공격은 지금처럼.

### CW-a 공격 묶음 새 세트로 + Akimbo 두 발째 이벤트

- [ ] a.1 `A/`(Akimbo) `Akimbo_Attack.anim` `m_Events`에 `ClipEvent2` 한 줄(TP-3).
- [ ] a.2 공격 묶음 9개 `attackClip`·`attackCompanionClips`(다른 칸 그대로, TP-4):

| 묶음(`B/`) | attackClip | attackCompanionClips |
|---|---|---|
| Enemy/CombatUnitAnimation_Cannon | Enemy_Cannon_Attack | Cannon_Attack |
| Enemy/CombatUnitAnimation_FixSo | Enemy_Miner_Attack | (없음) |
| Enemy/CombatUnitAnimation_Heavy | EnemyHeavy_Attack | (없음) |
| Enemy/CombatUnitAnimation_Stage1Boss | Stage1Boss_Attack | (없음) |
| Playerable/CombatUnitAnimation_Akimbo | Akimbo_Attack | (없음) — `shots` 그대로(이벤트 1 → 총구 0, 이벤트 2 → 총구 1) |
| Playerable/CombatUnitAnimation_Ballista | Ballista_Sniper_Attack | Ballista_Attack |
| Playerable/CombatUnitAnimation_Catus | CactusCannon_Attack | Slingshot_Attack |
| Playerable/CombatUnitAnimation_Cowboy | CowBoy_Attack | Horse_Attack, Lasso_Attack |
| Playerable/CombatUnitAnimation_Shotgun | ShotGun_Attack | (없음) |

- [ ] a.3 정적 재대조: 묶음 → 클립 참조가 §2.1과 같음, Akimbo 발사 목록의 이벤트 둘이 클립에 있음·시각 순서, 부속 겹침 0.
- [ ] **Ua(사용자):** 부트 통과. 유닛마다 공격 동작과 부속(말·무기·올가미·새총)이 함께 움직임, 맞는 순간·발사 위치(지금은 클립 절반 시점). 원거리 = Cannon·Ballista·Catus(곡사), Akimbo·메인(14) 두 발(총구 둘), Shotgun 부채꼴. 카우보이 끌어오기 뒤 공격이 이제 움직임. 보스 포효·소환 때 동작, 화약꽃(메인 스킬 — 공격 클립을 빌림). 특성 변형 유닛(TP-7)도 같은 동작.

### CW-b 피격 묶음

- [ ] b.1 묶음 11개 `hitClip`·`hitCompanionClips`:

| 묶음(`B/`) | hitClip | hitCompanionClips |
|---|---|---|
| Enemy/…_Cannon | Enemy_Cannon_Hit | Cannon_Hit |
| Enemy/…_FixSo | Enemy_Miner_Hit | (없음) |
| Enemy/…_Heavy | EnemyHeavy_Hit | (없음) |
| Enemy/…_Sabow | Enemy_Horse_Shovel_Hit | Enemy_Horse_Hit |
| Enemy/…_Stage1Boss | Stage1Boss_Hit | (없음) |
| Playerable/…_Akimbo | Akimbo_Hit | (없음) |
| Playerable/…_Ballista | Ballista_Sniper_Hit | Ballista_Hit |
| Playerable/…_Bommber | Horse_Player_Boomber_Hit | Horse_Player_Hit |
| Playerable/…_Catus | CactusCannon_Hit | Slingshot_Hit |
| Playerable/…_Cowboy | CowBoy_Hit | Horse_Hit, Lasso_Hit |
| Playerable/…_Shotgun | ShotGun_Hit | (없음) |

- [ ] b.2 정적 재대조(a.3과 같은 항목, 피격 칸).
- [ ] **Ub(사용자):** 부트 통과. 피격 동작 + 부속, 플래시·외곽선·이펙트가 맞는 순간에 함께, 그 유닛의 다음 사건이 피격 클립 끝까지 기다림(다른 유닛은 안 막힘, TP-13), 연발 앞 탄 접촉 피격, 넉백 피격(맞는 순간 밀림 + 젖힘), 피해로 죽을 때(사망 반응 — 피격 클립 안 씀), 보스 피격. 결과 재생이 너무 길어지면 Q-CW3을 다시 정함.

### CW-c 템포·이벤트 시각

- [ ] c.1 사용자가 유닛마다 정한 값을 받음(TP-2 형식). 받기 전에는 멈춤.
- [ ] c.2 Claude가 묶음 `attackSpeed`와 `.anim` 이벤트 시각을 적음. 시각 순서 규칙·발사 목록 대조.
- [ ] **Uc(사용자):** 다시 봄. 피격 클립 `ClipEvent`는 지금 피격 큐가 시작 시점만 써서 바꿀 필요 없음(바꾸고 싶으면 같은 방식).

### CW-d Sniper 새 모델·Entrenchment 프리팹

- [ ] d.1 **사용자 에디터 작업**(본보기 = 새 `Akimbo.prefab`):
  - 루트 크기 0.7(새 Akimbo·ShotGun과 같음), 모든 자식 레이어 6(새 Akimbo와 같음 — 지금 새 Sniper는 레이어 수정이 하나도 없음).
  - 루트에 `BoxCollider`(새 Akimbo 값: 크기 1 × 1.35 × 1, 중심 y 0.63 — 몸 접촉 모양).
  - 루트(Animator와 같은 GameObject)에 `CombatAnimationEvents` + 총 끝 뼈 아래 자식 `Muzzle`·`EffectPoint`를 만들어 연결(마탄 메인은 직선 원거리라 총구 필수).
  - `CombatUnitRagdoll`(꺼 둔 채, `boneLayout` 0 — 새 Akimbo와 같음).
- [ ] d.2 새 묶음 `B/Playerable/CombatUnitAnimation_Sniper.asset`(+ `.meta` 새 GUID): 공격 `Sniper_Attack`, 피격 `Sniper_Hit`, 부속 없음, `attackSpeed` 1, `hitDelay` 0, `shots` 없음, 시작 모드 0.
- [ ] d.3 `V/Playerable/UnitVisual_Sniper.asset` `prefab` → 새 Sniper(§2.2, fileID는 d.1 뒤 다시 확인), `animationPresentation` → d.2 묶음.
- [ ] d.4 `V/EnemyTrait/UnitVisual_UndeadHeavy_Entrenchment.asset` `prefab` → TP-9 값.
- [ ] d.5 정적 재대조: 새 Sniper 프리팹에 `CombatAnimationEvents` 하나·총구 연결·Animator 같은 GameObject, Sniper 클립 경로 ↔ Sniper Idle 경로.
- [ ] **Ud(사용자):** 부트 통과. 마탄 메인(로즈) Idle·공격·피격, 발사 위치(총구), 마탄 스킬 경로 비행 시작점, 몸 접촉(투사체가 몸에 닿는 순간), 사망 랙돌. Entrenchment(88) Idle·공격이 이제 움직임.

### CW-e 참조 없는 클립 제거

- [ ] e.1 작업 트리 기준으로 제거 목록 다시 셈(TP-11, 참조 검사 = 모든 YAML 에셋: `.asset`·`.controller`·`.overrideController`·`.prefab`·`.unity`·`.playable`·`.mask`). 설계 부록 A(47개)와 다르면 차이를 보고하고 멈춤.
- [ ] e.2 목록의 `.anim` + `.meta` 삭제(TP-12).
- [ ] e.3 삭제 뒤 다시 셈: 깨진 참조(지운 GUID를 가리키는 에셋) 0.
- [ ] **Ue(사용자):** 부트 통과, 모든 유닛 Idle·공격·피격, 콘솔에 Missing 없음.

### CW-f 문서·공유·묶음 끝 검토

- [ ] f.1 민형님 공유 새 메모 `docs/superpowers/handoffs/2026-11-17-combat-unit-new-clips-minhyeong-share.md`: 연결한 묶음(민형님이 고친 Ballista·Sabow·Bommber 포함), Akimbo `ClipEvent2` 추가, CW-c에서 바꾼 이벤트 시각 표, 지운 클립 목록, 남긴 새 세트 8개, 남은 중복 클립 보고, 새 Sniper 프리팹에 넣은 부품, 카우보이 옛 클립이 새 모델과 안 맞던 사실.
- [ ] f.2 재령님 공유 새 메모(88번 외형 프리팹 교체, 11-11부터 안 움직이던 원인).
- [ ] f.3 [이펙트·소리 창구 안내](../handoffs/2026-10-29-combat-effect-sound-cue-guide.md)의 "피격 클립 없음" 등 낡은 줄, 설계 §5 구현 메모, CLAUDE.md 상태 줄.
- [ ] f.4 묶음 끝 검토(새 에이전트 하나, 읽기 전용): 묶음·외형·`.anim` 변경을 부트 규칙과 대조, 삭제 뒤 깨진 참조, 문서 일치.

## 4. 고칠 파일·커밋 묶음

| 묶음 | 파일 | 함께 넣을 것 |
|---|---|---|
| ① CW-a·CW-b | `B/` 묶음 11개, `A/`(Akimbo) `Akimbo_Attack.anim` | **한 커밋**(Akimbo 묶음만 들어가고 `ClipEvent2`가 없으면 부트가 "발사 1의 이벤트 Event2가 없습니다"로 멈춤) |
| ② CW-c | 묶음 `attackSpeed`, 바꾼 `.anim` | ① 뒤 |
| ③ CW-d | 새 `Sniper.prefab`(Claude가 YAML로, TP-8 바뀜), `B/Playerable/CombatUnitAnimation_Sniper.asset` + `.meta`, `UnitVisual_Sniper.asset` | **한 커밋**(묶음이 붙은 외형인데 프리팹에 이벤트 컴포넌트가 없으면 부트가 멈춤). `UnitVisual_UndeadHeavy_Entrenchment.asset`은 따로 넣어도 됨 |
| ④ CW-e | 지운 `.anim` + `.meta` | ①~③ 뒤(참조 수가 그 상태 기준) |
| ⑤ CW-f | 문서 | — |

작업 트리의 `FMODStudioCache.asset`·`SCDream7 SDF.asset` 변경은 이번 작업이 아니다(에디터가 바꾼 것).

## 5. 실행 기록

### 5.1 CW-0 착수 전 확인(2026-11-17 사용자 "일단 진행" = TP-1~TP-13 그대로, 코드·에셋 변경 없음)

- **0.1:** `git fetch` 뒤 HEAD는 develop보다 3커밋 앞·뒤처짐 0, 받을 것 없음. 작업 트리 변경은 FMOD 캐시·글꼴(이번 작업 아님)뿐.
- **0.2 클립 규칙(정적, 스크래치 `cw0_rules.py`):** §2.1 GUID 34개 모두 같음. 몸 클립 22개 모두 이벤트 셋 하나씩·시각 순서·`ClipEnd` = 길이·Loop Time 꺼짐·루트 곡선 없음, 부속 12개 모두 이벤트 없음·Loop Time 꺼짐·몸(과 앞 부속)과 곡선 겹침 0·길이가 몸과 같음. 문제 0.
- **0.2 프리팹 계층 ↔ 클립 경로(사용자 요청 "캐논·라이더는 클립에 맞춘다고 빈 오브젝트 밑에 넣었는데 확인", 스크래치 `prefab_paths.py`·`fbx_tree.py`·`wire_paths.py` — 프리팹 YAML + 이진 FBX를 직접 읽어 Animator 기준 실제 경로를 만듦):**
  - 감싼 구조: 캐논 셋(`EnemyCannon`·`Ballistar`·`CactusCannon`)과 라이더 둘(`EnemyShabow`·`Boomber`)은 **빈 루트 → 모델(Animator·이벤트 컴포넌트·몸 상자)** 두 층(10-29 `c86703f6a`부터). 모델의 로컬 위치가 캐논 z −0.434·−0.576·−0.790, 라이더 y −0.006으로 옮겨져 있다. `CowBoy`와 나머지는 한 층.
  - 클립 경로는 Animator가 붙은 안쪽 모델 기준이라 **연결할 새 클립 34개가 모두 경로 100% 일치**(카우보이 지금 공격 클립만 0/217 — 알던 결함). 감싼 루트·안쪽 모델 자체는 클립이 못 움직여(루트 곡선 금지) 맞춰 둔 위치가 공격·피격 중에도 유지된다.
  - 새 프리팹 계층은 FBX 원본과 같고, 더해진 것은 `Muzzle`·`EffectPoint`(총구·이펙트 지점)와 `WeaponPitchRig`·`WeaponPitchTarget`(무기 조준)뿐 — 클립이 건드리지 않는 자리.
  - 새 공격·피격 클립의 첫 키 자세 = 지금 Idle 첫 키(위쪽 뼈 위치 차 0, 회전 차 0°) → 클립으로 바꾸는 순간 몸이 튀지 않음. **예외: 보스 `Stage1Boss_Attack`·`_Hit` 왼팔(`DEF-rigid_upper_arm.L`) 17.4°** — Ua에서 볼 것.
- **찾은 것(이번 변경 전부터, 사용자 결정 대기):**
  - **F-1 감싼 다섯 프리팹은 루트에 몸 상자가 없다.** 몸 모양은 프리팹 루트 `BoxCollider`만 읽는데(`CombatBodyShapeCache.Read` — `prefab.GetComponent<BoxCollider>()`), 상자가 안쪽 모델에 있다. 그래서 이 유닛들은 몸 상자 대신 표시 중심의 점 하나로 접촉을 계산하고, 전투 진입마다 "외형 프리팹 루트에 BoxCollider가 없거나…" 경고가 난다(`CombatBodyShapeCache.cs:86-90`). 투사체가 몸 앞면이 아니라 몸 가운데에 닿을 때 맞는 연출이 시작되는 정도의 차이. 클립 연결과는 상관없음. 고치는 안: (A) 안쪽 상자를 루트로 옮김 — 중심에 안쪽 위치를 더함(예: EnemyCannon (0, 0.63, −0.434)), 에셋만 / (B) 루트에 없으면 이벤트 컴포넌트 GameObject의 상자를 읽게 코드 변경 / (C) 그대로.
  - **F-2 캐논 셋의 루트 태그가 `EditorOnly`**(11-11 `a074807e6`). 플레이 모드에서는 영향 없음. 빌드에서 지워질 수 있어 의도인지 확인 필요. 같은 커밋에서 끈 Animator·`RigBuilder`·이벤트 컴포넌트는 맞는 설정이다 — 실행 중 클립 채널이 Animator를 켜고(`UnitClipChannel.cs:100`), 조준 장치는 `RigBuilder`가 꺼져 있어야 동작한다(`CombatWeaponPitchRig.cs:22`).
- **U0(사용자):** 대기 — 부트 DB 생성 통과, 카우보이 공격이 지금 안 움직임, 다른 유닛 공격은 지금처럼.

### 5.2 F-1 A 몸 상자를 루트로(2026-11-17 사용자 "루트로 옮김(A)", 에셋만, 코드 변경 없음)

- 다섯 프리팹(`EnemyCannon`·`Ballistar`·`CactusCannon`·`EnemyShabow`·`Boomber`)에서 안쪽 모델의 `BoxCollider`를 빈 루트로 옮김. 같은 컴포넌트(fileID 그대로)의 `m_GameObject`를 루트로, 안쪽 GO 구성 목록에서 빼고 루트 목록(Transform 다음)에 더함, 중심 += 안쪽 로컬 위치. 파일마다 세 줄, CRLF 유지, 원본은 스크래치 `box_backup/`.

| 프리팹 | 크기(그대로) | 중심 전 → 뒤 |
|---|---|---|
| EnemyCannon | 1 × 1.25 × 1 | (0, 0.63, 0) → (0, 0.63, −0.4338839) |
| Ballistar | 1 × 1.35 × 1 | (0, 0.63, 0) → (0, 0.63, −0.5763384) |
| CactusCannon | 1 × 1.35 × 1 | (0, 0.63, 0) → (0, 0.63, −0.7899423) |
| EnemyShabow | 1 × 1.85 × 1 | (0, 0.2, 0) → (0, 0.194173527, 0) |
| Boomber | 1 × 1.85 × 1 | (0, 0.45, 0) → (0, 0.455741599, 0) |

- 같은 월드 상자인 근거: 안쪽 모델은 루트 바로 아래·회전 없음·크기 1·레이어 같음(6 Entity), 몸 모양은 중심·크기 둘 다 루트 크기를 곱함(`CombatBodyShapeCache.cs:78-83`).
- 상자를 찾는 다른 코드(랙돌 `CombatUnitRagdoll.cs:126`, 준비 숨김 `CombatUnitPreparationLease.cs:56`)는 `GetComponentsInChildren<Collider>`라 위치가 바뀌어도 같음. 이 상자 fileID를 가리키는 다른 에셋 없음.
- 대조(정적): 다섯 모두 루트 상자 있음·안쪽 상자 없음, 모든 구성 목록 ↔ 컴포넌트 `m_GameObject` 일치, 줄 수 그대로.
- **Uf(사용자):** 전투 진입 때 "외형 프리팹 루트에 BoxCollider가 없거나…" 경고가 이 다섯 유닛에서 안 남, 투사체가 몸 앞면에 닿을 때 맞음, 사망 랙돌 그대로. **단 Ballistar·CactusCannon은 상자가 칸 가운데보다 뒤라 앞에서 온 직선 탄에 "몸·궤적 미교차" 경고가 날 수 있음(검토 M1, §5.9 f.4 — 결정 대기).** U0·Uf·Ug 문제 없음(사용자)은 이 경고를 보기 전 확인이다.
- 프리팹은 민형님 에셋(랙돌·IK 줄)이라 CW-f 민형님 메모에 적는다. 커밋은 클립 묶음과 따로 넣어도 된다.

### 5.3 F-2 태그 되돌림·CactusCannon 새총 다시 보이게(2026-11-17 사용자 "Untagged로 되돌려주고, CatusCannon 무기 SlingShot이 사라졌네, 원본에서 가져와 같은 위치에", 에셋만)

- **F-2:** 캐논 셋(`EnemyCannon`·`Ballistar`·`CactusCannon`) 루트 태그 `EditorOnly` → `Untagged`(파일마다 한 줄, 다른 `EditorOnly` 없음).
- **새총이 안 보인 원인:** `Slingshot` 오브젝트·`SkinnedMeshRenderer`·메시 참조는 프리팹에 그대로 있고, 메시 번호(`5192874587645291261`)도 Unity가 가져온 FBX 결과(Library 산출물, 11-17 05:01)에 있다. 다른 것은 **뼈 수**다 — FBX 원본의 새총 스킨은 뼈 6개(`BarristarRootBone`·`Body`·`Spine`·``Barristar``·`뼈대.004`·`뼈대.004_end`), 가져온 메시의 뼈 이름 해시 배열도 6개 같은 순서인데, 프리팹 렌더러 뼈 목록은 5개(`뼈대.004_end` 빠짐)였다. 메시와 렌더러의 뼈 수가 다르면 Unity가 그 메시를 그리지 않는다. 풀린 프리팹은 FBX가 바뀌어도 뼈 목록을 따라가지 않아서, 민형님 FBX 교체(`fbb9780d4`) 때 새총에 뼈 하나가 더해진 것이 빠진 것으로 보인다.
- **고친 것:** 프리팹에 이미 있는 `Cannon/BarristarRootBone/Body/Spine/Barristar``/뼈대.004/뼈대.004_end`(fileID `5789206986293710910`)를 새총 렌더러 뼈 목록 6번째로 더함(한 줄). 위치는 그대로 — 새총 오브젝트 위치(z 1.1873667, x축 −90°, 크기 100)가 FBX 원본 자리와 같다.
- 대조(정적): 풀린 유닛 프리팹 다섯의 렌더러 10개 모두 뼈 목록이 FBX 스킨과 순서까지 같음(고친 새총 포함), 다른 렌더러에는 같은 문제 없음. 프리팹 인스턴스(Akimbo 등 8개)는 FBX를 따라가 해당 없음.
- **Ug(사용자):** CactusCannon Idle·공격 때 새총이 보이고 몸을 따라 움직임, 새총 부속 클립(`Slingshot_Attack`)과 함께 움직임.
- 민형님 메모(CW-f)에 적을 것: FBX를 바꾸면 풀린 프리팹은 렌더러 뼈 목록이 그대로라 뼈가 바뀐 메시는 안 보일 수 있음.
- **U0·Uf·Ug 문제 없음(2026-11-17 사용자).**

### 5.4 CW-a 공격 묶음 새 세트로 + Akimbo 두 발째 이벤트(2026-11-17 사용자 "CW-a 진행해", 에셋만, 코드 변경 없음)

- a.1 `Akimbo_Attack.anim` `m_Events`에 `ClipEvent2`(0.90625초) 한 줄 묶음을 `ClipEvent` 뒤에(TP-3, CRLF 유지).
- a.2 공격 묶음 9개 `attackClip`·`attackCompanionClips`를 §3 CW-a 표대로(스크래치 `cw_wire.py attack`, 원본은 `cw_backup/`). 다른 칸(`attackSpeed`·`shots`·`hitDelay`·시작 모드) 그대로(TP-4). 파일마다 바뀐 줄만(+20 −12).
- a.3 정적 재대조(스크래치 `bundle_check.py` — 묶음 파일을 직접 읽음): 묶음 13개 모두 규칙 위반 0. 공격 칸 = §2.1 클립, 이벤트 셋·시각 순서·Loop 꺼짐·루트 곡선 없음, 부속은 이벤트 없음·곡선 겹침 0, Akimbo 발사 목록(이벤트 1 → 총구 0, 이벤트 2 → 총구 1)의 이벤트 둘(0.604·0.906)이 클립에 있음. 클립 경로 ↔ 프리팹 계층은 CW-0(§5.1)에서 100% 일치.
- 지금 맞는 순간(`ClipEvent`) = 클립 절반 자리(TP-1). 새 클립이 길어 옛 것보다 공격이 느림(카우보이 1.25 → 3.17초, 샷건 1.00 → 2.43, 보스 2.00 → 3.08 등) — CW-c에서.
- Akimbo 두 발 간격이 옛 0.47초 → 0.30초로 줄어 앞 탄 접촉 피격 플래시(0.4초)와 겹칠 수 있다. 겹치면 마지막 탄 피격이 앞 피격 연출 끝을 기다린다(11-11 연발 접촉 피격 기록, 새 규칙 없음).
- **Ua(사용자):** 부트 통과. 유닛마다 공격 동작 + 부속(말·무기·올가미·새총)이 함께 움직임, 발사 위치·맞는 순간. 원거리 = Cannon·Ballista·Catus(곡사), Akimbo·메인(14) 두 발(총구 둘), Shotgun 부채꼴. 카우보이 끌어오기 뒤 공격이 이제 움직임. 보스 포효·소환 동작, 보스 공격 시작 때 왼팔 튐(17.4°, §5.1), 화약꽃(메인 스킬). 특성 변형 유닛(TP-7)도 같은 동작.
- 커밋: 묶음 9개와 `Akimbo_Attack.anim`은 한 커밋(§4 ①, CW-b와 같은 커밋이어도 됨).

### 5.5 CW-b 피격 묶음(2026-11-17 사용자 "일단 다음 작업 진행해, 확인 마무리에 할게", 에셋만)

- 묶음 11개 `hitClip`·`hitCompanionClips`를 §3 CW-b 표대로(스크래치 `cw_wire.py hit`, 원본 `cw_backup/*.hit`). 다른 칸 그대로.
- 정적 재대조(`bundle_check.py`): 묶음 13개 공격·피격 칸 모두 규칙 위반 0. 부트 규칙 코드(`CombatUnitAnimationRules.ValidateUnit`)를 다시 읽어 피격 클립 조건은 "반응 묶음 있음" 하나뿐임을 확인 — 모든 외형에 `CombatUnitReaction_Default`가 있다. 큐 규칙: 반응 묶음 큐는 모두 시작 시점(`anchor` 1)이라 피격 클립 이벤트와 무관, 공격 표시 큐 중 클립 이벤트를 따로 쓰는 것은 Akimbo `Cue2`(=`ClipEvent2`, CW-a에서 넣음) 하나뿐.
- **Ub(사용자, 마지막에 한 번):** §3 CW-b 목록 그대로.

### 5.6 CW-d 일부 — 새 Sniper 묶음·Entrenchment(2026-11-17, 사용자 작업이 필요 없는 것만)

- d.2 새 `B/Playerable/CombatUnitAnimation_Sniper.asset` + `.meta`(GUID `57d0dfa3cd764613b5e6966b7da37c9a`, FixSo 묶음과 같은 칸 모양, 공격 `Sniper_Attack`·피격 `Sniper_Hit`·`attackSpeed` 1·발사 목록 없음·시작 모드 0, CRLF). 아직 어느 외형도 가리키지 않아 부트 검사 대상이 아니다.
- d.4 `UnitVisual_UndeadHeavy_Entrenchment.asset` `prefab` → 새 EnemyHeavy(TP-9). 이제 Heavy(26) 외형과 다른 칸은 키·`attackSoundSlotIndex`(−1)뿐.
- 보고만: 옛 `Colored/Enemy/HeavyEnemy.prefab`은 이제 참조하는 외형이 없다(지우지 않음, CLAUDE.md 규칙).
- **남은 것:** d.1 새 Sniper 프리팹 부품(사용자 에디터, §3 CW-d 목록) → d.3 `UnitVisual_Sniper` 프리팹·묶음 연결 → d.5 대조 → Ud.

### 5.7 CW-e 참조 없는 클립 제거(2026-11-17)

- e.1 작업 트리 기준으로 다시 셈(스크래치 `cwe_list.py`, 참조 = `Assets` 아래 모든 YAML 에셋의 GUID): `Prefabs/Characters/New` 아래 `.anim` 115개 중 참조 있음 60, 새 세트 미사용 남김 8(설계 부록 A와 같음), **제거 47 = 설계 부록 A와 정확히 같음**(빠진 것·더해진 것 0, 약 249MB = 237MiB).
- e.2 47개 `.anim` + `.meta` 삭제(파일 삭제만, `git rm`·커밋 안 함 — git 상태 ` D` 94개).
- e.3 다시 셈(`cwe_broken.py`): 지운 GUID 47개를 가리키는 에셋 0.
- 남긴 옛 클립 둘(참조 있음): `Stage1Boss/…/BaseHumanBone_Spical_Attack`(쓰는 외형 없는 `Enemy/CombatUnitAnimation_SoulEater` 묶음), `Boomber/…/BaseHumanBone_Horse_Shovel_Attack`(아군 SoulEater 묶음).
- 되돌리기: 지운 파일은 HEAD에 있어 `git checkout -- <경로>`로 돌아온다.
- **Ue(사용자, 마지막에 한 번):** 부트 통과, 모든 유닛 Idle·공격·피격, 콘솔에 Missing 없음.
- **커밋 순서 주의:** 이 삭제는 ①(CW-a·CW-b 묶음)과 같은 커밋이거나 그 뒤여야 한다. 삭제만 먼저 들어가면 HEAD의 옛 묶음이 지운 클립을 가리켜 부트가 멈춘다.

### 5.8 CW-d 나머지 — Sniper 새 프리팹 부품·외형 연결(2026-11-17 사용자 "Claude가 YAML로" = **TP-8 바뀜**: 프리팹 부품도 Claude가 YAML로)

- **번호 출처:** 새 Sniper는 FBX를 품은 변형 프리팹이라 FBX 안 오브젝트 번호(fileID)가 필요하다. Unity가 가져온 결과 `Library/Artifacts/bc/bc36912beb030f9ae8fbf41a1c9597cb`(11-17 05:01, 직렬화 형식 22)를 직접 읽음(스크래치 `sfile.py`·`sniper_fbx.py`). 루트 GO `919132149155446097`·루트 Transform·프리팹이 이미 쓰던 렌더러 번호 둘이 모두 이 결과에 있어 같은 가져오기임을 확인. GameObject 295개.
- **총 자리:** 라이플 = `BaseHumanBone/MCH-hand_ik.parent.R/hand_ik.R/큐브.001`(정적 메시, 크기 0.01) — 새 Akimbo가 총구를 총 메시 오브젝트(`HandGunLeft`, 손 뼈 아래, 크기 0.01) 바로 아래에 둔 것과 같은 규칙. 총구 끝: 기본 자세에서 −X 끝이 몸 앞쪽(Z +0.99), +X 끝은 어깨 뒤(스크래치 `rifle_ends.py`). FBX 정점(`rifle_tip.py`)으로 Unity 메시 축이 x = −FBX x/100·y·z = FBX/100임을 맞추고, 총구 끝 단면 가운데를 라이플 기준 **(−1.2673, −0.0032, −0.0536)**으로 둠. `EffectPoint`도 같은 자리(옛 Sniper와 같음). 회전은 없음(Akimbo `Muzzle2`와 같음).
- **`Sniper.prefab`에 더한 것(새 Akimbo와 같은 모양, 스크래치 `sniper_prefab.py`, 원본 `cw_backup/Sniper.prefab`, CRLF):** 루트 크기 0.7(수정 셋), 모든 GameObject 레이어 6(수정 295), 라이플 아래 `Muzzle`·`EffectPoint`(레이어 6)와 라이플 stripped Transform, 루트에 `BoxCollider`(1 × 1.35 × 1, 중심 높이 0.63)·`CombatAnimationEvents`(총구·이펙트 지점 연결, 추가 지점 없음)·`CombatUnitRagdoll`(꺼짐, `boneLayout` 0). 새 번호는 겹치지 않게 만듦.
- **d.3:** `UnitVisual_Sniper.asset` `prefab` → 새 Sniper(루트 stripped GO `904658592152046879`), `animationPresentation` → 새 묶음 `CombatUnitAnimation_Sniper`. 다른 칸(소리 등) 그대로.
- **d.5 대조(정적):** 프리팹 안 참조 11개 모두 문서 있음, FBX 번호 300개 모두 가져오기 결과에 있음, 루트 GO 컴포넌트 = Animator·상자·이벤트·랙돌(꺼짐), 묶음을 쓰는 외형 22개 모두 이벤트 컴포넌트 하나·Animator와 같은 GO·루트 상자 있음, 원거리 외형(Cannon 넷·Ballista·Catus·Akimbo·메인·ShotGun·Sniper) 모두 총구 있음(근접은 총구 필요 없음), Sniper 클립 경로 ↔ 새 프리팹 100%, 묶음 14개 규칙 위반 0. `CombatAttack_Linear`에는 이펙트 큐가 없어 필요한 지점은 발사 총구 하나.
- 보고만: 옛 `Colored/Playerable/Sniper.prefab`은 이제 참조하는 외형이 없다(지우지 않음).
- **Ud(사용자, 마지막에 한 번):** 부트 통과, 마탄 메인(로즈) Idle·공격·피격, 크기(다른 아군과 비슷한지)·밤 화면에서 보이는지(레이어 6), 발사 위치가 총구 끝인지, 마탄 스킬 경로 비행 시작점, 투사체가 몸에 닿는 순간, 사망 랙돌. Entrenchment(88) Idle·공격·피격.
- 커밋: `Sniper.prefab`·새 묶음(+ `.meta`)·`UnitVisual_Sniper.asset`은 한 커밋(§4 ③).

### 5.9 CW-f 문서·공유(2026-11-17 사용자 "일단 문서, 검토 진행")

- f.1 민형님 공유 새 메모 [new-clips-minhyeong-share](../handoffs/2026-11-17-combat-unit-new-clips-minhyeong-share.md)(미전달): 연결 표, `Akimbo_Attack` `ClipEvent2`(다시 내보낼 때 빠지면 부트 멈춤), 프리팹 변경(몸 상자·태그·새총 뼈·Sniper 부품), FBX 교체 뒤 풀린 프리팹 뼈 목록 주의, 지운 47개·남긴 것, 남은 같은 내용 클립 한 쌍(`Cannon_Idle` = `Ballista_Idle`, 곡선 없는 무기 Idle), 전투가 클립에 요구하는 것. CW-c 이벤트 시각은 뒤에 덧붙일 자리.
- f.2 재령님 공유 새 메모 [entrenchment-prefab-jaeryeong-share](../handoffs/2026-11-17-entrenchment-prefab-jaeryeong-share.md)(미전달): 88 프리팹 교체·이유·소리 칸 −1 확인 부탁.
- f.3 [이펙트·소리 창구 안내](../handoffs/2026-10-29-combat-effect-sound-cue-guide.md) §2.5 지점 자리 표를 지금 상태로(Akimbo·Linear(로즈 새 모델)·Shotgun·CactusCannon도 "둘 다 됨", Hammer 추가), 피격 클립 줄(아이비 23·소울이터 27만 없음 → 공유 반응 묶음의 피격 `Cue` 시점은 아직 못 씀), 제목 갱신. 설계 §5 구현 메모·상태 줄.
- f.4 **묶음 끝 검토(새 에이전트 하나, 읽기 전용, 11-17 완료): 부트가 멈출 결함 없음, 중간 1·낮음 4.** 확인된 것: 묶음 14개·외형 22개 부트 규칙 위반 0, 손으로 쓴 YAML 참조·구성 목록 끊김 0, Sniper FBX 번호 299개가 가져오기 결과에 있음·레이어 수정 295 = FBX GameObject 295, 지운 클립 GUID 참조 0(`Assets`·`ProjectSettings`·`Packages`), 새총 뼈 6개 순서가 가져오기 결과 뼈 이름 해시와 같음(11-05 옛 가져오기는 5개 — FBX 교체로 늘어난 것 확인, Ballista 같은 모양 메시는 지금도 5개라 해당 없음), 몸 상자 이동은 같은 월드 상자.
  - **M1(중간, 사용자 결정 대기):** 루트로 옮긴 상자가 이제 처음 쓰이는데, Ballistar·CactusCannon은 모델을 뒤로 옮겨 둔 만큼 상자도 칸 가운데보다 뒤다(루트 기준 z 범위 = 중심 ± 0.5에 루트 크기 0.7을 곱해 Ballistar 약 −0.75~−0.05, CactusCannon 약 −0.90~−0.20, EnemyCannon −1.12~+0.08은 겨우 덮음). 직선 탄 목표점은 칸 가운데(`CombatAttackStyle.cs:149` → `CombatAttackTarget.Resolve`)라 앞·옆에서 온 탄은 상자에 닿기 전에 끝나 "몸·궤적 미교차" 경고 + 칸 투영 시각으로 대신한다(`CombatAttackPlayback.cs:404-415`). 피격 이펙트 자리(`SubjectBody` = 상자 중심)도 0.4~0.55 뒤로. 안: (A) 캐논 셋 상자 중심을 칸 가운데(루트 원점 z 0)로 다시 잡음 — 다른 유닛 상자처럼 / (B) 상자 z를 늘려 가운데까지 덮음 / (C) 그대로 받아들이고 문서만. §5.2 Uf의 "투사체가 몸 앞면에 닿을 때 맞음"은 이 두 유닛에서는 아직 맞지 않는다.
  - **L1(낮음, 고침):** Sniper에 새로 쓴 라이플 stripped Transform 번호가 Unity 규칙(`(PrefabInstance ^ 원본 fileID) & 0x7FFF…`)과 달랐음(`4389200384762591842`) → 규칙 값 `5271581406186612731`로 세 자리(문서 머리·`m_Father` 둘) 바꿈. 루트 GO 번호는 원래 규칙대로였음.
  - **L2(낮음, 그대로):** `Ballistar.prefab` 작업 트리가 LF — 07:22에 Unity가 우리 YAML을 읽고 다시 저장한 것(문서 순서만 바뀜, 내용 같음, git이 정규화).
  - **L3(낮음, 고침):** 지운 클립 크기 표기 통일(약 249MB = 237MiB), §4 ③ "(사용자)" → Claude, 이펙트 안내 §2.5 아이비는 묶음·프리팹이 없다는 표현.
  - **L4(낮음, Ud에서):** Sniper 총구는 기본 자세에서 몸 앞쪽(월드 (−0.53, 0.04, 0.73))까지만 정적으로 확인 — 눈으로 볼 것.
- 같은 날 사용자 질문·관찰 기록(코드·에셋 변경 없음): 왜 Animator가 아니라 코드가 클립을 바꿔 끼우나, 블렌드, 라이더 말 클립(이미 부속으로 연결), Gravedigger `Recovery`(넣을 칸 없음)·카우보이 `PullBack`(끌어당김 선행, 넣을 칸 없음) → 근본 구조 제안 [motion-structure-proposal](../specs/2026-11-17-combat-unit-motion-structure-proposal.md)(동작 키 + 단계 목록, 결정 대기 Q-MS1~Q-MS5).
- **확인 중(사용자):** 캐논 셋(Ballistar·CactusCannon·EnemyCannon) 클립이 Unity 클립 미리보기에서 안 돎. 클립 파일·FBX 가져오기 설정은 다른 유닛과 같음. 캐논 셋은 안쪽 모델 Animator가 꺼져 있음(11-11) → 미리보기에 FBX를 넣어 도는지 사용자 확인 중. 돌면 프리팹 쪽 원인(꺼진 Animator 또는 빈 루트 경로), 고치는 안 = 세 프리팹 Animator 켜기(전투 동작 같음, 배치 고스트가 Idle로 움직이게 됨) — 사용자 "일단 확인"으로 보류.

### 5.10 남은 것(2026-11-17, 다른 PC에서 이어 함)

1. **M1 결정**(§5.9 f.4 — 캐논 셋 몸 상자를 칸 가운데로 다시 잡을지, 권장 A). 검토 결과의 나머지는 반영 끝.
2. CW-c 템포·이벤트 시각: 사용자가 Unity에서 보고 유닛별 `attackSpeed`·`ClipEvent`(Akimbo는 `ClipEvent2`도) 값을 주면 Claude가 적음(TP-2, 순서 규칙 시작 ≤ `ClipEvent` ≤ `ClipEvent2` ≤ 끝). 민형님 메모 §2에 덧붙임.
3. 마지막 Unity 확인 한 번: Ua(§5.4)·Ub(§3 CW-b)·Ud(§5.8)·Ue(§5.7).
4. 캐논 셋 미리보기 원인 확인 결과(§5.9).
5. 동작 구조 제안 결정(Q-MS1~Q-MS5) → 설계·계획. SoulEater를 Gravedigger 모델로(Recovery 포함)는 이 결정과 같이.
6. 커밋(사용자, §4 묶음·순서 — 클립 삭제는 묶음 변경과 같은 커밋이거나 그 뒤), 공유 메모 전달.
- 참고: 이번에 쓴 분석 스크립트(FBX 이진 읽기 `fbx_tree.py`, 프리팹 계층 `prefab_paths.py`, Unity 직렬화 파일 읽기 `sfile.py` 등)는 이 PC의 세션 스크래치에만 있다. 다른 PC에서 같은 대조가 필요하면 다시 만든다.
