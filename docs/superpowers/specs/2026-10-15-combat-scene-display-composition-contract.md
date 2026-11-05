# CombatSceneDisplay SD-D0 조립·소유권 명세

- 작성: View 작업, 2026-10-15. D0 조사 결과이며 아래 D1 코드는 아직 구현하지 않았다.
- Task·실행 상태 원본: [조립·기능 분리 계획](../plans/2026-10-14-combat-scene-display-composition-plan.md) §4·§10.2.
- 설계 원본: [후속 설계](2026-10-14-combat-scene-display-followup-design.md) §5.3·§5.5·§6. 이 문서는 그 방향의 D1 주입·순서를 구체화한다.
- 조사 기준: `6809cefab8a8e775cb6ce958757aa26b585eb123`. 시작 시 tracked 변경·untracked 파일 없음. 검증 산출물은 ignored `Temp/CombatSceneDisplaySDD0/`에 작성했다.

## 1. 기준 코드와 보존 범위

SD-A의 생성 전 중복 연결 검사, SD-B의 실제 Display를 통한 좁은 재생 계약, SD-C′의 Units 비용 대상 소유권이 현재 코드에 있다. 커밋은 Unity 실행 근거로 세지 않는다. 선행 검증·대기는 [이전 계획 §7.4~7.6](../plans/2026-10-14-combat-scene-display-followup-plan.md)이 원본이다.

계획 작성 당시 별도 작업이던 커서 감도는 현재 HEAD에 포함됐다. D1은 `IsCursorSnapEnabled`와 `CursorSnapThreshold`를 root 참조에서 호출마다 읽는 의미를 보존한다. 전자는 `_isReleased`도 확인하고, 후자는 현재 해제 검사 없이 root 값 또는 `0f`를 반환한다. 순수 추출 중 두 getter의 방어를 통일하지 않는다.

이하 운영 파일의 기본 경로는 `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/`이다. 기준 구현은 [CombatSceneDisplay.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatSceneDisplay.cs), 호출·인수 경계는 [CombatViewSystem.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/CombatViewSystem.cs)다.

## 2. 참조별 생성·사용·공유·정리

현재 Display의 협력 객체·설정 참조는 24개다. D1에서는 아래 취득·생성·Configure를 Factory로 옮기고 **같은 인스턴스**를 주입한다. 표의 정리 책임은 호출 순서를 정하는 Display와 실제 자원을 반환하는 Presenter를 구별한다. 씬에서 받은 컴포넌트를 소유 GameObject처럼 파괴한다는 뜻이 아니다.

| Display 참조 | 현재 취득·생성 → D1 위치 | 사용·공유 인스턴스 | Bind·Clear·Release 소유자 |
|---|---|---|---|
| `_field` | `root.Field` → Factory 취득 | Display, MovementPlanning, Pointer, EventPlayback 공유 | Display가 Build·Release 호출. Mapper는 매 Install의 Build 결과 |
| `_movementPlanning` | `new(field, units, ghost, config)` → Factory | 이동 편집·범위. Field/Units/Ghost/Config 공유 | Display의 `_fieldBound`에서 Clear, 최종 Release. 내부 원본 흐림·Ghost·Range는 Presenter 정리 |
| `_units` | `root.FieldUnits` + Configure → Factory | 비용·HP·원본·타임라인, MovementPlanning·Events 공유 | Display Clear/Release에서 미리보기 정리와 Release. 실제 View 대여는 Units 관리 |
| `_hand` | `root.Hand` + Configure → Factory | 손패·원본 표시, Pointer 공유 | Display Clear/Release에서 Release |
| `_hud` | `root.Hud` → Factory 취득 | 재화·이동 견적·완료 표시 | Display Install/Release에서 HideCompletion, Clear에서 Clear |
| `_ghost` | `root.Ghost` + Configure → Factory | 배치 표시와 MovementPlanning 공유 | Display Clear에서 Hide, 최종 Release. MovementPlanning은 Hide하며 최종 풀 반환을 중복 소유하지 않음 |
| `_abandon` | `root.AbandonPanel` → Factory 취득 | 포기 버튼·확인창 | Display가 Bind 전달. Release에서 null 연결·HideConfirm |
| `_actions` | `root.ActionPanel` → Factory 취득 | 행동 버튼·복귀 견적 | Display가 Bind 전달. Clear에서 가용성 초기화, Release에서 null 연결·초기화 |
| `_cursorSnapSettings` | root 자체 → Factory 전달 | 실시간 스냅 설정 조회 | 빌린 씬 참조. 별도 정리 없음 |
| `_pointer` | Field/Hand/Camera/Inset으로 new → Factory | 집기·좌표·UI Raycast. Field/Hand 공유 | Display Install에서 Bind, `_fieldBound`에서 Unbind, 최종 Release |
| `_devLayerPanel` | root 패널·버튼으로 new → Factory | 개발 Layer 표시·입력 연결 | Display Show/Hide 전달, 최종 Release |
| `_fieldConfig` | `tables.Field` → Factory 취득 | Field Build·MovementPlanning·Ghost 공유 | 빌린 설정. Display가 해제하지 않음 |
| `_colors` | `services.Colors` → Factory 취득 | Field Build·Units 이동 외형·Ghost 공유 | 빌린 서비스. 서비스 자체 Shutdown/Dispose 없음 |
| `_areaOutlines` | `services.AreaOutlines` → Factory 취득 | Field Build | 빌린 서비스. 사용자가 만든 scope/표시는 기존 소유자가 반환 |
| `_pointLines` | `services.PointLines` → Factory 취득 | Field Build | 위와 같음 |
| `_events` | EventPresenter new → Factory | Display Refresh·연결 수명, EventPlayback 공유 | Display Bind/Clear/Release. Playback은 재생·취소 전달 |
| `_audio` | `services.Audio` → Factory 취득 | Display BGM·완료음, Events 공유 | null 허용. 서비스 자체 해제 없음 |
| `_props` | StageProps/CellsRoot로 new → Factory | Install 배경 표시 | Display Install에서 Show, Clear/Release에서 Release |
| `_objects` | CellsRoot/Objects/Environments로 new → Factory | Refresh에서 현재 Mapper로 Render | Display Install 실패·Clear에서 Clear, 최종 Release. 실제 View 자원은 Presenter 소유 |
| `_terrain` | CellsRoot/Terrains/Environments로 new → Factory | Objects와 같은 Environments 조회 공유, 현재 Mapper 사용 | Objects와 같은 정리 경계 |
| `_railway` | CellsRoot/Railway/Effects로 new → Factory | Display 연결 수명, EventPlayback 공유 | Display Install/Clear/Release. Playback은 재생·취소 전달 |
| `_environmentEffects` | Effects/CellsRoot/Boom/규칙 배열로 new → Factory | Events의 Present 콜백·EventPlayback·Display 공유 | Display Bind/Clear/Release, Playback Cancel |
| `_thorns` | prefab이 있으면 new → Factory | Display Sync, Events 소비 콜백, EventPlayback에 동일 객체 | Display Clear/Release, Playback Cancel에서도 Clear. ThornPresenter가 만든 자식·clone만 소유 |
| `_eventPlayback` | Events/Railway/Field/Environment/Thorns로 new → Factory | Display의 사건 재생 구현 | 별도 Bind/Release 없음. Display가 CancelRailwayContacts 호출 |

Display 내부에 남는 세 항목은 주입 대상이 아니다.

| 항목 | 유지 이유 |
|---|---|
| `_fieldBound: Action[]` | Install 직전·실패·Clear의 공통 정리 목록. 정의·실행을 Display 한 곳에 유지 |
| `_isReleased` | 실제 Display의 공개 호출 방어·최종 수명 원본 |
| `_blockedWallCells` | 공격 벽 표시의 임시 목록. D1에서는 유지하며 D4에서 기능 구현과 함께 이동 |

비용 대상은 `FieldUnitPresenter`, 격자 층은 `FieldHighlightState`, 이동 편집·범위는 기존 MovementPlanning이 계속 소유한다. Factory에는 런타임 표시 상태나 전역 캐시를 두지 않는다.

## 3. D1의 명시적 주입 계약

새 진입점은 `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/CombatSceneDisplayFactory.cs`의 `internal static CombatSceneDisplay Create(CombatSceneRoot root, CombatDisplayTables tables, CombatDisplayServices services)`다. 네임스페이스는 `ProjectL.Systems.Factory.Runtime.InGame.View`이며 새 인터페이스·서비스 등록은 없다.

Display의 기존 root/tables/services 조립 생성자를 다음 서명으로 대체한다. 생성자 본문은 참조 보관과 기존 `_fieldBound` 구성만 담당한다. 검증·Configure·협력 객체 생성은 아래 Factory 순서에 둔다. 별도 후설정 setter나 무타입 Parts 묶음을 추가하지 않는다.

```csharp
internal CombatSceneDisplay(
    FieldView field,
    CombatMovementPlanningPresenter movementPlanning,
    FieldUnitPresenter units,
    HandPresenter hand,
    CombatHud hud,
    PlacementGhost ghost,
    CombatAbandonPanel abandon,
    CombatActionPanel actions,
    CombatSceneRoot cursorSnapSettings,
    CombatPointerMapper pointer,
    CombatDevLayerPanelBinding devLayerPanel,
    StageFieldViewData fieldConfig,
    IColorSystemService colors,
    IAreaOutlineSystemService areaOutlines,
    IDrawPointLineSystemService pointLines,
    CombatSceneEventPresenter events,
    IAudioSystemService audio,
    StageViewPropPresenter props,
    FieldObjectPresenter objects,
    FieldTerrainPresenter terrain,
    FieldRailwayPresenter railway,
    CombatEnvironmentEffectPresenter environmentEffects,
    FieldThornPresenter thorns,
    CombatSceneEventPlayback eventPlayback)
```

`audio`와 `thorns`는 null을 허용한다. 다른 참조의 유효성은 기존 Factory 입력·root·하위 생성자 검증에서 확보한다. 운영·실제 씬 fixture는 모두 Factory를 사용하며 옛 조립 생성자를 병행 유지하지 않는다. 긴 생성자는 D1의 중간 비용이고, D2~D4의 기능 분리 뒤 D5에서 남는 참조의 이유를 평가한다.

### 3.1 Factory 실행 순서

| 순서 | 실행 내용·보존 이유 |
|---|---|
| 1 | `root` → `tables` → `services` null 검사 |
| 2 | Field 설정, Colors → AreaOutlines → PointLines → Audio 취득 |
| 3 | `root.ValidateReferences()`. timing 검사는 EnemyActionGap → EnemyTurnGap → MovementTurn 순서. MovementStep은 여기서 검사하지 않음 |
| 4 | root의 Field → Units → Hand → Hud → Ghost 취득 |
| 5 | 기존 전체 생성자 `CombatMovementPlanningPresenter(field, units, ghost, fieldConfig)` |
| 6 | root의 AbandonPanel → ActionPanel 취득 |
| 7 | DevLayerPanelBinding 생성 |
| 8 | `CombatPointerMapper(field, hand, root.WorldCamera, root.InnerCellInset)` 생성 |
| 9 | 커서 설정으로 root 참조 보관. 스냅 값은 복사하지 않음 |
| 10 | `units.Configure(tables.Units, fieldConfig.FieldUnitScale, tables.Traits, root.WorldCamera)` |
| 11 | `units.ConfigureMovementAppearance(colors)` |
| 12 | `hand.Configure(tables.Units)` |
| 13 | StageProps → FieldObjects → FieldTerrain → FieldRailway 생성. 모두 기존 `field.CellsRoot` 사용 |
| 14 | EnvironmentEffects 생성. `EffectType.Boom`, 규칙은 PowderBarrel → Mine |
| 15 | `ghost.Configure(tables.Units, fieldConfig, colors)`. 내부 Release가 있으므로 중복 Attach 검사는 반드시 그 이전 |
| 16 | `FieldThornPresenter thorns = null` 지역 참조를 준비하고 기존 인자 순서로 EventPresenter 생성. 소비 콜백만 `cell => thorns?.Consume(cell)`, 환경 콜백은 같은 `environmentEffects.Present` |
| 17 | `tables.ThornField.Prefab` 취득. null이 아닐 때만 ThornPresenter를 생성해 지역 참조에 한 번 할당 |
| 18 | 같은 Events/Railway/Field/EnvironmentEffects/Thorns로 EventPlayback 생성 |
| 19 | §3의 24개 참조로 Display 생성·반환. Display가 아래 정리 목록 구성 |

```csharp
_fieldBound = new Action[]
{
    _eventPlayback.CancelRailwayContacts, _movementPlanning.Clear,
    _events.Clear, _environmentEffects.Clear, _pointer.Unbind,
    _railway.Clear, ClearThorns
};
```

`root.WorldCamera`도 선택값이다. null을 새 오류로 만들거나 Factory에서 `Camera.main`을 고정하지 않는다. 현재 PointerMapper는 호출 시 대체 카메라를 찾는다. Mapper도 Factory에서 저장하지 않고 Install 때 Build한 현재 Mapper에 연결한다.

### 3.2 가시밭 콜백 전제

현재 Display의 `ConsumeThorn`는 `_thorns?.Consume(cell)`만 호출하며 Display 상태를 읽지 않는다. EventPresenter 생성자는 소비 콜백을 필드에 저장할 뿐 호출하거나 하위 생성자에 넘기지 않는다. 실제 호출은 이후 ThornConsumed 사건 처리다. 따라서 Factory의 지역 참조 캡처로 바꾸어도 같은 객체에 도달한다.

지역 변수는 null로 초기화한 뒤 선택적 생성 성공 시 한 번만 할당하며, Factory 반환 뒤 재할당하거나 외부에 노출하지 않는다. 같은 참조를 EventPlayback·Display에도 넘긴다. ThornPresenter 생성자는 `FieldThorns_Inactive` GameObject를 만들므로 **EventPresenter의 step 검증보다 먼저 생성하지 않는다.** D1에서 Display의 ConsumeThorn 전달 메서드만 제거하고 수명용 ClearThorns·ReleaseThorns는 유지한다.

## 4. 수명·인수·실패 경계

공통 순서 원본은 계속 Display다. Factory로 옮기는 것은 §3의 조립뿐이다.

- Install: 공개 해제 검사 → `_fieldBound` 정리 → Field.Build → Railway.Install → Environment.Bind → Events.Bind → Pointer.Bind → 정적 강조 → Props.Show → Refresh. 실패 시 `_fieldBound`와 Objects/Terrain.Clear를 실행하고 기존 오류 수집을 유지한다.
- Clear: `_fieldBound` → 비용/HP → Ghost.Hide → Units/Hand.Release → Hud.Clear → 행동 버튼 숨김 → Objects/Terrain.Clear → Props/Field.Release. Clear는 재설치 가능 수명이다.
- Release: 플래그를 먼저 세우고 기존 정리 목록·순서·실패 수집을 유지한다. 취소·이동·미리보기·Pointer·Events·환경·레일·가시밭 → 완료창·개발/포기/행동 UI → Ghost·Units·Hand·Objects·Terrain·Props·Field 순서다.
- 실제 Display의 사건 시작·Install·Refresh·완료 표시 방어와 늦은 입력 표시 무시, null 콜백 연결 해제 전달을 유지한다. Presentation에는 내부 Playback 대신 계속 실제 Display를 넘긴다.

| 인수 경계 | 현재 사실과 D1 판정 |
|---|---|
| Factory 이전 | ViewSystem의 root·fault·초기화·활성 전투·`_display != null` 검사를 그대로 유지. StopInput 뒤에도 중복 생성 거절 |
| Factory 내부 | Configure가 이미 씬 객체를 바꾼 뒤 실패할 수 있음. 순서만 보존하며 사전 검증 통합·부분 복구는 별도 후속 |
| 반환 뒤 `_display` 저장 전 | 입력 Clear·Presentation.Attach 등이 실패하면 Detach가 생성 Display를 찾지 못할 수 있음. **미해결 위험 유지**, 이번 추출로 해결됐다고 쓰지 않음 |
| `_display` 저장 뒤 | Install 실패 시 보관된 Display를 Detach로 반환할 수 있음. 인수 전 실패와 혼동하지 않음 |
| root 구독 | AttachDisplay 정상 반환 뒤 구독. `_display=null` 정상 반환은 운영 경로에서 발견하지 못했으며 별도 잠재 위험으로 유지 |

범위 원본은 [Task 계획 §3](../plans/2026-10-14-combat-scene-display-composition-plan.md)다. D1에서 오류 회복 정책을 새로 정하지 않는다.

## 5. 호출부·변경 범위 지도

줄 번호는 D0 기준이다. 직접 `new CombatSceneDisplay`뿐 아니라 target-typed `new`와 helper 반환도 검색했다.

| 구분 | 현재 호출부 | D1 |
|---|---|---|
| 운영 1곳 | `CombatViewSystem.cs:332` | 사전 검사 뒤 Factory.Create 결과를 AttachDisplay에 전달 |
| 실제 fixture | `CombatDisplayReuseTests.cs:749` | Factory 전환 |
| 실제 fixture | `CombatPointerMappingTests.cs:230` | Factory 전환 |
| 실제 fixture | `CombatSceneEventPresenterTests.cs:70,292` | 두 경로 모두 Factory 전환 |
| 실제 fixture | `TerrainOperationalDisplayTests.cs:143,240` | 두 경로 모두 Factory 전환 |
| 실제 fixture | `WallAttackInteractionTests.cs:211` | Factory 전환 |
| 실제 fixture | `CombatMortarAssetTests.cs:608` | `CreateDisplay(...) => new(_root, ...)`의 target-typed 생성 전환 |
| 간접 fixture | `CombatRailwayContactSceneTests.cs:49~51` | EventPresenterTests fixture에서 Display를 얻음. 별도 생성 경로 아님 |

테스트 경로는 `Assets/Tests/EditMode/Framework/`다. 테스트 직접 생성은 **6파일 8곳**이며 FakeDisplay 기반 ViewSystem 테스트는 Factory에 결합시키지 않는다. 기존 teardown·실제 씬 관측을 유지한다.

이동 Presenter 생성은 현재 Display:95, `CombatMovementPlanningDisplayTests.cs:94`의 전체 생성자와 `CombatMovementRangeDisplayTests.cs:55`의 Field-only 생성자 3곳이다. D1은 운영 생성 위치만 Factory로 옮긴다. HUD 추가·초안 메서드 이전은 D2이고 Field-only 범위 테스트 경로를 보존한다. 배치·공격 구현과 `_blockedWallCells`는 D3·D4 대상이다.

후속 기능 이전의 운영 호출부도 다시 검색했다. 소비자의 호출 API는 유지하므로 아래 입력 클래스의 정책 수정은 D1~D4 변경 목록에 추가하지 않는다.

| 기능 | 현재 호출자·대표 위치 | 후속 변경·검증 경계 |
|---|---|---|
| 이동 초안 | `CombatPathInput.cs:404,452,480`의 SetPathPlanning/ShowDraft/ClearDraft | D2에서 Display 내부 구현만 MovementPlanning으로 이전. 기존 MovementPlanningDisplay/MovementRangeDisplay/PathInputCleanup/PointerMapping fixture 대조 |
| 배치 표시 | `PlacementDragInteraction.cs:194~209,257~258`의 Ghost·원본·좌표 표시, `:120`의 배치 가능 표시 | D3에서 배치 객체에 구현 집중. 입력 판단 유지, 기존 Ghost 공유·실제 씬 fixture와 새 좁은 fixture로 검증 |
| 공격 표시 | `AttackTargetingInteraction.cs:264~306,450~459`의 범위·영향·비용·HP·벽 표시/정리 | D4에서 공격 표시 구현 집중. 응답 판별 유지, WallAttackInteraction/AttackHealthPreviewDisplay/PointerMapping fixture 대조 |

위 기존 fixture와 `CombatSceneEventPresenterTests`, `CombatDisplayReuseTests` 파일의 존재를 확인했다. 새로운 기능별 fixture는 계획의 D2~D4 조건에 따르며 D0에서 만들지 않는다.

D1 추가 변경은 새 Factory `.cs/.meta`, Display, ViewSystem, 위 6개 fixture와 `CombatDisplayTables`·`CombatDisplayServices`·`CombatSceneEventPlayback`의 조립 설명 주석이다. Framework 공유 메모는 작성만 한다. Core·Buffer·FieldCamera·Architecture·씬·프리팹·DB 값·입력 정책은 변경하지 않는다.

## 6. 설계 기준 대조와 D1 인수 기준

| 기준 | D1에서 확인할 내용 |
|---|---|
| SRP | 생성·Configure의 변경은 Factory로, Display의 공개 방어·공통 수명은 유지 |
| OCP | 조립 변경을 한곳에서 할 수 있게 하되 범용 등록기나 추측성 확장 계층은 추가하지 않음 |
| LSP | 실제 Display의 해제·재설치·null 연결 해제·선택적 설정 계약 보존 |
| ISP | 소비자 재생·집기·피드백 계약 유지. 묶음 전체를 하위 기능으로 전달하지 않음 |
| DIP | 구체 조립은 Factory에 집중. 순수 추출에 필요하지 않은 인터페이스·서비스 추가 없음 |
| SSOT | 같은 Ghost/Units/Field/Thorns 공유. 비용·층·재생·해제 상태와 정리 목록을 복제하지 않음 |
| JMH/FW | Systems의 기존 Factory 경로 사용. 새 IServiceModule/AddModule/Builder 없음. UIInputBinding과 씬 진입/해제 경계 유지 |
| 기능 변경 독립성 | D1은 조립 수정 범위를 줄임. 이동·배치·공격 구현의 독립성은 D2~D4와 D5 변경 지도로 인수 |

컴파일·관리 회귀와 Unity 네이티브 검증을 구별한다. D0 실행 증거·대기 목록·재현 순서는 [D0 워크시트](../viewDocs/2026-10-14-combat-scene-display-sd-d0-worksheet.md)를, Phase 판정은 Task 계획 §10.2를 따른다.
