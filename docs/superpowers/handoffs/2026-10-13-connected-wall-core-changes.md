# 연결 Wall Core 변경사항

작성일: 2026-10-13. 대상: Core 개발 및 병합 리뷰. 범위: 공통 스테이지 배치를 지원하기 위해 진행한 C1~C3.

Core가 사방으로 연결된 Wall을 실제 셀의 형상으로 처리하도록 확장했다. 기존 직선 벽과 함께 ㄴ자·T자·두께 있는 벽을 지원하며, ㄴ자 안쪽 빈칸은 이동 가능한 공간으로 유지한다. 사용자가 그림 유지와 Core 수정, C1·C2·C3 진행을 명시적으로 지시해 구현과 인수를 완료했다.

## 해결한 문제

기존 `WallGeometryBuilder`는 연결된 Wall이 가로 또는 세로 한 줄이 아니면 준비를 거절했다. 예를 들어 `201@(1,2), 202@(2,2), 205@(2,3)`은 ㄴ자이므로 전투 준비 중 예외가 발생했다.

직선 검사만 제거하면 기존 직사각형 표현이 빈칸 `(1,3)`까지 벽으로 취급할 수 있다. 이번 변경은 실제 세 칸의 형상을 구성해 이 빈칸을 보존한다. 모든 스테이지에 입력한 Wall 12개는 다음 네 연결 성분으로 처리된다.

| 성분 | Source Id | 실제 Wall 셀 |
|---|---|---|
| 아래 왼쪽 | 201, 202, 205 | (1,2), (2,2), (2,3) |
| 아래 오른쪽 | 203, 204, 206 | (6,2), (7,2), (7,3) |
| 위 왼쪽 | 207, 209, 210 | (2,5), (1,6), (2,6) |
| 위 오른쪽 | 208, 211, 212 | (7,5), (6,6), (7,6) |

## 구현 파일

아래 파일은 모두 `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Combat/`에 있다.

| 파일 | 변경 | 역할 |
|---|---|---|
| [WallGeometry.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Combat/WallGeometry.cs) | 수정 | `WallRun.Parts` 추가. 연결 성분의 형상 조각을 복사된 읽기 전용 목록으로 보관 |
| [WallConvexPart.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Combat/WallConvexPart.cs) | 신규 및 .meta 추가 | 볼록 조각의 정수 경계, 외곽 변, 깎을 모서리를 표현하는 내부 불변 타입. `WallBoundary`·`WallCorner` 포함 |
| [WallGeometryBuilder.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Combat/WallGeometryBuilder.cs) | 수정 | 직선은 기존 단일 조각으로 구성하고, 일반 연결 성분은 실제 셀별 조각과 외곽 정보를 구성 |
| [WallSegmentClipper.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Domain/Combat/WallSegmentClipper.cs) | 수정 | 성분의 조각들을 검사해 실제로 교차하는 조각 중 가장 이른 진입 시각 반환 |

`WallRun`이라는 내부 이름과 `WallGeometry.Runs`는 유지했다. 이제 Run 하나는 직선에 한정되지 않는 연결 성분이다. `XMin/XMax/YMin/YMax`는 성분을 감싼 사각형이며, 비직선 성분의 실제 차단 판정에는 `Parts`를 사용한다.

## 형상과 판정 규칙

Wall 점유는 원본 `Cells`를 기준으로 한다. 성분을 감싼 사각형에 포함되더라도 Wall이 없는 칸은 점유하지 않는다. 일반 형상은 실제 셀의 볼록 조각을 합친 형태이며 U자·고리 형상의 빈 공간도 보존한다.

`CornerCutTicks`는 바깥 볼록 모서리에만 적용한다. 같은 성분의 셀들이 공유하는 변과 ㄴ자 안쪽의 오목 모서리는 깎지 않는다. 직선은 기존 단일 조각 경로를 유지한다.

선분이 실제 벽 내부를 가로지르면 차단하고 외곽에 닿거나 외곽선을 따라가기만 하면 통과한다. 셀 사이의 공유 변은 완성된 벽의 내부이므로 닫힌 분할 경계로 처리한다. 따라서 벽 내부의 이음새를 따라가는 선분이 잘못 통과하지 않는다. 시작점과 끝점이 같은 경우도 기존처럼 그 점이 실제 벽 내부인지 판정한다.

진입 시각 비교는 기존 `SegmentParameter`와 정수 분수 비교를 사용한다. 한 성분을 떠났다가 다시 진입해도 최초 진입을 반환한다. 성분 사이 진입 시각이 같으면 기존 `ComponentKey`, 즉 최소 Source Id로 순서를 결정한다.

다음 입력 제약은 계속 적용된다.

- Wall은 활성 상태이고 Player·Enemy 양측에 공개되어야 한다.
- Wall 원본 ID는 Terrain으로 분류되어야 하며 셀은 보드 안에 있어야 한다.
- 같은 셀에 Wall을 중복 배치할 수 없다.
- 서로 다른 연결 성분끼리 대각선으로 접하는 배치는 허용하지 않는다.

## 기존 계약과 소비자 영향

공개 Core Contracts, SO 입력, Buffer의 입력·발행 형식, Factory와 서비스 등록은 그대로다. 새 조각 타입은 Core 내부 계산용이므로 기획자가 SO에 조각 배열을 넣거나 View가 이를 조립할 필요가 없다.

`SourceIds`는 오름차순, `Cells`는 y/x 순을 유지한다. 공격 차단 결과에는 조각 ID를 만들지 않고 연결 성분 전체의 원본 `SourceIds/Cells`를 반환한다. 여러 조각을 통과하더라도 같은 성분의 증거를 중복 발행하지 않는다.

`AttackOcclusionResolver`, 이동·공격·AI 및 Buffer의 production 코드는 이번 작업에서 추가 수정하지 않았다. 기존 소비 경로가 확장된 형상을 사용한다. 직격·파급·관통과 `IgnoreWalls` 정책도 유지하며, `IgnoreWalls`가 Wall 셀 자체에 효과를 배치하도록 허용하는 것은 아니다.

SSOT 기준으로 원본 환경 사실과 Wall 설정에서 형상을 파생한다. `Parts`는 별도 가변 전투 상태가 아니다. SOLID 기준으로 Builder는 형상 조립, Clipper는 교차 계산, 기존 소비자는 정책 판정을 맡는다. JMH Framework 계약에 따라 Core는 Unity 타입이나 물리에 의존하지 않고 기존 입력·준비·발행 경계를 사용한다. 새 서비스나 생명주기 등록은 추가하지 않았다.

## 검증 결과

검증 수치와 실행 이력의 원본은 연결 Wall Phase 계획 (Git history: `2026-10-13-connected-wall-geometry-plan.md`)이다. 아래 부분집합을 전체 건수에 중복 합산하지 않는다.

| 검증 범위 | 결과 | 확인 내용 |
|---|---|---|
| C1 기하 회귀 | 155/155 | 새 형상 66건과 기존 직선 89건. 독립 외곽선 기준으로 37,500개 선분 비교 |
| C2 추가 전투 회귀 | 14/14 | 빈칸 이동·공격, 실제 Wall 차단, 미리보기·명령 일치, 직격·파급·관통·AI |
| 최종 순수 Core 전체 | 2,227/2,227 | C1·C2 부분집합 포함. 관리형 Mono/NUnit 실행 |
| Wall Buffer 발행 | 10/10 | 꺾인 Wall 준비·발행·종료 시 원본 셀 보존 |
| C3 관련 Unity EditMode | 178/178 | 실제 DB 에셋, 운영 준비·표시 및 Terrain·Object·Obstacle·레일 회귀 |
| C3 Database/Live 재생 | 1/1 | 정상 맵 입력으로 Combat 진입, 빈칸 실제 이동, Wall 칸 이동 거절 및 공격 미리보기 |

기하 검증은 production 조각 목록을 기대값으로 재사용하지 않고 독립 외곽선·유리수 교차 기준을 사용했다. ㄴ자·T자·2×2·U자·고리, 회전·평행이동, 모서리·접선·내부 이음새·재진입·큰 좌표와 입력 역순을 확인했다.

C3에서는 실제 Catalog를 통해 11개 전투 스테이지를 `CellPrefab`·`GridOutline` 각각 준비하고 표시했다. 총 22개 구성에서 Wall 12개·Object 3개·Obstacle 2개를 확인했다. Live 재생은 그중 실제 그래프에서 선택된 한 스테이지에서 수행했다. `(0,1) → (0,2) → (0,3) → (1,3)`으로 이동했고 Wall `(2,3)` 진입은 `DestinationOccupied`로 거절됐다. 모든 스테이지를 각각 Live로 재생했다는 의미는 아니다.

기존 `WallGeometryTests`와 `WallStateLifecycleTests`의 꺾인 벽 거절 기대는 지원 계약에 맞게 성공·실제 형상·원본 보존 검사로 변경했다. `ConnectedWallGeometryTests`, `ConnectedWallOutlineReference`, `ConnectedWallCombatTests`를 추가했고 `WallStatePublicationTests`를 보강했다. C3는 `TerrainOperationalDisplayTests`와 `ConnectedWallBootSmokeTests`로 운영 경로를 검증했다. 본부 Y=7 기대는 현재 SO의 Y=8에 맞춘 테스트 변경이며 본부 SO 위치를 바꾸지 않았다.

Core 관리형 컴파일과 Unity 컴파일은 통과했고 변경 파일에 새 컴파일 경고는 없었다. C3의 테스트 준비·서비스 수명·도메인 리로드 대응은 테스트 코드에서 보정했다. C3에서 production Core/View 코드를 추가 변경하지 않았고 DB·Combat 씬 보호 대상 509개 파일의 입력을 보존했다.

## 병합과 후속 작업

Core 변경을 옮길 때는 수정한 세 파일과 신규 `WallConvexPart.cs`·`.meta`를 함께 포함한다. 기존 테스트 기대 변경 및 신규 회귀 테스트도 함께 옮긴다. 공개 API 변경이나 SO 데이터 마이그레이션은 필요하지 않다.

Wall 12개 배치를 그대로 사용할 수 있다. 이후 배치를 편집할 때도 위의 공개·범위·중복·대각선 접촉 제약을 따른다. `WallRun`의 감싼 사각형을 새 소비자가 실제 차단 형상으로 사용하지 않도록 주의한다. 이 결과는 Wall 형상 지원에 해당하며 낭떠러지 등 바닥을 변형하는 Terrain 표현은 별도 작업이다.

설계 원본은 연결 Wall 형상 설계 (Git history: `2026-10-13-connected-wall-geometry-design.md`), 공용 경계 기록은 [Core 데이터 경계 인계의 5.10절](2026-09-24-view-core-data-boundary-handoff.md#510-연결-wall-실제-셀-형상--c1c2-완료-2026-10-13-사용자-명시-지시)이다. 커밋·push와 외부 전달은 수행하지 않았다.
