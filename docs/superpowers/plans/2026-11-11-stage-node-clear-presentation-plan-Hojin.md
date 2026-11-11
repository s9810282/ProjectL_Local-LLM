# 스테이지 노드 앞 이동·파괴 후 작은 점프 작업 계획

> **For agentic workers:** 승인된 Phase를 `superpowers:executing-plans`로 순서대로 구현한다. 사용자 지시를 우선하며 Phase마다 보고하고 멈춘다. 새 시험 코드·자동 커밋·push는 계획에 없다.

**Goal:** 모든 실제 노드에서 건물 앞까지 이동하고, 클리어 및 파괴 효과 종료 뒤 노드 중심으로 작게 점프한다.

**Architecture:** 기존 EffectSystem의 DB/Scope/Pool 경로를 사용한다. 이동, 파괴 자원 수명, 완료 순서, 조립을 분리하며 Buffer는 읽기만 한다. 전환 목적지는 변경하지 않는다.

**Tech Stack:** Unity 6000.0.73f1, C#, 기존 ChessPieceMotion·Delay·EffectScope·StageMap View.

**Spec:** [설계 원본](../specs/2026-11-11-stage-node-clear-presentation-design-Hojin.md).

**상태:** 2026-11-11 A~D 에셋·코드 반영, E 정적 통합 점검·인계 문서 정리 완료. **사용자 Unity 확인: 컴파일과 기본 완료 연출 정상.** 효과 8종 전체·반복/취소/pause/전환·DataBaseAssetTests의 세부 결과는 미확인으로 남긴다. 전체 Gate 인수 완료와 구분한다. 기준 HEAD `5fcbc0e08`, 파괴 원본 `f3406ea`.

## Global Constraints

- 설계 NC1~NC6를 적용한다. 모든 실제 노드 대상, 시작 장식 제외. 로비/다음 챕터 전환을 유지하며 이전 맵으로 강제 복귀하지 않는다.
- Core·FieldCamera·Architecture는 수정하지 않는다. EffectSystem/EffectScope/ParticleStopNotifier 공용 코드 변경은 이 계획에 포함하지 않는다.
- 원본 파괴 에셋은 보존하고 풀용 Variant로 적응한다. 기존 Missing Script 에셋을 삭제하지 않는다.
- View Test 코드를 작성하지 않는다. 기존 시험은 컴파일 호환에 필요한 호출만 최소 수정하고, 타인 시험의 실행 의미 변경은 먼저 묻는다. 컴파일·EditMode·재생은 사용자 Unity 확인으로 인계한다. MSBuild를 실행하지 않는다.
- 새 C#·폴더·에셋은 `.meta`를 함께 다룬다. UTF-8/LF, Allman, 블록 네임스페이스, 기본 sealed, 한국어 주석을 따른다.
- 커밋·push는 사용자가 한다. 각 Phase 후 보고·정지하며 다음 Phase 승인을 추정하지 않는다.
- 이 계획의 신설 API는 제안이다. 기존 호출부에 미치는 변경과 타입을 같은 Task에서 맞춘다.

## Review Focus

1. 루트 반환이 파편보다 빠르거나 첫 재생만 동작하는 경우 → A1 수명/정렬 미리보기, D2 실제 자연 반환·재생 재사용 확인.
2. 전투 복귀 로딩 속에서 연출이 소모되는 경우 → D1 준비/재생 분리 확인.
3. 팝업 닫힘의 재준비와 IsMapIdle 자기 대기로 연출이 멈추는 경우 → D1 상점·회복/보상 화면 순서 확인.
4. 새 Run에서 같은 NodeId를 재사용하거나 씬 교체 후 늦은 완료가 도착하는 경우 → C1, D2 세대/소비 기록 확인.
5. 회전된 건물·맵, 취소 복원, 반복 현재 위치 동기화에서 유닛이 중심으로 튀는 경우 → B1, B2 좌표/착지 확인.

## 0. 파일 지도와 작업 순서

아래 경로 별칭은 이 문서에서만 사용한다.

| 별칭 | 실제 경로 |
|---|---|
| V | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap` |
| C | `V/Completion` (신규 폴더) |
| VM | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/StageMap/Item` |
| VN | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/StageNode/Item` |
| RE | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/Render/Table/Effect` |
| EV | `Assets/ProjectL/Prefabs/StageMap/Effects` (신규 Variant 폴더) |
| EI | `Assets/ProjectL/DataBaseSO/RenderGroup/EffectTable/Items` |
| NI | `Assets/ProjectL/DataBaseSO/ViewGroup/StageNodeVisual/Item` |

순서: **A 효과 인수 → B 앞쪽 이동 → C 효과 어댑터·방문 기록 → D 전체 연결 → E 사용자 통합 확인/인계**. A 재생 인수 전에는 전체 파괴 종료가 검증됐다고 보고하지 않는다. 최신 사용자 Phase C 진행 지시에 따라 C의 어댑터 계약을 코드로 준비하되, Gate A 재생 인수와 D의 운영 연결/반환 검증을 완료로 간주하지 않는다. A·B·C만 완료한 것은 전체 기능 완료가 아니다.

설계의 설정값·매핑·예외 정책은 설계서가 원본이며 이 계획에서는 해당 절을 참조한다.

## Phase A — 파괴 효과를 기존 EffectSystem에 인수

### Task A1. Variant 8종과 Effect DB 등록

**Files**
- Create: `EV/<설계 §5.1의 원본 stem>_Pooled.prefab` 8종과 meta.
- Create: `EI/<설계 §5.1의 EffectType 이름>.asset` 8종과 meta.
- Modify: `RE/Enum/EffectType.cs`.
- Modify: `Assets/ProjectL/DataBaseSO/RenderGroup/EffectTable/EffectDataTable.asset`.
- Record: `docs/superpowers/handoffs/2026-11-11-stage-node-clear-effect-integration-Hojin.md` (실행 시 작성, 민형님 미전달 메모).

**Consumes:** 민형님 원본 8종, 현재 `IEffectSystemServices.GetEffectHandle` 및 `EffectScope.Play/StopAll`.

**Produces:** 설계 §5.1 키 8개로 조회 가능한 풀용 효과. 에셋 미리보기에서 루트 종료가 모든 자식 파편 종료 이후라는 기록. 실제 운영 풀 반환 인수는 D2에 남긴다.

- [x] 최신 git diff와 다른 작업자의 대상 파일 편집을 확인한다. EffectType 기존 숫자를 유지하고 새 키만 끝에 추가한다.
- [x] Variant에 루트 ParticleStopNotifier와 숨은 제어 입자 수명을 구성한다. 방출 지연·최대 생존 시간·루프 여부를 8종별 기록한다. 공용 notifier 코드 변경은 하지 않는다.
- [x] 원본 건물의 실제 바닥 보정/회전/크기에 맞게 Variant 정렬을 구성한다. 현재 운영 맵 스케일의 파일 설정을 대조하고 unsupported 스케일 조건을 명시한다. **실제 단위 받침에서 정렬 재생 확인은 아래 Unity 항목에 남는다.**
- [x] EffectDataSO 8개를 만들고 `EffectDataTable`에 등록한다. 초기 풀 0, MaxCount 1을 제안값으로 쓴다. 현재 MaxCount는 동시 실행 제한 보장이 아니므로 완료 조정자의 단일 실행 정책을 별도로 유지한다.
- [ ] 사용자 Unity 확인: 기존 프리팹/파티클 미리보기에서 8종 최초/반복 재생, 루트·자식 전체 수명, 위치·회전·크기 일치. 기존 DataBaseAssetTests 확인을 인계한다. 운영 호출자가 없는 A에서는 Scope 반환·StopAll 확인을 요구하거나 성공으로 기록하지 않는다. 새 테스트/도우미를 작성하지 않는다.
- [ ] **Gate A:** 결과를 인수 기록에 쓴다. 미실행이면 미확인, 실패하면 후속 연결을 멈추고 원인/공용 계약 필요 여부를 보고한다.

**A1 반영 기록 (2026-11-11)**

- 사용자 승인: “계획대로 Phase A부터 진행”. 기존 기능 브랜치에서 A1만 반영했으며 커밋·push하지 않았다.
- 선행 계약 대조: A의 EffectType/Variant 등록을 B의 BreakEffect 키와 C/D의 EffectScope가 소비한다. 공용 API 변경은 없고, C/D의 자연 종료 전제는 Gate A 인수 뒤 적용한다.
- 산출물: Variant 8 + EffectDataSO 8 및 meta, 신규 폴더 meta 2, enum 키 18~25, 테이블 참조 8개 추가. 기존 enum 0~17/테이블 16개는 보존했다.
- 설정: 원본 파편 56개는 지연 0 / 0초 Burst 1회 / 상수 수명 0.85초 / 비루프. 루트 제어 입자는 1초, Renderer off, Callback, Always Simulate. 자식은 Hierarchy Scaling/Local Alignment로 건물 크기·회전을 따른다.
- 정적 대조: source target fileID/GUID, 새 SO 참조, 신규 에셋 GUID 16개 중복 없음, 원본 및 공용 효과 코드 diff 없음 확인. 새 테스트·도우미·MSBuild는 실행/작성하지 않았다.
- Unity CLI의 Pipeline 패키지 미설치로 에디터 연결은 불가했다. import/컴파일/미리보기는 **미확인**이며 사용자 확인을 기다린다.
- 인수·민형님 미전달 메모: [Phase A 효과 구성 및 확인 순서](../handoffs/2026-11-11-stage-node-clear-effect-integration-Hojin.md). Gate A는 통과로 표시하지 않고 Phase B~E를 시작하지 않는다.
- 별도 읽기 전용 검수: 구체적 수정 필요 결함 없음. 신규 폴더 포함 meta GUID 18개 중복 없음 및 원본 `f3406ea` 보존 확인. 유보된 Unity import/재생/정렬은 Gate A, 운영 Scope 반환/취소/재대여는 Gate D에 남기는 것으로 판정했다. 최종 문서는 작성자가 확인한다.
- 사용자 Unity 확인 후속: `PPtr cast failed ... PrefabInstance to Prefab at FileID 100100000` 발생. Codex 생성 Variant 8개의 로컬 Instance ID가 import 생성 프리팹 핸들 ID와 충돌한 원인을 확인하고 Instance 선언/내부 참조 2줄씩 수정했다. 원본 연결의 `m_SourcePrefab.fileID=100100000`과 GUID/DB 루트 참조/효과 설정은 유지했다. 정적 ID 충돌·참조 재대조 8/8 정상. 앞선 검수에서 예약 ID 충돌을 놓쳤음을 인수 문서에 정정했다. 재import 후 오류 소멸·컴파일·재생은 여전히 미확인이다.
- 추가 재점검: override 560개의 대상 타입·전체 속성 경로, 등록 24개 고유 키/매핑, 원본 파편 56개 수명을 재대조했고 추가 결함은 발견하지 못했다. 최신 Editor.log에서 수정된 8종의 재import 완료와 그 이후 동일 PPtr/import 오류 기록 없음까지 확인했다. 이 Import 증거는 미리보기·컴파일·재생 검증과 구분한다. Gate A 재생 인수 대기 유지, 추가 에셋/C# 변경 없음.

**Phase 보고 후 정지.**

**Phase E 후속 확인:** 사용자 Unity 컴파일·기본 완료 연출 정상 답변을 인수했다. 8종 전체 미리보기·정렬·루트/자식 수명의 개별 실행 결과는 미확인으로 남긴다. 최신 확인 범위는 아래 Phase E 기록을 따른다.

## Phase B — 건물 앞 이동과 최종 자리 보존

### Task B1. 표시 설정과 바닥점 계산

**Files**
- Create: `VM/StageMarkerVisitData.cs`, `V/StageMarkerPlacement.cs`, `V/StageMarkerLanding.cs`와 meta.
- Modify: `VM/StageMapViewData.cs`, `VM/StageMapViewDataSO.cs`.
- Modify: `VN/StageNodeVisualData.cs`, `VN/StageNodeVisualDataSO.cs`.
- Modify: `Assets/ProjectL/DataBaseSO/ViewGroup/StageMapView/Item/StageMapView_Default.asset`, `NI/NodeVisual_*.asset`의 해당 필드.
- Modify: `V/StageNodeView.cs`, `V/StageMapViewScene.cs`, `V/StageMapViewDisplay.cs`.

**Interfaces**
- `StageMarkerVisitData(float frontClearance, float settleJumpHeight, float settleJumpSeconds)` / 동일 이름 PascalCase get-only 속성 / `Validate()`.
- `StageMarkerLanding { Front, Center }`.
- `StageMarkerPlacement.CalculateFront(Transform mapRoot, Vector3 centerWorld, IReadOnlyList<Vector3> buildingCornersInMap, Vector3 originWorld, float clearance) : Vector3` — 맵 로컬 바닥점 반환. 설계 §4.1 투영식을 따른다. 입력은 건물 클릭 상자의 모서리 8개를 맵 로컬로 변환한 값이며, 다시 축정렬 Bounds로 감싸지 않는다.
- `StageMapViewDisplay.GetMarkerDestination(int nodeId, StageMarkerLanding landing) : Vector3` — 맵 로컬 바닥점. 노드/건물 필수 구성이 없으면 예외.

- [x] SO → immutable Data 변환 및 §5.1의 값 검사를 추가한다. 노드 외형에는 `BreakEffect`만 추가하고 프리팹 매핑을 복제하지 않는다.
- [x] NI 외형 10개를 설계 매핑표대로 연결한다. StageKey 우선 조회는 유지한다.
- [x] 기존 건물 bounds를 사용하는 좁은 geometry 조회를 Display에 연결한다. 프레임마다 Renderer를 검색하지 않는다.
- [x] Scene이 여백 값으로 계산 부품을 만든다. 실제 출발 노드 중심은 이동 시점에 전달한다(사용자 후속 변경). 카메라 이동 소유권을 추가하지 않는다.
- [ ] 사용자 확인 항목: 실제 출발 연결선 쪽, 대각선/다른 출발 노드, 180도 회전 건물, 큰 건물 앞 여백, 시작 노드 중심. 비유한 값/길이 부족/지원 밖 스케일은 명시 오류다.

### Task B2. 두 이동 경로와 착지 보정 연결

**Files**
- Modify: `V/StageCurrentMarkerView.cs`, `V/StageMapViewDisplay.cs`, `V/StageMapViewMarkerJump.cs`.
- Modify: `V/StageMapNodeMovement.cs`, `V/StageCombatDeparture.cs`, `V/StageMapViewEntry.cs`, `V/StageMapViewEntryPreparation.cs`.
- Create: `V/StageMarkerJumpKind.cs`와 meta (`Approach`, `Settle`).

**Interfaces**
- `StageCurrentMarkerView.ShowAt(...기존 인자..., Vector3 landingOffset)` / `CompleteMove(Transform destination, Vector3 landingOffset)` — 노드 로컬 오프셋.
- `StageMapViewDisplay.BeginMarkerMove(int nodeId, Transform parent, StageMarkerLanding landing) : Transform` / `CompleteMarkerMove(int nodeId)` — 시작 때 계산한 목적지와 최종 자리를 사용.
- `StageMapViewDisplay.PlaceCurrentMarker(int nodeId, StageMarkerLanding landing) : void` — 준비 전용 명시 배치. 이동을 정리한 뒤 같은 NodeId라도 재배치한다. 매 Tick 동기화는 이 메서드를 호출하지 않는다.
- `StageMapViewMarkerJump.Begin(int nodeId, StageMarkerJumpKind kind, Func<bool> isMotionCurrent, Func<bool> checkContext)`.
- `TryArrive`, `FinishMotion`, `Cancel`의 호출 형태를 유지한다. `TryArrive`는 Approach에서는 마커+카메라, Settle에서는 마커 완료만 기다린다. `Settle`에서는 카메라 Begin/SetProgress/Commit/Cancel을 호출하지 않는다.

- [x] Begin/Complete/ShowAt/SetCurrentNode 모두 같은 landing 값을 사용하게 맞춘다. 동일 NodeId의 반복 동기화는 현재 Front 자리를 유지하고, 명시적 새 준비에서만 새 자리를 지정한다.
- [x] Approach는 앞쪽, Settle은 중심을 목적지로 사용한다. 작은 점프는 같은 노드 내 이동을 허용한다. 실제 부모의 로컬 좌표로 변환한 뒤 착지한다.
- [x] Settle의 TryArrive가 없는 카메라 operation 0을 기다리지 않도록 종류별 완료 판정을 연결한다. 작은 점프도 실제 이동 때 기존 이동 소리를 한 번 재사용한다.
- [x] 일반 이동과 출전 미리 이동을 Approach로 변경한다. 출전 카메라 확정은 수락 때라는 기존 정책을 유지한다. 취소 시 원래 노드의 자리도 복원한다.
- [x] 미클리어 현재 건물 표시를 유지한다. 시작 장식 정책과 과거 클리어 숨김은 유지하며 완료 연출의 임시 표시 지정은 D에서 연결한다.
- [ ] 사용자 Unity 확인: 전투/비전투 앞쪽 착지, 덱 취소·거절 복원, 프레임별 동기화 중 중심 스냅 없음, 카메라 앞쪽 정렬. B에서 아직 호출되지 않는 Settle의 실제 완료는 D2에서 확인한다. 기존 시험 참조 중 컴파일 호환만 최소 조정한다.

### B1/B2 실행 기록 — 2026-11-11

- 승인 범위: 사용자 요청에 따라 Phase B를 구현했다. C는 B의 컴파일/실행 의존이 있을 때만 함께 진행하는 조건이며, B에서 C 타입이나 어댑터를 참조하지 않아 조건이 성립하지 않았다. C~E는 시작하지 않는다.
- B1: MarkerVisit 설정(.4/.2/.25), 노드 외형 10개 BreakEffect 키, 클릭 상자 모서리 8개의 직접 투영과 맵 카메라 회전 주입을 반영했다(이후 아래 B 후속에서 출발 연결선 기준으로 교체). 데이터/씬 경계에서 비단위 스케일을 진단한다.
- B2: Approach/Settle 목적지·높이·시간·카메라 완료 조건을 분리했다. 표시가 Front/Center와 이동 시작 때의 목적지/노드 로컬 오프셋을 소유한다. 반복 동기화는 자리를 보존하며 새 준비는 명시 배치한다. 출전 취소는 출발 자리까지 복원한다. 미클리어 현재 건물은 유지한다.
- 조립: Scene이 계산 부품을 만들어 Display에 넘긴다. 기존 Entry의 Jump 생성 계약은 그대로이므로 StageMapViewEntry.cs 수정은 불필요했다. 서비스 등록·Core·FieldCamera·Architecture 변경은 없다.
- 구조 점검: 진행 사실은 기존 Buffer, 효과 연결은 Render DB, 이동 설정은 View DB가 각각 원본이다(SSOT). 좌표 계산/표시 소유/점프 실행/호출자 정책을 분리했다(SRP·ISP). 이동 종류 확장은 작은 enum 분기 안에 있고 파괴/완료 순서를 Jump에 넣지 않는다(OCP). 새 상속·대체 구현은 없고 기존 계약 호출부를 함께 맞췄다(LSP). 조립부에서 의존성을 전달하며 새 서비스/수명 계약은 만들지 않는다(DIP·FW). 효과·방문 기록을 읽지 않고 이동 부분만 수정·검수할 수 있다.
- 다른 작업자의 StageNodeOutlineView.cs 및 전투/Tutorial 변경은 수정하지 않았다. 새 View 시험·MSBuild·Unity 컴파일/재생은 수행하지 않았다. **컴파일 미확인.**
- 정적 검증: 생성자(target-typed new 포함)·변경 메서드 호출부를 검색/대조했다. Assets/Tests에는 변경 타입의 직접 참조가 없어 시험 파일 변경이 없었다. 외형 10개 → 등록 효과 8종 매핑과 설정 .4/.2/.25, 새 C# meta 4개의 GUID 유일성을 확인했다. 외형 에셋의 기존 필드는 변경되지 않았다. B 소유 파일의 `git diff --check` 통과, UTF-8/LF 확인. 전체 저장소의 다른 작업자 변경 검증을 뜻하지 않는다.
- 별도 읽기 전용 검수: Critical/Important 발견 없음. 문서 NC6의 과거 승인 범위 표기 1건은 최종 상태 정리에서 제거하고 계획의 승인 기록을 참조하도록 맞췄다. 미해결 minor 없음.
- 검수에서 유보한 동작: 효과 정렬·파편 수명/재사용은 Gate A와 D2, 로딩·팝업·Run별 소비 기록과 자동 파괴/작은 점프는 C/D에 남긴다. B의 실제 착지·취소·카메라 및 DataBaseAssetTests는 아래 사용자 확인 대상이다. 다른 작업자 변경은 검수 범위에서 제외했다. 정적 통과를 전체 기능 또는 사용자 인수 완료로 취급하지 않는다.

### Phase B 사용자 Unity 확인

1. Unity의 스크립트 컴파일 및 DB 생성에 오류가 없는지 확인한다. DB 에셋 변경에 대한 기존 `DataBaseAssetTests`도 사용자 Unity에서 확인한다.
2. Boot → StageSelect에서 시작 마커는 중심에 놓이는지 확인한다. 전투·상점·회복·강화·이벤트 노드는 실제로 출발한 노드와 이어진 선 쪽 건물 앞에 착지하고, 미클리어 건물은 유지되는지 확인한다.
3. 서로 다른 출발 방향·대각선 연결·180도 회전된 건물·큰 건물에서 앞쪽 방향과 여백을 확인한다. 착지 후 기다려도 매 프레임 동기화 때문에 중심으로 튀지 않아야 한다.
4. 전투 노드로 이동 후 덱 창에서 취소하거나 출전이 거절되면, 마커가 출발 노드의 원래 Front/Center로 돌아오고 카메라도 복원되는지 확인한다. 이후 다른 노드로 다시 이동할 수 있어야 한다.
5. 일반 승리 복귀와 비전투 팝업 닫기 후 재준비에서 마커가 공중/이전 노드에 남지 않는지 확인한다. 현재 B 정책은 이미 클리어된 노드는 중심, 미클리어 실제 노드는 앞이다. 로비·다음 챕터 전환은 기존대로여야 한다.
6. **B에서는 파괴 효과와 작은 점프가 자동 재생되지 않는다.** Settle의 실제 재생·효과 종료 연결은 D2 확인 항목이다. 화면 구도/여백의 최종 튜닝과 Gate A 미리보기 확인도 아직 남아 있다.

**Phase 보고 후 정지. 이 단계에는 클리어 후 작은 점프 자동 실행이 아직 없다.**

### B 후속 — 출발 연결선 기준으로 변경 (2026-11-11 사용자 승인)

사용자 선택: 카메라 쪽 대신 **실제로 출발한 노드와 이어진 선 쪽**. 기존 B1/B2 실행 기록은 변경 전 이력이며 이 절과 설계 NC2/§4.1이 최신 기준이다.

- [x] `StageMarkerPlacement.cs`: cameraRotation 대신 originWorld를 받는다. 건물 모서리 투영과 여백은 유지한다. 유한한 출발/목적지, 0이 아닌 XZ 선분, 선분 안 여백을 검사한다.
- [x] `StageMapViewScene.cs`, `StageMapViewDataSO.cs`: 계산 부품의 카메라 의존성을 제거하고 여백 Tooltip을 연결선 기준으로 바꾼다.
- [x] `StageNodeView.cs`, `StageMapViewDisplay.cs`: 이동 시작은 표시 중인 출발 NodeId, 복원은 Buffer의 유일한 직전 클리어/시작 연결 노드를 사용한다. 마커 오프셋으로 선 방향을 정하지 않는다. Start/Center 및 Settle 계약은 유지한다.
- [x] 소유 코드 정적 점검: 변경된 생성자/계산 호출, 실제 출발선과 복원선의 일치, 여러 진입선에서 평균 방향을 쓰지 않음, 카메라 참조 제거, SSOT·SOLID·FW, 인접 작업 보존. 시험·컴파일·MSBuild는 실행하지 않는다.
- [ ] 사용자 Unity 확인: 좌/우/대각선 출발, 합류 노드의 실제 출발선, 건물 회전과 무관한 방향, 덱 취소 후 같은 선 쪽 복원, 재준비/전투 복귀. **컴파일 미확인.**


- 구현 기록: 카메라 회전 입력/주입을 제거했고, 실제 출발 노드 중심과 재준비용 유일한 이전 연결선 조회를 연결했다. 기존 진행 Buffer를 읽기만 하며 새 경로 저장소·서비스·Core 변경은 없다. 공용 이동/카메라 부품 및 다른 작업자 파일은 수정하지 않았다.
- 검증: 변경 호출부 대조, 계산 부품의 카메라 참조 제거, B 후속 소유 파일 `git diff --check` 통과. 별도 읽기 전용 검수에서 Critical/Important/Minor 발견 없음. 정상 진행과 개발 Skip/Jump의 단일 전진 경로 및 복원 호출 순서를 대조했다. 실제 여백·합류 노드·취소·씬 복귀는 위 사용자 Unity 확인으로 남긴다. **컴파일 미확인.**

### B 표시 튜닝 — 건물과 착지점 간격 확대 (2026-11-11)

- 사용자 요청: 노드 바로 앞보다 조금 떨어져 착지한다.
- 운영 `StageMapView_Default.asset`의 `markerVisit.frontClearance`만 `0.4 → 0.8`로 조정했다. 출발 연결선 방향과 기존 이동/작은 점프 설정은 유지한다. C# 변경은 없다.
- 사용자 Unity 확인: Play를 다시 시작해 일반/큰 건물과 대각선 연결에서 간격을 확인한다. 컴파일 미확인, 화면 재생 미확인.

**Phase E 후속 확인:** 사용자 Unity 컴파일·기본 완료 연출 정상 답변을 인수했다. 개별 진입선·덱 취소 복원·스케일 오류의 개별 실행 결과는 미확인으로 남긴다. 최신 확인 범위는 아래 Phase E 기록을 따른다.

## Phase C — 파괴 수명과 방문 기록

### Task C1. System 수명 부품과 최소 계약

**Files**
- Create: `C/StageNodeClearKey.cs`, `C/StageNodeClearHistory.cs`, `C/StageNodeBreakStatus.cs`, `C/StageNodeBreakEffects.cs`와 meta.
- Create: `V/StageMapViewCompletionServices.cs`와 meta.
- Modify: `V/StageMapViewSystem.cs`, `V/StageMapViewServices.cs`, `V/StageMapViewEntryServices.cs`.
- Modify: `V/StageMapViewEntry.cs`, `V/StageMapNodeMovement.cs`, `V/StageCombatDeparture.cs`의 History 주입·Arm 지점.

**Interfaces**
- `StageNodeClearKey(RunId runId, int mapKey, int nodeId)` 값 형식. 동등성/해시의 필드는 이 셋만이다.
- `StageNodeClearHistory.Arm(StageNodeClearKey key) : void`, `IsPending(StageNodeClearKey key) : bool`, `TryConsume(StageNodeClearKey key) : bool`. 생성자에서 `IRunProgressBufferReadSink`를 받으며 최신 Run만 인정한다.
- `StageNodeBreakStatus { Playing, Completed, Cancelled }`.
- `StageNodeBreakEffects(IEffectSystemServices effects)` 및 `Begin(EffectType type, Transform sceneOwner, Pose pose) : long`, `GetStatus(long generation) : StageNodeBreakStatus`, `End(long generation) : void`.

- [x] History의 Arm/소비를 멱등으로 만들고 Run 변경을 반영한다. IsPending은 Arm && !Consumed이며, 소비 후 재Arm으로 재생이 되살아나지 않게 한다. 클리어 상태나 Transform 참조를 넣지 않는다.
- [x] 수락 후 착지가 확정된 진입에만 Arm한다. 출전 preview·취소·거절에 Arm하지 않는다. 전투 CompleteAccepted에서는 외부 lease 완료 게시 전에 기록을 설치하되 실패 시 오류 경계로 종료한다.
- [x] 파괴 전용 Scope 하나를 재사용하며 별도 받침·세대·Play 중 취소 지연을 구현한다. 전체 종료 인수는 A를 전제로 한다. 무효 세대는 Cancelled다.
- [x] System이 기존 Global Context로 서비스를 받아 부품들을 생성하고 좁은 묶음으로 주입한다. 낙하 EffectScope와 독립적으로 StopAll이 작동해야 한다.
- [x] 정적 확인: 동일 방문 반복/전투 씬 왕복 기록, 새 Run의 같은 NodeId, 늦은 callback 방어와 받침 해제 순서를 소스로 대조한다. C만으로 자동 파괴 재생은 시작하지 않으며, 효과 반환·취소의 실제 사용자 재생 확인은 D2에서 한다.

### C1 실행 기록 — 2026-11-11

- 최신 사용자 Phase C 진행 승인. A/B 코드는 유지하고 C만 구현한다. Gate A 미리보기와 운영 효과 재생 검증은 여전히 미확인이다. 새 View 시험·시험 도우미·MSBuild·Unity 컴파일/재생·커밋/push는 하지 않는다.
- 사전 계약 대조: B의 일반 수락 착지/출전 수락 완료가 C Arm의 입력이며, 같은 이동 문맥의 RunId/맵/NodeId를 사용한다. Phase A 키/Scope 반환 계약을 C가 소비하되 실제 전체 파편 종료 검증은 Gate A/D2에 남긴다. C의 History/Effects/좁은 서비스 묶음은 D가 소비하므로 이번 단계에서는 Begin/GetStatus/TryConsume의 자동 호출자를 추가하지 않는다.
- History: System 조립에서 한 객체를 생성해 이동과 출전/후속 완료 연출에 공유한다. Arm은 착지·수락 이후 외부 lease 완료 전에 실행한다. 반복 Arm은 소비 상태를 되돌리지 않는다. 최신 Run 변경은 기록을 비우며 이전 Run의 키로 새 기록을 만들지 않는다.
- Effects: 전용 Scope를 첫 Begin에 한 번 빌리고 재사용한다. 건물과 독립된 같은 씬의 단위 스케일 받침, 유한한 Pose, 단일 활성 세대, Playing/Completed/Cancelled, Play 중 End 유예, StopAll 후 childCount 확인과 받침 해제를 구성했다. 시작 실패는 원래 오류와 정리 오류를 함께 전달한다. D의 정상 완료도 End 뒤 작은 점프를 시작해야 한다.
- 서비스 경로: 기존 RenderSystemFactory → RenderSystemGroup.CoreGroup → Global Context → StageMapViewSystem.PostBindService에서 EffectSystem 서비스를 받는다. History/Effects는 일반 C# 부품이며 추가 등록/상위 재노출은 없다. 새 StageMapViewCompletionServices는 읽기/효과/이동 의존성만 전달한다.
- SSOT·SOLID·독립성: 클리어 사실은 Buffer만 소유하고 History는 방문/소비만 저장한다. 효과 프리팹 선택은 Render DB, NodeId별 효과 키는 기존 View DB다. 기록/효과 수명/이동 호출자/조립 책임을 분리하며 EffectScope·Core·카메라 및 다른 작업자 코드는 수정하지 않았다. 기존 대여/반환 계약을 유지하고 Scope 외부에서 풀 객체를 파괴하지 않는다(FW-011/012).
- 정적 대조: 변경 생성자 호출부 3곳을 맞췄으며 Assets/Tests의 해당 타입 직접 참조는 없었다. 새 C# 5개/폴더 1개의 meta GUID 6개가 유일하고, 새 소스의 namespace/UTF-8/LF와 소유 파일 `git diff --check`를 확인했다. 별도 읽기 전용 최종 검수에서 Critical/Important/Minor 지적 없음. 컴파일·재생 검증을 대신하지 않는다.
- 검수 유보의 처리: Gate A 파편 수명/정렬과 D2 운영 반환·연속 재생·중간 취소/씬 이탈/pause는 실제 Unity 확인 전까지 미확인으로 유지한다. 공용 EffectScope의 내부 등록 전 예외에 대한 풀 복구 보장은 이번 범위에서 확대하지 않는다. C는 원래 오류와 정리 오류를 전달하고 미반환 풀 객체를 직접 파괴하지 않는다. 다른 작업자 변경은 검수하지 않았다.
- D 연결 시 주의: Begin은 동기 재진입할 수 있으므로 반환된 세대를 받은 직후 준비 문맥을 다시 확인한다. Begin 중 완료 조정자가 취소되었으면 반환된 세대도 End로 정리해야 한다. 자연 종료는 Completed → End → 문맥 재확인 → Settle 순서를 유지한다.

### Phase C 사용자 Unity 확인

1. 새 스크립트의 Import/컴파일을 마친 뒤 Console에 새 오류가 없는지 확인한다. 추가 Inspector 연결은 없다. 순수 C# 부품은 System이 생성한다.
2. Boot → StageSelect 진입과 기존 맵 인트로/건물 낙하가 정상인지 확인한다. 파괴용 부품을 추가해도 낙하 효과와 서비스 연결이 막히지 않아야 한다.
3. 전투 노드 클릭 → 출발 연결선 쪽 착지 → 덱 취소 → 다시 출전 → 일반 승리 후 같은 맵 복귀를 확인한다. 취소/거절에서는 전투 진입이나 화면 정지가 없어야 한다.
4. 상점·회복·강화·이벤트 노드 진입/처리/팝업 닫기 및 다음 노드 이동을 확인한다. 새 Run 시작 후 같은 NodeId를 사용해도 기존 이동·씬 진입이 막히지 않아야 한다.
5. **이번 C만으로는 자동 파괴 또는 작은 점프가 재생되지 않는다.** 이번 화면 확인은 기존 이동/진입 경로의 회귀 확인이다. 방문 기록의 실제 한 번 소비, 효과 자연 반환/재사용/중간 취소·받침 정리·작은 점프까지의 관찰은 D2에 남긴다. 새 시험/임시 재생 호출자를 만들지 않았다.
6. 미완료 Gate A: 풀용 Variant 8종의 파편 정렬·수명을 Unity 미리보기에서 확인한다. C의 어댑터는 루트 반환이 모든 자식 종료 뒤라는 Phase A 계약을 사용하며, 소스 점검만으로 이 재생 인수를 완료 처리하지 않는다.

**Phase 보고 후 정지.**

**Phase E 후속 확인:** 사용자 Unity 컴파일·기본 완료 연출 정상 답변을 인수했다. 재대여·취소·Run 변경 세대의 개별 실행 결과는 미확인으로 남긴다. 최신 확인 범위는 아래 Phase E 기록을 따른다.

## Phase D — 파괴 완료와 작은 점프 연결

### Task D1. 완료 조정자와 준비/입력 경계

**Files**
- Create: `C/StageNodeClearPresentation.cs`, `C/StageNodeClearTarget.cs`, `C/StageNodeClearFrame.cs`와 meta.
- Modify: `V/StageMapViewScope.cs`, `V/StageMapViewEntryPreparation.cs`, `V/StageMapViewScene.cs`, `V/StageMapViewDisplay.cs`, `V/StageNodeView.cs`.
- Modify: `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/StageMapSceneFactory.cs`.

**Interfaces**
- `StageNodeClearTarget`: Key, BreakEffect, 실제 건물 WorldPose, MapRoot와 준비 버전. 표시 준비 이후에만 유효한 Scope 수명 값이다.
- `StageNodeClearFrame`: 현재 Key, 현재 Buffer IsCleared, 준비 버전·문맥 유효 여부, 외부 시작 허용 여부. 매 호출 입력값이며 진행 사실을 저장하는 두 번째 원본이 아니다.
- `StageNodeClearPresentation.Prepare(StageNodeClearKey current, bool isCleared, long preparationVersion) : void` — History로 대기/소비 여부를 결정. 표시 전에 호출.
- `StageNodeClearPresentation.Tick(StageNodeClearFrame frame) : void`, `Cancel() : void`, `BlocksMapInput : bool`.
- `StageMapViewDisplay`에 준비 문맥이 붙은 건물 임시 표시 지정과 대상 WorldPose 조회를 추가. 새 지정의 소유자는 완료 조정자 하나다.
- `StageMapSceneFactory.CreateCompletion(StageMapViewScene scene, StageMapViewCompletionServices services, StageMapCameraFocus focus, Func<bool> canWork, Func<StageMapMovementContext> capture, Func<StageMapMovementContext, bool> isCurrent, Action clearPointer) : StageNodeClearPresentation` — 신규 부품 생성·주입만 담당한다. 기존 필수 생성자 계약을 유지하기 위해 공유 focus를 작은 점프 부품에도 주입하되, Settle 실행에서는 Begin/Commit/Align을 호출하지 않는다.

- [x] 후보를 ShowNodes 전에 결정해 클리어 후보 건물을 보존하고 마커를 Front에 준비한다. 무기록/이미 소비한 클리어는 Center 최종 표시다. 명시 준비에서는 PlaceCurrentMarker를 사용해 같은 ID의 재준비도 재배치한다. 일반 갱신은 활성 Breaking/Settling 상태와 표시 지정을 유지한다.
- [x] 준비 중에는 배치만 하고 로딩 준비를 완료한다. 완료 재생을 로딩 Complete의 조건으로 넣지 않는다. 복귀 카메라 구도는 실제 Front 또는 Center 바닥점으로 정렬한다.
- [x] 설계 §4.3 시작 조건을 Scope의 한 함수로 계산해 주입한다. 프레임별 후보 관찰은 수행하되 자신의 BlocksMapInput/IsMapIdle에 시작이 막히지 않게 한다.
- [x] Tick에서 같은 준비/Run/맵/노드 확인 → History 소비 → 강조/클릭/건물 숨김 → 효과 Begin → 반환 세대 보관·즉시 문맥 재확인(중간 취소면 그 세대 End) → Completed 확인 → **효과 End** → 문맥 재확인 → Settle Begin → 착지 → 입력 차단 해제 순서를 연결한다. Cancelled를 완료로 소비하지 않는다. History 소비 후에도 활성 Breaking/Settling은 자기 세대가 계속 소유한다.
- [x] 숨긴 건물에 매달리지 않는 받침에 최종 WorldPose로 효과를 재생한다. 점프 목표 노드 Transform과 연결선을 보존한다.
- [x] Scope Tick을 팝업 처리 후, 선택 복귀·비전투 복원·증강 자동 열기 전에 완료 조정자가 평가되는 순서로 조정한다. 선택 화면 복귀 예약을 지우지 않고 보류한다.
- [x] 입력 차단은 기존 IsMapIdle에 합쳐 노드 클릭·호버·드래그·휠·덱/증강·개발 도구에 공통 적용한다. 일시정지/로딩 중 파괴→점프 단계 전이를 진행하지 않는다.
- [ ] 사용자 확인: 일반 전투 승리 복귀, 회복/상점 완료 닫힘과 재준비, 즉시 완료되는 노드, 뒤이어 뜨는 증강, 선택 화면 자동 복귀. 모든 노드의 완료는 Buffer로만 판단한다.

### Task D2. 재준비·취소·전환 마무리

**Files:** D1 파일 + `V/StageMapViewSystem.cs`의 Shutdown/Detach 연결, 필요 시 `V/StageMapViewEntry.cs`의 표시 복원 연결.

- [x] 준비 취소/Scope Supersede/Dispose에서 완료 부품을 먼저 무효화하고 효과·점프를 거둔 뒤 Scene을 해제한다. 중복 정리는 멱등으로 한다.
- [x] 소비 전 대기는 유지, 소비 후 중단은 재생하지 않고 다음 준비에서 최종 표시로 복원한다. 완료 이벤트/전투 lease를 취소 경로에서 게시하지 않는다.
- [x] NC5의 로비·다음 챕터 전환에서 이전 맵의 후보를 재생하지 않음을 확인한다. 기존 LifeCycle/Core 정책은 수정하지 않는다.
- [x] 예외는 RuntimeFaultBoundary에 전달한다. 필수 데이터/대상 유실을 성공·완료로 바꾸지 않는다.
- [ ] 사용자 확인: 파괴 중/작은 점프 중 맵 재준비·씬 이탈·새 Run, pause, 반복 완료 관찰, 이미 클리어된 개발 시작, 지원 밖 맵 스케일 오류. 시작점/미클리어 복원에서 불필요한 파괴가 없어야 한다.
- [ ] **Gate D:** 운영 완료 경로에서 전체 파편이 끝난 뒤 풀 반환됨 → End로 받침/세대 정리됨 → 카메라 없는 작은 점프 완료됨을 확인한다. 연속된 두 노드의 완료, 중간 StopAll, pause/재개, 다른 효과와의 간섭 없음까지 확인한다. A의 미리보기 증거와 분리해 기록하고 미실행이면 전체 인수 완료로 보고하지 않는다.

### D1/D2 실행 기록 — 2026-11-11

- 승인: 사용자 “Phase D 진행 … 파괴 이펙트가 끝난 뒤 작은 점프까지 연결”. D1/D2 코드만 구현하고 Phase E 인수 완료로 확대하지 않았다.
- 기존 튜닝 보존: D 최종 확인 시 운영 에셋 FrontClearance는 `1`, SettleJumpHeight/Seconds는 `0.2/0.25`다. D에서는 에셋 값을 변경하지 않았다. 앞선 B 후속의 0.8 기록은 당시 값이다.
- 완료 부품: `StageNodeClearPresentation`과 호출별 `StageNodeClearFrame`, 표시에서 빌리는 `StageNodeClearTarget` 및 meta 3개. ShowNodes 전 미소비 후보의 건물을 유지하고 Front에 배치한다. 후보는 기존 인트로 낙하 수집에서 제외한다. 무기록/소비한 클리어는 기본 숨김·Center를 사용한다.
- 운영 순서: History 소비 → 포인터/outline 정리 → 클릭 영역·건물 숨김 → Effect Begin → 반환 세대 보관 및 문맥 재확인 → 자연 반환 Completed → **End** → 문맥 재확인 → Settle → 착지 표시 확정 → 잠금 해제. 파편 수명을 코드 타이머로 복제하지 않았다. 활성 단계는 History가 소비됐다는 이유로 중단하지 않는다.
- 호출 순서: 팝업 Tick 다음에 평가하고 비전투 복원·증강 자동 열기·선택 화면 복귀보다 앞에 둔다. 시작 조건은 `CanPresentCompletion` 한 곳이며 자기 잠금/IsMapIdle에 의존하지 않는다. 미클리어 방문은 팝업을 막지 않는다. 선택 복귀 예약은 차단 중 유지한다.
- 취소: 준비 버전/연출 세대 무효화 → Effect End → 작은 점프와 표시 이동 정리 → 임시 표시 해제. Scope Supersede/Dispose, System StopInput/Detach/Shutdown은 기존 Preparation.Cancel 경로로 이 순서에 도달한다. 취소는 Core 결과/전투 lease 완료를 게시하지 않는다. Begin 반환 전에 취소되면 반환된 세대도 즉시 End한다.
- API 구체화: 제안 Prepare/Frame에 준비 버전을 명시하고 Factory에 포인터 정리 Action을 추가했다. 기존 IStageMapSceneFactory에 생성 계약을 연결했으며 Factory 구현/Scope 생성 호출은 각각 한 곳이다. 기존 Scene/System 이탈 경로를 재사용하므로 이 두 파일에는 D용 추가 변경이 필요하지 않았다. 설계의 동작/영역 경계는 유지한다.
- 구조 검수: 클리어/현재 노드는 Buffer, 방문 소비는 History, 외형별 EffectType은 View DB, 실제 풀 프리팹은 Render DB가 원본이다. 순서(Presentation), 효과 수명(Effects), 동작(Jump), 표시(Display/NodeView), 조립(Factory)을 분리했다. 효과 매핑 변경은 데이터, 점프 튜닝은 MarkerVisit, 순서 변경은 Completion에 머문다. 공통 회귀 경계는 Scope 입력/취소와 Display 표시/이동이다(SRP/OCP/ISP/DIP·독립성). 취소를 완료로 게시하지 않으며 기존 계약의 의미를 유지한다(LSP).
- FW/서비스: RenderSystemFactory 생성 → EffectSystem 내부 Builder 등록 → RenderSystemGroup.CoreGroup 공개 → StageMapViewSystem.PostBindService의 Global Render Context 조회 → CompletionServices/Factory 주입을 재사용한다. 루트 재등록·전역 검색·신규 서비스 등록 없이 EffectScope로만 대여/반환한다(FW-001~004/011~012). 새 UI를 만들지 않고 기존 UISystem 팝업/선택 복귀를 보류한다.
- 정적 확인: D 대상 C# 9개 UTF-8/LF·공백, 새 meta GUID 3개(Assets meta 7,952개와 대조) 중복 없음, 생성자/메서드 호출부, 종료 경로, 운영 효과 8개의 게임 시간 설정을 확인했다. 대상 `git diff --check` 통과. 최초 확인에서 기존 CRLF가 공백 오류로 표시되어 D 수정 파일만 LF로 정규화한 뒤 다시 통과했다.
- 다른 작업자의 outline/전투/Tutorial 및 Core·FieldCamera·Architecture·공용 EffectSystem 코드는 변경하지 않았다. 새 시험/도우미·MSBuild·Unity 컴파일/재생을 수행하지 않았다. **컴파일 미확인.** 커밋·push·외부 발송 없음.
- 별도 읽기 전용 최종 검수: Phase D의 신규 파일·변경 호출부·공용 효과 반환 경로를 독립 문맥에서 확인했고 Critical/Important/Minor 수정 요구는 없었다. 판단 유보 항목은 작성자가 다음과 같이 정리했다: 실제 파편/정렬/재대여/pause/카메라/착지는 기존 Gate A/B/D의 사용자 Unity 확인으로 유지(오류가 있으면 통합 인수 불가), 다른 작업자의 outline/Combat/Tutorial 자체 변경은 이번 수정/인수 범위에서 제외, 진행 중이던 문서 상태·체크박스는 이 실행 기록과 확인 목록으로 갱신 완료. 정적 검수 통과를 런타임 통과로 바꾸지 않는다.

### Phase D 사용자 Unity 확인 순서

1. **Import/컴파일:** Unity import 완료 후 Console에 새 오류가 없는지 확인한다. 이번 C# 3개가 인식되고, 기존 8종 Variant의 PPtr/Missing Script 오류가 없어야 한다. A/B의 DB 변경 검증은 기존 `DataBaseAssetTests`에서 확인한다.
2. **일반 전투 승리:** 클릭 → 실제 출발 연결선 쪽 건물 앞(현재 에셋 여백 1) 착지 → 전투 → 같은 맵 복귀. 로딩이 닫히기 전에 파괴가 소모되지 않고, 복귀 직후 건물과 앞쪽 유닛이 유지돼야 한다.
3. **완료 순서/재사용:** 모든 파편 종료·풀 반환 이후 높이 0.2/시간 0.25초의 작은 점프로 노드 중심에 착지해야 한다. 카메라는 작은 점프를 따라 움직이지 않는다. `StageNodeBreakAnchor`가 정리되고, 다음 노드에서도 효과가 다시 정상 재생돼야 한다. 효과 8종/외형 10개를 나눠 확인한다.
4. **상점·회복·강화·이벤트:** 미완료 팝업이 정상으로 열리고, 완료가 Buffer에 반영된 뒤 팝업을 닫아야 파괴한다. 닫힘으로 맵을 다시 준비해도 건물이 먼저 사라지거나 이미 파괴한 건물이 다시 나타나지 않아야 한다. 뒤이어 남은 증강/선택 화면이 정상으로 열린다.
5. **입력/중복:** 파괴 대기·재생·작은 점프 동안 다른 노드 클릭, 호버, 맵 끌기/휠, 덱/개발 도구가 막히고 착지 후 다시 가능해야 한다. 같은 노드의 반복 갱신은 효과/점프를 중복 재생하지 않는다. 다른 낙하/전투 효과와 풀 수명이 섞이지 않아야 한다.
6. **중단/정지:** 파괴 중과 작은 점프 중 각각 설정 pause/재개, 기존 개발 경로를 통한 맵 재준비, 씬 이탈을 확인한다. 취소 후 받침/효과/Delay가 남거나 옛 콜백이 새 유닛을 움직이면 안 된다. 파괴 시작 전 취소는 복귀 후 재생, 시작 후 취소는 재복귀 시 숨김·중심으로 복원돼야 한다.
7. **진행 경계:** 새 Run의 같은 NodeId는 새 방문으로 재생하며, 방문 기록 없이 이미 클리어된 개발 시작은 과거 연출을 재생하지 않는다. 시작 장식은 파괴하지 않는다. 로비/다음 챕터로 가는 승패·보스 종료는 기존 목적지를 유지한다. 지원 밖 맵/부모 스케일은 명시 오류로 멈춰야 한다.

위 항목은 **실행 미확인 체크리스트**다. 전체 파편 종료/실제 풀 반환/착지 성공을 코드 검수만으로 Gate D 통과라고 기록하지 않는다.

**Phase 보고 후 정지.**

**Phase E 후속 확인:** 사용자 Unity 컴파일·기본 완료 연출 정상 답변을 인수했다. 노드 종류별·반복·취소·pause·전환의 개별 실행 결과는 미확인으로 남긴다. 최신 확인 범위는 아래 Phase E 기록을 따른다.

## Phase E — 통합 확인과 문서 인계

- [x] A~D 사용자 확인 결과를 각 Phase에 기록하고 확인하지 않은 세부 항목은 미확인으로 남긴다.
- [x] 효과 8종/외형 10개 매핑, 프리팹 루트 설정 계약, 중복 EffectType을 현재 파일로 재대조한다.
- [ ] 기존 DataBaseAssetTests와 Gate A/B/C/D 세부 Unity 실행 결과를 취합한다. 컴파일·기본 연출 확인만으로 대체하지 않는다.
- [x] 데이터·이펙트·이동·완료 순서의 SSOT/SOLID/FW/독립성을 설계 §8과 대조한다.
- [x] 대상 `git diff --check`, 변경 파일 목록, 변경 API 호출부와 참조를 읽기 전용으로 확인한다.
- [x] 민형님 공유 메모에 원본/루트 종료 계약/enum·Factory 변경/공용 수정 여부와 최신 확인 범위를 기록한다. **미전달** 상태를 유지한다.
- [x] 코드·에셋 반영, 정적 확인, 사용자 Unity 확인, 남은 인수 항목을 구분해 문서와 보고를 정리한다.

### E 실행 기록 — 2026-11-11

사용자 승인: “그러면 Phase E 진행해주세요.” E에서는 기존 A~D의 점검과 문서 인계만 진행했으며 게임 코드·에셋을 추가 수정하지 않았다. 커밋·push·외부 메시지 발송은 하지 않았다.

**사용자 확인 원문:** “컴파일과 기본 완료 연출 정상 확인”. 이 결과는 사용자 Unity 실행 확인이며 Codex가 직접 컴파일·재생한 결과가 아니다. 확인 노드 종류, 반복 횟수, 취소·pause·전환 시나리오는 제공되지 않았다. 과거 A~D 기록의 “컴파일 미확인”은 각 기록 당시 상태이며, 현재 컴파일 상태는 이 답변으로 갱신한다.

| 항목 | 현재 인수 상태 | 증거와 남은 범위 |
|---|---|---|
| Unity 컴파일 | **사용자 정상 확인** | 위 답변. Codex 빌드 실행 없음. |
| 기본 완료 연출 | **사용자 정상 확인** | 기본 흐름의 정상 확인을 인수. 8종/모든 노드별 성공으로 확대하지 않음. |
| 효과 8종·외형 10개·테이블 24개 | **현재 파일 정적 대조 통과** | enum 18~25, 테이블 키 24개 중복 없음, SO→Variant→원본 GUID 해석 및 외형별 효과 매핑 10/10 확인. |
| Prefab 루트 종료 계약 | **설정 대조 통과 / 전체 재생 인수 대기** | Variant override 560개 구성, 루트 수명/게임 시간/Callback과 자식 56개의 비루프·0.85초 수명, notifier 연결, 예약 fileID 충돌 없음 확인. 실제 파편 전체 종료/반복/정렬은 별도 확인 필요. |
| 위치·튜닝 | **현재 코드·에셋 대조** | 효과는 실제 건물의 보정된 월드 위치·회전, 받침 스케일 1. 앞쪽 여백 1, 작은 점프 높이 0.2/시간 0.25초. 개별 건물 파편 정렬은 미확인. |
| 반복·취소·pause·새 Run·전환 | **세부 결과 미확인** | 기본 완료 성공과 별개. D 사용자 확인 5~7번 및 C 취소 계약 확인 필요. |
| 기존 DataBaseAssetTests | **사용자 실행 결과 미확인** | 에셋 교차 검증은 사용자 Unity에서 확인. 새 시험/도우미 없음. |

**정적 통합 점검 범위**

- A~D 관련 현재 변경 파일 91개(C# 32, SO 20, Variant 8, meta 31)를 분리해 `git diff --check` 통과 및 필요한 meta 존재를 확인했다. 다른 작업자의 `StageNodeOutlineView`, Combat/Tutorial, FMOD, InGame 맵 설정, 기존 스테이지 맵 계획 변경은 이 기능 변경 목록에 섞지 않았다.
- 변경 API의 운영 호출부를 대조했다. 접근 호출자 2개는 Approach, 완료 조정자는 Settle을 사용하며 Factory/Scope/Preparation의 새 생성 인수 연결을 확인했다. 기존 시험에서 이번 변경 API의 직접 참조는 검색되지 않았다. 이 정적 검색을 컴파일 증거로 사용하지 않는다.
- 후보 준비는 ShowNodes 앞, 완료 Tick은 팝업 뒤·선택 복귀/비전투 복원/증강 앞이다. 자기 잠금 없이 계산하는 시작 조건, History 소비 후 활성 단계 유지, Completed→End→문맥 재확인→Settle 순서와 취소 세대 검사를 현재 코드로 대조했다.
- 원본 파괴 프리팹 폴더와 공용 EffectSystem 폴더는 HEAD 대비 diff가 없다. 기존 A의 enum 키 추가, B의 View DB, C의 System 서비스 연결, D의 Factory 생성 계약 추가는 공유 메모에 남겼다.
- E 확인 시작 이후 대상 91개 파일의 SHA-256이 동일해 게임 코드·에셋·meta를 추가 변경하지 않았음을 대조했다. 대상 meta 31개의 GUID 중복 없음, 문서 3개의 UTF-8/LF·공백 및 로컬 링크 10개 존재를 확인했다.

**문서 최종 검수**

- 독립 읽기 전용 검수에서 최신 사용자 확인과 모순되는 설계 §9.3의 무조건적인 “컴파일 미확인 유지” 지침 1건을 발견했다. 검수자는 Minor로 분류했지만 작성자는 이후 인수 보고를 과거 상태로 되돌릴 수 있는 현재형 규칙 충돌로 판단해 문서 정확성상 필요한 수정으로 수용했다. “사용자 확인 전까지”라는 조건과 최신 Phase E 상태 참조를 넣어 바로잡았다. 게임 동작/승인 범위를 바꾼 결정은 아니다.
- 판단 유보는 실제 세부 재생, 런타임 구현 재검수, DataBaseAssetTests다. 작성자는 실제 세부 재생/DB 결과를 아래 미확인 인수 목록에 유지하고, 런타임 구현은 기존 D 검수와 이번 호출부·서비스 경계 대조를 근거로 하되 E 문서 검수만으로 새 실행 성공을 주장하지 않는다. 남은 문서 수정 지적은 없다.

**구조 점검**

- SSOT: 진행 사실은 Buffer, 방문/소비는 History, 노드별 선택 키는 View DB, 프리팹은 Render DB, 실제 마커는 Display/MarkerView가 소유한다. 실행 단계와 임시 표시 지정은 표현 수명이며 클리어 사실을 복제하지 않는다.
- SOLID: 효과 반환·점프·완료 순서·표시·조립 책임을 분리(S), 효과 선택/튜닝은 데이터로 변경(O), 취소를 정상 완료나 전투 lease 완료로 게시하지 않음(L), 읽기 싱크/전용 서비스 묶음으로 전달(I), 기존 인터페이스와 Factory 주입 경로 사용(D).
- FW/서비스 경로: RenderSystemFactory가 EffectSystem 생성 → Render 내부 Builder에 IEffectSystemServices/AddModule 등록 → RenderCoreGroup 보관/RenderSystemGroup이 IRenderServiceContext 공개 → GlobalSystemGroup은 IGlobalSystemGroupService만 공개 → StageMapViewSystem이 Global.RenderServiceCommand.CoreGroup에서 조회 → CompletionServices/Factory 주입. 하위 서비스를 루트로 재등록하지 않는다. 자원 반환은 EffectScope를 통한다(FW-001~004, 011~012).
- UI는 새 등록 단위 없이 기존 UISystem의 팝업/선택 복귀를 사용한다. 조립 변경은 Factory, 순서 변경은 Completion, 튜닝은 View DB에 머문다. 남는 공유 검증 경계는 Display의 자리/건물 표시와 Scope의 입력/취소다.

**남은 통합 인수**

1. 효과 8종/외형 10개 최초·반복 재생, 원래 건물과 파편 위치/바닥/180도 회전/크기 정렬.
2. 일반 전투 복귀 로딩과 상점·회복·강화·이벤트 완료 팝업 닫힘, 뒤이어 열리는 증강/선택 화면.
3. 두 노드 연속 완료·입력 재개·중복 재생 없음, 파괴 중/작은 점프 중 pause 및 취소·재준비·씬 이탈.
4. 새 Run/과거 클리어 개발 시작, 보스/로비/다음 챕터 기존 전환, 지원 밖 스케일 오류.
5. 기존 DataBaseAssetTests 결과.

상세 절차는 위 **Phase D 사용자 Unity 확인 순서**를 따른다. **E의 정적 점검·문서 정리는 완료했으며, 세부 Gate를 포함한 전체 통합 인수는 대기**한다.

## 설계 대비 확인표

| 설계 요구 | Task |
|---|---|
| NC1·NC2 앞쪽 목적지, 착지 스냅 방지 | B1·B2 |
| NC3 파괴 실제 종료 후 작은 점프 | A1·C1·D1 |
| NC4 모든 노드 및 팝업 완료 | B1 매핑·C1 Arm·D1 |
| NC5 기존 전환 유지 | D2 |
| §4.2 중복 재생/재시작/새 Run | C1·D2 |
| §4.3 로딩 교착/입력·팝업 순서 | D1 |
| §5 Effect DB/Scope/완료/정렬 | A1·C1 |
| §6 조립/§7 취소 수명 | C1·D1·D2 |
| §8 원칙·독립성 | 각 Task 및 E |

## 기록

- 2026-11-11: 사용자 NC4(모든 노드 적용), NC5(기존 전환 유지) 답변 반영. 문서만 작성. Phase A~E 미시작.
- 같은 날: 별도 builder check의 공유 focus 생성 의존성 누락 지적을 수용해 D1 서명을 보완했다. 자체 대조로 요구-Task 매핑, 문서 링크, 신규 파일 형식 및 미구현 상태 표시를 확인했다. 문서 승인과 Phase별 구현 승인은 아직 받지 않았다.
- 같은 날 사용자 요청 재점검: B2의 Settle 완료 판정/동일 노드 명시 배치, C1의 재Arm 멱등성, D1의 정상 효과 End 및 활성 상태 보존을 추가했다. B1의 앞쪽 좌표 입력을 설계와 같은 모서리 8개로 통일했다. A는 에셋 미리보기, D2는 운영 Scope 반환/취소 인수로 구분해 아직 없는 호출자로 검증하도록 요구하던 순서 공백을 해소했다. 구현은 시작하지 않았다.
