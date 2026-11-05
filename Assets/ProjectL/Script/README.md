# ProjectL 코드 구조

2026-09-22 정리 기준.

```text
Script/
├─ Framework/                   공용 기반 코드 (Framework 어셈블리)
│  ├─ Composition/              빌더와 런타임 실행
│  ├─ SystemBase/               시스템 계약, 키 기반 타입, 저장소
│  ├─ DataBase/                 데이터베이스 기반 타입
│  └─ Utility/                  풀, 큐, 이벤트 구독 수명 관리
├─ Systems/                     게임 시스템 (ProjectL.Systems 어셈블리)
│  ├─ ServiceKeys/              Factory 계층별 조회 키(Runtime·DBFactory)와 시스템 종류
│  ├─ DataBaseSystem/
│  │  ├─ Data/                  게임 데이터와 ScriptableObject 타입
│  │  └─ Queries/               데이터 조회 구현
│  ├─ Factory/
│  │  ├─ Loading/
│  │  └─ Runtime/
│  └─ RuntimeSystem/
│     ├─ BufferSystems/
│     ├─ InGameSystem/           CoreSystem/CoreCaculator에 Core 코드 포함
│     ├─ LifeCycleSystem/        Base 및 개별 LifeCycle 포함
│     └─ GlobalSystem/
│        ├─ UISystem/           Base 및 Screens 포함
│        ├─ RenderSystem/       Base 및 렌더 서비스 포함
│        ├─ AudioSystem/
│        ├─ InputSystem/
│        ├─ LoadingSystem/
│        ├─ JobSystem/
│        └─ UtilitySystem/
├─ Render/                      URP RendererFeature, 셰이더, 렌더 리소스
└─ Tool/Editor/                 모델 임포트 등 에디터 도구
```

## 배치 기준

- 전투 선택 원본은 `Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Selection/`의 `CombatSelectionState`에 있다. 같은 폴더의 `ICombatSelectionReader`·`ICombatUnitInspection`으로 읽기/입력 전달을 나눈다. DisplayFactory가 만들고 Display가 소유하며 정보창은 읽기만 한다. Display가 완료 표시 때 한 번 고정한 개체 목록을 선택과 UI가 공유한다.
- 입력 사실 조합은 `Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/CombatInputFactsReader.cs`다. 현재 연결과 Buffer를 매번 읽고 조정자의 실행/결과 소비를 대신하지 않는다. 생성은 기존 `Factory/Runtime/InGame/View/`에서 연결하며 별도 서비스로 등록하지 않는다. 사용 안내는 [스테이지 입력 §2.5](../../../docs/superpowers/viewDocs/stage-input-guide.md#25-5라운드-선택사실-읽기운영-조립-2026-10-26), 인수/잔여는 [5라운드 계획 §10](../../../docs/superpowers/plans/2026-10-26-combat-view-stage-round5-plan.md#10-v5-d-최종-인수와-잔여-2026-10-26)다.
- 부분 입력 조립 도우미 `Assets/Tests/EditMode/Framework/CombatInputTestFactory.cs`는 시험 어셈블리에 둔다. 운영 `CombatSceneInputFactory`는 필수 재료를 검사하고 전체 기능을 조립하며 시험 도우미에 의존하지 않는다. 새 `.cs`의 `.meta`와 `Selection.meta`를 함께 유지한다.
- 전투 선택 가용성 요청 Tracker 위치: [CombatActionAvailabilityTracker.cs](Systems/RuntimeSystem/InGameSystem/ViewSystem/Combat/Input/Interaction/CombatActionAvailabilityTracker.cs). 현재 사용·조립 안내는 [스테이지 입력 안내 §2.3](../../../docs/superpowers/viewDocs/stage-input-guide.md#23-새-core-행동-가용성-소비-2026-10-26)를 따른다.
- Framework에는 특정 게임 시스템을 몰라도 사용할 수 있는 계약, 기반 타입, 편의성 코드만 둔다. Framework는 Systems, Core, FMOD를 참조하지 않는다.
- UI, Render의 추상 클래스는 각 GlobalSystem 하위의 `Base`에 둔다. LifeCycle의 기반 클래스는 `RuntimeSystem/LifeCycleSystem/Base`에 둔다.
- 유지 UI는 `GlobalSystem/UISystem/Base/GlobalUIBase.cs`를 상속한다. 최초 씬 바인딩 때 수집한 목록을 `OpenGlobalUI()`로 일괄 열고, 씬 재바인딩에서도 목록과 열린 상태를 유지한다.
- 실제 시스템 생성과 조합은 Systems의 Factory가 담당한다. `FrameworkRuntime`은 공용 실행기로 Framework에 남는다.
- Render Core의 CameraGroup은 버추얼 카메라 이름 조회(CameraCatalogSystem), 유지 Unity 출력 카메라 이름 조회(UnityCameraCatalogSystem), 버추얼 연출(CameraEffectSystem)을 나눈다. 카메라 생성은 Bootstrap/씬 구성이 맡고, Unity 카탈로그는 최초 등록 때 플레이 중 카메라 최상위 루트에 DontDestroyOnLoad를 적용한 뒤 참조·핸들을 관리한다.
- 조회 키는 `Systems/ServiceKeys/{Runtime,DBFactory}`에서 소유 Factory와 같은 계층으로 나눈다. 그룹 키와 개별 시스템 키는 다른 파일에 두며, 루트 그룹 키는 `ProjectL.Systems.ServiceKeys.Runtime.SystemGroupsService`다. 세부 대응은 [개발 가이드 §5](Systems/Documentation/framework-programmer-guide.md#조회-키의-소유-계층-2026-10-17)를 따른다.
- LifeCycle과 GlobalSystem은 RuntimeSystem의 형제 그룹이다. `Factory/Runtime/LifeCycle`에서 생성하고 루트 FrameworkRuntimeFactory가 직접 등록한다. `runtime.GetService(SystemGroupsService.LifeCycle)`로 접근한다.
- 루트 등록 순서는 Buffer → Global → InGame → LifeCycle이다. 종료는 역순이므로 LifeCycle이 의존 서비스보다 먼저 종료된다. GlobalSystemGroup은 LifeCycle을 소유하거나 노출하지 않는다.
- ProjectL.Systems references Framework, Core.Contracts, Core.Application, and the required Unity UI and FMOD assemblies. Core.Application owns stateless chapter command composition; the Systems coordinator owns Buffer effects.
- `Systems/.../RenderSystem`은 게임의 렌더 서비스다. `Script/Render`에는 URP 기능과 셰이더 리소스를 둔다. 기존 Game/SceneView 카메라 필터와 프리팹 편집 제외 처리를 유지했다.
- `Tool/Editor/OutlineNormalPostprocessor.cs`는 복원한 모델 임포터다. 같은 위치의 정점 노멀을 평균 내 아웃라인용 UV3에 저장한다. 기존 Tool 폴더와 임포터의 GUID를 유지했다.

## 유지 UI 구현 (2026-10-01)

`GlobalUIBase`는 `UIBase`를 상속하며 `ParentID`를 `None`으로 고정한다. 최초 씬 바인딩 완료 전 등록된 유지 UI는 `_globalUIs`에 저장되고, 플레이 중에는 해당 UI의 `transform.root.gameObject`에 `DontDestroyOnLoad`가 적용된다. 같은 루트 아래의 Canvas와 다른 오브젝트도 함께 유지된다.

초기화·서비스 바인딩 완료 후 `global.UIServiceCommand.OpenGlobalUI()`를 호출하면 저장된 유지 UI만 일괄 열린다. 일반 화면과 자식 UI는 이 호출로 열리지 않는다. 유지 UI는 뒤로가기 이력에서 제외되며 `CloseUI(id)` 또는 `HideAllUI()`로 닫을 수 있다.

Boot의 `GameBootstrap.globalUICanvasPrefab`에 `GlobalUICanvas` 프리팹을 연결한다. Bootstrap은 최초 `BindScene` 전에 Canvas 전체를 한 번 생성하고 유지하며, Buffer 바인딩과 초기화 후 `OpenGlobalUI()`를 호출한다. StageSelect·Combat에는 같은 Canvas를 다시 배치하지 않는다. 덱의 UnitCard 프리팹은 유지 Canvas 아래에 한 번 생성한다.

Stage·Combat 로드 완료 시 UI 씬 바인딩을 LifeCycle 진입보다 먼저 실행한다. 유지 UI는 기존 인스턴스와 서비스 참조를 보존하고 새 씬의 일반 UI만 등록·바인딩한다. Bootstrap 종료 시 생성한 Global Canvas도 정리한다.

테스트 구현은 `GlobalSystem/UISystem/TestGlobalUI.cs`에 있다. `UIId.TestGlobal = 7`을 사용하고 `Start()`에서 `Open()`을 호출한다. `Open`과 `Hide`는 자신의 GameObject를 활성·비활성화한다. 자동 표시는 오브젝트와 부모가 활성 상태여서 `Start()`가 실행될 때만 동작하며, UISystem의 열린 상태 등록은 별도 서비스 호출이 필요하다.

## 이동과 호환성

기존 소스와 리소스는 `.meta`와 함께 이동했다. AudioSystem도 GlobalSystem 아래에 유지했다. Core는 `Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator` 아래에 있으며, 기존 씬의 위치는 그대로다. 데이터베이스 SO는 `Assets/ProjectL/DataBaseSO`로, 프리팹과 Resources는 아래 위치로 정리했다. 데이터 타입, 서비스와 팩토리의 네임스페이스 및 테스트의 리플렉션 조회는 새 어셈블리와 경로에 맞췄다.

Framework C# 파일은 18개, Systems의 서비스 코드는 143개이며, 그 안의 CoreCaculator에는 별도 어셈블리의 Core 코드 188개가 있다. 상세 구조는 [구조 문서](Systems/Documentation/framework-architecture.md), 서비스 사용·확장 계약은 [개발 가이드](Systems/Documentation/framework-programmer-guide.md)를 참고한다. 두 문서도 현재 계층과 네임스페이스에 맞춰 갱신했다.

## 네임스페이스

`Script` 아래 C# 코드는 `ProjectL.<Script 기준 상대 폴더 경로>`를 사용한다. 예를 들어 Core의 전투 도메인은 `ProjectL.Systems.RuntimeSystem.InGameSystem.CoreSystem.CoreCaculator.Domain.Combat`, UI 기반 타입은 `ProjectL.Systems.RuntimeSystem.GlobalSystem.UISystem.Base`, 모델 임포터는 `ProjectL.Tool.Editor`다.

Core의 기존 `ProjectL.Core.Contracts`, `ProjectL.Core.Domain`, `ProjectL.Core.Application` 어셈블리 이름과 GUID는 유지한다. 각 asmdef의 `rootNamespace`만 현재 경로에 맞췄다. 렌더 피처와 FakeGI, UIBase에는 기존 타입 위치를 나타내는 `MovedFrom`을 적용했다. 네임스페이스 변경 검증 결과는 `docs/superpowers/plans/2026-09-22-folder-namespaces.md`에 기록한다.

이동 목록과 검증 결과는 저장소의 `docs/superpowers/plans/2026-09-22-framework-move-manifest.json` 및 `2026-09-22-framework-systems-reorganization.md`에 기록했다.

## 리소스와 프리팹

기존 `Assets/DataBase/SO`의 SO 에셋 55개는 `Assets/ProjectL/DataBaseSO`로 이동했다. `Architecture`, `RenderGroup`, `Stage`의 내부 구조와 에셋·폴더 GUID를 유지했으며, 대상에 미리 만들어 둔 `StageGroup` 폴더도 보존했다. 이동 전후 파일 해시는 `docs/superpowers/plans/2026-09-22-so-move-manifest.json`에 기록했다.

```text
Assets/ProjectL/
├─ Resources/
│  ├─ Font/
│  ├─ Environment/             배경·소품·철도 모델/머티리얼/텍스처/애니메이션
│  ├─ Characters/              Prototype·Colored·Enemy/SourcePacks 원본
│  ├─ Artifacts/               유물 모델·텍스처·머티리얼
│  ├─ Shared/ShaderTests/      셰이더 검증용 아트 묶음
│  ├─ View/
│  └─ Test/Stage/Board/
└─ Prefabs/
   ├─ Default/
   ├─ Environment/Gameplay/    배치용 환경 프리팹
   ├─ Characters/              기존 Stage/Units 게임용 프리팹
   ├─ Artifacts/
   ├─ Stage/                   맵·철도 조립 프리팹
   ├─ StageSelect/             관련 머티리얼도 함께 유지
   ├─ Render/
   │  ├─ Effects/              데이터베이스 폴더에 있던 이펙트 프리팹
   │  └─ Lights/               데이터베이스 폴더에 있던 조명 프리팹
   └─ Test/
      ├─ Audio/                FMOD 오디오 테스트 프리팹
      └─ Characters/           기존 Resources/Test/Model/Prefab 원본
```

2026-09-22 이동 이력은 `docs/superpowers/plans/2026-09-22-resource-move-manifest.json`에 남아 있다. 위 트리는 2026-10-18 정리 결과다. Effect 관련 파일과 의존 원본은 기존 위치에 유지하고, 그 밖의 리소스·프리팹 300개는 파일과 `.meta`를 함께 이동해 원본 GUID와 내용을 보존했다. 테스트 모델 프리팹 4개는 Resources 로드 사용처가 없음을 확인한 뒤 `Prefabs/Test/Characters`로 옮겼으며, 게임용 프리팹의 중첩 참조는 기존 GUID를 유지한다. 외부 패키지의 원본 프리팹은 `Resources/Characters/Enemy/SourcePacks`의 상대 구조를 유지한다. FMOD 프로젝트, 플러그인, TMP 패키지 리소스는 각 기존 위치를 사용한다.

최신 이동 목록·검증 기록: `docs/superpowers/plans/2026-10-18-resource-reorganization-manifest.json`, `docs/superpowers/plans/2026-10-18-resource-reorganization-final-result.json`. Unity 재임포트 후 실제 표시 확인은 정적 파일·GUID 검사와 구분한다.
