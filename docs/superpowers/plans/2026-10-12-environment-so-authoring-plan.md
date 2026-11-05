# 전투 환경 SO 입력 Task 계획

설계: [환경 SO 입력 설계](../specs/2026-10-12-environment-so-authoring-design.md). 시작 기준: b1cbc627. 사용자 범위: 표의 네 묶음, InGame/View 분리, DB 등록 제외. 커밋·push는 사용자가 직접 한다.

**Follow-up status (2026-10-12):** The user subsequently authorized Core timing changes and production tornado delivery. Both phases of the configurable tornado timing plan (Git history: `2026-10-12-tornado-repeat-interval-plan.md`) are complete. Original entries below that describe fixed repetition or pending BattleDefinitionAdapter delivery are historical; see that plan for the current authoring procedure and actual Unity test evidence.

## Phase 0 — 조사·설계

- [x] 네 묶음 범위 확인.
- [x] SO 입력과 Core 계약/운영 전달 누락 조사.
- [x] SSOT·SOLID·JMH/FW 계약 점검 및 설계 작성.

## Phase 1 — 종류 정의와 표현 SO

- [x] E1.1 TerrainContentKind, 순수 TerrainDefinitionData, TerrainDefinitionDataSO.
- [x] E1.2 기존 ObstacleContentKind를 쓰는 순수 ObstacleDefinitionData, ObstacleDefinitionDataSO.
- [x] E1.3 ObjectContentKind, 순수 ObjectDefinitionData, ObjectDefinitionDataSO.
- [x] E1.4 TerrainVisualDataSO, ObjectVisualDataSO. Obstacle은 기존 UnitVisualDataSO 재사용.
- [x] E1.5 규칙→기존 Core 계약 변환 및 오류 경계 테스트, Unity SO 테스트 작성.
- [x] E1.6 컴파일·관리 실행 가능한 테스트 확인; Unity 확인 대기 명시.
- [x] Phase 1 완료 보고 뒤 멈춤(리뷰 지점).

이 Phase에서 완성되는 것은 Inspector에서 생성·설정할 종류 정의다. 스테이지별 배치, 실제 에셋 준비, 운영 등록은 완료로 주장하지 않는다.

## Phase 2 — 스테이지 SO 배치 입력

- [x] E2.1 정의 참조+원본 ID+좌표+공개 대상 입력. Obstacle에는 회전, Object에는 배열 순서.
- [x] E2.1a 사용자 추가 승인: Terrain·Object 외형 회전은 ViewGroup의 StageEnvironmentVisualDataSO에 stageKey/sourceId별로 분리. 위치는 복제하지 않음.
- [x] E2.2 기존 정적 지형·벽 입력의 직렬화 필드와 GUID 유지. Unity 직렬화 확인 대기.
- [x] E2.3 TerrainPresence/Static/Obstacle/Object 기존 순수 배치 목록 생성, 토네이도 목록 확장.
- [x] E2.4 정의 참조 누락·ID 중복·범위·공존·순서 검증.
- [x] E2.5 Core 소유 BattleDefinitionAdapter의 토네이도 전달 보완은 경계 문서 요청으로 기록. 사용자 별도 지시 없이 Core 계산/조립 파일 수정 금지.
- [x] E2.6 관리 실행 가능한 스테이지 배치 검증·회귀 확인. Unity SO 테스트 작성·확인 대기 명시 후 완료 보고 및 멈춤.

## Phase 3 — 설정용 에셋과 사용자 인계

- [x] E3.1 InGameGroup에 종류별 정의 에셋, ViewGroup에 대응 표현 에셋 준비. 수치는 기존 SO 초기값, 미확정 외형은 빈 참조로 두고 기획 입력 대기를 명시.
- [x] E3.2 SO 작성 방법과 스테이지 연결 안내. 빈 외형·미지정 레벨 값을 운영 준비 완료로 표시하지 않음.
- [x] E3.3 Catalog/Factory/Excel/Query 등록 제외 상태와 운영 Core 연결 대기 명시.
- [ ] E3.4 실제 에셋 변경 시 DataBaseAssetTests 포함 Unity 확인. 완료 보고 후 멈춤.

## Phase 4 — Inspector 입력 정리 (사용자 추가 승인)

- [x] E4.1 새 환경 입력을 Environment로 표시하고 Corner Cut Ticks를 Wall Settings로 분리.
- [x] E4.2 기존 Wall/Terrain 필드는 직렬화 이름·타입·값·변환을 유지하며 호환용 접힌 항목으로 분리. 값이 있으면 표시.
- [x] E4.3 SerializedProperty 기반 전용 Inspector, Undo·다중 편집과 Editor 컴파일 경계 유지.
- [x] E4.4 컴파일·기존 관리 환경 회귀 확인. 실제 Inspector/직렬화 확인 대기를 별도로 명시하고 완료 보고 후 멈춤.

## 실행 기록

### 2026-10-12 Phase 0

표의 네 묶음임을 사용자 응답으로 확인했다. 기존 Core 종류·배치 계약을 확인했고 운영 SO의 Obstacle/Object 입력과 토네이도 전달이 빠져 있음을 구분했다. 사용자 요청은 SO 입력이므로 이번 Phase 1은 정의/표현 타입과 검증까지만 진행한다. 기존 FMOD 변경 2개는 사용자/에디터 변경이며 작업 범위에 포함하지 않는다.

### 2026-10-12 Phase 1 — 코드 완료, Unity 확인 대기

| 작업 | 결과 |
|---|---|
| 지형 정의 | 정적 다섯 종류·토네이도·위치 능력치 보정의 SO 선택 키, 불변 Data, 입력 SO. 약한 지반 진입 한도·위치 보정 두 값·토네이도 첫 발동을 설정한다. 기존 Static/Presence/Tornado 배치로 변환할 수 있다. |
| 장애물 정의 | 기존 종류 enum으로 체력·점유 크기·폭발 피해·피격/파괴 보상을 작성한다. 기존 초기 장애물 계약으로 변환하고 종류에서 구조물 키를 도출한다. 장애물 자체는 공격 행위자로 만들지 않는다. |
| Object 정의 | 지뢰 피해·깃발 우선순위·배지 종류를 작성한다. 기존 세 초기 계약으로 변환하며 sourceId/cell/knownTo/entryOrdinal을 유지한다. |
| 표현 | 지형과 Object의 키·이름·설명·아이콘·프리팹·셀 중심 상대 오프셋/크기만 작성한다. 규칙 Data/SO 참조나 좌표를 저장하지 않는다. 장애물은 기존 UnitVisualDataSO의 Structure_PowderBarrel/Structure_Supplies를 사용한다. |
| 메타데이터 | 새 C# 12개와 새 폴더의 .meta 작성. 기존 GUID·에셋은 변경하지 않았다. |

**확인된 증거:**

- 변경 전 MSBuild ProjectL.Tests.Framework 오류 0. 변경 후 같은 프로젝트를 새 파일 추가용 CustomAfterMicrosoftCommonTargets로 빌드하여 오류 0, 변경 파일 새 경고 0. 기존 다른 파일의 CS0649·CS8632 경고는 유지됐다. 생성 csproj는 수정하지 않았다.
- Unity Mono에서 실제 테스트 메서드/매개변수와 NUnit assertion을 관리 실행: EnvironmentDefinitionTests 27/27, StageEnvironmentAdapterTests 24/24, WeakGroundAuthoringTests 중 관리 가능한 7/7, 합계 58/58. Unity Test Runner 실행 결과와 구분한다.
- 새 순수 규칙 Data 3개에 UnityEngine/Unity 객체 타입 없음. 새 C# 12개 LF·UTF-8 및 .meta 짝 확인. Core·Buffer·씬·Factory·Query 파일 변경 없음.
- 스크래치 증거: Temp/EnvironmentSOAuthoring/AdditionalSources.targets, build.log, ManagedChecks.cs, managed-checks.log. 이 폴더는 기존 Temp 제외 규칙에 따라 Git 대상이 아니다.

**Unity 확인 대기:** EnvironmentDefinitionAuthoringTests 14건(Terrain 7, Obstacle 2, Object 3, 표현 1, 잘못된 한도 1). Inspector 종류 선택, GetData마다 새 Data 생성, 종류에 맞지 않는 이전 직렬화 값 정규화, 실제 프리팹/아이콘 연결을 확인해야 한다. 스테이지 위치 입력은 Phase 2, 종류별 실제 .asset 준비는 Phase 3이며 아직 구현하지 않았다. DataBaseAssetTests는 실제 에셋 변경이 없어 이번 Phase에서 실행하지 않았다.

**작성 메뉴:** Create > ProjectL > Database > InGame > TerrainDefinition / ObstacleDefinition / ObjectDefinition. 표현은 View > TerrainVisual / ObjectVisual, 장애물은 기존 View > UnitVisualData. 새 에셋을 InGameGroup/ViewGroup 아래에 저장하되 현재 Catalog·테이블에 등록하는 작업은 범위 밖이다.

AGENTS.md의 "Phase가 끝나면 멈추고 보고한다(리뷰 지점)"에 따라 Phase 1에서 멈춘다. 커밋·push 없음.

### 2026-10-12 Phase 2 — 코드 완료, Unity 확인 대기

사용자가 Phase 2와 네 묶음의 위치·회전 입력을 지시했다. Core는 장애물 회전만 규칙에 반영하므로 Terrain/Object 회전을 ViewGroup으로 분리할지 질문했고, 사용자가 "외형 회전으로 분리"를 승인했다. 설계 §2·§5와 E2.1a에 반영했다.

**구현:**

- CombatStageData의 `Environment Definitions > Environment`에 Terrain/Obstacle/Object 세 배열을 추가했다. Terrain 배열은 정적 다섯 종류·토네이도·위치 보정 정의를 모두 받는다. 정의 참조·sourceId·cell·knownTo, 장애물의 definition·anchor·rotation을 순수 계약으로 변환한다. Object는 배열 순서 그대로 전달한다.
- 기존 `staticTerrainPlacements`·`initialWallCells`와 스크립트 GUID를 유지했다. 새 Terrain과 기존 인라인 지형을 합치고 Terrain/Object/토네이도 전체의 원본 ID 중복을 거절한다. 장애물 운영 개체 ID는 Core가 할당하므로 입력하지 않는다.
- CombatStageDetail에 마지막 선택 인자와 불변 복사 목록 `TornadoPlacements`를 추가했다. 기본 빈 배열로 기존 생성자 호출을 유지한다. 토네이도 자체는 기존 TornadoSourceFact 계약을 사용한다.
- FieldDefinition이 있으면 SO.GetData에서 기존 StageEnvironmentAdapter 및 CellEnvironmentQuery로 환경 범위·공존·벽/철도 규칙을 검사한다. 꽉 찬 직사각형 장애물의 회전된 전체 점유 영역도 검사한다. 필드 참조가 없는 미완성 SO는 기존처럼 GetData가 가능하며 범위 검사는 보류한다. 유닛·본부를 포함한 최종 점유 검증은 기존 Core 준비에 맡긴다.
- ViewGroup용 StageEnvironmentVisualDataSO에는 stageKey와 Terrain/Object의 sourceId별 Rotation4만 저장한다. 생략한 원본은 R0, 잘못된/중복 ID·회전은 거절한다. `ValidateBindings`는 향후 소비자가 Buffer 분류로 대조하는 경계다. InGame SO나 좌표 참조는 없다. 작성 메뉴는 `Create > ProjectL > Database > View > StageEnvironmentVisual`이다.
- [Core 경계 문서 §5.9](../handoffs/2026-09-24-view-core-data-boundary-handoff.md#59-환경-so-배치-입력과-토네이도-목록-2026-10-12-사용자-지시-미전달)에 BattleDefinitionAdapter의 `tornadoPlacements: combat.TornadoPlacements` 전달 요청을 기록했다. Core 파일 수정·외부 전달은 하지 않았다. 운영 토네이도 전달과 런타임 외형 회전 소비는 완료 상태가 아니다.

**확인된 증거:**

- ProjectL.Tests.Framework MSBuild 오류 0, 변경 파일 새 경고 0. 생성 csproj 대신 Temp/EnvironmentSOAuthoring/AdditionalSources.targets로 아직 미포함인 새 소스만 추가했다. 기존 다른 파일의 CS0649·CS8632 경고는 유지됐다.
- Unity Mono 관리 실행: EnvironmentStageTests 22/22, EnvironmentDefinitionTests 27/27, StageEnvironmentAdapterTests 24/24, WeakGroundAuthoringTests 관리 가능분 7/7, 합계 80/80. 토네이도 목록 복사·ID 충돌, 회전 4방향의 장애물 경계, 전체 점유 공존, 새/기존 벽 조합, Object 순서, View 회전 분류 대조를 확인했다. Unity Test Runner 실행과 구분한다.
- 스크래치 증거: build-phase2.log, managed-phase2-checks.log. 새 C# 5개 및 폴더/파일 .meta 작성. 순수 CombatStageDetail·환경 검증 파일에 UnityEngine 참조 없음. Core·Buffer·FieldCamera·Architecture·씬·Catalog·Query·Factory 수정 없음.

**Unity 확인 대기:** EnvironmentStageAuthoringTests 7건(네 묶음 전체 변환 1, 기존 직렬화/ID 충돌 1, 누락 정의 3, 필드 기반 전체 장애물 경계 1, 외형 회전 SO 1). Phase 1의 네이티브 SO 14건도 계속 확인 대기다. 이 프로젝트의 Unity가 열려 있어 같은 프로젝트에 별도 배치 테스트 프로세스를 실행하지 않았다. Inspector에서 실제 에셋을 저장·재로드하고 스테이지 연결을 확인해야 한다. 실제 .asset 값은 바꾸지 않았으며 Phase 3 설정 에셋 준비는 진행하지 않았다.

**사용 순서:** InGameGroup에 Phase 1 정의 SO를 만들고 CombatStageData의 Environment 배열에 연결한다. Terrain/Object의 sourceId는 스테이지 안에서 유일하게 정하고 cell과 knownTo를 입력한다. 장애물은 anchor와 rotation을 입력한다. ViewGroup에 StageEnvironmentVisual SO를 만들고 StageRoot와 같은 stageKey, 대응 sourceId와 외형 rotation을 넣는다. View SO에는 위치를 다시 작성하지 않는다.

AGENTS.md의 Phase 리뷰 지점에 따라 Phase 2에서 멈춘다. 커밋·push 없음.

### 2026-10-12 Phase 3 — 설정용 에셋 작성, Unity 확인 대기

사용자가 Phase 3과 두 그룹의 네 묶음 설정용 SO 에셋 생성을 지시했다. 기존 SO·Master 에셋·옛 에셋 값 목록·프리팹 목록에서 이 종류의 확정 수치와 대응 승인 외형을 찾지 못했다. 기존 ObstacleMaster의 나무 상자/돌벽/철벽/모래주머니는 새 Core의 화약통/보급품과 대응하지 않아 값을 옮기지 않았다. 종류별 SO 기본값을 명시적으로 저장하고 운영 수치·외형 입력 대기로 구분했다.

**작성 결과:**

| 저장 위치 | 에셋 |
|---|---|
| InGameGroup/Terrain/Item | HighGround, Sandstorm, Wall, WeakGround, Cliff, Tornado, PresenceModifier — 7개 |
| InGameGroup/Obstacle/Item | PowderBarrel, Supplies — 2개 |
| InGameGroup/Object/Item | Mine, Flag, SheriffBadge — 3개 |
| ViewGroup/TerrainVisual/Item | 규칙 종류와 같은 키의 표현 — 7개 |
| ViewGroup/UnitVisual/Item | 기존 UnitVisual 타입, Structure_PowderBarrel/Structure_Supplies — 2개 |
| ViewGroup/ObjectVisual/Item | 규칙 종류와 같은 키의 표현 — 3개 |
| ViewGroup/StageEnvironmentVisual/Item | StageEnvironmentVisual_Template — 1개, 회전 배열 비움 |

총 25개이며 에셋·새 폴더의 .meta를 함께 만들었다. 설정 안내·초기값 출처·실제 스테이지 연결 순서의 원본은 [설계 §8](../specs/2026-10-12-environment-so-authoring-design.md#8-phase-3-설정용-에셋과-입력-방법)이다. 한국어 표시 이름만 입력했으며 프리팹/아이콘은 지정하지 않았다. 특히 장애물 UnitVisual의 필수 프리팹이 비어 있어 Validate는 아직 실패한다. Catalog에 등록하지 않은 입력 시작점이며 운영 표시 준비 완료가 아니다. 기존 스테이지에 임의 좌표·회전·환경 효과를 추가하지 않았다.

**SSOT·SOLID·JMH/FW 점검:** 규칙 값은 InGame 정의 한 곳, 배치 좌표는 CombatStageData, 표현 자원과 외형 회전은 View SO에 둔다. 장애물 외형은 기존 UnitVisual 타입을 재사용했고 장애물 규칙을 기존 Structure SO에 중복 작성하지 않았다. 새 서비스/Manager/Builder/등록 경로가 없고 Buffer 상태를 쓰지 않는다. 설계 §6과 공통 인계 §4의 원칙을 유지한다. Core·FieldCamera·Architecture 파일을 수정하지 않았다.

**확인된 증거:**

- ProjectL.Tests.Framework MSBuild 오류 0, 이번 변경 파일 새 경고 0. 새 에셋 검증 fixture도 컴파일했다. 기존 다른 파일의 CS0649·CS8632는 유지됐다.
- 새 에셋 25개의 실제 스크립트 GUID 일치, Assets 전체에서 GUID 충돌 없음, NativeFormatImporter/mainObjectFileID 11400000, UTF-8/LF 검사 통과.
- 기존 관리 실행 가능한 환경 회귀 80/80 통과. 실제 AssetDatabase 로드 결과나 Unity Test Runner와 구분한다. 스크래치 증거는 build-phase3.log, managed-phase3-checks.log, CreatePhase3Assets.ps1이다.

**Unity 검증 준비·최종 확인:** EnvironmentAuthoringAssetTests 14건(지형 7, 장애물 2, Object 3, 회전 템플릿 1, Catalog 미등록 1)을 추가했다. 기존 DataBaseAssetTests와 Phase 1/2 SO 직렬화 테스트를 포함한 EditMode 필터를 준비했다. 열린 Unity에서 테스트를 실행할 일회성 Editor 도구를 준비하고 컴파일했지만, 에디터가 파일을 읽지 않아 시작/결과 파일이 만들어지지 않았다. 따라서 Unity 테스트 성공을 주장하지 않는다. 임시 Assets/EnvironmentVerificationTemp 폴더와 요청 플래그를 제거하여 나중에 자동 실행되지 않도록 정리했다. 도구 원본은 무시되는 Temp/EnvironmentSOAuthoring/EnvironmentVerificationBridge.cs에만 남겼다. E3.4는 Unity에서 EnvironmentAuthoringAssetTests·DataBaseAssetTests·EnvironmentDefinitionAuthoringTests·EnvironmentStageAuthoringTests 실행 확인이 남아 있다. 실제 저장·재로드/Inspector 확인도 이때 함께 한다.

기존 FMOD 에셋 2개와 Combat_Boss.asset의 변경은 이번 Phase 시작 전부터 존재했다. 이 변경은 덮어쓰거나 되돌리지 않았다. 커밋·push 없음. 이번 Phase는 설정용 에셋 작성과 인계까지이며 운영 DB 등록·Core 토네이도 전달·런타임 외형 소비 연결은 범위 밖이다.

설정용 에셋 작성 결과를 보고하고 Phase 3 리뷰 지점에서 멈춘다. 검증 대기는 위 E3.4와 구분하여 유지한다.

### 2026-10-12 Phase 4 — Inspector 입력 정리, Unity 확인 대기

사용자가 Environment 입력 통일, 기존 Wall/Terrain의 호환용 분리, Corner Cut Ticks의 Wall Settings 이동을 명시적으로 지시했다. 설계 §5·§8.2에 반영한 뒤 구현했다.

- CombatStageData에서 `Environment Definitions`라는 별도 헤더를 없애고 `Environment`를 기본 배치 입력으로 표시한다. 벽도 Terrain Placements의 Terrain_Wall 정의로 작성한다.
- `cornerCutTicks`를 `Wall Settings` 아래에 두고 새 Wall과 기존 벽에 공통 적용하는 값임을 툴팁에 적었다. 직렬화 필드·기본값·Range·WallBlockingSpec 변환은 유지했다.
- `initialWallCells`·`staticTerrainPlacements`의 이름·타입·기본값·변환을 유지하고 HideInInspector를 붙였다. 새 전용 Inspector의 기본 닫힌 `Legacy Environment (호환용)`에서 기존 두 배열을 확인·수정할 수 있다. 기본 Inspector의 별도 Walls/Terrain 입력 경로는 사라졌다.
- 기존 호환용 값이 있으면 접힌 상태에도 함께 처리됨을 표시한다. 여러 에셋을 선택했을 때도 각 에셋을 대조하여 기존 값이 있는 대상을 빠뜨리지 않는다. 자동 이관·배치 삭제·새 입력 우선순위 도입은 없다.
- `Systems/DataBaseSystem/Editor/CombatStageDataEditor.cs`와 파일/폴더 .meta를 추가했다. 클래스 전체를 UNITY_EDITOR 조건으로 감싸 기존 Systems 어셈블리의 에디터 전용 코드 관례를 따른다. 새 어셈블리 의존성이나 런타임 서비스를 만들지 않았다. SerializedObject.Update/ApplyModifiedProperties와 PropertyField를 사용하여 기존 Undo·다중 편집 경로를 유지한다.

**검증:** ProjectL.Tests.Framework MSBuild 오류 0, 이번 변경 파일 새 경고 0. 에디터 정의가 없는 별도 C# 컴파일도 오류 0으로 Editor 코드가 제외됨을 확인했다. Unity Mono 관리 환경 회귀는 80/80 통과했다. build-phase4.log, managed-phase4-checks.log, InspectorWithoutEditor.dll은 무시되는 Temp/EnvironmentSOAuthoring에 있다. 에셋 값·Core·Buffer·FieldCamera·Architecture·Catalog·Query·Factory는 이번 정리에서 수정하지 않았다. 변경 C#의 UTF-8/LF·.meta와 git diff --check를 확인했다.

**Unity 확인 대기:** 실제 CombatStageData Inspector에서 Environment 입력, Wall Settings 헤더, 호환용 접기/펼치기, 기존 값 알림을 확인한다. 기존 배열을 수정한 뒤 Undo·저장/재로드, 두 에셋 다중 선택도 확인해야 한다. 기존 EnvironmentStageAuthoringTests의 레거시 직렬화·ID 중복 검증을 실행한다. Phase 3 E3.4의 에셋 테스트 대기는 유지한다. 관리 실행 결과를 Unity 화면·직렬화 검증 완료로 대신하지 않는다.

사용자가 요청한 Inspector 정리 코드까지 완료 보고하고 리뷰 지점에서 멈춘다. 커밋·push 없음.
