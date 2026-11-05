# 서비스 조회 키 계층 분리 — Framework 공유 메모

2026-10-17, 미전달. 사용자 요청으로 구현했다. [설계·Task·검증](../specs/2026-10-17-service-key-layers-design.md).

기존 `ServiceKeys/SystemGroupsService.cs`와 `DataBaseGroupServiceKey.cs`의 여러 키 컨테이너를 Factory 소유 계층에 맞춰 분리했다. 그룹 키와 개별 시스템 키도 별도 파일로 나눴다. 각 파일의 네임스페이스는 폴더 경로와 일치한다.

| 이전 참조 | 새 참조 (`ProjectL.Systems.ServiceKeys` 기준) |
|---|---|
| `SystemGroupsService.Global/InGame/LifeCycle` | `Runtime.SystemGroupsService`의 같은 필드 |
| `BufferSystemKey.Buffer` | `Runtime.BufferSystemKey.Buffer` |
| `SystemGroupsService.Render/Utility/Input/Loding/Job` | `Runtime.Global.GlobalSystemGroupServiceKey`의 같은 필드 |
| `SystemGroupsService.UI`, `AudioSystemServiceKey.Audio` | `Runtime.Global.GlobalSystemServiceKey.UI/Audio` |
| `RenderSystemServiceKey.CameraGroup` | `Runtime.Global.Render.RenderSystemGroupServiceKey.CameraGroup` |
| `RenderSystemServiceKey`의 나머지 필드, `CameraSystemServiceKey` | `Runtime.Global.Render`의 같은 컨테이너·필드 |
| Input·Job·Loding·Utility 내부 시스템 키 | `Runtime.Global`의 같은 컨테이너·필드 |
| `InGameSystemGroupServiceKey.Core/View` | `Runtime.InGame.InGameSystemGroupServiceKey.Core/View` |
| `InGameSystemServiceKey` | `Runtime.InGame.Core.InGameCoreSystemServiceKey`의 같은 필드 |
| `LifeCycleSystemServiceKey` | `Runtime.LifeCycle.LifeCycleSystemServiceKey` |
| Buffer별 키 | `Runtime.Buffer`의 같은 컨테이너·필드 |
| 입력 수신 슬롯 키 | `Runtime`의 같은 컨테이너·필드 |
| `DataBaseGroupKey.Render/InGame/View` | `DBFactory.DataBaseGroupKey`의 같은 필드 |
| `DataBaseGroupKey.ArtifactBoardConfig` | `DBFactory.DataBaseServiceKey.ArtifactBoardConfig` |
| `RenderTableKey`, `InGameTableKey`, `ViewTableKey` | `DBFactory.Group`의 같은 컨테이너·필드 |

키는 총 137개이며 기존 필드 타입·이름·Name을 유지한다. 호환 별칭이나 중복 정의를 추가하지 않았다. 원본 키 파일 8개의 `.meta` GUID는 대표 컨테이너로 이동하고 새 파일·폴더에 새 GUID를 부여했다. 정적 키 클래스여서 MovedFrom이 필요한 직렬화 타입은 없다.

`Framework/SystemBase`의 키 타입·Reader·Store와 서비스 등록·초기화·Tick·종료·Factory 구현은 변경하지 않았다. 루트에서 하위 서비스를 직접 조회하면 기존처럼 실패하며 부모 Context를 통해 접근해야 한다. CoreSystem 변경은 using과 그룹의 키 참조뿐이고 [DB 경계 handoff §5.44](2026-09-24-view-core-data-boundary-handoff.md#544-서비스-조회-키의-factory-계층-분리-2026-10-17-미전달)에 기록했다.

진행 중이던 Render 팩토리 이동·CameraCatalogSystem 변경은 보존했다. 과거 날짜별 계획·인계는 이력으로 남겼으며 현재 키 위치는 위 표와 개발 가이드 §5를 기준으로 확인한다.
