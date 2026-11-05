# 전투 소리 창구 2차 — 오브젝트·장애물·추락·약한 지반·토네이도·아이템 설계 초안

> 날짜: 2026-10-27
> 상태: 결정 완료(2026-10-28, §4.1). 코드·에셋 변경은 없다. §4.2는 질문 풀이 기록이다. Task 계획: [environment-item-sound-plan](../plans/2026-10-28-combat-environment-item-sound-plan.md).
> 요청(사용자): 소리 창구가 없는 곳을 정리한 뒤, 이번 범위를 아래로 정했다.
> - 1번(오브젝트 발동·습득·생성, 장애물 소리 프리셋)은 지금 한다.
> - 추락·약한 지반 붕괴·토네이도(휩쓸릴 때·내려올 때 두 곳)·아이템 발동을 진행한다.
> - 특성에서 나오는 연출(포효·소환·가시·재생·끌어오기·넉백·표식 등)은 따로 뺀다.
> - 진행·결과(턴·승패 등)와 입력 소리는 이번 범위 밖이다.
>
> 관련: [투사체·피격 소리 설계](2026-10-27-combat-projectile-hit-sound-design.md)(1차 — 무엇/언제 분리, 슬롯 연결 대기 P9), AudioSystem 요청 메모 [audio-request](../handoffs/2026-10-27-combat-projectile-hit-sound-audio-request.md)

## 1. 확인한 사실

### 1.1 지금 사실 칸 사건의 소리가 나는 길

- 사실 칸 사건(처리기가 연출을 먼저 시작하지 않는 사건)은 사건 Presenter의 틀이 사건 순간에 `_sound.Play(e)`를 부른다. 소리 표 `CombatEventSounds.For(e)`가 무엇을 낼지 답한다(`CombatSceneEventPresenter.PresentEvent`).
- 장애물 반응·토네이도 발동·기차 호출이 이미 이 길로 소리를 낸다.
- 프리셋 번호는 `CombatSoundPresets`가 답한다. 표시 표 `CombatDisplayTables`가 한 번 만들고, 이 표는 이미 유닛·오브젝트·지형·아이템 외형 표와 설정 표를 모두 갖고 있다.

### 1.2 사건별

| 소리 | 사건(Core) | 지금 | 외형·값의 자리 |
|---|---|---|---|
| 오브젝트 발동·습득 | `CombatObjectActivationEvent`(원본 종류 `ObjectContentKind` = Mine·Flag·SheriffBadge, 발동한 유닛) | 소리 없음(사실 처리기가 오브젝트를 숨김) | 오브젝트 외형 `ObjectVisualDataSO`(종류별, 우리 파일) |
| 오브젝트 생성 | `CombatObjectCreatedEvent` | 소리 없음. **지금 Core가 이 사건을 내는 곳이 없다**(2026-10-28 계획 작성 때 확인, 형식 정의뿐) | 같은 외형 |
| 장애물 피해·파괴 | `CombatObstacleReactionEvent`(`ObstacleContentKind` = PowderBarrel·Supplies, Hit·Destroyed) | `ObsDamaged`·`ObsBroken`을 **프리셋 없이** 냄(모든 장애물이 같은 소리) | 장애물은 구조물 사실이고, 외형은 **유닛 외형 표**(`ObstacleContentProfile.KeyFor(종류)` 키)다. 유닛 외형에는 `unitSoundPreset`이 이미 있다 |
| 추락 | `CombatFallEvent`(사실 칸) → 이어서 `Died` | 추락 사건은 소리 없음. 사망은 추락 조립(가라앉기·작아지기)이 사망 소리를 냄 | 설정 표의 "낭떠러지 추락 연출 (임시)" 묶음(우리) |
| 약한 지반 붕괴 | `CombatMovementSourceEvent`(정적 지형의 종류가 약한 지반 → 낭떠러지로 바뀜, 사실 처리기가 칸 외형을 바꿈) | 소리 없음 | 지형 외형 `TerrainVisual_WeakGround`(호진님·우리 파일) |
| 토네이도 휩쓸림 | `Relocated` + `TornadoSourceId` → 호진님 `TryBeginTornado` 조립(들어 올림 조각) 시작 | 소리 없음. 토네이도 **발동**은 `Tornado` 슬롯이 이미 냄 | 토네이도 연출 설정 `TornadoPresentationSettingsSO`(호진님, 지형 외형 안) |
| 토네이도 착지 | 같은 조립의 완료 콜백(`_sound.Play(relocated)`) | 부르는 자리는 있으나 소리 표가 `Relocated`에 "없음"을 돌려줌 | 같은 설정 |
| 아이템 발동 | `CombatItemEvent`(`ItemActivated`, 아이템 키·소지자·단계) | 소리 없음(사실 처리기가 장착 칸을 바꿈) | 아이템 외형 `ConsumableItemVisual`(우리) |

### 1.3 구조물(사용자 "지금 있음, 유닛이랑 같음")

- 본부 같은 구조물의 피해·파괴는 `CombatStructureEvent`다. 사실 처리기는 사실만 반영하고, 소리 표에는 이 사건의 갈래가 **없다**. 그래서 지금 본부 피해·파괴는 소리가 나지 않는다.
- 같은 구조물이라도 장애물(화약통·보급품)은 장애물 반응 사건으로 소리가 난다.
- 사용자 말씀과 코드가 달라 Q-ES0으로 물었다. → 2026-10-28 사용자: 그대로 둔다(필요할 때 사용자가 직접 넣음).

### 1.4 AudioSystem 슬롯

장애물(`ObsDamaged`·`ObsBroken`)만 슬롯이 있다. 나머지는 새 `SfxEvent`가 필요하다. 1차와 같이 소리 표의 슬롯 칸을 연결 대기(null)로 두고 미리 연결할 수 있다(1차 계획 P9).

## 2. 설계

### ES1 무엇(프리셋 주인) — 1차 규칙 "무엇은 그 사실의 주인"

| 소리 | 슬롯(요청 안) | 프리셋 주인 |
|---|---|---|
| 오브젝트 발동·습득(지뢰·깃발·보안관 배지 공용) | `ObjectActivated`(새) | 오브젝트 외형 칸 `objectSoundPreset` 하나(유닛 `unitSoundPreset`과 같은 꼴, Q-ES2″) |
| 오브젝트 생성 | `ObjectCreated`(새) | 같은 `objectSoundPreset`(유닛이 `UnitPick`·`UnitDead`에 한 번호를 쓰는 것과 같음, Q-ES2″) |
| 장애물 피해·파괴 | `ObsDamaged`·`ObsBroken`(있음) | 그 장애물의 유닛 외형 `unitSoundPreset`(새 칸 없음, Q-ES3) |
| 추락 | `Fall`(새) | 설정 표 "전투 소리 프리셋" 묶음 `fallSoundPreset`(Q-ES4, 자리는 Q-ES5′) |
| 약한 지반 붕괴 | `GroundCollapse`(새) | 설정 표 "전투 소리 프리셋" 묶음 `weakGroundCollapseSoundPreset`(Q-ES5′) |
| 토네이도 휩쓸림·착지 | `TornadoLift`·`TornadoLand`(새) | 토네이도 연출 설정 칸 `liftSoundPreset`·`landSoundPreset`(Q-ES6′) |
| 아이템 발동 | `ItemActivated`(새) | 아이템 외형 칸 `itemSoundPreset`(구현 때 이름을 바꿈, 계획 P1·§6) |

- 칸 꼴은 1차와 같다: `[SerializeField, Min(-1)] int … = -1`(-1 = 소리 없음). 검사는 수 규칙 `CombatPresentationValueRules.RequireSoundPreset(allowNone: true)` 하나다.

### ES2 언제

- **사실 칸 사건**(오브젝트 발동·생성, 장애물, 추락, 약한 지반 붕괴, 아이템 발동)은 지금 틀의 길 그대로 **사건 순간**에 낸다. 시점·지연 칸은 두지 않는다(Q-ES7).
  - 추락은 추락 사건 순간(떨어지기 시작)에 나고, 이어지는 사망 조립이 사망 소리를 낸다.
  - 아이템은 동시 묶음 규칙상 소지자의 묶음에 붙어 그 순간에 난다.
- **토네이도**
  - 휩쓸림: 들어 올림 조립의 시작 항목(소리 조각 `AtStart`)이다.
  - 착지: 지금 완료 콜백 자리이고, 그 자리가 휩쓸림이 아닌 착지 소리를 내게 바꾼다.
  - 두 자리 모두 호진님 `TryBeginTornado` 안이다(공유 대상).

### ES3 프리셋 답 — `CombatSoundPresets`에 함수 더하기

- `Object(ObjectContentKind)`·`Obstacle(ObstacleContentKind)`·`Fall()`·`WeakGroundCollapse()`·`Item(아이템 키)`. 토네이도 둘은 여기 두지 않았다(계획 P2, §6).
- 생성자는 오브젝트·아이템 외형 표를 더 받는다(지형 표는 받지 않음, 계획 P2). 표시 표가 넘기므로 Factory는 바뀌지 않는다.
- 오브젝트는 `Object(ObjectContentKind)` 하나가 발동·습득·생성 모두에 답한다(유닛 `Unit(key, owner)`와 같은 꼴). 종류에 따라 슬롯을 고르는 규칙은 없다.
- 외형 표에 없는 키는 지금처럼 소리 없음이다.

### ES4 소리 표

- `For(e)`에 사건 갈래를 더한다. 토네이도 둘은 사건 하나에 소리가 둘이라, 슬롯 함수(1차 `ProjectileImpact`와 같은 꼴)로 부른다.
- 새 슬롯 칸은 연결 대기(null)이고, 슬롯이 생기면 칸만 채운다(1차 P9).

### ES5 장애물의 "프리셋 없음"

- 장애물 외형의 `unitSoundPreset`이 -1이면 **지금처럼 프리셋 없이 낸다.** 지금 동작을 그대로 지키기 위해서다.
- 그래서 유닛 소리의 "-1 = 소리 없음"과 뜻이 다르다. 소리 표의 장애물 갈래에 한 줄로 적는다(Q-ES3 B를 고르면 이 예외는 없음).

## 3. 영향(초안)

- **DB(우리 파일)**
  - 오브젝트 외형 칸 하나(`objectSoundPreset`, 에셋 셋: 지뢰·깃발·보안관 배지)
  - 아이템 외형 칸 하나
  - 설정 표 "전투 소리 프리셋" 묶음 칸 둘(추락·약한 지반 붕괴, Q-ES5′ 권장)
  - 토네이도 연출 설정 칸 둘(호진님 파일, 공유)
- **View**
  - 소리 표: 갈래·슬롯 칸·함수
  - `CombatSoundPresets`: 함수·생성자
  - 표시 표: 생성 한 줄
  - 호진님 `TryBeginTornado`: 휩쓸림 항목·착지 소리
- **에셋:** 새 칸은 기본값(-1)으로 읽혀 바꿀 것이 없다. 프리셋 번호는 FMOD 쪽 답을 받은 뒤 넣는다.
- **시험:** 남은 시험 가운데 바뀌는 것이 있는지는 계획에서 다시 센다. `Assets/Tests`의 큰 묶음은 사용자 의도로 지워졌다. 새 시험은 없다.
- **공유:** 호진님(토네이도 설정 칸·조립 두 자리), 현강님(소리 표·프리셋 함수), AudioSystem 담당(슬롯 요청 추가).
- **동작 변화:** 장애물은 ES5로 그대로다. 나머지는 슬롯이 연결 대기라 소리 변화가 없다.

## 4. 정할 것

### 4.1 정한 것(2026-10-28 사용자)

- **Q-ES0 = 그대로 둔다.** 구조물(본부 등)은 사용자가 필요할 때 직접 만들어 넣는다. 이번 범위에서 뺀다(§1.3 사실은 기록으로 남김).
- **Q-ES1 = A.** 오브젝트 생성 소리를 넣는다(슬롯 `ObjectCreated`).
- **Q-ES3 = A.** 장애물은 유닛 외형 `unitSoundPreset`을 다시 쓴다(-1이면 지금처럼 프리셋 없이, ES5).
- **Q-ES4 = A.** 추락 프리셋은 설정 표 `CombatPresentationSettingsData`의 값 하나다(사용자 이해와 같음).
- **사용자 지시:** 발동과 습득은 다른 소리가 나야 한다. 그래서 Q-ES2는 구분하는 방법을 다시 묻는다(§4.2).
- **Q-ES5′ = A**(약한 지반·추락 프리셋은 설정 표 "전투 소리 프리셋" 묶음), **Q-ES6′ = A**(토네이도는 `TornadoPresentationSettingsSO` 칸 둘), **Q-ES7′ = A**(지연 칸 없음). 모두 2026-10-28 사용자.
- **Q-ES2″(사용자 방향 "오브젝트마다 먹는 소리가 달라야 함, 유닛 사운드처럼"):** 오브젝트 외형 칸 `objectSoundPreset` 하나를 유닛 `unitSoundPreset`처럼 쓴다. 슬롯은 발동·습득 공용 `ObjectActivated` 하나이고, 오브젝트마다 다른 소리는 프리셋 번호가 가른다. 생성 슬롯 `ObjectCreated`도 같은 번호를 쓴다(2026-10-28 사용자 "같은 번호로" — `createSoundPreset` 칸은 두지 않는다). 그래서 Q-ES2′의 슬롯 둘·종류 규칙·"깃발도 습득인가" 확인은 필요 없다.
  - 오브젝트 외형은 종류마다 에셋 하나(`ObjectVisual_Mine`·`_Flag`·`_SheriffBadge`)다. "오브젝트마다"는 종류마다이고, 같은 종류(예: 지뢰 여럿)는 같은 소리다.

### 4.2 남은 것(사용자 질문을 풀어 다시 씀)

**말 풀이**
- **설정 표** = `CombatPresentationSettingsData`(우리 파일, 에셋 `CombatPresentationSettings_Default.asset`). 추락 연출 값(`fallSeconds` 등)과 "전투 소리 프리셋" 묶음(적 유닛 소리·배경음 프리셋 넷)이 이미 여기 있다.
- **토네이도 연출 설정** = `TornadoPresentationSettingsSO`(호진님 파일, 에셋 `TerrainVisual/Item/TornadoPresentation_Default.asset`). 토네이도 지형 외형이 `tornadoPresentation` 칸으로 참조하고, 다가가기·들어 올리기·내려오기의 시간·높이·곡선이 모여 있다. 설정 표와는 다른 SO다.
- **지연 칸** = 사건 순간에서 몇 초 뒤에 소리를 낼지 정하는 값이다. 1차 투사체 SO의 `flightSoundDelay`(발사 뒤 N초에 비행 소리)·`impactSoundDelay`가 그 예다.

| 질문 | 권장 | 다른 안 | 이유 |
|---|---|---|---|
| **Q-ES2′** 발동과 습득을 어떻게 다르게 | **A 슬롯 둘**: `ObjectActivated`(지뢰) · `ObjectPickedUp`(깃발·보안관 배지). 종류 → 슬롯은 코드 규칙 한 곳, 프리셋은 오브젝트 외형 칸 하나 | B 슬롯 하나 + 종류별 프리셋(옛 A) / C 오브젝트 외형에 "발동·습득" 고르는 칸 | Core는 세 종류 모두 유닛이 칸에 들어설 때 같은 사건 `CombatObjectActivationEvent`를 낸다(`MovementEnvironmentCalculator.cs:49`). 다른 것은 원본 종류뿐이다. 프리셋은 한 슬롯(FMOD 이벤트 하나) 안의 파라미터 프리셋이라, B로도 소리는 달라지지만 FMOD 이벤트는 하나다. 발동·습득을 다른 FMOD 이벤트로 만들려면 A다. 종류는 셋으로 고정이라 규칙은 코드 한 곳이면 된다. **확인: 깃발도 "습득"인가** |
| **Q-ES5′** 약한 지반 붕괴 주인 | **A 설정 표 "전투 소리 프리셋" 묶음에 값 하나**(`weakGroundCollapseSoundPreset`) | B 지형 외형 칸(옛 A) / C 프리셋 없이 슬롯 소리만 | 지형 외형 SO는 모든 지형 종류(낭떠러지·고지대·벽·토네이도…)가 같이 쓴다. 칸을 더하면 다른 지형 에셋에도 보이고, `tornadoPresentation`처럼 "약한 지반만" 검사가 하나 더 붙는다. 약한 지반은 종류가 하나라 값 하나면 충분하다. 무너지는 지형 종류가 늘면 그때 B로 옮긴다. **추락 소리도 같은 묶음에 둔다**(Q-ES4의 "추락 연출 값 옆"과 같은 SO, 자리만 소리 묶음) |
| **Q-ES6′** 토네이도 주인 | **A 토네이도 연출 설정 칸 둘**(`liftSoundPreset`·`landSoundPreset`, 호진님 파일, 공유) | B 설정 표 소리 묶음에 둘(우리 파일만) / C 프리셋 없이 슬롯 소리만 | 토네이도 연출 값이 이미 그 SO에 모여 있다. 토네이도 전용 SO라 Q-ES5′의 "다른 종류에도 보이는" 문제가 없다. B는 호진님 파일을 안 고치지만 토네이도 값이 두 SO로 나뉜다 |
| **Q-ES7′** 지연 칸 | **A 없음**(사건 순간에 바로 냄) | B 소리마다 지연 칸 | 사실 칸 사건은 지금 그 순간에 `Play`를 바로 부르는 길이다. 지연을 넣으려면 기다렸다 내는 장치(건너뛰기·취소 처리 포함)를 따로 만들어야 한다. 이번 요청에는 지연이 없었다 |

## 5. Phase(안)

- **ES-a** 값 칸(DB). 소리 변화는 없다.
- **ES-b** 연결: 소리 표·프리셋·토네이도 두 자리. 슬롯은 연결 대기이고, 장애물은 프리셋이 들어가면 바로 바뀐다.
- **ES-c** 문서·공유·AudioSystem 요청 메모 추가·묶음 끝 검토 하나.

## 6. 구현 메모(2026-10-28, ES-a~ES-c, 컴파일 미확인)

실행 기록은 [계획](../plans/2026-10-28-combat-environment-item-sound-plan.md) §5다. 이 절은 설계와 달라진 것과 코드에서 정한 것만 적는다.

- **설계와 다른 것 둘(계획 P1·P2)**
  - 아이템 외형 칸 이름은 `itemSoundPreset`이다(ES1의 `activateSoundPreset` 대신). 유닛 `unitSoundPreset`·오브젝트 `objectSoundPreset`과 같은 꼴이다.
  - 토네이도 프리셋은 `CombatSoundPresets`를 거치지 않는다. 들어 올림 조립이 들어 올림 대상 `CombatTornadoLiftTarget.Settings`에서 바로 읽는다(ES3의 "지형 표를 더 받는다"를 하지 않음). 1차 투사체 소리가 공격 값에서 바로 읽는 것과 같은 꼴이다.
- **ES1 표의 오브젝트 줄은 결정 Q-ES2″대로 칸 하나(`objectSoundPreset`)다.** 발동·습득 공용 슬롯 `ObjectActivated`와 생성 슬롯 `ObjectCreated`가 같은 번호를 쓴다. 종류에 따라 슬롯을 고르는 규칙은 없다.
- **장애물(ES5):** 소리 표의 `Obstacle(...)` 도우미 한 곳이 "-1이면 프리셋 없이"를 맡는다. 유닛 외형 `unitSoundPreset` 툴팁에 장애물에서의 뜻을 한 문장 적었다.
- **약한 지반 붕괴(계획 P3):** 소리는 "앞 정적 지형 = WeakGround, 뒤 = Cliff"일 때만 난다. 사실 처리기의 칸 외형 교체(종류가 바뀌면 언제나)와 조건이 다르다.
- **함수 이름:** 붕괴 프리셋 함수는 `WeakGroundCollapse()`다(ES3 초안의 `GroundCollapse()`). 슬롯 이름(요청 안)은 `GroundCollapse` 그대로다.
- **찾은 것:** Core는 오브젝트 생성 사건 `CombatObjectCreatedEvent`를 지금 내지 않는다. 생성 소리는 연결만 되어 있다.
- **묶음 끝 검토(계획 §5.3):** 약 8.9, 높음·중간 0, 낮음 8.
- **검토 반영(계획 §5.4, 2026-10-28 사용자 "권장대로 반영"):**
  - 아이템 소리는 "아이템이 쓰인(소모된) 순간"이다. 효과가 겹쳐 수치 없이 소모만 된 경우도 같은 소리이고, 판정은 Core 공개 속성 `CombatItemEvent.Activated` 하나다(L1).
  - 추락 소리는 유닛·구조물(장애물) 공용이다(L2).
  - 오브젝트 외형·토네이도 설정 Data의 새 인자는 필수 인자다(L7, 계획 P9 바꿈).
  - 토네이도 휩쓸림 소리는 조립 시작, 곧 중심으로 다가가기 시작에 난다(L8 문구).
- **공유(모두 미전달):** AudioSystem 요청 메모 §5(슬롯 일곱·장애물 프리셋 안내), 호진님 설정 표 메모 §8(설정 표 칸 둘·토네이도 설정 칸 둘·`TryBeginTornado` 두 자리), 현강님 사건 소리 메모 §8(소리 표 갈래·프리셋 함수).
