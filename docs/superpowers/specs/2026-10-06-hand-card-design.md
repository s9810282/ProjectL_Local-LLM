# 손패 카드화 설계 (2026-10-06)

- **목표:** 전투 손패를 3D 유닛 모델 대신 현강님의 정보 카드(`Prefabs/UI/UnitCard.prefab`) 외형으로 보여 준다. 옛 develop-4week 카드 손패(`aaca8b4c`)와 같은 경험을 새 구조에서 다시 만든다.
- **상태:** 설계. 진행·검증 기록은 View 계획 §7.50에만 적는다.
- **사용자 결정(2026-10-06):**
  - U1. 카드 외형은 `UnitCard.prefab`을 쓴다.
  - U2. 손패 소유자는 `HandPresenter`(Combat 씬 표시, CombatSceneDisplay가 값을 넘김)다. UISystem 화면으로 만들지 않는다.
  - U3. `UnitCardUI` 분리는 우리가 한다(현강님 코드 수정, 범위는 §3).
  - U4. 카드는 배치 단계에서만 보인다.
  - U5. 손패 카드의 Synergy/Function 패널 버튼은 숨긴다.
  - U6. 카드 Hover 때 정보 카드를 띄울지는 나중에 정한다(이번 범위 밖).

## 1. 지금 구조와 막히는 지점

| 대상 | 현재 |
|---|---|
| `UnitCard.prefab` | 루트에 `UnitCardUI : UIBase`(`UIId.UnitCard`). 초상화·이름·레벨·능력치 상자·Synergy/Function 패널. 400×550 |
| `UnitCardUI` | 표시(직렬화 참조·패널 Hover/고정)와 데이터 조회(Buffer `CharacterReadSink`, Unit DB, 보유 카드·영혼 보너스, `Update`에서 매 프레임 비교)가 한 클래스에 있다 |
| `DeckButtonUI` | `unitCard` 필드가 프리팹 에셋을 가리키면 유지 Canvas 아래에 한 장을 만든다(`EnsureUnitCardInstance`) |
| `HandPresenter` | 3D 손패. 키별 풀에서 `UnitVisual.Prefab`을 빌려 `contentRoot`에 슬롯 배치. 위치는 필드·카메라로 계산(`HandFieldLayout`). 집기·Drop 영역은 Ray와 평면 교차 |
| 입력 | Router → `PlacementDragInteraction` → `ICombatPointerPicker.TryPickHandUnit`·`IsOverHandDropZone`(구현 `CombatPointerMapper` → `HandPresenter`). Unity UI 이벤트를 쓰지 않는다 |

**막히는 지점:** 손패 카드마다 `UnitCardUI`가 붙으면
1. `GameBootstrap.BindLoadedUI`가 씬 로드마다 `UISystem.BindScene`을 다시 부르고, BindScene은 비활성까지 `UIBase`를 찾아 ID로 등록하므로 `UIId.UnitCard` **중복 등록 예외**가 난다.
2. 카드마다 Buffer를 직접 읽고 InGame Unit 표를 조회한다. 손패는 "값만 받는다"는 HandPresenter 원칙과 P6(View는 InGame을 읽지 않음)에 어긋난다.

## 2. 구조 결정

```text
UnitCard.prefab (기준 외형, SSOT)
  └ UnitCardView            표시 전용: 직렬화 참조, 이름 결정, 능력치 문구, 패널 Hover/고정, 패널 사용 여부
UnitCardUI.prefab (UnitCard.prefab의 Prefab Variant)
  └ + UnitCardUI : UIBase    데이터 조회만: Buffer·DB·보유 카드 → UnitCardView에 값 전달 (덱 정보 카드)
HandPresenter (Combat 씬)
  └ UnitCard.prefab 풀       CombatSceneDisplay가 넘긴 CharacterData·외형 표로 UnitCardView에 값 전달
```

- **C1 표시와 조회 분리(SRP):** `UnitCardView`는 무엇을 어떻게 그리는지만 안다. 어디서 값을 읽는지는 호출자(`UnitCardUI`, `HandPresenter`)가 정한다. 능력치 문구 형식(`HP : a / b` …)과 이름 결정 순서(카드 Visuals SO → UnitVisual 표 → 키)는 `UnitCardView` 한 곳에만 둔다(SSOT).
- **C2 외형 한 곳:** 외형 원본은 `UnitCard.prefab` 하나다. 덱 정보 카드는 그 Variant에 `UnitCardUI`만 더한다. 현강님이 외형을 고치면 덱과 손패에 함께 반영된다. 손패 전용 복사본을 만들지 않는다.
- **C3 UIBase는 덱 카드에만:** 손패 카드 인스턴스에는 `UIBase`가 없으므로 BindScene 중복 등록이 생기지 않는다.
- **C4 공개 계약 유지:** `UnitCardUI`의 public API(`SetCharacter`, `SetOwnedCharacter`, `SetLevel`, `Open`, `Hide`, `Refresh`, `ClosePanels`, Hover/Toggle 메서드, `CharacterId`, `OwnedCharacterId`)와 덱 동작은 바꾸지 않는다. 패널 Hover/고정 메서드는 `UnitCardView`로 위임한다.
- **C5 손패 카드 값:** HandPresenter는 `CharacterData`(체력·능력치)와 `IUnitVisualTableQuery`(초상화 `InfoIcon`·표시 이름)만 쓴다. InGame 표를 읽지 않는다. 손패 카드는 패널을 숨기므로(U5) 특성·시너지 내용은 채우지 않는다. 레벨은 Buffer에 없으므로 `-`.
- **C6 화면 공간 손패:** 카드는 Combat 씬 Canvas(Screen Space Overlay) 아래 `cardRoot`(RectTransform)에 놓는다. 슬롯 순서는 Core의 `ReserveSlotIndex`, 가운데 정렬·카드 수에 따른 간격 축소는 옛 `CardHandPresenter.Layout`을 따른다. 3D 위치 계산(`HandFieldLayout`, `LateUpdate`)은 쓰지 않는다.
- **C7 입력 경로 유지:** 집기·놓기는 지금처럼 입력 시스템 → Router → `PlacementDragInteraction`이다. 카드의 Unity UI 이벤트(옛 `CardHandItem`의 Drag 핸들러)는 쓰지 않는다. `ICombatPointerPicker`·Interaction·Router는 바꾸지 않는다.
  - `HandPresenter.TryPickUnit(Vector2 screen)`: 보이는 카드 RectTransform에 화면 점이 들어가는지(`RectTransformUtility.RectangleContainsScreenPoint`, Overlay라 카메라 null). 겹치면 뒤에 그려진(앞쪽) 카드가 이긴다.
  - `HandPresenter.ContainsDropPoint(Vector2 screen)`: `cardRoot` 영역(손패가 비어도 유지).
  - `CombatPointerMapper`의 손패 두 메서드는 Ray 대신 화면 좌표를 넘긴다. 카메라 Ray·필드 질의는 그대로다.
  - `TryMapPointer`는 GraphicRaycaster에 맞으면 취소 영역으로 본다. 카드는 UI이므로 "손패 위 = 되돌리기·취소 영역"이 그대로 성립한다. Drag 중인 카드는 `SetHiddenEntity`로 꺼지므로 필드 위 판정을 가리지 않는다.
- **C8 배치 단계만 표시(U4):** CombatSceneDisplay가 `state.CommandState.Turn.StagePhase == Deployment`이고 종료 전일 때만 손패 카드 루트를 켠다. 표시 조건은 Core 국면(Buffer 값)을 따른다. RecordInputOnly에서 개발 패널로 전투 Layer를 골라도 Core 국면은 배치라 카드는 보인다(알려진 차이, §6).
- **C9 재사용:** 카드는 하나의 풀(키별 풀 불필요)에서 빌리고 ID별 기존 카드를 다시 쓴다(`EntityViewSet`, W19). 같은 값이면 텍스트·이미지를 다시 쓰지 않는다.

## 3. 현강님 코드에 닿는 범위 (U3)

| 파일 | 변경 |
|---|---|
| `UISystem/Screens/Global/UnitCardUI.cs` | 직렬화 표시 참조와 패널 로직을 `UnitCardView`로 옮기고 위임. 조회 로직(Buffer·DB·보유·영혼·Update 비교)은 그대로. `[RequireComponent(typeof(UnitCardView))]` |
| `UISystem/Screens/Global/UnitCardView.cs`(신규) | §2 C1. 같은 폴더(덱과 손패가 함께 쓰는 UI 부품) |
| `Prefabs/UI/UnitCard.prefab` | 루트 `UnitCardUI` → `UnitCardView`(같은 직렬화 필드 이름 유지로 참조 보존) |
| `Prefabs/UI/UnitCardUI.prefab`(신규 Variant) | `UnitCardUI` 추가 |
| `Prefabs/UI/GlobalUICanvas.prefab` | `DeckButtonUI.unitCard`를 Variant로 다시 연결 |
| `UnitCardUI.md` | 구조 설명 갱신 |
| 테스트 `DeckButtonUITests` | 표시 필드를 `UnitCardView`에서 설정 |

`DeckButtonUI.cs`·`UnitCardVisualsSO.cs`·덱 동작은 바꾸지 않는다. 변경 내용은 현강님께 공유 메모로 알린다(발송은 사용자).

## 4. SOLID·SSOT·FW 점검

- **S:** 표시(`UnitCardView`)·조회(`UnitCardUI`)·손패 배치(`HandPresenter`)가 각자 하나의 이유로만 바뀐다.
- **O/L:** 새 카드 사용처는 `UnitCardView`에 값을 넘기기만 하면 된다. `UnitCardUI`의 공개 계약과 덱 동작은 그대로다.
- **I/D:** 입력 쪽은 기존 `ICombatPointerPicker`만 본다. HandPresenter는 Buffer·Core·UISystem을 모른다.
- **SSOT:** 외형은 `UnitCard.prefab` 하나, 능력치 문구·이름 결정은 `UnitCardView` 하나, 슬롯 순서는 Core `ReserveSlotIndex`.
- **FW:** 새 서비스·모듈 없음. 씬 오브젝트는 에디터에서 미리 둔다(runtime-scene-binding-guide). UISystem 등록 대상은 늘지 않는다.

## 5. 지우거나 남길 것

- `Display/HandUnitView.cs`, `Display/HandFieldLayout.cs`: 카드 손패 뒤 쓰지 않는다. 지울지는 사용자 확인 뒤(계획 H2 끝).
- `HandPresenter`의 3D 직렬화 값(`slotSpacing`, 필드 기준 위치·`pickHalfDepth`·`dropZoneSize`)은 카드 값으로 바꾼다. 씬 값은 다시 지정한다.

## 6. 범위 밖·알려진 차이

- 카드 Hover 정보 카드(U6), 카드 Hover 들림 연출(옛 `HoverLift`)은 이번 범위 밖이다. 들림은 표시만이라 후속으로 붙이기 쉽다.
- 손패 카드의 특성·시너지 표시는 패널을 숨겼으므로 채우지 않는다(U5).
- RecordInputOnly의 개발 전투 Layer에서도 카드가 보인다(C8). 그 상태에서 카드 위 클릭은 UI로 판정돼 경로·공격 입력이 되지 않는다.
- 레벨은 Buffer에 없어 `-`(덱과 같음).
