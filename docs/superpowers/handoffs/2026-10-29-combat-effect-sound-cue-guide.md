# 전투 이펙트·소리 창구 안내 (2026-10-29, 2026-11-01 이펙트 구조 2판 반영, 미전달)

> 보내는 사람: 용근(전투 View). 받는 사람: 전투 연출(이펙트·흔들림)을 넣는 사람, 사운드 작업자.
> 기준: 사용자 커밋 `efa5e7d7`(이펙트 창구 확장 + 소리 창구 2차).
> 이 문서는 **어느 사건의 연출을 어느 에셋의 어느 칸에 넣는지**(§1)와 **이펙트·흔들림을 넣는 규칙**(§2~§3)을 모은 안내입니다.
> **소리**의 슬롯·프리셋 번호·시점 값은 [사운드 작업자 안내](2026-10-28-combat-sound-guide-for-sound-worker.md)가 원본입니다. 이 문서는 소리 칸의 위치만 적고 그쪽 절을 가리킵니다.

## 1. 한눈에: 사건마다 넣는 칸

에셋 경로는 모두 `Assets/ProjectL/DataBaseSO/ViewGroup/` 아래입니다. "시점"·"자리"는 그 칸에서 고를 수 있는 값입니다(뜻은 §2.3·§2.4). 표에 없는 값을 넣으면 Play 시작 때 멈춥니다(§2.8).

### 1.1 공격·피격·사망(유닛 연출)

| 사건 | 이펙트·흔들림 칸 | 시점 | 이펙트 자리 | 소리 번호 칸 | 소리 시점 칸 |
|---|---|---|---|---|---|
| 근접 공격 | 공격 묶음 `CombatPresentation/AttackSetting/CombatAttack_*`의 `cues`(effects·shakes) | Start·Cue·Hit·End | ActorEffectPoint·ActorBody | 유닛 외형 `attackSoundPreset` | 같은 `cues`의 `sound` |
| 원거리 공격 | 같은 칸 | Start·Cue·Hit·End + Contact(이펙트만) | ActorEffectPoint·ActorMuzzle·ActorBody, Contact 시점이면 Contact만 | 같음 | 같음 |
| 투사체 비행·착탄 | 착탄 이펙트는 위 원거리 공격의 Contact 이펙트 | — | — | 투사체 부품 `CombatPresentation/Projectile/CombatProjectile_*`의 `flightSoundPreset`·`impactSoundPreset`(슬롯 대기) | 같은 부품의 `flightSoundDelay`·`impactSoundDelay` |
| 피격(죽지 않을 때) | 반응 묶음 `CombatPresentation/HitSetting/CombatUnitReaction_Default`의 `hitCues`(effects·shakes·flash) | Start·Cue(피격 클립 필요)·End | SubjectBody | 같은 `hitCues`의 `presetSounds` 항목(슬롯 대기) | 항목마다 |
| 사망 | 같은 반응 묶음의 `deathCues`(effects·shakes) | Start·End | SubjectBody | 아군 = 유닛 외형 `unitSoundPreset`, 적 = 설정 표 `enemyUnitSoundPreset` | 같은 `deathCues`의 `sound` |

- 공격 묶음은 여러 유닛이 같이 씁니다(어느 유닛이 어느 묶음인지는 §2.5 표, 또는 [사운드 안내](2026-10-28-combat-sound-guide-for-sound-worker.md) §5.2). 묶음을 고치면 그 유닛 모두에 적용됩니다.
- 반응 묶음은 지금 모든 캐릭터가 하나를 같이 씁니다.
- 낭떠러지 추락 사망은 사망 칸(`deathCues`)을 쓰지 않습니다. 가라앉기·작아지기 연출과 사망 소리만 납니다.

### 1.2 필드 사건(사실 사건)

이 칸들은 모두 **이펙트·흔들림만** 받고, **시점은 Start(사건 순간)만** 됩니다. 소리는 소리 표가 사건 순간에 냅니다(시점 칸 없음).

| 사건 | 이펙트·흔들림 칸 | 이펙트 자리 | 소리 번호 칸 |
|---|---|---|---|
| 오브젝트 발동(지뢰)·습득(깃발·보안관 배지) | 오브젝트 외형 `ObjectVisual/Item/ObjectVisual_*`의 `activationCues` | EventCell | 같은 외형의 `objectSoundPreset`(슬롯 대기) |
| 오브젝트 생성 | 칸 없음 | — | 같은 `objectSoundPreset`(슬롯 대기, Core가 이 사건을 아직 내지 않음) |
| 장애물 피해(화약통·보급품) | 장애물 외형 `UnitVisual/Item/Object/UnitVisual_*`의 `obstacleHitCues` | EventCell | 같은 외형의 `unitSoundPreset`(-1이면 프리셋 없이, 나옴) |
| 장애물 파괴 | 같은 외형의 `obstacleDestroyedCues` | EventCell | 같음 |
| 낭떠러지 추락(유닛·구조물) | 설정 표 `CombatPresentationSettings/Item/CombatPresentationSettings_Default`의 `fallCues` | EventCell | 같은 설정 표의 `fallSoundPreset`(슬롯 대기) |
| 약한 지반 붕괴 | 같은 설정 표의 `weakGroundCollapseCues` | EventCell | 같은 설정 표의 `weakGroundCollapseSoundPreset`(슬롯 대기) |
| 아이템 발동 | 아이템 외형 `ConsumableItemVisual/Item/ConsumableItemVisual_*`의 `activationCues` | SubjectBody(소지자) | 같은 외형의 `itemSoundPreset`(슬롯 대기) |

- 장애물 큐 칸은 유닛 외형 SO에 있지만 **장애물 외형만** 받습니다. 캐릭터 외형에 넣으면 Play 시작 때 멈춥니다.

### 1.3 토네이도·기차·카메라

| 사건 | 칸 | 시점·자리 | 소리 |
|---|---|---|---|
| 토네이도 휩쓸림·착지 | 토네이도 연출 설정 `TerrainVisual/Item/TornadoPresentation_Default`의 `cues`(effects·shakes) | Start = 휩쓸리는 순간, End = 내려앉은 순간. 자리는 SubjectBody(그 순간 서 있는 몸). **지연은 0만** | 같은 설정의 `liftSoundPreset`·`landSoundPreset`(슬롯 대기). 발동 소리는 `Tornado` 슬롯(프리셋 없이, 나옴) |
| 낙석 경고·착지 | 낙석 설정 `RockfallVisual/Item/RockfallVisual_Default`의 `warningCues`·`impactCues` | 경고 = Start, 자리는 맵 프리팹의 `RockfallTunnelBoom`(없으면 안 나옴). 착지 = End, 자리는 그 칸. 둘 다 EventCell | 같은 설정의 `warningSoundPreset`·`impactSoundPreset`(슬롯 대기) |
| 하붑 구름 | 하붑 설정 `HaboobVisual/HaboobVisual_Default`의 `cloudEffect`·`cloudPositionOffset`·`cloudRotationOffset` | 구름 칸마다 켜 둡니다. 큐가 아니라 시점·지연·끝 칸이 없습니다. 칸 표면이 기준이고, 빠진 칸은 방출을 멈춰 서서히 사라집니다 | 하붑 설정 `relocationSoundPreset`(슬롯 대기) |
| 기차 통과 | 레일 외형 `RailwayVisual/Item/RailwayVisual_Default`의 `effect` 하나 | 통과를 시작할 때 열차마다 하나. 열차를 따라 움직입니다 | `TrainCall`(프리셋 없이, 나옴) |
| 보스 포효 카메라 | 설정 표 "카메라 연출" `roarZoomRatio`·`roarShake*` | 시점은 코드 고정(박치기 순간) | 공격 소리(보스 외형 `attackSoundPreset`) |
| 기차 카메라 | 설정 표 "카메라 연출" `trainShake*`·`trainZoom*` | 시점은 코드 고정(앞면이 필드에 들어올 때·나갈 때) | 위와 같음 |

- 토네이도의 지연이 0이어야 하는 이유: 착지 칸은 들어 올림이 끝나는 순간 그립니다. 그 뒤에 남는 큐가 있으면 유닛이 휩쓸리기 전 칸으로 되돌아가 보입니다.
- 기차 이펙트 칸은 큐 형식이 아니라 종류 하나만 고릅니다(시점·보정 없음). 지금 값은 Empty(없음)입니다.

### 1.4 이 안내에 없는 소리

집기·취소·걸음·배경음·승리·UI 소리는 이펙트 칸이 없고 시점도 코드에 고정돼 있습니다. 목록과 번호는 [사운드 안내 §3](2026-10-28-combat-sound-guide-for-sound-worker.md#3-전투-소리-목록)에 있습니다.

## 2. 이펙트·흔들림 넣는 법

### 2.1 이펙트 종류(Render DB)

| 값 | 이름 | 프리팹 | 상태 |
|---|---|---|---|
| 0 | `Empty` | — | 이펙트 없음. 큐 항목에는 넣을 수 없습니다(항목을 지우세요) |
| 1 | `TestFire` | — | **Render 표에 등록돼 있지 않습니다.** 넣으면 Play 시작은 지나가고, 그 이펙트를 처음 낼 때 오류로 멈춥니다 |
| 2 | `Boom` | `Script/Render/Effect/Boom/BOOM.prefab` | 등록됨. 착탄·지뢰·화약통이 씁니다 |
| 3 | `GunFire` | `Script/Render/Effect/MuzzleFlash/MuzzleFlash_Toon.prefab` | 등록됨. 아직 아무 칸도 쓰지 않습니다 |
| 4 | `Hit` | `Script/Render/Effect/HitEffect/HitEffect.prefab` | 등록됨. 피격·사망이 씁니다 |
| 16 | `Smoke` | `Script/Render/Effect/EventEffect/SmokeEffect/Smoke.prefab` | 등록됨(풀 최대 32). 하붑 구름이 씁니다. **프리팹 레이어는 메인 카메라가 그리는 값이어야 보입니다.** 레이어 18(`Blending`)은 그리지 않아 0으로 바꿨습니다 |

**새 이펙트를 쓰려면** Render DB에 먼저 등록해야 합니다(Render DB는 민형님 영역이라 요청).

1. `DataBaseSystem/Data/Render/Table/Effect/Enum/EffectType.cs`에 값을 더합니다.
2. `DataBaseSO/RenderGroup/EffectTable/Items/`에 이펙트 항목 에셋을 만들고 그 값과 프리팹을 넣습니다.
3. `EffectDataTable.asset`의 목록에 그 항목을 넣습니다.
4. 그 뒤 위 §1의 칸에서 고릅니다.

**프리팹 조건**

- 자식 어딘가에 ParticleSystem이 있어야 합니다. 없으면 처음 낼 때 오류입니다.
- 끝은 파티클이 스스로 멈출 때 풀로 돌아갑니다. **반복(Loop) 파티클은 스스로 멈추지 않으므로** 결과 재생이 취소되거나 전투가 끝날 때까지 남습니다. 큐에는 반복하지 않는 파티클을 쓰세요.

### 2.2 이펙트 큐 항목의 칸

| 칸 | 뜻 |
|---|---|
| `effect` | 낼 이펙트 종류(§2.1) |
| `anchor` | 기준 시점(§2.3) |
| `delay` | 기준 시점 뒤 몇 초에 낼지(0 이상). Contact 시점과 토네이도 칸은 0만 됩니다 |
| `point` | 낼 자리(§2.4) |
| `positionOffset` | 자리 기준 위치 보정입니다. x = 오른쪽, y = 위, z = 그 자리의 앞(§2.4의 "앞"). **월드 단위**이고 필드 배율을 따르지 않습니다 |
| `rotationOffset` | 자리 회전 뒤에 더하는 회전(오일러 각, 도)입니다. 예: 위로 뿜는 이펙트를 바닥에 눕히려면 (-90, 0, 0) |
| `stayInPlace` | 켜면 몸 자리 이펙트도 낸 자리에 머뭅니다. 끄면(기본) 몸 자리(ActorBody·SubjectBody) 이펙트가 그 유닛의 위치를 따라갑니다(2026-10-31) |
| `end` | 언제 끝나나(2026-11-01): Natural(기본, 프리팹이 스스로 멈춤) · Seconds(`endSeconds` 뒤 거둠) · UnitAction(그 발동이 연 유닛 행동이 끝나면 거둠 — 유닛 행동이 있는 특성 발동 줄만, 대신 맞기 `DamageRedirected` 줄은 안 됨) |
| `endSeconds` | `end`가 Seconds일 때 거둘 초(0보다 큼) |

- 끝나지 않는 파티클(Looping·Ring Buffer)을 Natural로 내면 종류마다 한 번 콘솔 경고가 납니다. 그때는 Seconds를 고르거나, 오래 보일 이펙트면 §2.9의 자리에 넣습니다.
- 목록에 항목을 여럿 넣으면 모두 냅니다.

### 2.3 기준 시점(`anchor`)

| 시점 | 뜻 |
|---|---|
| `Start` | 그 연출이 시작하는 순간. 필드 사건 칸은 사건 순간, 토네이도는 휩쓸리는 순간 |
| `Cue` | 동작의 기준 순간. 클립이면 클립의 이벤트 타이밍, 코드 모션이면 근접 타격·원거리 발사 순간. 피격 칸은 피격 클립이 있는 유닛만 |
| `Hit` | 대상이 맞은 순간(공격 칸만) |
| `Contact` | 투사체가 닿는 순간(원거리 공격 칸의 이펙트만, 지연 0). 곡사는 착지, 직선은 몸에 닿는 순간 |
| `End` | 동작이 끝나는 순간(클립 끝·코드 모션 끝·반응 끝·토네이도 착지) |

소리 시점 칸도 같은 이름을 씁니다(소리는 Contact를 쓸 수 없음).

### 2.4 자리(`point`)

| 자리 | 위치 | 앞(z) 방향 | 쓰는 칸 |
|---|---|---|---|
| `ActorEffectPoint` | 공격자 프리팹의 이펙트 지점(`CombatAnimationEvents`의 effectPoint) | 그 지점의 회전 | 공격(조건 §2.5) |
| `ActorMuzzle` | 공격자 프리팹의 발사 지점(muzzle) | 공격자가 보는 방향(지점 회전은 안 씀) | 원거리 공격(조건 §2.5) |
| `ActorBody` | 공격자 몸 상자 가운데 | 공격자 모델 방향 | 공격 |
| `SubjectBody` | 그 사건 주체의 몸 상자 가운데 | 피격·사망 = 맞은 방향, 아이템 = 소지자가 서 있는 방향, 토네이도 = 그 순간 서 있는 방향 | 피격·사망·아이템·토네이도 |
| `Contact` | 투사체가 닿은 점 | 투사체가 날아온 방향(수평) | 원거리 공격의 Contact 시점만 |
| `EventCell` | 사건 칸의 바닥 표면 가운데(고지대·타일이면 그 높이) | 필드 방향 | 오브젝트·장애물·추락·약한 지반 |

y(위)는 모든 자리에서 필드 위쪽입니다. 앞 방향이 수평이기 때문입니다(`ActorEffectPoint`만 지점의 회전을 그대로 따름).

### 2.5 공격자 지점 자리를 쓸 수 있는 유닛

`ActorEffectPoint`·`ActorMuzzle`는 **공격 클립이 있고**, 프리팹의 `CombatAnimationEvents`에 그 지점이 지정된 유닛만 씁니다. 공격 묶음은 여러 유닛이 같이 쓰므로 **그 묶음을 쓰는 유닛이 모두** 조건을 갖춰야 합니다. 하나라도 못 갖추면 Play 시작 때 멈춥니다.

| 공격 묶음 | 쓰는 유닛(공격 클립 / 이펙트 지점 / 발사 지점) | 지점 자리 |
|---|---|---|
| `CombatAttack_Ballista` | 셰인(있음 / 있음 / 있음) | 둘 다 됨 |
| `CombatAttack_UndeadCannon` | 언데드 포병(있음 / 있음 / 있음) | 둘 다 됨 |
| `CombatAttack_Linear` | 에블린·MainCharacter·와이엇(있음 / 있음 / 있음), **로즈(클립 없음)** | 안 됨(로즈 때문) |
| `CombatAttack_CactusCannon` | 조세핀(클립 없음) | 안 됨 |
| `CombatAttack_Melee` | 언데드 광부·무덤지기·마인카트·보스·웨이드(클립 있음, 이펙트 지점 없음), 아이비(클립 없음) | 안 됨 |

- 안 되는 묶음에서는 `ActorBody`나 Contact를 쓰거나, 프로그래머가 묶음을 나눠야 합니다.
- **피격 클립은 지금 아무 유닛에도 없습니다.** 그래서 피격 칸의 `Cue` 시점은 지금 쓸 수 없습니다.

### 2.6 흔들림 큐 항목의 칸

| 칸 | 뜻 |
|---|---|
| `strength` | 세기(0이면 흔들림 없음) |
| `seconds` | 시간(초) |
| `shape` | 모양: 0 Custom · 1 Recoil · 2 Bump · 3 Explosion · 4 Rumble |
| `anchor`·`delay` | 이펙트와 같음(Contact는 안 됨) |

- 흔들림은 맵 카메라의 Impulse Listener가 받아야 보입니다. 전투 맵 프리팹(`Prefabs/Stage/MapProp/Stage`·`Stage_Test`·`Test/StageTest`)에는 붙어 있습니다.
- 같은 때 여러 흔들림이 겹치면 진행 중인 것보다 센 것만 새로 시작합니다.

### 2.7 피격 플래시(`hitCues.flash`)

- 모델이 `color`로 바뀌었다가 `seconds` 동안 원래 색으로 돌아옵니다. `seconds` 0이면 없습니다.
- 시점은 Start·Cue만 됩니다. 반응보다 길면 그 유닛의 다음 사건이 늦어집니다(플래시만 끝을 기다림).

### 2.8 지연과 기다림

- **유닛 연출 칸(공격·피격·사망)의 이펙트·흔들림·소리**는 그 연출의 항목입니다. 사건 진행은 막지 않지만, 연출은 모든 항목이 끝날 때까지 그 유닛을 쥐고 있습니다. 그래서 지연을 길게 주면 그 유닛의 다음 사건(과 주체 없는 사건)이 그만큼 늦게 시작할 수 있습니다. 플래시는 사건 진행도 기다립니다.
- **필드 사건 칸(§1.2)과 토네이도 칸**은 곁들임으로 냅니다. 사건 순서·시간을 막지 않습니다. 남은 지연이 있으면 기차 사건과 결과 끝만 그만큼 기다립니다.

### 2.9 오래 보이는 이펙트(조건이 맞는 동안, 2026-11-01)

큐가 아니라 아래 칸에 넣습니다. 끝은 조건이 정하고(켜지면 내고 꺼지면 거둠), HUD 아이콘과 같은 순간에 켜지고 꺼집니다. 반복(Loop)·링 버퍼 파티클이 맞습니다.

| 언제 | 넣는 곳 | 자리 |
|---|---|---|
| 상태(버프·디버프)가 걸려 있는 동안 | 상태 외형 `whileEffect`(+ 다시 내기 발동 `restartOn`) | 그 상태가 걸린 유닛 몸 |
| 특성을 가진 유닛이 필드에 있는 동안 | 특성 외형 `alwaysEffects` · 조건 WhileOwned | 보유 유닛 몸 |
| 보유 유닛의 공격 모드 동안, 조건을 채운 적마다(지금은 마무리 사격) | 특성 외형 `alwaysEffects` · 조건 AttackModeTarget | 그 적 몸 |

### 2.10 잘못 넣었을 때 언제 멈추나

| 무엇 | 언제 |
|---|---|
| 그 칸에서 못 쓰는 시점·자리, Contact·토네이도의 지연이 0이 아님, 큐 항목의 이펙트가 Empty | Play 시작(Boot의 DB 생성) |
| 지점 자리를 쓰는데 묶음을 쓰는 유닛이 조건을 못 갖춤(§2.5), 피격 Cue인데 피격 클립 없음 | Play 시작 |
| 캐릭터 외형에 장애물 큐를 넣음 | Play 시작 |
| Render 표에 없는 이펙트 종류(지금 `TestFire`), 파티클 없는 프리팹 | 그 이펙트를 처음 낼 때 |

Play 시작 때 멈추면 콘솔의 예외 문구에 에셋 이름(또는 키)과 이유가 나옵니다. 에셋을 Play 중에 고치면 다음 전투 진입이나 다음 Play부터 보입니다. 확실히 하려면 Play를 다시 시작하세요.

## 3. 지금 들어 있는 값

| 에셋 | 칸 | 값 |
|---|---|---|
| `CombatAttack_Ballista` | `cues` | 이펙트 Boom · Contact · Contact · 위치 보정 (0, **10**, 0). 소리 시점 Start |
| `CombatAttack_CactusCannon`·`CombatAttack_UndeadCannon` | `cues` | 이펙트 Boom · Contact · Contact. 소리 시점 Cue |
| `CombatAttack_Linear`·`CombatAttack_Melee` | `cues` | 이펙트·흔들림 없음. 소리 시점 Cue |
| `CombatUnitReaction_Default` | `hitCues` | 이펙트 Hit · Start · SubjectBody. 흔들림 세기 0.15 · 0.15초 · Bump · Start. 플래시 빨강 0.18초 · Start. 소리 항목 없음 |
| 같음 | `deathCues` | 이펙트 Hit · Start · SubjectBody. 흔들림 세기 0.3 · 0.3초 · Explosion · Start. 소리 시점 Start |
| `ObjectVisual_Mine` | `activationCues` | 이펙트 Boom · Start · EventCell · 회전 보정 (-90, 0, 0) |
| `UnitVisual_PowderBarrel` | `obstacleDestroyedCues` | 이펙트 Boom · Start · EventCell · 회전 보정 (-90, 0, 0). `obstacleHitCues`는 비어 있음 |
| 그 밖의 큐 칸 | — | 모두 비어 있습니다(깃발·배지·보급품·아이템 넷·추락·약한 지반·토네이도) |
| 특성 외형 발동 줄(2026-11-01) | `cues` | 정리표 순간 줄 15(모두 끝 Natural), 위치 교환 440 = 9 · SubjectBody · 끝 UnitAction. 값 목록은 [호진님 View DB 칸 메모](2026-11-01-combat-effect-structure-hojin-share.md) §2 |
| 상태 외형·특성 상시(2026-11-01) | `whileEffect`·`alwaysEffects` | 보호막 8(`restartOn` 240), 약점 노출 12, 희생 11·굳건한 의지 10(보유 동안) |
| `RailwayVisual_Default` | `effect` | Empty |
| 설정 표 "카메라 연출" | 포효·기차 | 포효 줌 0.85 · 흔들림 세기 1 · 1초 · Bump, 기차 흔들림 세기 1.01 · 1.5초 · Explosion · 줌 0.95(다가감 0.5초 · 돌아옴 0.4초) |

## 4. 확인 부탁(데이터에서 본 것)

- **Ballista 착탄 이펙트의 위치 보정이 y = 10입니다.** 월드 단위라 닿은 점에서 10만큼 위에 납니다. 다른 곡사 둘은 0입니다. 의도한 값인지 확인 부탁드립니다.
- **`GunFire`(머즐 플래시)는 등록돼 있지만 쓰는 칸이 없습니다.** 발사 지점에 붙이려면 §2.5대로 셰인·언데드 포병 묶음만 지금 바로 됩니다.
- **`TestFire`는 열거에만 있고 Render 표에 없습니다**(§2.1). 고르지 않도록 주의하세요.

## 5. 범위 밖(칸이 없는 것)

- 특성 연출(포효의 크기 변화·소환·가시·재생·끌어오기·넉백·표식), 진행·결과 화면, 걸음(뛰기) 착지에는 이펙트 칸이 없습니다.
- 소리 항목의 자리(`point`)는 지금 효과가 없습니다. 효과음이 위치 없이 나기 때문입니다.
- 칸이 더 필요하면 전투 View에 요청해 주세요.

## 6. 프로그래머용 위치(참고)

| 무엇 | 파일(`Script/Systems/` 아래) |
|---|---|
| 큐 칸·값 | `DataBaseSystem/Data/View/CombatPresentation/CombatEventCues.cs`·`CombatCueEntries.cs` |
| 시점·자리 열거 | 같은 폴더 `CombatCueAnchor.cs`·`CombatCuePoint.cs` |
| 칸마다 고를 수 있는 시점·자리(규칙 원본) | 같은 폴더 `CombatPresentationCueRules.cs` |
| 유닛 조건 검사 | 같은 폴더 `CombatUnitAnimationRules.cs` |
| 보정 적용·자리 해석 | `RuntimeSystem/InGameSystem/ViewSystem/Combat/Presentation/Pieces/CombatCuePoints.cs` |
| 유닛 연출 칸 → 조립 항목 | 같은 폴더 `CombatCueComposer.cs` |
| 필드 사건 칸(순간 큐) | `…/Combat/Playback/Ordering/CombatEventTypeTable.cs`(순간 칸), `…/Playback/Support/CombatCueSources.cs`(값)·`CombatMomentCuePlayback.cs`(냄) |
| 이펙트·카메라 창구 | `…/Combat/Presentation/Effects/CombatEffectWindow.cs`, `…/Presentation/Cameras/CombatCameraWindow.cs` |
| 오래 보이는 이펙트(§2.9) | `…/Combat/Presentation/Effects/CombatPersistentEffects.cs`(맞춤)·출처 셋 `CombatStatusEffectSource`·`CombatOwnedTraitEffectSource`·`CombatAttackModeEffectSource` |
| 행동 끝 이펙트 묶음 | `…/Combat/Presentation/Effects/CombatEffectActionGroup.cs` |
| 기차 이펙트 | `…/Combat/Display/Map/FieldRailwayPresenter.cs` |
| 새 필드 사건 칸을 더할 때 고칠 곳 | [폴더 배치 안내](../viewDocs/combat-view-folder-guide.md)의 사건 형식 지식 줄(여섯 곳) |
| 관련 설계 | [이펙트 창구 확장](../specs/2026-10-28-combat-effect-cue-expansion-design.md) · [범용 연출 구조](../specs/2026-10-27-combat-presentation-generic-structure-design.md) |
