# 전투 배경 씬 작업 Task 계획

> **2026-11-10 후속:** S7 배경·타일 공통 자동 배율은 [공통 계획 CS0~CS4](2026-11-10-combat-map-common-scale-plan.md)에서 Combat과 함께 진행한다. 현재는 설계·계획만 갱신했고 구현·에셋 이전은 미착수다. 기존 화면 보존 검사에 실패하면 전환하지 않는다.

- 작성: 2026-11-09. 전투 View 작업.
- 설계: [전투 배경 씬 작업 설계](../specs/2026-11-09-combat-map-authoring-scene-design.md).
- 작업 규칙: Phase마다 멈춰 보고한다. View Test 코드를 작성하지 않는다. 전투 View MSBuild를 실행하지 않는다. 이번 S3의 Unity Editor 직접 확인은 사용자 요청에 따른다. 커밋·push하지 않는다.

## S1 — Game 뷰 카메라 작업 씬

1. `Assets/Scenes/Dev/CombatMapAuthoring.unity`와 `.meta`를 만든다. Build Settings에 등록하지 않는다.
2. Combat 씬과 동일한 `CombatCamera.prefab` 인스턴스를 루트에 둔다. 원본 프리팹과 현재 구도 값은 유지한다.
3. Boot 씬 출력 카메라의 Camera·CinemachineBrain·URP 설정으로 Main Camera를 둔다. 기본 Directional Light를 둔다.
4. 정적 YAML 참조, 프리팹 GUID, 파일 ID, `git diff --check`를 확인한다. Unity에서는 씬을 열어 프리팹 링크·Game 뷰 출력·Play 전 카메라 Transform 반응을 확인한다.
5. 보고 후 멈춘다. S2는 별도 진행 지시를 받는다.

## S2 — 스테이지 선택과 Inspector 미리보기

- [x] 편집 루트와 Custom Inspector에서 운영 `ViewStageDataSO`를 선택한다. 표 등록·맵 프리팹 일치·보드 맥락을 검증한다.
- [x] 런타임 배치·타일 정렬 계산을 공유한다. 원본을 변경하지 않는 씬 편집 인스턴스를 관리한다.
- [x] 공용 카메라 설정 SO로 시작 줌·자세를 Game 뷰 상태에 적용한다. 가상 카메라 프리팹의 기준 Transform·렌즈는 덮어쓰지 않는다.
- [x] Unity에서 `PrefabWithTiles` 선택·불러오기, 보정 Undo, 씬 재열기, Game 뷰 출력을 직접 확인했다. SO 교체와 Combat Play 구도 일치는 미확인이다.

### S2 구현 기록 — 2026-11-09

- `CombatMapAuthoring` 루트에 선택 SO·불러온 SO·프리뷰 컨테이너·맵 프리팹 인스턴스 참조를 둔다. Inspector는 운영 표와 전투 Root·필드 크기·칸 간격을 기존 `CombatStageSoAssets`/`FbxCombatStageContext`에서 읽고, 명시적 불러오기 버튼에서만 프리뷰를 교체한다. 교체는 Undo 그룹으로 묶는다.
- `CombatMapAuthoringPlacement`는 배경 프리팹 인스턴스를 만들고 런타임과 같은 위치·회전·원본 배율×Scale Offset을 적용한다. PrefabWithTiles의 타일은 맵 인스턴스 밖의 형제 객체로 만들고, `CombatTileAlignmentMath`로 방향·기준 타일을 맞춘다. 옛 카메라·ImpulseListener는 씬 인스턴스에서만 끈다. 무작위 타일 외형은 StageKey 고정 시드의 참고 표시다.
- `CombatMapAuthoringCameraPose`는 Editor 조건부 Cinemachine 확장으로 공용 SO의 시작 줌·자세를 카메라 상태에만 적용한다. Unity에서 컴포넌트를 씬에 부착할 수 있도록 Runtime 폴더로 옮겼다. `CombatCameraPreviewSettings`가 설정 읽기와 시작 자세 계산을 한 곳에 두고, `FbxCombatCameraSource.ReadCamera`도 이를 사용한다. Inspector 버튼과 프로젝트 변경 시 설정을 다시 읽는다.
- 정적 확인: 새 `.cs`의 `.meta` 존재, 씬 local file ID 중복 없음, 편집 루트 스크립트 GUID·SceneRoots 참조 확인. View Test 코드와 MSBuild는 실행하지 않았다. S3 진행 중 Unity Editor에서 컴포넌트 로드·Console 오류 0을 확인했다. **Player 빌드 컴파일 미확인**.
- Unity 확인 절차: 편집 씬에서 `CombatMapAuthoring` 선택 → 운영 ViewStage SO 지정 → **스테이지 배경 불러오기** → Scene/Game 뷰를 나란히 둔다. 프랍 자식 이동 후 Game 뷰 반영, 선택 SO 변경 시 경고·교체, Ctrl+Z/Redo, 씬 저장·재열기를 확인한다. `PrefabWithTiles`면 `MapTileRoot`가 맵 프리팹 인스턴스의 형제인지, Zoom Pose의 시작 크기·자세가 Combat Play와 맞는지도 확인한다. S3 이전에는 맵 인스턴스의 `Apply All`을 사용하지 않는다.

## S3 — 원본 저장과 검증

- [x] 씬 루트에 보정 작업 값을 두고 커스텀 Inspector에서 편집한다. 같은 프리팹 인스턴스·타일을 유지하며 배치만 갱신한다.
- [x] **보정 SO 저장**은 선택한 운영 ViewStage SO의 보정 필드만 쓰고 Undo를 제공한다.
- [x] **프랍 프리팹 저장**은 맵 자식 override·추가·삭제만 원본 프리팹에 적용하도록 출처를 검사하고 표시용 루트 변환·옛 카메라 차단·타일을 제외한다. 적용 실패 시 씬 override 복원을 시도한다.
- [x] Unity에서 `PrefabWithTiles` 불러오기, 보정 Undo·SO 저장/복원, 프랍 자식 변경의 프리팹 적용, 씬 저장/재열기를 직접 확인했다. 임시 검증 변경은 원상복구했다.
- [ ] 프리팹 적용 Undo, `Prefab`/`MapDefinition` 실물 스테이지, FBX 배치 창 재열기·Combat Play 구도 비교.

### S3 구현·직접 확인 기록 — 2026-11-09

- Unity Editor에서 `Test 1` ViewStage(StageKey 10001, `PrefabWithTiles`)를 불러왔다. `Map3GrayBoxP`와 `MapTileRoot`가 형제이고 Game 뷰에 타일·배경이 함께 표시됐다. 보정 위치 Y=0.25 입력 시 맵 인스턴스 Y가 즉시 0.25가 됐고 Undo로 0으로 돌아왔다.
- `보정 SO 저장`으로 위치 Y=0.25를 실제 SO에 기록한 후 다시 0으로 저장했다. `Test 1.asset` SHA-256이 검증 전후 동일하다. `프랍 프리팹 저장`은 표시용 override만 있을 때 0건으로 종료했다. 맵 자식 `B1`의 이름을 임시로 바꿔 다시 저장하자 변경 1건이 원본 프리팹에 적용됐다. diff는 그 자식의 `m_Name` override 4줄만 보였고, 검증 전 원본 SHA-256으로 복구했다. 씬 파일 해시도 검증 전후 동일하다. Unity Refresh·씬 재열기 후 `B1`과 SO/프리팹 연결·타일·Game 뷰 표시를 확인했다. Unity Console 오류 0, 경고 0(조명 해상도 안내 로그는 있음).
- 현재 ViewStage Item 에셋 21개는 모두 mode `2`(`PrefabWithTiles`)이고, 운영 표에는 그중 20개가 등록됐다(`StageViewProp_Test2` 미등록). 런타임과 편집 코드는 mode `0`(`Prefab`), mode `1`(`MapDefinition`)도 처리한다. 후자의 실제 SO 사례 검증은 남았다. View Test 코드·MSBuild·커밋·push는 하지 않았다. **Player 빌드 컴파일 미확인**.

## S1 구현 기록

- 2026-11-09: `CombatMapAuthoring.unity`와 `.meta` 추가. Boot Main Camera의 Camera·Brain·URP·FMOD Listener 구성을 복사하고, 출력 카메라 Transform은 현재 Combat 가상 카메라 위치·회전으로 시작하도록 뒀다. Combat 씬의 `CombatCamera.prefab` 인스턴스 참조와 구도 override를 복사했고, 기본 Directional Light를 배치했다. Build Settings는 수정하지 않았다.
- 정적 확인: 씬의 local file ID 16개 중복 없음, MainCamera 태그 1개, SceneRoots 1개, 공용 카메라 prefab/Brain/URP GUID 참조 확인, LF 저장. Unity 컴파일·Game 뷰 시각 확인은 사용자 확인 대기.
- 수동 확인: 새 씬을 단독으로 열고 Game 뷰에 출력되는지, Hierarchy의 CombatCamera가 원본 prefab에 연결되는지, 편집 상태에서 가상 카메라 Transform을 조정할 때 프레임이 바뀌는지 확인한다. 배경 프랍과 런타임 시작 줌은 S2 이전 완료 기준이 아니다.

## S4 — 필드 격자 표시

1. [x] 운영 Field·Field View의 칸 수, `GridOutline` 간격, 데칼 머티리얼·선 폭·깊이를 읽는 편집 경계를 추가했다. `showGridDecal`은 편집 표시의 켜짐 조건으로 사용하지 않는다.
2. [x] 런타임 데칼과 편집 데칼의 원점·마스크·선 폭·투영 상자 계산을 `CombatGridDecalLayout`으로 모았다. 운영 표시 스위치·레이어 설정은 유지한다.
3. [x] 편집 씬 루트에 Editor 전용 `CombatMapAuthoringGridPreview`를 조립했다. 기존 S3 씬에는 Inspector 버튼으로 격자만 추가했다. 생성한 데칼·복제 재질은 저장 대상에서 제외한다.
4. [x] Unity에서 `Test 1`의 9×9, 1.5×1.5 격자가 Scene/Game 뷰에 보이고 표시 토글·씬 재열기 후 복원을 확인했다. Console 오류 0. 타일 보정 변화와 `Prefab`/`MapDefinition` 실물 스테이지·고저차 지형은 직접 재생하지 않았다. View Test 코드와 MSBuild는 실행하지 않았다. **Player 빌드 컴파일 미확인**.

### S4 구현·직접 확인 기록 — 2026-11-09

- `CombatMapAuthoring` 루트에 그리드 컴포넌트를 추가했다. 현재 `Test 1` 프리뷰를 버리지 않고 `필드 그리드 만들기`를 사용했다. 이후에는 스테이지 불러오기 때 자동 조립되고 Inspector의 `그리드 표시`와 `운영 그리드 설정 다시 읽기`를 사용할 수 있다.
- Game 뷰에서 9×9 격자선이 바닥에 투영됐다. 토글을 끄고 켜자 표시가 각각 사라졌다 복원됐다. 씬 저장·재열기 후에도 복원됐다. 씬 YAML에는 원본 GridDecal 재질과 칸 수·간격·데칼 설정만 있고 생성된 `FieldGridPreview` 오브젝트는 없다. **수치 정정:** `Test 1`은 맵 프리팹 루트 0.75와 SO Scale Offset 0.75를 곱하므로 실제 타일 간격은 `2 × 0.75 × 0.75 = 1.125`다. 운영 필드 간격 1.5와 다르다. 기존 1.5 일치 기록은 프리팹 루트 배율을 누락했다.
- 운영 `StageFieldView_Default.showGridDecal=0`과 운영 에셋은 그대로다. 편집용 데칼만 Default 렌더링 레이어를 추가로 받아 현재 타일 프리팹에 보인다. 렌더 깊이 밖의 고저차와 운영 지형 교체·Combat Play 구도 일치는 이번 확인 범위 밖이다. 커밋·push하지 않았다.

## S5 — 배경 X/Z 조작과 간격 불일치 표시

1. [x] `PrefabWithTiles`에서 무효인 `positionOffset` X/Z 입력을 빼고 높이 Y, 실제 배경 월드 X/Z, 기존 회전·배율·기준 타일 입력을 구분했다. 월드 X/Z 변경을 `mapTiles.offset`으로 역변환하는 계산은 편집 배치 경계에 뒀다.
2. [x] 같은 프리뷰를 유지하며 배경만 입력 방향으로 이동하고 타일·필드 그리드는 고정했다. 기존 Undo와 명시적 SO 저장 경계를 재사용한다. `Prefab`/`MapDefinition`은 기존 세 축 위치 입력을 유지한다.
3. [x] 타일 루트의 실제 축별 간격과 운영 필드 간격을 Inspector에 함께 표시하고, 불일치 시 경고한다. Combat 씬과 동일하게 필드 그리드 간격·맵 배율 데이터는 자동 수정하지 않는다.
4. [x] Unity에서 현재 `Test 1`의 1.125 대 1.5 경고와 X 입력 방향·그리드 고정·원복을 확인했다. Z·Undo 명령·실제 SO 저장은 이번 단계에서 실행하지 않았다. View Test 코드·MSBuild·커밋·push는 하지 않았다. **Player 컴파일 미확인**. 완료 후 보고하고 멈춘다.

### S5 구현·직접 확인 기록 — 2026-11-09

- `Test 1`의 맵 프리팹 `Map3GrayBoxP` 루트 배율은 0.75이고 SO Scale Offset도 0.75다. Unity 씬의 `MapTileRoot.localScale`이 0.5625인 것을 확인했다. 타일 간격은 `2 × 0.5625 = 1.125`이며 운영 Field View의 `outlineCellSpacing`은 1.5다. S4의 간격 일치 기록을 위에서 정정했다.
- Inspector의 `배경 월드 X/Z` X를 -2.25에서 -1.25로 바꾸자 배경만 오른쪽으로 이동하고 9×9 그리드는 필드 원점에 남았다. 작업 값의 `타일 오프셋 (원본)`이 변했다. X를 -2.25로 다시 입력해 원래 오프셋 (-1, -2, 0.5)과 화면 구도를 복원했다. SO 저장 버튼은 누르지 않아 운영 에셋은 이 확인에서 변경되지 않았다.
- 편집 그리드는 런타임과 같은 필드 간격을 사용한다. 스크립트 재로드 직후 그리드 표시가 사라졌지만 Inspector의 `운영 그리드 설정 다시 읽기`로 복원됐다. 자동 복원은 별도 후속 확인이 필요하다. Unity Editor에 새 Inspector가 표시된 것을 확인했으며 **Player 컴파일 미확인**이다.

### S5 수평 입력 보완 — 2026-11-09

- [x] `배경 월드 X/Z`의 두 번째 축이 Unity `Vector2Field`에서 Y로 보이는 혼동을 없애고 X·Z를 각각 표시했다.
- [x] X만 조정할 때 회전 역변환이 만든 극소 로컬 Z 성분을 제거하고, 기존 극소 오프셋도 0으로 정리한다. 의미 있는 2축 회전 성분은 보존한다. 저장·런타임 계약은 유지한다.
- [ ] `Test 1`의 180° 회전에서 X 조정 전후 Z 입력·타일 오프셋 Z가 0인지 사용자가 Unity에서 확인한다. View Test 코드·MSBuild·커밋·push는 하지 않았다. **컴파일 미확인**.

## S6 — 유닛 표면 높이 프리뷰

1. [x] 등록된 유닛 외형 중 한 개와 필드 칸을 선택하는 편집 상태·Inspector를 추가한다. 기본은 MainCharacter 외형과 필드 중앙 칸이다.
2. [x] 현재 작업 배치와 타일 세트의 `SurfaceHeight`로 유닛 발 Y를 계산하고, 필드 중심 X/Z와 운영 `fieldUnitScale`로 씬 프리뷰를 배치한다. 보정·칸·외형 변경 시 갱신하고 프리팹 저장 범위 밖에 둔다.
3. [x] 맞춤 실패·에셋 누락·타일 밖 높이 0을 구분해 표시한다. 정적 코드·에셋 참조를 확인한다. Unity에서는 사용자가 타일 높이가 다른 칸, 씬 재열기, Undo, 보정 변경을 확인한다. View Test 코드·MSBuild·커밋·push는 하지 않고 **컴파일 미확인**으로 보고한다.
4. 완료 후 보고하고 멈춘다.

### S6 코드 반영 — 2026-11-10

- `CombatMapAuthoringRoot`는 유닛 외형 키·칸·프리뷰 인스턴스·계산 높이만 보유한다. Inspector의 유닛 영역은 MainCharacter를 기본 선택하고 등록된 유닛 외형 목록과 필드 칸을 받는다. 프리뷰는 루트 아래 `SurfaceUnitPreview`로 생성하여 맵 프리팹의 저장 범위에서 제외한다.
- 배치기는 현재 작업 회전·실제 맵 배율·기준 타일을 기존 `CombatStageSoTileFit`에 전달하고 타일 세트 SO의 `SurfaceHeight`를 읽는다. 타일 밖은 높이 0으로 안내한다. 외형·칸·보정·프로젝트 변경·Undo 때 갱신하고 반복 Inspector 표시에서는 재조회하지 않는다.
- 이번 단계는 정적 참조만 확인했다. Unity에서 편집 씬의 `CombatMapAuthoring` 루트를 선택해 프리뷰가 나타나는지, 높이가 다른 두 칸의 유닛 발 Y, `fieldUnitScale`, 배경 보정 뒤 갱신, 씬 저장·재열기와 Undo를 확인해야 한다. View Test 코드·MSBuild·커밋·push는 하지 않았다. **컴파일 미확인**.
- 현재 `Test 1` 타일 배치는 종류 5·6·7이고 세 종류의 `SurfaceHeight`가 모두 0이다. 높이 차이는 높이가 0이 아닌 종류가 놓인 스테이지/칸에서 확인해야 한다.

## S7 — 공통 배율 이전 (미착수)

- [ ] 공통 계획 CS0: 현재 운영/편집 표시 결과와 미저장 씬 작업을 기록하고 전환 가능성을 분류한다. 이미 어긋난 타일을 이 작업에서 자동 보정하지 않는다.
- [ ] CS1~CS2: 제작 규격 2·OutlineCellSpacing 기반 공용 계산과 무손실 이전 미리보기를 준비한다. 원본 에셋은 아직 쓰지 않는다.
- [ ] CS3: 결과 수락 뒤 Combat·Authoring·기존 편집기/FBX 프리뷰를 함께 전환한다. 타일 방식 수동 배율/TileSize 입력을 정리하고 명시적 이전을 수행한다.
- [ ] CS4: 양쪽 화면의 이전/이후 결과, 높이·칸 대응·저장/Undo·재열기를 확인하고 구형 필드를 정리한다. 각 Phase 종료마다 멈춘다.

단계 내용은 공통 계획을 원본으로 삼아 여기서 별도 순서를 만들지 않는다. TileOffset X/Z 읽기 전용·Y 편집 UI는 앞선 사용자 요청으로 코드에 반영됐으나 공통 배율 전환 완료를 뜻하지 않는다. 이번 갱신은 문서만이며 **컴파일 미확인**이다.
