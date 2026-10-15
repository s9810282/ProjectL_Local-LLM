# 아티팩트 Framework 구현 — View/Flow 인수인계

작성일: 2026-10-03

대상: 아티팩트 화면, 입력, Job/Plan, LifeCycle 연결을 담당하는 View/Flow 프로그래머.

이 문서는 **아티팩트 배치 기능을 기존 Framework·Buffer 구조로 이전한 결과**를 연결하기 위한 인계 문서다. 전투 스테이지의 `EnterBattle` 변경 인계와는 별개다. 기존 [아티팩트 이전 완료 문서](2026-10-02-artifact-core-migration-handoff.md)의 기술 내용을 현재 코드 기준으로 정리했다.

## 1. 완료된 부분과 앞으로 연결할 부분

Core의 배치·이동·반환·확장·공간 프리뷰 계산, 기존 ArtifactBuffer의 상태/결과 저장, Framework 서비스 등록과 바인딩, 구형 상태 소유자 제거까지 완료됐다. 기존 ArtifactBuffer가 보유 목록과 배치 상태의 유일한 저장소다.

**실제 화면과 입력이 이 서비스로 연결된 것은 아니다.** 초기 보드 크기의 출처와 초기화 시점, 입력 전달, Job 실행/완료, 결과와 요청의 대응, 오래된 프리뷰 처리, 팝업 수명과 실제 UI 검증은 E3로 남아 있다.

| 구분 | 현재 상태 |
| --- | --- |
| A1/A2: 순수 배치 계산과 제안/프리뷰 | 완료 |
| E1: ArtifactBuffer 초기화·편집 결과·프리뷰 저장 경계 | 완료 |
| A3: Core 서비스, Factory 등록, DB/Buffer 바인딩 | 완료 |
| A4: 구형 Board/Pool/Map·Service·Lease와 교체/초기화 우회 제거 | 완료 |
| E3: 실제 bootstrap·입력·Job/Plan·LifeCycle·팝업 연결 | 미완료, 실행/View 담당 범위 |
| 실제 저작 데이터와 화면을 사용한 운영 검증 | 미완료 |
| 새 Run·재시작·저장/복구와 최종 아티팩트 초기화 정책 | 별도 결정 필요 |

## 2. 책임과 호출 경로

연결할 목표 흐름은 다음과 같다. 아래 흐름 전체가 이미 구현됐다는 뜻은 아니다.

```text
View가 입력 의도를 만든다
  → 기존 LifeCycle/Flow와 Register/Plan/Runner/Job 경로가 실행을 조율한다
  → Job이 CoreGroup.Artifacts의 해당 메서드를 한 번 호출한다
  → Core가 현재 Buffer 사실과 DB 정의로 계산하고 결과를 게시한다
  → View가 읽기 포트의 확정 상태·결과·프리뷰를 표시한다
```

| 담당 | 책임 |
| --- | --- |
| View | 클릭·드래그·회전 의도, 화면 좌표와 `CellCoord` 변환, 읽기 결과 표시, 연출 |
| Flow/Plan/Job/LifeCycle | 입력 의도 보관, 실행 시점, 한 번 실행/완료, 요청-결과 대응, 취소와 팝업 수명 |
| `ArtifactEditingService` | 현재 사실 조회, 편집 허용 여부 확인, 계산 포트 호출, 완성된 제안 게시 |
| 순수 Core 계산 | 회전·범위·충돌·재고 선택·배치 변경·공간 프리뷰 |
| `ArtifactBuffer` | 보유/배치 상태, 초기화 여부, 마지막 편집 결과·프리뷰 저장 |

View/Job은 별도 아티팩트 서비스·보드·풀·점유맵을 생성하거나 계산기를 직접 호출하지 않는다. 화면 표시용 드래그 상태는 가질 수 있지만, 그것을 실제 배치 상태로 취급하면 안 된다. 변경 후 화면의 기준은 Buffer에 게시된 상태다.

Factory만 구체 `ArtifactEditCalculator`를 생성한다. 실행 서비스는 Contracts의 `IArtifactEditCalculation`만 사용한다. 기존 Factory/`ExportServices`/`InitData`/`PostBindService`와 그룹 전달 경로를 유지한다.

## 3. View/Job이 사용할 Core API

기존 `IInGameSystemGroupService.CoreGroup.Artifacts`로 접근한다. 타입은 `IArtifactEditingService`이며, 기존 Core 그룹의 서비스 키는 `InGameSystemServiceKey.Artifacts`다. 새 루트 서비스나 별도 실행기를 만들 필요가 없다.

```csharp
bool InitializeBoard(int width, int height);
void Place(int artifactId, CellCoord anchor, Rotation4 rotation);
void Move(int instanceId, CellCoord anchor, Rotation4 rotation);
void ReturnToPool(int instanceId);
void ExpandBoard(int width, int height);
void PreviewPlace(int artifactId, CellCoord anchor, Rotation4 rotation);
void PreviewMove(int instanceId, CellCoord anchor, Rotation4 rotation);
```

| 호출 | 입력과 의미 |
| --- | --- |
| `InitializeBoard` | 신뢰할 수 있는 저작/설정상의 보드 크기로 초기 보유 목록을 구성한다. 초기화 여부를 나타내는 별도 `bool` 반환이다. |
| `Place` | 정의 ID인 `artifactId`로 Pool의 첫 번째 일치 아이템을 선택해 배치한다. 특정 Pool 인스턴스를 지정하는 API는 아니다. |
| `Move` | 이미 배치된 `instanceId`의 위치·회전을 변경한다. 자신의 기존 셀과 겹치는 이동은 허용한다. |
| `ReturnToPool` | 배치된 `instanceId`를 제거하고 Pool에 새 인스턴스를 추가한다. ID 주의사항은 아래 참조. |
| `ExpandBoard` | 목표 너비/높이로 확장한다. 비용 지불이나 보상 정책을 추가하는 호출은 아니다. |
| `PreviewPlace` | 지정한 정의의 공간 배치 가능성만 계산한다. 재고나 편집 권한은 보장하지 않는다. |
| `PreviewMove` | 배치 인스턴스의 후보 위치·회전을 공간 프리뷰로 계산한다. 실제 이동은 별도 호출이다. |

`artifactId`는 종류/정의 ID, `instanceId`는 보유·배치된 개별 아이템 ID다. **둘 다 0이 유효할 수 있다.** 0을 없음으로 취급하거나, 종류 ID를 개별 아이템 ID로 대신 사용하지 않는다.

`ReturnToPool`은 반환된 아이템에 기존 ID를 재사용하지 않는다. 현재 Pool/Placed 전체의 최대 ID보다 큰 새 ID를 발급하며, 결과의 `InstanceId`는 제거된 배치 아이템의 ID다. 결과 ID로 Pool 행을 그대로 추가하지 말고, 최신 `Current.PoolItems`를 다시 읽어 화면을 갱신한다.

동일 위치·회전으로 `Move`하면 성공한 변경 없음으로 처리될 수 있다. 확장 성공은 개별 대상이 없으므로 결과의 아이템 ID가 null일 수 있다.

## 4. 상태와 결과를 읽는 방법

기존 Buffer 그룹의 바인딩 경로에서 필요한 읽기 권한을 받는다. 모든 포트는 **같은 ArtifactBuffer 인스턴스**를 바라본다. 자식 키가 루트 resolver에 직접 공개돼 있다고 가정하지 않는다.

| 읽기 포트 | Buffer 그룹 속성 | 읽는 값 |
| --- | --- | --- |
| `IArtifactBufferReadSink` | `ArtifactReadSink` | `Current` |
| `IArtifactEditResultReadSink` | `ArtifactEditResultReadSink` | `LastEditResult` — `ArtifactActionResult?` |
| `IArtifactPreviewReadSink` | `ArtifactPreviewReadSink` | `LastPreview` — `ArtifactPlacementPreview?` |

`Current`는 `ArtifactBufferSnapshot`이며, `IsInitialized`, `BoardWidth`, `BoardHeight`, `PoolItems`, `PlacedItems`를 제공한다. 각 `ArtifactData`에는 `ArtifactId`, `InstanceId`, `IsPlaced`, `Anchor`, `Rotation`, `Cells`가 있다. 목록과 셀은 읽기 전용 복사본이며, 이전 스냅샷이 나중 편집으로 바뀌지 않는다. 화면 갱신 시 최신 `Current`를 읽는다.

초기화/편집/프리뷰 쓰기 포트도 같은 Buffer에 있지만 **Core 게시용 권한**이다. View가 `TryInitialize`, `PublishEdit`, `PublishPreview`로 직접 상태나 결과를 만들어 넣는 연결은 사용하지 않는다. `ToCalculationState()` 역시 Core의 계산 입력 변환용이며, View가 조립해서 계산기에 넘길 필요가 없다.

## 5. 편집 완료와 실패 처리

`Place`, `Move`, `ReturnToPool`, `ExpandBoard`는 `void`다. 성공 여부를 반환값이나 예외 유무만으로 추측하지 말고, **해당 호출이 정상 종료된 뒤 게시된 `LastEditResult`**로 판단한다.

| 상황 | Core/Buffer 동작 | 실행/View 처리 |
| --- | --- | --- |
| 편집 수락 | 상태와 성공 결과를 함께 게시하고 `LastPreview`를 지운다. | 확정 `Current`를 다시 읽고 성공 연출을 한다. |
| 정상적인 게임 규칙 거절 | 상태는 보존하고 실패 결과를 게시한다. `LastPreview`도 지운다. | 해당 요청은 정상적으로 한 번 완료하고 실패 사유를 표시한다. |
| 초기화/바인딩 누락, 정의·계산·오버플로·게시 예외 | 예외가 전파된다. 게시 이전 예외는 이전 상태와 출력이 유지된다. | 현재 요청의 실패/오류로 처리한다. 이전 결과를 현재 요청의 성공으로 읽지 않는다. |

`ArtifactActionResult`는 `Succeeded`, `FailureReason`, nullable `ArtifactId`/`InstanceId`를 제공한다. 실패는 대상 ID가 null이다. `LastEditResult == null`은 아직 게시된 편집 결과가 없다는 뜻이며, 실패 결과와 다르다.

주요 실패 사유:

| 값 | 표시/처리할 의미 |
| --- | --- |
| `LayoutLocked` | 아직 전투가 보존 중이어서 편집 불가 |
| `DefinitionNotFound` | 요청한 정의 없음 |
| `NotInPool` | 실제 배치할 재고 없음 |
| `InstanceNotFound` | 이동/반환할 배치 인스턴스 없음 |
| `OutOfBounds` | 보드 밖 배치 |
| `CellOccupied` | 다른 아이템과 충돌 |
| `InvalidBoardSize` | 유효하지 않은 보드 크기 |
| `BoardShrinkNotAllowed` | 축소 요청 |
| `BoardSizeUnchanged` | 같은 크기로 확장 요청 |
| `BoardRebuildFailed` | 확장 시 기존 배치 복원 실패, 상태 점검 필요 |

전체 enum은 `ArtifactActionFailureReason.cs`가 기준이다. 구형 `RevisionOverflow` 숫자는 예약값으로 남아 있으며, 새로운 revision 정책이나 재시도 동작을 의미하지 않는다.

**결과에는 요청 번호·처리 순번·acknowledge API가 없다.** `LastEditResult`는 마지막 값 하나이지 결과 큐가 아니다. 여러 요청을 실행한 뒤 마지막 값만 보고 모든 요청이 처리됐다고 판단할 수 없다. 기존 실행 경로 안에서 직렬 실행/즉시 결과 소비 등 요청과 결과를 대응시키는 방식을 E3에서 정하고 검증해야 한다. 반복 폴링으로 동일 성공을 다시 연출하거나, 예외를 매 Tick 자동 재실행하지 않는다. 필요한 계약 확장은 담당자 협의 사항이며 이미 구현된 것으로 가정하지 않는다.

## 6. 프리뷰와 좌표 표시

`LastPreview`는 `Shape`, `Anchor`, `FailureReason`을 제공한다. 편집 결과와 다른 채널이며, 호출 시 상태·재고·효과를 변경하지 않는다. 전투가 보존 중이어도 공간 프리뷰 계산 자체는 가능하다.

- `None`: 공간상 배치 가능. 실제 재고나 편집 가능 여부까지 수락된 것은 아니다.
- `OutOfBounds` / `CellOccupied`: 회전된 `Shape`가 포함된다. 후보 영역을 실패 색상으로 표시할 수 있다.
- `DefinitionNotFound` / `InstanceNotFound`: `Shape`가 null이다. 존재하지 않는 모양을 임의로 만들어 표시하지 않는다.

`ArtifactShapeSnapshot`에는 `Width`, `Height`, 회전된 **로컬** `Cells`가 있다. 프리뷰의 점유 셀은 `Anchor + Shape.Cells`를 화면 좌표로 변환한다. Core가 이미 회전을 적용했으므로 다시 회전시키지 않는다.

`Current.PlacedItems[].Cells`는 이미 회전·이동된 **절대 보드 좌표**다. `Anchor`를 다시 더하지 않는다. `PoolItems[].Cells`는 정의 기준 로컬 셀이다. 화면 픽셀/RectTransform으로의 변환은 View가 담당한다.

프리뷰 역시 요청 번호가 없다. 오래된 hover/회전 결과를 현재 드래그의 결과로 쓰지 않도록 실행 순서, 취소/교체, 팝업 종료 후 지연 결과 처리를 E3에서 정한다. 재고가 없어도 공간 프리뷰는 성공할 수 있으므로, 최종 배치 성공은 편집 결과로만 확인한다.

동일 정의 ID의 모양이 변경되는 특수 상황에서는 `PreviewMove`가 새 정의로 공간 실패를 보여도, 동일 위치·회전의 `Move`는 기존 배치를 보존하는 성공일 수 있다. 현재 호환 동작이다. 프리뷰를 실제 편집 결과로 대체하지 않는다.

## 7. 초기화·편집 잠금·보존 수명

서비스 등록과 바인딩만으로 초기 재고를 지급하지 않는다. 올바른 bootstrap 실행 주체가 저작된 보드 크기로 `InitializeBoard`를 호출해야 한다. 임의의 기본 크기나 팝업마다 신규 보드 생성 정책은 이번 구현에 포함되지 않는다.

첫 초기화는 DB 정의를 `ArtifactId` 순으로 읽고 `InitialCount`에 따라 재고를 구성한다. 같은 Buffer 수명에서 재호출하면 `false`를 반환하고 기존 상태·출력·크기를 보존한다. `false`를 새 재고 지급이나 초기화 실패 팝업의 근거로 단정하지 말고, 기존 `Current.IsInitialized`와 bootstrap 의도를 함께 확인한다.

DB 초기화/바인딩 전 호출이나 보드 초기화 전 편집·프리뷰 호출은 예외다. 첫 초기화를 전투 중에 시도해도 예외다. `InitializeBoard`의 `bool`은 편집 결과 채널과 별개다.

편집 잠금의 최종 기준은 **`CombatStateReadSink.HasBattle`**이다. 배치 준비, 진행 중 전투, 종료 결과를 연출하는 동안 모두 잠긴다. `Finished` 페이즈만 보고 편집을 열면 안 된다. 정상 `EndBattle`로 보존 전투가 정리된 뒤에 허용된다. View가 버튼을 미리 비활성화할 수는 있지만, 실제 실행 시의 판정은 Core가 다시 한다.

팝업 닫기, 챕터/스테이지 이동, 전투 정리, 일반 로비 복귀, `ResetRunProgress`는 아티팩트 재고를 지우거나 다시 지급하는 근거가 아니다. 구형 `IArtifactBufferWriteSink`, `ArtifactWriteSink`, `SetSnapshot`, `Clear`, `ArtifactBufferKey.Write`는 제거됐다. View가 초기화를 위해 우회 복원하지 않는다.

현재 ArtifactBuffer에는 출력만 지우는 공개 API도 없다. 팝업을 닫았다고 재고를 지우면 안 된다. 표시를 숨기고 오래된 출력을 무시하는 View 측 수명 처리와, Buffer 출력 정리 권한이 추가로 필요한지 여부는 E3에서 구분해 정한다. 새 Run/재시작/저장 복구의 최종 재고 초기화 권한은 별도 정책이다.

## 8. 현재 Job/Plan/LifeCycle 연결 상태와 E3 체크리스트

`JobRootType.Artifact`와 기존 Register 경로는 있다. 그러나 `ArtifactPlan`은 현재 `JobType.Start`에서 임시 `ArtifactJob`을 만들고, 그 Job은 실제 아티팩트 작업 없이 완료된다. `ArtifactLifeCycle`도 진입/종료 Flow 연결이 TODO인 골격이다. **Start를 등록했다고 보드 초기화나 배치가 실행되는 상태는 아니다.**

`Register(JobRootType, JobType)`와 `ArtifactPlan.Register(service, jobType)`에는 아이템/좌표/회전 인자를 전달하는 계약이 없다. `ProcessCommand`/`ProcessPreview` enum 값이 다른 경로에 있더라도 Artifact Plan이 이를 처리하는 구현은 없다. 연결은 단순히 메서드 한 줄 교체로 끝나지 않는다.

- [ ] E3-1: 저작된 보드 크기의 출처, 초기화 실행 주체·시점, 서비스 바인딩 완료 조건을 정하고 실제 bootstrap으로 한 번 초기화한다.
- [ ] E3-2: 기존 입력/등록/Plan/Job 경로에서 편집·프리뷰의 종류와 `artifactId` 또는 `instanceId`, 좌표, 회전을 실행까지 전달한다. 늦게 바뀐 다른 클릭을 읽거나, 확정 배치를 요청 보관용으로 바꾸지 않는다.
- [ ] E3-3: 선택한 Job이 Core API를 한 번 실행하고, 성공·게임 규칙 거절·예외를 구분한다. 정상 거절도 한 번 완료하며, 오류의 종료/복구 경로를 Framework 계약에 맞춰 정한다.
- [ ] E3-4: 요청 번호가 없는 마지막 결과/프리뷰와 실제 요청의 대응, 연출 한 번 처리, 빠른 입력의 직렬화/교체와 오래된 hover 결과 무시를 정한다.
- [ ] E3-5: 기존 LifeCycle/Flow에서 팝업 열기/닫기, 입력 중단, 지연 Job 취소/배출, 재개방과 결과 표시 수명을 연결한다. 출력 정리와 재고 초기화를 혼동하지 않는다.
- [ ] E3-6: 최신 Buffer 기준으로 Pool/Placed 표시를 갱신한다. 0 ID, 같은 종류 여러 개, 반환 후 새 Pool ID, 절대/로컬 셀 좌표, 동일 이동 성공을 검증한다.
- [ ] E3-7: 초기화 재호출, 재고 없음, 범위/충돌, 전투 준비·활성·종료 연출 중 잠금, 정리 후 편집, 빠른 hover/팝업 종료 후 결과 도착을 실제 실행 경로에서 검증한다.
- [ ] E3-8: 실제 DB 정의·View 리소스와 UI/PlayMode 검증을 수행한다. fixture의 서비스 호출 성공을 운영 화면 연결 완료로 취급하지 않는다.

기존 전투 처리는 Buffer의 `PlacedItems`에서 배치 효과를 읽는다. Pool 아이템이나 hover 프리뷰는 효과 입력이 아니다. View는 편집/hover 때 체력·공격력 등을 별도로 적용하지 않는다. 지원 효과의 실제 데이터/자원 완성도는 별도 확인 대상이다.

## 9. 근거와 참조 파일

이번 요청에서는 문서만 작성했다. 아래 결과는 이미 완료된 코드 검증 기록이며, 문서 작성으로 Unity 테스트를 다시 실행한 것은 아니다.

- 아티팩트 A4 완료 검증: 전체 1300/1305, 서비스 30/30. 실패 5개는 기존 Economy 저작 데이터 문제와 동일했다.
- 이후 P3 최종 회귀 기록: 전체 1371/1376, `ArtifactEditingServiceTests` 30/30. 같은 5개 실패 외 새 실패는 없었다. 전투 진입 테스트 추가로 전체 수가 달라졌으며, 이것은 아티팩트 E3 UI 검증 결과가 아니다.
- 기존 아티팩트 이전의 독립 리뷰에서 수정이 필요한 결함은 없었다. 실제 화면·입력·Job 연결과 저작 데이터는 별도 미완료다.

| 확인할 코드 | 목적 |
| --- | --- |
| [ArtifactEditingService.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/ArtifactSystem/ArtifactEditingService.cs) | 공개 API, 바인딩, 초기화·잠금·게시 |
| [InGameCoreSystemFactory.cs](../../../Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/Core/InGameCoreSystemFactory.cs) / [InGameCoreSystemGroup.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/InGameCoreSystemGroup.cs) | 기존 등록과 `Artifacts` 노출 |
| [ArtifactBuffer.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Artifact/ArtifactBuffer.cs) / [ArtifactBufferSnapshot.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Artifact/ArtifactBufferSnapshot.cs) | 읽기 권한과 상태·결과 저장 |
| [ArtifactData.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/BufferSystems/Artifact/ArtifactData.cs) | 화면에서 읽을 아이템 사실 |
| [ArtifactActionResult.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Results/ArtifactActionResult.cs) / [ArtifactActionFailureReason.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Results/ArtifactActionFailureReason.cs) | 결과와 실패 사유 |
| [ArtifactPlacementPreview.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Artifact/ArtifactPlacementPreview.cs) / [ArtifactShapeSnapshot.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/CoreCaculator/Contracts/Artifact/ArtifactShapeSnapshot.cs) | 프리뷰와 로컬 셀 |
| [ArtifactPlan.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Artifact/Plans/ArtifactPlan.cs) / [ArtifactJob.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Artifact/Jobs/ArtifactJob.cs) / [ArtifactLifeCycle.cs](../../../Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Artifact/ArtifactLifeCycle.cs) | 실제 연결이 필요한 기존 골격 |
| [ArtifactEditingServiceTests.cs](../../../Assets/Tests/EditMode/Framework/ArtifactEditingServiceTests.cs) | 서비스 동작·잠금·보존·효과 연결 사례 |

세부 계산 및 이전 이력은 [A2 계산 인계](2026-10-02-artifact-a2-calculation-handoff.md), [E1 Buffer 인계](2026-10-02-artifact-e1-buffer-handoff.md), [A3 서비스 인계](2026-10-03-artifact-a3-service-handoff.md), [아티팩트 이전 완료 인계](2026-10-02-artifact-core-migration-handoff.md)에 남아 있다. 이전 단계 문서의 미완료 표시와 테스트 수는 당시 기록이며, 현재 완료 범위는 이 문서 1절과 이전 완료 문서를 기준으로 구분한다.
