# ProjectL Stage 기능 구현 로드맵

> 과거 기록 (2026-10-06 정리): 아래 내용은 Framework/Buffer 전환 전 구현이며 현재 구현 지침이 아닙니다. 삭제된 계획·설계 원문은 Git 커밋 `606fdf4c`에서 확인할 수 있습니다. 현재 구조와 API는 [Framework 개발 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md)와 실제 코드를 기준으로 확인합니다.

- 갱신일: 2026-08-30
- 목적: Run Stage Map, Enemy 초기 구성, 전투 Stage Gimmick, Timeline, Unity Bootstrap의 구현 순서와 책임을 한곳에서 고정한다.

## 1. 용어

| 문서 용어 | 의미 |
|---|---|
| Run Stage Map | 시작부터 Boss까지 `StageKey`가 배치된 DAG와 현재 노드 선택 |
| Combat Stage Definition | 9×9 등의 전투 크기, 진영, Enemy, 지형, Gimmick을 기술하는 데이터 |
| Combat Grid State | 한 전투 중 Cell 점유와 현재 지형 상태 |
| StageSession | 한 Combat Stage의 배치, 전투, Timeline, Gimmick Runtime |

코드 이름을 즉시 일괄 변경하지 않는다. 문서와 새 타입에서는 위 의미를 사용해 Run Map의 `StageMapRunState`와 전투 Grid의 `Domain.Map.StageMapState`를 구분한다.

## 2. 현재 상태

| 작업 | 상태 | 기준 문서 |
|---|---|---|
| Stage Map Core Task 1~4 | 완료된 선행 작업 | `과거 문서 2026-08-25-stage-map-core.md (Git 이력)` |
| Enemy AI MVP | 완료 | `과거 문서 2026-08-29-enemy-ai-mvp.md (Git 이력)` |
| Enemy 초기 Stage Bootstrap | 다음 구현 | `과거 문서 2026-08-30-stage-initial-enemy-bootstrap.md (Git 이력)` |
| Railway + Timeline | Enemy Bootstrap 다음 | `과거 문서 2026-08-26-stage-gimmick-timeline.md (Git 이력)` |
| Stage Map Core Task 5~7 | Railway 다음 재개 | `docs/superpowers/guides/stage-map-core-implementation-guide.md` |
| Unity Stage Data + Bootstrap 통합 | 모든 Core 계약 완료 뒤 별도 계획 | 아직 구현 계획을 만들지 않음 |

상태 표의 완료 여부는 사용자에게 전달받은 현재 구현 상태를 기준으로 한다. 각 후속 작업 시작 시 해당 계획의 검증 명령으로 저장소 상태를 다시 확인한다.

## 3. 고정 실행 순서

~~~text
1. Stage Map Core Task 1~4                    완료
2. Enemy AI MVP                               완료
3. Stage Initial Enemy Bootstrap
4. Stage Gimmick & Timeline Task 1~8
5. Stage Map Core Guide Task 5~7
6. Core 전체 회귀
7. Unity Stage Data / Bootstrap Integration
8. Run Map → Combat Stage → 전투 → Run Map 복귀 통합 검증
~~~

3번은 전체 Unity 중간 계층을 앞당기는 작업이 아니다. `InitialEnemyPlacementSpec`, `StageConfig`, `StageSessionFactory`, Player-only 공개 배치 경계만 구현한다.

4번은 Enemy 생성이나 AI 행동을 소유하지 않는다. Railway, Rail source, `CompletedUnitTurnCount`, `AdvanceTimeline()`만 구현하고 3번에서 추가된 StageConfig Enemy 입력을 보존한다.

5번은 Run Map Core 자체를 완결한다. StageSession이나 Enemy/Gimmick 타입을 Run Map에 추가하지 않는다.

7번에서 처음으로 실제 Unity Combat Stage Definition과 `StageKey` Catalog를 확정한다.

## 4. 책임과 문서 분리

| 기능 | 소유 문서 | 변경하지 않는 경계 |
|---|---|---|
| Enemy 턴 판단 | Enemy AI MVP | 초기 배치, Stage Data, Railway |
| 초기 Enemy 구성과 Core 배치 | Stage Initial Enemy Bootstrap | AI Policy, Wave/Respawn, Run Map DAG |
| Railway와 Timeline Entry | Stage Gimmick & Timeline | Enemy 구성, Run Map DAG, Unity 연출 |
| 시작부터 Boss까지 경로 | Stage Map Core | Combat Grid, Enemy, Railway Runtime |
| StageKey를 실제 StageSession으로 변환 | 미래 Unity Bootstrap 계획 | StageMapSession 내부 생성, Core의 Unity 참조 |

기능별 plan은 독립적으로 테스트 가능한 산출물을 만든다. 이 로드맵은 순서와 연결만 설명하며 개별 plan의 구현 내용을 중복하지 않는다.

## 5. StageConfig 누적 계약

각 단계는 `StageConfig`를 새로 대체하지 않고 다음 입력을 누적한다.

~~~text
현재 StageConfig
├─ MapWidth / MapHeight
├─ ReserveCapacity
├─ PlayerDeploymentDepth / EnemyDeploymentDepth
└─ TimelineSeed

Enemy Bootstrap 완료 후
└─ InitialEnemyPlacements

Railway/Timeline 완료 후
├─ RailCells
└─ StageGimmicks
~~~

최종 Core 입력은 다음 책임을 갖는다.

~~~text
StageConfig
├─ 전투 공간과 진영
├─ 초기 Enemy 배치
├─ 초기 Rail Cell
└─ Stage Gimmick 설정
~~~

`StageKey`는 StageConfig에 넣지 않는다. 상위 Bootstrap이 StageKey로 Combat Stage Definition을 조회한 뒤 StageConfig를 만든다.

## 6. 마지막 Bootstrap Gate

Core 작업이 모두 완료되기 전에는 `StageMapSession`이 `StageSessionFactory`를 직접 호출하지 않는다.

최종 연결은 별도 상위 계층이 담당한다.

~~~text
StageMapSession.TrySelectNode(nodeId)
→ StageSelectionResult.SelectedStageKey
→ Unity Combat Stage Catalog 조회
→ Combat Stage Definition
   ├─ Map / Deployment Zones
   ├─ EnemyEncounterData.InitialSpawns
   ├─ Initial RailCells
   └─ Stage Gimmick authoring data
→ StageConfig
→ StageSessionFactory.Create(config, unitCatalog)
→ 이미 초기 Enemy와 Gimmick Runtime이 구성된 StageSession
→ View가 Snapshot으로 외형 생성
~~~

이 통합의 상세 plan은 다음 조건이 모두 만족된 뒤 작성한다.

1. Stage Initial Enemy Bootstrap 전체 테스트 통과
2. Stage Gimmick & Timeline 전체 테스트 통과
3. Stage Map Core Task 5~7 전체 테스트 통과
4. 현재 `Assets/Scripts/Stage/Core/StageData.cs`의 Run Map authoring 역할과 새 Combat Stage Definition의 이름·소유 위치 확정
5. EnemyEncounterData, Rail 공간 데이터, Gimmick schedule, View 연출 데이터의 Unity 직렬화 경계 확정

## 7. 범위 팽창 방지

현재 구현하지 않는다.

- Wave, Respawn, Reinforcement, Random Spawn
- Terrain Registry, CellTag, Effect Graph
- StageMapSession 안의 StageSession 생성
- Enemy AI의 Railway 위험 예측
- 범용 Stage Repository 또는 DI Container
- 기존 `StageData`의 성급한 이름 변경

미래 확장을 위해 남기는 것은 `EnemyEncounterData` 컨테이너 경계와 `StageConfig.InitialEnemyPlacements`뿐이다. 사용하지 않는 Runtime 추상화는 만들지 않는다.

