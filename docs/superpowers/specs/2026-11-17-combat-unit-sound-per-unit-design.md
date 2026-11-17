# 전투 유닛 소리를 유닛마다 — 설계 초안

- 날짜: 2026-11-17
- 상태: **결정 완료(11-17 사용자 "모두 A로" = Q-US1~Q-US4 A)**, Task 계획 [unit-sound-per-unit-plan](../plans/2026-11-17-combat-unit-sound-per-unit-plan.md). 코드·에셋 변경 없음(착수 전).
- 요청: 사용자 "공격 소리는 시점만 공격 연출에서, 어떤 소리인지는 UnitVisual에서 — 위치를 옮겨야 할까?" → "사망·피격도 카테고리별로 사운드가 다를 예정이니 그냥 유닛별로 다르게 만드는 게 나을 것 같다".
- 관련: [클립 칸 다시 짜기 설계](2026-11-17-combat-unit-clip-slot-redesign-design.md)(큐 시점 Event1~3·접촉 고르기), [투사체·피격 소리 설계](2026-10-27-combat-projectile-hit-sound-design.md)(피격 소리 목록 PS4), [사운드 작업자 안내](../handoffs/2026-10-28-combat-sound-guide-for-sound-worker.md), [이펙트·소리 창구 안내](../handoffs/2026-10-29-combat-effect-sound-cue-guide.md).

## 1. 지금

| 소리 | 무엇(어떤 소리) | 언제(시점) | 재생 |
|---|---|---|---|
| 공격(포효·스킬 포함) | 유닛 외형 `attackSoundSlotIndex`(UnitAttack 플레이어 이벤트 칸, 현강님 `686900826`) | 공격 연출 SO `cues.sound`·`additionalSounds`(같은 소리를 더 냄) | `CombatEventSounds.For` 공격·스킬·포효 갈래 |
| 피격 | 반응 SO `hitCues.presetSounds`(프리셋 + 시점 + 지연 + 자리) | 같은 줄 | `CombatEventSounds.UnitHit` — **효과음 칸 연결 대기(null)라 지금 소리 없음** |
| 사망 | 아군 = 외형 `unitSoundPreset`(선택·이동·취소와 같은 번호), 적 = 설정 표 `enemyUnitSoundPreset`(5) 하나 | 반응 SO `deathCues.sound`(하나, Start) | Unit 플레이어 `UnitDead` |

확인한 사실:
- 공격 연출 SO는 여러 유닛이 같이 쓴다. `CombatAttack_Melee` 한 에셋을 외형 12개가 쓰고, 그 유닛들의 공격 칸 번호는 3·9·11·10·5·12·10·11·−1(Shielder)로 다르다. 그래서 한 유닛만 시점을 바꾸려면 공격 연출 SO를 복사해야 하고, 그러면 이펙트·흔들림까지 갈라진다.
- 외형 셋(`UnitVisual_Demon_AnkleBreaker`·`UnitVisual_Demon_SoloAction`·`UnitVisual_lqwe`)은 에셋에 `attackSoundSlotIndex` 칸이 없어 기본값 −1이다. 그래서 Demon 변종 둘은 Demon(9)과 달리 지금 공격 소리가 없다(보고만, 고치지 않음).
- 칸 번호가 유닛당 하나라, 한 공격에 서로 다른 소리 둘(예: Event2 휘두르기 + Event1 맞음)을 못 낸다.
- 반응 SO는 모든 유닛이 같이 쓰는 `CombatUnitReaction_Default` 하나다. 피격·사망 소리를 유닛(카테고리)마다 다르게 할 자리가 없다.
- 적 외형에도 `unitSoundPreset` 값이 적혀 있지만(대포 4·Demon 1 등) 적은 사망·이동에 공용 5만 쓴다.
- 공격 연출 SO를 덮어쓰는 곳이 둘 있다: 플레이어 스킬 외형(`PlayerSkillVisualDataSO`)·특성 발동 줄(`TraitActivationVisual`). 지금 그 공격의 소리 시점은 덮어쓴 SO의 것이고(지금 에셋은 모두 Event1), 소리는 시전자의 공격 칸이다.

## 2. 범위

- 넣음: 유닛의 **공격·피격·사망** 소리를 "무엇 + 언제" 한 줄로 유닛마다 둔다.
- 그대로: 선택·이동·취소(`unitSoundPreset`·적 공용 프리셋), 장애물 피해·파괴, 투사체 착탄·비행(투사체 부품 SO), 환경·기믹·아이템·배경음, AudioSystem(GlobalSystem — 칸·플레이어는 바꾸지 않음).

## 3. 설계

### US1 자리 — 유닛 외형 한 곳

`UnitVisualDataSO`에 "전투 소리" 묶음을 둔다(**Q-US1**):

| 칸 | 줄 하나 | 번호의 뜻 |
|---|---|---|
| `attackSounds` | 번호 · 시점 · 접촉 고르기 · 지연 | UnitAttack 이벤트 칸(옛 `attackSoundSlotIndex`) |
| `hitSounds` | 번호 · 시점 · 지연 | `UnitHit` 프리셋(칸 연결 대기 그대로) |
| `deathSounds` | 번호 · 시점 · 지연 | `UnitDead` 프리셋 |

- 줄 형식은 하나(`CombatUnitSoundCueEntry` 가칭: 번호·시점·고르기·지연)이고 칸마다 툴팁이 번호의 뜻을 적는다. 목록을 비우면 그 소리가 없다(지금 −1과 같음).
- 공격 줄을 여럿 두면 다른 소리 여럿을 다른 시점에 낸다(`additionalSounds`의 "같은 소리 한 번 더"를 대신함).
- 피격 줄의 옛 자리(Point) 칸은 소리 위치에 쓰이지 않아(SFX가 위치 없이 냄) 두지 않는다(P1).
- 같은 카테고리 유닛은 같은 값을 외형마다 적는다(사용자 "유닛별로"). 대포 변종 넷·Demon 변종 셋처럼 같은 값이 여러 외형에 생긴다.

### US2 공유 연출 SO에서 소리 걷기

- 공격 연출 SO 큐의 `sound`·`additionalSounds`, 반응 SO의 피격 `presetSounds`·사망 `sound`, 외형의 `attackSoundSlotIndex`를 지운다. 공유 SO에는 이펙트·흔들림·플래시만 남는다.
- 큐 규칙의 "공격·사망 칸은 소리 시점이 꼭 있어야 함"·"사망은 하나만"·"프리셋 소리는 피격만"을 걷고, 시점 규칙은 US4로 옮긴다.
- 쓰는 곳이 없어지는 형식 `CombatSoundTimingCueEntry`·`CombatPresetSoundCueEntry`(+ 값)와 큐 그리기(`CombatCueEntryDrawer`)의 두 줄도 걷는다.

### US3 사망 소리의 원본

사망은 외형 `deathSounds` 하나가 원본이다(아군·적 모두). 적 공용 프리셋은 이동 소리에만 남고, `unitSoundPreset`은 선택·이동·취소(아군)·장애물에만 남는다(**Q-US2**는 옮기는 값).

### US4 시점 검사 — 외형 검사 한 곳

외형 표 검사(`CombatUnitAnimationRules`에서 `CombatPresentationCueRules.FindUnitError`를 부르는 곳)가 본다:
- 공격 줄: 그 유닛 공격 연출 SO의 종류(근접·원거리) 시점 규칙(`AllowsAnchor`, 접촉 Every 금지 — 지금 공격 소리 시점과 같음) + Event1~3은 공격 본 단계 몸 클립 표식(`HasEvent`, 코드 모션은 Event1만).
- 피격 줄: 피격 칸 시점 규칙 + 피격 클립 표식. 사망 줄: 사망 칸 규칙(Start·End).
- 공격 연출이 없는 외형(본부·장애물)은 공격 줄을 두면 멈춘다. 번호는 0 이상(지금 규칙 `RequireSoundPreset`).

### US5 덮어쓰기 공격(스킬·특성 추가 공격)

스킬·특성 발동 줄이 공격 연출 SO를 덮어써도 소리는 시전자 외형의 `attackSounds`를 쓴다(**Q-US3**). 덮어쓴 공격 종류가 받지 못하는 시점 줄(예: 원거리 유닛의 Contact 줄을 근접 덮어쓰기에서)은 Event1(타격·발사 순간)에 낸다. 지금 덮어쓰기 에셋의 소리 시점은 모두 Event1이라 시전자 줄이 Event1이면 화면이 같다. 스킬 전용 소리는 나중(`CombatEventSounds` 스킬 갈래 주석, 설계 SK3).

### US6 재생

- 조립(`CombatCueComposer`)의 소리 부분은 사건 칸 큐 대신 **그 사건의 유닛 소리 줄**을 받는다: 공격·스킬·포효 = 공격자 외형 `attackSounds`, 피격 = 대상 `hitSounds`, 사망 = 대상 `deathSounds`. 창구 묶음(`CombatCueSinks`)에 줄 목록 하나를 더한다.
- "무엇"은 줄 번호로 바로 만든다: 공격 `CombatSoundCue.Attack(번호)`, 피격 `UnitHit(번호)`, 사망 `Effect(UnitEvent.UnitDead, 번호)`. 사망 조건(보인 사실에 사망이 아직 반영되지 않았을 때만)은 지금 소리 표 그대로 한 곳에 둔다.
- 소리 표 `CombatEventSounds.For`의 공격·스킬·포효·사망 갈래와 `CombatSoundPresets.AttackSlot`은 걷는다(줄이 대신함). 다른 갈래는 그대로.

### US7 옮기기(지금 소리 그대로)

| 에셋 | 옮길 값 |
|---|---|
| 외형 26개 공격 | `attackSoundSlotIndex` ≥ 0이면 한 줄 = (그 번호, 그 외형 공격 연출 SO의 `sound` 시점·고르기·지연). 칸이 없는 셋은 −1이라 줄 없음(Demon 변종에 소리를 넣을지는 옮긴 뒤 따로). `additionalSounds`는 지금 모두 비어 있음 |
| 외형 사망 | 아군 = `unitSoundPreset` ≥ 0이면 (그 번호, Start, 0) / 적 = **Q-US2** |
| 외형 피격 | 반응 SO `presetSounds`가 비어 있어 옮길 것 없음 |

Claude가 YAML로 옮긴다(**Q-US4**). 코드와 에셋은 한 커밋이다(코드만 들어가면 소리가 사라지고, 에셋만 들어가면 모르는 칸).

## 4. 설계에서 정한 것(확인 대상)

- P1 피격 줄에 자리(Point) 칸을 두지 않는다(지금 소리 위치에 쓰이지 않음).
- P2 줄 형식은 셋 공용 하나, 번호 뜻은 칸 툴팁.
- P3 ~~공격 연출이 없는 외형의 공격 줄은 부트에서 멈춘다.~~ → 계획 TP-3로 바꿈(11-17 7기준 검토 R6): 연출이 없는 길(공격 표시 없는 공격·반응 묶음 없는 사망)은 사건 순간에만 나서 시점 Start만 받고, 반응 묶음 없는 유닛의 피격 줄은 영영 안 나서 멈춘다.
- P4 스킬의 소리 시점도 공격 줄(US5). 포효는 지금 소리 시점이 공격 연출 SO가 아니라 코드 순간(정면 공격 모션의 콜백)이라, 그 순간에 보스 공격 줄의 번호를 냅니다(계획 TP-5 — 11-17 계획 때 바로잡음).
- P5 선택·이동·취소는 이번에 옮기지 않는다(시점이 없는 소리라 문제가 없음).

## 5. 결정(11-17 사용자 "모두 A로")

아래 넷 모두 A로 정했다.

- **Q-US1 자리:** A 외형 칸에 직접(권장 — 사용자 "유닛별로", 한 곳에서 다 보임) / B 소리 묶음 SO를 외형이 참조(카테고리가 한 에셋을 공유, 클릭 한 번 더).
- **Q-US2 적 사망 소리 옮길 값:** A 지금 소리 그대로 적 공용 5(권장) / B 적 외형에 적힌 `unitSoundPreset` 값(대포 4·Demon 1 등 — 지금은 안 쓰이는 값이라 소리가 바뀜).
- **Q-US3 덮어쓰기 공격:** A 시전자 공격 줄, 못 받는 시점은 Event1로(권장) / B 스킬·특성 발동 줄에 소리 줄을 따로 둠(스킬 전용 소리, 범위가 커짐).
- **Q-US4 옮기기:** A Claude가 YAML로(권장, 값은 표 §3 US7) / B 에디터 도구 메뉴.

## 6. Phase(안)

- US-a 값·검사: 줄 형식·외형 칸 셋·외형 검사(옛 칸은 아직 남김, 화면 변화 없음).
- US-b 재생·걷기·옮기기: 조립이 유닛 줄을 읽음, 공유 SO 소리 칸·`attackSoundSlotIndex`·소리 표 갈래 걷기, 에셋 옮기기(한 커밋).
- US-c 문서·공유·검토: 사운드 작업자 안내·이펙트·소리 창구 안내의 공격·피격·사망 절, 현강님 공유(공격 칸 `attackSoundSlotIndex`·`CombatEventSounds` 공격·스킬·포효 갈래가 현강님 줄), 묶음 끝 검토.

착수 조건: 클립 칸 다시 짜기 CS-a~CS-d가 같은 파일(`CombatUnitAnimationRules`·`CombatPresentationCueRules`·`CombatCueEntries`·`CombatEventCues`·`CombatCueEntryDrawer`)을 고친 채 커밋 전이라, 그 묶음을 먼저 커밋하고 시작하는 것을 권한다.

## 7. 구현 메모(US-a~US-c, 2026-11-17)

- 계획 단계 7기준 검토(계획 §1.1, R1~R12)로 바뀐 것: 누구의 소리인지는 사건 형식 표 `ActorOf`(소리 표의 공격자·시전자·보스 사본을 걷음), 칸 → 효과음 슬롯 대응은 소리 표 `UnitSound` 하나, 덮어쓰기 공격은 공격 시작이 늘 맞춤(`AttackSoundsFor`, 바뀔 줄 없으면 같은 목록 — 조립에서 Event1로 대신하는 방식은 빗나간 공격에 소리가 생겨 버림), 특성 발동 출처 추가 공격은 지금처럼 소리 없음, P3를 "연출 없는 길은 Start·지연 0만·반응 없는 피격 줄은 막음"으로 바꿈.
- 줄 형식은 `DataBaseSystem/Data/View/CombatPresentation/CombatUnitSounds.cs` 하나(칸 이름 `CombatUnitSoundSlot`·입력 줄·값 줄·묶음 값). View의 줄 묶음 `CombatUnitSoundSet`은 소리 표 파일(`CombatEventSounds.cs`) 안에 둬 새 View 파일이 없다.
- 시계 없는 길(TP-5)은 `CombatEventAudio.Play(e)`(→ `PlayNow`)와 캐릭터 처리기 `AddSoundsNow`(추락·큐 없는 자폭) 둘이다. 포효 콜백·Presenter의 사실 사건 소리·전용 경로는 `Play(e)`를 그대로 부른다.
- `ComposeHit`·`ComposeDeath`는 인자를 늘리지 않고 이미 받는 소리 창구·주체/사망 사건으로 줄을 고른다(부르는 곳 무변경).
- 옮긴 값은 지금 소리 그대로다(대조표 계획 §6.2). 계획의 "Akimbo·Shotgun 첫 맞음"은 틀렸고 옛 값은 Event1이었다. 본부·장애물은 구조물이라 사망 줄이 없다.
- 컴파일 오류 하나(CS1729, `CombatCueSources.cs:27`의 대상 형식 `new(...)`)를 사용자 보고로 고쳤다. Ua·Ub 문제 없음(사용자).
- 문서: 사운드 작업자 안내(2026-11-17 판)·이펙트·소리 창구 안내, 공유(미전달) 현강님 [새 메모](../handoffs/2026-11-17-combat-unit-sound-hyeongang-share.md)·호진님 클립 메모 §12.
