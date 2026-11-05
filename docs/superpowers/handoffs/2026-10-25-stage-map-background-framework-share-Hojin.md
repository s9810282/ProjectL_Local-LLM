# StageMap 챕터 배경 DB 연결 공유 — 2026-10-25

- 작성자: 호진(이호진)

상태: Phase 2 구현 및 사용자 확인 완료(2026-10-25). 사용자 보고로 컴파일 오류 없음·배경 정상 표시·전투 왕복 후 중복 없음 확인. 외부 미전달.

근거: [스테이지 맵 설계](../specs/2026-10-10-stage-map-design-Hojin.md), [스테이지 맵 계획](../plans/2026-10-10-stage-map-plan-Hojin.md)(둘 다 2026-10-27에 옛 배경 설계·Task 계획을 합친 문서). 사용자가 이번 배경 작업을 Codex 단독으로 진행하고 Phase 2까지 연결하도록 승인했다.

## 운영 계약과 계층

- 생성: `ViewDataBaseFactory.Create`가 `ViewGroup`의 `StageMapBackgroundDataTableSO`로 `StageMapBackgroundTable`을 만든다.
- 내부 등록: 테이블의 `ExportServices`가 View DB 내부 Builder에 `IStageMapBackgroundTableQuery` 하나를 등록한다. `ViewTableKey.StageMapBackground`로 조회한다.
- 상위 공개: `ViewDataBase`가 Query를 보관하고 기존 `IViewQuery`의 `StageMapBackgroundTableQuery`로 노출한다. 상위에는 기존 `IViewQuery`만 Export한다. 새 하위 Query의 루트 재등록은 없다.
- 소비: `StageMapViewSystem.InitData` → 두 Scope 조립 경로 → `StageMapBackgroundDisplay`. 새 Runtime 시스템·Job·입력 서비스는 없다.
- 수명: 기존 맵 준비 완료 시 `Show(chapterKey, BackgroundRoot)`, 노드 상태 변경 시 유지, Scope 정리에서 Dispose. 같은 챕터·프리팹·부모는 재사용한다.
- 씬 계약: `StageMapSceneRoot.backgroundRoot` 필수. `MapRoot`와 서로 다른 형제 Transform이어야 한다.
- DB 계약: 챕터 키가 원본이다. 등록되지 않은 키는 Get에서 실패하며 1챕터로 대체하지 않는다. 실제 등록은 1챕터 하나다.

## 에셋 이전

- 프리팹: `Assets/ProjectL/Prefabs/StageSelect/Backgrounds/StageMapBackground_Chapter1.prefab`.
- 항목: `Assets/ProjectL/DataBaseSO/ViewGroup/StageMapBackground/Item/StageMapBackground_Chapter1.asset`.
- 테이블: 같은 그룹의 `StageMapBackgroundDataTable.asset`. 운영 `ViewGroup.asset`에 연결했다.
- Probs의 GameObject 143개·Transform 143개·MeshFilter 111개·MeshRenderer 111개를 옮겼다. 프리팹 루트 이름만 바꾸고 나머지 직렬화 블록 507개를 그대로 유지했다. 메시·머티리얼·Renderer·Transform 참조와 값도 같다.
- 씬의 원래 Probs 루트는 빈 BackgroundRoot로 재사용하고 StageMapSceneRoot 아래 MapRoot 옆에 뒀다. 원래 자손 506블록을 씬에서 제외했다. 나머지 기존 씬 블록 중 바뀐 것은 루트 GameObject/Transform, 부모 Transform, SceneRoot 컴포넌트, SceneRoots의 5개뿐이다.
- 배경은 런타임 DB 조회로 생성한다. 편집 모드의 빈 BackgroundRoot는 의도된 상태다.

## 기존 시험 계약 호환 조정

새 시험·시험 도우미를 추가하거나 실행하지 않았다. 새 필수 Query 때문에 기존 IViewQuery 구현 7개에 프로퍼티를 추가했다: ArtifactPopupConfigTests, GlobalInputCompositionTests, LobbyInputTests, LobbyViewSystemTests, UnityStageInputTests, ViewInputCompositionTests, ViewPopupCompositionTests.

직접 조립하는 기존 4개 파일에는 배경 Query와 BackgroundRoot를 연결했다: StageMapViewSystemTests, RecoveryStageMapViewTests, DevelopmentNavigationIntegrationTests, CombatEntryIntegrationTests.MapMovement. 기존 StageMapViewSystemTests의 2챕터 사례는 기존 DataBaseTestAssets로 임시 SO를 만들고 같은 프리팹을 지정한다. 이는 시험 입력이며 운영 2챕터 등록이나 기본 배경 정책이 아니다. 단언·사례·새 가짜 서비스는 추가하지 않았다. 실제 실행 결과는 미확인이다.

## 원칙 검수와 경계

- SSOT: 챕터→프리팹 연결은 View DB 항목 하나가 소유한다. 진행 키는 기존 Buffer에서 읽고 InGame DB를 읽거나 Core에 쓰지 않는다.
- SOLID: Data/조회/표시/조립을 분리했다. 표시 객체는 좁은 Query 계약을 받으며 노드·카메라·입력·Job을 참조하지 않는다. 기존 수명 계약을 유지하고 새 배경은 데이터 항목으로 추가한다.
- FW: 기존 DB 생성·내부 등록·상위 공개·조회 경로를 따른다. IDisposable 내부 객체는 별도 IServiceModule로 등록하지 않는다.
- 독립성: 다음 배경 추가는 프리팹과 테이블 입력으로 끝난다. 이번 필수 서비스 추가로 IViewQuery 구현과 직접 조립 fixture는 함께 맞췄다.
- Core·FieldCamera·Architecture·LifeCycle Job/Plan/Flow는 수정하지 않았다. 작업 전후 보호 경로 158개 중 Claude 설계 문서만 외부에서 갱신됐으며 덮어쓰거나 되돌리지 않았다. 나머지 157개는 최초 해시와 같았다.
- 정적 대조와 사용자 실행 확인을 구분한다. 2026-10-25 사용자가 Unity에서 컴파일 오류 없음, 배경 정상 표시, 전투 왕복 후 중복 없이 유지됨을 확인하고 완료 기록을 요청했다. Codex의 직접 실행 결과는 아니다.
- 상점·회복 후 상태, 별도 씬 이탈 정리, 콜백 재진입·실패 주입은 개별 확인 결과가 보고되지 않아 미확인으로 유지한다. 상세 출처와 범위는 계획의 P2 사용자 확인 완료 기록을 따른다.
