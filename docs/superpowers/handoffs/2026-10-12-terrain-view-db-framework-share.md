# Terrain View DB Phase 1 — Framework·View 작업자 공유

작성: 2026-10-12, View 작업. 상태: 코드·에셋 연결 완료, Unity 에셋 테스트/Inspector 확인 대기. 이 메모는 전달용 파일이며 외부 발송하지 않았다.

설계: [PlacedPrefab Terrain View](../specs/2026-10-12-placed-prefab-terrain-view-design.md). Task·실행 상태: [계획 Phase 1](../plans/2026-10-12-placed-prefab-terrain-view-plan.md).

## 조립 계약 변경

- ViewTableKey.TerrainVisual과 ITerrainVisualTableQuery를 추가했다. GetTerrainVisual(TerrainContentKind), TryGetTerrainVisual(kind, out data)로 기존 종류 키를 사용한다.
- IViewQuery/ViewDataBase에 TerrainVisualTableQuery 프로퍼티가 추가됐다. 직접 IViewQuery를 구현하는 코드에는 이 프로퍼티가 필요하다. 기존 테스트 Stub 두 곳은 보완했다.
- ViewDataBaseFactory는 TerrainVisualDataTableSO를 Registry에서 가져와 Query를 ExportServices로 등록한 뒤 기존 Builder를 한 번 Build한다. 이 Query에는 Tick 수명이 없으므로 AddModule은 없다.
- 별도 테스트/개발 Catalog의 ViewGroup을 만드는 경우 TerrainVisualDataTableSO도 포함해야 한다. 항목 없는 테이블은 허용한다. 기존 프로젝트 ViewGroup.asset에는 실제 테이블을 등록했다.

## 자원과 등록 정책

TerrainVisualDataTable.asset의 Items는 Wall/Sandstorm/Tornado/PresenceModifier 네 개다. 모드는 PlacedPrefab이며 SO의 Prefab에 Default Cube를 중첩 참조하는 Cube_Terrain_* 외형을 연결했다. 임시 색은 벽 회색, 모래폭풍 모래색, 토네이도 보라, 위치 보정 청록이다. 기존 Cube/다른 환경 머티리얼은 변경하지 않았다. 후속에는 SO의 Prefab을 정식 외형으로 교체할 수 있다.

Item 타입 오류·중복 종류·등록된 PlacedPrefab의 Prefab 누락은 DB 생성 때 거부한다. 입력 SO 자체는 미완성 자원을 허용한다. GroundShape는 Prefab 필수 계약을 적용하지 않으며 현재 고지대/약한 지반/낭떠러지 세 외형은 Table에 등록하지 않았다.

Core·Buffer·InGame DB·FieldCamera·Architecture·Framework 구현은 수정하지 않았다. StageEnvironmentVisual의 기존 회전 테이블도 유지했다. Terrain 필드 표시/회전 소비는 Phase 2/3 작업이므로 아직 화면에 나타나지 않는다. 기존 운영 스테이지의 비어 있는 Terrain 배치를 채우지 않았다.

## 구조와 검증

표현 값의 원본은 View SO이며 새 Query는 불변 Data만 조회한다. 전투 상태나 위치를 Query에 보관하지 않는다(SSOT/P5~P7). 기존 Factory/서비스 등록 경로를 사용하고 신규 System/Loop/Bootstrap 연결은 없다(SOLID/JMH/FW-001/003).

Unity 참조 DLL을 사용한 외부 C# 컴파일에서 ProjectL.Systems/ProjectL.Tests.Framework 오류 0, Terrain Query 5건과 기존 Object·Obstacle DB 관리 검증 7건 통과. 네 SO/중첩 Cube/머티리얼/색, 새 GUID 12개의 유일성, 기존 사용자 Object_Mine/Object_Flag 및 Default 자원 해시 유지를 확인했다.

새 TerrainVisualDatabaseTests의 UnityEditorNative 7건과 갱신한 EnvironmentAuthoringAssetTests, 기존 DataBaseAssetTests는 컴파일까지만 확인했다. 실행 가능한 Unity Test Runner 연결이 없어 실제 네이티브 에셋 로드는 아직 실행하지 않았다. T1.U에서 에디터 컴파일/해당 EditMode 테스트와 Inspector를 확인한다. 외부 컴파일과 관리 실행은 Unity 네이티브 실행 증거가 아니다. 증거 파일은 Temp/TerrainViewPhase1/ 아래의 컴파일 로그, managed-checks.log, asset-manifest.json이다.

커밋·push 없음. Phase 1 결과 보고 후 멈추며 다음 시작점은 Phase 2다.
