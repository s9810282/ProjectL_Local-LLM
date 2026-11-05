# 버추얼 / Unity Camera 카탈로그 분리

2026-10-17. 사용자 결정: 카메라 GameObject 이름으로 조회하며 Cinemachine 버추얼 카메라와 Unity Camera를 별도 카탈로그로 관리한다. Main Camera는 시작부터 DontDestroyOnLoad로 유지해 재사용한다. Render Camera의 목적은 후처리다.

## 설계

| 담당 | 대상 / 책임 | 수명 |
|---|---|---|
| 기존 CameraCatalogSystem | CinemachineVirtualCamera 이름 조회, Priority·구도·렌즈 핸들 | 기존 씬 바인딩 계약 유지 |
| 새 UnityCameraCatalogSystem | Unity Camera 이름 조회, 컬링·출력 RenderTexture·enabled 핸들 | 최초 BindScene에서 한 번 수집, 이후 BindScene은 유지 |
| CameraEffectSystem | 기존 버추얼 카탈로그를 사용한 줌·흔들림 | 기존 계약 유지 |
| CameraGroup / Factory | 두 카탈로그와 효과 시스템의 서비스 등록·수명 전달 / 생성·주입 | 런타임 |

### C1 — 이번 Phase의 계약

1. 두 카탈로그는 각각 독립된 이름 공간을 쓴다. 이름은 GameObject.name이며 Ordinal로 비교한다. 같은 카탈로그 안에서 로드된 씬 전체의 중복 이름은 오류다. 다른 종류의 카메라는 같은 이름을 쓸 수 있다.
2. Unity 카탈로그는 최초 BindScene에서 비활성 Camera도 포함해 수집한다. 유효하고 로드된 씬의 게임용 Camera만 대상으로 한다. 에디터 미리보기 씬의 Camera는 Game 타입이어도 이름 검사 전에 제외한다(C3). 중복 검증이 끝난 뒤 목록을 설치한다. 추가 씬 바인딩에서는 이 목록과 기존 핸들을 유지한다.
3. **2026-10-18 사용자 지시로 C5에서 변경:** Unity 카탈로그가 최초 등록 검증을 마친 후 플레이 중 각 Camera의 최상위 루트에 DontDestroyOnLoad를 적용한다. 같은 루트는 한 번만 처리하고 자식의 부모·Transform·활성 상태를 보존한다. 카메라 생성·파괴·MainCamera 태그 변경은 하지 않는다. 사용할 출력 카메라는 최초 BindScene 전에 준비해야 한다. EditMode에서는 영속화하지 않으며 중복 이름 등록 실패는 루트를 이동하지 않는다.
4. Unity 핸들은 SetCullingMask(int), SetTargetTexture(RenderTexture), SetEnabled(bool)를 제공한다. Unity Camera 참조나 Brain을 노출하지 않고 Transform·렌즈·구도 선택·후처리 효과 값을 조작하지 않는다. URP 효과와 두 출력의 동기화는 별도 기능이다.
5. Dispose는 해당 핸들 대여를 종료하고 Handle 자체를 풀에 반환한다. 카메라를 끄거나 설정을 복구하지 않는다. 씬 재연결은 유지 카메라의 대여를 끝내지 않는다. 대상 파괴 시 조작을 거부하며 Shutdown은 미반환 Handle을 일괄 회수한다.
6. **2026-10-18 사용자 결정으로 C2에서 변경:** Framework CustomPool<UnityCameraCatalogHandle>에서 Handle 객체 자체를 대여·반환한다. GetHandle은 풀에서 받은 Handle을 바인딩해서 그대로 반환하고 Dispose는 자기 자신을 반환한다. Node나 값 형식 래퍼를 두지 않는다. 반환 시 Camera·반환 소유자 참조를 비운다. 반환한 참조는 사용하지 않는 계약이다. 같은 객체가 다시 대여될 수 있으므로 재대여 뒤의 이전 참조와 새 참조는 구분하지 않는다. 풀에 여유가 없을 때만 Handle을 생성하며, 워밍업한 동시 대여 수 범위에서는 재사용한다.
7. CameraGroup에 UnityCameraCatalogSystem 접근자, CameraSystemServiceKey에 UnityCatalog 키를 추가한다. 기존 Catalog 키·CameraCatalogSystem 접근자와 버추얼 타입·소비자는 유지한다.

### 구조 점검

- **SSOT:** Unity 카메라의 실제 컬링·타깃·enabled가 원본이다. 카탈로그에는 설정 사본이나 MainCamera 선택 상태를 두지 않는다. 버추얼 구도·렌즈는 기존 Cinemachine 경로가 관리한다.
- **SOLID:** 서로 다른 타입·수명의 카탈로그를 분리한다(SRP). 기존 효과는 버추얼 인터페이스만 받으며 새 Unity 기능에 의존하지 않는다(ISP/DIP). 새 Unity 카탈로그는 기존 서비스의 계약을 변경하거나 대체하지 않는다(LSP). 설정 기능은 해당 핸들에서 확장한다(OCP).
- **FW-001~003:** 새 서비스는 전용 IServiceModule 인터페이스로 등록하고 BindScene/Shutdown 모듈로도 등록한다. Builder는 기존처럼 한 번 Build한다.
- **변경 독립성:** 출력 텍스처·컬링 기능 변경은 Unity 카탈로그/핸들과 해당 테스트에 머문다. 기존 줌·흔들림과 Lobby/StageMap의 버추얼 제어 파일은 수정할 필요가 없다. 공통 변경 지점은 Factory·Group·서비스 키의 조립뿐이다.
- Core·FieldCamera·Architecture·운영 씬 에셋은 수정하지 않는다.

## Task 계획 — Phase C1

- [x] Unity 카탈로그 서비스와 핸들, `.meta` 추가.
- [x] CameraGroupFactory·CameraGroup·CameraSystemServiceKey 연결.
- [x] 이름 중복·타입 분리·재바인딩 핸들 보존·파괴/Shutdown 무효화와 설정 제어 테스트 8개 작성·컴파일.
- [x] 기존 효과 연결 테스트의 새 접근자 확인과 서비스 getter 수 갱신. 기존 루트 조립 관리 테스트 3개 통과.
- [ ] Unity 네이티브에서 새 카탈로그·기존 버추얼/효과 테스트 실행. 다른 Unity 인스턴스의 프로젝트 점유로 자동 실행 불가.
- [x] 관련 컴파일·실행 결과와 Unity 확인 대기를 구분해서 기록.
- [x] 카메라 사용 문서·Framework 공유 메모 갱신. C1 코드 리뷰 지점에서 보고.

## 시작 시 확인한 기존 연결

기존 버추얼 카탈로그의 PostBindService는 루트 resolver에서 GlobalSystemGroupServiceKey.Loding을 직접 조회하고 AfterLoad에 재수집을 등록한다. Bootstrap은 Stage/Combat/Lobby의 SceneEnterLoaded(-190)에서도 CameraGroup.BindScene을 호출한다. 이 경로 정리는 카탈로그 두 종류 추가와 별개의 기존 연결 문제이며, 이번 C1에서 임의로 수정하지 않는다. 검증에서 이 경로가 실패하면 기존 상태와 새 코드 결과를 구분해 기록한다.

## Task 계획 — Phase C2: Unity 카메라 핸들 Pool (2026-10-18)

사용자 요청: 반복 new를 Pool로 관리한다. 대상은 C1에서 추가한 Unity 카탈로그다.

- [x] UnityCameraCatalogHandlePool과 meta 추가. 기존 Framework CustomPool을 재사용한다.
- [x] GetHandle에서 Handle 자체를 꺼내고 Dispose에서 Handle 자체를 반환한다. 기존 IUnityCameraCatalogHandle 반환 API를 유지한다.
- [x] 카탈로그가 활성 Handle 대여를 추적하고 Dispose/Shutdown에서 반환한다. 반환 시 Handle.Clear로 Camera·반환 소유자 참조를 비운다. 카탈로그 세대값은 제거한다.
- [x] Handle 객체 재사용, 동시 대여 분리, 반환 직후 조작/중복 반환 거부, Shutdown 반환과 워밍업 후 할당 회귀 테스트를 추가한다. 기존 C1의 씬 바인딩·출력 제어 계약을 유지한다.
- [x] 컴파일과 가능한 실행을 확인하고 문서를 갱신했다. C2 코드 리뷰 지점에서 보고한다.
- [ ] UnityCameraCatalogSystemTests 10개를 Unity 에디터에서 실행한다. 현재 프로젝트 잠금이 다른 프로세스에 잡혀 있다.

SSOT/SOLID/FW 점검: 카메라의 실제 출력 설정은 Camera가 소유한다. 카탈로그는 대여 중 Handle 집합을, Handle은 Camera와 반환 소유자 참조만 보관한다. Pool은 Handle 생성·반환 시 정리만 담당한다. 사용자는 Handle 직접 재사용을 선택했으며 반환 뒤 참조 사용을 금지한다. 기존 서비스 인터페이스와 모듈/Builder 등록은 유지하며 Framework Pool 자체는 변경하지 않는다(FW-001~004/014). 변경 범위는 Unity 카탈로그 내부와 해당 테스트·문서이며 버추얼 카탈로그·효과·View 기능·Factory/Group·Core에 확장하지 않는다. 인터페이스 반환도 클래스 참조여서 박싱하지 않는다.

## 실행 기록

- **C1 코드 완료, Unity 기능 확인 대기.** 기존 버추얼 카탈로그의 파일·API·소비자와 카메라 효과의 주입은 수정하지 않았다. 새 Unity 카탈로그 소스 2개와 폴더, 테스트 1개에 `.meta`를 추가했다.
- **컴파일 성공:** VS 18 MSBuild / UseSharedCompilation=false로 ProjectL.Tests.Framework.csproj와 참조 어셈블리를 빌드했다. 새 파일은 Temp/two-camera-catalogs-sources.targets를 CustomAfterMicrosoftCommonTargets로 반영했고 Unity 생성 csproj는 수정하지 않았다. 오류 0건, 새 소스/수정 연결 파일의 경고 0건. 기존 Core nullable 및 UI/테스트 필드·중복 using 경고는 남아 있다. 빌드 로그는 로컬 Logs/two-camera-catalogs-build.log에 있다.
- **기존 관리 테스트 3/3 통과:** LifeCycle 단독 루트 조립, 바인딩 전 CombatProgress 읽기 거부, 전체 Factory의 루트/하위 Context 노출 구분을 Unity 내장 Mono에서 실행했다. 전체 Factory 조립에는 새 Unity 카탈로그 생성·서비스 조회가 포함된다. Unity 네이티브 기능 검증과 구분한다.
- **새 테스트 8개는 작성·컴파일만 확인:** 실제 Camera 설정, 재바인딩 핸들 유지, 세대 수명, 타입/이름 공간 분리는 Unity Camera 생성과 씬 검색이 필요하다.
- **Unity 자동 실행 시도:** batchmode/nographics EditMode로 새 카탈로그, 기존 버추얼 카탈로그, Play 진입 통합을 제외한 기존 효과 테스트를 선택했다. Unity가 `Fatal Error! It looks like another Unity instance is running with this project open.`으로 테스트 시작 전에 종료했고 결과 XML은 생성되지 않았다. 열려 있는 에디터나 잠금 파일을 변경하지 않았다. 로그: 로컬 Logs/two-camera-catalogs-unity.log. 사용자 에디터에서 이 선택을 실행해야 한다.
- 기존 버추얼 카탈로그의 루트 Loding 직접 조회와 AfterLoad 재수집/Bootstrap 씬 진입 재수집 중복은 위의 기존 연결 항목대로 남겨 두었다. 이번 성공은 그 경로의 실행 성공을 뜻하지 않는다.
- 시작 시 수정돼 있던 JMHTest.unity·FMODStudioCache.asset은 편집하지 않았다. Core·FieldCamera·Architecture·Bootstrap·씬 변경 0건. 문서와 Framework 공유 메모는 준비했으며 외부 전달·커밋·push하지 않았다.

### C2 실행 기록 (2026-10-18)

- **Handle 자체 Pool 코드 완료.** UnityCameraCatalogHandlePool이 Framework CustomPool<UnityCameraCatalogHandle>을 소유하고 여유 Handle이 없을 때만 생성한다. GetHandle은 대여한 객체를 그대로 바인딩·반환한다. Handle.Dispose는 반환 소유자에게 this를 전달하고, 카탈로그의 활성 대여 집합에서 제거한 뒤 같은 Handle을 풀에 반환한다. 반환 시 Camera·반환 소유자를 비운다. Shutdown도 미반환 Handle을 회수하고 풀은 유지한다.
- 기존 인터페이스 반환 API를 유지한다. 별도 Node/struct 래퍼·세대값은 없다. 사용자는 실제 Handle 객체 대여·반환을 명시했고, 반환한 참조는 재사용하지 않는 계약이다. 다른 대여가 시작되기 전의 중복 Dispose는 참조 정리로 무시한다. 재대여 뒤에는 이전 참조와 새 참조를 식별하지 않는다.
- **컴파일 성공:** ProjectL.Tests.Framework와 참조 어셈블리 오류 0건. VS 18 MSBuild / UseSharedCompilation=false 및 새 소스만 반영하는 Temp targets를 사용했다. 새 Pool/변경 Unity 카탈로그·테스트의 경고 0건. 로그: 로컬 Logs/unity-camera-handle-pool-build.log.
- **Pool 테스트 4/4 통과:** Unity 내장 Mono에서 실제 Framework Pool·Handle 구현을 사용하는 UnityCameraCatalogHandlePoolTests를 실행했다. 동일 Handle 재대여, 동시 대여 객체 분리, Dispose의 this 반환과 소유자 정리·중복 반환 방지, 워밍업 뒤 256회 풀 대여·반환의 관리 메모리 할당 0을 확인했다. 로그: 로컬 Logs/unity-camera-handle-pool-tests.log.
- **Unity Camera 통합 테스트 10개는 컴파일 확인·실행 대기:** 전체 GetHandle 경로의 할당, 실제 출력 제어, Shutdown 뒤 재대여, 씬 바인딩 보존은 Unity 네이티브 실행이 필요하다. Temp/UnityLockfile을 읽기·배타 열기로 확인했으며 다른 프로세스가 사용 중이라는 IOException을 받았다. 열려 있는 Unity나 잠금 파일은 건드리지 않았다. C1의 같은 잠금으로 인한 batchmode 실패 뒤 재실행하지 않았다.
- 변경 범위는 Unity 카탈로그 2개 소스 수정·HandlePool 1개 소스 추가, 해당 테스트와 문서다. 버추얼 카탈로그·효과·Factory/Group/서비스 키·Bootstrap·Core·씬은 C2에서 추가 수정하지 않았다. 새 Pool·테스트에는 meta를 추가하고 기존 파일의 GUID는 유지했다. 커밋·push하지 않았다.

### C3 — 에디터 미리보기 카메라 제외 (2026-10-18)

사용자가 `Camera` 이름 중복 오류를 보고한 뒤 미리보기 카메라 제외를 명시적으로 요청했다. Unity 카탈로그의 수집 루프에서 `EditorSceneManager.IsPreviewSceneObject`를 이름 검사 전에 적용했다. Game 타입·활성 여부와 무관하게 미리보기 씬을 제외하고 실제 게임 카메라 중복 거부·최초 바인딩·Handle Pool 계약은 유지한다. UnityEditor 참조는 `UNITY_EDITOR` 조건부다.

- [x] 카탈로그 수집 필터와 사용 문서·공유 기록 반영.
- [x] 실제 PreviewScene의 Game Camera 조회 거부, 실제 출력과 중복된 활성/비활성 미리보기 Camera 제외 테스트 2개 추가·컴파일.
- [x] ProjectL.Tests.Framework 및 의존 어셈블리 MSBuild 성공. 변경 소스 새 경고 없음. 로그: Logs/unity-camera-preview-filter-build.log.
- [ ] Unity EditMode에서 두 새 테스트와 기존 실제 카메라 중복 거부 테스트 실행, JMHTest 재생 확인. 열린 에디터의 프로젝트 잠금 때문에 별도 Unity 인스턴스를 실행하지 않았다. 원래 오류의 두 Camera가 미리보기였는지는 실행 확인 전까지 미확정이다.

수집 대상 판정은 카탈로그 한 곳에 두고 카메라 이름·실제 상태의 원본은 그대로 유지한다(SSOT/SRP). 기존 조회 인터페이스·등록·수명·Pool 반환을 바꾸지 않는다(ISP/LSP, FW-001/002/014). 변경은 Unity 카탈로그와 해당 테스트·문서에 한정하며 생성 코드·Factory 조립·Core·FieldCamera·Architecture·씬은 수정하지 않았다(기능 변경 독립성).

### C4 — JMHTest FBX 카메라 임포트 정리 (2026-10-18)

사용자가 C3 뒤에도 같은 중복 오류를 보고했다. 씬 본문에 Camera가 없는 것만으로 런타임 생성이라고 추정한 앞선 분석은 불충분했다. JMHTest의 모든 m_SourcePrefab GUID를 모델 에셋으로 역추적하고 FBX 바이너리의 Model/연결 노드를 읽어 다음 6개에 이름 Camera인 실제 Camera 모델과 importCameras: 1이 있음을 확인했다. 단순 문자열 Camera 검색은 FBX 템플릿에도 일치하므로 실제 Model 타입이 Camera인 노드만 조사했다.

| FBX 경로 | GUID |
|---|---|
| Assets/ProjectL/Script/Render/Shader/View/NotUse/Background scene test/rock_test_002zz.fbx | f4fecf1ac93f9274f99b903426395a5f |
| Assets/ProjectL/Script/Render/Shader/View/NotUse/Background scene test/smallrock.fbx | 9f281dc2161dfa746ab8cba5048b02e6 |
| Assets/ProjectL/Script/Render/Shader/View/NotUse/Test/New Folder/TESTMAP.fbx | f235c377d6b73e94eb68461f9ff436ee |
| Assets/Scripts/Render/Shader/Background Test/ROCK1fbx.fbx | 1270cb4bdc61d484cbb3660f02bc225a |
| Assets/Scripts/Render/Shader/Background Test/rock1_test.fbx | ebe1401343f80f748a93fdd6140d6b13 |
| Assets/Scripts/Render/Shader/Background Test/rock2.fbx | 0d5e721c22c81f84ea960eac674a043d |

- [x] 해당 6개 meta의 importCameras만 0으로 변경. GUID·FBX 원본·메시/머티리얼 설정은 유지한다. 이 설정은 해당 모델을 사용하는 모든 인스턴스에 적용된다.
- [x] 카탈로그 중복 오류에 두 Camera의 씬 경로·계층 경로·Instance ID 추가. 정상 수집/대여 경로에는 추가 문자열을 만들지 않는다. 실제 카메라 중복 거부 계약은 유지한다.
- [x] 중복 경로 식별 테스트 추가·컴파일, ProjectL.Tests.Framework와 의존 어셈블리 MSBuild 성공. 새 경고 없음(Logs/unity-camera-fbx-import-build.log).
- [x] FBX 노드 재조사 및 6개 임포트 설정 0 확인, 6개 meta diff가 각각 설정 한 줄뿐인지 확인(Logs/jmhtest-fbx-camera-audit.log). 조사 스크립트는 Temp/audit_scene_fbx_cameras.py다.
- [ ] 사용자 Unity에서 재임포트 완료 뒤 JMHTest Play 및 실제 카메라 테스트 실행. 실행 중 두 객체의 Instance ID를 직접 확인한 결과와 정적 모델 조사는 구분한다.

불필요한 카메라의 원본인 모델 임포트 설정에서 정리하며 카탈로그에 모델별 제외 규칙이나 이름 Camera 예외를 넣지 않는다(SSOT/SRP·변경 독립성). 외부 서비스·Factory·수명·Pool 계약은 그대로다(SOLID/FW). 이번 추가 수정은 위 6개 meta·Unity 카탈로그 진단·해당 테스트·문서에 한정하고 Core·FieldCamera·Architecture·씬은 건드리지 않았다.

### C5 — Unity 카메라 최초 등록 시 영속화 (2026-10-18)

사용자가 Unity 카메라 등록 시 DontDestroyOnLoad 처리를 명시적으로 지시하여 기존 외부 영속화 책임을 카탈로그의 최초 등록으로 옮겼다. 수집·미리보기 제외·중복 검증 후 플레이 중 `camera.transform.root.gameObject`에 적용한다. HashSet으로 같은 루트는 한 번만 처리한다. 현재 Main–Render 계층을 분리하지 않으며 같은 루트의 다른 자식도 함께 유지된다. 카탈로그 재바인딩은 여전히 기존 참조/Handle을 유지하고 이후 생성 Camera를 수집하지 않는다. Shutdown도 Camera를 파괴하거나 원래 씬으로 되돌리지 않는다.

- [x] Unity 카탈로그 최초 등록의 영속화와 사용 문서·Framework 문서 반영.
- [x] UnityCameraCatalogPersistenceTests와 meta 추가: 씬 언로드 후 루트·비활성 자식·비활성 루트 Camera/Handle 유지·계층 보존, 등록 중복 실패 시 원래 씬 유지의 2개 PlayMode 진입 테스트 작성·컴파일.
- [x] ProjectL.Tests.Framework 및 의존 어셈블리 MSBuild 성공, 변경 소스 새 경고 없음(Logs/unity-camera-persistence-build.log).
- [ ] Unity에서 새 수명 테스트 2개 실행 및 JMHTest 등록 후 DontDestroyOnLoad 이동 확인. 열린 에디터와 프로젝트 잠금을 유지했으며 별도 Unity 인스턴스를 실행하지 않았다.

카메라의 실제 수명 상태는 Unity Scene이 소유하고 영속 등록 동작은 카탈로그 한 곳에 둔다(SSOT/SRP). Handle API·Pool 반환·Factory 등록과 그룹 수명 전달은 유지한다(ISP/LSP·FW-001/002/014). 기능 변경은 Unity 카탈로그·수명 테스트·해당 안내 문서에 한정하며 버추얼 카탈로그·효과·Core·FieldCamera·Architecture·씬·Bootstrap 코드는 수정하지 않았다(변경 독립성).
