# 아티팩트 팝업 배치·회전·반환 설계

작성일: 2026-10-06
상태: 사용자 UML 승인 후 Phase D 입력 구현. 검증·진행 상태 원본은 계획 §9. 공용 자동 오류 정리 연결은 협의 후 진행.

## 1. 사용자 요구와 불변 조건

StageSelect에서 버튼으로 팝업을 열고, 하단 보유 목록에서 배치판으로 드래그하여 배치한다. 배치된 항목은 이동·회전하고 하단 목록으로 반환할 수 있어야 한다. 배경은 스테이지 화면을 가리고 모델 뒤에 표시한다.

1. 기존 Framework 구조 유지가 최우선이다.
2. Core 코드는 임의 수정하지 않는다. 변경 필요 시 사용자에게 알리고 Core 작업자와 협의한다.
3. 전투 공용 경계 변경은 전투 작업자와 먼저 협의한다.
4. GTX 1060에서 250~300 FPS는 목표이며 검증 결과가 아니다. CPU·해상도·렌더링 조건을 기록한 Player 빌드에서 확인해야 한다.

## 2. 확인한 기존 경로

- ArtifactEditingService: Place(ArtifactId, Anchor, Rotation), Move(InstanceId, Anchor, Rotation), ReturnToPool(InstanceId), PreviewPlace, PreviewMove 제공. CanEdit는 전투 상태 HasBattle을 확인한다. 잠금은 정상 LayoutLocked 결과다.
- ArtifactEditScheduler: 편집 한 건, 최신 프리뷰 하나 및 대기 프리뷰 Job 최대 하나. 기존 Plan → JobRunner → Job 경로 재사용.
- ArtifactBuffer: Core 상태의 SSOT. PlacedItems.Cells는 절대 셀, PoolItems.Cells는 정의 로컬 셀이다.
- 프리뷰 Shape.Cells는 이미 회전된 로컬 셀이다. 화면에 표시할 때 Anchor만 더한다. 공간 프리뷰 성공은 재고·편집 권한 보장이 아니다.
- ArtifactInputController/Slot, UnityInputBridge, Artifact Layer 및 Q/E 바인딩이 이미 있다. 구형 artifact-input-guide의 ArtifactViewBuffer 경로는 최신 Core 인계와 다르므로 새 구현에서 사용하지 않는다.
- CameraCatalogSystem은 가상 카메라 제어 핸들이며 실제 출력 Camera 조회 API는 없다.

## 3. 구성과 책임

기존 ArtifactViewSystem은 수명·Layer·스케줄러·상태 갱신의 주인으로 유지한다.

- ArtifactSceneRoot: Inspector로 출력 Camera, 보드·배경·UI 참조 연결. 씬 진입 때 참조를 받아 보관한다.
- ArtifactBoardMapper(일반 C#): Camera Ray와 WorldRoot 로컬 보드 평면 교차, 중심·간격을 사용해 CellCoord 변환. Collider로 모델을 판별하지 않는다. Buffer의 절대 점유 셀로 배치 인스턴스를 찾는다.
- ArtifactInteraction(일반 C#): 선택 대상, 포인터, 임시 회전, 잡은 칸의 오프셋만 소유한다. 확정 상태·점유맵을 복제하지 않는다. Core 계산기를 직접 호출하지 않는다.
- ArtifactViewSystem: 기존 입력 Slot에 수신자를 Bind/Unbind하고 Interaction 의도를 Scheduler로 보낸다. 변경된 Buffer와 유효한 완료 프리뷰만 Presenter에 전달한다.
- Presenter: Floor 하나, 배치 모델, 드래그 모델, Core 프리뷰 셀, 목록 및 상세 문구 표시. 프리팹별 CustomPool과 명시적 대여 목록을 사용한다.
- View DB ArtifactPopupConfig: 보드 표현 중심·간격·높이, 배경 색·거리·여유 배율, 프리뷰 색·표현 설정. Core 논리 크기·모양·배치 규칙은 넣지 않는다.

## 4. 조작 계약

- Pool 슬롯에서 누름: 종류 ID를 선택한다. Core Place는 특정 Pool 인스턴스 선택 API가 아니므로 View가 인스턴스를 임의 지정하지 않는다.
- 배치 항목에서 누름: Buffer 절대 셀과 일치하는 InstanceId 및 원래 Anchor/Rotation을 받는다. 잡은 칸 오프셋을 보존한다.
- 드래그 중 Q/E: 임시 Rotation4만 변경하고 새 공간 프리뷰를 요청한다. 원본 배치 상태는 변경하지 않는다. 회전 시 Anchor 기준을 일관되게 유지한다.
- 보드에서 뗌: Pool 선택이면 Place, 배치 선택이면 Move 요청 하나를 등록한다. 가능 여부는 실행 결과로 확정한다.
- 배치 항목을 하단 반환 영역에 놓음: Return 요청 하나. 버튼·상세 UI·의도치 않은 화면 밖 해제는 취소로 처리하여 실수 반환을 막는다.
- 우클릭(2026-10-06 사용자 변경): 배치물 드래그 중에는 해당 InstanceId를 반환하고, 드래그 중이 아니면 포인터 아래 배치물을 반환한다. 보유 목록의 미배치 항목을 드래그 중이면 임시 선택만 정리한다. 기존 입력 액션 이름 Cancel은 호환성을 위해 유지한다.
- SelectCanceled, Layer 변경, 닫기, Detach: 임시 표현과 프리뷰를 정리하며 반환 요청을 만들지 않는다.
- 마지막 포인터 칸·회전·대상·Buffer 참조가 동일하면 새 프리뷰를 요청하지 않는다. 변경 직후 이전 프리뷰는 숨기고 최신 요청 결과만 표시한다.
- 투명 전체 화면 차단 Image는 Stage 입력을 막지만 보드 드래그 판정을 막아서는 안 된다. UI 버튼·목록·반환 영역은 별도로 구분한다. 이벤트 실행 순서에 의존해 슬롯을 선택하지 않도록 좌표 기반 대상 판정을 사용한다.

## 5. 배경·카메라 및 보드 정렬

배경은 출력 Camera 시야 전체를 덮는 World Space 표현이다. Camera 참조는 씬 진입 때 한 번 연결하며 팝업 열기마다 검색하지 않는다. 열 때와 열린 동안 Camera pose/lens/aspect 변경 시 화면 크기에 맞춘다. Orthographic와 Perspective를 구분한다. 배경은 보드 모델보다 뒤에 있어야 하며 Near/Far Clip 및 실제 모델 깊이를 검증한다.

카메라 최종 갱신과 Framework Tick 순서가 다르면 배경이 한 프레임 늦을 수 있다. 실제 Bootstrap/카메라 갱신 순서를 확인한 뒤 지원되는 갱신 지점을 선택한다. 기존 Framework에 LateTick이 있다고 가정하거나 전역 갱신 API를 임의 추가하지 않는다. 구현은 SRP beginCameraRendering 및 Built-in onPreCull을 구독하며 현재 출력 카메라·열린 팝업만 갱신한다. Attach/Detach에서 구독 수명을 관리한다.

Floor 모델의 실제 내부 배치 면과 논리 칸은 현재 일치한다고 검증되지 않았다. 모델 bounds 전체는 벽·계단을 포함하므로 배치 면으로 쓰지 않는다. 씬/표현 설정의 명시적인 보드 중심·높이·간격으로 4×4 셀을 맞추고, 클릭/모델/프리뷰가 같은 Mapper 좌표를 사용하도록 검증한다.

## 6. 성능 기준

닫힌 팝업의 입력·표시 갱신 없음. 매 프레임 Find, Instantiate/Destroy, LINQ, 목록 전체 Render, 새 EventSystem 결과 배열 생성을 피한다. 재사용 Raycast 목록·PointerEventData 및 풀 자원을 연결 시 준비한다. UI Raycast는 클릭/해제 및 필요한 포인터 변화 시 실행한다.

프리뷰 요청과 Core 결과에는 실제 할당이 있으므로 전체 드래그가 무할당이라고 주장하지 않는다. 동일 칸·회전에서는 요청을 만들지 않는다. 셀 이동 시 실행 수·할당량을 측정하고 Core 계산 할당이 병목이면 담당자 협의로 개선한다.

목표 검증: Player 빌드에서 해상도/CPU/GPU/VSync/프레임 제한을 기록하고 닫힘·열림·드래그·회전·연속 배치/반환의 CPU/GPU frame time, GC.Alloc 및 GC spike를 비교한다. Editor FPS나 컴파일 통과를 목표 성능 달성으로 기록하지 않는다.

## 7. 구현 전 협의 사항

공용 RuntimeFaultBoundary는 정리 콜백 하나만 허용한다. InGameViewSystemFactory는 현재 combatView.StopInput만 연결한다. GameBootstrap은 fault 이후 Tick을 중단하므로 ArtifactViewSystem.Tick에서 오류를 보고 정리하는 방식으로는 충분하지 않다.

권장 협의안: 조립 계층이 전투·아티팩트 StopInput을 호출하는 단일 정리 콜백을 소유하고, 한 정리 실패에도 다른 정리를 실행한다. 연결·해제 소유자와 Shutdown 순서를 함께 정한다. CombatViewSystem의 기존 DisconnectStopInput 소유권도 확인해야 한다. 협의 전에 Factory, RuntimeFaultBoundary, CombatViewSystem을 수정하지 않는다. 별도 오류 경계를 만들지 않는다.

Core 측 확인 사항: 기존 운영 아티팩트 규칙 에셋과 canonical effect compiler 불일치가 기록돼 있다. 배치 서비스 연결과 전투 효과 발동 검증은 별개다. 해당 에셋·컴파일러를 View에서 수정하지 않는다. 전투 진입/복귀, Run 재시작, 편집 잠금 해제 정책도 이번 작업에서 변경하지 않는다.

## 8. 검증과 구현 순서

1. 사용자 설계 검토·승인 후 독립된 Mapper·Interaction·표시·입력을 구현한다. 공용 오류 경계 연결은 협의 완료 및 결정 기록 뒤 진행한다.
2. 순수 Interaction·Mapper 테스트: 좌표, 잡은 칸 오프셋, 회전, 취소, Place/Move/Return ID 구분, UI 해제, 잠금 정상 거절, 오래된 프리뷰 폐기.
3. SceneRoot·View DB·Presenter 연결: 실제 배치 면 정렬, 배경 전체 화면, 프리뷰 셀의 Anchor와 회전 중복 방지, 풀 정리.
4. 기존 Slot/Layer/Scheduler 연결: 닫기·Detach·오류·Shutdown의 요청과 표현 수명 검증.
5. 실제 DB·Core 서비스로 배치→회전 이동→반환 검증. Core Buffer를 View가 쓰지 않는지 확인.
6. Unity 테스트 및 Boot→StageSelect 실제 입력 검증, 화면 비율/카메라 변화, 반복 열기·닫기, Player 성능 측정.

## 근거 문서 및 코드

- docs/superpowers/handoffs/2026-10-03-artifact-framework-view-handoff-ko.md
- docs/superpowers/plans/2026-10-05-artifact-popup-plan.md (Phase D, §10)
- docs/superpowers/viewDocs/runtime-scene-binding-guide.md
- docs/render-camera-catalog.md
- Assets/ProjectL/Script/Systems/Documentation/framework-programmer-guide.md
- Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/CoreSystem/ArtifactSystem/ArtifactEditingService.cs
- Assets/ProjectL/Script/Systems/RuntimeSystem/LifeCycleSystem/LifeCycles/Artifact/ArtifactEditScheduler.cs
- Assets/ProjectL/Script/Systems/RuntimeSystem/Common/RuntimeFaultBoundary.cs
- Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/InGameViewSystemFactory.cs
- Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/Boot/GameBootstrap.cs
