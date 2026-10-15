# 스테이지 선택 입력·요청 Buffer 사용 안내

- 작성일: 2026-09-30
- 작성 범위: 호진 작업 — 맵에서 스테이지를 선택하는 View 입력 기반
- 전달 대상: 맵 View 연결 담당, Core 요청 처리 담당, Framework 조립 담당
- 기준: [용근 작업자의 입력 사용 안내](stage-input-guide.md) §7과 현재 소스
- 상태: 입력 골격·StageViewBuffer·Factory 등록 구현. 실제 맵 표시와 Core 요청 처리 연결은 미구현.
- 이 문서는 사용·인계 안내이며, 아래 예시가 실제 화면 코드로 구현됐다는 뜻은 아니다.

## 1. 무엇을 준비했나

맵 선택 입력과 실행 요청 보관 경로를 준비했다. 용근 작업의 배치·전투 입력은 유지하며 공용 UnityInputBridge와 InputSystem을 재사용한다.

| 구성 | 현재 역할 |
|---|---|
| StageSelectionInputController | 포인터·선택·확인·취소 입력 해석 |
| IStageSelectionInputReceiver | 실제 맵 View가 구현할 수신 계약 |
| StageSelectionReceiverSlot | View 연결·해제와 입력 전달 |
| StageSelectionInputLayers.Selection | Common + StageSelection 맵 활성화 |
| StageViewBuffer | 번호가 있는 노드 선택 요청을 목록으로 보관 |

```text
PlayerInputActions / StageSelection
 → UnityInputBridge → InputSystem
 → StageSelectionInputController → StageSelectionReceiverSlot
 → 실제 맵 View [미구현]: 화면 좌표에서 노드 식별
 → StageViewBuffer: 선택 요청 보관
 → Core 처리 계층 [미연결]: 판단·진입 준비·결과 기록
 → View [미연결]: 결과 확인·표시·해당 요청 정리
```

InputSystem과 UnityInputBridge의 코드 위치는 Systems/GlobalSystem이다. 공용 Framework는 서비스 저장소와 런타임 생명주기를 제공하며 노드 선택 규칙을 알지 못한다.

## 2. 입력과 키 설정

설정 파일: [PlayerInputActions.inputactions](../../../Assets/ProjectL/Input/PlayerInputActions.inputactions)

| 액션 | 현재 바인딩 | View 수신 메서드 |
|---|---|---|
| PointerPosition | Pointer position | PointerMoved(x, y) |
| Select | Pointer press, ReleaseOnly | SelectPressed / SelectReleased / SelectCanceled |
| Confirm | Enter | ConfirmRequested() |
| Cancel | 마우스 오른쪽 버튼 | CancelRequested() |

Select는 Button + Press(behavior=1), initialStateCheck=false 계약이다. 정상 뗌은 Performed, 강제 종료는 Canceled로 구분한다. 포인터 Canceled의 기본 좌표는 마지막 위치를 덮어쓰지 않는다.

클릭 즉시 요청할지, 후보 선택 후 Confirm에서 요청할지는 아직 정하지 않았다. 양쪽에서 동일 요청을 중복 기록하지 않도록 실제 View 설계에서 하나의 확정 경로를 정한다.

## 3. View에서 연결하기

수신 계약:

```csharp
public interface IStageSelectionInputReceiver
{
    void PointerMoved(float screenX, float screenY);
    void SelectPressed(float screenX, float screenY);
    void SelectReleased(float screenX, float screenY);
    void SelectCanceled();
    void ConfirmRequested();
    void CancelRequested();
}
```

다음은 수신 View 내부에 들어갈 코드 조각이다. 서비스 키와 Layer는 internal이므로 현재 ProjectL.Systems 어셈블리 안에서 사용한다. 다른 어셈블리에서 쓰려면 공개 계약을 별도로 검토한다.

```csharp
// PostBindService에서 연결
_slot = resolver.GetService(StageSelectionInputKey.ReceiverSlot);
_layers = resolver.GetService(SystemGroupsService.Global)
    .InputServiceCommand.InputLayerServices;
var buffers = resolver.GetService(BufferSystemKey.Buffer);
_commands = buffers.StageViewWriteSink;

// 화면 진입: 초기화가 끝난 뒤
_slot.Bind(this);
_layers.ChangeLayer(StageSelectionInputLayers.Selection);

// 화면 종료: 다음 화면의 입력을 켜기 전에
_layers.ClearLayer();
_slot.Unbind(this);
```

필드 타입은 IStageSelectionReceiverSlot, IInputLayerService, IStageViewBufferWriteSink다. 필요한 네임스페이스는 ServiceKeys, GlobalSystem.InputSystem.View, InGameSystem.ViewSystem.StageSelectionInput, BufferSystems.StageView이며 모두 ProjectL.Systems 아래에 있다.

## 4. 요청 Buffer 사용

View에서 실제 노드가 식별되고 선택을 확정한 뒤 기록한다.

```csharp
int number = _commands.Submit(
    StageViewCommandData.SelectStageNode(nodeId));
```

nodeId는 기존 Core.Contracts.Ids.StageNodeId다. 요청에는 Number와 Data(Kind, NodeId)만 있다. DB 행, 그래프 전체, GameObject, 진입 가능 여부를 담지 않는다.

| API | 사용자 | 동작 |
|---|---|---|
| StageViewReadSink.Commands | Core 및 View | 삭제되지 않은 요청 목록 조회 |
| TryGetNextCommand(handledThrough, out command) | Core 처리 계층 | 지정 번호보다 큰 첫 요청 조회. 삭제하지 않음 |
| StageViewWriteSink.Submit(data) | View | 요청을 추가하고 번호 반환 |
| RemoveCommand(number) | View | 결과 확인된 요청 하나 삭제. 없으면 false |

Core 처리 계층은 읽기 계약만 사용한다. Core.Application/Domain이 Systems Buffer를 직접 참조하도록 바꾸지 않는다. 요청 조회 후 기존 Core 명령으로 변환하는 연결은 Systems 처리 계층 담당이다.

handledThrough는 **순서대로 처리 완료된 마지막 번호**다. 임의로 완료된 최대 번호를 넣으면 앞의 미처리 요청을 건너뛸 수 있다. 처리 중 중복 Job 등록 방지는 Core 측 실행 관리가 맡는다.

번호는 이 Buffer 인스턴스 안에서 증가하며 요청 삭제로 초기화되지 않는다. ArtifactViewBuffer·CombatViewBuffer 번호와 별개이므로 공동 결과 저장소에서는 Buffer 종류도 구분해야 한다.

Submit은 Core 실행 호출이 아니다. Core 결과를 확인하기 전에 RemoveCommand를 호출하지 않는다. 승인뿐 아니라 거절된 요청도 결과 확인 후 정리해야 한다.

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

## 5. 담당별 남은 연결

| 담당 | 남은 작업 |
|---|---|
| 맵 View | 맵 표시, 좌표→노드 판별, UI 입력 차단, 선택 확정, 결과 표시 |
| Core 처리 | StageViewReadSink 조회, 기존 PrepareStageEntry/ConfirmStageEntry 경로 연결, 요청 번호별 결과 기록 |
| 진행·Framework 조립 | 실제 런타임 구동, 화면 진입·종료 시점, DB 초기화 연결 |

StageProgressBuffer와 CurrentProgressBuffer는 Core 상태를 읽는 용도다. StageViewBuffer는 View 입력을 보관한다. View가 상태 Buffer를 쓰거나 Core 진입 API를 직접 호출하는 방식으로 우회하지 않는다.

진입 가능한 노드 표시를 위한 읽기 데이터와 현재 위치 마커의 의미는 별도 계약 확인이 필요하다. 요청 결과 저장 위치·보관 기간·화면 전환 또는 새 Run 때 미처리 요청을 정리하는 정책도 아직 확정하지 않았다. 이 Buffer에는 Clear API가 없다.

## 6. 구현 위치와 검증 범위

### 공용 BufferSystemGroup 변경 범위와 공유 사항

`BufferSystems/Core/BufferSystemGroup.cs`는 여러 Buffer의 서비스를 모아 제공하는 공용 조립 코드다. 폴더 이름의 Core만으로 파일 전체를 Core 게임 로직으로 판단하지 않는다. 다만 같은 파일에 게임 상태 변경 처리도 있으므로 수정 범위를 구분해야 한다.

이번 스테이지 선택 작업에서는 사용자 승인 범위인 입력 Buffer 등록을 위해 다음만 추가했다.

- `IBufferQuery`와 `BufferSystemGroup`에 `StageViewReadSink` / `StageViewWriteSink` 프로퍼티 추가.
- 생성자에서 `StageViewBufferKey.Read` / `Write`로 서비스를 조회하여 연결.
- 관련 조립 파일인 `BufferSystemFactory`에 생성·등록 추가, `DataBaseGroupServiceKey`에 조회 키 추가.

`ResetRunProgress`, `PrepareBattle`, `Apply`, `EndBattle` 등 기존 게임 상태 변경 로직은 수정하지 않았다. View 작업자가 입력 Buffer를 등록한다는 이유로 해당 로직이나 Core 소유 Buffer의 데이터 계약까지 임의로 변경하면 안 된다. 추가 변경이 필요하면 담당 작업자와 범위를 맞춘다.

Framework 작업자에게는 공용 조립 파일의 변경과 서비스 등록을, Core 작업자에게는 `StageViewReadSink`의 요청 조회 API를 공유한다. 공용 조회 객체에 WriteSink도 있다는 것이 Core의 입력 수정 권한을 뜻하지는 않는다. Core 처리 계층은 ReadSink만 사용한다. 이 문서는 공유할 내용이며 실제 전달 완료 기록은 아니다.

스크립트 루트는 Assets/ProjectL/Script/Systems/다.

- [입력 Controller](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageSelectionInput/StageSelectionInputController.cs): 같은 폴더에 Names, Layers, 수신 인터페이스, 슬롯.
- [서비스 키](../../../Assets/ProjectL/Script/Systems/ServiceKeys/StageSelectionInputServiceKey.cs)
- [StageViewBuffer](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/StageView/StageViewBuffer.cs): 같은 폴더에 Command와 CommandKind.
- [루트 Factory](../../../Assets/ProjectL/Script/Systems/Factory/Runtime/RunTimeSystemFactory.cs): 슬롯·Controller·Layer 등록.
- [Buffer Factory](../../../Assets/ProjectL/Script/Systems/Factory/Runtime/Buffer/BufferSystemFactory.cs): Buffer 생성. BufferSystemGroup과 DataBaseGroupServiceKey에서 조회 제공.

Unity 컴파일러와 프로젝트 참조를 사용한 별도 컴파일을 확인했다. Unity 에디터에서 맵을 띄워 실제 마우스·키보드로 검증한 상태는 아니다. 사용자 요청으로 이번 작업에서 추가한 전용 테스트 파일은 삭제했으므로 새 테스트 파일 실행을 선행 조건으로 안내하지 않는다.

화면 연결 시에는 선택 누름 중 Layer 전환·수신 대상 교체, UI 위 클릭, 재진입 시 중복 Bind, 승인·거절 결과 정리를 확인한다.
