# 서비스 조회 키의 Factory 계층 분리

2026-10-17. 사용자 요청: 그룹 키와 개별 시스템 키가 섞인 조회 키를 팩토리 계층처럼 나눈다.

## 설계

- 키는 그 서비스를 등록하는 Factory의 상대 계층을 `Systems/ServiceKeys/` 아래에 따른다. 네임스페이스도 실제 폴더와 맞춘다.
- 한 파일에는 키 컨테이너 하나를 둔다. 그룹을 찾는 키와 개별 시스템을 찾는 키는 별도 컨테이너에 둔다.
- `Runtime/SystemGroupsService`: 루트의 Global, InGame, LifeCycle 그룹. 루트 Buffer 조회는 internal인 `Runtime/BufferSystemKey`를 유지한다. 입력 수신 슬롯도 실제 루트 등록 위치인 `Runtime`에 둔다.
- `Runtime/Global/GlobalSystemGroupServiceKey`: Render, Utility, Input, Loding, Job 그룹. `GlobalSystemServiceKey`: UI, Audio 단일 시스템.
- `Runtime/Global/Render/RenderSystemGroupServiceKey`: CameraGroup. 같은 폴더의 `RenderSystemServiceKey`: 개별 렌더 시스템, `CameraSystemServiceKey`: 카메라 그룹 안의 Catalog, Effect.
- `Runtime/Global`: Input, Job, Loding, Utility 팩토리 각각의 내부 시스템 키.
- `Runtime/InGame/InGameSystemGroupServiceKey`: Core, View 그룹. `Runtime/InGame/Core/InGameCoreSystemServiceKey`: Core 팩토리가 등록하는 개별 서비스. View는 기존처럼 조회 키를 새로 만들지 않는다.
- `Runtime/Buffer`: Buffer 팩토리가 등록하는 Buffer별 키 파일. DB 키 파일에 있던 Buffer 키와 SystemGroupsService 파일의 RecoveryResultBufferKey를 이곳에 모은다.
- `DBFactory/DataBaseGroupKey`: Render, InGame, View Query 그룹. 같은 폴더의 `DataBaseServiceKey`: 보드 설정의 좁은 조회 계약. `DBFactory/Group/{Render,InGame,View}TableKey`: 각 DB 팩토리의 테이블 키.
- 서비스 종류 enum은 조회 키가 아니므로 기존 위치에 둔다. 호환 별칭이나 두 번째 키 정의를 남기지 않는다.

## 유지할 계약과 영향

서비스 인터페이스, 키 필드의 이름·타입·Name, 실제 ExportServices/AddService/AddModule, 초기화·Tick·종료 순서는 유지한다. Framework의 타입 기반 조회를 계층 검사나 문자열 기반 조회로 바꾸지 않는다. 이 변경은 키 정의의 소유 위치를 드러내는 작업이다.

기존 키 파일의 대표 컨테이너는 원본 `.meta` GUID를 가지고 이동한다. 분리한 새 파일·폴더에는 새 GUID를 부여한다. 정적 키 클래스는 SO/MonoBehaviour가 아니므로 MovedFrom 대상이 아니다. 테스트의 완전한 타입 이름 리플렉션도 함께 바꾼다.

Core 파일에는 변경된 키의 using/식별자 참조만 반영한다. Core 규칙·계산·상태·서비스 등록은 수정하지 않고 DB 경계 handoff §5에 접촉 내역을 남긴다. 진행 중이던 Render 팩토리 이동과 CameraCatalogSystem 수정은 유지한다.

## 구조 점검

- SSOT: 각 기존 키는 한 곳에만 정의하며, 별칭·전역 using으로 옛 평면 경로를 유지하지 않는다.
- SRP/OCP: 서비스 키 추가 시 소유 Factory 계층의 키 파일만 수정한다. 예를 들어 Camera Effect 키 변경은 Camera 키 파일에 머물며 DB·Buffer 키를 함께 읽을 필요가 없다.
- LSP/ISP/DIP: 기존 인터페이스와 좁은 읽기/쓰기 계약을 유지한다. 키 정리를 이유로 새로운 서비스나 넓은 Context를 추가하지 않는다.
- FW-001~003/016: 등록·모듈·Builder·실행 수명을 바꾸지 않는다. 조회 시 실제 부모 Context를 거쳐야 한다는 기존 계약은 그대로다.
- 변경 독립성: 분리 전 렌더 키 수정에는 Core·LifeCycle·Buffer 정의가 섞인 SystemGroupsService 파일을 열어야 했다. 분리 후 해당 Render 키 파일과 실제 소비자만 수정한다. 공통 키 기반 타입·Reader 계약은 Framework에 그대로 남는다.

## Task 계획 — Phase K1

1. 기존 컨테이너·멤버 목록과 GUID를 보관하고 키를 위 위치로 분리한다.
2. 실제 소비자의 using·변경된 컨테이너 이름·리플렉션 문자열을 갱신한다.
3. 구조 문서와 Framework 공유 메모, Core 접촉 내역을 기록한다.
4. 키 타입·멤버 보존, namespace/폴더 일치, GUID 보존·중복을 검사한다.
5. 관련 어셈블리 컴파일과 기존 조립/조회 테스트를 실행한다. Unity 실행 여부를 별도로 기록한다.

K1이 끝나면 결과를 보고하고 멈춘다. 기능 변경이나 추가 계층 도입은 다음 작업에 포함하지 않는다.

## 실행 기록

- **K1 코드 완료.** 기존 키 137개를 키 컨테이너 38개·파일 38개로 나눴다(기존 enum 파일 2개 별도). 실제 소비자의 using·변경된 컨테이너·리플렉션 문자열을 갱신했다. 기존 8개 파일의 GUID를 대표 컨테이너로 보존했다.
- **정적 검사 통과:** 137개 키의 필드 이름·타입·`new(nameof(필드))` 보존, 컨테이너별 단일 파일, namespace/폴더 일치, 키 GUID 중복 없음. CoreSystem 변경 diff는 using과 `InGameCoreSystemServiceKey` 참조로 한정됨을 검사했다. CoreCaculator 변경 0건.
- **실제 소스 빌드 제한:** 시작 시 이미 수정 중이던 `CameraCatalogSystem.cs`의 `PostBindService`에 미완성 `var Loding = systemServiceResolver.GetService<LodingSystemServiceKey>`가 있어 CS1002로 멈춘다. 이 작업에서는 해당 줄을 수정하지 않았다. 새 namespace 참조를 위한 using만 반영했다.
- **검증용 빌드 통과:** 위 파일을 Temp에 복사하고 미완성 줄 하나만 제외한 검증용 소스를 사용했다. VS 18 MSBuild / UseSharedCompilation=false로 `ProjectL.Tests.Framework.csproj`와 참조 어셈블리를 빌드해 오류 0건을 확인했다. Unity 생성 csproj는 수정하지 않았고 Temp의 `CustomAfterMicrosoftCommonTargets`로 이동한 키 목록과 검증용 Camera 파일을 반영했다. 경고는 기존 UI/테스트 필드·중복 using 및 Core의 nullable 컨텍스트 위치에서 발생하며 새 키 파일에는 없다.
- **관리 실행:** 검증용 빌드 DLL을 Unity 내장 Mono에서 기존 NUnit 테스트 메서드 8개로 실행했다. 루트 LifeCycle 단독 조립, CombatProgress 미연결 읽기 거부, 루트/자식 Context 노출 구분, Job 그룹 등록·수명 전달, Current/Stage Buffer 분리, Artifact Buffer 권한 분리 **6개 통과**. `ChapterCoordinator_BindsRealBufferServicesThroughCoreGroup`, `ChapterCoordinator_UsesRegisteredGraphAndSelectsCurrentMap` 2개는 Audio.PostBindService의 Unity 네이티브 `FindObjectsByType`가 관리 실행 환경에 없어 완료할 수 없었다. NUnit의 전체 Unity EditMode 실행 결과로 표기하지 않는다.
- **Unity 에디터 컴파일·EditMode는 미실행:** 원본 Camera의 미완성 호출을 해당 작업자가 완성한 뒤 확인해야 한다. 검증용 복사본의 성공과 원본 전체 프로젝트 성공을 구분한다.
- 개발 가이드·구조 문서·DB 가이드·Script README와 Framework 공유 메모를 갱신했다. Core 접촉은 DB 경계 handoff §5.44에 기록했다. 공유 메모는 미전달이며 커밋·push하지 않았다. Phase K1 리뷰 지점에서 멈춘다.
