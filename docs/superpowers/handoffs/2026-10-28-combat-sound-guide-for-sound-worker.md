# 전투 소리 현황·조절 안내 — 공격·피격·사망 소리를 유닛 외형 줄로 (2026-11-17, 미전달)

> 보내는 사람: 용근(전투 View). 받는 사람: 사운드 작업자(FMOD·AudioSystem 담당).
> **2026-11-17: 공격·피격·사망 소리의 "무엇(번호) + 언제(시점·지연)"를 유닛 외형 맨 끝 "전투 소리" 줄 한 곳으로 옮겼습니다**([유닛 소리를 유닛마다 설계](../specs/2026-11-17-combat-unit-sound-per-unit-design.md)). 여러 유닛이 같이 쓰던 공격 묶음·반응 묶음의 소리 칸과 외형의 `attackSoundSlotIndex`는 없어졌고, 지금 소리는 그대로 옮겼습니다. 그 줄(§1·§2.3·§3·§4.1·§4.2·§4.3·§5·§6·§8)을 고쳤습니다. 시점 이름도 바뀌었습니다(`Cue` → `Event1`, `Hit` → `Contact`·`FirstHit`).
> 2026-11-12: 약한 지반 진입·붕괴와 낭떠러지 추락의 프리셋 번호를 전투 연출 설정 표 한 곳에서 고르게 바꿨습니다([소리 프리셋 원본 설계](../specs/2026-11-12-combat-view-sound-preset-mortar-lifecycle-design.md) SP1~SP3). 그 줄(§3·§4.2·§6)을 고쳤습니다.
> 공격·포효는 2026-11-10 커밋 `686900826`의 `UnitAttack` 플레이어와 유닛 외형 에셋을 기준으로 갱신했습니다. 그 밖의 소리 안내는 2026-10-29 기준이며, 최신 슬롯 설정은 `Assets/Scenes/Boot.unity`에서 확인합니다.
> 이펙트·흔들림 칸과 사건마다 넣는 칸의 지도는 [이펙트·소리 창구 안내](2026-10-29-combat-effect-sound-cue-guide.md)에 있습니다. 슬롯 배치의 원본은 Boot 씬이고, 유닛별 공격·피격·사망 소리 줄의 원본은 유닛 외형 에셋입니다.
> 이 문서는 "지금 어떤 소리가, 언제, 어떤 번호로 나는지"와 "번호·타이밍을 바꾸려면 어디를 고치는지"를 모은 안내입니다. 새 슬롯 요청은 [슬롯 요청 메모](2026-10-27-combat-projectile-hit-sound-audio-request.md)에 따로 있습니다.

## 1. 소리가 나는 길

```text
FMOD 이벤트
  └ Boot 씬의 FMOD 플레이어(AMBGM·Unit·SFX)의 슬롯 ─ 슬롯 순번 = 코드 열거 값(AmbgmEvent·UnitEvent·SfxEvent)
       └ 슬롯의 Parameter Presets 목록 ─ 목록 순번(0부터) = "프리셋 번호"
전투 코드: 일반 효과음 → AudioSystem.Play(슬롯, 프리셋 번호)
공격·포효·스킬: 유닛 외형 attackSounds 줄의 번호 → AudioSystem.PlayUnitAttack(슬롯 번호)
피격·사망: 유닛 외형 hitSounds·deathSounds 줄의 번호 → AudioSystem.Play(UnitHit(대기)·UnitDead, 프리셋 번호)
```

- **소리의 내용**(어떤 파일·변종)은 FMOD 이벤트와 Boot 슬롯의 프리셋이 정합니다.
- **어떤 번호를 쓸지**는 전투 DB의 SO 칸이 정합니다(§4).
- **언제 낼지**는 일부는 DB의 시점 칸(§5.1~§5.5), 나머지는 코드에 고정돼 있습니다(§5.6).
- **-1의 뜻이 두 가지**입니다.
  - DB 칸의 -1은 "소리를 내지 않음"입니다(우리 쪽 규칙). 유닛 소리 줄에는 -1이 없습니다 — 끄려면 줄을 지웁니다.
  - `AudioSystem.Play`의 프리셋 인자 -1은 현재 프리셋을 유지하고 재생한다는 뜻입니다. `PlayUnitAttack`은 선택한 슬롯의 설정으로 재생합니다.
- DB는 `-1` 또는 `0` 이상인지(유닛 소리 줄은 0 이상인지) 검사합니다. 공격음 슬롯 범위와 이벤트 연결은 재생할 때 `AudioSystem`이 검사하며, 잘못된 구성은 예외로 보고합니다.

## 2. Boot 플레이어 슬롯 현황(`Assets/Scenes/Boot.unity`)

### 2.1 AMBGM 플레이어 (2026-10-29 기준)

| 순번 | 코드 열거 | FMOD 이벤트 | 프리셋 |
|---|---|---|---|
| 0 | `BGM` | `event:/BGM/Stage` | 0 Title · 1 StageSelect · 2 Day · 3 Night (파라미터 BGM) |
| 1 | `AMB` | `event:/PostFX/AMB/SelectScreen` | 없음 |
| 2 | `UIClick` | `event:/UI/Click` | 없음 |
| 3 | `UIHover` | `event:/UI/Hover` | 없음 |
| 4 | `UIReward` | `event:/UI/Reward` | 없음 |
| 5 | `StageChanger` | `event:/BGM/StageChanger` | 없음 |
| 6 | `GameStart` | `event:/UI/GameStart` | 없음 |

### 2.2 효과음 슬롯 (2026-10-29 기준)

| 순번 | 코드 열거 | FMOD 이벤트 | 프리셋(번호: 값) |
|---|---|---|---|
| 0 | `UnitPick` | `…/SFX/Unit/Pick` | 0 Base · 1 Base · 2 Rider · 3 Heavy · 4 Cannon · 5 Enemy (파라미터 UnitType) |
| 1 | `UnitCancel` | `…/SFX/Unit/Cancel` | 0 Base · 1 Base · 2 Rider · 3 Heavy · 4 Cannon (Enemy 없음) |
| 2 | `UnitMovement` | `…/SFX/Unit/Movement` | UnitPick과 같음(0~5) |
| 3 | `UnitDead` | `…/SFX/Unit/Dead` | UnitPick과 같음(0~5) |
| 5 | `ObsDamaged` | `…/SFX/Obstacle/Damaged` | 없음 |
| 6 | `ObsBroken` | `…/SFX/Obstacle/Broken` | 없음 |
| 7·8·9 | (열거 없음) | `…/SFX/Artifact/Cancel`·`Pick`·`Placement` | 없음. 코드에서 부르는 곳이 없습니다 |
| 10 | `TrainCall` | `…/SFX/Boss/Train` | 없음 |
| 11 | `Tornado` | `…/SFX/Terrain/Tornado` | 없음 |
| 12 | (열거 없음) | `…/SFX/Object/ObjectActive` | **없음**. 파라미터 `Object`(Mine·Badge·Flag)만 있습니다 |

`…`는 `event:/PostFX`입니다.

> 2026-11-12 덧붙임: 위 표는 10-29 기준입니다. 현강님 11-10 변경으로 순번이 바뀌었고, 이 안내가 다루는 둘은 지금 **SFX 5 `WeakTerrian`**(프리셋 0·1·2 = CrackLv 0·1·2)과 **Unit 12 `Fall`**(프리셋 없음)입니다. 최신 순번은 `AudioSystem.cs`의 열거와 Boot 씬이 원본입니다.

### 2.3 UnitAttack 플레이어 (2026-11-10 기준)

유닛 외형 `attackSounds` 줄의 번호(`sound`)는 Boot의 `UnitAttack` 플레이어에서 0부터 센 이벤트 슬롯 순번입니다. 슬롯마다 FMOD 이벤트 하나를 연결합니다. 슬롯 순서를 바꾸면 그 슬롯을 가리키는 유닛 외형 줄도 함께 갱신합니다.

| 슬롯 | `event:/PostFX/SFX/Unit/AttackEvents/EachUnit/` 아래 이벤트 |
|---|---|
| 0 | `Akimbo` |
| 1 | `Ballistar` |
| 2 | `Catus` |
| 3 | `Cowboy` |
| 4 | `GraveKeeper` |
| 5 | `Hammer` |
| 6 | `Shotgun` |
| 7 | `Sniper` |
| 8 | `Enemy/E_Cannon` |
| 9 | `Enemy/Pickel` |
| 10 | `Enemy/Sheld` |
| 11 | `Enemy/Shovel` |
| 12 | `Enemy/Boss_Stage1` |

## 3. 전투 소리 목록

"상태"의 **나옴**은 지금 소리가 나는 것입니다. **슬롯 대기**는 코드 연결은 끝났지만 Boot 슬롯과 코드 열거 값이 없어 아직 나지 않는 것입니다(§6).

| 소리 | 슬롯 | 언제 | 번호를 고르는 곳(§4) | 타이밍(§5) | 상태 |
|---|---|---|---|---|---|
| 유닛 집기(배치) | `UnitPick` | 손패·필드에서 유닛을 집는 순간 | 유닛 외형 `unitSoundPreset` | 코드 고정 | 나옴 |
| 배치 취소 | `UnitCancel` | 끌던 유닛을 내려놓지 않고 취소 | 집을 때와 같은 번호 | 코드 고정 | 나옴 |
| 공격 조준 취소 | `UnitCancel` | 공격 모드를 취소 | 유닛 외형 `unitSoundPreset` | 코드 고정 | 나옴 |
| 걸음 | `UnitMovement` | 한 칸 걸음 시작마다(아군만) | 유닛 외형 `unitSoundPreset` | 코드 고정 | 나옴 |
| 공격(스킬 공격 포함) | `UnitAttack` 플레이어 | 공격한 유닛 `attackSounds` 줄의 시점 | 같은 줄의 `sound` | 같은 줄의 `anchor`·`delay` | 나옴 |
| 보스 포효 | `UnitAttack` 플레이어 | 포효 박치기 순간 | 포효한 유닛 `attackSounds` 줄의 `sound` | 코드 고정(줄의 시점·지연은 쓰지 않음) | 나옴 |
| 사망 | `UnitDead` | 죽는 유닛 `deathSounds` 줄의 시점 | 같은 줄의 `sound` | 같은 줄의 `anchor`·`delay` | 나옴 |
| 낭떠러지 추락 사망 | `UnitDead` | 가라앉기 시작 | 사망과 같은 줄 | 코드 고정(줄의 시점·지연을 쓰지 않음) | 나옴 |
| 전투 시작(밤) | `StageChanger` + `BGM` | 전투가 시작되는 순간. 진입음을 내고 BGM 프리셋을 Night로 | 설정 표 `nightBgmPreset` | 코드 고정 | 나옴 |
| 밤 끝(낮) | `BGM` | 밤이 끝나는 순간에 BGM 프리셋을 Day로 | 설정 표 `dayBgmPreset` | 코드 고정 | 나옴 |
| 화면을 그릴 때 | `BGM` | 화면을 새로 그릴 때마다 국면(밤·낮)에 맞춰 프리셋을 맞추고, 꺼져 있으면 재생 | 위의 두 칸 | 코드 고정 | 나옴 |
| 승리 | `UIReward` | 승리 결과 화면이 뜰 때 | 없음 | 코드 고정 | 나옴 |
| 결과 확인 뒤 | `BGM` | 결과 화면을 확인한 뒤 맵으로 돌아가기 전 | 설정 표 `afterCompletionBgmPreset` | 코드 고정 | 나옴 |
| 기차 호출 | `TrainCall` | 기차가 호출되는 순간 | 없음(프리셋 없이) | 코드 고정 | 나옴 |
| 토네이도 이동 | `Tornado` | 보이는 폭풍이 이동을 시작하는 순간(이동 한 번에 한 번) | 없음(프리셋 없이) | 코드 고정 | 나옴 |
| 장애물 피해·파괴 | `ObsDamaged`·`ObsBroken` | 화약통·보급품이 맞거나 부서지는 순간 | 장애물 외형 `unitSoundPreset`. -1이면 프리셋 없이 | 코드 고정 | 나옴(지금은 프리셋 없이) |
| 투사체 비행 | (대기) `ProjectileFlight` | 발사 순간 + 지연 | 투사체 부품 `flightSoundPreset` | 투사체 부품 `flightSoundDelay` | 슬롯 대기 |
| 투사체 착탄 | (대기) `ProjectileImpact` | 맞는 순간 + 지연 | 투사체 부품 `impactSoundPreset` | 투사체 부품 `impactSoundDelay` | 슬롯 대기 |
| 피격 | (대기) `UnitHit` | 맞은 유닛 `hitSounds` 줄의 시점(죽지 않을 때) | 같은 줄의 `sound` | 같은 줄의 `anchor`·`delay` | 슬롯 대기(지금 줄도 없음) |
| 오브젝트 발동·습득 | (대기) `ObjectActivated` | 유닛이 오브젝트 칸에 들어선 순간. 지뢰 = 발동, 깃발·보안관 배지 = 습득 | 오브젝트 외형 `objectSoundPreset` | 코드 고정 | 슬롯 대기 |
| 오브젝트 생성 | (대기) `ObjectCreated` | 전투 중 오브젝트가 생긴 순간 | 같은 `objectSoundPreset` | 코드 고정 | 슬롯 대기. Core가 이 사건을 아직 내지 않습니다 |
| 낭떠러지 추락 | Unit `Fall` | 유닛이 떨어지기 시작하는 순간(구조물 추락은 소리 없음) | 설정 표 `fallSoundPreset`. **-1(지금)이면 칸의 프리셋을 바꾸지 않고 냄** | 코드 고정 | 나옴 |
| 약한 지반 진입 | SFX `WeakTerrian` | 약한 지반에 들어가는 순간(무너지는 진입은 붕괴 소리만) | 설정 표 `weakGroundEntrySoundPresets`(남은 진입 횟수별, 지금 `[1, 2]`) | 코드 고정 | 나옴 |
| 약한 지반 붕괴 | SFX `WeakTerrian` | 약한 지반이 낭떠러지로 바뀌는 순간 | 설정 표 `weakGroundCollapseSoundPreset`(지금 0) | 코드 고정 | 나옴 |
| 토네이도 휩쓸림 | (대기) `TornadoLift` | 휩쓸리는 순간(토네이도 중심으로 다가가기 시작) | 토네이도 연출 설정 `liftSoundPreset` | 코드 고정 | 슬롯 대기 |
| 토네이도 착지 | (대기) `TornadoLand` | 목적지에 닿는 순간 | 토네이도 연출 설정 `landSoundPreset` | 코드 고정 | 슬롯 대기 |
| 토네이도 방향 예고 | (대기) `TornadoWarning` | 보이는 폭풍이 다음 이동 방향을 정하는 순간 | 토네이도 연출 설정 `warningSoundPreset` | 코드 고정 | 슬롯 대기 |
| 낙석 경고(터널 폭발) | (대기) `RockfallWarning` | 경고가 시작되는 순간 한 번 | 낙석 설정 `warningSoundPreset` | 코드 고정 | 슬롯 대기 |
| 낙석 착지 | (대기) `RockfallImpact` | 돌이 모두 착지한 순간 한 번 | 낙석 설정 `impactSoundPreset` | 코드 고정 | 슬롯 대기 |
| 하붑 구름 이동 | (대기) `HaboobRelocation` | 구름 칸이 실제로 바뀌는 순간 한 번(칸마다 내지 않음) | 하붑 설정 `relocationSoundPreset` | 코드 고정 | 슬롯 대기 |
| 아이템 발동 | (대기) `ItemActivated` | 아이템이 쓰인(소모된) 순간. 효과가 겹쳐 수치 없이 소모될 때도 | 아이템 외형 `itemSoundPreset` | 코드 고정 | 슬롯 대기 |

**전투 밖 소리(다른 작업자 영역, 위치만):**
- Boot·로비는 BGM 프리셋 0(Title)입니다(`GameBootstrap`, `LobbyViewSystem`).
- 스테이지 맵은 BGM 프리셋 1(StageSelect)과 `AMB`입니다(`StageMapViewSystem`).
- 모든 UI 버튼 클릭·호버는 `UIClick`·`UIHover`입니다(`UIInputBinding`).
- 타이틀 시작은 `GameStart`입니다(`TitleUI`).

## 4. 프리셋과 공격음 슬롯 번호를 바꾸는 곳

에셋 경로는 모두 `Assets/ProjectL/DataBaseSO/ViewGroup/` 아래입니다. 인스펙터에서 바로 고치면 됩니다. Play 중에 고친 값은 칸에 따라 다음 전투부터 반영되기도 하고, Play를 다시 시작해야 반영되기도 합니다(유닛 소리 줄은 재생을 시작할 때 읽어 두므로 다시 시작해야 함). 확실히 하려면 Play를 다시 시작하세요. 일반 효과음·피격·사망에는 해당 슬롯의 프리셋 번호를 넣고, 공격·포효에는 §2.3의 `UnitAttack` 슬롯 순번을 넣습니다.

### 4.1 유닛 외형 `UnitVisual/Item/…/UnitVisual_*.asset`

- `unitSoundPreset`: 아군의 집기·취소·걸음 소리 번호입니다. 장애물 외형이면 피해·파괴 번호입니다. 적의 집기·걸음은 설정 표의 `enemyUnitSoundPreset`(지금 5 = Enemy) 하나를 씁니다.
- **"전투 소리" 줄(맨 끝, 2026-11-17)**: 공격·피격·사망 소리를 유닛마다 정합니다. 줄 하나 = `sound`(번호) · `anchor`(시점) · `contactFilter`(시점이 Contact일 때만) · `delay`(초).
  - `attackSounds`: 번호 = `UnitAttack` 슬롯 순번(§2.3). 포효·스킬 공격도 이 줄입니다. 줄을 여럿 두면 다른 소리를 다른 시점에 냅니다(예: Event2에 휘두르기, Event1에 맞음).
  - `hitSounds`: 번호 = 피격 프리셋(슬롯 대기 §6).
  - `deathSounds`: 번호 = `UnitDead` 프리셋. **적도 이제 유닛마다** 정합니다(옮길 때 모두 옛 공용 값 5로 넣음 — 카테고리별로 바꾸려면 이 줄만 고치면 됩니다).
  - 비운 목록은 그 소리가 없습니다. 같은 카테고리 유닛은 같은 값을 외형마다 적습니다.

| 에셋 | 이름 | `unitSoundPreset` | 공격 줄 `[번호, 시점]` | 사망 줄 `[번호, 시점]` |
|---|---|---|---|---|
| `Playerable/UnitVisual_Akimbo` | 에블린 | 0 (Base) | [0, Event1] | [0, Start] |
| `Playerable/UnitVisual_ShotGun` | 와이엇 | 3 (Heavy) | [6, Event1] | [3, Start] |
| `Playerable/UnitVisual_Sniper` | 로즈 | 1 (Base) | [7, Event1] | [1, Start] |
| `Playerable/UnitVisual_Ballista` | 셰인 | 4 (Cannon) | [1, **Start**] | [4, Start] |
| `Playerable/UnitVisual_CactusCannon` | 조세핀 | 4 (Cannon) | [2, Event1] | [4, Start] |
| `Playerable/UnitVisual_Cowboy` | 웨이드 | 2 (Rider) | [3, Event1] | [2, Start] |
| `Playerable/UnitVisual_Boomber` | 찰리 | 2 (Rider) | [5, Event1] | [2, Start] |
| `Playerable/UnitVisual_SoulEater` | 소울이터(임시) | 2 (Rider) | [5, Event1] | [2, Start] |
| `Playerable/UnitVisual_Shielder` | 아이비 | -1 (없음) | 없음 | 없음 |
| `Playerable/UnitVisual_MainCharacter` | MainCharacter | -1 (없음) | 없음 | 없음 |
| `Enemy/UnitVisual_Demon` | 언데드 광부 | (적 공용) | [9, Event1] | [5, Start] |
| `Enemy/UnitVisual_Gravekeeper` | 언데드 무덤지기 | (적 공용) | [11, Event1] | [5, Start] |
| `Enemy/UnitVisual_Cannon` | 언데드 포병 | (적 공용) | [8, Event1] | [5, Start] |
| `Enemy/UnitVisual_Heavy` | 언데드 마인카트 | (적 공용) | [10, Event1] | [5, Start] |
| `Enemy/UnitVisual_StageBoss1` | 보스 | (적 공용) | [12, Event1] | [5, Start] |
| `EnemyTrait/UnitVisual_UndeadCannon_*`(셋) | 언데드 포병 변형 | (적 공용) | [8, Event1] | [5, Start] |
| `EnemyTrait/UnitVisual_UndeadHeavy_Entrenchment` | 마인카트 변형 | (적 공용) | [10, Event1] | [5, Start] |
| `EnemyTrait/UnitVisual_UndeadRider_Disarm` | 무덤지기 변형 | (적 공용) | [11, Event1] | [5, Start] |
| `EnemyTrait/UnitVisual_Demon_AnkleBreaker`·`_SoloAction`, `Enemy/UnitVisual_lqwe` | 광부 변형 | (적 공용) | **없음**(옛 칸이 비어 있었음) | [5, Start] |
| `Enemy/UnitVisual_Headquarters` | 적 넥서스 | -1 | 없음 | 없음(구조물) |
| `Object/UnitVisual_PowderBarrel` | 화약통 | -1 (프리셋 없이) | 없음 | 없음(구조물) |
| `Object/UnitVisual_Supplies` | 보급품 | -1 (프리셋 없이) | 없음 | 없음(구조물) |

시점 `Event1`은 공격 클립의 기준 이벤트(코드 모션이면 타격·발사 순간), 지연은 모두 0, 피격 줄은 모두 비어 있습니다.

### 4.2 전투 연출 설정 표 `CombatPresentationSettings/Item/CombatPresentationSettings_Default.asset`

"전투 소리 프리셋" 묶음에 있습니다. 추락 칸만 그 아래 새 머리말 "전투 소리 프리셋 — -1이면 프리셋 없이 냄"에 따로 있습니다(-1의 뜻이 다른 칸과 달라서).

| 칸 | 지금 값 | 뜻 |
|---|---|---|
| `enemyUnitSoundPreset` | 5 (Enemy) | 모든 적의 집기·걸음 소리(사망은 2026-11-17부터 유닛 외형 `deathSounds`) |
| `nightBgmPreset` | 3 (Night) | 밤(전투) 배경음 |
| `dayBgmPreset` | 2 (Day) | 낮(배치) 배경음 |
| `afterCompletionBgmPreset` | 1 (StageSelect) | 결과 확인 뒤 배경음 |
| `weakGroundEntrySoundPresets` | `[1, 2]` | 약한 지반 진입. 첫 원소 = 남은 진입 1, 둘째 = 남은 2, 남은 횟수가 더 많으면 마지막 원소. 원소 -1은 그 횟수에 소리 없음, 빈 목록은 진입 소리 없음 |
| `weakGroundCollapseSoundPreset` | 0 | 약한 지반 붕괴. -1이면 소리 없음 |
| `fallSoundPreset` | -1 | 낭떠러지 추락(유닛만). **-1이면 프리셋을 바꾸지 않고 냄**(Boot `Fall` 칸에 지금 프리셋이 없어 기본 소리). 0 이상을 쓰려면 Boot `Fall` 칸에 그 프리셋을 먼저 만들어야 하고, 없으면 재생 때 오류입니다 |

### 4.3 그 밖의 칸(모두 슬롯 대기, 지금 값 -1)

| 에셋 | 칸 | 뜻 |
|---|---|---|
| `CombatPresentation/Projectile/CombatProjectile_Ballista`·`_Bullet`·`_CactusCannon`·`_UndeadCannon` | `impactSoundPreset`·`flightSoundPreset` | 투사체마다 착탄·비행 소리 |
| `UnitVisual/Item/**/UnitVisual_*` | `hitSounds`(목록, 지금 모두 비어 있음, §4.1) | 피격 소리. 유닛마다 정합니다. 줄을 여럿 넣으면 한 번 맞을 때 여러 소리가 납니다 |
| `ObjectVisual/Item/ObjectVisual_Mine`·`_Flag`·`_SheriffBadge` | `objectSoundPreset` | 오브젝트마다 발동·습득·생성 소리(한 번호를 두 슬롯이 같이 씀) |
| `ConsumableItemVisual/Item/ConsumableItemVisual_Lantern`·`_Roket`·`_Rope`·`_Whiskey` | `itemSoundPreset` | 아이템마다 발동 소리 |
| `TerrainVisual/Item/TornadoPresentation_Default` | `liftSoundPreset`·`landSoundPreset`·`warningSoundPreset` | 토네이도 휩쓸림·착지·방향 예고 |
| `RockfallVisual/Item/RockfallVisual_Default` | `warningSoundPreset`·`impactSoundPreset` | 낙석 경고·착지 |
| `HaboobVisual/HaboobVisual_Default` | `relocationSoundPreset` | 하붑 구름 이동 |

## 5. 타이밍을 바꾸는 곳

### 5.1 시점(Anchor) 이름

시점 이름(`Start`·`Event1`~`Event3`·`Contact`·`End`)의 뜻은 [이펙트·소리 창구 안내](2026-10-29-combat-effect-sound-cue-guide.md) §2.3에 있습니다. 옛 `Cue`는 `Event1`, 옛 `Hit`은 `Contact` + `contactFilter` FirstHit입니다(2026-11-17). 소리는 `Contact`·Every를 쓸 수 없습니다.

`delay`는 그 시점에서 몇 초 뒤에 낼지입니다(0 이상).

> **주의:** 시점 칸으로 내는 소리(공격·사망·피격·투사체)는 연출의 한 항목입니다. 연출은 모든 항목이 끝나야 끝나므로, 지연을 길게 주면 그만큼 다음 행동이 늦게 시작할 수 있습니다. 소리만 늦게 시작하고 싶으면 FMOD 이벤트 안의 앞 여백으로 맞추는 방법도 있습니다.

### 5.2 공격 소리 — 유닛 외형 `attackSounds` 줄

- 지금 값은 §4.1 표입니다(셰인만 Start, 나머지 Event1, 지연 0).
- 쓸 수 있는 시점: Start · Event1 · Event2·Event3(그 유닛 공격 클립에 `ClipEvent2`·`ClipEvent3` 표식이 있을 때만) · Contact + FirstHit(첫 실제 피격) · End.
- 공격 표시(공격 묶음)가 없는 외형은 사건 순간에만 나서 Start · 지연 0만 됩니다.
- 유닛마다 따로라 한 유닛만 바꿔도 다른 유닛에 영향이 없습니다(옛 방식은 공격 묶음을 같이 써서 복제가 필요했음).
- 스킬이 다른 공격 표시를 쓰면, 그 공격이 받지 못하는 시점의 줄은 Event1에 냅니다. 포효는 박치기 순간에 시점·지연 없이 냅니다.

### 5.3 사망 소리 — 유닛 외형 `deathSounds` 줄

- 지금 값은 모두 Start + 0이고, 쓸 수 있는 시점은 Start·End(사망 반응 기준)입니다.
- 반응 묶음이 없는 유닛은 사건 순간에만 나서 Start · 지연 0만 됩니다. 낭떠러지 추락 사망·깜빡임 없는 자폭 사망은 줄의 시점·지연을 쓰지 않고 시작 순간에 냅니다.

### 5.4 피격 소리 — 유닛 외형 `hitSounds` 줄(슬롯 대기)

- 줄마다 `sound`(번호, 0 이상), `anchor`, `delay`가 있습니다(자리 칸은 없앴습니다 — 효과음이 위치 없이 나서).
- 쓸 수 있는 시점은 Start · Event1~Event3 · End입니다. Event1~3은 그 유닛 피격 클립에 그 표식이 있을 때만 됩니다.
- 반응 묶음(`reactionPresentation`)이 없는 외형에는 넣을 수 없습니다(피격 연출이 없어 소리가 나지 않으므로 Play 시작 때 멈춤). 지금 캐릭터 외형은 모두 반응 묶음이 있습니다.

### 5.5 투사체 — 투사체 부품 `CombatProjectile_*`

- `flightSoundDelay`: 발사 순간 뒤 몇 초에 비행 소리를 낼지입니다. 발사마다 한 번 납니다.
- `impactSoundDelay`: 맞는 순간 뒤 몇 초에 착탄 소리를 낼지입니다. 곡사(포탄)는 착지, 직선(총알)은 몸에 닿는 순간이 "맞는 순간"입니다.

### 5.6 코드에 고정된 시점(데이터로 바꿀 수 없음)

- 해당 소리: 집기·취소, 걸음(한 칸 시작), 포효, 추락 사망, 배경음 전환, 승리, 사건 순간에 나는 소리(기차·토네이도 발동·장애물·오브젝트·추락·약한 지반·아이템), 토네이도 휩쓸림·착지.
- 이 소리들의 시점을 데이터로 바꾸려면 칸이 새로 필요합니다(전투 View에 요청).
- 칸 없이 맞추려면 FMOD 이벤트 안에서 시작을 늦추는 방법이 있습니다.

## 6. 슬롯이 아직 없는 소리 — 만들어 주실 것

코드 쪽 연결은 끝나 있습니다. 슬롯이 생기면 전투 코드의 소리 표(`CombatEventSounds`)에서 그 소리 줄 하나만 채우면 됩니다. 슬롯이 비어 있는 동안에는 DB에 번호를 넣어도 소리가 나지 않고 오류도 나지 않습니다.

| 소리 | 제안 이름 | 변종(프리셋) |
|---|---|---|
| 투사체 비행 | `ProjectileFlight` | 투사체 넷(Ballista 화살·Bullet 총알·CactusCannon·UndeadCannon 포탄) |
| 투사체 착탄 | `ProjectileImpact` | 투사체 넷 |
| 피격 | `UnitHit` | 카테고리(또는 유닛)마다 하나. 유닛 외형 `hitSounds` 줄이 번호를 고릅니다 |
| 오브젝트 발동·습득·생성 | `ObjectActivated`·`ObjectCreated` | 지뢰·깃발·배지마다 하나. **발동·습득은 이미 있는 SFX 12 `ObjectActive`(파라미터 Object = Mine·Badge·Flag)를 쓰면 됩니다.** 그 슬롯에 프리셋 셋을 만들고, 코드 열거에 12를 넣어 주시면 됩니다 |
| ~~낭떠러지 추락~~ | — | 필요 없음: Unit `Fall` 칸으로 이미 남(현강님 11-10 연결, 2026-11-12 정정) |
| ~~약한 지반 붕괴~~ | — | 필요 없음: SFX `WeakTerrian`(프리셋 CrackLv 0·1·2)으로 이미 남. 진입도 같은 칸 |
| 토네이도 휩쓸림·착지·방향 예고 | `TornadoLift`·`TornadoLand`·`TornadoWarning` | 하나씩 |
| 낙석 경고·착지 | `RockfallWarning`·`RockfallImpact` | 하나씩 |
| 하붑 구름 이동 | `HaboobRelocation` | 하나 |
| 아이템 발동 | `ItemActivated` | 아이템 넷(랜턴·로켓·로프·위스키)마다 하나 |

**순서**

1. Boot SFX 플레이어에 슬롯을 만들고 FMOD 이벤트를 연결합니다. SFX 순번 0~12는 이미 쓰이고 있어서 새 슬롯은 13부터입니다. 7~9는 Artifact, 12는 ObjectActive입니다.
2. 변종이 필요한 슬롯에는 Parameter Presets를 만듭니다. 목록 순번이 DB에 넣을 번호입니다.
3. `AudioSystem.cs`의 `SfxEvent`에 값을 넣습니다. 값은 Boot 슬롯 순번과 같아야 합니다.
4. 전투 View에 알려 주세요. 소리 표의 연결 칸을 채웁니다. DB 번호는 인스펙터에서 직접 넣으셔도 됩니다(§4).

## 7. 데이터에서 본 것(확인 부탁)

- **`unitSoundPreset` 0과 1은 둘 다 Base입니다.** 에블린이 0, 로즈가 1을 씁니다.
- **`UnitCancel`에는 Enemy(5) 프리셋이 없습니다.** 적은 취소 소리를 내지 않으므로 지금은 문제가 없습니다.
- **아이비**(`UnitVisual_Shielder`)와 **MainCharacter**는 `unitSoundPreset`이 -1이고 공격·사망 줄도 없어 집기·걸음·공격·사망 소리가 나지 않습니다.
- **광부 변형 셋**(`Demon_AnkleBreaker`·`Demon_SoloAction`·`lqwe`)은 옛 공격음 칸이 비어 있어 공격 소리가 없습니다(광부 본체는 9). 넣으려면 `attackSounds`에 줄 하나를 더하면 됩니다.
- SFX 7~9(Artifact)는 Boot에 있지만 코드 열거에 없고, 부르는 곳도 없습니다.
- SFX 12(ObjectActive)는 프리셋이 0개입니다. 오브젝트 외형에 번호를 넣기 전에 프리셋부터 있어야 합니다. 프리셋 없이 번호를 넣으면 재생 때 오류가 납니다(§1 주의).

## 8. 프로그래머용 위치(참고)

| 무엇 | 파일 |
|---|---|
| 슬롯 열거 | `Script/Systems/RuntimeSystem/GlobalSystem/AudioSystem/AudioSystem.cs` (`AmbgmEvent`·`UnitEvent`·`SfxEvent`) |
| 사건 → 슬롯·번호(소리 표), 유닛 소리 줄의 칸 → 슬롯(`UnitSound`), 연결 대기 칸 | `Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Playback/Support/CombatEventSounds.cs` |
| 번호를 DB에서 읽는 곳(유닛 소리 줄 보관 `UnitSounds`) | 같은 폴더 `CombatSoundPresets.cs` |
| 유닛 소리 줄 형식·검사 | `Script/Systems/DataBaseSystem/Data/View/CombatPresentation/CombatUnitSounds.cs`, 검사 `CombatPresentationCueRules.FindUnitSoundError` |
| 실제 재생 입구 | 같은 폴더 `CombatEventAudio.cs`(공격·포효는 `AudioSystem.PlayUnitAttack`), 입력 `Input/Interaction/CombatInputSounds.cs` |
| 시점으로 소리 항목을 붙이는 곳 | `Presentation/Pieces/CombatCueComposer.cs` |
| 관련 설계 | [투사체·피격 소리](../specs/2026-10-27-combat-projectile-hit-sound-design.md) · [소리 창구 2차](../specs/2026-10-27-combat-environment-item-sound-design.md) · [유닛 소리를 유닛마다](../specs/2026-11-17-combat-unit-sound-per-unit-design.md) |
