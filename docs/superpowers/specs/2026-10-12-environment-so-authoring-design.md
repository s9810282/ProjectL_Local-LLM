# 전투 환경 SO 입력 설계

작성: 2026-10-12. 대상: View 작업. 사용자 확인 범위는 정적 Terrain, 토네이도·위치 능력치 보정 Terrain, Obstacle, Object의 네 묶음이다.

**Follow-up status (2026-10-12):** User-authorized configurable tornado timing (Git history: `2026-10-12-tornado-repeat-interval-design.md`) Phases 1 and 2 are complete. The statements below about a fixed tornado repeat interval or pending Core delivery describe the original authoring scope and are superseded by this follow-up. High Ground/Sandstorm formulas and Terrain rendering scope remain unchanged. Implementation evidence and authoring steps are in the timing Task plan (Git history: `2026-10-12-tornado-repeat-interval-plan.md#phase-2-evidence--2026-10-12`).

## 1. 목표와 경계

종류 정의와 Core가 이미 받는 설정을 SO에서 작성하고, 스테이지별 위치 입력과 표현 입력을 분리한다. 사용자의 "DB는 일단 신경쓰지 말고 SO에만" 지시에 따라 Excel, Master 테이블, Query, Factory, Catalog 등록은 이번 범위 밖이다. 런타임 표시·연출 추가, Core 계산 공식 변경, 실제 레벨 디자인 값 결정도 별도 작업이다.

조사 시점에 Core는 네 묶음의 계산과 초기 배치 계약을 제공했고 기존 CombatStageData는 정적 지형·벽만 직렬화했다. Phase 2에서 위치 보정·정적 지형·Obstacle·Object 입력과 CombatStageDetail의 토네이도 목록을 추가했다. 운영 BattleDefinitionAdapter의 토네이도 전달은 Core 요청으로 남아 있다. SO 입력 완료와 운영 전투 반영 완료를 구분한다.

High Ground and Sandstorm formulas remain Core constants. The user-authorized timing follow-up extended the tornado contract: both first activation and repeat interval are now authored in TerrainDefinitionDataSO and captured in Core facts during preparation. First 2 / repeat 5 means completed individual unit turns 2, 7, 12, 17; both owners count and night changes do not reset the counter.

## 2. 소유권과 저장 위치

| 사실 | 소유 원본 | 포함하지 않는 것 |
|---|---|---|
| Terrain 종류·약한 지반 진입 한도·위치 능력치 보정·토네이도 첫 발동 | InGameGroup의 TerrainDefinition SO | 프리팹·아이콘·런타임 누적 횟수 |
| Obstacle 체력·점유 크기·폭발 피해·보급 보상 | InGameGroup의 ObstacleDefinition SO | 위치·표시 이름·프리팹 |
| Object 종류·지뢰 피해·깃발 우선순위 보너스 | InGameGroup의 ObjectDefinition SO | 위치·획득 여부·프리팹 |
| 스테이지별 정의 참조·좌표·장애물 회전·원본 ID·공개 대상 | InGameGroup의 스테이지 SO | 표현용으로 복제한 좌표 |
| 스테이지별 Terrain·Object 외형 회전 | ViewGroup의 StageEnvironmentVisual SO | 좌표·규칙 정의 SO 참조·장애물 회전 복제 |
| Terrain·Object의 이름·설명·아이콘·프리팹·표현 보정 | ViewGroup의 Visual SO | 규칙 수치·배치 좌표 |
| Obstacle 이름·아이콘·프리팹 | 기존 ViewGroup UnitVisualDataSO | 두 번째 Obstacle 외형 정의 |
| 실제 위치·생존·활성·진입 누적·보유 상태 | 기존 Buffer | SO에 기록한 가변 상태 |

새 스크립트는 기존 수동 아이템 관례인 Data/InGame/Table/<Terrain|Obstacle|Object>/Item과 Data/View/Table/<Terrain|Object>/Item에 둔다. Excel용 Core/Table/Item의 Master 행은 유지한다. 실제 새 에셋은 DataBaseSO/InGameGroup/<Terrain|Obstacle|Object>/Item과 ViewGroup/<TerrainVisual|ObjectVisual|UnitVisual>/Item에 둔다. 폴더 위치는 그룹 등록을 대신하지 않는다.

## 3. 식별과 정의

- TerrainContentKind는 SO의 전체 지형 종류를 구분하는 순수 키다. 기존 StaticTerrainKind가 토네이도·위치 보정을 포함하지 않아 SO용 키가 필요하다. 정적 다섯 종류는 명시적 변환으로 기존 enum을 재사용하며 Core enum은 수정하지 않는다.
- Obstacle은 기존 ObstacleContentKind와 그것에서 도출하는 EntityKey를 사용한다. 종류와 구조물 키를 따로 입력하지 않는다. 체력·점유 크기는 새 ObstacleDefinition에서만 작성하고 기존 Structure SO에 같은 값을 중복 작성하지 않는다.
- ObjectContentKind는 현재 서로 다른 세 계약 타입(Mine/Flag/SheriffBadge)의 SO 선택 키다. 변환 결과는 기존 InitialObjectPlacementSpec이다. Core 계약에 별도 enum을 추가하지 않는다.
- Terrain/Obstacle/Object의 규칙 Data는 Unity를 참조하지 않는 별도 파일의 불변 타입이다. SO.GetData는 매번 새 Data를 생성한다.
- View는 위 순수 키만 공유한다. InGame SO를 참조하거나 GetData로 규칙을 읽지 않는다. 미래 런타임 소비자는 Buffer가 제공하는 fact 종류·EntityKey로 외형을 선택해야 한다.
- 기존 UnitVisualDataSO가 이미 구조물 외형을 소유하므로 Obstacle용 Visual SO를 중복 생성하지 않는다. 화약통·보급품의 EntityKey로 기존 타입을 사용한다.

## 4. 설정 목록

| SO | 설정 |
|---|---|
| TerrainDefinitionDataSO | 종류; WeakGround의 entryThreshold; PresenceModifier의 attackBonus/maxHealthBonus; Tornado의 firstActivationAfterUnitTurns |
| ObstacleDefinitionDataSO | 종류; footprintWidth/Height; maxHealth; PowderBarrel의 explosionDamage; Supplies의 hitAttackBonus/destructionAttackBonus |
| ObjectDefinitionDataSO | 종류; Mine의 damage; Flag의 priorityBonus; SheriffBadge는 종류만 |
| TerrainVisualDataSO / ObjectVisualDataSO | 동일 종류 키; displayName; description; icon; prefab; localOffset; localScale |
| 기존 UnitVisualDataSO | Structure_PowderBarrel 또는 Structure_Supplies; 기존 이름·아이콘·프리팹 |

SO에서 다른 종류에만 적용되는 직렬화 값은 GetData 때 0으로 정규화한다. 해당 종류에서 사용되는 잘못된 값은 오류로 거절한다. 초기 상태로부터 계산할 수 있는 RemainingEntries, PendingCollapse, 현재 체력, 활성 상태는 종류 정의에 저장하지 않는다.

## 5. 스테이지 입력 후속 Phase

종류 정의와 위치 입력은 분리한다. 스테이지에서는 정의 SO 참조와 원본 ID·CellInput·Owner[]를 지정하고, Obstacle에는 Rotation4도 지정한다. GetData에서 SO 참조를 순수 정의 및 기존 Core 배치 계약으로 변환하며 Unity 객체를 런타임 Data에 남기지 않는다. 정의 값은 입력 SO 한 곳에서만 수정한다.

기존 staticTerrainPlacements와 initialWallCells의 직렬화 필드·GUID를 보존한다. 새 정의 참조 입력과 기존 인라인 입력을 함께 사용했을 때도 원본 ID·셀 공존 검증을 우회하지 않는다. Object 배열 순서가 기존 진입 순서를 결정한다.

2026-10-12 사용자 추가 승인: Inspector의 신규 환경 배치는 `Environment`로 통일한다. 기존 `initialWallCells`·`staticTerrainPlacements`는 필드 이름/타입/값과 변환을 유지하면서 기본 Inspector에서 숨기고 전용 Inspector의 `Legacy Environment (호환용)` 접힌 항목에서만 편집한다. 기존 값이 있으면 접힌 상태에도 그 사실을 표시한다. `cornerCutTicks`는 `Wall Settings`로 분리하며 새 Wall 정의와 기존 벽 모두에 적용한다. 자동 배치 이관·값 삭제·중복 우선순위 변경은 하지 않는다. 전용 Inspector는 Editor 조건으로만 컴파일하며 SerializedProperty를 사용해 Undo와 여러 에셋 편집을 유지한다.

2026-10-12 사용자 추가 결정: Terrain·Object의 회전은 외형용으로 분리한다. StageEnvironmentVisualDataSO에 StageRoot의 stageKey와 Terrain/Object의 sourceId별 Rotation4를 작성한다. 생략한 원본의 외형 회전은 R0이다. View 입력은 InGame SO를 참조하지 않고, 향후 Buffer의 EnvironmentSemanticBindings로 원본 분류를 대조한다. 런타임 외형 소비 연결은 이번 Phase 밖이다.

환경 입력 검증은 기존 StageEnvironmentAdapter.Read와 CellEnvironmentQuery.ValidateInitialPlacement를 호출하여 범위·원본 ID·공존·벽/철도 검사를 재사용한다. SO에서 만드는 장애물은 꽉 찬 직사각형이므로 90/270도에서는 폭·높이를 바꿔 전체 점유 셀을 검증 재료로 만든다. Core의 임의 모양 회전 계산을 복제하지 않으며 실제 점유는 기존 Core 준비가 결정한다. FieldDefinition이 없는 기존 미완성 SO는 GetData 시 범위/공존 검사를 보류하며 기존 CombatStageDetail.Validate의 필수 참조 검사는 유지한다. 기존 유닛·본부 배치와 환경의 최종 대조는 Core 준비 경계에서 수행한다.

토네이도 입력을 운영으로 전달하려면 CombatStageDetail의 순수 목록과 Core 소유 BattleDefinitionAdapter 전달 보완이 필요하다. 이는 다음 Phase의 경계 검토 대상이다. Core 파일에 닿는 변경은 임의 구현하지 않고 사용자 승인 범위 또는 Core 요청 문서로 처리한다.

## 6. SSOT·SOLID·FW 검토

- SSOT: 종류 수치는 InGame 정의, 좌표는 스테이지, 표현은 View, 실제 상태는 Buffer에만 둔다. View 외형 키는 규칙 정의를 참조하는 통로가 아니다.
- S: 정의 Data/SO, 스테이지 입력, Visual SO, 기존 Core 계산이 각각 책임을 유지한다.
- O: 기존 Core 계약을 변환 결과로 사용한다. 새 런타임 서비스·범용 등록 체계는 만들지 않는다.
- L: 초기 배치 생성은 기존 Core 계약의 유효성 조건을 만족해야 한다.
- I/D: View에 InGame Query나 정의 SO를 전달하지 않는다. SO 입력에는 IServiceModule이 필요하지 않다.
- FW-001~003: 새 서비스 등록·Builder 작업이 없으므로 해당 없음. FW-004: Buffer 상태를 직접 수정하지 않는다. FW-005~011 및 FW-013~016: 입력·씬·연출 수명 변경 없음. FW-012: 표현 자원을 SO에서 설정하는 것과 Render Query 등록 완료를 구분한다.
- JMH 지침 §23/26/27/28: 런타임 상태를 SO에 저장하지 않고, 새 Manager·범용 환경 서비스·추가 카메라 참조를 만들지 않는다.

## 7. 검증

정의→기존 Core 배치 계약 변환, 종류별 필수 값 거절, 표현 키 일치, 순수 Data의 Unity 참조 부재를 확인한다. SO 생성·직렬화·GetData 새 객체 생성은 Unity EditMode에서 확인한다. 새 파일과 .meta를 함께 작성한다. 기존 에셋 값은 Phase 1에서 변경하지 않는다.

Phase별 Task와 실행 증거의 원본은 [Task 계획](../plans/2026-10-12-environment-so-authoring-plan.md)이다.

## 8. Phase 3 설정용 에셋과 입력 방법

아래 에셋은 사용자 요청으로 만든 **설정용 초기 에셋**이다. Phase 3 생성 당시에는 Catalog·테이블·Query에 등록하지 않았다. 후속 View 연결에서 사용자가 DB 연결과 Cube 임시 외형을 승인하여 Object 외형 3개·Obstacle 외형 2개를 테이블에 등록했다. 현재 표현 연결 정책은 [Object·Obstacle View 설계 §4.1](2026-10-12-object-obstacle-view-design.md#41-사용자-추가-지시--cube-임시-외형과-items-등록)이 원본이다. 규칙 수치와 실제 스테이지 배치는 확정하지 않았다.

| 묶음 | InGameGroup 규칙 에셋 | ViewGroup 표현 에셋 |
|---|---|---|
| 정적 Terrain | `Terrain/Item/Terrain_HighGround`, `Terrain_Sandstorm`, `Terrain_Wall`, `Terrain_WeakGround`, `Terrain_Cliff` | `TerrainVisual/Item/TerrainVisual_` + 같은 종류 이름 |
| 토네이도·위치 보정 Terrain | `Terrain/Item/Terrain_Tornado`, `Terrain_PresenceModifier` | `TerrainVisual/Item/TerrainVisual_Tornado`, `TerrainVisual_PresenceModifier` |
| Obstacle | `Obstacle/Item/Obstacle_PowderBarrel`, `Obstacle_Supplies` | 기존 타입의 `UnitVisual/Item/UnitVisual_PowderBarrel`, `UnitVisual_Supplies` |
| Object | `Object/Item/Object_Mine`, `Object_Flag`, `Object_SheriffBadge` | `ObjectVisual/Item/ObjectVisual_` + 같은 종류 이름 |

경로 시작점은 `Assets/ProjectL/DataBaseSO/InGameGroup/` 및 `.../ViewGroup/`이고 확장자는 `.asset`이다. 규칙 12개·대응 표현 12개, 별도 `ViewGroup/StageEnvironmentVisual/Item/StageEnvironmentVisual_Template.asset` 1개로 총 25개다.

### 8.1 초기값의 출처와 입력 대기

| 설정 | 저장한 초기값 | 출처 / 후속 입력 |
|---|---|---|
| 약한 지반 진입 한도 | 3 | TerrainDefinitionDataSO의 현재 기본값. 기획 확정값이 아니다. |
| Tornado first activation / repeat interval | 3 / 3 completed unit turns | Both values are configurable under the SO's Tornado section. Defaults come from TornadoSourceFact.Interval; the timing follow-up preserves the captured instance interval in Core. |
| 위치 보정 공격력·최대 체력 | 0 / 0 | SO 기본값. 위치 보정 효과를 주려면 기획 값을 입력한다. |
| 장애물 크기·체력 | 1×1 / 20 | ObstacleDefinitionDataSO의 현재 기본값. 종류별 기획 값을 입력한다. |
| 화약통 폭발 피해 | 0 | SO 기본값. 기획 피해 입력 대기. |
| 보급품 피격·파괴 보상 | 0 / 0 | SO 기본값. 기획 보상 입력 대기. |
| 지뢰 피해·깃발 우선순위 | 0 / 0 | ObjectDefinitionDataSO 기본값. 기획 값 입력 대기. 배지는 종류 외 수치가 없다. |
| 외형 이름·오프셋·크기 | 한국어 종류 이름 / (0,0,0) / (1,1,1) | Terrain/Object 표현의 기본 보정. 설명은 비워 뒀다. |
| 프리팹·아이콘 | 미지정 | 대응하는 승인된 리소스를 찾지 못해 비워 뒀다. 임의 유닛·본부 프리팹을 대체 외형으로 연결하지 않았다. |
| 스테이지 외형 회전 템플릿 | stageKey 1, 회전 배열 비움 | 복제한 뒤 실제 StageRoot의 stageKey로 바꾸는 입력 시작점. 1은 실제 스테이지 연결 완료를 뜻하지 않는다. |

표는 Phase 3 생성 당시의 초기값이다. 후속 작업으로 Object·Obstacle에는 종류별 색상의 Default Cube 기반 임시 프리팹을 지정했다. 아이콘과 Terrain 외형은 계속 미지정이다. 종류와 무관한 직렬화 항목은 GetData에서 기존 규칙에 따라 0으로 정규화한다. **장애물 UnitVisualData는 프리팹이 필수**이며 미지정 상태에서 Validate가 실패하는 기존 계약은 유지한다.

### 8.2 실제 스테이지에 설정하는 순서

1. InGameGroup의 종류별 정의 에셋에서 필요한 수치를 입력한다. 같은 종류라도 스테이지별 수치가 다르면 정의 에셋을 복제하여 변형을 만든다. 원본 정의 하나를 여러 배치에서 참조할 수 있다.
2. 대상 CombatStageData의 `Environment > Terrain Placements`에 정적 지형·토네이도·위치 보정 정의를 연결한다. `Source Id`는 이 스테이지의 기존 인라인 지형·다른 Terrain·Object와 겹치지 않는 양수, `Cell`은 0부터 시작하는 필드 열(x)/행(y), `Known To`는 처음부터 공개할 Player/Enemy다. 빈 배열은 양쪽 모두 모르는 상태다.
3. `Obstacle Placements`에 화약통/보급품 정의·Anchor·Rotation을 입력한다. 회전은 0/90/180/270도이며 점유 영역에 반영된다. 장애물 개체 ID는 Core가 할당한다.
4. `Object Placements`에 지뢰/깃발/배지 정의·Source Id·Cell·Known To를 입력한다. 같은 칸의 Object 진입 순서는 배열 순서이며 ID 크기로 정렬하지 않는다.
5. ViewGroup의 대응 표현 에셋에 프리팹·아이콘·표시 이름을 지정한다. 장애물은 기존 UnitVisual 타입의 두 에셋을 사용한다. 위치·피해·체력을 표현 에셋에 다시 작성하지 않는다.
6. Terrain/Object 외형을 돌리려면 `StageEnvironmentVisual_Template`을 복제하고 실제 StageRoot와 같은 `Stage Key`를 넣는다. `Terrain Rotations` / `Object Rotations`에 해당 Source Id와 Rotation을 작성한다. 없는 원본은 R0이며 배치 좌표는 여기서 입력하지 않는다. 장애물 회전은 InGame 배치를 따른다.

필드 참조가 있으면 GetData에서 전체 환경 범위와 공존을 검사한다. 기존 인라인 정적 지형·벽은 `Legacy Environment (호환용)`에 남아 계속 처리된다. 새 배치는 `Environment`에서 작성하고 벽은 Terrain Placements에 Terrain_Wall 정의를 참조한다. `Wall Settings > Corner Cut Ticks`는 새 벽과 호환용 벽에 공통 적용한다. 호환용 입력과 새 정의 배치에 같은 원본/셀을 중복 작성하지 않는다. 이번 작업은 기존 스테이지의 배치 배열을 채우지 않았다.

### 8.3 운영 연결 경계

정의 SO는 CombatStageData의 참조로 순수 계약을 만들 수 있다. 그룹 폴더에 저장한 것만으로 DB 등록이 되지는 않는다. 입력 Phase 1~4에서는 DB 등록을 제외했고, 후속 사용자 승인으로 Object·Obstacle의 View DB와 표시 소비를 [별도 계획](../plans/2026-10-12-object-obstacle-view-plan.md) Phase 1~2에서 연결했다. 같은 계획 Phase 3에서 사용자 승인 확인용 배치를 전투 스테이지 11개와 실제 StageKey 회전 SO에 연결했다. Object/Obstacle 정의는 스테이지 입력 참조를 통해 Catalog 의존성에 포함되며 별도 InGame Query/Excel/Master 등록은 하지 않았다. 토네이도 운영 전달은 [Core 요청 §5.9](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#59-환경-so-배치-입력과-토네이도-목록-2026-10-12-사용자-지시-미전달) 대기다. Terrain 런타임 표시는 계속 보류하고 Unity 에셋/재생 확인은 별도 계획의 미완료 검증 항목을 따른다.
