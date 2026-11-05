# 전투 소리 창구 2차 — Task 계획

> 날짜: 2026-10-28
> 상태: ES-a~ES-c 완료(2026-10-28, 컴파일 미확인, Ua·Ub 확인 대기, §5.1~§5.3). P1~P10(§1)은 계획대로 진행했다. 묶음 끝 검토 약 8.9(높음·중간 0, §5.3), 권장 반영 완료(§5.4).
> 설계: [environment-item-sound-design](../specs/2026-10-27-combat-environment-item-sound-design.md)(ES1~ES5, 결정 §4.1)
> 1차: [투사체·피격 소리 계획](2026-10-27-combat-projectile-hit-sound-plan.md)(슬롯 연결 대기 P9의 원본)

## 0. 진행 규칙

- 전투 View 작업이라 MSBuild를 돌리지 않고 새 시험을 쓰지 않는다. 보고에는 "컴파일 미확인"을 적는다.
- 남은 시험은 컴파일이 깨지는 곳만 최소로 맞춘다. 지운 `Assets/Tests` 시험은 되살리거나 맞추지 않는다.
- Phase가 끝나면 멈추고 보고한다. Unity 확인은 사용자가 한다.
- 검토 에이전트는 묶음 끝(ES-c)에 한 번만 돌린다.
- 커밋·push는 사용자가 한다.
- Framework·GlobalSystem(AudioSystem 포함)·Core는 고치지 않는다.

### 0.1 결정(설계 §4.1, 2026-10-28 사용자)

| 질문 | 결정 |
|---|---|
| Q-ES0 구조물 | 그대로 둔다(필요할 때 사용자가 직접) |
| Q-ES1 오브젝트 생성 | 넣는다 |
| Q-ES2″ 오브젝트 소리 | 유닛처럼 외형 칸 `objectSoundPreset` 하나. 발동·습득 공용 슬롯 하나, 생성 슬롯도 같은 번호 |
| Q-ES3 장애물 | 유닛 외형 `unitSoundPreset` 다시 씀. -1이면 지금처럼 프리셋 없이 |
| Q-ES4·Q-ES5′ 추락·약한 지반 | 설정 표 `CombatPresentationSettingsData`의 "전투 소리 프리셋" 묶음에 값 하나씩 |
| Q-ES6′ 토네이도 | 호진님 `TornadoPresentationSettingsSO`에 칸 둘 |
| Q-ES7′ 지연 칸 | 없음. 사건 순간에 바로 낸다 |

### 0.2 확인한 사실(계획 작성 때)

- 이번 사건 다섯(오브젝트 발동·생성, 추락, 지형 원본 변화, 아이템, 장애물)은 모두 사건 형식 표의 사실 칸이다. 사건 Presenter 틀이 사건 순간에 `_sound.Play(e)`를 부르므로 Presenter는 바꾸지 않는다(`CombatSceneEventPresenter.PresentEvent`).
- 걸음 중 오브젝트 발동은 걸음 접촉 Gate가 닿는 순간에 사건을 시작한다(H-4b). 소리도 그 순간이다.
- 오브젝트 외형은 종류마다 에셋 하나(`ObjectVisual_Mine`·`_Flag`·`_SheriffBadge`)이고, 표 조회는 `IObjectVisualTableQuery.TryGetObjectVisual(kind)`이다. 원본 종류는 `EnvironmentVisualKinds.ObjectKind(source)`가 답한다.
- 아이템 외형은 넷(`Lantern`·`Roket`·`Rope`·`Whiskey`)이고, 조회는 `IConsumableItemVisualTableQuery.TryGet(itemKey)`이다. Core는 아이템 사건 종류를 단계로 정한다. BeforeDamage·AfterDamage·AttackCompleted가 `ItemActivated`이고, 장착 변경·버림은 다른 종류다(`CombatItemEvent.cs:101`).
- 장애물 유닛 외형 `UnitVisual_PowderBarrel`(키 21)·`UnitVisual_Supplies`(키 22)의 `unitSoundPreset`은 둘 다 -1이다. 그래서 장애물 갈래를 바꿔도 지금 소리는 같다. 키는 Core 공개 규칙 `ObstacleContentProfile.KeyFor(kind)`가 답한다(View가 이미 같은 형식을 씀).
- 약한 지반 붕괴는 `CombatMovementSourceEvent`의 앞·뒤가 정적 지형이고 종류가 WeakGround → Cliff로 바뀌는 사건이다. 사실 처리기는 종류가 바뀌면 칸 외형을 바꾼다(`CombatFactEventHandler.cs:74`).
- 토네이도 들어 올림 조립(`CombatCharacterEventHandler.TryBeginTornado`, 호진님 줄)은 들어 올림 대상 `CombatTornadoLiftTarget`이 토네이도 설정 `Settings`를 이미 갖고 있다. 완료 콜백의 `_sound.Play(relocated)`는 소리 표가 `Relocated`에 늘 "없음"을 돌려 지금은 아무 소리도 내지 않는다.
- **오브젝트 생성 사건 `CombatObjectCreatedEvent`는 지금 Core에서 내는 곳이 없다**(형식 정의와 우리 사건 형식 표 줄뿐). 생성 소리는 연결해 두지만, Core가 그 사건을 낼 때까지는 나지 않는다.
- 소리 조각 `CombatEventSoundPiece`는 시작하는 순간 끝난다. 조립에 넣어도 끝나는 시각은 바뀌지 않는다.
- 남은 시험 가운데 바뀌는 형식을 쓰는 것은 천명님 `PullAndMarkPresentationTests` 하나다. 그 시험은 `CombatDisplayTables.From(...).SoundPresets`로 읽기만 해서 컴파일 영향이 없다. **시험 변경은 없다.**

## 1. 계획에서 정한 것(사용자 확인 대상)

| 번호 | 정한 것 | 이유 |
|---|---|---|
| **P1** | 아이템 외형 칸 이름은 `itemSoundPreset`(설계 ES1의 `activateSoundPreset` 대신) | 유닛 `unitSoundPreset`·오브젝트 `objectSoundPreset`과 같은 꼴("주인 + SoundPreset")로 맞춘다. 나중에 아이템 소리 슬롯이 늘어도 같은 번호를 쓸 수 있다 |
| **P2** | 토네이도 프리셋은 `CombatSoundPresets`를 거치지 않는다. 조립이 들어 올림 대상의 `Settings`에서 바로 읽는다(설계 ES3의 "지형 표를 더 받는다"와 다름) | 1차 투사체 소리가 공격 값에서 바로 읽는 것과 같다. 같은 설정 객체를 두 길로 찾지 않는다. 그래서 `CombatSoundPresets`는 지형 표를 받지 않는다 |
| **P3** | 약한 지반 붕괴 소리는 "앞 종류 WeakGround, 뒤 종류 Cliff"일 때만 낸다 | 칸 외형은 어떤 종류 변화든 바꾸지만, 소리는 붕괴에만 낸다. 다른 지형 변화가 생겨도 붕괴 소리가 잘못 나지 않는다 |
| **P4** | 토네이도 착지 소리는 완료 콜백의 `_sound.Play(relocated)`를 착지 단서로 바꾼다. 소리 표에 `Relocated` 갈래는 만들지 않는다 | 지금 그 줄은 늘 "없음"이다. 착지는 토네이도 조립만의 일이다 |
| **P5** | 휩쓸림 소리는 같은 조립에 소리 조각 하나를 `AtStart`로 넣는다 | 사망 조립의 사망 소리와 같은 꼴이다. 소리 조각은 바로 끝나서 끝나는 시각이 같다 |
| **P6** | 장애물 갈래: 외형 번호가 0 이상이면 프리셋과 함께, -1이거나 외형이 없으면 지금처럼 프리셋 없이 낸다. 적 공용 프리셋 규칙은 적용하지 않는다 | 설계 ES5. 장애물은 적 유닛이 아니다 |
| **P7** | 새 슬롯 칸 일곱은 연결 대기(null)다. 이름(요청 안)은 `ObjectActivated`·`ObjectCreated`·`Fall`·`GroundCollapse`·`TornadoLift`·`TornadoLand`·`ItemActivated` | 1차 P9와 같다. 슬롯이 생기면 칸만 채운다 |
| **P8** | 새 DB 칸은 모두 `[SerializeField, Min(-1)] int … = -1`이고, 검사는 수 규칙 `RequireSoundPreset(allowNone: true)` 하나다(유닛 외형처럼 설정 Data의 `ValidateSoundPreset`을 부름) | 1차와 같은 꼴. 기존 에셋은 칸이 없어 -1로 읽혀 바꿀 것이 없다 |
| **P9** | Data 생성자에 칸을 더할 때는 끝 선택 인자(기본 -1)로 붙인다(`ObjectVisualData`·`TornadoPresentationSettingsData`) → **검토 L7 반영으로 필수 인자로 바꿈(§5.4)** | 다른 생성 호출이 없지만, 1차와 같은 꼴로 다른 사람 호출을 깨지 않게 둔다 |
| **P10** | 오브젝트 생성 소리의 종류도 `EnvironmentVisualKinds.ObjectKind(source)`로 구한다 | 발동과 같은 판정 한 곳 |

## 2. Phase

### ES-a 값 칸(DB) — 소리 변화 없음

| Task | 파일 | 내용 |
|---|---|---|
| Ea.1 | `DataBaseSystem/Data/View/Table/Object/Item/ObjectVisualDataSO.cs`(CRLF) | SO 칸 `objectSoundPreset`(툴팁: 발동·습득·생성 공용, -1 = 소리 없음), Data 생성자 끝 선택 인자·속성 `ObjectSoundPreset`·검사 |
| Ea.2 | `DataBaseSystem/Data/View/Table/ConsumableItem/Item/ConsumableItemVisualDataSO.cs`(CRLF) | 칸 `itemSoundPreset`(툴팁: 발동 소리), 속성 `ItemSoundPreset`, `Validate`에 검사 |
| Ea.3 | `DataBaseSystem/Data/View/Table/CombatPresentationSettings/Item/CombatPresentationSettingsDataSO.cs`(Data와 SO가 한 파일) | "전투 소리 프리셋" 묶음에 `fallSoundPreset`·`weakGroundCollapseSoundPreset`, 속성 둘, 검사 둘 |
| Ea.4 | `DataBaseSystem/Data/View/Table/Terrain/Item/TornadoPresentationSettingsSO.cs`·`TornadoPresentationSettingsData.cs`(호진님, CRLF) | SO 칸 `liftSoundPreset`·`landSoundPreset`, Data 생성자 끝 선택 인자 둘·속성·검사 |

- 에셋은 바꾸지 않는다. 프리셋 번호는 FMOD 쪽 답을 받은 뒤 사용자가 넣는다.
- **Ua 확인(사용자):** 컴파일, 오브젝트 외형 셋·아이템 외형 넷·설정 표·토네이도 설정 인스펙터에 새 칸이 -1로 보임, 전투 진입(DB 생성) 문제 없음.

### ES-b 연결(View) — 슬롯이 연결 대기라 소리 변화 없음

| Task | 파일 | 내용 |
|---|---|---|
| Eb.1 | `ViewSystem/Combat/Playback/Support/CombatSoundPresets.cs` | 생성자에 오브젝트·아이템 외형 표 추가. 함수 `Object(kind)`·`Obstacle(kind)`·`Item(itemKey)`·`Fall()`·`WeakGroundCollapse()`(표에 없으면 소리 없음) |
| Eb.2 | `ViewSystem/Combat/CombatDisplayTables.cs` | 생성 한 줄(`Objects`·`Items`를 넘김) |
| Eb.3 | `ViewSystem/Combat/Playback/Support/CombatEventSounds.cs`(CRLF) | 슬롯 칸 일곱(null), 갈래: 오브젝트 발동·생성, 추락, 약한 지반 붕괴(P3), 아이템 발동(`ItemActivated`만), 장애물 프리셋(P6). 슬롯 함수 `TornadoLift(preset)`·`TornadoLand(preset)` |
| Eb.4 | `ViewSystem/Combat/Playback/Handlers/CombatCharacterEventHandler.cs`(호진님 줄) | `TryBeginTornado`: 휩쓸림 소리 조각 `AtStart`(P5), 완료 콜백의 `_sound.Play(relocated)` → 착지 단서(P4) |

- Factory·Presenter·ServiceKeys는 바뀌지 않는다. 새 파일은 없다.
- **Ub 확인(사용자):** 컴파일. 전투에서 지뢰 밟기, 깃발·보안관 배지 습득, 장애물 피해·파괴, 넉백·끌어오기로 낭떠러지 추락, 약한 지반 붕괴(`Test` 스테이지 칸 (6,3)), 토네이도 휩쓸림·착지, 아이템 발동(위스키 등)에서 예외가 없고 소리가 지금과 같음(장애물 소리 포함).

### ES-c 문서·공유·검토

| Task | 내용 |
|---|---|
| Ec.1 | AudioSystem 요청 메모 [audio-request](../handoffs/2026-10-27-combat-projectile-hit-sound-audio-request.md)에 2차 슬롯 일곱을 더함(미전달) |
| Ec.2 | 호진님 설정 표 메모 `handoffs/2026-10-18-combat-presentation-settings-table-hojin-share.md` §8(설정 표 칸 둘, 토네이도 설정 칸 둘, `TryBeginTornado` 두 자리, 미전달) |
| Ec.3 | 현강님 사건 소리 메모 `handoffs/2026-10-18-combat-event-audio-move-hyeongang-share.md` §8(소리 표 갈래·슬롯 함수, 프리셋 함수, 미전달) |
| Ec.4 | 설계 §6 구현 메모(P1~P10 중 설계와 다른 것: P1·P2) |
| Ec.5 | 묶음 끝 검토 에이전트 하나(7기준), 결과 보고 |
| Ec.6 | CLAUDE.md 현재 상태·메모리 |

## 3. 커밋 묶음(사용자)

- 새 파일이 없다. 바뀐 DB 코드(Ea.1~Ea.4)와 View 코드(Eb.1~Eb.4)를 한 커밋에 넣는 것을 권한다. Eb는 Ea의 속성을 읽으므로 Eb만 따로 커밋하면 컴파일이 깨진다.
- 작업 트리의 다른 변경(`Boot.unity`, FMOD 캐시, `New Fmod Event Catalog.asset`, Addressables 설정, `dummy_background_*.fbx`)은 이번 작업이 아니다.

## 4. 위험

- **R1** 아이템 발동 사건은 Core가 결정 하나마다 하나씩 낸다. 한 공격에서 아이템 둘이 발동하면 소리도 둘이다(의도대로 보임). Ub에서 겹쳐 들리는지 본다(슬롯 연결 뒤).
- **R2** 오브젝트 생성 사건은 지금 Core가 내지 않는다(§0.2). 그래서 생성 소리는 Unity에서 확인할 수 없다. Core가 그 사건을 내기 시작하면(예: 전투 중 오브젝트가 생기는 기능) 그때 소리를 들어 본다. 여러 개가 한꺼번에 생기면 소리가 겹칠 수 있다.
- **R3** `TryBeginTornado`는 호진님 줄이다. 두 자리만 바꾸고 공유 메모에 적는다.

## 5. 실행 기록

### 5.1 ES-a 코드 완료(2026-10-28, 사용자 "ES-a 진행" — P1~P10 계획대로, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, 에셋 변경 없음)

| Task | 한 것 |
|---|---|
| Ea.1 | `ObjectVisualDataSO.cs`: SO 칸 `objectSoundPreset`(-1, 툴팁 "발동·습득·생성 공용"), Data 생성자 끝 선택 인자 `objectSoundPreset = -1`·속성 `ObjectSoundPreset`·검사(`CombatPresentationSettingsData.ValidateSoundPreset`, 유닛 외형과 같은 입구) |
| Ea.2 | `ConsumableItemVisualDataSO.cs`: 칸 `itemSoundPreset`(-1)·속성 `ItemSoundPreset`·`Validate` 한 줄(표가 만들 때 부름) |
| Ea.3 | `CombatPresentationSettingsDataSO.cs`: "전투 소리 프리셋" 묶음 끝에 `fallSoundPreset`·`weakGroundCollapseSoundPreset`(-1), 속성 둘, `Validate` 맨 뒤 검사 둘(앞 값들이 먼저 알리는 순서 유지), `ValidateSoundPreset` 문서 한 줄 |
| Ea.4 | 호진님 `TornadoPresentationSettingsSO.cs`: 머리말 "소리" 아래 `liftSoundPreset`·`landSoundPreset`(-1), `GetData` 끝 인자 둘. `TornadoPresentationSettingsData.cs`: 생성자 끝 선택 인자 둘·속성 `LiftSoundPreset`·`LandSoundPreset`·검사 |

- 다섯 파일 모두 CRLF를 유지했다(CR 수 = LF 수 확인).
- 기존 에셋은 새 칸이 없어 -1(소리 없음)로 읽는다. 소리 변화는 없다.
- 계획과 다른 점: 없음. 장애물 프리셋을 유닛 외형 `unitSoundPreset`으로 쓴다는 툴팁 문구는 동작이 바뀌는 ES-b(Eb.3)에서 고친다.
- **Ua 확인 목록(사용자):**
  1. 컴파일 오류 없음.
  2. 인스펙터에서 새 칸이 -1로 보임: 오브젝트 외형 셋(`ObjectVisual_Mine`·`_Flag`·`_SheriffBadge`), 아이템 외형 넷(`ConsumableItemVisual_*`), 설정 표 `CombatPresentationSettings_Default`("전투 소리 프리셋" 묶음 끝), 토네이도 설정 `TornadoPresentation_Default`("소리" 묶음).
  3. 전투 진입(부트 DB 생성) 문제 없음.

### 5.2 ES-b 코드 완료(2026-10-28, 사용자 "ES-b 진행" — ES-a Ua는 따로 보고받지 않음, 컴파일 미확인, 새 시험 없음, 시험 변경 없음, 에셋 변경 없음)

| Task | 한 것 |
|---|---|
| Eb.1 | `CombatSoundPresets.cs`: 생성자 `(visuals, objects, items, settings)`, 함수 `Obstacle(kind)`(장애물 키 = Core `ObstacleContentProfile.KeyFor`, 적 공용 규칙 안 씀)·`Object(kind)`·`Item(itemKey)`·`Fall()`·`WeakGroundCollapse()`. 클래스 문서에 토네이도는 여기서 답하지 않는다고 적음(P2) |
| Eb.2 | `CombatDisplayTables.cs`: 생성 한 줄 `new CombatSoundPresets(Units, Objects, Items, PresentationSettings)`, 문서 한 줄 |
| Eb.3 | `CombatEventSounds.cs`: 슬롯 칸 일곱(null), `For(e)` 갈래 다섯(오브젝트 발동·생성 → `EnvironmentVisualKinds.ObjectKind`, 추락, 약한 지반 붕괴 `IsWeakGroundCollapse`, 아이템 `ItemActivated`만), 장애물 갈래를 `Obstacle(...)` 도우미로(P6), 슬롯 함수 `TornadoLift`·`TornadoLand`. using `Display.Map` 하나 |
| Eb.4 | 호진님 `CombatCharacterEventHandler.TryBeginTornado`: 프리셋 둘을 `target.Settings`에서 읽고, 휩쓸림 소리 조각을 `AtStart`로 추가(P5), 완료 콜백의 `_sound.Play(relocated)`를 `_sound.Play(CombatEventSounds.TornadoLand(...))`로(P4) |
| 툴팁 | `UnitVisualDataSO.cs`의 `unitSoundPreset` 툴팁·속성 문서에 장애물 뜻(피해·파괴, -1이면 프리셋 없이) 한 문장 |

- 다섯 파일 모두 CRLF를 유지했다. 새 using으로 들어온 형식 이름이 겹치지 않는 것을 확인했다(1차 CS0104 교훈, `ObjectContentKind`·`ObstacleContentKind`·`StaticTerrainKind` 등 모두 한 곳에만 선언).
- 동작: 새 슬롯이 모두 null이라 새 소리는 나지 않는다. 장애물은 외형 둘(키 21·22)이 -1이라 지금처럼 프리셋 없이 낸다. 토네이도 착지 자리는 옛 줄도 소리가 없었다.
- 계획과 다른 점: 없음. 이 Phase의 새 예외 경로는 하나다. 오브젝트 사건의 원본 종류가 `EnvironmentVisualKinds`에 없으면 소리 표에서 예외가 난다. 지금 종류 셋은 모두 있고, 같은 경우 오브젝트 표시가 이미 예외를 낸다.
- **Ub 확인 목록(사용자):**
  1. 컴파일 오류 없음.
  2. 전투에서 지뢰 밟기, 깃발·보안관 배지 습득, 장애물(화약통·보급품) 피해·파괴, 넉백·끌어오기로 낭떠러지 추락, 약한 지반 붕괴(`Test` 스테이지 칸 (6,3)), 토네이도 휩쓸림·착지, 아이템 발동(위스키 등)에서 예외가 없음.
  3. 소리가 지금과 같음(장애물 소리 포함). 토네이도 휩쓸림·착지 연출의 시간·모양이 그대로임.

### 5.3 ES-c 문서·공유·묶음 끝 검토(2026-10-28, 코드 변경 없음)

- **Ec.1** AudioSystem 요청 메모 §5(슬롯 일곱, 장애물 프리셋 안내, 오브젝트 생성 사건 없음, 발동·습득 한 슬롯 질문). 머리말에 "2026-10-28 추가" 한 줄.
- **Ec.2** 호진님 설정 표 메모 §8(설정 표 칸 둘, 토네이도 설정 칸 둘, `TryBeginTornado` 두 자리).
- **Ec.3** 현강님 사건 소리 메모 §8(소리 표 갈래 다섯·장애물 도우미·슬롯 함수 둘·프리셋 함수·생성자).
- **Ec.4** 설계 §6 구현 메모. 검토 L8대로 ES1 아이템 줄·ES3 함수 이름을 코드에 맞춰 고쳤다.
- 공유 셋은 모두 미전달이다. 줄 끝은 각 파일 그대로다(요청 메모 LF, 두 공유 메모 CRLF).

#### 묶음 끝 검토(Ec.5, 새 에이전트 하나, 7기준)

**높음 0, 중간 0, 낮음 8. 합친 점수 약 8.9**(SSOT 8.5 · SOLID 8.5 · FW 9.5 · 독립성 9.0 · 캡슐화 9.0 · C#/Unity 8.5 · If/Tick 9.0). 컴파일을 깨는 곳(이름 모호성·접근성·생성자 호출·남은 시험)과 슬롯이 null인 동안 동작이 바뀌는 곳은 없었다.

| 번호 | 지적 | 확인 | 권장 |
|---|---|---|---|
| L1 | 아이템 "중복 효과"(수치 없이 소모만, `ItemEventReason.DuplicateEffect`)에도 발동 소리가 난다. 요청 메모는 "효과가 발동한 순간"이라 씀 | 맞음(`CombatItemEvent.cs:40·82`) | 결정: A 그대로 냄(아이템이 쓰인 것은 같음) + 조건을 Core 공개 속성 `item.Activated`로 + 메모 문구 고침 / B 실제 발동(`Reason == Activated`)만 |
| L2 | 구조물(장애물)이 낭떠러지에 떨어져도 `CombatFallEvent`가 나와 같은 추락 소리가 난다. 툴팁·메모는 "유닛"만 씀 | 맞음(`MovementProjection.ApplyFall` 구조물 갈래) | 결정: A 같은 소리 + 툴팁·메모를 "유닛·구조물"로 / B 유닛만 |
| L3 | 슬롯이 null이어도 프리셋 조회(`ObjectKind` 변환)를 먼저 한다 | 맞음, 실제로 걸릴 일 없음(§5.2) | 그대로 |
| L4 | 정적 지형 변화 분류가 세 곳(소리 표·사실 처리기·동시 묶음) | 맞음 | 다음 묶음 후보(기록만) |
| L5 | `unitSoundPreset` -1이 유닛·장애물에서 뜻이 다르다, `IsSound` 이중 검사 | 의도(ES5·Q-ES3) | 그대로 |
| L6 | 오브젝트·아이템·토네이도 Data가 설정 표 Data의 검사 입구를 참조(투사체는 수 규칙을 바로 부름), 예외 형식 `ArgumentOutOfRangeException` | 맞음, 유닛 외형 선례(P8) | 그대로(선례와 같게) |
| L7 | `ObjectVisualData`·`TornadoPresentationSettingsData` 생성자의 선택 인자는 부르는 곳이 `GetData()` 하나뿐 → 새 호출이 값을 빠뜨리면 조용히 소리 없음 | 맞음 | 필수 인자로(깨지는 곳 없음, 계획 P9 바꿈) |
| L8 | 문서 어긋남: ES3 `GroundCollapse()` ↔ 코드 `WeakGroundCollapse()`, ES1 `activateSoundPreset`, 계획 머리말·§5.3 없음, 토네이도 휩쓸림 툴팁("들어 올려지기 시작")은 실제로 조립 시작(다가가기 시작)에 남 | 맞음 | 문서 넷은 이 절에서 고침. 툴팁 문구는 반영 묶음에 |

- 반영 묶음(제안): L1 A·L2 A·L7·L8 툴팁. → 2026-10-28 사용자 "권장대로 반영"(§5.4).

### 5.4 검토 반영(2026-10-28, 사용자 "권장대로 반영", 컴파일 미확인, 새 시험 없음, 시험·에셋 변경 없음)

| 번호 | 한 것 |
|---|---|
| L1 A | `CombatEventSounds.For`의 아이템 갈래 조건을 `item.Kind == CombatEventKind.ItemActivated` → Core 공개 속성 `item.Activated`(같은 값, 중복 효과 포함)로, 주석 둘. 아이템 외형 `itemSoundPreset` 툴팁·`CombatSoundPresets.Item` 문서를 "쓰인(소모된) 순간, 중복 효과 포함"으로 |
| L2 A | 추락 갈래 주석, 설정 표 `fallSoundPreset` 툴팁·속성 문서, `CombatSoundPresets.Fall` 문서를 "유닛·구조물(장애물) 공용"으로. 동작은 그대로(구조물 추락도 같은 소리) |
| L7 | `ObjectVisualData` 생성자의 `objectSoundPreset`, `TornadoPresentationSettingsData` 생성자의 `liftSoundPreset`·`landSoundPreset`을 필수 인자로(계획 P9 바꿈). 부르는 곳은 각 SO의 `GetData()` 하나라 깨지는 곳 없음(다시 확인) |
| L8 | 토네이도 `liftSoundPreset` 툴팁·`LiftSoundPreset` 문서를 "휩쓸리는 순간(중심으로 다가가기 시작, 들어 올리기는 그 뒤)"으로 |
| 문서 | AudioSystem 요청 메모 §5.1(`Fall`·`TornadoLift`·`ItemActivated` 줄), 호진님 메모 §8.1·§8.2(필수 인자 안내 포함), 현강님 메모 §8 표 |

- 일곱 파일 모두 CRLF를 유지했다.
- 그대로 둔 것: L3·L5·L6. 다음 묶음 후보: L4(정적 지형 변화 분류를 한 판정 도우미로).
