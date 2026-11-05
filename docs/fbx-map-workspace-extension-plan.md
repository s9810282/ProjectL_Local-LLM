# 맵 작업실 확장 설계와 Task 계획

사용자 승인: 2026-10-25. 각 Phase를 구현·검증한 뒤 멈추고 보고한다.

## 설계

1. 작업실의 버추얼 카메라는 오소그래픽으로 고정한다. 원근 전환·FOV 입력을 제거한다. 기존 원근 카메라는 작업실에서 열 때 정규화한다. 화면 맞추기는 위치·회전만 복사하고 직교 크기는 유지한다.
2. FBX와 일반·Variant Prefab을 카탈로그, 직접 선택, 드래그앤드롭으로 배치한다. 배치본은 중첩 연결까지 완전히 Unpack한다. 원본 에셋을 수정하지 않고 맵 저장본에 독립 배치 데이터로 보관한다.
3. 작업실 Scene View에서 Global Volume은 아이콘·이름, Local Volume은 Collider 경계·이름으로 표시하고 클릭 선택을 제공한다.

## 경계와 구조 검토

- 편집 전용 `Tool/Editor/FbxPlacement*`와 해당 테스트에 한정한다. 런타임 Core·FieldCamera·Architecture 조립은 변경하지 않는다.
- SSOT: 배치 가능 에셋 판정은 `FbxPlacement`가 소유한다. 카탈로그·선택·드롭은 같은 판정을 사용하며 실제 생성·Unpack·Undo는 단일 경로로 처리한다.
- SOLID: 에셋 검증·생성은 배치 기능, 부모·프리뷰 씬 수명은 Workspace, UI 입력과 선택은 Window가 맡는다. 기존 공개 FBX API는 유지한다. 사용하지 않는 추상 인터페이스나 서비스 등록을 추가하지 않는다.
- JMH/FW: 이번 변경은 에디터 도구이며 FrameworkRuntime·Builder·서비스 등록·게임 입력/UI 수명을 사용하지 않는다. 기존 공개 Unity 편집 API와 작업실 소유 프리뷰 씬을 통한다.
- 기능 독립성(공동 인계 §4.6): 배치 종류 추가 전에는 검증기·카탈로그·선택/드롭이 FBX를 개별 가정했다. 이후에는 지원 에셋 판정 한 곳과 배치 테스트가 기준이며 카메라·조명·베이크 구현을 수정하지 않는다. 부모/표면/Undo/맵 저장은 남는 공통 경계이므로 이 경계의 회귀는 확인한다.

## Phase 1 — FBX/Prefab 공통 배치

- [x] 지원 확장자와 에셋 루트 검증 통합; 기존 FBX 전용 API 호환 유지.
- [x] 카탈로그·선택 필드·드롭을 공통 배치에 연결; 원본 Volume Profile 복사 정책 유지.
- [x] 일반/Variant·중첩 Unpack, 원본 보존, 표면·레이어·Undo/Redo·맵 저장 왕복 테스트 6건 추가.
- [x] 사용자 안내 갱신 및 툴·테스트 프로젝트 MSBuild 컴파일 확인.
- [ ] Unity Editor 테스트 실행 결과 및 수동 드래그 동작 확인.

## Phase 2 — 오소그래픽 고정 (대기)

- 생성·열기·편집·화면 맞추기의 렌즈 규칙 통합.
- 기존 카메라 정규화와 저장 왕복 확인.

## Phase 3 — Volume Scene View 표시 (대기)

- Global/Local 기즈모와 클릭 선택 구현.
- Collider 형태 및 선택·프로필 편집 동기화와 Unity 화면 검증.

## 실행 기록

- 시작 시 기존 변경: FMOD Event Catalog와 FMODStudioCache 각 1개. 이번 작업과 무관하다.
- Phase 1 구현: `GetPlacementAsset`을 선택·드롭·생성의 공통 검증으로 연결했다. 카탈로그 파일 형식도 같은 클래스가 제공한다. 기존 `GetModelAsset`·`AddFbx`·`AddFbxOnSurface`는 FBX 전용으로 유지하고, 공통 Workspace API에 위임한다.
- 기존 직렬화 선택 필드 `fbx`는 창의 선택값 호환을 위해 이름을 유지했다. UI에는 `배치 에셋`으로 표시한다.
- 프리팹 배치 후 기존 Volume 바인딩을 호출해 원본 Profile 대신 작업실 복사본을 편집한다.
- 컴파일: VS 2022 MSBuild로 `ProjectL.Tool.Editor.csproj`, `ProjectL.FbxPlacement.Editor.Tests.csproj` 빌드 성공(오류·경고 출력 없음). 변경 파일의 `git diff --check` 통과.
- Unity 실행 검증은 미확인이다. 배치 모드 실행은 기존 Project_L 에디터와의 프로젝트 잠금 충돌로 테스트를 시작하지 못했다(`Logs/fbx-phase1-unity.log`). 컴퓨터 제어로 기존 에디터의 Test Runner를 열었지만 테스트 결과는 확보하지 못했다. 실행 통과로 간주하지 않는다.
- 다음 리뷰 전 확인: EditMode에서 `ProjectL.FbxPlacement.Editor.Tests.FbxPlacementTests` 실행(신규 6건 포함), 에셋 탭의 일반/Variant 프리팹 더블클릭과 Project/카탈로그에서 작업 화면 드래그, Undo/Redo·맵 저장 후 재열기. Phase 2·3은 미구현이다.
