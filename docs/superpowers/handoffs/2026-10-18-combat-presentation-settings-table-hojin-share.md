# View DB 표 추가 공유: 전투 연출 전역 값 `CombatPresentationSettings` (용근 → 호진님)

- 작성일: 2026-10-18. 작성: 용근(View). 브랜치 `feature/Yonggeun/Ref-EventPresenter`.
- **상태: 전달용 문서. 아직 보내지 않았습니다.**
- 관련: [연출 조립 설계](../specs/2026-10-17-combat-presentation-composition-design.md) §9 I-5, [연출 바탕 계획](../plans/2026-10-18-combat-presentation-foundation-plan.md) §9·§12.8, [가시밭 표 공유](2026-10-14-thorn-field-visual-table-hojin-share.md)(같은 방식의 앞선 공유)

## 1. 요약

전투 연출의 전역 시간·크기 값(걸음·회전 시간, 적 행동·턴 간격, 보스 소환·포효 값)을 `CombatSceneRoot`의 씬 값에서 View DB 설정 표 `CombatPresentationSettings`(설정 하나)로 옮겼습니다. 이 때문에 호진님 시험 파일 6개에 **한 줄씩** 들어갔고, `ViewGroup.asset`에 표가 하나 등록됐습니다. 동작이 바뀌는 곳은 없습니다. 값은 `Combat.unity`의 값 그대로입니다.

## 2. 호진님 파일에서 바뀐 것

`IViewQuery`에 `CombatPresentationSettingsTableQuery` 속성이 생겨, 이 인터페이스를 직접 구현한 시험 가짜에 같은 파일의 가시밭 줄과 같은 모양으로 한 줄을 더했습니다.

| 파일 | 더한 줄 |
|---|---|
| `Tests/EditMode/Framework/ArtifactPopupConfigTests.cs` | `public ICombatPresentationSettingsTableQuery CombatPresentationSettingsTableQuery => null;` |
| `Tests/EditMode/Framework/GlobalInputCompositionTests.cs` | 같음 |
| `Tests/EditMode/Framework/LobbyInputTests.cs` | 같음 |
| `Tests/EditMode/Framework/ViewInputCompositionTests.cs` | 같음 |
| `Tests/EditMode/Framework/LobbyViewSystemTests.cs` | `… CombatPresentationSettingsTableQuery => throw new NotSupportedException();`(기존 가시밭 줄과 같은 방식, 전체 이름) |
| `Tests/EditMode/Framework/ViewPopupCompositionTests.cs` | `… CombatPresentationSettingsTableQuery => throw new NotSupportedException();` |

같은 변경이 `UnityStageInputTests.cs`(용근)에도 들어갔습니다.

## 3. 병합 때 주의

- **`Assets/ProjectL/DataBaseSO/ViewGroup/ViewGroup.asset`:** `databaseTables` 목록 끝에 `- {fileID: 11400000, guid: 0ca5ccef885f41e9963cf3625b9ac7ef, type: 2}` 한 줄이 늘었습니다. 호진님도 표를 추가하셨다면 같은 위치에서 충돌할 수 있습니다. **두 줄 모두 남기면 됩니다**(순서는 상관없음).
- **표 에셋이 없으면 DB 생성이 실패합니다.** 코드만 받고 `DataBaseSO/ViewGroup/CombatPresentationSettings/` 폴더를 빠뜨리지 않게, 한 커밋으로 묶어 올립니다.
- 새 `IViewQuery` 가짜를 만드실 때는 `CombatPresentationSettingsTableQuery`도 구현해야 합니다.

## 4. 새 파일

- 코드:
  - `DataBaseSystem/Data/View/Table/CombatPresentationSettings/`: `CombatPresentationSettingsDataTableSO.cs`, `Item/CombatPresentationSettingsDataSO.cs`
  - `DataBaseSystem/Queries/ViewDataBase/ViewTable/CombatPresentationSettingsTable.cs`
- 에셋: `DataBaseSO/ViewGroup/CombatPresentationSettings/CombatPresentationSettingsDataTable.asset`, `Item/CombatPresentationSettings_Default.asset`
- 등록: `IViewQuery`·`ViewDataBase`·`ViewDataBaseFactory`·`ViewTableKey.CombatPresentationSettings`

## 5. 2026-10-19 후속 — 카메라 연출 값 (카메라 연출 조각 K-3 넷·K-4 여섯·K-5 셋·K-6 셋, 미전달)

- **무엇:** 설정 Data(`Item/CombatPresentationSettingsDataSO.cs`)에 "카메라 연출" 묶음 값 넷을 더했습니다. 포효 때 맵 카메라를 줌하고, 박치기가 맞는 순간 흔드는 값입니다([카메라 연출 조각 설계](../specs/2026-10-19-combat-camera-pieces-design.md) K5·K6).

| 필드 | 코드 기본값 | 검사 |
|---|---|---|
| `roarZoomRatio` 포효 줌 배율(기준 렌즈에 곱함, 1이면 줌 없음) | 0.85 | 유한한 양수(설정 Data 조건 함수) |
| `roarShakeStrength` 흔들림 세기(0이면 흔들림 없음) | 0.4 | 유한한 0 이상(설정 Data 조건 함수) |
| `roarShakeSeconds` 흔들림 시간 | 0.25 | DB에서 검사하지 않음. 카메라 효과가 보낼 때 검사(결정 A) |
| `roarShakeShape` 흔들림 모양(Cinemachine `ImpulseShapes`) | Bump(2) | DB에서 검사하지 않음. Recoil~Rumble만 됨(결정 A) |

- **에셋:** `Item/CombatPresentationSettings_Default.asset`에 네 값을 적었습니다. 코드와 한 커밋입니다.
- **결정 A:** 흔들림 시간·모양의 조건 원본은 민형님 카메라 효과(`CameraShakeEvent`) 한 곳이라 DB 생성에서 거르지 않습니다. 잘못 넣으면 처음 흔들림을 보낼 때 예외가 납니다. 다만 맵 카메라에 흔들림 수신기(Listener)가 붙기 전에는 흔들림을 보내지 않으므로, 그때까지는 잘못된 값이 드러나지 않았습니다. Listener는 사용자가 붙였습니다(`339efa14`).
- **병합 때 주의:** 같은 설정 Data·에셋에 값을 더하셨다면 필드 줄끼리 충돌할 수 있습니다. 두 쪽 줄을 모두 남기면 됩니다.
- **에셋 값:** 위 표는 코드 기본값입니다. 에셋의 포효 흔들림은 사용자가 Unity에서 조정한 값(세기 1·1초·Explosion)입니다.
- **K-4 추가(2026-10-19) — 피격·사망 흔들림 값 여섯:** 유닛이 맞는 순간과 쓰러지기 시작할 때 맵 카메라를 흔드는 값입니다(아군·적 모두, [설계](../specs/2026-10-19-combat-camera-pieces-design.md) K8). 같은 "카메라 연출" 묶음 끝에 있습니다.

| 필드 | 코드 기본값 | 검사 |
|---|---|---|
| `hitShakeStrength` 피격 흔들림 세기(0이면 없음) | 0.15 | 유한한 0 이상(설정 Data 조건 함수) |
| `hitShakeSeconds` 피격 흔들림 시간 | 0.15 | DB에서 검사하지 않음(결정 A) |
| `hitShakeShape` 피격 흔들림 모양 | Bump(2) | DB에서 검사하지 않음(결정 A) |
| `deathShakeStrength` 사망 흔들림 세기(0이면 없음) | 0.3 | 유한한 0 이상(설정 Data 조건 함수) |
| `deathShakeSeconds` 사망 흔들림 시간 | 0.3 | DB에서 검사하지 않음(결정 A) |
| `deathShakeShape` 사망 흔들림 모양 | Explosion(3) | DB에서 검사하지 않음(결정 A) |

- **겹침:** 흔들림이 진행 중이면 그보다 센 흔들림만 나갑니다(포효 > 사망 > 피격 순으로 세기를 두면 같은 순간에 센 쪽 하나만 보입니다). 이 규칙은 코드(카메라 창구)에 있고 값으로 바꾸지 않습니다.
- **에셋:** 여섯 값을 `CombatPresentationSettings_Default.asset`에 적었습니다(에셋 값은 사용자 조정을 따름). 코드와 한 커밋입니다.
- **K-5 추가(2026-10-19) — 기차 흔들림 값 셋:** 기차 앞면이 필드(첫 레일 칸)에 들어오는 순간 맵 카메라를 흔드는 값입니다([설계](../specs/2026-10-19-combat-camera-pieces-design.md) K9). 같은 "카메라 연출" 묶음 끝에 있습니다. 기차 외형 표(`RailwayVisualData`)는 바꾸지 않았습니다.

| 필드 | 코드 기본값 | 검사 |
|---|---|---|
| `trainShakeStrength` 기차 흔들림 세기(0이면 없음) | 0.2 | 유한한 0 이상(설정 Data 조건 함수) |
| `trainShakeSeconds` 기차 흔들림 시간 | 1.5 | DB에서 검사하지 않음(결정 A) |
| `trainShakeShape` 기차 흔들림 모양 | Rumble(4) | DB에서 검사하지 않음(결정 A) |

- 세기를 사망(0.3)보다 약하게 두어, 기차에 치인 유닛의 사망 흔들림도 겹침 규칙을 지나 함께 보입니다. 에셋에 세 값을 적었고 코드와 한 커밋입니다.
- **K-6 추가(2026-10-19) — 기차 줌 값 셋:** 기차 앞면이 필드에 들어오면 살짝 줌인하고, 앞면이 마지막 레일 칸을 지나면 기준 크기로 돌아오는 값입니다([설계](../specs/2026-10-19-combat-camera-pieces-design.md) K10).

| 필드 | 코드 기본값 | 검사 |
|---|---|---|
| `trainZoomRatio` 기차 줌 배율(기준에 곱함, 작을수록 확대, 1이면 줌 없음) | 0.92 | 유한한 양수(설정 Data 조건 함수) |
| `trainZoomInSeconds` 줌으로 들어가는 시간 | 0.3 | 유한한 0 이상(설정 Data 조건 함수) |
| `trainZoomOutSeconds` 기준으로 돌아오는 시간 | 0.4 | 유한한 0 이상(설정 Data 조건 함수) |

- 에셋에 세 값을 적었고 코드와 한 커밋입니다.

## 6. 2026-10-20 후속 — 걸음 뜀 높이 값 하나 (고지대 View HG-3, 미전달)

- **무엇:** 설정 Data의 "유닛 이동 연출" 묶음에 값 하나를 더했습니다. 높이가 다른 칸 사이 걸음(고지대 오르내림·넉백)이 칸 경계에서 높은 칸보다 더 올라가는 높이입니다([고지대 View 설계](../specs/2026-10-20-combat-high-ground-view-design.md) HG-3, 계획 P11).

| 필드 | 코드 기본값 | 검사 |
|---|---|---|
| `stepClimbHop` 걸음 뜀 높이(필드 로컬 단위, 0이면 블록 모서리에 딱 닿음) | 0.1 | 유한한 0 이상(설정 Data 조건 함수 `ValidateStepClimbHop`, 사건 Presenter 생성자도 같은 함수) |

- **에셋:** `Item/CombatPresentationSettings_Default.asset`에 `stepClimbHop: 0.1`을 적었습니다. 코드와 한 커밋입니다.
- **병합 때 주의:** 같은 묶음(`movementTurnSeconds` 바로 뒤)에 줄을 더하셨다면 충돌할 수 있습니다. 두 쪽 줄을 모두 남기면 됩니다.

## 7. 2026-10-27 후속 — 낭떠러지 추락 연출 값 셋 (임시, 미전달)

- **무엇:** 설정 Data에 새 묶음 "낭떠러지 추락 연출 (임시)"을 "보스 능력 연출" 바로 뒤에 더했습니다. 넉백·끌어오기로 낭떠러지에 떨어진 유닛이 사망 반응 대신 가라앉으며 작아집니다(바닥은 `TerrainVisual_Cliff`의 검은 데칼 프리팹, 프리팹 연결은 에디터 작업).

| 필드 | 코드 기본값 | 검사 |
|---|---|---|
| `fallSeconds` 가라앉으며 작아지는 시간(초) | 0.5 | 유한한 0 이상(`ValidateSeconds`) |
| `fallDepth` 가라앉는 깊이(월드 단위, 점점 빨라짐) | 0.6 | 유한한 0 이상(새 `ValidateFallDepth`) |
| `fallEndScale` 끝 크기(시작 크기에 곱함) | 0.05 | 0보다 크고 1 이하(새 `ValidateFallEndScale`, 크기 대여가 0 배율을 받지 않음) |

- **검사 순서:** 세 값의 검사는 `Validate()` 맨 끝에 붙였습니다. 앞 값들이 함께 잘못됐을 때 먼저 알리는 값은 그대로입니다.
- **에셋:** `Item/CombatPresentationSettings_Default.asset`에 세 값을 `roarPeakScale` 바로 뒤에 적었습니다. 코드와 한 커밋입니다.

## 8. 2026-10-28 후속 — 소리 프리셋 넷: 설정 표 둘·토네이도 설정 둘 (소리 창구 2차, 미전달)

근거는 [전투 소리 창구 2차 설계](../specs/2026-10-27-combat-environment-item-sound-design.md)(결정 §4.1)와 [계획](../plans/2026-10-28-combat-environment-item-sound-plan.md) §5입니다. 토네이도 쪽은 호진님 파일이라 따로 적습니다.

### 8.1 전투 연출 설정 표(`CombatPresentationSettingsDataSO.cs`)

"전투 소리 프리셋" 묶음 끝(`afterCompletionBgmPreset` 바로 뒤)에 칸 둘을 더했습니다.

| 필드 | 코드 기본값 | 뜻 | 검사 |
|---|---|---|---|
| `fallSoundPreset` | -1 | 유닛·구조물(장애물)이 낭떠러지로 떨어지기 시작할 때. 유닛의 이어지는 사망 소리는 따로 납니다 | `ValidateSoundPreset`(-1 또는 0 이상) |
| `weakGroundCollapseSoundPreset` | -1 | 약한 지반이 무너져 낭떠러지가 될 때 | 같음 |

- 검사는 `Validate()` 맨 끝(추락 연출 값 뒤)입니다. 앞 값들이 먼저 알리는 순서는 그대로입니다.
- **에셋:** 적지 않았습니다. 칸이 없어 -1(소리 없음)로 읽힙니다. FMOD 번호가 정해지면 넣습니다.

### 8.2 토네이도 연출 설정(호진님 `TornadoPresentationSettingsSO.cs`·`TornadoPresentationSettingsData.cs`)

- **SO:** 곡선 칸 뒤에 머리말 "소리" 아래 칸 둘을 더했습니다. `GetData()`가 끝 인자 둘을 넘깁니다.

| 필드 | 코드 기본값 | 뜻 |
|---|---|---|
| `liftSoundPreset` | -1 | 유닛이 휩쓸리는 순간(중심으로 다가가기 시작, 들어 올리기는 그 뒤) |
| `landSoundPreset` | -1 | 유닛이 목적지에 내려앉을 때 |

- **Data:** 생성자 끝에 **필수 인자** 둘(`liftSoundPreset`, `landSoundPreset`), 속성 `LiftSoundPreset`·`LandSoundPreset`, 검사(전투 연출 설정의 `ValidateSoundPreset`)를 더했습니다. 처음엔 선택 인자(기본 -1)였는데, 새 호출이 값을 빠뜨려도 조용히 "소리 없음"이 되지 않게 묶음 끝 검토 뒤 필수로 바꿨습니다. 생성하는 곳은 SO의 `GetData()` 하나뿐이라 깨지는 곳은 없습니다. 브랜치에서 이 생성자를 새로 부르셨다면 두 값을 넘겨 주세요.
- 토네이도 **발동** 소리(`Tornado` 슬롯, 프리셋 없음)는 그대로입니다.
- **에셋:** `TornadoPresentation_Default.asset`은 고치지 않았습니다(-1로 읽힘).

### 8.3 토네이도 들어 올림 조립(호진님 `CombatCharacterEventHandler.TryBeginTornado`) 두 자리

1. **휩쓸림:** 들어 올림 조각 바로 뒤에 소리 조각 하나를 조립 시작(`AtStart`)에 더했습니다. 프리셋은 들어 올림 대상 `target.Settings.LiftSoundPreset`에서 읽습니다. 소리 조각은 시작하자마자 끝나서 조립이 끝나는 시각은 같습니다.
2. **착지:** 완료 콜백 맨 앞의 `_sound.Play(relocated)`를 `_sound.Play(CombatEventSounds.TornadoLand(landSoundPreset))`로 바꿨습니다. 옛 줄은 소리 표가 재배치 사건에 늘 "없음"을 돌려 소리가 나지 않았습니다. 나머지 순서(세대 확인 → 사실 반영 → 그리기 → 완료)는 그대로입니다.

- 슬롯 `TornadoLift`·`TornadoLand`는 AudioSystem에 요청했고 지금은 비어 있어(null) 소리가 나지 않습니다([요청 메모](2026-10-27-combat-projectile-hit-sound-audio-request.md) §5).
- **병합 때 주의:** 호진님 브랜치에서 `TryBeginTornado`나 토네이도 설정 SO를 고치셨다면 두 쪽 줄을 모두 남기면 됩니다.
- **부탁:** 따로 하실 일은 없습니다.

## 9. 2026-10-28 후속 — 걸음 모양 값 셋: 칸마다 뛰어 이동 (전투 걸음 모양, 미전달)

근거는 [전투 걸음 모양 설계](../specs/2026-10-28-combat-step-motion-style-design.md)(ST4)와 [계획](../plans/2026-10-28-combat-step-motion-style-plan.md) §5.1입니다. 자발 이동(아군·적의 경로 걸음)이 칸마다 한 번씩 뛰게 하고, 그 모양을 설정 표에서 고릅니다. 넉백·끌어오기 같은 밀림은 이 값과 관계없이 늘 걷습니다.

### 9.1 전투 연출 설정 표(`CombatPresentationSettingsDataSO.cs`)

"유닛 이동 연출" 묶음의 `stepClimbHop` 바로 뒤에 칸 셋을 더했습니다.

| 필드 | 코드 기본값 | 뜻 | 검사 |
|---|---|---|---|
| `voluntaryStepLook` | `Hop` | 자발 이동의 걸음 모양. 새 열거 `CombatStepLook { Walk = 0, Hop = 1 }`(같은 폴더 새 파일 `CombatStepLook.cs`). `Walk`면 지금까지처럼 걷습니다(`movementStepSeconds`·`stepClimbHop`) | 새 조건 함수 `ValidateStepLook`(열거에 있는 값) |
| `hopHeight` | 0.3 | 뛰기 꼭대기가 두 칸 중 높은 칸 표면보다 더 올라가는 높이(필드 로컬 단위). `Walk`면 쓰지 않습니다 | 새 조건 함수 `ValidateHopHeight`(유한한 0 이상) |
| `hopSeconds` | 0.25 | 뛰기 한 번(한 칸)의 시간(초). `Walk`면 쓰지 않습니다 | 기존 `ValidateStepSeconds`(유한한 양수, `movementStepSeconds`와 같은 조건) |

- 읽기 속성 `VoluntaryStepLook`·`HopHeight`·`HopSeconds`를 더했습니다.
- 검사는 `Validate()` 맨 끝(소리 프리셋 검사 뒤)입니다. 이 파일의 "새 값은 맨 뒤" 규칙을 따랐고, 앞 값들이 먼저 알리는 순서는 그대로입니다.
- **에셋:** `Item/CombatPresentationSettings_Default.asset`에 `voluntaryStepLook: 1`(Hop)·`hopHeight: 0.3`·`hopSeconds: 0.25`를 적었습니다. 코드와 한 커밋입니다.
- **칸이 없는 에셋:** Unity가 코드 기본값으로 읽으므로 `Hop`입니다. 옛 걸음으로 되돌리려면 그 에셋의 `voluntaryStepLook`을 `Walk`로 바꾸면 됩니다.
- **병합 때 주의:** 같은 묶음이나 `Validate()` 끝에 줄을 더하셨다면 충돌할 수 있습니다. 두 쪽 줄을 모두 남기면 됩니다. 이 파일에는 소리 창구 2차(§8.1) 줄도 같이 있습니다.
- **부탁:** 따로 하실 일은 없습니다. 뛰는 느낌(높이·시간)은 에셋 값으로 조정하시면 됩니다.

## 10. 2026-10-28 후속 — 연출 큐 칸: 설정 표 둘·토네이도 설정 하나, 환경 폭발 칸 삭제 (이펙트 창구 확장, 미전달)

근거는 [전투 이펙트 창구 확장 설계](../specs/2026-10-28-combat-effect-cue-expansion-design.md)(EC5·EC8·EC9)와 [계획](../plans/2026-10-28-combat-effect-cue-expansion-plan.md) §6입니다. 사실 사건(추락·약한 지반 붕괴·토네이도 등)에도 이펙트·흔들림을 데이터로 붙일 수 있게 했습니다. 큐 칸의 형식은 새 입력 `CombatVisualCueInput`(이펙트·흔들림 목록)입니다. 공격·피격·사망 큐와 항목 꼴이 같고, 소리는 칸이 없습니다(§8의 소리 프리셋 칸이 그대로 냅니다).

### 10.1 전투 연출 설정 표(`CombatPresentationSettingsDataSO.cs`)

파일 맨 뒤에 새 머리말 "전투 연출 큐 (이펙트 창구 확장 설계 EC5)"와 칸 둘을 더했습니다.

| 필드 | 뜻 | 쓸 수 있는 시점·자리 |
|---|---|---|
| `fallCues` | 유닛·구조물(장애물)이 낭떠러지로 떨어지기 시작하는 순간 | 시점 Start(사건 순간)만, 자리는 떨어진 칸(EventCell)만 |
| `weakGroundCollapseCues` | 약한 지반이 무너져 낭떠러지가 되는 순간 | 시점 Start만, 자리는 무너진 칸만 |

- 읽기 속성 `FallCues`·`WeakGroundCollapseCues`를 더했습니다. 검사는 `Validate()` 맨 끝이고, 큐 규칙 원본 `CombatPresentationCueRules.Require`를 부릅니다(잘못된 시점·자리면 부트 DB 생성이 멈춤).
- 이 큐의 지연은 다른 사건을 기다리게 하지 않습니다(막지 않는 곁들임 실행). 다만 기차 사건과 결과 끝은 그만큼 기다립니다. 툴팁에 적었습니다.
- **지운 칸:** "환경 연출" 머리말, `environmentExplosionEffect`(화약통·지뢰 폭발 이펙트 종류), 읽기 속성 `EnvironmentExplosionEffect`, 그 검사를 지웠습니다.
  - 폭발은 이제 화약통 외형(`UnitVisual_PowderBarrel`의 `obstacleDestroyedCues`)과 지뢰 외형(`ObjectVisual_Mine`의 `activationCues`)의 큐가 냅니다. 값은 Boom·Start·지연 0·EventCell·회전 보정 (−90, 0, 0)이고, 자리·방향은 옛 길과 같습니다.
  - 기본 에셋 `CombatPresentationSettings_Default.asset`에서 `environmentExplosionEffect: 2` 줄도 지웠습니다.
- **에셋:** 새 큐 칸 둘은 적지 않았습니다(빈 묶음으로 읽혀 화면 변화 없음).
- **병합 때 주의:** 이 파일에는 소리 2차(§8.1)·걸음 모양(§9.1) 줄도 같이 있습니다. 브랜치에서 `environmentExplosionEffect`를 읽으셨다면 그 줄은 외형 큐로 옮겨야 합니다(우리 View 코드에는 읽는 곳이 남지 않았습니다).

### 10.2 토네이도 연출 설정(호진님 `TornadoPresentationSettingsSO.cs`·`TornadoPresentationSettingsData.cs`)

- **SO:** 소리 칸 뒤에 머리말 "연출 큐 (이펙트 창구 확장 설계 EC5)"와 칸 `cues` 하나를 더했습니다. 시점 Start = 휩쓸리는 순간, End = 내려앉는 순간이고, 자리는 그 시점의 유닛 몸(SubjectBody)만 됩니다. `GetData()`가 끝 인자로 넘깁니다.
- **Data:** 생성자 끝에 **필수 인자** `cues`(null이면 예외), 속성 `Cues`, 검사(`CombatPresentationCueRules.Require(cues, CombatCueEvent.Tornado, …)`)를 더했습니다. 생성하는 곳은 SO의 `GetData()` 하나뿐이라 깨지는 곳은 없습니다.
- **지연은 0만 됩니다(묶음 끝 검토 M1, 계획 §6.5):** 처음에는 "지연을 주면 그 유닛의 다음 사건이 늦어진다"로 두었으나, 들어 올림이 끝나 착지 칸을 그린 뒤 지연된 큐가 끝나면서 자세 대여가 돌아갈 때 유닛이 휩쓸리기 전 칸으로 되돌아가 보이는 결함이 있었습니다. 큐 규칙(`CombatPresentationCueRules`)이 토네이도 칸의 이펙트·흔들림 지연이 0이 아니면 부트 DB 생성에서 거절합니다. 툴팁도 그렇게 고쳤습니다.
- **에셋:** `TornadoPresentation_Default.asset`은 고치지 않았습니다(빈 묶음).

### 10.3 토네이도 들어 올림 조립(호진님 `CombatCharacterEventHandler.TryBeginTornado`) 한 자리

- 들어 올림 조각을 더하는 줄이 그 항목 번호를 받게 했습니다(`int lift = composition.Add(…)`).
- 그 바로 뒤에 큐 붙이기 한 줄을 넣었습니다: `CombatCueComposer.Add(composition, End 시점 = lift 항목의 끝 신호, target.Settings.Cues, 자리 = 휩쓸리기 전 서 있는 몸 / 도착 사실의 서 있는 몸, 수신기 = 이펙트·카메라)`. 소리 수신기는 비워 둡니다(소리는 §8.3의 소리 조각이 냅니다).
- 서 있는 몸 자리는 보인 사실 `CombatShownFacts.StandingBodyOf`(몸 상자 가운데 + 서 있는 방향)를 부릅니다. 처음에는 이 파일에 private 도우미를 두었는데, 순간 큐 재생과 같은 규칙이라 묶음 끝 검토 L1로 한 곳에 모았습니다. 주석 한 줄도 "지연은 0만"으로 바꿨습니다.
- 큐가 비어 있으면 붙는 항목이 없어 지금 화면·완료 시각은 같습니다.

### 10.4 참고 — 외형 SO 큐 칸(우리 파일)

유닛 외형(`obstacleHitCues`·`obstacleDestroyedCues`, 장애물 키에만 허용), 오브젝트 외형(`activationCues`), 아이템 외형(`activationCues`)에도 같은 꼴의 큐 칸이 생겼습니다. 호진님 SO 모드 도구는 이 칸을 읽지 않아 영향이 없습니다.

- **부탁:** 따로 하실 일은 없습니다. 브랜치에서 `TryBeginTornado`나 토네이도 설정을 고치셨다면 병합 때 두 쪽 줄을 모두 남기고, 들어 올림 `Add` 줄이 번호를 받게만 맞춰 주세요.

## 11. 2026-10-29 후속 — 약한 지반 붕괴 연출 값 다섯·필드 외형 재생기 이름·지형 외형 표시 방식 (지형 외형 타일 교체, 미전달)

[타일 교체 설계](../specs/2026-10-29-combat-terrain-tile-replace-design.md) TR1·TR8, [계획](../plans/2026-10-29-combat-terrain-tile-replace-plan.md) §6.1·§6.4.

### 11.1 전투 연출 설정 표(`CombatPresentationSettingsDataSO.cs`)

새 묶음 "약한 지반 붕괴 연출 (타일 교체 설계 TR8)"을 "낭떠러지 추락 연출 (임시)" 바로 뒤에 두었습니다. 약한 지반이 낭떠러지로 바뀔 때 그 칸 외형이 떨리다 가라앉는 값입니다.

| 칸 | 기본값 | 뜻 |
|---|---|---|
| `tileCollapseShakeSeconds` | 0.35 | 떨리는 시간(초). 이 동안 다음 사건(그 칸 유닛 추락)은 기다립니다 |
| `tileCollapseShakeStrength` | 0.04 | 필드 가로 두 축으로 흔들리는 거리(월드 단위) |
| `tileCollapseShakeFrequency` | 25 | 초당 왕복 수 |
| `tileCollapseSinkSeconds` | 0.5 | 떨림 뒤 가라앉는 시간(초). 유닛 추락과 함께 진행 |
| `tileCollapseSinkDepth` | 1.5 | 가라앉는 깊이(월드 단위, 유닛 추락 `fallDepth`와 같은 단위) |

- 읽기 다섯·새 조건 함수 `ValidateTileCollapseAmount`(유한한 0 이상), 검사는 파일 규칙대로 `Validate()` 맨 뒤입니다. 값 객체는 새 `Presentation/Environment/CombatTileCollapseSettings`입니다.
- 기본 에셋 `CombatPresentationSettings_Default.asset`에도 같은 값 다섯 줄을 `fallEndScale` 다음에 넣었습니다. 그 뒤 떨림 세기만 0.1로 조정했습니다(에셋 값).

### 11.2 호진님 줄 이름 바뀜 — 토네이도 외형 재생기·필드 창구

토네이도 생성·축소를 돌리던 재생기를 바닥 교체 무너짐까지 맡도록 넓혔습니다. 토네이도 동작은 같습니다.

- `Playback/Players/CombatTornadoAppearancePlayer.cs` → `CombatFieldAppearancePlayer.cs`(`git mv`, `.meta` GUID 그대로). 생성자에 셋째 인자 `blocksStart`가 생겼고, `FindBlock`은 그것을, 완료·결과 끝 반영은 지금처럼 진행 중 여부를 봅니다. 막는 이유 값 `CombatStartBlock.TornadoAppearance` → `FieldAppearance`.
- 필드 창구 `CombatFieldEventPorts` 칸: `TickTornado`·`TornadoPlaying`·`CancelTornado` → `TickAppearance`·`AppearancePlaying`·`CancelAppearance`, 새 칸 `AppearanceBlocksStart`. 사건 Presenter의 재생기 생성 한 줄도 맞췄습니다.
- 지형 표시 `FieldTerrainPresenter`: `IsAppearancePlaying`은 토네이도 또는 무너짐이 남았는지, 새 `IsAppearanceBlockingStart`는 토네이도가 재생 중이거나 무너짐이 떨리는 중인지입니다. 토네이도는 전처럼 끝까지 다음 사건을 막습니다.

### 11.3 지형 외형 SO(`TerrainVisualDataSO.cs`)

- 표시 방식 이름: `PlacedPrefab` → `OnTile`(0), `GroundShape` → `ReplaceTile`(1). 고지대 에셋이 1 → 0으로 바뀌었습니다.
- `surfaceHeight`의 뜻이 방식마다 다릅니다. 타일 위는 바탕 위로 올리는 높이(0 이상), 바닥 교체는 그 칸의 새 바탕 높이(음수 가능)입니다. 그래서 칸의 `Min(0)`을 뗐고, 타일 위 음수는 생성자가 거절합니다.
- **토네이도는 `OnTile`이어야 합니다**(생성자 검사 한 줄). 지금 토네이도 에셋은 0이라 그대로 통과합니다. 호진님이 더하신 `tornadoPresentation` 칸·검사는 그대로입니다.

- **부탁:** 따로 하실 일은 없습니다. 브랜치에서 토네이도 재생기·필드 창구·토네이도 외형 방식을 고치셨다면 병합 때 새 이름으로 맞춰 주세요.

## 12. 2026-10-29 후속 2 — 낭떠러지 추락 값 바뀜·작아지기 시작 지점 (지형 외형 타일 교체 검토 반영, 미전달)

근거는 [타일 교체 계획](../plans/2026-10-29-combat-terrain-tile-replace-plan.md) §6.6입니다. 컴파일은 확인하지 않았습니다.

### 12.1 전투 연출 설정 표(`CombatPresentationSettingsDataSO.cs`) "낭떠러지 추락 연출 (임시)" 묶음

추락을 "그 자리에서 조금 가라앉으며 작아짐"에서 "크게 떨어지며 끝에만 작아짐"으로 바꿨습니다. 낭떠러지가 이제 빈 구멍이라 그 아래로 떨어지는 모습입니다. 칸 낭떠러지와 약한 지반 붕괴 둘 다 같은 조립을 씁니다.

| 칸 | 옛 기본값 | 새 기본값 | 조건 |
|---|---|---|---|
| `fallSeconds` | 0.5 | 0.6 | 그대로(유한한 0 이상) |
| `fallDepth` | 0.6 | 3 | 그대로(유한한 0 이상) |
| `fallEndScale` | 0.05 | 0.05 | 그대로 |
| `fallShrinkStart`(새, `fallEndScale` 바로 뒤) | — | 0.5 | 유한한 0 이상 1 미만(새 `ValidateFallShrinkStart`) |

- `fallShrinkStart`는 전체 시간 가운데 작아지기 시작하는 지점입니다. 그 뒤 남은 시간 동안 1 → `fallEndScale`로 줄고, 떨어지기와 같은 순간 끝납니다. 0이면 옛 모양(처음부터 작아짐)입니다.
- 기본 에셋 `CombatPresentationSettings_Default.asset`도 같은 값으로 바꿨습니다. 검사는 `Validate()` 맨 끝입니다.

### 12.2 함께 바뀐 것(검토 반영, 호진님 줄 아님)

- 지형 외형 SO `TerrainVisualDataSO.cs`: "타일 위(`OnTile`)는 프리팹 필수" 검사를 표(`TerrainVisualTable`)에서 Data 생성자로 옮겼습니다. 방식 규칙은 이제 생성자 한 곳입니다. `localOffset` 툴팁에 타일 칸에서 쓰이는 축을 적었습니다.
- 토네이도 바탕 높이는 이제 타일 바탕(새 `FieldSurfaceHeights.TileBaseHeightAt`)에서 읽습니다. 근처 약한 지반이 무너져도 토네이도가 다시 줄었다 커지지 않습니다. 호진님 토네이도 코드는 바꾸지 않았습니다(지형 표시의 바탕 계산 한 곳만 바뀜).

- **부탁:** 따로 하실 일은 없습니다. 브랜치에서 이 묶음 값을 바꾸셨다면 병합 때 새 칸 `fallShrinkStart`를 함께 맞춰 주세요.

## 13. 2026-11-02 — 낙석 값 표와 터널 자리 (낙석 전투 View, 미전달)

근거는 [낙석 계획](../plans/2026-11-01-combat-rockfall-view-plan-Hojin.md)입니다. 컴파일은 확인하지 않았습니다.

- 새 View 표 `RockfallVisual`(설정 하나): 돌·느낌표 프리팹, 느낌표 크기·띄움, 낙하 높이·옆 거리·시간·간격, 큐 둘, 소리 프리셋 둘, 이름·설명. 에셋은 `DataBaseSO/ViewGroup/RockfallVisual/`입니다.
- 스테이지 View 칸은 더하지 않았습니다. 처음 넣은 칸 셋(`hasRockfallTunnel` 등)은 걷었습니다.
- 터널 자리는 맵 프리팹 안의 빈 오브젝트 `RockfallTunnelBoom`입니다. 낙석을 쓰는 맵에는 이 오브젝트가 있어야 터널 폭발이 나옵니다.
- **부탁:** SO 모드 3D 미리보기는 이 오브젝트와 낙석을 그리지 않습니다. 필요하면 범위를 정해 주세요.

## 14. 2026-11-02 — 하붑 값 표 (하붑 전투 View, 미전달)

근거는 [하붑 계획](../plans/2026-11-01-combat-haboob-view-plan-Hojin.md)입니다. 컴파일은 확인하지 않았습니다.

- 새 View 표 `HaboobVisual`(설정 하나): 구름 이펙트 종류·위치 보정·회전 보정, 이동 소리 프리셋, 이름·설명. 에셋은 `DataBaseSO/ViewGroup/HaboobVisual/`입니다.
- 같은 표에 `relocationSeconds`(기본 1, 0 초과)를 더했습니다. 구름이 새 자리로 미끄러지는 시간이고, 도착할 때까지 다음 연출을 기다립니다.
- 켜기는 스테이지 전투 데이터의 `haboob.enabled`입니다. 칸 모양·주기는 Core 값입니다.
- **부탁:** SO 모드 3D 미리보기는 하붑 구름을 그리지 않습니다. 필요하면 범위를 정해 주세요.

## 15. 2026-11-02 — 낙석 돌 값과 능선 (낙석 전투 View, 미전달)

근거는 [낙석 계획 §7](../plans/2026-11-01-combat-rockfall-view-plan-Hojin.md)입니다.

- `RockfallVisual` 표: `dropSideDistance`를 지웠습니다(§13의 "옆 거리"). 새 칸은 `dropStartScatter`, `stoneScaleMin`·`stoneScaleMax`, `spinDegreesMin`·`spinDegreesMax`입니다. `dropHeight`는 능선이 없는 맵에서만 씁니다.
- 능선은 맵 프리팹 안의 빈 오브젝트 `RockfallRidgeStart`·`RockfallRidgeEnd`입니다. 돌산의 전투판 쪽 경사면 위에 놓습니다. 빠지면 돌이 칸 위에서 곧게 떨어집니다. 같은 이름이 둘 이상이면 낙석이 꺼져 있어도 전투 진입 때 멈춥니다.
- SO 모드: 검사에 경고·오류를 더했고 사용법 §5.11에 적었습니다. 3D 미리보기는 능선을 그리지 않습니다.

## 16. 2026-11-11 — 작렬 날려 감 걸음 시간·특성 외형 (작렬 전투 View, 미전달)

- 설정 표 "유닛 이동 연출" 묶음에 `flungStepSeconds`(기본 0.08초, 양수)를 더했습니다. 작렬로 날아가는 걸음 한 칸의 시간이고, 일반 넉백·끌어오기는 `movementStepSeconds` 그대로입니다. 기본 에셋에도 같은 값을 넣었습니다(코드와 한 커밋).
- 특성 외형 표에 `TraitVisual_CrashImpact`(키 520)를 더했습니다. 부딪힘 발동(410) 줄 하나 — 대상 몸에 `Boom`, 흔들림 0.3·0.3초(임시 값, 에디터에서 바꿔도 됩니다). 날기 시작(`KnockbackApplied`) 줄은 두지 않았습니다.
- 막은 칸이 맵 밖일 수 있어 칸 자리(`EventCell`)는 쓰지 않았습니다. 이제 맵 밖 발동 칸은 자리 목록에서 빠지므로 써도 멈추지는 않지만, 맵 끝 부딪힘에서는 이펙트가 나오지 않습니다.
- 설계: `docs/superpowers/specs/2026-11-11-combat-crash-impact-view-design.md`

## 17. 2026-11-12 — 약한 지반·추락 소리 프리셋 칸 셋 (소리 프리셋 원본, 미전달)

근거는 [소리 프리셋 원본·곡사 공통 수명 계획](../plans/2026-11-12-combat-view-sound-preset-mortar-lifecycle-plan.md) SM-a(§10.2)와 설계 SP1~SP3입니다. 설정 표 칸을 더할 때마다 이 메모에 적어 온 관례대로 씁니다.

- "전투 소리 프리셋" 묶음 칸 순서: 적 공용 → 배경음 셋 → **진입 목록 `weakGroundEntrySoundPresets`(새 칸)** → 붕괴 `weakGroundCollapseSoundPreset` → (새 머리말 "전투 소리 프리셋 — -1이면 프리셋 없이 냄") → 추락 `fallSoundPreset`.
  - 진입 목록(`List<int>`, 기본 `[1, 2]`): 첫 원소 = 남은 진입 1, 둘째 = 남은 2, 남은 횟수가 더 많으면 마지막 원소. 원소 -1은 그 횟수만 소리 없음, 빈 목록은 진입 소리 없음. 원소마다 -1 또는 0 이상을 검사하고, 목록이 없으면 예외입니다.
  - 붕괴 기본값 -1 → **0**(지금 나는 소리와 같음). -1이면 소리 없음.
  - 추락: -1의 뜻이 **"프리셋을 바꾸지 않고 냄"**으로 바뀌었습니다(Boot `Fall` 칸에 프리셋이 없어서). 다른 칸과 뜻이 달라 머리말을 따로 두었습니다. 0 이상은 Boot `Fall` 칸에 그 프리셋이 있어야 합니다(없으면 재생 때 오류, DB 검사로는 못 막음).
- 기본 에셋 `CombatPresentationSettings_Default.asset` 세 줄: 진입 목록 `- 1`·`- 2`, 붕괴 0, 추락 줄을 붕괴 뒤로 옮김(칸 순서와 같게). 코드와 한 커밋입니다.
- 설정 Data 요약 주석도 실제에 맞췄습니다: 하위 설정 묶음 셋(보드 등장·선로 낙하·Nexus 낙하)은 수 규칙을 바로 부르고, 보드 등장의 시간 칸 둘만 이 설정의 조건 함수(`ValidateSeconds`)를 씁니다(재평가 C12-N10).
- 복사 입구 `CopyWeakGroundEntrySoundPresets()`는 부를 때마다 새 배열을 줍니다. 전투 소리 프리셋 답이 만들어질 때(View 서비스 시작 = 재생 시작 때) 한 번 부릅니다(직렬화 목록을 밖에 내주지 않음). 그래서 재생 중에 진입 목록을 바꾸면 다음 재생부터 반영되고, 붕괴·추락 칸은 바로 반영됩니다.
- 진입 목록 칸에 `[Min(-1)]`을 붙였고(원소마다), 검사 예외의 인자 이름에 원소 순번이 나옵니다(`weakGroundEntrySoundPresets[1]` 등). `fallCues` 툴팁은 "소리는 유닛만 추락 프리셋이 냄"으로 고쳤습니다(2026-11-13, 묶음 끝 검토 낮음 반영).
