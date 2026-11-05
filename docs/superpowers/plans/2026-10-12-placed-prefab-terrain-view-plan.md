# PlacedPrefab Terrain View 연결 Task 계획

설계: [PlacedPrefab Terrain View 연결](../specs/2026-10-12-placed-prefab-terrain-view-design.md). 대상은 Wall/Sandstorm/Tornado/PresenceModifier 4종이다. 설계·계획 작성 후 사용자가 Phase 1을 승인했다. 각 Phase 완료 후 결과를 보고하고 멈춘다. 커밋·push는 사용자가 한다.

## Phase 0 — 조사·설계 (완료)

- [x] T0.1 TerrainVisualMode/외형 SO 7종과 현재 등록 상태 확인.
- [x] T0.2 네 종류의 Buffer 원본, TerrainSourceIds/공개 상태, 토네이도 운영 전달 확인.
- [x] T0.3 Object/Obstacle 표시, CustomPool, FieldGridMapper/두 필드 모드, 회전/해제 경계 조사.
- [x] T0.4 표시·DB·자원·수명 책임 및 SSOT/SOLID/JMH/FW 계약 설계.
- [x] T0.5 다음 3개 구현 Phase와 검증/실제 레벨 입력 경계 작성.

## Phase 1 — Terrain View DB와 Cube 임시 외형

목표: 네 외형이 운영 View DB에서 조회되고, 빈 Prefab에 기존 Default Cube 기반의 종류별 임시 자원이 연결된다. 이 Phase만으로 필드에 Terrain이 표시되지는 않는다.

- [x] T1.1 `Data/View/Table/Terrain/TerrainVisualDataTableSO.cs`와 `Queries/ViewDataBase/ViewTable/TerrainVisualTable.cs`/meta 작성. Get/TryGet, 타입/중복/PlacedPrefab 자원 검증.
- [x] T1.2 `DataBaseGroupServiceKey.cs`의 ViewTableKey, `ViewDataBase.cs`의 IViewQuery/구현, `ViewDataBaseFactory.cs` 등록 연결. 영향을 받는 테스트 Query 구현체 보완.
- [x] T1.3 `DataBaseSO/ViewGroup/TerrainVisual/TerrainVisualDataTable.asset`/meta 생성, ViewGroup.asset에 등록. Items는 4종만 작성, GroundShape 3종은 등록하지 않음.
- [x] T1.4 `Prefabs/Default/Environment/Cube_Terrain_{Wall,Sandstorm,Tornado,PresenceModifier}.prefab`와 종류별 머티리얼/meta 생성. 기존 Cube를 중첩 참조하고 색/바닥 원점 적용.
- [x] T1.5 네 TerrainVisual SO의 빈 Prefab만 임시 외형으로 연결. 이름/모드/기존 보정 유지, 지정된 사용자 프리팹은 유지.
- [x] T1.6 Query 테스트와 실제 Catalog/외형 에셋 검증 갱신. 기존 EnvironmentAuthoringAssetTests의 'Terrain 외형 전부 미등록' 기대를 'PlacedPrefab 4종 등록/GroundShape 3종 미등록'으로 변경. InGame Terrain 정의는 아직 스테이지가 참조하지 않는 현 상태 유지.
- [x] T1.7 컴파일, 실행 가능한 Query 테스트, GUID/중첩 프리팹/머티리얼 참조/UTF-8 LF/diff 검사. DB 에셋 변경에 필요한 DataBaseAssetTests 실행 여부를 별도로 기록.
- [x] T1.8 공용 DB 조립 변경 공유 메모 작성(준비만, 외부 발송 없음). 완료 보고 후 멈춤.
- [ ] T1.U Unity EditMode `DataBaseAssetTests`·환경 에셋 테스트 실행 및 Table Items/Prefab/색 Inspector 확인.

리뷰 지점: ViewGroup의 새 Table Items가 정확히 4개이고 임시 Cube가 네 가지 색으로 준비됐는지 확인한다. Query 프리팹 누락 정책은 기존 Object와 동일하다.

## Phase 2 — 네 Terrain의 실제 필드 표시

목표: Buffer의 네 종류를 Terrain 전용 Presenter로 표시하며 같은 Source Id는 재사용한다. 회전은 이 Phase에서 기본 R0만 사용하고 스테이지별 회전 소비는 Phase 3에서 연결한다.

- [x] T2.1 `Combat/Display/TerrainViewSet.cs`/meta 작성. 지원 원본 → 종류 키 해석, TerrainSourceIds/Active/KnownTo(Player) 필터, GroundShape 원본 제외, VisualMode 필터, Source Id별 종류 교체/삭제/반환/실패 정리.
- [x] T2.2 `Combat/Display/FieldTerrainPresenter.cs`/meta 작성. 부모/Query 주입, `FieldTerrains` 소유 루트, 종류별 CustomPool, FieldGridMapper/Offset/Scale/원본 회전과 기본 R0 적용.
- [x] T2.3 `CombatViewSystem.InitData`에서 Terrain Query 확보/Attach 의존성 확인, `CombatSceneDisplay` 생성자/Install/Refresh/Clear/Release에 전달·호출 연결. 관련 테스트의 Display 생성자 호출 보완.
- [x] T2.4 정적 지형·토네이도·위치 보정 모두 1개 경로로 표시. 토네이도는 최신 Buffer Cell로 갱신하고 같은 Source Id 인스턴스 유지. 발동 턴이나 다음 위치 계산 없음.
- [x] T2.5 표시 수명에 의미 있는 순수 테스트 작성: 네 원본 종류, 비공개/비활성/삭제/분류 변경, 같은 ID 재사용, 종류 변경, GroundShape가 누락된 Query를 요구하지 않음, 잘못된 형식·자원 실패 시 정리.
- [x] T2.6 Unity Presenter 테스트 작성: 변환된 CellsRoot, Offset/Scale/Prefab 기본 회전, 토네이도 위치 갱신, Clear 재사용, Release 반복/소유 루트만 파괴, CellPrefab/GridOutline 모드 표시.
- [x] T2.7 ObjectViewSet/Object 표시·Obstacle HUD의 관련 회귀 검증(관리 실행·네이티브 컴파일 확인, Unity 실행은 T2.U). Object/Obstacle의 수명·공개 규칙은 변경하지 않음.
- [x] T2.8 컴파일/실행 가능한 관리 검증/변경 범위 diff 점검, Unity 실행 대기 구분, 완료 보고 후 멈춤.
- [ ] T2.U Unity Test Runner의 FieldTerrainPresenter/Combat 표시 테스트와 두 필드 모드 실행 확인.

리뷰 지점: 바닥을 유지한 채 네 Terrain이 기본 방향으로 표시되고, 토네이도 위치 갱신/반환/씬 해제가 정상인지 확인한다. HUD·Terrain 전용 씬 컴포넌트를 추가하지 않는다.

## Phase 3 — 스테이지 외형 회전·통합 검증

목표: 기존 StageKey/Source Id 회전 SO를 실제 Terrain 표시까지 소비하고, 운영 변환 경로와 기존 표시의 공존을 검증한다.

- [x] T3.1 FieldTerrainPresenter에 기존 IStageEnvironmentVisualTableQuery 주입. StageKey → TerrainRotation(Source Id), 누락 시 R0, 필드 회전/Prefab 기본 회전과 조합.
- [x] T3.2 Terrain 회전 ID를 Buffer.TerrainSourceIds와 검증. 유효한 GroundShape 회전은 표시하지 않아도 허용. Object 회전과 혼용하지 않음. Stage 변경·입력 누락 때 이전 회전 잔류 방지.
- [x] T3.3 회전 테스트: R0/R90/R180/R270, StageKey별 입력, 항목/테이블 누락, 분류 불일치, 이동한 토네이도의 Source Id 회전 유지.
- [x] T3.4 임시 SO/테스트 배치로 운영 SO 데이터 변환 → 전투 준비 Buffer → 실제 Combat 표시 통합 검증. Core 파일을 고치지 않고 공개 Adapter/준비 계약을 쓰는 통합 테스트 작성·컴파일 완료. 실제 실행은 T3.U 대기.
- [ ] T3.5 실제 Catalog의 네 종류 Prefab/머티리얼, Object·Obstacle과 동시 표시, 두 FieldDisplayMode, Clear/Release/오류 경계를 검증. 관리 105건·정적 에셋 참조 점검 완료. DataBaseAssetTests/관련 환경·표시의 Unity 실행은 T3.U와 함께 대기.
- [x] T3.6 사용자용 SO 입력·Database 실행 확인 절차 작성. 현재 운영 11개 스테이지의 비어 있는 Terrain 배치는 임의로 채우지 않음.
- [x] T3.7 컴파일·실행 가능한 검증과 Unity 네이티브/재생 증거 구분, 최종 결과·후속 작업 기록, 완료 보고 후 멈춤.
- [ ] T3.U Unity Test Runner 통합 검증. 사용자 작성 또는 별도 승인된 배치가 생기면 실제 Play에서 4종/회전/토네이도 위치 갱신 확인.

리뷰 지점: SO 회전이 같은 StageKey/Source Id의 외형에만 적용되고, 다른 Stage나 누락된 입력에서는 R0으로 돌아오는지 확인한다.

## 후속 레벨 입력과 보류 범위

실제 스테이지 위치/Source Id/KnownTo/회전은 사용자 레벨 값 또는 별도 승인된 테스트 구성으로 작성한다. 이번 설계·구현 Task에는 운영 스테이지의 새 Terrain 배치 입력이 포함되지 않는다. 전체 View 연결을 위해 Core 규칙 수정이나 운영 위치 값을 임의로 만들 필요는 없다.

GroundShape 3종, 바닥 교체/구멍/높이, WeakGround → Cliff 상태 표현, Terrain HUD, 토네이도 보간/발동 VFX는 후속 설계다. 설계와 다른 자원/등록/수명 결정이 필요하면 구현 전에 사용자에게 묻는다.

## 실행 기록

2026-10-12 Phase 0 완료. 기존 작업 트리에는 직전 TerrainVisualMode 추가/7개 외형 SO 분류 변경과 FMOD 캐시 변경이 있다. 이번 단계에서는 위 두 문서만 새로 작성했다. Core/Buffer/API/외형/배치/씬/기존 Object·Obstacle 코드는 수정하지 않았다. 코드 구현·새 테스트 실행·커밋·push 없음. 다음 시작점은 Phase 1이다.

### 2026-10-12 Phase 1 — 코드·에셋 연결 완료, Unity 확인 대기

사용자 지시로 T1.1~T1.8을 구현했다. TerrainVisual Table SO/Query, ViewTableKey 및 IViewQuery/ViewDataBaseFactory 조립을 추가하고 테스트 Query Stub 2곳을 보완했다. 기존 ViewGroup에 새 TerrainVisualDataTable.asset을 등록하고 Items는 Wall/Sandstorm/Tornado/PresenceModifier 네 개만 넣었다. 등록된 PlacedPrefab은 Prefab 필수, 잘못된 Item/중복 종류는 거부하고 GroundShape의 자원 입력에는 이를 강제하지 않는다. Get/TryGet 및 ExportServices 계약을 기존 View Query와 맞췄다.

네 SO의 빈 Prefab에 Default Cube를 중첩 참조하는 Cube_Terrain_* 래퍼/종류별 머티리얼을 연결했다. 벽 회색/모래폭풍 모래색/토네이도 보라/위치 보정 청록을 적용하고 Cube 자식의 y=0.5로 바닥 원점을 유지했다. 기존 Cube·Object/Obstacle 프리팹/머티리얼은 수정하지 않았다. 위치/규칙 상태를 Query에 넣거나 런타임 fallback 자원 탐색을 만들지 않았다. 실제 Terrain 필드 표시와 외형 회전 소비는 다음 Phase다.

TerrainVisualDatabaseTests에서 Query 누락/잘못된 Item/키 등록, 종류별 조회/중복/모드별 자원 요구, 실제 Catalog의 4개 외형/색/중첩 Cube/기본 바닥 원점을 검사하도록 준비했다. EnvironmentAuthoringAssetTests는 PlacedPrefab 4개 등록/GroundShape 3개 미등록 기대를 반영했다. 공용 변경 공유 메모는 handoffs/2026-10-12-terrain-view-db-framework-share.md이며 외부 발송하지 않았다.

검증: Unity 참조 DLL 기반 외부 C# 컴파일에서 Systems/Framework 테스트 어셈블리 오류 0. Terrain Query 관리 검증 5건 + 기존 Object·Obstacle DB 관리 회귀 7건, 총 12/12 통과. 실제 파일에서 네 SO/Table/중첩 Cube/머티리얼/색의 참조, 신규 GUID 12개 유일성, UTF-8/LF와 이번 범위 diff를 확인했다. 사용자의 Object_Mine/Object_Flag 값 변경, 기존 Default/임시 자원과 FMOD 캐시 해시는 작업 시작 전과 동일하다. 전체 diff check의 두 Object SO m_EditorClassIdentifier 공백은 기존 사용자 변경이며 이번 작업에서 고치지 않았다.

Unity Test Runner 연결이 없어 DataBaseAssetTests·EnvironmentAuthoringAssetTests 및 새 UnityEditorNative 7건은 컴파일까지만 확인했다. T1.U는 실제 에셋 로드/Inspector 검증 대기다. 관리 검증은 네이티브 실행 증거가 아니다. 증거: Temp/TerrainViewPhase1/ProjectL.Systems-compile.log, ProjectL.Tests.Framework-compile.log, managed-checks.log, asset-manifest.json.

SSOT는 기존 View SO/Buffer 경계를 유지하며 새 Query는 조회/입력 검증만 담당한다(SOLID). FW-001/003 조립 계약과 JMH 기존 확장 경로를 따르고 새 수명 모듈/Bootstrap을 추가하지 않았다. Core·Buffer·InGame SO·FieldCamera·Architecture·씬·기존 Missing Script 에셋 변경 없음. 커밋·push 없음. Phase 1 코드 결과를 보고하고 멈추며 다음 시작점은 Phase 2다.

### 2026-10-12 Phase 2 — 필드 표시 코드 완료, Unity 확인 대기

사용자 지시로 T2.1~T2.8을 구현했다. TerrainViewSet은 TerrainSourceIds로 분류된 원본에서 StaticTerrainFact의 Wall/Sandstorm, TornadoSourceFact, DestinationTileModifierFact를 네 외형 키로 해석한다. Active 및 KnownTo(Player), View SO의 PlacedPrefab 모드만 표시하며 HighGround/WeakGround/Cliff는 Query 호출 없이 건너뛴다. Object 등 잘못 분류된 원본은 오류로 전달한다. Source Id별 외형을 유지하고 종류가 바뀌면 원래 종류 풀에 반환한다. 비활성·비공개·삭제·분류 제외된 원본도 반환한다.

FieldTerrainPresenter는 CellsRoot 아래 FieldTerrains 전용 루트와 종류별 CustomPool을 소유한다. FieldGridMapper에 최신 Cell/필드 축 Offset을 적용하고 원본 Prefab 스케일 × LocalScale, 기본 R0 × 원본 회전을 사용한다. 토네이도는 최종 Cell로 위치만 갱신하며 같은 Source Id 인스턴스를 재사용한다. 주기·다음 위치·규칙을 계산하지 않는다. Clear는 풀을 유지하고 Release는 전용 루트만 파괴한다. 표시·반환 실패에서는 남은 자원 정리를 시도하고 원본 오류와 정리 오류를 보존한다.

CombatViewSystem.InitData/Attach에서 Terrain Query를 확보·주입하고 CombatSceneDisplay의 Install/Refresh/Clear/Release에 연결했다. Install 실패에서는 사건/포인터 연결과 Object/Terrain 활성 연결을 모두 정리하며 정리 실패로 원본 오류를 덮지 않는다. 기존 Display 테스트 생성자 네 곳에 필수 Query를 전달했다. 별도 시스템·MonoBehaviour·Bootstrap·HUD·씬 참조를 추가하지 않았다.

검증: Unity 참조 DLL 기반 외부 C# 컴파일에서 Systems 및 Framework 테스트 어셈블리 오류 0, 이전 Phase 대비 새 경고 0. TerrainViewSet 22건 + 기존 ObjectViewSet 13건 + Terrain Query 5건 + Object·Obstacle DB 7건, 관리 검증 47/47 통과. 네 종류 식별, 같은 ID/같은 칸의 별도 ID, 토네이도 갱신, 공개·활성·삭제·분류·모드 변경, 종류 교체, GroundShape 조회 제외, 대여/표시/조회/반환 실패를 검사했다.

Unity 네이티브 테스트는 FieldTerrainPresenterTests 7건(변환된 부모·Offset/Scale/기본 회전, 이동 재사용, Clear/Release, 모드 전환, 좌표/외형/프리팹 실패)과 CombatSceneEventPresenterTests의 두 FieldDisplayMode 통합 2건을 추가했다. 실제 Catalog 외형·Object 공존·유닛 체력 유지·토네이도 위치와 공개 변경·Install 실패 정리·재설치·Release를 검사한다. 기존 Object 표시 및 NeutralObstacle_ShowsOnlyHealthHud_UpdatesDamage_AndReusesAfterDeath 회귀도 컴파일됐다. 현재 Unity Test Runner 실행 연결이 없어 네이티브 9건과 기존 Object/Obstacle HUD 실행은 T2.U에 남겼다. 관리 실행은 Unity 네이티브·Play 증거가 아니다.

증거: Temp/TerrainViewPhase2의 ProjectL.Systems-compile.log, ProjectL.Tests.Framework-compile.log, managed-checks.log, scope.json, scope-validation.log. 신규 4개 C#의 meta/GUID 유일성·UTF-8 LF와 이번 범위 diff를 확인했다. 기존 Object/Obstacle 표시 코드, 사용자 Object_Mine/Object_Flag SO, Combat.unity 해시는 시작 전과 동일하다. 외부 컴파일은 Unity가 생성한 프로젝트의 이전 Phase 응답 파일에 현재 신규 파일을 추가해 실행했다.

SSOT는 런타임 사실=Buffer/외형=View SO 경계를 유지한다. 원본 연결·Unity 배치·서비스 주입을 각 책임으로 분리하고 SOLID/JMH/FW의 기존 Query·풀·씬 수명 경로를 사용했다. Core·Buffer·InGame SO·FieldView·FieldCamera·Architecture·운영 배치·기존 Missing Script 에셋 수정 없음. 커밋·push 없음. Phase 2 리뷰 지점에서 멈추며 다음은 Phase 3의 StageKey/Source Id 외형 회전 소비다. 현재 운영 11개 스테이지 Terrain Placements가 비어 있으므로 이번 코드만으로 네 종류가 Play에 나타나지는 않는다.

### 2026-10-12 Phase 3 — 회전 연결 코드 완료, Unity 통합 실행 대기

사용자 지시로 스테이지 SO의 Terrain 회전을 실제 표시까지 연결했다. FieldTerrainPresenter에 기존 IStageEnvironmentVisualTableQuery를 필수 주입하고 Render에 StageKey를 전달한다. 해당 Stage의 GetTerrainRotation(Source Id)을 필드 방향과 원본 Prefab 로컬 회전에 곱하며 SO/항목이 없거나 StageKey=0이면 R0이다. Mapper/회전 참조는 Render 호출 중에만 유지하고 finally에서 해제하므로 다른 Stage·누락 입력에 이전 회전이 남지 않는다. CombatSceneDisplay에서 기존 StageKey/회전 Query를 넘기며 CombatDisplayValues의 설명을 보완했다.

Terrain 회전 목록은 TerrainSourceIds만 대조한다. Object 회전은 Object 소비자의 기존 검증에 맡기고, 분류에 없는 Terrain 회전 ID는 오류다. 유효한 GroundShape 원본 회전은 허용하지만 Visual Query/Prefab을 요구하거나 바닥을 바꾸지 않는다. 잘못된 회전 입력에서도 활성 Terrain을 반환하며 다음 Render에서 풀을 다시 쓸 수 있다.

TerrainRotationBindingTests의 11개 관리 검증을 추가했다. 네 회전/누락 ID 기본값, Object·Obstacle·미분류 ID 거부, Object/Terrain 회전 분리, 누락 Stage/빈 목록/null Bindings를 검사한다. FieldTerrainPresenterTests에 회전 합성 4건, Stage 변경·회전 항목/Stage 누락·StageKey 0, GroundShape 회전 허용, 잘못된 회전 분류 후 정리 테스트를 추가하고 기존 토네이도 이동 테스트에 R270 유지 검증을 넣었다. 외형 회전에 따라 필드 축 Offset을 돌리지 않는 것도 대조한다.

TerrainOperationalDisplayTests 2건은 운영 Catalog·Group·Stage Table·Normal A StageRoot/CombatStage·해당 회전 SO만 메모리에서 복제한다. 테스트에서 Source Id 201~205/행 3에 네 PlacedPrefab과 Cliff를 쓰고 기존 Object 3개·Obstacle 2개를 유지한다. 실제 DataBaseFactory 및 기본 DatabaseBattleInitializationSource가 포함된 Core Factory에서 로스터/선택을 준비한 뒤 공개 PrepareBattle로 Buffer에 반영하고 실제 CombatSceneDisplay를 설치한다. Terrain 원본 ID/좌표/공개·토네이도 SO의 초기/반복 간격, 외형 회전 4종, GroundShape 제외, Object 회전·Obstacle 체력 숫자 HUD 공존, 두 FieldDisplayMode, 재사용/Clear/Release를 검사한다. 운영 SO를 저장하지 않으며 Core/Buffer 파일도 고치지 않는다. 임시 입력은 테스트 내부에서만 존재한다.

검증: Unity 참조 DLL 기반 외부 C# 컴파일에서 Systems 및 Framework 테스트 어셈블리 오류 0, Phase 2 대비 새 경고 0. TerrainRotationBindingTests 11 + TerrainViewSetTests 22 + ObjectViewSetTests 13 + Terrain Query 5 + Object·Obstacle DB 7 + 기존 StageEnvironmentAdapterTests 25 + EnvironmentStageTests 22, 관리 105/105 통과. 실제 YAML에서 네 외형 Table/Prefab/Default Cube 중첩·바닥 원점/머티리얼·종류별 색/GUID 및 ViewGroup 등록을 다시 확인했다.

Unity Test Runner 실행 연결이 없어 새 네이티브 9건(기존 Presenter 추가 7 + 운영 SO 통합 2)과 갱신된 토네이도 회전 검증은 컴파일만 확인했다. DataBaseAssetTests·환경 에셋·실제 표시·기존 Object/Obstacle HUD 회귀·Play 실행은 T3.5/T3.U와 기존 T1.U/T2.U에 남는다. T3.4의 체크는 통합 테스트 작성·컴파일 완료이며 운영 SO → 실제 화면 성공의 실행 증거는 아직 아니다. 증거: Temp/TerrainViewPhase3의 두 compile.log, managed-checks.log, asset-validation.log, protected-hashes.json, scope.json, scope-validation.log.

사용자 입력·실행 가이드: [PlacedPrefab Terrain 입력·Unity 확인](../viewDocs/placed-prefab-terrain-guide.md). 위치/공개=InGame Environment, 외형=TerrainVisual, 회전=같은 StageKey의 TerrainRotations로 구분했다. Play From Boot의 Database/Live/개발 진행 설정과 노드 번호≠StageKey, FieldTerrains 인스턴스와 회전 확인, 두 필드 모드 및 Test Runner 절차를 적었다. DB 맵은 무작위 구성도 있으므로 노드 2가 항상 Normal A를 뜻한다고 안내하지 않는다.

SSOT/SOLID/JMH/FW 점검: 위치·활성·공개는 Buffer, 외형은 View Terrain SO, 방향은 기존 StageEnvironment SO 한 곳에서만 가져온다. Presenter는 View 인터페이스를 주입받고 전투 계산/새 시스템·별도 프레임 루프·Bootstrap·수명 계약을 추가하지 않았다. 기존 Source Id/풀/씬 수명과 오류 경계를 유지한다. 운영 Stage/회전 SO·사용자 Mine/Flag 값·FMOD 캐시·기존 Object/Obstacle 표시 코드·Combat 씬의 시작 전 해시를 보존했고 Core·Buffer·FieldView·FieldCamera·Architecture·기존 Missing Script 에셋 수정 없음. 커밋·push 없음.

세 Phase의 코드 연결은 완료됐다. 운영 11개 스테이지의 Terrain Placements와 Terrain Rotations는 여전히 비어 있으며 임의 배치는 저장하지 않았다. 사용자의 실제 레벨 입력과 Unity 검증이 다음 리뷰 항목이다. 여기서 구현을 멈춘다.

### 2026-10-12 후속 — 전 스테이지 Wall 확인용 배치

사용자가 Terrain Wall만 모든 스테이지에 확인용으로 배치하도록 명시적으로 요청했다. 기존 설계의 사용자 승인 후 레벨 입력 조건을 만족하므로 일반 A~J(StageKey 1101~1110)·Boss(2101) 11개 Combat SO의 Environment > Terrain Placements에 Wall 1개씩 추가했다. 정의는 기존 Terrain_Wall.asset, Source Id=201, Cell=(0,2), KnownTo=Player·Enemy다. 확인용 한 칸은 모든 필드에서 배치 구역 사이의 행이며 기존 Object/Obstacle/본부와 겹치지 않는다. 회전 입력은 생략해 기존 R0 기본값을 사용하고 StageEnvironmentVisual SO는 수정하지 않았다. 외형은 등록된 회색 Default Cube다.

- [x] TW1 11개 필드·기존 환경/개체/본부 배치와 ID 확인 후 Environment에 Wall 추가.
- [x] TW2 기존 DataBaseAssetTests의 모든 스테이지 운영 정의 검증을 Wall ID/종류/위치/활성/공개 및 Terrain 회전 분류/R0 기대에 맞춤. EnvironmentAuthoringAssetTests의 실제 Catalog 의존성에서 Wall 정의만 등록된 것으로 갱신.
- [x] TW3 YAML/정적 교차 검증에서 11곳의 필드 범위·중앙 행·ID 유일성·기존 Object/회전된 Obstacle 점유/본부와 비겹침 확인. Wall 블록을 되돌린 문자열이 시작 전 내용과 같은지 검사해 다른 필드 보존을 확인. 사용자 Mine/Flag SO·씬·11개 회전 SO·템플릿 해시 보존.
- [x] TW4 외부 Framework 테스트 어셈블리 컴파일 오류 0·새 경고 0. 입력 가이드 및 설계의 후속 상태 메모 갱신, 범위 diff/UTF-8 LF 점검.
- [ ] TW.U Unity Test Runner DataBaseAssetTests·EnvironmentAuthoringAssetTests 실행 및 Database/Live Play에서 각 스테이지의 회색 Wall 확인.

Unity Test Runner 실행 연결이 없어 네이티브 에셋 로드/운영 준비 검증은 컴파일까지만 확인했다. 정적 검증은 Unity 재생 증거가 아니다. 증거는 Temp/TerrainWallPlacement의 asset-validation.log, before-stage-inputs.json, protected-hashes.json, tests-compile.log, scope-validation.log다. 다른 Terrain·기존 Object/Obstacle·Core/Buffer/표시 코드·카메라·씬 변경 없음. 커밋·push 없음. 이 확인용 배치는 향후 실제 레벨 제작 때 사용자가 조정/제거한다.
