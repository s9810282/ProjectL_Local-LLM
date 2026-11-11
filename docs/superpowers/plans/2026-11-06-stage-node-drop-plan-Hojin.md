# Stage Node Drop Implementation Plan — 2026-11-06

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking. 프로젝트 사용자 규칙이 스킬의 테스트·커밋 절차보다 우선한다.

**Goal:** 각 Run에서 챕터 맵을 처음 보여줄 때, 현재 플레이어에서 가까운 노드부터 본부와 같은 가속 낙하·착지 효과를 한 번 재생한다.

**Architecture:** 최초 재생 기록과 효과 대여 창구는 System 수명에, 대상 자세·낙하 진행은 씬 Scope 수명에 둔다. 기존 로딩 준비 계약으로 배치·숨김을 마친 뒤 낙하를 시작하고, 입력·팝업 허용은 같은 연출 진행 상태에서 파생한다. 생성·연결은 기존 Factory에서 맡고 낙하·효과 구현은 전용 부품으로 분리한다.

**Tech Stack:** Unity 6000.0.73f1, C#, 기존 View DB SO, Buffer 읽기, `ILodingPreparation`, Render `IEffectSystemServices`/`EffectScope`.

**Spec:** [보완된 스테이지 노드 낙하 설계](../specs/2026-11-06-stage-node-drop-design-Hojin.md). 실행자는 설계와 이 계획을 함께 읽는다.

## Global Constraints

- 2026-11-06 사용자는 이번 기능의 Codex C# 작성 예외와 계획 문서 작성을 요청했고, 계획 보완 후 Phase 1 구현 시작을 지시했다. 현재 구현/검증 상태는 문서 끝 Phase 1 실행 기록을 따른다.
- 적용 작업 루트는 `C:/Users/Hyeon2-400/Project_L`이다. 기존 작업 트리의 다른 변경을 덮어쓰거나 stash/reset/clean하지 않는다. 커밋·push는 사용자가 한다.
- View 테스트 코드·시험 도우미를 만들지 않는다. MSBuild·테스트·Unity 재생을 실행하지 않는다. 각 Task의 검증은 정적 검토와 사용자 Unity 확인으로 구분하며 보고에는 **컴파일 미확인**을 적는다.
- 기존 시험 변경은 프로젝트 규칙의 최소 호환 수정 범위만 허용한다. 다른 사람이 쓴 시험의 실행 동작을 수정해야 하면 먼저 묻는다. 이를 피하려고 운영 검증을 느슨하게 만들지 않는다.
- Core·Buffer 구현·FieldCamera·Architecture·전투 본부 구현·공용 Render/Loading 구현·공용 프리팹은 수정하지 않는다. Missing Script 에셋을 삭제하지 않는다.
- 새 서비스·UI 화면·UIId·입력 Layer를 등록하지 않는다. 기존 Global Context와 Buffer 읽기 계약만 사용하며, SO에 런타임 재생 이력을 저장하지 않는다.
- 낙하 높이 **30 월드 단위**, 낙하 시간 **0.8초**, 전체 시작 시각 차이 **0.6초**, 마지막 착지 후 효과 대기 상한 **게임 시간 3초**. 효과는 기존 `EffectType.FallingRocks`다.
- 현재/클리어 건물 숨김, 시작 장식·마커·배경·연결선의 고정 배치, 기존 카메라 드래그·휠 정책을 유지한다. 같은 챕터의 전투 복귀·재그리기에서는 반복하지 않고 새 Run에서는 다시 허용한다.
- 네임스페이스는 폴더 경로, 블록 namespace, Allman, 공백 4칸, UTF-8/LF, `sealed` 기본, 한국어 주석. 새 `.cs`와 새 폴더에는 `.meta`를 만들고 기존 GUID는 유지한다.
- 스킬의 테스트 작성·실행·커밋 단계는 위 사용자 규칙에 따라 적용하지 않는다. 계획 수행 중 설계와 다른 결정이 필요하면 구현을 멈추고 먼저 확인한다.

## Review Focus

| 위험 조건 | 기대 동작 | 담당/확인 |
|---|---|---|
| 첫 Run의 맵 Job이 늦게 완료됨 | 배치·숨김 전 Reveal 금지, 준비가 IsLoading 종료를 기다리지 않음 | ND5 / U1 |
| 전투 복귀·새 Run·이전 Scope의 늦은 정리 | 같은 챕터는 재생 생략, 새 Run은 재생, 이전 정리는 새 효과에 영향 없음 | ND3·ND4·ND5 / U2·U5 |
| 연출 중 누르고 종료 뒤 떼기, 배경 드래그, 팝업 복원 대기 | 잔여 클릭 확정 없음, 호버 억제로 드래그가 끊기지 않음, 유효한 팝업 복원 한 번, 선택 UI 예약 보존 | ND6 / U3 |
| 설정 창 열기·닫기, 낙하 시간 0 | 낙하·파티클·3초 상한이 함께 멈추며 재개 후 이어짐 | ND4·ND6 / U4 |
| 효과 종료 알림 누락·복수 효과·취소 | 받침 반환으로 자연 종료 확인, 게임 시간 상한 뒤 회수, 노드 파괴 전에 반환 | ND3·ND4 / U5 |

## 파일과 책임

경로는 아래 접두사와 파일명을 결합한 저장소 상대 경로다. 접두사는 이 계획에서만 쓰는 표기이며 실제 폴더를 새로 바꾸지 않는다.

| 접두사 | 정확한 경로 |
|---|---|
| D | `Assets/ProjectL/Script/Systems/DataBaseSystem/Data/View/Table/StageMap/Item/` |
| V | `Assets/ProjectL/Script/Systems/RuntimeSystem/InGameSystem/ViewSystem/StageMap/` |
| F | `Assets/ProjectL/Script/Systems/Factory/Runtime/InGame/View/` |
| A | `Assets/ProjectL/DataBaseSO/ViewGroup/StageMapView/Item/` |

| 종류 | 파일 | 책임/Task |
|---|---|---|
| 새 파일 | `D/StageNodeDropData.cs` | 불변 설정·값 검사 / ND1 |
| 수정 | `D/StageMapViewData.cs`, `D/StageMapViewDataSO.cs`, `A/StageMapView_Default.asset` | 설정 입력·값 전달·운영 기본값 / ND1 |
| 새 폴더/파일 | `V/Entry/StageNodeDropTarget.cs` | 노드별 원래 자세·Renderer 상태·복원 / ND2 |
| 수정 | `V/StageNodeView.cs`, `V/StageMapViewDisplay.cs`, `V/StageMapViewScene.cs` | 생성 직후 숨김·대상 수집 전달 / ND2 |
| 새 파일 | `V/Entry/StageNodeDropEffects.cs` | EffectScope 재사용·받침·완료/소유 세대 / ND3 |
| 새 파일 | `V/Entry/StageNodeDropHistory.cs`, `V/Entry/StageNodeDropPresentation.cs` | Run별 최초 기록, 씬별 낙하 진행 / ND4 |
| 새 파일 | `V/Entry/StageMapLoadingPreparation.cs` | ILodingPreparation 어댑터 / ND5 |
| 수정 | `V/StageMapViewSystem.cs`, `V/StageMapViewServices.cs` | 기존 서비스 수신·System 수명 부품 보관·준비 등록 / ND5 |
| 수정 | `V/IStageMapSceneFactory.cs`, `F/StageMapSceneFactory.cs` | 연출 및 로딩 준비 생성·주입 / ND5 |
| 수정 | `V/StageMapViewScope.cs` | 준비·재생·정리 호출 순서, 입력 허용과 팝업 복원 / ND5·ND6 |
| 수정 | `V/StageMapViewInput.cs`, `V/StageMapInteraction.cs` | 노드 입력 수신 가드 / ND6 |
| 수정 | `docs/superpowers/viewDocs/stage-map-guide-Hojin.md`, 이 계획 | 설정·확인 항목·실행 증거 / ND7 |
| 새 문서 | `docs/superpowers/handoffs/2026-11-06-stage-node-drop-framework-share-Hojin.md` | Factory/로딩 연결 공유 메모, 미전달로 기록 / ND7 |

새 C# 6개와 `Entry` 폴더의 `.meta`를 함께 다룬다. 기존 `StageMapSelectionPresentation`·팝업 Controller·카메라 구현은 계약을 읽어 연결하며 수정 대상으로 확대하지 않는다. 시작 시 호출처 검색 결과가 달라지면 해당 변경의 소유자와 범위를 먼저 확인한다.

## Phase 1 — 구현·정적 검토·사용자 인계

의존 순서: **ND1 → ND2 → ND3 → ND4 → ND5 → ND6 → ND7**. 이 한 Phase의 중간 Task는 단독 출시하지 않는다. Phase 종료 후 사용자 Unity 확인을 기다리며 다음 기능으로 진행하지 않는다.

체크 표시는 구현과 Codex의 정적 확인 완료를 뜻한다. 각 Task의 **사용자 확인** 및 U1~U6은 별도이며, 사용자 Unity 확인 전에는 완료로 보지 않는다.

### ND1: 스테이지 낙하 설정

**Files:** 새 `D/StageNodeDropData.cs`와 `.meta`; 수정 `D/StageMapViewData.cs`, `D/StageMapViewDataSO.cs`, `A/StageMapView_Default.asset`.

**Interfaces:** `StageNodeDropData(bool enabled, float height, float seconds, float waveSpreadSeconds)`; get-only `Enabled`, `Height`, `Seconds`, `WaveSpreadSeconds`; `void Validate()`. `StageMapViewData.NodeDrop`가 이 값을 제공한다.

- [x] 기존 작업 상태와 적용 지침을 확인하고, 위 Data 생성자·`GetData()` 호출처를 검색한다. 생성자 인수 추가는 `StageMapViewData`의 기존 선택 인수 `hoverStyle` 앞에 필수 `StageNodeDropData nodeDrop`으로 넣는다.
- [x] 불변 설정 클래스를 만들고 세 실수의 음수·NaN·무한대를 거절한다. `StageMapViewData.Validate()`는 필수 NodeDrop과 그 값을 검사한다.
- [x] 기존 SO의 `Input` 안에 직렬화 입력 `nodeDrop`을 추가한다. `GetData()`마다 새 `StageNodeDropData`를 만들고 운영 에셋에 `enabled: 1`, `height: 30`, `seconds: 0.8`, `waveSpreadSeconds: 0.6`을 명시한다. 별도 테이블·SO 종류는 추가하지 않는다.
- [x] 0의 의미를 맞춘다: `waveSpreadSeconds == 0`은 동시 시작, 높이 또는 낙하 시간이 0이면 각 대상의 예정 시작 시각에 즉시 착지한다. 기능 꺼짐·대상 없음은 연출/효과/기록 없이 최종 표시로 완료한다.
- [x] **정적 확인:** 설정 원본이 스테이지 SO 한 곳인지, 운영 값·검사·생성자 전달이 맞는지, 새 GUID가 고유한지 확인한다. **사용자 확인:** U6의 값 변경과 `DataBaseAssetTests`.

### ND2: 숨긴 대상 준비와 원래 자세 복원

**Files:** 새 `V/Entry/StageNodeDropTarget.cs`, `V/Entry.meta`와 새 C# `.meta`; 수정 `V/StageNodeView.cs`, `V/StageMapViewDisplay.cs`, `V/StageMapViewScene.cs`.

**Interfaces:** `StageNodeDropTarget(int nodeId, Transform visual, Pose groundPose)`; get-only `NodeId`, `Visual`, `GroundPose`, `LandingPosition`; `void Hide()`, `void Reveal()`, `void ApplyHeightOffset(Vector3 worldUp, float worldHeight)`, `void Restore()`. `StageNodeView.FinalizeDropTargets(List<StageNodeDropTarget> targets)`는 실제 대상만 남긴다.

- [x] 대상은 생성 시 원래 local 위치·회전·크기와 Renderer별 enabled를 보관한다. `ApplyHeightOffset`은 원래 월드 착지 위치에 높이를 더하고, `Reveal`은 표시만 복원하며, `Restore`는 원래 자세와 표시를 반복 가능하게 복원한다. GameObject 활성·Collider·진행 사실을 바꾸지 않는다.
- [x] 맵 키를 받는 `StageMapViewDisplay.Show`와 `StageNodeView.Show`에 선택 인수 `List<StageNodeDropTarget> dropTargets = null`을 끝에 추가한다. `StageMapViewScene.ShowNodes(int mapKey, IReadOnlyList<StageProgressData> nodes, int currentNodeId, List<StageNodeDropTarget> dropTargets = null)`로 전달한다. null은 연출 수집을 요청하지 않는 기존 표시다.
- [x] 새 건물은 `PlaceBuilding`이 활성 Renderer로 크기와 최종 위치를 계산한 **직후** 대상 자세를 잡고 Renderer를 숨긴다. 측정 전에 숨겨 bounds 계산을 깨뜨리지 않는다. 표시 재사용 분기도 동일하게 원래 자세를 수집하며, 중간 콜백의 Scope/세대 교체는 기존 검사 흐름으로 처리한다.
- [x] 현재 위치를 적용한 뒤 `FinalizeDropTargets`가 기존 건물 표시 정책/조회로 실제 대상만 남긴다. 시작 장식·현재/클리어 건물은 복원 후 목록에서 제외하고, 보이는 일반·보스 건물만 유지한다. 다른 클래스에 완료/활성 정책을 복제하지 않는다.
- [x] 착지 효과의 Pose는 모델 피벗이 아니라 노드 기준 지면으로 계산한다. 위치는 노드 루트의 `TransformPoint(0, BuildingHeight, 0)`, 회전은 맵 루트 회전이다. 낙하시키는 것은 건물 외형이며 노드 루트·연결선·마커는 고정한다.
- [x] **정적 확인:** 부분 생성 실패에서도 이미 숨긴 Renderer를 복원하고, 정상 표시/재사용/교체 분기 모두 목록을 정리하는지 확인한다. **사용자 확인:** U1·U6의 순간 노출, 회전/스케일된 맵, 복수 Renderer와 원래 비활성 Renderer, 현재/클리어 숨김.

### ND3: 착지 효과의 완료·반환

**Files:** 새 `V/Entry/StageNodeDropEffects.cs`와 `.meta`.

**Interfaces:** `StageNodeDropEffects(IEffectSystemServices effects)`; `long Begin(Transform parent)`, `void Play(long generation, Pose groundPose)`, `bool IsComplete(long generation)`, `void End(long generation)`. System 수명의 객체이며 실제 EffectScope는 최초 사용 시 한 번만 빌린다.

- [x] `Begin`은 씬 연결의 고유한 양수 세대를 발급한다. 이전 소유가 반환되지 않은 중복 Begin은 상태 오류로 거절한다. 준비/연출 취소는 자신의 세대를 `End`에 넘긴다.
- [x] 각 `Play`는 해당 효과만 자식으로 두는 고정 받침에 `EffectScopeRequest(EffectType.FallingRocks, ...)`를 보낸다. 월드 Pose를 받침 local 좌표로 변환한다. 받침은 소유 씬의 독립 루트에 월드 단위 스케일로 두어 건물 이동과 맵의 회전/비균일 스케일을 상속하지 않는다. 공용 풀에 파티클 localScale 보정이 남는 것을 방지한다.
- [x] 성공적으로 재생한 받침의 `childCount == 0`으로 풀 반환을 관찰한다. `IsComplete`는 현재 세대의 모든 받침을 확인하고, 이미 끝난 이전 세대는 종료된 것으로 취급하되 새 받침을 조회하지 않는다. 효과 재생 실패를 자연 완료로 삼지 않는다.
- [x] `End`는 현재 소유 세대일 때만 `StopAll`을 호출하고 반환 뒤 자신이 만든 받침을 정리한다. 이전 세대/반복 End는 새 자원에 영향을 주지 않는다. 노티파이어 콜백 교체·빌린 파티클 Destroy·매번 GetEffectHandle 호출은 하지 않는다.
- [x] **정적 확인:** 정상 종료, 재생 중 실패, 시간 상한, 이탈, 이전 세대 재호출의 반환 경로를 추적한다. **사용자 확인:** U5의 자연 종료·효과 누락/반복 설정·재연결. 공용 효과 에셋을 검사용으로 수정하지 않는다.

### ND4: 최초 재생 기록과 거리 순서 낙하

**Files:** 새 `V/Entry/StageNodeDropHistory.cs`, `V/Entry/StageNodeDropPresentation.cs`와 `.meta`.

**Interfaces:**

- `StageNodeDropHistory(IRunProgressBufferReadSink runs)`; `bool HasStarted(RunId runId, int mapKey)`, `bool TryMarkStarted(RunId runId, int mapKey)`.
- `StageNodeDropPresentation(StageNodeDropData settings, StageNodeDropHistory history, StageNodeDropEffects effects)`; get-only `IsPending`, `IsPrepared`; `void BeginPreparation()`, `void Prepare(RunId runId, int mapKey, Transform mapRoot, Vector3 playerPosition, IReadOnlyList<StageNodeDropTarget> targets)`, `void Tick(float scaledDeltaTime, bool isLoading, bool isPaused)`, `void Cancel()`.

- [x] History는 실제 최신 RunId와 해당 Run의 시작한 맵 키 집합만 보관한다. 실제 RunId가 바뀌면 집합을 비우고, 빈/이전 Run 요청은 기록하지 않는다. `TryMarkStarted`는 최신 Run을 다시 확인해 이미 시작했거나 문맥이 달라졌으면 false를 반환한다. 이는 View 재생 이력이며 Core/Buffer를 쓰지 않는다.
- [x] Presentation은 생성 시 Preparing이며 상태는 `Preparing → WaitingForLoading → Dropping → WaitingForEffects → Complete`, 취소는 `Cancelled`로 둔다. `BeginPreparation`은 기존 소유 자원을 정리한 뒤 Preparing으로 전환한다. IsPending/IsPrepared는 이 상태에서 파생하며 별도 동기화 플래그를 만들지 않는다.
- [x] Prepare는 대상 목록을 표시용 스냅샷으로 인수하고 실제 플레이어의 현재 노드 위치를 기준으로 `Vector3.ProjectOnPlane(target.GroundPose.position - playerPosition, mapRoot.up.normalized).magnitude`를 계산한다. 최소/최대 거리를 구해 설계의 지연 수식을 적용한다. 대상 1개·같은 거리의 지연은 0, 같은 거리끼리는 동시 시작이다.
- [x] 기능 꺼짐·대상 없음·이미 시작한 맵이면 대상을 복원하고 Complete로 전환한다. 실제 낙하 대상의 Prepare에서는 Effects.Begin으로 소유 세대를 확보한 뒤 로딩 종료와 일시정지 해제까지 숨겨 기다린다. 그 조건을 만족할 때 첫 외형을 드러내기 직전에 최신 문맥의 `TryMarkStarted`에 성공한 경우에만 재생을 시작한다. 시작하지 못한 세대는 End 후 복원하고, 시작 후 중단한 경우 재진입은 최종 배치다.
- [x] 각 대상의 시작 지연 뒤 `height * (1 - t * t)`로 원래 착지 위치까지 가속 낙하한다. 착지마다 정확한 자세 복원과 효과 Play를 한 번 수행한다. 시간 0·큰 deltaTime에서도 착지/효과가 중복되지 않게 한다.
- [x] 마지막 착지 효과를 실제 발행한 뒤 대기 시계를 시작한다. 한 Tick의 deltaTime을 낙하와 새 대기 단계에 이중 소비하지 않는다. 모든 효과 반환 또는 게임 시간 3초 상한에서 End 후 Complete로 전환하고, 상한 초과 경고는 한 번만 남긴다.
- [x] isPaused일 때 시간 진행·처음 시작·시간 0 착지·효과 발행·최초 기록을 보류한다. Cancel은 일시정지/자연 종료를 기다리지 않고 효과 반환→대상 자세/표시 복원을 수행한다. 호출 도중 취소된 세대는 작업을 이어가지 않는다.
- [x] **정적 확인:** 상태 전이와 이력 기록 위치, 동일 거리/0/빈 목록, Run 교체·취소 시 소유권을 대조한다. **사용자 확인:** U2·U4·U5·U6.

### ND5: 서비스·Factory·로딩·씬 수명 연결

**Files:** 새 `V/Entry/StageMapLoadingPreparation.cs`와 `.meta`; 수정 `V/StageMapViewSystem.cs`, `V/StageMapViewServices.cs`, `V/IStageMapSceneFactory.cs`, `F/StageMapSceneFactory.cs`, `V/StageMapViewScope.cs`.

**Interfaces:**

- `StageMapViewServices`에 필수 `RunProgress: IRunProgressBufferReadSink`, `Loading: ILodingSystemService`, `NodeDropHistory: StageNodeDropHistory`, `NodeDropEffects: StageNodeDropEffects`를 추가한다. 대응 생성자 인수는 `runProgress`, `loading`, `nodeDropHistory`, `nodeDropEffects`다.
- 기존 Scope 생성자에 `StageNodeDropPresentation appearance`를 `faults` 앞에 추가한다. Scope에 `LodingPreparationState TryPrepareEntry()`와 `void CancelEntryPreparation()`을 추가한다.
- `IStageMapSceneFactory.Create` 서명은 유지하고 `ILodingPreparation CreatePreparation(StageMapViewScope scope, RuntimeFaultBoundary faults)`를 추가한다. 구현은 새 `StageMapLoadingPreparation(StageMapViewScope scope, RuntimeFaultBoundary faults)`를 만든다.

- [x] System의 PostBindService에서 `buffers.RunProgressReadSink`, `global.LodingServiceCommand.LodingService`, `global.RenderServiceCommand.CoreGroup.EffectSystemServices`를 받는다. History/Effects는 System 연결 수명에 한 번 만들고 Services에 보관한다. Factory는 씬마다 Presentation을 만들고 Scope에 주입한다.
- [x] System.Initialize에 `RegisterScenePreparation(SceneIndices.Stage, new LodingPreparationNode(GetScenePreparation))`를 연결한다. 새 `private ILodingPreparation GetScenePreparation()`은 현재 유효 Scope를 확인하고 Factory.CreatePreparation을 호출한다. 루트 서비스 재등록·기존 Enter/Exit/카메라 우선순위 변경은 없다.
- [x] Scope의 기존 최초 RefreshDisplay 흐름을 `TryPrepareEntry`로 모은다. Run·맵·현재 노드가 아직 준비 전이면 Pending, 유효한 준비 사실에서 누락/모순이 있으면 오류다. 현재 노드가 실제 목록/표시에 있는지 확인하고 배경→노드/숨김→현재 마커→카메라 범위→대상 확정→Presentation.Prepare 순서로 실행한다.
- [x] 준비 작업과 일반 Scope Tick은 이 창구를 함께 사용하되 같은 준비를 두 번 하지 않는다. `ILodingPreparation`의 Complete는 배치·숨김 준비 성공이며 Presentation의 재생 완료와 다르다. 준비에서 IsLoading 종료를 기다리지 않는다.
- [x] LoadingPreparation은 Begin 한 번, Pending 중 Tick, 반복 가능한 Cancel, terminal 상태를 구현한다. 취소/실패 상태를 먼저 확정하고 현재 준비만 정리한다. 성공·취소·실패 모두 임시 Scope 참조를 놓는다. Scope.TryPrepareEntry에서 준비 예외를 기존 오류 경계에 보고하고 Failed로 변환하여, 일반 Tick에서 먼저 준비해도 오류가 누락되지 않게 한다. 어댑터는 이미 보고된 Failed를 재보고하지 않는다. 로딩 표현 없는 경로도 같은 준비를 거친다.
- [x] Scope는 준비 뒤 `appearance.Tick(Time.deltaTime, services.Loading.IsLoading, Time.timeScale == 0f)`를 호출하고 실제 시작은 Presentation이 로딩 종료까지 보류한다. 진행 중 Run/맵 변경·Scope 폐기를 매 호출 경계에서 확인하고 재생 예외도 기존 오류 경계로 보고한다. 일반 RefreshDisplay가 대기/낙하 중 외형을 다시 그리거나 켜지 않게 한다.
- [x] RequestMapRedraw는 기존 연출 취소 뒤 새 준비를 요청한다. Supersede·Dispose·오류 정지는 효과/자세 복원과 보류 요청 폐기를 노드/배경 파괴보다 먼저 수행한다. 재사용 History는 유지하고, 폐기된 Scope의 재진입 콜백은 새 연결을 건드리지 않는다.
- [x] **정적 확인:** Factory 생성→Services 보관→기존 Group/Context 제공→소비 조회, 준비의 정상 대기/실패, BeginReveal과의 선후, 취소 순서를 추적한다. **사용자 확인:** U1·U2·U5.

### ND6: 노드 입력 가드와 보류한 팝업 복원

**Files:** 수정 `V/StageMapInteraction.cs`, `V/StageMapViewInput.cs`, `V/StageMapViewScope.cs`. 기존 SelectionPresentation·팝업 Controller는 읽기만 한다.

**Interfaces:** `StageMapInteraction` 생성자의 `feedback` 뒤에 필수 `Func<bool> canInteractWithNodes`를 추가한다. `StageMapViewInput.Create(StageMapViewScene scene, Func<bool> blocksEntry, Func<bool> canInteractWithNodes, Func<int, int, bool> interceptEntry = null)`로 전달한다. Scope의 `bool CanInteractWithNodes()`, `void RequestNoncombatRestore(int chapter)`, `void TryRestorePendingNoncombat()`가 호출 지점이다.

호버 정리는 `StageMapInteraction`의 새 `internal void ClearNodeHover()`로 분리한다. `StageMapViewInput.TickHover(bool enabled, float deltaTime, bool preservePointer = false)`는 연출 때문에 호버만 억제하는 경우와 기존 포인터 전체 취소를 구분한다.

- [x] CanInteractWithNodes는 현재 Scope·준비된 표시·로딩 종료·연출 비대기에서 파생한다. 이것을 노드 호버·누르기·떼기·최종 RequestEntry까지 확인한다. 차단 중 누른 노드는 Blocked 소유로 소비하고, 종료 뒤 뗌으로 확정하지 않는다. 호버 설명을 보내기 전에도 가드를 적용한다.
- [x] 준비/재그리기 시작에서 기존 누름·호버를 ClearPointer로 한 번 정리한다. StopInput과 Layer 해제는 임시 차단에 사용하지 않는다. 기존 UI 가림과 Background 소유의 드래그·휠 처리는 유지하며, 차단된 노드 클릭이 배경 드래그로 잘못 바뀌지 않게 한다.
- [x] `ClearNodeHover`는 호버 대상/알림만 비우고 누름 소유권이나 카메라 드래그를 종료하지 않는다. 현재 `TickHover(false)`의 매 Tick `ClearPointer()` 호출을 분리하여, 호버 비활성이고 preservePointer가 true면 ClearNodeHover만 호출하고 false면 기존 전체 취소를 유지한다. 호버 표시/윤곽선에는 어느 경우든 비활성 상태를 전달한다. Scope는 유효한 맵 선택 입력 문맥에서 연출 대기만이 차단 이유일 때 preservePointer를 true로 전달한다. 그동안 Background·Blocked 소유권은 떼기/취소까지 유지하고, 설정·팝업·Layer 이탈·씬 해제에 따른 기존 전체 취소는 우선한다.
- [x] 같은 진행 조건을 CanShowSelection, InterceptEntry, CanOpenDeckEdit, OpenTraitGrowthWhenIdle, CanUseDevelopmentTools와 호버 Tick에 반영한다. 설정 메뉴와 `CanOpenScreens`의 부모 수명 의미는 유지한다. 최초 선택 UI 예약은 HideSelection으로 지우지 않는다.
- [x] 기존 `RestoreNoncombat` 요청 입구를 RequestNoncombatRestore로 바꾼다. 현재 Scope의 Run/맵 식별과 복원 요청만 보류하며 상점/회복 사실을 복사하지 않는다. 최초 표시와 EntryObserver에서 오는 요청을 같은 입구로 모은다.
- [x] TryRestorePendingNoncombat는 연출 완료·허용 Layer·유효한 화면 부모·처리 요청 없음·다른 팝업 없음일 때 현재 Buffer를 다시 읽는다. 문맥이 달라진 요청은 폐기하고 유효한 요청은 소비 표시를 먼저 한 뒤 기존 TryRestore로 한 번 전달한다. 임시 차단이면 요청을 유지한다.
- [x] Tick 끝 순서를 기존 팝업 처리→보류 복원→기존 증강 열기→선택 UI 예약 소비→호버로 정리한다. 생략 경로도 같은 순서를 쓰며 이탈·재연결·Run/맵 교체에서는 보류 요청을 지운다.
- [x] **정적 확인:** UI 표시와 입력 수신을 분리해서 모든 제출 경로를 추적하고, 복원 요청이 유실/중복되지 않는지 확인한다. 연출 중 호버 Tick이 EndSelection/EndDrag를 호출하지 않는지, 차단된 누름의 Blocked 소유권이 유지되는지, 설정·팝업의 기존 전체 취소가 보존되는지 확인한다. **사용자 확인:** U3·U4의 잔여 클릭, 낙하/착지 효과 대기를 가로지르는 배경 드래그·휠·설정 창, 팝업과 초기 선택 UI 복원.

### ND7: 정적 검토·가이드·Phase 보고

**Files:** 위 변경 파일 전체; `docs/superpowers/viewDocs/stage-map-guide-Hojin.md`, 이 계획; 새 `docs/superpowers/handoffs/2026-11-06-stage-node-drop-framework-share-Hojin.md`.

- [x] 이 계획의 Task와 설계 요구를 아래 대응표로 대조한다. 설정·기록·진행의 SSOT, SOLID 5원칙, JMH 역할 분리, FW-004/011/012/016과 실제 영향 범위를 검토한다.
- [x] 변경된 생성자/메서드의 모든 호출처와 `.meta`/GUID/네임스페이스를 확인한다. `git diff --check`와 새 파일 형식 검사를 실행하고 UTF-8/LF·충돌 표식·참조 누락을 확인한다. 컴파일 성공으로 보고하지 않는다.
- [x] 서비스 검토 기록에 기존 EffectSystem의 내부 등록→RenderCoreGroup→Global Context→StageMap 소비, Factory의 생성/주입, 각 수명 부품의 정리 위치를 남긴다. 공용 등록/키/Group을 추가하지 않았는지 확인한다.
- [x] 가이드에 최초 재생 조건, SO 조정 위치, 설정 창 일시정지, 취소/재진입과 사용자 확인표를 추가한다. 공유 메모에는 StageMapSceneFactory/로딩 준비 연결 변경만 기록하고 **미전달**로 둔다. 외부 메시지는 보내지 않는다.
- [x] 실행 기록에 실제 수정 파일, 정적 확인 결과, 미확인 사항과 이 계획에서 달라진 점을 적는다. 사용자 승인 없는 설계 변경이나 보호 영역 수정이 필요하면 이 단계까지 밀어붙이지 말고 발견한 시점에 멈춘다.
- [x] **Phase 종료 보고:** 구현·정적 확인과 사용자 Unity 인수를 구분하여 보고한다. 컴파일 미확인·재생 미확인, 커밋/push 없음, U1~U6을 인계하고 멈춘다.

## 사용자 Unity 확인표

아래는 사용자에게 인계할 항목이며 계획 작성이나 정적 검토만으로 완료 표시하지 않는다.

| ID | 확인 경로 | 기대 결과 |
|---|---|---|
| U1 | 새 Run→첫 챕터, 맵 준비 지연, 로딩 표현 없는 개발 진입 | 준비 전 Reveal/완성 건물 순간 노출 없음. 준비 완료 뒤 로딩이 끝나고 낙하 시작. 상호 대기 없음 |
| U2 | 같은 챕터 전투 복귀·상점/회복 종료·재그리기, 다음 챕터, 새 Run | 같은 Run/챕터는 한 번. 다음 챕터·새 Run은 다시 재생. 시작 전 취소는 재시도, 시작 후 취소는 재진입 생략 |
| U3 | 낙하 중 노드를 누르고 종료 후 떼기, 낙하/착지 효과 대기 중 배경을 눌러 여러 프레임 드래그하고 연출 종료 뒤 떼기, 정지 포인터, 팝업 복원 | 잔여 클릭 확정·호버 설명·윤곽선 없음. 호버 억제가 배경 드래그나 Blocked 소유권을 매 프레임 해제하지 않음. 완료 후 새 클릭 정상. 팝업 복원 한 번·초기 선택 UI 복원. 기존 휠 정책과 설정/팝업의 전체 입력 취소 유지 |
| U4 | 준비 직전/낙하 중/착지 효과 대기 중 설정 창 열고 닫기 | 위치·파티클·3초 상한 동시 정지, 재개 후 이어짐. 시간 0도 정지 우회 없음. 정지 중 취소는 즉시 정리 |
| U5 | 자연 효과 종료, 상한 초과 경로, 씬 이탈/오류/재연결 | 효과 반환 뒤 받침·노드 정리. 대기 상한 경고 한 번. 이전 세대의 정리/조회가 새 효과에 영향 없음 |
| U6 | 챕터 1·2, 동일 거리/대상 1개/없음, 설정 0/꺼짐, 회전·스케일·복수 Renderer, 스케일된 맵에서 새 Run 반복 후 전투의 FallingRocks | 거리 순서·동시 시작·0 의미 정확. 원래 자세/숨김 보존. 반복 재생/전투에서 효과 크기 누적 변경 없음. 운영 기본값 일치. DB 에셋 교차 검증 `DataBaseAssetTests` 확인 |

U5의 오류/상한 경로를 기존 도구로 재현할 수 없으면 **미재현**으로 기록한다. 이를 위해 새 시험 코드·시험 도우미나 공용 에셋 변형을 만들지 않는다.

## 설계 요구 대응과 독립성

| 설계 요구 | 구현 Task |
|---|---|
| 본부와 같은 낙하/효과, 거리 순서, 튜닝 값 | ND1·ND2·ND3·ND4 |
| 현재/클리어·시작 장식·마커·연결선 유지 | ND2·ND6 |
| 챕터 최초 한 번, 새 Run, 취소·재진입 | ND4·ND5 |
| 검수 보완 1: 로딩 전 준비 | ND2·ND5 |
| 검수 보완 2: 효과 완료 판정 | ND3·ND4 |
| 검수 보완 3: 입력·팝업 복원 | ND6 |
| 검수 보완 4: 시간·일시정지 | ND4·ND5·ND6 |
| 자원 수명, SSOT/SOLID/JMH/FW, 확인 인계 | ND3·ND4·ND5·ND7 |

속도 조정은 SO, 거리/곡선 변경은 Presentation, 재생 정책 변경은 History, 효과 종료 방식은 Effects에서 처리한다. Scope에 이 기능을 직접 모으면 이 네 변경마다 입력·팝업·로딩 코드를 함께 읽어야 하므로 분리한다. 실제 공통 경계인 입력 허용·팝업 복원·준비/해제 순서를 바꿀 때는 U1~U5의 통합 확인을 함께 수행한다.

## 실행 기록

- 2026-11-06: 보완된 설계를 기준으로 계획 작성 및 자체 대조 완료. 기준 HEAD는 `88377dec170b74748cdbe47c48010a8de793b024`이며 실행 시작 시 다시 확인한다.
- 2026-11-06: 계획 검수 P2 1건을 수용하여 ND6에 호버만 정리하는 창구와 포인터 전체 취소의 분리, Background·Blocked 소유권 유지, 기존 설정/팝업 취소 우선 조건을 추가했다. Review Focus·U3·정적 확인 항목도 함께 보완했다. 문서 보완이며 구현 착수나 완료를 뜻하지 않는다.
- 계획 작성 시 모든 ND Task는 미착수였다. 아래 Phase 1 실행 기록을 최신 상태로 읽는다. 컴파일 미확인·Unity 재생 미확인.
- 계획 작성 때 존재한 별도 수정: FMOD 이벤트 Catalog, FMOD Editor Cache, C3 FBX 두 경로. 이 변경들은 본 작업 범위에 포함하지 않는다.

**리뷰 지점:** Phase 1 코드·정적 검토 보고 후 멈추고 사용자 Unity 인수를 기다린다. 다음 Phase나 추가 기능은 시작하지 않는다.

### Phase 1 실행 기록

- 2026-11-06 사용자 지시로 Phase 1 착수. HEAD는 계획 작성 시와 같은 `88377dec170b74748cdbe47c48010a8de793b024`, 브랜치는 `feature/Hojin/11_03_StageSelect`다. 계획의 지정 루트에서 작업하며 기존 FMOD·FBX 변경은 유지한다.
- 실행 방식: ND1~ND7 순차 구현, 정적 확인, 마지막 별도 문맥 리뷰. 사용자 지시에 따라 테스트 작성·실행, MSBuild, Unity 재생, 커밋·push와 그에 의존하는 스킬 자동 기록 스크립트는 사용하지 않는다. 이 절에 Task별 근거와 상태를 남긴다.
- 사전 인터페이스 대조: ND1 설정→ND4 Presentation, ND2 대상 목록→ND4 Prepare, ND3 세대별 효과→ND4 재생/취소, ND4 상태→ND5 준비/수명→ND6 입력/팝업 조건, ND6 호버 분리→U3 확인 경로가 연결된다. 발견된 추가 충돌 없음.
- ND1 구현·정적 대조: 불변 설정/검사, SO 입력과 운영 값 30/0.8/0.6을 연결했다. 생성 호출은 SO의 ToData 한 곳이며, 관련 기존 시험 호출은 검색에서 발견되지 않았다.
- ND2 구현·정적 대조: PlaceBuilding 뒤 Renderer 숨김, 재사용 외형 수집, 현재/완료 정책 적용 후 대상 확정, 실패/취소 복원을 연결했다. 노드 루트·Collider·진행 Buffer는 낙하 부품이 변경하지 않는다.
- ND3 구현·정적 대조: System 수명의 EffectScope 한 개, 고정 받침의 childCount 반환 관찰, 소유 세대 검사, StopAll 뒤 받침 정리를 구현했다. Play 도중 취소는 Play의 내부 등록이 끝난 뒤 반환하여 이전 세대가 새 효과를 정리하지 않도록 했다.
- ND4 구현·정적 대조: 최신 Run/맵별 실제 시작 기록, 거리 지연/가속 낙하, 설정 0/꺼짐/대상 없음, 일시정지, 마지막 착지 이후 3초 대기, 반복 취소를 구현했다. 큰 deltaTime에서도 낙하 Tick의 시간을 새 효과 대기에 재사용하지 않는다.
- ND5 구현·정적 대조: 기존 Global/Buffer 경로로 서비스를 받고 Factory에서 씬 연출/준비를 조립했다. Stage 준비는 배치·숨김만 기다리고 Reveal 뒤 실제 연출을 시작한다. Scope 준비 세대로 늦은 준비 취소를 분리하며, 정리 순서는 효과/자세→노드/배경이다.
- ND6 구현·정적 대조: 노드 입력의 각 수신/최종 제출 가드, 호버만 정리하는 경로, Background/Blocked 유지, 보류 팝업의 현재 사실 재조회와 복원→증강→선택 순서를 연결했다. 초기 선택 예약과 기존 팝업 Controller는 유지한다.
- 위 상태는 코드와 호출 경로의 정적 대조이며 컴파일·Unity 재생 성공을 뜻하지 않는다. ND7 문서·최종 정적 검토와 별도 문맥 리뷰 진행 중.
- ND7 정적 확인: 수정 파일 14개와 신규 파일/기존 설계·계획을 포함한 30개 파일의 UTF-8/LF·충돌 표식·C# meta 존재 확인 통과, 신규 meta 7개의 GUID 중복 없음. 기존 CRLF는 본 작업의 수정 파일만 LF로 맞췄다. 범위를 한정한 `git diff --check`는 오류 0이다.
- 생성자/메서드 호출처와 기존 시험 참조를 검색해 SO→Data, System→Services, Factory→Scope/Preparation, Input→Interaction 전달을 대조했다. 보호 영역과 기존 시험 파일 변경 없음. Git 전체 diff의 기존 FBX LFS 필터 실행 오류 때문에 작업 파일 경로를 명시해 정적 diff 검사를 수행했다. Git 설정은 변경하지 않았다.
- 구조 확인: 설정 원본은 SO, 재생 이력은 History, 진행 상태는 Presentation, 표시 정책은 NodeView다. Effects는 기존 Render 계약만 받고, Factory가 씬 부품을 조립한다. ILodingPreparation 완료는 배치 준비이며 재생 완료와 분리한다. 기존 입력 취소 계약과 FW-004/011/012/016 경계를 유지하고 새 루트 등록은 없다. 남는 통합 확인 경계는 로딩·입력·팝업·해제(U1~U5)다.
- 안내 가이드 §3.1·§9 및 Framework 공유 메모를 작성했다. 공유 메모는 **미전달**이다. 사용자 U1~U6, `DataBaseAssetTests`, 컴파일·재생은 모두 미확인이다.
- 최종 별도 문맥 리뷰: Critical 0, Important 1, Minor 0, 판단 제외 0. Important는 맵 스케일 상속으로 공용 효과 풀의 localScale이 반복해서 달라지는 경로다. `EffectScope.Play`의 부모 변경(월드 보존)과 `EffectPool.OnBeforeReturn`의 부모 변경(local 유지)을 직접 읽어 원인을 확인했다.
- 리뷰 지적 수용·수정: Stage 전용 받침을 소유 씬의 독립 루트로 옮기고 월드 단위 스케일을 유지한다. 회전/비균일 스케일도 상속하지 않으며 공용 Render/프리팹 수정은 없다. ND3와 U6에 이 조건을 명시했다. 수정은 정적 확인했으며 Unity 재현·수정 후 재생은 미확인이다. 사용자 제한에 따라 시험 코드는 추가하지 않았고 재리뷰는 실행하지 않았다.
- ND7 완료: 수정 후 `git diff --check` 오류 0, 신규 파일 15개의 UTF-8/LF·후행 공백·충돌 표식 검사 통과, 문서의 로컬 링크 3개 확인. Core/Buffer·공용 Loading/Render·기존 시험 diff 없음. 리뷰 Important 1건은 위 정적 보완으로 처리했고 보류한 Minor/판단 제외 항목은 없다.
- **Phase 1 코드 구현·정적 검토·문서 인계 완료. 컴파일 미확인·Unity 재생 미확인.** U1~U6과 `DataBaseAssetTests`는 사용자 확인 대기다. 새 시험 코드/도우미·MSBuild·테스트·Unity 실행·커밋·push 없음. 이 Phase에서 멈춘다.
- 2026-11-06 후속 확인: 사용자에게 Unity 확인 항목과 방법을 안내한 뒤 사용자가 "네 잘 돌아가고 있습니다"라고 정상 동작을 확인했다. **사용자 Unity 정상 재생 확인**으로 기록하며, Codex 직접 컴파일·재생 검증은 수행하지 않았다. U1~U6의 개별 예외 경로·`DataBaseAssetTests` 실행 여부까지 확인한 것으로 확대하지 않는다. 이후 논의한 선 그리기·카메라 왕복은 [새 진입 연출 설계](../specs/2026-11-06-stage-map-intro-design-Hojin.md)의 후속 범위이며 이 Phase의 기존 구현 완료와 구분한다.
