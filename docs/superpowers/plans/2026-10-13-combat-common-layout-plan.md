# 전투 공통 9×9 배치 — 설계·Task·실행 기록

> 현재 상태: C1·C2에서 ㄴ자 Wall 실제 형상을 지원했고 C3에서 실제 Unity 에셋/운영 정의·11개 스테이지의 Core 준비와 두 필드 모드 표시, 선택된 한 스테이지의 Database/Live 재생을 통과했다. 원래 Wall 12개와 환경 5종 배치는 보존했다. 최신 증거는 연결 Wall 계획의 C3 실행 기록을 따른다.

## 입력과 설계

2026-10-13 사용자 그림의 공통 배치를 일반 A~J·Boss 총 11개 Combat SO에 적용한다. 추가 질문에서 Object·Obstacle은 종류별 하나씩, 총 5개로 확정했다. 좌표는 왼쪽 아래가 (0,0)이며 그림 위쪽이 적 진영이다. 현재 11개 SO는 이미 Field_9x9를 참조하고 중앙 레일 9칸이 입력돼 있다.

| 대상 | 좌표·입력 |
|---|---|
| 아군 설치 구역 | y=0~1, Field_9x9.playerDeploymentDepth=2 유지 |
| 시작 적 스폰 구역 | y=7~8, Field_9x9.enemyDeploymentDepth=2 및 기존 EnemyLoadout 유지 |
| 레일 | x=0~8, y=4. 기존 발동·반복 간격 유지 |
| Wall | y=2/6: x=1,2,6,7. y=3/5: x=2,7. 총 12개 |
| 지뢰·깃발·배지 | 순서대로 (3,2), (4,2), (5,2) |
| 화약통·보급품 | 순서대로 (0,5), (8,5) |

Wall은 Environment > Terrain Placements에 기존 Terrain_Wall 정의와 Source Id=201~212를 넣는다. y가 낮은 행부터 x 순으로 번호를 부여하며 Player·Enemy 모두 공개, 외형 회전은 기존 R0 기본값을 사용한다. Object는 기존 Source Id=101~103, 공개 대상·외형 회전을 유지한다. Obstacle은 기존 정의·규칙 회전을 유지하고 위치만 바꾼다. 회색 칸은 별도 요소로 만들지 않는다. 유닛·본부 위치, 경제, 로스터, 적 구성, 씬, 프리팹과 다른 작업자의 변경을 보존한다.

SSOT: 위치·공개는 InGame 입력 SO, 크기·배치 깊이는 공용 Field SO, 외형·Object 회전은 기존 ViewGroup SO가 원본이다. 호환 Wall/Static Terrain 배열에 같은 입력을 중복하지 않는다. Core가 생성한 Buffer로 표시하는 기존 경계를 유지한다. SOLID/FW: 새 시스템·서비스·실행 분기를 만들지 않고 기존 DB authoring/Query/표시 계약을 사용한다. 초기 설계는 Core 변경 없이 가능하다고 판단했으나 Wall 형상 제약을 누락했다. 그림을 유지하려면 Core 확장이 필요하다. FieldCamera·Architecture 변경은 필요 없다.

## Phase L — SO 배치와 교차 검증

- [x] L1 작업 시작 값·보존 대상 기록, 11개 Environment 배치 적용.
- [x] L2 기존 실제 에셋 검증의 Wall 12개 및 Object/Obstacle 좌표 기대값 갱신.
- [x] L3 그림의 좌표·ID·정의·공개·회전, 구역·레일·본부와의 비겹침 및 범위 밖 필드 보존 점검.
- [x] L4 변경된 테스트 컴파일, 가이드·실행 기록 및 범위 diff 점검.
- [x] L.U 후속 C3에서 Unity EditMode 실제 에셋 검증 및 Database/Live 재생 확인(연결 Wall 계획 실행 기록 참조).

L1~L4 완료 후 리뷰 지점에서 멈춘다. Unity 실행 증거와 외부 컴파일·정적 점검은 구분하고 커밋·push는 하지 않는다.

## 실행 결과

11개 Combat SO의 Environment만 변경했다. 정의·수치·공개·Object/Obstacle 회전은 보존했으며 Source Id=201~212 Wall 12개, 101~103 Object 3개, Obstacle 2개를 작성했다. 최초 정적 검증은 범위·중복·겹침만 검사했고 Core의 Wall 연결 모양은 검사하지 않았다. 각 스테이지의 Environment 밖 텍스트는 시작 전과 동일하다. 기존 레일·본부·경제·적 구성 등 스테이지별 차이를 보존했다. 다른 DB 에셋/메타와 Combat.unity 총 487개 파일의 해시가 시작 전과 일치한다.

기존 DataBaseAssetTests의 EnvironmentAndViewRotations 검증을 공통 배치에 맞췄다. 현재 Unity Library의 실제 어셈블리 참조로 해당 테스트와 DataBaseTestAssets를 외부 C# 컴파일하여 오류 0·경고 0을 확인했다. 11개 YAML의 Wall 패턴/정의/ID/공개, Object/Obstacle 위치·1×1 점유, 설치·스폰 구역과 레일·본부 비겹침, 나머지 입력 보존, UTF-8 LF 및 범위 diff 점검을 통과했다. 증거는 Temp/CombatCommonLayout의 before-stage-inputs.json, protected-hashes.json, validate.ps1, asset-validation.log, tests-compile.log에 있다.

Unity Test Runner 및 Play를 실행한 증거는 없다. 기존 별도 테스트 `EveryCombatStage_UsesRequestedNineByNineCenterRail_AndBoundTrainPrefab`는 본부 Anchor.Y를 7로 기대하지만 현재 작업 시작 SO는 모두 8이었다. 이 기존 불일치는 이번 Environment 배치 변경에 포함하지 않았으며 실제 전체 DataBaseAssetTests 실행 시 해당 기대값이 실패할 수 있다. 본부 입력과 그 테스트는 보존했다.

Phase L의 코드·에셋 변경을 마치고 리뷰 지점에서 멈춘다. Core/Buffer/표시 코드·FieldCamera·Architecture·씬·프리팹 변경 없음. 커밋·push 없음.

## 실행 실패 인수와 수정안 (2026-10-13)

사용자가 `ArgumentException: 벽 연결 성분은 가로 또는 세로 한 줄이어야 합니다. 원본: 201@(1,2), 202@(2,2), 205@(2,3)`를 보고했다. Core WallGeometryBuilder는 사방 인접 Wall을 하나의 성분으로 모은 뒤 x 또는 y가 일정한 직선만 허용한다. 서로 다른 성분의 대각선 접촉도 금지한다. 그림의 네 ㄴ자 성분은 이 계약에 맞지 않는다. Source Id나 Corner Cut Ticks를 바꿔도 연결 구분은 좌표 기준이므로 해결되지 않는다.

실제 Core의 WallGeometryBuilder.cs·WallGeometry.cs를 수정 없이 스크래치 실행기에 컴파일해 같은 ID/좌표로 실패를 재현했다. 안쪽 (2,3)/(7,3)/(2,5)/(7,5), Source Id 205~208을 제외한 대안은 Wall 8개·가로 직선 성분 4개로 같은 Core 검증을 통과했다. 증거: Temp/CombatCommonLayout/WallShapeCheck.cs, wall-shape-compile.log, wall-shape-check.log. 이는 벽 형상 검증이며 전체 Unity 전투 준비 증거와는 구분한다.

- 그림 유지: Core의 ㄴ자 실제 셀 형상 지원이 필요하다. [Core 경계 인계 §13.19](../handoffs/2026-09-24-view-core-data-boundary-handoff.md)에 요청을 적었다. 검증 문구만 제거하면 현 사각형 WallRun이 ㄴ자의 빈칸까지 벽으로 계산할 수 있어 형상·클리핑 계약을 함께 검토해야 한다.
- Core 유지: 위 네 칸을 삭제해 8개로 변경한다. 사용자 그림과 달라지므로 선택 전 SO를 바꾸지 않는다. Object/Obstacle은 유지할 수 있다.

사용자는 그림 유지 및 Core 수정 설계부터 진행을 선택했고 Core 작업도 본인 담당이라고 밝혔다. 설계·구현 원본은 연결된 Wall Core 설계 (Git history: `2026-10-13-connected-wall-geometry-design.md`)와 C1~C3 Phase 계획 (Git history: `2026-10-13-connected-wall-geometry-plan.md`)으로 이전했다. 별도 외부 Core 담당자 허가나 회신은 선행 조건이 아니다. 설계 작성 시에는 Core를 수정하지 않았다. 후속 명시 지시로 C1·C2를 구현했고 순수 Core 전체 2,227건과 Wall Buffer 발행 10건이 통과했다. Core 변경은 경계 인계 §5.10, 실행 증거는 C1~C3 계획의 C1·C2 기록을 따른다. SO·씬 입력은 보존했고 후속 C3에서 11개 실제 운영 준비/표시 및 Database/Live 진입을 통과했다. Native 178/178과 Live 1/1의 범위는 연결 Wall 계획 C3 실행 기록을 따른다.
