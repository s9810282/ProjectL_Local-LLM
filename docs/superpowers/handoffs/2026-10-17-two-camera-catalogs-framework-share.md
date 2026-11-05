# 두 카메라 카탈로그 — Framework 공유 메모

2026-10-17 시작, 2026-10-18 C5까지 갱신, 미전달. 사용자 요청: 이름으로 접근하는 버추얼 카메라 / Unity Camera 전용 카탈로그 둘을 둔다. Unity 카메라는 최초 등록 때 카탈로그에서 DontDestroyOnLoad로 유지해 재사용한다.

- `CameraGroupFactory`가 새 UnityCameraCatalogSystem을 서비스·모듈로 등록한다. 기존 CameraCatalogSystem과 CameraEffectSystem의 생성·주입은 유지한다.
- `ICameraGroupServiceContext.UnityCameraCatalogSystem`과 `CameraSystemServiceKey.UnityCatalog`를 추가했다. 기존 Catalog 키와 버추얼 접근자는 그대로다. 하위 카탈로그를 런타임 루트에 직접 공개하지 않는다.
- Unity 카탈로그는 최초 BindScene에서 유효하고 로드된 씬의 게임용 Camera를 비활성 포함 이름별 수집한다. 이후 BindScene은 목록과 핸들을 유지한다. 핸들은 컬링·RenderTexture 타깃·Camera.enabled만 제공한다. 참조·설정 원본을 복제하거나 Brain/Transform 제어를 추가하지 않는다.
- 카메라 생성은 Bootstrap/씬 구성의 책임이다. C5 사용자 지시로 영속화는 Unity 카탈로그가 최초 등록 때 처리한다. 버추얼 카탈로그와 효과는 기존 수명·API를 따른다.
- 2026-10-18 C2 사용자 결정: Unity 카탈로그는 Framework CustomPool에서 UnityCameraCatalogHandle 객체 자체를 대여하고 Dispose에서 그 Handle 자체를 반환한다. 기존 인터페이스 반환 API를 유지하며 Node·struct 래퍼는 없다. 반환 후 참조는 사용하지 않는다. 동일 객체 재대여 뒤의 이전 참조와 새 참조는 구분하지 않는다. 카메라 목록·씬 바인딩과 효과·Factory/Group 계약은 유지한다.
- Core·FieldCamera·Architecture와 기존 운영 에셋은 수정하지 않았다.

등록/수명/권한·SSOT/SOLID/FW 점검, 검증 결과와 기존 로딩 연결 이슈는 [설계·Task·실행 기록](../specs/2026-10-17-two-camera-catalogs-design.md)에 있다. 사용 예시는 [카메라 카탈로그 문서](../../render-camera-catalog.md)를 참고한다.

2026-10-18 C3: 사용자 요청으로 Unity 카탈로그에서 에디터 미리보기 씬 Camera를 이름 검사 전에 제외했다. `CameraType.Game`인 미리보기 객체도 제외하며 실제 게임 카메라 이름 중복 검사는 유지한다. `EditorSceneManager.IsPreviewSceneObject`와 using은 UNITY_EDITOR 조건부다. 공개 서비스·Pool·수명·Factory 변경은 없다. PreviewScene 기반 테스트 2개 추가 및 ProjectL.Tests.Framework MSBuild 성공, 변경 소스 새 경고 없음(Logs/unity-camera-preview-filter-build.log). 실제 Unity 테스트·JMHTest 재생 확인은 대기 중이며 원래 중복 Camera의 출처는 아직 확정하지 않았다. 다른 영역·씬·미리보기 생성 코드는 변경하지 않았다.

2026-10-18 C4: 같은 오류가 지속되어 JMHTest의 SourcePrefab GUID와 FBX 실제 Model 노드를 조사했다. rock_test_002zz·smallrock·TESTMAP·ROCK1fbx·rock1_test·rock2에 Camera 모델이 포함되고 카메라 임포트가 켜져 있었다. 해당 6개 meta에서 importCameras만 0으로 바꿨으며 모델을 쓰는 모든 인스턴스에 적용된다. 전체 경로·GUID·검증은 설계 C4에 기록했다. 중복 오류에는 두 객체의 씬·계층 경로·Instance ID를 추가했고 실제 이름 중복 거부는 유지한다. 진단 테스트 추가·MSBuild 성공, 변경 소스 새 경고 없음(Logs/unity-camera-fbx-import-build.log). 정적 FBX 조사에서 모든 대상의 카메라 임포트 0을 확인했다(Logs/jmhtest-fbx-camera-audit.log). Unity 재임포트 뒤 Play 확인은 대기 중이다. Core·FieldCamera·Architecture·씬·원본 FBX·공개 서비스/수명/Factory는 이번 단계에서 변경하지 않았다.

2026-10-18 C5: 사용자 지시로 Unity 카탈로그가 이름 검증을 모두 마친 뒤 플레이 중 각 Camera의 최상위 루트에 DontDestroyOnLoad를 적용한다. 같은 루트는 한 번만 처리하며 자식 Camera의 부모·로컬 Transform·활성 상태를 유지한다. 같은 루트의 다른 오브젝트도 함께 영속화한다. EditMode·중복 등록 실패 시에는 적용하지 않는다. 공개 서비스/Handle Pool/이후 바인딩·Shutdown 정리는 그대로며 카메라 생성·파괴는 하지 않는다. UnityCameraCatalogPersistenceTests 2개와 meta를 추가하고 ProjectL.Tests.Framework MSBuild를 통과했다(Logs/unity-camera-persistence-build.log, 변경 소스 새 경고 없음). 실제 Unity 수명 테스트와 JMHTest Play는 대기 중이다. Core·FieldCamera·Architecture·씬·Bootstrap은 수정하지 않았다.
