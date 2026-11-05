# View 기능별 폴더 정리 — 설계 및 Task 계획

작성: 2026-10-10. 상태: Phase 0(조사·설계·이동 목록) 완료, 사용자 확인 전. 코드 이동·네임스페이스 변경은 아직 하지 않았다.

## 1. 요청과 범위

사용자 요청은 `InGameSystem/ViewSystem`의 큰 구간별 묶음을 기능별 폴더로 정리하는 것이다. 이번 변경은 **파일 배치와 이에 따른 네임스페이스·참조 정리**다. 앞서 논의한 Buffer-only 통신 및 Job/Plan 재설계와는 별개의 작업이다. 폴더 정리 완료를 통신 경계 리팩터링 완료로 취급하지 않는다.

대상은 현재 ViewSystem C# 109개다. 그중 101개를 이동하고 그룹·시스템 조정자·Boot/Dev 8개는 현재 위치를 유지한다. 이동 전에 확인한 각 .cs.meta GUID를 [이동 목록](2026-10-10-view-feature-folder-move-manifest.json)에 기록했다. 이 목록은 예정 경로이며 실제 이동 완료 기록이 아니다.

`InGameSystemGroup`·`InGameViewSystemGroup`·Factory의 서비스 등록 구성과 수명 순서는 유지한다. 기능 폴더를 만들기 위해 새로운 SystemGroup, 서비스, asmdef, 중계 객체를 만들지 않는다. Core 로직, FieldCamera, Architecture, Global Render/UI 구현, 씬·프리팹·DB 값은 변경하지 않는다.

## 2. 설계

기능을 찾는 사람이 입력/표시라는 큰 기술 계층을 모두 뒤지지 않도록, 기능 전용 입력 객체·표시 객체·인터페이스를 같은 기능 폴더에 둔다. 여러 기능에 실제로 사용되는 것은 공통 입력 또는 공용 재사용 폴더에 둔다. 동작을 새로 분리하거나 합치는 작업은 하지 않는다.

```text
ViewSystem/
├─ InGameViewSystemGroup.cs             그룹 조립·수명 유지
├─ Boot/                               시작·에디터 설정 유지
├─ Dev/                                개발 fixture 유지
├─ Combat/
│  ├─ CombatViewSystem.cs               조정자 위치 유지
│  ├─ ICombatViewService.cs             그룹 공개 계약 위치 유지
│  ├─ Input/                           Receiver·Router·Policy·Gate·Controller·집기·공통 Interaction
│  ├─ Placement/                       PlacementDragInteraction·PlacementGhost
│  ├─ Attack/                          AttackTargetingInteraction
│  ├─ Movement/                        경로 입력·초안·포인터 샘플·이동 재생·전용 계약
│  ├─ Field/                           FieldView·강조 상태·좌표·표시기·데칼·배경 프랍
│  ├─ Units/                           FieldUnitPresenter·FieldUnitView
│  ├─ Hand/                            HandPresenter
│  ├─ Hud/                             CombatHud
│  ├─ Playback/                        결과 관측·재생·사건 표시·Presentation 계약
│  ├─ Scene/                           SceneRoot·SceneDisplay·DisplayValues·집합 표시 계약
│  ├─ Abandon/                         포기 Controller·Panel·표시 계약
│  ├─ Development/                     개발 Layer Controller·표시 계약
│  └─ Common/                          손패/필드가 함께 사용하는 EntityViewSet
├─ StageMap/
│  ├─ StageMapViewSystem.cs             조정자 위치 유지
│  ├─ Input/                           기존 StageSelectionInput의 Controller·슬롯·이름·계약
│  ├─ Interaction/                     노드 선택·로그·피드백 계약
│  ├─ Entry/                           StageMapEntryFlow
│  ├─ Reading/                         StageMapViewReader
│  ├─ Picking/                         StageNodePicker·집기 계약
│  └─ Display/                         SceneRoot·Display·NodeView·LinkView
└─ ArtifactPopup/
   ├─ ArtifactViewSystem.cs             조정자 위치 유지
   ├─ Input/                           기존 ArtifactInput의 Controller·슬롯·이름·계약
   ├─ Interaction/                     ArtifactInteraction
   ├─ Board/                           논리 좌표 변환·배경 배치
   └─ Display/                         팝업·드래그·아이템·상태 문구·SceneRoot·화면 계약
```

`Combat/Display`, `StageInput`, `StageSelectionInput`, `ArtifactInput`은 이동 후 소스 파일을 보관하지 않는다. 새 경로의 단일 파일이 원본이며 옛 네임스페이스를 위한 중복 클래스·별칭 타입은 만들지 않는다. 기존 폴더 메타 삭제는 최종 Phase에서 빈 폴더와 정확한 경로를 확인한 뒤 별도로 보고한다.

### 원칙 점검

- **SSOT:** 파일과 타입을 복제하지 않는다. 기능별 실제 구현은 한 곳에 둔다. 런타임 상태·Buffer 소유권은 바꾸지 않는다.
- **SOLID:** 파일 위치를 정리할 뿐 책임과 계약은 유지한다. 현재 Core 직접 조회 등 미해결 경계를 폴더 이동으로 해결됐다고 기록하지 않는다.
- **FW:** 서비스/모듈 등록, Builder Build 1회, 입력 바인딩, 씬 Attach/Detach, 대여 자원 반환 순서는 유지한다. 공용 Framework 계약을 새로 만들지 않는다.
- **네임스페이스:** `ProjectL.<Script 아래 실제 폴더 경로>`로 함께 바꾼다. 폴더만 바꾸고 옛 네임스페이스를 남기지 않는다.
- **Unity:** .cs와 .meta는 한 쌍으로 이동하며 GUID를 유지한다. 네임스페이스가 바뀌는 MonoBehaviour에 기존 네임스페이스·어셈블리·클래스명 기준 `MovedFrom`을 추가한다. 클래스명·어셈블리·직렬화 필드명은 유지한다.
- **사용자 변경 보존:** 이동 대상 `FieldOutlineRenderer.cs`에 기존 사용자 변경 2행이 있다. 그 내용을 보존하며 코드 형식 정리를 섞지 않는다. FMOD 에셋·ExcelDataBase.meta 등 작업 범위 밖 변경도 그대로 둔다.

## 3. Task / Phase

### Phase 0 — 조사·설계·이동 목록 (완료, 리뷰 지점)

- [x] 상위 AGENTS.md, Assets/AGENTS.md, 폴더/네임스페이스 규칙 확인.
- [x] ViewSystem 109개 소스와 .meta GUID 확인.
- [x] 기능별 예정 경로·네임스페이스·Phase를 manifest에 기록.
- [x] 참조 영향 확인: 현재 검색 기준 View 소스 106개, 테스트 39개, Factory 2개, 그 밖 시스템 소스 4개가 관련 네임스페이스를 사용한다. 이는 검색 후보 수이고 모두 수정해야 한다는 확정 수는 아니다.
- [x] CoreSystem 내부에서는 해당 View 네임스페이스 참조를 발견하지 못함.
- [ ] 사용자에게 폴더 구조와 단계 범위 확인 받기.

### Phase 1 — Combat / 전투 입력 68개 이동 (승인 후)

- [ ] 이동 직전 작업 트리와 현재 경로를 재확인한다. manifest 이후 코드/파일 변경은 먼저 대조한다.
- [ ] Combat 소스와 StageInput의 전투 입력·이동 초안 소스를 기능 폴더로 .meta와 함께 이동한다. 절대 경로가 ViewSystem 안에 있는지 확인하고 이동 대상 덮어쓰기는 금지한다.
- [ ] 선언 네임스페이스, 내부 using, Factory·ServiceKeys·LifeCycle·테스트 등 실제 소비자의 using/정규화 타입명/리플렉션 문자열을 필요한 범위만 갱신한다.
- [ ] MonoBehaviour MovedFrom 추가. 새 폴더 .meta 준비. 파일별 GUID 일치와 사용자 변경 보존 확인.
- [ ] 외부 참조 갱신 과정에서 보호 영역 파일 변경이 필요하면 중단·보고한다. Core 로직은 수정하지 않는다.
- [ ] 컴파일 및 Combat 관련 테스트를 수행한다. Unity 확인이 불가능하면 수행한 검사와 남은 Unity 검증을 분리한다.
- [ ] **Phase 1 끝에서 멈추고 이동·검증 결과 보고. 다음 Phase 자동 진행 금지.**

### Phase 2 — StageMap / ArtifactPopup 33개 이동 (별도 진행 요청 후)

- [ ] StageSelectionInput 5개를 StageMap/Input으로, ArtifactInput 5개를 ArtifactPopup/Input으로 이동한다.
- [ ] StageMap·ArtifactPopup 내부 소스를 기능별로 이동한다. 상세 경로는 manifest의 phase 2를 따른다.
- [ ] 네임스페이스·참조·MovedFrom·GUID를 Phase 1과 같은 기준으로 검증한다.
- [ ] StageMap·Artifact 팝업·입력·Boot 스모크 테스트와 런타임 조립 테스트를 수행한다.
- [ ] **Phase 2 끝에서 멈추고 보고.**

### Phase 3 — 최종 검증·탐색 문서 정리 (별도 진행 요청 후)

- [ ] README와 현재 UML의 경로/네임스페이스 스냅샷을 실제 코드로 갱신한다. 과거 실행 기록을 일괄 치환하지 않는다.
- [ ] 전체 GUID 중복·유실, 옛 경로/네임스페이스의 실행 코드 잔존, 직렬화 타입 참조를 확인한다.
- [ ] 기존 폴더가 정말 비었는지 확인한다. 다른 파일이 있으면 삭제하지 않는다. 불필요한 빈 폴더 메타 정리는 정확한 대상과 복구 가능성을 보고한다.
- [ ] Unity 컴파일·EditMode, Combat/StageSelect 씬의 Missing Script 및 연결 상태를 확인한다. Unity 미실행이면 완료 검증과 분리해 남긴다.
- [ ] 이동 목록에 실제 완료/검증 상태를 추가한다. 커밋·push는 하지 않는다.

## 4. 이번 작업으로 바뀌지 않는 것

Buffer-only 경계 위반, Scheduler의 직접 Job 등록, 타임라인 예측 게시 계약, 진입 요청 객체, Plan 실행 구성은 현행 그대로다. 이 작업은 그 후속 리팩터링을 하기 전에 담당 기능 파일을 쉽게 찾도록 정리하는 단계다. 런타임 동작 변경이 필요하다고 판단되면 이 계획 안에서 구현하지 말고 별도 설계로 사용자에게 먼저 묻는다.
