# 전투 씬 표시 조립 위치 변경 공유 — SD-D1

> **2026-10-26 현재 5라운드 최종 조립 인계는 §31을 따른다(미전달).** 4라운드 V4-n과 5라운드 a/b/c는 각각 사용자 컴파일·기존 시험·Unity 정상 보고로 인수했고 d 통합 정리를 마쳤다. §26~30의 진행 중/미확인은 각 시점 이력이며, 직접 실행 여부/확인 공백은 5라운드 계획 §10에 구분했다.

- 작성: View 작업, 2026-10-15. Framework·View 작업자 참고용이며 **미전달**이다.
- 구현·검증 상태 원본: [조립 계획 §10.3](../plans/2026-10-14-combat-scene-display-composition-plan.md).
- 계약 원본: [D0 조립·소유권 명세](../specs/2026-10-15-combat-scene-display-composition-contract.md).

## 1. 변경 경로

기존 `CombatSceneDisplay(root, tables, services)`의 구체 생성·Configure·콜백 연결을 `Systems/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.Create(root, tables, services)`로 옮겼다. 실제 파일은 [CombatSceneDisplayFactory.cs](../../../Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs)다.

| 호출자 | 현재 경로 |
|---|---|
| 운영 `CombatViewSystem.Attach` | 활성 전투·중복 연결 등 기존 사전 검사 → Factory.Create → `AttachCreated`(2026-10-15 SV1부터, 인수 전 실패 반환 포함 — §4) |
| 실제 씬 fixture 6파일 | 같은 Factory.Create 사용. 직접 조립 생성자 경로 제거 |
| FakeDisplay 기반 단위 테스트 | 기존 AttachDisplay 경로 유지 |

Display는 준비된 참조를 명시적 생성자로 받고 공통 수명·공개 호출 방어를 유지한다. View DB·서비스 묶음은 Factory에서 필요한 개별 의존성으로 풀어 주며, 내부 Presenter 전체를 서비스로 등록하지 않는다.

## 2. Framework 계약과 영향

- 기존 Systems Factory 폴더 안의 구체 조립 헬퍼다. 새 IServiceModule, ServiceKey, AddModule, Builder, FrameworkRuntime 단계는 없다.
- Framework/Architecture 구현·Core/Buffer·FieldCamera·씬·프리팹·DB 값은 변경하지 않았다. UIInputBinding과 씬 Enter/Exit 연결 방식도 유지한다.
- 공용 서비스는 기존 소유자가 관리한다. Factory에 서비스의 별도 수명이나 전역 캐시를 추가하지 않는다.
- 같은 Field/Units/Ghost/Thorns를 공유하고 Display의 Install/Clear/Release 순서와 `_fieldBound` 정의를 유지한다. 커서 설정은 root에서 실시간으로 읽는다.
- 가시밭 콜백은 Factory 지역 참조를 캡처한다. EventPresenter 생성자가 저장만 하는 전제와 step 검증 뒤 ThornPresenter 생성 순서를 유지한다.

## 3. 유지한 한계·후속 범위

조립 도중 Configure가 일부 반영된 뒤 실패하는 위험, 반환된 Display를 ViewSystem이 인수하기 전 예외의 해제 누락 위험은 기존 상태로 남는다. 이번 변경을 조립 원자성·오류 복구 개선으로 인수하지 않는다. 인수 뒤 Install 실패는 기존 Detach가 보관된 Display를 반환한다.

D1은 조립 위치 분리다. 이동·배치·공격 구현 이전은 D2~D4이며, 기능별 변경 범위 인수와 Refresh 평가는 D5다. Unity 실제 씬·소유 자원 회귀의 대기 여부는 계획과 [D1 워크시트](../viewDocs/2026-10-14-combat-scene-display-sd-d1-worksheet.md)를 확인한다.

## 4. 2026-10-15 후속 — 구조 개선 SV1·SV5 (미전달, 이 문서와 함께 전달)

- 상태 원본: [SV 계획 §12](../plans/2026-10-15-combat-view-structure-improvement-plan.md).
- **SV1(위 §3의 해제 누락 해소):** `CombatViewSystem.Attach`는 Factory가 만든 Display를 `AttachCreated`로 넘겨받는다. `_display`에 저장하기 전 예외면 연출 Detach → Display Release 뒤 원래 예외를 던지고(정리까지 실패하면 원인을 맨 앞에 둔 `AggregateException`), 오류 상태로 인수하지 않으면 그 자리에서 반환한다. 씬 루트 이벤트는 인수했고 첫 그리기 안의 재진입 Detach로 놓이지 않았을 때만 구독한다. 이동 step 값 검사는 씬 루트 검사(`ValidatePresentationTiming`)로 올라가, Factory가 가시밭 표시를 사건 Presenter보다 먼저 만들어 `Consume`을 직접 넘긴다(§2의 지역 참조 캡처는 없어졌다). Factory 내부의 부분 Configure 실패 복구는 여전히 없다.
- **SV5(입력 조립 추가):** 같은 폴더에 `CombatSceneInputFactory.cs`(정적 구체 조립 헬퍼)를 더했다. `CreateSceneInteraction`이 경로 입력을 만들어 `CreateInteraction`(배치·공격 입력 기능을 만들어 경로와 묶음 `CombatInteraction`으로 돌려줌, 테스트도 같은 입구)에 넘기고, `CreateCursorSnap`이 커서 스냅을 만든다. 대기 관문(`CombatCommandGate`)과 Router는 조정자가 만든다. 새 IServiceModule·ServiceKey·AddModule·Builder 단계는 없다.
- `CombatDisplayServices.From(IGlobalSystemGroupService)`가 Global 그룹에서 표시용 Render·Audio 서비스를 고른다(조정자의 `new` 제거). Global 그룹 계약은 바꾸지 않았다.

## 5. 2026-10-15 후속 — 스테이지 1 보스 View 활성화 Phase 3·3b (미전달, 이 문서와 함께 전달)

- 상태 원본: [보스 활성화 계획 §6.6·§6.8~§6.10](../plans/2026-10-15-stage1-boss-view-activation-plan.md). 결정은 [보스 활성화 설계](../specs/2026-10-15-stage1-boss-view-activation-design.md) D6·D6a·D6b.
- 코드 기준: `cbf8073b`("[용근] 전투 View, Boss 1차"). 이 공유 문서 보완은 2026-10-16에 했다(작성 당시 누락).
- **변경:** `CombatSceneDisplayFactory.Create`가 `CombatSceneEventPresenter`를 만들 때 생성자 인자 두 개를 더 넘긴다.

| 인자 | Factory가 넘기는 값 | 용도 |
|---|---|---|
| `presentHighlight` | `cells => field.SetLayerCells(FieldHighlightLayer.PresentationHighlight, cells)` | 포효·소환 칸 강조. 재생 전용 층이라 입력 강조와 섞이지 않는다. 빈 목록이면 지운다 |
| `abilityMotion` | `root.CreateAbilityMotionSettings()` | 포효(커짐·박치기·작아짐)와 소환(바닥에서 돌며 등장)의 시간·크기 값. `CombatSceneRoot` "보스 능력 연출" 직렬화 값에서 만든다 |

- 두 인자 모두 선택 인자다. 생략하면 강조가 없고 연출 값은 `CombatAbilityMotionSettings.Instant`가 된다. 그래서 Factory를 거치지 않는 기존 시험 경로는 바꾸지 않았다.
- **Framework 계약:** 새 IServiceModule·ServiceKey·AddModule·Builder·FrameworkRuntime 단계는 없다. ServiceKeys·Group·asmdef·`JobType`은 바꾸지 않았다. 바뀐 공용 파일은 Factory의 위 생성자 호출 한 곳이다.
- **씬 값 검사:** `CombatSceneRoot.ValidatePresentationTiming`이 `CreateAbilityMotionSettings()`를 불러 값을 검사한다(SV1과 같은 위치). 값이 잘못되면 Factory 이전, 씬 루트 검사에서 실패한다.
- **검증 상태:** MSBuild 오류 0, 관리 실행 회귀 0(보스 활성화 계획 기록). Unity EditMode·재생 확인은 사용자 결과 대기다.
- **후속:** 전투 히트 타이밍 H-3의 Factory 변경은 아래 §6에 있다.

## 6. 2026-10-16 후속 — 전투 히트 타이밍 H-3 (미전달, 이 문서와 함께 전달)

- 상태 원본: [히트 타이밍 계획 §8.7](../plans/2026-10-16-combat-hit-timing-plan.md). 결정은 [히트 타이밍 설계](../specs/2026-10-16-combat-hit-timing-design.md) H4·H7.
- **변경:** `CombatSceneDisplayFactory.Create`가 `CombatSceneEventPlayback`을 만들 때 생성자 인자 하나를 더 넘긴다.

| 인자 | Factory가 넘기는 값 | 용도 |
|---|---|---|
| `characterBody` (`Func<CharacterCombatFacts, FieldGridMapper, CombatBody>`) | `units.BodyOf`(캐릭터 오버로드) | 기차 사망의 접촉 몸. 사망 직전 사실(`Expected`)의 배치 + 외형 프리팹 루트 BoxCollider 모양 |

- 필수 인자다(null이면 `ArgumentNullException`). `CombatSceneEventPlayback`은 Factory에서만 만든다.
- 재생 부품에는 표시 Presenter 전체나 DB를 넘기지 않고 변환 함수만 넘긴다(설계 H4).
- **Framework 계약:** 새 IServiceModule·ServiceKey·AddModule·Builder 단계는 없다. ServiceKeys·Group·asmdef·`JobType`은 바꾸지 않았다. 바뀐 공용 파일은 Factory의 위 생성자 호출 한 곳이다.
- **조립 순서:** `units.Configure`가 먼저 실행되고, 몸 모양은 첫 기차 결과 준비 때 외형 표에서 읽는다(키마다 1회 캐시, Configure 때 비움). 기존 Factory 순서를 바꾸지 않았다.
- **검증 상태:** MSBuild·관리 실행 결과는 계획 §8.7에 있다. Unity 레일 장면 시험은 사용자 결과 대기다.
- **후속:** 전투 히트 타이밍 H-4b의 Factory 변경은 아래 §7에 있다.

## 7. 2026-10-17 후속 — 전투 히트 타이밍 H-4b (미전달, 이 문서와 함께 전달)

- 상태 원본: [히트 타이밍 계획 §8.11](../plans/2026-10-16-combat-hit-timing-plan.md). 결정은 [이동 접촉 상세 설계](../specs/2026-10-17-combat-movement-contact-design.md) M2·M4·M5.
- **변경:** `CombatSceneDisplayFactory.Create`가 `CombatSceneEventPresenter`를 만들 때 선택 인자 둘을 더 넘긴다.

| 인자 | Factory가 넘기는 값 | 용도 |
|---|---|---|
| `thornBody` (`Func<CellCoord, FieldGridMapper, CombatBody>`) | `thorns.BodyOf`(가시 표시가 없으면 null) | 걸음 도중 가시 칸 접촉 몸. 가시 프리팹 루트 BoxCollider 모양 + 칸 배치 |
| `objectBody` (`Func<ObjectSourceFact, FieldGridMapper, CombatBody>`) | `objects.BodyOf` | 걸음 도중 오브젝트(깃발·배지·지뢰) 접촉 몸. 외형 프리팹 루트 BoxCollider 모양 + 표시된 인스턴스 배치 |

- 두 인자 모두 선택 인자다. 생략하면 그 묶음은 걸음 끝(지금과 같은 칸 도착 시점)에 해제된다. 그래서 Factory를 거치지 않는 기존 시험 경로는 바꾸지 않았다.
- 재생 부품에는 표시 Presenter 전체나 DB를 넘기지 않고 변환 함수만 넘긴다(H-3 §6과 같은 방식).
- **Framework 계약:** 새 IServiceModule·ServiceKey·AddModule·Builder·FrameworkRuntime 단계는 없다. ServiceKeys·Group·asmdef·`JobType`은 바꾸지 않았다. 바뀐 공용 파일은 Factory의 위 생성자 호출 한 곳이다.
- **조립 순서:** 가시·오브젝트 표시는 기존처럼 사건 Presenter보다 먼저 만든다. 몸 모양은 첫 접촉 걸음의 보간 시작 때 외형 프리팹에서 읽는다(프리팹마다 1회 캐시, 가시·오브젝트 `Clear` 때 비움). 기존 Factory 순서를 바꾸지 않았다.
- **검증 상태:** MSBuild·관리 실행 결과는 계획 §8.11에 있다. Unity 걸음 접촉 장면 시험은 사용자 결과 대기다.
- **후속:** H-4c의 Factory 변경은 아래 §8에 있다.

## 8. 2026-10-17 후속 — 전투 히트 타이밍 H-4c (미전달, 이 문서와 함께 전달)

- 상태 원본: [히트 타이밍 계획 §8.12](../plans/2026-10-16-combat-hit-timing-plan.md). 결정은 [이동 접촉 상세 설계](../specs/2026-10-17-combat-movement-contact-design.md) M6(Q-M2 A).
- **변경:** `CombatSceneDisplayFactory.Create`가 `CombatSceneEventPresenter`에 선택 인자 하나를 더 넘긴다(§7의 두 인자 뒤).

| 인자 | Factory가 넘기는 값 | 용도 |
|---|---|---|
| `hideObject` (`Action<long>`) | `objects.HideActivated` | 오브젝트 발동 사건 시작 때 그 원본의 표시 인스턴스를 숨긴다. 결과 끝 Refresh의 전체 Render가 최종 사실로 다시 맞춘다 |

- 선택 인자다. 생략하면 지금처럼 결과 끝에만 바뀐다. Factory를 거치지 않는 기존 시험 경로는 바꾸지 않았다.
- **Framework 계약:** 새 IServiceModule·ServiceKey·AddModule·Builder·FrameworkRuntime 단계는 없다. ServiceKeys·Group·asmdef·`JobType`은 바꾸지 않았다. 바뀐 공용 파일은 Factory의 위 생성자 호출 한 곳이다.
- **검증 상태:** MSBuild·관리 실행 결과는 계획 §8.12에 있다. Unity 시험과 재생 확인은 사용자 결과 대기다.

## 9. 2026-10-18 후속 — 전투 연출 바탕 A-3b HUD 피드백 분리 (미전달, 이 문서와 함께 전달)

- 상태 원본: [연출 바탕 계획 §6·§12.5](../plans/2026-10-18-combat-presentation-foundation-plan.md). 결정은 [연출 조립 설계](../specs/2026-10-17-combat-presentation-composition-design.md) §9 I-2(Q-R15 A).
- **변경:** `CombatSceneDisplayFactory.Create`가 입력 피드백 두 개에 `units` 대신, 유닛 표시가 소유한 두 객체를 넘긴다. 그 위에 주석 한 줄을 더했다.

| 생성자 | 전 | 후 |
|---|---|---|
| `CombatMovementPlanningPresenter` | `(field, units, ghost, fieldConfig)` | `(field, units.HudFeedback, units.Presentation, ghost, fieldConfig)` |
| `CombatAttackFeedback` | `(field, units)` | `(field, units.HudFeedback)` |

- `units.HudFeedback`(유닛 HUD 일시 피드백: 피해 숫자·비용·예상 체력)과 `units.Presentation`(A-3a 유닛 표시 창구)은 서비스가 아니다. `FieldUnitPresenter`가 처음 읽힐 때 한 번 만들어 소유하는 내부 객체다.
- `CombatPlacementFeedback`·`CombatSceneDisplay`의 생성자는 그대로다. 배치 피드백은 숨김 때문에 `units`를 계속 받는다.
- A-3a(창구 도입)는 Factory를 바꾸지 않았다. 사건 Presenter가 생성자 안에서 `units.Presentation`을 읽는다.
- **Framework 계약:** 새 IServiceModule·ServiceKey·AddModule·Builder·FrameworkRuntime 단계는 없다. ServiceKeys·Group·asmdef·`JobType`은 바꾸지 않았다. 바뀐 공용 파일은 Factory의 위 두 생성자 호출(과 주석 한 줄)이다.
- **조립 순서:** 두 객체는 `units.Configure` 전에 읽어도 된다. 만들 때는 표시 집합을 보지 않고, 표시를 찾을 때만 본다(그 전에는 찾지 못함 = 지금의 `Configure` 전 동작). 그래서 기존 Factory 순서를 바꾸지 않았다.
- **검증 상태:** MSBuild·관리 실행 결과는 계획 §12.5에 있다. Unity 시험은 사용자 결과 대기다.
- **후속:** 이 계획의 다음 Factory 변경은 A-5(전역 연출 값 DB 설정 표)다.

## 10. 2026-10-18 후속 — 전투 연출 바탕 A-5 전역 연출 값 원본 (미전달, 이 문서와 함께 전달)

- 상태 원본: [연출 바탕 계획 §9·§12.8](../plans/2026-10-18-combat-presentation-foundation-plan.md). 결정은 [연출 조립 설계](../specs/2026-10-17-combat-presentation-composition-design.md) §9 I-5(Q-R17 A).
- **요약:** 전투 연출의 전역 시간·크기 값 11개(걸음·회전 시간, 적 행동·턴 간격, 소환 등장 4개, 포효 3개)가 `CombatSceneRoot`의 씬 값에서 View DB 설정 표 `CombatPresentationSettings`(항목 하나)로 옮겨졌다. 값은 `Combat.unity`의 직렬화 값 그대로다. 입력·배치 값(`innerCellInset`·커서 스냅)은 씬에 남는다.
- **공용 파일 변경:**

| 파일 | 바뀐 것 |
|---|---|
| `ServiceKeys/DataBaseGroupServiceKey.cs` | `ViewTableKey.CombatPresentationSettings`(`ICombatPresentationSettingsTableQuery`) 한 항목 |
| `Factory/DBFactory/Group/ViewDataBaseFactory.cs` | `CombatPresentationSettingsTable` 생성·Export 한 묶음, using 한 줄 |
| `DataBaseSystem/Queries/ViewDataBase/ViewDataBase.cs` | `IViewQuery.CombatPresentationSettingsTableQuery` 속성과 조회 한 줄 |
| `DataBaseSO/ViewGroup/ViewGroup.asset` | `databaseTables` 끝에 `- {fileID: 11400000, guid: 0ca5ccef885f41e9963cf3625b9ac7ef, type: 2}` 한 줄 |
| `Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs` | 연출 값을 `tables.PresentationSettings`에서 읽고, 맨 앞에서 `presentation.Validate()` 후 `root.ValidateReferences()`. 능력 동작 값은 `CombatAbilityMotionSettings.From(presentation)` |

- **위 §4·§5의 옛 설명:** §4의 `CombatSceneRoot.ValidatePresentationTiming`과 §5 표의 `root.CreateAbilityMotionSettings()`는 이 변경으로 없어졌다. 지금 연출 값 검사는 설정 Data의 `Validate()`이고, 능력 동작 값은 위 `From`이 만든다.
- **새 파일:**
  - 코드: `DataBaseSystem/Data/View/Table/CombatPresentationSettings/`(`CombatPresentationSettingsDataTableSO.cs`, `Item/CombatPresentationSettingsDataSO.cs`), `DataBaseSystem/Queries/ViewDataBase/ViewTable/CombatPresentationSettingsTable.cs`
  - 에셋: `DataBaseSO/ViewGroup/CombatPresentationSettings/CombatPresentationSettingsDataTable.asset`, `Item/CombatPresentationSettings_Default.asset`
  - **코드와 에셋은 한 커밋이어야 한다.** 표 에셋이 없으면 DB 생성이 실패한다.
- **Framework 계약:** 새 IServiceModule은 설정 표 조회 하나(`ICombatPresentationSettingsTableQuery`, 기존 설정형 표와 같은 모양)다. AddModule·Builder·FrameworkRuntime 단계·asmdef·`JobType`은 바꾸지 않았다.
- **실패 시점:** 값이 조건을 어기면(걸음 시간 0 이하 등) 표가 만들 때(부트의 DB 생성) 실패한다. 예전에는 전투 진입 때 씬 검사에서 실패했다. `InputSettings`·`StageFieldView`와 같은 방식이다.
- **팀 안내(값 조정 위치):**
  - 연출 시간을 조정하려면 씬의 `CombatSceneRoot`가 아니라 `DataBaseSO/ViewGroup/CombatPresentationSettings/Item/CombatPresentationSettings_Default.asset`을 고친다.
  - ScriptableObject라 Play 중에 바꾼 값은 Play를 끝내도 남는다. 씬 값은 Play를 끝내면 되돌아갔다.
- **검증 상태:** MSBuild·관리 실행 결과는 계획 §12.8에 있다. Unity 시험(`DataBaseAssetTests`·`ViewDataTableTests`·재생 묶음)과 Play From Boot 전투 진입은 사용자 결과 대기다.

## 11. 2026-10-18 후속 — 전투 연출 조각·조립 B-1 (미전달, 이 문서와 함께 전달)

- **바뀐 파일:** `Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs`의 `new CombatSceneEventPresenter(...)` 호출 하나를 **이름 있는 인자**로 바꿨다(구조 원칙 평가 R5). 같은 형식의 인자(시간 셋, 몸 조회 둘)가 이어져 있어 자리가 바뀌어도 컴파일되던 위험을 막는다. 넘기는 값과 순서는 그대로다.
- **조립의 다른 변화:** 없다. 조각 실행 재생기(`CombatPieceRunPlayer`)는 사건 Presenter 생성자 안에서 조립한다(Presenter 분리 결정 Q-S3 A). Factory·ServiceKeys·Group·asmdef·`JobType`은 바꾸지 않았다.
- **Framework 계약:** 새 Service·Key·AddModule 없음.
- **검증 상태:** MSBuild·관리 실행 결과는 [연출 바탕 계획](../plans/2026-10-18-combat-presentation-foundation-plan.md) §12.10에 있다. Unity 묶음은 사용자 결과 대기다.

## 12. 2026-10-18 후속 — 전투 연출 조각·조립 B-4 피격 플래시 (미전달, 이 문서와 함께 전달)

- **Factory·조립:** 바꾸지 않았다. Factory·ServiceKeys·Group·asmdef·`JobType`·`ViewGroup.asset`도 그대로다.
- **색 손잡이 사용(FW-203, DR-004):** 유닛 모델 Renderer의 색 손잡이를 이동 반투명과 피격 플래시가 함께 쓴다.
  - 표시 하나에 외형 소유자 `UnitModelAppearance`가 하나 있다. 옛 `MovementModelOpacity.cs`에서 파일 이름만 바꿨고 GUID는 그대로다.
  - 이 소유자가 두 층(투명도·플래시)을 기억했다가 공개 `IColorHandle.SetOverrideData` 한 번으로 합쳐 쓴다. 손잡이는 Renderer마다 하나다.
  - 원래 재질과 PropertyBlock은 마지막 층이 끝날 때만 직접 되돌린다. 기존 G4/DR-004 방식 그대로이고, `ColorHandle.Dispose`가 PropertyBlock을 되돌리지 않아서 생긴 방식이다.
  - 플래시만 있을 때는 재질을 바꾸지 않는다.
- **손잡이 수명:** 유닛이 처음 반투명이나 플래시를 쓸 때 손잡이를 만든다. 풀 표시가 해제될 때까지 등록된 채 남는다(옛 이동 반투명과 같은 수명).
  - 이제 피격만 나도 맞은 유닛마다 손잡이가 생긴다.
  - 그 뒤 다른 시스템이 같은 유닛 Renderer에 손잡이를 만들려 하면 ColorSystem이 거절한다. 지금 그런 호출처는 없다.
  - 유닛 Renderer에 색을 거는 기능을 더하려면 이 소유자를 거치도록 같이 정해야 한다.
- **DB View SO 필드:** `CombatUnitReactionPresentationSO`에 두 필드를 더했다.
  - `hitFlashColor`(기본 빨강)와 `hitFlashDuration`(기본 0 = 플래시 없음). 음수·NaN·무한은 DB 생성에서 실패한다.
  - Data `CombatUnitReactionPresentationData`에 `HitFlashColor`·`HitFlashSeconds`·`HasHitFlash`가 생겼다.
  - 에셋 `DataBaseSO/ViewGroup/CombatPresentation/CombatUnitReaction_Default.asset`에 빨강·0.18초를 넣었다. 모든 캐릭터가 이 에셋 하나를 공유한다.
  - 새 테이블·Group 등록은 없다.
- **Framework 계약:** 새 Service·Key·AddModule 없음.
- **검증 상태:** MSBuild·관리 실행 결과는 [연출 바탕 계획](../plans/2026-10-18-combat-presentation-foundation-plan.md) §12.13에 있다. Unity 묶음은 사용자 결과 대기다.

## 13. 2026-10-19 후속 — 전투 가방 P-1 아이템 외형 표·재고 표시 (미전달, 이 문서와 함께 전달)

- **ServiceKeys:** `ServiceKeys/DBFactory/Group/ViewTableKey.cs`에 `ConsumableItemVisual`(`IConsumableItemVisualTableQuery`) 한 줄을 더했다.
- **DB Factory:** `Factory/DBFactory/Group/ViewDataBaseFactory.cs`가 `ConsumableItemVisualDataTableSO`로 `ConsumableItemVisualTable`을 만들어 등록한다(특성 외형 다음 줄). `IViewQuery`에 `ConsumableItemVisualTableQuery`가 생겼다.
  - `ViewGroup.asset`에 표 에셋 `DataBaseSO/ViewGroup/ConsumableItemVisual/ConsumableItemVisualDataTable.asset`을 더했다. 표가 없으면 DB 생성이 실패하므로 코드와 한 커밋이다.
  - `IViewQuery`를 구현하는 시험 가짜 7곳에 같은 멤버를 더했다(값은 null 또는 `NotSupportedException`, 기존 설정 표 멤버와 같은 방식).
- **표시 Factory:** `Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs`가 씬 루트의 가방(`root.Pack`)에 외형 표를 넘기고(`ConfigureItems`), 표시(`CombatSceneDisplay`) 생성자 끝의 선택 인자로 가방을 넘긴다. 가방이 없는 가짜 루트에서는 null이다.
- **조정자:** `CombatViewSystem`이 Buffer의 `ItemInventoryReadSink`를 바인딩 때 받는다(`BindServices` 끝의 선택 인자, 시험에서 생략하면 빈 재고).
- **Framework 계약:** 새 Service·AddModule 없음(DB 표 등록은 기존 `IExportSystemServices` 경로).
- **검증 상태:** 사용자 결정으로 이번 Phase는 MSBuild·관리 실행을 건너뛰었다([가방 계획](../plans/2026-10-19-combat-pack-item-plan.md) 머리말). Unity에서 처음 확인된다.

## 14. 2026-10-19 후속 — 전투 가방 P-2 드래그 장착 (미전달, 이 문서와 함께 전달)

- **입력 Factory:** `CombatSceneInputFactory.CreateSceneInteraction`에 끝 인자 `Func<int, int> itemStock`(조정자가 Buffer 재고를 읽어 넘김)을 더하고, 아이템 드래그 입력 `CombatItemDragInteraction`을 만들어 입력 묶음에 넣는다. `CreateInteraction`에는 끝 선택 인자 `item`이 생겼다(기존 호출은 그대로).
- **표시 계약:** `ICombatBattleDisplay`가 `ICombatItemDragFeedback`(끄는 아이템 그림)을 더 상속한다. `CombatSceneDisplay`가 가방에 넘긴다. 이 계약을 구현하는 시험 가짜 하나(`CombatViewSystemTests.FakeDisplay`)에 빈 구현을 더했다.
- **서비스:** `ICombatViewService`에 `ItemInput`(가방 UI 입구)이 생겼다. 새 Service 등록은 없다(기존 View 서비스의 멤버). 시험 가짜 4곳에 `null` 구현을 더했다.
- **검증 상태:** 사용자 결정으로 MSBuild·관리 실행을 건너뛰었다(가방 계획 머리말).

## 15. 2026-10-19 후속 — 전투 가방 P-3 HUD 아이템 아이콘 (미전달, 이 문서와 함께 전달)

- **표시 Factory:** `CombatSceneDisplayFactory`의 `units.Configure(...)` 끝에 `tables.Items`(아이템 외형 표, P-1에서 표시 표 묶음에 더함)를 넘긴다. `FieldUnitPresenter.Configure`의 끝 선택 인자 `itemVisuals`가 받아 유닛 HUD에 장착 아이템 아이콘을 그린다. 다른 호출은 그대로다.
- **새 등록·서비스 키:** 없다.
- **검증 상태:** 사용자 결정으로 MSBuild·관리 실행을 건너뛰었다(가방 계획 머리말).

## 16. 2026-10-19 후속 — 전투 카메라 연출 K-1 카메라 창구 (미전달, 이 문서와 함께 전달)

- **카메라 효과 사용:** 전투 표시가 민형님 `CameraEffectSystem`을 처음 쓴다. 공개 `ICameraEffectService.Dispatch`(줌 `CameraZoomData`·흔들림 `CameraShakeData`)만 부르고, `CameraGroup`·`CameraEffectSystem`·두 카탈로그·`DelayService` 코드는 바꾸지 않았다(사용자 조건 "Framework 측 수정은 안 하는 방향", [카메라 연출 조각 설계](../specs/2026-10-19-combat-camera-pieces-design.md)).
- **서비스 묶음:** `CombatDisplayServices`에 끝 선택 인자 `cameraEffects`를 더했고, `From`이 `render.CoreGroup.CameraGroup.CameraEffectSystem`을 고른다. 시험 조립은 생략할 수 있다(그때 카메라 연출이 없음).
- **표시 Factory:** `CombatSceneDisplayFactory`가 카메라 창구 `CombatCameraWindow` 하나를 만들어 맵 프랍 표시(`StageViewPropPresenter`, 끝 선택 인자)와 사건 Presenter(이름 있는 선택 인자 `camera:`)에 넘긴다.
  - 맵 프랍을 세운 직후 프랍 안의 버추얼 카메라(Stage.prefab의 Day·Night Camera)를 한 번 읽어 이름·기준 렌즈값·흔들림 채널을 창구에 넘기고, 프랍을 없애기 직전에 거둔다. 렌즈에는 쓰지 않고 읽기만 한다(카메라 카탈로그 문서의 "가진 참조에서 설정 복사" 방식). 쓰는 것은 모두 `Dispatch`다.
- **효과에 기대는 것:** 줌은 같은 카메라의 새 요청이 앞 요청을 덮어쓰는 규칙으로 취소·되돌림을 한다(기준값 0초 줌). 끝 알림이 없어 조각이 자기 시계로 센다. 흔들림 채널은 프리팹 Listener에서 읽는다(Listener 요청은 `handoffs/2026-10-19-camera-impulse-listener-request-minhyeong.md`).
- **새 등록·서비스 키·AddModule:** 없다.
- **지금 동작:** K-1은 연결만 하고 쓰는 조립이 없어 화면이 바뀌지 않는다. 포효 줌·흔들림은 K-3이다.
- **K-3 추가(2026-10-19):** `CombatSceneDisplayFactory`가 사건 Presenter에 이름 있는 선택 인자 `cameraMotion: CombatCameraMotionSettings.From(presentation)`(View DB 설정 표의 카메라 값)을 더 넘긴다. 포효 처리기가 이 값과 카메라 창구로 줌·흔들림 항목을 넣는다. 새 등록·서비스 키는 없다.
- **K-4 추가(2026-10-19):** 카메라 창구 생성자에 시계 인자가 생겼다 — `new CombatCameraWindow(services.CameraEffects, () => Time.time)`. 흔들림 겹침 규칙(진행 중이면 더 센 흔들림만 보냄, 설계 K8)이 이 시계로 진행 중을 판단하며, Cinemachine Impulse와 같은 Unity 시간이다. Factory 쪽 변경은 이 한 줄과 주석이고, 사건 Presenter가 받은 창구·카메라 값을 캐릭터 처리기에도 넘겨 피격·사망 흔들림을 넣는다(Presenter 안 조립). 새 등록·서비스 키는 없다.
- **K-5 추가(2026-10-19):** 카메라 값 `CombatCameraMotionSettings.From(presentation)`을 Factory에서 한 번 만들어 사건 Presenter(`cameraMotion:`)와 재생 조정 `CombatSceneEventPlayback`(끝 선택 인자 `camera`·`cameraMotion`)에 같이 넘긴다. 재생 조정은 기차 앞면이 필드에 들어오는 순간(접촉 예약의 진입 알림) 같은 카메라 창구로 기차 흔들림을 보낸다. 새 등록·서비스 키는 없다.
- **K-6 추가(2026-10-19):** 재생 조정이 같은 카메라 창구로 기차 줌(줌 대여 하나, 필드 진입에 줌인·이탈에 복귀)도 낸다. Factory 코드는 K-5 그대로다.
- **검증 상태:** MSBuild·관리 실행 결과는 [카메라 연출 조각 계획](../plans/2026-10-19-combat-camera-pieces-plan.md) §6에 있다. Unity 묶음은 사용자 결과 대기다.

## 17. 2026-10-19 후속 — 전투 유닛 호버 공격 범위 U-2 입력 Factory (미전달, 이 문서와 함께 전달)

- **바뀐 곳:** `CombatSceneInputFactory` 하나. 표시 Factory(`CombatSceneDisplayFactory`)·서비스 묶음·서비스 키·`AddModule`·등록은 바뀌지 않았다.
- **씬 조립:** `CreateSceneInteraction`이 유닛 호버 공격 범위 `CombatUnitRangeHover`를 만들어(입력 Buffer 쓰기·읽기, 전투 상태 읽기, 표시가 주는 칸 집기·입력 표시·UI 가림, Gate) `CreateInteraction`의 새 끝 선택 인자 `rangeHover`로 넘긴다. `CreateInteraction`은 이 값을 `CombatInteraction` 생성자의 새 끝 선택 인자로 넘긴다.
- **시험 조립:** `CreateInteraction`을 직접 쓰는 조립(배치·공격만 검증)은 `rangeHover`를 넘기지 않으므로 지금과 같다.
- **조정자:** `CombatViewSystem` Tick의 `_interaction.Poll(...)` 한 줄이 사실·캐릭터도 넘긴다(새 오버로드). 옛 한 인자 `Poll(preview)`는 시험용으로 남았다.
- 설계·계획: [호버 공격 범위 설계](../specs/2026-10-19-combat-unit-hover-attack-range-design.md) UR5·UR6, [계획](../plans/2026-10-19-combat-unit-hover-attack-range-plan.md) U-2(§7.3).

## 18. 2026-10-19 후속 — 전투 공격 궤적 TJ-3 표시·입력 Factory (미전달, 이 문서와 함께 전달)

- **바뀐 곳:** `CombatSceneDisplayFactory` 한 줄, `CombatSceneInputFactory` 두 곳. 서비스 묶음·서비스 키·`AddModule`·등록은 바뀌지 않았다(새 내부 협력 객체는 등록하지 않음).
- **표시 Factory:** 공격 피드백 생성이 궤적 경로 구성을 받는다 — `new CombatAttackFeedback(field, units.HudFeedback, new CombatAttackTrajectory(field, units.Presentation, units))`. 경로 구성은 필드(좁은 캔버스 계약 `ICombatTrajectoryCanvas`)·표시 창구(외형·무기)·유닛 위치 읽기(좁은 계약 `ICombatUnitPlacementReader`, `FieldUnitPresenter` 명시 구현)만 받는다.
- **입력 Factory:** `CreateInteraction`에 끝 선택 인자 `trajectory`(`ICombatAttackTrajectoryFeedback`)가 생겼고 `AttackTargetingInteraction` 생성자의 끝 선택 인자로 넘긴다. 씬 조립(`CreateSceneInteraction`)만 `trajectory: display`를 넘기고, 직접 쓰는 시험 조립은 지금과 같다(궤적 없음).
- **하위 Render 사용:** 궤적은 하위 Mesh·Color(`MultiLineCreateData`·Color 핸들)를 View에서 조합한다(`FieldCellFill`과 같은 꼴). 상위 `DrawPointLineSystem`·하위 Render 코드·`AreaOutline` 셰이더는 고치지 않았다.
- **MultiLine UV에 대해:** `CalcLineUVs`는 지금 누적 거리를 폭 방향(uv.y)에 넣고 길이 방향(uv.x)은 구간마다 0→1이다. 사용자 결정으로 이번에는 고치지 않았다. 궤적은 같은 길이 구간 하나에 점 하나(`_DashCount = 1`)로 그리고 `uvScale = 1 / 구간 길이`를 넘기므로, 나중에 UV를 길이 방향 누적값으로 고쳐도 궤적 화면은 같다. 고칠 때 확인할 사용자는 궤적과 격자 테두리(`DrawPointLine`, 텍스처 없는 재질이라 화면 같음) 둘이다.
- **관찰:** MultiLine 빌더가 갱신마다 List 다섯 개를 새로 만든다(궤적은 호버 칸이 바뀔 때만 갱신해 Hot Path는 아님).
- 설계·계획: [공격 궤적 설계](../specs/2026-10-19-combat-attack-trajectory-design.md) TJ2·TJ7, [계획](../plans/2026-10-19-combat-attack-trajectory-plan.md) TJ-3(§7.3).

## 19. 2026-10-19 후속 — 전투 클립 이펙트 EF-2 이펙트 창구 + 전투용 이펙트 등록 요청 (미전달, 이 문서와 함께 전달)

- **바뀐 곳:** `CombatSceneDisplayFactory` 두 줄. 서비스 묶음(`CombatDisplayServices`, 이미 `Effects`를 받음)·서비스 키·`AddModule`·등록은 바뀌지 않았다.
  - 창구 생성: `var effects = new CombatEffectWindow(services.Effects, field.CellsRoot);`(폭발 이펙트 Presenter 바로 아래)
  - 사건 Presenter에 이름 있는 선택 인자 `effects: effects`를 더 넘긴다. Presenter가 공격 처리기 → 클립 공격 조립으로 넘기고, 결과 재생 취소 때 `StopAll`·해제 때 `Release`를 부른다(창구 수명 주인 = 사건 Presenter).
- **이펙트 시스템 사용:** 공개 `IEffectSystemServices.GetEffectHandle()`과 `EffectScope.Play(EffectScopeRequest)`·`StopAll()`만 쓴다. Render 코드(`EffectSystem`·`EffectScope`·`EffectPool`·`ParticleStopNotifier`·Effect 표)는 고치지 않았다.
  - Scope는 창구가 처음 낼 때 **하나만** 받는다(낼 때마다 받으면 Shutdown까지 쌓임 — 사용 평가 G2). 그래도 전투 표시를 새로 조립할 때마다 하나씩 쌓이는 것은 남는다(기차·폭발 Presenter와 같은 종류, Scope 해제 공개 API 요청은 G2 그대로).
  - 이펙트는 필드 루트(`CellsRoot`) 아래에 월드 위치·회전을 지역 값으로 바꿔 놓고 유닛을 따라가지 않는다. 유닛 아래에 붙이면 유닛 표시가 꺼지거나 파괴될 때 멈춤 알림이 오지 않거나 풀의 이펙트가 같이 파괴될 수 있어서다(설계 E-R1).
- **요청 — 전투용 `EffectType` 등록:** 이제 View 클립 묶음 SO 공격 칸(`attackEffect`)이 `EffectType`을 고른다. 지금 등록된 종류는 `TestFire`·`Boom`뿐이라 전투 공격 이펙트(베기·총구 불꽃 등)가 필요하면 Render 쪽에 enum 값 + `EffectDataSO` 항목(프리팹 **루트**에 ParticleSystem·`ParticleStopNotifier`, 프로그래머 가이드 FW-012·DR-006)을 등록해 주시면 된다. 등록 안 된 종류를 고르면 View DB 검사는 통과하고 **처음 낼 때** `EffectSystem`이 예외를 낸다(View DB가 Render 표를 읽지 않음).
- **지금 동작:** 모든 공격 칸이 `Empty`라 화면은 바뀌지 않는다.
- **요청 범위 넓힘(2026-10-19, 투사체 접촉 이펙트 IE-b):** 공격 표시 SO(`CombatAttackPresentationSO`)의 새 칸 `impactEffect`도 같은 `EffectType`을 고른다(대포 착탄 폭발·Ballista 관통 등, 투사체가 대상에 닿는 순간 그 자리). **Factory는 이번에 바뀌지 않았다** — 사건 Presenter가 이미 받은 같은 창구를 공격 재생에 넘긴다. 등록이 필요한 이펙트가 착탄·관통용까지 늘어난다는 알림이다. 설계: [접촉 이펙트 설계](../specs/2026-10-19-combat-projectile-impact-effect-design.md).
- **요청 범위 넓힘(2026-10-20, 피격 이펙트 HE-c):** 반응 SO(`CombatUnitReactionPresentationSO`)의 새 칸 `hitEffect`도 같은 `EffectType`을 고른다(맞은 유닛 몸 중심, 피격·치명타). **Factory는 이번에도 바뀌지 않았다** — 사건 Presenter가 같은 창구를 캐릭터 처리기에 넘긴다. 피격 이펙트 프리팹은 **+Z가 맞은 방향(공격 출처 반대쪽)으로 튀게** 만들어 주시면 된다(대칭 이펙트는 상관없음). 설계: [피격 이펙트 설계](../specs/2026-10-20-combat-hit-effect-design.md).
- 설계·계획: [클립 이펙트 설계](../specs/2026-10-19-combat-clip-effect-design.md) EF1·EF6, [계획](../plans/2026-10-19-combat-clip-effect-plan.md) EF-2.

## 20. 2026-10-20 후속 — 런 덱 C1 편집 창 조립 순서 (미전달, 이 문서와 함께 전달)

천명님 C1 요청 §3의 조립 단계를 사용자 지시로 이 세션이 구현했습니다. 설계는 [런 덱 연결 설계](../specs/2026-10-20-run-deck-view-integration-design.md) RV1b, 계획은 [RD-1](../plans/2026-10-20-run-deck-view-integration-plan.md)입니다.

- **`FrameworkRuntimeFactory.Create`:** LifeCycle 조립을 InGame보다 **먼저 만듭니다.**
  - `LifeCycleSystemFactory.CreateComposition(faults)`가 그룹과 덱 편집 창을 함께 돌려줍니다.
  - 그 창을 `InGameSystemFactory.CreateComposition(battleSource, faults, lifeCycle.DeckEditAvailability)`에 넘깁니다.
  - **내보내는 순서(Buffer → Global → InGame → LifeCycle)와 공개 서명은 그대로입니다.**
- **`InGameSystemFactory`:** `CreateComposition`에 선택 인자 `deckEditAvailability`를 더했습니다. `Compose`는 그 값을 `InGameCoreSystemFactory.Create(..., deckEditAvailability)`로 넘깁니다.
  - InGame은 창을 등록하지 않고 참조만 넘깁니다(공용 `RuntimeFaultBoundary`와 같은 방식).
- **`LifeCycleSystemFactory`:** 새 `CreateComposition`이 창을 만들어 `StageLifeCycle`에 넘깁니다.
  - 창은 `AddService<IDeckEditAvailabilityRead>`로만 등록합니다. 생명주기 콜백이 없어 `AddModule`은 하지 않습니다(FW-001/002).
  - 기존 `Create`는 `.Group`을 돌려주는 위임으로 남겨 시험 호출이 그대로 동작합니다.
- **새 서비스 키·새 파일:** `LifeCycleSystemServiceKey.DeckEditAvailability`, `Factory/Runtime/LifeCycle/LifeCycleSystemComposition.cs`(+ `.meta`)
- `ILifeCycleSystemGroupService`에는 멤버를 더하지 않았습니다(시험 가짜 영향 회피, 요청에서도 선택 사항).

## 21. 2026-10-20 후속 — 고지대 걸음 뜀 높이 한 줄 (미전달, 이 문서와 함께 전달)

[고지대 View 계획](../plans/2026-10-20-combat-high-ground-view-plan.md) HG-3입니다.

- **`CombatSceneDisplayFactory`:** 사건 Presenter 생성에 이름 있는 인자 한 줄 `stepClimbHop: presentation.StepClimbHop`을 더했습니다. 값은 View DB 설정 표(`CombatPresentationSettings`)의 새 값입니다.
- `CombatSceneEventPresenter`의 새 인자는 기본값 0인 끝 선택 인자라, 다른 생성 호출은 그대로 컴파일됩니다.
- 새 서비스·서비스 키·파일 변경은 없습니다.

## 22. 2026-10-23 후속 — 전투 배경 맵을 카메라 연결 전에 세우기 + 안 쓰는 표 정리 (미전달, 이 문서와 함께 전달)

민형님이 물어보신 "맵 생성 단계를 BindScene 이전으로"를 반영했습니다.
- 설계: [배경 설계](../specs/2026-10-23-combat-backdrop-before-camera-bind-design.md) BD1~BD6
- 계획·실행 기록: [배경 계획](../plans/2026-10-23-combat-backdrop-before-camera-bind-plan.md) BK-1
- Stage 브랜치(`e706b65` 위)에서 했습니다. 컴파일·Unity 확인 전입니다.

**언제 생기나(바뀐 점)**

| | 전 | 후 |
|---|---|---|
| 맵 생성 | Combat Enter 100(View 연결) 안의 `Install` | **Combat Enter -195**(`CombatViewSystem.BackdropEnterPriority` → 2026-10-23 대신함: `SceneNodeOrder.CombatBackdrop`, 골격 점검 K-4) |
| -190 카메라 수집(`GameBootstrap` → CameraGroup `BindScene`) | 맵 카메라 없음. AfterLoad 10 재수집 때 들어감 | **맵 카메라(`Day Camera`·`Night Camera`)가 들어감** |
| 맵 없앰 | 표시 `Clear`·`Release` | 씬 이탈 Exit -100(`ReleaseScene`)·종료 |

- 스테이지 키는 씬 로드 전 전투 준비가 Buffer에 게시하므로 -195에서 읽을 수 있습니다.
- 낮/밤 화면 전환(`CombatPhaseTransitionPresenter`)과 줌은 지금도 카메라 이름으로 목록에서 찾습니다. 이제 -190 수집부터 맵 카메라가 있습니다.

**Factory(`CombatSceneDisplayFactory`)**
- 새 입구 `CreateBackdrop(root, tables)`: 배경 Presenter를 만듭니다. 부모는 필드 칸 루트입니다. 조정자가 -195에서 부릅니다.
- `Create(root, tables, services, backdrop = null)`: 끝 선택 인자를 더했습니다.
  - 창구를 만든 직후 `camera.AttachMap(backdrop.MapCameras)`를 부릅니다.
  - 배경 Presenter를 만들던 줄은 지웠습니다.
  - 표시 생성자에는 배경 대신 카메라 창구를 넘깁니다.
  - 표시를 조립하는 기존 시험 6개 파일은 그대로 컴파일됩니다.
- `StageViewPropPresenter` 생성자는 `(표, 부모)` 두 인자가 됐습니다. 창구 인자를 뺐습니다.
  - 민형님 `MapSoDatabaseAssemblyTests`의 두 인자 생성은 그대로입니다.
  - `Show`의 레거시 프리팹 / `MapAssembler` 분기와 보정 값은 바꾸지 않았습니다.

**바꾸지 않은 것**
- `GameBootstrap`의 -200·-190 Node.
- CameraGroup·카메라 카탈로그·카메라 효과·로딩 시스템.
- ViewStage 표, `MapDefinitionSO`·`MapAssembler`.
- 새 서비스·서비스 키는 없습니다.

**안 쓰는 표 정리(사용자 지시, ViewStage Phase 1 뒤 남은 것)**
- 지운 것:
  - `DataBaseSO/ViewGroup/StageEnvironmentVisual/`(표 1 + 항목 3, 카탈로그에서 이미 빠짐, 회전 값 모두 빈 값)
  - `StageEnvironmentVisualDataTableSO.cs`
  - `StageViewPropDataTableSO.cs`(참조 0)
  - 모두 `.meta`와 함께 지웠습니다. 지우기 전에 GUID 참조가 없는지 확인했습니다.
- 지운 스크립트를 쓰던 우리 시험 `TerrainOperationalDisplayTests`는 지형 회전 넣는 곳을 `ViewStageDataTableSO`·`ViewStageDataSO`로 옮겼습니다.
- **남겨 둔 것(Phase 2·소비자 전환 때 정리 권장):**
  - 레거시 항목 SO `StageViewPropDataSO`·`StageEnvironmentVisualDataSO`
  - 호환 표 두 클래스의 옛 `IDataProvider` 생성자
  - `MapSoDatabaseAssemblyTests`·`EnvironmentStageAuthoringTests`가 아직 이것들을 씁니다.
- `EnvironmentAuthoringAssetTests`의 Template 시험(`StageEnvironmentVisual_Template.asset`)은 병합 전부터 없는 에셋을 읽어 이미 실패하던 것입니다.

**확인 부탁**
1. 민형님이 보신 "배경에 카메라·셰이더 효과가 안 먹는" 증상이 정확히 어떤 경우였는지 알려 주세요. 어떤 효과가, 언제, 맵 SO였는지 레거시 프리팹이었는지가 궁금합니다. 그래야 이 변경으로 풀렸는지 확인할 수 있습니다.
2. 맵 SO(`MapDefinitionSO`)로 만든 맵에도 버추얼 카메라 이름이 `Day Camera`·`Night Camera`로 같은지 확인 부탁드립니다. 낮/밤 전환이 이 이름을 찾습니다.
3. ViewStage 항목에 `gameObject`(레거시)와 `map`이 둘 다 차 있으면 `map`이 이깁니다. 지금은 이를 경고하는 검사가 없습니다. Phase 2 저장 도구가 `map`을 넣을 때 `gameObject`를 비우는지, 아니면 "둘 중 하나만" 검사를 넣을지 정해 주세요.

## 23. 2026-10-24 후속 — 전투 View 단계 1라운드: 표시 조립 줄 (미전달, 이 문서와 함께 전달)

근거: [View 단계 설계](../specs/2026-10-24-combat-view-stage-design.md) W4, [1라운드 계획·실행 기록](../plans/2026-10-24-combat-view-stage-round1-plan.md) Phase V-c. 코드는 작업 트리에 있고 커밋 전입니다. Unity 확인 문제 없음(사용자).

- `Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs`(연결 줄)
  - 옛 손패(`HandPresenter`)를 걷었습니다. 지운 것은 지역 변수, `Configure` 줄, 넘기던 인자 셋(배치 피드백·포인터 변환·표시 생성자)입니다. 손패 선택은 유지 덱 카드가 합니다.
  - 정보창은 `root.UnitInfo.Configure(…)`입니다(`?.` 삭제).
  - 가방은 `root.Pack`을 바로 씁니다(null 삼항·주석 삭제). 위의 씬 루트 참조 검사가 정보창·가방 연결을 보장합니다.
- `CombatSceneDisplay` 생성자는 인자가 하나 줄었고(`hand`), 끝의 네 인자(정보창·유닛 HUD 층·가방·결과 화면)가 필수가 됐습니다. 생성하는 곳은 이 Factory 하나입니다.
- 서비스·키·그룹 노출과 Framework·GlobalSystem 코드는 바꾸지 않았습니다.

## 24. 2026-10-25 후속 — 전투 View 단계 2라운드: 생성 주입·표시 서비스 필수·화면 전환 (미전달, 이 문서와 함께 전달)

근거: [2라운드 설계](../specs/2026-10-24-combat-view-stage-round2-design.md), [2라운드 Task 계획·실행 기록](../plans/2026-10-24-combat-view-stage-round2-plan.md). 코드는 작업 트리에 있고 커밋 전입니다. Phase V2-a~V2-g 모두 Unity 확인 문제 없음(사용자). V2-g의 마지막 변경(아래 "재생 조정") 하나는 그 확인 뒤라 컴파일 미확인입니다. 줄 번호는 기준 커밋 `23436b00` 기준입니다.

- **연결 줄 `Factory/Runtime/InGame/View/InGameViewSystemFactory.cs:60`(호진님 줄):**
  ```diff
  -            var combatView = new CombatViewSystem(faults, new OrderedCombatPresentation());
  +            var combatView = new CombatViewSystem(faults, new OrderedCombatPresentation(),
  +                CombatSceneFactory.Instance, CombatSceneFactory.Instance);
  ```
  조정자가 Factory를 정적으로 부르지 않고 생성 계약 둘을 받습니다.
- **새 파일 `Factory/Runtime/InGame/View/CombatSceneFactory.cs`(+ `.meta`):**
  - 생성 계약 둘을 함께 구현하는 상태 없는 `sealed` 클래스입니다. `Instance` 하나만 씁니다.
    - `ViewSystem/Combat/Display/ICombatSceneFactory.cs`: 배경·표시
    - `ViewSystem/Combat/Input/ICombatSceneInputFactory.cs`: 연결 입력 묶음
  - 기존 정적 Factory 둘(`CombatSceneDisplayFactory`·`CombatSceneInputFactory`)에 위임합니다. 정적 Factory는 시험이 직접 부르므로 남겼습니다.
- **`CombatSceneInputFactory.cs`:**
  - V2-e: `CreateSceneInput`을 더했습니다. 조정자의 옛 `CreateInput`이 만들던 일곱을 같은 순서로 만들어 연결 입력 묶음 `CombatSceneInput`으로 돌려줍니다.
  - V2-g: 거절 안내의 생략 갈래(`:63-64`, 현강님 줄)를 지우고 환경 호버를 늘 만듭니다.
- **`CombatSceneDisplayFactory.cs`(V2-g):**
  - `:147-150`(민형님 줄): 화면 전환 Presenter를 늘 만듭니다.
    ```diff
    -            var phaseTransition = services.ScreenTransitions != null
    -                ? new CombatPhaseTransitionPresenter(services.ScreenTransitions,
    -                    CombatPhaseTransitionSettings.CreateDefault(), events.BeginEvent)
    -                : null;
    +            var phaseTransition = new CombatPhaseTransitionPresenter(services.ScreenTransitions,
    +                CombatPhaseTransitionSettings.CreateDefault(), events.BeginEvent);
    ```
  - `:55` `services` 인자 설명을 "모두 필수, 시험은 가짜"로 고쳤습니다.
- **`ViewSystem/Combat/CombatDisplayServices.cs`(V2-g):**
  - 선택 인자 넷(오디오·화면 전환·UI·카메라 효과)이 필수입니다. 민형님 줄은 `:27`(화면 전환 인자 설명)·`:35`(오디오 인자)·`:48`(화면 전환 대입)·`:88`(화면 전환 요약)입니다.
  - 운영 `From`은 이미 넷을 모두 넘기므로 운영 동작은 같습니다.
- **재생 조정 `ViewSystem/Combat/Playback/CombatSceneEventPlayback.cs`(V2-g 뒤 사용자 결정 Q-V2g-1 A, 컴파일 미확인):**
  - 생성자의 선택 인자 셋(화면 전환·카메라 창구·카메라 연출 값, 우리 줄 `:53-55`)이 필수입니다. 부르는 곳은 표시 Factory 하나이고 늘 넘깁니다.
  - 그래서 닿지 않게 된 `?.`를 걷었습니다. 민형님 줄은 일곱입니다.
    - `:62` `_phaseTransition = phaseTransition` → null이면 `ArgumentNullException`
    - `:72`·`:75` `_phaseTransition?.IsPlaying != true` → `!_phaseTransition.IsPlaying`
    - `:128` `_phaseTransition?.TryBegin(e, completed) == true` → `_phaseTransition.TryBegin(e, completed)`
    - `:164` `transitionPlaying`, `:172` `Tick`, `:188` `CancelPhaseTransition`
  - 기차 카메라(우리 줄)도 늘 만듭니다. 기차 흔들림·줌 값이 없으면 지금처럼 진입·이탈 알림을 받지 않습니다.
- **화면 전환 사건 고르기 `ViewSystem/Combat/Playback/CombatPhaseTransitionPresenter.cs`(V2-d, Q-V17 A):**
  - `:33-36`: 어떤 사건이 전환을 거는지를 사건 형식 표 `CombatEventTypeTable.TransitionOf`에서 읽습니다. `:37`은 그대로입니다.
  - `:4`: 더 쓰지 않게 된 `using …Contracts.Common`을 `using …Playback.Ordering`으로 바꿨습니다.
  - 전환 재생·완료 합류·Scope 대여는 그대로입니다. `TryBegin`의 서명·동작이 같아서 민형님 시험 `CombatPhaseTransitionPresenterTests`는 바꾸지 않았습니다.
- **고친 시험(허락, Q-T5):** `CombatSceneEventPresenterTests.cs:173`: `new CombatDisplayServices(…)` → 시험 도우미 `CombatViewSystemTests.DisplayServices(…, screenTransitions: …)`. 넘긴 화면 전환은 그대로이고, 나머지 셋(오디오·UI·카메라 효과)은 아무것도 하지 않는 가짜입니다.
- **시험 조립에 생긴 차이:** 이제 시험 조립에도 화면 전환이 생겨, 국면 사건(전투 진입·밤 끝·승리)은 다음 연출 Tick에 끝납니다. 시험의 화면 전환 가짜는 `Play` 즉시 끝나는 Scope를 돌려줍니다. 사건당 1 Tick을 넘지 않고, 이것으로 깨지는 시험은 없다고 판단했습니다(Unity에서 확인 문제 없음).
- 서비스·키·그룹 노출과 Framework·GlobalSystem 코드는 바꾸지 않았습니다.

## 25. 2026-10-25 후속 — 전투 View 단계 3라운드: 화면 전환 설정·표시·입력 생성 줄 (미전달, 이 문서와 함께 전달)

근거: [3라운드 설계](../specs/2026-10-25-combat-view-stage-round3-design.md), [3라운드 Task 계획·실행 기록](../plans/2026-10-25-combat-view-stage-round3-plan.md). 코드는 작업 트리에 있고 커밋 전입니다. Phase V3-a~V3-i 모두 **컴파일 미확인**입니다. 줄 번호는 기준 커밋 `35562e96` 기준입니다.

- **화면 전환 설정 `ViewSystem/Combat/Playback/CombatPhaseTransitionSettings.cs`(민형님 파일, V3-g, Q-V25 A):**
  ```diff
  -        internal static CombatPhaseTransitionSettings CreateDefault(Func<string, int> findLayer = null)
  +        internal static CombatPhaseTransitionSettings From(CombatPresentationSettingsData settings, Func<string, int> findLayer = null)
  ```
  - 값(레이어·카메라 이름·색·바뀌는 속도·양)은 View DB 전투 연출 설정 표에 있습니다. 기본값은 옛 상수(`AfterRendering`·`Day Camera`·cyan / `BefalRendering`·`Night Camera`·magenta / 30·40)와 같습니다.
  - 레이어가 없으면 예외인 검사는 그대로입니다. 바뀐 민형님 줄은 `:7·19·23-24·28·38-39`입니다.
- **표시 Factory `Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs`**
  - V3-g
    - 전환 설정을 설정 검사 바로 뒤(씬 객체를 건드리기 전)에 `From(presentation)`으로 만듭니다.
    - 배경이 있으면 두 카메라 이름이 배경의 맵 카메라 사실(`backdrop.MapCameras`)에 있는지 봅니다(없으면 `InvalidOperationException`, 이름을 문구에). 이전에는 첫 전환 때 카메라 카탈로그 조회에서 실패했습니다.
    - 환경 폭발 이펙트는 설정 값(`EnvironmentExplosionEffect`, 기본 `Boom`)을 넘깁니다(`EffectType.Boom` 고정 삭제).
  - V3-d: 가시밭 외형 프리팹이 필수가 되어(표 검사) 가시밭 표시를 늘 만듭니다.
  - V3-h: 표시 생성자에 소리 프리셋(`tables.SoundPresets`)을 넘깁니다.
  - V3-i: 사건 Presenter를 묶음으로 만듭니다.
    ```diff
    -            var events = new CombatSceneEventPresenter(units: units, stepSeconds: …, audio: audio, … 이름 있는 인자 열아홉 …);
    +            CombatEventPresentationValues eventValues = CombatEventPresentationValues.From(presentation);
    +            var fieldPorts = new CombatFieldEventPorts(consumeThorn: thorns.Consume, … 필드 창구 여덟 …);
    +            var events = new CombatSceneEventPresenter(units, eventValues, audio, tables.SoundPresets, camera, effects, field.CellsRoot,
    +                fieldPorts);
    ```
    기차 재생은 같은 카메라 연출 값(`eventValues.CameraMotion`)을 씁니다. 넘기는 출처는 옛 이름 인자와 하나씩 같습니다.
- **입력 Factory `CombatSceneInputFactory.cs`·생성 계약 `ICombatSceneInputFactory`·`CombatSceneFactory.cs`(V3-b·V3-d·V3-h)**
  - 연결 입력 묶음 생성은 `CreateSceneInput(display, sources)` 하나입니다. 서비스 수명 재료는 새 묶음 `ViewSystem/Combat/Input/CombatSceneInputSources.cs`(필수 열다섯)입니다. 생성 계약과 `CombatSceneFactory`의 위임도 같은 서명입니다.
  - 배치 진입 `CombatDeploymentEntry`를 환경 호버 뒤에 만듭니다(확인창 캐스트도 여기).
  - `CreateSceneInteraction`·`CreateInteraction`은 오디오 대신 입력 소리 창구 `CombatInputSounds`(오디오 + 소리 프리셋)를 같은 자리로 받습니다.
- **고친 민형님 시험(허락, Q-T7 c):** `CombatPhaseTransitionPresenterTests.cs:33·276`: `CreateDefault(…)` → `From(실제 DB 설정, …)`. 실제 DB 설정 읽기 도우미 `Presentation`을 더했습니다. 확인 내용(레이어 번호·카메라 이름·색·속도·양, 레이어 없음 예외)은 그대로입니다.
- **제안(고치지 않음):** `ViewSystem/Combat/Display/Field/FieldOutlineRenderer.cs:428`(민형님 줄)은 이동 경로 테두리 색을 `_palette.MovementDraft`에서 바로 씁니다. 3라운드 W10에서 강조 층의 색·순서는 층 표 `FieldHighlightLayers.Rows` 하나가 원본이 됐는데, 이 줄만 표를 거치지 않습니다. 층 표에서 읽게 바꿀지 정해 주세요.
- **알려 드리는 것:** 보스 스테이지(2101) 배경 `StageViewProp_Boss`의 맵 정의(`Assets/Map_Map.asset` → `Assets/Map.prefab`)에는 버추얼 카메라가 없어서, 위 카메라 이름 검사로 진입이 실패합니다. 사용자 결정으로 지금 세팅은 임시라 그대로 둡니다. 맵 도구로 만든 배경에도 `Day Camera`·`Night Camera`가 있어야 합니다.
- 서비스·키·그룹 노출과 Framework·GlobalSystem 코드는 바꾸지 않았습니다.

## 26. 2026-10-26 후속 — 4라운드 가용성 생성과 기존 표시 연결 (미전달)

- 구현/검증 원본: [4라운드 계획 §6](../plans/2026-10-25-combat-view-stage-round4-plan.md#6-문서-변경실행-기록). 사용자 지시로 V4-d/e/f를 구현했다. W14(d/e) 검토는 완료했고 W15(f/g) 검토는 g 끝이다. **변경 후 컴파일/Unity 확인은 미확인**이다.
- `CombatSceneInputFactory.CreateSceneInput`에서 Core Buffer 싱크로 Tracker 하나를 만들어 SceneInput이 소유/정지한다. 같은 Tracker를 공격/경로가 빌린다. 운영 `CreateSceneInteraction`에서 이 의존성은 필수이며 기존 범위/조준 격리 조립의 생략 계약만 유지한다.
- V4-f에서는 같은 `display.ShowActionAvailability` 메서드 창구만 SceneInput에 주입한다. SceneInput은 현재 키의 응답/해제를 표시 값이 달라질 때만 보내며 새 결과/닫힘/Stop에서 오래된 이유도 지운다. `ICombatBattleDisplay`의 표시 메서드 추가와 그 구현/FakeDisplay 보정 외에 Factory/Group의 등록 정책·공용 생성 계약은 변경하지 않았다.
- 새 서비스/ServiceKey·Group Export·Framework 변경은 없다. Global UI는 사용자 Q-V30 허락으로 NightCheckerUI·UnitCardTraitContent의 전투 표시 접점만 수정했으며 기존 UI 등록/개폐 경로를 유지한다. Scene/Prefab/DB 에셋·다른 작업자의 필드 카메라/Architecture는 변경하지 않았다. 외부 발송·커밋/push는 사용자가 한다.

## 27. 2026-10-26 V4-m — 4라운드 조립/계약 인계 정리 (미전달)

- 기준 `f616d5d9` 뒤 a/d/e/f/g/h 코드와 W13~W16 정적 검토가 끝났다. §26의 생성/표시 연결이 현재 코드에 있으며 **변경 후 컴파일·Unity/EditMode는 미확인**이다. V4-m은 문서만 정리했다.
- 변경 파일: `Factory/Runtime/InGame/View/CombatSceneInputFactory.cs`, `ViewSystem/Combat/Input/CombatSceneInput.cs`, `CombatViewSystem.cs`, `Display/ICombatBattleDisplay.cs`·`CombatSceneDisplay.cs`. 새 `Input/Interaction/CombatActionAvailabilityTracker.cs`와 `.meta`도 함께 인수해야 한다. 표시 변환 `Display/Hud/CombatActionStateText.cs`와 `.meta`는 UI 내부 협력자다.
- 생성→소유→소비: Factory가 기존 Buffer 싱크로 Tracker 하나 생성 → SceneInput 소유/Stop → 공격/경로가 같은 인스턴스를 빌림 → 기존 System 연결 접점에서 키/응답 동기화. 운영 CreateSceneInteraction의 가용성 의존성은 필수다. 기존 격리 시험 조립의 생략 계약을 전수 정리하지 않았다.
- 표시: 같은 Tracker의 현재 응답/해제를 `display.ShowActionAvailability` 메서드 창구로 전달한다. 새 UI 서비스·ServiceKey·Group Export·루트 재등록은 없다. SceneInput은 입력 객체이며 Framework 모듈로 따로 등록하지 않는다. 기존 UIBase/UISystem 개폐·UIInputBinding/수신 슬롯·Pointer/Submit 계약은 유지한다.
- 관련 기존 시험: 공통 FakeDisplay는 새 표시 메서드의 빈 구현만 맞췄다. 호진님 공격 Drag 시험은 현재 키 Ready를 실제 Core 조회로 준비/대기한다. 이번 라운드에서 민형님 기존 시험을 새로 수정하지 않았고 새 사례도 없다. 전경/배경 슬롯 순서 변경의 통합 확인은 계획 M04~M08·M13이다.
- 씬/Prefab/DB·Framework·ServiceKeys·Group 등록 정책·FieldCamera/Architecture 변경은 없다. 이전 §25의 표면/카메라 제안도 이번에 구현하지 않았다. 확인할 항목과 기능 미확인/구조 이월은 [4라운드 계획 §7](../plans/2026-10-25-combat-view-stage-round4-plan.md)에 모았다. 외부 발송과 커밋/push는 사용자가 한다.

## 28. 2026-10-26 V5-a — 선택 생성/소유와 입력 관측 연결 (미전달)

- CombatSceneDisplayFactory가 CombatSelectionState 하나를 만들고 Display가 소유한다. UnitInfo에는 읽기 계약을 주입한다. 기존 MonoBehaviour/씬 참조와 UI 등록·부모/Policy는 유지한다. 새 서비스/키/Group Export/Builder 변경은 없다.
- Display는 완료 표시 CharacterData 참조 목록을 한 번 고정하여 Selection/UnitInfo가 공유하고, Clear/Release/새 설치에서 정리한다. Buffer 목록 래퍼를 그대로 보관하지 않는다. Core/Buffer는 수정하지 않았다.
- CombatSceneInputFactory의 Path 선택은 Display 형식 탐색/Empty 대체 대신 기존 Facts의 선택 값을 읽는다. Facts의 빈 범위 관측에는 선택 ID를 값으로 넘겨 역호출 순환을 끊었다. 실제 운영/시험 전체 조립의 분리와 typed 확인창은 V5-c 후속이다.
- 두 Attach 입구가 명시적 Inspection/Reader를 첫 표시 전에 연결한다. 인수 전 AttachCreated 반환/AttachDisplay 호출자 보유와 중복/실패/재진입 수명은 보존한다. 실제/가짜 모두 같은 계약이며 Fake의 선택 1은 명시적으로 준비한다.
- W17 새 읽기 전용 검토에서 보고할 P1/P2 결함 없음. 변경 후 컴파일·기존 시험·Unity는 미확인이다. 이전 4라운드는 V5-0에서 사용자 전체 정상 보고를 받았으나 이 변경의 검증이 아니다. 새 시험/컴파일 실행·외부 발송·커밋/push는 하지 않았다. 상세 원본은 5라운드 계획 §8이다.

## 29. 2026-10-26 V5-b — 입력 사실 Reader 생성 연결 (미전달)

- 기존 ICombatSceneInputFactory에 CreateFactsReader를 추가하고 CombatSceneFactory가 정적 CombatSceneInputFactory로 위임한다. 서비스 바인딩마다 InputConnection 생성 뒤 새 Reader를 만들며 새 서비스/키/Builder/Group Export는 없다.
- Reader는 기존 state/input ReadSink·InputConnection/Playback/Progress/BlockedInputReport와 현재 연결/입력 준비/명령 대기/모달의 이름 있는 공급자만 받는다. 조정자나 Resolver 전체를 넘기지 않는다. 현재 씬 Selection/Display는 매번 Connection에서 읽고 서비스 수명 객체에 캡처하지 않는다.
- 재바인딩 시작 때 Reader/기존 입력 재료를 함께 무효화한다. InitData의 View 표 교체는 입력 재료만 무효화한다. Reader/입력 재료의 수명과 결과 확인 순서를 바꾸지 않았고 null 조립/바인딩 전 잘못된 읽기를 명시적으로 진단한다.
- View Facts의 SelectedActorId 명칭에 Path 선택 공급자를 맞췄다. typed 확인창/부분 시험 조립·Path optional 인자 제거는 V5-c 후속이다. 운영 입력 Factory의 조립 정책/모듈 등록과 Core/Framework/GlobalSystem/씬/Prefab/DB는 변경하지 않았다.
- a 변경 후 사용자 컴파일·기존 시험·Unity 정상 보고를 받았다. b는 W18 새 읽기 전용 검토에서 보고할 P1/P2 결함이 없었고 변경 후 컴파일/기존 시험/Unity 미확인이다. 상세 원본은 5라운드 계획 §8이며 외부 발송·커밋/push는 하지 않았다.

## 30. 2026-10-26 V5-c — 운영 필수 조립/시험 경계 (미전달)

- 확인창 capability는 BindServices에서 확인해 typed 필드/Sources.Confirmation으로 전달한다. 깊은 Text as 캐스팅은 제거했다. 기존 서비스의 기능이며 별도 서비스/등록/키/Builder/GlobalSystem 변경은 없다.
- CreateSceneInteraction은 필수 재료를 상태 변경 전에 검사하고 Placement/Attack/Path/Gesture/Item/Hover/Availability를 모두 명시 연결한다. Runtime의 부분 CreateInteraction은 제거했고 승인된 시험 helper로 옮겼다. 운영은 시험 어셈블리에 의존하지 않는다. 생성 실패/Detach의 기존 소유권·원인 예외를 우회하지 않는다.
- Path는 Facts의 SelectedActorId만 읽고 선택 별도 optional 공급자/Empty 대체를 제거했다. Facts의 빈 범위 진단은 선택 ID를 값으로 넘기는 비순환 경로 그대로다. 의도적 leaf 선택 기능/정상 대기/반복 해제는 일괄 필수화하지 않았다.
- b의 using 수정 후 사용자 전체 정상 보고를 받았다. c는 W19 새 읽기 전용 검토에서 보고할 P1/P2 결함이 없었으며 변경 후 컴파일/기존 시험/Unity 미확인이다. 기존 시험은 승인된 조립/호출·공통 확인창 Fake만 맞췄다. Core/Buffer/Framework/공용 UI·씬/Prefab/DB 변경·외부 발송/커밋/push는 없다. 원본은 5라운드 계획 §8이다.

## 31. 2026-10-26 V5-d — 5라운드 최종 조립 인계 (미전달)

- 현재 코드: DisplayFactory가 SelectionState를 생성하고 Display가 소유, UnitInfo에는 Reader만 주입한다. 완료 표시마다 Display가 한 번 고정한 CharacterData 참조 목록을 선택/UI가 공유한다. 입력은 명시적 Inspection/Reader로 연결하며 UI 형식 발견/선택 Empty 대체에 의존하지 않는다.
- FactsReader 생성은 기존 ICombatSceneInputFactory→CombatSceneFactory→CombatSceneInputFactory가 맡는다. BindServices가 현재 InputConnection 생성 뒤 Reader를 받고, Reader는 현재 연결을 매번 읽는다. 재바인딩 때 Reader/Sources 무효화, InitData 때 입력 재료 무효화. 특정 Display/Selection을 서비스 수명에 캡처하지 않는다.
- 배치 확인창은 기존 서비스가 제공하는 IUIConfirmationPresentation을 BindServices에서 검증해 Sources.Confirmation에 넘긴다. 전체 운영 CreateSceneInteraction은 필수 재료 검사 뒤 전체 기능을 연결한다. 부분 CreateInteraction은 시험 어셈블리 CombatInputTestFactory로 이동했고 운영은 이를 호출하지 않는다. 새 서비스/모듈·키/Builder·Group Export·루트 재등록·공용 Framework/GlobalSystem 계약 변경 없음.
- 소유권: 두 Attach 모두 TakeDisplay→BeginUse→첫 표시 순서다. AttachCreated는 인수 전 주인 없는 표시를 ReleaseUnowned, AttachDisplay는 인수하지 않은 표시를 호출자가 보유한다. 인수 뒤 실패는 기존 Detach로 정리한다. 중복 Attach/첫 그리기 중 재진입의 기존 보호, StopInput 뒤 최종 표시와 Display Release 책임을 보존했다.
- W17~19 정적 검토 및 a/b/c 각각의 사용자 “컴파일·기존 시험·Unity 확인 모두 정상” 보고를 인수했다. b의 누락 using은 사용자 컴파일 오류 보고 후 View Reader에서 수정하고 재확인받았다. d는 문서만 정리했다. 실제 실행과 정적 근거·재바인딩 등 개별 로그 공백은 [5라운드 최종 인수 §10](../plans/2026-10-26-combat-view-stage-round5-plan.md#10-v5-d-최종-인수와-잔여-2026-10-26)에 둔다. 여기의 정리는 외부 발송이 아니다.

## 32. 2026-10-27 전투 카메라 조작(이동·줌) — Factory 줄·View DB 칸 (미전달)

설계 [카메라 조작](../specs/2026-10-26-combat-camera-control-design.md)(CC1~CC7), 계획·실행 기록 [카메라 조작 계획](../plans/2026-10-27-combat-camera-control-plan.md) §6. 컴파일 미확인(상시 지시).

- **View DB 칸(민형님 파일, 사용자 결정 Q-CC2 A·Q-CC8 A):**
  - `StageViewPropData`에 SO 참조 칸 `cameraSettings`(새 `CombatCameraSettingsSO`)와 읽기 `CameraSettings`(비면 null, 있으면 매번 검사한 새 값)를 블록 끝에 더했습니다.
  - `ViewStageData` 생성자가 이 값이 비면 예외를 냅니다(부트 DB 생성에서 실패). `StageViewProp_*.asset` 12곳에 기본 에셋 `DataBaseSO/ViewGroup/CombatCamera/CombatCameraSettings_Default.asset`을 연결했습니다.
  - 민형님 `MapSoDatabaseAssemblyTests`는 표가 SO를 바로 읽어 `ViewStageData`를 거치지 않으므로 영향이 없습니다.
- **Factory 연결 줄:**
  - `CombatSceneDisplayFactory.Create`: 창구 `AttachMap` 바로 뒤에 카메라 시점 `CombatCameraView`를 만들고(배경이 없으면 맵 카메라 없는 시점) `Begin()`, 표시 생성자에 새 필수 인자 `cameraView`(창구 바로 뒤).
  - `CombatSceneInputFactory.CreateSceneInput`: 카메라 입력 `CombatCameraInput(display.CameraControl, display, 화면 크기)`를 만들어 연결 입력 묶음 생성자 끝 인자로.
- **카메라 창구 동작 변화:** 연출 줌(포효·기차) 배율은 이제 렌즈 기준값이 아니라 플레이어가 정한 보기 크기에 곱하고, 되돌림도 보기 크기로 갑니다. 보기 크기를 안 바꾸면 보내는 값은 지금과 같습니다. 맵 카메라 줌은 여전히 창구 한 곳에서만 보냅니다(`Dispatch(CameraZoomData)`, Framework 변경 없음).
- **맵 카메라 위치:** 시점이 Day·Night 버추얼 카메라 Transform을 같은 월드 오프셋으로 옮깁니다(Follow·LookAt이 빈 카메라라 Transform이 곧 위치). 표시 해제 때 처음 위치·크기로 되돌립니다. 보스 맵 `Map.prefab`에는 카메라가 없어 조작이 없습니다(§25의 보스 배경 카메라와 같은 사실).
- **입력:** 액션 에셋에 새 맵 `CombatCamera`(Pan·Zoom)를 더하고 `StageInputLayers` 배치·전투 둘 다 켭니다. 새 Controller `CombatCameraInputController`를 `StageInputFactory`에 등록했습니다. 서비스·키·Builder·Group Export·GlobalSystem 변경은 없습니다.

## 33. 2026-10-27 전투 넉백 미리보기 — 입력 Factory 연결 줄 (미전달)

설계 [넉백 미리보기 View](../specs/2026-10-27-combat-knockback-preview-view-design.md)(KB1~KB7), 계획·실행 기록 [넉백 미리보기 계획](../plans/2026-10-27-combat-knockback-preview-view-plan.md) §7. 공격 호버 때 Core `AttackPreview.Knockbacks`(천명님)를 화살표와 도착 칸으로 그립니다. Unity 확인은 사용자가 했습니다(문제 없음).

- **Factory 연결 줄:** `CombatSceneInputFactory.CreateSceneInteraction`의 `new AttackTargetingInteraction(...)`에 새 필수 인자 `knockback`을 `gate` 바로 뒤에 넣었습니다(값은 `display`). 표시 `CombatSceneDisplay`가 새 좁은 계약 `ICombatKnockbackPreviewFeedback`을 구현합니다.
- `CombatSceneDisplayFactory`는 바뀌지 않았습니다. 넉백 구성 부품 `CombatKnockbackPreview`는 `CombatAttackFeedback`이 필드만 받아 생성자 안에서 만듭니다.
- **Render 하위 부품 사용:** 화살표 색은 ColorSystem 핸들로 덮어씁니다(`CreateColorHandle` → `SetOverrideData(재질 칸, MainColor)` → `Dispose`). 화살표 인스턴스의 Renderer마다 핸들 하나이고, 필드를 해제할 때 거둡니다. 재질에 `_BaseColor`가 없으면 필드 구성 때 예외입니다. ColorSystem·서비스·키·Builder·GlobalSystem 변경은 없습니다.
- **강조 층:** 열거에 `KnockbackDestination = 1 << 13` 한 줄과 층 표 한 줄을 더했습니다. GridOutline 아웃라인 손잡이는 층마다 하나라 하나 늘었습니다(15 → 16). 뒤에 사용자 요청으로 이 층은 전용 색(`FieldView.knockbackDestinationColor`)과 칸 채우기를 쓰게 됐습니다(계획 §7.5).

## 34. 2026-10-27 AreaOutline 정렬 순서 — 데이터 칸 하나 (미전달, 사용자 허락으로 민형님 파일 수정)

설계·계획은 §33과 같고, 기록은 [넉백 미리보기 계획](../plans/2026-10-27-combat-knockback-preview-view-plan.md) §7.5입니다. 컴파일 미확인입니다.

- **왜:** 전투 GridOutline에서 칸 테두리끼리 겹칠 때(예: 넉백 도착 칸 테두리와 공격 범위 테두리) 어느 쪽이 위에 보일지 정해지지 않았습니다. AreaOutline 셰이더는 Transparent·`ZWrite Off`라 `YOffset` 차이로는 순서가 정해지지 않습니다. 렌더러 정렬 순서도 모두 0이라 Unity가 카메라 거리로 순서를 정합니다. 사용자가 AreaOutline 두 파일 수정을 허락했습니다(2026-10-27).
- **바꾼 것:**
  - `AreaOutlineData`: 속성 `SortingOrder`와 생성자 끝 선택 인자 `int sortingOrder = 0`을 더했습니다.
  - `AreaOutlineHandle.Update`: `MeshRenderer.sortingOrder = data.SortingOrder`를 넣습니다. `Reset`(풀 반환)에서는 0으로 되돌려 다음에 빌리는 쪽에 값이 남지 않게 합니다.
- **바뀌지 않은 것:** `IAreaOutlineHandle`, `AreaOutlineSystem`, 풀, 서비스 키, 셰이더는 그대로입니다. 기본값이 0이라 `TestRuner.cs`·`MeshSystemHandleTests` 같은 기존 호출은 그대로 컴파일되고 동작도 같습니다.
- **전투 쪽 사용:** `FieldOutlineRenderer`가 테두리 칸마다 층 우선순위 순서(레일 → 아군 배치 구역 → 층 표 순서 → 호버)로 0, 1, 2…를 넘깁니다. 정렬 순서의 원본은 새 표 `Display/Field/FieldSortingOrders`입니다. 칸 채우기는 −1(그대로), 공격 궤적 점선은 100(옛 1)입니다.
- **제안(이번에 안 바꿈):** 이동 경로선을 그리는 `DrawPointLine`도 같은 성질(투명, 정렬 순서 0)입니다. 경로선은 칸 중심을 잇는 선이라 테두리와는 교차점 정도만 겹칩니다. 필요하면 `DrawPointLineData`에도 같은 칸을 더할 수 있습니다.

## 35. 2026-10-27 약한 지반 붕괴 — 필드 창구 칸 하나 (미전달)

설계 [약한 지반 붕괴](../specs/2026-10-27-combat-weak-ground-collapse-after-move-design.md)(WG1~WG7), 계획·실행 기록 [약한 지반 붕괴 계획](../plans/2026-10-27-combat-weak-ground-collapse-plan.md) §7. 약한 지반이 무너지는 순간 그 칸 외형을 낭떠러지로 바꿉니다. 컴파일 미확인입니다.

- **Factory 연결 줄:** `CombatSceneDisplayFactory`의 `new CombatFieldEventPorts(...)`에 새 필수 이름 인자 `showTerrainChange: terrain.ShowChanged`를 `showTornado` 뒤에 넣었습니다(창구 여덟 → 아홉). 지형 표시 `FieldTerrainPresenter`에 새 입구 `ShowChanged(MovementTileFact, FieldGridMapper)`가 생겼습니다.
- Framework·GlobalSystem·서비스 키·Builder 변경은 없습니다. 지형 외형 풀(`CustomPool`) 사용 방식도 그대로입니다(종류가 바뀌면 옛 외형을 돌려주고 새 종류 외형을 빌림, 결과 끝 갱신과 같은 길).

## 36. 2026-10-28 전투 이펙트 창구 확장 — 사건 Presenter 인자 하나·환경 폭발 줄 삭제 (미전달)

설계 [전투 이펙트 창구 확장](../specs/2026-10-28-combat-effect-cue-expansion-design.md)(EC1~EC10), 계획·실행 기록 [이펙트 창구 확장 계획](../plans/2026-10-28-combat-effect-cue-expansion-plan.md) §6. 사실 사건(오브젝트 발동·장애물 피해/파괴·추락·약한 지반·아이템·토네이도)에 이펙트·흔들림을 데이터로 붙이고, 화약통·지뢰 폭발을 그 큐로 옮겼습니다. Unity 마지막 컴파일은 오류 없이 끝났습니다(Editor.log). 시험 실행은 하지 않았습니다.

- **Factory 연결 줄(`CombatSceneDisplayFactory`):**
  - 사건 Presenter 생성 줄에 새 필수 인자 `tables.CueSources`를 소리 프리셋 `tables.SoundPresets` 바로 뒤에 넣었습니다. 값(사건 순간 → 큐 값)은 표시 표 `CombatDisplayTables`가 View DB 외형 표·설정 표로 만듭니다.
  - 환경 폭발 Presenter(`CombatEnvironmentEffectPresenter`) 생성 두 줄, 필드 창구 이름 인자 `presentEnvironmentEffect:`, 재생 조정(`CombatSceneEventPlayback`)·씬 표시(`CombatSceneDisplay`) 생성의 그 인자를 지웠습니다.
  - 그 클래스와 폭발 판정 `CombatExplosionRules`(둘 다 우리 파일)는 삭제했습니다.
- **민형님 커밋에서 마지막으로 바뀐 줄 하나:** `CombatSceneDisplay`의 필드 연결 정리 목록(`e18c8313`)에서 `_environmentEffects.Clear`를 뺐습니다. 이펙트 회수는 이펙트 창구(`CombatEffectWindow`)가 사건 표시 취소·해제 때 이미 합니다.
- Framework·GlobalSystem·서비스 키·Builder 변경은 없습니다. `EffectScope` 사용 방식도 같습니다(이펙트 창구 하나가 Scope 하나). 사용자가 더한 `EffectScope.Stop(EffectType)`은 쓰지 않습니다.
- **부탁:** 따로 하실 일은 없습니다. 브랜치에서 표시 Factory의 Presenter 생성 줄을 고치셨다면 병합 때 `tables.CueSources` 인자를 남겨 주세요.

## 37. 2026-10-28 전투 맵 바닥 타일 — 배경 생성·표시 생성 줄 끝 인자 하나씩 (미전달)

구멍 난 배경 메시에 칸 단위 바닥 타일을 채우고(배경 방식 `PrefabWithTiles`), 타일 종류마다 다른 윗면 높이를 화면에 반영합니다. 설계는 [타일 맞춤](../specs/2026-10-28-combat-map-tile-alignment-design.md)(TA1~TA5)과 [타일 높이](../specs/2026-10-28-combat-map-tile-height-design.md)(TH1~TH6)이고, 실행 기록은 [맞춤 계획](../plans/2026-10-28-combat-map-tile-alignment-plan.md) §6·[높이 계획](../plans/2026-10-28-combat-map-tile-height-plan.md) §8입니다. 컴파일 미확인입니다.

- **Factory 연결 줄 둘(`CombatSceneDisplayFactory`):**
  - `CreateBackdrop`: `new StageViewPropPresenter(tables.StageProps, root.Field.CellsRoot, tables.Field)` — 셋째 인자로 필드 설정을 넘깁니다. 배경이 바닥 타일을 칸 경계에 맞출 칸 간격을 이것으로 읽습니다(GridOutline 모드만 지원).
  - `Create`: `new CombatSceneDisplay(...)` 끝 인자 `backdrop?.TileSurface` — 배경이 맞춤 뒤 만든 필드 칸별 타일 높이 표입니다. 타일 방식이 아니거나 배경이 없으면(표시 시험) null이고, 표시가 바닥 높이를 0으로 둡니다.
- **배경 표시 동작:** 프랍의 배경 방식 열거 `StageViewPropMode`(`Prefab = 0` 기본 / `MapDefinition = 1` / `PrefabWithTiles = 2`)가 세우는 방식을 고릅니다. 민형님이 넣은 맵 SO 줄(`MapAssembler.Instantiate`)은 그대로이고, 맵 SO는 이제 `MapDefinition` 방식에서만 읽힙니다(방식별 칸 검사는 `StageViewPropData.Validate()` 한 곳). 운영 스테이지 A·B·C는 `Prefab`, 보스는 `MapDefinition`이라 화면이 그대로이고, 타일 방식은 Test 스테이지 하나입니다.
- **타일 방식에서 하는 일(씬 로드 −195, 주인은 배경):** 배경 인스턴스 아래 `MapTileRoot`를 만들어 타일을 놓고, 배경만 90° 단위 남은 각만큼 돌리고 옮겨 기준 타일이 게임 (0,0) 칸에 오게 합니다(필드·Core 무변경). 그 뒤 칸마다 타일 높이를 읽어 높이 표를 한 번 만듭니다. 표시는 `Install` 때 이 표를 필드 높이 표의 바탕 층에 넣고, 유닛·강조·지형 외형 등이 그 위에 놓입니다.
- Framework·GlobalSystem·서비스 키·Builder·Group Export 변경은 없습니다. 맵 카메라 사실(§32)·화면 전환 카메라 검사(§25)는 맞춤 뒤에 읽으므로 돌린 배경의 카메라를 그대로 씁니다.

## 38. 2026-10-29 전투 이동 경로 화살표 — 선 서비스 연결 줄 삭제 (미전달)

설계 [이동 경로 화살표](../specs/2026-10-29-combat-movement-path-arrow-design.md)(MA1~MA7), 계획·실행 기록 [이동 경로 화살표 계획](../plans/2026-10-29-combat-movement-path-arrow-plan.md) §5. 민형님이 `b7c79107`("이동경로 라인 표시")에서 `DrawPointLineSystem` 선 핸들로 그리던 GridOutline 이동 경로를, 이제 **띠 + 꺾인 칸의 직각 이음 + 끝 화살촉 하나**로 그립니다. 그리기는 새 부품 `Display/Field/FieldMovementPathLine`(모양 규칙은 `FieldMovementPathShape`)이 MeshSystem·ColorSystem을 조합해 합니다(궤적 `FieldTrajectoryLine`과 같은 꼴). 컴파일 미확인입니다.

- **Factory 연결 줄 셋(`CombatSceneDisplayFactory`, 줄 번호는 바꾸기 전 HEAD 기준):**
  - `:9` using `…RenderSystem.View.DrawPointLineSystem` 삭제.
  - `:77` 지역 변수 `IDrawPointLineSystemService pointLines = services.PointLines;` 삭제.
  - `:178` `new CombatSceneDisplay(...)`의 `pointLines` 인자 삭제(`areaOutlines, meshes` 사이).
  - 같은 파일에 다른 세션(지형 타일 교체 TR-c2)의 연결 줄 변경(지형 표시 생성 인자, 필드 창구 외형 칸 넷)이 함께 있습니다. 그 내용은 그쪽 절에 적힙니다.
- **민형님 `b7c79107` 줄이 바뀐 곳:**
  - `FieldOutlineRenderer`: using·머리 주석·필드 `_pointLines`·생성자 null 검사·`Build`의 경로 선 핸들 만들기·`Apply`의 선 갈래·`Slot.LineHandle`을 지웠습니다. 이동 초안 슬롯은 테두리·선 핸들을 만들지 않고 부품 `_path`의 `Build`·`Show`·`Clear`·`Release`를 부릅니다. `Collect`의 이동 초안 갈래(칸을 모으는 줄)는 그대로입니다.
  - `CombatSceneDisplay` `:11` using·`:73` 필드(+ 생성자 인자·`_field.Build` 인자)를 지웠습니다.
  - `FieldView` `:7` using·`:126-127` 인자·null 검사를 지웠습니다.
  - 우리 파일 `CombatDisplayServices`의 속성 `PointLines`와 `From`의 `render.ViewGroup.DrawPointLineSystemServices` 줄도 지웠습니다.
- **그대로인 것:** GlobalSystem `DrawPointLineSystem`, `RenderViewGroup.DrawPointLineSystemServices`, 서비스 키는 그대로입니다(다른 쓰임을 위해 남김). 전투 View는 이제 이것을 쓰지 않습니다. Render DB `LineColor` 머티리얼 항목도 그대로입니다.
- **Render 하위 부품 사용:**
  - MeshSystem: 띠는 높이 구간마다 오브젝트 하나(`FieldMovementPath_{번호}`)에 `Register(MeshFilter)` → `Update(MultiLineCreateData)`로 칸이 바뀔 때만 다시 채웁니다. 화살촉(`FieldMovementPathArrow`)은 `CircleCreateData`(꼭짓점 3, 반지름 1)를 구성 때 한 번 넣고, 그 뒤로는 Transform 자세·배율만 바꿉니다.
  - ColorSystem: 오브젝트마다 `CreateColorHandle(renderer)` → `SetOverrideData(0, MainColor, 색)`, 색이 바뀔 때만 넣습니다.
  - 머티리얼은 View DB `StageFieldViewData.movementPathMaterial`(기본은 경로 전용 `Prefabs/Combat/Materials/MovementPath.mat` — 선 재질 `LineShader.mat`을 복제한 것이라 Render DB `LineColor`와 따로 고칠 수 있음)이고, `_BaseColor`가 없으면 필드 구성 때 예외입니다(`DrawPointLineSystem.InitData`와 같은 조건).
  - 해제는 오브젝트마다 Color 핸들 → Mesh 핸들 → GameObject 순서이고 필드를 해제할 때 거둡니다. MeshSystem·ColorSystem·서비스 키·Builder 변경은 없습니다.
- **§34 제안 정리:** §34 끝의 "`DrawPointLineData`에 정렬 순서 칸" 제안은 전투 경로에는 더 해당하지 않습니다. 새 경로는 불투명 재질이고 정렬 순서는 기본값입니다.
- **부탁:** 따로 하실 일은 없습니다. 브랜치에서 위 줄을 고치셨다면 병합 때 선 서비스 연결이 빠진 쪽을 남겨 주세요.

## 39. 2026-10-29 지형 외형 타일 교체·약한 지반 붕괴 연출 — 지형 표시 생성 인자 하나·필드 창구 이름 있는 인자 넷 (미전달)

[타일 교체 설계](../specs/2026-10-29-combat-terrain-tile-replace-design.md) TR4·TR8, [계획](../plans/2026-10-29-combat-terrain-tile-replace-plan.md) §6.3·§6.4. 바닥 교체 지형(약한 지반·낭떠러지)이 그 칸의 맵 타일을 끄고 교체 외형을 그 자리에 놓습니다. 약한 지반이 무너지면 외형이 떨리다 가라앉습니다.

- **바꾼 줄(`Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs`):**
  - using `ProjectL.Systems.RuntimeSystem.InGameSystem.ViewSystem.Combat.Presentation.Environment` 한 줄.
  - 지형 표시 생성 `new FieldTerrainPresenter(field.CellsRoot, tables.Terrains, tables.Environments, CombatTileCollapseSettings.From(presentation))` — 넷째 인자가 새로 생겼습니다. 값은 View DB 설정 표의 새 묶음 "약한 지반 붕괴 연출"(떨림 시간·세기·빠르기, 가라앉기 시간·깊이)입니다.
  - 필드 창구 `new CombatFieldEventPorts(…)`의 이름 있는 인자: `tickTornado`·`tornadoPlaying`·`cancelTornado` → `tickAppearance`·`appearancePlaying`·`cancelAppearance`(넘기는 메서드는 그대로 `terrain.TickAppearance`·`IsAppearancePlaying`·`CancelAppearance`), 새 인자 `appearanceBlocksStart: terrain.IsAppearanceBlockingStart`, 주석 한 줄.
- **왜:** 토네이도 외형 연출을 돌리던 재생기를 필드 외형 재생기로 넓혀 약한 지반 무너짐도 같이 맡깁니다. 무너짐은 떨리는 동안만 다음 사건(그 칸 유닛 추락)을 막고, 가라앉기까지 끝나야 완료입니다. 그래서 "다음 사건을 막는가"와 "진행 중인가"를 따로 넘깁니다. 토네이도는 전처럼 끝까지 막습니다.
- **그대로인 것:** 표시 생성 줄 끝 `backdrop?.TileSurface`(§37)는 그대로입니다. 같은 표가 이제 칸별 타일 오브젝트도 가지고, 표시가 설치 때 지형 표시에 넘깁니다(`_terrain.BindTiles`, 우리 파일). ServiceKeys·Builder·GlobalSystem 변경은 없습니다.
- **부탁:** 따로 하실 일은 없습니다. 브랜치에서 이 줄을 고치셨다면 병합 때 새 이름 있는 인자 넷과 지형 표시 넷째 인자를 남겨 주세요.

## 40. 2026-10-29 전투 특성 발동 연출 — 걸린 상태 표 연결 줄 넷 (미전달)

[특성 발동 연출 설계](../specs/2026-10-29-combat-trait-activation-presentation-design.md) TA9, [계획](../plans/2026-10-29-combat-trait-activation-presentation-plan.md) §6.5. 유닛에 걸린 상태(버프·디버프)의 HUD 칸을 새 View DB 표 **상태 외형 표** `UnitStatusVisual`(종류마다 아이콘·이름·설명 틀·구분)과 전투 View 상태 줄 표 `Display/Unit/CombatUnitStatuses`로 그립니다. 새 표라 연결 줄 넷을 바꿨습니다. 컴파일 미확인입니다.

- **바꾼 줄(모두 연결만, 기능·계약 그대로):**
  - `ServiceKeys/DBFactory/Group/ViewTableKey.cs`: 키 `UnitStatusVisual`(`SystemGroupKey<IUnitStatusVisualTableQuery>`) 하나, `ConsumableItemVisual` 다음.
  - `DataBaseSystem/Queries/ViewDataBase/ViewDataBase.cs`: 계약 `IViewQuery`와 구현에 속성 `UnitStatusVisualTableQuery` 하나씩, 생성자에 `GetService(ViewTableKey.UnitStatusVisual)` 한 줄.
  - `Factory/DBFactory/Group/ViewDataBaseFactory.cs`: using `…Data.View.Table.UnitStatus` 한 줄, 생성 줄 `new UnitStatusVisualTable(dataBaseGroup.GetDataBase<UnitStatusVisualDataTableSO>()).ExportServices(builder);` 하나.
  - `Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs:176`: `units.Configure(tables.Units, fieldConfig.FieldUnitScale, tables.UnitStatuses, tables.Traits, unitHudLayer, tables.Items)` — 상태 줄 표 인자 하나가 셋째 필수 인자로 들어감(상태 줄 표는 표시 표 `CombatDisplayTables`가 만듦, 빠뜨리면 걸린 상태 칸이 조용히 사라지므로 필수 — 묶음 끝 검토 L8). 뒤의 특성·HUD 층·아이템 인자는 자리만 한 칸 밀림.
- **에셋:** 새 폴더 `DataBaseSO/ViewGroup/UnitStatusVisual/`(표 + 상태 셋), `ViewGroup.asset`의 표 목록 끝에 한 줄. 코드와 한 커밋이어야 합니다(표 에셋이 없으면 `GetDataBase`가 Boot DB 생성에서 예외).
- **그대로인 것:** Builder·GlobalSystem·LifeCycle 변경은 없습니다. `IViewQuery`를 구현하는 시험 가짜는 지금 저장소에 없습니다(속성을 내는 쪽만 있음).
- **같은 작업의 다른 변경(Factory 무변경):** 특성 처리기·공격 시작 한 곳(`CombatAttackStart`)은 사건 Presenter 생성자 안에서 조립해 Factory를 바꾸지 않았습니다.
- **부탁:** 따로 하실 일은 없습니다. 브랜치에서 위 파일을 고치셨다면 병합 때 키·속성·생성 줄·인자를 남겨 주세요.

## 41. 2026-10-29 겹친 칸 강조 — 테두리 메시 선택 값 둘 (미전달, 사용자 허락으로 민형님 파일 수정)

설계 [겹친 강조 2단계](../specs/2026-10-29-combat-overlap-rounded-inset-design.md) §8(3판)·§9(4판), 계획·실행 기록 [2단계 계획](../plans/2026-10-29-combat-overlap-rounded-inset-plan.md) §7.3·§7.4. 컴파일 미확인입니다.

- **왜:** 전투 GridOutline에서 칸 테두리 모서리를 둥글게 깎고, 강조가 겹친 칸은 테두리를 안쪽으로 줄인 판 둘레에 맞춰야 합니다(사용자 요청). 테두리 메시가 직각 덩어리 경계만 만들어 View 쪽에서는 할 수 없었습니다. 사용자가 테두리 메시에 선택 값을 더하는 것을 허락했습니다(2026-10-29, 설계 Q-RC8).
- **바꾼 것(파일 셋):**
  - `GridOutlineCreateData`(`MeshGeometryData.cs`): 필드 `CornerRadius`(float)·`LoopInset`(Vector2), 생성자 끝 선택 인자 `float cornerRadius = 0f, Vector2 loopInset = default`.
  - `AreaOutlineData`: 같은 이름 속성 둘과 생성자 끝 선택 인자(`sortingOrder` 뒤). `ToMeshData`가 넘깁니다.
  - `MeshHightLevelBuilder.CreateGridOutline`: 덩어리 길 찾기는 그대로입니다. 고리마다 `AddOutlineLoop`이
    - `LoopInset`이 0보다 크면 곧은 변 가운데 점을 빼고(`CornersOf`), 꺾인 점마다 앞뒤 변을 안쪽(진행 방향 오른쪽)으로 옮긴 두 선의 교점을 씁니다(`InsetCorners`). 가로(X 방향) 변은 y, 세로(Z 방향) 변은 x만큼 옮깁니다.
    - `CornerRadius`가 0보다 크면 볼록(오른쪽으로 꺾임) 점만 사분원 일곱 점(직선 여섯 조각)으로 잇고, 오목 점은 직각 그대로입니다(`RoundCorners`). 반지름은 앞뒤 변 길이의 반으로 자르고, 두께 반보다 작으면 띠 안쪽 끝은 두 변 안쪽 선의 교점에 모입니다. 점선 U는 둥근 길이를 따라 이어집니다.
    - 둘 다 0이면 옛 사선 식(`MiterSides`, 옛 코드를 함수로 뺀 것)이라 옛 메시와 점·UV가 같습니다.
  - 새 줄은 파일처럼 CRLF입니다.
- **바뀌지 않은 것:** `IAreaOutlineHandle`, `AreaOutlineSystem`, 풀, 셰이더·`AreaOutline.mat`, 서비스 키, Factory. 기본값이 0이라 `TestRuner.cs` 같은 기존 호출은 그대로 컴파일되고 동작도 같습니다.
- **기댄 성질(코드 주석에 적음):** 길 찾기 고리는 진행 방향 오른쪽이 늘 칸 덩어리 안쪽입니다(바깥 둘레는 위에서 보아 시계 방향, 구멍 둘레는 반대). 그래서 띠 '+' 쪽(진행 방향 × 위)은 늘 바깥이고, 오른쪽으로 꺾이면 볼록입니다. 대각선으로만 닿은 칸은 오른쪽 돌기 규칙으로 따로 고리가 되어, 안쪽으로 옮기면 서로 떨어집니다.
- **전투 쪽 사용:** `FieldOutlineRenderer`가 덩어리 테두리에는 `cornerRadius`(칸 짧은 반변 × View DB `outlineCornerRadius`, 기본 0이라 지금 화면 그대로)를, 겹친 칸 테두리에는 깊이마다 `loopInset`·`cornerRadius`(채우기 부품의 판 모양 식)를 넘깁니다. 칸 채우기는 우리 새 셰이더 `Prefabs/Combat/Shaders/CombatCellFill.shader`로 바뀌어 채우기는 `AreaOutline.mat`을 쓰지 않습니다(테두리·궤적은 계속 씁니다).
- 중간(3판)에 칸마다 고리를 만드는 `cellLoopScale`을 넣었다가 4판에서 `loopInset`으로 바꿨습니다. 둘 다 커밋 전이라 들어가는 것은 최종 모양뿐입니다.
- **값 범위(검토 반영):** `LoopInset`의 음수·NaN 축은 0으로 봅니다(`Vector2.Max`). 셀 크기의 반 이상이면 고리가 뒤집히는데 이것은 검사하지 않고 필드·속성 주석에 적었습니다(전투 쪽은 늘 그보다 작게 넘김). `CornerRadius`는 0 이하·NaN이면 직각입니다.
- **선택 값 하나 더(5판, 2026-10-29, 사용자 허락 Q-RC17, 설계 §10·계획 §7.7):** 칸마다 네 변을 모두 그리는 방식을 더했습니다(지금 덩어리 방식은 그대로 기본).
  - `GridOutlineCreateData.EachCell`(bool)·`AreaOutlineData.EachCell`, 생성자 끝 선택 인자 `bool eachCell = false`(`loopInset` 뒤). 거짓이면 옛 메시와 같습니다.
  - `CreateGridOutline`: 참이면 덩어리 길 찾기 대신 `AddEachCellOutline`으로 갑니다. 안쪽 거리·반지름이 모두 0이면 `AddCellGridLines`가 칸마다 네 변을 그리되 맞닿은 변은 한 번만 그립니다(가로 변은 같은 줄에 이어진 것끼리 띠 하나·양 끝 두께 반 늘림, 세로 변은 변마다 양 끝 두께 반 줄임 → 교차점이 두 번 칠해지지 않음, 세로 점선 U는 이어진 줄 길이 기준). 아니면 칸마다 고리 하나(`AddOutlineLoop` 그대로, 네 모서리가 다 볼록)를 그립니다.
  - 도우미 `GridPoint`(옛 `GridLoopPoints` 안 식을 뺀 것)·`DashRepeats`(옛 고리 반복 수 식을 뺀 것, 고리도 부름)·`RunEnd`·`AddBand`(고리와 같은 꼭짓점·삼각형 순서). 새 줄은 CRLF입니다.

## 42. 2026-10-30 전투 특성 시스템 — 흔적 표시 연결 줄 셋 (미전달)

- 왜: 특성 흔적(땅 다지기·먼지 연막)을 필드에 그립니다([특성 시스템 View 설계](../specs/2026-10-29-combat-trait-system-view-design.md) TS7, [계획](../plans/2026-10-30-combat-trait-system-view-plan.md) §7.6 Te.5). 새 표시 `Display/Map/FieldTrailPresenter`는 특성 외형 표의 흔적 칸을 읽습니다.
- 바꾼 줄(`Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs`, 셋):
  - 표시 만들기 `var trails = new FieldTrailPresenter(field.CellsRoot, tables.Traits);`(가시밭 표시 다음).
  - 필드 창구 `CombatFieldEventPorts` 생성의 새 필수 칸 `showTrail: trails.Show,`(지형 바뀜 다음 자리).
  - 씬 표시 생성 인자 `trails`(가시밭 `thorns` 다음).
- 바뀌지 않은 것: 서비스 키·그룹·View DB 연결 줄·다른 Factory. 흔적 표시는 Mapper를 붙잡지 않아 해제·정리는 씬 표시가 맡습니다.
- 이번 묶음의 다른 변경(유닛 행동·공격 반응 등)은 사건 Presenter 생성자 안 조립이라 Factory 줄은 이 셋뿐입니다.

## 43. 2026-10-31 전투 몸 자리 이펙트 따라가기 — 이펙트 창구 생성 줄 둘 (미전달)

- 왜: 특성 발동·피격 등 유닛 몸 자리에 낸 이펙트가 유닛 이동을 따라가게 합니다([이펙트 따라가기·회수 설계](../specs/2026-10-31-combat-effect-follow-recall-design.md) FE2).
- 바꾼 줄(둘): `CombatEffectWindow` 생성에 셋째 인자 몸 읽기 `units.TrackBodyPresence`(같은 표시가 붙어 있나 + 몸 위치, 2026-10-31 FE11로 `TrackBody`에서 바꿈)를 넘깁니다.
  - `Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs`: `new CombatEffectWindow(services.Effects, field.CellsRoot, units.TrackBodyPresence)`(바로 위 주석 한 줄도 고침).
  - `Factory/Runtime/InGame/View/CombatTraitPreviewFactory.cs`: `new CombatEffectWindow(services.Effects, field.CellsRoot, units.TrackBodyPresence, scope)`.
- GlobalSystem 이펙트 시스템(`EffectScope`·`ParticleStopNotifier`)은 고치지 않았습니다. 따라가는 이펙트는 필드 루트 아래 우리 받침(`CombatEffectFollower`)을 `TargetParent`로 넘겨 놓고, 유닛 표시가 사라지면 받침이 그 이펙트의 파티클을 지워 기존 멈춤 알림으로 풀에 돌아가게 합니다.

## 44. 2026-10-31 도발당함 디버프 — 상태 표시 변환 등록 한 줄 (미전달)

- 왜: Core에 도발당함 디버프(`StatusEffectKey.Taunted = 14`)가 생겨, HUD가 그 상태를 그리려면 표시 변환이 등록돼야 합니다([도발 디버프 설계](../specs/2026-10-31-combat-taunt-debuff-design.md), [계획](../plans/2026-10-31-combat-trait-core-changes-plan.md) §2.6 Tb.1). 없으면 도발당한 적이 생기는 순간 `[CombatEffectFormatters] 표시 변환이 등록되지 않았습니다: Taunted` 예외입니다.
- 바꾼 줄(`Factory/Runtime/InGame/View/CombatUnitStatusFactory.cs`, 하나): 표시 변환 목록 끝에 `new CombatEffectFormatter<TauntedEffectSnapshot>(StatusEffectKey.Taunted)`(값 없음). 앞 줄 끝에 쉼표.
- 함께 넣은 것: View DB 상태 외형 `UnitStatusVisual_Taunted`와 표 등록. 표시 변환 묶음이 만들 때 등록된 종류마다 외형을 확인하므로 둘은 같은 커밋입니다.
- 바뀌지 않은 것: 서비스 키·그룹·다른 Factory.

## 45. 2026-11-01 전투 View 조립 변경 (미전달)

### 45.1 토네이도 전투 View — 표시 조립 줄

- 왜: Core 토네이도가 지형 칸이 아니라 스테이지 기믹이 되어, 지형 표시 대신 전용 표시가 그립니다([토네이도 View 설계](../specs/2026-11-01-combat-tornado-view-design-Hojin.md), [계획](../plans/2026-11-01-combat-tornado-view-plan-Hojin.md)).
- 바꾼 줄(`Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs`):
  - `var tornado = new FieldTornadoPresenter(field.CellsRoot, tables.Terrains);`를 더하고 표시 생성에 넘깁니다.
  - 필드 창구: `hideTornado`·`showTornado` 두 인자를 뺐고 끝 인자 `tornado`를 더했습니다. 외형 연출 넷(`tickAppearance`·`appearancePlaying`·`appearanceBlocksStart`·`cancelAppearance`)은 지형 표시와 토네이도 표시를 합쳐 넘깁니다.
- 같은 창구를 쓰는 `CombatTraitPreviewFactory.cs`도 맞췄습니다(용근님 줄, [공유 메모](2026-11-01-combat-tornado-view-yonggeun-share-Hojin.md)).
- 바뀌지 않은 것: 서비스 키·그룹·GlobalSystem·Framework.

### 45.2 전투 이펙트 구조 2판 — 공격 조준 생성 줄 인자 하나

- 왜: 마무리 사격처럼 공격 모드 동안 조건을 채운 적에게만 이펙트를 보이려면 공격 조준이 Core 범위 응답의 즉시 처치 대상을 표시 의도로 넘겨야 합니다([이펙트 끝 규칙 설계](../specs/2026-10-31-combat-effect-lifetime-design.md) EL3, [계획](../plans/2026-10-31-combat-effect-structure-plan.md) TP-E16·Ed.1).
- 바꾼 줄(`Factory/Runtime/InGame/View/CombatSceneInputFactory.cs`, 하나 + 바로 위 주석): `new AttackTargetingInteraction(inputWrite, inputRead, display, display, gate, display, display, sounds, …)` — 넉백 미리보기 다음에 새 필수 인자 `display`(새 좁은 계약 `ICombatAttackModeTargets`, 구현은 씬 표시)를 하나 더 넘깁니다.
- 바뀌지 않은 것: 서비스 키·그룹·다른 Factory. 이펙트 창구·사건 Presenter 생성 줄은 그대로입니다(유지 이펙트 부품·출처는 Presenter 생성자 안에서 만듦, 계획 TP-E15).

## 46. 2026-11-02 Night 맵 카메라 요구·밤 화면 전환 설정 제거 (미전달)

- 사용자 요청: `Night Camera`·`NightMap` 없이 맵을 쓰도록 밤 카메라 존재 검사와 Night 화면 전환 설정까지 제거합니다. 범위·검증은 [N1 계획·기록](../plans/2026-11-02-combat-night-transition-settings-removal-plan.md)입니다.
- Factory 변경: `CombatSceneDisplayFactory.Create`의 `RequireMapCamera(..., presentation.NightTransitionCamera)` 호출 한 줄을 제거하고 인접 주석을 갱신했습니다. Day 카메라 존재 검사와 공통 검사 함수는 유지합니다.
- 함께 변경: View DB 설정의 `nightTransitionLayer`·`nightTransitionCamera`·`nightTransitionColor` 필드·속성·이름 검사와 기본 SO 키를 제거합니다. `CombatPhaseTransitionSettings`는 Morning 요청만 생성하고 밤 레이어를 조회하지 않습니다. 비활성 Presenter의 주석 예제도 Morning 요청만 남깁니다.
- 조립 경로: 기존 Factory → 사건 재생 Factory → 주입된 Global Render 서비스 경로를 유지합니다. 서비스 등록·Export·소비자 조회·Scope 수명 계약 변경은 없습니다. 자동 화면 전환은 계속 비활성입니다.
- 공용 Render·수동 TestRuner·프로젝트 레이어, 씬·맵 프리팹, Core·FieldCamera·Architecture·밤 국면/BGM은 변경하지 않습니다. 실제 Unity 재생 확인과 빌드·테스트 실행 여부는 N1 기록에서 구분합니다.

## 47. 2026-11-02 낙석 전투 View (미전달)

- 왜: Core 기믹 낙석을 전용 표시·처리기로 받습니다([낙석 View 설계](../specs/2026-11-01-combat-rockfall-view-design-Hojin.md), [계획](../plans/2026-11-01-combat-rockfall-view-plan-Hojin.md)).
- View 표 연결: `ViewTableKey`, `ViewDataBase`, `ViewDataBaseFactory`에 `RockfallVisual` 표를 더했습니다. `ThornFieldVisual`과 같은 길입니다.
- `CombatSceneDisplayFactory.cs`: `new FieldRockfallPresenter(field, tables.Rockfall, backdrop?.RockfallTunnel)`를 더해 표시 생성과 필드 창구 끝 인자에 넘깁니다.
- `CombatTraitPreviewFactory.cs`: 같은 표시를 만들어 창구에 넘깁니다(빈 표시).
- `CombatSceneInputFactory.cs`: 환경 설명 생성 인자에 `tables.Rockfall`을 더했습니다.
- `CombatUnitStatusFactory.cs`: `Create` 끝 인자 `RockfallVisualData`를 상태 설명 표시에 넘깁니다(상태 출처를 "낙석"으로 표시).
- 이펙트 등록: `EffectType.FallingRocks`(15), `RenderGroup/EffectTable/Items/FallingRocks.asset`(풀 최대 16), 표 한 줄.
- 민형님 프리팹 변경: `FallingRocks.prefab` 루트에 `ParticleStopNotifier`가 없어 첫 재생 때 이펙트 풀이 예외를 던졌습니다. 사용자가 컴포넌트를 더했고, 파티클 값은 그대로입니다.
- 민형님 프리팹 사용: 돌은 `Prefabs/Stage/MapProp/smallrock (3).prefab`입니다. 프리팹 레이어가 21이라 돌을 만들 때 코드가 20으로 맞춥니다.
- 맵 프리팹 규칙: 터널 폭발 자리는 프랍 안의 빈 오브젝트 `RockfallTunnelBoom`의 위치·회전입니다. 없으면 폭발 없이 진행하고, 둘이면 예외입니다.
- 바뀌지 않은 것: 서비스 키·그룹·GlobalSystem·Framework.
- 컴파일 미확인, 새 시험 없음.

## 48. 2026-11-02 하붑 전투 View (미전달)

- 왜: Core 기믹 하붑을 전용 표시·처리기로 받습니다([하붑 View 설계](../specs/2026-11-01-combat-haboob-view-design-Hojin.md), [계획](../plans/2026-11-01-combat-haboob-view-plan-Hojin.md)).
- View 표 연결: `ViewTableKey`, `ViewDataBase`, `ViewDataBaseFactory`에 `HaboobVisual` 표를 더했습니다. `ThornFieldVisual`과 같은 길입니다.
- `CombatSceneDisplayFactory.cs`: `new FieldHaboobPresenter(tables.Haboob, effects)`를 더해 표시 생성과 필드 창구 끝 인자에 넘깁니다.
- `CombatTraitPreviewFactory.cs`: 이펙트 창구 만드는 줄을 필드 창구보다 앞으로 옮기고, 같은 표시를 만들어 넘깁니다(빈 표시).
- `CombatSceneInputFactory.cs`: 환경 설명 생성 인자에 `tables.Haboob`을 더했습니다.
- 이펙트 등록: `EffectType.Smoke`(16), `RenderGroup/EffectTable/Items/Smoke.asset`(풀 최대 32), 표 한 줄.
- **민형님 프리팹 변경:** `Script/Render/Effect/EventEffect/SmokeEffect/Smoke.prefab` 레이어 18(`Blending`) → 0(Default). 사용자가 저장했습니다. 메인 카메라 Culling Mask(0·20·21·30·31)에 18이 없어 전투에서 연기가 그려지지 않았습니다. 낮·밤 전환이 끝나면 마스크가 고정 레이어(Default·BothRendering·DayCameraRender·NightCameraRender) + 전환 레이어로 덮어써져, 카메라에 18을 더해도 전환 뒤 다시 빠집니다.
- **민형님 프리팹 변경 2:** 같은 프리팹의 시뮬레이션 공간 Local → World(`moveWithTransform: 1`). 하붑 구름이 새 자리로 옮겨 갈 때 이미 나온 연기가 뒤에 남아 사라지게 하려는 것입니다(사용자 요청). 이 프리팹은 하붑만 씁니다(`Smoke.asset` 하나가 참조). 멈춰 있을 때 모습은 같습니다.
- **확인 부탁:** 이 변경으로 이 연기는 `EffectOcclusionOutlineFeature`의 Effect Layers(18)에서 빠져, 연기에 가려진 캐릭터 윤곽선이 나오지 않습니다. 윤곽선의 Character Layers는 6(`Entity`)인데 확인한 아군 프리팹 둘(`Akimbo`, `Shotgun`)은 20이라 전투에서는 원래도 나오지 않을 것으로 봅니다. 윤곽선을 전투에서 쓰려면 고정 레이어와 유닛 레이어까지 함께 정해야 합니다.
- 바뀌지 않은 것: 서비스 키·그룹·GlobalSystem·Framework.
- 컴파일 미확인, 새 시험 없음.

## 49. 2026-11-02 낙석 돌 출발 능선 (미전달)

- 왜: 돌이 터널 한 점으로 모여 원뿔처럼 보여, 맵의 능선 구간에서 출발하게 했습니다([낙석 View 설계 §10](../specs/2026-11-01-combat-rockfall-view-design-Hojin.md), [계획 §7](../plans/2026-11-01-combat-rockfall-view-plan-Hojin.md)).
- `CombatSceneDisplayFactory.cs`·`CombatTraitPreviewFactory.cs`: `new FieldRockfallPresenter(...)` 끝에 인자 둘(`RockfallRidgeStart`·`RockfallRidgeEnd`)을 더했습니다. 배경 `StageViewPropPresenter`가 맵 프리팹에서 찾은 값입니다.
- **민형님 창 변경:** `CombatLayoutWindow.SoUx.cs`·`SoMode.cs`의 낙석 경고 함수 이름이 `AddSoRockfallPropWarnings`로 바뀌었습니다. SO 모드 쪽 줄이고 DT 모드는 그대로입니다.
- 바뀌지 않은 것: 서비스 키·그룹·GlobalSystem·Framework.
- 컴파일 미확인, 새 시험 없음.

## 50. 2026-11-04 전투 View 평가 후속 3·4차 — 필드 창구 조립 함수·입력 Factory 내부 단계 (미전달)

- 왜: 평가 C4-02(운영과 특성 미리보기의 필드 창구 조립이 두 벌)와 C8-14(입력 Factory 내부 단계가 밖에 열려 있음)의 1단계입니다([후속 수정 계획](../plans/2026-11-04-combat-view-evaluation-fixes-plan.md) §8).
- `CombatSceneDisplayFactory.cs`: 필드 창구(`CombatFieldEventPorts`) 조립을 새 `internal static CreateFieldPorts(field, thorns, objects, terrain, trails, tornado, rockfall, haboob)`로 옮겼습니다. 인자 null 검사 여덟이 들어 있습니다. 운영 `Create`가 이 함수를 부르고, 넘기는 람다·메서드와 순서는 그대로입니다.
- `CombatTraitPreviewFactory.cs`: 미리보기도 같은 함수를 부릅니다. 그래서 미리보기에도 토네이도 외형 연결(Tick·재생 중·시작 막음·취소)이 들어갑니다(사용자 결정 Q-N2 A). 지금 미리보기 시나리오에는 토네이도가 없어 화면은 같습니다.
- `CombatSceneInputFactory.cs`: `CreateEnvironmentHover`·`CreateSceneInteraction`·`CreateCursorSnap`을 internal에서 private으로 바꿨습니다. 부르는 곳은 같은 파일의 `CreateSceneInput` 하나입니다. 같은 파일 주석 셋도 지금 구조에 맞췄습니다.
- 같은 묶음에서 입력 기능 셋의 생성자 인자를 필수로 바꿨습니다(C5-02). 입력 Factory는 이미 모든 값을 넘기고 있어 생성 줄은 그대로입니다.
- 바뀌지 않은 것: 서비스 키·그룹·Builder·GlobalSystem·Framework, `CombatSceneDisplay`·`CombatSceneEventPresenter`·`CombatFieldEventPorts` 서명.
- 컴파일 미확인, 새 시험 없음.
