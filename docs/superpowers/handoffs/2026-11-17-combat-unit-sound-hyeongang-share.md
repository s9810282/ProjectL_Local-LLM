# 전투 유닛 소리를 유닛마다 — 현강님 공유 (2026-11-17, 미전달)

> 보내는 사람: 용근(전투 View). 받는 사람: 현강님(AudioSystem·사운드 연결).
> 원본: [유닛 소리를 유닛마다 설계](../specs/2026-11-17-combat-unit-sound-per-unit-design.md)(US1~US7, 결정 Q-US1~Q-US4 모두 A)·[계획](../plans/2026-11-17-combat-unit-sound-per-unit-plan.md) §6. 컴파일·Unity 확인 문제 없음(사용자, Ua·Ub). 커밋은 사용자.
> 사용자 요청: "공격 소리는 시점만 공격 연출에서, 어떤 소리인지는 UnitVisual에서 — 위치를 옮겨야 할까?" → "사망·피격도 카테고리별로 다를 예정이니 유닛별로". 사운드 작업자용 안내는 [사운드 안내](2026-10-28-combat-sound-guide-for-sound-worker.md)(2026-11-17 판)에 고쳐 두었습니다.

## 1. 한 줄 요약

공격·피격·사망 소리의 **무엇(번호) + 언제(시점·지연)**를 유닛 외형 맨 끝 "전투 소리" 줄(`attackSounds`·`hitSounds`·`deathSounds`) 한 곳으로 옮겼습니다. AudioSystem 호출(`PlayUnitAttack`·`Play(UnitEvent.UnitDead, …)`)과 Boot 슬롯은 그대로입니다. 지금 소리도 그대로입니다.

## 2. 바뀐 현강님 줄

| 자리 | 옛 줄 | 지금 |
|---|---|---|
| 외형 `UnitVisualDataSO.cs`의 `attackSoundSlotIndex` 칸·`AttackSoundSlotIndex` 읽기·검사(`686900826`) | 공격·포효 `UnitAttack` 슬롯 순번 | **지움.** 같은 번호를 그 외형의 `attackSounds` 줄 `sound`로 옮김(에셋 23개 — 칸이 비어 있던 광부 변형 셋은 줄 없음) |
| `CombatEventSounds.For`의 공격·스킬·포효 갈래(`CombatSoundCue.Attack(presets.AttackSlot(…))`) | 사건마다 공격자·시전자·보스를 꺼내 슬롯을 고름 | **지움.** 새 `CombatEventSounds.UnitSoundsOf`가 사건 형식 표 `ActorOf`로 누구인지 고르고 그 유닛의 줄을 돌려줌. 줄 칸 → 슬롯 대응은 `UnitSound(slot, 번호)` 하나(공격 = `CombatSoundCue.Attack(번호)` 그대로) |
| `CombatSoundPresets.AttackSlot(key)` | 외형에서 슬롯 읽기 | **지움.** 대신 `UnitSounds(key)`(키마다 처음 읽을 때 보관) |
| `CombatEventSounds.For`의 사망 갈래·`Unit(UnitEvent.UnitDead, …)` | 아군 = `unitSoundPreset`, 적 = 설정 표 `enemyUnitSoundPreset` | 사망은 죽는 유닛의 `deathSounds` 줄. "보인 사실에 아직 사망이 반영 전일 때만" 조건은 그대로(`IsUnshownDeath`) |

- `CombatEventAudio.Play(e)`는 유닛 소리 사건이면 그 유닛 줄을 모두 지금 냅니다(시점·지연 없이 — 연출 없이 지나가는 공격·반응 묶음 없는 사망·포효 콜백·전용 경로). 그 밖의 사건은 지금처럼 `For(e)`입니다.
- 줄이 있는 공격·피격·사망 연출은 조립(`CombatCueComposer`)이 줄마다 그 시점·지연에 냅니다(옛 공격 묶음 `cues.sound`·반응 묶음 `deathCues.sound`·`hitCues.presetSounds` 자리).
- 특성 발동이 출처인 추가 공격은 지금처럼 공격 소리가 없습니다.

## 3. 바뀐 뜻(값은 그대로)

- 외형 `unitSoundPreset`: 아군의 **선택·이동·취소**(와 장애물 피해·파괴)만. 사망은 사망 줄입니다.
- 설정 표 `enemyUnitSoundPreset`(5): 적의 **선택·이동**만. 적 사망은 외형마다 사망 줄입니다 — 옮길 때 모든 적 외형에 이 값 5를 그대로 넣었습니다(카테고리별로 바꾸려면 그 줄만 고치면 됨).
- 피격 효과음 칸(`UnitHit`)은 여전히 연결 대기입니다. 슬롯이 생기면 `CombatEventSounds`의 `UnitHitSlot` 한 줄만 채우면 되고, 번호는 외형 `hitSounds` 줄(유닛마다)이 고릅니다(옛 반응 묶음 하나 → 유닛마다로 바뀜).

## 4. 확인 부탁

- Boot `UnitAttack` 슬롯 순서를 바꾸실 때는 이제 외형 `attackSounds` 줄의 번호를 함께 고쳐 주세요(옛 `attackSoundSlotIndex` 자리).
- 피격 슬롯을 만드실 때 변종(프리셋)을 카테고리마다 둘지 정해 주시면, 외형 `hitSounds` 줄을 그 번호로 채우면 됩니다.
