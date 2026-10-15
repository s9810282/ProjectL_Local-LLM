# 아티팩트 입력·요청 Buffer 사용 안내

> **2026-10-06 최신 변경:** 사용자 요청으로 Artifact/Close와 아티팩트 ESC 닫기 연결을 제거했습니다. 팝업은 화면의 뒤로가기 버튼으로 닫습니다. Common/OpenSettings의 ESC 바인딩은 유지합니다. 아래 ESC 구현·검증 기록은 변경 전 이력입니다. 이번 변경 후 실제 Play 재검증은 별도입니다.

## 현재 연결 방식 (2026-10-06)

이하 2026-09-30 골격 설명은 과거 계약 기록이다. 현재 팝업은 `ArtifactViewBuffer`에 명령을 쓰지 않고 `ArtifactEditScheduler → ArtifactPlan → JobRunner → Job → CoreGroup.Artifacts`를 사용한다. 해당 요청 객체에서 결과를 읽고 Core 소유 ArtifactBuffer의 ReadSink에서 화면 상태를 읽는다. 최신 계약은 [Core 인계](../handoffs/2026-10-03-artifact-framework-view-handoff-ko.md), 진행·검증 원본은 [팝업 계획 §9](../plans/2026-10-05-artifact-popup-plan.md)를 따른다.

### 조작

1. Boot에서 StageSelect로 진입한 뒤 아티팩트 버튼을 누른다.
2. 하단 보유 항목을 누른 채 배치판으로 끈다. 공간상 가능하면 녹색, 겹치거나 범위 밖이면 빨간색 셀 프리뷰다.
3. 누른 상태에서 Q/E로 회전하고 보드에서 놓으면 배치한다. 실제 편집 승인 여부는 Core 결과로 확인한다.
4. 배치된 항목의 점유 칸을 누르고 끌면 이동한다. Q/E 회전도 같은 방식이다.
5. 배치된 항목을 하단 보유 목록 영역에 놓으면 반환한다. 버튼·상세 UI·임의 화면 밖에서 놓으면 취소한다.
6. 우클릭은 회수다(2026-10-06 사용자 변경, [상호작용 설계 §4](../specs/2026-10-06-artifact-popup-interaction-design.md)). 배치물을 끄는 중이면 그 배치물을, 끌고 있지 않으면 포인터 아래 배치물을 반환(Return) 요청한다. 보유 목록 항목을 끄는 중이면 임시 선택만 정리한다. 입력 액션 이름 `Cancel`은 호환을 위해 유지한다.
7. 닫기, Layer 강제 종료(SelectCanceled), Detach는 임시 드래그와 프리뷰만 정리하며 Return 요청을 만들지 않는다.

### 씬·DB와 수명

- SceneRoot의 Output Camera는 씬의 실제 Main Camera를 Inspector로 지정한다. 열 때마다 Camera.main/Find로 검색하지 않는다.
- WorldRoot 로컬 XZ의 Board Center/Cell Spacing을 Mapper와 모델·프리뷰 표시가 공유한다. 논리 크기는 Buffer에서 받는다.
- View 설정 Cell Scale은 Floor 전체 배율이고 Model Scale은 아티팩트 모델 배율이다. Preview Valid/Invalid Color, Background Distance/Margin도 View 설정이다.
- 배경은 출력 Camera 시야를 덮는 World Space 사각형이다. 렌더 직전에 카메라 위치·투영이 바뀐 경우에만 맞춘다. Overlay 차단은 투명하게 유지한다.
- 입력 Slot은 열 때 Bind, 닫기/Detach/StopInput 때 Unbind한다. 요청이 일치하는 최신 프리뷰만 표시하고 동일 후보에서는 새 요청을 만들지 않는다.
- 공용 자동 오류 정리는 아직 전투 콜백만 연결돼 있다. [협의 문서](../handoffs/2026-10-06-artifact-popup-fault-cleanup-coordination.md)의 합의 전에는 공용 콜백을 변경하지 않는다. 자체 StopInput이 있다고 자동 오류 정리가 연결된 것은 아니다.

---

- 작성일: 2026-09-30
- 작성 범위: 호진 작업 — 아티팩트 편집 View 입력 기반
- 전달 대상: 아티팩트 View 연결 담당, Core 아티팩트 처리 담당, Framework 조립 담당
- 기준: [용근 작업자의 입력 사용 안내](stage-input-guide.md), 현재 Core ArtifactService API
- 상태: 입력 골격·ArtifactViewBuffer·Factory 등록 구현. 실제 편집 View와 Core 요청 처리 연결은 미구현.
- 관련: [스테이지 선택 안내](stage-selection-input-guide.md), [아티팩트 DB·Buffer 인계](../handoffs/2026-09-28-artifact-db-migration-handoff.md)

## 1. 두 Buffer를 구분한다

| 이름 | 소유·쓰기 담당 | 읽기 담당 | 현재 상태 |
|---|---|---|---|
| ArtifactBuffer | Core 측 | View | Snapshot 도입 전 Artifacts / Add / Clear 골격으로 복원. ArtifactData는 빈 연결용 타입 |
| ArtifactViewBuffer | View | Core 처리 계층 및 View | Place / Move / Return 요청 보관 구현 |

ArtifactBuffer를 View 입력 보관함으로 사용하지 않는다. Core의 ArtifactState도 수정하지 않았다. 실제 보드 크기·보유 목록·배치 상태를 View에 제공하는 상태 계약은 Core 담당과 정해야 한다.

기존 Core 전달 문서는 Stage·Combat 두 입력 Buffer를 제안했지만, 이번에는 사용자 승인으로 ArtifactViewBuffer를 별도로 만들었다. Core 담당자는 추가된 읽기 계약을 연결해야 한다. 문서 작성만으로 Core 연결이나 외부 공유가 완료된 것은 아니다.

## 2. 전체 구조

```text
PlayerInputActions / Artifact
 → 기존 UnityInputBridge → InputSystem
 → ArtifactInputController → ArtifactInputSlot
 → 실제 아티팩트 View [미구현]
     목록/배치 항목 판별, 포인터→논리 칸, 선택 중 회전 표현
 → ArtifactViewBuffer
 → Core 처리 계층 [미연결]: 요청 읽기·기존 서비스 실행
 → Core 소유 상태·결과 저장 영역 [계약/연결 필요]
 → View [미연결]: 결과 표시·확인한 요청 정리
```

입력 계층은 Core·DB·Buffer·씬 오브젝트를 참조하지 않는다. 좌표를 무엇으로 해석할지는 View가 정하며, 배치 가능 여부와 효과 계산은 Core가 판단한다.

## 3. 액션과 키 변경

설정 파일: [PlayerInputActions.inputactions](../../../Assets/ProjectL/Input/PlayerInputActions.inputactions)

| 액션 | 현재 바인딩 | 의미 |
|---|---|---|
| PointerPosition | Pointer position | 화면 좌표 |
| Select | Pointer press, ReleaseOnly | 선택 누름·정상 뗌·강제 취소 |
| RotateLeft | 키보드 Q | 반시계 방향 90도 회전 요청 |
| RotateRight | 키보드 E | 시계 방향 90도 회전 요청 |
| Cancel | 마우스 오른쪽 버튼 | 회수(2026-10-06 변경, §현재 연결 방식 6). 액션 이름만 호환 유지 |

Q/E 바인딩은 2026-10-04에 추가했다([스테이지 선택 계획 §7](../plans/2026-10-03-stage-map-view-plan.md)). 9/30 작성 당시에는 RotateLeft/RotateRight에 바인딩이 없었다. 키를 바꾸려면 Input Actions 에디터에서 해당 액션의 바인딩을 수정한다. 액션 이름과 종류를 유지하면 키 변경에 C# 수정은 필요 없다. 회전은 PressOnly, 선택은 ReleaseOnly 계약이다.

회전 메서드는 **요청 전달만** 한다. 실제 선택 중 회전 값·표현 변경은 View 담당이다. 미배치 항목의 회전 키를 누를 때마다 실행 요청을 쌓지 않고, 배치 확정 시 최종 Rotation4를 기록하는 사용 방향이다.

## 4. View에서 입력 받기

```csharp
public interface IArtifactInputReceiver
{
    void PointerMoved(float screenX, float screenY);
    void SelectPressed(float screenX, float screenY);
    void SelectReleased(float screenX, float screenY);
    void SelectCanceled();
    void RotateLeft();
    void RotateRight();
    void Cancel();
}
```

아래는 실제 수신 View 안에서 사용할 코드 조각이다. 현재 서비스 키와 Layer는 internal이므로 ProjectL.Systems 어셈블리 내부 사용을 전제로 한다.

```csharp
// PostBindService
_slot = resolver.GetService(ArtifactInputKey.ReceiverSlot);
_layers = resolver.GetService(SystemGroupsService.Global)
    .InputServiceCommand.InputLayerServices;
_commands = resolver.GetService(BufferSystemKey.Buffer)
    .ArtifactViewWriteSink;

// 화면 진입: 초기화 완료 후
_slot.Bind(this);
_layers.ChangeLayer(ArtifactInputLayers.Artifact);

// 화면 종료: 다음 화면의 입력을 켜기 전에
_layers.ClearLayer();
_slot.Unbind(this);
```

필드 타입은 IArtifactInputSlot, IInputLayerService, IArtifactViewBufferWriteSink다. 필요한 네임스페이스는 ProjectL.Systems의 ServiceKeys, GlobalSystem.InputSystem.View, InGameSystem.ViewSystem.ArtifactInput, BufferSystems.ArtifactView다.

목록 클릭인지, 배치된 항목 이동인지, 빈 보드 클릭인지는 View가 구분한다. SelectCanceled에서는 임시 드래그·선택 표현만 정리하며 명령을 기록하지 않는다. Cancel은 사용자가 취소 액션을 실행한 것이고, Return 명령과 동일하지 않다. (2026-09-30 골격 기준 설명이다. 현재 팝업은 Cancel(우클릭)을 회수로 처리한다. §현재 연결 방식 6 참조.)

## 5. 요청 데이터와 식별자

현재 Core Application/Artifact/ArtifactService.cs의 API에 맞췄다.

| View 요청 | 식별자 | 기존 Core 메서드와 대응 |
|---|---|---|
| Place | ArtifactId: 아티팩트 종류 ID | Place(artifactId, anchor, rotation) |
| Move | InstanceId: 배치 개체 ID | Move(instanceId, newAnchor, newRotation) |
| Return | InstanceId: 배치 개체 ID | ReturnToPool(instanceId) |

예를 들어 같은 종류 ID 1001의 아티팩트가 개체 ID 7, 8로 구분될 수 있다. 최초 배치 API는 현재 종류 ID를 받아 Core가 보유 항목을 선택한다. View가 특정 보유 개체를 직접 골라 배치해야 한다면 Core API와 요청 계약 변경을 먼저 협의해야 한다. View에서 개체 ID를 발급하지 않는다.

CellCoord와 Rotation4는 기존 Core.Contracts.Common 타입을 재사용한다. 월드·화면 좌표, GameObject, SO, Core 상태 객체를 명령에 담지 않는다.

```csharp
// 실제 View가 사용자 조작을 확정한 뒤 호출
int placeNumber = _commands.Submit(
    ArtifactViewCommandData.Place(artifactId, cell, rotation));

int moveNumber = _commands.Submit(
    ArtifactViewCommandData.Move(instanceId, cell, rotation));

int returnNumber = _commands.Submit(
    ArtifactViewCommandData.Return(instanceId));
```

세 호출은 각각 별도의 사용 예시이며 한 번의 조작에서 모두 실행하지 않는다.

Buffer는 양수 ID·유효한 요청 종류·Rotation4 값처럼 형식만 검사한다. 보유 여부, 보드 경계, 겹침, 배치 허용과 효과 계산은 Core가 담당한다.

## 6. Core 읽기와 View 정리 계약

| API | 역할 |
|---|---|
| ArtifactViewReadSink.Commands | 아직 삭제되지 않은 요청 목록 |
| TryGetNextCommand(handledThrough, out command) | 지정 번호보다 큰 첫 요청 조회. 삭제하지 않음 |
| ArtifactViewWriteSink.Submit(data) | 목록에 보관하고 새 번호 반환 |
| RemoveCommand(number) | 해당 요청 하나만 삭제. 없으면 false |

요청은 Number와 Data(Kind, ArtifactId, InstanceId, Cell, Rotation)로 구성된다. 사용하지 않는 필드는 기본값이다. Core는 Kind에 맞는 필드만 읽는다.

- Core 처리 계층은 읽기 계약만 사용한다. Core.Application/Domain이 Systems Buffer를 직접 참조하지 않는다.
- 요청 조회·Submit은 즉시 Core 규칙 실행을 의미하지 않는다.
- 번호는 Buffer 인스턴스 내부에서 증가하며 삭제해도 재사용하지 않는다. Stage·Combat 번호와는 별개다.
- handledThrough는 순차 처리 완료 번호다. 처리 중 요청을 다음 Tick에 또 Job으로 등록하지 않도록 Core 실행 관리가 별도 상태를 유지해야 한다.
- 승인 또는 거절 결과를 View가 확인한 뒤 해당 번호를 RemoveCommand로 정리한다.
- 결과 저장 위치·보관 규칙·화면 전환과 새 Run에서 미처리 요청을 정리하는 정책은 미확정이다. Clear API는 없다.
- 배치 가능 미리보기 요청이나 결과 계약은 아직 없다. 실제 배치 요청을 미리보기 용도로 사용하지 않는다.

## 공통 입력 수명과 주의사항

- Controller와 Layer는 루트 Factory에서 모두 등록한다. 런타임 중 `Configure`를 다시 호출하지 않는다.
- 실제 View는 일반 C# 수신 객체로 구현할 수 있다. 이번 입력 골격은 MonoBehaviour 추가를 요구하지 않는다.
- 입력이 필요한 화면에 들어올 때 수신 대상을 먼저 Bind한 뒤 Layer를 켠다. 입력 서비스의 데이터 초기화와 런타임 Initialize 이후에 호출한다.
- 화면을 떠날 때는 전환 담당이 이전 입력을 끄고, 이전 View를 Unbind한 다음 새 View와 Layer를 연결한다. 늦게 실행된 이전 화면의 ClearLayer가 새 화면 입력까지 끄지 않도록 전환 순서를 한 곳에서 관리한다.
- 중복 Bind는 같은 대상이어도 예외다. 다른 대상에 대한 Unbind는 무시된다. 연결된 View는 파괴 전에 해제한다.
- 누름을 받은 View만 해당 뗌·취소를 받는다. 누르는 도중 View가 바뀌어도 이전 뗌이 새 View로 전달되지 않는다.
- SelectCanceled는 Layer 해제·화면 전환 등의 강제 종료다. 표시만 정리하고 실행 요청을 기록하지 않는다.
- 수신 대상이 없으면 입력은 버린다. 슬롯은 요청을 보관하는 Buffer가 아니다.
- UnityInputBridge를 재사용한다. 별도 PlayerInput이나 Keyboard.current로 입력 경로를 중복 생성하지 않는다.
- uGUI 버튼은 EventSystem 경로다. View의 같은 의미의 처리 메서드로 연결할 수 있지만, UI 위 클릭이 보드·맵 선택으로 중복 처리되지 않도록 차단하는 책임은 실제 View에 있다.
- Common 맵을 활성화하는 것과 설정 화면 동작의 구현 완료는 다르다. 해당 기능의 Controller와 수신 처리가 별도로 필요하다.

## 7. 남은 작업

| 담당 | 남은 작업 |
|---|---|
| 아티팩트 View | 목록·배치판 표시, 대상 판별, 논리 칸 변환, 회전 표현, 확정 시 요청 기록, 결과 표시 |
| Core 담당 | 상태 데이터 계약, ArtifactViewReadSink 소비, 요청 실행·중복 방지, 승인/거절 결과 제공 |
| 진행·Framework 조립 | 런타임 초기화, 화면 진입·종료 연결, 입력 Layer 전환 순서 |
| 표현 에셋 작업 | 현재 표현 SO 3종의 미지정 아이콘·프리팹 준비 |

이 입력 골격 자체는 MonoBehaviour를 추가하지 않는다. 실제 View의 씬 연결 방식은 후속 설계 대상이다. 화면 표시가 완료됐거나 Core 결과를 이미 받을 수 있다고 해석하지 않는다.

## 8. 파일 위치와 검증

### 공용 BufferSystemGroup 변경 범위와 공유 사항

`BufferSystems/Core/BufferSystemGroup.cs`는 공용 Buffer 서비스의 조립·조회 경로다. 이번 아티팩트 입력 작업에서는 사용자 승인 범위인 별도 입력 Buffer 등록을 위해 다음만 추가했다.

- `IBufferQuery`와 `BufferSystemGroup`에 `ArtifactViewReadSink` / `ArtifactViewWriteSink` 프로퍼티 추가.
- 생성자에서 `ArtifactViewBufferKey.Read` / `Write`로 서비스를 조회하여 연결.
- `BufferSystemFactory`에 생성·등록 추가, `DataBaseGroupServiceKey`에 조회 키 추가.

같은 파일의 `ResetRunProgress`, `PrepareBattle`, `Apply`, `EndBattle` 등 게임 상태 변경 로직은 수정하지 않았다. 입력 Buffer 등록을 위해 공용 조립 코드를 변경하는 것과 Core 소유 상태·처리 규칙을 변경하는 것은 별개다. 이번 등록은 기존 `ArtifactReadSink` / `ArtifactWriteSink`의 계약을 변경하지 않는다. 기존 `ArtifactBuffer`의 선택 복원은 별도 사용자 요청에 따른 작업이며 관련 인계 문서에 기록했다.

Framework 작업자에게는 공용 파일과 서비스 등록 변경을, Core 작업자에게는 새 `ArtifactViewReadSink` 및 기존 상태용 `ArtifactReadSink`와의 차이를 공유한다. Core 처리 계층은 새 입력 Buffer의 ReadSink만 사용한다. Core 상태 처리나 초기화 정책의 추가 변경은 담당 작업자와 범위를 맞춰야 한다. 이 문서는 공유할 내용이며 실제 전달 완료 기록은 아니다.

- [ArtifactInputController](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/ArtifactInput/ArtifactInputController.cs): 같은 폴더에 Names, Layers, 수신 인터페이스, Slot.
- [ArtifactInputServiceKey](../../../Assets/ProjectL/Script/Systems/ServiceKeys/ArtifactInputServiceKey.cs)
- [ArtifactViewBuffer](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/ArtifactView/ArtifactViewBuffer.cs): 같은 폴더에 Command와 CommandKind.
- [루트 Factory](../../../Assets/ProjectL/Script/Systems/Factory/Runtime/RunTimeSystemFactory.cs): 입력 슬롯·Controller·Layer 등록.
- [Buffer Factory](../../../Assets/ProjectL/Script/Systems/Factory/Runtime/Buffer/BufferSystemFactory.cs): Buffer 생성. BufferSystemGroup과 DataBaseGroupServiceKey에서 조회 제공.

Unity 컴파일러와 프로젝트 참조로 Systems 및 기존 Framework 테스트 어셈블리 컴파일을 확인했다. 액션 파일의 기존 맵 유지·추가 액션·GUID 중복 여부를 확인했다. 신규 테스트 파일은 만들지 않았고, Unity 에디터 실제 장치·화면 동작은 미검증이다.

화면 연결 뒤에는 회전 키 설정, 정상 뗌과 강제 취소 구분, 화면 전환 중 입력 차단, 요청 중복 기록 방지, Core 결과 대응을 확인한다.
