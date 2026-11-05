# Object·Obstacle View 연결 설계

작성: 2026-10-12. 선행 입력 설계: [환경 SO 입력](2026-10-12-environment-so-authoring-design.md). 사용자 지시: Terrain은 표시 방식 검토를 보류하고 Object·Obstacle의 View 연결부터 진행한다. 후속 답변으로 ViewGroup 테이블·DB 등록까지 승인했다.

## 1. 범위와 현재 경로

Object는 Mine/Flag/SheriffBadge, Obstacle은 PowderBarrel/Supplies를 대상으로 한다. Terrain 프리팹 표시, 낭떠러지의 필드 형상 변경, Core 규칙, FieldCamera, Architecture, 공격/파괴 VFX는 범위 밖이다. InGame 정의 SO를 View가 읽거나 씬에서 직접 DB를 우회하는 경로를 만들지 않는다.

현재 CombatStateData는 Environment, EnvironmentBindings, Structures를 공개한다. ObjectSourceFact의 Id/Cell/Active/KnownTo가 Object 표시의 원본이다. 장애물은 이미 FieldUnitPresenter의 구조물 경로에서 EntityId별 재사용, 점유 중심 위치, Rotation, 체력, 죽은 개체 반환을 처리한다. 장애물 표시기를 중복 구현하지 않는다.

최초 조사 때 외형 SO의 프리팹은 비어 있었다. Phase 1 이후 사용자가 빈 프리팹에는 `Prefabs/Default/Cube`와 임시 색상을 적용하여 Items를 등록하도록 지시했다. 아래 §4.1의 임시 자원 정책을 적용한다. 실제 필드 소비 연결과 재생 확인은 별도 상태다.

## 2. 책임과 데이터 흐름

| 사실/역할 | 원본/담당 |
|---|---|
| Object 종류별 이름·아이콘·프리팹·위치/크기 보정 | ObjectVisualDataSO → ObjectVisualTable |
| 스테이지의 SourceId별 외형 회전 | StageEnvironmentVisualDataSO → StageEnvironmentVisualTable |
| Object 위치·활성·공개 여부·분류 | Combat Buffer의 Environment/EnvironmentBindings |
| Obstacle 위치·회전·체력·파괴 | Combat Buffer의 Structures |
| Obstacle 종류별 외형 | 기존 UnitVisualTable |
| 표시 시점과 값 전달 | 기존 CombatViewSystem → CombatSceneDisplay |
| Object 인스턴스 재사용·반환 | Phase 2의 Object 전용 Presenter |

ObjectVisualTable은 ObjectContentKind로, StageEnvironmentVisualTable은 StageRoot와 같은 StageKey로 조회한다. 기존 Query 관례처럼 Get은 누락 시 KeyNotFoundException, TryGet은 false를 반환한다. 중복 키, 잘못된 항목은 등록 때 거절한다. 입력 SO의 미완성 프리팹 허용은 유지하지만 운영 ObjectVisualTable에 등록한 항목은 프리팹이 필수다.

스테이지 회전 테이블에 대응 StageKey가 없으면 Object 회전은 R0을 사용한다. 대응 SO가 있으면 Object SourceId를 Buffer의 Object 분류와 대조한다. Terrain 회전 소비는 이번 범위 밖이며 Object 연결이 Terrain 회전 검사를 요구하지 않는다. 기존 SO 타입은 유지한다.

## 3. 표시 정책과 수명

Object는 Buffer에서 Object로 분류된 ObjectSourceFact 중 Active이며 KnownTo에 Player가 있는 원본만 표시한다. 종류는 MineSourceFact/FlagSourceFact/SheriffBadgeSourceFact의 계약으로 대응하고, 규칙 문자열이나 능력치로 추측하지 않는다. 모르는 종류는 명시적 오류로 처리한다. KnownTo가 빈 배치는 표시되지 않는다.

위치는 기존 FieldGridMapper의 CellCenterToWorld를 사용한다. LocalOffset은 필드 좌표축 기준의 표현 보정, LocalScale은 프리팹 원래 크기에 곱하는 배율이다. 외형 회전은 기존 RotationToWorld를 사용한다. 프리팹의 기본 로컬 회전도 보존한다. 한 칸의 Object 여러 개는 SourceId별로 유지하고 좌표를 식별자로 사용하지 않는다.

Install에서 준비, 결과 재생이 끝난 Refresh에서 최신 Buffer 사실로 갱신한다. 제거/비활성/비공개가 된 원본은 반환한다. 매 Refresh마다 전부 생성하지 않는다. Clear/Release는 기존 표시 정리 경계와 함께 호출하며 반복 정리가 안전해야 한다. 사건 도중 단계별 지뢰 폭발/수집 연출은 별도 확장이다.

### 3.1 Phase 2 구현 책임

`ObjectViewSet<TView>`는 Unity 객체를 모르는 SourceId별 연결이며 분류·활성·공개 여부, 종류 대응, 대여/반환을 처리한다. `FieldObjectPresenter`는 두 View Query와 기존 FieldGridMapper로 프리팹·변환·종류별 CustomPool을 관리한다. Object의 SourceId는 EntityId와 다른 식별자이므로 CoreEntityId나 유닛 표시 집합에 섞지 않는다.

CombatViewSystem.InitData가 두 Query를 받고 씬 표시 생성 시 전달한다. CombatSceneDisplay가 Install/Refresh에서 현재 Environment/EnvironmentBindings/StageKey를 Presenter에 넘긴다. 새 MonoBehaviour나 씬 검색·직렬화 참조를 추가하지 않고 기존 Field.CellsRoot 아래 `FieldObjects`를 처음 필요한 때 만든다. Clear는 비활성 반환 후 풀을 유지하고 Release는 전용 루트와 풀 참조까지 정리한다. 반환 실패가 생겨도 다른 인스턴스 반환을 시도하며 원래 표시 실패를 보존한다.

소비된 Object의 SourceId가 분류에 남아 있어도 현재 Environment에 없으면 표시하지 않는다. 회전 검증은 ObjectRotations만 분류와 대조하며 현재 존재까지 요구하지 않는다. LocalOffset은 CellCenterToLocal에 더한 뒤 LocalToWorld로 옮긴다. 위치·회전·크기가 같으면 Transform을 다시 쓰지 않는다. Mapper와 회전 데이터는 Render 호출 동안만 참조한다.

## 4. Phase 1 등록 정책

ObjectVisualDataTableSO·StageEnvironmentVisualDataTableSO, Query 2개, ServiceKeys, ViewDataBase, ViewDataBaseFactory를 연결한다. 두 빈 테이블 에셋을 ViewGroup에 등록한다. 기존 Catalog가 ViewGroup을 참조하므로 별도 Catalog 필드를 만들지 않는다.

Phase 1 최초 구현은 빈 테이블로 등록했고, 후속 사용자 지시로 ObjectVisual 3개·UnitVisual 장애물 2개에 §4.1의 임시 프리팹을 연결하여 Items에 등록한다. StageEnvironmentVisual_Template는 계속 미등록이며 임의의 StageKey=1을 실제 스테이지에 연결하지 않는다.

### 4.1 사용자 추가 지시 — Cube 임시 외형과 Items 등록

현재 다섯 SO에서 프리팹이 비어 있으면 `Assets/ProjectL/Prefabs/Default/Cube.prefab`를 자식으로 참조하는 종류별 임시 프리팹을 연결한다. 기존 Cube와 공용 머티리얼은 수정하지 않는다. 색은 새 URP Lit 머티리얼에 한 번만 저장하고, SO/Presenter에 같은 색 필드를 중복 추가하지 않는다. 런타임 Resources/AssetDatabase 검색이나 renderer.material 복제로 처리하지 않는다. 새 프리팹은 `Prefabs/Default/Environment`, 머티리얼은 그 아래 `Materials`에 둔다.

| 종류 | 임시 색상 |
|---|---|
| Mine | 빨강 |
| Flag | 파랑 |
| SheriffBadge | 노랑 |
| PowderBarrel | 주황 |
| Supplies | 초록 |

프리팹 루트는 기존 표시기가 위치/회전을 적용할 기준이고, Cube 자식은 높이 0.5로 두어 밑면이 필드 면에 닿게 한다. 프리팹 미지정 여부를 현재 에셋 입력에서 확인하여 채우며 이미 지정된 외형은 덮어쓰지 않는다. ObjectVisualDataTable에 세 Object, 기존 UnitVisualDataTable에 두 장애물을 추가하고 기존 Items를 유지한다. 기존 Query의 필수 프리팹 검증을 완화하지 않는다. 이후 최종 외형은 SO의 Prefab 참조를 교체한다. Terrain 임시 자원과 Phase 2 표시 소비는 이번 추가 지시 범위 밖이다.

### 4.2 Phase 3 — 실제 스테이지 키 연결

2026-10-12 사용자는 제안한 확인용 배치를 모든 전투 스테이지에 적용하도록 지시했다. 일반 A~J(1101~1110)와 Boss(2101)의 CombatStageData에 Object 세 개와 Obstacle 두 개를 넣는다. Object는 지뢰 ID 101/(1,2), 깃발 ID 102/(2,2), 배지 ID 103/(3,2)이며 Player·Enemy 모두에게 공개한다. 화약통은 (4,2)/R90, 보급품은 (5,2)/R0이다. 기존 규칙 정의 값과 승인된 임시 Cube 외형은 유지한다.

각 StageRoot와 같은 StageKey를 가진 `StageEnvironmentVisual_{Normal_A~J,Boss}.asset`을 만들고 StageEnvironmentVisualDataTable의 Items에 11개를 등록한다. Object 회전은 101/R0, 102/R90, 103/R180이며 좌표는 복제하지 않는다. SourceId는 스테이지 안에서만 유일하므로 StageKey별 SO에서 같은 세 ID를 재사용한다. 회전 템플릿은 계속 미등록이다. 장애물 회전은 InGame 배치에서만 작성한다. Boss의 미완성 Terrain/Obstacle/Object 입력 세 개는 사용자 답변에 따라 비우고 승인한 배치로 대체한다.

이 배치는 최종 레벨 디자인이 아니라 사용자가 승인한 표시 확인용 구성이다. 최종 프리팹·아이콘·규칙 수치 교체는 별도 작업이다. 실제 SO 내용은 Database 전투 정의에서 사용하며 Dev Fixture는 자체 Environment를 쓰므로 이 배치를 반영하지 않는다. 회전 SO가 등록된 StageKey를 Fixture의 서로 다른 SourceId 분류로 표시하면 기존 불일치 검증이 오류를 보고한다. 재생 확인은 `Play From Boot 설정 > Battle Definition = Database`와 전투 노드로 수행한다. 이번 Phase에서 Boot 씬이나 로컬 실행 설정을 바꾸지 않는다.

운영 스테이지가 정의 SO를 입력 참조로 사용하므로 Catalog의 에셋 의존성에는 Object/Obstacle 정의가 포함된다. 별도 InGame 환경 Query나 Master 테이블 등록은 추가하지 않는다. 입력 참조는 GetData에서 기존 순수 계약으로 바뀌고, View는 계속 Buffer와 View Query만 사용한다.

### 4.3 사용자 추가 지시 철회 — Object HUD 없음

2026-10-12 사용자가 Object HUD 롤백을 지시했다. Object UnitHUD 연결·Object 모드·이름/설명 프리팹 변경·공용 FieldHudAnchor 추출과 관련 테스트를 모두 되돌린다. 유닛/구조물의 기존 UnitHUD 구현은 이전 상태로 유지한다. Phase 1~3의 Object 외형·필드 표시·배치·회전 SO 연결은 유지하며 Object에는 HUD를 붙이지 않는다.

### 4.4 사용자 추가 지시 — Obstacle 체력 HUD

2026-10-12 사용자는 Obstacle에 UnitHUD를 붙이고 체력바와 체력 숫자만 표시하도록 지시했다. 기존 FieldUnitPresenter는 구조물마다 UnitHUD를 생성하지만 Owner.Enemy일 때만 표시한다. 기존 적 구조물 표시를 유지하면서 Buffer의 StructureCombatFacts.Definition.Key를 공용 ObstacleContentProfile.IsNativeObstacleKey 계약으로 분류한 장애물에도 표시한다. Owner.Neutral인 화약통/보급품을 포함하며 아군 본부의 기존 표시 정책은 유지한다.

HP 원본은 Buffer의 Health.CurrentHealth/MaxHealth이고 기존 UnitHUD.SetStructure의 체력 전용 모드를 사용한다. 코스트/버프/턴 순서는 숨긴다. 프리팹·높이·크기·카메라 방향과 풀 반환/사망 정리는 기존 경로를 재사용한다. Object HUD 철회는 유지하며 별도 표시기·서비스·규칙 수치·SO 필드는 만들지 않는다. SSOT/SOLID 및 JMH/FW 기준에 따라 기존 구조물 표시 책임 안에서 범위를 확장한다. 이번 추가 범위를 단일 Phase로 구현·검증·보고한다.

2026-10-12 후속 사용자 지시로 UnitHUD에 직렬화 `obstacleHealthColor`를 추가한다(기본 회색, 공용 HUD.prefab에도 회색 저장). SetStructure에서 Buffer의 구조물 키로 장애물 여부를 정하고 HP 갱신/미리보기에서도 장애물 색을 사용한다. SetUnit/Clear에서는 분류를 초기화해 풀 재사용 시 유닛·본부 색을 복원한다. 진영과 관계없이 장애물은 이 색을 우선 사용하며 체력 숫자 색은 변경하지 않는다. 색의 입력은 기존 HUD 프리팹 한 곳, 분류는 기존 계약, HP는 Buffer를 따른다. 별도 서비스·Core 변경은 없다.

후속 사용자 지시로 FieldUnitPresenter의 `Obstacle HUD` 영역에 `obstacleHudOffset`(월드 위치 보정)과 `obstacleHudScale`(월드 크기)을 따로 둔다. 초기값은 기존 표시와 같은 (0,1.5,0)/0.02이며 Combat 씬에도 저장한다. 외형 생성 시 같은 장애물 키 계약으로 설정을 선택하고 기존 AttachUnitHud/카메라 보정/종류별 풀을 재사용한다. 유닛과 일반 구조물은 기존 unitHudOffset/unitHudScale을 계속 사용한다. 공용 HUD 프리팹은 유지한다. 이 두 값은 서로 독립적으로 작성하는 표현 설정이며 좌표/규칙 상태를 복제하지 않는다. 별도 컴포넌트·서비스·Core 변경은 없다.

## 5. SSOT·SOLID·JMH/FW 점검

- SSOT: 규칙은 InGame/Core, 동적 사실은 Buffer, 외형 자원과 회전은 View SO에 둔다. 좌표·활성·공개 상태를 View SO에 복제하지 않는다. 장애물의 기존 구조물 표시와 EntityId를 재사용한다.
- SOLID: 테이블은 입력 변환과 키 조회, Presenter는 인스턴스 표시/수명, 조정자는 갱신 순서를 맡는다. 필요한 두 Query만 주입하고 기존 FieldUnitPresenter에 Object 분기를 추가하지 않는다.
- JMH 지침 §2·§23·§25~28: 생성·조립은 Factory, Query와 런타임 상태 분리, 씬 검색을 늘리지 않음, 불필요한 새 Manager/시스템/추상 계층 없음.
- FW-001/003: Query 서비스는 기존 Builder에 인터페이스로 등록, Build 1회 유지. 정적 Query는 수명 모듈이 아니므로 FW-002 AddModule 불필요.
- FW-004: Core 및 Buffer 소유 상태에 쓰지 않음. 입력 계약 FW-005~009를 변경하지 않음.
- FW-010~014: 기존 격자 변환과 풀 계약을 재사용하고 반환·씬 해제 수명을 검증한다. 렌더 서비스 자체를 새로 만들지 않는다.

## 6. 검증

Phase 1은 잘못된/중복 키, 누락 조회, 빈 테이블, 실제 ViewGroup 등록 및 DataBaseFactory 생성 회귀를 확인한다. 순수 관리 실행과 Unity AssetDatabase/SO 생성 검증을 구분한다. Phase 2는 SourceId 재사용, 비공개/비활성 원본 제거, 회전/위치, Clear/Release 및 기존 장애물 표시 회귀를 확인한다. 실제 프리팹/씬 재생 검증은 Phase 3이다. 커밋·push는 사용자가 직접 한다.
