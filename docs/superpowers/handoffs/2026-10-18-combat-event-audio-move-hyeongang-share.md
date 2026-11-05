# 전투 사건 소리 호출 위치 이동 (용근 → 현강님)

- 작성일: 2026-10-18. 작성: 용근(View). 브랜치 `feature/Yonggeun/Ref-EventPresenter`.
- **상태: 전달용 문서. 아직 보내지 않았습니다.**
- 관련: [Presenter 분리 설계](../specs/2026-10-18-combat-scene-event-presenter-split-design.md) S5·Q-S2 A, [분리 계획](../plans/2026-10-18-combat-scene-event-presenter-split-plan.md) §4(S-3)

## 1. 요약

- `CombatSceneEventPresenter`가 너무 커서 책임별로 나누고 있습니다. 그중 **전투 결과 재생의 소리 호출을 새 클래스 `CombatEventAudio`로 옮겼습니다.**
- **위치만 옮겼습니다.** 어떤 사건에서 어떤 슬롯·프리셋을 부르는지, 부르는 시점은 그대로입니다. Audio 로직은 이번 작업 범위가 아닙니다(리팩토링 R4 결정 유지).
- 코드에서 `CombatSceneEventPresenter.UnitPreset`·`AttackPreset`을 부르던 곳은 `CombatEventAudio.UnitPreset`·`AttackPreset`으로 바꿔야 합니다(§3). 현강님 브랜치에 새로 부르는 곳이 있으면 병합 때 컴파일 오류로 드러납니다.

## 2. 옮긴 것

새 파일: `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatEventAudio.cs`

| 전(`CombatSceneEventPresenter`) | 후(`CombatEventAudio`) |
|---|---|
| `PlayAudio(CombatEvent)` (private) | `Play(CombatEvent)` |
| `PlayMovementSound(CharacterCombatFacts)` (private) | `PlayMovement(CharacterCombatFacts)` |
| `PlayUnit(SfxEvent, CharacterCombatFacts)` (private) | 같은 이름(private) |
| `internal static int UnitPreset(EntityKey, Owner, CategoryKey?)` | 같은 서명 |
| `internal static int AttackPreset(EntityKey)` | 같은 서명 |
| `NoAudio`(포효용 빈 콜백, private) | `internal static readonly Action NoAudio` |

- 값은 그대로입니다: 적 유닛 5, 메인 캐릭터 0, 범주 Base·Rider·Heavy·Cannon은 `(int)범주 / 10`, 공격 프리셋 메인 0·Akimbo 1·Sniper 2·ShotGun 6·Gravekeeper 7·CactusCannon 9·Ballista 11, Demon은 -1(재생 안 함).
- 사건별 소리도 그대로입니다: 사망(아직 보인 사실에 사망이 반영되지 않았을 때) `UnitDead`, 성공한 공격 `UnitAttack`, 턴 시작 `UnitPick`, 전투 진입 BGM 프리셋 3, 밤 끝 BGM 프리셋 2, 장애물 반응 `ObsBroken`/`ObsDamaged`, 걸음 `UnitMovement`.
- 사건 Presenter는 생성자에서 받은 `IAudioSystemService`를 그대로 새 클래스에 넘깁니다. `CombatSceneDisplayFactory`의 조립 서명은 바뀌지 않았습니다.

## 3. 같이 고친 호출 위치

호출 대상 이름만 바꿨습니다. 동작은 같습니다.

| 파일 | 바꾼 것 |
|---|---|
| `ViewSystem/Combat/AttackTargetingInteraction.cs` (`Cancel`의 취소 소리) | `CombatSceneEventPresenter.UnitPreset` → `CombatEventAudio.UnitPreset` |
| `ViewSystem/Combat/PlacementDragInteraction.cs` (`BeginDrag`의 드래그 프리셋) | 같음 |
| `Tests/EditMode/Framework/GameAudioTests.cs` | `UnitPreset`·`AttackPreset` 호출 5곳 |
| `Tests/EditMode/Framework/StageMapCombatEntryBootSmokeTests.cs` | `UnitPreset`·`AttackPreset` 호출 5곳 |
| `Tests/EditMode/Framework/CombatStandardAttackAssetTests.cs` | `AttackPreset` 1곳 |
| `Tests/EditMode/Framework/CombatSceneEventPresenterTests.cs` | `UnitPreset`·`AttackPreset` 각 1곳 |
| `Tests/EditMode/Framework/CombatRailwayContactSceneTests.cs` | 시험용 소리 주입 대상을 Presenter의 `_audio`에서 `CombatEventAudio`의 `_audio`로(Presenter 필드 `_sound` 경유) |

## 4. 부탁

- 따로 하실 일은 없습니다. 현강님 브랜치에서 위 두 인터랙션 파일이나 소리 시험을 고치셨다면 병합 때 이 이름 변경과 겹칠 수 있으니 알려 주세요.
- 앞으로 전투 사건 소리를 바꾸실 때는 `CombatEventAudio` 한 곳만 보시면 됩니다. → 2026-10-18 B-3 뒤로는 아래 §5를 보세요.

## 5. 2026-10-18 후속 — "무엇을 내나" 표와 배경음 프리셋 함수 (B-3, 미전달, 이 문서와 함께 전달)

전투 연출 조각·조립 작업([조각·조립 설계](../specs/2026-10-18-combat-presentation-pieces-design.md) BP8, 결정 Q-B4 A)으로 소리를 "정하는 곳"만 한 곳으로 모았습니다. **값·프리셋 번호·호출 시점·AudioSystem 사용 방식은 바뀌지 않았습니다.**

| 무엇 | 지금 자리 | 고치는 방법 |
|---|---|---|
| 결과 재생 사건의 효과음·배경음 "무엇을 내나" | `ViewSystem/Combat/CombatEventSounds.cs`의 `For(사건, 보인 사실 조회)` | 사건 하나의 소리를 바꾸거나 더할 때 `switch` 한 줄 |
| 걸음 소리 | 같은 파일 `Movement(유닛)` | 같음 |
| 국면 → 배경음 프리셋(밤 3·낮 2·승패 확인 뒤 1) | 같은 파일 `BgmPreset(CombatBgmPhase)` | 번호를 바꿀 때 이 함수 한 곳. 사건 소리(전투 진입·밤 끝)·표시 갱신(`CombatSceneDisplay.Refresh`)·조정자(`CombatViewSystem.PresentCompletion`)가 이 함수를 부릅니다 |
| 유닛·공격 프리셋 번호 | `CombatEventAudio.UnitPreset`·`AttackPreset`(그대로) | 그대로. 입력 쪽(취소·드래그 소리)과 `GameAudioTests`가 계속 씁니다 |
| AudioSystem 호출 | `CombatEventAudio`(표의 답을 `Play`·`SetPreset`으로 냄) | 보통은 고칠 일 없음 |

- **언제 내는지는 그대로입니다.** 사실 반영 전(틀), 공격 연출의 발사·명중 콜백, 걸음 시작 콜백입니다. 사망 소리만 처리기의 직접 호출에서 "사망 조립"의 소리 조각으로 옮겼고, 순서(사망 반응 시작 → 사망 소리 → 사실 반영)는 같습니다.
- **범위 밖(그대로):** 승리 효과음(`CombatSceneDisplay.ShowCompletion`의 `UIReward`), 부트·스테이지 맵의 배경음 프리셋(`GameBootstrap` 0, `StageMapViewSystem` 1), 입력 쪽 취소·집기 소리.
- **새 시험:** `Tests/EditMode/Framework/CombatEventSoundsTests.cs`(사건별 단서, 배경음 번호, `RecordingGameAudio`로 실제 호출 확인). 현강님 `RecordingGameAudio`를 그대로 썼습니다.
- **부탁:** 따로 하실 일은 없습니다. 현강님 브랜치에서 `CombatEventAudio.Play`의 분기를 고치셨다면 병합 때 `CombatEventSounds.For`로 옮겨 주세요(분기 내용은 같은 모양입니다).

## 6. 2026-10-27 후속 — 소리 "언제"가 데이터로 (범용 연출 구조, 미전달)

근거는 [범용 연출 구조 설계](../specs/2026-10-27-combat-presentation-generic-structure-design.md) G2와 [계획](../plans/2026-10-27-combat-presentation-generic-structure-plan.md) §13.6입니다. **Audio 로직·프리셋·소리 표(`CombatEventSounds`)는 그대로입니다.** 바뀐 것은 "언제 내나"입니다.

- 공격·사망 소리의 시점은 공격 묶음 SO·반응 SO의 칸 큐(소리 시점)가 정합니다. 기본은 공격 = 동작 기준 순간(Cue) + 0, 사망 = 시작(Start) + 0이라 옛 순간과 같습니다. 낼지는 지금처럼 프리셋(-1 = 없음)이 정합니다.
- 클립 묶음의 `soundDelay` 칸은 걷고, 같은 값은 공격 칸 큐의 소리 시점 지연입니다(에셋 9개 모두 0이었음).
- 코드 모션 공격(클립 없는 유닛)의 소리는 이제 조립 항목입니다. 애니메이션이 부르던 효과음 콜백 자리가 기준 순간 신호(`MotionCue`)가 되고, 그 신호로 소리 조각이 같은 호출 안에서 소리를 냅니다(근접의 "소리 → 해제" 순서 그대로).
- **포효 처리기의 정면 공격 줄**(현강님 `9e48ce94` "공격 재연결", `CombatRoarEventHandler.cs`)을 새 공격 재생 입구 `TryBeginMotion`으로 바꿨습니다. 포효 소리 콜백 `() => _sound.Play(roar)`는 그대로 넘기고, 박치기 해제를 `CombatHitMoment.ReleaseThenSignal`로 묶었습니다. 소리 순간·내용은 같습니다.

## 7. 2026-10-27 후속 — 투사체 착탄·비행·피격 소리 연결 (미전달)

근거는 [투사체·피격 소리 설계](../specs/2026-10-27-combat-projectile-hit-sound-design.md)와 [계획](../plans/2026-10-27-combat-projectile-hit-sound-plan.md) §8입니다. **Audio 로직·기존 프리셋 함수·기존 사건 소리(`For(e)`)는 그대로입니다.** 소리 표·소리 창구·소리 조각에 입구만 더했습니다.

- **소리 표 `CombatEventSounds`**
  - 슬롯 함수 셋을 더했습니다: `ProjectileImpact(preset)`·`ProjectileFlight(preset)`·`UnitHit(preset)`.
  - 효과음 칸은 `SfxEvent?` 셋(지금 null = AudioSystem 슬롯 대기)입니다. 비어 있는 동안은 소리 없음을 돌려줍니다.
  - AudioSystem 담당께 슬롯을 요청했습니다([요청 메모](2026-10-27-combat-projectile-hit-sound-audio-request.md)). 슬롯이 생기면 그 세 줄만 채웁니다.
- **소리 창구 `CombatEventAudio`**
  - 옛 `Emit`을 `Play(CombatSoundCue)` 하나로 합쳤습니다(내용 그대로). 사건·걸음·투사체·피격 소리가 모두 이 입구를 지납니다.
  - 사건의 단서를 고르는 `CueOf(e)`(= `CombatEventSounds.For`)를 더했습니다. `Play(e)`는 `Play(CueOf(e))`입니다.
- **소리 조각 `CombatEventSoundPiece`**
  - 생성자가 `(소리 창구, Func<CombatSoundCue>)` 하나로 바뀌었고, 조각이 시작하는 순간에 단서를 고릅니다.
  - 사망 조립은 `() => sound.CueOf(death)`를 넘깁니다. 사망 소리를 고르는 순간(사실 반영 전)은 그대로입니다.
- **프리셋을 갖는 곳**
  - 착탄·비행: 투사체 부품 SO
  - 피격: 반응 묶음 SO의 피격 소리 목록
  - 공격·사망: 지금처럼 유닛 외형
- **언제 내는지**: 큐 붙이기 `CombatCueComposer`가 정합니다.
  - 착탄 = 맞는 순간 + 지연
  - 비행 = 발사 순간 + 지연
  - 피격 = 항목의 시점 + 지연

## 8. 2026-10-28 후속 — 오브젝트·장애물·추락·약한 지반·토네이도·아이템 소리 (소리 창구 2차, 미전달)

근거는 [전투 소리 창구 2차 설계](../specs/2026-10-27-combat-environment-item-sound-design.md)(결정 §4.1)와 [계획](../plans/2026-10-28-combat-environment-item-sound-plan.md) §5입니다. **Audio 로직·소리 창구 `CombatEventAudio`·기존 사건 갈래는 그대로입니다.** 소리 표에 갈래를, 프리셋 객체에 함수를 더했습니다.

- **소리 표 `CombatEventSounds.For(e)`** — 갈래 다섯을 더했습니다. 모두 사건 순간(지금 틀의 `_sound.Play(e)`)에 냅니다.

| 사건 | 슬롯(지금 null) | 프리셋 |
|---|---|---|
| `CombatObjectActivationEvent`(지뢰 발동·깃발·배지 습득) | `ObjectActivated` | `presets.Object(종류)` |
| `CombatObjectCreatedEvent` | `ObjectCreated` | 같은 번호(지금 Core가 내지 않는 사건) |
| `CombatFallEvent`(유닛·구조물 모두) | `Fall` | `presets.Fall()` |
| `CombatMovementSourceEvent`(약한 지반 → 낭떠러지일 때만) | `GroundCollapse` | `presets.WeakGroundCollapse()` |
| `CombatItemEvent`(Core `Activated` = 소모, 중복 효과 포함. 장착·버림은 소리 없음) | `ItemActivated` | `presets.Item(아이템 키)` |

- **장애물 갈래:** `Obstacle(...)` 도우미로 옮겼습니다. 장애물 외형(유닛 외형 키 21·22)의 `unitSoundPreset`이 0 이상이면 프리셋과 함께, -1이면 **지금처럼 프리셋 없이** 냅니다. 두 외형 모두 -1이라 지금 소리는 같습니다.
- **슬롯 함수 둘:** `TornadoLift(preset)`·`TornadoLand(preset)`. 토네이도 들어 올림 조립(호진님 `TryBeginTornado`)이 부르고, 프리셋은 토네이도 연출 설정이 갖습니다.
- **슬롯 칸 일곱**은 1차 셋과 같이 `SfxEvent?` = null(AudioSystem 슬롯 대기)입니다. 슬롯이 생기면 그 줄만 채웁니다([요청 메모](2026-10-27-combat-projectile-hit-sound-audio-request.md) §5).
- **프리셋 객체 `CombatSoundPresets`**
  - 생성자가 `(유닛 외형 표, 오브젝트 외형 표, 아이템 외형 표, 설정 표)`가 됐습니다. 만드는 곳은 `CombatDisplayTables` 한 곳뿐이라 그 줄만 바꿨습니다.
  - 함수 다섯: `Obstacle(종류)`·`Object(종류)`·`Item(키)`·`Fall()`·`WeakGroundCollapse()`. 외형 표에 없으면 소리 없음(-1)입니다.
- **부탁:** 따로 하실 일은 없습니다. 현강님 브랜치에서 `CombatEventSounds`의 장애물 갈래를 고치셨다면 병합 때 새 도우미 `Obstacle(...)`로 옮겨 주세요.

## 9. 2026-10-28 후속 — 사실 사건 소리 갈래가 사건 형식 표의 "순간" 칸을 읽음 (이펙트 창구 확장, 미전달)

근거는 [전투 이펙트 창구 확장 설계](../specs/2026-10-28-combat-effect-cue-expansion-design.md)(EC6, 7기준 검토 F2 = A)와 [계획](../plans/2026-10-28-combat-effect-cue-expansion-plan.md) P15·P16·§6.2입니다. **Audio 로직·소리 창구 `CombatEventAudio`·슬롯·프리셋 값은 그대로이고, 나는 소리도 같습니다.** 같은 사실 사건에 이펙트 큐가 붙으면서 "이 사건은 어떤 순간인가"를 소리 표와 이펙트가 같이 읽게 했습니다.

- **사건 형식 표 `CombatEventTypeTable`:** 순간 칸 `moment`와 질문 `TryGetMoment(e, out moment)`를 더했습니다. 값은 새 파일 `Playback/Ordering/CombatEventMoment.cs`입니다(순간 종류 `CombatMomentKind` 일곱 + 열쇠·칸·주체).
  - 채운 줄: 장애물 반응(피해·파괴), 오브젝트 발동·생성, 추락, 약한 지반 붕괴(정적 지형 원본 사건 중 붕괴만), 아이템(`Activated`만).
- **소리 표 `CombatEventSounds.For(e)`:** §8에서 더한 사실 갈래 여섯(장애물·오브젝트 발동·오브젝트 생성·추락·약한 지반·아이템)을 `default` 한 갈래로 합쳤습니다. `TryGetMoment`가 순간을 주면 `Moment(moment, presets)`(순간 종류 → 슬롯·프리셋 switch 하나)를 부르고, 아니면 소리 없음입니다.
- **장애물 도우미:** `Obstacle(슬롯, 장애물 종류, presets)`로 바꿨습니다(피해 = `ObsDamaged`, 파괴 = `ObsBroken`). "외형 프리셋이 -1이면 프리셋 없이" 규칙은 그대로입니다.
- 쓰지 않게 된 `Display.Map` using과 약한 지반 판정 함수를 지웠습니다. 판정은 형식 표가 새 `Playback/Ordering/CombatTerrainChanges.cs`로 합니다.
- **결과:** 순간 종류 → 슬롯 대응은 옛 갈래를 줄마다 옮긴 것이라 소리가 같습니다. 슬롯 일곱은 여전히 연결 대기(null)이고, 장애물 슬롯 둘은 지금처럼 납니다.
- **부탁:** 따로 하실 일은 없습니다. 브랜치에서 §8 갈래를 고치셨다면 병합 때 그 내용을 `Moment` switch의 같은 종류 줄로 옮겨 주세요. 새 사실 사건 소리를 더할 때는 형식 표 줄에 순간을 채우고 `Moment`에 한 줄 더하면 됩니다(이펙트 큐도 같은 순간을 읽습니다).
