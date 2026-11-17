# 전투 유닛 소리를 유닛마다 — Task 계획

- 날짜: 2026-11-17
- 설계: [unit-sound-per-unit-design](../specs/2026-11-17-combat-unit-sound-per-unit-design.md)(US1~US7, 결정 Q-US1~Q-US4 모두 A — 11-17 사용자 "모두 A로")
- 상태: **계획 작성, 코드 착수 전.** 계획에서 정한 것 TP-1~TP-14는 사용자 확인 대상.
- 규칙: View 작업이라 새 시험 없음·MSBuild 없음(컴파일 미확인으로 보고). 우리 시험은 컴파일이 깨지는 곳만 호출을 맞춘다. Phase가 끝나면 멈추고 보고한다. 검토는 묶음 끝에 한 번.

## 0. 착수 조건

- 클립 칸 다시 짜기 CS-a~CS-d(커밋 전)가 같은 파일을 고쳐 두었다: `CombatUnitAnimationRules`·`CombatPresentationCueRules`·`CombatCueEntries`·`CombatEventCues`·`CombatCueEntryDrawer`·`CombatUnitAnimationSO`. **그 묶음을 먼저 커밋하고 시작하는 것을 권한다.** 그대로 그 위에서 이어 해도 되지만 커밋을 나누기 어렵다.
- 다른 작업자 줄(US-c에서 현강님께 공유): 외형 `attackSoundSlotIndex` 칸(`686900826`, 11-10), 소리 표 `CombatEventSounds.For`의 공격·스킬·포효 갈래(`686900826`·`df422cbcf` 등 현강님 줄 + 우리 `918817c87` 스킬 줄), `CombatSoundPresets.AttackSlot`.

## 1. 계획에서 정한 것(확인 대상)

- **TP-1 형식·파일:** 새 파일 `DataBaseSystem/Data/View/CombatPresentation/CombatUnitSounds.cs`(+ `.meta`는 Unity가 만듦) 하나에 입력 줄 `CombatUnitSoundCueEntry`(칸 `sound` 번호 · `anchor` · `contactFilter` · `delay`), 값 줄 `CombatUnitSoundCue`, 묶음 값 `CombatUnitSoundsData`(`Attack`·`Hit`·`Death` 목록)를 둔다. 줄 형식은 셋 공용(설계 P2).
- **TP-2 외형 칸:** `UnitVisualData`에 `[Header("전투 소리")]` 아래 `attackSounds`·`hitSounds`·`deathSounds` 목록을 직접 둔다(Q-US1 A). 읽기 `Sounds`는 매번 새 값(다른 외형 값과 같은 규칙, 보관은 쓰는 쪽).
- **TP-3 검사 원본(검토 R6 반영):** `CombatPresentationCueRules.FindUnitSoundError(sounds, attack, attackMotion, hasReaction, hitMotion)` 하나.
  - 공격 줄의 시점은 그 유닛 공격 종류 칸(`AttackEventOf(kind)`)의 `AllowsAnchor`를 따르고, 접촉 Every는 받지 않는다(지금 공격 소리 시점 규칙과 같음).
  - 피격 줄은 Hit 칸, 사망 줄은 Death 칸(Start·End) 규칙이다.
  - Event1~3은 그 칸 본 단계 몸 클립에 표식이 있어야 한다. 지금 `FindEventAnchorError`의 표식 검사를 시점 하나 단위(`FindClipEventAnchorError`)로 나눠 큐와 소리 줄이 같이 부른다. 코드 모션 공격은 Event1만 된다.
  - 줄이 쓰이지 않는 조합은 부트에서 막는다(조용히 안 나는 소리 없게):
    - 공격 표시가 없는 외형의 공격 줄, 반응 묶음이 없는 외형의 사망 줄은 사건 순간에만 난다(연출 없이 사실만 반영하는 길). 그래서 시점은 Start만 받는다. 설계 P3("공격 표시가 없으면 공격 줄 금지")는 지금 되는 구성(연출 없는 공격도 사건 순간 소리)을 막아 이것으로 바꾼다.
    - 반응 묶음이 없는 외형의 피격 줄은 피격 조립이 없어 영영 나지 않으므로 멈춘다.
  - 번호는 0 이상이다. 줄 값 생성자가 `RequireSoundPreset(allowNone: false)`로 본다(끄려면 줄을 지움).
  - 부르는 곳은 외형 검사 `CombatUnitAnimationRules.ValidateUnit` 한 곳이다(새 인자 `sounds`). 클립 표식을 보는 유닛 조건이라 큐의 유닛 조건(`FindUnitError`)과 같은 자리이고, 규칙 원본은 큐 규칙 클래스다(검토 R12).
- **TP-4 덮어쓰기 공격(Q-US3 A, 검토 R3 반영):** 규칙 함수 `CombatPresentationCueRules.AttackSoundsFor(rows, cueEvent, motion)` 하나(US-b에서 만듦, 검토 R11).
  - 줄마다 그 공격 표시 종류·클립이 받는 시점이면 그대로, 아니면 Event1(지연 그대로, 고르기 Every)로 바꾼다. 허용 판정은 TP-3과 같은 함수다.
  - 공격 시작(`CombatAttackStart`)은 덮어쓴 공격인지 모른다(표시 값을 받기만 함). 그래서 늘 이 함수로 맞추고, 바뀐 줄이 없으면 받은 목록을 그대로 돌려준다(할당 없음). 자기 공격 표시는 부트 검사를 지나 늘 그대로다.
  - 조립(`CombatCueComposer`)에서 "시작할 수 없으면 Event1"로 대신하지 않는다. 빗나간 공격의 첫 맞음(Contact·FirstHit) 줄은 지금처럼 소리가 없어야 하는데, 그 방식이면 Event1에 소리가 생긴다.
- **TP-5 시계 없는 길:** 줄마다 그 순간에 번호를 모두 낸다(시점·지연은 쓰지 않음).
  - 해당하는 길: 포효 정면 공격의 소리 콜백(`CombatRoarEventHandler`의 `_sound.Play(roar)`), Presenter의 사실 사건 소리(`_sound.Play(e)` — 연출 없이 지나가는 공격, 반응 묶음 없는 사망), 전용 경로 `PlayEventSound`, 추락 사망·큐 없는 자폭 사망의 `CueOf(death)` 조각.
  - 지금 에셋은 줄이 하나씩이라 소리가 같다. 설계 P4의 "포효는 보스 공격 연출 SO 시점"은 틀려 바로잡았다.
- **TP-6 무엇을 낼지(검토 R2 반영):** 줄 목록 셋의 이름은 DB 쪽 열거 `CombatUnitSoundSlot`(Attack·Hit·Death)이고, 묶음 값 `CombatUnitSoundsData.Of(slot)`가 그 목록을 준다.
  - 칸 → 단서 대응은 소리 표 함수 하나 `CombatEventSounds.UnitSound(slot, 번호)`뿐이다. 조립·처리기는 칸 이름만 넘긴다.
    - 공격(스킬·포효 포함) = `CombatSoundCue.Attack(번호)`
    - 피격 = `UnitHit(번호)`(칸 연결 대기 그대로)
    - 사망 = `Effect(UnitEvent.UnitDead, 번호)`
  - 사망은 지금처럼 "보인 사실에 사망이 아직 반영되지 않았을 때만" 낸다. 조건은 소리 표 한 곳에 남기고, 조각이 시작하는 순간에 본다(지금 `CueOf(death)`와 같은 때).
- **TP-7 누구의 어느 줄(검토 R1·R4·R10 반영):**
  - 어느 사건이 어느 칸을 쓰는지는 소리 표가 정한다: 공격·스킬·포효 사건 = Attack, 사망 캐릭터 사건 = Death, 피격은 피격 조립이 Hit를 넘긴다. 그 밖의 사건(특성 발동 출처의 추가 공격 포함)은 유닛 소리가 없다. 특성 추가 공격은 지금도 공격 소리가 없어(출처가 발동 사건, 기록 "다시 친 공격 소리는 TB") 그대로 둔다 — 공격 모양의 공격자로 고르면 소리가 새로 생긴다.
  - 누구인지는 사건 형식 표를 쓴다: 공격·스킬·포효 = `CombatEventTypeTable.ActorOf`(지금 소리 표가 공격자·시전자·보스를 사건마다 따로 꺼내는 사본을 걷음), 사망 = 사건 캐릭터. 키는 보인 사실 찾기(`findCharacter`)로 얻는다.
  - 소리 창구 `CombatEventAudio`가 (사건 또는 유닛) → 유닛 소리 묶음(칸, 줄 목록) 입구를 낸다.
  - 줄 목록은 `CombatSoundPresets`가 키마다 처음 읽을 때 보관한다(외형 값은 읽을 때마다 새로 만들어 사건마다 배열이 생기므로). 보관은 그 객체 수명(전투 표시 표, View 서비스 수명에 한 번)이라 플레이 중 외형 SO를 고치면 다시 시작한 뒤 반영된다(진입 목록 복사와 같은 성질 — 툴팁에 적음).
  - `CombatSoundPresets.AttackSlot`은 지우고, `Unit()`의 문서에서 사망을 뺀다(선택·이동·취소만).
- **TP-8 조립(검토 R5 반영):** 창구 묶음 `CombatCueSinks`의 `soundEvent` 칸을 유닛 소리 묶음 칸으로 바꾼다. null이면 유닛 소리를 넣지 않는다(지금 `soundEvent` null과 같은 뜻 — 사실 사건·낙석·특성 큐).
  - `CombatCueComposer`는 소리 시점(`SoundTimings`)·프리셋 소리(`PresetSounds`) 대신 그 줄마다 `anchors.TryStart(anchor, filter, delay)`로 소리 조각을 넣는다. 투사체 소리는 그대로다.
  - 부르는 곳: 공격 시작 `ForAttack`, 캐릭터 처리기 조립 넷 — `ComposeHit` 두 갈래(부르는 곳 `TryBeginHit`·`BeginPushHit`·`BeginClipOnlyHit`·`TryBeginContactHit`), `ComposeDeath`, `ComposeSelfDestructDeath`. `ComposeFall`·큐 없는 자폭은 TP-5(그 순간에 줄마다).
- **TP-9 큐에서 걷기:**
  - `CombatEventCues`의 `Sound`·`SoundTimings`·`PresetSounds`와 생성자 인자 셋을 지운다. 생성자는 `(effects, shakes, flash)`가 된다.
  - 입력 `CombatAttackCueInput.sound`·`additionalSounds`, `CombatHitCueInput.presetSounds`, `CombatDeathCueInput.sound`를 지운다.
  - 형식 `CombatSoundTimingCueEntry`·`CombatSoundTimingCue`·`CombatPresetSoundCueEntry`·`CombatPresetSoundCue`를 지운다.
  - 큐 규칙의 소리 갈래(`needsSound`·사망 하나·프리셋 소리 피격만)와 `UsesAnchor`의 소리 부분을 지운다.
  - 큐 그리기 `CombatCueEntryDrawer`는 지운 형식 두 줄을 빼고 새 줄 형식 한 줄을 더한다(여백 그대로).
- **TP-10 생성자 호출 맞춤:** `new CombatEventCues(effects, shakes, null, null)`을 부르는 곳 둘의 호출만 맞춘다. 운영 코드 `CombatTraitFeedbackPlayback.cs:110`, 우리 시험 `Tests/Editor/CombatField/StructureHitPresentationTests.cs:56`(확인 내용 그대로).
- **TP-11 옮길 값(Q-US2 A·Q-US4 A, Claude가 YAML로):**
  - 공격 줄: `attackSoundSlotIndex` ≥ 0인 외형마다 한 줄 = (번호, 그 외형 공격 연출 SO `sound`의 시점·고르기·지연).
  - 사망 줄: 아군 외형(`Playerable/`)은 `unitSoundPreset` ≥ 0이면 (번호, Start, Every, 0). 적 외형(`Enemy/`·`EnemyTrait/`)은 설정 표 `enemyUnitSoundPreset` 값 5로 (5, Start, Every, 0).
  - 피격 줄: 없음(반응 SO `presetSounds`가 비어 있음).
  - `Object/`(본부·화약통·보급품)는 줄이 없다. 착수 때 이들이 캐릭터 사망 사건을 내는지 다시 확인한다 — 지금 규칙(적이 아니면 외형 번호, −1이면 없음)과 같은지 대조.
  - 둘 다 가능한 유닛(`Both`)은 지금 없다(모두 PlayerOnly 또는 EnemyOnly — 11-17 확인). 그래서 외형 하나에 사망 번호 하나로 지금 소리가 지켜진다.
- **TP-12 에셋에서 걷기:** 외형의 `attackSoundSlotIndex` 줄, 공격 연출 SO 9개의 `sound`·`additionalSounds` 블록, 반응 SO의 피격 `presetSounds`·사망 `sound` 블록을 지운다. 줄 끝 형식(CRLF/LF)은 파일마다 그대로 둔다.
- **TP-13 설정 표 칸:** `enemyUnitSoundPreset`은 남긴다(이동 소리). 툴팁에서 사망을 빼고, 사망은 외형 사망 줄이라고 적는다.
- **TP-14 Demon 변종 무음:** `UnitVisual_Demon_AnkleBreaker`·`UnitVisual_Demon_SoloAction`은 공격 줄 없이 옮긴다(지금과 같음). 소리를 넣을지는 옮긴 뒤 사용자가 정한다.
- **TP-15 캡슐화(검토 R7):** 입력 줄은 `private` 직렬화 칸 + `ToCue()`만, 값 줄·묶음 값은 `sealed`·읽기 전용(`IReadOnlyList`, 생성 때 복사), 빈 목록은 공유 빈 배열 하나(줄 없는 유닛은 `for`가 0번 — null 갈래 없음). 보관 사전은 `CombatSoundPresets` 안 `private`.
- **TP-16 직렬화(검토 R8):** 외형 목록은 다른 큐 목록과 같은 `List<T>` + `new()`. 지운 칸의 YAML 키는 에셋에서 직접 지운다(남겨도 무시되지만 다음 저장 때 다른 사람 diff에 섞임).
- **TP-17 Phase 경계(검토 R11):** 부르는 곳 없는 코드를 남기지 않게 `AttackSoundsFor`는 US-b에서 만든다. US-a는 값·검사·그리기만.

### 1.1 7기준 검토(11-17 사용자 "기준 검토하고 보완한 뒤 작업 시작해", 착수 전)

| # | 기준 | 찾은 것 | 반영 |
|---|---|---|---|
| R1 | SSOT | 소리 표가 공격·스킬·포효마다 공격자·시전자·보스를 따로 꺼냄 — 사건 형식 표 `ActorOf`와 같은 사실의 사본 | TP-7: `ActorOf`를 씀 |
| R2 | SSOT·SRP | "줄 목록 → 어느 소리 칸" 대응이 처리기·조립 여러 곳에 흩어질 수 있음 | TP-6: 칸 이름 열거 + 소리 표 함수 하나 |
| R3 | 독립성·If | 공격 시작은 덮어쓴 공격인지 모름 → 덮어쓰기 표지를 새로 만들어야 함. 조립에서 대신 시점으로 바꾸면 빗나간 공격에 소리가 생김 | TP-4: 늘 맞춤, 바뀔 줄 없으면 같은 목록 |
| R4 | 독립성(동작 보존) | 공격자를 공격 모양에서 고르면 특성 추가 공격에 공격 소리가 새로 생김 | TP-7: 출처 사건으로 소리 표가 고름(지금과 같음) |
| R5 | 정확성 | 피격 조립 갈래가 넷(보통·밀림 함께·클립만·연발 앞 접촉)인데 계획이 "피격 조립"으로만 적음 | TP-8: 조립 함수와 부르는 곳을 모두 적음 |
| R6 | 정확성(조용한 무동작) | 반응 묶음 없는 유닛의 피격 줄은 영영 안 남. 연출 없는 길의 줄 시점·지연은 쓰이지 않음. 설계 P3는 지금 되는 구성을 막음 | TP-3: 피격 줄 막음, 연출 없는 길은 Start만, P3 바꿈 |
| R7 | 캡슐화 | 값·목록·보관의 공개 범위가 적혀 있지 않음 | TP-15 |
| R8 | C#/Unity | 지운 칸의 YAML 키 처리, 목록 형식이 적혀 있지 않음 | TP-16 |
| R9 | If/Tick | 사건마다 외형 값을 새로 만들면 배열이 생김(GC) | TP-7 보관(이미 있음), 빈 목록 공유(TP-15) |
| R10 | FW·수명 | 보관 수명 때문에 플레이 중 SO 수정이 바로 안 보임 | TP-7: 툴팁·문서에 적음 |
| R11 | 독립성 | US-a에 부르는 곳 없는 함수가 한 Phase 동안 남음 | TP-17 |
| R12 | SRP | 소리 검사를 클립 규칙 클래스(`ValidateUnit`)가 부름 | 받아들임: 클립 표식을 보는 유닛 조건이라 큐 유닛 조건과 같은 자리, 규칙 원본은 큐 규칙 |

점수(10점, 알려진 문제 기준 추정): SSOT 7.5 → 9.5 · SOLID 8 → 9 · FW 9 → 9.5 · 독립성 7.5 → 9 · 캡슐화 8.5 → 9.5 · C#/Unity 8.5 → 9 · If/Tick 8 → 9 = 평균 약 **8.1 → 9.2**.

## 2. Phase US-a — 값·검사(화면 변화 없음)

| Task | 할 일 | 파일 |
|---|---|---|
| Ua.1 | 새 줄 형식·값·칸 이름 열거·묶음 값(TP-1·TP-6·TP-15). 줄 값은 시점·고르기 검사를 지금 큐 줄과 같은 함수(`RequireDefined`·`RequireFilter`)로, 번호 0 이상 | 새 `CombatUnitSounds.cs` |
| Ua.2 | 외형 세 목록·`Sounds` 읽기(TP-2·TP-16), 툴팁 셋(번호의 뜻·비우면 없음·시점 규칙·보관 수명) | `UnitVisualDataSO.cs` |
| Ua.3 | 검사 `FindUnitSoundError`·표식 검사 나눔(TP-3) | `CombatPresentationCueRules.cs` |
| Ua.4 | 외형 검사가 소리 줄을 넘김(`ValidateUnit` 인자) | `CombatUnitAnimationRules.cs`·`UnitVisualDataSO.cs` |
| Ua.5 | 큐 그리기에 새 줄 형식 한 줄 | `CombatCueEntryDrawer.cs` |

옛 칸·옛 재생은 이 Phase에서 그대로라 소리는 지금과 같다. 새 목록은 비어 있다.

**Ua 확인(사용자):** 컴파일, 부트 통과, 외형 인스펙터의 "전투 소리" 세 목록·큐 목록 여백. 시험 삼아 줄을 하나 넣어 보고 잘못된 시점(예: 근접 유닛 공격 줄에 Contact)이면 부트가 멈추는지.

## 3. Phase US-b — 재생·걷기·옮기기(코드와 에셋 한 커밋)

| Task | 할 일 | 파일 |
|---|---|---|
| Ub.1 | 소리 창구 유닛 소리 입구·보관(TP-7) | `CombatEventAudio.cs`·`CombatSoundPresets.cs` |
| Ub.2 | 소리 표: 공격·스킬·포효·사망 갈래를 유닛 소리 묶음으로, 시계 없는 길은 줄마다(TP-5·TP-6) | `CombatEventSounds.cs`·`CombatEventAudio.cs` |
| Ub.3 | 창구 묶음·조립 소리 부분(TP-8) | `CombatCueComposer.cs` |
| Ub.4 | 규칙 `AttackSoundsFor`(TP-4), 공격 시작이 출처 사건의 유닛 소리 묶음을 넘기며 늘 그 함수로 맞춤 | `CombatPresentationCueRules.cs`·`CombatAttackStart.cs` |
| Ub.5 | 캐릭터 처리기 피격·사망·자폭·추락 조립이 대상 소리 묶음을 넘김 | `CombatCharacterEventHandler.cs` |
| Ub.6 | 큐에서 소리 걷기(TP-9), 외형 `attackSoundSlotIndex` 지움, 설정 표 툴팁(TP-13), 공격 연출·반응 SO 툴팁·큐 머리 | `CombatEventCues.cs`·`CombatCueEntries.cs`·`CombatPresentationCueRules.cs`·`UnitVisualDataSO.cs`·`CombatCueEntryDrawer.cs`·`CombatAttackPresentationSO.cs`·`CombatUnitReactionPresentationSO.cs`·`CombatPresentationSettingsDataSO.cs` |
| Ub.7 | 생성자 호출 맞춤 둘(TP-10) | `CombatTraitFeedbackPlayback.cs`·`StructureHitPresentationTests.cs` |
| Ub.8 | 에셋 옮기기·걷기(TP-11·TP-12): 외형 26개, 공격 연출 SO 9개, 반응 SO 1개 | `DataBaseSO/ViewGroup/UnitVisual/Item/**`·`CombatPresentation/AttackSetting/*`·`HitSetting/CombatUnitReaction_Default.asset` |
| Ub.9 | 옮긴 값 대조표(외형마다 옛 번호·시점 → 새 줄)를 실행 기록에 남김 | 이 문서 §6 |

**Ub 확인(사용자):**
1. 부트가 통과하는지.
2. 공격 소리가 지금과 같은 시점에 나는지 — Ballista는 Start, 나머지(Akimbo·Shotgun 포함)는 모두 Event1(옛 공격 연출 SO 값 그대로 — 계획 작성 때 "Akimbo·Shotgun 첫 맞음"이라 적은 것은 틀려 바로잡음, §6.2).
3. 포효·스킬 소리가 지금처럼 나는지.
4. 아군·적 사망 소리가 지금과 같은지(적은 모두 5).
5. 반응 묶음 없는 유닛, 추락 사망, 자폭 사망의 소리가 나는지.
6. 한 유닛에만 공격 줄을 하나 더 넣어(예: Cowboy Event2) 그 유닛만 바뀌는지.

## 4. Phase US-c — 문서·공유·검토

| Task | 할 일 |
|---|---|
| Uc.1 | 사운드 작업자 안내(`handoffs/2026-10-28-combat-sound-guide-for-sound-worker.md`)의 공격·피격·사망 절, 이펙트·소리 창구 안내(`handoffs/2026-10-29-combat-effect-sound-cue-guide.md`)의 소리 시점 절 |
| Uc.2 | 현강님 공유 새 메모(바뀐 현강님 줄: `attackSoundSlotIndex` → 외형 공격 줄, 소리 표 공격·스킬·포효 갈래, `AttackSlot`), 미전달 |
| Uc.3 | 설계 §7 구현 메모, 폴더 배치 안내(`CombatUnitSounds` 한 줄), CLAUDE.md |
| Uc.4 | 묶음 끝 검토(새 에이전트 하나, 7기준) |

## 5. 커밋 묶음

- 이 묶음 전: 클립 칸 CS-a~CS-d(같은 파일).
- US-a: 새 `CombatUnitSounds.cs`(+ `.meta`)와 고친 DB 파일. 단독으로 커밋할 수 있다(옛 길 그대로).
- US-b: 코드와 에셋은 **한 커밋**이다. 코드만 들어가면 공격·사망 소리가 사라지고, 에셋만 들어가면 외형 줄이 읽히지 않는다(옛 칸을 지운 뒤라 남은 YAML 키는 무시됨).
- US-c: 문서.

## 6. 실행 기록

### 6.1 US-a 값·검사 코드 완료(11-17 사용자 "기준 검토하고 보완한 뒤 작업 시작해", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Ua 대기)

착수 전 7기준 검토(§1.1, R1~R12)를 계획에 반영하고 시작했다. 클립 칸 CS-a~CS-d는 아직 커밋 전이라 그 위에서 이어 했다(같은 파일 넷: 큐 규칙·클립 규칙·외형·큐 그리기).

- Ua.1 새 `DataBaseSystem/Data/View/CombatPresentation/CombatUnitSounds.cs`(`.meta`는 Unity가 만듦): 칸 이름 `CombatUnitSoundSlot`(Attack 1·Hit 2·Death 3), 입력 줄 `CombatUnitSoundCueEntry`(`sound`·`anchor`·`contactFilter`·`delay`, private 칸 + `ToCue`), 값 줄 `CombatUnitSoundCue`(번호 0 이상·시점 정의·고르기·지연 0 이상), 묶음 값 `CombatUnitSoundsData`(`Attack`·`Hit`·`Death` 읽기 전용 사본, 빈 목록 공유, `Of(slot)`·`IsEmpty`·`Read(입력 셋)`·`Empty`).
- Ua.2 `UnitVisualDataSO.cs`: 클래스 끝에 `[Header("전투 소리 — 줄마다 번호 · 시점 · 지연, 이 유닛만의 값")]` + `attackSounds`·`hitSounds`·`deathSounds`(`List<T>` + `new()`, 툴팁에 번호 뜻·시점·연출 없는 길·피격 칸 연결 대기·포효·추락/자폭), 읽기 `Sounds`(매번 새 값), 검사가 `ReadSounds()`로 읽어 줄 값 오류를 키와 함께 알림. 옛 칸 `attackSoundSlotIndex`는 US-b까지 그대로.
- Ua.3 `CombatPresentationCueRules.cs`: `FindUnitSoundError(sounds, attack, attackMotion, hasReaction, hitMotion)`(TP-3, 검토 R6 규칙), 줄 하나 검사 `FindSoundAnchorError`·사건 순간만 `FindMomentOnlyError`(Start · 지연 0), 표식 검사를 시점 하나 단위 `FindClipEventError`로 나눠 큐(`FindEventAnchorError`)와 소리 줄이 같이 부름. 큐 표식 오류 문구 꼴이 조금 바뀜("○ 칸의 큐 — 시점 Event2의 표식(ClipEvent2)이 … 없습니다", 검사 뜻은 같음).
- Ua.4 `CombatUnitAnimationRules.ValidateUnit`에 인자 `sounds`(셋째 뒤, null이면 예외)·큐 유닛 조건 바로 뒤에 소리 줄 조건. 부르는 곳은 외형 검사 한 곳.
- Ua.5 `DataBaseSystem/Editor/CombatCueEntryDrawer.cs`에 새 줄 형식 한 줄(목록 여백·구분선 같음).

계획과 다른 점:
- 툴팁의 "보관 수명"(TP-7·R10)은 보관이 US-b에서 생기므로 그때 적는다.
- 사건 순간만 받는 줄은 시점뿐 아니라 지연도 0만 받는다(그 길은 지연을 쓰지 않아 — R6 원칙을 그대로 넓힘).

화면 변화 없음: 새 목록은 비어 있고 재생은 아직 옛 칸(`attackSoundSlotIndex`·공격 연출·반응 SO 소리)을 읽는다. 에셋 변경 없음.

**Ua 확인(사용자):** 컴파일, 부트 통과, 외형 인스펙터 끝의 "전투 소리" 세 목록과 줄 사이 여백. 시험 삼아 줄을 넣어 보면 — 근접 유닛 공격 줄 Contact(Every) → 부트 멈춤, 반응 묶음 없는 외형의 피격 줄 → 멈춤, Event2 표식 없는 클립에 Event2 줄 → 멈춤(확인 뒤 줄은 지움 — 아직 소리는 안 남). **Ua 문제 없음(11-17 사용자).**

### 6.2 US-b 재생·걷기·옮기기 코드·에셋 완료(11-17 사용자 "Ua 문제 없음, US-b", 컴파일 미확인, 새 시험 없음, 시험 변경 = 우리 `StructureHitPresentationTests:56` 호출 한 줄, Ub 대기)

코드:
- Ub.1 `CombatSoundPresets`: `UnitSounds(key)`(키마다 처음 읽을 때 보관, 외형 없으면 빈 묶음), `AttackSlot` 지움, `Unit()`은 선택·이동·취소만(문서). `CombatEventAudio`: `Play(e)`가 유닛 소리면 줄을 모두 지금 냄(`PlayNow`, TP-5) 아니면 소리 표 `For`, 새 입구 `UnitSoundsOf(e)`·`HitSoundsOf(id)`·`CueOf(set, row)`(사망은 아직 반영 전일 때만), 옛 `CueOf(e)` 지움.
- Ub.2 `CombatEventSounds`: 새 값 `CombatUnitSoundSet`(칸·줄·사망 사건, 줄 없으면 만들지 않음, `ForAttack(kind, motion)`), `UnitSoundsOf`(공격·스킬·포효 = `CombatEventTypeTable.ActorOf`의 공격 줄, 사망 = 그 캐릭터의 사망 줄, 그 밖 null — 특성 발동 출처는 null), `UnitSound(slot, 번호)`(칸 → 효과음 칸 한 곳), `IsUnshownDeath`(옛 사망 조건), `For`에서 공격·스킬·포효·사망 갈래 지움.
- Ub.3 `CombatCueComposer`: 창구 묶음 `soundEvent` → `unitSounds`(`CombatUnitSoundSet`, null = 없음), 조립 소리 부분은 줄마다 `TryStart(시점, 고르기, 지연)`·연결 대기 칸(피격)이면 항목 없음, `AddSound` 지움.
- Ub.4 규칙 `CombatPresentationCueRules.AttackSoundsFor`(TP-4, 바뀐 줄 없으면 같은 목록), `CombatAttackStart.SinksFor(shape, settings, motion, projectileSounds)`가 출처 사건의 공격 줄을 그 공격 표시 종류·클립에 늘 맞춤(부르는 곳 넷: 범위 스킬 클립·코드 모션·원거리 클립·근접 클립).
- Ub.5 `CombatCharacterEventHandler`: `ComposeDeath`·`ComposeSelfDestructDeath`(큐 있음) = 창구 묶음에 사망 줄, `ComposeFall`·큐 없는 자폭 = 새 `AddSoundsNow`(줄마다 시작 순간), `ComposeHit` = `sound?.HitSoundsOf(subject)` — 부르는 곳 넷(보통·밀림 함께·클립만·연발 앞 접촉)은 무변경. `ComposeFall`에 소리 창구 null 검사.
- Ub.6 `CombatEventCues` 생성자 `(effects, shakes, flash)`·`Sound`·`SoundTimings`·`PresetSounds`·소리 `Read` 둘 지움, 입력 셋의 소리 칸 지움, `CombatCueEntries`의 소리 형식 넷 지움, 큐 규칙 소리 갈래·`UsesAnchor` 소리 부분 지움, 외형 `attackSoundSlotIndex`(칸·읽기·검사) 지움, 외형 공격 줄 툴팁에 덮어쓰기·보관 수명(R10), `unitSoundPreset` 툴팁(사망 뺌), 설정 표 `enemyUnitSoundPreset` 툴팁(선택·이동만), 큐 그리기 형식 두 줄 지움, 툴팁·주석 열(공격 연출 SO·반응 SO·반응 값·공격 값·큐 입력 머리·규칙 요약·특성 처리기·공격 시작·클립 공격·코드 모션 공격).
- Ub.7 생성자 호출 둘: `CombatTraitFeedbackPlayback.cs:110`, 우리 시험 `StructureHitPresentationTests.cs:56`(확인 내용 그대로).

에셋(Ub.8, 줄 끝 형식 파일마다 그대로):
- 외형 26개: `attackSoundSlotIndex` 줄 23개 지움(칸이 없던 셋은 없음), 끝에 `attackSounds`·`hitSounds: []`·`deathSounds`.
- 공격 연출 SO 9개: `cues.sound`(+ `additionalSounds: []`) 블록 지움. 반응 SO: 피격 `presetSounds: []`·사망 `sound` 블록 지움.

옮긴 값 대조(Ub.9, 옛 값 → 새 줄 `[번호, 시점, 고르기, 지연]`, 시점 1 = Start·2 = Event1):

| 외형 | 옛 공격 칸 · 그 공격 연출 SO 시점 | 새 공격 줄 | 옛 사망(아군 = `unitSoundPreset`, 적 = 공용 5) | 새 사망 줄 |
|---|---|---|---|---|
| Akimbo | 0 · Event1 | [0, 2, 1, 0] | 0 | [0, 1, 1, 0] |
| Ballista | 1 · Start | [1, 1, 1, 0] | 4 | [4, 1, 1, 0] |
| Boomber | 5 · Event1(Hammer) | [5, 2, 1, 0] | 2 | [2, 1, 1, 0] |
| CactusCannon | 2 · Event1 | [2, 2, 1, 0] | 4 | [4, 1, 1, 0] |
| Cowboy | 3 · Event1(Melee) | [3, 2, 1, 0] | 2 | [2, 1, 1, 0] |
| ShotGun | 6 · Event1 | [6, 2, 1, 0] | 3 | [3, 1, 1, 0] |
| Sniper | 7 · Event1(Linear) | [7, 2, 1, 0] | 1 | [1, 1, 1, 0] |
| SoulEater | 5 · Event1(Melee) | [5, 2, 1, 0] | 2 | [2, 1, 1, 0] |
| MainCharacter · Shielder | −1 | 없음 | −1 | 없음 |
| Cannon · UndeadCannon 셋 | 8 · Event1 | [8, 2, 1, 0] | 5 | [5, 1, 1, 0] |
| Demon | 9 · Event1 | [9, 2, 1, 0] | 5 | [5, 1, 1, 0] |
| Gravekeeper · UndeadRider_Disarm | 11 · Event1 | [11, 2, 1, 0] | 5 | [5, 1, 1, 0] |
| Heavy · UndeadHeavy_Entrenchment | 10 · Event1 | [10, 2, 1, 0] | 5 | [5, 1, 1, 0] |
| StageBoss1 | 12 · Event1 | [12, 2, 1, 0] | 5 | [5, 1, 1, 0] |
| Demon_AnkleBreaker · Demon_SoloAction · lqwe(키 83) | 칸 없음(−1) | 없음(TP-14) | 5 | [5, 1, 1, 0] |
| Headquarters · PowderBarrel · Supplies | −1 | 없음 | 구조물이라 캐릭터 사망 사건이 없음 | 없음 |

- 옛 공격 연출 SO 9개의 시점은 Ballista만 Start, 나머지 모두 Event1, 지연 0, 추가 시점 없음이었다(Akimbo·Shotgun·Hammer·SoulEater는 고르기 칸 1이 적혀 있었지만 Event1이라 쓰이지 않음).
- TP-11 확인: 본부·장애물(화약통·보급품)은 Core에서 구조물(`StructureCombatFacts`, 장애물 = 중립 구조물)이라 캐릭터 사망 사건을 내지 않아 사망 줄이 없어도 지금과 같다. 둘 다 가능한(`Both`) 유닛은 없다.
- 계획 Ub 확인 2번의 "Akimbo·Shotgun 첫 맞음"은 틀려 고쳤다(옛 값은 Event1).

계획과 다른 점:
- `ComposeHit`·`ComposeDeath`는 인자를 늘리지 않고 이미 받는 소리 창구·주체/사망 사건으로 줄을 고른다(부르는 곳 무변경).
- 시계 없는 길은 `CombatEventAudio.Play(e)` 한 곳(`PlayNow`)과 처리기 `AddSoundsNow`가 맡는다(포효 콜백·Presenter 사실 사건 소리·전용 경로는 `Play(e)`를 그대로 부름).

**Ub 확인(사용자):** 위 §3 Ub 확인 1~6. 지금 에셋에는 피격 줄이 없어 피격 소리는 그대로 없다(칸도 연결 대기).

**컴파일 오류 고침(11-17 사용자 보고 CS1729):** `CombatCueSources.cs:27`의 빈 큐 `NoCues = new(null, null, null, null)`이 대상 형식 `new(...)` 꼴이라 Ub.7 검색(`new CombatEventCues(`)에서 빠졌다 → `new(null, null, null)`. 같은 꼴·지운 API 참조를 다시 찾아 더 없음을 확인했다. 그 사이 다른 작업이 피격 조립을 새 파일 `Playback/Support/CombatHitStart.cs`(아직 추적 안 됨)로 옮겼는데, 피격 소리 줄(`sound?.HitSoundsOf(subject)`)은 그 파일에 그대로 있다.

### 6.3 US-c 문서·공유·묶음 끝 검토 (2026-11-17, 사용자 "Ud, Ub 문제 없음, CS-e, US-c: 진행해", 코드 변경 없음)

- **Ub 문제 없음(사용자).**
- 문서(Uc.1·Uc.3): 사운드 작업자 안내(제목 "공격·피격·사망 소리를 유닛 외형 줄로", §1·§2.3·§3·§4.1 표(외형마다 공격·사망 줄)·§4.2·§4.3·§5.1~§5.4·§6·§7·§8), 이펙트·소리 창구 안내(§1.1 소리 열·§1.3 포효·§2.10·§6), 폴더 배치 안내(§2 소리 규칙 둘·Support 줄), 설계 §7 구현 메모.
- 공유(Uc.2, 미전달): 현강님 새 메모 `handoffs/2026-11-17-combat-unit-sound-hyeongang-share.md`(`attackSoundSlotIndex`·소리 표 공격·스킬·포효 갈래·`AttackSlot`·사망 갈래가 바뀜, `unitSoundPreset`·`enemyUnitSoundPreset` 뜻), 호진님 클립 메모 §12(외형 소리 줄·`attackSoundSlotIndex` 지움·`ValidateUnit` 인자).
- **묶음 끝 검토(Uc.4, 클립 칸 CS-e와 한 번에, 약 8.3)** — 기록 클립 칸 계획 §5.4. 유닛 소리 쪽 결과:
  - 동작 보존: 외형 26개의 새 공격·사망 줄이 옛 값과 모두 같음, YAML 들여쓰기·줄 끝 형식 문제 0, 공유 SO에 남은 소리 키 0, 부트 검사(`FindUnitSoundError`) 26개 모두 통과로 봄.
  - **M1 사망 줄의 End 시점·지연 > 0은 소리가 조용히 빠짐**(코드로 확인): 사망 조립이 시작한 바로 뒤(`TryBegin` 다음 줄) 사실에 사망을 반영하는데, 소리 조각이 시작할 때 "아직 반영 전"인지 보므로 시작 순간·지연 0이 아닌 줄은 늘 없음이 된다. 부트는 통과하고 툴팁은 End를 고를 수 있다고 안내함. 지금 에셋은 모두 Start·0이라 닿지 않음. 권장 A: 조립 길은 처리기가 이미 "반영 전"을 보장하므로 조립을 만들 때 판단을 끝내고(줄 묶음에서 사망 사건을 비워 넘김) 시작 때 확인은 시계 없는 길(`PlayNow`)에만 남김. 대안 B: 사망 줄을 Start·지연 0만 받게 검사·툴팁을 좁힘.
  - L6 부트 검사와 실행의 "연출 없는 길" 기준이 다름: 공격 표시는 있는데 코드 모션 부품도 클립도 없으면 실행은 사건 순간에 줄을 모두 내지만 검사는 시점을 제한하지 않음(지금 외형은 닿지 않음).
  - L7(추정) 피격 효과음 칸을 연결하면 표시가 꺼진 유닛의 Event1~3 피격 줄이 기준 시점 표에서 예외를 낼 수 있음(피격 큐 이펙트의 Event 시점에도 원래 같은 길, 지금은 칸이 연결 대기라 닿지 않음).
- **반영은 사용자 결정 대기.**

**검토 반영(11-17 사용자 "권장대로 반영", 컴파일 미확인, 새 시험 없음, 시험 변경 없음, Unity 확인 대기):**
- M1 = A: 사망 조립(반응·추락·자폭 깜빡임)은 조립 때 "아직 사망 반영 전"을 한 번 판단한다. 새 입구 `CombatEventAudio.DeathSoundsForComposition(death)`(반영 전이면 판단을 끝낸 줄 `CombatUnitSoundSet.Decided()` — 사망 사건을 비운 같은 줄, 이미 사망으로 보였으면 null), 캐릭터 처리기 조립 넷이 이것을 씀. 낼 때 판단은 시계 없는 길(`PlayNow`)에만 남음 → 사망 줄의 End·지연도 소리가 남.
- L1의 소리 쪽 주석(반응 SO 툴팁·값 규칙·`CombatUnitSounds` 생성자 주석·창구 묶음 문서)과 L2 `IsEmpty` 지움은 클립 칸 계획 §5.4 반영 기록에 함께.
- 그대로: L6(검사와 실행의 "연출 없는 길" 기준 차이)·L7(피격 슬롯 연결 때의 잠복 예외)은 기록만.
- **확인(사용자):** 컴파일, 어느 외형의 사망 줄을 `[번호, End, Every, 0]` 또는 지연 0.2로 바꿔 사망 소리가 반응 끝·0.2초 뒤에 나는지(확인 뒤 되돌림), 지금 에셋의 사망 소리가 그대로인지.

**검토 반영 확인 문제 없음(11-17 사용자 "확인 문제 없음").** 유닛 소리(US-a~US-c)는 사용자 커밋·공유 전달만 남음.
