# 전투 투사체·피격 소리 설계 (2판)

> 날짜: 2026-10-27
> 상태: 결정 완료(§4). PS-a·PS-b 코드 완료(Unity 확인 문제 없음 — 사용자, 구현 메모 §6). Task 계획은 [projectile-hit-sound-plan](../plans/2026-10-27-combat-projectile-hit-sound-plan.md).
> 요청(사용자):
> - "폭탄 착탄 혹은 총알이 지나갈 시 등에 따른 사운드 넣을 구간이 필요"
> - "HitSetting에 HitSound도 없네"
> - "착탄 소리는 투사체에 따라 달라야 해서 Preset 번호가 필요해서 투사체 SO에 두려는 것"
>
> 1판과 다른 점(결정 반영): 비행 소리는 투사체 SO의 지연으로 조절한다. 피격 소리는 반응 묶음 큐 목록(이펙트와 같은 칸 꼴)에 둔다. 그래서 투사체 소리를 내는 자리가 공격 재생에서 큐 붙이기로 옮겨졌다.
>
> 관련: [범용 연출 구조 설계](2026-10-27-combat-presentation-generic-structure-design.md)(G2 큐·G4 부품), AudioSystem 요청 메모 [audio-request](../handoffs/2026-10-27-combat-projectile-hit-sound-audio-request.md)

## 1. 확인한 사실

### 1.1 지금 소리 구조

| 소리 | 무엇(슬롯 + 프리셋) | 프리셋의 주인 | 언제 |
|---|---|---|---|
| 공격 | `SfxEvent.UnitAttack` + 공격 프리셋 | 공격한 유닛(`UnitVisual.attackSoundPreset`) | 공격 묶음 `cues.sound`(기본 Cue + 0) |
| 사망 | `SfxEvent.UnitDead` + 유닛 프리셋 | 죽은 유닛(`UnitVisual.unitSoundPreset`, 적은 설정 표 공용 값) | 반응 묶음 `deathCues.sound`(기본 Start + 0) |
| 피격 | 없음 | — | 큐 규칙이 피격 칸의 소리를 금지한다(`CombatPresentationCueRules:77`). 피격 조립에는 소리 창구가 없다. |
| 착탄·비행 | 없음 | — | — |

어느 사건에 어느 슬롯을 내는지는 소리 표 `CombatEventSounds`가 정한다(SSOT).

### 1.2 AudioSystem(GlobalSystem, 우리 영역 밖)

- 슬롯은 `SfxEvent { UnitPick 0, UnitCancel 1, UnitMovement 2, UnitDead 3, UnitAttack 4, ObsDamaged 5, ObsBroken 6, TrainCall 10, Tornado 11 }`다(Boot 플레이어 슬롯 순서).
- 착탄·비행·피격 슬롯은 없다. 없는 슬롯을 재생하면 `RequireCue`가 예외를 낸다.
- 프리셋은 그 슬롯 이벤트의 `parameterPresets` 번호다. AudioSystem의 `-1`은 "변경 없이 재생"이다.
  - 우리 쪽 `-1`(`CombatSoundPresets.None`)은 "소리 없음"이다. `CombatSoundCue.Effect`가 걸러서 AudioSystem에는 보내지 않는다.
- SFX 슬롯은 위치 없이 낸다. 객체에 붙은 플레이어 API(`Play(playerId, requesterObject, slot)`)가 있지만 쓰는 곳이 없다.
- `AudioSystem.cs`의 커밋 작성자는 Twinsgm·JMH다.

### 1.3 투사체가 떠나고 닿는 순간 = 조립의 기준 시점

투사체 종류(직선·곡사)에서는 기준 시점 둘이 곧 그 순간이다.

| 기준 시점 | 코드 모션 | 클립 원거리 |
|---|---|---|
| **Cue = 발사** | 애니메이션 `fired` 콜백 → `MotionCue` | 발사 조각이 이벤트 타이밍(`ClipEvent` + 0)에 쏨 |
| **Hit = 착탄** | 착탄 → 해제 → `Hit`(곡사는 착지, 직선은 대상 몸 접촉) | 발사 조각이 맞음 → 해제 → `Hit` |

- 조각 실행은 모든 항목이 끝나야 전체 종료한다(`CombatPieceRun` `_remainingItems`). 그래서 지연을 준 소리 항목도 투사체가 도착한 뒤까지 살아 있다가 난다.
- 대신 행동 실행이므로 그동안 다음 행동이 기다린다.

## 2. 설계

### PS1 무엇을 낼지(프리셋)를 정하는 곳

| 소리 | 프리셋을 정하는 곳 | 결정 |
|---|---|---|
| 착탄 | 투사체 부품 `CombatProjectileSO.impactSoundPreset` | 1판 그대로 |
| 비행 | 같은 SO의 `flightSoundPreset` | 1판 그대로 |
| 피격 | 반응 묶음 `CombatUnitReactionPresentationSO`의 피격 칸 소리 큐 항목 | Q-PS2 |

- 투사체 칸은 `UnitVisual`과 같은 꼴이다: `[SerializeField, Min(-1)] int … = -1`. 기본 -1 = 소리 없음. 검사는 `CombatPresentationSettingsData.ValidateSoundPreset`.
- 피격 프리셋은 반응 묶음 단위다. 지금 반응 묶음은 기본 하나(`CombatUnitReaction_Default`)를 모든 캐릭터가 쓴다. 그래서 유닛마다 다른 피격 소리가 필요하면 반응 묶음을 나누면 된다("일단" 이 자리, 사용자).
- 사망 소리는 그대로다. 유닛 프리셋을 쓰고 시점만 큐에 있다.

### PS2 슬롯 대응은 소리 표 한 곳

- 새 슬롯 셋은 AudioSystem 담당이 만든다. 이름은 요청 안이다: `ProjectileImpact`·`ProjectileFlight`·`UnitHit`.
- 소리 표 `CombatEventSounds`에 프리셋을 받아 소리 단서를 만드는 함수 셋을 더한다: `ProjectileImpact(preset)`·`ProjectileFlight(preset)`·`UnitHit(preset)`. 슬롯 이름이 적힌 곳은 이 셋뿐이다.
- `For(e)`(사건 → 소리)는 바꾸지 않는다. 피격 프리셋이 반응 묶음에 있으므로, 반응 묶음이 없는 외형(본부·화약통·보급품)의 일반 경로에서는 피격 소리가 나지 않는다.

### PS3 언제 — 투사체 소리는 기준 시점 + 투사체 칸

- **비행:** 시점 Cue(발사) + `flightSoundDelay`(투사체 SO, 0 이상). Q-PS1.
- **착탄:** 시점 Hit(착탄) + `impactSoundDelay`(투사체 SO, 0 이상). 2026-10-27 사용자 "조절할 수 있게 열어주삼"에 따라 계획 P10으로 더했다.
  - 투사체 공격에서 접촉(Contact)과 맞음(Hit)은 같은 호출이다(`ImpactThen`: 접촉 이펙트 → 맞음 신호). 총알·Cannon 모두 그렇다.
  - 그래서 시점 고르기 칸은 차이가 없어 두지 않고, 조절은 지연으로 한다.
- 내는 자리는 **큐 붙이기 `CombatCueComposer`**다(1판의 공격 재생 `PlayLaunch`안을 바꿈).
  - 지연이 생겨 시계가 필요해졌다. 조립 항목은 이미 "기준 시점 + 지연"을 다룬다.
  - 발사 두 길(코드 모션·클립 원거리)이 같은 시점 표를 쓰므로 한 곳에서 끝난다.
  - 공격 재생·전략 맥락 계약은 바꾸지 않는다.
- 붙이기는 공격 칸일 때, 공격 값의 투사체 소리 프리셋이 소리를 낼 때(0 이상)만 항목을 넣는다. 근접(부품 없음)은 -1이라 항목이 없다.
- 공격 소리(유닛)·비행 소리(투사체)가 같은 순간이면 겹쳐 난다. 의도한 동작이다.

### PS4 피격 소리 — 이펙트와 같은 칸 꼴의 목록 (Q-PS3)

- 피격 칸 입력에 `presetSounds` 목록을 둔다(처음 이름 `sounds`, 묶음 R에서 바꿈).
  - 항목 = 프리셋 + 시점(Anchor) + 지연(Delay) + 자리(Point)로, 이펙트 큐와 같은 꼴이다.
  - 값은 새 불변 값 `CombatPresetSoundCue`, 묶음 `CombatEventCues.PresetSounds`다.
- 규칙(큐 규칙 한 곳)
  - 목록은 **피격 칸에서만** 받는다. 공격·사망 칸은 지금의 `sound`(시점만)를 그대로 쓴다.
  - 프리셋은 0 이상이다(-1 항목은 뜻이 없어 막는다. 끄려면 항목을 지운다).
  - 시점·자리는 이펙트 큐와 같은 표로 검사한다(피격 칸: Start·Cue·End, 자리 SubjectBody 등).
  - 지연은 0 이상이다.
  - 목록은 비어도 된다.
- 피격 칸의 옛 `sound`(시점만) 금지 규칙은 그대로 둔다. 피격 칸의 소리는 목록 하나다.
- **자리(Point)는 지금 소리 위치에 쓰이지 않는다.** SFX 슬롯이 위치 없이 내기 때문이다. 칸은 결정대로 두고, 검사는 이펙트와 같이 한다. 3D 소리(객체 플레이어)를 붙일 때 쓴다. 칸 툴팁과 문서에 이것을 적는다.
- 재생: 피격 조립(`ComposeHit`)이 소리 창구를 받고, 붙이기가 목록 항목마다 `UnitHit(preset)` 소리 항목을 넣는다.

### PS5 값 객체

- 공격: `CombatAttackPresentationData`에 `ImpactSoundPreset`·`ImpactSoundDelay`·`FlightSoundPreset`·`FlightSoundDelay`를 더한다(착탄 지연은 계획 P10으로 더함). 생성자 끝 선택 인자이고, 기본은 -1·0·-1·0이다.
  - 공격 묶음 SO `CreateData`가 투사체 부품에서 읽는다.
  - 평평한 인자가 셋 늘어난다(범용 구조 검토 M3 때 투사체 값 묶음으로 옮김).
- 반응: `CombatEventCues`에 `PresetSounds`(끝 선택 인자, 기본 빈 목록)를 더한다. 그래서 지금의 묶음 생성 호출(시험 도우미 포함)은 그대로다.

### PS6 비행 소리의 범위

- 비행 소리는 발사 + 지연 때 한 번이다.
- 따라다니는 3D 소리는 범위 밖이다. 투사체 프리팹 FMOD 플레이어와 객체 지정 재생을 쓰는 길이며, AudioSystem 담당과 정할 일이다.

### PS7 순서

- PS-a(값)는 AudioSystem과 상관없이 할 수 있다. 화면·소리 변화는 없다.
- PS-b(재생)는 **슬롯이 없어도 연결을 미리 다 한다**(2026-10-27 사용자, 계획 P9).
  - 소리 표의 슬롯 칸 셋을 "연결 대기"(null)로 둔다. 그동안은 프리셋을 넣어도 소리를 내지 않고 예외도 없다.
  - `SfxEvent` 값이 생기면 그 칸 셋만 채운다. `SfxEvent`에 없는 값을 숫자로 짐작해 쓰지 않는다.

## 3. 영향

- **AudioSystem 담당(요청):** `SfxEvent` 셋, Boot SFX 플레이어 슬롯 셋, FMOD 이벤트·파라미터 프리셋(투사체별 착탄·비행 변종, 피격 변종).
- **PS-a**
  - 투사체 부품 칸 셋·검사
  - 공격 값 속성 넷
  - 묶음 SO 읽기
  - 큐 형식(`CombatPresetSoundCue`·입력 칸·`CombatEventCues.PresetSounds`)
  - 큐 규칙
  - 반응 기본 에셋 `hitCues.presetSounds: []`
- **PS-b**
  - 소리 표 함수 셋
  - 소리 창구가 단서 하나를 내는 입구
  - 소리 조각이 단서를 받도록
  - 큐 붙이기(피격 목록·투사체 둘)
  - 피격 조립 소리 창구
- **시험:** 기본값이 모두 "소리 없음"(투사체 -1, 목록 비어 있음)이라, 값을 넣기 전까지 소리·항목 수가 바뀌는 시험은 없을 것으로 본다. 계획 Pb.0에서 다시 센다. 새 시험은 없다.
- **공유:** AudioSystem 담당 요청 메모(2판으로 고침), 현강님 사건 소리 메모(소리 표·소리 조각), 호진님·민형님 없음.

## 4. 결정 (2026-10-27 사용자)

| 질문 | 결정 |
|---|---|
| Q-PS1 비행 소리 | 발사 때 한 번. **투사체 SO의 지연 값으로만 조절**(`flightSoundDelay`) |
| Q-PS2 피격 소리 프리셋 | **반응 묶음(`CombatUnitReactionPresentationSO`)에서 설정**(일단) |
| Q-PS3 피격 소리 칸 | **이펙트와 같은 세팅: Anchor·Delay·Point**(+ 프리셋), 목록 |
| Q-PS4 착탄 이펙트 | 그대로(공격 묶음 큐) |
| Q-PS5 단계 | PS-a부터 |

이 결정으로 1판의 "피격 = 맞은 유닛의 유닛 프리셋"·"피격 판정 함수 공유"·"공격 재생 `PlayLaunch`/`LaunchThen`"은 빠졌다.

예상(7기준, 알려진 문제 기준)은 약 8.3 그대로다.
- 내림 요인: 소리 큐가 둘(시점만 / 프리셋 포함)이 된다. 공격 값 평평한 인자가 는다.
- 올림 요인: 투사체 소리가 붙이기 한 곳에서 끝나고, 공격 재생은 바뀌지 않는다.

## 5. Phase

- **PS-0** AudioSystem 요청 메모 전달(사용자).
- **PS-a** 값(지금).
- **PS-b** 재생(슬롯이 생긴 뒤).
- **PS-c** 문서·공유.

## 6. 구현 메모 (2026-10-27, PS-a·PS-b 코드 완료, PS-a·PS-b Unity 확인 문제 없음 — 사용자)

계획과 다른 점, 그리고 설계 글에 없던 것만 적는다. Task별 기록은 [계획 §8](../plans/2026-10-27-combat-projectile-hit-sound-plan.md).

- **착탄 지연(계획 P10):** 투사체 부품에 `impactSoundDelay`를 더했다(사용자 "조절할 수 있게"). Hit/Contact를 고르는 칸은 두지 않았다. 두 시점이 같은 호출이라 고르는 칸이 효과가 없다.
- **슬롯 연결 대기(계획 P9):**
  - 소리 표 `CombatEventSounds`에 `SfxEvent?` 칸 셋을 두었다(지금 null). 슬롯 함수 셋은 칸이 비면 소리 없음을 돌려준다.
  - 그래서 큐 붙이기가 항목을 만들지 않고, 지금 소리·시험은 그대로다.
  - 슬롯이 생기면 그 세 줄만 채운다. 없는 값을 숫자로 짐작하지 않는다.
- **투사체 소리 값:** `CombatProjectileSounds`(공격 값 → 착탄·비행 프리셋·지연)를 큐 붙이기와 같은 파일에 두었다. 소리를 낼지(프리셋 -1·슬롯 연결 대기)는 큐 붙이기의 `AddSound` 한 곳이 거른다. 처음에는 `From`도 "둘 다 -1이면 null"로 걸렀으나, 같은 판단이 두 곳이 되어 묶음 끝 검토(낮음 2)에 따라 걷었다.
- **소리 조각:** 사건으로 만드는 생성자(공격·사망 소리)와 단서로 만드는 생성자(투사체·피격 소리)가 한 조각에 함께 있다. 소리 창구는 단서 하나를 내는 입구를 하나 더 가진다.
- **피격 조립:** 소리 창구만 받고 소리 사건은 받지 않는다(피격 칸에는 소리 시점이 없음).
- **이름 겹침(PS-b 뒤 사용자 보고 CS0104):** `CombatSoundCue`라는 이름이 DB 소리 시점 값과 소리 표 단서 둘에 있다. 두 네임스페이스를 함께 들이는 큐 붙이기에서만 겹쳐, 그 자리를 네임스페이스로 가렸다. 이름을 나누는 것은 묶음 끝 검토의 후보다.
- **범위 밖 그대로:**
  - 반응 묶음이 없는 외형(본부·화약통·보급품)은 피격 소리가 없다.
  - 치명타(그 피해로 죽는 사건)에는 피격 소리가 없다. 사망 사건은 피격 조립이 아니라 사망 조립(사망 소리)이 맡는다(묶음 끝 검토 낮음 6, 지금 구조 그대로).
  - 자리 칸은 지금 소리 위치에 쓰이지 않는다.
  - 따라다니는 3D 비행 소리는 하지 않았다.
- **묶음 R 반영(2026-10-27 사용자 "권장대로", 계획 §8.4):**
  - **이름 나눔:** DB 소리 시점 값을 `CombatSoundTimingCue`(+ `…Entry`)로, 프리셋 소리 목록을 `PresetSounds`(입력 칸·에셋 키 `presetSounds`)로 바꿨다. 소리 표의 단서 `CombatSoundCue`와 이름이 겹치지 않으므로 큐 붙이기의 네임스페이스 가림을 걷었다.
  - **소리 프리셋 규칙 하나:** "-1 = 없음, 0 이상 = 소리"의 원본은 DB 수 규칙 `CombatPresentationValueRules`(`NoSoundPreset`·`IsSoundPreset`·`RequireSoundPreset(allowNone)`)다. 설정 표 `ValidateSoundPreset`은 이것을 부르는 한 줄이 됐다. 공격 값·프리셋 소리 큐·런타임 `CombatSoundPresets.None`·`IsSound`도 이것을 부른다. 공격 값은 설정 표 네임스페이스에 기대지 않는다.
  - **투사체 소리 값 자리:** `CombatProjectileSounds`를 `Playback/Support/`의 새 파일로 옮겼다(+ `.meta`).
  - **소리 조각 한 모양:** 조각이 `Func<CombatSoundCue>` 하나를 받아 시작할 때 고른다. 사건 소리는 소리 창구 `CueOf(e)`로 그 순간의 보인 사실에서 고른다(사망 소리 순간은 그대로다).
  - **소리 창구 입구 하나:** 옛 `Emit`을 `Play(CombatSoundCue)`로 합쳤다. 사건·걸음·투사체·피격 소리가 모두 이 입구를 지난다.
