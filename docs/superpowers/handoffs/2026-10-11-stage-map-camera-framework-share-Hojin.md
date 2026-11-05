# StageSelect 카메라 사용·Framework 계약 점검 인계

- 작성자: 호진(이호진)

> **로비 최신 구성·완료 상태(10-24)는 §9를 따른다.** §2의 `LobbyOutputCamera`·`LobbyArtifactCamera` 구성은 이전 이력이다. 고정 `LobbyCamera` 하나와 Boot 출력 Camera 재사용으로 바뀌었다. 사용자 재생 확인과 기존 EditMode 테스트 전부 통과 보고를 근거로 이전 작업을 마감했다.

- 작성: 2026-10-11. 대상: Framework/Render 카메라 담당. **전달용 문서 작성 완료, 외부 전달 전.**
- 기준: `5f27fb9bf7f902050545bd8e465566b3bca265cc` 위의 StageSelect 카메라 변경 작업본, Unity 6000.0.73f1, Cinemachine 2.10.7, Unity Test Framework 1.6.0.
- 결론: 현재 범위에서 Framework·UI·Camera 계약 위반은 발견하지 않았다. CameraCatalog의 공개 핸들로 제어하며, 게임별 이동 정책은 View가 소유한다. 기존 실패 3건·판정 보류 1건의 원인을 분리한 뒤 사용자 승인으로 테스트만 수정했고, **관련 875/875 통과**를 확인했다. 최신 결과는 §8이다.
- 후속 테스트 수정에서도 운영 코드는 유지했다. 커밋·푸시·스테이징은 하지 않았다.

## 1. 지킨 기준과 게임별 결정

| 기준 | 이 구현에서의 적용 |
|---|---|
| [Framework 개발 가이드](../../../Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md) FW-004, 카메라 서비스 안내 | Global → Render → CameraGroup의 공개 Catalog 서비스 사용. 내부 Node/Pool과 실제 출력 카메라 Transform 직접 수정 없음 |
| 같은 가이드 FW-005~007 | 기존 InputSystem 구성과 Submit·Layer 경로 유지. 새 Action/Controller/입력 수집기 추가 없음 |
| 같은 가이드 FW-008~009, [UI 입력 규칙](../../../Assets/AGENTS.md), [ArtifactPopupUI 계약](../viewDocs/artifact-guide-Hojin.md#2-artifactpopupui-연결) | 기존 UIInputBinding·IUIInputReceiver 및 UISystem의 Open/Close 이력 유지. Button.onClick·Inspector UnityEvent 연결 추가 없음 |
| [CameraCatalog 계약](../../render-camera-catalog.md) | 이름으로 핸들 한 번 대여, 전체 설정을 복사한 Customize, 사용 종료 시 Dispose. 비활성 가상 카메라도 씬 바인딩에서 수집 |
| [CameraEffect 계약](../../render-camera-effects.md) | 직접 드래그에 시간 효과를 만들지 않음. 씬 재바인딩에서 Catalog와 Effect 수명을 함께 전달 |
| FW-016, [Framework 구조](../../../Assets/ProjectL/Script/Systems/Documentation/framework-architecture.md) | 서비스 연결 뒤 사용. 씬 로드 후 카메라 수집을 View 연결보다 먼저 실행하고, 씬 이탈 때 핸들 반환 |
| [개발 지침](../framework/Framework%20Development%20Guidelines.md), [SSOT·SOLID 적용 기준 §4](2026-09-30-combat-view-design-session-handoff.md#4-반드시-지킬-설계-원칙) | 입력 소유권·UI 가림·카메라 이동 책임 분리. Buffer의 노드 식별과 실제 표시 위치를 사용하고 노드 배치 공식을 복제하지 않음 |

빈 배경 왼쪽 드래그, Start↔Boss 축 제한, 우선순위 10/20, Cut 전환은 **StageSelect의 게임별 결정**이다. 공용 Framework가 모든 화면에 강제하는 규칙은 아니다. 카메라 핸들 반환의 직접 근거는 CameraCatalog 문서이며, FW-011의 렌더 자원 반환 원칙과도 일치한다.

## 2. 씬에 구성한 카메라 (LV5 현재 구성)

대상 씬은 [StageSelect.unity](../../../Assets/Scenes/StageSelect.unity)다.

| 오브젝트 | 역할·설정 |
|---|---|
| 기존 `Main Camera` | 화면 출력 및 노드 피킹용 실제 Camera. 기존 MainCamera 태그 유지. CinemachineBrain 추가, LateUpdate, Default Blend = Cut |
| `StageMapCamera` | 맵용 CinemachineVirtualCamera. 우선순위 10. 숨겨지는 MapRoot 밖에 배치. StageMapSceneRoot의 Map Camera 필드로 참조 |

Stage에는 `StageMapCamera` 하나가 남으며 실제 Main Camera + Brain이 화면 출력과 피킹을 맡는다. 시작↔Boss 축 드래그는 기존 Catalog 핸들의 전체 설정 복사·Customize를 사용한다. 상점 동안에는 현재 맵 위치를 유지한다.

**LV5에서 로비로 이전(→ 바뀜: 2026-10-24 §9 — 고정 `LobbyCamera` 하나와 Boot 출력 Camera 재사용):** 아티팩트 팝업·WorldRoot·독립 배경과 옛 `StageArtifactCamera`를 Lobby.unity로 옮겼으며 이름은 `LobbyArtifactCamera`다. 아티팩트 카메라의 위치·회전·직교 렌즈·Priority 20은 유지했다. 로비 출력 `LobbyOutputCamera` + Brain이 이를 선택한다. 팝업의 기존 WorldRoot 활성 수명으로 선택하고 닫으면 고정 `LobbyCamera`(Priority 10)로 복귀한다. LobbyWorld만 숨겨 시설·플레이어가 보드와 겹치지 않게 하며 UI·SceneRoot·출력/고정 카메라는 숨김 밖에 둔다.

이름 수집은 CameraGroup이 비활성을 포함해 수행한다. 설정을 변경하지 않는 부모 활성 수명은 ArtifactPopupView가 소유하며, Catalog.Dispose를 원복 수단으로 쓰지 않는다. Stage와 Lobby의 출력 카메라나 가상 카메라 핸들을 공유하지 않는다. 최신 입력·Flow·씬 수명은 [아티팩트 안내](../viewDocs/artifact-guide-Hojin.md), LV5 검증은 [로비 계획](../plans/2026-10-11-lobby-plan-Hojin.md)을 따른다. 아래 §6~8의 과거 875건 기록은 당시 Stage 아티팩트 구성의 검증 이력이다.

## 3. 서비스 연결과 수명 순서

서비스 조회는 [StageMapViewSystem.PostBindService](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapViewSystem.cs)에서 한 번 한다.

```csharp
_cameras = global.RenderServiceCommand.CoreGroup.CameraGroup.CameraCatalogSystem;
```

여기서 `RenderServiceCommand.CoreGroup`은 Render의 서비스 묶음이다. 게임 규칙을 계산하는 InGame CoreSystem을 호출하는 경로가 아니다.

```text
Boot: Runtime 조립·초기 바인딩·서비스 연결·Initialize
  ↓ Stage 또는 Combat 씬 로드 완료
우선순위 -200: UISystem.BindScene
우선순위 -190: CameraGroup.BindScene                 ← 이번 추가
우선순위    0: 해당 LifeCycle 진입
우선순위  100: StageMapViewSystem.AttachFromScene     ← Stage일 때
  → SceneRoot 및 실제 Camera.main 조회
  → StageMapCameraDrag 생성(검증·초기값만, 대여 없음)
  → 기존 입력 슬롯 연결 / StageSelection Layer
  ↓ AfterLoad (2026-10-19 변경)
우선순위   10: CameraCatalogSystem 재수집(Framework)  ← 그 전 핸들 모두 무효
우선순위  100: StageMapViewSystem 맵 카메라 핸들 대여 = GetHandle(씬 가상 카메라 이름)
  → 데이터가 준비된 Tick에서 맵 표시 / Start·Boss 경계 설정
  ↓ 씬 이탈
우선순위 -100: StageMapViewSystem.Detach
  → 입력·드래그 중단 / 카메라 핸들 Dispose / 표시·참조 정리
```

[GameBootstrap.BindUILifeCycle](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Boot/GameBootstrap.cs)에 Stage와 Combat의 `new LodingNode(-190, cameras.BindScene)`을 등록했다. 실제 호출 대상은 `ISystemBindScene`을 구현하는 **CameraGroup**이다. Render 전체를 다시 바인딩하거나 Runtime.Initialize를 다시 호출하지 않는다.

**재바인딩의 영향:** CameraCatalog는 기존 핸들을 무효화하고, CameraEffectSystem은 진행 중 효과를 Dispose한 뒤 새 이벤트를 구성한다. 이번 훅은 Stage만이 아니라 Combat 진입에도 적용된다. 현재 맵 핸들은 Stage Exit에서 먼저 반환한다. 향후 씬에 머무르는 동안 다른 곳에서 CameraGroup.BindScene을 추가 호출하면, 기존 제어자도 중단하고 새 핸들을 받는 순서를 함께 정해야 한다.

**2026-10-19 대여 시점 변경:** Framework가 `CameraCatalogSystem`에 AfterLoad(10) 재수집을 추가한 뒤(`b308c88d`), 연결(100)에서 빌린 맵 핸들이 그 재수집으로 무효가 되어 드래그 때 `ObjectDisposedException`이 났다. Catalog 문서의 "씬 재수집 전 대여 수명 종료는 호출자 책임"에 맞춰, View는 핸들을 AfterLoad(`StageMapViewSystem.CameraHandlePriority` = 100)에서 빌린다. 대여 전에 정한 경계 위치는 대여할 때 적용하고, 대여 실패는 연결 실패와 같이 Detach한다. 로비 고정 카메라도 같은 이유로 `LobbyViewSystem.CameraHandlePriority`(AfterLoad 100)에서 빌리며, 대여 직후 부모 정지 확인은 예전 `Start`의 경계를 그대로 옮겼다. 전투는 화면 전환 재생 동안만 핸들을 빌려 로딩 뒤이므로 바꾸지 않았다. Bootstrap의 -190 수집도 그대로다.

정상 Detach 외에 오류/종료의 StopInput도 핸들을 반환한다. 입력 정리가 실패하더라도 카메라 정리는 별도로 시도한다. 대여 중 연결이 끝나면 늦게 받은 핸들을 바로 반환한다. Dispose는 반복 호출에 안전하며 카메라 설정 자체를 되돌리지 않는다.

## 4. 드래그가 실제 카메라까지 전달되는 경로

```text
PointerPosition / Select
 → UnityInputBridge → InputSystem.Submit → Layer 검사
 → StageSelectionInputController → StageSelectionReceiverSlot
 → StageMapInteraction
     ├─ UI에서 누름: 차단 상태 유지
     ├─ 노드에서 누름: 기존 노드 선택 처리
     └─ 빈 배경에서 누름: StageMapCameraDrag.Begin/Move/EndDrag
                         → ICameraCatalogHandle.Customize(data)
                         → StageMapCamera
                         → CinemachineBrain → 실제 Main Camera
```

- UI 판정은 [StageMapPointerBlocker](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapPointerBlocker.cs) 한 곳에서 현재 좌표의 EventSystem 레이캐스트 결과 중 GraphicRaycaster를 확인한다. PointerEventData와 결과 목록을 재사용하며 UI 컨트롤별 레이캐스트를 추가하지 않았다.
- 누름 때 정한 소유권을 뗄 때까지 유지한다. UI·노드에서 시작해 빈 곳으로 옮겨도 카메라는 움직이지 않는다. 배경 드래그 중 노드를 지나도 호버나 노드 진입을 만들지 않는다.
- 노드 입력은 기존처럼 같은 노드에서 정상 뗄 때 진입을 요청한다. 카메라 드래그 자체에는 Buffer 요청 번호·Flow·Plan·Job·Core 계산이 필요하지 않다. 표현 위치만 View에서 바꾼다.
- Release·우클릭 Cancel·Select Canceled·입력 해제는 드래그를 종료한다. 기존 UnityInputLayerRuntime은 이전 Layer 상태에서 ActionMap을 끄며 Canceled를 전달하므로 팝업 전환도 드래그를 종료한다.

[StageMapCameraDrag](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/StageMapCameraDrag.cs)는 처음에 `new CameraCatalogData(camera)`로 전체 설정을 복사하고 이름으로 핸들을 한 번 얻는다. 이동 때는 그 값의 Position만 변경해 Customize에 전달한다.

```csharp
CameraCatalogData data = _initial;
data.Position += _axis * _offset;
_handle.Customize(data);
```

축은 MapRoot.forward다. 시작·Boss는 StageProgress Buffer에서 식별하고, 위치는 StageMapViewDisplay.TryGetNode의 실제 표시 Transform에서 받는다. 초기 카메라 중심선과 맵 평면의 교점을 기준으로 두 노드까지의 축 방향 거리를 이동 한계로 삼는다. 끝 노드를 진행 축의 화면 중앙까지 볼 수 있는 범위이며, 카메라 위치를 무조건 `0~Boss.Z`로 제한하는 방식은 아니다.

드래그 시작 시 실제 출력 카메라로 진행 축 1단위의 화면상 이동 벡터를 구하고, 마우스 이동을 그 벡터에 투영한다. 맵을 잡아 끄는 느낌이 나도록 카메라는 반대 방향으로 움직인다. 회전·높이·가로 구도·직교 크기를 유지하고, 맵 재표시 때는 현재 이동량을 새 범위 안에서 유지한다.

## 5. 앞으로 확장할 때 유지할 조건

1. 로드된 모든 씬에서 가상 카메라 이름은 대소문자까지 포함해 유일해야 한다. 이름 변경·추가·삭제 뒤에는 수명 순서에 맞춘 재바인딩과 핸들 재대여가 필요하다.
2. 현재 맵은 직교, Follow/LookAt 없음, 고정 각도·렌즈의 단독 제어를 전제로 한다. 실제 출력 Camera+Brain과 SceneRoot의 MapCamera 참조가 필요하다.
3. Customize는 **전체 교체**다. 같은 카메라에 외부 줌·회전·우선순위 변경을 동시에 적용하면 다음 드래그가 초기 복사값으로 덮어쓸 수 있다. 그런 기능을 추가할 때는 먼저 한 소유자의 설정 합성·제어권 전환 정책을 정해야 한다. 현재 StageMapCamera에는 별도 줌/효과 제어자가 없다.
4. Artifact 카메라는 팝업의 기존 부모 활성 수명으로 전환된다. 별도 부모로 옮기거나 MapRoot 아래로 맵 카메라를 옮기면 이 구성을 다시 검토해야 한다.
5. Core·FieldCamera·Architecture·공용 CameraCatalog/Effect/입력/UI 내부 코드는 이번 변경에서 수정하지 않았다.

## 6. 기존 실패 3건·판정 보류 1건의 원인

작업 전 `repaired-all.xml`과 카메라 적용 후 `final-related.xml`에 아래 네 결과가 모두 동일하게 있다. 이번 진단에서는 운영 코드를 유지하고 실행 조건·테스트 기대값만 바꾸어 원인을 분리했다.

| 원래 테스트 | 원인과 카메라 변경과의 관계 | 진단 결과 |
|---|---|---|
| CameraEffectSystemTests.RenderFactoryExposesCameraGroupAndForwardsLifecycle | RenderSystemGroup.BindScene → RenderRuntimeRoot.BindScene이 RenderRoot에 DontDestroyOnLoad를 호출한다. EditMode에서는 허용되지 않아 카메라 모듈 연결 전에 실패한다. 관련 Render 코드·원래 테스트는 이번 작업에서 변경하지 않았다. | 같은 기존 테스트 메서드를 Play 모드에서 실행해 기존 카메라 그룹·줌 assertion 및 Shutdown 호출 통과 |
| GameBootstrapFaultBoundaryTests.InputCleanupFailure_DoesNotSkipRuntimeShutdown_OrRetryOnDestroy | 테스트가 의도적으로 주입한 `종료 입력 정리 실패`가 실제 로그 메시지다. 기대하는 영어 `Input cleanup failed during shutdown`은 외부 예외의 스택 추적에 있다. | 기대 정규식만 실제 메시지에 맞추면 원래 Shutdown 1회·Tick 0회 assertion 통과 |
| GameBootstrapFaultBoundaryTests.RuntimeShutdownFailure_IsReported_WithoutRestartingOrRetryingFailedCleanup | 실제 메시지는 `하위 Runtime 정리 실패`, 기대값 `Runtime shutdown failed`는 스택 추적에 있다. 카메라와 무관한 같은 로그 검증 문제다. | 기대 정규식만 맞추면 원래 종료 1회·Job/Tick 0회 assertion 통과 |
| StageMapViewSystemTests.EnterNode_WithoutMainCamera_Throws | Assume.That(Camera.main, Is.Null)에서 중단돼 실제 예외 assertion까지 가지 않는다. EditMode 러너는 DefaultGameObjects 씬을 만들며 테스트가 카메라 부재 조건을 격리하지 않는다. 이번 추가 가상 카메라는 MainCamera 태그가 없고 실제 Camera도 아니다. | 기존 MainCamera 태그를 잠시 해제해 전제를 갖춘 뒤 원래 메서드 통과. finally에서 태그 복원. 진단 로그의 기존 카메라는 `Main Camera`, 씬 경로는 빈 문자열 |

두 Bootstrap 테스트는 비활성 GameObject에 최소 Runtime을 주입하여 실행한다. Start/BindUILifeCycle을 호출하지 않으므로 이번에 추가한 카메라 재바인딩 훅은 해당 실패 경로에서 실행되지 않는다.

원인 확인 소스: [RenderRuntimeRoot](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/RenderSystem/RenderRuntimeRoot.cs), [RenderSystemGroup](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/GlobalSystem/RenderSystem/RenderSystemGroup.cs), [CameraEffectSystemTests](../../../Assets/Tests/EditMode/Framework/CameraEffectSystemTests.cs), [GameBootstrapFaultBoundaryTests](../../../Assets/Tests/EditMode/Framework/GameBootstrapFaultBoundaryTests.cs), [StageMapViewSystemTests](../../../Assets/Tests/EditMode/Framework/StageMapViewSystemTests.cs).

설치된 공식 Test Framework 소스에서도 LogScope는 message/stacktrace를 분리하고 LogMatch는 `log.Message`만 정규식과 비교한다. 따라서 스택에 영어 문구가 보여도 기존 LogAssert.Expect와 일치하지 않는다. 또한 Runtime 하위 Shutdown 실패 시 남은 모듈 정리까지 보장되는 것은 아니다. 이는 기존 Framework 종료 계약의 제한이며 이번 카메라 점검으로 해소됐다고 해석하지 않는다.

원인 분석 뒤 사용자가 위 테스트의 영구 수정을 승인했다. 렌더 통합 검증의 Play 모드 실행, Bootstrap의 실제 로그 기대값, 카메라 부재 조건의 태그 격리를 §8에 반영했다. 공용 Render 코드에 EditMode 우회 조건을 넣지 않았다.

## 7. 원인 분석 당시 실행 증거와 검증 범위

아래는 테스트 영구 수정 전 진단 이력이다. 현재 통과 결과는 §8을 따른다.

- 기존 관련 회귀: **875건 중 871 통과·3 실패·1 판정 보류**. 기록은 [스테이지 맵 계획의 카메라 드래그 결과](../plans/2026-10-10-stage-map-plan-Hojin.md)(옛 구현 계획 §9)에 있다.
- 이번 조건 분리 실행: **15/15 통과, Unity 종료 코드 0**. 렌더 Play 진단 1 + 카메라 부재 진단 1 + Bootstrap 5 + 카메라 8. Bootstrap은 두 로그 기대값만 임시 변경했으며 원래 행동 assertion을 유지했다. 새 컴파일 오류 없음.
- 카메라 8건은 실제 Catalog 단위 검증 4건과 Boot→StageSelect 재생 검증 4건이다. 이동 축·원래 렌즈/회전 보존·양끝 한계·끝에서 역방향 이동·취소/해제·회전된 맵·UI/노드 누름 차단·Artifact 구도와 맵 위치 복귀를 확인했다.
- Boot 검증은 Framework Submit과 EventSystem handler dispatch를 사용한다. 실제 마우스 장치 전체 경로를 자동 재현한 것은 아니며, 팝업 취소 검증은 Canceled를 명시 제출하고 실제 Layer 취소 순서는 소스로 대조했다. 사용자의 손으로 하는 Play 확인과 구분한다.
- 진단용 테스트 파일은 Assets에서 제거했고 Bootstrap 테스트 파일은 원본 바이트/SHA-256으로 복원했다. 따라서 **원래 테스트 3실패·1보류를 영구 수정하여 전체 통과시킨 결과는 아니다.**
- 복원 후 원래 네 테스트만 재실행해 **3실패·1판정 보류, Unity 종료 코드 2**가 같은 원인으로 재현됨을 확인했다. 해당 실행도 컴파일 오류는 없었다.

로컬 증거:

> 2026-10-13: 아래 증거 링크는 git에서 제외되는 `.superpowers/` 폴더를 가리켜 작성한 PC에서만 열린다. 다른 PC나 다른 작업자에게는 본문의 수치만 근거로 쓰고, 원본이 필요하면 따로 받는다. 이 문서를 전달할 때는 필요한 XML·JSON을 따로 첨부한다. (2026-10-27 확인: 이 PC의 `.superpowers/sdd`에도 `2026-10-11-post-user-merge`·`2026-10-11-stage-map-camera` 폴더가 없어 아래 링크가 열리지 않는다.)

- [작업 전 결과](../../../.superpowers/sdd/2026-10-11-post-user-merge/repaired-all.xml)
- [카메라 적용 후 관련 회귀](../../../.superpowers/sdd/2026-10-11-stage-map-camera/final-related.xml)
- [이번 조건 분리 15건](../../../.superpowers/sdd/2026-10-11-stage-map-camera/failure-audit-diagnostic.xml)
- [복원 후 원래 네 테스트 재현](../../../.superpowers/sdd/2026-10-11-stage-map-camera/failure-audit-original.xml)
- [결과 집계·소스 복원 해시 확인](../../../.superpowers/sdd/2026-10-11-stage-map-camera/failure-audit-verification.json)
- [진단 실행 스크립트](../../../.superpowers/sdd/2026-10-11-stage-map-camera/run-audit.ps1), [진단 보조 소스](../../../.superpowers/sdd/2026-10-11-stage-map-camera/CameraFailureAuditDiagnostics.cs)

`.superpowers/sdd`는 로컬 검증 자료다. 다른 작업자에게 문서만 전달할 경우 원본 XML이 함께 전달된다고 가정하지 않는다. 재검증 대상은 위 테스트의 정식 이름과 실행 조건을 따른다.

## 8. 테스트 정식 수정·최종 검증 (2026-10-11)

사용자 요청에 따라 기존 검증 내용을 유지하고 아래 세 테스트 파일만 수정했다.

- `CameraEffectSystemTests`: `RenderFactoryExposesCameraGroupAndForwardsLifecycle`에만 UnitySetUp/UnityTearDown으로 EnterPlayMode/ExitPlayMode를 적용했다. 재생 진입 후 기존 SetUp이 fixture를 생성하고, 기존 그룹·줌 검증 및 Shutdown 호출을 수행한다. 실제 Play 모드인지도 검증한다. 시작 씬 설정은 SessionState로 보관해 도메인 리로드 후에도 복원하며, 다른 카메라 효과 단위 테스트는 EditMode에 남는다. 설치된 공식 Test Framework 1.6.0의 UnitySetUp/UnityTearDown 예제와 TestCommandBuilder의 실행 순서를 따랐다.
- `GameBootstrapFaultBoundaryTests`: 두 LogAssert.Expect를 실제 내부 예외 메시지 `종료 입력 정리 실패` / `하위 Runtime 정리 실패`로 맞췄다. 종료 1회·재시도 없음·Tick/Job 0회 검증을 유지하고 예상하지 않은 로그를 무시하지 않는다.
- `StageMapViewSystemTests`: MainCamera 태그를 try/finally 안에서 잠시 해제하고 복원한다. Assume으로 환경에 따라 보류하던 대신 Assert로 카메라 부재를 확인하고, 실제 MainCamera 누락 예외 및 수신 슬롯 미연결을 검증한다.

Unity 6000.0.73f1에서 이전과 같은 관련 필터를 실행했다.

```text
StageMap;StageSelection;StageFlow;Shop;Artifact;
CameraCatalogSystemTests;CameraEffectSystemTests;GameBootstrap;InGameViewFaultCleanup
```

**875건 중 875 통과, 실패 0, 판정 보류 0, 건너뜀 0, Unity 종료 코드 0.** 기존 네 항목과 카메라 8건이 모두 포함되어 통과했다. 컴파일 오류 0, git diff --check 통과. 카메라 구현 단계에서 기록한 운영 소스 해시와 동일함도 확인했다. 테스트 수정으로 게임 기능을 바꾸지 않았으며 전체 프로젝트의 모든 테스트를 실행한 결과로 확대하지 않는다.

> 2026-10-13: 아래 증거 링크는 git에서 제외되는 `.superpowers/` 폴더를 가리켜 작성한 PC에서만 열린다. 다른 PC나 다른 작업자에게는 본문의 수치만 근거로 쓰고, 원본이 필요하면 따로 받는다. (2026-10-27 확인: 이 PC에도 `.superpowers/sdd/2026-10-11-stage-map-camera` 폴더가 없어 아래 링크가 열리지 않는다.)

- [최종 관련 테스트 결과](../../../.superpowers/sdd/2026-10-11-stage-map-camera/tests-fixed-related.xml)
- [Unity 실행 로그](../../../.superpowers/sdd/2026-10-11-stage-map-camera/tests-fixed-related.log)
- [네 항목·카메라 결과 및 운영 코드 해시 확인](../../../.superpowers/sdd/2026-10-11-stage-map-camera/tests-fixed-verification.json)

## 9. 로비 고정 카메라·Boot 출력 재사용 (2026-10-24, 외부 미전달)

현재 설계 원본은 [로비 설계의 카메라 절](../specs/2026-10-11-lobby-design-Hojin.md)(옛 카메라 설계 §7), 최종 완료·검증 근거는 [로비 계획의 카메라 이전 결과](../plans/2026-10-11-lobby-plan-Hojin.md)(옛 실행 기록 §2.10)이다. 아래는 Framework/Render 작업자에게 전달할 관찰과 사용 경계이며 Framework 구현 변경 요청은 없다.

- Lobby 씬의 `LobbyOutputCamera`와 `LobbyArtifactCamera`를 제거했다. 실제 출력은 Boot가 유지하는 Main Camera이고 로비 구도는 고정 `LobbyCamera` 하나다. 다른 씬처럼 연결 때 `Camera.main`을 한 번 받아 Scope·피킹·배경에 전달한다.
- 로비가 Global의 기존 RenderServiceCommand → CoreGroup → CameraGroup → UnityCameraCatalogSystem 경로를 소비한다. 서비스 루트 재등록이나 추가 카메라 생성은 없다.
- 팝업 표시 동안만 공개 출력 핸들로 `Capture → SetCullingMask(기존 | UI)`, 닫기·씬 이탈·오류 정리에서 `Restore → Dispose`한다. 이름은 받은 출력 Camera의 gameObject.name이 원본이다. Default 층 모델이 있으므로 UI 전용 마스크로 바꾸지 않는다.
- Scope·팝업 Controller·표시 해제 입구에서 같은 멱등 출력 Exit를 다른 정리 콜백보다 먼저 호출한다. 교체된 Scope의 늦은 정리는 새 출력 상태를 복원하지 않고 대여만 반환한다. 카메라 위치·렌즈·우선순위를 변경하지 않으며 구도 복원도 필요 없다.
- 보드는 `ArtifactWorldRoot` 배치로 고정 카메라에 맞췄다. 입력과 표시가 같은 루트 변환을 사용하며, 배경은 기존 View 설정 깊이와 출력 화면으로 자동 맞춘다. 가상 카메라 핸들의 기존 AfterLoad 100 대여 시점은 유지한다.
- 전투 화면 전환은 완료 뒤 UI 없는 마스크를 남길 수 있으므로 팝업을 열 때의 현재 마스크를 저장한다. Boot 씬의 기본값으로 되돌리는 코드는 없다.
- Boot의 `RenderTexture Camera`도 MainCamera 태그를 갖지만 평소 Camera 컴포넌트는 꺼져 있다. 로비는 평소 Main Camera를 받는다. 전투 화면 전환 중 `Camera.main`의 모호함을 없애기 위한 태그 정리는 별도 선택 사항이며 이번 작업에서 Boot는 수정하지 않았다.

로비 카메라 이전 작업은 완료했다(10-24). 코드·씬·기존 시험 호환성 정리를 반영했고 새 시험은 작성하지 않았다. 후속 수정 후 사용자가 기존 배경 오류 미발생·화면 흐림 해소·팝업 열기/닫기·드래그 배치·카메라 구도 유지, 전투 포기 후 로비 복귀·팝업 재사용·Culling Mask 원상복원·콘솔 오류 없음과 기존 EditMode 테스트 전부 통과를 확인했다(옛 계획 §2.7~2.10, 합친 로비 계획 참조). 재생·시험 통과는 사용자 실행 보고이며 Codex의 별도 실행 결과와 구분한다. Codex의 Unity 배치 확인은 동일 프로젝트가 이미 열려 있어 초기화 전에 종료됐고 별도 컴파일·재생·테스트·Profiler 실행 증거는 없다. §6~8의 옛 통과 수치를 이번 변경 검증으로 사용하지 않는다. 커밋·push·외부 전달은 하지 않았다.
