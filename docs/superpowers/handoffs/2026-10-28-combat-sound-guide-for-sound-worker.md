# 전투 소리 현황·조절 안내 — 공격·포효 슬롯 갱신 (2026-11-10, 미전달)

> 보내는 사람: 용근(전투 View). 받는 사람: 사운드 작업자(FMOD·AudioSystem 담당).
> 공격·포효는 2026-11-10 커밋 `686900826`의 `UnitAttack` 플레이어와 유닛 외형 에셋을 기준으로 갱신했습니다. 그 밖의 소리 안내는 2026-10-29 기준이며, 최신 슬롯 설정은 `Assets/Scenes/Boot.unity`에서 확인합니다.
> 이펙트·흔들림 칸과 사건마다 넣는 칸의 지도는 [이펙트·소리 창구 안내](2026-10-29-combat-effect-sound-cue-guide.md)에 있습니다. 슬롯 배치의 원본은 Boot 씬이고, 유닛별 공격 슬롯 번호의 원본은 유닛 외형 에셋입니다.
> 이 문서는 "지금 어떤 소리가, 언제, 어떤 번호로 나는지"와 "번호·타이밍을 바꾸려면 어디를 고치는지"를 모은 안내입니다. 새 슬롯 요청은 [슬롯 요청 메모](2026-10-27-combat-projectile-hit-sound-audio-request.md)에 따로 있습니다.

## 1. 소리가 나는 길

```text
FMOD 이벤트
  └ Boot 씬의 FMOD 플레이어(AMBGM·Unit·SFX)의 슬롯 ─ 슬롯 순번 = 코드 열거 값(AmbgmEvent·UnitEvent·SfxEvent)
       └ 슬롯의 Parameter Presets 목록 ─ 목록 순번(0부터) = "프리셋 번호"
전투 코드: 일반 효과음 → AudioSystem.Play(슬롯, 프리셋 번호)
공격·포효: 유닛 외형의 attackSoundSlotIndex → AudioSystem.PlayUnitAttack(슬롯 번호)
```

- **소리의 내용**(어떤 파일·변종)은 FMOD 이벤트와 Boot 슬롯의 프리셋이 정합니다.
- **어떤 번호를 쓸지**는 전투 DB의 SO 칸이 정합니다(§4).
- **언제 낼지**는 일부는 DB의 시점 칸(§5.1~§5.5), 나머지는 코드에 고정돼 있습니다(§5.6).
- **-1의 뜻이 두 가지**입니다.
  - DB 칸의 -1은 "소리를 내지 않음"입니다(우리 쪽 규칙).
  - `AudioSystem.Play`의 프리셋 인자 -1은 현재 프리셋을 유지하고 재생한다는 뜻입니다. `PlayUnitAttack`은 선택한 슬롯의 설정으로 재생합니다.
- DB는 `-1` 또는 `0` 이상인지 검사합니다. 공격음 슬롯 범위와 이벤트 연결은 재생할 때 `AudioSystem`이 검사하며, 잘못된 구성은 예외로 보고합니다.

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

### 2.3 UnitAttack 플레이어 (2026-11-10 기준)

`attackSoundSlotIndex`는 Boot의 `UnitAttack` 플레이어에서 0부터 센 이벤트 슬롯 순번입니다. 슬롯마다 FMOD 이벤트 하나를 연결합니다. 슬롯 순서를 바꾸면 그 슬롯을 가리키는 유닛 외형 값도 함께 갱신합니다.

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
| 공격 | `UnitAttack` 플레이어 | 공격 묶음의 소리 시점 | 공격한 유닛의 `attackSoundSlotIndex` | 공격 묶음 `cues.sound` | 나옴 |
| 보스 포효 | `UnitAttack` 플레이어 | 포효 박치기 순간 | 포효한 유닛의 `attackSoundSlotIndex` | 코드 고정 | 나옴 |
| 사망 | `UnitDead` | 반응 묶음의 사망 소리 시점 | 아군 = 유닛 외형 `unitSoundPreset`, 적 = 설정 표 `enemyUnitSoundPreset` | 반응 묶음 `deathCues.sound` | 나옴 |
| 낭떠러지 추락 사망 | `UnitDead` | 가라앉기 시작 | 사망과 같음 | 코드 고정(사망 시점 칸을 쓰지 않음) | 나옴 |
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
| 피격 | (대기) `UnitHit` | 반응 묶음 피격 소리 항목의 시점(죽지 않을 때) | 반응 묶음 `hitCues.presetSounds`의 항목 | 같은 항목의 시점·지연 | 슬롯 대기 |
| 오브젝트 발동·습득 | (대기) `ObjectActivated` | 유닛이 오브젝트 칸에 들어선 순간. 지뢰 = 발동, 깃발·보안관 배지 = 습득 | 오브젝트 외형 `objectSoundPreset` | 코드 고정 | 슬롯 대기 |
| 오브젝트 생성 | (대기) `ObjectCreated` | 전투 중 오브젝트가 생긴 순간 | 같은 `objectSoundPreset` | 코드 고정 | 슬롯 대기. Core가 이 사건을 아직 내지 않습니다 |
| 낭떠러지 추락 | (대기) `Fall` | 유닛·구조물이 떨어지기 시작하는 순간 | 설정 표 `fallSoundPreset` | 코드 고정 | 슬롯 대기 |
| 약한 지반 붕괴 | (대기) `GroundCollapse` | 약한 지반이 낭떠러지로 바뀌는 순간 | 설정 표 `weakGroundCollapseSoundPreset` | 코드 고정 | 슬롯 대기 |
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

에셋 경로는 모두 `Assets/ProjectL/DataBaseSO/ViewGroup/` 아래입니다. 인스펙터에서 바로 고치면 됩니다. Play 중에 고친 값은 칸에 따라 다음 전투부터 반영되기도 하고, Play를 다시 시작해야 반영되기도 합니다. 확실히 하려면 Play를 다시 시작하세요. 일반 효과음에는 해당 슬롯의 프리셋 번호를 넣고, 공격·포효에는 §2.3의 `UnitAttack` 슬롯 순번을 넣습니다.

### 4.1 유닛 외형 `UnitVisual/Item/…/UnitVisual_*.asset`

- `unitSoundPreset`: 아군의 집기·취소·걸음·사망 소리 번호입니다. 장애물 외형이면 피해·파괴 번호입니다.
- `attackSoundSlotIndex`는 공격·포효가 사용할 `UnitAttack` 슬롯 순번입니다. `-1`이면 공격·포효 소리를 생략합니다.
- **적은 `unitSoundPreset` 대신 설정 표의 `enemyUnitSoundPreset`(지금 5 = Enemy) 하나를 씁니다.** 그래서 적 외형의 `unitSoundPreset` 값은 지금 쓰이지 않습니다. 적의 공격은 각 외형의 `attackSoundSlotIndex`를 씁니다.

| 에셋 | 이름 | `unitSoundPreset` | `attackSoundSlotIndex` |
|---|---|---|---|
| `Playerable/UnitVisual_Akimbo` | 에블린 | 0 (Base) | 0 |
| `Playerable/UnitVisual_ShotGun` | 와이엇 | 3 (Heavy) | 6 |
| `Playerable/UnitVisual_Sniper` | 로즈 | 1 (Base) | 7 |
| `Playerable/UnitVisual_Ballista` | 셰인 | 4 (Cannon) | 1 |
| `Playerable/UnitVisual_CactusCannon` | 조세핀 | 4 (Cannon) | 2 |
| `Playerable/UnitVisual_Cowboy` | 웨이드 | 2 (Rider) | 3 |
| `Playerable/UnitVisual_Boomber` | — | 2 (Rider) | 5 |
| `Playerable/UnitVisual_Shielder` | 아이비 | -1 (없음) | -1 (없음) |
| `Playerable/UnitVisual_MainCharacter` | MainCharacter | -1 (없음) | -1 (없음) |
| `Enemy/UnitVisual_Demon` | 언데드 광부 | (적 공용) | 9 |
| `Enemy/UnitVisual_Gravekeeper` | 언데드 무덤지기 | (적 공용) | 11 |
| `Enemy/UnitVisual_Cannon` | 언데드 포병 | (적 공용) | 8 |
| `Enemy/UnitVisual_Heavy` | 언데드 마인카트 | (적 공용) | 10 |
| `Enemy/UnitVisual_StageBoss1` | 보스 | (적 공용) | 12 |
| `Enemy/UnitVisual_Headquarters` | 적 넥서스 | -1 | -1 |
| `Object/UnitVisual_PowderBarrel` | 화약통 | -1 (프리셋 없이) | -1 |
| `Object/UnitVisual_Supplies` | 보급품 | -1 (프리셋 없이) | -1 |

### 4.2 전투 연출 설정 표 `CombatPresentationSettings/Item/CombatPresentationSettings_Default.asset`

"전투 소리 프리셋" 묶음에 있습니다.

| 칸 | 지금 값 | 뜻 |
|---|---|---|
| `enemyUnitSoundPreset` | 5 (Enemy) | 모든 적의 집기·걸음·사망 소리 |
| `nightBgmPreset` | 3 (Night) | 밤(전투) 배경음 |
| `dayBgmPreset` | 2 (Day) | 낮(배치) 배경음 |
| `afterCompletionBgmPreset` | 1 (StageSelect) | 결과 확인 뒤 배경음 |
| `fallSoundPreset` | -1 | 낭떠러지 추락(유닛·구조물 공용, 슬롯 대기) |
| `weakGroundCollapseSoundPreset` | -1 | 약한 지반 붕괴(슬롯 대기) |

### 4.3 그 밖의 칸(모두 슬롯 대기, 지금 값 -1)

| 에셋 | 칸 | 뜻 |
|---|---|---|
| `CombatPresentation/Projectile/CombatProjectile_Ballista`·`_Bullet`·`_CactusCannon`·`_UndeadCannon` | `impactSoundPreset`·`flightSoundPreset` | 투사체마다 착탄·비행 소리 |
| `CombatPresentation/HitSetting/CombatUnitReaction_Default` | `hitCues.presetSounds`(목록, 지금 비어 있음) | 피격 소리. 모든 캐릭터가 이 반응 묶음 하나를 같이 씁니다. 항목을 여럿 넣으면 한 번 맞을 때 여러 소리가 납니다 |
| `ObjectVisual/Item/ObjectVisual_Mine`·`_Flag`·`_SheriffBadge` | `objectSoundPreset` | 오브젝트마다 발동·습득·생성 소리(한 번호를 두 슬롯이 같이 씀) |
| `ConsumableItemVisual/Item/ConsumableItemVisual_Lantern`·`_Roket`·`_Rope`·`_Whiskey` | `itemSoundPreset` | 아이템마다 발동 소리 |
| `TerrainVisual/Item/TornadoPresentation_Default` | `liftSoundPreset`·`landSoundPreset`·`warningSoundPreset` | 토네이도 휩쓸림·착지·방향 예고 |
| `RockfallVisual/Item/RockfallVisual_Default` | `warningSoundPreset`·`impactSoundPreset` | 낙석 경고·착지 |
| `HaboobVisual/HaboobVisual_Default` | `relocationSoundPreset` | 하붑 구름 이동 |

## 5. 타이밍을 바꾸는 곳

### 5.1 시점(Anchor) 이름

시점 이름(`Start`·`Cue`·`Hit`·`Contact`·`End`)의 뜻은 [이펙트·소리 창구 안내](2026-10-29-combat-effect-sound-cue-guide.md) §2.3에 있습니다. 소리는 `Contact`를 쓸 수 없습니다.

`delay`는 그 시점에서 몇 초 뒤에 낼지입니다(0 이상).

> **주의:** 시점 칸으로 내는 소리(공격·사망·피격·투사체)는 연출의 한 항목입니다. 연출은 모든 항목이 끝나야 끝나므로, 지연을 길게 주면 그만큼 다음 행동이 늦게 시작할 수 있습니다. 소리만 늦게 시작하고 싶으면 FMOD 이벤트 안의 앞 여백으로 맞추는 방법도 있습니다.

### 5.2 공격 소리 — 공격 묶음 `CombatPresentation/AttackSetting/CombatAttack_*.asset`의 `cues` → `sound`

| 공격 묶음 | 쓰는 유닛 | 지금 시점 + 지연 |
|---|---|---|
| `CombatAttack_Linear` | 에블린·와이엇·로즈·MainCharacter | Cue + 0 |
| `CombatAttack_Melee` | 언데드 광부·무덤지기·마인카트·보스·웨이드·아이비 | Cue + 0 |
| `CombatAttack_CactusCannon` | 조세핀 | Cue + 0 |
| `CombatAttack_UndeadCannon` | 언데드 포병 | Cue + 0 |
| `CombatAttack_Ballista` | 셰인 | **Start** + 0 |

- 쓸 수 있는 시점: Start·Cue·Hit·End.
- 묶음 하나를 여러 유닛이 같이 쓰므로, 고치면 그 유닛 모두에 적용됩니다. 한 유닛만 바꾸려면 묶음을 복제해 그 유닛 외형에 연결해야 합니다(프로그래머와 상의).

### 5.3 사망 소리 — 반응 묶음 `CombatPresentation/HitSetting/CombatUnitReaction_Default`의 `deathCues` → `sound`

- 지금 값은 Start + 0이고, 쓸 수 있는 시점은 Start·End입니다.
- 모든 캐릭터가 같이 씁니다. 낭떠러지 추락 사망은 이 칸을 쓰지 않고 가라앉기 시작에 납니다.

### 5.4 피격 소리 — 같은 반응 묶음의 `hitCues` → `presetSounds` 항목(슬롯 대기)

- 항목마다 `preset`(번호, 0 이상), `anchor`, `delay`, `point`가 있습니다.
- 쓸 수 있는 시점은 Start·Cue·End입니다. Cue는 피격 클립이 있는 유닛만 쓸 수 있습니다.
- `point`(소리 자리)는 지금 효과가 없습니다. 효과음이 위치 없이 나기 때문입니다.

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
| 피격 | `UnitHit` | 처음엔 하나면 됩니다(반응 묶음이 하나) |
| 오브젝트 발동·습득·생성 | `ObjectActivated`·`ObjectCreated` | 지뢰·깃발·배지마다 하나. **발동·습득은 이미 있는 SFX 12 `ObjectActive`(파라미터 Object = Mine·Badge·Flag)를 쓰면 됩니다.** 그 슬롯에 프리셋 셋을 만들고, 코드 열거에 12를 넣어 주시면 됩니다 |
| 낭떠러지 추락 | `Fall` | 하나 |
| 약한 지반 붕괴 | `GroundCollapse` | 하나 |
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
- **아이비**(`UnitVisual_Shielder`)와 **MainCharacter**는 두 번호가 모두 -1이라 집기·걸음·공격·사망 소리가 나지 않습니다.
- SFX 7~9(Artifact)는 Boot에 있지만 코드 열거에 없고, 부르는 곳도 없습니다.
- SFX 12(ObjectActive)는 프리셋이 0개입니다. 오브젝트 외형에 번호를 넣기 전에 프리셋부터 있어야 합니다. 프리셋 없이 번호를 넣으면 재생 때 오류가 납니다(§1 주의).

## 8. 프로그래머용 위치(참고)

| 무엇 | 파일 |
|---|---|
| 슬롯 열거 | `Script/Systems/RuntimeSystem/GlobalSystem/AudioSystem/AudioSystem.cs` (`AmbgmEvent`·`UnitEvent`·`SfxEvent`) |
| 사건 → 슬롯·번호(소리 표), 연결 대기 칸 | `Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Playback/Support/CombatEventSounds.cs` |
| 번호를 DB에서 읽는 곳 | 같은 폴더 `CombatSoundPresets.cs` |
| 실제 재생 입구 | 같은 폴더 `CombatEventAudio.cs`(공격·포효는 `AudioSystem.PlayUnitAttack`), 입력 `Input/Interaction/CombatInputSounds.cs` |
| 시점 칸으로 소리 항목을 붙이는 곳 | `Presentation/Pieces/CombatCueComposer.cs` |
| 관련 설계 | [투사체·피격 소리](../specs/2026-10-27-combat-projectile-hit-sound-design.md) · [소리 창구 2차](../specs/2026-10-27-combat-environment-item-sound-design.md) |
