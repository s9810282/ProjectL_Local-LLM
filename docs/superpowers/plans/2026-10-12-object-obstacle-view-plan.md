# Object·Obstacle View 연결 Task 계획

설계: [Object·Obstacle View 연결](../specs/2026-10-12-object-obstacle-view-design.md). 사용자 승인: Terrain 제외, Object/Obstacle부터, View DB 연결 포함. 큰 작업의 Phase별 완료 후 멈춤은 AGENTS.md를 따른다.

## Phase 0 — 조사·설계

- [x] V0.1 Buffer의 Object 원본/분류와 구조물 표시 경로 조사.
- [x] V0.2 View DB 연결 범위 확대 사용자 확인.
- [x] V0.3 책임·가시성·수명·미완성 자원 등록 정책 및 SSOT/SOLID/JMH/FW 설계.

## Phase 1 — View DB 조회 경로

- [x] V1.1 ObjectVisual/StageEnvironmentVisual Table SO와 Query 작성.
- [x] V1.2 서비스 키·ViewDataBase·Factory 연결. 기존 IViewQuery 테스트 구현 보완.
- [x] V1.3 빈 테이블 에셋 2개와 .meta 작성, ViewGroup 등록. 미지정 프리팹·회전 템플릿 Items 등록은 보류.
- [x] V1.4 조회/중복/자원 검증 및 실제 에셋·DB 회귀 검증 준비.
- [x] V1.5 컴파일·실행 가능한 검증, Unity 대기 구분, 완료 보고 후 멈춤.
- [ ] V1.U Unity에서 ObjectObstacleViewDatabaseTests의 네이티브 6건·DataBaseAssetTests·EnvironmentAuthoringAssetTests 실행 및 테이블 Inspector 확인.

## Phase 1A — 사용자 추가 지시: Cube 임시 외형·Items 등록

- [x] V1A.1 기본 Cube를 참조하는 임시 프리팹·종류별 색상 머티리얼 5쌍과 .meta 작성.
- [x] V1A.2 빈 외형 SO 5개에 연결, ObjectVisual 3개·UnitVisual 장애물 2개 Items 등록.
- [x] V1A.3 기존 에셋/DB 검증을 새 등록 정책으로 갱신하고 컴파일·정적 에셋 연결 확인. Unity 검증 대기 구분.
- [x] V1A.4 결과 보고 후 멈춤. Phase 2 표시 구현은 후속 지시를 따른다.

## Phase 2 — 필드 표시 연결

- [x] V2.1 Object SourceId별 인스턴스 재사용/반환 Presenter.
- [x] V2.2 Buffer의 Object 분류·원본을 전달하고 Player 공개/활성 상태 반영.
- [x] V2.3 StageKey별 Object 회전과 FieldGridMapper 위치/외형 보정 적용.
- [x] V2.4 CombatSceneDisplay Install/Refresh/Clear/Release 연결 및 장애물 기존 경로 유지.
- [x] V2.5 관리 회귀 실행·네이티브 검증 코드 준비, 완료 보고 후 멈춤.
- [ ] V2.U Unity에서 FieldObjectPresenterTests 5건, CombatSceneEventPresenterTests의 ObjectFacts 테스트 1건 및 기존 Combat 표시/입력 회귀 실행. 재생 화면 확인은 V3.4와 함께 진행.

## Phase 3 — 자원·운영 등록과 Unity 확인

- [x] V3.1 사용자가 승인한 종류별 Cube 임시 외형 5개를 유지. 최종 프리팹·아이콘 교체는 자원 제공 뒤 별도 작업.
- [x] V3.2 실제 StageKey/SourceId 회전 에셋 연결. 외형 Items의 임시 등록은 Phase 1A에서 수행.
- [x] V3.3 사용자 승인 확인용 배치를 전투 스테이지 11개에 연결. 규칙 수치는 유지.
- [ ] V3.4 DataBaseAssetTests·관련 EditMode 테스트, 생성/회전/비공개/소멸/재진입 재생 확인.
- [x] V3.5 에셋 연결 완료 상태와 Unity 미확인 항목 보고 후 멈춤.

### Phase 3 입력 확정 — 2026-10-12 사용자 답변

사용자가 확인용 배치 제안을 **모든 전투 스테이지에 적용**하도록 지시했다. 일반 A~J(StageKey 1101~1110)와 Boss(2101)에 같은 다섯 배치를 넣는다. 지뢰 ID 101/(1,2)/외형 R0, 깃발 ID 102/(2,2)/외형 R90, 배지 ID 103/(3,2)/외형 R180이며 Player·Enemy 모두에게 공개한다. 화약통 (4,2)/R90, 보급품 (5,2)/R0는 규칙 배치에 회전을 둔다. 규칙 수치는 기존 정의 SO 값, 표현은 승인된 Cube를 유지한다. 각 실제 StageKey의 회전 SO를 따로 만들어 View 테이블에 등록한다. SourceId는 스테이지 안에서만 유일하므로 서로 다른 StageKey에서 재사용한다.

Boss에 저장된 SourceId=0 Terrain 및 정의가 없는 Obstacle/Object 세 미완성 항목은 사용자 답변으로 비우고 위 확인용 배치로 대체한다. Terrain 배치를 새로 넣지는 않는다. 상점/이벤트에는 전투 필드 배치가 없다. 이번 승인 값은 최종 레벨 디자인과 구분한다.

## 사용자 추가 Phase — Object UnitHUD 철회

사용자의 Object HUD 롤백 지시로 OH1~OH4 결과를 되돌렸다. OH.U는 철회되어 남은 검증 목록에서 제외한다. Object 필드 표시와 스테이지 배치/회전 SO에 대한 기존 V1.U·V2.U·V3.4는 유지한다. 현재 상태는 설계 §4.3과 아래 롤백 기록을 따른다.

## 사용자 추가 Phase — Obstacle 체력 HUD

- [x] BH1 장애물 구조물을 기존 UnitHUD 체력 전용 표시 대상에 포함.
- [x] BH2 화약통/보급품의 체력 갱신·풀 반환/재사용, 기존 구조물·유닛 모드 회귀 검증 준비.
- [x] BH3 컴파일·관리 회귀 확인 및 Unity 대기 구분 후 보고/멈춤.
- [ ] BH.U Unity에서 UnitHUDTests·CombatSceneEventPresenterTests 및 실제 장애물 체력바/숫자 재생 확인.

후속 사용자 지시: `obstacleHealthColor` 기본 회색 및 프리팹 연결, 장애물 분류/풀 재사용 색 복원, 기존 테스트 보완·컴파일을 같은 HUD 범위에서 수행한다. 실제 화면 색 확인은 BH.U에서 함께 진행한다.

후속 사용자 지시: FieldUnitPresenter의 장애물 전용 Offset/Scale 입력과 Combat 씬 값을 연결하고, 기존 두 장애물 테스트에서 유닛 설정과 분리/풀 재사용을 대조한다. 컴파일 후 같은 HUD 추가 Phase로 보고한다. 실제 화면은 BH.U에서 확인한다.

## 실행 기록

### 2026-10-12 시작

현재 Object/Obstacle 외형 프리팹은 미지정이다. 장애물은 이미 구조물 표시 경로가 있고 외형 테이블 등록이 남아 있다. Object는 새로운 표시 소비자가 필요하다. Phase 1은 조회/조립까지이며 필드 생성 완료로 보고하지 않는다. 기존 환경 입력 Phase 1~4의 검증 상태와 미전달 Core 토네이도 요청은 유지한다.

### 2026-10-12 Phase 1 — 코드·조회 경로 완료, Unity 에셋 검증 대기

사용자가 View DB 연결까지 포함하도록 승인했다. ObjectVisualDataTableSO·StageEnvironmentVisualDataTableSO와 두 Query를 작성하고 기존 ServiceKeys/ViewDataBase/ViewDataBaseFactory에 등록했다. IViewQuery가 늘어나 기존 ArtifactPopupConfigTests·UnityStageInputTests의 가짜 Query에도 두 프로퍼티를 추가했다.

ObjectVisualTable은 Kind, StageEnvironmentVisualTable은 StageKey로 조회한다. Get 누락은 KeyNotFoundException, TryGet 누락은 false/null이다. 중복 키·null·잘못된 항목을 거절하며, 운영 Object 외형 등록에는 프리팹을 요구한다. 기존 ObjectVisualDataSO의 미완성 입력 허용은 유지했다. Builder의 1회 Build와 인터페이스 등록을 유지한다.

ViewGroup/ObjectVisual/ObjectVisualDataTable.asset 및 ViewGroup/StageEnvironmentVisual/StageEnvironmentVisualDataTable.asset을 빈 Items로 만들고 ViewGroup.asset에 테이블 참조만 추가했다. Catalog의 기존 ViewGroup 참조를 사용한다. Object 외형 3개, 장애물 외형 2개와 StageEnvironmentVisual_Template의 Items 등록은 아직 하지 않았다. 실제 외형이나 StageKey를 추측하지 않았다.

**검증:** ProjectL.Tests.Framework MSBuild 오류 0, 변경 소스의 새 경고 0. 기존 무관한 CS0649/CS8632는 유지됐다. 생성 csproj에 이미 들어간 Editor 소스를 추가 대상에서 제거하여 임시 중복 소스 경고를 정리했다. csproj 자체는 편집하지 않았다. 관리 테스트는 ObjectObstacleViewDatabaseTests 7/7와 기존 환경 회귀 80/80, 합계 87/87 통과했다. 신규 7건은 null/잘못된 입력·빈 테이블 누락 조회·두 서비스 키 등록 검증이다. 네이티브 6건은 Object 세 종류 등록/중복, 미지정 프리팹 거절, 스테이지 회전 조회/중복, 실제 Catalog 생성/두 테이블 연결 검증이며 컴파일까지만 확인했다.

새 파일/에셋 .meta, Assets 내 새 GUID 중복 없음, 테이블의 스크립트 GUID 일치, ViewGroup 등록, 빈 Items, UTF-8/LF 및 git diff --check를 확인했다. 관리 실행 결과를 Unity Test Runner/AssetDatabase 실행으로 대신하지 않는다. V1.U가 남아 있다. 증거는 무시되는 Temp/EnvironmentSOAuthoring의 build-object-obstacle-view-phase1.log와 managed-object-obstacle-view-phase1-checks.log다.

**다음:** Phase 2의 Object Presenter 및 Install/Refresh/Clear/Release 소비 연결. 장애물은 기존 FieldUnitPresenter 구조물 경로를 유지한다. 실제 프리팹 등록·배치 재생 확인은 Phase 3이다. 이번 Phase에서 Core·Buffer·FieldCamera·Architecture·씬·InGame 에셋은 수정하지 않았다. 커밋·push는 하지 않았다.

AGENTS.md의 Phase 리뷰 지점에 따라 Phase 1 완료 보고 뒤 멈춘다.

### 2026-10-12 Phase 1A — 임시 외형·Items 등록 완료, Unity 확인 대기

사용자가 프리팹이 비어 있으면 Prefab/Default의 Cube와 임시 색상을 쓰고 Items를 등록하도록 명시적으로 지시했다. 이전 미등록 정책을 설계 §4.1로 갱신했다. 현재 다섯 외형 SO의 Prefab이 비어 있음을 확인한 뒤에만 채웠다. 런타임 자동 검색/필수 프리팹 검증 완화 대신 입력 에셋에 실제 임시 프리팹 참조를 지정했다.

`Assets/ProjectL/Prefabs/Default/Environment/Cube_{Mine,Flag,SheriffBadge,PowderBarrel,Supplies}.prefab`과 `Materials/Cube_*.mat`을 만들었다. 각 프리팹은 기존 Default/Cube를 자식으로 참조하고 머티리얼만 종류별로 재정의한다. Cube 자식의 y=0.5로 밑면을 필드 기준면에 맞췄다. 색은 지뢰 빨강, 깃발 파랑, 배지 노랑, 화약통 주황, 보급품 초록이다. 공용 Cube·공용 머티리얼 원본은 수정하지 않았다. 새 에셋 10개와 두 폴더의 .meta를 함께 작성했다. 색 원본은 각 머티리얼이며 SO/표시 코드의 중복 색 필드는 없다.

ObjectVisualDataTable에 Object 외형 세 개를 등록했다. 기존 UnitVisualDataTable의 9개를 유지하면서 화약통·보급품 두 개를 추가해 11개가 됐다. InGame 규칙 SO와 Terrain 외형, 스테이지 회전 템플릿은 계속 미등록이다. 규칙 수치·실제 스테이지 배치·아이콘은 변경하지 않았다. Object 필드 소비 연결은 Phase 2에 남아 있다.

**검증:** SO → 임시 프리팹의 실제 GameObject fileID → 기본 Cube/개별 머티리얼 참조, 다섯 색의 구분, 새 GUID 중복 없음, 각 Item 단일 등록, 기존 UnitVisual Items 유지, UTF-8/LF와 git diff --check 통과. EnvironmentAuthoringAssetTests의 Catalog 경계와 ObjectObstacleViewDatabaseTests의 실제 Catalog 검증을 새 등록 정책에 맞게 갱신했다. 프로젝트 NUnit에서 지원하지 않는 Count 표현을 기존 Count()/Is.EqualTo 형식으로 수정했다. 최종 MSBuild 오류 0, 변경 파일의 새 경고 0. 관리 실행 회귀 87/87 통과. 증거는 Temp/EnvironmentSOAuthoring/build-object-obstacle-view-phase1a.log 및 managed-object-obstacle-view-phase1a-checks.log다.

**Unity 대기:** V1.U의 DataBaseAssetTests·EnvironmentAuthoringAssetTests·ObjectObstacleViewDatabaseTests를 Unity Test Runner에서 실행해야 한다. Cube 중첩 프리팹/머티리얼의 AssetDatabase 실제 로드와 화면 색상은 아직 확인하지 않았다. 정적 YAML 참조 검증·관리 실행을 네이티브 검증 완료로 보고하지 않는다. Core·Buffer·FieldCamera·Architecture·씬·런타임 표시 코드는 수정하지 않았다. 기존 FMOD 캐시 변경은 건드리지 않았다. 커밋·push 없음. 이번 사용자 추가 범위 완료를 보고하고 멈춘다.

### 2026-10-12 Phase 2 — Object 필드 표시 코드 완료, Unity 검증 대기

사용자가 Phase 2를 지시했다. ObjectViewSet과 FieldObjectPresenter로 SourceId별 재사용/종류별 풀을 분리하고 CombatViewSystem.InitData → CombatSceneDisplay 생성 → Install/Refresh/Clear/Release에 연결했다. 지뢰·깃발·배지를 현재 Buffer의 Object 분류와 타입 계약으로 대응하며 Active이고 KnownTo에 Player가 있을 때만 표시한다. 동일 칸의 여러 원본도 별도 표시한다. 소비되어 분류에만 남은 ID는 재생성하지 않는다.

FieldGridMapper로 칸 중심과 필드 축 기준 LocalOffset, 기본 프리팹 회전×Object 회전, 기본 크기×LocalScale을 적용한다. StageKey 또는 SourceId 회전이 없으면 R0이다. 대응 회전 SO의 Object 목록만 Buffer 분류와 대조하고 Terrain 목록은 소비하지 않는다. Object SO에서 규칙/현재 위치를 읽지 않는다. 정리 및 실패 시 반환을 수행하고 최종 Release는 소유한 FieldObjects 루트까지 없앤다. 장애물은 기존 FieldUnitPresenter의 구조물 표시 경로를 유지했다.

**구조 점검:** 동적 상태 SSOT는 기존 Buffer, 외형/회전 SSOT는 View DB다. Object의 SourceId와 유닛/장애물의 EntityId를 섞지 않았다. 순수 연결 집합·Unity 표시·기존 조정자의 책임을 나누고 필요한 두 Query만 주입했다. CustomPool·격자 변환·기존 InitData 조립을 재사용하며 새로운 Framework 서비스/Manager, MonoBehaviour, 씬 검색/참조를 만들지 않았다. Core·Buffer·InGame 에셋·FieldCamera·Architecture·씬은 수정하지 않았다.

**검증:** 최종 ProjectL.Tests.Framework MSBuild 오류 0, 변경 파일의 새 경고 0. 새 ObjectViewSetTests 13/13과 이전 환경/DB 회귀 87/87로 관리 실행 100/100 통과. 재사용/새 위치, 같은 칸의 여러 ID, 동적 추가, 비활성/비공개/제거, 분류와 타입 불일치, 종류 교체, 세 종류 계약, 대여/표시/반환 실패 정리, Terrain 회전 무관성과 Object 회전 분류 검증을 포함한다. Unity Transform/풀/소유 루트 테스트 5건과 실제 Combat 씬·Catalog의 세 임시 Cube로 Install/Refresh/Clear/재설치/Release를 검증하는 1건을 추가했으며 컴파일까지만 확인했다. 기존 씬 테스트 생성자 4곳의 두 Query 주입을 보완했다.

새 .cs 네 개의 .meta, GUID 중복, UTF-8/LF, git diff --check를 확인한다. 증거는 Temp/EnvironmentSOAuthoring/build-object-obstacle-view-phase2.log 및 managed-object-obstacle-view-phase2-checks.log다. Unity Test Runner/재생 검증은 V2.U·V3.4에 남으며 관리 실행 결과로 대체하지 않는다. 실제 스테이지 배치와 회전 에셋 등록은 Phase 3 범위라 임의 값을 채우지 않았다. KnownTo에 Player가 없는 원본은 화면에 나오지 않는다. 커밋·push 없음. AGENTS.md의 Phase 리뷰 지점에 따라 이번 코드 완료를 보고하고 멈춘다.

### 2026-10-12 Phase 3 — 전투 11개 배치·회전 SO 연결 완료, Unity 확인 대기

사용자 답변으로 확정한 Phase 3 입력을 일반 A~J와 Boss에 적용했다. CombatStageData 11개에 Object 세 배치와 Obstacle 두 배치를 넣고, StageEnvironmentVisual SO 11개/.meta를 만들어 회전 테이블 Items에 등록했다. 기존 StageRoot/CombatStage/Field 참조·규칙 수치·경제·적 구성·본부·철도·씬은 유지한다. Boss의 SourceId=0 Terrain·빈 Object/Obstacle 세 항목만 승인에 따라 교체했다. Terrain 배열은 비어 있으며 회전 템플릿도 계속 미등록이다. 종류별 Cube 외형은 계속 사용한다.

EnvironmentAuthoringAssetTests의 Catalog 의존성 검증은 운영 스테이지가 Object/Obstacle 정의 SO를 참조하는 현재 상태로 갱신했다. ObjectObstacleViewDatabaseTests는 회전 Items 11개와 실제 StageKey별 세 회전을 검증한다. DataBaseAssetTests에는 실제 Catalog → BattleDefinitionAdapter의 운영 정의 → Object 위치/공개/진입 순서·Obstacle 위치/회전 → View 회전 분류 대조를 전투 11개에 수행하는 네이티브 검증을 추가했다. Core 소스는 수정하지 않았다.

**검증:** ProjectL.Tests.Framework MSBuild 오류 0, 변경 테스트의 새 경고 0. 관리 회귀 100/100 통과. 별도 스크래치 검증은 실제 YAML의 정의/Field/StageRoot/회전 테이블 GUID 참조, SourceId/좌표/공개 enum 값, StageKey와 회전 목록을 읽어 순수 정의·배치 계약을 만들고 기존 CombatStageEnvironmentAuthoringValidation 및 FieldObjectPresenter.ValidateObjectRotations에 전달했다. 11/11 통과했으며 필드 범위·공존·ID 분류 검사를 포함한다. 새 회전 에셋/메타의 GUID 유일성·스크립트 참조·UTF-8/LF, git diff --check를 확인했다. 증거는 Temp/EnvironmentSOAuthoring/build-object-obstacle-view-phase3.log, managed-object-obstacle-view-phase3-checks.log, phase3-asset-checks.log다.

**남은 확인:** V1.U·V2.U·V3.4의 Unity Test Runner/AssetDatabase 실제 로드와 재생 화면 확인은 미실행이다. 현재 도구에 Unity 에디터 제어·테스트 실행 연결이 없고 열려 있는 프로젝트를 별도 Unity 프로세스로 중복 실행하지 않았다. MSBuild와 YAML/관리 계약 검증을 네이티브 실행으로 보고하지 않는다. `Battle Definition = Database`에서 노드에 진입해야 이 SO를 사용한다. Fixture는 자체 환경이라 배치를 반영하지 않으며 동일 StageKey의 회전 SourceId와 불일치하면 명시적 검증 오류가 난다. Boot/EditorPrefs는 변경하지 않았다.

전체 Phase 3 검증 완료가 아니라 **사용자가 요청한 스테이지 배치·회전 SO 연결 완료** 상태다. 최종 자원·수치는 제공 전까지 임시 설정을 유지한다. 커밋·push 없음. Phase 리뷰 지점에서 보고하고 멈춘다.

### 2026-10-12 Object UnitHUD — 구현 이력, 후속 사용자 지시로 롤백

사용자가 Object에도 UnitHUD를 붙이도록 지시했다. 표시 선호를 질문한 뒤 HP/Cost/Turn 계약이 없는 현행 Object에서 구현할 수 있는 이름·설명을 기본안으로 적용했다(설계 §4.3). Core의 수치를 임의로 만들지 않았다. FieldHudAnchor에 기존 FieldUnitView의 Canvas 생성/카메라 방향/크기 보정을 옮겨 공유하고, FieldUnitPresenter의 기존 프리팹·Offset·Scale·Camera 설정을 부착 함수로 주입했다. 새 MonoBehaviour는 표시 대상 자식 HUD만 관리하며 Framework 서비스나 전투 상태를 갖지 않는다.

FieldObjectPresenter는 종류별 외형의 DisplayName/Icon/Description을 UnitHUD Object 모드에 전달한다. HP/Cost/Turn을 숨기고 이름과 기존 UIInputBinding 설명 아이콘을 표시한다. 아이콘이 없으면 기존 아이콘 템플릿을 사용한다. 설명이 없으면 설명 패널에 이름을 표시한다. 공용 HUD.prefab에 ObjectName과 DescriptionPanel을 추가해 참조를 연결했고 기존 유닛/구조물 HP/Cost 참조와 프리팹 GUID는 유지했다. 이름/설명 값은 View SO 한 곳에서 작성하며 좌표/활성/공개 상태는 기존 Buffer 계약을 따른다.

같은 SourceId는 HUD를 재사용하고, 반환 시 설명 선택/수치를 Clear하고 다음 대여에 Object 모드를 다시 적용한다. Object SourceId를 EntityId로 바꾸거나 FieldUnitView를 Object에 붙이지 않는다. HUD 최초 활성화 뒤 내용을 적용하여 Awake의 포인터 초기화가 설명 아이콘 입력을 지우지 않도록 했다. HUD 부착 실패 시 새 외형을 제거한다. 기존 Clear/Release 경계는 유지한다.

**검증:** ProjectL.Tests.Framework MSBuild 오류 0, 변경 소스의 새 경고 0. 관리 환경/DB/SourceId 회귀 100/100 통과. UnitHUDTests에 Object의 이름/설명·숫자 영역 비활성·설명 선택 정리·유닛 모드 복원 테스트를 추가했고 기존 빌보드 테스트를 공용 Anchor 호출로 갱신했다. FieldObjectPresenterTests에는 실제 공용 프리팹 로드, 카메라/위치/스케일, Object의 FieldUnitView 없음, Clear/풀 재사용/Release 검증 1건을 추가했다. 두 신규 테스트와 기존 HUD/씬 테스트는 컴파일까지만 확인했고 Unity 네이티브 실행은 OH.U에 남는다. 관리 실행 100건은 새 HUD 네이티브 테스트의 실행 증거가 아니다.

공용 HUD 프리팹의 42개 로컬 fileID 유일성·참조 유효성, 신규 이름/설명 필드 연결, 새 Anchor .meta GUID 유일성·UTF-8/LF, 이번 HUD 범위 git diff --check를 확인했다. 전체 diff check의 기존 Obstacle_PowderBarrel/Supplies m_EditorClassIdentifier 공백은 이번 작업과 무관한 사용자 에셋 변경이라 건드리지 않았다. 증거는 Temp/EnvironmentSOAuthoring/build-object-hud.log, managed-object-hud-checks.log, object-hud-scope-diff-check.log다.

Core·Buffer·InGame 입력 에셋·FieldCamera·Architecture·씬·FMOD 캐시는 수정하지 않았다. 커밋·push 없음. 이번 사용자 추가 Phase의 코드/프리팹 연결 결과를 보고하고 멈춘다.

### 2026-10-12 Object HUD 롤백 — 사용자 지시

사용자가 Object HUD만 롤백하도록 지시했다. UnitHUD Object 모드와 HUD.prefab의 ObjectName/DescriptionPanel 추가, 공용 FieldHudAnchor 추출/부착 함수와 두 HUD 신규 테스트를 제거했다. 기존 FieldUnitView·FieldUnitPresenter·UnitHUD·UnitHUDTests·공용 HUD.prefab은 해당 추가 작업 전 HEAD 내용과 동일하다. Object 전용 Presenter/테스트와 CombatSceneDisplay는 HUD 추가 부분만 되돌렸다. Object 필드 생성/재사용/표시 여부, 배치 11개·StageKey/SourceId 회전 SO와 View DB 연결은 유지한다. Object는 HUD 없는 Cube 외형으로 표시한다.

OH1~OH4는 구현 이력으로만 남고 OH.U 검증 대기는 철회했다. 기존 V1.U·V2.U·V3.4의 Object 표시/DB 검증 대기는 유지한다. SSOT/SOLID/FW 계약은 기존 Phase 2 구성으로 돌아가며 추가 상태·서비스·Core 변경이 없다. 신규 공유 컴포넌트 .cs/.meta를 함께 제거했고 런타임/테스트/프리팹에 Object HUD 참조가 없음을 확인했다. 기존 HUD 파일의 diff는 비어 있다. Unity 생성 csproj 자체는 편집하지 않고 외부 MSBuild용 Temp targets에서 이미 삭제한 파일의 Compile 입력만 제외한다.

Core·Buffer·InGame SO·FieldCamera·Architecture·FMOD 및 이전 Phase 1~3 변경은 건드리지 않았다. 커밋·push 없음.

**롤백 검증:** 최종 MSBuild 오류 0, 관리 환경/DB/표시 회귀 100/100 통과. 신규 Object HUD 참조 없음과 기존 HUD 파일 diff 없음 확인. 증거는 Temp/EnvironmentSOAuthoring/build-object-hud-rollback.log 및 managed-object-hud-rollback-checks.log다. Unity 네이티브 실행은 하지 않았다.

### 2026-10-12 Obstacle 체력 HUD — 코드 연결 완료, Unity 확인 대기

사용자가 Obstacle에는 UnitHUD를 붙이고 체력바/숫자만 표시하도록 지시했다. FieldUnitPresenter의 구조물 HUD 조건을 기존 적 구조물 또는 공용 ObstacleContentProfile.IsNativeObstacleKey 계약에 해당하는 장애물로 확장했다. Owner.Neutral인 화약통/보급품이 기존 UnitHUD.SetStructure 체력 전용 모드를 사용한다. HP 원본은 전달받은 StructureCombatFacts.Health이며 View SO/규칙 수치를 추가하지 않았다. 유닛의 코스트/버프/턴 순서, 아군 본부의 기존 표시 정책과 Object HUD 철회는 유지했다.

기존 프리팹·Offset/Scale·카메라 방향·HealthLabel 대체와 풀 수명을 재사용했다. 새 컴포넌트/서비스/씬 참조는 없다. SSOT는 Buffer 체력, SOLID는 기존 구조물 Presenter/HUD 책임, JMH/FW는 기존 생성/반환/씬 해제 계약을 유지한다. Core·Buffer·InGame SO·View SO·프리팹·씬·FieldCamera·Architecture·FMOD 캐시는 수정하지 않았다.

**검증:** ProjectL.Tests.Framework MSBuild 오류 0, 변경 소스의 새 경고 0, 관리 환경/DB/표시 회귀 100/100 통과, git diff --check 통과. UnitHUDTests의 구조물 체력 전용/피해 갱신/유닛 모드 복원 테스트를 적 본부·중립 화약통·중립 보급품의 3개 경우로 확장했다. CombatSceneEventPresenterTests에는 실제 Combat 씬/Catalog/HUD에서 두 중립 장애물의 체력바/숫자·추가 영역 숨김·7/20 피해 갱신·파괴 반환·다른 EntityId 풀 재사용·Clear를 검증하는 2개 네이티브 경우를 추가했다. HUD 테스트는 컴파일까지만 확인했고 관리 100건을 해당 네이티브 실행 증거로 대신하지 않는다. BH.U에서 Unity Test Runner와 실제 재생 확인이 남는다.

증거는 Temp/EnvironmentSOAuthoring/build-obstacle-health-hud.log, managed-obstacle-health-hud-checks.log, obstacle-health-hud-diff-check.log다. 커밋·push 없음. 이번 추가 Phase 결과를 보고하고 멈춘다.

### 2026-10-12 Obstacle 체력바 색 — 사용자 추가 지시 완료

UnitHUD에 직렬화 obstacleHealthColor(기본 Color.gray)를 추가하고 공용 HUD.prefab에 회색 RGBA(0.5,0.5,0.5,1)를 저장했다. SetStructure에서 기존 장애물 키 계약으로 표시 분류를 정하고 RefreshHealth는 장애물 색을 진영 색보다 우선 적용한다. 체력/예상 체력 갱신에서도 유지되며 SetUnit/Clear는 분류를 초기화해 기존 유닛/본부 색을 복원한다. 색 입력은 기존 HUD 프리팹, HP는 Buffer, 분류는 전달받은 구조물 키를 사용한다. 숫자 색·코스트·버프/턴 표시 정책은 유지했다.

기존 UnitHUDTests의 구조물 체력 갱신/유닛 재사용 테스트에 Fill Image와 회색/적 색 복원 검증을 보완했다. 새 테스트 종류는 추가하지 않았다. MSBuild 오류 0과 이번 범위 git diff --check 통과를 확인했고 Unity 네이티브 색 검증/화면 확인은 BH.U에 남는다. 이번 색 변경에 무관한 관리 환경 회귀 100건은 반복 실행하지 않았다. 증거는 Temp/EnvironmentSOAuthoring/build-obstacle-health-color.log 및 obstacle-health-color-diff-check.log다. Core·Buffer·SO·씬·Object 외형 사용자 변경·FMOD는 건드리지 않았다. 커밋·push 없음.

### 2026-10-12 Obstacle HUD 위치·크기 분리 — 사용자 추가 지시 완료

FieldUnitPresenter의 Obstacle HUD 영역에 obstacleHudOffset(Vector3)와 obstacleHudScale(float, Min 0.001)을 추가했다. 초기값은 기존 화면과 같은 (0,1.5,0)/0.02이며 Combat 씬의 동일 컴포넌트에도 저장했다. CreateView에서 공용 장애물 키 계약으로 두 장애물을 분류해 전용 값을 기존 AttachUnitHud에 전달하고, 유닛/일반 구조물은 기존 Unit HUD 값을 사용한다. HUD 프리팹·색·체력 전용 모드·카메라 방향·월드 크기 보정·종류별 풀 수명은 기존 경로를 재사용한다. 설정은 외형 최초 생성 때 적용되므로 Inspector에서는 재생 전에 조정한다.

기존 CombatSceneEventPresenterTests 두 장애물 경우에 서로 다른 장애물 Offset/Scale을 작성해 유닛 설정 유지, 장애물 전용 위치/크기와 파괴 후 풀 재사용을 대조하도록 보완했다. 별도 테스트/컴포넌트/서비스는 만들지 않았다. 새 입력은 독립적인 표현 설정이며 위치/규칙 사실을 중복 소유하지 않는다(SSOT). 기존 Presenter 생성 책임과 FW 풀/해제 계약을 유지한다(SOLID/JMH).

최종 MSBuild 오류 0, 변경 범위 git diff --check 통과. 네이티브 테스트는 컴파일까지만 확인했고 실제 Inspector/표시/풀 재사용 실행은 BH.U에서 확인한다. 이번 변경과 무관한 관리 환경 회귀는 반복 실행하지 않았다. 증거는 Temp/EnvironmentSOAuthoring/build-obstacle-hud-transform.log 및 obstacle-hud-transform-diff-check.log다. Core·Buffer·InGame/View SO·FieldCamera·Architecture·공용 프리팹은 수정하지 않았다. 커밋·push 없음.

### 2026-10-12 Terrain 표현 방식 분류 — 사용자 추가 지시 완료

사용자가 합의한 분류대로 ViewGroup에 TerrainVisualMode(PlacedPrefab=0 / GroundShape=1)를 추가했다. TerrainVisualDataSO의 visualMode 입력은 GetData()에서 새 불변 TerrainVisualData.VisualMode로 전달하며 지원하지 않는 enum 값은 거부한다. 기본 SO 종류인 HighGround에 맞춰 새 SO의 기본 모드는 GroundShape다.

외형 SO 7개에 값을 명시했다. Wall·Sandstorm·Tornado·PresenceModifier는 PlacedPrefab, HighGround·WeakGround·Cliff는 GroundShape다. 이 모드는 규칙 종류와 별개의 표현 설정이며 View SO만 원본으로 둔다(SSOT). 기존 GetData 책임을 유지하고 별도 서비스나 런타임 상태를 추가하지 않았다(SOLID/JMH/FW). Core·InGame SO·Buffer·FieldCamera·Architecture는 수정하지 않았다. 실제 지형 생성 및 바닥 형상 변경은 이번 범위에 포함하지 않는다.

기존 환경 정의 테스트의 생성자 호출과 SO 전달 검증을 갱신하고, 실제 Terrain 외형 에셋 7종의 기존 교차 검증에 기대 모드를 추가했다. Unity 참조 DLL을 사용한 외부 C# 컴파일에서 ProjectL.Systems와 ProjectL.Tests.Framework 모두 오류 0, 환경 정의 관리 회귀 31/31 통과. 에셋 7개의 종류/모드/스크립트 GUID, 새 .meta GUID 유일성, UTF-8/LF와 git diff --check도 확인했다. 증거는 Temp/TerrainVisualMode/의 컴파일 로그와 managed-checks.log다. Unity Test Runner의 DataBaseAssetTests·EnvironmentAuthoringAssetTests·EnvironmentDefinitionAuthoringTests 실행은 아직 확인하지 않았다. 관리 실행은 네이티브 에셋 로드 검증을 대신하지 않는다. 커밋·push 없음.
