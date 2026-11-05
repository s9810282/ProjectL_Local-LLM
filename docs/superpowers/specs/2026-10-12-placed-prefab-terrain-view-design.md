# PlacedPrefab Terrain View 연결 설계

> 후속 상태: 세 Phase 코드 연결 뒤 2026-10-12 Wall 확인용 한 칸을 추가했고, 2026-10-13 사용자 그림에 따라 일반 A~J·Boss 11곳을 공통 9×9 레벨로 갱신했다. Environment의 Wall은 12개(Source Id=201~212, Player·Enemy 공개, 기본 R0)이며 Object/Obstacle 5개를 빈칸으로 옮겼다. 좌표·검증·현재 상태는 [공통 배치 계획](../plans/2026-10-13-combat-common-layout-plan.md)을 따른다. 아래 조사 당시의 빈 배치 기록은 설계 시작 시점의 상태다.

작성: 2026-10-12, View 작업. 사용자 요청: PlacedPrefab Terrain 4종만 연결하고, 비어 있는 외형에는 기존 Default Cube와 임시 색상을 사용한다. 이번 요청의 실행 범위는 설계·Phase 계획 작성까지다. 구현 Task는 [별도 계획](../plans/2026-10-12-placed-prefab-terrain-view-plan.md)을 따른다.

선행 작업은 [환경 SO 입력 설계](2026-10-12-environment-so-authoring-design.md), [Object·Obstacle View 설계](2026-10-12-object-obstacle-view-design.md), TerrainVisualMode 추가 기록(Object·Obstacle 계획의 마지막 절)이다. 설계 기준은 [전투 View 인계 §4](../handoffs/2026-09-30-combat-view-design-session-handoff.md), JMH Framework 개발 지침, Framework 프로그래머 가이드다.

## 1. 범위와 조사 결과

대상은 Wall, Sandstorm, Tornado, PresenceModifier다. 외형 생성, 위치·회전·활성·공개 상태 반영, 인스턴스 재사용과 해제를 연결한다. HighGround, WeakGround, Cliff의 GroundShape 표시, 바닥 형상 변경, Terrain HUD, 토네이도 이동 보간/발동 VFX, Core 규칙 변경은 후속 범위다.

현재 TerrainVisualDataSO 7개에는 VisualMode가 저장되어 있고 위 4종은 PlacedPrefab이다. 네 Prefab은 모두 비어 있으며 TerrainVisual Table SO·Query·ViewGroup 등록은 없다. 기존 Object는 종류별 임시 Cube/머티리얼을 외형 SO에 직접 연결하고 운영 Query에서는 Prefab 누락을 거부한다. 같은 자원 입력 방식을 따른다.

운영 Core Adapter는 Terrain 배치를 Environment와 EnvironmentBindings.TerrainSourceIds로 전달한다. 최근 병합으로 TornadoPlacements 전달도 제공된다. 새로운 Core·Buffer 필드가 필요하지 않다. 현재 Combat_Normal_A~J·Combat_Boss 11개의 Environment/Terrain Placements는 비어 있고 StageEnvironmentVisual의 Terrain Rotations도 비어 있다. 배치를 작성하지 않은 스테이지에서는 표시가 없는 것이 정상이다.

## 2. 원본과 책임

| 사실 또는 책임 | 소유자 |
|---|---|
| 종류 키·VisualMode·프리팹·이름·아이콘·설명·LocalOffset/Scale | TerrainVisualDataSO → TerrainVisualTable |
| 위치·활성 상태·KnownTo·Source Id | Buffer의 MovementEnvironmentFacts.Tiles |
| Terrain/Object/Obstacle 분류 | Buffer의 EnvironmentSemanticBindings |
| 스테이지별 Terrain 외형 회전 | 기존 StageEnvironmentVisualDataSO의 TerrainRotations |
| 현재 StageKey와 표시 시점 | 기존 CombatViewSystem → CombatSceneDisplay |
| Source Id별 외형 연결/반환 | TerrainViewSet |
| Unity 인스턴스·격자 변환·종류별 풀·소유 루트 | FieldTerrainPresenter |
| 이동 차단·폭풍 효과·능력치 보정·토네이도 발동/이동 | 기존 Core |

```text
ViewGroup TerrainVisual + StageEnvironmentVisual
              ↓ InitData / Query
CombatViewSystem → CombatSceneDisplay → FieldTerrainPresenter
                       ↑                    ↓
              전달받은 CombatState     TerrainViewSet / CustomPool
              Environment + Bindings       ↓
                                   CellsRoot / FieldTerrains
```

View는 InGame 정의·배치 SO를 읽지 않는다. TerrainContentKind는 기존 공용 조회 키만 사용한다. Presenter는 Buffer 서비스에 직접 접근하지 않고 Display가 넘긴 불변 사실만 소비한다. GameBootstrap·새 MonoBehaviour·새 프레임 루프·Core 호출은 추가하지 않는다.

## 3. 네 종류의 원본 식별과 표시 정책

| 원본 형식 | 외형 조회 키 | 표시 |
|---|---|---|
| StaticTerrainFact, Kind=Wall | TerrainContentKind.Wall | 벽 프리팹 |
| StaticTerrainFact, Kind=Sandstorm | TerrainContentKind.Sandstorm | 모래폭풍 프리팹 |
| TornadoSourceFact | TerrainContentKind.Tornado | 현재 Cell에 토네이도 프리팹 |
| DestinationTileModifierFact | TerrainContentKind.PresenceModifier | 현재 Cell에 영역 표시 프리팹 |

TerrainSourceIds에 분류된 원본 중 Active=true이고 KnownTo에 Player가 있는 것만 표시한다. Enemy만 공개되었거나 공개 대상이 없는 원본은 표시하지 않는다. 비활성/비공개/삭제/분류에서 제외된 원본의 기존 View는 반환한다. 규칙 문자열 RuleKey로 종류를 추측하지 않는다.

StaticTerrainFact의 HighGround/WeakGround/Cliff는 이번 소비자가 명시적으로 건너뛴다. GroundShape SO 등록이나 Prefab을 요구하지 않는다. Terrain 분류에 ObjectSourceFact 등 지원하지 않는 형식이 들어오면 계약 오류로 처리한다. 네 지원 종류의 Query 누락도 설정 오류다. Query에서 읽은 VisualMode가 GroundShape이면 이번 Presenter는 표시하지 않는다. 모드 값을 종류에 따라 다시 계산하거나 강제로 바꾸지 않는다.

TerrainViewSet은 Source Id → 표시 인스턴스/종류 연결만 보관한다. 위치·능력치·발동 턴·KnownTo의 가변 사본을 보관하지 않는다. 같은 Source Id의 종류가 바뀌면 기존 종류 풀에 반환하고 새 종류 외형을 대여한다. 동일한 위치의 서로 다른 Source Id를 좌표로 합치지 않는다. 겹침 허용 여부는 Core의 기존 입력 검증을 따른다.

토네이도는 Refresh에서 최종 Cell을 적용한다. View에서 이동 방향, 발동 시점, 주기, 다음 위치를 계산하지 않는다. 상태가 갱신되어도 같은 Source Id의 인스턴스를 재사용한다. 기존 결과 재생 순서를 유지하며 이번에는 이동 보간이나 별도의 Coroutine을 도입하지 않는다.

## 4. View DB와 임시 외형

새 파일은 Data/View/Table/Terrain/TerrainVisualDataTableSO.cs와 Queries/ViewDataBase/ViewTable/TerrainVisualTable.cs다. 기존 TerrainVisualDataSO/불변 Data를 재사용한다. 테이블 이름은 향후 GroundShape 입력에도 사용할 수 있는 TerrainVisual로 유지한다.

ITerrainVisualTableQuery는 IServiceModule이며 GetTerrainVisual(kind), TryGetTerrainVisual(kind, out data)를 제공한다. Get 누락은 KeyNotFoundException, TryGet 누락은 false다. 생성자는 internal이며 IDataProvider<IDatabaseItem>을 받는다. 잘못된 Item/중복 종류/등록된 PlacedPrefab의 Prefab 누락은 DB 생성 때 거부한다. GroundShape는 향후 등록될 수 있지만 이 Phase에서는 3종을 Items에 넣지 않으며 그 Prefab을 필수로 만들지 않는다.

ViewTableKey.TerrainVisual, IViewQuery.TerrainVisualTableQuery, ViewDataBaseFactory 등록을 추가한다. 새 Query는 ExportServices로 제공하고 기존 Builder의 Build 전에 조립한다. Tick 수명이 없으므로 AddModule은 추가하지 않는다. ViewGroup.asset에 TerrainVisualDataTable.asset 하나를 등록하고 Items는 4개만 등록한다. IViewQuery 테스트 구현체도 필요한 프로퍼티를 보완한다.

빈 Prefab 처리도 Object의 기존 방식처럼 **임시 프리팹을 만들어 SO의 Prefab에 연결한 뒤 등록**한다. 이미 지정한 외형은 덮어쓰지 않는다. 런타임에서 경로 문자열/Resources.Load/GameObject.CreatePrimitive로 자원을 찾거나 생성하지 않는다. 이후 SO의 Prefab을 정식 에셋으로 교체하면 Presenter 변경 없이 적용된다. 등록 이후 다시 Prefab을 비운 항목은 기존 Object와 같은 설정 오류 정책을 따른다.

| 종류 | 새 Default/Environment 프리팹 | 임시 색 RGBA |
|---|---|---|
| Wall | Cube_Terrain_Wall | 회색 (0.45, 0.45, 0.45, 1) |
| Sandstorm | Cube_Terrain_Sandstorm | 모래색 (0.85, 0.65, 0.25, 1) |
| Tornado | Cube_Terrain_Tornado | 보라 (0.60, 0.30, 0.90, 1) |
| PresenceModifier | Cube_Terrain_PresenceModifier | 청록 (0.20, 0.80, 0.70, 1) |

기존 Prefabs/Default/Cube.prefab을 중첩 참조하는 종류별 래퍼와 머티리얼을 만든다. 기존 Cube와 Object/Obstacle 임시 머티리얼을 수정하지 않는다. 래퍼 루트 원점은 셀 바닥 중심, Cube 자식 중심은 y=0.5로 두어 바닥에 서게 한다. 크기와 위치는 기존 SO의 LocalScale/Offset으로 조정한다. 임시 머티리얼은 에셋으로 공유하고 Refresh마다 Material을 생성하거나 Renderer.material로 복제하지 않는다. 기존 칸 선택은 필드 평면을 사용하므로 Terrain Collider로 규칙·입력을 구현하지 않는다.

## 5. Presenter와 격자 배치

FieldTerrainPresenter는 plain C# sealed class다. 부모 Transform, ITerrainVisualTableQuery, 기존 IStageEnvironmentVisualTableQuery를 생성자로 받는다. 종류별 CustomPool을 소유하고 CellsRoot 아래 FieldTerrains 루트를 필요할 때 생성한다. FieldView의 셀 Renderer·강조 층에 Terrain 표시 책임을 넣지 않는다.

위치 = FieldGridMapper.LocalToWorld(CellCenterToLocal(source.Cell) + LocalOffset), 회전 = RotationToWorld(외형 회전) × 원본 Prefab 로컬 회전, 스케일 = Prefab 로컬 스케일 × LocalScale이다. Offset은 기존 Object와 같은 필드 로컬 축 기준이며 외형 회전에 따라 추가 회전시키지 않는다. FieldUnitScale을 Terrain에 적용하지 않는다. 모든 표현 보정은 기존 SO 값을 사용한다.

FieldGridMapper를 사용하므로 CellPrefab/GridOutline 두 필드 표시 모드에 같은 경로로 대응한다. 셀 오브젝트 존재 여부에 의존하지 않고 기본 바닥을 숨기거나 교체하지 않는다. Terrain은 EntityId나 FieldUnitView를 갖지 않고 HUD도 붙이지 않는다.

ObjectViewSet/FieldObjectPresenter의 수명 패턴은 참고하되 Object에 Terrain 타입 분기를 추가하지 않는다. 이번에는 Object의 공개 조건·표시 계약을 유지하고 TerrainViewSet으로 지원 종류/모드 필터를 분리한다. 공통 범용 환경 프레임워크나 Object·Obstacle의 전면 재구성은 만들지 않는다(JMH 과도한 추상화 금지). 상태 사실의 SSOT를 공유하는 것이 중요하며 각 Presenter의 인스턴스 연결은 표시용 캐시다.

## 6. 회전과 수명

StageKey로 기존 StageEnvironmentVisualTable을 조회하고 GetTerrainRotation(source.Id)를 사용한다. 해당 Stage SO가 없거나 원본별 회전 입력이 없으면 R0이다. SO에 저장된 TerrainRotations의 Source Id는 Buffer.TerrainSourceIds와 대조하며 불일치는 오류다. 아직 표시하지 않는 GroundShape의 유효한 Source Id가 회전 목록에 있어도 Prefab을 요구하지 않는다. ObjectRotations를 Terrain 소비자가 대신 적용하지 않는다. 레거시 WallCells도 Core가 이미 TerrainSourceIds/StaticTerrainFact로 변환하므로 별도 레거시 표시 경로를 만들지 않는다. 자동 생성된 레거시 ID의 외형 회전은 기본 R0을 쓰고 명시적인 회전 입력은 Environment Source Id를 권장한다.

CombatSceneDisplay는 기존 Object와 같은 경계에서 Terrain을 조립/Render/Clear/Release한다. Install 실패, Refresh 실패, 씬 Detach에서도 정리되는 경로를 연결한다. 오류가 난 Terrain 표시기는 자신의 일부 갱신된 표시를 모두 반환하고 기존 오류 경계로 예외를 전달한다. 새 오류 처리 System을 만들지 않는다.

Clear는 활성 연결을 반환하고 풀/전용 루트는 유지한다. Release는 반복 호출해도 안전하며 활성 인스턴스 반환을 시도한 뒤 비활성 인스턴스까지 포함한 전용 루트를 파괴하고 풀 참조를 비운다. 다른 필드 자식·Object·유닛·셀은 파괴하지 않는다. 반환 하나가 실패해도 나머지를 반환하고 복수 오류는 AggregateException으로 보존한다. 해제 뒤 Render는 InvalidOperationException이다.

## 7. 구조·계약 점검

| 기준 | 적용 |
|---|---|
| SSOT/P5~P7 | 런타임 위치/활성/공개는 Buffer, 표현 모드/자원은 Terrain SO, 회전은 기존 StageEnvironment SO. 다른 데이터에서 규칙을 계산하지 않음 |
| SOLID S/D | Query는 조회/입력 검증, ViewSet은 연결 수명, Presenter는 Unity 배치, Display는 값 전달. 서비스 인터페이스를 생성자로 주입 |
| JMH §23/§27/§28/§33 | Query와 전투 상태 분리, 기존 조립점 조사, 새 책임을 전용 표시기에 배치, 범용 System/Factory 추가 없음 |
| FW-001/003 | Query를 제공 인터페이스로 등록, Build 전에 조립 |
| FW-002 | Query에 프레임 수명이 없으므로 AddModule 없음 |
| FW-004/013~016 | 기존 공개 계약과 CustomPool 경로 사용, Combat Attach/Detach 수명으로 해제, 이벤트 구독/별도 루프 없음 |
| 경계 | Core·Buffer·FieldCamera·Architecture·GameBootstrap·기존 Missing Script 에셋 변경 없음 |

ServiceKeys/ViewDataBaseFactory/IViewQuery의 변경은 Phase 1 완료 때 Framework/View 작업자용 공유 메모를 작성한다. Core에 수정 요청이 필요해지면 구현을 멈추고 기존 데이터 경계 handoff에 요청을 기록한다. 이번 조사에서는 Core 변경 없이 연결 가능하다.

## 8. 검증과 스테이지 입력 경계

Phase별 Query/표시 수명 테스트와 실제 SO 교차 검증을 수행한다. 핵심은 공개 여부, GroundShape 제외, Source Id 재사용, 토네이도 위치 변경, 삭제/반환, StageKey별 회전 기본값, 실패 정리, 두 필드 모드, 실제 Cube/머티리얼 참조다. Object와 Obstacle 표시/Obstacle HUD가 유지되는지도 관련 테스트로 확인한다.

현재 운영 스테이지의 Terrain 배치는 비어 있으므로 View 연결만으로 4종이 화면에 등장하지 않는다. 통합 검증은 테스트에서 임시 SO/배치를 구성해 운영 데이터 변환 → 준비된 Buffer → 실제 Combat 표시 경로로 검사한다. 이 검증을 위해 사용자 레벨 SO에 임의 좌표·Source Id·회전·KnownTo를 저장하지 않는다. 실제 스테이지 배치 작업은 사용자 값 또는 별도 승인된 테스트 배치가 있을 때 후속으로 진행한다. 예전 Object 11개 스테이지 배치 승인을 새로운 Terrain 배치 승인으로 확대하지 않는다.

Unity에서의 확인은 Database 모드가 기준이다. Fixture의 Environment는 별도 데이터이므로 운영 Terrain SO 전달의 증거로 사용하지 않는다. 실제 배치를 작성할 때는 CombatStageData.Environment.Terrain Placements에 정의/Source Id/Cell/KnownTo(Player 포함)를, 같은 StageKey의 StageEnvironmentVisual SO에 해당 Source Id별 Terrain Rotations를 작성한다. 회전은 생략 가능하다.

Unity 에디터 컴파일과 EditMode DataBaseAssetTests/환경 에셋/Presenter 통합 검증이 기준이다. 외부 컴파일·순수 관리 실행·YAML/GUID 점검은 실행 가능한 보조 증거로 기록하며 Unity 네이티브 실행과 구분한다. 각 Phase 결과를 보고한 뒤 멈춘다.
